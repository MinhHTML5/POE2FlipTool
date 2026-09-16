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
            lblOCRDebug = new Label();
            lblItemName = new Label();
            ((System.ComponentModel.ISupportInitialize)pctOCRDebug).BeginInit();
            SuspendLayout();
            // 
            // pctOCRDebug
            // 
            pctOCRDebug.BorderStyle = BorderStyle.FixedSingle;
            pctOCRDebug.Location = new Point(8, 27);
            pctOCRDebug.Name = "pctOCRDebug";
            pctOCRDebug.Size = new Size(208, 52);
            pctOCRDebug.SizeMode = PictureBoxSizeMode.CenterImage;
            pctOCRDebug.TabIndex = 0;
            pctOCRDebug.TabStop = false;
            // 
            // btnCorrect
            // 
            btnCorrect.BackColor = Color.FromArgb(192, 255, 192);
            btnCorrect.Location = new Point(142, 87);
            btnCorrect.Name = "btnCorrect";
            btnCorrect.Size = new Size(74, 27);
            btnCorrect.TabIndex = 2;
            btnCorrect.Text = "Dismiss";
            btnCorrect.UseVisualStyleBackColor = false;
            btnCorrect.Click += btnCorrect_Click;
            // 
            // lblOCRDebug
            // 
            lblOCRDebug.Font = new Font("Segoe UI", 12F, FontStyle.Regular, GraphicsUnit.Point, 0);
            lblOCRDebug.Location = new Point(8, 85);
            lblOCRDebug.Name = "lblOCRDebug";
            lblOCRDebug.Size = new Size(103, 23);
            lblOCRDebug.TabIndex = 1;
            lblOCRDebug.Text = "263:1";
            lblOCRDebug.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // lblItemName
            // 
            lblItemName.Font = new Font("Segoe UI", 12F, FontStyle.Regular, GraphicsUnit.Point, 0);
            lblItemName.Location = new Point(8, 1);
            lblItemName.Name = "lblItemName";
            lblItemName.Size = new Size(154, 23);
            lblItemName.TabIndex = 4;
            lblItemName.Text = "Vaal cultivation orb";
            lblItemName.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // OCRDebug
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BorderStyle = BorderStyle.FixedSingle;
            Controls.Add(lblItemName);
            Controls.Add(btnCorrect);
            Controls.Add(lblOCRDebug);
            Controls.Add(pctOCRDebug);
            Name = "OCRDebug";
            Size = new Size(222, 117);
            ((System.ComponentModel.ISupportInitialize)pctOCRDebug).EndInit();
            ResumeLayout(false);
        }

        #endregion

        private PictureBox pctOCRDebug;
        private Button btnCorrect;
        private Label lblOCRDebug;
        private Label lblItemName;
    }
}
