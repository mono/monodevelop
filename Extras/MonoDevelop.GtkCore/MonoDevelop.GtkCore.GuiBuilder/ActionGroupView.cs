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
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui;

namespace MonoDevelop.GtkCore.GuiBuilder
{
	public class ActionGroupView: CombinedDesignView
	{
		Stetic.Editor.ActionGroupEditor editor;
		Stetic.PreviewBox designer;
		Hashtable actionCopyMap = new Hashtable ();
		Stetic.Wrapper.ActionGroup groupCopy;
		Stetic.Wrapper.ActionGroup group;
		CodeBinder codeBinder;
		GuiBuilderProject project;
		
		MonoDevelopActionGroupToolbar toolbar;
		Gtk.VBox vbox;
		
		public ActionGroupView (IViewContent content, Stetic.Wrapper.ActionGroup group, GuiBuilderProject project): base (content)
		{
			this.project = project;
			
			vbox = new Gtk.VBox ();
			toolbar = new MonoDevelopActionGroupToolbar (true);
			toolbar.BindField += new EventHandler (OnBindField);
			vbox.PackStart (toolbar, false, false, 0);
			vbox.ShowAll ();
			
			Gtk.EventBox b = new Gtk.EventBox ();
			vbox.PackStart (b, true, true, 3);
			b.ShowAll ();
			AddButton (GettextCatalog.GetString ("Actions"), vbox);
			
			editor = new Stetic.Editor.ActionGroupEditor ();
			
			designer = Stetic.EmbedWindow.Wrap (editor, -1, -1);
			b.Add (designer);
			toolbar.Bind (editor);
			Load (group);
			editor.GroupModified += new EventHandler (OnGroupModified);

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
			editor.Dispose ();
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
					editor.SelectedAction = (Stetic.Wrapper.Action) e.Key;
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
			OnDirtyChanged (a);
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
			if (editor.SelectedAction != null) {
				codeBinder.BindToField (editor.SelectedAction);
			}
		}
	}
}
