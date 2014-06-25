//
// ExtensibleTreeView.cs
//
// Author:
//   Lluis Sanchez Gual
//   Mike Krüger <mkrueger@novell.com>
//
// Copyright (C) 2005-2008 Novell, Inc (http://www.novell.com)
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

//#define TREE_VERIFY_INTEGRITY

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

using Mono.Addins;
using MonoDevelop.Core;
using MonoDevelop.Components;
using MonoDevelop.Ide.Commands;
using MonoDevelop.Components.Commands;
using MonoDevelop.Ide.Gui.Pads;
using MonoDevelop.Projects.Extensions;
using System.Linq;
using MonoDevelop.Ide.Gui.Components.Internal;

namespace MonoDevelop.Ide.Gui.Components
{
	public partial class ExtensibleTreeView: Control, IExtensibleTreeViewFrontend
	{
		ExtensibleTreeViewBackend backend;
		NodeBuilder[] builders;
		Dictionary<Type, NodeBuilder[]> builderChains = new Dictionary<Type, NodeBuilder[]> ();
		NodePositionDictionary nodeHash = new NodePositionDictionary ();

		TreeBuilderContext builderContext;
		Hashtable callbacks = new Hashtable ();
		bool editingText;

		TreePadOption[] options;
		TreeOptions globalOptions;

		internal bool sorting;

		object[] copyObjects;
		DragOperation currentTransferOperation;

		TransactedNodeStore transactionStore;
		int updateLockCount;
		int updateCount;
		string contextMenuPath;
		IDictionary<string,string> contextMenuTypeNameAliases;

		public event EventHandler SelectionChanged;

		public IDictionary<string,string> ContextMenuTypeNameAliases {
			get { return contextMenuTypeNameAliases; }
			set { contextMenuTypeNameAliases = value; }
		}

		public string Id { get; set; }

		public bool AllowsMultipleSelection {
			get { return backend.AllowsMultipleSelection; }
			set { backend.AllowsMultipleSelection = value; }
		}

		public ExtensibleTreeView ()
		{
			#if __MAC__
			backend = new MacExtensibleTreeViewBackend ();
			#else
			backend = new ExtensibleTreeGtkBackend ();
			#endif
		}

		public ExtensibleTreeView (NodeBuilder[] builders, TreePadOption[] options) : this ()
		{
			Initialize (builders, options);
		}

		TreeOptions IExtensibleTreeViewFrontend.GlobalOptions {
			get { return globalOptions; }
		}

		ExtensibleTreeViewBackend IExtensibleTreeViewFrontend.Backend {
			get { return backend; }
		}

		protected override object CreateNativeWidget ()
		{
			return backend.CreateWidget ();
		}

		void CustomFontPropertyChanged (object sender, EventArgs a)
		{
			backend.UpdateFont ();
		}

		public void Initialize (NodeBuilder[] builders, TreePadOption[] options)
		{
			Initialize (builders, options, null);
		}

		public virtual void Initialize (NodeBuilder[] builders, TreePadOption[] options, string contextMenuPath)
		{
			this.contextMenuPath = contextMenuPath;
			builderContext = new TreeBuilderContext (this);

			SetBuilders (builders, options);
			IdeApp.Preferences.CustomPadFontChanged += CustomFontPropertyChanged;;

			backend.Initialize (this);
		}

		public void UpdateBuilders (NodeBuilder[] builders, TreePadOption[] options)
		{
			// Save the current state
			ITreeNavigator root = GetRootNode ();
			NodeState state = root != null ? root.SaveState () : null;
			object obj = root != null ? root.DataItem : null;

			Clear ();

			// Clean cached builder chains
			builderChains.Clear ();

			// Update the builders
			SetBuilders (builders, options);

			// Restore the this
			if (obj != null)
				LoadTree (obj);

			root = GetRootNode ();
			if (root != null && state != null)
				root.RestoreState (state);
		}

		void SetBuilders (NodeBuilder[] buildersArray, TreePadOption[] options)
		{
			// Create default options

			List<NodeBuilder> builders = new List<NodeBuilder> ();
			foreach (NodeBuilder nb in buildersArray) {
				if (!(nb is TreeViewItemBuilder))
					builders.Add (nb);
			}
			builders.Add (new TreeViewItemBuilder ());

			this.options = options;
			globalOptions = new TreeOptions ();
			foreach (TreePadOption op in options)
				globalOptions [op.Id] = op.DefaultValue;
			globalOptions.Pad = this;

			// Check that there is only one TypeNodeBuilder per type

			Hashtable bc = new Hashtable ();
			foreach (NodeBuilder nb in builders) {
				TypeNodeBuilder tnb = nb as TypeNodeBuilder;
				if (tnb != null) {
					if (tnb.UseReferenceEquality)
						nodeHash.RegisterByRefType (tnb.NodeDataType);
					TypeNodeBuilder other = (TypeNodeBuilder) bc [tnb.NodeDataType];
					if (other != null)
						throw new ApplicationException (string.Format ("The type node builder {0} can't be used in this context because the type {1} is already handled by {2}", nb.GetType(), tnb.NodeDataType, other.GetType()));
					bc [tnb.NodeDataType] = tnb;
				}
				else if (!(nb is NodeBuilderExtension))
					throw new InvalidOperationException (string.Format ("Invalid NodeBuilder type: {0}. NodeBuilders must inherit either from TypeNodeBuilder or NodeBuilderExtension", nb.GetType()));
			}

			NodeBuilders = builders.ToArray ();

			foreach (NodeBuilder nb in builders)
				nb.SetContext (builderContext);
		}

