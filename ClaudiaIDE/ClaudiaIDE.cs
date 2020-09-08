using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using ClaudiaIDE.Settings;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text.Editor;

namespace ClaudiaIDE
{
    /// <summary>
    /// Adornment class that draws a square box in the top right hand corner of the viewport
    /// </summary>
    public class ClaudiaIDE
    {
        private readonly IAdornmentLayer _adornmentLayer;
        private readonly Dictionary<int, DependencyObject> _defaultThemeColor = new Dictionary<int, DependencyObject>();
        private readonly Canvas _editorCanvas = new Canvas {IsHitTestVisible = false};
        private readonly IWpfTextView _view;
        private bool _hasImage;
        private bool _isMainWindow;
        private bool _isRootWindow;
        private Brush _transparentBrush;
        private DependencyObject _wpfTextViewHost;

        /// <summary>
        /// Creates a square image and attaches an event handler to the layout changed event that
        /// adds the the square in the upper right-hand corner of the TextView via the adornment layer
        /// </summary>
        /// <param name="view">The <see cref="IWpfTextView"/> upon which the adornment will be drawn</param>
        public ClaudiaIDE(IWpfTextView view)
        {
            _view = view;
            _adornmentLayer = view.GetAdornmentLayer("ClaudiaIDE");
            view.LayoutChanged += OnViewLayoutChanged;
            view.Closed += OnViewClosed;
            view.BackgroundBrushChanged += OnViewBackgroundBrushChanged;
            //should be on ui thread here
            Setting.Instance.OnChanged.AddEventHandler(ReloadSettings);
            ImageProvider.Instance.Loader.ImageChanged += InvokeChangeImage;
            ImageProvider.Instance.ProviderChanged += OnProviderChanged;
            VSColorTheme.ThemeChanged += OnThemeChanged;
            SetTransparentBrush();
            ChangeImage();
            RefreshAdornment();
        }

        private DependencyObject WpfTextViewHost
        {
            get
            {
                if (_wpfTextViewHost != null) return _wpfTextViewHost;
                _wpfTextViewHost = FindUI(_editorCanvas,
                    _isRootWindow ? DependencyType.WpfTextView : DependencyType.WpfMultiViewHost);
                if (_wpfTextViewHost != null)
                    RenderOptions.SetBitmapScalingMode(_wpfTextViewHost, BitmapScalingMode.Fant);
                return _wpfTextViewHost;
            }
        }

        private void OnThemeChanged(ThemeChangedEventArgs e)
        {
            SetTransparentBrush();
            SetCanvasBackground();
        }


        private void SetTransparentBrush()
        {
            var color = VSColorTheme.GetThemedColor(TreeViewColors.BackgroundColorKey);
            _transparentBrush =
                new SolidColorBrush(Color.FromArgb(color.A < 255 ? color.A : (byte) 0, color.R, color.G, color.B));
        }

        private void OnProviderChanged(object sender, EventArgs e)
        {
            ImageProvider.Instance.Loader.ImageChanged += InvokeChangeImage;
            ChangeImage();
        }

        private void OnViewBackgroundBrushChanged(object s, BackgroundBrushChangedEventArgs e)
        {
            _hasImage = false;
            SetCanvasBackground();
        }

        private void OnViewClosed(object s, EventArgs e)
        {
            ImageProvider.Instance.ProviderChanged -= OnProviderChanged;
            ImageProvider.Instance.Loader.ImageChanged -= InvokeChangeImage;
            Setting.Instance.OnChanged.RemoveEventHandler(ReloadSettings);
        }

        private void OnViewLayoutChanged(object s, TextViewLayoutChangedEventArgs e)
        {
            if (!_hasImage) ChangeImage();
            else RefreshBackground();
        }

        private void InvokeChangeImage(object sender, EventArgs e)
        {
            ChangeImage();
        }

        private void ReloadSettings(object sender, EventArgs e)
        {
            _hasImage = false;
            ChangeImage();
        }

