//
// MSBuildFileFormat.cs
//
// Author:
//   Ankit Jain <jankit@novell.com>
//
// Copyright (C) 2006 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using MonoDevelop.Core;
using MonoDevelop.Projects;

using Mono.Addins;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.XPath;

namespace MonoDevelop.Prj2Make
{
	public class MSBuildFileFormat : IFileFormat
	{
		static XmlNamespaceManager manager;
		SlnFileFormat solutionFormat = new SlnFileFormat ();

		static List<MSBuildProjectExtension> extensions;

		public MSBuildFileFormat ()
		{
		}

		public string Name {
			get { return "Visual Studio .NET 2005"; }
		}

		public string GetValidFormatName (object obj, string fileName)
		{
			if (solutionFormat.CanWriteFile (obj))
				return solutionFormat.GetValidFormatName (obj, fileName);
			
			if (obj is DotNetProject) {
				string lang = ((DotNetProject)obj).LanguageName;
				if (lang == null || lang == "C#")
					//default
					return Path.ChangeExtension (fileName, ".csproj");
				if (lang == "VBNet")
					return Path.ChangeExtension (fileName, ".vbproj");
			}
			return fileName;
		}

		public bool CanReadFile (string file)
		{
			if (solutionFormat.CanReadFile (file))
				return true;
			
			if (Utils.GetLanguage (file) == null)
				return false;

			//FIXME: Need a better way to check the rootelement
			XmlReader xr = null;
			try {
				xr = XmlReader.Create (file);
				xr.MoveToContent ();

				if (xr.NodeType == XmlNodeType.Element && String.Compare (xr.LocalName, "Project") == 0 &&
					String.Compare (xr.NamespaceURI, Utils.ns) == 0)
					return true;

			} catch (FileNotFoundException fex) {
				Console.WriteLine (GettextCatalog.GetString ("File not found {0} : ", file));
				return false;
			} catch (XmlException xe) {
				Console.WriteLine (GettextCatalog.GetString ("Error reading file {0} : ", xe.ToString ()));
				return false;
			} finally {
				if (xr != null)
					((IDisposable)xr).Dispose ();
			}

			return false;
		}

		public bool CanWriteFile (object obj)
		{
			return (obj is DotNetProject) || solutionFormat.CanWriteFile (obj);
		}

		public System.Collections.Specialized.StringCollection GetExportFiles (object obj)
		{
			if (obj is Combine)
				return solutionFormat.GetExportFiles (obj);
			return null;
		}
		
		public static XmlNamespaceManager NamespaceManager {
			get {
				if (manager == null) {
					manager = new XmlNamespaceManager (new NameTable ());
					manager.AddNamespace ("tns", Utils.ns);
				}

				return manager;
			}
		}

		public void WriteFile (string file, object node, IProgressMonitor monitor)
		{
			if (solutionFormat.CanWriteFile (node)) {
				solutionFormat.WriteFile (file, node, monitor);
				return;
			}
			
			if (node == null)
				return;

			DotNetProject project = node as DotNetProject;
			if (project == null)
				throw new InvalidOperationException ("The provided object is not a DotNetProject");
			
			string tmpfilename = String.Empty;
			try {
				monitor.BeginTask (GettextCatalog.GetString ("Saving project: {0}", file), 1);
				try {
					if (File.Exists (file))
						tmpfilename = Path.GetTempFileName ();
				} catch (IOException) {
				}

				if (tmpfilename == String.Empty) {
					WriteFileInternal (file, project, monitor);
				} else {
					WriteFileInternal (tmpfilename, project, monitor);
					File.Delete (file);
					File.Move (tmpfilename, file);
				}
			} catch (Exception ex) {
				monitor.ReportError (GettextCatalog.GetString ("Could not save project: {0}", file), ex);
				Console.WriteLine ("Could not save project: {0}, {1}", file, ex);

				if (tmpfilename != String.Empty)
					File.Delete (tmpfilename);
				throw;
			} finally {
				monitor.EndTask ();
			}
		}

