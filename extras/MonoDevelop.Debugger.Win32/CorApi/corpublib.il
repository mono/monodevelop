//---------------------------------------------------------------------
//  This file is part of the CLR Managed Debugger (mdbg) Sample.
// 
//  Copyright (C) Microsoft Corporation.  All rights reserved.
//---------------------------------------------------------------------

//  Microsoft (R) .NET Framework IL Disassembler.  Version 1.0.3705.0
//  Copyright (C) Microsoft Corporation 1998-2001. All rights reserved.

// ============== CLASS STRUCTURE DECLARATION ==================
//
.namespace Microsoft.Samples.Debugging.CorPublish.NativeApi
{
  .class public auto ansi import CorpubPublishClass
         extends [mscorlib]System.Object
         implements Microsoft.Samples.Debugging.CorPublish.NativeApi.ICorPublish,
                    Microsoft.Samples.Debugging.CorPublish.NativeApi.CorpubPublish
  {
  } // end of class CorpubPublishClass

  .class interface public abstract auto ansi import CorpubPublish
         implements Microsoft.Samples.Debugging.CorPublish.NativeApi.ICorPublish
  {
  } // end of class CorpubPublish

  .class interface public abstract auto ansi import ICorPublish
  {
  } // end of class ICorPublish

  .class public auto ansi sealed COR_PUB_ENUMPROCESS
         extends [mscorlib]System.Enum
  {
  } // end of class COR_PUB_ENUMPROCESS

  .class public auto ansi sealed __MIDL___MIDL_itf_corpub_0000_0001
         extends [mscorlib]System.Enum
  {
  } // end of class __MIDL___MIDL_itf_corpub_0000_0001

  .class interface public abstract auto ansi import ICorPublishProcessEnum
         implements Microsoft.Samples.Debugging.CorPublish.NativeApi.ICorPublishEnum
  {
  } // end of class ICorPublishProcessEnum

  .class interface public abstract auto ansi import ICorPublishEnum
  {
  } // end of class ICorPublishEnum

  .class interface public abstract auto ansi import ICorPublishProcess
  {
  } // end of class ICorPublishProcess

  .class interface public abstract auto ansi import ICorPublishAppDomainEnum
         implements Microsoft.Samples.Debugging.CorPublish.NativeApi.ICorPublishEnum
  {
  } // end of class ICorPublishAppDomainEnum

  .class interface public abstract auto ansi import ICorPublishAppDomain
  {
  } // end of class ICorPublishAppDomain

} // end of namespace Microsoft.Samples.Debugging.CorPublish.NativeApi


// =============================================================


// =============== GLOBAL FIELDS AND METHODS ===================


// =============================================================


// =============== CLASS MEMBERS DECLARATION ===================
//   note that class flags, 'extends' and 'implements' clauses
//          are provided here for information only

