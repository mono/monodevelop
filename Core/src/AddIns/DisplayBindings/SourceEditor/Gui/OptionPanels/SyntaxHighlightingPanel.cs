using System;
using System.IO;
using System.Collections;
using System.Text;
using Gtk;
using Gnome;
using Pango;
using GtkSourceView;

using MonoDevelop.Core.Properties;
using MonoDevelop.Services;
using MonoDevelop.Core.Services;
using MonoDevelop.Core.AddIns.Codons;
using MonoDevelop.Internal.ExternalTool;
using MonoDevelop.Gui.Dialogs;
using MonoDevelop.Gui.Widgets;

namespace MonoDevelop.EditorBindings.Gui.OptionPanels
{
	public class SyntaxHighlightingPanel : AbstractOptionPanel
	{
		SyntaxHighlightingPanelWidget widget;
		
		public override void LoadPanelContents()
		{
			Add (widget = new SyntaxHighlightingPanelWidget ((IProperties) CustomizationObject));
		}
		
		public override bool StorePanelContents()
		{
			widget.Store ((IProperties) CustomizationObject);
			return true;
		}
	
		class SyntaxHighlightingPanelWidget : GladeWidgetExtract 
		{	
			[Glade.Widget] CheckButton enableSyntaxHighlighting;
			[Glade.Widget] ComboBox sourceLanguages;
			[Glade.Widget] Gtk.TreeView stylesTreeView;
			[Glade.Widget] ToggleButton boldToggle;
			[Glade.Widget] ToggleButton italicToggle;
			[Glade.Widget] ToggleButton underlineToggle;
			[Glade.Widget] ToggleButton strikeToggle;
			[Glade.Widget] ColorButton fgColorButton;
			[Glade.Widget] ColorButton bgColorButton;
			[Glade.Widget] Button restoreDefaultButton;
			[Glade.Widget] VBox childrenVBox;

			SourceViewService svs = (SourceViewService) ServiceManager.GetService (typeof (SourceViewService));
			SourceLanguage currentLanguage;
			SourceTagStyle currentStyle;
			string styleid;
			
			public SyntaxHighlightingPanelWidget (IProperties CustomizationObject) :  base ("EditorBindings.glade", "SyntaxHighlightingPanel")
			{
 				enableSyntaxHighlighting.Active = ((IProperties) CustomizationObject).GetProperty ("SyntaxHighlight", true);

				// add available sourceLanguages
				ListStore store = new ListStore (typeof (string));
				foreach (SourceLanguage sl in svs.AvailableLanguages)
					store.AppendValues (sl.Name);
				store.SetSortColumnId (0, SortType.Ascending);
				sourceLanguages.Model = store;

				CellRendererText cr = new CellRendererText ();
				sourceLanguages.PackStart (cr, true);
				sourceLanguages.AddAttribute (cr, "text", 0);
				sourceLanguages.Active = 0;

				stylesTreeView.AppendColumn ("styles", new CellRendererText (), "text", 0);
				stylesTreeView.Selection.Changed += new EventHandler (OnStyleChanged);
			}

			public void Store (IProperties CustomizationObject)
			{
				((IProperties) CustomizationObject).SetProperty ("SyntaxHighlight", enableSyntaxHighlighting.Active);
			}

			void SetCurrentLanguage (string name)
			{
				currentLanguage = svs.FindLanguage (name);
				SetTreeValues ();
			}

			void SetSourceTagStyle ()
			{
				SourceTagStyle sts = currentStyle;
				boldToggle.Active = sts.Bold;
				italicToggle.Active = sts.Italic;
				underlineToggle.Active = sts.Underline;
				strikeToggle.Active = sts.Strikethrough;
				fgColorButton.Color = sts.Foreground;
				bgColorButton.Color = sts.Background;
				restoreDefaultButton.Sensitive = !sts.IsDefault;
			}

			void SetTreeValues ()
			{
				// name, id
				ListStore store = new ListStore (typeof (string), typeof (string));
				foreach (SourceTag t in currentLanguage.Tags)
					store.AppendValues (t.Name, t.Id);
				stylesTreeView.Model = store;

				TreeIter first;
				store.GetIterFirst (out first);
				stylesTreeView.Selection.SelectIter (first);
			}

			private void OnButtonToggled (object sender, EventArgs a)
			{
				SourceTagStyle sts = currentStyle;
				sts.Bold = boldToggle.Active;
				sts.Italic = italicToggle.Active;
				sts.Underline = underlineToggle.Active;
				sts.Strikethrough = strikeToggle.Active;
				sts.IsDefault = false;
				currentLanguage.SetTagStyle (styleid, sts);
				restoreDefaultButton.Sensitive = true;
			}

			private void OnColorSet (object sender, EventArgs a)
			{
				SourceTagStyle sts = currentStyle;
				sts.Foreground = fgColorButton.Color;
				sts.Background = bgColorButton.Color;
				sts.IsDefault = false;
				currentLanguage.SetTagStyle (styleid, sts);
				restoreDefaultButton.Sensitive = true;
			}

			private void OnHighlightingToggled (object sender, EventArgs a)
			{
				CheckButton cb = sender as CheckButton;
				childrenVBox.Sensitive = cb.Active;
			}

			private void OnLanguageSelected (object sender, EventArgs a)
			{
				TreeIter iter;
				if (sourceLanguages.GetActiveIter (out iter)) {
					SetCurrentLanguage ((string) sourceLanguages.Model.GetValue (iter, 0));
				}
			}

			private void OnRestoreClicked (object sender, EventArgs a)
			{
				currentLanguage = svs.RestoreDefaults (currentLanguage);
				OnStyleChanged (stylesTreeView.Selection, EventArgs.Empty);
			}

			private void OnStyleChanged (object sender, EventArgs a)
			{
				TreeIter iter;
				TreeModel model;
				TreeSelection selection = sender as TreeSelection;

				if (selection.GetSelected (out model, out iter)) {
					styleid = (string) model.GetValue (iter, 1);
					currentStyle = currentLanguage.GetTagStyle (styleid);
					SetSourceTagStyle ();
				}
			}
		}
	}
}

