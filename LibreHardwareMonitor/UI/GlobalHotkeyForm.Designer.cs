namespace LibreHardwareMonitor.UI
{
    partial class GlobalHotkeyForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.btnOk = new System.Windows.Forms.Button();
            this.txtShowHideHotKey = new System.Windows.Forms.TextBox();
            this.txtEnableRtssServiceHotKey = new System.Windows.Forms.TextBox();
            this.chkShowHideHotKey = new System.Windows.Forms.CheckBox();
            this.chkEnableRtssServiceHotKey = new System.Windows.Forms.CheckBox();
            this.btnCancel = new System.Windows.Forms.Button();
            this.btnClearShowHideHotKey = new System.Windows.Forms.Button();
            this.btnClearEnableRtssServiceHotKey = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // btnOk
            // 
            this.btnOk.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnOk.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnOk.Location = new System.Drawing.Point(208, 117);
            this.btnOk.Name = "btnOk";
            this.btnOk.Size = new System.Drawing.Size(75, 23);
            this.btnOk.TabIndex = 4;
            this.btnOk.Text = "OK";
            this.btnOk.UseVisualStyleBackColor = true;
            // 
            // txtShowHideHotKey
            // 
            this.txtShowHideHotKey.BackColor = System.Drawing.SystemColors.Window;
            this.txtShowHideHotKey.Location = new System.Drawing.Point(234, 24);
            this.txtShowHideHotKey.Name = "txtShowHideHotKey";
            this.txtShowHideHotKey.ReadOnly = true;
            this.txtShowHideHotKey.Size = new System.Drawing.Size(100, 20);
            this.txtShowHideHotKey.TabIndex = 6;
            // 
            // txtEnableRtssServiceHotKey
            // 
            this.txtEnableRtssServiceHotKey.BackColor = System.Drawing.SystemColors.Window;
            this.txtEnableRtssServiceHotKey.Location = new System.Drawing.Point(234, 61);
            this.txtEnableRtssServiceHotKey.Name = "txtEnableRtssServiceHotKey";
            this.txtEnableRtssServiceHotKey.ReadOnly = true;
            this.txtEnableRtssServiceHotKey.Size = new System.Drawing.Size(100, 20);
            this.txtEnableRtssServiceHotKey.TabIndex = 7;
            // 
            // chkShowHideHotKey
            // 
            this.chkShowHideHotKey.AutoSize = true;
            this.chkShowHideHotKey.Location = new System.Drawing.Point(21, 26);
            this.chkShowHideHotKey.Name = "chkShowHideHotKey";
            this.chkShowHideHotKey.Size = new System.Drawing.Size(115, 17);
            this.chkShowHideHotKey.TabIndex = 8;
            this.chkShowHideHotKey.Text = "Show/Hide hotkey";
            this.chkShowHideHotKey.UseVisualStyleBackColor = true;
            this.chkShowHideHotKey.CheckedChanged += new System.EventHandler(this.ShowHideHotKeyCheckedChanged);
            // 
            // chkEnableRtssServiceHotKey
            // 
            this.chkEnableRtssServiceHotKey.AutoSize = true;
            this.chkEnableRtssServiceHotKey.Location = new System.Drawing.Point(21, 63);
            this.chkEnableRtssServiceHotKey.Name = "chkEnableRtssServiceHotKey";
            this.chkEnableRtssServiceHotKey.Size = new System.Drawing.Size(113, 17);
            this.chkEnableRtssServiceHotKey.TabIndex = 9;
            this.chkEnableRtssServiceHotKey.Text = "Run RTSS hotkey";
            this.chkEnableRtssServiceHotKey.UseVisualStyleBackColor = true;
            this.chkEnableRtssServiceHotKey.CheckedChanged += new System.EventHandler(this.EnableRtssServiceHotKeyCheckedChanged);
            // 
            // btnCancel
            // 
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.Location = new System.Drawing.Point(289, 117);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(75, 23);
            this.btnCancel.TabIndex = 10;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            // 
            // btnClearShowHideHotKey
            // 
            this.btnClearShowHideHotKey.Location = new System.Drawing.Point(340, 22);
            this.btnClearShowHideHotKey.Name = "btnClearShowHideHotKey";
            this.btnClearShowHideHotKey.Size = new System.Drawing.Size(23, 23);
            this.btnClearShowHideHotKey.TabIndex = 11;
            this.btnClearShowHideHotKey.Text = "X";
            this.btnClearShowHideHotKey.UseVisualStyleBackColor = true;
            this.btnClearShowHideHotKey.Click += new System.EventHandler(this.ClearShowHideHotKeyClick);
            // 
            // btnClearEnableRtssServiceHotKey
            // 
            this.btnClearEnableRtssServiceHotKey.Location = new System.Drawing.Point(340, 59);
            this.btnClearEnableRtssServiceHotKey.Name = "btnClearEnableRtssServiceHotKey";
            this.btnClearEnableRtssServiceHotKey.Size = new System.Drawing.Size(23, 23);
            this.btnClearEnableRtssServiceHotKey.TabIndex = 12;
            this.btnClearEnableRtssServiceHotKey.Text = "X";
            this.btnClearEnableRtssServiceHotKey.UseVisualStyleBackColor = true;
            this.btnClearEnableRtssServiceHotKey.Click += new System.EventHandler(this.ClearEnableRtssServiceHotKeyClick);
            // 
            // GlobalHotkeyForm
            // 
            this.AcceptButton = this.btnOk;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.btnCancel;
            this.ClientSize = new System.Drawing.Size(376, 152);
            this.Controls.Add(this.btnClearEnableRtssServiceHotKey);
            this.Controls.Add(this.btnClearShowHideHotKey);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.chkEnableRtssServiceHotKey);
            this.Controls.Add(this.chkShowHideHotKey);
            this.Controls.Add(this.txtEnableRtssServiceHotKey);
            this.Controls.Add(this.txtShowHideHotKey);
            this.Controls.Add(this.btnOk);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.KeyPreview = true;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "GlobalHotkeyForm";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Global Hotkey options";
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.GlobalHotkeyFormClosed);
            this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.GlobalHotkeyFormKeyDown);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.Button btnOk;
        private System.Windows.Forms.TextBox txtShowHideHotKey;
        private System.Windows.Forms.TextBox txtEnableRtssServiceHotKey;
        private System.Windows.Forms.CheckBox chkShowHideHotKey;
        private System.Windows.Forms.CheckBox chkEnableRtssServiceHotKey;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.Button btnClearShowHideHotKey;
        private System.Windows.Forms.Button btnClearEnableRtssServiceHotKey;
    }
}
