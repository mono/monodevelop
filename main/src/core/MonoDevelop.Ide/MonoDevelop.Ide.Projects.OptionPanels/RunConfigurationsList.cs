//
// RunConfigurationsList.cs
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
using MonoDevelop.Core;
using MonoDevelop.Projects;
using Xwt;
using System.Collections;
using System.Collections.Generic;

namespace MonoDevelop.Ide.Projects.OptionPanels
{
	class RunConfigurationsList: Widget
	{
		Xwt.ListView list;
		Xwt.ListStore listStore;
		Xwt.DataField<RunConfiguration> configCol = new Xwt.DataField<RunConfiguration> ();
		Xwt.DataField<string> configNameCol = new Xwt.DataField<string> ();
		Xwt.DataField<string> accessibleCol = new Xwt.DataField<string> ();
		Xwt.DataField<Xwt.Drawing.Image> configIconCol = new Xwt.DataField<Xwt.Drawing.Image> ();

		public RunConfigurationsList ()
		{
			listStore = new Xwt.ListStore (configCol, configNameCol, configIconCol, accessibleCol);
			list = new Xwt.ListView (listStore);
			list.HeadersVisible = false;

			var imgCell = new ImageCellView { ImageField = configIconCol };
			var textCell = new TextCellView { MarkupField = configNameCol };
			list.Columns.Add (GettextCatalog.GetString ("Name"), imgCell, textCell);
			textCell.AccessibleFields.Label = accessibleCol;

			Content = list;

			list.SelectionChanged += (s, o) => SelectionChanged?.Invoke (this, o);
			list.RowActivated += (s, o) => RowActivated?.Invoke (this, o);
		}

		public void Fill (IEnumerable<RunConfiguration> configurations)
		{
			var currentRow = list.SelectedRow;
			listStore.Clear ();
			foreach (var c in configurations) {
				var r = listStore.AddRow ();
				var txt = "<b>" + c.Name + "</b>\n" + c.Summary;
				var icon = !string.IsNullOrEmpty (c.IconId) ? ImageService.GetIcon (c.IconId) : ImageService.GetIcon ("md-prefs-play", Gtk.IconSize.Dnd);
				listStore.SetValues (r, configCol, c, configNameCol, txt, configIconCol, icon, accessibleCol, c.Name + " " + c.Summary);
			}
			if (currentRow != -1) {
				if (currentRow < listStore.RowCount)
					list.SelectRow (currentRow);
				else
					list.SelectRow (listStore.RowCount - 1);
			} else if (listStore.RowCount > 0)
				list.SelectRow (0);
		}

		public RunConfiguration SelectedConfiguration {
			get {
				if (list.SelectedRow == -1)
					return null;
				return listStore.GetValue (list.SelectedRow, configCol);
			}
			set {
				for (int n = 0; n < listStore.RowCount; n++) {
					if (value == listStore.GetValue (n, configCol)) {
						list.SelectRow (n);
						return;
					}
				}
			}
		}

		public event EventHandler SelectionChanged;
		public event EventHandler RowActivated;
	}
}