		void WriteFileInternal (string file, DotNetProject project, IProgressMonitor monitor)
		{
			string platform = "AnyCPU";
			bool newdoc = false;
			XmlDocument doc = null;

			MSBuildData data = Utils.GetMSBuildData (project);
			if (data == null) {
				//Create a new XmlDocument
				doc = new XmlDocument ();
				data = new MSBuildData ();
				data.Document = doc;
				newdoc = true;

				string type_guid;
				string type_guids = String.Empty;
				string longest_guid = String.Empty;
				foreach (MSBuildProjectExtension extn in GuidToExtensions) {
					string g = extn.GetGuidChain (project);
					if (g == null)
						continue;
					//HACK HACK
					if (g.Length > longest_guid.Length)
						longest_guid = g;
				}
				type_guids = longest_guid;
				MSBuildProjectExtension chain = GetExtensionChainFromTypeGuid (ref type_guids, out type_guid, project.LanguageName, file);
				data.ExtensionChain = chain;
				data.TypeGuids = type_guids;

				XmlElement e = doc.CreateElement ("Project", Utils.ns);
				doc.AppendChild (e);
				e.SetAttribute ("DefaultTargets", "Build");
			} else {
				doc = data.Document;
			}

			//Write out the Configurations
			//FIXME: Not touching global config right now,
			//instead just writing out everything in the individual configs
			XmlElement globalConfigElement = data.GlobalConfigElement;
			if (globalConfigElement == null) {
				globalConfigElement = doc.CreateElement ("PropertyGroup", Utils.ns);
				doc.DocumentElement.AppendChild (globalConfigElement);

				data.GlobalConfigElement = globalConfigElement;
				data.Guid = String.Format ("{{{0}}}", Guid.NewGuid ().ToString ().ToUpper ());

				if (newdoc)
					Utils.EnsureChildValue (globalConfigElement, "ProjectTypeGuids", data.TypeGuids);

				//FIXME: EnsureChildValue for AssemblyName <-> OutputAssembly
				//	Get this from where? different configs could have different ones.. 
			}

			Utils.EnsureChildValue (globalConfigElement, "ProjectGuid", data.Guid);
			Utils.EnsureChildValue (globalConfigElement, "RootNamespace", project.DefaultNamespace);

			//Default Config and platform
			//Note: Ignoring this, not relevant for MD, but might be useful for prj2make
			//For new projects, adding these elements in SaveProject
			//
			//string [] defaultActivePlatform = GetConfigPlatform (project.ActiveConfiguration.Name);
			//SetForNullCondition (doc, globalConfigElement, "Configuration", defaultActivePlatform [0]);
			//SetForNullCondition (doc, globalConfigElement, "Platform", defaultActivePlatform [1]);

			foreach (DotNetProjectConfiguration config in project.Configurations) {
				XmlElement configElement = null;

				if (data.ConfigElements.ContainsKey (config)) {
					configElement = data.ConfigElements [config];
				} else {
					//Create node for new configuration
					configElement = doc.CreateElement ("PropertyGroup", Utils.ns);
					doc.DocumentElement.AppendChild (configElement);

					//string configname = config.Name;
					string [] t = GetConfigPlatform (config.Name);
					//if (configname != config.Name)
					//	config.Name = configname;
					configElement.SetAttribute ("Condition", 
						String.Format (" '$(Configuration)|$(Platform)' == '{0}|{1}' ", t [0], t [1]));
					data.ConfigElements [config] = configElement;
				}

				data.ExtensionChain.WriteConfig (project, config, configElement, monitor);
			}

			// Always update the project references
			foreach (ProjectReference pref in project.ProjectReferences)
				data.ProjectReferenceElements [pref] = data.ExtensionChain.ReferenceToXmlElement (data, project, pref);
		
			foreach (ProjectFile pfile in project.ProjectFiles) {
				XmlElement xe = data.ExtensionChain.FileToXmlElement (data, project, pfile);
				if (xe != null)
					data.ProjectFileElements [pfile] = xe;
			}

			//FIXME: Set ActiveConfiguration
			CleanUpEmptyItemGroups (doc);

			if (newdoc) {
				XmlElement elem = doc.CreateElement ("Configuration", Utils.ns);
				data.GlobalConfigElement.AppendChild (elem);
				elem.InnerText = "Debug";
				elem.SetAttribute ("Condition", " '$(Configuration)' == '' ");

				elem = doc.CreateElement ("Platform", Utils.ns);
				data.GlobalConfigElement.AppendChild (elem);
				elem.InnerText = "AnyCPU";
				elem.SetAttribute ("Condition", " '$(Platform)' == '' ");

			}

			data.ExtensionChain.OnFinishWrite (data, project);

			if (newdoc) {
				// Do this at the end, so that it can be detected that this is
				// a non-msbuild project being converted
				project.ExtendedProperties [typeof (MSBuildFileFormat)] = data;
			}

			XmlWriterSettings settings = new XmlWriterSettings ();
			settings.OmitXmlDeclaration = true;
			settings.NewLineChars = "\r\n";
			settings.NewLineHandling = NewLineHandling.Replace;
			settings.Encoding = Encoding.UTF8;
			settings.Indent = true;

			using (XmlWriter xw = XmlWriter.Create (file, settings)) {
				doc.Save (xw);
				xw.Close ();
			}

			return;
		}

