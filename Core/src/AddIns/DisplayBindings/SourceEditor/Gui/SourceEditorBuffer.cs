using Gtk;
using GLib;

using MonoDevelop.Gui;
using MonoDevelop.Internal.Project;
using MonoDevelop.Core.Properties;
using MonoDevelop.Core.AddIns;
using MonoDevelop.Services;
using MonoDevelop.Core.Services;
using MonoDevelop.Core.AddIns.Codons;
using MonoDevelop.Internal.Parser;

using System;
using System.IO;
using System.Collections;
using System.Runtime.InteropServices;

using ICSharpCode.SharpRefactory.Parser;
using GtkSourceView;
	
namespace MonoDevelop.SourceEditor.Gui
{
	public enum SourceMarkerType
	{
		SourceEditorBookmark,
		BreakpointMark,
		ExecutionMark
	}

	// This gives us a nice way to avoid the try/finally
	// which is really long.
	struct NoUndo : IDisposable
	{
		SourceEditorBuffer b;
		
		public NoUndo (SourceEditorBuffer b)
		{
			this.b = b;
			b.BeginNotUndoableAction ();
		}
		
		public void Dispose ()
		{
			b.EndNotUndoableAction ();
		}
	}
	
	// This gives us a nice way to avoid the try/finally
	// which is really long.
	struct AtomicUndo : IDisposable
	{
		SourceEditorBuffer b;
		
		public AtomicUndo (SourceEditorBuffer b)
		{
			this.b = b;
			b.BeginUserAction ();
		}
		
		public void Dispose ()
		{
			b.EndUserAction ();
		}
	}
	
	public class SourceEditorBuffer : SourceBuffer, IClipboardHandler
	{	
		SourceViewService svs = ServiceManager.GetService (typeof (SourceViewService)) as SourceViewService;
		TextTag markup;
		TextTag complete_ahead;
		TextTag compilation_error;
		TextMark complete_end;
		TextTag highlightLineTag;
		AtomicUndo atomic_undo;
		SourceEditorView view;
		int highlightLine = -1;
		bool underlineErrors = true;

		IParserService ps = (IParserService)ServiceManager.GetService (typeof (IParserService));

		public SourceEditorView View
		{
			get { return view; }
			set { view = value; }
		}

		public bool UnderlineErrors {
			get { return underlineErrors; }
			set {
				underlineErrors = value;
				/* still too broken to leave on
				if (underlineErrors) {
					ps.ParseInformationChanged += (ParseInformationEventHandler) Runtime.DispatchService.GuiDispatch (new ParseInformationEventHandler (ParseChanged));
				}
				else {
					ps.ParseInformationChanged -= ParseChanged;
				}
				*/
			}
		}

		public SourceEditorBuffer (SourceEditorView view) : this ()
		{
			this.view = view;
		}
		
		public SourceEditorBuffer () : base (new SourceTagTable ())
		{
			markup = new TextTag ("breakpoint");
			markup.Background = "yellow";
			TagTable.Add (markup);

			complete_ahead = new TextTag ("complete_ahead");
			complete_ahead.Foreground = "grey";
			TagTable.Add (complete_ahead);

			compilation_error = new TextTag ("compilation_error");
			compilation_error.Underline = Pango.Underline.Error;
			TagTable.Add (compilation_error);

			complete_end = CreateMark (null, StartIter, true);
			highlightLineTag = new TextTag ("highlightLine");
			highlightLineTag.Background = "lightgrey";
			TagTable.Add (highlightLineTag);
		}
		
		public override void Dispose ()
		{
			Language = null;
			base.Dispose ();
		}

		void ParseChanged (object o, ParseInformationEventArgs e)
		{
			if (view != null && view.ParentEditor.DisplayBinding.ContentName == e.FileName)
			{
				RemoveTag (compilation_error, StartIter, EndIter);

				if (e.ParseInformation.MostRecentCompilationUnit.ErrorsDuringCompile)
					DrawErrors (e.ParseInformation.MostRecentCompilationUnit.ErrorInformation);
			}
		}

		void DrawErrors (ErrorInfo[] errors)
		{
			foreach (ErrorInfo error in errors)
				DrawError (error.Line - 1, error.Column - 1);
		}

