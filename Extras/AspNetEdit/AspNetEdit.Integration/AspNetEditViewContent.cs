//
// AspNetEditViewContent.cs: The SecondaryViewContent that lets AspNetEdit 
//         be used as a designer in MD.
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

using MonoDevelop.Ide.Gui;
using MonoDevelop.Core;
using MonoDevelop.Core.Execution;
using MonoDevelop.DesignerSupport.Toolbox;

using AspNetEdit.Editor;

namespace AspNetEdit.Integration
{
	
	public class AspNetEditViewContent : AbstractSecondaryViewContent, IToolboxConsumer
	{
		IViewContent viewContent;
		EditorHost editor;
		Gtk.Socket designerSocket;
		Gtk.Socket propGridSocket;
		
		internal AspNetEditViewContent (IViewContent viewContent)
		{
			this.viewContent = viewContent;
			
			editor = (EditorHost) Runtime.ProcessService.CreateExternalProcessObject (typeof (EditorHost), false);
			
			designerSocket = new Gtk.Socket ();
			propGridSocket = new Gtk.Socket ();
			
			designerSocket.Realized += delegate { editor.AttachDesigner (designerSocket.Id); };
			propGridSocket.Realized += delegate { editor.AttachPropertyGrid (propGridSocket.Id); };
			
			//designerSocket.FocusOutEvent += delegate {
			//	MonoDevelop.DesignerSupport.DesignerSupport.Service.PropertyPad.BlankPad (); };
			designerSocket.FocusInEvent += delegate {
				MonoDevelop.DesignerSupport.DesignerSupport.Service.PropertyPad.UseCustomWidget (propGridSocket);
			};
			
			
		}
		
		public override Gtk.Widget Control {
			get { return designerSocket; }
		}
		
		public override string TabPageLabel {
			get { return "Designer"; }
		}
		
		public override void Dispose()
		{
			propGridSocket.Dispose ();
			designerSocket.Dispose ();
			editor.Dispose ();
		}
		
		#region IToolboxConsumer
		
		public void Use (ItemToolboxNode node)
		{
			if (node is ToolboxItemToolboxNode)
				editor.UseToolboxNode (node);
		}
		
		//used to filter toolbox items
		private static ToolboxItemFilterAttribute[] atts = new ToolboxItemFilterAttribute[] {
			new System.ComponentModel.ToolboxItemFilterAttribute ("System.Web.UI", ToolboxItemFilterType.Allow)
		};
			
		public ToolboxItemFilterAttribute[] ToolboxFilterAttributes {
			get { return atts; }
		}
		
		//Used if ToolboxItemFilterAttribute demands ToolboxItemFilterType.Custom
		//If not expecting it, should just return false
		public bool CustomFilterSupports (ItemToolboxNode item)
		{
			return false;
		}
		
		#endregion IToolboxConsumer
	}
}
