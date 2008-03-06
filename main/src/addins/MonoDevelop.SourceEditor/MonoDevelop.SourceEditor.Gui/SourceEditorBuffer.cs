using Gtk;
using GLib;

using MonoDevelop.Core.Gui;
using MonoDevelop.Ide.Gui.Content;
using MonoDevelop.Core;
using Mono.Addins;
using MonoDevelop.Projects;
using MonoDevelop.Projects.Parser;
using MonoDevelop.Projects.Text;
using MonoDevelop.Ide.Gui;

using System;
using System.IO;
using System.Text;
using System.Collections;
using System.Runtime.InteropServices;

using GtkSourceView;
	
namespace MonoDevelop.SourceEditor.Gui
{/* FIXME GTKSV 2
	public enum SourceMarkerType
	{
		SourceEditorBookmark,
		BreakpointMark,
		ExecutionMark
	}
*/
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
		TextTag markup;
		TextTag complete_ahead;
		TextTag compilation_error;
		TextMark complete_end;
		TextTag highlightLineTag;
		AtomicUndo atomic_undo;
		SourceEditorView view;
		int highlightLine = -1;
		string sourceEncoding;
		
		protected SourceEditorBuffer (IntPtr ptr): base (ptr)
		{
		}

		public SourceEditorView View
		{
			get { return view; }
			set { view = value; }
		}

		public string SourceEncoding {
			get { return sourceEncoding; }
			set { sourceEncoding = value; }
		}
		
		public SourceEditorBuffer (SourceEditorView view) : this ()
		{
			this.view = view;
		}
		
		public SourceEditorBuffer () : base (new TextTagTable ())
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
			
			MaxUndoLevels = 1000;
			
