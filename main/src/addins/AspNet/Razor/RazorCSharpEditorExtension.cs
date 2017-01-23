//
// RazorCSharpEditorExtension.cs
//
// Author:
//		Piotr Dowgiallo <sparekd@gmail.com>
//
// Copyright (c) 2012 Piotr Dowgiallo
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
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Razor.Generator;
using System.Web.Razor.Parser.SyntaxTree;
using Mono.TextEditor;
using MonoDevelop.Core;
using MonoDevelop.Core.Text;
using MonoDevelop.Ide.Editor;
using MonoDevelop.Ide.CodeCompletion;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.TypeSystem;
using MonoDevelop.Xml.Dom;
using MonoDevelop.Xml.Parser;
using MonoDevelop.AspNet.Html;
using MonoDevelop.AspNet.Razor.Dom;
using MonoDevelop.AspNet.Razor.Parser;
using MonoDevelop.Ide.Editor.Extension;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using MonoDevelop.Ide.Editor.Projection;

namespace MonoDevelop.AspNet.Razor
{
	public class RazorCSharpEditorExtension : BaseHtmlEditorExtension
	{
		protected RazorCSharpParsedDocument razorDocument;
		internal UnderlyingDocumentInfo hiddenInfo;
		IRazorCompletionBuilder completionBuilder;

		bool isInCSharpContext;
		static readonly Regex DocTypeRegex = new Regex (@"(?:PUBLIC|public)\s+""(?<fpi>[^""]*)""\s+""(?<uri>[^""]*)""");

		ICompletionWidget defaultCompletionWidget;
		MonoDevelop.Ide.Editor.TextEditor defaultEditor;
		DocumentContext defaultDocumentContext;

		// RazorSyntaxMode syntaxMode;

		UnderlyingDocument HiddenDoc {
			get { return hiddenInfo.UnderlyingDocument; }
		}

		RazorPageInfo PageInfo {
			get { return razorDocument.PageInfo; }
		}

		protected override XmlRootState CreateRootState ()
		{
			return new RazorRootState ();
		}

		public override string CompletionLanguage {
			get {
				return "Razor";
			}
		}

		public RazorCSharpEditorExtension ()
		{
		}

		/// <summary>
		/// Used by unit tests.
		/// </summary>
		internal RazorCSharpEditorExtension (MonoDevelop.Ide.Gui.Document doc, RazorCSharpParsedDocument parsedDoc, bool cSharpContext)
		{
			razorDocument = parsedDoc;
			Initialize (doc.Editor, doc);
			if (cSharpContext) {
				InitializeCodeCompletion ();
				SwitchToHidden ();
			}
		}

		protected override void Initialize ()
		{
			base.Initialize ();

			defaultCompletionWidget = CompletionWidget;
			defaultDocumentContext = DocumentContext;
			defaultEditor = Editor;
			completionBuilder = RazorCompletionBuilderService.GetBuilder ("C#");

			// defaultEditor.TextChanging += UnderlyingDocument_TextReplacing;
			//syntaxMode = new RazorSyntaxMode (Editor, DocumentContext);
			//var textEditorData = DocumentContext.GetContent<TextEditorData> ();
			//if (textEditorData != null)
			//	textEditorData.Document.SyntaxMode = syntaxMode;
		}

		public override void Dispose ()
		{
			//if (syntaxMode != null) {
			//	var textEditorData = DocumentContext.GetContent<TextEditorData> ();
			//	if (textEditorData != null)
			//		textEditorData.Document.SyntaxMode = null;
			//	syntaxMode.Dispose ();
			//	syntaxMode = null;
			//}
			// defaultEditor.TextChanging -= UnderlyingDocument_TextReplacing;
			base.Dispose ();
		}

		// Handles text modifications in hidden document
		void UnderlyingDocument_TextReplacing (object sender, MonoDevelop.Core.Text.TextChangeEventArgs e)
		{
			if (razorDocument == null)
				return;

			EnsureUnderlyingDocumentSet ();
			int off = CalculateCaretPosition (e.Offset);

			if (e.RemovalLength > 0) {
				int removalLength = e.RemovalLength;
				if (off + removalLength > HiddenDoc.Editor.Length)
					removalLength = HiddenDoc.Editor.Length - off;
				HiddenDoc.Editor.RemoveText (off, removalLength);
			}
			if (e.InsertionLength > 0) {
				if (isInCSharpContext) {
					HiddenDoc.Editor.InsertText (off, e.InsertedText.Text);
					HiddenDoc.HiddenAnalysisDocument = HiddenDoc.HiddenAnalysisDocument.WithText (Microsoft.CodeAnalysis.Text.SourceText.From (HiddenDoc.Editor.Text));
				} else // Insert spaces to correctly calculate offsets until next reparse
					HiddenDoc.Editor.InsertText (off, new String (' ', e.InsertionLength));
			}
			if (codeFragment != null)
				codeFragment.EndOffset += (e.InsertionLength - e.RemovalLength);
		}

