using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing.Design;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using ClaudiaIDE.Localized;
using ClaudiaIDE.Options;
using Microsoft;
using Microsoft.VisualStudio.Settings;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell.Settings;
using Microsoft.VisualStudio.Threading;
using Newtonsoft.Json;
using Task = System.Threading.Tasks.Task; /*
 * Implementation based on https://github.com/madskristensen/OptionsSample
 */


namespace ClaudiaIDE.Settings
{
    public class Setting
    {
        private static readonly AsyncLazy<Setting> LiveModel =
            new AsyncLazy<Setting>(CreateAsync, ThreadHelper.JoinableTaskFactory);

        private static readonly AsyncLazy<ShellSettingsManager> SettingsManager =
            new AsyncLazy<ShellSettingsManager>(GetSettingsManagerAsync, ThreadHelper.JoinableTaskFactory);

        private static readonly string CollectionName = typeof(Setting).FullName;
        private static readonly string DefaultBackgroundFolder;
        private static readonly string DefaultBackgroundImage;


        public WeakEvent<EventArgs> OnChanged = new WeakEvent<EventArgs>();

        static Setting()
        {
            var assemblylocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            DefaultBackgroundFolder =
                Path.Combine(string.IsNullOrEmpty(assemblylocation) ? "" : assemblylocation, "Images");
            DefaultBackgroundImage = Path.Combine(string.IsNullOrEmpty(assemblylocation) ? "" : assemblylocation,
                "Images\\background.png");
        }

        private Setting() { }
        
        public static Setting Instance
        {
            get
            {
                ThreadHelper.ThrowIfNotOnUIThread();
                return ThreadHelper.JoinableTaskFactory.Run(GetLiveInstanceAsync);
            }
        }

        [LocalManager.LocalizedCategoryAttribute("SingleImage")]
        [LocalManager.LocalizedDisplayNameAttribute("FilePathType")]
        [LocalManager.LocalizedDescriptionAttribute("FilePathTypeDes")]
        [EditorAttribute(typeof(BrowseFile), typeof(UITypeEditor))]
        public string BackgroundImageAbsolutePath { get; set; } = DefaultBackgroundImage;

        [LocalManager.LocalizedCategoryAttribute("Slideshow")]
        [LocalManager.LocalizedDisplayNameAttribute("DirectoryPathType")]
        [LocalManager.LocalizedDescriptionAttribute("DirectoryPathTypeDes")]
        [EditorAttribute(typeof(BrowseDirectory), typeof(UITypeEditor))]
        public string BackgroundImagesDirectoryAbsolutePath { get; set; } = DefaultBackgroundFolder;

        [LocalManager.LocalizedCategoryAttribute("Layout")]
        [LocalManager.LocalizedDisplayNameAttribute("ExpandToIDEType")]
        [LocalManager.LocalizedDescriptionAttribute("ExpandToIDETypeDes")]
        public bool ExpandToIDE { get; set; } = false;

        [LocalManager.LocalizedCategoryAttribute("Slideshow")]
        [LocalManager.LocalizedDisplayNameAttribute("ImageExtensionsType")]
        [LocalManager.LocalizedDescriptionAttribute("ImageExtensionsTypeDes")]
        public string Extensions { get; set; } = ".png, .jpg";

        [LocalManager.LocalizedCategoryAttribute("Image")]
        [LocalManager.LocalizedDisplayNameAttribute("BackgroundType")]
        [LocalManager.LocalizedDescriptionAttribute("BackgroundTypeDes")]
        [PropertyPageTypeConverter(typeof(ImageBackgroundTypeConverter))]
        [TypeConverter(typeof(ImageBackgroundTypeConverter))]
        public ImageBackgroundType ImageBackgroundType { get; set; } = ImageBackgroundType.Single;

        public TimeSpan ImageFadeAnimationInterval { get; set; } = TimeSpan.FromSeconds(0);

        [LocalManager.LocalizedCategoryAttribute("Layout")]
        [LocalManager.LocalizedDisplayNameAttribute("ImageStretchType")]
        [LocalManager.LocalizedDescriptionAttribute("ImageStretchTypeDes")]
        [PropertyPageTypeConverter(typeof(ImageStretchTypeConverter))]
        [TypeConverter(typeof(ImageStretchTypeConverter))]
        public ImageStretch ImageStretch { get; set; } = ImageStretch.None;

