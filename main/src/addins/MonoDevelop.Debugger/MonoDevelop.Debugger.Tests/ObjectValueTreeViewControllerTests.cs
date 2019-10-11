//
// ObjectValueTreeViewControllerTests.cs
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
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using NUnit.Framework;

using Mono.Debugging.Client;

namespace MonoDevelop.Debugger.Tests
{
	class DummyDebuggerService : IDebuggerService
	{
		public bool IsConnected => true;

		public bool IsPaused => true;

		public bool HasInlineVisualizer (ObjectValueNode node)
		{
			return false;
		}

		public bool HasValueVisualizers (ObjectValueNode node)
		{
			return false;
		}

		public void NotifyVariableChanged ()
		{
		}

		public bool ShowValueVisualizer (ObjectValueNode node)
		{
			return false;
		}
	}

	class DummyStackFrame : IStackFrame
	{
		public EvaluationOptions CloneSessionEvaluationOpions ()
		{
			return new EvaluationOptions ();
		}

		public ObjectValueNode EvaluateExpression (string expression)
		{
			return new FakeObjectValueNode (expression);
		}

		public ObjectValueNode [] EvaluateExpressions (IList<string> expressions)
		{
			var values = new ObjectValueNode [expressions.Count];

			for (int i = 0; i < expressions.Count; i++)
				values [i] = new FakeObjectValueNode (expressions [i]);

			return values;
		}
	}

	class DummyObjectValueTreeViewController : ObjectValueTreeViewController
	{
		protected override IDebuggerService OnGetDebuggerService ()
		{
			return new DummyDebuggerService ();
		}

		public void SetViewControl (IObjectValueTreeView control)
		{
			ConfigureView (control);
		}
	}

	class ObjectValueNodeReplacedEventArgs : EventArgs
	{
		public ObjectValueNodeReplacedEventArgs (ObjectValueNode node, ObjectValueNode[] replacementNodes)
		{
			Node = node;
			ReplacementNodes = replacementNodes;
		}

		public ObjectValueNode Node {
			get; private set;
		}

		public ObjectValueNode[] ReplacementNodes {
			get; private set;
		}
	}

	class DummyObjectValueTreeView : IObjectValueTreeView
	{
		public bool AllowEditing { get; set; }
		public bool AllowExpanding { get; set; }
		public PinnedWatch PinnedWatch { get; set; }

		public int PinnedWatchOffset { get; set; }

		public event EventHandler<ObjectValueNodeEventArgs> NodeExpand;

		public object EmitNodeExpand (ObjectValueNode node)
		{
			var args = new ObjectValueNodeEventArgs (node);
			NodeExpand (this, args);
			return args.Response;
		}

		public event EventHandler<ObjectValueNodeEventArgs> NodeCollapse;

		public object EmitNodeCollapse (ObjectValueNode node)
		{
			var args = new ObjectValueNodeEventArgs (node);
			NodeCollapse (this, args);
			return args.Response;
		}

		public event EventHandler<ObjectValueNodeEventArgs> NodeLoadMoreChildren;

		public object EmitNodeLoadMoreChildren (ObjectValueNode node)
		{
			var args = new ObjectValueNodeEventArgs (node);
			NodeLoadMoreChildren (this, args);
			return args.Response;
		}

		public event EventHandler<ObjectValueNodeEventArgs> NodeRefresh;

		public object EmitNodeRefresh (ObjectValueNode node)
		{
			var args = new ObjectValueNodeEventArgs (node);
			NodeRefresh (this, args);
			return args.Response;
		}

		public event EventHandler<ObjectValueNodeEventArgs> NodeGetCanEdit;

		public object EmitNodeGetCanEdit (ObjectValueNode node)
		{
			var args = new ObjectValueNodeEventArgs (node);
			NodeGetCanEdit (this, args);
			return args.Response;
		}

		public event EventHandler<ObjectValueEditEventArgs> NodeEditValue;

		public object EmitNodeEditValue (ObjectValueNode node, string newValue)
		{
			var args = new ObjectValueEditEventArgs (node, newValue);
			NodeEditValue (this, args);
			return args.Response;
		}

		public event EventHandler<ObjectValueNodeEventArgs> NodeRemoved;

		public object EmitNodeRemoved (ObjectValueNode node)
		{
			var args = new ObjectValueNodeEventArgs (node);
			NodeRemoved (this, args);
			return args.Response;
		}

		public event EventHandler<ObjectValueNodeEventArgs> NodePinned;

		public object EmitNodePinned (ObjectValueNode node)
		{
			var args = new ObjectValueNodeEventArgs (node);
			NodePinned (this, args);
			return args.Response;
		}

		public event EventHandler<EventArgs> NodeUnpinned;

		public object EmitNodeUnpinned (ObjectValueNode node)
		{
			var args = new ObjectValueNodeEventArgs (node);
			NodeUnpinned (this, args);
			return args.Response;
		}

