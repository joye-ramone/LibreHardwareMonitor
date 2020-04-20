﻿// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// Partial Copyright (C) Michael Möller <mmoeller@openhardwaremonitor.org> and Contributors.
// All Rights Reserved.

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using LibreHardwareMonitor.Hardware;
using LibreHardwareMonitor.Utilities;
using OxyPlot;
using OxyPlot.Annotations;
using OxyPlot.Axes;
using OxyPlot.WindowsForms;
using OxyPlot.Series;

namespace LibreHardwareMonitor.UI
{
    public class PlotPanel : UserControl
    {
        private readonly PersistentSettings _settings;
        private readonly UnitManager _unitManager;
        private readonly PlotView _plot;
        private readonly PlotModel _model;
        private readonly TimeSpanAxis _timeAxis = new TimeSpanAxis();
        private readonly SortedDictionary<SensorType, LinearAxis> _axes = new SortedDictionary<SensorType, LinearAxis>();
        private readonly Dictionary<SensorType, LineAnnotation> _annotations = new Dictionary<SensorType, LineAnnotation>();

        private UserOption _stackedAxes;
        private UserOption _showAxesLabels;
        private UserOption _timeAxisEnableZoom;
        private UserOption _yAxesEnableZoom;
        private DateTime _now;
        private float _dpiX;
        private float _dpiY;
        private double _dpiXScale = 1;
        private double _dpiYScale = 1;

        private Point _mouseDownLocation;

        protected internal static readonly Dictionary<SensorType, string> Units = new Dictionary<SensorType, string>
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

        public PlotPanel(PersistentSettings settings, UnitManager unitManager)
        {
            _settings = settings;
            _unitManager = unitManager;

            SetDpi();

            _model = CreatePlotModel();

            var menu = CreateMenu();

            _plot = new PlotView
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,

                Model = _model,
                Controller =  new SensorPlotController()
            };

            _plot.MouseDown += (sender, e) => { _mouseDownLocation = e.Location; };
            _plot.MouseUp += (sender, e) =>
            {
                if (e.Button == MouseButtons.Right && _mouseDownLocation == e.Location)
                {
                    menu.Show(_plot, _mouseDownLocation);
                }
            };

            UpdateAxesPosition();

            SuspendLayout();
            Controls.Add(_plot);
            ResumeLayout(true);
        }

        public void SetCurrentSettings()
        {
            _settings.SetValue("plotPanel.MinTimeSpan", (float)_timeAxis.ActualMinimum);
            _settings.SetValue("plotPanel.MaxTimeSpan", (float)_timeAxis.ActualMaximum);

            foreach (LinearAxis axis in _axes.Values)
            {
                _settings.SetValue("plotPanel.Min" + axis.Key, (float)axis.ActualMinimum);
                _settings.SetValue("plotPanel.Max" + axis.Key, (float)axis.ActualMaximum);
            }
        }

