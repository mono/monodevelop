//
// ObjectValueTreeViewModels.cs
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

namespace MonoDevelop.Debugger
{
	/// <summary>
	/// Internal interface to support the notion that the debugging service might return a placeholder
	/// object for all locals for instance. 
	/// </summary>
	interface IEvaluatingGroupObjectValueNode
	{
		/// <summary>
		/// Gets a value indicating whether this object that was evaulating was evaluating a group
		/// of objects, such as locals, and whether we should replace the node with a set of new
		/// nodes once evaulation has completed
		/// </summary>
		bool IsEvaluatingGroup { get; }

		/// <summary>
		/// Get an array of new objectvalue nodes that should replace the current node in the tree
		/// </summary>
		AbstractObjectValueNode[] GetEvaluationGroupReplacementNodes ();
	}

	/// <summary>
	/// Internal interface to support the notion that the debugging service might return a placeholder
	/// object for all locals for instance. 
	/// </summary>
	interface ISupportChildObjectValueNodeReplacement
	{
		/// <summary>
		/// Replaces the given child node with a new set of nodes
		/// </summary>
		void ReplaceChildNode (AbstractObjectValueNode node, AbstractObjectValueNode[] newNodes);
	}

	/// <summary>
	/// Base class for AbstractObjectValueNode implementations
	/// </summary>
	public abstract class AbstractObjectValueNode
	{
		readonly List<AbstractObjectValueNode> children = new List<AbstractObjectValueNode> ();
		bool allChildrenLoaded;

		protected AbstractObjectValueNode (string name)
		{
			Name = name;
		}

		/// <summary>
		/// Gets the parent node.
		/// </summary>
		public AbstractObjectValueNode Parent { get; set; }

		/// <summary>
		/// Gets the name of the object
		/// </summary>
		public string Name { get; }

		/// <summary>
		/// Gets the "path" of the object ("root object/parent object/variable name").
		/// </summary>
		public string Path {
			get {
				if (Parent != null)
					return Parent.Path + "/" + Name;

				return "/" + Name;
			}
		}

		/// <summary>
		/// Gets the collection of children that have been loaded from the debugger
		/// </summary>
		public IReadOnlyList<AbstractObjectValueNode> Children => children;

		/// <summary>
		/// Gets a value indicating whether the node has children or not.
		/// The children may not yet be loaded and Children may return 0 items if not loaded
		/// </summary>
		public virtual bool HasChildren => false;

		/// <summary>
		/// Gets a value indicating whether all children for this node have been loaded from the debugger
		/// </summary>
		public bool ChildrenLoaded => allChildrenLoaded;

		// TODO: make the setter private and get the node to do the expansion
		/// <summary>
		/// Gets or sets a value indicating whether the node is expanded
		/// </summary>
		public virtual bool IsExpanded { get; set; }

		/// <summary>
		/// Gets a value indicating whether the object is an enumerable
		/// </summary>
		public virtual bool IsEnumerable => false;

		/// <summary>
		/// Gets a value indicating whether the debugger is still evaluating the object
		/// </summary>
		public virtual bool IsEvaluating => false;

		/// <summary>
		/// Gets a value indicating whether the value can be edited by the user or not
		/// </summary>
		public virtual bool CanEdit => false;


		public virtual string DisplayValue => string.Empty;


		public virtual bool IsUnknown => false;
		public virtual bool IsReadOnly => false;
		public virtual bool IsError => false;
		public virtual bool IsNotSupported => false;
		public virtual string Value => string.Empty;
		public virtual bool IsImplicitNotSupported => false;
		public virtual ObjectValueFlags Flags => ObjectValueFlags.None;
		public virtual bool IsNull => false;
		public virtual bool IsPrimitive => false;
		public virtual string TypeName => string.Empty;
		public virtual bool CanRefresh => false;
		public virtual bool HasFlag (ObjectValueFlags flag) => false;

		/// <summary>
		/// Fired when the value of the object has changed
		/// </summary>
		public event EventHandler ValueChanged;

		/// <summary>
		/// Attempts to set the value of the node to newValue
		/// </summary>
		public virtual void SetValue (string newValue)
		{
		}

