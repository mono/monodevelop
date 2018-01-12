//
// NamedArgumentCompletionTests.cs
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
using UnitTests;
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
using MonoDevelop.Core;
using MonoDevelop.Ide;
using System.Threading.Tasks;

namespace MonoDevelop.CSharpBinding
{
	[TestFixture]
	public class NamedArgumentCompletionTests : TestBase
	{
		class TestCompletionWidget : ICompletionWidget
		{
			DocumentContext documentContext;

			TextEditor editor;

			public TestCompletionWidget (MonoDevelop.Ide.Editor.TextEditor editor, DocumentContext documentContext)
			{
				this.editor = editor;
				this.documentContext = documentContext;
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
			var tww = new TestWorkbenchWindow ();
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

			var project = MonoDevelop.Projects.Services.ProjectService.CreateProject ("C#");
			project.Name = "test";
			project.FileName = "test.csproj";
			project.Files.Add (new ProjectFile (content.ContentName, BuildAction.Compile));

			var solution = new MonoDevelop.Projects.Solution ();
			solution.AddConfiguration ("", true);
			solution.DefaultSolutionFolder.AddItem (project);
			using (var monitor = new ProgressMonitor ())
				await TypeSystemService.Load (solution, monitor);
			content.Project = project;
			doc.SetProject (project);

			var compExt = new CSharpCompletionTextEditorExtension ();
			compExt.Initialize (doc.Editor, doc);
			content.Contents.Add (compExt);

			await doc.UpdateParseDocument ();
			TypeSystemService.Unload (solution);
			return Tuple.Create (compExt, content);
		}

		async Task<string> Test(string input, string type, string member, Gdk.Key key = Gdk.Key.Return)
		{
			var s = await Setup (input);
			var ext = s.Item1;
			TestViewContent content = s.Item2;

			var listWindow = new CompletionListWindow ();
			var widget = new TestCompletionWidget (ext.Editor, ext.DocumentContext);
			listWindow.CompletionWidget = widget;
			listWindow.CodeCompletionContext = widget.CurrentCodeCompletionContext;
			var sm = ext.DocumentContext.ParsedDocument.GetAst<SemanticModel> ();

			var t = sm.Compilation.GetTypeByMetadataName (type);
			var foundMember = t.GetMembers().First (m => m.Name == member);
			var data = new CompletionData (foundMember.Name);
			data.DisplayFlags |= DisplayFlags.NamedArgument;
			KeyActions ka = KeyActions.Process;
			data.InsertCompletionText (listWindow, ref ka, KeyDescriptor.FromGtk (key, (char)key, Gdk.ModifierType.None));

			return widget.CompletedWord;
		}

		[Ignore ("Changed in roslyn completion.")]
		[Test]
		public async Task TestSimpleCase ()
		{
			DefaultSourceEditorOptions.Instance.AutoInsertMatchingBracket = true;
			string completion = await Test (@"class MyClass
{
	int foo;
	void MyMethod ()
	{
		$
	}
}", "MyClass", "foo");
			Assert.AreEqual ("foo = ", completion);
		}


		[Test]
		public async Task TestNoAutoCase ()
		{
			DefaultSourceEditorOptions.Instance.AutoInsertMatchingBracket = false;
			string completion = await Test (@"class MyClass
{
	int foo;
	void MyMethod ()
	{
		$
	}
}", "MyClass", "foo", Gdk.Key.space);
			Assert.AreEqual ("foo", completion);
		}


		// Bug 60365 - Escaped keywords autocomplete to the unescaped keyword https://bugzilla.xamarin.com/show_bug.cgi?id=60365
		[Ignore("Crashes with an accessibility exception")]
		[Test]
		public async Task TestBug60365 ()
		{
			var tww = new TestWorkbenchWindow ();
			TestViewContent content = new TestViewContent ();
			tww.ViewContent = content;
			content.ContentName = "/a.cs";
			content.Data.MimeType = "text/x-csharp";

			var doc = new MonoDevelop.Ide.Gui.Document (tww);

			var text = "@c$";
			int endPos = text.IndexOf ('$');
			if (endPos >= 0)
				text = text.Substring (0, endPos) + text.Substring (endPos + 1);

			content.Text = text;
			content.CursorPosition = System.Math.Max (0, endPos);

			var project = MonoDevelop.Projects.Services.ProjectService.CreateProject ("C#");
			project.Name = "test";
			project.FileName = "test.csproj";
			project.Files.Add (new ProjectFile (content.ContentName, BuildAction.Compile));

			var solution = new MonoDevelop.Projects.Solution ();
			solution.AddConfiguration ("", true);
			solution.DefaultSolutionFolder.AddItem (project);
			using (var monitor = new ProgressMonitor ())
				await TypeSystemService.Load (solution, monitor);
			content.Project = project;
			doc.SetProject (project);

			var ext = new CSharpCompletionTextEditorExtension ();
			ext.Initialize (doc.Editor, doc);
			var listWindow = new CompletionListWindow ();
			var widget = new TestCompletionWidget (ext.Editor, ext.DocumentContext);
			listWindow.CompletionWidget = widget;
			listWindow.CodeCompletionContext = widget.CurrentCodeCompletionContext;

			var list = await  ext.HandleCodeCompletionAsync (widget.CurrentCodeCompletionContext, new CompletionTriggerInfo (CompletionTriggerReason.CharTyped, 'c'));
			var ka = KeyActions.Complete;
			list.First (d => d.CompletionText  == "class").InsertCompletionText (listWindow, ref ka, KeyDescriptor.Tab);

			Assert.AreEqual ("@class", content.Text);

			content.Contents.Add (ext);

			await doc.UpdateParseDocument ();
			TypeSystemService.Unload (solution);
		}

	}
}