		// FIXME: underlines under keywords get ignored
		// because we class with gtksourceview
		void DrawError (int line, int column)
		{
			TextIter start = GetIterAtLine (line);

			// FIXME: why is this necessary
			if (column < start.CharsInLine) {
				start.LineOffset = column;
			}
			else {
				start.LineOffset = start.CharsInLine;
			}

			// FIXME: sometimes this is wrong
			start.BackwardWordStart ();

			TextIter end = start;
			end.ForwardWordEnd ();

			//Console.WriteLine ("underline error: {0}", GetText (start, end, false));
			//if (GetText (start, end, false).Trim () != "")
				ApplyTag (compilation_error, start, end);
			//else
			//	Console.WriteLine ("something didn't work");
		}		
		
		public void MarkupLine (int linenumber)
		{
			TextIter begin_line = GetIterAtLine (linenumber);
			TextIter end_line = begin_line;
			begin_line.LineOffset = 0;
			end_line.ForwardToLineEnd ();
			ApplyTag (markup, begin_line, end_line);
		}
		
		public void UnMarkupLine (int line)
		{
			ClearMarks (SourceMarkerType.ExecutionMark);
			RemoveTag (markup, StartIter, EndIter);
		}
		
		public void HighlightLine (int linenumber)
		{
			TextIter begin_line = GetIterAtLine (linenumber);
			TextIter end_line = begin_line;
			begin_line.LineOffset = 0;
			end_line.ForwardToLineEnd ();
			ApplyTag (highlightLineTag, begin_line, end_line);
			highlightLine = linenumber;
		}
		
		public void HideHighlightLine ()
		{
			if (highlightLine != -1) {
				RemoveTag (highlightLineTag, StartIter, EndIter);
				highlightLine = -1;
			}
		}
		
		public void DropCompleteAhead ()
		{
			if (GetIterAtMark (complete_end).Offset == 0)
				return;
			RemoveTag (complete_ahead, GetIterAtMark (InsertMark), GetIterAtMark (complete_end));
			TextIter insertIter = GetIterAtMark (InsertMark);
			TextIter completionEnd = GetIterAtMark (complete_end);
			Delete (ref insertIter, ref completionEnd);
			MoveMark (complete_end, GetIterAtOffset (0));
		}

		public void CompleteAhead (string what)
		{
			DropCompleteAhead ();
			TextIter insertIter = GetIterAtMark (InsertMark);
			InsertWithTags (ref insertIter, what, new TextTag[] 
							{ complete_ahead });
			TextIter it = GetIterAtMark (InsertMark);
			MoveMark (complete_end, it);
			it.BackwardChars (what.Length);
			PlaceCursor (it);
		}

		public void StartAtomicUndo ()
		{
			atomic_undo = new AtomicUndo (this);
		}

		public void EndAtomicUndo ()
		{
			atomic_undo.Dispose ();
		}
		
		public void LoadFile (string file, string mime)
		{
			StreamReader sr = System.IO.File.OpenText (file);
			LoadText (sr.ReadToEnd (), mime);		
			sr.Close ();			
		}
		
		public void LoadFile (string file)
		{
			using (NoUndo n = new NoUndo (this)) {
				StreamReader sr = System.IO.File.OpenText (file);
				LoadText(sr.ReadToEnd ());
				sr.Close ();
			}
		}

		// needed to make sure the text is valid
		[DllImport("libglib-2.0-0.dll")]
		static extern bool g_utf8_validate(string text, int textLength, IntPtr end);
		
		public void LoadText (string text, string mime)
		{
			SourceLanguage lang = svs.GetLanguageFromMimeType (mime);
			if (lang != null) 
				Language = lang;

			LoadText(text);
		}
		

		//
		// NOTE: Text is set to null if the file could not be loaded (i.e. not valid utf8 text
		//
		public void LoadText (string text)
		{
			if (g_utf8_validate (text, text.Length, IntPtr.Zero))
			{
				using (NoUndo n = new NoUndo (this))
					Text = text;
			}
			else
			{
				using (NoUndo n = new NoUndo (this))
					Text = null;
			}

			Modified = false;
			ScrollToTop ();			
		}

		void ScrollToTop ()
		{
			PlaceCursor (StartIter);
			if (View != null) {
				View.ScrollMarkOnscreen (InsertMark);
				GLib.Timeout.Add (20, new TimeoutHandler (changeFocus));
			}
		}

