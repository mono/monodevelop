//
// EditorHost.cs: Host for AspNetEdit designer.
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
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Drawing.Design;
using System.ComponentModel.Design.Serialization;

using MonoDevelop.Core.Gui;
using MonoDevelop.DesignerSupport.Toolbox;

using AspNetEdit.Editor.ComponentModel;
using AspNetEdit.Editor.UI;
using AspNetEdit.Integration;

namespace AspNetEdit.Editor
{
	
	public class EditorHost : GuiSyncObject, IDisposable
	{
		DesignerHost host;
		ServiceContainer services;
		RootDesignerView designerView;
		MonoDevelopProxy proxy;
		
		public EditorHost (MonoDevelopProxy proxy)
			: this (proxy, null, null)
		{
		}
		
		public EditorHost (MonoDevelopProxy proxy, string document, string fileName)
		{
			MonoDevelop.Core.Gui.Services.DispatchService.AssertGuiThread ();
			
			this.proxy = proxy;
			
			//set up the services
			services = new ServiceContainer ();
			services.AddService (typeof (INameCreationService), new NameCreationService ());
			services.AddService (typeof (ISelectionService), new SelectionService ());
			services.AddService (typeof (ITypeResolutionService), new TypeResolutionService ());
			services.AddService (typeof (IEventBindingService), new EventBindingService (proxy));
			ExtenderListService extListServ = new ExtenderListService ();
			services.AddService (typeof (IExtenderListService), extListServ);
			services.AddService (typeof (IExtenderProviderService), extListServ);
			services.AddService (typeof (ITypeDescriptorFilterService), new TypeDescriptorFilterService ());
			//services.AddService (typeof (IToolboxService), toolboxService);
			
			System.Diagnostics.Trace.WriteLine ("Creating AspNetEdit editor");
			host = new DesignerHost (services);
			
			if (document != null)
				host.Load (document, fileName);
			else
				host.NewFile ();
			
			host.Activate ();
			System.Diagnostics.Trace.WriteLine ("AspNetEdit host activated; getting designer view");
			
			IRootDesigner rootDesigner = (IRootDesigner) host.GetDesigner (host.RootComponent);
			designerView = (RootDesignerView) rootDesigner.GetView (ViewTechnology.Passthrough);
			designerView.Realized += delegate { System.Diagnostics.Trace.WriteLine ("Designer view realized"); };
		}
		
		public Gtk.Widget DesignerView {
			get { return designerView; }
		}
		
		public ServiceContainer Services {
			get { return services; }
		}
		
		public DesignerHost DesignerHost {
			get { return host; }
		}
		
		public void UseToolboxNode (ItemToolboxNode node)
		{
			ToolboxItemToolboxNode tiNode = node as ToolboxItemToolboxNode;
			
			if (tiNode != null) {
				//load the type into this process
				tiNode.Type.Load ();
				
				System.Drawing.Design.ToolboxItem ti = tiNode.GetToolboxItem ();
				ti.CreateComponents (host);
			}
		}
		
		public void LoadDocument (string document, string fileName)
		{
			System.Diagnostics.Trace.WriteLine ("Copying document to editor.");
			host.Reset ();
			
			host.Load (document, fileName);
			host.Activate ();
		}
		
		public string GetDocument ()
		{
			MonoDevelop.Core.Gui.Services.DispatchService.AssertGuiThread ();
			string doc = "";
			
			System.Console.WriteLine("persisting document");
			doc = host.PersistDocument ();
				
			return doc;
		}
		
		#region IDisposable
		
		bool disposed = false;
		public virtual void Dispose ()
		{
			System.Console.WriteLine("disposing editor host");
			if (disposed)
				return;
			
			disposed = true;
			designerView.Dispose ();
			
			GC.SuppressFinalize (this);
		}
		
		~EditorHost ()
		{
			Dispose ();
		}
		
		#endregion IDisposable
	}
}
