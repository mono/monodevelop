// MSBuildProjectService.cs
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
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Globalization;
using Mono.Addins;
using MonoDevelop.Projects;
using MonoDevelop.Projects.Extensions;
using MonoDevelop.Projects.Serialization;
using MonoDevelop.Core;

namespace MonoDevelop.Projects.Formats.MSBuild
{
	public static class MSBuildProjectService
	{
		const string ItemTypesExtensionPath = "/MonoDevelop/ProjectModel/MSBuildItemTypes";
		public const string GenericItemGuid = "{9344bdbb-3e7f-41fc-a0dd-8665d75ee146}";
		public const string FolderTypeGuid = "{2150E333-8FDC-42A3-9474-1A3956D46DE8}";
		
		static DataContext dataContext;
		
		public static DataContext DataContext {
			get {
				if (dataContext == null) {
					dataContext = new MSBuildDataContext ();
					Services.ProjectService.InitializeDataContext (dataContext);
					foreach (ItemMember prop in MSBuildProjectHandler.ExtendedMSBuildProperties) {
						ItemProperty iprop = new ItemProperty (prop.Name, prop.Type);
						iprop.IsExternal = false;
						dataContext.RegisterProperty (prop.DeclaringType, iprop);
					}
				}
				return dataContext;
			}
		}
		
		static MSBuildProjectService ()
		{
			Services.ProjectService.DataContextChanged += delegate {
				dataContext = null;
			};
		}
		
		public static SolutionEntityItem LoadItem (IProgressMonitor monitor, string fileName, string typeGuid, string itemGuid)
		{
			if ((typeGuid != null && string.Compare (typeGuid, GenericItemGuid, true) == 0) || Path.GetExtension (fileName) == ".mdproj") {
				MSBuildProjectHandler handler = new MSBuildProjectHandler (GenericItemGuid, null, null);
				return handler.Load (monitor, fileName, null, null);
			}
			
			foreach (ItemTypeNode node in GetItemTypeNodes ()) {
				if (node.CanHandleFile (fileName, typeGuid))
					return node.LoadSolutionItem (monitor, fileName, itemGuid);
			}
			return null;
		}
		
		internal static MSBuildProjectHandler GetItemHandler (SolutionEntityItem item)
		{
			MSBuildProjectHandler handler = item.ItemHandler as MSBuildProjectHandler;
			if (handler != null)
				return handler;
			else
				throw new InvalidOperationException ("Not an MSBuild project");
		}
		
		internal static void SetId (SolutionItem item, string id)
		{
			MSBuildHandler handler = item.ItemHandler as MSBuildHandler;
			if (handler != null)
				handler.ItemId = id;
			else
				throw new InvalidOperationException ("Not an MSBuild project");
		}
		
		internal static void InitializeItemHandler (SolutionItem item)
		{
			SolutionEntityItem eitem = item as SolutionEntityItem;
			if (eitem != null) {
				foreach (ItemTypeNode node in GetItemTypeNodes ()) {
					if (node.CanHandleItem (eitem)) {
						node.InitializeHandler (eitem);
						foreach (DotNetProjectSubtypeNode snode in GetItemSubtypeNodes ()) {
							if (snode.CanHandleItem (eitem)) {
								snode.InitializeHandler (eitem);
								break;
							}
						}
						return;
					}
				}
				MSBuildProjectHandler h = new MSBuildProjectHandler (GenericItemGuid, null, null);
				h.Item = eitem;
				eitem.SetItemHandler (h);
			}
			else if (item is SolutionFolder) {
				MSBuildHandler h = new MSBuildHandler (FolderTypeGuid, null);
				h.Item = item;
				item.SetItemHandler (h);
			}
		}
		
		internal static DotNetProjectSubtypeNode GetDotNetProjectSubtype (string typeGuids)
		{
			if (!string.IsNullOrEmpty (typeGuids)) {
				foreach (string guid in typeGuids.Split (';')) {
					string tguid = guid.Trim ();
					foreach (DotNetProjectSubtypeNode st in GetItemSubtypeNodes ()) {
						if (st.SupportsType (tguid))
							return st;
					}
				}
			}
			return null;
		}
		
		static IEnumerable<ItemTypeNode> GetItemTypeNodes ()
		{
			foreach (ExtensionNode node in AddinManager.GetExtensionNodes (ItemTypesExtensionPath)) {
				if (node is ItemTypeNode)
					yield return (ItemTypeNode) node;
			}
		}
		
		static IEnumerable<DotNetProjectSubtypeNode> GetItemSubtypeNodes ()
		{
			foreach (ExtensionNode node in AddinManager.GetExtensionNodes (ItemTypesExtensionPath)) {
				if (node is DotNetProjectSubtypeNode)
					yield return (DotNetProjectSubtypeNode) node;
			}
		}
		
		internal static ItemTypeNode FindHandlerForFile (string file)
		{
			foreach (ItemTypeNode node in GetItemTypeNodes ()) {
				if (node.CanHandleFile (file, null)) {
					return node;
				}
			}
			return null;
		}
		
