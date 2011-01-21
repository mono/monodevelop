using System;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using Microsoft.WindowsAPICodePack.ShellExtensions.Interop;
using Microsoft.WindowsAPICodePack.ShellExtensions.Resources;
using Microsoft.WindowsAPICodePack.Shell;

namespace Microsoft.WindowsAPICodePack.ShellExtensions
{
    /// <summary>
    /// This is the base class for all WPF-based preview handlers and provides their basic functionality.
    /// To create a custom preview handler that contains a WPF user control,
    /// a class must derive from this, use the <typeparamref name="PreviewHandlerAttribute"/>,
    /// and implement 1 or more of the following interfaces: 
    /// <typeparamref name="IPreviewFromStream"/>, 
    /// <typeparamref name="IPreviewFromShellObject"/>, 
    /// <typeparamref name="IPreviewFromFile"/>.   
    /// </summary>
    public abstract class WpfPreviewHandler : PreviewHandler, IDisposable
    {
        HwndSource _source = null;
        private IntPtr _parentHandle = IntPtr.Zero;
        private NativeRect _bounds;

        /// <summary>
        /// This control must be populated by the deriving class before the preview is shown.
        /// </summary>
        public UserControl Control { get; protected set; }

        /// <summary>
        /// Throws an exception if the Control property has not been populated.
        /// </summary>
        protected void ThrowIfNoControl()
        {
            if (Control == null)
            {
                throw new InvalidOperationException(LocalizedMessages.PreviewHandlerControlNotInitialized);
            }
        }

        /// <summary>
        /// Updates the placement of the Control.
        /// </summary>
        protected void UpdatePlacement()
        {
            if (_source != null)
            {
                HandlerNativeMethods.SetParent(_source.Handle, _parentHandle);

                HandlerNativeMethods.SetWindowPos(_source.Handle, new IntPtr((int)SetWindowPositionInsertAfter.Top),
                0, 0, Math.Abs(_bounds.Left - _bounds.Right), Math.Abs(_bounds.Top - _bounds.Bottom), SetWindowPositionOptions.ShowWindow);
            }
        }
                
        protected override void SetParentHandle(IntPtr handle)
        {
            _parentHandle = handle;
            UpdatePlacement();
        }

        protected override void Initialize()
        {
            if (_source == null)
            {
                ThrowIfNoControl();

                HwndSourceParameters p = new HwndSourceParameters();
                p.WindowStyle = (int)(WindowStyles.Child | WindowStyles.Visible | WindowStyles.ClipSiblings);
                p.ParentWindow = _parentHandle;
                p.Width = Math.Abs(_bounds.Left - _bounds.Right);
                p.Height = Math.Abs(_bounds.Top - _bounds.Bottom);

                _source = new HwndSource(p);
                _source.CompositionTarget.BackgroundColor = Brushes.WhiteSmoke.Color;
                _source.RootVisual = (Visual)Control.Content;
            }
            UpdatePlacement();
        }

        protected override IntPtr Handle
        {
            get
            {
                {
                    if (_source == null)
                    {
                        throw new InvalidOperationException(LocalizedMessages.WpfPreviewHandlerNoHandle);
                    }
                    return _source.Handle;
                }
            }
        }

        protected override void UpdateBounds(NativeRect bounds)
        {
            _bounds = bounds;
            UpdatePlacement();
        }

        protected override void HandleInitializeException(Exception caughtException)
        {
            if (caughtException == null) { return; }

            TextBox text = new TextBox
            {
                IsReadOnly = true,
                MaxLines = 20,
                Text = caughtException.ToString()
            };
            Control = new UserControl() { Content = text };
        }

        protected override void SetFocus()
        {
            Control.Focus();
        }

        protected override void SetBackground(int argb)
        {
            Control.Background = new SolidColorBrush(Color.FromArgb(
                (byte)((argb >> 24) & 0xFF), //a         
                (byte)((argb >> 16) & 0xFF), //r
                (byte)((argb >> 8) & 0xFF), //g
                (byte)(argb & 0xFF))); //b
        }

        protected override void SetForeground(int argb)
        {
            Control.Foreground = new SolidColorBrush(Color.FromArgb(
                 (byte)((argb >> 24) & 0xFF), //a                
                 (byte)((argb >> 16) & 0xFF), //r
                 (byte)((argb >> 8) & 0xFF), //g
                 (byte)(argb & 0xFF))); //b                 
        }

        protected override void SetFont(Interop.LogFont font)
        {
            if (font == null) { throw new ArgumentNullException("font"); }
            
            Control.FontFamily = new FontFamily(font.FaceName);
            Control.FontSize = font.Height;
            Control.FontWeight = font.Weight > 0 && font.Weight < 1000 ?
                System.Windows.FontWeight.FromOpenTypeWeight(font.Weight) :
                System.Windows.FontWeights.Normal;
        }

        #region IDisposable Members

        /// <summary>
        /// Preview handler control finalizer
        /// </summary>
        ~WpfPreviewHandler()
        {
            Dispose(false);
        }

        /// <summary>
        /// Disposes the control
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Provides means to dispose the object.
        /// When overriden, it is imperative that base.Dispose(true) is called within the implementation.
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing && _source != null)
            {
                _source.Dispose();
            }
        }

        #endregion

    }
}
