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
        private Setting _settings;
        private Window _mainWindow;
        private Image _current;

        protected override async Task InitializeAsync(CancellationToken cancellationToken,
            IProgress<ServiceProgressData> progress)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
            await NextImage.InitializeAsync(this);
            await PauseSlideshow.InitializeAsync(this);
            _settings = await Setting.GetLiveInstanceAsync();
            Debug.Assert(Application.Current.MainWindow != null,
                "Application.Current.MainWindow != null");
            _mainWindow = Application.Current.MainWindow;
            if (_mainWindow.IsLoaded) Setup();
            else _mainWindow.Loaded += (sender, args) => Setup();
            _mainWindow.Closing += (s, e) =>
            {
                ImageProvider.Instance.ProviderChanged -= OnProviderChanged;
                ImageProvider.Instance.Loader.ImageChanged -= InvokeChangeImage;
                _settings.OnChanged.RemoveEventHandler(ReloadSettings);
            };
        }

        private void Setup()
        {
            ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                _settings.OnChanged.AddEventHandler(ReloadSettings);
                Debug.Assert(ImageProvider.Instance != null,
                    "ImageProvider.Instance != null");
                ImageProvider.Instance.Loader.ImageChanged += InvokeChangeImage;
                ImageProvider.Instance.ProviderChanged += OnProviderChanged;
                ReloadSettings(this, EventArgs.Empty);
            });
        }

        private static void SetDockTargetBackgrounds(IEnumerable<DependencyObject> dockTargets)
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

        private async Task ChangeImageAsync()
        {
            var loadImageTask = ImageProvider.Instance.Loader.GetBitmapAsync();

            await JoinableTaskFactory.SwitchToMainThreadAsync();
            var rRootGrid = (Grid) _mainWindow.Template.FindName("RootGrid", _mainWindow);
            if (_settings.ImageBackgroundType == ImageBackgroundType.Single || !_settings.ExpandToIDE)
            {
                rRootGrid.Children.Remove(_current);
                _current = null;
            }

            if (!_settings.ExpandToIDE)
            {
                return;
            }

            if (_settings.ImageBackgroundType == ImageBackgroundType.Single || _current == null)
            {
                _current = new Image
                {
                    Source = await loadImageTask,
                    Stretch = _settings.ImageStretch.ConvertTo(),
                    HorizontalAlignment = _settings.PositionHorizon.ConvertToHorizontalAlignment(),
                    VerticalAlignment = _settings.PositionVertical.ConvertToVerticalAlignment(),
                    Opacity = _settings.Opacity
                };

                Grid.SetRowSpan(_current, 4);
                RenderOptions.SetBitmapScalingMode(_current, BitmapScalingMode.Fant);

                rRootGrid.Children.Insert(0, _current);

                var docktargets = rRootGrid.Descendants<DependencyObject>().Where(x =>
                    x.GetType().FullName == "Microsoft.VisualStudio.PlatformUI.Shell.Controls.DockTarget");
                SetDockTargetBackgrounds(docktargets);
            }
            else
            {
                _current.AnimateImageSourceChange(
                    await loadImageTask,
                    n =>
                    {
                        n.Stretch = _settings.ImageStretch.ConvertTo();
                        n.HorizontalAlignment = _settings.PositionHorizon.ConvertToHorizontalAlignment();
                        n.VerticalAlignment = _settings.PositionVertical.ConvertToVerticalAlignment();
                    },
                    new AnimateImageChangeParams
                    {
                        FadeTime = _settings.ImageFadeAnimationInterval,
                        TargetOpacity = _settings.Opacity
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