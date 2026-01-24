namespace POE2FlipTool
{
    partial class OCRDebug
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

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            pctOCRDebug = new PictureBox();
            btnCorrect = new Button();
            btnWrong = new Button();
            lblOCRDebug = new Label();
            ((System.ComponentModel.ISupportInitialize)pctOCRDebug).BeginInit();
            SuspendLayout();
            // 
            // pctOCRDebug
            // 
            pctOCRDebug.BorderStyle = BorderStyle.FixedSingle;
            pctOCRDebug.Location = new Point(8, 9);
            pctOCRDebug.Name = "pctOCRDebug";
            pctOCRDebug.Size = new Size(244, 96);
            pctOCRDebug.SizeMode = PictureBoxSizeMode.CenterImage;
            pctOCRDebug.TabIndex = 0;
            pctOCRDebug.TabStop = false;
            // 
            // btnCorrect
            // 
            btnCorrect.BackColor = Color.FromArgb(192, 255, 192);
            btnCorrect.Location = new Point(258, 63);
            btnCorrect.Name = "btnCorrect";
            btnCorrect.Size = new Size(74, 42);
            btnCorrect.TabIndex = 2;
            btnCorrect.Text = "Correct";
            btnCorrect.UseVisualStyleBackColor = false;
            btnCorrect.Click += btnCorrect_Click;
            // 
            // btnWrong
            // 
            btnWrong.BackColor = Color.FromArgb(255, 192, 192);
            btnWrong.Location = new Point(338, 63);
            btnWrong.Name = "btnWrong";
            btnWrong.Size = new Size(74, 42);
            btnWrong.TabIndex = 3;
            btnWrong.Text = "Wrong";
            btnWrong.UseVisualStyleBackColor = false;
            btnWrong.Click += btnWrong_Click;
            // 
            // lblOCRDebug
            // 
            lblOCRDebug.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            lblOCRDebug.Font = new Font("Segoe UI", 12F, FontStyle.Regular, GraphicsUnit.Point, 0);
            lblOCRDebug.Location = new Point(258, 9);
            lblOCRDebug.Name = "lblOCRDebug";
            lblOCRDebug.Size = new Size(154, 38);
            lblOCRDebug.TabIndex = 1;
            lblOCRDebug.Text = "263:1";
            lblOCRDebug.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // OCRDebug
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BorderStyle = BorderStyle.FixedSingle;
            Controls.Add(btnWrong);
            Controls.Add(btnCorrect);
            Controls.Add(lblOCRDebug);
            Controls.Add(pctOCRDebug);
            Name = "OCRDebug";
            Size = new Size(423, 115);
            ((System.ComponentModel.ISupportInitialize)pctOCRDebug).EndInit();
            ResumeLayout(false);
        }

        #endregion

        private PictureBox pctOCRDebug;
        private Button btnCorrect;
        private Button btnWrong;
        private Label lblOCRDebug;
    }
}
