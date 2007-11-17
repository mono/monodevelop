//
// AddinInstallDialog.cs
//
// Author:
//   Lluis Sanchez Gual
//
// Copyright (C) 2007 Novell, Inc (http://www.novell.com)
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
using System.Text;
using System.Threading;
using System.Collections;
using System.Collections.Specialized;
using Mono.Unix;

using Gtk;

using Mono.Addins.Setup;
using Mono.Addins.Description;

namespace Mono.Addins.Gui
{
	partial class AddinInstallDialog : Dialog
	{
		ListStore repoStore;
		AddinTreeWidget tree;
		bool installing;
		
		PackageCollection packagesToInstall;
		string uninstallId;
		
		InstallMonitor installMonitor;
		SetupService service;
		
		
		public AddinInstallDialog (SetupService service)
		{
			Build ();
			this.service = service;
			wizardNotebook.ShowTabs = false;
			ActionArea.Hide ();
			
			tree = new InstallAddinTreeWidget (addinTree);
			tree.AllowSelection = true;
			tree.SelectionChanged += new EventHandler (OnAddinSelectionChanged);
			
			repoStore = new ListStore (typeof(string), typeof(string));
			repoCombo.Model = repoStore;
			CellRendererText crt = new CellRendererText ();
			repoCombo.PackStart (crt, true);
			repoCombo.AddAttribute (crt, "text", 0);
			filterComboBox.Active = 1;
			
			imageInstall.Stock = "md-software-update";
			imageInstall.IconSize = (int)IconSize.Dialog;
			
			FillRepos ();
			repoCombo.Active = 0;
			LoadAddins ();
			FillAddinInfo ();
			OnPageChanged ();
		}
		
		public override void Dispose ()
		{
			base.Dispose ();
			Destroy ();
		}
		
		void FillRepos ()
		{
			int i = repoCombo.Active;
			repoStore.Clear ();
			
			repoStore.AppendValues (Catalog.GetString ("All registered repositories"), "");
			
			foreach (AddinRepository rep in service.Repositories.GetRepositories ()) {
				repoStore.AppendValues (rep.Title, rep.Url);
			}
			repoCombo.Active = i;
		}
		
		void LoadAddins ()
		{
			object s = tree.SaveStatus ();
			
			tree.Clear ();
			
			Gtk.TreeIter iter;
			if (!repoCombo.GetActiveIter (out iter))
				return;
				
			bool showUpdates = filterComboBox.Active >= 1;
			bool showNotInstalled = filterComboBox.Active <= 1;
			
			string rep = (string) repoStore.GetValue (iter, 1);
			
			AddinRepositoryEntry[] reps;
			if (rep == "")
				reps = service.Repositories.GetAvailableAddins ();
			else
				reps = service.Repositories.GetAvailableAddins (rep);
			
			foreach (AddinRepositoryEntry arep in reps) {
				Addin sinfo = AddinManager.Registry.GetAddin (arep.Addin.Id);
				if (!Services.InApplicationNamespace (service, arep.Addin.Id))
					continue;
				
				if (sinfo == null) {
					if (showNotInstalled)
						tree.AddAddin (arep.Addin, arep, true);
					continue;
				}
				
				if (showUpdates && Addin.CompareVersions (sinfo.Version, arep.Addin.Version) <= 0)
					continue;
				
				tree.AddAddin (arep.Addin, arep, true);
			}
			FillAddinInfo ();
			
			// Only show the select all button when "Show updates only" is selected
			btnSelectAll.Visible = filterComboBox.Active == 2;
			btnUnselectAll.Visible = filterComboBox.Active == 2;
			
			tree.RestoreStatus (s);
		}
		
		void OnAddinSelectionChanged (object o, EventArgs e)
		{
			UpdateAddinSelection ();
		}
			
		void UpdateAddinSelection ()
		{
			btnNext.Sensitive = (tree.GetSelectedAddins().Length != 0);
		}
		
		protected void OnActiveAddinChanged (object o, EventArgs e)
		{
			FillAddinInfo ();
		}
		
		protected void OnNextPage (object sender, EventArgs e)
		{
			wizardNotebook.NextPage ();
			OnPageChanged ();
		}
		
