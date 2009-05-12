//---------------------------------------------------------------------
//  This file is part of the CLR Managed Debugger (mdbg) Sample.
// 
//  Copyright (C) Microsoft Corporation.  All rights reserved.
//---------------------------------------------------------------------


// These interfaces serve as an extension to the BCL's SymbolStore interfaces.
namespace Microsoft.Samples.Debugging.CorSymbolStore 
{
    using System.Diagnostics.SymbolStore;

    // Interface does not need to be marked with the serializable attribute
    using System;
	using System.Text;
    using System.Runtime.InteropServices;
    using System.Runtime.InteropServices.ComTypes;

    [
        ComImport,
        Guid("B4CE6286-2A6B-3712-A3B7-1EE1DAD467B5"),
        InterfaceType(ComInterfaceType.InterfaceIsIUnknown),
        ComVisible(false)
    ]
    internal interface ISymUnmanagedReader
    {
        void GetDocument([MarshalAs(UnmanagedType.LPWStr)] String url,
                              Guid language,
                              Guid languageVendor,
                              Guid documentType,
                              [MarshalAs(UnmanagedType.Interface)] out ISymUnmanagedDocument retVal);
  
        void GetDocuments(int cDocs,
                               out int pcDocs,
                               [In, Out, MarshalAs(UnmanagedType.LPArray)] ISymUnmanagedDocument[] pDocs);
        
    
        void GetUserEntryPoint(out SymbolToken EntryPoint);
    
        void GetMethod(SymbolToken methodToken,
                          [MarshalAs(UnmanagedType.Interface)] out ISymUnmanagedMethod retVal);
    
        void GetMethodByVersion(SymbolToken methodToken,
                                      int version,
                                      [MarshalAs(UnmanagedType.Interface)] out ISymUnmanagedMethod retVal);
    
        void GetVariables(SymbolToken parent,
                            int pVars,
                            out int pcVars,
                            [In, Out, MarshalAs(UnmanagedType.LPArray)] ISymUnmanagedVariable[] vars);

        void GetGlobalVariables(int cVars,
                                    out int pcVars,
                                    [In, Out, MarshalAs(UnmanagedType.LPArray)] ISymUnmanagedVariable[] vars);

         
        void GetMethodFromDocumentPosition(ISymUnmanagedDocument document,
                                              int line,
                                              int column,
                                              [MarshalAs(UnmanagedType.Interface)] out ISymUnmanagedMethod retVal);
    
        void GetSymAttribute(SymbolToken parent,
                                [MarshalAs(UnmanagedType.LPWStr)] String name,
                                int sizeBuffer,
                                out int lengthBuffer,
                                byte[] buffer);
    
        void GetNamespaces(int cNameSpaces,
                                out int pcNameSpaces,
                                [In, Out, MarshalAs(UnmanagedType.LPArray)] ISymUnmanagedNamespace[] namespaces);
    
        void Initialize(IntPtr importer,
                       [MarshalAs(UnmanagedType.LPWStr)] String filename,
                       [MarshalAs(UnmanagedType.LPWStr)] String searchPath,
                       IStream stream);
    
        void UpdateSymbolStore([MarshalAs(UnmanagedType.LPWStr)] String filename,
                                     IStream stream);
    
        void ReplaceSymbolStore([MarshalAs(UnmanagedType.LPWStr)] String filename,
                                      IStream stream);
    
        void GetSymbolStoreFileName(int cchName,
                                           out int pcchName,
                                           [MarshalAs(UnmanagedType.LPWStr)] StringBuilder szName);
    
        void GetMethodsFromDocumentPosition(ISymUnmanagedDocument document,
                                                      int line,
                                                      int column,
                                                      int cMethod,
                                                      out int pcMethod,
                                                      [In, Out, MarshalAs(UnmanagedType.LPArray)] ISymUnmanagedMethod[] pRetVal);
    
        void GetDocumentVersion(ISymUnmanagedDocument pDoc,
                                      out int version,
                                      out Boolean pbCurrent);
    
        void GetMethodVersion(ISymUnmanagedMethod pMethod,
                                   out int version);
    };

    [
        ComImport,
        Guid("E502D2DD-8671-4338-8F2A-FC08229628C4"),
        InterfaceType(ComInterfaceType.InterfaceIsIUnknown),
        ComVisible(false)
    ]
    internal interface ISymUnmanagedEncUpdate
    {

        void UpdateSymbolStore2(IStream stream,
                                      [In, Out, MarshalAs(UnmanagedType.LPArray)] SymbolLineDelta[] iSymbolLineDeltas,
                                      int cDeltaLines);
    
        void GetLocalVariableCount(SymbolToken mdMethodToken,
                                        out int pcLocals);
    
        void GetLocalVariables(SymbolToken mdMethodToken,
                                  int cLocals,
                                  [In, Out, MarshalAs(UnmanagedType.LPArray)] ISymUnmanagedVariable[] rgLocals,
                                  out int pceltFetched);
    }
    

