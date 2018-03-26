// AssemblyReferencePanel.cs
//  
// Author:
//       Todd Berman <tberman@sevenl.net>
//       Lluis Sanchez Gual <lluis@novell.com>
// 
// Copyright (c) 2004 Todd Berman
// Copyright (c) 2009 Novell, Inc (http://www.novell.com)
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
using System.IO;
using System.Text;
using System.Linq;
using MonoDevelop.Projects;
using MonoDevelop.Core;
using MonoDevelop.Components;

using Gtk;
using MonoDevelop.Core.Assemblies;
using MonoDevelop.Core.Text;
using MonoDevelop.Ide.Gui.Dialogs;
using MonoDevelop.Components.Extensions;
using System.Collections.Generic;
using System.Reflection;

namespace MonoDevelop.Ide.Projects
{
	internal class AssemblyReferencePanel : VBox, IReferencePanel
	{
		SelectReferenceDialog selectDialog;
		ListStore store = null;
		private TreeView treeView = null;
		StringMatcher stringMatcher;
		List<AssemblyInfo> assemblies = new List<AssemblyInfo> ();
		FilePath basePath;

		class AssemblyInfo
		{
			public AssemblyInfo (FilePath file)
			{
				this.File = file.CanonicalPath;
				try {
					Version = new AssemblyName (SystemAssemblyService.GetAssemblyName (file)).Version.ToString ();
				} catch {
					Version = "";
				}
			}
			
			public FilePath File;
			public string Version;
			public bool Selected;
		}

		const int ColName = 0;
		const int ColVersion = 1;
		const int ColAssemblyInfo = 2;
		const int ColSelected = 3;
		const int ColPath = 4;
		const int ColIcon = 5;
		const int ColMatchRank = 6;

		public AssemblyReferencePanel (SelectReferenceDialog selectDialog)
		{
			Spacing = 6;
			this.selectDialog = selectDialog;

			store = new ListStore (typeof (string), typeof (string), typeof (AssemblyInfo), typeof (bool), typeof (string), typeof(IconId), typeof(int));
			treeView = new TreeView (store);

			TreeViewColumn firstColumn = new TreeViewColumn ();
			CellRendererToggle tog_render = new CellRendererToggle ();
			tog_render.Toggled += AddReference;
			firstColumn.PackStart (tog_render, false);
			firstColumn.AddAttribute (tog_render, "active", ColSelected);

			treeView.AppendColumn (firstColumn);

			TreeViewColumn secondColumn = new TreeViewColumn ();
			secondColumn.Title = GettextCatalog.GetString ("Assembly");
			CellRendererImage crp = new CellRendererImage ();
			secondColumn.PackStart (crp, false);
			secondColumn.AddAttribute (crp, "icon-id", ColIcon);

			CellRendererText text_render = new CellRendererText ();
			secondColumn.PackStart (text_render, true);
			secondColumn.AddAttribute (text_render, "markup", ColName);

			treeView.AppendColumn (secondColumn);

			treeView.AppendColumn (GettextCatalog.GetString ("Version"), new CellRendererText (), "markup", ColVersion);
			treeView.AppendColumn (GettextCatalog.GetString ("Path"), new CellRendererText (), "text", ColPath);

			treeView.Columns[1].Resizable = true;

			store.SetSortColumnId (0, SortType.Ascending);
			store.SetSortFunc (0, new TreeIterCompareFunc (Sort));

			ScrolledWindow sc = new ScrolledWindow ();
			sc.ShadowType = Gtk.ShadowType.In;
			sc.Add (treeView);
			PackStart (sc, true, true, 0);

			var but = new Gtk.Button (GettextCatalog.GetString ("Browse..."));
			but.Clicked += HandleClicked;
			var hbox = new Gtk.HBox ();
			hbox.PackEnd (but, false, false, 0);
			PackStart (hbox, false, false, 0);

			ShowAll ();
			BorderWidth = 6;
		}

