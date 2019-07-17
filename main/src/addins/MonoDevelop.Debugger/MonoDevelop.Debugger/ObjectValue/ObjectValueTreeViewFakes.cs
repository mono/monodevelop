//
// ObjectValueTreeViewFakes.cs
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
using System.Threading;
using System.Threading.Tasks;
using Mono.Debugging.Client;

namespace MonoDevelop.Debugger
{
	/// <summary>
	/// An AbstractObjectValueNode used for debugging
	/// </summary>
	abstract class DebugObjectValueNode : ObjectValueNode
	{
		protected DebugObjectValueNode (string name) : base (name)
		{
		}

		public override string Value => "none";
		public override string DisplayValue => "dummy";
		public override string TypeName => GetType ().ToString ();

		public override bool HasChildren => true;
	}

	/// <summary>
	/// An AbstractObjectValueNode used for debugging
	/// </summary>
	sealed class FakeIndexedObjectValueNode : DebugObjectValueNode
	{
		public FakeIndexedObjectValueNode (int index) : base ($"indexed[{index}]")
		{
			Value = $"indexed[{index}]";
			DisplayValue = $"indexed[{index}]";
		}

		public override string Value { get; }
		public override string DisplayValue { get; }

		public override bool HasChildren => false;
	}

	/// <summary>
	/// An AbstractObjectValueNode used for debugging
	/// </summary>
	sealed class FakeIsImplicitNotSupportedObjectValueNode : DebugObjectValueNode
	{
		string value;
		bool isImplicitNotSupported;
		public FakeIsImplicitNotSupportedObjectValueNode () : base ($"implicit")
		{
			value = $"implicit";
			isImplicitNotSupported = true;
		}

		public override string Value => value;
		public override string DisplayValue => value;

		public override bool HasChildren => false;
		public override bool IsImplicitNotSupported => isImplicitNotSupported;
		public override bool CanRefresh => true;

		public override void Refresh (EvaluationOptions options)
		{
			value = "refreshed";
			//IsImplicitNotSupported should be false once we forcibly refresh the node
			isImplicitNotSupported = false;
		}
	}

	/// <summary>
	/// An AbstractObjectValueNode used for debugging
	/// </summary>
	sealed class FakeObjectValueNode : DebugObjectValueNode
	{
		bool hasChildren;

		public FakeObjectValueNode (string name, bool children = true) : base (name)
		{
			hasChildren = children;
		}

		public override string Value => "none";
		public override string DisplayValue => "dummy";

		public override bool HasChildren => true;

		protected override async Task<IEnumerable<ObjectValueNode>> OnLoadChildrenAsync (CancellationToken cancellationToken)
		{
			// TODO: do some sleeping...
			await Task.Delay (1000);
			return new [] { new FakeObjectValueNode ($"child of {Name}") };
		}
	}

	/// <summary>
	/// An AbstractObjectValueNode used for debugging
	/// </summary>
	sealed class FakeEnumerableObjectValueNode : DebugObjectValueNode
	{
		readonly int maxItems;

		public FakeEnumerableObjectValueNode (int count) : base ($"enumerable {count}")
		{
			maxItems = count;
		}

		public override string Value => $"Enumerable{maxItems}";
		public override string DisplayValue => $"Enumerable{maxItems}";

		public override bool HasChildren => true;
		public override bool IsEnumerable => true;

		protected override async Task<IEnumerable<ObjectValueNode>> OnLoadChildrenAsync (CancellationToken cancellationToken)
		{
			await Task.Delay (1000);
			var result = new List<ObjectValueNode> ();
			for (int i = 0; i < maxItems; i++) {
				result.Add (new FakeIndexedObjectValueNode (i));
			}

			return result;
		}

		protected override async Task<Tuple<IEnumerable<ObjectValueNode>, bool>> OnLoadChildrenAsync (int index, int count, CancellationToken cancellationToken)
		{
			await Task.Delay (1000);
			var max = Math.Min (maxItems, index+count);
			var result = new List<ObjectValueNode> ();
			for (int i = index; i < max; i++) {
				result.Add (new FakeIndexedObjectValueNode (i));
			}

			return Tuple.Create<IEnumerable<ObjectValueNode>, bool> (result, result.Count < count);
		}
	}

	/// <summary>
	/// An AbstractObjectValueNode used for debugging
	/// </summary>
	sealed class FakeEvaluatingObjectValueNode : DebugObjectValueNode
	{
		bool isEvaluating;
		bool hasChildren;

		public FakeEvaluatingObjectValueNode () : base ("evaluating")
		{
			isEvaluating = true;
			DoTest ();
		}

		public override string Value => "none";
		public override string DisplayValue => "dummy";

		public override bool HasChildren => hasChildren;
		public override bool IsEvaluating => isEvaluating;

		protected override async Task<IEnumerable<ObjectValueNode>> OnLoadChildrenAsync (CancellationToken cancellationToken)
		{
			// TODO: do some sleeping...
			await Task.Delay (1000);

			return new [] { new FakeObjectValueNode ($"child of {Name}", true) };
		}

		async void DoTest ()
		{
			await Task.Delay (3000);
			isEvaluating = false;
			hasChildren = true;
			OnValueChanged (EventArgs.Empty);
		}
	}

	/// <summary>
	/// An AbstractObjectValueNode used for debugging
	/// </summary>
	sealed class FakeEvaluatingGroupObjectValueNode : DebugObjectValueNode, IEvaluatingGroupObjectValueNode
	{
		bool isEvaluating;
		int evalNodes;

		public FakeEvaluatingGroupObjectValueNode (int nodes) : base ($"eval group {nodes}")
		{
			isEvaluating = true;
			evalNodes = nodes;
			DoTest ();
		}

		public override string Value => "none";
		public override string DisplayValue => $"evg {evalNodes}";

		public override bool IsEvaluating => isEvaluating;

		#region IEvaluatingGroupObjectValueNode
		bool IEvaluatingGroupObjectValueNode.IsEvaluatingGroup => true;

		ObjectValueNode [] IEvaluatingGroupObjectValueNode.GetEvaluationGroupReplacementNodes ()
		{
			var replacementNodes = new ObjectValueNode [evalNodes];

			for (int i = 0; i < evalNodes; i++) {
				replacementNodes [i] = new FakeObjectValueNode ($"child of {Name}", false) {
					Parent = Parent
				};
			}

			return replacementNodes;
		}
		#endregion

		async void DoTest ()
		{
			await Task.Delay (5000);
			isEvaluating = false;
			OnValueChanged (EventArgs.Empty);
		}
	}
}
