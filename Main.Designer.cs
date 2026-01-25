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
            lblTutorial1 = new Label();
            lblTutorial2 = new Label();
            lblTutorial3 = new Label();
            lblTutorial4 = new Label();
            flpDebug = new FlowLayoutPanel();
            lblTutorial5 = new Label();
            lblTutorial6 = new Label();
            lblPOEChoosed = new Label();
            panel1 = new Panel();
            chkCheckChaos = new CheckBox();
            chkCheckExalt = new CheckBox();
            btnClearDebug = new Button();
            panel1.SuspendLayout();
            SuspendLayout();
            // 
            // lblTutorial1
            // 
            lblTutorial1.AutoSize = true;
            lblTutorial1.Location = new Point(13, 10);
            lblTutorial1.Name = "lblTutorial1";
            lblTutorial1.Size = new Size(314, 15);
            lblTutorial1.TabIndex = 3;
            lblTutorial1.Text = "- Open currency exchange and your inventory (important)";
            // 
            // lblTutorial2
            // 
            lblTutorial2.AutoSize = true;
            lblTutorial2.Location = new Point(13, 34);
            lblTutorial2.Name = "lblTutorial2";
            lblTutorial2.Size = new Size(341, 15);
            lblTutorial2.TabIndex = 4;
            lblTutorial2.Text = "- Press ctrl + N to start script (or ctrl + 0 to just test one sample)";
            // 
            // lblTutorial3
            // 
            lblTutorial3.AutoSize = true;
            lblTutorial3.Location = new Point(13, 58);
            lblTutorial3.Name = "lblTutorial3";
            lblTutorial3.Size = new Size(156, 15);
            lblTutorial3.TabIndex = 5;
            lblTutorial3.Text = "- AFK, go do something else";
            // 
            // lblTutorial4
            // 
            lblTutorial4.AutoSize = true;
            lblTutorial4.Location = new Point(13, 82);
            lblTutorial4.Name = "lblTutorial4";
            lblTutorial4.Size = new Size(226, 15);
            lblTutorial4.TabIndex = 6;
            lblTutorial4.Text = "- Press ctrl + N again to immediately stop";
            // 
            // flpDebug
            // 
            flpDebug.AutoScroll = true;
            flpDebug.BorderStyle = BorderStyle.Fixed3D;
            flpDebug.FlowDirection = FlowDirection.TopDown;
            flpDebug.Location = new Point(550, 9);
            flpDebug.Name = "flpDebug";
            flpDebug.Size = new Size(305, 619);
            flpDebug.TabIndex = 7;
            flpDebug.WrapContents = false;
            // 
            // lblTutorial5
            // 
            lblTutorial5.AutoSize = true;
            lblTutorial5.Location = new Point(13, 108);
            lblTutorial5.Name = "lblTutorial5";
            lblTutorial5.Size = new Size(147, 15);
            lblTutorial5.TabIndex = 8;
            lblTutorial5.Text = "- OCR history shown ---->";
            // 
            // lblTutorial6
            // 
            lblTutorial6.AutoSize = true;
            lblTutorial6.Location = new Point(13, 136);
            lblTutorial6.Name = "lblTutorial6";
            lblTutorial6.Size = new Size(293, 15);
            lblTutorial6.TabIndex = 9;
            lblTutorial6.Text = "- Press \"Wrong\" will save the image in OCRError folder";
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
            // panel1
            // 
            panel1.BorderStyle = BorderStyle.FixedSingle;
            panel1.Controls.Add(lblTutorial1);
            panel1.Controls.Add(lblTutorial2);
            panel1.Controls.Add(lblTutorial6);
            panel1.Controls.Add(lblTutorial3);
            panel1.Controls.Add(lblTutorial5);
            panel1.Controls.Add(lblTutorial4);
            panel1.Location = new Point(12, 62);
            panel1.Name = "panel1";
            panel1.Size = new Size(365, 166);
            panel1.TabIndex = 11;
            // 
            // chkCheckChaos
            // 
            chkCheckChaos.AutoSize = true;
            chkCheckChaos.Checked = true;
            chkCheckChaos.CheckState = CheckState.Checked;
            chkCheckChaos.Location = new Point(383, 62);
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
            chkCheckExalt.Location = new Point(383, 87);
            chkCheckExalt.Name = "chkCheckExalt";
            chkCheckExalt.Size = new Size(131, 19);
            chkCheckExalt.TabIndex = 13;
            chkCheckExalt.Text = "Check Exalt <-> Div";
            chkCheckExalt.UseVisualStyleBackColor = true;
            // 
            // btnClearDebug
            // 
            btnClearDebug.ForeColor = Color.Black;
            btnClearDebug.Location = new Point(780, 638);
            btnClearDebug.Name = "btnClearDebug";
            btnClearDebug.Size = new Size(75, 36);
            btnClearDebug.TabIndex = 14;
            btnClearDebug.Text = "Clear all";
            btnClearDebug.UseVisualStyleBackColor = true;
            btnClearDebug.Click += btnClearDebug_Click;
            // 
            // Main
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(865, 685);
            Controls.Add(btnClearDebug);
            Controls.Add(chkCheckExalt);
            Controls.Add(chkCheckChaos);
            Controls.Add(panel1);
            Controls.Add(lblPOEChoosed);
            Controls.Add(flpDebug);
            Name = "Main";
            Text = "POE Flip tool";
            Load += Main_Load;
            panel1.ResumeLayout(false);
            panel1.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion
        private Label lblTutorial1;
        private Label lblTutorial2;
        private Label lblTutorial3;
        private Label lblTutorial4;
        private FlowLayoutPanel flpDebug;
        private Label lblTutorial5;
        private Label lblTutorial6;
        private Label lblPOEChoosed;
        private Panel panel1;
        private CheckBox chkCheckChaos;
        private CheckBox chkCheckExalt;
        private Button btnClearDebug;
    }
}
