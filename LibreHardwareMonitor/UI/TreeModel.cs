﻿// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// Partial Copyright (C) Michael Möller <mmoeller@openhardwaremonitor.org> and Contributors.
// All Rights Reserved.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Aga.Controls.Tree;

namespace LibreHardwareMonitor.UI
{
    public class TreeModel : ITreeModel
    {
        private readonly Node _root;
        private bool _forceVisible;

        public TreeModel(Node visibleRoot)
        {
            _root = new Node("ModelRoot") { Model = this };
            _root.AddChild(visibleRoot);
        }

        public TreePath GetPath(Node node)
        {
            if (node == _root)
                return TreePath.Empty;

            Stack<object> stack = new Stack<object>();
            while (node != _root)
            {
                stack.Push(node);
                node = node.Parent;
            }
            return new TreePath(stack.ToArray());
        }

        private Node GetNode(TreePath treePath)
        {
            Node parent = _root;
            foreach (object obj in treePath.FullPath)
            {
                if (!(obj is Node node) || node.Parent != parent)
                    return null;

                parent = node;
            }
            return parent;
        }

        public IEnumerable GetChildren(TreePath treePath)
        {
            Node node = GetNode(treePath);
            if (node != null)
            {
                var nodesToReturn = node
                    .Nodes
                    .Where(n => _forceVisible || n.IsVisible);

                foreach (Node n in nodesToReturn)
                {
                    yield return n;
                }
            }
        }

        public bool IsLeaf(TreePath treePath)
        {
            return false;
        }

        public bool ForceVisible
        {
            get
            {
                return _forceVisible;
            }
            set
            {
                if (value != _forceVisible)
                {
                    _forceVisible = value;
                    OnStructureChanged(_root);
                }
            }
        }

#pragma warning disable 67
        public event EventHandler<TreePathEventArgs> StructureChanged;

        public event EventHandler<TreeModelEventArgs> NodesChanged;
        public event EventHandler<TreeModelEventArgs> NodesInserted;
        public event EventHandler<TreeModelEventArgs> NodesRemoved;
#pragma warning restore 67

        public void OnStructureChanged(Node node)
        {
            StructureChanged?.Invoke(this, new TreeModelEventArgs(GetPath(node), new object[0]));
        }

        public void OnNodeChanged(Node parent, int index, Node node)
        {
            if (parent != null)
            {
                TreePath path = GetPath(parent);
                if (path != null)
                    NodesChanged?.Invoke(this, new TreeModelEventArgs(path, new[] { index }, new object[] { node }));
            }
        }

        public void OnNodeInserted(Node parent, int index, Node node)
        {
            TreeModelEventArgs args = new TreeModelEventArgs(GetPath(parent), new[] { index }, new object[] { node });
            NodesInserted?.Invoke(this, args);
        }

        public void OnNodeRemoved(Node parent, int index, Node node)
        {
            TreeModelEventArgs args = new TreeModelEventArgs(GetPath(parent), new[] { index }, new object[] { node });
            NodesRemoved?.Invoke(this, args);
        }

    }
}