		/* Finds an element named @elementName, with a attribute Condition, which has "$(@elementName) = ''"
		 * and sets @value for that. Creates the element if its not found. */
		void SetForNullCondition (XmlDocument doc, XmlElement configElement, string elementName, string value)
		{
			XmlNodeList list = doc.SelectNodes (String.Format (
					"/tns:Project/tns:PropertyGroup/tns:{0}[@Condition]", elementName),
					NamespaceManager);
			foreach (XmlNode node in list) {
				if (CheckNullCondition (node as XmlElement, elementName)) {
					node.InnerText = value;
					return;
				}
			}

			//Add new xml element for active config
			XmlElement elem = doc.CreateElement (elementName, Utils.ns);
			configElement.AppendChild (elem);
			elem.InnerText = value;

			elem.SetAttribute ("Condition", " '$(" + elementName + ")' == '' ");
		}

		bool CheckNullCondition (XmlElement elem, string varName)
		{
			if (elem == null)
				return false;

			//FIXME: This will get instantiated repeatedly, save this
			StringDictionary dic = ParseCondition (elem.Attributes ["Condition"].Value);

			string varUpper = varName.ToUpper ();
			if (dic.Keys.Count == 1 && 
				dic.ContainsKey (varUpper) && String.IsNullOrEmpty (dic [varUpper])) {
				// Eg. '$(Configuration)' == ''
				return true;
			}

			return false;
		}

		void CleanUpEmptyItemGroups (XmlDocument doc)
		{
			XmlNodeList list = doc.SelectNodes ("/tns:Project/tns:ItemGroup[count(child)=0]", NamespaceManager);
			List<XmlNode> del = new List<XmlNode> ();
			foreach (XmlNode n in list) {
				if (!n.HasChildNodes)
					del.Add (n);
			}

			foreach (XmlNode n in del)
				n.ParentNode.RemoveChild (n);
		}

		public void SaveProject (DotNetProject project, IProgressMonitor monitor)
		{
			WriteFile (project.FileName, project, monitor);
			SetupHandlers (project);
		}

		//Reader
		public object ReadFile (string fileName, IProgressMonitor monitor)
		{
			// If it is a solution, use the solution reader
			if (solutionFormat.CanReadFile (fileName)) {
				return solutionFormat.ReadFile (fileName, monitor);
			}
			
			Project project = null;
			if (monitor == null || fileName == null)
				//FIXME: Use NullProgressMonitor for monitor?
				return null;

