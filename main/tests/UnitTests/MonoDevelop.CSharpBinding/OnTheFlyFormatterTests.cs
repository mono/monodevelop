// 
// OnTheFlyFormatterTests.cs
//  
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
// 
// Copyright (c) 2012 Xamarin Inc. (http://xamarin.com)
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

using MonoDevelop.CSharp.Parser;
using Mono.TextEditor;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using ICSharpCode.NRefactory;
using ICSharpCode.NRefactory.TypeSystem;
using MonoDevelop.Ide.TypeSystem;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Gui.Content;
using MonoDevelop.CSharp.Formatting;
using UnitTests;
using MonoDevelop.Projects.Policies;
using MonoDevelop.CSharp.Completion;
using MonoDevelop.CSharpBinding.Tests;

namespace MonoDevelop.CSharpBinding
{
	[TestFixture]
	public class OnTheFlyFormatterTests : UnitTests.TestBase
	{
		static CSharpTextEditorIndentation Setup (string input, out TestViewContent content)
		{
			TestWorkbenchWindow tww = new TestWorkbenchWindow ();
			content = new TestViewContent ();
			content.Data.Options.IndentStyle = IndentStyle.Auto;
			tww.ViewContent = content;
			content.ContentName = "a.cs";
			content.GetTextEditorData ().Document.MimeType = "text/x-csharp";

			Document doc = new Document (tww);

			var text = input;
			int endPos = text.IndexOf ('$');
			if (endPos >= 0)
				text = text.Substring (0, endPos) + text.Substring (endPos + 1);

			content.Text = text;
			content.CursorPosition = System.Math.Max (0, endPos);


			var compExt = new CSharpCompletionTextEditorExtension ();
			compExt.Initialize (doc);
			content.Contents.Add (compExt);
			
			var ext = new CSharpTextEditorIndentation ();
			CSharpTextEditorIndentation.OnTheFlyFormatting = true;
			ext.Initialize (doc);
			content.Contents.Add (ext);
			
			doc.UpdateParseDocument ();
			return ext;
		}

