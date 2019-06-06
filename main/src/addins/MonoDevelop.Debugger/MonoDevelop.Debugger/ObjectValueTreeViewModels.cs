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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Mono.Debugging.Client;
using MonoDevelop.Components;
using MonoDevelop.Core;

namespace MonoDevelop.Debugger
{
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

		Task<IEnumerable<IObjectValueNode>> LoadChildrenAsync (CancellationToken cancellationToken);
	}

	/// <summary>
	/// Base class for IObjectValueNode implementations
	/// </summary>
	public abstract class AbstractObjectValueNode : IObjectValueNode
	{
		readonly List<IObjectValueNode> children = new List<IObjectValueNode> ();

		protected AbstractObjectValueNode (string parentPath, string name)
		{
			this.Name = name;
			if (parentPath.EndsWith ("/", StringComparison.OrdinalIgnoreCase)) {
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

		protected void AddValues (IEnumerable<IObjectValueNode> values)
		{
			this.children.AddRange (values);
		}

		public virtual Task<IEnumerable<IObjectValueNode>> LoadChildrenAsync (CancellationToken cancellationToken)
		{
			return Task.FromResult (Enumerable.Empty<IObjectValueNode> ());
		}

		protected void ClearChildren ()
		{
			this.children.Clear ();
		}

		protected void OnValueChanged (EventArgs e)
		{
			this.ValueChanged?.Invoke (this, e);
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
		public override bool HasFlag (ObjectValueFlags flag) => this.DebuggerObject.HasFlag (flag);

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

	/// <summary>
	/// Special node used as the root of the treeview. 
	/// </summary>
	sealed class RootObjectValueNode : AbstractObjectValueNode
	{
		public RootObjectValueNode () : base (string.Empty, string.Empty)
		{
		}

		public override bool HasChildren => true;

		public new void AddValues (IEnumerable<IObjectValueNode> values)
		{
			base.AddValues (values);
		}
	}

	#region Mocking support abstractions
	public interface IDebuggerService
	{
		bool IsConnected { get; }
		bool IsPaused { get; }
	}


	public interface IStackFrame
	{

	}

	sealed class ProxyDebuggerService : IDebuggerService
	{
		public bool IsConnected => DebuggingService.IsConnected;

		public bool IsPaused => DebuggingService.IsPaused;
	}

	sealed class ProxyStackFrame : IStackFrame
	{
		readonly StackFrame frame;

		public ProxyStackFrame (StackFrame frame)
		{
			this.frame = frame;
		}

		public StackFrame StackFrame => this.frame;
	}
	#endregion

	#region Event classes
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
		public NodeEvaluationCompletedEventArgs (IObjectValueNode node) : base (node)
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
	#endregion
}
