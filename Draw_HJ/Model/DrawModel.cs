using System.Drawing;

namespace Draw_HJ
{
    public class DrawModel
    {
        private Graphics g;
        public Graphics G { get => g; set => g = value; }
        private Color color;
        public Color Color { get => color; set => color = value; }
        private Color colorBackground;
        public Color ColorBackground { get => colorBackground; set => colorBackground = value; }
        private Tools tool;
        public Tools Tool { get => tool; set => tool = value; }
        private Fills fill;
        public Fills Fill { get => fill; set => fill = value; }
        public int Pixel { get; set; }


        public Image Resize(Image image, Size size)
        {
            Image resizeImage = new Bitmap(size.Width, size.Height);

            using (Graphics graphics = Graphics.FromImage((Bitmap)resizeImage))
            {
                graphics.DrawImage(image, new Rectangle(new Point(0,0), image.Size));
            }

            return resizeImage;
        }
    }
}
