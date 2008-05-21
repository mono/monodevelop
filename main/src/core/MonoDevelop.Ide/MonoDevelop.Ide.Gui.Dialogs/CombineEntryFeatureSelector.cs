// CombineEntryFeatureSelector.cs
//
// Author:
//   Lluis Sanchez Gual <lluis@novell.com>
//
// Copyright (c) 2007 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//
//


using System;
using System.Collections.Generic;
using Gtk;
using MonoDevelop.Ide.Templates;
using MonoDevelop.Projects;
using MonoDevelop.Core;
using MonoDevelop.Core.Gui;

namespace MonoDevelop.Ide.Gui.Dialogs
{
	public partial class CombineEntryFeatureSelector : Gtk.Bin
	{
		List<ISolutionItemFeature> selectedFeatures = new List<ISolutionItemFeature> ();
		List<Gtk.Widget> selectedEditors = new List<Gtk.Widget> ();
		SolutionItem entry;
		SolutionFolder parentCombine;
		VBox box = new VBox ();
		Gdk.Cursor handCursor;
		
		public CombineEntryFeatureSelector ()
		{
			this.Build();
			handCursor = new Gdk.Cursor (Gdk.CursorType.Hand1);
			box.Spacing = 6;
			box.BorderWidth = 3;
			box.Show ();
		}
		
		public void Fill (SolutionFolder parentCombine, SolutionItem entry, ISolutionItemFeature[] features)
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
			foreach (ISolutionItemFeature feature in features)
				if (feature.IsEnabled (parentCombine, entry)) {
					Gtk.Widget editor = AddFeature (feature);
					selectedFeatures.Add (feature);
					selectedEditors.Add (editor);
				}
			foreach (ISolutionItemFeature feature in features)
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
		
		Gtk.Widget AddFeature (ISolutionItemFeature feature)
		{
			Gtk.HBox cbox = new Gtk.HBox ();
			CheckButton check = null;
			
			Label fl = new Label ();
			fl.Wrap = true;
			fl.WidthRequest = 630;
			fl.Markup = "<b>" + feature.Title + "</b>\n<small>" + feature.Description + "</small>";
			bool enabledByDefault = feature.IsEnabled (parentCombine, entry);

			if (enabledByDefault) {
				Alignment al = new Alignment (0,0,0,0);
				al.SetPadding (6,6,6,6);
				al.Add (fl);
				cbox.PackStart (al, false, false, 0);
			}
			else {
				check = new CheckButton ();
				check.Image = fl;
				cbox.PackStart (check, false, false, 0);
				check.ModifyBg (StateType.Prelight, Style.MidColors [(int)StateType.Normal]);
				check.BorderWidth = 3;
			}
			EventBox eb = new EventBox ();
			if (!enabledByDefault) {
				eb.Realized += delegate {
					eb.GdkWindow.Cursor = handCursor;
				};
			}
			eb.ModifyBg (StateType.Normal, Style.MidColors[(int)StateType.Normal]);
			eb.Add (cbox);
			eb.ShowAll ();
			box.PackStart (eb, false, false, 0);
			
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
			}
			
			if (check != null) {
				ISolutionItemFeature f = feature;
				check.Toggled += delegate {
					OnClickFeature (f, check, fbox, editor);
				};
			} else {
				fbox.Show ();
			}
			return editor;
		}
		
		void OnClickFeature (ISolutionItemFeature feature, CheckButton check, HBox fbox, Gtk.Widget editor)
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
				ISolutionItemFeature pf = selectedFeatures [n];
				string msg = pf.Validate (parentCombine, entry, selectedEditors [n]);
				if (!string.IsNullOrEmpty (msg)) {
					msg = pf.Title + ": " + msg;
					MessageService.ShowError ((Gtk.Window)this.Toplevel, msg);
					return false;
				}
			}
			return true;
		}
		
		public void ApplyFeatures ()
		{
			for (int n=0; n<selectedFeatures.Count; n++) {
				try {
					ISolutionItemFeature pf = selectedFeatures [n];
					pf.ApplyFeature (parentCombine, entry, selectedEditors [n]);
				}
				catch (Exception ex) {
					MessageService.ShowException (ex);
				}
			}
		}
	}
}
