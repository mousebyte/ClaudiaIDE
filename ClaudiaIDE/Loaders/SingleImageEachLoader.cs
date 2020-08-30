using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using ClaudiaIDE.Settings;

namespace ClaudiaIDE.Loaders
{
    class SingleImageEachLoader : ImageLoader
    {
        private ImageFileList _imagePaths;

        public SingleImageEachLoader(Setting settings)
            : base(settings, ImageBackgroundType.SingleEach)
        {
            settings.OnChanged.AddEventHandler((sender, args) => Setup());
            Setup();
        }

        public override Task<BitmapSource> GetBitmapAsync()
        {
            _imagePaths.Next();
            return LoadImageAsync(_imagePaths.Current);
        }

        private void Setup()
        {
            _imagePaths = new ImageFileList(
                Settings.Extensions,
                Settings.BackgroundImagesDirectoryAbsolutePath,
                true,
                true
            );
        }
    }
}