//
// ObjectValueTreeViewController.cs
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
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using Mono.Debugging.Client;

using MonoDevelop.Core;
using MonoDevelop.Components;

using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Core.Imaging;

namespace MonoDevelop.Debugger
{
	public enum PreviewButtonIcon
	{
		None,
		Hidden,
		RowHover,
		Hover,
		Active,
		Selected,
	}

	public interface IObjectValueDebuggerService
	{
		bool CanQueryDebugger { get; }
		IStackFrame Frame { get; }
		Task<CompletionData> GetCompletionDataAsync (string expression, CancellationToken token);
	}

	[Flags]
	public enum ObjectValueTreeViewFlags
	{
		None                 = 0,
		AllowPinning         = 1 << 0,
		AllowPopupMenu       = 1 << 1,
		AllowSelection       = 1 << 2,
		CompactView          = 1 << 3,
		HeadersVisible       = 1 << 4,
		RootPinVisible       = 1 << 5,

		// Macros
		ObjectValuePadFlags  = AllowPopupMenu | AllowSelection | HeadersVisible,
		TooltipFlags         = AllowPinning | AllowPopupMenu | AllowSelection | CompactView | RootPinVisible,
		PinnedWatchFlags     = AllowPinning | AllowPopupMenu | CompactView,
		ExceptionCaughtFlags = AllowSelection | HeadersVisible
	}

	public class ObjectValueTreeViewController : IObjectValueDebuggerService
	{
		readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource ();
		public const int MaxEnumerableChildrenToFetch = 20;

		IObjectValueTreeView view;
		IDebuggerService debuggerService;
		bool allowWatchExpressions;
		bool allowEditing;
		bool allowExpanding = true;

		/// <summary>
		/// Holds a dictionary of tasks that are fetching children values of the given node
		/// </summary>
		readonly Dictionary<ObjectValueNode, Task<int>> childFetchTasks = new Dictionary<ObjectValueNode, Task<int>> ();

		/// <summary>
		/// Holds a dictionary of arbitrary objects for nodes that are currently "Evaluating" by the debugger
		/// When the node has completed evaluation ValueUpdated event will be fired, passing the given object
		/// </summary>
		readonly Dictionary<ObjectValueNode, object> evaluationWatches = new Dictionary<ObjectValueNode, object> ();

		/// <summary>
		/// Holds a dictionary of node paths and the values. Used to show values that have changed from one frame to the next.
		/// </summary>
		readonly Dictionary<string, CheckpointState> oldValues = new Dictionary<string, CheckpointState> ();

		public ObjectValueTreeViewController (bool allowWatchExpressions = false)
		{
			AllowWatchExpressions = allowWatchExpressions;
			Root = new RootObjectValueNode ();
		}

		public IDebuggerService Debugger {
			get {
				if (debuggerService == null) {
					debuggerService = OnGetDebuggerService ();
				}

				return debuggerService;
			}
		}

		public ObjectValueNode Root { get; private set; }

		public IStackFrame Frame { get; set; }

		/// <summary>
		/// Gets a value indicating whether the user should be able to edit values in the tree
		/// </summary>
		public bool AllowEditing {
			get => allowEditing;
			set {
				allowEditing = value;
				if (view != null) {
					view.AllowEditing = value;
				}
			}
		}

		/// <summary>
		/// Gets a value indicating whether or not the user should be able to expand nodes in the tree.
		/// </summary>
		public bool AllowExpanding {
			get => allowExpanding;
			set {
				allowExpanding = value;
				if (view != null) {
					view.AllowExpanding = value;
				}
			}
		}

		/// <summary>
		/// Gets a value indicating whether the user should be able to add watch expressions to the tree
		/// </summary>
		public bool AllowWatchExpressions {
			get; private set;
		}

		public PinnedWatch PinnedWatch {
			get { return view?.PinnedWatch; }
			set {
				if (view != null) {
					view.PinnedWatch = value;
				}
			}
		}

		public PinnedWatchLocation PinnedWatchLocation {
			get; set;
		}

		public bool CanQueryDebugger {
			get {
				return Debugger.IsConnected && Debugger.IsPaused;
			}
		}