		protected override void OnParsedDocumentUpdated ()
		{
			base.OnParsedDocumentUpdated ();
			try {
				razorDocument = CU as RazorCSharpParsedDocument;
				if (razorDocument == null || razorDocument.PageInfo.CSharpSyntaxTree == null)
					return;

				CreateDocType ();

				// Don't update C# code in hiddenInfo when:
				// 1) We are in a RazorState, and the completion window is visible,
				// it'll freeze (or disappear if we call OnCompletionContextChanged).
				// 2) We're in the middle of writing a Razor expression - if we're in an incorrect state,
				// the generated code migh be behind what we've been already written.

				var state = Tracker.Engine.CurrentState;
				if (state is RazorState && CompletionWindowManager.IsVisible || 
				    (!updateNeeded && (state is RazorSpeculativeState || state is RazorExpressionState)))
					UpdateHiddenDocument (false);
				else {
					UpdateHiddenDocument ();
					updateNeeded = false;
				}
			} catch (Exception e) {
				LoggingService.LogError ("Error while updating razor completion.", e); 
			}
		}

		void CreateDocType ()
		{
			DocType = new XDocType (MonoDevelop.Ide.Editor.DocumentLocation.Empty);
			var matches = DocTypeRegex.Match (razorDocument.PageInfo.DocType);
			if (matches.Success) {
				DocType.PublicFpi = matches.Groups ["fpi"].Value;
				DocType.Uri = matches.Groups ["uri"].Value;
			}
		}

		void EnsureUnderlyingDocumentSet ()
		{
			if (hiddenInfo == null)
				UpdateHiddenDocument ();
		}

		void UpdateHiddenDocument (bool updateSourceCode = true)
		{
			//if (!updateSourceCode && hiddenInfo != null) {
			//	hiddenInfo.UnderlyingDocument.HiddenParsedDocument = razorDocument.PageInfo.ParsedDocument;
			//	return;
			//} else if (updateSourceCode && hiddenInfo != null) {
			//	hiddenInfo.UnderlyingDocument.Editor.Text = razorDocument.PageInfo.CSharpCode;
			//	hiddenInfo.UnderlyingDocument.HiddenParsedDocument = razorDocument.PageInfo.ParsedDocument;
			//	hiddenInfo.UnderlyingDocument.HiddenAnalysisDocument = razorDocument.PageInfo.AnalysisDocument;
			//	
			//	codeFragment = null;
			//	return;
			//}

			//hiddenInfo = new UnderlyingDocumentInfo ();

			//var viewContent = new HiddenTextEditorViewContent ();
			//viewContent.Project = DocumentContext.Project;
			//viewContent.ContentName = "Generated.cs"; // Use a name with .cs extension to get csharp ambience
			//viewContent.Text = razorDocument.PageInfo.CSharpCode;

			//var workbenchWindow = new HiddenWorkbenchWindow ();
			//workbenchWindow.ViewContent = viewContent;
			//hiddenInfo.UnderlyingDocument = new UnderlyingDocument (workbenchWindow) {
			//	HiddenParsedDocument = razorDocument.PageInfo.ParsedDocument,
			//	HiddenAnalysisDocument = razorDocument.PageInfo.AnalysisDocument
			//};

			//// completion window needs this
			//Gtk.Widget editor = hiddenInfo.UnderlyingDocument.Editor;
			//editor.Parent = ((Gtk.Widget)Editor).Parent;

			//currentMappings = razorDocument.PageInfo.GeneratorResults.DesignTimeLineMappings;
			//codeFragment = null;
		}

		#region Code completion

		XObject prevNode;
		bool updateNeeded;

