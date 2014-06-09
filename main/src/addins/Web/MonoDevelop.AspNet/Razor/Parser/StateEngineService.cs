//
// StateEngineService.cs
//
// Author:
//		Piotr Dowgiallo <sparekd@gmail.com>
//
// Copyright (c) 2012 Piotr Dowgiallo
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

using System.Diagnostics;
using ICSharpCode.NRefactory;
using MonoDevelop.Core;
using MonoDevelop.Xml.Dom;
using MonoDevelop.Xml.Parser;

namespace MonoDevelop.AspNet.Razor.Parser
{
	public static class StateEngineService
	{
		public static void EndCodeFragment<T> (IXmlParserContext context, int minus = 0) where T : XNode
		{
			EndCodeFragment<T> (context, context.LocationMinus (minus));
		}

		public static void EndCodeFragment<T> (IXmlParserContext context, TextLocation loc) where T : XNode
		{
			var top = context.Nodes.Pop ();
			var node = top as T;
			if (node == null) {
				if (top.IsEnded)
					node = context.Nodes.Pop () as T;
				if (node == null) {
					Debug.Fail ("Unexpected node at the top of the stack");
					LoggingService.LogError ("Error in Razor StateEngine parser: unexpected node at the top of the stack.\n"
						+ "Expected: {0}\n{1}", typeof (T), context.ToString ());
					return;
				}
			}
			node.End (loc);
			if (context.BuildTree) {
				var xObj = context.Nodes.Peek ();
				var container = xObj as XContainer;
				if (container != null)
					container.AddChildNode (node);
			}
		}
	}
}
