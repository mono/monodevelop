//---------------------------------------------------------------------
//  This file is part of the CLR Managed Debugger (mdbg) Sample.
// 
//  Copyright (C) Microsoft Corporation.  All rights reserved.
//---------------------------------------------------------------------
using System;
using System.Collections;
using System.Diagnostics;

using Microsoft.Samples.Debugging.CorDebug.NativeApi;

namespace Microsoft.Samples.Debugging.CorDebug
{

    public struct CorActiveFunction
    {
        public int ILoffset
        { 
            get 
            {
                return m_ilOffset; 
            } 
        }
        private int m_ilOffset;
            
        public CorFunction Function
        { 
            get 
            {
                return m_function; 
            } 
        }
        private CorFunction m_function;
            
        public CorModule Module
        { 
            get 
            {
                return m_module; 
            } 
        }
        private CorModule m_module;

        internal CorActiveFunction(int ilOffset,CorFunction managedFunction,CorModule managedModule)
        {
            m_ilOffset = ilOffset;
            m_function = managedFunction;
            m_module = managedModule;
        }
    }

    /** A thread in the debugged process. */
    public sealed class CorThread : WrapperBase
    {
        internal CorThread (ICorDebugThread thread)
            :base(thread)
        {
            m_th = thread;
        }

        internal ICorDebugThread GetInterface ()
        {
            return m_th;
        }

#if CORAPI_EXPOSE_RAW_INTERFACES
        [CLSCompliant(false)]
        public ICorDebugThread Raw
        {
            get 
            { 
                return m_th;
            }
        }
#endif
        
        /** The process that this thread is in. */
        public CorProcess Process
        {
            get 
            {
                ICorDebugProcess p = null;
                m_th.GetProcess (out p);
                return CorProcess.GetCorProcess(p);
            }
        }

        /** the OS id of the thread. */
        public int Id
        {
            get 
            {
                uint id = 0;
                m_th.GetID (out id);
                return (int) id;
            }
        }

        /** The handle of the active part of the thread. */
        public IntPtr Handle
        {
            get 
            {
                IntPtr h = IntPtr.Zero;
                m_th.GetHandle (out h);
                return h;
            }
        }

        /** The AppDomain that owns the thread. */
        public CorAppDomain AppDomain
        {
            get 
            {
                ICorDebugAppDomain ad = null;
                m_th.GetAppDomain (out ad);
                return new CorAppDomain (ad);
            }
        }

        /** Set the current debug state of the thread. */
        [CLSCompliant(false)]
        public CorDebugThreadState DebugState
        {
            get 
            {
                CorDebugThreadState s = CorDebugThreadState.THREAD_RUN;
                m_th.GetDebugState (out s);
                return s;
            }
            set 
            {
                m_th.SetDebugState (value);
            }
        }

        /** the user state. */
        [CLSCompliant(false)]
        public CorDebugUserState UserState
        {
            get 
            {
                CorDebugUserState s = CorDebugUserState.USER_STOP_REQUESTED;
                m_th.GetUserState (out s);
                return s;
            }
        }

        /** the exception object which is currently being thrown by the thread. */
        public CorValue CurrentException
        {
            get 
            {
                ICorDebugValue v = null;
                m_th.GetCurrentException (out v);
                return (v==null) ? null : new CorValue (v);
            }
        }

        /** 
         * Clear the current exception object, preventing it from being thrown.
         */
        public void ClearCurrentException ()
        {
            m_th.ClearCurrentException ();
        }

        /** 
         * Intercept the current exception.
         */
        public void InterceptCurrentException(CorFrame frame)
        {
            ICorDebugThread2 m_th2 = (ICorDebugThread2)m_th;
            m_th2.InterceptCurrentException(frame.m_frame);
        }

        /** 
         * create a stepper object relative to the active frame in this thread.
         */
        public CorStepper CreateStepper ()
        {
            ICorDebugStepper s = null;
            m_th.CreateStepper (out s);
            return new CorStepper (s);
        }

        /** All stack chains in the thread. */
        public IEnumerable Chains
        {
            get 
            {
                ICorDebugChainEnum ec = null;
                m_th.EnumerateChains (out ec);
                return (ec==null)?null:new CorChainEnumerator (ec);
            }
        }
        
