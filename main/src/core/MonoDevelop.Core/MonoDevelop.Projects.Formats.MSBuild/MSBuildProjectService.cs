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
using System.Linq;
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
using System.Threading;

namespace MonoDevelop.Projects.Formats.MSBuild
{
	public static class MSBuildProjectService
	{
		const string ItemTypesExtensionPath = "/MonoDevelop/ProjectModel/MSBuildItemTypes";
		public const string GenericItemGuid = "{9344BDBB-3E7F-41FC-A0DD-8665D75EE146}";
		public const string FolderTypeGuid = "{2150E333-8FDC-42A3-9474-1A3956D46DE8}";
		
		//NOTE: default toolsversion should match the default format.
		// remember to update the builder process' app.config too
		public const string DefaultFormat = "MSBuild12";
		
		static DataContext dataContext;
		
		static IMSBuildGlobalPropertyProvider[] globalPropertyProviders;
		static Dictionary<string,RemoteBuildEngine> builders = new Dictionary<string, RemoteBuildEngine> ();
		static GenericItemTypeNode genericItemTypeNode = new GenericItemTypeNode ();

		internal static bool ShutDown { get; private set; }
		
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
			DefaultMSBuildVerbosity = PropertyService.Get ("MonoDevelop.Ide.MSBuildVerbosity", MSBuildVerbosity.Normal);

			Runtime.ShuttingDown += (sender, e) => ShutDown = true;

			const string gppPath = "/MonoDevelop/ProjectModel/MSBuildGlobalPropertyProviders";
			globalPropertyProviders = AddinManager.GetExtensionObjects<IMSBuildGlobalPropertyProvider> (gppPath);
			foreach (var gpp in globalPropertyProviders) {
				gpp.GlobalPropertiesChanged += HandleGlobalPropertyProviderChanged;
			}
		}

		static void HandleGlobalPropertyProviderChanged (object sender, EventArgs e)
		{
			lock (builders) {
				var gpp = (IMSBuildGlobalPropertyProvider) sender;
				foreach (var builder in builders.Values)
					builder.SetGlobalProperties (gpp.GetGlobalProperties ());
			}
		}

		static void HandlePropertyChanged (object sender, PropertyChangedEventArgs e)
		{
			if (e.Key == "MonoDevelop.Ide.MSBuildVerbosity") {
				DefaultMSBuildVerbosity = (MSBuildVerbosity) e.NewValue;
			}
		}

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

