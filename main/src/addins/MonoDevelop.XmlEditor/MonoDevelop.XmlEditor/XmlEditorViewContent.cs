//
// MonoDevelop XML Editor
//
// Copyright (C) 2007 Matthew Ward
// Copyright (C) 2004-2007 MonoDevelop Team
//

using Gnome.Vfs;
using Gtk;
using GtkSourceView;
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Gui.Content;
using MonoDevelop.Ide.Gui.Search;
using MonoDevelop.Projects.Text;
using System;
using System.IO;

namespace MonoDevelop.XmlEditor
{
	public class XmlEditorViewContent : AbstractViewContent, IEditableTextBuffer, IDocumentInformation, IClipboardHandler
	{
		XmlEditorWindow xmlEditorWindow;
		SourceBuffer buffer;
		XmlEditorView view;
		const string TextXmlMimeType = "text/xml";
		const string ApplicationXmlMimeType = "application/xml";
		string fileName = String.Empty;
		EventHandler<PropertyChangedEventArgs> propertyChangedHandler;
		string stylesheetFileName;
		
		public XmlEditorViewContent()
		{
			xmlEditorWindow = new XmlEditorWindow(this);
			view = xmlEditorWindow.View;
			buffer = (SourceBuffer)view.Buffer;
			buffer.Changed += BufferChanged;

			view.SchemaCompletionDataItems = XmlSchemaManager.SchemaCompletionDataItems;
			SetInitialValues();	 
			
			// Watch for changes to the source editor properties.
			propertyChangedHandler = (EventHandler<PropertyChangedEventArgs>)DispatchService.GuiDispatch(new EventHandler<PropertyChangedEventArgs>(SourceEditorPropertyChanged));
			TextEditorProperties.Properties.PropertyChanged += propertyChangedHandler;

			buffer.ModifiedChanged += new EventHandler (OnModifiedChanged);
			XmlEditorAddInOptions.PropertyChanged += new EventHandler<PropertyChangedEventArgs>(XmlEditorPropertyChanged);
			
			XmlSchemaManager.UserSchemaAdded += new EventHandler(UserSchemaAdded);
			XmlSchemaManager.UserSchemaRemoved += new EventHandler(UserSchemaRemoved);
			
			xmlEditorWindow.ShowAll();
		}
		
		public static bool IsMimeTypeHandled(string mimeType)
		{
			Console.WriteLine("mimeType: " + mimeType);
			if (mimeType != null) {
				if (mimeType == TextXmlMimeType || mimeType == ApplicationXmlMimeType) {
					return true;
				}
			}
			return false;
		}
		
		/// <summary>
		/// Determines whether the file can be displayed by
		/// the xml editor.
		/// </summary>
		public static bool IsFileNameHandled(string fileName)
		{			
			if (fileName == null) {
				return false;
			}
			
			string vfsname = fileName;
			vfsname = vfsname.Replace ("%", "%25");
			vfsname = vfsname.Replace ("#", "%23");
			vfsname = vfsname.Replace ("?", "%3F");
			string mimeType = MimeType.GetMimeTypeForUri (vfsname);
			if (IsMimeTypeHandled(mimeType)) {
				return true;
			}
			
			return XmlFileExtensions.IsXmlFileExtension(Path.GetExtension(fileName));
		}
		
		public XmlEditorView XmlEditorView {
			get {
				return view;
			}
		}
		
		public override Gtk.Widget Control {
			get {
				return xmlEditorWindow;
			}
		}
		
		public override bool IsDirty {
			get {
				return base.IsDirty;
			}
			set {
				buffer.Modified = value;
			}
		}
		
		public override string UntitledName {
			get {
				return base.UntitledName;
			}
			set {
				base.UntitledName = value;
				fileName = value;
				if (value != null) {
        			SetDefaultSchema(value);
				}
			}
		}
		
		public override string TabPageLabel {
			get { 
				return "XML";
			}
		}
		
		public string FileName {
			get {
				return fileName;
			}
		}
		
		/// <summary>
		/// Gets or sets the stylesheet associated with this xml file.
		/// </summary>
		public string StylesheetFileName {
			get {
				return stylesheetFileName;
			}
			set {
				stylesheetFileName = value;
			}
		}
		
		public bool IsSchema {
			get {
				if (fileName != null) {
					string extension = Path.GetExtension(fileName);
					if (extension != null) {
						return String.Compare(extension, ".xsd", true) == 0;
					}
				}
				return false;
			}
		}
		
		public override void Load(string fileName)
		{
			using (StreamReader reader = System.IO.File.OpenText(fileName)) {
				LoadContent(reader.ReadToEnd());
			}		
			ContentName = fileName;
			this.fileName = fileName;
			SetDefaultSchema(ContentName);
		}
		