        private void ChangeImage()
        {
            ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
            {
                var loadImageTask = ImageProvider.Instance.Loader.GetBitmapAsync();
                var settings = await Setting.GetLiveInstanceAsync();
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                SetCanvasBackground();
                if (WpfTextViewHost == null) return;

                var opacity = settings.ExpandToIDE && _isMainWindow ? 0.0 : settings.Opacity;
                if (_isRootWindow)
                {
                    var grid = new Grid
                    {
                        Name = "ClaudiaIdeImage",
                        HorizontalAlignment = HorizontalAlignment.Stretch,
                        VerticalAlignment = VerticalAlignment.Stretch,
                        IsHitTestVisible = false
                    };
                    var img = new Image
                    {
                        Source = await loadImageTask,
                        Stretch = settings.ImageStretch.ConvertTo(),
                        HorizontalAlignment = settings.PositionHorizon.ConvertToHorizontalAlignment(),
                        VerticalAlignment = settings.PositionVertical.ConvertToVerticalAlignment(),
                        Opacity = opacity,
                        IsHitTestVisible = false
                    };
                    grid.Children.Insert(0, img);
                    Grid.SetRowSpan(grid, 3);
                    Grid.SetColumnSpan(grid, 3);
                    if (VisualTreeHelper.GetParent(WpfTextViewHost) is Grid p)
                    {
                        foreach (var c in p.Children)
                        {
                            if ((c as Grid)?.Name != "ClaudiaIdeImage") continue;
                            p.Children.Remove(c as UIElement);
                            break;
                        }

                        p.Children.Insert(0, grid);
                    }
                }
                else
                {
                    var nib = new ImageBrush(await loadImageTask)
                    {
                        Stretch = settings.ImageStretch.ConvertTo(),
                        AlignmentX = settings.PositionHorizon.ConvertTo(),
                        AlignmentY = settings.PositionVertical.ConvertTo(),
                        Opacity = opacity,
                        Viewbox = new Rect(new Point(settings.ViewBoxPointX, 0), new Size(1, 1))
                    };
                    _wpfTextViewHost.SetValue(Panel.BackgroundProperty, nib);
                }

                _hasImage = true;
            }).FileAndForget("claudiaide/claudiaide/changeimage");
        }

