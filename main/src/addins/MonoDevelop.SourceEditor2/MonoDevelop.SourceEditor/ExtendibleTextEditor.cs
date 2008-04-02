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
using System.IO;
using System.Text;

using Gtk;

using Mono.TextEditor;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Gui.Content;
using MonoDevelop.Projects.Parser;
using MonoDevelop.Projects.Gui.Completion;
using MonoDevelop.Components.Commands;
using Mono.TextEditor.Highlighting;
using MonoDevelop.Ide.CodeTemplates;

namespace MonoDevelop.SourceEditor
{
	public class ExtendibleTextEditor : Mono.TextEditor.TextEditor
	{
		ITextEditorExtension extension = null;
		LanguageItemWindow languageItemWindow;
		SourceEditorView view;
		
		const int LanguageItemTipTimer = 800;
		ILanguageItem tipItem;
		bool showTipScheduled;
		int langTipX, langTipY;
		uint tipTimeoutId;
		Gdk.Point menuPopupLocation;
		
		public ITextEditorExtension Extension {
			get {
				return extension;
			}
			set {
				extension = value;
			}
		}
		
		public ExtendibleTextEditor (SourceEditorView view, Mono.TextEditor.Document doc) : base (doc)
		{
			Initialize (view);
		}
		
		public ExtendibleTextEditor (SourceEditorView view)
		{
			Initialize (view);
		}
		
		void Initialize (SourceEditorView view)
		{
			this.view = view;
			Document.TextReplaced += delegate {
				this.HideLanguageItemWindow ();
			};
			Caret.PositionChanged += delegate {
				this.HideLanguageItemWindow ();
				if (extension != null)
					extension.CursorPositionChanged ();
			};
			Document.TextReplaced += delegate (object sender, ReplaceEventArgs args) {
				if (extension != null)
					extension.TextChanged (args.Offset, args.Offset + Math.Max (args.Count, args.Value != null ? args.Value.Length : 0));
			};
			keyBindings [GetKeyCode (Gdk.Key.Tab)] = new TabAction (this);
			keyBindings [GetKeyCode (Gdk.Key.BackSpace)] = new AdvancedBackspaceAction ();
			
			this.ButtonPressEvent += delegate(object sender, Gtk.ButtonPressEventArgs args) {
				if (args.Event.Button == 3) {
					int textEditorXOffset = (int)args.Event.X - this.TextViewMargin.XOffset;
					if (textEditorXOffset < 0)
						return;
					this.menuPopupLocation = new Gdk.Point ((int)args.Event.X, (int)args.Event.Y);
					DocumentLocation loc= this.TextViewMargin.VisualToDocumentLocation (textEditorXOffset, (int)args.Event.Y);
					if (!this.IsSomethingSelected || !this.SelectionRange.Contains (Document.LocationToOffset (loc)))
						Caret.Location = loc;
					
					this.ShowPopup ();
					base.ResetMouseState ();
				}
			};
			this.Realized += delegate {
				FireOptionsChange ();
			};
		}
		public void FireOptionsChange ()
		{
			this.OptionsChanged (null, null);
		}
		
		protected override void OptionsChanged (object sender, EventArgs args)
		{
			if (view.Control != null) {
				((SourceEditorWidget)view.Control).IsClassBrowserVisible = SourceEditorOptions.Options.EnableQuickFinder;
				if (!SourceEditorOptions.Options.ShowFoldMargin)
					this.Document.ClearFoldSegments ();
			}
			base.OptionsChanged (sender, args);
		}
		
