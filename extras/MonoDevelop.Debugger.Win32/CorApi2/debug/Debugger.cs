//---------------------------------------------------------------------
//  This file is part of the CLR Managed Debugger (mdbg) Sample.
// 
//  Copyright (C) Microsoft Corporation.  All rights reserved.
//---------------------------------------------------------------------
using System;
using System.Collections;
using System.Diagnostics;
using System.Runtime.InteropServices;
#if !MDBG_FAKE_COM
using System.Runtime.InteropServices.ComTypes;
#endif
using System.Threading;
using System.Text;
using System.Security.Permissions;
using System.Globalization;


using Microsoft.Samples.Debugging.CorDebug.NativeApi;

[assembly:CLSCompliant(true)]
[assembly:System.Runtime.InteropServices.ComVisible(false)]
[assembly:SecurityPermission(SecurityAction.RequestMinimum, Unrestricted=true)]

namespace Microsoft.Samples.Debugging.CorDebug
{
    /**
     * Wraps the native CLR Debugger.
     * Note that we don't derive the class from WrapperBase, becuase this
     * class will never be returned in any callback.
     */
    public sealed  class CorDebugger : MarshalByRefObject
    {
        private const int MaxVersionStringLength = 256; // == MAX_PATH
        
        public static string GetDebuggerVersionFromFile(string pathToExe)
        {
            Debug.Assert( !string.IsNullOrEmpty(pathToExe) );
            if( string.IsNullOrEmpty(pathToExe) )
                throw new ArgumentException("Value cannot be null or empty.", "pathToExe");
            int neededSize;
            StringBuilder sb = new StringBuilder(MaxVersionStringLength);
            NativeMethods.GetRequestedRuntimeVersion(pathToExe, sb, sb.Capacity, out neededSize);
            return sb.ToString();
        }

        public static string GetDebuggerVersionFromPid(int pid)
        {
            using(ProcessSafeHandle ph = NativeMethods.OpenProcess((int)(NativeMethods.ProcessAccessOptions.PROCESS_VM_READ |
                                                                         NativeMethods.ProcessAccessOptions.PROCESS_QUERY_INFORMATION |
                                                                         NativeMethods.ProcessAccessOptions.PROCESS_DUP_HANDLE |
                                                                         NativeMethods.ProcessAccessOptions.SYNCHRONIZE),
                                                                   false, // inherit handle
                                                                   pid) )
            {
                if( ph.IsInvalid )
                    throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
                int neededSize;
                StringBuilder sb = new StringBuilder(MaxVersionStringLength);
                NativeMethods.GetVersionFromProcess(ph, sb, sb.Capacity, out neededSize);
                return sb.ToString();
            }
        }

        public static string GetDefaultDebuggerVersion()
        {
            int size;
            NativeMethods.GetCORVersion(null,0,out size);
            Debug.Assert(size>0);
            StringBuilder sb = new StringBuilder(size);
            int hr = NativeMethods.GetCORVersion(sb,sb.Capacity,out size);
            Marshal.ThrowExceptionForHR(hr);
            return sb.ToString();
        }
     

        /// <summary>Creates a debugger wrapper from Guid.</summary>
        public CorDebugger(Guid debuggerGuid)
        {
            ICorDebug rawDebuggingAPI;
            NativeMethods.CoCreateInstance(ref debuggerGuid,
                                           IntPtr.Zero, // pUnkOuter
                                           1, // CLSCTX_INPROC_SERVER
                                           ref NativeMethods.IIDICorDebug,
                                           out rawDebuggingAPI);
            InitFromICorDebug(rawDebuggingAPI);
        }
        /// <summary>Creates a debugger interface that is able debug requested verison of CLR</summary>
        /// <param name="debuggerVerison">Version number of the debugging interface.</param>
        /// <remarks>The version number is usually retrieved either by calling one of following mscoree functions:
        /// GetCorVerison, GetRequestedRuntimeVersion or GetVersionFromProcess.</remarks>
        public CorDebugger (string debuggerVersion)
        {
            InitFromVersion(debuggerVersion);
        }

        ~CorDebugger()
        {
            if(m_debugger!=null)
                try 
                {
                    Terminate();
                } 
                catch
                {
                    // sometimes we cannot terminate because GC collects object in wrong
                    // order. But since the whole process is shutting down, we really
                    // don't care.
                    // TODO: we need to define IDisposable pattern to CorDebug so that we are able to
                    // dispose stuff in correct order.
                }
        }

#if CORAPI_EXPOSE_RAW_INTERFACES
        [CLSCompliant(false)]
        public ICorDebug Raw
        {
            get 
            { 
                return m_debugger;
            }
        }
#endif

        /**
         * Closes the debugger.  After this method is called, it is an error
         * to call any other methods on this object.
         */
        public void Terminate ()
        {
            Debug.Assert(m_debugger!=null);
            ICorDebug d= m_debugger;
            m_debugger = null;
            d.Terminate ();
        }

        /**
         * Specify the callback object to use for managed events.
         */
        internal void SetManagedHandler (ICorDebugManagedCallback managedCallback)
        {
            m_debugger.SetManagedHandler (managedCallback);
        }

        /**
         * Specify the callback object to use for unmanaged events.
         */
        internal void SetUnmanagedHandler (ICorDebugUnmanagedCallback nativeCallback)
        {
            m_debugger.SetUnmanagedHandler (nativeCallback);
        }

        /**
         * Launch a process under the control of the debugger.
         *
         * Parameters are the same as the Win32 CreateProcess call.
         */
        public CorProcess CreateProcess (
                                         String applicationName,
                                         String commandLine
                                         )
        {
            return CreateProcess (applicationName, commandLine, ".");
        }

        /**
         * Launch a process under the control of the debugger.
         *
         * Parameters are the same as the Win32 CreateProcess call.
         */
        public CorProcess CreateProcess (
                                         String applicationName,
                                         String commandLine,
                                         String currentDirectory
                                         )
        {
            return CreateProcess (applicationName, commandLine, currentDirectory, 0);
        }

        /**
         * Launch a process under the control of the debugger.
         *
         * Parameters are the same as the Win32 CreateProcess call.
         */
        public CorProcess CreateProcess (
                                         String applicationName,
                                         String commandLine,
                                         String currentDirectory,
                                         int    flags
                                         )
        {
            PROCESS_INFORMATION pi = new PROCESS_INFORMATION ();

            STARTUPINFO si = new STARTUPINFO ();
            si.cb = Marshal.SizeOf(si);

            // initialize safe handles 
            si.hStdInput = new Microsoft.Win32.SafeHandles.SafeFileHandle(new IntPtr(0),false);
            si.hStdOutput = new Microsoft.Win32.SafeHandles.SafeFileHandle(new IntPtr(0),false);
            si.hStdError = new Microsoft.Win32.SafeHandles.SafeFileHandle(new IntPtr(0),false);

            CorProcess ret;

            //constrained execution region (Cer)
            System.Runtime.CompilerServices.RuntimeHelpers.PrepareConstrainedRegions();
            try 
            {
            } 
            finally
            {
                ret = CreateProcess (
                                     applicationName,
                                     commandLine, 
                                     null,
                                     null,
                                     true,   // inherit handles
                                     flags,  // creation flags
                                     new IntPtr(0),      // environment
                                     currentDirectory,
                                     si,     // startup info
                                     ref pi, // process information
                                     CorDebugCreateProcessFlags.DEBUG_NO_SPECIAL_OPTIONS);
                NativeMethods.CloseHandle (pi.hProcess);
                NativeMethods.CloseHandle (pi.hThread);
            }

            return ret;
        }

