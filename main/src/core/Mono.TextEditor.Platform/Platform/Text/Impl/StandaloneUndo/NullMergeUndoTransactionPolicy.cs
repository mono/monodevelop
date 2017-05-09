//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
// This file contain implementations details that are subject to change without notice.
// Use at your own risk.
//
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
