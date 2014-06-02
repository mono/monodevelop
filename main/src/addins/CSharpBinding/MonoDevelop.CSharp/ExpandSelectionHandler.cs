// 
// ExpandSelectionHandler.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2011 Novell, Inc (http://www.novell.com)
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
using ICSharpCode.NRefactory.CSharp;
using MonoDevelop.Components.Commands;
using MonoDevelop.Ide;
using System.Collections.Generic;

namespace MonoDevelop.CSharp
{
	enum ExpandCommands
	{
		ExpandSelection,
		ShrinkSelection
	}
	
	class ExpandSelectionHandler : CommandHandler
	{
		protected override void Run ()
		{
			var doc = IdeApp.Workbench.ActiveDocument;
			var parsedDocument = doc.ParsedDocument;
			if (parsedDocument == null)
				return;
			var unit = parsedDocument.GetAst<SyntaxTree> ();
			if (unit == null)
				return;
			var node = unit.GetNodeAt (doc.Editor.Caret.Location);
			if (node == null)
				return;
			
			if (doc.Editor.IsSomethingSelected) {
				while (node != null && doc.Editor.MainSelection.IsSelected (node.StartLocation, node.EndLocation)) {
					node = node.Parent;
				}
			}
			
			if (node != null)
				doc.Editor.SetSelection (node.StartLocation, node.EndLocation);
		}
	}
	
	class ShrinkSelectionHandler : CommandHandler
	{
		protected override void Run ()
		{
			var doc = IdeApp.Workbench.ActiveDocument;
			var parsedDocument = doc.ParsedDocument;
			if (parsedDocument == null)
				return;
			var unit = parsedDocument.GetAst<SyntaxTree> ();
			if (unit == null)
				return;
			var node = unit.GetNodeAt (doc.Editor.Caret.Line, doc.Editor.Caret.Column);
			if (node == null)
				return;
			var nodeStack = new Stack<AstNode> ();
			nodeStack.Push (node);
			if (doc.Editor.IsSomethingSelected) {
				while (node != null && doc.Editor.MainSelection.IsSelected (node.StartLocation, node.EndLocation)) {
					node = node.Parent;
					if (node != null) {
						if (nodeStack.Count > 0 && nodeStack.Peek ().StartLocation == node.StartLocation && nodeStack.Peek ().EndLocation == node.EndLocation)
							nodeStack.Pop ();
						nodeStack.Push (node);
					}
				}
			}
			
			if (nodeStack.Count > 2) {
				nodeStack.Pop (); // parent
				nodeStack.Pop (); // current node
				node = nodeStack.Pop (); // next children in which the caret is
				doc.Editor.SetSelection (node.StartLocation, node.EndLocation);
			} else {
				doc.Editor.ClearSelection ();
			}
		}
	}
}

