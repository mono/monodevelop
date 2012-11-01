// ExtendibleTextEditor.cs
//
// Author:
//   Mike Krüger <mkrueger@novell.com>
//
// Copyright (c) 2008 Novell, Inc (http://www.novell.com)
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

using Gtk;

using Mono.TextEditor;
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Gui.Content;
using MonoDevelop.Ide.CodeCompletion;
using MonoDevelop.Components.Commands;
using Mono.TextEditor.Highlighting;
using MonoDevelop.Ide.CodeTemplates;
using Mono.Addins;
using MonoDevelop.Projects.Text;
using MonoDevelop.Ide;
using MonoDevelop.Ide.CodeFormatting;
using MonoDevelop.SourceEditor.Extension;
using ICSharpCode.NRefactory.TypeSystem;
using MonoDevelop.Ide.TypeSystem;
using ICSharpCode.NRefactory.Semantics;

namespace MonoDevelop.SourceEditor
{
	public class ExtensibleTextEditor : Mono.TextEditor.TextEditor
	{
		internal object MemoryProbe = Counters.EditorsInMemory.CreateMemoryProbe ();
		
		SourceEditorView view;
		ExtensionContext extensionContext;
		Adjustment cachedHAdjustment, cachedVAdjustment;
		
		public ITextEditorExtension Extension {
			get;
			set;
		}
		
		public new ISourceEditorOptions Options {
			get { return (ISourceEditorOptions)base.Options; }
		}
		
		public ExtensibleTextEditor (SourceEditorView view, ISourceEditorOptions options, Mono.TextEditor.TextDocument doc) : base(doc, options)
		{
			Initialize (view);
		}
		
		public ExtensibleTextEditor (SourceEditorView view)
		{
			base.Options = new StyledSourceEditorOptions (view.Project, null);
			Initialize (view);
		}
		
		internal SourceEditorView View {
			get { return view; }
		}
		
		void Initialize (SourceEditorView view)
		{
			this.view = view;
			Caret.PositionChanged += delegate {
				if (Extension != null) {
					try {
						Extension.CursorPositionChanged ();
					} catch (Exception ex) {
						ReportExtensionError (ex);
					}
				}
			};
			
			Document.TextReplaced += HandleSkipCharsOnReplace;
			
			Document.TextReplaced += delegate(object sender, DocumentChangeEventArgs args) {
				if (Extension != null) {
					try {
						Extension.TextChanged (args.Offset, args.Offset + Math.Max (args.RemovalLength, args.InsertionLength));
					} catch (Exception ex) {
						ReportExtensionError (ex);
					}
				}
			};
			
			UpdateEditMode ();
			this.DoPopupMenu = ShowPopup;
		}
		
		void HandleSkipCharsOnReplace (object sender, DocumentChangeEventArgs args)
		{
			var skipChars = GetTextEditorData ().SkipChars;
			for (int i = 0; i < skipChars.Count; i++) {
				var sc = skipChars [i];
				if (args.Offset > sc.Offset) {
					skipChars.RemoveAt (i);
					i--;
					continue;
				}
				if (args.Offset <= sc.Offset) {
					sc.Offset += args.ChangeDelta;
				}
			}
		}
		
		public ExtensionContext ExtensionContext {
			get {
				return extensionContext;
			}
			set {
				if (extensionContext != null) {
					extensionContext.RemoveExtensionNodeHandler ("MonoDevelop/SourceEditor2/TooltipProviders", OnTooltipProviderChanged);
					ClearTooltipProviders ();
				}
				extensionContext = value;
				if (extensionContext != null)
					extensionContext.AddExtensionNodeHandler ("MonoDevelop/SourceEditor2/TooltipProviders", OnTooltipProviderChanged);
			}
		}
		
		static bool? testNewViMode = null;
		static bool TestNewViMode {
			get {
				if (!testNewViMode.HasValue)
					testNewViMode = System.Environment.GetEnvironmentVariable ("TEST_NEW_VI_MODE") != null;
				return testNewViMode.Value;
			}
		}
		
