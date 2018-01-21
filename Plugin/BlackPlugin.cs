using Draw_HJ;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Plugin
{
    public class BlackPlugin : IPlugin
    {
        public string Name
        {
            get
            {
                return "Black Image";
            }
        }

        public Bitmap Work(Bitmap bitmap)
        {
            Color color = Color.Black;
            Color actualColor;

            Bitmap newBitmap = new Bitmap(bitmap.Width, bitmap.Height);
            for (int i = 0; i < bitmap.Width; i++)
            {
                for (int j = 0; j < bitmap.Height; j++)
                {
                    actualColor = bitmap.GetPixel(i, j);

                    if (actualColor.A > 150)
                        newBitmap.SetPixel(i, j, color);
                    else
                        newBitmap.SetPixel(i, j, actualColor);
                }
            }
            return newBitmap;
        }
    }
}
