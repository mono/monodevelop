//
// MakefileData.cs
//
// Author:
//   Ankit Jain <jankit@novell.com>
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
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

using MonoDevelop.Core;
using MonoDevelop.Core.Execution;
using MonoDevelop.Core.ProgressMonitoring;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Projects.Serialization;
using MonoDevelop.Projects;

namespace MonoDevelop.Autotools
{
	[DataItem ("MakefileInfo")]
	public class MakefileData : ICloneable
	{
		bool integrationEnabled;
		string relativeMakefileName;
		string buildTargetName;
		string cleanTargetName;
		string executeTargetName;
		Project ownerProject;
		CustomMakefile makefile;

		public MakefileData ()
		{
			integrationEnabled = false;
			relativeMakefileName = String.Empty;
			buildTargetName = "";
			cleanTargetName = "clean";
			executeTargetName = "";
		}

		public Project OwnerProject {
			get { return ownerProject; }
			set {
				if (!Object.ReferenceEquals (ownerProject, value) && value != null) {
					//Setup handlers
					ProjectReferenceEventHandler refhandler = delegate {
						if (SyncReferences)
							dirty = true;
					};
					
					value.ReferenceRemovedFromProject += refhandler;
					value.ReferenceAddedToProject += refhandler;

					ProjectFileEventHandler filehandler = delegate (object sender, ProjectFileEventArgs e) {
						MakefileVar var = null;
						switch (e.ProjectFile.BuildAction) {
						case BuildAction.Compile:
							var = BuildFilesVar;
							break;
						case BuildAction.FileCopy:
							var = DeployFilesVar;
							break;
						case BuildAction.EmbedAsResource:
							var = ResourcesVar;
							break;
						case BuildAction.Nothing:
							var = OthersVar;
							break;
						}
						if (var != null && var.Sync)
							dirty = true;
					};

					value.FileRemovedFromProject += filehandler;
					value.FileAddedToProject += filehandler;

					value.FilePropertyChangedInProject += delegate {
						dirty = true;
					};

					value.FileRenamedInProject += delegate {
						dirty = true;
					};
				}
				ownerProject = value;
			}
		}

		[ItemProperty (DefaultValue = false)]
		public bool IntegrationEnabled {
			get { return integrationEnabled; }
			set { integrationEnabled = value;}
		}
		
		[ItemProperty (DefaultValue = "")]
		public string RelativeMakefileName {
			get {
				if (ownerProject == null)
					return relativeMakefileName;
				return ownerProject.GetRelativeChildPath (relativeMakefileName);
			}
			set {
				if (String.Compare (relativeMakefileName, value) == 0)
					return;

				relativeMakefileName = value;
				makefile = null;
				baseDirectory = null;
				InitBuildVars ();
			}
		}

		public string AbsoluteMakefileName {
			get {
				if (ownerProject == null)
					return relativeMakefileName;
				return ownerProject.GetAbsoluteChildPath (relativeMakefileName);
			}
		}

		string baseDirectory = null;
		public string BaseDirectory {
			get {
				//FIXME: Check for AbsoluteMakefileName == null or empty
				return Path.GetDirectoryName (AbsoluteMakefileName);
			}
		}

		[ItemProperty (DefaultValue = "all")]
		public string BuildTargetName {
			get { return buildTargetName; }
			set { buildTargetName = value;}
		}

		[ItemProperty (DefaultValue = "clean")]
		public string CleanTargetName {
			get { return cleanTargetName; }
			set { cleanTargetName = value;}
		}

		[ItemProperty]
		public string ExecuteTargetName {
			get { return executeTargetName; }
			set { executeTargetName = value;}
		}

		//Makefile variables
		MakefileVar buildFilesVar;
		[ItemProperty]
		public MakefileVar BuildFilesVar {
			get {
				if (buildFilesVar == null)
					buildFilesVar = new MakefileVar ();
				return buildFilesVar;
			}
			set { buildFilesVar = value; }
		}

		MakefileVar deployFilesVar;
		[ItemProperty]
		public MakefileVar DeployFilesVar {
			get {
				if (deployFilesVar == null)
					deployFilesVar = new MakefileVar ();
				return deployFilesVar;
			}
			set { deployFilesVar = value; }
		}

		MakefileVar resourcesVar;
		[ItemProperty]
		public MakefileVar ResourcesVar {
			get {
				if (resourcesVar == null)
					resourcesVar = new MakefileVar ();
				return resourcesVar;
			}
			set { resourcesVar = value; }
		}

		MakefileVar othersVar;
		[ItemProperty]
		public MakefileVar OthersVar {
			get {
				if (othersVar == null)
					othersVar = new MakefileVar ();
				return othersVar;
			}
			set { othersVar = value; }
    		}

		//Individual Sync of *RefVar is ignored, instead
		//SyncReferences is used
		bool syncReferences = false;
		[ItemProperty (DefaultValue = false)]
		public bool SyncReferences {
			get { return syncReferences; }
			set {
				GacRefVar.Sync = value;
				AsmRefVar.Sync = value;
				ProjectRefVar.Sync =value;
				syncReferences = value;
			}
		}

		bool SaveReferences = true;

		bool isAutotoolsProject;
		[ItemProperty (DefaultValue = false)]
		public bool IsAutotoolsProject {
			get { return isAutotoolsProject; }
			set {
				if (isAutotoolsProject == value)
					return;
				isAutotoolsProject = value;
				configuredPackages = null;
			}
		}

