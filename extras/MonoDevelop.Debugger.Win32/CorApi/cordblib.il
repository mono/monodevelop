//---------------------------------------------------------------------
//  This file is part of the CLR Managed Debugger (mdbg) Sample.
// 
//  Copyright (C) Microsoft Corporation.  All rights reserved.
//---------------------------------------------------------------------

//  Microsoft (R) .NET Framework IL Disassembler.  Version 2.0.31117.00
//  Copyright (C) Microsoft Corporation 1998-2003. All rights reserved.


// MVID: {ED5AC2EB-E2E2-4B6C-B2ED-C74617CFB7E8}
.imagebase 0x00400000
.file alignment 0x00001000
.stackreserve 0x00100000
.subsystem 0x0003       // WINDOWS_CUI
.corflags 0x00000001    //  ILONLY
// Image base: 0x03DF0000


// =============== CLASS MEMBERS DECLARATION ===================

.namespace Microsoft.Samples.Debugging.CorDebug.NativeApi
{
  .class public sequential ansi SECURITY_ATTRIBUTES
         extends ['mscorlib']System.Object
  {
    .pack 8
    .size 0
    .custom instance void ['mscorlib']System.Runtime.InteropServices.ComVisibleAttribute::.ctor(bool) = ( 01 00 00 00 00 ) 
    .field public int32 nLength
    .field public native int lpSecurityDescriptor
    .field public bool bInheritHandle
    .method public hidebysig specialname rtspecialname 
            instance void .ctor() il managed
    {
      // Code size       15 (0xf)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  ldc.i4.s   12
      IL_0003:  stfld      int32 Microsoft.Samples.Debugging.CorDebug.NativeApi.SECURITY_ATTRIBUTES::nLength
      IL_0008:  ldarg.0
      IL_0009:  call       instance void ['mscorlib']System.Object::.ctor()
      IL_000e:  ret
    } // end of method 'SECURITY_ATTRIBUTES::.ctor'
 
  } // end of class 'SECURITY_ATTRIBUTES'

  .class public sequential autochar STARTUPINFO
         extends ['mscorlib']System.Object
  {
    .pack 8
    .size 0
    .custom instance void ['mscorlib']System.Runtime.InteropServices.ComVisibleAttribute::.ctor(bool) = ( 01 00 00 00 00 ) 
    .field public int32 cb
    .field public class System.String lpReserved
    .field public class System.String lpDesktop
    .field public class System.String lpTitle
    .field public int32 dwX
    .field public int32 dwY
    .field public int32 dwXSize
    .field public int32 dwYSize
    .field public int32 dwXCountChars
    .field public int32 dwYCountChars
    .field public int32 dwFillAttribute
    .field public int32 dwFlags
    .field public int16 wShowWindow
    .field public int16 cbReserved2
    .field public native int lpReserved2
    .field public class [mscorlib]Microsoft.Win32.SafeHandles.SafeFileHandle hStdInput
    .field public class [mscorlib]Microsoft.Win32.SafeHandles.SafeFileHandle hStdOutput
    .field public class [mscorlib]Microsoft.Win32.SafeHandles.SafeFileHandle hStdError
    .method public hidebysig specialname rtspecialname 
            instance void .ctor() il managed
    {
      // Code size       7 (0x7)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  call       instance void ['mscorlib']System.Object::.ctor()
      IL_0006:  ret
    } // end of method 'STARTUPINFO::.ctor'

  } // end of class 'STARTUPINFO'


  .class public sequential ansi PROCESS_INFORMATION
         extends ['mscorlib']System.Object
  {
    .pack 8
    .size 0
    .custom instance void ['mscorlib']System.Runtime.InteropServices.ComVisibleAttribute::.ctor(bool) = ( 01 00 00 00 00 ) 
    .field public native int hProcess
    .field public native int hThread
    .field public int32 dwProcessId
    .field public int32 dwThreadId
    .method public hidebysig specialname rtspecialname 
            instance void .ctor() il managed
    {
      // Code size       7 (0x7)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  call       instance void ['mscorlib']System.Object::.ctor()
      IL_0006:  ret
    } // end of method 'PROCESS_INFORMATION::.ctor'

  } // end of class 'PROCESS_INFORMATION'

  //
  // This whole part is union definition for DEBUG_EVENT
  // 

  .class public sequential ansi sealed beforefieldinit CREATE_PROCESS_DEBUG_INFO
  extends [mscorlib]System.ValueType
  {
  .field public native int hFile
  .field public native int hProcess
  .field public native int hThread
  .field public native int lpBaseOfImage
  .field public uint32 dwDebugInfoFileOffset
  .field public uint32 nDebugInfoSize
  .field public native int lpThreadLocalBase
  .field public native int lpStartAddress
  .field public native int lpImageName
  .field public uint16 fUnicode
  } // end of class CREATE_PROCESS_DEBUG_INFO

  .class public sequential ansi sealed beforefieldinit EXCEPTION_RECORD
  extends [mscorlib]System.ValueType
  {
  .field public uint32 ExceptionCode
  .field public uint32 ExceptionFlags
  .field public native int ExceptionRecord
  .field public native int ExceptionAddress
  .field public uint32 NumberParameters
  .field public native int ExceptionAddress0
  .field public native int ExceptionAddress1
  .field public native int ExceptionAddress2
  .field public native int ExceptionAddress3
  } // end of class EXCEPTION_RECORD

  .class public sequential ansi sealed beforefieldinit EXCEPTION_DEBUG_INFO
  extends [mscorlib]System.ValueType
  {
  .field public valuetype Microsoft.Samples.Debugging.CorDebug.NativeApi.EXCEPTION_RECORD ExceptionRecord
  .field public uint32 dwFirstChance
  } // end of class EXCEPTION_DEBUG_INFO

  .class public sequential ansi sealed beforefieldinit CREATE_THREAD_DEBUG_INFO
  extends [mscorlib]System.ValueType
  {
  .field public native int hThread
  .field public native int lpThreadLocalBase
  .field public native int lpStartAddress
  } // end of class CREATE_THREAD_DEBUG_INFO

  .class public sequential ansi sealed beforefieldinit EXIT_THREAD_DEBUG_INFO
  extends [mscorlib]System.ValueType
  {
  .field public uint32 dwExitCode
  } // end of class EXIT_THREAD_DEBUG_INFO

  .class public sequential ansi sealed beforefieldinit EXIT_PROCESS_DEBUG_INFO
  extends [mscorlib]System.ValueType
  {
  .field public uint32 dwExitCode
  } // end of class EXIT_PROCESS_DEBUG_INFO

  .class public sequential ansi sealed beforefieldinit LOAD_DLL_DEBUG_INFO
  extends [mscorlib]System.ValueType
  {
  .field public native int hFile
  .field public native int lpBaseOfDll
  .field public uint32 dwDebugInfoFileOffset
  .field public uint32 nDebugInfoSize
  .field public native int lpImageName
  .field public uint16 fUnicode
  } // end of class LOAD_DLL_DEBUG_INFO

  .class public sequential ansi sealed beforefieldinit UNLOAD_DLL_DEBUG_INFO
  extends [mscorlib]System.ValueType
  {
  .field public native int lpBaseOfDll
  } // end of class UNLOAD_DLL_DEBUG_INFO

  .class public sequential ansi sealed beforefieldinit OUTPUT_DEBUG_STRING_INFO
  extends [mscorlib]System.ValueType
  {
  .field public native int lpDebugStringData
  .field public uint16 fUnicode
  .field public uint16 nDebugStringLenght
  } // end of class OUTPUT_DEBUG_STRING_INFO

  .class public explicit ansi beforefieldinit DEBUG_EVENT
  extends [mscorlib]System.Object
  {
  .field [0] public uint32 dwDebugEventCode
  .field [4] public uint32 dwProcessId
  .field [8] public uint32 dwThreadId
  .field [12] public valuetype Microsoft.Samples.Debugging.CorDebug.NativeApi.CREATE_PROCESS_DEBUG_INFO CreateProcess
  .field [12] public valuetype Microsoft.Samples.Debugging.CorDebug.NativeApi.EXCEPTION_DEBUG_INFO Exception
  .field [12] public valuetype Microsoft.Samples.Debugging.CorDebug.NativeApi.CREATE_THREAD_DEBUG_INFO CreateThread
  .field [12] public valuetype Microsoft.Samples.Debugging.CorDebug.NativeApi.EXIT_THREAD_DEBUG_INFO ExitThread
  .field [12] public valuetype Microsoft.Samples.Debugging.CorDebug.NativeApi.EXIT_PROCESS_DEBUG_INFO ExitProcess
  .field [12] public valuetype Microsoft.Samples.Debugging.CorDebug.NativeApi.LOAD_DLL_DEBUG_INFO LoadDll
  .field [12] public valuetype Microsoft.Samples.Debugging.CorDebug.NativeApi.UNLOAD_DLL_DEBUG_INFO UnloadDll
  .field [12] public valuetype Microsoft.Samples.Debugging.CorDebug.NativeApi.OUTPUT_DEBUG_STRING_INFO OutputDebugString
  .method public hidebysig specialname rtspecialname 
  instance void  .ctor() cil managed
  {
  // Code size       7 (0x7)
  .maxstack  8
  IL_0000:  ldarg.0
  IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
  IL_0006:  ret
  } // end of method DEBUG_EVENT::.ctor

  } // end of class DEBUG_EVENT
}
.class public auto ansi import Microsoft.Samples.Debugging.CorDebug.NativeApi.CorDebugClass
       extends [mscorlib]System.Object
       implements Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebug,
                  Microsoft.Samples.Debugging.CorDebug.NativeApi.CorDebug
{
  .custom instance void [mscorlib]System.Runtime.InteropServices.TypeLibTypeAttribute::.ctor(int16) = ( 01 00 02 00 00 00 ) 
  .custom instance void [mscorlib]System.Runtime.InteropServices.ClassInterfaceAttribute::.ctor(int16) = ( 01 00 00 00 00 00 ) 
  .custom instance void [mscorlib]System.Runtime.InteropServices.GuidAttribute::.ctor(string) = ( 01 00 24 36 66 65 66 34 34 64 30 2D 33 39 65 37   // ..$6fef44d0-39e7
                                                                                                  2D 34 63 37 37 2D 62 65 38 65 2D 63 39 66 38 63   // -4c77-be8e-c9f8c
                                                                                                  66 39 38 38 36 33 30 00 00 )                      // f988630..
  .method public specialname rtspecialname 
          instance void  .ctor() runtime managed internalcall
  {
  } // end of method CorDebugClass::.ctor

  .method public hidebysig newslot virtual 
          instance void  Initialize() runtime managed internalcall
  {
    .override Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebug::Initialize
  } // end of method CorDebugClass::Initialize

  .method public hidebysig newslot virtual 
          instance void  Terminate() runtime managed internalcall
  {
    .override Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebug::Terminate
  } // end of method CorDebugClass::Terminate

  .method public hidebysig newslot virtual 
          instance void  SetManagedHandler([in] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugManagedCallback  marshal( interface) pCallback) runtime managed internalcall
  {
    .override Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebug::SetManagedHandler
  } // end of method CorDebugClass::SetManagedHandler

  .method public hidebysig newslot virtual 
          instance void  SetUnmanagedHandler([in] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugUnmanagedCallback  marshal( interface) pCallback) runtime managed internalcall
  {
    .override Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebug::SetUnmanagedHandler
  } // end of method CorDebugClass::SetUnmanagedHandler

  .method public hidebysig newslot virtual 
          instance void  CreateProcess([in] string  marshal( lpwstr) lpApplicationName,
                                       [in] string  marshal( lpwstr) lpCommandLine,
                                       [in] class Microsoft.Samples.Debugging.CorDebug.NativeApi.SECURITY_ATTRIBUTES lpProcessAttributes,
                                       [in] class Microsoft.Samples.Debugging.CorDebug.NativeApi.SECURITY_ATTRIBUTES lpThreadAttributes,
                                       [in] int32 bInheritHandles,
                                       [in] uint32 dwCreationFlags,
                                       [in] native int lpEnvironment,
                                       [in] string  marshal( lpwstr) lpCurrentDirectory,
                                       [in] class Microsoft.Samples.Debugging.CorDebug.NativeApi.STARTUPINFO lpStartupInfo,
                                       [in] class Microsoft.Samples.Debugging.CorDebug.NativeApi.PROCESS_INFORMATION lpProcessInformation,
                                       [in] valuetype Microsoft.Samples.Debugging.CorDebug.NativeApi.CorDebugCreateProcessFlags debuggingFlags,
                                       [out] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugProcess&  marshal( interface) ppProcess) runtime managed internalcall
  {
    .override Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebug::CreateProcess
  } // end of method CorDebugClass::CreateProcess

  .method public hidebysig newslot virtual 
          instance void  DebugActiveProcess([in] uint32 id,
                                            [in] int32 win32Attach,
                                            [out] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugProcess&  marshal( interface) ppProcess) runtime managed internalcall
  {
    .override Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebug::DebugActiveProcess
  } // end of method CorDebugClass::DebugActiveProcess

  .method public hidebysig newslot virtual 
          instance void  EnumerateProcesses([out] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugProcessEnum&  marshal( interface) ppProcess) runtime managed internalcall
  {
    .override Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebug::EnumerateProcesses
  } // end of method CorDebugClass::EnumerateProcesses

  .method public hidebysig newslot virtual 
          instance void  GetProcess([in] uint32 dwProcessId,
                                    [out] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugProcess&  marshal( interface) ppProcess) runtime managed internalcall
  {
    .override Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebug::GetProcess
  } // end of method CorDebugClass::GetProcess

  .method public hidebysig newslot virtual 
          instance void  CanLaunchOrAttach([in] uint32 dwProcessId,
                                           [in] int32 win32DebuggingEnabled) runtime managed internalcall
  {
    .override Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebug::CanLaunchOrAttach
  } // end of method CorDebugClass::CanLaunchOrAttach

} // end of class Microsoft.Samples.Debugging.CorDebug.NativeApi.CorDebugClass

.class interface public abstract auto ansi import Microsoft.Samples.Debugging.CorDebug.NativeApi.CorDebug
       implements Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebug
{
  .custom instance void [mscorlib]System.Runtime.InteropServices.CoClassAttribute::.ctor(class [mscorlib]System.Type) = ( 01 00 3C 4D 69 63 72 6F 73 6F 66 74 2E 53 61 6D   // ..<Microsoft.Sam
                                                                                                                          70 6C 65 73 2E 44 65 62 75 67 67 69 6E 67 2E 43   // ples.Debugging.C
                                                                                                                          6F 72 44 65 62 75 67 2E 4E 61 74 69 76 65 41 70   // orDebug.NativeAp
                                                                                                                          69 2E 43 6F 72 44 65 62 75 67 43 6C 61 73 73 00   // i.CorDebugClass.
                                                                                                                          00 ) 

  .custom instance void [mscorlib]System.Runtime.InteropServices.GuidAttribute::.ctor(string) = ( 01 00 24 33 44 36 46 35 46 36 31 2D 37 35 33 38   // ..$3D6F5F61-7538
                                                                                                  2D 31 31 44 33 2D 38 44 35 42 2D 30 30 31 30 34   // -11D3-8D5B-00104
                                                                                                  42 33 35 45 37 45 46 00 00 )                      // B35E7EF..
} // end of class Microsoft.Samples.Debugging.CorDebug.NativeApi.CorDebug

.class interface public abstract auto ansi import Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebug
{
  .custom instance void [mscorlib]System.Runtime.InteropServices.GuidAttribute::.ctor(string) = ( 01 00 24 33 44 36 46 35 46 36 31 2D 37 35 33 38   // ..$3D6F5F61-7538
                                                                                                  2D 31 31 44 33 2D 38 44 35 42 2D 30 30 31 30 34   // -11D3-8D5B-00104
                                                                                                  42 33 35 45 37 45 46 00 00 )                      // B35E7EF..
  .custom instance void [mscorlib]System.Runtime.InteropServices.InterfaceTypeAttribute::.ctor(int16) = ( 01 00 01 00 00 00 ) 
  .method public hidebysig newslot abstract virtual 
          instance void  Initialize() runtime managed internalcall
  {
  } // end of method ICorDebug::Initialize

  .method public hidebysig newslot abstract virtual 
          instance void  Terminate() runtime managed internalcall
  {
  } // end of method ICorDebug::Terminate

  .method public hidebysig newslot abstract virtual 
          instance void  SetManagedHandler([in] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugManagedCallback  marshal( interface) pCallback) runtime managed internalcall
  {
  } // end of method ICorDebug::SetManagedHandler

  .method public hidebysig newslot abstract virtual 
          instance void  SetUnmanagedHandler([in] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugUnmanagedCallback  marshal( interface) pCallback) runtime managed internalcall
  {
  } // end of method ICorDebug::SetUnmanagedHandler

  .method public hidebysig newslot abstract virtual 
          instance void  CreateProcess([in] string  marshal( lpwstr) lpApplicationName,
                                       [in] string  marshal( lpwstr) lpCommandLine,
                                       [in] class Microsoft.Samples.Debugging.CorDebug.NativeApi.SECURITY_ATTRIBUTES lpProcessAttributes,
                                       [in] class Microsoft.Samples.Debugging.CorDebug.NativeApi.SECURITY_ATTRIBUTES lpThreadAttributes,
                                       [in] int32 bInheritHandles,
                                       [in] uint32 dwCreationFlags,
                                       [in] native int lpEnvironment,
                                       [in] string  marshal( lpwstr) lpCurrentDirectory,
                                       [in] class Microsoft.Samples.Debugging.CorDebug.NativeApi.STARTUPINFO lpStartupInfo,
                                       [in] class Microsoft.Samples.Debugging.CorDebug.NativeApi.PROCESS_INFORMATION lpProcessInformation,
                                       [in] valuetype Microsoft.Samples.Debugging.CorDebug.NativeApi.CorDebugCreateProcessFlags debuggingFlags,
                                       [out] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugProcess&  marshal( interface) ppProcess) runtime managed internalcall
  {
  } // end of method ICorDebug::CreateProcess

  .method public hidebysig newslot abstract virtual 
          instance void  DebugActiveProcess([in] uint32 id,
                                            [in] int32 win32Attach,
                                            [out] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugProcess&  marshal( interface) ppProcess) runtime managed internalcall
  {
  } // end of method ICorDebug::DebugActiveProcess

  .method public hidebysig newslot abstract virtual 
          instance void  EnumerateProcesses([out] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugProcessEnum&  marshal( interface) ppProcess) runtime managed internalcall
  {
  } // end of method ICorDebug::EnumerateProcesses

  .method public hidebysig newslot abstract virtual 
          instance void  GetProcess([in] uint32 dwProcessId,
                                    [out] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugProcess&  marshal( interface) ppProcess) runtime managed internalcall
  {
  } // end of method ICorDebug::GetProcess

  .method public hidebysig newslot abstract virtual 
          instance void  CanLaunchOrAttach([in] uint32 dwProcessId,
                                           [in] int32 win32DebuggingEnabled) runtime managed internalcall
  {
  } // end of method ICorDebug::CanLaunchOrAttach

} // end of class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebug

.class interface public abstract auto ansi import Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugManagedCallback
{
  .custom instance void [mscorlib]System.Runtime.InteropServices.InterfaceTypeAttribute::.ctor(int16) = ( 01 00 01 00 00 00 ) 
  .custom instance void [mscorlib]System.Runtime.InteropServices.GuidAttribute::.ctor(string) = ( 01 00 24 33 44 36 46 35 46 36 30 2D 37 35 33 38   // ..$3D6F5F60-7538
                                                                                                  2D 31 31 44 33 2D 38 44 35 42 2D 30 30 31 30 34   // -11D3-8D5B-00104
                                                                                                  42 33 35 45 37 45 46 00 00 )                      // B35E7EF..
  .method public hidebysig newslot abstract virtual 
          instance void  Breakpoint([in] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugAppDomain  marshal( interface) pAppDomain,
                                    [in] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugThread  marshal( interface) pThread,
                                    [in] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugBreakpoint  marshal( interface) pBreakpoint) runtime managed internalcall
  {
  } // end of method ICorDebugManagedCallback::Breakpoint

  .method public hidebysig newslot abstract virtual 
          instance void  StepComplete([in] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugAppDomain  marshal( interface) pAppDomain,
                                      [in] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugThread  marshal( interface) pThread,
                                      [in] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugStepper  marshal( interface) pStepper,
                                      [in] valuetype Microsoft.Samples.Debugging.CorDebug.NativeApi.CorDebugStepReason reason) runtime managed internalcall
  {
  } // end of method ICorDebugManagedCallback::StepComplete

  .method public hidebysig newslot abstract virtual 
          instance void  Break([in] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugAppDomain  marshal( interface) pAppDomain,
                               [in] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugThread  marshal( interface) thread) runtime managed internalcall
  {
  } // end of method ICorDebugManagedCallback::Break

  .method public hidebysig newslot abstract virtual 
          instance void  Exception([in] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugAppDomain  marshal( interface) pAppDomain,
                                   [in] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugThread  marshal( interface) pThread,
                                   [in] int32 unhandled) runtime managed internalcall
  {
  } // end of method ICorDebugManagedCallback::Exception

  .method public hidebysig newslot abstract virtual 
          instance void  EvalComplete([in] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugAppDomain  marshal( interface) pAppDomain,
                                      [in] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugThread  marshal( interface) pThread,
                                      [in] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugEval  marshal( interface) pEval) runtime managed internalcall
  {
  } // end of method ICorDebugManagedCallback::EvalComplete

  .method public hidebysig newslot abstract virtual 
          instance void  EvalException([in] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugAppDomain  marshal( interface) pAppDomain,
                                       [in] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugThread  marshal( interface) pThread,
                                       [in] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugEval  marshal( interface) pEval) runtime managed internalcall
  {
  } // end of method ICorDebugManagedCallback::EvalException

  .method public hidebysig newslot abstract virtual 
          instance void  CreateProcess([in] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugProcess  marshal( interface) pProcess) runtime managed internalcall
  {
  } // end of method ICorDebugManagedCallback::CreateProcess

  .method public hidebysig newslot abstract virtual 
          instance void  ExitProcess([in] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugProcess  marshal( interface) pProcess) runtime managed internalcall
  {
  } // end of method ICorDebugManagedCallback::ExitProcess

  .method public hidebysig newslot abstract virtual 
          instance void  CreateThread([in] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugAppDomain  marshal( interface) pAppDomain,
                                      [in] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugThread  marshal( interface) thread) runtime managed internalcall
  {
  } // end of method ICorDebugManagedCallback::CreateThread

  .method public hidebysig newslot abstract virtual 
          instance void  ExitThread([in] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugAppDomain  marshal( interface) pAppDomain,
                                    [in] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugThread  marshal( interface) thread) runtime managed internalcall
  {
  } // end of method ICorDebugManagedCallback::ExitThread

  .method public hidebysig newslot abstract virtual 
          instance void  LoadModule([in] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugAppDomain  marshal( interface) pAppDomain,
                                    [in] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugModule  marshal( interface) pModule) runtime managed internalcall
  {
  } // end of method ICorDebugManagedCallback::LoadModule

  .method public hidebysig newslot abstract virtual 
          instance void  UnloadModule([in] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugAppDomain  marshal( interface) pAppDomain,
                                      [in] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugModule  marshal( interface) pModule) runtime managed internalcall
  {
  } // end of method ICorDebugManagedCallback::UnloadModule

  .method public hidebysig newslot abstract virtual 
          instance void  LoadClass([in] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugAppDomain  marshal( interface) pAppDomain,
                                   [in] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugClass  marshal( interface) c) runtime managed internalcall
  {
  } // end of method ICorDebugManagedCallback::LoadClass

  .method public hidebysig newslot abstract virtual 
          instance void  UnloadClass([in] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugAppDomain  marshal( interface) pAppDomain,
                                     [in] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugClass  marshal( interface) c) runtime managed internalcall
  {
  } // end of method ICorDebugManagedCallback::UnloadClass

  .method public hidebysig newslot abstract virtual 
          instance void  DebuggerError([in] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugProcess  marshal( interface) pProcess,
                                       [in] int32  marshal( error) errorHR,
                                       [in] uint32 errorCode) runtime managed internalcall
  {
  } // end of method ICorDebugManagedCallback::DebuggerError

  .method public hidebysig newslot abstract virtual 
          instance void  LogMessage([in] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugAppDomain  marshal( interface) pAppDomain,
                                    [in] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugThread  marshal( interface) pThread,
                                    [in] int32 lLevel,
                                    [in] string  marshal( lpwstr) pLogSwitchName,
                                    [in] string  marshal( lpwstr) pMessage) runtime managed internalcall
  {
  } // end of method ICorDebugManagedCallback::LogMessage

  .method public hidebysig newslot abstract virtual 
          instance void  LogSwitch([in] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugAppDomain  marshal( interface) pAppDomain,
                                   [in] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugThread  marshal( interface) pThread,
                                   [in] int32 lLevel,
                                   [in] uint32 ulReason,
                                   [in] string  marshal( lpwstr)  pLogSwitchName,
                                   [in] string  marshal( lpwstr)  pParentName) runtime managed internalcall
  {
  } // end of method ICorDebugManagedCallback::LogSwitch

  .method public hidebysig newslot abstract virtual 
          instance void  CreateAppDomain([in] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugProcess  marshal( interface) pProcess,
                                         [in] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugAppDomain  marshal( interface) pAppDomain) runtime managed internalcall
  {
  } // end of method ICorDebugManagedCallback::CreateAppDomain

  .method public hidebysig newslot abstract virtual 
          instance void  ExitAppDomain([in] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugProcess  marshal( interface) pProcess,
                                       [in] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugAppDomain  marshal( interface) pAppDomain) runtime managed internalcall
  {
  } // end of method ICorDebugManagedCallback::ExitAppDomain

  .method public hidebysig newslot abstract virtual 
          instance void  LoadAssembly([in] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugAppDomain  marshal( interface) pAppDomain,
                                      [in] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugAssembly  marshal( interface) pAssembly) runtime managed internalcall
  {
  } // end of method ICorDebugManagedCallback::LoadAssembly

  .method public hidebysig newslot abstract virtual 
          instance void  UnloadAssembly([in] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugAppDomain  marshal( interface) pAppDomain,
                                        [in] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugAssembly  marshal( interface) pAssembly) runtime managed internalcall
  {
  } // end of method ICorDebugManagedCallback::UnloadAssembly

  .method public hidebysig newslot abstract virtual 
          instance void  ControlCTrap([in] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugProcess  marshal( interface) pProcess) runtime managed internalcall
  {
  } // end of method ICorDebugManagedCallback::ControlCTrap

  .method public hidebysig newslot abstract virtual 
          instance void  NameChange([in] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugAppDomain  marshal( interface) pAppDomain,
                                    [in] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugThread  marshal( interface) pThread) runtime managed internalcall
  {
  } // end of method ICorDebugManagedCallback::NameChange

  .method public hidebysig newslot abstract virtual 
          instance void  UpdateModuleSymbols([in] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugAppDomain  marshal( interface) pAppDomain,
                                             [in] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugModule  marshal( interface) pModule,
                                             [in] class [mscorlib]System.Runtime.InteropServices.ComTypes.IStream  marshal( interface) pSymbolStream) runtime managed internalcall
  {
  } // end of method ICorDebugManagedCallback::UpdateModuleSymbols

  .method public hidebysig newslot abstract virtual 
          instance void  EditAndContinueRemap([in] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugAppDomain  marshal( interface) pAppDomain,
                                              [in] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugThread  marshal( interface) pThread,
                                              [in] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugFunction  marshal( interface) pFunction,
                                              [in] int32 fAccurate) runtime managed internalcall
  {
  } // end of method ICorDebugManagedCallback::EditAndContinueRemap

  .method public hidebysig newslot abstract virtual 
          instance void  BreakpointSetError([in] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugAppDomain  marshal( interface) pAppDomain,
                                            [in] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugThread  marshal( interface) pThread,
                                            [in] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugBreakpoint  marshal( interface) pBreakpoint,
                                            [in] uint32 dwError) runtime managed internalcall
  {
  } // end of method ICorDebugManagedCallback::BreakpointSetError

} // end of class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugManagedCallback

