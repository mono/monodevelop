//
// EnvironmentVariableCollectionEditor.cs
//
// Author:
//       Lluis Sanchez Gual <lluis@xamarin.com>
//
// Copyright (c) 2016 Xamarin, Inc (http://www.xamarin.com)
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
using MonoDevelop.Core;
using Xwt;
using System.Linq;
namespace MonoDevelop.Components
{
	public class EnvironmentVariableCollectionEditor: VBox
	{
		ListView list;
		ListStore store;
		DataField<string> keyField = new DataField<string> ();
		DataField<string> valueField = new DataField<string> ();
		TextCellView valueCell;
		Button deleteButton;

		public EnvironmentVariableCollectionEditor ()
		{
			store = new ListStore (keyField, valueField);
			list = new ListView (store);
			PackStart (list, true);

			TextCellView crt = new TextCellView ();
			crt.Editable = true;
			crt.TextField = keyField;
			var col = list.Columns.Add (GettextCatalog.GetString ("Variable"), crt);
			col.CanResize = true;
			crt.TextChanged += (s,a) => NotifyChanged ();

			valueCell = new TextCellView ();
			valueCell.Editable = true;
			valueCell.TextField = valueField;
			col = list.Columns.Add (GettextCatalog.GetString ("Value"), valueCell);
			col.CanResize = true;
			valueCell.TextChanged += (s, a) => NotifyChanged ();

			var box = new HBox ();

			var btn = new Button (GettextCatalog.GetString ("Add"));
			btn.Clicked += delegate {
				var row = store.AddRow ();
				list.SelectRow (row);
				list.StartEditingCell (row, crt);
				crt.TextChanged += CrtTextChanged;
				UpdateButtons ();
			};
			box.PackStart (btn);

			deleteButton = new Button (GettextCatalog.GetString ("Remove"));
			deleteButton.Clicked += delegate {
				var row = list.SelectedRow;
				if (row != -1) {
					store.RemoveRow (row);
					if (row < store.RowCount)
						list.SelectRow (row);
					else if (store.RowCount > 0)
						list.SelectRow (store.RowCount - 1);
					UpdateButtons ();
					NotifyChanged ();
				}
			};
			box.PackStart (deleteButton);

			PackStart (box);
			UpdateButtons ();
		}

		void UpdateButtons ()
		{
			deleteButton.Sensitive = store.RowCount > 0;
		}

		public void LoadValues (IDictionary<string, string> values)
		{
			store.Clear ();
			foreach (KeyValuePair<string, string> val in values)
				store.SetValues (store.AddRow (), keyField, val.Key, valueField, val.Value);
			UpdateButtons ();
		}

		public void StoreValues (IDictionary<string, string> values)
		{
			var keys = new HashSet<string> ();
			for (int n = 0; n < store.RowCount; n++) {
				string var = store.GetValue (n, keyField);
				string val = store.GetValue (n, valueField);
				if (!string.IsNullOrEmpty (var)) {
					values [var] = val;
					keys.Add (var);
				}
			}
			foreach (var k in values.Keys.ToArray ())
				if (!keys.Contains (k))
					values.Remove (k);
		}

		void CrtTextChanged (object sender, WidgetEventArgs e)
		{
			var crt = (TextCellView)sender;
			crt.TextChanged -= CrtTextChanged;
			var r = list.CurrentEventRow;
			NotifyChanged ();
			Xwt.Application.TimeoutInvoke (100, delegate {
				list.StartEditingCell (r, valueCell); return false;
			});
		}

		void NotifyChanged ()
		{
			Changed?.Invoke (this, EventArgs.Empty);
		}

		public event EventHandler Changed;
	}
}