		void UpdateEditMode ()
		{
			if (Options.UseViModes) {
				if (TestNewViMode) {
					if (!(CurrentMode is NewIdeViMode))
					CurrentMode = new NewIdeViMode (this);
				} else {
					if (!(CurrentMode is IdeViMode))
						CurrentMode = new IdeViMode (this);
				}
			} else {
		//		if (!(CurrentMode is SimpleEditMode)){
					SimpleEditMode simpleMode = new SimpleEditMode ();
					simpleMode.KeyBindings [EditMode.GetKeyCode (Gdk.Key.Tab)] = new TabAction (this).Action;
					simpleMode.KeyBindings [EditMode.GetKeyCode (Gdk.Key.BackSpace)] = EditActions.AdvancedBackspace;
					CurrentMode = simpleMode;
		//		}
			}
		}

		void UnregisterAdjustments ()
		{
			if (cachedHAdjustment != null)
				cachedHAdjustment.ValueChanged -= HAdjustment_ValueChanged;
			if (cachedVAdjustment != null)
				cachedVAdjustment.ValueChanged -= VAdjustment_ValueChanged;
			cachedHAdjustment = null;
			cachedVAdjustment = null;
		}

		protected override void OnDestroyed ()
		{
			UnregisterAdjustments ();

			ExtensionContext = null;
			view = null;
			base.OnDestroyed ();
			if (Options != null) {
				Options.Dispose ();
				base.Options = null;
			}
		}
		
		void OnTooltipProviderChanged (object s, ExtensionNodeEventArgs a)
		{
			TooltipProvider provider;
			try {
				provider = (TooltipProvider) a.ExtensionObject;
			} catch (Exception e) {
				LoggingService.LogError ("Can't create tooltip provider:"+ a.ExtensionNode, e);
				return;
			}
			if (a.Change == ExtensionChange.Add) {
				AddTooltipProvider (provider);
			} else {
				RemoveTooltipProvider (provider);
			}
		}
		
		public void FireOptionsChange ()
		{
			this.OptionsChanged (null, null);
		}
		
		protected override void OptionsChanged (object sender, EventArgs args)
		{
			if (view != null && view.Control != null) {
				if (!Options.ShowFoldMargin)
					this.Document.ClearFoldSegments ();
			}
			UpdateEditMode ();
			base.OptionsChanged (sender, args);
		}
		
		bool isInKeyStroke = false;
		protected override bool OnKeyPressEvent (Gdk.EventKey evnt)
		{
			isInKeyStroke = true;
			try {
				// Handle keyboard toolip popup
	/*			if ((evnt.Key == Gdk.Key.F1 && (evnt.State & Gdk.ModifierType.ControlMask) == Gdk.ModifierType.ControlMask)) {
					Gdk.Point p = this.TextViewMargin.LocationToDisplayCoordinates (this.Caret.Location);
					this.mx = p.X;
					this.my = p.Y;
					this.ShowTooltip ();
					return true;
				}
	*/			
				return base.OnKeyPressEvent (evnt);
			} finally {
				isInKeyStroke = false;
			}
		}
		
		bool ExtensionKeyPress (Gdk.Key key, uint ch, Gdk.ModifierType state)
		{
			try {
				return Extension.KeyPress (key, (char)ch, state);
			} catch (Exception ex) {
				ReportExtensionError (ex);
			}
			return false;
		}
		
		void ReportExtensionError (Exception ex) 
		{
			MonoDevelop.Core.LoggingService.LogError ("Error in text editor extension chain", ex);
			MessageService.ShowException (ex, "Error in text editor extension chain");
		}
		
		IEnumerable<char> TextWithoutCommentsAndStrings {
			get {
				return from p in GetTextWithoutCommentsAndStrings (Document, 0, Document.TextLength) select p.Key;
			}
		}
		
