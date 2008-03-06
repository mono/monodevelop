//
// MonoDevelop XML Editor
//
// Copyright (C) 2006 Matthew Ward
//

using Gtk;
using System;

namespace MonoDevelop.XmlEditor
{
	public class XmlEditorWindow : ScrolledWindow
	{	
		XmlEditorView xmlEditorView;
		
		public XmlEditorWindow() : this(null)
		{
		}
		
		public XmlEditorWindow(XmlEditorViewContent viewContent)
		{
			xmlEditorView = new XmlEditorView(viewContent);
			Add(xmlEditorView);
			ShowAll();
		}
		
		public XmlEditorView View {
			get {
				return xmlEditorView;
			}
		}
	}
	
}