		bool changeFocus ()
		{
			View.GrabFocus ();
			return false;
		}
		
		public void Save (string fileName)
		{
			TextWriter s = new StreamWriter (fileName, false);
			s.Write (Text);
			s.Close ();
			Modified = false;
		}

#region IClipboardHandler
		bool HasSelection
		{
			get {
				TextIter dummy, dummy2;
				return GetSelectionBounds (out dummy, out dummy2);
			}
		}

		public string GetSelectedText ()
		{
			if (HasSelection)
			{
				TextIter select1, select2;
				GetSelectionBounds (out select1, out select2);
				return GetText (select1, select2, true);
			}
			
			return String.Empty;
		}
		
		bool IClipboardHandler.EnableCut
		{
			get { return true; }
		}
		
		bool IClipboardHandler.EnableCopy
		{
			get { return true; }
		}
		
		bool IClipboardHandler.EnablePaste
		{
			get {
				return true;
			}
		}
		
		bool IClipboardHandler.EnableDelete
		{
			get { return true; }
		}
		
		bool IClipboardHandler.EnableSelectAll
		{
			get { return true; }
		}
		
		void IClipboardHandler.Cut (object sender, EventArgs e)
		{
			if (HasSelection)
				CutClipboard (clipboard, true);
		}
		
		void IClipboardHandler.Copy (object sender, EventArgs e)
		{
			if (HasSelection)
				CopyClipboard (clipboard);
		}
		
		void IClipboardHandler.Paste (object sender, EventArgs e)
		{
			if (clipboard.WaitIsTextAvailable ()) {
				PasteClipboard (clipboard);
				View.ScrollMarkOnscreen (InsertMark);
			}
		}
		
		void IClipboardHandler.Delete (object sender, EventArgs e)
		{
			if (HasSelection)
				DeleteSelection (true, true);
		}
		
		void IClipboardHandler.SelectAll (object sender, EventArgs e)
		{
			// Sadly, this is not in our version of the bindings:
			//
			//gtk_text_view_select_all (GtkWidget *widget,
			//			  gboolean select)
			//{
			//	gtk_text_buffer_get_bounds (buffer, &start_iter, &end_iter);
			//	gtk_text_buffer_move_mark_by_name (buffer, "insert", &start_iter);
			//	gtk_text_buffer_move_mark_by_name (buffer, "selection_bound", &end_iter);
			
			MoveMark ("insert", StartIter);
			MoveMark ("selection_bound", EndIter);
		}
		
		Gtk.Clipboard clipboard = Gtk.Clipboard.Get (Gdk.Atom.Intern ("CLIPBOARD", false));
#endregion

#region Bookmark Operations
		
		//
		// GtkSourceView made this difficult because they took over
		// the TextMark type for their SourceMarker, so we have to marshall manually. 
		//
		// http://bugzilla.gnome.org/show_bug.cgi?id=132525
		//
		[DllImport("gtksourceview-1.0")]
		static extern IntPtr gtk_source_buffer_get_markers_in_region (IntPtr raw, ref Gtk.TextIter begin, ref Gtk.TextIter end);
		
		[DllImport("gtksourceview-1.0")]
		static extern IntPtr gtk_source_buffer_create_marker(IntPtr raw, string name, string type, ref Gtk.TextIter where);

		[DllImport("gtksourceview-1.0")]
		static extern void gtk_source_buffer_delete_marker(IntPtr raw, IntPtr marker);
		
		[DllImport("glibsharpglue-2")]
		static extern IntPtr gtksharp_slist_get_data (IntPtr l);

		[DllImport("glibsharpglue-2")]
		static extern IntPtr gtksharp_slist_get_next (IntPtr l);
		
		[DllImport("gtksourceview-1.0")]
		static extern IntPtr gtk_source_marker_get_marker_type(IntPtr raw);
		
		[DllImport("libglib-2.0-0.dll")]
		static extern void g_slist_free (IntPtr l);
		
		[DllImport("gtksourceview-1.0")]
		static extern void gtk_source_buffer_get_iter_at_marker (IntPtr raw, ref Gtk.TextIter iter, IntPtr marker);
		
		public void ToggleBookmark ()
		{
			ToggleBookmark (GetIterAtMark (InsertMark).Line);
		}
		
