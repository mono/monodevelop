//Copyright (c) Microsoft Corporation.  All rights reserved.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Microsoft.WindowsAPICodePack.Shell;
using Microsoft.WindowsAPICodePack.Shell.PropertySystem;
using MS.WindowsAPICodePack.Internal;

namespace Microsoft.WindowsAPICodePack.Dialogs
{
    // Disable warning if a method declaration hides another inherited from a parent COM interface
    // To successfully import a COM interface, all inherited methods need to be declared again with 
    // the exception of those already declared in "IUnknown"
#pragma warning disable 0108

    [ComImport(),
    Guid(ShellIIDGuid.IFileDialog),
    InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IFileDialog : IModalWindow
    {
        // Defined on IModalWindow - repeated here due to requirements of COM interop layer.
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime),
        PreserveSig]
        int Show([In] IntPtr parent);

        // IFileDialog-Specific interface members.

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void SetFileTypes(
            [In] uint cFileTypes,
            [In, MarshalAs(UnmanagedType.LPArray)] ShellNativeMethods.FilterSpec[] rgFilterSpec);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void SetFileTypeIndex([In] uint iFileType);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetFileTypeIndex(out uint piFileType);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void Advise(
            [In, MarshalAs(UnmanagedType.Interface)] IFileDialogEvents pfde,
            out uint pdwCookie);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void Unadvise([In] uint dwCookie);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void SetOptions([In] ShellNativeMethods.FileOpenOptions fos);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetOptions(out ShellNativeMethods.FileOpenOptions pfos);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void SetDefaultFolder([In, MarshalAs(UnmanagedType.Interface)] IShellItem psi);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void SetFolder([In, MarshalAs(UnmanagedType.Interface)] IShellItem psi);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetFolder([MarshalAs(UnmanagedType.Interface)] out IShellItem ppsi);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetCurrentSelection([MarshalAs(UnmanagedType.Interface)] out IShellItem ppsi);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void SetFileName([In, MarshalAs(UnmanagedType.LPWStr)] string pszName);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetFileName([MarshalAs(UnmanagedType.LPWStr)] out string pszName);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void SetTitle([In, MarshalAs(UnmanagedType.LPWStr)] string pszTitle);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void SetOkButtonLabel([In, MarshalAs(UnmanagedType.LPWStr)] string pszText);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void SetFileNameLabel([In, MarshalAs(UnmanagedType.LPWStr)] string pszLabel);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetResult([MarshalAs(UnmanagedType.Interface)] out IShellItem ppsi);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void AddPlace([In, MarshalAs(UnmanagedType.Interface)] IShellItem psi, ShellNativeMethods.FileDialogAddPlacement fdap);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void SetDefaultExtension([In, MarshalAs(UnmanagedType.LPWStr)] string pszDefaultExtension);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void Close([MarshalAs(UnmanagedType.Error)] int hr);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void SetClientGuid([In] ref Guid guid);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void ClearClientData();