		/// <summary>
		/// Tells the object to refresh its values from the debugger
		/// </summary>
		public virtual void Refresh ()
		{
		}

		/// <summary>
		/// Tells the object to refresh its values from the debugger
		/// </summary>
		public virtual void Refresh (EvaluationOptions options)
		{
		}

		/// <summary>
		/// Asynchronously loads all children for the node into Children.
		/// The task will complete immediately if all the children have previously been loaded and
		/// the debugger will not be re-queried
		/// </summary>
		public async Task<int> LoadChildrenAsync (CancellationToken cancellationToken)
		{
			if (!allChildrenLoaded) {
				var loadedChildren = await OnLoadChildrenAsync (cancellationToken);
				AddChildren (loadedChildren);
				allChildrenLoaded = true;

				return loadedChildren.Count ();
			}

			return 0;
		}

		/// <summary>
		/// Asynchronously loads a range of at most count children for the node into Children.
		/// Subsequent calls will load an additional count children into Children until all children
		/// have been loaded.
		/// The task will complete immediately if all the children have previously been loaded and
		/// the debugger will not be re-queried. 
		/// </summary>
		public async Task<int> LoadChildrenAsync (int count, CancellationToken cancellationToken)
		{
			if (!allChildrenLoaded) {
				var loadedChildren = await OnLoadChildrenAsync (children.Count, count, cancellationToken);
				AddChildren (loadedChildren.Item1);
				allChildrenLoaded = loadedChildren.Item2;

				return loadedChildren.Item1.Count ();
			}

			return 0;
		}

		protected void AddChild (AbstractObjectValueNode value)
		{
			value.Parent = this;
			children.Add (value);
		}

		protected void AddChildren (IEnumerable<AbstractObjectValueNode> values)
		{
			foreach (var value in values)
				AddChild (value);
		}

		protected void RemoveChildAt (int index)
		{
			var child = children[index];
			children.RemoveAt (index);
			child.Parent = null;
		}

		protected void ReplaceChildAt (int index, AbstractObjectValueNode value)
		{
			var child = children[index];
			children[index] = value;
			value.Parent = this;
			child.Parent = null;
		}

		protected void InsertChildAt (int index, AbstractObjectValueNode value)
		{
			children.Insert (index, value);
			value.Parent = this;
		}

		protected void ClearChildren ()
		{
			foreach (var child in children)
				child.Parent = null;

			children.Clear ();

			allChildrenLoaded = false;
		}

		protected virtual Task<IEnumerable<AbstractObjectValueNode>> OnLoadChildrenAsync (CancellationToken cancellationToken)
		{
			return Task.FromResult (Enumerable.Empty<AbstractObjectValueNode> ());
		}

		/// <summary>
		/// Returns the children that were loaded and a bool indicating whether all children have now been loaded
		/// </summary>
		protected virtual Task<Tuple<IEnumerable<AbstractObjectValueNode>, bool>> OnLoadChildrenAsync (int index, int count, CancellationToken cancellationToken)
		{
			return Task.FromResult (Tuple.Create (Enumerable.Empty<AbstractObjectValueNode> (), true));
		}

		protected void OnValueChanged (EventArgs e)
		{
			ValueChanged?.Invoke (this, e);
		}
	}

	/// <summary>
	/// Represents a node in a tree structure that holds ObjectValue from the debugger.
	/// </summary>
	public class ObjectValueNode : AbstractObjectValueNode, IEvaluatingGroupObjectValueNode
	{
		public ObjectValueNode (ObjectValue value) : base (value.Name)
		{
			DebuggerObject = value;

			value.ValueChanged += OnDebuggerValueChanged;
		}

		// TODO: try and make this private
		public ObjectValue DebuggerObject { get; }


		public override bool HasChildren => DebuggerObject.HasChildren;
		public override bool IsEnumerable => DebuggerObject.Flags.HasFlag (ObjectValueFlags.IEnumerable);
		public override bool IsEvaluating => DebuggerObject.IsEvaluating;
		public override bool CanEdit => GetCanEdit();