		public bool UseAutotools {
			get { return ConfiguredPackages != null; }
		}

		MakefileVar gacRefVar;
		[ItemProperty]
		public MakefileVar GacRefVar {
			get {
				if (gacRefVar == null)
					gacRefVar = new MakefileVar ();
				return gacRefVar;
			}
			set { gacRefVar = value; }
		}

		MakefileVar asmRefVar;
		[ItemProperty]
		public MakefileVar AsmRefVar {
			get {
				if (asmRefVar == null)
					asmRefVar = new MakefileVar ();
				return asmRefVar;
			}
			set { asmRefVar = value; }
		}

		MakefileVar projectRefVar;
		[ItemProperty]
		public MakefileVar ProjectRefVar {
			get {
				if (projectRefVar == null)
					projectRefVar = new MakefileVar ();
				return projectRefVar;
			}
			set { projectRefVar = value; }
		}

		string relativeConfigureInPath = String.Empty;
		[ItemProperty (DefaultValue = "")]
		//FIXME: Sanitize usage .. relative required or absolute ??
		public string RelativeConfigureInPath {
			get {
				return relativeConfigureInPath;
			}
			set {
				if (String.Compare (relativeConfigureInPath, value) == 0)
					return;

				relativeConfigureInPath = value;
				configuredPackages = null;

				if (String.IsNullOrEmpty (relativeConfigureInPath))
					return;

				relativeConfigureInPath = GetRelativePath (relativeConfigureInPath);
				InitBuildVars ();
			}
		}

		public string AbsoluteConfigureInPath {
			get { return GetAbsolutePath (RelativeConfigureInPath); }
		}

		ConfiguredPackagesManager configuredPackages = null;
		public ConfiguredPackagesManager ConfiguredPackages {
			get { return configuredPackages; }
		}

		string outputDirVar;
		[ItemProperty]
		public string OutputDirVar {
			get { return outputDirVar; }
			set { outputDirVar = value;}
    		}

		string assemblyNameVar;
		[ItemProperty]
		public string AssemblyNameVar {
			get { return assemblyNameVar; }
			set { assemblyNameVar = value;}
    		}

		public CustomMakefile Makefile {
			get {
				if (makefile == null)
					makefile = new CustomMakefile (AbsoluteMakefileName);
				return makefile;
			}
		}

		//FIXME: not required to be public, no property needed
		List<string> unresolvedReferences;
		List<string> UnresolvedReferences {
			get { 
				if (unresolvedReferences == null)
					unresolvedReferences = new List<string> ();
				return unresolvedReferences;
			}
    		}

		Dictionary<string, string> buildVariables ;
		public Dictionary<string, string> BuildVariables  {
			get {
				if (buildVariables == null)
					buildVariables = new Dictionary<string, string> ();
				return buildVariables;
			}
    		}

		bool dirty = false;

		public object Clone ()
		{
			MakefileData data = new MakefileData ();
			data.OwnerProject = this.OwnerProject;
			data.IntegrationEnabled = this.IntegrationEnabled;
			data.RelativeMakefileName = this.RelativeMakefileName;
			data.BuildTargetName = this.BuildTargetName;
			data.CleanTargetName = this.CleanTargetName;
			data.ExecuteTargetName = this.ExecuteTargetName;
			
			data.BuildFilesVar = new MakefileVar (this.BuildFilesVar);
			data.DeployFilesVar = new MakefileVar (this.DeployFilesVar);
			data.ResourcesVar = new MakefileVar (this.ResourcesVar);
			data.OthersVar = new MakefileVar (this.OthersVar);
			data.GacRefVar = new MakefileVar (this.GacRefVar);
			data.AsmRefVar = new MakefileVar (this.AsmRefVar);
			data.ProjectRefVar = new MakefileVar (this.ProjectRefVar);

			data.SyncReferences = this.SyncReferences;
			data.IsAutotoolsProject = this.IsAutotoolsProject;
			data.RelativeConfigureInPath = this.RelativeConfigureInPath;
			data.OutputDirVar = this.OutputDirVar;
			data.AssemblyNameVar = this.AssemblyNameVar;

			data.unresolvedReferences = new List<string> (this.UnresolvedReferences);

			return data;
		}

		void InitBuildVars ()
		{
			if (!String.IsNullOrEmpty (RelativeConfigureInPath)) {
				BuildVariables ["top_srcdir"] = RelativeConfigureInPath;
				BuildVariables ["top_builddir"] = RelativeConfigureInPath;
			}

			if (AbsoluteMakefileName != String.Empty) {
				BuildVariables ["srcdir"] = BaseDirectory;
			}
		}

		public string GetRelativePath (string path)
		{
			if (Path.IsPathRooted (path))
				return Runtime.FileService.AbsoluteToRelativePath (BaseDirectory, path);
			else
				return path;
		}

		public string GetAbsolutePath (string path)
		{
			if (Path.IsPathRooted (path))
				return path;
			else
				return Runtime.FileService.RelativeToAbsolutePath (BaseDirectory, path);
		}

		public void Save ()
		{
			Makefile.Save ();
		}

		IProgressMonitor monitor = null;

