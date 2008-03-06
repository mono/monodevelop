//
// MonoDevelop XML Editor
//
// Copyright (C) 2005-2006 Matthew Ward
//

using Gdk;
using Gtk;
using GtkSourceView;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Projects.Gui.Completion;
using System;

namespace MonoDevelop.XmlEditor
{
	/// <summary>
	/// Holds the text for  namespace, child element or attribute 
	/// autocomplete (intellisense).
	/// </summary>
	public class XmlCompletionData : ICompletionData, IActionCompletionData
	{
		string text;
		DataType dataType = DataType.XmlElement;
		string description = String.Empty;
		
		/// <summary>
		/// The type of text held in this object.
		/// </summary>
		public enum DataType {
			XmlElement = 1,
			XmlAttribute = 2,
			NamespaceUri = 3,
			XmlAttributeValue = 4
		}
		
		public XmlCompletionData(string text)
			: this(text, String.Empty, DataType.XmlElement)
		{
		}
		
		public XmlCompletionData(string text, string description)
			: this(text, description, DataType.XmlElement)
		{
		}

		public XmlCompletionData(string text, DataType dataType)
			: this(text, String.Empty, dataType)
		{
		}		

		public XmlCompletionData(string text, string description, DataType dataType)
		{
			this.text = text;
			this.description = description;
			this.dataType = dataType;  
		}		
		
		public DataType XmlCompletionDataType {
			get {
				return dataType;
			}
		}
		
		public string Image {
			get {
				return Gtk.Stock.GoForward;
			}
		}
		
		public string[] Text {
			get {
				return new string[] { text };
			}
		}
		
		public string CompletionString {
			get { 
				return text; 
			}
		}
		
		/// <summary>
		/// Returns the xml item's documentation as retrieved from
		/// the xs:annotation/xs:documentation element.
		/// </summary>
		public string Description {
			get {
				return description;
			}
		}
		
		public void InsertAction(ICompletionWidget widget, ICodeCompletionContext completionContext)
		{
			XmlEditorView view = widget as XmlEditorView;
			if (view != null) {
				TextEditor editor = new TextEditor(view.ViewContent);
				editor.DeleteText(completionContext.TriggerOffset, editor.CursorPosition - completionContext.TriggerOffset);
	
				if ((dataType == DataType.XmlElement) || (dataType == DataType.XmlAttributeValue)) {
					widget.InsertAtCursor(text);
				} else if (dataType == DataType.NamespaceUri) {
					widget.InsertAtCursor(String.Concat("\"", text, "\""));					
				} else {
					// Insert an attribute.
					widget.InsertAtCursor(String.Concat(text, "=\"\""));
					
					// Move caret into the middle of the attribute quotes.
					editor.CursorPosition--;
					editor.Select(editor.CursorPosition, editor.CursorPosition);
				}
			}
		}

		public int CompareTo(object obj)
		{
			if ((obj == null) || !(obj is XmlCompletionData)) {
				return -1;
			}
			return text.CompareTo(((XmlCompletionData)obj).text);
		}
	}
}
