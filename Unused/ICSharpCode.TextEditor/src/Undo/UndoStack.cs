// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.Diagnostics;
using System.Collections;

namespace MonoDevelop.TextEditor.Undo
{
	/// <summary>
	/// This class implements an undo stack
	/// </summary>
	public class UndoStack
	{
		Stack undostack = new Stack();
		Stack redostack = new Stack();
		
		/// <summary>
		/// </summary>
		public event EventHandler ActionUndone;
		/// <summary>
		/// </summary>
		public event EventHandler ActionRedone;
		
		/// <summary>
		/// </summary>
		public bool AcceptChanges = true;
		
		/// <summary>
		/// This property is EXCLUSIVELY for the UndoQueue class, don't USE it
		/// </summary>
		internal Stack _UndoStack {
			get {
				return undostack;
			}
		}
		
		/// <summary>
		/// </summary>
		public bool CanUndo {
			get {
				return undostack.Count > 0;
			}
		}
		
		/// <summary>
		/// </summary>
		public bool CanRedo {
			get {
				return redostack.Count > 0;
			}
		}
		
		public int UndoCount { get { return undostack.Count; } }
		
		/// <summary>
		/// You call this method to pool the last x operations from the undo stack
		/// to make 1 operation from it.
		/// </summary>
		public void UndoLast(int x)
		{
			if (x > 0)
				undostack.Push(new UndoQueue(this, x));
		}
		
		/// <summary>
		/// Call this method to undo the last operation on the stack
		/// </summary>
		public void Undo()
		{
			if (undostack.Count > 0) {
				IUndoableOperation uedit = (IUndoableOperation)undostack.Pop();
				redostack.Push(uedit);
				uedit.Undo();
				OnActionUndone();
			}
		}
		
		/// <summary>
		/// Call this method to redo the last undone operation
		/// </summary>
		public void Redo()
		{
			if (redostack.Count > 0) {
				IUndoableOperation uedit = (IUndoableOperation)redostack.Pop();
				undostack.Push(uedit);
				uedit.Redo();
				OnActionRedone();
			}
		}
		
		/// <summary>
		/// Call this method to push an UndoableOperation on the undostack, the redostack
		/// will be cleared, if you use this method.
		/// </summary>
		public void Push(IUndoableOperation operation) 
		{
			if (operation == null) {
				throw new ArgumentNullException("UndoStack.Push(UndoableOperation operation) : operation can't be null");
			}
			
			if (AcceptChanges) {
				undostack.Push(operation);
				ClearRedoStack();
			}
		}
		
		/// <summary>
		/// Call this method, if you want to clear the redo stack
		/// </summary>
		public void ClearRedoStack()
		{
			redostack.Clear();
		}
		
		/// <summary>
		/// </summary>
		public void ClearAll()
		{
			undostack.Clear();
			redostack.Clear();
		}
		
		/// <summary>
		/// </summary>
		protected void OnActionUndone()
		{
			if (ActionUndone != null) {
				ActionUndone(null, null);
			}
		}
		
		/// <summary>
		/// </summary>
		protected void OnActionRedone()
		{
			if (ActionRedone != null) {
				ActionRedone(null, null);
			}
		}
	}
}
