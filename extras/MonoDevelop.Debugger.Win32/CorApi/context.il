//---------------------------------------------------------------------
//  This file is part of the CLR Managed Debugger (mdbg) Sample.
// 
//  Copyright (C) Microsoft Corporation.  All rights reserved.
//---------------------------------------------------------------------
//
// this file defines CONTEXT structure just like in Win32
// for the original Win32 definition see winnt.h
//

//
// the following .cs code was used to disassemble this .il
//
/*
using System;
using System.Runtime.InteropServices;

namespace Microsoft.Samples.Debugging.CorDebug.NativeApi
{
enum Win32Sizes
{
	SIZE_OF_80387_REGISTERS = 80,
	MAXIMUM_SUPPORTED_EXTENSION = 512
};

public struct FLOATING_SAVE_AREA 
{
	public UInt32	ControlWord;
	public UInt32	StatusWord;
	public UInt32	TagWord;
	public UInt32	ErrorOffset;
	public UInt32	ErrorSelector;
	public UInt32	DataOffset;
	public UInt32	DataSelector;

	[MarshalAs(UnmanagedType.ByValArray, SizeConst=(int)Win32Sizes.SIZE_OF_80387_REGISTERS)]
	public Byte[]	RegisterArea;
	public UInt32	Cr0NpxState;
} 

public struct WIN32_CONTEXT
{
	// See description of the CONTEXT structure in winnt.h

	public UInt32 ContextFlags;

	public UInt32 Dr0;
	public UInt32 Dr1;
	public UInt32 Dr2;
	public UInt32 Dr3;
	public UInt32 Dr6;
	public UInt32 Dr7;

	public FLOATING_SAVE_AREA FloatSave;

	public UInt32 SegGs;
	public UInt32 SegFs;
	public UInt32 SegEs;
	public UInt32 SegDs;

	public UInt32 Edi;
	public UInt32 Esi;
	public UInt32 Ebx;
	public UInt32 Edx;
	public UInt32 Ecx;
	public UInt32 Eax;

	public UInt32 Ebp;
	public UInt32 Eip;
	public UInt32 SegCs;
	public UInt32 EFlags;
	public UInt32 Esp;
	public UInt32 SegSs;

	[MarshalAs(UnmanagedType.ByValArray, SizeConst=(int)Win32Sizes.MAXIMUM_SUPPORTED_EXTENSION)]
	public Byte[] ExtendedRegisters;
}
}
*/

// =============== CLASS MEMBERS DECLARATION ===================

.namespace Microsoft.Samples.Debugging.CorDebug.NativeApi
{
  .class public sequential ansi sealed beforefieldinit WIN32_FLOATING_SAVE_AREA
         extends [mscorlib]System.ValueType
  {
    .field public uint32 ControlWord
    .field public uint32 StatusWord
    .field public uint32 TagWord
    .field public uint32 ErrorOffset
    .field public uint32 ErrorSelector
    .field public uint32 DataOffset
    .field public uint32 DataSelector
    .field public  marshal( fixed array [80]) uint8[] RegisterArea
    .field public uint32 Cr0NpxState
  } // end of class WIN32_FLOATING_SAVE_AREA

  .class public sequential ansi sealed beforefieldinit WIN32_CONTEXT
         extends [mscorlib]System.ValueType
  {
    .field public uint32 ContextFlags
    .field public uint32 Dr0
    .field public uint32 Dr1
    .field public uint32 Dr2
    .field public uint32 Dr3
    .field public uint32 Dr6
    .field public uint32 Dr7
    .field public valuetype Microsoft.Samples.Debugging.CorDebug.NativeApi.WIN32_FLOATING_SAVE_AREA FloatSave
    .field public uint32 SegGs
    .field public uint32 SegFs
    .field public uint32 SegEs
    .field public uint32 SegDs
    .field public uint32 Edi
    .field public uint32 Esi
    .field public uint32 Ebx
    .field public uint32 Edx
    .field public uint32 Ecx
    .field public uint32 Eax
    .field public uint32 Ebp
    .field public uint32 Eip
    .field public uint32 SegCs
    .field public uint32 EFlags
    .field public uint32 Esp
    .field public uint32 SegSs
    .field public  marshal( fixed array [512]) uint8[] ExtendedRegisters
  } // end of class WIN32_CONTEXT

} // end of namespace Microsoft.Samples.Debugging.CorDebug.NativeApi