        /** The most recent chain in the thread, if any. */
        public CorChain ActiveChain
        {
            get 
            {
                ICorDebugChain ch = null;
                m_th.GetActiveChain (out ch);
                return ch == null ? null : new CorChain (ch);
            }
        }

        /** Get the active frame. */
        public CorFrame ActiveFrame
        {
            get 
            {
                ICorDebugFrame f = null;
                m_th.GetActiveFrame (out f);
                return f==null ? null : new CorFrame (f);
            }
        }

        /** Get the register set for the active part of the thread. */
        public CorRegisterSet RegisterSet
        {
            get 
            {
                ICorDebugRegisterSet r = null;
                m_th.GetRegisterSet (out r);
                return r==null?null:new CorRegisterSet (r);
            }
        }

        /** Creates an evaluation object. */
        public CorEval CreateEval ()
        {
            ICorDebugEval e = null;
            m_th.CreateEval (out e);
            return e==null?null:new CorEval (e);
        }

        /** Get the runtime thread object. */
        public CorValue ThreadVariable
        {
            get 
            {
                ICorDebugValue v = null;
                m_th.GetObject (out v);
                return new CorValue (v);
            }
        }

        public CorActiveFunction[] GetActiveFunctions()
        {
            ICorDebugThread2 m_th2 = (ICorDebugThread2) m_th;
            UInt32 pcFunctions;
            m_th2.GetActiveFunctions(0,out pcFunctions,null);
            COR_ACTIVE_FUNCTION[] afunctions = new COR_ACTIVE_FUNCTION[pcFunctions];
            m_th2.GetActiveFunctions(pcFunctions,out pcFunctions,afunctions);
            CorActiveFunction[] caf = new CorActiveFunction[pcFunctions];
            for(int i=0;i<pcFunctions;++i)
            {
                caf[i] = new CorActiveFunction((int) afunctions[i].ilOffset,
                                               new CorFunction((ICorDebugFunction)afunctions[i].pFunction),
                                               afunctions[i].pModule==null?null:new CorModule(afunctions[i].pModule)
                                               );
            }
            return caf;
        }
        

        private ICorDebugThread m_th;

    } /* class Thread */



    public enum CorFrameType
    {
        ILFrame, NativeFrame, InternalFrame
    }

    
    public sealed class CorFrame : WrapperBase
    {
        internal CorFrame(ICorDebugFrame frame)
            :base(frame)
        {
            m_frame = frame;
        }

#if CORAPI_EXPOSE_RAW_INTERFACES
        [CLSCompliant(false)]
        public ICorDebugFrame Raw
        {
            get 
            { 
                return m_frame;
            }
        }
#endif
        
        public CorStepper CreateStepper()
        {
            ICorDebugStepper istepper;
            m_frame.CreateStepper(out istepper);
            return ( istepper==null ? null : new CorStepper(istepper) );
        }

        public CorFrame Callee
        {
            get 
            {
                ICorDebugFrame iframe;
                m_frame.GetCallee(out iframe);
                return ( iframe==null ? null : new CorFrame(iframe) );
            }
        }

        public CorFrame Caller
        {
            get 
            {
                ICorDebugFrame iframe;
                m_frame.GetCaller(out iframe);
                return ( iframe==null ? null : new CorFrame(iframe) );
            }
        }

        public CorChain Chain
        {
            get 
            {
                ICorDebugChain ichain;
                m_frame.GetChain(out ichain);
                return ( ichain==null ? null : new CorChain(ichain) );
            }
        }

        public CorCode Code
        {
            get
            {
                ICorDebugCode icode;
                m_frame.GetCode(out icode);
                return ( icode==null ? null : new CorCode(icode) );
            }
        }

        public CorFunction Function
        {
            get 
            {
                ICorDebugFunction ifunction;
                m_frame.GetFunction(out ifunction);
                return ( ifunction==null ? null : new CorFunction(ifunction) );
            }
        }

        public int FunctionToken
        {
            get 
            {
                uint token;
                m_frame.GetFunctionToken(out token);
                return (int)token;
            }
        }