        // Not supported:  IShellItemFilter is not defined, converting to IntPtr.
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void SetFilter([MarshalAs(UnmanagedType.Interface)] IntPtr pFilter);
    }

    [ComImport(),
    Guid(ShellIIDGuid.IFileOpenDialog),
    InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IFileOpenDialog : IFileDialog
    {
        // Defined on IModalWindow - repeated here due to requirements of COM interop layer.
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime),
        PreserveSig]
        int Show([In] IntPtr parent);

        // Defined on IFileDialog - repeated here due to requirements of COM interop layer.
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void SetFileTypes([In] uint cFileTypes, [In] ref ShellNativeMethods.FilterSpec rgFilterSpec);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void SetFileTypeIndex([In] uint iFileType);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetFileTypeIndex(out uint piFileType);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void Advise(
            [In, MarshalAs(UnmanagedType.Interface)] IFileDialogEvents pfde,
            out uint pdwCookie);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void Unadvise([In] uint dwCookie);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void SetOptions([In] ShellNativeMethods.FileOpenOptions fos);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetOptions(out ShellNativeMethods.FileOpenOptions pfos);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void SetDefaultFolder([In, MarshalAs(UnmanagedType.Interface)] IShellItem psi);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void SetFolder([In, MarshalAs(UnmanagedType.Interface)] IShellItem psi);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetFolder([MarshalAs(UnmanagedType.Interface)] out IShellItem ppsi);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetCurrentSelection([MarshalAs(UnmanagedType.Interface)] out IShellItem ppsi);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void SetFileName([In, MarshalAs(UnmanagedType.LPWStr)] string pszName);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetFileName([MarshalAs(UnmanagedType.LPWStr)] out string pszName);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void SetTitle([In, MarshalAs(UnmanagedType.LPWStr)] string pszTitle);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void SetOkButtonLabel([In, MarshalAs(UnmanagedType.LPWStr)] string pszText);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void SetFileNameLabel([In, MarshalAs(UnmanagedType.LPWStr)] string pszLabel);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetResult([MarshalAs(UnmanagedType.Interface)] out IShellItem ppsi);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void AddPlace([In, MarshalAs(UnmanagedType.Interface)] IShellItem psi, ShellNativeMethods.FileDialogAddPlacement fdap);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void SetDefaultExtension([In, MarshalAs(UnmanagedType.LPWStr)] string pszDefaultExtension);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void Close([MarshalAs(UnmanagedType.Error)] int hr);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void SetClientGuid([In] ref Guid guid);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void ClearClientData();

        // Not supported:  IShellItemFilter is not defined, converting to IntPtr.
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void SetFilter([MarshalAs(UnmanagedType.Interface)] IntPtr pFilter);

        // Defined by IFileOpenDialog.
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetResults([MarshalAs(UnmanagedType.Interface)] out IShellItemArray ppenum);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime),
        PreserveSig]
        int GetSelectedItems([MarshalAs(UnmanagedType.Interface)] out IShellItemArray ppsai);
    }

    [ComImport(),
    Guid(ShellIIDGuid.IFileSaveDialog),
    InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IFileSaveDialog : IFileDialog
    {
        // Defined on IModalWindow - repeated here due to requirements of COM interop layer.
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime),
        PreserveSig]
        int Show([In] IntPtr parent);

        // Defined on IFileDialog - repeated here due to requirements of COM interop layer.
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void SetFileTypes(
            [In] uint cFileTypes,
            [In] ref ShellNativeMethods.FilterSpec rgFilterSpec);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void SetFileTypeIndex([In] uint iFileType);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetFileTypeIndex(out uint piFileType);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void Advise(
            [In, MarshalAs(UnmanagedType.Interface)] IFileDialogEvents pfde,
            out uint pdwCookie);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void Unadvise([In] uint dwCookie);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void SetOptions([In] ShellNativeMethods.FileOpenOptions fos);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetOptions(out ShellNativeMethods.FileOpenOptions pfos);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void SetDefaultFolder([In, MarshalAs(UnmanagedType.Interface)] IShellItem psi);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void SetFolder([In, MarshalAs(UnmanagedType.Interface)] IShellItem psi);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetFolder([MarshalAs(UnmanagedType.Interface)] out IShellItem ppsi);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetCurrentSelection([MarshalAs(UnmanagedType.Interface)] out IShellItem ppsi);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void SetFileName([In, MarshalAs(UnmanagedType.LPWStr)] string pszName);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetFileName([MarshalAs(UnmanagedType.LPWStr)] out string pszName);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void SetTitle([In, MarshalAs(UnmanagedType.LPWStr)] string pszTitle);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void SetOkButtonLabel([In, MarshalAs(UnmanagedType.LPWStr)] string pszText);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void SetFileNameLabel([In, MarshalAs(UnmanagedType.LPWStr)] string pszLabel);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetResult([MarshalAs(UnmanagedType.Interface)] out IShellItem ppsi);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void AddPlace(
            [In, MarshalAs(UnmanagedType.Interface)] IShellItem psi,
            ShellNativeMethods.FileDialogAddPlacement fdap);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void SetDefaultExtension([In, MarshalAs(UnmanagedType.LPWStr)] string pszDefaultExtension);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void Close([MarshalAs(UnmanagedType.Error)] int hr);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void SetClientGuid([In] ref Guid guid);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void ClearClientData();

        // Not supported:  IShellItemFilter is not defined, converting to IntPtr.
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void SetFilter([MarshalAs(UnmanagedType.Interface)] IntPtr pFilter);

        // Defined by IFileSaveDialog interface.

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void SetSaveAsItem([In, MarshalAs(UnmanagedType.Interface)] IShellItem psi);

        // Not currently supported: IPropertyStore.
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void SetProperties([In, MarshalAs(UnmanagedType.Interface)] IntPtr pStore);

        [PreserveSig]
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        int SetCollectedProperties(
            [In] IPropertyDescriptionList pList,
            [In] bool fAppendDefault);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        [PreserveSig]
        HResult GetProperties(out IPropertyStore ppStore);

        // Not currently supported: IPropertyStore, IFileOperationProgressSink.
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void ApplyProperties(
            [In, MarshalAs(UnmanagedType.Interface)] IShellItem psi,
            [In, MarshalAs(UnmanagedType.Interface)] IntPtr pStore,
            [In, ComAliasName("ShellObjects.wireHWND")] ref IntPtr hwnd,
            [In, MarshalAs(UnmanagedType.Interface)] IntPtr pSink);
    }

    [ComImport,
    Guid(ShellIIDGuid.IFileDialogEvents),
    InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IFileDialogEvents
    {
        // NOTE: some of these callbacks are cancelable - returning S_FALSE means that 
        // the dialog should not proceed (e.g. with closing, changing folder); to 
        // support this, we need to use the PreserveSig attribute to enable us to return
        // the proper HRESULT.

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime),
        PreserveSig]
        HResult OnFileOk([In, MarshalAs(UnmanagedType.Interface)] IFileDialog pfd);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime),
        PreserveSig]
        HResult OnFolderChanging(
            [In, MarshalAs(UnmanagedType.Interface)] IFileDialog pfd,
            [In, MarshalAs(UnmanagedType.Interface)] IShellItem psiFolder);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void OnFolderChange([In, MarshalAs(UnmanagedType.Interface)] IFileDialog pfd);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void OnSelectionChange([In, MarshalAs(UnmanagedType.Interface)] IFileDialog pfd);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void OnShareViolation(
            [In, MarshalAs(UnmanagedType.Interface)] IFileDialog pfd,
            [In, MarshalAs(UnmanagedType.Interface)] IShellItem psi,
            out ShellNativeMethods.FileDialogEventShareViolationResponse pResponse);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void OnTypeChange([In, MarshalAs(UnmanagedType.Interface)] IFileDialog pfd);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void OnOverwrite([In, MarshalAs(UnmanagedType.Interface)] IFileDialog pfd,
            [In, MarshalAs(UnmanagedType.Interface)] IShellItem psi,
            out ShellNativeMethods.FileDialogEventOverwriteResponse pResponse);
    }

    [ComImport,
    Guid(ShellIIDGuid.IFileDialogCustomize),
    InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IFileDialogCustomize
    {
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void EnableOpenDropDown([In] int dwIDCtl);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void AddMenu(
            [In] int dwIDCtl,
            [In, MarshalAs(UnmanagedType.LPWStr)] string pszLabel);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void AddPushButton(
            [In] int dwIDCtl,
            [In, MarshalAs(UnmanagedType.LPWStr)] string pszLabel);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void AddComboBox([In] int dwIDCtl);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void AddRadioButtonList([In] int dwIDCtl);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void AddCheckButton(
            [In] int dwIDCtl,
            [In, MarshalAs(UnmanagedType.LPWStr)] string pszLabel,
            [In] bool bChecked);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void AddEditBox(
            [In] int dwIDCtl,
            [In, MarshalAs(UnmanagedType.LPWStr)] string pszText);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void AddSeparator([In] int dwIDCtl);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void AddText(
            [In] int dwIDCtl,
            [In, MarshalAs(UnmanagedType.LPWStr)] string pszText);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void SetControlLabel(
            [In] int dwIDCtl,
            [In, MarshalAs(UnmanagedType.LPWStr)] string pszLabel);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetControlState(
            [In] int dwIDCtl,
            [Out] out ShellNativeMethods.ControlState pdwState);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void SetControlState(
            [In] int dwIDCtl,
            [In] ShellNativeMethods.ControlState dwState);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetEditBoxText(
            [In] int dwIDCtl,
            [MarshalAs(UnmanagedType.LPWStr)] out string ppszText);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void SetEditBoxText(
            [In] int dwIDCtl,
            [In, MarshalAs(UnmanagedType.LPWStr)] string pszText);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetCheckButtonState(
            [In] int dwIDCtl,
            [Out] out bool pbChecked);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void SetCheckButtonState(
            [In] int dwIDCtl,
            [In] bool bChecked);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void AddControlItem(
            [In] int dwIDCtl,
            [In] int dwIDItem,
            [In, MarshalAs(UnmanagedType.LPWStr)] string pszLabel);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void RemoveControlItem(
            [In] int dwIDCtl,
            [In] int dwIDItem);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void RemoveAllControlItems([In] int dwIDCtl);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetControlItemState(
            [In] int dwIDCtl,
            [In] int dwIDItem,
            [Out] out ShellNativeMethods.ControlState pdwState);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void SetControlItemState(
            [In] int dwIDCtl,
            [In] int dwIDItem,
            [In] ShellNativeMethods.ControlState dwState);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetSelectedControlItem(
            [In] int dwIDCtl,
            [Out] out int pdwIDItem);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void SetSelectedControlItem(
            [In] int dwIDCtl,
            [In] int dwIDItem); // Not valid for OpenDropDown.

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void StartVisualGroup(
            [In] int dwIDCtl,
            [In, MarshalAs(UnmanagedType.LPWStr)] string pszLabel);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void EndVisualGroup();

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void MakeProminent([In] int dwIDCtl);
    }

    [ComImport,
    Guid(ShellIIDGuid.IFileDialogControlEvents),
    InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IFileDialogControlEvents
    {
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void OnItemSelected(
            [In, MarshalAs(UnmanagedType.Interface)] IFileDialogCustomize pfdc,
            [In] int dwIDCtl,
            [In] int dwIDItem);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void OnButtonClicked(
            [In, MarshalAs(UnmanagedType.Interface)] IFileDialogCustomize pfdc,
            [In] int dwIDCtl);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void OnCheckButtonToggled(
            [In, MarshalAs(UnmanagedType.Interface)] IFileDialogCustomize pfdc,
            [In] int dwIDCtl,
            [In] bool bChecked);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void OnControlActivating(
            [In, MarshalAs(UnmanagedType.Interface)] IFileDialogCustomize pfdc,
            [In] int dwIDCtl);
    }
    // Restore the warning
#pragma warning restore 0108

}