		[Ignore("Semicolon formatting partially deactivated.")]
		[Test]
		public void TestSemicolon ()
		{
			TestViewContent content;
			var ext = Setup (@"class Foo
{
	void Test ()
	{
		Console.WriteLine ()      ;$
	}
}", out content);
			ext.KeyPress (Gdk.Key.semicolon, ';', Gdk.ModifierType.None);
			
			var newText = content.Text;
			Assert.AreEqual (@"class Foo
{
	void Test ()
	{
		Console.WriteLine ();
	}
}", newText);

		}

		[Ignore("FIXME")]
		[Test]
		public void TestCloseBrace ()
		{
			TestViewContent content;
			var ext = Setup (@"class Foo
{
	void Test ()
	{
		Console.WriteLine()                   ;
	}$
}", out content);
			ext.KeyPress (Gdk.Key.braceright, '}', Gdk.ModifierType.None);
			
			var newText = content.Text;
			Console.WriteLine (newText);
			Assert.AreEqual (@"class Foo
{
	void Test ()
	{
		Console.WriteLine ();
	}
}", newText);

		}

		
		/// <summary>
		/// Bug 5080 - Pressing tab types /t instead of tabbing
		/// </summary>
		[Test]
		public void TestBug5080 ()
		{
			TestViewContent content;
			var ext = Setup ("\"Hello\n\t$", out content);
			ext.ReindentOnTab ();

			var newText = content.Text;
			Assert.AreEqual ("\"Hello\n", newText);
		}


		[Test]
		public void TestVerbatimToNonVerbatimConversion ()
		{
			TestViewContent content;
			Setup ("@$\"\t\"", out content);
			content.GetTextEditorData ().Remove (0, 1);
			var newText = content.Text;
			Assert.AreEqual ("\"\\t\"", newText);
		}

		[Test]
		public void TestNonVerbatimToVerbatimConversion ()
		{
			TestViewContent content;
			var ext = Setup ("$\"\\t\"", out content);
			content.GetTextEditorData ().Insert (0, "@");
			ext.KeyPress ((Gdk.Key)'@', '@', Gdk.ModifierType.None);
			var newText = content.Text;
			Assert.AreEqual ("@\"\t\"", newText);
		}

		/// <summary>
		/// Bug 14686 - Relative path strings containing backslashes have incorrect behavior when removing the @ symbol.
		/// </summary>
		[Test]
		public void TestBug14686 ()
		{
			TestViewContent content;
			var ext = Setup ("$\"\\\\\"", out content);
			content.GetTextEditorData ().Insert (0, "@");
			ext.KeyPress ((Gdk.Key)'@', '@', Gdk.ModifierType.None);
			var newText = content.Text;
			Assert.AreEqual ("@\"\\\"", newText);
		}

		[Test]
		public void TestBug14686Case2 ()
		{
			TestViewContent content;
			var ext = Setup ("$\"\\\"", out content);
			content.GetTextEditorData ().Insert (0, "@");
			ext.KeyPress ((Gdk.Key)'@', '@', Gdk.ModifierType.None);
			var newText = content.Text;
			Assert.AreEqual ("@\"\\\"", newText);

			ext = Setup ("$\"\\\"a", out content);
			content.GetTextEditorData ().Insert (0, "@");
			ext.KeyPress ((Gdk.Key)'@', '@', Gdk.ModifierType.None);
			newText = content.Text;
			Assert.AreEqual ("@\"\\\"a", newText);

		}
		[Test]
		public void TestCorrectReindentNextLine ()
		{
			TestViewContent content;
			var ext = Setup (@"
class Foo
{
	void Bar ()
	{
		try {
		} catch (Exception e) {$}
	}
}
", out content);
			ext.ReindentOnTab ();
			MiscActions.InsertNewLine (content.Data);
			ext.KeyPress ((Gdk.Key)'\n', '\n', Gdk.ModifierType.None);

			var newText = content.Text;

			var expected = @"
class Foo
{
	void Bar ()
	{
		try {
		} catch (Exception e) {
		}
	}
}
";
			if (newText != expected)
				Console.WriteLine (newText);
			Assert.AreEqual (expected, newText);
		}

		/// <summary>
		/// Bug 16174 - Editor still inserting unwanted tabs
		/// </summary>
		[Test]
		public void TestBug16174_AutoIndent ()
		{
			TestViewContent content;

			var ext = Setup  ("namespace Foo\n{\n\tpublic class Bar\n\t{\n$\t\tvoid Test()\n\t\t{\n\t\t}\n\t}\n}\n", out content);
			ext.document.Editor.Options.IndentStyle = IndentStyle.Auto;
			MiscActions.InsertNewLine (content.Data);
			ext.KeyPress (Gdk.Key.Return, '\n', Gdk.ModifierType.None);

			var newText = content.Text;

			var expected = "namespace Foo\n{\n\tpublic class Bar\n\t{\n\n\t\tvoid Test()\n\t\t{\n\t\t}\n\t}\n}\n";
			if (newText != expected)
				Console.WriteLine (newText);
			Assert.AreEqual (expected, newText);
		}

		[Test]
		public void TestBug16174_VirtualIndent ()
		{
			TestViewContent content;

			var ext = Setup  ("namespace Foo\n{\n\tpublic class Bar\n\t{\n$\t\tvoid Test()\n\t\t{\n\t\t}\n\t}\n}\n", out content);
			ext.document.Editor.Options.IndentStyle = IndentStyle.Virtual;
			MiscActions.InsertNewLine (content.Data);
			ext.KeyPress (Gdk.Key.Return, '\n', Gdk.ModifierType.None);

			var newText = content.Text;

			var expected = "namespace Foo\n{\n\tpublic class Bar\n\t{\n\n\t\tvoid Test()\n\t\t{\n\t\t}\n\t}\n}\n";
			if (newText != expected)
				Console.WriteLine (newText);
			Assert.AreEqual (expected, newText);
		}


		/// <summary>
		/// Bug 16283 - Wrong literal string addition
		/// </summary>
		[Test]
		public void TestBug16283 ()
		{
			TestViewContent content;
			var ext = Setup ("$\"\\dev\\null {0}\"", out content);
			content.GetTextEditorData ().Insert (0, "@");
			ext.KeyPress ((Gdk.Key)'@', '@', Gdk.ModifierType.None);
			var newText = content.Text;
			Assert.AreEqual ("@\"\\dev\null {0}\"", newText);
		}
	}
}

