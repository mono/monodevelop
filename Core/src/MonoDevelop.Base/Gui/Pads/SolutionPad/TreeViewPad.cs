//
// TreeViewPad.cs
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
using System.IO;
using System.ComponentModel;
using System.Drawing;
using System.Diagnostics;
using System.Collections;
using System.Collections.Specialized;
using System.Collections.Utility;
using System.Xml;
using System.Resources;
using System.Text;

using MonoDevelop.Core.Properties;
using MonoDevelop.Core.AddIns;
using MonoDevelop.Core.AddIns.Codons;
using MonoDevelop.Core.Services;
using MonoDevelop.Internal.Project;
using MonoDevelop.Gui.Dialogs;
using MonoDevelop.Services;
using MonoDevelop.Gui.Widgets;
using MonoDevelop.Commands;

namespace MonoDevelop.Gui.Pads
{
	/// <summary>
	/// This class implements a project browser.
	/// </summary>
	public class TreeViewPad : IPadContent, IMementoCapable, ICommandDelegatorRouter
	{
		string title;
		string icon;
		string id;
		string defaultPosition = "left";
		
		internal const int TextColumn = 0;
		internal const int OpenIconColumn = 1;
		internal const int ClosedIconColumn = 2;
		internal const int DataItemColumn = 3;
		internal const int BuilderChainColumn = 4;
		internal const int WeightColumn = 5;
		internal const int FilledColumn = 6;
		
		NodeBuilder[] builders;
		Hashtable builderChains = new Hashtable ();
		Hashtable nodeHash = new Hashtable ();
		Gtk.TreeView tree;
		Gtk.TreeStore store;
		internal Gtk.TreeViewColumn complete_column;
		internal Gtk.CellRendererText text_render;
		TreeBuilderContext builderContext;
		Hashtable callbacks = new Hashtable ();
		
		TreePadOption[] options;
		TreeOptions globalOptions;
		Hashtable nodeOptions = new Hashtable ();
		
		TreeNodeNavigator workNode;
		TreeNodeNavigator compareNode1;
		TreeNodeNavigator compareNode2;
		
		object dragObject;
		object copyObject;
		DragOperation currentTransferOperation;

		Gtk.Frame contentPanel;

		private static Gtk.TargetEntry [] target_table = new Gtk.TargetEntry [] {
			new Gtk.TargetEntry ("text/uri-list", 0, 11 ),
			new Gtk.TargetEntry ("text/plain", 0, 22),
			new Gtk.TargetEntry ("application/x-rootwindow-drop", 0, 33)
		};
	
		public string Id {
			get { return id; }
			set { id = value; }
		}
		
		public string DefaultPlacement {
			get { return defaultPosition; }
			set { defaultPosition = value; }
		}

		public virtual Gtk.Widget Control {
			get {
				return contentPanel;
			}
		}

		public string Title {
			get { return title; }
			set { title = value; }
		}

		public string Icon {
			get { return icon; }
			set { icon = value; }
		}

		public void RedrawContent()
		{
		}
		
		public TreeViewPad ()
		{
		}
		
		public TreeViewPad (string label, string icon, NodeBuilder[] builders, TreePadOption[] options)
		{
			Initialize (label, icon, builders, options);
		}
		
		public virtual void Initialize (string label, string icon, NodeBuilder[] builders, TreePadOption[] options)
		{
			// Create default options
			
			this.options = options;
			globalOptions = new TreeOptions ();
			foreach (TreePadOption op in options)
				globalOptions [op.Id] = op.DefaultValue;
				
			globalOptions.Pad = this;
			
			// Check that there is only one node builder per type
			
			Hashtable bc = new Hashtable ();
			foreach (NodeBuilder nb in builders) {
				TypeNodeBuilder tnb = nb as TypeNodeBuilder;
				if (tnb != null) {
					TypeNodeBuilder other = (TypeNodeBuilder) bc [tnb.NodeDataType];
					if (other != null)
						throw new ApplicationException (string.Format ("The type node builder {0} can't be used in this context because the type {1} is already handled by {2}", nb.GetType(), tnb.NodeDataType, other.GetType()));
					bc [tnb.NodeDataType] = tnb;
				}
				else if (!(nb is NodeBuilderExtension))
					throw new InvalidOperationException (string.Format ("Invalid NodeBuilder type: {0}. NodeBuilders must inherit either from TypeNodeBuilder or NodeBuilderExtension", nb.GetType()));
			}
			
			NodeBuilders = builders;
			Title = label;
			Icon = icon;

			builderContext = new TreeBuilderContext (this);
			
			tree = new Gtk.TreeView ();
			
			/*
			0 -- Text
			1 -- Icon (Open)
			2 -- Icon (Closed)
			3 -- Node Data
			4 -- Builder chain
			5 -- Pango weight
			6 -- Expanded
			*/
			store = new Gtk.TreeStore (typeof (string), typeof (Gdk.Pixbuf), typeof (Gdk.Pixbuf), typeof (object), typeof (object), typeof(int), typeof(bool));
			tree.Model = store;

			tree.EnableModelDragDest (target_table, Gdk.DragAction.Copy | Gdk.DragAction.Move);
			Gtk.Drag.SourceSet (tree, Gdk.ModifierType.Button1Mask, target_table, Gdk.DragAction.Copy | Gdk.DragAction.Move);

			store.DefaultSortFunc = new Gtk.TreeIterCompareFunc (CompareNodes);
			store.SetSortColumnId (/* GTK_TREE_SORTABLE_DEFAULT_SORT_COLUMN_ID */ -1, Gtk.SortType.Ascending);
			
			tree.HeadersVisible = false;
			tree.SearchColumn = 0;
			tree.EnableSearch = true;
			complete_column = new Gtk.TreeViewColumn ();
			complete_column.Title = "column";

			Gtk.CellRendererPixbuf pix_render = new Gtk.CellRendererPixbuf ();
			complete_column.PackStart (pix_render, false);
			complete_column.AddAttribute (pix_render, "pixbuf", OpenIconColumn);
			complete_column.AddAttribute (pix_render, "pixbuf-expander-open", OpenIconColumn);
			complete_column.AddAttribute (pix_render, "pixbuf-expander-closed", ClosedIconColumn);

			text_render = new Gtk.CellRendererText ();
			text_render.Edited += new Gtk.EditedHandler (HandleOnEdit);
			
			complete_column.PackStart (text_render, true);
			complete_column.AddAttribute (text_render, "text", TextColumn);
			complete_column.AddAttribute (text_render, "weight", WeightColumn);
	
			tree.AppendColumn (complete_column);
			
			Gtk.ScrolledWindow sw = new Gtk.ScrolledWindow ();
			sw.Add(tree);
			contentPanel = new Gtk.Frame ();
			contentPanel.Add(sw);
			
			tree.TestExpandRow += new Gtk.TestExpandRowHandler (OnTestExpandRow);
			tree.RowActivated += new Gtk.RowActivatedHandler(OnNodeActivated);
			
			contentPanel.ButtonReleaseEvent += new Gtk.ButtonReleaseEventHandler(OnButtonRelease);
			contentPanel.PopupMenu += new Gtk.PopupMenuHandler (OnPopupMenu);
			
			foreach (NodeBuilder nb in builders)
				nb.SetContext (builderContext);
			
			workNode = new TreeNodeNavigator (this);
			compareNode1 = new TreeNodeNavigator (this);
			compareNode2 = new TreeNodeNavigator (this);
			
			tree.DragBegin += new Gtk.DragBeginHandler (OnDragBegin);
			tree.DragDataReceived += new Gtk.DragDataReceivedHandler (OnDragDataReceived);
			tree.DragDrop += new Gtk.DragDropHandler (OnDragDrop);
			tree.DragEnd += new Gtk.DragEndHandler (OnDragEnd);
			tree.DragMotion += new Gtk.DragMotionHandler (OnDragMotion);
			
			tree.CursorChanged += new EventHandler (OnSelectionChanged);
			
			contentPanel.ShowAll ();
		}

