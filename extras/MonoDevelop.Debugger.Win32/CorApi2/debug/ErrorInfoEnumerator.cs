//---------------------------------------------------------------------
//  This file is part of the CLR Managed Debugger (mdbg) Sample.
// 
//  Copyright (C) Microsoft Corporation.  All rights reserved.
//---------------------------------------------------------------------
using System;
using System.Collections;

using Microsoft.Samples.Debugging.CorDebug.NativeApi;

namespace Microsoft.Samples.Debugging.CorDebug
{
    /** 
     * Exposes an enumerator for ErrorInfo objects. 
     *
     * This is horribly broken at this point, as ErrorInfo isn't implemented yet.
     */
    internal class CorErrorInfoEnumerator : IEnumerable, IEnumerator, ICloneable
    {
        private ICorDebugErrorInfoEnum m_enum;

#if CORAPI_SKIP
        private ErrorInfo m_einfo;
#else
        private Object m_einfo;
#endif

        internal CorErrorInfoEnumerator (ICorDebugErrorInfoEnum erroInfoEnumerator)
        {
            m_enum = erroInfoEnumerator;
        }

        //
        // ICloneable interface
        //
        public Object Clone ()
        {
            ICorDebugEnum clone = null;
            m_enum.Clone (out clone);
            return new CorErrorInfoEnumerator ((ICorDebugErrorInfoEnum)clone);
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
#if CORAPI_SKIP
            ICorDebugErrorInfo[] a = new ICorDebugErrorInfo[1];
            uint c = 0;
            int r = m_enum.Next ((uint) a.Length, a, out c);
            if (r==0 && c==1) // S_OK && we got 1 new element
                m_einfo = new ErrorInfo (a[0]);
            else
                m_einfo = null;
            return m_einfo != null;
#else
            return false;
#endif
        }

        public void Reset ()
        {
            m_enum.Reset ();
            m_einfo = null;
        }

        public Object Current
        {
            get 
            {
                return m_einfo;
            }
        }
    } /* class ErrorInfoEnumerator */
} /* namespace */