		protected override bool OnKeyPressEvent (Gdk.EventKey evnt)
		{
			bool result = true;
			char ch = (char)evnt.Key;
			
			// Handle keyboard menu popup
			if (evnt.Key == Gdk.Key.Menu || (evnt.Key == Gdk.Key.F10 && (evnt.State & Gdk.ModifierType.ShiftMask) == Gdk.ModifierType.ShiftMask)) {
				this.menuPopupLocation = this.TextViewMargin.LocationToDisplayCoordinates (this.Caret.Location);
				this.menuPopupLocation.Y += this.TextViewMargin.LineHeight;
				this.ShowPopup ();
				return true;
			}
			
			// Handle keyboard toolip popup
			if ((evnt.Key == Gdk.Key.F1 && (evnt.State & Gdk.ModifierType.ControlMask) == Gdk.ModifierType.ControlMask)) {
				Gdk.Point p = this.TextViewMargin.LocationToDisplayCoordinates (this.Caret.Location);
				this.mx = p.X;
				this.my = p.Y;
				this.ShowTooltip ();
				return true;
			}
			if (evnt.Key == Gdk.Key.Escape) {
				bool b = extension.KeyPress (evnt.Key, evnt.State);
				if (b) {
					view.SourceEditorWidget.RemoveSearchWidget ();
					return true;
				}
				return false;
			}
			
			bool inStringOrComment = false;
			if (SourceEditorOptions.Options.AutoInsertMatchingBracket && (ch == '{' || ch == '[' || ch == '(' || ch == '"' || ch == '\'' )) {
				LineSegment line = Document.GetLine (Caret.Line);
				Stack<Span> stack = line.StartSpan != null ? new Stack<Span> (line.StartSpan) : new Stack<Span> ();
				SyntaxModeService.ScanSpans (Document, Document.SyntaxMode, stack, line.Offset, Caret.Offset);
				foreach (Span span in stack) {
					if (span.Color == "comment" || span.Color == "literal") {
						inStringOrComment = true;
						break;
					}
				}
			}
			Document.BeginAtomicUndo ();
			if (extension != null) {
				if (extension.KeyPress (evnt.Key, evnt.State)) 
					result = base.OnKeyPressEvent (evnt);
			} else {
				result = base.OnKeyPressEvent (evnt);
			}
			
			if (SourceEditorOptions.Options.AutoInsertTemplates && IsTemplateKnown ())
				DoInsertTemplate ();
			if (SourceEditorOptions.Options.AutoInsertMatchingBracket && !inStringOrComment) {
				switch (ch) {
				case '{':
					if (extension != null) {
						int offset = Caret.Offset;
						extension.KeyPress (Gdk.Key.Return, Gdk.ModifierType.None);
						extension.KeyPress ((Gdk.Key)'}', Gdk.ModifierType.None);
						Caret.Offset = offset;
						extension.KeyPress (Gdk.Key.Return, Gdk.ModifierType.None);
					} else {
						result = base.OnKeyPressEvent (evnt);
						base.SimulateKeyPress (Gdk.Key.Return, Gdk.ModifierType.None);
						Document.Insert (Caret.Offset, "}");
					}
					break;
				case '[':
					Document.Insert (Caret.Offset, new StringBuilder ("]"));
					break;
				case '(':
					Document.Insert (Caret.Offset, new StringBuilder (")"));
					break;
				case '\'':
					Document.Insert (Caret.Offset, new StringBuilder ("'"));
					break;
				case '"':
					Document.Insert (Caret.Offset, new StringBuilder ("\""));
					break;
				}
			}
			Document.EndAtomicUndo ();
			return result;
		}
		
		double mx, my;
		protected override bool OnMotionNotifyEvent (Gdk.EventMotion evnt)
		{
			mx = evnt.X - textViewMargin.XOffset;
			my = evnt.Y;
			bool result = base.OnMotionNotifyEvent (evnt);
			UpdateLanguageItemWindow ();
			return result;
		}
		
		void UpdateLanguageItemWindow ()
		{
			if (languageItemWindow != null) {
				// Tip already being shown. Update it.
				ShowTooltip ();
			}
			else if (showTipScheduled) {
				// Tip already scheduled. Reset the timer.
				GLib.Source.Remove (tipTimeoutId);
				tipTimeoutId = GLib.Timeout.Add (LanguageItemTipTimer, ShowTooltip);
			}
			else {
				// Start a timer to show the tip
				showTipScheduled = true;
				tipTimeoutId = GLib.Timeout.Add (LanguageItemTipTimer, ShowTooltip);
			}
		}
		