.class interface public abstract auto ansi import Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugAppDomain
       implements Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugController
{
  .custom instance void [mscorlib]System.Runtime.InteropServices.InterfaceTypeAttribute::.ctor(int16) = ( 01 00 01 00 00 00 ) 
  .custom instance void [mscorlib]System.Runtime.InteropServices.ComConversionLossAttribute::.ctor() = ( 01 00 00 00 ) 
  .custom instance void [mscorlib]System.Runtime.InteropServices.GuidAttribute::.ctor(string) = ( 01 00 24 33 44 36 46 35 46 36 33 2D 37 35 33 38   // ..$3D6F5F63-7538
                                                                                                  2D 31 31 44 33 2D 38 44 35 42 2D 30 30 31 30 34   // -11D3-8D5B-00104
                                                                                                  42 33 35 45 37 45 46 00 00 )                      // B35E7EF..
  .method public hidebysig newslot abstract virtual 
          instance void  Stop([in] uint32 dwTimeout) runtime managed internalcall
  {
  } // end of method ICorDebugAppDomain::Stop

  .method public hidebysig newslot abstract virtual 
          instance void  Continue([in] int32 fIsOutOfBand) runtime managed internalcall
  {
  } // end of method ICorDebugAppDomain::Continue

  .method public hidebysig newslot abstract virtual 
          instance void  IsRunning([out] int32& pbRunning) runtime managed internalcall
  {
  } // end of method ICorDebugAppDomain::IsRunning

  .method public hidebysig newslot abstract virtual 
          instance void  HasQueuedCallbacks([in] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugThread  marshal( interface) pThread,
                                            [out] int32& pbQueued) runtime managed internalcall
  {
  } // end of method ICorDebugAppDomain::HasQueuedCallbacks

  .method public hidebysig newslot abstract virtual 
          instance void  EnumerateThreads([out] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugThreadEnum&  marshal( interface) ppThreads) runtime managed internalcall
  {
  } // end of method ICorDebugAppDomain::EnumerateThreads

  .method public hidebysig newslot abstract virtual 
          instance void  SetAllThreadsDebugState([in] valuetype Microsoft.Samples.Debugging.CorDebug.NativeApi.CorDebugThreadState state,
                                                 [in] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugThread  marshal( interface) pExceptThisThread) runtime managed internalcall
  {
  } // end of method ICorDebugAppDomain::SetAllThreadsDebugState

  .method public hidebysig newslot abstract virtual 
          instance void  Detach() runtime managed internalcall
  {
  } // end of method ICorDebugAppDomain::Detach

  .method public hidebysig newslot abstract virtual 
          instance void  Terminate([in] uint32 exitCode) runtime managed internalcall
  {
  } // end of method ICorDebugAppDomain::Terminate

  .method public hidebysig newslot abstract virtual 
          instance void  CanCommitChanges([in] uint32 cSnapshots,
                                          [in] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugEditAndContinueSnapshot&  marshal( interface) pSnapshots,
                                          [out] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugErrorInfoEnum&  marshal( interface) pError) runtime managed internalcall
  {
  } // end of method ICorDebugAppDomain::CanCommitChanges

  .method public hidebysig newslot abstract virtual 
          instance void  CommitChanges([in] uint32 cSnapshots,
                                       [in] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugEditAndContinueSnapshot&  marshal( interface) pSnapshots,
                                       [out] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugErrorInfoEnum&  marshal( interface) pError) runtime managed internalcall
  {
  } // end of method ICorDebugAppDomain::CommitChanges

  .method public hidebysig newslot abstract virtual 
          instance void  GetProcess([out] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugProcess&  marshal( interface) ppProcess) runtime managed internalcall
  {
  } // end of method ICorDebugAppDomain::GetProcess

  .method public hidebysig newslot abstract virtual 
          instance void  EnumerateAssemblies([out] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugAssemblyEnum&  marshal( interface) ppAssemblies) runtime managed internalcall
  {
  } // end of method ICorDebugAppDomain::EnumerateAssemblies

  .method public hidebysig newslot abstract virtual 
          instance void  GetModuleFromMetaDataInterface([in] object  marshal( iunknown) pIMetaData,
                                                        [out] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugModule&  marshal( interface) ppModule) runtime managed internalcall
  {
  } // end of method ICorDebugAppDomain::GetModuleFromMetaDataInterface

  .method public hidebysig newslot abstract virtual 
          instance void  EnumerateBreakpoints([out] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugBreakpointEnum&  marshal( interface) ppBreakpoints) runtime managed internalcall
  {
  } // end of method ICorDebugAppDomain::EnumerateBreakpoints

  .method public hidebysig newslot abstract virtual 
          instance void  EnumerateSteppers([out] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugStepperEnum&  marshal( interface) ppSteppers) runtime managed internalcall
  {
  } // end of method ICorDebugAppDomain::EnumerateSteppers

  .method public hidebysig newslot abstract virtual 
          instance void  IsAttached([out] int32& pbAttached) runtime managed internalcall
  {
  } // end of method ICorDebugAppDomain::IsAttached

  .method public hidebysig newslot abstract virtual 
          instance void  GetName([in] uint32 cchName,
                                 [out] uint32& pcchName,
								 [out] class [mscorlib]System.Text.StringBuilder  marshal( lpwstr) szName
                                 ) runtime managed internalcall
  {
  } // end of method ICorDebugAppDomain::GetName

  .method public hidebysig newslot abstract virtual 
          instance void  GetObject([out] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugValue&  marshal( interface) ppObject) runtime managed internalcall
  {
  } // end of method ICorDebugAppDomain::GetObject

  .method public hidebysig newslot abstract virtual 
          instance void  Attach() runtime managed internalcall
  {
  } // end of method ICorDebugAppDomain::Attach

  .method public hidebysig newslot abstract virtual 
          instance void  GetID([out] uint32& pId) runtime managed internalcall
  {
  } // end of method ICorDebugAppDomain::GetID

} // end of class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugAppDomain

.class interface public abstract auto ansi import Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugController
{
  .custom instance void [mscorlib]System.Runtime.InteropServices.InterfaceTypeAttribute::.ctor(int16) = ( 01 00 01 00 00 00 ) 
  .custom instance void [mscorlib]System.Runtime.InteropServices.GuidAttribute::.ctor(string) = ( 01 00 24 33 44 36 46 35 46 36 32 2D 37 35 33 38   // ..$3D6F5F62-7538
                                                                                                  2D 31 31 44 33 2D 38 44 35 42 2D 30 30 31 30 34   // -11D3-8D5B-00104
                                                                                                  42 33 35 45 37 45 46 00 00 )                      // B35E7EF..
  .method public hidebysig newslot abstract virtual 
          instance void  Stop([in] uint32 dwTimeout) runtime managed internalcall
  {
  } // end of method ICorDebugController::Stop

  .method public hidebysig newslot abstract virtual 
          instance void  Continue([in] int32 fIsOutOfBand) runtime managed internalcall
  {
  } // end of method ICorDebugController::Continue

  .method public hidebysig newslot abstract virtual 
          instance void  IsRunning([out] int32& pbRunning) runtime managed internalcall
  {
  } // end of method ICorDebugController::IsRunning

  .method public hidebysig newslot abstract virtual 
          instance void  HasQueuedCallbacks([in] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugThread  marshal( interface) pThread,
                                            [out] int32& pbQueued) runtime managed internalcall
  {
  } // end of method ICorDebugController::HasQueuedCallbacks

  .method public hidebysig newslot abstract virtual 
          instance void  EnumerateThreads([out] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugThreadEnum&  marshal( interface) ppThreads) runtime managed internalcall
  {
  } // end of method ICorDebugController::EnumerateThreads

  .method public hidebysig newslot abstract virtual 
          instance void  SetAllThreadsDebugState([in] valuetype Microsoft.Samples.Debugging.CorDebug.NativeApi.CorDebugThreadState state,
                                                 [in] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugThread  marshal( interface) pExceptThisThread) runtime managed internalcall
  {
  } // end of method ICorDebugController::SetAllThreadsDebugState

  .method public hidebysig newslot abstract virtual 
          instance void  Detach() runtime managed internalcall
  {
  } // end of method ICorDebugController::Detach

  .method public hidebysig newslot abstract virtual 
          instance void  Terminate([in] uint32 exitCode) runtime managed internalcall
  {
  } // end of method ICorDebugController::Terminate

  .method public hidebysig newslot abstract virtual 
          instance void  CanCommitChanges([in] uint32 cSnapshots,
                                          [in] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugEditAndContinueSnapshot&  marshal( interface) pSnapshots,
                                          [out] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugErrorInfoEnum&  marshal( interface) pError) runtime managed internalcall
  {
  } // end of method ICorDebugController::CanCommitChanges

  .method public hidebysig newslot abstract virtual 
          instance void  CommitChanges([in] uint32 cSnapshots,
                                       [in] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugEditAndContinueSnapshot&  marshal( interface) pSnapshots,
                                       [out] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugErrorInfoEnum&  marshal( interface) pError) runtime managed internalcall
  {
  } // end of method ICorDebugController::CommitChanges

} // end of class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugController

.namespace Microsoft.Samples.Debugging.CorDebug.NativeApi
{
 .class public sequential ansi sealed beforefieldinit _CodeChunkInfo
      extends [mscorlib]System.ValueType
  {
    .pack 8
    .size 0
    .field public uint64 startAddr
    .field public uint32 length
  } // end of class _CodeChunkInfo

 .class public sequential ansi sealed beforefieldinit COR_ACTIVE_FUNCTION
         extends [mscorlib]System.ValueType
  {
    .pack 4
    .size 0
    .field public class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugAppDomain pAppDomain
    .field public class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugModule pModule
    .field public class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugFunction2 pFunction
	.field public uint32 ilOffset
	.field public uint32 Flags
  } // end of class 'COR_ACTIVE_FUNCTION'

 .class public sequential ansi sealed beforefieldinit COR_DEBUG_IL_TO_NATIVE_MAP
         extends [mscorlib]System.ValueType
  {
    .pack 4
    .size 0
	.field public uint32 ilOffset
    .field public uint32 nativeStartOffset
	.field public uint32 nativeEndOffset
  } // end of class 'COR_DEBUG_IL_TO_NATIVE_MAP'
}
.class interface public abstract auto ansi import Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugThread
{
  .custom instance void [mscorlib]System.Runtime.InteropServices.GuidAttribute::.ctor(string) = ( 01 00 24 39 33 38 43 36 44 36 36 2D 37 46 42 36   // ..$938C6D66-7FB6
                                                                                                  2D 34 46 36 39 2D 42 33 38 39 2D 34 32 35 42 38   // -4F69-B389-425B8
                                                                                                  39 38 37 33 32 39 42 00 00 )                      // 987329B..
  .custom instance void [mscorlib]System.Runtime.InteropServices.InterfaceTypeAttribute::.ctor(int16) = ( 01 00 01 00 00 00 ) 
  .method public hidebysig newslot abstract virtual 
          instance void  GetProcess([out] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugProcess&  marshal( interface) ppProcess) runtime managed internalcall
  {
  } // end of method ICorDebugThread::GetProcess

  .method public hidebysig newslot abstract virtual 
          instance void  GetID([out] uint32& pdwThreadId) runtime managed internalcall
  {
  } // end of method ICorDebugThread::GetID

  .method public hidebysig newslot abstract virtual 
          instance void  GetHandle([out] native int& phThreadHandle) runtime managed internalcall
  {
  } // end of method ICorDebugThread::GetHandle

  .method public hidebysig newslot abstract virtual 
          instance void  GetAppDomain([out] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugAppDomain&  marshal( interface) ppAppDomain) runtime managed internalcall
  {
  } // end of method ICorDebugThread::GetAppDomain

  .method public hidebysig newslot abstract virtual 
          instance void  SetDebugState([in] valuetype Microsoft.Samples.Debugging.CorDebug.NativeApi.CorDebugThreadState state) runtime managed internalcall
  {
  } // end of method ICorDebugThread::SetDebugState

  .method public hidebysig newslot abstract virtual 
          instance void  GetDebugState([out] valuetype Microsoft.Samples.Debugging.CorDebug.NativeApi.CorDebugThreadState& pState) runtime managed internalcall
  {
  } // end of method ICorDebugThread::GetDebugState

  .method public hidebysig newslot abstract virtual 
          instance void  GetUserState([out] valuetype Microsoft.Samples.Debugging.CorDebug.NativeApi.CorDebugUserState& pState) runtime managed internalcall
  {
  } // end of method ICorDebugThread::GetUserState

  .method public hidebysig newslot abstract virtual 
          instance void  GetCurrentException([out] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugValue&  marshal( interface) ppExceptionObject) runtime managed internalcall
  {
  } // end of method ICorDebugThread::GetCurrentException

  .method public hidebysig newslot abstract virtual 
          instance void  ClearCurrentException() runtime managed internalcall
  {
  } // end of method ICorDebugThread::ClearCurrentException

  .method public hidebysig newslot abstract virtual 
          instance void  CreateStepper([out] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugStepper&  marshal( interface) ppStepper) runtime managed internalcall
  {
  } // end of method ICorDebugThread::CreateStepper

  .method public hidebysig newslot abstract virtual 
          instance void  EnumerateChains([out] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugChainEnum&  marshal( interface) ppChains) runtime managed internalcall
  {
  } // end of method ICorDebugThread::EnumerateChains

  .method public hidebysig newslot abstract virtual 
          instance void  GetActiveChain([out] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugChain&  marshal( interface) ppChain) runtime managed internalcall
  {
  } // end of method ICorDebugThread::GetActiveChain

  .method public hidebysig newslot abstract virtual 
          instance void  GetActiveFrame([out] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugFrame&  marshal( interface) ppFrame) runtime managed internalcall
  {
  } // end of method ICorDebugThread::GetActiveFrame

  .method public hidebysig newslot abstract virtual 
          instance void  GetRegisterSet([out] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugRegisterSet&  marshal( interface) ppRegisters) runtime managed internalcall
  {
  } // end of method ICorDebugThread::GetRegisterSet

  .method public hidebysig newslot abstract virtual 
          instance void  CreateEval([out] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugEval&  marshal( interface) ppEval) runtime managed internalcall
  {
  } // end of method ICorDebugThread::CreateEval

  .method public hidebysig newslot abstract virtual 
          instance void  GetObject([out] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugValue&  marshal( interface) ppObject) runtime managed internalcall
  {
  } // end of method ICorDebugThread::GetObject

} // end of class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugThread

.class interface public abstract auto ansi import Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugProcess
       implements Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugController
{
  .custom instance void [mscorlib]System.Runtime.InteropServices.GuidAttribute::.ctor(string) = ( 01 00 24 33 44 36 46 35 46 36 34 2D 37 35 33 38   // ..$3D6F5F64-7538
                                                                                                  2D 31 31 44 33 2D 38 44 35 42 2D 30 30 31 30 34   // -11D3-8D5B-00104
                                                                                                  42 33 35 45 37 45 46 00 00 )                      // B35E7EF..
  .custom instance void [mscorlib]System.Runtime.InteropServices.InterfaceTypeAttribute::.ctor(int16) = ( 01 00 01 00 00 00 ) 
  .custom instance void [mscorlib]System.Runtime.InteropServices.ComConversionLossAttribute::.ctor() = ( 01 00 00 00 ) 
  .method public hidebysig newslot abstract virtual 
          instance void  Stop([in] uint32 dwTimeout) runtime managed internalcall
  {
  } // end of method ICorDebugProcess::Stop

  .method public hidebysig newslot abstract virtual 
          instance void  Continue([in] int32 fIsOutOfBand) runtime managed internalcall
  {
  } // end of method ICorDebugProcess::Continue

  .method public hidebysig newslot abstract virtual 
          instance void  IsRunning([out] int32& pbRunning) runtime managed internalcall
  {
  } // end of method ICorDebugProcess::IsRunning

  .method public hidebysig newslot abstract virtual 
          instance void  HasQueuedCallbacks([in] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugThread  marshal( interface) pThread,
                                            [out] int32& pbQueued) runtime managed internalcall
  {
  } // end of method ICorDebugProcess::HasQueuedCallbacks

  .method public hidebysig newslot abstract virtual 
          instance void  EnumerateThreads([out] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugThreadEnum&  marshal( interface) ppThreads) runtime managed internalcall
  {
  } // end of method ICorDebugProcess::EnumerateThreads

  .method public hidebysig newslot abstract virtual 
          instance void  SetAllThreadsDebugState([in] valuetype Microsoft.Samples.Debugging.CorDebug.NativeApi.CorDebugThreadState state,
                                                 [in] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugThread  marshal( interface) pExceptThisThread) runtime managed internalcall
  {
  } // end of method ICorDebugProcess::SetAllThreadsDebugState

  .method public hidebysig newslot abstract virtual 
          instance void  Detach() runtime managed internalcall
  {
  } // end of method ICorDebugProcess::Detach

  .method public hidebysig newslot abstract virtual 
          instance void  Terminate([in] uint32 exitCode) runtime managed internalcall
  {
  } // end of method ICorDebugProcess::Terminate

  .method public hidebysig newslot abstract virtual 
          instance void  CanCommitChanges([in] uint32 cSnapshots,
                                          [in] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugEditAndContinueSnapshot&  marshal( interface) pSnapshots,
                                          [out] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugErrorInfoEnum&  marshal( interface) pError) runtime managed internalcall
  {
  } // end of method ICorDebugProcess::CanCommitChanges

  .method public hidebysig newslot abstract virtual 
          instance void  CommitChanges([in] uint32 cSnapshots,
                                       [in] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugEditAndContinueSnapshot&  marshal( interface) pSnapshots,
                                       [out] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugErrorInfoEnum&  marshal( interface) pError) runtime managed internalcall
  {
  } // end of method ICorDebugProcess::CommitChanges

  .method public hidebysig newslot abstract virtual 
          instance void  GetID([out] uint32& pdwProcessId) runtime managed internalcall
  {
  } // end of method ICorDebugProcess::GetID

  .method public hidebysig newslot abstract virtual 
          instance void  GetHandle([out] native int& phProcessHandle) runtime managed internalcall
  {
    .param [1]
    .custom instance void [mscorlib]System.Runtime.InteropServices.ComAliasNameAttribute::.ctor(string) = ( 01 00 09 48 50 52 4F 43 45 53 53 2A 00 00 )          // ...HPROCESS*..  
  } // end of method ICorDebugProcess::GetHandle

  .method public hidebysig newslot abstract virtual 
          instance void  GetThread([in] uint32 dwThreadId,
                                   [out] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugThread&  marshal( interface) ppThread) runtime managed internalcall
  {
  } // end of method ICorDebugProcess::GetThread

  .method public hidebysig newslot abstract virtual 
          instance void  EnumerateObjects([out] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugObjectEnum&  marshal( interface) ppObjects) runtime managed internalcall
  {
  } // end of method ICorDebugProcess::EnumerateObjects

  .method public hidebysig newslot abstract virtual 
          instance void  IsTransitionStub([in] uint64 address,
                                          [out] int32& pbTransitionStub) runtime managed internalcall
  {
  } // end of method ICorDebugProcess::IsTransitionStub

  .method public hidebysig newslot abstract virtual 
          instance void  IsOSSuspended([in] uint32 threadID,
                                       [out] int32& pbSuspended) runtime managed internalcall
  {
  } // end of method ICorDebugProcess::IsOSSuspended

  .method public hidebysig newslot abstract virtual 
          instance void  GetThreadContext([in] uint32 threadID,
                                          [in] uint32 contextSize,
                                          [in] native int context) runtime managed internalcall
  {
    .param [3]
    .custom instance void [mscorlib]System.Runtime.InteropServices.ComAliasNameAttribute::.ctor(string) = ( 01 00 05 42 59 54 45 2A 00 00 )                   // ...BYTE*..
  } // end of method ICorDebugProcess::GetThreadContext

  .method public hidebysig newslot abstract virtual 
          instance void  SetThreadContext([in] uint32 threadID,
                                          [in] uint32 contextSize,
                                          [in] native int context) runtime managed internalcall
  {
    .param [3]
    .custom instance void [mscorlib]System.Runtime.InteropServices.ComAliasNameAttribute::.ctor(string) = ( 01 00 05 42 59 54 45 2A 00 00 )                   // ...BYTE*..
  } // end of method ICorDebugProcess::SetThreadContext

  .method public hidebysig newslot abstract virtual 
          instance void  ReadMemory([in] uint64 address,
                                    [in] uint32 size,
                                    [out] unsigned int8[] marshal([]) buffer,
                                    [out] uint32& read) runtime managed internalcall
  {
    .param [4]
    .custom instance void [mscorlib]System.Runtime.InteropServices.ComAliasNameAttribute::.ctor(string) = ( 01 00 07 53 49 5A 45 5F 54 2A 00 00 )             // ...SIZE_T*..
  } // end of method ICorDebugProcess::ReadMemory

  .method public hidebysig newslot abstract virtual 
          instance void  WriteMemory([in] uint64 address,
                                     [in] uint32 size,
                                     [in] unsigned int8[] marshal([]) buffer,
                                     [out] uint32& written) runtime managed internalcall
  {
    .param [4]
    .custom instance void [mscorlib]System.Runtime.InteropServices.ComAliasNameAttribute::.ctor(string) = ( 01 00 07 53 49 5A 45 5F 54 2A 00 00 )             // ...SIZE_T*..
  } // end of method ICorDebugProcess::WriteMemory

  .method public hidebysig newslot abstract virtual 
          instance void  ClearCurrentException([in] uint32 threadID) runtime managed internalcall
  {
  } // end of method ICorDebugProcess::ClearCurrentException

  .method public hidebysig newslot abstract virtual 
          instance void  EnableLogMessages([in] int32 fOnOff) runtime managed internalcall
  {
  } // end of method ICorDebugProcess::EnableLogMessages

  .method public hidebysig newslot abstract virtual 
          instance void  ModifyLogSwitch([in] string  marshal( lpwstr)  pLogSwitchName,
                                         [in] int32 lLevel) runtime managed internalcall
  {
  } // end of method ICorDebugProcess::ModifyLogSwitch

  .method public hidebysig newslot abstract virtual 
          instance void  EnumerateAppDomains([out] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugAppDomainEnum&  marshal( interface) ppAppDomains) runtime managed internalcall
  {
  } // end of method ICorDebugProcess::EnumerateAppDomains

  .method public hidebysig newslot abstract virtual 
          instance void  GetObject([out] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugValue&  marshal( interface) ppObject) runtime managed internalcall
  {
  } // end of method ICorDebugProcess::GetObject

  .method public hidebysig newslot abstract virtual 
          instance void  ThreadForFiberCookie([in] uint32 fiberCookie,
                                              [out] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugThread&  marshal( interface) ppThread) runtime managed internalcall
  {
  } // end of method ICorDebugProcess::ThreadForFiberCookie

  .method public hidebysig newslot abstract virtual 
          instance void  GetHelperThreadID([out] uint32& pThreadID) runtime managed internalcall
  {
  } // end of method ICorDebugProcess::GetHelperThreadID

} // end of class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugProcess

.class interface public abstract auto ansi import Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugObjectEnum
       implements Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugEnum
{
  .custom instance void [mscorlib]System.Runtime.InteropServices.GuidAttribute::.ctor(string) = ( 01 00 24 43 43 37 42 43 42 30 32 2D 38 41 36 38   // ..$CC7BCB02-8A68
                                                                                                  2D 31 31 44 32 2D 39 38 33 43 2D 30 30 30 30 46   // -11D2-983C-0000F
                                                                                                  38 30 38 33 34 32 44 00 00 )                      // 808342D..
  .custom instance void [mscorlib]System.Runtime.InteropServices.InterfaceTypeAttribute::.ctor(int16) = ( 01 00 01 00 00 00 ) 
  .custom instance void [mscorlib]System.Runtime.InteropServices.ComConversionLossAttribute::.ctor() = ( 01 00 00 00 ) 
  .method public hidebysig newslot abstract virtual 
          instance void  Skip([in] uint32 celt) runtime managed internalcall
  {
  } // end of method ICorDebugObjectEnum::Skip

  .method public hidebysig newslot abstract virtual 
          instance void  Reset() runtime managed internalcall
  {
  } // end of method ICorDebugObjectEnum::Reset

  .method public hidebysig newslot abstract virtual 
          instance void  Clone([out] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugEnum&  marshal( interface) ppEnum) runtime managed internalcall
  {
  } // end of method ICorDebugObjectEnum::Clone

  .method public hidebysig newslot abstract virtual 
          instance void  GetCount([out] uint32& pcelt) runtime managed internalcall
  {
  } // end of method ICorDebugObjectEnum::GetCount

  .method public hidebysig newslot abstract virtual 
          instance int32  Next([in] uint32 celt,
                              [out] unsigned int64[] marshal([ ]) objects,
                              [out] uint32& pceltFetched) runtime managed internalcall preservesig
  {
  } // end of method ICorDebugObjectEnum::Next

} // end of class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugObjectEnum

.class interface public abstract auto ansi import Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugEnum
{
  .custom instance void [mscorlib]System.Runtime.InteropServices.GuidAttribute::.ctor(string) = ( 01 00 24 43 43 37 42 43 42 30 31 2D 38 41 36 38   // ..$CC7BCB01-8A68
                                                                                                  2D 31 31 44 32 2D 39 38 33 43 2D 30 30 30 30 46   // -11D2-983C-0000F
                                                                                                  38 30 38 33 34 32 44 00 00 )                      // 808342D..
  .custom instance void [mscorlib]System.Runtime.InteropServices.InterfaceTypeAttribute::.ctor(int16) = ( 01 00 01 00 00 00 ) 
  .method public hidebysig newslot abstract virtual 
          instance void  Skip([in] uint32 celt) runtime managed internalcall
  {
  } // end of method ICorDebugEnum::Skip

  .method public hidebysig newslot abstract virtual 
          instance void  Reset() runtime managed internalcall
  {
  } // end of method ICorDebugEnum::Reset

  .method public hidebysig newslot abstract virtual 
          instance void  Clone([out] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugEnum&  marshal( interface) ppEnum) runtime managed internalcall
  {
  } // end of method ICorDebugEnum::Clone

  .method public hidebysig newslot abstract virtual 
          instance void  GetCount([out] uint32& pcelt) runtime managed internalcall
  {
  } // end of method ICorDebugEnum::GetCount

} // end of class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugEnum

.class interface public abstract auto ansi import Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugAppDomainEnum
       implements Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugEnum
{
  .custom instance void [mscorlib]System.Runtime.InteropServices.InterfaceTypeAttribute::.ctor(int16) = ( 01 00 01 00 00 00 ) 
  .custom instance void [mscorlib]System.Runtime.InteropServices.ComConversionLossAttribute::.ctor() = ( 01 00 00 00 ) 
  .custom instance void [mscorlib]System.Runtime.InteropServices.GuidAttribute::.ctor(string) = ( 01 00 24 36 33 43 41 31 42 32 34 2D 34 33 35 39   // ..$63CA1B24-4359
                                                                                                  2D 34 38 38 33 2D 42 44 35 37 2D 31 33 46 38 31   // -4883-BD57-13F81
                                                                                                  35 46 35 38 37 34 34 00 00 )                      // 5F58744..
  .method public hidebysig newslot abstract virtual 
          instance void  Skip([in] uint32 celt) runtime managed internalcall
  {
  } // end of method ICorDebugAppDomainEnum::Skip

  .method public hidebysig newslot abstract virtual 
          instance void  Reset() runtime managed internalcall
  {
  } // end of method ICorDebugAppDomainEnum::Reset

  .method public hidebysig newslot abstract virtual 
          instance void  Clone([out] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugEnum&  marshal( interface) ppEnum) runtime managed internalcall
  {
  } // end of method ICorDebugAppDomainEnum::Clone

  .method public hidebysig newslot abstract virtual 
          instance void  GetCount([out] uint32& pcelt) runtime managed internalcall
  {
  } // end of method ICorDebugAppDomainEnum::GetCount

  .method public hidebysig newslot abstract virtual 
          instance int32  Next([in] uint32 celt,
                              [out] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugAppDomain[] marshal([ ]) values,
                              [out] uint32& pceltFetched) runtime managed internalcall preservesig
  {
  } // end of method ICorDebugAppDomainEnum::Next

} // end of class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugAppDomainEnum

.class interface public abstract auto ansi import Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugValue
{
  .custom instance void [mscorlib]System.Runtime.InteropServices.InterfaceTypeAttribute::.ctor(int16) = ( 01 00 01 00 00 00 ) 
  .custom instance void [mscorlib]System.Runtime.InteropServices.GuidAttribute::.ctor(string) = ( 01 00 24 43 43 37 42 43 41 46 37 2D 38 41 36 38   // ..$CC7BCAF7-8A68
                                                                                                  2D 31 31 44 32 2D 39 38 33 43 2D 30 30 30 30 46   // -11D2-983C-0000F
                                                                                                  38 30 38 33 34 32 44 00 00 )                      // 808342D..
  .method public hidebysig newslot abstract virtual 
          instance void  GetType([out] valuetype Microsoft.Samples.Debugging.CorDebug.NativeApi.CorElementType& pType) runtime managed internalcall
  {
  } // end of method ICorDebugValue::GetType

  .method public hidebysig newslot abstract virtual 
          instance void  GetSize([out] uint32& pSize) runtime managed internalcall
  {
  } // end of method ICorDebugValue::GetSize

  .method public hidebysig newslot abstract virtual 
          instance void  GetAddress([out] uint64& pAddress) runtime managed internalcall
  {
  } // end of method ICorDebugValue::GetAddress

  .method public hidebysig newslot abstract virtual 
          instance void  CreateBreakpoint([out] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugValueBreakpoint&  marshal( interface) ppBreakpoint) runtime managed internalcall
  {
  } // end of method ICorDebugValue::CreateBreakpoint

} // end of class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugValue

.class interface public abstract auto ansi import Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugValueBreakpoint
       implements Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugBreakpoint
{
  .custom instance void [mscorlib]System.Runtime.InteropServices.InterfaceTypeAttribute::.ctor(int16) = ( 01 00 01 00 00 00 ) 
  .custom instance void [mscorlib]System.Runtime.InteropServices.GuidAttribute::.ctor(string) = ( 01 00 24 43 43 37 42 43 41 45 42 2D 38 41 36 38   // ..$CC7BCAEB-8A68
                                                                                                  2D 31 31 44 32 2D 39 38 33 43 2D 30 30 30 30 46   // -11D2-983C-0000F
                                                                                                  38 30 38 33 34 32 44 00 00 )                      // 808342D..
  .method public hidebysig newslot abstract virtual 
          instance void  Activate([in] int32 bActive) runtime managed internalcall
  {
  } // end of method ICorDebugValueBreakpoint::Activate

  .method public hidebysig newslot abstract virtual 
          instance void  IsActive([out] int32& pbActive) runtime managed internalcall
  {
  } // end of method ICorDebugValueBreakpoint::IsActive

  .method public hidebysig newslot abstract virtual 
          instance void  GetValue([out] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugValue&  marshal( interface) ppValue) runtime managed internalcall
  {
  } // end of method ICorDebugValueBreakpoint::GetValue

} // end of class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugValueBreakpoint

.class interface public abstract auto ansi import Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugBreakpoint
{
  .custom instance void [mscorlib]System.Runtime.InteropServices.GuidAttribute::.ctor(string) = ( 01 00 24 43 43 37 42 43 41 45 38 2D 38 41 36 38   // ..$CC7BCAE8-8A68
                                                                                                  2D 31 31 44 32 2D 39 38 33 43 2D 30 30 30 30 46   // -11D2-983C-0000F
                                                                                                  38 30 38 33 34 32 44 00 00 )                      // 808342D..
  .custom instance void [mscorlib]System.Runtime.InteropServices.InterfaceTypeAttribute::.ctor(int16) = ( 01 00 01 00 00 00 ) 
  .method public hidebysig newslot abstract virtual 
          instance void  Activate([in] int32 bActive) runtime managed internalcall
  {
  } // end of method ICorDebugBreakpoint::Activate

  .method public hidebysig newslot abstract virtual 
          instance void  IsActive([out] int32& pbActive) runtime managed internalcall
  {
  } // end of method ICorDebugBreakpoint::IsActive

} // end of class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugBreakpoint

