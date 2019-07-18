//
// ObjectValueNodeEventArgs.cs
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

namespace MonoDevelop.Debugger
{
	public class ObjectValueNodeEventArgs : EventArgs
	{
		public ObjectValueNodeEventArgs (ObjectValueNode node)
		{
			Node = node;
		}

		public ObjectValueNode Node {
			get; private set;
		}
	}

	public sealed class ObjectValueNodeEvaluationCompletedEventArgs : ObjectValueNodeEventArgs
	{
		public ObjectValueNodeEvaluationCompletedEventArgs (ObjectValueNode node, ObjectValueNode [] replacementNodes) : base (node)
		{
			ReplacementNodes = replacementNodes;
		}

		/// <summary>
		/// Gets an array of nodes that should be used to replace the node that finished evaluating.
		/// Some sets of values, like local variables, frame locals and the like are fetched asynchronously
		/// and may take some time to fetch. In this case, a single object is returned that is a place holder
		/// for 0 or more values that should be expanded in the place of the evaluating node.
		/// </summary>
		public ObjectValueNode[] ReplacementNodes { get; }
	}

	public sealed class ObjectValueNodeChildrenChangedEventArgs : ObjectValueNodeEventArgs
	{
		public ObjectValueNodeChildrenChangedEventArgs (ObjectValueNode node, int index, int count) : base (node)
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
}
