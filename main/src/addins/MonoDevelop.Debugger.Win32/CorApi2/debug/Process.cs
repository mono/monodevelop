//---------------------------------------------------------------------
//  This file is part of the CLR Managed Debugger (mdbg) Sample.
// 
//  Copyright (C) Microsoft Corporation.  All rights reserved.
//---------------------------------------------------------------------
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

using Microsoft.Samples.Debugging.CorDebug.NativeApi;

namespace Microsoft.Samples.Debugging.CorDebug
{
    /** A process running some managed code. */
    public sealed class CorProcess : CorController, IDisposable
    {
        [CLSCompliant(false)]
        public static CorProcess GetCorProcess(ICorDebugProcess process)
        {
            Debug.Assert(process!=null);
            lock(m_instances) 
            {
                if(!m_instances.Contains(process))
                {
                    CorProcess p = new CorProcess(process);
                    m_instances.Add(process,p);
                    return p;
                }
                return (CorProcess)m_instances[process];
            }
        }

        public void Dispose()
        {
            // Release event handlers. The event handlers are strong references and may keep
            // other high-level objects (such as things in the MdbgEngine layer) alive.
            m_callbacksArray = null;            
            
            // Remove ourselves from instances hash.
            lock(m_instances) 
            {
                m_instances.Remove(_p());
            }
        }

        private CorProcess (ICorDebugProcess process)
            : base (process)
        {
            InitCallbacks ();
        }

        private void InitCallbacks ()
        {
            m_callbacksArray = new Dictionary<ManagedCallbackType, DebugEventHandler<CorEventArgs>> {
                {ManagedCallbackType.OnBreakpoint, (sender, args) => OnBreakpoint (sender, (CorBreakpointEventArgs) args)},
                {ManagedCallbackType.OnStepComplete, (sender, args) => OnStepComplete (sender, (CorStepCompleteEventArgs) args)},
                {ManagedCallbackType.OnBreak, (sender, args) => OnBreak (sender, (CorThreadEventArgs) args)},
                {ManagedCallbackType.OnException, (sender, args) => OnException (sender, (CorExceptionEventArgs) args)},
                {ManagedCallbackType.OnEvalComplete, (sender, args) => OnEvalComplete (sender, (CorEvalEventArgs) args)},
                {ManagedCallbackType.OnEvalException, (sender, args) => OnEvalException (sender, (CorEvalEventArgs) args)},
                {ManagedCallbackType.OnCreateProcess, (sender, args) => OnCreateProcess (sender, (CorProcessEventArgs) args)},
                {ManagedCallbackType.OnProcessExit, (sender, args) => OnProcessExit (sender, (CorProcessEventArgs) args)},
                {ManagedCallbackType.OnCreateThread, (sender, args) => OnCreateThread (sender, (CorThreadEventArgs) args)},
                {ManagedCallbackType.OnThreadExit, (sender, args) => OnThreadExit (sender, (CorThreadEventArgs) args)},
                {ManagedCallbackType.OnModuleLoad, (sender, args) => OnModuleLoad (sender, (CorModuleEventArgs) args)},
                {ManagedCallbackType.OnModuleUnload, (sender, args) => OnModuleUnload (sender, (CorModuleEventArgs) args)},
                {ManagedCallbackType.OnClassLoad, (sender, args) => OnClassLoad (sender, (CorClassEventArgs) args)},
                {ManagedCallbackType.OnClassUnload, (sender, args) => OnClassUnload (sender, (CorClassEventArgs) args)},
                {ManagedCallbackType.OnDebuggerError, (sender, args) => OnDebuggerError (sender, (CorDebuggerErrorEventArgs) args)},
                {ManagedCallbackType.OnLogMessage, (sender, args) => OnLogMessage (sender, (CorLogMessageEventArgs) args)},
                {ManagedCallbackType.OnLogSwitch, (sender, args) => OnLogSwitch (sender, (CorLogSwitchEventArgs) args)},
                {ManagedCallbackType.OnCreateAppDomain, (sender, args) => OnCreateAppDomain (sender, (CorAppDomainEventArgs) args)},
                {ManagedCallbackType.OnAppDomainExit, (sender, args) => OnAppDomainExit (sender, (CorAppDomainEventArgs) args)},
                {ManagedCallbackType.OnAssemblyLoad, (sender, args) => OnAssemblyLoad (sender, (CorAssemblyEventArgs) args)},
                {ManagedCallbackType.OnAssemblyUnload, (sender, args) => OnAssemblyUnload (sender, (CorAssemblyEventArgs) args)},
                {ManagedCallbackType.OnControlCTrap, (sender, args) => OnControlCTrap (sender, (CorProcessEventArgs) args)},
                {ManagedCallbackType.OnNameChange, (sender, args) => OnNameChange (sender, (CorThreadEventArgs) args)},
                {ManagedCallbackType.OnUpdateModuleSymbols, (sender, args) => OnUpdateModuleSymbols (sender, (CorUpdateModuleSymbolsEventArgs) args)},
                {ManagedCallbackType.OnFunctionRemapOpportunity, (sender, args) => OnFunctionRemapOpportunity (sender, (CorFunctionRemapOpportunityEventArgs) args)},
                {ManagedCallbackType.OnFunctionRemapComplete, (sender, args) => OnFunctionRemapComplete (sender, (CorFunctionRemapCompleteEventArgs) args)},
                {ManagedCallbackType.OnBreakpointSetError, (sender, args) => OnBreakpointSetError (sender, (CorBreakpointEventArgs) args)},
                {ManagedCallbackType.OnException2, (sender, args) => OnException2 (sender, (CorException2EventArgs) args)},
                {ManagedCallbackType.OnExceptionUnwind2, (sender, args) => OnExceptionUnwind2 (sender, (CorExceptionUnwind2EventArgs) args)},
                {ManagedCallbackType.OnMDANotification, (sender, args) => OnMDANotification (sender, (CorMDAEventArgs) args)},
                {ManagedCallbackType.OnExceptionInCallback, (sender, args) => OnExceptionInCallback (sender, (CorExceptionInCallbackEventArgs) args)},
            };
        }

