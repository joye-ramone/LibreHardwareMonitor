using System.Collections.Generic;
using System.Drawing;
using LibreHardwareMonitor.Hardware;

namespace LibreHardwareMonitor.UI
{
    public class SensorTypeInfo
    {
        public static SensorTypeInfo Instance { get; } = new SensorTypeInfo();

        private readonly IDictionary<SensorType, Image> _images = new Dictionary<SensorType, Image>();

        private SensorTypeInfo()
        {
            
        }

        public Image GetImage(SensorType sensorType)
        {
            if (_images.TryGetValue(sensorType, out Image image))
                return image;

            switch (sensorType)
            {
                case SensorType.Voltage:
                    image = Utilities.EmbeddedResources.GetImage("voltage.png");
                    break;
                case SensorType.Clock:
                    image = Utilities.EmbeddedResources.GetImage("clock.png");
                    break;
                case SensorType.Load:
                    image = Utilities.EmbeddedResources.GetImage("load.png");
                    break;
                case SensorType.Temperature:
                    image = Utilities.EmbeddedResources.GetImage("temperature.png");
                    break;
                case SensorType.Fan:
                    image = Utilities.EmbeddedResources.GetImage("fan.png");
                    break;
                case SensorType.Flow:
                    image = Utilities.EmbeddedResources.GetImage("flow.png");
                    break;
                case SensorType.Control:
                    image = Utilities.EmbeddedResources.GetImage("control.png");
                    break;
                case SensorType.Level:
                    image = Utilities.EmbeddedResources.GetImage("level.png");
                    break;
                case SensorType.Power:
                    image = Utilities.EmbeddedResources.GetImage("power.png");
                    break;
                case SensorType.Data:
                    image = Utilities.EmbeddedResources.GetImage("data.png");
                    break;
                case SensorType.SmallData:
                    image = Utilities.EmbeddedResources.GetImage("data.png");
                    break;
                case SensorType.Factor:
                    image = Utilities.EmbeddedResources.GetImage("factor.png");
                    break;
                case SensorType.Frequency:
                    image = Utilities.EmbeddedResources.GetImage("clock.png");
                    break;
                case SensorType.Throughput:
                    image = Utilities.EmbeddedResources.GetImage("throughput.png");
                    break;
            }

            _images.Add(sensorType, image);

            return image;
        }

        public string GetName(SensorType sensorType)
        {
            switch (sensorType)
            {
                case SensorType.Voltage:
                    return "Voltages";
                case SensorType.Clock:
                    return "Clocks";
                case SensorType.Load:
                    return "Load";
                case SensorType.Temperature:
                    return "Temperatures";
                case SensorType.Fan:
                    return "Fans";
                case SensorType.Flow:
                    return "Flows";
                case SensorType.Control:
                    return "Controls";
                case SensorType.Level:
                    return "Levels";
                case SensorType.Power:
                    return "Powers";
                case SensorType.Data:
                    return "Data";
                case SensorType.SmallData:
                    return "Data";
                case SensorType.Factor:
                    return "Factors";
                case SensorType.Frequency:
                    return "Frequencies";
                case SensorType.Throughput:
                    return "Throughput";
            }

            return string.Empty;
        }
    }
}
