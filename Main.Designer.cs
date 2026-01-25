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
            SuspendLayout();
            // 
            // lblTutorial1
            // 
            lblTutorial1.AutoSize = true;
            lblTutorial1.Location = new Point(12, 9);
            lblTutorial1.Name = "lblTutorial1";
            lblTutorial1.Size = new Size(314, 15);
            lblTutorial1.TabIndex = 3;
            lblTutorial1.Text = "- Open currency exchange and your inventory (important)";
            // 
            // lblTutorial2
            // 
            lblTutorial2.AutoSize = true;
            lblTutorial2.Location = new Point(12, 33);
            lblTutorial2.Name = "lblTutorial2";
            lblTutorial2.Size = new Size(341, 15);
            lblTutorial2.TabIndex = 4;
            lblTutorial2.Text = "- Press ctrl + N to start script (or ctrl + 0 to just test one sample)";
            // 
            // lblTutorial3
            // 
            lblTutorial3.AutoSize = true;
            lblTutorial3.Location = new Point(12, 57);
            lblTutorial3.Name = "lblTutorial3";
            lblTutorial3.Size = new Size(156, 15);
            lblTutorial3.TabIndex = 5;
            lblTutorial3.Text = "- AFK, go do something else";
            // 
            // lblTutorial4
            // 
            lblTutorial4.AutoSize = true;
            lblTutorial4.Location = new Point(12, 81);
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
            flpDebug.Size = new Size(305, 665);
            flpDebug.TabIndex = 7;
            flpDebug.WrapContents = false;
            // 
            // lblTutorial5
            // 
            lblTutorial5.AutoSize = true;
            lblTutorial5.Location = new Point(12, 107);
            lblTutorial5.Name = "lblTutorial5";
            lblTutorial5.Size = new Size(147, 15);
            lblTutorial5.TabIndex = 8;
            lblTutorial5.Text = "- OCR history shown ---->";
            // 
            // lblTutorial6
            // 
            lblTutorial6.AutoSize = true;
            lblTutorial6.Location = new Point(12, 135);
            lblTutorial6.Name = "lblTutorial6";
            lblTutorial6.Size = new Size(293, 15);
            lblTutorial6.TabIndex = 9;
            lblTutorial6.Text = "- Press \"Wrong\" will save the image in OCRError folder";
            // 
            // Main
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(865, 686);
            Controls.Add(lblTutorial6);
            Controls.Add(lblTutorial5);
            Controls.Add(flpDebug);
            Controls.Add(lblTutorial4);
            Controls.Add(lblTutorial3);
            Controls.Add(lblTutorial2);
            Controls.Add(lblTutorial1);
            Name = "Main";
            Text = "2";
            Load += Main_Load;
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
    }
}
