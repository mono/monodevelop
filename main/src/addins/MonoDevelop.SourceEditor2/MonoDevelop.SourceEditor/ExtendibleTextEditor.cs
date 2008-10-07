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
using System.ComponentModel;

using Gtk;

using Mono.TextEditor;
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Gui.Content;
using MonoDevelop.Projects.Dom;
using MonoDevelop.Projects.Dom.Parser;
using MonoDevelop.Projects.Gui.Completion;
using MonoDevelop.Components.Commands;
using Mono.TextEditor.Highlighting;
using MonoDevelop.Ide.CodeTemplates;
using Mono.Addins;

namespace MonoDevelop.SourceEditor
{
	public class ExtensibleTextEditor : Mono.TextEditor.TextEditor
	{
		ITextEditorExtension extension = null;
		SourceEditorView view;
		Dictionary<int, Error> errors;
		
		Gdk.Point menuPopupLocation;
		
		public ITextEditorExtension Extension {
			get {
				return extension;
			}
			set {
				extension = value;
			}
		}
		
		public ExtensibleTextEditor (SourceEditorView view, Mono.TextEditor.Document doc) : base (doc, null)
		{
			Initialize (view);
		}
		
		public ExtensibleTextEditor (SourceEditorView view)
		{
			Initialize (view);
		}
		
		internal SourceEditorView View {
			get { return view; }
		}

		internal Dictionary<int, Error> Errors {
			get {
				return errors;
			}
			set {
				errors = value;
			}
		}
		
		void Initialize (SourceEditorView view)
		{
			this.view = view;
			Caret.PositionChanged += delegate {
				if (extension != null) {
					try {
						extension.CursorPositionChanged ();
					} catch (Exception ex) {
						ReportExtensionError (ex);
					}
				}
			};
			Document.TextReplaced += delegate (object sender, ReplaceEventArgs args) {
				if (extension != null) {
					try {
						extension.TextChanged (args.Offset, 
						    args.Offset + Math.Max (args.Count, args.Value != null ? args.Value.Length : 0));
					} catch (Exception ex) {
						ReportExtensionError (ex);
					}
				}
			};
			
			bool vi = false;
			if (vi) {
				ViMode viMode = new ViMode ();
				viMode.StatusChanged += delegate (object sender, EventArgs args) {
					IdeApp.Workbench.StatusBar.ShowMessage (((ViMode)sender).Status);
				};
				CurrentMode = viMode;
			} else {
				SimpleEditMode simpleMode = new SimpleEditMode ();
				simpleMode.KeyBindings [EditMode.GetKeyCode (Gdk.Key.Tab)] = new TabAction (this).Action;
				simpleMode.KeyBindings [EditMode.GetKeyCode (Gdk.Key.BackSpace)] = EditActions.AdvancedBackspace;
				CurrentMode = simpleMode;
			}
			
			this.ButtonPressEvent += OnPopupMenu;

			AddinManager.AddExtensionNodeHandler ("MonoDevelop/SourceEditor2/TooltipProviders", OnTooltipProviderChanged);
		}

		protected override void OnDestroyed ()
		{
			extension = null;
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
		}
		
		public void FireOptionsChange ()
		{
			this.OptionsChanged (null, null);
		}
		
		protected override void OptionsChanged (object sender, EventArgs args)
		{
			if (view.Control != null) {
				((SourceEditorWidget)view.Control).ShowClassBrowser = SourceEditorOptions.Options.EnableQuickFinder;
				if (!SourceEditorOptions.Options.ShowFoldMargin)
					this.Document.ClearFoldSegments ();
			}
			base.OptionsChanged (sender, args);
		}
		