        private void RefreshBackground()
        {
            SetCanvasBackground();
            if (WpfTextViewHost == null) return;
            var settings = Setting.Instance;
            var opacity = settings.ExpandToIDE && _isMainWindow ? 0.0 : settings.Opacity;

            var background = WpfTextViewHost.GetType()
                .GetProperty("Background")
                ?.GetValue(WpfTextViewHost) as ImageBrush;
            ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                if (background == null && _isRootWindow)
                {
                    var element = (UIElement) WpfTextViewHost;
                    var c = element.Opacity;
                    element.Opacity = c < 0.01 ? 0.01 : c - 0.01;
                    element.Opacity = c;
                }
                else
                {
                    Debug.Assert(background != null, nameof(background) + " != null");
                    background.Opacity = opacity < 0.01 ? 0.01 : opacity - 0.01;
                    background.Opacity = opacity;
                }
            }).FileAndForget("claudiaide/claudiaide/refreshbackground");
        }

        private void RefreshAdornment()
        {
            _adornmentLayer.RemoveAdornmentsByTag("ClaudiaIDE");
            _adornmentLayer.AddAdornment(AdornmentPositioningBehavior.ViewportRelative,
                null,
                "ClaudiaIDE",
                _editorCanvas,
                null);
        }


        private static DependencyType GetDependencyType(DependencyObject obj)
        {
            var fullName = obj.GetType().FullName;
            return fullName switch
            {
                "Microsoft.VisualStudio.Text.Editor.Implementation.WpfTextView" => DependencyType.WpfTextView,
                "Microsoft.VisualStudio.Editor.Implementation.WpfMultiViewHost" => DependencyType.WpfMultiViewHost,
                "Microsoft.VisualStudio.Text.Editor.Implementation.WpfTextViewHost" => DependencyType.WpfTextViewHost,
                "Microsoft.VisualStudio.PlatformUI.MainWindow" => DependencyType.MainWindow,
                "System.Windows.Controls.Grid" => DependencyType.Grid,
                "System.Windows.Controls.Border" => DependencyType.Border,
                _ => DependencyType.Other
            };
        }


        private void SetCanvasBackground()
        {
            GetViewType();
            var isTransparent = true;
            var current = _editorCanvas as DependencyObject;
            var bottomGridBackgroundSet = false;
            var bottomBorderBackgroundSet = false;
            var settings = Setting.Instance;
            while (current != null)
            {
                var objname = current.GetType().GetProperty("Name")?.GetValue(current) as string;

                if (!string.IsNullOrEmpty(objname) &&
                    (objname.Equals("RootGrid", StringComparison.OrdinalIgnoreCase)
                     || objname.Equals("MainWindow", StringComparison.OrdinalIgnoreCase))) return;

                switch (GetDependencyType(current))
                {
                    case DependencyType.WpfTextView when _isRootWindow:
                    case DependencyType.WpfTextView
                        when FindUI(current, DependencyType.WpfMultiViewHost) == null:
                        return;
                    case DependencyType.WpfTextView:
                        break;
                    case DependencyType.Grid:
                        if (!bottomGridBackgroundSet && settings.ExpandToIDE)
                        {
                            SetSemiTransparentBackground(current);
                            bottomGridBackgroundSet = true;
                        }
                        else
                        {
                            SetBackgroundToTransparent(current, true);
                        }

                        break;
                    case DependencyType.Border:
                        if (!bottomBorderBackgroundSet && settings.ExpandToIDE)
                        {
                            SetSemiTransparentBackground(current);
                            bottomBorderBackgroundSet = true;
                        }
                        else
                        {
                            SetBackgroundToTransparent(current, true);
                        }

                        break;
                    case DependencyType.WpfMultiViewHost:
                        isTransparent = false;
                        break;
                    default:
                        SetBackgroundToTransparent(current, isTransparent);
                        break;
                }

                current = current is Visual || current is Visual3D
                    ? VisualTreeHelper.GetParent(current)
                    : LogicalTreeHelper.GetParent(current);
            }
        }

        private void SetSemiTransparentBackground(DependencyObject obj)
        {
            var prop = obj.GetType().GetProperty("Background");
            prop?.SetValue(obj, _transparentBrush);
        }

        private void GetViewType()
        {
            if (!(_view is DependencyObject parent))
                throw new InvalidOperationException("Could not cast view to DependencyObject.");
            DependencyObject current;

            do
            {
                current = parent;
                parent = current is Visual || current is Visual3D
                    ? VisualTreeHelper.GetParent(current)
                    : LogicalTreeHelper.GetParent(current);
            } while (parent != null);

            var type = GetDependencyType(current);

            _isRootWindow = type == DependencyType.WpfMultiViewHost || type == DependencyType.WpfTextViewHost;
            _isMainWindow = type == DependencyType.MainWindow;
            // maybe editor with designer area or other view window
        }

        private static DependencyObject FindUI(DependencyObject d, DependencyType type)
        {
            var current = d;
            do
            {
                if (GetDependencyType(current) == type) return current;
                current = current is Visual || current is Visual3D
                    ? VisualTreeHelper.GetParent(current)
                    : LogicalTreeHelper.GetParent(current);
            } while (current != null);

            return null;
        }

        private void SetBackgroundToTransparent(DependencyObject d, bool isTransparent)
        {
            var property = d.GetType().GetProperty("Background");
            if (!(property?.GetValue(d) is Brush brush)) return;
            ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                try
                {
                    var hash = d.GetHashCode();
                    if (isTransparent)
                    {
                        if (!_defaultThemeColor.ContainsKey(hash))
                            _defaultThemeColor[hash] = brush;
                        property.SetValue(d, Brushes.Transparent);
                    }
                    else if (_defaultThemeColor.TryGetValue(hash, out var obj))
                    {
                        property.SetValue(d, (Brush) obj);
                    }
                }
                catch (Exception e)
                {
                    throw new InvalidOperationException("Error: SetBackgroundToTransparent failed.", e);
                }
            });
        }

        private enum DependencyType
        {
            WpfTextView,
            WpfMultiViewHost,
            WpfTextViewHost,
            MainWindow,
            Grid,
            Border,
            Other
        }
    }
}