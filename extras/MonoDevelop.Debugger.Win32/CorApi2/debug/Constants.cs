//---------------------------------------------------------------------
//  This file is part of the CLR Managed Debugger (mdbg) Sample.
// 
//  Copyright (C) Microsoft Corporation.  All rights reserved.
//---------------------------------------------------------------------
using System;
using System.Diagnostics;
using System.Globalization;

using Microsoft.Samples.Debugging.CorDebug.NativeApi;

namespace Microsoft.Samples.Debugging.CorDebug
{

    [Flags]
    public enum ContextFlags
    {
        CONTEXT_CONTROL = 0x00010001,
        CONTEXT_INTEGER = 0x00010002,
        CONTEXT_SEGMENTS = 0x00010004,
        CONTEXT_FLOATING_POINT = 0x00010008,
        CONTEXT_DEBUG_REGISTERS = 0x00010010,
        CONTEXT_EXTENDED_REGISTERS = 0x00010020,
        CONTEXT_FULL = CONTEXT_CONTROL + CONTEXT_INTEGER + CONTEXT_SEGMENTS,
        CONTEXT_ALL  = CONTEXT_CONTROL + CONTEXT_INTEGER + CONTEXT_FLOATING_POINT + CONTEXT_DEBUG_REGISTERS + CONTEXT_EXTENDED_REGISTERS
    }

    public enum CorDebuggerVersion
    {
        RTM     = 1, //v1.0
        Everett = 2, //v1.1
        Whidbey = 3, //v2.0
    }

    // copied from Cordebug.idl
    [Flags]
    public enum CorDebugJITCompilerFlags
    {
        CORDEBUG_JIT_DEFAULT = 0x1,
        CORDEBUG_JIT_DISABLE_OPTIMIZATION = 0x3,
        CORDEBUG_JIT_ENABLE_ENC = 0x7
    }

    // keep in sync with CorHdr.h
    public enum CorTokenType
    {
        mdtModule               = 0x00000000,       //          
        mdtTypeRef              = 0x01000000,       //          
        mdtTypeDef              = 0x02000000,       //          
        mdtFieldDef             = 0x04000000,       //           
        mdtMethodDef            = 0x06000000,       //       
        mdtParamDef             = 0x08000000,       //           
        mdtInterfaceImpl        = 0x09000000,       //  
        mdtMemberRef            = 0x0a000000,       //       
        mdtCustomAttribute      = 0x0c000000,       //      
        mdtPermission           = 0x0e000000,       //       
        mdtSignature            = 0x11000000,       //       
        mdtEvent                = 0x14000000,       //           
        mdtProperty             = 0x17000000,       //           
        mdtModuleRef            = 0x1a000000,       //       
        mdtTypeSpec             = 0x1b000000,       //           
        mdtAssembly             = 0x20000000,       //
        mdtAssemblyRef          = 0x23000000,       //
        mdtFile                 = 0x26000000,       //
        mdtExportedType         = 0x27000000,       //
        mdtManifestResource     = 0x28000000,       //
        mdtGenericParam         = 0x2a000000,       //
        mdtMethodSpec           = 0x2b000000,       //
        mdtGenericParamConstraint = 0x2c000000,
        
        mdtString               = 0x70000000,       //          
        mdtName                 = 0x71000000,       //
        mdtBaseType             = 0x72000000,       // Leave this on the high end value. This does not correspond to metadata table
    }

    public abstract class TokenUtils
    {
        public static CorTokenType TypeFromToken(int token)
        {
            return (CorTokenType) ((UInt32)token & 0xff000000);
        }

        public static int RidFromToken(int token)
        {
            return (int)( (UInt32)token & 0x00ffffff);
        }

        public static bool IsNullToken(int token)
        {
            return (RidFromToken(token)==0);
        }
    }

#if MDBG_FEATURE_INTEROP
    // Native debug event Codes that are returned through NativeStop event
    public enum NativeDebugEventCode
    {
        EXCEPTION_DEBUG_EVENT       =1,
        CREATE_THREAD_DEBUG_EVENT   =2,
        CREATE_PROCESS_DEBUG_EVENT  =3,
        EXIT_THREAD_DEBUG_EVENT     =4,
        EXIT_PROCESS_DEBUG_EVENT    =5,
        LOAD_DLL_DEBUG_EVENT        =6,
        UNLOAD_DLL_DEBUG_EVENT      =7,
        OUTPUT_DEBUG_STRING_EVENT   =8,
        RIP_EVENT                   =9,
    }

    public static class NativeDebugEvent
    {
        /// <summary>
        /// Given a LoadModule debug event (and the process), get the ImageName
        /// </summary>
        /// <param name="corProcess"> The CorProcess where this image is loaded.</param>
        /// <param name="eventLoadDll"> The LOAD_DLL_DEBUG_INFO event.</param>
        /// <returns> The image name or null if it couldn't be determined from the event</returns>
        private static string GetImageNameFromDebugEvent(CorProcess corProcess, LOAD_DLL_DEBUG_INFO eventLoadDll)
        {
            string moduleName;
            bool bUnicode = eventLoadDll.fUnicode!=0;
            
            if(eventLoadDll.lpImageName == IntPtr.Zero)
            {
                return null;
            }
            else
            {
                byte[] buffer = new byte[4];
                int bytesRead = corProcess.ReadMemory(eventLoadDll.lpImageName.ToInt64(),buffer);
                Debug.Assert(bytesRead==buffer.Length);

                IntPtr newptr=new IntPtr((int)buffer[0]+((int)buffer[1]<<8)+((int)buffer[2]<<16)+((int)buffer[3]<<24));

                if(newptr == IntPtr.Zero)
                {
                    return null;
                }
                else
                {
                    System.Text.StringBuilder sb = new System.Text.StringBuilder();
                    if(bUnicode)
                        buffer = new byte[2];
                    else
                        buffer = new byte[1];
                    do
                    {
                        bytesRead = corProcess.ReadMemory(newptr.ToInt64(),buffer);
                        Debug.Assert(bytesRead==buffer.Length);
                        if(bytesRead<buffer.Length)
                            break;
                        int b;
                        if(bUnicode)
                            b=(int)buffer[0]+((int)buffer[1]<<8);
                        else
                            b=(int)buffer[0];
                            
                        if(b==0)
                            break;
                        sb.Append((char)b);
                        newptr=new IntPtr(newptr.ToInt32()+2);
                    }
                    while(true);
                    moduleName = sb.ToString();
                }
            }                                       

            return moduleName;
        }

