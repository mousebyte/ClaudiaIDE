using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ClaudiaIDE.Loaders
{
    internal class ImageFileList
    {
        private readonly IList<string> _filePaths;
        private readonly bool _loop;
        private readonly bool _shuffle;
        private int _index;

        public ImageFileList(string extensions, string path, bool loop, bool shuffle)
        {
            _loop = loop;
            _shuffle = shuffle;
            var ext = extensions.Split(new[] {",", " "}, StringSplitOptions.RemoveEmptyEntries);
            _filePaths = Directory.GetFiles(Path.GetFullPath(path))
                .Where(x => ext.Contains(Path.GetExtension(x).ToLower()))
                .OrderBy(x => x)
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
    }
}