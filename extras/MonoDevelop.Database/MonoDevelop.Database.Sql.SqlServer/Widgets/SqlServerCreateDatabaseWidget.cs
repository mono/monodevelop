//
// Authors:
//   Ben Motmans  <ben.motmans@gmail.com>
//
// Copyright (c) 2008 Ben Motmans
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

using Gtk;
using System;

namespace MonoDevelop.Database.Sql.SqlServer
{
	[System.ComponentModel.Category("MonoDevelop.Database.Sql.SqlServer")]
	[System.ComponentModel.ToolboxItem(true)]
	public partial class SqlServerCreateDatabaseWidget : Gtk.Bin
	{		
		ListStore storeCollations = new ListStore (typeof(string), typeof(SqlServerCollationSchema));
		public SqlServerCreateDatabaseWidget()
		{
			this.Build();
			comboCollations.Model = storeCollations;
		}
		
		public void Initialize (SqlServerSchemaProvider provider)
		{
			SqlServerCollationSchemaCollection collations = provider.GetCollations ();
			Console.WriteLine (collations.Count);
			foreach (SqlServerCollationSchema coll in collations)
				storeCollations.AppendValues (string.Format ("{0} - {1}", coll.Name, coll.Description), coll);
		}
		
		protected virtual void OnComboMaxSizeChanged (object sender, System.EventArgs e)
		{
			if (((Gtk.ComboBox)sender).ActiveText == AddinCatalog.GetString ("UNLIMITED")) {
				spinMaxSize.Sensitive = false;
				spinMaxSize.Value = 0;
			} else
				spinMaxSize.Sensitive = true;
		}
		
		public void SetDatabaseOptions (SqlServerDatabaseSchema schema)
		{
			TreeIter iter;
			if (comboCollations.GetActiveIter (out iter))
				schema.Collation = (SqlServerCollationSchema)storeCollations.GetValue (iter, 1);
			SizeType size;
			
			size = (SizeType)Enum.Parse (typeof(SizeType), comboSize.ActiveText);
			schema.Size = new FileSize (Convert.ToInt32(spinSize.Value), size);
			size = (SizeType)Enum.Parse (typeof(SizeType), comboMaxSize.ActiveText);
			schema.MaxSize = new FileSize (Convert.ToInt32(spinMaxSize.Value), size);
			if (comboFilegrowth.ActiveText == "%")
				size = SizeType.PERCENTAGE; 
			else
				size = (SizeType)Enum.Parse (typeof(SizeType), comboFilegrowth.ActiveText);
			schema.FileGrowth = new FileSize (Convert.ToInt32(spinFilegrowth.Value), size);
			schema.LogicalName = entryName.Text;
			schema.FileName = entryFilename.Text;
		}
		
		protected virtual void OnComboFilegrowthChanged (object sender, System.EventArgs e)
		{
			if (((Gtk.ComboBox)sender).ActiveText == "%") {
				spinFilegrowth.Adjustment = new Adjustment (0, 0, 100, 1, 1, 1);
				spinFilegrowth.GrabFocus ();
			}
			else
				spinFilegrowth.Adjustment = new Adjustment (0, 0, 2147483647, 1, 1, 1);
		}
		
		
	}
}
