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

namespace Microsoft.VisualStudio.Text.Operations.Standalone
{
    internal class UndoTransactionImpl : ITextUndoTransaction
    {
        #region Private Fields

        private readonly UndoHistoryImpl history;
        private readonly UndoTransactionImpl parent;

        private string description;
        private UndoTransactionState state;
        private List<ITextUndoPrimitive> primitives;
        private IMergeTextUndoTransactionPolicy mergePolicy;

        #endregion

        public UndoTransactionImpl(ITextUndoHistory history, ITextUndoTransaction parent, string description)
        {
            if (history == null)
            {
                throw new ArgumentNullException("history", String.Format(CultureInfo.CurrentUICulture, "Strings.ArgumentCannotBeNull", "UndoTransactionImpl", "history"));
            }

            if (String.IsNullOrEmpty(description))
            {
                throw new ArgumentNullException("description", String.Format(CultureInfo.CurrentUICulture, "Strings.ArgumentCannotBeNull", "UndoTransactionImpl", "description"));
            }

            this.history = history as UndoHistoryImpl;

            if (this.history == null)
            {
                throw new ArgumentException("Strings.InvalidHistoryInTransaction");
            }

            this.parent = parent as UndoTransactionImpl;

            if (this.parent == null && parent != null)
            {
                throw new ArgumentException("Strings.InvalidParentInTransaction");
            }

            this.description = description;

            this.state = UndoTransactionState.Open;
            this.primitives = new List<ITextUndoPrimitive>();
            this.mergePolicy = NullMergeUndoTransactionPolicy.Instance;
            this.IsReadOnly = true;
        }

        /// <summary>
        /// This is how you turn transaction into "Invalid" state. Use it to indicate that this transaction is retired forever,
        /// such as when clearing transactions from the redo stack.
        /// </summary>
        internal void Invalidate()
        {
            this.state = UndoTransactionState.Invalid;
        }

        internal bool IsInvalid
        {
            get { return this.state == UndoTransactionState.Invalid; }
        }

        /// <summary>
        /// Used by UndoHistoryImpl.cs to allow UndoPrimitives to be modified during merging.
        /// </summary>
        internal bool IsReadOnly { get; set; }

        /// <summary>
        /// Description is the [localized] string that describes the transaction to a user.
        /// </summary>
        public string Description
        {
            get { return this.description; }
            set { this.description = value; }
        }

        /// <summary>
        /// State is the UndoTransactionState for the UndoTransaction, as described in that type.
        /// </summary>
        public UndoTransactionState State
        {
            get { return this.state; }
        }

        /// <summary>
        /// History is a reference to the UndoHistory that contains this transaction.
        /// </summary>
        public ITextUndoHistory History
        {
            get { return this.history; }
        }

        /// <summary>
        /// UndoPrimitives allows access to the list of primitives in this transaction container, but should only be called
        /// after the transaction has been completed. 
        /// </summary>
        public IList<ITextUndoPrimitive> UndoPrimitives
        {
            get 
            {
                if (this.IsReadOnly)
                    return this.primitives.AsReadOnly();
                else
                    return this.primitives;
            }
        }

        /// <summary>
        /// Complete marks the transaction finished and eligible for Undo.
        /// </summary>
        public void Complete()
        {
            if (this.State != UndoTransactionState.Open)
            {
                throw new InvalidOperationException("Strings.CompleteCalledOnTransationThatIsNotOpened");
            }

            this.state = UndoTransactionState.Completed;
            
            // now we need to pump these primitives into the parent, if the parent exists.
            FlattenPrimitivesToParent();
        }

        /// <summary>
        /// This is called by the transaction when it is complete. It results in the parent getting
        /// all of this transaction's undo history, so that transactions are not really recursive (they
        /// exist for rollback).
        /// </summary>
        public void FlattenPrimitivesToParent()
        {
            if (this.parent != null)
            {
                // first, copy up each primitive. 
                this.parent.CopyPrimitivesFrom(this);

                // once all the primitives are in the parent, just clear them so
                // no one has a chance to tweak them here, or do/undo us.
                this.primitives.Clear();
            }
        }

        /// <summary>
        /// Copies all of the primitives from the given transaction, and appends them to the UndoPrimitives list.
        /// </summary>
        /// <param name="transaction">The UndoTransactionImpl to copy from.</param>
        public void CopyPrimitivesFrom(UndoTransactionImpl transaction)
        {
            foreach (ITextUndoPrimitive p in transaction.UndoPrimitives)
            {
                this.AddUndo(p);
            }
        }

