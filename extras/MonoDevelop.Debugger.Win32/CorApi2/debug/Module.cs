//---------------------------------------------------------------------
//  This file is part of the CLR Managed Debugger (mdbg) Sample.
// 
//  Copyright (C) Microsoft Corporation.  All rights reserved.
//---------------------------------------------------------------------
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

using Microsoft.Samples.Debugging.CorDebug.NativeApi;
using Microsoft.Samples.Debugging.CorMetadata.NativeApi;

namespace Microsoft.Samples.Debugging.CorDebug
{
    public sealed class CorMDA : WrapperBase
    {
        private ICorDebugMDA m_mda;
        internal CorMDA(ICorDebugMDA mda)
            :base(mda)
        {
            m_mda = mda;
        }

        public CorDebugMDAFlags Flags
        {
            get
            {
                CorDebugMDAFlags flags;
                m_mda.GetFlags(out flags);
                return flags;
            }
        }

        string m_cachedName = null;
        public string Name        
        {
            get 
            {
                // This is thread safe because even in a race, the loser will just do extra work.
                // but no harm done.
                if (m_cachedName == null)
                {
                    uint len = 0;
                    m_mda.GetName(0, out len, null);
                                    
                    char[] name = new char[len];
                    uint fetched = 0;

                    m_mda.GetName ((uint) name.Length, out fetched, name);
                    // ``fetched'' includes terminating null; String doesn't handle null, so we "forget" it.
                    m_cachedName = new String (name, 0, (int) (fetched-1));
                }
                return m_cachedName;               
            } // end get
        }

        public string XML
        {
            get 
            {
                uint len = 0;
                m_mda.GetXML(0, out len, null);
                                
                char[] name = new char[len];
                uint fetched = 0;

                m_mda.GetXML ((uint) name.Length, out fetched, name);
                // ``fetched'' includes terminating null; String doesn't handle null, so we "forget" it.
                return new String (name, 0, (int) (fetched-1));
            }            
        }

        public string Description
        {
            get 
            {
                uint len = 0;
                m_mda.GetDescription(0, out len, null);
                                
                char[] name = new char[len];
                uint fetched = 0;

                m_mda.GetDescription((uint) name.Length, out fetched, name);
                // ``fetched'' includes terminating null; String doesn't handle null, so we "forget" it.
                return new String (name, 0, (int) (fetched-1));
            }            
        }

        public int OsTid
        {
            get
            {
                uint tid;
                m_mda.GetOSThreadId(out tid);
                return (int) tid;
            }            
        }
    } // end CorMDA

    public sealed class CorModule : WrapperBase
    {
        private ICorDebugModule m_module;

		public System.Diagnostics.SymbolStore.ISymbolReader SymbolReader { get; set; }

        internal CorModule (ICorDebugModule managedModule)
            :base(managedModule)
        {
            m_module = managedModule;
        }

#if CORAPI_EXPOSE_RAW_INTERFACES
        [CLSCompliant(false)]
        public ICorDebugModule Raw
        {
            get 
            { 
                return m_module;
            }
        }
#endif

        /** The process this module is in. */
        public CorProcess Process
        {
            get
            {
                ICorDebugProcess proc = null;
                m_module.GetProcess (out proc);
                return CorProcess.GetCorProcess (proc);
            }
        }

        /** The base address of this module */
        public long BaseAddress
        {
            get
            {
                ulong addr = 0;
                m_module.GetBaseAddress (out addr);
                return (long) addr;
            }
        }

        /** The assembly this module is in. */
        public CorAssembly Assembly
        {
            get
            {
                ICorDebugAssembly a = null;
                m_module.GetAssembly (out a);
                return new CorAssembly (a);
            }
        }

        /** The name of the module. */
        public String Name
        {
            get
            {
                // <strip>@TODO: is this big enough?</strip>
                char[] name = new Char[300];
                uint fetched = 0;
                m_module.GetName ((uint) name.Length, out fetched, name);
                // ``fetched'' includes terminating null; String doesn't handle null,
                // so we "forget" it.
                return new String (name, 0, (int) (fetched-1));
            }
        }

