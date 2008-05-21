
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
using MonoDevelop.Projects.Serialization;

namespace MonoDevelop.Projects
{
	public enum Subtype {
		Code,
		Directory
	}
	
	public enum BuildAction {
		[Description ("Nothing")] Nothing,
		[Description ("Compile")] Compile,
		[Description ("Embed as resource")] EmbedAsResource,
		[Description ("Deploy")] FileCopy,
		[Description ("Exclude")] Exclude		
	}
	
	/// <summary>
	/// This class represent a file information in an IProject object.
	/// </summary>
	public class ProjectFile : ICloneable, IExtendedDataItem
	{
		Hashtable extendedProperties;
		
		[ProjectPathItemProperty("name")]
		string filename;
		
		[ItemProperty("subtype")]	
		Subtype subtype;
		
		[ItemProperty("buildaction")]
		BuildAction buildaction;
		
		string dependsOn;
		
		[ItemProperty("data", DefaultValue="")]
		string data;

		[ItemProperty("resource_id", DefaultValue="")]
		string resourceId = String.Empty;
		
		Project project;
		
		public ProjectFile()
		{
		}
		
		public ProjectFile(string filename)
		{
			this.filename = FileService.GetFullPath (filename);
			subtype       = Subtype.Code;
			buildaction   = BuildAction.Compile;
		}
		
		public ProjectFile(string filename, BuildAction buildAction)
		{
			this.filename = FileService.GetFullPath (filename);
			subtype       = Subtype.Code;
			buildaction   = buildAction;
		}
		
		internal void SetProject (Project prj)
		{
			project = prj;
		}
		
		public IDictionary ExtendedProperties {
			get {
				if (extendedProperties == null)
					extendedProperties = new Hashtable ();
				return extendedProperties;
			}
		}
						
		[ReadOnly(true)]
		public string Name {
			get {
				return filename;
			}
			set {
				Debug.Assert (value != null && value.Length > 0, "name == null || name.Length == 0");
				string oldName = filename;
				filename = FileService.GetFullPath (value);
				if (project != null)
					project.NotifyFileRenamedInProject (new ProjectFileRenamedEventArgs (project, this, oldName));
			}
		}
		
		public string FilePath {
			get {
				return filename;
			}
		}
		
		public string RelativePath {
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
		
		[Browsable(false)]
		public Subtype Subtype {
			get {
				return subtype;
			}
			set {
				subtype = value;
				if (project != null)
					project.NotifyFilePropertyChangedInProject (this);
			}
		}
		
		public BuildAction BuildAction {
			get {
				return buildaction;
			}
			set {
				buildaction = value;
				if (project != null)
					project.NotifyFilePropertyChangedInProject (this);
			}
		}
		
		[Browsable(false)]
		public string DependsOn {
			get {
				return dependsOn;
			}
			set {
				dependsOn = value;
				if (project != null)
					project.NotifyFilePropertyChangedInProject (this);
			}
		}
		
		[Browsable(false)]
		public string Data {
			get {
				return data;
			}
			set {
				data = value;
				if (project != null)
					project.NotifyFilePropertyChangedInProject (this);
			}
		}

		public string ResourceId {
			get {
				if ((resourceId == null || resourceId.Length == 0) && project != null)
					return Services.ProjectService.GetDefaultResourceId (this);

				return resourceId;
			}
			set {
				resourceId = value;
				if (project != null)
					project.NotifyFilePropertyChangedInProject (this);
			}
		}

		public bool IsExternalToProject {
			get {
				return project != null && !Path.GetFullPath (Name).StartsWith (project.BaseDirectory);
			}
		}
		
		public object Clone()
		{
			ProjectFile pf = (ProjectFile) MemberwiseClone();
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
	}
}
