//
// AutomaticBracketInsertionTests.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2013 Xamarin Inc. (http://xamarin.com)
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

using System.Linq;
using MonoDevelop.CSharpBinding.Tests;
using MonoDevelop.Ide.Gui;
using MonoDevelop.CSharp.Completion;
using MonoDevelop.Ide.CodeCompletion;
using MonoDevelop.Ide.Editor;
using MonoDevelop.Ide.Editor.Extension;
using Microsoft.CodeAnalysis;
using MonoDevelop.Projects;
using MonoDevelop.Core.ProgressMonitoring;
using MonoDevelop.Ide.TypeSystem;
using MonoDevelop.CSharp.Formatting;
using MonoDevelop.Projects.Policies;
using MonoDevelop.Core;
using System.Threading.Tasks;

namespace MonoDevelop.CSharpBinding
{
	[TestFixture]
	public class AutomaticBracketInsertionTests : MonoDevelop.Ide.IdeTestBase
	{
		class TestCompletionWidget : ICompletionWidget 
		{
			MonoDevelop.Ide.Editor.TextEditor editor;

			DocumentContext documentContext;

			public TestCompletionWidget (TextEditor editor, DocumentContext document)
			{
				this.editor = editor;
				documentContext = document;
			}

			public string CompletedWord {
				get;
				set;
			}
			#region ICompletionWidget implementation
			public event EventHandler CompletionContextChanged {
				add { /* TODO */ }
				remove { /* TODO */ }
			}

			public string GetText (int startOffset, int endOffset)
			{
				return editor.GetTextBetween (startOffset, endOffset);
			}

			public char GetChar (int offset)
			{
				return  editor.GetCharAt (offset);
			}

			public CodeCompletionContext CreateCodeCompletionContext (int triggerOffset)
			{
				var line = editor.GetLineByOffset (triggerOffset); 
				return new CodeCompletionContext {
					TriggerOffset = triggerOffset,
					TriggerLine = line.LineNumber,
					TriggerLineOffset = line.Offset,
					TriggerXCoord = 0,
					TriggerYCoord = 0,
					TriggerTextHeight = 0,
					TriggerWordLength = 0
				};
			}

			public CodeCompletionContext CurrentCodeCompletionContext {
				get {
					return CreateCodeCompletionContext (editor.CaretOffset);
				}
			}

			public string GetCompletionText (CodeCompletionContext ctx)
			{
				return "";
			}

			public void SetCompletionText (CodeCompletionContext ctx, string partial_word, string complete_word)
			{
				this.CompletedWord = complete_word;
			}

			public void SetCompletionText (CodeCompletionContext ctx, string partial_word, string complete_word, int offset)
			{
				this.CompletedWord = complete_word;
			}

			public void Replace (int offset, int count, string text)
			{
			}

			public int CaretOffset {
				get {
					return editor.CaretOffset;
				}
				set {
					editor.CaretOffset = value;
				}
			}

			public int TextLength {
				get {
					return editor.Length;
				}
			}

			public int SelectedLength {
				get {
					return 0;
				}
			}

			public Gtk.Style GtkStyle {
				get {
					return null;
				}
			}

			double ICompletionWidget.ZoomLevel {
				get {
					return 1;
				}
			}
			#endregion
		}


