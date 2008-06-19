// OverridesImplementsDialog.cs
//
//Author:
//    Ankit Jain  <jankit@novell.com>
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
using System.Collections.Generic;
using System.Text;

using Gtk;

using MonoDevelop.Core;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Projects.Ambience;
using MonoDevelop.Projects.CodeGeneration;
using MonoDevelop.Projects.Parser;
using Ambience_ = MonoDevelop.Projects.Ambience.Ambience;

namespace MonoDevelop.Ide
{
	public partial class OverridesImplementsDialog : Gtk.Dialog
	{
		IClass cls;
		TreeStore store;
		CodeRefactorer refactorer;
		Ambience ambience;

		private const int colCheckedIndex = 0;
		private const int colIconIndex = 1;
		private const int colNameIndex = 2;
		private const int colExplicitIndex = 3;
		private const int colItemIndex = 4;

		private const ConversionFlags default_conversion_flags =
			ConversionFlags.ShowParameterNames |
			ConversionFlags.ShowGenericParameters |
			ConversionFlags.ShowReturnType |
			ConversionFlags.ShowParameters |
			ConversionFlags.UseIntrinsicTypeNames;

		public OverridesImplementsDialog (IClass cls)
		{
			this.Build();
			this.cls = cls;

			// FIXME: title
			Title = GettextCatalog.GetString ("Override and/or implement members");

			store = new TreeStore (typeof (bool), typeof (Gdk.Pixbuf), typeof (string), typeof (bool), typeof (ILanguageItem));

			// Column #1
			TreeViewColumn nameCol = new TreeViewColumn ();
			nameCol.Title = GettextCatalog.GetString ("Name");
			nameCol.Expand = true;

			CellRendererToggle cbRenderer = new CellRendererToggle ();
			cbRenderer.Activatable = true;
			cbRenderer.Toggled += OnSelectToggled;
			nameCol.PackStart (cbRenderer, false);
			nameCol.AddAttribute (cbRenderer, "active", colCheckedIndex);

			CellRendererPixbuf iconRenderer = new CellRendererPixbuf ();
			nameCol.PackStart (iconRenderer, false);
			nameCol.AddAttribute (iconRenderer, "pixbuf", colIconIndex);

			CellRendererText nameRenderer = new CellRendererText ();
			nameCol.PackStart (nameRenderer, true);
			nameCol.AddAttribute (nameRenderer, "text", colNameIndex);

			treeview.AppendColumn (nameCol);

			// Column #2
			CellRendererToggle explicitRenderer = new CellRendererToggle ();
			explicitRenderer.Activatable = true;
			explicitRenderer.Xalign = 0.0f;
			explicitRenderer.Toggled += OnExplicitToggled;
			TreeViewColumn explicitCol = new TreeViewColumn ();
			explicitCol.Title = GettextCatalog.GetString ("Explicit");
			explicitCol.PackStart (explicitRenderer, true);
			explicitCol.SetCellDataFunc (explicitRenderer, new TreeCellDataFunc (RenderExplicitCheckbox));
			explicitCol.AddAttribute (explicitRenderer, "active", colExplicitIndex);
			treeview.AppendColumn (explicitCol);

			store.SetSortColumnId (colNameIndex, SortType.Ascending);
			treeview.Model = store;

			buttonCancel.Clicked += OnCancelClicked;
			buttonOk.Clicked += OnOKClicked;
			buttonSelectAll.Clicked += delegate { SelectAll (true); };
			buttonUnselectAll.Clicked += delegate { SelectAll (false); };

			refactorer = IdeApp.Workspace.GetCodeRefactorer (IdeApp.ProjectOperations.CurrentSelectedSolution);
			ambience = MonoDevelop.Projects.Services.Ambience.GetAmbienceForFile (cls.Region.FileName);
			PopulateTreeView ();
			UpdateOKButton ();
		}

		void PopulateTreeView ()
		{
			List<IMember> class_members = new List<IMember> ();
			List<IMember> interface_members = new List<IMember> ();

			refactorer.FindOverridables (cls, class_members, interface_members, false, true);

			Dictionary<string, TreeIter> iter_cache = new Dictionary<string,TreeIter> ();
			PopulateTreeView (class_members, iter_cache,
					IdeApp.Services.Resources.GetIcon (MonoDevelop.Core.Gui.Stock.Class, IconSize.Menu));
			PopulateTreeView (interface_members, iter_cache,
					IdeApp.Services.Resources.GetIcon (MonoDevelop.Core.Gui.Stock.Interface, IconSize.Menu));
		}

		void PopulateTreeView (List<IMember> members, Dictionary<string, TreeIter> iter_cache, Gdk.Pixbuf parent_icon)
		{
			foreach (IMember member in members) {
				TreeIter iter;
				if (!iter_cache.TryGetValue (member.DeclaringType.FullyQualifiedName, out iter)) {
					iter = store.AppendValues (false, parent_icon,
			                                   GetDescriptionString (member.DeclaringType,
								   default_conversion_flags | ConversionFlags.UseFullyQualifiedNames),
							   false, member.DeclaringType);
					iter_cache [member.DeclaringType.FullyQualifiedName] = iter;
				}

				store.AppendValues (iter, false,
				                    IdeApp.Services.Resources.GetIcon (Services.Icons.GetIcon (member), IconSize.Menu),
				                    GetDescriptionString (member, default_conversion_flags), false, member);
			}
		}

#region Event handlers
		void RenderExplicitCheckbox (Gtk.TreeViewColumn column, Gtk.CellRenderer cell, Gtk.TreeModel model, Gtk.TreeIter iter)
		{
			CellRendererToggle cellToggle = (CellRendererToggle) cell;
			ILanguageItem item = (ILanguageItem) store.GetValue (iter, colItemIndex);
			bool parent_is_class = false;

			// Don't show 'explicit' checkbox for Class/interface rows or class methods
			TreeIter parentIter;
			if (item is IMember && store.IterParent (out parentIter, iter)) {
				IClass parentClass = store.GetValue (parentIter, colItemIndex) as IClass;
				if (parentClass != null && parentClass.ClassType != ClassType.Interface)
					parent_is_class = true;
			}

			if (parent_is_class || item is IClass) {
				cellToggle.Visible = false;
			} else {
				cellToggle.Visible = true;
				cellToggle.Active = GetExplicit (iter);
			}
		}