    [
        ComImport,
        Guid("20D9645D-03CD-4e34-9C11-9848A5B084F1"),
        InterfaceType(ComInterfaceType.InterfaceIsIUnknown),
        ComVisible(false)
    ]
    internal interface ISymUnmanagedReaderSymbolSearchInfo
    {
        void GetSymbolSearchInfoCount(out int pcSearchInfo);
    
        void GetSymbolSearchInfo(int cSearchInfo,
                                    out int pcSearchInfo,
                                    [In, Out, MarshalAs(UnmanagedType.LPArray)] ISymUnmanagedSymbolSearchInfo[] searchInfo);
    }
    

   internal class SymReader : ISymbolReader, ISymbolReader2, ISymbolReaderSymbolSearchInfo, ISymbolEncUpdate, IDisposable
    {
    
        private ISymUnmanagedReader m_reader; // Unmanaged Reader pointer
    
        internal SymReader(ISymUnmanagedReader reader)
        {
            m_reader = reader;
        }

        public void Dispose()
        {
            // Release our unmanaged resources
            m_reader = null;
        }

        public ISymbolDocument GetDocument(String url,
                                        Guid language,
                                        Guid languageVendor,
                                        Guid documentType)
        {
            ISymUnmanagedDocument document = null;
            m_reader.GetDocument(url, language, languageVendor, documentType, out document);
            return new SymbolDocument(document);
        }

        public ISymbolDocument[] GetDocuments()
        {
            int cDocs = 0;
            m_reader.GetDocuments(0, out cDocs, null);
            ISymUnmanagedDocument[] unmanagedDocuments = new ISymUnmanagedDocument[cDocs];
            m_reader.GetDocuments(cDocs, out cDocs, unmanagedDocuments);

            ISymbolDocument[] documents = new SymbolDocument[cDocs];
            uint i;
            for (i = 0; i < cDocs; i++)
            {
                documents[i] = new SymbolDocument(unmanagedDocuments[i]);
            }
            return documents;
        }

        public SymbolToken UserEntryPoint 
        { 
            get
            {
                 SymbolToken entryPoint;
                 m_reader.GetUserEntryPoint(out entryPoint);
                 return entryPoint;
             }
        }

        public ISymbolMethod GetMethod(SymbolToken method)
        {
            ISymUnmanagedMethod unmanagedMethod = null;
            m_reader.GetMethod(method, out unmanagedMethod);
            return new SymMethod(unmanagedMethod);
        }

        public ISymbolMethod GetMethod(SymbolToken method, int version)
        {
            ISymUnmanagedMethod unmanagedMethod = null;
            m_reader.GetMethodByVersion(method, version, out unmanagedMethod);
            return new SymMethod(unmanagedMethod);
        }
        
        public ISymbolVariable[] GetVariables(SymbolToken parent)
        {
            int cVars = 0;
            uint i;
            m_reader.GetVariables(parent, 0, out cVars, null);
            ISymUnmanagedVariable[] unmanagedVariables = new ISymUnmanagedVariable[cVars];
            m_reader.GetVariables(parent, cVars, out cVars, unmanagedVariables);
            SymVariable[] variables = new SymVariable[cVars];

            for (i = 0; i < cVars; i++)
            {
                variables[i] = new SymVariable(unmanagedVariables[i]);
            }
            return variables;
        }

        public ISymbolVariable[] GetGlobalVariables()
        {
            int cVars = 0;
            uint i;
            m_reader.GetGlobalVariables(0, out cVars, null);
            ISymUnmanagedVariable[] unmanagedVariables = new ISymUnmanagedVariable[cVars];
            m_reader.GetGlobalVariables(cVars, out cVars, unmanagedVariables);
            SymVariable[] variables = new SymVariable[cVars];
            
            for (i = 0; i < cVars; i++)
            {
                variables[i] = new SymVariable(unmanagedVariables[i]);
            }
            return variables;
        }

        public ISymbolMethod GetMethodFromDocumentPosition(ISymbolDocument document,
                                                        int line,
                                                        int column)
        {
            ISymUnmanagedMethod unmanagedMethod = null;
            m_reader.GetMethodFromDocumentPosition(((SymbolDocument)document).InternalDocument, line, column, out unmanagedMethod);
            return new SymMethod(unmanagedMethod);
        }

        public byte[] GetSymAttribute(SymbolToken parent, String name)
        {
            byte[] Data;
            int cData = 0;
            m_reader.GetSymAttribute(parent, name, 0, out cData, null);
            Data = new byte[cData];
            m_reader.GetSymAttribute(parent, name, cData, out cData, Data);
            return Data;
        }

        public ISymbolNamespace[] GetNamespaces()
        {
            int count = 0;
            uint i;
            m_reader.GetNamespaces(0, out count, null);
            ISymUnmanagedNamespace[] unmanagedNamespaces = new ISymUnmanagedNamespace[count];
            m_reader.GetNamespaces(count, out count, unmanagedNamespaces);
            ISymbolNamespace[] namespaces = new SymNamespace[count];
            
            for (i = 0; i < count; i++)
            {
                namespaces[i] = new SymNamespace(unmanagedNamespaces[i]);
            }
            return namespaces;
        }

