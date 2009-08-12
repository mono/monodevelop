
using System;
using System.Collections;
using System.Text;
using System.Collections.Specialized;
using MonoDevelop.Ide.Templates;
using MonoDevelop.Core;
using Mono.Addins;
using Mono.Addins.Description;
using MonoDevelop.Projects;
using MonoDevelop.Components;
using Gtk;

namespace MonoDevelop.AddinAuthoring
{
	[System.ComponentModel.Category("widget")]
	[System.ComponentModel.ToolboxItem(true)]
	public partial class AddinFeatureWidget : Gtk.Bin
	{
		bool loading;
		bool idSet;
		bool nsSet;
		bool isRoot;
		
		public AddinFeatureWidget ()
		{
			this.Build();
		}
		
		public void Load (Solution solution, DotNetProject project, bool forOptionsPanel)
		{
			AddinData data = AddinData.GetAddinData (project);
			if (data != null && !forOptionsPanel) {
				boxLibraryType.Visible = false;
				AddinDescription desc = data.CachedAddinManifest;
				if (project.CompileTarget != CompileTarget.Library || desc.IsRoot || solution.HasAddinRoot ()) {
					boxRepo.Visible = false;
					hseparator.Visible = false;
					isRoot = true;
				}
				else {
					if (solution.HasAddinRoot ())
						boxRepo.Visible = false;
					else {
						RegistryInfo app = solution.GetAddinData().ExternalRegistryInfo;
						if (app != null) {
							regSelector.RegistryInfo = app;
							regSelector.Sensitive = false;
						}
					}
					isRoot = false;
				}
				entryName.Text = project.Name;
				entryId.Text = project.Name;
			} else {
				if (project.CompileTarget != CompileTarget.Library) {
					labelExtensibleApp.Visible = data == null;
					boxRepo.Visible = false;
					boxLibraryType.Visible = false;
					hseparator.Visible = false;
					isRoot = true;
				}
				else {
					labelExtensibleApp.Visible = false;
					if (data != null && data.CachedAddinManifest != null && data.CachedAddinManifest.IsRoot)
						radiobuttonLibrary.Active = true;
					else
						radiobuttonAddin.Active = true;
					isRoot = radiobuttonLibrary.Active;
					if (solution.HasAddinRoot ())
						boxRepo.Visible = false;
				}
				if (data != null) {
					((Gtk.Container)tableNames.Parent).Remove (tableNames);
					((Gtk.Container)labelAddinInfo.Parent).Remove (labelAddinInfo);
				} else {
					entryName.Text = project.Name;
					entryId.Text = project.Name;
				}
			}
			if (project.CompileTarget == CompileTarget.Library) {
				if (radiobuttonLibrary.Active)
					labelAddinInfo.Text = AddinManager.CurrentLocalizer.GetString ("Library information:");
				else
					labelAddinInfo.Text = AddinManager.CurrentLocalizer.GetString ("Add-in information:");
			} else {
				labelAddinInfo.Text = AddinManager.CurrentLocalizer.GetString ("Application information:");
			}
			
			UpdateControls ();
		}

		protected virtual void OnButtonBrowseClicked(object sender, System.EventArgs e)
		{
			
/*			FolderDialog fd = new FolderDialog (AddinManager.CurrentLocalizer.GetString ("Select Repository"));
			
			try {
				fd.SetFilename (Environment.GetFolderPath (Environment.SpecialFolder.Personal));
				
				int response = fd.Run ();
				
				if (response == (int) ResponseType.Ok) {
					paths.Add (fd.Filename);
					UpdatePaths ();
					comboRepos.Active = paths.Count - 1;
				}
			} finally {
				fd.Destroy ();
			}
			*/
		}
		
		void UpdateControls ()
		{
			boxRepo.Sensitive = !isRoot;
		}

		public RegistryInfo RegistryInfo {
			get { return isRoot ? null : regSelector.RegistryInfo; }
		}
		
		public bool HasRegistryInfo {
			get { return boxRepo.Visible && boxRepo.Sensitive; }
		}
		
		public bool IsRoot {
			get { return isRoot; }
		}
		
		public string AddinName {
			get { return entryName.Text; }
		}
		
		public string AddinId {
			get { return entryId.Text; }
		}
		
		public string AddinNamespace {
			get { return comboNs.Entry.Text; }
		}
		