		void OnDragBegin (object o, Gtk.DragBeginArgs arg)
		{
			ITreeNavigator nav = GetSelectedNode ();
			if (nav == null) return;
			dragObject = nav.DataItem;
			Gdk.Pixbuf px = (Gdk.Pixbuf) store.GetValue (nav.CurrentPosition._iter, OpenIconColumn);
			Gtk.Drag.SetIconPixbuf (arg.Context, px, -10, -10);
		}
		
		void OnDragDataReceived (object o, Gtk.DragDataReceivedArgs args)
		{
			if (dragObject != null) {
				bool res = CheckAndDrop (args.X, args.Y, true, args.Context, dragObject);
				Gtk.Drag.Finish (args.Context, res, true, args.Time);
			} else {
				bool res = CheckAndDrop (args.X, args.Y, true, args.Context, args.SelectionData);
//				string fullData = System.Text.Encoding.UTF8.GetString (args.SelectionData.Data);
				Gtk.Drag.Finish (args.Context, res, true, args.Time);
			}
		}
		
		void OnDragDrop (object o, Gtk.DragDropArgs args)
		{
			if (dragObject != null) {
				bool res = CheckAndDrop (args.X, args.Y, true, args.Context, dragObject);
				Gtk.Drag.Finish (args.Context, res, true, args.Time);
			}
		}
		
		void OnDragEnd (object o, Gtk.DragEndArgs args)
		{
			dragObject = null;
		}
		
		[GLib.ConnectBefore]
		void OnDragMotion (object o, Gtk.DragMotionArgs args)
		{
			if (dragObject != null) {
				if (!CheckAndDrop (args.X, args.Y, false, args.Context, dragObject)) {
					Gdk.Drag.Status (args.Context, (Gdk.DragAction)0, args.Time);
					args.RetVal = true;
				}
			}
		}
		
		bool CheckAndDrop (int x, int y, bool drop, Gdk.DragContext ctx, object obj)
		{
			Gtk.TreePath path;
			Gtk.TreeViewDropPosition pos;
			if (!tree.GetDestRowAtPos (x, y, out path, out pos)) return false;
			
			Gtk.TreeIter iter;
			if (!store.GetIter (out iter, path)) return false;
			
			TreeNodeNavigator nav = new TreeNodeNavigator (this, iter);
			NodeBuilder[] chain = nav.BuilderChain;
			bool foundHandler = false;
			
			DragOperation oper = ctx.Action == Gdk.DragAction.Copy ? DragOperation.Copy : DragOperation.Move;
			
			foreach (NodeBuilder nb in chain) {
				NodeCommandHandler handler = nb.CommandHandler;
				handler.SetCurrentNode (nav);
				if (handler.CanDropNode (obj, oper)) {
					foundHandler = true;
					if (drop)
						handler.OnNodeDrop (obj, oper);
				}
			}
			return foundHandler;
		}


		public virtual void Dispose ()
		{
			Clear ();
			foreach (NodeBuilder nb in builders)
				nb.Dispose ();
		}
		
		protected NodeBuilder[] NodeBuilders {
			get { return builders; }
			set { builders = value; }
		}
		
		protected void LoadTree (object nodeObject)
		{
			Clear ();
			TreeBuilder builder = new TreeBuilder (this);
			builder.AddChild (nodeObject, true);
			builder.Expanded = true;
		}
		
		protected void Clear ()
		{
			copyObject = dragObject = null;
			
			object[] obs = new object [nodeHash.Count];
			nodeHash.Keys.CopyTo (obs, 0);
			
			foreach (object dataObject in obs) {
				NodeBuilder[] chain = GetBuilderChain (dataObject.GetType());
				foreach (NodeBuilder nb in chain)
					nb.OnNodeRemoved (dataObject);
			}

			nodeHash = new Hashtable ();
			nodeOptions = new Hashtable ();
			store.Clear ();
		}
		
		public ITreeNavigator GetSelectedNode ()
		{
			Gtk.TreeModel foo;
			Gtk.TreeIter iter;
			if (!tree.Selection.GetSelected (out foo, out iter))
				return null;
			
			return new TreeNodeNavigator (this, iter);
		}
		
		public ITreeNavigator GetNodeAtPosition (NodePosition position)
		{
			return new TreeNodeNavigator (this, position._iter);
		}
		
		public ITreeNavigator GetNodeAtObject (object dataObject)
		{
			return GetNodeAtObject (dataObject, false);
		}
		
		public ITreeNavigator GetNodeAtObject (object dataObject, bool createTreeBranch)
		{
			object it = nodeHash [dataObject];
			if (it == null) {
				if (createTreeBranch) {
					TypeNodeBuilder tnb = GetTypeNodeBuilder (dataObject.GetType());
					if (tnb == null) return null;
					
					object parent = tnb.GetParentObject (dataObject);
					if (parent == null) return null;
					
					ITreeNavigator pnav = GetNodeAtObject (parent, true);
					if (pnav == null) return null;
					
					pnav.MoveToFirstChild ();
					
					// The child should be now in the tree. Try again.
					it = nodeHash [dataObject];
					if (it == null)
						return null;
				} else
					return null;
			}
			
			if (it is Gtk.TreeIter[])
				return new TreeNodeNavigator (this, ((Gtk.TreeIter[])it)[0]);
			else
				return new TreeNodeNavigator (this, (Gtk.TreeIter)it);
		}
		
