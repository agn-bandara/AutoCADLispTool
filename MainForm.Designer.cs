namespace AutoCADLispTool
{
    partial class MainForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.ContextMenuStrip _listContextMenu;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _cancellationTokenSource?.Dispose();
                _logger?.Dispose();
                _toolTip?.Dispose();
                components?.Dispose();
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
            this.txtLspFile = new System.Windows.Forms.TextBox();
            this.btnLoad = new System.Windows.Forms.Button();
            this.txtCommand = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.lstDwgList = new System.Windows.Forms.ListView();
            this.btnProcess = new System.Windows.Forms.Button();
            this.btnDwgs = new System.Windows.Forms.Button();
            this.chkClose = new System.Windows.Forms.CheckBox();
            this.btnAppend = new System.Windows.Forms.Button();
            this.btnClear = new System.Windows.Forms.Button();
            this.btnClearSuccess = new System.Windows.Forms.Button();
            this.prgDrawingProgress = new System.Windows.Forms.ProgressBar();
            this.lblProgress = new System.Windows.Forms.Label();
            this.statusStrip = new System.Windows.Forms.StatusStrip();
            this.statusLabelHelp = new System.Windows.Forms.ToolStripStatusLabel();
            this.statusLabelCount = new System.Windows.Forms.ToolStripStatusLabel();
            this.statusStrip.SuspendLayout();
            this.SuspendLayout();
            // 
            // txtLspFile
            // 
            this.txtLspFile.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtLspFile.Location = new System.Drawing.Point(13, 13);
            this.txtLspFile.Name = "txtLspFile";
            this.txtLspFile.ReadOnly = true;
            this.txtLspFile.Size = new System.Drawing.Size(414, 20);
            this.txtLspFile.TabIndex = 0;
            // 
            // btnLoad
            // 
            this.btnLoad.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnLoad.Location = new System.Drawing.Point(352, 35);
            this.btnLoad.Name = "btnLoad";
            this.btnLoad.Size = new System.Drawing.Size(75, 23);
            this.btnLoad.TabIndex = 1;
            this.btnLoad.Text = "Browse...";
            this.btnLoad.UseVisualStyleBackColor = true;
            this.btnLoad.Click += new System.EventHandler(this.btnLoad_Click);
            // 
            // txtCommand
            // 
            this.txtCommand.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtCommand.Location = new System.Drawing.Point(78, 64);
            this.txtCommand.Name = "txtCommand";
            this.txtCommand.Size = new System.Drawing.Size(349, 20);
            this.txtCommand.TabIndex = 2;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 67);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(60, 13);
            this.label1.TabIndex = 3;
            this.label1.Text = "Command :";
            // 
            // lstDwgList
            // 
            this.lstDwgList.AllowDrop = true;
            this.lstDwgList.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.lstDwgList.FullRowSelect = true;
            this.lstDwgList.GridLines = true;
            this.lstDwgList.HideSelection = false;
            this.lstDwgList.Location = new System.Drawing.Point(13, 90);
            this.lstDwgList.Name = "lstDwgList";
            this.lstDwgList.Size = new System.Drawing.Size(414, 225);
            this.lstDwgList.TabIndex = 4;
            this.lstDwgList.UseCompatibleStateImageBehavior = false;
            this.lstDwgList.View = System.Windows.Forms.View.Details;
            // 
            // btnProcess
            // 
            this.btnProcess.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnProcess.Location = new System.Drawing.Point(353, 353);
            this.btnProcess.Name = "btnProcess";
            this.btnProcess.Size = new System.Drawing.Size(75, 52);
            this.btnProcess.TabIndex = 5;
            this.btnProcess.Text = "Process";
            this.btnProcess.UseVisualStyleBackColor = true;
            this.btnProcess.Click += new System.EventHandler(this.btnProcess_Click);
            // 
            // btnDwgs
            // 
            this.btnDwgs.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnDwgs.Location = new System.Drawing.Point(250, 353);
            this.btnDwgs.Name = "btnDwgs";
            this.btnDwgs.Size = new System.Drawing.Size(95, 23);
            this.btnDwgs.TabIndex = 6;
            this.btnDwgs.Text = "Load Dwg";
            this.btnDwgs.UseVisualStyleBackColor = true;
            this.btnDwgs.Click += new System.EventHandler(this.btnDwgs_Click);
            // 
            // chkClose
            // 
            this.chkClose.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.chkClose.AutoSize = true;
            this.chkClose.Checked = true;
            this.chkClose.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkClose.Location = new System.Drawing.Point(14, 359);
            this.chkClose.Name = "chkClose";
            this.chkClose.Size = new System.Drawing.Size(145, 17);
            this.chkClose.TabIndex = 7;
            this.chkClose.Text = "Close drawing after finish";
            this.chkClose.UseVisualStyleBackColor = true;
            // 
            // btnAppend
            // 
            this.btnAppend.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnAppend.Location = new System.Drawing.Point(250, 382);
            this.btnAppend.Name = "btnAppend";
            this.btnAppend.Size = new System.Drawing.Size(95, 23);
            this.btnAppend.TabIndex = 8;
            this.btnAppend.Text = "Append Dwg";
            this.btnAppend.UseVisualStyleBackColor = true;
            this.btnAppend.Click += new System.EventHandler(this.btnAppend_Click);
            // 
            // btnClear
            // 
            this.btnClear.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnClear.Location = new System.Drawing.Point(14, 382);
            this.btnClear.Name = "btnClear";
            this.btnClear.Size = new System.Drawing.Size(75, 23);
            this.btnClear.TabIndex = 9;
            this.btnClear.Text = "Clear List";
            this.btnClear.UseVisualStyleBackColor = true;
            this.btnClear.Click += new System.EventHandler(this.btnClear_Click);
            // 
            // btnClearSuccess
            // 
            this.btnClearSuccess.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnClearSuccess.Location = new System.Drawing.Point(100, 382);
            this.btnClearSuccess.Name = "btnClearSuccess";
            this.btnClearSuccess.Size = new System.Drawing.Size(140, 23);
            this.btnClearSuccess.TabIndex = 13;
            this.btnClearSuccess.Text = "Clear Success";
            this.btnClearSuccess.UseVisualStyleBackColor = true;
            this.btnClearSuccess.Click += new System.EventHandler(this.btnClearSuccess_Click);
            // 
            // prgDrawingProgress
            // 
            this.prgDrawingProgress.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.prgDrawingProgress.Location = new System.Drawing.Point(12, 321);
            this.prgDrawingProgress.Name = "prgDrawingProgress";
            this.prgDrawingProgress.Size = new System.Drawing.Size(340, 23);
            this.prgDrawingProgress.TabIndex = 10;
            // 
            // lblProgress
            // 
            this.lblProgress.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.lblProgress.AutoSize = true;
            this.lblProgress.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.lblProgress.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblProgress.Location = new System.Drawing.Point(360, 322);
            this.lblProgress.Name = "lblProgress";
            this.lblProgress.Size = new System.Drawing.Size(34, 22);
            this.lblProgress.TabIndex = 11;
            this.lblProgress.Text = "0%";
            // 
            // statusStrip
            // 
            this.statusStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.statusLabelHelp,
            this.statusLabelCount});
            this.statusStrip.Location = new System.Drawing.Point(0, 418);
            this.statusStrip.Name = "statusStrip";
            this.statusStrip.Size = new System.Drawing.Size(440, 22);
            this.statusStrip.SizingGrip = false;
            this.statusStrip.TabIndex = 12;
            this.statusStrip.Text = "statusStrip";
            // 
            // statusLabelHelp
            // 
            this.statusLabelHelp.Name = "statusLabelHelp";
            this.statusLabelHelp.Size = new System.Drawing.Size(259, 17);
            this.statusLabelHelp.Spring = true;
            this.statusLabelHelp.Text = "Select a LISP file and drawings to start.";
            this.statusLabelHelp.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // statusLabelCount
            // 
            this.statusLabelCount.Name = "statusLabelCount";
            this.statusLabelCount.Size = new System.Drawing.Size(77, 17);
            this.statusLabelCount.Text = "0 drawings";
            this.statusLabelCount.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(440, 440);
            this.Controls.Add(this.statusStrip);
            this.Controls.Add(this.lblProgress);
            this.Controls.Add(this.prgDrawingProgress);
            this.Controls.Add(this.btnClearSuccess);
            this.Controls.Add(this.btnClear);
            this.Controls.Add(this.btnAppend);
            this.Controls.Add(this.chkClose);
            this.Controls.Add(this.btnDwgs);
            this.Controls.Add(this.btnProcess);
            this.Controls.Add(this.lstDwgList);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.txtCommand);
            this.Controls.Add(this.btnLoad);
            this.Controls.Add(this.txtLspFile);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.Name = "MainForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Run Lisp";
            this.statusStrip.ResumeLayout(false);
            this.statusStrip.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox txtLspFile;
        private System.Windows.Forms.Button btnLoad;
        private System.Windows.Forms.TextBox txtCommand;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.ListView lstDwgList;
        private System.Windows.Forms.Button btnProcess;
        private System.Windows.Forms.Button btnDwgs;
        private System.Windows.Forms.CheckBox chkClose;
        private System.Windows.Forms.Button btnAppend;
        private System.Windows.Forms.Button btnClear;
        private System.Windows.Forms.Button btnClearSuccess;
        private System.Windows.Forms.ProgressBar prgDrawingProgress;
        private System.Windows.Forms.Label lblProgress;
        private System.Windows.Forms.StatusStrip statusStrip;
        private System.Windows.Forms.ToolStripStatusLabel statusLabelHelp;
        private System.Windows.Forms.ToolStripStatusLabel statusLabelCount;
    }
}