		public bool IsBookmarked (int linenum)
		{
			return IsMarked (linenum, SourceMarkerType.SourceEditorBookmark);
		}

		public bool IsBreakpoint (int linenum)
		{
			return IsMarked (linenum, SourceMarkerType.BreakpointMark);
		}
		
		public bool IsMarked (int linenum, SourceMarkerType type)
		{
			TextIter insert = GetIterAtLine (linenum);
			TextIter begin_line = insert, end_line = insert;
			begin_line.LineOffset = 0;

			while (! end_line.EndsLine ())
				end_line.ForwardChar ();
			
			IntPtr lst = gtk_source_buffer_get_markers_in_region (Handle, ref begin_line, ref end_line);

			bool fnd_marker = false;
			
			IntPtr current = lst;
			while (current != IntPtr.Zero)
			{
				IntPtr data = gtksharp_slist_get_data (current);
				IntPtr nm = gtk_source_marker_get_marker_type (data);

				string name = GLib.Marshaller.PtrToStringGFree (nm);
				if (name == type.ToString ()) {
					fnd_marker = true;
					break;
				}
				current = gtksharp_slist_get_next (current);
			}

			if (lst != IntPtr.Zero)
				g_slist_free (lst);

			return fnd_marker;
		}

		public void ToggleBookmark (int linenum)
		{
			ToggleMark (linenum, SourceMarkerType.SourceEditorBookmark);
		}
		
		public void ToggleMark (int linenum, SourceMarkerType type)
		{
			TextIter insert = GetIterAtLine (linenum);
			TextIter begin_line = insert, end_line = insert;
			begin_line.LineOffset = 0;
			
			while (! end_line.EndsLine ())
				end_line.ForwardChar ();
			
			
			IntPtr lst = gtk_source_buffer_get_markers_in_region (Handle, ref begin_line, ref end_line);

			bool found_marker = false;
			
			//
			// The problem is that the buffer owns the
			// reference to the marker. So, if we use the nice Gtk# stuff, we get
			// a problem when we dispose it later. Thus we must basically write this
			// in C.
			// FIXME: Is there a bug# for this?
			
			IntPtr current = lst;
			while (current != IntPtr.Zero) {
				
				IntPtr data = gtksharp_slist_get_data (current);
				IntPtr nm = gtk_source_marker_get_marker_type (data);
				string name = GLib.Marshaller.PtrToStringGFree (nm);
				if (name == type.ToString ()) {
					gtk_source_buffer_delete_marker (Handle, data);
					found_marker = true;
				}
				
				current = gtksharp_slist_get_next (current);
			}
			
			if (lst != IntPtr.Zero)
				g_slist_free (lst);
			
			if (found_marker)
				return;
			
			switch (type.ToString ()) {
				case "ExecutionMark":
					begin_line.LineOffset = 2;
					break;
				case "BreakpointMark":
					begin_line.LineOffset = 1;
					break;
			}

			gtk_source_buffer_create_marker (Handle, null, type.ToString (), ref begin_line);
		}
		
		[DllImport("gtksourceview-1.0")]
		static extern IntPtr gtk_source_buffer_get_prev_marker(IntPtr raw, ref Gtk.TextIter iter);
		
		[DllImport("gtksourceview-1.0")]
		static extern IntPtr gtk_source_buffer_get_last_marker(IntPtr raw);
		
		[DllImport("gtksourceview-1.0")]
		static extern IntPtr gtk_source_marker_prev (IntPtr raw);
		
		public void PrevBookmark ()
		{
			TextIter loc = GetIterAtMark (InsertMark);
			int ln = loc.Line;
			
			IntPtr prevMarker = gtk_source_buffer_get_prev_marker (Handle, ref loc);
			//IntPtr firstMarker = prevMarker;
			bool first = true;
			while (true) {
				// Thats a wrap!
				if (prevMarker == IntPtr.Zero)
					prevMarker = gtk_source_buffer_get_last_marker (Handle);
				
				// no markers
				if (prevMarker == IntPtr.Zero)
					return;
				
				IntPtr nm = gtk_source_marker_get_marker_type (prevMarker);
				string name = GLib.Marshaller.PtrToStringGFree (nm);
				if (name == "SourceEditorBookmark") {
					gtk_source_buffer_get_iter_at_marker (Handle, ref loc, prevMarker);
					
					if (! first || loc.Line != ln)
						break;
				}
				
				prevMarker = gtk_source_marker_prev (prevMarker);
				
				first = false;
			}
			
			PlaceCursor (loc);
		}
		
