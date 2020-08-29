using System;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using ClaudiaIDE.Settings;

namespace ClaudiaIDE
{
    internal interface IImageLoader
    {
        ImageBackgroundType BackgroundType { get; }
        Task<BitmapSource> GetBitmapAsync();
        event EventHandler ImageChanged;
    }
}