		public string Validate ()
		{
			if (!isRoot && RegistryInfo == null && boxRepo.Visible)
				return AddinManager.CurrentLocalizer.GetString ("Please select the application to be extended by this add-in.");
			else
				return null;
		}

		protected virtual void OnEntryIdChanged (object sender, System.EventArgs e)
		{
			if (!loading)
				idSet = true;
		}

		protected virtual void OnEntryNameChanged (object sender, System.EventArgs e)
		{
			if (!idSet) {
				loading = true;
				entryId.Text = GenerateId (entryName.Text);
				loading = false;
			}
		}
		
		string GenerateId (string id)
		{
			StringBuilder sb = new StringBuilder ();
			bool up = true;
			foreach (char c in id) {
				if (c == ' ' || (!char.IsLetterOrDigit (c) && c != '_')) {
					up = true;
					continue;
				}
				if (up) {
					sb.Append (char.ToUpper (c));
					up = false;
				}
				else
					sb.Append (c);
			}
			return sb.ToString ();
		}

		protected virtual void OnRegSelectorChanged (object sender, System.EventArgs e)
		{
			RegistryInfo ri = regSelector.RegistryInfo;
			AddinRegistry reg = new AddinRegistry (ri.RegistryPath, ri.ApplicationPath);
			Hashtable names = new Hashtable ();
			foreach (Addin ad in reg.GetAddinRoots ()) {
				if (ad.Namespace.Length > 0)
					names [ad.Namespace] = ad.Namespace;
			}
			foreach (Addin ad in reg.GetAddins ()) {
				if (ad.Namespace.Length > 0)
					names [ad.Namespace] = ad.Namespace;
			}
			
			loading = true;
			try {
				string uk = null;
				((Gtk.ListStore)comboNs.Model).Clear ();
				foreach (string s in names.Keys) {
					comboNs.AppendText (s);
					uk = s;
				}
				if (names.Count == 1 && !nsSet)
					comboNs.Entry.Text = uk;
			} finally {
				loading = false;
			}
		}

		protected virtual void OnComboNsChanged (object sender, System.EventArgs e)
		{
			if (!loading)
				nsSet = true;
		}

		protected virtual void OnRadiobuttonLibraryToggled (object sender, System.EventArgs e)
		{
			isRoot = radiobuttonLibrary.Active;
			UpdateControls ();
		}
	}
	
	public class AddinFeature: ISolutionItemFeature
	{
		public string Title {
			get { return AddinManager.CurrentLocalizer.GetString ("Extensibility"); }
		}
		
		public string Description {
			get { return AddinManager.CurrentLocalizer.GetString ("Support of extensibility with add-ins"); }
		}
		
		public bool SupportsSolutionItem (SolutionFolder parentCombine, SolutionItem entry)
		{
			return entry is DotNetProject;
		}

		public Widget CreateFeatureEditor (SolutionFolder parentFolder, SolutionItem entry)
		{
			AddinFeatureWidget w = new AddinFeatureWidget ();
			w.Load (parentFolder.ParentSolution, (DotNetProject)entry, false);
			return w;
		}
		
		public bool IsEnabled (SolutionFolder parentCombine, SolutionItem entry)
		{
			return AddinData.GetAddinData ((DotNetProject)entry) != null;
		}

		public string Validate (SolutionFolder parentCombine, SolutionItem entry, Widget ed)
		{
			AddinFeatureWidget editor = (AddinFeatureWidget) ed;
			return editor.Validate ();
		}

		public void ApplyFeature (SolutionFolder parentCombine, SolutionItem entry, Widget ed)
		{
			AddinFeatureWidget editor = (AddinFeatureWidget) ed;
			AddinData data = AddinData.EnableAddinAuthoringSupport ((DotNetProject) entry);
			
			DotNetProject project = (DotNetProject) entry;
			if (editor.HasRegistryInfo)
				project.ParentSolution.GetAddinData().ExternalRegistryInfo = editor.RegistryInfo;

			AddinDescription desc = data.LoadAddinManifest ();
			if (editor.AddinId.Length > 0)
				desc.LocalId = editor.AddinId;
			if (editor.AddinName.Length > 0)
				desc.Name = editor.AddinName;
			desc.Namespace = editor.AddinNamespace;
			desc.IsRoot = project.CompileTarget != CompileTarget.Library || editor.IsRoot;
			desc.Version = "1.0";
			desc.Save ();
			data.NotifyChanged ();
		}
	}
}
