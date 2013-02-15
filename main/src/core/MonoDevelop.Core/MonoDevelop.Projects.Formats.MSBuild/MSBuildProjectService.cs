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
using System.Diagnostics;
using System.Collections.Generic;
using System.IO;
using System.Globalization;
using System.Runtime.Serialization.Formatters.Binary;
using Mono.Addins;
using MonoDevelop.Core.ProgressMonitoring;
using MonoDevelop.Projects;
using MonoDevelop.Projects.Extensions;
using MonoDevelop.Core.Serialization;
using MonoDevelop.Core;
using MonoDevelop.Core.Assemblies;
using Cecil = Mono.Cecil;

namespace MonoDevelop.Projects.Formats.MSBuild
{
	public static class MSBuildProjectService
	{
		const string ItemTypesExtensionPath = "/MonoDevelop/ProjectModel/MSBuildItemTypes";
		public const string GenericItemGuid = "{9344bdbb-3e7f-41fc-a0dd-8665d75ee146}";
		public const string FolderTypeGuid = "{2150E333-8FDC-42A3-9474-1A3956D46DE8}";
		
		//NOTE: default toolsversion should match the default format.
		// remember to update the builder process' app.config too
		public const string DefaultFormat = "MSBuild10";
		const string REFERENCED_MSBUILD_TOOLS = "4.0";
		internal const string DefaultToolsVersion = REFERENCED_MSBUILD_TOOLS;
		
		static DataContext dataContext;
		
		static Dictionary<string,RemoteBuildEngine> builders = new Dictionary<string, RemoteBuildEngine> ();
		static GenericItemTypeNode genericItemTypeNode = new GenericItemTypeNode ();
		
		public static DataContext DataContext {
			get {
				if (dataContext == null) {
					dataContext = new MSBuildDataContext ();
					Services.ProjectService.InitializeDataContext (dataContext);
					foreach (ItemMember prop in MSBuildProjectHandler.ExtendedMSBuildProperties) {
						ItemProperty iprop = new ItemProperty (prop.Name, prop.Type);
						iprop.IsExternal = prop.IsExternal;
						if (prop.CustomAttributes != null)
							iprop.CustomAttributes = prop.CustomAttributes;
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

			PropertyService.PropertyChanged += HandlePropertyChanged;
			DefaultBuildWithMSBuild = PropertyService.Get ("MonoDevelop.Ide.BuildWithMSBuild", false);
			DefaultMSBuildVerbosity = PropertyService.Get ("MonoDevelop.Ide.MSBuildVerbosity", MSBuildVerbosity.Normal);
		}

		static void HandlePropertyChanged (object sender, PropertyChangedEventArgs e)
		{
			if (e.Key == "MonoDevelop.Ide.BuildWithMSBuild") {
				DefaultBuildWithMSBuild = (bool) e.NewValue;
			} else if (e.Key == "MonoDevelop.Ide.MSBuildVerbosity") {
				DefaultMSBuildVerbosity = (MSBuildVerbosity) e.NewValue;
			}
		}

		internal static bool DefaultBuildWithMSBuild { get; private set; }
		internal static MSBuildVerbosity DefaultMSBuildVerbosity { get; private set; }
		
		public static SolutionEntityItem LoadItem (IProgressMonitor monitor, string fileName, MSBuildFileFormat expectedFormat, string typeGuid, string itemGuid)
		{
			foreach (ItemTypeNode node in GetItemTypeNodes ()) {
				if (node.CanHandleFile (fileName, typeGuid))
					return node.LoadSolutionItem (monitor, fileName, expectedFormat, itemGuid);
			}
			
			if (string.IsNullOrEmpty (typeGuid) && IsProjectSubtypeFile (fileName)) {
				typeGuid = LoadProjectTypeGuids (fileName);
				foreach (ItemTypeNode node in GetItemTypeNodes ()) {
					if (node.CanHandleFile (fileName, typeGuid))
						return node.LoadSolutionItem (monitor, fileName, expectedFormat, itemGuid);
				}
			}

			return null;
		}
		
		internal static IResourceHandler GetResourceHandlerForItem (DotNetProject project)
		{
			foreach (ItemTypeNode node in GetItemTypeNodes ()) {
				DotNetProjectNode pNode = node as DotNetProjectNode;
				if (pNode != null && pNode.CanHandleItem (project))
					return pNode.GetResourceHandler ();
			}
			return new MSBuildResourceHandler ();
		}
		
		internal static MSBuildHandler GetItemHandler (SolutionEntityItem item)
		{
			MSBuildHandler handler = item.ItemHandler as MSBuildHandler;
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
							if (snode.CanHandleItem (eitem))
								snode.InitializeHandler (eitem);
						}
						return;
					}
				}
			}
			else if (item is SolutionFolder) {
				MSBuildHandler h = new MSBuildHandler (FolderTypeGuid, null);
				h.Item = item;
				item.SetItemHandler (h);
			}
		}
		