		//use events.. 
		public void UpdateProject (IProgressMonitor monitor, bool promptForRemoval)
		{
			if (!IntegrationEnabled)
				return;

			if (ownerProject == null)
				throw new InvalidOperationException (GettextCatalog.GetString ("Internal Error: ownerProject not set"));

			this.monitor = monitor;

			try {
				Makefile.GetVariables ();
			} catch (Exception e) {
				monitor.ReportError (GettextCatalog.GetString (
					"Invalid makefile : {0}. Disabling Makefile integration.", AbsoluteMakefileName), e);
				IntegrationEnabled = false;

				return;
			}

			//FIXME: Improve the message
			if (promptForRemoval) {
				int choice = IdeApp.Services.MessageService.ShowCustomDialog (
						GettextCatalog.GetString ("Enable Makefile integration"),
						GettextCatalog.GetString ("Enabling Makefile integration. You can choose to have either the Project or the Makefile be used as the master copy. This is done only when enabling this feature. After this, the Makefile will be taken as the master copy."),
						"Project", "Makefile");

				if (choice == 0) {
					//Sync Project --> Makefile
					dirty = true;
					return;
				}
			}

			dirty = true;
			try {
				if (IsAutotoolsProject)
					configuredPackages = new ConfiguredPackagesManager (Path.Combine (AbsoluteConfigureInPath, "configure.in"));
			} catch (Exception e) {
				Console.WriteLine (String.Format (
					"Error trying to read configure.in : {0} for project {1} : {2} ",
					AbsoluteConfigureInPath, OwnerProject.Name, e.ToString ()));

				monitor.ReportWarning (GettextCatalog.GetString (
					"Error trying to read configure.in ({0}) for project {1} : {2} ",
					AbsoluteConfigureInPath, OwnerProject.Name, e.Message));
			}

			ReadFiles (BuildFilesVar, BuildAction.Compile, "Build", promptForRemoval);
			ReadFiles (DeployFilesVar, BuildAction.FileCopy, "Deploy", promptForRemoval);
			ReadFiles (OthersVar, BuildAction.Nothing, "Others", promptForRemoval);
			ReadFiles (ResourcesVar, BuildAction.EmbedAsResource, "Resources", promptForRemoval);

			if (!SyncReferences)
				return;

			try {
				SaveReferences = true;

				//Clear all Assembly & Project references
				RemoveReferences (ReferenceType.Assembly);
				RemoveReferences (ReferenceType.Project);

				//Do these for DotNetProject only
				DotNetProject dotnetProject = ownerProject as DotNetProject;
				if (dotnetProject != null) {
					GacRefVar.Extra.Clear ();
					AsmRefVar.Extra.Clear ();
					ProjectRefVar.Extra.Clear ();

					ReadReferences (GacRefVar, ReferenceType.Gac, "Gac References", dotnetProject);

					// !SaveReferences indicates that previous ref reading failed
					if (SaveReferences && String.Compare (AsmRefVar.Name, GacRefVar.Name) != 0)
						ReadReferences (AsmRefVar, ReferenceType.Assembly, "Asm References", dotnetProject);
					if (SaveReferences && (String.Compare (ProjectRefVar.Name, GacRefVar.Name) != 0) && 
						(String.Compare (ProjectRefVar.Name, AsmRefVar.Name) != 0))
						ReadReferences (ProjectRefVar, ReferenceType.Project, "Project References", dotnetProject);
					
					//Resolve References
					if (ownerProject.RootCombine != null)
						ResolveProjectReferences (ownerProject.RootCombine);
				}
			} catch (Exception e) {
				Console.WriteLine ("Error in loading references : {0}. Skipping syncing of references", e.Message);
				monitor.ReportWarning (GettextCatalog.GetString (
					"Error in loading references : {0}. Skipping syncing of references", e.Message));

				SaveReferences = false;
			}

			this.monitor = null;
		}

		void RemoveReferences (ReferenceType refType)
		{
			List<ProjectReference> toRemove = new List<ProjectReference> ();

			foreach (ProjectReference pref in ownerProject.ProjectReferences)
				if (pref.ReferenceType == refType)
					toRemove.Add (pref);

			foreach (ProjectReference pref in toRemove)
				ownerProject.ProjectReferences.Remove (pref);
		}

		void ReadFiles (MakefileVar fileVar, BuildAction buildAction, string id, bool promptForRemoval)
		{
			try { 
				fileVar.SaveEnabled = true;
				ReadFilesActual (fileVar, buildAction, id, promptForRemoval);
			} catch (Exception e) {
				Console.WriteLine ("Error in loading files for {0}. Skipping.", id);
				monitor.ReportWarning (GettextCatalog.GetString ("Error in loading files for {0}. Skipping.", id));
				fileVar.SaveEnabled = false;
			}
		}

