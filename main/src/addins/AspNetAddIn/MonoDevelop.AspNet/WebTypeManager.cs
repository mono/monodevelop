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
using System.Configuration;
using System.Web.Configuration;

using MonoDevelop.Projects.Text;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Projects.Dom;
using MonoDevelop.Projects.Dom.Parser;

namespace MonoDevelop.AspNet
{
	
	
	public static class WebTypeManager
	{
	
		public static string GetRegisteredTypeName (AspNetAppProject project, string webDirectory, string tagPrefix, string tagName)
		{
			if (project == null)
				return GetMachineRegisteredTypeName (tagPrefix, tagName);
			
			//global control registration not possible in ASP.NET 1.1
			if (project.ClrVersion == MonoDevelop.Core.ClrVersion.Net_1_1)
				return null;
			
			//read the web.config files at each level
			//look up a level if a result not found until we hit the project root
			DirectoryInfo dir = new DirectoryInfo (webDirectory);
			string projectRootParent = new DirectoryInfo (project.BaseDirectory).Parent.FullName;
			while (dir != null && dir.FullName != projectRootParent) {
				string configPath =  Path.Combine (dir.FullName, "web.config");
				if (File.Exists (configPath)) {
					string fullName = GetFullTypeNameFromConfig (configPath, tagPrefix, tagName);
					if (fullName != null)
						return fullName;
				}
				dir = dir.Parent;
			}
			
			return GetMachineRegisteredTypeName (tagPrefix, tagName);
		}
		
		public static string GetMachineRegisteredTypeName (string tagPrefix, string tagName)
		{
			//check in machine.config
			Configuration config = ConfigurationManager.OpenMachineConfiguration ();
			PagesSection pages = (PagesSection) config.GetSection ("system.web/pages");
			
			foreach (TagPrefixInfo tpxInfo in pages.Controls) {
				if (tpxInfo.TagPrefix != tagPrefix)
					continue;
				string fullName = AssemblyTypeNameLookup (tagName, tpxInfo.Namespace, tpxInfo.Assembly);
				if (fullName != null)
					return fullName;
				//user controls don't make sense in machine.config; ignore them
			}
			return null;
		}
		