			try {
				monitor.BeginTask (GettextCatalog.GetString ("Loading project: {0}", fileName), 1);
				project = LoadProject (fileName, monitor);
			} catch (Exception ex) {
				monitor.ReportError (GettextCatalog.GetString ("Could not load project: {0}", fileName), ex);
				throw;
			} finally {
				monitor.EndTask ();
			}

			return project;
		}

		//FIXME: Use monitor to report warnings/errors
		DotNetProject LoadProject (string fname, IProgressMonitor monitor)
		{
			XmlDocument doc = new XmlDocument ();
			doc.Load (fname);

			XPathNavigator nav = doc.CreateNavigator ();
			nav.MoveToFirstChild ();

			while (! (nav.UnderlyingObject is XmlElement))
				nav.MoveToNext ();

			if (nav.NamespaceURI != Utils.ns)
				throw new UnknownProjectVersionException (fname, nav.NamespaceURI);

			//Resolve ../'s 
			fname = Path.GetFullPath (fname);
			string lang = Utils.GetLanguage (fname);
			string basePath = Path.GetDirectoryName (fname);

			//try to get type guid
			string type_guid;
			string type_guids = String.Empty;
			XmlNode node = doc.SelectSingleNode ("/tns:Project/tns:PropertyGroup/tns:ProjectTypeGuids", NamespaceManager);
			if (node != null) {
				if (node.NodeType == XmlNodeType.Element)
					type_guids = ((XmlElement) node).InnerText;
			}

			MSBuildProjectExtension extensionChain = GetExtensionChainFromTypeGuid (ref type_guids, out type_guid, lang, fname);

			//Create the project
			DotNetProject project = extensionChain.CreateProject (type_guid, fname, type_guids);
			project.FileName = fname;
			project.Version = "0.1"; //FIXME:
			//Default project name
			project.Name = Path.GetFileNameWithoutExtension (fname);
			project.FileFormat = new MSBuildFileFormat ();
			project.ClrVersion = ClrVersion.Net_2_0;

			MSBuildData data = new MSBuildData ();
			data.Document = doc;
			data.ExtensionChain = extensionChain;
			project.ExtendedProperties [typeof (MSBuildFileFormat)] = data;

			//Read the global config
			XPathNodeIterator iter = nav.Select ("/tns:Project/tns:PropertyGroup[not(@Condition)]", NamespaceManager);

			DotNetProjectConfiguration globalConfig = (DotNetProjectConfiguration) project.CreateConfiguration ("Temp");
			globalConfig.ClrVersion = ClrVersion.Net_2_0;

			string str_tmp = String.Empty;
			string guid = null;
			string rootNamespace = String.Empty;
			while (iter.MoveNext ()) {
				if (guid == null && 
					Utils.ReadAsString (iter.Current, "ProjectGuid", ref str_tmp, false))
					guid = str_tmp;

				//FIXME: Add basePath to list of params
				extensionChain.ReadConfig (project, globalConfig, iter.Current, basePath, monitor);

				//FIXME: Handle case when >1 global PropertyGroups exist,
				data.GlobalConfigElement = (XmlElement) iter.Current.UnderlyingObject;

				//FIXME: RootNamespace can be specified per-config, but we are 
				//taking the first occurrence
				if (rootNamespace == String.Empty &&
					Utils.ReadAsString (iter.Current, "RootNamespace", ref str_tmp, false)) {
					rootNamespace = str_tmp;
				}
			}
			project.DefaultNamespace = rootNamespace;

			if (guid != null)
				data.Guid = guid;

			//ReadItemGroups : References, Source files etc
			extensionChain.ReadItemGroups (data, project, globalConfig, basePath, monitor);

			//Load configurations
			iter = nav.Select ("/tns:Project/tns:PropertyGroup[@Condition]", NamespaceManager);
			while (iter.MoveNext ()) {
				StringDictionary dic = ParseCondition (
						iter.Current.GetAttribute ("Condition", NamespaceManager.DefaultNamespace));

				string configname = GetConfigName (dic);
				if (configname == null)
					continue;

				DotNetProjectConfiguration config = 
					(DotNetProjectConfiguration) project.GetConfiguration (configname);

				if (config == null) {
					config = (DotNetProjectConfiguration) globalConfig.Clone ();
					config.Name = configname;

					project.Configurations.Add (config);
				}

				extensionChain.ReadConfig (project, config, iter.Current, basePath, monitor);
				data.ConfigElements [config] = (XmlElement) iter.Current.UnderlyingObject;
			}

			//Read project-type specific FlavorProperties
			if (data.FlavorPropertiesParent != null) {
				foreach (XmlNode n in data.FlavorPropertiesParent.ChildNodes) {
					if (!n.HasChildNodes || n.Attributes ["GUID"] == null)
						//nothing to read
						continue;
					string tguid = n.Attributes ["GUID"].Value;
					if (String.IsNullOrEmpty (tguid))
						continue;
					extensionChain.ReadFlavorProperties (data, project, n, tguid);
				}
			}

			/* Note: Ignoring this, not required for MD, but might be useful in prj2make
			string confname = default_config + "|" + default_platform;
			if (project.Configurations [confname] != null)
				project.ActiveConfiguration = project.Configurations [confname]; */

			extensionChain.OnFinishRead (data, project);
			SetupHandlers (project);

			return project;
		}