			// If it is a known unsupported project, load it as UnknownProject
			var projectInfo = MSBuildProjectService.GetUnknownProjectTypeInfo (typeGuid != null ? new [] { typeGuid } : new string[0], fileName);
			if (projectInfo != null && projectInfo.LoadFiles) {
				if (typeGuid == null)
					typeGuid = projectInfo.Guid;
				var h = new MSBuildProjectHandler (typeGuid, "", itemGuid);
				h.SetUnsupportedType (projectInfo);
				return h.Load (monitor, fileName, expectedFormat, "", null);
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

		public static void CheckHandlerUsesMSBuildEngine (SolutionItem item, out bool useByDefault, out bool require)
		{
			var handler = item.ItemHandler as MSBuildProjectHandler;
			if (handler == null) {
				useByDefault = require = false;
				return;
			}
			useByDefault = handler.UseMSBuildEngineByDefault;
			require = handler.RequireMSBuildEngine;
		}

		internal static DotNetProjectSubtypeNode GetDotNetProjectSubtype (string typeGuids)
		{
			if (!string.IsNullOrEmpty (typeGuids))
				return GetDotNetProjectSubtype (typeGuids.Split (';').Select (t => t.Trim ()));
			else
				return null;
		}

		internal static DotNetProjectSubtypeNode GetDotNetProjectSubtype (IEnumerable<string> typeGuids)
		{
			Type ptype = null;
			DotNetProjectSubtypeNode foundNode = null;
			foreach (string guid in typeGuids) {
				foreach (DotNetProjectSubtypeNode st in GetItemSubtypeNodes ()) {
					if (st.SupportsType (guid)) {
						if (ptype == null || ptype.IsAssignableFrom (st.Type)) {
							ptype = st.Type;
							foundNode = st;
						}
					}
				}
			}
			return foundNode;
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
		
		internal static bool CanReadFile (FilePath file)
		{
			foreach (ItemTypeNode node in GetItemTypeNodes ()) {
				if (node.CanHandleFile (file, null)) {
					return true;
				}
			}
			if (IsProjectSubtypeFile (file)) {
				string typeGuids = LoadProjectTypeGuids (file);
				foreach (ItemTypeNode node in GetItemTypeNodes ()) {
					if (node.CanHandleFile (file, typeGuids)) {
						return true;
					}
				}
			}
			return GetUnknownProjectTypeInfo (new string[0], file) != null;
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
			if (baseDirectory != null) {
				absPath = FileService.NormalizeRelativePath (FileService.AbsoluteToRelativePath (baseDirectory, absPath));
			}
			return EscapeString (absPath).Replace ('/', '\\');
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
				resultPath = FileService.GetFullPath (path);
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
					if (string.Compare (Path.GetFileName (e), names [n], StringComparison.OrdinalIgnoreCase) == 0) {
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

		static bool runLocal = false;
		
		internal static RemoteProjectBuilder GetProjectBuilder (TargetRuntime runtime, string minToolsVersion, string file, string solutionFile)
		{
			lock (builders) {
				//attempt to use 14.0 builder first if available
				string toolsVersion = "14.0";
				string binDir = runtime.GetMSBuildBinPath ("14.0");
				if (binDir == null) {
					toolsVersion = "12.0";
					binDir = runtime.GetMSBuildBinPath ("12.0");
					if (binDir == null) {
						//fall back to 4.0, we know it's always available
						toolsVersion = "4.0";
					}
				}

				//check the ToolsVersion we found can handle the project
				Version tv, mtv;
				if (Version.TryParse (toolsVersion, out tv) && Version.TryParse (minToolsVersion, out mtv) && tv < mtv) {
					string error = null;
					if (runtime is MsNetTargetRuntime && minToolsVersion == "12.0")
						error = "MSBuild 2013 is not installed. Please download and install it from " +
						"http://www.microsoft.com/en-us/download/details.aspx?id=40760";
					throw new InvalidOperationException (error ?? string.Format (
						"Runtime '{0}' does not have MSBuild '{1}' ToolsVersion installed",
						runtime.Id, toolsVersion)
					);
				}

				//one builder per solution
				string builderKey = runtime.Id + " # " + solutionFile;
				RemoteBuildEngine builder;
				if (builders.TryGetValue (builderKey, out builder)) {
					builder.ReferenceCount++;
					return new RemoteProjectBuilder (file, builder);
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
				runtime.GetToolsExecutionEnvironment ().MergeTo (pinfo);
				
				Process p = null;

				try {
					IBuildEngine engine;
					if (!runLocal) {
						p = runtime.ExecuteAssembly (pinfo);

						// The builder app will write the build engine reference
						// after reading the process id from the standard input
						ManualResetEvent ev = new ManualResetEvent (false);
						string responseKey = "[MonoDevelop]";
						string sref = null;
						p.ErrorDataReceived += (sender, e) => {
							if (e.Data == null)
								return;

							if (e.Data.StartsWith (responseKey, StringComparison.Ordinal)) {
								sref = e.Data.Substring (responseKey.Length);
								ev.Set ();
							} else
								Console.WriteLine (e.Data);
						};
						p.BeginErrorReadLine ();
						p.StandardInput.WriteLine (Process.GetCurrentProcess ().Id.ToString ());
						if (!ev.WaitOne (TimeSpan.FromSeconds (5)))
							throw new Exception ("MSBuild process could not be started");
						
						byte[] data = Convert.FromBase64String (sref);
						MemoryStream ms = new MemoryStream (data);
						BinaryFormatter bf = new BinaryFormatter ();
						engine = (IBuildEngine)bf.Deserialize (ms);
					} else {
						var asm = System.Reflection.Assembly.LoadFrom (exe);
						var t = asm.GetType ("MonoDevelop.Projects.Formats.MSBuild.BuildEngine");
						engine = (IBuildEngine)Activator.CreateInstance (t);
					}
					engine.SetCulture (GettextCatalog.UICulture);
					engine.SetGlobalProperties (GetCoreGlobalProperties (solutionFile));
					foreach (var gpp in globalPropertyProviders)
						engine.SetGlobalProperties (gpp.GetGlobalProperties ());
					builder = new RemoteBuildEngine (p, engine);
				} catch {
					if (p != null) {
						try {
							p.Kill ();
						} catch {
						}
					}
					throw;
				}

				builders [builderKey] = builder;
				builder.ReferenceCount = 1;
				builder.Disconnected += delegate {
					lock (builders)
						builders.Remove (builderKey);
				};
				return new RemoteProjectBuilder (file, builder);
			}
		}

		static IDictionary<string,string> GetCoreGlobalProperties (string slnFile)
		{
			var dictionary = new Dictionary<string,string> ();

			//this causes build targets to behave how they should inside an IDE, instead of in a command-line process
			dictionary.Add ("BuildingInsideVisualStudio", "true");

			//we don't have host compilers in MD, and this is set to true by some of the MS targets
			//which causes it to always run the CoreCompile task if BuildingInsideVisualStudio is also
			//true, because the VS in-process compiler would take care of the deps tracking
			dictionary.Add ("UseHostCompilerIfAvailable", "false" );

			if (string.IsNullOrEmpty (slnFile))
				return dictionary;

			dictionary.Add ("SolutionPath", Path.GetFullPath (slnFile));
			dictionary.Add ("SolutionName", Path.GetFileNameWithoutExtension (slnFile));
			dictionary.Add ("SolutionFilename", Path.GetFileName (slnFile));
			dictionary.Add ("SolutionDir", Path.GetDirectoryName (slnFile) + Path.DirectorySeparatorChar);

			return dictionary;;
		}
		
		static string GetExeLocation (TargetRuntime runtime, string toolsVersion)
		{
			FilePath sourceExe = typeof(MSBuildProjectService).Assembly.Location;

			if ((runtime is MsNetTargetRuntime) && int.Parse (toolsVersion.Split ('.')[0]) >= 4)
				toolsVersion = "dotnet." + toolsVersion;

			var exe = sourceExe.ParentDirectory.Combine ("MSBuild", toolsVersion, "MonoDevelop.Projects.Formats.MSBuild.exe");
			if (File.Exists (exe))
				return exe;
			
			throw new InvalidOperationException ("Unsupported MSBuild ToolsVersion '" + toolsVersion + "'");
		}

		internal static void ReleaseProjectBuilder (RemoteBuildEngine engine)
		{
			lock (builders) {
				if (--engine.ReferenceCount != 0)
					return;
				builders.Remove (builders.First (kvp => kvp.Value == engine).Key);
			}
			engine.Dispose ();
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

		internal static UnknownProjectTypeNode GetUnknownProjectTypeInfo (string[] guids, string fileName = null)
		{
			var ext = fileName != null ? Path.GetExtension (fileName).TrimStart ('.') : null;
			var nodes = AddinManager.GetExtensionNodes<UnknownProjectTypeNode> ("/MonoDevelop/ProjectModel/UnknownMSBuildProjectTypes")
				.Where (p => guids.Any (p.MatchesGuid) || (ext != null && p.Extension == ext)).ToList ();
			return nodes.FirstOrDefault (n => !n.IsSolvable) ?? nodes.FirstOrDefault (n => n.IsSolvable);
		}

		public static MSBuildProjectHandler GetHandler (Project project)
		{
			return (MSBuildProjectHandler) project.GetItemHandler ();
		}
	}
	
	class MSBuildDataContext: DataContext
	{
		protected override DataType CreateConfigurationDataType (Type type)
		{
			if (type == typeof(bool))
				return new MSBuildBoolDataType ();
			else if (type == typeof(bool?))
				return new MSBuildNullableBoolDataType ();
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

	class MSBuildNullableBoolDataType: PrimitiveDataType
	{
		public MSBuildNullableBoolDataType (): base (typeof(bool))
		{
		}

		internal protected override DataNode OnSerialize (SerializationContext serCtx, object mapData, object value)
		{
			return new MSBuildNullableBoolDataValue (Name, (bool?) value);
		}

		internal protected override object OnDeserialize (SerializationContext serCtx, object mapData, DataNode data)
		{
			var d = (DataValue)data;
			if (string.IsNullOrEmpty (d.Value))
				return (bool?) null;
			return (bool?) String.Equals (d.Value, "true", StringComparison.OrdinalIgnoreCase);
		}
	}

	class MSBuildNullableBoolDataValue : DataValue
	{
		public MSBuildNullableBoolDataValue (string name, bool? value)
			: base (name, value.HasValue? (value.Value? "True" : "False") : null)
		{
			RawValue = value;
		}

		public bool? RawValue { get; private set; }
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