        /**
         * Launch a process under the control of the debugger.
         *
         * Parameters are the same as the Win32 CreateProcess call.
         *
         * The caller should remember to execute:
         *
         *    Microsoft.Win32.Interop.Windows.CloseHandle (
         *      processInformation.hProcess);
         *
         * after CreateProcess returns.
         */
        [CLSCompliant(false)]
        public CorProcess CreateProcess (
                                         String                      applicationName,
                                         String                      commandLine,
                                         SECURITY_ATTRIBUTES         processAttributes,
                                         SECURITY_ATTRIBUTES         threadAttributes,
                                         bool                        inheritHandles,
                                         int                         creationFlags,
                                         IntPtr                      environment,  // <strip>@TODO fix the environment</strip>
                                         String                      currentDirectory,
                                         STARTUPINFO                 startupInfo,
                                         ref PROCESS_INFORMATION     processInformation,
                                         CorDebugCreateProcessFlags  debuggingFlags)
        {
            /*
             * If commandLine is: <c:\a b\a arg1 arg2> and c:\a.exe does not exist, 
             *    then without this logic, "c:\a b\a.exe" would be tried next.
             * To prevent this ambiguity, this forces the user to quote if the path 
             *    has spaces in it: <"c:\a b\a" arg1 arg2>
             */
            if(null == applicationName && !commandLine.StartsWith("\""))
            {
                int firstSpace = commandLine.IndexOf(" ");
                if(firstSpace != -1)
                    commandLine = String.Format(CultureInfo.InvariantCulture, "\"{0}\" {1}", commandLine.Substring(0,firstSpace), commandLine.Substring(firstSpace, commandLine.Length-firstSpace));
            }

            ICorDebugProcess proc = null;

            m_debugger.CreateProcess (
                                  applicationName, 
                                  commandLine, 
                                  processAttributes,
                                  threadAttributes, 
                                  inheritHandles ? 1 : 0, 
                                  (uint) creationFlags, 
                                  environment, 
                                  currentDirectory, 
                                  startupInfo, 
                                  processInformation, 
                                  debuggingFlags,
                                  out proc);

            return CorProcess.GetCorProcess(proc);
        }

        /** 
         * Attach to an active process
         */
        public CorProcess DebugActiveProcess (int processId, bool win32Attach)
        {
            ICorDebugProcess proc = null;
            m_debugger.DebugActiveProcess ((uint)processId, win32Attach ? 1 : 0, out proc);
            return CorProcess.GetCorProcess(proc);
        }

        /**
         * Enumerate all processes currently being debugged.
         */
        public IEnumerable Processes
        {
            get
            {
                ICorDebugProcessEnum eproc = null;
                m_debugger.EnumerateProcesses (out eproc);
                return new CorProcessEnumerator (eproc);
            }
        }

        /**
         * Get the Process object for the given PID.
         */
        public CorProcess GetProcess (int processId)
        {
            ICorDebugProcess proc = null;
            m_debugger.GetProcess ((uint) processId, out proc);
            return CorProcess.GetCorProcess(proc);
        }

        ////////////////////////////////////////////////////////////////////////////////
        //
        // CorDebugger private implement part
        //
        ////////////////////////////////////////////////////////////////////////////////

        // called by constructors during initialization
        private void InitFromVersion(string debuggerVersion)
        {
            if( debuggerVersion.StartsWith("v1") )
            {
                throw new ArgumentException( "Can't debug a version 1 CLR process (\"" + debuggerVersion + 
                    "\").  Run application in a version 2 CLR, or use a version 1 debugger instead." );
            }
            
            ICorDebug rawDebuggingAPI;
#if MDBG_FAKE_COM
            // TODO: Ideally, there wouldn't be any difference in the corapi code for MDBG_FAKE_COM.
            // This would require puting this initialization logic into the wrapper and interop assembly, which doesn't seem right.
            // We should also release this pUnk, but doing that here would be difficult and we aren't done with it until
            // we shutdown anyway.
            IntPtr pUnk = NativeMethods.CreateDebuggingInterfaceFromVersion((int)CorDebuggerVersion.Whidbey, debuggerVersion);
            rawDebuggingAPI = new NativeApi.CorDebugClass(pUnk);
#else
            rawDebuggingAPI = NativeMethods.CreateDebuggingInterfaceFromVersion((int)CorDebuggerVersion.Whidbey,debuggerVersion);
#endif
		    InitFromICorDebug(rawDebuggingAPI);
    	}
        
        private void InitFromICorDebug(ICorDebug rawDebuggingAPI)
        {
            Debug.Assert(rawDebuggingAPI!=null);
            if( rawDebuggingAPI==null )
                throw new ArgumentException("Cannot be null.","rawDebugggingAPI");
            
            m_debugger = rawDebuggingAPI;
            m_debugger.Initialize ();
            m_debugger.SetManagedHandler (new ManagedCallback(this));
#if MDBG_FEATURE_INTEROP
            try
            {
                m_debugger.SetUnmanagedHandler(new UnmanagedCallback(this));
            }
            catch(NotImplementedException)
            {
            }
#endif
    	}            

        /**
         * Helper for invoking events.  Checks to make sure that handlers
         * are hooked up to a handler before the handler is invoked.
         *
         * We want to allow maximum flexibility by our callers.  As such,
         * we don't require that they call <code>e.Controller.Continue</code>,
         * nor do we require that this class call it.  <b>Someone</b> needs
         * to call it, however.
         *
         * Consequently, if an exception is thrown and the process is stopped,
         * the process is continued automatically.
         */
        void InternalFireEvent(ManagedCallbackType callbackType,CorEventArgs e)
        {
            CorProcess owner;
            CorController c = e.Controller;
            Debug.Assert(c!=null);
            if(c is CorProcess)
                owner = (CorProcess)c ;
            else 
            {
                Debug.Assert(c is CorAppDomain);
                owner = (c as CorAppDomain).Process;
            }
            Debug.Assert(owner!=null);
            try 
            {
                owner.DispatchEvent(callbackType,e);
            }
            finally
            {
                if(e.Continue)
                {
#if MDBG_FEATURE_INTEROP
                    // this is special case for interop debugging
                    // where we continue from OOB native event in which
                    // case we have to call Continue with true (OOBound).
                    if( (e is CorNativeStopEventArgs)
                        && (e as CorNativeStopEventArgs).IsOutOfBand )
                        e.Controller.Continue(true);
                    else
#endif
                        e.Controller.Continue(false);
                }
            }
        }

        ////////////////////////////////////////////////////////////////////////////////
        //
        // ManagedCallback
        //
        ////////////////////////////////////////////////////////////////////////////////

        /**
         * This is the object that gets passed to the debugger.  It's
         * the intermediate "source" of the events, which repackages
         * the event arguments into a more approprate form and forwards
         * the call to the appropriate function.
         */
        private class ManagedCallback : ICorDebugManagedCallback, ICorDebugManagedCallback2
        {
            public ManagedCallback (CorDebugger outer)
            {
                m_outer = outer;
            }

            void ICorDebugManagedCallback.Breakpoint (ICorDebugAppDomain  appDomain,
                                    ICorDebugThread     thread,
                                    ICorDebugBreakpoint breakpoint)
            {
                m_outer.InternalFireEvent(ManagedCallbackType.OnBreakpoint,
                                   new CorBreakpointEventArgs (appDomain==null?null:new CorAppDomain(appDomain), 
                                                               thread==null?null:new CorThread (thread),
                                                               breakpoint==null?null:new CorFunctionBreakpoint ((ICorDebugFunctionBreakpoint)breakpoint)
                                                               ));
            }

            void ICorDebugManagedCallback.StepComplete ( ICorDebugAppDomain  appDomain,
                                       ICorDebugThread     thread,
                                       ICorDebugStepper    stepper,
                                       CorDebugStepReason  stepReason)
            {
                m_outer.InternalFireEvent(ManagedCallbackType.OnStepComplete,
                                   new CorStepCompleteEventArgs (appDomain==null?null:new CorAppDomain (appDomain), 
                                                                thread==null?null:new CorThread (thread), 
                                                                stepper==null?null:new CorStepper(stepper), 
                                                                stepReason));
            }
        