		protected override void Dispose (bool disposing)
		{
			base.Dispose (disposing);

			if (disposing) {
				foreach (var ob in nodeHash.AllObjects.ToArray ())
					NotifyNodeRemoved (ob, null);

				foreach (var b in NodeBuilders)
					b.Dispose ();

				copyObjects = null;
				nodeHash.Clear ();
			}
		}

		public void EnableDragUriSource (Func<object,string> nodeToUri)
		{
			backend.EnableDragUriSource (nodeToUri);
		}

		public void EnableAutoTooltips ()
		{
			backend.EnableAutoTooltips ();
		}

		void IExtensibleTreeViewFrontend.NotifySelectionChanged ()
		{
			OnSelectionChanged ();
		}

		protected virtual void OnSelectionChanged ()
		{
			if (SelectionChanged != null)
				SelectionChanged (this, EventArgs.Empty);
		}

		bool IExtensibleTreeViewFrontend.CheckAndDrop (TreeNodeNavigator nav, DragOperation oper, DropPosition dropPos, bool drop, object[] obj)
		{
			NodeBuilder[] chain = nav.NodeBuilderChain;
			bool foundHandler = false;

			bool updatesLocked = false;

			try {
				foreach (NodeBuilder nb in chain) {
					try {
						NodeCommandHandler handler = nb.CommandHandler;
						handler.SetCurrentNode (nav);
						if (handler.CanDropMultipleNodes (obj, oper, dropPos)) {
							foundHandler = true;
							if (drop) {
								if (!updatesLocked) {
									LockUpdates ();
									updatesLocked = true;
								}
								handler.OnMultipleNodeDrop (obj, oper, dropPos);
							}
						}
					} catch (Exception ex) {
						LoggingService.LogError (ex.ToString ());
					}
				}
			} catch (Exception ex) {
				// We're now in an indeterminate state, so report the exception
				// and exit.
				GLib.ExceptionManager.RaiseUnhandledException (ex, true);
				return false;
			} finally {
				if (updatesLocked)
					UnlockUpdates ();
			}
			return foundHandler;
		}

		internal void LockUpdates ()
		{
			if (++updateLockCount == 1) {
				transactionStore = new TransactedNodeStore (this, backend);
				BeginTreeUpdate ();
			}
		}

		internal void UnlockUpdates ()
		{
			if (--updateLockCount == 0) {
				try {
					TransactedNodeStore store = transactionStore;
					transactionStore = null;
					store.CommitChanges ();
				} finally {
					EndTreeUpdate ();
				}
			}
		}

		public void BeginTreeUpdate ()
		{
			if (++updateCount == 1)
				backend.BeginTreeUpdate ();
		}

		public void EndTreeUpdate ()
		{
			if (--updateCount == 0)
				backend.EndTreeUpdate ();
		}

		public void CancelTreeUpdate ()
		{
			updateCount--;
		}

		internal ITreeBuilder CreateBuilder (ITreeNavigator nav = null)
		{
			if (transactionStore != null)
				return new TransactedTreeBuilder (this, transactionStore, (TreeNodeNavigator)nav);
			else if (nav is ITreeBuilder)
				return (ITreeBuilder)nav;
			else {
				var r = backend.CreateNavigator ();
				if (nav != null)
					r.MoveToPosition (nav.CurrentPosition);
				return r;
			}
		}

		protected NodeBuilder[] NodeBuilders {
			get { return builders; }
			set { builders = value; }
		}

		NodePositionDictionary IExtensibleTreeViewFrontend.NodeHash {
			get {
				return nodeHash;
			}
		}

		public ITreeBuilderContext BuilderContext {
			get {
				return builderContext;
			}
		}

		public object[] CopyObjects {
			get {
				return copyObjects;
			}
			set {
				copyObjects = value;
			}
		}

		public DragOperation CurrentTransferOperation {
			get {
				return currentTransferOperation;
			}
		}

		public ITreeBuilder LoadTree (object nodeObject)
		{
			try {
				BeginTreeUpdate ();
				Clear ();
				var builder = backend.CreateNavigator ();
				builder.AddRoot (nodeObject);
				builder.Expanded = true;
				InitialSelection ();
				return builder;
			} finally {
				EndTreeUpdate ();
			}
		}

		public ITreeBuilder AddChild (object nodeObject)
		{
			var builder = backend.CreateNavigator ();
			builder.AddRoot (nodeObject);
			builder.Expanded = true;
			InitialSelection ();
			return builder;
		}

		public void RemoveChild (object nodeObject)
		{
			var builder = backend.CreateNavigator ();
			if (builder.MoveToObject (nodeObject)) {
				builder.Remove ();
				InitialSelection ();
			}
		}

		void InitialSelection ()
		{
			if (!GetSelectedNodes ().Any ()) {
				var nav = backend.GetRootNode ();
				if (nav != null)
					nav.Selected = true;
			}
		}

		public void Clear ()
		{
			try {
				BeginTreeUpdate ();

				copyObjects = null;

				foreach (object dataObject in nodeHash.AllObjects.ToArray ())
					NotifyNodeRemoved (dataObject, null);

				backend.Clear ();
				nodeHash.Clear ();
			} finally {
				EndTreeUpdate ();
			}
		}

		public ITreeNavigator GetSelectedNode ()
		{
			var pos = backend.GetSelectedNode ();
			if (pos != null)
				return backend.GetNodeAtPosition (pos);
			else
				return null;
		}

