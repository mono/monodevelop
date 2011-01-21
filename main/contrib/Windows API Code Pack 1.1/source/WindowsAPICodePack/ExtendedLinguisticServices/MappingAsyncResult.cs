// Copyright (c) Microsoft Corporation.  All rights reserved.

using System;
using System.Threading;

namespace Microsoft.WindowsAPICodePack.ExtendedLinguisticServices
{

    /// <summary>
    /// <see cref="System.IAsyncResult">IAsyncResult</see> implementation for use with asynchronous calls to ELS.
    /// </summary>
    public class MappingAsyncResult : IAsyncResult, IDisposable
    {
        private object _callerData;
        private MappingPropertyBag _bag;
        private MappingResultState _resultState;
        private ManualResetEvent _waitHandle;
        private AsyncCallback _asyncCallback;

        internal MappingAsyncResult(
            object callerData,
            AsyncCallback asyncCallback)
        {
            _callerData = callerData;
            _asyncCallback = asyncCallback;
            _waitHandle = new ManualResetEvent(false);
        }

        internal AsyncCallback AsyncCallback
        {
            get
            {
                return _asyncCallback;
            }
        }

        /// <summary>
        /// Queries whether the operation completed successfully.
        /// </summary>
        public bool Succeeded
        {
            get
            {
                return _bag != null && _resultState.HResult == 0;
            }
        }

        /// <summary>
        /// Gets the resulting <see cref="MappingPropertyBag">MappingPropertyBag</see> (if it exists).
        /// </summary>
        public MappingPropertyBag PropertyBag
        {
            get
            {
                return _bag;
            }
        }

        /// <summary>
        /// Returns the current result state associated with this operation.
        /// </summary>
        public MappingResultState ResultState
        {
            get
            {
                return _resultState;
            }
        }

        /// <summary>
        /// Returns the caller data associated with this operation.
        /// </summary>
        public object CallerData
        {
            get
            {
                return _callerData;
            }
        }

        internal void SetResult(MappingPropertyBag bag, MappingResultState resultState)
        {
            _resultState = resultState;
            _bag = bag;
        }

        #region IAsyncResult Members

        // returns MappingResultState
        /// <summary>
        /// Returns the result state.
        /// </summary>
        public object AsyncState
        {
            get
            {
                return ResultState;
            }
        }

        /// <summary>
        /// Gets the WaitHandle which will be notified when
        /// the opration completes (successfully or not).
        /// </summary>
        public WaitHandle AsyncWaitHandle
        {
            get
            {
                return _waitHandle;
            }
        }
                
        /// <summary>
        /// From MSDN:
        /// Most implementers of the IAsyncResult interface
        /// will not use this property and should return false.
        /// </summary>
        public bool CompletedSynchronously
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Queries whether the operation has completed.
        /// </summary>
        public bool IsCompleted
        {
            get
            {
                Thread.MemoryBarrier();
                return AsyncWaitHandle.WaitOne(0, false);
            }
        }

        #endregion

        #region IDisposable Members

        /// <summary>
        /// Dispose the MappingAsyncresult
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Dispose the MappingAsyncresult
        /// </summary>
        protected virtual void Dispose(bool disposed)
        {
            if (disposed)
            {
                _waitHandle.Close();
            }
        }

        #endregion
    }

}
