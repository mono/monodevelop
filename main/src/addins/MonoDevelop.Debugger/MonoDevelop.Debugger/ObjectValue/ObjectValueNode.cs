//
// ObjectValueNode.cs
//
// Author:
//       Jeffrey Stedfast <jestedfa@microsoft.com>
//
// Copyright (c) 2019 Microsoft Corp.
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

namespace MonoDevelop.Debugger
{
	/// <summary>
	/// Base class for ObjectValue nodes.
	/// </summary>
	public abstract class ObjectValueNode
	{
		readonly List<ObjectValueNode> children = new List<ObjectValueNode> ();

		protected ObjectValueNode (string name)
		{
			Name = name;
		}

		/// <summary>
		/// Gets the parent node.
		/// </summary>
		public ObjectValueNode Parent { get; set; }

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
		public IReadOnlyList<ObjectValueNode> Children => children;

		/// <summary>
		/// Gets a value indicating whether the node has children or not.
		/// The children may not yet be loaded and Children may return 0 items if not loaded
		/// </summary>
		public virtual bool HasChildren => false;

		/// <summary>
		/// Gets a value indicating whether all children for this node have been loaded from the debugger
		/// </summary>
		public bool ChildrenLoaded { get; private set; }

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
			if (!ChildrenLoaded) {
				var loadedChildren = await OnLoadChildrenAsync (cancellationToken);
				AddChildren (loadedChildren);
				ChildrenLoaded = true;

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
			if (!ChildrenLoaded) {
				var loadedChildren = await OnLoadChildrenAsync (children.Count, count, cancellationToken);
				AddChildren (loadedChildren.Item1);
				ChildrenLoaded = loadedChildren.Item2;

				return loadedChildren.Item1.Count ();
			}

			return 0;
		}

		protected void AddChild (ObjectValueNode value)
		{
			value.Parent = this;
			children.Add (value);
		}

		protected void AddChildren (IEnumerable<ObjectValueNode> values)
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

		protected void ReplaceChildAt (int index, ObjectValueNode value)
		{
			var child = children[index];
			children[index] = value;
			value.Parent = this;
			child.Parent = null;
		}

		protected void InsertChildAt (int index, ObjectValueNode value)
		{
			children.Insert (index, value);
			value.Parent = this;
		}

		protected void ClearChildren ()
		{
			foreach (var child in children)
				child.Parent = null;

			children.Clear ();

			ChildrenLoaded = false;
		}

		protected virtual Task<IEnumerable<ObjectValueNode>> OnLoadChildrenAsync (CancellationToken cancellationToken)
		{
			return Task.FromResult (Enumerable.Empty<ObjectValueNode> ());
		}

		/// <summary>
		/// Returns the children that were loaded and a bool indicating whether all children have now been loaded
		/// </summary>
		protected virtual Task<Tuple<IEnumerable<ObjectValueNode>, bool>> OnLoadChildrenAsync (int index, int count, CancellationToken cancellationToken)
		{
			return Task.FromResult (Tuple.Create (Enumerable.Empty<ObjectValueNode> (), true));
		}

		protected void OnValueChanged (EventArgs e)
		{
			ValueChanged?.Invoke (this, e);
		}
	}
}