        private static Hashtable m_instances = new Hashtable();

        private ICorDebugProcess _p ()
        {
            return (ICorDebugProcess) GetController();
        }



        /** The OS ID of the process. */
        public int Id
        {
            get 
            {
                uint id = 0;
                _p().GetID (out id);
                return (int) id;
            }
        }

        /** Returns a handle to the process. */
        public IntPtr Handle
        {
            get 
            {
                IntPtr h = IntPtr.Zero;
                _p().GetHandle (out h);
                return h;
            }
        }

        public Version Version
        {
            get 
            {
                _COR_VERSION cv;
                (_p() as ICorDebugProcess2).GetVersion(out cv);
                return new Version((int)cv.dwMajor,(int)cv.dwMinor,(int)cv.dwBuild,(int)cv.dwSubBuild);
            }
        }

        /** All managed objects in the process. */
        public IEnumerable Objects
        {
            get 
            {
                ICorDebugObjectEnum eobj = null;
                _p().EnumerateObjects (out eobj);
                return new CorObjectEnumerator (eobj);
            }
        }

        /** Is the address inside a transition stub? */
        public bool IsTransitionStub (long address)
        {
            int y = 0;
            _p().IsTransitionStub ((ulong)address, out y);
            return !(y==0);
        }

        /** Has the thread been suspended? */
        public bool IsOSSuspended (int tid)
        {
            int y = 0;
            _p().IsOSSuspended ((uint) tid, out y);
            return !(y==0);
        }

        /** Gets managed thread for threadId. 
         * Returns NULL if tid is not a managed thread. That's very common in interop-debugging cases.
         */
        public CorThread GetThread(int threadId)
        {
            ICorDebugThread thread = null;
            try
            {
                _p().GetThread((uint)threadId, out thread);
            }
            catch (ArgumentException)
            {
            }
            return (thread == null) ? null : (new CorThread(thread));
        }

        /* Get the context for the given thread. */
        // See WIN32_CONTEXT structure declared in context.il
        public void GetThreadContext ( int threadId, IntPtr contextPtr, int context_size )
        {

            _p().GetThreadContext( (uint)threadId, (uint) context_size, contextPtr );
            return;
        }

        /* Set the context for a given thread. */
        public void SetThreadContext (int threadId, IntPtr contextPtr, int context_size)
        {
            _p().SetThreadContext( (uint)threadId, (uint) context_size, contextPtr );
        }

        /** Read memory from the process. */
        public long ReadMemory (long address, byte[] buffer)
        {
            Debug.Assert(buffer!=null);
            IntPtr read = IntPtr.Zero;
            _p().ReadMemory ((ulong) address, (uint) buffer.Length, buffer, out read);
            return read.ToInt64();
        }