		// Tries to get an extension chain for a @type_guids chain.
		// If @type_guids is null, then tries to determine type_guid from
		// the language
		MSBuildProjectExtension GetExtensionChainFromTypeGuid (ref string type_guids, out string type_guid, string lang, string fname)
		{
			if (String.IsNullOrEmpty (type_guids)) {
				if (!MSBuildFileFormat.ProjectTypeGuids.ContainsKey (lang))
					throw new Exception (String.Format ("Unknown project type : {0}", fname));
				type_guids = type_guid = MSBuildFileFormat.ProjectTypeGuids [lang];
			}
			type_guid = type_guids.Split (';') [0];

			string [] type_guid_list = type_guids.Split (new char [] {';'}, StringSplitOptions.RemoveEmptyEntries);
			MSBuildProjectExtension [] extensions = new MSBuildProjectExtension [type_guid_list.Length + 1];
			for (int i = 0; i < type_guid_list.Length; i ++) {
				foreach (MSBuildProjectExtension extn in GuidToExtensions) {
					if (extn.Supports (type_guid_list [i], fname, type_guids)) {
						extensions [i] = extn;
						break;
					}
				}
				if (extensions [i] == null)
					throw new Exception (String.Format ("Unsupported Project type, guid : {0}", type_guid_list [i]));
			}
			extensions [type_guid_list.Length] = new DefaultMSBuildProjectExtension ();

			for (int i = 0; i < extensions.Length - 1; i ++)
				extensions [i].Next = extensions [i + 1];

			return extensions [0];
		}

		static void SetupHandlers (DotNetProject project)
		{
			//References
			project.ReferenceRemovedFromProject += new ProjectReferenceEventHandler (HandleReferenceRemoved);
			project.ReferenceAddedToProject += new ProjectReferenceEventHandler (HandleReferenceAdded);

			//Files
			project.FileRemovedFromProject += new ProjectFileEventHandler (HandleFileRemoved);
			project.FileAddedToProject += new ProjectFileEventHandler (HandleFileAdded);
			project.FilePropertyChangedInProject += new ProjectFileEventHandler (HandleFilePropertyChanged);
			project.FileRenamedInProject += new ProjectFileRenamedEventHandler (HandleFileRenamed);

			//Configurations
			project.ConfigurationRemoved += new ConfigurationEventHandler (HandleConfigurationRemoved);

			project.NameChanged += new CombineEntryRenamedEventHandler (HandleRename);
		}