        /** These flags set things like TrackJitInfo, PreventOptimization, IgnorePDBs, and EnableEnC
        * The setter here will sometimes not successfully set the EnableEnc bit (0x4) when asked to, and
        * we have hidden this detail from users of this layer.
        * If you are interested in handling this case, simply use the getter to check what the new value is after setting it.
        * If they don't match and no exception was thrown, you may assume that's what happened
        */
        public CorDebugJITCompilerFlags JITCompilerFlags
        {
            get
            {
                uint retval = 0;
                (m_module as ICorDebugModule2).GetJITCompilerFlags(out retval);
                return (CorDebugJITCompilerFlags)retval;
            }
            set
            {
                // ICorDebugModule2.SetJITCompilerFlags can return successful HRESULTS other than S_OK.
                // Since we have asked the COMInterop layer to preservesig, we need to marshal any failing HRESULTS.
                Marshal.ThrowExceptionForHR((m_module as ICorDebugModule2).SetJITCompilerFlags((uint)value));
            }
        }

        /** This is Debugging support for Type Forwarding */
        public CorAssembly ResolveAssembly(int tkAssemblyRef)
        {
            ICorDebugAssembly assm = null;
            (m_module as ICorDebugModule2).ResolveAssembly((uint)tkAssemblyRef, out assm);
            return new CorAssembly(assm);
        }

        /** 
         * should the jitter preserve debugging information for methods 
         * in this module?
         */
        public void EnableJitDebugging (bool trackJitInfo, bool allowJitOpts)
        {
            m_module.EnableJITDebugging (trackJitInfo ? 1 : 0, 
                                      allowJitOpts ? 1 : 0);
        }

        /** Are ClassLoad callbacks called for this module? */
        public void EnableClassLoadCallbacks (bool value)
        {
            m_module.EnableClassLoadCallbacks (value ? 1 : 0);
        }

        /** Get the function from the metadata info. */
        public CorFunction GetFunctionFromToken (int functionToken)
        {
            ICorDebugFunction corFunction;
            m_module.GetFunctionFromToken((uint)functionToken,out corFunction);
            return (corFunction==null?null:new CorFunction(corFunction));
        }

#if CORAPI_SKIP
        /** Get the function from the relative address */
        public CorFunction GetFunctionFromRVA (long address);
#endif

        /** get the class from metadata info. */
        public CorClass GetClassFromToken (int classToken)
        {
            ICorDebugClass c = null;
            m_module.GetClassFromToken ((uint)classToken, out c);
            return new CorClass (c);
        }

        /** 
         * create a breakpoint which is triggered when code in the module
         * is executed.
         */
        public CorModuleBreakpoint CreateBreakpoint ()
        {
            ICorDebugModuleBreakpoint mbr = null;
            m_module.CreateBreakpoint (out mbr);
            return new CorModuleBreakpoint (mbr);
        }

#if CORAPI_SKIP
        /** Edit & continue support */
        public EditAndContinueSnapshot GetEditAndContinueSnapshot ();

        /** ??? */
#endif

        public object GetMetaDataInterface (Guid interfaceGuid)
        {
            IMetadataImport obj;
            m_module.GetMetaDataInterface(ref interfaceGuid,out obj);
            return obj;
        }
            

        /** Get the token for the module table entry of this object. */
        public int Token
        {
            get
            {
                uint t = 0;
                m_module.GetToken (out t);
                return (int) t;
            }
        }

        /** is this a dynamic module? */
        public bool IsDynamic
        {
            get 
            {
                int b = 0;
                m_module.IsDynamic (out b);
                return !(b==0);
            }
        }

		/** is this an InMemory module? */
		public bool IsInMemory
		{
			get {
				int b = 0;
				m_module.IsInMemory (out b);
				return !(b==0);
			}
		}


        /** get the value object for the given global variable. */
        public CorValue GetGlobalVariableValue (int fieldToken)
        {
            ICorDebugValue v = null;
            m_module.GetGlobalVariableValue ((uint) fieldToken, out v);
            return new CorValue (v);
        }


        /** The size (in bytes) of the module. */
        public int Size
        {
            get
            {
                uint s = 0;
                m_module.GetSize (out s);
                return (int) s;
            }
        }

        public void ApplyChanges(byte[] deltaMetadata,byte[] deltaIL)
        {
            (m_module as ICorDebugModule2).ApplyChanges((uint)deltaMetadata.Length,deltaMetadata,(uint)deltaIL.Length,deltaIL);
        }

        public void SetJmcStatus(bool isJustMyCOde,int[] tokens)
        {
            Debug.Assert(tokens==null); // <strip>@TODO not yet implemented</strip>
            uint i=0;
            (m_module as ICorDebugModule2).SetJMCStatus(isJustMyCOde?1:0,0,ref i);
        }
    } /* class Module */
} /* namespace */