.namespace Microsoft.Samples.Debugging.CorPublish.NativeApi
{
  .class public auto ansi import CorpubPublishClass
         extends [mscorlib]System.Object
         implements Microsoft.Samples.Debugging.CorPublish.NativeApi.ICorPublish,
                    Microsoft.Samples.Debugging.CorPublish.NativeApi.CorpubPublish
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.ClassInterfaceAttribute::.ctor(int16) = ( 01 00 00 00 00 00 ) 
    .custom instance void [mscorlib]System.Runtime.InteropServices.GuidAttribute::.ctor(string) = ( 01 00 24 30 34 37 61 39 61 34 30 2D 36 35 37 65   // ..$047a9a40-657e
                                                                                                    2D 31 31 64 33 2D 38 64 35 62 2D 30 30 31 30 34   // -11d3-8d5b-00104
                                                                                                    62 33 35 65 37 65 66 00 00 )                      // b35e7ef..
    .custom instance void [mscorlib]System.Runtime.InteropServices.TypeLibTypeAttribute::.ctor(int16) = ( 01 00 02 00 00 00 ) 
    .method public specialname rtspecialname 
            instance void  .ctor() runtime managed internalcall
    {
    } // end of method CorpubPublishClass::.ctor

    .method public hidebysig newslot virtual 
            instance void  EnumProcesses([in] valuetype Microsoft.Samples.Debugging.CorPublish.NativeApi.COR_PUB_ENUMPROCESS Type,
                                         [out] class Microsoft.Samples.Debugging.CorPublish.NativeApi.ICorPublishProcessEnum&  marshal( interface) ppIEnum) runtime managed internalcall
    {
      .param [1]
      .custom instance void [mscorlib]System.Runtime.InteropServices.ComAliasNameAttribute::.ctor(string) = ( 01 00 24 43 6F 72 70 75 62 50 72 6F 63 65 73 73   // ..$CorpubProcess
                                                                                                              4C 69 62 2E 43 4F 52 5F 50 55 42 5F 45 4E 55 4D   // Lib.COR_PUB_ENUM
                                                                                                              50 52 4F 43 45 53 53 00 00 )                      // PROCESS..
      .override Microsoft.Samples.Debugging.CorPublish.NativeApi.ICorPublish::EnumProcesses
    } // end of method CorpubPublishClass::EnumProcesses

    .method public hidebysig newslot virtual 
            instance void  GetProcess([in] unsigned int32 pid,
                                      [out] class Microsoft.Samples.Debugging.CorPublish.NativeApi.ICorPublishProcess&  marshal( interface) ppProcess) runtime managed internalcall
    {
      .override Microsoft.Samples.Debugging.CorPublish.NativeApi.ICorPublish::GetProcess
    } // end of method CorpubPublishClass::GetProcess

  } // end of class CorpubPublishClass

  .class interface public abstract auto ansi import CorpubPublish
         implements Microsoft.Samples.Debugging.CorPublish.NativeApi.ICorPublish
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.CoClassAttribute::.ctor(class [mscorlib]System.Type) = ( 01 00 43 4D 69 63 72 6F 73 6F 66 74 2E 53 61 6D   // ..CMicrosoft.Sam
                                                                                                                          70 6C 65 73 2E 44 65 62 75 67 67 69 6E 67 2E 43   // ples.Debugging.C
                                                                                                                          6F 72 50 75 62 6C 69 73 68 2E 4E 61 74 69 76 65   // orPublish.Native
                                                                                                                          41 70 69 2E 43 6F 72 70 75 62 50 75 62 6C 69 73   // Api.CorpubPublis
                                                                                                                          68 43 6C 61 73 73 00 00 )                         // hClass..
    .custom instance void [mscorlib]System.Runtime.InteropServices.GuidAttribute::.ctor(string) = ( 01 00 24 39 36 31 33 41 30 45 37 2D 35 41 36 38   // ..$9613A0E7-5A68
                                                                                                    2D 31 31 44 33 2D 38 46 38 34 2D 30 30 41 30 43   // -11D3-8F84-00A0C
                                                                                                    39 42 34 44 35 30 43 00 00 )                      // 9B4D50C..
  } // end of class CorpubPublish

  .class interface public abstract auto ansi import ICorPublish
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.GuidAttribute::.ctor(string) = ( 01 00 24 39 36 31 33 41 30 45 37 2D 35 41 36 38   // ..$9613A0E7-5A68
                                                                                                    2D 31 31 44 33 2D 38 46 38 34 2D 30 30 41 30 43   // -11D3-8F84-00A0C
                                                                                                    39 42 34 44 35 30 43 00 00 )                      // 9B4D50C..
    .custom instance void [mscorlib]System.Runtime.InteropServices.InterfaceTypeAttribute::.ctor(int16) = ( 01 00 01 00 00 00 ) 
    .method public hidebysig newslot virtual abstract 
            instance void  EnumProcesses([in] valuetype Microsoft.Samples.Debugging.CorPublish.NativeApi.COR_PUB_ENUMPROCESS Type,
                                         [out] class Microsoft.Samples.Debugging.CorPublish.NativeApi.ICorPublishProcessEnum&  marshal( interface) ppIEnum) runtime managed internalcall
    {
      .param [1]
      .custom instance void [mscorlib]System.Runtime.InteropServices.ComAliasNameAttribute::.ctor(string) = ( 01 00 24 43 6F 72 70 75 62 50 72 6F 63 65 73 73   // ..$CorpubProcess
                                                                                                              4C 69 62 2E 43 4F 52 5F 50 55 42 5F 45 4E 55 4D   // Lib.COR_PUB_ENUM
                                                                                                              50 52 4F 43 45 53 53 00 00 )                      // PROCESS..
    } // end of method ICorPublish::EnumProcesses

    .method public hidebysig newslot virtual abstract 
            instance void  GetProcess([in] unsigned int32 pid,
                                      [out] class Microsoft.Samples.Debugging.CorPublish.NativeApi.ICorPublishProcess&  marshal( interface) ppProcess) runtime managed internalcall
    {
    } // end of method ICorPublish::GetProcess

  } // end of class ICorPublish

  .class public auto ansi sealed COR_PUB_ENUMPROCESS
         extends [mscorlib]System.Enum
  {
    .field public specialname rtspecialname int32 value__
    .field public static literal valuetype Microsoft.Samples.Debugging.CorPublish.NativeApi.COR_PUB_ENUMPROCESS COR_PUB_MANAGEDONLY = int32(0x00000001)
  } // end of class COR_PUB_ENUMPROCESS

  .class public auto ansi sealed __MIDL___MIDL_itf_corpub_0000_0001
         extends [mscorlib]System.Enum
  {
    .field public specialname rtspecialname int32 value__
    .field public static literal valuetype Microsoft.Samples.Debugging.CorPublish.NativeApi.__MIDL___MIDL_itf_corpub_0000_0001 COR_PUB_MANAGEDONLY = int32(0x00000001)
  } // end of class __MIDL___MIDL_itf_corpub_0000_0001

  .class interface public abstract auto ansi import ICorPublishProcessEnum
         implements Microsoft.Samples.Debugging.CorPublish.NativeApi.ICorPublishEnum
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.InterfaceTypeAttribute::.ctor(int16) = ( 01 00 01 00 00 00 ) 
    .custom instance void [mscorlib]System.Runtime.InteropServices.GuidAttribute::.ctor(string) = ( 01 00 24 41 33 37 46 42 44 34 31 2D 35 41 36 39   // ..$A37FBD41-5A69
                                                                                                    2D 31 31 44 33 2D 38 46 38 34 2D 30 30 41 30 43   // -11D3-8F84-00A0C
                                                                                                    39 42 34 44 35 30 43 00 00 )                      // 9B4D50C..
    .method public hidebysig newslot virtual abstract 
            instance void  Skip([in] unsigned int32 celt) runtime managed internalcall
    {
    } // end of method ICorPublishProcessEnum::Skip

    .method public hidebysig newslot virtual abstract 
            instance void  Reset() runtime managed internalcall
    {
    } // end of method ICorPublishProcessEnum::Reset

    .method public hidebysig newslot virtual abstract 
            instance void  Clone([out] class Microsoft.Samples.Debugging.CorPublish.NativeApi.ICorPublishEnum&  marshal( interface) ppEnum) runtime managed internalcall
    {
    } // end of method ICorPublishProcessEnum::Clone

    .method public hidebysig newslot virtual abstract 
            instance void  GetCount([out] unsigned int32& pcelt) runtime managed internalcall
    {
    } // end of method ICorPublishProcessEnum::GetCount

    .method public hidebysig newslot virtual abstract 
            instance int32  Next([in] unsigned int32 celt,
                                 [out] class Microsoft.Samples.Debugging.CorPublish.NativeApi.ICorPublishProcess&  marshal( interface) objects,
                                [out] unsigned int32& pceltFetched) runtime managed internalcall preservesig
    {
    } // end of method ICorPublishProcessEnum::Next

  } // end of class ICorPublishProcessEnum

  .class interface public abstract auto ansi import ICorPublishEnum
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.GuidAttribute::.ctor(string) = ( 01 00 24 43 30 42 32 32 39 36 37 2D 35 41 36 39   // ..$C0B22967-5A69
                                                                                                    2D 31 31 44 33 2D 38 46 38 34 2D 30 30 41 30 43   // -11D3-8F84-00A0C
                                                                                                    39 42 34 44 35 30 43 00 00 )                      // 9B4D50C..
    .custom instance void [mscorlib]System.Runtime.InteropServices.InterfaceTypeAttribute::.ctor(int16) = ( 01 00 01 00 00 00 ) 
    .method public hidebysig newslot virtual abstract 
            instance void  Skip([in] unsigned int32 celt) runtime managed internalcall
    {
    } // end of method ICorPublishEnum::Skip

    .method public hidebysig newslot virtual abstract 
            instance void  Reset() runtime managed internalcall
    {
    } // end of method ICorPublishEnum::Reset

    .method public hidebysig newslot virtual abstract 
            instance void  Clone([out] class Microsoft.Samples.Debugging.CorPublish.NativeApi.ICorPublishEnum&  marshal( interface) ppEnum) runtime managed internalcall
    {
    } // end of method ICorPublishEnum::Clone

    .method public hidebysig newslot virtual abstract 
            instance void  GetCount([out] unsigned int32& pcelt) runtime managed internalcall
    {
    } // end of method ICorPublishEnum::GetCount

  } // end of class ICorPublishEnum

  .class interface public abstract auto ansi import ICorPublishProcess
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.GuidAttribute::.ctor(string) = ( 01 00 24 31 38 44 38 37 41 46 31 2D 35 41 36 41   // ..$18D87AF1-5A6A
                                                                                                    2D 31 31 44 33 2D 38 46 38 34 2D 30 30 41 30 43   // -11D3-8F84-00A0C
                                                                                                    39 42 34 44 35 30 43 00 00 )                      // 9B4D50C..
    .custom instance void [mscorlib]System.Runtime.InteropServices.InterfaceTypeAttribute::.ctor(int16) = ( 01 00 01 00 00 00 ) 
    .method public hidebysig newslot virtual abstract 
            instance void  IsManaged([out] int32& pbManaged) runtime managed internalcall
    {
    } // end of method ICorPublishProcess::IsManaged

    .method public hidebysig newslot virtual abstract 
            instance void  EnumAppDomains([out] class Microsoft.Samples.Debugging.CorPublish.NativeApi.ICorPublishAppDomainEnum&  marshal( interface) ppEnum) runtime managed internalcall
    {
    } // end of method ICorPublishProcess::EnumAppDomains

    .method public hidebysig newslot virtual abstract 
            instance void  GetProcessID([out] unsigned int32& pid) runtime managed internalcall
    {
    } // end of method ICorPublishProcess::GetProcessID

    .method public hidebysig newslot virtual abstract 
            instance void  GetDisplayName([in] unsigned int32 cchName,
                                          [out] unsigned int32& pcchName,
                          		// REPLACED          [out] unsigned int16& szName
										 [out] class [mscorlib]System.Text.StringBuilder  marshal( lpwstr) szName

										) runtime managed internalcall
    {
    } // end of method ICorPublishProcess::GetDisplayName

  } // end of class ICorPublishProcess

  .class interface public abstract auto ansi import ICorPublishAppDomainEnum
         implements Microsoft.Samples.Debugging.CorPublish.NativeApi.ICorPublishEnum
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.GuidAttribute::.ctor(string) = ( 01 00 24 39 46 30 43 39 38 46 35 2D 35 41 36 41   // ..$9F0C98F5-5A6A
                                                                                                    2D 31 31 44 33 2D 38 46 38 34 2D 30 30 41 30 43   // -11D3-8F84-00A0C
                                                                                                    39 42 34 44 35 30 43 00 00 )                      // 9B4D50C..
    .custom instance void [mscorlib]System.Runtime.InteropServices.InterfaceTypeAttribute::.ctor(int16) = ( 01 00 01 00 00 00 ) 
    .method public hidebysig newslot virtual abstract 
            instance void  Skip([in] unsigned int32 celt) runtime managed internalcall
    {
    } // end of method ICorPublishAppDomainEnum::Skip

    .method public hidebysig newslot virtual abstract 
            instance void  Reset() runtime managed internalcall
    {
    } // end of method ICorPublishAppDomainEnum::Reset

    .method public hidebysig newslot virtual abstract 
            instance void  Clone([out] class Microsoft.Samples.Debugging.CorPublish.NativeApi.ICorPublishEnum&  marshal( interface) ppEnum) runtime managed internalcall
    {
    } // end of method ICorPublishAppDomainEnum::Clone

    .method public hidebysig newslot virtual abstract 
            instance void  GetCount([out] unsigned int32& pcelt) runtime managed internalcall
    {
    } // end of method ICorPublishAppDomainEnum::GetCount

    .method public hidebysig newslot virtual abstract 
            instance int32  Next([in] unsigned int32 celt,
                             [out] class Microsoft.Samples.Debugging.CorPublish.NativeApi.ICorPublishAppDomain&  marshal( interface) objects,
                             [out] unsigned int32& pceltFetched) runtime managed internalcall preservesig
    {
    } // end of method ICorPublishAppDomainEnum::Next

  } // end of class ICorPublishAppDomainEnum

  .class interface public abstract auto ansi import ICorPublishAppDomain
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.GuidAttribute::.ctor(string) = ( 01 00 24 44 36 33 31 35 43 38 46 2D 35 41 36 41   // ..$D6315C8F-5A6A
                                                                                                    2D 31 31 44 33 2D 38 46 38 34 2D 30 30 41 30 43   // -11D3-8F84-00A0C
                                                                                                    39 42 34 44 35 30 43 00 00 )                      // 9B4D50C..
    .custom instance void [mscorlib]System.Runtime.InteropServices.InterfaceTypeAttribute::.ctor(int16) = ( 01 00 01 00 00 00 ) 
    .method public hidebysig newslot virtual abstract 
            instance void  GetID([out] unsigned int32& puId) runtime managed internalcall
    {
    } // end of method ICorPublishAppDomain::GetID

    .method public hidebysig newslot virtual abstract 
            instance void  GetName([in] unsigned int32 cchName,
                                   [out] unsigned int32& pcchName,
                            // REPLACED       [out] unsigned int16& szName
								   [out] class [mscorlib]System.Text.StringBuilder  marshal( lpwstr) szName
								   ) runtime managed internalcall
    {
    } // end of method ICorPublishAppDomain::GetName

  } // end of class ICorPublishAppDomain


// =============================================================

} // end of namespace Microsoft.Samples.Debugging.CorPublish.NativeApi

//*********** DISASSEMBLY COMPLETE ***********************
// WARNING: Created Win32 resource file corpublib.res