		public static bool SupportsProjectType (string projectFile)
		{
			if (!string.IsNullOrWhiteSpace (projectFile)) {
				// If we have a project file, try to load it.
				try {
					using (var monitor = new ConsoleProgressMonitor ()) {
						return MSBuildProjectService.LoadItem (monitor, projectFile, null, null, null) != null;
					}
				} catch {
					return false;
				}
			}

			return false;
		}

		internal static DotNetProjectSubtypeNode GetDotNetProjectSubtype (string typeGuids)
		{
			if (!string.IsNullOrEmpty (typeGuids)) {
				Type ptype = null;
				DotNetProjectSubtypeNode foundNode = null;
				foreach (string guid in typeGuids.Split (';')) {
					string tguid = guid.Trim ();
					foreach (DotNetProjectSubtypeNode st in GetItemSubtypeNodes ()) {
						if (st.SupportsType (tguid)) {
							if (ptype == null || ptype.IsAssignableFrom (st.Type)) {
								ptype = st.Type;
								foundNode = st;
							}
						}
					}
				}
				return foundNode;
			}
			return null;
		}
		
		static IEnumerable<ItemTypeNode> GetItemTypeNodes ()
		{
			foreach (ExtensionNode node in AddinManager.GetExtensionNodes (ItemTypesExtensionPath)) {
				if (node is ItemTypeNode)
					yield return (ItemTypeNode) node;
			}
			yield return genericItemTypeNode;
		}
		
		internal static IEnumerable<DotNetProjectSubtypeNode> GetItemSubtypeNodes ()
		{
			foreach (ExtensionNode node in AddinManager.GetExtensionNodes (ItemTypesExtensionPath)) {
				if (node is DotNetProjectSubtypeNode)
					yield return (DotNetProjectSubtypeNode) node;
			}
		}
		
		internal static ItemTypeNode FindHandlerForFile (FilePath file)
		{
			foreach (ItemTypeNode node in GetItemTypeNodes ()) {
				if (node.CanHandleFile (file, null)) {
					return node;
				}
			}
			if (IsProjectSubtypeFile (file)) {
				string typeGuids = LoadProjectTypeGuids (file);
				foreach (ItemTypeNode node in GetItemTypeNodes ()) {
					if (node.CanHandleFile (file, typeGuids)) {
						return node;
					}
				}
			}
			return null;
		}
		
		internal static string GetExtensionForItem (SolutionEntityItem item)
		{
			foreach (DotNetProjectSubtypeNode node in GetItemSubtypeNodes ()) {
				if (!string.IsNullOrEmpty (node.Extension) && node.CanHandleItem (item))
					return node.Extension;
			}
			foreach (ItemTypeNode node in GetItemTypeNodes ()) {
				if (node.CanHandleItem (item)) {
					return node.Extension;
				}
			}
			// The generic handler should always be found
			throw new InvalidOperationException ();
		}
		
		static bool IsProjectSubtypeFile (FilePath file)
		{
			foreach (DotNetProjectSubtypeNode node in GetItemSubtypeNodes ()) {
				if (!string.IsNullOrEmpty (node.Extension) && node.CanHandleFile (file, null))
					return true;
			}
			return false;
		}
		
		static char[] specialCharacters = new char [] {'%', '$', '@', '(', ')', '\'', ';', '?' };
		
		public static string EscapeString (string str)
		{
			int i = str.IndexOfAny (specialCharacters);
			while (i != -1) {
				str = str.Substring (0, i) + '%' + ((int) str [i]).ToString ("X") + str.Substring (i + 1);
				i = str.IndexOfAny (specialCharacters, i + 3);
			}
			return str;
		}

		public static string UnescapePath (string path)
		{
			if (string.IsNullOrEmpty (path))
				return path;
			
			if (!Platform.IsWindows)
				path = path.Replace ("\\", "/");
			
			return UnscapeString (path);
		}
		
		public static string UnscapeString (string str)
		{
			int i = str.IndexOf ('%');
			while (i != -1 && i < str.Length - 2) {
				int c;
				if (int.TryParse (str.Substring (i+1, 2), NumberStyles.HexNumber, null, out c))
					str = str.Substring (0, i) + (char) c + str.Substring (i + 3);
				i = str.IndexOf ('%', i + 1);
			}
			return str;
		}
		
		public static string ToMSBuildPath (string baseDirectory, string absPath)
		{
			absPath = EscapeString (absPath);
			if (baseDirectory != null) {
				absPath = FileService.NormalizeRelativePath (FileService.AbsoluteToRelativePath (
				         baseDirectory, absPath));
			}
			return absPath.Replace ('/', '\\');
		}
		
