// ****************************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// ****************************************************************************

using System;

namespace Microsoft.VisualStudio.Text.Operations.Standalone
{
    /// <summary>
    /// Represents an empty <see cref="IMergeTextUndoTransactionPolicy"/> implementation, which disallows merging between transactions.
    /// </summary>
    public sealed class NullMergeUndoTransactionPolicy : IMergeTextUndoTransactionPolicy
    {
        #region Private Fields

        private static NullMergeUndoTransactionPolicy instance;

        #endregion

        #region Private Constructor

        private NullMergeUndoTransactionPolicy() { }

        #endregion

        /// <summary>
        /// Gets the <see cref="NullMergeUndoTransactionPolicy"/> object.
        /// </summary>
        public static IMergeTextUndoTransactionPolicy Instance
        {
            get 
            {
                if (NullMergeUndoTransactionPolicy.instance == null)
                {
                    NullMergeUndoTransactionPolicy.instance = new NullMergeUndoTransactionPolicy();
                }

                return instance;
            }
        }

        public bool TestCompatiblePolicy(IMergeTextUndoTransactionPolicy other)
        {
            return false;
        }

        public bool CanMerge(ITextUndoTransaction newerTransaction, ITextUndoTransaction olderTransaction)
        {
            return false;
        }

        public void PerformTransactionMerge(ITextUndoTransaction existingTransaction, ITextUndoTransaction newTransaction)
        {
            throw new InvalidOperationException("Strings.NullMergePolicyCannotMerge");
        }
    }
}
