// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// Partial Copyright (C) Michael Möller <mmoeller@openhardwaremonitor.org> and Contributors.
// All Rights Reserved.

using System;
using System.Drawing;
using LibreHardwareMonitor.Hardware;
using LibreHardwareMonitor.Utilities;

namespace LibreHardwareMonitor.UI
{
    public class SensorNode : Node
    {
        private readonly PersistentSettings _settings;
        private readonly UnitManager _unitManager;

        private bool _plot;
        private Color? _penColor;

        public SensorNode(ISensor sensor, PersistentSettings settings, UnitManager unitManager)
        {
            Sensor = sensor;

            _settings = settings;
            _unitManager = unitManager;

            bool hidden = settings.GetValue(new Identifier(sensor.Identifier, "hidden").ToString(), sensor.IsDefaultHidden);
            base.IsVisible = !hidden;

            Plot = settings.GetValue(new Identifier(sensor.Identifier, "plot").ToString(), false);
            string id = new Identifier(sensor.Identifier, "penColor").ToString();

            if (settings.Contains(id))
                PenColor = settings.GetValue(id, Color.Black);
        }

        public override string Text
        {
            get { return Sensor.Name; }
            set { Sensor.Name = value; }
        }

        public override bool IsVisible
        {
            get { return base.IsVisible; }
            set
            {
                base.IsVisible = value;
                _settings.SetValue(new Identifier(Sensor.Identifier, "hidden").ToString(), !value);
            }
        }

        public Color? PenColor
        {
            get { return _penColor; }
            set
            {
                _penColor = value;

                string id = new Identifier(Sensor.Identifier, "penColor").ToString();
                if (value.HasValue)
                    _settings.SetValue(id, value.Value);
                else
                    _settings.Remove(id);

                PlotSelectionChanged?.Invoke(this, null);
            }
        }

        public bool Plot
        {
            get { return _plot; }
            set
            {
                _plot = value;
                _settings.SetValue(new Identifier(Sensor.Identifier, "plot").ToString(), value);
                PlotSelectionChanged?.Invoke(this, null);
            }
        }

        public event EventHandler PlotSelectionChanged;

        public ISensor Sensor { get; }

        public string Value
        {
            get { return ValueToString(Sensor.Value); }
        }

        public string Min
        {
            get { return ValueToString(Sensor.Min); }
        }

        public string Max
        {
            get { return ValueToString(Sensor.Max); }
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;

            if (!(obj is SensorNode s))
                return false;

            return (Sensor == s.Sensor);
        }

        public override int GetHashCode()
        {
            return Sensor.GetHashCode();
        }

        private string ValueToString(float? value)
        {
            if (!value.HasValue)
            {
                return "-";
            }

            string format = _unitManager.GetFormatWithUnit(Sensor.SensorType, value);

            if (Sensor.SensorType == SensorType.Temperature)
            {
                value = _unitManager.LocalizeTemperature(value);
            }
            else if (Sensor.SensorType == SensorType.Throughput)
            {
                value = _unitManager.ScaleThroughput(value);
            }

            string formatted = string.Format(format, value);
            return formatted;
        }
    }
}
