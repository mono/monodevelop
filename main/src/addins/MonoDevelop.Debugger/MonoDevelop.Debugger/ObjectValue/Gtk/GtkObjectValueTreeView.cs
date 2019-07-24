//
// GtkObjectValueTreeView.cs
//
// Author:
//       gregm <gregm@microsoft.com>
//
// Copyright (c) 2019 Microsoft
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using Gdk;
using Gtk;

using Mono.Debugging.Client;

using MonoDevelop.Components;
using MonoDevelop.Core;
using MonoDevelop.Ide;
using MonoDevelop.Ide.CodeCompletion;
using MonoDevelop.Components.Commands;
using MonoDevelop.Ide.Commands;
using MonoDevelop.Ide.Editor.Extension;
using MonoDevelop.Ide.Fonts;

namespace MonoDevelop.Debugger
{
	// TODO: when we remove from store, remove from allNodes
	[System.ComponentModel.ToolboxItem (true)]
	public class GtkObjectValueTreeView : TreeView, ICompletionWidget, IObjectValueTreeView
	{
		static readonly Gtk.TargetEntry [] DropTargets = {
			new Gtk.TargetEntry ("text/plain;charset=utf-8", Gtk.TargetFlags.App, 0)
		};

		readonly ObjectValueTreeViewController controller;

		// mapping of a node to the node's location in the tree view
		readonly Dictionary<ObjectValueNode, TreeRowReference> allNodes = new Dictionary<ObjectValueNode, TreeRowReference> ();

		// keep this lot....
		readonly Xwt.Drawing.Image noLiveIcon;
		readonly Xwt.Drawing.Image liveIcon;

		readonly TreeViewState state;
		readonly TreeStore store;
		readonly string createMsg;
		bool restoringState;
		StackFrame frame;
		bool disposed;

		bool columnsAdjusted;
		bool columnSizesUpdating;
		bool allowStoreColumnSizes;
		double expColWidth;
		double valueColWidth;
		double typeColWidth;

		readonly CellRendererTextWithIcon crtExp;
		readonly ValueCellRenderer crtValue;
		readonly CellRendererText crtType;
		readonly CellRendererRoundedButton crpButton;
		readonly CellRendererImage evaluateStatusCell;
		readonly CellRendererImage crpPin;
		readonly CellRendererImage crpLiveUpdate;
		readonly CellRendererImage crpViewer;
		Entry editEntry;
		Mono.Debugging.Client.CompletionData currentCompletionData;

		readonly TreeViewColumn expCol;
		readonly TreeViewColumn valueCol;
		readonly TreeViewColumn typeCol;
		readonly TreeViewColumn pinCol;

		static readonly CommandEntrySet menuSet;

		const int NameColumn = 0;
		const int ValueColumn = 1;
		const int TypeColumn = 2;
		const int NameEditableColumn = 3;
		const int ValueEditableColumn = 4;
		const int IconColumn = 5;
		const int NameColorColumn = 6;
		const int ValueColorColumn = 7;
		const int ValueButtonVisibleColumn = 8;
		const int PinIconColumn = 9;
		const int LiveUpdateIconColumn = 10;
		const int ViewerButtonVisibleColumn = 11;
		const int PreviewIconColumn = 12;
		const int EvaluateStatusIconColumn = 13;
		const int EvaluateStatusIconVisibleColumn = 14;
		const int ValueButtonTextColumn = 15;
		const int ObjectNodeColumn = 16;

		enum LocalCommands
		{
			AddWatch
		}

		static GtkObjectValueTreeView ()
		{
			// Context menu definition

			menuSet = new CommandEntrySet ();
			menuSet.AddItem (DebugCommands.AddWatch);
			menuSet.AddSeparator ();
			menuSet.AddItem (EditCommands.Copy);
			menuSet.AddItem (EditCommands.Rename);
			menuSet.AddItem (EditCommands.DeleteKey);
		}

		public GtkObjectValueTreeView (ObjectValueTreeViewController controller, bool allowEditing, bool headersVisible)
		{
			// ensure this is set when we set up the view, don't try and refresh just yet
			this.allowEditing = allowEditing;


			this.controller = controller;
			this.controller.PinnedWatchChanged += Controller_PinnedWatchChanged;
			this.controller.ChildrenLoaded += Controller_NodeChildrenLoaded;
			this.controller.EvaluationCompleted += Controller_EvaluationCompleted;

			store = new TreeStore (typeof (string), typeof (string), typeof (string), typeof (bool), typeof (bool), typeof (string), typeof (string), typeof (string), typeof (bool), typeof (string), typeof (Xwt.Drawing.Image), typeof (bool), typeof (string), typeof (Xwt.Drawing.Image), typeof (bool), typeof (string), typeof (ObjectValueNode));
			Model = store;
			SearchColumn = -1; // disable the interactive search
			RulesHint = true;
			HeadersVisible = headersVisible;
			EnableSearch = false;
			Selection.Mode = Gtk.SelectionMode.Multiple;
			Selection.Changed += HandleSelectionChanged;
			ResetColumnSizes ();

			EnableModelDragDest (DropTargets, Gdk.DragAction.Copy);
			DragDataReceived += OnDragDataReceived;

			Pango.FontDescription newFont;

			if (controller.CompactView) {
				newFont = IdeServices.FontService.SansFont.CopyModified (Ide.Gui.Styles.FontScale11);
			} else {
				newFont = IdeServices.FontService.SansFont.CopyModified (Ide.Gui.Styles.FontScale12);
			}

			liveIcon = ImageService.GetIcon ("md-live", IconSize.Menu);
			noLiveIcon = liveIcon.WithAlpha (0.5);

			expCol = new TreeViewColumn ();
			expCol.Title = GettextCatalog.GetString ("Name");
			var crp = new CellRendererImage ();
			expCol.PackStart (crp, false);
			expCol.AddAttribute (crp, "stock_id", IconColumn);
			crtExp = new CellRendererTextWithIcon ();
			crtExp.FontDesc = newFont;
			expCol.PackStart (crtExp, true);
			expCol.AddAttribute (crtExp, "text", NameColumn);
			expCol.AddAttribute (crtExp, "editable", NameEditableColumn);
			expCol.AddAttribute (crtExp, "foreground", NameColorColumn);
			expCol.AddAttribute (crtExp, "icon", PreviewIconColumn);
			expCol.Resizable = true;
			expCol.Sizing = TreeViewColumnSizing.Fixed;
			expCol.MinWidth = 15;
			expCol.AddNotification ("width", OnColumnWidthChanged);
			AppendColumn (expCol);

			valueCol = new TreeViewColumn ();
			valueCol.Title = GettextCatalog.GetString ("Value");
			valueCol.MaxWidth = controller.CompactView ? 800 : int.MaxValue;
			evaluateStatusCell = new CellRendererImage ();
			valueCol.PackStart (evaluateStatusCell, false);
			valueCol.AddAttribute (evaluateStatusCell, "visible", EvaluateStatusIconVisibleColumn);
			valueCol.AddAttribute (evaluateStatusCell, "image", EvaluateStatusIconColumn);
			var crColorPreview = new CellRendererColorPreview ();
			valueCol.PackStart (crColorPreview, false);
			valueCol.SetCellDataFunc (crColorPreview, ValueDataFunc);
			crpButton = new CellRendererRoundedButton ();
			crpButton.FontDesc = newFont;
			valueCol.PackStart (crpButton, false);
			valueCol.AddAttribute (crpButton, "visible", ValueButtonVisibleColumn);
			valueCol.AddAttribute (crpButton, "text", ValueButtonTextColumn);
			crpViewer = new CellRendererImage ();
			if (controller.CompactView)
				crpViewer.Image = ImageService.GetIcon (Stock.Edit).WithSize (12, 12);
			else
				crpViewer.Image = ImageService.GetIcon (Stock.Edit, IconSize.Menu);
			valueCol.PackStart (crpViewer, false);
			valueCol.AddAttribute (crpViewer, "visible", ViewerButtonVisibleColumn);
			crtValue = new ValueCellRenderer ();
			crtValue.Ellipsize = Pango.EllipsizeMode.End;
			crtValue.Compact = controller.CompactView;
			crtValue.FontDesc = newFont;
			valueCol.PackStart (crtValue, true);
			valueCol.AddAttribute (crtValue, "texturl", ValueColumn);
			valueCol.AddAttribute (crtValue, "editable", ValueEditableColumn);
			valueCol.AddAttribute (crtValue, "foreground", ValueColorColumn);
			valueCol.Resizable = true;
			valueCol.MinWidth = 15;
			valueCol.AddNotification ("width", OnColumnWidthChanged);
			//			valueCol.Expand = true;
			valueCol.Sizing = TreeViewColumnSizing.Fixed;
			AppendColumn (valueCol);

			typeCol = new TreeViewColumn ();
			typeCol.Title = GettextCatalog.GetString ("Type");
			typeCol.Visible = !controller.CompactView;
			crtType = new CellRendererText ();
			crtType.FontDesc = newFont;
			typeCol.PackStart (crtType, true);
			typeCol.AddAttribute (crtType, "text", TypeColumn);
			typeCol.Resizable = true;
			typeCol.Sizing = TreeViewColumnSizing.Fixed;
			typeCol.MinWidth = 15;
			typeCol.AddNotification ("width", OnColumnWidthChanged);
			//			typeCol.Expand = true;
			AppendColumn (typeCol);

			pinCol = new TreeViewColumn ();
			crpPin = new CellRendererImage ();
			pinCol.PackStart (crpPin, false);
			pinCol.AddAttribute (crpPin, "stock_id", PinIconColumn);
			crpLiveUpdate = new CellRendererImage ();
			pinCol.PackStart (crpLiveUpdate, false);
			pinCol.AddAttribute (crpLiveUpdate, "image", LiveUpdateIconColumn);
			pinCol.Resizable = false;
			pinCol.Visible = controller.AllowPinning;
			pinCol.Expand = false;
			pinCol.Sizing = TreeViewColumnSizing.Fixed;
			pinCol.FixedWidth = 16;
			AppendColumn (pinCol);

			state = new TreeViewState (this, NameColumn);

			crtExp.Edited += OnExpressionEdited;
			crtExp.EditingStarted += OnExpressionStartedEditing;
			crtExp.EditingCanceled += OnEditingCancelled;
			crtValue.EditingStarted += OnValueEditing;
			crtValue.Edited += OnValueEdited;
			crtValue.EditingCanceled += OnEditingCancelled;

			createMsg = GettextCatalog.GetString ("Click here to add a new watch");
			CompletionWindowManager.WindowClosed += HandleCompletionWindowClosed;
			PreviewWindowManager.WindowClosed += HandlePreviewWindowClosed;
			ScrollAdjustmentsSet += HandleScrollAdjustmentsSet;

			expanderSize = (int) StyleGetProperty ("expander-size") + 4; //+4 is hardcoded in gtk.c code
			horizontal_separator = (int) StyleGetProperty ("horizontal-separator");
			grid_line_width = (int) StyleGetProperty ("grid-line-width");
			focus_line_width = (int) StyleGetProperty ("focus-line-width") * 2; //we just use *2 version in GetMaxWidth

			AdjustColumnSizes ();
		}

