using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ClaudiaIDE.Helpers;
using ClaudiaIDE.MenuCommands;
using ClaudiaIDE.Options;
using ClaudiaIDE.Settings;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Task = System.Threading.Tasks.Task;

namespace ClaudiaIDE
{
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [InstalledProductRegistration("#110", "#112", "2.2.10", IconResourceID = 400)]
    [ProvideOptionPage(typeof(ClaudiaIdeOptionPageGrid), "ClaudiaIDE", "General", 110, 116, true)]
    [Guid("7442ac19-889b-4699-a817-e6e054877ee3")]
    [ProvideAutoLoad(UIContextGuids.EmptySolution, PackageAutoLoadFlags.BackgroundLoad)]
    [ProvideAutoLoad(UIContextGuids.NoSolution, PackageAutoLoadFlags.BackgroundLoad)]
    [ProvideAutoLoad(UIContextGuids.SolutionExists, PackageAutoLoadFlags.BackgroundLoad)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    public sealed class ClaudiaIdePackage : AsyncPackage
    {
        private Window _mainWindow;
        private Grid _rootGrid;
        private Image _current;

        private bool _hasTransparentTheme;

        //assume transparent theme if the color resource identified by this key has an alpha less than 255 (opaque).
        private static readonly ThemeResourceKey TransparentThemeDetectKey = TreeViewColors.BackgroundColorKey;

        protected override async Task InitializeAsync(CancellationToken cancellationToken,
            IProgress<ServiceProgressData> progress)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
            await NextImage.InitializeAsync(this);
            await PauseSlideshow.InitializeAsync(this);
            Debug.Assert(Application.Current.MainWindow != null,
                "Application.Current.MainWindow != null");
            _mainWindow = Application.Current.MainWindow;
            if (_mainWindow.IsLoaded) Setup();
            else _mainWindow.Loaded += (sender, args) => Setup();
            _mainWindow.Closing += (s, e) =>
            {
                ImageProvider.Instance.ProviderChanged -= OnProviderChanged;
                ImageProvider.Instance.Loader.ImageChanged -= InvokeChangeImage;
            };
        }


        private void DetectTransparentTheme()
        {
            var color = VSColorTheme.GetThemedColor(TransparentThemeDetectKey);
            _hasTransparentTheme = color.A < 255;
        }

        private void Setup()
        {
            ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
            {
                var settings = await Setting.GetLiveInstanceAsync();
                settings.OnChanged.AddEventHandler(ReloadSettings);
                VSColorTheme.ThemeChanged += args => DetectTransparentTheme();
                DetectTransparentTheme();
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                //root grid shouldn't change
                _rootGrid = (Grid) _mainWindow.Template.FindName("RootGrid", _mainWindow);
                Debug.Assert(ImageProvider.Instance != null,
                    "ImageProvider.Instance != null");
                ImageProvider.Instance.Loader.ImageChanged += InvokeChangeImage;
                ImageProvider.Instance.ProviderChanged += OnProviderChanged;
                ReloadSettings(this, EventArgs.Empty);
            });
        }

        private const string DockTargetName = "Microsoft.VisualStudio.PlatformUI.Shell.Controls.DockTarget";

        private IEnumerable<DependencyObject> GetDockTargets()
        {
            return _rootGrid.Descendants<DependencyObject>().Where(x =>
                x.GetType().FullName?.Equals(DockTargetName, StringComparison.OrdinalIgnoreCase) ?? false);
        }

        private static void SetTransparentBackgrounds(IEnumerable<DependencyObject> dockTargets)
        {
            foreach (var docktarget in dockTargets)
            {
                var grids = docktarget.Descendants<Grid>();
                foreach (var g in grids)
                {
                    if (g == null) continue;
                    var prop = g.GetType().GetProperty("Background");
                    if (!(prop?.GetValue(g) is SolidColorBrush bg) || bg.Color.A == 0x00) continue;
                    prop.SetValue(g, new SolidColorBrush(Color.FromArgb(0, bg.Color.R, bg.Color.G, bg.Color.B)));
                }
            }
        }

        private void ResetImage()
        {
            if (_current == null) return;
            _rootGrid.Children.Remove(_current);
            _current = null;
        }

        private async Task ChangeImageAsync()
        {
            var loadImageTask = ImageProvider.Instance.Loader.GetBitmapAsync();
            var settings = await Setting.GetLiveInstanceAsync();
            await JoinableTaskFactory.SwitchToMainThreadAsync();
            if (!settings.ExpandToIDE)
            {
                ResetImage();
                return;
            }

            if (settings.ImageBackgroundType == ImageBackgroundType.Single || _current == null)
            {
                ResetImage();
                _current = new Image
                {
                    Source = await loadImageTask,
                    Stretch = settings.ImageStretch.ConvertTo(),
                    HorizontalAlignment = settings.PositionHorizon.ConvertToHorizontalAlignment(),
                    VerticalAlignment = settings.PositionVertical.ConvertToVerticalAlignment(),
                    Opacity = settings.Opacity
                };

                Grid.SetRowSpan(_current, 4);
                RenderOptions.SetBitmapScalingMode(_current, BitmapScalingMode.Fant);
                _rootGrid.Children.Insert(0, _current);

                if (!_hasTransparentTheme)
                {
                    SetTransparentBackgrounds(GetDockTargets());
                }
            }
            else
            {
                _current.AnimateImageSourceChange(
                    await loadImageTask,
                    n =>
                    {
                        n.Stretch = settings.ImageStretch.ConvertTo();
                        n.HorizontalAlignment = settings.PositionHorizon.ConvertToHorizontalAlignment();
                        n.VerticalAlignment = settings.PositionVertical.ConvertToVerticalAlignment();
                    },
                    new AnimateImageChangeParams
                    {
                        FadeTime = settings.ImageFadeAnimationInterval,
                        TargetOpacity = settings.Opacity
                    }
                );
            }
        }

        private void OnProviderChanged(object sender, EventArgs args)
        {
            ImageProvider.Instance.Loader.ImageChanged += InvokeChangeImage;
        }

        private void InvokeChangeImage(object sender, EventArgs e)
        {
            ThreadHelper.JoinableTaskFactory.RunAsync(ChangeImageAsync)
                .FileAndForget("claudiaide/claudiaidepackage/invokechangeimage");
        }

        private void ReloadSettings(object sender, EventArgs e)
        {
            InvokeChangeImage(this, EventArgs.Empty);
        }
    }
}