        public CorFrameType FrameType
        {
            get 
            {
                ICorDebugILFrame ilframe = GetILFrame();
                if (ilframe != null)
                    return CorFrameType.ILFrame;
                
                ICorDebugInternalFrame iframe = GetInternalFrame();
                if (iframe != null)
                    return CorFrameType.InternalFrame;

                return CorFrameType.NativeFrame;
            }
        }
        
        [CLSCompliant(false)]
        public CorDebugInternalFrameType InternalFrameType
        {
            get
            {
                ICorDebugInternalFrame iframe = GetInternalFrame();
                CorDebugInternalFrameType ft;
                
                if(iframe==null)
                    throw new Exception("Cannot get frame type on non-internal frame");
                
                iframe.GetFrameType(out ft);
                return ft;
            }
        }
        
        [CLSCompliant(false)]
        public void GetStackRange(out UInt64 startOffset,out UInt64 endOffset)
        {
            m_frame.GetStackRange(out startOffset,out endOffset);
        }

        [CLSCompliant(false)]
        public void GetIP(out uint offset, out CorDebugMappingResult mappingResult)
        {
            ICorDebugILFrame ilframe = GetILFrame();
            if(ilframe==null) 
            {
                offset = 0;
                mappingResult = CorDebugMappingResult.MAPPING_NO_INFO;
            }
            else
                ilframe.GetIP(out offset, out mappingResult);
        }

        public void SetIP(int offset)
        {
            ICorDebugILFrame ilframe = GetILFrame();
            if(ilframe==null)
                throw new Exception("Cannot set an IP on non-il frame");
            ilframe.SetIP((uint)offset);
        }

        public bool CanSetIP(int offset)
        {
            ICorDebugILFrame ilframe = GetILFrame();
            if( ilframe==null )
                return false;
            return (ilframe.CanSetIP((uint)offset)==(int)HResult.S_OK);
        }

        public bool CanSetIP(int offset, out int hresult)
        {
            ICorDebugILFrame ilframe = GetILFrame();
            if( ilframe==null )
            {
                hresult = (int)HResult.E_FAIL;
                return false;
            }
            hresult = ilframe.CanSetIP((uint)offset);
            return (hresult==(int)HResult.S_OK);
        }

        [CLSCompliant(false)]
        public void GetNativeIP(out uint offset)
        {
            ICorDebugNativeFrame nativeFrame = m_frame as ICorDebugNativeFrame;
            Debug.Assert( nativeFrame!=null );
            nativeFrame.GetIP(out offset);
        }

        public CorValue GetLocalVariable(int index)
        {
            ICorDebugILFrame ilframe = GetILFrame();
            if(ilframe==null)
                return null;
            
            ICorDebugValue value;
            try
            {
                ilframe.GetLocalVariable((uint)index,out value);
            }
            catch(System.Runtime.InteropServices.COMException e)
            {
                // If you are stopped in the Prolog, the variable may not be available.
                // CORDBG_E_IL_VAR_NOT_AVAILABLE is returned after dubugee triggers StackOverflowException
                if(e.ErrorCode == (int)HResult.CORDBG_E_IL_VAR_NOT_AVAILABLE)
                {
                    return null;
                }
                else
                {
                    throw;
                }
            }
            return (value==null)?null:new CorValue(value);
        }

        public int GetLocalVariablesCount()
        {
            ICorDebugILFrame ilframe = GetILFrame();
            if(ilframe==null)
                return -1;

            ICorDebugValueEnum ve;
            ilframe.EnumerateLocalVariables(out ve);
            uint count;
            ve.GetCount(out count);
            return (int)count;
        }

        public CorValue GetArgument(int index)
        {
            ICorDebugILFrame ilframe = GetILFrame();
            if(ilframe==null)
                return null;


            ICorDebugValue value;
            ilframe.GetArgument((uint)index,out value);
            return (value==null)?null:new CorValue(value);
        }

        public int GetArgumentCount()
        {
            ICorDebugILFrame ilframe = GetILFrame();
            if(ilframe==null)
                return -1;

            ICorDebugValueEnum ve;
            ilframe.EnumerateArguments(out ve);
            uint count;
            ve.GetCount(out count);
            return (int)count;
        }

