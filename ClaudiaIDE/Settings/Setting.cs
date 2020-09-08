using System;
/*
 * Implementation based on https://github.com/madskristensen/OptionsSample
 */
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing.Design;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using ClaudiaIDE.Localized;
using ClaudiaIDE.Options;
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
        private static readonly AsyncLazy<Setting> LiveModel =
            new AsyncLazy<Setting>(CreateAsync, ThreadHelper.JoinableTaskFactory);

        private static readonly AsyncLazy<ShellSettingsManager> SettingsManager =
            new AsyncLazy<ShellSettingsManager>(GetSettingsManagerAsync, ThreadHelper.JoinableTaskFactory);

        private static readonly string DefaultBackgroundImage;
        private static readonly string DefaultBackgroundFolder;

        public WeakEvent<EventArgs> OnChanged = new WeakEvent<EventArgs>();

        public static Task<Setting> GetLiveInstanceAsync() => LiveModel.GetValueAsync();

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

        static Setting()
        {
            var assemblylocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            DefaultBackgroundFolder =
                Path.Combine(string.IsNullOrEmpty(assemblylocation) ? "" : assemblylocation, "Images");
            DefaultBackgroundImage = Path.Combine(string.IsNullOrEmpty(assemblylocation) ? "" : assemblylocation,
                DefaultBackgroundImage);
        }

        private Setting() { }

        [LocalManager.LocalizedCategory("Image")]
        [LocalManager.LocalizedDisplayName("BackgroundType")]
        [LocalManager.LocalizedDescription("BackgroundTypeDes")]
        [Microsoft.VisualStudio.Shell.PropertyPageTypeConverter(typeof(ImageBackgroundTypeConverter))]
        [TypeConverter(typeof(ImageBackgroundTypeConverter))]
        public ImageBackgroundType ImageBackgroundType { get; set; } = ImageBackgroundType.Single;

        [LocalManager.LocalizedCategory("Image")]
        [LocalManager.LocalizedDisplayName("OpacityType")]
        [LocalManager.LocalizedDescription("OpacityTypeDes")]
        public double Opacity { get; set; } = 0.35d;

        [LocalManager.LocalizedCategoryAttribute("Layout")]
        [LocalManager.LocalizedDisplayName("VerticalAlignmentType")]
        [LocalManager.LocalizedDescription("VerticalAlignmentTypeDes")]
        [Microsoft.VisualStudio.Shell.PropertyPageTypeConverter(typeof(PositionVTypeConverter))]
        [TypeConverter(typeof(PositionVTypeConverter))]
        public PositionV PositionVertical { get; set; } = PositionV.Bottom;

        [LocalManager.LocalizedCategoryAttribute("Layout")]
        [LocalManager.LocalizedDisplayName("HorizontalAlignmentType")]
        [LocalManager.LocalizedDescription("HorizontalAlignmentTypeDes")]
        [Microsoft.VisualStudio.Shell.PropertyPageTypeConverter(typeof(PositionHTypeConverter))]
        [TypeConverter(typeof(PositionHTypeConverter))]
        public PositionH PositionHorizon { get; set; } = PositionH.Right;

        [LocalManager.LocalizedCategory("Layout")]
        [LocalManager.LocalizedDisplayName("MaxWidthType")]
        [LocalManager.LocalizedDescription("MaxWidthTypeDes")]
        public int MaxWidth { get; set; } = 0;

        [LocalManager.LocalizedCategory("Layout")]
        [LocalManager.LocalizedDisplayName("MaxHeightType")]
        [LocalManager.LocalizedDescription("MaxHeightTypeDes")]
        public int MaxHeight { get; set; } = 0;

        [LocalManager.LocalizedCategory("Layout")]
        [LocalManager.LocalizedDisplayName("SoftEdgeX")]
        [LocalManager.LocalizedDescription("SoftEdgeDes")]
        public int SoftEdgeX { get; set; } = 0;

        [LocalManager.LocalizedCategory("Layout")]
        [LocalManager.LocalizedDisplayName("SoftEdgeY")]
        [LocalManager.LocalizedDescription("SoftEdgeDes")]
        public int SoftEdgeY { get; set; } = 0;

        [LocalManager.LocalizedCategory("SingleImage")]
        [LocalManager.LocalizedDisplayName("FilePathType")]
        [LocalManager.LocalizedDescription("FilePathTypeDes")]
        [EditorAttribute(typeof(BrowseFile), typeof(UITypeEditor))]
        public string BackgroundImageAbsolutePath { get; set; } = DefaultBackgroundImage;

        [LocalManager.LocalizedCategory("Slideshow")]
        [LocalManager.LocalizedDisplayName("UpdateIntervalType")]
        [LocalManager.LocalizedDescription("UpdateIntervalTypeDes")]
        [Microsoft.VisualStudio.Shell.PropertyPageTypeConverter(typeof(TimeSpanConverter))]
        [TypeConverter(typeof(TimeSpanConverter))]
        public TimeSpan UpdateImageInterval { get; set; } = TimeSpan.FromMinutes(30);

        public TimeSpan ImageFadeAnimationInterval { get; set; } = TimeSpan.FromSeconds(0);

        [LocalManager.LocalizedCategory("Slideshow")]
        [LocalManager.LocalizedDisplayName("DirectoryPathType")]
        [LocalManager.LocalizedDescription("DirectoryPathTypeDes")]
        [EditorAttribute(typeof(BrowseDirectory), typeof(UITypeEditor))]
        public string BackgroundImagesDirectoryAbsolutePath { get; set; } = DefaultBackgroundFolder;

        [LocalManager.LocalizedCategory("Slideshow")]
        [LocalManager.LocalizedDisplayName("ImageExtensionsType")]
        [LocalManager.LocalizedDescription("ImageExtensionsTypeDes")]
        public string Extensions { get; set; } = ".png, .jpg";

        [LocalManager.LocalizedCategory("Slideshow")]
        [LocalManager.LocalizedDisplayName("LoopSlideshowType")]
        [LocalManager.LocalizedDescription("LoopSlideshowTypeDes")]
        public bool LoopSlideshow { get; set; } = true;

        [LocalManager.LocalizedCategoryAttribute("Slideshow")]
        [LocalManager.LocalizedDisplayName("ShuffleSlideshowType")]
        [LocalManager.LocalizedDescription("ShuffleSlideshowTypeDes")]
        public bool ShuffleSlideshow { get; set; } = true;

        [LocalManager.LocalizedCategory("Layout")]
        [LocalManager.LocalizedDisplayName("ImageStretchType")]
        [LocalManager.LocalizedDescription("ImageStretchTypeDes")]
        [Microsoft.VisualStudio.Shell.PropertyPageTypeConverter(typeof(ImageStretchTypeConverter))]
        [TypeConverter(typeof(ImageStretchTypeConverter))]
        public ImageStretch ImageStretch { get; set; } = ImageStretch.None;

        [LocalManager.LocalizedCategory("Layout")]
        [LocalManager.LocalizedDisplayName("ExpandToIDEType")]
        [LocalManager.LocalizedDescription("ExpandToIDETypeDes")]
        public bool ExpandToIDE { get; set; } = false;

        [LocalManager.LocalizedCategory("Layout")]
        [LocalManager.LocalizedDisplayName("ViewBoxPointX")]
        [LocalManager.LocalizedDescription("ViewBoxPointXDes")]
        public double ViewBoxPointX { get; set; } = 0;

        [LocalManager.LocalizedCategory("Layout")]
        [LocalManager.LocalizedDisplayName("ViewBoxPointY")]
        [LocalManager.LocalizedDescription("ViewBoxPointYDes")]
        public double ViewBoxPointY { get; set; } = 0;


        public async Task SaveAsync()
        {
            var manager = await SettingsManager.GetValueAsync();
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
            var manager = await SettingsManager.GetValueAsync();
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

        public void Load()
        {
            ThreadHelper.JoinableTaskFactory.Run(LoadAsync);
        }

        public void Save()
        {
            ThreadHelper.JoinableTaskFactory.Run(SaveAsync);
        }

        private void OnApplyChanged()
        {
            OnChanged?.RaiseEvent(this, EventArgs.Empty);
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