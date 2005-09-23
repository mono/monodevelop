//
// DataGrid - attempt at creating a DataGrid for GTK#
//            using a GTK# TreeView.  The goal is to have similar
//            functionality to a System.Windows.Forms.DataGrid
//            or System.Web.UI.WebControls.DataGrid.  This includes
//            data binding support.
//    
// Based on the sample/TreeViewDemo.cs
//
// Author: Kristian Rietveld <kris@gtk.org>
//         Daniel Morgan <danmorg@sc.rr.com>
//         Christian Hergert <chris@mosaix.net>
//
// (c) 2002 Kristian Rietveld
// (c) 2002 Daniel Morgan
// (c) 2004 Christian Hergert

using System;
using System.Collections;
using System.ComponentModel;
using System.Reflection;
using System.Text;
using GLib;
using Gtk;
using System.Runtime.InteropServices;

namespace MonoDevelop.Gui.Widgets
{
	/// <summary>
	/// Column for inserting into a DataGrid
	/// </summary>
	public class DataGridColumn 
	{
		private string columnName = "";
		private Gtk.TreeViewColumn treeViewColumn = null;
		
		/// <summary>
		/// Title for the Column
		/// </summary>
		public string ColumnName {
			get {
				return columnName;
			}
			set {
				columnName = value;
			}
		}
		
		/// <summary>
		/// Gtk Column widget
		/// </summary>
		public Gtk.TreeViewColumn TreeViewColumn {
			get {
				return treeViewColumn;
			}
			set {
				treeViewColumn = value;
			}
		}
	}

	public class DataGrid : Gtk.VBox
	{
		private Gtk.ListStore store = null;
		private Gtk.TreeView treeView = null;

		public DataGridColumn[] gridColumns = null;

		public DataGrid () : base(false, 4) 
		{		
			ScrolledWindow sw = new ScrolledWindow ();
			this.PackStart (sw, true, true, 0);

			treeView = new Gtk.TreeView (store);

			sw.Add (treeView);
		}

		// FIXME: need to place in a base class
		//        the DataSource, DataMember, DataBind()
		//        public members.
		//        maybe we can call the base class
		//        BaseDataList for GTK#?

		private object dataSource = null;

		private string dataMember = "";

		public object DataSource {
			get {
				return dataSource;
			}
			set {
				dataSource = value;
			}
		}

		public string DataMember {
			get {
				return dataMember;
			}
			set {
				dataMember = value;
			}
		}

		public void DataBind () 
		{
			Clear ();

			System.Object o = null;
			o = GetResolvedDataSource (DataSource, DataMember);
			IEnumerable ie = (IEnumerable) o;
			ITypedList tlist = (ITypedList) o;

			// FIXME: does not belong in this base method
			Gtk.TreeIter iter = new Gtk.TreeIter ();
									
			PropertyDescriptorCollection pdc = tlist.GetItemProperties (new PropertyDescriptor[0]);

			// FIXME: does not belong in this base method
			gridColumns = new DataGridColumn[pdc.Count];

			// FIXME: does not belong in base method
			// define the columns in the treeview store
			// based on the schema of the result
			/*
			uint[] theTypes = new uint[pdc.Count];
			for (int col = 0; col < pdc.Count; col++) {
				theTypes[col] = (int) TypeFundamentals.TypeString;
			}
			*/
			GType [] theTypes = new GType[pdc.Count];
			for ( int col=0; col<pdc.Count; col++ ) {
				theTypes[col] = (GType)GType.String;
			}
			store.ColumnTypes = theTypes;

			// FIXME: does not belong in this base method
			int colndx = -1;
			foreach (PropertyDescriptor pd in pdc) {
				colndx ++;
				gridColumns[colndx] = new DataGridColumn ();
				gridColumns[colndx].ColumnName = pd.Name;				
			}

			foreach (System.Object obj in ie) {
				ICustomTypeDescriptor custom = (ICustomTypeDescriptor) obj;
				PropertyDescriptorCollection properties;
				properties = custom.GetProperties ();
				
				iter = NewRow ();
				int cv = 0;
				foreach (PropertyDescriptor property in properties) {
					object oPropValue = property.GetValue (obj);
					string sPropValue = oPropValue.ToString ();
					
					// FIXME: does not belong in this base method
					SetColumnValue (iter, cv, sPropValue);

					cv++;
				}
			}

			// FIXME: does not belong in this base method
			treeView.Model = store;
			AutoCreateTreeViewColumns ( treeView );
		}

