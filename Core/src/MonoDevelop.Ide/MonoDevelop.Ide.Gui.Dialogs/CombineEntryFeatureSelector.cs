
using System;
using System.Collections.Generic;
using Gtk;
using MonoDevelop.Ide.Templates;
using MonoDevelop.Projects;
using MonoDevelop.Core;

namespace MonoDevelop.Ide.Gui.Dialogs
{
	public partial class CombineEntryFeatureSelector : Gtk.Bin
	{
		List<ICombineEntryFeature> selectedFeatures = new List<ICombineEntryFeature> ();
		List<Gtk.Widget> selectedEditors = new List<Gtk.Widget> ();
		CombineEntry entry;
		Combine parentCombine;
		VBox box = new VBox ();
		
		public CombineEntryFeatureSelector ()
		{
			this.Build();
			box.Spacing = 6;
			box.BorderWidth = 6;
			box.Show ();
		}
		
		public void Fill (Combine parentCombine, CombineEntry entry, ICombineEntryFeature[] features)
		{
			selectedFeatures.Clear ();
			selectedEditors.Clear ();
			
			this.entry = entry;
			this.parentCombine = parentCombine;
			
			foreach (Gtk.Widget w in box.Children) {
				box.Remove (w);
				w.Destroy ();
			}
			// Show enabled features at the beginning
			foreach (ICombineEntryFeature feature in features)
				if (feature.IsEnabled (parentCombine, entry)) {
					Gtk.Widget editor = AddFeature (feature);
					selectedFeatures.Add (feature);
					selectedEditors.Add (editor);
				}
			foreach (ICombineEntryFeature feature in features)
				if (!feature.IsEnabled (parentCombine, entry))
					AddFeature (feature);
			
			if (box.Children.Length == 0) {
				// No features
				Label lab = new Label ();
				lab.Xalign = 0;
				lab.Text = GettextCatalog.GetString ("There are no additional features available for this project.");
				box.PackStart (lab, false, false, 0);
				lab.Show ();
			}
			scrolled.AddWithViewport (box);
		}
		
		Gtk.Widget AddFeature (ICombineEntryFeature feature)
		{
			Gtk.HBox cbox = new Gtk.HBox ();
			CheckButton check = null;
			
			Label fl = new Label ();
			fl.Markup = "<b>" + feature.Title + "</b>";
			
			if (feature.IsEnabled (parentCombine, entry)) {
				cbox.PackStart (fl, false, false, 0);
			}
			else {
				check = new CheckButton ();
				check.Image = fl;
				cbox.PackStart (check, false, false, 0);
			}
			box.PackStart (cbox, false, false, 0);
			cbox.ShowAll ();
			
			HBox fbox = new HBox ();
			Gtk.Widget editor = feature.CreateFeatureEditor (parentCombine, entry);
			if (editor != null) {
				Label sp = new Label ("");
				sp.WidthRequest = 24;
				sp.Show ();
				fbox.PackStart (sp, false, false, 0);
				editor.Show ();
				fbox.PackStart (editor, false, false, 0);
				box.PackStart (fbox, false, false, 0);
				HSeparator sep = new HSeparator ();
				sep.Show ();
				box.PackStart (sep, false, false, 0);
			}
			
			if (check != null) {
				ICombineEntryFeature f = feature;
				check.Toggled += delegate {
					OnClickFeature (f, check, fbox, editor);
				};
			} else {
				fbox.ShowAll ();
			}
			return editor;
		}
		
		void OnClickFeature (ICombineEntryFeature feature, CheckButton check, HBox fbox, Gtk.Widget editor)
		{
			if (editor != null)
				fbox.Visible = check.Active;
			if (check.Active) {
				selectedFeatures.Add (feature);
				selectedEditors.Add (editor);
			} else {
				selectedFeatures.Remove (feature);
				selectedEditors.Remove (editor);
			}
		}
		
		public bool Validate ()
		{
			for (int n=0; n<selectedFeatures.Count; n++) {
				ICombineEntryFeature pf = selectedFeatures [n];
				string msg = pf.Validate (parentCombine, entry, selectedEditors [n]);
				if (!string.IsNullOrEmpty (msg)) {
					msg = pf.Title + ": " + msg;
					IdeApp.Services.MessageService.ShowError ((Gtk.Window) this.Toplevel, msg);
					return false;
				}
			}
			return true;
		}
		
		public void ApplyFeatures ()
		{
			for (int n=0; n<selectedFeatures.Count; n++) {
				try {
					ICombineEntryFeature pf = selectedFeatures [n];
					pf.ApplyFeature (parentCombine, entry, selectedEditors [n]);
				}
				catch (Exception ex) {
					IdeApp.Services.MessageService.ShowError (ex);
				}
			}
		}
	}
}
