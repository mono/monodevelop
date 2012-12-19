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
using MonoDevelop.AspNet.Gui;
using MonoDevelop.Ide.TypeSystem;
using MonoDevelop.AspNet.Parser.Dom;
using MonoDevelop.AspNet.Mvc.Parser;
using ICSharpCode.NRefactory.TypeSystem;
using Mono.TextEditor;
using System.Web.Razor.Parser.SyntaxTree;
using ICSharpCode.NRefactory;
using MonoDevelop.Xml.StateEngine;
using MonoDevelop.AspNet.Mvc.StateEngine;
using MonoDevelop.Ide.CodeCompletion;
using MonoDevelop.AspNet.Mvc.Completion;
using ICSharpCode.NRefactory.Completion;
using MonoDevelop.Ide.Gui;
using System.Web.Razor.Generator;
using System.Text.RegularExpressions;

namespace MonoDevelop.AspNet.Mvc.Gui
{
	public class RazorCSharpEditorExtension : BaseHtmlEditorExtension
	{
		protected RazorCSharpParsedDocument razorDocument;
		protected UnderlyingDocumentInfo hiddenInfo;
		IRazorCompletionBuilder completionBuilder;

		bool isInCSharpContext;
		Regex DocTypeRegex = new Regex (@"(?:PUBLIC|public)\s+""(?<fpi>[^""]*)""\s+""(?<uri>[^""]*)""");

		ICompletionWidget defaultCompletionWidget;
		Document defaultDocument;

		RazorSyntaxMode syntaxMode;

		UnderlyingDocument HiddenDoc	{
			get { return hiddenInfo.UnderlyingDocument; }
		}

		RazorPageInfo PageInfo {
			get { return razorDocument.PageInfo; }
		}

		protected override RootState CreateRootState ()
		{
			return new RazorFreeState ();
		}

		public override void Initialize ()
		{
			base.Initialize ();

			defaultCompletionWidget = CompletionWidget;
			defaultDocument = Document;
			completionBuilder = RazorCompletionBuilderService.GetBuilder ("C#");

			defaultDocument.Editor.Document.TextReplacing += UnderlyingDocument_TextReplacing;
			defaultDocument.Editor.Caret.PositionChanged += delegate
			{
				OnCompletionContextChanged (CompletionWidget, EventArgs.Empty);
			};
			syntaxMode = new RazorSyntaxMode (Document);
			defaultDocument.Editor.Document.SyntaxMode = syntaxMode;

		}

		public override void Dispose ()
		{
			if (syntaxMode != null) {
				defaultDocument.Editor.Document.SyntaxMode = null;
				syntaxMode.Dispose ();
				syntaxMode = null;
			}
			defaultDocument.Editor.Document.TextReplacing -= UnderlyingDocument_TextReplacing;
			base.Dispose ();
		}

		// Handles text modifications in hidden document
		void UnderlyingDocument_TextReplacing (object sender, DocumentChangeEventArgs e)
		{
			if (razorDocument == null)
				return;

			EnsureUnderlyingDocumentSet ();
			int off = CalculateCaretPosition (e.Offset);

			if (e.RemovalLength > 0) {
				int removalLength = e.RemovalLength;
				if (off + removalLength > HiddenDoc.Editor.Length)
					removalLength = HiddenDoc.Editor.Length - off;
				HiddenDoc.Editor.Remove (new TextSegment (off, removalLength));
			}
			if (e.InsertionLength > 0) {
				if (isInCSharpContext)
					HiddenDoc.Editor.Insert (off, e.InsertedText.Text);
				else // Insert spaces to correctly calculate offsets until next reparse
					HiddenDoc.Editor.Insert (off, new String (' ', e.InsertionLength));
			}
			if (codeFragment != null)
				codeFragment.EndOffset += (e.InsertionLength - e.RemovalLength);
		}

		protected override void OnParsedDocumentUpdated ()
		{
			base.OnParsedDocumentUpdated ();

			razorDocument = CU as RazorCSharpParsedDocument;
			if (razorDocument == null || razorDocument.PageInfo.CSharpParsedFile == null)
				return;

			CreateDocType ();

			// Don't update C# code in hiddenInfo when:
			// 1) We are in a RazorState, and the completion window is visible,
			// it'll freeze (or disappear if we call OnCompletionContextChanged).
			// 2) We're in the middle of writing a Razor expression - if we're in an incorrect state,
			// the generated code migh be behind what we've been already written.

			var state = Tracker.Engine.CurrentState;
			if (state is RazorState && CompletionWindowManager.IsVisible || (!updateNeeded && (state is RazorSpeculativeState
				|| state is RazorExpressionState)))
				UpdateHiddenDocument (false);
			else {
				UpdateHiddenDocument ();
				updateNeeded = false;
			}
		}