        /** Write memory in the process. */
        public long WriteMemory (long address, byte[] buffer)
        {
            IntPtr written = IntPtr.Zero;
            _p().WriteMemory ((ulong) address, (uint) buffer.Length, buffer, out written);
            return written.ToInt64();
        }

        /** Clear the current unmanaged exception on the given thread. */
        public void ClearCurrentException (int threadId)
        {
            _p().ClearCurrentException ((uint) threadId);
        }

        /** enable/disable sending of log messages to the debugger for logging. */
        public void EnableLogMessages (bool value)
        {
            _p().EnableLogMessages (value ? 1 : 0);
        }

        /** Modify the specified switches severity level */
        public void ModifyLogSwitch (String name, int level)
        {
            _p().ModifyLogSwitch (name,level);
        }

        /** All appdomains in the process. */
        public IEnumerable AppDomains
        {
            get
            {
                ICorDebugAppDomainEnum ead = null;
                _p().EnumerateAppDomains (out ead);
                return new CorAppDomainEnumerator (ead);
            }
        }

        /** Get the runtime proces object. */
        public CorValue ProcessVariable
        {
            get
            {
                ICorDebugValue v = null;
                _p().GetObject (out v);
                return new CorValue (v);
            }
        }

        /** These flags set things like TrackJitInfo, PreventOptimization, IgnorePDBs, and EnableEnC */
        /**  Any combination of bits in this DWORD flag enum is ok, but if its not a valid set, you may get an error */
        public CorDebugJITCompilerFlags DesiredNGENCompilerFlags
        {
            get
            {
                uint retval = 0;
                ((ICorDebugProcess2)_p()).GetDesiredNGENCompilerFlags(out retval);
                return (CorDebugJITCompilerFlags)retval;
            }
            set
            {
                ((ICorDebugProcess2)_p()).SetDesiredNGENCompilerFlags((uint)value);
            }
        }

        public CorReferenceValue GetReferenceValueFromGCHandle(IntPtr gchandle)
        {
		ICorDebugProcess2 p2 = (ICorDebugProcess2)_p();
		ICorDebugReferenceValue retval;
		p2.GetReferenceValueFromGCHandle(gchandle, out retval);
		return new CorReferenceValue(retval);
        }

        /** get the thread for a cookie. */
        public CorThread ThreadForFiberCookie (int cookie)
        {
            ICorDebugThread thread = null;
            _p().ThreadForFiberCookie ((uint) cookie, out thread);
            return (thread==null)?null:(new CorThread (thread));
        }

        /** set a BP in native code */
        public byte[] SetUnmanagedBreakpoint( long address )
        {
            UInt32 outLen;
            byte[] ret = new Byte[1];
            ICorDebugProcess2 p2 = (ICorDebugProcess2)_p();
            p2.SetUnmanagedBreakpoint( (UInt64)address, 1, ret, out outLen );
            Debug.Assert( outLen == 1 );
            return ret;
        }

        /** clear a previously set BP in native code */
        public void ClearUnmanagedBreakpoint( long address )
        {
            ICorDebugProcess2 p2 = (ICorDebugProcess2)_p();
            p2.ClearUnmanagedBreakpoint( (UInt64)address );
        }

        public override void Stop (int timeout)
        {
            _p().Stop ((uint)timeout);
        }

        public override void Continue (bool outOfBand)
        {
            if( !outOfBand &&                               // OOB event can arrive anytime (we just ignore them).
                (m_callbackAttachedEvent!=null) )
            {
                // first special call to "Continue" -- this fake continue will start delivering
                // callbacks.
                Debug.Assert( !outOfBand );
                ManualResetEvent ev = m_callbackAttachedEvent;
                // we set the m_callbackAttachedEvent to null first to prevent races.
                m_callbackAttachedEvent = null;
                ev.Set();
            }
            else
                base.Continue(outOfBand);
        }
        
        // when process is first created wait till callbacks are enabled.
        private ManualResetEvent m_callbackAttachedEvent = new ManualResetEvent(false);

