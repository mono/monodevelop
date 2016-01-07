//
// KnownRoslynProviderAssembliesPanelWidget.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2016 
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
using System.Text;
using Gtk;
using MonoDevelop.Ide.Gui.Dialogs;

namespace MonoDevelop.PackageManagement.Refactoring
{
	
	class KnownRoslynProviderAssembliesPanel : OptionsPanel
	{
		KnownRoslynProviderAssembliesPanelWidget widget;

		public override Widget CreatePanelWidget ()
		{
			return widget = new KnownRoslynProviderAssembliesPanelWidget ();
		}

		public override void ApplyChanges ()
		{
			widget.ApplyChanges ();
		}
	}

	[System.ComponentModel.ToolboxItem (true)]
	partial class KnownRoslynProviderAssembliesPanelWidget : Gtk.Bin
	{
		HashSet<string> loadAnalyzers = new HashSet<string> ();
		readonly TreeStore treeStore = new TreeStore (typeof(bool), typeof(string), typeof (string));

		public KnownRoslynProviderAssembliesPanelWidget ()
		{
			Build ();

			var toggleRenderer = new CellRendererToggle ();
			toggleRenderer.Toggled += delegate(object o, ToggledArgs args) {
				TreeIter iter;
				if (treeStore.GetIterFromString (out iter, args.Path)) {
					var md5 = (string)treeStore.GetValue (iter, 2);
					if (loadAnalyzers.Contains (md5)) {
						loadAnalyzers.Remove (md5);
					} else {
						loadAnalyzers.Add (md5);
					}
				}
			};

			var titleCol = new TreeViewColumn ();
			treeview1.AppendColumn (titleCol);
			titleCol.PackStart (toggleRenderer, false);
			titleCol.Sizing = TreeViewColumnSizing.Autosize;
			titleCol.SetCellDataFunc (toggleRenderer, delegate (TreeViewColumn treeColumn, CellRenderer cell, TreeModel model, TreeIter iter) {
				var md5 = (string)treeStore.GetValue (iter, 2);
				toggleRenderer.Active = loadAnalyzers.Contains (md5);
			});
			var cellRendererText = new CellRendererText {
				Ellipsize = Pango.EllipsizeMode.End
			};
			titleCol.PackStart (cellRendererText, true);
			titleCol.AddAttribute (cellRendererText, "markup", 1);

			var cellRendererText2 = new CellRendererText {
				Ellipsize = Pango.EllipsizeMode.End
			};
			titleCol.PackStart (cellRendererText2, true);
			titleCol.AddAttribute (cellRendererText2, "markup", 2);

			foreach (var analyzerMd5 in PackageCodeDiagnosticProvider.loadAnalyzers.Value.Split (',')) {
				if (string.IsNullOrEmpty (analyzerMd5))
					continue;
				loadAnalyzers.Add (analyzerMd5);
			}
			var knownAnalyzers = PackageCodeDiagnosticProvider.knownAnalyzers.Value.Split (',');

			for (int i = 0; i < knownAnalyzers.Length; i += 2) {
				var md5 = knownAnalyzers [i];
				if (string.IsNullOrEmpty (md5))
					break;
				var name = knownAnalyzers [i + 1];

				treeStore.AppendValues (loadAnalyzers.Contains (md5), name, md5); 
			}
			treeview1.HeadersVisible = false;
			treeview1.Model = treeStore;
		}

		internal void ApplyChanges ()
		{
			var loadAnalyzerBuilder = new StringBuilder ();
			foreach (var id in loadAnalyzers) {
				loadAnalyzerBuilder.Append (id);
				loadAnalyzerBuilder.Append (',');
			}
			PackageCodeDiagnosticProvider.loadAnalyzers.Value = loadAnalyzerBuilder.ToString ();
		}
	}
}

