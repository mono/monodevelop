// MSBuildProjectHandler.cs
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
using System.Xml;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using MonoDevelop.Core;
using MonoDevelop.Projects;
using MonoDevelop.Core.Serialization;
using MonoDevelop.Projects.Formats.MD1;
using MonoDevelop.Projects.Extensions;

namespace MonoDevelop.Projects.Formats.MSBuild
{
	public class MSBuildProjectHandler: MSBuildHandler, IResourceHandler, IPathHandler
	{
		string fileContent;
		List<string> targetImports = new List<string> ();
		IResourceHandler customResourceHandler;
		List<string> subtypeGuids = new List<string> ();
		const string Unspecified = null;
		
		SolutionEntityItem EntityItem {
			get { return (SolutionEntityItem) Item; }
		}
		
		public System.Collections.Generic.List<string> TargetImports {
			get {
				return targetImports;
			}
			set {
				targetImports = value;
			}
		}

		public void SetCustomResourceHandler (IResourceHandler value)
		{
			customResourceHandler = value;
		}

		public List<string> SubtypeGuids {
			get {
				return subtypeGuids;
			}
		}
		
		public MSBuildProjectHandler (string typeGuid, string import, string itemId): base (typeGuid, itemId)
		{
			if (import != null && import.Trim().Length > 0)
				this.targetImports.AddRange (import.Split (':'));
		}

		public override BuildResult RunTarget (IProgressMonitor monitor, string target, string configuration)
		{
			if (Item is DotNetProject) {
				MD1DotNetProjectHandler handler = new MD1DotNetProjectHandler ((DotNetProject)Item);
				return handler.RunTarget (monitor, target, configuration);
			} else
				return null;
		}
		
		public string GetDefaultResourceId (ProjectFile file)
		{
			if (customResourceHandler != null)
				return customResourceHandler.GetDefaultResourceId (file);
			else
				return MSBuildResourceHandler.Instance.GetDefaultResourceId (file);
		}
		
		public string EncodePath (string path, string oldPath)
		{
			string basePath = Path.GetDirectoryName (EntityItem.FileName);
			return FileService.RelativeToAbsolutePath (basePath, path);
		}
		
		public string DecodePath (string path)
		{
			string basePath = Path.GetDirectoryName (EntityItem.FileName);
			return FileService.AbsoluteToRelativePath (basePath, path);
		}

		public SolutionEntityItem Load (IProgressMonitor monitor, string fileName, string language, Type itemClass)
		{
			MSBuildProject p = new MSBuildProject ();
			fileContent = File.ReadAllText (fileName);
			p.LoadXml (fileContent);
			
			MSBuildPropertyGroup globalGroup = p.GetGlobalPropertyGroup ();
			
			// Avoid crash if there is not global group
			if (globalGroup == null)
				globalGroup = p.AddNewPropertyGroup (false);
			
			string itemGuid = globalGroup.GetPropertyValue ("ProjectGuid");
			string projectTypeGuids = globalGroup.GetPropertyValue ("ProjectTypeGuids");
			string itemType = globalGroup.GetPropertyValue ("ItemType");

			subtypeGuids.Clear ();
			if (projectTypeGuids != null) {
				foreach (string guid in projectTypeGuids.Split (';')) {
					string sguid = guid.Trim ();
					if (sguid.Length > 0 && sguid != TypeGuid)
						subtypeGuids.Add (guid);
				}
			}
			
			Item = CreateSolutionItem (language, projectTypeGuids, itemType, itemClass);

			Item.SetItemHandler (this);
			MSBuildProjectService.SetId (Item, itemGuid);
			
			SolutionEntityItem it = (SolutionEntityItem) Item;
			
			it.FileName = fileName;
			it.Name = System.IO.Path.GetFileNameWithoutExtension (fileName);
			
			try {
				ProjectExtensionUtil.BeginLoadOperation ();
				Load (monitor, p);
			} finally {
				ProjectExtensionUtil.EndLoadOperation ();
			}
			
			return it;
		}
		
		SolutionItem CreateSolutionItem (string language, string typeGuids, string itemType, Type itemClass)
		{
			// All the parameters are optional, but at least one must be provided.
			
			SolutionItem item = null;
			
			if (!string.IsNullOrEmpty (typeGuids)) {
				DotNetProjectSubtypeNode st = MSBuildProjectService.GetDotNetProjectSubtype (typeGuids);
				if (st != null) {
					item = st.CreateInstance (language);
					if (!string.IsNullOrEmpty (st.Import))
						targetImports.AddRange (st.Import.Split (':'));
				}
			}
			if (item == null && itemClass != null)
				item = (SolutionItem) Activator.CreateInstance (itemClass);
			
			if (item == null && !string.IsNullOrEmpty (language))
				item = new DotNetProject (language);
			
			if (item == null) {
				if (string.IsNullOrEmpty (itemType))
					throw new InvalidOperationException ("Unknown solution item type.");
					
				DataType dt = MSBuildProjectService.DataContext.GetConfigurationDataType (itemType);
				if (dt == null)
					throw new InvalidOperationException ("Unknown solution item type: " + itemType);
					
				item = (SolutionItem) Activator.CreateInstance (dt.ValueType);
			}
			
			// Basic initialization
			
			if (item is DotNetProject) {
				DotNetProject p = (DotNetProject) item;
				p.TargetFramework = Services.ProjectService.DefaultTargetFramework;
			}
			return item;
		}
		