		static IEnumerable<KeyValuePair <char, int>> GetTextWithoutCommentsAndStrings (Mono.TextEditor.TextDocument doc, int start, int end) 
		{
			bool isInString = false, isInChar = false;
			bool isInLineComment = false, isInBlockComment = false;
			
			for (int pos = start; pos < end; pos++) {
				char ch = doc.GetCharAt (pos);
				switch (ch) {
					case '\r':
					case '\n':
						isInLineComment = false;
						break;
					case '/':
						if (isInBlockComment) {
							if (pos > 0 && doc.GetCharAt (pos - 1) == '*') 
								isInBlockComment = false;
						} else  if (!isInString && !isInChar && pos + 1 < doc.TextLength) {
							char nextChar = doc.GetCharAt (pos + 1);
							if (nextChar == '/')
								isInLineComment = true;
							if (!isInLineComment && nextChar == '*')
								isInBlockComment = true;
						}
						break;
					case '"':
						if (!(isInChar || isInLineComment || isInBlockComment)) 
							isInString = !isInString;
						break;
					case '\'':
						if (!(isInString || isInLineComment || isInBlockComment)) 
							isInChar = !isInChar;
						break;
					default :
						if (!(isInString || isInChar || isInLineComment || isInBlockComment))
							yield return new KeyValuePair<char, int> (ch, pos);
						break;
				}
			}
		}
		
		
		protected override bool OnIMProcessedKeyPressEvent (Gdk.Key key, uint ch, Gdk.ModifierType state)
		{
			bool result = true;
			if (key == Gdk.Key.Escape) {
				bool b = Extension != null ? ExtensionKeyPress (key, ch, state) : base.OnIMProcessedKeyPressEvent (key, ch, state);
				if (b) {
					view.SourceEditorWidget.RemoveSearchWidget ();
					return true;
				}
				return false; 
			}

			if (Document == null)
				return true;

			bool inStringOrComment = false;
			DocumentLine line = Document.GetLine (Caret.Line);
			if (line == null)
				return true;
			bool inChar = false;
			bool inComment = false;
			bool inString = false;
			//			string escape = "\"";
			var stack = line.StartSpan.Clone ();
			var sm = Document.SyntaxMode as SyntaxMode;
			if (sm != null)
				Mono.TextEditor.Highlighting.SyntaxModeService.ScanSpans (Document, sm, sm, stack, line.Offset, Caret.Offset);
			foreach (Span span in stack) {
				if (string.IsNullOrEmpty (span.Color))
					continue;
				if (span.Color == "string.other") {
					inStringOrComment = inChar = inString = true;
					break;
				}
				if (span.Color == "string.single" || span.Color == "string.double" || span.Color.StartsWith ("comment")) {
					inStringOrComment = true;
					inChar |= span.Color == "string.single";
					inComment |= span.Color.StartsWith ("comment");
					inString = !inChar && !inComment;
					//escape = span.Escape;
					break;
				}
			}
			if (Caret.Offset > 0) {
				char c = GetCharAt (Caret.Offset - 1);
				if (c == '"' || c == '\'')
					inStringOrComment = inChar = inString = true;
			}

			// insert template when space is typed (currently disabled - it's annoying).
			bool templateInserted = false;
			//!inStringOrComment && (key == Gdk.Key.space) && DoInsertTemplate ();
			bool returnBetweenBraces = key == Gdk.Key.Return && (state & (Gdk.ModifierType.ControlMask | Gdk.ModifierType.ShiftMask)) == Gdk.ModifierType.None && Caret.Offset > 0 && Caret.Offset < Document.TextLength && Document.GetCharAt (Caret.Offset - 1) == '{' && Document.GetCharAt (Caret.Offset) == '}' && !inStringOrComment;
//			int initialOffset = Caret.Offset;
			const string openBrackets = "{[('\"";
			const string closingBrackets = "}])'\"";
			int braceIndex = openBrackets.IndexOf ((char)ch);
			var skipChars = GetTextEditorData ().SkipChars;
			var skipChar = skipChars.Find (sc => sc.Char == (char)ch && sc.Offset == Caret.Offset);
//			bool startedAtomicOperation = false;

			// special handling for escape chars inside ' and "
			if (Caret.Offset > 0) {
				char charBefore = Document.GetCharAt (Caret.Offset - 1);
				if (inStringOrComment && (ch == '"' || (inChar && ch == '\'')) && charBefore == '\\')
					skipChar = null;
			}
			char insertionChar = '\0';
			bool insertMatchingBracket = false;
			IDisposable undoGroup = null;
			if (skipChar == null && Options.AutoInsertMatchingBracket && braceIndex >= 0 && !IsSomethingSelected) {
				if (!inStringOrComment) {
					char closingBrace = closingBrackets [braceIndex];
					char openingBrace = openBrackets [braceIndex];

					int count = 0;
					foreach (char curCh in TextWithoutCommentsAndStrings) {
						if (curCh == openingBrace) {
							count++;
						} else if (curCh == closingBrace) {
							count--;
						}
					}

					if (count >= 0) {
						insertMatchingBracket = true;
						insertionChar = closingBrace;
					}
				} else {
					char charBefore = Document.GetCharAt (Caret.Offset - 1);
					if (!inString && !inComment && !inChar && ch == '"' && charBefore != '\\') {
						insertMatchingBracket = true;
						insertionChar = '"';
					}
				}
			}
			
			//Console.WriteLine (Caret.Offset + "/" + insOff);
			if (insertMatchingBracket)
				undoGroup = Document.OpenUndoGroup ();

			var oldMode = Caret.IsInInsertMode;
			if (skipChar != null) {
				Caret.IsInInsertMode = false;
				skipChars.Remove (skipChar);
			}
			if (Extension != null) {
				if (ExtensionKeyPress (key, ch, state)) 
					result = base.OnIMProcessedKeyPressEvent (key, ch, state);
				if (returnBetweenBraces)
					HitReturn ();
			} else {
				result = base.OnIMProcessedKeyPressEvent (key, ch, state);
				if (returnBetweenBraces)
					HitReturn ();
			}
			if (skipChar != null) {
				Caret.IsInInsertMode = oldMode;
			}

			if (insertMatchingBracket) {
				GetTextEditorData ().EnsureCaretIsNotVirtual ();
				int offset = Caret.Offset;
				Caret.AutoUpdatePosition = false;
				Insert (offset, insertionChar.ToString ());
				Caret.AutoUpdatePosition = true;
				GetTextEditorData ().SetSkipChar (offset, insertionChar);
				undoGroup.Dispose ();
			}
			return templateInserted || result;
		}
		
