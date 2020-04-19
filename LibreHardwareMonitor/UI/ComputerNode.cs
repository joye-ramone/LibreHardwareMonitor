using System;
using System.Collections.Generic;
using System.Linq;
using Aga.Controls.Tree.NodeControls;
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

        protected override void NodeAdded(Node node)
        {
            if (node is HardwareNode hardwareNode)
            {
                var identifier = new Identifier(hardwareNode.Hardware.Identifier, "sortIndex").ToString();

                int storedIndex = _settings.GetValue(identifier, Int32.MaxValue);
                if (storedIndex == Int32.MaxValue)
                {
                    if (Nodes.Count > 1)
                    {
                        storedIndex = Hardware().Where(i => i != hardwareNode).Max(i => i.SortIndex) + 1;
                    }
                    else storedIndex = 0;
                }

                hardwareNode.SortIndex = storedIndex;
            }
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

        public bool Move(HardwareNode hardwareNode, int dir)
        {
            HardwareNode prevNode = null;

            IEnumerable<HardwareNode> nodes = Hardware().OrderBy(i => i.SortIndex);

            if (dir > 0)
            {
                nodes = nodes.Reverse();
            }

            foreach (var node in nodes)
            {
                if (node == hardwareNode)
                {
                    if (prevNode != null)
                    {
                        int a = prevNode.SortIndex;
                        prevNode.SortIndex = node.SortIndex;
                        node.SortIndex = a;

                        RefreshTree();

                        return true;
                    }
                    break;
                }

                prevNode = node;
            }

            return false;
        }

        private void RefreshTree()
        {
            TreeModel model = RootTreeModel();
            model?.OnStructureChanged(this);
        }
    }
}
