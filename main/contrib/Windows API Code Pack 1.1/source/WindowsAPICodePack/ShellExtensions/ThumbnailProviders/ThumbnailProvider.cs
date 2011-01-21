using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Shell;
using Microsoft.WindowsAPICodePack.ShellExtensions.Interop;
using Microsoft.WindowsAPICodePack.ShellExtensions.Resources;
using Microsoft.WindowsAPICodePack.Taskbar;

namespace Microsoft.WindowsAPICodePack.ShellExtensions
{
    /// <summary>
    /// This is the base class for all thumbnail providers and provides their basic functionality.
    /// To create a custom thumbnail provider a class must derive from this, use the <typeparamref name="ThumbnailProviderAttribute"/>,
    /// and implement 1 or more of the following interfaces: 
    /// <typeparamref name="IThumbnailFromStream"/>, <typeparamref name="IThumbnailFromShellObject"/>, <typeparamref name="IThumbnailFromFile"/>.   
    /// </summary>
    public abstract class ThumbnailProvider : IThumbnailProvider, ICustomQueryInterface, IDisposable,
        IInitializeWithStream, IInitializeWithItem, IInitializeWithFile
    {
        // Determines which interface should be called to return a bitmap
        private Bitmap GetBitmap(int sideLength)
        {
            IThumbnailFromStream stream;
            IThumbnailFromShellObject shellObject;
            IThumbnailFromFile file;

            if (_stream != null && (stream = this as IThumbnailFromStream) != null)
            {
                return stream.ConstructBitmap(_stream, sideLength);
            }
            if (_shellObject != null && (shellObject = this as IThumbnailFromShellObject) != null)
            {
                return shellObject.ConstructBitmap(_shellObject, sideLength);
            }
            if (_info != null && (file = this as IThumbnailFromFile) != null)
            {
                return file.ConstructBitmap(_info, sideLength);
            }

            throw new InvalidOperationException(
                string.Format(System.Globalization.CultureInfo.InvariantCulture,
                LocalizedMessages.ThumbnailProviderInterfaceNotImplemented,
                this.GetType().Name));
        }

        /// <summary>
        /// Sets the AlphaType of the generated thumbnail.
        /// Override this method in a derived class to change the thumbnails AlphaType, default is Unknown.
        /// </summary>
        /// <returns>ThumnbailAlphaType</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate")]
        public virtual ThumbnailAlphaType GetThumbnailAlphaType()
        {
            return ThumbnailAlphaType.Unknown;
        }        

        private StorageStream _stream = null;
        private FileInfo _info = null;
        private ShellObject _shellObject = null;

        #region IThumbnailProvider Members

        void IThumbnailProvider.GetThumbnail(uint sideLength, out IntPtr hBitmap, out uint alphaType)
        {
            using (Bitmap map = GetBitmap((int)sideLength))
            {
                hBitmap = map.GetHbitmap();
            }
            alphaType = (uint)GetThumbnailAlphaType();
        }

        #endregion

        #region ICustomQueryInterface Members

        CustomQueryInterfaceResult ICustomQueryInterface.GetInterface(ref Guid iid, out IntPtr ppv)
        {
            ppv = IntPtr.Zero;

            // Forces COM to not use the managed (free threaded) marshaler
            if (iid == HandlerNativeMethods.IMarshalGuid)
            {
                return CustomQueryInterfaceResult.Failed;
            }

            if ((iid == HandlerNativeMethods.IInitializeWithStreamGuid && !(this is IThumbnailFromStream))
                || (iid == HandlerNativeMethods.IInitializeWithItemGuid && !(this is IThumbnailFromShellObject))
                || (iid == HandlerNativeMethods.IInitializeWithFileGuid && !(this is IThumbnailFromFile)))
            {
                return CustomQueryInterfaceResult.Failed;
            }

            return CustomQueryInterfaceResult.NotHandled;
        }

        #endregion

        #region COM Registration

        /// <summary>
        /// Called when the assembly is registered via RegAsm.
        /// </summary>
        /// <param name="registerType">Type to be registered.</param>
        [ComRegisterFunction]
        private static void Register(Type registerType)
        {
            if (registerType != null && registerType.IsSubclassOf(typeof(ThumbnailProvider)))
            {
                object[] attributes = registerType.GetCustomAttributes(typeof(ThumbnailProviderAttribute), true);
                if (attributes != null && attributes.Length == 1)
                {
                    ThumbnailProviderAttribute attribute = attributes[0] as ThumbnailProviderAttribute;
                    ThrowIfInvalid(registerType, attribute);
                    RegisterThumbnailHandler(registerType.GUID.ToString("B"), attribute);
                }
            }
        }

        private static void RegisterThumbnailHandler(string guid, ThumbnailProviderAttribute attribute)
        {
            // set process isolation
            using (RegistryKey clsidKey = Registry.ClassesRoot.OpenSubKey("CLSID"))
            using (RegistryKey guidKey = clsidKey.OpenSubKey(guid, true))
            {
                guidKey.SetValue("DisableProcessIsolation", attribute.DisableProcessIsolation ? 1 : 0, RegistryValueKind.DWord);

                using (RegistryKey inproc = guidKey.OpenSubKey("InprocServer32", true))
                {
                    inproc.SetValue("ThreadingModel", "Apartment", RegistryValueKind.String);
                }
            }

            // register file as an approved extension
            using (RegistryKey approvedShellExtensions = Registry.LocalMachine.OpenSubKey(
                 @"SOFTWARE\Microsoft\Windows\CurrentVersion\Shell Extensions\Approved", true))
            {
                approvedShellExtensions.SetValue(guid, attribute.Name, RegistryValueKind.String);
            }

            // register extension with each extension in the list
            string[] extensions = attribute.Extensions.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string extension in extensions)
            {
                using (RegistryKey extensionKey = Registry.ClassesRoot.CreateSubKey(extension)) // Create makes it writable
                using (RegistryKey shellExKey = extensionKey.CreateSubKey("shellex"))
                using (RegistryKey providerKey = shellExKey.CreateSubKey(HandlerNativeMethods.IThumbnailProviderGuid.ToString("B")))
                {
                    providerKey.SetValue(null, guid, RegistryValueKind.String);

                    if (attribute.ThumbnailCutoff == ThumbnailCutoffSize.Square20)
                    {
                        extensionKey.DeleteValue("ThumbnailCutoff", false);
                    }
                    else
                    {
                        extensionKey.SetValue("ThumbnailCutoff", (int)attribute.ThumbnailCutoff, RegistryValueKind.DWord);
                    }


                    if (attribute.TypeOverlay != null)
                    {
                        extensionKey.SetValue("TypeOverlay", attribute.TypeOverlay, RegistryValueKind.String);
                    }

                    if (attribute.ThumbnailAdornment == ThumbnailAdornment.Default)
                    {
                        extensionKey.DeleteValue("Treatment", false);
                    }
                    else
                    {
                        extensionKey.SetValue("Treatment", (int)attribute.ThumbnailAdornment, RegistryValueKind.DWord);
                    }
                }
            }
        }