		IEnumerable<SelectionGroup> GetSelectedNodesGrouped ()
		{
			var positions = backend.GetSelectedNodes ();
			if (positions.Length == 0) {
				return new SelectionGroup [0];
			}
			if (positions.Length == 1) {
				SelectionGroup grp = new SelectionGroup ();
				var nav = backend.GetNodeAtPosition (positions [0]);
				grp.BuilderChain = nav.NodeBuilderChain;
				grp.Nodes = new List<ITreeNavigator> ();
				grp.Nodes.Add (nav);
				return new [] { grp };
			}

			Dictionary<NodeBuilder[], SelectionGroup> dict = new Dictionary<NodeBuilder[],SelectionGroup> ();
			for (int n=0; n<positions.Length; n++) {
				SelectionGroup grp;
				var nav = backend.GetNodeAtPosition (positions [n]);
				if (!dict.TryGetValue (nav.NodeBuilderChain, out grp)) {
					grp = new SelectionGroup ();
					grp.BuilderChain = nav.NodeBuilderChain;
					grp.Nodes = new List<ITreeNavigator> ();
					dict [nav.NodeBuilderChain] = grp;
				}
				grp.Nodes.Add (nav);
			}
			return dict.Values;
		}

		public bool MultipleNodesSelected ()
		{
			return backend.MultipleNodesSelected ();
		}

		public ITreeNavigator[] GetSelectedNodes ()
		{
			var nodes = backend.GetSelectedNodes ();
			var res = new ITreeNavigator [nodes.Length];
			for (int n = 0; n < nodes.Length; n++)
				res [n] = backend.GetNodeAtPosition (nodes [n]);
			return res;
		}

		public ITreeNavigator GetNodeAtPosition (NodePosition position)
		{
			return backend.GetNodeAtPosition (position);
		}

		public ITreeNavigator GetNodeAtObject (object dataObject)
		{
			return GetNodeAtObject (dataObject, false);
		}

		public ITreeNavigator GetNodeAtObject (object dataObject, bool createTreeBranch)
		{
			var pos = nodeHash.GetNodePositions (dataObject);
			if (pos.Length == 0) {
				if (createTreeBranch) {
					TypeNodeBuilder tnb = GetTypeNodeBuilder (dataObject.GetType());
					if (tnb == null) return null;

					object parent = tnb.GetParentObject (dataObject);
					if (parent == null || parent == dataObject || dataObject.Equals (parent)) return null;

					ITreeNavigator pnav = GetNodeAtObject (parent, true);
					if (pnav == null) return null;

					pnav.MoveToFirstChild ();

					// The child should be now in the this. Try again.
					pos = nodeHash.GetNodePositions (dataObject);
					if (pos.Length == 0)
						return null;
				} else
					return null;
			}

			return backend.GetNodeAtPosition (pos [0]);
		}

		public ITreeNavigator GetRootNode ()
		{
			return backend.GetRootNode ();
		}

		internal bool IsRegistered (object dataObject)
		{
			return nodeHash.IsRegistered (dataObject);
		}

		void IExtensibleTreeViewFrontend.RegisterNode (NodePosition it, object dataObject, NodeBuilder[] chain, bool fireAddedEvent)
		{
			if (nodeHash.RegisterNode (dataObject, it)) {
				if (fireAddedEvent) {
					if (chain == null)
						chain = GetBuilderChain (dataObject.GetType ());
					foreach (NodeBuilder nb in chain) {
						try {
							nb.OnNodeAdded (dataObject);
						} catch (Exception ex) {
							LoggingService.LogError (ex.ToString ());
						}
					}
				}
			}
		}

		void IExtensibleTreeViewFrontend.UnregisterNode (object dataObject, NodePosition pos, NodeBuilder[] chain, bool fireRemovedEvent)
		{
			// Remove object from copy list

			if (copyObjects != null) {
				int i = Array.IndexOf (copyObjects, dataObject);
				if (i != -1) {
					ArrayList list = new ArrayList (copyObjects);
					list.RemoveAt (i);
					if (list.Count > 0)
						copyObjects = list.ToArray ();
					else
						copyObjects = null;
				}
			}

			if (nodeHash.UnregisterNode (dataObject, pos) && fireRemovedEvent)
				NotifyNodeRemoved (dataObject, chain);
		}

		public void AddNodeInsertCallback (object dataObject, TreeNodeCallback callback)
		{
			if (IsRegistered (dataObject)) {
				callback (GetNodeAtObject (dataObject));
				return;
			}

			ArrayList list = callbacks [dataObject] as ArrayList;
			if (list != null)
				list.Add (callback);
			else {
				list = new ArrayList ();
				list.Add (callback);
				callbacks [dataObject] = list;
			}
		}

		internal object GetNextCommandTarget ()
		{
			return null;
		}

		class MulticastNodeRouter: IMultiCastCommandRouter
		{
			ArrayList targets;

			public MulticastNodeRouter (ArrayList targets)
			{
				this.targets = targets;
			}

			public IEnumerable GetCommandTargets ()
			{
				return targets;
			}
		}

		internal object GetDelegatedCommandTarget ()
		{
			// If a node is being edited, don't delegate commands to the
			// node builders, since what's selected is not the node,
			// but the node label. In this way commands such as Delete
			// will be handled by the node Entry.
			if (editingText)
				return null;

			ArrayList targets = new ArrayList ();