		protected void ConfigureView (IObjectValueTreeView control)
		{
			view = control;

			view.AllowExpanding = AllowExpanding;
			view.PinnedWatch = PinnedWatch;

			view.NodeExpand += OnViewNodeExpand;
			view.NodeCollapse += OnViewNodeCollapse;
			view.NodeLoadMoreChildren += OnViewNodeLoadMoreChildren;
			view.ExpressionAdded += OnViewExpressionAdded;
			view.ExpressionEdited += OnViewExpressionEdited;
			view.NodeRefresh += OnViewNodeRefresh;
			view.NodeGetCanEdit += OnViewNodeCanEdit;
			view.NodeEditValue += OnViewNodeEditValue;
			view.NodeRemoved += OnViewNodeRemoved;
			view.NodePinned += OnViewNodePinned;
			view.NodeUnpinned += OnViewNodeUnpinned;
			view.NodeShowVisualiser += OnViewNodeShowVisualiser;
		}

		public GtkObjectValueTreeView GetGtkControl (ObjectValueTreeViewFlags flags)
		{
			if (view != null)
				throw new InvalidOperationException ("You can only get the control once for each controller instance");

			var control = new GtkObjectValueTreeView (this, this, AllowEditing, flags);

			ConfigureView (control);

			return control;
		}

		public MacObjectValueTreeView GetMacControl (ObjectValueTreeViewFlags flags)
		{
			if (view != null)
				throw new InvalidOperationException ("You can only get the control once for each controller instance");

			var control = new MacObjectValueTreeView (this, this, AllowEditing, flags);

			ConfigureView (control);

			return control;
		}

		public Control GetControl (ObjectValueTreeViewFlags flags)
		{
			if (Platform.IsMac)
				return GetMacControl (flags);

			return GetGtkControl (flags);
		}

		public void CancelAsyncTasks ()
		{
			cancellationTokenSource.Cancel ();
		}

		/// <summary>
		/// Clears the controller of nodes and resets the root to a new empty node
		/// </summary>
		public void ClearValues ()
		{
			((RootObjectValueNode) Root).Clear ();

			Runtime.RunInMainThread (() => {
				view.Cleared ();
			}).Ignore ();
		}

		/// <summary>
		/// Clear everything
		/// </summary>
		public void ClearAll ()
		{
			ClearEvaluationCompletionRegistrations ();
			ClearValues ();
		}

		/// <summary>
		/// Adds values to the root node, eg locals or watch expressions
		/// </summary>
		public void AddValue (ObjectValueNode value)
		{
			((RootObjectValueNode) Root).AddValue (value);
			RegisterNode (value);

			Runtime.RunInMainThread (() => {
				LoggingService.LogInfo ("Appending value '{0}' to tree view", value.Name);
				view.Appended (value);
			}).Ignore ();
		}

		/// <summary>
		/// Adds values to the root node, eg locals or watch expressions
		/// </summary>
		public void AddValues (IEnumerable<ObjectValueNode> values)
		{
			var nodes = values.ToList ();

			((RootObjectValueNode) Root).AddValues (nodes);

			foreach (var node in nodes)
				RegisterNode (node);

			Runtime.RunInMainThread (() => {
				view.Appended (nodes);
			}).Ignore ();
		}

		public async Task<CompletionData> GetCompletionDataAsync (string expression, CancellationToken token)
		{
			if (CanQueryDebugger && Frame != null) {
				// TODO: improve how we get at the underlying real stack frame
				return await DebuggingService.GetCompletionDataAsync (Frame.GetStackFrame (), expression, token);
			}

			return null;
		}

		void CreatePinnedWatch (string expression, int height)
		{
			var watch = new PinnedWatch ();

			if (PinnedWatch != null) {
				watch.Location = PinnedWatch.Location;
				watch.OffsetX = PinnedWatch.OffsetX;
				watch.OffsetY = PinnedWatch.OffsetY + height + 5;
			} else {
				watch.Location = PinnedWatchLocation;
				watch.OffsetX = -1; // means that the watch should be placed at the line coordinates defined by watch.Line
				watch.OffsetY = -1;
			}

			watch.Expression = expression;
			DebuggingService.PinnedWatches.Add (watch);
		}

		void RemovePinnedWatch ()
		{
			DebuggingService.PinnedWatches.Remove (PinnedWatch);
		}

		void RemoveValue (ObjectValueNode node)
		{
			var toplevel = node.Parent is RootObjectValueNode;
			int index;

			if (node.Parent != null) {
				index = node.Parent.Children.IndexOf (node);
			} else {
				index = -1;
			}

			UnregisterNode (node);
			OnEvaluationCompleted (node, new ObjectValueNode[0]);

			if (AllowWatchExpressions && toplevel && index != -1)
				ExpressionRemoved?.Invoke (this, new ExpressionRemovedEventArgs (index, node.Name));
		}

