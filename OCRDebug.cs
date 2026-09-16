namespace POE2FlipTool
{
    public partial class OCRDebug : UserControl
    {
        public OCRDebug()
        {
            InitializeComponent();
        }

        public void SetItemName(string name)
        {
            lblItemName.Text = name;
        }

        public void SetDebugImage(Image img)
        {
            pctOCRDebug.Image = img;
        }

        public void SetDebugText(string text)
        {
            lblOCRDebug.Text = text;
        }

        public void Init(string name, Image img, string text)
        {
            SetItemName(name);
            SetDebugImage(img);
            SetDebugText(text);
        }

        private void btnCorrect_Click(object sender, EventArgs e)
        {
            var parent = this.Parent;
            parent?.Controls.Remove(this);
            this.Dispose();
        }
    }
}
