// 
// ContextActionExtensions.cs
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
using Mono.TextEditor;
using ICSharpCode.NRefactory.CSharp;
using MonoDevelop.Projects.Dom;

namespace MonoDevelop.CSharp.QuickFix
{
	public static class ContextActionExtensions
	{
		public static void Replace (this AstNode node, MonoDevelop.Ide.Gui.Document doc, AstNode replaceWith)
		{
			string text = CSharpQuickFix.OutputNode (doc, replaceWith, "").Trim ();
		
			int offset = doc.Editor.LocationToOffset (node.StartLocation.Line, node.StartLocation.Column);
			int endOffset = doc.Editor.LocationToOffset (node.EndLocation.Line, node.EndLocation.Column);
				
			doc.Editor.Replace (offset, endOffset - offset, text);
		}
		
		public static void FormatText (this AstNode node, MonoDevelop.Ide.Gui.Document doc)
		{
			doc.UpdateParseDocument ();
			MonoDevelop.CSharp.Formatting.OnTheFlyFormatter.Format (doc, doc.Dom, new DomLocation (node.StartLocation.Line, node.StartLocation.Column));
		}
	}
}

