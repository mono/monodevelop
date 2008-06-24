// 
// ClassOutlineTextEditorExtension.cs
// 
// Author:
//   Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (C) 2008 Novell, Inc (http://www.novell.com)
// 
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Collections;
using System.Collections.Generic;
using Gtk;

using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Gui.Content;
using MonoDevelop.Projects.Parser;

namespace MonoDevelop.DesignerSupport
{
	
	
	public class ClassOutlineTextEditorExtension : TextEditorExtension, IOutlinedDocument
	{
		ICompilationUnit lastCU = null;
		TreeView outlineTreeView;
		TreeStore outlineTreeStore;
		bool refreshingOutline;
		bool disposed;
		
		public override bool ExtendsEditor (Document doc, IEditableTextBuffer editor)
		{
			MonoDevelop.Projects.IDotNetLanguageBinding binding = 
				MonoDevelop.Projects.Services.Languages.GetBindingPerFileName
				(doc.IsUntitled? doc.UntitledName : doc.FileName)
				as MonoDevelop.Projects.IDotNetLanguageBinding;
			return binding != null;
		}
		
		public override void Initialize ()
		{
			IdeApp.Workspace.ParserDatabase.ParseInformationChanged += UpdateDocumentOutline;
			base.Initialize ();
		}
		
		public override void Dispose ()
		{
			if (disposed)
				return;
			disposed = true;
			IdeApp.Workspace.ParserDatabase.ParseInformationChanged -= UpdateDocumentOutline;
			base.Dispose ();
		}
		
		Widget MonoDevelop.DesignerSupport.IOutlinedDocument.GetOutlineWidget ()
		{
			if (outlineTreeView != null)
				return outlineTreeView;
			
			outlineTreeStore = new TreeStore (typeof(object));
			outlineTreeView = new TreeView (outlineTreeStore);
			
			CellRendererPixbuf pixRenderer = new CellRendererPixbuf ();
			pixRenderer.Xpad = 0;
			pixRenderer.Ypad = 0;
			CellRendererText textRenderer = new CellRendererText ();
			textRenderer.Xpad = 0;
			textRenderer.Ypad = 0;
			
			TreeViewColumn treeCol = new TreeViewColumn ();
			treeCol.PackStart (pixRenderer, false);
			treeCol.SetCellDataFunc (pixRenderer, new TreeCellDataFunc (OutlineTreeIconFunc));
			treeCol.PackStart (textRenderer, true);
			treeCol.SetCellDataFunc (textRenderer, new TreeCellDataFunc (OutlineTreeTextFunc));
			outlineTreeView.AppendColumn (treeCol);
			
			outlineTreeView.HeadersVisible = false;
			
			outlineTreeView.Selection.Changed += delegate {
				TreeIter iter;
				if (!outlineTreeView.Selection.GetSelected (out iter))
					return;
				object o = outlineTreeStore.GetValue (iter, 0);
				int line = -1, col = -1;
				if (o is IClass) {
					line = ((IClass)o).Region.BeginLine;
					col = ((IClass)o).Region.BeginColumn;
				} else if (o is IMember) {
					line = ((IMember)o).Region.BeginLine;
					col = ((IMember)o).Region.BeginColumn;
				}
				if (line > -1) {
					Editor.JumpTo (line, Math.Max (1, col));
				}
			};
			
			IFileParserContext context = IdeApp.Workspace.ParserDatabase.GetFileParserContext (this.FileName);
			this.lastCU = (ICompilationUnit) context.ParseFile (this.FileName).MostRecentCompilationUnit;
			outlineTreeView.Realized += delegate { RefillOutlineStore (); };
						
			ScrolledWindow sw = new ScrolledWindow ();
			sw.Add (outlineTreeView);
			sw.ShowAll ();
			return sw;
		}
		
		void OutlineTreeIconFunc (TreeViewColumn column, CellRenderer cell, TreeModel model, TreeIter iter)
		{
			CellRendererPixbuf pixRenderer = (CellRendererPixbuf) cell;
			object o = model.GetValue (iter, 0);
			if (o is IClass) {
				pixRenderer.Pixbuf = IdeApp.Services.Resources.GetIcon
					(IdeApp.Services.Icons.GetIcon ((IClass)o), IconSize.Menu);
			} else if (o is IMember) {
				pixRenderer.Pixbuf = IdeApp.Services.Resources.GetIcon
					(IdeApp.Services.Icons.GetIcon ((IMember)o), IconSize.Menu);
			} else if (o is FoldingRegion) {
				pixRenderer.Pixbuf = IdeApp.Services.Resources.GetIcon ("gtk-add", IconSize.Menu);
			}
		}
		
		void OutlineTreeTextFunc (TreeViewColumn column, CellRenderer cell, TreeModel model, TreeIter iter)
		{
			CellRendererText txtRenderer = (CellRendererText) cell;
			object o = model.GetValue (iter, 0);
			MonoDevelop.Projects.Ambience.Ambience am = GetAmbience ();
			if (o is IClass) {
				txtRenderer.Text = am.Convert ((IClass)o,
				                               MonoDevelop.Projects.Ambience.ConversionFlags.UseIntrinsicTypeNames |
				                               MonoDevelop.Projects.Ambience.ConversionFlags.ShowParameters |
				                               MonoDevelop.Projects.Ambience.ConversionFlags.ShowParameterNames |
				                               MonoDevelop.Projects.Ambience.ConversionFlags.ShowGenericParameters);
			} else if (o is IMember) {
				txtRenderer.Text = am.Convert ((IMember)o,
				                               MonoDevelop.Projects.Ambience.ConversionFlags.UseIntrinsicTypeNames |
				                               MonoDevelop.Projects.Ambience.ConversionFlags.ShowParameters |
				                               MonoDevelop.Projects.Ambience.ConversionFlags.ShowParameterNames |
				                               MonoDevelop.Projects.Ambience.ConversionFlags.ShowGenericParameters);
			} else if (o is FoldingRegion) {
				string name = ((FoldingRegion)o).Name.Trim ();
				if (string.IsNullOrEmpty (name))
					name = "#region";
				txtRenderer.Text = name;
			}
		}
		
