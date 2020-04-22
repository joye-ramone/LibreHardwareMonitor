// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
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
using OxyPlot.WindowsForms;

namespace LibreHardwareMonitor.UI
{
    public class PlotPanel : UserControl
    {
        private readonly PersistentSettings _settings;
        private readonly UnitManager _unitManager;
        private readonly PlotView _plot;

        private readonly ScaledPlotModel _model;

        private UserOption _stackedAxes;
        private UserOption _showAxesLabels;
        private UserOption _timeAxisEnableZoom;
        private UserOption _yAxesEnableZoom;

        private DateTime _now;

        private double _dpiXScale = 1;
        private double _dpiYScale = 1;

        private Point _mouseDownLocation;

        public PlotPanel(PersistentSettings settings, UnitManager unitManager)
        {
            _settings = settings;
            _unitManager = unitManager;

            SetDpi();

            _model = CreatePlotModel();

            _plot = new PlotView
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,

                Model = _model,
                Controller =  new SensorPlotController()
            };

            ContextMenu menu = CreateMenu();

            _plot.MouseDown += (sender, e) => { _mouseDownLocation = e.Location; };
            _plot.MouseUp += (sender, e) =>
            {
                if (e.Button == MouseButtons.Right && _mouseDownLocation == e.Location)
                {
                    menu.Show(_plot, _mouseDownLocation);
                }
            };

            SuspendLayout();
            Controls.Add(_plot);
            ResumeLayout(true);
        }

        public void SaveCurrentSettings()
        {
            _model.SaveCurrentSettings();
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
                _model.StackedAxes = _stackedAxes.Value;

                InvalidateData();
            };
            menu.MenuItems.Add(stackedAxesMenuItem);

            _showAxesLabels.Changed += (sender, e) => { _model.ShowValueAxesLabels = _showAxesLabels.Value; };
            menu.MenuItems.Add(showAxesLabelsMenuItem);

            MenuItem timeAxisMenuItem = new MenuItem("Time Axis");

            void UpdateTimeAxisZoom(double min, double max)
            {
                _model.TimeAxisZoom(min, max);

                InvalidateData();
            }

            MenuItem[] timeAxisMenuItems =
            {
                new MenuItem("Enable Zoom"),
                new MenuItem("Auto", 
                    (s, e) => { UpdateTimeAxisZoom(0, double.NaN); }),
                new MenuItem("5 min", 
                    (s, e) => { UpdateTimeAxisZoom(0, 5 * 60); }),
                new MenuItem("10 min", 
                    (s, e) => { UpdateTimeAxisZoom(0, 10 * 60); }),
                new MenuItem("20 min", 
                    (s, e) => { UpdateTimeAxisZoom(0, 20 * 60); }),
                new MenuItem("30 min", 
                    (s, e) => { UpdateTimeAxisZoom(0, 30 * 60); }),
                new MenuItem("45 min", 
                    (s, e) => { UpdateTimeAxisZoom(0, 45 * 60); }),
                new MenuItem("1 h", 
                    (s, e) => { UpdateTimeAxisZoom(0, 60 * 60); }),
                new MenuItem("1.5 h", 
                    (s, e) => { UpdateTimeAxisZoom(0, 1.5 * 60 * 60); }),
                new MenuItem("2 h", 
                    (s, e) => { UpdateTimeAxisZoom(0, 2 * 60 * 60); }),
                new MenuItem("3 h", 
                    (s, e) => { UpdateTimeAxisZoom(0, 3 * 60 * 60); }),
                new MenuItem("6 h", 
                    (s, e) => { UpdateTimeAxisZoom(0, 6 * 60 * 60); }),
                new MenuItem("12 h", 
                    (s, e) => { UpdateTimeAxisZoom(0, 12 * 60 * 60); }),
                new MenuItem("24 h", 
                    (s, e) => { UpdateTimeAxisZoom(0, 24 * 60 * 60); })
            };

            foreach (MenuItem mi in timeAxisMenuItems)
                timeAxisMenuItem.MenuItems.Add(mi);

            menu.MenuItems.Add(timeAxisMenuItem);

            _timeAxisEnableZoom = new UserOption("timeAxisEnableZoom", true, timeAxisMenuItems[0], _settings);
            _timeAxisEnableZoom.Changed += (sender, e) => { _model.EnableTimeAxisZoom = _timeAxisEnableZoom.Value; };

            MenuItem yAxesMenuItem = new MenuItem("Value Axes");
            MenuItem[] yAxesMenuItems =
            {
                new MenuItem("Enable Zoom"),
                new MenuItem("AutoScale All", (s, e) =>
                {
                    _model.AutoScaleAllYAxes();

                    InvalidateData();
                })
            };

            foreach (MenuItem mi in yAxesMenuItems)
                yAxesMenuItem.MenuItems.Add(mi);

            menu.MenuItems.Add(yAxesMenuItem);

            _yAxesEnableZoom = new UserOption("yAxesEnableZoom", true, yAxesMenuItems[0], _settings);
            _yAxesEnableZoom.Changed += (sender, e) => { _model.EnableValueAxesZoom = _yAxesEnableZoom.Value; };

            return menu;
        }

        private ScaledPlotModel CreatePlotModel()
        {
            var model = new ScaledPlotModel(_settings, _unitManager);

            model.ApplyScale(_dpiXScale, _dpiYScale);

            return model;
        }

        private void SetDpi()
        {
            // https://msdn.microsoft.com/en-us/library/windows/desktop/dn469266(v=vs.85).aspx
            const int defaultDpi = 96;

            Graphics g = CreateGraphics();

            try
            {
                double dpiX = g.DpiX;
                double dpiY = g.DpiY;

                if (dpiX > 0)
                    _dpiXScale = dpiX / defaultDpi;
                if (dpiY > 0)
                    _dpiYScale = dpiY / defaultDpi;
            }
            finally
            {
                g.Dispose();
            }
        }

        public void SetSensors(List<ISensor> sensors, IDictionary<ISensor, Color> colors)
        {
            _model.SetSensors(sensors, (sensor, line) =>
            {
                line.ItemsSource = sensor.Values.Select(value => CreateDataPoint(sensor.SensorType, value));
                line.Color = colors[sensor].ToOxyColor();
            });

            InvalidateData();
        }

        private DataPoint CreateDataPoint(SensorType type, SensorValue value)
        {
            float displayedValue;

            if (type == SensorType.Temperature)
            {
                displayedValue = _unitManager.LocalizeTemperature(value.Value);
            }
            else
            {
                displayedValue = value.Value;
            }

            return new DataPoint((_now - value.Time).TotalSeconds, displayedValue);
        }

        public void InvalidateData()
        {
            _now = DateTime.UtcNow;

            _model.UpdateAxes();

            _plot.InvalidatePlot(true);
        }
    }
}
