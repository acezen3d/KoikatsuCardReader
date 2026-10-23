namespace KoikatsuCardReader
{
    partial class MainForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            this.tableLayoutPanelMain = new System.Windows.Forms.TableLayoutPanel();
            this.flowLayoutPanelZipmodMap = new System.Windows.Forms.FlowLayoutPanel();
            this.buttonBuildZipmodMap = new System.Windows.Forms.Button();
            this.labelZipmodMap = new System.Windows.Forms.Label();
            this.buttonOpenZipmodMap = new System.Windows.Forms.Button();
            this.panelSeparator = new System.Windows.Forms.Panel();
            this.flowLayoutPanelPath = new System.Windows.Forms.FlowLayoutPanel();
            this.textBoxPath = new System.Windows.Forms.TextBox();
            this.buttonBrowse = new System.Windows.Forms.Button();
            this.checkBoxRecursive = new System.Windows.Forms.CheckBox();
            this.checkBoxParseCharactersInScenes = new System.Windows.Forms.CheckBox();
            this.flowLayoutPanelAction = new System.Windows.Forms.FlowLayoutPanel();
            this.buttonScan = new System.Windows.Forms.Button();
            this.buttonCancel = new System.Windows.Forms.Button();
            this.progressBar = new System.Windows.Forms.ProgressBar();
            this.labelStatus = new System.Windows.Forms.Label();
            this.dataGridView = new System.Windows.Forms.DataGridView();
            this.columnFileName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.columnPath = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.columnName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.columnGame = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.columnType = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.columnPlugins = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.columnZipmods = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.columnStatus = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.flowLayoutPanelMode = new System.Windows.Forms.FlowLayoutPanel();
            this.radioButtonSelected = new System.Windows.Forms.RadioButton();
            this.radioButtonAll = new System.Windows.Forms.RadioButton();
            this.buttonExport = new System.Windows.Forms.Button();
            this.treeViewDetails = new System.Windows.Forms.TreeView();
            this.tableLayoutPanelMain.SuspendLayout();
            this.flowLayoutPanelZipmodMap.SuspendLayout();
            this.flowLayoutPanelPath.SuspendLayout();
            this.flowLayoutPanelAction.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView)).BeginInit();
            this.flowLayoutPanelMode.SuspendLayout();
            this.SuspendLayout();
            // 
            // tableLayoutPanelMain
            // 
            this.tableLayoutPanelMain.ColumnCount = 1;
            this.tableLayoutPanelMain.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanelMain.Controls.Add(this.flowLayoutPanelZipmodMap, 0, 0);
            this.tableLayoutPanelMain.Controls.Add(this.panelSeparator, 0, 1);
            this.tableLayoutPanelMain.Controls.Add(this.flowLayoutPanelPath, 0, 2);
            this.tableLayoutPanelMain.Controls.Add(this.flowLayoutPanelAction, 0, 3);
            this.tableLayoutPanelMain.Controls.Add(this.dataGridView, 0, 4);
            this.tableLayoutPanelMain.Controls.Add(this.flowLayoutPanelMode, 0, 5);
            this.tableLayoutPanelMain.Controls.Add(this.treeViewDetails, 0, 6);
            this.tableLayoutPanelMain.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanelMain.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanelMain.Name = "tableLayoutPanelMain";
            this.tableLayoutPanelMain.Padding = new System.Windows.Forms.Padding(6);
            this.tableLayoutPanelMain.RowCount = 7;
            this.tableLayoutPanelMain.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanelMain.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanelMain.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanelMain.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanelMain.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 55F));
            this.tableLayoutPanelMain.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanelMain.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 45F));
            this.tableLayoutPanelMain.Size = new System.Drawing.Size(901, 800);
            this.tableLayoutPanelMain.TabIndex = 0;
            // 
            // flowLayoutPanelZipmodMap
            // 
            this.flowLayoutPanelZipmodMap.AutoSize = true;
            this.flowLayoutPanelZipmodMap.Controls.Add(this.buttonBuildZipmodMap);
            this.flowLayoutPanelZipmodMap.Controls.Add(this.labelZipmodMap);
            this.flowLayoutPanelZipmodMap.Controls.Add(this.buttonOpenZipmodMap);
            this.flowLayoutPanelZipmodMap.Dock = System.Windows.Forms.DockStyle.Fill;
            this.flowLayoutPanelZipmodMap.Location = new System.Drawing.Point(9, 9);
            this.flowLayoutPanelZipmodMap.Name = "flowLayoutPanelZipmodMap";
            this.flowLayoutPanelZipmodMap.Size = new System.Drawing.Size(883, 29);
            this.flowLayoutPanelZipmodMap.TabIndex = 0;
            this.flowLayoutPanelZipmodMap.WrapContents = false;
            // 
            // buttonBuildZipmodMap
            // 
            this.buttonBuildZipmodMap.Location = new System.Drawing.Point(0, 3);
            this.buttonBuildZipmodMap.Margin = new System.Windows.Forms.Padding(0, 3, 3, 3);
            this.buttonBuildZipmodMap.Name = "buttonBuildZipmodMap";
            this.buttonBuildZipmodMap.Size = new System.Drawing.Size(150, 23);
            this.buttonBuildZipmodMap.TabIndex = 0;
            this.buttonBuildZipmodMap.Text = "Build zipmod map";
            this.buttonBuildZipmodMap.UseVisualStyleBackColor = true;
            this.buttonBuildZipmodMap.Click += new System.EventHandler(this.ButtonBuildZipmodMap_Click);
            // 
            // labelZipmodMap
            // 
            this.labelZipmodMap.AutoSize = true;
            this.labelZipmodMap.Location = new System.Drawing.Point(156, 8);
            this.labelZipmodMap.Margin = new System.Windows.Forms.Padding(3, 8, 3, 0);
            this.labelZipmodMap.Name = "labelZipmodMap";
            this.labelZipmodMap.Size = new System.Drawing.Size(131, 12);
            this.labelZipmodMap.TabIndex = 1;
            this.labelZipmodMap.Text = "Zipmod map: not built";
            // 
            // buttonOpenZipmodMap
            // 
            this.buttonOpenZipmodMap.Enabled = false;
            this.buttonOpenZipmodMap.Location = new System.Drawing.Point(293, 3);
            this.buttonOpenZipmodMap.Name = "buttonOpenZipmodMap";
            this.buttonOpenZipmodMap.Size = new System.Drawing.Size(60, 23);
            this.buttonOpenZipmodMap.TabIndex = 2;
            this.buttonOpenZipmodMap.Text = "Open";
            this.buttonOpenZipmodMap.UseVisualStyleBackColor = true;
            this.buttonOpenZipmodMap.Click += new System.EventHandler(this.ButtonOpenZipmodMap_Click);
            // 
            // panelSeparator
            // 
            this.panelSeparator.BackColor = System.Drawing.SystemColors.ControlDark;
            this.panelSeparator.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelSeparator.Location = new System.Drawing.Point(9, 49);
            this.panelSeparator.Margin = new System.Windows.Forms.Padding(3, 8, 3, 8);
            this.panelSeparator.Name = "panelSeparator";
            this.panelSeparator.Size = new System.Drawing.Size(883, 1);
            this.panelSeparator.TabIndex = 1;
            // 
            // flowLayoutPanelPath
            // 
            this.flowLayoutPanelPath.AutoSize = true;
            this.flowLayoutPanelPath.Controls.Add(this.textBoxPath);
            this.flowLayoutPanelPath.Controls.Add(this.buttonBrowse);
            this.flowLayoutPanelPath.Controls.Add(this.checkBoxRecursive);
            this.flowLayoutPanelPath.Controls.Add(this.checkBoxParseCharactersInScenes);
            this.flowLayoutPanelPath.Dock = System.Windows.Forms.DockStyle.Fill;
            this.flowLayoutPanelPath.Location = new System.Drawing.Point(9, 61);
            this.flowLayoutPanelPath.Name = "flowLayoutPanelPath";
            this.flowLayoutPanelPath.Size = new System.Drawing.Size(883, 29);
            this.flowLayoutPanelPath.TabIndex = 2;
            this.flowLayoutPanelPath.WrapContents = false;
            // 
            // textBoxPath
            // 
            this.textBoxPath.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.textBoxPath.Location = new System.Drawing.Point(0, 4);
            this.textBoxPath.Margin = new System.Windows.Forms.Padding(0, 3, 3, 3);
            this.textBoxPath.Name = "textBoxPath";
            this.textBoxPath.Size = new System.Drawing.Size(460, 21);
            this.textBoxPath.TabIndex = 0;
            // 
            // buttonBrowse
            // 
            this.buttonBrowse.Location = new System.Drawing.Point(466, 3);
            this.buttonBrowse.Name = "buttonBrowse";
            this.buttonBrowse.Size = new System.Drawing.Size(75, 23);
            this.buttonBrowse.TabIndex = 1;
            this.buttonBrowse.Text = "Browse...";
            this.buttonBrowse.UseVisualStyleBackColor = true;
            this.buttonBrowse.Click += new System.EventHandler(this.ButtonBrowse_Click);
            // 
            // checkBoxRecursive
            // 
            this.checkBoxRecursive.AutoSize = true;
            this.checkBoxRecursive.Checked = true;
            this.checkBoxRecursive.CheckState = System.Windows.Forms.CheckState.Checked;
            this.checkBoxRecursive.Location = new System.Drawing.Point(547, 7);
            this.checkBoxRecursive.Margin = new System.Windows.Forms.Padding(3, 7, 3, 3);
            this.checkBoxRecursive.Name = "checkBoxRecursive";
            this.checkBoxRecursive.Size = new System.Drawing.Size(132, 16);
            this.checkBoxRecursive.TabIndex = 2;
            this.checkBoxRecursive.Text = "Include subfolders";
            this.checkBoxRecursive.UseVisualStyleBackColor = true;
            // 
            // checkBoxParseCharactersInScenes
            // 
            this.checkBoxParseCharactersInScenes.AutoSize = true;
            this.checkBoxParseCharactersInScenes.Location = new System.Drawing.Point(685, 7);
            this.checkBoxParseCharactersInScenes.Margin = new System.Windows.Forms.Padding(3, 7, 3, 3);
            this.checkBoxParseCharactersInScenes.Name = "checkBoxParseCharactersInScenes";
            this.checkBoxParseCharactersInScenes.Size = new System.Drawing.Size(180, 16);
            this.checkBoxParseCharactersInScenes.TabIndex = 3;
            this.checkBoxParseCharactersInScenes.Text = "Parse characters in scenes";
            this.checkBoxParseCharactersInScenes.UseVisualStyleBackColor = true;
            // 
            // flowLayoutPanelAction
            // 
            this.flowLayoutPanelAction.AutoSize = true;
            this.flowLayoutPanelAction.Controls.Add(this.buttonScan);
            this.flowLayoutPanelAction.Controls.Add(this.buttonCancel);
            this.flowLayoutPanelAction.Controls.Add(this.progressBar);
            this.flowLayoutPanelAction.Controls.Add(this.labelStatus);
            this.flowLayoutPanelAction.Dock = System.Windows.Forms.DockStyle.Fill;
            this.flowLayoutPanelAction.Location = new System.Drawing.Point(9, 96);
            this.flowLayoutPanelAction.Name = "flowLayoutPanelAction";
            this.flowLayoutPanelAction.Size = new System.Drawing.Size(883, 29);
            this.flowLayoutPanelAction.TabIndex = 3;
            this.flowLayoutPanelAction.WrapContents = false;
            // 
            // buttonScan
            // 
            this.buttonScan.Location = new System.Drawing.Point(0, 3);
            this.buttonScan.Margin = new System.Windows.Forms.Padding(0, 3, 3, 3);
            this.buttonScan.Name = "buttonScan";
            this.buttonScan.Size = new System.Drawing.Size(75, 23);
            this.buttonScan.TabIndex = 0;
            this.buttonScan.Text = "Scan";
            this.buttonScan.UseVisualStyleBackColor = true;
            this.buttonScan.Click += new System.EventHandler(this.ButtonScan_Click);
            // 
            // buttonCancel
            // 
            this.buttonCancel.Enabled = false;
            this.buttonCancel.Location = new System.Drawing.Point(81, 3);
            this.buttonCancel.Name = "buttonCancel";
            this.buttonCancel.Size = new System.Drawing.Size(75, 23);
            this.buttonCancel.TabIndex = 1;
            this.buttonCancel.Text = "Cancel";
            this.buttonCancel.UseVisualStyleBackColor = true;
            this.buttonCancel.Click += new System.EventHandler(this.ButtonCancel_Click);
            // 
            // progressBar
            // 
            this.progressBar.Location = new System.Drawing.Point(162, 4);
            this.progressBar.Margin = new System.Windows.Forms.Padding(3, 4, 3, 3);
            this.progressBar.Name = "progressBar";
            this.progressBar.Size = new System.Drawing.Size(250, 21);
            this.progressBar.TabIndex = 2;
            // 
            // labelStatus
            // 
            this.labelStatus.AutoSize = true;
            this.labelStatus.Location = new System.Drawing.Point(418, 8);
            this.labelStatus.Margin = new System.Windows.Forms.Padding(3, 8, 3, 0);
            this.labelStatus.Name = "labelStatus";
            this.labelStatus.Size = new System.Drawing.Size(35, 12);
            this.labelStatus.TabIndex = 3;
            this.labelStatus.Text = "Ready";
            // 
            // dataGridView
            // 
            this.dataGridView.AllowUserToAddRows = false;
            this.dataGridView.AllowUserToDeleteRows = false;
            this.dataGridView.AllowUserToResizeRows = false;
            this.dataGridView.BackgroundColor = System.Drawing.SystemColors.Window;
            this.dataGridView.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridView.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.columnFileName,
            this.columnPath,
            this.columnName,
            this.columnGame,
            this.columnType,
            this.columnPlugins,
            this.columnZipmods,
            this.columnStatus});
            this.dataGridView.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dataGridView.Location = new System.Drawing.Point(9, 131);
            this.dataGridView.MultiSelect = false;
            this.dataGridView.Name = "dataGridView";
            this.dataGridView.ReadOnly = true;
            this.dataGridView.RowHeadersVisible = false;
            this.dataGridView.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dataGridView.Size = new System.Drawing.Size(883, 341);
            this.dataGridView.TabIndex = 4;
            this.dataGridView.SelectionChanged += new System.EventHandler(this.DataGridView_SelectionChanged);
            // 
            // columnFileName
            // 
            this.columnFileName.HeaderText = "File name";
            this.columnFileName.Name = "columnFileName";
            this.columnFileName.ReadOnly = true;
            this.columnFileName.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Programmatic;
            this.columnFileName.Width = 200;
            // 
            // columnPath
            // 
            this.columnPath.HeaderText = "Path";
            this.columnPath.Name = "columnPath";
            this.columnPath.ReadOnly = true;
            this.columnPath.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Programmatic;
            this.columnPath.Width = 340;
            // 
            // columnName
            // 
            this.columnName.HeaderText = "Name";
            this.columnName.Name = "columnName";
            this.columnName.ReadOnly = true;
            this.columnName.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Programmatic;
            this.columnName.Width = 150;
            // 
            // columnGame
            // 
            this.columnGame.HeaderText = "Game";
            this.columnGame.Name = "columnGame";
            this.columnGame.ReadOnly = true;
            this.columnGame.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Programmatic;
            this.columnGame.Width = 110;
            // 
            // columnType
            // 
            this.columnType.HeaderText = "Type";
            this.columnType.Name = "columnType";
            this.columnType.ReadOnly = true;
            this.columnType.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Programmatic;
            this.columnType.Width = 90;
            // 
            // columnPlugins
            // 
            this.columnPlugins.HeaderText = "Plugins";
            this.columnPlugins.Name = "columnPlugins";
            this.columnPlugins.ReadOnly = true;
            this.columnPlugins.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Programmatic;
            this.columnPlugins.Width = 70;
            // 
            // columnZipmods
            // 
            this.columnZipmods.HeaderText = "Zipmods";
            this.columnZipmods.Name = "columnZipmods";
            this.columnZipmods.ReadOnly = true;
            this.columnZipmods.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Programmatic;
            this.columnZipmods.Width = 70;
            // 
            // columnStatus
            // 
            this.columnStatus.HeaderText = "Status";
            this.columnStatus.Name = "columnStatus";
            this.columnStatus.ReadOnly = true;
            this.columnStatus.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Programmatic;
            // 
            // flowLayoutPanelMode
            // 
            this.flowLayoutPanelMode.AutoSize = true;
            this.flowLayoutPanelMode.Controls.Add(this.radioButtonSelected);
            this.flowLayoutPanelMode.Controls.Add(this.radioButtonAll);
            this.flowLayoutPanelMode.Controls.Add(this.buttonExport);
            this.flowLayoutPanelMode.Dock = System.Windows.Forms.DockStyle.Fill;
            this.flowLayoutPanelMode.Location = new System.Drawing.Point(9, 478);
            this.flowLayoutPanelMode.Name = "flowLayoutPanelMode";
            this.flowLayoutPanelMode.Size = new System.Drawing.Size(883, 29);
            this.flowLayoutPanelMode.TabIndex = 5;
            this.flowLayoutPanelMode.WrapContents = false;
            // 
            // radioButtonSelected
            // 
            this.radioButtonSelected.AutoSize = true;
            this.radioButtonSelected.Checked = true;
            this.radioButtonSelected.Location = new System.Drawing.Point(3, 6);
            this.radioButtonSelected.Margin = new System.Windows.Forms.Padding(3, 6, 3, 3);
            this.radioButtonSelected.Name = "radioButtonSelected";
            this.radioButtonSelected.Size = new System.Drawing.Size(101, 16);
            this.radioButtonSelected.TabIndex = 0;
            this.radioButtonSelected.TabStop = true;
            this.radioButtonSelected.Text = "Selected card";
            this.radioButtonSelected.UseVisualStyleBackColor = true;
            this.radioButtonSelected.CheckedChanged += new System.EventHandler(this.ViewMode_CheckedChanged);
            // 
            // radioButtonAll
            // 
            this.radioButtonAll.AutoSize = true;
            this.radioButtonAll.Location = new System.Drawing.Point(110, 6);
            this.radioButtonAll.Margin = new System.Windows.Forms.Padding(3, 6, 3, 3);
            this.radioButtonAll.Name = "radioButtonAll";
            this.radioButtonAll.Size = new System.Drawing.Size(89, 16);
            this.radioButtonAll.TabIndex = 1;
            this.radioButtonAll.Text = "All results";
            this.radioButtonAll.UseVisualStyleBackColor = true;
            this.radioButtonAll.CheckedChanged += new System.EventHandler(this.ViewMode_CheckedChanged);
            // 
            // buttonExport
            // 
            this.buttonExport.Enabled = false;
            this.buttonExport.Location = new System.Drawing.Point(205, 3);
            this.buttonExport.Name = "buttonExport";
            this.buttonExport.Size = new System.Drawing.Size(90, 23);
            this.buttonExport.TabIndex = 2;
            this.buttonExport.Text = "Export JSON";
            this.buttonExport.UseVisualStyleBackColor = true;
            this.buttonExport.Click += new System.EventHandler(this.ButtonExport_Click);
            // 
            // treeViewDetails
            // 
            this.treeViewDetails.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.treeViewDetails.Dock = System.Windows.Forms.DockStyle.Fill;
            this.treeViewDetails.FullRowSelect = true;
            this.treeViewDetails.HideSelection = false;
            this.treeViewDetails.Location = new System.Drawing.Point(9, 513);
            this.treeViewDetails.Name = "treeViewDetails";
            this.treeViewDetails.Size = new System.Drawing.Size(883, 278);
            this.treeViewDetails.TabIndex = 6;
            // 
            // MainForm
            // 
            this.ClientSize = new System.Drawing.Size(901, 800);
            this.Controls.Add(this.tableLayoutPanelMain);
            this.MinimumSize = new System.Drawing.Size(800, 600);
            this.Name = "MainForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "KoikatsuCardReader";
            this.tableLayoutPanelMain.ResumeLayout(false);
            this.tableLayoutPanelMain.PerformLayout();
            this.flowLayoutPanelZipmodMap.ResumeLayout(false);
            this.flowLayoutPanelZipmodMap.PerformLayout();
            this.flowLayoutPanelPath.ResumeLayout(false);
            this.flowLayoutPanelPath.PerformLayout();
            this.flowLayoutPanelAction.ResumeLayout(false);
            this.flowLayoutPanelAction.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView)).EndInit();
            this.flowLayoutPanelMode.ResumeLayout(false);
            this.flowLayoutPanelMode.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel tableLayoutPanelMain;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanelZipmodMap;
        private System.Windows.Forms.Button buttonBuildZipmodMap;
        private System.Windows.Forms.Label labelZipmodMap;
        private System.Windows.Forms.Button buttonOpenZipmodMap;
        private System.Windows.Forms.Panel panelSeparator;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanelPath;
        private System.Windows.Forms.TextBox textBoxPath;
        private System.Windows.Forms.Button buttonBrowse;
        private System.Windows.Forms.CheckBox checkBoxRecursive;
        private System.Windows.Forms.CheckBox checkBoxParseCharactersInScenes;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanelAction;
        private System.Windows.Forms.Button buttonScan;
        private System.Windows.Forms.Button buttonCancel;
        private System.Windows.Forms.Button buttonExport;
        private System.Windows.Forms.ProgressBar progressBar;
        private System.Windows.Forms.Label labelStatus;
        private System.Windows.Forms.DataGridView dataGridView;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanelMode;
        private System.Windows.Forms.RadioButton radioButtonSelected;
        private System.Windows.Forms.RadioButton radioButtonAll;
        private System.Windows.Forms.TreeView treeViewDetails;
        private System.Windows.Forms.DataGridViewTextBoxColumn columnFileName;
        private System.Windows.Forms.DataGridViewTextBoxColumn columnPath;
        private System.Windows.Forms.DataGridViewTextBoxColumn columnName;
        private System.Windows.Forms.DataGridViewTextBoxColumn columnGame;
        private System.Windows.Forms.DataGridViewTextBoxColumn columnType;
        private System.Windows.Forms.DataGridViewTextBoxColumn columnPlugins;
        private System.Windows.Forms.DataGridViewTextBoxColumn columnZipmods;
        private System.Windows.Forms.DataGridViewTextBoxColumn columnStatus;
    }
}