		static void HandleRename (object sender, CombineEntryRenamedEventArgs e)
		{
			if (e.CombineEntry.ParentCombine == null)
				//Ignore if the project is not yet a part of a Combine
				return;

			string oldfname = e.CombineEntry.FileName;
			string extn = Path.GetExtension (oldfname);
			string dir = Path.GetDirectoryName (oldfname);
			string newfname = Path.Combine (dir, e.NewName + extn);

			Runtime.FileService.MoveFile (oldfname, newfname);
			e.CombineEntry.FileName = newfname;
		}

		//Event handlers
		static void HandleConfigurationRemoved (object sender, ConfigurationEventArgs e)
		{
			DotNetProject project = (DotNetProject) sender;
			MSBuildData d = Utils.GetMSBuildData (project);
			if (d == null || !d.ConfigElements.ContainsKey ((DotNetProjectConfiguration) e.Configuration))
				return;

			XmlElement elem = d.ConfigElements [(DotNetProjectConfiguration)e.Configuration];
			elem.ParentNode.RemoveChild (elem);
			d.ConfigElements.Remove ((DotNetProjectConfiguration)e.Configuration);
		}

		//References

		static void HandleReferenceRemoved (object sender, ProjectReferenceEventArgs e)
		{
			MSBuildData d = Utils.GetMSBuildData (e.Project);
			if (d == null || !d.ProjectReferenceElements.ContainsKey (e.ProjectReference))
				return;

			XmlElement elem = d.ProjectReferenceElements [e.ProjectReference];
			elem.ParentNode.RemoveChild (elem);
			d.ProjectReferenceElements.Remove (e.ProjectReference);
		}

		static void HandleReferenceAdded (object sender, ProjectReferenceEventArgs e)
		{
			try {
				MSBuildData d = Utils.GetMSBuildData (e.Project);
				if (d == null)
					return;

				d.ProjectReferenceElements [e.ProjectReference] = 
					d.ExtensionChain.ReferenceToXmlElement (d, e.Project, e.ProjectReference);
			} catch (Exception ex) {
				Runtime.LoggingService.ErrorFormat ("{0}", ex.Message);
				Console.WriteLine ("{0}", ex.ToString ());
				throw;
			}
		}

		//ProjectFile-s

		static void HandleFileRemoved (object sender, ProjectFileEventArgs e)
		{
			MSBuildData d = Utils.GetMSBuildData (e.Project);
			if (d == null || !d.ProjectFileElements.ContainsKey (e.ProjectFile))
				return;

			XmlElement elem = d.ProjectFileElements [e.ProjectFile];
			elem.ParentNode.RemoveChild (elem);
			d.ProjectFileElements.Remove (e.ProjectFile);
		}

		static void HandleFileAdded (object sender, ProjectFileEventArgs e)
		{
			MSBuildData d = Utils.GetMSBuildData (e.Project);
			if (d == null)
				return;

			XmlElement xe = d.ExtensionChain.FileToXmlElement (d, e.Project, e.ProjectFile);
			if (xe != null)
				d.ProjectFileElements [e.ProjectFile] = xe;
		}

		static void HandleFileRenamed (object sender, ProjectFileRenamedEventArgs e)
		{
			MSBuildData d = Utils.GetMSBuildData (e.Project);
			if (d == null || !d.ProjectFileElements.ContainsKey (e.ProjectFile))
				return;

			//FIXME: Check whether this file is a ApplicationIcon and accordingly update that?
			XmlElement elem = d.ExtensionChain.FileToXmlElement (d, e.Project, e.ProjectFile);
			if (elem != null)
				d.ProjectFileElements [e.ProjectFile] = elem;
		}

