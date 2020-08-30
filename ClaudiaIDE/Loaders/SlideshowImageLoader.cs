using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using ClaudiaIDE.Settings;

namespace ClaudiaIDE.Loaders
{
    internal class SlideshowImageLoader : ImageLoader
    {
        private readonly Timer _timer;
        private BitmapSource _bitmap;
        private ImageFileList _slideshow;
        public bool Paused { get; set; }

        public SlideshowImageLoader(Setting settings)
            : base(settings, ImageBackgroundType.Slideshow)
        {
            settings.OnChanged.AddEventHandler(ReloadSettings);
            _timer = new Timer(s =>
            {
                if (!Paused) NextImage();
            });
            Setup();
        }

        public void NextImage()
        {
            if (!_slideshow.Next()) return;
            _bitmap = null;
            InvokeImageChanged();
        }

        public override async Task<BitmapSource> GetBitmapAsync()
        {
            return _bitmap ??= await LoadImageAsync(_slideshow.Current);
        }

        private void ReloadSettings(object sender, EventArgs e)
        {
            if (Settings.ImageBackgroundType == ImageBackgroundType.Slideshow) Setup();
            else _timer.Change(Timeout.Infinite, Timeout.Infinite);
        }

        private void Setup()
        {
            _slideshow = new ImageFileList(
                Settings.Extensions,
                Settings.BackgroundImagesDirectoryAbsolutePath,
                Settings.LoopSlideshow,
                Settings.ShuffleSlideshow
            );
            Paused = false;
            _timer.Change(0, (int) Settings.UpdateImageInterval.TotalMilliseconds);
            InvokeImageChanged();
        }

        ~SlideshowImageLoader()
        {
            Settings.OnChanged.RemoveEventHandler(ReloadSettings);
        }
    }
}