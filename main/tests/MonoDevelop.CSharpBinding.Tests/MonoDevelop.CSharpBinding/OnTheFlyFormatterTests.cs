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
using System.Threading.Tasks;
using MonoDevelop.Ide.Gui.Content;

namespace MonoDevelop.CSharpBinding
{
	[TestFixture]
	class OnTheFlyFormatterTests : ICSharpCode.NRefactory6.TestBase
	{
		static async Task Simulate(string input, Action<TestViewContent, CSharpTextEditorIndentation> act, CSharpFormattingPolicy formattingPolicy = null)
		{
			TestWorkbenchWindow tww = new TestWorkbenchWindow();
			var content = new TestViewContent();
			content.Data.Options = new CustomEditorOptions
			{
				IndentStyle = IndentStyle.Auto
			};

			tww.ViewContent = content;
			content.ContentName = "/a.cs";
			content.Data.MimeType = "text/x-csharp";

			var doc = new Document(tww);

			var sb = new StringBuilder();
			int cursorPosition = 0, selectionStart = -1, selectionEnd = -1;

			for (int i = 0; i < input.Length; i++)
			{
				var ch = input[i];
				switch (ch)
				{
					case '$':
						cursorPosition = sb.Length;
						break;
					case '<':
						if (i + 1 < input.Length)
						{
							if (input[i + 1] == '-')
							{
								selectionStart = sb.Length;
								i++;
								break;
							}
						}
						goto default;
					case '-':
						if (i + 1 < input.Length)
						{
							var next = input[i + 1];
							if (next == '>')
							{
								selectionEnd = sb.Length;
								i++;
								break;
							}
						}
						goto default;
					default:
						sb.Append(ch);
						break;
				}
			}
			content.Text = sb.ToString();
			content.CursorPosition = cursorPosition;

			var project = Services.ProjectService.CreateProject("C#");
			project.Name = "test";
			project.FileName = "test.csproj";
			project.Files.Add(new ProjectFile(content.ContentName, BuildAction.Compile));
			var textStylePolicy = Projects.Policies.PolicyService.InvariantPolicies.Get<TextStylePolicy>().WithTabsToSpaces(true);

			project.Policies.Set(textStylePolicy, content.Data.MimeType);
			project.Policies.Set(formattingPolicy  ?? Projects.Policies.PolicyService.InvariantPolicies.Get<CSharpFormattingPolicy>(), content.Data.MimeType);

			var solution = new MonoDevelop.Projects.Solution();
			solution.AddConfiguration("", true);
			solution.DefaultSolutionFolder.AddItem(project);
			using (var monitor = new ProgressMonitor())
				await TypeSystemService.Load(solution, monitor);
			content.Project = project;
			doc.SetProject(project);
			var compExt = new CSharpCompletionTextEditorExtension ();
			compExt.Initialize (doc.Editor, doc);
			content.Contents.Add (compExt);
			
			var ext = new CSharpTextEditorIndentation ();
			CSharpTextEditorIndentation.OnTheFlyFormatting = true;
			ext.Initialize (doc.Editor, doc);
			content.Contents.Add (ext);
			
			await doc.UpdateParseDocument ();
			if (selectionStart >= 0 && selectionEnd >= 0)
				content.GetTextEditorData ().SetSelection (selectionStart, selectionEnd);
			try {
				act (content, ext);
			} finally {
				TypeSystemService.Unload (solution);
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

		
		/// <summary>
		/// Bug 5080 - Pressing tab types /t instead of tabbing
		/// </summary>
		[Test]
		public async Task TestBug5080 ()
		{
			await Simulate ("\"Hello\n\t$", (content, ext) => {
				ext.ReindentOnTab ();

				var newText = content.Text;
				Assert.AreEqual ("\"Hello\n", newText);
			});
		}


		[Test]
		public async Task TestVerbatimToNonVerbatimConversion ()
		{
			await Simulate ("@$\"\t\"", (content, ext) => {
				content.Data.RemoveText (0, 1);
				var newText = content.Text;
				Assert.AreEqual ("\"\\t\"", newText);
			});
		}

		[Test]
		public async Task TestNonVerbatimToVerbatimConversion ()
		{
			await Simulate ("$\"\\t\"", (content, ext) => {
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
		public async Task TestBug14686 ()
		{
			await Simulate ("$\"\\\\\"", (content, ext) => {
				content.Data.InsertText (0, "@");
				ext.KeyPress (KeyDescriptor.FromGtk ((Gdk.Key)'@', '@', Gdk.ModifierType.None));
				var newText = content.Text;
				Assert.AreEqual ("@\"\\\"", newText);
			});
		}

		[Test]
		public async Task TestBug14686Case2 ()
		{
			await Simulate ("$\"\\\"", (content, ext) => {
				content.Data.InsertText (0, "@");
				ext.KeyPress (KeyDescriptor.FromGtk ((Gdk.Key)'@', '@', Gdk.ModifierType.None));
				var newText = content.Text;
				Assert.AreEqual ("@\"\\\"", newText);
			});

			await Simulate ("$\"\\\"a", (content, ext) => {
				content.Data.InsertText (0, "@");
				ext.KeyPress (KeyDescriptor.FromGtk ((Gdk.Key)'@', '@', Gdk.ModifierType.None));
				var newText = content.Text;
				Assert.AreEqual ("@\"\\\"a", newText);
			});

		}
		[Test]
		public async Task TestCorrectReindentNextLine ()
		{
			await Simulate (@"
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
		public async Task TestBug16174_VirtualIndent ()
		{
			await Simulate ("namespace Foo\n{\n\tpublic class Bar\n\t{\n$\t\tvoid Test()\n\t\t{\n\t\t}\n\t}\n}\n", (content, ext) => {
				var options = new CustomEditorOptions {
					IndentStyle = IndentStyle.Virtual,
					TabsToSpaces = false
				};

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
		public async Task TestBug16283 ()
		{
			await Simulate ("$\"\\dev\\null {0}\"", (content, ext) => {
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
		public async Task TestBug17765 ()
		{
			await Simulate (@"
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
				content.Data.Options = new CustomEditorOptions {
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
				content.Data.Options = new CustomEditorOptions {
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
				content.Data.Options = new CustomEditorOptions {
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
				content.Data.Options = new CustomEditorOptions {
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
				content.Data.Options = new CustomEditorOptions {
					IndentStyle = IndentStyle.Virtual,
					DefaultEolMarker = "\r\n"
				};
				ext.KeyPress(KeyDescriptor.FromGtk((Gdk.Key)'}', '}', Gdk.ModifierType.None));

				var newText = content.Text;
				Assert.AreEqual("public class Application\r\n{\r\n\tstatic void Main (string[] args)\r\n\t{\r\n\t\t// abcd\r\n\t\t{\r\n\t\t}\r\n", newText);
			});
		}

		/// <summary>
		/// Bug 38954 - Format document changes position of caret
		/// </summary>
		[Test]
		public async Task TestBug38954 ()
		{
			await Simulate (@"
class EmptyClass
{
    public EmptyClass()
    {
        $Console.WriteLine() ;
    }
}", (content, ext) => { 
				var oldOffset = ext.Editor.CaretOffset;
				OnTheFlyFormatter.Format (ext.Editor, ext.DocumentContext);
				var newOffset = ext.Editor.CaretOffset;
				Assert.AreEqual (oldOffset, newOffset);
			});

		} 


		/// <summary>
		/// Bug 51549 - Format document changes position of caret
		/// </summary>
		[Test]
		public async Task TestBug51549 ()
		{
			await Simulate (@"
using System;

class MyContext
{
    public static void Main()
    {
        Console.WriteLine   $   (""Hello world!"");
    }
}", (content, ext) => {
				var oldOffset = ext.Editor.CaretOffset;
				OnTheFlyFormatter.Format (ext.Editor, ext.DocumentContext);
				var newOffset = ext.Editor.CaretOffset;
				Assert.AreEqual (oldOffset - 3, newOffset);
			});
		}

		/// <summary>
		/// Bug 59287 - [VSFeedback Ticket] #490276 - Automatic Space Inserted in Parenthesis (edit)
		/// </summary>
		[Test]
		public async Task TestBug59287()
		{
			var policy = Projects.Policies.PolicyService.InvariantPolicies.Get<CSharpFormattingPolicy>();
			policy = policy.Clone();
			policy.SpaceWithinOtherParentheses = true;

			await Simulate(@"
using System;

class MyContext
{
	public static void Main()
	{
		if (something == f$)
	}
}", (content, ext) => {
				ext.KeyPress(KeyDescriptor.FromGtk((Gdk.Key)'f', 'f', Gdk.ModifierType.None));
				Assert.AreEqual(@"
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
		/// Bug 514890 - [Feedback] #region indentation changes when #if added to C# source file (VS 7.2 build 636).
		/// </summary>
		[Test]
		public async Task TestBug514890 ()
		{
			var text = @"using System;
namespace TestConsole
{
#if
    public class Test
    {
        #region foo
        public Test()
        {
        }
        #endregion
    }
}
";
			await Simulate (text, (content, ext) => { 
				OnTheFlyFormatter.Format (ext.Editor, ext.DocumentContext, 39, 41);
				Assert.AreEqual (text, content.Text);
			});

		} 
	}
}