		static async Task<Tuple<CSharpCompletionTextEditorExtension,TestViewContent>> Setup (string input)
		{
			TestWorkbenchWindow tww = new TestWorkbenchWindow ();
			TestViewContent content = new TestViewContent ();
			tww.ViewContent = content;
			content.ContentName = "/a.cs";
			content.Data.MimeType = "text/x-csharp";

			var doc = new MonoDevelop.Ide.Gui.Document (tww);

			var text = input;
			int endPos = text.IndexOf ('$');
			if (endPos >= 0)
				text = text.Substring (0, endPos) + text.Substring (endPos + 1);

			content.Text = text;
			content.CursorPosition = System.Math.Max (0, endPos);

			var project = Services.ProjectService.CreateProject ("C#");
			project.Name = "test";
			project.FileName = "test.csproj";
			project.Files.Add (new ProjectFile (content.ContentName, BuildAction.Compile)); 
			project.Policies.Set (PolicyService.InvariantPolicies.Get<CSharpFormattingPolicy> (), CSharpFormatter.MimeType);
			var solution = new MonoDevelop.Projects.Solution ();
			solution.AddConfiguration ("", true); 
			solution.DefaultSolutionFolder.AddItem (project);
			using (var monitor = new ProgressMonitor ())
				await TypeSystemService.Load (solution, monitor);
			content.Owner = project;
			doc.SetProject (project);


			var compExt = new CSharpCompletionTextEditorExtension ();
			compExt.Initialize (doc.Editor, doc);
			content.Contents.Add (compExt);

			await doc.UpdateParseDocument ();
			TypeSystemService.Unload (solution);
			return Tuple.Create (compExt, content);
		}

		async Task<string> Test(string input, string type, string member, Gdk.Key key = Gdk.Key.Return, bool isDelegateExpected = false)
		{
			var s = await Setup (input);
			var ext = s.Item1;
			TestViewContent content = s.Item2;

			var listWindow = new CompletionListWindow ();
			var widget = new TestCompletionWidget (ext.Editor, ext.DocumentContext);
			listWindow.CompletionWidget = widget;
			listWindow.CodeCompletionContext = widget.CurrentCodeCompletionContext;
			var model = ext.DocumentContext.ParsedDocument.GetAst<SemanticModel> ();
			DefaultSourceEditorOptions.Instance.AutoInsertMatchingBracket = true;

			var t = model.Compilation.GetTypeByMetadataName (type);

			var method = member != null ? model.LookupSymbols (s.Item1.Editor.CaretOffset).OfType<IMethodSymbol> ().First (m => m.Name == member) : t.GetMembers ().OfType<IMethodSymbol> ().First (m => m.MethodKind == MethodKind.Constructor);
			var factory = new RoslynCodeCompletionFactory (ext, model);
			var data = new RoslynSymbolCompletionData (null, factory, method);
			data.IsDelegateExpected = isDelegateExpected;
			KeyActions ka = KeyActions.Process;
			data.InsertCompletionText (listWindow, ref ka, KeyDescriptor.FromGtk (key, (char)key, Gdk.ModifierType.None)); 

			return widget.CompletedWord;
		}