.class public auto ansi sealed Microsoft.Samples.Debugging.CorDebug.NativeApi.CorDebugThreadState
       extends [mscorlib]System.Enum
{
  .field public specialname rtspecialname int32 value__
  .field public static literal valuetype Microsoft.Samples.Debugging.CorDebug.NativeApi.CorDebugThreadState THREAD_RUN = int32(0x00000000)
  .field public static literal valuetype Microsoft.Samples.Debugging.CorDebug.NativeApi.CorDebugThreadState THREAD_SUSPEND = int32(0x00000001)
} // end of class Microsoft.Samples.Debugging.CorDebug.NativeApi.CorDebugThreadState

.class public auto ansi sealed Microsoft.Samples.Debugging.CorDebug.NativeApi.CorDebugUserState
       extends [mscorlib]System.Enum
{
  .field public specialname rtspecialname int32 value__
  .field public static literal valuetype Microsoft.Samples.Debugging.CorDebug.NativeApi.CorDebugUserState USER_STOP_REQUESTED = int32(0x00000001)
  .field public static literal valuetype Microsoft.Samples.Debugging.CorDebug.NativeApi.CorDebugUserState USER_SUSPEND_REQUESTED = int32(0x00000002)
  .field public static literal valuetype Microsoft.Samples.Debugging.CorDebug.NativeApi.CorDebugUserState USER_BACKGROUND = int32(0x00000004)
  .field public static literal valuetype Microsoft.Samples.Debugging.CorDebug.NativeApi.CorDebugUserState USER_UNSTARTED = int32(0x00000008)
  .field public static literal valuetype Microsoft.Samples.Debugging.CorDebug.NativeApi.CorDebugUserState USER_STOPPED = int32(0x00000010)
  .field public static literal valuetype Microsoft.Samples.Debugging.CorDebug.NativeApi.CorDebugUserState USER_WAIT_SLEEP_JOIN = int32(0x00000020)
  .field public static literal valuetype Microsoft.Samples.Debugging.CorDebug.NativeApi.CorDebugUserState USER_SUSPENDED = int32(0x00000040)
  .field public static literal valuetype Microsoft.Samples.Debugging.CorDebug.NativeApi.CorDebugUserState USER_UNSAFE_POINT = int32(0x00000080)
} // end of class Microsoft.Samples.Debugging.CorDebug.NativeApi.CorDebugUserState

.class interface public abstract auto ansi import Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugStepper
{
  .custom instance void [mscorlib]System.Runtime.InteropServices.InterfaceTypeAttribute::.ctor(int16) = ( 01 00 01 00 00 00 ) 
  .custom instance void [mscorlib]System.Runtime.InteropServices.GuidAttribute::.ctor(string) = ( 01 00 24 43 43 37 42 43 41 45 43 2D 38 41 36 38   // ..$CC7BCAEC-8A68
                                                                                                  2D 31 31 44 32 2D 39 38 33 43 2D 30 30 30 30 46   // -11D2-983C-0000F
                                                                                                  38 30 38 33 34 32 44 00 00 )                      // 808342D..
  .method public hidebysig newslot abstract virtual 
          instance void  IsActive([out] int32& pbActive) runtime managed internalcall
  {
  } // end of method ICorDebugStepper::IsActive

  .method public hidebysig newslot abstract virtual 
          instance void  Deactivate() runtime managed internalcall
  {
  } // end of method ICorDebugStepper::Deactivate

  .method public hidebysig newslot abstract virtual 
          instance void  SetInterceptMask([in] valuetype Microsoft.Samples.Debugging.CorDebug.NativeApi.CorDebugIntercept mask) runtime managed internalcall
  {
  } // end of method ICorDebugStepper::SetInterceptMask

  .method public hidebysig newslot abstract virtual 
          instance void  SetUnmappedStopMask([in] valuetype Microsoft.Samples.Debugging.CorDebug.NativeApi.CorDebugUnmappedStop mask) runtime managed internalcall
  {
  } // end of method ICorDebugStepper::SetUnmappedStopMask

  .method public hidebysig newslot abstract virtual 
          instance void  Step([in] int32 bStepIn) runtime managed internalcall
  {
  } // end of method ICorDebugStepper::Step

  .method public hidebysig newslot abstract virtual 
          instance void  StepRange([in] int32 bStepIn,
                                   [in] valuetype Microsoft.Samples.Debugging.CorDebug.NativeApi.COR_DEBUG_STEP_RANGE[] marshal([ ]) ranges,
                                   [in] uint32 cRangeCount) runtime managed internalcall
  {
  } // end of method ICorDebugStepper::StepRange

  .method public hidebysig newslot abstract virtual 
          instance void  StepOut() runtime managed internalcall
  {
  } // end of method ICorDebugStepper::StepOut

  .method public hidebysig newslot abstract virtual 
          instance void  SetRangeIL([in] int32 bIL) runtime managed internalcall
  {
  } // end of method ICorDebugStepper::SetRangeIL

} // end of class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugStepper

.class public auto ansi sealed Microsoft.Samples.Debugging.CorDebug.NativeApi.CorDebugIntercept
       extends [mscorlib]System.Enum
{
  .field public specialname rtspecialname int32 value__
  .field public static literal valuetype Microsoft.Samples.Debugging.CorDebug.NativeApi.CorDebugIntercept INTERCEPT_NONE = int32(0x00000000)
  .field public static literal valuetype Microsoft.Samples.Debugging.CorDebug.NativeApi.CorDebugIntercept INTERCEPT_CLASS_INIT = int32(0x00000001)
  .field public static literal valuetype Microsoft.Samples.Debugging.CorDebug.NativeApi.CorDebugIntercept INTERCEPT_EXCEPTION_FILTER = int32(0x00000002)
  .field public static literal valuetype Microsoft.Samples.Debugging.CorDebug.NativeApi.CorDebugIntercept INTERCEPT_SECURITY = int32(0x00000004)
  .field public static literal valuetype Microsoft.Samples.Debugging.CorDebug.NativeApi.CorDebugIntercept INTERCEPT_CONTEXT_POLICY = int32(0x00000008)
  .field public static literal valuetype Microsoft.Samples.Debugging.CorDebug.NativeApi.CorDebugIntercept INTERCEPT_INTERCEPTION = int32(0x00000010)
  .field public static literal valuetype Microsoft.Samples.Debugging.CorDebug.NativeApi.CorDebugIntercept INTERCEPT_ALL = int32(0x0000FFFF)
} // end of class Microsoft.Samples.Debugging.CorDebug.NativeApi.CorDebugIntercept

.class public auto ansi sealed Microsoft.Samples.Debugging.CorDebug.NativeApi.CorDebugUnmappedStop
       extends [mscorlib]System.Enum
{
  .field public specialname rtspecialname int32 value__
  .field public static literal valuetype Microsoft.Samples.Debugging.CorDebug.NativeApi.CorDebugUnmappedStop STOP_NONE = int32(0x00000000)
  .field public static literal valuetype Microsoft.Samples.Debugging.CorDebug.NativeApi.CorDebugUnmappedStop STOP_PROLOG = int32(0x00000001)
  .field public static literal valuetype Microsoft.Samples.Debugging.CorDebug.NativeApi.CorDebugUnmappedStop STOP_EPILOG = int32(0x00000002)
  .field public static literal valuetype Microsoft.Samples.Debugging.CorDebug.NativeApi.CorDebugUnmappedStop STOP_NO_MAPPING_INFO = int32(0x00000004)
  .field public static literal valuetype Microsoft.Samples.Debugging.CorDebug.NativeApi.CorDebugUnmappedStop STOP_OTHER_UNMAPPED = int32(0x00000008)
  .field public static literal valuetype Microsoft.Samples.Debugging.CorDebug.NativeApi.CorDebugUnmappedStop STOP_UNMANAGED = int32(0x00000010)
  .field public static literal valuetype Microsoft.Samples.Debugging.CorDebug.NativeApi.CorDebugUnmappedStop STOP_ALL = int32(0x0000FFFF)
  .field public static literal valuetype Microsoft.Samples.Debugging.CorDebug.NativeApi.CorDebugUnmappedStop STOP_ONLYJUSTMYCODE = int32(0x00010000)
} // end of class Microsoft.Samples.Debugging.CorDebug.NativeApi.CorDebugUnmappedStop

.class public sequential ansi sealed beforefieldinit Microsoft.Samples.Debugging.CorDebug.NativeApi.COR_DEBUG_STEP_RANGE
       extends [mscorlib]System.ValueType
{
  .pack 4
  .size 0
  .field public uint32 startOffset
  .field public uint32 endOffset
} // end of class Microsoft.Samples.Debugging.CorDebug.NativeApi.COR_DEBUG_STEP_RANGE

.class interface public abstract auto ansi import Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugChainEnum
       implements Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugEnum
{
  .custom instance void [mscorlib]System.Runtime.InteropServices.InterfaceTypeAttribute::.ctor(int16) = ( 01 00 01 00 00 00 ) 
  .custom instance void [mscorlib]System.Runtime.InteropServices.ComConversionLossAttribute::.ctor() = ( 01 00 00 00 ) 
  .custom instance void [mscorlib]System.Runtime.InteropServices.GuidAttribute::.ctor(string) = ( 01 00 24 43 43 37 42 43 42 30 38 2D 38 41 36 38   // ..$CC7BCB08-8A68
                                                                                                  2D 31 31 44 32 2D 39 38 33 43 2D 30 30 30 30 46   // -11D2-983C-0000F
                                                                                                  38 30 38 33 34 32 44 00 00 )                      // 808342D..
  .method public hidebysig newslot abstract virtual 
          instance void  Skip([in] uint32 celt) runtime managed internalcall
  {
  } // end of method ICorDebugChainEnum::Skip

  .method public hidebysig newslot abstract virtual 
          instance void  Reset() runtime managed internalcall
  {
  } // end of method ICorDebugChainEnum::Reset

  .method public hidebysig newslot abstract virtual 
          instance void  Clone([out] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugEnum&  marshal( interface) ppEnum) runtime managed internalcall
  {
  } // end of method ICorDebugChainEnum::Clone

  .method public hidebysig newslot abstract virtual 
          instance void  GetCount([out] uint32& pcelt) runtime managed internalcall
  {
  } // end of method ICorDebugChainEnum::GetCount

  .method public hidebysig newslot abstract virtual 
          instance int32  Next([in] uint32 celt,
                              [out] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugChain[] marshal([ ]) chains,
                              [out] uint32& pceltFetched) runtime managed internalcall preservesig
  {
  } // end of method ICorDebugChainEnum::Next

} // end of class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugChainEnum

.class interface public abstract auto ansi import Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugChain
{
  .custom instance void [mscorlib]System.Runtime.InteropServices.GuidAttribute::.ctor(string) = ( 01 00 24 43 43 37 42 43 41 45 45 2D 38 41 36 38   // ..$CC7BCAEE-8A68
                                                                                                  2D 31 31 44 32 2D 39 38 33 43 2D 30 30 30 30 46   // -11D2-983C-0000F
                                                                                                  38 30 38 33 34 32 44 00 00 )                      // 808342D..
  .custom instance void [mscorlib]System.Runtime.InteropServices.InterfaceTypeAttribute::.ctor(int16) = ( 01 00 01 00 00 00 ) 
  .method public hidebysig newslot abstract virtual 
          instance void  GetThread([out] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugThread&  marshal( interface) ppThread) runtime managed internalcall
  {
  } // end of method ICorDebugChain::GetThread

  .method public hidebysig newslot abstract virtual 
          instance void  GetStackRange([out] uint64& pStart,
                                       [out] uint64& pEnd) runtime managed internalcall
  {
  } // end of method ICorDebugChain::GetStackRange

  .method public hidebysig newslot abstract virtual 
          instance void  GetContext([out] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugContext&  marshal( interface) ppContext) runtime managed internalcall
  {
  } // end of method ICorDebugChain::GetContext

  .method public hidebysig newslot abstract virtual 
          instance void  GetCaller([out] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugChain&  marshal( interface) ppChain) runtime managed internalcall
  {
  } // end of method ICorDebugChain::GetCaller

  .method public hidebysig newslot abstract virtual 
          instance void  GetCallee([out] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugChain&  marshal( interface) ppChain) runtime managed internalcall
  {
  } // end of method ICorDebugChain::GetCallee

  .method public hidebysig newslot abstract virtual 
          instance void  GetPrevious([out] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugChain&  marshal( interface) ppChain) runtime managed internalcall
  {
  } // end of method ICorDebugChain::GetPrevious

  .method public hidebysig newslot abstract virtual 
          instance void  GetNext([out] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugChain&  marshal( interface) ppChain) runtime managed internalcall
  {
  } // end of method ICorDebugChain::GetNext

  .method public hidebysig newslot abstract virtual 
          instance void  IsManaged([out] int32& pManaged) runtime managed internalcall
  {
  } // end of method ICorDebugChain::IsManaged

  .method public hidebysig newslot abstract virtual 
          instance void  EnumerateFrames([out] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugFrameEnum&  marshal( interface) ppFrames) runtime managed internalcall
  {
  } // end of method ICorDebugChain::EnumerateFrames

  .method public hidebysig newslot abstract virtual 
          instance void  GetActiveFrame([out] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugFrame&  marshal( interface) ppFrame) runtime managed internalcall
  {
  } // end of method ICorDebugChain::GetActiveFrame

  .method public hidebysig newslot abstract virtual 
          instance void  GetRegisterSet([out] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugRegisterSet&  marshal( interface) ppRegisters) runtime managed internalcall
  {
  } // end of method ICorDebugChain::GetRegisterSet

  .method public hidebysig newslot abstract virtual 
          instance void  GetReason([out] valuetype Microsoft.Samples.Debugging.CorDebug.NativeApi.CorDebugChainReason& pReason) runtime managed internalcall
  {
  } // end of method ICorDebugChain::GetReason

} // end of class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugChain

.class interface public abstract auto ansi import Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugContext
       implements Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugObjectValue
{
  .custom instance void [mscorlib]System.Runtime.InteropServices.InterfaceTypeAttribute::.ctor(int16) = ( 01 00 01 00 00 00 ) 
  .custom instance void [mscorlib]System.Runtime.InteropServices.GuidAttribute::.ctor(string) = ( 01 00 24 43 43 37 42 43 42 30 30 2D 38 41 36 38   // ..$CC7BCB00-8A68
                                                                                                  2D 31 31 44 32 2D 39 38 33 43 2D 30 30 30 30 46   // -11D2-983C-0000F
                                                                                                  38 30 38 33 34 32 44 00 00 )                      // 808342D..
  .method public hidebysig newslot abstract virtual 
          instance void  GetType([out] valuetype Microsoft.Samples.Debugging.CorDebug.NativeApi.CorElementType& pType) runtime managed internalcall
  {
  } // end of method ICorDebugContext::GetType

  .method public hidebysig newslot abstract virtual 
          instance void  GetSize([out] uint32& pSize) runtime managed internalcall
  {
  } // end of method ICorDebugContext::GetSize

  .method public hidebysig newslot abstract virtual 
          instance void  GetAddress([out] uint64& pAddress) runtime managed internalcall
  {
  } // end of method ICorDebugContext::GetAddress

  .method public hidebysig newslot abstract virtual 
          instance void  CreateBreakpoint([out] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugValueBreakpoint&  marshal( interface) ppBreakpoint) runtime managed internalcall
  {
  } // end of method ICorDebugContext::CreateBreakpoint

  .method public hidebysig newslot abstract virtual 
          instance void  GetClass([out] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugClass&  marshal( interface) ppClass) runtime managed internalcall
  {
  } // end of method ICorDebugContext::GetClass

  .method public hidebysig newslot abstract virtual 
          instance void  GetFieldValue([in] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugClass  marshal( interface) pClass,
                                       [in] uint32 fieldDef,
                                       [out] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugValue&  marshal( interface) ppValue) runtime managed internalcall
  {
  } // end of method ICorDebugContext::GetFieldValue

  .method public hidebysig newslot abstract virtual 
          instance void  GetVirtualMethod([in] uint32 memberRef,
                                          [out] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugFunction&  marshal( interface) ppFunction) runtime managed internalcall
  {
  } // end of method ICorDebugContext::GetVirtualMethod

  .method public hidebysig newslot abstract virtual 
          instance void  GetContext([out] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugContext&  marshal( interface) ppContext) runtime managed internalcall
  {
  } // end of method ICorDebugContext::GetContext

  .method public hidebysig newslot abstract virtual 
          instance void  IsValueClass([out] int32& pbIsValueClass) runtime managed internalcall
  {
  } // end of method ICorDebugContext::IsValueClass

  .method public hidebysig newslot abstract virtual 
          instance void  GetManagedCopy([out] object&  marshal( iunknown) ppObject) runtime managed internalcall
  {
  } // end of method ICorDebugContext::GetManagedCopy

  .method public hidebysig newslot abstract virtual 
          instance void  SetFromManagedCopy([in] object  marshal( iunknown) pObject) runtime managed internalcall
  {
  } // end of method ICorDebugContext::SetFromManagedCopy

} // end of class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugContext

.class interface public abstract auto ansi import Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugObjectValue
       implements Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugValue
{
  .custom instance void [mscorlib]System.Runtime.InteropServices.InterfaceTypeAttribute::.ctor(int16) = ( 01 00 01 00 00 00 ) 
  .custom instance void [mscorlib]System.Runtime.InteropServices.GuidAttribute::.ctor(string) = ( 01 00 24 31 38 41 44 33 44 36 45 2D 42 37 44 32   // ..$18AD3D6E-B7D2
                                                                                                  2D 31 31 44 32 2D 42 44 30 34 2D 30 30 30 30 46   // -11D2-BD04-0000F
                                                                                                  38 30 38 34 39 42 44 00 00 )                      // 80849BD..
  .method public hidebysig newslot abstract virtual 
          instance void  GetType([out] valuetype Microsoft.Samples.Debugging.CorDebug.NativeApi.CorElementType& pType) runtime managed internalcall
  {
  } // end of method ICorDebugObjectValue::GetType

  .method public hidebysig newslot abstract virtual 
          instance void  GetSize([out] uint32& pSize) runtime managed internalcall
  {
  } // end of method ICorDebugObjectValue::GetSize

  .method public hidebysig newslot abstract virtual 
          instance void  GetAddress([out] uint64& pAddress) runtime managed internalcall
  {
  } // end of method ICorDebugObjectValue::GetAddress

  .method public hidebysig newslot abstract virtual 
          instance void  CreateBreakpoint([out] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugValueBreakpoint&  marshal( interface) ppBreakpoint) runtime managed internalcall
  {
  } // end of method ICorDebugObjectValue::CreateBreakpoint

  .method public hidebysig newslot abstract virtual 
          instance void  GetClass([out] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugClass&  marshal( interface) ppClass) runtime managed internalcall
  {
  } // end of method ICorDebugObjectValue::GetClass

  .method public hidebysig newslot abstract virtual 
          instance void  GetFieldValue([in] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugClass  marshal( interface) pClass,
                                       [in] uint32 fieldDef,
                                       [out] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugValue&  marshal( interface) ppValue) runtime managed internalcall
  {
  } // end of method ICorDebugObjectValue::GetFieldValue

  .method public hidebysig newslot abstract virtual 
          instance void  GetVirtualMethod([in] uint32 memberRef,
                                          [out] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugFunction&  marshal( interface) ppFunction) runtime managed internalcall
  {
  } // end of method ICorDebugObjectValue::GetVirtualMethod

  .method public hidebysig newslot abstract virtual 
          instance void  GetContext([out] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugContext&  marshal( interface) ppContext) runtime managed internalcall
  {
  } // end of method ICorDebugObjectValue::GetContext

  .method public hidebysig newslot abstract virtual 
          instance void  IsValueClass([out] int32& pbIsValueClass) runtime managed internalcall
  {
  } // end of method ICorDebugObjectValue::IsValueClass

  .method public hidebysig newslot abstract virtual 
          instance void  GetManagedCopy([out] object&  marshal( iunknown) ppObject) runtime managed internalcall
  {
  } // end of method ICorDebugObjectValue::GetManagedCopy

  .method public hidebysig newslot abstract virtual 
          instance void  SetFromManagedCopy([in] object  marshal( iunknown) pObject) runtime managed internalcall
  {
  } // end of method ICorDebugObjectValue::SetFromManagedCopy

} // end of class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugObjectValue

.class interface public abstract auto ansi import Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugClass
{
  .custom instance void [mscorlib]System.Runtime.InteropServices.GuidAttribute::.ctor(string) = ( 01 00 24 43 43 37 42 43 41 46 35 2D 38 41 36 38   // ..$CC7BCAF5-8A68
                                                                                                  2D 31 31 44 32 2D 39 38 33 43 2D 30 30 30 30 46   // -11D2-983C-0000F
                                                                                                  38 30 38 33 34 32 44 00 00 )                      // 808342D..
  .custom instance void [mscorlib]System.Runtime.InteropServices.InterfaceTypeAttribute::.ctor(int16) = ( 01 00 01 00 00 00 ) 
  .method public hidebysig newslot abstract virtual 
          instance void  GetModule([out] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugModule&  marshal( interface) pModule) runtime managed internalcall
  {
  } // end of method ICorDebugClass::GetModule

  .method public hidebysig newslot abstract virtual 
          instance void  GetToken([out] uint32& pTypeDef) runtime managed internalcall
  {
  } // end of method ICorDebugClass::GetToken

  .method public hidebysig newslot abstract virtual 
          instance void  GetStaticFieldValue([in] uint32 fieldDef,
                                             [in] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugFrame  marshal( interface) pFrame,
                                             [out] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugValue&  marshal( interface) ppValue) runtime managed internalcall
  {
  } // end of method ICorDebugClass::GetStaticFieldValue

} // end of class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugClass


.class interface public abstract auto ansi import Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugMDA
{
  //.custom instance void [mscorlib]System.Runtime.InteropServices.ComConversionLossAttribute::.ctor() = ( 01 00 00 00 ) 
  .custom instance void [mscorlib]System.Runtime.InteropServices.GuidAttribute::.ctor(string) = ( 01 00 24 43 43 37 32 36 46 32 46 2D 31 44 42 37   // ..$CC726F2F-1DB7
                                                                                                  2D 34 35 39 42 2D 42 30 45 43 2D 30 35 46 30 31   // -459B-B0EC-05F01
                                                                                                  44 38 34 31 42 34 32 00 00 )                      // D841B42..
  .custom instance void [mscorlib]System.Runtime.InteropServices.InterfaceTypeAttribute::.ctor(int16) = ( 01 00 01 00 00 00 ) 
  .method public hidebysig newslot abstract virtual 
          instance void  GetName([in] uint32 cchName,
                                 [out] uint32& pcchName,
                                 char[] marshal([ ]) szName) runtime managed internalcall
  {
  } // end of method ICorDebugMDA::GetName

                                
  .method public hidebysig newslot abstract virtual 
          instance void  GetDescription([in] uint32 cchName,
                                        [out] uint32& pcchName,
                                        char[] marshal([ ]) szName) runtime managed internalcall
  {
  } // end of method ICorDebugMDA::GetDescription

  .method public hidebysig newslot abstract virtual 
          instance void  GetXML([in] uint32 cchName,
                                [out] uint32& pcchName,
                                char[] marshal([ ]) szName) runtime managed internalcall
  {
  } // end of method ICorDebugMDA::GetXML

  .method public hidebysig newslot abstract virtual 
          instance void  GetFlags([out] valuetype Microsoft.Samples.Debugging.CorDebug.NativeApi.CorDebugMDAFlags& pFlags) runtime managed internalcall
  {
  } // end of method ICorDebugMDA::GetFlags

  .method public hidebysig newslot abstract virtual 
          instance void  GetOSThreadId([out] uint32& pOsTid) runtime managed internalcall
  {
  } // end of method ICorDebugMDA::GetOSThreadId

} // end of class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugMDA

.class interface public abstract auto ansi import Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugModule
{
  .custom instance void [mscorlib]System.Runtime.InteropServices.GuidAttribute::.ctor(string) = ( 01 00 24 44 42 41 32 44 38 43 31 2D 45 35 43 35   // ..$DBA2D8C1-E5C5
                                                                                                  2D 34 30 36 39 2D 38 43 31 33 2D 31 30 41 37 43   // -4069-8C13-10A7C
                                                                                                  36 41 42 46 34 33 44 00 00 )                      // 6ABF43D..
  .custom instance void [mscorlib]System.Runtime.InteropServices.InterfaceTypeAttribute::.ctor(int16) = ( 01 00 01 00 00 00 ) 
  .custom instance void [mscorlib]System.Runtime.InteropServices.ComConversionLossAttribute::.ctor() = ( 01 00 00 00 ) 
  .method public hidebysig newslot abstract virtual 
          instance void  GetProcess([out] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugProcess&  marshal( interface) ppProcess) runtime managed internalcall
  {
  } // end of method ICorDebugModule::GetProcess

  .method public hidebysig newslot abstract virtual 
          instance void  GetBaseAddress([out] uint64& pAddress) runtime managed internalcall
  {
  } // end of method ICorDebugModule::GetBaseAddress

  .method public hidebysig newslot abstract virtual 
          instance void  GetAssembly([out] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugAssembly&  marshal( interface) ppAssembly) runtime managed internalcall
  {
  } // end of method ICorDebugModule::GetAssembly

  .method public hidebysig newslot abstract virtual 
          instance void  GetName([in] uint32 cchName,
                                 [out] uint32& pcchName,
                                 char[] marshal([ ]) szName) runtime managed internalcall
  {
  } // end of method ICorDebugModule::GetName

  .method public hidebysig newslot abstract virtual 
          instance void  EnableJITDebugging([in] int32 bTrackJITInfo,
                                            [in] int32 bAllowJitOpts) runtime managed internalcall
  {
  } // end of method ICorDebugModule::EnableJITDebugging

  .method public hidebysig newslot abstract virtual 
          instance void  EnableClassLoadCallbacks([in] int32 bClassLoadCallbacks) runtime managed internalcall
  {
  } // end of method ICorDebugModule::EnableClassLoadCallbacks

  .method public hidebysig newslot abstract virtual 
          instance void  GetFunctionFromToken([in] uint32 methodDef,
                                              [out] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugFunction&  marshal( interface) ppFunction) runtime managed internalcall
  {
  } // end of method ICorDebugModule::GetFunctionFromToken

  .method public hidebysig newslot abstract virtual 
          instance void  GetFunctionFromRVA([in] uint64 rva,
                                            [out] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugFunction&  marshal( interface) ppFunction) runtime managed internalcall
  {
  } // end of method ICorDebugModule::GetFunctionFromRVA

  .method public hidebysig newslot abstract virtual 
          instance void  GetClassFromToken([in] uint32 typeDef,
                                           [out] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugClass&  marshal( interface) ppClass) runtime managed internalcall
  {
  } // end of method ICorDebugModule::GetClassFromToken

  .method public hidebysig newslot abstract virtual 
          instance void  CreateBreakpoint([out] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugModuleBreakpoint&  marshal( interface) ppBreakpoint) runtime managed internalcall
  {
  } // end of method ICorDebugModule::CreateBreakpoint

  .method public hidebysig newslot abstract virtual 
          instance void  GetEditAndContinueSnapshot([out] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugEditAndContinueSnapshot&  marshal( interface) ppEditAndContinueSnapshot) runtime managed internalcall
  {
  } // end of method ICorDebugModule::GetEditAndContinueSnapshot

  .method public hidebysig newslot abstract virtual 
          instance void  GetMetaDataInterface([in] valuetype [mscorlib]System.Guid& riid,
  // TODO: We may want to just return an IUnknown here, but then the fake-com wrappers will need some way of knowing how to wrap it
                                              [out] class Microsoft.Samples.Debugging.CorMetadata.NativeApi.IMetadataImport&  marshal( interface)  ppObj) runtime managed internalcall
  {
    .param [1]
    .custom instance void [mscorlib]System.Runtime.InteropServices.ComAliasNameAttribute::.ctor(string) = ( 01 00 06 52 45 46 49 49 44 00 00 )                // ...REFIID..
  } // end of method ICorDebugModule::GetMetaDataInterface

  .method public hidebysig newslot abstract virtual 
          instance void  GetToken([out] uint32& pToken) runtime managed internalcall
  {
  } // end of method ICorDebugModule::GetToken

  .method public hidebysig newslot abstract virtual 
          instance void  IsDynamic([out] int32& pDynamic) runtime managed internalcall
  {
  } // end of method ICorDebugModule::IsDynamic

  .method public hidebysig newslot abstract virtual 
          instance void  GetGlobalVariableValue([in] uint32 fieldDef,
                                                [out] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugValue&  marshal( interface) ppValue) runtime managed internalcall
  {
  } // end of method ICorDebugModule::GetGlobalVariableValue

  .method public hidebysig newslot abstract virtual 
          instance void  GetSize([out] uint32& pcBytes) runtime managed internalcall
  {
  } // end of method ICorDebugModule::GetSize

  .method public hidebysig newslot abstract virtual 
          instance void  IsInMemory([out] int32& pInMemory) runtime managed internalcall
  {
  } // end of method ICorDebugModule::IsInMemory

} // end of class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugModule

.class interface public abstract auto ansi import Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugAssembly
{
  .custom instance void [mscorlib]System.Runtime.InteropServices.GuidAttribute::.ctor(string) = ( 01 00 24 44 46 35 39 35 30 37 43 2D 44 34 37 41   // ..$DF59507C-D47A
                                                                                                  2D 34 35 39 45 2D 42 43 45 32 2D 36 34 32 37 45   // -459E-BCE2-6427E
                                                                                                  41 43 38 46 44 30 36 00 00 )                      // AC8FD06..
  .custom instance void [mscorlib]System.Runtime.InteropServices.InterfaceTypeAttribute::.ctor(int16) = ( 01 00 01 00 00 00 ) 
  .custom instance void [mscorlib]System.Runtime.InteropServices.ComConversionLossAttribute::.ctor() = ( 01 00 00 00 ) 
  .method public hidebysig newslot abstract virtual 
          instance void  GetProcess([out] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugProcess&  marshal( interface) ppProcess) runtime managed internalcall
  {
  } // end of method ICorDebugAssembly::GetProcess

  .method public hidebysig newslot abstract virtual 
          instance void  GetAppDomain([out] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugAppDomain&  marshal( interface) ppAppDomain) runtime managed internalcall
  {
  } // end of method ICorDebugAssembly::GetAppDomain

  .method public hidebysig newslot abstract virtual 
          instance void  EnumerateModules([out] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugModuleEnum&  marshal( interface) ppModules) runtime managed internalcall
  {
  } // end of method ICorDebugAssembly::EnumerateModules

  .method public hidebysig newslot abstract virtual 
          instance void  GetCodeBase([in] uint32 cchName,
                                     [out] uint32& pcchName,
                                     char[] marshal([ ]) szName) runtime managed internalcall
  {
  } // end of method ICorDebugAssembly::GetCodeBase

  .method public hidebysig newslot abstract virtual 
          instance void  GetName([in] uint32 cchName,
                                 [out] uint32& pcchName,
                                 char[] marshal([ ]) szName) runtime managed internalcall
  {
  } // end of method ICorDebugAssembly::GetName

} // end of class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugAssembly

