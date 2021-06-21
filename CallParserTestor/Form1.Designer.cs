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
            this.components = new System.ComponentModel.Container();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle9 = new System.Windows.Forms.DataGridViewCellStyle();
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
            this.columnHeader6 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.label1 = new System.Windows.Forms.Label();
            this.ButtonSelectFolder = new System.Windows.Forms.Button();
            this.OpenPrefixFileDialog = new System.Windows.Forms.OpenFileDialog();
            this.LabelCallsLoadedDistinct = new System.Windows.Forms.Label();
            this.DataGridViewResults = new System.Windows.Forms.DataGridView();
            this.panel1 = new System.Windows.Forms.Panel();
            this.button4 = new System.Windows.Forms.Button();
            this.CheckBoxCompoundCalls = new System.Windows.Forms.CheckBox();
            this.CheckBoxMergeHits = new System.Windows.Forms.CheckBox();
            this.CheckBoxQRZ = new System.Windows.Forms.CheckBox();
            this.TextBoxQRZuserId = new System.Windows.Forms.TextBox();
            this.TextBoxQRZPassword = new System.Windows.Forms.TextBox();
            this.callLookUpBindingSource = new System.Windows.Forms.BindingSource(this.components);
            ((System.ComponentModel.ISupportInitialize)(this.DataGridViewResults)).BeginInit();
            this.panel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.callLookUpBindingSource)).BeginInit();
            this.SuspendLayout();
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(300, 104);
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
            this.TextBoxCall.Location = new System.Drawing.Point(466, 107);
            this.TextBoxCall.Name = "TextBoxCall";
            this.TextBoxCall.Size = new System.Drawing.Size(94, 23);
            this.TextBoxCall.TabIndex = 1;
            this.TextBoxCall.Text = "TX4YKP/R";
            // 
            // button2
            // 
            this.button2.Location = new System.Drawing.Point(14, 137);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(154, 27);
            this.button2.TabIndex = 2;
            this.button2.Text = "Load Call Signs";
            this.button2.UseVisualStyleBackColor = true;
            this.button2.Click += new System.EventHandler(this.ButtonLoadCallSigns_Click);
            // 
            // button3
            // 
            this.button3.Location = new System.Drawing.Point(300, 170);
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
            this.LabelElapsedTime.Location = new System.Drawing.Point(463, 199);
            this.LabelElapsedTime.Name = "LabelElapsedTime";
            this.LabelElapsedTime.Size = new System.Drawing.Size(0, 15);
            this.LabelElapsedTime.TabIndex = 4;
            // 
            // LabelHitCount
            // 
            this.LabelHitCount.AutoSize = true;
            this.LabelHitCount.Location = new System.Drawing.Point(463, 142);
            this.LabelHitCount.Name = "LabelHitCount";
            this.LabelHitCount.Size = new System.Drawing.Size(0, 15);
            this.LabelHitCount.TabIndex = 5;
            // 
            // LabelPerCallTime
            // 
            this.LabelPerCallTime.AutoSize = true;
            this.LabelPerCallTime.Location = new System.Drawing.Point(463, 176);
            this.LabelPerCallTime.Name = "LabelPerCallTime";
            this.LabelPerCallTime.Size = new System.Drawing.Size(0, 15);
            this.LabelPerCallTime.TabIndex = 6;
            // 
            // TextBoxPrefixFilePath
            // 
            this.TextBoxPrefixFilePath.Location = new System.Drawing.Point(16, 43);
            this.TextBoxPrefixFilePath.Name = "TextBoxPrefixFilePath";
            this.TextBoxPrefixFilePath.Size = new System.Drawing.Size(440, 23);
            this.TextBoxPrefixFilePath.TabIndex = 7;
            // 
            // ButtonLoadPrefixFile
            // 
            this.ButtonLoadPrefixFile.Location = new System.Drawing.Point(14, 104);
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
            this.LabelCallsLoaded.Location = new System.Drawing.Point(12, 179);
            this.LabelCallsLoaded.Name = "LabelCallsLoaded";
            this.LabelCallsLoaded.Size = new System.Drawing.Size(0, 15);
            this.LabelCallsLoaded.TabIndex = 10;
            // 
            // ButtonSemiBatch
            // 
            this.ButtonSemiBatch.Location = new System.Drawing.Point(466, 9);
            this.ButtonSemiBatch.Name = "ButtonSemiBatch";
            this.ButtonSemiBatch.Size = new System.Drawing.Size(154, 27);
            this.ButtonSemiBatch.TabIndex = 11;
            this.ButtonSemiBatch.Text = "Semi Batch";
            this.ButtonSemiBatch.UseVisualStyleBackColor = true;
            this.ButtonSemiBatch.Visible = false;
            this.ButtonSemiBatch.Click += new System.EventHandler(this.ButtonSemiBatch_Click);
            // 
            // ListViewResults
            // 
            this.ListViewResults.BackColor = System.Drawing.Color.Honeydew;
            this.ListViewResults.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeader1,
            this.columnHeader2,
            this.columnHeader3,
            this.columnHeader4,
            this.columnHeader5,
            this.columnHeader6});
            this.ListViewResults.Dock = System.Windows.Forms.DockStyle.Top;
            this.ListViewResults.HideSelection = false;
            this.ListViewResults.Location = new System.Drawing.Point(0, 0);
            this.ListViewResults.Name = "ListViewResults";
            this.ListViewResults.Size = new System.Drawing.Size(727, 149);
            this.ListViewResults.TabIndex = 12;
            this.ListViewResults.UseCompatibleStateImageBehavior = false;
            this.ListViewResults.View = System.Windows.Forms.View.Details;
            // 
            // columnHeader1
            // 
            this.columnHeader1.Text = "Call";
            this.columnHeader1.Width = 100;
            // 
            // columnHeader2
            // 
            this.columnHeader2.Text = "Kind";
            // 
            // columnHeader3
            // 
            this.columnHeader3.Text = "Country";
            this.columnHeader3.Width = 100;
            // 
            // columnHeader4
            // 
            this.columnHeader4.Text = "Province";
            this.columnHeader4.Width = 100;
            // 
            // columnHeader5
            // 
            this.columnHeader5.Text = "Dxcc";
            this.columnHeader5.Width = 119;
            // 
            // columnHeader6
            // 
            this.columnHeader6.Text = "Call Sign Flags";
            this.columnHeader6.Width = 150;
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
            this.ButtonSelectFolder.Location = new System.Drawing.Point(466, 42);
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
            // LabelCallsLoadedDistinct
            // 
            this.LabelCallsLoadedDistinct.AutoSize = true;
            this.LabelCallsLoadedDistinct.Location = new System.Drawing.Point(13, 198);
            this.LabelCallsLoadedDistinct.Name = "LabelCallsLoadedDistinct";
            this.LabelCallsLoadedDistinct.Size = new System.Drawing.Size(0, 15);
            this.LabelCallsLoadedDistinct.TabIndex = 15;
            // 
            // DataGridViewResults
            // 
            this.DataGridViewResults.AllowUserToAddRows = false;
            this.DataGridViewResults.AllowUserToDeleteRows = false;
            dataGridViewCellStyle9.ForeColor = System.Drawing.Color.MidnightBlue;
            this.DataGridViewResults.AlternatingRowsDefaultCellStyle = dataGridViewCellStyle9;
            this.DataGridViewResults.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.DisplayedCells;
            this.DataGridViewResults.AutoSizeRowsMode = System.Windows.Forms.DataGridViewAutoSizeRowsMode.DisplayedCells;
            this.DataGridViewResults.BackgroundColor = System.Drawing.Color.Honeydew;
            this.DataGridViewResults.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.DataGridViewResults.Dock = System.Windows.Forms.DockStyle.Fill;
            this.DataGridViewResults.Location = new System.Drawing.Point(0, 149);
            this.DataGridViewResults.Name = "DataGridViewResults";
            this.DataGridViewResults.ReadOnly = true;
            this.DataGridViewResults.Size = new System.Drawing.Size(727, 412);
            this.DataGridViewResults.TabIndex = 16;
            this.DataGridViewResults.CellFormatting += new System.Windows.Forms.DataGridViewCellFormattingEventHandler(this.DataGridViewResults_CellFormatting);
            this.DataGridViewResults.DataBindingComplete += new System.Windows.Forms.DataGridViewBindingCompleteEventHandler(this.DataGridViewResults_DataBindingComplete);
            this.DataGridViewResults.RowsAdded += new System.Windows.Forms.DataGridViewRowsAddedEventHandler(this.DataGridViewResults_RowsAdded);
            // 
            // panel1
            // 
            this.panel1.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.panel1.BackColor = System.Drawing.SystemColors.ActiveCaption;
            this.panel1.Controls.Add(this.DataGridViewResults);
            this.panel1.Controls.Add(this.ListViewResults);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.panel1.Location = new System.Drawing.Point(0, 233);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(727, 561);
            this.panel1.TabIndex = 17;
            // 
            // button4
            // 
            this.button4.Location = new System.Drawing.Point(300, 137);
            this.button4.Name = "button4";
            this.button4.Size = new System.Drawing.Size(154, 27);
            this.button4.TabIndex = 18;
            this.button4.Text = "Load Delphi Compare File";
            this.button4.UseVisualStyleBackColor = true;
            this.button4.Click += new System.EventHandler(this.Button4_Click);
            // 
            // CheckBoxCompoundCalls
            // 
            this.CheckBoxCompoundCalls.AutoSize = true;
            this.CheckBoxCompoundCalls.Location = new System.Drawing.Point(174, 142);
            this.CheckBoxCompoundCalls.Name = "CheckBoxCompoundCalls";
            this.CheckBoxCompoundCalls.Size = new System.Drawing.Size(115, 19);
            this.CheckBoxCompoundCalls.TabIndex = 19;
            this.CheckBoxCompoundCalls.Text = "Compound Calls";
            this.CheckBoxCompoundCalls.UseVisualStyleBackColor = true;
            // 
            // CheckBoxMergeHits
            // 
            this.CheckBoxMergeHits.AutoSize = true;
            this.CheckBoxMergeHits.Enabled = false;
            this.CheckBoxMergeHits.Location = new System.Drawing.Point(174, 109);
            this.CheckBoxMergeHits.Name = "CheckBoxMergeHits";
            this.CheckBoxMergeHits.Size = new System.Drawing.Size(84, 19);
            this.CheckBoxMergeHits.TabIndex = 20;
            this.CheckBoxMergeHits.Text = "Merge Hits";
            this.CheckBoxMergeHits.UseVisualStyleBackColor = true;
            this.CheckBoxMergeHits.CheckedChanged += new System.EventHandler(this.CheckBoxMergeHits_CheckedChanged);
            // 
            // CheckBoxQRZ
            // 
            this.CheckBoxQRZ.AutoSize = true;
            this.CheckBoxQRZ.Location = new System.Drawing.Point(576, 109);
            this.CheckBoxQRZ.Name = "CheckBoxQRZ";
            this.CheckBoxQRZ.Size = new System.Drawing.Size(92, 19);
            this.CheckBoxQRZ.TabIndex = 21;
            this.CheckBoxQRZ.Text = "QRZ Lookup";
            this.CheckBoxQRZ.UseVisualStyleBackColor = true;
            // 
            // TextBoxQRZuserId
            // 
            this.TextBoxQRZuserId.Location = new System.Drawing.Point(15, 72);
            this.TextBoxQRZuserId.Name = "TextBoxQRZuserId";
            this.TextBoxQRZuserId.Size = new System.Drawing.Size(228, 23);
            this.TextBoxQRZuserId.TabIndex = 22;
            this.TextBoxQRZuserId.Enter += new System.EventHandler(this.TextBoxQRZuserId_Enter);
            this.TextBoxQRZuserId.Leave += new System.EventHandler(this.TextBoxQRZuserId_Leave);
            // 
            // TextBoxQRZPassword
            // 
            this.TextBoxQRZPassword.Location = new System.Drawing.Point(249, 72);
            this.TextBoxQRZPassword.Name = "TextBoxQRZPassword";
            this.TextBoxQRZPassword.Size = new System.Drawing.Size(205, 23);
            this.TextBoxQRZPassword.TabIndex = 23;
            this.TextBoxQRZPassword.Enter += new System.EventHandler(this.TextBoxQRZPassword_Enter);
            this.TextBoxQRZPassword.Leave += new System.EventHandler(this.TextBoxQRZPassword_Leave);
            // 
            // callLookUpBindingSource
            // 
            this.callLookUpBindingSource.DataSource = typeof(W6OP.CallParser.CallLookUp);
            // 
            // Form1
            // 
            this.AcceptButton = this.button1;
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(727, 794);
            this.Controls.Add(this.TextBoxQRZPassword);
            this.Controls.Add(this.TextBoxQRZuserId);
            this.Controls.Add(this.CheckBoxQRZ);
            this.Controls.Add(this.CheckBoxMergeHits);
            this.Controls.Add(this.CheckBoxCompoundCalls);
            this.Controls.Add(this.button4);
            this.Controls.Add(this.panel1);
            this.Controls.Add(this.LabelCallsLoadedDistinct);
            this.Controls.Add(this.ButtonSelectFolder);
            this.Controls.Add(this.label1);
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
            this.Load += new System.EventHandler(this.Form1_Load);
            ((System.ComponentModel.ISupportInitialize)(this.DataGridViewResults)).EndInit();
            this.panel1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.callLookUpBindingSource)).EndInit();
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
        private System.Windows.Forms.Label LabelCallsLoadedDistinct;
        private System.Windows.Forms.DataGridView DataGridViewResults;
        private System.Windows.Forms.BindingSource callLookUpBindingSource;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Button button4;
        private System.Windows.Forms.CheckBox CheckBoxCompoundCalls;
        private System.Windows.Forms.CheckBox CheckBoxMergeHits;
        private System.Windows.Forms.ColumnHeader columnHeader6;
        private System.Windows.Forms.CheckBox CheckBoxQRZ;
        private System.Windows.Forms.TextBox TextBoxQRZuserId;
        private System.Windows.Forms.TextBox TextBoxQRZPassword;
    }
}

