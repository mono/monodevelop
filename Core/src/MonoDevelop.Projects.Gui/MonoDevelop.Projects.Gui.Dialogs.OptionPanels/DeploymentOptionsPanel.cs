//
// DeploymentOptionsPanel.cs
//
// Author:
//   Lluis Sanchez Gual
//
// Copyright (C) 2006 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Collections.Generic;
using Gtk;
using Gdk;
using MonoDevelop.Core;
using MonoDevelop.Core.Properties;
using MonoDevelop.Core.Gui.Dialogs;
using MonoDevelop.Projects.Deployment;
using MonoDevelop.Components;
using MonoDevelop.Projects.Gui.Deployment;

namespace MonoDevelop.Projects.Gui.Dialogs.OptionPanels
{
	public class DeploymentOptionsPanel : AbstractOptionPanel
	{
		DeploymentOptionsWidget widget;
		
		class DeploymentOptionsWidget : GladeWidgetExtract 
		{
			// Gtk Controls
			[Glade.Widget] Gtk.TreeView targetsTree;
			[Glade.Widget] Gtk.HBox targetsBox;
			[Glade.Widget] Gtk.Button buttonEdit;
			[Glade.Widget] Gtk.Button buttonRemove;
			
			CombineEntry entry;
			List<DeployTarget> targets = new List<DeployTarget> ();
			ListStore store;

			public DeploymentOptionsWidget (IProperties CustomizationObject) : 
				base ("Base.glade", "DeployOptionsPanel")
			{
				entry = ((IProperties)CustomizationObject).GetProperty("Combine") as Combine;
				if (entry == null)
					entry = ((IProperties)CustomizationObject).GetProperty("Project") as Project;
					
				if (entry == null)
					return;
				
				if (MonoDevelop.Projects.Services.DeployService.GetDeployHandlers (entry).Length == 0) {
					foreach (Gtk.Widget w in targetsBox.Children)
						targetsBox.Remove (w);
					Gtk.Label lab = new Gtk.Label (GettextCatalog.GetString ("There are no deployment handlers available for this solution item."));
					lab.Xalign = 0;
					targetsBox.PackStart (lab, false, false, 0);
					return;
				}
					
				store = new ListStore (typeof(Gdk.Pixbuf), typeof(string), typeof(object));
				targetsTree.Model = store;
				
				targetsTree.HeadersVisible = false;
				Gtk.CellRendererPixbuf cr = new Gtk.CellRendererPixbuf();
				cr.Yalign = 0;
				targetsTree.AppendColumn ("", cr, "pixbuf", 0);
				targetsTree.AppendColumn ("", new Gtk.CellRendererText(), "markup", 1);
				
				foreach (DeployTarget target in entry.DeployTargets) {
					if (target is UnknownDeployTarget)
						targets.Add (target);
					else {
						DeployTarget ct = target.DeployHandler.CreateTarget (entry);
						ct.CopyFrom (target);
						targets.Add (ct);
					}
				}
				
				targetsTree.Selection.Changed += delegate (object s, EventArgs a) {
					UpdateButtons ();
				};
				
				FillTargets ();
			}
			
			void FillTargets ()
			{
				store.Clear ();
				foreach (DeployTarget target in targets) {
					if (target is UnknownDeployTarget) {
						Gdk.Pixbuf pix = MonoDevelop.Core.Gui.Services.Resources.GetIcon ("md-package", Gtk.IconSize.Dialog);
						string desc = "<b>" + target.Name + "</b>\n<small>Unknown target</small>";
						store.AppendValues (pix, desc, target);
					} else {
						Gdk.Pixbuf pix = MonoDevelop.Core.Gui.Services.Resources.GetIcon (target.DeployHandler.Icon, Gtk.IconSize.Dialog);
						string desc = "<b>" + target.Name + "</b>";
						desc += "\n<small>" + target.DeployHandler.Description + "</small>";
						store.AppendValues (pix, desc, target);
					}
				}
			}
			
			void UpdateButtons ()
			{
				DeployTarget t = GetSelection ();
				if (t != null) {
					buttonRemove.Sensitive = true;
					buttonEdit.Sensitive = !(t is UnknownDeployTarget) && DeployTargetEditor.HasEditor (t);
				} else {
					buttonRemove.Sensitive = false;
					buttonEdit.Sensitive = false;
				}
			}
			
			protected void OnAddTarget (object s, EventArgs args)
			{
				using (AddDeployTargetDialog dlg = new AddDeployTargetDialog (entry)) {
					if (dlg.Run () == (int) Gtk.ResponseType.Ok) {
						targets.Add (dlg.NewTarget);
						FillTargets ();
					}
				}
			}

			protected void OnRemoveTarget (object s, EventArgs args)
			{
				DeployTarget t = GetSelection ();
				if (t != null) {
					targets.Remove (t);
					FillTargets ();
				}
			}

			protected void OnEditTarget (object s, EventArgs args)
			{
				DeployTarget t = GetSelection ();
				if (t != null) {
					DeployTarget tc = t.DeployHandler.CreateTarget (entry);
					tc.CopyFrom (t);
					using (EditDeployTargetDialog dlg = new EditDeployTargetDialog (tc)) {
						if (dlg.Run () == (int) Gtk.ResponseType.Ok) {
							targets [targets.IndexOf (t)] = tc;
							FillTargets ();
						}
					}
				}
			}
			
			DeployTarget GetSelection ()
			{
				Gtk.TreeModel model;
				Gtk.TreeIter iter;
				
				if (targetsTree.Selection.GetSelected (out model, out iter)) {
					return (DeployTarget) store.GetValue (iter, 2);
				} else
					return null;
			}

			public bool Store()
			{
				entry.DeployTargets.Clear ();
				entry.DeployTargets.AddRange (targets);
				return true;
			}
		}

		public override void LoadPanelContents()
		{
			try {
				Add (widget = new DeploymentOptionsWidget ((IProperties) CustomizationObject));
			} catch (Exception ex) {
				Console.WriteLine (ex);
			}
		}

		public override bool StorePanelContents ()
		{
			bool success = widget.Store ();
 			return success;
		}					
	}
}