		bool allowEditing;

		/// <summary>
		/// Gets a value indicating whether the user should be able to edit values in the tree
		/// </summary>
		public bool AllowEditing {
			get => allowEditing;
			set {
				if (allowEditing == value)
					return;

				allowEditing = value;

				Refresh (false);
			}
		}

		protected override void OnDestroyed ()
		{
			CompletionWindowManager.WindowClosed -= HandleCompletionWindowClosed;
			PreviewWindowManager.WindowClosed -= HandlePreviewWindowClosed;
			PreviewWindowManager.DestroyWindow ();
			crtExp.Edited -= OnExpressionEdited;
			crtExp.EditingStarted -= OnExpressionStartedEditing;
			crtExp.EditingCanceled -= OnEditingCancelled;
			crtValue.EditingStarted -= OnValueEditing;
			crtValue.Edited -= OnValueEdited;
			crtValue.EditingCanceled -= OnEditingCancelled;

			typeCol.RemoveNotification ("width", OnColumnWidthChanged);
			valueCol.RemoveNotification ("width", OnColumnWidthChanged);
			expCol.RemoveNotification ("width", OnColumnWidthChanged);

			ScrollAdjustmentsSet -= HandleScrollAdjustmentsSet;
			if (oldHadjustment != null) {
				oldHadjustment.ValueChanged -= UpdatePreviewPosition;
				oldVadjustment.ValueChanged -= UpdatePreviewPosition;
				oldHadjustment = null;
				oldVadjustment = null;
			}

			controller.PinnedWatchChanged -= Controller_PinnedWatchChanged;
			controller.ChildrenLoaded -= Controller_NodeChildrenLoaded;
			controller.EvaluationCompleted -= Controller_EvaluationCompleted;

			disposed = true;
			controller.CancelAsyncTasks ();

			base.OnDestroyed ();
		}

		protected override void OnSizeAllocated (Gdk.Rectangle allocation)
		{
			base.OnSizeAllocated (allocation);
			AdjustColumnSizes ();
			UpdatePreviewPosition ();
		}

		protected override void OnShown ()
		{
			base.OnShown ();
			AdjustColumnSizes ();
			if (controller.CompactView)
				RecalculateWidth ();
		}

		protected override void OnRealized ()
		{
			base.OnRealized ();
			AdjustColumnSizes ();
		}

		void OnDragDataReceived (object o, DragDataReceivedArgs args)
		{
			if (!controller.AllowWatchExpressions)
				return;

			var text = args.SelectionData.Text;

			args.RetVal = true;

			if (string.IsNullOrEmpty (text))
				return;

			foreach (var expression in text.Split (new [] { '\n' })) {
				if (string.IsNullOrWhiteSpace (expression))
					continue;

				controller.AddExpression (expression.Trim ());
			}
		}

		void Controller_PinnedWatchChanged (object sender, EventArgs e)
		{
			Runtime.RunInMainThread (() => {
				if (controller.PinnedWatch == null) {
					pinCol.FixedWidth = 16;
				} else {
					pinCol.FixedWidth = 38;
				}
			}).Ignore ();
		}

		/// <summary>
		/// Triggered when the children of a node have been loaded
		/// </summary>
		void Controller_NodeChildrenLoaded (object sender, ObjectValueNodeChildrenChangedEventArgs e)
		{
			Runtime.RunInMainThread (() => {
				OnChildrenLoaded (e.Node, e.Index, e.Count);
			}).Ignore ();
		}

		/// <summary>
		/// Triggered when a node has completed evaluation and we have data to show the user
		/// </summary>
		void Controller_EvaluationCompleted (object sender, ObjectValueNodeEvaluationCompletedEventArgs e)
		{
			Runtime.RunInMainThread (() => {
				OnEvaluationCompleted (e.Node, e.ReplacementNodes);
			}).Ignore ();
		}

		void OnChildrenLoaded (ObjectValueNode node, int index, int count)
		{
			if (disposed)
				return;

			if (node == controller.Root) {
				// TODO: how to tell whether to reset scroll position or not?
				Refresh (false);
			} else {
				// the children of a specific node changed
				// remove the children for that node, then reload the children
				if (GetTreeIterFromNode (node, out TreeIter iter, out TreeIter parent)) {
					// rather than simply replacing the children of this node we will merge
					// them in so that the tree does not collapse the row when the last child is removed
					MergeChildrenIntoTree (node, iter, index, count);

					// if we did not load all the children, add a More node
					if (!node.ChildrenLoaded) {
						AppendNodeToTreeModel (iter, null, new ShowMoreValuesObjectValueNode (node));
					}
				}
			}

			if (controller.CompactView) {
				RecalculateWidth ();
			}
		}

		// TODO: if we don't want the scrolling, we can probably get rid of this
		public void OnNodeExpanded (ObjectValueNode node)
		{
			if (disposed)
				return;

			if (node.IsExpanded) {
				// if the node is _still_ expanded then adjust UI and scroll
				var path = GetTreePathForNode (node);

				if (!GetRowExpanded (path)) {
					ExpandRow (path, false);
				}

				if (controller.CompactView)
					RecalculateWidth ();

				// TODO: all this scrolling kind of seems awkward
				//if (path != null)
				//	ScrollToCell (path, expCol, true, 0f, 0f);
			}
		}

		/// <summary>
		/// Merge the node's children as children of the node in the tree
		/// </summary>
		void MergeChildrenIntoTree (ObjectValueNode node, TreeIter nodeIter, int index, int count)
		{
			var nodeChildren = node.Children.ToList ();

			if (nodeChildren.Count == 0) {
				RemoveChildren (nodeIter);
				return;
			}

			var visibleChildrenCount = store.IterNChildren (nodeIter);

			int ix = 0;
			while (ix < nodeChildren.Count) {
				// if we have existing visible rows in the tree, update the values and remove children
				if (ix < visibleChildrenCount) {
					if (store.IterNthChild (out TreeIter childIter, nodeIter, ix)) {
						RemoveChildren (childIter);
						SetValues (nodeIter, childIter, null, nodeChildren [ix]);
					}
				} else {
					AppendNodeToTreeModel (nodeIter, null, nodeChildren [ix]);
				}

				ix++;
			}

			if (ix < visibleChildrenCount) {
				// remove extra nodes we don't need anymore
				while (store.IterNthChild (out TreeIter childIter, nodeIter, ix))
					Remove (ref childIter);
			}
		}

		/// <summary>
		/// Updates or replaces the node with the given replacement nodes when the debugger notifies
		/// that the node has completed evaulation
		/// </summary>
		void OnEvaluationCompleted (ObjectValueNode node, ObjectValueNode[] replacementNodes)
		{
			if (disposed)
				return;

			if (GetTreeIterFromNode (node, out TreeIter iter, out TreeIter parent)) {
				// TODO we can use an expression node here
				// Keep the expression name entered by the user
				//if (store.IterDepth (iter) == 0)
				//	val.Name = (string) store.GetValue (iter, NameColumn);

				RemoveChildren (iter);

				if (replacementNodes.Length == 0) {
					// we can remove the node altogether, eg there are no local variables to show
					Remove (ref iter);
				} else if (replacementNodes.Length > 0) {
					node = replacementNodes [0];
					SetValues (parent, iter, node.Name, node);

					for (int n = 1; n < replacementNodes.Length; n++) {
						iter = store.InsertNodeAfter (iter);
						SetValues (parent, iter, null, replacementNodes[n]);
					}
				}
			}

			if (controller.CompactView) {
				RecalculateWidth ();
			}
		}

		bool Remove (ref TreeIter iter)
		{
			var node = GetNodeAtIter (iter);

			if (node != null && allNodes.TryGetValue (node, out TreeRowReference row)) {
				allNodes.Remove (node);
				row.Dispose ();
			}

			return store.Remove (ref iter);
		}

		void RemoveChildren (TreeIter iter)
		{
			while (store.IterChildren (out TreeIter child, iter)) {
				RemoveChildren (child);
				Remove (ref child);
			}
		}

