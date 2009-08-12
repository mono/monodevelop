//
// Visitor.cs: base class for walking an ASP.NET document tree
//
// Authors:
//   Michael Hutchinson <m.j.hutchinson@gmail.com>
//
// Copyright (C) 2006 Michael Hutchinson
//
//
// This source code is licenced under The MIT License:
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;

namespace MonoDevelop.AspNet.Parser.Dom
{
	/// <summary>
	/// If passed to a node, a visitor will be walked along the tree and one of these methods will be called 
	/// for each node.
	/// </summary>
	public abstract class Visitor
	{
		bool quickExit = false;
		
		/// <value>
		/// If false, the visitor will stop walking the data structure.
		/// </value>
		public bool QuickExit {
			get { return quickExit; }
			set { quickExit = true; }
		}
		
		public virtual void Visit (DirectiveNode node)
		{
		}
		
		public virtual void Visit (TextNode node)
		{
		}
		
		public virtual void Visit (TagNode node)
		{
		}
		
		public virtual void Visit (ExpressionNode node)
		{
		}
		
		public virtual void Visit (RootNode node)
		{
		}
		
		public virtual void Visit (ServerCommentNode node)
		{
		}
		
		public virtual void Visit (ServerIncludeNode node)
		{
		}
		
		public virtual void Leave (ParentNode node)
		{
		}
	}
}
