
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
	public partial class AddinFeatureWidget : Gtk.Bin
	{
		StringCollection paths = new StringCollection ();
		bool loading;
		bool idSet;
		bool nsSet;
		
		public AddinFeatureWidget ()
		{
			this.Build();
		}
		
		public void Load (DotNetProject project, bool showNameEntry)
		{
			if (!showNameEntry) {
				((Gtk.Container)tableNames.Parent).Remove (tableNames);
				((Gtk.Container)labelAddinInfo.Parent).Remove (labelAddinInfo);
				((Gtk.Container)checkRoot.Parent).Remove (checkRoot);
			}
			
			AddinData data = AddinData.GetAddinData (project);
			if (data != null) {
				regSelector.RegistryPath = data.RegistryPath;
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
		}

		public string RegistryPath {
			get { return regSelector.RegistryPath; }
		}
		
		public string StartupPath {
			get { return regSelector.StartupPath; }
		}
		
		public bool IsRoot {
			get { return checkRoot.Active; }
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
			if (RegistryPath.Length == 0)
				return AddinManager.CurrentLocalizer.GetString ("Please select the location of the add-in registry");
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
			AddinRegistry reg = new AddinRegistry (regSelector.RegistryPath);
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
	}
	
	public class AddinFeature: ISolutionItemFeature
	{
		public string Title {
			get { return AddinManager.CurrentLocalizer.GetString ("Add-in Support"); }
		}
		
		public string Description {
			get { return AddinManager.CurrentLocalizer.GetString ("Add-in Support"); }
		}
		
		public bool SupportsSolutionItem (SolutionFolder parentCombine, SolutionItem entry)
		{
			return entry is DotNetProject;
		}

		public Widget CreateFeatureEditor (SolutionFolder parentCombine, SolutionItem entry)
		{
			AddinFeatureWidget w = new AddinFeatureWidget ();
			w.Load ((DotNetProject)entry, true);
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
			
			data.AddinRegistry = new AddinRegistry (editor.RegistryPath);
			
			AddinDescription desc = data.LoadAddinManifest ();
			if (editor.AddinId.Length > 0)
				desc.LocalId = editor.AddinId;
			if (editor.AddinName.Length > 0)
				desc.Name = editor.AddinName;
			desc.Namespace = editor.AddinNamespace;
			desc.IsRoot = editor.IsRoot;
			desc.Version = "1.0";
			desc.Save ();
			data.NotifyChanged ();
		}
	}
}
