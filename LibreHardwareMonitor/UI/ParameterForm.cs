﻿// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// Partial Copyright (C) Michael Möller <mmoeller@openhardwaremonitor.org> and Contributors.
// All Rights Reserved.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Forms;
using LibreHardwareMonitor.Hardware;

namespace LibreHardwareMonitor.UI
{
    public partial class ParameterForm : Form
    {
        private class ParameterRow : INotifyPropertyChanged
        {
            public readonly IParameter Parameter;

            private float _value = float.NaN;
            private bool _default = true;

            public event PropertyChangedEventHandler PropertyChanged;

            private void NotifyPropertyChanged(string propertyName)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }

            public ParameterRow(IParameter parameter)
            {
                Parameter = parameter;
                Value = parameter.Value;
                Default = parameter.IsDefault;
            }

            public string Name
            {
                get { return Parameter.Name; }
            }

            public string Description
            {
                get { return Parameter.Description; }
            }

            public float Value
            {
                get { return _value; }
                set
                {
                    _default = false;
                    _value = value;
                    NotifyPropertyChanged(nameof(Default));
                    NotifyPropertyChanged(nameof(Value));
                }
            }

            public bool Default
            {
                get { return _default; }
                set
                {
                    _default = value;
                    if (_default)
                        _value = Parameter.DefaultValue;
                    NotifyPropertyChanged(nameof(Default));
                    NotifyPropertyChanged(nameof(Value));
                }
            }
        }

        private BindingList<ParameterRow> _parameterRows;

        public string Caption
        {
            get { return captionLabel.Text; }
            set { captionLabel.Text = value; }
        }

        public ParameterForm()
        {
            InitializeComponent();

            CancelButton = cancelButton;
        }

        public void SetParameters(IReadOnlyList<IParameter> value)
        {
            _parameterRows = new BindingList<ParameterRow>();

            foreach (IParameter parameter in value)
            {
                _parameterRows.Add(new ParameterRow(parameter));
            }

            bindingSource.DataSource = _parameterRows;
        }

        private void DataGridView_RowEnter(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0 && e.RowIndex < _parameterRows.Count)
                descriptionLabel.Text = _parameterRows[e.RowIndex].Description;
            else
                descriptionLabel.Text = string.Empty;
        }

        private void DataGridView_CellValidating(object sender, DataGridViewCellValidatingEventArgs e)
        {
            if (e.ColumnIndex == 2 && !float.TryParse(e.FormattedValue.ToString(), out float _))
            {
                dataGridView.Rows[e.RowIndex].Cells[0].ErrorText = "Invalid value";
                e.Cancel = true;
            }
        }

        private void DataGridView_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            dataGridView.Rows[e.RowIndex].Cells[0].ErrorText = string.Empty;
        }

        private void OkButton_Click(object sender, EventArgs e)
        {
            foreach (ParameterRow row in _parameterRows)
            {
                if (row.Default)
                    row.Parameter.IsDefault = true;
                else
                    row.Parameter.Value = row.Value;
            }
        }

        private void DataGridView_CurrentCellDirtyStateChanged(object sender, EventArgs e)
        {
            if (dataGridView.CurrentCell is DataGridViewCheckBoxCell || dataGridView.CurrentCell is DataGridViewComboBoxCell)
                dataGridView.CommitEdit(DataGridViewDataErrorContexts.Commit);
        }
    }
}
