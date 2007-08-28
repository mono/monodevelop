using System;
using System.IO;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using Gtk;
using Gdk;
using Global = Gtk.Global;
using GtkSourceView;

using Mono.Addins;

using MonoDevelop.Components.Commands;

using MonoDevelop.Core;
using MonoDevelop.Core.Gui;
using MonoDevelop.Core.Gui.Utils;

using MonoDevelop.Ide.Commands;
using MonoDevelop.Ide.CodeTemplates;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Gui.Content;

using MonoDevelop.Projects;
using MonoDevelop.Projects.Gui.Completion;
using MonoDevelop.Projects.Parser;

using MonoDevelop.SourceEditor;
using MonoDevelop.SourceEditor.Actions;
using MonoDevelop.SourceEditor.FormattingStrategy;

namespace MonoDevelop.SourceEditor.Gui
{
	public class SourceEditorView : SourceView, ICompletionWidget, ITextEditorExtension
	{
		public static readonly bool HighlightCurrentLineSupported;
		public static bool HighlightSpacesEnabled = TextEditorProperties.HighlightSpaces;
		public static bool HighlightTabsEnabled = TextEditorProperties.HighlightTabs;
		public static bool HighlightNewlinesEnabled = TextEditorProperties.HighlightNewlines;
		
		public readonly SourceEditor ParentEditor;
		internal IFormattingStrategy fmtr = new DefaultFormattingStrategy ();
		public SourceEditorBuffer buf;
		bool codeCompleteEnabled;
		bool autoInsertTemplates;
		EditActionCollection editactions = new EditActionCollection ();
		LanguageItemWindow languageItemWindow;
		ITextEditorExtension extension;
		TextEditor thisEditor;
		DrawControlCharacterImp controlsDrawer;
		event EventHandler completionContextChanged;
		
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
		
		static SourceEditorView ()
		{
			SourceView view = new SourceView ();
			try
			{
				GetHighlightCurrentLine (view.Handle);
				HighlightCurrentLineSupported = true;
			}
			catch
			{
				HighlightCurrentLineSupported = false;
			}
			finally
			{
				view.Destroy ();
				view = null;
			}
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
			controlsDrawer = new DrawControlCharacterImp (this);
			HighlightCurrentLine = true;
			buf.PlaceCursor (buf.StartIter);
			GrabFocus ();
			buf.MarkSet += new MarkSetHandler (BufferMarkSet);
			buf.Changed += new EventHandler (BufferChanged);
			LoadEditActions ();
			this.Events = this.Events | EventMask.PointerMotionMask | EventMask.LeaveNotifyMask | EventMask.ExposureMask;
		}
		
		public new void Dispose ()
		{
			controlsDrawer.Detach ();
			HideLanguageItemWindow ();
			buf.MarkSet -= new MarkSetHandler (BufferMarkSet);
			buf.Changed -= new EventHandler (BufferChanged);
			base.Dispose ();
		}
		
		public ITextEditorExtension AttachExtension (ITextEditorExtension extension)
		{
			this.extension = extension;
			return this;
		}
		
		TextEditor ThisEditor {
			get {
				if (thisEditor == null)
					thisEditor = TextEditor.GetTextEditor (ParentEditor.DisplayBinding);
				return thisEditor;
			}
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
				
				IParserContext pctx = GetParserContext ();
				if (pctx == null)
					return false;

				languageItemWindow = new LanguageItemWindow (tipItem, pctx, GetAmbience ());
				
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
				NotifyCompletionContextChanged ();
				buf.HideHighlightLine ();
			}
		}

		void LoadEditActions ()
		{
			string editactionsPath = "/MonoDevelop/SourceEditor/EditActions";
			IEditAction[] actions = (IEditAction[]) AddinManager.GetExtensionObjects (editactionsPath, typeof(IEditAction));
			foreach (IEditAction action in actions)
				editactions.Add (action);
		}
		
		protected override bool OnFocusOutEvent (EventFocus e)
		{
			NotifyCompletionContextChanged ();
			return base.OnFocusOutEvent (e);
		}
		
		void BufferChanged (object s, EventArgs args)
		{
			NotifyCompletionContextChanged ();
		}
		