		// TODO: can we improve this
		public string GetDisplayValueWithVisualisers (ObjectValueNode node, out bool showViewerButton)
		{
			showViewerButton = false;
			if (node == null)
				return null;

			string result;
			showViewerButton = !node.IsNull && Debugger.HasValueVisualizers (node);

			if (!node.IsNull && Debugger.HasInlineVisualizer (node)) {
				try {
					result = node.GetInlineVisualisation ();
				} catch (Exception) {
					result = node.GetDisplayValue ();
				}
			} else {
				result = node.GetDisplayValue ();
			}

			return result;
		}

		#region Checkpoints
		public void ChangeCheckpoint ()
		{
			// clear old values,
			// iterate over all the nodes and store the values so we can compare
			// on the next update
			oldValues.Clear ();
			if (Root != null) {
				ChangeCheckpoint (Root);
			}
		}

		public void ResetChangeTracking ()
		{
			oldValues.Clear ();
		}

		/// <summary>
		/// Returns true if the value of the node is different from it's last value
		/// at the last checkpoint. Returns false if the node was not scanned at the
		/// last checkpoint
		/// </summary>
		public bool GetNodeHasChangedSinceLastCheckpoint (ObjectValueNode node)
		{
			if (oldValues.TryGetValue (node.Path, out CheckpointState checkpointState)) {
				return node.Value != checkpointState.Value;
			}

			return false;
		}

		/// <summary>
		/// Returns true if the node was expanded when the last checkpoint was made
		/// </summary>
		public bool GetNodeWasExpandedAtLastCheckpoint (ObjectValueNode node)
		{
			if (oldValues.TryGetValue (node.Path, out CheckpointState checkpointState)) {
				return checkpointState.Expanded;
			}

			return false;
		}
		#endregion

		#region Expressions

		public event EventHandler<ExpressionAddedEventArgs> ExpressionAdded;
		public event EventHandler<ExpressionChangedEventArgs> ExpressionChanged;
		public event EventHandler<ExpressionRemovedEventArgs> ExpressionRemoved;

		public void AddExpression (string expression)
		{
			if (!AllowWatchExpressions) {
				LoggingService.LogInfo ("Watch expressions not allowed.");
				return;
			}

			LoggingService.LogInfo ("Evaluating expression '{0}'", expression);

			ObjectValueNode node;
			if (Frame != null) {
				node = Frame.EvaluateExpression (expression);
			} else {
				var value = ObjectValue.CreateUnknown (expression);
				node = new DebuggerObjectValueNode (value);
			}

			AddValue (node);

			ExpressionAdded?.Invoke (this, new ExpressionAddedEventArgs (expression));
		}

		public void AddExpressions (IList<string> expressions)
		{
			if (!AllowWatchExpressions)
				return;

			if (Frame != null) {
				var nodes = Frame.EvaluateExpressions (expressions);
				AddValues (nodes);

				var expressionAdded = ExpressionAdded;
				if (expressionAdded != null) {
					foreach (var expression in expressions)
						expressionAdded (this, new ExpressionAddedEventArgs (expression));
				}
			}
		}

		bool EditExpression (ObjectValueNode node, string newExpression)
		{
			var oldExpression = node.Name;

			if (oldExpression == newExpression)
				return false;

			int index = node.Parent.Children.IndexOf (node);

			UnregisterNode (node);
			if (string.IsNullOrEmpty (newExpression)) {
				// we want the expression removed from the tree
				OnEvaluationCompleted (node, new ObjectValueNode[0]);
				ExpressionRemoved?.Invoke (this, new ExpressionRemovedEventArgs (index, oldExpression));
				return true;
			}

			var expressionNode = Frame.EvaluateExpression (newExpression);
			RegisterNode (expressionNode);
			OnEvaluationCompleted (node, new ObjectValueNode[] { expressionNode });
			ExpressionChanged?.Invoke (this, new ExpressionChangedEventArgs (index, oldExpression, newExpression));

			return true;
		}

		public void ReEvaluateExpressions ()
		{
			if (!AllowWatchExpressions)
				return;

			foreach (var node in Root.Children)
				node.Refresh ();
		}
		#endregion

