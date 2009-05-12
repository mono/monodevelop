//---------------------------------------------------------------------
//  This file is part of the CLR Managed Debugger (mdbg) Sample.
// 
//  Copyright (C) Microsoft Corporation.  All rights reserved.
//---------------------------------------------------------------------


// These interfaces serve as an extension to the BCL's SymbolStore interfaces.
namespace Microsoft.Samples.Debugging.CorSymbolStore 
{
    using System.Diagnostics.SymbolStore;

    
    using System;
    using System.Text;
    using System.Runtime.InteropServices;
    using System.Runtime.InteropServices.ComTypes;
	
    [
        ComImport,
        Guid("AA544d42-28CB-11d3-bd22-0000f80849bd"),
        InterfaceType(ComInterfaceType.InterfaceIsIUnknown),
        ComVisible(false)
    ]
    internal interface ISymUnmanagedBinder
    {
        void GetReaderForFile(IntPtr importer,
                                  [MarshalAs(UnmanagedType.LPWStr)] String filename,
                                  [MarshalAs(UnmanagedType.LPWStr)] String SearchPath,
                                  [MarshalAs(UnmanagedType.Interface)] out ISymUnmanagedReader retVal);
        
        void GetReaderFromStream(IntPtr importer,
                                        IStream stream,
                                        [MarshalAs(UnmanagedType.Interface)] out ISymUnmanagedReader retVal);
    }

    [
        ComImport,
        Guid("ACCEE350-89AF-4ccb-8B40-1C2C4C6F9434"),
        InterfaceType(ComInterfaceType.InterfaceIsIUnknown),
        ComVisible(false)
    ]
	internal interface ISymUnmanagedBinder2
    {
        // ISymUnmanagedBinder methods (need to define the base interface methods also, per COM interop requirements)
        void GetReaderForFile(IntPtr importer,
                                  [MarshalAs(UnmanagedType.LPWStr)] String filename,
                                  [MarshalAs(UnmanagedType.LPWStr)] String SearchPath,
                                  [MarshalAs(UnmanagedType.Interface)] out ISymUnmanagedReader retVal);
        
        void GetReaderFromStream(IntPtr importer,
                                        IStream stream,
                                        [MarshalAs(UnmanagedType.Interface)] out ISymUnmanagedReader retVal);

        // ISymUnmanagedBinder2 methods 
        void GetReaderForFile2(IntPtr importer,
                                  [MarshalAs(UnmanagedType.LPWStr)] String fileName,
                                  [MarshalAs(UnmanagedType.LPWStr)] String searchPath,
                                  int searchPolicy,
                                  [MarshalAs(UnmanagedType.Interface)] out ISymUnmanagedReader pRetVal);
    }
    
    [
        ComImport,
        Guid("28AD3D43-B601-4d26-8A1B-25F9165AF9D7"),
        InterfaceType(ComInterfaceType.InterfaceIsIUnknown),
        ComVisible(false)
    ]
    internal interface ISymUnmanagedBinder3 : ISymUnmanagedBinder2
    {
        // ISymUnmanagedBinder methods (need to define the base interface methods also, per COM interop requirements)
        new void GetReaderForFile(IntPtr importer,
                                  [MarshalAs(UnmanagedType.LPWStr)] String filename,
                                  [MarshalAs(UnmanagedType.LPWStr)] String SearchPath,
                                  [MarshalAs(UnmanagedType.Interface)] out ISymUnmanagedReader retVal);
        
        new void GetReaderFromStream(IntPtr importer,
                                        IStream stream,
                                        [MarshalAs(UnmanagedType.Interface)] out ISymUnmanagedReader retVal);

        // ISymUnmanagedBinder2 methods 
        new void GetReaderForFile2(IntPtr importer,
				   [MarshalAs(UnmanagedType.LPWStr)] String fileName,
				   [MarshalAs(UnmanagedType.LPWStr)] String searchPath,
				   int searchPolicy,
				   [MarshalAs(UnmanagedType.Interface)] out ISymUnmanagedReader pRetVal);