            void ICorDebugManagedCallback.Break (
                               ICorDebugAppDomain  appDomain,
                               ICorDebugThread     thread)
            {
                m_outer.InternalFireEvent(ManagedCallbackType.OnBreak,
                                   new CorThreadEventArgs (
                                                          appDomain==null?null:new CorAppDomain (appDomain), 
                                                          thread==null?null:new CorThread (thread)));
            }
        
            void ICorDebugManagedCallback.Exception (
                                                     ICorDebugAppDomain  appDomain,
                                                     ICorDebugThread     thread,
                                                     int                 unhandled)
            {
                m_outer.InternalFireEvent(ManagedCallbackType.OnException,
                                   new CorExceptionEventArgs (
                                                             appDomain==null?null:new CorAppDomain (appDomain), 
                                                             thread==null?null:new CorThread (thread),
                                                             !(unhandled == 0)));
            }
            /* pass false if ``unhandled'' is 0 -- mapping TRUE to true, etc. */

            void ICorDebugManagedCallback.EvalComplete (
                                      ICorDebugAppDomain  appDomain,
                                      ICorDebugThread     thread,
                                      ICorDebugEval       eval)
            {
                m_outer.InternalFireEvent(ManagedCallbackType.OnEvalComplete,
                                  new CorEvalEventArgs (
                                                        appDomain==null?null:new CorAppDomain(appDomain), 
                                                        thread==null?null:new CorThread (thread), 
                                                        eval==null?null:new CorEval(eval)));
            }

            void ICorDebugManagedCallback.EvalException (
                                       ICorDebugAppDomain appDomain,
                                       ICorDebugThread thread,
                                       ICorDebugEval eval)
            {
                m_outer.InternalFireEvent(ManagedCallbackType.OnEvalException,
                                  new CorEvalEventArgs (
                                                        appDomain==null?null:new CorAppDomain (appDomain), 
                                                        thread==null?null:new CorThread (thread), 
                                                        eval==null?null:new CorEval (eval)));
            }

            void ICorDebugManagedCallback.CreateProcess (
                                       ICorDebugProcess process)
            {
                m_outer.InternalFireEvent(ManagedCallbackType.OnCreateProcess,
                                  new CorProcessEventArgs (
                                                           process==null?null:CorProcess.GetCorProcess(process)));
            }

            void ICorDebugManagedCallback.ExitProcess (
                                     ICorDebugProcess process)
            {
                m_outer.InternalFireEvent(ManagedCallbackType.OnProcessExit,
                                   new CorProcessEventArgs (
                                                           process==null?null:CorProcess.GetCorProcess(process)));
            }

            void ICorDebugManagedCallback.CreateThread (
                                      ICorDebugAppDomain appDomain,
                                      ICorDebugThread thread)
            {
                m_outer.InternalFireEvent(ManagedCallbackType.OnCreateThread,
                                  new CorThreadEventArgs (
                                                          appDomain==null?null:new CorAppDomain(appDomain), 
                                                          thread==null?null:new CorThread (thread)));
            }

            void ICorDebugManagedCallback.ExitThread (
                                    ICorDebugAppDomain appDomain,
                                    ICorDebugThread thread)
            {
                m_outer.InternalFireEvent(ManagedCallbackType.OnThreadExit,
                                  new CorThreadEventArgs (
                                                          appDomain==null?null:new CorAppDomain(appDomain), 
                                                          thread==null?null:new CorThread (thread)));
            }

            void ICorDebugManagedCallback.LoadModule (
                                    ICorDebugAppDomain appDomain,
                                    ICorDebugModule managedModule)
            {
                m_outer.InternalFireEvent(ManagedCallbackType.OnModuleLoad,
                                  new CorModuleEventArgs (
                                                          appDomain==null?null:new CorAppDomain(appDomain), 
                                                          managedModule==null?null:new CorModule (managedModule)));
            }

            void ICorDebugManagedCallback.UnloadModule (
                                      ICorDebugAppDomain appDomain,
                                      ICorDebugModule managedModule)
            {
                m_outer.InternalFireEvent(ManagedCallbackType.OnModuleUnload,
                                  new CorModuleEventArgs (
                                                          appDomain==null?null:new CorAppDomain (appDomain), 
                                                          managedModule==null?null:new CorModule (managedModule)));
            }

            void ICorDebugManagedCallback.LoadClass (
                                   ICorDebugAppDomain appDomain,
                                   ICorDebugClass c)
            {
                m_outer.InternalFireEvent(ManagedCallbackType.OnClassLoad,
                                   new CorClassEventArgs (
                                                         appDomain==null?null:new CorAppDomain(appDomain), 
                                                         c==null?null:new CorClass (c)));
            }

            void ICorDebugManagedCallback.UnloadClass (
                                     ICorDebugAppDomain appDomain,
                                     ICorDebugClass c)
            {
                m_outer.InternalFireEvent(ManagedCallbackType.OnClassUnload,
                                  new CorClassEventArgs (
                                                         appDomain==null?null:new CorAppDomain(appDomain), 
                                                         c==null?null:new CorClass (c)));
            }

            void ICorDebugManagedCallback.DebuggerError (
                                       ICorDebugProcess  process,
                                       int               errorHR,
                                       uint              errorCode)
            {
                m_outer.InternalFireEvent(ManagedCallbackType.OnDebuggerError,
                                  new CorDebuggerErrorEventArgs (
                                                                 process==null?null:CorProcess.GetCorProcess (process), 
                                                                 errorHR, 
                                                                 (int) errorCode));
            }

            void ICorDebugManagedCallback.LogMessage (
                                    ICorDebugAppDomain  appDomain,
                                    ICorDebugThread     thread,
                                    int                 level,
                                    string              logSwitchName,  
                                    string              message)        
            {
                m_outer.InternalFireEvent(ManagedCallbackType.OnLogMessage,
                                   new CorLogMessageEventArgs (
                                                              appDomain==null?null:new CorAppDomain(appDomain), 
                                                              thread==null?null:new CorThread (thread), 
                                                              level, logSwitchName, message));
            }

            void ICorDebugManagedCallback.LogSwitch (
                                   ICorDebugAppDomain  appDomain,
                                   ICorDebugThread     thread,
                                   int                 level,
                                   uint                reason,
                                   string              logSwitchName,  
                                   string              parentName)     
            {
                m_outer.InternalFireEvent(ManagedCallbackType.OnLogSwitch,
                                  new CorLogSwitchEventArgs (
                                                             appDomain==null?null:new CorAppDomain(appDomain), 
                                                             thread==null?null:new CorThread (thread), 
                                                             level, (int) reason, logSwitchName, parentName));
            }

            void ICorDebugManagedCallback.CreateAppDomain (
                                         ICorDebugProcess    process,
                                         ICorDebugAppDomain  appDomain)
            {
                m_outer.InternalFireEvent(ManagedCallbackType.OnCreateAppDomain,
                                  new CorAppDomainEventArgs (
                                                             process==null?null:CorProcess.GetCorProcess(process), 
                                                             appDomain==null?null:new CorAppDomain(appDomain)));
            }

            void ICorDebugManagedCallback.ExitAppDomain (
                                       ICorDebugProcess    process,
                                       ICorDebugAppDomain  appDomain)
            {
                m_outer.InternalFireEvent(ManagedCallbackType.OnAppDomainExit,
                                  new CorAppDomainEventArgs (
                                                             process==null?null:CorProcess.GetCorProcess(process), 
                                                             appDomain==null?null:new CorAppDomain (appDomain)));
            }

