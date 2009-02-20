//
// WebTypeManager.cs: Handles ASP.NET type lookups for web projects.
//
// Authors:
//   Michael Hutchinson <mhutchinson@novell.com>
//
// Copyright (C) 2007 Novell, Inc (http://www.novell.com)
//
//
// This source code is licenced under The MIT License:
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

using System;
using System.Collections.Generic;
using System.Xml;
using System.IO;
using System.Linq;
using System.Configuration;
using System.Web.Configuration;

using MonoDevelop.Core;
using MonoDevelop.Projects.Text;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Projects.Gui.Completion;
using MonoDevelop.Projects.Dom;
using MonoDevelop.Projects.Dom.Parser;

namespace MonoDevelop.AspNet
{
	
	
	public static class WebTypeManager
	{
		
		//NOTE: we can't just fall through to GetRegisteredType, as we may be able to determine  usercontrols
		public static string GetRegisteredTypeName (AspNetAppProject project, string webDirectory, string tagPrefix, string tagName)
		{
			IType t = GetRegisteredType (project, webDirectory, tagPrefix, tagName);
			if (t != null)
				return t.FullName;
			return null;		
		}
		
		public static IType GetRegisteredType (AspNetAppProject project, string webDirectory, string tagPrefix, string tagName)
		{
			if (project == null)
				return GetMachineRegisteredType (project, tagPrefix, tagName);
			
			//global control registration not possible in ASP.NET 1.1
			if (project.TargetFramework.ClrVersion == MonoDevelop.Core.ClrVersion.Net_1_1)
				return null;
			
			//read the web.config files at each level
			//look up a level if a result not found until we hit the project root
			foreach (RegistrationInfo info in project.ControlRegistrationCache.GetInfosForPath (webDirectory)) {
				if (info.PrefixMatches (tagPrefix)) {
					if (info.IsAssembly) {
						ProjectDom dom = ResolveAssembly (project, info.Assembly);
						if (dom == null)
							continue;
						
						IType type = AssemblyTypeLookup (dom, info.Namespace, tagName);
						if (type != null)
								return type;
					}
					if (info.IsUserControl && info.NameMatches (tagName)) {
						IType type = GetUserControlType (project, info.Source, Path.GetDirectoryName (info.ConfigFile));
						if (type != null)
								return type;
					}
				}
			}
			
			return GetMachineRegisteredType (project, tagPrefix, tagName);
		}
		
		public static IEnumerable<CompletionData> GetRegisteredTypeCompletionData (AspNetAppProject project, string webDirectory, IType baseType)
		{
			if (project == null) {
				foreach (CompletionData cd in GetMachineRegisteredTypeCompletionData (project, baseType))
					yield return cd;
				yield break;
			}	
			
			//global control registration not possible in ASP.NET 1.1
			if (project.TargetFramework.ClrVersion == MonoDevelop.Core.ClrVersion.Net_1_1)
				yield break;
			
			//read the web.config files at each level
			//look up a level if a result not found until we hit the project root
			foreach (RegistrationInfo info in project.ControlRegistrationCache.GetInfosForPath (webDirectory)) {
				if (info.IsAssembly) {
					ProjectDom dom = WebTypeManager.ResolveAssembly (project, info.Assembly);
					if (dom == null)
						continue;
					foreach (IType t in ListControlClasses (baseType, dom, info.Namespace))
						yield return new MonoDevelop.AspNet.Parser.AspTagCompletionData (info.TagPrefix + ":", t);
				}
				else if (info.IsUserControl) {
					//TODO: resolve docs
					//IType t = GetUserControlType (project, info.Source, Path.GetDirectoryName (info.ConfigFile));
					//if (t != null)
					yield return new CompletionData (info.TagPrefix + ":" + info.TagName, Gtk.Stock.GoForward);
				}
			}
			
			foreach (CompletionData cd in GetMachineRegisteredTypeCompletionData (project, baseType))
				yield return cd;
		}
		
		public static string GetMachineRegisteredTypeName (AspNetAppProject project, string tagPrefix, string tagName)
		{
			IType t = GetMachineRegisteredType (project, tagPrefix, tagName);
			if (t != null)
				return t.FullName;
			return null;			
		}
		