		void Load (IProgressMonitor monitor, MSBuildProject msproject)
		{
			MSBuildSerializer ser = CreateSerializer ();
			ser.SerializationContext.BaseFile = EntityItem.FileName;
			ser.SerializationContext.ProgressMonitor = monitor;
			
			MSBuildPropertyGroup globalGroup = msproject.GetGlobalPropertyGroup ();
			
			Item.SetItemHandler (this);
			
			DotNetProject dotNetProject = Item as DotNetProject;
			
			string assemblyName = null;
			string frameworkVersion = "2.0";
			
			// Read all items
			
			foreach (MSBuildItem buildItem in msproject.GetAllItems ()) {
				ProjectItem it = ReadItem (ser, buildItem);
				if (it != null)
					((SolutionEntityItem)Item).Items.Add (it);
			}
			
			if (dotNetProject != null) {
				// Get the common assembly name
				assemblyName = globalGroup.GetPropertyValue ("AssemblyName");
				frameworkVersion = globalGroup.GetPropertyValue ("TargetFrameworkVersion");
				if (frameworkVersion != null && frameworkVersion.StartsWith ("v"))
					frameworkVersion = frameworkVersion.Substring (1);
				if (!string.IsNullOrEmpty (frameworkVersion))
					dotNetProject.TargetFramework = Runtime.SystemAssemblyService.GetTargetFramework (frameworkVersion);
			}
			
			// Read configurations
			
			List<ConfigData> configData = GetConfigData (msproject);
			List<ConfigData> readConfigData = new List<ConfigData> ();
			
			foreach (ConfigData cgrp in configData) {
				readConfigData.Add (cgrp);

				string conf = cgrp.Config;
				string platform = cgrp.Platform;

				if (platform == Unspecified && conf != Unspecified && !ContainsSpecificPlatformConfiguration (configData, conf))
					platform = string.Empty;

				// It may be a partial configuration
				if (conf == Unspecified || platform == Unspecified)
					continue;
				
				MSBuildPropertyGroup grp = CreateMergedConfiguration (readConfigData, conf, platform);
				SolutionItemConfiguration config = EntityItem.CreateConfiguration (conf);
				
				if (config is DotNetProjectConfiguration) {
					// Clean the default assembly name
					((DotNetProjectConfiguration)config).OutputAssembly = string.Empty;
				}
				
				config.Platform = platform;
				DataItem data = ReadPropertyGroupMetadata (ser, grp, config);
				ser.Deserialize (config, data);
				EntityItem.Configurations.Add (config);
				
				if (config is DotNetProjectConfiguration) {
					DotNetProjectConfiguration dpc = (DotNetProjectConfiguration) config;
					if (dpc.CompilationParameters != null) {
						data = ReadPropertyGroupMetadata (ser, grp, dpc.CompilationParameters);
						ser.Deserialize (dpc.CompilationParameters, data);
					}
					
					if (!string.IsNullOrEmpty (assemblyName) && string.IsNullOrEmpty (dpc.OutputAssembly))
						dpc.OutputAssembly = assemblyName;
				}
			}
			
			// Read extended properties
			
			DataItem globalData = ReadPropertyGroupMetadata (ser, globalGroup, Item);
			
			string extendedData = msproject.GetProjectExtensions ("MonoDevelop");
			if (!string.IsNullOrEmpty (extendedData)) {
				StringReader sr = new StringReader (extendedData);
				DataItem data = (DataItem) XmlConfigurationReader.DefaultReader.Read (new XmlTextReader (sr));
				globalData.ItemData.AddRange (data.ItemData);
			}
			ser.Deserialize (Item, globalData);

			// Final initializations

			string fx = Item.ExtendedProperties ["InternalTargetFrameworkVersion"] as string;
			if (fx != null && dotNetProject != null) {
				dotNetProject.TargetFramework = Runtime.SystemAssemblyService.GetTargetFramework (fx);
				Item.ExtendedProperties.Remove ("InternalTargetFrameworkVersion");
			}
			
			Item.NeedsReload = false;
		}
		