		[Test]
		public async Task TestSimpleCase ()
		{
			string completion = await Test (@"class MyClass
{
	void FooBar ()
	{
		$
	}
}", "MyClass", "FooBar");
			Assert.AreEqual ("FooBar();|", completion); 
		}

		[Test]
		public async Task TestBracketAlreadyThere ()
		{
			string completion = await Test (@"class MyClass
{
	void FooBar ()
	{
		$ ();
	}
}", "MyClass", "FooBar");
			Assert.AreEqual ("FooBar", completion); 
		}

		[Test]
		public async Task TestBracketAlreadyThereCase2 ()
		{
			string completion = await Test (@"class MyClass
{
	void FooBar ()
	{
		Test($);
	}
}", "MyClass", "FooBar");
			Assert.AreEqual ("FooBar()|", completion); 
		}

		[Test]
		public async Task TestParameter ()
		{
			string completion = await Test (@"class MyClass
{
	void FooBar ()
	{
		Test(foo, $
	}
}", "MyClass", "FooBar");
			Assert.AreEqual ("FooBar()|", completion); 
		}

		[Test]
		public async Task TestOverloads ()
		{
			string completion = await Test (@"class MyClass
{
	void FooBar (int foo)
	{
	}
	void FooBar ()
	{
		$
	}
}", "MyClass", "FooBar");
			Assert.AreEqual ("FooBar(|);", completion); 
		}

		[Test]
		public async Task TestExpressionCase ()
		{
			string completion = await Test (@"class MyClass
{
	int FooBar ()
	{
		int i;
		i = $
	}
}", "MyClass", "FooBar");
			Assert.AreEqual ("FooBar()|", completion); 
		}

		[Test]
		public async Task TestExpressionCaseWithOverloads ()
		{
			string completion = await Test (@"class MyClass
{
	int FooBar (int foo)
	{
	}
	
	int FooBar ()
	{
		int i;
		i = $
	}
}", "MyClass", "FooBar");
			Assert.AreEqual ("FooBar(|)", completion); 
		}

		[Test]
		public async Task TestDelegateCase ()
		{
			string completion = await Test (@"using System;
class MyClass
{
	int FooBar ()
	{
		Func<int> i;
		i = $
	}
}", "MyClass", "FooBar", Gdk.Key.Return, true);
			Assert.AreEqual ("FooBar", completion); 
		}

		[Test]
		public async Task TestDotCompletion ()
		{
			string completion = await Test (@"class MyClass
{
	void FooBar ()
	{
		$
	}
}", "MyClass", "FooBar", (Gdk.Key)'.');
			Assert.AreEqual ("FooBar()", completion); 
		}


		
		[Test]
		public async Task TestConstructorSimple ()
		{
			string completion = await Test (@"class MyClass
{
	public MyClass () {}

	void FooBar ()
	{
		$
	}
}", "MyClass", null);
			Assert.AreEqual ("MyClass()|", completion); 
		}

		[Test]
		public async Task TestConstructorWithOverloads ()
		{
			string completion = await Test (@"class MyClass
{
	public MyClass () {}
	public MyClass (int x) {}

	void FooBar ()
	{
		$
	}
}", "MyClass", null);
			Assert.AreEqual ("MyClass(|)", completion); 
		}

		[Test]
		public async Task TestGenericCase1 ()
		{
			string completion = await Test (@"class MyClass
{
	void FooBar<T> ()
	{
		$
	}
}", "MyClass", "FooBar");
			Assert.AreEqual ("FooBar<|>();", completion); 
		}

		[Test]
		public async Task TestGenericCase2 ()
		{
			string completion = await Test (@"class MyClass
{
	void FooBar<T> (T t)
	{
		$
	}
}", "MyClass", "FooBar");
			Assert.AreEqual ("FooBar(|);", completion); 
		}

		[Test]
		public async Task TestGenericDotCompletion ()
		{
			string completion = await Test (@"class MyClass
{
	void FooBar<T> ()
	{
		$
	}
}", "MyClass", "FooBar", (Gdk.Key)'.');
			Assert.AreEqual ("FooBar<>()", completion); 
		}

		[Test]
		public async Task TestInsertionBug ()
		{
			string completion = await Test (@"class MyClass
{
	void FooBar ()
	{
		$
		if (true) { }
	}
}", "MyClass", "FooBar");
			Assert.AreEqual ("FooBar();|", completion); 
		}

		
		[Test]
		public async Task TestGenericConstructor ()
		{
			string completion = await Test (@"class MyClass<T>
{
	public MyClass () {}

	void FooBar ()
	{
		$
	}
}", "MyClass`1", null);
			Assert.AreEqual ("MyClass<|>()", completion); 
		}

		[Test]
		public async Task TestBracketAlreadyThereGenericCase ()
		{
			string completion = await Test (@"class MyClass
{
	void FooBar<T> ()
	{
		$<string>();
	}
}", "MyClass", "FooBar");
			Assert.AreEqual ("FooBar", completion); 
		}

		[Test]
		public async Task TestBug41308 ()
		{
			string completion = await Test (@"
class MyClass
{
	void Test<T> (T t)
	{
	}

	void FooBar ()
	{
		$
	}
}", "MyClass", "Test");
			Assert.AreEqual ("Test(|);", completion);
		}

		/// <summary>
		/// Bug 54423 - Code completion automatically closes parenthesis, instead of offering completions
		/// </summary> 
		[Test]
		public async Task TestBug54423 ()
		{
			string completion = await Test (@"
class MyClass
{
	void FooBar ()
	{
		void Test (int foo)
		{
		}
		$
	}
}", "MyClass", "Test");
			Assert.AreEqual ("Test(|);", completion);
		}

	}
}

