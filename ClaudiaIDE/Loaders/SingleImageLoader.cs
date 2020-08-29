using System;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using ClaudiaIDE.Settings;

namespace ClaudiaIDE.Loaders
{
    internal class SingleImageLoader : ImageLoader
    {
        private BitmapSource _bitmap;

        public SingleImageLoader(Setting settings) : base(settings, ImageBackgroundType.Single)
        {
            settings.OnChanged.AddEventHandler(ReloadSettings);
        }


        public override async Task<BitmapSource> GetBitmapAsync()
        {
            return _bitmap ??= await LoadImageAsync(Settings.BackgroundImageAbsolutePath);
        }

        private void ReloadSettings(object sender, EventArgs e)
        {
            _bitmap = null;
            InvokeImageChanged();
        }

        ~SingleImageLoader()
        {
            Settings.OnChanged.RemoveEventHandler(ReloadSettings);
        }
    }
}