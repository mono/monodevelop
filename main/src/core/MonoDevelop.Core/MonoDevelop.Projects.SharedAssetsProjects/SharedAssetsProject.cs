//
// SharedProject.cs
//
// Author:
//       Lluis Sanchez <lluis@xamarin.com>
//
// Copyright (c) 2014 Xamarin Inc
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

using System;
using System.Linq;
using System.Collections.Generic;
using MonoDevelop.Core;
using System.IO;
using System.Xml;
using MonoDevelop.Projects.Policies;
using System.Threading.Tasks;
using MonoDevelop.Projects.MSBuild;

namespace MonoDevelop.Projects.SharedAssetsProjects
{
	[ExportProjectType ("{D954291E-2A0B-460D-934E-DC6B0785DB48}", Extension="shproj", Alias="SharedAssetsProject")]
	public sealed class SharedAssetsProject: Project, IDotNetFileContainer
	{
		Solution currentSolution;
		LanguageBinding languageBinding;
		string languageName;
		FilePath projItemsPath;
		MSBuildProject projitemsProject;
		HashSet<MSBuildItem> usedMSBuildItems = new HashSet<MSBuildItem> ();

		const string CSharptargets = @"$(MSBuildExtensionsPath32)\Microsoft\VisualStudio\v$(VisualStudioVersion)\CodeSharing\Microsoft.CodeSharing.CSharp.targets";
		const string FSharptargets = @"$(MSBuildExtensionsPath32)\Microsoft\VisualStudio\v$(VisualStudioVersion)\CodeSharing\Microsoft.CodeSharing.FSharp.targets";

		public SharedAssetsProject ()
		{
			Initialize (this);
		}

		public SharedAssetsProject (string language): this ()
		{
			languageName = language;
		}

		protected override void OnNameChanged (SolutionItemRenamedEventArgs e)
		{
			if (!projItemsPath.IsNullOrEmpty && Path.GetFileNameWithoutExtension (e.OldName) == projItemsPath.FileNameWithoutExtension) {
				// We are going to rename the projitems file, but before that, let's get all referencing projects, since they reference
				// using the old path
				string oldPath = ProjItemsPath;
				var refProjects = GetReferencingProjects ().ToArray ();

				// Set the new projitems file name
				projItemsPath = projItemsPath.ParentDirectory.Combine (e.NewName) + projItemsPath.Extension;

				// Update all referencing projects
				foreach (var p in refProjects) {
					var sr = p.References.FirstOrDefault (r => r.GetItemsProjectPath () == oldPath);
					if (sr != null)
						sr.SetItemsProjectPath (ProjItemsPath);
				}
			}
			base.OnNameChanged (e);
		}

		protected override void OnInitializeFromTemplate (ProjectCreateInformation projectCreateInfo, XmlElement projectOptions)
		{
			// Get the language before calling OnInitializeFromTemplate so the language binding
			// is available when adding new files to the project if the project is added to
			// an existing solution.
			languageName = projectOptions.GetAttribute ("language");

			base.OnInitializeFromTemplate (projectCreateInfo, projectOptions);

			string templateDefaultNamespace = GetDefaultNamespace (projectCreateInfo, projectOptions);
			DefaultNamespace = templateDefaultNamespace ?? projectCreateInfo.ProjectName;
		}

		static string GetDefaultNamespace (ProjectCreateInformation projectCreateInfo, XmlElement projectOptions)
		{
			string defaultNamespace = projectOptions.Attributes["DefaultNamespace"]?.Value;
			if (defaultNamespace != null)
				return StringParserService.Parse (defaultNamespace, projectCreateInfo.Parameters);

			return null;
		}

		protected override void OnReadProject (ProgressMonitor monitor, MSBuildProject msproject)
		{
			base.OnReadProject (monitor, msproject);

			var import = msproject.Imports.FirstOrDefault (im => im.Label == "Shared");
			if (import == null)
				return;

			// TODO: load the type from msbuild
			foreach (var item in msproject.Imports) {
				if (item.Project.Equals (CSharptargets, StringComparison.OrdinalIgnoreCase)) {
					LanguageName = "C#";
					break;
				}
				if (item.Project.Equals (FSharptargets, StringComparison.OrdinalIgnoreCase)) {
					LanguageName = "F#";
					break;
				}
			}
			//If for some reason the language name is empty default it to C#
			if (String.IsNullOrEmpty(LanguageName))
				LanguageName = "C#";

			projItemsPath = MSBuildProjectService.FromMSBuildPath (msproject.BaseDirectory, import.Project);

			MSBuildProject p = new MSBuildProject (msproject.EngineManager);
			p.Load (projItemsPath);
			p.Evaluate ();

			var cp = p.PropertyGroups.FirstOrDefault (g => g.Label == "Configuration");
			if (cp != null)
				DefaultNamespace = cp.GetValue ("Import_RootNamespace");

			LoadProjectItems (p, ProjectItemFlags.None, usedMSBuildItems);

			projitemsProject = p;
		}