.class interface public abstract auto ansi import Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugAssembly2
{
  .custom instance void [mscorlib]System.Runtime.InteropServices.GuidAttribute::.ctor(string) = ( 01 00 24 34 32 36 44 31 46 39 45 2D 36 44 44 34   // ..$426D1F9E-6DD4
                                                                                                  2D 34 34 43 38 2D 41 45 43 37 2D 32 36 43 44 42   // -44C8-AEC7-26CDB
                                                                                                  41 46 34 45 33 39 38 00 00 )                      // AF4E398..
  .custom instance void [mscorlib]System.Runtime.InteropServices.InterfaceTypeAttribute::.ctor(int16) = ( 01 00 01 00 00 00 ) 
  .method public hidebysig newslot abstract virtual 
          instance void  IsFullyTrusted([out] int32& pbFullyTrusted) runtime managed internalcall
  {
  } // end of method ICorDebugAssembly2::IsFullyTrusted

} // end of class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugAssembly2

.class interface public abstract auto ansi import Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugModuleEnum
       implements Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugEnum
{
  .custom instance void [mscorlib]System.Runtime.InteropServices.GuidAttribute::.ctor(string) = ( 01 00 24 43 43 37 42 43 42 30 39 2D 38 41 36 38   // ..$CC7BCB09-8A68
                                                                                                  2D 31 31 44 32 2D 39 38 33 43 2D 30 30 30 30 46   // -11D2-983C-0000F
                                                                                                  38 30 38 33 34 32 44 00 00 )                      // 808342D..
  .custom instance void [mscorlib]System.Runtime.InteropServices.InterfaceTypeAttribute::.ctor(int16) = ( 01 00 01 00 00 00 ) 
  .custom instance void [mscorlib]System.Runtime.InteropServices.ComConversionLossAttribute::.ctor() = ( 01 00 00 00 ) 
  .method public hidebysig newslot abstract virtual 
          instance void  Skip([in] uint32 celt) runtime managed internalcall
  {
  } // end of method ICorDebugModuleEnum::Skip

  .method public hidebysig newslot abstract virtual 
          instance void  Reset() runtime managed internalcall
  {
  } // end of method ICorDebugModuleEnum::Reset

  .method public hidebysig newslot abstract virtual 
          instance void  Clone([out] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugEnum&  marshal( interface) ppEnum) runtime managed internalcall
  {
  } // end of method ICorDebugModuleEnum::Clone

  .method public hidebysig newslot abstract virtual 
          instance void  GetCount([out] uint32& pcelt) runtime managed internalcall
  {
  } // end of method ICorDebugModuleEnum::GetCount

  .method public hidebysig newslot abstract virtual 
          instance int32  Next([in] uint32 celt,
                              [out] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugModule[] marshal([ ]) modules,
                              [out] uint32& pceltFetched) runtime managed internalcall preservesig
  {
  } // end of method ICorDebugModuleEnum::Next

} // end of class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugModuleEnum

.class interface public abstract auto ansi import Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugFunction
{
  .custom instance void [mscorlib]System.Runtime.InteropServices.GuidAttribute::.ctor(string) = ( 01 00 24 43 43 37 42 43 41 46 33 2D 38 41 36 38   // ..$CC7BCAF3-8A68
                                                                                                  2D 31 31 44 32 2D 39 38 33 43 2D 30 30 30 30 46   // -11D2-983C-0000F
                                                                                                  38 30 38 33 34 32 44 00 00 )                      // 808342D..
  .custom instance void [mscorlib]System.Runtime.InteropServices.InterfaceTypeAttribute::.ctor(int16) = ( 01 00 01 00 00 00 ) 
  .method public hidebysig newslot abstract virtual 
          instance void  GetModule([out] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugModule&  marshal( interface) ppModule) runtime managed internalcall
  {
  } // end of method ICorDebugFunction::GetModule

  .method public hidebysig newslot abstract virtual 
          instance void  GetClass([out] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugClass&  marshal( interface) ppClass) runtime managed internalcall
  {
  } // end of method ICorDebugFunction::GetClass

  .method public hidebysig newslot abstract virtual 
          instance void  GetToken([out] uint32& pMethodDef) runtime managed internalcall
  {
  } // end of method ICorDebugFunction::GetToken

  .method public hidebysig newslot abstract virtual 
          instance void  GetILCode([out] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugCode&  marshal( interface) ppCode) runtime managed internalcall
  {
  } // end of method ICorDebugFunction::GetILCode

  .method public hidebysig newslot abstract virtual 
          instance void  GetNativeCode([out] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugCode&  marshal( interface) ppCode) runtime managed internalcall
  {
  } // end of method ICorDebugFunction::GetNativeCode

  .method public hidebysig newslot abstract virtual 
          instance void  CreateBreakpoint([out] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugFunctionBreakpoint&  marshal( interface) ppBreakpoint) runtime managed internalcall
  {
  } // end of method ICorDebugFunction::CreateBreakpoint

  .method public hidebysig newslot abstract virtual 
          instance void  GetLocalVarSigToken([out] uint32& pmdSig) runtime managed internalcall
  {
  } // end of method ICorDebugFunction::GetLocalVarSigToken

  .method public hidebysig newslot abstract virtual 
          instance void  GetCurrentVersionNumber([out] uint32& pnCurrentVersion) runtime managed internalcall
  {
  } // end of method ICorDebugFunction::GetCurrentVersionNumber

} // end of class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugFunction

.class interface public abstract auto ansi import Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugCode
{
  .custom instance void [mscorlib]System.Runtime.InteropServices.InterfaceTypeAttribute::.ctor(int16) = ( 01 00 01 00 00 00 ) 
  .custom instance void [mscorlib]System.Runtime.InteropServices.GuidAttribute::.ctor(string) = ( 01 00 24 43 43 37 42 43 41 46 34 2D 38 41 36 38   // ..$CC7BCAF4-8A68
                                                                                                  2D 31 31 44 32 2D 39 38 33 43 2D 30 30 30 30 46   // -11D2-983C-0000F
                                                                                                  38 30 38 33 34 32 44 00 00 )                      // 808342D..
  .custom instance void [mscorlib]System.Runtime.InteropServices.ComConversionLossAttribute::.ctor() = ( 01 00 00 00 ) 
  .method public hidebysig newslot abstract virtual 
          instance void  IsIL([out] int32& pbIL) runtime managed internalcall
  {
  } // end of method ICorDebugCode::IsIL

  .method public hidebysig newslot abstract virtual 
          instance void  GetFunction([out] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugFunction&  marshal( interface) ppFunction) runtime managed internalcall
  {
  } // end of method ICorDebugCode::GetFunction

  .method public hidebysig newslot abstract virtual 
          instance void  GetAddress([out] uint64& pStart) runtime managed internalcall
  {
  } // end of method ICorDebugCode::GetAddress

  .method public hidebysig newslot abstract virtual 
          instance void  GetSize([out] uint32& pcBytes) runtime managed internalcall
  {
  } // end of method ICorDebugCode::GetSize

  .method public hidebysig newslot abstract virtual 
          instance void  CreateBreakpoint([in] uint32 offset,
                                          [out] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugFunctionBreakpoint&  marshal( interface) ppBreakpoint) runtime managed internalcall
  {
  } // end of method ICorDebugCode::CreateBreakpoint

  .method public hidebysig newslot abstract virtual 
          instance void  GetCode([in] uint32 startOffset,
                                 [in] uint32 endOffset,
                                 [in] uint32 cBufferAlloc,
                                 [out] unsigned int8[] marshal([+5]) buffer,
                                 [out] uint32& pcBufferSize) runtime managed internalcall
  {
  } // end of method ICorDebugCode::GetCode

  .method public hidebysig newslot abstract virtual 
          instance void  GetVersionNumber([out] uint32& nVersion) runtime managed internalcall
  {
  } // end of method ICorDebugCode::GetVersionNumber

  .method public hidebysig newslot abstract virtual 
          instance void  GetILToNativeMapping([in] uint32 cMap,
                                              [out] uint32& pcMap,
                                              [out] valuetype Microsoft.Samples.Debugging.CorDebug.NativeApi.COR_DEBUG_IL_TO_NATIVE_MAP[] marshal( [ +1 ] ) map) runtime managed internalcall
  {
  } // end of method ICorDebugCode::GetILToNativeMapping

  .method public hidebysig newslot abstract virtual 
          instance void  GetEnCRemapSequencePoints([in] uint32 cMap,
                                                   [out] uint32& pcMap,
                                                   [out] uint32[] marshal([]) offsets) runtime managed internalcall
  {
  } // end of method ICorDebugCode::GetEnCRemapSequencePoints

} // end of class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugCode

.class interface public abstract auto ansi import Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugCode2
{
  .custom instance void [mscorlib]System.Runtime.InteropServices.InterfaceTypeAttribute::.ctor(int16) = ( 01 00 01 00 00 00 ) 
  .custom instance void [mscorlib]System.Runtime.InteropServices.GuidAttribute::.ctor(string) = ( 01 00 24 35 46 36 39 36 35 30 39 2D 34 35 32 46   // ..$5F696509-452F
                                                                                                  2D 34 34 33 36 2D 41 33 46 45 2D 34 44 31 31 46   // -4436-A3FE-4D11F
                                                                                                  45 37 45 32 33 34 37 00 00 )
  .custom instance void [mscorlib]System.Runtime.InteropServices.ComConversionLossAttribute::.ctor() = ( 01 00 00 00 ) 
  .method public hidebysig newslot abstract virtual 
          instance void  GetCodeChunks([in] uint32 cbufSize, [out] uint32& pcnumChunks, [out] valuetype Microsoft.Samples.Debugging.CorDebug.NativeApi._CodeChunkInfo[] marshal( [+1] ) chunks) runtime managed internalcall
  {
  } // end of method ICorDebugCode2::GetCodeChunks

  .method public hidebysig newslot abstract virtual 
          instance void  GetCompilerFlags([out] uint32& pdwFlags) runtime managed internalcall
  {
  } // end of method ICorDebugCode2::GetCompilerFlags
  
} // end of class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugCode2

.class interface public abstract auto ansi import Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugFunctionBreakpoint
       implements Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugBreakpoint
{
  .custom instance void [mscorlib]System.Runtime.InteropServices.InterfaceTypeAttribute::.ctor(int16) = ( 01 00 01 00 00 00 ) 
  .custom instance void [mscorlib]System.Runtime.InteropServices.GuidAttribute::.ctor(string) = ( 01 00 24 43 43 37 42 43 41 45 39 2D 38 41 36 38   // ..$CC7BCAE9-8A68
                                                                                                  2D 31 31 44 32 2D 39 38 33 43 2D 30 30 30 30 46   // -11D2-983C-0000F
                                                                                                  38 30 38 33 34 32 44 00 00 )                      // 808342D..
  .method public hidebysig newslot abstract virtual 
          instance void  Activate([in] int32 bActive) runtime managed internalcall
  {
  } // end of method ICorDebugFunctionBreakpoint::Activate

  .method public hidebysig newslot abstract virtual 
          instance void  IsActive([out] int32& pbActive) runtime managed internalcall
  {
  } // end of method ICorDebugFunctionBreakpoint::IsActive

  .method public hidebysig newslot abstract virtual 
          instance void  GetFunction([out] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugFunction&  marshal( interface) ppFunction) runtime managed internalcall
  {
  } // end of method ICorDebugFunctionBreakpoint::GetFunction

  .method public hidebysig newslot abstract virtual 
          instance void  GetOffset([out] uint32& pnOffset) runtime managed internalcall
  {
  } // end of method ICorDebugFunctionBreakpoint::GetOffset

} // end of class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugFunctionBreakpoint

.class interface public abstract auto ansi import Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugModuleBreakpoint
       implements Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugBreakpoint
{
  .custom instance void [mscorlib]System.Runtime.InteropServices.GuidAttribute::.ctor(string) = ( 01 00 24 43 43 37 42 43 41 45 41 2D 38 41 36 38   // ..$CC7BCAEA-8A68
                                                                                                  2D 31 31 44 32 2D 39 38 33 43 2D 30 30 30 30 46   // -11D2-983C-0000F
                                                                                                  38 30 38 33 34 32 44 00 00 )                      // 808342D..
  .custom instance void [mscorlib]System.Runtime.InteropServices.InterfaceTypeAttribute::.ctor(int16) = ( 01 00 01 00 00 00 ) 
  .method public hidebysig newslot abstract virtual 
          instance void  Activate([in] int32 bActive) runtime managed internalcall
  {
  } // end of method ICorDebugModuleBreakpoint::Activate

  .method public hidebysig newslot abstract virtual 
          instance void  IsActive([out] int32& pbActive) runtime managed internalcall
  {
  } // end of method ICorDebugModuleBreakpoint::IsActive

  .method public hidebysig newslot abstract virtual 
          instance void  GetModule([out] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugModule&  marshal( interface) ppModule) runtime managed internalcall
  {
  } // end of method ICorDebugModuleBreakpoint::GetModule

} // end of class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugModuleBreakpoint

.class interface public abstract auto ansi import Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugEditAndContinueSnapshot
{
  .custom instance void [mscorlib]System.Runtime.InteropServices.GuidAttribute::.ctor(string) = ( 01 00 24 36 44 43 33 46 41 30 31 2D 44 37 43 42   // ..$6DC3FA01-D7CB
                                                                                                  2D 31 31 44 32 2D 38 41 39 35 2D 30 30 38 30 43   // -11D2-8A95-0080C
                                                                                                  37 39 32 45 35 44 38 00 00 )                      // 792E5D8..
  .custom instance void [mscorlib]System.Runtime.InteropServices.InterfaceTypeAttribute::.ctor(int16) = ( 01 00 01 00 00 00 ) 
  .method public hidebysig newslot abstract virtual 
          instance void  CopyMetaData([in] class [mscorlib]System.Runtime.InteropServices.ComTypes.IStream  marshal( interface) pIStream,
                                      [out] valuetype [mscorlib]System.Guid& pMvid) runtime managed internalcall
  {
  } // end of method ICorDebugEditAndContinueSnapshot::CopyMetaData

  .method public hidebysig newslot abstract virtual 
          instance void  GetMvid([out] valuetype [mscorlib]System.Guid& pMvid) runtime managed internalcall
  {
  } // end of method ICorDebugEditAndContinueSnapshot::GetMvid

  .method public hidebysig newslot abstract virtual 
          instance void  GetRoDataRVA([out] uint32& pRoDataRVA) runtime managed internalcall
  {
  } // end of method ICorDebugEditAndContinueSnapshot::GetRoDataRVA

  .method public hidebysig newslot abstract virtual 
          instance void  GetRwDataRVA([out] uint32& pRwDataRVA) runtime managed internalcall
  {
  } // end of method ICorDebugEditAndContinueSnapshot::GetRwDataRVA

  .method public hidebysig newslot abstract virtual 
          instance void  SetPEBytes([in] class [mscorlib]System.Runtime.InteropServices.ComTypes.IStream  marshal( interface) pIStream) runtime managed internalcall
  {
  } // end of method ICorDebugEditAndContinueSnapshot::SetPEBytes

  .method public hidebysig newslot abstract virtual 
          instance void  SetILMap([in] uint32 mdFunction,
                                  [in] uint32 cMapSize,
                                  [in] valuetype Microsoft.Samples.Debugging.CorDebug.NativeApi.COR_IL_MAP& map) runtime managed internalcall
  {
  } // end of method ICorDebugEditAndContinueSnapshot::SetILMap

  .method public hidebysig newslot abstract virtual 
          instance void  SetPESymbolBytes([in] class [mscorlib]System.Runtime.InteropServices.ComTypes.IStream  marshal( interface) pIStream) runtime managed internalcall
  {
  } // end of method ICorDebugEditAndContinueSnapshot::SetPESymbolBytes

} // end of class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugEditAndContinueSnapshot

.class public sequential ansi sealed beforefieldinit Microsoft.Samples.Debugging.CorDebug.NativeApi._LARGE_INTEGER
       extends [mscorlib]System.ValueType
{
  .pack 8
  .size 0
  .field public int64 QuadPart
} // end of class Microsoft.Samples.Debugging.CorDebug.NativeApi._LARGE_INTEGER

.class public sequential ansi sealed beforefieldinit Microsoft.Samples.Debugging.CorDebug.NativeApi._ULARGE_INTEGER
       extends [mscorlib]System.ValueType
{
  .pack 8
  .size 0
  .field public uint64 QuadPart
} // end of class Microsoft.Samples.Debugging.CorDebug.NativeApi._ULARGE_INTEGER


.class public sequential ansi sealed beforefieldinit Microsoft.Samples.Debugging.CorDebug.NativeApi.COR_IL_MAP
       extends [mscorlib]System.ValueType
{
  .pack 4
  .size 0
  .field public uint32 oldOffset
  .field public uint32 newOffset
  .field public int32 fAccurate
} // end of class Microsoft.Samples.Debugging.CorDebug.NativeApi.COR_IL_MAP

.class interface public abstract auto ansi import Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugFrame
{
  .custom instance void [mscorlib]System.Runtime.InteropServices.InterfaceTypeAttribute::.ctor(int16) = ( 01 00 01 00 00 00 ) 
  .custom instance void [mscorlib]System.Runtime.InteropServices.GuidAttribute::.ctor(string) = ( 01 00 24 43 43 37 42 43 41 45 46 2D 38 41 36 38   // ..$CC7BCAEF-8A68
                                                                                                  2D 31 31 44 32 2D 39 38 33 43 2D 30 30 30 30 46   // -11D2-983C-0000F
                                                                                                  38 30 38 33 34 32 44 00 00 )                      // 808342D..
  .method public hidebysig newslot abstract virtual 
          instance void  GetChain([out] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugChain&  marshal( interface) ppChain) runtime managed internalcall
  {
  } // end of method ICorDebugFrame::GetChain

  .method public hidebysig newslot abstract virtual 
          instance void  GetCode([out] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugCode&  marshal( interface) ppCode) runtime managed internalcall
  {
  } // end of method ICorDebugFrame::GetCode

  .method public hidebysig newslot abstract virtual 
          instance void  GetFunction([out] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugFunction&  marshal( interface) ppFunction) runtime managed internalcall
  {
  } // end of method ICorDebugFrame::GetFunction

  .method public hidebysig newslot abstract virtual 
          instance void  GetFunctionToken([out] uint32& pToken) runtime managed internalcall
  {
  } // end of method ICorDebugFrame::GetFunctionToken

  .method public hidebysig newslot abstract virtual 
          instance void  GetStackRange([out] uint64& pStart,
                                       [out] uint64& pEnd) runtime managed internalcall
  {
  } // end of method ICorDebugFrame::GetStackRange

  .method public hidebysig newslot abstract virtual 
          instance void  GetCaller([out] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugFrame&  marshal( interface) ppFrame) runtime managed internalcall
  {
  } // end of method ICorDebugFrame::GetCaller

  .method public hidebysig newslot abstract virtual 
          instance void  GetCallee([out] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugFrame&  marshal( interface) ppFrame) runtime managed internalcall
  {
  } // end of method ICorDebugFrame::GetCallee

  .method public hidebysig newslot abstract virtual 
          instance void  CreateStepper([out] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugStepper&  marshal( interface) ppStepper) runtime managed internalcall
  {
  } // end of method ICorDebugFrame::CreateStepper

} // end of class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugFrame

.class interface public abstract auto ansi import Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugFrameEnum
       implements Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugEnum
{
  .custom instance void [mscorlib]System.Runtime.InteropServices.InterfaceTypeAttribute::.ctor(int16) = ( 01 00 01 00 00 00 ) 
  .custom instance void [mscorlib]System.Runtime.InteropServices.ComConversionLossAttribute::.ctor() = ( 01 00 00 00 ) 
  .custom instance void [mscorlib]System.Runtime.InteropServices.GuidAttribute::.ctor(string) = ( 01 00 24 43 43 37 42 43 42 30 37 2D 38 41 36 38   // ..$CC7BCB07-8A68
                                                                                                  2D 31 31 44 32 2D 39 38 33 43 2D 30 30 30 30 46   // -11D2-983C-0000F
                                                                                                  38 30 38 33 34 32 44 00 00 )                      // 808342D..
  .method public hidebysig newslot abstract virtual 
          instance void  Skip([in] uint32 celt) runtime managed internalcall
  {
  } // end of method ICorDebugFrameEnum::Skip

  .method public hidebysig newslot abstract virtual 
          instance void  Reset() runtime managed internalcall
  {
  } // end of method ICorDebugFrameEnum::Reset

  .method public hidebysig newslot abstract virtual 
          instance void  Clone([out] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugEnum&  marshal( interface) ppEnum) runtime managed internalcall
  {
  } // end of method ICorDebugFrameEnum::Clone

  .method public hidebysig newslot abstract virtual 
          instance void  GetCount([out] uint32& pcelt) runtime managed internalcall
  {
  } // end of method ICorDebugFrameEnum::GetCount

  .method public hidebysig newslot abstract virtual 
          instance int32  Next([in] uint32 celt,
                              [out] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugFrame[] marshal([ ]) frames,
                              [out] uint32& pceltFetched) runtime managed internalcall preservesig
  {
  } // end of method ICorDebugFrameEnum::Next

} // end of class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugFrameEnum

.class interface public abstract auto ansi import Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugRegisterSet
{
  .custom instance void [mscorlib]System.Runtime.InteropServices.InterfaceTypeAttribute::.ctor(int16) = ( 01 00 01 00 00 00 ) 
  .custom instance void [mscorlib]System.Runtime.InteropServices.GuidAttribute::.ctor(string) = ( 01 00 24 43 43 37 42 43 42 30 42 2D 38 41 36 38   // ..$CC7BCB0B-8A68
                                                                                                  2D 31 31 44 32 2D 39 38 33 43 2D 30 30 30 30 46   // -11D2-983C-0000F
                                                                                                  38 30 38 33 34 32 44 00 00 )                      // 808342D..
  .custom instance void [mscorlib]System.Runtime.InteropServices.ComConversionLossAttribute::.ctor() = ( 01 00 00 00 ) 
  .method public hidebysig newslot abstract virtual 
          instance void  GetRegistersAvailable([out] uint64& pAvailable) runtime managed internalcall
  {
  } // end of method ICorDebugRegisterSet::GetRegistersAvailable

  .method public hidebysig newslot abstract virtual 
          instance void  GetRegisters([in] uint64 mask,
                                      [in] uint32 regCount,
                                      [out] uint64[] marshal( [ +2 ]) regBuffer) runtime managed internalcall
  {
  } // end of method ICorDebugRegisterSet::GetRegisters

  .method public hidebysig newslot abstract virtual 
          instance void  SetRegisters([in] uint64 mask,
                                      [in] uint32 regCount,
                                      [in] uint64[] marshal( [ +2 ]) regBuffer) runtime managed internalcall
  {
  } // end of method ICorDebugRegisterSet::SetRegisters

  .method public hidebysig newslot abstract virtual 
          instance void  GetThreadContext([in] uint32 contextSize,
                                          [in] native int context) runtime managed internalcall
  {
    .param [2]
    .custom instance void [mscorlib]System.Runtime.InteropServices.ComAliasNameAttribute::.ctor(string) = ( 01 00 05 42 59 54 45 2A 00 00 )                   // ...BYTE*..
  } // end of method ICorDebugRegisterSet::GetThreadContext

  .method public hidebysig newslot abstract virtual 
          instance void  SetThreadContext([in] uint32 contextSize,
                                          [in] native int context) runtime managed internalcall
  {
    .param [2]
    .custom instance void [mscorlib]System.Runtime.InteropServices.ComAliasNameAttribute::.ctor(string) = ( 01 00 05 42 59 54 45 2A 00 00 )                   // ...BYTE*..
  } // end of method ICorDebugRegisterSet::SetThreadContext

} // end of class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugRegisterSet

.class public auto ansi sealed Microsoft.Samples.Debugging.CorDebug.NativeApi.CorDebugChainReason
       extends [mscorlib]System.Enum
{
  .field public specialname rtspecialname int32 value__
  .field public static literal valuetype Microsoft.Samples.Debugging.CorDebug.NativeApi.CorDebugChainReason CHAIN_NONE = int32(0x00000000)
  .field public static literal valuetype Microsoft.Samples.Debugging.CorDebug.NativeApi.CorDebugChainReason CHAIN_CLASS_INIT = int32(0x00000001)
  .field public static literal valuetype Microsoft.Samples.Debugging.CorDebug.NativeApi.CorDebugChainReason CHAIN_EXCEPTION_FILTER = int32(0x00000002)
  .field public static literal valuetype Microsoft.Samples.Debugging.CorDebug.NativeApi.CorDebugChainReason CHAIN_SECURITY = int32(0x00000004)
  .field public static literal valuetype Microsoft.Samples.Debugging.CorDebug.NativeApi.CorDebugChainReason CHAIN_CONTEXT_POLICY = int32(0x00000008)
  .field public static literal valuetype Microsoft.Samples.Debugging.CorDebug.NativeApi.CorDebugChainReason CHAIN_INTERCEPTION = int32(0x00000010)
  .field public static literal valuetype Microsoft.Samples.Debugging.CorDebug.NativeApi.CorDebugChainReason CHAIN_PROCESS_START = int32(0x00000020)
  .field public static literal valuetype Microsoft.Samples.Debugging.CorDebug.NativeApi.CorDebugChainReason CHAIN_THREAD_START = int32(0x00000040)
  .field public static literal valuetype Microsoft.Samples.Debugging.CorDebug.NativeApi.CorDebugChainReason CHAIN_ENTER_MANAGED = int32(0x00000080)
  .field public static literal valuetype Microsoft.Samples.Debugging.CorDebug.NativeApi.CorDebugChainReason CHAIN_ENTER_UNMANAGED = int32(0x00000100)
  .field public static literal valuetype Microsoft.Samples.Debugging.CorDebug.NativeApi.CorDebugChainReason CHAIN_DEBUGGER_EVAL = int32(0x00000200)
  .field public static literal valuetype Microsoft.Samples.Debugging.CorDebug.NativeApi.CorDebugChainReason CHAIN_CONTEXT_SWITCH = int32(0x00000400)
  .field public static literal valuetype Microsoft.Samples.Debugging.CorDebug.NativeApi.CorDebugChainReason CHAIN_FUNC_EVAL = int32(0x00000800)
} // end of class Microsoft.Samples.Debugging.CorDebug.NativeApi.CorDebugChainReason

.class interface public abstract auto ansi import Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugEval
{
  .custom instance void [mscorlib]System.Runtime.InteropServices.InterfaceTypeAttribute::.ctor(int16) = ( 01 00 01 00 00 00 ) 
  .custom instance void [mscorlib]System.Runtime.InteropServices.GuidAttribute::.ctor(string) = ( 01 00 24 43 43 37 42 43 41 46 36 2D 38 41 36 38   // ..$CC7BCAF6-8A68
                                                                                                  2D 31 31 44 32 2D 39 38 33 43 2D 30 30 30 30 46   // -11D2-983C-0000F
                                                                                                  38 30 38 33 34 32 44 00 00 )                      // 808342D..
  .method public hidebysig newslot abstract virtual 
          instance void  CallFunction([in] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugFunction  marshal( interface) pFunction,
                                      [in] uint32 nArgs,
                                      [in] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugValue[]  marshal( [ +1 ] ) ppArgs) runtime managed internalcall
  {
  } // end of method ICorDebugEval::CallFunction

  .method public hidebysig newslot abstract virtual 
          instance void  NewObject([in] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugFunction  marshal( interface) pConstructor,
                                   [in] uint32 nArgs,
                                   [in] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugValue[]  marshal( [ +1 ] ) ppArgs) runtime managed internalcall
  {
  } // end of method ICorDebugEval::NewObject

  .method public hidebysig newslot abstract virtual 
          instance void  NewObjectNoConstructor([in] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugClass  marshal( interface) pClass) runtime managed internalcall
  {
  } // end of method ICorDebugEval::NewObjectNoConstructor

  .method public hidebysig newslot abstract virtual 
          instance void  NewString([in] string  marshal( lpwstr) 'string') runtime managed internalcall
  {
  } // end of method ICorDebugEval::NewString

  .method public hidebysig newslot abstract virtual 
          instance void  NewArray([in] valuetype Microsoft.Samples.Debugging.CorDebug.NativeApi.CorElementType elementType,
                                  [in] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugClass  marshal( interface) pElementClass,
                                  [in] uint32 rank,
                                  [in] uint32& dims,
                                  [in] uint32& lowBounds) runtime managed internalcall
  {
  } // end of method ICorDebugEval::NewArray

  .method public hidebysig newslot abstract virtual 
          instance void  IsActive([out] int32& pbActive) runtime managed internalcall
  {
  } // end of method ICorDebugEval::IsActive

  .method public hidebysig newslot abstract virtual 
          instance void  Abort() runtime managed internalcall
  {
  } // end of method ICorDebugEval::Abort

  .method public hidebysig newslot abstract virtual 
          instance void  GetResult([out] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugValue&  marshal( interface) ppResult) runtime managed internalcall
  {
  } // end of method ICorDebugEval::GetResult

  .method public hidebysig newslot abstract virtual 
          instance void  GetThread([out] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugThread&  marshal( interface) ppThread) runtime managed internalcall
  {
  } // end of method ICorDebugEval::GetThread

  .method public hidebysig newslot abstract virtual 
          instance void  CreateValue([in] valuetype Microsoft.Samples.Debugging.CorDebug.NativeApi.CorElementType elementType,
                                     [in] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugClass  marshal( interface) pElementClass,
                                     [out] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugValue&  marshal( interface) ppValue) runtime managed internalcall
  {
  } // end of method ICorDebugEval::CreateValue

} // end of class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugEval

