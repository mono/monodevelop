//
// MonoDevelop XML Editor
//
// Copyright (C) 2006 Matthew Ward
// Copyright (C) 2004-2006 MonoDevelop Team
//

using Gnome.Vfs;
using Gtk;
using GtkSourceView;
using MonoDevelop.Core.Properties;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Gui.Content;
using MonoDevelop.Projects.Text;
using MonoDevelop.SourceEditor.Properties;
using System;
using System.IO;

namespace MonoDevelop.XmlEditor
{
	public class XmlEditorViewContent : AbstractViewContent, IEditableTextBuffer
	{
		XmlEditorWindow xmlEditorWindow;
		SourceBuffer buffer;
		XmlEditorView view;
		const string XmlMimeType = "text/xml";
		string fileName = String.Empty;
		
		public XmlEditorViewContent()
		{
			xmlEditorWindow = new XmlEditorWindow();
			view = xmlEditorWindow.View;
			buffer = (SourceBuffer)view.Buffer;

			view.SchemaCompletionDataItems = XmlSchemaManager.SchemaCompletionDataItems;
			SetInitialValues();	 
			
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
			Console.WriteLine("FileName: " + fileName);
			
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
		
		public string FileName {
			get {
				return fileName;
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
			XmlEditorAddInOptions.PropertyChanged -= XmlEditorPropertyChanged;
			XmlSchemaManager.UserSchemaAdded -= new EventHandler(UserSchemaAdded);
			XmlSchemaManager.UserSchemaRemoved -= new EventHandler(UserSchemaRemoved);
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
					Console.WriteLine("XmlEditor: Unhandled property change: " + e.Key);
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
				return String.Empty;
			}
			set { 
			}
		}
		
		public int GetPositionFromLineColumn (int line, int column)
		{
			return -1;
		}
		
		public void InsertText (int position, string text)
		{
		}
		
		public void DeleteText (int pos, int length)
		{
		}
		
		public event EventHandler TextChanged {
			add {
			}
			remove {
			}
		}
		
		public int CursorPosition {
			get { 
				return -1;
			}
			set { 
			}
		}
		
		public void GetLineColumnFromPosition (int position, out int line, out int column)
		{
			column = -1;
			line = -1;
		}
		
		public void ShowPosition (int position)
		{
		}
		
		public string GetText (int startPosition, int endPosition)
		{
			return String.Empty;
		}
		
		int ITextFile.Length {
			get { 
				return 0;
			}
		}
		
		public void Select (int startPosition, int endPosition)
		{
		}
		
		public int SelectionStartPosition {
			get {
				return -1;
			}
		}
		
		public int SelectionEndPosition {
			get {
				return -1;
			}
		}		

		#endregion    
	}
}