		public void LoadContent(string content)
		{
			buffer.Text = content;
			buffer.Modified = false;
		}
		
		public override void Dispose()
		{
			XmlEditorAddInOptions.PropertyChanged -= new EventHandler<PropertyChangedEventArgs>(XmlEditorPropertyChanged);
			XmlSchemaManager.UserSchemaAdded -= new EventHandler(UserSchemaAdded);
			XmlSchemaManager.UserSchemaRemoved -= new EventHandler(UserSchemaRemoved);
			TextEditorProperties.Properties.PropertyChanged -= propertyChangedHandler;
			xmlEditorWindow.Dispose();
		}
		
		public override void Save(string fileName)
		{
			using (TextWriter s = new StreamWriter (fileName, true)) { 
			}
			
			using (TextWriter s = new StreamWriter (fileName, false)) {
				s.Write (buffer.Text);
			}
			ContentName = fileName;
			xmlEditorWindow.View.Buffer.Modified = false;
		}
		
		/// <summary>
		/// Code taken from SourceEditorDisplayBinding.
		/// </summary>
		void SetInitialValues()
		{
			view.ShowSchemaAnnotation = XmlEditorAddInOptions.ShowSchemaAnnotation;
			view.AutoCompleteElements = XmlEditorAddInOptions.AutoCompleteElements;
			
			view.ModifyFont (TextEditorProperties.Font);
			view.ShowLineNumbers = TextEditorProperties.ShowLineNumbers;
			((SourceBuffer)view.Buffer).HighlightMatchingBrackets = TextEditorProperties.ShowMatchingBracket;
			view.ShowRightMargin = TextEditorProperties.ShowVerticalRuler;
			view.InsertSpacesInsteadOfTabs = TextEditorProperties.ConvertTabsToSpaces;
			view.AutoIndent = (TextEditorProperties.IndentStyle == IndentStyle.Auto);
			view.HighlightCurrentLine = TextEditorProperties.HighlightCurrentLine;
			((SourceBuffer)view.Buffer).HighlightSyntax = TextEditorProperties.SyntaxHighlight;

			if (TextEditorProperties.TabIndent > -1)
				view.TabWidth = (uint) TextEditorProperties.TabIndent;
			else
				view.TabWidth = (uint) 4;
			
			if (TextEditorProperties.VerticalRulerRow > -1)
				view.RightMarginPosition = (uint) TextEditorProperties.VerticalRulerRow;
			else
				view.RightMarginPosition = 80;
			
			UpdateStyleScheme ();
		}
		
		void OnModifiedChanged (object o, EventArgs e)
		{
			base.IsDirty = view.Buffer.Modified;
		}
		
		void UpdateStyleScheme ()
		{
			string id = TextEditorProperties.Properties.Get<string> ("GtkSourceViewStyleScheme", "classic");
			SourceStyleScheme scheme = GtkSourceView.SourceStyleSchemeManager.Default.GetScheme (id);
			if (scheme == null)
				MonoDevelop.Core.LoggingService.LogWarning ("GTKSourceView style scheme '" + id + "' is missing.");
			else
				((SourceBuffer)view.Buffer).StyleScheme = scheme;
		}
		
		void XmlEditorPropertyChanged(object o, PropertyChangedEventArgs e)
 		{
			switch (e.Key) {
				case "AutoCompleteElements":
					view.AutoCompleteElements = XmlEditorAddInOptions.AutoCompleteElements;
					break;
				case "ShowSchemaAnnotation":
					view.ShowSchemaAnnotation = XmlEditorAddInOptions.ShowSchemaAnnotation;
					break;
				default:
					string extension = Path.GetExtension(fileName).ToLower();
					if (e.Key == extension) {
						SetDefaultSchema(extension);
					} else {
						Console.WriteLine("XmlEditor: Unhandled property change: " + e.Key);
					}
					break;
			}
		}
		