		ProjectItem ReadItem (MSBuildSerializer ser, MSBuildItem buildItem)
		{
			Project project = Item as Project;
			DotNetProject dotNetProject = Item as DotNetProject;
			
			DataType dt = ser.DataContext.GetConfigurationDataType (buildItem.Name);
			
			if (project != null) {
				if (buildItem.Name == "Folder") {
					// Read folders
					string path = MSBuildProjectService.FromMSBuildPath (project.BaseDirectory, buildItem.Include);
					return new ProjectFile () { Name = Path.GetDirectoryName (path), Subtype = Subtype.Directory };
				}
				else if (buildItem.Name == "Reference" && dotNetProject != null) {
					ProjectReference pref;
					if (buildItem.HasMetadata ("HintPath")) {
						string path = MSBuildProjectService.FromMSBuildPath (dotNetProject.BaseDirectory, buildItem.GetMetadata ("HintPath"));
						if (File.Exists (path)) {
							pref = new ProjectReference (ReferenceType.Assembly, path);
							pref.LocalCopy = buildItem.GetMetadata ("Private") != "False";
						} else {
							pref = new ProjectReference (ReferenceType.Gac, buildItem.Include);
						}
					} else {
						string asm = buildItem.Include;
						// This is a workaround for a VS bug. Looks like it is writing this assembly incorrectly
						if (asm == "System.configuration")
							asm = "System.Configuration";
						else if (asm == "System.XML")
							asm = "System.Xml";
						pref = new ProjectReference (ReferenceType.Gac, asm);
					}
					pref.Condition = buildItem.Condition;
					pref.SpecificVersion = buildItem.GetMetadata ("SpecificVersion") != "False";
					ReadBuildItemMetadata (ser, buildItem, pref, typeof(ProjectReference));
					return pref;
				}
				else if (buildItem.Name == "ProjectReference" && dotNetProject != null) {
					string name = buildItem.GetMetadata ("Name");
					// The name of the project is the first word of the string (it may contain other stuff).
					int i = name.IndexOf (' ');
					if (i != -1)
						name = name.Substring (0, i);
					ProjectReference pref = new ProjectReference (ReferenceType.Project, name);
					pref.LocalCopy = buildItem.GetMetadata ("Private") != "False";
					pref.Condition = buildItem.Condition;
					return pref;
				}
				else if (dt == null && !string.IsNullOrEmpty (buildItem.Include)) {
					// Unknown item. Must be a file.
					if (!UnsupportedItems.Contains (buildItem.Name))
						return ReadProjectFile (ser, project, buildItem, typeof(ProjectFile));
				}
			}
			
			if (dt != null && typeof(ProjectItem).IsAssignableFrom (dt.ValueType)) {
				ProjectItem obj = (ProjectItem) Activator.CreateInstance (dt.ValueType);
				ReadBuildItemMetadata (ser, buildItem, obj, dt.ValueType);
				return obj;
			}
			
			return null;
		}
		
		class ConfigData
		{
			public ConfigData (string conf, string plt, MSBuildPropertyGroup grp)
			{
				Config = conf;
				Platform = plt;
				Group = grp;
			}
			
			public string Config;
			public string Platform;
			public MSBuildPropertyGroup Group;
		}

		MSBuildPropertyGroup CreateMergedConfiguration (List<ConfigData> configData, string conf, string platform)
		{
			MSBuildPropertyGroup merged = null;
			
			foreach (ConfigData grp in configData) {
				if ((grp.Config == conf || grp.Config == Unspecified) && (grp.Platform == platform || grp.Platform == Unspecified)) {
					if (merged == null)
						merged = grp.Group;
					else
						merged = MSBuildPropertyGroup.Merge (merged, grp.Group);
				}
			}
			return merged;
		}

		bool ContainsSpecificPlatformConfiguration (List<ConfigData> configData, string conf)
		{
			foreach (ConfigData grp in configData) {
				if (grp.Config == conf && grp.Platform != Unspecified)
					return true;
			}
			return false;
		}