		void HitReturn ()
		{
			int o = Caret.Offset - 1;
			while (o > 0 && char.IsWhiteSpace (GetCharAt (o - 1))) 
				o--;
			Caret.Offset = o;
			ExtensionKeyPress (Gdk.Key.Return, (char)0, Gdk.ModifierType.None);			
		}
		
		internal string GetErrorInformationAt (int offset)
		{
			DocumentLocation location = Document.OffsetToLocation (offset);
			DocumentLine line = Document.GetLine (location.Line);
			if (line == null)
				return null;
			var error = line.Markers.FirstOrDefault (m => m is ErrorMarker) as ErrorMarker;
			
			if (error != null) {
				if (error.Info.ErrorType == ErrorType.Warning)
					return GettextCatalog.GetString ("<b>Parser Warning</b>: {0}",
					                                 GLib.Markup.EscapeText (error.Info.Message));
				return GettextCatalog.GetString ("<b>Parser Error</b>: {0}",
				                                 GLib.Markup.EscapeText (error.Info.Message));
			}
			return null;
		}
		
		internal ParsedDocument ParsedDocument {
			get {
				var doc = IdeApp.Workbench.ActiveDocument;
				if (doc != null) 
					return doc.ParsedDocument;
				return null;
			}
		}
		
		public MonoDevelop.Projects.Project Project {
			get {
				var doc = IdeApp.Workbench.ActiveDocument;
				if (doc != null) 
					return doc.Project;
				return null;
			}
		}
		
		int           oldOffset = -1;
		ResolveResult resolveResult = null;
		DomRegion     resolveRegion = DomRegion.Empty;
		public ResolveResult GetLanguageItem (int offset, out DomRegion region)
		{
			// we'll cache old results.
			if (offset == oldOffset) {
				region = this.resolveRegion;
				return this.resolveResult;
			}
			
			oldOffset = offset;
			
			if (textEditorResolverProvider != null) {
				this.resolveResult = textEditorResolverProvider.GetLanguageItem (view.WorkbenchWindow.Document, offset, out region);
				this.resolveRegion = region;
			} else {
				region = DomRegion.Empty;
				this.resolveResult = null;
				this.resolveRegion = region;
			}
			
			return this.resolveResult;
		}
		
		public CodeTemplateContext GetTemplateContext ()
		{
			if (IsSomethingSelected) {
				var result = GetLanguageItem (Caret.Offset, Document.GetTextAt (SelectionRange));
				if (result != null && !result.IsError)
					return CodeTemplateContext.InExpression;
			}
			return CodeTemplateContext.Standard;
		}
		
		ITextEditorResolverProvider textEditorResolverProvider;
		
		public ITextEditorResolverProvider TextEditorResolverProvider {
			get { return this.textEditorResolverProvider; }
			internal set { this.textEditorResolverProvider = value; }
		}
		
