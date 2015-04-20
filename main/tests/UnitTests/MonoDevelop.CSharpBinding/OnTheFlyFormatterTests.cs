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

using System.Text;
using MonoDevelop.Ide.TypeSystem;
using MonoDevelop.Ide.Gui;
using MonoDevelop.CSharp.Formatting;
using MonoDevelop.CSharp.Completion;
using MonoDevelop.CSharpBinding.Tests;
using MonoDevelop.Ide.Editor;
using MonoDevelop.Ide.Editor.Extension;
using MonoDevelop.Projects;
using MonoDevelop.Core.ProgressMonitoring;
using MonoDevelop.Core;

namespace MonoDevelop.CSharpBinding
{
	[TestFixture]
	public class OnTheFlyFormatterTests : UnitTests.TestBase
	{
		static void Simulate (string input, Action<TestViewContent, CSharpTextEditorIndentation> act)
		{
			TestWorkbenchWindow tww = new TestWorkbenchWindow ();
			var content = new TestViewContent ();
			content.Data.Options = new CustomEditorOptions {
				IndentStyle = IndentStyle.Auto
			};

			tww.ViewContent = content;
			content.ContentName = "/a.cs";
			content.Data.MimeType = "text/x-csharp";

			var doc = new Document (tww);

			var sb = new StringBuilder ();
			int cursorPosition = 0, selectionStart = -1, selectionEnd = -1;

			for (int i = 0; i < input.Length; i++) {
				var ch = input [i];
				switch (ch) {
				case '$':
					cursorPosition = sb.Length;
					break;
				case '<':
					if (i + 1 < input.Length) {
						if (input [i + 1] == '-') {
							selectionStart = sb.Length;
							i++;
							break;
						}
					}
					goto default;
				case '-':
					if (i + 1 < input.Length) {
						var next = input [i + 1];
						if (next == '>') {
							selectionEnd = sb.Length;
							i++;
							break;
						}
					}
					goto default;
				default:
					sb.Append (ch);
					break;
				}
			}
			content.Text = sb.ToString ();
			content.CursorPosition = cursorPosition;

			var project = Services.ProjectService.CreateProject ("C#");
			project.Name = "test";
			project.FileName = "test.csproj";
			project.Files.Add (new ProjectFile (content.ContentName, BuildAction.Compile)); 
			project.Policies.Set (Projects.Policies.PolicyService.InvariantPolicies.Get<CSharpFormattingPolicy> (), CSharpFormatter.MimeType);

			var solution = new MonoDevelop.Projects.Solution ();
			solution.AddConfiguration ("", true); 
			solution.DefaultSolutionFolder.AddItem (project);
			using (var monitor = new ProgressMonitor ())
				TypeSystemService.Load (solution, monitor, false);
			content.Project = project;
			doc.SetProject (project);

			var compExt = new CSharpCompletionTextEditorExtension ();
			compExt.Initialize (doc.Editor, doc);
			content.Contents.Add (compExt);
			
			var ext = new CSharpTextEditorIndentation ();
			CSharpTextEditorIndentation.OnTheFlyFormatting = true;
			ext.Initialize (doc.Editor, doc);
			content.Contents.Add (ext);
			
			doc.UpdateParseDocument ();
			if (selectionStart >= 0 && selectionEnd >= 0)
				content.GetTextEditorData ().SetSelection (selectionStart, selectionEnd);
			try {
				act (content, ext);
			} finally {
				TypeSystemService.Unload (solution);
			}
		}

		[Test]
		public void TestSemicolon ()
		{
			Simulate (@"class Foo
{
	void Test ()
	{
		Console.WriteLine ()      ;$
	}
}", (content, ext) => {
				ext.KeyPress (KeyDescriptor.FromGtk (Gdk.Key.semicolon, ';', Gdk.ModifierType.None));
			
				var newText = content.Text;
				Assert.AreEqual (@"class Foo
{
	void Test ()
	{
		Console.WriteLine();
	}
}", newText);
			});
		}

		[Test]
		public void TestCloseBrace ()
		{
			Simulate (@"class Foo
{
	void Test ()
	{
		Console.WriteLine()                   ;
	}$
}", (content, ext) => {
				ext.KeyPress (KeyDescriptor.FromGtk (Gdk.Key.braceright, '}', Gdk.ModifierType.None));

				var newText = content.Text;
				Console.WriteLine (newText);
				Assert.AreEqual (@"class Foo
{
	void Test()
	{
		Console.WriteLine();
	}
}", newText);
			});

		}