		/// <summary>
		/// Returns true if the node can be edited
		/// </summary>
		bool CanEditObject (ObjectValueNode node)
		{
			if (AllowEditing) {
				if (node.IsUnknown) {
					if (Frame != null) {
						return false;
					}
				}

				return node.CanEdit;
			}

			return false;
		}

		/// <summary>
		/// Edits the value of the node and returns a value indicating whether the node's value changed from
		/// when the node was initially loaded from the debugger
		/// </summary>
		bool EditNodeValue (ObjectValueNode node, string newValue)
		{
			if (node == null || !AllowEditing)
				return false;

			try {
				if (node.Value == newValue)
					return false;

				// make sure we set an old value for this node so we can show that it has changed
				if (!oldValues.TryGetValue (node.Path, out CheckpointState state)) {
					oldValues[node.Path] = new CheckpointState (node);
				}

				// ensure the parent and node are in the checkpoint and expanded
				// so that the tree expands the node we just edited when refreshed
				EnsureNodeIsExpandedInCheckpoint (node);

				node.SetValue (newValue);
			} catch (Exception ex) {
				LoggingService.LogError ($"Could not set value for object '{node.Name}'", ex);
				return false;
			}

			// now, refresh the parent
			var parent = node.Parent; /*FindNode (node.ParentId);*/
			if (parent != null) {
				parent.Refresh ();
				RegisterForEvaluationCompletion (parent, true);
			}

			// the locals pad, for example, will reload all the values once this is fired
			// prior to reloading, a new checkpoint will be made
			Debugger.NotifyVariableChanged ();

			return true;
		}

		bool ShowNodeValueVisualizer (ObjectValueNode node)
		{
			if (node != null) {

				// make sure we set an old value for this node so we can show that it has changed
				if (!oldValues.TryGetValue (node.Path, out CheckpointState state)) {
					oldValues[node.Path] = new CheckpointState (node);
				}

				// ensure the parent and node are in the checkpoint and expanded
				// so that the tree expands the node we just edited when refreshed
				EnsureNodeIsExpandedInCheckpoint (node);

				if (Debugger.ShowValueVisualizer (node)) {
					// the value of the node changed so now refresh the parent
					var parent = node.Parent; /*FindNode (node.ParentId);*/
					if (parent != null) {
						parent.Refresh ();
						RegisterForEvaluationCompletion (parent, true);
					}

					return true;
				}
			}

			return false;
		}

		void EnsureNodeIsExpandedInCheckpoint (ObjectValueNode node)
		{
			var parent = node.Parent; /*FindNode (node.ParentId);*/

			while (parent != null && parent != Root) {
				if (oldValues.TryGetValue (parent.Path, out CheckpointState state)) {
					state.Expanded = true;
				} else {
					oldValues[parent.Path] = new CheckpointState (parent) { Expanded = true };
				}

				parent = parent.Parent; /*FindNode (parent.ParentId);*/
			}
		}

		void RefreshNode (ObjectValueNode node)
		{
			if (node == null)
				return;

			if (CanQueryDebugger && Frame != null) {
				UnregisterForEvaluationCompletion (node);

				var options = Frame.CloneSessionEvaluationOpions ();
				options.AllowMethodEvaluation = true;
				options.AllowToStringCalls = true;
				options.AllowTargetInvoke = true;
				options.EllipsizeStrings = false;

				node.Refresh (options);

				RegisterForEvaluationCompletion (node);
			}
		}

		#region View event handlers
		void OnViewNodeExpand (object sender, ObjectValueNodeEventArgs e)
		{
			ExpandNodeAsync (e.Node).Ignore ();
		}

		void OnViewNodeCollapse (object sender, ObjectValueNodeEventArgs e)
		{
			e.Node.IsExpanded = false;
		}

		void OnViewNodeLoadMoreChildren (object sender, ObjectValueNodeEventArgs e)
		{
			FetchMoreChildrenAsync (e.Node).Ignore ();
		}

		void OnViewExpressionAdded (object sender, ObjectValueExpressionEventArgs e)
		{
			LoggingService.LogInfo ("ObjectValueTreeViewController.OnViewExpressionAdded");
			AddExpression (e.Expression);
		}

		void OnViewExpressionEdited (object sender, ObjectValueExpressionEventArgs e)
		{
			EditExpression (e.Node, e.Expression);
		}

		void OnViewNodeRefresh (object sender, ObjectValueNodeEventArgs e)
		{
			RefreshNode (e.Node);
		}

