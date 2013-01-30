// DotNetProjectSubtype.cs
//
// Author:
//   Lluis Sanchez Gual <lluis@novell.com>
//
// Copyright (c) 2008 Novell, Inc (http://www.novell.com)
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
using System.Linq;
using Mono.Addins;
using MonoDevelop.Projects.Formats.MSBuild;
using System.Collections.Generic;
using MonoDevelop.Core;

namespace MonoDevelop.Projects.Extensions
{
	[ExtensionNodeChild (typeof(DotNetProjectSubtypeNodeImport), "AddImport")]
	[ExtensionNodeChild (typeof(DotNetProjectSubtypeNodeImport), "RemoveImport")]
	public class DotNetProjectSubtypeNode: ExtensionNode
	{
		[NodeAttribute]
		string guid = null;
		
		[NodeAttribute]
		string type = null;
		
		[NodeAttribute]
		string import = null;
		
		[NodeAttribute]
		string extension = null;

		[NodeAttribute]
		string exclude = null;

		[NodeAttribute]
		bool useXBuild = false;
		
		[NodeAttribute]
		string migrationHandler;

		[NodeAttribute]
		bool migrationRequired = true;

		Type itemType;

		public string Import {
			get {
				return import;
			}
		}
		
		public Type Type {
			get {
				if (itemType == null) {
					itemType = Addin.GetType (type, true);
					if (!typeof(MonoDevelop.Projects.DotNetProject).IsAssignableFrom (itemType))
						throw new InvalidOperationException ("Type must be a subclass of DotNetProject");
				}
				return itemType;
			}
		}

		public string Extension {
			get { 
				return extension; 
			}
		}
		
		public string Exclude {
			get { 
				return exclude; 
			}
		}
		
		public string Guid {
			get { return guid; }
		}
		
		public bool UseXBuild {
			get { return useXBuild; }
		}
		
		public bool IsMigration {
			get { return migrationHandler != null; }
		}
		
		public bool IsMigrationRequired {
			get { return migrationRequired; }
		}

		public IDotNetSubtypeMigrationHandler MigrationHandler {
			get { return (IDotNetSubtypeMigrationHandler) Addin.CreateInstance (migrationHandler); }
		}
		
		public bool SupportsType (string guid)
		{
			return string.Compare (this.guid, guid, true) == 0;
		}
		
		public DotNetProject CreateInstance (string language)
		{
			return (DotNetProject) Activator.CreateInstance (Type, language);
		}
		
		public virtual bool CanHandleItem (SolutionEntityItem item)
		{
			return !(IsMigration && IsMigrationRequired) && Type.IsAssignableFrom (item.GetType ());
		}
		
		public virtual bool CanHandleType (Type type)
		{
			return !(IsMigration && IsMigrationRequired) && Type.IsAssignableFrom (type);
		}

		public virtual bool CanHandleFile (string fileName, string typeGuid)
		{
			if (typeGuid != null && typeGuid.ToLower().Contains(guid.ToLower()))
				return true;
			if (!string.IsNullOrEmpty (extension) && System.IO.Path.GetExtension (fileName) == "." + extension)
				return true;
			return false;
		}
		
		public virtual void InitializeHandler (SolutionEntityItem item)
		{
			MSBuildProjectHandler h = (MSBuildProjectHandler) ProjectExtensionUtil.GetItemHandler (item);
			UpdateImports (item, h.TargetImports);
			h.SubtypeGuids.Add (guid);
			if (UseXBuild)
				h.ForceUseMSBuild = true;
		}
		
		public void UpdateImports (SolutionEntityItem item, List<string> imports)
		{
			DotNetProject p = (DotNetProject) item;
			if (!string.IsNullOrEmpty (import))
				imports.AddRange (import.Split (':'));
			if (!string.IsNullOrEmpty (exclude))
				exclude.Split (':').ToList ().ForEach (i => imports.Remove (i));
			
			foreach (DotNetProjectSubtypeNodeImport iob in ChildNodes) {
				if (iob.Language == p.LanguageName) {
					if (iob.IsAdd)
						imports.AddRange (iob.Projects.Split (':'));
					else
						iob.Projects.Split (':').ToList ().ForEach (i => imports.Remove (i));
				}
			}
		}
	}
	
	class DotNetProjectSubtypeNodeImport: ExtensionNode
	{
		protected override void Read (NodeElement elem)
		{
			IsAdd = elem.NodeName == "AddImport";
			base.Read (elem);
		}
		
		[NodeAttribute ("language")]
		public string Language { get; set; }
		
		[NodeAttribute ("projects")]
		public string Projects { get; set; }
		
		public bool IsAdd { get; private set; }
	}
	
	public interface IDotNetSubtypeMigrationHandler
	{
		IEnumerable<string> FilesToBackup (string filename);
		Type Migrate (IProjectLoadProgressMonitor monitor, MSBuildProject project, string fileName, string language);
		bool CanPromptForMigration { get; }
		MigrationType PromptForMigration (IProjectLoadProgressMonitor monitor, MSBuildProject project, string fileName, string language);
	}
	
	public enum MigrationType {
		Ignore,
		Migrate,
		BackupAndMigrate,
	}
}