		void ReadFilesActual (MakefileVar fileVar, BuildAction buildAction, string id, bool promptForRemoval)
		{
			fileVar.Extra.Clear ();
			if (!fileVar.Sync || fileVar.Name == String.Empty)
				return;

			//All filenames are treated as relative to the Makefile path
			List<string> files = Makefile.GetListVariable (fileVar.Name);
			if (files == null) {
				//FIXME: Move this to the caller, try-catch there
				Console.WriteLine (GettextCatalog.GetString (
					"Makefile variable '{0}' not found. Skipping syncing of {1} File list for project {2}.",
					fileVar.Name, id, ownerProject.Name));

				monitor.ReportWarning (GettextCatalog.GetString (
					"Makefile variable '{0}' not found. Skipping syncing of {1} File list for project {2}.",
					fileVar.Name, id, ownerProject.Name));

				fileVar.SaveEnabled = false;
				return;
			}

			//FIXME: Trim?
			bool usePrefix = !String.IsNullOrEmpty (fileVar.Prefix);
			int len = 0;
			if (fileVar.Prefix != null)
				len = fileVar.Prefix.Length;

			Dictionary<string, ProjectFile> existingFiles = new Dictionary<string, ProjectFile> ();
			foreach (ProjectFile pf in ownerProject.ProjectFiles) {
				if (pf.BuildAction == buildAction)
					existingFiles [ownerProject.GetAbsoluteChildPath (pf.FilePath)] = pf;
			}

			// True if the user has been warned that filenames contain Variables
			// but no configure.in path is set
			bool autotoolsWarned = false;

			foreach (string f in files) { 
				string fname = f.Trim ();
				
				try { 
					if (usePrefix && String.Compare (fileVar.Prefix, 0, fname, 0, len) == 0)
						//FIXME: If it doesn't match, then?
						fname = fname.Substring (len);

					string resourceId = null;
					if (buildAction == BuildAction.EmbedAsResource && fname.IndexOf (',') >= 0) {
						string [] tmp = fname.Split (new char [] {','}, 2);
						fname = tmp [0];
						if (tmp.Length > 1)
							resourceId = tmp [1];
					}

					if ((fname.Length > 2 && fname [0] == '$' && fname [1] == '(') && !UseAutotools) {
						fileVar.Extra.Add (f);
						if (!autotoolsWarned) {
							Console.WriteLine (GettextCatalog.GetString (
								"Files in variable '{0}' contain variables which cannot be parsed without path " +
								"to the configure.in being set. Ignoring such files.", fileVar.Name));

							monitor.ReportWarning (GettextCatalog.GetString (
								"Files in variable '{0}' contain variables which cannot be parsed without path " +
								"to the configure.in being set. Ignoring such files.", fileVar.Name));
							autotoolsWarned = true;
						}
						continue;
					}

					fname = ResolveBuildVars (fname);

					//File path in the makefile are relative to the makefile,
					//but have to be added to the project as relative to project.BaseDirectory
					string absPath = Path.GetFullPath (Path.Combine (BaseDirectory, fname));

					if (existingFiles.ContainsKey (absPath)) {
						existingFiles.Remove (absPath);
						continue;
					}

					if (!File.Exists (absPath)) {
						//Invalid file, maybe we couldn't parse it correctly!
						Console.WriteLine ("Invalid file : '{0}'. Ignoring", fname);
						fileVar.Extra.Add (f);
						continue;
					}

					ProjectFile pf = ownerProject.AddFile (absPath, buildAction);
					if (buildAction == BuildAction.EmbedAsResource && resourceId != null)
						pf.ResourceId = resourceId;
				} catch (Exception e) {
					fileVar.Extra.Add (f);
				}
			}

			if (existingFiles.Count > 0) {
				foreach (ProjectFile file in existingFiles.Values)
					ownerProject.ProjectFiles.Remove (file);
			}
		}

		Dictionary<string, ProjectReference> existingGacRefs = null;

		void ReadReferences (MakefileVar refVar, ReferenceType refType, string id, DotNetProject project)
		{
			if (refVar.Name == String.Empty || project == null)
				return;

			//All filenames are treated as relative to the Makefile path
			List<string> references = Makefile.GetListVariable (refVar.Name);
			if (references == null) {
				
				Console.WriteLine (GettextCatalog.GetString (
					"Makefile variable '{0}' not found. Skipping syncing of {1} all references for project {2}.",
					refVar.Name, id, project.Name));

				monitor.ReportWarning (GettextCatalog.GetString (
					"Makefile variable '{0}' not found. Skipping syncing of {1} all references for project {2}.",
					refVar.Name, id, project.Name));
				SaveReferences = false;
				return;
			}

			existingGacRefs = new Dictionary<string, ProjectReference> ();
			foreach (ProjectReference pref in OwnerProject.ProjectReferences) {
				if (pref.ReferenceType == ReferenceType.Gac) {
					string [] files = pref.GetReferencedFileNames ();
					if (files != null)
						existingGacRefs [files [0]] = pref;
				}
			}

			//FIXME: Trim?
			bool usePrefix = !String.IsNullOrEmpty (refVar.Prefix);
			int len = 0;
			if (refVar.Prefix != null)
				len = refVar.Prefix.Length;

			ReferencedPackages.Clear ();
			foreach (string r in references) {
				//Handle -r:System,System.Data also
				try { 
					ParseReference (r.Trim (), usePrefix, refVar.Prefix, len, refType, 
						project, refVar.Extra);
				} catch (Exception e) {
					Console.WriteLine ("Unable to parse reference '{0}'. Ignoring.", r);
					monitor.ReportWarning (GettextCatalog.GetString (
						"Unable to parse reference '{0}', reason : {1}. Ignoring.", r, e.Message));
					refVar.Extra.Add (r);
				}
			}

			if (existingGacRefs.Count > 0) {
				foreach (ProjectReference pref in existingGacRefs.Values)
					project.ProjectReferences.Remove (pref);
			}

			referencedPackages = null;
			existingGacRefs = null;
		}

