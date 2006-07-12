
using System;
using System.Text;
using System.Threading;
using System.Collections;

using Gtk;
using Glade;

using MonoDevelop.Core.AddIns.Setup;
using MonoDevelop.Core.ProgressMonitoring;
using MonoDevelop.Core.Gui.ProgressMonitoring;

namespace MonoDevelop.Core.Gui.Dialogs
{
	class AddinInstallDialog : IDisposable
	{
		[Glade.Widget ("AddinInstallDialog")] Dialog dialog;
		[Glade.Widget ("wizardNotebook")] Notebook wizard;
		[Glade.Widget] Gtk.TreeView addinTree;
		[Glade.Widget] ComboBox repoCombo;
		[Glade.Widget] Button btnNext;
		[Glade.Widget] Button btnPrev;
		[Glade.Widget] Button btnCancel;
		[Glade.Widget] Button btnOk;
		[Glade.Widget] Button btnInfo;
		[Glade.Widget] Button btnSelectAll;
		[Glade.Widget] Button btnUnselectAll;
		[Glade.Widget] Label labelSummary;
		[Glade.Widget] Label progressLabel;
		[Glade.Widget] Label labelResult;
		[Glade.Widget] ProgressBar progressBar;
		[Glade.Widget] ProgressBar mainProgressBar;
		[Glade.Widget] Image imageError;
		[Glade.Widget] Image imageInfo;
		[Glade.Widget] Image imageInstall;
		[Glade.Widget] ComboBox filterComboBox;
		
		ListStore repoStore;
		AddinTreeWidget tree;
		bool installing;
		
		PackageCollection packagesToInstall;
		string uninstallId;
		
