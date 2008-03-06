//
// MonoDevelop XML Editor
//
// Copyright (C) 2006 Matthew Ward
//

using MonoDevelop.Core;
using MonoDevelop.Core.Gui.Dialogs;
using System;

namespace MonoDevelop.XmlEditor
{
	/// <summary>
	/// Configuration settings for the xml editor.
	/// </summary>
	public class XmlEditorOptionsPanel : AbstractOptionPanel
	{
		XmlEditorOptionsPanelWidget widget;
				
		public XmlEditorOptionsPanel()
		{
		}
		
		/// <summary>
		/// Initialises the panel.
		/// </summary>
		public override void LoadPanelContents()
		{
			widget = new XmlEditorOptionsPanelWidget();
			Add(widget);
			widget.AutoCompleteElements = XmlEditorAddInOptions.AutoCompleteElements;
			widget.ShowSchemaAnnotation = XmlEditorAddInOptions.ShowSchemaAnnotation;
		}

		/// <summary>
		/// Saves any changes.
		/// </summary>
		public override bool StorePanelContents()
		{
			XmlEditorAddInOptions.AutoCompleteElements = widget.AutoCompleteElements;
			XmlEditorAddInOptions.ShowSchemaAnnotation = widget.ShowSchemaAnnotation;
			
			return true;
		}
	}
}