		//FIXME: Doesn't need to be public
		List<string> referencedPackages;
		public List<string> ReferencedPackages  {
			get {
				if (referencedPackages == null)
					referencedPackages = new List<string> ();
				return referencedPackages;
			}
    		}

		//FIXME: change return type to bool, on false, extra.Add (reference)
		void ParseReference (string reference, bool usePrefix, string prefix, int len, ReferenceType refType, 
				DotNetProject project, List<string> extra)
		{
			string rname = reference;
			if (rname.Length > 3 && rname [0] == '$' && rname [1] == '(' && rname [rname.Length - 1] == ')') {
				if (!UseAutotools) {
					extra.Add (reference);
					return;
				}

				// Autotools based project

				if (!rname.EndsWith ("_LIBS)")) {
					//Not a pkgconfig *_LIBS var
					extra.Add (reference);
					return;
				}

				string pkgVarName = rname.Substring (2, rname.Length - 3).Replace ("_LIBS", "");
				List<string> pkgNames = ConfiguredPackages.GetNamesFromVarName (pkgVarName);
				if (pkgNames == null) {
					Console.WriteLine ("Package named '{0}' not found in configure.in. Ignoring reference to {1}",
						pkgVarName, rname);
					extra.Add (reference);
					return;
				}

				bool added = false;
				foreach (string pkgName in pkgNames) {
					if (ReferencedPackages.Contains (pkgName))
						continue;

					// Add all successfully added packages to ReferencedPackages
					if (LoadPackageReference (pkgName, project, prefix)) {
						ReferencedPackages.Add (pkgName);
						added = true;
					}
				}

				// none of the packages could be added
				if (!added)
					extra.Add (reference);

				return;
			}

			if (rname.StartsWith ("-pkg:")) {
				string pkgName = rname.Substring (5).Trim ();
				//-pkg:foo,bar
				foreach (string s in pkgName.Split (new char [] {','})) {
					if (ReferencedPackages.Contains (s))
						continue;

					if (LoadPackageReference (s, project, prefix))
						ReferencedPackages.Add (s);
					else
						//extra.Add (reference);
						extra.Add ("-pkg:" + s);
				}

				return;
			}

			if (usePrefix && String.Compare (prefix, 0, rname, 0, len) == 0)
				rname = rname.Substring (len);

			//FIXME: try/catch around the split refs ?
			foreach (string r in rname.Split (new char [] {','})) {
				string refname = r;
				if (refname.StartsWith ("$(") && !UseAutotools) {
					//Eg. -r:$(top_builddir)/foo.dll
					extra.Add (reference);
					continue;
				}

				refname = ResolveBuildVars (refname);

				//if refname is part of a package then add as gac
				if (refname.IndexOf (Path.DirectorySeparatorChar) < 0 &&
					ParseReferenceAsGac (refname, project) != null)
					continue;
				
				string fullpath = Path.GetFullPath (Path.Combine (BaseDirectory, refname));
				if (existingGacRefs.ContainsKey (fullpath)) {
					existingGacRefs.Remove (fullpath);
					continue;
				}

				// Check that its a valid assembly
				string fullname = null;
				try {
					fullname = AssemblyName.GetAssemblyName (fullpath).FullName;
				} catch {
					Console.WriteLine (GettextCatalog.GetString (
						"Invalid assembly reference : {0}. Ignoring.", refname));

					monitor.ReportWarning (GettextCatalog.GetString (
						"Invalid assembly reference : {0}. Ignoring.", refname));
					extra.Add (reference);
					continue;
				}

				if (Runtime.SystemAssemblyService.GetPackageFromPath (fullpath) != null && fullname != null) {
					ProjectReference pref = new ProjectReference (ReferenceType.Gac, fullname);
					project.ProjectReferences.Add (pref);
					continue;
				}

				//Else add to unresolved project refs
				UnresolvedReferences.Add (fullpath);
			}
		}

		bool LoadPackageReference (string pkgName, DotNetProject project, string prefix)
		{
			SystemPackage pkg = Runtime.SystemAssemblyService.GetPackage (pkgName);
			if (pkg == null) {
				Console.WriteLine ("Error: No package named '{0}' found. Ignoring.", pkgName);
				return false;
			}

			foreach (string s in pkg.Assemblies) {
				try {
					//Get fullname of the assembly
					string fullname = AssemblyName.GetAssemblyName (s).ToString ();

					//ParseReference (fullname, false, prefix, 0, ReferenceType.Gac, project, null);
					string fullpath = Path.GetFullPath (s);
					ProjectReference pref = null;
					if (existingGacRefs.ContainsKey (fullpath)) {
						pref = existingGacRefs [fullpath];
						//FIXME: if dup gac refs are there in the makefile, then 
						//the second will get added, and so the project will now have dups!
						existingGacRefs.Remove (fullpath);
						return true;
					}

					pref = new ProjectReference (ReferenceType.Gac, fullname);
					project.ProjectReferences.Add (pref);
				} catch {
					//Ignore
					Console.WriteLine ("Error: Invalid assembly reference ({0}) found in package {1}. Ignoring.",
						s, pkg.Name);
				}
			}

			return true;
		}

