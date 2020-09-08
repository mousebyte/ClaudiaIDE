using System;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.InteropServices;
using ClaudiaIDE.Settings;
using Microsoft.VisualStudio.Shell;
using System.Drawing.Design;
using System.Windows.Forms.Design;
using System.Windows.Forms;
using System.IO;
using ClaudiaIDE.Localized;

namespace ClaudiaIDE.Options
{
    [ClassInterface(ClassInterfaceType.AutoDual)]
    [CLSCompliant(false), ComVisible(true)]
    [Guid("441f0a76-1771-41c2-817c-81b8b03fb0e8")]
    public class ClaudiaIdeOptionPageGrid : DialogPage
    {
        private Setting _model;

        public ClaudiaIdeOptionPageGrid()
        {
            _model = ThreadHelper.JoinableTaskFactory.Run(Setting.CreateAsync);
        }

        public override object AutomationObject => _model;
        public override void LoadSettingsFromStorage()
        {
            _model.Load();
        }

        public override void SaveSettingsToStorage()
        {
            _model.Save();
        }
    }

    public class ImageBackgroundTypeConverter : EnumConverter
    {
        public ImageBackgroundTypeConverter()
            : base(typeof(ImageBackgroundType))
        {

        }

        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            if (sourceType == typeof(string)) return true;

            return base.CanConvertFrom(context, sourceType);
        }

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            string str = value as string;

            if (str != null)
            {
                if (str == "Single") return ImageBackgroundType.Single;
                else if (str == "Slideshow") return ImageBackgroundType.Slideshow;
                else if (str == "SingleEach") return ImageBackgroundType.SingleEach;
                else return ImageBackgroundType.Single;
            }

            return base.ConvertFrom(context, culture, value);
        }

        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            if (destinationType == typeof(string))
            {
                string result = null;
                if ((int)value == 0)
                {
                    result = "Single";
                }
                else if ((int)value == 1)
                {
                    result = "Slideshow";
                }
                else if ((int)value == 2)
                {
                    result = "SingleEach";
                }

                if (result != null) return result;
            }

            return base.ConvertTo(context, culture, value, destinationType);
        }
    }

    public class PositionHTypeConverter : EnumConverter
    {
        public PositionHTypeConverter()
            : base(typeof(PositionH))
        {

        }

        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            if (sourceType == typeof(string)) return true;

            return base.CanConvertFrom(context, sourceType);
        }

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            string str = value as string;

            if (str != null)
            {
                if (str == "Right") return PositionH.Right;
                if (str == "Left") return PositionH.Left;
            }

            return base.ConvertFrom(context, culture, value);
        }

        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            if (destinationType == typeof(string))
            {
                string result = null;
                if ((int)value == 0)
                {
                    result = "Left";
                }
                else if ((int)value == 1)
                {
                    result = "Right";
                }

                if (result != null) return result;
            }

            return base.ConvertTo(context, culture, value, destinationType);
        }
    }

    public class PositionVTypeConverter : EnumConverter
    {
        public PositionVTypeConverter()
            : base(typeof(PositionV))
        {

        }

        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            if (sourceType == typeof(string)) return true;

            return base.CanConvertFrom(context, sourceType);
        }

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            string str = value as string;

            if (str != null)
            {
                if (str == "Top") return PositionV.Top;
                if (str == "Bottom") return PositionV.Bottom;
            }

            return base.ConvertFrom(context, culture, value);
        }

        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            if (destinationType == typeof(string))
            {
                string result = null;
                if ((int)value == 0)
                {
                    result = "Top";
                }
                else if ((int)value == 1)
                {
                    result = "Bottom";
                }

                if (result != null) return result;
            }

            return base.ConvertTo(context, culture, value, destinationType);
        }
    }

    public class ImageStretchTypeConverter : EnumConverter
    {
        public ImageStretchTypeConverter()
            : base(typeof(ImageStretch))
        {

        }

        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            if (sourceType == typeof(string)) return true;

            return base.CanConvertFrom(context, sourceType);
        }

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            string str = value as string;

            if (str != null)
            {
                if (str == "None") return ImageStretch.None;
                if (str == "Uniform") return ImageStretch.Uniform;
                if (str == "UniformToFill") return ImageStretch.UniformToFill;
                if (str == "Fill") return ImageStretch.Fill;
            }

            return base.ConvertFrom(context, culture, value);
        }

        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            if (destinationType == typeof(string))
            {
                string result = null;
                if ((int)value == 0)
                {
                    result = "None";
                }
                else if ((int)value == 1)
                {
                    result = "Uniform";
                }
                else if ((int)value == 2)
                {
                    result = "UniformToFill";
                }
                else if ((int)value == 3)
                {
                    result = "Fill";
                }

                if (result != null) return result;
            }

            return base.ConvertTo(context, culture, value, destinationType);
        }
    }

    internal class BrowseDirectory : UITypeEditor
    {
        public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext context)
        {
            return UITypeEditorEditStyle.Modal;
        }
        public override object EditValue(ITypeDescriptorContext context, IServiceProvider provider, object value)
        {
            IWindowsFormsEditorService edSvc = (IWindowsFormsEditorService)provider.GetService(typeof(IWindowsFormsEditorService));
            if (edSvc != null)
            {
                var open = new FolderBrowserDialog();
                if (open.ShowDialog() == DialogResult.OK)
                {
                    return open.SelectedPath;
                }
            }
            return value;
        }
        public override bool GetPaintValueSupported(System.ComponentModel.ITypeDescriptorContext context)
        {
            return false;
        }
    }

    internal class BrowseFile : UITypeEditor
    {
        public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext context)
        {
            return UITypeEditorEditStyle.Modal;
        }
        public override object EditValue(ITypeDescriptorContext context, IServiceProvider provider, object value)
        {
            IWindowsFormsEditorService edSvc = (IWindowsFormsEditorService)provider.GetService(typeof(IWindowsFormsEditorService));
            if (edSvc != null)
            {
                OpenFileDialog open = new OpenFileDialog();
                open.FileName = Path.GetFileName((string)value);

                try
                {
                    open.InitialDirectory = Path.GetDirectoryName((string)value);
                }
                catch (Exception)
                {
                }

                if (open.ShowDialog() == DialogResult.OK)
                {
                    return open.FileName;
                }
            }
            return value;
        }
        public override bool GetPaintValueSupported(ITypeDescriptorContext context)
        {
            return false;
        }
    }

}