		void SaveState ()
		{
			state.Save ();
		}

		void LoadState ()
		{
			restoringState = true;
			state.Load ();
			restoringState = false;
		}

		//public void Update ()
		//{
		//	//cachedValues.Clear ();
		//	Refresh (true);
		//}

		void Refresh (bool resetScrollPosition)
		{
			// Note: this is a hack that ideally we could get rid of...
			if (IsRealized && resetScrollPosition)
				ScrollToPoint (0, 0);

			SaveState ();

			CleanPinIcon ();
			store.Clear ();

			bool showExpanders = controller.AllowWatchExpressions;

			if (controller.Root != null) {
				if (LoadNode (controller.Root, TreeIter.Zero)) {
					showExpanders = true;
				}
			}

			if (showExpanders)
				ShowExpanders = true;

			if (controller.AllowWatchExpressions)
				store.AppendValues (createMsg, "", "", true, true, null, Ide.Gui.Styles.ColorGetHex (Styles.ObjectValueTreeValueDisabledText), Ide.Gui.Styles.ColorGetHex (Styles.ObjectValueTreeValueDisabledText));

			LoadState ();
		}

		bool LoadNode (ObjectValueNode node, TreeIter parent)
		{
			var result = false;
			foreach (var val in node.Children) {
				// append value calls setvalues which adds a dummy row for new and unloaded children.
				var iter = AppendNodeToTreeModel (parent, null, val);
				if (val.HasChildren) {
					result = true;
					if (val.ChildrenLoaded || val.Children.Count > 0) {
						// if any children for the node have already been loaded then add
						// these to the tree immediately instead of adding a dummy loading node
						LoadNode (val, iter);

						// make sure the load more button is enabled for enumerable nodes that are not fully loaded
						if (!val.ChildrenLoaded) {
							AppendNodeToTreeModel (iter, null, new ShowMoreValuesObjectValueNode (node));
						}
					}
				}
			}

			return result;
		}

		/// <summary>
		/// Fired when the user clicks on the value button, eg "Show Value", 'More Values", "Show Values"
		/// </summary>
		/// <param name="it"></param>
		void HandleValueButton (TreeIter it)
		{
			var node = GetNodeAtIter (it);
			HideValueButton (it);

			if (node.IsEnumerable) {
				if (node is ShowMoreValuesObjectValueNode moreNode) {
					controller.FetchMoreChildrenAsync (moreNode.EnumerableNode).Ignore ();
				} else {
					// use ExpandRow to expand so we see the loading message, expanding the node will trigger a fetch of the children
					var treePath = GetTreePathForNode (node);
					ExpandRow (treePath, false);
				}
			} else {
				// this is likely to support IsImplicitNotSupported 
				controller.RefreshNode (node);
				// update the tree
				if (store.IterParent (out TreeIter parentIter, it)) {
					SetValues (parentIter, it, null, node);
				} else {
					SetValues (TreeIter.Zero, it, null, node);
				}
			}
		}

		void HideValueButton(TreeIter iter)
		{
			store.SetValue (iter, ValueButtonTextColumn, string.Empty);
		}

		TreeIter AppendNodeToTreeModel (TreeIter parent, string name, ObjectValueNode valueNode)
		{
			TreeIter iter;

			if (parent.Equals (TreeIter.Zero))
				iter = store.AppendNode ();
			else
				iter = store.AppendNode (parent);

			SetValues (parent, iter, name, valueNode);
			return iter;
		}

		// TODO: refactor this so that we can update a node without needing to know the parent iter all the time
		void SetValues (TreeIter parent, TreeIter it, string name, ObjectValueNode val, bool updateJustValue = false)
		{
			// create a link to the node in the tree view and it's path

			// TODO: test if the link to the node we have is the same as the link we want to set or remove the invalid link
			//if (allNodes.TryGetValue (val, out TreeRowReference row)) {
			//	allNodes.Remove (val);
			//	row.Dispose ();
			//}
			allNodes [val] = new TreeRowReference (store, store.GetPath (it));


			string strval;
			string nameColor = null;
			string valueColor = null;
			string valueButton = null;
			string evaluateStatusIcon = null;


			name = name ?? val.Name;

			bool hasParent = !parent.Equals (TreeIter.Zero);
			bool showViewerButton = false;

			string valPath;
			if (!hasParent)
				valPath = "/" + name;
			else
				valPath = GetIterPath (parent) + "/" + name;

			if (val.IsUnknown) {
				if (frame != null) {
					strval = GettextCatalog.GetString ("The name '{0}' does not exist in the current context.", val.Name);
					nameColor = Ide.Gui.Styles.ColorGetHex (Styles.ObjectValueTreeValueDisabledText);
				} else {
					strval = string.Empty;
				}
				evaluateStatusIcon = MonoDevelop.Ide.Gui.Stock.Warning;
			} else if (val.IsError || val.IsNotSupported) {
				evaluateStatusIcon = MonoDevelop.Ide.Gui.Stock.Warning;
				strval = val.Value;
				int i = strval.IndexOf ('\n');
				if (i != -1)
					strval = strval.Substring (0, i);
				valueColor = Ide.Gui.Styles.ColorGetHex (Styles.ObjectValueTreeValueErrorText);
			} else if (val.IsImplicitNotSupported) {
				strval = "";//val.Value; with new "Show Value" button we don't want to display message "Implicit evaluation is disabled"
				valueColor = Ide.Gui.Styles.ColorGetHex (Styles.ObjectValueTreeValueDisabledText);
				if (val.CanRefresh)
					valueButton = GettextCatalog.GetString ("Show Value");
			} else if (val.IsEvaluating) {
				strval = GettextCatalog.GetString ("Evaluating...");

				evaluateStatusIcon = "md-spinner-16";

				valueColor = Ide.Gui.Styles.ColorGetHex (Styles.ObjectValueTreeValueDisabledText);
				if (val.GetIsEvaluatingGroup ()) {
					nameColor = Ide.Gui.Styles.ColorGetHex (Styles.ObjectValueTreeValueDisabledText);
					name = val.Name;
				}
			} else if (val.IsEnumerable) {
				if (val is ShowMoreValuesObjectValueNode) {
					valueButton = GettextCatalog.GetString ("Show More");
				} else {
					valueButton = GettextCatalog.GetString ("Show Values");
				}
				strval = "";
			} else {
				strval = controller.GetDisplayValueWithVisualisers (val, out showViewerButton);

				if (controller.GetNodeHasChangedSinceLastCheckpoint(val)) {
					nameColor = valueColor = Ide.Gui.Styles.ColorGetHex (Styles.ObjectValueTreeValueModifiedText);
				}
			}

			strval = strval.Replace ("\r\n", " ").Replace ("\n", " ");

			store.SetValue (it, ValueColumn, strval);
			if (updateJustValue)
				return;

			bool canEdit = controller.CanEditObject (val);
			string icon = ObjectValueTreeViewController.GetIcon (val.Flags);

			store.SetValue (it, NameColumn, name);
			store.SetValue (it, TypeColumn, val.TypeName);
			store.SetValue (it, ObjectNodeColumn, val);
			store.SetValue (it, NameEditableColumn, !hasParent && controller.AllowWatchExpressions);
			store.SetValue (it, ValueEditableColumn, canEdit);
			store.SetValue (it, IconColumn, icon);
			store.SetValue (it, NameColorColumn, nameColor);
			store.SetValue (it, ValueColorColumn, valueColor);
			store.SetValue (it, EvaluateStatusIconVisibleColumn, evaluateStatusIcon != null);
			store.LoadIcon (it, EvaluateStatusIconColumn, evaluateStatusIcon, IconSize.Menu);
			store.SetValue (it, ValueButtonVisibleColumn, valueButton != null);
			store.SetValue (it, ValueButtonTextColumn, valueButton);
			store.SetValue (it, ViewerButtonVisibleColumn, showViewerButton);


			if (ValidObjectForPreviewIcon (it))
				store.SetValue (it, PreviewIconColumn, "md-empty");

			if (!hasParent && controller.PinnedWatch != null) {
				store.SetValue (it, PinIconColumn, "md-pin-down");
				if (controller.PinnedWatch.LiveUpdate)
					store.SetValue (it, LiveUpdateIconColumn, liveIcon);
				else
					store.SetValue (it, LiveUpdateIconColumn, noLiveIcon);
			}
			if (controller.RootPinAlwaysVisible && (!hasParent && controller.PinnedWatch == null && controller.AllowPinning))
				store.SetValue (it, PinIconColumn, "md-pin-up");

			if (val.HasChildren && val.Children.Count == 0) {
				// Add dummy node, we need this or the expander isn't shown, but only if the children are not
				// already loaded
				store.AppendValues (it, GettextCatalog.GetString ("Loading\u2026"), "", "", false);
				if (!ShowExpanders) {
					ShowExpanders = true;
				}

				if (controller.GetNodeWasExpandedAtLastCheckpoint (val)) {
					ExpandRow (store.GetPath (it), false);
				}
			}
		}

		protected override bool OnTestExpandRow (TreeIter iter, TreePath path)
		{
			if (!restoringState) {
				if (!controller.AllowExpanding)
					return true;

				if (GetRowExpanded (path))
					return true;

				TreeIter parent;
				if (store.IterParent (out parent, iter)) {
					if (!GetRowExpanded (store.GetPath (parent)))
						return true;
				}
			}

			return base.OnTestExpandRow (iter, path);
		}

		public event EventHandler<ObjectValueNodeEventArgs> NodeExpanded;