		bool ShowTooltip ()
		{
			string errorInfo;

			showTipScheduled = false;
			int xloc = (int)mx;
			int yloc = (int)my;
			ILanguageItem item = GetLanguageItem (Document.LocationToOffset (base.VisualToDocumentLocation ((int)mx, (int)my)));
			if (item != null) {
				// Tip already being shown for this language item?
				if (languageItemWindow != null && tipItem != null && tipItem.Equals (item))
					return false;
				
				langTipX = xloc;
				langTipY = yloc;
				tipItem = item;

				HideLanguageItemWindow ();
				
				IParserContext pctx = view.GetParserContext ();
				if (pctx == null)
					return false;

				DoShowTooltip (new LanguageItemWindow (tipItem, pctx, view.GetAmbience (), 
				                                        GetErrorInformationAt (Caret.Offset)), langTipX, langTipY);
				
				
			} else if (!string.IsNullOrEmpty ((errorInfo = GetErrorInformationAt(Caret.Offset)))) {
				// Error tooltip already shown
				if (languageItemWindow != null /*&& tiItem == ti.Line*/)
					return false;
				//tiItem = ti.Line;
				
				HideLanguageItemWindow ();
				DoShowTooltip (new LanguageItemWindow (null, null, null, errorInfo), xloc, yloc);
			} else
				HideLanguageItemWindow ();
			
			return false;
		}
		
		void DoShowTooltip (LanguageItemWindow liw, int xloc, int yloc)
		{
			languageItemWindow = liw;
			
			int ox = 0, oy = 0;
			this.GdkWindow.GetOrigin (out ox, out oy);
			int w = languageItemWindow.Child.SizeRequest ().Width;
			languageItemWindow.Move (xloc + ox - (w/2), yloc + oy + 20);
			languageItemWindow.ShowAll ();
		}
		
		protected override void OnUnrealized ()
		{
			if (showTipScheduled) {
				GLib.Source.Remove (tipTimeoutId);
				showTipScheduled = false;
			}
			base.OnUnrealized ();
		}
		string GetErrorInformationAt (int offset)
		{
//			ErrorInfo info;
//			if (errors.TryGetValue (iter.Line, out info))
//				return "<b>" + GettextCatalog.GetString ("Parser Error:") + "</b> " + info.Message;
//			else
				return null;
		}
		
		public ILanguageItem GetLanguageItem (int offset)
		{
			string txt = this.Document.Text;
			string fileName = view.ContentName;
			if (fileName == null)
				fileName = view.UntitledName;
			
			IParserContext ctx = view.GetParserContext ();
			if (ctx == null)
				return null;
			
			IExpressionFinder expressionFinder = null;
			if (fileName != null)
				expressionFinder = ctx.GetExpressionFinder (fileName);
			
			string expression = expressionFinder == null ? TextUtilities.GetExpressionBeforeOffset (view, offset) : expressionFinder.FindFullExpression (txt, offset).Expression;
			if (expression == null)
				return null;
			
			int lineNumber = this.Document.OffsetToLineNumber (offset);
			LineSegment line = this.Document.GetLine (lineNumber);
			
			return ctx.ResolveIdentifier (expression, lineNumber + 1, line.Offset + 1, fileName, txt);
		}
		

		protected override bool OnLeaveNotifyEvent (Gdk.EventCrossing evnt)		
		{
			HideLanguageItemWindow ();
			return base.OnLeaveNotifyEvent (evnt);
		}
		
		protected override bool OnFocusOutEvent (Gdk.EventFocus evnt)
		{
			CompletionWindowManager.HideWindow ();
			ParameterInformationWindowManager.HideWindow ();
			HideLanguageItemWindow ();
			return base.OnFocusOutEvent (evnt); 
		}
		
		protected override bool OnScrollEvent (Gdk.EventScroll evnt)
		{
			CompletionWindowManager.HideWindow ();
			ParameterInformationWindowManager.HideWindow ();
			HideLanguageItemWindow ();
			return base.OnScrollEvent (evnt);
		}
		
		public void HideLanguageItemWindow ()
		{
			if (showTipScheduled) {
				GLib.Source.Remove (tipTimeoutId);
				showTipScheduled = false;
			}
			if (languageItemWindow != null) {
				languageItemWindow.Destroy ();
				languageItemWindow = null;
			}
		}

