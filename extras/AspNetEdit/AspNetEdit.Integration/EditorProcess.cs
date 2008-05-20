//
// EditorProcess.cs: Hosts AspNetEdit in a remote process for MonoDevelop.
//
// Authors:
//   Michael Hutchinson <m.j.hutchinson@gmail.com>
//
// Copyright (C) 2006 Michael Hutchinson
//
//
// This source code is licenced under The MIT License:
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
using Gtk;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Drawing.Design;
using System.ComponentModel.Design.Serialization;
using System.IO;

using MonoDevelop.Core.Execution;
using MonoDevelop.Core;
using MonoDevelop.DesignerSupport.Toolbox;

using AspNetEdit.Editor.UI;
using AspNetEdit.Editor.ComponentModel;
using AspNetEdit.Integration;

namespace AspNetEdit.Editor
{
	[AddinDependency ("MonoDevelop.AspNet")]
	public class EditorProcess : MonoDevelop.DesignerSupport.RemoteDesignerProcess
	{
		EditorHost host;
		ServiceContainer services;
		Frame geckoFrame;
		PropertyGrid propertyGrid;
		
		public EditorProcess ()
		{
			#if TRACE
				System.Diagnostics.TextWriterTraceListener listener = new System.Diagnostics.TextWriterTraceListener (System.Console.Out);
				System.Diagnostics.Trace.Listeners.Add (listener);
			#endif
		}
		
		public void Initialise (MonoDevelopProxy proxy, string document, string fileName)
		{
			StartGuiThread ();
			Gtk.Application.Invoke ( delegate { LoadGui (proxy, document, fileName); });
		}
		
		public EditorHost Editor {
			get { return host; }
		}
		
		protected override void HandleError (Exception e)
		{
			//remove the grid in case it was the source of the exception, as GTK# expose exceptions can fire repeatedly
			//also user should not be able to edit things when showing exceptions
			if (propertyGrid != null) {
				Gtk.Container parent = propertyGrid.Parent as Gtk.Container;
				if (parent != null)
					parent.Remove (propertyGrid);
				
				propertyGrid.Destroy ();
				propertyGrid = null;
			}
			
			//show the error message
			base.HandleError (e);
		}
		
		void LoadGui (MonoDevelopProxy proxy, string document, string fileName)
		{
			System.Diagnostics.Trace.WriteLine ("Creating AspNetEdit EditorHost");
			host = new EditorHost (proxy);
			host.Initialise (document, fileName);
			System.Diagnostics.Trace.WriteLine ("Created AspNetEdit EditorHost");
			
			System.Diagnostics.Trace.WriteLine ("Building AspNetEdit GUI");
			Gtk.VBox outerBox = new Gtk.VBox ();
			
			geckoFrame = new Frame ();
			geckoFrame.Shadow = ShadowType.In;
			geckoFrame.Add (host.DesignerView);
			outerBox.PackEnd (geckoFrame, true, true, 0);
			
			Toolbar tb = BuildToolbar ();
			outerBox.PackStart (tb, false, false, 0);
			
			outerBox.ShowAll ();
			base.DesignerWidget = outerBox;
			
			//grid picks up some services from the designer host
			propertyGrid = new PropertyGrid (host.Services);
			propertyGrid.ShowAll ();
			base.PropertyGridWidget = propertyGrid;
			System.Diagnostics.Trace.WriteLine ("Built AspNetEdit GUI");
		}
		
		Toolbar BuildToolbar ()
		{
			Toolbar buttons = new Toolbar ();
			
			// * Clipboard
			
			ToolButton undoButton = new ToolButton (Stock.Undo);
			buttons.Add (undoButton);
			undoButton.Clicked += delegate { host.DesignerHost.RootDocument.DoCommand (EditorCommand.Undo); };

			ToolButton redoButton = new ToolButton (Stock.Redo);
			buttons.Add (redoButton);
			redoButton.Clicked += delegate { host.DesignerHost.RootDocument.DoCommand (EditorCommand.Redo); };

			ToolButton cutButton = new ToolButton (Stock.Cut);
			buttons.Add (cutButton);
			cutButton.Clicked += delegate { host.DesignerHost.RootDocument.DoCommand (EditorCommand.Cut); };

			ToolButton copyButton = new ToolButton (Stock.Copy);
			buttons.Add (copyButton);
			copyButton.Clicked += delegate { host.DesignerHost.RootDocument.DoCommand (EditorCommand.Copy); };

			ToolButton pasteButton = new ToolButton (Stock.Paste);
			buttons.Add (pasteButton);
			pasteButton.Clicked += delegate { host.DesignerHost.RootDocument.DoCommand (EditorCommand.Paste); };
			
			
			// * Text style
			
			buttons.Add (new SeparatorToolItem());
			
			ToolButton boldButton = new ToolButton (Stock.Bold);
			buttons.Add (boldButton);
			boldButton.Clicked += delegate { host.DesignerHost.RootDocument.DoCommand (EditorCommand.Bold); };
			
			ToolButton italicButton = new ToolButton (Stock.Italic);
			buttons.Add (italicButton);
			italicButton.Clicked += delegate { host.DesignerHost.RootDocument.DoCommand (EditorCommand.Italic); };
			
			ToolButton underlineButton = new ToolButton (Stock.Underline);
			buttons.Add (underlineButton);
			underlineButton.Clicked += delegate { host.DesignerHost.RootDocument.DoCommand (EditorCommand.Underline); };
			
			ToolButton indentButton = new ToolButton (Stock.Indent);
			buttons.Add (indentButton);
			indentButton.Clicked += delegate { host.DesignerHost.RootDocument.DoCommand (EditorCommand.Indent); };
			
			ToolButton unindentButton = new ToolButton (Stock.Unindent);
			buttons.Add (unindentButton);
			unindentButton.Clicked += delegate { host.DesignerHost.RootDocument.DoCommand (EditorCommand.Outdent); };
			
			return buttons;
		}
		
		bool disposed = false;
		public override void Dispose ()
		{
			System.Diagnostics.Trace.WriteLine ("Disposing AspNetEdit editor process");
			
			if (disposed)
				return;
			disposed = true;
			
			host.Dispose ();		
			base.Dispose ();
			System.Diagnostics.Trace.WriteLine ("AspNetEdit editor process disposed");
		}
	}
}
