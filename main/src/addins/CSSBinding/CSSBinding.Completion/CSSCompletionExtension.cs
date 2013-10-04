
// CSSCompletionExtension.cs
//
// Author:
//       Diyoda Sajjana <>
//
// Copyright (c) 2013 Diyoda Sajjana
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
using MonoDevelop.Ide.Gui.Content;
using MonoDevelop.Ide.CodeCompletion;
using MonoDevelop.Core;
using Mono.TextEditor;


namespace CSSBinding.Completion
{
	public class CSSCompletionExtension : CompletionTextEditorExtension
	{
		public CSSCompletionExtension ()
		{

		}

		internal Mono.TextEditor.TextEditorData TextEditorData {
			get {
				var doc = Document;
				if (doc == null)
					return null;
				return doc.Editor;
			}
		}


		public new MonoDevelop.Ide.Gui.Document Document {
			get {
				return base.document;
			}
		}


		public MonoDevelop.Projects.Project Project {
			get {
				return document.Project;
			}
		}


		public override ICompletionDataList HandleCodeCompletion (CodeCompletionContext completionContext, char completionChar, ref int triggerWordLength)
		{
			if (!EnableCodeCompletion)
				return null;
			if (!EnableAutoCodeCompletion && char.IsLetter (completionChar))
				return null;

			//	var timer = Counters.ResolveTime.BeginTiming ();
			try {
				if (char.IsLetterOrDigit (completionChar) || completionChar == '_') {
					if (completionContext.TriggerOffset > 1 && char.IsLetterOrDigit (document.Editor.GetCharAt (completionContext.TriggerOffset - 2)))
						return null;
					triggerWordLength = 1;
				}
				return InternalHandleCodeCompletion (completionContext, completionChar, false, ref triggerWordLength);
			} catch (Exception e) {
				LoggingService.LogError ("Unexpected code completion exception." + Environment.NewLine + 
				                         "FileName: " + Document.FileName + Environment.NewLine + 
				                         "Position: line=" + completionContext.TriggerLine + " col=" + completionContext.TriggerLineOffset + Environment.NewLine + 
				                         "Line text: " + Document.Editor.GetLineText (completionContext.TriggerLine), 
				                         e);
				return null;
			} finally {
//							if (timer != null)
//								timer.Dispose ();
			}
		}

		ICompletionDataList InternalHandleCodeCompletion (CodeCompletionContext completionContext, char completionChar, bool ctrlSpace, ref int triggerWordLength)
		{
//			var data = TextEditorData;
//			if (data.CurrentMode is TextLinkEditMode) {
//				if (((TextLinkEditMode)data.CurrentMode).TextLinkMode == TextLinkMode.EditIdentifier)
//					return null;
//			}
//			if (Unit == null || CSharpUnresolvedFile == null)
//				return null;
//			if(typeSystemSegmentTree == null)
//				return null;
//
//			var list = new CSharpCompletionDataList ();
//			list.Resolver = CSharpUnresolvedFile != null ? CSharpUnresolvedFile.GetResolver (UnresolvedFileCompilation, Document.Editor.Caret.Location) : new CSharpResolver (Compilation);
//			var ctx = CSharpUnresolvedFile.GetTypeResolveContext (UnresolvedFileCompilation, data.Caret.Location) as CSharpTypeResolveContext;
//
//			var engine = new CSharpCompletionEngine (
//				data.Document,
//				typeSystemSegmentTree,
//				new CompletionDataFactory (this, new CSharpResolver (ctx)),
//				Document.GetProjectContext (),
//				ctx
//				);
//
//			if (Document.HasProject) {
//				var configuration = Document.Project.GetConfiguration (MonoDevelop.Ide.IdeApp.Workspace.ActiveConfiguration) as DotNetProjectConfiguration;
//				var par = configuration != null ? configuration.CompilationParameters as CSharpCompilerParameters : null;
//				if (par != null)
//					engine.LanguageVersion = MonoDevelop.CSharp.Parser.TypeSystemParser.ConvertLanguageVersion (par.LangVersion);
//			}
//
//			engine.FormattingPolicy = FormattingPolicy.CreateOptions ();
//			engine.EolMarker = data.EolMarker;
//			engine.IndentString = data.Options.IndentationString;
//			try {
//				foreach (var cd in engine.GetCompletionData (completionContext.TriggerOffset, ctrlSpace)) {
//					list.Add (cd);
//					if (cd is IListData)
//						((IListData)cd).List = list;
//				}
//			} catch (Exception e) {
//				LoggingService.LogError ("Error while getting completion data.", e);
//			}
//			list.AutoCompleteEmptyMatch = engine.AutoCompleteEmptyMatch;
//			list.AutoCompleteEmptyMatchOnCurlyBrace = engine.AutoCompleteEmptyMatchOnCurlyBracket;
//			list.AutoSelect = engine.AutoSelect;
//			list.DefaultCompletionString = engine.DefaultCompletionString;
//			list.CloseOnSquareBrackets = engine.CloseOnSquareBrackets;
//			if (ctrlSpace)
//				list.AutoCompleteUniqueMatch = true;
//			return list.Count > 0 ? list : null;
			return null;
		}

		public override ICompletionDataList CodeCompletionCommand (CodeCompletionContext completionContext)
		{
			int triggerWordLength = 0;
			char ch = completionContext.TriggerOffset > 0 ? TextEditorData.GetCharAt (completionContext.TriggerOffset - 1) : '\0';
			return InternalHandleCodeCompletion (completionContext, ch, true, ref triggerWordLength);
		}

	}

//	class CSSCompletionDataList : CompletionDataList
//	{
//		public CSharpResolver Resolver {
//			get;
//			set;
//		}
//	}
}