		public override bool IsUnknown => DebuggerObject.IsUnknown;
		public override bool IsReadOnly => DebuggerObject.IsReadOnly;
		public override bool IsError => DebuggerObject.IsError;
		public override bool IsNotSupported => DebuggerObject.IsNotSupported;
		public override string Value => DebuggerObject.Value;
		public override bool IsImplicitNotSupported => DebuggerObject.IsImplicitNotSupported;
		public override ObjectValueFlags Flags => DebuggerObject.Flags;
		public override bool IsNull => DebuggerObject.IsNull;
		public override bool IsPrimitive => DebuggerObject.IsPrimitive;
		public override string TypeName => DebuggerObject.TypeName;
		public override string DisplayValue => DebuggerObject.DisplayValue;
		public override bool CanRefresh => DebuggerObject.CanRefresh;
		public override bool HasFlag (ObjectValueFlags flag) => DebuggerObject.HasFlag (flag);

		public override void SetValue (string newValue)
		{
			DebuggerObject.Value = newValue;
		}

		public override void Refresh ()
		{
			DebuggerObject.Refresh ();
		}

		public override void Refresh (EvaluationOptions options)
		{
			DebuggerObject.Refresh (options);
		}

		#region IEvaluatingGroupObjectValueNode
		bool IEvaluatingGroupObjectValueNode.IsEvaluatingGroup => DebuggerObject.IsEvaluatingGroup;

		AbstractObjectValueNode [] IEvaluatingGroupObjectValueNode.GetEvaluationGroupReplacementNodes ()
		{
			var replacementNodes = new AbstractObjectValueNode [DebuggerObject.ArrayCount];

			for (int i = 0; i < replacementNodes.Length; i++) {
				replacementNodes [i] = new ObjectValueNode (DebuggerObject.GetArrayItem (i)) {
					Parent = Parent
				};
			}

			return replacementNodes;
		}
		#endregion

		protected override async Task<IEnumerable<AbstractObjectValueNode>> OnLoadChildrenAsync (CancellationToken cancellationToken)
		{
			var childValues = await GetChildrenAsync (DebuggerObject, cancellationToken);

			return childValues.Select (x => new ObjectValueNode (x));
		}

		protected override async Task<Tuple<IEnumerable<AbstractObjectValueNode>, bool>> OnLoadChildrenAsync (int index, int count, CancellationToken cancellationToken)
		{
			var values = await GetChildrenAsync (DebuggerObject, index, count, cancellationToken);
			var nodes = values.Select (value => new ObjectValueNode (value));

			// if we returned less that we asked for, we assume we've now loaded all children
			return Tuple.Create<IEnumerable<AbstractObjectValueNode>, bool> (nodes, values.Length < count);
		}


		static Task<ObjectValue[]> GetChildrenAsync (ObjectValue value, CancellationToken cancellationToken)
		{
			return Task.Factory.StartNew (delegate (object arg) {
				try {
					return ((ObjectValue) arg).GetAllChildren ();
				} catch (Exception ex) {
					// Note: this should only happen if someone breaks ObjectValue.GetAllChildren()
					LoggingService.LogError ("Failed to get ObjectValue children.", ex);
					return new ObjectValue [0];
				}
			}, value, cancellationToken, TaskCreationOptions.None, TaskScheduler.Default);
		}

		static Task<ObjectValue[]> GetChildrenAsync (ObjectValue value, int index, int count, CancellationToken cancellationToken)
		{
			return Task.Factory.StartNew (delegate (object arg) {
				try {
					return ((ObjectValue)arg).GetRangeOfChildren (index, count);
				} catch (Exception ex) {
					// Note: this should only happen if someone breaks ObjectValue.GetAllChildren()
					LoggingService.LogError ("Failed to get ObjectValue range of children.", ex);
					return new ObjectValue [0];
				}
			}, value, cancellationToken, TaskCreationOptions.None, TaskScheduler.Default);
		}

		void OnDebuggerValueChanged (object sender, EventArgs e)
		{
			OnValueChanged (e);
		}

