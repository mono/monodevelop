
//  ProjectFile.cs
//
// Author:
//   Lluis Sanchez Gual <lluis@novell.com>
//   Viktoria Dudka  <viktoriad@remobjects.com>
// 
// Copyright (c) 2009 Novell, Inc (http://www.novell.com)
// Copyright (c) 2009 RemObjects Software
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
//
//

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using MonoDevelop;
using MonoDevelop.Core;
using MonoDevelop.Core.Serialization;
using MonoDevelop.Projects;
using MonoDevelop.Projects.Extensions;
using MonoDevelop.Projects.MSBuild;
using MonoDevelop.Projects.Policies;

namespace MonoDevelop.Projects
{
	public enum Subtype
	{
		Code,
		Directory
	}

	/// <summary>
	/// This class represent a file information in an IProject object.
	/// </summary>
	public class ProjectFile : ProjectItem, ICloneable, IFileItem, IDisposable
	{
		public ProjectFile ()
		{
		}

		public ProjectFile (string filename): this (filename, MonoDevelop.Projects.BuildAction.Compile)
		{
		}

		public ProjectFile (string filename, string buildAction)
		{
			this.filename = FileService.GetFullPath (filename);
			subtype = Subtype.Code;
			BuildAction = buildAction;
		}

		public override string Include {
			get {
				if (Project != null) {
					string path = MSBuildProjectService.ToMSBuildPath (Project.ItemDirectory, FilePath);
					if (path.Length > 0) {
						//directory paths must end with '/'
						if ((Subtype == Subtype.Directory) && path [path.Length - 1] != '\\')
							path = path + "\\";
						return path;
					}
				}
				return base.Include;
			}
			protected set {
				base.Include = value;
			}
		}

		internal protected override void Read (Project project, IMSBuildItemEvaluated buildItem)
		{
			base.Read (project, buildItem);

			if (buildItem.Name == "Folder") {
				// Read folders
				string path = MSBuildProjectService.FromMSBuildPath (project.ItemDirectory, buildItem.Include);
				Name = Path.GetDirectoryName (path);
				Subtype = Subtype.Directory;
				return;
			}

			Name = MSBuildProjectService.FromMSBuildPath (project.ItemDirectory, buildItem.Include);
			BuildAction = buildItem.Name;

			DependsOn = buildItem.Metadata.GetPathValue ("DependentUpon", relativeToPath:FilePath.ParentDirectory);

			string copy = buildItem.Metadata.GetValue ("CopyToOutputDirectory");
			if (!string.IsNullOrEmpty (copy)) {
				switch (copy) {
				case "None": break;
				case "Always": CopyToOutputDirectory = FileCopyMode.Always; break;
				case "PreserveNewest": CopyToOutputDirectory = FileCopyMode.PreserveNewest; break;
				default:
					LoggingService.LogWarning (
						"Unrecognised value {0} for CopyToOutputDirectory MSBuild property",
						copy);
					break;
				}
			}

			Visible = buildItem.Metadata.GetValue ("Visible", true);
			resourceId = buildItem.Metadata.GetValue ("LogicalName");
			contentType = buildItem.Metadata.GetValue ("SubType");
			generator = buildItem.Metadata.GetValue ("Generator");
			customToolNamespace = buildItem.Metadata.GetValue ("CustomToolNamespace");
			lastGenOutput = buildItem.Metadata.GetValue ("LastGenOutput");
			Link = buildItem.Metadata.GetPathValue ("Link", relativeToProject:false);
		}