		public override void Save (MonoDevelop.Core.IProgressMonitor monitor)
		{
			if (Item is UnknownProject || Item is UnknownSolutionItem)
				return;
			
			bool newProject = false;
			SolutionEntityItem eitem = EntityItem;
			
			MSBuildSerializer ser = CreateSerializer ();
			ser.SerializationContext.BaseFile = eitem.FileName;
			ser.SerializationContext.ProgressMonitor = monitor;
			
			DotNetProject dotNetProject = Item as DotNetProject;
			
			MSBuildProject msproject = new MSBuildProject ();
			if (fileContent != null) {
				msproject.LoadXml (fileContent);
			} else {
				msproject.DefaultTargets = "Build";
				newProject = true;
			}

			// Global properties
			
			MSBuildPropertyGroup globalGroup = msproject.GetGlobalPropertyGroup ();
			if (globalGroup == null) {
				globalGroup = msproject.AddNewPropertyGroup (false);
			}
			
			if (eitem.Configurations.Count > 0) {
				ItemConfiguration conf = eitem.Configurations ["Debug"];
				if (conf == null) conf = eitem.Configurations [0];
				MSBuildProperty bprop = SetGroupProperty (globalGroup, "Configuration", conf.Name, false);
				bprop.Condition = " '$(Configuration)' == '' ";
				
				string platform = conf.Platform.Length == 0 ? "AnyCPU" : conf.Platform;
				bprop = SetGroupProperty (globalGroup, "Platform", platform, false);
				bprop.Condition = " '$(Platform)' == '' ";
			}
			
			if (TypeGuid == MSBuildProjectService.GenericItemGuid) {
				DataType dt = MSBuildProjectService.DataContext.GetConfigurationDataType (Item.GetType ());
				SetGroupProperty (globalGroup, "ItemType", dt.Name, false);
			}

			Item.ExtendedProperties ["ProjectGuid"] = Item.ItemId;
			if (subtypeGuids.Count > 0) {
				string gg = "";
				foreach (string sg in subtypeGuids) {
					if (gg.Length > 0)
						gg += ";";
					gg += sg;
				}
				gg += ";" + TypeGuid;
				Item.ExtendedProperties ["ProjectTypeGuids"] = gg.ToUpper ();
			}
			else
				Item.ExtendedProperties.Remove ("ProjectTypeGuids");

			string productVersion = (string) Item.ExtendedProperties ["ProductVersion"];
			if (productVersion == null) {
				Item.ExtendedProperties ["ProductVersion"] = TargetFormat.ProductVersion;
				productVersion = TargetFormat.ProductVersion;
			}

			Item.ExtendedProperties ["SchemaVersion"] = "2.0";
			
			if (TargetFormat.ToolsVersion != "2.0")
				msproject.ToolsVersion = TargetFormat.ToolsVersion;
			else
				msproject.ToolsVersion = string.Empty;

			
			string targetFramework = null;
			IList supportedFrameworks = TargetFormat.FrameworkVersions;
			
			if (dotNetProject != null) {

				targetFramework = dotNetProject.TargetFramework.Id;

				// If the file format does not support this framework version, store the highest version
				// supported in the TargetFrameworkVersion property and the real one in the extended properties
				if (!supportedFrameworks.Contains (targetFramework)) {
					dotNetProject.ExtendedProperties ["InternalTargetFrameworkVersion"] = targetFramework;
					targetFramework = FindClosestSupportedVersion (targetFramework);
				} else
					dotNetProject.ExtendedProperties.Remove ("InternalTargetFrameworkVersion");
			}

			// This serialize call will write data to ser.InternalItemProperties and ser.ExternalItemProperties
			ser.Serialize (Item, Item.GetType ());
			
			if (fileContent == null)
				ser.InternalItemProperties.ItemData.Sort (globalConfigOrder);
			
			WritePropertyGroupMetadata (globalGroup, ser.InternalItemProperties.ItemData, ser, Item);
			
			// Find a common assembly name for all configurations
			
			string assemblyName = null;
			
			foreach (SolutionItemConfiguration conf in eitem.Configurations) {
				DotNetProjectConfiguration cp = conf as MonoDevelop.Projects.DotNetProjectConfiguration;
				if (cp != null) {
					if (assemblyName == null)
						assemblyName = cp.OutputAssembly;
					else if (assemblyName != cp.OutputAssembly)
						assemblyName = string.Empty;
					
					if (newProject)
						cp.ExtendedProperties ["ErrorReport"] = "prompt";
					
					string debugType = (string) cp.ExtendedProperties ["DebugType"];
					if (cp.DebugMode) {
						if (debugType != "full" && debugType != "pdbonly")
							cp.ExtendedProperties ["DebugType"] = "full";
					}
					else if (debugType != "none" && debugType != "pdbonly")
						cp.ExtendedProperties ["DebugType"] = "none";
				}
			}
			
			if (!string.IsNullOrEmpty (assemblyName))
				SetGroupProperty (globalGroup, "AssemblyName", assemblyName, false);
			else
				globalGroup.RemoveProperty ("AssemblyName");
			
			// Configurations

			List<ConfigData> configData = GetConfigData (msproject);
			
			foreach (SolutionItemConfiguration conf in eitem.Configurations) {
				bool newConf = false;
				MSBuildPropertyGroup propGroup = FindPropertyGroup (configData, conf);
				if (propGroup == null) {
					propGroup = msproject.AddNewPropertyGroup (false);
					propGroup.Condition = BuildConfigCondition (conf.Name, conf.Platform);
					newConf = true;
				}
				
				DotNetProjectConfiguration netConfig = conf as DotNetProjectConfiguration;
				
				DataItem ditem = (DataItem) ser.Serialize (conf);
				
				if (netConfig != null) {
					// Remove all compilation parameters properties from the data item, since we are going to write them again.
					ClassDataType dt = (ClassDataType) ser.DataContext.GetConfigurationDataType (netConfig.CompilationParameters.GetType ());
					foreach (ItemProperty prop in dt.GetProperties (ser.SerializationContext, netConfig.CompilationParameters)) {
						DataNode n = ditem.ItemData [prop.Name];
						if (n != null)
							ditem.ItemData.Remove (n);
					}
					DataItem ditemComp = (DataItem) ser.Serialize (netConfig.CompilationParameters);
					ditem.ItemData.AddRange (ditemComp.ItemData);
				}

				if (newConf)
					ditem.ItemData.Sort (configOrder);
				
				WritePropertyGroupMetadata (propGroup, ditem.ItemData, ser, conf, netConfig != null ? netConfig.CompilationParameters : null);
				
				if (!string.IsNullOrEmpty (assemblyName))
					propGroup.RemoveProperty ("AssemblyName");

				UnmergeBaseConfiguration (configData, propGroup, conf.Name, conf.Platform);
			}
			
			// Remove old items
			ArrayList list = new ArrayList ();
			foreach (object ob in msproject.GetAllItems ())
				list.Add (ob);
			
			// Add the new items
			foreach (object ob in ((SolutionEntityItem)Item).Items)
				SaveItem (monitor, ser, msproject, ob);
			
			foreach (MSBuildItem buildItem in list)
				msproject.RemoveItem (buildItem);
		
			if (dotNetProject != null) {

				// If the format only supports one fx version, there is no need to store it
				if (supportedFrameworks.Count > 1)
					SetGroupProperty (globalGroup, "TargetFrameworkVersion", "v" + targetFramework, false);
				else
					globalGroup.RemoveProperty ("TargetFrameworkVersion");
				
			}

			if (newProject) {
				foreach (string import in TargetImports)
					msproject.AddNewImport (import, null);
			}
			
			DataItem extendedData = ser.ExternalItemProperties;
			if (extendedData.HasItemData) {
				extendedData.Name = "Properties";
				StringWriter sw = new StringWriter ();
				XmlConfigurationWriter.DefaultWriter.Write (new XmlTextWriter (sw), extendedData);
				msproject.SetProjectExtensions ("MonoDevelop", sw.ToString ());
			} else
				msproject.RemoveProjectExtensions ("MonoDevelop");
			
			msproject.Save (eitem.FileName);
		}
		