		static string GetFullTypeNameFromConfig (string configFile, string tagPrefix, string tagName)
		{
			XmlTextReader reader = null;
			try {
				//load the document from the text editor if it's open, else from the file
				IEditableTextFile textFile = MonoDevelop.DesignerSupport.OpenDocumentFileProvider.Instance.GetEditableTextFile (configFile);
				if (textFile != null)
					reader = new XmlTextReader (textFile.Text, XmlNodeType.Document, null);
				else
					reader = new XmlTextReader (configFile);
				reader.WhitespaceHandling = WhitespaceHandling.None;
				
				reader.MoveToContent();
				if (reader.Name == "configuration"
				    && reader.ReadToDescendant ("system.web") && reader.NodeType == XmlNodeType.Element
				    && reader.ReadToDescendant ("pages") && reader.NodeType == XmlNodeType.Element
					&& reader.ReadToDescendant ("controls") && reader.NodeType == XmlNodeType.Element
				    && reader.ReadToDescendant ("add") && reader.NodeType == XmlNodeType.Element) {
					do {
						//check the tag prefix matches
						if (reader.MoveToAttribute ("tagPrefix") && reader.Value == tagPrefix) {
							//look up tags in assemblies
							if (reader.MoveToAttribute ("namespace")) {
								string _namespace = reader.Value;
								string _assembly = reader.MoveToAttribute ("assembly")? reader.Value : null;
								string fullName = AssemblyTypeNameLookup (tagName, _namespace, _assembly);
								if (fullName != null)
									return fullName;
							}
							
							//look up tag in user controls
							if (reader.MoveToAttribute ("tagName") && reader.Value == tagName
							    && reader.MoveToAttribute ("src") && !string.IsNullOrEmpty (reader.Value)) {
								string src = reader.Value;
								string fullName = GetControlTypeName (src, Path.GetDirectoryName (configFile));
								if (fullName != null) {
									return fullName;
								}
							}
						}
					} while (reader.ReadToNextSibling ("add"));
				}
			} catch (XmlException) {
			} finally {
				if (reader!= null)
					reader.Close ();
			}
			return null;
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
		
		public static string SystemWebControlLookup (string tagName, MonoDevelop.Core.ClrVersion clrVersion)
		{
			ProjectDom database = GetSystemWebAssemblyContext (clrVersion);
			IType cls = database.GetType ("System.Web.UI.WebControls." + tagName, -1, false, true);
			return cls != null? cls.FullName : null;
		}
		
		static ProjectDom GetSystemWebAssemblyContext (MonoDevelop.Core.ClrVersion clrVersion)
		{
			string assem = MonoDevelop.Core.Runtime.SystemAssemblyService.GetAssemblyNameForVersion ("System.Web", clrVersion);
			return MonoDevelop.Projects.Dom.Parser.ProjectDomService.GetAssemblyProjectDom (assem);
		}
		
		public static string AssemblyTypeNameLookup (string tagName, string namespac, string assem)
		{
			IType cls = AssemblyTypeLookup (tagName, namespac, assem);
			return cls != null? cls.FullName : null;
		}
		
		public static IType AssemblyTypeLookup (string tagName, string namespac, string assem)
		{
			ProjectDom database = MonoDevelop.Projects.Dom.Parser.ProjectDomService.GetAssemblyProjectDom (assem);
			if (database == null)
				return null;
//			ctx.UpdateDatabase ();
			return database.GetType (namespac + "." + tagName, -1, false, true);
		}
		
		#endregion
		
		#region System type listings
		
		static MonoDevelop.Core.ClrVersion GetProjectClrVersion (AspNetAppProject project)
		{
			return project == null? MonoDevelop.Core.ClrVersion.Default : project.ClrVersion;
		}
		
		public static ProjectDom GetSystemWebDom (AspNetAppProject project)
		{
			return MonoDevelop.Projects.Dom.Parser.ProjectDomService.GetAssemblyProjectDom (
				MonoDevelop.Core.Runtime.SystemAssemblyService.GetAssemblyNameForVersion (
					"System.Web", GetProjectClrVersion (project)));
		}
		
		
		public static IEnumerable<IType> ListSystemControlClasses (AspNetAppProject project)
		{
			return ListControlClasses (
				MonoDevelop.Core.Runtime.SystemAssemblyService.GetAssemblyNameForVersion (
					"System.Web",
					GetProjectClrVersion (project)),
				"System.Web.UI.WebControls");
		}
		
		public static IEnumerable<IType> ListControlClasses (string assem, string namespac)
		{
			
			ProjectDom database = MonoDevelop.Projects.Dom.Parser.ProjectDomService.GetAssemblyProjectDom (assem);
			if (database == null)
				yield break;
			
			DomReturnType swc = new DomReturnType ("System.Web.UI.Control");
			
			//return classes if they derive from system.web.ui.control
			foreach (IMember mem in database.GetNamespaceContents (namespac, true, true)) {
				IType cls = mem as IType;
				if (cls != null && !cls.IsAbstract && cls.IsPublic && cls.IsBaseType (swc))
					yield return cls;
			}
		}
		
		#endregion
		
		public static string TypeNameLookup (AspNetAppProject project, string tagName, string namespac, string assem)
		{
			IType cls = TypeLookup (project, tagName, namespac, assem);
			return cls != null? cls.FullName : null;
		}
		
		public static IType TypeLookup (AspNetAppProject project, string tagName, string namespac, string assem)
		{
			IType cls = null;
			ProjectDom database = null;
			
			if (!string.IsNullOrEmpty (namespac)) {
				if (!string.IsNullOrEmpty (assem))
					database = MonoDevelop.Projects.Dom.Parser.ProjectDomService.GetAssemblyProjectDom (assem);
				else if (project != null)
					database = MonoDevelop.Projects.Dom.Parser.ProjectDomService.GetDatabaseProjectDom (project);
				else
					database = GetSystemWebAssemblyContext (MonoDevelop.Core.ClrVersion.Default);
//FIXME				
//ctx.UpdateDatabase ();
				
				if (database == null) {
					MonoDevelop.Core.LoggingService.LogError ("WebTypeManager could not obtain a type database.");
					return null;
				}
				
				cls = database.GetType (namespac + "." + tagName, 0, false, true);
			}
			return cls;
		}
		
		public static string GetControlPrefix (AspNetAppProject project, IType control)
		{
			if (control.Namespace == "System.Web.UI.WebControls")
				return "asp";
			else if (control.Namespace == "System.Web.UI.HtmlControls")
				return string.Empty;
			
			//todo: look in web.config etc
			
			//machine.config
			Configuration config = ConfigurationManager.OpenMachineConfiguration ();
			PagesSection pages = (PagesSection) config.GetSection ("system.web/pages");
			foreach (TagPrefixInfo tpxInfo in pages.Controls)
				if (!string.IsNullOrEmpty (tpxInfo.Namespace) && !string.IsNullOrEmpty (tpxInfo.TagPrefix) 
				    && control.Namespace == tpxInfo.Namespace)
					return tpxInfo.TagPrefix;
			
			return null;
		}
		
		public static string GetControlTypeName (string fileName, string relativeToPath)
		{
			//FIXME: actually look up the type
			//or maybe it's not necessary, as the compilers can't handle the types because
			//they're only generated when the UserControl is hit.
			return "System.Web.UI.UserControl";
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
	}
}
