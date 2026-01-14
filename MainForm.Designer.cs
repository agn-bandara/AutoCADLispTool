namespace AutoCADLispTool
{
    partial class MainForm
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
            this.prgDrawingProgress = new System.Windows.Forms.ProgressBar();
            this.lblProgress = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // txtLspFile
            // 
            this.txtLspFile.Location = new System.Drawing.Point(13, 13);
            this.txtLspFile.Name = "txtLspFile";
            this.txtLspFile.Size = new System.Drawing.Size(327, 20);
            this.txtLspFile.TabIndex = 0;
            // 
            // btnLoad
            // 
            this.btnLoad.Location = new System.Drawing.Point(265, 35);
            this.btnLoad.Name = "btnLoad";
            this.btnLoad.Size = new System.Drawing.Size(75, 23);
            this.btnLoad.TabIndex = 1;
            this.btnLoad.Text = "Load File";
            this.btnLoad.UseVisualStyleBackColor = true;
            this.btnLoad.Click += new System.EventHandler(this.btnLoad_Click);
            // 
            // txtCommand
            // 
            this.txtCommand.Location = new System.Drawing.Point(78, 64);
            this.txtCommand.Name = "txtCommand";
            this.txtCommand.Size = new System.Drawing.Size(262, 20);
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
            this.lstDwgList.FullRowSelect = true;
            this.lstDwgList.GridLines = true;
            this.lstDwgList.HideSelection = false;
            this.lstDwgList.Location = new System.Drawing.Point(13, 90);
            this.lstDwgList.Name = "lstDwgList";
            this.lstDwgList.Size = new System.Drawing.Size(327, 225);
            this.lstDwgList.TabIndex = 4;
            this.lstDwgList.UseCompatibleStateImageBehavior = false;
            this.lstDwgList.View = System.Windows.Forms.View.Details;
            // 
            // btnProcess
            // 
            this.btnProcess.Location = new System.Drawing.Point(266, 353);
            this.btnProcess.Name = "btnProcess";
            this.btnProcess.Size = new System.Drawing.Size(75, 52);
            this.btnProcess.TabIndex = 5;
            this.btnProcess.Text = "Process";
            this.btnProcess.UseVisualStyleBackColor = true;
            this.btnProcess.Click += new System.EventHandler(this.btnProcess_Click);
            // 
            // btnDwgs
            // 
            this.btnDwgs.Location = new System.Drawing.Point(165, 353);
            this.btnDwgs.Name = "btnDwgs";
            this.btnDwgs.Size = new System.Drawing.Size(95, 23);
            this.btnDwgs.TabIndex = 6;
            this.btnDwgs.Text = "Load Dwg";
            this.btnDwgs.UseVisualStyleBackColor = true;
            this.btnDwgs.Click += new System.EventHandler(this.btnDwgs_Click);
            // 
            // chkClose
            // 
            this.chkClose.AutoSize = true;
            this.chkClose.Checked = true;
            this.chkClose.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkClose.Location = new System.Drawing.Point(14, 359);
            this.chkClose.Name = "chkClose";
            this.chkClose.Size = new System.Drawing.Size(117, 17);
            this.chkClose.TabIndex = 7;
            this.chkClose.Text = "Close after Process";
            this.chkClose.UseVisualStyleBackColor = true;
            // 
            // btnAppend
            // 
            this.btnAppend.Location = new System.Drawing.Point(165, 382);
            this.btnAppend.Name = "btnAppend";
            this.btnAppend.Size = new System.Drawing.Size(95, 23);
            this.btnAppend.TabIndex = 8;
            this.btnAppend.Text = "Append Dwg";
            this.btnAppend.UseVisualStyleBackColor = true;
            this.btnAppend.Click += new System.EventHandler(this.btnAppend_Click);
            // 
            // btnClear
            // 
            this.btnClear.Location = new System.Drawing.Point(14, 382);
            this.btnClear.Name = "btnClear";
            this.btnClear.Size = new System.Drawing.Size(75, 23);
            this.btnClear.TabIndex = 9;
            this.btnClear.Text = "Clear List";
            this.btnClear.UseVisualStyleBackColor = true;
            this.btnClear.Click += new System.EventHandler(this.btnClear_Click);
            // 
            // prgDrawingProgress
            // 
            this.prgDrawingProgress.Location = new System.Drawing.Point(12, 321);
            this.prgDrawingProgress.Name = "prgDrawingProgress";
            this.prgDrawingProgress.Size = new System.Drawing.Size(254, 23);
            this.prgDrawingProgress.TabIndex = 10;
            // 
            // lblProgress
            // 
            this.lblProgress.AutoSize = true;
            this.lblProgress.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.lblProgress.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblProgress.Location = new System.Drawing.Point(272, 322);
            this.lblProgress.Name = "lblProgress";
            this.lblProgress.Size = new System.Drawing.Size(34, 22);
            this.lblProgress.TabIndex = 11;
            this.lblProgress.Text = "0%";
            // 
            // MainFom
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(352, 417);
            this.Controls.Add(this.lblProgress);
            this.Controls.Add(this.prgDrawingProgress);
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
            this.Name = "MainFom";
            this.Text = "Run Lisp";
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
        private System.Windows.Forms.ProgressBar prgDrawingProgress;
        private System.Windows.Forms.Label lblProgress;
    }
}