		protected override void OnRowExpanded (TreeIter iter, TreePath path)
		{
			var node = GetNodeAtIter (iter);

			base.OnRowExpanded (iter, path);

			if (controller.CompactView)
				RecalculateWidth ();

			HideValueButton (iter);

			NodeExpanded?.Invoke (this, new ObjectValueNodeEventArgs (node));
		}

		public event EventHandler<ObjectValueNodeEventArgs> NodeCollapsed;

		protected override void OnRowCollapsed (TreeIter iter, TreePath path)
		{
			var node = GetNodeAtIter (iter);

			base.OnRowCollapsed (iter, path);

			if (controller.CompactView)
				RecalculateWidth ();

			NodeCollapsed?.Invoke (this, new ObjectValueNodeEventArgs (node));

			// TODO: all this scrolling kind of seems awkward
			//ScrollToCell (path, expCol, true, 0f, 0f);
		}

		string GetIterPath (TreeIter iter)
		{
			var path = new StringBuilder ();

			do {
				string name = (string) store.GetValue (iter, NameColumn);
				path.Insert (0, "/" + name);
			} while (store.IterParent (out iter, iter));

			return path.ToString ();
		}

		void OnExpressionStartedEditing (object s, EditingStartedArgs args)
		{
			if (!store.GetIterFromString (out TreeIter iter, args.Path))
				return;

			var entry = (Entry) args.Editable;
			if (entry.Text == createMsg)
				entry.Text = string.Empty;

			OnStartEditing (args);
		}

		void OnExpressionEdited (object s, EditedArgs args)
		{
			OnEndEditing ();

			if (!store.GetIterFromString (out TreeIter iter, args.Path))
				return;

			var node = GetNodeAtIter (iter);

			if (node == null) {
				if (args.NewText.Length > 0)
					controller.AddExpression (args.NewText);
			} else {
				controller.EditExpression (node, args.NewText);
			}
		}

		bool editing;

		void OnValueEditing (object s, EditingStartedArgs args)
		{
			TreeIter it;
			if (!store.GetIterFromString (out it, args.Path))
				return;

			var entry = (Entry)args.Editable;

			var val = GetDebuggerObjectValueAtIter (it);
			string strVal = null;
			if (val != null) {
				if (val.TypeName == "string") {
					var opt = frame.DebuggerSession.Options.EvaluationOptions.Clone ();
					opt.EllipsizeStrings = false;
					strVal = '"' + Mono.Debugging.Evaluation.ExpressionEvaluator.EscapeString ((string)val.GetRawValue (opt)) + '"';
				} else {
					strVal = val.Value;
				}
			}
			if (!string.IsNullOrEmpty (strVal))
				entry.Text = strVal;

			entry.GrabFocus ();
			OnStartEditing (args);
		}

		void OnValueEdited (object s, EditedArgs args)
		{
			OnEndEditing ();

			if (!store.GetIterFromString (out TreeIter iter, args.Path))
				return;

			// get the node that we just edited
			var val = GetNodeAtIter (iter);
			if (controller.EditNodeValue (val, args.NewText)) {
				// update the store
				//store.SetValue (it, ValueColumn, val.GetDisplayValue());
				SetValues (TreeIter.Zero, iter, null, val);
			}
		}

		void OnEditingCancelled (object s, EventArgs args)
		{
			OnEndEditing ();
		}

		void OnStartEditing (EditingStartedArgs args)
		{
			editing = true;
			editEntry = (Entry)args.Editable;
			editEntry.KeyPressEvent += OnEditKeyPress;
			editEntry.KeyReleaseEvent += OnEditKeyRelease;

			controller.OnStartEditing ();
		}

		void OnEndEditing ()
		{
			editing = false;
			editEntry.KeyPressEvent -= OnEditKeyPress;
			editEntry.KeyReleaseEvent -= OnEditKeyRelease;

			CompletionWindowManager.HideWindow ();
			currentCompletionData = null;

			controller.OnEndEditing ();
		}

		void OnEditKeyRelease (object sender, EventArgs e)
		{
			if (!wasHandled) {
				CompletionWindowManager.PostProcessKeyEvent (KeyDescriptor.FromGtk (key, keyChar, modifierState));
				PopupCompletion ((Entry) sender);
			}
		}

		bool wasHandled;
		CodeCompletionContext ctx;
		Gdk.Key key;
		char keyChar;
		Gdk.ModifierType modifierState;
		uint keyValue;

		[GLib.ConnectBeforeAttribute]
		void OnEditKeyPress (object s, KeyPressEventArgs args)
		{
			wasHandled = false;
			key = args.Event.Key;
			keyChar = (char)args.Event.Key;
			modifierState = args.Event.State;
			keyValue = args.Event.KeyValue;

			if (currentCompletionData != null) {
				wasHandled = CompletionWindowManager.PreProcessKeyEvent (KeyDescriptor.FromGtk (key, keyChar, modifierState));
				args.RetVal = wasHandled;
			}
		}

		static bool IsCompletionChar (char c)
		{
			return char.IsLetter (c) || c == '_' || c == '.';
		}

		CancellationTokenSource cts = new CancellationTokenSource ();
		async void PopupCompletion (Entry entry)
		{
			try {
				char c = (char)Gdk.Keyval.ToUnicode (keyValue);
				if (currentCompletionData == null && IsCompletionChar (c)) {
					string expr = entry.Text.Substring (0, entry.CursorPosition);
					cts.Cancel ();
					cts = new CancellationTokenSource ();
					currentCompletionData = await GetCompletionDataAsync (expr, cts.Token);
					if (currentCompletionData != null) {
						var dataList = new DebugCompletionDataList (currentCompletionData);
						ctx = ((ICompletionWidget)this).CreateCodeCompletionContext (expr.Length - currentCompletionData.ExpressionLength);
						CompletionWindowManager.ShowWindow (null, c, dataList, this, ctx);
					}
				}

			} catch (OperationCanceledException) {
			}
		}

		TreeIter lastPinIter;

		enum PreviewButtonIcons
		{
			None,
			Hidden,
			RowHover,
			Hover,
			Active,
			Selected,
		}

		PreviewButtonIcons iconBeforeSelected;
		PreviewButtonIcons currentIcon;
		TreeIter currentHoverIter = TreeIter.Zero;

		bool ValidObjectForPreviewIcon (TreeIter it)
		{
			var obj = GetDebuggerObjectValueAtIter (it);
			if (obj == null) {
				return false;
			} else {
				if (obj.IsNull)
					return false;
				if (obj.IsPrimitive) {
					//obj.DisplayValue.Contains ("|") is special case to detect enum with [Flags]
					return obj.TypeName == "string" || (obj.DisplayValue != null && obj.DisplayValue.Contains ("|"));
				}
				if (string.IsNullOrEmpty (obj.TypeName))
					return false;
			}
			return true;
		}


		protected override bool OnMotionNotifyEvent (Gdk.EventMotion evnt)
		{
			TreePath path;
			if (!editing && GetPathAtPos ((int)evnt.X, (int)evnt.Y, out path)) {

				TreeIter it;
				if (store.GetIter (out it, path)) {
					TreeViewColumn col;
					CellRenderer cr;
					if (GetCellAtPos ((int)evnt.X, (int)evnt.Y, out path, out col, out cr) && cr == crtExp) {
						using (var layout = new Pango.Layout (PangoContext)) {
							layout.FontDescription = crtExp.FontDesc.Copy ();
							layout.FontDescription.Family = crtExp.Family;

							var name = (string) store.GetValue (it, NameColumn);
							if (!string.IsNullOrEmpty (name))
								layout.SetText (name);

							int w, h;
							layout.GetPixelSize (out w, out h);
							var cellArea = GetCellRendererArea (path, col, cr);
							var iconXOffset = cellArea.X + w + cr.Xpad * 3;
							if (iconXOffset < evnt.X &&
							   iconXOffset + 16 > evnt.X) {
								SetPreviewButtonIcon (PreviewButtonIcons.Hover, it);
							} else {
								SetPreviewButtonIcon (PreviewButtonIcons.RowHover, it);
							}
						}
					} else {
						SetPreviewButtonIcon (PreviewButtonIcons.RowHover, it);
					}

					if (controller.AllowPinning) {
						if (path.Depth > 1 || controller.PinnedWatch == null) {
							if (!it.Equals (lastPinIter)) {
								store.SetValue (it, PinIconColumn, "md-pin-up");
								CleanPinIcon ();
								if (path.Depth > 1 || !controller.RootPinAlwaysVisible)
									lastPinIter = it;
							}
						}
					}
				}
			} else {
				SetPreviewButtonIcon (PreviewButtonIcons.Hidden);
			}
			return base.OnMotionNotifyEvent (evnt);
		}

		void CleanPinIcon ()
		{
			if (!lastPinIter.Equals (TreeIter.Zero) && store.IterIsValid (lastPinIter)) {
				store.SetValue (lastPinIter, PinIconColumn, null);
			}
			lastPinIter = TreeIter.Zero;
		}

		protected override bool OnLeaveNotifyEvent (Gdk.EventCrossing evnt)
		{
			if (!editing)
				CleanPinIcon ();
			SetPreviewButtonIcon (PreviewButtonIcons.Hidden);
			return base.OnLeaveNotifyEvent (evnt);
		}