        public void RemapFunction(int newILOffset)
        {
            ICorDebugILFrame ilframe = GetILFrame();
            if(ilframe==null)
                throw new Exception("Cannot remap on non-il frame.");
            ICorDebugILFrame2 ilframe2 = (ICorDebugILFrame2) ilframe;
            ilframe2.RemapFunction((uint)newILOffset);
        }

        [CLSCompliant(false)]
        public ICorDebugILFrame GetILFrame()
        {
            if(!m_ilFrameCached) 
            {
                m_ilFrameCached = true;
                try 
                {
                    m_ilFrame = (ICorDebugILFrame) m_frame;
                }
                catch(InvalidCastException ) 
                {
                    m_ilFrame = null; // running on free version without ini file ???
                }
            }
            return m_ilFrame;
        }

        private ICorDebugInternalFrame GetInternalFrame()
        {
            if(!m_iFrameCached) 
            {
                m_iFrameCached = true;
                try 
                {
                    m_iFrame = (ICorDebugInternalFrame)m_frame;
                }
                catch(InvalidCastException) 
                {
                    m_iFrame = null;
                }
            }
            return m_iFrame;
        }

        // 'TypeParameters' returns an enumerator that goes yields generic args from
        // both the class and the method. To enumerate just the generic args on the 
        // method, we need to skip past the class args. We have to get that skip value
        // from the metadata. This is a helper function to efficiently get an enumerator that skips
        // to a given spot (likely past the class generic args). 
        public IEnumerable GetTypeParamEnumWithSkip(int skip)
        {
            if (skip < 0)
            {
                throw new ArgumentException("Skip parameter must be positive");
            }
            IEnumerable e = this.TypeParameters;
            Debug.Assert(e is CorTypeEnumerator);
            // Skip will throw if we try to skip the whole collection <strip>(a bug?)</strip>
            int total  = (e as CorTypeEnumerator).Count;
            if (skip >= total)
            {
                return new CorTypeEnumerator(null); // empty.
            }
            
            (e as CorTypeEnumerator).Skip(skip);
            return e;
        }
    
        public IEnumerable TypeParameters
        {
            get 
            {
                ICorDebugTypeEnum icdte = null;
                ICorDebugILFrame ilf = GetILFrame();
                
                (ilf as ICorDebugILFrame2).EnumerateTypeParameters(out icdte);
                return new CorTypeEnumerator(icdte);        // icdte can be null, is handled by enumerator
            }
        }
        
        private ICorDebugILFrame m_ilFrame = null;
        private bool m_ilFrameCached = false;

        private ICorDebugInternalFrame m_iFrame = null;
        private bool m_iFrameCached = false;

        internal ICorDebugFrame m_frame;
    }

    public sealed class CorChain : WrapperBase
    {
        internal CorChain(ICorDebugChain chain)
            :base(chain)
        {
            m_chain = chain;
        }

#if CORAPI_EXPOSE_RAW_INTERFACES
        [CLSCompliant(false)]
        public ICorDebugChain Raw
        {
            get 
            { 
                return m_chain;
            }
        }
#endif

        public CorFrame ActiveFrame
        {
            get 
            {
                ICorDebugFrame iframe;
                m_chain.GetActiveFrame(out iframe);
                return ( iframe==null ? null : new CorFrame(iframe) );
            }
        }

        public CorChain Callee
        {
            get 
            {
                ICorDebugChain ichain;
                m_chain.GetCallee(out ichain);
                return ( ichain==null ? null : new CorChain(ichain) );
            }
        }
      
        public CorChain Caller
        {
            get 
            {
                ICorDebugChain ichain;
                m_chain.GetCaller(out ichain);
                return ( ichain==null ? null : new CorChain(ichain) );
            }
        }
      
        public CorContext Context
        {
            get 
            {
                ICorDebugContext icontext;
                m_chain.GetContext(out icontext);
                return ( icontext==null ? null : new CorContext(icontext) );
            }
        }
      
        public CorChain Next
        {
            get 
            {
                ICorDebugChain ichain;
                m_chain.GetNext(out ichain);
                return ( ichain==null ? null : new CorChain(ichain) );
            }
        }

