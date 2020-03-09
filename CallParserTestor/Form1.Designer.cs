namespace CallParserTestor
{
    partial class Form1
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
            this.button1 = new System.Windows.Forms.Button();
            this.TextBoxCall = new System.Windows.Forms.TextBox();
            this.button2 = new System.Windows.Forms.Button();
            this.button3 = new System.Windows.Forms.Button();
            this.LabelElapsedTime = new System.Windows.Forms.Label();
            this.LabelHitCount = new System.Windows.Forms.Label();
            this.LabelPerCallTime = new System.Windows.Forms.Label();
            this.TextBoxPrefixFilePath = new System.Windows.Forms.TextBox();
            this.ButtonLoadPrefixFile = new System.Windows.Forms.Button();
            this.LabelCallsLoaded = new System.Windows.Forms.Label();
            this.ButtonSemiBatch = new System.Windows.Forms.Button();
            this.ListViewResults = new System.Windows.Forms.ListView();
            this.columnHeader1 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader2 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader3 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader4 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader5 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.label1 = new System.Windows.Forms.Label();
            this.ButtonSelectFolder = new System.Windows.Forms.Button();
            this.OpenPrefixFileDialog = new System.Windows.Forms.OpenFileDialog();
            this.SuspendLayout();
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(229, 90);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(154, 27);
            this.button1.TabIndex = 0;
            this.button1.Text = "Lookup Individual Call";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.ButtonSingleCallLookup_Click);
            // 
            // TextBoxCall
            // 
            this.TextBoxCall.CharacterCasing = System.Windows.Forms.CharacterCasing.Upper;
            this.TextBoxCall.Location = new System.Drawing.Point(390, 92);
            this.TextBoxCall.Name = "TextBoxCall";
            this.TextBoxCall.Size = new System.Drawing.Size(116, 23);
            this.TextBoxCall.TabIndex = 1;
            this.TextBoxCall.Text = "BU2EO/W4";
            // 
            // button2
            // 
            this.button2.Location = new System.Drawing.Point(14, 123);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(154, 27);
            this.button2.TabIndex = 2;
            this.button2.Text = "Load Call Signs";
            this.button2.UseVisualStyleBackColor = true;
            this.button2.Click += new System.EventHandler(this.ButtonLoadCallSigns_Click);
            // 
            // button3
            // 
            this.button3.Location = new System.Drawing.Point(229, 157);
            this.button3.Name = "button3";
            this.button3.Size = new System.Drawing.Size(154, 27);
            this.button3.TabIndex = 3;
            this.button3.Text = "Batch Lookup";
            this.button3.UseVisualStyleBackColor = true;
            this.button3.Click += new System.EventHandler(this.ButtonBatchCallSignLookup_Click);
            // 
            // LabelElapsedTime
            // 
            this.LabelElapsedTime.AutoSize = true;
            this.LabelElapsedTime.Location = new System.Drawing.Point(15, 184);
            this.LabelElapsedTime.Name = "LabelElapsedTime";
            this.LabelElapsedTime.Size = new System.Drawing.Size(0, 15);
            this.LabelElapsedTime.TabIndex = 4;
            // 
            // LabelHitCount
            // 
            this.LabelHitCount.AutoSize = true;
            this.LabelHitCount.Location = new System.Drawing.Point(395, 129);
            this.LabelHitCount.Name = "LabelHitCount";
            this.LabelHitCount.Size = new System.Drawing.Size(0, 15);
            this.LabelHitCount.TabIndex = 5;
            // 
            // LabelPerCallTime
            // 
            this.LabelPerCallTime.AutoSize = true;
            this.LabelPerCallTime.Location = new System.Drawing.Point(395, 163);
            this.LabelPerCallTime.Name = "LabelPerCallTime";
            this.LabelPerCallTime.Size = new System.Drawing.Size(0, 15);
            this.LabelPerCallTime.TabIndex = 6;
            // 
            // TextBoxPrefixFilePath
            // 
            this.TextBoxPrefixFilePath.Location = new System.Drawing.Point(14, 42);
            this.TextBoxPrefixFilePath.Name = "TextBoxPrefixFilePath";
            this.TextBoxPrefixFilePath.Size = new System.Drawing.Size(440, 23);
            this.TextBoxPrefixFilePath.TabIndex = 7;
            // 
            // ButtonLoadPrefixFile
            // 
            this.ButtonLoadPrefixFile.Location = new System.Drawing.Point(14, 90);
            this.ButtonLoadPrefixFile.Name = "ButtonLoadPrefixFile";
            this.ButtonLoadPrefixFile.Size = new System.Drawing.Size(153, 27);
            this.ButtonLoadPrefixFile.TabIndex = 9;
            this.ButtonLoadPrefixFile.Text = "Load Prefix File";
            this.ButtonLoadPrefixFile.UseVisualStyleBackColor = true;
            this.ButtonLoadPrefixFile.Click += new System.EventHandler(this.ButtonLoadPrefixFile_Click);
            // 
            // LabelCallsLoaded
            // 
            this.LabelCallsLoaded.AutoSize = true;
            this.LabelCallsLoaded.Location = new System.Drawing.Point(12, 157);
            this.LabelCallsLoaded.Name = "LabelCallsLoaded";
            this.LabelCallsLoaded.Size = new System.Drawing.Size(0, 15);
            this.LabelCallsLoaded.TabIndex = 10;
            // 
            // ButtonSemiBatch
            // 
            this.ButtonSemiBatch.Location = new System.Drawing.Point(229, 123);
            this.ButtonSemiBatch.Name = "ButtonSemiBatch";
            this.ButtonSemiBatch.Size = new System.Drawing.Size(154, 27);
            this.ButtonSemiBatch.TabIndex = 11;
            this.ButtonSemiBatch.Text = "Semi Batch";
            this.ButtonSemiBatch.UseVisualStyleBackColor = true;
            this.ButtonSemiBatch.Click += new System.EventHandler(this.ButtonSemiBatch_Click);
            // 
            // ListViewResults
            // 
            this.ListViewResults.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeader1,
            this.columnHeader2,
            this.columnHeader3,
            this.columnHeader4,
            this.columnHeader5});
            this.ListViewResults.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.ListViewResults.HideSelection = false;
            this.ListViewResults.Location = new System.Drawing.Point(0, 236);
            this.ListViewResults.Name = "ListViewResults";
            this.ListViewResults.Size = new System.Drawing.Size(772, 321);
            this.ListViewResults.TabIndex = 12;
            this.ListViewResults.UseCompatibleStateImageBehavior = false;
            this.ListViewResults.View = System.Windows.Forms.View.Details;
            // 
            // columnHeader1
            // 
            this.columnHeader1.Text = "Call";
            this.columnHeader1.Width = 150;
            // 
            // columnHeader2
            // 
            this.columnHeader2.Text = "Kind";
            // 
            // columnHeader3
            // 
            this.columnHeader3.Text = "Country";
            this.columnHeader3.Width = 150;
            // 
            // columnHeader4
            // 
            this.columnHeader4.Text = "Province";
            this.columnHeader4.Width = 150;
            // 
            // columnHeader5
            // 
            this.columnHeader5.Text = "Dxcc";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(15, 23);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(362, 15);
            this.label1.TabIndex = 13;
            this.label1.Text = "Select Prefix File (if left empty the internal resource file will be used)";
            // 
            // ButtonSelectFolder
            // 
            this.ButtonSelectFolder.Location = new System.Drawing.Point(475, 42);
            this.ButtonSelectFolder.Name = "ButtonSelectFolder";
            this.ButtonSelectFolder.Size = new System.Drawing.Size(31, 23);
            this.ButtonSelectFolder.TabIndex = 14;
            this.ButtonSelectFolder.Text = ". . .";
            this.ButtonSelectFolder.TextAlign = System.Drawing.ContentAlignment.TopCenter;
            this.ButtonSelectFolder.UseVisualStyleBackColor = true;
            this.ButtonSelectFolder.Click += new System.EventHandler(this.ButtonSelectFolder_Click);
            // 
            // OpenPrefixFileDialog
            // 
            this.OpenPrefixFileDialog.FileName = "prefix.xml";
            this.OpenPrefixFileDialog.Filter = "Prefix File|*.xml";
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(772, 557);
            this.Controls.Add(this.ButtonSelectFolder);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.ListViewResults);
            this.Controls.Add(this.ButtonSemiBatch);
            this.Controls.Add(this.LabelCallsLoaded);
            this.Controls.Add(this.ButtonLoadPrefixFile);
            this.Controls.Add(this.TextBoxPrefixFilePath);
            this.Controls.Add(this.LabelPerCallTime);
            this.Controls.Add(this.LabelHitCount);
            this.Controls.Add(this.LabelElapsedTime);
            this.Controls.Add(this.button3);
            this.Controls.Add(this.button2);
            this.Controls.Add(this.TextBoxCall);
            this.Controls.Add(this.button1);
            this.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Name = "Form1";
            this.Text = "Form1";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.TextBox TextBoxCall;
        private System.Windows.Forms.Button button2;
        private System.Windows.Forms.Button button3;
        private System.Windows.Forms.Label LabelElapsedTime;
        private System.Windows.Forms.Label LabelHitCount;
        private System.Windows.Forms.Label LabelPerCallTime;
        private System.Windows.Forms.TextBox TextBoxPrefixFilePath;
        private System.Windows.Forms.Button ButtonLoadPrefixFile;
        private System.Windows.Forms.Label LabelCallsLoaded;
        private System.Windows.Forms.Button ButtonSemiBatch;
        private System.Windows.Forms.ListView ListViewResults;
        private System.Windows.Forms.ColumnHeader columnHeader1;
        private System.Windows.Forms.ColumnHeader columnHeader2;
        private System.Windows.Forms.ColumnHeader columnHeader3;
        private System.Windows.Forms.ColumnHeader columnHeader4;
        private System.Windows.Forms.ColumnHeader columnHeader5;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button ButtonSelectFolder;
        private System.Windows.Forms.OpenFileDialog OpenPrefixFileDialog;
    }
}

