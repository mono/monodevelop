// 
// ClassNavigationWindow.cs
//  
// Author:
//       nikhil <${AuthorEmail}>
// 
// Copyright (c) 2010 nikhil
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
// TypeNavigationWindow.cs
//  
// Author:
//       Nikhil Sarda <diff.operator@gmail.com>
// 
// Copyright (c) 2010 Nikhil Sarda
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
using System;
using System.Collections.Generic;
using System.Linq;
using MonoDevelop.Projects.Dom;
using Gtk;
using MonoDevelop.Components;
using MonoDevelop.Ide.CodeCompletion;
using Gdk;
using MonoDevelop.Core;
using MonoDevelop.Ide;
using MonoDevelop.Components.DockToolbars;
namespace MonoDevelop.Navigation
{
	public partial class ClassNavigationWindow : Gtk.Window
	{
		TreeView navigationTreeView, membersTreeView;
		TreeStore navigationStore, membersStore;
		TreeModelFilter memberFilter;
		
		const string showOverriden = "MonoDevelop.Navigation.ShowOverriden";
		const string showInherited = "MonoDevelop.Navigation.ShowInherited";
		
		enum DataColumns
		{
			Icon,
			Name,
			Reference
		}
		
		ClassNavigationWindow (DomType type, CodeCompletionContext completionContext) : base(Gtk.WindowType.Toplevel)
		{
			this.Decorated = false;
			this.Build ();
			WindowTransparencyDecorator.Attach (this);
			
			navigationTreeView = new TreeView();
			
			navigationStore = new TreeStore (typeof (Pixbuf), // Icon
		                                 typeof (string), // type name
		                                 typeof (IType)  // reference to objects
		                                 );
			
			TreeViewColumn iconCol, typenameCol; 
			
			navigationTreeView.RulesHint = false;
			navigationTreeView.Model = navigationStore;
			navigationTreeView.HeadersVisible = false;

			iconCol = new TreeViewColumn (GettextCatalog.GetString ("Icon"), new Gtk.CellRendererPixbuf (), "pixbuf", 0);
			iconCol.Expand = false;
			iconCol.Resizable = false;
			navigationTreeView.AppendColumn (iconCol);
			
			typenameCol = new TreeViewColumn (GettextCatalog.GetString ("Type name"), new Gtk.CellRendererText(), "text", 1);
			typenameCol.Expand = true;
			typenameCol.Resizable = true;
			navigationTreeView.AppendColumn (typenameCol);
			
			AddData(navigationStore, type);
			
			navigationTreeView.RowActivated += delegate {
				Gtk.TreeIter selectedIter;
				if (navigationTreeView.Selection.GetSelected (out selectedIter)) {
					var rowSelectType = (DomType)navigationStore.GetValue (selectedIter, 2);
					if (rowSelectType != null) {
						IdeApp.ProjectOperations.JumpToDeclaration (rowSelectType);
					}
				}
			};
			
			membersTreeView = new TreeView();
			
			membersStore = new TreeStore (typeof (Pixbuf),
			                              typeof (string),
			                              typeof (IType)
			                              );
			
			TreeViewColumn memberIconCol, memberNameCol;
			
			membersTreeView.RulesHint = false;
			membersTreeView.HeadersVisible = false;

			memberIconCol = new TreeViewColumn (GettextCatalog.GetString ("Icon"), new Gtk.CellRendererPixbuf (), "pixbuf", 0);
			memberIconCol.Expand = false;
			memberIconCol.Resizable = false;
			membersTreeView.AppendColumn (memberIconCol);
			
			memberNameCol = new TreeViewColumn (GettextCatalog.GetString ("Type name"), new Gtk.CellRendererText(), "text", 1);
			memberNameCol.Expand = false;
			memberNameCol.Resizable = false;
			membersTreeView.AppendColumn (memberNameCol);
			
			navigationTreeView.CursorChanged += delegate {
				membersStore.Clear ();
				Gtk.TreeIter selectedIter;
				if (navigationTreeView.Selection.GetSelected (out selectedIter)) {
					var rowSelectType = (DomType)navigationStore.GetValue (selectedIter, 2);
					foreach (var member in rowSelectType.Members) {
						Gdk.Pixbuf memberIcon;
						switch (member.MemberType) {
							case MemberType.Event:
								memberIcon = ImageService.GetPixbuf ("md-event", Gtk.IconSize.Menu);
								break;
							case MemberType.Field:
								memberIcon = ImageService.GetPixbuf ("md-field", Gtk.IconSize.Menu);
								break;
							case MemberType.Method:
								memberIcon = ImageService.GetPixbuf ("md-method", Gtk.IconSize.Menu);
								break;
							case MemberType.Property:
								memberIcon = ImageService.GetPixbuf("md-property", Gtk.IconSize.Menu);
								break;
							case MemberType.Type:
								memberIcon = ImageService.GetPixbuf("md-class", Gtk.IconSize.Menu);
								break;
							default:
								memberIcon = ImageService.GetPixbuf("md-parameter", Gtk.IconSize.Menu);
								break;
						}
						membersStore.AppendValues (memberIcon, member.Name, member);
					}
				}
			};
			
			membersTreeView.RowActivated += delegate {
				Gtk.TreeIter selectedIter;
				if (membersTreeView.Selection.GetSelected (out selectedIter)) {
					var rowSelectType = (IMember)memberFilter.GetValue (selectedIter, 2);
					if (rowSelectType != null) {
						IdeApp.ProjectOperations.JumpToDeclaration (rowSelectType);
					}
				}
			};
			
			this.inheritedToggle.Image = ImageService.GetImage ("md-method", Gtk.IconSize.Menu);
			this.inheritedToggle.TooltipText = GettextCatalog.GetString ("Show inherited members");
			this.inheritedToggle.Toggled += FilterChanged;
			
			this.overridenToggle.Image = ImageService.GetImage ("md-protected-method", Gtk.IconSize.Menu);
			this.overridenToggle.TooltipText = GettextCatalog.GetString ("Show overriden members");
			this.overridenToggle.Toggled += FilterChanged;
			
			TreeModelFilterVisibleFunc filterFunct = new TreeModelFilterVisibleFunc (FilterMembers);
			memberFilter = new TreeModelFilter (membersStore, null);
			memberFilter.VisibleFunc = filterFunct;
			membersTreeView.Model = memberFilter;
			
			this.navigationWindow.Child = navigationTreeView;
			this.memberWindow.Child = membersTreeView;
			
			int x = completionContext.TriggerXCoord;
			int y = completionContext.TriggerYCoord;

			int w, h;
			GetSize (out w, out h);
			
			int myMonitor = Screen.GetMonitorAtPoint (x, y);
			Gdk.Rectangle geometry = Screen.GetMonitorGeometry (myMonitor);

			if (x + w > geometry.Right)
				x = geometry.Right - w;

			if (y + h > geometry.Bottom)
				y = y - completionContext.TriggerTextHeight - h;
			
			Move (x, y);
		}
		
