
using System;
using System.Collections;
using System.Collections.ObjectModel;
using MonoDevelop.Core;
using MonoDevelop.Projects;

namespace MonoDevelop.Deployment.Gui
{
	[System.ComponentModel.Category("MonoDevelop.Deployment")]
	[System.ComponentModel.ToolboxItem(true)]
	internal partial class PackagingFeatureWidget : Gtk.Bin
	{
		SolutionItem entry;
		SolutionFolder parentFolder;
		ArrayList packages = new ArrayList ();
		PackagingProject newPackProject;
		bool creatingPackProject;
		
		public PackagingFeatureWidget (SolutionFolder parentFolder, SolutionItem entry)
		{
			this.Build();
			this.entry = entry;
			this.parentFolder = parentFolder;
			
			creatingPackProject = entry is PackagingProject;
			
			if (!creatingPackProject) {
				ReadOnlyCollection<PackagingProject> packProjects = parentFolder.ParentSolution.GetAllSolutionItems<PackagingProject> ();
				newPackProject = new PackagingProject ();
			
				string label = GettextCatalog.GetString ("Create packages for this project in a new Packaging Project");
				AddCreatePackageSection (box, label, newPackProject, packProjects.Count > 0);
			
				foreach (PackagingProject project in packProjects)
					AddProject (project);
			} else {
				string label = GettextCatalog.GetString ("Select packages to add to the new Packaging Project");
				AddCreatePackageSection (box, label, (PackagingProject) entry, false);
			}
		}
		
		void AddProject (PackagingProject project)
		{
			string pname = project.Name;
			SolutionFolder c = project.ParentFolder;
			while (c != null) {
				pname = c.Name + " / " + pname;
				if (c.IsRoot)
					break;
				c = c.ParentFolder;
			}
			
			// Get a list of packages that can contain the new project
			ArrayList list = new ArrayList ();
			foreach (Package p in project.Packages) {
				if (p.PackageBuilder.CanBuild (entry))
					list.Add (p);
			}
			
			string label;
			Gtk.VBox vbox;
			
			if (list.Count > 0)
			{
				label = GettextCatalog.GetString ("Add the new project to the Packaging Project '{0}'", pname);
				Gtk.CheckButton checkAddNew = new Gtk.CheckButton (label);
				checkAddNew.Show ();
				box.PackStart (checkAddNew, false, false, 0);
				
				Gtk.Widget hbox;
				AddBox (box, out hbox, out vbox);
				checkAddNew.Toggled += delegate {
					hbox.Visible = checkAddNew.Active;
					if (!checkAddNew.Active)
						DisableChecks (hbox);
				};
				
				// Options for adding the project to existing packages
				
				label = GettextCatalog.GetString ("Add the project to existing packages");
				Gtk.CheckButton checkAddExist = new Gtk.CheckButton (label);
				checkAddExist.Show ();
				vbox.PackStart (checkAddExist, false, false, 0);
				
				Gtk.VBox vboxPacks;
				Gtk.Widget thbox;
				AddBox (vbox, out thbox, out vboxPacks);
				checkAddExist.Toggled += delegate {
					thbox.Visible = checkAddExist.Active;
					if (!checkAddExist.Active)
						DisableChecks (thbox);
				};
				
				foreach (Package p in list) {
					label = p.Name;
					if (label != p.PackageBuilder.Description)
						label += " (" + p.PackageBuilder.Description + ")";
					Gtk.CheckButton checkPackage = new Gtk.CheckButton (label);
					checkPackage.Show ();
					vboxPacks.PackStart (checkPackage, false, false, 0);
					RegisterCheck (checkPackage, project, p);
				}
				
				// Options for creating new packages
				
				label = GettextCatalog.GetString ("Create new packages for the project");
				AddCreatePackageSection (vbox, label, project, true);
			}
			else {
				label = GettextCatalog.GetString ("Add new packages for this project in the packaging project '{0}'", pname);
				AddCreatePackageSection (box, label, project, true);
			}
		}
		