        private ContextMenu CreateMenu()
        {
            ContextMenu menu = new ContextMenu();

            MenuItem stackedAxesMenuItem = new MenuItem("Stacked Axes");
            _stackedAxes = new UserOption("stackedAxes", true, stackedAxesMenuItem, _settings);

            MenuItem showAxesLabelsMenuItem = new MenuItem("Show Axes Labels");
            _showAxesLabels = new UserOption("showAxesLabels", true, showAxesLabelsMenuItem, _settings);

            _stackedAxes.Changed += (sender, e) =>
            {
                UpdateAxesPosition();
                InvalidatePlot();
            };
            menu.MenuItems.Add(stackedAxesMenuItem);

            _showAxesLabels.Changed += (sender, e) =>
            {
                foreach (var a in _axes)
                {
                    if (_showAxesLabels.Value)
                    {
                        a.Value.Title = a.Key.ToString();

                        if (Units.ContainsKey(a.Key))
                            a.Value.Unit = Units[a.Key];
                    }
                    else
                    {
                        a.Value.Title = null;
                        a.Value.Unit = null;
                    }
                }
            };
            menu.MenuItems.Add(showAxesLabelsMenuItem);

            MenuItem timeAxisMenuItem = new MenuItem("Time Axis");

            MenuItem[] timeAxisMenuItems =
            {
                new MenuItem("Enable Zoom"),
                new MenuItem("Auto", (s, e) => { TimeAxisZoom(0, double.NaN); }),
                new MenuItem("5 min", (s, e) => { TimeAxisZoom(0, 5 * 60); }),
                new MenuItem("10 min", (s, e) => { TimeAxisZoom(0, 10 * 60); }),
                new MenuItem("20 min", (s, e) => { TimeAxisZoom(0, 20 * 60); }),
                new MenuItem("30 min", (s, e) => { TimeAxisZoom(0, 30 * 60); }),
                new MenuItem("45 min", (s, e) => { TimeAxisZoom(0, 45 * 60); }),
                new MenuItem("1 h", (s, e) => { TimeAxisZoom(0, 60 * 60); }),
                new MenuItem("1.5 h", (s, e) => { TimeAxisZoom(0, 1.5 * 60 * 60); }),
                new MenuItem("2 h", (s, e) => { TimeAxisZoom(0, 2 * 60 * 60); }),
                new MenuItem("3 h", (s, e) => { TimeAxisZoom(0, 3 * 60 * 60); }),
                new MenuItem("6 h", (s, e) => { TimeAxisZoom(0, 6 * 60 * 60); }),
                new MenuItem("12 h", (s, e) => { TimeAxisZoom(0, 12 * 60 * 60); }),
                new MenuItem("24 h", (s, e) => { TimeAxisZoom(0, 24 * 60 * 60); })
            };

            foreach (MenuItem mi in timeAxisMenuItems)
                timeAxisMenuItem.MenuItems.Add(mi);

            menu.MenuItems.Add(timeAxisMenuItem);

            _timeAxisEnableZoom = new UserOption("timeAxisEnableZoom", true, timeAxisMenuItems[0], _settings);
            _timeAxisEnableZoom.Changed += (sender, e) =>
            {
                _timeAxis.IsZoomEnabled = _timeAxisEnableZoom.Value;
            };

            MenuItem yAxesMenuItem = new MenuItem("Value Axes");
            MenuItem[] yAxesMenuItems =
            {
                new MenuItem("Enable Zoom"),
                new MenuItem("Autoscale All", (s, e) => { AutoScaleAllYAxes(); })
            };

            foreach (MenuItem mi in yAxesMenuItems)
                yAxesMenuItem.MenuItems.Add(mi);

            menu.MenuItems.Add(yAxesMenuItem);

            _yAxesEnableZoom = new UserOption("yAxesEnableZoom", true, yAxesMenuItems[0], _settings);
            _yAxesEnableZoom.Changed += (sender, e) =>
            {
                foreach (LinearAxis axis in _axes.Values)
                    axis.IsZoomEnabled = _yAxesEnableZoom.Value;
            };

            return menu;
        }

        private PlotModel CreatePlotModel()
        {
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

                axis.Zoom(_settings.GetValue("plotPanel.Min" + axis.Key, float.NaN), _settings.GetValue("plotPanel.Max" + axis.Key, float.NaN));

                if (Units.ContainsKey(type))
                    axis.Unit = Units[type];

                _axes.Add(type, axis);
                _annotations.Add(type, annotation);
            }

            var model = new ScaledPlotModel(_dpiXScale, _dpiYScale);
            model.Axes.Add(_timeAxis);

            foreach (LinearAxis axis in _axes.Values)
                model.Axes.Add(axis);

            model.IsLegendVisible = false;

