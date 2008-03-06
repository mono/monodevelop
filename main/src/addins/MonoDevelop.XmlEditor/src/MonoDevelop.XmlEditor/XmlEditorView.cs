//
// MonoDevelop XML Editor
//
// Copyright (C) 2006 Matthew Ward
// Copyright (C) 2004-2006 MonoDevelop Team
//

using Gdk;
using Gtk;
using GtkSourceView;
using MonoDevelop.Components.Commands;
using MonoDevelop.Ide.Commands;
using MonoDevelop.Ide.Gui.Content;
using MonoDevelop.Ide.Gui.Search;
using MonoDevelop.Projects.Gui.Completion;
using MonoDevelop.SourceEditor;
using MonoDevelop.SourceEditor.Gui.Dialogs;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.XPath;

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
		XPathNodeTextMarker xpathNodeTextMarker;
		
		public XmlEditorView()
		{
			InitSyntaxHighlighting();
			xpathNodeTextMarker = new XPathNodeTextMarker(buffer);
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
		
		/// <summary>
		/// Finds the xml nodes that match the specified xpath.
		/// </summary>
		/// <returns>An array of XPathNodeMatch items. These include line number 
		/// and line position information aswell as the node found.</returns>
		public static XPathNodeMatch[] SelectNodes(string xml, string xpath, ReadOnlyCollection<XmlNamespace> namespaces)
		{
			XmlTextReader xmlReader = new XmlTextReader(new StringReader(xml));
			xmlReader.XmlResolver = null;
			XPathDocument doc = new XPathDocument(xmlReader);
			XPathNavigator navigator = doc.CreateNavigator();
			
			// Add namespaces.
			XmlNamespaceManager namespaceManager = new XmlNamespaceManager(navigator.NameTable);
			foreach (XmlNamespace xmlNamespace in namespaces) {
				namespaceManager.AddNamespace(xmlNamespace.Prefix, xmlNamespace.Uri);
			}
	
			// Run the xpath query.                                                        
			XPathNodeIterator iterator = navigator.Select(xpath, namespaceManager);
			
			List<XPathNodeMatch> nodes = new List<XPathNodeMatch>();
			while (iterator.MoveNext()) {
				nodes.Add(new XPathNodeMatch(iterator.Current));
			}			
			return nodes.ToArray();
		}
		
		/// <summary>
		/// Finds the xml nodes that match the specified xpath.
		/// </summary>
		/// <returns>An array of XPathNodeMatch items. These include line number 
		/// and line position information aswell as the node found.</returns>
		public static XPathNodeMatch[] SelectNodes(string xml, string xpath)
		{
			List<XmlNamespace> list = new List<XmlNamespace>();
			return SelectNodes(xml, xpath, new ReadOnlyCollection<XmlNamespace>(list));
		}
		
		/// <summary>
		/// Finds the xml nodes in the current document that match the specified xpath.
		/// </summary>
		/// <returns>An array of XPathNodeMatch items. These include line number 
		/// and line position information aswell as the node found.</returns>
		public XPathNodeMatch[] SelectNodes(string xpath, ReadOnlyCollection<XmlNamespace> namespaces)
		{
			return SelectNodes(Buffer.Text, xpath, namespaces);
		}
		
		/// <summary>
		/// Highlights the xpath matches in the xml.
		/// </summary>
		public void AddXPathMarkers(XPathNodeMatch[] nodes)
		{
			xpathNodeTextMarker.AddMarkers(nodes);
		}
		
		/// <summary>
		/// Removes the xpath match highlighting.
		/// </summary>
		public void RemoveXPathMarkers()
		{
			xpathNodeTextMarker.RemoveMarkers();
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
	
		public string GetSelectedText ()
		{
			TextIter start;
			TextIter end;
			if (buffer.GetSelectionBounds (out start, out end)) {
				return buffer.GetText(start, end, true);
			}
			return String.Empty;
		}
		
		/// <summary>
		/// Search methods taken from SourceEditorWidget
		/// </summary>
		public void SetSearchPattern ()
		{
			string selectedText = GetSelectedText ();
			if (selectedText != null && selectedText.Length > 0) {
				SearchReplaceManager.SearchOptions.SearchPattern = selectedText.Split ('\n')[0];
			}
		}
		
		[CommandHandler (SearchCommands.Find)]
		public void Find()
		{
			SetSearchPattern();
			SearchReplaceManager.ShowFindWindow ();
		}
		
		[CommandHandler (SearchCommands.FindNext)]
		public void FindNext ()
		{
			SearchReplaceManager.FindNext ();
		}
	
		[CommandHandler (SearchCommands.FindPrevious)]
		public void FindPrevious ()
		{
			SearchReplaceManager.FindPrevious ();
		}
	
		[CommandHandler (SearchCommands.FindNextSelection)]
		public void FindNextSelection ()
		{
			SetSearchPattern();
			SearchReplaceManager.FindNext ();
		}
	
		[CommandHandler (SearchCommands.FindPreviousSelection)]
		public void FindPreviousSelection ()
		{
			SetSearchPattern();
			SearchReplaceManager.FindPrevious ();
		}
	
		[CommandHandler (SearchCommands.Replace)]
		public void Replace ()
		{ 
			SetSearchPattern ();
			SearchReplaceManager.ShowFindReplaceWindow ();
		} 
		
		public string GetText (int start, int length)
		{
			TextIter begin_iter = buffer.GetIterAtOffset (start);
			TextIter end_iter = buffer.GetIterAtOffset (start + length);
			return buffer.GetText (begin_iter, end_iter, true);
		}
		
		public int GetLowerSelectionBounds ()
		{
			TextIter start;
			TextIter end;
			if (buffer.GetSelectionBounds (out start, out end)) {
				return start.Offset < end.Offset ? start.Offset : end.Offset;
			}
			return 0;
		}
		
		[CommandHandler (EditorCommands.GotoLineNumber)]
		public void GotoLineNumber ()
		{
			if (!GotoLineNumberDialog.IsVisible)
				using (GotoLineNumberDialog gnd = new GotoLineNumberDialog ())
					gnd.Run ();
		}
		
		#region IClipboardHandler

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
					case Gdk.Key.ISO_Left_Tab:
						bool unindent = IsShiftPressed(evnt.State);
						if (IndentSelection(unindent)) {
							return true;
						}
						break;
					case Gdk.Key.Tab:
						if (IndentSelection(false)) {
							return true;
						}
						break;
					default:
						if (IsXmlAttributeValueChar(evnt.KeyValue)) {
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

			// Ignore if is empty element, element
			// has no name or if comment.
			if (iter.Char == "/" || iter.Char == "<" || iter.Char == "-") {
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
		void IndentLine ()
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
		
		void ScrollToCursor()
		{
			TextIter iter = buffer.GetIterAtMark (buffer.InsertMark);
			if (!VisibleRect.Contains (GetIterLocation(iter))) {
				ScrollToMark (buffer.InsertMark, 0.1, false, 0, 0);
			}
		}
		
		bool HasSelection {
			get {
				TextIter start;
				TextIter end;
				return buffer.GetSelectionBounds(out start, out end);
			}
		}

		bool IsShiftPressed(Gdk.ModifierType modifierType)
		{
			return (modifierType & Gdk.ModifierType.ShiftMask) != 0;
		}
		
		bool IndentSelection (bool unindent)
		{
			TextIter begin, end;
			if (!buffer.GetSelectionBounds (out begin, out end))
				return false;
			
			int y0 = begin.Line, y1 = end.Line;

			// If last line isn't selected, it's illogical to indent it.
			if (end.StartsLine())
				y1--;

			if (y0 == y1)
				return false;
			
			try {
				buffer.BeginUserAction();
				if (unindent)
					UnIndentLines (y0, y1);
				else
					IndentLines (y0, y1);
				SelectLines (y0, y1);
			} finally {
				buffer.EndUserAction();
			}
			
			return true;
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
					buffer.Delete (ref start, ref end);
					
				} else if (c == ' ') {
					int cnt = 0;
					int max = (int) TabsWidth;
					
					while (cnt <= max && end.Char[0] == ' ' && ! end.EndsLine ()) {
						cnt ++;
						end.ForwardChar ();
					}
					
					if (cnt == 0)
						return;
					
					buffer.Delete (ref start, ref end);
				}
			}
		}
		
		void SelectLines (int y0, int y1)
		{
			buffer.PlaceCursor (buffer.GetIterAtLine (y0));
			
			TextIter end = buffer.GetIterAtLine (y1);
			end.ForwardToLineEnd ();
			buffer.MoveMark ("selection_bound", end);
		}
		
		/// <summary>
		/// Check the key is a valid char for an xml attribute value.
		/// We cannot just pass the EventKey.KeyValue to the
		/// Char.IsLetterOrDigit method since special keys, for
		/// example the cursor keys, map to valid letters.
		/// </summary>
		static bool IsXmlAttributeValueChar(uint keyValue)
		{
			if (keyValue >= 0 && keyValue <= 0xFD00) {
				return XmlParser.IsAttributeValueChar((char)keyValue);
			}
			return false;
		}
	}
}