		bool GetCanEdit()
		{
			var val = DebuggerObject;
			bool canEdit;

			if (val.IsUnknown) {
				//if (frame != null) {
				//	canEdit = false;
				//} else {
					canEdit = !val.IsReadOnly;
				//}
			} else if (val.IsError || val.IsNotSupported) {
				canEdit = false;
			} else if (val.IsImplicitNotSupported) {
				canEdit = false;
			} else if (val.IsEvaluating) {
				canEdit = false;
			} else if (val.Flags.HasFlag (ObjectValueFlags.IEnumerable)) {
				canEdit = false;
			} else {
				canEdit = val.IsPrimitive && !val.IsReadOnly;
			}

			return canEdit;
		}
	}

	/// <summary>
	/// Special node used as the root of the treeview. 
	/// </summary>
	sealed class RootObjectValueNode : AbstractObjectValueNode, ISupportChildObjectValueNodeReplacement
	{
		public RootObjectValueNode () : base (string.Empty)
		{
			IsExpanded = true;
		}

		public override bool HasChildren => true;

		public void AddValue (AbstractObjectValueNode value)
		{
			AddChild (value);
		}

		public void AddValues (IEnumerable<AbstractObjectValueNode> values)
		{
			AddChildren (values);
		}

		public void RemoveValueAt (int index)
		{
			RemoveChildAt (index);
		}

		public void ReplaceValueAt (int index, AbstractObjectValueNode value)
		{
			ReplaceChildAt (index, value);
		}

		void ISupportChildObjectValueNodeReplacement.ReplaceChildNode (AbstractObjectValueNode node, AbstractObjectValueNode [] newNodes)
		{
			var ix = Children.IndexOf (node);
			System.Diagnostics.Debug.Assert (ix >= 0, "The node being replaced should be a child of this node");
			if (newNodes.Length == 0) {
				RemoveChildAt (ix);
				return;
			}

			ReplaceChildAt (ix, newNodes [0]);

			for (int i = 1; i < newNodes.Length; i++) {
				ix++;
				InsertChildAt (ix, newNodes [i]);
			}
		}
	}

	/// <summary>
	/// Special node used to indicate that more values are available. 
	/// </summary>
	sealed class ShowMoreValuesObjectValueNode : AbstractObjectValueNode
	{
		public ShowMoreValuesObjectValueNode (AbstractObjectValueNode enumerableNode) : base (string.Empty)
		{
			EnumerableNode = enumerableNode;
		}

		public override bool IsEnumerable => true;

		public AbstractObjectValueNode EnumerableNode { get; }
	}

	#region Mocking support abstractions
	public interface IDebuggerService
	{
		bool IsConnected { get; }
		bool IsPaused { get; }
		void NotifyVariableChanged ();
		bool HasValueVisualizers (AbstractObjectValueNode node);
		bool HasInlineVisualizer (AbstractObjectValueNode node);
		bool ShowValueVisualizer (AbstractObjectValueNode node);
	}


	public interface IStackFrame
	{
		EvaluationOptions CloneSessionEvaluationOpions ();
		AbstractObjectValueNode EvaluateExpression (string expression);
		AbstractObjectValueNode [] EvaluateExpressions (IList<string> expressions);
	}

	sealed class ProxyDebuggerService : IDebuggerService
	{
		public bool IsConnected => DebuggingService.IsConnected;

		public bool IsPaused => DebuggingService.IsPaused;

		public void NotifyVariableChanged()
		{
			DebuggingService.NotifyVariableChanged ();
		}

		public bool HasValueVisualizers (AbstractObjectValueNode node)
		{
			var val = node.GetDebuggerObjectValue ();
			if (val != null) {
				return DebuggingService.HasValueVisualizers (val);
			}

			return false;
		}

		public bool HasInlineVisualizer (AbstractObjectValueNode node)
		{
			var val = node.GetDebuggerObjectValue ();
			if (val != null) {
				return DebuggingService.HasInlineVisualizer (val);
			}

			return false;
		}