		internal protected override void Write (Project project, MSBuildItem buildItem)
		{
			base.Write (project, buildItem);

			buildItem.Metadata.SetValue ("DependentUpon", DependsOn, FilePath.Empty, relativeToPath:FilePath.ParentDirectory);
			buildItem.Metadata.SetValue ("SubType", ContentType, "");
			buildItem.Metadata.SetValue ("Generator", Generator, "");
			buildItem.Metadata.SetValue ("CustomToolNamespace", CustomToolNamespace, "");
			buildItem.Metadata.SetValue ("LastGenOutput", LastGenOutput, "");
			buildItem.Metadata.SetValue ("Link", Link, FilePath.Empty, relativeToProject:false);
			buildItem.Metadata.SetValue ("CopyToOutputDirectory", CopyToOutputDirectory.ToString (), "None");
			buildItem.Metadata.SetValue ("Visible", Visible, true);

			var resId = ResourceId;

			// For EmbeddedResource, emit LogicalName only when it does not match the default msbuild resource Id
			if (project is DotNetProject && BuildAction == MonoDevelop.Projects.BuildAction.EmbeddedResource && ((DotNetProject)project).GetDefaultMSBuildResourceId (this) == resId)
				resId = "";

			buildItem.Metadata.SetValue ("LogicalName", resId, "");
		}


		Subtype subtype;
		public Subtype Subtype {
			get { return subtype; }
			set {
				subtype = value;
				if (subtype == Subtype.Directory)
					ItemName = "Folder";
				OnChanged ("Subtype");
			}
		}

		public string Name {
			get { return filename; }

			set {
				Debug.Assert (!String.IsNullOrEmpty (value));

				FilePath oldVirtualPath = ProjectVirtualPath;
				FilePath oldPath = filename;

				filename = FileService.GetFullPath (value);

				if (HasChildren) {
					foreach (ProjectFile projectFile in DependentChildren)
						projectFile.dependsOn = Path.GetFileName (FilePath);
				}

				// If the file is a link, rename the link too
				if (IsLink && Link.FileName == oldPath.FileName)
					link = Path.Combine (Path.GetDirectoryName (link), filename.FileName);

				// If a file that belongs to a project is being renamed, update the value of UnevaluatedInclude
				// since that is used when saving
				if (Project != null)
					UnevaluatedInclude = Include;

				OnPathChanged (oldPath, filename, oldVirtualPath, ProjectVirtualPath);

				if (Project != null)
					Project.NotifyFileRenamedInProject (new ProjectFileRenamedEventArgs (Project, this, oldPath));
			}
		}

		public string BuildAction {
			get { return ItemName; }
			set {
				ItemName = string.IsNullOrEmpty (value) ? MonoDevelop.Projects.BuildAction.None : value;
				OnChanged ("BuildAction");
			}
		}

		string resourceId = String.Empty;

		/// <summary>
		/// Gets the resource id of this file for the provided policy
		/// </summary>
		internal string GetResourceId (ResourceNamePolicy policy)
		{
			if (string.IsNullOrEmpty (resourceId) && (Project is DotNetProject))
				return ((DotNetProject)Project).GetDefaultResourceIdForPolicy (this, policy);
			return resourceId;
		}

		FilePath filename;
		public FilePath FilePath {
			get { return filename; }
		}

		FilePath IFileItem.FileName {
			get { return FilePath; }
		}

		/// <summary>
		/// The file should be treated as effectively having this relative path within the project. If the file is
		/// a link or outside the project root, this will not be the same as the physical file.
		/// </summary>
		public FilePath ProjectVirtualPath {
			get {
				if (!Link.IsNullOrEmpty)
					return Link;
				if (Project != null) {
					var rel = Project.GetRelativeChildPath (FilePath);
					if (!rel.ToString ().StartsWith ("..", StringComparison.Ordinal))
						return rel;
				}
				return FilePath.FileName;
			}
		}


		string contentType;
		public string ContentType {
			get { return contentType ?? ""; }
			set {
				contentType = value;
				OnChanged ("ContentType");
			}
		}

		bool visible = true;
		
		/// <summary>
		/// Whether the file should be shown to the user.
		/// </summary>
		public bool Visible {
			get { return visible; }
			set {
				if (visible != value) {
					visible = value;
					OnChanged ("Visible");
				}
			}
		}

		string generator;
		
		/// <summary>
		/// The ID of a custom code generator.
		/// </summary>
		public string Generator {
			get { return generator ?? ""; }
			set {
				if (generator != value) {
					generator = value;
					OnChanged ("Generator");
				}
			}
		}
		
		string customToolNamespace;
		