		void OnViewNodeCanEdit (object sender, ObjectValueNodeEventArgs e)
		{
			e.Response = CanEditObject (e.Node);
		}

		void OnViewNodeEditValue (object sender, ObjectValueEditEventArgs e)
		{
			e.Response = EditNodeValue (e.Node, e.NewValue);
		}

		void OnViewNodeRemoved (object sender, ObjectValueNodeEventArgs e)
		{
			RemoveValue (e.Node);
		}

		void OnViewNodeShowVisualiser (object sender, ObjectValueNodeEventArgs e)
		{
			e.Response = ShowNodeValueVisualizer (e.Node);
		}

		void OnViewNodePinned (object sender, ObjectValueNodeEventArgs e)
		{
			CreatePinnedWatch (e.Node.Expression, view.PinnedWatchOffset);
		}

		void OnViewNodeUnpinned (object sender, EventArgs e)
		{
			RemovePinnedWatch ();
		}

		#endregion

		#region Fetching and loading children
		/// <summary>
		/// Marks a node as expanded and fetches children for the node if they have not been already fetched
		/// </summary>
		async Task ExpandNodeAsync (ObjectValueNode node)
		{
			// if we think the node is expanded already, no need to trigger this again
			if (node.IsExpanded)
				return;

			node.IsExpanded = true;

			int index = node.Children.Count;
			int count = 0;

			if (node.IsEnumerable) {
				// if we already have some loaded, don't load more - that is a specific user gesture
				if (index == 0) {
					// page the children in, instead of loading them all at once
					count = await FetchChildrenAsync (node, MaxEnumerableChildrenToFetch, cancellationTokenSource.Token);
				}
			} else {
				count = await FetchChildrenAsync (node, -1, cancellationTokenSource.Token);
			}

			await Runtime.RunInMainThread (() => {
				// tell the view about the children, even if there are, in fact, none
				view.LoadNodeChildren (node, index, count);
				view.OnNodeExpanded (node);
			});
		}

		async Task<int> FetchMoreChildrenAsync (ObjectValueNode node)
		{
			if (node.ChildrenLoaded) {
				return 0;
			}

			try {
				if (childFetchTasks.TryGetValue (node, out Task<int> task)) {
					// there is already a task to fetch the children
					return await task;
				}

				try {
					var oldCount = node.Children.Count;
					var result = await node.LoadChildrenAsync (MaxEnumerableChildrenToFetch, cancellationTokenSource.Token);

					// if any of them are still evaluating register for
					// a completion event so that we can tell the UI
					for (int i = oldCount; i < oldCount + result; i++) {
						var c = node.Children[i];
						RegisterNode (c);
					}

					// always send the event so that the UI can determine if the node has finished loading.
					OnChildrenLoaded (node, oldCount, result);

					return result;
				} finally {
					childFetchTasks.Remove (node);
				}
			} catch (Exception ex) {
				LoggingService.LogInternalError (ex);
			}

			return 0;
		}

		/// <summary>
		/// Fetches the child nodes and returns the count of new children that were loaded.
		/// The children will be in node.Children.
		/// </summary>
		async Task<int> FetchChildrenAsync (ObjectValueNode node, int count, CancellationToken cancellationToken)
		{
			if (node.ChildrenLoaded) {
				return 0;
			}

			try {
				if (childFetchTasks.TryGetValue (node, out Task<int> task)) {
					// there is already a task to fetch the children
					return await task;
				}

				try {
					int result = 0;
					if (count > 0) {
						var oldCount = node.Children.Count;
						result = await node.LoadChildrenAsync (count, cancellationToken);

						// if any of them are still evaluating register for
						// a completion event so that we can tell the UI
						for (int i = oldCount; i < oldCount + result; i++) {
							var c = node.Children[i];
							RegisterNode (c);
						}
					} else {
						result = await node.LoadChildrenAsync (cancellationToken);

						// if any of them are still evaluating register for
						// a completion event so that we can tell the UI
						foreach (var c in node.Children) {
							RegisterNode (c);
						}
					}

					return result;
				} finally {
					childFetchTasks.Remove (node);
				}
			} catch (Exception ex) {
				LoggingService.LogInternalError (ex);
			}

			return 0;
		}
		#endregion