        public CorChain Previous
        {
            get 
            {
                ICorDebugChain ichain;
                m_chain.GetPrevious(out ichain);
                return ( ichain==null ? null : new CorChain(ichain) );
            }
        }

        [CLSCompliant(false)]
        public CorDebugChainReason Reason
        {
            get 
            {
                CorDebugChainReason reason;
                m_chain.GetReason(out reason);
                return reason;
            }
        }

        public CorRegisterSet RegisterSet
        {
            get 
            {
                ICorDebugRegisterSet r = null;
                m_chain.GetRegisterSet (out r);
                return r==null?null:new CorRegisterSet (r);
            }
        }
      
        public void GetStackRange(out Int64 pStart, out Int64 pEnd)
        {
            UInt64 start = 0;
            UInt64 end = 0;
            m_chain.GetStackRange(out start, out end);
            pStart = (Int64) start;
            pEnd = (Int64) end;
        }
      
        public CorThread Thread
        {
            get 
            {
                ICorDebugThread ithread;
                m_chain.GetThread(out ithread);
                return ( ithread==null ? null : new CorThread(ithread) );
            }
        }

        public bool IsManaged
        {
            get 
            {
                int managed;
                m_chain.IsManaged(out managed);
                return ( managed!=0 ? true : false );
            }
        }

        public IEnumerable Frames
        {
            get 
            {
                ICorDebugFrameEnum ef = null;
                m_chain.EnumerateFrames (out ef);
                return (ef==null)?null:new CorFrameEnumerator (ef);
            }
        }

        private ICorDebugChain m_chain;
    }

    internal class CorFrameEnumerator : IEnumerable, IEnumerator, ICloneable
    {
        internal CorFrameEnumerator (ICorDebugFrameEnum frameEnumerator)
        {
            m_enum = frameEnumerator;
        }

        //
        // ICloneable interface
        //
        public Object Clone ()
        {
            ICorDebugEnum clone = null;
            m_enum.Clone (out clone);
            return new CorFrameEnumerator ((ICorDebugFrameEnum)clone);
        }

        //
        // IEnumerable interface
        //
        public IEnumerator GetEnumerator ()
        {
            return this;
        }

        //
        // IEnumerator interface
        //
        public bool MoveNext ()
        {
            ICorDebugFrame[] a = new ICorDebugFrame[1];
            uint c = 0;
            int r = m_enum.Next ((uint)a.Length, a, out c);
            if (r==0 && c==1) // S_OK && we got 1 new element
                m_frame = new CorFrame (a[0]);
            else
                m_frame = null;
            return m_frame != null;
        }

        public void Reset ()
        {
            m_enum.Reset ();
            m_frame = null;
        }

        public Object Current
        {
            get 
            {
                return m_frame;
            }
        }

        private ICorDebugFrameEnum m_enum;
        private CorFrame m_frame;
    }


    public struct IL2NativeMap
    {
        public int IlOffset
        { 
            get 
            { 
                return m_ilOffset; 
            } 
        }
        private int m_ilOffset;

        public int  NativeStartOffset
        { 
            get 
            { 
                return m_nativeStartOffset; 
            } 
        }
        private int m_nativeStartOffset;

        public int  NativeEndOffset
        { 
            get 
            { 
                return m_nativeEndOffset; 
            } 
        }
        private int m_nativeEndOffset;

        internal IL2NativeMap(int ilOffset, int nativeStartOffset, int nativeEndOffset)
        {
            m_ilOffset = ilOffset;
            m_nativeStartOffset = nativeStartOffset;
            m_nativeEndOffset = nativeEndOffset;
        }
    }


    public sealed class CorCode : WrapperBase
    {
        internal CorCode(ICorDebugCode code)
            :base(code)
        {
            m_code = code;
        }

#if CORAPI_EXPOSE_RAW_INTERFACES
        [CLSCompliant(false)]
        public ICorDebugCode Raw
        {
            get 
            { 
                return m_code;
            }
        }
#endif

        public CorFunctionBreakpoint CreateBreakpoint(int offset)
        {
            ICorDebugFunctionBreakpoint ibreakpoint;
            m_code.CreateBreakpoint((uint)offset,out ibreakpoint);
            return ( ibreakpoint==null ? null : new CorFunctionBreakpoint(ibreakpoint) );
        }
      
