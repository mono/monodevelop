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
		Hashtable actionCopyMap = new Hashtable ();
		Stetic.Wrapper.ActionGroup groupCopy;
		Stetic.Wrapper.ActionGroup group;
		CodeBinder codeBinder;
		GuiBuilderProject project;
		
		MonoDevelopActionGroupToolbar toolbar;
		
		public ActionGroupView (IViewContent content, Stetic.Wrapper.ActionGroup group, GuiBuilderProject project): base (content)
		{
			this.project = project;
			
			toolbar = new MonoDevelopActionGroupToolbar (true);
			toolbar.BindField += new EventHandler (OnBindField);
			
			designer = Stetic.UserInterface.CreateActionGroupDesigner (project.SteticProject, toolbar);
			designer.ShowAll ();
			AddButton (GettextCatalog.GetString ("Actions"), designer);
			
			Load (group);
			designer.Editor.GroupModified += new EventHandler (OnGroupModified);

			codeBinder = new CodeBinder (project.Project, new OpenDocumentFileProvider (), groupCopy);
		}
		
		public Stetic.Wrapper.ActionGroup ActionGroup {
			get { return group; }
			set { Load (value); }
		}
		
		void Load (Stetic.Wrapper.ActionGroup group)
		{
			this.group = group;
			actionCopyMap.Clear ();
				
			groupCopy = new Stetic.Wrapper.ActionGroup ();
			groupCopy.Name = group.Name;
			
			foreach (Stetic.Wrapper.Action action in group.Actions) {
				Stetic.Wrapper.Action dupaction = action.Clone ();
				groupCopy.Actions.Add (dupaction);
				actionCopyMap [dupaction] = action;
			}
			groupCopy.Changed += new EventHandler (UpdateName);
			groupCopy.SignalAdded += new Stetic.SignalEventHandler (OnSignalAdded);
			groupCopy.SignalChanged += new Stetic.SignalChangedEventHandler (OnSignalChanged);
			toolbar.ActiveGroup = groupCopy;
		}
		
		public override void Save (string fileName)
		{
			base.Save (fileName);
			codeBinder.UpdateBindings (fileName);

			if (group.Name != groupCopy.Name)
				group.Name = groupCopy.Name;
			
			foreach (Stetic.Wrapper.Action actionCopy in groupCopy.Actions) {
				Stetic.Wrapper.Action action = (Stetic.Wrapper.Action) actionCopyMap [actionCopy];
				if (action != null)
					action.CopyFrom (actionCopy);
				else {
					action = actionCopy.Clone ();
					actionCopyMap [actionCopy] = action;
					group.Actions.Add (action);
				}
			}
			
			ArrayList todelete = new ArrayList ();
			foreach (Stetic.Wrapper.Action actionCopy in actionCopyMap.Keys) {
				if (!groupCopy.Actions.Contains (actionCopy))
					todelete.Add (actionCopy);
			}
			
			foreach (Stetic.Wrapper.Action actionCopy in todelete) {
				Stetic.Wrapper.Action action = (Stetic.Wrapper.Action) actionCopyMap [actionCopy];
				group.Actions.Remove (action);
				actionCopyMap.Remove (actionCopy);
			}
			project.Save ();
		}
		
		public override void Dispose ()
		{
			designer.Dispose ();
			designer = null;
			toolbar.BindField -= new EventHandler (OnBindField);
			toolbar.Dispose ();
			toolbar = null;
			base.Dispose ();
		}
		
		public void ShowDesignerView ()
		{
			ShowPage (1);
		}
		
		public void SelectAction (Stetic.Wrapper.Action action)
		{
			foreach (DictionaryEntry e in actionCopyMap) {
				if (e.Value == action)
					designer.Editor.SelectedAction = (Stetic.Wrapper.Action) e.Key;
			}
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
			designer.UpdateObjectViewers ();
		}
		
		void UpdateName (object s, EventArgs a)
		{
		}
		
		void OnGroupModified (object s, EventArgs a)
		{
			OnContentChanged (a);
			IsDirty = true;
		}
		
		void OnSignalAdded (object s, Stetic.SignalEventArgs a)
		{
			codeBinder.BindSignal (a.Signal);
		}
		
		void OnSignalChanged (object s, Stetic.SignalChangedEventArgs a)
		{
			codeBinder.UpdateSignal (a.OldSignal, a.Signal);
		}
		
		void OnBindField (object s, EventArgs args)
		{
			if (designer.Editor.SelectedAction != null) {
				codeBinder.BindToField (designer.Editor.SelectedAction);
			}
		}
	}
}