		void ShowPopup ()
		{
			HideLanguageItemWindow ();
			CommandEntrySet cset = IdeApp.CommandService.CreateCommandEntrySet ("/MonoDevelop/SourceEditor2/ContextMenu/Editor");
			Gtk.Menu menu = IdeApp.CommandService.CreateMenu (cset);
			
			menu.Destroyed += delegate {
				this.QueueDraw ();
			};
			//menu.Popup (null, null, new Gtk.MenuPositionFunc (PositionPopupMenu), 3, Gtk.Global.CurrentEventTime);
			//menu.Hidden += delegate {
//					menu.Destroy ();
//				};
			
			IdeApp.CommandService.ShowContextMenu (menu);
		}
		
/*		void PositionPopupMenu (Menu menu, out int x, out int y, out bool pushIn) 
		{
			this.GdkWindow.GetOrigin (out x, out y);
			x += this.menuPopupLocation.X;
			y += this.menuPopupLocation.Y;
			pushIn = true;
		}*/
		
//		protected override void OnPopulatePopup (Menu menu)
//		{
//			
//			CommandEntrySet cset = IdeApp.CommandService.CreateCommandEntrySet ("");
//			if (cset.Count > 0) {
//				cset.AddItem (Command.Separator);
//				IdeApp.CommandService.InsertOptions (menu, cset, 0);
//			}
//			base.OnPopulatePopup (menu);
//		}
//		
		
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
		
		public int DeleteWordBeforeCaret ()
		{
			int offset = this.Caret.Offset;
			int start  = FindPrevWordStart (offset);
			Document.Remove (start, offset - start);
			return start;
		}

		public string GetLeadingWhiteSpace (int lineNr)
		{
			LineSegment line = Document.GetLine (lineNr);
			int index = 0;
			while (index < line.EditableLength && Char.IsWhiteSpace (Document.GetCharAt (line.Offset + index)))
				index++;
 	   		return index > 0 ? Document.GetTextAt (line.Offset, index) : "";
		}