		ProjectReference ParseReferenceAsGac (string rname, DotNetProject project)
		{
			string aname = rname;
			if (rname.EndsWith (".dll", StringComparison.InvariantCultureIgnoreCase))
				//-r:Mono.Posix.dll
				aname = rname.Substring (0, rname.Length - 4);

			string fullname = Runtime.SystemAssemblyService.GetAssemblyFullName (aname);
			if (fullname == null)
				return null;

			fullname = Runtime.SystemAssemblyService.GetAssemblyNameForVersion (fullname, project.ClrVersion);
			if (fullname == null)
				return null;

			ProjectReference pref = null;

			string fullpath = Runtime.SystemAssemblyService.GetAssemblyLocation (fullname);
			if (existingGacRefs.ContainsKey (fullpath)) {
				pref = existingGacRefs [fullpath];
				existingGacRefs.Remove (fullpath);
				return pref;
			}

			pref = new ProjectReference (ReferenceType.Gac, fullname);
			project.ProjectReferences.Add (pref);

			return pref;
		}

		public static void ResolveProjectReferences (Combine c)
		{
			CombineEntryCollection projects = c.GetAllProjects ();
			foreach (Project sproj in projects) {
				MakefileData mdata = sproj.ExtendedProperties ["MonoDevelop.Autotools.MakefileInfo"] as MakefileData;
				if (mdata == null || mdata.UnresolvedReferences.Count == 0)
					continue;

				List<string> toRemove = new List<string> ();
				foreach (string refstr in mdata.UnresolvedReferences) {
					//Resolve
					foreach (Project p in projects) {
						if (String.Compare (p.GetOutputFileName (), refstr) == 0) {
							sproj.ProjectReferences.Add (new ProjectReference (p));
							toRemove.Add (refstr);
							break;
						}
					}
				}

				foreach (string s in toRemove)
					mdata.UnresolvedReferences.Remove (s);
			}
		}

		string ResolveBuildVars (string filename)
		{   
			if (filename.IndexOf ('$') < 0)
				return filename;

			StringBuilder sb = new StringBuilder ();
			char c;
			int len = filename.Length;
			for (int i = 0; i < len; i ++) {
				c = filename [i];
				if (c != '$' || (i + 3 >= len)) {
					sb.Append (c);
					continue;
				}

				if (filename [i + 1] == '(') {
					int j = i + 2;
					while (j < len && filename [j] != ')')
						j ++;

					if (j >= len) {
						sb.Append (filename.Substring (i));
						break;
					}

					string varname = filename.Substring (i + 2, j - (i + 2));

					if (BuildVariables.ContainsKey (varname))
						sb.Append (BuildVariables [varname]);
					else
						sb.Append ("$(" + varname + ")");
					i = j;
				}
			}

			return sb.ToString ();
		}

		//Converts a absolute filename to use the specified buildvar like top_builddir,
		//if applicable
		public string EncodeFileName (string filename, string varname, bool isAbsolute)
		{
			if (!UseAutotools)
				return filename;

			string varpath = null;
			if (isAbsolute)
				varpath = GetAbsolutePath (BuildVariables [varname]);
			else
				varpath = GetRelativePath (BuildVariables [varname]);

			if (filename.StartsWith (varpath))
				return String.Format ("$({0}){1}{2}", varname, Path.DirectorySeparatorChar, filename.Substring (varpath.Length));

			return filename;
		}

		//Writing methods

		public void UpdateMakefile (IProgressMonitor monitor)
		{
			//FIXME: AssemblyName & OutputDir

			if (!dirty || !IntegrationEnabled)
				return;

			this.monitor = monitor;

			if (UnresolvedReferences.Count > 0) {
				//Add all references that could not be resolved as Asm refs
				foreach (string s in UnresolvedReferences)
					ownerProject.ProjectReferences.Add (new ProjectReference (ReferenceType.Assembly, s));

				//Clearing it
				UnresolvedReferences.Clear ();
			}

			bool makeRelative = (OwnerProject.BaseDirectory != BaseDirectory);

			//FIXME: If anything fails while writing, skip completely
			//FIXME: All the file vars must be distinct
			WriteFiles (BuildFilesVar, BuildAction.Compile, makeRelative, "Build");
			WriteFiles (DeployFilesVar, BuildAction.FileCopy, makeRelative, "Deploy");
			WriteFiles (OthersVar, BuildAction.Nothing, makeRelative, "Others");
			WriteFiles (ResourcesVar, BuildAction.EmbedAsResource, makeRelative, "Resources");

			if (SyncReferences && SaveReferences) {
				Makefile.ClearVariableValue (GacRefVar.Name);
				Makefile.ClearVariableValue (AsmRefVar.Name);
				Makefile.ClearVariableValue (ProjectRefVar.Name);

				WriteReferences (GacRefVar, ReferenceType.Gac, makeRelative, "Gac");
				WriteReferences (AsmRefVar, ReferenceType.Assembly, makeRelative, "Assembly");
				WriteReferences (ProjectRefVar, ReferenceType.Project, makeRelative, "Project");
			}

			Save ();
			dirty = false;

			this.monitor = null;
		}