		private int Sort (TreeModel model, TreeIter left, TreeIter right)
		{
			int result = 0;

			if (stringMatcher != null) {
				result = ((int)model.GetValue (right, ColMatchRank)).CompareTo ((int)model.GetValue (left, ColMatchRank));
				if (result != 0)
					return result;
			}

			result = String.Compare ((string)model.GetValue (left, 0), (string)model.GetValue (right, 0), StringComparison.InvariantCultureIgnoreCase);

			if (result != 0)
				return result;

			return String.Compare ((string)model.GetValue (left, ColVersion), (string)model.GetValue (right, ColVersion), StringComparison.InvariantCultureIgnoreCase);
		}

		void HandleClicked (object sender, EventArgs e)
		{
			var dlg = new OpenFileDialog (GettextCatalog.GetString ("Select Assembly"), MonoDevelop.Components.FileChooserAction.Open);
//			dlg.AddFilter (GettextCatalog.GetString ("Assemblies"), "*.[Dd][Ll][Ll]", "*.[Ee][Xx][Ee]");
			dlg.AddFilter (GettextCatalog.GetString ("Assemblies"), "*.dll", "*.exe");
			dlg.CurrentFolder = basePath;
			dlg.SelectMultiple = true;
			dlg.TransientFor = selectDialog;
			if (dlg.Run ()) {
				basePath = dlg.CurrentFolder;
				foreach (string file in dlg.SelectedFiles) {
					var fn = new FilePath (file).CanonicalPath;
					var asm = assemblies.FirstOrDefault (a => a.File.Equals (fn));
					if (asm != null) {
						if (!asm.Selected) {
							asm.Selected = true;
							AddReference (file);
						}
						continue;
					}

					bool isAssembly = true;
					try	{
						SystemAssemblyService.GetAssemblyName (System.IO.Path.GetFullPath (file));
					} catch {
						isAssembly = false;
					}

					if (isAssembly) {
						assemblies.Add (new AssemblyInfo (file) { Selected = true });
						AddReference (file);
						if (IsExternalAssembly (file))
							selectDialog.RegisterFileReference (file);
						else if (!IsNuGetAssembly (file))
							selectDialog.RegisterFileReference (file, project.ParentSolution.FileName);
					} else {
						MessageService.ShowError (GettextCatalog.GetString ("File '{0}' is not a valid .Net Assembly", file));
					}
				}
				Reset ();
			}
		}

		FilePath nugetDir;
		DotNetProject project;
		
		public void SetProject (DotNetProject configureProject)
		{
			this.project = configureProject;
			assemblies.Clear ();

			if (configureProject == null) {
				Reset ();
				return;
			}

			nugetDir = configureProject.ParentSolution.ItemDirectory.Combine ("packages");

			foreach (var asm in selectDialog.GetRecentFileReferences (project.ParentSolution.FileName)) {
				if (File.Exists (asm))
					assemblies.Add (new AssemblyInfo (asm));
			}

			foreach (var pr in configureProject.ParentSolution.GetAllItems<DotNetProject> ().SelectMany (p => p.References).Where (r => r.ReferenceType == ReferenceType.Assembly && !string.IsNullOrEmpty (r.HintPath))) {
				var file = new FilePath (pr.HintPath).CanonicalPath;
				if (File.Exists (file) && !IsNuGetAssembly (file) && !assemblies.Any (a => a.File.Equals (file)))
					assemblies.Add (new AssemblyInfo (pr.HintPath));
			}

			basePath = configureProject.BaseDirectory;
			Reset ();
		}
		
		bool IsNuGetAssembly (FilePath p)
		{
			return p.IsChildPathOf (nugetDir);
		}

		bool IsExternalAssembly (FilePath p)
		{
			return !p.IsChildPathOf (project.ParentSolution.BaseDirectory) && !p.IsChildPathOf (project.ParentSolution.ItemDirectory);
		}

