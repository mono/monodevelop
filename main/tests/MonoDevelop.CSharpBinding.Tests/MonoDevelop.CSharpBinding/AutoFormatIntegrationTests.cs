//
// AutoFormatIntegrationTests.cs
//
// Author:
//       Mike Krüger <mikkrg@microsoft.com>
//
// Copyright (c) 2018 Microsoft Corporation. All rights reserved.
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
using MonoDevelop.Ide.Gui;
using MonoDevelop.CSharp.Formatting;
using MonoDevelop.CSharp.Completion;
using MonoDevelop.Ide.Editor;
using MonoDevelop.Ide.Editor.Extension;
using MonoDevelop.Projects;
using MonoDevelop.Core;
using System.Threading.Tasks;
using MonoDevelop.Ide.Gui.Content;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Gui.Documents;

namespace MonoDevelop.CSharpBinding
{
	[TestFixture]
	class AutoFormatIntegrationTests : ICSharpCode.NRefactory6.TestBase
	{
		static async Task Simulate (string input, Action<TestViewContent, EditorFormattingServiceTextEditorExtension> act, CSharpFormattingPolicy formattingPolicy = null, EolMarker eolMarker = EolMarker.Unix)
		{
			var content = new TestViewContent ();
			await content.Initialize (new FileDescriptor ("/a.cs", null, null));

			content.Editor.Options = new CustomEditorOptions {
				IndentStyle = IndentStyle.Auto
			};

			content.Editor.MimeType = "text/x-csharp";

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

			var project = IdeServices.ProjectService.CreateProject ("C#");
			project.Name = "test";
			project.FileName = "test.csproj";
			project.Files.Add (new ProjectFile (content.FilePath, BuildAction.Compile));
			var textStylePolicy = Projects.Policies.PolicyService.InvariantPolicies.Get<TextStylePolicy> ().WithTabsToSpaces (false)
										  .WithEolMarker (eolMarker);

			project.Policies.Set (textStylePolicy, content.Editor.MimeType);
			project.Policies.Set (formattingPolicy ?? Projects.Policies.PolicyService.InvariantPolicies.Get<CSharpFormattingPolicy> (), content.Editor.MimeType);

			var solution = new MonoDevelop.Projects.Solution ();
			solution.AddConfiguration ("", true);
			solution.DefaultSolutionFolder.AddItem (project);
			using (var monitor = new ProgressMonitor ())
				await IdeApp.TypeSystemService.Load (solution, monitor);
			content.Owner = project;

			using (var testCase = await Ide.TextEditorExtensionTestCase.Create (content, null, false)) {
				var doc = testCase.Document;
				var compExt = new CSharpCompletionTextEditorExtension ();
				compExt.Initialize (doc.Editor, doc.DocumentContext);
				content.AddContent (compExt);

				var ext = new EditorFormattingServiceTextEditorExtension ();
				ext.Initialize (doc.Editor, doc.DocumentContext);
				content.AddContent (ext);

				await doc.DocumentContext.UpdateParseDocument ();
				if (selectionStart >= 0 && selectionEnd >= 0)
					content.GetTextEditorData ().SetSelection (selectionStart, selectionEnd);

				act (content, ext);
			}
		}

