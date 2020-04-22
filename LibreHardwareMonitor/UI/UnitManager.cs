// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// Partial Copyright (C) Michael Möller <mmoeller@openhardwaremonitor.org> and Contributors.
// All Rights Reserved.

using System.Collections.Generic;
using System.Runtime.InteropServices.WindowsRuntime;
using LibreHardwareMonitor.Hardware;
using LibreHardwareMonitor.Utilities;

namespace LibreHardwareMonitor.UI
{
    public enum TemperatureUnit
    {
        Celsius = 0,
        Fahrenheit = 1
    }

    public class UnitManager
    {
        private static readonly Dictionary<SensorType, string> _units = new Dictionary<SensorType, string>
        {
            {SensorType.Voltage, "V"},
            {SensorType.Clock, "MHz"},
            {SensorType.Temperature, "°C"}, // should be set on refresh depending on current settings
            {SensorType.Load, "%"},
            {SensorType.Frequency, "Hz"},
            {SensorType.Fan, "RPM"},
            {SensorType.Flow, "L/h"},
            {SensorType.Control, "%"},
            {SensorType.Level, "%"},
            {SensorType.Factor, ""},
            {SensorType.Power, "W"},
            {SensorType.Data, "GB"},
            {SensorType.SmallData, "MB"},
            {SensorType.Throughput, "KB/s"}, // should be set depending on scale of value
        };

        private readonly PersistentSettings _settings;

        private TemperatureUnit _temperatureUnit;

        public UnitManager(PersistentSettings settings)
        {
            _settings = settings;

            _temperatureUnit = (TemperatureUnit)settings.GetValue("TemperatureUnit", (int)TemperatureUnit.Celsius);
        }

        public TemperatureUnit TemperatureUnit
        {
            get { return _temperatureUnit; }
            set
            {
                _temperatureUnit = value;
                _settings.SetValue("TemperatureUnit", (int)_temperatureUnit);
            }
        }

        private string TemperatureUnitString
        {
            get => TemperatureUnit == TemperatureUnit.Celsius ? "°C" : "°F";
        }

        public float? LocalizeTemperature(float? valueInCelsius)
        {
            if (!valueInCelsius.HasValue)
            {
                return null;
            }

            return LocalizeTemperature(valueInCelsius.Value);
        }

        public float LocalizeTemperature(float valueInCelsius)
        {
            if (_temperatureUnit == TemperatureUnit.Celsius)
            {
                return valueInCelsius;
            }

            return valueInCelsius * 1.8f + 32;
        }

        public float ScaleThroughput(float throughput)
        {
            float result;

            //switch (sensor.Name)
            {
                //case "Connection Speed": ???
                //    switch (value)
                //    {
                //        case 100 * 1000 * 1000:
                //            result = "100 Mbps";
                //            break;
                //        case 1000 * 1000 * 1000:
                //            result = "1 Gbps";
                //            break;
                //        default:
                //            if (value < 1024)
                //                result = $"{value:F0} bps";
                //            else if (value < 1024 * 1024)
                //                result = $"{value / 1024:F1} Kbps";
                //            else if (value < 1024 * 1024 * 1024)
                //                result = $"{value / 1024 * 1024:F1} Mbps";
                //            else
                //                result = $"{value / 1024 * 1024 * 1024:F1} Gbps";
                //            break;
                //    }
                //    break;
                //default:
                    result = throughput < 1024 * 1024
                        ? throughput / 1024
                        : throughput / (1024 * 1024);
            }
            return result;
        }

        public float? ScaleThroughput(float? throughput)
        {
            if (!throughput.HasValue)
            {
                return null;
            }

            return ScaleThroughput(throughput.Value);
        }

        public string GetUnit(SensorType sensorType, float? value = null)
        {
            if (sensorType == SensorType.Temperature)
            {
                return TemperatureUnitString;
            }

            if (sensorType == SensorType.Throughput && value.HasValue)
            {
                //switch (sensor.Name)
                {
                    //case "Connection Speed": ???
                    //    switch (sensor.Value)
                    //    {
                    //        case 100 * 1000 * 1000:
                    //            result = "100 Mbps";
                    //            break;
                    //        case 1000 * 1000 * 1000:
                    //            result = "1 Gbps";
                    //            break;
                    //        default:
                    //            if (sensor.Value < 1024)
                    //                result = $"{sensor.Value:F0} bps";
                    //            else if (sensor.Value < 1024 * 1024)
                    //                result = $"{sensor.Value / 1024:F1} Kbps";
                    //            else if (sensor.Value < 1024 * 1024 * 1024)
                    //                result = $"{sensor.Value / 1024 * 1024:F1} Mbps";
                    //            else
                    //                result = $"{sensor.Value / 1024 * 1024 * 1024:F1} Gbps";
                    //            break;
                    //    }
                    //    break;
                    //default:
                    return value.Value < 1024 * 1024 
                        ? "KB/s" 
                        : "MB/s";
                }
            }

            if (!_units.TryGetValue(sensorType, out string unit))
            {
                return string.Empty;
            }

            return unit;
        }

        public string GetFormat(SensorType sensorType, int index = 0)
        {
            switch (sensorType)
            {
                case SensorType.Fan:
                    return "{" + index + ":F0}";
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
                    return "{" + index + ":F1}";
                case SensorType.Voltage:
                case SensorType.Factor:
                    return "{" + index + ":F3}";
            }

            return "{" + index + ":F1}";
        }

        public string GetFormatWithUnit(SensorType sensorType, float? value = null, int index = 0)
        {
            return $"{GetFormat(sensorType)} {GetUnit(sensorType, value)}";
        }
    }
}