		void AddCreatePackageSection (Gtk.VBox vbox, string label, PackagingProject parentProject, bool showCheck)
		{
			Gtk.VBox vboxNewPacks;
			Gtk.Widget hbox;
			if (showCheck) {
				Gtk.CheckButton check = new Gtk.CheckButton (label);
				check.Show ();
				vbox.PackStart (check, false, false, 0);
				
				AddBox (vbox, out hbox, out vboxNewPacks);
				check.Toggled += delegate {
					hbox.Visible = check.Active;
					if (!check.Active)
						DisableChecks (hbox);
				};
			} else {
				Gtk.Label lab = new Gtk.Label (label);
				lab.Show ();
				vbox.PackStart (lab, false, false, 0);
				AddBox (vbox, out hbox, out vboxNewPacks);
				hbox.Show ();
			}
			
			ICollection builders;
			if (creatingPackProject)
				builders = DeployService.GetPackageBuilders ();
			else
				builders = DeployService.GetSupportedPackageBuilders (entry);
			
			foreach (PackageBuilder pb in builders) {
				if (creatingPackProject) {
					pb.SetSolutionItem (parentFolder.ParentSolution.RootFolder);
					// Add all compatible projects
					foreach (SolutionItem e in parentFolder.ParentSolution.GetAllSolutionItems ()) {
						if (pb.CanBuild (e))
							pb.AddEntry (e);
					}
				} else {
					pb.SetSolutionItem (parentFolder, new SolutionItem [] { entry });
				}
				
				PackageBuilder[] defp = pb.CreateDefaultBuilders ();
				if (defp.Length == 0)
					continue;
				if (defp.Length == 1)
					AddPackageBuilder (vboxNewPacks, parentProject, defp[0]);
				else {
					Gtk.CheckButton checkBuilder = new Gtk.CheckButton (pb.Description);
					checkBuilder.Show ();
					vboxNewPacks.PackStart (checkBuilder, false, false, 0);
					Gtk.VBox vboxDefPacks;
					Gtk.Widget thbox;
					AddBox (vboxNewPacks, out thbox, out vboxDefPacks);
					checkBuilder.Toggled += delegate {
						thbox.Visible = checkBuilder.Active;
						if (!checkBuilder.Active)
							DisableChecks (thbox);
					};
					foreach (PackageBuilder dp in defp)
						AddPackageBuilder (vboxDefPacks, parentProject, dp);
				}
			}
		}
		
		void AddPackageBuilder (Gtk.VBox parent, PackagingProject project, PackageBuilder pb)
		{
			Gtk.CheckButton check = new Gtk.CheckButton (pb.DefaultName);
			check.Show ();
			parent.PackStart (check, false, false, 0);
			Package pkg = new Package (pb);
			pkg.Name = pb.DefaultName;
			RegisterCheck (check, project, pkg);
		}
		
		void AddBox (Gtk.VBox parent, out Gtk.Widget box, out Gtk.VBox vbox)
		{
			Gtk.HBox hbox = new Gtk.HBox ();
			Gtk.Label sep = new Gtk.Label ("");
			sep.WidthRequest = 24;
			sep.Show ();
			hbox.PackStart (sep, false, false, 0);
			
			vbox = new Gtk.VBox ();
			vbox.Spacing = 6;
			vbox.Show ();
			hbox.PackStart (vbox, true, true, 0);
			
			parent.PackStart (hbox, false, false, 0);
			box = hbox;
		}
		
		void RegisterCheck (Gtk.CheckButton check, PackagingProject project, Package package)
		{
			PackageInfo pi = new PackageInfo ();
			pi.Check = check;
			pi.Project = project;
			pi.Package = package;
			packages.Add (pi);
		}
		
		void DisableChecks (Gtk.Widget w)
		{
			Gtk.CheckButton c = w as Gtk.CheckButton;
			if (c != null) {
				c.Active = false;
				return;
			}
			
			Gtk.Container co = w as Gtk.Container;
			if (co != null) {
				foreach (Gtk.Widget cw in co.Children)
					DisableChecks (cw);
			}
		}
		
		public void ApplyFeature ()
		{
			foreach (PackageInfo pi in packages) {
				if (!pi.Check.Active)
					continue;
				
				if (pi.Package.ParentProject == null)
					pi.Project.Packages.Add (pi.Package);
				else {
					pi.Package.PackageBuilder.AddEntry (parentFolder);
					pi.Package.PackageBuilder.AddEntry (entry);
				}
			}
			if (newPackProject != null && newPackProject.Packages.Count > 0) {
				newPackProject.Name = "Packages";
				newPackProject.FileName = System.IO.Path.Combine (parentFolder.BaseDirectory, "Packages.mdse");
				parentFolder.Items.Add (newPackProject);
			}
		}
	}
	
	class PackageInfo
	{
		public Gtk.CheckButton Check;
		public PackagingProject Project;
		public Package Package;
	}
}