		internal override void SaveProjectItems (ProgressMonitor monitor, MSBuildProject msproject, HashSet<MSBuildItem> loadedItems, string pathPrefix)
		{
			// Save project items in the .projitems file
			base.SaveProjectItems (monitor, projitemsProject, usedMSBuildItems, "$(MSBuildThisFileDirectory)");
		}

		protected override void OnWriteProject (ProgressMonitor monitor, MonoDevelop.Projects.MSBuild.MSBuildProject msproject)
		{
			if (projItemsPath == FilePath.Null)
				projItemsPath = Path.ChangeExtension (FileName, ".projitems");

			if (projitemsProject == null) {
				projitemsProject = new MSBuildProject (msproject.EngineManager);
				projitemsProject.FileName = projItemsPath;
				var grp = projitemsProject.GetGlobalPropertyGroup ();
				if (grp == null)
					grp = projitemsProject.AddNewPropertyGroup (false);
				grp.SetValue ("MSBuildAllProjects", "$(MSBuildAllProjects);$(MSBuildThisFileFullPath)");
				grp.SetValue ("HasSharedItems", true);
				grp.SetValue ("SharedGUID", ItemId, preserveExistingCase:true);
			}

			IMSBuildPropertySet configGrp = projitemsProject.PropertyGroups.FirstOrDefault (g => g.Label == "Configuration");
			if (configGrp == null) {
				configGrp = projitemsProject.AddNewPropertyGroup (true);
				configGrp.Label = "Configuration";
			}
			configGrp.SetValue ("Import_RootNamespace", DefaultNamespace);

			base.OnWriteProject (monitor, msproject);

			var newProject = FileName == null || projitemsProject.IsNewProject;
			if (newProject) {
				var grp = msproject.GetGlobalPropertyGroup ();
				if (grp == null)
					grp = msproject.AddNewPropertyGroup (false);
				grp.SetValue ("ProjectGuid", ItemId, preserveExistingCase:true);
				var import = msproject.AddNewImport (@"$(MSBuildExtensionsPath)\$(MSBuildToolsVersion)\Microsoft.Common.props");
				import.Condition = @"Exists('$(MSBuildExtensionsPath)\$(MSBuildToolsVersion)\Microsoft.Common.props')";
				msproject.AddNewImport (@"$(MSBuildExtensionsPath32)\Microsoft\VisualStudio\v$(VisualStudioVersion)\CodeSharing\Microsoft.CodeSharing.Common.Default.props");
				msproject.AddNewImport (@"$(MSBuildExtensionsPath32)\Microsoft\VisualStudio\v$(VisualStudioVersion)\CodeSharing\Microsoft.CodeSharing.Common.props");
				import = msproject.AddNewImport (MSBuildProjectService.ToMSBuildPath (FileName.ParentDirectory, projItemsPath));
				import.Label = "Shared";
				if (LanguageName.Equals("C#", StringComparison.OrdinalIgnoreCase)) {
					msproject.AddNewImport (CSharptargets);
				}
				else if (LanguageName.Equals("F#", StringComparison.OrdinalIgnoreCase)) {
					msproject.AddNewImport (FSharptargets);
				}

			} else {
				var itemsImport = msproject.Imports.FirstOrDefault (i => i.Label == "Shared");
				if (itemsImport != null)
					itemsImport.Project = MSBuildProjectService.ToMSBuildPath (FileName.ParentDirectory, projItemsPath);
				else {
					var import = msproject.AddNewImport (MSBuildProjectService.ToMSBuildPath (FileName.ParentDirectory, projItemsPath));
					import.Label = "Shared";
				}
			}

			// having no ToolsVersion is equivalent to 2.0, roundtrip that correctly
			if (ToolsVersion != "2.0")
				msproject.ToolsVersion = ToolsVersion;
			else if (string.IsNullOrEmpty (msproject.ToolsVersion))
				msproject.ToolsVersion = null;
			else
				msproject.ToolsVersion = "2.0";

			projitemsProject.Save (projItemsPath);
		}