		void MonoDevelop.DesignerSupport.IOutlinedDocument.ReleaseOutlineWidget ()
		{
			if (outlineTreeView == null)
				return;
			
			ScrolledWindow w = (ScrolledWindow) outlineTreeView.Parent;
			w.Dispose ();
			outlineTreeView.Destroy ();
			outlineTreeStore.Dispose ();
			outlineTreeStore = null;
			outlineTreeView = null;
		}
		
		void UpdateDocumentOutline (object sender, ParseInformationEventArgs args)
		{
			// This event handler can get called when files other than the current content are updated. eg.
			// when loading a new document. If we didn't do this check the member combo for this tab would have
			// methods for a different class in it!
			if (Document.FileName == args.FileName) {
				lastCU = (ICompilationUnit) args.ParseInformation.MostRecentCompilationUnit;
				//limit update rate to 5s
				if (!refreshingOutline) {
					refreshingOutline = true;
					GLib.Timeout.Add (5000, new GLib.TimeoutHandler (RefillOutlineStore));
				}
			}
		}
		
		bool RefillOutlineStore ()
		{
			MonoDevelop.Core.Gui.DispatchService.AssertGuiThread ();
			Gdk.Threads.Enter ();
			refreshingOutline = false;
			if (outlineTreeStore == null || !outlineTreeView.IsRealized)
				return false;
			
			outlineTreeStore.Clear ();
			if (lastCU != null) {
				BuildTreeChildren (outlineTreeStore, TreeIter.Zero, lastCU);
				outlineTreeView.ExpandAll ();
			}
			
			Gdk.Threads.Leave ();
			
			//stop timeout handler
			return false;
		}
		
		static void BuildTreeChildren (TreeStore store, TreeIter parent, ICompilationUnit unit)
		{
			foreach (IClass cls in unit.Classes) {
				TreeIter childIter;
				if (!parent.Equals (TreeIter.Zero))
					childIter = store.AppendValues (parent, cls);
				else
					childIter = store.AppendValues (cls);
				
				AddTreeClassContents (store, childIter, unit, cls);
			}
		}
		
		static void AddTreeClassContents (TreeStore store, TreeIter parent, ICompilationUnit unit, IClass cls)
		{
			List<object> items = new List<object> ();
			foreach (object o in cls.Fields)
				items.Add (o);
			foreach (object o in cls.Properties)
				items.Add (o);
			foreach (object o in cls.Methods)
				items.Add (o);
			foreach (object o in cls.Events)
				items.Add (o);
			foreach (object o in cls.InnerClasses)
				items.Add (o);
			
			items.Sort (delegate (object x, object y) {
				IRegion r1 = GetRegion (x), r2 = GetRegion (y);
				return r1.CompareTo (r2);
			});
			
			List<FoldingRegion> regions = new List<FoldingRegion> ();
			foreach (FoldingRegion fr in unit.FoldingRegions)
				//check regions inside class
				if (RegionContains (cls.Region, fr.Region))
					regions.Add (fr);
			regions.Sort (delegate (FoldingRegion x, FoldingRegion y) { return x.Region.CompareTo (y.Region); });
			
			IEnumerator<FoldingRegion> regionEnumerator = regions.GetEnumerator ();
			if (!regionEnumerator.MoveNext ())
				regionEnumerator = null;
			
			FoldingRegion currentRegion = null;
			TreeIter currentParent = parent;
			foreach (object item in items) {
				
				//no regions left; quick exit
				if (regionEnumerator != null) {
					IRegion itemRegion = GetRegion (item);
					
					//advance to a region that could potentially contain this member
					while (regionEnumerator != null && !OuterEndsAfterInner (regionEnumerator.Current.Region, itemRegion))
						if (!regionEnumerator.MoveNext ())
							regionEnumerator = null;
					
					//if member is within region, make sure it's the current parent.
					//If not, move target iter back to class parent
					if (regionEnumerator != null && RegionContains (regionEnumerator.Current.Region, itemRegion)) {
						if (currentRegion != regionEnumerator.Current) {
							currentParent = store.AppendValues (parent, regionEnumerator.Current);
							currentRegion = regionEnumerator.Current;
						}
					} else {
						currentParent = parent;
					}	
				}
				
				
				TreeIter childIter = store.AppendValues (currentParent, item);
				if (item is IClass)
					AddTreeClassContents (store, childIter, unit, (IClass)item);
			}
		}
		
		static IRegion GetRegion (object o)
		{
			if (o is IClass)
				return ((IClass)o).Region;
			else if (o is IMember)
				return ((IMember)o).Region;
			else
				throw new InvalidOperationException (o.GetType ().ToString ());
		}
		
		static bool OuterEndsAfterInner (IRegion outer, IRegion inner)
		{
			return (outer.EndLine > inner.EndLine
			        || (outer.EndLine == inner.EndLine && outer.EndColumn > inner.EndColumn));
		}
		
		static bool RegionContains (IRegion outer, IRegion inner)
		{
			return
				//check beginning
				(outer.BeginLine < inner.BeginLine
					|| (outer.BeginLine == inner.BeginLine && outer.BeginColumn < inner.BeginColumn))
				//check end
				&& (outer.EndLine > inner.EndLine
					|| (outer.EndLine == inner.EndLine && outer.EndColumn > inner.EndColumn));
			
		}
	}
}