		void SaveItem (MonoDevelop.Core.IProgressMonitor monitor, MSBuildSerializer ser, MSBuildProject msproject, object ob)
		{
			if (ob is ProjectReference) {
				SaveReference (monitor, ser, msproject, (ProjectReference) ob);
			}
			else if (ob is ProjectFile) {
				SaveProjectFile (ser, msproject, (ProjectFile) ob);
			}
			else {
				DataType dt = ser.DataContext.GetConfigurationDataType (ob.GetType ());
				string itemName = dt.Name;
				MSBuildItem buildItem = msproject.AddNewItem (itemName, "");
				WriteBuildItemMetadata (ser, buildItem, ob);
			}
		}
		
		void SaveProjectFile (MSBuildSerializer ser, MSBuildProject msproject, ProjectFile file)
		{
			string itemName = (file.Subtype == Subtype.Directory)? "Folder" : file.BuildAction;

			string path = MSBuildProjectService.ToMSBuildPath (Item.BaseDirectory, file.FilePath);
			if (path.Length == 0)
				return;
			
			//directory paths must end with '/'
			if ((file.Subtype == Subtype.Directory) && path[path.Length-1] != '/')
				path = path + "/";
			
			MSBuildItem buildItem = msproject.AddNewItem (itemName, path);
			WriteBuildItemMetadata (ser, buildItem, file);
			
			if (!string.IsNullOrEmpty (file.DependsOn))
				buildItem.SetMetadata ("DependentUpon", MSBuildProjectService.ToMSBuildPath (Path.GetDirectoryName (file.FilePath), file.DependsOn));
			if (!string.IsNullOrEmpty (file.ContentType))
				buildItem.SetMetadata ("SubType", file.ContentType);
			
			if (!string.IsNullOrEmpty (file.Generator))
				buildItem.SetMetadata ("Generator", file.Generator);
			else
				buildItem.UnsetMetadata ("Generator");
			
			buildItem.Condition = file.Condition;
			
			if (file.CopyToOutputDirectory == FileCopyMode.None) {
				buildItem.UnsetMetadata ("CopyToOutputDirectory");
			} else {
				buildItem.SetMetadata ("CopyToOutputDirectory", file.CopyToOutputDirectory.ToString ());
			}
			
			if (!file.Visible) {
				buildItem.SetMetadata ("Visible", "False");
			} else {
				buildItem.UnsetMetadata ("Visible");
			}
			
			if (file.BuildAction == BuildAction.EmbeddedResource) {
				//Emit LogicalName only when it does not match the default Id
				if (GetDefaultResourceId (file) != file.ResourceId)
					buildItem.SetMetadata ("LogicalName", file.ResourceId);
			}
		}
		
