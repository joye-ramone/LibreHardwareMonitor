using System;
using System.Collections.Generic;
using System.Linq;
using LibreHardwareMonitor.Hardware;
using LibreHardwareMonitor.Utilities;

namespace LibreHardwareMonitor.UI
{
    public class ComputerNode : Node
    {
        private readonly PersistentSettings _settings;

        public ComputerNode(PersistentSettings settings) : base(Environment.MachineName)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));

            Image = EmbeddedResources.GetImage("computer.png");
        }

        public override void AddChild(Node node)
        {
            if (node is HardwareNode hardwareNode)
            {
                var identifier = new Identifier(hardwareNode.Hardware.Identifier, "sortIndex").ToString();

                int storedIndex = _settings.GetValue(identifier, Int32.MaxValue);
                if (storedIndex == Int32.MaxValue)
                {
                    if (InternalNodes.Count > 0)
                    {
                        storedIndex = Hardware().Max(n => n.SortIndex) + 1;
                    }
                    else storedIndex = 0;
                }

                hardwareNode.SortIndex = storedIndex;

                InsertAtSortOrder(hardwareNode);
            }
            else
                base.AddChild(node);
        }

        public void SaveSortOrder()
        {
            // reorder and save it

            int index = 0;

            foreach (var node in Hardware().OrderBy(i => i.SortIndex))
            {
                var identifier = new Identifier(node.Hardware.Identifier, "sortIndex").ToString();

                _settings.SetValue(identifier, index++);
            }
        }

        private IEnumerable<HardwareNode> Hardware()
        {
            return Nodes.OfType<HardwareNode>();
        }

        public bool Move(HardwareNode targetNode, bool dir)
        {
            IEnumerable<HardwareNode> nodes = Hardware().OrderBy(i => i.SortIndex);

            if (dir)
            {
                nodes = nodes.Reverse();
            }

            HardwareNode sourceNode = null;

            foreach (HardwareNode node in nodes)
            {
                if (node == targetNode)
                {
                    break;
                }

                sourceNode = node;
            }

            if (sourceNode != null)
            {
                int index = sourceNode.SortIndex;
                sourceNode.SortIndex = targetNode.SortIndex;
                targetNode.SortIndex = index;

                RemoveChild(targetNode);
                InsertAtSortOrder(targetNode);

                return true;
            }

            return false;
        }

        private void InsertAtSortOrder(HardwareNode hardwareNode)
        {
            int i = 0;
            while (i < InternalNodes.Count && ((HardwareNode)InternalNodes[i]).SortIndex < hardwareNode.SortIndex)
                i++;

            InternalNodes.Insert(i, hardwareNode);
        }
    }
}
