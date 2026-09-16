namespace POE2FlipTool
{
    partial class Main
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            flpDebug = new FlowLayoutPanel();
            lblPOEChoosed = new Label();
            chkCheckChaos = new CheckBox();
            chkCheckExalt = new CheckBox();
            btnClearDebug = new Button();
            grpCategories = new GroupBox();
            btnCategoriesReload = new Button();
            btnCategoriesNone = new Button();
            btnCategoriesAll = new Button();
            flpCategories = new FlowLayoutPanel();
            dgvPrices = new DataGridView();
            chkShowProfitPerDiv = new CheckBox();
            btnResetSort = new Button();
            cmbLeague = new ComboBox();
            chkShowTradeVolume = new CheckBox();
            lblDivToEx = new Label();
            txtDivToEx = new TextBox();
            lblDivToChaos = new Label();
            txtDivToChaos = new TextBox();
            lblTutorial4 = new Label();
            lblTutorial2 = new Label();
            lblTutorial1 = new Label();
            grpGuide = new GroupBox();
            grpOCRHistory = new GroupBox();
            grpCategories.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dgvPrices).BeginInit();
            grpGuide.SuspendLayout();
            grpOCRHistory.SuspendLayout();
            SuspendLayout();
            // 
            // flpDebug
            // 
            flpDebug.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Right;
            flpDebug.AutoScroll = true;
            flpDebug.BorderStyle = BorderStyle.Fixed3D;
            flpDebug.FlowDirection = FlowDirection.TopDown;
            flpDebug.Location = new Point(6, 22);
            flpDebug.Name = "flpDebug";
            flpDebug.Size = new Size(246, 264);
            flpDebug.TabIndex = 7;
            flpDebug.WrapContents = false;
            // 
            // lblPOEChoosed
            // 
            lblPOEChoosed.AutoSize = true;
            lblPOEChoosed.Font = new Font("Segoe UI", 20.25F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblPOEChoosed.Location = new Point(12, 9);
            lblPOEChoosed.Name = "lblPOEChoosed";
            lblPOEChoosed.Size = new Size(91, 37);
            lblPOEChoosed.TabIndex = 10;
            lblPOEChoosed.Text = "POE 1";
            // 
            // chkCheckChaos
            // 
            chkCheckChaos.AutoSize = true;
            chkCheckChaos.Checked = true;
            chkCheckChaos.CheckState = CheckState.Checked;
            chkCheckChaos.Location = new Point(12, 228);
            chkCheckChaos.Name = "chkCheckChaos";
            chkCheckChaos.Size = new Size(139, 19);
            chkCheckChaos.TabIndex = 12;
            chkCheckChaos.Text = "Check Chaos <-> Div";
            chkCheckChaos.UseVisualStyleBackColor = true;
            // 
            // chkCheckExalt
            // 
            chkCheckExalt.AutoSize = true;
            chkCheckExalt.Checked = true;
            chkCheckExalt.CheckState = CheckState.Checked;
            chkCheckExalt.Location = new Point(12, 203);
            chkCheckExalt.Name = "chkCheckExalt";
            chkCheckExalt.Size = new Size(131, 19);
            chkCheckExalt.TabIndex = 13;
            chkCheckExalt.Text = "Check Exalt <-> Div";
            chkCheckExalt.UseVisualStyleBackColor = true;
            // 
            // btnClearDebug
            // 
            btnClearDebug.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            btnClearDebug.ForeColor = Color.Black;
            btnClearDebug.Location = new Point(177, 292);
            btnClearDebug.Name = "btnClearDebug";
            btnClearDebug.Size = new Size(75, 36);
            btnClearDebug.TabIndex = 14;
            btnClearDebug.Text = "Clear all";
            btnClearDebug.UseVisualStyleBackColor = true;
            btnClearDebug.Click += btnClearDebug_Click;
            // 
            // grpCategories
            // 
            grpCategories.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left;
            grpCategories.Controls.Add(btnCategoriesReload);
            grpCategories.Controls.Add(btnCategoriesNone);
            grpCategories.Controls.Add(btnCategoriesAll);
            grpCategories.Controls.Add(flpCategories);
            grpCategories.Location = new Point(12, 259);
            grpCategories.Name = "grpCategories";
            grpCategories.Size = new Size(264, 294);
            grpCategories.TabIndex = 15;
            grpCategories.TabStop = false;
            grpCategories.Text = "Categories to scan";
            // 
            // btnCategoriesReload
            // 
            btnCategoriesReload.Location = new Point(183, 22);
            btnCategoriesReload.Name = "btnCategoriesReload";
            btnCategoriesReload.Size = new Size(75, 23);
            btnCategoriesReload.TabIndex = 3;
            btnCategoriesReload.Text = "Reload";
            btnCategoriesReload.UseVisualStyleBackColor = true;
            btnCategoriesReload.Click += btnCategoriesReload_Click;
            // 
            // btnCategoriesNone
            // 
            btnCategoriesNone.Location = new Point(87, 22);
            btnCategoriesNone.Name = "btnCategoriesNone";
            btnCategoriesNone.Size = new Size(90, 23);
            btnCategoriesNone.TabIndex = 2;
            btnCategoriesNone.Text = "Select none";
            btnCategoriesNone.UseVisualStyleBackColor = true;
            btnCategoriesNone.Click += btnCategoriesNone_Click;
            // 
            // btnCategoriesAll
            // 
            btnCategoriesAll.Location = new Point(6, 22);
            btnCategoriesAll.Name = "btnCategoriesAll";
            btnCategoriesAll.Size = new Size(75, 23);
            btnCategoriesAll.TabIndex = 1;
            btnCategoriesAll.Text = "Select all";
            btnCategoriesAll.UseVisualStyleBackColor = true;
            btnCategoriesAll.Click += btnCategoriesAll_Click;
            // 
            // flpCategories
            // 
            flpCategories.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            flpCategories.AutoScroll = true;
            flpCategories.BorderStyle = BorderStyle.FixedSingle;
            flpCategories.FlowDirection = FlowDirection.TopDown;
            flpCategories.Location = new Point(6, 51);
            flpCategories.Name = "flpCategories";
            flpCategories.Size = new Size(252, 236);
            flpCategories.TabIndex = 0;
            flpCategories.WrapContents = false;
            // 
            // dgvPrices
            // 
            dgvPrices.AllowUserToAddRows = false;
            dgvPrices.AllowUserToDeleteRows = false;
            dgvPrices.AllowUserToResizeRows = false;
            dgvPrices.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            dgvPrices.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvPrices.Location = new Point(300, 12);
            dgvPrices.MultiSelect = false;
            dgvPrices.Name = "dgvPrices";
            dgvPrices.RowHeadersVisible = false;
            dgvPrices.SelectionMode = DataGridViewSelectionMode.CellSelect;
            dgvPrices.Size = new Size(1267, 850);
            dgvPrices.TabIndex = 17;
            // 
            // lblTutorial4
            // 
            lblTutorial4.Location = new Point(6, 109);
            lblTutorial4.Name = "lblTutorial4";
            lblTutorial4.Size = new Size(226, 15);
            lblTutorial4.TabIndex = 6;
            lblTutorial4.Text = "- Press ctrl + N again to immediately stop";
            // 
            // lblTutorial2
            // 
            lblTutorial2.Location = new Point(6, 68);
            lblTutorial2.Name = "lblTutorial2";
            lblTutorial2.Size = new Size(234, 41);
            lblTutorial2.TabIndex = 4;
            lblTutorial2.Text = "- Press ctrl + N to start script (or ctrl + 0 to just test current reading)";
            // 
            // lblTutorial1
            // 
            lblTutorial1.Location = new Point(6, 21);
            lblTutorial1.Name = "lblTutorial1";
            lblTutorial1.Size = new Size(226, 33);
            lblTutorial1.TabIndex = 3;
            lblTutorial1.Text = "- Open currency exchange alone. Must close all other windows. Make sure currency exchange is at the center.";
            // 
            // grpGuide
            // 
            grpGuide.Controls.Add(lblTutorial1);
            grpGuide.Controls.Add(lblTutorial2);
            grpGuide.Controls.Add(lblTutorial4);
            grpGuide.Location = new Point(12, 49);
            grpGuide.Name = "grpGuide";
            grpGuide.Size = new Size(264, 138);
            grpGuide.TabIndex = 19;
            grpGuide.TabStop = false;
            grpGuide.Text = "Read me";
            // 
            // grpOCRHistory
            // 
            grpOCRHistory.Controls.Add(flpDebug);
            grpOCRHistory.Controls.Add(btnClearDebug);
            grpOCRHistory.Location = new Point(18, 576);
            grpOCRHistory.Name = "grpOCRHistory";
            grpOCRHistory.Size = new Size(258, 334);
            grpOCRHistory.TabIndex = 20;
            grpOCRHistory.TabStop = false;
            grpOCRHistory.Text = "OCR History";
            //
            // chkShowProfitPerDiv
            //
            chkShowProfitPerDiv.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            chkShowProfitPerDiv.AutoSize = true;
            chkShowProfitPerDiv.Location = new Point(1330, 875);
            chkShowProfitPerDiv.Name = "chkShowProfitPerDiv";
            chkShowProfitPerDiv.Size = new Size(131, 19);
            chkShowProfitPerDiv.TabIndex = 18;
            chkShowProfitPerDiv.Text = "Show profit per div";
            chkShowProfitPerDiv.UseVisualStyleBackColor = true;
            chkShowProfitPerDiv.CheckedChanged += chkShowProfitPerDiv_CheckedChanged;
            //
            // btnResetSort
            //
            btnResetSort.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            btnResetSort.Location = new Point(1467, 870);
            btnResetSort.Name = "btnResetSort";
            btnResetSort.Size = new Size(100, 27);
            btnResetSort.TabIndex = 19;
            btnResetSort.Text = "Reset sorting";
            btnResetSort.UseVisualStyleBackColor = true;
            btnResetSort.Click += btnResetSort_Click;
            //
            // cmbLeague
            //
            cmbLeague.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbLeague.FormattingEnabled = true;
            cmbLeague.Location = new Point(109, 17);
            cmbLeague.Name = "cmbLeague";
            cmbLeague.Size = new Size(167, 23);
            cmbLeague.TabIndex = 20;
            cmbLeague.SelectedIndexChanged += cmbLeague_SelectedIndexChanged;
            //
            // lblDivToEx
            //
            lblDivToEx.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            lblDivToEx.AutoSize = true;
            lblDivToEx.Location = new Point(300, 876);
            lblDivToEx.Name = "lblDivToEx";
            lblDivToEx.Size = new Size(64, 15);
            lblDivToEx.TabIndex = 21;
            lblDivToEx.Text = "Div -> Ex:";
            //
            // txtDivToEx
            //
            txtDivToEx.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            txtDivToEx.Location = new Point(366, 872);
            txtDivToEx.Name = "txtDivToEx";
            txtDivToEx.Size = new Size(70, 23);
            txtDivToEx.TabIndex = 22;
            txtDivToEx.TextAlign = HorizontalAlignment.Right;
            txtDivToEx.KeyDown += txtRate_KeyDown;
            txtDivToEx.Validated += txtDivToEx_Validated;
            //
            // lblDivToChaos
            //
            lblDivToChaos.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            lblDivToChaos.AutoSize = true;
            lblDivToChaos.Location = new Point(452, 876);
            lblDivToChaos.Name = "lblDivToChaos";
            lblDivToChaos.Size = new Size(83, 15);
            lblDivToChaos.TabIndex = 23;
            lblDivToChaos.Text = "Div -> Chaos:";
            //
            // txtDivToChaos
            //
            txtDivToChaos.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            txtDivToChaos.Location = new Point(537, 872);
            txtDivToChaos.Name = "txtDivToChaos";
            txtDivToChaos.Size = new Size(70, 23);
            txtDivToChaos.TabIndex = 24;
            txtDivToChaos.TextAlign = HorizontalAlignment.Right;
            txtDivToChaos.KeyDown += txtRate_KeyDown;
            txtDivToChaos.Validated += txtDivToChaos_Validated;
            //
            // chkShowTradeVolume
            //
            chkShowTradeVolume.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            chkShowTradeVolume.AutoSize = true;
            chkShowTradeVolume.Location = new Point(1190, 875);
            chkShowTradeVolume.Name = "chkShowTradeVolume";
            chkShowTradeVolume.Size = new Size(126, 19);
            chkShowTradeVolume.TabIndex = 25;
            chkShowTradeVolume.Text = "Show trade volume";
            chkShowTradeVolume.UseVisualStyleBackColor = true;
            chkShowTradeVolume.CheckedChanged += chkShowTradeVolume_CheckedChanged;
            //
            // Main
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1579, 920);
            Controls.Add(grpOCRHistory);
            Controls.Add(grpGuide);
            Controls.Add(chkShowTradeVolume);
            Controls.Add(cmbLeague);
            Controls.Add(lblDivToEx);
            Controls.Add(txtDivToEx);
            Controls.Add(lblDivToChaos);
            Controls.Add(txtDivToChaos);
            Controls.Add(btnResetSort);
            Controls.Add(chkShowProfitPerDiv);
            Controls.Add(dgvPrices);
            Controls.Add(grpCategories);
            Controls.Add(chkCheckExalt);
            Controls.Add(chkCheckChaos);
            Controls.Add(lblPOEChoosed);
            Name = "Main";
            Text = "POE Flip tool";
            Load += Main_Load;
            grpCategories.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)dgvPrices).EndInit();
            grpGuide.ResumeLayout(false);
            grpOCRHistory.ResumeLayout(false);
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion
        private FlowLayoutPanel flpDebug;
        private Label lblPOEChoosed;
        private CheckBox chkCheckChaos;
        private CheckBox chkCheckExalt;
        private Button btnClearDebug;
        private GroupBox grpCategories;
        private FlowLayoutPanel flpCategories;
        private Button btnCategoriesAll;
        private Button btnCategoriesNone;
        private Button btnCategoriesReload;
        private DataGridView dgvPrices;
        private CheckBox chkShowProfitPerDiv;
        private Button btnResetSort;
        private ComboBox cmbLeague;
        private CheckBox chkShowTradeVolume;
        private Label lblDivToEx;
        private TextBox txtDivToEx;
        private Label lblDivToChaos;
        private TextBox txtDivToChaos;
        private Label lblPrices;
        private Label lblTutorial4;
        private Label lblTutorial2;
        private Label lblTutorial1;
        private GroupBox grpGuide;
        private GroupBox grpOCRHistory;
    }
}
