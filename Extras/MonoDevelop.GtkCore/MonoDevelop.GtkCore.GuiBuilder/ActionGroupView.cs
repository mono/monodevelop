//
// ActionGroupView.cs
//
// Author:
//   Lluis Sanchez Gual
//
// Copyright (C) 2006 Novell, Inc (http://www.novell.com)
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

using MonoDevelop.Projects;
using MonoDevelop.Projects.Parser;
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui;

namespace MonoDevelop.GtkCore.GuiBuilder
{
	public class ActionGroupView: CombinedDesignView
	{
		Stetic.ActionGroupDesigner designer;
		CodeBinder codeBinder;
		GuiBuilderProject project;
		Stetic.ActionGroupComponent group;
		
		public ActionGroupView (IViewContent content, Stetic.ActionGroupComponent group, GuiBuilderProject project): base (content)
		{
			this.project = project;
			this.group = group;
			
			designer = project.SteticProject.CreateActionGroupDesigner (group, false);
			designer.AllowActionBinding = true;
			designer.BindField += new EventHandler (OnBindField);
			
			designer.ShowAll ();
			AddButton (GettextCatalog.GetString ("Actions"), designer);
			designer.ModifiedChanged += new EventHandler (OnGroupModified);
			designer.SignalAdded += OnSignalAdded;
			designer.SignalChanged += OnSignalChanged;

			codeBinder = new CodeBinder (project.Project, new OpenDocumentFileProvider (), group);
		}
		
		public Stetic.ActionGroupComponent ActionGroup {
			get { return group; }
			set { Load (value.Name); }
		}
		
		public override void Save (string fileName)
		{
			base.Save (fileName);
			codeBinder.UpdateBindings (fileName);
			
			designer.Save ();
			project.Save ();
		}
		
		public override void Dispose ()
		{
			designer.BindField -= new EventHandler (OnBindField);
			designer.Dispose ();
			designer = null;
			base.Dispose ();
		}
		
		public void ShowDesignerView ()
		{
			ShowPage (1);
		}
		
		public void SelectAction (Stetic.ActionComponent action)
		{
			designer.SelectedAction = action;
		}
		
		public override void JumpToSignalHandler (Stetic.Signal signal)
		{
			IClass cls = codeBinder.GetClass ();
			foreach (IMethod met in cls.Methods) {
				if (met.Name == signal.Handler) {
					ShowPage (1);
					JumpTo (met.Region.BeginLine, met.Region.BeginColumn);
					break;
				}
			}
		}
		
		protected override void OnDocumentActivated ()
		{
			// FIXME: uncomment
			// designer.UpdateObjectViewers ();
		}
		
		void OnGroupModified (object s, EventArgs a)
		{
			if (designer.Modified)
				OnContentChanged (a);
			IsDirty = designer.Modified;
		}
		
		void OnSignalAdded (object s, Stetic.ComponentSignalEventArgs a)
		{
			codeBinder.BindSignal (a.Signal);
		}
		
		void OnSignalChanged (object s, Stetic.ComponentSignalEventArgs a)
		{
			codeBinder.UpdateSignal (a.OldSignal, a.Signal);
		}
		
		void OnBindField (object s, EventArgs args)
		{
			if (designer.SelectedAction != null) {
				codeBinder.BindToField (designer.SelectedAction);
			}
		}
	}
}
