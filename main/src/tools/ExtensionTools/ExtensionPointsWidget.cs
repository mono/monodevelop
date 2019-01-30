//
// ExtensionPointsWidget.cs
//
// Author:
//       Marius Ungureanu <maungu@microsoft.com>
//
// Copyright (c) 2018 Microsoft Inc.
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
using Mono.Addins;
using Mono.Addins.Description;
using Xwt;
namespace MonoDevelop.ExtensionTools
{
	class ExtensionPointsWidget : Widget
	{
		readonly ListStore listStore;
		readonly ListView listView;
		readonly DataField<string> labelField = new DataField<string> ();
		readonly Label summary = new Label ();

		public ExtensionPointsWidget (Addin[] addins = null)
		{
			addins = addins ?? AddinManager.Registry.GetAllAddins ();

			listStore = new ListStore (labelField);
			listView = new ListView (listStore);
			listView.RowActivated += ListView_RowActivated;

			var col = listView.Columns.Add ("Name", labelField);
			col.Expands = true;

			FillData (addins);

			var vbox = new VBox ();
			vbox.PackStart (summary, false);
			vbox.PackStart (listView, true);
			Content = vbox;
		}

		void ListView_RowActivated (object sender, ListViewRowEventArgs e)
		{
			var value = listStore.GetValue (e.RowIndex, labelField);

			var window = new Window {
				Content = new ExtensionNodesWidget (value),
				Height = 800,
				Width = 600,
			};
			window.Show ();
		}

		void FillData (Addin[] addins)
		{
			var points = GatherExtensionPoints (addins);

			summary.Text = $"Count: {points.Length}";

			foreach (var point in points) {
				int row = listStore.AddRow ();
				listStore.SetValue (row, labelField, point);
			}
		}

		string[] GatherExtensionPoints (Addin[] addins)
		{
			var points = new HashSet<string> ();

			foreach (var addin in addins) {
				foreach (ExtensionPoint extensionPoint in addin.Description.ExtensionPoints) {
					points.Add (extensionPoint.Path);
				}
			}

			return points.ToSortedArray ();
		}
	}
}