		static void HandleFilePropertyChanged (object sender, ProjectFileEventArgs e)
		{
			//Subtype, BuildAction, DependsOn, Data

			MSBuildData d = Utils.GetMSBuildData (e.Project);
			if (d == null || !d.ProjectFileElements.ContainsKey (e.ProjectFile))
				return;

			XmlElement elem = d.ProjectFileElements [e.ProjectFile];

			//BuildAction
			string buildAction = BuildActionToString (e.ProjectFile.BuildAction);
			if (buildAction == null) {
				Runtime.LoggingService.WarnFormat ("BuildAction.{0} not supported!", e.ProjectFile.BuildAction);
				Console.WriteLine ("BuildAction.{0} not supported!", e.ProjectFile.BuildAction);
				return;
			}

			if (elem.LocalName != buildAction) {
				XmlElement newElem = d.Document.CreateElement (buildAction, Utils.ns);
				XmlNode parent = elem.ParentNode;

				List<XmlNode> list = new List<XmlNode> ();
				foreach (XmlNode n in elem.ChildNodes)
					list.Add (n);
				foreach (XmlNode n in list)
					newElem.AppendChild (elem.RemoveChild (n));

				list.Clear ();
				foreach (XmlAttribute a in elem.Attributes)
					list.Add (a);

				foreach (XmlAttribute a in list)
					newElem.Attributes.Append (elem.Attributes.Remove (a));

				parent.RemoveChild (elem);
				parent.AppendChild (newElem);

				d.ProjectFileElements [e.ProjectFile] = newElem;
			}

			//DependentUpon is relative to the basedir of the 'pf' (resource file)
			if (!String.IsNullOrEmpty (e.ProjectFile.DependsOn))
				Utils.EnsureChildValue (d.ProjectFileElements [e.ProjectFile], "DependentUpon",
					Utils.CanonicalizePath (Runtime.FileService.AbsoluteToRelativePath (
						Path.GetDirectoryName (e.ProjectFile.Name), e.ProjectFile.DependsOn)));
			//FIXME: Subtype, Data
		}

		internal static string BuildActionToString (BuildAction ba)
		{
			switch (ba) {
			case BuildAction.Nothing:
				return "None";								
			case BuildAction.Compile:
				return "Compile";
			case BuildAction.EmbedAsResource:
				return "EmbeddedResource";
			case BuildAction.FileCopy:
				return "Content";
			case BuildAction.Exclude:
				//FIXME:
				break;
			}

			return null;
		}

		//Reading

		StringDictionary ParseCondition (string condition)
		{
			StringDictionary dic = new StringDictionary ();

			if (condition == null || condition.Length == 0)
				return dic;

			Match m = ConditionRegex.Match (condition);
			if (!m.Success)
				return dic;

			string left = m.Groups [1].Value;
			string right = m.Groups [2].Value;

			string [] left_parts = left.Split ('|');
			string [] right_parts = right.Split ('|');

			for (int i = 0; i < left_parts.Length; i ++) {
				if (left_parts [i].StartsWith ("$(") &&
					left_parts [i].EndsWith (")")) {
					//FIXME: Yuck!
					string key = left_parts [i].Substring (2, left_parts [i].Length - 3);
					if (i < right_parts.Length)
						dic [key.ToUpper ()] = right_parts [i].Trim ();
					else
						dic [key.ToUpper ()] = String.Empty;
				}

			}

			return dic;
		}

		//Utility methods

		static string GetConfigName (StringDictionary dic)
		{
			if (!dic.ContainsKey ("CONFIGURATION") || 
				String.IsNullOrEmpty (dic ["CONFIGURATION"]))
				return null;

			string configname = dic ["CONFIGURATION"];
			if (dic.ContainsKey ("PLATFORM") && !String.IsNullOrEmpty (dic ["PLATFORM"])) {
				if (String.Compare (dic ["PLATFORM"], "AnyCPU", true) == 0)
					configname = configname + "|Any CPU";
				else
					configname = configname + "|" + dic ["PLATFORM"];
			}

			return configname;
		}