		/// <summary>
		/// Overrides the namespace in which the custom code generator should generate code.
		/// </summary>
		public string CustomToolNamespace {
			get { return customToolNamespace ?? ""; }
			set {
				if (customToolNamespace != value) {
					customToolNamespace = value;
					OnChanged ("CustomToolNamespace");
				}
			}
		}
		
		
		string lastGenOutput;
		
		/// <summary>
		/// The file most recently generated by the custom tool. Relative to this file's parent directory.
		/// </summary>
		public string LastGenOutput {
			get { return lastGenOutput ?? ""; }
			set {
				if (lastGenOutput != value) {
					lastGenOutput = value;
					OnChanged ("LastGenOutput");
				}
			}
		}
		
		string link;
		
		/// <summary>
		/// If the file's real path is outside the project root, this value can be used to set its virtual path
		/// within the project root. Use ProjectVirtualPath to read the effective virtual path for any file.
		/// </summary>
		public FilePath Link {
			get { return link ?? ""; }
			set {
				if (link != value) {
					if (value.IsAbsolute || value.ToString ().StartsWith ("..", StringComparison.Ordinal))
						throw new ArgumentException ("Invalid value for Link property");

					var oldLink = link;
					link = value;

					OnVirtualPathChanged (oldLink, link);
					OnChanged ("Link");
				}
			}
		}
		
		/// <summary>
		/// Whether the file is a link.
		/// </summary>
		public bool IsLink {
			get {
				return !Link.IsNullOrEmpty || (Project != null && !FilePath.IsChildPathOf (Project.BaseDirectory));
			}
		}
		
		/// <summary>
		/// Whether the file is outside the project base directory.
		/// </summary>
		public bool IsExternalToProject {
			get {
				return !FilePath.IsChildPathOf (Project.BaseDirectory);
			}
		}

		FileCopyMode copyToOutputDirectory = FileCopyMode.None;
		public FileCopyMode CopyToOutputDirectory {
			get { return copyToOutputDirectory; }
			set {
				if (copyToOutputDirectory != value) {
					copyToOutputDirectory = value;
					OnChanged ("CopyToOutputDirectory");
				}
			}
		}

		#region File grouping
		string dependsOn;
		public string DependsOn {
			get { return dependsOn ?? ""; }

			set {
				if (dependsOn != value) {
					var oldPath = !string.IsNullOrEmpty (dependsOn)
						? FilePath.ParentDirectory.Combine (Path.GetFileName (dependsOn))
						: FilePath.Empty;
					dependsOn = value;
	
					if (dependsOnFile != null) {
						dependsOnFile.dependentChildren.Remove (this);
						dependsOnFile = null;
					}
	
					if (Project != null && value != null)
						Project.UpdateDependency (this, oldPath);
	
					OnChanged ("DependsOn");
				}
			}
		}

		ProjectFile dependsOnFile;
		public ProjectFile DependsOnFile {
			get { return dependsOnFile; }
			internal set { dependsOnFile = value; }
		}

		List<ProjectFile> dependentChildren;
		public bool HasChildren {
			get { return dependentChildren != null && dependentChildren.Count > 0; }
		}

		public IList<ProjectFile> DependentChildren {
			get { return ((IList<ProjectFile>)dependentChildren) ?? new ProjectFile[0]; }
		}

		internal FilePath DependencyPath {
			get {
				return FilePath.ParentDirectory.Combine (Path.GetFileName (DependsOn));
			}
		}

		internal bool ResolveParent (ProjectFile potentialParentFile)
		{
			if (potentialParentFile.FilePath == DependencyPath) {
				dependsOnFile = potentialParentFile;
				if (dependsOnFile.dependentChildren == null)
					dependsOnFile.dependentChildren = new List<ProjectFile> ();
				dependsOnFile.dependentChildren.Add (this);
				return true;
			}
			return false;
		}