		public void SignalRefChange (ProjectReference refInfo, bool newState)
		{
			if (refInfo.ReferenceType == ReferenceType.Assembly && !string.IsNullOrEmpty (refInfo.HintPath)) {
				var path = new FilePath (refInfo.HintPath).CanonicalPath;
				var asm = assemblies.FirstOrDefault (a => a.File.Equals (path));
				if (asm != null)
					asm.Selected = newState;
				else if (newState)
					assemblies.Add (new AssemblyInfo (refInfo.HintPath) { Selected = true });
				Reset ();
			}
		}
		
		public void SetFilter (string filter)
		{
			if (!string.IsNullOrEmpty (filter))
				stringMatcher = StringMatcher.GetMatcher (filter, false);
			else
				stringMatcher = null;
			Reset ();
		}
		
		public void Reset ()
		{
			store.Clear ();

			foreach (var asm in assemblies) {

				int matchRank = 0;
				string name, version;

				if (stringMatcher != null) {
					string txt = asm.File.FileName + " " + asm.Version;
					if (!stringMatcher.CalcMatchRank (txt, out matchRank))
						continue;
					int[] match = stringMatcher.GetMatch (txt);
					name = GetMatchMarkup (treeView, asm.File.FileName, match, 0);
					version = GetMatchMarkup (treeView, asm.Version, match, asm.File.FileName.Length + 1);
				} else {
					name = GLib.Markup.EscapeText (asm.File.FileName);
					version = GLib.Markup.EscapeText (asm.Version);
				}

				store.AppendValues (name, 
					version, 
					asm, 
					asm.Selected, 
					asm.File.ToString (), 
					MonoDevelop.Ide.Gui.Stock.OpenFolder,
					matchRank);
			}
		}

		internal static string GetMatchMarkup (Gtk.Widget widget, string text, int[] matches, int startIndex)
		{
			StringBuilder result = StringBuilderCache.Allocate ();
			int lastPos = 0;
			var color = HslColor.GenerateHighlightColors (widget.Style.Base (StateType.Normal), 
				widget.Style.Text (StateType.Normal), 3)[2];
			for (int n=0; n < matches.Length; n++) {
				int pos = matches[n] - startIndex;
				if (pos < 0 || pos >= text.Length)
					continue;
				if (pos - lastPos > 0)
					result.Append (GLib.Markup.EscapeText (text.Substring (lastPos, pos - lastPos)));
				result.Append ("<span foreground=\"");
				result.Append (color.ToPangoString ());
				result.Append ("\">");
				result.Append (GLib.Markup.EscapeText (text[pos].ToString ()));
				result.Append ("</span>");
				lastPos = pos + 1;
			}
			if (lastPos < text.Length)
				result.Append (GLib.Markup.EscapeText (text.Substring (lastPos, text.Length - lastPos)));
			return StringBuilderCache.ReturnAndFree (result);
		}

		public void AddReference (object sender, Gtk.ToggledArgs e)
		{
			Gtk.TreeIter iter;
			store.GetIterFromString (out iter, e.Path);
			var ainfo = (AssemblyInfo)store.GetValue (iter, ColAssemblyInfo);
			string fullName = (string)store.GetValue (iter, ColPath);
			if (!(bool)store.GetValue (iter, ColSelected)) {
				store.SetValue (iter, ColSelected, true);
				ainfo.Selected = true;
				AddReference (fullName);
			}
			else {
				store.SetValue (iter, ColSelected, false);
				ainfo.Selected = false;
				RemoveReference (fullName);

				// Ensure that this file is kept in the recents list, so it can be added later on
				if (IsExternalAssembly (fullName))
					selectDialog.RegisterFileReference (fullName);
				else if (!IsNuGetAssembly (fullName))
					selectDialog.RegisterFileReference (fullName, project.ParentSolution.FileName);
			}
		}

		void AddReference (FilePath path)
		{
			selectDialog.AddReference (ProjectReference.CreateAssemblyFileReference (path));
		}

		void RemoveReference (FilePath path)
		{
			selectDialog.RemoveReference (ReferenceType.Assembly, path.FileNameWithoutExtension);
		}
	}
}
