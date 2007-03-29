
using System;
using System.Collections;
using System.Collections.Generic;
using MonoDevelop.Projects.Deployment;

namespace MonoDevelop.Projects.Gui.Deployment
{
	public partial class CombinedDeployEditorWidget : Gtk.Bin
	{
		Gtk.ListStore store;
		CombinedDeployTarget combinedTarget;
		List<DeployTarget> targets;
		
		public CombinedDeployEditorWidget (CombinedDeployTarget combinedTarget)
		{
			this.combinedTarget = combinedTarget;
			this.Build();
		
			store = new Gtk.ListStore (typeof(Gdk.Pixbuf), typeof(string), typeof(object));
			targetsTree.Model = store;
			
			targetsTree.HeadersVisible = false;
			Gtk.CellRendererPixbuf cr = new Gtk.CellRendererPixbuf();
			cr.Yalign = 0;
			targetsTree.AppendColumn ("", cr, "pixbuf", 0);
			targetsTree.AppendColumn ("", new Gtk.CellRendererText(), "markup", 1);
			
			targetsTree.Selection.Changed += delegate (object s, EventArgs a) {
				UpdateButtons ();
			};
			
			targets = combinedTarget.DeployTargets;
			FillTargets ();
			if (targets.Count > 0)
				SelectTarget (targets[0]);
			UpdateButtons ();
		}

		void FillTargets ()
		{
			DeployTarget selTarget = GetSelection ();
			
			store.Clear ();
			foreach (DeployTarget target in targets) {
				if (target is UnknownDeployTarget) {
					Gdk.Pixbuf pix = MonoDevelop.Core.Gui.Services.Resources.GetIcon ("md-package", Gtk.IconSize.LargeToolbar);
					string desc = "<b>" + target.Name + "</b>\n<small>Unknown target</small>";
					store.AppendValues (pix, desc, target);
				} else {
					Gdk.Pixbuf pix = MonoDevelop.Core.Gui.Services.Resources.GetIcon (target.Icon, Gtk.IconSize.LargeToolbar);
					string desc = "<b>" + target.Name + "</b>";
					desc += "\n<small>" + target.Description + "</small>";
					store.AppendValues (pix, desc, target);
				}
			}
			SelectTarget (selTarget);
		}
		
		void UpdateButtons ()
		{
			DeployTarget t = GetSelection ();
			if (t != null) {
				buttonRemove.Sensitive = true;
				buttonEdit.Sensitive = !(t is UnknownDeployTarget) && DeployTargetEditor.HasEditor (t);
				int i = targets.IndexOf (t);
				buttonUp.Sensitive = i > 0;
				buttonDown.Sensitive = i >= 0 && i < targets.Count - 1;
			} else {
				buttonRemove.Sensitive = false;
				buttonEdit.Sensitive = false;
				buttonUp.Sensitive = false;
				buttonDown.Sensitive = false;
			}
		}
		
		protected virtual void OnButtonAddClicked(object sender, System.EventArgs e)
		{
			using (AddDeployTargetDialog dlg = new AddDeployTargetDialog (combinedTarget.CombineEntry)) {
				if (dlg.Run () == (int) Gtk.ResponseType.Ok) {
					targets.Add (dlg.NewTarget);
					FillTargets ();
					SelectTarget (dlg.NewTarget);
				}
			}
		}

		protected virtual void OnButtonRemoveClicked(object sender, System.EventArgs e)
		{
			DeployTarget t = GetSelection ();
			if (t != null) {
				targets.Remove (t);
				FillTargets ();
			}
		}

		protected virtual void OnButtonEditClicked(object sender, System.EventArgs e)
		{
			DeployTarget t = GetSelection ();
			if (t != null) {
				DeployTarget tc = t.Clone ();
				using (EditDeployTargetDialog dlg = new EditDeployTargetDialog (tc)) {
					if (dlg.Run () == (int) Gtk.ResponseType.Ok) {
						targets [targets.IndexOf (t)] = tc;
						FillTargets ();
						SelectTarget (tc);
					}
				}
			}
		}

		protected virtual void OnButtonUpClicked(object sender, System.EventArgs e)
		{
			DeployTarget t = GetSelection ();
			int i = targets.IndexOf (t);
			if (i > 0) {
				targets [i] = targets [i - 1];
				targets [i - 1] = t;
				FillTargets ();
			}
		}

		protected virtual void OnButtonDownClicked(object sender, System.EventArgs e)
		{
			DeployTarget t = GetSelection ();
			int i = targets.IndexOf (t);
			if (i >= 0 && i < targets.Count - 1) {
				targets [i] = targets [i + 1];
				targets [i + 1] = t;
				FillTargets ();
			}
		}
		
		void SelectTarget (DeployTarget target)
		{
			if (target == null)
				return;
			Gtk.TreeIter iter;
			if (store.GetIterFirst (out iter)) {
				do {
					DeployTarget t = (DeployTarget) store.GetValue (iter, 2);
					if (t == target) {
						targetsTree.Selection.SelectIter (iter);
						return;
					}
				} while (store.IterNext (ref iter));
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
	}
}
