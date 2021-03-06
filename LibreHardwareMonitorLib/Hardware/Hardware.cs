﻿// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// Partial Copyright (C) Michael Möller <mmoeller@openhardwaremonitor.org> and Contributors.
// All Rights Reserved.

using System;
using System.Collections.Generic;
using System.Linq;

namespace LibreHardwareMonitor.Hardware
{
    public abstract class Hardware : IHardware
    {
        protected readonly HashSet<ISensor> _active = new HashSet<ISensor>();

        protected readonly string _name;
        protected readonly ISettings _settings;

        private string _customName;

        protected Hardware(string name, Identifier identifier, ISettings settings)
        {
            _settings = settings;
            _name = name;
            Identifier = identifier;
            _customName = settings.GetValue(new Identifier(Identifier, "name").ToString(), name);
        }

        public abstract HardwareType HardwareType { get; }

        public Identifier Identifier { get; }

        public string Name
        {
            get { return _customName; }
            set
            {
                _customName = !string.IsNullOrEmpty(value) ? value : _name;

                _settings.SetValue(new Identifier(Identifier, "name").ToString(), _customName);
            }
        }

        public virtual IHardware Parent
        {
            get { return null; }
        }

        public virtual ISensor[] Sensors
        {
            get { return _active.ToArray(); }
        }

        public IHardware[] SubHardware
        {
            get { return new IHardware[0]; }
        }

        public virtual string GetReport()
        {
            return null;
        }

        public abstract void Update();

        public void Accept(IVisitor visitor)
        {
            if (visitor == null) throw new ArgumentNullException(nameof(visitor));

            visitor.VisitHardware(this);
        }

        public virtual void Traverse(IVisitor visitor)
        {
            if (visitor == null) throw new ArgumentNullException(nameof(visitor));

            foreach (ISensor sensor in _active)
            {
                sensor.Accept(visitor);
            }
        }

        public event SensorEventHandler SensorAdded;

        protected virtual void ActivateSensor(ISensor sensor)
        {
            if (_active.Add(sensor))
                SensorAdded?.Invoke(sensor);
        }

        public event SensorEventHandler SensorRemoved;

        protected virtual void DeactivateSensor(ISensor sensor)
        {
            if (_active.Remove(sensor))
                SensorRemoved?.Invoke(sensor);
        }

        public virtual void Close()
        {
            foreach (ISensor sensor in _active)
            {
                if (sensor is Sensor own)
                {
                    own.SetSensorValuesToSettings();
                }
            }
        }
    }
}