        [CLSCompliant(false)]
        public ulong Address
        {
            get 
            {
                UInt64 start;
                m_code.GetAddress(out start);
                return start;
            }
        }

        public CorDebugJITCompilerFlags CompilerFlags
        {
            get 
            {
                uint dwFlags;
                (m_code as ICorDebugCode2).GetCompilerFlags(out dwFlags);
                return (CorDebugJITCompilerFlags)dwFlags;
            }
        }
      
        public byte[] GetCode()
        {
            uint codeSize = (uint)this.Size;
            
            byte[] code = new byte[codeSize];
            uint returnedCode;
            m_code.GetCode(0,codeSize,codeSize,code,out returnedCode);
            Debug.Assert(returnedCode==codeSize);
            return code;
        }

        [CLSCompliant(false)]
        public _CodeChunkInfo[] GetCodeChunks()
        {
            UInt32 pcnumChunks;
            (m_code as ICorDebugCode2).GetCodeChunks(0, out pcnumChunks, null);
            if (pcnumChunks == 0)
                return new _CodeChunkInfo[0];

            _CodeChunkInfo[] chunks = new _CodeChunkInfo[pcnumChunks];
            (m_code as ICorDebugCode2).GetCodeChunks((uint)chunks.Length, out pcnumChunks, chunks);
            return chunks;
        }
        
        public CorFunction GetFunction()
        {
            ICorDebugFunction ifunction;
            m_code.GetFunction(out ifunction);
            return ( ifunction==null ? null : new CorFunction(ifunction) );
        }

        public IL2NativeMap[] GetILToNativeMapping()
        {
            UInt32 pcMap;
            m_code.GetILToNativeMapping(0,out pcMap,null);
            if(pcMap==0) 
                return new IL2NativeMap[0];
            
            COR_DEBUG_IL_TO_NATIVE_MAP[] map = new COR_DEBUG_IL_TO_NATIVE_MAP[pcMap];
            m_code.GetILToNativeMapping((uint)map.Length,out pcMap,map);

            IL2NativeMap[] ret = new IL2NativeMap[map.Length];
            for(int i=0;i<map.Length;i++)
            {
                ret[i] = new IL2NativeMap((int) map[i].ilOffset,
                                          (int) map[i].nativeStartOffset,
                                          (int) map[i].nativeEndOffset
                                          );
            }
            return ret;
        }
      
        [CLSCompliant(false)]
        public int Size
        {
            get 
            {
                UInt32 pcBytes;
                m_code.GetSize(out pcBytes);
                return (int)pcBytes;
            }
        }
      
        public int VersionNumber
        {
            get 
            {
                UInt32 nVersion;
                m_code.GetVersionNumber(out nVersion);
                return (int)nVersion;
            }
        }
      
        public bool IsIL
        {
            get 
            {
                Int32 pbIL;
                m_code.IsIL(out pbIL);
                return ( pbIL != 0 ? true : false );
            }
        }

        private ICorDebugCode m_code;
    }

    /** Exposes an enumerator for CodeEnum. */
    internal class CorCodeEnumerator : IEnumerable, IEnumerator, ICloneable
    {
        private ICorDebugCodeEnum m_enum;
        private CorCode m_c;

        internal CorCodeEnumerator (ICorDebugCodeEnum codeEnumerator)
        {
            m_enum = codeEnumerator;
        }

        //
        // ICloneable interface
        //
        public Object Clone ()
        {
            ICorDebugEnum clone = null;
            m_enum.Clone (out clone);
            return new CorCodeEnumerator ((ICorDebugCodeEnum)clone);
        }

        //
        // IEnumerable interface
        //
        public IEnumerator GetEnumerator ()
        {
            return this;
        }

        //
        // IEnumerator interface
        //
        public bool MoveNext ()
        {
            ICorDebugCode[] a = new ICorDebugCode[1];
            uint c = 0;
            int r = m_enum.Next ((uint) a.Length, a, out c);
            if (r==0 && c==1) // S_OK && we got 1 new element
                m_c = new CorCode (a[0]);
            else
                m_c = null;
            return m_c != null;
        }

