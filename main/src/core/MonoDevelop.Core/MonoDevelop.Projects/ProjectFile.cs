
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

		public ProjectFile (string filename)
		{
			this.filename = FileService.GetFullPath (filename);
			subtype = Subtype.Code;
			buildaction = MonoDevelop.Projects.BuildAction.Compile;
		}

		public ProjectFile (string filename, string buildAction)
		{
			this.filename = FileService.GetFullPath (filename);
			subtype = Subtype.Code;
			buildaction = buildAction;
		}

		[ItemProperty("subtype")]
		private Subtype subtype;
		public Subtype Subtype {
			get { return subtype; }
			set {
				subtype = value;
				OnChanged ();
			}
		}

		[ItemProperty("data", DefaultValue = "")]
		private string data = "";
		public string Data {
			get { return data; }
			set {
				data = value;
				OnChanged ();
			}
		}


		public string Name {
			get { return filename; }

			set {
				Debug.Assert (!String.IsNullOrEmpty (value));

				FilePath oldFileName = filename;
				filename = FileService.GetFullPath (value);

				if (HasChildren) {
					foreach (ProjectFile projectFile in DependentChildren)
						projectFile.dependsOn = Path.GetFileName (FilePath);
				}

				if (project != null)
					project.NotifyFileRenamedInProject (new ProjectFileRenamedEventArgs (project, this, oldFileName));
			}
		}


		[ItemProperty("buildaction")]
		string buildaction = MonoDevelop.Projects.BuildAction.None;
		public string BuildAction {
			get { return buildaction; }
			set {
				buildaction = string.IsNullOrEmpty (value) ? MonoDevelop.Projects.BuildAction.None : value;
				OnChanged ();
			}
		}

		[ItemProperty("resource_id", DefaultValue = "")]
		string resourceId = String.Empty;

		internal string GetResourceId (IResourceHandler resourceHandler)
		{
			if (string.IsNullOrEmpty (resourceId))
				return resourceHandler.GetDefaultResourceId (this);
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
				if (project != null) {
					var rel = project.GetRelativeChildPath (FilePath);
					if (!rel.ToString ().StartsWith (".."))
						return rel;
				}
				return FilePath.FileName;
			}
		}

		Project project;
		public Project Project {
			get { return project; }
		}

		[ItemProperty("SubType")]
		string contentType = String.Empty;
		public string ContentType {
			get { return contentType; }
			set {
				contentType = value;
				OnChanged ();
			}
		}

		[ItemProperty("Visible", DefaultValue = true)]
		bool visible = true;
		
		/// <summary>
		/// Whether the file should be shown to the user.
		/// </summary>
		public bool Visible {
			get { return visible; }
			set {
				if (visible != value) {
					visible = value;
					OnChanged ();
				}
			}
		}

		[ItemProperty("Generator", DefaultValue = "")]
		string generator;
		
		/// <summary>
		/// The ID of a custom code generator.
		/// </summary>
		public string Generator {
			get { return generator; }
			set {
				if (generator != value) {
					generator = value;
					OnChanged ();
				}
			}
		}
		
		[ItemProperty("CustomToolNamespace", DefaultValue = "")]
		string customToolNamespace;
		
		/// <summary>
		/// Overrides the namespace in which the custom code generator should generate code.
		/// </summary>
		public string CustomToolNamespace {
			get { return customToolNamespace; }
			set {
				if (customToolNamespace != value) {
					customToolNamespace = value;
					OnChanged ();
				}
			}
		}
		
		
		[ItemProperty("LastGenOutput", DefaultValue = "")]
		string lastGenOutput;
		
		/// <summary>
		/// The file most recently generated by the custom tool. Relative to this file's parent directory.
		/// </summary>
		public string LastGenOutput {
			get { return lastGenOutput; }
			set {
				if (lastGenOutput != value) {
					lastGenOutput = value;
					OnChanged ();
				}
			}
		}
		
		
		[RelativeProjectPathItemProperty("Link", DefaultValue = "")]
		string link;
		
		/// <summary>
		/// If the file's real path is outside the project root, this value can be used to set its virtual path
		/// within the project root. Use ProjectVirtualPath to read the effective virtual path for any file.
		/// </summary>
		public FilePath Link {
			get { return link; }
			set {
				if (link != value) {
					if (value.IsAbsolute || value.ToString ().StartsWith (".."))
						throw new ArgumentException ("value");
					link = value;
					OnChanged ();
				}
			}
		}
		
		/// <summary>
		/// Whether the file is a link.
		/// </summary>
		public bool IsLink {
			get {
				return !Link.IsNullOrEmpty || (project != null && !FilePath.IsChildPathOf (project.BaseDirectory));
			}
		}
		
		/// <summary>
		/// Whether the file is outside the project base directory.
		/// </summary>
		public bool IsExternalToProject {
			get {
				return !FilePath.IsChildPathOf (project.BaseDirectory);
			}
		}

		[ItemProperty("copyToOutputDirectory", DefaultValue = FileCopyMode.None)]
		FileCopyMode copyToOutputDirectory;
		public FileCopyMode CopyToOutputDirectory {
			get { return copyToOutputDirectory; }
			set {
				if (copyToOutputDirectory != value) {
					copyToOutputDirectory = value;
					OnChanged ();
				}
			}
		}

		#region File grouping
		string dependsOn;
		public string DependsOn {
			get { return dependsOn; }

			set {
				dependsOn = value;

				if (dependsOnFile != null) {
					dependsOnFile.dependentChildren.Remove (this);
					dependsOnFile = null;
				}

				if (project != null && value != null) {
					project.ResolveDependencies (this);
				}

				OnChanged ();
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

		internal bool ResolveParent ()
		{
			if (dependsOnFile == null && (!string.IsNullOrEmpty (dependsOn) && project != null)) {
				//NOTE also that the dependent files are always assumed to be in the same directory
				//This matches VS behaviour
				string parentPath = Path.Combine (Path.GetDirectoryName (FilePath), Path.GetFileName (DependsOn));

				//don't allow cyclic references
				if (parentPath == FilePath) {
					MonoDevelop.Core.LoggingService.LogWarning ("Cyclic dependency in project '{0}': file '{1}' depends on '{2}'", project == null ? "(none)" : project.Name, FilePath, parentPath);
					return true;
				}

				dependsOnFile = project.Files.GetFile (parentPath);
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

		public string ResourceId {
			get {
				if (BuildAction != MonoDevelop.Projects.BuildAction.EmbeddedResource)
					return string.Empty;
				if (string.IsNullOrEmpty (resourceId) && project is DotNetProject)
					return ((DotNetProject)project).ResourceHandler.GetDefaultResourceId (this);
				return resourceId;
			}
			set {
				resourceId = value;
				OnChanged ();
			}
		}

		internal void SetProject (Project project)
		{
			this.project = project;
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
			pf.project = null;
			return pf;
		}

		public virtual void Dispose ()
		{
		}

		protected virtual void OnChanged ()
		{
			if (project != null)
				project.NotifyFilePropertyChangedInProject (this);
		}
	}
}
