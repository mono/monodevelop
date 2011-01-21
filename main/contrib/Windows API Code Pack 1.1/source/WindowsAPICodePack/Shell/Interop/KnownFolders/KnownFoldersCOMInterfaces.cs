//Copyright (c) Microsoft Corporation.  All rights reserved.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using MS.WindowsAPICodePack.Internal;

namespace Microsoft.WindowsAPICodePack.Shell
{
    // Disable warning if a method declaration hides another inherited from a parent COM interface
    // To successfully import a COM interface, all inherited methods need to be declared again with 
    // the exception of those already declared in "IUnknown"
#pragma warning disable 0108

    [ComImport,
    Guid(KnownFoldersIIDGuid.IKnownFolder),
    InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IKnownFolderNative
    {
        [MethodImpl(MethodImplOptions.InternalCall,
            MethodCodeType = MethodCodeType.Runtime)]
        Guid GetId();

        [MethodImpl(MethodImplOptions.InternalCall,
            MethodCodeType = MethodCodeType.Runtime)]
        FolderCategory GetCategory();

        [MethodImpl(MethodImplOptions.InternalCall,
            MethodCodeType = MethodCodeType.Runtime)]
        [PreserveSig]
        HResult GetShellItem([In] int i,
             ref Guid interfaceGuid,
             [Out, MarshalAs(UnmanagedType.Interface)] out IShellItem2 shellItem);

        [return: MarshalAs(UnmanagedType.LPWStr)]
        [MethodImpl(MethodImplOptions.InternalCall,
            MethodCodeType = MethodCodeType.Runtime)]
        string GetPath([In] int option);

        [MethodImpl(MethodImplOptions.InternalCall,
            MethodCodeType = MethodCodeType.Runtime)]
        void SetPath([In] int i, [In] string path);

        [MethodImpl(MethodImplOptions.InternalCall,
            MethodCodeType = MethodCodeType.Runtime)]
        void GetIDList([In] int i,
            [Out] out IntPtr itemIdentifierListPointer);

        [MethodImpl(MethodImplOptions.InternalCall,
            MethodCodeType = MethodCodeType.Runtime)]
        Guid GetFolderType();

        [MethodImpl(MethodImplOptions.InternalCall,
            MethodCodeType = MethodCodeType.Runtime)]
        RedirectionCapability GetRedirectionCapabilities();

        [MethodImpl(MethodImplOptions.InternalCall,
            MethodCodeType = MethodCodeType.Runtime)]
        void GetFolderDefinition(
            [Out, MarshalAs(UnmanagedType.Struct)] out KnownFoldersSafeNativeMethods.NativeFolderDefinition definition);

    }

    [ComImport,
    Guid(KnownFoldersIIDGuid.IKnownFolderManager),
    InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IKnownFolderManager
    {
        [MethodImpl(MethodImplOptions.InternalCall,
            MethodCodeType = MethodCodeType.Runtime)]
        void FolderIdFromCsidl(int csidl,
           [Out] out Guid knownFolderID);

        [MethodImpl(MethodImplOptions.InternalCall,
            MethodCodeType = MethodCodeType.Runtime)]
        void FolderIdToCsidl([In, MarshalAs(UnmanagedType.LPStruct)] Guid id,
          [Out] out int csidl);

        [MethodImpl(MethodImplOptions.InternalCall,
            MethodCodeType = MethodCodeType.Runtime)]
        void GetFolderIds([Out] out IntPtr folders,
          [Out] out UInt32 count);

        [PreserveSig]
        [MethodImpl(MethodImplOptions.InternalCall,
            MethodCodeType = MethodCodeType.Runtime)]
        HResult GetFolder([In, MarshalAs(UnmanagedType.LPStruct)] Guid id,
          [Out, MarshalAs(UnmanagedType.Interface)] out IKnownFolderNative knownFolder);

        [MethodImpl(MethodImplOptions.InternalCall,
            MethodCodeType = MethodCodeType.Runtime)]
        void GetFolderByName(string canonicalName,
          [Out, MarshalAs(UnmanagedType.Interface)] out IKnownFolderNative knownFolder);

        [MethodImpl(MethodImplOptions.InternalCall,
            MethodCodeType = MethodCodeType.Runtime)]
        void RegisterFolder(
            [In, MarshalAs(UnmanagedType.LPStruct)] Guid knownFolderGuid,
            [In] ref KnownFoldersSafeNativeMethods.NativeFolderDefinition knownFolderDefinition);

        [MethodImpl(MethodImplOptions.InternalCall,
            MethodCodeType = MethodCodeType.Runtime)]
        void UnregisterFolder(
            [In, MarshalAs(UnmanagedType.LPStruct)] Guid knownFolderGuid);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void FindFolderFromPath(
            [In, MarshalAs(UnmanagedType.LPWStr)] string path,
            [In] int mode,
            [Out, MarshalAs(UnmanagedType.Interface)] out IKnownFolderNative knownFolder);

        [PreserveSig]
        [MethodImpl(MethodImplOptions.InternalCall,
            MethodCodeType = MethodCodeType.Runtime)]
        HResult FindFolderFromIDList(IntPtr pidl, [Out, MarshalAs(UnmanagedType.Interface)] out IKnownFolderNative knownFolder);

        [MethodImpl(MethodImplOptions.InternalCall,
            MethodCodeType = MethodCodeType.Runtime)]
        void Redirect();
    }
    
    [ComImport]
    [Guid("4df0c730-df9d-4ae3-9153-aa6b82e9795a")]
    internal class KnownFolderManagerClass : IKnownFolderManager
    {
        
        [MethodImpl(MethodImplOptions.InternalCall,
            MethodCodeType = MethodCodeType.Runtime)]
        public virtual extern void FolderIdFromCsidl(int csidl,
            [Out] out Guid knownFolderID);

        [MethodImpl(MethodImplOptions.InternalCall,
            MethodCodeType = MethodCodeType.Runtime)]
        public virtual extern void FolderIdToCsidl(
            [In, MarshalAs(UnmanagedType.LPStruct)] Guid id,
            [Out] out int csidl);

        [MethodImpl(MethodImplOptions.InternalCall,
            MethodCodeType = MethodCodeType.Runtime)]
        public virtual extern void GetFolderIds(
            [Out] out IntPtr folders,
            [Out] out UInt32 count);

        [PreserveSig]
        [MethodImpl(MethodImplOptions.InternalCall,
            MethodCodeType = MethodCodeType.Runtime)]
        public virtual extern HResult GetFolder(
            [In, MarshalAs(UnmanagedType.LPStruct)] Guid id,
            [Out, MarshalAs(UnmanagedType.Interface)] 
              out IKnownFolderNative knownFolder);

        [MethodImpl(MethodImplOptions.InternalCall,
            MethodCodeType = MethodCodeType.Runtime)]
        public virtual extern void GetFolderByName(
            string canonicalName,
            [Out, MarshalAs(UnmanagedType.Interface)] out IKnownFolderNative knownFolder);

        [MethodImpl(MethodImplOptions.InternalCall,
            MethodCodeType = MethodCodeType.Runtime)]
        public virtual extern void RegisterFolder(
            [In, MarshalAs(UnmanagedType.LPStruct)] Guid knownFolderGuid,
            [In] ref KnownFoldersSafeNativeMethods.NativeFolderDefinition knownFolderDefinition);

        [MethodImpl(MethodImplOptions.InternalCall,
            MethodCodeType = MethodCodeType.Runtime)]
        public virtual extern void UnregisterFolder(
            [In, MarshalAs(UnmanagedType.LPStruct)] Guid knownFolderGuid);

        [MethodImpl(MethodImplOptions.InternalCall,
            MethodCodeType = MethodCodeType.Runtime)]
        public virtual extern void FindFolderFromPath(
            [In, MarshalAs(UnmanagedType.LPWStr)] string path,
            [In] int mode,
            [Out, MarshalAs(UnmanagedType.Interface)] out IKnownFolderNative knownFolder);

        [PreserveSig]
        [MethodImpl(MethodImplOptions.InternalCall,
            MethodCodeType = MethodCodeType.Runtime)]
        public virtual extern HResult FindFolderFromIDList(IntPtr pidl, [Out, MarshalAs(UnmanagedType.Interface)] out IKnownFolderNative knownFolder);

        [MethodImpl(MethodImplOptions.InternalCall,
            MethodCodeType = MethodCodeType.Runtime)]
        public virtual extern void Redirect();
    }
}
