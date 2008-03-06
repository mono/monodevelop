//
// MonoDevelop XML Editor
//
// Copyright (C) 2006 Matthew Ward
// Copyright (C) 2004-2006 MonoDevelop Team
//

using Gdk;
using Gtk;
using GtkSourceView;
using MonoDevelop.Ide.Gui.Content;
using MonoDevelop.Projects.Gui.Completion;
using System;
using System.Text;

namespace MonoDevelop.XmlEditor
{
	public class XmlEditorView : SourceView, ICompletionWidget, IClipboardHandler
	{	
		XmlSchemaCompletionDataCollection schemaCompletionDataItems = new XmlSchemaCompletionDataCollection();
		XmlSchemaCompletionData defaultSchemaCompletionData = null;
		string defaultNamespacePrefix = String.Empty;
		bool autoCompleteElements = true;
		bool showSchemaAnnotation = true;
		SourceBuffer buffer;
		Gtk.Clipboard clipboard = Gtk.Clipboard.Get(Gdk.Atom.Intern("CLIPBOARD", false));

		public XmlEditorView()
		{
			InitSyntaxHighlighting();
		}
		
		/// <summary>
		/// Gets the schemas that the xml editor will use.
		/// </summary>
		/// <remarks>
		/// Probably should have NOT a 'set' property, but allowing one
		/// allows us to share the completion data amongst multiple
		/// xml editor controls.
		/// </remarks>
		public XmlSchemaCompletionDataCollection SchemaCompletionDataItems {
			get {
				return schemaCompletionDataItems;
			}
			set {
				schemaCompletionDataItems = value;
			}
		}
		
		/// <summary>
		/// Gets or sets the default namespace prefix.
		/// </summary>
		public string DefaultNamespacePrefix {
			get {
				return defaultNamespacePrefix;
			}
			set {
				defaultNamespacePrefix = value;
			}
		}
		
		/// <summary>
		/// Gets or sets the default schema completion data associated with this
		/// view.
		/// </summary>
		public XmlSchemaCompletionData DefaultSchemaCompletionData {
			get {
				return defaultSchemaCompletionData;
			}
			
			set {
				defaultSchemaCompletionData = value;
			}
		}	
		
		public bool AutoCompleteElements {
			get {
				return autoCompleteElements;
			}
			set {
				autoCompleteElements = value;
			}
		}	
		
		public bool ShowSchemaAnnotation {
			get {
				return showSchemaAnnotation;
			}
			set {
				showSchemaAnnotation = value;
			}
		}
		
		public void UndoChange()
		{
			if (buffer.CanUndo()) {
				buffer.Undo();
				ScrollToCursor();
			}	
		}	
			
		public void RedoChange()
		{
			if (buffer.CanRedo()) {
				buffer.Redo();
				ScrollToCursor();
			}
		}
		
		protected override bool OnFocusOutEvent(EventFocus e)
		{
			XmlCompletionListWindow.HideWindow();
			return base.OnFocusOutEvent(e);
		}

		protected override bool OnKeyPressEvent (Gdk.EventKey evnt)
		{
			if (XmlCompletionListWindow.ProcessKeyEvent (evnt))
				return true;
									
			try {
				bool result;
				switch (evnt.Key) {
					case Gdk.Key.less:
					case Gdk.Key.space:
					case Gdk.Key.equal:
						result = base.OnKeyPressEvent(evnt);
						ShowCompletionWindow((char)evnt.KeyValue);
						return result;
					case Gdk.Key.greater:
						result = base.OnKeyPressEvent(evnt);
						if (autoCompleteElements) {
							AutoCompleteElement();
						}
						return result;
					case Gdk.Key.Return:
						result = base.OnKeyPressEvent(evnt);
						IndentLine();
						return result;
					default:
						if (XmlParser.IsAttributeValueChar((char)evnt.KeyValue)) {
							if (IsInsideQuotes()) {
								// Show the completion window then get it to
								// process the key event so it selects the list item
								// that starts with the character just typed in.
								ShowCompletionWindow((char)evnt.KeyValue);
								XmlCompletionListWindow.ProcessKeyEvent(evnt);
								return base.OnKeyPressEvent(evnt);;
							}
						}
						break;
				}
			} catch (Exception ex) {
				Console.WriteLine(String.Concat("EXCEPTION: ", ex));
			}
			return base.OnKeyPressEvent(evnt);
		}
		
		void ShowCompletionWindow(char key)
		{
			PrepareCompletionDetails (Buffer.GetIterAtMark (Buffer.InsertMark));
			XmlCompletionListWindow.ShowWindow(key, GetCompletionDataProvider(), this);
			//if (EnableCodeCompletion && PeekCharIsWhitespace ()) {
			//	PrepareCompletionDetails (buf.GetIterAtMark (buf.InsertMark));
			//	CompletionListWindow.ShowWindow (key, GetCodeCompletionDataProvider (false), this);
			//}
		}
		
		ICompletionDataProvider GetCompletionDataProvider()
		{
			return new XmlCompletionDataProvider(schemaCompletionDataItems, defaultSchemaCompletionData, defaultNamespacePrefix);
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
			this.triggerMark = Buffer.CreateMark (null, iter, true);
		}
		