        [LocalManager.LocalizedCategoryAttribute("Slideshow")]
        [LocalManager.LocalizedDisplayNameAttribute("LoopSlideshowType")]
        [LocalManager.LocalizedDescriptionAttribute("LoopSlideshowTypeDes")]
        public bool LoopSlideshow { get; set; } = true;

        [LocalManager.LocalizedCategoryAttribute("Layout")]
        [LocalManager.LocalizedDisplayNameAttribute("MaxHeightType")]
        [LocalManager.LocalizedDescriptionAttribute("MaxHeightTypeDes")]
        public int MaxHeight { get; set; } = 0;

        [LocalManager.LocalizedCategoryAttribute("Layout")]
        [LocalManager.LocalizedDisplayNameAttribute("MaxWidthType")]
        [LocalManager.LocalizedDescriptionAttribute("MaxWidthTypeDes")]
        public int MaxWidth { get; set; } = 0;

        [LocalManager.LocalizedCategoryAttribute("Image")]
        [LocalManager.LocalizedDisplayNameAttribute("OpacityType")]
        [LocalManager.LocalizedDescriptionAttribute("OpacityTypeDes")]
        public double Opacity { get; set; } = 0.35d;

        [LocalManager.LocalizedCategoryAttribute("Layout")]
        [LocalManager.LocalizedDisplayNameAttribute("HorizontalAlignmentType")]
        [LocalManager.LocalizedDescriptionAttribute("HorizontalAlignmentTypeDes")]
        [PropertyPageTypeConverter(typeof(PositionHTypeConverter))]
        [TypeConverter(typeof(PositionHTypeConverter))]
        public PositionH PositionHorizon { get; set; } = PositionH.Right;

        [LocalManager.LocalizedCategoryAttribute("Layout")]
        [LocalManager.LocalizedDisplayNameAttribute("VerticalAlignmentType")]
        [LocalManager.LocalizedDescriptionAttribute("VerticalAlignmentTypeDes")]
        [PropertyPageTypeConverter(typeof(PositionVTypeConverter))]
        [TypeConverter(typeof(PositionVTypeConverter))]
        public PositionV PositionVertical { get; set; } = PositionV.Bottom;

        [LocalManager.LocalizedCategoryAttribute("Slideshow")]
        [LocalManager.LocalizedDisplayNameAttribute("ShuffleSlideshowType")]
        [LocalManager.LocalizedDescriptionAttribute("ShuffleSlideshowTypeDes")]
        public bool ShuffleSlideshow { get; set; } = true;

        [LocalManager.LocalizedCategoryAttribute("Layout")]
        [LocalManager.LocalizedDisplayNameAttribute("SoftEdgeX")]
        [LocalManager.LocalizedDescriptionAttribute("SoftEdgeDes")]
        public int SoftEdgeX { get; set; } = 0;

        [LocalManager.LocalizedCategoryAttribute("Layout")]
        [LocalManager.LocalizedDisplayNameAttribute("SoftEdgeY")]
        [LocalManager.LocalizedDescriptionAttribute("SoftEdgeDes")]
        public int SoftEdgeY { get; set; } = 0;

        [LocalManager.LocalizedCategoryAttribute("Slideshow")]
        [LocalManager.LocalizedDisplayNameAttribute("UpdateIntervalType")]
        [LocalManager.LocalizedDescriptionAttribute("UpdateIntervalTypeDes")]
        [PropertyPageTypeConverter(typeof(TimeSpanConverter))]
        [TypeConverter(typeof(TimeSpanConverter))]
        public TimeSpan UpdateImageInterval { get; set; } = TimeSpan.FromMinutes(30);

        [LocalManager.LocalizedCategoryAttribute("Layout")]
        [LocalManager.LocalizedDisplayNameAttribute("ViewBoxPointX")]
        [LocalManager.LocalizedDescriptionAttribute("ViewBoxPointXDes")]
        public double ViewBoxPointX { get; set; } = 0;

        [LocalManager.LocalizedCategoryAttribute("Layout")]
        [LocalManager.LocalizedDisplayNameAttribute("ViewBoxPointY")]
        [LocalManager.LocalizedDescriptionAttribute("ViewBoxPointYDes")]
        public double ViewBoxPointY { get; set; } = 0;

        public static async Task<Setting> CreateAsync()
        {
            var inst = new Setting();
            await inst.LoadAsync();
            return inst;
        }


        public static Task<Setting> GetLiveInstanceAsync()
        {
            return LiveModel.GetValueAsync();
        }

        public void Load()
        {
            ThreadHelper.JoinableTaskFactory.Run(LoadAsync);
        }