		protected void OnPrevPage (object sender, EventArgs e)
		{
			wizardNotebook.PrevPage ();
			OnPageChanged ();
		}
		
		protected void OnCancel (object sender, EventArgs e)
		{
			if (installing) {
				if (Services.AskQuestion (Catalog.GetString ("Are you sure you want to cancel the installation?")))
					installMonitor.Cancel ();
			} else
				Respond (ResponseType.Cancel);
		}
		
		protected void OnOk (object sender, EventArgs e)
		{
			Respond (ResponseType.Ok);
		}
		
		protected void OnManageSites (object sender, EventArgs e)
		{
			ManageSitesDialog dlg = new ManageSitesDialog (service);
			dlg.TransientFor = this;
			try {
				dlg.Run ();
				FillRepos ();
			} finally {
				dlg.Destroy ();
			}
		}


		bool updateDone;
		
		protected void OnUpdateRepo (object sender, EventArgs e)
		{
			Thread t = new Thread (new ThreadStart (RunUpdate));
			t.Start ();
			updateDone = false;
			while (!updateDone) {
				while (Gtk.Application.EventsPending ())
					Gtk.Application.RunIteration ();
				Thread.Sleep (50);
			}
			LoadAddins ();
		}
		
		void RunUpdate ()
		{
			try {
				service.Repositories.UpdateAllRepositories (null);
			} finally {
				updateDone = true;
			}
		}
		
		protected void OnRepoChanged (object sender, EventArgs e)
		{
			LoadAddins ();
		}
		
		public void OnPageChanged ()
		{
			switch (wizardNotebook.CurrentPage) {
			case 0:
				btnPrev.Sensitive = false;
				btnNext.Sensitive = (tree.GetSelectedAddins().Length != 0);
				break;
			case 1:
				FillSummaryPage ();
				break;
			case 2:
				Install ();
				break;
			case 3:
				btnPrev.Hide ();
				btnNext.Hide ();
				btnCancel.Hide ();
				btnOk.Show ();
				break;
			}
		}
		
		protected void OnGotoWeb (object sender, EventArgs e)
		{
			AddinHeader info = tree.ActiveAddin;
			if (info == null)
				return;
				
			if (info.Url != "")
				Gnome.Url.Show (info.Url);
		}
		
		protected void OnShowInfo (object sender, EventArgs e)
		{
			AddinHeader info = tree.ActiveAddin;
			if (info == null)
				return;

			AddinInfoDialog dlg = new AddinInfoDialog (info);
			try {
				dlg.Run ();
			} finally {
				dlg.Destroy ();
			}
		}

		protected void OnFilterChanged (object sender, EventArgs e)
		{
			LoadAddins ();
			UpdateAddinSelection ();
		}
		
		protected void OnSelectAll (object sender, EventArgs e)
		{
			tree.SelectAll ();
		}
		
		protected void OnUnselectAll (object sender, EventArgs e)
		{
			tree.UnselectAll ();
		}
		
		public void SetUninstallMode (AddinHeader info)
		{
			btnPrev.Hide ();
			btnNext.Sensitive = true;
			
			uninstallId = info.Id;
			wizardNotebook.CurrentPage = 1;
			
			StringBuilder sb = new StringBuilder ();
			sb.Append ("<b>").Append (Catalog.GetString ("The following packages will be uninstalled:")).Append ("</b>\n\n");
			sb.Append (info.Name + "\n\n");
			
			Addin[] sinfos = service.GetDependentAddins (info.Id, true);
			if (sinfos.Length > 0) {
				sb.Append ("<b>").Append (Catalog.GetString ("There are other add-ins that depend on the previous ones which will also be uninstalled:")).Append ("</b>\n\n");
				foreach (Addin si in sinfos)
					sb.Append (si.Description.Name + "\n");
			}
			
			labelSummary.Markup = sb.ToString ();
		}
		
