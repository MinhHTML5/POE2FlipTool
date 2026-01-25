using System.Data;
using System.Text.RegularExpressions;

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

        public void Init (string name, Image img, string text)
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

        private void btnWrong_Click(object sender, EventArgs e)
        {
            if (pctOCRDebug.Image != null)
            {
                SaveWithNextIndex(pctOCRDebug.Image as Bitmap);
            }

            var parent = this.Parent;
            parent?.Controls.Remove(this);
            this.Dispose();
        }

        public void SaveWithNextIndex(Bitmap bitmap)
        {
            Regex FileIndexRegex = new Regex(@"^sample_(\d+)\.png$", RegexOptions.IgnoreCase);

            Directory.CreateDirectory("ocrErrors");

            int nextIndex = Directory
                .EnumerateFiles("ocrErrors", "error_*.png")
                .Select(path => Path.GetFileName(path))
                .Select(name =>
                {
                    var match = FileIndexRegex.Match(name);
                    return match.Success ? int.Parse(match.Groups[1].Value) : -1;
                })
                .Where(i => i >= 0)
                .DefaultIfEmpty(-1)
                .Max() + 1;

            string filePath = Path.Combine("errorImages", $"sample_{nextIndex}.png");
            bitmap.Save(filePath);
        }
    }
}
