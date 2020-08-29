using System;
using ClaudiaIDE.Loaders;
using ClaudiaIDE.Settings;

namespace ClaudiaIDE
{
    internal class ImageProvider
    {
        private readonly Setting _settings;
        private ImageLoader _imageLoader;

        private ImageProvider(Setting settings)
        {
            _settings = settings;
            settings.OnChanged.AddEventHandler(OnSettingsChanged);
            ReloadSettings();
        }

        public IImageLoader Loader => _imageLoader;

        public static void Initialize(Setting settings)
        {
            Instance = new ImageProvider(settings);
        }

        public static ImageProvider Instance { get; private set; }

        private void ReloadSettings()
        {
            if (_settings.ImageBackgroundType == _imageLoader?.BackgroundType) return;
            _imageLoader = _settings.ImageBackgroundType switch
            {
                ImageBackgroundType.Single => new SingleImageLoader(_settings),
                ImageBackgroundType.Slideshow => new SlideshowImageLoader(_settings)
                //ImageBackgroundType.SingleEach => 
            };
            InvokeProviderChanged();
        }

        private void OnSettingsChanged(object sender, EventArgs e)
        {
            ReloadSettings();
        }

        public event EventHandler ProviderChanged;

        protected virtual void InvokeProviderChanged()
        {
            ProviderChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}