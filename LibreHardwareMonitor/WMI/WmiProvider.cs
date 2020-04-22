// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// Partial Copyright (C) Michael Möller <mmoeller@openhardwaremonitor.org> and Contributors.
// All Rights Reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Instrumentation;
using LibreHardwareMonitor.Hardware;

[assembly: Instrumented("root/LibreHardwareMonitor")]

[System.ComponentModel.RunInstaller(true)]
public class InstanceInstaller : DefaultManagementProjectInstaller { }

namespace LibreHardwareMonitor.Wmi
{
    /// <summary>
    /// The WMI Provider.
    /// This class is not exposed to WMI itself.
    /// </summary>
    public class WmiProvider : IDisposable
    {
        private readonly Dictionary<string, IWmiObject> _activeInstances = new Dictionary<string, IWmiObject>();

        private readonly IComputer _computer;

        public WmiProvider(IComputer computer)
        {
            _computer = computer ?? throw new ArgumentNullException(nameof(computer));
        }

        public void Start()
        {
            foreach (IHardware hardware in _computer.Hardware)
            {
                ComputerHardwareAdded(hardware);
            }

            _computer.HardwareAdded += ComputerHardwareAdded;
            _computer.HardwareRemoved += ComputerHardwareRemoved;
        }

        public void Stop()
        {
            _computer.HardwareAdded -= ComputerHardwareAdded;
            _computer.HardwareRemoved -= ComputerHardwareRemoved;

            foreach (IHardware hardware in _computer.Hardware)
            {
                ComputerHardwareRemoved(hardware);
            }

            foreach (IWmiObject item in _activeInstances.Values)
            {
                try
                {
                    Revoke(item);
                }
                catch { }
            }

            _activeInstances.Clear();
        }

        public void Update()
        {
            foreach (IWmiObject instance in _activeInstances.Values)
            {
                instance.Update();
            }
        }

        private void ComputerHardwareAdded(IHardware hardware)
        {
            if (!_activeInstances.ContainsKey(hardware.Identifier.ToString()))
            {
                foreach (ISensor sensor in hardware.Sensors)
                    HardwareSensorAdded(sensor);

                hardware.SensorAdded += HardwareSensorAdded;
                hardware.SensorRemoved += HardwareSensorRemoved;

                Hardware hw = new Hardware(hardware);
                _activeInstances[hw.Identifier] = hw;

                try
                {
                    Publish(hw);
                }
                catch { }
            }

            foreach (IHardware subHardware in hardware.SubHardware)
            {
                ComputerHardwareAdded(subHardware);
            }
        }

        private void HardwareSensorAdded(ISensor data)
        {
            Sensor sensor = new Sensor(data);
            _activeInstances[sensor.Identifier] = sensor;

            try
            {
                Publish(sensor);
            }
            catch {  }
        }

        private void ComputerHardwareRemoved(IHardware hardware)
        {
            hardware.SensorAdded -= HardwareSensorAdded;
            hardware.SensorRemoved -= HardwareSensorRemoved;

            foreach (ISensor sensor in hardware.Sensors)
                HardwareSensorRemoved(sensor);

            foreach (IHardware subHardware in hardware.SubHardware)
                ComputerHardwareRemoved(subHardware);

            RevokeInstance(hardware.Identifier.ToString());
        }

        private void HardwareSensorRemoved(ISensor sensor)
        {
            RevokeInstance(sensor.Identifier.ToString());
        }

        private void RevokeInstance(string identifier)
        {
            if (!_activeInstances.TryGetValue(identifier, out IWmiObject item))
            {
                return;
            }

            try
            {
                Revoke(item);
            }
            catch { }

            _activeInstances.Remove(identifier);
        }

        private static void Revoke(IWmiObject item) => Instrumentation.Revoke(item);

        private static void Publish(IWmiObject hw) => Instrumentation.Publish(hw);

        public void Dispose()
        {
            Stop();
        }
    }
}
