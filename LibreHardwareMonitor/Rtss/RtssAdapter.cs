﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using LibreHardwareMonitor.Hardware;
using LibreHardwareMonitor.UI;
using LibreHardwareMonitor.Utilities;
using RTSSSharedMemoryNET;

namespace LibreHardwareMonitor.Rtss
{
    public sealed class RtssDisplayItem
    {
        public string SensorName { get; set; }

        public SensorType SensorType { get; set; }

        public string HardwareName { get; set; }

        public string Color { get; set; } // hex RRGGBB

        public Func<float?> Value { get; set; }
    }

    public sealed class RtssAdapter
    {
        private const string RtssTags = "<P=0,0><A0=-6><A1=4><C0=FFA0A0><C1=FF00A0><C2=FFFFFF><S0=-50><S1=-75><S2=50>";
        private const string RtssNewLine = "\n";
        private const string RtssNewLine2 = "\n\n";

        public bool GroupByType { get; }
        public bool UseSensorNameAsKey { get; }
        public bool AddFpsDetails { get; }

        private readonly PersistentSettings _settings;
        private readonly UnitManager _unitManager;
        private readonly RtssService _rtssService;

        private readonly List<RtssDisplayItem> _displayItems = new List<RtssDisplayItem>();

        private bool _enabled;
        private bool _initializing;

        private OSD _osd;

        public string RtssServiceLocation
        {
            get { return _rtssService.RtssServiceLocation; }
            set { _rtssService.RtssServiceLocation = value; }
        }

        public bool IsRunning
        {
            get => _rtssService.IsRunning;
        }

        public bool IsAvailable
        {
            get => _rtssService.IsAvailable;
        }

        public RtssAdapter(PersistentSettings settings, UnitManager unitManager)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _unitManager = unitManager ?? throw new ArgumentNullException(nameof(unitManager));
            _rtssService = new RtssService(_settings);

            GroupByType = _settings.GetValue("RtssAdapter.GroupByType", false);
            UseSensorNameAsKey = _settings.GetValue("RtssAdapter.UseSensorNameAsKey", false);
            AddFpsDetails = _settings.GetValue("RtssAdapter.AddFpsDetails", true);
        }

        public void SetSensors(List<ISensor> sensors, IDictionary<ISensor, Color> colors)
        {
            _displayItems.Clear();

            _displayItems.AddRange(sensors.Select(s => new RtssDisplayItem
            {
                SensorName = s.Name,
                SensorType = s.SensorType,
                HardwareName = s.Hardware.Name,
                Color = HexConverter(colors[s]),
                Value = () => s.Value
            }));
        }

        private int _try;

        public void InvalidateData()
        {
            if (_enabled)
            {
                if (_osd == null && !_initializing)
                {
                    if (_try <= 0 && _rtssService.IsRunning)
                    {
                        Open();

                        _try = 5;
                    }

                    _try--;
                }

                if (_osd != null)
                {
                    _try = 0;

                    string data = _displayItems.Count > 0 ? FormatData() : string.Empty;

                    try
                    {
                        _osd.Update(data);
                    }
                    catch (Exception)
                    {
                        Close();
                    }
                }
            }
        }

        private string FormatData()
        {
            string FormatValue(RtssDisplayItem item)
            {
                float? v = item.Value();

                if (!v.HasValue)
                {
                    return "-";
                }

                string unit = _unitManager.GetUnit(item.SensorType, v);

                if (item.SensorType == SensorType.Temperature)
                {
                    v = _unitManager.LocalizeTemperature(v);
                }
                if (item.SensorType == SensorType.Throughput)
                {
                    v = _unitManager.ScaleThroughput(v);
                }

                string formatted = string.Format(_unitManager.GetFormat(item.SensorType), v);

                return $"<A0>{formatted}<A><A1><S2> {unit}<S><A>";
            }

            string result;

            //add order by sensor types in both cases
            //hardware should be ordered in same way as in tree

            if (GroupByType)
            {
                // group items by type

                string FormatName(RtssDisplayItem item)
                {
                    if (UseSensorNameAsKey)
                        return $"{item.SensorName}<S0> ({item.HardwareName})<S>";
                    return $"{item.HardwareName}<S0> ({item.SensorName})<S>";
                }

                var data = _displayItems
                    .GroupBy(i => i.SensorType)
                    .OrderBy(i => i.Key);

                var d = data.Select(subgroup =>
                {
                    string group = $"<S1>{subgroup.Key}:{RtssNewLine}<S>";

                    IEnumerable<RtssDisplayItem> displayItems = UseSensorNameAsKey
                        ? subgroup.OrderBy(i => i.SensorName)
                        : subgroup.OrderBy(i => i.HardwareName);

                    var items = displayItems
                        .Select(item => $"<C={item.Color}>{FormatName(item)}:\t{FormatValue(item)}<C>");

                    group += string.Join(RtssNewLine, items);

                    return group;
                });

                result = string.Join(RtssNewLine2, d);
            }
            else
            {
                // group items by hardware

                string FormatName(RtssDisplayItem item)
                {
                    if (UseSensorNameAsKey)
                        return $"{item.SensorName}<S0> ({item.SensorType})<S>";
                    return $"{item.SensorType}<S0> ({item.SensorName})<S>";
                }

                var data = _displayItems
                    .GroupBy(i => i.HardwareName)
                    .OrderBy(i => i.Key);

                var d = data.Select(subgroup =>
                {
                    string group = $"<S1>{subgroup.Key}:{RtssNewLine}<S>";

                    IEnumerable<RtssDisplayItem> displayItems = UseSensorNameAsKey
                        ? subgroup.OrderBy(i => i.SensorName)
                        : subgroup.OrderBy(i => i.SensorType);

                    var items = displayItems
                        .Select(item => $"<C={item.Color}>{FormatName(item)}:\t{FormatValue(item)}<C>");

                    group += string.Join(RtssNewLine, items);

                    return group;
                });

                result = string.Join(RtssNewLine2, d);
            }

            if (AddFpsDetails)
            {
                if (result.Length > 0)
                {
                    result += RtssNewLine2;
                }

                result += "<C2><APP><C>	<A0><FR><A><A1><S1> FPS<S><A> <A0><FT><A><A1><S2> ms<S><A>";
            }

            return RtssTags + result;
        }

        public void SaveCurrentSettings()
        {
             _settings.SetValue("RtssAdapter.GroupByType", GroupByType);
             _settings.SetValue("RtssAdapter.UseSensorNameAsKey", UseSensorNameAsKey);
        }

        public void Start()
        {
            if (_enabled)
                return;

            if (!_rtssService.IsRunning)
            {
                if (_initializing)
                    return;

                _initializing = true;

                if (_rtssService.TryRun())
                {
                    Task.Delay(TimeSpan.FromSeconds(2)).ContinueWith(t =>
                    {
                        _initializing = false;
                    }, TaskScheduler.FromCurrentSynchronizationContext());
                }
                else
                {
                    _initializing = false;
                }
            }

            _enabled = true;
        }

        public void Stop()
        {
            if (!_enabled)
                return;

            Close();

            _enabled = false;
        }

        private void Open()
        {
            try
            {
                _osd = new OSD("LibreHardwareMonitor.RtssAdapter");
            }
            catch (Exception)
            {
            }
        }

        private void Close()
        {
            try
            {
                _osd?.Dispose();
            }
            catch (Exception)
            {
            }

            _osd = null;
        }

        private static string HexConverter(Color c)
        {
            return c.R.ToString("X2") + c.G.ToString("X2") + c.B.ToString("X2");
        }
    }
}