		#region Evaluation watches
		/// <summary>
		/// Registers the ValueChanged event for a node where IsEvaluating is true. If the node is not evaluating, and
		/// sendImmediatelyIfNotEvaluating is true, then fire OnEvaluatingNodeValueChanged immediately 
		/// </summary>
		void RegisterForEvaluationCompletion (ObjectValueNode node, bool sendImmediatelyIfNotEvaluating = false)
		{
			if (node.IsEvaluating) {
				evaluationWatches[node] = null;
				node.ValueChanged += OnEvaluatingNodeValueChanged;
			} else if (sendImmediatelyIfNotEvaluating) {
				OnEvaluatingNodeValueChanged (node, EventArgs.Empty);
			}
		}

		/// <summary>
		/// Removes the ValueChanged handler from the node
		/// </summary>
		void UnregisterForEvaluationCompletion (ObjectValueNode node)
		{
			if (node != null) {
				node.ValueChanged -= OnEvaluatingNodeValueChanged;
				evaluationWatches.Remove (node);
			}
		}

		/// <summary>
		/// Removes all ValueChanged handlers for evaluating nodes
		/// </summary>
		void ClearEvaluationCompletionRegistrations ()
		{
			foreach (var node in evaluationWatches.Keys) {
				node.ValueChanged -= OnEvaluatingNodeValueChanged;
			}

			evaluationWatches.Clear ();
		}

		#endregion

		protected virtual IDebuggerService OnGetDebuggerService ()
		{
			return new ObjectValueDebuggerService ();
		}

		/// <summary>
		/// Registers the node in the index and sets a watch for evaluating nodes
		/// </summary>
		void RegisterNode (ObjectValueNode node)
		{
			if (node != null) {
				RegisterForEvaluationCompletion (node);
			}
		}

		void UnregisterNode (ObjectValueNode node)
		{
			if (node != null) {
				UnregisterForEvaluationCompletion (node);
			}
		}

		/// <summary>
		/// Creates a checkpoint of the value of the node and any children that are expanded
		/// </summary>
		void ChangeCheckpoint (ObjectValueNode node)
		{
			oldValues[node.Path] = new CheckpointState (node);

			if (node.IsExpanded) {
				foreach (var child in node.Children) {
					ChangeCheckpoint (child);
				}
			}
		}

		#region Event triggers
		void OnChildrenLoaded (ObjectValueNode node, int index, int count)
		{
			Runtime.RunInMainThread (() => {
				view.LoadNodeChildren (node, index, count);
			}).Ignore ();
		}

		/// <summary>
		/// Triggered in response to ValueChanged on a node
		/// </summary>
		void OnEvaluatingNodeValueChanged (object sender, EventArgs e)
		{
			if (sender is ObjectValueNode node) {
				UnregisterForEvaluationCompletion (node);

				if (sender is IEvaluatingGroupObjectValueNode evalGroupNode) {
					if (evalGroupNode.IsEvaluatingGroup) {
						var replacementNodes = evalGroupNode.GetEvaluationGroupReplacementNodes ();

						foreach (var newNode in replacementNodes) {
							RegisterNode (newNode);
						}

						OnEvaluationCompleted (sender as ObjectValueNode, replacementNodes);
					} else {
						OnEvaluationCompleted (sender as ObjectValueNode);
					}
				} else {
					OnEvaluationCompleted (sender as ObjectValueNode);
				}
			}
		}

		void OnEvaluationCompleted (ObjectValueNode node)
		{
			Runtime.RunInMainThread (() => {
				view.LoadEvaluatedNode (node, new ObjectValueNode[] { node });
			}).Ignore ();
		}

		void OnEvaluationCompleted (ObjectValueNode node, ObjectValueNode[] replacementNodes)
		{
			// `node` returns us a set of new nodes that need to be replaced into the children
			// of node.parent. This should only be applicable to direct children of the root since
			// this construct is to support placehold values for "locals" etc
			if (node.Parent is ISupportChildObjectValueNodeReplacement replacerParent) {
				replacerParent.ReplaceChildNode (node, replacementNodes);
			}

			Runtime.RunInMainThread (() => {
				view.LoadEvaluatedNode (node, replacementNodes);
			}).Ignore ();
		}
		#endregion

		class CheckpointState
		{
			public CheckpointState (ObjectValueNode node)
			{
				Expanded = node.IsExpanded;
				Value = node.Value;
			}

			public bool Expanded { get; set; }
			public string Value { get; set; }
		}

