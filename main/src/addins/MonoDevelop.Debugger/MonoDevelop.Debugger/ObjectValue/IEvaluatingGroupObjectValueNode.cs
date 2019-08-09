//
// IEvaluatingGroupObjectValueNode.cs
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
		ObjectValueNode[] GetEvaluationGroupReplacementNodes ();
	}
}
