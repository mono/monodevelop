using System;

using Gtk;

using MonoDevelop.Core;
using MonoDevelop.Core.Properties;
using MonoDevelop.Core.Gui.Dialogs;

using MonoDevelop.SourceEditor;
using MonoDevelop.SourceEditor.Properties;

using CSharpBinding.FormattingStrategy.Properties;

namespace CSharpBinding {
	partial class FormattingPanelWidget : Gtk.Bin {
		public FormattingPanelWidget ()
		{
			Build ();
			
			// disable until implemented
			indentCaseLabels.Sensitive = false;
			
			// set checkbox/radiobutton values
			switch (FormattingProperties.GotoLabelIndentStyle) {
			case GotoLabelIndentStyle.LeftJustify:
				indentGotoLabelsLeftJustify.Active = true;
				break;
			case GotoLabelIndentStyle.OneLess:
				indentGotoLabelsUpOneLevel.Active = true;
				break;
			case GotoLabelIndentStyle.Normal:
				indentGotoLabelsNormally.Active = true;
				break;
			}
		}
		
		public bool Store ()
		{
			if (indentGotoLabelsLeftJustify.Active)
				FormattingProperties.GotoLabelIndentStyle = GotoLabelIndentStyle.LeftJustify;
			else if (indentGotoLabelsUpOneLevel.Active)
				FormattingProperties.GotoLabelIndentStyle = GotoLabelIndentStyle.OneLess;
			else if (indentGotoLabelsNormally.Active)
				FormattingProperties.GotoLabelIndentStyle = GotoLabelIndentStyle.Normal;
			
			return true;
		}
	}
	
	public class FormattingPanel : AbstractOptionPanel {
		FormattingPanelWidget widget;
		
		public override void LoadPanelContents ()
		{
			Add (widget = new FormattingPanelWidget ());
		}
		
		public override bool StorePanelContents ()
		{
			return widget.Store ();
		}
	}
}
