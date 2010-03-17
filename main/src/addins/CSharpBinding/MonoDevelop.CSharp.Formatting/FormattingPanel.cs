
using Gtk;

using MonoDevelop.CSharp.Formatting;
using MonoDevelop.Ide.Gui.Dialogs;

namespace MonoDevelop.CSharp.Formatting
{
	partial class FormattingPanelWidget : Gtk.Bin {
		public FormattingPanelWidget ()
		{
			Build ();
			
			indentCaseLabels.Active = FormattingProperties.IndentCaseLabels;
			
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
		
		public void Store ()
		{
			FormattingProperties.IndentCaseLabels = indentCaseLabels.Active;
			
			if (indentGotoLabelsLeftJustify.Active)
				FormattingProperties.GotoLabelIndentStyle = GotoLabelIndentStyle.LeftJustify;
			else if (indentGotoLabelsUpOneLevel.Active)
				FormattingProperties.GotoLabelIndentStyle = GotoLabelIndentStyle.OneLess;
			else if (indentGotoLabelsNormally.Active)
				FormattingProperties.GotoLabelIndentStyle = GotoLabelIndentStyle.Normal;
		}
	}
	
	public class FormattingPanel : OptionsPanel
	{
		FormattingPanelWidget widget;
		
		public override Widget CreatePanelWidget ()
		{
			return (widget = new FormattingPanelWidget ());
		}

		public override void ApplyChanges ()
		{
			widget.Store ();
		}
	}
}
