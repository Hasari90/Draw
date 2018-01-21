using System.Drawing;

namespace Draw_HJ
{
    public interface IPlugin
    {
        string Name { get; }
        Bitmap Work(Bitmap bitmap);
    }
}
