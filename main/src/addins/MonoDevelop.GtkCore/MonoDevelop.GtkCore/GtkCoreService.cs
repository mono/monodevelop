//
// GtkCoreService.cs
//
// Author:
//   Lluis Sanchez Gual
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

using System;
using System.IO;
using System.Xml;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.CodeDom;
using System.CodeDom.Compiler;

using MonoDevelop.Core;
using MonoDevelop.Core.Gui;
using MonoDevelop.Projects;
using MonoDevelop.Projects.CodeGeneration;
using MonoDevelop.Projects.Serialization;
using MonoDevelop.Projects.Parser;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Components.Commands;

namespace MonoDevelop.GtkCore
{
	public class GtkCoreService
	{
		static string[] supportedGtkVersions;
		static string defaultGtkVersion;
		
		internal static void Initialize ()
		{
			FileService.FileChanged += new EventHandler<FileEventArgs> (OnFileChanged);
			Runtime.SystemAssemblyService.PackagesChanged += delegate {
				supportedGtkVersions = null;
			};
		}
		
		public static string[] SupportedGtkVersions {
			get {
				FindSupportedGtkVersions ();
				return supportedGtkVersions;
			}
		}
		
		public static string DefaultGtkVersion {
			get {
				FindSupportedGtkVersions ();
				return defaultGtkVersion; 
			}
		}
		
		public static bool SupportsGtkDesigner (Project project)
		{
			DotNetProject dp = project as DotNetProject;
			if (dp == null || dp.LanguageBinding == null || dp.LanguageBinding.GetCodeDomProvider () == null)
				return false;
			
			RefactorOperations ops = RefactorOperations.AddField | RefactorOperations.AddMethod | RefactorOperations.RenameField;
			CodeRefactorer cref = IdeApp.Workspace.GetCodeRefactorer (project.ParentSolution);
			return cref.LanguageSupportsOperation (dp.LanguageBinding.Language, ops); 
		}
		
		static void FindSupportedGtkVersions ()
		{
			if (supportedGtkVersions == null) {
				List<string> versions = new List<string> ();
				foreach (SystemPackage p in Runtime.SystemAssemblyService.GetPackages ()) {
					if (p.Name == "gtk-sharp-2.0") {
						versions.Add (p.Version);
						if (p.Version.StartsWith ("2.8"))
							defaultGtkVersion = p.Version;
					}
				}
				versions.Sort ();
				supportedGtkVersions = versions.ToArray ();
				if (defaultGtkVersion == null && supportedGtkVersions.Length > 0)
					defaultGtkVersion = supportedGtkVersions [0];
			}
		}
		
		public static event GtkSupportEvent GtkSupportChanged;
		
		public static GtkDesignInfo GetGtkInfo (Project project)
		{
			if (!(project is DotNetProject))
				return null;

			IExtendedDataItem item = (IExtendedDataItem) project;
			GtkDesignInfo info = (GtkDesignInfo) item.ExtendedProperties ["GtkDesignInfo"];
			if (info == null)
				return null;

			info.Bind ((DotNetProject) project);
			return info;
		}
		
		internal static GtkDesignInfo EnableGtkSupport (Project project)
		{
			GtkDesignInfo info = GetGtkInfo (project);
			if (info != null)
				return info;

			info = new GtkDesignInfo ((DotNetProject) project);
			info.TargetGtkVersion = GtkCoreService.DefaultGtkVersion;
			info.UpdateGtkFolder ();
			
			if (GtkSupportChanged != null)
				GtkSupportChanged (project, true);

			return info;
		}
		
		internal static void DisableGtkSupport (Project project)
		{
			GtkDesignInfo info = GetGtkInfo (project);
			if (info == null)
				return;

			project.ExtendedProperties ["GtkDesignInfo"] = null;
			info.Dispose ();
			
			if (GtkSupportChanged != null)
				GtkSupportChanged (project, false);
		}
		
		internal static bool SupportsPartialTypes (DotNetProject project)
		{
			return project.UsePartialTypes;
		}
		
		static void OnFileChanged (object s, FileEventArgs args)
		{
			if (!IdeApp.Workspace.IsOpen)
				return;

			foreach (Project project in IdeApp.Workspace.GetAllProjects ()) {
				if (!project.IsFileInProject (args.FileName))
					continue;
					
				GtkDesignInfo info = GtkCoreService.GetGtkInfo (project);
				if (info == null)
					continue;

				IdeApp.Workspace.ParserDatabase.UpdateFile (project, args.FileName, null);
				foreach (IClass cls in info.GetExportedClasses ()) {
					if (cls.Region.FileName == args.FileName)
						UpdateObjectsFile (project, cls, null);
				}
			}
		}
		
