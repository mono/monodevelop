//
// MonoDevelop XML Editor
//
// Copyright (C) 2006 Matthew Ward
// Copyright (C) 2004-2006 MonoDevelop Team
//

using Gnome.Vfs;
using Gtk;
using GtkSourceView;
using MonoDevelop.Core;
using MonoDevelop.Core.Gui;
using MonoDevelop.Core.Properties;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Gui.Content;
using MonoDevelop.Ide.Gui.Search;
using MonoDevelop.Projects.Text;
using MonoDevelop.SourceEditor.Properties;
using System;
using System.IO;

namespace MonoDevelop.XmlEditor
{
	public class XmlEditorViewContent : AbstractViewContent, IEditableTextBuffer, IDocumentInformation, IPositionable
	{
		XmlEditorWindow xmlEditorWindow;
		SourceBuffer buffer;
		XmlEditorView view;
		const string XmlMimeType = "text/xml";
		string fileName = String.Empty;
		PropertyEventHandler propertyChangedHandler;
		IProperties sourceEditorProperties;
		string stylesheetFileName;
		
		public XmlEditorViewContent()
		{
			xmlEditorWindow = new XmlEditorWindow();
			view = xmlEditorWindow.View;
			buffer = (SourceBuffer)view.Buffer;

			view.SchemaCompletionDataItems = XmlSchemaManager.SchemaCompletionDataItems;
			SetInitialValues();	 
			
			// Watch for changes to the source editor properties.
			DispatchService service = (DispatchService)ServiceManager.GetService(typeof(DispatchService));
			propertyChangedHandler = (PropertyEventHandler)service.GuiDispatch(new PropertyEventHandler(SourceEditorPropertyChanged));
			PropertyService propertyService = (PropertyService)ServiceManager.GetService(typeof(PropertyService));
			sourceEditorProperties = ((IProperties)propertyService.GetProperty("MonoDevelop.TextEditor.Document.Document.DefaultDocumentAggregatorProperties", new DefaultProperties()));
			sourceEditorProperties.PropertyChanged += propertyChangedHandler;

			buffer.ModifiedChanged += new EventHandler (OnModifiedChanged);
			XmlEditorAddInOptions.PropertyChanged += new PropertyEventHandler(XmlEditorPropertyChanged);
			
			XmlSchemaManager.UserSchemaAdded += new EventHandler(UserSchemaAdded);
			XmlSchemaManager.UserSchemaRemoved += new EventHandler(UserSchemaRemoved);
			
			xmlEditorWindow.ShowAll();
		}
		
		public static bool IsMimeTypeHandled(string mimeType)
		{
			Console.WriteLine("mimeType: " + mimeType);
			if (mimeType != null && mimeType == XmlMimeType) {
				return true;
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
			XmlEditorAddInOptions.PropertyChanged -= new PropertyEventHandler(XmlEditorPropertyChanged);
			XmlSchemaManager.UserSchemaAdded -= new EventHandler(UserSchemaAdded);
			XmlSchemaManager.UserSchemaRemoved -= new EventHandler(UserSchemaRemoved);
			sourceEditorProperties.PropertyChanged -= propertyChangedHandler;
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
			view.ModifyFont(TextEditorProperties.Font);
			view.ShowLineNumbers = TextEditorProperties.ShowLineNumbers;
			view.InsertSpacesInsteadOfTabs = TextEditorProperties.ConvertTabsToSpaces;
			view.ShowSchemaAnnotation = XmlEditorAddInOptions.ShowSchemaAnnotation;
			view.AutoCompleteElements = XmlEditorAddInOptions.AutoCompleteElements;

			if (TextEditorProperties.TabIndent > -1)
				view.TabsWidth = (uint) TextEditorProperties.TabIndent;
			else
				view.TabsWidth = (uint) 4;
		}
		
		void OnModifiedChanged (object o, EventArgs e)
		{
			base.IsDirty = view.Buffer.Modified;
		}
		
		void XmlEditorPropertyChanged(object o, PropertyEventArgs e)
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
		
		void SourceEditorPropertyChanged(object o, PropertyEventArgs e)
		{
			switch (e.Key) {
				case "DefaultFont":
					view.ModifyFont(TextEditorProperties.Font);
					break;
				default:
					Console.WriteLine("XmlEditor: Unhandled source editor property change: " + e.Key);
					break;
			}
		}
		
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
        
		public IClipboardHandler ClipboardHandler {
			get { 
				return view;
			}
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
				((IClipboardHandler)view).Delete (null, null);
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
			Console.WriteLine("DeleteText");
		}
		
		public event EventHandler TextChanged {
			add {
				buffer.Changed += value;
			}
			remove {
				buffer.Changed -= value;
			}
		}
		
		public int CursorPosition {
			get { 
				return buffer.GetIterAtMark(buffer.InsertMark).Offset;
			}
			set { 
				Console.WriteLine("CursorPosition set");
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
		
		public string GetText (int startPosition, int endPosition)
		{
			Console.WriteLine("GetText");
			return String.Empty;
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
		
		public void JumpTo(int line, int column)
		{
			TextIter iter = buffer.GetIterAtLine (line - 1);
			iter.LineOffset = column - 1;

			buffer.PlaceCursor (iter);		
			//buffer.HighlightLine (line - 1);	
			view.ScrollToMark (buffer.InsertMark, 0.3, false, 0, 0);
		}
		
		#endregion
	}
}