		void SaveReference (MonoDevelop.Core.IProgressMonitor monitor, MSBuildSerializer ser, MSBuildProject msproject, ProjectReference pref)
		{
			MSBuildItem buildItem;
			if (pref.ReferenceType == ReferenceType.Assembly) {
				string asm = null;
				if (File.Exists (pref.Reference)) {
					try {
						asm = AssemblyName.GetAssemblyName (pref.Reference).FullName;
					} catch (Exception ex) {
						string msg = string.Format ("Could not get full name for assembly '{0}'.", pref.Reference);
						monitor.ReportWarning (msg);
						LoggingService.LogError (msg, ex);
					}
				}
				if (asm == null)
					asm = Path.GetFileNameWithoutExtension (pref.Reference);
				buildItem = msproject.AddNewItem ("Reference", asm);
				if (!pref.SpecificVersion)
					buildItem.SetMetadata ("SpecificVersion", "False");
				buildItem.SetMetadata ("HintPath", MSBuildProjectService.ToMSBuildPath (Item.BaseDirectory, pref.Reference));
				if (!pref.LocalCopy)
					buildItem.SetMetadata ("Private", "False");
			}
			else if (pref.ReferenceType == ReferenceType.Gac) {
				string include = pref.StoredReference;
				SystemPackage pkg = pref.Package;
				if (pkg != null && pkg.IsFrameworkPackage) {
					int i = include.IndexOf (',');
					if (i != -1)
						include = include.Substring (0, i).Trim ();
				}
				buildItem = msproject.AddNewItem ("Reference", include);
				if (!pref.SpecificVersion)
					buildItem.SetMetadata ("SpecificVersion", "False");
				IList supportedFrameworks = TargetFormat.FrameworkVersions;
				if (pkg != null && pkg.IsFrameworkPackage && supportedFrameworks.Contains (pkg.TargetFramework) && pkg.TargetFramework != "2.0" && supportedFrameworks.Count > 1) {
					TargetFramework fx = Runtime.SystemAssemblyService.GetTargetFramework (pkg.TargetFramework);
					buildItem.SetMetadata ("RequiredTargetFramework", fx.Id);
				}
			}
			else if (pref.ReferenceType == ReferenceType.Project) {
				Project refProj = Item.ParentSolution.FindProjectByName (pref.Reference);
				if (refProj != null) {
					buildItem = msproject.AddNewItem ("ProjectReference", MSBuildProjectService.ToMSBuildPath (Item.BaseDirectory, refProj.FileName));
					MSBuildProjectHandler handler = refProj.ItemHandler as MSBuildProjectHandler;
					if (handler != null)
						buildItem.SetMetadata ("Project", handler.Item.ItemId);
					buildItem.SetMetadata ("Name", refProj.Name);
					if (!pref.LocalCopy)
						buildItem.SetMetadata ("Private", "False");
				} else {
					monitor.ReportWarning (GettextCatalog.GetString ("Reference to unknown project '{0}' ignored.", pref.Reference));
					return;
				}
			}
			else {
				// Custom
				DataType dt = ser.DataContext.GetConfigurationDataType (pref.GetType ());
				buildItem = msproject.AddNewItem (dt.Name, pref.Reference);
			}
			WriteBuildItemMetadata (ser, buildItem, pref);
			buildItem.Condition = pref.Condition;
		}

		void UnmergeBaseConfiguration (List<ConfigData> configData, MSBuildPropertyGroup propGroup, string conf, string platform)
		{
			MSBuildPropertyGroup baseGroup = null;
			
			foreach (ConfigData data in configData) {
				if (data.Group == propGroup)
					break;
				if ((data.Config == conf || data.Config == Unspecified) && (data.Platform == platform || data.Platform == Unspecified)) {
					if (baseGroup == null)
						baseGroup = data.Group;
					else
						baseGroup = MSBuildPropertyGroup.Merge (baseGroup, data.Group);
				}
			}
			if (baseGroup != null)
				propGroup.UnMerge (baseGroup);
		}
		
		void ReadBuildItemMetadata (DataSerializer ser, MSBuildItem buildItem, object dataItem, Type extendedType)
		{
			DataItem ditem = new DataItem ();
			foreach (ItemProperty prop in ser.GetProperties (dataItem)) {
				if (prop.Name == "Include")
					ditem.ItemData.Add (new DataValue ("Include", buildItem.Include));
				else if (buildItem.HasMetadata (prop.Name)) {
					string data = buildItem.GetMetadata (prop.Name);
					ditem.ItemData.Add (GetDataNode (prop, data));
				}
			}
			ConvertFromMsbuildFormat (ditem);
			ser.Deserialize (dataItem, ditem);
		}
		
		void WriteBuildItemMetadata (DataSerializer ser, MSBuildItem buildItem, object dataItem)
		{
			DataItem ditem = (DataItem) ser.Serialize (dataItem, dataItem.GetType ());
			if (ditem.HasItemData) {
				foreach (DataNode node in ditem.ItemData) {
					if (node.Name == "Include" && node is DataValue)
						buildItem.Include = ((DataValue) node).Value;
					else {
						ConvertToMsbuildFormat (node);
						buildItem.SetMetadata (node.Name, GetXmlString (node), node is DataItem);
					}
				}
			}
		}
		
		DataItem ReadPropertyGroupMetadata (DataSerializer ser, MSBuildPropertyGroup propGroup, object dataItem)
		{
			DataItem ditem = new DataItem ();

			foreach (MSBuildProperty bprop in propGroup.Properties) {
				DataNode node = null;
				foreach (XmlNode xnode in bprop.Element.ChildNodes) {
					if (xnode is XmlElement) {
						node = XmlConfigurationReader.DefaultReader.Read ((XmlElement)xnode);
						break;
					}
				}
				if (node == null)
					node = new DataValue (bprop.Name, bprop.Value);
				
				ConvertFromMsbuildFormat (node);
				ditem.ItemData.Add (node);
			}
			
			return ditem;
		}
		