		protected override IEnumerable<FilePath> OnGetItemFiles (bool includeReferencedFiles)
		{
			var list = base.OnGetItemFiles (includeReferencedFiles).ToList ();
			if (!string.IsNullOrEmpty (FileName))
				list.Add (ProjItemsPath);
			return list;
		}

		public string LanguageName {
			get { return languageName; }
			set { languageName = value; }
		}

		public string DefaultNamespace { get; set; }

		public FilePath ProjItemsPath {
			get {
				return !projItemsPath.IsNull ? projItemsPath : FileName.ChangeExtension (".projitems");
			}
			set {
				projItemsPath = value;
			}
		}

		protected override void OnGetTypeTags (HashSet<string> types)
		{
			types.Add ("SharedAssets");
			types.Add ("DotNet");
		}

		protected override string[] OnGetSupportedLanguages ()
		{
			return new [] { "", languageName };
		}

		public LanguageBinding LanguageBinding {
			get {
				if (languageBinding == null && languageName != null) 
					languageBinding = LanguageBindingService.GetBindingPerLanguageName (languageName);
				return languageBinding;
			}
		}

		protected override bool OnGetIsCompileable (string fileName)
		{
			return LanguageBinding != null && LanguageBinding.IsSourceCodeFile (fileName);
		}

		protected override Task<BuildResult> OnBuild (MonoDevelop.Core.ProgressMonitor monitor, ConfigurationSelector configuration, OperationContext operationContext)
		{
			return Task.FromResult (BuildResult.CreateSuccess ());
		}

		protected override bool OnGetSupportsTarget (string target)
		{
			return false;
		}

		protected override ProjectFeatures OnGetSupportedFeatures ()
		{
			return ProjectFeatures.None;
		}

		protected override bool OnFastCheckNeedsBuild (ConfigurationSelector configuration)
		{
			return false;
		}

		protected override IEnumerable<string> OnGetStandardBuildActions ()
		{
			return BuildAction.DotNetActions;
		}

		protected override IList<string> OnGetCommonBuildActions ()
		{
			return BuildAction.DotNetCommonActions;
		}

		/// <summary>
		/// Gets the default namespace for the file, according to the naming policy.
		/// </summary>
		/// <remarks>Always returns a valid namespace, even if the fileName is null.</remarks>
		public string GetDefaultNamespace (string fileName, bool useVisualStudioNamingPolicy = false)
		{
			return DotNetProject.GetDefaultNamespace (this, DefaultNamespace, fileName, useVisualStudioNamingPolicy);
		}

		protected override void OnBoundToSolution ()
		{
			if (currentSolution != null)
				DisconnectFromSolution ();

			base.OnBoundToSolution ();

			ParentSolution.ReferenceAddedToProject += HandleReferenceAddedToProject;
			ParentSolution.ReferenceRemovedFromProject += HandleReferenceRemovedFromProject;
			ParentSolution.SolutionItemAdded += HandleSolutionItemAdded;
			currentSolution = ParentSolution;

			// Maybe there is a project that is already referencing this one. It may happen when creating a solution
			// from a template
			foreach (var p in ParentSolution.GetAllItems<DotNetProject> ())
				ProcessProject (p);
		}

		void HandleSolutionItemAdded (object sender, SolutionItemChangeEventArgs e)
		{
			var p = e.SolutionItem as DotNetProject;
			if (p != null)
				// Maybe the new project already contains a reference to this shared project
				ProcessProject (p);

			var folder = e.SolutionItem as SolutionFolder;
			if (folder != null) {
				foreach (var proj in folder.GetAllItems<DotNetProject>()) {
					ProcessProject (proj);
				}
			}
		}

		protected override void OnDispose ()
		{
			if (projitemsProject != null)
				projitemsProject.Dispose ();
			DisconnectFromSolution ();
			base.OnDispose ();
		}

		void DisconnectFromSolution ()
		{
			if (currentSolution != null) {
				currentSolution.ReferenceAddedToProject -= HandleReferenceAddedToProject;
				currentSolution.ReferenceRemovedFromProject -= HandleReferenceRemovedFromProject;
				currentSolution.SolutionItemAdded -= HandleSolutionItemAdded;
				currentSolution = null;
			}
		}

		void HandleReferenceRemovedFromProject (object sender, ProjectReferenceEventArgs e)
		{
			if (e.ProjectReference.ReferenceType == ReferenceType.Project && e.ProjectReference.Reference == Name) {
				foreach (var f in Files) {
					var pf = e.Project.GetProjectFile (f.FilePath);
					if ((pf.Flags & ProjectItemFlags.DontPersist) != 0)
						e.Project.Files.Remove (pf.FilePath);
				}
			}
		}

