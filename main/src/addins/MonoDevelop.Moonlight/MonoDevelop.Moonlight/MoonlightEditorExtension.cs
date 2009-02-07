// 
// MoonlightEditorExtension.cs
// 
// Author:
//   Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (C) 2008 Novell, Inc (http://www.novell.com)
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

using MonoDevelop.Projects.Dom;
using MonoDevelop.Projects.Dom.Parser;
using MonoDevelop.Projects.Gui.Completion;
using MonoDevelop.Xml.StateEngine; 

namespace MonoDevelop.Moonlight
{
	
	
	public class MoonlightEditorExtension : MonoDevelop.XmlEditor.Gui.BaseXmlEditorExtension
	{
		
		public MoonlightEditorExtension ()
		{
		}
		
		protected override IEnumerable<string> SupportedExtensions {
			get {
				yield return ".xaml";
			}
		}
		
		#region Code completion
		
//		static ProjectDom GetMLDom (MoonlightProject project)
//		{
//			return ProjectDomService.GetAssemblyDom (
//				MonoDevelop.Core.Runtime.SystemAssemblyService.GetAssemblyNameForVersion (
//					"System.Windows", GetProjectTargetFramework (project)));
//		}
		
		public static IEnumerable<IType> ListControlClasses (ProjectDom database, string namespac)
		{
			if (database == null)
				yield break;
			
			DomReturnType swd = new DomReturnType ("System.Windows.DependencyObject");
			
			//return classes if they derive from system.web.ui.control
			foreach (IMember mem in database.GetNamespaceContents (namespac, true, true)) {
				IType cls = mem as IType;
				if (cls != null && !cls.IsAbstract && cls.IsPublic && cls.IsBaseType (swd))
					yield return cls;
			}
		}
		
		static IEnumerable<MonoDevelop.Projects.Dom.IProperty> GetAllProperties (
		    MonoDevelop.Projects.Dom.Parser.ProjectDom projectDatabase,
		    MonoDevelop.Projects.Dom.IType cls)
		{
			foreach (MonoDevelop.Projects.Dom.IType type in projectDatabase.GetInheritanceTree (cls))
				foreach (MonoDevelop.Projects.Dom.IProperty prop in type.Properties)
					yield return prop;
		}
		
		static IEnumerable<MonoDevelop.Projects.Dom.IEvent> GetAllEvents (
		    MonoDevelop.Projects.Dom.Parser.ProjectDom projectDatabase,
		    MonoDevelop.Projects.Dom.IType cls)
		{
			foreach (MonoDevelop.Projects.Dom.IType type in projectDatabase.GetInheritanceTree (cls))
				foreach (MonoDevelop.Projects.Dom.IEvent ev in type.Events)
					yield return ev;
		}
		
		static IEnumerable<T> GetUniqueMembers<T> (IEnumerable<T> members) where T : MonoDevelop.Projects.Dom.IMember
		{
			Dictionary <string, bool> existingItems = new Dictionary<string,bool> ();
			foreach (T item in members) {
				if (existingItems.ContainsKey (item.Name))
					continue;
				existingItems[item.Name] = true;
				yield return item;
			}
		}
		
		static void AddControlMembers (CompletionDataList list,  ProjectDom database, IType controlClass, 
		                               Dictionary<string, string> existingAtts)
		{
			//add atts only if they're not already in the tag
			foreach (IProperty prop in GetUniqueMembers<MonoDevelop.Projects.Dom.IProperty> (GetAllProperties (database, controlClass)))
				if (prop.IsPublic && (existingAtts == null || !existingAtts.ContainsKey (prop.Name)))
					list.Add (prop.Name, prop.StockIcon, prop.Documentation);
			
			//similarly add events
			foreach (MonoDevelop.Projects.Dom.IEvent eve 
			    in GetUniqueMembers<MonoDevelop.Projects.Dom.IEvent> (GetAllEvents (database, controlClass))) {
				string eveName = eve.Name;
				if (eve.IsPublic && (existingAtts == null || !existingAtts.ContainsKey (eveName)))
					list.Add (eveName, eve.StockIcon, eve.Documentation);
			}
		}
		
