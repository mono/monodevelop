
//  ProjectFile.cs
//
//  This file was derived from a file from #Develop. 
//
//  Copyright (C) 2001-2007 Mike Krüger <mkrueger@novell.com>
// 
//  This program is free software; you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation; either version 2 of the License, or
//  (at your option) any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
//  GNU General Public License for more details.
//  
//  You should have received a copy of the GNU General Public License
//  along with this program; if not, write to the Free Software
//  Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA

using System;
using System.IO;
using System.ComponentModel;
using System.Diagnostics;
using System.Xml;
using System.Collections;
using System.Collections.Generic;
using MonoDevelop.Core;
using MonoDevelop.Core.Serialization;
using MonoDevelop.Projects.Extensions;

namespace MonoDevelop.Projects
{
	public enum Subtype {
		Code,
		Directory
	}
	
	/// <summary>
	/// This class represent a file information in an IProject object.
	/// </summary>
	public class ProjectFile : ProjectItem, ICloneable, IFileItem
	{
		FilePath filename;
		
		[ItemProperty("subtype")]	
		Subtype subtype;
		
		[ItemProperty("SubType")]	
		string contentType = String.Empty;
		
		[ItemProperty("buildaction")]
		string buildaction = MonoDevelop.Projects.BuildAction.None;
		
		string dependsOn;
		
		[ItemProperty("data", DefaultValue="")]
		string data;

		[ItemProperty("resource_id", DefaultValue="")]
		string resourceId = String.Empty;
		
		[ItemProperty("copyToOutputDirectory", DefaultValue=FileCopyMode.None)]
		FileCopyMode copyToOutputDirectory;
		
		[ItemProperty("Visible", DefaultValue=true)]
		bool visible = true;
		
		[ItemProperty("Generator", DefaultValue="")]
		string generator;
		
		Project project;
		ProjectFile dependsOnFile;
		List<ProjectFile> dependentChildren;
		
		public ProjectFile()
		{
		}
		
		public ProjectFile(string filename)
		{
			this.filename = FileService.GetFullPath (filename);
			subtype       = Subtype.Code;
			buildaction   = MonoDevelop.Projects.BuildAction.Compile;
		}
		
		public ProjectFile(string filename, string buildAction)
		{
			this.filename = FileService.GetFullPath (filename);
			subtype       = Subtype.Code;
			buildaction   = buildAction;
		}
		
		internal void SetProject (Project prj)
		{
			project = prj;
		}
		
		public string Name {
			get {
				return filename;
			}
			set {
				Debug.Assert (value != null && value.Length > 0, "name == null || name.Length == 0");
				FilePath oldName = filename;
				filename = FileService.GetFullPath (value);
				
				if (HasChildren)
					foreach (ProjectFile child in DependentChildren)
						//go direct to private member to avoid triggering events and invalidating the 
						// collection. It hasn't really changed anyway.
						//NOTE: also that the dependent files are always assumed to be in the same directory
						//This matches VS behaviour
						child.dependsOn = Path.GetFileName (FilePath);
				
				if (project != null)
					project.NotifyFileRenamedInProject (new ProjectFileRenamedEventArgs (project, this, oldName));
			}
		}
		
		public FilePath FilePath {
			get {
				return filename;
			}
		}
		
		FilePath IFileItem.FileName {
			get { return FilePath; }
		}
		
		public FilePath RelativePath {
			get {
				if (project != null)
					return project.GetRelativeChildPath (filename);
				else
					return filename;
			}
		}
		
		public Project Project {
			get { return project; }
		}
		
		public Subtype Subtype {
			get {
				return subtype;
			}
			set {
				subtype = value;
				OnChanged ();
			}
		}

		public string ContentType {
			get {
				return contentType;
			}
			set {
				contentType = value;
				OnChanged ();
			}
		}
			
		
		public string BuildAction {
			get {
				return buildaction;
			}
			set {
				buildaction = string.IsNullOrEmpty (value)? MonoDevelop.Projects.BuildAction.None : value;
				OnChanged ();
			}
		}
		
		public bool Visible {
			get { return visible; }
			set {
				if (visible != value) {
					visible = value;
					if (project != null)
						project.NotifyFilePropertyChangedInProject (this);
				}
			}
		}
		
		public string Generator {
			get { return generator; }
			set {
				if (generator != value) {
					generator = value;
					if (project != null)
						project.NotifyFilePropertyChangedInProject (this);
				}
			}
		}
		
		public FileCopyMode CopyToOutputDirectory {
			get { return copyToOutputDirectory; }
			set {
				if (copyToOutputDirectory != value) {
					copyToOutputDirectory = value;
					if (project != null)
						project.NotifyFilePropertyChangedInProject (this);
				}
			}
		}
		
		
		#region File grouping
		
		public string DependsOn {
			get {
				return dependsOn;
			}
			set {
				dependsOn = value;
				if (dependsOnFile != null) {
					dependsOnFile.dependentChildren.Remove (this);
					dependsOnFile = null;
				}
				if (value != null && project != null)
					project.ResolveDependencies (this);
				
				OnChanged ();
			}
		}
		
		public ProjectFile DependsOnFile {
			get { return dependsOnFile; }
			internal set { dependsOnFile = value; }
		}
		
		public bool HasChildren {
			get { return dependentChildren != null && dependentChildren.Count > 0; }
		}

		public IEnumerable<ProjectFile> DependentChildren {
			get { return ((IEnumerable<ProjectFile>)dependentChildren) ?? new ProjectFile[0]; }
		}
		
		internal bool ResolveParent ()
		{
			if (dependsOnFile == null && (!string.IsNullOrEmpty (dependsOn) && project != null)) {
				//NOTE also that the dependent files are always assumed to be in the same directory
				//This matches VS behaviour
				string parentPath = Path.Combine (Path.GetDirectoryName (FilePath), Path.GetFileName (DependsOn));
				
				//don't allow cyclic references
				if (parentPath == FilePath) {
					MonoDevelop.Core.LoggingService.LogWarning
						("Cyclic dependency in project '{0}': file '{1}' depends on '{2}'",
						 project == null? "(none)" : project.Name, FilePath, parentPath);
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
		
		public string Data {
			get {
				return data;
			}
			set {
				data = value;
				OnChanged ();
			}
		}

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
		
		internal string GetResourceId (IResourceHandler resourceHandler)
		{
			if (string.IsNullOrEmpty (resourceId))
				return resourceHandler.GetDefaultResourceId (this);
			return resourceId;
		}
		
		public bool IsExternalToProject {
			get {
				return project != null && !Path.GetFullPath (Name).StartsWith (project.BaseDirectory);
			}
		}
		
		public object Clone()
		{
			ProjectFile pf = (ProjectFile) MemberwiseClone();
			pf.dependsOnFile = null;
			pf.dependentChildren = null;
			pf.project = null;
			return pf;
		}
		
		public override string ToString()
		{
			return "[ProjectFile: FileName=" + filename + ", Subtype=" + subtype + ", BuildAction=" + BuildAction + "]";
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