            void ICorDebugManagedCallback.LoadAssembly (
                                      ICorDebugAppDomain  appDomain,
                                      ICorDebugAssembly   assembly)
            {
                m_outer.InternalFireEvent(ManagedCallbackType.OnAssemblyLoad,
                                  new CorAssemblyEventArgs (
                                                            appDomain==null?null:new CorAppDomain (appDomain), 
                                                            assembly==null?null:new CorAssembly (assembly)));
            }

            void ICorDebugManagedCallback.UnloadAssembly (
                                        ICorDebugAppDomain  appDomain,
                                        ICorDebugAssembly   assembly)
            {
                m_outer.InternalFireEvent(ManagedCallbackType.OnAssemblyUnload,
                                  new CorAssemblyEventArgs (
                                                            appDomain==null?null:new CorAppDomain(appDomain), 
                                                            assembly==null?null:new CorAssembly (assembly)));
            }

            void ICorDebugManagedCallback.ControlCTrap (ICorDebugProcess process)
            {
                m_outer.InternalFireEvent(ManagedCallbackType.OnControlCTrap,
                                  new CorProcessEventArgs (
                                                           process==null?null:CorProcess.GetCorProcess(process)
                                                           ));
            }

            void ICorDebugManagedCallback.NameChange (
                                    ICorDebugAppDomain  appDomain,
                                    ICorDebugThread     thread)
            {
                m_outer.InternalFireEvent(ManagedCallbackType.OnNameChange,
                                  new CorThreadEventArgs (
                                                          appDomain==null?null:new CorAppDomain(appDomain), 
                                                          thread==null?null:new CorThread (thread)));
            }

        // TODO: Enable support for dynamic modules (reflection emit) with FAKE_COM?
        // Rotor doesn't have ISymbolBinder2 or IStream, so we may have to add them
            void ICorDebugManagedCallback.UpdateModuleSymbols (
                                             ICorDebugAppDomain  appDomain,
                                             ICorDebugModule     managedModule,
#if MDBG_FAKE_COM
                                            IntPtr                  stream)
#else
                                             IStream             stream)
#endif
            {
                m_outer.InternalFireEvent(ManagedCallbackType.OnUpdateModuleSymbols,
                                  new CorUpdateModuleSymbolsEventArgs(
                                                                      appDomain==null?null:new CorAppDomain(appDomain), 
                                                                      managedModule==null?null:new CorModule (managedModule), 
                                                                      stream));
            }

            void ICorDebugManagedCallback.EditAndContinueRemap(
                                             ICorDebugAppDomain appDomain, 
                                             ICorDebugThread thread, 
                                             ICorDebugFunction managedFunction, 
                                             int isAccurate)
            {
                Debug.Assert(false); //OBSOLETE callback
            }

            
            void ICorDebugManagedCallback.BreakpointSetError(
                                           ICorDebugAppDomain appDomain, 
                                           ICorDebugThread thread, 
                                           ICorDebugBreakpoint breakpoint, 
                                           UInt32 errorCode)
            {
                m_outer.InternalFireEvent(ManagedCallbackType.OnBreakpointSetError,
                                  new CorBreakpointSetErrorEventArgs(
                                                            appDomain==null?null:new CorAppDomain(appDomain),
                                                            thread==null?null:new CorThread(thread),
                                                            null, // <strip>@TODO breakpoint==null?null:new CorBreakpoint(breakpoint),</strip>
                                                            (int)errorCode));
            }

            void ICorDebugManagedCallback2.FunctionRemapOpportunity(ICorDebugAppDomain appDomain,
                                                                           ICorDebugThread thread,
                                                                           ICorDebugFunction oldFunction,
                                                                           ICorDebugFunction newFunction,
                                                                           uint oldILoffset)
            {
                m_outer.InternalFireEvent(ManagedCallbackType.OnFunctionRemapOpportunity,
                                          new CorFunctionRemapOpportunityEventArgs(
                                                                                   appDomain==null?null:new CorAppDomain(appDomain), 
                                                                                   thread==null?null:new CorThread (thread),
                                                                                   oldFunction==null?null:new CorFunction(oldFunction),
                                                                                   newFunction==null?null:new CorFunction(newFunction),
                                                                                   (int)oldILoffset
                                                                                   ));
            }

            void ICorDebugManagedCallback2.FunctionRemapComplete(ICorDebugAppDomain appDomain, 
                                                                 ICorDebugThread thread, 
                                                                 ICorDebugFunction managedFunction)
            {
                m_outer.InternalFireEvent(ManagedCallbackType.OnFunctionRemapComplete,
                                   new CorFunctionRemapCompleteEventArgs(
                                                          appDomain==null?null:new CorAppDomain(appDomain), 
                                                          thread==null?null:new CorThread (thread),
                                                          managedFunction==null?null:new CorFunction(managedFunction)
                                                          ));
            }

            void ICorDebugManagedCallback2.CreateConnection(ICorDebugProcess process,uint connectionId, ref ushort connectionName)
            {
                // <strip>@TODO - </strip>Not Implemented
                Debug.Assert(false);
            }
            
            void ICorDebugManagedCallback2.ChangeConnection(ICorDebugProcess process,uint connectionId)
            {
                // <strip>@TODO -</strip> Not Implemented
                Debug.Assert(false);
            }
            
            void ICorDebugManagedCallback2.DestroyConnection(ICorDebugProcess process,uint connectionId)
            {
                // <strip>@TODO - </strip>Not Implemented
                Debug.Assert(false);
            }

            void ICorDebugManagedCallback2.Exception(ICorDebugAppDomain ad, ICorDebugThread thread, 
                                                     ICorDebugFrame frame, uint offset, 
                                                     CorDebugExceptionCallbackType eventType, uint flags) //@TODO flags should not be UINT
            {
                m_outer.InternalFireEvent(ManagedCallbackType.OnException2,
                                          new CorException2EventArgs(
                                                            ad==null?null:new CorAppDomain(ad), 
                                                            thread==null?null:new CorThread (thread),
                                                            frame==null?null:new CorFrame(frame),
                                                            (int)offset,
                                                            eventType, 
                                                            (int)flags
                                                            ));
            }
            
            void ICorDebugManagedCallback2.ExceptionUnwind(ICorDebugAppDomain ad, ICorDebugThread thread, 
                                                           CorDebugExceptionUnwindCallbackType eventType, uint flags)
            {
                m_outer.InternalFireEvent(ManagedCallbackType.OnExceptionUnwind2,
                                          new CorExceptionUnwind2EventArgs(
                                                            ad==null?null:new CorAppDomain(ad), 
                                                            thread==null?null:new CorThread (thread),
                                                            eventType, 
                                                            (int)flags
                                                            ));
            }

            // Get process from controller 
            private CorProcess GetProcessFromController(ICorDebugController pController)
            {
                CorProcess p;
                ICorDebugProcess p2 = pController as ICorDebugProcess;
                if (p2 != null)
                {
                    p = CorProcess.GetCorProcess(p2);
                }
                else
                {
                    ICorDebugAppDomain a2 = (ICorDebugAppDomain) pController;
                    p = new CorAppDomain(a2).Process;
                }
                return p;
            }

            void ICorDebugManagedCallback2.MDANotification(ICorDebugController pController,
                                                           ICorDebugThread   thread,
                                                           ICorDebugMDA  pMDA)
            {
                CorMDA c = new CorMDA(pMDA);
                string szName = c.Name;
                CorDebugMDAFlags f = c.Flags;
                CorProcess p = GetProcessFromController(pController);
                
                
                m_outer.InternalFireEvent(ManagedCallbackType.OnMDANotification,
                                          new CorMDAEventArgs (c, 
                                                               thread==null?null:new CorThread (thread),
                                                               p));
            }
            