		protected override bool OnKeyPressEvent (Gdk.EventKey evnt)
		{
			// Handle keyboard menu popup
			if (evnt.Key == Gdk.Key.Menu || (evnt.Key == Gdk.Key.F10 && (evnt.State & Gdk.ModifierType.ShiftMask) == Gdk.ModifierType.ShiftMask)) {
				this.menuPopupLocation = this.TextViewMargin.LocationToDisplayCoordinates (this.Caret.Location);
				this.menuPopupLocation.Y += this.TextViewMargin.LineHeight;
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
		}
		
		bool ExtensionKeyPress (Gdk.Key key, char ch, Gdk.ModifierType state)
		{
			try {
				return extension.KeyPress (key, ch, state);
			} catch (Exception ex) {
				ReportExtensionError (ex);
			}
			return false;
		}
		
		void ReportExtensionError (Exception ex) 
		{
			MonoDevelop.Core.LoggingService.LogError ("Error in text editor extension chain", ex);
			MonoDevelop.Core.Gui.MessageService.ShowException (ex, "Error in text editor extension chain");
		}
		
		protected override bool OnIMProcessedKeyPressEvent (Gdk.EventKey evnt, char ch)
		{
			bool result = true;
			if (evnt.Key == Gdk.Key.Escape) {
				bool b;
				if (extension != null)
					b = ExtensionKeyPress (evnt.Key, ch, evnt.State);
				else
					b = base.OnIMProcessedKeyPressEvent (evnt, ch);
				if (b) {
					view.SourceEditorWidget.RemoveSearchWidget ();
					return true;
				}
				return false;
			}
			
			bool inStringOrComment = false;
			bool templateDetected  = SourceEditorOptions.Options.AutoInsertTemplates && IsTemplateKnown ();
			if (SourceEditorOptions.Options.AutoInsertMatchingBracket && (ch == '{' || ch == '[' || ch == '(' || ch == '"' || ch == '\'' ) || templateDetected) {
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
			if (Document == null)
				return true;
			Document.BeginAtomicUndo ();
			if (extension != null) {
				if (ExtensionKeyPress (evnt.Key, ch, evnt.State)) 
					result = base.OnIMProcessedKeyPressEvent (evnt, ch);
			} else {
				result = base.OnIMProcessedKeyPressEvent (evnt, ch);
			}
			
			if (!inStringOrComment && templateDetected)
				DoInsertTemplate ();
			if (SourceEditorOptions.Options.AutoInsertMatchingBracket && !inStringOrComment) {
				switch (ch) {
				case '{':
					if (extension != null) {
						int offset = Caret.Offset;
						ExtensionKeyPress (Gdk.Key.Return, (char)0, Gdk.ModifierType.None);
						ExtensionKeyPress (Gdk.Key.braceright, '}', Gdk.ModifierType.None);
						Caret.Offset = offset;
						ExtensionKeyPress (Gdk.Key.Return, (char)0, Gdk.ModifierType.None);
					} else {
						result = base.OnIMProcessedKeyPressEvent (evnt, ch);
						base.SimulateKeyPress (Gdk.Key.Return, 0, Gdk.ModifierType.None);
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
		
		internal string GetErrorInformationAt (int offset)
		{
			Error error;
			DocumentLocation location = Document.OffsetToLocation (offset);
			if (errors.TryGetValue (location.Line, out error))
				return "<b>" + GettextCatalog.GetString ("Parser Error:") + " </b> " + error.info.Message;
			else
				return null;
		}
		
		public ProjectDom ProjectDom {
			get {
				ProjectDom result = null;
				MonoDevelop.Ide.Gui.Document doc = IdeApp.Workbench.ActiveDocument;
				if (doc != null || doc.Project == null) 
					result = ProjectDomService.GetProjectDom (doc.Project);
				
				if (result == null)
					result = ProjectDomService.GetFileDom (View.ContentName);
				
				return result;
			}
		}
		
		int           oldOffset = -1;
		ResolveResult resolveResult = null;
		public ResolveResult GetLanguageItem (int offset)
		{
			string txt = this.Document.Text;
			string fileName = view.ContentName ?? view.UntitledName;
			
			// we'll cache old results.
			if (offset == oldOffset)
				return this.resolveResult;
			oldOffset = offset;
			
			this.resolveResult = null;
			MonoDevelop.Ide.Gui.Document doc = IdeApp.Workbench.ActiveDocument;
			if (doc == null)
				return null;
			
			IParser parser = ProjectDomService.GetParser (fileName, Document.MimeType);
			if (parser == null)
				return null;

			ProjectDom        dom      = this.ProjectDom;
			IResolver         resolver = parser.CreateResolver (dom, doc, fileName);
			IExpressionFinder expressionFinder = parser.CreateExpressionFinder (dom);
			if (resolver == null || expressionFinder == null) 
				return null;
			
			ExpressionResult expressionResult = expressionFinder.FindFullExpression (txt, offset);
			if (expressionResult == null) 
				return null;
			
			DocumentLocation loc = Document.OffsetToLocation (offset);
			this.resolveResult = resolver.Resolve (expressionResult, new DomLocation (loc.Line + 1, loc.Column + 1));
			return this.resolveResult;
		}
		
		protected override bool OnFocusOutEvent (Gdk.EventFocus evnt)
		{
			CompletionWindowManager.HideWindow ();
			ParameterInformationWindowManager.HideWindow ();
			return base.OnFocusOutEvent (evnt); 
		}
		
		protected override bool OnScrollEvent (Gdk.EventScroll evnt)
		{
			CompletionWindowManager.HideWindow ();
			ParameterInformationWindowManager.HideWindow ();
			return base.OnScrollEvent (evnt);
		}
		
		void ShowPopup ()
		{
			HideTooltip ();
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
		
		[CommandHandler (MonoDevelop.Ide.Commands.EditCommands.Delete)]
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
		
#endregion
		
	}
}