		bool WriteFiles (MakefileVar fileVar, BuildAction action, bool makeRelative, string id)
		{
			if (!fileVar.Sync || !fileVar.SaveEnabled)
				return false;

			if (Makefile.GetListVariable (fileVar.Name) == null) {
				//Var not found, skip
                                Console.WriteLine (GettextCatalog.GetString (
                                        "Makefile variable '{0}' not found. Skipping writing of {1} Files to the makefile.", fileVar.Name, id));
                                monitor.ReportWarning (GettextCatalog.GetString (
                                        "Makefile variable '{0}' not found. Skipping writing of {1} Files to the makefile.", fileVar.Name, id));
				return false;
			}

			List<string> files = new List<string> ();
			foreach (ProjectFile pf in OwnerProject.ProjectFiles) {
				if (pf.BuildAction == action) {
					string str = null;
					if (makeRelative)
						//Files are relative to the Makefile
						str = GetRelativePath (pf.FilePath);
					else
						str = pf.RelativePath;

					if (pf.IsExternalToProject)
						str = EncodeFileName (str, "top_srcdir", false);
					else
						str = EncodeFileName (str, "srcdir", false);

					if (pf.BuildAction == BuildAction.EmbedAsResource)
						//FIXME: When to write resource ids?
						str = String.Format ("{0}{1},{2}", fileVar.Prefix, str, pf.ResourceId);
					else
						str = String.Format ("{0}{1}", fileVar.Prefix, str);

					files.Add (str);
				}
			}

			foreach (string s in fileVar.Extra)
				files.Add (s);

			Makefile.SetListVariable (fileVar.Name, files);
			return true;
		}

		bool WriteReferences (MakefileVar refVar, ReferenceType refType, bool makeRelative, string id)
		{
			//Reference vars can be shared too, so use existing list
			//Eg. REF for both Gac and Asm ref
			List<string> references = Makefile.GetListVariable (refVar.Name);
			if (references == null) {
				//Var not found, skip
                                Console.WriteLine (GettextCatalog.GetString (
                                        "Makefile variable '{0}' not found. Skipping syncing of {1} references.", refVar.Name, id));

                                monitor.ReportWarning (GettextCatalog.GetString (
                                        "Makefile variable '{0}' not found. Skipping syncing of {1} references.", refVar.Name, id));
				return false;
			}

			//if .Value is true
			//	key -> Varname, Emit as $key_LIBS
			//if .Value is false
			//	key -> pkgname, emit as -pkg:$key
			Dictionary<string, bool> hasAcSubstPackages = new Dictionary<string, bool> ();

			foreach (ProjectReference pr in OwnerProject.ProjectReferences) {
				if (pr.ReferenceType != refType)
					continue;

				string refstr = String.Empty;
				switch (refType) {
				case ReferenceType.Gac:
					//Assemblies coming from packages are always added as Gac
					refstr = GacRefToString (pr, hasAcSubstPackages);
					if (refstr == null)
						continue;
					break;
				case ReferenceType.Assembly:
					refstr = AsmRefToString (pr.Reference);
					break;
				case ReferenceType.Project:
					refstr = ProjectRefToString (pr);
					if (refstr == null)
						continue;
					break;
				default:
					//not supported
					continue;
				}

				references.Add (String.Format ("{0}{1}", refVar.Prefix, refstr));
			}

			//Add packages
			foreach (KeyValuePair<string, bool> pair in hasAcSubstPackages) {
				if (pair.Value)
					references.Add (String.Format ("$({0}_LIBS)", pair.Key));
				else
					references.Add (String.Format ("-pkg:{0}", pair.Key));
			}

			foreach (string s in refVar.Extra)
				references.Add (s);

			Makefile.SetListVariable (refVar.Name, references);
			return true;
		}

		string GacRefToString (ProjectReference pr, Dictionary<string, bool> hasAcSubstPackages)
		{
			//Gac ref can be a full name OR a path!
			//FIXME: Use GetReferencedFileName and GetPackageFromPath ?
			string fullname = pr.Reference;
			SystemPackage pkg = Runtime.SystemAssemblyService.GetPackageFromFullName (pr.Reference);
			if (pkg == null) {
				//reference could be a path
				pkg = Runtime.SystemAssemblyService.GetPackageFromPath (Path.GetFullPath (pr.Reference));
				if (pkg != null) {
					//Path
					try {
						fullname = AssemblyName.GetAssemblyName (pr.Reference).FullName;
					} catch {
						//Invalid assembly!
						//let it fall through and be emitted as a asm ref
						pkg = null;
					}
				}
			}

			if (pkg == null)
				return AsmRefToString (pr.GetReferencedFileNames () [0]);

			// Reference is from a package

			if (pkg.IsCorePackage)
				//pkg:mono, Eg. System, System.Data etc
				return fullname.Split (new char [] {','}, 2) [0];

			//Ref is from a non-core package
			string varname = null;
			if (UseAutotools)
				//Check whether ref'ed in configure.in
				varname = ConfiguredPackages.GetVarNameFromName (pkg.Name);

			if (varname == null) {
				//Package not referenced in configure.in
				//Or not a autotools based project,
				//so emit -pkg:

				if (!hasAcSubstPackages.ContainsKey (pkg.Name)) {
					if (UseAutotools) {
						//Warn only if UseAutotools
						Console.WriteLine (GettextCatalog.GetString (
							"A reference to package '{0}' is being emitted, as atleast one assembly from the package " +
							"is used in the project. But it is not specified in the configure.in, you might need to " +
							"add it for successfully building on other systems.", pkg.Name));

						monitor.ReportWarning (GettextCatalog.GetString (
							"A reference to package '{0}' is being emitted, as atleast one assembly from the package " +
							"is used in the project. But it is not specified in the configure.in, you might need to " +
							"add it for successfully building on other systems.", pkg.Name));
					}

					hasAcSubstPackages [pkg.Name] = false;
				}
			} else {
				// If the package as AC_SUBST(FOO_LIBS) defined, then
				// emit FOO_LIBS, else emit -pkg:foo
				if (ConfiguredPackages.HasAcSubst (varname + "_LIBS")) {
					hasAcSubstPackages [varname] = true;
				} else {
					hasAcSubstPackages [pkg.Name] = false;
				}
			}

			return null;
		}