.class interface public abstract auto ansi import Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugThreadEnum
       implements Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugEnum
{
  .custom instance void [mscorlib]System.Runtime.InteropServices.ComConversionLossAttribute::.ctor() = ( 01 00 00 00 ) 
  .custom instance void [mscorlib]System.Runtime.InteropServices.GuidAttribute::.ctor(string) = ( 01 00 24 43 43 37 42 43 42 30 36 2D 38 41 36 38   // ..$CC7BCB06-8A68
                                                                                                  2D 31 31 44 32 2D 39 38 33 43 2D 30 30 30 30 46   // -11D2-983C-0000F
                                                                                                  38 30 38 33 34 32 44 00 00 )                      // 808342D..
  .custom instance void [mscorlib]System.Runtime.InteropServices.InterfaceTypeAttribute::.ctor(int16) = ( 01 00 01 00 00 00 ) 
  .method public hidebysig newslot abstract virtual 
          instance void  Skip([in] uint32 celt) runtime managed internalcall
  {
  } // end of method ICorDebugThreadEnum::Skip

  .method public hidebysig newslot abstract virtual 
          instance void  Reset() runtime managed internalcall
  {
  } // end of method ICorDebugThreadEnum::Reset

  .method public hidebysig newslot abstract virtual 
          instance void  Clone([out] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugEnum&  marshal( interface) ppEnum) runtime managed internalcall
  {
  } // end of method ICorDebugThreadEnum::Clone

  .method public hidebysig newslot abstract virtual 
          instance void  GetCount([out] uint32& pcelt) runtime managed internalcall
  {
  } // end of method ICorDebugThreadEnum::GetCount

  .method public hidebysig newslot abstract virtual 
          instance int32  Next([in] uint32 celt,
                              [out] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugThread[] marshal([ ]) threads,
                              [out] uint32& pceltFetched) runtime managed internalcall preservesig
  {
  } // end of method ICorDebugThreadEnum::Next

} // end of class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugThreadEnum

.class interface public abstract auto ansi import Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugErrorInfoEnum
       implements Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugEnum
{
  .custom instance void [mscorlib]System.Runtime.InteropServices.GuidAttribute::.ctor(string) = ( 01 00 24 46 30 45 31 38 38 30 39 2D 37 32 42 35   // ..$F0E18809-72B5
                                                                                                  2D 31 31 44 32 2D 39 37 36 46 2D 30 30 41 30 43   // -11D2-976F-00A0C
                                                                                                  39 42 34 44 35 30 43 00 00 )                      // 9B4D50C..
  .custom instance void [mscorlib]System.Runtime.InteropServices.InterfaceTypeAttribute::.ctor(int16) = ( 01 00 01 00 00 00 ) 
  .custom instance void [mscorlib]System.Runtime.InteropServices.ComConversionLossAttribute::.ctor() = ( 01 00 00 00 ) 
  .method public hidebysig newslot abstract virtual 
          instance void  Skip([in] uint32 celt) runtime managed internalcall
  {
  } // end of method ICorDebugErrorInfoEnum::Skip

  .method public hidebysig newslot abstract virtual 
          instance void  Reset() runtime managed internalcall
  {
  } // end of method ICorDebugErrorInfoEnum::Reset

  .method public hidebysig newslot abstract virtual 
          instance void  Clone([out] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugEnum&  marshal( interface) ppEnum) runtime managed internalcall
  {
  } // end of method ICorDebugErrorInfoEnum::Clone

  .method public hidebysig newslot abstract virtual 
          instance void  GetCount([out] uint32& pcelt) runtime managed internalcall
  {
  } // end of method ICorDebugErrorInfoEnum::GetCount

  .method public hidebysig newslot abstract virtual 
          instance int32 Next([in] uint32 celt,
                              [out] native int errors,
                              [out] uint32& pceltFetched) runtime managed internalcall preservesig
  {
	// This is deprecated so we don't bother supporting ICorDebugEditAndContinueErrorInfo
    .param [2]
    .custom instance void [mscorlib]System.Runtime.InteropServices.ComAliasNameAttribute::.ctor(string) = ( 01 00 23 49 43 6F 72 44 65 62 75 67 45 64 69 74   // ..#ICorDebugEdit
                                                                                                            41 6E 64 43 6F 6E 74 69 6E 75 65 45 72 72 6F 72   // AndContinueError
                                                                                                            49 6E 66 6F 2A 2A 00 00 )                         // Info**..
  } // end of method ICorDebugErrorInfoEnum::Next

} // end of class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugErrorInfoEnum

.class interface public abstract auto ansi import Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugAssemblyEnum
       implements Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugEnum
{
  .custom instance void [mscorlib]System.Runtime.InteropServices.InterfaceTypeAttribute::.ctor(int16) = ( 01 00 01 00 00 00 ) 
  .custom instance void [mscorlib]System.Runtime.InteropServices.ComConversionLossAttribute::.ctor() = ( 01 00 00 00 ) 
  .custom instance void [mscorlib]System.Runtime.InteropServices.GuidAttribute::.ctor(string) = ( 01 00 24 34 41 32 41 31 45 43 39 2D 38 35 45 43   // ..$4A2A1EC9-85EC
                                                                                                  2D 34 42 46 42 2D 39 46 31 35 2D 41 38 39 46 44   // -4BFB-9F15-A89FD
                                                                                                  46 45 30 46 45 38 33 00 00 )                      // FE0FE83..
  .method public hidebysig newslot abstract virtual 
          instance void  Skip([in] uint32 celt) runtime managed internalcall
  {
  } // end of method ICorDebugAssemblyEnum::Skip

  .method public hidebysig newslot abstract virtual 
          instance void  Reset() runtime managed internalcall
  {
  } // end of method ICorDebugAssemblyEnum::Reset

  .method public hidebysig newslot abstract virtual 
          instance void  Clone([out] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugEnum&  marshal( interface) ppEnum) runtime managed internalcall
  {
  } // end of method ICorDebugAssemblyEnum::Clone

  .method public hidebysig newslot abstract virtual 
          instance void  GetCount([out] uint32& pcelt) runtime managed internalcall
  {
  } // end of method ICorDebugAssemblyEnum::GetCount

  .method public hidebysig newslot abstract virtual 
          instance int32  Next([in] uint32 celt,
                              [out] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugAssembly[] marshal([ ]) values,
                              [out] uint32& pceltFetched) runtime managed internalcall preservesig
  {
  } // end of method ICorDebugAssemblyEnum::Next

} // end of class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugAssemblyEnum

.class interface public abstract auto ansi import Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugBreakpointEnum
       implements Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugEnum
{
  .custom instance void [mscorlib]System.Runtime.InteropServices.ComConversionLossAttribute::.ctor() = ( 01 00 00 00 ) 
  .custom instance void [mscorlib]System.Runtime.InteropServices.GuidAttribute::.ctor(string) = ( 01 00 24 43 43 37 42 43 42 30 33 2D 38 41 36 38   // ..$CC7BCB03-8A68
                                                                                                  2D 31 31 44 32 2D 39 38 33 43 2D 30 30 30 30 46   // -11D2-983C-0000F
                                                                                                  38 30 38 33 34 32 44 00 00 )                      // 808342D..
  .custom instance void [mscorlib]System.Runtime.InteropServices.InterfaceTypeAttribute::.ctor(int16) = ( 01 00 01 00 00 00 ) 
  .method public hidebysig newslot abstract virtual 
          instance void  Skip([in] uint32 celt) runtime managed internalcall
  {
  } // end of method ICorDebugBreakpointEnum::Skip

  .method public hidebysig newslot abstract virtual 
          instance void  Reset() runtime managed internalcall
  {
  } // end of method ICorDebugBreakpointEnum::Reset

  .method public hidebysig newslot abstract virtual 
          instance void  Clone([out] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugEnum&  marshal( interface) ppEnum) runtime managed internalcall
  {
  } // end of method ICorDebugBreakpointEnum::Clone

  .method public hidebysig newslot abstract virtual 
          instance void  GetCount([out] uint32& pcelt) runtime managed internalcall
  {
  } // end of method ICorDebugBreakpointEnum::GetCount

  .method public hidebysig newslot abstract virtual 
          instance int32  Next([in] uint32 celt,
                              [out] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugBreakpoint[] marshal([ ]) breakpoints,
                              [out] uint32& pceltFetched) runtime managed internalcall preservesig
  {
  } // end of method ICorDebugBreakpointEnum::Next

} // end of class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugBreakpointEnum

.class interface public abstract auto ansi import Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugStepperEnum
       implements Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugEnum
{
  .custom instance void [mscorlib]System.Runtime.InteropServices.ComConversionLossAttribute::.ctor() = ( 01 00 00 00 ) 
  .custom instance void [mscorlib]System.Runtime.InteropServices.GuidAttribute::.ctor(string) = ( 01 00 24 43 43 37 42 43 42 30 34 2D 38 41 36 38   // ..$CC7BCB04-8A68
                                                                                                  2D 31 31 44 32 2D 39 38 33 43 2D 30 30 30 30 46   // -11D2-983C-0000F
                                                                                                  38 30 38 33 34 32 44 00 00 )                      // 808342D..
  .custom instance void [mscorlib]System.Runtime.InteropServices.InterfaceTypeAttribute::.ctor(int16) = ( 01 00 01 00 00 00 ) 
  .method public hidebysig newslot abstract virtual 
          instance void  Skip([in] uint32 celt) runtime managed internalcall
  {
  } // end of method ICorDebugStepperEnum::Skip

  .method public hidebysig newslot abstract virtual 
          instance void  Reset() runtime managed internalcall
  {
  } // end of method ICorDebugStepperEnum::Reset

  .method public hidebysig newslot abstract virtual 
          instance void  Clone([out] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugEnum&  marshal( interface) ppEnum) runtime managed internalcall
  {
  } // end of method ICorDebugStepperEnum::Clone

  .method public hidebysig newslot abstract virtual 
          instance void  GetCount([out] uint32& pcelt) runtime managed internalcall
  {
  } // end of method ICorDebugStepperEnum::GetCount

  .method public hidebysig newslot abstract virtual 
          instance int32  Next([in] uint32 celt,
                              [out] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugStepper[] marshal([ ]) steppers,
                              [out] uint32& pceltFetched) runtime managed internalcall preservesig
  {
  } // end of method ICorDebugStepperEnum::Next

} // end of class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugStepperEnum

.class public auto ansi sealed Microsoft.Samples.Debugging.CorDebug.NativeApi.CorDebugStepReason
       extends [mscorlib]System.Enum
{
  .field public specialname rtspecialname int32 value__
  .field public static literal valuetype Microsoft.Samples.Debugging.CorDebug.NativeApi.CorDebugStepReason STEP_NORMAL = int32(0x00000000)
  .field public static literal valuetype Microsoft.Samples.Debugging.CorDebug.NativeApi.CorDebugStepReason STEP_RETURN = int32(0x00000001)
  .field public static literal valuetype Microsoft.Samples.Debugging.CorDebug.NativeApi.CorDebugStepReason STEP_CALL = int32(0x00000002)
  .field public static literal valuetype Microsoft.Samples.Debugging.CorDebug.NativeApi.CorDebugStepReason STEP_EXCEPTION_FILTER = int32(0x00000003)
  .field public static literal valuetype Microsoft.Samples.Debugging.CorDebug.NativeApi.CorDebugStepReason STEP_EXCEPTION_HANDLER = int32(0x00000004)
  .field public static literal valuetype Microsoft.Samples.Debugging.CorDebug.NativeApi.CorDebugStepReason STEP_INTERCEPT = int32(0x00000005)
  .field public static literal valuetype Microsoft.Samples.Debugging.CorDebug.NativeApi.CorDebugStepReason STEP_EXIT = int32(0x00000006)
} // end of class Microsoft.Samples.Debugging.CorDebug.NativeApi.CorDebugStepReason

.class interface public abstract auto ansi import Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugUnmanagedCallback
{
  .custom instance void [mscorlib]System.Runtime.InteropServices.GuidAttribute::.ctor(string) = ( 01 00 24 35 32 36 33 45 39 30 39 2D 38 43 42 35   // ..$5263E909-8CB5
                                                                                                  2D 31 31 44 33 2D 42 44 32 46 2D 30 30 30 30 46   // -11D3-BD2F-0000F
                                                                                                  38 30 38 34 39 42 44 00 00 )                      // 80849BD..
  .custom instance void [mscorlib]System.Runtime.InteropServices.InterfaceTypeAttribute::.ctor(int16) = ( 01 00 01 00 00 00 ) 
  .method public hidebysig newslot abstract virtual 
          instance void  DebugEvent([in] class Microsoft.Samples.Debugging.CorDebug.NativeApi.DEBUG_EVENT pDebugEvent,
                                    [in] int32 fOutOfBand) runtime managed internalcall
  {
  } // end of method ICorDebugUnmanagedCallback::DebugEvent

} // end of class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugUnmanagedCallback

.class public sequential ansi sealed beforefieldinit Microsoft.Samples.Debugging.CorDebug.NativeApi._SECURITY_ATTRIBUTES
       extends [mscorlib]System.ValueType
{
  .pack 4
  .size 0
  .field public uint32 nLength
  .field public native int lpSecurityDescriptor
  .field public int32 bInheritHandle
} // end of class Microsoft.Samples.Debugging.CorDebug.NativeApi._SECURITY_ATTRIBUTES

.class public auto ansi sealed Microsoft.Samples.Debugging.CorDebug.NativeApi.CorDebugCreateProcessFlags
       extends [mscorlib]System.Enum
{
  .field public specialname rtspecialname int32 value__
  .field public static literal valuetype Microsoft.Samples.Debugging.CorDebug.NativeApi.CorDebugCreateProcessFlags DEBUG_NO_SPECIAL_OPTIONS = int32(0x00000000)
} // end of class Microsoft.Samples.Debugging.CorDebug.NativeApi.CorDebugCreateProcessFlags

.class interface public abstract auto ansi import Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugProcessEnum
       implements Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugEnum
{
  .custom instance void [mscorlib]System.Runtime.InteropServices.GuidAttribute::.ctor(string) = ( 01 00 24 43 43 37 42 43 42 30 35 2D 38 41 36 38   // ..$CC7BCB05-8A68
                                                                                                  2D 31 31 44 32 2D 39 38 33 43 2D 30 30 30 30 46   // -11D2-983C-0000F
                                                                                                  38 30 38 33 34 32 44 00 00 )                      // 808342D..
  .custom instance void [mscorlib]System.Runtime.InteropServices.ComConversionLossAttribute::.ctor() = ( 01 00 00 00 ) 
  .custom instance void [mscorlib]System.Runtime.InteropServices.InterfaceTypeAttribute::.ctor(int16) = ( 01 00 01 00 00 00 ) 
  .method public hidebysig newslot abstract virtual 
          instance void  Skip([in] uint32 celt) runtime managed internalcall
  {
  } // end of method ICorDebugProcessEnum::Skip

  .method public hidebysig newslot abstract virtual 
          instance void  Reset() runtime managed internalcall
  {
  } // end of method ICorDebugProcessEnum::Reset

  .method public hidebysig newslot abstract virtual 
          instance void  Clone([out] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugEnum&  marshal( interface) ppEnum) runtime managed internalcall
  {
  } // end of method ICorDebugProcessEnum::Clone

  .method public hidebysig newslot abstract virtual 
          instance void  GetCount([out] uint32& pcelt) runtime managed internalcall
  {
  } // end of method ICorDebugProcessEnum::GetCount

  .method public hidebysig newslot abstract virtual 
          instance int32  Next([in] uint32 celt,
                              [out] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugProcess[] marshal([ ]) processes,
                              [out] uint32& pceltFetched) runtime managed internalcall preservesig
  {
  } // end of method ICorDebugProcessEnum::Next

} // end of class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugProcessEnum

.class public auto ansi import Microsoft.Samples.Debugging.CorDebug.NativeApi.EmbeddedCLRCorDebugClass
       extends [mscorlib]System.Object
       implements Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebug,
                  Microsoft.Samples.Debugging.CorDebug.NativeApi.EmbeddedCLRCorDebug
{
  .custom instance void [mscorlib]System.Runtime.InteropServices.ClassInterfaceAttribute::.ctor(int16) = ( 01 00 00 00 00 00 ) 
  .custom instance void [mscorlib]System.Runtime.InteropServices.GuidAttribute::.ctor(string) = ( 01 00 24 32 31 31 46 31 32 35 34 2D 42 43 37 45   // ..$211F1254-BC7E
                                                                                                  2D 34 41 46 35 2D 42 39 41 41 2D 30 36 37 33 30   // -4AF5-B9AA-06730
                                                                                                  38 44 38 33 44 44 31 00 00 )                      // 8D83DD1..
  .custom instance void [mscorlib]System.Runtime.InteropServices.TypeLibTypeAttribute::.ctor(int16) = ( 01 00 02 00 00 00 ) 
  .method public specialname rtspecialname 
          instance void  .ctor() runtime managed internalcall
  {
  } // end of method EmbeddedCLRCorDebugClass::.ctor

  .method public hidebysig newslot virtual 
          instance void  Initialize() runtime managed internalcall
  {
    .override Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebug::Initialize
  } // end of method EmbeddedCLRCorDebugClass::Initialize

  .method public hidebysig newslot virtual 
          instance void  Terminate() runtime managed internalcall
  {
    .override Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebug::Terminate
  } // end of method EmbeddedCLRCorDebugClass::Terminate

  .method public hidebysig newslot virtual 
          instance void  SetManagedHandler([in] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugManagedCallback  marshal( interface) pCallback) runtime managed internalcall
  {
    .override Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebug::SetManagedHandler
  } // end of method EmbeddedCLRCorDebugClass::SetManagedHandler

  .method public hidebysig newslot virtual 
          instance void  SetUnmanagedHandler([in] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugUnmanagedCallback  marshal( interface) pCallback) runtime managed internalcall
  {
    .override Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebug::SetUnmanagedHandler
  } // end of method EmbeddedCLRCorDebugClass::SetUnmanagedHandler

  .method public hidebysig newslot virtual 
          instance void  CreateProcess([in] string  marshal( lpwstr) lpApplicationName,
                                       [in] string  marshal( lpwstr) lpCommandLine,
                                       [in] class Microsoft.Samples.Debugging.CorDebug.NativeApi.SECURITY_ATTRIBUTES lpProcessAttributes,
                                       [in] class Microsoft.Samples.Debugging.CorDebug.NativeApi.SECURITY_ATTRIBUTES lpThreadAttributes,
                                       [in] int32 bInheritHandles,
                                       [in] uint32 dwCreationFlags,
                                       [in] native int lpEnvironment,
                                       [in] string  marshal( lpwstr) lpCurrentDirectory,
                                       [in] class Microsoft.Samples.Debugging.CorDebug.NativeApi.STARTUPINFO lpStartupInfo,
                                       [in] class Microsoft.Samples.Debugging.CorDebug.NativeApi.PROCESS_INFORMATION lpProcessInformation,
                                       [in] valuetype Microsoft.Samples.Debugging.CorDebug.NativeApi.CorDebugCreateProcessFlags debuggingFlags,
                                       [out] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugProcess&  marshal( interface) ppProcess) runtime managed internalcall
  {
    .param [9]
    .custom instance void [mscorlib]System.Runtime.InteropServices.ComAliasNameAttribute::.ctor(string) = ( 01 00 30 4D 69 63 72 6F 73 6F 66 74 2E 44 65 62   // ..0Microsoft.Deb
                                                                                                            75 67 67 69 6E 67 2E 43 6F 72 44 65 62 75 67 2E   // ugging.CorDebug.
                                                                                                            4E 61 74 69 76 65 41 70 69 2E 55 4C 4F 4E 47 5F   // NativeApi.ULONG_
                                                                                                            50 54 52 00 00 )                                  // PTR..
    .param [10]
    .custom instance void [mscorlib]System.Runtime.InteropServices.ComAliasNameAttribute::.ctor(string) = ( 01 00 30 4D 69 63 72 6F 73 6F 66 74 2E 44 65 62   // ..0Microsoft.Deb
                                                                                                            75 67 67 69 6E 67 2E 43 6F 72 44 65 62 75 67 2E   // ugging.CorDebug.
                                                                                                            4E 61 74 69 76 65 41 70 69 2E 55 4C 4F 4E 47 5F   // NativeApi.ULONG_
                                                                                                            50 54 52 00 00 )                                  // PTR..
    .override Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebug::CreateProcess
  } // end of method EmbeddedCLRCorDebugClass::CreateProcess

  .method public hidebysig newslot virtual 
          instance void  DebugActiveProcess([in] uint32 id,
                                            [in] int32 win32Attach,
                                            [out] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugProcess&  marshal( interface) ppProcess) runtime managed internalcall
  {
    .override Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebug::DebugActiveProcess
  } // end of method EmbeddedCLRCorDebugClass::DebugActiveProcess

  .method public hidebysig newslot virtual 
          instance void  EnumerateProcesses([out] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugProcessEnum&  marshal( interface) ppProcess) runtime managed internalcall
  {
    .override Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebug::EnumerateProcesses
  } // end of method EmbeddedCLRCorDebugClass::EnumerateProcesses

  .method public hidebysig newslot virtual 
          instance void  GetProcess([in] uint32 dwProcessId,
                                    [out] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugProcess&  marshal( interface) ppProcess) runtime managed internalcall
  {
    .override Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebug::GetProcess
  } // end of method EmbeddedCLRCorDebugClass::GetProcess

  .method public hidebysig newslot virtual 
          instance void  CanLaunchOrAttach([in] uint32 dwProcessId,
                                           [in] int32 win32DebuggingEnabled) runtime managed internalcall
  {
    .override Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebug::CanLaunchOrAttach
  } // end of method EmbeddedCLRCorDebugClass::CanLaunchOrAttach

} // end of class Microsoft.Samples.Debugging.CorDebug.NativeApi.EmbeddedCLRCorDebugClass

.class interface public abstract auto ansi import Microsoft.Samples.Debugging.CorDebug.NativeApi.EmbeddedCLRCorDebug
       implements Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebug
{
  .custom instance void [mscorlib]System.Runtime.InteropServices.GuidAttribute::.ctor(string) = ( 01 00 24 33 44 36 46 35 46 36 31 2D 37 35 33 38   // ..$3D6F5F61-7538
                                                                                                  2D 31 31 44 33 2D 38 44 35 42 2D 30 30 31 30 34   // -11D3-8D5B-00104
                                                                                                  42 33 35 45 37 45 46 00 00 )                      // B35E7EF..
  .custom instance void [mscorlib]System.Runtime.InteropServices.CoClassAttribute::.ctor(class [mscorlib]System.Type) = ( 01 00 47 4D 69 63 72 6F 73 6F 66 74 2E 53 61 6D   // ..GMicrosoft.Sam
                                                                                                                          70 6C 65 73 2E 44 65 62 75 67 67 69 6E 67 2E 43   // ples.Debugging.C
                                                                                                                          6F 72 44 65 62 75 67 2E 4E 61 74 69 76 65 41 70   // orDebug.NativeAp
                                                                                                                          69 2E 45 6D 62 65 64 64 65 64 43 4C 52 43 6F 72   // i.EmbeddedCLRCor
                                                                                                                          44 65 62 75 67 43 6C 61 73 73 00 00 )             // DebugClass..
} // end of class Microsoft.Samples.Debugging.CorDebug.NativeApi.EmbeddedCLRCorDebug

.class public auto ansi import Microsoft.Samples.Debugging.CorDebug.NativeApi.CorDebugManagerClass
       extends [mscorlib]System.Object
       implements Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebug,
                  Microsoft.Samples.Debugging.CorDebug.NativeApi.CorDebugManager
{
  .custom instance void [mscorlib]System.Runtime.InteropServices.GuidAttribute::.ctor(string) = ( 01 00 24 42 37 36 42 31 37 45 46 2D 31 36 46 41   // ..$B76B17EF-16FA
                                                                                                  2D 34 33 41 33 2D 42 41 42 46 2D 44 42 36 45 35   // -43A3-BABF-DB6E5
                                                                                                  39 34 33 39 45 42 30 00 00 )                      // 9439EB0..
  .custom instance void [mscorlib]System.Runtime.InteropServices.ClassInterfaceAttribute::.ctor(int16) = ( 01 00 00 00 00 00 ) 
  .custom instance void [mscorlib]System.Runtime.InteropServices.TypeLibTypeAttribute::.ctor(int16) = ( 01 00 02 00 00 00 ) 
  .method public specialname rtspecialname 
          instance void  .ctor() runtime managed internalcall
  {
  } // end of method CorDebugManagerClass::.ctor

  .method public hidebysig newslot virtual 
          instance void  Initialize() runtime managed internalcall
  {
    .override Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebug::Initialize
  } // end of method CorDebugManagerClass::Initialize

  .method public hidebysig newslot virtual 
          instance void  Terminate() runtime managed internalcall
  {
    .override Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebug::Terminate
  } // end of method CorDebugManagerClass::Terminate

  .method public hidebysig newslot virtual 
          instance void  SetManagedHandler([in] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugManagedCallback  marshal( interface) pCallback) runtime managed internalcall
  {
    .override Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebug::SetManagedHandler
  } // end of method CorDebugManagerClass::SetManagedHandler

  .method public hidebysig newslot virtual 
          instance void  SetUnmanagedHandler([in] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugUnmanagedCallback  marshal( interface) pCallback) runtime managed internalcall
  {
    .override Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebug::SetUnmanagedHandler
  } // end of method CorDebugManagerClass::SetUnmanagedHandler

  .method public hidebysig newslot virtual 
          instance void  CreateProcess([in] string  marshal( lpwstr) lpApplicationName,
                                       [in] string  marshal( lpwstr) lpCommandLine,
                                       [in] class Microsoft.Samples.Debugging.CorDebug.NativeApi.SECURITY_ATTRIBUTES lpProcessAttributes,
                                       [in] class Microsoft.Samples.Debugging.CorDebug.NativeApi.SECURITY_ATTRIBUTES lpThreadAttributes,
                                       [in] int32 bInheritHandles,
                                       [in] uint32 dwCreationFlags,
                                       [in] native int lpEnvironment,
                                       [in] string  marshal( lpwstr) lpCurrentDirectory,
                                       [in] class Microsoft.Samples.Debugging.CorDebug.NativeApi.STARTUPINFO lpStartupInfo,
                                       [in] class Microsoft.Samples.Debugging.CorDebug.NativeApi.PROCESS_INFORMATION lpProcessInformation,
                                       [in] valuetype Microsoft.Samples.Debugging.CorDebug.NativeApi.CorDebugCreateProcessFlags debuggingFlags,
                                       [out] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugProcess&  marshal( interface) ppProcess) runtime managed internalcall
  {
    .param [9]
    .custom instance void [mscorlib]System.Runtime.InteropServices.ComAliasNameAttribute::.ctor(string) = ( 01 00 30 4D 69 63 72 6F 73 6F 66 74 2E 44 65 62   // ..0Microsoft.Deb
                                                                                                            75 67 67 69 6E 67 2E 43 6F 72 44 65 62 75 67 2E   // ugging.CorDebug.
                                                                                                            4E 61 74 69 76 65 41 70 69 2E 55 4C 4F 4E 47 5F   // NativeApi.ULONG_
                                                                                                            50 54 52 00 00 )                                  // PTR..
    .param [10]
    .custom instance void [mscorlib]System.Runtime.InteropServices.ComAliasNameAttribute::.ctor(string) = ( 01 00 30 4D 69 63 72 6F 73 6F 66 74 2E 44 65 62   // ..0Microsoft.Deb
                                                                                                            75 67 67 69 6E 67 2E 43 6F 72 44 65 62 75 67 2E   // ugging.CorDebug.
                                                                                                            4E 61 74 69 76 65 41 70 69 2E 55 4C 4F 4E 47 5F   // NativeApi.ULONG_
                                                                                                            50 54 52 00 00 )                                  // PTR..
    .override Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebug::CreateProcess
  } // end of method CorDebugManagerClass::CreateProcess

  .method public hidebysig newslot virtual 
          instance void  DebugActiveProcess([in] uint32 id,
                                            [in] int32 win32Attach,
                                            [out] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugProcess&  marshal( interface) ppProcess) runtime managed internalcall
  {
    .override Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebug::DebugActiveProcess
  } // end of method CorDebugManagerClass::DebugActiveProcess

  .method public hidebysig newslot virtual 
          instance void  EnumerateProcesses([out] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugProcessEnum&  marshal( interface) ppProcess) runtime managed internalcall
  {
    .override Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebug::EnumerateProcesses
  } // end of method CorDebugManagerClass::EnumerateProcesses

  .method public hidebysig newslot virtual 
          instance void  GetProcess([in] uint32 dwProcessId,
                                    [out] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugProcess&  marshal( interface) ppProcess) runtime managed internalcall
  {
    .override Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebug::GetProcess
  } // end of method CorDebugManagerClass::GetProcess

  .method public hidebysig newslot virtual 
          instance void  CanLaunchOrAttach([in] uint32 dwProcessId,
                                           [in] int32 win32DebuggingEnabled) runtime managed internalcall
  {
    .override Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebug::CanLaunchOrAttach
  } // end of method CorDebugManagerClass::CanLaunchOrAttach

} // end of class Microsoft.Samples.Debugging.CorDebug.NativeApi.CorDebugManagerClass

.class interface public abstract auto ansi import Microsoft.Samples.Debugging.CorDebug.NativeApi.CorDebugManager
       implements Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebug
{
  .custom instance void [mscorlib]System.Runtime.InteropServices.CoClassAttribute::.ctor(class [mscorlib]System.Type) = ( 01 00 43 4D 69 63 72 6F 73 6F 66 74 2E 53 61 6D   // ..CMicrosoft.Sam
                                                                                                                          70 6C 65 73 2E 44 65 62 75 67 67 69 6E 67 2E 43   // ples.Debugging.C
                                                                                                                          6F 72 44 65 62 75 67 2E 4E 61 74 69 76 65 41 70   // orDebug.NativeAp
                                                                                                                          69 2E 43 6F 72 44 65 62 75 67 4D 61 6E 61 67 65   // i.CorDebugManage
                                                                                                                          72 43 6C 61 73 73 00 00 )                         // rClass..
  .custom instance void [mscorlib]System.Runtime.InteropServices.GuidAttribute::.ctor(string) = ( 01 00 24 33 44 36 46 35 46 36 31 2D 37 35 33 38   // ..$3D6F5F61-7538
                                                                                                  2D 31 31 44 33 2D 38 44 35 42 2D 30 30 31 30 34   // -11D3-8D5B-00104
                                                                                                  42 33 35 45 37 45 46 00 00 )                      // B35E7EF..
} // end of class Microsoft.Samples.Debugging.CorDebug.NativeApi.CorDebugManager

