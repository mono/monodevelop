//Copyright (c) Microsoft Corporation.  All rights reserved.

using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using Microsoft.WindowsAPICodePack.Shell.Resources;
using MS.WindowsAPICodePack.Internal;

namespace Microsoft.WindowsAPICodePack.Shell
{
    /// <summary>
    /// Represents a standard system icon.
    /// </summary>
    public class StockIcon : IDisposable
    {
        #region Private Members

        private StockIconIdentifier identifier = StockIconIdentifier.Application;
        private StockIconSize currentSize = StockIconSize.Large;
        private bool linkOverlay;
        private bool selected;
        private bool invalidateIcon = true;
        private IntPtr hIcon = IntPtr.Zero;

        #endregion

        #region Public Constructors

        /// <summary>
        /// Creates a new StockIcon instance with the specified identifer, default size 
        /// and no link overlay or selected states.
        /// </summary>
        /// <param name="id">A value that identifies the icon represented by this instance.</param>
        public StockIcon(StockIconIdentifier id)
        {
            identifier = id;
            invalidateIcon = true;
        }

        /// <summary>
        /// Creates a new StockIcon instance with the specified identifer and options.
        /// </summary>
        /// <param name="id">A value that identifies the icon represented by this instance.</param>
        /// <param name="size">A value that indicates the size of the stock icon.</param>
        /// <param name="isLinkOverlay">A bool value that indicates whether the icon has a link overlay.</param>
        /// <param name="isSelected">A bool value that indicates whether the icon is in a selected state.</param>
        public StockIcon(StockIconIdentifier id, StockIconSize size, bool isLinkOverlay, bool isSelected)
        {
            identifier = id;
            linkOverlay = isLinkOverlay;
            selected = isSelected;
            currentSize = size;
            invalidateIcon = true;
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets or sets a value indicating whether the icon appears selected.
        /// </summary>
        /// <value>A <see cref="System.Boolean"/> value.</value>
        public bool Selected
        {
            get { return selected; }
            set
            {
                selected = value;
                invalidateIcon = true;
            }
        }

        /// <summary>
        /// Gets or sets a value that cotrols whether to put a link overlay on the icon.
        /// </summary>
        /// <value>A <see cref="System.Boolean"/> value.</value>
        public bool LinkOverlay
        {
            get { return linkOverlay; }
            set
            {
                linkOverlay = value;
                invalidateIcon = true;
            }
        }

        /// <summary>
        /// Gets or sets a value that controls the size of the Stock Icon.
        /// </summary>
        /// <value>A <see cref="Microsoft.WindowsAPICodePack.Shell.StockIconSize"/> value.</value>
        public StockIconSize CurrentSize
        {
            get { return currentSize; }
            set
            {
                currentSize = value;
                invalidateIcon = true;
            }
        }

        /// <summary>
        /// Gets or sets the Stock Icon identifier associated with this icon.
        /// </summary>
        public StockIconIdentifier Identifier
        {
            get { return identifier; }
            set
            {
                identifier = value;
                invalidateIcon = true;
            }
        }

        /// <summary>
        /// Gets the icon image in <see cref="System.Drawing.Bitmap"/> format. 
        /// </summary>
        public Bitmap Bitmap
        {
            get
            {
                UpdateHIcon();

                return hIcon != IntPtr.Zero ? Bitmap.FromHicon(hIcon) : null;
            }
        }

        /// <summary>
        /// Gets the icon image in <see cref="System.Windows.Media.Imaging.BitmapSource"/> format. 
        /// </summary>
        public BitmapSource BitmapSource
        {
            get
            {
                UpdateHIcon();

                return (hIcon != IntPtr.Zero) ?
                    Imaging.CreateBitmapSourceFromHIcon(hIcon, Int32Rect.Empty, null) : null;
            }
        }

        /// <summary>
        /// Gets the icon image in <see cref="System.Drawing.Icon"/> format.
        /// </summary>
        public Icon Icon
        {
            get
            {
                UpdateHIcon();

                return hIcon != IntPtr.Zero ? Icon.FromHandle(hIcon) : null;
            }
        }

        #endregion

        #region Private Methods

        private void UpdateHIcon()
        {
            if (invalidateIcon)
            {
                if (hIcon != IntPtr.Zero)
                    CoreNativeMethods.DestroyIcon(hIcon);

                hIcon = GetHIcon();

                invalidateIcon = false;
            }
        }

        private IntPtr GetHIcon()
        {
            // Create our internal flag to pass to the native method
            StockIconsNativeMethods.StockIconOptions flags = StockIconsNativeMethods.StockIconOptions.Handle;

            // Based on the current settings, update the flags
            if (CurrentSize == StockIconSize.Small)
            {
                flags |= StockIconsNativeMethods.StockIconOptions.Small;
            }
            else if (CurrentSize == StockIconSize.ShellSize)
            {
                flags |= StockIconsNativeMethods.StockIconOptions.ShellSize;
            }
            else
            {
                flags |= StockIconsNativeMethods.StockIconOptions.Large;  // default
            }

            if (Selected)
            {
                flags |= StockIconsNativeMethods.StockIconOptions.Selected;
            }

            if (LinkOverlay)
            {
                flags |= StockIconsNativeMethods.StockIconOptions.LinkOverlay;
            }

            // Create a StockIconInfo structure to pass to the native method.
            StockIconsNativeMethods.StockIconInfo info = new StockIconsNativeMethods.StockIconInfo();
            info.StuctureSize = (UInt32)Marshal.SizeOf(typeof(StockIconsNativeMethods.StockIconInfo));

            // Pass the struct to the native method
            HResult hr = StockIconsNativeMethods.SHGetStockIconInfo(identifier, flags, ref info);

            // If we get an error, return null as the icon requested might not be supported
            // on the current system
            if (hr != HResult.Ok)
            {
                if (hr == HResult.InvalidArguments)
                {
                    throw new InvalidOperationException(
                        string.Format(System.Globalization.CultureInfo.InvariantCulture,
                        LocalizedMessages.StockIconInvalidGuid,
                        identifier));
                }

                return IntPtr.Zero;
            }

            // If we succeed, return the HIcon
            return info.Handle;
        }

        #endregion

        #region IDisposable Members

        /// <summary>
        /// Release the native and managed objects
        /// </summary>
        /// <param name="disposing">Indicates that this is being called from Dispose(), rather than the finalizer.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                // dispose managed resources here
            }

            // Unmanaged resources
            if (hIcon != IntPtr.Zero)
                CoreNativeMethods.DestroyIcon(hIcon);
        }

        /// <summary>
        /// Release the native objects
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// 
        /// </summary>
        ~StockIcon()
        {
            Dispose(false);
        }

        #endregion
    }
}

