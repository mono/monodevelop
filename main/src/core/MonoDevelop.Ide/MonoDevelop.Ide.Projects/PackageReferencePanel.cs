// PackageReferencePanel.cs
//
// Author:
//     Todd Berman <tberman@sevenl.net>
//     Viktoria Dudka <viktoriad@remobjects.com>
//
// Copyright (c) 2004 Todd Berman
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
using System.Linq;
using System.Text;
using Gtk;
using MonoDevelop.Core.Assemblies;
using MonoDevelop.Projects;
using MonoDevelop.Core;
using System.Globalization;
using MonoDevelop.Ide.Gui.Components;
using MonoDevelop.Core.Text;

namespace MonoDevelop.Ide.Projects
{
    internal class PackageReferencePanel : VBox, IReferencePanel 
    {
        ListStore store = null;
        private TreeView treeView = null;
		bool showAll;
		DotNetProject configureProject;
		StringMatcher stringMatcher;
		HashSet<string> selection = new HashSet<string> ();

        private IAssemblyContext targetContext;
        private TargetFramework targetVersion;
        private SelectReferenceDialog selectDialog = null;
		
		const int ColName = 0;
		const int ColVersion = 1;
		const int ColAssembly = 2;
		const int ColSelected = 3;
		const int ColFullName = 4;
		const int ColPackage = 5;
		const int ColIcon = 6;
		const int ColMatchRank = 7;
		const int ColType = 8;
        
        public PackageReferencePanel (SelectReferenceDialog selectDialog, bool showAll)
        {
            this.selectDialog = selectDialog;
			this.showAll = showAll;
			
            store = new ListStore (typeof (string), typeof (string), typeof (SystemAssembly), typeof (bool), typeof (string), typeof (string), typeof(IconId), typeof(int), typeof(ReferenceType));
            treeView = new TreeView (store);

            TreeViewColumn firstColumn = new TreeViewColumn ();
            CellRendererToggle tog_render = new CellRendererToggle ();
            tog_render.Toggled += new Gtk.ToggledHandler (AddReference);
            firstColumn.PackStart (tog_render, false);
            firstColumn.AddAttribute (tog_render, "active", ColSelected);

            treeView.AppendColumn (firstColumn);

            TreeViewColumn secondColumn = new TreeViewColumn ();
            secondColumn.Title = GettextCatalog.GetString ("Assembly");
            CellRendererIcon crp = new CellRendererIcon ();
            secondColumn.PackStart (crp, false);
            secondColumn.AddAttribute (crp, "icon-id", ColIcon);

            CellRendererText text_render = new CellRendererText ();
            secondColumn.PackStart (text_render, true);
            secondColumn.AddAttribute (text_render, "markup", ColName);

            treeView.AppendColumn (secondColumn);

            treeView.AppendColumn (GettextCatalog.GetString ("Version"), new CellRendererText (), "markup", ColVersion);
            treeView.AppendColumn (GettextCatalog.GetString ("Package"), new CellRendererText (), "text", ColPackage);

            treeView.Columns[1].Resizable = true;

            store.SetSortColumnId (0, SortType.Ascending);
            store.SetSortFunc (0, new TreeIterCompareFunc (Sort));

            ScrolledWindow sc = new ScrolledWindow ();
            sc.ShadowType = Gtk.ShadowType.In;
            sc.Add (treeView);
            this.PackStart (sc, true, true, 0);
            ShowAll ();
            BorderWidth = 6;
        }
		
		public void SetProject (DotNetProject netProject)
		{
			selection.Clear ();
			configureProject = netProject;
			SetTargetFramework (netProject.AssemblyContext, netProject.TargetFramework);
			Reset ();
		}
		
		public void SetFilter (string filter)
		{
			if (!string.IsNullOrEmpty (filter))
				stringMatcher = StringMatcher.GetMatcher (filter, false);
			else
				stringMatcher = null;
			Reset ();
		}

        public void SetTargetFramework (IAssemblyContext targetContext, TargetFramework targetVersion)
        {
            this.targetContext = targetContext;
            this.targetVersion = targetVersion;
        }

