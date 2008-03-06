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
		
		public XmlEditorWindow()
		{
			xmlEditorView = new XmlEditorView();
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
