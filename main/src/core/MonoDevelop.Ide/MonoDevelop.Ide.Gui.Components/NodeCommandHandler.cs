//
// NodeCommandHandler.cs
//
// Author:
//   Lluis Sanchez Gual
//
// Copyright (C) 2005 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using MonoDevelop.Ide.Commands;
using MonoDevelop.Components.Commands;

namespace MonoDevelop.Ide.Gui.Components
{
	[MultiSelectionNodeHandler]
	[NodeCommandHandler.TransactedNodeHandler]
	public class NodeCommandHandler: ICommandRouter
	{
		ITreeNavigator[] currentNodes;
		ExtensibleTreeView tree;
		object nextTarget;
		CanDeleteFlags canDeleteFlags;

		[Flags]
		enum CanDeleteFlags {
			NotChecked = 0,
			Checked = 1,
			Single = 2,
			Multiple = 4
		}
		
		internal void Initialize (ExtensibleTreeView tree)
		{
			this.tree = tree;
		}
		
		internal void SetCurrentNode (ITreeNavigator currentNode)
		{
			this.currentNodes = new ITreeNavigator [] { currentNode };
		}
		
		internal void SetCurrentNodes (ITreeNavigator[] currentNodes)
		{
			this.currentNodes = currentNodes;
		}
		
		internal void SetNextTarget (object nextTarget)
		{
			this.nextTarget = nextTarget;
		}
		
		object ICommandRouter.GetNextCommandTarget ()
		{
			return nextTarget;
		}
		
		internal protected ITreeNavigator[] CurrentNodes {
			get { return currentNodes; }
		}
		
		protected ITreeNavigator CurrentNode {
			get { return currentNodes [0]; }
		}
		
		protected ExtensibleTreeView Tree {
			get { return tree; }
		}
		
		public virtual void OnRenameStarting (ref int selectionStart, ref int selectionLength)
		{
		}

		public virtual void RenameItem (string newName)
		{
		}
		
		public virtual void ActivateItem ()
		{
		}
		
		public virtual void ActivateMultipleItems ()
		{
			if (currentNodes.Length == 1)
				ActivateItem ();
			else {
				ITreeNavigator[] nodes = currentNodes;
				try {
					currentNodes = new ITreeNavigator [1];
					foreach (ITreeNavigator nod in nodes) {
						currentNodes [0] = nod;
						ActivateItem ();
					}
				} finally {
					currentNodes = nodes;
				}
			}
		}
		
		public virtual void OnItemSelected ()
		{
		}

		internal protected virtual bool MultipleSelectedNodes {
			get { return CurrentNodes.Length > 1; }
		}
		
		[CommandUpdateHandler (EditCommands.Delete)]
		internal void CanDeleteCurrentItem (CommandInfo info)
		{
			info.Bypass = !CanDeleteMultipleItems ();
		}
		
		[CommandHandler (EditCommands.Delete)]
		[AllowMultiSelection]
		internal void DeleteCurrentItem ()
		{
			DeleteMultipleItems ();
		}

		bool CheckCanDeleteFlags (CanDeleteFlags flag)
		{
			// if DeleteItem or DeleteMultipleItems, we can assume that the delete
			// operation is supported. If it is not supported for a specific context,
			// then the CanDelete* method will be overriden, in which case this
			// CheckCanDeleteFlags won't be used.
			
			if (canDeleteFlags == CanDeleteFlags.NotChecked) {
				canDeleteFlags |= CanDeleteFlags.Checked;
				if (GetType().GetMethod ("DeleteItem").DeclaringType != typeof(NodeCommandHandler))
					canDeleteFlags |= CanDeleteFlags.Single;
				if (GetType().GetMethod ("DeleteMultipleItems").DeclaringType != typeof(NodeCommandHandler) &&
				    GetType().GetMethod ("CanDeleteItem").DeclaringType == typeof(NodeCommandHandler))
					canDeleteFlags |= CanDeleteFlags.Multiple;
			}
			return (canDeleteFlags & flag) != 0;
		}
		
		public virtual bool CanDeleteItem ()
		{
			return CheckCanDeleteFlags (CanDeleteFlags.Single);
		}
		
		public virtual bool CanDeleteMultipleItems ()
		{
			if (CheckCanDeleteFlags (CanDeleteFlags.Multiple))
				return true;
			
			if (currentNodes.Length == 1)
				return CanDeleteItem ();
			else {
				ITreeNavigator[] nodes = currentNodes;
				try {
					currentNodes = new ITreeNavigator [1];
					foreach (ITreeNavigator nod in nodes) {
						currentNodes [0] = nod;
						if (!CanDeleteItem ())
							return false;
					}
				} finally {
					currentNodes = nodes;
				}
				return true;
			}
		}
		