		/* Returns [0] : Config name, [1] : Platform */
		static string [] GetConfigPlatform (string name)
		{
			//FIXME: Handle Abc|Foo|x64 ? VS2005 doesn't allow a config
			//name with |
			string [] tmp = name.Split (new char [] {'|'}, 2);
			string [] ret = new string [2];
			ret [0] = tmp [0];
			if (tmp.Length < 2) {
				ret [1] = "AnyCPU";
				//name = String.Format ("{0}|{1}", ret [0], ret [1]);
			} else {
				ret [1] = tmp [1];
			}

			return ret;
		}

		public static XmlElement GetFlavorPropertiesElement (MSBuildData data, string guid, bool create)
		{
			XmlElement parent = data.FlavorPropertiesParent;
			if (parent == null) {
				if (!create)
					return null;

				parent = Utils.GetXmlElement (data.Document, data.Document, "/Project/ProjectExtensions/VisualStudio", create);
			}

			foreach (XmlNode node in parent.ChildNodes) {
					if (node.NodeType != XmlNodeType.Element || node.LocalName != "FlavorProperties")
						continue;
					if (node.Attributes ["GUID"] != null && String.Compare (node.Attributes ["GUID"].Value, guid, true) == 0)
						return (XmlElement) node;
			}

			//FlavorProperties not found
			if (!create)
				return null;

			XmlElement flavor_properties_element = data.Document.CreateElement ("FlavorProperties", Utils.ns);
			flavor_properties_element.SetAttribute ("GUID", guid);
			data.FlavorPropertiesParent.AppendChild (flavor_properties_element);
			return flavor_properties_element;
		}

		static List<MSBuildProjectExtension> GuidToExtensions {
			get {
				if (extensions == null) {
					extensions = new List<MSBuildProjectExtension> ();
					OnProjectExtensionsChanged (null, null);
					AddinManager.AddExtensionNodeHandler ("/MonoDevelop/Prj2Make/MSBuildProjectExtension", OnProjectExtensionsChanged);
				}
				return extensions;
			}
		}

		static void OnProjectExtensionsChanged (object s, ExtensionNodeEventArgs args)
		{
			extensions.Clear ();
			foreach (MSBuildProjectExtension extn in
				AddinManager.GetExtensionObjects ("/MonoDevelop/Prj2Make/MSBuildProjectExtension", typeof (MSBuildProjectExtension))) {
				extensions.Add (extn);
			}
			extensions.Add (new DefaultMSBuildProjectExtension ());
		}

		static Regex conditionRegex = null;
		static Regex ConditionRegex {
			get {
				if (conditionRegex == null)
					conditionRegex = new Regex (@"'([^']*)'\s*==\s*'([^']*)'");
				return conditionRegex;
			}
		}

		// Contains only assembly name like "System.Xml"
		// used to get the correct case of assembly names,
		// like System.XML
		static Dictionary<string, string> assemblyNamesTable = null;
		internal static Dictionary<string, string> AssemblyNamesTable {
			get {
				if (assemblyNamesTable == null) {
					assemblyNamesTable = new Dictionary<string, string> (StringComparer.InvariantCultureIgnoreCase);
					foreach (string fullname in Runtime.SystemAssemblyService.GetAssemblyFullNames ()) {
						string name = fullname;
						if (name.IndexOf (',') >= 0)
							name = name.Substring (0, name.IndexOf (',')).Trim ();
						assemblyNamesTable [name] = name;
					}
				}

				return assemblyNamesTable;
			}

		}

		static Dictionary<string, string> projectTypeGuids = null;
		public static Dictionary<string, string> ProjectTypeGuids {
			get {
				if (projectTypeGuids == null) {
					projectTypeGuids = new Dictionary<string, string> ();
					// values must be in UpperCase
					projectTypeGuids ["C#"] = "{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}";
					projectTypeGuids ["VBNet"] = "{F184B08F-C81C-45F6-A57F-5ABD9991F28F}";
				}
				return projectTypeGuids;
			}
		}

	}
}