		void CreateDocType ()
		{
			DocType = new MonoDevelop.Xml.StateEngine.XDocType (TextLocation.Empty);
			var matches = DocTypeRegex.Match (razorDocument.PageInfo.DocType);
			DocType.PublicFpi = matches.Groups["fpi"].Value;
			DocType.Uri = matches.Groups["uri"].Value;
		}

		void EnsureUnderlyingDocumentSet ()
		{
			if (hiddenInfo == null)
				UpdateHiddenDocument ();
		}

		void UpdateHiddenDocument (bool updateSourceCode = true)
		{
			if (!updateSourceCode && hiddenInfo != null) {
				hiddenInfo.UnderlyingDocument.HiddenParsedDocument = razorDocument.PageInfo.CSharpParsedFile;
				hiddenInfo.UnderlyingDocument.HiddenCompilation = razorDocument.PageInfo.Compilation;
				return;
			}

			hiddenInfo = new UnderlyingDocumentInfo ();

			var viewContent = new HiddenTextEditorViewContent ();
			viewContent.Project = Document.Project;
			viewContent.ContentName = "Generated.cs"; // Use a name with .cs extension to get csharp ambience
			viewContent.Text = razorDocument.PageInfo.CSharpCode;

			var workbenchWindow = new HiddenWorkbenchWindow ();
			workbenchWindow.ViewContent = viewContent;
			hiddenInfo.UnderlyingDocument = new UnderlyingDocument (workbenchWindow) {
				HiddenParsedDocument = razorDocument.PageInfo.CSharpParsedFile,
				HiddenCompilation = razorDocument.PageInfo.Compilation
			};

			currentMappings = razorDocument.PageInfo.GeneratorResults.DesignTimeLineMappings;
			codeFragment = null;
		}

		#region Code completion

		XObject prevNode;
		bool updateNeeded;

		public override bool KeyPress (Gdk.Key key, char keyChar, Gdk.ModifierType modifier)
		{
			Tracker.UpdateEngine ();
			if (razorDocument == null)
				return NonCSharpCompletion (key, keyChar, modifier);

			var n = Tracker.Engine.Nodes.Peek ();
			if (prevNode is RazorExpression && !(n is RazorExpression))
				updateNeeded = true;
			prevNode = n;

			var state = Tracker.Engine.CurrentState;
			int off = document.Editor.Caret.Offset;

			char previousChar = off > 0 ? document.Editor.GetCharAt (off - 1) : ' ';
			char beforePrevious = off > 1 ? document.Editor.GetCharAt (off - 2) : ' ';

			// Determine completion context here, before calling base method to set the context correctly

			// Rule out Razor comments, html, transition sign (@) and e-mail addresses
			if (state is RazorCommentState || (previousChar != '@' && !(state is RazorState))  || keyChar == '@'
				|| (previousChar == '@' && Char.IsLetterOrDigit (beforePrevious)))
				return NonCSharpCompletion (key, keyChar, modifier);

			// Determine if we are inside generics
			if (previousChar == '<') {
				var codeState = state as RazorCodeFragmentState;
				if (codeState == null || !codeState.IsInsideGenerics)
					return NonCSharpCompletion (key, keyChar, modifier);
			}
			// Determine whether we begin an html tag or generics
			else if (keyChar == '<' && (n is XElement || !Char.IsLetterOrDigit (previousChar)))
				return NonCSharpCompletion (key, keyChar, modifier);
			// Determine whether we are inside html text or in code
			else if (previousChar != '@' && n is XElement && !(state is RazorSpeculativeState) && !(state is RazorExpressionState))
			    return NonCSharpCompletion (key, keyChar, modifier);

			// We're in C# context
			InitializeCodeCompletion ();
			SwitchToHidden ();

			bool result;
			try {
				result = base.KeyPress (key, keyChar, modifier);
				if (EnableParameterInsight && (keyChar == ',' || keyChar == ')') && CanRunParameterCompletionCommand ())
				    base.RunParameterCompletionCommand ();
			} finally {
				SwitchToReal ();
			}

			return result;
		}

		protected void SwitchToHidden ()
		{
			isInCSharpContext = true;
			document = HiddenDoc;
			CompletionWidget = completionBuilder.CreateCompletionWidget (defaultDocument, hiddenInfo);
		}

