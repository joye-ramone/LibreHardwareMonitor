using System;
using System.Linq;
using System.Collections.Generic;
using LibreHardwareMonitor.Hardware;
using LibreHardwareMonitor.Utilities;
using OxyPlot;
using OxyPlot.Annotations;
using OxyPlot.Axes;
using OxyPlot.Series;

namespace LibreHardwareMonitor.UI
{
    public sealed class ScaledPlotModel : PlotModel
    {
        internal static readonly Dictionary<SensorType, string> Units = new Dictionary<SensorType, string>
        {
            { SensorType.Voltage, "V" },
            { SensorType.Clock, "MHz" },
            { SensorType.Temperature, "°C" },
            { SensorType.Load, "%" },
            { SensorType.Fan, "RPM" },
            { SensorType.Flow, "L/h" },
            { SensorType.Control, "%" },
            { SensorType.Level, "%" },
            { SensorType.Factor, "1" },
            { SensorType.Power, "W" },
            { SensorType.Data, "GB" },
            { SensorType.Frequency, "Hz" }
        };

        private readonly TimeSpanAxis _timeAxis = new TimeSpanAxis();

        private readonly IDictionary<SensorType, LinearAxis> _axes = new SortedDictionary<SensorType, LinearAxis>();
        private readonly IDictionary<SensorType, LineAnnotation> _annotations = new Dictionary<SensorType, LineAnnotation>();

        private readonly PersistentSettings _settings;
        private readonly UnitManager _unitManager;

        private bool _showValueAxesLabels;

        public bool ShowValueAxesLabels
        {
            get { return _showValueAxesLabels; }
            set
            {
                if (_showValueAxesLabels != value)
                {
                    _showValueAxesLabels = value;

                    UpdateValueAxesLabels();
                }
            }
        }

        private void UpdateValueAxesLabels()
        {
            foreach (var a in _axes)
            {
                LinearAxis axis = a.Value;

                if (_showValueAxesLabels)
                {
                    axis.Title = a.Key.ToString();

                    if (Units.ContainsKey(a.Key))
                    {
                        axis.Unit = Units[a.Key];
                    }
                }
                else
                {
                    axis.Title = null;
                    axis.Unit = null;
                }
            }
        }

        private bool _enableValueAxesZoom;

        public bool EnableValueAxesZoom
        {
            get { return _enableValueAxesZoom; }
            set
            {
                if (_enableValueAxesZoom != value)
                {
                    _enableValueAxesZoom = value;

                    UpdateValueAxesZoom();
                }
            }
        }

        private void UpdateValueAxesZoom()
        {
            foreach (LinearAxis axis in _axes.Values)
                axis.IsZoomEnabled = _enableValueAxesZoom;
        }

        public bool EnableTimeAxisZoom
        {
            get { return _timeAxis.IsZoomEnabled; }
            set { _timeAxis.IsZoomEnabled = value; }
        }

        private bool _stackedAxes;

        public bool StackedAxes
        {
            get { return _stackedAxes; }
            set
            {
                if (_stackedAxes != value)
                {
                    _stackedAxes = value;

                    UpdateValueAxesPosition();
                }
            }
        }

        public ScaledPlotModel(PersistentSettings settings, UnitManager unitManager)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _unitManager = unitManager ?? throw new ArgumentNullException(nameof(unitManager));

            InitDefaults();

            _timeAxis.Position = AxisPosition.Bottom;
            _timeAxis.MajorGridlineStyle = LineStyle.Solid;
            _timeAxis.MajorGridlineThickness = 1;
            _timeAxis.MajorGridlineColor = OxyColor.FromRgb(192, 192, 192);
            _timeAxis.MinorGridlineStyle = LineStyle.Solid;
            _timeAxis.MinorGridlineThickness = 1;
            _timeAxis.MinorGridlineColor = OxyColor.FromRgb(232, 232, 232);
            _timeAxis.StartPosition = 1;
            _timeAxis.EndPosition = 0;
            _timeAxis.MinimumPadding = 0;
            _timeAxis.MaximumPadding = 0;
            _timeAxis.AbsoluteMinimum = 0;
            _timeAxis.Minimum = 0;
            _timeAxis.AbsoluteMaximum = 24 * 60 * 60;
            _timeAxis.Zoom(
              _settings.GetValue("plotPanel.MinTimeSpan", 0.0f),
              _settings.GetValue("plotPanel.MaxTimeSpan", 10.0f * 60));