		void HandleReferenceAddedToProject (object sender, ProjectReferenceEventArgs e)
		{
			if (e.ProjectReference.ReferenceType == ReferenceType.Project && e.ProjectReference.Reference == Name) {
				ProcessNewReference (e.ProjectReference);
			}
		}

		void ProcessProject (DotNetProject p)
		{
			// When the projitems file name doesn't match the shproj file name, the reference we add to the referencing projects
			// uses the projitems name, not the shproj name. Here we detect such case and re-add the references using the correct name
			var referencesToFix = p.References.Where (r => r.GetItemsProjectPath () == ProjItemsPath && r.Reference != Name).ToList ();
			foreach (var r in referencesToFix) {
				p.References.Remove (r);
				p.References.Add (ProjectReference.CreateProjectReference (this));
			}

			foreach (var pref in p.References.Where (r => r.ReferenceType == ReferenceType.Project && r.Reference == Name))
				ProcessNewReference (pref);
		}

		void ProcessNewReference (ProjectReference pref)
		{
			pref.Flags = ProjectItemFlags.DontPersist;
			pref.SetItemsProjectPath (ProjItemsPath);
			foreach (var f in Files.Reverse()) {
				if (pref.OwnerProject.Files.GetFile (f.FilePath) == null && f.Subtype != Subtype.Directory) {
					var cf = (ProjectFile)f.Clone ();
					cf.Flags |= ProjectItemFlags.DontPersist | ProjectItemFlags.Hidden;
					pref.OwnerProject.Files.Insert (0, cf);
				}
			}
		}

		protected override void OnFilePropertyChangedInProject (ProjectFileEventArgs e)
		{
			base.OnFilePropertyChangedInProject (e);
			foreach (var p in GetReferencingProjects ()) {
				foreach (var f in e) {
					if (f.ProjectFile.Subtype == Subtype.Directory)
						continue;
					var pf = (ProjectFile) f.ProjectFile.Clone ();
					pf.Flags |= ProjectItemFlags.DontPersist | ProjectItemFlags.Hidden;
					p.Files.Remove (pf.FilePath);
					p.Files.Add (pf);
				}
			}
		}

		protected override void OnFileAddedToProject (ProjectFileEventArgs e)
		{
			base.OnFileAddedToProject (e);
			foreach (var p in GetReferencingProjects ()) {
				foreach (var f in e) {
					if (f.ProjectFile.Subtype != Subtype.Directory && p.Files.GetFile (f.ProjectFile.FilePath) == null) {
						var pf = (ProjectFile)f.ProjectFile.Clone ();
						pf.Flags |= ProjectItemFlags.DontPersist | ProjectItemFlags.Hidden;
						p.Files.Insert (0, pf);
					}
				}
			}
		}

		protected override void OnFileRemovedFromProject (ProjectFileEventArgs e)
		{
			base.OnFileRemovedFromProject (e);
			foreach (var p in GetReferencingProjects ()) {
				foreach (var f in e) {
					if (f.ProjectFile.Subtype != Subtype.Directory)
						p.Files.Remove (f.ProjectFile.FilePath);
				}
			}
		}

		protected override void OnFileRenamedInProject (ProjectFileRenamedEventArgs e)
		{
			base.OnFileRenamedInProject (e);
			foreach (var p in GetReferencingProjects ()) {
				foreach (var f in e) {
					if (f.ProjectFile.Subtype == Subtype.Directory)
						continue;
					var pf = (ProjectFile) f.ProjectFile.Clone ();
					p.Files.Remove (f.OldName);
					pf.Flags |= ProjectItemFlags.DontPersist | ProjectItemFlags.Hidden;
					p.Files.Insert (0, pf);
				}
			}
		}

		IEnumerable<DotNetProject> GetReferencingProjects ()
		{
			if (ParentSolution == null)
				return new DotNetProject[0];

			return ParentSolution.GetAllItems<DotNetProject> ().Where (p => p.References.Any (r => r.GetItemsProjectPath () == ProjItemsPath));
		}
	}

	internal static class SharedAssetsProjectExtensions
	{
		public static string GetItemsProjectPath (this ProjectReference r)
		{
			return (string) r.ExtendedProperties ["MSBuild.SharedAssetsProject"];
		}

		public static void SetItemsProjectPath (this ProjectReference r, string path)
		{
			r.ExtendedProperties ["MSBuild.SharedAssetsProject"] = path;
		}
	}
}