        /// <summary>
        /// Cancel marks an Open transaction Canceled, and Undoes and clears any primitives that have been added.
        /// </summary>
        public void Cancel()
        {
            if (this.State != UndoTransactionState.Open)
            {
                throw new InvalidOperationException("Strings.CancelCalledOnTransationThatIsNotOpened");
            }

            for (int i = primitives.Count - 1; i >= 0; --i)
            {
                primitives[i].Undo();
            }

            this.primitives.Clear();
            this.state = UndoTransactionState.Canceled;
        }

        /// <summary>
        /// AddUndo adds a new primitive to the end of the list when the transaction is Open.
        /// </summary>
        /// <param name="undo"></param>
        public void AddUndo(ITextUndoPrimitive undo)
        {
            if (State != UndoTransactionState.Open)
            {
                throw new InvalidOperationException("Strings.AddUndoCalledOnTransationThatIsNotOpened");
            }

            this.primitives.Add(undo);
            undo.Parent = this;

            MergeMostRecentUndoPrimitive();
        }

        /// <summary>
        /// This is called by AddUndo, so that primitives are always in a fully merged state as we go.
        /// </summary>
        protected void MergeMostRecentUndoPrimitive()
        {
            // no merging unless there are at least two items
            if (primitives.Count < 2)
            {
                return;
            }

            ITextUndoPrimitive top = primitives[primitives.Count - 1];

            ITextUndoPrimitive victim = null;
            int victimIndex = -1;

            for (int i = primitives.Count - 2; i >= 0; --i)
            {
                if (top.GetType() == primitives[i].GetType() && top.CanMerge(primitives[i]))
                {
                    victim = primitives[i];
                    victimIndex = i;
                    break;
                }
            }

            if (victim != null)
            {
                ITextUndoPrimitive newPrimitive = top.Merge(victim);
                primitives.RemoveRange(primitives.Count - 1, 1);
                primitives.RemoveRange(victimIndex, 1);
                primitives.Add(newPrimitive);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public ITextUndoTransaction Parent
        {
            get { return parent; }
        }

        /// <summary>
        /// This is true iff every contained primitive is CanRedo and we are in an Undone state.
        /// </summary>
        public bool CanRedo
        {
            get
            {
                if (this.state == UndoTransactionState.Invalid)
                {
                    return true;
                }

                if (this.State != UndoTransactionState.Undone)
                {
                    return false;
                }

                foreach (ITextUndoPrimitive primitive in UndoPrimitives)
                {
                    if (!primitive.CanRedo)
                    {
                        return false;
                    }
                }

                return true;                
            }
        }

        /// <summary>
        /// This is true iff every contained primitive is CanUndo and we are in a Completed state.
        /// </summary>
        public bool CanUndo
        {
            get 
            {
                if (this.state == UndoTransactionState.Invalid)
                {
                    return true;
                }

                if (this.State != UndoTransactionState.Completed)
                {
                    return false;
                }

                foreach (ITextUndoPrimitive primitive in UndoPrimitives)
                {
                    if (!primitive.CanUndo)
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public void Do()
        {
            if (this.state == UndoTransactionState.Invalid)
            {
                return;
            }

            if (!CanRedo)
            {
                throw new InvalidOperationException("Strings.DoCalledButCanRedoFalse");
            }

            this.state = UndoTransactionState.Redoing;

            for (int i = 0; i < primitives.Count; ++i)
            {
                primitives[i].Do();
            }

            this.state = UndoTransactionState.Completed;
        }

        /// <summary>
        /// This defers to the linked transaction if there is one.
        /// </summary>
        public void Undo()
        {
            if (this.state == UndoTransactionState.Invalid)
            {
                return;
            }

            if (!CanUndo)
            {
                throw new InvalidOperationException("Strings.UndoCalledButCanUndoFalse");
            }

            this.state = UndoTransactionState.Undoing;

            for (int i = primitives.Count - 1; i >= 0; --i)
            {
                primitives[i].Undo();
            }

            this.state = UndoTransactionState.Undone;
        }

        /// <summary>
        /// 
        /// </summary>
        public IMergeTextUndoTransactionPolicy MergePolicy
        {
            get { return this.mergePolicy; }
            set 
            {
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }

                this.mergePolicy = value; 
            }
        }

        /// <summary>
        /// Closes a transaction and disposes it.
        /// </summary>
        public void Dispose()
        {
            GC.SuppressFinalize(this);
            switch (this.State)
            {
                case UndoTransactionState.Open:
                    Cancel();
                    break;

                case UndoTransactionState.Canceled:
                case UndoTransactionState.Completed:
                    break;

                case UndoTransactionState.Redoing:
                case UndoTransactionState.Undoing:
                case UndoTransactionState.Undone:
                    throw new InvalidOperationException("Strings.ClosingAnOpenTransactionThatAppearsToBeUndoneOrUndoing");
            }
            history.EndTransaction(this);
        }
    }
}
