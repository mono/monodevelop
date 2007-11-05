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
using Gtk;

using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Gui.Content;
using MonoDevelop.Core;
using MonoDevelop.Core.Execution;
using MonoDevelop.DesignerSupport.Toolbox;
using MonoDevelop.DesignerSupport;
using MonoDevelop.DesignerSupport.PropertyGrid;
using AspNetEdit.Editor;

namespace AspNetEdit.Integration
{
	
	public class AspNetEditViewContent : AbstractSecondaryViewContent, IToolboxConsumer //, IEditableTextBuffer
	{
		IViewContent viewContent;
		EditorProcess editorProcess;
		
		Gtk.Socket designerSocket;
		Gtk.Socket propGridSocket;
		
		DesignerFrame designerFrame;
		
		MonoDevelopProxy proxy;
		
		bool activated = false;
		bool suppressSerialisation = false;
		
		internal AspNetEditViewContent (IViewContent viewContent)
		{
			this.viewContent = viewContent;
			
			designerFrame = new DesignerFrame (this);
			designerFrame.CanFocus = true;
			designerFrame.Shadow = ShadowType.None;
			designerFrame.BorderWidth = 0;
			
			viewContent.WorkbenchWindow.Closing += workbenchWindowClosingHandler;
			viewContent.DirtyChanged += vcDirtyChanged;
			viewContent.BeforeSave += vcBeforeSave;
			
			designerFrame.Show ();
		}
		
		void workbenchWindowClosingHandler (object sender, WorkbenchWindowEventArgs args)
		{
			if (activated)
				suppressSerialisation = true;
		}
		
		void vcDirtyChanged (object sender, System.EventArgs e)
		{
			if (activated && !viewContent.IsDirty)
				viewContent.IsDirty = true;
		}
				
		void vcBeforeSave (object sender, System.EventArgs e)
		{
			if (activated)
				saveDocumentToTextView ();
		}
		
		public override Gtk.Widget Control {
			get { return designerFrame; }
		}
		
		public override string TabPageLabel {
			get { return "Designer"; }
		}
		
		bool disposed = false;
		
		public override void Dispose ()
		{
			if (disposed)
				return;
			
			disposed = true;
			
			base.WorkbenchWindow.Closing -= workbenchWindowClosingHandler;
			viewContent.DirtyChanged -= vcDirtyChanged;
			viewContent.BeforeSave -= vcBeforeSave;
			
			DestroyEditorAndSockets ();
			designerFrame.Destroy ();
			base.Dispose ();
		}
		
		public override void Selected ()
		{
			if (editorProcess != null)
				throw new Exception ("Editor should be null when document is selected");
			
			designerSocket = new Gtk.Socket ();
			designerSocket.Show ();
			designerFrame.Add (designerSocket);
			
			propGridSocket = new Gtk.Socket ();
			propGridSocket.Show ();
			
			editorProcess = (EditorProcess) Runtime.ProcessService.CreateExternalProcessObject (typeof (EditorProcess), false);
			
			if (designerSocket.IsRealized)
				editorProcess.AttachDesigner (designerSocket.Id);
			if (propGridSocket.IsRealized)
				editorProcess.AttachPropertyGrid (propGridSocket.Id);
			
			designerSocket.Realized += delegate { editorProcess.AttachDesigner (designerSocket.Id); };
			propGridSocket.Realized += delegate { editorProcess.AttachPropertyGrid (propGridSocket.Id); };
			
			//designerSocket.FocusOutEvent += delegate {
			//	MonoDevelop.DesignerSupport.DesignerSupport.Service.PropertyPad.BlankPad (); };
			
			//hook up proxy for event binding
			MonoDevelop.Projects.Parser.IClass codeBehind = null;
			if (viewContent.Project != null) {
				MonoDevelop.Projects.ProjectFile pf = viewContent.Project.GetProjectFile (viewContent.ContentName);
				if (pf != null) {
					MonoDevelop.DesignerSupport.CodeBehind.CodeBehindClass cc = 
						DesignerSupport.Service.CodeBehindService.GetChildClass (pf);
					if (cc != null)
						codeBehind = cc.IClass;
				}
			}
			proxy = new MonoDevelopProxy (viewContent.Project, codeBehind);
			
			ITextBuffer textBuf = (ITextBuffer) viewContent.GetContent (typeof(ITextBuffer));			
			editorProcess.Initialise (proxy, textBuf.Text, viewContent.ContentName);
			
			activated = true;
			
			//FIXME: track 'dirtiness' properly
			viewContent.IsDirty = true;
		}
		
		public override void Deselected ()
		{
			activated = false;
			
			//don't need to save if window is closing
			if (!suppressSerialisation)
				saveDocumentToTextView ();
			
			DestroyEditorAndSockets ();
		}
			
		void saveDocumentToTextView ()
		{
			if (!editorProcess.ExceptionOccurred) {
				IEditableTextBuffer textBuf = (IEditableTextBuffer) viewContent.GetContent (typeof(IEditableTextBuffer));
				
				string doc = null;
				try {
					doc = editorProcess.Editor.GetDocument ();
				} catch (Exception e) {
					IdeApp.Services.MessageService.ShowError (e, "The document could not be retrieved from the designer");
				}
			
				if (doc != null)
					textBuf.Text = doc;
			}
		}
		
		void DestroyEditorAndSockets ()
		{
			if (proxy != null) {
				proxy.Dispose ();
				proxy = null;
			}
			
			if (editorProcess != null) {
				editorProcess.Dispose ();
				editorProcess = null;
			}
			
			if (propGridSocket != null) {
				propGridSocket.Dispose ();
				propGridSocket = null;
			}
			
			if (designerSocket != null) {
				designerFrame.Remove (designerSocket);
				designerSocket.Dispose ();
				designerSocket = null;
			}
		}
		
		#region IToolboxConsumer
		
		public void ConsumeItem (ItemToolboxNode node)
		{
			if (node is ToolboxItemToolboxNode)
				editorProcess.Editor.UseToolboxNode (node);
		}
		
		//used to filter toolbox items
		private static ToolboxItemFilterAttribute[] atts = new ToolboxItemFilterAttribute[] {
			new System.ComponentModel.ToolboxItemFilterAttribute ("System.Web.UI", ToolboxItemFilterType.Allow)
		};
			
		public ToolboxItemFilterAttribute[] ToolboxFilterAttributes {
			get { return atts; }
		}
		
		public System.Collections.Generic.IList<ItemToolboxNode> GetDynamicItems ()
		{
			return null;
		}
		
		//Used if ToolboxItemFilterAttribute demands ToolboxItemFilterType.Custom
		//If not expecting it, should just return false
		public bool CustomFilterSupports (ItemToolboxNode item)
		{
			return false;
		}
		
		public void DragItem (ItemToolboxNode item, Widget source, Gdk.DragContext ctx)
		{
		}
		
		public TargetEntry[] DragTargets {
			get { return null; }
		}
		
		string IToolboxConsumer.DefaultItemDomain {
			get { return null; }
		}

		#endregion IToolboxConsumer
		
		class DesignerFrame: Frame, ICustomPropertyPadProvider
		{
			AspNetEditViewContent view;
			
			public DesignerFrame (AspNetEditViewContent view)
			{
				this.view = view;
			}
			
			Gtk.Widget ICustomPropertyPadProvider.GetCustomPropertyWidget ()
			{
				return view.propGridSocket;
			}
			
			void ICustomPropertyPadProvider.DisposeCustomPropertyWidget ()
			{
			}
		}
	}
}
