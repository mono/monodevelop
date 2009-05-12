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
    using System.Reflection;
    using System.Runtime.InteropServices;
    using System.Runtime.InteropServices.ComTypes;

    [StructLayout(LayoutKind.Sequential)]
    public struct ImageDebugDirectory {
        int     Characteristics;
        int     TimeDateStamp;
        short   MajorVersion;
        short   MinorVersion;
        int     Type;
        int     SizeOfData;
        int     AddressOfRawData;
        int     PointerToRawData;
    };

    [
        ComVisible(false)
    ]
    internal interface ISymbolWriter2 : ISymbolWriter
    {
    
        void Initialize(Object emitter,
                    String filename,
                    Boolean fullBuild);
    
        void Initialize(Object emitter,
                        String filename,
                        IStream stream,
                        Boolean fullBuild);
    
    
        byte[] GetDebugInfo(out ImageDebugDirectory iDD);
                             
        void RemapToken(SymbolToken oldToken,
                            SymbolToken newToken);
                             
        void Initialize(Object emitter,
                        String tempfilename,
                        IStream stream,
                        Boolean fullBuild,
                        String finalfilename);

        void DefineConstant(String name,
                               Object value,
                               byte[] signature);
    
        void Abort();   

        void DefineLocalVariable(String name,
                                     int attributes,
                                     SymbolToken sigToken,
                                     int addressKind,
                                     int addr1,
                                     int addr2,
                                     int addr3,
                                     int startOffset,
                                     int endOffset);
    
        void DefineGlobalVariable(String name,
                                       int attributes,
                                       SymbolToken sigToken,
                                       int addressKind,
                                       int addr1,
                                       int addr2,
                                       int addr3);
        
        
         void DefineConstant(String name,
                                  Object value,
                                  SymbolToken sigToken);
    }
}