		public static IType GetMachineRegisteredType (AspNetAppProject project, string tagPrefix, string tagName)
		{
			//check in machine.config
			Configuration config = ConfigurationManager.OpenMachineConfiguration ();
			PagesSection pages = (PagesSection) config.GetSection ("system.web/pages");
			
			foreach (TagPrefixInfo tpxInfo in pages.Controls) {
				if (tpxInfo.TagPrefix != tagPrefix)
					continue;
				ProjectDom dom = WebTypeManager.ResolveAssembly (project, tpxInfo.Assembly);
				if (dom == null)
						continue;
				IType type = AssemblyTypeLookup (dom, tpxInfo.Namespace, tagName);
				if (type != null)
					return type;
				//user controls don't make sense in machine.config; ignore them
			}
			return null;
		}
		
		public static IEnumerable<MonoDevelop.Projects.Gui.Completion.CompletionData>
			GetMachineRegisteredTypeCompletionData (AspNetAppProject project, IType baseType)
		{
			Configuration config = ConfigurationManager.OpenMachineConfiguration ();
			PagesSection pages = (PagesSection) config.GetSection ("system.web/pages");
			
			foreach (TagPrefixInfo tpxInfo in pages.Controls) {
				if (!String.IsNullOrEmpty (tpxInfo.Namespace) && !String.IsNullOrEmpty (tpxInfo.Assembly) && !string.IsNullOrEmpty (tpxInfo.TagPrefix)) {
					ProjectDom dom = WebTypeManager.ResolveAssembly (project, tpxInfo.Assembly);
					if (dom != null)
						foreach (IType type in ListControlClasses (baseType, dom, tpxInfo.Namespace))
							yield return new MonoDevelop.AspNet.Parser.AspTagCompletionData (tpxInfo.TagPrefix, type);
				}
			}
		}
		
		#region HTML type lookups
		
		public static string HtmlControlLookup (string tagName)
		{
			return HtmlControlLookup (tagName, null);
		}
		
		public static string HtmlControlLookup (string tagName, string typeAttribute)
		{
			string htmc = "System.Web.UI.HtmlControls.";
			switch (tagName.ToLower ()) {
			case "a":
				return htmc + "HtmlAnchor";
			case "button":
				return htmc + "HtmlButton";
			case "form":
				return htmc + "HtmlForm";
			case "head":
				return htmc + "HtmlHead";
			case "img":
				return htmc + "HtmlImage";
			case "input":
				string val = lookupHtmlInput (typeAttribute);
				return val != null? htmc + val : null;
			case "link":
				return htmc + "HtmlLink";
			case "meta":
				return htmc + "HtmlMeta";
			case "select":
				return htmc + "HtmlSelect";
			case "table":
				return htmc + "HtmlTable";
			case "th":
			case "td":
				return htmc + "HtmlTableCell";
			case "tr":
				return htmc + "HtmlTableRow";
			case "textarea":
				return htmc + "HtmlTextArea";
			case "title":
				return htmc + "HtmlTitle";
			default:
				return htmc + "HtmlGenericControl";
			}
		}
		
		static string lookupHtmlInput (string type)
		{
			switch (type != null? type.ToLower () : null)
			{
			case "button":
			case "reset":
			case "submit":
				return "HtmlInputButton";
			case "checkbox":
				return "HtmlInputCheckBox";
			case "file":
				return "HtmlInputFile";
			case "hidden":
				return "HtmlInputHidden";
			case "image":
				return "HtmlInputImage";
			case "password":
				return "HtmlInputText";
			case "radio":
				return "HtmlInputRadioButton";
			case "text":
				return "HtmlInputText";
			default:
				return "HtmlInputControl";
			}
		}
		
		
		#endregion 
		
		#region Control type lookups
		
		public static string SystemTypeNameLookup (string tagName, AspNetAppProject project)
		{
			return SystemTypeNameLookup (tagName, WebTypeManager.GetProjectTargetFramework (project));
		}
		
		public static IType SystemTypeLookup (string tagName, AspNetAppProject project)
		{
			return SystemTypeLookup (tagName, WebTypeManager.GetProjectTargetFramework (project));
		}
		
		public static string SystemTypeNameLookup (string tagName, MonoDevelop.Core.TargetFramework targetFramework)
		{
			IType cls = SystemTypeLookup (tagName, targetFramework);
			return cls != null? cls.FullName : null;
		}
		