		public static string GetIcon (ObjectValueFlags flags)
		{
			if ((flags & ObjectValueFlags.Field) != 0 && (flags & ObjectValueFlags.ReadOnly) != 0)
				return "md-literal";

			string global = (flags & ObjectValueFlags.Global) != 0 ? "static-" : string.Empty;
			string source;

			switch (flags & ObjectValueFlags.OriginMask) {
			case ObjectValueFlags.Property: source = "property"; break;
			case ObjectValueFlags.Type: source = "class"; global = string.Empty; break;
			case ObjectValueFlags.Method: source = "method"; break;
			case ObjectValueFlags.Literal: return "md-literal";
			case ObjectValueFlags.Namespace: return "md-name-space";
			case ObjectValueFlags.Group: return "md-open-resource-folder";
			case ObjectValueFlags.Field: source = "field"; break;
			case ObjectValueFlags.Variable: return "md-variable";
			default: return "md-empty";
			}

			string access;
			switch (flags & ObjectValueFlags.AccessMask) {
			case ObjectValueFlags.Private: access = "private-"; break;
			case ObjectValueFlags.Internal: access = "internal-"; break;
			case ObjectValueFlags.InternalProtected:
			case ObjectValueFlags.Protected: access = "protected-"; break;
			default: access = string.Empty; break;
			}

			return "md-" + access + global + source;
		}

		internal static string GetAccessibilityTitleForIcon (ObjectValueFlags flags, string defaultTitle = null)
		{
			if ((flags & ObjectValueFlags.Field) != 0 && (flags & ObjectValueFlags.ReadOnly) != 0)
				return GettextCatalog.GetString ("Literal");

			string global = (flags & ObjectValueFlags.Global) != 0 ? GettextCatalog.GetString ("Static") : string.Empty;
			string source;

			switch (flags & ObjectValueFlags.OriginMask) {
			case ObjectValueFlags.Property: source = GettextCatalog.GetString ("Property"); break;
			case ObjectValueFlags.Type: source = GettextCatalog.GetString ("Class"); global = string.Empty; break;
			case ObjectValueFlags.Method: source = GettextCatalog.GetString ("Method"); break;
			case ObjectValueFlags.Literal: return GettextCatalog.GetString ("Literal");
			case ObjectValueFlags.Namespace: return GettextCatalog.GetString ("Namespace");
			case ObjectValueFlags.Group: return GettextCatalog.GetString ("Open Resource Folder");
			case ObjectValueFlags.Field: source = GettextCatalog.GetString ("Field"); break;
			case ObjectValueFlags.Variable: return GettextCatalog.GetString ("Variable");
			default: return defaultTitle;
			}

			string access;
			switch (flags & ObjectValueFlags.AccessMask) {
			case ObjectValueFlags.Private: access = GettextCatalog.GetString ("Private"); break;
			case ObjectValueFlags.Internal: access = GettextCatalog.GetString ("Internal"); break;
			case ObjectValueFlags.InternalProtected:
			case ObjectValueFlags.Protected: access = GettextCatalog.GetString ("Protected"); break;
			default: access = string.Empty; break;
			}

			return access + " " + global + " " + source;
		}

		internal static string GetAccessibilityTitleForIcon (string iconName, string defaultTitle)
		{
			switch (iconName) {
			case "md-warning":
				return GettextCatalog.GetString ("Warning");
			default:
				return defaultTitle;
			}
		}

		static int GetKnownImageId (ObjectValueFlags flags)
		{
			var name = GetIcon (flags);

			switch (name) {
			case "md-empty": return -1;
			case "md-literal": return KnownImageIds.Literal;
			case "md-name-space": return KnownImageIds.Namespace;
			case "md-variable": return KnownImageIds.LocalVariable;

			case "md-property": return KnownImageIds.PropertyPublic;
			case "md-method": return KnownImageIds.MethodPublic;
			case "md-class": return KnownImageIds.ClassPublic;
			case "md-field": return KnownImageIds.FieldPublic;

			case "md-private-property": return KnownImageIds.PropertyPrivate;
			case "md-private-method": return KnownImageIds.MethodPrivate;
			case "md-private-class": return KnownImageIds.ClassPrivate;
			case "md-private-field": return KnownImageIds.FieldPrivate;

			case "md-internal-property": return KnownImageIds.PropertyInternal;
			case "md-internal-method": return KnownImageIds.MethodInternal;
			case "md-internal-class": return KnownImageIds.ClassInternal;
			case "md-internal-field": return KnownImageIds.FieldInternal;

			case "md-protected-property": return KnownImageIds.PropertyProtected;
			case "md-protected-method": return KnownImageIds.MethodProtected;
			case "md-protected-class": return KnownImageIds.ClassProtected;
			case "md-protected-field": return KnownImageIds.FieldProtected;

			case "md-private-static-property": return KnownImageIds.PropertyPrivate;
			case "md-private-static-method": return KnownImageIds.MethodPrivate;
			case "md-private-static-field": return KnownImageIds.FieldPrivate;

			case "md-internal-static-property": return KnownImageIds.PropertyInternal;
			case "md-internal-static-method": return KnownImageIds.MethodInternal;
			case "md-internal-static-field": return KnownImageIds.FieldInternal;

			case "md-protected-static-property": return KnownImageIds.PropertyProtected;
			case "md-protected-static-method": return KnownImageIds.MethodProtected;
			case "md-protected-static-field": return KnownImageIds.FieldProtected;

			default:
				LoggingService.LogWarning ("Unknown Debugger ImageId: {0}", name);
				return -1;
			}
		}

