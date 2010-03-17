// GacReferencePanel.cs
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

namespace MonoDevelop.Ide.Projects
{
    internal class GacReferencePanel : VBox, IReferencePanel 
    {
        ListStore store = null;
        private TreeView treeView = null;

        private SelectReferenceDialog selectDialog = null;
        
        public GacReferencePanel (SelectReferenceDialog selectDialog)
        {
            this.selectDialog = selectDialog;

            store = new ListStore (typeof (string), typeof (string), typeof (SystemAssembly), typeof (bool), typeof (string), typeof (string));
            treeView = new TreeView (store);

            TreeViewColumn firstColumn = new TreeViewColumn ();
            CellRendererToggle tog_render = new CellRendererToggle ();
            tog_render.Toggled += new Gtk.ToggledHandler (AddReference);
            firstColumn.PackStart (tog_render, false);
            firstColumn.AddAttribute (tog_render, "active", 3);

            treeView.AppendColumn (firstColumn);

            TreeViewColumn secondColumn = new TreeViewColumn ();
            secondColumn.Title = GettextCatalog.GetString ("Assembly");
            CellRendererPixbuf crp = new CellRendererPixbuf ();
            secondColumn.PackStart (crp, false);
            crp.StockId = "md-package";

            CellRendererText text_render = new CellRendererText ();
            secondColumn.PackStart (text_render, true);
            secondColumn.AddAttribute (text_render, "text", 0);

            treeView.AppendColumn (secondColumn);

            treeView.AppendColumn (GettextCatalog.GetString ("Version"), new CellRendererText (), "text", 1);
            treeView.AppendColumn (GettextCatalog.GetString ("Package"), new CellRendererText (), "text", 5);

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

        private IAssemblyContext targetContext;
        private TargetFramework targetVersion;
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

                if (systemAssembly.Package.IsInternalPackage) {
                    store.AppendValues (systemAssembly.Name, 
                                        systemAssembly.Version, 
                                        systemAssembly, 
                                        false, 
                                        systemAssembly.FullName, 
                                        systemAssembly.Package.Name + " " + GettextCatalog.GetString ("(Provided by MonoDevelop)"));
                }
                else {
                    store.AppendValues (systemAssembly.Name, systemAssembly.Version, systemAssembly, false, systemAssembly.FullName, systemAssembly.Package.Name);
                }
            }
        }

        public void SignalRefChange (ProjectReference refInfo, bool newState)
        {
            TreeIter iter = new TreeIter ();

            if (store.GetIterFirst (out iter)) {
                do {
                    SystemAssembly systemAssembly = store.GetValue(iter, 2) as SystemAssembly;
                    
                    if ( (refInfo.Reference == systemAssembly.FullName) && (refInfo.Package == systemAssembly.Package) ) {
                        store.SetValue(iter, 3, newState);
                        return;
                    }
                } while (store.IterNext (ref iter));
            }

        }

        private int Sort (TreeModel model, TreeIter left, TreeIter right)
        {
            int result = String.Compare ((string)model.GetValue (left, 0), (string)model.GetValue (right, 0), StringComparison.InvariantCultureIgnoreCase);

            if (result != 0)
                return result;

            return String.Compare ((string)model.GetValue (left, 1), (string)model.GetValue (right, 1), StringComparison.InvariantCultureIgnoreCase);
        }

        public void AddReference (object sender, Gtk.ToggledArgs e)
        {
            Gtk.TreeIter iter;
            store.GetIterFromString (out iter, e.Path);
            if ((bool)store.GetValue (iter, 3) == false) {
                store.SetValue (iter, 3, true);
                ProjectReference pr = new ProjectReference ((SystemAssembly)store.GetValue (iter, 2));
                selectDialog.AddReference (pr);
            }
            else {
                store.SetValue (iter, 3, false);
                selectDialog.RemoveReference (ReferenceType.Gac, (string)store.GetValue (iter, 4));
            }
        }
    }
}
