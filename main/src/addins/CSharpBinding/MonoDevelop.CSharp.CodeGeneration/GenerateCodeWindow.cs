// 
// GenerateCodeWindow.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
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
using MonoDevelop.Components;
using Gtk;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Refactoring;
using System.Collections.Generic;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Editor;

namespace MonoDevelop.CodeGeneration
{
	partial class GenerateCodeWindow : Gtk.Window
	{
		TreeStore generateActionsStore = new TreeStore (typeof(Xwt.Drawing.Image), typeof (string), typeof (ICodeGenerator));
		
		class CustomTreeView : TreeView
		{
			protected override bool OnKeyPressEvent (Gdk.EventKey evnt)
			{
				if (evnt.Key == Gdk.Key.Return || evnt.Key == Gdk.Key.KP_Enter) {
					OnSubmit (EventArgs.Empty);
					return true;
				}
				if (evnt.Key == Gdk.Key.Escape) {
					OnCancel (EventArgs.Empty);
					return true;
				}
				return base.OnKeyPressEvent (evnt);
			}
			
			protected virtual void OnSubmit (EventArgs e)
			{
				if (Submit != null)
					Submit (this, e);
			}
			public event EventHandler Submit;
			
			protected virtual void OnCancel (EventArgs e)
			{
				if (Cancel != null)
					Cancel (this, e);
			}
			public event EventHandler Cancel;
		}
		
		CustomTreeView treeviewSelection = new CustomTreeView ();
		CustomTreeView treeviewGenerateActions = new CustomTreeView ();
		IGenerateAction curInitializeObject = null;
		CodeGenerationOptions options;
		
		GenerateCodeWindow (CodeGenerationOptions options, MonoDevelop.Ide.CodeCompletion.CodeCompletionContext completionContext) : base(Gtk.WindowType.Toplevel)
		{
			this.options = options;
			this.Build ();
			scrolledwindow1.Child = treeviewGenerateActions;
			scrolledwindow1.ShowAll ();
			
			scrolledwindow2.Child = treeviewSelection;
			scrolledwindow2.ShowAll ();
			
			treeviewGenerateActions.Cancel += delegate {
				Destroy ();
			};
			treeviewGenerateActions.Submit += delegate {
				treeviewSelection.GrabFocus ();
			};
			
			treeviewSelection.Cancel += delegate {
				treeviewGenerateActions.GrabFocus ();
			};
			
			treeviewSelection.Submit += delegate {
				if (curInitializeObject != null) {
					curInitializeObject.GenerateCode (treeviewSelection);
					curInitializeObject = null;
				}
				Destroy ();
			};
			
			WindowTransparencyDecorator.Attach (this);
			
			treeviewSelection.HeadersVisible = false;
			
			treeviewGenerateActions.HeadersVisible = false;
			treeviewGenerateActions.Model = generateActionsStore;
			treeviewGenerateActions.SearchColumn = -1; // disable the interactive search

			TreeViewColumn column = new TreeViewColumn ();
			var pixbufRenderer = new CellRendererImage ();
			column.PackStart (pixbufRenderer, false);
			column.AddAttribute (pixbufRenderer, "image", 0);
			
			CellRendererText textRenderer = new CellRendererText ();
			column.PackStart (textRenderer, true);
			column.AddAttribute (textRenderer, "text", 1);
			column.Expand = true;
			treeviewGenerateActions.AppendColumn (column);
			
			treeviewGenerateActions.Selection.Changed += TreeviewGenerateActionsSelectionChanged;
			this.Remove (this.vbox1);
			BorderBox messageArea = new BorderBox ();
			messageArea.Add (vbox1);
			this.Add (messageArea);
			this.ShowAll ();
			
			int x = completionContext.TriggerXCoord;
			int y = completionContext.TriggerYCoord;

			int w, h;
			GetSize (out w, out h);
			
			int myMonitor = Screen.GetMonitorAtPoint (x, y);
			Xwt.Rectangle geometry = DesktopService.GetUsableMonitorGeometry (Screen.Number, myMonitor);

			if (x + w > geometry.Right)
				x = (int)geometry.Right - w;

			if (y + h > geometry.Bottom)
				y = y - completionContext.TriggerTextHeight - h;
			
			Move (x, y);
		}
		
		void Populate (List<ICodeGenerator> validGenerators)
		{
			foreach (var generator in validGenerators)
				generateActionsStore.AppendValues (ImageService.GetIcon (generator.Icon, IconSize.Menu), generator.Text, generator);
			
			TreeIter iter;
			if (generateActionsStore.GetIterFirst (out iter))
				treeviewGenerateActions.Selection.SelectIter (iter);
			treeviewGenerateActions.GrabFocus ();
		}

		void TreeviewGenerateActionsSelectionChanged (object sender, EventArgs e)
		{
			treeviewSelection.Model = null;
			foreach (TreeViewColumn column in treeviewSelection.Columns) {
				treeviewSelection.RemoveColumn (column);
			}
			
			TreeIter iter;
			if (treeviewGenerateActions.Selection.GetSelected (out iter)) {
				ICodeGenerator codeGenerator = (ICodeGenerator)generateActionsStore.GetValue (iter, 2);
				labelDescription.Text = codeGenerator.GenerateDescription;
				curInitializeObject = codeGenerator.InitalizeSelection (options, treeviewSelection);
			} else {
				labelDescription.Text = "";
				curInitializeObject = null;
			}
		}
		
		public static void ShowIfValid (TextEditor editor, DocumentContext context, MonoDevelop.Ide.CodeCompletion.CodeCompletionContext completionContext)
		{
			var options = CodeGenerationOptions.CreateCodeGenerationOptions (editor, context);
			
			var validGenerators = new List<ICodeGenerator> ();
			foreach (var generator in CodeGenerationService.CodeGenerators) {
				if (generator.IsValid (options))
					validGenerators.Add (generator);
			}
			if (validGenerators.Count < 1)
				return;
			
			var window = new GenerateCodeWindow (options, completionContext);
			window.Populate (validGenerators);
		}
		
		protected override bool OnFocusOutEvent (Gdk.EventFocus evnt)
		{
			Destroy ();
			return base.OnFocusOutEvent (evnt);
		}
		
		class BorderBox : HBox
		{
			public BorderBox () : base (false, 8)
			{
				BorderWidth = 3;
			}
			
			protected override bool OnExposeEvent (Gdk.EventExpose evnt)
			{
				Style.PaintFlatBox (Style,
				                    evnt.Window,
				                    StateType.Normal,
				                    ShadowType.Out,
				                    evnt.Area,
				                    this,
				                    "tooltip",
				                    Allocation.X + 1,
				                    Allocation.Y + 1,
				                    Allocation.Width - 2,
				                    Allocation.Height - 2);
				
				return base.OnExposeEvent (evnt);
			}
		}
		
/*		protected override bool OnExposeEvent (Gdk.EventExpose evnt)
		{
			Console.WriteLine ("!!!!");
			Style.PaintFlatBox (Style,
				                    evnt.Window,
				                    StateType.Normal,
				                    ShadowType.Out,
				                    evnt.Area,
				                    this,
				                    "tooltip",
				                    Allocation.X + 1,
				                    Allocation.Y + 1,
				                    Allocation.Width - 2,
				                    Allocation.Height - 2);
			return base.OnExposeEvent (evnt);
		}*/
	}
}
