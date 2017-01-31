//
// TextEditorProjectionTests.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2015 Xamarin Inc. (http://xamarin.com)
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
using System.Collections.Generic;
using MonoDevelop.Ide.Editor.Projection;
using MonoDevelop.Core.Text;
using MonoDevelop.Ide.Gui;
using MonoDevelop.CSharpBinding;
using UnitTests;
using MonoDevelop.CSharpBinding.Tests;
using System.Linq;
using MonoDevelop.Ide.Editor.Highlighting;
using MonoDevelop.Ide.Editor.Extension;
using MonoDevelop.Ide.CodeCompletion;
using System.Threading.Tasks;
using MonoDevelop.Components.Commands;
using MonoDevelop.Ide.Commands;
using ICSharpCode.NRefactory.CSharp;
using Gtk;
using System.Collections.Immutable;

namespace MonoDevelop.Ide.Editor
{
	[TestFixture]
	public class TextEditorProjectionTests : TestBase
	{
		[Test]
		public void TestProjectionUpdate ()
		{
			var editor = TextEditorFactory.CreateNewEditor ();
			editor.Text = "1234567890";

			var projectedDocument = TextEditorFactory.CreateNewDocument (
				new StringTextSource ("__12__34__56__78__90"),
				"a"
			);

			var segments = new List<ProjectedSegment> ();
			for (int i = 0; i < 5; i++) {
				segments.Add (new ProjectedSegment (i * 2, 2 + i * 4, 2));
			}
			var projection = new Projection.Projection (projectedDocument, segments);
			var tww = new TestWorkbenchWindow ();
			var content = new TestViewContent ();
			tww.ViewContent = content;

			var originalContext = new Document (tww);
			var projectedEditor = projection.CreateProjectedEditor (originalContext);
			editor.SetOrUpdateProjections (originalContext, new [] { projection }, TypeSystem.DisabledProjectionFeatures.All);
			editor.InsertText (1, "foo");
			Assert.AreEqual ("__1foo2__34__56__78__90", projectedEditor.Text);

			Assert.AreEqual (2, projection.ProjectedSegments.ElementAt (0).ProjectedOffset);
			Assert.AreEqual (2 + "foo".Length, projection.ProjectedSegments.ElementAt (0).Length);
			for (int i = 1; i < 5; i++) {
				Assert.AreEqual (2 + i * 4 + "foo".Length, projection.ProjectedSegments.ElementAt (i).ProjectedOffset);
				Assert.AreEqual (2, projection.ProjectedSegments.ElementAt (i).Length);
			}
		}

		[Test]
		public void TestProjectionHighlighting ()
		{
			var editor = TextEditorFactory.CreateNewEditor ();
			var options = new CustomEditorOptions (editor.Options);
			options.EditorTheme = "Tango";
			editor.Options = options;
			editor.Text = "1234567890";

			var projectedDocument = TextEditorFactory.CreateNewDocument (
				new StringTextSource ("__12__34__56__78__90"),
				"a"
			);

			var segments = new List<ProjectedSegment> ();
			for (int i = 0; i < 5; i++) {
				segments.Add (new ProjectedSegment (i * 2, 2 + i * 4, 2));
			}
			var projection = new Projection.Projection (projectedDocument, segments);
			var tww = new TestWorkbenchWindow ();
			var content = new TestViewContent ();
			tww.ViewContent = content;

			var originalContext = new Document (tww);
			var projectedEditor = projection.CreateProjectedEditor (originalContext);
			projectedEditor.SemanticHighlighting = new TestSemanticHighlighting (projectedEditor, originalContext);
			editor.SetOrUpdateProjections (originalContext, new [] { projection }, TypeSystem.DisabledProjectionFeatures.None);

			var markup = editor.GetMarkup (0, editor.Length, new MarkupOptions (MarkupFormat.Pango));
			var color = "#3363a4";
			Assert.AreEqual ("<span foreground=\"" + color + "\">1</span><span foreground=\"#222222\">234</span><span foreground=\"" + color + "\">5</span><span foreground=\"#222222\">678</span><span foreground=\"" + color + "\">9</span><span foreground=\"#222222\">0</span>", markup);
		}

		class TestSemanticHighlighting : SemanticHighlighting
		{
			public TestSemanticHighlighting (MonoDevelop.Ide.Editor.TextEditor editor, MonoDevelop.Ide.Editor.DocumentContext documentContext) : base (editor, documentContext)
			{
			}

			public override IEnumerable<ColoredSegment> GetColoredSegments (ISegment segment)
			{
				for (int i = 0; i < segment.Length; i++) {
					char ch = base.editor.GetCharAt (segment.Offset + i);
					if (ch == '1' || ch == '5' || ch == '9')
						yield return new ColoredSegment (segment.Offset + i, 1, new ScopeStack ("keyword"));
				}
			}

