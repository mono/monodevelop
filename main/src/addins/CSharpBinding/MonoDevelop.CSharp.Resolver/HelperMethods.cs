// 
// HelperMethods.cs
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
using System.Linq;
using System.IO;
using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Drawing;
using System.Text;
using MonoDevelop.Core;
using MonoDevelop.Projects.Dom;
using MonoDevelop.Projects.Dom.Parser;
using MonoDevelop.Projects;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Gui.Content;
using MonoDevelop.Ide.CodeTemplates;
using MonoDevelop.Ide.CodeCompletion;
using MonoDevelop.Refactoring;
using ICSharpCode.OldNRefactory.Visitors;
using ICSharpCode.OldNRefactory.Parser;
using ICSharpCode.OldNRefactory.Ast;
using ICSharpCode.OldNRefactory;
using MonoDevelop.CSharp.Parser;
using MonoDevelop.CSharp.Completion;
using Mono.TextEditor;
using ICSharpCode.NRefactory.CSharp;

namespace MonoDevelop.CSharp
{
	static class HelperMethods
	{
		public static void SetText (this CompletionData data, string text)
		{
			if (data is CompletionData) {
				((CompletionData)data).CompletionText = text;
			} else if (data is MonoDevelop.Ide.CodeCompletion.MemberCompletionData) {
				((MonoDevelop.Ide.CodeCompletion.MemberCompletionData)data).CompletionText = text;
			} else {
				System.Console.WriteLine("Unknown completion data:" + data);
			}
		}
		
		public static ICSharpCode.NRefactory.CSharp.CompilationUnit Parse (this ICSharpCode.NRefactory.CSharp.CSharpParser parser, TextEditorData data)
		{
			using (var stream = data.OpenStream ()) {
				return parser.Parse (stream);
			}
		}
		
		public static AstNode ParseSnippet (this ICSharpCode.NRefactory.CSharp.CSharpParser parser, TextEditorData data)
		{
			using (var stream = new  StreamReader (data.OpenStream ())) {
				var result = parser.ParseExpression (stream);
				if (!parser.HasErrors)
					return result;
			}
			parser.ErrorPrinter.Reset ();
			using (var stream = new  StreamReader (data.OpenStream ())) {
				var result = parser.ParseStatements (stream);
				if (!parser.HasErrors)
					return result.First ();
			}
			parser.ErrorPrinter.Reset ();
			using (var stream = data.OpenStream ()) {
				return parser.Parse (stream);
			}
		}
	}
}