		protected override bool OnKeyPressEvent (Gdk.EventKey evnt)
		{
			// Ignore if editing a cell
			if (editing)
				return base.OnKeyPressEvent (evnt);

			TreePath [] selected = Selection.GetSelectedRows ();
			bool changed = false;
			TreePath lastPath;

			if (selected == null || selected.Length < 1)
				return base.OnKeyPressEvent (evnt);

			switch (evnt.Key) {
			case Gdk.Key.Left:
			case Gdk.Key.KP_Left:
				foreach (var path in selected) {
					lastPath = path.Copy ();
					if (GetRowExpanded (path)) {
						CollapseRow (path);
						changed = true;
					} else if (path.Up ()) {
						Selection.UnselectPath (lastPath);
						Selection.SelectPath (path);
						changed = true;
					}
				}
				break;
			case Gdk.Key.Right:
			case Gdk.Key.KP_Right:
				foreach (var path in selected) {
					if (!GetRowExpanded (path)) {
						ExpandRow (path, false);
						changed = true;
					} else {
						lastPath = path.Copy ();
						path.Down ();
						if (lastPath.Compare (path) != 0) {
							Selection.UnselectPath (lastPath);
							Selection.SelectPath (path);
							changed = true;
						}
					}
				}
				break;
			case Gdk.Key.Delete:
			case Gdk.Key.KP_Delete:
			case Gdk.Key.BackSpace:
				string expression;
				ObjectValue val;
				TreeIter iter;

				if (!controller.AllowEditing || !controller.AllowWatchExpressions)
					return base.OnKeyPressEvent (evnt);

				// Note: since we'll be modifying the tree, we need to make changes from bottom to top
				Array.Sort (selected, new TreePathComparer (true));

				foreach (var path in selected) {
					if (!Model.GetIter (out iter, path))
						continue;

					var node = GetNodeAtIter (iter);
					if (controller.RemoveValue (node))
						changed = true;

					//val = GetDebuggerObjectValueAtIter (iter);
					//expression = GetFullExpression (iter);

					//// FIXME: expressions use ObjectValues now, so this logic probably needs to change...

					//// Lookup and remove
					//if (val != null && values.Contains (val)) {
					//	RemoveValue (val);
					//	changed = true;
					//} else if (!string.IsNullOrEmpty (expression) && controller.RemoveExpression (expression)) {
					//	changed = true;
					//}
				}
				break;
			}

			return changed || base.OnKeyPressEvent (evnt);
		}

		Gdk.Rectangle GetCellRendererArea (TreePath path, TreeViewColumn col, CellRenderer cr)
		{
			var rect = this.GetCellArea (path, col);
			int x, width;
			col.CellGetPosition (cr, out x, out width);
			return new Gdk.Rectangle (rect.X + x, rect.Y, width, rect.Height);
		}

		Gdk.Rectangle startPreviewCaret;
		double startHAdj;
		double startVAdj;

		protected override bool OnButtonPressEvent (Gdk.EventButton evnt)
		{
			allowStoreColumnSizes = true;

			TreeViewColumn col;
			CellRenderer cr;
			TreePath path;
			bool closePreviewWindow = true;
			bool clickProcessed = false;

			TreeIter it;
			if (this.controller.CanQueryDebugger && evnt.Button == 1 && GetCellAtPos ((int)evnt.X, (int)evnt.Y, out path, out col, out cr) && store.GetIter (out it, path)) {
				if (cr == crpViewer) {
					clickProcessed = true;
					var node = GetNodeAtIter (it);
					if (controller.ShowNodeValueVisualizer (node)) {
						SetValues (TreeIter.Zero, it, null, node);
					}
				} else if (cr == crtExp && !PreviewWindowManager.IsVisible && ValidObjectForPreviewIcon (it)) {
					var val = GetDebuggerObjectValueAtIter (it);
					startPreviewCaret = GetCellRendererArea (path, col, cr);
					startHAdj = Hadjustment.Value;
					startVAdj = Vadjustment.Value;
					int w, h;
					using (var layout = new Pango.Layout (PangoContext)) {
						layout.FontDescription = crtExp.FontDesc.Copy ();
						layout.FontDescription.Family = crtExp.Family;
						layout.SetText ((string) store.GetValue (it, NameColumn));
						layout.GetPixelSize (out w, out h);
					}
					startPreviewCaret.X += (int)(w + cr.Xpad * 3);
					startPreviewCaret.Width = 16;
					ConvertTreeToWidgetCoords (startPreviewCaret.X, startPreviewCaret.Y, out startPreviewCaret.X, out startPreviewCaret.Y);
					startPreviewCaret.X += (int)Hadjustment.Value;
					startPreviewCaret.Y += (int)Vadjustment.Value;
					if (startPreviewCaret.X < evnt.X &&
						startPreviewCaret.X + 16 > evnt.X) {
						clickProcessed = true;
						if (controller.CompactView) {
							SetPreviewButtonIcon (PreviewButtonIcons.Active, it);
						} else {
							SetPreviewButtonIcon (PreviewButtonIcons.Selected, it);
						}
						DebuggingService.ShowPreviewVisualizer (val, this, startPreviewCaret);
						closePreviewWindow = false;
					} else {
						if (editing)
							base.OnButtonPressEvent (evnt);//End current editing
						if (!Selection.IterIsSelected (it))
							base.OnButtonPressEvent (evnt);//Select row, so base.OnButtonPressEvent below starts editing
					}
				} else if (cr == crtValue) {
					if ((Platform.IsMac && ((evnt.State & Gdk.ModifierType.Mod2Mask) > 0)) ||
						(!Platform.IsMac && ((evnt.State & Gdk.ModifierType.ControlMask) > 0))) {
						var url = crtValue.Text.Trim ('"', '{', '}');
						Uri uri;
						if (url != null && Uri.TryCreate (url, UriKind.Absolute, out uri) && (uri.Scheme == "http" || uri.Scheme == "https")) {
							clickProcessed = true;
							IdeServices.DesktopService.ShowUrl (url);
						}
					}
				} else if (cr == crtExp) {
					if (editing)
						base.OnButtonPressEvent (evnt);//End current editing
					if (!Selection.IterIsSelected (it))
						base.OnButtonPressEvent (evnt);//Select row, so base.OnButtonPressEvent below starts editing
				} else if (!editing) {
					if (cr == crpButton) {
						clickProcessed = true;
						HandleValueButton (it);
					} else if (cr == crpPin) {
						clickProcessed = true;
						TreeIter pi;
						if (controller.PinnedWatch != null && !store.IterParent (out pi, it))
							controller.RemovePinnedWatch ();
						else
							CreatePinnedWatch (it);
					} else if (cr == crpLiveUpdate) {
						clickProcessed = true;
						TreeIter pi;
						if (controller.PinnedWatch != null && !store.IterParent (out pi, it)) {
							DebuggingService.SetLiveUpdateMode (controller.PinnedWatch, !controller.PinnedWatch.LiveUpdate);
							if (controller.PinnedWatch.LiveUpdate)
								store.SetValue (it, LiveUpdateIconColumn, liveIcon);
							else
								store.SetValue (it, LiveUpdateIconColumn, noLiveIcon);
						}
					}
				}
			}

			if (closePreviewWindow) {
				PreviewWindowManager.DestroyWindow ();
			}

			if (clickProcessed)
				return true;

			//HACK: show context menu in release event instead of show event to work around gtk bug
			if (evnt.TriggersContextMenu ()) {
				//	ShowPopup (evnt);
				if (!this.IsClickedNodeSelected ((int)evnt.X, (int)evnt.Y)) {
					//pass click to base so it can update the selection
					//unless the node is already selected, in which case we don't want to change the selection(deselect multi selection)
					base.OnButtonPressEvent (evnt);
				}
				return true;
			} else {
				return base.OnButtonPressEvent (evnt);
			}
		}

		protected override bool OnButtonReleaseEvent (Gdk.EventButton evnt)
		{
			allowStoreColumnSizes = false;
			var res = base.OnButtonReleaseEvent (evnt);

			//HACK: show context menu in release event instead of show event to work around gtk bug
			if (evnt.IsContextMenuButton ()) {
				ShowPopup (evnt);
				return true;
			}
			return res;
		}

		protected override bool OnPopupMenu ()
		{
			ShowPopup (null);
			return true;
		}

		void ShowPopup (Gdk.EventButton evt)
		{
			if (controller.AllowPopupMenu)
				this.ShowContextMenu (evt, menuSet, this);
		}

		[CommandUpdateHandler (EditCommands.SelectAll)]
		protected void UpdateSelectAll (CommandInfo cmd)
		{
			if (editing) {
				cmd.Bypass = true;
				return;
			}
			TreeIter iter;

			cmd.Enabled = store.GetIterFirst (out iter);
		}

		[CommandHandler (EditCommands.SelectAll)]
		protected new void OnSelectAll ()
		{
			if (editing) {
				base.OnSelectAll ();
				return;
			}
			Selection.SelectAll ();
		}

		[CommandHandler (EditCommands.Copy)]
		protected void OnCopy ()
		{
			TreePath [] selected = Selection.GetSelectedRows ();
			TreeIter iter;

			if (selected == null || selected.Length == 0)
				return;

			if (selected.Length == 1) {
				var editable = IdeApp.Workbench.RootWindow.Focus as Editable;

				if (editable != null) {
					editable.CopyClipboard ();
					return;
				}
			}

			var str = new StringBuilder ();
			bool needsNewLine = false;
			for (int i = 0; i < selected.Length; i++) {
				if (!store.GetIter (out iter, selected [i]))
					continue;
				if (needsNewLine)
					str.AppendLine ();
				needsNewLine = true;

				string value = (string) store.GetValue (iter, ValueColumn);
				string type = (string) store.GetValue (iter, TypeColumn);
				if (type == "string") {
					var objVal = GetDebuggerObjectValueAtIter (iter);
					if (objVal != null) {
						var opt = frame.DebuggerSession.Options.EvaluationOptions.Clone ();
						opt.EllipsizeStrings = false;
						value = '"' + Mono.Debugging.Evaluation.ExpressionEvaluator.EscapeString ((string)objVal.GetRawValue (opt)) + '"';
					}
				}
				str.Append (value);
			}

			Clipboard.Get (Gdk.Selection.Clipboard).Text = str.ToString ();
		}