		public ResolveResult GetLanguageItem (int offset, string expression)
		{
			oldOffset = offset;
			
			if (textEditorResolverProvider != null) {
				this.resolveResult = textEditorResolverProvider.GetLanguageItem (view.WorkbenchWindow.Document, offset, expression);
			} else {
				this.resolveResult = null;
			}
	
			return this.resolveResult;
		}

//		public string GetExpression (int offset)
//		{
//			if (textEditorResolverProvider != null) 
//				return textEditorResolverProvider.GetExpression (view.WorkbenchWindow.Document, offset);
//			return string.Empty;
//		}
		
		string GetExpressionBeforeOffset (int offset)
		{
			int start = offset;
			while (start > 0 && IsIdChar (Document.GetCharAt (start)))
				start--;
			while (offset < Document.TextLength && IsIdChar (Document.GetCharAt (offset)))
				offset++;
			start++;
			if (offset - start > 0 && start < Document.TextLength)
				return Document.GetTextAt (start, offset - start);
			else
				return string.Empty;
		}
		
		bool IsIdChar (char c)
		{
			return char.IsLetterOrDigit (c) || c == '_';
		}
		
		protected override bool OnFocusOutEvent (Gdk.EventFocus evnt)
		{
			CompletionWindowManager.HideWindow ();
			ParameterInformationWindowManager.HideWindow (null, view);
			return base.OnFocusOutEvent (evnt); 
		}
		
		void ShowPopup (Gdk.EventButton evt)
		{
			// Fire event that will close an open outo complete window
			view.FireCompletionContextChanged ();
			HideTooltip ();
			const string menuPath = "/MonoDevelop/SourceEditor2/ContextMenu/Editor";
			var ctx = ExtensionContext ?? AddinManager.AddinEngine;
			CommandEntrySet cset = IdeApp.CommandService.CreateCommandEntrySet (ctx, menuPath);
			Gtk.Menu menu = IdeApp.CommandService.CreateMenu (cset);
			
			var imMenu = CreateInputMethodMenuItem (GettextCatalog.GetString ("_Input Methods"));
			if (imMenu != null) {
				menu.Append (new SeparatorMenuItem ());
				menu.Append (imMenu);
			}
			
			menu.Destroyed += delegate {
				this.QueueDraw ();
			};
			
			if (evt != null) {
				GtkWorkarounds.ShowContextMenu (menu, this, evt);
			} else {
				var pt = LocationToPoint (this.Caret.Location);
				GtkWorkarounds.ShowContextMenu (menu, this, new Gdk.Rectangle (pt.X, pt.Y, 1, (int)LineHeight));
			}
		}
		
#region Templates
		int FindPrevWordStart (int offset)
		{
			while (--offset >= 0 && !Char.IsWhiteSpace (Document.GetCharAt (offset))) 
				;
			return ++offset;
		}

		public string GetWordBeforeCaret ()
		{
			int offset = this.Caret.Offset;
			int start  = FindPrevWordStart (offset);
			return Document.GetTextAt (start, offset - start);
		}
		
		public bool IsTemplateKnown ()
		{
			string word = GetWordBeforeCaret ();
			bool result = false;
			foreach (CodeTemplate template in CodeTemplateService.GetCodeTemplates (Document.MimeType)) {
				if (template.Shortcut == word) {
					result = true;
				} else if (template.Shortcut.StartsWith (word)) {
					result = false;
					break;
				}
			}
			return result;
		}
		
		public bool DoInsertTemplate ()
		{
			string word = GetWordBeforeCaret ();
			foreach (CodeTemplate template in CodeTemplateService.GetCodeTemplates (Document.MimeType)) {
				if (template.Shortcut == word) {
					InsertTemplate (template, view.WorkbenchWindow.Document);
					return true;
				}
			}
			return false;
		}
		

		internal void InsertTemplate (CodeTemplate template, MonoDevelop.Ide.Gui.Document document)
		{
			using (var undo = Document.OpenUndoGroup ()) {
				var result = template.InsertTemplateContents (document);

				var links = result.TextLinks;

				var tle = new TextLinkEditMode (this, result.InsertPosition, links);
				tle.TextLinkMode = TextLinkMode.General;
				if (tle.ShouldStartTextLinkMode) {
					tle.OldMode = CurrentMode;
					tle.StartMode ();
					CurrentMode = tle;
				}
			}
		}

		protected override void OnScrollAdjustmentsSet()
		{
			UnregisterAdjustments ();
			if (HAdjustment != null) {
				cachedHAdjustment = HAdjustment;
				HAdjustment.ValueChanged += HAdjustment_ValueChanged;
			}
			if (VAdjustment != null) {
				cachedVAdjustment = VAdjustment;
				VAdjustment.ValueChanged += VAdjustment_ValueChanged;
			}
		}