		public ITreeNavigator GetRootNode ()
		{
			Gtk.TreeIter iter;
			if (!store.GetIterFirst (out iter)) return null;
			return new TreeNodeNavigator (this, iter);
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
		
		object ICommandDelegatorRouter.GetNextCommandTarget ()
		{
			return null;
		}
		
		object ICommandDelegatorRouter.GetDelegatedCommandTarget ()
		{
			TreeNodeNavigator node = (TreeNodeNavigator) GetSelectedNode ();
			if (node != null) {
				NodeBuilder[] chain = node.NodeBuilderChain;
				if (chain.Length > 0) {
					NodeCommandHandler[] handlers = new NodeCommandHandler [chain.Length];
					for (int n=0; n<chain.Length; n++)
						handlers [n] = chain [n].CommandHandler;
					
					for (int n=0; n<handlers.Length; n++) {
						handlers [n].SetCurrentNode (node);
						if (n < chain.Length - 1)
							handlers [n].SetNextTarget (handlers [n+1]);
						else
							handlers [n].SetNextTarget (null);
					}
					return handlers [0];
				}
			}
			return null;
		}

		[CommandHandler (ViewCommands.Open)]
		public virtual void ActivateCurrentItem ()
		{
			TreeNodeNavigator node = (TreeNodeNavigator) GetSelectedNode ();
			if (node != null) {
				NodeBuilder[] chain = node.NodeBuilderChain;
				NodePosition pos = node.CurrentPosition;
				foreach (NodeBuilder b in chain) {
					NodeCommandHandler handler = b.CommandHandler;
					handler.SetCurrentNode (node);
					handler.ActivateItem ();
					node.MoveToPosition (pos);
				}
			}
		}

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
			
			if (copyObject != null) {
				TreeBuilder tb = new TreeBuilder (this);
				if (tb.MoveToObject (copyObject))
					tb.Update ();
			}
		}
		
		[CommandUpdateHandler (EditCommands.Copy)]
		protected void UpdateCopyCurrentItem (CommandInfo info)
		{
			info.Enabled = CanTransferCurrentItem (DragOperation.Copy);
		}

		[CommandUpdateHandler (EditCommands.Cut)]
		protected void UpdateCutCurrentItem (CommandInfo info)
		{
			info.Enabled = CanTransferCurrentItem (DragOperation.Move);
		}
		
		void TransferCurrentItem (DragOperation oper)
		{
			TreeNodeNavigator node = (TreeNodeNavigator) GetSelectedNode ();
			if (node != null) {
				NodeBuilder[] chain = node.NodeBuilderChain;
				NodePosition pos = node.CurrentPosition;
				foreach (NodeBuilder b in chain) {
					NodeCommandHandler handler = b.CommandHandler;
					handler.SetCurrentNode (node);
					if ((handler.CanDragNode () & oper) != 0) {
						node.MoveToPosition (pos);
						copyObject = node.DataItem;
						currentTransferOperation = oper;
						break;
					}
					node.MoveToPosition (pos);
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
					NodeCommandHandler handler = b.CommandHandler;
					handler.SetCurrentNode (node);
					if ((handler.CanDragNode () & oper) != 0)
						return true;
					node.MoveToPosition (pos);
				}
			}
			return false;
		}
		
		[CommandHandler (EditCommands.Paste)]
		public void PasteToCurrentItem ()
		{
			if (copyObject == null) return;
			
			TreeNodeNavigator node = (TreeNodeNavigator) GetSelectedNode ();
			if (node != null) {
				NodeBuilder[] chain = node.NodeBuilderChain;
				NodePosition pos = node.CurrentPosition;
				foreach (NodeBuilder b in chain) {
					NodeCommandHandler handler = b.CommandHandler;
					handler.SetCurrentNode (node);
					if (handler.CanDropNode (copyObject, currentTransferOperation)) {
						node.MoveToPosition (pos);
						handler.OnNodeDrop (copyObject, currentTransferOperation);
					}
					node.MoveToPosition (pos);
				}
			}
			CancelTransfer ();
		}

