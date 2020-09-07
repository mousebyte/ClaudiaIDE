using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing.Design;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using ClaudiaIDE.Localized;
using ClaudiaIDE.Options;
using EnvDTE;
using EnvDTE80;
using Microsoft;
using Microsoft.VisualStudio.Settings;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell.Settings;
using Microsoft.VisualStudio.Threading;
using Newtonsoft.Json;
using AsyncServiceProvider = Microsoft.VisualStudio.Shell.AsyncServiceProvider;
using ThreadHelper = Microsoft.VisualStudio.Shell.ThreadHelper;

namespace ClaudiaIDE.Settings
{
    public class Setting
    {
        private static AsyncLazy<Setting> _liveModel =
            new AsyncLazy<Setting>(CreateAsync, ThreadHelper.JoinableTaskFactory);

        private static AsyncLazy<ShellSettingsManager> _settingsManager =
            new AsyncLazy<ShellSettingsManager>(GetSettingsManagerAsync, ThreadHelper.JoinableTaskFactory);

        private static readonly string CONFIGFILE = "config.txt";
        private const string DefaultBackgroundImage = "Images\\background.png";
        private const string DefaultBackgroundFolder = "Images";

        internal System.IServiceProvider ServiceProvider { get; set; }

        public WeakEvent<EventArgs> OnChanged = new WeakEvent<EventArgs>();

        public static Task<Setting> GetLiveInstanceAsync() => _liveModel.GetValueAsync();

        private static async Task<ShellSettingsManager> GetSettingsManagerAsync()
        {
            var svc =
                await AsyncServiceProvider.GlobalProvider.GetServiceAsync(typeof(SVsSettingsManager)) as
                    IVsSettingsManager;
            Assumes.Present(svc);
            return new ShellSettingsManager(svc);
        }

        public static async Task<Setting> CreateAsync()
        {
            var inst = new Setting();
            await inst.LoadAsync();
            return inst;
        }

        private IEnumerable<PropertyInfo> GetOptionProperties()
        {
            return GetType().GetProperties().Where(p => p.PropertyType.IsSerializable && p.PropertyType.IsPublic);
        }

        public static Setting Instance
        {
            get
            {
                ThreadHelper.ThrowIfNotOnUIThread();
                return ThreadHelper.JoinableTaskFactory.Run(GetLiveInstanceAsync);
            }
        }

        private Setting()
        {
            var assemblylocation = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            BackgroundImagesDirectoryAbsolutePath =
                Path.Combine(string.IsNullOrEmpty(assemblylocation) ? "" : assemblylocation, DefaultBackgroundFolder);
            BackgroundImageAbsolutePath = Path.Combine(string.IsNullOrEmpty(assemblylocation) ? "" : assemblylocation,
                DefaultBackgroundImage);
            Opacity = 0.35;
            PositionHorizon = PositionH.Right;
            PositionVertical = PositionV.Bottom;
            ImageStretch = ImageStretch.None;
            UpdateImageInterval = TimeSpan.FromMinutes(30);
            ImageFadeAnimationInterval = TimeSpan.FromSeconds(0);
            Extensions = ".png, .jpg";
            ImageBackgroundType = ImageBackgroundType.Single;
            LoopSlideshow = true;
            ShuffleSlideshow = false;
            MaxWidth = 0;
            MaxHeight = 0;
            SoftEdgeX = 0;
            SoftEdgeY = 0;
            ExpandToIDE = false;
            ViewBoxPointX = 0;
            ViewBoxPointY = 0;
        }

        [LocalManager.LocalizedCategory("Image")]
        [LocalManager.LocalizedDisplayName("BackgroundType")]
        [LocalManager.LocalizedDescription("BackgroundTypeDes")]
        [Microsoft.VisualStudio.Shell.PropertyPageTypeConverter(typeof(ImageBackgroundTypeConverter))]
        [TypeConverter(typeof(ImageBackgroundTypeConverter))]
        public ImageBackgroundType ImageBackgroundType { get; set; }

        [LocalManager.LocalizedCategory("Image")]
        [LocalManager.LocalizedDisplayName("OpacityType")]
        [LocalManager.LocalizedDescription("OpacityTypeDes")]
        public double Opacity { get; set; }