		public override bool KeyPress (KeyDescriptor descriptor)
		{
			Tracker.UpdateEngine ();
			if (razorDocument == null)
				return NonCSharpCompletion (descriptor);

			var n = Tracker.Engine.Nodes.Peek ();
			if (prevNode is RazorExpression && !(n is RazorExpression))
				updateNeeded = true;
			prevNode = n;
			var state = Tracker.Engine.CurrentState;
			int off = Editor.CaretOffset;

			char previousChar = off > 0 ? Editor.GetCharAt (off - 1) : ' ';
			char beforePrevious = off > 1 ? Editor.GetCharAt (off - 2) : ' ';

			// Determine completion context here, before calling base method to set the context correctly

			// Rule out Razor comments, html, transition sign (@) and e-mail addresses
			if (state is RazorCommentState || (previousChar != '@' && !(state is RazorState))  || descriptor.KeyChar == '@'
				|| (previousChar == '@' && Char.IsLetterOrDigit (beforePrevious)))
				return NonCSharpCompletion (descriptor);

			// Determine if we are inside generics
			if (previousChar == '<') {
				var codeState = state as RazorCodeFragmentState;
				if (codeState == null || !codeState.IsInsideGenerics)
					return NonCSharpCompletion (descriptor);
			}
			// Determine whether we begin an html tag or generics
			else if (descriptor.KeyChar == '<' && (n is XElement || !Char.IsLetterOrDigit (previousChar)))
				return NonCSharpCompletion (descriptor);
			// Determine whether we are inside html text or in code
			else if (previousChar != '@' && n is XElement && !(state is RazorSpeculativeState) && !(state is RazorExpressionState))
				return NonCSharpCompletion (descriptor);

			return base.KeyPress (descriptor);
			// We're in C# context
			//InitializeCodeCompletion ();
			//SwitchToHidden ();

			//bool result;
			//try {
			//	result = base.KeyPress (descriptor);
			//	if (/*EnableParameterInsight &&*/ (descriptor.KeyChar == ',' || descriptor.KeyChar == ')') && CanRunParameterCompletionCommand ())
			//	    base.RunParameterCompletionCommand ();
			//} finally {
			//	SwitchToReal ();
			//}

			//return result;
		}

		protected void SwitchToHidden ()
		{
			isInCSharpContext = true;
			DocumentContext = HiddenDoc;
			Editor = HiddenDoc.Editor;
			CompletionWidget = completionBuilder.CreateCompletionWidget (defaultEditor, defaultDocumentContext, hiddenInfo);
		}

		protected void SwitchToReal ()
		{
			isInCSharpContext = false;
			DocumentContext = defaultDocumentContext;
			Editor = defaultEditor;
			CompletionWidget = defaultCompletionWidget;
		}

		bool NonCSharpCompletion (KeyDescriptor descriptor)
		{
			isInCSharpContext = false;
			return base.KeyPress (descriptor);
		}

		protected void InitializeCodeCompletion ()
		{
			EnsureUnderlyingDocumentSet ();
			hiddenInfo.OriginalCaretPosition = defaultEditor.CaretOffset;
			hiddenInfo.CaretPosition = CalculateCaretPosition ();
			HiddenDoc.Editor.CaretOffset = hiddenInfo.CaretPosition;
		}

		class CodeFragment
		{
			public int StartOffset { get; set; }
			public int StartRealOffset { get; set; }
			public int EndOffset { get; set; }

			public CodeFragment ()
			{}

			public CodeFragment (int startOff, int startRealOff, int endOffset)
			{
				StartOffset = startOff;
				StartRealOffset = startRealOff;
				EndOffset = endOffset;
			}
		}

		int GetDefaultPosition ()
		{
			var root = razorDocument.PageInfo.CSharpSyntaxTree?.GetRoot ();
			if (root == null)
				return -1;

			var type = root.DescendantNodes ().OfType<TypeDeclarationSyntax> ().FirstOrDefault ();
			if (type == null) {
				return -1;
			}
			var method = type.DescendantNodes ()
				.OfType <MethodDeclarationSyntax> ()
				.FirstOrDefault (m => m.Identifier.ValueText == "Execute");
			if (method == null) {
				return -1;
			}
			var location = method.Body.GetLocation ();
			return location.SourceSpan.Start + 1;
		}

		IDictionary<int, GeneratedCodeMapping> currentMappings;
		CodeFragment codeFragment;

		int CalculateCaretPosition ()
		{
			return CalculateCaretPosition (defaultEditor.CaretOffset);
		}

