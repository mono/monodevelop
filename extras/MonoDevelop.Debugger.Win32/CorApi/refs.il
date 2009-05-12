//---------------------------------------------------------------------
//  This file is part of the CLR Managed Debugger (mdbg) Sample.
// 
//  Copyright (C) Microsoft Corporation.  All rights reserved.
//---------------------------------------------------------------------

//
// Common assembly refs and declarations for the CorApi.netmodule
// This is the IL portion of the raw API that imports the native interfaces.


.assembly extern mscorlib
{
  // This value should never change, even across versions.
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )                         // .z\V.4..

  // @todo - it would be nice to cooperative with the build
  // to automate this value.
  .ver 2:0:0:0
}

.module CorApi.dll
// [module:System.CLSCompliant(false)]
.custom instance void [mscorlib]System.CLSCompliantAttribute::.ctor(bool) = ( 01 00 01 00 00 ) 
// MVID: {668BBB50-6634-45E5-8256-7ECEEAEC577E}
.imagebase 0x00400000
.subsystem 0x00000003
.file alignment 512
.corflags 0x00000001
