// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Microsoft Public License (MS-PL)

using System;

namespace RazorEditorParserFixed
{
    internal class DisposableAction : IDisposable
    {
        private Action _action;

        public DisposableAction(Action action)
        {
            if (action == null)
            {
                throw new ArgumentNullException("action");
            }
            _action = action;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            // If we were disposed by the finalizer it's because the user didn't use a "using" block, so don't do anything!
            if (disposing)
            {
                _action();
            }
        }
    }
}