        public void Skip (uint celt)
        {
            m_enum.Skip (celt);
            m_c = null;
        }

        public void Reset ()
        {
            m_enum.Reset ();
            m_c = null;
        }

        public Object Current
        {
            get 
            {
                return m_c;
            }
        }
    } /* class CodeEnumerator */

    public sealed class CorFunction : WrapperBase
    {
        internal CorFunction(ICorDebugFunction managedFunction)
            :base(managedFunction)
        {
            m_function = managedFunction;
        }

#if CORAPI_EXPOSE_RAW_INTERFACES
        [CLSCompliant(false)]
        public ICorDebugFunction Raw
        {
            get 
            { 
                return m_function;
            }
        }
#endif

        public CorFunctionBreakpoint CreateBreakpoint()
        {
            ICorDebugFunctionBreakpoint ifuncbreakpoint;
            m_function.CreateBreakpoint(out ifuncbreakpoint);
            return ( ifuncbreakpoint==null ? null : new CorFunctionBreakpoint(ifuncbreakpoint) );
        }
      
        public CorClass Class
        {
            get
            {
                ICorDebugClass iclass;
                m_function.GetClass(out iclass);
                return ( iclass==null ? null : new CorClass(iclass) );
            }
        }
      
        // <strip>@TODO void GetCurrentVersionNumber(ref UInt32 pnCurrentVersion);</strip>
      
        public CorCode ILCode
        {
            get 
            {
                ICorDebugCode icode;
                m_function.GetILCode(out icode);
                return ( icode==null ? null : new CorCode(icode) );
            }
        }
      
        public CorCode NativeCode
        {
            get 
            {
                ICorDebugCode icode;
                m_function.GetNativeCode(out icode);
                return ( icode==null ? null : new CorCode(icode) );
            }
        }
        
        // <strip>@TODO void GetLocalVarSigToken(ref UInt32 pmdSig);</strip>

        public CorModule Module
        {
            get 
            {
                ICorDebugModule imodule;
                m_function.GetModule(out imodule);
                return ( imodule==null ? null : new CorModule(imodule) );
            }
        }
      
        public int Token
        {
            get 
            {
                UInt32 pMethodDef;
                m_function.GetToken(out pMethodDef);
                return (int)pMethodDef;
            }
        }

        public int Version
        {
            get 
            {
                UInt32 pVersion;
                (m_function as ICorDebugFunction2).GetVersionNumber(out pVersion);
                return (int)pVersion;
            }
        }

        public bool JMCStatus
        {
            get
            {
                int status;
                (m_function as ICorDebugFunction2).GetJMCStatus(out status);
                return status!=0;
            }
            set
            {
                (m_function as ICorDebugFunction2).SetJMCStatus(value?1:0);
            }
        }
        internal ICorDebugFunction m_function;
    }

    public sealed class CorContext : WrapperBase
    {
        internal CorContext(ICorDebugContext context)
            :base(context)
        {
            m_context = context;
        }

#if CORAPI_EXPOSE_RAW_INTERFACES
        [CLSCompliant(false)]
        public ICorDebugContext Raw
        {
            get 
            { 
                return m_context;
            }
        }
#endif

        //<strip>@TODO IMPLEMENT</strip>
        // Following functions are not implemented
        /*
          void CreateBreakpoint(ref CORDBLib.ICorDebugValueBreakpoint ppBreakpoint);
          void GetAddress(ref UInt64 pAddress);
          void GetClass(ref CORDBLib.ICorDebugClass ppClass);
          void GetContext(ref CORDBLib.ICorDebugContext ppContext);
          void GetFieldValue(CORDBLib.ICorDebugClass pClass, UInt32 fieldDef, ref CORDBLib.ICorDebugValue ppValue);
          void GetManagedCopy(ref Object ppObject);
          void GetSize(ref UInt32 pSize);
          void GetType(ref UInt32 pType);
          void GetVirtualMethod(UInt32 memberRef, ref CORDBLib.ICorDebugFunction ppFunction);
          void IsValueClass(ref Int32 pbIsValueClass);
          void SetFromManagedCopy(object pObject);
        */
        private ICorDebugContext m_context;
    }

} /* namespace */