            return model;
        }

        private void SetDpi()
        {
            // https://msdn.microsoft.com/en-us/library/windows/desktop/dn469266(v=vs.85).aspx
            const int defaultDpi = 96;
            Graphics g = CreateGraphics();

            try
            {
                _dpiX = g.DpiX;
                _dpiY = g.DpiY;
            }
            finally
            {
                g.Dispose();
            }

            if (_dpiX > 0)
                _dpiXScale = _dpiX / defaultDpi;
            if (_dpiY > 0)
                _dpiYScale = _dpiY / defaultDpi;
        }

        public void SetSensors(List<ISensor> sensors, IDictionary<ISensor, Color> colors)
        {
            _model.Series.Clear();

            var types = new HashSet<SensorType>();

            DataPoint CreateDataPoint(SensorType type, SensorValue value)
            {
                float displayedValue;

                if (type == SensorType.Temperature && _unitManager.TemperatureUnit == TemperatureUnit.Fahrenheit)
                {
                    displayedValue = UnitManager.CelsiusToFahrenheit(value.Value);
                }
                else
                {
                    displayedValue = value.Value;
                }

                return new DataPoint((_now - value.Time).TotalSeconds, displayedValue);
            }

            foreach (ISensor sensor in sensors)
            {
                var series = new LineSeries
                {
                    ItemsSource = sensor.Values.Select(value => CreateDataPoint(sensor.SensorType, value)),
                    Color = colors[sensor].ToOxyColor(),
                    StrokeThickness = 1,
                    YAxisKey = _axes[sensor.SensorType].Key,
                    Title = sensor.Hardware.Name + " " + sensor.Name
                };

                string typeName = sensor.SensorType.ToString();

                series.TrackerFormatString = "{0}\nTime: {2:hh\\:mm\\:ss\\.fff}\n" + typeName + ": {4:.##}";

                if (Units.ContainsKey(sensor.SensorType))
                    series.TrackerFormatString += " " + Units[sensor.SensorType];

                _model.Series.Add(series);

                types.Add(sensor.SensorType);
            }

            foreach (KeyValuePair<SensorType, LinearAxis> pair in _axes.Reverse())
            {
                LinearAxis axis = pair.Value;
                SensorType type = pair.Key;
                axis.IsAxisVisible = types.Contains(type);
            }

            UpdateAxesPosition();
            InvalidatePlot();
        }

        private void UpdateAxesPosition()
        {
            if (_stackedAxes.Value)
            {
                int count = _axes.Values.Count(axis => axis.IsAxisVisible);
                double start = 0.0;

                foreach (KeyValuePair<SensorType, LinearAxis> pair in _axes.Reverse())
                {
                    LinearAxis axis = pair.Value;
                    axis.StartPosition = start;
                    double delta = axis.IsAxisVisible ? 1.0 / count : 0;
                    start += delta;
                    axis.EndPosition = start;
                    axis.PositionTier = 0;
                    axis.MajorGridlineStyle = LineStyle.Solid;
                    axis.MinorGridlineStyle = LineStyle.Solid;

                    LineAnnotation annotation = _annotations[pair.Key];
                    annotation.Y = axis.ActualMinimum;

                    if (!_model.Annotations.Contains(annotation)) 
                        _model.Annotations.Add(annotation);
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

                    if (_model.Annotations.Contains(annotation)) 
                        _model.Annotations.Remove(_annotations[pair.Key]);
                }
            }
        }

        public void InvalidatePlot()
        {
            _now = DateTime.UtcNow;

            if (_axes != null)
            {
                foreach (KeyValuePair<SensorType, LinearAxis> pair in _axes)
                {
                    LinearAxis axis = pair.Value;
                    SensorType type = pair.Key;

                    if (_showAxesLabels.Value && type == SensorType.Temperature)
                        axis.Unit = _unitManager.TemperatureUnit == TemperatureUnit.Celsius ? "°C" : "°F";
                    
                    if (!_stackedAxes.Value) 
                        continue;

                    var annotation = _annotations[pair.Key];
                    annotation.Y = axis.ActualMaximum;
                }
            }

            _plot?.InvalidatePlot(true);
        }

        public void TimeAxisZoom(double min, double max)
        {
            bool axisIsZoomEnabled = _timeAxis.IsZoomEnabled;

            _timeAxis.IsZoomEnabled = true;
            _timeAxis.Zoom(min, max);

            InvalidatePlot();

            _timeAxis.IsZoomEnabled = axisIsZoomEnabled;
        }

        public void AutoScaleAllYAxes()
        {
            bool axisIsZoomEnabled = _yAxesEnableZoom.Value;

            foreach (LinearAxis axis in _axes.Values)
            {
                axis.IsZoomEnabled = true;
                axis.Zoom(double.NaN, double.NaN);
                axis.IsZoomEnabled = axisIsZoomEnabled;
            }

            InvalidatePlot();
        }
    }
}
