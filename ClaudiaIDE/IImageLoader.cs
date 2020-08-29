using System;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace ClaudiaIDE
{
    internal interface IImageLoader
    {
        Task<BitmapSource> GetBitmapAsync();
        event EventHandler ImageChanged;
    }
}