		void VAdjustment_ValueChanged (object sender, EventArgs e)
		{
			CompletionWindowManager.HideWindow ();
			ParameterInformationWindowManager.HideWindow (null, view);
		}

		void HAdjustment_ValueChanged (object sender, EventArgs e)
		{
			if (!isInKeyStroke) {
				CompletionWindowManager.HideWindow ();
				ParameterInformationWindowManager.HideWindow (null, view);
			}
		}
		
#endregion
		
#region Key bindings

		[CommandHandler (MonoDevelop.Ide.Commands.TextEditorCommands.LineEnd)]
		internal void OnLineEnd ()
		{
			RunAction (CaretMoveActions.LineEnd);
		}
		
		[CommandHandler (MonoDevelop.Ide.Commands.TextEditorCommands.LineStart)]
		internal void OnLineStart ()
		{
			RunAction (CaretMoveActions.LineHome);
		}
		
		[CommandHandler (MonoDevelop.Ide.Commands.TextEditorCommands.DeleteLeftChar)]
		internal void OnDeleteLeftChar ()
		{
			RunAction (DeleteActions.Backspace);
		}
		
		[CommandHandler (MonoDevelop.Ide.Commands.TextEditorCommands.DeleteRightChar)]
		internal void OnDeleteRightChar ()
		{
			RunAction (DeleteActions.Delete);
		}
		
		[CommandHandler (MonoDevelop.Ide.Commands.TextEditorCommands.CharLeft)]
		internal void OnCharLeft ()
		{
			RunAction (CaretMoveActions.Left);
		}
		
		[CommandHandler (MonoDevelop.Ide.Commands.TextEditorCommands.CharRight)]
		internal void OnCharRight ()
		{
			RunAction (CaretMoveActions.Right);
		}
		
		[CommandHandler (MonoDevelop.Ide.Commands.TextEditorCommands.LineUp)]
		internal void OnLineUp ()
		{
			RunAction (CaretMoveActions.Up);
		}
		
		[CommandHandler (MonoDevelop.Ide.Commands.TextEditorCommands.LineDown)]
		internal void OnLineDown ()
		{
			RunAction (CaretMoveActions.Down);
		}
		
		[CommandHandler (MonoDevelop.Ide.Commands.TextEditorCommands.DocumentStart)]
		internal void OnDocumentStart ()
		{
			RunAction (CaretMoveActions.ToDocumentStart);
		}
		
		[CommandHandler (MonoDevelop.Ide.Commands.TextEditorCommands.DocumentEnd)]
		internal void OnDocumentEnd ()
		{
			RunAction (CaretMoveActions.ToDocumentEnd);
		}
		
		[CommandHandler (MonoDevelop.Ide.Commands.TextEditorCommands.PageUp)]
		internal void OnPageUp ()
		{
			RunAction (CaretMoveActions.PageUp);
		}
		
		[CommandHandler (MonoDevelop.Ide.Commands.TextEditorCommands.PageDown)]
		internal void OnPageDown ()
		{
			RunAction (CaretMoveActions.PageDown);
		}
		
		[CommandHandler (MonoDevelop.Ide.Commands.TextEditorCommands.DeleteLine)]
		internal void OnDeleteLine ()
		{
			RunAction (DeleteActions.CaretLine);
		}
		
		[CommandHandler (MonoDevelop.Ide.Commands.TextEditorCommands.DeleteToLineEnd)]
		internal void OnDeleteToLineEnd ()
		{
			RunAction (DeleteActions.CaretLineToEnd);
		}
		
 		[CommandHandler (MonoDevelop.Ide.Commands.TextEditorCommands.ScrollLineUp)]
		internal void OnScrollLineUp ()
		{
			RunAction (ScrollActions.Up);
		}
		
		[CommandHandler (MonoDevelop.Ide.Commands.TextEditorCommands.ScrollLineDown)]
		internal void OnScrollLineDown ()
		{
			RunAction (ScrollActions.Down);
		}
		
		[CommandHandler (MonoDevelop.Ide.Commands.TextEditorCommands.ScrollPageUp)]
		internal void OnScrollPageUp ()
		{
			RunAction (ScrollActions.PageUp);
		}
		