		[CommandHandler (EditCommands.Delete)]
		[CommandHandler (EditCommands.DeleteKey)]
		protected void OnDelete ()
		{
			// TODO: remove all nodes at once
			var nodesToDelete = new List<ObjectValueNode> ();
			foreach (var path in Selection.GetSelectedRows ()) {
				if (!store.GetIter (out TreeIter iter, path))
					continue;

				var node = GetNodeAtIter (iter);
				nodesToDelete.Add (node);
			}

			foreach (var node in nodesToDelete) 
				controller.RemoveValue (node);
		}

		[CommandUpdateHandler (EditCommands.Delete)]
		[CommandUpdateHandler (EditCommands.DeleteKey)]
		protected void OnUpdateDelete (CommandInfo cinfo)
		{
			if (editing) {
				cinfo.Bypass = true;
				return;
			}

			if (!controller.AllowWatchExpressions) {
				cinfo.Visible = false;
				return;
			}

			TreePath [] sel = Selection.GetSelectedRows ();
			if (sel.Length == 0) {
				cinfo.Enabled = false;
				return;
			}

			foreach (TreePath tp in sel) {
				if (tp.Depth > 1) {
					cinfo.Enabled = false;
					return;
				}
			}
		}

		[CommandHandler (DebugCommands.AddWatch)]
		protected void OnAddWatch ()
		{
			var expressions = new List<string> ();

			foreach (var tp in Selection.GetSelectedRows ()) {
				TreeIter it;

				if (store.GetIter (out it, tp)) {
					var expression = GetFullExpression (it);

					if (!string.IsNullOrEmpty (expression))
						expressions.Add (expression);
				}
			}

			foreach (var expression in expressions)
				DebuggingService.AddWatch (expression);
		}

		[CommandUpdateHandler (DebugCommands.AddWatch)]
		protected void OnUpdateAddWatch (CommandInfo cinfo)
		{
			cinfo.Enabled = Selection.GetSelectedRows ().Length > 0;
		}

		[CommandHandler (EditCommands.Rename)]
		protected void OnRename ()
		{
			TreeIter it;
			if (store.GetIter (out it, Selection.GetSelectedRows () [0]))
				SetCursor (store.GetPath (it), Columns [0], true);
		}

		[CommandUpdateHandler (EditCommands.Rename)]
		protected void OnUpdateRename (CommandInfo cinfo)
		{
			cinfo.Visible = controller.AllowWatchExpressions;
			cinfo.Enabled = Selection.GetSelectedRows ().Length == 1;
		}

		protected override void OnRowActivated (TreePath path, TreeViewColumn column)
		{
			base.OnRowActivated (path, column);

			if (!controller.CanQueryDebugger)
				return;

			TreePath [] selected = Selection.GetSelectedRows ();
			TreeIter iter;

			if (!store.GetIter (out iter, selected [0]))
				return;

			var val = GetDebuggerObjectValueAtIter (iter);
			if (val != null && val.Name == DebuggingService.DebuggerSession.EvaluationOptions.CurrentExceptionTag)
				DebuggingService.ShowExceptionCaughtDialog ();

			if (val != null && DebuggingService.HasValueVisualizers (val))
				DebuggingService.ShowValueVisualizer (val);
		}


		bool GetCellAtPos (int x, int y, out TreePath path, out TreeViewColumn col, out CellRenderer cellRenderer)
		{
			if (GetPathAtPos (x, y, out path, out col)) {
				var cellArea = GetCellArea (path, col);
				x -= cellArea.X;
				foreach (var cr in col.CellRenderers) {
					int xo, w;
					col.CellGetPosition (cr, out xo, out w);
					var visible = cr.Visible;
					if (cr == crpViewer) {
						if (store.GetIter (out var it, path)) {
							visible = (bool) store.GetValue (it, ViewerButtonVisibleColumn);
						}
					} else if (cr == evaluateStatusCell) {
						if (store.GetIter (out var it, path)) {
							visible = (bool) store.GetValue (it, EvaluateStatusIconVisibleColumn);
						}
					} else if (cr == crpButton) {
						if (store.GetIter (out var it, path)) {
							visible = (bool) store.GetValue (it, ValueButtonVisibleColumn);
						}
					}
					if (visible && x >= xo && x < xo + w) {
						cellRenderer = cr;
						return true;
					}
				}
			}
			cellRenderer = null;
			return false;
		}

		string GetFullExpression (TreeIter it)
		{
			var path = store.GetPath (it);
			string name, expression = "";

			while (path.Depth != 1) {
				var val = GetDebuggerObjectValueAtIter (it);
				if (val == null)
					return null;

				expression = val.ChildSelector + expression;
				if (!store.IterParent (out it, it))
					break;

				path = store.GetPath (it);
			}

			name = (string) store.GetValue (it, NameColumn);

			return name + expression;
		}

		void CreatePinnedWatch (TreeIter it)
		{
			var expression = GetFullExpression (it);

			if (string.IsNullOrEmpty (expression))
				return;

			var height = SizeRequest ().Height;

			if (controller.PinnedWatch != null)
				CollapseAll ();

			controller.CreatePinnedWatch (expression, height);
		}

		#region ICompletionWidget implementation 

		CodeCompletionContext ICompletionWidget.CurrentCodeCompletionContext {
			get {
				return ((ICompletionWidget)this).CreateCodeCompletionContext (editEntry.Position);
			}
		}

		public double ZoomLevel {
			get {
				return 1;
			}
		}

		public event EventHandler CompletionContextChanged;

		protected virtual void OnCompletionContextChanged (EventArgs e)
		{
			var handler = CompletionContextChanged;

			if (handler != null)
				handler (this, e);
		}

		string ICompletionWidget.GetText (int startOffset, int endOffset)
		{
			string text = editEntry.Text;

			if (startOffset < 0 || endOffset < 0 || startOffset > endOffset || startOffset >= text.Length)
				return "";

			int length = Math.Min (endOffset - startOffset, text.Length - startOffset);

			return text.Substring (startOffset, length);
		}

		void ICompletionWidget.Replace (int offset, int count, string text)
		{
			if (count > 0)
				editEntry.Text = editEntry.Text.Remove (offset, count);
			if (!string.IsNullOrEmpty (text))
				editEntry.Text = editEntry.Text.Insert (offset, text);
		}

		int ICompletionWidget.CaretOffset {
			get {
				return editEntry.Position;
			}
			set {
				editEntry.Position = value;
			}
		}

		char ICompletionWidget.GetChar (int offset)
		{
			string txt = editEntry.Text;

			return offset >= txt.Length ? '\0' : txt [offset];
		}

		CodeCompletionContext ICompletionWidget.CreateCodeCompletionContext (int triggerOffset)
		{
			int x, y;
			editEntry.GdkWindow.GetOrigin (out x, out y);
			editEntry.GetLayoutOffsets (out int tx, out int ty);
			int cp = editEntry.TextIndexToLayoutIndex (editEntry.Position);
			Pango.Rectangle rect = editEntry.Layout.IndexToPos (cp);
			x += Pango.Units.ToPixels (rect.X) + tx;
			y += editEntry.Allocation.Height;

			return new CodeCompletionContext (
				x, y, editEntry.SizeRequest ().Height,
				triggerOffset, 0, triggerOffset,
				currentCompletionData.ExpressionLength
			);
		}

		string ICompletionWidget.GetCompletionText (CodeCompletionContext ctx)
		{
			return editEntry.Text.Substring (ctx.TriggerOffset, ctx.TriggerWordLength);
		}

		void ICompletionWidget.SetCompletionText (CodeCompletionContext ctx, string partial_word, string complete_word)
		{
			int cursorOffset = editEntry.Position - (ctx.TriggerOffset + partial_word.Length);
			int sp = ctx.TriggerOffset;
			editEntry.DeleteText (sp, sp + partial_word.Length);
			editEntry.InsertText (complete_word, ref sp);
			editEntry.Position = sp + cursorOffset; // sp is incremented by InsertText
		}

		void ICompletionWidget.SetCompletionText (CodeCompletionContext ctx, string partial_word, string complete_word, int offset)
		{
			int cursorOffset = editEntry.Position - (ctx.TriggerOffset + partial_word.Length);
			int sp = ctx.TriggerOffset;
			editEntry.DeleteText (sp, sp + partial_word.Length);
			editEntry.InsertText (complete_word, ref sp);
			editEntry.Position = sp + offset + cursorOffset; // sp is incremented by InsertText
		}

		int ICompletionWidget.TextLength {
			get {
				return editEntry.Text.Length;
			}
		}

		int ICompletionWidget.SelectedLength {
			get {
				return 0;
			}
		}

		Style ICompletionWidget.GtkStyle {
			get {
				return editEntry.Style;
			}
		}

		#endregion

		async Task<Mono.Debugging.Client.CompletionData> GetCompletionDataAsync (string expression, CancellationToken token)
		{
			if (controller.CanQueryDebugger && frame != null)
				return await DebuggingService.GetCompletionDataAsync (frame, expression, token);

			return null;
		}