.class interface public abstract auto ansi import Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugReferenceValue
       implements Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugValue
{
  .custom instance void [mscorlib]System.Runtime.InteropServices.GuidAttribute::.ctor(string) = ( 01 00 24 43 43 37 42 43 41 46 39 2D 38 41 36 38   // ..$CC7BCAF9-8A68
                                                                                                  2D 31 31 44 32 2D 39 38 33 43 2D 30 30 30 30 46   // -11D2-983C-0000F
                                                                                                  38 30 38 33 34 32 44 00 00 )                      // 808342D..
  .custom instance void [mscorlib]System.Runtime.InteropServices.InterfaceTypeAttribute::.ctor(int16) = ( 01 00 01 00 00 00 ) 
  .method public hidebysig newslot abstract virtual 
          instance void  GetType([out] valuetype Microsoft.Samples.Debugging.CorDebug.NativeApi.CorElementType& pType) runtime managed internalcall
  {
  } // end of method ICorDebugReferenceValue::GetType

  .method public hidebysig newslot abstract virtual 
          instance void  GetSize([out] uint32& pSize) runtime managed internalcall
  {
  } // end of method ICorDebugReferenceValue::GetSize

  .method public hidebysig newslot abstract virtual 
          instance void  GetAddress([out] uint64& pAddress) runtime managed internalcall
  {
  } // end of method ICorDebugReferenceValue::GetAddress

  .method public hidebysig newslot abstract virtual 
          instance void  CreateBreakpoint([out] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugValueBreakpoint&  marshal( interface) ppBreakpoint) runtime managed internalcall
  {
  } // end of method ICorDebugReferenceValue::CreateBreakpoint

  .method public hidebysig newslot abstract virtual 
          instance void  IsNull([out] int32& pbNull) runtime managed internalcall
  {
  } // end of method ICorDebugReferenceValue::IsNull

  .method public hidebysig newslot abstract virtual 
          instance void  GetValue([out] uint64& pValue) runtime managed internalcall
  {
  } // end of method ICorDebugReferenceValue::GetValue

  .method public hidebysig newslot abstract virtual 
          instance void  SetValue([in] uint64 'value') runtime managed internalcall
  {
  } // end of method ICorDebugReferenceValue::SetValue

  .method public hidebysig newslot abstract virtual 
          instance void  Dereference([out] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugValue&  marshal( interface) ppValue) runtime managed internalcall
  {
  } // end of method ICorDebugReferenceValue::Dereference

  .method public hidebysig newslot abstract virtual 
          instance void  DereferenceStrong([out] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugValue&  marshal( interface) ppValue) runtime managed internalcall
  {
  } // end of method ICorDebugReferenceValue::DereferenceStrong

} // end of class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugReferenceValue

.class interface public abstract auto ansi import Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugHeapValue
       implements Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugValue
{
  .custom instance void [mscorlib]System.Runtime.InteropServices.InterfaceTypeAttribute::.ctor(int16) = ( 01 00 01 00 00 00 ) 
  .custom instance void [mscorlib]System.Runtime.InteropServices.GuidAttribute::.ctor(string) = ( 01 00 24 43 43 37 42 43 41 46 41 2D 38 41 36 38   // ..$CC7BCAFA-8A68
                                                                                                  2D 31 31 44 32 2D 39 38 33 43 2D 30 30 30 30 46   // -11D2-983C-0000F
                                                                                                  38 30 38 33 34 32 44 00 00 )                      // 808342D..
  .method public hidebysig newslot abstract virtual 
          instance void  GetType([out] valuetype Microsoft.Samples.Debugging.CorDebug.NativeApi.CorElementType& pType) runtime managed internalcall
  {
  } // end of method ICorDebugHeapValue::GetType

  .method public hidebysig newslot abstract virtual 
          instance void  GetSize([out] uint32& pSize) runtime managed internalcall
  {
  } // end of method ICorDebugHeapValue::GetSize

  .method public hidebysig newslot abstract virtual 
          instance void  GetAddress([out] uint64& pAddress) runtime managed internalcall
  {
  } // end of method ICorDebugHeapValue::GetAddress

  .method public hidebysig newslot abstract virtual 
          instance void  CreateBreakpoint([out] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugValueBreakpoint&  marshal( interface) ppBreakpoint) runtime managed internalcall
  {
  } // end of method ICorDebugHeapValue::CreateBreakpoint

  .method public hidebysig newslot abstract virtual 
          instance void  IsValid([out] int32& pbValid) runtime managed internalcall
  {
  } // end of method ICorDebugHeapValue::IsValid

  .method public hidebysig newslot abstract virtual 
          instance void  CreateRelocBreakpoint([out] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugValueBreakpoint&  marshal( interface) ppBreakpoint) runtime managed internalcall
  {
  } // end of method ICorDebugHeapValue::CreateRelocBreakpoint

} // end of class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugHeapValue

.class interface public abstract auto ansi import Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugStringValue
       implements Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugHeapValue
{
  .custom instance void [mscorlib]System.Runtime.InteropServices.GuidAttribute::.ctor(string) = ( 01 00 24 43 43 37 42 43 41 46 44 2D 38 41 36 38   // ..$CC7BCAFD-8A68
                                                                                                  2D 31 31 44 32 2D 39 38 33 43 2D 30 30 30 30 46   // -11D2-983C-0000F
                                                                                                  38 30 38 33 34 32 44 00 00 )                      // 808342D..
  .custom instance void [mscorlib]System.Runtime.InteropServices.InterfaceTypeAttribute::.ctor(int16) = ( 01 00 01 00 00 00 ) 
  .custom instance void [mscorlib]System.Runtime.InteropServices.ComConversionLossAttribute::.ctor() = ( 01 00 00 00 ) 
  .method public hidebysig newslot abstract virtual 
          instance void  GetType([out] valuetype Microsoft.Samples.Debugging.CorDebug.NativeApi.CorElementType& pType) runtime managed internalcall
  {
  } // end of method ICorDebugStringValue::GetType

  .method public hidebysig newslot abstract virtual 
          instance void  GetSize([out] uint32& pSize) runtime managed internalcall
  {
  } // end of method ICorDebugStringValue::GetSize

  .method public hidebysig newslot abstract virtual 
          instance void  GetAddress([out] uint64& pAddress) runtime managed internalcall
  {
  } // end of method ICorDebugStringValue::GetAddress

  .method public hidebysig newslot abstract virtual 
          instance void  CreateBreakpoint([out] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugValueBreakpoint&  marshal( interface) ppBreakpoint) runtime managed internalcall
  {
  } // end of method ICorDebugStringValue::CreateBreakpoint

  .method public hidebysig newslot abstract virtual 
          instance void  IsValid([out] int32& pbValid) runtime managed internalcall
  {
  } // end of method ICorDebugStringValue::IsValid

  .method public hidebysig newslot abstract virtual 
          instance void  CreateRelocBreakpoint([out] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugValueBreakpoint&  marshal( interface) ppBreakpoint) runtime managed internalcall
  {
  } // end of method ICorDebugStringValue::CreateRelocBreakpoint

  .method public hidebysig newslot abstract virtual 
          instance void  GetLength([out] uint32& pcchString) runtime managed internalcall
  {
  } // end of method ICorDebugStringValue::GetLength

  .method public hidebysig newslot abstract virtual 
          instance void  GetString([in] uint32 cchString,
                                   [out] uint32& pcchString,
                                   [out] class [mscorlib]System.Text.StringBuilder  marshal( lpwstr) szString) runtime managed internalcall
  {
  } // end of method ICorDebugStringValue::GetString

} // end of class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugStringValue

.class interface public abstract auto ansi import Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugGenericValue
       implements Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugValue
{
  .custom instance void [mscorlib]System.Runtime.InteropServices.InterfaceTypeAttribute::.ctor(int16) = ( 01 00 01 00 00 00 ) 
  .custom instance void [mscorlib]System.Runtime.InteropServices.GuidAttribute::.ctor(string) = ( 01 00 24 43 43 37 42 43 41 46 38 2D 38 41 36 38   // ..$CC7BCAF8-8A68
                                                                                                  2D 31 31 44 32 2D 39 38 33 43 2D 30 30 30 30 46   // -11D2-983C-0000F
                                                                                                  38 30 38 33 34 32 44 00 00 )                      // 808342D..
  .method public hidebysig newslot abstract virtual 
          instance void  GetType([out] valuetype Microsoft.Samples.Debugging.CorDebug.NativeApi.CorElementType& pType) runtime managed internalcall
  {
  } // end of method ICorDebugGenericValue::GetType

  .method public hidebysig newslot abstract virtual 
          instance void  GetSize([out] uint32& pSize) runtime managed internalcall
  {
  } // end of method ICorDebugGenericValue::GetSize

  .method public hidebysig newslot abstract virtual 
          instance void  GetAddress([out] uint64& pAddress) runtime managed internalcall
  {
  } // end of method ICorDebugGenericValue::GetAddress

  .method public hidebysig newslot abstract virtual 
          instance void  CreateBreakpoint([out] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugValueBreakpoint&  marshal( interface) ppBreakpoint) runtime managed internalcall
  {
  } // end of method ICorDebugGenericValue::CreateBreakpoint

  .method public hidebysig newslot abstract virtual 
          instance void  GetValue([out] native int pTo) runtime managed internalcall
  {
  } // end of method ICorDebugGenericValue::GetValue

  .method public hidebysig newslot abstract virtual 
          instance void  SetValue([in] native int pFrom) runtime managed internalcall
  {
  } // end of method ICorDebugGenericValue::SetValue

} // end of class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugGenericValue

.class interface public abstract auto ansi import Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugBoxValue
       implements Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugHeapValue
{
  .custom instance void [mscorlib]System.Runtime.InteropServices.GuidAttribute::.ctor(string) = ( 01 00 24 43 43 37 42 43 41 46 43 2D 38 41 36 38   // ..$CC7BCAFC-8A68
                                                                                                  2D 31 31 44 32 2D 39 38 33 43 2D 30 30 30 30 46   // -11D2-983C-0000F
                                                                                                  38 30 38 33 34 32 44 00 00 )                      // 808342D..
  .custom instance void [mscorlib]System.Runtime.InteropServices.InterfaceTypeAttribute::.ctor(int16) = ( 01 00 01 00 00 00 ) 
  .method public hidebysig newslot abstract virtual 
          instance void  GetType([out] valuetype Microsoft.Samples.Debugging.CorDebug.NativeApi.CorElementType& pType) runtime managed internalcall
  {
  } // end of method ICorDebugBoxValue::GetType

  .method public hidebysig newslot abstract virtual 
          instance void  GetSize([out] uint32& pSize) runtime managed internalcall
  {
  } // end of method ICorDebugBoxValue::GetSize

  .method public hidebysig newslot abstract virtual 
          instance void  GetAddress([out] uint64& pAddress) runtime managed internalcall
  {
  } // end of method ICorDebugBoxValue::GetAddress

  .method public hidebysig newslot abstract virtual 
          instance void  CreateBreakpoint([out] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugValueBreakpoint&  marshal( interface) ppBreakpoint) runtime managed internalcall
  {
  } // end of method ICorDebugBoxValue::CreateBreakpoint

  .method public hidebysig newslot abstract virtual 
          instance void  IsValid([out] int32& pbValid) runtime managed internalcall
  {
  } // end of method ICorDebugBoxValue::IsValid

  .method public hidebysig newslot abstract virtual 
          instance void  CreateRelocBreakpoint([out] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugValueBreakpoint&  marshal( interface) ppBreakpoint) runtime managed internalcall
  {
  } // end of method ICorDebugBoxValue::CreateRelocBreakpoint

  .method public hidebysig newslot abstract virtual 
          instance void  GetObject([out] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugObjectValue&  marshal( interface) ppObject) runtime managed internalcall
  {
  } // end of method ICorDebugBoxValue::GetObject

} // end of class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugBoxValue

.class interface public abstract auto ansi import Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugArrayValue
       implements Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugHeapValue
{
  .custom instance void [mscorlib]System.Runtime.InteropServices.GuidAttribute::.ctor(string) = ( 01 00 24 30 34 30 35 42 30 44 46 2D 41 36 36 30   // ..$0405B0DF-A660
                                                                                                  2D 31 31 44 32 2D 42 44 30 32 2D 30 30 30 30 46   // -11D2-BD02-0000F
                                                                                                  38 30 38 34 39 42 44 00 00 )                      // 80849BD..
  .custom instance void [mscorlib]System.Runtime.InteropServices.InterfaceTypeAttribute::.ctor(int16) = ( 01 00 01 00 00 00 ) 
  .custom instance void [mscorlib]System.Runtime.InteropServices.ComConversionLossAttribute::.ctor() = ( 01 00 00 00 ) 
  .method public hidebysig newslot abstract virtual 
          instance void  GetType([out] valuetype Microsoft.Samples.Debugging.CorDebug.NativeApi.CorElementType& pType) runtime managed internalcall
  {
  } // end of method ICorDebugArrayValue::GetType

  .method public hidebysig newslot abstract virtual 
          instance void  GetSize([out] uint32& pSize) runtime managed internalcall
  {
  } // end of method ICorDebugArrayValue::GetSize

  .method public hidebysig newslot abstract virtual 
          instance void  GetAddress([out] uint64& pAddress) runtime managed internalcall
  {
  } // end of method ICorDebugArrayValue::GetAddress

  .method public hidebysig newslot abstract virtual 
          instance void  CreateBreakpoint([out] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugValueBreakpoint&  marshal( interface) ppBreakpoint) runtime managed internalcall
  {
  } // end of method ICorDebugArrayValue::CreateBreakpoint

  .method public hidebysig newslot abstract virtual 
          instance void  IsValid([out] int32& pbValid) runtime managed internalcall
  {
  } // end of method ICorDebugArrayValue::IsValid

  .method public hidebysig newslot abstract virtual 
          instance void  CreateRelocBreakpoint([out] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugValueBreakpoint&  marshal( interface) ppBreakpoint) runtime managed internalcall
  {
  } // end of method ICorDebugArrayValue::CreateRelocBreakpoint

  .method public hidebysig newslot abstract virtual 
          instance void  GetElementType([out] valuetype Microsoft.Samples.Debugging.CorDebug.NativeApi.CorElementType& pType) runtime managed internalcall
  {
  } // end of method ICorDebugArrayValue::GetElementType

  .method public hidebysig newslot abstract virtual 
          instance void  GetRank([out] uint32& pnRank) runtime managed internalcall
  {
  } // end of method ICorDebugArrayValue::GetRank

  .method public hidebysig newslot abstract virtual 
          instance void  GetCount([out] uint32& pnCount) runtime managed internalcall
  {
  } // end of method ICorDebugArrayValue::GetCount

  .method public hidebysig newslot abstract virtual 
          instance void  GetDimensions([in] uint32 cdim,
                                       [out] uint32[]  marshal([ + 0]) dims) runtime managed internalcall
  {
  } // end of method ICorDebugArrayValue::GetDimensions

  .method public hidebysig newslot abstract virtual 
          instance void  HasBaseIndicies([out] int32& pbHasBaseIndicies) runtime managed internalcall
  {
  } // end of method ICorDebugArrayValue::HasBaseIndicies

  .method public hidebysig newslot abstract virtual 
          instance void  GetBaseIndicies([in] uint32 cdim,
                                         [out] uint32[] marshal([ + 0]) indicies) runtime managed internalcall
  {
  } // end of method ICorDebugArrayValue::GetBaseIndicies

  .method public hidebysig newslot abstract virtual 
          instance void  GetElement([in] uint32 cdim,
                                    [in] int32[] marshal([+0]) indices,
                                    [out] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugValue&  marshal( interface) ppValue) runtime managed internalcall
  {
  } // end of method ICorDebugArrayValue::GetElement

  .method public hidebysig newslot abstract virtual 
          instance void  GetElementAtPosition([in] uint32 nPosition,
                                              [out] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugValue&  marshal( interface) ppValue) runtime managed internalcall
  {
  } // end of method ICorDebugArrayValue::GetElementAtPosition

} // end of class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugArrayValue

.class interface public abstract auto ansi import Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugILFrame
       implements Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugFrame
{
  .custom instance void [mscorlib]System.Runtime.InteropServices.InterfaceTypeAttribute::.ctor(int16) = ( 01 00 01 00 00 00 ) 
  .custom instance void [mscorlib]System.Runtime.InteropServices.GuidAttribute::.ctor(string) = ( 01 00 24 30 33 45 32 36 33 31 31 2D 34 46 37 36   // ..$03E26311-4F76
                                                                                                  2D 31 31 44 33 2D 38 38 43 36 2D 30 30 36 30 39   // -11D3-88C6-00609
                                                                                                  37 39 34 35 34 31 38 00 00 )                      // 7945418..
  .method public hidebysig newslot abstract virtual 
          instance void  GetChain([out] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugChain&  marshal( interface) ppChain) runtime managed internalcall
  {
  } // end of method ICorDebugILFrame::GetChain

  .method public hidebysig newslot abstract virtual 
          instance void  GetCode([out] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugCode&  marshal( interface) ppCode) runtime managed internalcall
  {
  } // end of method ICorDebugILFrame::GetCode

  .method public hidebysig newslot abstract virtual 
          instance void  GetFunction([out] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugFunction&  marshal( interface) ppFunction) runtime managed internalcall
  {
  } // end of method ICorDebugILFrame::GetFunction

  .method public hidebysig newslot abstract virtual 
          instance void  GetFunctionToken([out] uint32& pToken) runtime managed internalcall
  {
  } // end of method ICorDebugILFrame::GetFunctionToken

  .method public hidebysig newslot abstract virtual 
          instance void  GetStackRange([out] uint64& pStart,
                                       [out] uint64& pEnd) runtime managed internalcall
  {
  } // end of method ICorDebugILFrame::GetStackRange

  .method public hidebysig newslot abstract virtual 
          instance void  GetCaller([out] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugFrame&  marshal( interface) ppFrame) runtime managed internalcall
  {
  } // end of method ICorDebugILFrame::GetCaller

  .method public hidebysig newslot abstract virtual 
          instance void  GetCallee([out] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugFrame&  marshal( interface) ppFrame) runtime managed internalcall
  {
  } // end of method ICorDebugILFrame::GetCallee

  .method public hidebysig newslot abstract virtual 
          instance void  CreateStepper([out] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugStepper&  marshal( interface) ppStepper) runtime managed internalcall
  {
  } // end of method ICorDebugILFrame::CreateStepper

  .method public hidebysig newslot abstract virtual 
          instance void  GetIP([out] uint32& pnOffset,
                               [out] valuetype Microsoft.Samples.Debugging.CorDebug.NativeApi.CorDebugMappingResult& pMappingResult) runtime managed internalcall
  {
  } // end of method ICorDebugILFrame::GetIP

  .method public hidebysig newslot abstract virtual 
          instance void  SetIP([in] uint32 nOffset) runtime managed internalcall
  {
  } // end of method ICorDebugILFrame::SetIP

  .method public hidebysig newslot abstract virtual 
          instance void  EnumerateLocalVariables([out] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugValueEnum&  marshal( interface) ppValueEnum) runtime managed internalcall
  {
  } // end of method ICorDebugILFrame::EnumerateLocalVariables

  .method public hidebysig newslot abstract virtual 
          instance void  GetLocalVariable([in] uint32 dwIndex,
                                          [out] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugValue&  marshal( interface) ppValue) runtime managed internalcall
  {
  } // end of method ICorDebugILFrame::GetLocalVariable

  .method public hidebysig newslot abstract virtual 
          instance void  EnumerateArguments([out] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugValueEnum&  marshal( interface) ppValueEnum) runtime managed internalcall
  {
  } // end of method ICorDebugILFrame::EnumerateArguments

  .method public hidebysig newslot abstract virtual 
          instance void  GetArgument([in] uint32 dwIndex,
                                     [out] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugValue&  marshal( interface) ppValue) runtime managed internalcall
  {
  } // end of method ICorDebugILFrame::GetArgument

  .method public hidebysig newslot abstract virtual 
          instance void  GetStackDepth([out] uint32& pDepth) runtime managed internalcall
  {
  } // end of method ICorDebugILFrame::GetStackDepth

  .method public hidebysig newslot abstract virtual 
          instance void  GetStackValue([in] uint32 dwIndex,
                                       [out] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugValue&  marshal( interface) ppValue) runtime managed internalcall
  {
  } // end of method ICorDebugILFrame::GetStackValue

  .method public hidebysig newslot abstract virtual 
          instance int32 CanSetIP([in] uint32 nOffset) runtime managed internalcall preservesig
  {
  } // end of method ICorDebugILFrame::CanSetIP

} // end of class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugILFrame

.class public auto ansi sealed Microsoft.Samples.Debugging.CorDebug.NativeApi.CorDebugMappingResult
       extends [mscorlib]System.Enum
{
  .field public specialname rtspecialname int32 value__
  .field public static literal valuetype Microsoft.Samples.Debugging.CorDebug.NativeApi.CorDebugMappingResult MAPPING_PROLOG = int32(0x00000001)
  .field public static literal valuetype Microsoft.Samples.Debugging.CorDebug.NativeApi.CorDebugMappingResult MAPPING_EPILOG = int32(0x00000002)
  .field public static literal valuetype Microsoft.Samples.Debugging.CorDebug.NativeApi.CorDebugMappingResult MAPPING_NO_INFO = int32(0x00000004)
  .field public static literal valuetype Microsoft.Samples.Debugging.CorDebug.NativeApi.CorDebugMappingResult MAPPING_UNMAPPED_ADDRESS = int32(0x00000008)
  .field public static literal valuetype Microsoft.Samples.Debugging.CorDebug.NativeApi.CorDebugMappingResult MAPPING_EXACT = int32(0x00000010)
  .field public static literal valuetype Microsoft.Samples.Debugging.CorDebug.NativeApi.CorDebugMappingResult MAPPING_APPROXIMATE = int32(0x00000020)
} // end of class Microsoft.Samples.Debugging.CorDebug.NativeApi.CorDebugMappingResult

.class interface public abstract auto ansi import Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugValueEnum
       implements Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugEnum
{
  .custom instance void [mscorlib]System.Runtime.InteropServices.ComConversionLossAttribute::.ctor() = ( 01 00 00 00 ) 
  .custom instance void [mscorlib]System.Runtime.InteropServices.InterfaceTypeAttribute::.ctor(int16) = ( 01 00 01 00 00 00 ) 
  .custom instance void [mscorlib]System.Runtime.InteropServices.GuidAttribute::.ctor(string) = ( 01 00 24 43 43 37 42 43 42 30 41 2D 38 41 36 38   // ..$CC7BCB0A-8A68
                                                                                                  2D 31 31 44 32 2D 39 38 33 43 2D 30 30 30 30 46   // -11D2-983C-0000F
                                                                                                  38 30 38 33 34 32 44 00 00 )                      // 808342D..
  .method public hidebysig newslot abstract virtual 
          instance void  Skip([in] uint32 celt) runtime managed internalcall
  {
  } // end of method ICorDebugValueEnum::Skip

  .method public hidebysig newslot abstract virtual 
          instance void  Reset() runtime managed internalcall
  {
  } // end of method ICorDebugValueEnum::Reset

  .method public hidebysig newslot abstract virtual 
          instance void  Clone([out] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugEnum&  marshal( interface) ppEnum) runtime managed internalcall
  {
  } // end of method ICorDebugValueEnum::Clone

  .method public hidebysig newslot abstract virtual 
          instance void  GetCount([out] uint32& pcelt) runtime managed internalcall
  {
  } // end of method ICorDebugValueEnum::GetCount

  .method public hidebysig newslot abstract virtual 
          instance int32 Next([in] uint32 celt,
                              [out] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugValue[] marshal([ ]) values,
                              [out] uint32& pceltFetched) runtime managed internalcall preservesig
  {
  } // end of method ICorDebugValueEnum::Next

} // end of class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugValueEnum

.class interface public abstract auto ansi import Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugInternalFrame
       implements Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugFrame
{
  .custom instance void [mscorlib]System.Runtime.InteropServices.GuidAttribute::.ctor(string) = ( 01 00 24 42 39 32 43 43 37 46 37 2D 39 44 32 44   // ..$B92CC7F7-9D2D
                                                                                                  2D 34 35 43 34 2D 42 43 32 42 2D 36 32 31 46 43   // -45C4-BC2B-621FC
                                                                                                  43 39 44 46 42 46 34 00 00 )                      // C9DFBF4..
  .custom instance void [mscorlib]System.Runtime.InteropServices.InterfaceTypeAttribute::.ctor(int16) = ( 01 00 01 00 00 00 ) 
  .method public hidebysig newslot abstract virtual 
          instance void  GetChain([out] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugChain&  marshal( interface) ppChain) runtime managed internalcall
  {
  } // end of method ICorDebugInternalFrame::GetChain

  .method public hidebysig newslot abstract virtual 
          instance void  GetCode([out] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugCode&  marshal( interface) ppCode) runtime managed internalcall
  {
  } // end of method ICorDebugInternalFrame::GetCode

  .method public hidebysig newslot abstract virtual 
          instance void  GetFunction([out] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugFunction&  marshal( interface) ppFunction) runtime managed internalcall
  {
  } // end of method ICorDebugInternalFrame::GetFunction

  .method public hidebysig newslot abstract virtual 
          instance void  GetFunctionToken([out] uint32& pToken) runtime managed internalcall
  {
  } // end of method ICorDebugInternalFrame::GetFunctionToken

  .method public hidebysig newslot abstract virtual 
          instance void  GetStackRange([out] uint64& pStart,
                                       [out] uint64& pEnd) runtime managed internalcall
  {
  } // end of method ICorDebugInternalFrame::GetStackRange

  .method public hidebysig newslot abstract virtual 
          instance void  GetCaller([out] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugFrame&  marshal( interface) ppFrame) runtime managed internalcall
  {
  } // end of method ICorDebugInternalFrame::GetCaller

  .method public hidebysig newslot abstract virtual 
          instance void  GetCallee([out] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugFrame&  marshal( interface) ppFrame) runtime managed internalcall
  {
  } // end of method ICorDebugInternalFrame::GetCallee

  .method public hidebysig newslot abstract virtual 
          instance void  CreateStepper([out] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugStepper&  marshal( interface) ppStepper) runtime managed internalcall
  {
  } // end of method ICorDebugInternalFrame::CreateStepper

  .method public hidebysig newslot abstract virtual 
          instance void  GetFrameType([out] valuetype Microsoft.Samples.Debugging.CorDebug.NativeApi.CorDebugInternalFrameType& pType) runtime managed internalcall
  {
  } // end of method ICorDebugInternalFrame::GetFrameType

} // end of class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugInternalFrame

.class public auto ansi sealed Microsoft.Samples.Debugging.CorDebug.NativeApi.CorDebugInternalFrameType
       extends [mscorlib]System.Enum
{
  .field public specialname rtspecialname int32 value__
  .field public static literal valuetype Microsoft.Samples.Debugging.CorDebug.NativeApi.CorDebugInternalFrameType STUBFRAME_NONE = int32(0x00000000)
  .field public static literal valuetype Microsoft.Samples.Debugging.CorDebug.NativeApi.CorDebugInternalFrameType STUBFRAME_M2U = int32(0x00000001)
  .field public static literal valuetype Microsoft.Samples.Debugging.CorDebug.NativeApi.CorDebugInternalFrameType STUBFRAME_U2M = int32(0x00000002)
  .field public static literal valuetype Microsoft.Samples.Debugging.CorDebug.NativeApi.CorDebugInternalFrameType STUBFRAME_APPDOMAIN_TRANSITION = int32(0x00000003)
  .field public static literal valuetype Microsoft.Samples.Debugging.CorDebug.NativeApi.CorDebugInternalFrameType STUBFRAME_LIGHTWEIGHT_FUNCTION = int32(0x00000004)
  .field public static literal valuetype Microsoft.Samples.Debugging.CorDebug.NativeApi.CorDebugInternalFrameType STUBFRAME_FUNC_EVAL = int32(0x00000005)
} // end of class Microsoft.Samples.Debugging.CorDebug.NativeApi.CorDebugInternalFrameType

