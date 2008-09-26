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

		public IResourceHandler CustomResourceHandler {
			get {
				return customResourceHandler;
			}
			set {
				customResourceHandler = value;
			}
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
			if (customResourceHandler != null) {
				string res = customResourceHandler.GetDefaultResourceId (file);
				if (!string.IsNullOrEmpty (res))
					return res;
			}
			return MSBuildProjectService.GetDefaultResourceId (file);
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
				p.ClrVersion = ClrVersion.Net_2_0;
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
			
			// Read files
			
			Project project = Item as Project;
			if (project != null) {
				foreach (MSBuildItem buildItem in msproject.GetAllItems ("Compile"))
					AddFile (ser, project, buildItem, BuildAction.Compile);
				foreach (MSBuildItem buildItem in msproject.GetAllItems ("EmbeddedResource"))
					AddFile (ser, project, buildItem, BuildAction.EmbedAsResource);
				foreach (MSBuildItem buildItem in msproject.GetAllItems ("Content"))
					AddFile (ser, project, buildItem, BuildAction.Exclude);
				foreach (MSBuildItem buildItem in msproject.GetAllItems ("None"))
					AddFile (ser, project, buildItem, BuildAction.Nothing);
			}
			
			string assemblyName = null;
			string frameworkVersion = "v2.0";
			
			// Read project references
			
			DotNetProject dotNetProject = Item as DotNetProject;
			if (dotNetProject != null) {
				foreach (MSBuildItem buildItem in msproject.GetAllItems ("Reference")) {
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
						pref = new ProjectReference (ReferenceType.Gac, buildItem.Include);
					}
					ReadBuildItemMetadata (ser, buildItem, pref, typeof(ProjectReference));
					dotNetProject.References.Add (pref);
				}
				foreach (MSBuildItem buildItem in msproject.GetAllItems ("ProjectReference")) {
					ProjectReference pref = new ProjectReference (ReferenceType.Project, buildItem.GetMetadata ("Name"));
					pref.LocalCopy = buildItem.GetMetadata ("Private") != "False";
					dotNetProject.References.Add (pref);
				}
				
				// Get the common assembly name
				assemblyName = globalGroup.GetPropertyValue ("AssemblyName");
				frameworkVersion = globalGroup.GetPropertyValue ("TargetFrameworkVersion");
			}
			
			// Read configurations
			
			string conf, platform;
			foreach (MSBuildPropertyGroup grp in msproject.PropertyGroups) {
				if (ParseConfigCondition (grp.Condition, out conf, out platform)) {
					SolutionItemConfiguration config = EntityItem.CreateConfiguration (conf);
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
						
						if (!string.IsNullOrEmpty (assemblyName))
							dpc.OutputAssembly = assemblyName;
						
						string fw = (string) dpc.ExtendedProperties ["TargetFrameworkVersion"];
						if (fw == null)
							fw = frameworkVersion;
						dpc.ClrVersion = GetClrVersion (fw);
					}
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
			
			Item.NeedsReload = false;
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
			
			ser.Serialize (Item, Item.GetType ());
			ser.InternalItemProperties.ItemData.Sort (globalConfigOrder);
			foreach (DataNode node in ser.InternalItemProperties.ItemData)
				SetGroupProperty (globalGroup, node.Name, GetXmlString (node), node is DataItem);
			
			// Find a common assembly name for all configurations
			
			string assemblyName = null;
			string clrVersion = null;
			
			foreach (SolutionItemConfiguration conf in eitem.Configurations) {
				DotNetProjectConfiguration cp = conf as MonoDevelop.Projects.DotNetProjectConfiguration;
				if (cp != null) {
					if (assemblyName == null)
						assemblyName = cp.OutputAssembly;
					else if (assemblyName != cp.OutputAssembly)
						assemblyName = string.Empty;
					
					if (clrVersion == null)
						clrVersion = GetFrameworkVersion (cp.ClrVersion);
					else if (clrVersion != GetFrameworkVersion (cp.ClrVersion))
						clrVersion = string.Empty;
					
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
			
			Item.ExtendedProperties ["SchemaVersion"] = "2.0";
			
			string productVersion = (string) Item.ExtendedProperties ["ProductVersion"];
			if (productVersion == null) {
				Item.ExtendedProperties ["ProductVersion"] = MSBuildFileFormatVS05.Version;
				productVersion = MSBuildFileFormatVS05.Version;
			}
			
			if (!string.IsNullOrEmpty (assemblyName))
				SetGroupProperty (globalGroup, "AssemblyName", assemblyName, false);
			
			if (!string.IsNullOrEmpty (clrVersion)) {
				// When using the VS05 format, only write the framework version if it is not 2.0
				if (productVersion != MSBuildFileFormatVS05.Version || clrVersion != "v2.0")
					SetGroupProperty (globalGroup, "TargetFrameworkVersion", clrVersion, false);
			}
			
			// Configurations
			
			foreach (SolutionItemConfiguration conf in eitem.Configurations) {
				MSBuildPropertyGroup propGroup = FindPropertyGroup (msproject, conf);
				if (propGroup == null) {
					propGroup = msproject.AddNewPropertyGroup (false);
					propGroup.Condition = BuildConfigCondition (conf.Name, conf.Platform);
				}
				
				DotNetProjectConfiguration netConfig = conf as DotNetProjectConfiguration;
				if (netConfig != null) {
					if (string.IsNullOrEmpty (clrVersion))
						netConfig.ExtendedProperties ["TargetFrameworkVersion"] = GetFrameworkVersion (netConfig.ClrVersion);
					else
						netConfig.ExtendedProperties.Remove ("TargetFrameworkVersion");
				}
				
				DataItem ditem = (DataItem) ser.Serialize (conf);
				
				if (netConfig != null) {
					DataItem ditemComp = (DataItem) ser.Serialize (netConfig.CompilationParameters);
					ditem.ItemData.AddRange (ditemComp.ItemData);
				}
				
				ditem.ItemData.Sort (configOrder);
				WritePropertyGroupMetadata (propGroup, ditem);
				
				if (!string.IsNullOrEmpty (assemblyName))
					propGroup.RemoveProperty ("AssemblyName");
			}
			
			Project project = Item as Project;
			DotNetProject dotNetProject = Item as DotNetProject;
			
			if (dotNetProject != null) {
				// Remove all references and add the new ones
				
				MSBuildItemGroup refgrp = null;
				MSBuildItemGroup prefgrp = null;
					
				ArrayList list = new ArrayList ();
				foreach (object ob in msproject.GetAllItems ("Reference", "ProjectReference"))
					list.Add (ob);
				
				foreach (ProjectReference pref in dotNetProject.References) {
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
						if (refgrp == null)
							refgrp = FindItemGroup (msproject, "Reference");
						if (asm == null)
							asm = Path.GetFileNameWithoutExtension (pref.Reference);
						buildItem = refgrp.AddNewItem ("Reference", asm);
						buildItem.SetMetadata ("SpecificVersion", "False");
						buildItem.SetMetadata ("HintPath", MSBuildProjectService.ToMSBuildPath (project.BaseDirectory, pref.Reference));
						if (!pref.LocalCopy)
							buildItem.SetMetadata ("Private", "False");
					}
					else if (pref.ReferenceType == ReferenceType.Gac) {
						string include = pref.Reference;
						SystemPackage sp = Runtime.SystemAssemblyService.GetPackageFromFullName (include);
						if (sp != null && sp.IsCorePackage) {
							int i = include.IndexOf (',');
							include = include.Substring (0, i).Trim ();
						}
						if (refgrp == null)
							refgrp = FindItemGroup (msproject, "Reference");
						buildItem = refgrp.AddNewItem ("Reference", include);
					}
					else if (pref.ReferenceType == ReferenceType.Project) {
						Project refProj = project.ParentSolution.FindProjectByName (pref.Reference);
						if (refProj != null) {
							if (prefgrp == null)
								prefgrp = FindItemGroup (msproject, "ProjectReference");
							buildItem = prefgrp.AddNewItem ("ProjectReference", MSBuildProjectService.ToMSBuildPath (project.BaseDirectory, refProj.FileName));
							MSBuildProjectHandler handler = refProj.ItemHandler as MSBuildProjectHandler;
							if (handler != null)
								buildItem.SetMetadata ("Project", handler.Item.ItemId);
							buildItem.SetMetadata ("Name", refProj.Name);
							if (!pref.LocalCopy)
								buildItem.SetMetadata ("Private", "False");
						} else {
							monitor.ReportWarning (GettextCatalog.GetString ("Reference to unknown project '{0}' ignored.", pref.Reference));
							continue;
						}
					}
					else {
						buildItem = msproject.AddNewItem ("CustomReference", pref.Reference);
					}
					WriteBuildItemMetadata (ser, buildItem, pref);
				}
				
				foreach (MSBuildItem buildItem in list)
					msproject.RemoveItem (buildItem);
			}
			
			if (project != null) {
				
				// Remove all files and add the new ones
				
				ArrayList list = new ArrayList ();
				foreach (object ob in msproject.GetAllItems ("Compile","EmbeddedResource","Content","None"))
					list.Add (ob);

				Hashtable grps = new Hashtable ();
				foreach (ProjectFile file in project.Files) {
					if (file.Subtype == Subtype.Directory)
						continue;
					string itemName;
					switch (file.BuildAction) {
						case BuildAction.Compile: itemName = "Compile"; break;
						case BuildAction.EmbedAsResource: itemName = "EmbeddedResource"; break;
						case BuildAction.FileCopy:
						case BuildAction.Exclude: itemName = "Content"; break;
						case BuildAction.Nothing: itemName = "None"; break;
						default: continue; // Ignore
					}
					MSBuildItemGroup fgrp = (MSBuildItemGroup) grps [itemName];
					if (fgrp == null) {
						fgrp = FindItemGroup (msproject, itemName);
						grps [itemName] = fgrp;
					}
					MSBuildItem buildItem = fgrp.AddNewItem (itemName, MSBuildProjectService.ToMSBuildPath (project.BaseDirectory, file.FilePath));
					WriteBuildItemMetadata (ser, buildItem, file);

					if (!string.IsNullOrEmpty (file.DependsOn))
						buildItem.SetMetadata ("DependentUpon", MSBuildProjectService.ToMSBuildPath (Path.GetDirectoryName (file.FilePath), file.DependsOn));
					if (!string.IsNullOrEmpty (file.ContentType))
						buildItem.SetMetadata ("SubType", file.ContentType);
					
					if (file.BuildAction == BuildAction.FileCopy) {
						buildItem.SetMetadata ("CopyToOutputDirectory", "Always");
					}
					else if (file.BuildAction == BuildAction.EmbedAsResource) {
						//Emit LogicalName if we are writing elements for a Non-MSBuidProject,
						//  (eg. when converting a gtk-sharp project, it might depend on non-vs
						//  style resource naming )
						//Or when the resourceId is different from the default one
						if (Services.ProjectService.GetDefaultResourceId (file) != file.ResourceId)
							buildItem.SetMetadata ("LogicalName", file.ResourceId);
					}
				}
				
				foreach (MSBuildItem buildItem in list)
					msproject.RemoveItem (buildItem);
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
			}
			
			msproject.Save (eitem.FileName);
		}
		
		void ReadBuildItemMetadata (DataSerializer ser, MSBuildItem buildItem, object dataItem, Type extendedType)
		{
			DataItem ditem = new DataItem ();
			foreach (ItemProperty prop in ser.GetProperties (dataItem)) {
				if (buildItem.HasMetadata (prop.Name)) {
					string data = buildItem.GetMetadata (prop.Name);
					ditem.ItemData.Add (GetDataNode (prop, data));
				}
			}
			ser.Deserialize (dataItem, ditem);
		}
		
		void WriteBuildItemMetadata (DataSerializer ser, MSBuildItem buildItem, object dataItem)
		{
			DataItem ditem = (DataItem) ser.Serialize (dataItem, dataItem.GetType ());
			if (ditem.HasItemData) {
				foreach (DataNode node in ditem.ItemData)
					buildItem.SetMetadata (node.Name, GetXmlString (node), node is DataItem);
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
				if (node != null)
					ditem.ItemData.Add (node);
				else 
					ditem.ItemData.Add (new DataValue (bprop.Name, bprop.Value));
			}
			
			return ditem;
		}
		
		void WritePropertyGroupMetadata (MSBuildPropertyGroup propGroup, DataItem ditem)
		{
			if (ditem.HasItemData) {
				foreach (DataNode node in ditem.ItemData)
					SetGroupProperty (propGroup, node.Name, GetXmlString (node), node is DataItem);
			}
		}
		
		MSBuildProperty SetGroupProperty (MSBuildPropertyGroup propGroup, string name, string value, bool isLiteral)
		{
			propGroup.SetPropertyValue (name, value);
			return propGroup.GetProperty (name);
		}
		
		MSBuildPropertyGroup FindPropertyGroup (MSBuildProject msproject, SolutionItemConfiguration config)
		{
			string conf, platform;
			foreach (MSBuildPropertyGroup grp in msproject.PropertyGroups) {
				if (ParseConfigCondition (grp.Condition, out conf, out platform)) {
					if (conf == config.Name && platform == config.Platform)
						return grp;
				}
			}
			return null;
		}
		
		MSBuildItemGroup FindItemGroup (MSBuildProject msproject, string itemName)
		{
			foreach (MSBuildItemGroup grp in msproject.ItemGroups) {
				foreach (MSBuildItem it in grp.Items) {
					if (it.Name == itemName)
						return grp;
				}
			}
			return msproject.AddNewItemGroup ();
		}
		
		void AddFile (DataSerializer ser, Project project, MSBuildItem buildItem, BuildAction buildAction)
		{
			if (buildAction == BuildAction.Exclude && buildItem.HasMetadata ("CopyToOutputDirectory")) {
				string copyMode = buildItem.GetMetadata("CopyToOutputDirectory");
				if (copyMode == "PreserveNewest" || copyMode == "Always")
					buildAction = BuildAction.FileCopy;
			}
			
			string path = MSBuildProjectService.FromMSBuildPath (project.BaseDirectory, buildItem.Include);
			ProjectFile file = new ProjectFile (path, buildAction);
			
			ReadBuildItemMetadata (ser, buildItem, file, typeof(ProjectFile));
			
			string dependentFile = buildItem.GetMetadata ("DependentUpon");
			if (!string.IsNullOrEmpty (dependentFile)) {
				dependentFile = MSBuildProjectService.FromMSBuildPath (Path.GetDirectoryName (path), dependentFile);
				file.DependsOn = dependentFile;
			}
			
			string resourceId = buildItem.GetMetadata ("LogicalName");
			if (!string.IsNullOrEmpty (resourceId))
				file.ResourceId = resourceId;
			
			string contentType = buildItem.GetMetadata ("SubType");
			if (!string.IsNullOrEmpty (contentType))
				file.ContentType = contentType;
			
			project.Files.Add (file);
		}

		bool ParseConfigCondition (string cond, out string config, out string platform)
		{
			config = platform = null;
			int i = cond.IndexOf ("==");
			if (i == -1)
				return false;
			if (cond.Substring (0, i).Trim () != "'$(Configuration)|$(Platform)'")
				return false;
			cond = cond.Substring (i+2).Trim (' ','\'');
			i = cond.IndexOf ('|');
			config = cond.Substring (0, i);
			platform = cond.Substring (i+1);
			if (platform == "AnyCPU")
				platform = string.Empty;
			return true;
		}
		
		string BuildConfigCondition (string config, string platform)
		{
			if (platform.Length == 0)
				platform = "AnyCPU";
			return " '$(Configuration)|$(Platform)' == '" + config + "|" + platform + "' ";
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
		
		ClrVersion GetClrVersion (string frameworkVersion)
		{
			switch (frameworkVersion) {
				case "v1.1": return ClrVersion.Net_1_1;
				case "v2.0": return ClrVersion.Net_2_0;
				case "v2.1": return ClrVersion.Clr_2_1;
				case "v3.0": return ClrVersion.Net_2_0;
				case "v3.5": return ClrVersion.Net_2_0;
			}
			return ClrVersion.Net_2_0;
		}
		
		string GetFrameworkVersion (ClrVersion version)
		{
			switch (version) {
				case ClrVersion.Net_1_1: return "v1.1";
				case ClrVersion.Net_2_0: return "v2.0";
				case ClrVersion.Clr_2_1: return "v2.1";
				case ClrVersion.Default: return GetFrameworkVersion (Runtime.SystemAssemblyService.CurrentClrVersion);
			}
			return null;
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
		
		internal static readonly ItemMember[] ExtendedMSBuildProperties = new ItemMember [] {
			new ItemMember (typeof(Project), "ProductVersion"),
			new ItemMember (typeof(Project), "SchemaVersion"),
			new ItemMember (typeof(Project), "ProjectGuid"),
			new ItemMember (typeof(Project), "ProjectTypeGuids"),
			new ItemMember (typeof(DotNetProjectConfiguration), "DebugType"),
			new ItemMember (typeof(DotNetProjectConfiguration), "ErrorReport"),
			new ItemMember (typeof(DotNetProjectConfiguration), "TargetFrameworkVersion"),
			new ItemMember (typeof(ProjectReference), "RequiredTargetFramework"),
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
			if (instance is SolutionEntityItem)
				return prop.IsExtendedProperty (typeof(SolutionEntityItem));
			if (instance is ProjectFile)
				return prop.IsExtendedProperty (typeof(ProjectFile));
			if (instance is ProjectReference)
				return prop.IsExtendedProperty (typeof(ProjectReference));
			if (instance is DotNetProjectConfiguration)
				if (prop.Name == "CodeGeneration")
					return false;
			return true;
		}
		
		protected override DataNode OnSerializeProperty (ItemProperty prop, SerializationContext serCtx, object instance, object value)
		{
			DataNode data = base.OnSerializeProperty (prop, serCtx, instance, value);
			if (instance is SolutionEntityItem) {
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