		[Test]
		public async Task TestSemicolon ()
		{
			await Simulate (@"class Foo
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
		public async Task TestCloseBrace ()
		{
			await Simulate (@"class Foo
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
		public async Task TestCloseBraceIf ()
		{
			//Notice that some text stay unformatted by design
			await Simulate (@"class Foo
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
		public async Task TestCloseBraceCatch ()
		{
			//Notice that some text stay unformatted by design
			await Simulate (@"class Foo
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

		[Test]
		public async Task TestBug14686Case2 ()
		{
			await Simulate ("$\"\\\"", (content, ext) => {
				content.Editor.InsertText (0, "@");
				ext.KeyPress (KeyDescriptor.FromGtk ((Gdk.Key)'@', '@', Gdk.ModifierType.None));
				var newText = content.Text;
				Assert.AreEqual ("@\"\\\"", newText);
			});

			await Simulate ("$\"\\\"a", (content, ext) => {
				content.Editor.InsertText (0, "@");
				ext.KeyPress (KeyDescriptor.FromGtk ((Gdk.Key)'@', '@', Gdk.ModifierType.None));
				var newText = content.Text;
				Assert.AreEqual ("@\"\\\"a", newText);
			});

		}


		/// <summary>
		/// Bug 16174 - Editor still inserting unwanted tabs
		/// </summary>
		[Test]
		public async Task TestBug16174_AutoIndent ()
		{
			await Simulate ("namespace Foo\n{\n\tpublic class Bar\n\t{\n$\t\tvoid Test()\n\t\t{\n\t\t}\n\t}\n}\n", (content, ext) => {
				var options = new CustomEditorOptions ();
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
		public async Task TestAfterCommentLine ()
		{
			await Simulate (@"class Foo
{
	void Test ()
	{
		//random comment
		Console.WriteLine ()      ;$
	}
}", (content, ext) => {
				content.Editor.Options = new CustomEditorOptions {
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

		// Bug 44747 - Automatic indentation of preprocessor directives is not consistent
		[Test]
		public async Task TestBug44747 ()
		{
			await Simulate (@"class Foo
{
	void Test()
	{
		#$
	}
}", (content, ext) => {
				content.Editor.Options = new CustomEditorOptions {
					IndentStyle = IndentStyle.Virtual
				};
				ext.KeyPress (KeyDescriptor.FromGtk ((Gdk.Key)'#', '#', Gdk.ModifierType.None));

				var newText = content.Text;
				Assert.AreEqual (@"class Foo
{
	void Test()
	{
#
	}
}", newText);
			});
		}

		[Test]
		public async Task TestBug44747_regions ()
		{
			await Simulate (@"class Foo
{
	void Test()
	{
#region$
	}
}", (content, ext) => {
				content.Editor.Options = new CustomEditorOptions {
					IndentStyle = IndentStyle.Virtual
				};
				ext.KeyPress (KeyDescriptor.FromGtk ((Gdk.Key)'n', 'n', Gdk.ModifierType.None));

				var newText = content.Text;
				Assert.AreEqual (@"class Foo
{
	void Test()
	{
		#region
	}
}", newText);
			});

			await Simulate (@"class Foo
{
	void Test()
	{
		#region foo
#endregion$
	}
}", (content, ext) => {
				content.Editor.Options = new CustomEditorOptions {
					IndentStyle = IndentStyle.Virtual
				};
				ext.KeyPress (KeyDescriptor.FromGtk ((Gdk.Key)'n', 'n', Gdk.ModifierType.None));

				var newText = content.Text;
				Assert.AreEqual (@"class Foo
{
	void Test()
	{
		#region foo
		#endregion
	}
}", newText);
			});
		}

		// Bug 44747 - Automatic indentation of preprocessor directives is not consistent
		[Test]
		public async Task TestBug17902 ()
		{
			await Simulate (@"class Test17902
{
    public void Foo()
    {
		{
		if (true)
		{
			if (true)
			{
			}
			else
			{
				System.Console.WriteLine(1);
			}
		}
		else
		{
			System.Console.WriteLine(2);
		}
		}$
    }
}", (content, ext) => {
				content.Editor.Options = new CustomEditorOptions {
					IndentStyle = IndentStyle.Virtual
				};
				ext.KeyPress (KeyDescriptor.FromGtk (Gdk.Key.braceright, '}', Gdk.ModifierType.None));

				var newText = content.Text;
				Assert.AreEqual (@"class Test17902
{
    public void Foo()
    {
		{
			if (true)
			{
				if (true)
				{
				}
				else
				{
					System.Console.WriteLine(1);
				}
			}
			else
			{
				System.Console.WriteLine(2);
			}
		}
    }
}", newText);
			});
		}

		/// <summary>
		/// Bug 46817 - Xamarin Studio hides characters in auto format
		/// </summary>
		[Test]
		public async Task TestBug46817 ()
		{
			await Simulate ("public class Application\r\n{\r\n\tstatic void Main (string[] args)\r\n\t{\r\n\t\t// abcd\r\n\t\t{\r\n\t\t\t\t}$\r\n", (content, ext) => {
				content.Editor.Options = new CustomEditorOptions {
					IndentStyle = IndentStyle.Virtual,
					DefaultEolMarker = "\r\n"
				};
				ext.KeyPress (KeyDescriptor.FromGtk ((Gdk.Key)'}', '}', Gdk.ModifierType.None));

				var newText = content.Text;
				Assert.AreEqual ("public class Application\r\n{\r\n\tstatic void Main (string[] args)\r\n\t{\r\n\t\t// abcd\r\n\t\t{\r\n\t\t}\r\n", newText);
			}, eolMarker: EolMarker.Windows);
		}

		/// <summary>
		/// Bug 59287 - [VSFeedback Ticket] #490276 - Automatic Space Inserted in Parenthesis (edit)
		/// </summary>
		[Test]
		public async Task TestBug59287 ()
		{
			var policy = Projects.Policies.PolicyService.InvariantPolicies.Get<CSharpFormattingPolicy> ();
			policy = policy.Clone ();
			policy.SpaceWithinOtherParentheses = true;

			await Simulate (@"
using System;
class MyContext
{
	public static void Main()
	{
		if (something == f$)
	}
}", (content, ext) => {
				ext.KeyPress (KeyDescriptor.FromGtk ((Gdk.Key)'f', 'f', Gdk.ModifierType.None));
				Assert.AreEqual (@"
using System;
class MyContext
{
	public static void Main()
	{
		if (something == f)
	}
}", content.Text);
			}, policy);
		}

		/// <summary>
		/// Fixes VSTS Bug 780196: [Feedback] ディレクティブの入力時に他のregionが開く-Another #6998
		/// </summary>
		[Test]
		public async Task TestBug780196 ()
		{
			await Simulate (@"class Foo
{
#region foo
	void Test()
	{
	}
#endregion

	#$

}", (content, ext) => {
				content.Editor.Options = new CustomEditorOptions {
					IndentStyle = IndentStyle.Virtual
				};
				ext.KeyPress (KeyDescriptor.FromGtk ((Gdk.Key)'#', '#', Gdk.ModifierType.None));

				var newText = content.Text;
				Assert.AreEqual (@"class Foo
{
#region foo
	void Test()
	{
	}
#endregion

#

}", newText);
			});
		}
	}
}
