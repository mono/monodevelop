// Copyright (c) Microsoft Corporation.  All rights reserved.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Microsoft.WindowsAPICodePack.Shell.PropertySystem;
using MS.WindowsAPICodePack.Internal;

namespace Microsoft.WindowsAPICodePack.Sensors
{
    /// <summary>
    /// Holds a collection of PROPERTYKEY values. This interface can be retrieved from a method 
    /// or, if a new object is required, call CoCreate with CLSID_PortableDeviceKeyCollection.
    /// </summary>
    [ComImport, Guid("DADA2357-E0AD-492E-98DB-DD61C53BA353"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IPortableDeviceKeyCollection
    {
        void GetCount(out UInt32 pcElems);
        [PreserveSig]
        HResult GetAt([In] UInt32 dwIndex, out PropertyKey pKey);
        void Add([In] ref PropertyKey Key);
        void Clear();
        void RemoveAt([In] UInt32 dwIndex);
    }

    /// <summary>
    /// The nativeIPortableDeviceValues interface holds a collection of PROPERTYKEY/PropVariant pairs. 
    /// Values in the collection do not need to be all the same VARTYPE. Values are stored as key-value 
    /// pairs; each key must be unique in the collection. Clients can search for items by PROPERTYKEY 
    /// or zero-based index. Data values are stored as PropVariant structures. You can add or retrieve 
    /// values of any type by using the generic methods SetValue and GetValue, or you add items by using 
    /// the method specific to the type of data added. 
    /// </summary>
    [ComImport, Guid("6848F6F2-3155-4F86-B6F5-263EEEAB3143"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IPortableDeviceValues
    {
        void GetCount([In] ref uint pcelt);
        void GetAt([In] uint index, [In, Out] ref PropertyKey pKey, [In, Out] PropVariant pValue);
        void SetValue([In] ref PropertyKey key, [In] PropVariant pValue);
        void GetValue([In] ref PropertyKey key, [Out] PropVariant pValue);
        void SetStringValue([In] ref PropertyKey key, [In, MarshalAs(UnmanagedType.LPWStr)] string Value);
        void GetStringValue([In] ref PropertyKey key, [MarshalAs(UnmanagedType.LPWStr)] out string pValue);
        void SetUnsignedIntegerValue([In] ref PropertyKey key, [In] uint Value);
        void GetUnsignedIntegerValue([In] ref PropertyKey key, out uint pValue);
        void SetSignedIntegerValue([In] ref PropertyKey key, [In] int Value);
        void GetSignedIntegerValue([In] ref PropertyKey key, out int pValue);
        void SetUnsignedLargeIntegerValue([In] ref PropertyKey key, [In] ulong Value);
        void GetUnsignedLargeIntegerValue([In] ref PropertyKey key, out ulong pValue);
        void SetSignedLargeIntegerValue([In] ref PropertyKey key, [In] long Value);
        void GetSignedLargeIntegerValue([In] ref PropertyKey key, out long pValue);
        void SetFloatValue([In] ref PropertyKey key, [In] float Value);
        void GetFloatValue([In] ref PropertyKey key, out float pValue);
        void SetErrorValue([In] ref PropertyKey key, [In, MarshalAs(UnmanagedType.Error)] int Value);
        void GetErrorValue([In] ref PropertyKey key, [MarshalAs(UnmanagedType.Error)] out int pValue);
        void SetKeyValue([In] ref PropertyKey key, [In] ref PropertyKey Value);
        void GetKeyValue([In] ref PropertyKey key, out PropertyKey pValue);
        void SetBoolValue([In] ref PropertyKey key, [In] int Value);
        void GetBoolValue([In] ref PropertyKey key, out int pValue);
        void SetIUnknownValue([In] ref PropertyKey key, [In, MarshalAs(UnmanagedType.IUnknown)] object pValue);
        void GetIUnknownValue([In] ref PropertyKey key, [MarshalAs(UnmanagedType.IUnknown)] out object ppValue);
        void SetGuidValue([In] ref PropertyKey key, [In] ref Guid Value);
        void GetGuidValue([In] ref PropertyKey key, out Guid pValue);
        void SetBufferValue([In] ref PropertyKey key, [In] byte[] pValue, [In] uint cbValue);
        void GetBufferValue([In] ref PropertyKey key, [Out] IntPtr ppValue, out uint pcbValue);
        void SetnativeIPortableDeviceValuesValue([In] ref PropertyKey key, [In, MarshalAs(UnmanagedType.Interface)] IPortableDeviceValues pValue);
        void GetnativeIPortableDeviceValuesValue([In] ref PropertyKey key, [MarshalAs(UnmanagedType.Interface)] out IPortableDeviceValues ppValue);
        void SetIPortableDevicePropVariantCollectionValue([In] ref PropertyKey key, [In, MarshalAs(UnmanagedType.Interface)] IPortableDevicePropVariantCollection pValue);
        void GetIPortableDevicePropVariantCollectionValue([In] ref PropertyKey key, [MarshalAs(UnmanagedType.Interface)] out IPortableDevicePropVariantCollection ppValue);
        void SetIPortableDeviceKeyCollectionValue([In] ref PropertyKey key, [In, MarshalAs(UnmanagedType.Interface)] IPortableDeviceKeyCollection pValue);
        void GetIPortableDeviceKeyCollectionValue([In] ref PropertyKey key, [MarshalAs(UnmanagedType.Interface)] out IPortableDeviceKeyCollection ppValue);
        void SetnativeIPortableDeviceValuesCollectionValue([In] ref PropertyKey key, [In, MarshalAs(UnmanagedType.Interface)] IPortableDeviceValuesCollection pValue);
        void GetnativeIPortableDeviceValuesCollectionValue([In] ref PropertyKey key, [MarshalAs(UnmanagedType.Interface)] out IPortableDeviceValuesCollection ppValue);
        void RemoveValue([In] ref PropertyKey key);
        void CopyValuesFromPropertyStore([In, MarshalAs(UnmanagedType.Interface)] IPropertyStore pStore);
        void CopyValuesToPropertyStore([In, MarshalAs(UnmanagedType.Interface)] IPropertyStore pStore);
        void Clear();
    }

    /// <summary>
    /// Holds a collection of indexed nativeIPortableDeviceValues interfaces. This interface can be 
    /// retrieved from a method, or if a new object is required, call CoCreate with 
    /// CLSID_PortableDeviceValuesCollection.
    /// </summary>
    [ComImport, Guid("6E3F2D79-4E07-48C4-8208-D8C2E5AF4A99"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IPortableDeviceValuesCollection
    {
        /// <summary>
        /// Retrieves the number of items in the collection.
        /// </summary>
        /// <param name="pcElems">Pointer to a DWORD that contains the number of nativeIPortableDeviceValues interfaces in the collection.</param>
        void GetCount([In] ref uint pcElems);
        
        /// <summary>
        /// Retrieves an item from the collection by a zero-based index.
        /// </summary>
        /// <param name="dwIndex">DWORD that specifies a zero-based index in the collection.</param>
        /// <param name="ppValues">Address of a variable that receives a pointer to an nativeIPortableDeviceValues interface from the collection. The caller is responsible for calling Release on this interface when done with it</param>
        void GetAt([In] uint dwIndex, [MarshalAs(UnmanagedType.Interface)] out IPortableDeviceValues ppValues);
        
        /// <summary>
        /// Adds an item to the collection.
        /// </summary>
        /// <param name="pValues">Pointer to an nativeIPortableDeviceValues interface to add to the collection. The interface is not actually copied, but AddRef is called on it</param>
        void Add([In, MarshalAs(UnmanagedType.Interface)] IPortableDeviceValues pValues);
        
        /// <summary>
        /// Releases all items from the collection.
        /// </summary>
        void Clear();
        
        /// <summary>
        /// Removes an item from the collection by a zero-based index.
        /// </summary>
        /// <param name="dwIndex">DWORD that specifies a zero-based index in the collection.</param>
        void RemoveAt([In] uint dwIndex);
    }

    /// <summary>
    /// Holds a collection of PropVariant values of the same VARTYPE. The VARTYPE of the first item 
    /// that is added to the collection determines the VARTYPE of the collection. An attempt to add 
    /// an item of a different VARTYPE may fail if the PropVariant value cannot be changed to the 
    /// collection?s current VARTYPE. To change the VARTYPE of the collection manually, call ChangeType
    /// </summary>
    [ComImport, Guid("89B2E422-4F1B-4316-BCEF-A44AFEA83EB3"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IPortableDevicePropVariantCollection
    {
        void GetCount([In] ref uint pcElems);
        void GetAt([In] uint dwIndex, [Out] PropVariant pValue);
        void Add([In] PropVariant pValue);
        void GetType(out ushort pvt);
        void ChangeType([In] ushort vt);
        void Clear();
        void RemoveAt([In] uint dwIndex);
    }

    /// <summary>
    /// Exposes methods for enumerating, getting, and setting property values.
    /// </summary>
    [ComImport, Guid("886D8EEB-8CF2-4446-8D02-CDBA1DBDCF99"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IPropertyStore
    {
        void GetCount(out uint cProps);
        void GetAt([In] uint iProp, out PropertyKey pKey);
        void GetValue([In] ref PropertyKey key, [Out] PropVariant pv);
        void SetValue([In] ref PropertyKey key, [In] PropVariant propvar);
        void Commit();
    }

    [ComImport, Guid("DE2D022D-2480-43BE-97F0-D1FA2CF98F4F"), ClassInterface(ClassInterfaceType.None), TypeLibType(TypeLibTypeFlags.FCanCreate)]
    internal class PortableDeviceKeyCollection : IPortableDeviceKeyCollection
    {
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        public virtual extern void GetCount(out UInt32 pcElems);

        [PreserveSig]
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        public virtual extern HResult GetAt([In] UInt32 dwIndex, out PropertyKey pKey);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        public virtual extern void Add([In] ref PropertyKey Key);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        public virtual extern void Clear();

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        public virtual extern void RemoveAt([In] UInt32 dwIndex);
    }

    [ComImport, Guid("0C15D503-D017-47CE-9016-7B3F978721CC"), ClassInterface(ClassInterfaceType.None), TypeLibType(TypeLibTypeFlags.FCanCreate)]
    internal class PortableDeviceValues : IPortableDeviceValues
    {
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        public virtual extern void GetCount([In] ref uint pcelt);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        public virtual extern void GetAt([In] uint index, [In, Out] ref PropertyKey pKey, [In, Out] PropVariant pValue);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        public virtual extern void SetValue([In] ref PropertyKey key, [In] PropVariant pValue);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        public virtual extern void GetValue([In] ref PropertyKey key, [Out] PropVariant pValue);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        public virtual extern void SetStringValue([In] ref PropertyKey key, [In, MarshalAs(UnmanagedType.LPWStr)] string Value);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        public virtual extern void GetStringValue([In] ref PropertyKey key, [MarshalAs(UnmanagedType.LPWStr)] out string pValue);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        public virtual extern void SetUnsignedIntegerValue([In] ref PropertyKey key, [In] uint Value);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        public virtual extern void GetUnsignedIntegerValue([In] ref PropertyKey key, out uint pValue);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        public virtual extern void SetSignedIntegerValue([In] ref PropertyKey key, [In] int Value);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        public virtual extern void GetSignedIntegerValue([In] ref PropertyKey key, out int pValue);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        public virtual extern void SetUnsignedLargeIntegerValue([In] ref PropertyKey key, [In] ulong Value);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        public virtual extern void GetUnsignedLargeIntegerValue([In] ref PropertyKey key, out ulong pValue);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        public virtual extern void SetSignedLargeIntegerValue([In] ref PropertyKey key, [In] long Value);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        public virtual extern void GetSignedLargeIntegerValue([In] ref PropertyKey key, out long pValue);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        public virtual extern void SetFloatValue([In] ref PropertyKey key, [In] float Value);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        public virtual extern void GetFloatValue([In] ref PropertyKey key, out float pValue);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        public virtual extern void SetErrorValue([In] ref PropertyKey key, [In, MarshalAs(UnmanagedType.Error)] int Value);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        public virtual extern void GetErrorValue([In] ref PropertyKey key, [MarshalAs(UnmanagedType.Error)] out int pValue);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        public virtual extern void SetKeyValue([In] ref PropertyKey key, [In] ref PropertyKey Value);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        public virtual extern void GetKeyValue([In] ref PropertyKey key, out PropertyKey pValue);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        public virtual extern void SetBoolValue([In] ref PropertyKey key, [In] int Value);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        public virtual extern void GetBoolValue([In] ref PropertyKey key, out int pValue);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        public virtual extern void SetIUnknownValue([In] ref PropertyKey key, [In, MarshalAs(UnmanagedType.IUnknown)] object pValue);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        public virtual extern void GetIUnknownValue([In] ref PropertyKey key, [MarshalAs(UnmanagedType.IUnknown)] out object ppValue);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        public virtual extern void SetGuidValue([In] ref PropertyKey key, [In] ref Guid Value);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        public virtual extern void GetGuidValue([In] ref PropertyKey key, out Guid pValue);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        public virtual extern void SetBufferValue([In] ref PropertyKey key, [In] byte[] pValue, [In] uint cbValue);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        public virtual extern void GetBufferValue([In] ref PropertyKey key, [Out] IntPtr ppValue, out uint pcbValue);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        public virtual extern void SetnativeIPortableDeviceValuesValue([In] ref PropertyKey key, [In, MarshalAs(UnmanagedType.Interface)] IPortableDeviceValues pValue);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        public virtual extern void GetnativeIPortableDeviceValuesValue([In] ref PropertyKey key, [MarshalAs(UnmanagedType.Interface)] out IPortableDeviceValues ppValue);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        public virtual extern void SetIPortableDevicePropVariantCollectionValue([In] ref PropertyKey key, [In, MarshalAs(UnmanagedType.Interface)] IPortableDevicePropVariantCollection pValue);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        public virtual extern void GetIPortableDevicePropVariantCollectionValue([In] ref PropertyKey key, [MarshalAs(UnmanagedType.Interface)] out IPortableDevicePropVariantCollection ppValue);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        public virtual extern void SetIPortableDeviceKeyCollectionValue([In] ref PropertyKey key, [In, MarshalAs(UnmanagedType.Interface)] IPortableDeviceKeyCollection pValue);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        public virtual extern void GetIPortableDeviceKeyCollectionValue([In] ref PropertyKey key, [MarshalAs(UnmanagedType.Interface)] out IPortableDeviceKeyCollection ppValue);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        public virtual extern void SetnativeIPortableDeviceValuesCollectionValue([In] ref PropertyKey key, [In, MarshalAs(UnmanagedType.Interface)] IPortableDeviceValuesCollection pValue);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        public virtual extern void GetnativeIPortableDeviceValuesCollectionValue([In] ref PropertyKey key, [MarshalAs(UnmanagedType.Interface)] out IPortableDeviceValuesCollection ppValue);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        public virtual extern void RemoveValue([In] ref PropertyKey key);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        public virtual extern void CopyValuesFromPropertyStore([In, MarshalAs(UnmanagedType.Interface)] IPropertyStore pStore);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        public virtual extern void CopyValuesToPropertyStore([In, MarshalAs(UnmanagedType.Interface)] IPropertyStore pStore);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        public virtual extern void Clear();
    }
}
