
// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.IO;
using System.ComponentModel;
using System.Diagnostics;
using System.Xml;
using MonoDevelop.Internal.Serialization;
using MonoDevelop.Gui.Components;

namespace MonoDevelop.Internal.Project
{
	public enum Subtype {
		Code,
		Directory,
		WinForm,
		WebForm,
		XmlForm,
		WebService,		
		WebReferences,
		Dataset
	}
	
	public enum BuildAction {
		Nothing,
		Compile,
		EmbedAsResource,
		Exclude		
	}
	
	/// <summary>
	/// This class represent a file information in an IProject object.
	/// </summary>
	public class ProjectFile : LocalizedObject, ICloneable
	{
		[ProjectPathItemProperty("name")]
		string filename;
		
		[ItemProperty("subtype")]	
		Subtype subtype;
		
		[ItemProperty("buildaction")]
		BuildAction buildaction;
		
		[ItemProperty("dependson", DefaultValue="")]		
		string dependsOn;
		
		[ItemProperty("data", DefaultValue="")]
		string data;
		
		Project project;
		
		public ProjectFile()
		{
		}
		
		public ProjectFile(string filename)
		{
			this.filename = filename;
			subtype       = Subtype.Code;
			buildaction   = BuildAction.Compile;
		}
		
		public ProjectFile(string filename, BuildAction buildAction)
		{
			this.filename = filename;
			subtype       = Subtype.Code;
			buildaction   = buildAction;
		}
		
		internal void SetProject (Project prj)
		{
			project = prj;
		}
						
		[LocalizedProperty("${res:MonoDevelop.Internal.Project.ProjectFile.Name}",
		                   Description ="${res:MonoDevelop.Internal.Project.ProjectFile.Name.Description}")]
		[ReadOnly(true)]
		public string Name {
			get {
				return filename;
			}
			set {
				Debug.Assert (value != null && value.Length > 0, "name == null || name.Length == 0");
				string oldName = filename;
				filename = value;
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
			}
		}
		
		[LocalizedProperty("${res:MonoDevelop.Internal.Project.ProjectFile.BuildAction}",
		                   Description ="${res:MonoDevelop.Internal.Project.ProjectFile.BuildAction.Description}")]
		public BuildAction BuildAction {
			get {
				return buildaction;
			}
			set {
				buildaction = value;
			}
		}
		
		[Browsable(false)]
		public string DependsOn {
			get {
				return dependsOn;
			}
			set {
				dependsOn = value;
			}
		}
		
		[Browsable(false)]
		public string Data {
			get {
				return data;
			}
			set {
				data = value;
			}
		}
		
		public object Clone()
		{
			return MemberwiseClone();
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
