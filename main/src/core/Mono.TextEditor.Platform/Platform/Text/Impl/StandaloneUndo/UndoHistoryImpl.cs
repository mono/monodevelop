//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
// This file contain implementations details that are subject to change without notice.
// Use at your own risk.
//
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.Text.Operations.Standalone
{
    internal class UndoHistoryImpl : ITextUndoHistory
    {
        public event EventHandler<TextUndoRedoEventArgs> UndoRedoHappened;
        public event EventHandler<TextUndoTransactionCompletedEventArgs> UndoTransactionCompleted;

        #region Private Fields

        private UndoTransactionImpl currentTransaction;
        private Stack<ITextUndoTransaction> undoStack;
        private Stack<ITextUndoTransaction> redoStack;
        private DelegatedUndoPrimitiveImpl activeUndoOperationPrimitive;
        private TextUndoHistoryState state;
        private PropertyCollection properties;

        #endregion

        internal UndoHistoryRegistryImpl UndoHistoryRegistry;

        public UndoHistoryImpl(UndoHistoryRegistryImpl undoHistoryRegistry)
        {
            this.currentTransaction = null;
            this.UndoHistoryRegistry = undoHistoryRegistry;
            this.undoStack = new Stack<ITextUndoTransaction>();
            this.redoStack = new Stack<ITextUndoTransaction>();
            this.activeUndoOperationPrimitive = null;
            this.state = TextUndoHistoryState.Idle;
        }

        /// <summary>
        /// The full undo stack for this history. Does not include any currently opened or redo transactions.
        /// </summary>
        public IEnumerable<ITextUndoTransaction> UndoStack
        {
            get { return this.undoStack; }
        }

        /// <summary>
        /// The full redo stack for this history. Does not include any currently opened or undo transactions.
        /// </summary>
        public IEnumerable<ITextUndoTransaction> RedoStack
        {
            get { return this.redoStack; }
        }

        /// <summary>
        /// It returns most recently pushed (topmost) item of the <see cref="ITextUndoHistory.UndoStack"/> or if the stack is
        /// empty it returns null.
        /// </summary>
        public ITextUndoTransaction LastUndoTransaction
        {
            get 
            {
                if (this.undoStack.Count != 0)
                {
                    return this.undoStack.Peek();
                }

                return null;
            }
        }

        /// <summary>
        /// It returns most recently pushed (topmost) item of the <see cref="ITextUndoHistory.RedoStack"/> or if the stack is
        /// empty it returns null.
        /// </summary>
        public ITextUndoTransaction LastRedoTransaction
        {
            get 
            {
                if (this.redoStack.Count != 0)
                {
                    return this.redoStack.Peek();
                }

                return null;
            }
        }

        /// <summary>
        /// Whether a single undo is permissible (corresponds to the most recent visible undo UndoTransaction's CanUndo).        
        /// </summary>
        /// <remarks>
        /// If there are hidden transactions on top of the visible transaction, this property returns true only they are 
        /// undoable as well.
        /// </remarks>
        public bool CanUndo
        {
            get 
            {
                if (this.undoStack.Count > 0)
                {
                    return this.undoStack.Peek().CanUndo;
                }
                else
                {
                    return false;
                }
            }
        }

        /// <summary>
        /// Whether a single redo is permissible (corresponds to the most recent visible redo UndoTransaction's CanRedo).
        /// </summary>
        /// <remarks>
        /// If there are hidden transactions on top of the visible transaction, this property returns true only they are 
        /// redoable as well.
        /// </remarks>
        public bool CanRedo
        {
            get
            {
                if (this.redoStack.Count > 0)
                {
                    return this.redoStack.Peek().CanRedo;
                }
                else
                {
                    return false;
                }
            }
        }

        /// <summary>
        /// The most recent visible undo UndoTransactions's Description.
        /// </summary>
        public string UndoDescription
        {
            get 
            {
                if (this.undoStack.Count > 0)
                {
                    return this.undoStack.Peek().Description;
                }
                else
                {
                    return "Strings.HistoryCantUndo";
                }
            }
        }

        /// <summary>
        /// The most recent visible redo UndoTransaction's Description.
        /// </summary>
        public string RedoDescription
        {
            get 
            {
                if (this.undoStack.Count > 0)
                {
                    return this.redoStack.Peek().Description;
                }
                else
                {
                    return "Strings.HistoryCantRedo";
                }
            }
        }

        /// <summary>
        /// The current UndoTransaction in progress.
        /// </summary>
        public ITextUndoTransaction CurrentTransaction
        {
            get { return this.currentTransaction; }
        }

        /// <summary>
        /// 
        /// </summary>
        public TextUndoHistoryState State
        {
            get { return this.state; }
        }

        /// <summary>
        /// Creates a new transaction, nests it in the previously current transaction, and marks it current.
        /// If there is a redo stack, it gets cleared.
        /// UNDONE: should the redo-clearing happen now or when the new transaction is committed?
        /// </summary>
        /// <param name="description">A string description for the transaction.</param>
        /// <param name="isHidden">The new transaction.</param>
        /// <returns></returns>
        public ITextUndoTransaction CreateTransaction(string description)
        {
            if (String.IsNullOrEmpty(description))
            {
                throw new ArgumentNullException("description", String.Format(CultureInfo.CurrentUICulture, "Strings.ArgumentCannotBeNull", "CreateTransaction", "description"));
            }

            // If there is a pending transaction that has already been completed, we should not be permitted
            // to open a new transaction, since it cannot later be added to its parent.
            if ((this.currentTransaction != null) && (this.currentTransaction.State != UndoTransactionState.Open))
            {
                throw new InvalidOperationException("Strings.CannotCreateTransactionWhenCurrentTransactionNotOpen");
            }

            // new transactions that are visible should clear the redo stack.
            if (this.currentTransaction == null)
            {
                foreach (UndoTransactionImpl redoTransaction in this.redoStack)
                {
                    redoTransaction.Invalidate();
                }

                this.redoStack.Clear();
            }

            UndoTransactionImpl newTransaction = new UndoTransactionImpl(this, this.currentTransaction, description);

            this.currentTransaction = newTransaction;

            return this.currentTransaction;
        }

        /// <summary>
        /// Performs requested amount of undo operation and places the transactions on the redo stack.
        /// UNDONE: What if there is a currently opened transaction?
        /// </summary>
        /// <param name="count">The number of undo operations to perform. At the end of the operation, requested number of visible
        /// transactions are undone. Hence actual number of transactions undone might be more than this number if there are some 
        /// hidden transactions adjacent to (on top of or at the bottom of) the visible ones.
        /// </param>        
        /// <remarks>
        /// After the last visible transaction is undone, hidden transactions left on top the stack are undone as well until a 
        /// visible or linked transaction is encountered or stack is emptied totally.
        /// </remarks>
        public void Undo(int count)
        {
            if (count <= 0)
            {
                throw new ArgumentException(String.Format(CultureInfo.CurrentUICulture, "Strings.RedoAndUndoAcceptOnlyPositiveCounts", "Undo", count), "count");
            }

            if (!IsThereEnoughVisibleTransactions(this.undoStack, count))
            {
                throw new InvalidOperationException(String.Format(CultureInfo.CurrentUICulture, "Strings.CannotUndoMoreTransactionsThanExist", "undo", count));
            }

            TextUndoHistoryState originalState = this.state;
            this.state = TextUndoHistoryState.Undoing;
            using (new AutoEnclose(delegate { this.state = originalState; }))
            {
                while (count > 0)
                {
                    if (!this.undoStack.Peek().CanUndo)
                    {
                        throw new InvalidOperationException("Strings.CannotUndoRequestedPrimitiveFromHistoryUndo");
                    }

                    ITextUndoTransaction ut = this.undoStack.Pop();
                    ut.Undo();
                    this.redoStack.Push(ut);

                    RaiseUndoRedoHappened(this.state, ut);

                    --count;
                }
            }
        }

        /// <summary>
        /// Performs an undo operation and places the primitives on the redo stack, up until (and 
        /// including) the transaction indicated. This is called by the linked undo transaction that
        /// is aware of the linking relationship between transactions, and it does not call back into
        /// the transactions' public Undo().
        /// </summary>
        /// <param name="transaction"></param>
        public void UndoInIsolation(UndoTransactionImpl transaction)
        {
            TextUndoHistoryState originalState = this.state;
            this.state = TextUndoHistoryState.Undoing;
            using (new AutoEnclose(delegate { this.state = originalState; }))
            {

                if (this.undoStack.Contains(transaction))
                {
                    UndoTransactionImpl undone = null;
                    while (undone != transaction)
                    {
                        UndoTransactionImpl ut = this.undoStack.Pop() as UndoTransactionImpl;
                        ut.Undo();
                        this.redoStack.Push(ut);

                        RaiseUndoRedoHappened(this.state, ut); 

                        undone = ut;
                    }
                }
            }
        }

        /// <summary>
        /// Performs requested amount of redo operation and places the transactions on the undo stack.
        /// UNDONE: What if there is a currently opened transaction?
        /// </summary>
        /// <param name="count">The number of redo operations to perform. At the end of the operation, requested number of visible
        /// transactions are redone. Hence actual number of transactions redone might be more than this number if there are some 
        /// hidden transactions adjacent to (on top of or at the bottom of) the visible ones.
        /// </param>        
        /// <remarks>
        /// After the last visible transaction is redone, hidden transactions left on top the stack are redone as well until a 
        /// visible or linked transaction is encountered or stack is emptied totally.
        /// </remarks>
        public void Redo(int count)
        {
            if (count <= 0)
            {
                throw new ArgumentException(String.Format(CultureInfo.CurrentUICulture, "Strings.RedoAndUndoAcceptOnlyPositiveCounts", "Redo", count), "count");
            }

            if (!IsThereEnoughVisibleTransactions(this.redoStack, count))
            {
                throw new InvalidOperationException(String.Format(CultureInfo.CurrentUICulture, "Strings.CannotUndoMoreTransactionsThanExist", "redo", count));
            }

            TextUndoHistoryState originalState = this.state;
            this.state = TextUndoHistoryState.Redoing;
            using (new AutoEnclose(delegate { this.state = originalState; }))
            {
                while (count > 0)
                {
                    if (!this.redoStack.Peek().CanRedo)
                    {
                        throw new InvalidOperationException("Strings.CannotRedoRequestedPrimitiveFromHistoryRedo");
                    }
                    ITextUndoTransaction ut = this.redoStack.Pop();
                    ut.Do();
                    this.undoStack.Push(ut);

                    RaiseUndoRedoHappened(this.state, ut);

                    --count;
                }
            }
        }

        /// <summary>
        /// Performs a redo operation and places the primitives on the redo stack, up until (and 
        /// including) the transaction indicated. This is called by the linked undo transaction that
        /// is aware of the linking relationship between transactions, and it does not call back into
        /// the transactions' public Redo().
        /// </summary>
        /// <param name="transaction"></param>
        public void RedoInIsolation(UndoTransactionImpl transaction)
        {
            TextUndoHistoryState originalState = this.state;
            this.state = TextUndoHistoryState.Redoing;
            using (new AutoEnclose(delegate { this.state = originalState; }))
            {
                if (this.redoStack.Contains(transaction))
                {
                    UndoTransactionImpl redone = null;
                    while (redone != transaction)
                    {
                        UndoTransactionImpl ut = this.redoStack.Pop() as UndoTransactionImpl;
                        ut.Do();
                        this.undoStack.Push(ut);

                        RaiseUndoRedoHappened(this.state, ut); 

                        redone = ut;
                    }
                }
            }
        }
        
        /// <summary>
        /// This method is called from the DelegatedUndoPrimitive just as it starts a do or undo, so that this
        /// history knows to forward any new UndoableOperations to the primitive. This and its pair EndForward... only manage
        /// the state of the activeUndoOperationPrimitive.
        /// </summary>
        /// <param name="primitive">The delegated primitive to be marked active</param>
        public void ForwardToUndoOperation(DelegatedUndoPrimitiveImpl primitive)
        {
            if (this.activeUndoOperationPrimitive != null)
            {
                throw new InvalidOperationException();
            }

            this.activeUndoOperationPrimitive = primitive;
        }

        /// <summary>
        /// This method ends the lifetime of the activeUndoOperationPrimitive and should be called after ForwardToUndoOperation.
        /// </summary>
        /// <param name="primitive">The previously active delegated primitive--used for sanity check.</param>
        public void EndForwardToUndoOperation(DelegatedUndoPrimitiveImpl primitive)
        {
            if (this.activeUndoOperationPrimitive != primitive)
            {
                throw new InvalidOperationException();
            }

            this.activeUndoOperationPrimitive = null;
        }

        /// <summary>
        /// This is how the transactions alert their containing history that they have finished
        /// (likely from the Dispose() method). 
        /// </summary>
        /// <param name="transaction">This is the transaction that's finishing. It should match the history's current transaction.
        /// If it does not match, then the current transaction will be discarded and an exception will be thrown.</param>
        public void EndTransaction(ITextUndoTransaction transaction)
        {
            if (this.currentTransaction != transaction)
            {
                this.currentTransaction = null;
                throw new InvalidOperationException("Strings.EndTransactionOutOfOrder");
            }

            // only add completed transactions to their parents (or the stack)
            if (this.currentTransaction.State == UndoTransactionState.Completed)
            {
                if (this.currentTransaction.Parent == null) // stack bottomed out!
                {
                    MergeOrPushToUndoStack(this.currentTransaction);
                }
            }
            this.currentTransaction = this.currentTransaction.Parent as UndoTransactionImpl;
        }

        /// <summary>
        /// This does two different things, depending on the MergeUndoTransactionPolicys in question.
        /// It either simply pushes the current transaction to the undo stack, OR it merges it with
        /// the most recent item in the stack.
        /// </summary>
        private void MergeOrPushToUndoStack(UndoTransactionImpl transaction)
        {
            ITextUndoTransaction transactionAdded;
            TextUndoTransactionCompletionResult transactionResult;

            UndoTransactionImpl utPrevious = this.undoStack.Count > 0 ? this.undoStack.Peek() as UndoTransactionImpl : null;
            if (utPrevious != null && ProceedWithMerge(transaction, utPrevious))
            {
                // Temporarily make utPrevious non-read-only, during merge.
                utPrevious.IsReadOnly = false;
                try
                {
                    transaction.MergePolicy.PerformTransactionMerge(utPrevious, transaction);
                }
                finally
                {
                    utPrevious.IsReadOnly = true;
                }

                // utPrevious is already on the undo stack, so we don't need to add it; but report
                // it as the added transaction in the UndoTransactionCompleted event.
                transactionAdded = utPrevious;
                transactionResult = TextUndoTransactionCompletionResult.TransactionMerged;
            }
            else
            {
                this.undoStack.Push(transaction);

                transactionAdded = transaction;
                transactionResult = TextUndoTransactionCompletionResult.TransactionAdded;
            }
            RaiseUndoTransactionCompleted(transactionAdded, transactionResult);            
        }

        public bool ValidTransactionForMarkers(ITextUndoTransaction transaction)
        {
            return transaction == null                     // you can put a marker on the null transaction
                || this.currentTransaction == transaction  // you can put a marker on the currently active transaction
                || (transaction.History == this && !(transaction.State == UndoTransactionState.Invalid));
                                                           // and you can put a marker on any transaction in this history.
        }

        public static bool IsThereEnoughVisibleTransactions(Stack<ITextUndoTransaction> stack, int visibleCount)
        {
            if (visibleCount <= 0)
            {
                return true;
            }

            foreach (ITextUndoTransaction transaction in stack)
            {
                visibleCount--;

                if (visibleCount <= 0)
                {
                    return true;
                }
            }

            return false;
        }

        private bool ProceedWithMerge(UndoTransactionImpl transaction1, UndoTransactionImpl transaction2)
        {
            UndoHistoryRegistryImpl registry = UndoHistoryRegistry;

            return transaction1.MergePolicy != null
                && transaction2.MergePolicy != null
                && transaction1.MergePolicy.TestCompatiblePolicy(transaction2.MergePolicy)
                && transaction1.MergePolicy.CanMerge(transaction1, transaction2);
        }        

        private void RaiseUndoRedoHappened(TextUndoHistoryState state, ITextUndoTransaction transaction)
        {
            EventHandler<TextUndoRedoEventArgs> undoRedoHappened = UndoRedoHappened;
            if (undoRedoHappened != null)
            {
                undoRedoHappened(this, new TextUndoRedoEventArgs(state, transaction));
            }
        }

        private void RaiseUndoTransactionCompleted(ITextUndoTransaction transaction, TextUndoTransactionCompletionResult result)
        {
            EventHandler<TextUndoTransactionCompletedEventArgs> undoTransactionAdded = UndoTransactionCompleted;
            if (undoTransactionAdded != null)
            {
                undoTransactionAdded(this, new TextUndoTransactionCompletedEventArgs(transaction, result));
            }
        }

        public PropertyCollection Properties
        {
            get 
            {
                if (this.properties == null)
                {
                    this.properties = new PropertyCollection();
                }
                return this.properties;
            }
        }
    }
}

