// Copyright (c) Microsoft Corporation.  All rights reserved.

using System;
using System.Runtime.InteropServices;

namespace Microsoft.WindowsAPICodePack.ExtendedLinguisticServices
{

    [Flags]
    internal enum ServiceTypes
    {
        None = 0x0,
        IsOneToOneLanguageMapping = 0x1,
        HasSubServices = 0x2,
        OnlineOnly = 0x4,
        HighLevel = 0x8,
        LowLevel = 0x16,
    }

    [Flags]
    internal enum EnumTypes
    {
        None = 0x0,
        OnlineService = 0x1,
        OfflineService = 0x2,
        HighLevel = 0x4,
        LowLevel = 0x8,
    }

    // Lives in native memory.
    // Only used for a temporary managed copy.
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    internal struct Win32Service
    {
        internal IntPtr _size;
        [MarshalAs(UnmanagedType.LPWStr)]
        internal string _copyright;
        internal ushort _majorVersion;
        internal ushort _minorVersion;
        internal ushort _buildVersion;
        internal ushort _stepVersion;
        internal uint _inputContentTypesCount;
        internal IntPtr _inputContentTypes;
        internal uint _outputContentTypesCount;
        internal IntPtr _outputContentTypes;
        internal uint _inputLanguagesCount;
        internal IntPtr _inputLanguages;
        internal uint _outputLanguagesCount;
        internal IntPtr _outputLanguages;
        internal uint _inputScriptsCount;
        internal IntPtr _inputScripts;
        internal uint _outputScriptsCount;
        internal IntPtr _outputScripts;
        internal Guid _guid;
        [MarshalAs(UnmanagedType.LPWStr)]
        internal string _category;
        [MarshalAs(UnmanagedType.LPWStr)]
        internal string _description;
        internal uint _privateDataSize;
        internal IntPtr _privateData;
        internal IntPtr _context;
        internal ServiceTypes _serviceTypes;
    }

    // Lives in managed memory. Used to pass parameters.
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    internal struct Win32EnumOptions
    {
        internal IntPtr _size;
        [MarshalAs(UnmanagedType.LPWStr)]
        internal string _category;
        [MarshalAs(UnmanagedType.LPWStr)]
        internal string _inputLanguage;
        [MarshalAs(UnmanagedType.LPWStr)]
        internal string _outputLanguage;
        [MarshalAs(UnmanagedType.LPWStr)]
        internal string _inputScript;
        [MarshalAs(UnmanagedType.LPWStr)]
        internal string _outputScript;
        [MarshalAs(UnmanagedType.LPWStr)]
        internal string _inputContentType;
        [MarshalAs(UnmanagedType.LPWStr)]
        internal string _outputContentType;
        internal IntPtr _pGuid;
        internal EnumTypes _serviceTypes;
    }

    // Lives in managed memory. Used to pass parameters.
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    internal struct Win32Options
    {
        internal IntPtr _size;
        [MarshalAs(UnmanagedType.LPWStr)]
        internal string _inputLanguage;
        [MarshalAs(UnmanagedType.LPWStr)]
        internal string _outputLanguage;
        [MarshalAs(UnmanagedType.LPWStr)]
        internal string _inputScript;
        [MarshalAs(UnmanagedType.LPWStr)]
        internal string _outputScript;
        [MarshalAs(UnmanagedType.LPWStr)]
        internal string _inputContentType;
        [MarshalAs(UnmanagedType.LPWStr)]
        internal string _outputContentType;
        internal IntPtr _UILanguage;
        internal IntPtr _recognizeCallback;
        internal IntPtr _recognizeCallerData;
        internal uint _recognizeCallerDataSize;
        internal IntPtr _actionCallback;
        internal IntPtr _actionCallerData;
        internal uint _actionCallerDataSize;
        internal uint _serviceFlag;
        internal uint _getActionDisplayName;
    }

    // Lives in managed memory. Used to represent results.
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    internal struct Win32DataRange
    {
        internal uint _startIndex;
        internal uint _endIndex;
        internal IntPtr _description;
        internal uint _descriptionLength;
        internal IntPtr _data;
        internal uint _dataSize;
        [MarshalAs(UnmanagedType.LPWStr)]
        internal string _contentType;
        internal IntPtr _actionIDs;
        internal uint _actionsCount;
        internal IntPtr _actionDisplayNames;
    }

    // Lives in managed memory.
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    internal struct Win32PropertyBag
    {
        internal IntPtr _size;
        internal IntPtr _ranges;
        internal uint _rangesCount;
        internal IntPtr _serviceData;
        internal uint _serviceDataSize;
        internal IntPtr _callerData;
        internal uint _callerDataSize;
        internal IntPtr _context;
    }

}
