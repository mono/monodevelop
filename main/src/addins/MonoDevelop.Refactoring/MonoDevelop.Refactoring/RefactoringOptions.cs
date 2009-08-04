// 
// RefactoringOptions.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2009 Novell, Inc (http://www.novell.com)
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
using MonoDevelop.Projects.Dom.Parser;
using MonoDevelop.Projects.Dom;
using MonoDevelop.Ide.Gui.Content;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Core.Gui;
using System.Text;

namespace MonoDevelop.Refactoring
{
	public class RefactoringOptions
	{
		public ProjectDom Dom {
			get;
			set;
		}
		
		public Document Document {
			get;
			set;
		}
		
		public IDomVisitable SelectedItem {
			get;
			set;
		}
		
		public ResolveResult ResolveResult {
			get;
			set;
		}
		
		public string MimeType {
			get {
				return DesktopService.GetMimeTypeForUri (Document.FileName);
			}
		}
		
		public Mono.TextEditor.TextEditorData GetTextEditorData ()
		{
			Mono.TextEditor.ITextEditorDataProvider view = Document.ActiveView as Mono.TextEditor.ITextEditorDataProvider;
			if (view == null)
				return null;

			return view.GetTextEditorData ();
		}
		
		public INRefactoryASTProvider GetASTProvider ()
		{
			return RefactoringService.GetASTProvider (MimeType);
		}
		
		public IResolver GetResolver ()
		{
			MonoDevelop.Projects.Dom.Parser.IParser domParser = GetParser ();
			if (domParser == null)
				return null;
			return domParser.CreateResolver (Dom, Document, Document.FileName);
		}
		
		public MonoDevelop.Projects.Dom.Parser.IParser GetParser ()
		{
			return ProjectDomService.GetParser (Document.FileName, MimeType);
		}
		
		public string GetWhitespaces (int insertionOffset)
		{
			StringBuilder result = new StringBuilder ();
			for (int i = insertionOffset; i < Document.TextEditor.TextLength; i++) {
				char ch = Document.TextEditor.GetCharAt (i);
				if (ch == ' ' || ch == '\t') {
					result.Append (ch);
				} else {
					break;
				}
			}
			return result.ToString ();
		}
		
		public string GetIndent (IMember member)
		{
			return GetWhitespaces (Document.TextEditor.GetPositionFromLineColumn (member.Location.Line, 1));
		}
		
		public ParsedDocument ParseDocument ()
		{
			return ProjectDomService.Parse (Dom.Project, Document.FileName, DesktopService.GetMimeTypeForUri (Document.FileName), Document.TextEditor.Text);
		}
		
	}
}