        /// <summary>
        /// Called when the assembly is registered via RegAsm.
        /// </summary>
        /// <param name="registerType">Type to register.</param>
        [ComUnregisterFunction]
        private static void Unregister(Type registerType)
        {
            if (registerType != null && registerType.IsSubclassOf(typeof(ThumbnailProvider)))
            {
                object[] attributes = registerType.GetCustomAttributes(typeof(ThumbnailProviderAttribute), true);
                if (attributes != null && attributes.Length == 1)
                {
                    ThumbnailProviderAttribute attribute = attributes[0] as ThumbnailProviderAttribute;
                    UnregisterThumbnailHandler(registerType.GUID.ToString("B"), attribute);
                }
            }
        }

        private static void UnregisterThumbnailHandler(string guid, ThumbnailProviderAttribute attribute)
        {
            string[] extensions = attribute.Extensions.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string extension in extensions)
            {
                using (RegistryKey extKey = Registry.ClassesRoot.OpenSubKey(extension, true))
                using (RegistryKey shellexKey = extKey.OpenSubKey("shellex", true))
                {
                    shellexKey.DeleteSubKey(HandlerNativeMethods.IThumbnailProviderGuid.ToString("B"), false);

                    extKey.DeleteValue("ThumbnailCutoff", false);
                    extKey.DeleteValue("TypeOverlay", false);
                    extKey.DeleteValue("Treatment", false); // Thumbnail adornment
                }
            }

            using (RegistryKey approvedShellExtensions = Registry.LocalMachine.OpenSubKey(
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\Shell Extensions\Approved", true))
            {
                approvedShellExtensions.DeleteValue(guid, false);
            }
        }

        private static void ThrowIfInvalid(Type type, ThumbnailProviderAttribute attribute)
        {
            var interfaces = type.GetInterfaces();
            bool interfaced = interfaces.Any(x => x == typeof(IThumbnailFromStream));

            if (interfaces.Any(x => x == typeof(IThumbnailFromShellObject) || x == typeof(IThumbnailFromFile)))
            {
                // According to MSDN (http://msdn.microsoft.com/en-us/library/cc144114(v=VS.85).aspx)
                // A thumbnail provider that does not implement IInitializeWithStream must opt out of 
                // running in the isolated process. The default behavior of the indexer opts in
                // to process isolation regardless of which interfaces are implemented.                
                if (!interfaced && !attribute.DisableProcessIsolation)
                {
                    throw new InvalidOperationException(
                        string.Format(System.Globalization.CultureInfo.InvariantCulture,
                        LocalizedMessages.ThumbnailProviderDisabledProcessIsolation,
                        type.Name));
                }
                interfaced = true;
            }

            if (!interfaced)
            {
                throw new InvalidOperationException(
                        string.Format(System.Globalization.CultureInfo.InvariantCulture,
                        LocalizedMessages.ThumbnailProviderInterfaceNotImplemented,
                        type.Name));
            }
        }

        #endregion

        #region IInitializeWithStream Members

        void IInitializeWithStream.Initialize(System.Runtime.InteropServices.ComTypes.IStream stream, Shell.AccessModes fileMode)
        {
            _stream = new StorageStream(stream, fileMode != Shell.AccessModes.ReadWrite);
        }

        #endregion

        #region IInitializeWithItem Members

        void IInitializeWithItem.Initialize(Shell.IShellItem shellItem, Shell.AccessModes accessMode)
        {
            _shellObject = ShellObjectFactory.Create(shellItem);
        }

        #endregion

        #region IInitializeWithFile Members

        void IInitializeWithFile.Initialize(string filePath, Shell.AccessModes fileMode)
        {
            _info = new FileInfo(filePath);
        }

        #endregion

        #region IDisposable Members

        /// <summary>
        /// Finalizer for the thumbnail provider.
        /// </summary>
        ~ThumbnailProvider()
        {
            Dispose(false);
        }

        /// <summary>
        /// Disposes the thumbnail provider.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disploses the thumbnail provider.
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing && _stream != null)
            {
                _stream.Dispose();
            }
        }

        #endregion

    }
}