            private CorDebugger m_outer;
        }

        
        
#if MDBG_FEATURE_INTEROP
        private class UnmanagedCallback : ICorDebugUnmanagedCallback
        {
            public UnmanagedCallback (CorDebugger outer)
            {
                Debug.Assert(outer!=null);
                m_outer = outer;
            }

#if !FIXED_BUG21115 //<strip>@TODO  remove once we'll be able to call Continue from Win32 callbakck.</strip>
            private struct Win32NativeEvent
            {
                public Win32NativeEvent(CorProcess process,CorDebugger outer,int threadId,DEBUG_EVENT debugEvent)
                {
                    Debug.Assert(process!=null);
                    Debug.Assert(outer!=null);
                    m_process = process;
                    m_outer = outer;
                    m_threadId = threadId;
                    m_debugEvent = debugEvent;
                }
                private CorProcess m_process;
                private CorDebugger m_outer;
                private int m_threadId;
                private DEBUG_EVENT m_debugEvent;

				// This is always dispatched on the W32ET. That thread can't do anything useful so we
				// hand off to another thread.
                static public void DispatchEvent(Win32NativeEvent event_)
                {
					// If Mdbg has mutliple debuggees, they'll share this.
					lock(g_lock)
					{
						if( g_workToDo == null )
						{
							g_workToDo     = new AutoResetEvent(false);
							g_callWorkDone = new AutoResetEvent(true);
							Thread t = new System.Threading.Thread(new System.Threading.ThreadStart(Win32NativeEvent.Dispatch));
							t.IsBackground = true;
							t.Start();
						}
					}

					g_callWorkDone.WaitOne();
					g_event = event_;
					g_workToDo.Set();

					// We now return without waiting for the DispatchThread to finish
                }

				// Statics 
				static private object g_lock = new object();
                static private AutoResetEvent g_callWorkDone=null;
                static private AutoResetEvent g_workToDo = null;
                static private Win32NativeEvent g_event;

