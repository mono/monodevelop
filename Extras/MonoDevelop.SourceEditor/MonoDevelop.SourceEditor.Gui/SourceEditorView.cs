using System;
using System.IO;
using System.Runtime.InteropServices;

using Gtk;
using Gdk;
using Global = Gtk.Global;
using GtkSourceView;

using MonoDevelop.Components.Commands;
using MonoDevelop.Core;
using MonoDevelop.Core.AddIns;
using MonoDevelop.Core.Gui;
using MonoDevelop.Core.Gui.Utils;
using MonoDevelop.Ide.CodeTemplates;
using MonoDevelop.Ide.Commands;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Projects;
using MonoDevelop.Projects.Gui.Completion;
using MonoDevelop.Projects.Parser;
using MonoDevelop.SourceEditor.Actions;
using MonoDevelop.SourceEditor.Document;
using MonoDevelop.SourceEditor.InsightWindow;
using MonoDevelop.SourceEditor.Properties;
using MonoDevelop.SourceEditor.FormattingStrategy;
using MonoDevelop.SourceEditor;

namespace MonoDevelop.SourceEditor.Gui
{
	public class SourceEditorView : SourceView, IFormattableDocument, ICompletionWidget
	{	
		public readonly SourceEditor ParentEditor;
		internal IFormattingStrategy fmtr = new DefaultFormattingStrategy ();
		public SourceEditorBuffer buf;
		bool codeCompleteEnabled;
		bool autoHideCompletionWindow = true;
		bool autoInsertTemplates;
		EditActionCollection editactions = new EditActionCollection ();
		LanguageItemWindow languageItemWindow;
		CodeCompletionDataProvider currentCompletionProvider;
		
		const int LanguageItemTipTimer = 800;
		ILanguageItem tipItem;
		bool showTipScheduled;
		int langTipX, langTipY;
		uint tipTimeoutId;

		public bool EnableCodeCompletion {
			get { return codeCompleteEnabled; }
			set { codeCompleteEnabled = value; }
		}

		public bool AutoInsertTemplates {
			get { return autoInsertTemplates; }
			set { autoInsertTemplates = value; }
		}
		
		protected SourceEditorView (IntPtr p): base (p)
		{
		}

		public SourceEditorView (SourceEditorBuffer buf, SourceEditor parent)
		{
			this.ParentEditor = parent;
			this.TabsWidth = 4;
			Buffer = this.buf = buf;
			AutoIndent = false;
			SmartHomeEnd = true;
			ShowLineNumbers = true;
			ShowLineMarkers = true;
			buf.PlaceCursor (buf.StartIter);
			GrabFocus ();
			buf.MarkSet += new MarkSetHandler (BufferMarkSet);
			buf.Changed += new EventHandler (BufferChanged);
			LoadEditActions ();
			this.Events = this.Events | EventMask.PointerMotionMask | EventMask.LeaveNotifyMask;
		}
		
		public new void Dispose ()
		{
			if (currentCompletionProvider != null) {
				currentCompletionProvider.Dispose ();
				currentCompletionProvider = null;
			}
			HideLanguageItemWindow ();
			buf.MarkSet -= new MarkSetHandler (BufferMarkSet);
			buf.Changed -= new EventHandler (BufferChanged);
			base.Dispose ();
		}
		
		protected override bool OnMotionNotifyEvent (Gdk.EventMotion evnt)
		{
			bool res = base.OnMotionNotifyEvent (evnt);
			UpdateLanguageItemWindow ();
			return res;
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
			ModifierType mask; // ignored
			int xloc, yloc;

			showTipScheduled = false;
				
			this.GetWindow (TextWindowType.Text).GetPointer (out xloc, out yloc, out mask);

			TextIter ti = this.GetIterAtLocation (xloc + this.VisibleRect.X, yloc + this.VisibleRect.Y);
			ILanguageItem item = GetLanguageItem (ti);
			
			if (item != null) {
				// Tip already being shown for this language item?
				if (languageItemWindow != null && tipItem != null && tipItem.Equals (item))
					return false;
				
				langTipX = xloc;
				langTipY = yloc;
				tipItem = item;

				HideLanguageItemWindow ();

				languageItemWindow = new LanguageItemWindow (tipItem, GetParserContext ());
				
				int ox, oy;
				this.GetWindow (TextWindowType.Text).GetOrigin (out ox, out oy);
				int w = languageItemWindow.Child.SizeRequest().Width;
				languageItemWindow.Move (langTipX + ox - (w/2), langTipY + oy + 20);
				languageItemWindow.ShowAll ();
			} else
				HideLanguageItemWindow ();
			
			return false;
		}
		