		internal static void UpdateObjectsFile (Project project)
		{
			GtkDesignInfo info = GtkCoreService.GetGtkInfo (project);
			if (info == null) return;
			info.UpdateGtkFolder ();

			XmlDocument doc = new XmlDocument ();
			if (File.Exists (info.ObjectsFile))
				doc.Load (info.ObjectsFile);
			
			// Add or update the selected classes

			ArrayList exported = new ArrayList ();
			foreach (IClass cls in info.GetExportedClasses ()) {
				UpdateClass (project, doc, cls, null);
				exported.Add (cls.FullyQualifiedName);
			}
				
			// Remove from the file the unselected classes
			
			ArrayList toDelete = new ArrayList ();
			
			foreach (XmlElement elem in doc.SelectNodes ("objects/object")) {
				string name = elem.GetAttribute ("type");
				if (!exported.Contains (name))
					toDelete.Add (elem);
			}
			
			foreach (XmlElement elem in toDelete)
				elem.ParentNode.RemoveChild (elem);
			
			doc.Save (info.ObjectsFile);
		}
		
		internal static void UpdateProjectName (Project project, string oldName, string newName)
		{
			GtkDesignInfo info = GtkCoreService.GetGtkInfo (project);
			if (info == null) return;

			XmlDocument doc = new XmlDocument ();
			if (!File.Exists (info.ObjectsFile))
				return;
			
			doc.Load (info.ObjectsFile);
			
			bool modified = false;
			
			foreach (XmlElement elem in doc.SelectNodes ("objects/object")) {
				string cat = elem.GetAttribute ("palette-category");
				if (cat == oldName) {
					elem.SetAttribute ("palette-category", newName);
					modified = true;
				}
			}
			
			if (modified)
				doc.Save (info.ObjectsFile);
		}
		
		static void UpdateObjectsFile (Project project, IClass widgetClass, IClass wrapperClass)
		{
			GtkDesignInfo info = GtkCoreService.GetGtkInfo (project);
			info.UpdateGtkFolder ();
			
			XmlDocument doc = new XmlDocument ();
			if (File.Exists (info.ObjectsFile))
				doc.Load (info.ObjectsFile);
				
			UpdateClass (project, doc, widgetClass, wrapperClass);
			
			doc.Save (info.ObjectsFile);
		}
		
		static void UpdateClass (Project project, XmlDocument doc, IClass widgetClass, IClass wrapperClass)
		{
			IParserContext ctx = IdeApp.Workspace.ParserDatabase.GetProjectParserContext (project);
			string typeName = widgetClass.FullyQualifiedName;
			XmlElement objectElem = (XmlElement) doc.SelectSingleNode ("objects/object[@type='" + typeName + "']");
			
			if (objectElem == null) {
			
				// The widget class is not yet in the XML file. Create an element for it.
				objectElem = doc.CreateElement ("object");
				objectElem.SetAttribute ("type", typeName);
				objectElem.SetAttribute ("palette-category", project.Name);
				objectElem.SetAttribute ("allow-children", "false");
				if (wrapperClass != null)
					objectElem.SetAttribute ("wrapper", wrapperClass.FullyQualifiedName);
				
				// By default add a reference to Gtk.Widget properties and events
				XmlElement itemGroups = objectElem.OwnerDocument.CreateElement ("itemgroups");
				objectElem.AppendChild (itemGroups);
				
				itemGroups = objectElem.OwnerDocument.CreateElement ("signals");
				objectElem.AppendChild (itemGroups);
				
				objectElem.SetAttribute ("base-type", GetBaseType (widgetClass, project));
				doc.DocumentElement.AppendChild (objectElem);
			}
			
			MergeObject (project, ctx, objectElem, widgetClass, wrapperClass);
		}
		
		static string GetBaseType (IClass widgetClass, Project project)
		{
			GtkDesignInfo info = GetGtkInfo (project);
			string[] types = info.GuiBuilderProject.SteticProject.GetWidgetTypes ();
			Hashtable typesHash = new Hashtable ();
			foreach (string t in types)
				typesHash [t] = t;
				
			IParserContext pctx = IdeApp.Workspace.ParserDatabase.GetProjectParserContext (project);
			string ret = GetBaseType (widgetClass, pctx, typesHash);
			return ret ?? "Gtk.Widget";
		}
		
