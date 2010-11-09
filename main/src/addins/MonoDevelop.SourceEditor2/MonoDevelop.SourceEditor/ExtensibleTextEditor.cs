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
using MonoDevelop.Projects.Dom;
using MonoDevelop.Projects.Dom.Parser;
using MonoDevelop.Ide.CodeCompletion;
using MonoDevelop.Projects.CodeGeneration;
using MonoDevelop.Components.Commands;
using Mono.TextEditor.Highlighting;
using MonoDevelop.Ide.CodeTemplates;
using Mono.Addins;
using MonoDevelop.Projects.Text;
using MonoDevelop.Ide;

namespace MonoDevelop.SourceEditor
{
	public class ExtensibleTextEditor : Mono.TextEditor.TextEditor
	{
		internal object MemoryProbe = Counters.EditorsInMemory.CreateMemoryProbe ();
		
		SourceEditorView view;
		
		Cairo.Point menuPopupLocation;
		
		public ITextEditorExtension Extension {
			get;
			set;
		}
		
		public new ISourceEditorOptions Options {
			get { return (ISourceEditorOptions)base.Options; }
		}
		
		public ExtensibleTextEditor (SourceEditorView view, ISourceEditorOptions options, Mono.TextEditor.Document doc) : base(doc, options)
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
			
			Document.TextReplaced += delegate(object sender, ReplaceEventArgs args) {
				if (Extension != null) {
					try {
						Extension.TextChanged (args.Offset, args.Offset + Math.Max (args.Count, args.Value != null ? args.Value.Length : 0));
					} catch (Exception ex) {
						ReportExtensionError (ex);
					}
				}
			};
			
			UpdateEditMode ();
			this.GetTextEditorData ().Paste += HandleTextPaste;
			
			this.ButtonPressEvent += OnPopupMenu;

			AddinManager.AddExtensionNodeHandler ("MonoDevelop/SourceEditor2/TooltipProviders", OnTooltipProviderChanged);
		}
		