.class interface public abstract auto ansi import Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugNativeFrame
       implements Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugFrame
{
  .custom instance void [mscorlib]System.Runtime.InteropServices.InterfaceTypeAttribute::.ctor(int16) = ( 01 00 01 00 00 00 ) 
  .custom instance void [mscorlib]System.Runtime.InteropServices.GuidAttribute::.ctor(string) = ( 01 00 24 30 33 45 32 36 33 31 34 2D 34 46 37 36   // ..$03E26314-4F76
                                                                                                  2D 31 31 44 33 2D 38 38 43 36 2D 30 30 36 30 39   // -11D3-88C6-00609
                                                                                                  37 39 34 35 34 31 38 00 00 )                      // 7945418..
  .method public hidebysig newslot abstract virtual 
          instance void  GetChain([out] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugChain&  marshal( interface) ppChain) runtime managed internalcall
  {
  } // end of method ICorDebugNativeFrame::GetChain

  .method public hidebysig newslot abstract virtual 
          instance void  GetCode([out] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugCode&  marshal( interface) ppCode) runtime managed internalcall
  {
  } // end of method ICorDebugNativeFrame::GetCode

  .method public hidebysig newslot abstract virtual 
          instance void  GetFunction([out] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugFunction&  marshal( interface) ppFunction) runtime managed internalcall
  {
  } // end of method ICorDebugNativeFrame::GetFunction

  .method public hidebysig newslot abstract virtual 
          instance void  GetFunctionToken([out] uint32& pToken) runtime managed internalcall
  {
  } // end of method ICorDebugNativeFrame::GetFunctionToken

  .method public hidebysig newslot abstract virtual 
          instance void  GetStackRange([out] uint64& pStart,
                                       [out] uint64& pEnd) runtime managed internalcall
  {
  } // end of method ICorDebugNativeFrame::GetStackRange

  .method public hidebysig newslot abstract virtual 
          instance void  GetCaller([out] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugFrame&  marshal( interface) ppFrame) runtime managed internalcall
  {
  } // end of method ICorDebugNativeFrame::GetCaller

  .method public hidebysig newslot abstract virtual 
          instance void  GetCallee([out] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugFrame&  marshal( interface) ppFrame) runtime managed internalcall
  {
  } // end of method ICorDebugNativeFrame::GetCallee

  .method public hidebysig newslot abstract virtual 
          instance void  CreateStepper([out] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugStepper&  marshal( interface) ppStepper) runtime managed internalcall
  {
  } // end of method ICorDebugNativeFrame::CreateStepper

  .method public hidebysig newslot abstract virtual 
          instance void  GetIP([out] uint32& pnOffset) runtime managed internalcall
  {
  } // end of method ICorDebugNativeFrame::GetIP

  .method public hidebysig newslot abstract virtual 
          instance void  SetIP([in] uint32 nOffset) runtime managed internalcall
  {
  } // end of method ICorDebugNativeFrame::SetIP

  .method public hidebysig newslot abstract virtual 
          instance void  GetRegisterSet([out] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugRegisterSet&  marshal( interface) ppRegisters) runtime managed internalcall
  {
  } // end of method ICorDebugNativeFrame::GetRegisterSet

  .method public hidebysig newslot abstract virtual 
          instance void  GetLocalRegisterValue([in] valuetype Microsoft.Samples.Debugging.CorDebug.NativeApi.CorDebugRegister reg,
                                               [in] uint32 cbSigBlob,
                                               [in] uint32 pvSigBlob,
                                               [out] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugValue&  marshal( interface) ppValue) runtime managed internalcall
  {
    .param [3]
    .custom instance void [mscorlib]System.Runtime.InteropServices.ComAliasNameAttribute::.ctor(string) = ( 01 00 30 4D 69 63 72 6F 73 6F 66 74 2E 44 65 62   // ..0Microsoft.Deb
                                                                                                            75 67 67 69 6E 67 2E 43 6F 72 44 65 62 75 67 2E   // ugging.CorDebug.
                                                                                                            4E 61 74 69 76 65 41 70 69 2E 55 4C 4F 4E 47 5F   // NativeApi.ULONG_
                                                                                                            50 54 52 00 00 )                                  // PTR..
  } // end of method ICorDebugNativeFrame::GetLocalRegisterValue

  .method public hidebysig newslot abstract virtual 
          instance void  GetLocalDoubleRegisterValue([in] valuetype Microsoft.Samples.Debugging.CorDebug.NativeApi.CorDebugRegister highWordReg,
                                                     [in] valuetype Microsoft.Samples.Debugging.CorDebug.NativeApi.CorDebugRegister lowWordReg,
                                                     [in] uint32 cbSigBlob,
                                                     [in] uint32 pvSigBlob,
                                                     [out] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugValue&  marshal( interface) ppValue) runtime managed internalcall
  {
    .param [4]
    .custom instance void [mscorlib]System.Runtime.InteropServices.ComAliasNameAttribute::.ctor(string) = ( 01 00 30 4D 69 63 72 6F 73 6F 66 74 2E 44 65 62   // ..0Microsoft.Deb
                                                                                                            75 67 67 69 6E 67 2E 43 6F 72 44 65 62 75 67 2E   // ugging.CorDebug.
                                                                                                            4E 61 74 69 76 65 41 70 69 2E 55 4C 4F 4E 47 5F   // NativeApi.ULONG_
                                                                                                            50 54 52 00 00 )                                  // PTR..
  } // end of method ICorDebugNativeFrame::GetLocalDoubleRegisterValue

  .method public hidebysig newslot abstract virtual 
          instance void  GetLocalMemoryValue([in] uint64 address,
                                             [in] uint32 cbSigBlob,
                                             [in] uint32 pvSigBlob,
                                             [out] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugValue&  marshal( interface) ppValue) runtime managed internalcall
  {
    .param [3]
    .custom instance void [mscorlib]System.Runtime.InteropServices.ComAliasNameAttribute::.ctor(string) = ( 01 00 30 4D 69 63 72 6F 73 6F 66 74 2E 44 65 62   // ..0Microsoft.Deb
                                                                                                            75 67 67 69 6E 67 2E 43 6F 72 44 65 62 75 67 2E   // ugging.CorDebug.
                                                                                                            4E 61 74 69 76 65 41 70 69 2E 55 4C 4F 4E 47 5F   // NativeApi.ULONG_
                                                                                                            50 54 52 00 00 )                                  // PTR..
  } // end of method ICorDebugNativeFrame::GetLocalMemoryValue

  .method public hidebysig newslot abstract virtual 
          instance void  GetLocalRegisterMemoryValue([in] valuetype Microsoft.Samples.Debugging.CorDebug.NativeApi.CorDebugRegister highWordReg,
                                                     [in] uint64 lowWordAddress,
                                                     [in] uint32 cbSigBlob,
                                                     [in] uint32 pvSigBlob,
                                                     [out] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugValue&  marshal( interface) ppValue) runtime managed internalcall
  {
    .param [4]
    .custom instance void [mscorlib]System.Runtime.InteropServices.ComAliasNameAttribute::.ctor(string) = ( 01 00 30 4D 69 63 72 6F 73 6F 66 74 2E 44 65 62   // ..0Microsoft.Deb
                                                                                                            75 67 67 69 6E 67 2E 43 6F 72 44 65 62 75 67 2E   // ugging.CorDebug.
                                                                                                            4E 61 74 69 76 65 41 70 69 2E 55 4C 4F 4E 47 5F   // NativeApi.ULONG_
                                                                                                            50 54 52 00 00 )                                  // PTR..
  } // end of method ICorDebugNativeFrame::GetLocalRegisterMemoryValue

  .method public hidebysig newslot abstract virtual 
          instance void  GetLocalMemoryRegisterValue([in] uint64 highWordAddress,
                                                     [in] valuetype Microsoft.Samples.Debugging.CorDebug.NativeApi.CorDebugRegister lowWordRegister,
                                                     [in] uint32 cbSigBlob,
                                                     [in] uint32 pvSigBlob,
                                                     [out] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugValue&  marshal( interface) ppValue) runtime managed internalcall
  {
    .param [4]
    .custom instance void [mscorlib]System.Runtime.InteropServices.ComAliasNameAttribute::.ctor(string) = ( 01 00 30 4D 69 63 72 6F 73 6F 66 74 2E 44 65 62   // ..0Microsoft.Deb
                                                                                                            75 67 67 69 6E 67 2E 43 6F 72 44 65 62 75 67 2E   // ugging.CorDebug.
                                                                                                            4E 61 74 69 76 65 41 70 69 2E 55 4C 4F 4E 47 5F   // NativeApi.ULONG_
                                                                                                            50 54 52 00 00 )                                  // PTR..
  } // end of method ICorDebugNativeFrame::GetLocalMemoryRegisterValue

  .method public hidebysig newslot abstract virtual 
          instance int32 CanSetIP([in] uint32 nOffset) runtime managed internalcall preservesig
  {
  } // end of method ICorDebugNativeFrame::CanSetIP

} // end of class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugNativeFrame

.class public auto ansi sealed Microsoft.Samples.Debugging.CorDebug.NativeApi.CorDebugRegister
       extends [mscorlib]System.Enum
{
  .field public specialname rtspecialname int32 value__
  .field public static literal valuetype Microsoft.Samples.Debugging.CorDebug.NativeApi.CorDebugRegister REGISTER_INSTRUCTION_POINTER = int32(0x00000000)
  .field public static literal valuetype Microsoft.Samples.Debugging.CorDebug.NativeApi.CorDebugRegister REGISTER_STACK_POINTER = int32(0x00000001)
  .field public static literal valuetype Microsoft.Samples.Debugging.CorDebug.NativeApi.CorDebugRegister REGISTER_FRAME_POINTER = int32(0x00000002)
  .field public static literal valuetype Microsoft.Samples.Debugging.CorDebug.NativeApi.CorDebugRegister REGISTER_X86_EIP = int32(0x00000000)
  .field public static literal valuetype Microsoft.Samples.Debugging.CorDebug.NativeApi.CorDebugRegister REGISTER_X86_ESP = int32(0x00000001)
  .field public static literal valuetype Microsoft.Samples.Debugging.CorDebug.NativeApi.CorDebugRegister REGISTER_X86_EBP = int32(0x00000002)
  .field public static literal valuetype Microsoft.Samples.Debugging.CorDebug.NativeApi.CorDebugRegister REGISTER_X86_EAX = int32(0x00000003)
  .field public static literal valuetype Microsoft.Samples.Debugging.CorDebug.NativeApi.CorDebugRegister REGISTER_X86_ECX = int32(0x00000004)
  .field public static literal valuetype Microsoft.Samples.Debugging.CorDebug.NativeApi.CorDebugRegister REGISTER_X86_EDX = int32(0x00000005)
  .field public static literal valuetype Microsoft.Samples.Debugging.CorDebug.NativeApi.CorDebugRegister REGISTER_X86_EBX = int32(0x00000006)
  .field public static literal valuetype Microsoft.Samples.Debugging.CorDebug.NativeApi.CorDebugRegister REGISTER_X86_ESI = int32(0x00000007)
  .field public static literal valuetype Microsoft.Samples.Debugging.CorDebug.NativeApi.CorDebugRegister REGISTER_X86_EDI = int32(0x00000008)
  .field public static literal valuetype Microsoft.Samples.Debugging.CorDebug.NativeApi.CorDebugRegister REGISTER_X86_FPSTACK_0 = int32(0x00000009)
  .field public static literal valuetype Microsoft.Samples.Debugging.CorDebug.NativeApi.CorDebugRegister REGISTER_X86_FPSTACK_1 = int32(0x0000000A)
  .field public static literal valuetype Microsoft.Samples.Debugging.CorDebug.NativeApi.CorDebugRegister REGISTER_X86_FPSTACK_2 = int32(0x0000000B)
  .field public static literal valuetype Microsoft.Samples.Debugging.CorDebug.NativeApi.CorDebugRegister REGISTER_X86_FPSTACK_3 = int32(0x0000000C)
  .field public static literal valuetype Microsoft.Samples.Debugging.CorDebug.NativeApi.CorDebugRegister REGISTER_X86_FPSTACK_4 = int32(0x0000000D)
  .field public static literal valuetype Microsoft.Samples.Debugging.CorDebug.NativeApi.CorDebugRegister REGISTER_X86_FPSTACK_5 = int32(0x0000000E)
  .field public static literal valuetype Microsoft.Samples.Debugging.CorDebug.NativeApi.CorDebugRegister REGISTER_X86_FPSTACK_6 = int32(0x0000000F)
  .field public static literal valuetype Microsoft.Samples.Debugging.CorDebug.NativeApi.CorDebugRegister REGISTER_X86_FPSTACK_7 = int32(0x00000010)
  .field public static literal valuetype Microsoft.Samples.Debugging.CorDebug.NativeApi.CorDebugRegister REGISTER_AMD64_RIP = int32(0x00000000)
  .field public static literal valuetype Microsoft.Samples.Debugging.CorDebug.NativeApi.CorDebugRegister REGISTER_AMD64_RSP = int32(0x00000001)
  .field public static literal valuetype Microsoft.Samples.Debugging.CorDebug.NativeApi.CorDebugRegister REGISTER_AMD64_RBP = int32(0x00000002)
  .field public static literal valuetype Microsoft.Samples.Debugging.CorDebug.NativeApi.CorDebugRegister REGISTER_AMD64_RAX = int32(0x00000003)
  .field public static literal valuetype Microsoft.Samples.Debugging.CorDebug.NativeApi.CorDebugRegister REGISTER_AMD64_RCX = int32(0x00000004)
  .field public static literal valuetype Microsoft.Samples.Debugging.CorDebug.NativeApi.CorDebugRegister REGISTER_AMD64_RDX = int32(0x00000005)
  .field public static literal valuetype Microsoft.Samples.Debugging.CorDebug.NativeApi.CorDebugRegister REGISTER_AMD64_RBX = int32(0x00000006)
  .field public static literal valuetype Microsoft.Samples.Debugging.CorDebug.NativeApi.CorDebugRegister REGISTER_AMD64_RSI = int32(0x00000007)
  .field public static literal valuetype Microsoft.Samples.Debugging.CorDebug.NativeApi.CorDebugRegister REGISTER_AMD64_RDI = int32(0x00000008)
  .field public static literal valuetype Microsoft.Samples.Debugging.CorDebug.NativeApi.CorDebugRegister REGISTER_AMD64_R8 = int32(0x00000009)
  .field public static literal valuetype Microsoft.Samples.Debugging.CorDebug.NativeApi.CorDebugRegister REGISTER_AMD64_R9 = int32(0x0000000A)
  .field public static literal valuetype Microsoft.Samples.Debugging.CorDebug.NativeApi.CorDebugRegister REGISTER_AMD64_R10 = int32(0x0000000B)
  .field public static literal valuetype Microsoft.Samples.Debugging.CorDebug.NativeApi.CorDebugRegister REGISTER_AMD64_R11 = int32(0x0000000C)
  .field public static literal valuetype Microsoft.Samples.Debugging.CorDebug.NativeApi.CorDebugRegister REGISTER_AMD64_R12 = int32(0x0000000D)
  .field public static literal valuetype Microsoft.Samples.Debugging.CorDebug.NativeApi.CorDebugRegister REGISTER_AMD64_R13 = int32(0x0000000E)
  .field public static literal valuetype Microsoft.Samples.Debugging.CorDebug.NativeApi.CorDebugRegister REGISTER_AMD64_R14 = int32(0x0000000F)
  .field public static literal valuetype Microsoft.Samples.Debugging.CorDebug.NativeApi.CorDebugRegister REGISTER_AMD64_R15 = int32(0x00000010)
  .field public static literal valuetype Microsoft.Samples.Debugging.CorDebug.NativeApi.CorDebugRegister REGISTER_AMD64_XMM0 = int32(0x00000011)
  .field public static literal valuetype Microsoft.Samples.Debugging.CorDebug.NativeApi.CorDebugRegister REGISTER_AMD64_XMM1 = int32(0x00000012)
  .field public static literal valuetype Microsoft.Samples.Debugging.CorDebug.NativeApi.CorDebugRegister REGISTER_AMD64_XMM2 = int32(0x00000013)
  .field public static literal valuetype Microsoft.Samples.Debugging.CorDebug.NativeApi.CorDebugRegister REGISTER_AMD64_XMM3 = int32(0x00000014)
  .field public static literal valuetype Microsoft.Samples.Debugging.CorDebug.NativeApi.CorDebugRegister REGISTER_AMD64_XMM4 = int32(0x00000015)
  .field public static literal valuetype Microsoft.Samples.Debugging.CorDebug.NativeApi.CorDebugRegister REGISTER_AMD64_XMM5 = int32(0x00000016)
  .field public static literal valuetype Microsoft.Samples.Debugging.CorDebug.NativeApi.CorDebugRegister REGISTER_AMD64_XMM6 = int32(0x00000017)
  .field public static literal valuetype Microsoft.Samples.Debugging.CorDebug.NativeApi.CorDebugRegister REGISTER_AMD64_XMM7 = int32(0x00000018)
  .field public static literal valuetype Microsoft.Samples.Debugging.CorDebug.NativeApi.CorDebugRegister REGISTER_AMD64_XMM8 = int32(0x00000019)
  .field public static literal valuetype Microsoft.Samples.Debugging.CorDebug.NativeApi.CorDebugRegister REGISTER_AMD64_XMM9 = int32(0x0000001A)
  .field public static literal valuetype Microsoft.Samples.Debugging.CorDebug.NativeApi.CorDebugRegister REGISTER_AMD64_XMM10 = int32(0x0000001B)
  .field public static literal valuetype Microsoft.Samples.Debugging.CorDebug.NativeApi.CorDebugRegister REGISTER_AMD64_XMM11 = int32(0x0000001C)
  .field public static literal valuetype Microsoft.Samples.Debugging.CorDebug.NativeApi.CorDebugRegister REGISTER_AMD64_XMM12 = int32(0x0000001D)
  .field public static literal valuetype Microsoft.Samples.Debugging.CorDebug.NativeApi.CorDebugRegister REGISTER_AMD64_XMM13 = int32(0x0000001E)
  .field public static literal valuetype Microsoft.Samples.Debugging.CorDebug.NativeApi.CorDebugRegister REGISTER_AMD64_XMM14 = int32(0x0000001F)
  .field public static literal valuetype Microsoft.Samples.Debugging.CorDebug.NativeApi.CorDebugRegister REGISTER_AMD64_XMM15 = int32(0x00000020)
  .field public static literal valuetype Microsoft.Samples.Debugging.CorDebug.NativeApi.CorDebugRegister REGISTER_IA64_BSP = int32(0x00000002)
  .field public static literal valuetype Microsoft.Samples.Debugging.CorDebug.NativeApi.CorDebugRegister REGISTER_IA64_R0 = int32(0x00000003)
  .field public static literal valuetype Microsoft.Samples.Debugging.CorDebug.NativeApi.CorDebugRegister REGISTER_IA64_F0 = int32(0x00000083)
} // end of class Microsoft.Samples.Debugging.CorDebug.NativeApi.CorDebugRegister

.class interface public abstract auto ansi import Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugManagedCallback2
{
  .custom instance void [mscorlib]System.Runtime.InteropServices.GuidAttribute::.ctor(string) = ( 01 00 24 32 35 30 45 35 45 45 41 2D 44 42 35 43   // ..$250E5EEA-DB5C
                                                                                                  2D 34 43 37 36 2D 42 36 46 33 2D 38 43 34 36 46   // -4C76-B6F3-8C46F
                                                                                                  31 32 45 33 32 30 33 00 00 )                      // 12E3203..
  .custom instance void [mscorlib]System.Runtime.InteropServices.InterfaceTypeAttribute::.ctor(int16) = ( 01 00 01 00 00 00 ) 
  .method public hidebysig newslot abstract virtual 
          instance void  FunctionRemapOpportunity([in] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugAppDomain  marshal( interface) pAppDomain,
                                                  [in] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugThread  marshal( interface) pThread,
                                                  [in] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugFunction  marshal( interface) pOldFunction,
                                                  [in] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugFunction  marshal( interface) pNewFunction,
                                                  [in] uint32 oldILOffset) runtime managed internalcall
  {
  } // end of method ICorDebugManagedCallback2::FunctionRemapOpportunity

  .method public hidebysig newslot abstract virtual 
          instance void  CreateConnection([in] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugProcess  marshal( interface) pProcess,
                                          [in] uint32 dwConnectionId,
                                          [in] uint16& pConnName) runtime managed internalcall
  {
  } // end of method ICorDebugManagedCallback2::CreateConnection

  .method public hidebysig newslot abstract virtual 
          instance void  ChangeConnection([in] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugProcess  marshal( interface) pProcess,
                                          [in] uint32 dwConnectionId) runtime managed internalcall
  {
  } // end of method ICorDebugManagedCallback2::ChangeConnection

  .method public hidebysig newslot abstract virtual 
          instance void  DestroyConnection([in] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugProcess  marshal( interface) pProcess,
                                           [in] uint32 dwConnectionId) runtime managed internalcall
  {
  } // end of method ICorDebugManagedCallback2::DestroyConnection

  .method public hidebysig newslot abstract virtual 
          instance void  Exception([in] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugAppDomain  marshal( interface) pAppDomain,
                                   [in] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugThread  marshal( interface) pThread,
                                   [in] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugFrame  marshal( interface) pFrame,
                                   [in] uint32 nOffset,
                                   [in] valuetype Microsoft.Samples.Debugging.CorDebug.NativeApi.CorDebugExceptionCallbackType dwEventType,
                                   [in] uint32 dwFlags) runtime managed internalcall
  {
  } // end of method ICorDebugManagedCallback2::Exception

  .method public hidebysig newslot abstract virtual 
          instance void  ExceptionUnwind([in] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugAppDomain  marshal( interface) pAppDomain,
                                         [in] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugThread  marshal( interface) pThread,
                                         [in] valuetype Microsoft.Samples.Debugging.CorDebug.NativeApi.CorDebugExceptionUnwindCallbackType dwEventType,
                                         [in] uint32 dwFlags) runtime managed internalcall
  {
  } // end of method ICorDebugManagedCallback2::ExceptionUnwind

  .method public hidebysig newslot abstract virtual 
          instance void  FunctionRemapComplete([in] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugAppDomain  marshal( interface) pAppDomain,
                                               [in] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugThread  marshal( interface) pThread,
                                               [in] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugFunction  marshal( interface) pFunction) runtime managed internalcall
  {
  } // end of method ICorDebugManagedCallback2::FunctionRemapComplete

  .method public hidebysig newslot abstract virtual 
          instance void  MDANotification([in] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugController marshal( interface) pController,
                                         [in] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugThread   marshal( interface) pThread,
                                         //[in] object  marshal( iunknown) pMDA
									     [in] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugMDA marshal( interface) pMDA
                                         ) runtime managed internalcall
  {
  } // end of method ICorDebugManagedCallback2::MDANotification	

} // end of class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugManagedCallback2

.class public auto ansi sealed Microsoft.Samples.Debugging.CorDebug.NativeApi.CorDebugExceptionCallbackType
       extends [mscorlib]System.Enum
{
  .field public specialname rtspecialname int32 value__
  .field public static literal valuetype Microsoft.Samples.Debugging.CorDebug.NativeApi.CorDebugExceptionCallbackType DEBUG_EXCEPTION_FIRST_CHANCE = int32(0x00000001)
  .field public static literal valuetype Microsoft.Samples.Debugging.CorDebug.NativeApi.CorDebugExceptionCallbackType DEBUG_EXCEPTION_USER_FIRST_CHANCE = int32(0x00000002)
  .field public static literal valuetype Microsoft.Samples.Debugging.CorDebug.NativeApi.CorDebugExceptionCallbackType DEBUG_EXCEPTION_CATCH_HANDLER_FOUND = int32(0x00000003)
  .field public static literal valuetype Microsoft.Samples.Debugging.CorDebug.NativeApi.CorDebugExceptionCallbackType DEBUG_EXCEPTION_UNHANDLED = int32(0x00000004)
} // end of class Microsoft.Samples.Debugging.CorDebug.NativeApi.CorDebugExceptionCallbackType

.class public auto ansi sealed Microsoft.Samples.Debugging.CorDebug.NativeApi.CorDebugExceptionUnwindCallbackType
       extends [mscorlib]System.Enum
{
  .field public specialname rtspecialname int32 value__
  .field public static literal valuetype Microsoft.Samples.Debugging.CorDebug.NativeApi.CorDebugExceptionUnwindCallbackType DEBUG_EXCEPTION_UNWIND_BEGIN = int32(0x00000001)
  .field public static literal valuetype Microsoft.Samples.Debugging.CorDebug.NativeApi.CorDebugExceptionUnwindCallbackType DEBUG_EXCEPTION_INTERCEPTED = int32(0x00000002)
} // end of class Microsoft.Samples.Debugging.CorDebug.NativeApi.CorDebugExceptionUnwindCallbackType

.class interface public abstract auto ansi import Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugAppDomain2
{
  .custom instance void [mscorlib]System.Runtime.InteropServices.InterfaceTypeAttribute::.ctor(int16) = ( 01 00 01 00 00 00 ) 
  .custom instance void [mscorlib]System.Runtime.InteropServices.GuidAttribute::.ctor(string) = ( 01 00 24 30 39 36 45 38 31 44 35 2D 45 43 44 41   // ..$096E81D5-ECDA
                                                                                                  2D 34 32 30 32 2D 38 33 46 35 2D 43 36 35 39 38   // -4202-83F5-C6598
                                                                                                  30 41 39 45 46 37 35 00 00 )                      // 0A9EF75..
  .method public hidebysig newslot abstract virtual 
          instance void  GetArrayOrPointerType([in] valuetype Microsoft.Samples.Debugging.CorDebug.NativeApi.CorElementType elementType,
                                               [in] uint32 nRank,
                                               [in] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugType  marshal( interface) pTypeArg,
                                               [out] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugType&  marshal( interface) ppType) runtime managed internalcall
  {
  } // end of method ICorDebugAppDomain2::GetArrayOrPointerType

  .method public hidebysig newslot abstract virtual 
          instance void  GetFunctionPointerType([in] uint32 nTypeArgs,
                                                [in] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugType[] marshal( [ +0 ]) ppTypeArgs,
                                                [out] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugType&  marshal( interface) ppType) runtime managed internalcall
  {
  } // end of method ICorDebugAppDomain2::GetFunctionPointerType

} // end of class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugAppDomain2

.class interface public abstract auto ansi import Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugType
{
  .custom instance void [mscorlib]System.Runtime.InteropServices.GuidAttribute::.ctor(string) = ( 01 00 24 44 36 31 33 46 30 42 42 2D 41 43 45 31   // ..$D613F0BB-ACE1
                                                                                                  2D 34 43 31 39 2D 42 44 37 32 2D 45 34 43 30 38   // -4C19-BD72-E4C08
                                                                                                  44 35 44 41 37 46 35 00 00 )                      // D5DA7F5..
  .custom instance void [mscorlib]System.Runtime.InteropServices.InterfaceTypeAttribute::.ctor(int16) = ( 01 00 01 00 00 00 ) 
  .method public hidebysig newslot abstract virtual 
          instance void  GetType([out] valuetype Microsoft.Samples.Debugging.CorDebug.NativeApi.CorElementType& ty) runtime managed internalcall
  {
  } // end of method ICorDebugType::GetType

  .method public hidebysig newslot abstract virtual 
          instance void  GetClass([out] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugClass&  marshal( interface) ppClass) runtime managed internalcall
  {
  } // end of method ICorDebugType::GetClass

  .method public hidebysig newslot abstract virtual 
          instance void  EnumerateTypeParameters([out] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugTypeEnum&  marshal( interface) ppTyParEnum) runtime managed internalcall
  {
  } // end of method ICorDebugType::EnumerateTypeParameters

  .method public hidebysig newslot abstract virtual 
          instance void  GetFirstTypeParameter([out] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugType&  marshal( interface) 'value') runtime managed internalcall
  {
  } // end of method ICorDebugType::GetFirstTypeParameter

  .method public hidebysig newslot abstract virtual 
          instance void  GetBase([out] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugType&  marshal( interface) pBase) runtime managed internalcall
  {
  } // end of method ICorDebugType::GetBase

  .method public hidebysig newslot abstract virtual 
          instance void  GetStaticFieldValue([in] uint32 fieldDef,
                                             [in] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugFrame  marshal( interface) pFrame,
                                             [out] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugValue&  marshal( interface) ppValue) runtime managed internalcall
  {
  } // end of method ICorDebugType::GetStaticFieldValue

  .method public hidebysig newslot abstract virtual 
          instance void  GetRank([out] uint32& pnRank) runtime managed internalcall
  {
  } // end of method ICorDebugType::GetRank

} // end of class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugType

.class interface public abstract auto ansi import Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugTypeEnum
       implements Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugEnum
{
  .custom instance void [mscorlib]System.Runtime.InteropServices.ComConversionLossAttribute::.ctor() = ( 01 00 00 00 ) 
  .custom instance void [mscorlib]System.Runtime.InteropServices.GuidAttribute::.ctor(string) = ( 01 00 24 31 30 46 32 37 34 39 39 2D 39 44 46 32   // ..$10F27499-9DF2
                                                                                                  2D 34 33 43 45 2D 38 33 33 33 2D 41 33 32 31 44   // -43CE-8333-A321D
                                                                                                  37 43 39 39 43 42 34 00 00 )                      // 7C99CB4..
  .custom instance void [mscorlib]System.Runtime.InteropServices.InterfaceTypeAttribute::.ctor(int16) = ( 01 00 01 00 00 00 ) 
  .method public hidebysig newslot abstract virtual 
          instance void  Skip([in] uint32 celt) runtime managed internalcall
  {
  } // end of method ICorDebugTypeEnum::Skip

  .method public hidebysig newslot abstract virtual 
          instance void  Reset() runtime managed internalcall
  {
  } // end of method ICorDebugTypeEnum::Reset

  .method public hidebysig newslot abstract virtual 
          instance void  Clone([out] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugEnum&  marshal( interface) ppEnum) runtime managed internalcall
  {
  } // end of method ICorDebugTypeEnum::Clone

  .method public hidebysig newslot abstract virtual 
          instance void  GetCount([out] uint32& pcelt) runtime managed internalcall
  {
  } // end of method ICorDebugTypeEnum::GetCount


  .method public hidebysig newslot abstract virtual 
           instance int32  Next([in] uint32 celt,
                              [out] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugType[] marshal([ ]) values,
                              [out] uint32& pceltFetched) runtime managed internalcall preservesig
  {
  } // end of method ICorDebugTypeEnum::Next

} // end of class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugTypeEnum

