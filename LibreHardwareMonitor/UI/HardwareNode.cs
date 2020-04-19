// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// Partial Copyright (C) Michael Möller <mmoeller@openhardwaremonitor.org> and Contributors.
// All Rights Reserved.

using LibreHardwareMonitor.Hardware;
using LibreHardwareMonitor.Utilities;
using System;
using System.Collections.Generic;

namespace LibreHardwareMonitor.UI
{
    public class HardwareNode : Node, IExpandPersistNode
    {
        private readonly PersistentSettings _settings;
        private readonly UnitManager _unitManager;
        private readonly List<TypeNode> _typeNodes = new List<TypeNode>();

        private readonly string _expandedIdentifier;
        private readonly string _hiddenIdentifier;

        private bool _expanded;

        public event EventHandler PlotSelectionChanged;

        public HardwareNode(IHardware hardware, PersistentSettings settings, UnitManager unitManager)
        {
            _settings = settings;
            _unitManager = unitManager;

            Hardware = hardware;

            Image = HardwareTypeImage.Instance.GetImage(hardware.HardwareType);

            _hiddenIdentifier = new Identifier(hardware.Identifier, "hidden").ToString();
            bool hidden = settings.GetValue(_hiddenIdentifier, false);
            base.IsVisible = !hidden;

            foreach (SensorType sensorType in Enum.GetValues(typeof(SensorType)))
                _typeNodes.Add(new TypeNode(sensorType, hardware.Identifier, _settings));

            foreach (ISensor sensor in hardware.Sensors)
                SensorAdded(sensor);

            hardware.SensorAdded += SensorAdded;
            hardware.SensorRemoved += SensorRemoved;

            _expandedIdentifier = new Identifier(hardware.Identifier, "expanded").ToString();
            _expanded = settings.GetValue(_expandedIdentifier, true);
        }

        public int SortIndex { get; set; }

        public override string Text
        {
            get { return Hardware.Name; }
            set { Hardware.Name = value; }
        }

        public override bool IsVisible
        {
            get { return base.IsVisible; }
            set
            {
                base.IsVisible = value;
                _settings.SetValue(_hiddenIdentifier, !value);
            }
        }

        public IHardware Hardware { get; }

        public bool Expanded
        {
            get => _expanded;
            set
            {
                _expanded = value;
                if (!_expanded)
                {
                    _settings.SetValue(_expandedIdentifier, _expanded);
                }
                else
                {
                    _settings.Remove(_expandedIdentifier);
                }
            }
        }

        private void UpdateTypeNode(TypeNode node)
        {
            if (node.Count > 0)
            {
                if (!InternalNodes.Contains(node))
                {
                    AddChild(node);
                }
            }
            else
            {
                if (InternalNodes.Contains(node))
                {
                    RemoveChild(node);
                }
            }
        }

        public override void AddChild(Node node)
        {
            if (node is TypeNode typeNode)
            {
                int i = 0;
                while (i < InternalNodes.Count && ((TypeNode)InternalNodes[i]).SensorType < typeNode.SensorType)
                    i++;

                InternalNodes.Insert(i, node);
            }
            else
                base.AddChild(node);
        }

        private void SensorAdded(ISensor sensor)
        {
            foreach (TypeNode typeNode in _typeNodes)
            {
                if (typeNode.SensorType == sensor.SensorType)
                {
                    SensorNode sensorNode = new SensorNode(sensor, _settings, _unitManager);

                    sensorNode.PlotSelectionChanged += SensorPlotSelectionChanged;
                    typeNode.AddChild(sensorNode);

                    UpdateTypeNode(typeNode);
                }
            }

            PlotSelectionChanged?.Invoke(this, null);
        }

        private void SensorRemoved(ISensor sensor)
        {
            foreach (TypeNode typeNode in _typeNodes)
            {
                if (typeNode.SensorType == sensor.SensorType)
                {
                    SensorNode sensorNode = null;

                    foreach (Node node in typeNode.Nodes)
                    {
                        if (node is SensorNode n && n.Sensor == sensor)
                        {
                            sensorNode = n;
                            break;
                        }
                    }

                    if (sensorNode != null)
                    {
                        sensorNode.PlotSelectionChanged -= SensorPlotSelectionChanged;
                        typeNode.RemoveChild(sensorNode);

                        UpdateTypeNode(typeNode);
                    }

                    break;
                }
            }

            PlotSelectionChanged?.Invoke(this, null);
        }

        private void SensorPlotSelectionChanged(object sender, EventArgs e)
        {
            PlotSelectionChanged?.Invoke(this, null);
        }
    }
}