		[CommandUpdateHandler (EditCommands.Paste)]
		protected void UpdatePasteToCurrentItem (CommandInfo info)
		{
			if (copyObject != null) {
				TreeNodeNavigator node = (TreeNodeNavigator) GetSelectedNode ();
				if (node != null) {
					NodeBuilder[] chain = node.NodeBuilderChain;
					NodePosition pos = node.CurrentPosition;
					foreach (NodeBuilder b in chain) {
						NodeCommandHandler handler = b.CommandHandler;
						handler.SetCurrentNode (node);
						if (handler.CanDropNode (copyObject, currentTransferOperation)) {
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
			if (copyObject != null) {
				object oldCopyObject = copyObject;
				copyObject = null;
				if (currentTransferOperation == DragOperation.Move) {
					TreeBuilder tb = new TreeBuilder (this);
					if (tb.MoveToObject (oldCopyObject))
						tb.Update ();
				}
			}
		}

		/// <summary>
		/// If you want to edit a node label. Select the node you want to edit and then
		/// call this method, instead of using the LabelEdit Property and the BeginEdit
		/// Method directly.
		/// </summary>
		[CommandHandler (EditCommands.Rename)]
		public void StartLabelEdit()
		{
			Gtk.TreeModel foo;
			Gtk.TreeIter iter;
			if (!tree.Selection.GetSelected (out foo, out iter))
				return;
			
			workNode.MoveToIter (iter);
			workNode.ExpandToNode (); //make sure the parent of the node that is being edited is expanded
			store.SetValue (iter, TreeViewPad.TextColumn, workNode.NodeName);
			
			text_render.Editable = true;
			tree.SetCursor (store.GetPath (iter), complete_column, true);
		}

		void HandleOnEdit (object o, Gtk.EditedArgs e)
		{
			text_render.Editable = false;
			Gtk.TreeIter iter;
			if (!store.GetIterFromString (out iter, e.Path))
				throw new Exception("Error calculating iter for path " + e.Path);

			if (e.NewText == null || e.NewText.Length == 0) {
				return;
			}

			ITreeNavigator nav = new TreeNodeNavigator (this, iter);
			NodePosition pos = nav.CurrentPosition;

			NodeBuilder[] chain = (NodeBuilder[]) store.GetValue (iter, BuilderChainColumn);
			foreach (NodeBuilder b in chain) {
				NodeCommandHandler handler = b.CommandHandler;
				handler.SetCurrentNode (nav);
				handler.RenameItem (e.NewText);
				nav.MoveToPosition (pos);
			}
		}
		
		public void SaveTreeState (XmlElement el)
		{
			ITreeNavigator root = GetRootNode ();
			if (root == null) return;

			NodeState state = root.SaveState ();
			XmlElement child = state.ToXml (el.OwnerDocument);
			el.AppendChild (child);
		}
		
		public void RestoreTreeState (XmlElement parent)
		{
			ITreeNavigator nav = GetRootNode ();
			XmlElement rootNode = parent ["Node"];
			NodeState state = NodeState.FromXml (rootNode);
			nav.RestoreState (state);
		}
		
		TypeNodeBuilder GetTypeNodeBuilder (Type type)
		{
			NodeBuilder[] chain = GetBuilderChain (type);
			if (chain == null) return null;
			return (TypeNodeBuilder) chain [0];
		}
		
		NodeBuilder[] GetBuilderChain (Type type)
		{
			NodeBuilder[] chain = builderChains [type] as NodeBuilder[];
			if (chain == null)
			{
				ArrayList list = new ArrayList ();
				foreach (NodeBuilder nb in builders) {
					if (nb is TypeNodeBuilder) {
						if (((TypeNodeBuilder)nb).NodeDataType.IsAssignableFrom (type))
							list.Insert (0, nb);
					}
					else if (((NodeBuilderExtension)nb).CanBuildNode (type))
						list.Add (nb);
				}
				
				chain = (NodeBuilder[]) list.ToArray (typeof(NodeBuilder));
				
				if (chain.Length == 0 || !(chain[0] is TypeNodeBuilder))
					chain = null;

				builderChains [type] = chain;
			}
			return chain;
		}
		
		TypeNodeBuilder GetTypeNodeBuilder (Gtk.TreeIter iter)
		{
			NodeBuilder[] chain = (NodeBuilder[]) store.GetValue (iter, TreeViewPad.BuilderChainColumn);
			if (chain != null && chain.Length > 0)
				return chain[0] as TypeNodeBuilder;
			return null;
		}
		
		int CompareNodes (Gtk.TreeModel model, Gtk.TreeIter a, Gtk.TreeIter b)
		{
			NodeBuilder[] chain1 = (NodeBuilder[]) store.GetValue (a, BuilderChainColumn);
			if (chain1 == null) return 0;
			
			compareNode1.MoveToIter (a);
			compareNode2.MoveToIter (b);

			TypeNodeBuilder tb1 = (TypeNodeBuilder) chain1[0];
			int sort = tb1.CompareObjects (compareNode1, compareNode2);
			if (sort != TypeNodeBuilder.DefaultSort) return sort;
			
			NodeBuilder[] chain2 = (NodeBuilder[]) store.GetValue (b, BuilderChainColumn);
			if (chain2 == null) return 0;
			
			TypeNodeBuilder tb2 = (TypeNodeBuilder) chain2[0];
			
			if (chain1 != chain2) {
				sort = tb2.CompareObjects (compareNode2, compareNode1);
				if (sort != TypeNodeBuilder.DefaultSort) return sort * -1;
			}
			
			object o1 = store.GetValue (a, DataItemColumn);
			object o2 = store.GetValue (b, DataItemColumn);
			return string.Compare (tb1.GetNodeName (compareNode1, o1), tb2.GetNodeName (compareNode2, o2), true);
		}
		
		internal bool GetFirstNode (object dataObject, out Gtk.TreeIter iter)
		{
			object it = nodeHash [dataObject];
			if (it == null) {
				iter = Gtk.TreeIter.Zero;
				return false;
			}
			else if (it is Gtk.TreeIter)
				iter = (Gtk.TreeIter) it;
			else
				iter = ((Gtk.TreeIter[])it)[0];
			return true;
		}
		
		internal void RegisterNode (Gtk.TreeIter it, object dataObject, NodeBuilder[] chain)
		{
			object currentIt = nodeHash [dataObject];
			if (currentIt == null) {
				nodeHash [dataObject] = it;
				if (chain == null) chain = GetBuilderChain (dataObject.GetType());
				foreach (NodeBuilder nb in chain)
					nb.OnNodeAdded (dataObject);
			} else {
				if (currentIt is Gtk.TreeIter[]) {
					Gtk.TreeIter[] arr = (Gtk.TreeIter[]) currentIt;
					Gtk.TreeIter[] newArr = new Gtk.TreeIter [arr.Length + 1];
					arr.CopyTo (newArr, 0);
					newArr [arr.Length] = it;
					nodeHash [dataObject] = newArr;
				} else {
					nodeHash [dataObject] = new Gtk.TreeIter [] { it, (Gtk.TreeIter) currentIt};
				}
			}
		}
		
		internal void UnregisterNode (object dataObject, Gtk.TreeIter iter, NodeBuilder[] chain)
		{
			if (dataObject == copyObject)
				copyObject = null;
				
			nodeOptions.Remove (iter);
			object currentIt = nodeHash [dataObject];
			if (currentIt is Gtk.TreeIter[]) {
				Gtk.TreeIter[] arr = (Gtk.TreeIter[]) currentIt;
				int i = Array.IndexOf (arr, iter);
				if (arr.Length > 2) {
					Gtk.TreeIter[] newArr = new Gtk.TreeIter[arr.Length - 1];
					Array.Copy (arr, 0, newArr, 0, i);
					if (i < newArr.Length)
						Array.Copy (arr, i+1, newArr, i, arr.Length - i - 1);
					nodeHash [dataObject] = newArr;
				} else {
					if (i == 0) nodeHash [dataObject] = arr[1];
					else nodeHash [dataObject] = arr[0];
				}
			} else {
				nodeHash.Remove (dataObject);
				if (chain == null) chain = GetBuilderChain (dataObject.GetType());
				foreach (NodeBuilder nb in chain)
					nb.OnNodeRemoved (dataObject);
			}
		}
		
		internal bool IsRegistered (object dataObject)
		{
			return nodeHash.Contains (dataObject);
		}
		
		internal void NotifyInserted (Gtk.TreeIter it, object dataObject)
		{
			if (callbacks.Count > 0) {
				ArrayList list = callbacks [dataObject] as ArrayList;
				if (list != null) {
					ITreeNavigator nav = new TreeNodeNavigator (this, it);
					NodePosition pos = nav.CurrentPosition;
					foreach (TreeNodeCallback callback in list) {
						callback (nav);
						nav.MoveToPosition (pos);
					}
					callbacks.Remove (dataObject);
				}
			}
		}
		
		TreeOptions GetOptions (Gtk.TreeIter iter, bool createSpecificOptions)
		{
			if (nodeOptions.Count == 0) {
				if (createSpecificOptions) {
					TreeOptions ops = globalOptions.CloneOptions (iter);
					nodeOptions [iter] = ops;
					return ops;
				}
				else
					return globalOptions;
			}
			
			TreeOptions result = null;
			Gtk.TreeIter it = iter;
			do {
				result = nodeOptions [it] as TreeOptions;
			} while (result == null && store.IterParent (out it, it));

			if (result == null)
				result = globalOptions;
			
			if (createSpecificOptions && !it.Equals (iter)) {
				TreeOptions ops = result.CloneOptions (iter);
				nodeOptions [iter] = ops;
				return ops;
			} else
				return result;
		}
		
		internal void ClearOptions (Gtk.TreeIter iter)
		{
			if (nodeOptions.Count == 0)
				return;
				
			ArrayList toDelete = new ArrayList ();
			string path = store.GetPath (iter).ToString () + ":";
			
			foreach (Gtk.TreeIter nit in nodeOptions.Keys) {
				string npath = store.GetPath (nit).ToString () + ":";
				if (npath.StartsWith (path))
					toDelete.Add (nit);
			}

			foreach (object ob in toDelete)
				nodeOptions.Remove (ob);
		}
		
		internal TreeOptions GetIterOptions (Gtk.TreeIter iter)
		{
			return nodeOptions [iter] as TreeOptions;
		}

		internal void SetIterOptions (Gtk.TreeIter iter, TreeOptions ops)
		{
			ops.Pad = this;
			ops.Iter = iter;
			nodeOptions [iter] = ops;
		}

		internal string GetNamePathFromIter (Gtk.TreeIter iter)
		{
			workNode.MoveToIter (iter);
			StringBuilder sb = new StringBuilder ();
			do {
				string name = workNode.NodeName;
				if (sb.Length > 0) sb.Insert (0, '/');
				name = name.Replace ("%","%%");
				name = name.Replace ("/","_%_");
				sb.Insert (0, name);
			} while (workNode.MoveToParent ());

			return sb.ToString ();
		}
		
		internal void RefreshNode (Gtk.TreeIter iter)
		{
			ITreeBuilder builder = new TreeBuilder (this, iter);
			builder.UpdateAll ();
		}
		
		internal void ResetState (Gtk.TreeIter iter)
		{
			TreeBuilder builder = new TreeBuilder (this, iter);
			builder.ResetState ();
		}
		
		internal bool GetIterFromNamePath (string path, out Gtk.TreeIter iter)
		{
			if (!store.GetIterFirst (out iter))
				return false;
				
			TreeNodeNavigator nav = new TreeNodeNavigator (this, iter);
			string[] names = path.Split ('/');

			int n = 0;
			bool more;
			do {
				string name = names [n].Replace ("_%_","/");
				name = name.Replace ("%%","%");
				
				if (nav.NodeName == name) {
					iter = nav.CurrentPosition._iter;
					if (++n == names.Length) return true;
					more = nav.MoveToFirstChild ();
				} else
					more = nav.MoveNext ();
			} while (more);

			return false;
		}

		public void StealFocus ()
		{
			GLib.Timeout.Add (20, new GLib.TimeoutHandler (wantFocus));
		}
		bool wantFocus ()
		{
			tree.GrabFocus ();
			StartLabelEdit ();
			return false;
		}

		private void OnTestExpandRow (object sender, Gtk.TestExpandRowArgs args)
		{
			bool filled = (bool) store.GetValue (args.Iter, FilledColumn);
			if (!filled) {
				TreeBuilder nb = new TreeBuilder (this, args.Iter);
				args.RetVal = !nb.FillNode ();
			} else
				args.RetVal = false;
		}

		void ShowPopup ()
		{
			ITreeNavigator tnav = GetSelectedNode ();
			TypeNodeBuilder nb = GetTypeNodeBuilder (tnav.CurrentPosition._iter);
			if (nb == null || nb.ContextMenuAddinPath == null) {
				if (options.Length > 0) {
					CommandEntrySet opset = new CommandEntrySet ();
					opset.AddItem (ViewCommands.TreeDisplayOptionList);
					opset.AddItem (Command.Separator);
					opset.AddItem (ViewCommands.ResetTreeDisplayOptions);
					Runtime.Gui.CommandService.ShowContextMenu (opset);
				}
			} else {
				CommandEntrySet eset = Runtime.Gui.CommandService.CreateCommandEntrySet (nb.ContextMenuAddinPath);
				eset.AddItem (Command.Separator);
				CommandEntrySet opset = eset.AddItemSet (GettextCatalog.GetString ("Display Options"));
				opset.AddItem (ViewCommands.TreeDisplayOptionList);
				opset.AddItem (Command.Separator);
				opset.AddItem (ViewCommands.ResetTreeDisplayOptions);
				Runtime.Gui.CommandService.ShowContextMenu (eset);
			}
		}
		
		[CommandUpdateHandler (ViewCommands.TreeDisplayOptionList)]
		protected void BuildTreeOptionsMenu (CommandArrayInfo info)
		{
			ITreeNavigator tnav = GetSelectedNode ();
			ITreeOptions currentOptions = tnav.Options;
			foreach (TreePadOption op in options) {
				CommandInfo ci = new CommandInfo (op.Label);
				ci.Checked = currentOptions [op.Id];
				info.Add (ci, op.Id);
			}
		}
		
		[CommandHandler (ViewCommands.TreeDisplayOptionList)]
		protected void OptionToggled (string optionId)
		{
			Gtk.TreeModel foo;
			Gtk.TreeIter iter;
			if (!tree.Selection.GetSelected (out foo, out iter))
				return;

			TreeOptions ops = GetOptions (iter, true);
			ops [optionId] = !ops [optionId];
		}
		
		[CommandHandler (ViewCommands.ResetTreeDisplayOptions)]
		protected void ResetOptions ()
		{
			Gtk.TreeModel foo;
			Gtk.TreeIter iter;
			if (!tree.Selection.GetSelected (out foo, out iter))
				return;

			ClearOptions (iter);
			TreeBuilder tb = new TreeBuilder (this, iter);
			tb.UpdateAll ();
		}

		void OnPopupMenu (object o, Gtk.PopupMenuArgs args)
		{
			if (GetSelectedNode () != null)
				ShowPopup ();
		}

		private void OnButtonRelease(object sender, Gtk.ButtonReleaseEventArgs args)
		{
			if (args.Event.Button == 3 && GetSelectedNode() != null) {
				ShowPopup ();
			}
		}

		protected virtual void OnNodeActivated (object sender, Gtk.RowActivatedArgs args)
		{
			ActivateCurrentItem ();
		}
		
		protected virtual void OnSelectionChanged (object sender, EventArgs args)
		{
			TreeNodeNavigator node = (TreeNodeNavigator) GetSelectedNode ();
			if (node != null) {
				NodeBuilder[] chain = node.NodeBuilderChain;
				NodePosition pos = node.CurrentPosition;
				foreach (NodeBuilder b in chain) {
					NodeCommandHandler handler = b.CommandHandler;
					handler.SetCurrentNode (node);
					handler.OnItemSelected ();
					node.MoveToPosition (pos);
				}
			}
		}
		
		public IXmlConvertable CreateMemento ()
		{
			return new TreeViewPadMemento (this);
		}

		public void SetMemento (IXmlConvertable memento)
		{
			((TreeViewPadMemento)memento).Restore (this);
		}

		// ********* Own events
		protected virtual void OnTitleChanged(EventArgs e)
		{
			if (TitleChanged != null) {
				TitleChanged(this, e);
			}
		}

		protected virtual void OnIconChanged(EventArgs e)
		{
			if (IconChanged != null) {
				IconChanged(this, e);
			}
		}

		public event EventHandler TitleChanged;
		public event EventHandler IconChanged;

		class PadCheckMenuItem: Gtk.CheckMenuItem
		{
			internal string Id;
			
			public PadCheckMenuItem (string label, string id): base (label) {
				Id = id;
			}
		}

		class TreeBuilderContext: ITreeBuilderContext
		{
			TreeViewPad pad;
			Hashtable icons = new Hashtable ();
			Hashtable composedIcons = new Hashtable ();
			
			internal TreeBuilderContext (TreeViewPad pad)
			{
				this.pad = pad;
			}
			
			public ITreeBuilder GetTreeBuilder ()
			{
				Gtk.TreeIter iter;
				if (!pad.store.GetIterFirst (out iter))
					return new TreeBuilder (pad, Gtk.TreeIter.Zero);
				else
					return new TreeBuilder (pad, iter);
			}
			
			public ITreeBuilder GetTreeBuilder (object dataObject)
			{
				Gtk.TreeIter iter;
				if (!pad.GetFirstNode (dataObject, out iter)) return null;
				return new TreeBuilder (pad, iter);
			}
			
			public ITreeBuilder GetTreeBuilder (ITreeNavigator navigator)
			{
				return new TreeBuilder (pad, navigator.CurrentPosition._iter);
			}
		
			public Gdk.Pixbuf GetIcon (string id)
			{
				Gdk.Pixbuf icon = icons [id] as Gdk.Pixbuf;
				if (icon == null) {
					icon = pad.tree.RenderIcon (id, Gtk.IconSize.Menu, "");
					icons [id] = icon;
				}
				return icon;
			}
			
			public Gdk.Pixbuf GetComposedIcon (Gdk.Pixbuf baseIcon, object compositionKey)
			{
				Hashtable itable = composedIcons [baseIcon] as Hashtable;
				if (itable == null) return null;
				return itable [compositionKey] as Gdk.Pixbuf;
			}
			
			public void CacheComposedIcon (Gdk.Pixbuf baseIcon, object compositionKey, Gdk.Pixbuf composedIcon)
			{
				Hashtable itable = composedIcons [baseIcon] as Hashtable;
				if (itable == null) {
					itable = new Hashtable ();
					composedIcons [baseIcon] = itable;
				}
				itable [compositionKey] = composedIcon;
			}
			
			public ITreeNavigator GetTreeNavigator (object dataObject)
			{
				Gtk.TreeIter iter;
				if (!pad.GetFirstNode (dataObject, out iter)) return null;
				return new TreeNodeNavigator (pad, iter);
			}
			
			public TreeViewPad Tree {
				get { return pad; }
			}
		}
		
		
		class TreeNodeNavigator: ITreeNavigator, ITreeOptions
		{
			protected TreeViewPad pad;
			protected Gtk.TreeView tree;
			protected Gtk.TreeStore store;
			protected Gtk.TreeIter currentIter;
			
			public TreeNodeNavigator (TreeViewPad pad): this (pad, Gtk.TreeIter.Zero)
			{
			}
			
			public TreeNodeNavigator (TreeViewPad pad, Gtk.TreeIter iter)
			{
				this.pad = pad;
				tree = pad.tree;
				store = pad.store;
				currentIter = iter;
			}
			
			public ITreeNavigator Clone ()
			{
				return new TreeNodeNavigator (pad, currentIter);
			}

			
			public object DataItem {
				get { return store.GetValue (currentIter, TreeViewPad.DataItemColumn); }
			}
			
			internal NodeBuilder[] NodeBuilderChain {
				get { return (NodeBuilder[]) store.GetValue (currentIter, TreeViewPad.BuilderChainColumn); }
			}
			
			public bool Selected {
				get {
					return tree.Selection.IterIsSelected (currentIter);
				}
				set {
					if (value != Selected) {
						Gtk.TreeIter parent = currentIter;
						while (store.IterParent (out parent, parent)) {
							Gtk.TreePath path = store.GetPath (parent);
							tree.ExpandRow (path, false);
						}
				
						tree.Selection.SelectIter (currentIter);
						tree.SetCursor (store.GetPath (currentIter), pad.complete_column, false);
//						tree.ScrollToCell (store.GetPath (currentIter), null, false, 0, 0);
					}
				}
			}
			
			public NodePosition CurrentPosition {
				get {
					NodePosition pos = new NodePosition ();
					pos._iter = currentIter;
					return pos;
				}
			}
		
			bool ITreeOptions.this [string name] {
				get { return pad.GetOptions (currentIter, false) [name]; }
				set { pad.GetOptions (currentIter, true) [name] = value; }
			}
			
			public bool MoveToPosition (NodePosition position)
			{
				currentIter = (Gtk.TreeIter) position._iter;
				return true;
			}
			
			internal void MoveToIter (Gtk.TreeIter iter)
			{
				currentIter = iter;
			}
			
			public bool MoveToRoot ()
			{
				return store.GetIterFirst (out currentIter);
			}
		
			public bool MoveToObject (object dataObject)
			{
				Gtk.TreeIter iter;
				if (!pad.GetFirstNode (dataObject, out iter)) return false;
				currentIter = iter;
				return true;
			}
		
			public bool MoveToParent ()
			{
				return store.IterParent (out currentIter, currentIter);
			}
			
			public bool MoveToParent (Type dataType)
			{
				Gtk.TreeIter newIter = currentIter;
				while (store.IterParent (out newIter, newIter)) {
					object data = store.GetValue (newIter, TreeViewPad.DataItemColumn);
					if (dataType.IsInstanceOfType (data)) {
						currentIter = newIter;
						return true;
					}
				}
				return false;
			}
			
			public bool MoveToFirstChild ()
			{
				EnsureFilled ();
				Gtk.TreeIter it;
				if (!store.IterChildren (out it, currentIter))
					return false;
				
				currentIter = it;
				return true;
			}
			
			public bool MoveNext ()
			{
				return store.IterNext (ref currentIter);
			}
			
			public bool HasChild (string name, Type dataType)
			{
				if (MoveToChild (name, dataType)) {
					MoveToParent ();
					return true;
				} else
					return false;
			}
			
			public bool HasChildren ()
			{
				EnsureFilled ();
				Gtk.TreeIter it;
				return store.IterChildren (out it, currentIter);
			}
		
			public bool FindChild (object dataObject)
			{
				return FindChild (dataObject, false);
			}
			
			public bool FindChild (object dataObject, bool recursive)
			{
				object it = pad.nodeHash [dataObject];
				
				if (it == null)
					return false;
				else if (it is Gtk.TreeIter) {
					if (IsChildIter (currentIter, (Gtk.TreeIter)it, recursive)) {
						currentIter = (Gtk.TreeIter)it;
						return true;
					} else
						return false;
				} else {
					foreach (Gtk.TreeIter cit in (Gtk.TreeIter[])it) {
						if (IsChildIter (currentIter, cit, recursive)) {
							currentIter = (Gtk.TreeIter)cit;
							return true;
						}
					}
					return false;
				}
			}
			
			bool IsChildIter (Gtk.TreeIter pit, Gtk.TreeIter cit, bool recursive)
			{
				while (store.IterParent (out cit, cit)) {
					if (cit.Equals (pit)) return true;
					if (!recursive) return false;
				}
				return false;
			}
			
			public bool MoveToChild (string name, Type dataType)
			{
				EnsureFilled ();
				Gtk.TreeIter oldIter = currentIter;

				if (!MoveToFirstChild ()) {
					currentIter = oldIter;
					return false;
				}

				do {
					if (name == NodeName) return true;
				} while (MoveNext ());

				currentIter = oldIter;
				return false;
			}
			
			public bool Expanded {
				get { return tree.GetRowExpanded (store.GetPath (currentIter)); }
				set {
					if (value && !Expanded) {
						Gtk.TreePath path = store.GetPath (currentIter);
						tree.ExpandRow (path, false);
					}
					else if (!value && Expanded) {
						Gtk.TreePath path = store.GetPath (currentIter);
						tree.CollapseRow (path);
					}
				}
			}

			public ITreeOptions Options {
				get { return this; }
			}
			
			public NodeState SaveState ()
			{
				return NodeState.SaveState (pad, this);
			}
			
			public void RestoreState (NodeState state)
			{
				NodeState.RestoreState (pad, this, state);
			}
		
			public void Refresh ()
			{
				ITreeBuilder builder = new TreeBuilder (pad, currentIter);
				builder.UpdateAll ();
			}
			
			public void ExpandToNode ()
			{
				Gtk.TreeIter it;
				if (store.IterParent (out it, currentIter)) {
					Gtk.TreePath path = store.GetPath (it);
					tree.ExpandToPath (path);
				}
			}
			
			public string NodeName {
				get {
					object data = DataItem;
					NodeBuilder[] chain = BuilderChain;
					if (chain != null && chain.Length > 0) return ((TypeNodeBuilder)chain[0]).GetNodeName (this, data);
					else return store.GetValue (currentIter, TreeViewPad.TextColumn) as string;
				}
			}
			
			public NodeBuilder[] BuilderChain {
				get { return (NodeBuilder[]) store.GetValue (currentIter, TreeViewPad.BuilderChainColumn); }
			}
			
			public object GetParentDataItem (Type type, bool includeCurrent)
			{
				if (includeCurrent && type.IsInstanceOfType (DataItem))
					return DataItem;

				Gtk.TreeIter it = currentIter;
				while (store.IterParent (out it, it)) {
					object data = store.GetValue (it, TreeViewPad.DataItemColumn);
					if (type.IsInstanceOfType (data))
						return data;
				}
				return null;
			}
		
			protected virtual void EnsureFilled ()
			{
				if (!(bool) store.GetValue (currentIter, TreeViewPad.FilledColumn))
					new TreeBuilder (pad, currentIter).FillNode ();
			}
			
			public bool Filled {
				get { return (bool) store.GetValue (currentIter, TreeViewPad.FilledColumn); }
			}
		}
		
		
		class TreeBuilder: TreeNodeNavigator, ITreeBuilder
		{
			public TreeBuilder (TreeViewPad pad): base (pad)
			{
			}

			public TreeBuilder (TreeViewPad pad, Gtk.TreeIter iter): base (pad, iter)
			{
			}
			
			protected override void EnsureFilled ()
			{
				if (!(bool) store.GetValue (currentIter, TreeViewPad.FilledColumn))
					FillNode ();
			}
			
			public bool FillNode ()
			{
				store.SetValue (currentIter, TreeViewPad.FilledColumn, true);
				
				Gtk.TreeIter child;
				if (store.IterChildren (out child, currentIter))
					store.Remove (ref child);
				
				NodeBuilder[] chain = (NodeBuilder[]) store.GetValue (currentIter, TreeViewPad.BuilderChainColumn);
				object dataObject = store.GetValue (currentIter, TreeViewPad.DataItemColumn);
				CreateChildren (chain, dataObject);
				return store.IterHasChild (currentIter);
			}
			
			public void UpdateAll ()
			{
				Update ();
				UpdateChildren ();
			}
			
			public void Update ()
			{
				object data = store.GetValue (currentIter, TreeViewPad.DataItemColumn);
				NodeBuilder[] chain = (NodeBuilder[]) store.GetValue (currentIter, TreeViewPad.BuilderChainColumn);
				
				NodeAttributes ats = GetAttributes (chain, data);
				UpdateNode (chain, ats, data);
				
			}
			
			public void ResetState ()
			{
				Update ();
				RemoveChildren (currentIter);

				object data = store.GetValue (currentIter, TreeViewPad.DataItemColumn);
				NodeBuilder[] chain = (NodeBuilder[]) store.GetValue (currentIter, TreeViewPad.BuilderChainColumn);

				if (!HasChildNodes (chain, data))
					FillNode ();
				else {
					RemoveChildren (currentIter);
					store.AppendNode (currentIter);	// Dummy node
					store.SetValue (currentIter, TreeViewPad.FilledColumn, false);
				}
				
			}
			
			public void UpdateChildren ()
			{
				object data = store.GetValue (currentIter, TreeViewPad.DataItemColumn);
				NodeBuilder[] chain = (NodeBuilder[]) store.GetValue (currentIter, TreeViewPad.BuilderChainColumn);
				
				if (!(bool) store.GetValue (currentIter, TreeViewPad.FilledColumn)) {
					if (!HasChildNodes (chain, data))
						FillNode ();
					return;
				}
				
				NodeState ns = SaveState ();
				RestoreState (ns);
			}
			
			void RemoveChildren (Gtk.TreeIter it)
			{
				Gtk.TreeIter child;
				while (store.IterChildren (out child, it)) {
					RemoveChildren (child);
					object childData = store.GetValue (child, TreeViewPad.DataItemColumn);
					if (childData != null)
						pad.UnregisterNode (childData, child, null);
					store.Remove (ref child);
				}
			}
			
			public void Remove ()
			{
				RemoveChildren (currentIter);
				object data = store.GetValue (currentIter, TreeViewPad.DataItemColumn);
				pad.UnregisterNode (data, currentIter, null);
				store.Remove (ref currentIter);
			}
			
			public void Remove (bool moveToParent)
			{
				Gtk.TreeIter parent;
				store.IterParent (out parent, currentIter);

				Remove ();

				if (moveToParent)
					currentIter = parent;
			}
			
			public void AddChild (object dataObject)
			{
				AddChild (dataObject, false);
			}
			
			NodeAttributes GetAttributes (NodeBuilder[] chain, object dataObject)
			{
				Gtk.TreeIter oldIter = currentIter;
				NodeAttributes ats = NodeAttributes.None;
				
				foreach (NodeBuilder nb in chain) {
					nb.GetNodeAttributes (this, dataObject, ref ats);
					currentIter = oldIter;
				}
				return ats;
			}
			
			public void AddChild (object dataObject, bool moveToChild)
			{
				if (dataObject == null) throw new ArgumentNullException ("dataObject");
				
				NodeBuilder[] chain = pad.GetBuilderChain (dataObject.GetType ());
				if (chain == null) return;
				
				Gtk.TreeIter oldIter = currentIter;
				NodeAttributes ats = GetAttributes (chain, dataObject);
				if ((ats & NodeAttributes.Hidden) != 0)
					return;
				
				Gtk.TreeIter it;
				if (!currentIter.Equals (Gtk.TreeIter.Zero)) {
					if (!Filled) return;
					it = store.AppendNode (currentIter);
				}
				else
					it = store.AppendNode ();
				
				pad.RegisterNode (it, dataObject, chain);
				
				BuildNode (it, chain, ats, dataObject);
				if (moveToChild)
					currentIter = it;
				else
					currentIter = oldIter;

				pad.NotifyInserted (it, dataObject);
			}
			
			void BuildNode (Gtk.TreeIter it, NodeBuilder[] chain, NodeAttributes ats, object dataObject)
			{
				Gtk.TreeIter oldIter = currentIter;
				currentIter = it;
				
				// It is *critical* that we set this first. We will
				// sort after this call, so we must give as much info
				// to the sort function as possible.
				store.SetValue (it, TreeViewPad.DataItemColumn, dataObject);
				store.SetValue (it, TreeViewPad.BuilderChainColumn, chain);
				
				UpdateNode (chain, ats, dataObject);
				
				bool hasChildren = HasChildNodes (chain, dataObject);
				store.SetValue (currentIter, TreeViewPad.FilledColumn, !hasChildren);

				if (hasChildren)
					store.AppendNode (currentIter);	// Dummy node

				currentIter = oldIter;
			}
			
			bool HasChildNodes (NodeBuilder[] chain, object dataObject)
			{
				Gtk.TreeIter citer = currentIter;
				foreach (NodeBuilder nb in chain) {
					bool res = nb.HasChildNodes (this, dataObject);
					currentIter = citer;
					if (res) return true;
				}
				return false;
			}
			
			void UpdateNode (NodeBuilder[] chain, NodeAttributes ats, object dataObject)
			{
				Gdk.Pixbuf icon = null;
				Gdk.Pixbuf closedIcon = null;
				string text = string.Empty;
				Gtk.TreeIter citer = currentIter;
				
				foreach (NodeBuilder builder in chain) {
					builder.BuildNode (this, dataObject, ref text, ref icon, ref closedIcon);
					currentIter = citer;
				}
					
				if (closedIcon == null) closedIcon = icon;
				
				if (dataObject == pad.copyObject && pad.currentTransferOperation == DragOperation.Move) {
					Gdk.Pixbuf gicon = pad.builderContext.GetComposedIcon (icon, "fade");
					if (gicon == null) {
						gicon = Runtime.Gui.Icons.MakeTransparent (icon, 0.5);
						pad.builderContext.CacheComposedIcon (icon, "fade", gicon);
					}
					icon = gicon;
					gicon = pad.builderContext.GetComposedIcon (closedIcon, "fade");
					if (gicon == null) {
						gicon = Runtime.Gui.Icons.MakeTransparent (closedIcon, 0.5);
						pad.builderContext.CacheComposedIcon (closedIcon, "fade", gicon);
					}
					closedIcon = gicon;
				}
				
				SetNodeInfo (currentIter, ats, text, icon, closedIcon);
			}
			
			void SetNodeInfo (Gtk.TreeIter it, NodeAttributes ats, string text, Gdk.Pixbuf icon, Gdk.Pixbuf closedIcon)
			{
				store.SetValue (it, TreeViewPad.TextColumn, text);
				if (icon != null) store.SetValue (it, TreeViewPad.OpenIconColumn, icon);
				if (closedIcon != null) store.SetValue (it, TreeViewPad.ClosedIconColumn, closedIcon);
				tree.QueueDraw ();
			}

			void CreateChildren (NodeBuilder[] chain, object dataObject)
			{
				Gtk.TreeIter it = currentIter;
				foreach (NodeBuilder builder in chain) {
					builder.BuildChildNodes (this, dataObject);
					currentIter = it;
				}
			}
		}
		
		internal class TreeOptions: Hashtable, ITreeOptions
		{
			TreeViewPad pad;
			Gtk.TreeIter iter;
			
			public TreeOptions ()
			{
			}
			
			public TreeOptions (TreeViewPad pad, Gtk.TreeIter iter)
			{
				this.pad = pad;
				this.iter = iter;
			}
			
			public TreeViewPad Pad {
				get { return pad; }
				set { pad = value; }
			}

			public Gtk.TreeIter Iter {
				get { return iter; }
				set { iter = value; }
			}

			public bool this [string name] {
				get {
					object op = base [name];
					if (op == null) return false;
					return (bool) op;
				}
				set {
					base [name] = value;
					if (pad != null)
						pad.RefreshNode (iter);
				}
			}
			
			public TreeOptions CloneOptions (Gtk.TreeIter newIter)
			{
				TreeOptions ops = new TreeOptions (pad, newIter);
				foreach (DictionaryEntry de in this)
					ops [de.Key] = de.Value;
				return ops;
			}
		}
	}
	
	public class TreeViewPadMemento : IXmlConvertable
	{
		TreeViewPad treeView = null;
		XmlElement parent = null;
		
		public TreeViewPadMemento()
		{
		}
		
		public TreeViewPadMemento (TreeViewPad treeView)
		{
			this.treeView = treeView;
		}
		
		public void Restore (TreeViewPad view)
		{
			view.RestoreTreeState (parent);
		}
		
		public object FromXmlElement (XmlElement element)
		{
			this.parent = element;
			return this;
		}
		
		public XmlElement ToXmlElement (XmlDocument doc)
		{
			Debug.Assert(treeView != null);
			
			XmlElement treenode  = doc.CreateElement ("TreeView");
			treeView.SaveTreeState (treenode);
			return treenode;
		}
	}
	
	public delegate void TreeNodeCallback (ITreeNavigator nav);
}