		[DllImport("gtksourceview-1.0")]
		static extern IntPtr gtk_source_buffer_get_first_marker (IntPtr raw);
		
		[DllImport("gtksourceview-1.0")]
		static extern IntPtr gtk_source_buffer_get_next_marker(IntPtr raw, ref Gtk.TextIter iter);
		
		[DllImport("gtksourceview-1.0")]
		static extern IntPtr gtk_source_marker_next(IntPtr raw);
		
		public void NextBookmark ()
		{
			TextIter loc = GetIterAtMark (InsertMark);
			int ln = loc.Line;
			
			IntPtr nextMarker = gtk_source_buffer_get_next_marker (Handle, ref loc);
			//IntPtr firstMarker = nextMarker;
			bool first = true;
			while (true) {
				// Thats a wrap!
				if (nextMarker == IntPtr.Zero)
					nextMarker = gtk_source_buffer_get_first_marker (Handle);
				
				// no markers
				if (nextMarker == IntPtr.Zero)
					return;
				
				IntPtr nm = gtk_source_marker_get_marker_type (nextMarker);
				string name = GLib.Marshaller.PtrToStringGFree (nm);
				if (name == "SourceEditorBookmark") {
					gtk_source_buffer_get_iter_at_marker (Handle, ref loc, nextMarker);
					
					if (! first || loc.Line != ln)
						break;
				}
				
				nextMarker = gtk_source_marker_next (nextMarker);
				
				first = false;
			}
			
			PlaceCursor (loc);
		}

		public void ClearBookmarks ()
		{
			ClearMarks (SourceMarkerType.SourceEditorBookmark);
		}
		
		public void ClearMarks (SourceMarkerType type)
		{
			TextIter begin = StartIter;
			TextIter end = EndIter;
			IntPtr lst = gtk_source_buffer_get_markers_in_region (Handle, ref begin, ref end);
			
			IntPtr current = lst;
			while (current != IntPtr.Zero) {
				
				IntPtr data = gtksharp_slist_get_data (current);
				IntPtr nm = gtk_source_marker_get_marker_type (data);
				string name = GLib.Marshaller.PtrToStringGFree (nm);
				if (name == type.ToString ())
					gtk_source_buffer_delete_marker (Handle, data);
				
				current = gtksharp_slist_get_next (current);
			}
			
			if (lst != IntPtr.Zero)
				g_slist_free (lst);
		}
#endregion

#region ITextBufferStrategy compat interface, this should be removed ASAP

		public int Length
		{
			get { return EndIter.Offset + 1; }
		}

		public char GetCharAt (int offset)
		{
			/*if (offset < 0)
			  offset = 0;
			  TextIter begin_iter = GetIterAtOffset (offset);
			  TextIter next_iter = begin_iter;
			  next_iter.ForwardChar ();
			  string text = GetText (begin_iter, next_iter, true);
			  if (text != null && text.Length >= 1)
			  return text[0];*/
			//New test implementation
			if (offset < 0)
				offset = 0;
			TextIter iter = GetIterAtOffset (offset);
			if (iter.Equals (TextIter.Zero))
				return ' ';
			if (iter.Char == null || iter.Char.Length == 0)
				return ' ';
			return iter.Char[0];
		}

		public string GetText (int start, int length)
		{
			TextIter begin_iter = GetIterAtOffset (start);
			TextIter end_iter = GetIterAtOffset (start + length);
			return GetText (begin_iter, end_iter, true);
		}

		public void Insert (int offset, string text)
		{
			TextIter put = GetIterAtOffset (offset);
			Insert (ref put, text);
		}

		public int GetLowerSelectionBounds ()
		{
			if (HasSelection)
			{
				TextIter select1, select2;
				GetSelectionBounds (out select1, out select2);
				return select1.Offset > select2.Offset ? select2.Offset : select1.Offset;
			}
			return 0;
		}

		public void Delete (int offset, int length)
		{
			TextIter start = GetIterAtOffset (offset);
			TextIter end = GetIterAtOffset (offset + length);
			Delete (ref start, ref end);
		}

