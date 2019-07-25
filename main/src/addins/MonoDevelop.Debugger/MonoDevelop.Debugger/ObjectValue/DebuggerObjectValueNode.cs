//
// DebuggerObjectValueNode.cs
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

using MonoDevelop.Core;

namespace MonoDevelop.Debugger
{
	/// <summary>
	/// Represents a node in a tree structure that holds an ObjectValue from the debugger.
	/// </summary>
	class DebuggerObjectValueNode : ObjectValueNode, IEvaluatingGroupObjectValueNode
	{
		public DebuggerObjectValueNode (ObjectValue value) : base (value.Name)
		{
			DebuggerObject = value;

			value.ValueChanged += OnDebuggerValueChanged;
		}

		// TODO: try and make this private
		public ObjectValue DebuggerObject { get; }

		/// <summary>
		/// Gets the expression for the node that can be used when pinning node
		/// </summary>
		public override string Expression {
			get {
				string expression = "";

				var node = this;
				var name = node.Name;
				while (node != null && node.Parent is DebuggerObjectValueNode) {
					expression = node.DebuggerObject.ChildSelector + expression;
					node = (DebuggerObjectValueNode)node.Parent;
					name = node.Name;
				}

				return name + expression;
			}
		}

		public override bool HasChildren => DebuggerObject.HasChildren;
		public override bool IsEnumerable => DebuggerObject.Flags.HasFlag (ObjectValueFlags.IEnumerable);
		public override bool IsEvaluating => DebuggerObject.IsEvaluating;
		public override bool CanEdit => GetCanEdit ();

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

		ObjectValueNode [] IEvaluatingGroupObjectValueNode.GetEvaluationGroupReplacementNodes ()
		{
			var replacementNodes = new ObjectValueNode[DebuggerObject.ArrayCount];

			for (int i = 0; i < replacementNodes.Length; i++) {
				replacementNodes[i] = new DebuggerObjectValueNode (DebuggerObject.GetArrayItem (i)) {
					Parent = Parent
				};
			}

			return replacementNodes;
		}
		#endregion

		protected override async Task<IEnumerable<ObjectValueNode>> OnLoadChildrenAsync (CancellationToken cancellationToken)
		{
			var childValues = await GetChildrenAsync (DebuggerObject, cancellationToken);

			return childValues.Select (x => new DebuggerObjectValueNode (x));
		}

		protected override async Task<Tuple<IEnumerable<ObjectValueNode>, bool>> OnLoadChildrenAsync (int index, int count, CancellationToken cancellationToken)
		{
			var values = await GetChildrenAsync (DebuggerObject, index, count, cancellationToken);
			var nodes = values.Select (value => new DebuggerObjectValueNode (value));

			// if we returned less that we asked for, we assume we've now loaded all children
			return Tuple.Create<IEnumerable<ObjectValueNode>, bool> (nodes, values.Length < count);
		}

		static Task<ObjectValue[]> GetChildrenAsync (ObjectValue value, CancellationToken cancellationToken)
		{
			return Task.Run (() => {
				try {
					return value.GetAllChildren ();
				} catch (Exception ex) {
					// Note: this should only happen if someone breaks ObjectValue.GetAllChildren()
					LoggingService.LogError ("Failed to get ObjectValue children.", ex);
					return new ObjectValue[0];
				}
			}, cancellationToken);
		}

		static Task<ObjectValue []> GetChildrenAsync (ObjectValue value, int index, int count, CancellationToken cancellationToken)
		{
			return Task.Run(() => {
				try {
					return value.GetRangeOfChildren (index, count);
				} catch (Exception ex) {
					// Note: this should only happen if someone breaks ObjectValue.GetAllChildren()
					LoggingService.LogError ("Failed to get ObjectValue range of children.", ex);
					return new ObjectValue[0];
				}
			}, cancellationToken);
		}

		void OnDebuggerValueChanged (object sender, EventArgs e)
		{
			OnValueChanged (e);
		}

		bool GetCanEdit ()
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
}