		int CalculateCaretPosition (int currentOffset)
		{
			if (codeFragment != null) {
				int diff = currentOffset - codeFragment.StartRealOffset;
				int off = codeFragment.StartOffset + diff;
				if (diff >= 0 && off <= codeFragment.EndOffset)
					return off;
			}

			KeyValuePair<int, GeneratedCodeMapping> map;

			var defaultPosition = GetDefaultPosition ();
			if (defaultPosition < 0) {
				defaultPosition = 0;
			}

			// If it's first line of code, create a default temp mapping, and use it until next reparse
			if (currentMappings.Count == 0) {
				string newLine = "\r\n#line 0 \r\n ";
				HiddenDoc.Editor.InsertText (defaultPosition, newLine);
				map = new KeyValuePair<int, GeneratedCodeMapping> (0, new GeneratedCodeMapping (currentOffset - 1, 0, 0, 0, 0));
				currentMappings.Add (map);
			} else {
				var result = currentMappings.Where (m => m.Value.StartOffset <= currentOffset);
				if (!result.Any ())
					return defaultPosition;
				map = result.Last ();
			}

			string pattern = "#line " + map.Key + " ";
			int pos = HiddenDoc.Editor.Text.IndexOf (pattern, 0, HiddenDoc.Editor.Length, StringComparison.Ordinal);
			if (pos == -1 || !map.Value.StartOffset.HasValue)
				return defaultPosition;

			int startRealOff = map.Value.StartOffset.Value;
			int offDifference = currentOffset - (startRealOff + map.Value.CodeLength);
			var line = HiddenDoc.Editor.GetLineByOffset (pos);
			int endHiddenOff = line.NextLine.Offset + map.Value.StartGeneratedColumn + map.Value.CodeLength;

			int hiddenOff;

			// If off is inside the map
			if (offDifference <= 0) {
				int delta = currentOffset - startRealOff;
				hiddenOff = line.NextLine.Offset + map.Value.StartGeneratedColumn + delta - 1;
				codeFragment = new CodeFragment (hiddenOff, currentOffset, endHiddenOff);
			} else {
				// It's a new code fragment - create a temp mapping, and use it until next reparse
				int key = currentMappings.Last ().Key + 1;
				string newLine = "\r\n#line " + key + " \r\n ";
				int newOff = endHiddenOff;

				if (HiddenDoc.Editor.GetCharAt (newOff) == '\n')
					newOff++;

				// We start a new mapping right after the preceding one, but need to include the difference
				// between mapping's start and the current offset
				HiddenDoc.Editor.InsertText (newOff, newLine);
				HiddenDoc.Editor.InsertText (newOff + newLine.Length, new String (' ', offDifference) + " \r\n");

				var newMap = new KeyValuePair<int, GeneratedCodeMapping> (key, new GeneratedCodeMapping (
					startRealOff + map.Value.CodeLength, 0, 0, 0, offDifference));
				currentMappings.Add (newMap);
				hiddenOff = newOff + newLine.Length + offDifference;
				codeFragment = new CodeFragment (newOff + newLine.Length, newMap.Value.StartOffset.Value,
					newOff + newLine.Length + offDifference);
			}

			return hiddenOff;
		}

		public override async System.Threading.Tasks.Task<ICompletionDataList> HandleCodeCompletionAsync (CodeCompletionContext completionContext, CompletionTriggerInfo triggerInfo, System.Threading.CancellationToken token)
		{
			if (triggerInfo.CompletionTriggerReason == CompletionTriggerReason.CompletionCommand) {
				if (hiddenInfo != null && (isInCSharpContext || Tracker.Engine.CurrentState is RazorState)
					&& !(Tracker.Engine.Nodes.Peek () is XElement)) {
					InitializeCodeCompletion ();
					return await completionBuilder.HandlePopupCompletion (defaultEditor, defaultDocumentContext, hiddenInfo);
				}
			}
			char previousChar = defaultEditor.CaretOffset > 1 ? defaultEditor.GetCharAt (
				defaultEditor.CaretOffset - 2) : ' ';
			if (triggerInfo.CompletionTriggerReason != CompletionTriggerReason.CharTyped)
				return null;
			// Don't show completion window when directive's name is being typed
			var directive = Tracker.Engine.Nodes.Peek () as RazorDirective;
			if (directive != null && !directive.FirstBracket.HasValue)
				return null;
			var completionChar = triggerInfo.TriggerCharacter.Value;
			if (hiddenInfo != null && isInCSharpContext) {
				var list = (CompletionDataList) await completionBuilder.HandleCompletion (defaultEditor, defaultDocumentContext, completionContext,
					hiddenInfo, completionChar, token);

				if (list != null) {
					//filter out the C# templates, many of them are not valid
					int oldCount = list.Count;
					list = FilterCSharpTemplates (list);
					int templates = list.Count - oldCount;

					if (previousChar == '@') {
						RazorCompletion.AddAllRazorSymbols (list, razorDocument.PageInfo.HostKind);
					}
					if (templates > 0) {
						AddFilteredRazorTemplates (list, previousChar == '@', true);
					}
				}
				return list;
			}

			return await base.HandleCodeCompletionAsync (completionContext, triggerInfo, token);
		}

