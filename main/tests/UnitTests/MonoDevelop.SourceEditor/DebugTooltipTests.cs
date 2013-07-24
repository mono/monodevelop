//
// DebugTooltipTests.cs
//
// Author: Jeffrey Stedfast <jeff@xamarin.com>
//
// Copyright (c) 2013 Xamarin Inc.
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
using NUnit.Framework;
using Mono.TextEditor;
using ICSharpCode.NRefactory.Semantics;
using ICSharpCode.NRefactory.TypeSystem;
using MonoDevelop.CSharp.Resolver;
using MonoDevelop.CSharpBinding;
using MonoDevelop.CSharpBinding.Tests;
using MonoDevelop.CSharp.Completion;
using MonoDevelop.Ide.Gui;
using ICSharpCode.NRefactory.CSharp;
using ICSharpCode.NRefactory.CSharp.TypeSystem;
using ICSharpCode.NRefactory.CSharp.Resolver;
using MonoDevelop.CSharp;

namespace MonoDevelop.SourceEditor
{
	[TestFixture ()]
	public class DebugTooltipTests
	{
		static Document Setup (string input)
		{
			var tww = new TestWorkbenchWindow ();
			var content = new TestViewContent ();
			tww.ViewContent = content;
			content.ContentName = "a.cs";
			content.GetTextEditorData ().Document.MimeType = "text/x-csharp";
			var doc = new Document (tww);

			var text = input;
			int endPos = text.IndexOf ('$');
			if (endPos >= 0)
				text = text.Substring (0, endPos) + text.Substring (endPos + 1);

			content.Text = text;
			content.CursorPosition = Math.Max (0, endPos);

			var compExt = new CSharpCompletionTextEditorExtension ();
			compExt.Initialize (doc);
			content.Contents.Add (compExt);

			doc.UpdateParseDocument ();
			return doc;
		}

		static string ResolveExpression (Document doc, int offset)
		{
			var editor = doc.Editor;
			var unit = doc.ParsedDocument.GetAst<SyntaxTree> ();
			if (unit == null)
				return null;

			var file = doc.ParsedDocument.ParsedFile as CSharpUnresolvedFile;
			if (file == null)
				return null;

			ResolveResult result;
			AstNode node;
			var loc = editor.OffsetToLocation (offset);
			if (!doc.TryResolveAt (loc, out result, out node))
				return null;
			if (result is LocalResolveResult)
				return ((LocalResolveResult)result).Variable.Name;
			return editor.GetTextBetween (node.StartLocation, node.EndLocation);
		}

		[Test ()]
		public void TestLocalVariables ()
		{
			var content = @"// Test local variables
using System;

namespace DebuggerTooltipTests
{
	class MainClass
	{
		public int MyField {
			get; set;
		}

		public static void Main (string[] args)
		{
			int basicLocalVariable = 5;
			int localVarFromMethod = Foo ();
			int castingLocalVariable = ((Foo) args [0]).FooValue;
			int localVarFromProperties = this.FirstProperty.SecondProperty;
			int propertyFromBase = base.BaseProperty;

			MyField = 7;
		}
	}
}
";
			var document = Setup (content);
			Assert.AreEqual ("basicLocalVariable", ResolveExpression (document, content.IndexOf ("basicLocalVariable") + 1));
			Assert.AreEqual ("localVarFromMethod", ResolveExpression (document, content.IndexOf ("localVarFromMethod") + 1));
			Assert.AreEqual ("((Foo) args [0]).FooValue", ResolveExpression (document, content.IndexOf ("FooValue") + 1));
			Assert.AreEqual ("this.FirstProperty", ResolveExpression (document, content.IndexOf ("FirstProperty") + 1));
			Assert.AreEqual ("this.FirstProperty.SecondProperty", ResolveExpression (document, content.IndexOf ("SecondProperty") + 1));
			Assert.AreEqual ("base.BaseProperty", ResolveExpression (document, content.IndexOf ("BaseProperty") + 1));
			Assert.AreEqual ("MyField", ResolveExpression (document, content.LastIndexOf ("MyField") + 1));
		}
	}
}
