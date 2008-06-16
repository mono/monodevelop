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
			}
		}
		
		void MonoDevelop.DesignerSupport.IOutlinedDocument.ReleaseOutlineWidget ()
		{
			if (outlineTreeView == null)
				return;
			
			ScrolledWindow w = (ScrolledWindow) outlineTreeView.Parent;
			w.Destroy ();
			w.Dispose ();
			outlineTreeView.Destroy ();
			outlineTreeView.Dispose ();
			outlineTreeStore.Dispose ();
			outlineTreeStore = null;
			outlineTreeView = null;
		}
		
		void UpdateDocumentOutline (object sender, ParseInformationEventArgs args)
		{
			// This event handler can get called when files other than the current content are updated. eg.
			// when loading a new document. If we didn't do this check the member combo for this tab would have
			// methods for a different class in it!
			if (Document.FileName == args.FileName && !refreshingOutline) {
				refreshingOutline = true;
				lastCU = (ICompilationUnit) args.ParseInformation.MostRecentCompilationUnit;
				GLib.Timeout.Add (1000, new GLib.TimeoutHandler (RefillOutlineStore));
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
				
				AddTreeClassContents (store, childIter, cls);
			}
		}
		
		static void AddTreeClassContents (TreeStore store, TreeIter parent, IClass cls)
		{
			foreach (ILanguageItem item in Merge (
				new IEnumerable[] { cls.Fields, cls.Events, cls.Properties, cls.InnerClasses, cls.Methods },
				delegate (object item) {
					if (item is IClass)
						return 1 - ((IClass)item).Region.BeginLine;
					else if (item is IMember)
						return 1 - ((IMember)item).Region.BeginLine;
					else
						return 0;
				}))
			{
				TreeIter childIter = store.AppendValues (parent, item);
				if (item is IClass)
					AddTreeClassContents (store, childIter, (IClass)item);
			}
		}
		
		delegate int PrecendenceSelector (object item);
		
		static IEnumerable Merge (IEnumerable<IEnumerable> enumerables, PrecendenceSelector selector)
		{
			bool carryOn = false;
			List<IEnumerator> enumerators = new List<IEnumerator> ();
			foreach (IEnumerable enumerable in enumerables) {
				IEnumerator enumerator = enumerable.GetEnumerator ();
				if (enumerator.MoveNext ())
					enumerators.Add (enumerator);
			}
			
			while (enumerators.Count > 0) {
				int maxPrecendence = int.MinValue;
				IEnumerator best = null;
				
				for (int i = 0; i < enumerators.Count; i++) {
					IEnumerator current = enumerators[i];
					int precendence = selector (current.Current);
					if (precendence > maxPrecendence) {
						maxPrecendence = precendence;
						best = current;
					}
				}
				
				yield return best.Current;
				
				if (!best.MoveNext ())
					enumerators.Remove (best);
			}
			
		}
	}
}