		public virtual void DeleteItem ()
		{
		}
		
		public virtual void DeleteMultipleItems ()
		{
			if (currentNodes.Length == 1)
				DeleteItem ();
			else {
				ITreeNavigator[] nodes = currentNodes;
				try {
					currentNodes = new ITreeNavigator [1];
					foreach (ITreeNavigator nod in nodes) {
						currentNodes [0] = nod;
						DeleteItem ();
					}
				} finally {
					currentNodes = nodes;
				}
			}
		}
		
		public virtual void RefreshItem ()
		{
		}
		
		public virtual void RefreshMultipleItems ()
		{
			if (currentNodes.Length == 1)
				RefreshItem ();
			else {
				ITreeNavigator[] nodes = currentNodes;
				try {
					currentNodes = new ITreeNavigator [1];
					foreach (ITreeNavigator nod in nodes) {
						currentNodes [0] = nod;
						RefreshItem ();
					}
				} finally {
					currentNodes = nodes;
				}
			}
		}
		
		public virtual DragOperation CanDragNode ()
		{
			return DragOperation.None;
		}
		
		public virtual bool CanDropNode (object dataObject, DragOperation operation, DropPosition position)
		{
			if (position == DropPosition.Into)
				return CanDropNode (dataObject, operation);
			else
				return false;
		}
		
		public virtual bool CanDropNode (object dataObject, DragOperation operation)
		{
			return false;
		}
		
		DropPosition cachedPosition;
		
		public virtual bool CanDropMultipleNodes (object[] dataObjects, DragOperation operation, DropPosition position)
		{
			cachedPosition = position;
			return CanDropMultipleNodes (dataObjects, operation);
		}
		
		public virtual bool CanDropMultipleNodes (object[] dataObjects, DragOperation operation)
		{
			foreach (object ob in dataObjects)
				if (!CanDropNode (ob, operation, cachedPosition))
					return false;
			return true;
		}
		
		public virtual void OnNodeDrop (object dataObjects, DragOperation operation, DropPosition position)
		{
			OnNodeDrop (dataObjects, operation);
		}
		
		public virtual void OnNodeDrop (object dataObjects, DragOperation operation)
		{
		}
		
		public virtual void OnMultipleNodeDrop (object[] dataObjects, DragOperation operation, DropPosition position)
		{
			cachedPosition = position;
			OnMultipleNodeDrop (dataObjects, operation);
		}
		
		public virtual void OnMultipleNodeDrop (object[] dataObjects, DragOperation operation)
		{
			foreach (object ob in dataObjects)
				OnNodeDrop (ob, operation, cachedPosition);
		}

		internal class TransactedNodeHandlerAttribute: CustomCommandTargetAttribute
		{
			protected override void Run (object target, Command cmd)
			{
				NodeCommandHandler nch = (NodeCommandHandler) target;
				if (nch.tree == null) {
					base.Run (target, cmd);
					return;
				}
				try {
					nch.tree.LockUpdates ();
					base.Run (target, cmd);
				} finally {
					nch.tree.UnlockUpdates ();
				}
			}
	
			protected override void Run (object target, Command cmd, object data)
			{
				base.Run (target, cmd, data);
			}
		}
	}


	internal class MultiSelectionNodeHandlerAttribute: CustomCommandUpdaterAttribute
	{
		// If multiple nodes are selected and the method does not have the AllowMultiSelectionAttribute
		// attribute, disable the command.
		
		protected override void CommandUpdate (object target, CommandArrayInfo cinfo)
		{
			NodeCommandHandler nc = (NodeCommandHandler) target;
			base.CommandUpdate (target, cinfo);
			if (nc.MultipleSelectedNodes) {
				bool allowMultiArray = false;
				ICommandArrayUpdateHandler h = ((ICommandArrayUpdateHandler)this).Next;
				while (h != null) {
					if (h is AllowMultiSelectionAttribute) {
						allowMultiArray = true;
						break;
					}
					h = h.Next;
				}
				if (!allowMultiArray)
					cinfo.Clear ();
			}
		}
		
		protected override void CommandUpdate (object target, CommandInfo cinfo)
		{
			NodeCommandHandler nc = (NodeCommandHandler) target;
			
			base.CommandUpdate (target, cinfo);
			
			if (nc.MultipleSelectedNodes) {
				bool allowMulti = false;
				ICommandUpdateHandler h = ((ICommandUpdateHandler)this).Next;
				while (h != null) {
					if (h is AllowMultiSelectionAttribute) {
						allowMulti = true;
						break;
					}
					h = h.Next;
				}
				if (!allowMulti)
					cinfo.Enabled = false;
			}
		}
	}

	public class AllowMultiSelectionAttribute: CustomCommandUpdaterAttribute
	{
	}
}