		protected void SwitchToReal ()
		{
			isInCSharpContext = false;
			document = defaultDocument;
			CompletionWidget = defaultCompletionWidget;
		}

		bool NonCSharpCompletion (Gdk.Key key, char keyChar, Gdk.ModifierType modifier)
		{
			isInCSharpContext = false;
			return base.KeyPress (key, keyChar, modifier);
		}

		protected void InitializeCodeCompletion ()
		{
			EnsureUnderlyingDocumentSet ();
			hiddenInfo.OriginalCaretPosition = defaultDocument.Editor.Caret.Offset;
			hiddenInfo.CaretPosition = CalculateCaretPosition ();
			HiddenDoc.Editor.Caret.Offset = hiddenInfo.CaretPosition;
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
			var type = razorDocument.PageInfo.CSharpParsedFile.TopLevelTypeDefinitions.FirstOrDefault ();
			if (type == null) {
				return -1;
			}
			var method = type.Members.FirstOrDefault (m => m.Name == "Execute");
			if (method == null) {
				return -1;
			}
			return HiddenDoc.Editor.LocationToOffset (method.BodyRegion.Begin) + 1;
		}

		IDictionary<int, GeneratedCodeMapping> currentMappings;
		CodeFragment codeFragment;

		int CalculateCaretPosition ()
		{
			return CalculateCaretPosition (defaultDocument.Editor.Caret.Offset);
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
				HiddenDoc.Editor.Insert (defaultPosition, newLine);
				map = new KeyValuePair<int, GeneratedCodeMapping> (0, new GeneratedCodeMapping (currentOffset - 1, 0, 0, 0, 0));
				currentMappings.Add (map);
			} else {
				var result = currentMappings.Where (m => m.Value.StartOffset <= currentOffset);
				if (!result.Any ())
					return defaultPosition;
				map = result.Last ();
			}

			string pattern = "#line " + map.Key + " ";
			int pos = HiddenDoc.Editor.Document.IndexOf (pattern, 0, HiddenDoc.Editor.Document.TextLength, StringComparison.Ordinal);
			if (pos == -1 || !map.Value.StartOffset.HasValue)
				return defaultPosition;

			int startRealOff = map.Value.StartOffset.Value;
			int offDifference = currentOffset - (startRealOff + map.Value.CodeLength);
			var line = HiddenDoc.Editor.Document.GetLineByOffset (pos);
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
				HiddenDoc.Editor.Insert (newOff, newLine);
				HiddenDoc.Editor.Insert (newOff + newLine.Length, new String (' ', offDifference) + " \r\n");

				var newMap = new KeyValuePair<int, GeneratedCodeMapping> (key, new GeneratedCodeMapping (
					startRealOff + map.Value.CodeLength, 0, 0, 0, offDifference));
				currentMappings.Add (newMap);
				hiddenOff = newOff + newLine.Length + offDifference;
				codeFragment = new CodeFragment (newOff + newLine.Length, newMap.Value.StartOffset.Value,
					newOff + newLine.Length + offDifference);
			}