        public void Initialize(Object importer, String filename,
                       String searchPath, IStream stream)
        {
            IntPtr uImporter = IntPtr.Zero;
            try {
                uImporter = Marshal.GetIUnknownForObject(importer);
                m_reader.Initialize(uImporter, filename, searchPath, stream);
            } finally {
                if (uImporter != IntPtr.Zero)
                    Marshal.Release(uImporter);
            }
        }
        
        public void UpdateSymbolStore(String fileName, IStream stream)
        {
            m_reader.UpdateSymbolStore(fileName, stream);
        }

        public void ReplaceSymbolStore(String fileName, IStream stream)
        {
            m_reader.ReplaceSymbolStore(fileName, stream);
        }

        
        public String GetSymbolStoreFileName()
        {            
            StringBuilder fileName;
            int count = 0;
            
            // @todo - there's a bug in Diasymreader where we can't query the size of the pdb filename.
            // So we'll just estimate large as a workaround. See VSWhidbey bug 321941.
            //m_reader.GetSymbolStoreFileName(0, out count, null);
            count = 300;
            fileName = new StringBuilder(count);
            m_reader.GetSymbolStoreFileName(count, out count, fileName);
            return fileName.ToString();
        }
        
        public ISymbolMethod[] GetMethodsFromDocumentPosition(
                ISymbolDocument document, int line, int column)
        
        {
            ISymUnmanagedMethod[] unmanagedMethods;
            ISymbolMethod[] methods;
            int count = 0;
            uint i;
            m_reader.GetMethodsFromDocumentPosition(((SymbolDocument)document).InternalDocument, line, column, 0, out count, null);
            unmanagedMethods = new ISymUnmanagedMethod[count];
            m_reader.GetMethodsFromDocumentPosition(((SymbolDocument)document).InternalDocument, line, column, count, out count, unmanagedMethods);
            methods = new ISymbolMethod[count];
            
            for (i = 0; i < count; i++)
            {
                methods[i] = new SymMethod(unmanagedMethods[i]);
            }
            return methods;
        }
        
        public int GetDocumentVersion(ISymbolDocument document,
                                     out Boolean isCurrent)
        {
            int version = 0;
            m_reader.GetDocumentVersion(((SymbolDocument)document).InternalDocument, out version, out isCurrent);
            return version;
        }
        
        public int GetMethodVersion(ISymbolMethod method)
        {
            int version = 0;
            m_reader.GetMethodVersion(((SymMethod)method).InternalMethod, out version);
            return version;
        }


        public void UpdateSymbolStore(IStream stream,
                                     SymbolLineDelta[] iSymbolLineDeltas)
        {
            ((ISymUnmanagedEncUpdate)m_reader).UpdateSymbolStore2(stream, iSymbolLineDeltas, iSymbolLineDeltas.Length);
        }
    
        public int GetLocalVariableCount(SymbolToken mdMethodToken)
        {
            int count = 0;
            ((ISymUnmanagedEncUpdate)m_reader).GetLocalVariableCount(mdMethodToken, out count);
            return count;
        }
    
        public ISymbolVariable[] GetLocalVariables(SymbolToken mdMethodToken)
        {
            int count = 0;
            ((ISymUnmanagedEncUpdate)m_reader).GetLocalVariables(mdMethodToken, 0, null, out count);
            ISymUnmanagedVariable[] unmanagedVariables = new ISymUnmanagedVariable[count];
            ((ISymUnmanagedEncUpdate)m_reader).GetLocalVariables(mdMethodToken, count, unmanagedVariables, out count);

            ISymbolVariable[] variables = new ISymbolVariable[count];
            uint i;
            for (i = 0; i < count; i++)
            {
                variables[i] = new SymVariable(unmanagedVariables[i]);
            }
            return variables;
        }

        
        public int GetSymbolSearchInfoCount()
        {
            int count = 0;
            ((ISymUnmanagedReaderSymbolSearchInfo)m_reader).GetSymbolSearchInfoCount(out count);
            return count;
        }
    
        public ISymbolSearchInfo[] GetSymbolSearchInfo()
        {
            int count = 0;
            ((ISymUnmanagedReaderSymbolSearchInfo)m_reader).GetSymbolSearchInfo(0, out count, null);
            ISymUnmanagedSymbolSearchInfo[] unmanagedSearchInfo = new ISymUnmanagedSymbolSearchInfo[count];
            ((ISymUnmanagedReaderSymbolSearchInfo)m_reader).GetSymbolSearchInfo(count, out count, unmanagedSearchInfo);

            ISymbolSearchInfo[] searchInfo = new ISymbolSearchInfo[count];

            uint i;
            for (i = 0; i < count; i++)
            {
                searchInfo[i] = new SymSymbolSearchInfo(unmanagedSearchInfo[i]);
            }
            return searchInfo;
            
        }
    }
}