			foreach (SelectionGroup grp in GetSelectedNodesGrouped ()) {
				NodeBuilder[] chain = grp.BuilderChain;
				if (chain.Length > 0) {
					ITreeNavigator[] nodes = grp.Nodes.ToArray ();
					NodeCommandTargetChain targetChain = null;
					NodeCommandTargetChain lastNode = null;
					foreach (NodeBuilder nb in chain) {
						NodeCommandTargetChain newNode = new NodeCommandTargetChain (nb.CommandHandler, nodes);
						if (lastNode == null)
							targetChain = lastNode = newNode;
						else {
							lastNode.Next = newNode;
							lastNode = newNode;
						}
					}

					if (targetChain != null)
						targets.Add (targetChain);
				}
			}
			if (targets.Count == 1)
				return targets[0];
			else if (targets.Count > 1)
				return new MulticastNodeRouter (targets);
			else
				return null;
		}

		public void ExpandCurrentItem ()
		{
			try {
				LockUpdates ();

				IEnumerable<SelectionGroup> nodeGroups = GetSelectedNodesGrouped ();
				if (nodeGroups.Count () == 1) {
					SelectionGroup grp = nodeGroups.First ();

					if (grp.Nodes.Count () == 1) {
						ITreeNavigator node = grp.Nodes.First ();
						if (node.Expanded) {
							grp.SavePositions ();
							node.Selected = false;
							if (node.MoveToFirstChild ())
								node.Selected = true;

							// This exit statement is so that it doesn't do 2 actions at a time.
							// As in, navigate, then expand.
							return;
						}
					}
				}

				foreach (SelectionGroup grp in nodeGroups) {
					grp.SavePositions ();

					foreach (var node in grp.Nodes) {
						node.Expanded = true;
					}
				}
			} finally {
				UnlockUpdates ();
			}
		}

		public void CollapseCurrentItem ()
		{
			try {
				LockUpdates ();

				IEnumerable<SelectionGroup> nodeGroups = GetSelectedNodesGrouped ();
				if (nodeGroups.Count () == 1) {
					SelectionGroup grp = nodeGroups.First ();

					if (grp.Nodes.Count () == 1)
					{
						ITreeNavigator node = grp.Nodes.First ();
						if (!node.HasChildren () || !node.Expanded) {
							grp.SavePositions ();
							node.Selected = false;
							if (node.MoveToParent ())
								node.Selected = true;

							// This exit statement is so that it doesn't do 2 actions at a time.
							// As in, navigate, then collapse.
							return;
						}
					}
				}

				foreach (SelectionGroup grp in nodeGroups) {
					grp.SavePositions ();

					foreach (var node in grp.Nodes) {
						node.Expanded = false;
					}
				}
			} finally {
				UnlockUpdates ();
			}
		}

		void IExtensibleTreeViewFrontend.OnCurrentItemActivated ()
		{
			ActivateCurrentItem ();
		}

		[CommandHandler (ViewCommands.Open)]
		public virtual void ActivateCurrentItem ()
		{
			try {
				LockUpdates ();
				foreach (SelectionGroup grp in GetSelectedNodesGrouped ()) {
					grp.SavePositions ();
					foreach (NodeBuilder b in grp.BuilderChain) {
						NodeCommandHandler handler = b.CommandHandler;
						handler.SetCurrentNodes (grp.Nodes.ToArray ());
						handler.ActivateMultipleItems ();
						if (!grp.RestorePositions (this))
							break;
					}
				}
				OnCurrentItemActivated (EventArgs.Empty);
			} finally {
				UnlockUpdates ();
			}
		}

		void IExtensibleTreeViewFrontend.OnCurrentItemDeleted ()
		{
			DeleteCurrentItem ();
		}

		public void DeleteCurrentItem ()
		{
			try {
				LockUpdates ();
				OnDeleteCurrentItem ();
			} finally {
				UnlockUpdates ();
			}
		}

		protected virtual void OnDeleteCurrentItem ()
		{
			try {
				LockUpdates ();
				foreach (SelectionGroup grp in GetSelectedNodesGrouped ()) {
					NodeBuilder[] chain = grp.BuilderChain;
					grp.SavePositions ();
					foreach (NodeBuilder b in chain) {
						NodeCommandHandler handler = b.CommandHandler;
						handler.SetCurrentNodes (grp.Nodes.ToArray ());
						if (handler.CanDeleteMultipleItems ()) {
							if (!grp.RestorePositions (this))
								return;
							handler.DeleteMultipleItems ();
							// FIXME: fixes bug #396566, but it is not 100% correct
							// It can only be fully fixed if updates to the tree are delayed
							break;
						}
						if (!grp.RestorePositions (this))
							return;
					}
				}
			} finally {
				UnlockUpdates ();
			}
		}

		protected virtual bool CanDeleteCurrentItem ()
		{
			foreach (SelectionGroup grp in GetSelectedNodesGrouped ()) {
				NodeBuilder[] chain = grp.BuilderChain;
				grp.SavePositions ();
				foreach (NodeBuilder b in chain) {
					NodeCommandHandler handler = b.CommandHandler;
					handler.SetCurrentNodes (grp.Nodes.ToArray ());
					if (handler.CanDeleteMultipleItems ())
						return true;
					if (!grp.RestorePositions (this))
						return false;
				}
			}
			return false;
		}