		void FillAddinInfo ()
		{
			AddinHeader info = tree.ActiveAddin;
			btnInfo.Sensitive = info != null;
			
/*			if (info == null) {
				infoLabel.Markup = "";
				linkLabel.Visible = false;
				return;
			}
				
			StringBuilder sb = new StringBuilder ();
			sb.Append ("<b><big>" + info.Name + "</big></b>\n\n");
			
			if (info.Description != "")
				sb.Append (info.Description + "\n\n");
			
			sb.Append ("<small>");
			
			sb.Append ("<b>").Append (Catalog.GetString ("Version:")).Append ("</b>\n").Append (info.Version).Append ("\n\n");
			
			if (info.Author != "")
				sb.Append ("<b>").Append (Catalog.GetString ("Author:")).Append ("</b>\n").Append (info.Author).Append ("\n\n");
			
			if (info.Copyright != "")
				sb.Append ("<b>").Append (Catalog.GetString ("Copyright:")).Append ("</b>\n").Append (info.Copyright).Append ("\n\n");
			
			if (info.Dependencies.Count > 0) {
				sb.Append ("<b>").Append (Catalog.GetString ("Add-in Dependencies:")).Append ("</b>\n");
				foreach (PackageDependency dep in info.Dependencies)
					sb.Append (dep.Name + "\n");
			}
			
			sb.Append ("</small>");
			
			linkLabel.Visible = info.Url != "";
				
			infoLabel.Markup = sb.ToString ();*/
		}

		
		void FillSummaryPage ()
		{
			btnPrev.Sensitive = true;
			
			AddinHeader[] infos = tree.GetSelectedAddins ();
			PackageCollection packs = new PackageCollection ();
			foreach (AddinHeader info in infos) {
				AddinRepositoryEntry arep = (AddinRepositoryEntry) tree.GetAddinData (info);
				packs.Add (Package.FromRepository (arep));
			}
			
			packagesToInstall = new PackageCollection (packs);
			
			PackageCollection toUninstall;
			DependencyCollection unresolved;
			bool res;
			
			InstallMonitor m = new InstallMonitor ();
			res = service.ResolveDependencies (m, packs, out toUninstall, out unresolved);
			
			StringBuilder sb = new StringBuilder ();
			if (!res) {
				sb.Append ("<b><span foreground=\"red\">").Append (Catalog.GetString ("The selected add-ins can't be installed because there are dependency conflicts.")).Append ("</span></b>\n");
				foreach (string s in m.Errors) {
					sb.Append ("<b><span foreground=\"red\">" + s + "</span></b>\n");
				}
				sb.Append ("\n");
			}
			
			if (m.Warnings.Count != 0) {
				foreach (string w in m.Warnings) {
					sb.Append ("<b><span foreground=\"red\">" + w + "</span></b>\n");
				}
				sb.Append ("\n");
			}
			
			sb.Append ("<b>").Append (Catalog.GetString ("The following packages will be installed:")).Append ("</b>\n\n");
			foreach (Package p in packs) {
				sb.Append (p.Name);
				if (!p.SharedInstall)
					sb.Append (Catalog.GetString (" (in user directory)"));
				sb.Append ("\n");
			}
			sb.Append ("\n");
			
			if (toUninstall.Count > 0) {
				sb.Append ("<b>").Append (Catalog.GetString ("The following packages need to be uninstalled:")).Append ("</b>\n\n");
				foreach (Package p in toUninstall) {
					sb.Append (p.Name + "\n");
				}
				sb.Append ("\n");
			}
			
			if (unresolved.Count > 0) {
				sb.Append ("<b>").Append (Catalog.GetString ("The following dependencies could not be resolved:")).Append ("</b>\n\n");
				foreach (Dependency p in unresolved) {
					sb.Append (p.Name + "\n");
				}
				sb.Append ("\n");
			}
			btnNext.Sensitive = res;
			labelSummary.Markup = sb.ToString ();
		}
		