		protected override bool OnButtonPressEvent (Gdk.EventButton e)
		{
			if (extension != null)
				extension.CursorPositionChanged ();

			NotifyCompletionContextChanged ();
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
			
			CommandEntrySet cset = IdeApp.CommandService.CreateCommandEntrySet ("/MonoDevelop/SourceEditor/ContextMenu/Editor");
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

		[CommandHandler (TextEditorCommands.DeleteToLineEnd)]
		internal void DeleteLine ()
		{
			TextIter start = buf.GetIterAtMark (buf.InsertMark);
			TextIter end = start;
			
			if (!end.EndsLine ()) {
				// Delete up to, but not including, the end-of-line marker
				end.ForwardToLineEnd ();
			} else {
				// Delete the end-of-line marker
				end.Offset++;
			}
			
			using (AtomicUndo a = new AtomicUndo (buf)) 
				buf.Delete (ref start, ref end);
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
		
		public MonoDevelop.Projects.Ambience.Ambience GetAmbience ()
		{
			Project project = ParentEditor.DisplayBinding.Project;
			if (project != null)
				return project.Ambience;
			else {
				string file = ParentEditor.DisplayBinding.IsUntitled ? ParentEditor.DisplayBinding.UntitledName : ParentEditor.DisplayBinding.ContentName;
				return MonoDevelop.Projects.Services.Ambience.GetAmbienceForFile (file);
			}
		}

		[CommandHandler (HelpCommands.Help)]
		internal void MonodocResolver ()
		{
			TextIter insertIter = buf.GetIterAtMark (buf.InsertMark);
			ILanguageItem languageItem = GetLanguageItem (insertIter);
			
			if (languageItem != null)
				IdeApp.HelpOperations.ShowHelp (MonoDevelop.Projects.Services.DocumentationService.GetHelpUrl(languageItem));
		}
		
		ILanguageItem GetLanguageItem (TextIter ti)
		{
			string txt = buf.Text;
			string fileName = ParentEditor.DisplayBinding.ContentName;
			if (fileName == null)
				fileName = ParentEditor.DisplayBinding.UntitledName;

			IParserContext ctx = GetParserContext ();
			if (ctx == null)
				return null;

			IExpressionFinder expressionFinder = null;
			if (fileName != null)
				expressionFinder = ctx.GetExpressionFinder (fileName);

			string expression = expressionFinder == null ? TextUtilities.GetExpressionBeforeOffset (this, ti.Offset) : expressionFinder.FindFullExpression (txt, ti.Offset).Expression;
			if (expression == null)
				return null;

			return ctx.ResolveIdentifier (expression, ti.Line + 1, ti.LineOffset + 1, fileName, txt);
		}

		[CommandHandler (TextEditorCommands.ScrollLineUp)]
		internal void ScrollUp ()
		{
			ParentEditor.Vadjustment.Value -= (ParentEditor.Vadjustment.StepIncrement / 5);
			if (ParentEditor.Vadjustment.Value < 0.0d)
				ParentEditor.Vadjustment.Value = 0.0d;

			ParentEditor.Vadjustment.ChangeValue();
		}

		[CommandHandler (TextEditorCommands.ScrollLineDown)]
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
			evntCopy = evnt;
			if (extension == null)
				return ((ITextEditorExtension)this).KeyPress (evnt.Key, evnt.State);
			else
				return extension.KeyPress (evnt.Key, evnt.State);
		}
		
		Gdk.EventKey evntCopy;

		bool ITextEditorExtension.KeyPress (Gdk.Key key, Gdk.ModifierType modifier)
		{
			Gdk.EventKey evnt = evntCopy;
			HideLanguageItemWindow ();
			
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
			return res;
		}
		
		protected override void OnMoveCursor (MovementStep step, int count, bool extend_selection)
		{
			if (extension != null)
				extension.CursorPositionChanged ();
			base.OnMoveCursor (step, count, extend_selection);
		}
		
		void ITextEditorExtension.CursorPositionChanged ()
		{
			NavigationService.Log (ParentEditor.DisplayBinding.BuildNavPoint ());
		}

		object ITextEditorExtension.GetExtensionCommandTarget ()
		{
			// There is no need to process commands here because they are all
			// processed before the editor extensions
			return null;
		}
		
		[CommandHandler (TextEditorCommands.CharRight)]
		internal void CursorRight ()
		{
			buf.BeginUserAction ();
			TextIter it = buf.GetIterAtMark (buf.InsertMark);
			it.ForwardChar ();
			buf.MoveMark (buf.InsertMark, it); 
			buf.MoveMark (buf.SelectionBound, it); 
			buf.EndUserAction ();
		}
		
		int FindPrevWordStart (string doc, int offset)
		{
			for (offset--; offset >= 0; offset--) {
				if (System.Char.IsWhiteSpace (doc, offset))
					break;
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

		public string GetLeadingWhiteSpace (int line)
		{
			string lineText = ThisEditor.GetLineText (line + 1);
			int index = 0;
			while (index < lineText.Length && System.Char.IsWhiteSpace (lineText[index]))
				index++;
			
 	   		return index > 0 ? lineText.Substring (0, index) : "";
		}

		// handles the details of inserting a code template
		// returns true if it was inserted
		public bool InsertTemplate ()
		{
			if (AutoInsertTemplates) {
				string word = GetWordBeforeCaret ();
				if (word != null) {
					CodeTemplateGroup templateGroup = CodeTemplateService.GetTemplateGroupPerFilename (ParentEditor.DisplayBinding.ContentName);
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
		public bool IndentSelection (bool unindent, bool requireLineSelection)
		{
			TextIter begin, end;
			bool hasSelection = buf.GetSelectionBounds (out begin, out end);
			if (!hasSelection) {
				if (requireLineSelection)
					return false;
				else
					begin = end = buf.GetIterAtMark (buf.InsertMark);
			}
			
			int y0 = begin.Line, y1 = end.Line;
			
			if (requireLineSelection && y0 == y1)
				return false;

			// If last line isn't selected, it's illogical to indent it.
			if (end.StartsLine() && hasSelection && y0 != y1)
				y1--;

			using (AtomicUndo a = new AtomicUndo (buf)) {
				if (unindent)
					UnIndentLines (y0, y1);
				else
					IndentLines (y0, y1);
				if (hasSelection)
					SelectLines (y0, y1);
			}
			
			return true;
		}

		string IndentString
		{
			get { return !InsertSpacesInsteadOfTabs ? "\t" : new string (' ', (int) TabsWidth); }
		}
		
		public void FormatLine ()
		{
			if (TextEditorProperties.IndentStyle == IndentStyle.Smart) {
				TextIter iter = buf.GetIterAtMark (buf.InsertMark);
				fmtr.FormatLine (ThisEditor, iter.Line + 1, iter.Offset, '\n', IndentString, TextEditorProperties.AutoInsertCurlyBracket);
			}
			IndentLine ();
		}

		public void IndentLine ()
		{
			TextIter iter = buf.GetIterAtMark (buf.InsertMark);
			
			// preserve offset in line
			int n, offset = 0;
			
			if ((n = fmtr.IndentLine (ThisEditor, iter.Line + 1, IndentString)) == 0)
				return;
			
			offset += n;
			
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

			if (y1 < Buffer.EndIter.Line) {
				TextIter end = Buffer.GetIterAtLine (y1 + 1);
				end.LineOffset = 0;
				Buffer.MoveMark ("selection_bound", end);
			} else
				Buffer.MoveMark ("selection_bound", Buffer.EndIter);
		}

#endregion


#region ICompletionWidget

		void NotifyCompletionContextChanged ()
		{
			if (completionContextChanged != null)
				completionContextChanged (this, EventArgs.Empty);
		}

		event EventHandler ICompletionWidget.CompletionContextChanged {
			add { completionContextChanged += value; }
			remove { completionContextChanged -= value; }
		}

		CodeCompletionContext ICompletionWidget.CreateCodeCompletionContext (int triggerOffset)
		{
			TextIter iter = Buffer.GetIterAtOffset (triggerOffset);
			Gdk.Rectangle rect = GetIterLocation (iter);
			int wx, wy;
			BufferToWindowCoords (Gtk.TextWindowType.Widget, rect.X, rect.Y + rect.Height, out wx, out wy);
			int tx, ty;
			GdkWindow.GetOrigin (out tx, out ty);

			CodeCompletionContext ctx = new CodeCompletionContext ();
			ctx.TriggerOffset = iter.Offset;
			ctx.TriggerLine = iter.Line;
			ctx.TriggerLineOffset = iter.LineOffset;
			ctx.TriggerXCoord = tx + wx;
			ctx.TriggerYCoord = ty + wy;
			ctx.TriggerTextHeight = rect.Height;
			return ctx;
		}
		
		string ICompletionWidget.GetCompletionText (ICodeCompletionContext ctx)
		{
			return Buffer.GetText (Buffer.GetIterAtOffset (ctx.TriggerOffset), Buffer.GetIterAtMark (Buffer.InsertMark), false);
		}
		int ICompletionWidget.SelectedLength { get { return buf.GetSelectedText ().Length; } }
		void ICompletionWidget.SetCompletionText (ICodeCompletionContext ctx, string partial_word, string complete_word)
		{
			TextIter iter1, iter2;
			if (buf.GetSelectionBounds (out iter1, out iter2)) {
				buf.Delete (ref iter1, ref iter2);
			}
			TextIter offsetIter = buf.GetIterAtOffset (ctx.TriggerOffset);
			TextIter endIter = buf.GetIterAtOffset (offsetIter.Offset + partial_word.Length);
			buf.MoveMark (buf.InsertMark, offsetIter);
			buf.Delete (ref offsetIter, ref endIter);
			buf.InsertAtCursor (complete_word);
			ScrollMarkOnscreen (buf.InsertMark);
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

		Gtk.Style ICompletionWidget.GtkStyle
		{
			get
			{
				return Style.Copy();
			}
		}
#endregion
		
#region HighlightCurrentLine functionality
		//gboolean gtk_source_view_get_highlight_current_line (GtkSourceView *view);
		[DllImport ("gtksourceview-1.0", EntryPoint="gtk_source_view_get_highlight_current_line")]
		static extern bool GetHighlightCurrentLine (IntPtr raw);

		//void gtk_source_view_set_highlight_current_line (GtkSourceView *view, gboolean show);
		[DllImport("gtksourceview-1.0", EntryPoint="gtk_source_view_set_highlight_current_line")]
		static extern void SetHighlightCurrentLine (IntPtr raw, bool show);

		[GLib.Property ("highlight-current-line")]
		public bool HighlightCurrentLine {
			get  {
				if (HighlightCurrentLineSupported)
					return GetHighlightCurrentLine (Handle);
				return false;
			}
			set  {
				if (HighlightCurrentLineSupported)
					SetHighlightCurrentLine (Handle, value);
			}
		}
#endregion
 
#region Drawing control characters functionality
		class DrawControlCharacterImp
		{
			SourceView view;

			public DrawControlCharacterImp (SourceView view)
			{
				this.view = view;
				view.WidgetEventAfter += OnWidgetEvent;
			}

			public void Detach ()
			{
				view.WidgetEventAfter -= OnWidgetEvent;
				view = null;
			}

			void OnWidgetEvent (object o, WidgetEventAfterArgs args)
			{
				if (args.Event.Type == Gdk.EventType.Expose &&
				    o is TextView &&
				    args.Event.Window == view.GetWindow (TextWindowType.Text))
				{
					int x, y;
					view.WindowToBufferCoords (TextWindowType.Text,
					                           args.Event.Window.ClipRegion.Clipbox.X,
					                           args.Event.Window.ClipRegion.Clipbox.Y,
					                           out x, out y);

					TextIter start, end;
					int topLine;
					view.GetLineAtY (out start, y, out topLine);
					view.GetLineAtY (out end, y + args.Event.Window.ClipRegion.Clipbox.Height, out topLine);
					end.ForwardToLineEnd ();
					Draw (args.Event.Window, view, start, end);
				}
			}

			static Cairo.Color GetDrawingColorForIter (TextView view, TextIter iter)
			{
				TextIter start, end;
				Gdk.Color color;
				Gdk.Color bgColor;
				
				if (iter.Buffer.GetSelectionBounds (out start, out end) && iter.InRange (start, end)) {
					bgColor = view.Style.Base (StateType.Selected);
					color = view.Style.Text (StateType.Selected);
				} else {
					bgColor = view.Style.Base (StateType.Normal);
					color = view.Style.Text (StateType.Normal);
				}
				
				//simple interpolation 1/4 of way between BG colour and text colour
				int red   = (bgColor.Red   * 3 + color.Red  ) / 4;
				int green = (bgColor.Green * 3 + color.Green) / 4;
				int blue  = (bgColor.Blue  * 3 + color.Blue ) / 4;

				return new Cairo.Color ((double)(red) / UInt16.MaxValue,
				                        (double)(green) / UInt16.MaxValue,
				                        (double)(blue) / UInt16.MaxValue);
			}

			static void DrawSpaceAtIter (Cairo.Context cntx, TextView view, TextIter iter)
			{
				Gdk.Rectangle rect = view.GetIterLocation (iter);
				int x, y;
				view.BufferToWindowCoords (TextWindowType.Text,
				                           rect.X + rect.Width / 2,
				                           rect.Y + rect.Height / 2,
				                           out x, out y);
				cntx.Save ();
				cntx.Color =  GetDrawingColorForIter (view, iter);
				//no overlap on the circle, even if context is set to LineCap.Square
				cntx.LineCap = Cairo.LineCap.Butt;
				
				cntx.MoveTo (x, y);
				cntx.Arc (x, y, 0.5, 0, 2 * Math.PI);
				
				cntx.Stroke ();
				cntx.Restore ();
			}

			static void DrawTabAtIter (Cairo.Context cntx, TextView view, TextIter iter)
			{
				Gdk.Rectangle rect = view.GetIterLocation (iter);
				int x, y;
				view.BufferToWindowCoords (TextWindowType.Text,
				                           rect.X,
				                           rect.Y + rect.Height / 2,
				                           out x, out y);
				cntx.Save ();
				cntx.Color =  GetDrawingColorForIter (view, iter);
				
				double arrowSize = 3;
				cntx.MoveTo (x + 2, y + 0);
				cntx.RelLineTo (new Cairo.Distance (rect.Width - 4, 0));
				cntx.RelLineTo (new Cairo.Distance (-arrowSize, -arrowSize));
				cntx.RelMoveTo (new Cairo.Distance (arrowSize, arrowSize));
				cntx.RelLineTo (new Cairo.Distance (-arrowSize, arrowSize));
				
				cntx.Stroke ();
				cntx.Restore ();
			}
			
			static void DrawLineEndAtIter (Cairo.Context cntx, TextView view, TextIter iter)
			{
				Gdk.Rectangle rect = view.GetIterLocation (iter);
				int x, y;
				view.BufferToWindowCoords (TextWindowType.Text,
				                           rect.X,
				                           rect.Y + rect.Height / 2,
				                           out x, out y);
				cntx.Save ();
				cntx.Color =  GetDrawingColorForIter (view, iter);
				
				double arrowSize = 3;
				cntx.MoveTo (x + 10, y);
				cntx.RelLineTo (new Cairo.Distance (0, -arrowSize));
				cntx.RelMoveTo (new Cairo.Distance (0, arrowSize));
				cntx.RelLineTo (new Cairo.Distance (-8, 0));
				cntx.RelLineTo (new Cairo.Distance (arrowSize, arrowSize));
				cntx.RelMoveTo (new Cairo.Distance (-arrowSize, -arrowSize));
				cntx.RelLineTo (new Cairo.Distance (arrowSize, -arrowSize));
				
				cntx.Stroke ();
				cntx.Restore ();
			}

			static void Draw (Gdk.Drawable drawable, TextView view, TextIter start, TextIter end)
			{
				if (HighlightSpacesEnabled || HighlightTabsEnabled || HighlightNewlinesEnabled)
				{
					Cairo.Context cntx = Gdk.CairoHelper.Create (drawable);
					
					//shift to pixel grid to reduce antialiasing
					cntx.Antialias = Cairo.Antialias.Default;
					cntx.LineCap = Cairo.LineCap.Square;
					cntx.LineWidth = 1;
					cntx.Translate (0.5, 0.5);

					TextIter iter = start;
					while (iter.Compare (end) <= 0)
					{
						switch (iter.Char)
						{
							case " ":
							  if (HighlightSpacesEnabled)
								  DrawSpaceAtIter (cntx, view, iter);
								break;
							case "\t":
							  if (HighlightTabsEnabled)
								  DrawTabAtIter (cntx, view, iter);
								break;
							case "\n":
							case "\r":
							  if (HighlightNewlinesEnabled)
								  DrawLineEndAtIter (cntx, view, iter);
								break;
							default:
								break;
						}
						if (! iter.ForwardChar ())
							break;
					}
					((IDisposable)cntx).Dispose ();
				}
			}
		}
#endregion
	}
}