		// borrowed from Mono's System.Web implementation
		protected IEnumerable GetResolvedDataSource(object source, string member) 
		{
			if (source != null && source is IListSource) {
				IListSource src = (IListSource) source;
				IList list = src.GetList ();
				if (!src.ContainsListCollection) {
					return list;
				}
				if (list != null && list is ITypedList) {

					ITypedList tlist = (ITypedList) list;
					PropertyDescriptorCollection pdc = tlist.GetItemProperties (new PropertyDescriptor[0]);
					if (pdc != null && pdc.Count > 0) {
						PropertyDescriptor pd = null;
						if (member != null && member.Length > 0) {
							pd = pdc.Find (member, true);
						} else {
							pd = pdc[0];
						}
						if (pd != null) {
							object rv = pd.GetValue (list[0]);
							if (rv != null && rv is IEnumerable) {
								return (IEnumerable)rv;
							}
						}
						throw new Exception ("ListSource_Missing_DataMember");
					}
					throw new Exception ("ListSource_Without_DataMembers");
				}
			}
			if (source is IEnumerable) {
				return (IEnumerable)source;
			}
			return null;
		}

		public void Clear () 
		{
			if (store != null) {
				store.Clear ();
				store = null;
				//store = new ListStore ((int)TypeFundamentals.TypeString);
				store = new ListStore( GType.String );
			}
			else {
				//store = new ListStore ((int)TypeFundamentals.TypeString);
				store = new ListStore( GType.String );
			}	

			if (gridColumns != null) {
				for (int c = 0; c < gridColumns.Length; c++) {
					if (gridColumns[c] != null) {
						if (gridColumns[c].TreeViewColumn != null) {
							treeView.RemoveColumn (gridColumns[c].TreeViewColumn);
							gridColumns[c].TreeViewColumn = null;
						}
						gridColumns[c] = null;
					}
				}
				gridColumns = null;
			}
		}

		// for DEBUG only
		public void AppendText (string text) 
		{
			Console.WriteLine ("DataGrid DEBUG: " + text);
			Console.Out.Flush ();
		}

		public TreeIter NewRow () 
		{ 
			/*
			TreeIter rowTreeIter = new TreeIter();
			store.Append (out rowTreeIter);
			return rowTreeIter;
			*/
			return store.Append();
		}

		public void AddRow (object[] columnValues) 
		{	
			TreeIter iter = NewRow ();			
			for(int col = 0; col < columnValues.Length; col++) {
				string cellValue = columnValues[col].ToString ();
				SetColumnValue (iter, col, cellValue);
			}
		}

		public void SetColumnValue (TreeIter iter, int column, string value) 
		{
			GLib.Value cell = new GLib.Value (value);
			store.SetValue (iter, column, cell);	
		}

		private void AutoCreateTreeViewColumns (Gtk.TreeView theTreeView) 
		{
			for(int col = 0; col < gridColumns.Length; col++) {
				// escape underscore _ because it is used
				// as the underline in menus and labels
				StringBuilder name = new StringBuilder ();
				foreach (char ch in gridColumns[col].ColumnName) {
					if (ch == '_')
						name.Append ("__");
					else
						name.Append (ch);
				}
				TreeViewColumn tvc;
				tvc = CreateColumn (theTreeView, col, 
						name.ToString ());
				theTreeView.AppendColumn (tvc);
			}
		}

		// TODO: maybe need to create 
		// a DataGridColumnCollection of DataGridColumn
		// and a DataGridColumn contain a TreeViewColumn
		public TreeViewColumn CreateColumn (Gtk.TreeView theTreeView, int col, 
						string columnName) 
		{
			TreeViewColumn NameCol = new TreeViewColumn ();		 
			CellRenderer NameRenderer = new CellRendererText ();
			
			NameCol.Title = columnName;
			NameCol.PackStart (NameRenderer, true);
			NameCol.AddAttribute (NameRenderer, "text", col);

			gridColumns[col].TreeViewColumn = NameCol;
			
			return NameCol;
		}
	}
}