		public static IType SystemTypeLookup (string tagName, MonoDevelop.Core.TargetFramework targetFramework)
		{
			return AssemblyTypeLookup (GetSystemWebDom (targetFramework), "System.Web.UI.WebControls", tagName);
		}
		
		public static string AssemblyTypeNameLookup (ProjectDom assemblyDatabase, string namespac, string tagName)
		{
			IType cls = AssemblyTypeLookup (assemblyDatabase, namespac, tagName);
			return cls != null? cls.FullName : null;
		}
		
		public static IType AssemblyTypeLookup (ProjectDom assemblyDatabase, string namespac, string tagName)
		{
			return (assemblyDatabase == null)
				? null
				: assemblyDatabase.GetType (namespac + "." + tagName, false, false);
		}
		
		#endregion
		
		public static ProjectDom ResolveAssembly (AspNetAppProject project, string assemblyName)
		{
			ProjectDom dom = InternalResolveAssembly (project, assemblyName);
			if (dom == null)
				LoggingService.LogWarning ("Failed to obtain completion database for {0}", assemblyName);
			return dom;
		}
		
		static ProjectDom InternalResolveAssembly (AspNetAppProject project, string assemblyName)
		{
			string path;
			if (project == null) {
				assemblyName = Runtime.SystemAssemblyService.GetAssemblyFullName (assemblyName);
				if (assemblyName == null)
					return null;
				assemblyName = Runtime.SystemAssemblyService.GetAssemblyNameForVersion (assemblyName, TargetFramework.Default);
				if (assemblyName == null)
					return null;
				path = Runtime.SystemAssemblyService.GetAssemblyLocation (assemblyName);
			} else {
				path = project.ResolveAssembly (assemblyName);
			}
			
			if (path == null)
				return null;
			return MonoDevelop.Projects.Dom.Parser.ProjectDomService.GetAssemblyDom (path);
		}
		
		#region System type listings
		
		public static MonoDevelop.Core.TargetFramework GetProjectTargetFramework (AspNetAppProject project)
		{
			return project == null? MonoDevelop.Core.TargetFramework.Default : project.TargetFramework;
		}
		
		public static ProjectDom GetSystemWebDom (AspNetAppProject project)
		{
			return GetSystemWebDom (GetProjectTargetFramework (project));
		}
		
		static ProjectDom GetSystemWebDom (MonoDevelop.Core.TargetFramework targetFramework)
		{
			string file = Runtime.SystemAssemblyService.GetAssemblyNameForVersion ("System.Web, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a", targetFramework);
			if (String.IsNullOrEmpty (file))
				throw new Exception ("System.Web assembly name not found for framework " + targetFramework.Id);
			file = Runtime.SystemAssemblyService.GetAssemblyLocation (file);
			if (String.IsNullOrEmpty (file))
				throw new Exception ("System.Web assembly file not found for framework " + targetFramework.Id);
			ProjectDom dom = ProjectDomService.GetAssemblyDom (file);
			if (dom == null)
				throw new Exception ("System.Web parse database not found for framework " + targetFramework.Id + " file '" + file + "'");
			return dom;
		}
		
		public static IEnumerable<IType> ListSystemControlClasses (IType baseType, AspNetAppProject project)
		{
			return ListControlClasses (baseType, GetSystemWebDom (project), "System.Web.UI.WebControls");
		}
		
		public static IEnumerable<IType> ListControlClasses (IType baseType, ProjectDom database, string namespac)
		{
			if (database == null)
				yield break;
			
			//return classes if they derive from system.web.ui.control
			foreach (IType type in database.GetSubclasses (baseType, false, new string [] {namespac}))
				if (!type.IsAbstract && type.IsPublic)
					yield return type;
			
			if (!baseType.IsAbstract && baseType.IsPublic && baseType.Namespace == namespac) {
				IType t = database.GetType (baseType.FullName);
				if (t != null)
					yield return baseType;
			}
		}
		
