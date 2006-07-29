//
// EditorHost.cs: Hosts AspNetEdit in a remote process for MonoDevelop.
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

namespace AspNetEdit.Editor
{
	
	public class EditorHost : MonoDevelop.DesignerSupport.RemoteDesignerProcess
	{
		DesignerHost host;
		ServiceContainer services;
		Frame geckoFrame;
		PropertyGrid propertyGrid;
		
		public EditorHost ()
		{
			
			#if TRACE
				System.Diagnostics.TextWriterTraceListener listener = new System.Diagnostics.TextWriterTraceListener (System.Console.Out);
				System.Diagnostics.Trace.Listeners.Add (listener);
			#endif
			
			#region Designer services and host
			
			//set up the services
			services = new ServiceContainer ();
			services.AddService (typeof (INameCreationService), new NameCreationService ());
			services.AddService (typeof (ISelectionService), new SelectionService ());
			//services.AddService (typeof (IEventBindingService), new EventBindingService (window));
			services.AddService (typeof (ITypeResolutionService), new TypeResolutionService ());
			ExtenderListService extListServ = new AspNetEdit.Editor.ComponentModel.ExtenderListService ();
			services.AddService (typeof (IExtenderListService), extListServ);
			services.AddService (typeof (IExtenderProviderService), extListServ);
			services.AddService (typeof (ITypeDescriptorFilterService), new TypeDescriptorFilterService ());
			//services.AddService (typeof (IToolboxService), toolboxService);
			
			#endregion
			
			#region build the GUI
			
			Gtk.VBox outerBox = new Gtk.VBox ();
			
			geckoFrame = new Frame ();
			geckoFrame.Shadow = ShadowType.In;
			outerBox.PackEnd (geckoFrame, true, true, 0);
			
			Toolbar tb = BuildToolbar ();
			outerBox.PackStart (tb, false, false, 0);
			
			outerBox.ShowAll ();	
			base.DesignerWidget = outerBox;
			
			#endregion GUI
			
			StartGuiThread ();
			
			Gtk.Application.Invoke (delegate {
				System.Diagnostics.Trace.WriteLine ("Activating host");
				host = new DesignerHost (services);
				
				//grid picks up some services from the designer host
				propertyGrid = new PropertyGrid (services);
				propertyGrid.ShowAll ();
				base.PropertyGridWidget = propertyGrid;
				
				host.NewFile ();
				host.Activate ();
				
				IRootDesigner rootDesigner = (IRootDesigner) host.GetDesigner (host.RootComponent);
				RootDesignerView designerView = (RootDesignerView) rootDesigner.GetView (ViewTechnology.Passthrough);
				geckoFrame.Add (designerView);
				
				geckoFrame.ShowAll ();
				
				designerView.Realized += delegate { System.Diagnostics.Trace.WriteLine ("Designer view realized"); };
			});
		}
		
		Toolbar BuildToolbar ()
		{
			Toolbar buttons = new Toolbar ();
			
			// * Clipboard
			
			ToolButton undoButton = new ToolButton (Stock.Undo);
			buttons.Add (undoButton);
			undoButton.Clicked += delegate { host.RootDocument.DoCommand (EditorCommand.Undo); };

			ToolButton redoButton = new ToolButton (Stock.Redo);
			buttons.Add (redoButton);
			redoButton.Clicked += delegate { host.RootDocument.DoCommand (EditorCommand.Redo); };

			ToolButton cutButton = new ToolButton (Stock.Cut);
			buttons.Add (cutButton);
			cutButton.Clicked += delegate { host.RootDocument.DoCommand (EditorCommand.Cut); };

			ToolButton copyButton = new ToolButton (Stock.Copy);
			buttons.Add (copyButton);
			copyButton.Clicked += delegate { host.RootDocument.DoCommand (EditorCommand.Copy); };

			ToolButton pasteButton = new ToolButton (Stock.Paste);
			buttons.Add (pasteButton);
			pasteButton.Clicked += delegate { host.RootDocument.DoCommand (EditorCommand.Paste); };
			
			
			// * Text style
			
			buttons.Add (new SeparatorToolItem());
			
			ToolButton boldButton = new ToolButton (Stock.Bold);
			buttons.Add (boldButton);
			boldButton.Clicked += delegate { host.RootDocument.DoCommand (EditorCommand.Bold); };
			
			ToolButton italicButton = new ToolButton (Stock.Italic);
			buttons.Add (italicButton);
			italicButton.Clicked += delegate { host.RootDocument.DoCommand (EditorCommand.Italic); };
			
			ToolButton underlineButton = new ToolButton (Stock.Underline);
			buttons.Add (underlineButton);
			underlineButton.Clicked += delegate { host.RootDocument.DoCommand (EditorCommand.Underline); };
			
			ToolButton indentButton = new ToolButton (Stock.Indent);
			buttons.Add (indentButton);
			indentButton.Clicked += delegate { host.RootDocument.DoCommand (EditorCommand.Indent); };
			
			ToolButton unindentButton = new ToolButton (Stock.Unindent);
			buttons.Add (unindentButton);
			unindentButton.Clicked += delegate { host.RootDocument.DoCommand (EditorCommand.Outdent); };
			
			return buttons;
		}
		
		#region expose commands to MD
		
		public void UseToolboxNode (ItemToolboxNode node)
		{
			ToolboxItemToolboxNode tiNode = node as ToolboxItemToolboxNode;
			if (tiNode != null) {
				Gtk.Application.Invoke (delegate {
					System.Drawing.Design.ToolboxItem ti = tiNode.GetToolboxItem ();
					ti.CreateComponents (host);
				});
			}
		}
		
		#endregion
	}
}
