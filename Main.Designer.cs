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
            pctDebug = new PictureBox();
            lblDebugTitle = new Label();
            lblDebug = new Label();
            lblTutorial1 = new Label();
            lblTutorial2 = new Label();
            lblTutorial3 = new Label();
            lblTutorial4 = new Label();
            ((System.ComponentModel.ISupportInitialize)pctDebug).BeginInit();
            SuspendLayout();
            // 
            // pctDebug
            // 
            pctDebug.Location = new Point(12, 108);
            pctDebug.Name = "pctDebug";
            pctDebug.Size = new Size(374, 57);
            pctDebug.SizeMode = PictureBoxSizeMode.Zoom;
            pctDebug.TabIndex = 0;
            pctDebug.TabStop = false;
            // 
            // lblDebugTitle
            // 
            lblDebugTitle.AutoSize = true;
            lblDebugTitle.Location = new Point(12, 186);
            lblDebugTitle.Name = "lblDebugTitle";
            lblDebugTitle.Size = new Size(95, 15);
            lblDebugTitle.TabIndex = 1;
            lblDebugTitle.Text = "Debug OCR text:";
            // 
            // lblDebug
            // 
            lblDebug.AutoSize = true;
            lblDebug.Location = new Point(113, 186);
            lblDebug.Name = "lblDebug";
            lblDebug.Size = new Size(43, 15);
            lblDebug.TabIndex = 2;
            lblDebug.Text = "- text -";
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
            // Main
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(406, 218);
            Controls.Add(lblTutorial4);
            Controls.Add(lblTutorial3);
            Controls.Add(lblTutorial2);
            Controls.Add(lblTutorial1);
            Controls.Add(lblDebug);
            Controls.Add(lblDebugTitle);
            Controls.Add(pctDebug);
            Name = "Main";
            Text = "Main";
            Load += Main_Load;
            ((System.ComponentModel.ISupportInitialize)pctDebug).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private PictureBox pctDebug;
        private Label lblDebugTitle;
        private Label lblDebug;
        private Label lblTutorial1;
        private Label lblTutorial2;
        private Label lblTutorial3;
        private Label lblTutorial4;
    }
}
