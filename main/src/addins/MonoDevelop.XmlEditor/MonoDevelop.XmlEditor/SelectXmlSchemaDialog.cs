//
// MonoDevelop XML Editor
//
// Copyright (C) 2006 Matthew Ward
//

using Gtk;
using System;

namespace MonoDevelop.XmlEditor
{
	public class SelectXmlSchemaDialog : IDisposable
	{
		[Glade.Widget ("SelectXmlSchemaDialog")] Dialog dialog;
		[Glade.Widget] TreeView schemaTreeView;

		bool disposed;
		string NoSchemaSelectedText = "None";
				
		ListStore schemaList = new ListStore(typeof(string), typeof(string));

		public SelectXmlSchemaDialog(string[] namespaces)
		{
			new Glade.XML(null, "XmlEditor.glade", "SelectXmlSchemaDialog", null).Autoconnect(this);

			schemaTreeView.Model = schemaList;
			schemaList.SetSortColumnId(1, SortType.Ascending);
			schemaTreeView.Selection.Mode = SelectionMode.Single;
			CellRendererText renderer = new CellRendererText();
			renderer.Editable = false;
			schemaTreeView.AppendColumn("Namespace", renderer, "text", 0, "uri", 1);
			
			PopulateList(namespaces);
		}
		
		/// <summary>
		/// Gets or sets the selected schema namesace.
		/// </summary>
		public string SelectedNamespaceUri {
			get {
				TreeIter iter;
				if (schemaTreeView.Selection.GetSelected(out iter)) {
					return GetNamespaceUri(iter);
				}
				return String.Empty;
			}
			
			set {
				TreeIter firstIter;
				bool findingNamespace = schemaList.GetIterFirst(out firstIter);
				TreeIter iter = firstIter;
				while (findingNamespace) {
					if (GetNamespaceUri(iter) == value) {
						schemaTreeView.Selection.SelectIter(iter);
						ScrollToItem(iter);
						return;
					}
					findingNamespace = schemaList.IterNext(ref iter);
				}
						
				// Select the option representing "no schema" if 
				// the value does not exist in the list box.
				schemaTreeView.Selection.SelectIter(firstIter);
			}
		}
		
		public void Dispose()
		{
			if (!disposed) {
				disposed = true;
				dialog.Destroy ();
				dialog.Dispose();
			}
		}
		
		public int Run()
		{
			dialog.Show();
			return dialog.Run();
		}
		
		void PopulateList(string[] namespaces)
		{
			schemaList.AppendValues(NoSchemaSelectedText, String.Empty);
			foreach (string schemaNamespace in namespaces) {
				schemaList.AppendValues(schemaNamespace, schemaNamespace);
			}
		}
		
		string GetNamespaceUri(TreeIter iter)
		{
			return (string)schemaList.GetValue(iter, 1);
		}
		
		/// <summary>
		/// Scrolls the list so the specified item is visible.
		/// </summary>
		void ScrollToItem(TreeIter iter)
		{
			TreePath path = schemaList.GetPath(iter);
			if (path != null) {
				schemaTreeView.ScrollToCell(path, null, false, 0, 0);
			}
		}
	}
}