		ProjectDom GetDb ()
		{
			if (this.Document.Project != null)
				return ProjectDomService.GetProjectDom (this.Document.Project);
			else
				//FIXME: add fallback
				return null;
		}
		
		void GetType (IAttributedXObject attributedOb, Action<IType, ProjectDom> action)
		{
			ProjectDom database = GetDb ();
			if (database == null)
				return;
			foreach (string namespc in namespaces) {
				IType controlType = database.GetType (namespc + "." + attributedOb.Name.Name);
				if (controlType != null) {
					action (controlType, database);
					break;
				}
			}
		}
		
		string[] namespaces = { "System.Windows.Media", "System.Windows.Media.Animation",
			"System.Windows.Controls", "System.Windows.Shapes" };
		
		protected override void GetElementCompletions(CompletionDataList list)
		{
			base.GetElementCompletions (list);
			ProjectDom database = GetDb ();
			if (database == null)
				return;
			
			IType type = database.GetType ("System.Windows.DependencyObject");
			if (type == null)
				return;
			
			foreach (string namespc in namespaces)
				foreach (IType t in ListControlClasses (database, namespc))
					list.Add (t.Name, Gtk.Stock.GoForward, t.Documentation);
		}
		
//		static MonoDevelop.Core.TargetFramework GetProjectTargetFramework (MoonlightProject project)
//		{
//			return project == null? MonoDevelop.Core.TargetFramework.Default : project.TargetFramework;
//		}
		
		protected override void GetAttributeCompletions (CompletionDataList list, IAttributedXObject attributedOb, 
		                                                 Dictionary<string, string> existingAtts)
		{
			base.GetAttributeCompletions (list, attributedOb, existingAtts);
			if (!existingAtts.ContainsKey ("x:Name"))
				list.Add ("x:Name");
			
			GetType (attributedOb, delegate (IType type, ProjectDom dom) {
				AddControlMembers (list, dom, type, existingAtts);
			});
		}
		
		protected override void GetAttributeValueCompletions (CompletionDataList list, IAttributedXObject attributedOb, 
		                                                      XAttribute att)
		{
			base.GetAttributeValueCompletions (list, attributedOb, att);
			
			GetType (attributedOb, delegate (IType type, ProjectDom dom) {
				foreach (IProperty prop in GetAllProperties (dom, type)) {
					if (prop.Name != att.Name.FullName)
						continue;
					
					//boolean completion
					if (prop.ReturnType.FullName == "System.Boolean") {
						list.Add ("true", "md-literal");
						list.Add ("false", "md-literal");
						return;
					}
					
					//color completion
					if (prop.ReturnType.FullName == "System.Windows.Media.Color") {
						System.Drawing.ColorConverter conv = new System.Drawing.ColorConverter ();
						foreach (System.Drawing.Color c in conv.GetStandardValues (null)) {
							if (c.IsSystemColor)
								continue;
							string hexcol = string.Format ("#{0:x2}{1:x2}{2:x2}", c.R, c.G, c.B);
							list.Add (c.Name, hexcol);
						}
						return;
					}
					
					//enum completion
					MonoDevelop.Projects.Dom.IType retCls = dom.GetType (prop.ReturnType);
					if (retCls != null && retCls.ClassType == MonoDevelop.Projects.Dom.ClassType.Enum) {
						foreach (MonoDevelop.Projects.Dom.IField enumVal in retCls.Fields)
							if (enumVal.IsPublic && enumVal.IsStatic)
								list.Add (enumVal.Name, "md-literal", enumVal.Documentation);
						return;
					}
				}
			});
		}
		
		#endregion
		
	}
}