		InstallMonitor installMonitor;
		
		
		public AddinInstallDialog ()
		{
			ServiceManager.GetService (typeof(DispatchService));
			new Glade.XML (null, "Base.glade", "AddinInstallDialog", null).Autoconnect (this);
			wizard.ShowTabs = false;
			dialog.ActionArea.Hide ();
			
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
		
		void FillRepos ()
		{
			int i = repoCombo.Active;
			repoStore.Clear ();
			
			repoStore.AppendValues (GettextCatalog.GetString ("All registered repositories"), "");
			
			foreach (RepositoryRecord rep in Runtime.SetupService.GetRepositories ()) {
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
				reps = Runtime.SetupService.GetAvailableAddins ();
			else
				reps = Runtime.SetupService.GetAvailableAddins (rep);
			
			foreach (AddinRepositoryEntry arep in reps) {
				AddinSetupInfo sinfo = Runtime.SetupService.GetInstalledAddin (arep.Addin.Id);
				
				if (sinfo == null) {
					if (showNotInstalled)
						tree.AddAddin (arep.Addin, arep, true);
					continue;
				}
				
				if (showUpdates && AddinInfo.CompareVersions (sinfo.Addin.Version, arep.Addin.Version) <= 0)
					continue;
				
				tree.AddAddin (arep.Addin, arep, true);
			}
			FillAddinInfo ();
			
			// Only show the select all button when "Show updates only" is selected
			btnSelectAll.Visible = filterComboBox.Active == 2;
			btnUnselectAll.Visible = filterComboBox.Active == 2;
			
			tree.RestoreStatus (s);
		}
		
		public void Show ()
		{
			dialog.ShowAll ();
		}
		
		public void Run ()
		{
			dialog.Show ();
			dialog.Run ();
		}
		
		public void Dispose ()
		{
			dialog.Destroy ();
			dialog.Dispose ();
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
			wizard.NextPage ();
			OnPageChanged ();
		}
		
		protected void OnPrevPage (object sender, EventArgs e)
		{
			wizard.PrevPage ();
			OnPageChanged ();
		}
		
		protected void OnCancel (object sender, EventArgs e)
		{
			if (installing) {
				if (Services.MessageService.AskQuestion (GettextCatalog.GetString ("Are you sure you want to cancel the installation?")))
					installMonitor.AsyncOperation.Cancel ();
			} else
				dialog.Respond (ResponseType.Cancel);
		}
		
		protected void OnOk (object sender, EventArgs e)
		{
			dialog.Respond (ResponseType.Ok);
		}
		
		protected void OnManageSites (object sender, EventArgs e)
		{
			using (ManageSitesDialog dlg = new ManageSitesDialog ()) {
				dlg.Run ();
				FillRepos ();
			}
		}
		
		IProgressMonitor updateMonitor;
		
		protected void OnUpdateRepo (object sender, EventArgs e)
		{
			Thread t = new Thread (new ThreadStart (RunUpdate));
			updateMonitor = new MessageDialogProgressMonitor (true, true, false);
			t.Start ();
			updateMonitor.AsyncOperation.WaitForCompleted ();
			LoadAddins ();
		}
		
		void RunUpdate ()
		{
			try {
				Runtime.SetupService.UpdateRepositories (updateMonitor);
			} finally {
				updateMonitor.Dispose ();
			}
		}
		
		protected void OnRepoChanged (object sender, EventArgs e)
		{
			LoadAddins ();
		}
		
		public void OnPageChanged ()
		{
			switch (wizard.CurrentPage) {
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
			AddinInfo info = tree.ActiveAddin;
			if (info == null)
				return;
				
			if (info.Url != "")
				Gnome.Url.Show (info.Url);
		}
		
		protected void OnShowInfo (object sender, EventArgs e)
		{
			AddinInfo info = tree.ActiveAddin;
			if (info == null)
				return;

			using (AddinInfoDialog dlg = new AddinInfoDialog (info)) {
				dlg.Run ();
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
		
		public void SetUninstallMode (AddinInfo info)
		{
			btnPrev.Hide ();
			btnNext.Sensitive = true;
			
			uninstallId = info.Id;
			wizard.CurrentPage = 1;
			
			StringBuilder sb = new StringBuilder ();
			sb.Append (GettextCatalog.GetString ("<b>The following packages will be uninstalled:</b>\n\n"));
			sb.Append (info.Name + "\n\n");
			
			AddinSetupInfo[] sinfos = Runtime.SetupService.GetDependentAddins (info.Id, true);
			if (sinfos.Length > 0) {
				sb.Append (GettextCatalog.GetString ("<b>There are other add-ins that depend on the previous ones which will also be uninstalled:</b>\n\n"));
				foreach (AddinSetupInfo si in sinfos)
					sb.Append (si.Addin.Name + "\n");
			}
			
			labelSummary.Markup = sb.ToString ();
		}
		
		void FillAddinInfo ()
		{
			AddinInfo info = tree.ActiveAddin;
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
			
			sb.Append ("<b>").Append (GettextCatalog.GetString ("Version:")).Append ("</b>\n").Append (info.Version).Append ("\n\n");
			
			if (info.Author != "")
				sb.Append ("<b>").Append (GettextCatalog.GetString ("Author:")).Append ("</b>\n").Append (info.Author).Append ("\n\n");
			
			if (info.Copyright != "")
				sb.Append ("<b>").Append (GettextCatalog.GetString ("Copyright:")).Append ("</b>\n").Append (info.Copyright).Append ("\n\n");
			
			if (info.Dependencies.Count > 0) {
				sb.Append ("<b>").Append (GettextCatalog.GetString ("Add-in Dependencies:")).Append ("</b>\n");
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
			
			AddinInfo[] infos = tree.GetSelectedAddins ();
			PackageCollection packs = new PackageCollection ();
			foreach (AddinInfo info in infos) {
				AddinRepositoryEntry arep = (AddinRepositoryEntry) tree.GetAddinData (info);
				packs.Add (AddinPackage.FromRepository (arep));
			}
			
			packagesToInstall = new PackageCollection (packs);
			
			PackageCollection toUninstall;
			PackageDependencyCollection unresolved;
			bool res;
			
			NullProgressMonitor m = new NullProgressMonitor ();
			res = Runtime.SetupService.ResolveDependencies (m, packs, out toUninstall, out unresolved);
			
			StringBuilder sb = new StringBuilder ();
			if (!res) {
				sb.Append (GettextCatalog.GetString ("<b><span foreground=\"red\">The selected add-ins can't be installed because there are dependency conflicts.</span></b>\n"));
				foreach (ProgressError err in m.Errors) {
					sb.Append (GettextCatalog.GetString ("<b><span foreground=\"red\">" + err.Message + "</span></b>\n"));
				}
				sb.Append ("\n");
			}
			
			if (m.Warnings.Length != 0) {
				foreach (string w in m.Warnings) {
					sb.Append (GettextCatalog.GetString ("<b><span foreground=\"red\">" + w + "</span></b>\n"));
				}
				sb.Append ("\n");
			}
			
			sb.Append (GettextCatalog.GetString ("<b>The following packages will be installed:</b>\n\n"));
			foreach (Package p in packs) {
				sb.Append (p.Name);
				if (p is AddinPackage && !((AddinPackage)p).RootInstall)
					sb.Append (GettextCatalog.GetString (" (in user directory)"));
				sb.Append ("\n");
			}
			sb.Append ("\n");
			
			if (toUninstall.Count > 0) {
				sb.Append (GettextCatalog.GetString ("<b>The following packages need to be uninstalled:</b>\n\n"));
				foreach (Package p in toUninstall) {
					sb.Append (p.Name + "\n");
				}
				sb.Append ("\n");
			}
			
			if (unresolved.Count > 0) {
				sb.Append (GettextCatalog.GetString ("<b>The following dependencies could not be resolved:</b>\n\n"));
				foreach (PackageDependency p in unresolved) {
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
				okmessage = GettextCatalog.GetString ("The installation has been successfully completed.");
				errmessage = GettextCatalog.GetString ("The instalation failed!");
			} else {
				oper = new ThreadStart (RunUninstall);
				okmessage = GettextCatalog.GetString ("The uninstallation has been successfully completed.");
				errmessage = GettextCatalog.GetString ("The uninstallation failed!");
			}
			
			Thread t = new Thread (oper);
			t.Start ();
			
			installing = true;
			installMonitor.AsyncOperation.WaitForCompleted ();
			installing = false;
			
			wizard.NextPage ();

			if (installMonitor.AsyncOperation.Success) {
				imageError.Visible = false;
				imageInfo.Visible = true;
				txt = "<b>" + okmessage + "</b>\n\n";
				txt += GettextCatalog.GetString ("You need to restart MonoDevelop for the changes to take effect.");
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
				Runtime.SetupService.Install (installMonitor, packagesToInstall);
			} catch (Exception ex) {
				// Nothing
			} finally {
				installMonitor.Dispose ();
			}
		}
		
		void RunUninstall ()
		{
			try {
				Runtime.SetupService.Uninstall (installMonitor, uninstallId);
			} catch (Exception ex) {
				// Nothing
			} finally {
				installMonitor.Dispose ();
			}
		}
	}
	
	class InstallMonitor: BaseProgressMonitor
	{
		Label progressLabel;
		ProgressBar progressBar;
		ProgressBar mainProgressBar;
		
		public InstallMonitor (Label progressLabel, ProgressBar progressBar, ProgressBar mainProgressBar)
		{
			this.progressLabel = progressLabel;
			this.progressBar = progressBar;
			this.mainProgressBar = mainProgressBar;
		}
		
		protected override void OnProgressChanged ()
		{
			progressLabel.Text = CurrentTask;
			mainProgressBar.Fraction = GlobalWork;
			progressBar.Fraction = CurrentTaskWork;
		}
		
		protected override void OnWriteLog (string text)
		{
			Console.WriteLine (text);
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
			col.Title = GettextCatalog.GetString ("Repository");
			CellRendererText crt = new CellRendererText ();
			col.PackStart (crt, true);
			col.AddAttribute (crt, "text", ncol);
			treeView.AppendColumn (col);
		}
		
		protected override void UpdateRow (TreeIter iter, AddinInfo info, object dataItem, bool enabled, string icon)
		{
			base.UpdateRow (iter, info, dataItem, enabled, icon);
			AddinRepositoryEntry arep = (AddinRepositoryEntry) dataItem;
			treeStore.SetValue (iter, ncol, arep.Repository.Name);
		}
	}
}

