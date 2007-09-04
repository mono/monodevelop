// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike KrÃ¼ger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;

using Mono.Addins;
using MonoDevelop.Core;
using MonoDevelop.Core.Gui.Codons;
using MonoDevelop.Components.Commands;

namespace MonoDevelop.Core.Gui.Dialogs {
	
	/// <summary>
	/// TreeView options are used, when more options will be edited (for something like
	/// IDE Options + Plugin Options)
	/// </summary>
	public class TreeViewOptions {
		List<IDialogPanel> optionPanels = new List<IDialogPanel> ();
		protected Properties properties = null;
		CommandManager cmdManager;
		Dictionary<string, Gtk.TreeIter> descriptors = new Dictionary<string, Gtk.TreeIter> ();

		protected Gtk.TreeStore treeStore;
		
		[Glade.Widget] protected Gtk.TreeView  TreeView;
		[Glade.Widget] protected Gtk.ScrolledWindow TreeViewScrolledWindow;
		[Glade.Widget] protected Gtk.VBox TreeViewContainer;
		[Glade.Widget] Gtk.Label     optionTitle;
		[Glade.Widget] Gtk.Notebook  mainBook;
		[Glade.Widget] Gtk.Image     panelImage;
		[Glade.Widget] Gtk.Dialog    TreeViewOptionDialog;
		
		public Properties Properties {
			get {
				return properties;
			}
		}

		protected string Title {
			set {
				TreeViewOptionDialog.Title = value;
			}
		}
		
		protected void AcceptEvent (object sender, EventArgs e)
		{
			StoreContents ();
		}
		
		protected virtual bool StoreContents ()
		{
			foreach (IDialogPanel pane in optionPanels) {
				if (!pane.WasActivated)
					continue;
				if (!pane.ReceiveDialogMessage (DialogMessage.OK))
					return false;
			}
			TreeViewOptionDialog.Hide ();
			return true;
		}
		
		protected CommandManager CommandManager {
			get { return cmdManager; }
		}
	
		public int Run ()
		{
			int r = TreeViewOptionDialog.Run ();
			cmdManager.Dispose ();
			return r;
		}
	
		protected bool b = true;
		
		protected void SetOptionPanelTo(IDialogPanelDescriptor descriptor)
		{
			if (descriptor != null && descriptor.DialogPanel != null) {
				descriptor.DialogPanel.ReceiveDialogMessage(DialogMessage.Activated);
				mainBook.CurrentPage = mainBook.PageNum (descriptor.DialogPanel.Control);
				if (descriptor.DialogPanel.Icon == null) {
					panelImage.Stock = Gtk.Stock.Preferences;
				} else {
					//FIXME: this needs to actually switch over the ImageType and use that instead of this *hack*
					if (descriptor.DialogPanel.Icon.Stock != null)
						panelImage.Stock = descriptor.DialogPanel.Icon.Stock;
					else
						panelImage.Pixbuf = descriptor.DialogPanel.Icon.Pixbuf;
				}
				optionTitle.Markup = "<span weight=\"bold\" size=\"x-large\">" + descriptor.Label + "</span>";
				TreeViewOptionDialog.ShowAll ();
			}
		}		
		
		protected void AddNodes (object customizer, Gtk.TreeIter iter, IEnumerable descriptors)
		{
			foreach (IDialogPanelDescriptor descriptor in descriptors)
				AddNode (descriptor.Label, customizer, iter, descriptor);
		}
		
		protected virtual void AddNode (string label, object customizer, Gtk.TreeIter iter, IDialogPanelDescriptor descriptor)
		{
			if (descriptor.DialogPanel != null) { // may be null, if it is only a "path"
				descriptor.DialogPanel.CustomizationObject = customizer;
				if (descriptor.DialogPanel.Control is Gtk.Frame) 
					((Gtk.Frame)descriptor.DialogPanel.Control).Shadow = Gtk.ShadowType.None;
				optionPanels.Add (descriptor.DialogPanel);
				mainBook.AppendPage (descriptor.DialogPanel.Control, new Gtk.Label ("a"));
			}
			
			Gtk.TreeIter i;
			if (iter.Equals (Gtk.TreeIter.Zero)) {
				i = treeStore.AppendValues (label, descriptor);
			} else {
				i = treeStore.AppendValues (iter, label, descriptor);
			}

			descriptors [descriptor.ID] = i;
			AddChildNodes (customizer, i, descriptor);
			
			if (iter.Equals (Gtk.TreeIter.Zero))
				TreeView.ExpandRow (treeStore.GetPath (i), false);
		}
		
