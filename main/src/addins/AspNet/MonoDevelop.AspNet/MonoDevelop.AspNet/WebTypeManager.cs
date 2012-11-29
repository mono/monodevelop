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
using MonoDevelop.Ide.CodeCompletion;
using MonoDevelop.Core.Assemblies;
using ICSharpCode.NRefactory.TypeSystem;
using MonoDevelop.Ide.TypeSystem;
using ICSharpCode.NRefactory.TypeSystem.Implementation;

namespace MonoDevelop.AspNet
{
	public class WebTypeContext
	{
		const string sysWebAssemblyName = "System.Web, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
		
		public WebTypeContext (AspNetAppProject project)
		{
			if (project == null)
				throw new ArgumentException ("project");
			Project = project;
			SystemWebDom = GetSystemWebDom (project);
			if (Compilation == null)
				throw new InvalidOperationException ("Could not load parse database for project");
		}
		
		public AspNetAppProject Project { get; private set; }
		public ICompilation SystemWebDom { get; private set; }
		public ICompilation Compilation { get { return TypeSystemService.GetCompilation (Project);} }
		
		public TargetFramework TargetFramework {
			get { return Project.TargetFramework; }
		}
		
		//FIXME: this shouldn't be public
		public static ICompilation GetSystemWebDom (AspNetAppProject project)
		{
			return GetSystemWebDom (project.TargetRuntime, project.TargetFramework);
		}
		
		static ICompilation GetSystemWebDom (TargetRuntime runtime, TargetFramework targetFramework)
		{
			string file = runtime.AssemblyContext.GetAssemblyNameForVersion (sysWebAssemblyName, targetFramework);
			if (string.IsNullOrEmpty (file))
				throw new Exception ("System.Web assembly name not found for framework " + targetFramework.Id);
			file = runtime.AssemblyContext.GetAssemblyLocation (file, targetFramework);
			if (string.IsNullOrEmpty (file))
				throw new Exception ("System.Web assembly file not found for framework " + targetFramework.Id);
			var dom = new SimpleCompilation (TypeSystemService.LoadAssemblyContext (runtime, targetFramework, file), new IAssemblyReference[] {
				TypeSystemService.LoadAssemblyContext (runtime, targetFramework, typeof (object).Assembly.Location)
			});
			if (dom == null)
				throw new Exception ("System.Web parse database not found for framework " + targetFramework.Id + " file '" + file + "'");
			return dom;
		}
		
		#region Assembly resolution
		
		Dictionary<string,ICompilation> cachedDoms = new Dictionary<string, ICompilation> ();
				
		public ICompilation ResolveAssembly (string assemblyName)
		{
			ICompilation dom;
			if (!cachedDoms.TryGetValue (assemblyName, out dom)) {
				cachedDoms [assemblyName] = dom = Project.ResolveAssemblyDom (assemblyName);
				if (dom == null)
					LoggingService.LogWarning ("Failed to obtain completion database for '{0}'", assemblyName);
			}
			return dom;
		}
		
		#endregion
				
		public IType GetRegisteredType (string webDirectory, string tagPrefix, string tagName)
		{
			//global control registration not possible in ASP.NET 1.1
			if (TargetFramework.ClrVersion == MonoDevelop.Core.ClrVersion.Net_1_1)
				return null;
			
			//read the web.config files at each level
			//look up a level if a result not found until we hit the project root
			foreach (var info in Project.RegistrationCache.GetControlsForPath (webDirectory)) {
				if (!info.PrefixMatches (tagPrefix))
					continue;
				if (info.IsAssembly) {
					var dom = ResolveAssembly (info.Assembly);
					if (dom == null)
						continue;
					
					var type = AssemblyTypeLookup (dom, info.Namespace, tagName);
					if (type != null)
							return type;
				}
				if (info.IsUserControl && info.NameMatches (tagName)) {
					var type = GetUserControlType (info.Source, info.ConfigFile);
					if (type != null)
							return type;
				}
			}
			
			return GetMachineRegisteredType (tagPrefix, tagName);
		}
		
		public string GetRegisteredTypeName (string webDirectory, string tagPrefix, string tagName)
		{
			IType t = GetRegisteredType (webDirectory, tagPrefix, tagName);
			if (t != null)
				return t.FullName;
			
			//NOTE: we can't just fall through to GetRegisteredType, as we may be able to determine usercontrols
			return null;		
		}
				
		public IEnumerable<CompletionData> GetRegisteredTypeCompletionData (string webDirectory, IType baseType)
		{
			//global control registration not possible in ASP.NET 1.1
			if (TargetFramework.ClrVersion == MonoDevelop.Core.ClrVersion.Net_1_1)
				yield break;
			
			//read the web.config files at each level
			//look up a level if a result not found until we hit the project root
			foreach (var info in Project.RegistrationCache.GetControlsForPath (webDirectory)) {
				if (info.IsAssembly) {
					var dom = ResolveAssembly (info.Assembly);
					if (dom == null)
						continue;
					foreach (var t in ListControlClasses (baseType, dom, info.Namespace))
						yield return new MonoDevelop.AspNet.Parser.AspTagCompletionData (info.TagPrefix + ":", t);
				}
				else if (info.IsUserControl) {
					//TODO: resolve docs
					//IType t = GetUserControlType (project, info.Source, info.ConfigFile);
					//if (t != null)
					yield return new CompletionData (info.TagPrefix + ":" + info.TagName, Gtk.Stock.GoForward);
				}
			}
			
			foreach (CompletionData cd in GetMachineRegisteredTypeCompletionData (baseType))
				yield return cd;
		}
		
		public string GetMachineRegisteredTypeName (string tagPrefix, string tagName)
		{
			var t = GetMachineRegisteredType (tagPrefix, tagName);
			if (t != null)
				return t.FullName;
			return null;			
		}
		