		void OnSelectToggled (object o, ToggledArgs args)
		{
			TreeIter iter;
			if (!store.GetIterFromString (out iter, args.Path))
				return;

			bool old_value = GetChecked (iter);
			store.SetValue (iter, colCheckedIndex, !old_value);

			TreeIter parent;
			if (store.IterParent (out parent, iter)) {
				// Member(method/property/etc) row clicked
				// Set the parent's 'checked' state according to the children's state
				bool all_children_checked = true;
				foreach (TreeIter child in GetAllNodes (parent, false)) {
					if (!GetChecked (child)) {
						all_children_checked = false;
						break;
					}
				}

				store.SetValue (parent, colCheckedIndex, all_children_checked);
			} else {
				// Mark children's state to match parent's checked state
				foreach (TreeIter child in GetAllNodes (iter, false))
					store.SetValue (child, colCheckedIndex, !old_value);
			}
			UpdateOKButton ();
		}

		void OnExplicitToggled (object o, ToggledArgs args)
		{
			TreeIter iter;
			if (!store.GetIterFromString (out iter, args.Path))
				return;

			store.SetValue (iter, colExplicitIndex, !GetExplicit (iter));
		}

		void OnCancelClicked (object sender, EventArgs e)
		{
			((Widget) this).Destroy ();
		}

		void UpdateOKButton ()
		{
			bool atleast_one_checked = false;
			foreach (TreeIter iter in GetAllNodes (TreeIter.Zero, true)) {
				if (GetChecked (iter)) {
					atleast_one_checked = true;
					break;
				}
			}
			buttonOk.Sensitive = atleast_one_checked;
		}

		void OnOKClicked (object sender, EventArgs e)
		{
			try {
				TreeIter parent;
				if (!store.GetIterFirst (out parent))
					return;

				do {
					IClass klass = GetIClass (parent);
					bool is_interface = klass.ClassType == ClassType.Interface;
					IReturnType privateImplementationType = DefaultReturnType.FromString (klass.FullyQualifiedName);

					foreach (TreeIter child in GetAllNodes (parent, false)) {
						if (GetChecked (child))
							refactorer.ImplementMember (cls, GetIMember (child),
						                            (is_interface && GetExplicit (child)) ? privateImplementationType : null);
					}
				} while (store.IterNext (ref parent));
			} finally {
				((Widget) this).Destroy ();
			}
		}
#endregion

#region Helper methods
		void SelectAll (bool select)
		{
			foreach (TreeIter iter in GetAllNodes (TreeIter.Zero, true))
				store.SetValue (iter, colCheckedIndex, select);
			UpdateOKButton ();
		}

		bool GetChecked (TreeIter iter)
		{
			return (bool) store.GetValue (iter, colCheckedIndex);
		}

		IClass GetIClass (TreeIter iter)
		{
			return (IClass) store.GetValue (iter, colItemIndex);
		}

		IMember GetIMember (TreeIter iter)
		{
			return (IMember) store.GetValue (iter, colItemIndex);
		}

		bool GetExplicit (TreeIter iter)
		{
			return (bool) store.GetValue (iter, colExplicitIndex);
		}

		IEnumerable<TreeIter> GetAllNodes (TreeIter parent, bool iter_children)
		{
			TreeIter child;
			if (parent.Equals (TreeIter.Zero)) {
				if (!store.IterChildren (out child))
					yield break;
			} else if (!store.IterChildren (out child, parent)) {
				yield break;
			}

			do {
				yield return child;
				if (iter_children && store.IterHasChild (child)) {
					TreeIter iter;
					if (store.IterChildren (out iter, child)) {
						do {
							yield return iter;
						} while (store.IterNext (ref iter));
					}
				}
			} while (store.IterNext (ref child));
		}

		string GetDescriptionString (ILanguageItem item, ConversionFlags flags)
		{
			string sig = LanguageItemToString (item, flags & ~ConversionFlags.ShowReturnType);
			IMember member = item as IMember;
			if (member == null || (flags & ConversionFlags.ShowReturnType) == 0)
				return sig;

			return sig + " : " + ambience.Convert (member.ReturnType, default_conversion_flags);
		}

		string LanguageItemToString (ILanguageItem item, ConversionFlags flags)
		{
			if (item is IClass)
				return ambience.Convert ((IClass) item, flags);
			if (item is IMethod)
				return ambience.Convert ((IMethod) item, flags);
			if (item is IProperty)
				return ambience.Convert ((IProperty) item, flags);
			if (item is IEvent)
				return ambience.Convert ((IEvent) item, flags);
			if (item is IIndexer)
				return ambience.Convert ((IIndexer) item, flags);

			throw new InvalidOperationException (String.Format ("Unsupported language item type: {0}", item.GetType ().ToString ()));
		}

#endregion

	}

}