		#endregion
		
		
		public static string GetControlPrefix (AspNetAppProject project, string webDirectory, IType control)
		{
			if (control.Namespace == "System.Web.UI.WebControls")
				return "asp";
			else if (control.Namespace == "System.Web.UI.HtmlControls")
				return string.Empty;
			
			//todo: handle user controls
			foreach (RegistrationInfo info in project.ControlRegistrationCache.GetInfosForPath (webDirectory)) {
				if (info.IsAssembly && info.Namespace == control.Namespace) {
					ProjectDom dom = ResolveAssembly (project, info.Assembly);
					if (dom != null && AssemblyTypeLookup (dom, info.Namespace, control.Name) != null)
						return info.TagPrefix;
				}
			}
			
			//machine.config
			Configuration config = ConfigurationManager.OpenMachineConfiguration ();
			PagesSection pages = (PagesSection) config.GetSection ("system.web/pages");
			foreach (TagPrefixInfo tpxInfo in pages.Controls)
				if (!string.IsNullOrEmpty (tpxInfo.Namespace) && !string.IsNullOrEmpty (tpxInfo.TagPrefix) 
				    && control.Namespace == tpxInfo.Namespace)
					return tpxInfo.TagPrefix;
			
			return null;
		}
		
		public static string GetUserControlTypeName (AspNetAppProject project, string fileName, string relativeToPath)
		{
			IType type = GetUserControlType (project, fileName, relativeToPath);
			if (type != null)
				return type.FullName;
			return null;		
		}
		
		public static IType GetUserControlType (AspNetAppProject project, string fileName, string relativeToPath)
		{
			//FIXME: actually look up the type
			//or maybe it's not necessary, as the compilers can't handle the types because
			//they're only generated when the UserControl is hit.
			return AssemblyTypeLookup (GetSystemWebDom (GetProjectTargetFramework (project)), "System.Web.UI", "UserControl");
		}
		
		#region Global control registration tracking
		
		static XmlTextReader GetConfigReader (string configFile)
		{
			IEditableTextFile textFile = 
				MonoDevelop.DesignerSupport.OpenDocumentFileProvider.Instance.GetEditableTextFile (configFile);
			if (textFile != null)
				return new XmlTextReader (textFile.Text, XmlNodeType.Document, null);
			else
				return new XmlTextReader (configFile);
		}
		
		static IEnumerable<TagPrefixInfo> GetRegistrationTags (XmlTextReader reader)
		{
			reader.WhitespaceHandling = WhitespaceHandling.None;
			reader.MoveToContent();
			
			if (reader.Name == "configuration"
			    && reader.ReadToDescendant ("system.web") && reader.NodeType == XmlNodeType.Element
			    && reader.ReadToDescendant ("pages") && reader.NodeType == XmlNodeType.Element
			    && reader.ReadToDescendant ("controls") && reader.NodeType == XmlNodeType.Element
			    && reader.ReadToDescendant ("add") && reader.NodeType == XmlNodeType.Element)
			{
				do {
					if (reader.MoveToAttribute ("tagPrefix")) {
						string prefix = reader.Value;
						
						//assemblies
						if (reader.MoveToAttribute ("namespace")) {
							string _namespace = reader.Value;
							string _assembly = reader.MoveToAttribute ("assembly")? reader.Value : null;
							yield return new TagPrefixInfo (prefix, _namespace, _assembly, null, null);
						}
						
						//user controls
						if (reader.MoveToAttribute ("tagName")) {
							string tagName = reader.Value;
							string src = reader.MoveToAttribute ("src")? reader.Value : null;
							yield return new TagPrefixInfo (prefix, null, null, tagName, src);
						}
					}
				} while (reader.ReadToNextSibling ("add"));
			};
		}
		
		#endregion
		
		//N.B. web.config and machine.config can add/remove items to this list
		static void AddDefaultImportedNamespaces (System.Collections.Generic.Dictionary<string, object> list)
		{
			//see http://msdn.microsoft.com/en-us/library/eb44kack.aspx
			object flag = new object ();
			list ["System"] = flag;
			list ["System.Collections"] = flag;
			list ["System.Collections.Specialized"] = flag;
			list ["System.Configuration"] = flag;
			list ["System.Text"] = flag;
			list ["System.Text.RegularExpressions"] = flag;
			list ["System.Web"] = flag;
			list ["System.Web.Caching"] = flag;
			list ["System.Web.Profile"] = flag;
			list ["System.Web.Security"] = flag;
			list ["System.Web.SessionState"] = flag;
			list ["System.Web.UI"] = flag;
			list ["System.Web.UI.HtmlControls"] = flag;
			list ["System.Web.UI.WebControls"] = flag;
			list ["System.Web.UI.WebControls.WebParts "] = flag;
		}
	}
}