		void WritePropertyGroupMetadata (MSBuildPropertyGroup propGroup, DataCollection itemData, MSBuildSerializer ser, params object[] itemsToReplace)
		{
			var notWrittenProps = new HashSet<string> ();
			
			foreach (object ob in itemsToReplace) {
				if (ob == null)
					continue;
				ClassDataType dt = (ClassDataType) ser.DataContext.GetConfigurationDataType (ob.GetType ());
				foreach (ItemProperty prop in dt.GetProperties (ser.SerializationContext, ob))
					notWrittenProps.Add (prop.Name);
			}
	
			foreach (DataNode node in itemData) {
				notWrittenProps.Remove (node.Name);
				ConvertToMsbuildFormat (node);
				SetGroupProperty (propGroup, node.Name, GetXmlString (node), node is DataItem);
			}
			foreach (string prop in notWrittenProps)
				propGroup.RemoveProperty (prop);
		}

		void ConvertToMsbuildFormat (DataNode node)
		{
			ReplaceChar (node, true, '.', '-');
		}
		
		void ConvertFromMsbuildFormat (DataNode node)
		{
			ReplaceChar (node, true, '-', '.');
		}
		
		void ReplaceChar (DataNode node, bool force, char oldChar, char newChar)
		{
			DataItem it = node as DataItem;
			if ((force || it != null) && node.Name != null)
				node.Name = node.Name.Replace (oldChar, newChar);
			if (it != null) {
				foreach (DataNode cnode in it.ItemData)
					ReplaceChar (cnode, !it.UniqueNames, oldChar, newChar);
			}
		}

		List<ConfigData> GetConfigData (MSBuildProject msproject)
		{
			List<ConfigData> configData = new List<ConfigData> ();
			foreach (MSBuildPropertyGroup cgrp in msproject.PropertyGroups) {
				string conf, platform;
				if (ParseConfigCondition (cgrp.Condition, out conf, out platform))
					configData.Add (new ConfigData (conf, platform, cgrp));
			}
			return configData;
		}
		
		MSBuildProperty SetGroupProperty (MSBuildPropertyGroup propGroup, string name, string value, bool isLiteral)
		{
			propGroup.SetPropertyValue (name, value);
			return propGroup.GetProperty (name);
		}
		
		MSBuildPropertyGroup FindPropertyGroup (List<ConfigData> configData, SolutionItemConfiguration config)
		{
			foreach (ConfigData data in configData) {
				if (data.Config == config.Name && data.Platform == config.Platform)
					return data.Group;
			}
			return null;
		}
		
		ProjectFile ReadProjectFile (DataSerializer ser, Project project, MSBuildItem buildItem, Type type)
		{
			string path = MSBuildProjectService.FromMSBuildPath (project.BaseDirectory, buildItem.Include);
			ProjectFile file = (ProjectFile) Activator.CreateInstance (type);
			file.Name = path;
			file.BuildAction = buildItem.Name;
			
			ReadBuildItemMetadata (ser, buildItem, file, type);
			
			string dependentFile = buildItem.GetMetadata ("DependentUpon");
			if (!string.IsNullOrEmpty (dependentFile)) {
				dependentFile = MSBuildProjectService.FromMSBuildPath (Path.GetDirectoryName (path), dependentFile);
				file.DependsOn = dependentFile;
			}
			
			string copyToOutputDirectory = buildItem.GetMetadata ("CopyToOutputDirectory");
			if (!string.IsNullOrEmpty (copyToOutputDirectory)) {
				switch (copyToOutputDirectory) {
				case "None": break;
				case "Always": file.CopyToOutputDirectory = FileCopyMode.Always; break;
				case "PreserveNewest": file.CopyToOutputDirectory = FileCopyMode.PreserveNewest; break;
				default:
					MonoDevelop.Core.LoggingService.LogWarning (
						"Unrecognised value {0} for CopyToOutputDirectory MSBuild property",
						copyToOutputDirectory);
					break;
				}
			}
			
			if (buildItem.GetMetadata ("Visible") == "False")
				file.Visible = false;
				
			
			string resourceId = buildItem.GetMetadata ("LogicalName");
			if (!string.IsNullOrEmpty (resourceId))
				file.ResourceId = resourceId;
			
			string contentType = buildItem.GetMetadata ("SubType");
			if (!string.IsNullOrEmpty (contentType))
				file.ContentType = contentType;
			
			string generator = buildItem.GetMetadata ("Generator");
			if (!string.IsNullOrEmpty (generator))
				file.Generator = generator;
			
			file.Condition = buildItem.Condition;
			return file;
		}

		bool ParseConfigCondition (string cond, out string config, out string platform)
		{
			config = platform = null;
			int i = cond.IndexOf ("==");
			if (i == -1)
				return false;
			if (cond.Substring (0, i).Trim () == "'$(Configuration)|$(Platform)'") {
				cond = cond.Substring (i+2).Trim (' ','\'');
				i = cond.IndexOf ('|');
				config = cond.Substring (0, i);
				platform = cond.Substring (i+1);
				if (platform == "AnyCPU")
					platform = string.Empty;
				return true;
			}
			else if (cond.Substring (0, i).Trim () == "'$(Configuration)'") {
				config = cond.Substring (i+2).Trim (' ','\'');
				platform = Unspecified;
				return true;
			}
			else if (cond.Substring (0, i).Trim () == "'$(Platform)'") {
				config = Unspecified;
				platform = cond.Substring (i+2).Trim (' ','\'');
				if (platform == "AnyCPU")
					platform = string.Empty;
				return true;
			}
			return false;
		}
		