            _timeAxis.StringFormat = "h:mm";

            foreach (SensorType type in Enum.GetValues(typeof(SensorType)))
            {
                string typeName = type.ToString();

                var axis = new LinearAxis
                {
                    Position = AxisPosition.Left,
                    MajorGridlineStyle = LineStyle.Solid,
                    MajorGridlineThickness = 1,
                    MajorGridlineColor = _timeAxis.MajorGridlineColor,
                    MinorGridlineStyle = LineStyle.Solid,
                    MinorGridlineThickness = 1,
                    MinorGridlineColor = _timeAxis.MinorGridlineColor,
                    AxislineStyle = LineStyle.Solid,
                    Title = typeName,
                    Key = typeName,
                };

                var annotation = new LineAnnotation
                {
                    Type = LineAnnotationType.Horizontal,
                    ClipByXAxis = false,
                    ClipByYAxis = false,
                    LineStyle = LineStyle.LongDash,
                    Color = OxyColors.Black,
                    YAxisKey = typeName,
                    StrokeThickness = 1,
                };

                axis.AxisChanged += (sender, args) => annotation.Y = axis.ActualMinimum;
                axis.TransformChanged += (sender, args) => annotation.Y = axis.ActualMinimum;

                axis.Zoom(
                    _settings.GetValue("plotPanel.Min" + axis.Key, float.NaN),
                    _settings.GetValue("plotPanel.Max" + axis.Key, float.NaN));

                if (Units.ContainsKey(type))
                {
                    axis.Unit = Units[type];
                }

                _axes.Add(type, axis);
                _annotations.Add(type, annotation);
            }

            Axes.Add(_timeAxis);

            foreach (LinearAxis axis in _axes.Values)
            {
                Axes.Add(axis);
            }

            UpdateValueAxesLabels();
            UpdateValueAxesZoom();
        }

        private void InitDefaults()
        {
            IsLegendVisible = false;

            Padding = new OxyThickness(14, 8, 14, 8);
        }

        public void ApplyScale(double scaleX, double scaleY)
        {
            PlotMargins = new OxyThickness(PlotMargins.Left * scaleX,
                PlotMargins.Top * scaleY,
                PlotMargins.Right * scaleX,
                PlotMargins.Bottom * scaleY
            );

            Padding = new OxyThickness(Padding.Left * scaleX,
                Padding.Top * scaleY,
                Padding.Right * scaleX,
                Padding.Bottom * scaleY
            );

            TitlePadding *= scaleX;
            LegendSymbolLength *= scaleX;
            LegendSymbolMargin *= scaleX;
            LegendPadding *= scaleX;
            LegendColumnSpacing *= scaleX;
            LegendItemSpacing *= scaleX;
            LegendMargin *= scaleX;
        }

        public void SaveCurrentSettings()
        {
            _settings.SetValue("plotPanel.MinTimeSpan", (float)_timeAxis.ActualMinimum);
            _settings.SetValue("plotPanel.MaxTimeSpan", (float)_timeAxis.ActualMaximum);

            foreach (LinearAxis axis in _axes.Values)
            {
                _settings.SetValue("plotPanel.Min" + axis.Key, (float)axis.ActualMinimum);
                _settings.SetValue("plotPanel.Max" + axis.Key, (float)axis.ActualMaximum);
            }
        }

        public void TimeAxisZoom(double min, double max)
        {
            bool axisIsZoomEnabled = _timeAxis.IsZoomEnabled;

            _timeAxis.IsZoomEnabled = true;
            _timeAxis.Zoom(min, max);

            _timeAxis.IsZoomEnabled = axisIsZoomEnabled;
        }