        [LocalManager.LocalizedCategoryAttribute("Layout")]
        [LocalManager.LocalizedDisplayName("VerticalAlignmentType")]
        [LocalManager.LocalizedDescription("VerticalAlignmentTypeDes")]
        [Microsoft.VisualStudio.Shell.PropertyPageTypeConverter(typeof(PositionVTypeConverter))]
        [TypeConverter(typeof(PositionVTypeConverter))]
        public PositionV PositionVertical { get; set; }


        [LocalManager.LocalizedCategoryAttribute("Layout")]
        [LocalManager.LocalizedDisplayName("HorizontalAlignmentType")]
        [LocalManager.LocalizedDescription("HorizontalAlignmentTypeDes")]
        [Microsoft.VisualStudio.Shell.PropertyPageTypeConverter(typeof(PositionHTypeConverter))]
        [TypeConverter(typeof(PositionHTypeConverter))]
        public PositionH PositionHorizon { get; set; }

        [LocalManager.LocalizedCategory("Layout")]
        [LocalManager.LocalizedDisplayName("MaxWidthType")]
        [LocalManager.LocalizedDescription("MaxWidthTypeDes")]
        public int MaxWidth { get; set; }

        [LocalManager.LocalizedCategory("Layout")]
        [LocalManager.LocalizedDisplayName("MaxHeightType")]
        [LocalManager.LocalizedDescription("MaxHeightTypeDes")]
        public int MaxHeight { get; set; }

        [LocalManager.LocalizedCategory("Layout")]
        [LocalManager.LocalizedDisplayName("SoftEdgeX")]
        [LocalManager.LocalizedDescription("SoftEdgeDes")]
        public int SoftEdgeX { get; set; }

        [LocalManager.LocalizedCategory("Layout")]
        [LocalManager.LocalizedDisplayName("SoftEdgeY")]
        [LocalManager.LocalizedDescription("SoftEdgeDes")]
        public int SoftEdgeY { get; set; }

        [LocalManager.LocalizedCategory("SingleImage")]
        [LocalManager.LocalizedDisplayName("FilePathType")]
        [LocalManager.LocalizedDescription("FilePathTypeDes")]
        [EditorAttribute(typeof(BrowseFile), typeof(UITypeEditor))]
        public string BackgroundImageAbsolutePath { get; set; }

        [LocalManager.LocalizedCategory("Slideshow")]
        [LocalManager.LocalizedDisplayName("UpdateIntervalType")]
        [LocalManager.LocalizedDescription("UpdateIntervalTypeDes")]
        [Microsoft.VisualStudio.Shell.PropertyPageTypeConverter(typeof(TimeSpanConverter))]
        [TypeConverter(typeof(TimeSpanConverter))]
        public TimeSpan UpdateImageInterval { get; set; }

        public TimeSpan ImageFadeAnimationInterval { get; set; }

        [LocalManager.LocalizedCategory("Slideshow")]
        [LocalManager.LocalizedDisplayName("DirectoryPathType")]
        [LocalManager.LocalizedDescription("DirectoryPathTypeDes")]
        [EditorAttribute(typeof(BrowseDirectory), typeof(UITypeEditor))]
        public string BackgroundImagesDirectoryAbsolutePath { get; set; }

        [LocalManager.LocalizedCategory("Slideshow")]
        [LocalManager.LocalizedDisplayName("ImageExtensionsType")]
        [LocalManager.LocalizedDescription("ImageExtensionsTypeDes")]
        public string Extensions { get; set; }

        [LocalManager.LocalizedCategory("Slideshow")]
        [LocalManager.LocalizedDisplayName("LoopSlideshowType")]
        [LocalManager.LocalizedDescription("LoopSlideshowTypeDes")]
        public bool LoopSlideshow { get; set; }
        [LocalManager.LocalizedCategoryAttribute("Slideshow")]
        [LocalManager.LocalizedDisplayName("ShuffleSlideshowType")]
        [LocalManager.LocalizedDescription("ShuffleSlideshowTypeDes")]
        public bool ShuffleSlideshow { get; set; }
        [LocalManager.LocalizedCategory("Layout")]
        [LocalManager.LocalizedDisplayName("ImageStretchType")]
        [LocalManager.LocalizedDescription("ImageStretchTypeDes")]
        [Microsoft.VisualStudio.Shell.PropertyPageTypeConverter(typeof(ImageStretchTypeConverter))]
        [TypeConverter(typeof(ImageStretchTypeConverter))]
        public ImageStretch ImageStretch { get; set; }

