//---------------------------------------------------------------------
//  This file is part of the CLR Managed Debugger (mdbg) Sample.
// 
//  Copyright (C) Microsoft Corporation.  All rights reserved.
//---------------------------------------------------------------------
using System;
using System.Collections;
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
        }

        private static Hashtable m_instances = new Hashtable();

        private ICorDebugProcess _p ()
        {
            return (ICorDebugProcess) GetController();
        }

#if CORAPI_EXPOSE_RAW_INTERFACES
        [CLSCompliant(false)]
        public ICorDebugProcess Raw
        {
            get 
            { 
                return _p();
            }
        }
#endif

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
        public int ReadMemory (long address, byte[] buffer)
        {
            Debug.Assert(buffer!=null);
            uint read = 0;
            _p().ReadMemory ((ulong) address, (uint) buffer.Length, buffer, out read);
            return (int) read;
        }

        /** Write memory in the process. */
        public int WriteMemory (long address, byte[] buffer)
        {
            uint written = 0;
            _p().WriteMemory ((ulong) address, (uint) buffer.Length, buffer, out written);
            return (int) written;
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

        private Delegate[] m_callbacksArray = new Delegate[(int)ManagedCallbackTypeCount.Last+1];
        
        internal void DispatchEvent(ManagedCallbackType callback,CorEventArgs e)
        {
            try
            {
                if( m_callbackAttachedEvent!=null )
                    m_callbackAttachedEvent.WaitOne(); // waits till callbacks are enabled
                Debug.Assert((int)callback>=0 && (int)callback<m_callbacksArray.Length);
                Delegate d = m_callbacksArray[(int)callback];
                if( d!=null )
                    d.DynamicInvoke( new Object[]{this,e});
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
                    Delegate d = m_callbacksArray[(int)ManagedCallbackType.OnExceptionInCallback];
                    if( d!=null )
                        d.DynamicInvoke( new Object[]{this, e2});
                } 
                catch(Exception ex2)
                {
                    Debug.Assert(false,"Exception in Exception notification callback: "+ex2.ToString());
                    // ignore it -- there is nothing we can do.
                }
                e.Continue = e2.Continue;
            }
        }

        public event BreakpointEventHandler OnBreakpoint
        {
            add 
            {
                int i = (int)ManagedCallbackType.OnBreakpoint;
                m_callbacksArray[i] = (BreakpointEventHandler)m_callbacksArray[i] + value; 
            } 
            remove 
            { 
                int i = (int)ManagedCallbackType.OnBreakpoint;
                m_callbacksArray[i] = (BreakpointEventHandler)m_callbacksArray[i] - value; 
            }
        }
                
        public event BreakpointEventHandler OnBreakpointSetError
        {
            add 
            {
                int i = (int)ManagedCallbackType.OnBreakpointSetError;
                m_callbacksArray[i] = (BreakpointEventHandler)m_callbacksArray[i] + value; 
            } 
            remove 
            { 
                int i = (int)ManagedCallbackType.OnBreakpointSetError;
                m_callbacksArray[i] = (BreakpointEventHandler)m_callbacksArray[i] - value; 
            }
        }

        public event StepCompleteEventHandler OnStepComplete
        {
            add 
            {
                int i = (int)ManagedCallbackType.OnStepComplete;
                m_callbacksArray[i] = (StepCompleteEventHandler)m_callbacksArray[i] + value; 
            } 
            remove 
            { 
                int i = (int)ManagedCallbackType.OnStepComplete;
                m_callbacksArray[i] = (StepCompleteEventHandler)m_callbacksArray[i] - value; 
            }
        }

        public event CorThreadEventHandler OnBreak
        {
            add 
            {
                int i = (int)ManagedCallbackType.OnBreak;
                m_callbacksArray[i] = (CorThreadEventHandler)m_callbacksArray[i] + value; 
            } 
            remove 
            { 
                int i = (int)ManagedCallbackType.OnBreak;
                m_callbacksArray[i] = (CorThreadEventHandler)m_callbacksArray[i] - value; 
            }
        }

        public event CorExceptionEventHandler OnException
        {
            add 
            {
                int i = (int)ManagedCallbackType.OnException;
                m_callbacksArray[i] = (CorExceptionEventHandler)m_callbacksArray[i] + value; 
            } 
            remove 
            { 
                int i = (int)ManagedCallbackType.OnException;
                m_callbacksArray[i] = (CorExceptionEventHandler)m_callbacksArray[i] - value; 
            }
        }

        public event EvalEventHandler OnEvalComplete
        {
            add 
            {
                int i = (int)ManagedCallbackType.OnEvalComplete;
                m_callbacksArray[i] = (EvalEventHandler)m_callbacksArray[i] + value; 
            } 
            remove 
            { 
                int i = (int)ManagedCallbackType.OnEvalComplete;
                m_callbacksArray[i] = (EvalEventHandler)m_callbacksArray[i] - value; 
            }
        }

        public event EvalEventHandler OnEvalException
        {
            add 
            {
                int i = (int)ManagedCallbackType.OnEvalException;
                m_callbacksArray[i] = (EvalEventHandler)m_callbacksArray[i] + value; 
            } 
            remove 
            { 
                int i = (int)ManagedCallbackType.OnEvalException;
                m_callbacksArray[i] = (EvalEventHandler)m_callbacksArray[i] - value; 
            }
        }

        public event CorProcessEventHandler OnCreateProcess
        {
            add 
            {
                int i = (int)ManagedCallbackType.OnCreateProcess;
                m_callbacksArray[i] = (CorProcessEventHandler)m_callbacksArray[i] + value; 
            } 
            remove 
            { 
                int i = (int)ManagedCallbackType.OnCreateProcess;
                m_callbacksArray[i] = (CorProcessEventHandler)m_callbacksArray[i] - value; 
            }
        }

        public event CorProcessEventHandler OnProcessExit
        {
            add 
            {
                int i = (int)ManagedCallbackType.OnProcessExit;
                m_callbacksArray[i] = (CorProcessEventHandler)m_callbacksArray[i] + value; 
            } 
            remove 
            { 
                int i = (int)ManagedCallbackType.OnProcessExit;
                m_callbacksArray[i] = (CorProcessEventHandler)m_callbacksArray[i] - value; 
            }
        }

        public event CorThreadEventHandler OnCreateThread
        {
            add 
            {
                int i = (int)ManagedCallbackType.OnCreateThread;
                m_callbacksArray[i] = (CorThreadEventHandler)m_callbacksArray[i] + value; 
            } 
            remove 
            { 
                int i = (int)ManagedCallbackType.OnCreateThread;
                m_callbacksArray[i] = (CorThreadEventHandler)m_callbacksArray[i] - value; 
            }
        }

        public event CorThreadEventHandler OnThreadExit
        {
            add 
            {
                int i = (int)ManagedCallbackType.OnThreadExit;
                m_callbacksArray[i] = (CorThreadEventHandler)m_callbacksArray[i] + value; 
            } 
            remove 
            { 
                int i = (int)ManagedCallbackType.OnThreadExit;
                m_callbacksArray[i] = (CorThreadEventHandler)m_callbacksArray[i] - value; 
            }
        }

        public event CorModuleEventHandler OnModuleLoad
        {
            add 
            {
                int i = (int)ManagedCallbackType.OnModuleLoad;
                m_callbacksArray[i] = (CorModuleEventHandler)m_callbacksArray[i] + value; 
            } 
            remove 
            { 
                int i = (int)ManagedCallbackType.OnModuleLoad;
                m_callbacksArray[i] = (CorModuleEventHandler)m_callbacksArray[i] - value; 
            }
        }

        public event CorModuleEventHandler OnModuleUnload
        {
            add 
            {
                int i = (int)ManagedCallbackType.OnModuleUnload;
                m_callbacksArray[i] = (CorModuleEventHandler)m_callbacksArray[i] + value; 
            } 
            remove 
            { 
                int i = (int)ManagedCallbackType.OnModuleUnload;
                m_callbacksArray[i] = (CorModuleEventHandler)m_callbacksArray[i] - value; 
            }
        }

        public event CorClassEventHandler OnClassLoad
        {
            add 
            {
                int i = (int)ManagedCallbackType.OnClassLoad;
                m_callbacksArray[i] = (CorClassEventHandler)m_callbacksArray[i] + value; 
            } 
            remove 
            { 
                int i = (int)ManagedCallbackType.OnClassLoad;
                m_callbacksArray[i] = (CorClassEventHandler)m_callbacksArray[i] - value; 
            }
        }

        public event CorClassEventHandler OnClassUnload
        {
            add 
            {
                int i = (int)ManagedCallbackType.OnClassUnload;
                m_callbacksArray[i] = (CorClassEventHandler)m_callbacksArray[i] + value; 
            } 
            remove 
            { 
                int i = (int)ManagedCallbackType.OnClassUnload;
                m_callbacksArray[i] = (CorClassEventHandler)m_callbacksArray[i] - value; 
            }
        }

        public event DebuggerErrorEventHandler OnDebuggerError
        {
            add 
            {
                int i = (int)ManagedCallbackType.OnDebuggerError;
                m_callbacksArray[i] = (DebuggerErrorEventHandler)m_callbacksArray[i] + value; 
            } 
            remove 
            { 
                int i = (int)ManagedCallbackType.OnDebuggerError;
                m_callbacksArray[i] = (DebuggerErrorEventHandler)m_callbacksArray[i] - value; 
            }
        }

		public event MDANotificationEventHandler OnMDANotification
		{
			add {
				int i = (int)ManagedCallbackType.OnMDANotification;
				m_callbacksArray[i] = (MDANotificationEventHandler)m_callbacksArray[i] + value; 
			} 
			remove { 
				int i = (int)ManagedCallbackType.OnMDANotification;
				m_callbacksArray[i] = (MDANotificationEventHandler)m_callbacksArray[i] - value; 
			}
		}

        public event LogMessageEventHandler OnLogMessage
        {
            add 
            {
                int i = (int)ManagedCallbackType.OnLogMessage;
                m_callbacksArray[i] = (LogMessageEventHandler)m_callbacksArray[i] + value; 
            } 
            remove 
            { 
                int i = (int)ManagedCallbackType.OnLogMessage;
                m_callbacksArray[i] = (LogMessageEventHandler)m_callbacksArray[i] - value; 
            }
        }
		


        public event LogSwitchEventHandler OnLogSwitch
        {
            add 
            {
                int i = (int)ManagedCallbackType.OnLogSwitch;
                m_callbacksArray[i] = (LogSwitchEventHandler)m_callbacksArray[i] + value; 
            } 
            remove 
            { 
                int i = (int)ManagedCallbackType.OnLogSwitch;
                m_callbacksArray[i] = (LogSwitchEventHandler)m_callbacksArray[i] - value; 
            }
        }

        public event CorAppDomainEventHandler OnCreateAppDomain
        {
            add 
            {
                int i = (int)ManagedCallbackType.OnCreateAppDomain;
                m_callbacksArray[i] = (CorAppDomainEventHandler)m_callbacksArray[i] + value; 
            } 
            remove 
            { 
                int i = (int)ManagedCallbackType.OnCreateAppDomain;
                m_callbacksArray[i] = (CorAppDomainEventHandler)m_callbacksArray[i] - value; 
            }
        }

        public event CorAppDomainEventHandler OnAppDomainExit
        {
            add 
            {
                int i = (int)ManagedCallbackType.OnAppDomainExit;
                m_callbacksArray[i] = (CorAppDomainEventHandler)m_callbacksArray[i] + value; 
            } 
            remove 
            { 
                int i = (int)ManagedCallbackType.OnAppDomainExit;
                m_callbacksArray[i] = (CorAppDomainEventHandler)m_callbacksArray[i] - value; 
            }
        }

        public event CorAssemblyEventHandler OnAssemblyLoad
        {
            add 
            {
                int i = (int)ManagedCallbackType.OnAssemblyLoad;
                m_callbacksArray[i] = (CorAssemblyEventHandler)m_callbacksArray[i] + value; 
            } 
            remove 
            { 
                int i = (int)ManagedCallbackType.OnAssemblyLoad;
                m_callbacksArray[i] = (CorAssemblyEventHandler)m_callbacksArray[i] - value; 
            }
        }

        public event CorAssemblyEventHandler OnAssemblyUnload
        {
            add 
            {
                int i = (int)ManagedCallbackType.OnAssemblyUnload;
                m_callbacksArray[i] = (CorAssemblyEventHandler)m_callbacksArray[i] + value; 
            } 
            remove 
            { 
                int i = (int)ManagedCallbackType.OnAssemblyUnload;
                m_callbacksArray[i] = (CorAssemblyEventHandler)m_callbacksArray[i] - value; 
            }
        }

        public event CorProcessEventHandler OnControlCTrap
        {
            add 
            {
                int i = (int)ManagedCallbackType.OnControlCTrap;
                m_callbacksArray[i] = (CorProcessEventHandler)m_callbacksArray[i] + value; 
            } 
            remove 
            { 
                int i = (int)ManagedCallbackType.OnControlCTrap;
                m_callbacksArray[i] = (CorProcessEventHandler)m_callbacksArray[i] - value; 
            }
        }

        public event CorThreadEventHandler OnNameChange
        {
            add 
            {
                int i = (int)ManagedCallbackType.OnNameChange;
                m_callbacksArray[i] = (CorThreadEventHandler)m_callbacksArray[i] + value; 
            } 
            remove 
            { 
                int i = (int)ManagedCallbackType.OnNameChange;
                m_callbacksArray[i] = (CorThreadEventHandler)m_callbacksArray[i] - value; 
            }
        }

        public event UpdateModuleSymbolsEventHandler OnUpdateModuleSymbols
        {
            add 
            {
                int i = (int)ManagedCallbackType.OnUpdateModuleSymbols;
                m_callbacksArray[i] = (UpdateModuleSymbolsEventHandler)m_callbacksArray[i] + value; 
            } 
            remove 
            { 
                int i = (int)ManagedCallbackType.OnUpdateModuleSymbols;
                m_callbacksArray[i] = (UpdateModuleSymbolsEventHandler)m_callbacksArray[i] - value; 
            }
        }

        public event CorFunctionRemapOpportunityEventHandler OnFunctionRemapOpportunity
        {
            add 
            {
                int i = (int)ManagedCallbackType.OnFunctionRemapOpportunity;
                m_callbacksArray[i] = (CorFunctionRemapOpportunityEventHandler)m_callbacksArray[i] + value; 
            } 
            remove 
            { 
                int i = (int)ManagedCallbackType.OnFunctionRemapOpportunity;
                m_callbacksArray[i] = (CorFunctionRemapOpportunityEventHandler)m_callbacksArray[i] - value; 
            }
        }

        public event CorFunctionRemapCompleteEventHandler OnFunctionRemapComplete
        {
            add 
            {
                int i = (int)ManagedCallbackType.OnFunctionRemapComplete;
                m_callbacksArray[i] = (CorFunctionRemapCompleteEventHandler)m_callbacksArray[i] + value; 
            } 
            remove 
            { 
                int i = (int)ManagedCallbackType.OnFunctionRemapComplete;
                m_callbacksArray[i] = (CorFunctionRemapCompleteEventHandler)m_callbacksArray[i] - value; 
            }
        }

        public event CorException2EventHandler OnException2
        {
            add 
            {
                int i = (int)ManagedCallbackType.OnException2;
                m_callbacksArray[i] = (CorException2EventHandler)m_callbacksArray[i] + value; 
            } 
            remove 
            { 
                int i = (int)ManagedCallbackType.OnException2;
                m_callbacksArray[i] = (CorException2EventHandler)m_callbacksArray[i] - value; 
            }
        }

        public event CorExceptionUnwind2EventHandler OnExceptionUnwind2
        {
            add 
            {
                int i = (int)ManagedCallbackType.OnExceptionUnwind2;
                m_callbacksArray[i] = (CorExceptionUnwind2EventHandler)m_callbacksArray[i] + value; 
            } 
            remove 
            { 
                int i = (int)ManagedCallbackType.OnExceptionUnwind2;
                m_callbacksArray[i] = (CorExceptionUnwind2EventHandler)m_callbacksArray[i] - value; 
            }
        }

#if MDBG_FEATURE_INTEROP
        public event CorNativeStopEventHandler OnNativeStop
        {
            add 
            {
                int i = (int)ManagedCallbackType.OnNativeStop;
                m_callbacksArray[i] = (CorNativeStopEventHandler)m_callbacksArray[i] + value; 
            } 
            remove 
            { 
                int i = (int)ManagedCallbackType.OnNativeStop;
                m_callbacksArray[i] = (CorNativeStopEventHandler)m_callbacksArray[i] - value; 
            }
        }

#endif
        public event CorExceptionInCallbackEventHandler OnExceptionInCallback
        {
            add 
            {
                int i = (int)ManagedCallbackType.OnExceptionInCallback;
                m_callbacksArray[i] = (CorExceptionInCallbackEventHandler)m_callbacksArray[i] + value; 
            } 
            remove 
            { 
                int i = (int)ManagedCallbackType.OnExceptionInCallback;
                m_callbacksArray[i] = (CorExceptionInCallbackEventHandler)m_callbacksArray[i] - value; 
            }
        }

    } /* class Process */
} /* namespace */