		protected override bool OnLeaveNotifyEvent (Gdk.EventCrossing evnt)		
		{
			HideLanguageItemWindow ();
			return base.OnLeaveNotifyEvent (evnt);
		}
		
		protected override bool OnScrollEvent (Gdk.EventScroll evnt)
		{
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
		
		void BufferMarkSet (object s, MarkSetArgs a)
		{
			if (a.Mark.Name == "insert") {
				if (autoHideCompletionWindow)
					CompletionListWindow.HideWindow ();
				buf.HideHighlightLine ();
			}
		}

		void LoadEditActions ()
		{
			string editactionsPath = "/AddIns/DefaultTextEditor/EditActions";
			if (AddInTreeSingleton.AddInTree.TreeNodeExists (editactionsPath)) {
				IEditAction[] actions = (IEditAction[]) (AddInTreeSingleton.AddInTree.GetTreeNode (editactionsPath).BuildChildItems (this)).ToArray (typeof(IEditAction));
				foreach (IEditAction action in actions)
					editactions.Add (action);
			} else {
				Console.WriteLine ("/AddIns/DefaultTextEditor/EditActions addin path not found");
			}
		}
		
		protected override bool OnFocusOutEvent (EventFocus e)
		{
			CompletionListWindow.HideWindow ();
			return base.OnFocusOutEvent (e);
		}
		
		void BufferChanged (object s, EventArgs args)
		{
			if (autoHideCompletionWindow)
				CompletionListWindow.HideWindow ();
		}
		
		protected override bool OnButtonPressEvent (Gdk.EventButton e)
		{
			CompletionListWindow.HideWindow ();
			HideLanguageItemWindow ();
			
			if (!ShowLineMarkers)
				return base.OnButtonPressEvent (e);
			
			if (e.Window == GetWindow (Gtk.TextWindowType.Left)) {
				int x, y;
				WindowToBufferCoords (Gtk.TextWindowType.Left, (int) e.X, (int) e.Y, out x, out y);
				TextIter line;
				int top;

				GetLineAtY (out line, y, out top);
				buf.PlaceCursor (line);		
				
				if (e.Button == 1) {
					buf.ToggleBookmark (line.Line);
				} else if (e.Button == 3) {
					CommandEntrySet cset = new CommandEntrySet ();
					cset.AddItem (EditorCommands.ToggleBookmark);
					cset.AddItem (EditorCommands.ClearBookmarks);
					cset.AddItem (Command.Separator);
					cset.AddItem (DebugCommands.ToggleBreakpoint);
					cset.AddItem (DebugCommands.ClearAllBreakpoints);
					Gtk.Menu menu = IdeApp.CommandService.CreateMenu (cset);
					
					menu.Popup (null, null, null, 3, e.Time);
				}
			} else if (e.Button == 3 && buf.GetSelectedText ().Length == 0) {
				int x, y;
				WindowToBufferCoords (Gtk.TextWindowType.Text, (int) e.X, (int) e.Y, out x, out y);
				buf.PlaceCursor (GetIterAtLocation (x, y));		
			}
			
			return base.OnButtonPressEvent (e);
		}
		
		protected override void OnPopulatePopup (Menu menu)
		{
			HideLanguageItemWindow ();
			
			CommandEntrySet cset = IdeApp.CommandService.CreateCommandEntrySet ("/SharpDevelop/ViewContent/DefaultTextEditor/ContextMenu");
			if (cset.Count > 0) {
				cset.AddItem (Command.Separator);
				IdeApp.CommandService.InsertOptions (menu, cset, 0);
			}
			base.OnPopulatePopup (menu);
		}
		
		public void ShowBreakpointAt (int linenumber)
		{
			if (!buf.IsMarked (linenumber, SourceMarkerType.BreakpointMark))
				buf.ToggleMark (linenumber, SourceMarkerType.BreakpointMark);
		}
		
		public void ClearBreakpointAt (int linenumber)
		{
			if (buf.IsMarked (linenumber, SourceMarkerType.BreakpointMark))
				buf.ToggleMark (linenumber, SourceMarkerType.BreakpointMark);
		}
		
		public void ExecutingAt (int linenumber)
		{
			buf.ToggleMark (linenumber, SourceMarkerType.ExecutionMark);
			buf.MarkupLine (linenumber);	
		}

		public void ClearExecutingAt (int linenumber)
		{
			buf.ToggleMark (linenumber, SourceMarkerType.ExecutionMark);
			buf.UnMarkupLine (linenumber);
		}

		public void SimulateKeyPress (ref Gdk.EventKey evnt)
		{
			Global.PropagateEvent (this, evnt);
		}

		// remove the current line, including the new line chars
		internal void DeleteLine ()
		{
			TextIter start = buf.GetIterAtMark (buf.InsertMark);
			start.LineOffset = 0;
			TextIter end = start;
			if (!end.EndsLine ())
				end.ForwardToLineEnd ();
			// delete the end of the line
			end.Offset++;
			using (AtomicUndo a = new AtomicUndo (buf)) 
				buf.Delete (ref start, ref end);
		}

		internal void ShowCodeCompletion (char key)
		{
			if (EnableCodeCompletion && PeekCharIsWhitespace ()) {
				PrepareCompletionDetails (buf.GetIterAtMark (buf.InsertMark));
				CompletionListWindow.ShowWindow (key, GetCodeCompletionDataProvider (false), this);
			}
		}

		// FIXME: this method is broken
		// and will trigger at various incorrect spots
		internal void TriggerCodeComplete ()
		{
			
			TextIter iter = buf.GetIterAtMark (buf.InsertMark);
			char triggerChar = '\0';
			TextIter triggerIter = TextIter.Zero;
			do {
				if (iter.Char == null || iter.Char.Length == 0) {
					break;
				}
				switch (iter.Char[0]) {
				case ' ':
				case '\t':
				case '.':
				case '(':
				case '[':
					triggerIter = iter;
					triggerChar = iter.Char[0];
					break;
				}
				if (!triggerIter.Equals (TextIter.Zero))
					break;
				iter.BackwardChar ();
			} while (iter.LineOffset != 0);

			if (triggerIter.Equals (TextIter.Zero))
				return;
			triggerIter.ForwardChar ();
			
			PrepareCompletionDetails(triggerIter);
			CompletionListWindow.ShowWindow (triggerChar, GetCodeCompletionDataProvider (true), this);
		}

		IParserContext GetParserContext ()
		{
			string file = ParentEditor.DisplayBinding.IsUntitled ? ParentEditor.DisplayBinding.UntitledName : ParentEditor.DisplayBinding.ContentName;
			Project project = ParentEditor.DisplayBinding.Project;
			IParserDatabase pdb = IdeApp.ProjectOperations.ParserDatabase;
			
			if (project != null)
				return pdb.GetProjectParserContext (project);
			else
				return pdb.GetFileParserContext (file);
		}

		CodeCompletionDataProvider GetCodeCompletionDataProvider (bool ctrl)
		{
			if (currentCompletionProvider != null)
				currentCompletionProvider.Dispose ();
			IParserContext ctx = GetParserContext ();
			string file = ParentEditor.DisplayBinding.IsUntitled ? ParentEditor.DisplayBinding.UntitledName : ParentEditor.DisplayBinding.ContentName;
			currentCompletionProvider = new CodeCompletionDataProvider (ctx, file, ctrl);
			return currentCompletionProvider;
		}
			
		internal bool MonodocResolver ()
		{
			TextIter insertIter = buf.GetIterAtMark (buf.InsertMark);
			TextIter triggerIter = TextIter.Zero;
			try {
				do {
					switch (insertIter.Char[0]) {
					case ' ':
					case '\t':
					case '\r':
					case '\n':
					case '.':
					case ';':
					case '(':
					case '[':
						triggerIter = insertIter;
						break;
					}
					if (!triggerIter.Equals (TextIter.Zero))
						break;
					insertIter.ForwardChar ();
				} while (insertIter.LineOffset != 0);
				triggerIter.ForwardChar ();
			} catch {
				return false;
			}
			ILanguageItem languageItem = GetLanguageItem (triggerIter);
			
			if (languageItem == null)
				return false;

			IdeApp.HelpOperations.ShowHelp (MonoDevelop.Projects.Services.DocumentationService.GetHelpUrl(languageItem));
			
			return true;
		}
		
		ILanguageItem GetLanguageItem (TextIter ti)
		{
			string txt = buf.Text;
			string fileName = ParentEditor.DisplayBinding.ContentName;
			IParserContext ctx = GetParserContext ();
			IExpressionFinder expressionFinder = ctx.GetExpressionFinder (fileName);
			string expression = expressionFinder == null ? TextUtilities.GetExpressionBeforeOffset (this, ti.Offset) : expressionFinder.FindFullExpression (txt, ti.Offset).Expression;
			if (expression == null)
				return null;

			return ctx.ResolveIdentifier (expression, ti.Line + 1, ti.LineOffset + 1, fileName, txt);
		}

		internal void ScrollUp ()
		{
			ParentEditor.Vadjustment.Value -= (ParentEditor.Vadjustment.StepIncrement / 5);
			if (ParentEditor.Vadjustment.Value < 0.0d)
				ParentEditor.Vadjustment.Value = 0.0d;

			ParentEditor.Vadjustment.ChangeValue();
		}

		internal void ScrollDown ()
		{
			double maxvalue = ParentEditor.Vadjustment.Upper - ParentEditor.Vadjustment.PageSize;
			double newvalue = ParentEditor.Vadjustment.Value + (ParentEditor.Vadjustment.StepIncrement / 5);

			if (newvalue > maxvalue)
				ParentEditor.Vadjustment.Value = maxvalue;
			else
				ParentEditor.Vadjustment.Value = newvalue;

			ParentEditor.Vadjustment.ChangeValue();
		}
		
		protected override bool OnKeyPressEvent (Gdk.EventKey evnt)
		{
			HideLanguageItemWindow ();
			
			if (CompletionListWindow.ProcessKeyEvent (evnt))
				return true;
			
			autoHideCompletionWindow = false;
			bool res = false;
			IEditAction action = editactions.GetAction (evnt.Key, evnt.State);
			if (action != null) {
				action.PreExecute (this);
				if (action.PassToBase)
					base.OnKeyPressEvent (evnt);
				
				action.Execute (this);
				
				if (action.PassToBase)
					base.OnKeyPressEvent (evnt);
				action.PostExecute (this);
				
				res = true;
			} else {
				res = base.OnKeyPressEvent (evnt);
			}
			autoHideCompletionWindow = true;
			return res;
		}

		internal bool GotoSelectionEnd ()
		{
			return buf.GotoSelectionEnd ();
		}

		internal bool GotoSelectionStart ()
		{
			return buf.GotoSelectionStart ();
		}
		
		public int FindPrevWordStart (string doc, int offset)
		{
			for ( offset-- ; offset >= 0 ; offset--)
			{
				if (System.Char.IsWhiteSpace (doc, offset)) break;
			}
			return ++offset;
		}

		public string GetWordBeforeCaret ()
		{
			int offset = buf.GetIterAtMark (buf.InsertMark).Offset;
			int start = FindPrevWordStart (buf.Text, offset);
			return buf.Text.Substring (start, offset - start);
		}
		
		public int DeleteWordBeforeCaret ()
		{
			int offset = buf.GetIterAtMark (buf.InsertMark).Offset;
			int start = FindPrevWordStart (buf.Text, offset);
			TextIter startIter = buf.GetIterAtOffset (start);
			TextIter offsetIter = buf.GetIterAtOffset (offset);
			buf.Delete (ref startIter, ref offsetIter);
			return start;
		}

		public string GetLeadingWhiteSpace (int line) {
			string lineText = ((IFormattableDocument)this).GetLineAsString (line);
			int index = 0;
			while (index < lineText.Length && System.Char.IsWhiteSpace (lineText[index])) {
    				index++;
    			}
 	   		return index > 0 ? lineText.Substring (0, index) : "";
		}

		// handles the details of inserting a code template
		// returns true if it was inserted
		public bool InsertTemplate ()
		{
			if (AutoInsertTemplates) {
				string word = GetWordBeforeCaret ();
				if (word != null) {
					CodeTemplateGroup templateGroup = CodeTemplateLoader.GetTemplateGroupPerFilename (ParentEditor.DisplayBinding.ContentName);
					if (templateGroup != null) {
						foreach (CodeTemplate template in templateGroup.Templates) {
							if (template.Shortcut == word) {
								InsertTemplate (template);
								return true;
							}
						}
					}
				}
			}
			return false;
		}
		
		public void InsertTemplate (CodeTemplate template)
		{
			TextIter iter = buf.GetIterAtMark (buf.InsertMark);
			int newCaretOffset = iter.Offset;
			string word = GetWordBeforeCaret ().Trim ();
			int beginLine = iter.Line;
			int endLine = beginLine;
			if (word.Length > 0)
				newCaretOffset = DeleteWordBeforeCaret ();
			
			string leadingWhiteSpace = GetLeadingWhiteSpace (beginLine);

			int finalCaretOffset = newCaretOffset;
			
			for (int i =0; i < template.Text.Length; ++i) {
				switch (template.Text[i]) {
					case '|':
						finalCaretOffset = newCaretOffset;
						break;
					case '\r':
						break;
					case '\t':
						buf.InsertAtCursor ("\t");
						newCaretOffset++;
						break;
					case '\n':
						buf.InsertAtCursor ("\n");
						newCaretOffset++;
						endLine++;
						break;
					default:
						buf.InsertAtCursor (template.Text[i].ToString ());
						newCaretOffset++;
						break;
				}
			}
			
			if (endLine > beginLine) {
				IndentLines (beginLine+1, endLine, leadingWhiteSpace);
			}
			
			buf.PlaceCursor (buf.GetIterAtOffset (finalCaretOffset));
		}


#region Indentation
		public bool IndentSelection (bool unindent)
		{
			TextIter begin, end;
			if (!buf.GetSelectionBounds (out begin, out end))
				return false;
			
			int y0 = begin.Line, y1 = end.Line;

			// If last line isn't selected, it's illogical to indent it.
			if (end.StartsLine())
				y1--;

			if (y0 == y1)
				return false;
			
			using (AtomicUndo a = new AtomicUndo (buf)) {
				if (unindent)
					UnIndentLines (y0, y1);
				else
					IndentLines (y0, y1);
				SelectLines (y0, y1);
			}
			
			return true;
		}

		public void FormatLine ()
		{
			if (((IFormattableDocument)this).IndentStyle == IndentStyle.Smart) {
				TextIter iter = buf.GetIterAtMark (buf.InsertMark);
				fmtr.FormatLine (this, iter.Line, iter.Offset, '\n');
			}
			IndentLine ();
		}

		public void IndentLine ()
		{
			TextIter iter = buf.GetIterAtMark (buf.InsertMark);

			// preserve offset in line
			int offset = 0;
			int chars = fmtr.IndentLine (this, iter.Line);
			offset += chars;

			// FIXME: not quite right yet
			// restore the offset
			TextIter nl = buf.GetIterAtMark (buf.InsertMark);
			if (offset < nl.CharsInLine)
				nl.LineOffset = offset;
			buf.PlaceCursor (nl);
		}

		void IndentLines (int y0, int y1)
		{
			IndentLines (y0, y1, InsertSpacesInsteadOfTabs ? new string (' ', (int) TabsWidth) : "\t");
		}

		void IndentLines (int y0, int y1, string indent)
		{
			for (int l = y0; l <= y1; l ++) {
				TextIter it = Buffer.GetIterAtLine (l);
				if (!it.EndsLine())
					Buffer.Insert (ref it, indent);
			}
		}
		
		void UnIndentLines (int y0, int y1)
		{
			for (int l = y0; l <= y1; l ++) {
				TextIter start = Buffer.GetIterAtLine (l);
				TextIter end = start;
				
				char c = start.Char[0];
				
				if (c == '\t') {
					end.ForwardChar ();
					buf.Delete (ref start, ref end);
					
				} else if (c == ' ') {
					int cnt = 0;
					int max = (int) TabsWidth;
					
					while (cnt <= max && end.Char[0] == ' ' && ! end.EndsLine ()) {
						cnt ++;
						end.ForwardChar ();
					}
					
					if (cnt == 0)
						return;
					
					buf.Delete (ref start, ref end);
				}
			}
		}
		
		void SelectLines (int y0, int y1)
		{
			Buffer.PlaceCursor (Buffer.GetIterAtLine (y0));
			
			TextIter end = Buffer.GetIterAtLine (y1);
			end.ForwardToLineEnd ();
			Buffer.MoveMark ("selection_bound", end);
		}

		void PrepareCompletionDetails(TextIter iter)
		{
			Gdk.Rectangle rect = GetIterLocation (Buffer.GetIterAtMark (Buffer.InsertMark));
			int wx, wy;
			BufferToWindowCoords (Gtk.TextWindowType.Widget, rect.X, rect.Y + rect.Height, out wx, out wy);
			int tx, ty;
			GdkWindow.GetOrigin (out tx, out ty);

			this.completionX = tx + wx;
			this.completionY = ty + wy;
			this.textHeight = rect.Height;
			this.triggerMark = buf.CreateMark (null, iter, true);
		}
#endregion

#region IFormattableDocument
		string IFormattableDocument.GetLineAsString (int ln)
		{
			TextIter begin = Buffer.GetIterAtLine (ln);
			TextIter end = begin;
			if (!end.EndsLine ())
				end.ForwardToLineEnd ();
			
			return begin.GetText (end);
		}
		
		void IFormattableDocument.BeginAtomicUndo ()
		{
			Buffer.BeginUserAction ();
		}
		void IFormattableDocument.EndAtomicUndo ()
		{
			Buffer.EndUserAction ();
		}
		
		void IFormattableDocument.ReplaceLine (int ln, string txt)
		{
			TextIter begin = Buffer.GetIterAtLine (ln);
			TextIter end = begin;
			if (!end.EndsLine ())
				end.ForwardToLineEnd ();
			
			Buffer.Delete (ref begin, ref end);
			Buffer.Insert (ref begin, txt);
		}
		
		IndentStyle IFormattableDocument.IndentStyle
		{
			get { return TextEditorProperties.IndentStyle; }
		}
		
		bool IFormattableDocument.AutoInsertCurlyBracket
		{
			get { return TextEditorProperties.AutoInsertCurlyBracket; }
		}
		
		string IFormattableDocument.TextContent
		{ get { return ParentEditor.DisplayBinding.Text; } }
		
		int IFormattableDocument.TextLength
		{ get { return Buffer.EndIter.Offset; } }
		
		char IFormattableDocument.GetCharAt (int offset)
		{
			TextIter it = Buffer.GetIterAtOffset (offset);
			return it.Char[0];
		}
		
		void IFormattableDocument.Insert (int offset, string text)
		{
			TextIter insertIter = Buffer.GetIterAtOffset (offset);
			Buffer.Insert (ref insertIter, text);
		}
		
		string IFormattableDocument.IndentString
		{
			get { return !InsertSpacesInsteadOfTabs ? "\t" : new string (' ', (int) TabsWidth); }
		}
		
		int IFormattableDocument.GetClosingBraceForLine (int ln, out int openingLine)
		{
			int offset = MonoDevelop.Projects.Gui.Completion.TextUtilities.SearchBracketBackward
				(this, Buffer.GetIterAtLine (ln).Offset - 1, '{', '}');
			
			openingLine = offset == -1 ? -1 : Buffer.GetIterAtOffset (offset).Line;
			return offset;
		}
		
		void IFormattableDocument.GetLineLengthInfo (int ln, out int offset, out int len)
		{
			TextIter begin = Buffer.GetIterAtLine (ln);
			offset = begin.Offset;
			len = begin.CharsInLine;
		}
#endregion

#region ICompletionWidget

		private int completionX;
		int ICompletionWidget.TriggerXCoord
		{
			get
			{
				return completionX;
			}
		}

		private int completionY;
		int ICompletionWidget.TriggerYCoord
		{
			get
			{
				return completionY;
			}
		}

		private int textHeight;
		int ICompletionWidget.TriggerTextHeight
		{
			get
			{
				return textHeight;
			}
		}

		string ICompletionWidget.CompletionText
		{
			get
			{
				return Buffer.GetText (Buffer.GetIterAtMark (triggerMark), Buffer.GetIterAtMark (Buffer.InsertMark), false);
			}
		}

		void ICompletionWidget.SetCompletionText (string partial_word, string complete_word)
		{
			TextIter offsetIter = buf.GetIterAtMark(triggerMark);
                        TextIter endIter = buf.GetIterAtOffset (offsetIter.Offset + partial_word.Length);
                        buf.MoveMark (buf.InsertMark, offsetIter);
                        buf.Delete (ref offsetIter, ref endIter);
                        buf.InsertAtCursor (complete_word);
		}

		void ICompletionWidget.InsertAtCursor (string text)
		{
			buf.InsertAtCursor (text);
		}
		
		string ICompletionWidget.Text
		{
			get
			{
				return buf.Text;
			}
		}

		int ICompletionWidget.TextLength
		{
			get
			{
				return buf.EndIter.Offset + 1;
			}
		}

		char ICompletionWidget.GetChar (int offset)
		{
			return buf.GetIterAtOffset (offset).Char[0];
		}

		string ICompletionWidget.GetText (int startOffset, int endOffset)
		{
			return buf.GetText(buf.GetIterAtOffset (startOffset), buf.GetIterAtOffset(endOffset), true);
		}

		private TextMark triggerMark;
		int ICompletionWidget.TriggerOffset
		{
			get
			{
				return buf.GetIterAtMark (triggerMark).Offset;
			}
		}

		int ICompletionWidget.TriggerLine
		{
			get
			{
				return buf.GetIterAtMark (triggerMark).Line;
			}
		}

		int ICompletionWidget.TriggerLineOffset
		{
			get
			{
				return buf.GetIterAtMark (triggerMark).LineOffset;
			}
		}

		Gtk.Style ICompletionWidget.GtkStyle
		{
			get
			{
				return Style.Copy();
			}
		}

		bool PeekCharIsWhitespace ()
		{
			TextIter start = buf.GetIterAtMark (buf.InsertMark);
			TextIter end = buf.GetIterAtLine (start.Line);
			end.Offset = start.Offset + 1;
			string text = buf.GetText (start, end, true);
			// if it is not whitespace or the start of a comment
			// we disable completion except for by ctl+space
			if (text.Length == 1)
				return IsSeparator (text[0]);
			return true;
		}
		
		bool IsSeparator (char c)
		{
			// FIXME: this is language specific.
			return !System.Char.IsLetterOrDigit (c) && c != '_';
		}
#endregion
	}
}
