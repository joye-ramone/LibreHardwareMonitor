// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// Partial Copyright (C) Michael Möller <mmoeller@openhardwaremonitor.org> and Contributors.
// All Rights Reserved.

using LibreHardwareMonitor.Hardware;
using LibreHardwareMonitor.Utilities;

namespace LibreHardwareMonitor.UI
{
    public sealed class TypeNode : Node, IExpandPersistNode
    {
        private readonly PersistentSettings _settings;
        private readonly string _expandedIdentifier;
        private bool _expanded;

        public TypeNode(SensorType sensorType, Identifier parentId, PersistentSettings settings)
        {
            SensorType = sensorType;

            _expandedIdentifier = new Identifier(parentId, SensorType.ToString(), ".expanded").ToString();
            _settings = settings;

            Image = SensorTypeInfo.Instance.GetImage(sensorType);
            Text = SensorTypeInfo.Instance.GetName(sensorType);

            _expanded = settings.GetValue(_expandedIdentifier, true);
        }

        protected override void NodeRemoved(Node node)
        {
            node.IsVisibleChanged -= ChildIsVisibleChanged;
            ChildIsVisibleChanged(null);
        }

        protected override void NodeAdded(Node node)
        {
            node.IsVisibleChanged += ChildIsVisibleChanged;
            ChildIsVisibleChanged(null);
        }

        private void ChildIsVisibleChanged(Node node)
        {
            foreach (Node n in Nodes)
            {
                if (n.IsVisible)
                {
                    IsVisible = true;
                    return;
                }
            }
            IsVisible = false;
        }

        public SensorType SensorType { get; }

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

        public override void AddChild(Node node)
        {
            if (node is SensorNode sensorNode)
            {
                int i = 0;
                while (i < InternalNodes.Count && ((SensorNode)InternalNodes[i]).Sensor.Index < sensorNode.Sensor.Index)
                    i++;

                InternalNodes.Insert(i, sensorNode);
            }
            else
                base.AddChild(node);
        }
    }
}