		[Test]
		public void TestCloseBraceIf ()
		{
			//Notice that some text stay unformatted by design
			Simulate (@"class Foo
{
	void Test ()
			{
		Console.WriteLine()                   ;
		if(true){
		Console.WriteLine()                   ;
	}$
	}
}", (content, ext) => {
				ext.KeyPress (KeyDescriptor.FromGtk (Gdk.Key.braceright, '}', Gdk.ModifierType.None));
			
				var newText = content.Text;
				Console.WriteLine (newText);
				Assert.AreEqual (@"class Foo
{
	void Test ()
			{
		Console.WriteLine()                   ;
		if (true)
		{
			Console.WriteLine();
		}
	}
}", newText);
			});
		}

		[Test]
		public void TestCloseBraceCatch ()
		{
			//Notice that some text stay unformatted by design
			Simulate (@"class Foo
{
	void Test ()
			{
		Console.WriteLine()                   ;
					try{
		Console.WriteLine()                   ;
	}catch(Exception e){
	}$
	}
}", (content, ext) => {
				ext.KeyPress (KeyDescriptor.FromGtk (Gdk.Key.braceright, '}', Gdk.ModifierType.None));
			
				var newText = content.Text;
				Console.WriteLine (newText);
				Assert.AreEqual (@"class Foo
{
	void Test ()
			{
		Console.WriteLine()                   ;
		try
		{
			Console.WriteLine();
		}
		catch (Exception e)
		{
		}
	}
}", newText);
			});
		}

		
		/// <summary>
		/// Bug 5080 - Pressing tab types /t instead of tabbing
		/// </summary>
		[Test]
		public void TestBug5080 ()
		{
			Simulate ("\"Hello\n\t$", (content, ext) => {
				ext.ReindentOnTab ();

				var newText = content.Text;
				Assert.AreEqual ("\"Hello\n", newText);
			});
		}


		[Test]
		public void TestVerbatimToNonVerbatimConversion ()
		{
			Simulate ("@$\"\t\"", (content, ext) => {
				content.Data.RemoveText (0, 1);
				var newText = content.Text;
				Assert.AreEqual ("\"\\t\"", newText);
			});
		}

		[Test]
		public void TestNonVerbatimToVerbatimConversion ()
		{
			Simulate ("$\"\\t\"", (content, ext) => {
				content.Data.InsertText (0, "@");
				ext.KeyPress (KeyDescriptor.FromGtk ((Gdk.Key)'@', '@', Gdk.ModifierType.None));
				var newText = content.Text;
				Assert.AreEqual ("@\"\t\"", newText);
			});
		}

		/// <summary>
		/// Bug 14686 - Relative path strings containing backslashes have incorrect behavior when removing the @ symbol.
		/// </summary>
		[Test]
		public void TestBug14686 ()
		{
			Simulate ("$\"\\\\\"", (content, ext) => {
				content.Data.InsertText (0, "@");
				ext.KeyPress (KeyDescriptor.FromGtk ((Gdk.Key)'@', '@', Gdk.ModifierType.None));
				var newText = content.Text;
				Assert.AreEqual ("@\"\\\"", newText);
			});
		}

		[Test]
		public void TestBug14686Case2 ()
		{
			Simulate ("$\"\\\"", (content, ext) => {
				content.Data.InsertText (0, "@");
				ext.KeyPress (KeyDescriptor.FromGtk ((Gdk.Key)'@', '@', Gdk.ModifierType.None));
				var newText = content.Text;
				Assert.AreEqual ("@\"\\\"", newText);
			});

			Simulate ("$\"\\\"a", (content, ext) => {
				content.Data.InsertText (0, "@");
				ext.KeyPress (KeyDescriptor.FromGtk ((Gdk.Key)'@', '@', Gdk.ModifierType.None));
				var newText = content.Text;
				Assert.AreEqual ("@\"\\\"a", newText);
			});

		}
		[Test]
		public void TestCorrectReindentNextLine ()
		{
			Simulate (@"
class Foo
{
	void Bar ()
	{
		try {
		} catch (Exception e) {$}
	}
}
", (content, ext) => {
				ext.ReindentOnTab ();
				EditActions.NewLine (ext.Editor);
				ext.KeyPress (KeyDescriptor.FromGtk ((Gdk.Key)'\n', '\n', Gdk.ModifierType.None));

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
			});
		}