        [LocalManager.LocalizedCategory("Layout")]
        [LocalManager.LocalizedDisplayName("ExpandToIDEType")]
        [LocalManager.LocalizedDescription("ExpandToIDETypeDes")]
        public bool ExpandToIDE { get; set; }

        [LocalManager.LocalizedCategory("Layout")]
        [LocalManager.LocalizedDisplayName("ViewBoxPointX")]
        [LocalManager.LocalizedDescription("ViewBoxPointXDes")]
        public double ViewBoxPointX { get; set; }

        [LocalManager.LocalizedCategory("Layout")]
        [LocalManager.LocalizedDisplayName("ViewBoxPointY")]
        [LocalManager.LocalizedDescription("ViewBoxPointYDes")]
        public double ViewBoxPointY { get; set; }

        public void Serialize()
        {
            var config = JsonSerializer<Setting>.Serialize(this);

            var assemblylocation = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            var configpath = Path.Combine(string.IsNullOrEmpty(assemblylocation) ? "" : assemblylocation, CONFIGFILE);

            using (var s = new StreamWriter(configpath, false, Encoding.ASCII))
            {
                s.Write(config);
                s.Close();
            }
        }


        public async Task SaveAsync()
        {
            var manager = await _settingsManager.GetValueAsync();
            var settingsStore = manager.GetWritableSettingsStore(SettingsScope.UserSettings);
            if (!settingsStore.CollectionExists(CollectionName))
            {
                settingsStore.CreateCollection(CollectionName);
            }

            foreach (var property in GetOptionProperties())
            {
                var output = JsonConvert.SerializeObject(property.GetValue(this));
                settingsStore.SetString(CollectionName, property.Name, output);
            }

            var liveModel = await GetLiveInstanceAsync();

            if (this != liveModel)
            {
                await liveModel.LoadAsync();
                liveModel.OnApplyChanged();
            }
        }

        private string CollectionName => typeof(Setting).FullName;

        public async Task LoadAsync()
        {
            var manager = await _settingsManager.GetValueAsync();
            var settingsStore = manager.GetReadOnlySettingsStore(SettingsScope.UserSettings);
            if (!settingsStore.CollectionExists(CollectionName))
            {
                return;
            }


            foreach (var property in GetOptionProperties())
            {
                try
                {
                    var serializedProp = settingsStore.GetString(CollectionName, property.Name);
                    var value = JsonConvert.DeserializeObject(serializedProp, property.PropertyType,
                        new JsonSerializerSettings());
                    property.SetValue(this, value);
                }
                catch (Exception e)
                {
                    System.Diagnostics.Debug.Write(e);
                }
            }
        }

        private static object DeserializeValue(string value, Type type)
        {
            var b = Convert.FromBase64String(value);
            using (var strm = new MemoryStream(b))
            {
                var formatter = new BinaryFormatter();
                return formatter.Deserialize(strm);
            }
        }

        private static string SerializeValue<T>(T value)
        {
            using (var strm = new MemoryStream())
            {
                return TypeDescriptor.GetConverter(typeof(T)).ConvertToString(value);
            }
        }

        public void Load()
        {
            ThreadHelper.JoinableTaskFactory.Run(LoadAsync);
        }

        public void Save()
        {
            ThreadHelper.JoinableTaskFactory.Run(SaveAsync);
        }

        public void OnApplyChanged()
        {
            OnChanged?.RaiseEvent(this, EventArgs.Empty);
        }