		internal static string ToMSBuildPathRelative (string baseDirectory, string absPath)
		{
			FilePath file = ToMSBuildPath (baseDirectory, absPath);
			return file.ToRelative (baseDirectory);
		}

		
		internal static string FromMSBuildPathRelative (string basePath, string relPath)
		{
			FilePath file = FromMSBuildPath (basePath, relPath);
			return file.ToRelative (basePath);
		}

		public static string FromMSBuildPath (string basePath, string relPath)
		{
			string res;
			FromMSBuildPath (basePath, relPath, out res);
			return res;
		}
		
		internal static bool IsAbsoluteMSBuildPath (string path)
		{
			if (path.Length > 1 && char.IsLetter (path [0]) && path[1] == ':')
				return true;
			if (path.Length > 0 && path [0] == '\\')
				return true;
			return false;
		}
		
		internal static bool FromMSBuildPath (string basePath, string relPath, out string resultPath)
		{
			resultPath = relPath;
			
			if (string.IsNullOrEmpty (relPath))
				return false;
			
			string path = UnescapePath (relPath);

			if (char.IsLetter (path [0]) && path.Length > 1 && path[1] == ':') {
				if (Platform.IsWindows) {
					resultPath = path; // Return the escaped value
					return true;
				} else
					return false;
			}
			
			bool isRooted = Path.IsPathRooted (path);
			
			if (!isRooted && basePath != null) {
				path = Path.Combine (basePath, path);
				isRooted = Path.IsPathRooted (path);
			}
			
			// Return relative paths as-is, we can't do anything else with them
			if (!isRooted) {
				resultPath = FileService.NormalizeRelativePath (path);
				return true;
			}
			
			// If we're on Windows, don't need to fix file casing.
			if (Platform.IsWindows) {
				resultPath = Path.GetFullPath (path);
				return true;
			}
			
			// If the path exists with the exact casing specified, then we're done
			if (System.IO.File.Exists (path) || System.IO.Directory.Exists (path)){
				resultPath = Path.GetFullPath (path);
				return true;
			}
			
			// Not on Windows, file doesn't exist. That could mean the project was brought from Windows
			// and the filename case in the project doesn't match the file on disk, because Windows is
			// case-insensitive. Since we have an absolute path, search the directory for the file with
			// the correct case.
			string[] names = path.Substring (1).Split ('/');
			string part = "/";
			
			for (int n=0; n<names.Length; n++) {
				string[] entries;

				if (names [n] == ".."){
					if (part == "/")
						return false; // Can go further back. It's not an existing file
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
					resultPath = part;
					return true;
				}

				part = fpath;
			}
			resultPath = Path.GetFullPath (part);
			return true;
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
		
		public static RemoteProjectBuilder GetProjectBuilder (TargetRuntime runtime, string toolsVersion, string file)
		{
			lock (builders) {
				var toolsFx = Runtime.SystemAssemblyService.GetTargetFramework (new TargetFrameworkMoniker (toolsVersion));
				string binDir = runtime.GetMSBuildBinPath (toolsFx);
				
				if (!runtime.IsInstalled (toolsFx))
					throw new InvalidOperationException (string.Format (
						"Runtime '{0}' does not have the MSBuild '{1}' framework installed",
						runtime.Id, toolsVersion));
				
				string builderKey = runtime.Id + " " + toolsVersion;
				RemoteBuildEngine builder;
				if (builders.TryGetValue (builderKey, out builder)) {
					builder.ReferenceCount++;
					return new RemoteProjectBuilder (file, binDir, builder);
				}

				//always start the remote process explicitly, even if it's using the current runtime and fx
				//else it won't pick up the assembly redirects from the builder exe
				var exe = GetExeLocation (runtime, toolsVersion);
				MonoDevelop.Core.Execution.RemotingService.RegisterRemotingChannel ();
				var pinfo = new ProcessStartInfo (exe) {
					UseShellExecute = false,
					CreateNoWindow = true,
					RedirectStandardError = true,
					RedirectStandardInput = true,
				};
				runtime.GetToolsExecutionEnvironment (toolsFx).MergeTo (pinfo);
				
				Process p = null;
				try {
					p = runtime.ExecuteAssembly (pinfo);
					p.StandardInput.WriteLine (Process.GetCurrentProcess ().Id.ToString ());
					string sref = p.StandardError.ReadLine ();
					byte[] data = Convert.FromBase64String (sref);
					MemoryStream ms = new MemoryStream (data);
					BinaryFormatter bf = new BinaryFormatter ();
					builder = new RemoteBuildEngine (p, (IBuildEngine) bf.Deserialize (ms));
				} catch {
					if (p != null) {
						try {
							p.Kill ();
						} catch { }
					}
					throw;
				}
				
				builders [builderKey] = builder;
				builder.ReferenceCount = 1;
				return new RemoteProjectBuilder (file, binDir, builder);
			}
		}
		
		static string GetExeLocation (TargetRuntime runtime, string toolsVersion)
		{
			FilePath sourceExe = typeof(ProjectBuilder).Assembly.Location;

			if ((runtime is MsNetTargetRuntime) && int.Parse (toolsVersion.Split ('.')[0]) >= 4)
				toolsVersion = "dotnet." + toolsVersion;

			if (toolsVersion == REFERENCED_MSBUILD_TOOLS)
				return sourceExe;
			
			var exe = sourceExe.ParentDirectory.Combine ("MSBuild", toolsVersion, sourceExe.FileName);
			if (File.Exists (exe))
				return exe;
			
			throw new InvalidOperationException ("Unsupported MSBuild ToolsVersion '" + toolsVersion + "'");
		}

		internal static void ReleaseProjectBuilder (RemoteBuildEngine engine)
		{
			lock (builders) {
				if (engine.ReferenceCount > 0) {
					if (--engine.ReferenceCount == 0) {
						engine.ReleaseTime = DateTime.Now.AddSeconds (3);
						ScheduleProjectBuilderCleanup (engine.ReleaseTime.AddMilliseconds (500));
					}
				}
			}
		}
		
		static DateTime nextCleanup = DateTime.MinValue;
		
		static void ScheduleProjectBuilderCleanup (DateTime cleanupTime)
		{
			lock (builders) {
				if (cleanupTime < nextCleanup)
					return;
				nextCleanup = cleanupTime;
				System.Threading.ThreadPool.QueueUserWorkItem (delegate {
					DateTime tnow = DateTime.Now;
					while (tnow < nextCleanup) {
						System.Threading.Thread.Sleep ((int)(nextCleanup - tnow).TotalMilliseconds);
						CleanProjectBuilders ();
						tnow = DateTime.Now;
					}
				});
			}
		}
		
		static void CleanProjectBuilders ()
		{
			lock (builders) {
				DateTime tnow = DateTime.Now;
				foreach (var val in new Dictionary<string,RemoteBuildEngine> (builders)) {
					if (val.Value.ReferenceCount == 0 && val.Value.ReleaseTime <= tnow) {
						builders.Remove (val.Key);
						val.Value.Dispose ();
					}
				}
			}
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

		static string LoadProjectTypeGuids (string fileName)
		{
			MSBuildProject project = new MSBuildProject ();
			project.Load (fileName);
			
			MSBuildPropertySet globalGroup = project.GetGlobalPropertyGroup ();
			if (globalGroup == null)
				return null;

			return globalGroup.GetPropertyValue ("ProjectTypeGuids");
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
			return new MSBuildBoolDataValue (Name, (bool) value);
		}
		
		internal protected override object OnDeserialize (SerializationContext serCtx, object mapData, DataNode data)
		{
			return String.Equals (((DataValue)data).Value, "true", StringComparison.OrdinalIgnoreCase);
		}
	}

	class MSBuildBoolDataValue : DataValue
	{
		public MSBuildBoolDataValue (string name, bool value)
			: base (name, value ? "True" : "False")
		{
			RawValue = value;
		}

		public bool RawValue { get; private set; }
	}
	
	public class MSBuildResourceHandler: IResourceHandler
	{
		public static MSBuildResourceHandler Instance = new MSBuildResourceHandler ();
		
		public virtual string GetDefaultResourceId (ProjectFile file)
		{
			string fname = file.ProjectVirtualPath;
			fname = FileService.NormalizeRelativePath (fname);
			fname = Path.Combine (Path.GetDirectoryName (fname).Replace (' ','_'), Path.GetFileName (fname));

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

			string rname = fname.Replace (Path.DirectorySeparatorChar, '.');
			
			DotNetProject dp = file.Project as DotNetProject;

			if (dp == null || String.IsNullOrEmpty (dp.DefaultNamespace))
				return rname;
			else
				return dp.DefaultNamespace + "." + rname;
		}
	}
	
	class GenericItemTypeNode: ItemTypeNode
	{
		public GenericItemTypeNode (): base (MSBuildProjectService.GenericItemGuid, "mdproj", null)
		{
		}
		
		public override bool CanHandleItem (SolutionEntityItem item)
		{
			return true;
		}
		
		public override SolutionEntityItem LoadSolutionItem (IProgressMonitor monitor, string fileName, MSBuildFileFormat expectedFormat, string itemGuid)
		{
			MSBuildProjectHandler handler = new MSBuildProjectHandler (Guid, Import, itemGuid);
			return handler.Load (monitor, fileName, expectedFormat, null, null);
		}
	}
}