		public event EventHandler<ObjectValueNodeEventArgs> NodeShowVisualiser;

		public object EmitNodeShowVisualizer (ObjectValueNode node)
		{
			var args = new ObjectValueNodeEventArgs (node);
			NodeShowVisualiser (this, args);
			return args.Response;
		}

		public event EventHandler<ObjectValueExpressionEventArgs> ExpressionAdded;

		public object EmitExpressionAdded (ObjectValueNode node, string expression)
		{
			var args = new ObjectValueExpressionEventArgs (node, expression);
			ExpressionAdded (this, args);
			return args.Response;
		}

		public event EventHandler<ObjectValueExpressionEventArgs> ExpressionEdited;

		public object EmitExpressionEdited (ObjectValueNode node, string expression)
		{
			var args = new ObjectValueExpressionEventArgs (node, expression);
			ExpressionEdited (this, args);
			return args.Response;
		}

		public event EventHandler StartEditing;
		public event EventHandler EndEditing;

		public event EventHandler<ObjectValueNodeEventArgs> ViewAppendedNode;

		public void Appended (ObjectValueNode node)
		{
			ViewAppendedNode.Invoke (this, new ObjectValueNodeEventArgs (node));
		}

		public void Appended (IList<ObjectValueNode> nodes)
		{
			foreach (var node in nodes)
				ViewAppendedNode?.Invoke (this, new ObjectValueNodeEventArgs (node));
		}

		public event EventHandler ViewCleared;

		public void Cleared ()
		{
			ViewCleared?.Invoke (this, EventArgs.Empty);
		}

		public event EventHandler<ObjectValueNodeReplacedEventArgs> ViewReplacedNode;

		public void LoadEvaluatedNode (ObjectValueNode node, ObjectValueNode[] replacementNodes)
		{
			ViewReplacedNode?.Invoke (this, new ObjectValueNodeReplacedEventArgs (node, replacementNodes));
		}

		public event EventHandler<ObjectValueNodeEventArgs> ViewLoadedChildren;

		public void LoadNodeChildren (ObjectValueNode node, int startIndex, int count)
		{
			ViewLoadedChildren?.Invoke (this, new ObjectValueNodeEventArgs (node));
		}

		public event EventHandler<ObjectValueNodeEventArgs> ViewExpandedNode;

		public void OnNodeExpanded (ObjectValueNode node)
		{
			ViewExpandedNode?.Invoke (this, new ObjectValueNodeEventArgs (node));
		}
	}

	[TestFixture]
	public class ObjectValueTreeViewControllerTests
	{
		[Test]
		public async Task TestBasicFunctionalityAsync ()
		{
			var controller = new DummyObjectValueTreeViewController ();
			var view = new DummyObjectValueTreeView ();
			ObjectValueNode[] replacements = null;
			int appended = 0;
			int replaced = 0;
			int expanded = 0;
			int cleared = 0;
			int loaded = 0;

			view.ViewAppendedNode += (o, e) => {
				appended++;
			};

			view.ViewReplacedNode += (o, e) => {
				replaced++;
				replacements = e.ReplacementNodes;
			};

			view.ViewLoadedChildren += (o, e) => {
				loaded++;
			};

			view.ViewExpandedNode += (o, e) => {
				expanded++;
			};

			view.ViewCleared += (o, e) => {
				cleared++;
			};

			controller.SetViewControl (view);
			controller.Frame = new DummyStackFrame ();

			var xx = new List<ObjectValueNode> ();

			xx.Add (new FakeObjectValueNode ("f1"));
			xx.Add (new FakeIsImplicitNotSupportedObjectValueNode ());

			xx.Add (new FakeEvaluatingGroupObjectValueNode (1));
			xx.Add (new FakeEvaluatingGroupObjectValueNode (0));
			xx.Add (new FakeEvaluatingGroupObjectValueNode (5));

			xx.Add (new FakeEvaluatingObjectValueNode ());
			xx.Add (new FakeEnumerableObjectValueNode (10));
			xx.Add (new FakeEnumerableObjectValueNode (20));
			xx.Add (new FakeEnumerableObjectValueNode (23));

			controller.AddValues (xx);

			Assert.AreEqual (xx.Count, appended, "Number of appended object value nodes do not match.");

			// the fake evaluating nodes are using a 5000 timer, so 5100 should be enough...
			await Task.Delay (5100);

			Assert.AreEqual (4, replaced, "Number of replaced nodes does not match.");

			// expand the "f1" node
			view.EmitNodeExpand (xx[0]);

			// expanding a fake node uses a 1000 timer, so 1100 should be enough
			await Task.Delay (1100);

			Assert.AreEqual (1, expanded, "Expected the f1 node to be expanded.");

			controller.ClearAll ();

			Assert.AreEqual (1, cleared, "Expected ClearAll to clear the values.");
		}
	}
}