        public static Setting Deserialize()
        {
            var assemblylocation = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            var configpath = Path.Combine(string.IsNullOrEmpty(assemblylocation) ? "" : assemblylocation, CONFIGFILE);
            string config = "";

            using (var s = new StreamReader(configpath, Encoding.ASCII, false))
            {
                config = s.ReadToEnd();
                s.Close();
            }

            var ret = JsonSerializer<Setting>.DeSerialize(config);
            ret.BackgroundImageAbsolutePath = ToFullPath(ret.BackgroundImageAbsolutePath, DefaultBackgroundImage);
            ret.BackgroundImagesDirectoryAbsolutePath =
                ToFullPath(ret.BackgroundImagesDirectoryAbsolutePath, DefaultBackgroundFolder);
            return ret;
        }

        public static string ToFullPath(string path, string defaultPath)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                path = defaultPath;
            }

            var assemblylocation = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            if (!Path.IsPathRooted(path))
            {
                path = Path.Combine(string.IsNullOrEmpty(assemblylocation) ? "" : assemblylocation, path);
            }

            return path;
        }
    }

    [CLSCompliant(false), ComVisible(true)]
    [Guid("12d9a45f-ec0b-4a96-88dc-b0cba1f4789a")]
    public enum PositionV
    {
        Top,
        Bottom,
        Center
    }

    [CLSCompliant(false), ComVisible(true)]
    [Guid("8b2e3ece-fbf7-43ba-b369-3463726b828d")]
    public enum PositionH
    {
        Left,
        Right,
        Center
    }

    [CLSCompliant(false), ComVisible(true)]
    [Guid("5C96CFAA-FE54-49A9-8AB7-E85B66731228")]
    public enum ImageBackgroundType
    {
        Single = 0,
        Slideshow = 1,
        SingleEach = 2
    }

    [CLSCompliant(false), ComVisible(true)]
    [Guid("C89AFB79-39AF-4716-BB91-0F77323DD89B")]
    public enum ImageStretch
    {
        None = 0,
        Uniform = 1,
        UniformToFill = 2,
        Fill = 3
    }

    public static class ImageStretchConverter
    {
        public static System.Windows.Media.Stretch ConvertTo(this ImageStretch source)
        {
            switch (source)
            {
                case ImageStretch.Fill:
                    return System.Windows.Media.Stretch.Fill;
                case ImageStretch.None:
                    return System.Windows.Media.Stretch.None;
                case ImageStretch.Uniform:
                    return System.Windows.Media.Stretch.Uniform;
                case ImageStretch.UniformToFill:
                    return System.Windows.Media.Stretch.UniformToFill;
            }

            return System.Windows.Media.Stretch.None;
        }
    }

    public static class PositionConverter
    {
        public static System.Windows.Media.AlignmentY ConvertTo(this PositionV source)
        {
            switch (source)
            {
                case PositionV.Bottom:
                    return System.Windows.Media.AlignmentY.Bottom;
                case PositionV.Center:
                    return System.Windows.Media.AlignmentY.Center;
                case PositionV.Top:
                    return System.Windows.Media.AlignmentY.Top;
            }

            return System.Windows.Media.AlignmentY.Bottom;
        }

        public static System.Windows.VerticalAlignment ConvertToVerticalAlignment(this PositionV source)
        {
            switch (source)
            {
                case PositionV.Bottom:
                    return System.Windows.VerticalAlignment.Bottom;
                case PositionV.Center:
                    return System.Windows.VerticalAlignment.Center;
                case PositionV.Top:
                    return System.Windows.VerticalAlignment.Top;
            }

            return System.Windows.VerticalAlignment.Bottom;
        }

        public static System.Windows.Media.AlignmentX ConvertTo(this PositionH source)
        {
            switch (source)
            {
                case PositionH.Left:
                    return System.Windows.Media.AlignmentX.Left;
                case PositionH.Center:
                    return System.Windows.Media.AlignmentX.Center;
                case PositionH.Right:
                    return System.Windows.Media.AlignmentX.Right;
            }

            return System.Windows.Media.AlignmentX.Right;
        }

        public static System.Windows.HorizontalAlignment ConvertToHorizontalAlignment(this PositionH source)
        {
            switch (source)
            {
                case PositionH.Left:
                    return System.Windows.HorizontalAlignment.Left;
                case PositionH.Center:
                    return System.Windows.HorizontalAlignment.Center;
                case PositionH.Right:
                    return System.Windows.HorizontalAlignment.Right;
            }

            return System.Windows.HorizontalAlignment.Right;
        }
    }
}