		internal void SetCustomFont (Pango.FontDescription font)
		{
			crpButton.FontDesc = crtExp.FontDesc = crtType.FontDesc = crtValue.FontDesc = font;
		}

		#region UI support

		static void ValueDataFunc (Gtk.TreeViewColumn tree_column, Gtk.CellRenderer cell, Gtk.TreeModel model, Gtk.TreeIter iter)
		{
			Xwt.Drawing.Color? color;
			ObjectValue val = null;

			var node = (ObjectValueNode) model.GetValue (iter, ObjectNodeColumn);
			if (node != null) {
				val = node.GetDebuggerObjectValue ();
			}

			if (val == null) {
				val = GetDebuggerObjectValueAtIter (iter, model);
			}

			if (val != null && !val.IsNull && DebuggingService.HasGetConverter<Xwt.Drawing.Color> (val)) {
				try {
					color = DebuggingService.GetGetConverter<Xwt.Drawing.Color> (val).GetValue (val);
				} catch (Exception) {
					color = null;
				}
			} else {
				color = null;
			}

			if (color != null) {
				((CellRendererColorPreview)cell).Color = (Xwt.Drawing.Color)color;
				cell.Visible = true;
			} else {
				cell.Visible = false;
			}
		}

		int expanderSize;
		int horizontal_separator;
		int grid_line_width;
		int focus_line_width;

		int GetMaxWidth (TreeViewColumn column, TreeIter iter)
		{
			var path = Model.GetPath (iter);
			int x, y, w, h;
			int columnWidth = 0;
			column.CellSetCellData (Model, iter, false, false);
			var area = new Gdk.Rectangle (0, 0, 1000, 1000);
			bool firstCell = true;
			foreach (var cellRenderer in column.CellRenderers) {
				if (!cellRenderer.Visible)
					continue;
				if (!firstCell && columnWidth > 0)
					columnWidth += column.Spacing;
				cellRenderer.GetSize (this, ref area, out x, out y, out w, out h);
				columnWidth += w + focus_line_width;
				firstCell = false;
			}
			if (ExpanderColumn == column) {
				columnWidth += horizontal_separator + (path.Depth - 1) * LevelIndentation;
				if (ShowExpanders)
					columnWidth += path.Depth * expanderSize;
			} else {
				columnWidth += horizontal_separator;
			}
			if (this.GetRowExpanded (path)) {
				var childrenCount = Model.IterNChildren (iter);
				for (int i = 0; i < childrenCount; i++) {
					TreeIter childIter;
					if (!Model.IterNthChild (out childIter, iter, i))
						break;
					columnWidth = Math.Max (columnWidth, GetMaxWidth (column, childIter));
				}
			}
			return columnWidth;
		}

		void RecalculateWidth ()
		{
			if (!Model.GetIterFirst (out TreeIter iter))
				return;

			foreach (var column in new [] { expCol, valueCol }) {//No need to calculate for Type and PinIcon columns
																 // +1 is here because apperently when we calculate MaxWidth and set to FixedWidth
																 // later GTK when cacluate needed width for Label it doesn't have enough space
																 // and puts "..." to end of text thinking there is not enough space
																 // I assume this is because rounding(floating point) calculation errors
																 // hence do +1 and avoid such problems.
				column.FixedWidth = GetMaxWidth (column, iter) + 1;
			}
		}

		void SetPreviewButtonIcon (PreviewButtonIcons icon, TreeIter it = default (TreeIter))
		{
			if (PreviewWindowManager.IsVisible || editing) {
				return;
			}
			if (!it.Equals (TreeIter.Zero)) {
				if (!ValidObjectForPreviewIcon (it)) {
					icon = PreviewButtonIcons.None;
				}
			}
			if (!currentHoverIter.Equals (it)) {
				if (!currentHoverIter.Equals (TreeIter.Zero) && store.IterIsValid (currentHoverIter)) {
					if (ValidObjectForPreviewIcon (currentHoverIter)) {
						if ((string) store.GetValue (currentHoverIter, PreviewIconColumn) != "md-empty")
							store.SetValue (currentHoverIter, PreviewIconColumn, "md-empty");
					}
				}
			}
			if (!it.Equals (TreeIter.Zero) && store.IterIsValid (it)) {
				if (icon == PreviewButtonIcons.Selected) {
					if ((currentIcon == PreviewButtonIcons.Active ||
						currentIcon == PreviewButtonIcons.Hover ||
						currentIcon == PreviewButtonIcons.RowHover) && it.Equals (TreeIter.Zero)) {
						iconBeforeSelected = currentIcon;
					}
				} else if (icon == PreviewButtonIcons.Active ||
						   icon == PreviewButtonIcons.Hover ||
						   icon == PreviewButtonIcons.RowHover) {
					iconBeforeSelected = icon;
					if (Selection.IterIsSelected (it)) {
						icon = PreviewButtonIcons.Selected;
					}
				}

				switch (icon) {
				case PreviewButtonIcons.None:
					if (store.GetValue (it, PreviewIconColumn) != null)
						store.SetValue (it, PreviewIconColumn, null);
					break;
				case PreviewButtonIcons.Hidden:
					if ((string) store.GetValue (it, PreviewIconColumn) != "md-empty")
						store.SetValue (it, PreviewIconColumn, "md-empty");
					break;
				case PreviewButtonIcons.RowHover:
					if ((string) store.GetValue (it, PreviewIconColumn) != "md-preview-normal")
						store.SetValue (it, PreviewIconColumn, "md-preview-normal");
					break;
				case PreviewButtonIcons.Hover:
					if ((string) store.GetValue (it, PreviewIconColumn) != "md-preview-hover")
						store.SetValue (it, PreviewIconColumn, "md-preview-hover");
					break;
				case PreviewButtonIcons.Active:
					if ((string) store.GetValue (it, PreviewIconColumn) != "md-preview-active")
						store.SetValue (it, PreviewIconColumn, "md-preview-active");
					break;
				case PreviewButtonIcons.Selected:
					if ((string) store.GetValue (it, PreviewIconColumn) != "md-preview-selected") {
						store.SetValue (it, PreviewIconColumn, "md-preview-selected");
					}
					break;
				}
				currentIcon = icon;
				currentHoverIter = it;
			} else {
				currentIcon = PreviewButtonIcons.None;
				currentHoverIter = TreeIter.Zero;
			}
		}

		void HandleSelectionChanged (object sender, EventArgs e)
		{
			if (!currentHoverIter.Equals (TreeIter.Zero) && store.IterIsValid (currentHoverIter)) {
				if (Selection.IterIsSelected (currentHoverIter)) {
					SetPreviewButtonIcon (PreviewButtonIcons.Selected, currentHoverIter);
				} else {
					SetPreviewButtonIcon (iconBeforeSelected, currentHoverIter);
				}
			}
		}

		Adjustment oldHadjustment;
		Adjustment oldVadjustment;
		//Don't convert this event handler to override OnSetScrollAdjustments as it causes problems
		void HandleScrollAdjustmentsSet (object o, ScrollAdjustmentsSetArgs args)
		{
			if (oldHadjustment != null) {
				oldHadjustment.ValueChanged -= UpdatePreviewPosition;
				oldVadjustment.ValueChanged -= UpdatePreviewPosition;
			}
			oldHadjustment = Hadjustment;
			oldVadjustment = Vadjustment;
			oldHadjustment.ValueChanged += UpdatePreviewPosition;
			oldVadjustment.ValueChanged += UpdatePreviewPosition;
		}

		void UpdatePreviewPosition (object sender, EventArgs e)
		{
			UpdatePreviewPosition ();
		}

		void UpdatePreviewPosition ()
		{
			if (startPreviewCaret.IsEmpty)
				return;
			var newCaret = new Gdk.Rectangle (
							   (int)(startPreviewCaret.Left + (startHAdj - Hadjustment.Value)),
							   (int)(startPreviewCaret.Top + (startVAdj - Vadjustment.Value)),
							   startPreviewCaret.Width,
							   startPreviewCaret.Height);
			var treeViewRectangle = new Gdk.Rectangle (
										this.VisibleRect.X - (int)Hadjustment.Value,
										this.VisibleRect.Y - (int)Vadjustment.Value,
										this.VisibleRect.Width,
										this.VisibleRect.Height);
			if (treeViewRectangle.Contains (new Gdk.Point (
					newCaret.X + newCaret.Width / 2,
					newCaret.Y + newCaret.Height / 2 - (controller.CompactView ? 0 : 30)))) {
				PreviewWindowManager.RepositionWindow (newCaret);
			} else {
				PreviewWindowManager.DestroyWindow ();
			}
		}

		void HandlePreviewWindowClosed (object sender, EventArgs e)
		{
			SetPreviewButtonIcon (PreviewButtonIcons.Hidden);
		}

		void HandleCompletionWindowClosed (object sender, EventArgs e)
		{
			currentCompletionData = null;
		}

		void OnColumnWidthChanged (object o, GLib.NotifyArgs args)
		{
			if (!columnSizesUpdating && allowStoreColumnSizes) {
				StoreColumnSizes ();
			}
		}

		void AdjustColumnSizes ()
		{
			if (!Visible || Allocation.Width <= 0 || columnSizesUpdating || controller.CompactView)
				return;

			columnSizesUpdating = true;

			double width = (double)Allocation.Width;

			int texp = Math.Max ((int)(width * expColWidth), 1);
			if (texp != expCol.FixedWidth) {
				expCol.FixedWidth = texp;
			}

			if (typeCol.Visible) {
				int ttype = Math.Max ((int)(width * typeColWidth), 1);
				if (ttype != typeCol.FixedWidth) {
					typeCol.FixedWidth = ttype;
				}
			}

			int tval = Math.Max ((int)(width * valueColWidth), 1);

			if (tval != valueCol.FixedWidth) {
				valueCol.FixedWidth = tval;
				Application.Invoke ((o, args) => { QueueResize (); });
			}

			columnSizesUpdating = false;
			columnsAdjusted = true;
		}