        public void AutoScaleAllYAxes()
        {
            bool axisIsZoomEnabled = _enableValueAxesZoom;

            foreach (LinearAxis axis in _axes.Values)
            {
                axis.IsZoomEnabled = true;
                axis.Zoom(double.NaN, double.NaN);
                axis.IsZoomEnabled = axisIsZoomEnabled;
            }
        }

        private void UpdateValueAxesPosition()
        {
            if (_stackedAxes)
            {
                int count = _axes.Values.Count(axis => axis.IsAxisVisible);

                double start = 0.0;

                foreach (KeyValuePair<SensorType, LinearAxis> pair in _axes.Reverse())
                {
                    LinearAxis axis = pair.Value;

                    axis.StartPosition = start;
                    start += axis.IsAxisVisible ? 1.0 / count : 0;
                    axis.EndPosition = start;
                    axis.PositionTier = 0;
                    axis.MajorGridlineStyle = LineStyle.Solid;
                    axis.MinorGridlineStyle = LineStyle.Solid;

                    LineAnnotation annotation = _annotations[pair.Key];
                    annotation.Y = axis.ActualMinimum;

                    if (!Annotations.Contains(annotation))
                        Annotations.Add(annotation);
                }
            }
            else
            {
                int tier = 0;

                foreach (KeyValuePair<SensorType, LinearAxis> pair in _axes.Reverse())
                {
                    LinearAxis axis = pair.Value;

                    if (axis.IsAxisVisible)
                    {
                        axis.StartPosition = 0;
                        axis.EndPosition = 1;
                        axis.PositionTier = tier;

                        tier++;
                    }
                    else
                    {
                        axis.StartPosition = 0;
                        axis.EndPosition = 0;
                        axis.PositionTier = 0;
                    }

                    axis.MajorGridlineStyle = LineStyle.None;
                    axis.MinorGridlineStyle = LineStyle.None;

                    LineAnnotation annotation = _annotations[pair.Key];

                    if (Annotations.Contains(annotation))
                        Annotations.Remove(_annotations[pair.Key]);
                }
            }
        }

        public void SetSensors(IEnumerable<ISensor> sensors, Action<ISensor, LineSeries> configure)
        {
            var types = new HashSet<SensorType>();

            Series.Clear();

            foreach (ISensor sensor in sensors)
            {
                var series = new LineSeries
                {
                    StrokeThickness = 1,
                    YAxisKey = _axes[sensor.SensorType].Key,
                    Title = sensor.Hardware.Name + " " + sensor.Name
                };

                configure?.Invoke(sensor, series);

                string typeName = sensor.SensorType.ToString();

                series.TrackerFormatString = "{0}\nTime: {2:hh\\:mm\\:ss\\.fff}\n" + typeName + ": {4:.##}";

                if (Units.ContainsKey(sensor.SensorType))
                {
                    series.TrackerFormatString += " " + Units[sensor.SensorType];
                }

                Series.Add(series);

                types.Add(sensor.SensorType);
            }

            foreach (KeyValuePair<SensorType, LinearAxis> pair in _axes.Reverse())
            {
                LinearAxis axis = pair.Value;
                SensorType type = pair.Key;

                axis.IsAxisVisible = types.Contains(type);
            }

            UpdateValueAxesPosition();

            AutoScaleAllYAxes();
        }

        public void UpdateAxes()
        {
            foreach (KeyValuePair<SensorType, LinearAxis> pair in _axes)
            {
                LinearAxis axis = pair.Value;
                SensorType type = pair.Key;

                if (_showValueAxesLabels && type == SensorType.Temperature)
                    axis.Unit = _unitManager.TemperatureUnit == TemperatureUnit.Celsius ? "°C" : "°F";

                if (!_stackedAxes)
                    continue;

                LineAnnotation annotation = _annotations[pair.Key];
                annotation.Y = axis.ActualMaximum;
            }
        }
    }
}
