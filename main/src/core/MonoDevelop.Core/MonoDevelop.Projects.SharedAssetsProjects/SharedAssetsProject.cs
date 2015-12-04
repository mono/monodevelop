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

namespace MonoDevelop.Projects.SharedAssetsProjects
{
	public class SharedAssetsProject: Project, IDotNetFileContainer
	{
		Solution currentSolution;
		IDotNetLanguageBinding languageBinding;
		string languageName;
		FilePath projItemsPath;

		public SharedAssetsProject ()
		{
		}

		public SharedAssetsProject (string language)
		{
			languageName = language;
		}

		public SharedAssetsProject (ProjectCreateInformation projectCreateInfo, XmlElement projectOptions)
		{
			languageName = projectOptions.GetAttribute ("language");
			DefaultNamespace = projectCreateInfo.ProjectName;
		}

		internal protected override List<FilePath> OnGetItemFiles (bool includeReferencedFiles)
		{
			var list = base.OnGetItemFiles (includeReferencedFiles);
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

		public override IEnumerable<string> GetProjectTypes ()
		{
			yield return "SharedAssets";
			yield return "DotNet";
		}

		public override string[] SupportedLanguages {
			get {
				return new [] { "", languageName };
			}
		}

		public IDotNetLanguageBinding LanguageBinding {
			get {
				if (languageBinding == null)
					languageBinding = LanguageBindingService.GetBindingPerLanguageName (languageName) as IDotNetLanguageBinding;
				return languageBinding;
			}
		}

		public override bool IsCompileable (string fileName)
		{
			return LanguageBinding.IsSourceCodeFile (fileName);
		}

		protected override BuildResult OnBuild (MonoDevelop.Core.IProgressMonitor monitor, ConfigurationSelector configuration)
		{
			return new BuildResult ();
		}

		internal protected override bool OnGetSupportsTarget (string target)
		{
			return false;
		}

		internal protected override bool OnGetSupportsExecute ()
		{
			return false;
		}

		public override bool FastCheckNeedsBuild (ConfigurationSelector configuration)
		{
			return false;
		}

		protected override IEnumerable<string> GetStandardBuildActions ()
		{
			return BuildAction.DotNetActions;
		}

		protected override IList<string> GetCommonBuildActions ()
		{
			return BuildAction.DotNetCommonActions;
		}

		/// <summary>
		/// Gets the default namespace for the file, according to the naming policy.
		/// </summary>
		/// <remarks>Always returns a valid namespace, even if the fileName is null.</remarks>
		public string GetDefaultNamespace (string fileName)
		{
			return DotNetProject.GetDefaultNamespace (this, DefaultNamespace, fileName);
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
			foreach (var p in ParentSolution.GetAllSolutionItems<DotNetProject> ())
				ProcessProject (p);
		}

		void HandleSolutionItemAdded (object sender, SolutionItemChangeEventArgs e)
		{
			var p = e.SolutionItem as DotNetProject;
			if (p != null)
				// Maybe the new project already contains a reference to this shared project
				ProcessProject (p);
		}

		public override void Dispose ()
		{
			base.Dispose ();
			DisconnectFromSolution ();
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
				p.References.Add (new ProjectReference (this));
			}

			foreach (var pref in p.References.Where (r => r.ReferenceType == ReferenceType.Project && r.Reference == Name))
				ProcessNewReference (pref);
		}

		void ProcessNewReference (ProjectReference pref)
		{
			pref.Flags = ProjectItemFlags.DontPersist;
			pref.SetItemsProjectPath (ProjItemsPath);
			foreach (var f in Files) {
				if (pref.OwnerProject.Files.GetFile (f.FilePath) == null && f.Subtype != Subtype.Directory) {
					var cf = (ProjectFile)f.Clone ();
					cf.Flags |= ProjectItemFlags.DontPersist | ProjectItemFlags.Hidden;
					pref.OwnerProject.Files.Add (cf);
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
						p.Files.Add (pf);
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
					p.Files.Add (pf);
				}
			}
		}

		IEnumerable<DotNetProject> GetReferencingProjects ()
		{
			if (ParentSolution == null)
				return new DotNetProject[0];

			return ParentSolution.GetAllSolutionItems<DotNetProject> ().Where (p => p.References.Any (r => r.GetItemsProjectPath () != null));
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