		internal static ItemTypeNode FindHandlerForItem (SolutionEntityItem item)
		{
			foreach (ItemTypeNode node in GetItemTypeNodes ()) {
				if (node.CanHandleItem (item)) {
					return node;
				}
			}
			return null;
		}
		
		public static string ToMSBuildPath (string baseDirectory, string absPath)
		{
			return FileService.NormalizeRelativePath (FileService.AbsoluteToRelativePath (
			         baseDirectory, absPath)).Replace ('/', '\\');
		}
		

		internal static string FromMSBuildPath (string basePath, string relPath)
		{
			if (relPath == null || relPath.Length == 0)
				return null;
			
			string path = relPath;
			if (Path.DirectorySeparatorChar != '\\')
				path = path.Replace ("\\", "/");

			if (char.IsLetter (path [0]) && path.Length > 1 && path[1] == ':')
				return null;
			
			if (basePath != null)
				path = Path.Combine (basePath, path);

			if (System.IO.File.Exists (path)){
				return Path.GetFullPath (path);
			}
				
			if (Path.IsPathRooted (path)) {
					
				// Windows paths are case-insensitive. When mapping an absolute path
				// we can try to find the correct case for the path.
				
				string[] names = path.Substring (1).Split ('/');
				string part = "/";
				
				for (int n=0; n<names.Length; n++) {
					string[] entries;

					if (names [n] == ".."){
						part = Path.GetFullPath (part + "/..");
						continue;
					}
					
					entries = Directory.GetFileSystemEntries (part);
					
					string fpath = null;
					foreach (string e in entries) {
						if (string.Compare (Path.GetFileName (e), names[n], true) == 0) {
							fpath = e;
							break;
						}
					}
					if (fpath == null) {
						// Part of the path does not exist. Can't do any more checking.
						part = Path.GetFullPath (part);
						for (; n < names.Length; n++)
							part += "/" + names[n];
						return part;
					}

					part = fpath;
				}
				return Path.GetFullPath (part);
			} else {
				return Path.GetFullPath (path);
			}
		}

		public static string GetDefaultResourceId (ProjectFile file)
		{
			string fname = file.RelativePath;
			if (file.IsExternalToProject)
				fname = Path.GetFileName (fname);
			else
				fname = FileService.NormalizeRelativePath (fname);

			if (String.Compare (Path.GetExtension (fname), ".resx", true) == 0) {
				fname = Path.ChangeExtension (fname, ".resources");
			} else {
				string only_filename, culture, extn;
				if (MSBuildProjectService.TrySplitResourceName (fname, out only_filename, out culture, out extn)) {
					//remove the culture from fname
					//foo.it.bmp -> foo.bmp
					fname = only_filename + "." + extn;
				}
			}

			string rname = fname.Replace ('/', '.');
			
			DotNetProject dp = file.Project as DotNetProject;

			if (dp == null || String.IsNullOrEmpty (dp.DefaultNamespace))
				return rname;
			else
				return dp.DefaultNamespace + "." + rname;
		}
		
		//Given a filename like foo.it.resx, splits it into - foo, it, resx
		//Returns true only if a valid culture is found
		//Note: hand-written as this can get called lotsa times
		public static bool TrySplitResourceName (string fname, out string only_filename, out string culture, out string extn)
		{
			only_filename = culture = extn = null;

			int last_dot = -1;
			int culture_dot = -1;
			int i = fname.Length - 1;
			while (i >= 0) {
				if (fname [i] == '.') {
					last_dot = i;
					break;
				}
				i --;
			}
			if (i < 0)
				return false;

			i--;
			while (i >= 0) {
				if (fname [i] == '.') {
					culture_dot = i;
					break;
				}
				i --;
			}
			if (culture_dot < 0)
				return false;

			culture = fname.Substring (culture_dot + 1, last_dot - culture_dot - 1);
			if (!CultureNamesTable.ContainsKey (culture))
				return false;

			only_filename = fname.Substring (0, culture_dot);
			extn = fname.Substring (last_dot + 1);
			return true;
		}

		static Dictionary<string, string> cultureNamesTable;
		static Dictionary<string, string> CultureNamesTable {
			get {
				if (cultureNamesTable == null) {
					cultureNamesTable = new Dictionary<string, string> ();
					foreach (CultureInfo ci in CultureInfo.GetCultures (CultureTypes.AllCultures))
						cultureNamesTable [ci.Name] = ci.Name;
				}

				return cultureNamesTable;
			}
		}
	}
	
	class MSBuildDataContext: DataContext
	{
		protected override DataType CreateConfigurationDataType (Type type)
		{
			if (type == typeof(bool))
				return new MSBuildBoolDataType ();
			else
				return base.CreateConfigurationDataType (type);
		}
	}
	
	class MSBuildBoolDataType: PrimitiveDataType
	{
		public MSBuildBoolDataType (): base (typeof(bool))
		{
		}
		
		internal protected override DataNode OnSerialize (SerializationContext serCtx, object mapData, object value)
		{
			return new DataValue (Name, (bool)value ? "true" : "false");
		}
		
		internal protected override object OnDeserialize (SerializationContext serCtx, object mapData, DataNode data)
		{
			return ((DataValue)data).Value == "true";
		}
	}
}