		void SourceEditorPropertyChanged(object o, PropertyChangedEventArgs e)
		{
			switch (e.Key) {
				case "DefaultFont":
					view.ModifyFont(TextEditorProperties.Font);
					break;
				case "ShowLineNumbers":
					view.ShowLineNumbers = TextEditorProperties.ShowLineNumbers;
					break;
				case "ConvertTabsToSpaces":
					view.InsertSpacesInsteadOfTabs = TextEditorProperties.ConvertTabsToSpaces;
					break;
				case "ShowBracketHighlight":
					((SourceBuffer)view.Buffer).HighlightMatchingBrackets = TextEditorProperties.ShowMatchingBracket;
					break;
				case "ShowVRuler":
					view.ShowRightMargin = TextEditorProperties.ShowVerticalRuler;
					break;
				case "VRulerRow":
					if (TextEditorProperties.VerticalRulerRow > -1)
						view.RightMarginPosition = (uint) TextEditorProperties.VerticalRulerRow;
					else
						view.RightMarginPosition = 80;
					break;
				case "TabIndent":
					if (TextEditorProperties.TabIndent > -1)
						view.TabWidth = (uint) TextEditorProperties.TabIndent;
					else
						view.TabWidth = (uint) 4;
					break;
				case "IndentStyle":
					view.AutoIndent = (TextEditorProperties.IndentStyle == IndentStyle.Auto);
					break;
				case "HighlightCurrentLine":
					view.HighlightCurrentLine = TextEditorProperties.HighlightCurrentLine;
					break;
				case "GtkSourceViewStyleScheme":
					UpdateStyleScheme ();
					break;
				default:
					MonoDevelop.Core.LoggingService.LogWarning ("XmlEditor: Unhandled source editor property change: " + e.Key);
					break;
			}
		}
		
		#region IClipboardHandler
		public bool EnableCut {
			get {
				return ((IClipboardHandler)view).EnableCut;
			}
		}
		public bool EnableCopy {
			get {
				return ((IClipboardHandler)view).EnableCopy;
			}
		}
		public bool EnablePaste {
			get {
				return ((IClipboardHandler)view).EnablePaste;
			}
		}
		public bool EnableDelete {
			get {
				return ((IClipboardHandler)view).EnableDelete;
			}
		}
		public bool EnableSelectAll {
			get {
				return ((IClipboardHandler)view).EnableSelectAll;
			}
		}
		
		public void Cut ()
		{
			((IClipboardHandler)view).Cut ();
		}
		
		public void Copy ()
		{
			((IClipboardHandler)view).Copy ();
		}
		
		public void Paste ()
		{
			((IClipboardHandler)view).Paste ();
		}
		
		public void Delete ()
		{
			((IClipboardHandler)view).Delete ();
		}
		
		public void SelectAll ()
		{
			((IClipboardHandler)view).SelectAll ();
		}
		#endregion
		
		/// <summary>
        /// Sets the default schema and namespace prefix that the xml editor will use.
        /// </summary>
        void SetDefaultSchema(string fileName)
        {
        	if (fileName == null) {
        		return;
        	}
        	string extension = Path.GetExtension(fileName).ToLower();
 	        view.DefaultSchemaCompletionData = XmlSchemaManager.GetSchemaCompletionData(extension);
 	        view.DefaultNamespacePrefix = XmlSchemaManager.GetNamespacePrefix(extension);
        }
        
        /// <summary>
        /// Updates the default schema association since the schema
        /// may have been added.
        /// </summary>
        void UserSchemaAdded(object source, EventArgs e)
        {	
        	SetDefaultSchema(ContentName);
        }
        
        /// <summary>
        /// Updates the default schema association since the schema
        /// may have been removed.
        /// </summary>
        void UserSchemaRemoved(object source, EventArgs e)
        {
        	SetDefaultSchema(ContentName);
        }   
        
        #region IEditableTextBuffer
        
		public event EventHandler<TextChangedEventArgs> TextChanged;
		
		public void BeginAtomicUndo ()
		{
			//Buffer.BeginUserAction ();
		}
		
		public void EndAtomicUndo ()
		{
			//Buffer.EndUserAction ();
		}
		
		public string Name {
			get { 
				return ContentName;
			}
		}
		
		public string Text {
			get {
//				if (needsUpdate) {
//					cachedText = se.Buffer.Text;
//				}
				return buffer.Text;
			}
			set { 
				try {
					buffer.BeginUserAction();
					buffer.Text = value;
				} finally {
					buffer.EndUserAction();
				}
			}
		}
		
		public void Undo ()
		{
			view.UndoChange();
		}
		
		public bool EnableUndo {
			get { return buffer.CanUndo; }
		}
		
		public bool EnableRedo {
			get { return buffer.CanRedo; }
		}
		
		public void Redo ()
		{
			view.RedoChange();
		}
		
		public string SelectedText {
			get {
				return view.GetSelectedText();
			}
			set { 
				int offset = view.GetLowerSelectionBounds ();
				((IClipboardHandler)view).Delete ();
				TextIter iter = buffer.GetIterAtOffset (offset);
				buffer.Insert (ref iter, value);
				buffer.PlaceCursor(iter);
				view.ScrollMarkOnscreen (buffer.InsertMark);
			}
		}
		
		public int GetPositionFromLineColumn (int line, int column)
		{
			Console.WriteLine("GetPositionFromLineColumn");
			return -1;
		}
		
