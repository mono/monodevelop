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

using MonoDevelop.Ide.CodeCompletion;
using MonoDevelop.Xml.StateEngine;
using ICSharpCode.NRefactory.TypeSystem;
using MonoDevelop.Ide.TypeSystem; 

namespace MonoDevelop.Moonlight
{
	
	
	public class MoonlightEditorExtension : MonoDevelop.XmlEditor.Gui.BaseXmlEditorExtension
	{
		
		public MoonlightEditorExtension ()
		{
		}
		
		#region Code completion
		
//		static ITypeResolveContext GetMLDom (MoonlightProject project)
//		{
//			return TypeSystemService.GetAssemblyDom (
//				MonoDevelop.Core.Runtime.SystemAssemblyService.GetAssemblyNameForVersion (
//					"System.Windows", GetProjectTargetFramework (project)));
//		}
		
		public static IEnumerable<IType> ListControlClasses (ICompilation database, INamespace namespac)
		{
			if (database == null)
				yield break;
			
			var swd = database.LookupType ("System.Windows", "DependencyObject");
			
			//return classes if they derive from system.web.ui.control
			foreach (var cls in namespac.Types) {
				if (cls != null && !cls.IsAbstract && cls.IsPublic && cls.IsBaseType (swd))
					yield return cls;
			}
		}
		
		static IEnumerable<T> GetUniqueMembers<T> (IEnumerable<T> members) where T : IMember
		{
			Dictionary <string, bool> existingItems = new Dictionary<string,bool> ();
			foreach (T item in members) {
				if (existingItems.ContainsKey (item.Name))
					continue;
				existingItems[item.Name] = true;
				yield return item;
			}
		}
		
		static void AddControlMembers (CompletionDataList list, IType controlClass, 
		                               Dictionary<string, string> existingAtts)
		{
			//add atts only if they're not already in the tag
			foreach (IProperty prop in GetUniqueMembers<IProperty> (controlClass.GetProperties ()))
				if (prop.IsPublic && (existingAtts == null || !existingAtts.ContainsKey (prop.Name)))
					list.Add (prop.Name, prop.GetStockIcon (), prop.Documentation);
			
			//similarly add events
			foreach (var eve 
			    in GetUniqueMembers<IEvent> (controlClass.GetEvents ())) {
				string eveName = eve.Name;
				if (eve.IsPublic && (existingAtts == null || !existingAtts.ContainsKey (eveName)))
					list.Add (eveName, eve.GetStockIcon (), eve.Documentation);
			}
		}
		
		ICompilation GetDb ()
		{
			return Document.Compilation;
		}
		
		void GetType (IAttributedXObject attributedOb, Action<IType, ICompilation> action)
		{
			var database = GetDb ();
			if (database == null)
				return;
			foreach (string namespc in namespaces) {
				var controlType = database.MainAssembly.GetTypeDefinition (namespc, attributedOb.Name.Name, 0);
				if (controlType != null) {
					action (controlType, database);
					break;
				}
			}
		}
		
		string[] namespaces = { "System.Windows.Media", "System.Windows.Media.Animation",
			"System.Windows.Controls", "System.Windows.Shapes" };
		
		protected override void GetElementCompletions (CompletionDataList list)
		{
			base.GetElementCompletions (list);
			var database = GetDb ();
			if (database == null)
				return;
			
			IType type = database.LookupType ("System.Windows", "DependencyObject");
			if (type == null)
				return;
			
			foreach (string namespc in namespaces) {
				INamespace ns = database.RootNamespace;
				foreach (var sn in namespc.Split ('.')) {
					ns = ns.GetChildNamespace (sn);
				}
				foreach (IType t in ListControlClasses (database, ns))
					list.Add (t.Name, Gtk.Stock.GoForward, t.GetDefinition ().Documentation);
			}
		}
		
//		static MonoDevelop.Core.TargetFramework GetProjectTargetFramework (MoonlightProject project)
//		{
//			return project == null? MonoDevelop.Core.TargetFramework.Default : project.TargetFramework;
//		}
		
		protected override CompletionDataList GetAttributeCompletions (IAttributedXObject attributedOb, 
			Dictionary<string, string> existingAtts)
		{
			var list = base.GetAttributeCompletions (attributedOb, existingAtts) ?? new CompletionDataList ();
			if (!existingAtts.ContainsKey ("x:Name"))
				list.Add ("x:Name");
			
			GetType (attributedOb, delegate (IType type, ICompilation dom) {
				AddControlMembers (list, type, existingAtts);
			});
			return list.Count > 0? list : null;
		}
		
		protected override CompletionDataList GetAttributeValueCompletions (IAttributedXObject attributedOb, XAttribute att)
		{
			var list = base.GetAttributeValueCompletions (attributedOb, att) ?? new CompletionDataList ();
			var ctx = document.Compilation;
			GetType (attributedOb, delegate (IType type, ICompilation dom) {
				foreach (IProperty prop in type.GetProperties ()) {
					if (prop.Name != att.Name.FullName)
						continue;
					
					//boolean completion
					if (prop.ReturnType.Equals (ctx.FindType (typeof (bool)))) {
						list.Add ("true", "md-literal");
						list.Add ("false", "md-literal");
						return;
					}
					
					//color completion
					if (prop.ReturnType.ReflectionName == "System.Windows.Media.Color") {
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
					var retCls = prop.ReturnType;
					if (retCls != null && retCls.Kind == TypeKind.Enum) {
						foreach (var enumVal in retCls.GetFields ())
							if (enumVal.IsPublic && enumVal.IsStatic)
								list.Add (enumVal.Name, "md-literal", enumVal.Documentation);
						return;
					}
				}
			});
			return list.Count > 0? list : null;
		}
		
		#endregion
		
	}
}