		static string GetBaseType (IClass cls, IParserContext pctx, Hashtable typesHash)
		{
			foreach (IReturnType bt in cls.BaseTypes) {
				if (typesHash.Contains (bt.FullyQualifiedName))
					return bt.FullyQualifiedName;
			}

			foreach (IReturnType bt in cls.BaseTypes) {
				IClass bcls = pctx.GetClass (bt.FullyQualifiedName, true, true);
				if (bcls != null) {
					string ret = GetBaseType (bcls, pctx, typesHash);
					if (ret != null)
						return ret;
				}
			}
			return null;
		}
		
		static void MergeObject (Project project, IParserContext ctx, XmlElement objectElem, IClass widgetClass, IClass wrapperClass)
		{
			string topType = GetBaseType (widgetClass, project);
			
			if (!widgetClass.IsPublic)
				objectElem.SetAttribute ("internal", "true");
			else
				objectElem.RemoveAttribute ("internal");

			ListDictionary properties = new ListDictionary ();
			ListDictionary events = new ListDictionary ();
			
			CollectMembers (ctx, widgetClass, true, topType, properties, events);
			if (wrapperClass != null)
				CollectMembers (ctx, wrapperClass, false, null, properties, events);
			
			foreach (IProperty prop in properties.Values)
				MergeProperty (objectElem, prop);
			
			foreach (IEvent ev in events.Values)
				MergeEvent (objectElem, ev);
			
			// Remove old properties
			
			ArrayList toDelete = new ArrayList ();
			foreach (XmlElement xprop in objectElem.SelectNodes ("itemgroups/itemgroup/property")) {
				if (!properties.Contains (xprop.GetAttribute ("name")))
					toDelete.Add (xprop);
			}
			
			// Remove old signals
			
			foreach (XmlElement xevent in objectElem.SelectNodes ("signals/itemgroup/signal")) {
				if (!events.Contains (xevent.GetAttribute ("name")))
					toDelete.Add (xevent);
			}
			
			foreach (XmlElement el in toDelete) {
				XmlElement pe = (XmlElement) el.ParentNode;
				pe.RemoveChild (el);
				if (pe.ChildNodes.Count == 0)
					pe.ParentNode.RemoveChild (pe);
			}
		}
		
		static void CollectMembers (IParserContext ctx, IClass cls, bool inherited, string topType, ListDictionary properties, ListDictionary events)
		{
			if (cls.FullyQualifiedName == topType)
				return;

			foreach (IProperty prop in cls.Properties)
				if (IsBrowsable (ctx, prop))
					properties [prop.Name] = prop;

			foreach (IEvent ev in cls.Events)
				if (IsBrowsable (ctx, ev))
					events [ev.Name] = ev;
					
			if (inherited) {
				foreach (IReturnType bt in cls.BaseTypes) {
					IClass bcls = ctx.GetClass (bt.FullyQualifiedName, true, true);
					if (bcls != null && bcls.ClassType != ClassType.Interface)
						CollectMembers (ctx, bcls, true, topType, properties, events);
				}
			}
		}
		
		static void MergeProperty (XmlElement objectElem, IProperty prop)
		{
			XmlElement itemGroups = objectElem ["itemgroups"];
			if (itemGroups == null) {
				itemGroups = objectElem.OwnerDocument.CreateElement ("itemgroups");
				objectElem.AppendChild (itemGroups);
			}
			
			string cat = GetCategory (prop);
			XmlElement itemGroup = GetItemGroup (prop.DeclaringType, itemGroups, cat, "Properties");
			
			XmlElement propElem = (XmlElement) itemGroup.SelectSingleNode ("property[@name='" + prop.Name + "']");
			if (propElem == null) {
				propElem = itemGroup.OwnerDocument.CreateElement ("property");
				propElem.SetAttribute ("name", prop.Name);
				itemGroup.AppendChild (propElem);
			}
		}
		
		static void MergeEvent (XmlElement objectElem, IEvent evnt)
		{
			XmlElement itemGroups = objectElem ["signals"];
			if (itemGroups == null) {
				itemGroups = objectElem.OwnerDocument.CreateElement ("signals");
				objectElem.AppendChild (itemGroups);
			}
			
			string cat = GetCategory (evnt);
			XmlElement itemGroup = GetItemGroup (evnt.DeclaringType, itemGroups, cat, "Signals");
			
			XmlElement signalElem = (XmlElement) itemGroup.SelectSingleNode ("signal[@name='" + evnt.Name + "']");
			if (signalElem == null) {
				signalElem = itemGroup.OwnerDocument.CreateElement ("signal");
				signalElem.SetAttribute ("name", evnt.Name);
				itemGroup.AppendChild (signalElem);
			}
		}
		