		public void Replace (int offset, int length, string pattern)
		{
			Delete (offset, length);
			Insert (offset, pattern);
		}

		public static SourceEditorBuffer CreateTextBufferFromFile (string filename)
		{
			SourceEditorBuffer buff = new SourceEditorBuffer ();
			buff.LoadFile (filename);
			// don't return a buffer that couldn't load the file
			if (buff.Text == null) {
				return null;
			} else {
				return buff;
			}
		}

#endregion

#region ICodeStyleOperations
		public void CommentCode ()
		{
			string commentTag = "//"; // as default
			commentTag = Runtime.Languages.GetBindingPerFileName (WorkbenchSingleton.Workbench.ActiveWorkbenchWindow.ViewContent.ContentName).CommentTag;
			
			TextIter textStart;
			TextIter textEnd;
			GetSelectionBounds (out textStart, out textEnd);
			if (textStart.Line == textEnd.Line)
			{ // all the code is in one line, just comment the select text
				textStart.LineOffset = 0;
				Insert (ref textStart, commentTag);
			}
			else
			{ // comment the entire lines
				int numberOfLines = textEnd.Line - textStart.Line + 1;
				TextMark mTextStart = CreateMark (null, textStart, true);
				TextMark mTextTmp = mTextStart;
				
				for (int i=0; i<numberOfLines; i++)
				{
					TextIter textTmp = GetIterAtMark (mTextTmp);
					// add the comment tag
					textTmp.LineOffset = 0;
					Insert (ref textTmp, commentTag);
					// setup a mark on next line
					textTmp = GetIterAtMark (mTextTmp);
					textTmp.ForwardLine ();
					mTextTmp = CreateMark (null, textTmp, true);
				}			
			}
		}
		
		public void UncommentCode ()
		{
			string commentTag = "//"; // as default
			commentTag = Runtime.Languages.GetBindingPerFileName (WorkbenchSingleton.Workbench.ActiveWorkbenchWindow.ViewContent.ContentName).CommentTag;
			
			TextIter textStart;
			TextIter textEnd;
			GetSelectionBounds (out textStart, out textEnd);
			if (textStart.Line == textEnd.Line)
			{ // all the code is in one line, just umcomment is text starts with comment tag
				textStart.LineOffset = 0;
				textEnd = textStart;
				textEnd.ForwardChars (commentTag.Length);
				if (textStart.GetText (textEnd) == commentTag)
					Delete (ref textStart, ref textEnd);
			}
			else
			{ // uncomment the entire lines
				int numberOfLines = textEnd.Line - textStart.Line + 1;
				TextMark mTextStart = CreateMark (null, textStart, true);
				TextMark mTextTmp = mTextStart;
				
				for (int i=0; i<numberOfLines; i++)
				{
					TextIter textTmp = GetIterAtMark (mTextTmp);
					// delete the comment tag
					textTmp.LineOffset = 0;
										
					// the user can have spaces at start of line, handling that
					TextIter textLineEnd = textTmp;
					textLineEnd.ForwardToLineEnd ();
					string trimmedText, fullText;
					int spaces;
					fullText = textTmp.GetText (textLineEnd);
					trimmedText = fullText.TrimStart ();
					spaces = fullText.Length - trimmedText.Length;
					if (trimmedText.StartsWith (commentTag))
					{
						textTmp.ForwardChars (spaces);
						textEnd = textTmp;
						textEnd.ForwardChars (commentTag.Length);
						Delete (ref textTmp, ref textEnd);
					}
					
					// setup a mark on next line
					textTmp = GetIterAtMark (mTextTmp);
					textTmp.ForwardLine ();
					mTextTmp = CreateMark (null, textTmp, true);
				}			
			}			
		}
#endregion
		
		public bool GotoSelectionEnd ()
		{
			TextIter textStart;
			TextIter textEnd;
			if (GetSelectionBounds (out textStart, out textEnd))
			{
				MoveMark (SelectionBound, textEnd);
				MoveMark (InsertMark, textEnd);
				return true;
			}
			return false;
		}
		
		public bool GotoSelectionStart ()
		{
			TextIter textStart;
			TextIter textEnd;
			if (GetSelectionBounds (out textStart, out textEnd))
			{
				MoveMark (SelectionBound, textStart);
				MoveMark (InsertMark, textStart);
				return true;
			}
			return false;
		}
	}
}