		[CommandHandler (MonoDevelop.Ide.Commands.TextEditorCommands.ScrollPageDown)]
		internal void OnScrollPageDown ()
		{
			RunAction (ScrollActions.PageDown);
		}

		[CommandHandler (MonoDevelop.Ide.Commands.TextEditorCommands.GotoMatchingBrace)]
		internal void OnGotoMatchingBrace ()
		{
			RunAction (MiscActions.GotoMatchingBracket);
		}

		[CommandHandler (MonoDevelop.Ide.Commands.TextEditorCommands.SelectionMoveLeft)]
		internal void OnSelectionMoveLeft ()
		{
			RunAction (SelectionActions.MoveLeft);
		}

		[CommandHandler (MonoDevelop.Ide.Commands.TextEditorCommands.SelectionMoveRight)]
		internal void OnSelectionMoveRight ()
		{
			RunAction (SelectionActions.MoveRight);
		}

		[CommandHandler (MonoDevelop.Ide.Commands.TextEditorCommands.MovePrevWord)]
		internal void OnMovePrevWord ()
		{
			RunAction (CaretMoveActions.PreviousWord);
		}

		[CommandHandler (MonoDevelop.Ide.Commands.TextEditorCommands.MoveNextWord)]
		internal void OnMoveNextWord ()
		{
			RunAction (CaretMoveActions.NextWord);
		}

		[CommandHandler (MonoDevelop.Ide.Commands.TextEditorCommands.SelectionMovePrevWord)]
		internal void OnSelectionMovePrevWord ()
		{
			RunAction (SelectionActions.MovePreviousWord);
		}

		[CommandHandler (MonoDevelop.Ide.Commands.TextEditorCommands.SelectionMoveNextWord)]
		internal void OnSelectionMoveNextWord ()
		{
			RunAction (SelectionActions.MoveNextWord);
		}
		
		[CommandHandler (MonoDevelop.Ide.Commands.TextEditorCommands.MovePrevSubword)]
		internal void OnMovePrevSubword ()
		{
			RunAction (CaretMoveActions.PreviousSubword);
		}

		[CommandHandler (MonoDevelop.Ide.Commands.TextEditorCommands.MoveNextSubword)]
		internal void OnMoveNextSubword ()
		{
			RunAction (CaretMoveActions.NextSubword);
		}

		[CommandHandler (MonoDevelop.Ide.Commands.TextEditorCommands.SelectionMovePrevSubword)]
		internal void OnSelectionMovePrevSubword ()
		{
			RunAction (SelectionActions.MovePreviousSubword);
		}

		[CommandHandler (MonoDevelop.Ide.Commands.TextEditorCommands.SelectionMoveNextSubword)]
		internal void OnSelectionMoveNextSubword ()
		{
			RunAction (SelectionActions.MoveNextSubword);
		}

		[CommandHandler (MonoDevelop.Ide.Commands.TextEditorCommands.SelectionMoveUp)]
		internal void OnSelectionMoveUp ()
		{
			RunAction (SelectionActions.MoveUp);
		}

		[CommandHandler (MonoDevelop.Ide.Commands.TextEditorCommands.SelectionMoveDown)]
		internal void OnSelectionMoveDown ()
		{
			RunAction (SelectionActions.MoveDown);
		}

		[CommandHandler (MonoDevelop.Ide.Commands.TextEditorCommands.SelectionMoveHome)]
		internal void OnSelectionMoveHome ()
		{
			RunAction (SelectionActions.MoveLineHome);
		}

		[CommandHandler (MonoDevelop.Ide.Commands.TextEditorCommands.SelectionMoveEnd)]
		internal void OnSelectionMoveEnd ()
		{
			RunAction (SelectionActions.MoveLineEnd);
		}

		[CommandHandler (MonoDevelop.Ide.Commands.TextEditorCommands.SelectionMoveToDocumentStart)]
		internal void OnSelectionMoveToDocumentStart ()
		{
			RunAction (SelectionActions.MoveToDocumentStart);
		}

		[CommandHandler (MonoDevelop.Ide.Commands.TextEditorCommands.ExpandSelectionToLine)]
		internal void OnExpandSelectionToLine ()
		{
			RunAction (SelectionActions.ExpandSelectionToLine);
		}

		[CommandHandler (MonoDevelop.Ide.Commands.TextEditorCommands.SelectionMoveToDocumentEnd)]
		internal void OnSelectionMoveToDocumentEnd ()
		{
			RunAction (SelectionActions.MoveToDocumentEnd);
		}