.class interface public abstract auto ansi import Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugProcess2
{
  .custom instance void [mscorlib]System.Runtime.InteropServices.InterfaceTypeAttribute::.ctor(int16) = ( 01 00 01 00 00 00 ) 
  .custom instance void [mscorlib]System.Runtime.InteropServices.ComConversionLossAttribute::.ctor() = ( 01 00 00 00 ) 
  .custom instance void [mscorlib]System.Runtime.InteropServices.GuidAttribute::.ctor(string) = ( 01 00 24 41 44 31 42 33 35 38 38 2D 30 45 46 30   // ..$AD1B3588-0EF0
                                                                                                  2D 34 37 34 34 2D 41 34 39 36 2D 41 41 30 39 41   // -4744-A496-AA09A
                                                                                                  39 46 38 30 33 37 31 00 00 )                      // 9F80371..
  .method public hidebysig newslot abstract virtual 
          instance void  GetThreadForTaskID([in] uint64 taskid,
                                            [out] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugThread2&  marshal( interface) ppThread) runtime managed internalcall
  {
  } // end of method ICorDebugProcess2::GetThreadForTaskID

  .method public hidebysig newslot abstract virtual 
          instance void  GetVersion([out] valuetype Microsoft.Samples.Debugging.CorDebug.NativeApi._COR_VERSION& version) runtime managed internalcall
  {
  } // end of method ICorDebugProcess2::GetVersion

  .method public hidebysig newslot abstract virtual 
          instance void  SetUnmanagedBreakpoint([in] uint64 address,
                                                [in] uint32 bufsize,
                                                [out] unsigned int8[] marshal([]) buffer,
                                                [out] uint32& bufLen) runtime managed internalcall
  {
  } // end of method ICorDebugProcess2::SetUnmanagedBreakpoint

  .method public hidebysig newslot abstract virtual 
          instance void  ClearUnmanagedBreakpoint([in] uint64 address) runtime managed internalcall
  {
  } // end of method ICorDebugProcess2::ClearUnmanagedBreakpoint

  .method public hidebysig newslot abstract virtual 
          instance void SetDesiredNGENCompilerFlags([in] uint32 pdwFlags) runtime managed internalcall
  {
  } // end of method ICorDebugProcess2::SetDesiredNGENCompilerFlags

  .method public hidebysig newslot abstract virtual 
          instance void  GetDesiredNGENCompilerFlags([out] uint32& pdwFlags) runtime managed internalcall
  {
  } // end of method ICorDebugProcess2::GetDesiredNGENCompilerFlags

  .method public hidebysig newslot abstract virtual 

          instance void  GetReferenceValueFromGCHandle([in] native int handle,
                                                       [out] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugReferenceValue&  marshal(interface) pOutValue) runtime managed internalcall
  {

  } // end of method ICorDebugProcess2::GetReferenceValueFromGCHandle 

} // end of class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugProcess2

.class interface public abstract auto ansi import Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugThread2
{
  .custom instance void [mscorlib]System.Runtime.InteropServices.GuidAttribute::.ctor(string) = ( 01 00 24 32 42 44 39 35 36 44 39 2D 37 42 30 37   // ..$2BD956D9-7B07
                                                                                                  2D 34 42 45 46 2D 38 41 39 38 2D 31 32 41 41 38   // -4BEF-8A98-12AA8
                                                                                                  36 32 34 31 37 43 35 00 00 )                      // 62417C5..
  .custom instance void [mscorlib]System.Runtime.InteropServices.InterfaceTypeAttribute::.ctor(int16) = ( 01 00 01 00 00 00 ) 
  .custom instance void [mscorlib]System.Runtime.InteropServices.ComConversionLossAttribute::.ctor() = ( 01 00 00 00 ) 
  .method public hidebysig newslot abstract virtual 
          instance void  GetActiveFunctions([in] uint32 cFunctions,
                                            [out] uint32& pcFunctions,
                                            [out] valuetype Microsoft.Samples.Debugging.CorDebug.NativeApi.COR_ACTIVE_FUNCTION[] marshal( [ +1 ] ) pFunctions) runtime managed internalcall
  {
  } // end of method ICorDebugThread2::GetActiveFunctions

  .method public hidebysig newslot abstract virtual 
          instance void  GetConnectionID([out] uint32& pdwConnectionId) runtime managed internalcall
  {
  } // end of method ICorDebugThread2::GetConnectionID

  .method public hidebysig newslot abstract virtual 
          instance void  GetTaskID([out] uint64& pTaskId) runtime managed internalcall
  {
  } // end of method ICorDebugThread2::GetTaskID

  .method public hidebysig newslot abstract virtual 
          instance void  GetVolatileOSThreadID([out] uint32& pdwTid) runtime managed internalcall
  {
  } // end of method ICorDebugThread2::GetVolatileOSThreadID

  .method public hidebysig newslot abstract virtual 
          instance void  InterceptCurrentException([in] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugFrame  marshal( interface) pFrame) runtime managed internalcall
  {
  } // end of method ICorDebugThread2::InterceptCurrentException

} // end of class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugThread2

.class public sequential ansi sealed beforefieldinit Microsoft.Samples.Debugging.CorDebug.NativeApi._COR_VERSION
       extends [mscorlib]System.ValueType
{
  .pack 4
  .size 0
  .field public uint32 dwMajor
  .field public uint32 dwMinor
  .field public uint32 dwBuild
  .field public uint32 dwSubBuild
} // end of class Microsoft.Samples.Debugging.CorDebug.NativeApi._COR_VERSION

.class interface public abstract auto ansi import Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugStepper2
{
  .custom instance void [mscorlib]System.Runtime.InteropServices.InterfaceTypeAttribute::.ctor(int16) = ( 01 00 01 00 00 00 ) 
  .custom instance void [mscorlib]System.Runtime.InteropServices.GuidAttribute::.ctor(string) = ( 01 00 24 43 35 42 36 45 39 43 33 2D 45 37 44 31   // ..$C5B6E9C3-E7D1
                                                                                                  2D 34 41 38 45 2D 38 37 33 42 2D 37 46 30 34 37   // -4A8E-873B-7F047
                                                                                                  46 30 37 30 36 46 37 00 00 )                      // F0706F7..
  .method public hidebysig newslot abstract virtual 
          instance void  SetJMC([in] int32 fIsJMCStepper) runtime managed internalcall
  {
  } // end of method ICorDebugStepper2::SetJMC

} // end of class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugStepper2

.class interface public abstract auto ansi import Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugILFrame2
{
  .custom instance void [mscorlib]System.Runtime.InteropServices.InterfaceTypeAttribute::.ctor(int16) = ( 01 00 01 00 00 00 ) 
  .custom instance void [mscorlib]System.Runtime.InteropServices.GuidAttribute::.ctor(string) = ( 01 00 24 35 44 38 38 41 39 39 34 2D 36 43 33 30   // ..$5D88A994-6C30
                                                                                                  2D 34 37 39 42 2D 38 39 30 46 2D 42 43 45 46 38   // -479B-890F-BCEF8
                                                                                                  38 42 31 32 39 41 35 00 00 )                      // 8B129A5..
  .method public hidebysig newslot abstract virtual 
          instance void  RemapFunction([in] uint32 newILOffset) runtime managed internalcall
  {
  } // end of method ICorDebugILFrame2::RemapFunction

  .method public hidebysig newslot abstract virtual 
          instance void  EnumerateTypeParameters([out] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugTypeEnum&  marshal( interface) ppTyParEnum) runtime managed internalcall
  {
  } // end of method ICorDebugILFrame2::EnumerateTypeParameters

} // end of class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugILFrame2

.class interface public abstract auto ansi import Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugModule2
{
  .custom instance void [mscorlib]System.Runtime.InteropServices.InterfaceTypeAttribute::.ctor(int16) = ( 01 00 01 00 00 00 ) 
  .custom instance void [mscorlib]System.Runtime.InteropServices.GuidAttribute::.ctor(string) = ( 01 00 24 37 46 43 43 35 46 42 35 2D 34 39 43 30   // ..$7FCC5FB5-49C0
                                                                                                  2D 34 31 44 45 2D 39 39 33 38 2D 33 42 38 38 42   // -41DE-9938-3B88B
                                                                                                  35 42 39 41 44 44 37 00 00 )                      // 5B9ADD7..
  .method public hidebysig newslot abstract virtual 
          instance void  SetJMCStatus([in] int32 bIsJustMyCode,
                                      [in] uint32 cTokens,
                                      [in] uint32& pTokens) runtime managed internalcall
  {
  } // end of method ICorDebugModule2::SetJMCStatus

  .method public hidebysig newslot abstract virtual 
          instance void  ApplyChanges([in] uint32 cbMetadata,
                                      [in] uint8[] marshal([]) pbMetadata,
                                      [in] uint32 cbIL,
                                      [in] uint8[] marshal([]) pbIL) runtime managed internalcall
  {
  } // end of method ICorDebugModule2::ApplyChanges

  .method public hidebysig newslot abstract virtual 
          instance int32  SetJITCompilerFlags([in] uint32 dwFlags) runtime managed internalcall preservesig
  {
  } // end of method ICorDebugModule2::SetJITCompilerFlags

  .method public hidebysig newslot abstract virtual 
          instance void  GetJITCompilerFlags([out] uint32& pdwFlags) runtime managed internalcall
  {
  } // end of method ICorDebugModule2::GetJITCompilerFlags

  .method public hidebysig newslot abstract virtual 
          instance void  ResolveAssembly([in] uint32 tkAssemblyRef,
                                         [out] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugAssembly&  marshal( interface) ppAssembly) runtime managed internalcall
  {
  } // end of method ICorDebugModule2::ResolveAssembly

} // end of class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugModule2

.class interface public abstract auto ansi import Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugFunction2
{
  .custom instance void [mscorlib]System.Runtime.InteropServices.InterfaceTypeAttribute::.ctor(int16) = ( 01 00 01 00 00 00 ) 
  .custom instance void [mscorlib]System.Runtime.InteropServices.GuidAttribute::.ctor(string) = ( 01 00 24 45 46 30 43 34 39 30 42 2D 39 34 43 33   // ..$EF0C490B-94C3
                                                                                                  2D 34 45 34 44 2D 42 36 32 39 2D 44 44 43 31 33   // -4E4D-B629-DDC13
                                                                                                  34 43 35 33 32 44 38 00 00 )                      // 4C532D8..
  .method public hidebysig newslot abstract virtual 
          instance void  SetJMCStatus([in] int32 bIsJustMyCode) runtime managed internalcall
  {
  } // end of method ICorDebugFunction2::SetJMCStatus

  .method public hidebysig newslot abstract virtual 
          instance void  GetJMCStatus([out] int32& pbIsJustMyCode) runtime managed internalcall
  {
  } // end of method ICorDebugFunction2::GetJMCStatus


  // This method was deprecated and actually removed, but we added it back in to preserve vtable layout.
  .method public hidebysig newslot abstract virtual 
          instance void  EnumerateNativeCode([out] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugCodeEnum&  marshal( interface) ppCodeEnum) runtime managed internalcall
  {
  } // end of method ICorDebugFunction2::EnumerateNativeCode

  .method public hidebysig newslot abstract virtual 
          instance void  GetVersionNumber([out] uint32& pnVersion) runtime managed internalcall
  {
  } // end of method ICorDebugFunction2::GetVersionNumber

} // end of class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugFunction2

.class interface public abstract auto ansi import Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugCodeEnum
       implements Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugEnum
{
  .custom instance void [mscorlib]System.Runtime.InteropServices.InterfaceTypeAttribute::.ctor(int16) = ( 01 00 01 00 00 00 ) 
  .custom instance void [mscorlib]System.Runtime.InteropServices.ComConversionLossAttribute::.ctor() = ( 01 00 00 00 ) 
  .custom instance void [mscorlib]System.Runtime.InteropServices.GuidAttribute::.ctor(string) = ( 01 00 24 35 35 45 39 36 34 36 31 2D 39 36 34 35   // ..$55E96461-9645
                                                                                                  2D 34 35 45 34 2D 41 32 46 46 2D 30 33 36 37 38   // -45E4-A2FF-03678
                                                                                                  37 37 41 42 43 44 45 00 00 )                      // 77ABCDE..
  .method public hidebysig newslot abstract virtual 
          instance void  Skip([in] uint32 celt) runtime managed internalcall
  {
  } // end of method ICorDebugCodeEnum::Skip

  .method public hidebysig newslot abstract virtual 
          instance void  Reset() runtime managed internalcall
  {
  } // end of method ICorDebugCodeEnum::Reset

  .method public hidebysig newslot abstract virtual 
          instance void  Clone([out] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugEnum&  marshal( interface) ppEnum) runtime managed internalcall
  {
  } // end of method ICorDebugCodeEnum::Clone

  .method public hidebysig newslot abstract virtual 
          instance void  GetCount([out] uint32& pcelt) runtime managed internalcall
  {
  } // end of method ICorDebugCodeEnum::GetCount

  .method public hidebysig newslot abstract virtual 
          instance int32  Next([in] uint32 celt,
                              [out] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugCode[] marshal([ ]) values,
                              [out] uint32& pceltFetched) runtime managed internalcall preservesig
  {
  } // end of method ICorDebugCodeEnum::Next

} // end of class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugCodeEnum

.class interface public abstract auto ansi import Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugClass2
{
  .custom instance void [mscorlib]System.Runtime.InteropServices.GuidAttribute::.ctor(string) = ( 01 00 24 42 30 30 38 45 41 38 44 2D 37 41 42 31   // ..$B008EA8D-7AB1
                                                                                                  2D 34 33 46 37 2D 42 42 32 30 2D 46 42 42 35 41   // -43F7-BB20-FBB5A
                                                                                                  30 34 30 33 38 41 45 00 00 )                      // 04038AE..
  .custom instance void [mscorlib]System.Runtime.InteropServices.InterfaceTypeAttribute::.ctor(int16) = ( 01 00 01 00 00 00 ) 
  .method public hidebysig newslot abstract virtual 
          instance void  GetParameterizedType([in] valuetype Microsoft.Samples.Debugging.CorDebug.NativeApi.CorElementType elementType,
                                              [in] uint32 nTypeArgs,
                                              [in] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugType[] marshal( [ +1 ]) ppTypeArgs,
                                              [out] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugType&  marshal( interface) ppType) runtime managed internalcall
  {
  } // end of method ICorDebugClass2::GetParameterizedType

  .method public hidebysig newslot abstract virtual 
          instance void  SetJMCStatus([in] int32 bIsJustMyCode) runtime managed internalcall
  {
  } // end of method ICorDebugClass2::SetJMCStatus

} // end of class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugClass2

.class interface public abstract auto ansi import Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugEval2
{
  .custom instance void [mscorlib]System.Runtime.InteropServices.InterfaceTypeAttribute::.ctor(int16) = ( 01 00 01 00 00 00 ) 
  .custom instance void [mscorlib]System.Runtime.InteropServices.GuidAttribute::.ctor(string) = ( 01 00 24 46 42 30 44 39 43 45 37 2D 42 45 36 36   // ..$FB0D9CE7-BE66
                                                                                                  2D 34 36 38 33 2D 39 44 33 32 2D 41 34 32 41 30   // -4683-9D32-A42A0
                                                                                                  34 45 32 46 44 39 31 00 00 )                      // 4E2FD91..
  .method public hidebysig newslot abstract virtual 
          instance void  CallParameterizedFunction([in] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugFunction  marshal( interface) pFunction,
                                                   [in] uint32 nTypeArgs,
                                                   [in] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugType[] marshal( [ +1 ]) ppTypeArgs,
                                                   [in] uint32 nArgs,
                                                   [in] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugValue[] marshal( [ +3 ]) ppArgs) runtime managed internalcall
  {
  } // end of method ICorDebugEval2::CallParameterizedFunction

  .method public hidebysig newslot abstract virtual 
          instance void  CreateValueForType([in] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugType  marshal( interface) pType,
                                            [out] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugValue&  marshal( interface) ppValue) runtime managed internalcall
  {
  } // end of method ICorDebugEval2::CreateValueForType

  .method public hidebysig newslot abstract virtual 
          instance void  NewParameterizedObject([in] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugFunction  marshal( interface) pConstructor,
                                                [in] uint32 nTypeArgs,
                                                [in] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugType[] marshal( [ +1 ]) ppTypeArgs,
                                                [in] uint32 nArgs,
                                                [in] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugValue[] marshal( [ +3 ]) ppArgs) runtime managed internalcall
  {
  } // end of method ICorDebugEval2::NewParameterizedObject

  .method public hidebysig newslot abstract virtual 
          instance void  NewParameterizedObjectNoConstructor([in] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugClass  marshal( interface) pClass,
                                                             [in] uint32 nTypeArgs,
                                                             [in] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugType[] marshal( [ +1 ]) ppTypeArgs) runtime managed internalcall
  {
  } // end of method ICorDebugEval2::NewParameterizedObjectNoConstructor

  .method public hidebysig newslot abstract virtual 
          instance void  NewParameterizedArray([in] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugType  marshal( interface) pElementType,
                                               [in] uint32 rank,
                                               [in] uint32& dims,
                                               [in] uint32& lowBounds) runtime managed internalcall
  {
  } // end of method ICorDebugEval2::NewParameterizedArray

  .method public hidebysig newslot abstract virtual 
          instance void  NewStringWithLength([in] string  marshal( lpwstr) 'string',
                                             [in] uint32 uiLength) runtime managed internalcall
  {
  } // end of method ICorDebugEval2::NewStringWithLength

  .method public hidebysig newslot abstract virtual 
          instance void  RudeAbort() runtime managed internalcall
  {
  } // end of method ICorDebugEval2::RudeAbort

} // end of class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugEval2

.class interface public abstract auto ansi import Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugValue2
{
  .custom instance void [mscorlib]System.Runtime.InteropServices.InterfaceTypeAttribute::.ctor(int16) = ( 01 00 01 00 00 00 ) 
  .custom instance void [mscorlib]System.Runtime.InteropServices.GuidAttribute::.ctor(string) = ( 01 00 24 35 45 30 42 35 34 45 37 2D 44 38 38 41   // ..$5E0B54E7-D88A
                                                                                                  2D 34 36 32 36 2D 39 34 32 30 2D 41 36 39 31 45   // -4626-9420-A691E
                                                                                                  30 41 37 38 42 34 39 00 00 )                      // 0A78B49..
  .method public hidebysig newslot abstract virtual 
          instance void  GetExactType([out] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugType&  marshal( interface) ppType) runtime managed internalcall
  {
  } // end of method ICorDebugValue2::GetExactType

} // end of class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugValue2

.class interface public abstract auto ansi import Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugObjectValue2
{
  .custom instance void [mscorlib]System.Runtime.InteropServices.InterfaceTypeAttribute::.ctor(int16) = ( 01 00 01 00 00 00 ) 
  .custom instance void [mscorlib]System.Runtime.InteropServices.GuidAttribute::.ctor(string) = ( 01 00 24 34 39 45 34 41 33 32 30 2D 34 41 39 42   // ..$49E4A320-4A9B
                                                                                                  2D 34 45 43 41 2D 42 31 30 35 2D 32 32 39 46 42   // -4ECA-B105-229FB
                                                                                                  37 44 35 30 30 39 46 00 00 )                      // 7D5009F..
  .method public hidebysig newslot abstract virtual 
          instance void  GetVirtualMethodAndType([in] uint32 memberRef,
                                                 [out] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugFunction&  marshal( interface) ppFunction,
                                                 [out] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugType&  marshal( interface) ppType) runtime managed internalcall
  {
  } // end of method ICorDebugObjectValue2::GetVirtualMethodAndType

} // end of class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugObjectValue2

.class interface public abstract auto ansi import Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugHandleValue
       implements Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugReferenceValue
{
  .custom instance void [mscorlib]System.Runtime.InteropServices.GuidAttribute::.ctor(string) = ( 01 00 24 30 32 39 35 39 36 45 38 2D 32 37 36 42   // ..$029596E8-276B
                                                                                                  2D 34 36 41 31 2D 39 38 32 31 2D 37 33 32 45 39   // -46A1-9821-732E9
                                                                                                  36 42 42 42 30 30 42 00 00 )                      // 6BBB00B..
  .custom instance void [mscorlib]System.Runtime.InteropServices.InterfaceTypeAttribute::.ctor(int16) = ( 01 00 01 00 00 00 ) 
  .method public hidebysig newslot abstract virtual 
          instance void  GetType([out] valuetype Microsoft.Samples.Debugging.CorDebug.NativeApi.CorElementType& pType) runtime managed internalcall
  {
  } // end of method ICorDebugHandleValue::GetType

  .method public hidebysig newslot abstract virtual 
          instance void  GetSize([out] uint32& pSize) runtime managed internalcall
  {
  } // end of method ICorDebugHandleValue::GetSize

  .method public hidebysig newslot abstract virtual 
          instance void  GetAddress([out] uint64& pAddress) runtime managed internalcall
  {
  } // end of method ICorDebugHandleValue::GetAddress

  .method public hidebysig newslot abstract virtual 
          instance void  CreateBreakpoint([out] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugValueBreakpoint&  marshal( interface) ppBreakpoint) runtime managed internalcall
  {
  } // end of method ICorDebugHandleValue::CreateBreakpoint

  .method public hidebysig newslot abstract virtual 
          instance void  IsNull([out] int32& pbNull) runtime managed internalcall
  {
  } // end of method ICorDebugHandleValue::IsNull

  .method public hidebysig newslot abstract virtual 
          instance void  GetValue([out] uint64& pValue) runtime managed internalcall
  {
  } // end of method ICorDebugHandleValue::GetValue

  .method public hidebysig newslot abstract virtual 
          instance void  SetValue([in] uint64 'value') runtime managed internalcall
  {
  } // end of method ICorDebugHandleValue::SetValue

  .method public hidebysig newslot abstract virtual 
          instance void  Dereference([out] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugValue&  marshal( interface) ppValue) runtime managed internalcall
  {
  } // end of method ICorDebugHandleValue::Dereference

  .method public hidebysig newslot abstract virtual 
          instance void  DereferenceStrong([out] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugValue&  marshal( interface) ppValue) runtime managed internalcall
  {
  } // end of method ICorDebugHandleValue::DereferenceStrong

  .method public hidebysig newslot abstract virtual 
          instance void  GetHandleType([out] valuetype Microsoft.Samples.Debugging.CorDebug.NativeApi.CorDebugHandleType& pType) runtime managed internalcall
  {
  } // end of method ICorDebugHandleValue::GetHandleType

  .method public hidebysig newslot abstract virtual 
          instance void  Dispose() runtime managed internalcall
  {
  } // end of method ICorDebugHandleValue::Dispose

} // end of class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugHandleValue

.class public auto ansi sealed Microsoft.Samples.Debugging.CorDebug.NativeApi.CorDebugHandleType
       extends [mscorlib]System.Enum
{
  .field public specialname rtspecialname int32 value__
  .field public static literal valuetype Microsoft.Samples.Debugging.CorDebug.NativeApi.CorDebugHandleType HANDLE_STRONG = int32(0x00000001)
  .field public static literal valuetype Microsoft.Samples.Debugging.CorDebug.NativeApi.CorDebugHandleType HANDLE_WEAK_TRACK_RESURRECTION = int32(0x00000002)
} // end of class Microsoft.Samples.Debugging.CorDebug.NativeApi.CorDebugHandleType

.class interface public abstract auto ansi import Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugHeapValue2
{
  .custom instance void [mscorlib]System.Runtime.InteropServices.InterfaceTypeAttribute::.ctor(int16) = ( 01 00 01 00 00 00 ) 
  .custom instance void [mscorlib]System.Runtime.InteropServices.GuidAttribute::.ctor(string) = ( 01 00 24 45 33 41 43 34 44 36 43 2D 39 43 42 37   // ..$E3AC4D6C-9CB7
                                                                                                  2D 34 33 45 36 2D 39 36 43 43 2D 42 32 31 35 34   // -43E6-96CC-B2154
                                                                                                  30 45 35 30 38 33 43 00 00 )                      // 0E5083C..
  .method public hidebysig newslot abstract virtual 
          instance void  CreateHandle([in] valuetype Microsoft.Samples.Debugging.CorDebug.NativeApi.CorDebugHandleType 'type',
                                      [out] class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugHandleValue&  marshal( interface) ppHandle) runtime managed internalcall
  {
  } // end of method ICorDebugHeapValue2::CreateHandle

} // end of class Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugHeapValue2

// =============================================================

// has to be kept in sync with clr/src/inc/CorHdr.h, see comments there
.class public auto ansi sealed Microsoft.Samples.Debugging.CorDebug.NativeApi.CorElementType
       extends [mscorlib]System.Enum
{
  .custom instance void [mscorlib]System.FlagsAttribute::.ctor() = ( 01 00 00 00 ) 
  .custom instance void [mscorlib]System.CLSCompliantAttribute::.ctor(bool) = ( 01 00 01 00 00 ) 
  .field public specialname rtspecialname int32 value__
  .field public static literal valuetype Microsoft.Samples.Debugging.CorDebug.NativeApi.CorElementType ELEMENT_TYPE_END = int32(0x00000000)
  .field public static literal valuetype Microsoft.Samples.Debugging.CorDebug.NativeApi.CorElementType ELEMENT_TYPE_VOID = int32(0x00000001)
  .field public static literal valuetype Microsoft.Samples.Debugging.CorDebug.NativeApi.CorElementType ELEMENT_TYPE_BOOLEAN = int32(0x00000002)
  .field public static literal valuetype Microsoft.Samples.Debugging.CorDebug.NativeApi.CorElementType ELEMENT_TYPE_CHAR = int32(0x00000003)
  .field public static literal valuetype Microsoft.Samples.Debugging.CorDebug.NativeApi.CorElementType ELEMENT_TYPE_I1 = int32(0x00000004)
  .field public static literal valuetype Microsoft.Samples.Debugging.CorDebug.NativeApi.CorElementType ELEMENT_TYPE_U1 = int32(0x00000005)
  .field public static literal valuetype Microsoft.Samples.Debugging.CorDebug.NativeApi.CorElementType ELEMENT_TYPE_I2 = int32(0x00000006)
  .field public static literal valuetype Microsoft.Samples.Debugging.CorDebug.NativeApi.CorElementType ELEMENT_TYPE_U2 = int32(0x00000007)
  .field public static literal valuetype Microsoft.Samples.Debugging.CorDebug.NativeApi.CorElementType ELEMENT_TYPE_I4 = int32(0x00000008)
  .field public static literal valuetype Microsoft.Samples.Debugging.CorDebug.NativeApi.CorElementType ELEMENT_TYPE_U4 = int32(0x00000009)
  .field public static literal valuetype Microsoft.Samples.Debugging.CorDebug.NativeApi.CorElementType ELEMENT_TYPE_I8 = int32(0x0000000A)
  .field public static literal valuetype Microsoft.Samples.Debugging.CorDebug.NativeApi.CorElementType ELEMENT_TYPE_U8 = int32(0x0000000B)
  .field public static literal valuetype Microsoft.Samples.Debugging.CorDebug.NativeApi.CorElementType ELEMENT_TYPE_R4 = int32(0x0000000C)
  .field public static literal valuetype Microsoft.Samples.Debugging.CorDebug.NativeApi.CorElementType ELEMENT_TYPE_R8 = int32(0x0000000D)
  .field public static literal valuetype Microsoft.Samples.Debugging.CorDebug.NativeApi.CorElementType ELEMENT_TYPE_STRING = int32(0x0000000E)
  .field public static literal valuetype Microsoft.Samples.Debugging.CorDebug.NativeApi.CorElementType ELEMENT_TYPE_PTR = int32(0x0000000F)
  .field public static literal valuetype Microsoft.Samples.Debugging.CorDebug.NativeApi.CorElementType ELEMENT_TYPE_BYREF = int32(0x00000010)
  .field public static literal valuetype Microsoft.Samples.Debugging.CorDebug.NativeApi.CorElementType ELEMENT_TYPE_VALUETYPE = int32(0x00000011)
  .field public static literal valuetype Microsoft.Samples.Debugging.CorDebug.NativeApi.CorElementType ELEMENT_TYPE_CLASS = int32(0x00000012)
  .field public static literal valuetype Microsoft.Samples.Debugging.CorDebug.NativeApi.CorElementType ELEMENT_TYPE_ARRAY = int32(0x00000014)
  .field public static literal valuetype Microsoft.Samples.Debugging.CorDebug.NativeApi.CorElementType ELEMENT_TYPE_TYPEDBYREF = int32(0x00000016)
  .field public static literal valuetype Microsoft.Samples.Debugging.CorDebug.NativeApi.CorElementType ELEMENT_TYPE_I = int32(0x00000018)
  .field public static literal valuetype Microsoft.Samples.Debugging.CorDebug.NativeApi.CorElementType ELEMENT_TYPE_U = int32(0x00000019)
  .field public static literal valuetype Microsoft.Samples.Debugging.CorDebug.NativeApi.CorElementType ELEMENT_TYPE_FNPTR = int32(0x0000001B)
  .field public static literal valuetype Microsoft.Samples.Debugging.CorDebug.NativeApi.CorElementType ELEMENT_TYPE_OBJECT = int32(0x0000001C)
  .field public static literal valuetype Microsoft.Samples.Debugging.CorDebug.NativeApi.CorElementType ELEMENT_TYPE_SZARRAY = int32(0x0000001D)
  .field public static literal valuetype Microsoft.Samples.Debugging.CorDebug.NativeApi.CorElementType ELEMENT_TYPE_CMOD_REQD = int32(0x0000001F)
  .field public static literal valuetype Microsoft.Samples.Debugging.CorDebug.NativeApi.CorElementType ELEMENT_TYPE_CMOD_OPT = int32(0x00000020)
  .field public static literal valuetype Microsoft.Samples.Debugging.CorDebug.NativeApi.CorElementType ELEMENT_TYPE_INTERNAL = int32(0x00000021)
  .field public static literal valuetype Microsoft.Samples.Debugging.CorDebug.NativeApi.CorElementType ELEMENT_TYPE_MAX = int32(0x00000022)
  .field public static literal valuetype Microsoft.Samples.Debugging.CorDebug.NativeApi.CorElementType ELEMENT_TYPE_MODIFIER = int32(0x00000040)
  .field public static literal valuetype Microsoft.Samples.Debugging.CorDebug.NativeApi.CorElementType ELEMENT_TYPE_SENTINEL = int32(0x00000041)
  .field public static literal valuetype Microsoft.Samples.Debugging.CorDebug.NativeApi.CorElementType ELEMENT_TYPE_PINNED = int32(0x00000045)
} // end of class Microsoft.Samples.Debugging.CorDebug.NativeApi.CorElementType

// has to be kept in sync with cordebug.idl
.class public auto ansi sealed Microsoft.Samples.Debugging.CorDebug.NativeApi.CorDebugMDAFlags
       extends [mscorlib]System.Enum
{
  .custom instance void [mscorlib]System.FlagsAttribute::.ctor() = ( 01 00 00 00 ) 
  .custom instance void [mscorlib]System.CLSCompliantAttribute::.ctor(bool) = ( 01 00 01 00 00 ) 
  .field public specialname rtspecialname int32 value__
  .field public static literal valuetype Microsoft.Samples.Debugging.CorDebug.NativeApi.CorDebugMDAFlags MDA_FLAG_SEVERE = int32(0x00000001)
  .field public static literal valuetype Microsoft.Samples.Debugging.CorDebug.NativeApi.CorDebugMDAFlags MDA_FLAG_SLIP = int32(0x00000002)
} // end of class Microsoft.Samples.Debugging.CorDebug.NativeApi.CorDebugMDAFlags