		protected virtual Gtk.TreeIter AddPath (string label, Gtk.TreeIter iter)
		{
			if (iter.Equals (Gtk.TreeIter.Zero))
				return treeStore.AppendValues (label, null);
			else
				return treeStore.AppendValues (iter, label, null);
		}
		
		protected virtual void AddChildNodes (object customizer, Gtk.TreeIter iter, IDialogPanelDescriptor descriptor)
		{
			if (descriptor.DialogPanelDescriptors != null) {
				AddNodes (customizer, iter, descriptor.DialogPanelDescriptors);
			}
		}
		
		protected virtual void SelectNode(object sender, EventArgs e)
		{
			Gtk.TreeModel mdl;
			Gtk.TreeIter  iter;
			if (TreeView.Selection.GetSelected (out mdl, out iter)) {
				IDialogPanelDescriptor descriptor = treeStore.GetValue (iter, 1) as IDialogPanelDescriptor;
				OnSelectNode (iter, descriptor);
			}
		}
		
		protected virtual void OnSelectNode (Gtk.TreeIter iter, IDialogPanelDescriptor descriptor)
		{
			if (treeStore.IterHasChild (iter) && (descriptor == null || descriptor.DialogPanel == null)) {
				Gtk.TreeIter new_iter;
				treeStore.IterChildren (out new_iter, iter);
				Gtk.TreePath new_path = treeStore.GetPath (new_iter);
				TreeView.ExpandToPath (new_path);
				TreeView.Selection.SelectPath (new_path);
			} else {
				SetOptionPanelTo (descriptor);
			}
		}
		
		// selects a specific node in the treeview options
		protected void SelectSpecificNode(Gtk.TreeIter iter)
		{
			TreeView.GrabFocus();
			Gtk.TreePath new_path = treeStore.GetPath (iter);
			TreeView.ExpandToPath (new_path);
			TreeView.Selection.SelectPath (new_path);
			IDialogPanelDescriptor descriptor = treeStore.GetValue (iter, 1) as IDialogPanelDescriptor;  
			if (descriptor != null)
				SetOptionPanelTo (descriptor);
		}
		
		public void SelectPanel (string id)
		{
			object it = descriptors [id];
			if (it != null)
				SelectSpecificNode ((Gtk.TreeIter) it);
		}
		
		public TreeViewOptions (Gtk.Window parentWindow, Properties properties, ExtensionNode node)
		{
			this.properties = properties;
			
			Glade.XML treeViewXml = new Glade.XML (null, "Base.glade", "TreeViewOptionDialog", null);
			treeViewXml.Autoconnect (this);
		
			TreeViewOptionDialog.TransientFor = parentWindow;
			TreeViewOptionDialog.WindowPosition = Gtk.WindowPosition.CenterOnParent;
		
			TreeViewOptionDialog.Title = GettextCatalog.GetString ("MonoDevelop Preferences");

			cmdManager = new CommandManager (TreeViewOptionDialog);
			cmdManager.RegisterGlobalHandler (this); 
			
			this.InitializeComponent();
			
			if (node != null)
				AddNodes (properties, Gtk.TreeIter.Zero, node.GetChildObjects (false));
			
			SelectFirstNode ();
		}

		protected void SelectFirstNode ()
		{
			TreeView.GrabFocus ();
			SelectNode (null, null);
		}
		
		// this is virtual so that inheriting classes can extend (for example to make the text cell editable)
		protected virtual void InitializeComponent () 
		{
			treeStore = new Gtk.TreeStore (typeof (string), typeof (IDialogPanelDescriptor));
			
			TreeView.Model = treeStore;
			TreeView.AppendColumn ("", new Gtk.CellRendererText (), "text", 0);
			TreeView.Selection.Changed += new EventHandler (SelectNode);
		}
		
		internal void CancelEvent (object o, EventArgs args)
		{
			TreeViewOptionDialog.Hide ();
		}

		// Glade tries to find this event (glade signal is wired to it)
		protected virtual void OnButtonRelease(object sender, Gtk.ButtonReleaseEventArgs e)
		{
			// do nothing. this is need to wire up button release event for ProjectOptionsDialog
		}

	}
}
