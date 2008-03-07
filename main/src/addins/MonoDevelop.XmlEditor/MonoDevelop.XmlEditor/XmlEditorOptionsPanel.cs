//
// MonoDevelop XML Editor
//
// Copyright (C) 2006 Matthew Ward
//

using MonoDevelop.Core;
using MonoDevelop.Core.Gui.Dialogs;

using System;
using Gtk;

namespace MonoDevelop.XmlEditor
{
	/// <summary>
	/// Configuration settings for the xml editor.
	/// </summary>
	public class XmlEditorOptionsPanel : OptionsPanel
	{
		XmlEditorOptionsPanelWidget widget;
		
		public override Widget CreatePanelWidget ()
		{
			widget = new XmlEditorOptionsPanelWidget();
			widget.AutoCompleteElements = XmlEditorAddInOptions.AutoCompleteElements;
			widget.ShowSchemaAnnotation = XmlEditorAddInOptions.ShowSchemaAnnotation;
			return widget;
		}
		
		public override void ApplyChanges ()
		{
			XmlEditorAddInOptions.AutoCompleteElements = widget.AutoCompleteElements;
			XmlEditorAddInOptions.ShowSchemaAnnotation = widget.ShowSchemaAnnotation;
		}
	}
}