		[CommandHandler (MonoDevelop.Ide.Commands.TextEditorCommands.SwitchCaretMode)]
		internal void OnSwitchCaretMode ()
		{
			RunAction (MiscActions.SwitchCaretMode);
		}

		[CommandHandler (MonoDevelop.Ide.Commands.TextEditorCommands.InsertTab)]
		internal void OnInsertTab ()
		{
			RunAction (MiscActions.InsertTab);
		}

		[CommandHandler (MonoDevelop.Ide.Commands.TextEditorCommands.RemoveTab)]
		internal void OnRemoveTab ()
		{
			RunAction (MiscActions.RemoveTab);
		}

		[CommandHandler (MonoDevelop.Ide.Commands.TextEditorCommands.InsertNewLine)]
		internal void OnInsertNewLine ()
		{
			RunAction (MiscActions.InsertNewLine);
		}
		
		[CommandHandler (MonoDevelop.Ide.Commands.TextEditorCommands.InsertNewLineAtEnd)]
		internal void OnInsertNewLineAtEnd ()
		{
			RunAction (MiscActions.InsertNewLineAtEnd);
		}
		
		[CommandHandler (MonoDevelop.Ide.Commands.TextEditorCommands.InsertNewLinePreserveCaretPosition)]
		internal void OnInsertNewLinePreserveCaretPosition ()
		{
			RunAction (MiscActions.InsertNewLinePreserveCaretPosition);
		}
		
		[CommandHandler (MonoDevelop.Ide.Commands.TextEditorCommands.CompleteStatement)]
		internal void OnCompleteStatement ()
		{
			var doc = IdeApp.Workbench.ActiveDocument;
			var generator = CodeGenerator.CreateGenerator (doc);
			if (generator != null) {
				generator.CompleteStatement (doc);
			}
		}
		
		[CommandHandler (MonoDevelop.Ide.Commands.TextEditorCommands.DeletePrevWord)]
		internal void OnDeletePrevWord ()
		{
			RunAction (DeleteActions.PreviousWord);
		}

		[CommandHandler (MonoDevelop.Ide.Commands.TextEditorCommands.DeleteNextWord)]
		internal void OnDeleteNextWord ()
		{
			RunAction (DeleteActions.NextWord);
		}
		
		[CommandHandler (MonoDevelop.Ide.Commands.TextEditorCommands.DeletePrevSubword)]
		internal void OnDeletePrevSubword ()
		{
			RunAction (DeleteActions.PreviousSubword);
		}

		[CommandHandler (MonoDevelop.Ide.Commands.TextEditorCommands.DeleteNextSubword)]
		internal void OnDeleteNextSubword ()
		{
			RunAction (DeleteActions.NextSubword);
		}

		[CommandHandler (MonoDevelop.Ide.Commands.TextEditorCommands.SelectionPageDownAction)]
		internal void OnSelectionPageDownAction ()
		{
			RunAction (SelectionActions.MovePageDown);
		}

		[CommandHandler (MonoDevelop.Ide.Commands.TextEditorCommands.SelectionPageUpAction)]
		internal void OnSelectionPageUpAction ()
		{
			RunAction (SelectionActions.MovePageUp);
		}
		
		[CommandHandler (MonoDevelop.SourceEditor.SourceEditorCommands.PulseCaret)]
		internal void OnPulseCaretCommand ()
		{
			StartCaretPulseAnimation ();
		}
		
		[CommandHandler (MonoDevelop.Ide.Commands.TextEditorCommands.TransposeCharacters)]
		internal void TransposeCharacters ()
		{
			RunAction (MiscActions.TransposeCharacters);
		}

		[CommandHandler (MonoDevelop.Ide.Commands.TextEditorCommands.DuplicateLine)]
		internal void DuplicateLine ()
		{
			RunAction (MiscActions.DuplicateLine);
		}

		[CommandHandler (MonoDevelop.Ide.Commands.TextEditorCommands.RecenterEditor)]
		internal void RecenterEditor ()
		{
			RunAction (MiscActions.RecenterEditor);
		}
		
		[CommandHandler (MonoDevelop.Ide.Commands.EditCommands.JoinWithNextLine)]
		internal void JoinLines ()
		{
			using (var undo = Document.OpenUndoGroup ()) {
				RunAction (Mono.TextEditor.Vi.ViActions.Join);
			}
		}
		
#endregion
		
	}
}