		internal bool ResolveParent ()
		{
			if (dependsOnFile == null && (!string.IsNullOrEmpty (dependsOn) && Project != null)) {
				//NOTE also that the dependent files are always assumed to be in the same directory
				//This matches VS behaviour
				var parentPath = DependencyPath;

				//don't allow cyclic references
				if (parentPath == FilePath) {
					LoggingService.LogWarning (
						"Cyclic dependency in project '{0}': file '{1}' depends on '{2}'",
						Project == null ? "(none)" : Project.Name, FilePath, parentPath
					);
					return true;
				}

				dependsOnFile = Project.Files.GetFile (parentPath);
				if (dependsOnFile != null) {
					if (dependsOnFile.dependentChildren == null)
						dependsOnFile.dependentChildren = new List<ProjectFile> ();
					dependsOnFile.dependentChildren.Add (this);
					return true;
				} else {
					return false;
				}
			} else {
				return true;
			}
		}
		#endregion

		// FIXME: rename this to LogicalName for a better mapping to the MSBuild property
		public string ResourceId {
			get {
				// If the resource id is not set, return the project's default
				if (BuildAction == MonoDevelop.Projects.BuildAction.EmbeddedResource && string.IsNullOrEmpty (resourceId) && Project is DotNetProject)
					return ((DotNetProject)Project).GetDefaultResourceId (this);

				return resourceId;
			}
			set {
				if (resourceId != value) {
					var oldVal = ResourceId;
					resourceId = value;
					if (ResourceId != oldVal)
						OnChanged ("ResourceId");
				}
			}
		}

		protected override void OnProjectSet ()
		{
			base.OnProjectSet ();
			if (Project != null) {
				base.Include = Include;
				OnVirtualPathChanged (FilePath.Null, ProjectVirtualPath);
			}
		}

		public override string ToString ()
		{
			return "[ProjectFile: FileName=" + filename + "]";
		}

		public object Clone ()
		{
			ProjectFile pf = (ProjectFile)MemberwiseClone ();
			pf.dependsOnFile = null;
			pf.dependentChildren = null;
			pf.Project = null;
			pf.VirtualPathChanged = null;
			pf.PathChanged = null;
			pf.BackingItem = null;
			pf.BackingEvalItem = null;
			return pf;
		}

		public virtual void Dispose ()
		{
		}

		internal event EventHandler<ProjectFileVirtualPathChangedEventArgs> VirtualPathChanged;

		void OnVirtualPathChanged (FilePath oldVirtualPath, FilePath newVirtualPath)
		{
			var handler = VirtualPathChanged;

			if (handler != null)
				handler (this, new ProjectFileVirtualPathChangedEventArgs (this, oldVirtualPath, newVirtualPath));
		}

		internal event EventHandler<ProjectFilePathChangedEventArgs> PathChanged;

		void OnPathChanged (FilePath oldPath, FilePath newPath, FilePath oldVirtualPath, FilePath newVirtualPath)
		{
			var handler = PathChanged;

			if (handler != null)
				handler (this, new ProjectFilePathChangedEventArgs (this, oldPath, newPath, oldVirtualPath, newVirtualPath));
		}

		protected virtual void OnChanged (string property)
		{
			if (Project != null)
				Project.NotifyFilePropertyChangedInProject (this, property);
		}
	}

	class ProjectFileVirtualPathChangedEventArgs : EventArgs
	{
		public ProjectFileVirtualPathChangedEventArgs (ProjectFile projectFile, FilePath oldPath, FilePath newPath)
		{
			ProjectFile = projectFile;
			OldVirtualPath = oldPath;
			NewVirtualPath = newPath;
		}

		public ProjectFile ProjectFile { get; private set; }
		public FilePath OldVirtualPath { get; private set; }
		public FilePath NewVirtualPath { get; private set; }
	}

	class ProjectFilePathChangedEventArgs : ProjectFileVirtualPathChangedEventArgs
	{
		public ProjectFilePathChangedEventArgs (ProjectFile projectFile, FilePath oldPath, FilePath newPath, FilePath oldVirtualPath, FilePath newVirtualPath) : base (projectFile, oldVirtualPath, newVirtualPath)
		{
			OldPath = oldPath;
			NewPath = newPath;
		}

		public FilePath OldPath { get; private set; }
		public FilePath NewPath { get; private set; }
	}
}