		//recreating the list is over 2x as fast as using remove operations, saves typically 10ms
		static CompletionDataList FilterCSharpTemplates (CompletionDataList list)
		{
			var newList = new CompletionDataList () {
				AutoCompleteEmptyMatch = list.AutoCompleteEmptyMatch,
				AutoCompleteUniqueMatch = list.AutoCompleteUniqueMatch,
				AutoSelect = list.AutoSelect,
				CloseOnSquareBrackets = list.CloseOnSquareBrackets,
				CompletionSelectionMode = list.CompletionSelectionMode,
				DefaultCompletionString = list.DefaultCompletionString,
				IsSorted = list.IsSorted,
				TriggerWordLength = list.TriggerWordLength,
				TriggerWordStart = list.TriggerWordStart,
			};
			foreach (var l in list) {
				var c =  l as CompletionData;
				if (c == null || (c.Icon.Name != "md-template" && c.Icon.Name != "md-template-surroundwith"))
					newList.Add (c);
			}
			return newList;
		}

		static void AddFilteredRazorTemplates (CompletionDataList list, bool atTemplates, bool stripLeadingAt)
		{
			//add the razor templates then filter them based on whether we follow an @ char, so we don't have
			//lots of duplicates
			int count = list.Count;
			MonoDevelop.Ide.CodeTemplates.CodeTemplateService.AddCompletionDataForMime ("text/x-cshtml", list);
			for (int i = count; i < list.Count; i++) {
				var d = (CompletionData) list[i];
				if (atTemplates) {
					if (d.CompletionText[0] != '@') {
						list.RemoveAt (i);
					} else if (stripLeadingAt) {
						//avoid inserting a double-@, which would not expand correctly
						d.CompletionText = d.CompletionText.Substring (1);
					}
				} else if (d.CompletionText[0] == '@') {
					list.RemoveAt (i);
				}
			}
		}

		protected override async Task<ICompletionDataList> HandleCodeCompletion (
			CodeCompletionContext completionContext, bool forced, CancellationToken token)
		{
			var currentLocation = new MonoDevelop.Ide.Editor.DocumentLocation (completionContext.TriggerLine, completionContext.TriggerLineOffset);
			char currentChar = completionContext.TriggerOffset < 1 ? ' ' : Editor.GetCharAt (completionContext.TriggerOffset - 1);

			var codeState = Tracker.Engine.CurrentState as RazorCodeFragmentState;
			if (currentChar == '<' && codeState != null) {
				if (!codeState.IsInsideParentheses && !codeState.IsInsideGenerics) {
					var list = await GetElementCompletions (token);
					return list;
				}
			} else if (currentChar == '>' && Tracker.Engine.CurrentState is RazorCodeFragmentState)
				return ClosingTagCompletion (Editor, currentLocation);

			return await base.HandleCodeCompletion (completionContext, forced, token);
		}

		//we override to ensure we get parent element name even if there's a razor node in between
		protected override async Task<CompletionDataList> GetElementCompletions (CancellationToken token)
		{
			var list = new CompletionDataList ();
			var el = Tracker.Engine.Nodes.OfType<XElement> ().FirstOrDefault ();
			var parentName = el == null ? new XName () : el.Name;

			await AddHtmlTagCompletionData (list, Schema, parentName, token);
			AddMiscBeginTags (list);

			//FIXME: don't show this after any elements
			if (DocType == null)
				list.Add ("!DOCTYPE", "md-literal", MonoDevelop.Core.GettextCatalog.GetString ("Document type"));
			return list;
		}