        public async Task LoadAsync()
        {
            var manager = await SettingsManager.GetValueAsync();
            var settingsStore = manager.GetReadOnlySettingsStore(SettingsScope.UserSettings);
            if (!settingsStore.CollectionExists(CollectionName)) return;


            foreach (var property in GetOptionProperties())
                try
                {
                    var serializedProp = settingsStore.GetString(CollectionName, property.Name);
                    var value = JsonConvert.DeserializeObject(serializedProp, property.PropertyType,
                        new JsonSerializerSettings());
                    property.SetValue(this, value);
                }
                catch (Exception e)
                {
                    Debug.Write(e);
                }
        }

        public void Save()
        {
            ThreadHelper.JoinableTaskFactory.Run(SaveAsync);
        }


        public async Task SaveAsync()
        {
            var manager = await SettingsManager.GetValueAsync();
            var settingsStore = manager.GetWritableSettingsStore(SettingsScope.UserSettings);
            if (!settingsStore.CollectionExists(CollectionName)) settingsStore.CreateCollection(CollectionName);

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

        private IEnumerable<PropertyInfo> GetOptionProperties()
        {
            return GetType().GetProperties().Where(p => p.PropertyType.IsSerializable && p.PropertyType.IsPublic);
        }

        private static async Task<ShellSettingsManager> GetSettingsManagerAsync()
        {
            var svc =
                await AsyncServiceProvider.GlobalProvider.GetServiceAsync(typeof(SVsSettingsManager)) as
                    IVsSettingsManager;
            Assumes.Present(svc);
            return new ShellSettingsManager(svc);
        }

        private void OnApplyChanged()
        {
            OnChanged?.RaiseEvent(this, EventArgs.Empty);
        }
    }

    [CLSCompliant(false)]
    [ComVisible(true)]
    [Guid("12d9a45f-ec0b-4a96-88dc-b0cba1f4789a")]
    public enum PositionV
    {
        Top,
        Bottom,
        Center
    }

    [CLSCompliant(false)]
    [ComVisible(true)]
    [Guid("8b2e3ece-fbf7-43ba-b369-3463726b828d")]
    public enum PositionH
    {
        Left,
        Right,
        Center
    }

    [CLSCompliant(false)]
    [ComVisible(true)]
    [Guid("5C96CFAA-FE54-49A9-8AB7-E85B66731228")]
    public enum ImageBackgroundType
    {
        Single = 0,
        Slideshow = 1,
        SingleEach = 2
    }

    [CLSCompliant(false)]
    [ComVisible(true)]
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
        public static Stretch ConvertTo(this ImageStretch source)
        {
            switch (source)
            {
                case ImageStretch.Fill:
                    return Stretch.Fill;
                case ImageStretch.None:
                    return Stretch.None;
                case ImageStretch.Uniform:
                    return Stretch.Uniform;
                case ImageStretch.UniformToFill:
                    return Stretch.UniformToFill;
            }

            return Stretch.None;
        }
    }

    public static class PositionConverter
    {
        public static AlignmentY ConvertTo(this PositionV source)
        {
            switch (source)
            {
                case PositionV.Bottom:
                    return AlignmentY.Bottom;
                case PositionV.Center:
                    return AlignmentY.Center;
                case PositionV.Top:
                    return AlignmentY.Top;
            }

            return AlignmentY.Bottom;
        }

        public static AlignmentX ConvertTo(this PositionH source)
        {
            switch (source)
            {
                case PositionH.Left:
                    return AlignmentX.Left;
                case PositionH.Center:
                    return AlignmentX.Center;
                case PositionH.Right:
                    return AlignmentX.Right;
            }

            return AlignmentX.Right;
        }

        public static HorizontalAlignment ConvertToHorizontalAlignment(this PositionH source)
        {
            switch (source)
            {
                case PositionH.Left:
                    return HorizontalAlignment.Left;
                case PositionH.Center:
                    return HorizontalAlignment.Center;
                case PositionH.Right:
                    return HorizontalAlignment.Right;
            }

            return HorizontalAlignment.Right;
        }

        public static VerticalAlignment ConvertToVerticalAlignment(this PositionV source)
        {
            switch (source)
            {
                case PositionV.Bottom:
                    return VerticalAlignment.Bottom;
                case PositionV.Center:
                    return VerticalAlignment.Center;
                case PositionV.Top:
                    return VerticalAlignment.Top;
            }

            return VerticalAlignment.Bottom;
        }
    }
}