using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ClaudiaIDE.Settings;

namespace ClaudiaIDE.Loaders
{
    //abstract base for asynchronous image loading
    internal abstract class ImageLoader : IImageLoader
    {
        protected readonly Setting Settings;

        public ImageBackgroundType BackgroundType { get; }

        protected ImageLoader(Setting settings, ImageBackgroundType type)
        {
            Settings = settings;
            BackgroundType = type;
        }

        public abstract Task<BitmapSource> GetBitmapAsync();

        protected void InvokeImageChanged()
        {
            ImageChanged?.Invoke(this, EventArgs.Empty);
        }

        protected Task<BitmapSource> LoadImageAsync(string path)
        {
            return Task.Factory.StartNew(
                () =>
                {
                    if (!File.Exists(path)) return null;
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();

                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.CreateOptions = BitmapCreateOptions.None;
                    bitmap.UriSource = new Uri(path, UriKind.RelativeOrAbsolute);
                    bitmap.EndInit();
                    bitmap.Freeze();
                    if (Settings.ImageStretch == ImageStretch.None)
                        bitmap = EnsureMaxWidthHeight(
                            bitmap,
                            Settings.MaxWidth,
                            Settings.MaxHeight
                        );
                    return Math.Abs(bitmap.DpiX - 96) > 1 || Math.Abs(bitmap.DpiY - 96) > 1
                        ? ConvertToDpi96(bitmap)
                        : bitmap;
                },
                CancellationToken.None,
                TaskCreationOptions.None,
                TaskScheduler.Default
            );
        }

        private static BitmapSource ConvertToDpi96(BitmapImage image)
        {
            const int dpi = 96;
            var width = image.PixelWidth;
            var height = image.PixelHeight;

            var stride = width * 4;
            var pixelData = new byte[stride * height];
            image.CopyPixels(pixelData, stride, 0);

            var source = BitmapSource.Create(
                width,
                height,
                dpi,
                dpi,
                image.Format,
                null,
                pixelData,
                stride
            );
            source.Freeze();
            return source;
        }

        private static BitmapImage EnsureMaxWidthHeight
            (BitmapImage original, int maxWidth, int maxHeight)
        {
            var setWidth = maxWidth > 0 && original.PixelWidth > maxWidth;
            var setHeight = maxHeight > 0 && original.PixelHeight > maxHeight;
            if (!setWidth && !setHeight) return original;
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.CreateOptions = BitmapCreateOptions.None;
            bitmap.UriSource = original.UriSource;
            if (setWidth) bitmap.DecodePixelWidth = maxWidth;
            if (setWidth) bitmap.DecodePixelHeight = maxHeight;
            bitmap.EndInit();
            bitmap.Freeze();
            return bitmap;
        }

        /// <inheritdoc />
        public event EventHandler ImageChanged;
    }
}