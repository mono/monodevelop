//
// MonoDevelop XML Editor
//
// Copyright (C) 2006 Matthew Ward
//

using MonoDevelop.Ide.Codons;
using MonoDevelop.Ide.Gui;
using System;
using System.IO;

namespace MonoDevelop.XmlEditor
{
	/// <summary>
	/// Display binding for the xml editor.  
	/// </summary>
	public class XmlDisplayBinding : IDisplayBinding
	{
		public XmlDisplayBinding()
		{
		}		
		
		/// <summary>
		/// The name of the binding
		/// </summary>
		public string DisplayName {
			get {
				return "XML Editor";
			}
		}
			
		/// <summary>
		/// Can create content for 'XML' mime types.
		/// </summary>
		public bool CanCreateContentForMimeType(string mimeType)
		{
			return XmlEditorViewContent.IsMimeTypeHandled(mimeType);
		}

		public IViewContent CreateContentForMimeType(string mimeType, Stream content)
		{
			string text = String.Empty;
			using (StreamReader reader = new StreamReader (content)) {
				text = reader.ReadToEnd ();
			}

			XmlEditorViewContent view = new XmlEditorViewContent();
			view.LoadContent(text);
			return view;
		}
		
		/// <summary>
		/// Can only create content for file with extensions that are 
		/// known to be xml files as specified in the SyntaxModes.xml file.
		/// </summary>
		public bool CanCreateContentForFile(string fileName)
		{
			return XmlEditorViewContent.IsFileNameHandled(fileName);
		}
		
		public IViewContent CreateContentForFile(string fileName)
		{
			XmlEditorViewContent view = new XmlEditorViewContent();
			view.Load(fileName);
			return view;
		}
	}
}
