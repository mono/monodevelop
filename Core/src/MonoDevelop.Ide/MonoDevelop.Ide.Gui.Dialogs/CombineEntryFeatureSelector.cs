
using System;
using System.Collections.Generic;
using Gtk;
using MonoDevelop.Ide.Templates;
using MonoDevelop.Ide.Projects;

namespace MonoDevelop.Ide.Gui.Dialogs
{
	public partial class CombineEntryFeatureSelector : Gtk.Bin
	{
		List<ICombineEntryFeature> selectedFeatures = new List<ICombineEntryFeature> ();
		List<Gtk.Widget> selectedEditors = new List<Gtk.Widget> ();
		IProject entry;
		Solution parentCombine;
		VBox box = new VBox ();
		
		public CombineEntryFeatureSelector ()
		{
			this.Build();
			box.Spacing = 6;
			box.BorderWidth = 6;
			box.Show ();
		}
		
		public void Fill (Solution parentCombine, IProject entry, ICombineEntryFeature[] features)
		{
			selectedFeatures.Clear ();
			selectedEditors.Clear ();
			
			this.entry = entry;
			this.parentCombine = parentCombine;
			
			foreach (Gtk.Widget w in box.Children) {
				box.Remove (w);
				w.Destroy ();
			}
			foreach (ICombineEntryFeature feature in features) {
				Gtk.HBox cbox = new Gtk.HBox ();
				CheckButton check = new CheckButton ();
				Label fl = new Label ();
				fl.Markup = "<b>" + feature.Title + "</b>";
				check.Image = fl;
				cbox.PackStart (check, false, false, 0);
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
				
				ICombineEntryFeature f = feature;
				check.Toggled += delegate {
					OnClickFeature (f, check, fbox, editor);
				};
				if (feature.IsEnabled (parentCombine, entry))
					check.Active = true;
			}
			scrolled.AddWithViewport (box);
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