		#region ICompletionWidget

		int completionX;
		int ICompletionWidget.TriggerXCoord {
			get	{
				return completionX;
			}
		}

		int completionY;
		int ICompletionWidget.TriggerYCoord {
			get	{
				return completionY;
			}
		}

		int textHeight;
		int ICompletionWidget.TriggerTextHeight {
			get	{
				return textHeight;
			}
		}

		string ICompletionWidget.CompletionText {
			get	{
				string completionText = Buffer.GetText (Buffer.GetIterAtMark (triggerMark), Buffer.GetIterAtMark (Buffer.InsertMark), false);
				return completionText;
			}
		}

		void ICompletionWidget.SetCompletionText (string partial_word, string complete_word)
		{
			TextIter offsetIter = Buffer.GetIterAtMark(triggerMark);
        	TextIter endIter = Buffer.GetIterAtOffset (offsetIter.Offset + partial_word.Length);
        	Buffer.MoveMark (Buffer.InsertMark, offsetIter);
        	Buffer.Delete (ref offsetIter, ref endIter);
        	Buffer.InsertAtCursor (complete_word);
		}

		void ICompletionWidget.InsertAtCursor (string text)
		{
			Buffer.InsertAtCursor (text);
		}
		
		string ICompletionWidget.Text {
			get	{
				string text = Buffer.Text;
				return text;
			}
		}

		int ICompletionWidget.TextLength {
			get	{
				return Buffer.EndIter.Offset + 1;
			}
		}

		char ICompletionWidget.GetChar (int offset)
		{
			return Buffer.GetIterAtOffset (offset).Char[0];
		}

		string ICompletionWidget.GetText (int startOffset, int endOffset)
		{
			string text = Buffer.GetText(Buffer.GetIterAtOffset (startOffset), Buffer.GetIterAtOffset(endOffset), true);
			return text;
		}

		TextMark triggerMark;
		int ICompletionWidget.TriggerOffset {
			get	{
				return Buffer.GetIterAtMark (triggerMark).Offset;
			}
		}

		int ICompletionWidget.TriggerLine {
			get	{
				return Buffer.GetIterAtMark (triggerMark).Line;
			}
		}

		int ICompletionWidget.TriggerLineOffset {
			get	{
				return Buffer.GetIterAtMark (triggerMark).LineOffset;
			}
		}

		Gtk.Style ICompletionWidget.GtkStyle {
			get	{
				return Style.Copy();
			}
		}
#endregion

		void InitSyntaxHighlighting()
		{
			buffer = new SourceBuffer(new SourceTagTable());
			buffer.Highlight = true;
			
			SourceLanguagesManager slm = new SourceLanguagesManager();
			SourceLanguage lang = slm.GetLanguageFromMimeType ("text/xml");
			if (lang != null) {
				buffer.Language = lang;
			}

			Buffer = buffer;
		}
		
		/// <summary>
		/// Checks whether the caret is inside a set of quotes (" or ').
		/// </summary>
		bool IsInsideQuotes()
		{
			// Get character at cursor.
			TextIter iter = Buffer.GetIterAtMark(Buffer.InsertMark);			
			string charAfter = iter.Char;
			
			// Get character before cursor
			if (!iter.BackwardChar()) {
				return false;
			}
			string charBefore = iter.Char;
				
			if (((charBefore == "\'") && (charAfter == "\'")) ||
				((charBefore == "\"") && (charAfter == "\""))) {
				return true;
			}
			return false;
		}
		
		/// <summary>
		/// Greater than key just pressed so try and
		/// autocomplete the xml element.
		/// </summary>
		void AutoCompleteElement()
		{
			// Move to char before '>'
			TextIter iter = Buffer.GetIterAtMark(Buffer.InsertMark);						
			if (!iter.BackwardChars(2)) {
				return;
			}		
			int elementEndOffset = iter.Offset + 1;

			// Ignore if is empty element or element
			// has no name.
			if (iter.Char == "/" || iter.Char == "<") {
				return;
			}
					
			// Work backwards and try to find the
			// element start.
			while (iter.BackwardChar()) {
				if (iter.Char == "<") {
					// Get element name.
					string elementName = GetElementNameFromOffset(iter.Offset + 1, elementEndOffset);
					if (elementName == null) {
						return;
					}
					
					// Insert element end tag.
					int offset = Buffer.GetIterAtMark(Buffer.InsertMark).Offset;
					Buffer.InsertAtCursor(elementName);
					
					// Move cursor between element tags.
					TextIter cursorIter = Buffer.GetIterAtOffset(offset);
					Buffer.PlaceCursor(cursorIter);
					return;
				}
			}
		}
		
		string GetElementNameFromOffset(int startOffset, int endOffset)
		{
			TextIter elementStart = Buffer.GetIterAtOffset(startOffset);
			string text = elementStart.GetText(Buffer.GetIterAtOffset(endOffset));
			return GetElementNameFromStartElement(text.Trim());
		}
		