		void IExtensibleTreeViewFrontend.OnSelectionChanged ()
		{
			TreeNodeNavigator node = (TreeNodeNavigator) GetSelectedNode ();
			if (node != null) {
				NodeBuilder[] chain = node.NodeBuilderChain;
				NodePosition pos = node.CurrentPosition;
				foreach (NodeBuilder b in chain) {
					try {
						NodeCommandHandler handler = b.CommandHandler;
						handler.SetCurrentNode (node);
						handler.OnItemSelected ();
					} catch (Exception ex) {
						LoggingService.LogError (ex.ToString ());
					}
					node.MoveToPosition (pos);
				}
			}
		}

		[CommandHandler (ViewCommands.RefreshTree)]
		public virtual void RefreshCurrentItem ()
		{
			try {
				BeginTreeUpdate ();
				try {
					LockUpdates ();
					foreach (SelectionGroup grp in GetSelectedNodesGrouped ()) {
						NodeBuilder[] chain = grp.BuilderChain;
						grp.SavePositions ();
						foreach (NodeBuilder b in chain) {
							NodeCommandHandler handler = b.CommandHandler;
							handler.SetCurrentNodes (grp.Nodes.ToArray ());
							if (!grp.RestorePositions (this))
								return;
							handler.RefreshMultipleItems ();
							if (!grp.RestorePositions (this))
								return;
						}
					}
				} finally {
					UnlockUpdates ();
				}
				RefreshTree ();
			} finally {
				EndTreeUpdate ();
			}
		}

		protected virtual void OnCurrentItemActivated (EventArgs args)
		{
			if (CurrentItemActivated != null)
				CurrentItemActivated (this, args);
		}

		public event EventHandler CurrentItemActivated;

		[CommandHandler (EditCommands.Copy)]
		public void CopyCurrentItem ()
		{
			CancelTransfer ();
			TransferCurrentItem (DragOperation.Copy);
		}

		[CommandHandler (EditCommands.Cut)]
		public void CutCurrentItem ()
		{
			CancelTransfer ();
			TransferCurrentItem (DragOperation.Move);

			if (copyObjects != null) {
				try {
					BeginTreeUpdate ();
					foreach (object ob in copyObjects) {
						ITreeBuilder tb = CreateBuilder ();
						if (tb.MoveToObject (ob))
							tb.Update ();
					}
				} finally {
					EndTreeUpdate ();
				}
			}
		}

		[CommandUpdateHandler (EditCommands.Copy)]
		protected void UpdateCopyCurrentItem (CommandInfo info)
		{
			if (editingText) {
				info.Bypass = true;
				return;
			}
			info.Enabled = CanTransferCurrentItem (DragOperation.Copy);
		}

		[CommandUpdateHandler (EditCommands.Cut)]
		protected void UpdateCutCurrentItem (CommandInfo info)
		{
			if (editingText) {
				info.Bypass = true;
				return;
			}
			info.Enabled = CanTransferCurrentItem (DragOperation.Move);
		}

		void TransferCurrentItem (DragOperation oper)
		{
			foreach (SelectionGroup grp in GetSelectedNodesGrouped ()) {
				NodeBuilder[] chain = grp.BuilderChain;
				grp.SavePositions ();
				foreach (NodeBuilder b in chain) {
					try {
						NodeCommandHandler handler = b.CommandHandler;
						handler.SetCurrentNodes (grp.Nodes.ToArray ());
						if ((handler.CanDragNode () & oper) != 0) {
							grp.RestorePositions (this);
							copyObjects = grp.DataItems;
							currentTransferOperation = oper;
							break;
						}
					} catch (Exception ex) {
						LoggingService.LogError (ex.ToString ());
					}
					grp.RestorePositions (this);
				}
			}
		}

		bool CanTransferCurrentItem (DragOperation oper)
		{
			TreeNodeNavigator node = (TreeNodeNavigator) GetSelectedNode ();
			if (node != null) {
				NodeBuilder[] chain = node.NodeBuilderChain;
				NodePosition pos = node.CurrentPosition;
				foreach (NodeBuilder b in chain) {
					try {
						NodeCommandHandler handler = b.CommandHandler;
						handler.SetCurrentNode (node);
						if ((handler.CanDragNode () & oper) != 0)
							return true;
					} catch (Exception ex) {
						LoggingService.LogError (ex.ToString ());
					}
					node.MoveToPosition (pos);
				}
			}
			return false;
		}

		[CommandHandler (EditCommands.Paste)]
		public void PasteToCurrentItem ()
		{
			if (copyObjects == null) return;

			try {
				LockUpdates ();
				TreeNodeNavigator node = (TreeNodeNavigator) GetSelectedNode ();
				if (node != null) {
					NodeBuilder[] chain = node.NodeBuilderChain;
					NodePosition pos = node.CurrentPosition;
					foreach (NodeBuilder b in chain) {
						NodeCommandHandler handler = b.CommandHandler;
						handler.SetCurrentNode (node);
						if (handler.CanDropMultipleNodes (copyObjects, currentTransferOperation, DropPosition.Into)) {
							node.MoveToPosition (pos);
							handler.OnMultipleNodeDrop (copyObjects, currentTransferOperation, DropPosition.Into);
						}
						node.MoveToPosition (pos);
					}
				}
				if (currentTransferOperation == DragOperation.Move)
					CancelTransfer ();
			} finally {
				UnlockUpdates ();
			}
		}