		void FilterChanged (object sender, EventArgs e)
		{
			PropertyService.Set (showInherited, inheritedToggle.Active);
			PropertyService.Set (showOverriden, overridenToggle.Active);
			memberFilter.Refilter ();
		}
		
		void AddData (TreeStore store, DomType domType)
		{
			Stack<IType> baseTypes = new Stack<IType>();
			IType curType = domType;
			while (true) {
				if ((curType.BaseType == null || curType.BaseTypes == null) || 
				    (curType.FullName == "System.Object") || 
				    (curType.BaseTypes.Where(x => curType.SourceProjectDom.GetType(x).ClassType == ClassType.Class).Count() <= 0))
					break;
				baseTypes.Push (curType);
				foreach (IReturnType type in curType.BaseTypes) {
					IType resolvedType = curType.SourceProjectDom.GetType(type);
					if (resolvedType != null) {
						if (resolvedType.ClassType == ClassType.Class) {
							curType = resolvedType;
							break;
						}
					}
				}
			}
			var rowIter = store.AppendValues (ImageService.GetPixbuf("md-class", Gtk.IconSize.Menu),
			                   				 curType.Name,
			                   				 curType);
			while (baseTypes.Count > 0) {
				curType = baseTypes.Pop ();
				rowIter = store.AppendValues (rowIter, ImageService.GetPixbuf("md-class", Gtk.IconSize.Menu),
			                   				 curType.Name,
			                   				 curType);	
			}	
			foreach (var type in domType.SourceProjectDom.GetSubclasses (domType)) {
				if ((type as DomType).BaseType.FullName.Equals(domType.FullName))
					AddSubClassData (store, type as DomType, rowIter);
			}
		}
		
		void AddSubClassData (TreeStore store, DomType domType, Gtk.TreeIter parentIter)
		{
			var rowIter = store.AppendValues (parentIter, ImageService.GetPixbuf("md-class", Gtk.IconSize.Menu),
				                                  domType.Name,
				                                  domType);
			foreach (var type in domType.SourceProjectDom.GetSubclasses (domType)) {
				if ((type as DomType).BaseType.FullName.Equals(domType.FullName))
					AddSubClassData (store, type as DomType, rowIter);
			}
		}
		
		bool FilterMembers (TreeModel model, TreeIter iter)
		{
			bool canShow = false;
			try {
				IMember member = model.GetValue (iter, 2) as IMember;
				if (member == null)
					return true;
				if (!overridenToggle.Active && !inheritedToggle.Active)
					return true;
				if (member.IsOverride && overridenToggle.Active) canShow = true;
				else if (member.IsProtectedOrInternal && inheritedToggle.Active) canShow = true;
			} catch {
				return false;
			}
			return canShow;
		}
		
		protected override bool OnFocusOutEvent (Gdk.EventFocus evnt)
		{
			Destroy();
			return base.OnFocusOutEvent (evnt);
		}
		
		public static void ShowClassNavigationWnd (IType type, CodeCompletionContext completionContext)
		{
			if (type != null && (type is DomType)) {
				ClassNavigationWindow wnd = new ClassNavigationWindow(type as DomType, completionContext);
				wnd.ShowAll();
			}
		}
	}
}