		/// <summary>
		/// Bug 16174 - Editor still inserting unwanted tabs
		/// </summary>
		[Test]
		public void TestBug16174_AutoIndent ()
		{
			Simulate ("namespace Foo\n{\n\tpublic class Bar\n\t{\n$\t\tvoid Test()\n\t\t{\n\t\t}\n\t}\n}\n", (content, ext) => {
				var options = DefaultSourceEditorOptions.Instance;
				options.IndentStyle = IndentStyle.Auto;
				ext.Editor.Options = options;
				EditActions.NewLine (ext.Editor);
				ext.KeyPress (KeyDescriptor.FromGtk (Gdk.Key.Return, '\n', Gdk.ModifierType.None));

				var newText = content.Text;

				var expected = "namespace Foo\n{\n\tpublic class Bar\n\t{\n\n\t\tvoid Test()\n\t\t{\n\t\t}\n\t}\n}\n";
				if (newText != expected)
					Console.WriteLine (newText);
				Assert.AreEqual (expected, newText);
			});
		}

		[Test]
		public void TestBug16174_VirtualIndent ()
		{
			Simulate ("namespace Foo\n{\n\tpublic class Bar\n\t{\n$\t\tvoid Test()\n\t\t{\n\t\t}\n\t}\n}\n", (content, ext) => {
				var options = DefaultSourceEditorOptions.Instance;
				options.IndentStyle = IndentStyle.Virtual;
				ext.Editor.Options = options;
				EditActions.NewLine (ext.Editor);
				ext.KeyPress (KeyDescriptor.FromGtk (Gdk.Key.Return, '\n', Gdk.ModifierType.None));

				var newText = content.Text;

				var expected = "namespace Foo\n{\n\tpublic class Bar\n\t{\n\n\t\tvoid Test()\n\t\t{\n\t\t}\n\t}\n}\n";
				if (newText != expected)
					Console.WriteLine (newText);
				Assert.AreEqual (expected, newText);
			});
		}


		/// <summary>
		/// Bug 16283 - Wrong literal string addition
		/// </summary>
		[Test]
		public void TestBug16283 ()
		{
			Simulate ("$\"\\dev\\null {0}\"", (content, ext) => {
				content.Data.InsertText (0, "@");
				ext.KeyPress (KeyDescriptor.FromGtk ((Gdk.Key)'@', '@', Gdk.ModifierType.None));
				var newText = content.Text;
				Assert.AreEqual ("@\"\\dev\null {0}\"", newText);
			});
		}

		/// <summary>
		/// Bug 17765 - Format selection adding extra leading whitespace on function
		/// </summary>
		[Test]
		public void TestBug17765 ()
		{
			Simulate (@"
namespace FormatSelectionTest
{
	public class EmptyClass
	{
		<-public EmptyClass ()
		{
		}->
	}
}", (content, ext) => {

				OnTheFlyFormatter.Format (ext.Editor, ext.DocumentContext, ext.Editor.SelectionRange.Offset, ext.Editor.SelectionRange.EndOffset); 


				Assert.AreEqual (@"
namespace FormatSelectionTest
{
	public class EmptyClass
	{
		public EmptyClass()
		{
		}
	}
}", ext.Editor.Text);
			});
		}

		[Test]
		public void TestAfterCommentLine ()
		{
			Simulate (@"class Foo
{
	void Test ()
	{
		//random comment
		Console.WriteLine ()      ;$
	}
}", (content, ext) => {
				content.Data.Options = new CustomEditorOptions {
					IndentStyle = IndentStyle.Virtual
				};
				ext.KeyPress (KeyDescriptor.FromGtk (Gdk.Key.semicolon, ';', Gdk.ModifierType.None));
			
				var newText = content.Text;
				Assert.AreEqual (@"class Foo
{
	void Test ()
	{
		//random comment
		Console.WriteLine();
	}
}", newText);
			});
		}
	}
}