		void StoreColumnSizes ()
		{
			if (!IsRealized || !Visible || !columnsAdjusted || controller.CompactView)
				return;

			double width = (double)Allocation.Width;
			expColWidth = ((double)expCol.Width) / width;
			valueColWidth = ((double)valueCol.Width) / width;
			if (typeCol.Visible)
				typeColWidth = ((double)typeCol.Width) / width;
		}

		void ResetColumnSizes ()
		{
			expColWidth = 0.3;
			valueColWidth = 0.5;
			typeColWidth = 0.2;
		}

		#endregion

		#region Cell renderers
		class CellRendererTextWithIcon : CellRendererText
		{
			IconId icon;

			[GLib.Property ("icon")]
			public string Icon {
				get {
					return icon;
				}
				set {
					icon = value;
				}
			}

			Xwt.Drawing.Image img {
				get {
					return ImageService.GetIcon (icon, IconSize.Menu);
				}
			}

			public override void GetSize (Widget widget, ref Gdk.Rectangle cell_area, out int x_offset, out int y_offset, out int width, out int height)
			{
				base.GetSize (widget, ref cell_area, out x_offset, out y_offset, out width, out height);
				if (!icon.IsNull)
					width += (int)(Xpad * 2 + img.Width);
			}

			protected override void Render (Gdk.Drawable window, Widget widget, Gdk.Rectangle background_area, Gdk.Rectangle cell_area, Gdk.Rectangle expose_area, CellRendererState flags)
			{
				base.Render (window, widget, background_area, cell_area, expose_area, flags);
				if (!icon.IsNull) {
					using (var ctx = Gdk.CairoHelper.Create (window)) {
						using (var layout = new Pango.Layout (widget.PangoContext)) {
							layout.FontDescription = IdeServices.FontService.SansFont.CopyModified (Ide.Gui.Styles.FontScale11);
							layout.FontDescription.Family = Family;
							layout.SetText (Text);
							int w, h;
							layout.GetPixelSize (out w, out h);
							var x = cell_area.X + w + 3 * Xpad;
							var y = cell_area.Y + cell_area.Height / 2 - (int)(img.Height / 2);
							ctx.DrawImage (widget, img, x, y);
						}
					}
				}
			}
		}

		class ValueCellRenderer : CellRendererText
		{
			public bool Compact;

			[GLib.Property ("texturl")]
			public string TextUrl {
				get {
					return Text;
				}
				set {
					Uri uri;

					try {
						if (value != null && Uri.TryCreate (value.Trim ('"', '{', '}'), UriKind.Absolute, out uri) && (uri.Scheme == "http" || uri.Scheme == "https")) {
							Underline = Pango.Underline.Single;
							Foreground = Ide.Gui.Styles.LinkForegroundColor.ToHexString (false);
						} else {
							Underline = Pango.Underline.None;
						}
					} catch (Exception) {
						// MONO BUG: Uri.TryCreate() throws when unicode characters are encountered. See bug #47364
						Underline = Pango.Underline.None;
					}

					Text = value;
				}
			}

			public override void GetSize (Widget widget, ref Gdk.Rectangle cell_area, out int x_offset, out int y_offset, out int width, out int height)
			{
				if (Compact)
					this.Ellipsize = Pango.EllipsizeMode.None;
				base.GetSize (widget, ref cell_area, out x_offset, out y_offset, out width, out height);
				if (Compact)
					this.Ellipsize = Pango.EllipsizeMode.End;
			}
		}

		class CellRendererColorPreview : CellRenderer
		{
			protected override void Render (Gdk.Drawable window, Widget widget, Gdk.Rectangle background_area, Gdk.Rectangle cell_area, Gdk.Rectangle expose_area, CellRendererState flags)
			{
				var darkColor = Color.WithIncreasedLight (-0.15);

				using (Cairo.Context cr = Gdk.CairoHelper.Create (window)) {
					double center_x = cell_area.X + Math.Round ((double)(cell_area.Width / 2d));
					double center_y = cell_area.Y + Math.Round ((double)(cell_area.Height / 2d));

					// TODO: VV: On retina this should be LineWidth = 0.5 and Arc size needs to match

					// @1x:
					cr.LineWidth = 1;
					cr.Arc (center_x, center_y, 5.5f, 0, 2 * Math.PI);

					cr.SetSourceRGBA (Color.Red, Color.Green, Color.Blue, 1);
					cr.FillPreserve ();
					cr.SetSourceRGBA (darkColor.Red, darkColor.Green, darkColor.Blue, 1);
					cr.Stroke ();
				}
			}

			public override void GetSize (Widget widget, ref Gdk.Rectangle cell_area, out int x_offset, out int y_offset, out int width, out int height)
			{
				x_offset = y_offset = 0;
				height = width = 16;
			}

			public Xwt.Drawing.Color Color { get; set; }
		}

		class CellRendererRoundedButton : CellRendererText
		{
			const int TopBottomPadding = 1;

			protected override void Render (Gdk.Drawable window, Widget widget, Gdk.Rectangle background_area, Gdk.Rectangle cell_area, Gdk.Rectangle expose_area, CellRendererState flags)
			{
				if (string.IsNullOrEmpty (Text)) {
					return;
				}
				using (var cr = Gdk.CairoHelper.Create (window)) {
					using (var layout = new Pango.Layout (widget.PangoContext)) {
						layout.SetText (Text);
						layout.FontDescription = FontDesc;
						layout.FontDescription.Family = Family;
						int w, h;
						layout.GetPixelSize (out w, out h);
						int xpad = (int)Xpad;
						cr.RoundedRectangle (
							cell_area.X + xpad + 0.5,
							cell_area.Y + TopBottomPadding + 0.5,
							w + (cell_area.Height - 2 * TopBottomPadding) - 1,
							cell_area.Height - TopBottomPadding * 2 - 1,
							(cell_area.Height - (TopBottomPadding * 2)) / 2);
						cr.LineWidth = 1;
						cr.SetSourceColor (Styles.ObjectValueTreeValuesButtonBackground.ToCairoColor ());
						cr.FillPreserve ();
						cr.SetSourceColor (Styles.ObjectValueTreeValuesButtonBorder.ToCairoColor ());
						cr.Stroke ();

						int YOffset = (cell_area.Height - h) / 2;
						if (((GtkObjectValueTreeView)widget).controller.CompactView && !Platform.IsWindows)
							YOffset += 1;
						cr.SetSourceColor (Styles.ObjectValueTreeValuesButtonText.ToCairoColor ());
						cr.MoveTo (cell_area.X + (cell_area.Height - TopBottomPadding * 2 + 1) / 2 + xpad,
								   cell_area.Y + YOffset);
						cr.ShowLayout (layout);
					}
				}
			}

			public override void GetSize (Widget widget, ref Gdk.Rectangle cell_area, out int x_offset, out int y_offset, out int width, out int height)
			{
				base.GetSize (widget, ref cell_area, out x_offset, out y_offset, out width, out height);
				x_offset = y_offset = 0;
				if (string.IsNullOrEmpty (Text)) {
					width = 0;
					height = 0;
					return;
				}
				using (var layout = new Pango.Layout (widget.PangoContext)) {
					layout.SetText (Text);
					layout.FontDescription = FontDesc;
					layout.FontDescription.Family = Family;
					int w, h;
					layout.GetPixelSize (out w, out h);
					width = w + (height - 2 * TopBottomPadding) + 2 * (int)Xpad;
				}
			}
		}

		#endregion

		//==================================================================================================================

		#region Locator methods
		static ObjectValueNode GetNodeAtIter (TreeIter iter, TreeModel model)
		{
			return (ObjectValueNode) model.GetValue (iter, ObjectNodeColumn);
		}

		static ObjectValue GetDebuggerObjectValueAtIter (TreeIter iter, TreeModel model)
		{
			// TODO: clean up, maybe even remove this method
			var node = GetNodeAtIter (iter, model);

			return node?.GetDebuggerObjectValue ();
		}

		ObjectValueNode GetNodeAtIter (TreeIter iter)
		{
			return (ObjectValueNode) store.GetValue (iter, ObjectNodeColumn);
		}

		ObjectValue GetDebuggerObjectValueAtIter (TreeIter iter)
		{
			return GetDebuggerObjectValueAtIter (iter, store);
		}

		TreePath GetTreePathForNode (ObjectValueNode node)
		{
			if (allNodes.TryGetValue (node, out TreeRowReference treeRef)) {
				if (treeRef.Valid ()) {
					return treeRef.Path;
				}
			}

			return null;
		}

		/// <summary>
		/// Returns true if the iter of a node and it's parent can be found given the path of the node
		/// </summary>
		bool GetTreeIterFromNode (ObjectValueNode node, out TreeIter iter, out TreeIter parentIter)
		{
			parentIter = TreeIter.Zero;
			iter = TreeIter.Zero;

			if (allNodes.TryGetValue (node, out TreeRowReference treeRef)) {
				if (treeRef.Valid ()) {
					if (store.GetIter (out iter, treeRef.Path)) {
						store.IterParent (out parentIter, iter);

						return true;
					}
				}
			}

			return false;
		}
		#endregion

	}
}