				// This is called on a separate thread from the W32ET. MDbg shares this thread across all debuggees
				// (in the same way ICorDebug shares the RCET)
                public static void Dispatch()
                {
                    while(true)
                    {
						try
						{
							g_workToDo.WaitOne();

							g_event.m_outer.InternalFireEvent(ManagedCallbackType.OnNativeStop,
								new CorNativeStopEventArgs (g_event.m_process,
								g_event.m_threadId,
								g_event.m_debugEvent,
								false /*not OOB event*/));
						}
						finally
						{
							g_callWorkDone.Set();
						}
                    }
                }
            }
#endif
            void Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugUnmanagedCallback.DebugEvent(DEBUG_EVENT debugEvent, int isOutOfBand)
            {
                try 
                {                
                    CorProcess corProcess = m_outer.GetProcess((int)debugEvent.dwProcessId);
                    string callArgs = NativeDebugEvent.DebugEventToString(corProcess,debugEvent);
                    Trace.WriteLine("UnmanagedCallback::DebugEvent(fOutOfBand="+isOutOfBand+")" + callArgs);
                    
                    // Should never get Out-Of-Band Native BPs. <strip>(@TODO could happen in interop debugging scenarios)</strip>
                    if( ((NativeDebugEventCode)debugEvent.dwDebugEventCode == NativeDebugEventCode.EXCEPTION_DEBUG_EVENT) &&
                        (debugEvent.Exception.ExceptionRecord.ExceptionCode == 0x80000003) && (isOutOfBand != 0) )
                    {
                        Debug.Assert(false,"ERROR: Out-Of-Band native breakpoint recieved!");
                    }
                    
                    // Should never get In-Of-Band ExitThread Event (for Whidbey, not true in Everett)
                    if( ((NativeDebugEventCode)debugEvent.dwDebugEventCode == NativeDebugEventCode.EXIT_THREAD_DEBUG_EVENT) &&
                        (isOutOfBand == 0) )
                    {
                        Debug.Assert(false,"ERROR: In-Band ExitThread unamanged event received!");
                    }
                    
                    // dispatch the event
                    if(isOutOfBand!=0)
                    {
                        /* note that in this event debugger cannot use any managed debuggger API,
                         * therefore it in effect cannot stop.
                         * The CorNativeStopEventArgs class has a check that Continue cannot be set to false
                         * in this case.
                         */
                        m_outer.InternalFireEvent(ManagedCallbackType.OnNativeStop,
                                                  new CorNativeStopEventArgs (corProcess,
                                                                              (int)debugEvent.dwThreadId,
                                                                              debugEvent,
                                                                              true /*is OOB event*/));
                        // since the user cannot signal that we want to stop on OOB event, InternalFireEvent will always call
                        // continue for us.
                    }
                    else
                    {
                        // we need to dispatch handling and managing Win32 events
                        // received to other thread (Need documentation).
                        // 
                        Win32NativeEvent.DispatchEvent(new Win32NativeEvent(corProcess,m_outer,(int)debugEvent.dwThreadId,debugEvent));
                    }
                    

                    // We need to take care of closing *SOME* handles.
                    // following sample give us some clues....
                    // ms-help://MS.MSDNQTR.2002OCT.1033/debug/base/writing_the_debugger_s_main_loop.htm
                    // some handles are closed by ContinueDebugEvent as described in
                    // ms-help://MS.MSDNQTR.2002OCT.1033/debug/base/continuedebugevent.htm
                    //
                    switch((NativeDebugEventCode)debugEvent.dwDebugEventCode)
                    {
                    case NativeDebugEventCode.CREATE_PROCESS_DEBUG_EVENT:
                        NativeMethods.CloseHandle(debugEvent.CreateProcess.hFile);
                        break;
                    case NativeDebugEventCode.LOAD_DLL_DEBUG_EVENT:
                        NativeMethods.CloseHandle(debugEvent.LoadDll.hFile);
                        break;
                        
                        // this special case is for debugging on Win2k, where
                        // OUTPUT_DEBUG_STRING_EVENT requires clrearing exception
                    case NativeDebugEventCode.OUTPUT_DEBUG_STRING_EVENT:
                        corProcess.ClearCurrentException((int)debugEvent.dwThreadId);
                        break;
                    }
                }
                catch(Exception e) 
                {
                    Debug.Assert(false,"Exception in Win32 callback "+e.ToString());
                    throw;
                }
            }
            private CorDebugger m_outer = null;

        }

#endif	//	MDBG_FEATURE_INTEROP
        private ICorDebug m_debugger = null;
    } /* class Debugger */


	public class ProcessSafeHandle : Microsoft.Win32.SafeHandles.SafeHandleZeroOrMinusOneIsInvalid
    {
        private ProcessSafeHandle()
            : base(true)
        {
        }
        
        private ProcessSafeHandle(IntPtr handle, bool ownsHandle) : base (ownsHandle)
        {
            SetHandle(handle);
        }
     
        override protected bool ReleaseHandle()
        {
            return NativeMethods.CloseHandle(handle);
        }
    }

    internal static class NativeMethods
    {
        private const string Kernel32LibraryName = "kernel32.dll";
        private const string Ole32LibraryName    = "ole32.dll";
#if FEATURE_PAL
        private const string ShimLibraryName = "sscoree.dll";
#else
        private const string ShimLibraryName = "mscoree.dll";
#endif

        [
         System.Runtime.ConstrainedExecution.ReliabilityContract(System.Runtime.ConstrainedExecution.Consistency.WillNotCorruptState, System.Runtime.ConstrainedExecution.Cer.Success),
         DllImport(Kernel32LibraryName)
        ]
        public static extern bool CloseHandle(IntPtr handle);


        [
         DllImport(ShimLibraryName, CharSet=CharSet.Unicode, PreserveSig=false)
        ]
#if MDBG_FAKE_COM
        public static extern IntPtr CreateDebuggingInterfaceFromVersion(int iDebuggerVersion
                                                                           ,string szDebuggeeVersion);
#else
        public static extern ICorDebug CreateDebuggingInterfaceFromVersion(int iDebuggerVersion
                                                                           ,string szDebuggeeVersion);
#endif

        [
         DllImport(ShimLibraryName, CharSet=CharSet.Unicode)
        ]
        public static extern int GetCORVersion([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder  szName
                                               ,Int32 cchBuffer
                                               ,out Int32 dwLength);

        [
         DllImport(ShimLibraryName, CharSet=CharSet.Unicode, PreserveSig=false)
        ]
        public static extern void GetVersionFromProcess(ProcessSafeHandle hProcess, StringBuilder versionString,
                                                        Int32 bufferSize, out Int32 dwLength);

        [
         DllImport(ShimLibraryName, CharSet=CharSet.Unicode, PreserveSig=false)
        ]
        public static extern void GetRequestedRuntimeVersion(string pExe, StringBuilder pVersion,
                                                             Int32 cchBuffer, out Int32 dwLength);

        public enum ProcessAccessOptions : int
        {
            PROCESS_TERMINATE         = 0x0001,
            PROCESS_CREATE_THREAD     = 0x0002,
            PROCESS_SET_SESSIONID     = 0x0004,
            PROCESS_VM_OPERATION      = 0x0008,
            PROCESS_VM_READ           = 0x0010,
            PROCESS_VM_WRITE          = 0x0020,
            PROCESS_DUP_HANDLE        = 0x0040,
            PROCESS_CREATE_PROCESS    = 0x0080,
            PROCESS_SET_QUOTA         = 0x0100,
            PROCESS_SET_INFORMATION   = 0x0200,
            PROCESS_QUERY_INFORMATION = 0x0400,
            PROCESS_SUSPEND_RESUME    = 0x0800,
            SYNCHRONIZE               = 0x100000,
        }

        [
         DllImport(Kernel32LibraryName, PreserveSig=true)
        ]
        public static extern ProcessSafeHandle OpenProcess(Int32 dwDesiredAccess, bool bInheritHandle, Int32 dwProcessId);

        public static Guid IIDICorDebug = new Guid("3d6f5f61-7538-11d3-8d5b-00104b35e7ef");
        
        [
         DllImport(Ole32LibraryName, PreserveSig=false)
        ]
        public static extern void CoCreateInstance(ref Guid rclsid, IntPtr pUnkOuter,
                                                   Int32 dwClsContext,
                                                   ref Guid riid, // must be "ref NativeMethods.IIDICorDebug"
                                                   [MarshalAs(UnmanagedType.Interface)]out ICorDebug debuggingInterface
                                                   );
    }

    ////////////////////////////////////////////////////////////////////////////////
    //
    // CorEvent Classes & Corresponding delegates
    //
    ////////////////////////////////////////////////////////////////////////////////
    
    /**
     * All of the Debugger events make a Controller available (to specify
     * whether or not to continue the program, or to stop, etc.).
     *
     * This serves as the base class for all events used for debugging.
     *
     * NOTE: If you don't want <b>Controller.Continue(false)</b> to be
     * called after event processing has finished, you need to set the
     * <b>Continue</b> property to <b>false</b>.
     */

    public class CorEventArgs : EventArgs 
    {
        private CorController m_controller;

        private bool m_continue;

        public CorEventArgs (CorController controller)
        {
            m_controller = controller;
            m_continue = true;
        }

        /** The Controller of the current event. */
        public CorController  Controller
        {
            get 
            {
                return m_controller;
            }
        }

        /** 
         * The default behavior after an event is to Continue processing
         * after the event has been handled.  This can be changed by
         * setting this property to false.
         */
        public virtual bool Continue
        {
            get 
            {
                return m_continue;
            }
            set 
            {
                m_continue = value;
            }
        }
    }


    /**
     * This class is used for all events that only have access to the 
     * CorProcess that is generating the event.
     */
    public class CorProcessEventArgs : CorEventArgs
    {
        public CorProcessEventArgs (CorProcess process)
            : base (process)
        {
        }

        /** The process that generated the event. */
        public CorProcess Process
        {
            get 
            {
                return (CorProcess) Controller;
            }
        }
    }

    public delegate void CorProcessEventHandler (Object sender, 
                                                 CorProcessEventArgs e);


    /**
     * The event arguments for events that contain both a CorProcess
     * and an CorAppDomain.
     */
    public class CorAppDomainEventArgs : CorProcessEventArgs
    {
        private CorAppDomain m_ad;

        public CorAppDomainEventArgs (CorProcess process, CorAppDomain ad)
            : base (process)
        {
            m_ad = ad;
        }

        /** The AppDomain that generated the event. */
        public CorAppDomain AppDomain
        {
            get 
            {
                return m_ad;
            }
        }
    }

    public delegate void CorAppDomainEventHandler (Object sender, 
                                                   CorAppDomainEventArgs e);

  
    /**
     * The base class for events which take an CorAppDomain as their
     * source, but not a CorProcess.
     */
    public class CorAppDomainBaseEventArgs : CorEventArgs
    {
        public CorAppDomainBaseEventArgs (CorAppDomain ad)
            : base (ad)
        {
        }

        public CorAppDomain AppDomain
        {
            get 
            {
                return (CorAppDomain) Controller;
            }
        }
    }


    /**
     * Arguments for events dealing with threads.
     */
    public class CorThreadEventArgs : CorAppDomainBaseEventArgs
    {
        private CorThread m_thread;

        public CorThreadEventArgs (CorAppDomain appDomain, CorThread thread)
            : base (appDomain!=null?appDomain:thread.AppDomain)
        {
            m_thread = thread;
        }

        /** The CorThread of interest. */
        public CorThread Thread
        {
            get 
            {
                return m_thread;
            }
        }
    }

    public delegate void CorThreadEventHandler (Object sender, 
                                                CorThreadEventArgs e);


    /**
     * Arguments for events involving breakpoints.
     */
    public class CorBreakpointEventArgs : CorThreadEventArgs
    {
        private CorBreakpoint m_break;

        public CorBreakpointEventArgs (CorAppDomain appDomain, 
                                       CorThread thread, 
                                       CorBreakpoint managedBreakpoint)
            : base (appDomain, thread)
        {
            m_break = managedBreakpoint;
        }

        /** The breakpoint involved. */
        public CorBreakpoint Breakpoint
        {
            get 
            {
                return m_break;
            }
        }
    }

    public delegate void BreakpointEventHandler (Object sender, 
                                                 CorBreakpointEventArgs e);


    /**
     * Arguments for when a Step operation has completed.
     */
    public class CorStepCompleteEventArgs : CorThreadEventArgs
    {
        private CorStepper    m_stepper;
        private CorDebugStepReason  m_stepReason;

        [CLSCompliant(false)]
        public CorStepCompleteEventArgs (CorAppDomain appDomain, CorThread thread, 
                                         CorStepper stepper, CorDebugStepReason stepReason)
            : base (appDomain, thread)
        {
            m_stepper = stepper;
            m_stepReason = stepReason;
        }

        public CorStepper Stepper
        {
            get 
            {
                return m_stepper;
            }
        }

        [CLSCompliant(false)]
        public CorDebugStepReason StepReason
        {
            get 
            {
                return m_stepReason;
            }
        }
    }

    public delegate void StepCompleteEventHandler (Object sender, 
                                                   CorStepCompleteEventArgs e);


    /**
     * For events dealing with exceptions.
     */
    public class CorExceptionEventArgs : CorThreadEventArgs
    {
        bool  m_unhandled;

        public CorExceptionEventArgs (CorAppDomain appDomain, 
                                      CorThread thread, 
                                      bool unhandled)
            : base (appDomain, thread)
        {
            m_unhandled = unhandled;
        }

        /** Has the exception been handled yet? */
        public bool Unhandled
        {
            get 
            {
                return m_unhandled;
            }
        }
    }

    public delegate void CorExceptionEventHandler (Object sender, 
                                                   CorExceptionEventArgs e);


    /**
     * For events dealing the evaluation of something...
     */
    public class CorEvalEventArgs : CorThreadEventArgs
    {
        CorEval m_eval;

        public CorEvalEventArgs (CorAppDomain appDomain, CorThread thread, 
                                 CorEval eval)
            : base (appDomain, thread)
        {
            m_eval = eval;
        }

        /** The object being evaluated. */
        public CorEval  Eval
        {
            get 
            {
                return m_eval;
            }
        }
    }

    public delegate void EvalEventHandler (Object sender, CorEvalEventArgs e);


    /**
     * For events dealing with module loading/unloading.
     */
    public class CorModuleEventArgs : CorAppDomainBaseEventArgs
    {
        CorModule m_managedModule;

        public CorModuleEventArgs (CorAppDomain appDomain, CorModule managedModule)
            : base (appDomain)
        {
            m_managedModule = managedModule;
        }

        public CorModule  Module
        {
            get 
            {
                return m_managedModule;
            }
        }
    }

    public delegate void CorModuleEventHandler (Object sender, 
                                                CorModuleEventArgs e);


    /**
     * For events dealing with class loading/unloading.
     */
    public class CorClassEventArgs : CorAppDomainBaseEventArgs
    {
        CorClass m_class;

        public CorClassEventArgs (CorAppDomain appDomain, CorClass managedClass)
            : base (appDomain)
        {
            m_class = managedClass;
        }

        public CorClass  Class
        {
            get 
            {
                return m_class;
            }
        }
    }

    public delegate void CorClassEventHandler (Object sender, 
                                               CorClassEventArgs e);

  
    /**
     * For events dealing with debugger errors.
     */
    public class CorDebuggerErrorEventArgs : CorProcessEventArgs
    {
        int   m_hresult;
        int   m_errorCode;

        public CorDebuggerErrorEventArgs (CorProcess process, int hresult, 
                                          int errorCode)
            : base (process)
        {
            m_hresult = hresult;
            m_errorCode = errorCode;
        }
        
        public int  HResult
        {
            get 
            {
                return m_hresult;
            }
        }

        public int ErrorCode
        {
            get 
            {
                return m_errorCode;
            }
        }
    }

    public delegate void DebuggerErrorEventHandler (Object sender, 
                                                    CorDebuggerErrorEventArgs e);


    /**
     * For events dealing with Assemblies.
     */
    public class CorAssemblyEventArgs : CorAppDomainBaseEventArgs
    {
        private CorAssembly m_assembly;
        public CorAssemblyEventArgs (CorAppDomain appDomain, 
                                     CorAssembly assembly)
            : base (appDomain)
        {
            m_assembly = assembly;
        }

        /** The Assembly of interest. */
        public CorAssembly Assembly
        {
            get 
            {
                return m_assembly;
            }
        }
    }

    public delegate void CorAssemblyEventHandler (Object sender, 
                                                  CorAssemblyEventArgs e);


    /**
     * For events dealing with logged messages.
     */
    public class CorLogMessageEventArgs : CorThreadEventArgs
    {
        int m_level;
        string m_logSwitchName;
        string m_message;

        public CorLogMessageEventArgs (CorAppDomain appDomain, CorThread thread,
                                       int level, string logSwitchName, string message)
            : base (appDomain, thread)
        {
            m_level = level;
            m_logSwitchName = logSwitchName;
            m_message = message;
        }

        public int  Level
        {
            get 
            {
                return m_level;
            }
        }

        public string  LogSwitchName
        {
            get 
            {
                return m_logSwitchName;
            }
        }

        public string Message
        {
            get 
            {
                return  m_message;
            }
        }
    }

    public delegate void LogMessageEventHandler (Object sender, 
                                                 CorLogMessageEventArgs e);


    /**
     * For events dealing with logged messages.
     */
    public class CorLogSwitchEventArgs : CorThreadEventArgs
    {
        int m_level;

        int m_reason;

        string m_logSwitchName;

        string m_parentName;

        public CorLogSwitchEventArgs (CorAppDomain appDomain, CorThread thread,
                                      int level, int reason, string logSwitchName, string parentName)
            : base (appDomain, thread)
        {
            m_level = level;
            m_reason = reason;
            m_logSwitchName = logSwitchName;
            m_parentName = parentName;
        }

        public int  Level
        {
            get 
            {
                return m_level;
            }
        }

        public int Reason
        {
            get 
            {
                return m_reason;
            }
        }

        public string  LogSwitchName
        {
            get 
            {
                return m_logSwitchName;
            }
        }

        public string ParentName
        {
            get 
            {
                return  m_parentName;
            }
        }
    }

    public delegate void LogSwitchEventHandler (Object sender, 
                                                CorLogSwitchEventArgs e);


	/**
	 * For events dealing with MDA messages.
	 */
	public class CorMDAEventArgs : CorProcessEventArgs
    {
        // Thread may be null.
		public CorMDAEventArgs (CorMDA mda, CorThread thread, CorProcess proc)
			: base (proc)
		{
		    m_mda = mda;
		    m_thread = thread;
		    //m_proc = proc;
		}

        CorMDA m_mda;
        public CorMDA MDA { get { return m_mda; } }
        
        CorThread m_thread;
        public CorThread Thread { get  {return m_thread; } }
                
        //CorProcess m_proc;
        //CorProcess Process { get { return m_proc; } }
    }

	public delegate void MDANotificationEventHandler (Object sender, 
												 CorMDAEventArgs e);



    /**
     * For events dealing module symbol updates.
     */
    public class CorUpdateModuleSymbolsEventArgs : CorModuleEventArgs
    {
#if MDBG_FAKE_COM
        IntPtr m_stream;

        [CLSCompliant(false)]
        public CorUpdateModuleSymbolsEventArgs (CorAppDomain appDomain, 
                                                CorModule managedModule, 
                                                IntPtr stream)
            : base (appDomain, managedModule)
        {
            m_stream = stream ;
        }

        [CLSCompliant(false)]
        public IntPtr Stream
        {
            get 
            {
                return m_stream;
            }
        }


#else
        IStream m_stream;

        [CLSCompliant(false)]
        public CorUpdateModuleSymbolsEventArgs (CorAppDomain appDomain, 
                                                CorModule managedModule, 
                                                IStream stream)
            : base (appDomain, managedModule)
        {
            m_stream = stream ;
        }

        [CLSCompliant(false)]
        public IStream Stream
        {
            get 
            {
                return m_stream;
            }
        }
#endif
    }

    public delegate void UpdateModuleSymbolsEventHandler (Object sender, 
                                                          CorUpdateModuleSymbolsEventArgs e);

    public sealed  class CorExceptionInCallbackEventArgs: CorEventArgs
    {
        public CorExceptionInCallbackEventArgs(CorController controller,Exception exceptionThrown)
            : base(controller)
        {
            m_exceptionThrown = exceptionThrown;
        }

        public Exception ExceptionThrown
        { 
            get 
            { 
                return m_exceptionThrown; 
            } 
        }

        private Exception m_exceptionThrown;
    }

    public delegate void CorExceptionInCallbackEventHandler(Object sender,
                                             CorExceptionInCallbackEventArgs e);


    /**
     * Edit and Continue callbacks
     */
    public class CorEditAndContinueRemapEventArgs : CorThreadEventArgs
    {
        public CorEditAndContinueRemapEventArgs (CorAppDomain appDomain, 
                                        CorThread thread,
                                        CorFunction managedFunction,
                                        int acccurate)
            : base (appDomain, thread)
        {
            m_managedFunction = managedFunction ;
            m_accurate = acccurate;
        }

        public CorFunction Function
        {
            get 
            {
                return m_managedFunction;
            }
        }

        public bool IsAccurate
        {
            get 
            {
                return m_accurate!=0;
            }
        }

        private CorFunction m_managedFunction;
        private int m_accurate;
    }
    public delegate void CorEditAndContinueRemapEventHandler (Object sender, 
                                                              CorEditAndContinueRemapEventArgs e);

    
    public class CorBreakpointSetErrorEventArgs : CorThreadEventArgs
    {
        public CorBreakpointSetErrorEventArgs (CorAppDomain appDomain, 
                                        CorThread thread,
                                        CorBreakpoint breakpoint,
                                        int errorCode)
            : base (appDomain, thread)
        {
            m_breakpoint = breakpoint ;
            m_errorCode = errorCode ;
        }

        public CorBreakpoint Breakpoint
        {
            get 
            {
                return m_breakpoint;
            }
        }

        public int ErrorCode
        {
            get 
            {
                return m_errorCode;
            }
        }

        private CorBreakpoint m_breakpoint;
        private int m_errorCode;
    }
    public delegate void CorBreakpointSetErrorEventHandler(Object sender, 
                                                           CorBreakpointSetErrorEventArgs e);


    public sealed  class CorFunctionRemapOpportunityEventArgs: CorThreadEventArgs
    {
        public CorFunctionRemapOpportunityEventArgs(CorAppDomain appDomain, 
                                           CorThread thread,
                                           CorFunction oldFunction,
                                           CorFunction newFunction,
                                           int oldILoffset
                                           )
            : base (appDomain, thread)
        {
            m_oldFunction = oldFunction;
            m_newFunction = newFunction;
            m_oldILoffset = oldILoffset;
        }

        public CorFunction OldFunction
        {
            get 
            {
                return m_oldFunction;
            }
        }

        public CorFunction NewFunction
        {
            get 
            {
                return m_newFunction;
            }
        }

        public int OldILOffset
        {
            get 
            {
                return m_oldILoffset;
            }
        }
        
        private CorFunction m_oldFunction,m_newFunction;
        private int m_oldILoffset;
    }

    public delegate void CorFunctionRemapOpportunityEventHandler(Object sender,
                                                       CorFunctionRemapOpportunityEventArgs e);

    public sealed  class CorFunctionRemapCompleteEventArgs: CorThreadEventArgs
    {
        public CorFunctionRemapCompleteEventArgs(CorAppDomain appDomain, 
                                           CorThread thread,
                                           CorFunction managedFunction
                                           )
            : base (appDomain, thread)
        {
            m_managedFunction = managedFunction;
        }

        public CorFunction Function
        {
            get 
            {
                return m_managedFunction;
            }
        }

        private CorFunction m_managedFunction;
    }

    public delegate void CorFunctionRemapCompleteEventHandler(Object sender,
                                                              CorFunctionRemapCompleteEventArgs e);


    public class CorExceptionUnwind2EventArgs : CorThreadEventArgs
    {
    
        [CLSCompliant(false)]
        public CorExceptionUnwind2EventArgs(CorAppDomain appDomain, CorThread thread,
                                            CorDebugExceptionUnwindCallbackType eventType,
                                            int flags)
            : base (appDomain, thread)
        {
            m_eventType = eventType;
            m_flags = flags;
        }
        
        [CLSCompliant(false)]
        public CorDebugExceptionUnwindCallbackType EventType
        { 
            get 
            { 
                return m_eventType; 
            }
        }

        public int Flags
        { 
            get 
            { 
                return m_flags; 
            } 
        }

        CorDebugExceptionUnwindCallbackType m_eventType;
        int m_flags;
    }

    public delegate void CorExceptionUnwind2EventHandler (Object sender, 
                                                   CorExceptionUnwind2EventArgs e);


    public class CorException2EventArgs : CorThreadEventArgs
    {
    
        [CLSCompliant(false)]
        public CorException2EventArgs(CorAppDomain appDomain, 
                                      CorThread thread,
                                      CorFrame frame, 
                                      int offset,
                                      CorDebugExceptionCallbackType eventType,
                                      int flags)
            : base (appDomain, thread)
        {
            m_frame = frame;
            m_offset = offset;
            m_eventType = eventType;
            m_flags = flags;
        }

        public CorFrame Frame
        {
            get 
            {
                return m_frame;
            }
        }

        public int Offset
        { 
            get 
            { 
                return m_offset; 
            } 
        }

        [CLSCompliant(false)]
        public CorDebugExceptionCallbackType EventType
        { 
            get 
            { 
                return m_eventType; 
            } 
        }

        public int Flags
        { 
            get 
            { 
                return m_flags; 
            } 
        }

        CorFrame m_frame;
        int m_offset;
        CorDebugExceptionCallbackType m_eventType;
        int m_flags;
    }

    public delegate void CorException2EventHandler (Object sender, 
                                                   CorException2EventArgs e);

#if MDBG_FEATURE_INTEROP
    public class CorNativeStopEventArgs : CorProcessEventArgs
    {
        [CLSCompliant(false)]
        public CorNativeStopEventArgs(CorProcess process, 
                                      int threadId,
                                      DEBUG_EVENT debugEvent,
                                      bool isOutOfBand)
            : base (process)
        {
            m_threadId = threadId;
            m_debugEvent = debugEvent;
            m_isOutOfBand = isOutOfBand;
        }
        
        public int ThreadId
        { 
            get 
            { 
                return m_threadId; 
            } 
        }

        public bool IsOutOfBand
        { 
            get 
            { 
                return m_isOutOfBand; 
            } 
        }

        [CLSCompliant(false)]
        public DEBUG_EVENT DebugEvent
        { 
            get 
            { 
                return m_debugEvent; 
            } 
        }

        public override bool Continue
        {
            get 
            {
                // we should not be able to change default for OOB events
                return base.Continue;
            }
            set 
            {
                if(m_isOutOfBand && (value == false))
                {
                    Debug.Assert(false,"Cannot stop on OOB events");
                    throw new InvalidOperationException("Cannot stop on OOB events");
                }
                base.Continue = value;
            }
        }
		
        private int m_threadId;
        private DEBUG_EVENT m_debugEvent;
        private bool m_isOutOfBand;
    }

    public delegate void CorNativeStopEventHandler (Object sender, 
                                                    CorNativeStopEventArgs e);
#endif

    public enum ManagedCallbackType 
    {
        OnBreakpoint,
        OnStepComplete,
        OnBreak,
        OnException,
        OnEvalComplete,
        OnEvalException,
        OnCreateProcess,
        OnProcessExit,
        OnCreateThread,
        OnThreadExit,
        OnModuleLoad,
        OnModuleUnload,
        OnClassLoad,
        OnClassUnload,
        OnDebuggerError,
        OnLogMessage,
        OnLogSwitch,
        OnCreateAppDomain,
        OnAppDomainExit,
        OnAssemblyLoad,
        OnAssemblyUnload,
        OnControlCTrap,
        OnNameChange,
        OnUpdateModuleSymbols,
        OnFunctionRemapOpportunity,
        OnFunctionRemapComplete,
        OnBreakpointSetError,
        OnException2,
        OnExceptionUnwind2,
#if MDBG_FEATURE_INTEROP
        OnNativeStop,
#endif
        OnMDANotification,
        OnExceptionInCallback,
    }
    internal enum ManagedCallbackTypeCount 
    {
        Last = ManagedCallbackType.OnExceptionInCallback,
    }

} /* namespace */