		void Install ()
		{
			btnPrev.Sensitive = false;
			btnNext.Sensitive = false;
			
			string txt;
			string okmessage;
			string errmessage;
			
			installMonitor = new InstallMonitor (progressLabel, progressBar, mainProgressBar);
			ThreadStart oper;
				
			if (uninstallId == null) {
				oper = new ThreadStart (RunInstall);
				okmessage = Catalog.GetString ("The installation has been successfully completed.");
				errmessage = Catalog.GetString ("The installation failed!");
			} else {
				oper = new ThreadStart (RunUninstall);
				okmessage = Catalog.GetString ("The uninstallation has been successfully completed.");
				errmessage = Catalog.GetString ("The uninstallation failed!");
			}
			
			Thread t = new Thread (oper);
			t.Start ();
			
			installing = true;
			installMonitor.WaitForCompleted ();
			installing = false;
			
			wizardNotebook.NextPage ();

			if (installMonitor.Success) {
				imageError.Visible = false;
				imageInfo.Visible = true;
				txt = "<b>" + okmessage + "</b>\n\n";
			} else {
				imageError.Visible = true;
				imageInfo.Visible = false;
				txt = "<span foreground=\"red\"><b>" + errmessage + "</b></span>\n\n";
				foreach (string s in installMonitor.Errors)
					txt += s + "\n";
			}
			
			labelResult.Markup = txt;
			OnPageChanged ();
		}
		
		void RunInstall ()
		{
			try {
				service.Install (installMonitor, packagesToInstall);
			} catch {
				// Nothing
			} finally {
				installMonitor.Dispose ();
			}
		}
		
		void RunUninstall ()
		{
			try {
				service.Uninstall (installMonitor, uninstallId);
			} catch {
				// Nothing
			} finally {
				installMonitor.Dispose ();
			}
		}
	}
	
	class InstallMonitor: IProgressStatus, IDisposable
	{
		Label progressLabel;
		ProgressBar progressBar;
		ProgressBar mainProgressBar;
		StringCollection errors = new StringCollection ();
		StringCollection warnings = new StringCollection ();
		bool canceled;
		bool done;
		
		public InstallMonitor (Label progressLabel, ProgressBar progressBar, ProgressBar mainProgressBar)
		{
			this.progressLabel = progressLabel;
			this.progressBar = progressBar;
			this.mainProgressBar = mainProgressBar;
		}
		
		public InstallMonitor ()
		{
		}
		
		public void SetMessage (string msg)
		{
			if (progressLabel != null)
				progressLabel.Text = msg;
		}
		
		public void SetProgress (double progress)
		{
			if (mainProgressBar != null)
				mainProgressBar.Fraction = progress;
			if (progressBar != null)
				progressBar.Fraction = progress;
		}
		
		public void Log (string msg)
		{
			Console.WriteLine (msg);
		}
		
		public void ReportWarning (string message)
		{
			warnings.Add (message);
		}
		
		public void ReportError (string message, Exception exception)
		{
			errors.Add (message);
		}
		
		public bool IsCanceled {
			get { return canceled; }
		}
		
		public StringCollection Errors {
			get { return errors; }
		}
		
		public StringCollection Warnings {
			get { return warnings; }
		}
		
		public void Cancel ()
		{
			canceled = true;
		}
		
		public int LogLevel {
			get { return 1; }
		}
		
		public void Dispose ()
		{
			done = true;
		}
		
		public void WaitForCompleted ()
		{
			while (!done) {
				while (Gtk.Application.EventsPending ())
					Gtk.Application.RunIteration ();
				Thread.Sleep (50);
			}
		}
		
		public bool Success {
			get { return errors.Count == 0; }
		}
	}
	
	class InstallAddinTreeWidget: AddinTreeWidget
	{
		int ncol;
		public InstallAddinTreeWidget (Gtk.TreeView treeView): base (treeView)
		{
		}
		
		protected override void AddStoreTypes (ArrayList list)
		{
			base.AddStoreTypes (list);
			ncol = list.Count;
			list.Add (typeof(string));
		}
		
		protected override void CreateColumns ()
		{
			base.CreateColumns ();
			TreeViewColumn col = new TreeViewColumn ();
			col.Title = Catalog.GetString ("Repository");
			CellRendererText crt = new CellRendererText ();
			col.PackStart (crt, true);
			col.AddAttribute (crt, "text", ncol);
			treeView.AppendColumn (col);
		}
		
		protected override void UpdateRow (TreeIter iter, AddinHeader info, object dataItem, bool enabled, Gdk.Pixbuf icon)
		{
			base.UpdateRow (iter, info, dataItem, enabled, icon);
			AddinRepositoryEntry arep = (AddinRepositoryEntry) dataItem;
			treeStore.SetValue (iter, ncol, arep.RepositoryName);
		}
	}
}