		static XmlElement GetItemGroup (IClass cls, XmlElement itemGroups, string cat, string groupName)
		{
			XmlElement itemGroup;
			
			if (cat != "")
				itemGroup = (XmlElement) itemGroups.SelectSingleNode ("itemgroup[@name='" + cat + "']");
			else
				itemGroup = (XmlElement) itemGroups.SelectSingleNode ("itemgroup[(not(@name) or @name='') and not(@ref)]");
			
			if (itemGroup == null) {
				itemGroup = itemGroups.OwnerDocument.CreateElement ("itemgroup");
				if (cat != null && cat != "") {
					itemGroup.SetAttribute ("name", cat);
					itemGroup.SetAttribute ("label", cat);
				} else
					itemGroup.SetAttribute ("label", cls.Name + " " + groupName);
				itemGroups.AppendChild (itemGroup);
			}
			return itemGroup;
		}
		
		static string GetCategory (IMember member)
		{
			foreach (IAttributeSection section in member.Attributes) {
				foreach (IAttribute at in section.Attributes) {
					if (at.Name == "Category" || at.Name == "CategoryAttribute" || at.Name == "System.ComponentModel.CategoryAttribute"|| at.Name == "System.ComponentModel.Category") {
						if (at.PositionalArguments != null && at.PositionalArguments.Length > 0) {
							CodePrimitiveExpression exp = at.PositionalArguments [0] as CodePrimitiveExpression;
							if (exp != null && exp.Value != null)
								return exp.Value.ToString ();
						}
					}
				}
			}
			return "";
		}
		
		static bool IsBrowsable (IParserContext ctx, IMember member)
		{
			if (!member.IsPublic)
				return false;

			IProperty prop = member as IProperty;
			if (prop != null) {
				if (!prop.CanGet || !prop.CanSet)
					return false;
				if (!IsTypeSupported (ctx, prop.ReturnType))
					return false;
			}

			foreach (IAttributeSection section in member.Attributes) {
				foreach (IAttribute at in section.Attributes) {
					if (at.Name == "Browsable" || at.Name == "BrowsableAttribute" || at.Name == "System.ComponentModel.BrowsableAttribute"|| at.Name == "System.ComponentModel.Browsable") {
						if (at.PositionalArguments != null && at.PositionalArguments.Length > 0) {
							CodePrimitiveExpression exp = at.PositionalArguments [0] as CodePrimitiveExpression;
							if (exp != null && exp.Value != null && exp.Value is bool) {
								return (bool) exp.Value;
							}
						}
					}
				}
			}
			return true;
		}
		
		static public IClass[] GetExportableClasses (Project project)
		{
			IParserContext pctx = IdeApp.Workspace.ParserDatabase.GetProjectParserContext (project);
			ArrayList list = new ArrayList ();
			foreach (IClass cls in pctx.GetProjectContents ())
				if (IsWidget (cls, pctx)) list.Add (cls);

			return (IClass[]) list.ToArray (typeof(IClass));
		}
		
		static bool IsWidget (IClass cls, IParserContext pctx)
		{
			foreach (IReturnType bt in cls.BaseTypes) {
				if (bt.FullyQualifiedName == "Gtk.Widget")
					return true;
			}

			foreach (IReturnType bt in cls.BaseTypes) {
				IClass bcls = pctx.GetClass (bt.FullyQualifiedName, true, true);
				if (bcls != null) {
					if (IsWidget (bcls, pctx))
						return true;
				}
			}
			return false;
		}
		
		static bool IsTypeSupported (IParserContext ctx, IReturnType rtype)
		{
			switch (rtype.FullyQualifiedName) {
				case "Gtk.Adjustment":
				case "System.DateTime":
				case "System.TimeSpan":
				case "System.String":
				case "System.Boolean":
				case "System.Char":
				case "System.SByte":
				case "System.Byte":
				case "System.Int16":
				case "System.UInt16":
				case "System.Int32":
				case "System.UInt32":
				case "System.Int64":
				case "System.UInt64":
				case "System.Single":
				case "System.Double":
				case "System.Decimal":
					return true;
			}
			return false;
		}
	}
	
	class GtkCoreStartupCommand: CommandHandler
	{
		protected override void Run()
		{
			GtkCoreService.Initialize ();
		}
	}
	
	public delegate void GtkSupportEvent (Project project, bool enabled);
}