		/*
		public override bool GetParameterCompletionCommandOffset (out int cpos)
		{
			if (hiddenInfo != null && isInCSharpContext)
				return completionBuilder.GetParameterCompletionCommandOffset (defaultEditor, defaultDocumentContext, hiddenInfo, out cpos);

			return base.GetParameterCompletionCommandOffset (out cpos);
		}*/
		public override Task<int> GetCurrentParameterIndex (int startOffset, CancellationToken token)
		{
			if (hiddenInfo != null && isInCSharpContext) {
				return completionBuilder.GetCurrentParameterIndex (defaultEditor, defaultDocumentContext, hiddenInfo, startOffset);
			}

			return base.GetCurrentParameterIndex (startOffset, token);
		}

		public override Task<MonoDevelop.Ide.CodeCompletion.ParameterHintingResult> HandleParameterCompletionAsync (
			CodeCompletionContext completionContext, char completionChar, CancellationToken token)
		{
			if (hiddenInfo != null && isInCSharpContext) {
				return completionBuilder.HandleParameterCompletion (defaultEditor, defaultDocumentContext, completionContext,
					hiddenInfo, completionChar);
			}

			return base.HandleParameterCompletionAsync (completionContext, completionChar, token);
		}

		#endregion

		#region Document outline

		protected override void RefillOutlineStore (ParsedDocument doc, Gtk.TreeStore store)
		{
			var htmlRoot = razorDocument.PageInfo.HtmlRoot;
			var razorRoot = razorDocument.PageInfo.RazorRoot;
			var blocks = new List<Block> ();
			GetBlocks (razorRoot, blocks);
			BuildTreeChildren (store, Gtk.TreeIter.Zero, htmlRoot, blocks);
		}

		void GetBlocks (Block root, IList<Block> blocks)
		{
			foreach (var block in root.Children.Where (n => n.IsBlock).Select (n => n as Block)) {
				if (block.Type != BlockType.Markup)
					blocks.Add (block);
				if (block.Type != BlockType.Helper)
					GetBlocks (block, blocks);
			}
		}

		protected override void InitializeOutlineColumns (MonoDevelop.Ide.Gui.Components.PadTreeView outlineTree)
		{
			outlineTree.TextRenderer.Xpad = 0;
			outlineTree.TextRenderer.Ypad = 0;
			outlineTree.AppendColumn ("OutlineNode", outlineTree.TextRenderer, new Gtk.TreeCellDataFunc (OutlineTreeDataFunc));
		}

		protected override void OutlineSelectionChanged (object selection)
		{
			SelectNode ((RazorOutlineNode)selection);
		}

		void BuildTreeChildren (Gtk.TreeStore store, Gtk.TreeIter parent, XContainer p, IList<Block> blocks)
		{
			foreach (XNode node in p.Nodes) {
				var el = node as XElement;
				if (el == null) {
					var startLoc = node.Region.Begin;
					var endLoc = node.Region.End;
					var doc = defaultEditor;

					var blocksBetween = blocks.Where (n => n.Start.AbsoluteIndex >= doc.LocationToOffset (startLoc.Line, startLoc.Column)
						&& n.Start.AbsoluteIndex <= doc.LocationToOffset (endLoc.Line, endLoc.Column));

					foreach (var block in blocksBetween) {
						var outlineNode = new RazorOutlineNode (block) {
							Location = new MonoDevelop.Ide.Editor.DocumentRegion (doc.OffsetToLocation (block.Start.AbsoluteIndex),
								doc.OffsetToLocation (block.Start.AbsoluteIndex + block.Length))
						};
						if (!parent.Equals (Gtk.TreeIter.Zero))
							store.AppendValues (parent, outlineNode);
						else
							store.AppendValues (outlineNode);
					}
					continue;
				}

				Gtk.TreeIter childIter;
				if (!parent.Equals (Gtk.TreeIter.Zero))
					childIter = store.AppendValues (parent, new RazorOutlineNode(el));
				else
					childIter = store.AppendValues (new RazorOutlineNode(el));

				BuildTreeChildren (store, childIter, el, blocks);
			}
		}

		void OutlineTreeDataFunc (Gtk.TreeViewColumn column, Gtk.CellRenderer cell, Gtk.TreeModel model, Gtk.TreeIter iter)
		{
			Gtk.CellRendererText txtRenderer = (Gtk.CellRendererText)cell;
			RazorOutlineNode n = (RazorOutlineNode)model.GetValue (iter, 0);
			txtRenderer.Text = n.Name;
		}

		void SelectNode (RazorOutlineNode n)
		{
			EditorSelect (n.Location);
		}

		#endregion
	}
}
