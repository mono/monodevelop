//
// MonoDevelop XML Editor
//
// Copyright (C) 2006 Matthew Ward
//

using Gtk;
using MonoDevelop.Core.Gui.Components;
using MonoDevelop.Components;
using MonoDevelop.Core.Gui.Dialogs;
using System;

namespace MonoDevelop.XmlEditor
{
	public class XmlEditorOptionsPanelWidget : GladeWidgetExtract
	{
		[Glade.Widget] CheckButton autoCompleteElementsCheckButton;
 		[Glade.Widget] CheckButton showSchemaAnnotationCheckButton;
 		
		public XmlEditorOptionsPanelWidget () : base ("XmlEditor.glade", "XmlEditorOptionsPanel")
		{
		}
		
		public bool AutoCompleteElements {
			get {
				return autoCompleteElementsCheckButton.Active;
			}
			set {
				autoCompleteElementsCheckButton.Active = value;
			}
		}
		
		public bool ShowSchemaAnnotation {
			get {
				return showSchemaAnnotationCheckButton.Active;
			}
			set {
				showSchemaAnnotationCheckButton.Active = value;
			}
		}
	}
}