        // returns a message from the event
        static string GetMessageFromDebugEvent(CorProcess corProcess, OUTPUT_DEBUG_STRING_INFO eventOds)
        {
            bool isUnicode = eventOds.fUnicode!=0;
                    
            byte[] buffer = new byte[isUnicode?eventOds.nDebugStringLenght*2:eventOds.nDebugStringLenght];
            int bytesRead = corProcess.ReadMemory(eventOds.lpDebugStringData.ToInt64(),buffer);
            Debug.Assert(buffer.Length==bytesRead);
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            for(int i=0;i<buffer.Length;i++)
            {
                int val;
                if(isUnicode)
                {
                    val =(int)buffer[i]+((int)buffer[i+1]<<8);
                    i++;
                }
                else
                    val = buffer[i];
                sb.Append((char)val);
            }
            return sb.ToString();
        }

        // returns string that contains a human-readable content of DEBUG_EVENT passed in
        internal static string DebugEventToString(CorProcess corProcess,DEBUG_EVENT debugEvent)
        {
            string callArgs;// = new StringBuilder();
            switch((NativeDebugEventCode)debugEvent.dwDebugEventCode)
            {
            case NativeDebugEventCode.EXCEPTION_DEBUG_EVENT:
                callArgs = String.Format(CultureInfo.InvariantCulture, @"
EXCEPTION_DEBUG_EVENT:
   FirstChance: {0}
   ExceptionCode: 0x{1:x}
   ExceptionFlags: 0x{2:x}
   Address: 0x{3:x}
", new Object[]{
                                             (debugEvent.Exception.dwFirstChance!=0?true:false),
                                             debugEvent.Exception.ExceptionRecord.ExceptionCode,
                                             debugEvent.Exception.ExceptionRecord.ExceptionFlags,
                                             (int)debugEvent.Exception.ExceptionRecord.ExceptionAddress});
                //<strip>@TODO For AV add written from /to</strip>
                break;
            case NativeDebugEventCode.CREATE_THREAD_DEBUG_EVENT:
                callArgs = String.Format(CultureInfo.InvariantCulture, @"
CREATE_THREAD_DEBUG_EVENT:
   ThreadLocalBase: 0x{0:x}
   StartAddress: 0x{1:x}
", new Object[]{
                                             (int)debugEvent.CreateThread.lpThreadLocalBase,
                                             (int)debugEvent.CreateThread.lpStartAddress});
                break;

            case NativeDebugEventCode.EXIT_THREAD_DEBUG_EVENT:
                callArgs = String.Format(CultureInfo.InvariantCulture, @"
EXIT_THREAD_DEBUG_EVENT:
   ExitCode: 0x{0:x}
", new Object[]{
                                             debugEvent.ExitThread.dwExitCode});
                break;

            case NativeDebugEventCode.EXIT_PROCESS_DEBUG_EVENT:
                callArgs = String.Format(CultureInfo.InvariantCulture, @"
EXIT_PROCESS_DEBUG_EVENT:
   ExitCode: 0x{0:x}
", new Object[]{
                                             debugEvent.ExitProcess.dwExitCode});
                break;

            case NativeDebugEventCode.LOAD_DLL_DEBUG_EVENT: {
                string moduleName = GetImageNameFromDebugEvent(corProcess, debugEvent.LoadDll);
                    
                if(moduleName == null)
                {
                     if (debugEvent.LoadDll.lpImageName == IntPtr.Zero)
                         moduleName = "N/A (lpImageName==null)";
                     else
                         moduleName = "N/A (in process)";
                }
				
                callArgs = String.Format(CultureInfo.InvariantCulture, @"
LOAD_DLL_DEBUG_EVENT:
   BaseAddress: 0x{0:x}
   ModuleName:  {1}
", new Object[]{
                                             (int)debugEvent.LoadDll.lpBaseOfDll, moduleName});
                break;

            }

            case NativeDebugEventCode.OUTPUT_DEBUG_STRING_EVENT: {
                string message = GetMessageFromDebugEvent(corProcess,debugEvent.OutputDebugString);
                callArgs = String.Format(CultureInfo.InvariantCulture, @"
OUTPUT_DEBUG_STRING_EVENT:
   text: {0}
", new Object[]{
                                             message});
                break;
            }

            default:
                callArgs = String.Format(CultureInfo.InvariantCulture, @"
{0}:
", new Object[]{
                                             ((NativeDebugEventCode)debugEvent.dwDebugEventCode)
                                         });
                break;
            }
            return callArgs;
        }

    }
#endif	//	MDBG_FEATURE_INTEROP
}