        public void Reset ()
        {
            store.Clear ();

            foreach (SystemAssembly systemAssembly in targetContext.GetAssemblies (targetVersion)) {
                if (systemAssembly.Package.IsFrameworkPackage && systemAssembly.Name == "mscorlib")
                    continue;
				
				bool selected = IsSelected (ReferenceType.Package, systemAssembly.FullName, systemAssembly.Package.Name);
				int matchRank = 0;
				string name, version;
				
				if (stringMatcher != null) {
					string txt = systemAssembly.Name + " " + systemAssembly.Version;
					if (!stringMatcher.CalcMatchRank (txt, out matchRank))
						continue;
					int[] match = stringMatcher.GetMatch (txt);
					name = GetMatchMarkup (treeView, systemAssembly.Name, match, 0);
					version = GetMatchMarkup (treeView, systemAssembly.Version, match, systemAssembly.Name.Length + 1);
				} else {
					name = GLib.Markup.EscapeText (systemAssembly.Name);
					version = GLib.Markup.EscapeText (systemAssembly.Version);
				}
				string pkg = systemAssembly.Package.GetDisplayName ();
				if (systemAssembly.Package.IsInternalPackage)
					pkg += " " + GettextCatalog.GetString ("(Provided by {0})", BrandingService.ApplicationName);
				
				store.AppendValues (name, 
					version, 
					systemAssembly, 
					selected, 
					systemAssembly.FullName, 
					pkg,
					MonoDevelop.Ide.Gui.Stock.Package,
					matchRank,
					ReferenceType.Package);
			}
			
			if (showAll) {
				Solution openSolution = configureProject.ParentSolution;
				if (openSolution == null)
					return;
				
				Dictionary<DotNetProject,bool> references = new Dictionary<DotNetProject, bool> ();
				
				foreach (Project projectEntry in openSolution.GetAllSolutionItems<Project>()) {
	
					if (projectEntry == configureProject)
						continue;
	
					bool selected = IsSelected (ReferenceType.Project, projectEntry.Name, "");
					int matchRank = 0;
					string name;
					
					if (stringMatcher != null) {
						if (!stringMatcher.CalcMatchRank (projectEntry.Name, out matchRank))
							continue;
						int[] match = stringMatcher.GetMatch (projectEntry.Name);
						name = GetMatchMarkup (treeView, projectEntry.Name, match, 0);
					} else {
						name = GLib.Markup.EscapeText (projectEntry.Name);
					}
					
					DotNetProject netProject = projectEntry as DotNetProject;
					if (netProject != null) {
						if (ProjectReferencePanel.ProjectReferencesProject (references, null, netProject, configureProject.Name))
							continue;
					    else if (!configureProject.TargetFramework.CanReferenceAssembliesTargetingFramework (netProject.TargetFramework))
							continue;
					}
					store.AppendValues (name, "", null, selected, projectEntry.Name, "", projectEntry.StockIcon, matchRank, ReferenceType.Project);
				}
				
				foreach (FilePath file in selectDialog.GetRecentFileReferences ()) {
					bool selected = IsSelected (ReferenceType.Assembly, file, "");
					int matchRank = 0;
					string fname = file.FileName;
					string name;

					string version = string.Empty;
					try {
						string sname = SystemAssemblyService.GetAssemblyName (file);
						var aname = SystemAssemblyService.ParseAssemblyName (sname);
						version = aname.Version.ToString ();
					} catch {
						continue;
					}
					
					if (stringMatcher != null) {
						if (!stringMatcher.CalcMatchRank (fname, out matchRank))
							continue;
						int[] match = stringMatcher.GetMatch (fname);
						name = GetMatchMarkup (treeView, fname, match, 0);
					} else {
						name = GLib.Markup.EscapeText (fname);
					}
					store.AppendValues (name, version, null, selected, (string)file, GLib.Markup.EscapeText (file), MonoDevelop.Ide.Gui.Stock.OpenFolder, matchRank, ReferenceType.Assembly);
				}
			}
        }
		
		internal static string GetMatchMarkup (Gtk.Widget widget, string text, int[] matches, int startIndex)
		{
			StringBuilder result = new StringBuilder ();
			int lastPos = 0;
			var color = Mono.TextEditor.HslColor.GenerateHighlightColors (widget.Style.Base (StateType.Normal), 
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
			return result.ToString ();
		}

        public void SignalRefChange (ProjectReference refInfo, bool newState)
        {
			if (!showAll && refInfo.ReferenceType != ReferenceType.Package)
				return;
			
            TreeIter iter;
			bool found = false;
			
            if (store.GetIterFirst (out iter)) {
                do {
					if (refInfo.ReferenceType == (ReferenceType) store.GetValue(iter, ColType)) {
						switch (refInfo.ReferenceType) {
						case ReferenceType.Package:
							SystemAssembly systemAssembly = store.GetValue(iter, ColAssembly) as SystemAssembly;
							if ((refInfo.Reference == systemAssembly.FullName) && (refInfo.Package == systemAssembly.Package) )
								found = true;
							break;
						case ReferenceType.Project:
							if ((string)store.GetValue (iter, ColFullName) == refInfo.Reference)
								found = true;
							break;
						case ReferenceType.Assembly:
							if ((string)store.GetValue (iter, ColFullName) == refInfo.Reference)
								found = true;
							break;
						}
					}
                } while (!found && store.IterNext (ref iter));
            }
			if (found)
				store.SetValue(iter, ColSelected, newState);
			SetSelection (refInfo.ReferenceType, refInfo.Reference, refInfo.Package != null ? refInfo.Package.Name : "", newState);
        }
		
		void SetSelection (ReferenceType type, string name, string pkg, bool selected)
		{
			if (selected)
				selection.Add (type + " " + name + " " + pkg);
			else
				selection.Remove (type + " " + name + " " + pkg);
		}
		
		bool IsSelected (ReferenceType type, string name, string pkg)
		{
			return selection.Contains (type + " " + name + " " + pkg);
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

        public void AddReference (object sender, Gtk.ToggledArgs e)
        {
            Gtk.TreeIter iter;
            store.GetIterFromString (out iter, e.Path);
			ReferenceType rt = (ReferenceType) store.GetValue(iter, ColType);
			string fullName = (string)store.GetValue (iter, ColFullName);
            if ((bool)store.GetValue (iter, ColSelected) == false) {
                store.SetValue (iter, ColSelected, true);
				if (rt == ReferenceType.Package)
					selectDialog.AddReference (new ProjectReference ((SystemAssembly)store.GetValue (iter, ColAssembly)));
				else
					selectDialog.AddReference (new ProjectReference (rt, fullName));
            }
            else {
                store.SetValue (iter, ColSelected, false);
                selectDialog.RemoveReference (rt, fullName);
            }
        }
    }
}
