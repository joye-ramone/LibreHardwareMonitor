using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
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

        public bool GroupByType { get; } = false;
        public bool UseSensorNameAsKey { get; } = true;

        private readonly PersistentSettings _settings;
        private readonly UnitManager _unitManager;

        private readonly List<RtssDisplayItem> _displayItems = new List<RtssDisplayItem>();

        private readonly OSD _osd;

        public RtssAdapter(PersistentSettings settings, UnitManager unitManager)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _unitManager = unitManager ?? throw new ArgumentNullException(nameof(unitManager));

            try
            {
                _osd = new OSD("LibreHardwareMonitor.RtssAdapter");
            }
            catch(Exception)
            { }

            GroupByType = _settings.GetValue("RtssAdapter.GroupByType", false);
            UseSensorNameAsKey = _settings.GetValue("RtssAdapter.UseSensorNameAsKey", false);
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

        public void InvalidateData()
        {
            if (_osd != null)
            {
                if (_displayItems.Count > 0)
                {
                    string data = FormatData();
                    _osd.Update(data);
                }
                else
                {
                    _osd.Update(string.Empty);
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

                if (item.SensorType == SensorType.Temperature)
                {
                    if (_unitManager.TemperatureUnit == TemperatureUnit.Fahrenheit)
                    {
                        v = UnitManager.CelsiusToFahrenheit(v);
                    }
                }

                return string.Format(GetFormat(item.SensorType), v);
            }

            string FormatUnit(RtssDisplayItem item)
            {
                if (item.SensorType != SensorType.Temperature)
                {
                    return ScaledPlotModel.Units[item.SensorType];
                }

                return _unitManager.TemperatureUnit == TemperatureUnit.Fahrenheit ? "°F" : "°C";
            }

            string result = RtssTags;

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
                        .Select(item =>
                            $"<C={item.Color}>{FormatName(item)}:\t<A0>{FormatValue(item)}<A><A1><S2> {FormatUnit(item)}<S><A><C>");

                    group += string.Join(RtssNewLine, items);

                    return group;
                });

                result += string.Join(RtssNewLine2, d);
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
                        .Select(item =>
                            $"<C={item.Color}>{FormatName(item)}:\t<A0>{FormatValue(item)}<A><A1><S2> {FormatUnit(item)}<S><A><C>");

                    group += string.Join(RtssNewLine, items);

                    return group;
                });

                result += string.Join(RtssNewLine2, d);
            }

            return result;
        }

        public void SaveCurrentSettings()
        {
             _settings.SetValue("RtssAdapter.GroupByType", GroupByType);
             _settings.SetValue("RtssAdapter.UseSensorNameAsKey", UseSensorNameAsKey);
        }

        public void Stop()
        {
            _osd?.Dispose();
        }

        private static string HexConverter(Color c)
        {
            return c.R.ToString("X2") + c.G.ToString("X2") + c.B.ToString("X2");
        }

        private static string GetFormat(SensorType sensorType)
        {
            switch (sensorType)
            {
                case SensorType.Fan:
                    return "{0:F0}";
                case SensorType.Clock:
                case SensorType.Load:
                case SensorType.Flow:
                case SensorType.Control:
                case SensorType.Level:
                case SensorType.Power:
                case SensorType.Data:
                case SensorType.Frequency:
                case SensorType.Throughput:
                case SensorType.SmallData:
                case SensorType.Temperature:
                    return "{0:F1}";
                case SensorType.Voltage:
                case SensorType.Factor:
                    return "{0:F3}";
            }

            return "{0:F1}";
        }
    }
}
