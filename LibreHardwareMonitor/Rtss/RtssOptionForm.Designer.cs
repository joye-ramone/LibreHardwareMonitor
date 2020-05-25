namespace LibreHardwareMonitor.Rtss
{
    partial class RtssOptionForm
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
            this.components = new System.ComponentModel.Container();
            System.Windows.Forms.Label label1;
            System.Windows.Forms.Label label2;
            System.Windows.Forms.Label label3;
            System.Windows.Forms.Label label4;
            System.Windows.Forms.Label label5;
            this.btnCancel = new System.Windows.Forms.Button();
            this.btnOk = new System.Windows.Forms.Button();
            this.statusTimer = new System.Windows.Forms.Timer(this.components);
            this.btnDetectPath = new System.Windows.Forms.Button();
            this.chkAddFpt = new System.Windows.Forms.CheckBox();
            this.txtRtssLocation = new System.Windows.Forms.TextBox();
            this.chkSeparateGroups = new System.Windows.Forms.CheckBox();
            this.chkUseSensorNameAsKey = new System.Windows.Forms.CheckBox();
            this.lblAvailableState = new System.Windows.Forms.Label();
            this.lblRunningState = new System.Windows.Forms.Label();
            this.btnTryRun = new System.Windows.Forms.Button();
            this.radioButton1 = new System.Windows.Forms.RadioButton();
            this.radioButton2 = new System.Windows.Forms.RadioButton();
            this.radioButton3 = new System.Windows.Forms.RadioButton();
            label1 = new System.Windows.Forms.Label();
            label2 = new System.Windows.Forms.Label();
            label3 = new System.Windows.Forms.Label();
            label4 = new System.Windows.Forms.Label();
            label5 = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new System.Drawing.Point(9, 22);
            label1.Name = "label1";
            label1.Size = new System.Drawing.Size(63, 13);
            label1.TabIndex = 5;
            label1.Text = "RTSS path:";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new System.Drawing.Point(9, 52);
            label2.Name = "label2";
            label2.Size = new System.Drawing.Size(84, 13);
            label2.TabIndex = 10;
            label2.Text = "RTSS available:";
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new System.Drawing.Point(9, 82);
            label3.Name = "label3";
            label3.Size = new System.Drawing.Size(77, 13);
            label3.TabIndex = 11;
            label3.Text = "RTSS running:";
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Location = new System.Drawing.Point(203, 111);
            label4.Name = "label4";
            label4.Size = new System.Drawing.Size(92, 13);
            label4.TabIndex = 15;
            label4.Text = "Group sensors by:";
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.Location = new System.Drawing.Point(10, 111);
            label5.Name = "label5";
            label5.Size = new System.Drawing.Size(46, 13);
            label5.TabIndex = 19;
            label5.Text = "Options:";
            // 
            // btnCancel
            // 
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.Location = new System.Drawing.Point(410, 176);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(75, 23);
            this.btnCancel.TabIndex = 3;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            // 
            // btnOk
            // 
            this.btnOk.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnOk.Location = new System.Drawing.Point(329, 176);
            this.btnOk.Name = "btnOk";
            this.btnOk.Size = new System.Drawing.Size(75, 23);
            this.btnOk.TabIndex = 2;
            this.btnOk.Text = "OK";
            this.btnOk.UseVisualStyleBackColor = true;
            // 
            // statusTimer
            // 
            this.statusTimer.Tick += new System.EventHandler(this.StatusTimerTick);
            // 
            // btnDetectPath
            // 
            this.btnDetectPath.Location = new System.Drawing.Point(410, 17);
            this.btnDetectPath.Name = "btnDetectPath";
            this.btnDetectPath.Size = new System.Drawing.Size(75, 23);
            this.btnDetectPath.TabIndex = 4;
            this.btnDetectPath.Text = "Detect";
            this.btnDetectPath.UseVisualStyleBackColor = true;
            this.btnDetectPath.Click += new System.EventHandler(this.DetectPathClick);
            // 
            // chkAddFpt
            // 
            this.chkAddFpt.AutoSize = true;
            this.chkAddFpt.Location = new System.Drawing.Point(12, 132);
            this.chkAddFpt.Name = "chkAddFpt";
            this.chkAddFpt.Size = new System.Drawing.Size(116, 17);
            this.chkAddFpt.TabIndex = 6;
            this.chkAddFpt.Text = "Show in-game FPS";
            this.chkAddFpt.UseVisualStyleBackColor = true;
            // 
            // txtRtssLocation
            // 
            this.txtRtssLocation.Font = new System.Drawing.Font("Courier New", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.txtRtssLocation.Location = new System.Drawing.Point(78, 20);
            this.txtRtssLocation.Name = "txtRtssLocation";
            this.txtRtssLocation.Size = new System.Drawing.Size(326, 21);
            this.txtRtssLocation.TabIndex = 7;
            this.txtRtssLocation.TextChanged += new System.EventHandler(this.LocationTextChanged);
            // 
            // chkSeparateGroups
            // 
            this.chkSeparateGroups.AutoSize = true;
            this.chkSeparateGroups.Location = new System.Drawing.Point(12, 155);
            this.chkSeparateGroups.Name = "chkSeparateGroups";
            this.chkSeparateGroups.Size = new System.Drawing.Size(104, 17);
            this.chkSeparateGroups.TabIndex = 8;
            this.chkSeparateGroups.Text = "Separate groups";
            this.chkSeparateGroups.UseVisualStyleBackColor = true;
            // 
            // chkUseSensorNameAsKey
            // 
            this.chkUseSensorNameAsKey.AutoSize = true;
            this.chkUseSensorNameAsKey.Location = new System.Drawing.Point(12, 178);
            this.chkUseSensorNameAsKey.Name = "chkUseSensorNameAsKey";
            this.chkUseSensorNameAsKey.Size = new System.Drawing.Size(122, 17);
            this.chkUseSensorNameAsKey.TabIndex = 9;
            this.chkUseSensorNameAsKey.Text = "Sensor name as key";
            this.chkUseSensorNameAsKey.UseVisualStyleBackColor = true;
            // 
            // lblAvailableState
            // 
            this.lblAvailableState.AutoSize = true;
            this.lblAvailableState.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.lblAvailableState.Location = new System.Drawing.Point(99, 52);
            this.lblAvailableState.Name = "lblAvailableState";
            this.lblAvailableState.Size = new System.Drawing.Size(51, 16);
            this.lblAvailableState.TabIndex = 12;
            this.lblAvailableState.Text = "label4";
            // 
            // lblRunningState
            // 
            this.lblRunningState.AutoSize = true;
            this.lblRunningState.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.lblRunningState.Location = new System.Drawing.Point(99, 82);
            this.lblRunningState.Name = "lblRunningState";
            this.lblRunningState.Size = new System.Drawing.Size(51, 16);
            this.lblRunningState.TabIndex = 13;
            this.lblRunningState.Text = "label5";
            // 
            // btnTryRun
            // 
            this.btnTryRun.Location = new System.Drawing.Point(410, 47);
            this.btnTryRun.Name = "btnTryRun";
            this.btnTryRun.Size = new System.Drawing.Size(75, 23);
            this.btnTryRun.TabIndex = 14;
            this.btnTryRun.Text = "Try Run";
            this.btnTryRun.UseVisualStyleBackColor = true;
            this.btnTryRun.Click += new System.EventHandler(this.TryRunClick);
            // 
            // radioButton1
            // 
            this.radioButton1.AutoSize = true;
            this.radioButton1.Location = new System.Drawing.Point(207, 132);
            this.radioButton1.Name = "radioButton1";
            this.radioButton1.Size = new System.Drawing.Size(51, 17);
            this.radioButton1.TabIndex = 16;
            this.radioButton1.TabStop = true;
            this.radioButton1.Text = "None";
            this.radioButton1.UseVisualStyleBackColor = true;
            // 
            // radioButton2
            // 
            this.radioButton2.AutoSize = true;
            this.radioButton2.Location = new System.Drawing.Point(207, 155);
            this.radioButton2.Name = "radioButton2";
            this.radioButton2.Size = new System.Drawing.Size(49, 17);
            this.radioButton2.TabIndex = 17;
            this.radioButton2.TabStop = true;
            this.radioButton2.Text = "Type";
            this.radioButton2.UseVisualStyleBackColor = true;
            // 
            // radioButton3
            // 
            this.radioButton3.AutoSize = true;
            this.radioButton3.Location = new System.Drawing.Point(207, 178);
            this.radioButton3.Name = "radioButton3";
            this.radioButton3.Size = new System.Drawing.Size(71, 17);
            this.radioButton3.TabIndex = 18;
            this.radioButton3.TabStop = true;
            this.radioButton3.Text = "Hardware";
            this.radioButton3.UseVisualStyleBackColor = true;
            // 
            // RtssOptionForm
            // 
            this.AcceptButton = this.btnOk;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.btnCancel;
            this.ClientSize = new System.Drawing.Size(497, 211);
            this.Controls.Add(label5);
            this.Controls.Add(this.radioButton3);
            this.Controls.Add(this.radioButton2);
            this.Controls.Add(this.radioButton1);
            this.Controls.Add(label4);
            this.Controls.Add(this.btnTryRun);
            this.Controls.Add(this.lblRunningState);
            this.Controls.Add(this.lblAvailableState);
            this.Controls.Add(label3);
            this.Controls.Add(label2);
            this.Controls.Add(this.chkUseSensorNameAsKey);
            this.Controls.Add(this.chkSeparateGroups);
            this.Controls.Add(this.txtRtssLocation);
            this.Controls.Add(this.chkAddFpt);
            this.Controls.Add(label1);
            this.Controls.Add(this.btnDetectPath);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnOk);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "RtssOptionForm";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "RivaTuner Statistics Server options";
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.OptionFormClosed);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.Button btnOk;
        private System.Windows.Forms.Timer statusTimer;
        private System.Windows.Forms.Button btnDetectPath;
        private System.Windows.Forms.CheckBox chkAddFpt;
        private System.Windows.Forms.TextBox txtRtssLocation;
        private System.Windows.Forms.CheckBox chkSeparateGroups;
        private System.Windows.Forms.CheckBox chkUseSensorNameAsKey;
        private System.Windows.Forms.Label lblAvailableState;
        private System.Windows.Forms.Label lblRunningState;
        private System.Windows.Forms.Button btnTryRun;
        private System.Windows.Forms.RadioButton radioButton1;
        private System.Windows.Forms.RadioButton radioButton2;
        private System.Windows.Forms.RadioButton radioButton3;
    }
}