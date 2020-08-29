using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        private Slideshow _slideshow;
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
            if (Settings.ImageBackgroundType != ImageBackgroundType.Single) Setup();
            else _timer.Change(Timeout.Infinite, Timeout.Infinite);
        }

        private void Setup()
        {
            _slideshow = new Slideshow(
                Settings.Extensions,
                Settings.BackgroundImagesDirectoryAbsolutePath,
                Settings.LoopSlideshow,
                Settings.ShuffleSlideshow
            );
            _timer.Change(0, (int) Settings.UpdateImageInterval.TotalMilliseconds);
            InvokeImageChanged();
        }

        ~SlideshowImageLoader()
        {
            Settings.OnChanged.RemoveEventHandler(ReloadSettings);
        }

        private class Slideshow
        {
            private readonly IList<string> _filePaths;
            private readonly bool _loop;
            private readonly bool _shuffle;
            private int _index;

            public Slideshow(string extensions, string path, bool loop, bool shuffle)
            {
                _loop = loop;
                _shuffle = shuffle;
                var ext = extensions.Split(new[] {",", " "}, StringSplitOptions.RemoveEmptyEntries);
                _filePaths = Directory.GetFiles(Path.GetFullPath(path))
                    .Where(x => ext.Contains(Path.GetExtension(x).ToLower()))
                    .ToList();
                if (shuffle) _filePaths.Shuffle();
            }

            public string Current => _filePaths[_index];

            public bool Next()
            {
                if (_index + 1 >= _filePaths.Count) return TryLoop();
                _index++;
                return true;
            }

            //make sure the same image doesn't show up too soon
            private bool ValidateShuffle(IList<string> prevOrder)
            {
                var count = prevOrder.Count;
                if (count <= 2) return true;
                var firstIsDifferent =
                    _filePaths[0] != prevOrder[count - 1]
                    && _filePaths[0] != prevOrder[count - 2];
                var secondIsDifferent =
                    count == 3
                    || _filePaths[1] != prevOrder[count - 1]
                    && _filePaths[1] != prevOrder[count - 2];
                return firstIsDifferent && secondIsDifferent;
            }

            private bool TryLoop()
            {
                if (!_loop) return false;
                _index = 0;
                if (_shuffle)
                {
                    var prevOrder = new List<string>(_filePaths);
                    do
                    {
                        _filePaths.Shuffle();
                    } while (!ValidateShuffle(prevOrder));
                }

                return true;
            }
        }
    }
}