//---------------------------------------------------------------------
//  This file is part of the CLR Managed Debugger (mdbg) Sample.
// 
//  Copyright (C) Microsoft Corporation.  All rights reserved.
//---------------------------------------------------------------------


// These interfaces serve as an extension to the BCL's SymbolStore interfaces.
namespace Microsoft.Samples.Debugging.CorSymbolStore 
{
    using System.Diagnostics.SymbolStore;

	using System.Runtime.InteropServices;
	using System;
    
    [
        ComVisible(false)
    ]
    internal interface ISymbolEnCMethod: ISymbolMethod
    {
        String GetFileNameFromOffset(int dwOffset);
   
        int GetLineFromOffset(int dwOffset,
                                  out int pcolumn,
                                  out int pendLine,
                                  out int pendColumn,
                                  out int pdwStartOffset);
    }
}