		[CommandUpdateHandler (EditCommands.Paste)]
		protected void UpdatePasteToCurrentItem (CommandInfo info)
		{
			if (editingText) {
				info.Bypass = true;
				return;
			}

			if (copyObjects != null) {
				TreeNodeNavigator node = (TreeNodeNavigator) GetSelectedNode ();
				if (node != null) {
					NodeBuilder[] chain = node.NodeBuilderChain;
					NodePosition pos = node.CurrentPosition;
					foreach (NodeBuilder b in chain) {
						NodeCommandHandler handler = b.CommandHandler;
						handler.SetCurrentNode (node);
						if (handler.CanDropMultipleNodes (copyObjects, currentTransferOperation, DropPosition.Into)) {
							info.Enabled = true;
							return;
						}
						node.MoveToPosition (pos);
					}
				}
			}
			info.Enabled = false;
		}

		void CancelTransfer ()
		{
			try {
				BeginTreeUpdate ();
				if (copyObjects != null) {
					object[] oldCopyObjects = copyObjects;
					copyObjects = null;
					if (currentTransferOperation == DragOperation.Move) {
						foreach (object ob in oldCopyObjects) {
							ITreeBuilder tb = CreateBuilder ();
							if (tb.MoveToObject (ob))
								tb.Update ();
						}
					}
				}
			} finally {
				EndTreeUpdate ();
			}
		}

		public NodeState SaveTreeState ()
		{
			ITreeNavigator root = GetRootNode ();
			if (root == null)
				return null;

			var state = root.SaveState ();

			var s = new Dictionary<string, bool> ();
			foreach (TreePadOption opt in options) {
				bool val;
				if (globalOptions.TryGetValue (opt.Id, out val) && val != opt.DefaultValue)
					s[opt.Id] = val;
			}
			if (s.Count != 0) {
				state.Options = s;
			}

			return state;
		}

		public void RestoreTreeState (NodeState state)
		{
			if (state == null)
				return;

			ITreeNavigator nav = GetRootNode ();
			if (nav == null)
				return;

			try {
				BeginTreeUpdate ();
				nav.RestoreState (state);

				globalOptions = new TreeOptions ();
				foreach (TreePadOption opt in options) {
					bool val = false;
					if (state.Options == null || !state.Options.TryGetValue (opt.Id, out val))
						val = opt.DefaultValue;
					globalOptions[opt.Id] = val;
				}
				globalOptions.Pad = this;
				RefreshTree ();
			} finally {
				EndTreeUpdate ();
			}
		}

		TypeNodeBuilder GetTypeNodeBuilder (Type type)
		{
			NodeBuilder[] chain = GetBuilderChain (type);
			if (chain == null) return null;
			return (TypeNodeBuilder) chain [0];
		}

		NodeBuilder[] IExtensibleTreeViewFrontend.GetBuilderChain (Type type)
		{
			return GetBuilderChain (type);
		}

		public NodeBuilder[] GetBuilderChain (Type type)
		{
			NodeBuilder[] chain;
			builderChains.TryGetValue (type, out chain);
			if (chain == null) {
				List<NodeBuilder> list = new List<NodeBuilder> ();

				// Find the most specific node builder type.
				TypeNodeBuilder bestTypeNodeBuilder = null;
				Type bestNodeType = null;

				foreach (NodeBuilder nb in builders) {
					if (nb is TypeNodeBuilder) {
						TypeNodeBuilder tnb = (TypeNodeBuilder) nb;
						if (tnb.NodeDataType.IsAssignableFrom (type)) {
							if (bestNodeType == null || bestNodeType.IsAssignableFrom (tnb.NodeDataType)) {
								bestNodeType = tnb.NodeDataType;
								bestTypeNodeBuilder = tnb;
							}
						}
					} else {
						try {
							if (((NodeBuilderExtension)nb).CanBuildNode (type))
								list.Add (nb);
						} catch (Exception ex) {
							LoggingService.LogError (ex.ToString ());
						}
					}
				}

				if (bestTypeNodeBuilder != null) {
					list.Insert (0, bestTypeNodeBuilder);
					chain = list.ToArray ();
				} else
					chain = null;

				builderChains [type] = chain;
			}
			return chain;
		}

		int IExtensibleTreeViewFrontend.CompareObjects (NodeBuilder[] chain, ITreeNavigator thisNode, ITreeNavigator otherNode)
		{
			int result = NodeBuilder.DefaultSort;
			for (int n=0; n<chain.Length; n++) {
				int sort = chain[n].CompareObjects (thisNode, otherNode);
				if (sort != NodeBuilder.DefaultSort)
					result = sort;
			}
			return result;
		}

		void NotifyNodeRemoved (object dataObject, NodeBuilder[] chain)
		{
			if (chain == null)
				chain = GetBuilderChain (dataObject.GetType());
			foreach (NodeBuilder nb in chain) {
				try {
					nb.OnNodeRemoved (dataObject);
				} catch (Exception ex) {
					LoggingService.LogError (ex.ToString ());
				}
			}
		}

		public void NotifyInserted (ITreeNavigator nav, object dataObject)
		{
			if (callbacks.Count > 0) {
				ArrayList list = callbacks [dataObject] as ArrayList;
				if (list != null) {
					NodePosition pos = nav.CurrentPosition;
					foreach (TreeNodeCallback callback in list) {
						callback (nav);
						nav.MoveToPosition (pos);
					}
					callbacks.Remove (dataObject);
				}
			}
		}

		internal void ResetState (ITreeNavigator nav)
		{
			if (nav is TreeNodeNavigator)
				((TreeNodeNavigator)nav).ResetState ();
			else if (nav is TransactedTreeBuilder)
				((TransactedTreeBuilder)nav).ResetState ();
			else {
				ITreeBuilder builder = CreateBuilder (nav);
				ResetState (builder);
			}
		}