			return hiddenOff;
		}

		public override ICompletionDataList HandleCodeCompletion (CodeCompletionContext completionContext,
			char completionChar, ref int triggerWordLength)
		{
			if (!EnableCodeCompletion)
				return null;

			char previousChar = defaultDocument.Editor.Caret.Offset > 1 ? defaultDocument.Editor.GetCharAt (
				defaultDocument.Editor.Caret.Offset - 2) : ' ';

			// Don't show completion window when directive's name is being typed
			var directive = Tracker.Engine.Nodes.Peek () as RazorDirective;
			if (directive != null && !directive.FirstBracket.HasValue)
				return null;

			if (hiddenInfo != null && isInCSharpContext) {
				var list = (CompletionDataList) completionBuilder.HandleCompletion (defaultDocument, completionContext,
					hiddenInfo, completionChar, ref triggerWordLength);

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

			return base.HandleCodeCompletion (completionContext, completionChar, ref triggerWordLength);
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

		protected override ICompletionDataList HandleCodeCompletion (CodeCompletionContext completionContext,
			bool forced, ref int triggerWordLength)
		{
			if (!EnableCodeCompletion)
				return null;

			var currentLocation = new TextLocation (completionContext.TriggerLine, completionContext.TriggerLineOffset);
			char currentChar = completionContext.TriggerOffset < 1 ? ' ' : Buffer.GetCharAt (completionContext.TriggerOffset - 1);

			var codeState = Tracker.Engine.CurrentState as RazorCodeFragmentState;
			if (currentChar == '<' && codeState != null) {
				if (!codeState.IsInsideParentheses && !codeState.IsInsideGenerics) {
					var list = new CompletionDataList ();
					GetElementCompletions (list);
					return list;
				}
			} else if (currentChar == '>' && Tracker.Engine.CurrentState is RazorCodeFragmentState)
				return ClosingTagCompletion (EditableBuffer, currentLocation);

			return base.HandleCodeCompletion (completionContext, forced, ref triggerWordLength);
		}

		//we override to ensure we get parent element name even if there's a razor node in between
		protected override void GetElementCompletions (CompletionDataList list)
		{
			var el = Tracker.Engine.Nodes.OfType<XElement> ().FirstOrDefault ();
			var parentName = el == null ? new XName () : el.Name;

			AddHtmlTagCompletionData (list, Schema, parentName.ToLower ());
			AddMiscBeginTags (list);

			//FIXME: don't show this after any elements
			if (DocType == null)
				list.Add ("!DOCTYPE", "md-literal", MonoDevelop.Core.GettextCatalog.GetString ("Document type"));
		}

		public override ICompletionDataList CodeCompletionCommand (CodeCompletionContext completionContext)
		{
			if (hiddenInfo != null && (isInCSharpContext || Tracker.Engine.CurrentState is RazorState)
				&& !(Tracker.Engine.Nodes.Peek () is XElement)) {
				InitializeCodeCompletion ();
				return completionBuilder.HandlePopupCompletion (defaultDocument, hiddenInfo);
			}

			return base.CodeCompletionCommand (completionContext);
		}

		public override bool GetParameterCompletionCommandOffset (out int cpos)
		{
			if (hiddenInfo != null && isInCSharpContext)
				return completionBuilder.GetParameterCompletionCommandOffset (defaultDocument, hiddenInfo, out cpos);

			return base.GetParameterCompletionCommandOffset (out cpos);
		}

		public override int GetCurrentParameterIndex (int startOffset)
		{
			if (hiddenInfo != null && isInCSharpContext) {
				return completionBuilder.GetCurrentParameterIndex (defaultDocument, hiddenInfo, startOffset);
			}

			return base.GetCurrentParameterIndex (startOffset);
		}

		public override ParameterDataProvider HandleParameterCompletion (CodeCompletionContext completionContext,
			char completionChar)
		{
			if (hiddenInfo != null && isInCSharpContext) {
				return completionBuilder.HandleParameterCompletion (defaultDocument, completionContext,
					hiddenInfo, completionChar);
			}

			return base.HandleParameterCompletion (completionContext, completionChar);
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
			SelectNode ((OutlineNode)selection);
		}

		void BuildTreeChildren (Gtk.TreeStore store, Gtk.TreeIter parent, ParentNode p, IList<Block> blocks)
		{
			foreach (Node node in p) {
				if (!(node is TagNode)) {
					var startLoc = new TextLocation (node.Location.BeginLine, node.Location.BeginColumn);
					var endLoc = new TextLocation (node.Location.EndLine, node.Location.EndColumn);
					var doc = defaultDocument.Editor.Document;

					var blocksBetween = blocks.Where (n => n.Start.AbsoluteIndex >= doc.GetOffset (startLoc)
						&& n.Start.AbsoluteIndex <= doc.GetOffset (endLoc));

					foreach (var block in blocksBetween) {
						var outlineNode = new OutlineNode (block) {
							Location = new DomRegion (doc.OffsetToLocation (block.Start.AbsoluteIndex),
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
					childIter = store.AppendValues (parent, new OutlineNode(node as TagNode));
				else
					childIter = store.AppendValues (new OutlineNode(node as TagNode));

				ParentNode pChild = node as ParentNode;
				if (pChild != null)
					BuildTreeChildren (store, childIter, pChild, blocks);
			}
		}

		void OutlineTreeDataFunc (Gtk.TreeViewColumn column, Gtk.CellRenderer cell, Gtk.TreeModel model, Gtk.TreeIter iter)
		{
			Gtk.CellRendererText txtRenderer = (Gtk.CellRendererText)cell;
			OutlineNode n = (OutlineNode)model.GetValue (iter, 0);
			txtRenderer.Text = n.Name;
		}

		void SelectNode (OutlineNode n)
		{
			EditorSelect (n.Location);
		}

		#endregion
	}
}
