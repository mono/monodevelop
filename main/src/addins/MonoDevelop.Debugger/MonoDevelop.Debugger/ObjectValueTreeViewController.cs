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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

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
	public class ObjectValueTreeViewController
	{
		/// <summary>
		/// Holds a dictionary of tasks that are fetching children values of the given node
		/// </summary>
		readonly Dictionary<IObjectValueNode, Task> childFetchTasks = new Dictionary<IObjectValueNode, Task> ();

		/// <summary>
		/// Holds a dictionary of arbitrary objects for nodes that are currently "Evaluating" by the debugger
		/// When the node has completed evaluation ValueUpdated event will be fired, passing the given object
		/// </summary>
		readonly Dictionary<IObjectValueNode, object> evaluationWatches = new Dictionary<IObjectValueNode, object> ();

		public ObjectValueTreeViewController ()
		{
		}

		public IDebuggerService Debugger { get; private set; }
		public IObjectValueNode Root { get; private set; }

		public IStackFrame Frame { get; set; }
		public bool AllowEditing { get; set; }
		public bool AllowAdding { get; set; }


		public bool CanQueryDebugger {
			get {
				this.EnsureDebuggerService ();
				return this.Debugger.IsConnected && this.Debugger.IsPaused;
			}
		}



		public event EventHandler<ChildrenChangedEventArgs> ChildrenChanged;

		public event EventHandler<NodeEvaluationCompletedEventArgs> EvaluationCompleted;

		public object GetControl()
		{
			return new GtkObjectValueTreeView (this);
		}

		/// <summary>
		/// Clears the controller of nodes and resets the root to a new empty node
		/// </summary>
		public void ClearValues()
		{
			this.Root = this.OnCreateRoot ();
			this.OnChildrenChanged (this.Root, false);
		}

		/// <summary>
		/// Adds values to the root node, eg locals or watch expressions
		/// </summary>
		public void AddValues(IEnumerable<IObjectValueNode> values)
		{
			if (this.Root == null) {
				this.Root = this.OnCreateRoot ();
			}

			var allNodes = values.ToList ();
			((RootObjectValueNode)this.Root).AddValues (allNodes);

			// TODO: we want to enumerate just the once
			foreach (var x in allNodes) {
				this.RegisterForEvaluationCompletion (x);
			}

			this.OnChildrenChanged (this.Root, false);
		}

		public void ChangeCheckpoint ()
		{
		}


		public void ResetChangeTracking () { }

		/// <summary>
		/// Clear everything
		/// </summary>
		public void ClearAll ()
		{
			this.ClearEvaluationCompletionRegistrations ();
			this.ClearValues ();
		}

		#region Expanding children Tasks
		/// <summary>
		/// Fetches the child nodes and executes the completion handler once fetched
		/// </summary>
		public void FetchChildren(IObjectValueNode node, CancellationToken cancellationToken, bool expandOnCompletion)
		{
			if (!childFetchTasks.TryGetValue (node, out Task task)) {
				// fetch the child of the node
				task = node.LoadChildrenAsync (cancellationToken).ContinueWith (t => {
					try {
						if (!t.IsFaulted && !t.IsCanceled) {
							var allChildren = t.Result.ToList ();

							foreach (var c in allChildren) {
								this.RegisterForEvaluationCompletion (c);
							}

							this.OnChildrenChanged (node, expandOnCompletion);
						}
					} finally {
						childFetchTasks.Remove (node);
					}
				}, cancellationToken, TaskContinuationOptions.None, Xwt.Application.UITaskScheduler);

				childFetchTasks.Add (node, task);
			}
		}
		#endregion

		#region Evaluation watches

		/// <summary>
		/// Registers the ValueChanged event for a node where IsEvaluating is true
		/// </summary>
		void RegisterForEvaluationCompletion(IObjectValueNode node)
		{
			if (node != null && node.IsEvaluating) {
				this.evaluationWatches [node] = null;
				node.ValueChanged += OnEvaluatingNodeValueChanged;
			}
		}

		/// <summary>
		/// Removes the ValueChanged handler from the node
		/// </summary>
		void UnregisterForEvaluationCompletion (IObjectValueNode node)
		{
			if (node != null) {
				node.ValueChanged -= OnEvaluatingNodeValueChanged;
				this.evaluationWatches.Remove (node);
			}
		}

		/// <summary>
		/// Removes all ValueChanged handlers for evaluating nodes
		/// </summary>
		void ClearEvaluationCompletionRegistrations()
		{
			foreach (var node in this.evaluationWatches.Keys) {
				node.ValueChanged -= OnEvaluatingNodeValueChanged;
			}

			this.evaluationWatches.Clear ();
		}

		#endregion


		/// <summary>
		/// Called when clearing, by default sets the root to a new ObjectValueNode
		/// </summary>
		protected virtual IObjectValueNode OnCreateRoot ()
		{
			return new RootObjectValueNode ();
		}

		protected virtual IDebuggerService OnGetDebuggerService()
		{
			return new ProxyDebuggerService ();
		}

		void EnsureDebuggerService()
		{
			if (this.Debugger == null) {
				this.Debugger = this.OnGetDebuggerService ();
			}
		}

		void OnChildrenChanged (IObjectValueNode node, bool expandOnCompletion)
		{
			ChildrenChanged?.Invoke (this, new ChildrenChangedEventArgs (node, expandOnCompletion));
		}

		/// <summary>
		/// Triggered in response to ValueChanged on a node
		/// </summary>
		void OnEvaluatingNodeValueChanged (object sender, EventArgs e)
		{
			if (sender is IObjectValueNode node) {
				this.UnregisterForEvaluationCompletion (node);
			}

			this.OnEvaluationCompleted (sender as IObjectValueNode);

			// TODO: if is evaluating group, fetch the children
		}

		void OnEvaluationCompleted (IObjectValueNode node)
		{
			EvaluationCompleted?.Invoke (this, new NodeEvaluationCompletedEventArgs (node));
		}

	}

	#region Extension methods and helpers
	/// <summary>
	/// Helper class to mimic existing API
	/// </summary>
	public static class ObjectValueTreeViewControllerExtensions
	{
		public static void SetStackFrame(this ObjectValueTreeViewController controller, StackFrame frame)
		{
			controller.Frame = new ProxyStackFrame (frame);
		}

		public static StackFrame GetStackFrame (this ObjectValueTreeViewController controller)
		{
			return (controller.Frame as ProxyStackFrame)?.StackFrame;
		}


		public static void AddValues (this ObjectValueTreeViewController controller, IEnumerable<ObjectValue> values)
		{
			controller.AddValues (values.Select (x => new ObjectValueNode (x, controller.Root.Path)));
		}

	}

	public static class ObjectValueNodeExtensions
	{
		public static string GetDisplayValue(this IObjectValueNode node)
		{
			if (node.DisplayValue == null)
				return "(null)";

			if (node.DisplayValue.Length > 1000)
				// Truncate the string to stop the UI from hanging
				// when calculating the size for very large amounts
				// of text.
				return node.DisplayValue.Substring (0, 1000) + "…";

			return node.DisplayValue;
		}

		public static ObjectValue GetDebuggerObjectValue(this IObjectValueNode node)
		{
			if (node is ObjectValueNode val) {
				return val.DebuggerObject;
			}

			return null;
		}
	}
	#endregion
}