        private Dictionary<ManagedCallbackType, DebugEventHandler<CorEventArgs>> m_callbacksArray;
        
        
        internal void DispatchEvent(ManagedCallbackType callback,CorEventArgs e)
        {
            try
            {
                if( m_callbackAttachedEvent!=null )
                    m_callbackAttachedEvent.WaitOne(); // waits till callbacks are enabled
                var d = m_callbacksArray[callback];
                d(this,e);
            }
            catch(Exception ex)
            {
                CorExceptionInCallbackEventArgs e2 = new CorExceptionInCallbackEventArgs(e.Controller,ex);
                Debug.Assert(false,"Exception in callback: "+ex.ToString());
                try 
                {
                    // we need to dispatch the exceptin in callback error, but we cannot
                    // use DispatchEvent since throwing exception in ExceptionInCallback
                    // would lead to infinite recursion.
                    Debug.Assert( m_callbackAttachedEvent==null);
                    var d = m_callbacksArray[ManagedCallbackType.OnExceptionInCallback];
                    d(this, e2);
                } 
                catch(Exception ex2)
                {
                    Debug.Assert(false,"Exception in Exception notification callback: "+ex2.ToString());
                    // ignore it -- there is nothing we can do.
                }
                e.Continue = e2.Continue;
            }
        }

        public event DebugEventHandler<CorBreakpointEventArgs> OnBreakpoint = delegate { };
        public event DebugEventHandler<CorBreakpointEventArgs> OnBreakpointSetError = delegate { };
        public event DebugEventHandler<CorStepCompleteEventArgs> OnStepComplete = delegate { };
        public event DebugEventHandler<CorThreadEventArgs> OnBreak = delegate { };
        public event DebugEventHandler<CorExceptionEventArgs> OnException = delegate { };
        public event DebugEventHandler<CorEvalEventArgs> OnEvalComplete = delegate { };
        public event DebugEventHandler<CorEvalEventArgs> OnEvalException = delegate { };
        public event DebugEventHandler<CorProcessEventArgs> OnCreateProcess = delegate { };
        public event DebugEventHandler<CorProcessEventArgs> OnProcessExit = delegate { };
        public event DebugEventHandler<CorThreadEventArgs> OnCreateThread = delegate { };
        public event DebugEventHandler<CorThreadEventArgs> OnThreadExit = delegate { };
        public event DebugEventHandler<CorModuleEventArgs> OnModuleLoad = delegate { };
        public event DebugEventHandler<CorModuleEventArgs> OnModuleUnload = delegate { };
        public event DebugEventHandler<CorClassEventArgs> OnClassLoad = delegate { };
        public event DebugEventHandler<CorClassEventArgs> OnClassUnload = delegate { };
        public event DebugEventHandler<CorDebuggerErrorEventArgs> OnDebuggerError = delegate { };
        public event DebugEventHandler<CorMDAEventArgs> OnMDANotification = delegate { };
        public event DebugEventHandler<CorLogMessageEventArgs> OnLogMessage = delegate { };
        public event DebugEventHandler<CorLogSwitchEventArgs> OnLogSwitch = delegate { };
        public event DebugEventHandler<CorAppDomainEventArgs> OnCreateAppDomain = delegate { };
        public event DebugEventHandler<CorAppDomainEventArgs> OnAppDomainExit = delegate { };
        public event DebugEventHandler<CorAssemblyEventArgs> OnAssemblyLoad = delegate { };
        public event DebugEventHandler<CorAssemblyEventArgs> OnAssemblyUnload = delegate { };
        public event DebugEventHandler<CorProcessEventArgs> OnControlCTrap = delegate { };
        public event DebugEventHandler<CorThreadEventArgs> OnNameChange = delegate { };
        public event DebugEventHandler<CorUpdateModuleSymbolsEventArgs> OnUpdateModuleSymbols = delegate { };
        public event DebugEventHandler<CorFunctionRemapOpportunityEventArgs> OnFunctionRemapOpportunity = delegate { };
        public event DebugEventHandler<CorFunctionRemapCompleteEventArgs> OnFunctionRemapComplete = delegate { };
        public event DebugEventHandler<CorException2EventArgs> OnException2 = delegate { };
        public event DebugEventHandler<CorExceptionUnwind2EventArgs> OnExceptionUnwind2 = delegate { };
        public event DebugEventHandler<CorExceptionInCallbackEventArgs> OnExceptionInCallback = delegate { };

    }

    public delegate void DebugEventHandler<in TArgs> (Object sender, TArgs args) where TArgs : CorEventArgs;

/* class Process */
} /* namespace */
