//Copyright (c) Microsoft Corporation.  All rights reserved.

using System;
using System.ComponentModel;
using System.Text;
using System.Windows.Forms;
using Microsoft.WindowsAPICodePack.Shell;
using MS.WindowsAPICodePack.Internal;

namespace Microsoft.WindowsAPICodePack.Controls.WindowsForms
{
    /// <summary>
    /// Implements a CommandLink button that can be used in 
    /// WinForms user interfaces.
    /// </summary>    
    public class CommandLink : Button
    {
        /// <summary>
        /// Gets a System.Windows.Forms.CreateParams on the base class when 
        /// creating a window.
        /// </summary>        
        protected override CreateParams CreateParams
        {

            get
            {
                // Add BS_COMMANDLINK style before control creation.
                CreateParams cp = base.CreateParams;

                cp.Style = AddCommandLinkStyle(cp.Style);

                return cp;
            }
        }

        // Let Windows handle the rendering.
        /// <summary>
        /// Creates a new instance of this class.
        /// </summary>
        public CommandLink()
        {
            CoreHelpers.ThrowIfNotVista();

            FlatStyle = FlatStyle.System;
        }

        // Add Design-Time Support.

        /// <summary>
        /// Increase default width.
        /// </summary>
        protected override System.Drawing.Size DefaultSize
        {
            get { return new System.Drawing.Size(180, 60); }
        }

        /// <summary>
        /// Specifies the supporting note text
        /// </summary>
        [Category("Appearance")]
        [Description("Specifies the supporting note text.")]
        [BrowsableAttribute(true)]
        [DefaultValue("(Note Text)")]
        public string NoteText
        {
            get { return (GetNote(this)); }
            set
            {
                SetNote(this, value);
            }
        }

        /// <summary>
        /// Enable shield icon to be set at design-time.
        /// </summary>
        [Category("Appearance")]
        [Description("Indicates whether the button should be decorated with the security shield icon (Windows Vista only).")]
        [BrowsableAttribute(true)]
        [DefaultValue(false)]
        public bool UseElevationIcon
        {
            get { return (useElevationIcon); }
            set
            {
                useElevationIcon = value;
                SetShieldIcon(this, this.useElevationIcon);
            }
        }
        private bool useElevationIcon;


        #region Interop helpers

        private static int AddCommandLinkStyle(int style)
        {
            // Only add BS_COMMANDLINK style on Windows Vista or above.
            // Otherwise, button creation will fail.
            if (CoreHelpers.RunningOnVista)
            {
                style |= ShellNativeMethods.CommandLink;
            }

            return style;
        }

        private static string GetNote(System.Windows.Forms.Button Button)
        {
            IntPtr retVal = CoreNativeMethods.SendMessage(
                Button.Handle,
                ShellNativeMethods.GetNoteLength,
                IntPtr.Zero,
                IntPtr.Zero);

            // Add 1 for null terminator, to get the entire string back.
            int len = ((int)retVal) + 1;
            StringBuilder strBld = new StringBuilder(len);

            retVal = CoreNativeMethods.SendMessage(Button.Handle, ShellNativeMethods.GetNote, ref len, strBld);
            return strBld.ToString();
        }

        private static void SetNote(System.Windows.Forms.Button button, string text)
        {
            // This call will be ignored on versions earlier than Windows Vista.
            CoreNativeMethods.SendMessage(button.Handle, ShellNativeMethods.SetNote, 0, text);
        }

        static internal void SetShieldIcon(System.Windows.Forms.Button Button, bool Show)
        {
            IntPtr fRequired = new IntPtr(Show ? 1 : 0);
            CoreNativeMethods.SendMessage(
               Button.Handle,
                ShellNativeMethods.SetShield,
                IntPtr.Zero,
                fRequired);
        }

        #endregion

        /// <summary>
        /// Indicates whether this feature is supported on the current platform.
        /// </summary>
        public static bool IsPlatformSupported
        {
            get
            {
                // We need Windows Vista onwards ...
                return CoreHelpers.RunningOnVista;
            }
        }
    }
}