		/// <summary>
		/// If you want to edit a node label. Select the node you want to edit and then
		/// call this method, instead of using the LabelEdit Property and the BeginEdit
		/// Method directly.
		/// </summary>
		[CommandHandler (EditCommands.Rename)]
		public void StartLabelEdit ()
		{
			GLib.Timeout.Add (20, new GLib.TimeoutHandler (wantFocus));
		}

		[CommandUpdateHandler (EditCommands.Rename)]
		public void UpdateStartLabelEdit (CommandInfo info)
		{
			if (editingText || GetSelectedNodes ().Length != 1) {
				info.Visible = false;
				return;
			}

			TreeNodeNavigator node = (TreeNodeNavigator) GetSelectedNode ();
			NodeAttributes attributes = GetNodeAttributes (node);
			if ((attributes & NodeAttributes.AllowRename) == 0) {
				info.Visible = false;
				return;
			}
		}

		NodeAttributes GetNodeAttributes (TreeNodeNavigator node)
		{
			object dataObject = node.DataItem;
			NodeAttributes attributes = NodeAttributes.None;

			ITreeNavigator parentNode = node.Clone ();
			parentNode.MoveToParent ();
			NodePosition pos = parentNode.CurrentPosition;

			foreach (NodeBuilder b in node.NodeBuilderChain) {
				try {
					b.GetNodeAttributes (parentNode, dataObject, ref attributes);
				} catch (Exception ex) {
					LoggingService.LogError (ex.ToString ());
				}
				parentNode.MoveToPosition (pos);
			}
			return attributes;
		}


		bool wantFocus ()
		{
			backend.StartLabelEdit ();
			return false;
		}

		void IExtensibleTreeViewFrontend.RenameNode (TreeNodeNavigator nav, string newText)
		{
			try {
				LockUpdates ();
				NodeBuilder[] chain = nav.NodeBuilderChain;
				NodePosition pos = nav.CurrentPosition;
				foreach (NodeBuilder b in chain) {
					try {
						NodeCommandHandler handler = b.CommandHandler;
						handler.SetCurrentNode (nav);
						handler.RenameItem (newText);
					} catch (Exception ex) {
						MessageService.ShowException (ex);
						LoggingService.LogError (ex.ToString ());
					}
					nav.MoveToPosition (pos);
				}
			} finally {
				UnlockUpdates ();
			}
		}

		[CommandUpdateHandler (ViewCommands.TreeDisplayOptionList)]
		protected void BuildTreeOptionsMenu (CommandArrayInfo info)
		{
			foreach (TreePadOption op in options) {
				CommandInfo ci = new CommandInfo (op.Label);
				ci.Checked = globalOptions [op.Id];
				info.Add (ci, op.Id);
			}
		}

		[CommandHandler (ViewCommands.TreeDisplayOptionList)]
		protected void OptionToggled (string optionId)
		{
			globalOptions [optionId] = !globalOptions [optionId];
			RefreshRoots ();
		}

		[CommandHandler (ViewCommands.ResetTreeDisplayOptions)]
		protected void ResetOptions ()
		{
			foreach (TreePadOption op in options)
				globalOptions [op.Id] = op.DefaultValue;

			RefreshRoots ();
		}

		void RefreshRoots ()
		{
			var nav = backend.GetRootNode ();
			if (nav == null)
				return;

			var tb = CreateBuilder (nav);
			do {
				tb.UpdateAll ();
			} while (tb.MoveNext ());
		}

		protected void RefreshTree ()
		{
			BeginTreeUpdate ();
			try {
				foreach (var node in GetSelectedNodes ()) {
					ITreeBuilder tb = CreateBuilder (node);
					tb.UpdateAll ();
				}
			} finally {
				EndTreeUpdate ();
			}
		}

		[CommandHandler (ViewCommands.CollapseAllTreeNodes)]
		protected void CollapseTree ()
		{
			backend.CollapseTree ();
		}

		public bool ShowSelectionPopupButton {
			get { return backend.ShowSelectionPopupButton; }
			set { backend.ShowSelectionPopupButton = value; }
		}

		NodeAttributes IExtensibleTreeViewFrontend.GetAttributes (ITreeBuilder tb, NodeBuilder[] chain, object dataObject)
		{
			NodePosition pos = tb.CurrentPosition;
			NodeAttributes ats = NodeAttributes.None;

			foreach (NodeBuilder nb in chain) {
				try {
					nb.GetNodeAttributes (tb, dataObject, ref ats);
				} catch (Exception ex) {
					LoggingService.LogError (ex.ToString ());
				}
				tb.MoveToPosition (pos);
			}
			return ats;
		}

		bool IExtensibleTreeViewFrontend.HasChildNodes (ITreeBuilder tb, NodeBuilder[] chain, object dataObject)
		{
			NodePosition pos = tb.CurrentPosition;
			foreach (NodeBuilder nb in chain) {
				try {
					bool res = nb.HasChildNodes (tb, dataObject);
					if (res) return true;
				} catch (Exception ex) {
					LoggingService.LogError (ex.ToString ());
				} finally {
					tb.MoveToPosition (pos);
				}
			}
			return false;
		}

