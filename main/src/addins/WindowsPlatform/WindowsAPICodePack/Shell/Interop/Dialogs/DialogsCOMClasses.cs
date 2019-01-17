//Copyright (c) Microsoft Corporation.  All rights reserved.

using System;
using System.Runtime.InteropServices;
using Microsoft.WindowsAPICodePack.Shell;

namespace Microsoft.WindowsAPICodePack.Dialogs
{

    // Dummy base interface for CommonFileDialog coclasses.
    internal interface NativeCommonFileDialog
    {
    }

    // Coclass interfaces - designed to "look like" the object 
    // in the API, so that the 'new' operator can be used in a 
    // straightforward way. Behind the scenes, the C# compiler
    // morphs all 'new CoClass()' calls to 'new CoClassWrapper()'.

    [ComImport,
    Guid(ShellIIDGuid.IFileOpenDialog),
    CoClass(typeof(FileOpenDialogRCW))]
    internal interface NativeFileOpenDialog : IFileOpenDialog
    {
    }

    [ComImport,
    Guid(ShellIIDGuid.IFileSaveDialog),
    CoClass(typeof(FileSaveDialogRCW))]
    internal interface NativeFileSaveDialog : IFileSaveDialog
    {
    }

    // .NET classes representing runtime callable wrappers.
    [ComImport,
    ClassInterface(ClassInterfaceType.None),
    TypeLibType(TypeLibTypeFlags.FCanCreate),
    Guid(ShellCLSIDGuid.FileOpenDialog)]
    internal class FileOpenDialogRCW
    {
    }

    [ComImport,
    ClassInterface(ClassInterfaceType.None),
    TypeLibType(TypeLibTypeFlags.FCanCreate),
    Guid(ShellCLSIDGuid.FileSaveDialog)]
    internal class FileSaveDialogRCW
    {
    }

}