		public static ImageId GetImageId (ObjectValueFlags flags)
		{
			int id = GetKnownImageId (flags);

			return id == -1 ? default : new ImageId (KnownImageIds.ImageCatalogGuid, id);
		}

		public static string GetPreviewButtonIcon (PreviewButtonIcon icon)
		{
			switch (icon) {
			case PreviewButtonIcon.Hidden: return "md-empty";
			case PreviewButtonIcon.RowHover: return "md-preview-normal";
			case PreviewButtonIcon.Hover: return "md-preview-hover";
			case PreviewButtonIcon.Active: return "md-preview-active";
			case PreviewButtonIcon.Selected: return "md-preview-selected";
			default: return null;
			}
		}
	}

	#region Extension methods and helpers
	/// <summary>
	/// Helper class to mimic existing API
	/// </summary>
	public static class ObjectValueTreeViewControllerExtensions
	{
		public static void SetStackFrame (this ObjectValueTreeViewController controller, StackFrame frame)
		{
			controller.Frame = new ProxyStackFrame (frame);
		}

		public static StackFrame GetStackFrame (this ObjectValueTreeViewController controller)
		{
			return (controller.Frame as ProxyStackFrame)?.StackFrame;
		}

		public static StackFrame GetStackFrame (this IStackFrame frame)
		{
			return (frame as ProxyStackFrame)?.StackFrame;
		}

		public static void AddValue (this ObjectValueTreeViewController controller, ObjectValue value)
		{
			controller.AddValue (new DebuggerObjectValueNode (value));
		}

		public static void AddValues (this ObjectValueTreeViewController controller, IEnumerable<ObjectValue> values)
		{
			controller.AddValues (values.Select (value => new DebuggerObjectValueNode (value)));
		}

		public static string[] GetExpressions (this ObjectValueTreeViewController controller)
		{
			// given that expressions are only supported by themselves (ie not mixed with locals for example)
			// and they are all children of the root, we can mimic a list of expressions by just grabbing the
			// name property of the root children
			if (controller.Root == null)
				return new string[0];

			return controller.Root.Children.Select (c => c.Name).ToArray ();
		}
	}

	public static class ObjectValueNodeExtensions
	{
		public static string GetDisplayValue (this ObjectValueNode node)
		{
			if (node.DisplayValue == null)
				return "(null)";

			if (node.DisplayValue.Length > 1000) {
				// Truncate the string to stop the UI from hanging
				// when calculating the size for very large amounts
				// of text.
				return node.DisplayValue.Substring (0, 1000) + "…";
			}

			return node.DisplayValue;
		}

		public static ObjectValue GetDebuggerObjectValue (this ObjectValueNode node)
		{
			if (node != null && node is DebuggerObjectValueNode val) {
				return val.DebuggerObject;
			}

			return null;
		}

		public static bool GetIsEvaluatingGroup (this ObjectValueNode node)
		{
			return (node is IEvaluatingGroupObjectValueNode evg && evg.IsEvaluatingGroup);
		}

		public static string GetInlineVisualisation (this ObjectValueNode node)
		{
			// TODO: this is not possible to mock as it is
			if (node is DebuggerObjectValueNode val) {
				return DebuggingService.GetInlineVisualizer (val.DebuggerObject).InlineVisualize (val.DebuggerObject);
			}

			return node.GetDisplayValue ();
		}
	}
	#endregion
}