		NodeInfo IExtensibleTreeViewFrontend.GetNodeInfo (ITreeBuilder tb, NodeBuilder[] chain, object dataObject)
		{
			NodeInfo nodeInfo = new NodeInfo () {
				Label = string.Empty
			};

			NodePosition pos = tb.CurrentPosition;

			foreach (NodeBuilder builder in chain) {
				try {
					builder.BuildNode (tb, dataObject, nodeInfo);
				} catch (Exception ex) {
					LoggingService.LogError (ex.ToString ());
				}
				tb.MoveToPosition (pos);
			}

			if (nodeInfo.ClosedIcon == null) nodeInfo.ClosedIcon = nodeInfo.Icon;

			if (CopyObjects != null && ((IList)CopyObjects).Contains (dataObject) && CurrentTransferOperation == DragOperation.Move) {
				var gicon = BuilderContext.GetComposedIcon (nodeInfo.Icon, "fade");
				if (gicon == null) {
					gicon = nodeInfo.Icon.WithAlpha (0.5);
					BuilderContext.CacheComposedIcon (nodeInfo.Icon, "fade", gicon);
				}
				nodeInfo.Icon = gicon;
				gicon = BuilderContext.GetComposedIcon (nodeInfo.ClosedIcon, "fade");
				if (gicon == null) {
					gicon = nodeInfo.ClosedIcon.WithAlpha (0.5);
					BuilderContext.CacheComposedIcon (nodeInfo.ClosedIcon, "fade", gicon);
				}
				nodeInfo.ClosedIcon = gicon;
			}
			return nodeInfo;
		}

		CommandEntrySet IExtensibleTreeViewFrontend.CreateContextMenu ()
		{
			return OnCreateContextMenu ();
		}

		protected virtual CommandEntrySet OnCreateContextMenu ()
		{
			TreeNodeNavigator tnav = (TreeNodeNavigator) GetSelectedNode ();
			if (tnav == null)
				return null;
			TypeNodeBuilder nb = tnav.TypeNodeBuilder;
			string menuPath = nb != null && nb.ContextMenuAddinPath != null ? nb.ContextMenuAddinPath : contextMenuPath;
			if (menuPath == null) {
				if (options.Length > 0) {
					CommandEntrySet opset = new CommandEntrySet ();
					opset.AddItem (ViewCommands.TreeDisplayOptionList);
					opset.AddItem (Command.Separator);
					opset.AddItem (ViewCommands.ResetTreeDisplayOptions);
					return opset;
				}
				return null;
			} else {
				ExtensionContext ctx = AddinManager.CreateExtensionContext ();
				ctx.RegisterCondition ("ItemType", new ItemTypeCondition (tnav.DataItem.GetType (), contextMenuTypeNameAliases));
				CommandEntrySet eset = IdeApp.CommandService.CreateCommandEntrySet (ctx, menuPath);

				eset.AddItem (Command.Separator);
				if (!tnav.Clone ().MoveToParent ()) {
					CommandEntrySet opset = eset.AddItemSet (GettextCatalog.GetString ("Display Options"));
					opset.AddItem (ViewCommands.TreeDisplayOptionList);
					opset.AddItem (Command.Separator);
					opset.AddItem (ViewCommands.ResetTreeDisplayOptions);
					//	opset.AddItem (ViewCommands.CollapseAllTreeNodes);
				}
				eset.AddItem (ViewCommands.RefreshTree);
				return eset;
			}
		}

		internal class TreeBuilderContext: ITreeBuilderContext
		{
			ExtensibleTreeView pad;
			Hashtable icons = new Hashtable ();
			Hashtable composedIcons = new Hashtable ();

			internal TreeBuilderContext (ExtensibleTreeView pad)
			{
				this.pad = pad;
			}

			public ITreeBuilder GetTreeBuilder ()
			{
				var nav = pad.backend.GetRootNode ();
				if (nav == null)
					return pad.backend.CreateNavigator ();
				else
					return pad.CreateBuilder (nav);
			}

			public ITreeBuilder GetTreeBuilder (object dataObject)
			{
				ITreeBuilder tb = pad.CreateBuilder ();
				if (tb.MoveToObject (dataObject))
					return tb;
				else
					return null;
			}

			public ITreeBuilder GetTreeBuilder (ITreeNavigator navigator)
			{
				return pad.CreateBuilder (navigator);
			}

			public Xwt.Drawing.Image GetIcon (string id)
			{
				Xwt.Drawing.Image icon = icons [id] as Xwt.Drawing.Image;
				if (icon == null) {
					icon = ImageService.GetIcon (id).WithSize (Gtk.IconSize.Menu);
					icons [id] = icon;
				}
				return icon;
			}

			public Xwt.Drawing.Image GetComposedIcon (Xwt.Drawing.Image baseIcon, object compositionKey)
			{
				Hashtable itable = composedIcons [baseIcon] as Hashtable;
				if (itable == null) return null;
				return itable [compositionKey] as Xwt.Drawing.Image;
			}

			public Xwt.Drawing.Image CacheComposedIcon (Xwt.Drawing.Image baseIcon, object compositionKey, Xwt.Drawing.Image composedIcon)
			{
				Hashtable itable = composedIcons [baseIcon] as Hashtable;
				if (itable == null) {
					itable = new Hashtable ();
					composedIcons [baseIcon] = itable;
				}
				itable [compositionKey] = composedIcon;
				return composedIcon;
			}

			public ITreeNavigator GetTreeNavigator (object dataObject)
			{
				return pad.GetNodeAtObject (dataObject, false);
			}

			public ExtensibleTreeView Tree {
				get { return pad; }
			}
		}
	}

}
