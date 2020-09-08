using System;
using ClaudiaIDE.Loaders;
using ClaudiaIDE.Settings;
using Microsoft.VisualStudio.Shell;

namespace ClaudiaIDE
{
    internal class ImageProvider
    {
        private ImageLoader _imageLoader;

        static ImageProvider()
        {
            ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
            {
                Instance = new ImageProvider();
                var settings = await Setting.GetLiveInstanceAsync();
                settings.OnChanged.AddEventHandler(Instance.OnSettingsChanged);
                Instance.ReloadSettings();
            });
        }

        public static ImageProvider Instance { get; private set; }

        public IImageLoader Loader => _imageLoader;

        public event EventHandler ProviderChanged;

        private void InvokeProviderChanged()
        {
            ProviderChanged?.Invoke(this, EventArgs.Empty);
        }

        private void ReloadSettings()
        {
            ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
            {
                var settings = await Setting.GetLiveInstanceAsync();
                if (settings.ImageBackgroundType == _imageLoader?.BackgroundType) return;
                _imageLoader = settings.ImageBackgroundType switch
                {
                    ImageBackgroundType.Single => new SingleImageLoader(settings),
                    ImageBackgroundType.Slideshow => new SlideshowImageLoader(settings),
                    ImageBackgroundType.SingleEach => new SingleImageEachLoader(settings),
                    _ => throw new ArgumentOutOfRangeException()
                };
                InvokeProviderChanged();
            });
        }

        private void OnSettingsChanged(object sender, EventArgs e)
        {
            ReloadSettings();
        }
    }
}