		string AsmRefToString (string reference)
		{
			if (reference.StartsWith (BaseDirectory))
				return GetRelativePath (reference);
			else
				//Reference is external to this project
				return EncodeFileName (reference, "top_builddir", true);
		}

		string ProjectRefToString (ProjectReference pr)
		{
			string [] tmp = pr.GetReferencedFileNames ();
			if (tmp == null || tmp.Length == 0)
				//Reference not found, ignoring
				return null;

			return AsmRefToString (tmp [0]);
		}

	}

	//FIXME: ConfiguredPackagedStore ?
	//This should be shared by all projects, having the same configure.in
	public class ConfiguredPackagesManager
	{
		Dictionary<string, List<string>> pkgVarNameToPkgName;
		Dictionary<string, string> pkgNameToPkgVarName;

		//This dict has entries for all vars that have a
		//AC_SUBST(VARNAME_LIBS) defined
		Dictionary<string, string> varNameAcSubst;

		string fullpath;

		public ConfiguredPackagesManager (string fullpath)
		{
			this.fullpath = fullpath;
			
			if (!File.Exists (fullpath) || String.Compare (Path.GetFileName (fullpath), "configure.in") != 0)
				//FIXME: Exception type?
				throw new ArgumentException (GettextCatalog.GetString ("Unable to find configure.in at {0}", fullpath));

			ReadPackagesList ();
		}

		//Gets the pkg-config name from the makefile (or autoconf?) var name
		public List<string> GetNamesFromVarName (string varname)
		{
			if (!pkgVarNameToPkgName.ContainsKey (varname)) {
				Console.WriteLine ("pkg-config variable {0} not found in pkgVarNameToPkgName.", varname);
				return null;
			}

			return pkgVarNameToPkgName [varname];
		}

		//Gets the pkg-config varname from a package name (info comes from configure.in)
		public string GetVarNameFromName (string name)
		{
			if (!pkgNameToPkgVarName.ContainsKey (name)) {
				Console.WriteLine ("Package named '{0}' not specified in configure.in", name);
				return null;
			}

			return pkgNameToPkgVarName [name];
		}

		public bool HasAcSubst (string varname)
		{
			return varNameAcSubst.ContainsKey (varname);
		}

		void ReadPackagesList ()
		{
			pkgVarNameToPkgName = new Dictionary<string, List<string>> ();
			pkgNameToPkgVarName = new Dictionary<string, string> ();
			varNameAcSubst = new Dictionary<string, string> ();

			string content = null;
			using (StreamReader sr = new StreamReader (fullpath))
				content = sr.ReadToEnd ();

			foreach (Match match in PkgCheckModulesRegex.Matches (content)) {
				if (!match.Success)
					continue;

				List<string> pkgs = new List<string> ();
				string pkgId = match.Groups ["pkgId"].Value;
				foreach (Capture c in match.Groups ["content"].Captures) {
					string s = c.Value.Trim ();
					if (s.Length == 0)
						continue;
					pkgs.Add (s);
					pkgNameToPkgVarName [s] = pkgId;
				}

				pkgVarNameToPkgName [pkgId] = pkgs;
			}
			
			foreach (Match match in AcSubstRegex.Matches (content)) {
				if (!match.Success)
					continue;

				string s = match.Groups [1].Value;
				if (!s.EndsWith ("_LIBS"))
					continue;

				string pkgVarName = s.Replace ("_LIBS", "");
				List<string> l = GetNamesFromVarName (pkgVarName);

				if (l != null && l.Count == 1 &&
					Runtime.SystemAssemblyService.GetPackage (l [0]) != null) {
					//PKG_CHECK_MODULES for pkgVarName was found
					//and it references only a single managed package
					//
					//This ensures that we don't emit $(FOO_LIBS)
					//for pkgVarName's that reference > 1 package or
					//for unmanaged packages
					varNameAcSubst [s] = s;
				}
			}
		}

		static Regex pkgCheckModulesRegex = null;
		static Regex PkgCheckModulesRegex {
			get {
				if (pkgCheckModulesRegex == null)
					pkgCheckModulesRegex = new Regex (
						@".*PKG_CHECK_MODULES\(\s*(?<pkgId>[^,\) \t]*)\s*,(\s*(?<content>[^,\) \t]*)\s*[><=]*\s*[^,\) \t]*)*");
				return pkgCheckModulesRegex;
			}
		}

		static Regex acsubstRegex = null;
		static Regex AcSubstRegex {
			get {
				if (acsubstRegex == null)
					acsubstRegex = new Regex (@"AC_SUBST\(\s*([^\)\s]+)\s*\)");
				return acsubstRegex;
			}
		}
	}

}
