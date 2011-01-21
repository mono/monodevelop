using System;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.WindowsAPICodePack.ShellExtensions.Interop;
using Microsoft.WindowsAPICodePack.ShellExtensions.Resources;
using Microsoft.WindowsAPICodePack.Shell;

namespace Microsoft.WindowsAPICodePack.ShellExtensions
{
    /// <summary>
    /// This is the base class for all WinForms-based preview handlers and provides their basic functionality.
    /// To create a custom preview handler that contains a WinForms user control,
    /// a class must derive from this, use the <typeparamref name="PreviewHandlerAttribute"/>,
    /// and implement 1 or more of the following interfaces: 
    /// <typeparamref name="IPreviewFromStream"/>, 
    /// <typeparamref name="IPreviewFromShellObject"/>, 
    /// <typeparamref name="IPreviewFromFile"/>.   
    /// </summary>
    public abstract class WinFormsPreviewHandler : PreviewHandler, IDisposable
    {
        /// <summary>
        /// This control must be populated by the deriving class before the preview is shown.
        /// </summary>
        public UserControl Control { get; protected set; }

        protected void ThrowIfNoControl()
        {
            if (Control == null)
            {
                throw new InvalidOperationException(LocalizedMessages.PreviewHandlerControlNotInitialized);
            }
        }

        /// <summary>
        /// Called when an exception is thrown during itialization of the preview control.
        /// </summary>
        /// <param name="caughtException"></param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", 
            Justification="The object remains reachable through the Controls collection which can be disposed at a later time.")]
        protected override void HandleInitializeException(Exception caughtException)
        {
            if (caughtException == null) { throw new ArgumentNullException("caughtException"); }

            Control = new UserControl();
            Control.Controls.Add(new TextBox
                {
                    ReadOnly = true,
                    Multiline = true,
                    Dock = DockStyle.Fill,
                    Text = caughtException.ToString(),
                    BackColor = Color.OrangeRed
                });
        }

        protected override void UpdateBounds(NativeRect bounds)
        {
            Control.Bounds = Rectangle.FromLTRB(bounds.Left, bounds.Top, bounds.Right, bounds.Bottom);
            Control.Visible = true;
        }

        protected override void SetFocus()
        {
            Control.Focus();
        }

        protected override void SetBackground(int argb)
        {
            Control.BackColor = Color.FromArgb(argb);
        }

        protected override void SetForeground(int argb)
        {
            Control.ForeColor = Color.FromArgb(argb);
        }

        protected override void SetFont(Interop.LogFont font)
        {
            Control.Font = Font.FromLogFont(font);
        }

        protected override IntPtr Handle
        {
            get
            {
                {
                    return Control.Handle;
                }
            }
        }

        protected override void SetParentHandle(IntPtr handle)
        {
            HandlerNativeMethods.SetParent(Control.Handle, handle);
        }

        #region IDisposable Members

        ~WinFormsPreviewHandler()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing && Control != null)
            {
                Control.Dispose();
            }
        }

        #endregion
    }
}
