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
using MonoDevelop.Database.Sql;
using MonoDevelop.Database.Designer;
using MonoDevelop.Database.Components;

namespace MonoDevelop.Database.Sql.MySql
{
	[System.ComponentModel.Category("MonoDevelop.Database.Sql.MySql")]
	[System.ComponentModel.ToolboxItem(true)]
	public partial class MySqlCreateDatabaseWidget : Bin
	{
		ListStore storeCollation = new ListStore (typeof(string), typeof(MySqlCollationSchema));
		ListStore storeCharset = new ListStore (typeof(string), typeof(MySqlCharacterSetSchema));
		
		public MySqlCreateDatabaseWidget ()
		{
			this.Build();
			comboCharset.Model = storeCharset;
			comboCollation.Model = storeCollation;

			CellRendererText text = new Gtk.CellRendererText ();
			comboCharset.PackStart (text, true);
			comboCharset.AddAttribute (text, "text", 0);
			
			CellRendererText text2 = new Gtk.CellRendererText ();
			comboCollation.PackStart (text2, true);
			comboCollation.AddAttribute (text2, "text", 0);
			
		}
		
		public void ClearCombos ()
		{
			storeCollation.Clear ();
			storeCharset.Clear ();
		}
		
		public void AddCollation (string description, MySqlCollationSchema collationValue)
		{
			storeCollation.AppendValues (description, collationValue);
		}

		public void AddCharset (string description, MySqlCharacterSetSchema charsetValue)
		{
			storeCharset.AppendValues (description, charsetValue);
			
		}
		
		public void SetDatabaseOptions (MySqlDatabaseSchema schema)
		{
			
			TreeIter iterCharset;
			TreeIter iterCollation; 
			if (comboCharset.GetActiveIter (out iterCharset) && comboCollation.GetActiveIter (out iterCollation))
			{
				schema.CharacterSetName = ((MySqlCharacterSetSchema)storeCharset.GetValue (iterCharset, 1)).Name;
				schema.CollationName = ((MySqlCollationSchema)storeCollation.GetValue (iterCollation, 1)).Name;
				schema.Comment = "";
				// ls.Statement = string.Format ("CHARACTER SET {0} COLLATE {1}", 
				
			}
		}
	}
}
