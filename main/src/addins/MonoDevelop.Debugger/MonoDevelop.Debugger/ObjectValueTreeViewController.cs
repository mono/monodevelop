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
	public interface IDebuggerService
	{
		bool IsConnected { get; }
		bool IsPaused { get; }
	}

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
		/// Adds values to the root node
		/// </summary>
		public void AddValues(IEnumerable<IObjectValueNode> values)
		{
			if (this.Root == null) {
				this.Root = this.OnCreateRoot ();
			}

			var allNodes = values.ToList ();
			this.Root.AddValues (allNodes);

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

	public sealed class ProxyDebuggerService : IDebuggerService
	{
		public bool IsConnected => DebuggingService.IsConnected;

		public bool IsPaused => DebuggingService.IsPaused;
	}

	public interface IObjectValueNode
	{
		/// <summary>
		/// Gets the path of the node from the root.
		/// </summary>
		string Path { get; }

		/// <summary>
		/// Gets or sets a value indicating whether the node is expanded
		/// </summary>
		bool IsExpanded { get; set; }



		string Name { get; }
		bool HasChildren { get; }
		bool IsEvaluating { get; }

		IEnumerable<IObjectValueNode> Children { get; }
		bool IsUnknown { get; }
		bool IsReadOnly { get; }
		bool IsError { get; }
		bool IsNotSupported { get; }
		string Value { get; }
		bool IsImplicitNotSupported { get; }
		bool IsEvaluatingGroup { get; }
		ObjectValueFlags Flags { get; }
		bool IsNull { get; }
		bool IsPrimitive { get; }
		string TypeName { get; }
		string DisplayValue { get; }
		bool CanRefresh { get; }
		bool HasFlag (ObjectValueFlags flag);

		event EventHandler ValueChanged;

		void AddValues (IEnumerable<IObjectValueNode> values);
		Task<IEnumerable<IObjectValueNode>> LoadChildrenAsync (CancellationToken cancellationToken);
	}

	public interface IStackFrame
	{

	}

	public sealed class ObjectValueTreeViewStackFrame : IStackFrame
	{
		readonly StackFrame frame;

		public ObjectValueTreeViewStackFrame (StackFrame frame)
		{
			this.frame = frame;
		}

		public StackFrame StackFrame => this.frame;
	}

	/// <summary>
	/// Helper class to mimic existing API
	/// </summary>
	public static class ObjectValueTreeViewControllerExtensions
	{
		public static void SetStackFrame(this ObjectValueTreeViewController controller, StackFrame frame)
		{
			controller.Frame = new ObjectValueTreeViewStackFrame (frame);
		}

		public static StackFrame GetStackFrame (this ObjectValueTreeViewController controller)
		{
			return (controller.Frame as ObjectValueTreeViewStackFrame)?.StackFrame;
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

	/// <summary>
	/// Represents a node in a tree structure that holds ObjectValue from the debugger.
	/// </summary>
	public sealed class ObjectValueNode : AbstractObjectValueNode
	{
		bool childrenLoaded;

		public ObjectValueNode (ObjectValue value, string parentPath) : base (parentPath, value.Name)
		{
			this.DebuggerObject = value;

			value.ValueChanged += OnDebuggerValueChanged;
		}

		public ObjectValue DebuggerObject { get; }

		public override bool HasChildren => this.DebuggerObject.HasChildren;

		public override bool IsEvaluating => this.DebuggerObject.IsEvaluating;

		public override bool IsUnknown => this.DebuggerObject.IsUnknown;
		public override bool IsReadOnly => this.DebuggerObject.IsReadOnly;
		public override bool IsError => this.DebuggerObject.IsError;
		public override bool IsNotSupported => this.DebuggerObject.IsNotSupported;
		public override string Value => this.DebuggerObject.Value;
		public override bool IsImplicitNotSupported => this.DebuggerObject.IsImplicitNotSupported;
		public override bool IsEvaluatingGroup => this.DebuggerObject.IsEvaluatingGroup;
		public override ObjectValueFlags Flags => this.DebuggerObject.Flags;
		public override bool IsNull => this.DebuggerObject.IsNull;
		public override bool IsPrimitive => this.DebuggerObject.IsPrimitive;
		public override string TypeName => this.DebuggerObject.TypeName;
		public override string DisplayValue => this.DebuggerObject.DisplayValue;
		public override bool CanRefresh => this.DebuggerObject.CanRefresh;
		public override bool HasFlag (ObjectValueFlags flag) => this.DebuggerObject.HasFlag(flag);

		public override async Task<IEnumerable<IObjectValueNode>> LoadChildrenAsync (CancellationToken cancellationToken)
		{
			if (this.childrenLoaded) {
				return this.Children;
			}

			var childValues = await GetChildrenAsync (this.DebuggerObject, cancellationToken);

			this.childrenLoaded = true;
			this.ClearChildren ();
			this.AddValues (childValues.Select (x => new ObjectValueNode (x, this.Path)));

			return this.Children;
		}


		static Task<ObjectValue []> GetChildrenAsync (ObjectValue value, CancellationToken cancellationToken)
		{
			return Task.Factory.StartNew<ObjectValue []> (delegate (object arg) {
				try {
					return ((ObjectValue)arg).GetAllChildren ();
				} catch (Exception ex) {
					// Note: this should only happen if someone breaks ObjectValue.GetAllChildren()
					LoggingService.LogError ("Failed to get ObjectValue children.", ex);
					return new ObjectValue [0];
				}
			}, value, cancellationToken, TaskCreationOptions.None, TaskScheduler.Default);
		}

		private void OnDebuggerValueChanged (object sender, EventArgs e)
		{
			this.OnValueChanged (e);
		}

	}

	public abstract class NodeEventArgs : EventArgs
	{
		public NodeEventArgs (IObjectValueNode node)
		{
			this.Node = node;
		}

		public IObjectValueNode Node { get; }
	}

	public sealed class NodeEvaluationCompletedEventArgs : NodeEventArgs
	{
		public NodeEvaluationCompletedEventArgs (IObjectValueNode node) : base(node)
		{
		}
	}

	public sealed class ChildrenChangedEventArgs : NodeEventArgs
	{
		public ChildrenChangedEventArgs (IObjectValueNode node, bool expandOnCompletion) : base (node)
		{
			this.Expand = expandOnCompletion;
		}

		public bool Expand { get; }
	}

	public sealed class NodeChangedEventArgs : NodeEventArgs
	{
		public NodeChangedEventArgs (IObjectValueNode node) : base (node)
		{
		}
	}


	public abstract class AbstractObjectValueNode : IObjectValueNode
	{
		readonly List<IObjectValueNode> children = new List<IObjectValueNode> ();

		protected AbstractObjectValueNode (string parentPath, string name)
		{
			this.Name = name;
			if (parentPath.EndsWith("/", StringComparison.OrdinalIgnoreCase)) {
				this.Path = parentPath + name;
			} else {
				this.Path = parentPath + "/" + name;
			}
		}

		public string Path { get; }
		public string Name { get; }

		public virtual string DisplayValue => string.Empty;

		public virtual bool HasChildren => false;

		public virtual bool IsEvaluating => false;
		public virtual bool IsUnknown => false;
		public virtual bool IsReadOnly => false;
		public virtual bool IsError => false;
		public virtual bool IsNotSupported => false;
		public virtual string Value => string.Empty;
		public virtual bool IsImplicitNotSupported => false;
		public virtual bool IsEvaluatingGroup => false;
		public virtual ObjectValueFlags Flags => ObjectValueFlags.None;
		public virtual bool IsNull => false;
		public virtual bool IsPrimitive => false;
		public virtual string TypeName => string.Empty;
		public virtual bool CanRefresh => false;
		public virtual bool HasFlag (ObjectValueFlags flag) => false;

		public virtual bool IsExpanded { get; set; }

		public event EventHandler ValueChanged;

		public IEnumerable<IObjectValueNode> Children => this.children;

		public void AddValues (IEnumerable<IObjectValueNode> values)
		{
			this.children.AddRange (values);
		}

		public virtual Task<IEnumerable<IObjectValueNode>> LoadChildrenAsync (CancellationToken cancellationToken)
		{
			return Task.FromResult (Enumerable.Empty<IObjectValueNode> ());
		}

		protected void ClearChildren()
		{
			this.children.Clear ();
		}

		protected void OnValueChanged(EventArgs e)
		{
			this.ValueChanged?.Invoke (this, e);
		}
	}



	public sealed class RootObjectValueNode : AbstractObjectValueNode
	{
		public RootObjectValueNode () : base(string.Empty, string.Empty)
		{
		}

		public override bool HasChildren => true;
	}

	public abstract class DebugObjectValueNode : AbstractObjectValueNode
	{
		protected DebugObjectValueNode (string parentPath, string name) : base (parentPath, name)
		{
		}

		public override bool HasChildren => true;

		public override string Value => "none";
		public override string TypeName => "No Type";
		public override string DisplayValue => "dummy";
	}


	public sealed class FakeObjectValueNode : DebugObjectValueNode
	{
		public FakeObjectValueNode (string parentPath) : base (parentPath, "fake")
		{
		}

		public override bool HasChildren => true;

		public override string Value => "none";
		public override string TypeName => "No Type";
		public override string DisplayValue => "dummy";


		public override async Task<IEnumerable<IObjectValueNode>> LoadChildrenAsync (CancellationToken cancellationToken)
		{
			// TODO: do some sleeping...
			await Task.Delay (1000);

			this.ClearChildren ();
			this.AddValues (new [] { new FakeObjectValueNode (this.Path) });
			return this.Children;
		}
	}

	public sealed class FakeEvaluatingObjectValueNode : DebugObjectValueNode
	{
		bool isEvaluating;
		bool hasChildren;
		public FakeEvaluatingObjectValueNode (string parentPath) : base (parentPath, "evaluating")
		{
			this.isEvaluating = true;
			DoTest ();
		}

		public override bool HasChildren => hasChildren;
		public override bool IsEvaluating => isEvaluating;

		public override string Value => "none";
		public override string TypeName => "No Type";
		public override string DisplayValue => "dummy";


		public override async Task<IEnumerable<IObjectValueNode>> LoadChildrenAsync (CancellationToken cancellationToken)
		{
			// TODO: do some sleeping...
			await Task.Delay (1000);

			this.ClearChildren ();
			this.AddValues (new [] { new FakeObjectValueNode (this.Path) });
			return this.Children;
		}

		async void DoTest()
		{
			await Task.Delay (3000);
			this.isEvaluating = false;
			this.hasChildren = true;
			this.OnValueChanged (EventArgs.Empty);
		}
	}
}