        // ISymUnmanagedBinder3 methods 
        void GetReaderFromCallback(IntPtr importer,
                                   [MarshalAs(UnmanagedType.LPWStr)] String fileName,
                                   [MarshalAs(UnmanagedType.LPWStr)] String searchPath,
                                   int searchPolicy,
				   IntPtr callback,
                                   [MarshalAs(UnmanagedType.Interface)] out ISymUnmanagedReader pRetVal);
    }

    /// <include file='doc\symbinder.uex' path='docs/doc[@for="SymbolBinder"]/*' />

    public class SymbolBinder: ISymbolBinder1, ISymbolBinder2
    {
        ISymUnmanagedBinder m_binder;

        /// <include file='doc\symbinder.uex' path='docs/doc[@for="SymbolBinder.SymbolBinder"]/*' />
        public SymbolBinder()
        {
            Guid CLSID_CorSymBinder = new Guid("0A29FF9E-7F9C-4437-8B11-F424491E3931");
            m_binder = (ISymUnmanagedBinder)Activator.CreateInstance(Type.GetTypeFromCLSID(CLSID_CorSymBinder));
        }
        
        /// <include file='doc\symbinder.uex' path='docs/doc[@for="SymbolBinder.GetReader"]/*' />
        public ISymbolReader GetReader(IntPtr importer, String filename,
                                          String searchPath)
        {
            ISymUnmanagedReader reader = null;
            m_binder.GetReaderForFile(importer, filename, searchPath, out reader);
            return new SymReader(reader);
        }

        /// <include file='doc\symbinder.uex' path='docs/doc[@for="SymbolBinder.GetReaderForFile"]/*' />
        public ISymbolReader GetReaderForFile(Object importer, String filename,
                                           String searchPath)
        {
            ISymUnmanagedReader reader = null;
            IntPtr uImporter = IntPtr.Zero;
            try {
                uImporter = Marshal.GetIUnknownForObject(importer);
                m_binder.GetReaderForFile(uImporter, filename, searchPath, out reader);
            } finally {
                if (uImporter != IntPtr.Zero)
                    Marshal.Release(uImporter);
            }
            return new SymReader(reader);
        }
        
        /// <include file='doc\symbinder.uex' path='docs/doc[@for="SymbolBinder.GetReaderForFile1"]/*' />
        public ISymbolReader GetReaderForFile(Object importer, String fileName,
                                           String searchPath, SymSearchPolicies searchPolicy)
        {
            ISymUnmanagedReader symReader = null;
            IntPtr uImporter = IntPtr.Zero;
            try {
                uImporter = Marshal.GetIUnknownForObject(importer);
                ((ISymUnmanagedBinder2)m_binder).GetReaderForFile2(uImporter, fileName, searchPath, (int)searchPolicy, out symReader);
            } finally {
                if (uImporter != IntPtr.Zero)
                    Marshal.Release(uImporter);
            }
            return new SymReader(symReader);
        }
        
        /// <include file='doc\symbinder.uex' path='docs/doc[@for="SymbolBinder.GetReaderForFile2"]/*' />
        public ISymbolReader GetReaderForFile(Object importer, String fileName,
                                           String searchPath, SymSearchPolicies searchPolicy,
                                           IntPtr callback)
        {
            ISymUnmanagedReader reader = null;
            IntPtr uImporter = IntPtr.Zero;
            try {
                uImporter = Marshal.GetIUnknownForObject(importer);
                ((ISymUnmanagedBinder3)m_binder).GetReaderFromCallback(uImporter, fileName, searchPath, (int)searchPolicy, callback, out reader);
            } finally {
                if (uImporter != IntPtr.Zero)
                    Marshal.Release(uImporter);
            }
            return new SymReader(reader);
        }
        
        /// <include file='doc\symbinder.uex' path='docs/doc[@for="SymbolBinder.GetReaderFromStream"]/*' />
        public ISymbolReader GetReaderFromStream(Object importer, IStream stream)
        {
            ISymUnmanagedReader reader = null;
            IntPtr uImporter = IntPtr.Zero;
            try {
                uImporter = Marshal.GetIUnknownForObject(importer);
                ((ISymUnmanagedBinder2)m_binder).GetReaderFromStream(uImporter, stream, out reader);
            } finally {
                if (uImporter != IntPtr.Zero)
                    Marshal.Release(uImporter);
            }
            return new SymReader(reader);
        }
    }
    
}