		public bool ShowValueVisualizer (AbstractObjectValueNode node)
		{
			var val = node.GetDebuggerObjectValue ();
			if (val != null) {
				return DebuggingService.ShowValueVisualizer (val);
			}

			return false;
		}
	}

	sealed class ProxyStackFrame : IStackFrame
	{
		readonly Dictionary<string, ObjectValue> cachedValues = new Dictionary<string, ObjectValue> ();

		public ProxyStackFrame (StackFrame frame)
		{
			StackFrame = frame;
		}

		public StackFrame StackFrame {
			get; private set;
		}

		public EvaluationOptions CloneSessionEvaluationOpions ()
		{
			return StackFrame.DebuggerSession.Options.EvaluationOptions.Clone ();
		}

		public AbstractObjectValueNode EvaluateExpression (string expression)
		{
			ObjectValue value;
			if (cachedValues.TryGetValue (expression, out value))
				return new ObjectValueNode(value);

			if (StackFrame != null)
				value = StackFrame.GetExpressionValue (expression, true);
			else
				value = ObjectValue.CreateUnknown (expression);

			cachedValues [expression] = value;

			return new ObjectValueNode (value);
		}

		public AbstractObjectValueNode [] EvaluateExpressions (IList<string> expressions)
		{
			var values = new ObjectValue [expressions.Count];
			var unknown = new List<string> ();

			for (int i = 0; i < expressions.Count; i++) {
				if (!cachedValues.TryGetValue (expressions [i], out ObjectValue value))
					unknown.Add (expressions [i]);
				else
					values [i] = value;
			}

			ObjectValue [] qvalues;

			if (StackFrame != null) {
				qvalues = StackFrame.GetExpressionValues (unknown.ToArray (), true);
			} else {
				qvalues = new ObjectValue [unknown.Count];
				for (int i = 0; i < qvalues.Length; i++)
					qvalues [i] = ObjectValue.CreateUnknown (unknown [i]);
			}

			for (int i = 0, v = 0; i < values.Length; i++) {
				if (values [i] == null) {
					var value = qvalues [v++];

					cachedValues [expressions [i]] = value;
					values [i] = value;
				}
			}

			return values.Select(v => new ObjectValueNode(v)).ToArray();
		}

	}
	#endregion

	#region Event classes
	public abstract class NodeEventArgs : EventArgs
	{
		protected NodeEventArgs (AbstractObjectValueNode node)
		{
			Node = node;
		}

		public AbstractObjectValueNode Node {
			get; private set;
		}
	}

	public sealed class NodeEvaluationCompletedEventArgs : NodeEventArgs
	{
		public NodeEvaluationCompletedEventArgs (AbstractObjectValueNode node, AbstractObjectValueNode[] replacementNodes) : base (node)
		{
			ReplacementNodes = replacementNodes;
		}

		/// <summary>
		/// Gets an array of nodes that should be used to replace the node that finished evaluating.
		/// Some sets of values, like local variables, frame locals and the like are fetched asynchronously
		/// and may take some time to fetch. In this case, a single object is returned that is a place holder
		/// for 0 or more values that should be expanded in the place of the evaluating node.
		/// </summary>
		public AbstractObjectValueNode [] ReplacementNodes { get; }
	}

	/// <summary>
	/// Event args for when a node has been expanded
	/// </summary>
	public sealed class NodeExpandedEventArgs : NodeEventArgs
	{
		public NodeExpandedEventArgs (AbstractObjectValueNode node) : base (node)
		{
		}
	}

	public sealed class ChildrenChangedEventArgs : NodeEventArgs
	{
		public ChildrenChangedEventArgs (AbstractObjectValueNode node, int index, int count) : base (node)
		{
			Index = index;
			Count = count;
		}

		/// <summary>
		/// Gets the count of child nodes that were loaded
		/// </summary>
		public int Count { get; }

		/// <summary>
		/// Gets the index of the first child that was loaded
		/// </summary>
		public int Index { get; }
	}

	public sealed class NodeChangedEventArgs : NodeEventArgs
	{
		public NodeChangedEventArgs (AbstractObjectValueNode node) : base (node)
		{
		}
	}
	#endregion
}