		public IType GetMachineRegisteredType (string tagPrefix, string tagName)
		{
			//check in machine.config
			var config = ConfigurationManager.OpenMachineConfiguration ();
			var pages = (PagesSection) config.GetSection ("system.web/pages");
			
			foreach (TagPrefixInfo tpxInfo in pages.Controls) {
				if (tpxInfo.TagPrefix != tagPrefix)
					continue;
				var dom = ResolveAssembly (tpxInfo.Assembly);
				if (dom == null)
						continue;
				IType type = AssemblyTypeLookup (dom, tpxInfo.Namespace, tagName);
				if (type != null)
					return type;
				//user controls don't make sense in machine.config; ignore them
			}
			return null;
		}
		
		public IEnumerable<MonoDevelop.Ide.CodeCompletion.CompletionData> GetMachineRegisteredTypeCompletionData (IType baseType)
		{
			var config = ConfigurationManager.OpenMachineConfiguration ();
			var pages = (PagesSection) config.GetSection ("system.web/pages");
			
			foreach (TagPrefixInfo tpxInfo in pages.Controls) {
				if (!string.IsNullOrEmpty (tpxInfo.Namespace) && !string.IsNullOrEmpty (tpxInfo.Assembly) && !string.IsNullOrEmpty (tpxInfo.TagPrefix)) {
					var dom = ResolveAssembly (tpxInfo.Assembly);
					if (dom != null)
						foreach (var type in ListControlClasses (baseType, dom, tpxInfo.Namespace))
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
		
		public IType HtmlControlTypeLookup (string tagName, string typeAttribute)
		{
			var str = HtmlControlLookup (tagName, typeAttribute);
			if (str != null) {
				return ReflectionHelper.ParseReflectionName (str).Resolve (SystemWebDom);
			}
			return null;
		}
		
		#endregion
		
		#region System type listings
		
		public static IEnumerable<IType> ListSystemControlClasses (IType baseType, AspNetAppProject project)
		{
			return ListControlClasses (baseType, GetSystemWebDom (project), "System.Web.UI.WebControls");
		}
		
		public static IEnumerable<IType> ListControlClasses (IType baseType, ICompilation database, string namespac)
		{
			if (database == null)
				yield break;
			var baseTypeDefinition = baseType.GetDefinition ();
			//return classes if they derive from system.web.ui.control
			foreach (var type in baseTypeDefinition.GetSubTypeDefinitions ().Where (t => t.Namespace == namespac))
				if (!type.IsAbstract && type.IsPublic)
					yield return type;
			
			if (!baseTypeDefinition.IsAbstract && baseTypeDefinition.IsPublic && baseTypeDefinition.Namespace == namespac) {
				yield return baseType;
			}
		}
		
		#endregion
		
		#region Control type lookups
		
		public string SystemTypeNameLookup (string tagName)
		{
			IType cls = SystemTypeLookup (tagName);
			return cls != null? cls.FullName : null;
		}
		
		public IType SystemTypeLookup (string tagName)
		{
			return AssemblyTypeLookup (SystemWebDom, "System.Web.UI.WebControls", tagName);
		}
		
		public static string AssemblyTypeNameLookup (ICompilation assemblyDatabase, string namespac, string tagName)
		{
			var cls = AssemblyTypeLookup (assemblyDatabase, namespac, tagName);
			return cls != null? cls.FullName : null;
		}
		
		public static IType AssemblyTypeLookup (ICompilation assemblyDatabase, string namespac, string tagName)
		{
			return (assemblyDatabase == null)
				? null
				: assemblyDatabase.MainAssembly.GetTypeDefinition (namespac, tagName, 0);
		}
		
		#endregion
		
		public string GetControlPrefix (string webDirectory, IType control)
		{
			if (control.Namespace == "System.Web.UI.WebControls")
				return "asp";
			else if (control.Namespace == "System.Web.UI.HtmlControls")
				return string.Empty;
			
			//todo: handle user controls
			foreach (var info in Project.RegistrationCache.GetControlsForPath (webDirectory)) {
				if (info.IsAssembly && info.Namespace == control.Namespace) {
					var dom = ResolveAssembly (info.Assembly);
					if (dom != null && AssemblyTypeLookup (dom, info.Namespace, control.Name) != null)
						return info.TagPrefix;
				}
			}
			
			//machine.config
			var config = ConfigurationManager.OpenMachineConfiguration ();
			var pages = (PagesSection) config.GetSection ("system.web/pages");
			foreach (TagPrefixInfo tpxInfo in pages.Controls)
				if (!string.IsNullOrEmpty (tpxInfo.Namespace) && !string.IsNullOrEmpty (tpxInfo.TagPrefix) 
				    && control.Namespace == tpxInfo.Namespace)
					return tpxInfo.TagPrefix;
			
			return null;
		}
		
		public string GetUserControlTypeName (string virtualPath, string relativeToFile)
		{
			string absolute = Project.VirtualToLocalPath (virtualPath, relativeToFile);
			var typeName = Project.GetCodebehindTypeName (absolute);
			return typeName ?? "System.Web.UI.UserControl";
		}
		
		public IType GetUserControlType ( string virtualPath, string relativeToFile)
		{
			string absolute = Project.VirtualToLocalPath (virtualPath, relativeToFile);
			var type = Project.GetCodebehindType (absolute);
			return type ?? AssemblyTypeLookup (SystemWebDom, "System.Web.UI", "UserControl");
		}
		
		#region Global control registration tracking
		
		static XmlTextReader GetConfigReader (string configFile)
		{
			IEditableTextFile textFile = 
				MonoDevelop.Ide.TextFileProvider.Instance.GetEditableTextFile (configFile);
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
