//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
// This file contain implementations details that are subject to change without notice.
// Use at your own risk.
//
namespace Microsoft.VisualStudio.Text.Operations.Implementation
{
    using System;
    using System.Linq;
    using System.Text;
    using Microsoft.VisualStudio.Text.Editor;
    using Microsoft.VisualStudio.Text;
    using System.Diagnostics;

    [Flags]
    internal enum TextTransactionMergeDirections
    {
        Forward     = 0x0001,
        Backward    = 0x0002
    }

    /// <summary>
    /// This is the merge policy used for determining whether text's undo transactions can be merged.
    /// </summary>
    internal class TextTransactionMergePolicy : IMergeTextUndoTransactionPolicy
    {
        #region Private members
        TextTransactionMergeDirections _allowableMergeDirections;
        #endregion

        #region Constructors
        public TextTransactionMergePolicy() : this (TextTransactionMergeDirections.Forward | TextTransactionMergeDirections.Backward)
        {
        }

        public TextTransactionMergePolicy(TextTransactionMergeDirections allowableMergeDirections)
        {
            _allowableMergeDirections = allowableMergeDirections;
        }
        #endregion

        #region IMergeTextUndoTransactionPolicy Members

        public bool CanMerge(ITextUndoTransaction newTransaction, ITextUndoTransaction oldTransaction)
        {
            // Validate
            if (newTransaction == null)
            {
                throw new ArgumentNullException("newTransaction");
            }

            if (oldTransaction == null)
            {
                throw new ArgumentNullException("oldTransaction");
            }

            TextTransactionMergePolicy oldPolicy = oldTransaction.MergePolicy as TextTransactionMergePolicy;
            TextTransactionMergePolicy newPolicy = newTransaction.MergePolicy as TextTransactionMergePolicy;
            if (oldPolicy == null || newPolicy == null)
            {
                throw new InvalidOperationException("The MergePolicy for both transactions should be a TextTransactionMergePolicy.");
            }

            // Make sure the merge policy directions permit merging these two transactions.
            if ((oldPolicy._allowableMergeDirections & TextTransactionMergeDirections.Forward) == 0 ||
                (newPolicy._allowableMergeDirections & TextTransactionMergeDirections.Backward) == 0)
            {
                return false;
            }

            // Only merge text transactions that have the same description
            if (newTransaction.Description != oldTransaction.Description)
            {
                return false;
            }

            // If one of the transactions is empty, than it is safe to merge
            if ((newTransaction.UndoPrimitives.Count == 0) || (oldTransaction.UndoPrimitives.Count == 0))
            {
                return true;
            }

            // Make sure that we only merge consecutive edits
            ITextUndoPrimitive newerBeforeTextChangePrimitive = newTransaction.UndoPrimitives[0];
            ITextUndoPrimitive olderAfterTextChangePrimitive = oldTransaction.UndoPrimitives[oldTransaction.UndoPrimitives.Count - 1];

            return newerBeforeTextChangePrimitive.CanMerge(olderAfterTextChangePrimitive);
        }

        public void PerformTransactionMerge(ITextUndoTransaction existingTransaction, ITextUndoTransaction newTransaction)
        {
            if (existingTransaction == null)
                throw new ArgumentNullException("existingTransaction");
            if (newTransaction == null)
                throw new ArgumentNullException("newTransaction");

            // Remove trailing AfterTextBufferChangeUndoPrimitive from previous transaction and skip copying
            // initial BeforeTextBufferChangeUndoPrimitive from newTransaction, as they are unnecessary.
            int copyStartIndex = 0;
            int existingCount = existingTransaction.UndoPrimitives.Count;
            int newCount = newTransaction.UndoPrimitives.Count;
            if (existingCount > 0 && 
                newCount > 0 && 
                existingTransaction.UndoPrimitives[existingCount - 1] is AfterTextBufferChangeUndoPrimitive &&
                newTransaction.UndoPrimitives[0] is BeforeTextBufferChangeUndoPrimitive)
            {
                existingTransaction.UndoPrimitives.RemoveAt(existingCount - 1);
                copyStartIndex = 1;
            }
            else
            {
                // Unless undo is disabled (in which case both transactions will be empty), this is unexpected.
                Debug.Assert(existingCount == 0 && newCount == 0,
                    "Expected previous transaction to end with AfterTextBufferChangeUndoPrimitive and "
                    + "new transaction to start with BeforeTextBufferChangeUndoPrimitive");
            }

            // Copy items from newTransaction into existingTransaction.
            for (int i = copyStartIndex; i < newTransaction.UndoPrimitives.Count; i++)
            {
                existingTransaction.UndoPrimitives.Add(newTransaction.UndoPrimitives[i]);
            }
        }

        public bool TestCompatiblePolicy(IMergeTextUndoTransactionPolicy other)
        {
            if (other == null)
            {
                throw new ArgumentNullException("other");
            }

            // Only merge transaction if they are both a text transaction
            return this.GetType() == other.GetType();
        }

        #endregion
    }
}