			base.InsertText += OnInsertText;
			base.DeleteRange += onDeleteRangeAfter;
			base.DeleteRange += onDeleteRangeBefore;
		}
		
		void OnInsertText (object sender, InsertTextArgs args)
		{
			int lines = 0;
			for (int i = 0; i < args.Text.Length; i++) {
				if (args.Text [i] == '\n')
					lines++;
			}
			TextIter iter = this.GetIterAtOffset (args.Pos.Offset - args.Length);
			if (lines != 0)
				OnLineCountChanged (iter.Line, lines, iter.LineOffset);
			
			OnTextChanged (args.Pos.Offset, args.Pos.Offset + args.Length);
		}
		
		bool onTextChangedFiring = false;
		protected void OnTextChanged (int startOffset, int endOffset)
		{
			if (onTextChangedFiring)
				throw new InvalidOperationException ("Cannot modify the text buffer within a TextChanged event handler");
			
			if (TextChanged != null) {
				onTextChangedFiring = true;
				TextChanged (this, new TextChangedEventArgs (startOffset, endOffset));
				onTextChangedFiring = false;
			}
		}
		
		public event EventHandler<TextChangedEventArgs> TextChanged;
		
		public delegate void LineCountChange (int line, int count, int column);
		public event LineCountChange LineCountChanged;
		protected virtual void OnLineCountChanged (int line, int count, int column)
		{
			if (LineCountChanged != null)
				LineCountChanged (line, count, column);
		}
		
		//HACK: Can't use an override, as it should use ref TextIter parameters, but GTK# doesn't.
		//bug https://bugzilla.novell.com/show_bug.cgi?id=341762
		//protected override void OnDeleteRange (TextIter start, TextIter end)
		//Hence we have to have two event handlers; one to get state before the delete, 
		//and the other to fire the OnTextChanged event afterwards
		[GLib.ConnectBefore]
		void onDeleteRangeBefore (object sender, DeleteRangeArgs args)
		{
			onDeleteRangeStartLine = args.Start.Line;
			onDeleteRangeEndLine = args.End.Line;
			onDeleteRangeStartCol = args.Start.LineOffset;
			onDeleteRangeStartIndex = args.Start.Offset;
			onDeleteRangeEndIndex = args.End.Offset;
		}
		
		int onDeleteRangeStartLine = -1, onDeleteRangeEndLine = -1, onDeleteRangeStartCol = -1;
		int onDeleteRangeStartIndex = -1, onDeleteRangeEndIndex = -1;
		
		void onDeleteRangeAfter (object sender, DeleteRangeArgs args)
		{
			// We want the count to be negative here if lines were removed.
			int count = onDeleteRangeStartLine - onDeleteRangeEndLine;
			if (count != 0) 
				OnLineCountChanged (onDeleteRangeStartLine, count, onDeleteRangeStartCol);
			OnTextChanged (onDeleteRangeStartIndex, onDeleteRangeEndIndex);
		}
		
		public override void Dispose ()
		{
			base.InsertText -= OnInsertText;
			base.DeleteRange -= onDeleteRangeAfter;
			base.DeleteRange -= onDeleteRangeBefore;
			
			Language = null;
			base.Dispose ();
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
			//FIXME GTKSV2
			//ClearMarks (SourceMarkerType.ExecutionMark);
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
		
		public void LoadFile (string file)
		{
			LoadFile (file, null);
		}
		
		public void LoadFile (string fileName, string mime)
		{
			LoadFile (fileName, mime, null);
		}
		
		public void LoadFile (string fileName, string mime, string encoding)
		{
			TextFile file = TextFile.ReadFile (fileName, encoding);
			sourceEncoding = file.SourceEncoding;
			LoadText (file.Text, mime);
		}
		
		public void LoadText (string text, string mime)
		{
			if (mime != null) {
				SourceLanguage lang = SourceViewService.GetLanguageFromMimeType (mime);
				if (lang != null) 
					Language = lang;
			}
			
			using (NoUndo n = new NoUndo (this)) {
				Text = text;
			}
			
			Modified = false;
			ScrollToTop ();
		}

		void ScrollToTop ()
		{
			PlaceCursor (StartIter);
			if (View != null) {
//				View.ScrollMarkOnscreen (InsertMark);
//				GLib.Timeout.Add (20, new TimeoutHandler (changeFocus));
			}
		}

		protected bool changeFocus ()
		{
			View.GrabFocus ();
			return false;
		}
		
		public void Save (string fileName, string encoding)
		{
			// This is workaround for Mono bug #77423.
			TextWriter s = new StreamWriter (fileName, true);
			s.Close ();
			
			TextFile.WriteFile (fileName, Text, SourceEncoding);
			Modified = false;
		}

#region IClipboardHandler
		// FIXME: remove when we depend on gtk > 2.10
		bool _HasSelection
		{
			get {
				TextIter dummy, dummy2;
				return GetSelectionBounds (out dummy, out dummy2);
			}
		}

		public string GetSelectedText ()
		{
			if (_HasSelection)
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
		
		void IClipboardHandler.Cut ()
		{
			if (_HasSelection)
				CutClipboard (clipboard, true);
		}
		
		void IClipboardHandler.Copy ()
		{
			if (_HasSelection)
				CopyClipboard (clipboard);
		}
		
		void IClipboardHandler.Paste ()
		{
			if (clipboard.WaitIsTextAvailable ()) {
				PasteClipboard (clipboard);
				View.ScrollMarkOnscreen (InsertMark);
			}
		}
		
		void IClipboardHandler.Delete ()
		{
			if (_HasSelection)
				DeleteSelection (true, true);
			else 
				this.Delete (GetIterAtMark (InsertMark).Offset, 1);
		}
		
		void IClipboardHandler.SelectAll ()
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
		/* fixme gtksv 2.0
		[DllImport("libgtksourceview-1.0.so.0")]
		static extern IntPtr gtk_source_buffer_get_markers_in_region (IntPtr raw, ref Gtk.TextIter begin, ref Gtk.TextIter end);
		
		[DllImport("libgtksourceview-1.0.so.0")]
		static extern IntPtr gtk_source_buffer_create_marker(IntPtr raw, string name, string type, ref Gtk.TextIter where);

		[DllImport("libgtksourceview-1.0.so.0")]
		static extern void gtk_source_buffer_delete_marker(IntPtr raw, IntPtr marker);
		
		[DllImport("libglibsharpglue-2.so")]
		static extern IntPtr gtksharp_slist_get_data (IntPtr l);

		[DllImport("libglibsharpglue-2.so")]
		static extern IntPtr gtksharp_slist_get_next (IntPtr l);
		
		[DllImport("libgtksourceview-1.0.so.0")]
		static extern IntPtr gtk_source_marker_get_marker_type(IntPtr raw);
		
		[DllImport("libglib-2.0.so")]
		static extern void g_slist_free (IntPtr l);
		
		[DllImport("libgtksourceview-1.0.so.0")]
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
		}*/
		/* FIXME GTKSV 2.0
		[DllImport("libgtksourceview-1.0.so.0")]
		static extern IntPtr gtk_source_buffer_get_prev_marker(IntPtr raw, ref Gtk.TextIter iter);
		
		[DllImport("libgtksourceview-1.0.so.0")]
		static extern IntPtr gtk_source_buffer_get_last_marker(IntPtr raw);
		
		[DllImport("libgtksourceview-1.0.so.0")]
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
		}*/
		/* FIXME GTKSV 2.0
		[DllImport("libgtksourceview-1.0.so.0")]
		static extern IntPtr gtk_source_buffer_get_first_marker (IntPtr raw);
		
		[DllImport("libgtksourceview-1.0.so.0")]
		static extern IntPtr gtk_source_buffer_get_next_marker(IntPtr raw, ref Gtk.TextIter iter);
		
		[DllImport("libgtksourceview-1.0.so.0")]
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
		}*/
#endregion

#region ITextBufferStrategy compat interface, this should be removed ASAP

		public int Length
		{
			get { return EndIter.Offset + 1; }
		}

		public char GetCharAt (int offset)
		{
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
			if (_HasSelection)
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
			TextMark curMark, endMark;
			TextIter start, end;
			string commentTag;
			
			if (GetSelectionBounds (out start, out end)) {
				//selection; can contain multiple lines
				// Don't comment lines where no chars are actually selected (fixes bug #81632)
				if (end.LineOffset == 0)
					end.BackwardLine ();
			}
			
			ILanguageBinding binding = Services.Languages.GetBindingPerFileName (IdeApp.Workbench.ActiveDocument.FileName);
			commentTag = "//";
			if (binding != null && binding.CommentTag != null)
				commentTag = binding.CommentTag;
			
			start.LineOffset = 0;
			
			endMark = CreateMark (null, end, false);
			using (new AtomicUndo (this)) {
				while (start.Line <= end.Line) {
					curMark = CreateMark (null, start, true);
					Insert (ref start, commentTag);
					start = GetIterAtMark (curMark);
					end = GetIterAtMark (endMark);
					start.ForwardLine ();
				}
			}
		}
		
		public void UncommentCode ()
		{
			string commentTag = "//"; // as default
			commentTag = Services.Languages.GetBindingPerFileName (IdeApp.Workbench.ActiveDocument.FileName).CommentTag;
			
			TextIter textStart;
			TextIter textEnd;
			GetSelectionBounds (out textStart, out textEnd);
			
			int numberOfLines = textStart.Line == textEnd.Line ? 1 : textEnd.Line - textStart.Line + 1;
			TextMark mTextStart = CreateMark (null, textStart, true);
			TextMark mTextTmp = mTextStart;
			
			using (new AtomicUndo (this)) {
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