		void HandleSkipCharsOnReplace (object sender, ReplaceEventArgs args)
		{
			for (int i = 0; i < skipChars.Count; i++) {
				SkipChar sc = skipChars[i];
				if (args.Offset < sc.Start || args.Offset > sc.Offset) {
					skipChars.RemoveAt (i);
					i--;
					continue;
				}
				if (args.Offset <= sc.Offset) {
					sc.Offset -= args.Count;
					if (!string.IsNullOrEmpty (args.Value)) {
						sc.Offset += args.Value.Length;
					}
				}
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

		protected override void OnDestroyed ()
		{
			view = null;
			this.ButtonPressEvent -= OnPopupMenu;
			AddinManager.RemoveExtensionNodeHandler  ("MonoDevelop/SourceEditor2/TooltipProviders", OnTooltipProviderChanged);
			base.OnDestroyed ();
		}
		
		void OnTooltipProviderChanged (object s, ExtensionNodeEventArgs a)
		{
			if (a.Change == ExtensionChange.Add) {
				TooltipProviders.Add ((ITooltipProvider) a.ExtensionObject);
			} else {
				TooltipProviders.Remove ((ITooltipProvider) a.ExtensionObject);
			}
		}

		void OnPopupMenu (object sender, Gtk.ButtonPressEventArgs args)
		{
			if (args.Event.Button == 3) {
				int textEditorXOffset = (int)args.Event.X - (int)this.TextViewMargin.XOffset;
				if (textEditorXOffset < 0)
					return;
				this.menuPopupLocation = new Cairo.Point ((int)args.Event.X, (int)args.Event.Y);
				DocumentLocation loc= PointToLocation (textEditorXOffset, (int)args.Event.Y);
				if (!this.IsSomethingSelected || !this.SelectionRange.Contains (Document.LocationToOffset (loc)))
					Caret.Location = loc;
				
				this.ShowPopup ();
				base.ResetMouseState ();
			}
		}
		
		public void FireOptionsChange ()
		{
			this.OptionsChanged (null, null);
		}
		
		protected override void OptionsChanged (object sender, EventArgs args)
		{
			if (view.Control != null) {
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
				// Handle keyboard menu popup
				if (evnt.Key == Gdk.Key.Menu || (evnt.Key == Gdk.Key.F10 && (evnt.State & Gdk.ModifierType.ShiftMask) == Gdk.ModifierType.ShiftMask)) {
					this.menuPopupLocation = LocationToPoint (this.Caret.Location);
					this.menuPopupLocation.Y += (int)this.TextViewMargin.LineHeight;
					this.ShowPopup ();
					return true;
				}
				
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
				return from p in GetTextWithoutCommentsAndStrings (Document, 0, Document.Length) select p.Key;
			}
		}
		
		static IEnumerable<KeyValuePair <char, int>> GetTextWithoutCommentsAndStrings (Mono.TextEditor.Document doc, int start, int end) 
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
						} else  if (!isInString && !isInChar && pos + 1 < doc.Length) {
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
		
		class SkipChar {
			public int Start { get; set; }
			public int Offset { get; set; }
			public char Char  { get; set; }
			
			public override string ToString ()
			{
				return string.Format ("[SkipChar: Start={0}, Offset={1}, Char={2}]", Start, Offset, Char);
			}
		}
		
		List<SkipChar> skipChars = new List<SkipChar> ();
		void SetInsertionChar (int offset, char ch)
		{
			skipChars.Add (new SkipChar () {
				Start = offset - 1,
				Offset = offset,
				Char = ch
			});
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
			LineSegment line = Document.GetLine (Caret.Line);
			if (line == null)
				return true;
			bool inChar = false;
			bool inComment = false;
			bool inString = false;
//			string escape = "\"";
			var stack = line.StartSpan.Clone();
			Mono.TextEditor.Highlighting.SyntaxModeService.ScanSpans (Document, Document.SyntaxMode, Document.SyntaxMode, stack, line.Offset, Caret.Offset);
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
			Document.BeginAtomicUndo ();

			// insert template when space is typed (currently disabled - it's annoying).
			bool templateInserted = false;
			//!inStringOrComment && (key == Gdk.Key.space) && DoInsertTemplate ();
			bool returnBetweenBraces = key == Gdk.Key.Return && (state & (Gdk.ModifierType.ControlMask | Gdk.ModifierType.ShiftMask)) == Gdk.ModifierType.None && Caret.Offset > 0 && Caret.Offset < Document.Length && Document.GetCharAt (Caret.Offset - 1) == '{' && Document.GetCharAt (Caret.Offset) == '}' && !inStringOrComment;
//			int initialOffset = Caret.Offset;
			const string openBrackets = "{[('\"";
			const string closingBrackets = "}])'\"";
			int braceIndex = openBrackets.IndexOf ((char)ch);
			SkipChar skipChar = skipChars.Find (sc => sc.Char == (char)ch && sc.Offset == Caret.Offset);
			

			// special handling for escape chars inside ' and "
			if (Caret.Offset > 0) {
				char charBefore = Document.GetCharAt (Caret.Offset - 1);
				if (inStringOrComment && (ch == '"' || (inChar && ch == '\'')) && charBefore == '\\')
					skipChar = null;
			}
			char insertionChar = '\0';
			if (skipChar == null && Options.AutoInsertMatchingBracket && braceIndex >= 0) {
				if (!inStringOrComment) {
					char closingBrace = closingBrackets[braceIndex];
					char openingBrace = openBrackets[braceIndex];

					int count = 0;
					foreach (char curCh in TextWithoutCommentsAndStrings) {
						if (curCh == openingBrace) {
							count++;
						} else if (curCh == closingBrace) {
							count--;
						}
					}

					if (count >= 0) {
						GetTextEditorData ().EnsureCaretIsNotVirtual ();
						
						int offset = Caret.Offset;
						insertionChar = closingBrace;
						Insert (offset, closingBrace.ToString ());
						Caret.Offset = offset;
					}
				} else {
					char charBefore = Document.GetCharAt (Caret.Offset - 1);
					if (!inString && !inComment && !inChar && ch == '"' && charBefore != '\\') {
						GetTextEditorData ().EnsureCaretIsNotVirtual ();
						insertionChar = '"';
						int offset = Caret.Offset;
						Insert (Caret.Offset, "\"");
						Caret.Offset = offset;
					}
				}
			}
			
			//Console.WriteLine (Caret.Offset + "/" + insOff);
			if (skipChar != null) {
				Caret.Offset++;
				skipChars.Remove (skipChar);
			} else {
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
			}
			if (insertionChar != '\0')
				SetInsertionChar (Caret.Offset, insertionChar);
			
			if (templateInserted) {
				Document.EndAtomicUndo ();
				return true;
			}
				
			Document.EndAtomicUndo ();
			return result;
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
			LineSegment line = Document.GetLine (location.Line);
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
		
		public ProjectDom ProjectDom {
			get {
				MonoDevelop.Ide.Gui.Document doc = IdeApp.Workbench.ActiveDocument;
				if (doc != null) 
					return doc.Dom;
				return null;
			}
		}
		
		int           oldOffset = -1;
		ResolveResult resolveResult = null;
		public ResolveResult GetLanguageItem (int offset)
		{
			
			// we'll cache old results.
			if (offset == oldOffset)
				return this.resolveResult;
			oldOffset = offset;
			
			if (textEditorResolverProvider != null) {
				this.resolveResult = textEditorResolverProvider.GetLanguageItem (this.ProjectDom, GetTextEditorData (), offset);
			} else {
				this.resolveResult = null;
			}
			
			return this.resolveResult;
		}
		
		public CodeTemplateContext GetTemplateContext ()
		{
			if (IsSomethingSelected) {
				string fileName = view.ContentName ?? view.UntitledName;
				IParser parser = ProjectDomService.GetParser (fileName);
				if (parser == null)
					return CodeTemplateContext.Standard;

				IExpressionFinder expressionFinder = parser.CreateExpressionFinder (ProjectDom);
				if (expressionFinder == null) 
					return CodeTemplateContext.Standard;
				if (expressionFinder.IsExpression (Document.GetTextAt (SelectionRange)))
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
				this.resolveResult = textEditorResolverProvider.GetLanguageItem (this.ProjectDom, GetTextEditorData (), offset, expression);
			} else {
				this.resolveResult = null;
			}
	
			return this.resolveResult;
		}

		public string GetExpression (int offset)
		{
			string fileName = View.ContentName;
			if (fileName == null)
				fileName = View.UntitledName;
			
			IExpressionFinder expressionFinder = ProjectDomService.GetExpressionFinder (fileName);
			string expression = expressionFinder == null ? GetExpressionBeforeOffset (offset) : expressionFinder.FindFullExpression (Document.Text, offset).Expression;
			
			if (expression == null)
				return string.Empty;
			else
				return expression.Trim ();
		}
		
		string GetExpressionBeforeOffset (int offset)
		{
			int start = offset;
			while (start > 0 && IsIdChar (Document.GetCharAt (start)))
				start--;
			while (offset < Document.Length && IsIdChar (Document.GetCharAt (offset)))
				offset++;
			start++;
			if (offset - start > 0 && start < Document.Length)
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
			ParameterInformationWindowManager.HideWindow ();
			return base.OnFocusOutEvent (evnt); 
		}
		
		void ShowPopup ()
		{
			HideTooltip ();
			CommandEntrySet cset = IdeApp.CommandService.CreateCommandEntrySet ("/MonoDevelop/SourceEditor2/ContextMenu/Editor");
			Gtk.Menu menu = IdeApp.CommandService.CreateMenu (cset);
			menu.Append (new SeparatorMenuItem ());
			menu.Append (CreateInputMethodMenuItem (GettextCatalog.GetString ("_Input Methods")));
			menu.Destroyed += delegate {
				this.QueueDraw ();
			};
			
			menu.Popup (null, null, new Gtk.MenuPositionFunc (PositionPopupMenu), 0, Gtk.Global.CurrentEventTime);
		}
		
		void PositionPopupMenu (Menu menu, out int x, out int y, out bool pushIn)
		{
			this.GdkWindow.GetOrigin (out x, out y);
			x += this.menuPopupLocation.X;
			y += this.menuPopupLocation.Y;
			Requisition request = menu.SizeRequest ();
			Gdk.Rectangle geometry = Screen.GetMonitorGeometry (Screen.GetMonitorAtPoint (x, y));
			
			y = Math.Max (geometry.Top, Math.Min (y, geometry.Bottom - request.Height));
			x = Math.Max (geometry.Left, Math.Min (x, geometry.Right - request.Width));
			pushIn = true;
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
		
		void HandleTextPaste (int insertionOffset, string text)
		{
			if (PropertyService.Get ("OnTheFlyFormatting", false)) {
				Formatter prettyPrinter = TextFileService.GetFormatter (Document.MimeType);
				if (prettyPrinter != null && prettyPrinter.SupportsOnTheFlyFormatting && ProjectDom != null && text != null) {
					try {
						string newText = prettyPrinter.FormatText (ProjectDom.Project.Policies, Document.MimeType, Document.Text, insertionOffset, insertionOffset + text.Length);
						if (!string.IsNullOrEmpty (newText)) {
							Replace (insertionOffset, text.Length, newText);
							Caret.Offset = insertionOffset + newText.Length;
						}
					} catch (Exception e) {
						Console.WriteLine (e);
					}
				}
			}
		}
		
		internal void InsertTemplate (CodeTemplate template, MonoDevelop.Ide.Gui.Document document)
		{
			Document.BeginAtomicUndo ();
			var result = template.InsertTemplateContents (document);
			TextLinkEditMode tle = new TextLinkEditMode (this, 
			                                             result.InsertPosition,
			                                             result.TextLinks);
			
			if (PropertyService.Get ("OnTheFlyFormatting", false)) {
				Formatter prettyPrinter = TextFileService.GetFormatter (Document.MimeType);
				if (prettyPrinter != null && prettyPrinter.SupportsOnTheFlyFormatting) {
					int endOffset = result.InsertPosition + result.Code.Length;
					string text = prettyPrinter.FormatText (document.Project.Policies, Document.MimeType, Document.Text, result.InsertPosition, endOffset);
					string oldText = Document.GetTextAt (result.InsertPosition, result.Code.Length);
					//					Console.WriteLine (result.InsertPosition);
					//					Console.WriteLine ("old:" + oldText);
					//					Console.WriteLine ("new:" + text);
					Replace (result.InsertPosition, result.Code.Length, text);
					Caret.Offset = result.InsertPosition + TranslateOffset (oldText, text, Caret.Offset - result.InsertPosition);
					foreach (TextLink textLink in tle.Links) {
						foreach (ISegment segment in textLink.Links) {
							segment.Offset = TranslateOffset (oldText, text, segment.Offset);
						}
					}
				}
			}
			
			if (tle.ShouldStartTextLinkMode) {
				tle.OldMode = CurrentMode;
				tle.StartMode ();
				CurrentMode = tle;
			}
			Document.EndAtomicUndo ();
		}
		
		static int TranslateOffset (string baseInput, string formattedInput, int offset)
		{
			int i = 0;
			int j = 0;
			while (i < baseInput.Length && j < formattedInput.Length && i < offset) {
				char ch1 = baseInput[i];
				char ch2 = formattedInput[j];
				bool ch1IsWs = Char.IsWhiteSpace (ch1);
				bool ch2IsWs = Char.IsWhiteSpace (ch2);
				if (ch1 == ch2 || ch1IsWs && ch2IsWs) {
					i++;
					j++;
				} else if (!ch1IsWs && ch2IsWs) {
					j++;
				} else if (ch1IsWs && !ch2IsWs) {
					i++;
				} else {
					return -1;
				}
			}
			return j;
		}
		
		protected override void HAdjustmentValueChanged ()
		{
			base.HAdjustmentValueChanged ();
			if (!isInKeyStroke) {
				CompletionWindowManager.HideWindow ();
				ParameterInformationWindowManager.HideWindow ();
			}
		}
		
		protected override void VAdjustmentValueChanged ()
		{
			base.VAdjustmentValueChanged ();
			CompletionWindowManager.HideWindow ();
			ParameterInformationWindowManager.HideWindow ();
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
			CodeRefactorer refactorer = IdeApp.Workspace.GetCodeRefactorer (IdeApp.ProjectOperations.CurrentSelectedSolution);
			DomLocation caretLocation = refactorer.CompleteStatement (ProjectDom, Document.FileName, new DomLocation (Caret.Line, Caret.Column));
			Caret.Line   = caretLocation.Line;
			Caret.Column = caretLocation.Column;
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
		
		[CommandHandler (MonoDevelop.SourceEditor.SourceEditorCommands.ToggleCodeFocus)]
		internal void OnToggleCodeFocus ()
		{
			foldMarkerMargin.IsInCodeFocusMode = !foldMarkerMargin.IsInCodeFocusMode;
		}
		
		[CommandHandler (MonoDevelop.Ide.Commands.TextEditorCommands.TransposeCharacters)]
		internal void TransposeCharacters ()
		{
			RunAction (MiscActions.TransposeCharacters);
		}
		
		[CommandHandler (MonoDevelop.Ide.Commands.TextEditorCommands.RecenterEditor)]
		internal void RecenterEditor ()
		{
			RunAction (MiscActions.RecenterEditor);
		}
		
		[CommandHandler (MonoDevelop.Ide.Commands.EditCommands.JoinWithNextLine)]
		internal void JoinLines ()
		{
			try {
				Document.BeginAtomicUndo ();
				RunAction (Mono.TextEditor.Vi.ViActions.Join);
			} finally {
				Document.EndAtomicUndo ();
			}
		}
		
#endregion
		
	}
}