		string BuildConfigCondition (string config, string platform)
		{
			if (platform.Length == 0)
				platform = "AnyCPU";
			return " '$(Configuration)|$(Platform)' == '" + config + "|" + platform + "' ";
		}

		string FindClosestSupportedVersion (string version)
		{
			foreach (string supv in TargetFormat.FrameworkVersions) {
				TargetFramework fx = Runtime.SystemAssemblyService.GetTargetFramework (supv);
				if (fx.IsCompatibleWithFramework (version))
					return fx.Id;
			}
			return TargetFormat.FrameworkVersions [TargetFormat.FrameworkVersions.Length - 1];
		}
		
		string GetXmlString (DataNode node)
		{
			if (node is DataValue)
				return ((DataValue)node).Value;
			else {
				StringWriter sw = new StringWriter ();
				XmlTextWriter xw = new XmlTextWriter (sw);
				XmlConfigurationWriter.DefaultWriter.Write (xw, node);
				return sw.ToString ();
			}
		}
		
		DataNode GetDataNode (ItemProperty prop, string xmlString)
		{
			if (prop.DataType.IsSimpleType)
				return new DataValue (prop.Name, xmlString);
			else {
				StringReader sr = new StringReader (xmlString);
				return XmlConfigurationReader.DefaultReader.Read (new XmlTextReader (sr));
			}
		}
		
		internal virtual MSBuildSerializer CreateSerializer ()
		{
			return new MSBuildSerializer (EntityItem.FileName);
		}
		
		static readonly MSBuildElementOrder globalConfigOrder = new MSBuildElementOrder (
			"Configuration","Platform","ProductVersion","SchemaVersion","ProjectGuid","ProjectTypeGuids", "OutputType",
		    "AppDesignerFolder","RootNamespace","AssemblyName","StartupObject"
		);
		static readonly MSBuildElementOrder configOrder = new MSBuildElementOrder (
			"DebugSymbols","DebugType","Optimize","OutputPath","DefineConstants","ErrorReport","WarningLevel",
		    "TreatWarningsAsErrors","DocumentationFile"
		);

		// Those are properties which are dynamically set by this file format
		
		internal static readonly ItemMember[] ExtendedMSBuildProperties = new ItemMember [] {
			new ItemMember (typeof(Project), "ProductVersion"),
			new ItemMember (typeof(Project), "SchemaVersion"),
			new ItemMember (typeof(Project), "ProjectGuid"),
			new ItemMember (typeof(Project), "ProjectTypeGuids"),
			new ItemMember (typeof(DotNetProjectConfiguration), "DebugType"),
			new ItemMember (typeof(DotNetProjectConfiguration), "ErrorReport"),
			new ItemMember (typeof(DotNetProjectConfiguration), "TargetFrameworkVersion"),
			new ItemMember (typeof(ProjectReference), "RequiredTargetFramework"),
			new ItemMember (typeof(Project), "InternalTargetFrameworkVersion", true),
		};
		
		// Items generated by VS but which MD is not using and should be ignored
		
		internal static readonly IList<string> UnsupportedItems = new string[] {
			"BootstrapperFile", "AppDesigner"
		};
	}
	
	class MSBuildSerializer: DataSerializer
	{
		public DataItem InternalItemProperties = new DataItem ();
		public DataItem ExternalItemProperties = new DataItem ();
		
		public MSBuildSerializer (string baseFile): base (MSBuildProjectService.DataContext)
		{
			// Use windows separators
			SerializationContext.BaseFile = baseFile;
			SerializationContext.DirectorySeparatorChar = '\\';
		}
		
		protected override bool CanHandleProperty (ItemProperty prop, SerializationContext serCtx, object instance)
		{
			if (instance is Project) {
				if (prop.Name == "Contents")
					return false;
			}
			if (instance is DotNetProject) {
				if (prop.Name == "References")
					return false;
			}
			if (instance is SolutionEntityItem) {
				if (prop.Name == "Policies")
					return true;
				return prop.IsExtendedProperty (typeof(SolutionEntityItem));
			}
			if (instance is ProjectFile)
				return prop.IsExtendedProperty (typeof(ProjectFile));
			if (instance is ProjectReference)
				return prop.IsExtendedProperty (typeof(ProjectReference)) || prop.Name == "Package";
			if (instance is DotNetProjectConfiguration)
				if (prop.Name == "CodeGeneration")
					return false;
			return true;
		}
		
		protected override DataNode OnSerializeProperty (ItemProperty prop, SerializationContext serCtx, object instance, object value)
		{
			DataNode data = base.OnSerializeProperty (prop, serCtx, instance, value);
			if (instance is SolutionEntityItem && data != null) {
				if (prop.IsExternal)
					ExternalItemProperties.ItemData.Add (data);
				else
					InternalItemProperties.ItemData.Add (data);
			}
			return data;
		}
	}
	
	class MSBuildElementOrder: Dictionary<string, int>
	{
		public MSBuildElementOrder (params string[] elements)
		{
			for (int n=0; n<elements.Length; n++)
				this [elements [n]] = n;
		}
	}
}