			protected override void DocumentParsed ()
			{
			}
		}

		[Test]
		public void TestProjectionCompletion ()
		{
			var editor = TextEditorFactory.CreateNewEditor ();
			var options = new CustomEditorOptions (editor.Options);
			options.EditorTheme = "Tango";
			editor.Options = options;
			editor.Text = "12345678901234567890";

			var projectedDocument = TextEditorFactory.CreateNewDocument (
				new StringTextSource ("__12__34__56__78__90"),
				"a"
			);

			var segments = new List<ProjectedSegment> ();
			for (int i = 0; i < 5; i++) {
				segments.Add (new ProjectedSegment (i * 2, 2 + i * 4, 2));
			}
			var projection = new Projection.Projection (projectedDocument, segments);
			var tww = new TestWorkbenchWindow ();
			var content = new TestViewContent ();
			tww.ViewContent = content;

			var originalContext = new Document (tww);
			var projectedEditor = projection.CreateProjectedEditor (originalContext);
			TestCompletionExtension orignalExtension;
			editor.SetExtensionChain (originalContext, new [] { orignalExtension = new TestCompletionExtension (editor) { CompletionWidget = new EmptyCompletionWidget (editor)  } });
			TestCompletionExtension projectedExtension;
			projectedEditor.SetExtensionChain (originalContext, new [] { projectedExtension = new TestCompletionExtension (editor) { CompletionWidget = new EmptyCompletionWidget (projectedEditor) } });

			editor.SetOrUpdateProjections (originalContext, new [] { projection }, TypeSystem.DisabledProjectionFeatures.None);
			editor.CaretOffset = 1;

			var service = new CommandManager ();
			service.LoadCommands ("/MonoDevelop/Ide/Commands");
			service.DispatchCommand (TextEditorCommands.ShowCompletionWindow, null, editor.CommandRouter);
			Assert.IsFalse (orignalExtension.CompletionRun);
			Assert.IsTrue (projectedExtension.CompletionRun);

			editor.CaretOffset = 15;
			CompletionWindowManager.HideWindow ();
			service.DispatchCommand (TextEditorCommands.ShowCompletionWindow, null, editor.CommandRouter);
			Assert.IsTrue (orignalExtension.CompletionRun);
		}

		class TestCompletionExtension : CompletionTextEditorExtension
		{
			internal bool CompletionRun;

			public TestCompletionExtension (TextEditor editor)
			{
				Editor = editor;
			}

			public override Task<ICompletionDataList> HandleCodeCompletionAsync (CodeCompletionContext completionContext, CompletionTriggerInfo triggerInfo, System.Threading.CancellationToken token)
			{
				CompletionRun = true;
				var list = new CompletionDataList ();
				list.Add ("foo");
				return Task.FromResult<CodeCompletion.ICompletionDataList> (list);
			}

			public override bool CanRunCompletionCommand ()
			{
				return true;
			}
		}

		class EmptyCompletionWidget : ICompletionWidget
		{
			TextEditor editor;

			public EmptyCompletionWidget (TextEditor editor)
			{
				this.editor = editor;
			}

			int ICompletionWidget.CaretOffset
			{
				get
				{
					return 0;
				}

				set
				{
				}
			}

			CodeCompletionContext ICompletionWidget.CurrentCodeCompletionContext
			{
				get
				{
					return null;
				}
			}

			Style ICompletionWidget.GtkStyle
			{
				get
				{
					return null;
				}
			}

			int ICompletionWidget.SelectedLength
			{
				get
				{
					return 1;
				}
			}

			int ICompletionWidget.TextLength
			{
				get
				{
					return 1;
				}
			}

			double ICompletionWidget.ZoomLevel
			{
				get
				{
					return 1;
				}
			}

			event EventHandler ICompletionWidget.CompletionContextChanged
			{
				add
				{
				}

				remove
				{
				}
			}

			CodeCompletionContext ICompletionWidget.CreateCodeCompletionContext (int triggerOffset)
			{
				return new CodeCompletionContext () { TriggerOffset = editor.CaretOffset };
			}

			char ICompletionWidget.GetChar (int offset)
			{
				return 'a';
			}

			string ICompletionWidget.GetCompletionText (CodeCompletionContext ctx)
			{
				return "";
			}

			string ICompletionWidget.GetText (int startOffset, int endOffset)
			{
				return "";
			}

			void ICompletionWidget.Replace (int offset, int count, string text)
			{
			}

			void ICompletionWidget.SetCompletionText (CodeCompletionContext ctx, string partial_word, string complete_word)
			{
			}

			void ICompletionWidget.SetCompletionText (CodeCompletionContext ctx, string partial_word, string complete_word, int completeWordOffset)
			{
			}
		}
	}
}