		public void InsertText (int position, string text)
		{
			Console.WriteLine("InsertText");
		}
		
		public void DeleteText (int pos, int length)
		{
			TextIter start = buffer.GetIterAtOffset (pos);
			TextIter end = buffer.GetIterAtOffset (pos + length);
			buffer.Delete (ref start, ref end);
		}
				
		public int CursorPosition {
			get { 
				return buffer.GetIterAtMark(buffer.InsertMark).Offset;
			}
			set { 
				buffer.MoveMark (buffer.InsertMark, buffer.GetIterAtOffset (value));
			}
		}
				
		public void GetLineColumnFromPosition (int position, out int line, out int column)
		{
			Console.WriteLine("GetLineColumnFromPosition");
			column = -1;
			line = -1;
		}
		
		public void ShowPosition (int position)
		{
			view.ScrollToIter (buffer.GetIterAtOffset (position), 0.3, false, 0, 0);
		}
		
		public char GetCharAt (int offset)
		{
			if (offset < 0)
				offset = 0;
			TextIter iter = buffer.GetIterAtOffset (offset);
			if (iter.Equals (TextIter.Zero))
				return ' ';
			if (iter.Char == null || iter.Char.Length == 0)
				return ' ';
			return iter.Char[0];
		}
				
		public string GetText (int startPosition, int endPosition)
		{
			return buffer.GetText (buffer.GetIterAtOffset (startPosition), buffer.GetIterAtOffset (endPosition), true);
		}
		
		int ITextFile.Length {
			get { 
				Console.WriteLine("ITextFile.Length");
				return 0;
			}
		}
		
		public void Select (int startPosition, int endPosition)
		{
			buffer.MoveMark (buffer.InsertMark, buffer.GetIterAtOffset (startPosition));
			buffer.MoveMark (buffer.SelectionBound, buffer.GetIterAtOffset (endPosition));
		}
		
		public int SelectionStartPosition {
			get {
				TextIter p1 = buffer.GetIterAtMark (buffer.InsertMark);
				TextIter p2 = buffer.GetIterAtMark (buffer.SelectionBound);
				if (p1.Offset < p2.Offset) return p1.Offset;
				else return p2.Offset;
			}
		}
		
		public int SelectionEndPosition {
			get {
				TextIter p1 = buffer.GetIterAtMark (buffer.InsertMark);
				TextIter p2 = buffer.GetIterAtMark (buffer.SelectionBound);
				if (p1.Offset > p2.Offset) return p1.Offset;
				else return p2.Offset;
			}
		}		

		#endregion
		
		#region IDocumentInformation
	
		string IDocumentInformation.FileName {
			get { return ContentName != null ? ContentName : UntitledName; }
		}
		
		public ITextIterator GetTextIterator ()
		{
			int startOffset = buffer.GetIterAtMark (buffer.InsertMark).Offset;
			return new SourceViewTextIterator (this, view, startOffset);
		}
		
		public string GetLineTextAtOffset (int offset)
		{
			TextIter resultIter = buffer.GetIterAtOffset (offset);
			TextIter start_line = resultIter, end_line = resultIter;
			start_line.LineOffset = 0;
			end_line.ForwardToLineEnd ();
			return view.GetText (start_line.Offset, end_line.Offset - start_line.Offset);
		}

		#endregion
		
		#region IPositionable
		
		public void SetCaretTo (int line, int column)
		{
			// NOTE: 1 based!			
			TextIter itr = view.Buffer.GetIterAtLine (line - 1);
			itr.LineOffset = column - 1;

			view.Buffer.PlaceCursor (itr);	
			view.ScrollToMark (view.Buffer.InsertMark, 0.3, false, 0, 0);
			GLib.Timeout.Add (20, new GLib.TimeoutHandler (changeFocus));
		}
		
		protected virtual void OnCaretPositionSet (EventArgs args)
		{
			if (CaretPositionSet != null) 
				CaretPositionSet (this, args);
		}
		public event EventHandler CaretPositionSet;

		//This code exists to workaround a gtk+ 2.4 regression/bug
		//
		//The gtk+ 2.4 treeview steals focus with double clicked
		//row_activated.
		// http://bugzilla.gnome.org/show_bug.cgi?id=138458
		bool changeFocus ()
		{
			if (!view.IsRealized)
				return false;
			view.GrabFocus ();
			view.ScrollToMark (view.Buffer.InsertMark, 0.3, false, 0, 0);
			OnCaretPositionSet (EventArgs.Empty);
			return false;
		}
		
		#endregion
		
		void BufferChanged(object source, EventArgs e)
		{
			if (TextChanged != null) {
				TextChanged(this, new TextChangedEventArgs(0, 0));
			}
		}
	}
}