		public bool IsTemplateKnown ()
		{
			string word = GetWordBeforeCaret ();
			CodeTemplateGroup templateGroup = CodeTemplateService.GetTemplateGroupPerFilename (this.view.ContentName);
			if (String.IsNullOrEmpty (word) || templateGroup == null) 
				return false;
			
			bool result = false;
			foreach (CodeTemplate template in templateGroup.Templates) {
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
			CodeTemplateGroup templateGroup = CodeTemplateService.GetTemplateGroupPerFilename (this.view.ContentName);
			if (String.IsNullOrEmpty (word) || templateGroup == null) 
				return false;
			
			foreach (CodeTemplate template in templateGroup.Templates) {
				if (template.Shortcut == word) {
					InsertTemplate (template);
					return true;
				}
			}
			return false;
		}
		
		public void InsertTemplate (CodeTemplate template)
		{
			int offset = Caret.Offset;
			string word = GetWordBeforeCaret ().Trim ();
			if (word.Length > 0)
				offset = DeleteWordBeforeCaret ();
			
			string leadingWhiteSpace = GetLeadingWhiteSpace (Caret.Line);

			int finalCaretOffset = offset + template.Text.Length;
			StringBuilder builder = new StringBuilder ();
			for (int i = 0; i < template.Text.Length; ++i) {
				switch (template.Text[i]) {
				case '|':
					finalCaretOffset = i + offset;
					break;
				case '\r':
					break;
				case '\n':
					builder.Append (Environment.NewLine);
					builder.Append (leadingWhiteSpace);
					break;
				default:
					builder.Append (template.Text[i]);
					break;
				}
			}
			
//			if (endLine > beginLine) {
//				IndentLines (beginLine+1, endLine, leadingWhiteSpace);
//			}
			Document.Insert (offset, builder);
			Caret.Offset = finalCaretOffset;
		}		
#endregion
		
#region Key bindings

		[CommandHandler (MonoDevelop.Ide.Commands.TextEditorCommands.LineEnd)]
		internal void OnLineEnd ()
		{
			RunAction (new CaretMoveLeft ());
		}
		
		[CommandHandler (MonoDevelop.Ide.Commands.TextEditorCommands.LineStart)]
		internal void OnLineStart ()
		{
			RunAction (new CaretMoveEnd ());
		}
		
		[CommandHandler (MonoDevelop.Ide.Commands.TextEditorCommands.DeleteLeftChar)]
		internal void OnDeleteLeftChar ()
		{
			RunAction (new BackspaceAction ());
		}
		
		[CommandHandler (MonoDevelop.Ide.Commands.EditCommands.Delete)]
		[CommandHandler (MonoDevelop.Ide.Commands.TextEditorCommands.DeleteRightChar)]
		internal void OnDeleteRightChar ()
		{
			RunAction (new DeleteAction ());
		}
		
		[CommandHandler (MonoDevelop.Ide.Commands.TextEditorCommands.CharLeft)]
		internal void OnCharLeft ()
		{
			RunAction (new CaretMoveLeft ());
		}
		
		[CommandHandler (MonoDevelop.Ide.Commands.TextEditorCommands.CharRight)]
		internal void OnCharRight ()
		{
			RunAction (new CaretMoveRight ());
		}
		
		[CommandHandler (MonoDevelop.Ide.Commands.TextEditorCommands.LineUp)]
		internal void OnLineUp ()
		{
			RunAction (new CaretMoveUp ());
		}
		
		[CommandHandler (MonoDevelop.Ide.Commands.TextEditorCommands.LineDown)]
		internal void OnLineDown ()
		{
			RunAction (new CaretMoveDown ());
		}
		
		[CommandHandler (MonoDevelop.Ide.Commands.TextEditorCommands.DocumentStart)]
		internal void OnDocumentStart ()
		{
			RunAction (new CaretMoveToDocumentStart ());
		}
		
		[CommandHandler (MonoDevelop.Ide.Commands.TextEditorCommands.DocumentEnd)]
		internal void OnDocumentEnd ()
		{
			RunAction (new CaretMoveToDocumentEnd ());
		}
		
		[CommandHandler (MonoDevelop.Ide.Commands.TextEditorCommands.PageUp)]
		internal void OnPageUp ()
		{
			RunAction (new PageUpAction ());
		}
		
		[CommandHandler (MonoDevelop.Ide.Commands.TextEditorCommands.PageDown)]
		internal void OnPageDown ()
		{
			RunAction (new PageDownAction ());
		}
		
		[CommandHandler (MonoDevelop.Ide.Commands.TextEditorCommands.DeleteLine)]
		internal void OnDeleteLine ()
		{
			RunAction (new DeleteCaretLine ());
		}
		
		[CommandHandler (MonoDevelop.Ide.Commands.TextEditorCommands.DeleteToLineEnd)]
		internal void OnDeleteToLineEnd ()
		{
			RunAction (new DeleteCaretLineToEnd ());
		}
		
 		[CommandHandler (MonoDevelop.Ide.Commands.TextEditorCommands.ScrollLineUp)]
		internal void OnScrollLineUp ()
		{
			RunAction (new ScrollUpAction ());
		}
		
		[CommandHandler (MonoDevelop.Ide.Commands.TextEditorCommands.ScrollLineDown)]
		internal void OnScrollLineDown ()
		{
			RunAction (new ScrollDownAction ());
		}

		[CommandHandler (MonoDevelop.Ide.Commands.TextEditorCommands.GotoMatchingBrace)]
		internal void OnGotoMatchingBrace ()
		{
			RunAction (new GotoMatchingBracket ());
		}

		[CommandHandler (MonoDevelop.Ide.Commands.TextEditorCommands.SelectionMoveLeft)]
		internal void OnSelectionMoveLeft ()
		{
			RunAction (new SelectionMoveLeft ());
		}

		[CommandHandler (MonoDevelop.Ide.Commands.TextEditorCommands.SelectionMoveRight)]
		internal void OnSelectionMoveRight ()
		{
			RunAction (new SelectionMoveRight ());
		}

		[CommandHandler (MonoDevelop.Ide.Commands.TextEditorCommands.MovePrevWord)]
		internal void OnMovePrevWord ()
		{
			RunAction (new CaretMovePrevWord ());
		}

		[CommandHandler (MonoDevelop.Ide.Commands.TextEditorCommands.MoveNextWord)]
		internal void OnMoveNextWord ()
		{
			RunAction (new CaretMoveNextWord ());
		}

		[CommandHandler (MonoDevelop.Ide.Commands.TextEditorCommands.SelectionMovePrevWord)]
		internal void OnSelectionMovePrevWord ()
		{
			RunAction (new SelectionMovePrevWord ());
		}

		[CommandHandler (MonoDevelop.Ide.Commands.TextEditorCommands.SelectionMoveNextWord)]
		internal void OnSelectionMoveNextWord ()
		{
			RunAction (new SelectionMoveNextWord ());
		}

		[CommandHandler (MonoDevelop.Ide.Commands.TextEditorCommands.SelectionMoveUp)]
		internal void OnSelectionMoveUp ()
		{
			RunAction (new SelectionMoveUp ());
		}

		[CommandHandler (MonoDevelop.Ide.Commands.TextEditorCommands.SelectionMoveDown)]
		internal void OnSelectionMoveDown ()
		{
			RunAction (new SelectionMoveDown ());
		}

		[CommandHandler (MonoDevelop.Ide.Commands.TextEditorCommands.SelectionMoveHome)]
		internal void OnSelectionMoveHome ()
		{
			RunAction (new SelectionMoveHome ());
		}

		[CommandHandler (MonoDevelop.Ide.Commands.TextEditorCommands.SelectionMoveEnd)]
		internal void OnSelectionMoveEnd ()
		{
			RunAction (new SelectionMoveEnd ());
		}

		[CommandHandler (MonoDevelop.Ide.Commands.TextEditorCommands.SelectionMoveToDocumentStart)]
		internal void OnSelectionMoveToDocumentStart ()
		{
			RunAction (new SelectionMoveToDocumentStart ());
		}

		[CommandHandler (MonoDevelop.Ide.Commands.TextEditorCommands.SelectionMoveToDocumentEnd)]
		internal void OnSelectionMoveToDocumentEnd ()
		{
			RunAction (new SelectionMoveToDocumentEnd ());
		}

		[CommandHandler (MonoDevelop.Ide.Commands.TextEditorCommands.SwitchCaretMode)]
		internal void OnSwitchCaretMode ()
		{
			RunAction (new SwitchCaretModeAction ());
		}

		[CommandHandler (MonoDevelop.Ide.Commands.TextEditorCommands.InsertTab)]
		internal void OnInsertTab ()
		{
			RunAction (new InsertTab ());
		}

		[CommandHandler (MonoDevelop.Ide.Commands.TextEditorCommands.RemoveTab)]
		internal void OnRemoveTab ()
		{
			RunAction (new RemoveTab ());
		}

		[CommandHandler (MonoDevelop.Ide.Commands.TextEditorCommands.InsertNewLine)]
		internal void OnInsertNewLine ()
		{
			RunAction (new InsertNewLine ());
		}

		[CommandHandler (MonoDevelop.Ide.Commands.TextEditorCommands.DeletePrevWord)]
		internal void OnDeletePrevWord ()
		{
			RunAction (new DeletePrevWord ());
		}

		[CommandHandler (MonoDevelop.Ide.Commands.TextEditorCommands.DeleteNextWord)]
		internal void OnDeleteNextWord ()
		{
			RunAction (new DeleteNextWord ());
		}

		[CommandHandler (MonoDevelop.Ide.Commands.TextEditorCommands.SelectionPageDownAction)]
		internal void OnSelectionPageDownAction ()
		{
			RunAction (new SelectionPageDownAction ());
		}

		[CommandHandler (MonoDevelop.Ide.Commands.TextEditorCommands.SelectionPageUpAction)]
		internal void OnSelectionPageUpAction ()
		{
			RunAction (new SelectionPageUpAction ());
		}
		
#endregion
		
	}
}
