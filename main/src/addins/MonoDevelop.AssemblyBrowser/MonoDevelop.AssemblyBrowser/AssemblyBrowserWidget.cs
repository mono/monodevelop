//
// AssemblyBrowserWidget.cs
//
// Author:
//   Mike Krüger <mkrueger@novell.com>
//
// Copyright (C) 2008 Novell, Inc (http://www.novell.com)
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
using Mono.Cecil;

using MonoDevelop.Core;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Gui.Pads;
using MonoDevelop.Ide.Commands;
using MonoDevelop.Components.Commands;

namespace MonoDevelop.AssemblyBrowser
{
	public partial class AssemblyBrowserWidget : Gtk.Bin
	{
		MonoDevelopTreeView treeView;

		public MonoDevelopTreeView TreeView {
			get {
				return treeView;
			}
		}
		
		public AssemblyBrowserWidget ()
		{
			this.Build();
			treeView = new MonoDevelopTreeView (new NodeBuilder[] { 
				new ErrorNodeBuilder (),
				new AssemblyNodeBuilder (),
				new ModuleReferenceNodeBuilder (),
				new ModuleDefinitionNodeBuilder (),
				new ReferenceFolderNodeBuilder (this),
				new ResourceFolderNodeBuilder (),
				new ResourceNodeBuilder (),
				new NamespaceBuilder (),
				new DomTypeNodeBuilder (/*this*/),
				new DomMethodNodeBuilder (),
				new DomFieldNodeBuilder (),
				new DomEventNodeBuilder (),
				new DomPropertyNodeBuilder (),
				new BaseTypeFolderNodeBuilder (),
				new DomReturnTypeNodeBuilder ()
				}, new TreePadOption [] {});
			scrolledwindow2.Add (treeView);
			scrolledwindow2.ShowAll ();
			
			this.descriptionLabel.ModifyFont (Pango.FontDescription.FromString ("Sans 9"));
			this.disassemblerLabel.ModifyFont (Pango.FontDescription.FromString ("Monospace 10"));
			this.disassemblerLabel.ModifyBg (Gtk.StateType.Normal, new Gdk.Color (255, 255, 220));
			this.decompilerLabel.ModifyFont (Pango.FontDescription.FromString ("Monospace 10"));
			this.decompilerLabel.ModifyBg (Gtk.StateType.Normal, new Gdk.Color (255, 255, 220));
			
			this.vpaned1.ExposeEvent += VPaneExpose;
			this.hpaned1.ExposeEvent += HPaneExpose;
			this.notebook1.SwitchPage += delegate {
				// Hack for the switch page select all bug.
				this.disassemblerLabel.Selectable = false;
				this.decompilerLabel.Selectable = false;
			};
			
			treeView.Tree.CursorChanged += delegate {
				CreateOutput ();
			};
			this.searchInCombobox.AppendText (GettextCatalog.GetString ("Types"));
			this.searchInCombobox.AppendText (GettextCatalog.GetString ("Members"));
			this.searchInCombobox.Active = 0;
			this.searchInCombobox.Changed += delegate {
				searchMember = this.searchInCombobox.Active != 0;
				CreateColumns ();
			};
				
			this.notebook1.SetTabLabel (this.disassemblerScrolledWindow, new Label (GettextCatalog.GetString ("Disassembler")));
			this.notebook1.SetTabLabel (this.decompilerScrolledWindow, new Label (GettextCatalog.GetString ("Decompiler")));
			this.notebook1.SetTabLabel (this.searchWidget, new Label (GettextCatalog.GetString ("Search")));
			this.searchWidget.Visible = false;
				
			typeListStore = new Gtk.ListStore (typeof (Gdk.Pixbuf), // type image
			                                   typeof (string),     // name
			                                   typeof (string),     // namespace
			                                   typeof (string)     // assembly
			                                  );
			
			memberListStore = new Gtk.ListStore (typeof (Gdk.Pixbuf), // member image
			                                   typeof (string),     // name
			                                   typeof (string),     // Declaring type full name
			                                   typeof (string)     // assembly
			                                  );
			CreateColumns ();
		}
		bool searchMember = false;
		Gtk.ListStore typeListStore;
		Gtk.ListStore memberListStore;
		
		void CreateColumns ()
		{
			foreach (TreeViewColumn column in searchTreeview.Columns) {
				searchTreeview.RemoveColumn (column);
			}
			TreeViewColumn col;
			if (searchMember) {
				col = searchTreeview.AppendColumn ("Member", new Gtk.CellRendererText (), "pixbuf", 0, "text", 1);
				col.Resizable = true;
				col = searchTreeview.AppendColumn ("Declaring Type", new Gtk.CellRendererText (), "text", 2);
				col.Resizable = true;
				col = searchTreeview.AppendColumn ("Assembly", new Gtk.CellRendererText (), "text", 3);
				col.Resizable = true;
				searchTreeview.Model = memberListStore;
			} else {
				col = searchTreeview.AppendColumn ("Type", new Gtk.CellRendererText (), "pixbuf", 0, "text", 1);
				col.Resizable = true;
				col = searchTreeview.AppendColumn ("Namespace", new Gtk.CellRendererText (), "text", 2);
				col.Resizable = true;
				col = searchTreeview.AppendColumn ("Assembly", new Gtk.CellRendererText (), "text", 3);
				col.Resizable = true;
				searchTreeview.Model = typeListStore;
			}
		}
		void CreateOutput ()
		{
			MonoDevelop.Ide.Gui.Pads.ITreeNavigator nav = treeView.GetSelectedNode ();
			
			if (nav != null) {
				IAssemblyBrowserNodeBuilder builder = nav.TypeNodeBuilder as IAssemblyBrowserNodeBuilder;
				this.disassemblerLabel.Selectable = false;
				if (builder != null) {
					this.descriptionLabel.Markup   = builder.GetDescription (nav);
					this.disassemblerLabel.Markup = builder.GetDisassembly (nav);
				} else {
					this.descriptionLabel.Markup =  this.disassemblerLabel.Markup = "";
				}
				this.disassemblerLabel.Selectable = true;
				
				DomMethodNodeBuilder methodBuilder = nav.TypeNodeBuilder as DomMethodNodeBuilder;
				this.decompilerLabel.Selectable = false;
				if (methodBuilder != null) {
					this.decompilerLabel.Markup = methodBuilder.GetDecompiledCode (nav);
				} else {
					this.decompilerLabel.Markup = "";
				}
				this.decompilerLabel.Selectable = true;
			}
		}
			
		int oldSize = -1;
		void VPaneExpose (object sender, Gtk.ExposeEventArgs args)
		{
			int size = this.vpaned1.Allocation.Height - 96;
			if (size == oldSize)
				return;
			this.vpaned1.Position = oldSize = size;
		}
		int oldSize2 = -1;
		void HPaneExpose (object sender, Gtk.ExposeEventArgs args)
		{
			int size = this.Allocation.Width;
			if (size == oldSize2)
				return;
			oldSize2 = size;
			this.hpaned1.Position = Math.Min (350, this.Allocation.Width * 2 / 3);
		}
		public void AddReference (string fileName)
		{
			AssemblyDefinition assemblyDefinition = AssemblyFactory.GetAssembly (fileName);
			treeView.LoadTree (assemblyDefinition);
		}
		
		[CommandHandler (SearchCommands.Find)]
		public void ShowSearchWidget ()
		{
			this.searchWidget.Visible = true;
			this.notebook1.Page = 2;
			this.searchEntry.GrabFocus ();
			
		}
	}
}
