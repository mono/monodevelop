//
// MonoDevelop XML Editor
//
// Copyright (C) 2005-2007 Matthew Ward
//

using Gdk;
using Gtk;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Projects.Gui.Completion;
using System;
using MonoDevelop.XmlEditor;

namespace MonoDevelop.XmlEditor.Completion
{
	/// <summary>
	/// Holds the text for  namespace, child element or attribute 
	/// autocomplete (intellisense).
	/// </summary>
	public class XmlCompletionData : IActionCompletionData
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
			get { return dataType; }
		}
		
		public string Icon {
			get { return Gtk.Stock.GoForward; }
		}
		
		public string DisplayText {
			get { return text; }
		}
		
		public string CompletionText {
			get { return text; }
		}
		
		/// <summary>
		/// Returns the xml item's documentation as retrieved from
		/// the xs:annotation/xs:documentation element.
		/// </summary>
		public string Description {
			get { return description; }
		}
		
		public DisplayFlags DisplayFlags {
			get { return DisplayFlags.None; }
		}
		
		public void InsertCompletionText (ICompletionWidget widget, ICodeCompletionContext completionContext)
		{
			MonoDevelop.Ide.Gui.Content.IEditableTextBuffer buf = widget as MonoDevelop.Ide.Gui.Content.IEditableTextBuffer;
			if (buf != null) {
				buf.BeginAtomicUndo ();
				buf.DeleteText (completionContext.TriggerOffset, buf.CursorPosition - completionContext.TriggerOffset);
				if ((dataType == DataType.XmlElement) || (dataType == DataType.XmlAttributeValue)) {
					buf.InsertText (buf.CursorPosition, text);
				} else if (dataType == DataType.NamespaceUri) {
					buf.InsertText (buf.CursorPosition,String.Concat("\"", text, "\""));					
				} else {
					// Insert an attribute.
					buf.InsertText (buf.CursorPosition,String.Concat(text, "=\"\""));
					
					// Move caret into the middle of the attribute quotes.
					buf.CursorPosition--;
					buf.Select (buf.CursorPosition, buf.CursorPosition);
				}
				buf.EndAtomicUndo ();
			}
		}		
	}
}