		/// <summary>
		/// Tries to get the element name from an element start tag
		/// string.  The element start tag string in this case
		/// means the text inside the start tag.
		string GetElementNameFromStartElement(string text)
		{
			string name = text.Trim();
			// A forward slash means we are in an element so ignore
			// it.
			if (name.Length == 0 || name[0] == '/') {
				return null;
			}
			
			// Ignore any attributes.
			int index = name.IndexOf(' ');
			if (index >= 0) {
				name = name.Substring(0, index);
			}
			
			if (name.Length == 0) {
				return null;
			}
			return String.Concat("</", name, ">");
		}
		
		/// <summary>
		/// Taken straight from the SourceEditorView code
		/// </summary>
		public void IndentLine ()
		{
			TextIter iter = Buffer.GetIterAtMark (Buffer.InsertMark);

			// preserve offset in line
			int offset = iter.LineOffset;
			int chars = AutoIndentLine (iter.Line);
			offset += chars;

			// FIXME: not quite right yet
			// restore the offset
			TextIter nl = Buffer.GetIterAtMark (Buffer.InsertMark);
			if (offset < nl.CharsInLine)
				nl.LineOffset = offset;
			Buffer.PlaceCursor (nl);
		}
		
		/// <summary>
		/// Taken straight from the DefaultHighlightingStrategy code
		/// </summary>
		int AutoIndentLine (int lineNumber)
		{
			string indentation = lineNumber != 0 ? GetIndentation (lineNumber - 1) : "";
			
			if (indentation.Length > 0) {
				string newLineText = indentation + GetLineAsString (lineNumber).Trim ();
				ReplaceLine (lineNumber, newLineText);
			}
			
			return indentation.Length;
		}
		
		/// <summary>
		/// Taken straight from the DefaultHighlightingStrategy code
		/// </summary>
		string GetIndentation (int lineNumber)
		{
			string lineText = GetLineAsString (lineNumber);
			StringBuilder whitespaces = new StringBuilder ();
			
			foreach (char ch in lineText) {
				if (! System.Char.IsWhiteSpace (ch))
					break;
				whitespaces.Append (ch);
			}
			return whitespaces.ToString ();
		} 
		
		/// <summary>
		/// Taken straight from the SourceEditorView code
		/// </summary>
		string GetLineAsString (int ln)
		{
			TextIter begin = Buffer.GetIterAtLine (ln);
			TextIter end = begin;
			if (!end.EndsLine ())
				end.ForwardToLineEnd ();
			
			return begin.GetText (end);
		}
		
		/// <summary>
		/// Taken straight from the SourceEditorView code
		/// </summary>
		void ReplaceLine (int ln, string txt)
		{
			TextIter begin = Buffer.GetIterAtLine (ln);
			TextIter end = begin;
			if (!end.EndsLine ())
				end.ForwardToLineEnd ();
			
			Buffer.Delete (ref begin, ref end);
			Buffer.Insert (ref begin, txt);
		}
		
		#region IClipboardHandler

		bool HasSelection {
			get {
				TextIter start;
				TextIter end;
				return buffer.GetSelectionBounds(out start, out end);
			}
		}

		public string GetSelectedText ()
		{
			TextIter start;
			TextIter end;
			if (buffer.GetSelectionBounds (out start, out end)) {
				return buffer.GetText(start, end, true);
			}
			return String.Empty;
		}
		
		bool IClipboardHandler.EnableCut {
			get { return true; }
		}

		bool IClipboardHandler.EnableCopy {
			get { return true; }
		}
		
		bool IClipboardHandler.EnablePaste {
			get { return true; }
		}
		
		bool IClipboardHandler.EnableDelete	{
			get { return true; }
		}
		
		bool IClipboardHandler.EnableSelectAll{
			get { return true; }
		}
		
		void IClipboardHandler.Cut (object sender, EventArgs e)
		{
			if (HasSelection)
				buffer.CutClipboard(clipboard, true);
		}
		
		void IClipboardHandler.Copy (object sender, EventArgs e)
		{
			if (HasSelection)
				buffer.CopyClipboard(clipboard);
		}
		
		void IClipboardHandler.Paste (object sender, EventArgs e)
		{
			if (clipboard.WaitIsTextAvailable()) {
				buffer.PasteClipboard(clipboard);
				ScrollMarkOnscreen(buffer.InsertMark);
			}
		}
		
		void IClipboardHandler.Delete (object sender, EventArgs e)
		{
			if (HasSelection)
				buffer.DeleteSelection(true, true);
		}
		
		void IClipboardHandler.SelectAll(object sender, EventArgs e)
		{			
			buffer.MoveMark("insert", buffer.StartIter);
			buffer.MoveMark("selection_bound", buffer.EndIter);
		}

		#endregion	
		
		void ScrollToCursor()
		{
			TextIter iter = buffer.GetIterAtMark (buffer.InsertMark);
			if (!VisibleRect.Contains (GetIterLocation(iter))) {
				ScrollToMark (buffer.InsertMark, 0.1, false, 0, 0);
			}
		}
	}
}
