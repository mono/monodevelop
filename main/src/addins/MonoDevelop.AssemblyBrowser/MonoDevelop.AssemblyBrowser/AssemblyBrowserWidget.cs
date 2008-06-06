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
using System.Collections.Generic;
using System.Threading;

using Gtk;
using Mono.Cecil;

using MonoDevelop.Core;
using MonoDevelop.Core.Gui;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Gui.Pads;
using MonoDevelop.Ide.Commands;
using MonoDevelop.Components.Commands;
using MonoDevelop.Projects.Dom;

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
			this.searchInCombobox.AppendText (GettextCatalog.GetString ("Disassembler"));
			this.searchInCombobox.AppendText (GettextCatalog.GetString ("Decompiler"));
			this.searchInCombobox.Active = 0;
			this.searchInCombobox.Changed += delegate {
				this.searchMode = (SearchMode)this.searchInCombobox.Active;
				CreateColumns ();
				StartSearch ();
			};
				
			this.notebook1.SetTabLabel (this.disassemblerScrolledWindow, new Label (GettextCatalog.GetString ("Disassembler")));
			this.notebook1.SetTabLabel (this.decompilerScrolledWindow, new Label (GettextCatalog.GetString ("Decompiler")));
			this.notebook1.SetTabLabel (this.searchWidget, new Label (GettextCatalog.GetString ("Search")));
			//this.searchWidget.Visible = false;
				
			typeListStore = new Gtk.ListStore (typeof (Gdk.Pixbuf), // type image
			                                   typeof (string),     // name
			                                   typeof (string),     // namespace
			                                   typeof (string),     // assembly
				                               typeof (IMember)
			                                  );
			
			memberListStore = new Gtk.ListStore (typeof (Gdk.Pixbuf), // member image
			                                   typeof (string),     // name
			                                   typeof (string),     // Declaring type full name
			                                   typeof (string),     // assembly
				                               typeof (IMember)
			                                  );
			CreateColumns ();
			this.searchEntry.Changed += delegate {
				StartSearch ();
			};
			this.searchTreeview.RowActivated += delegate {
				Gtk.TreeIter selectedIter;
				if (searchTreeview.Selection.GetSelected (out selectedIter)) {
					MonoDevelop.Projects.Dom.IMember member = (MonoDevelop.Projects.Dom.IMember)(searchMode != SearchMode.Type ? memberListStore.GetValue (selectedIter, 4) : typeListStore.GetValue (selectedIter, 4));
					MonoDevelop.Ide.Gui.Pads.ITreeNavigator nav = SearchMember (member);
					if (nav != null) {
						nav.ExpandToNode ();
						nav.Selected = true;
					}
					if (searchMode == SearchMode.Disassembler) {
						this.notebook1.Page = 0;
						int idx = DomMethodNodeBuilder.Disassemble ((DomCecilMethod)member, false).ToUpper ().IndexOf (searchEntry.Text.ToUpper ());
						this.disassemblerLabel.Selectable = true;
						this.disassemblerLabel.SelectRegion (idx, idx + searchEntry.Text.Length);
					}
					if (searchMode == SearchMode.Decompiler) {
						this.notebook1.Page = 1;
						int idx = DomMethodNodeBuilder.Decompile ((DomCecilMethod)member, false).ToUpper ().IndexOf (searchEntry.Text.ToUpper ());
						this.disassemblerLabel.Selectable = true;
						this.disassemblerLabel.SelectRegion (idx, idx + searchEntry.Text.Length);
					}
				}
			};
		}
		
		ITreeNavigator SearchMember (IMember member)
		{
			string fullName      = member.FullName;
			string declaringType = member.DeclaringType != null ? member.DeclaringType.FullName : null;
			return SearchMember (declaringType, fullName);
		}
			
		ITreeNavigator SearchMember (string typeName, string fullName)
		{
			return SearchMember (treeView.GetRootNode (), typeName, fullName);
		}
		
		ITreeNavigator SearchMember (ITreeNavigator nav, string typeName, string fullName)
		{
			IMember member = nav.DataItem as IMember;
			if (member != null) {
				string declaringType = member.DeclaringType != null ? member.DeclaringType.FullName : null;
				if (typeName == declaringType && fullName == member.FullName) 
					return nav;
			}
			if (nav.DataItem is Namespace) {
				if (!(typeName+fullName).StartsWith (((Namespace)nav.DataItem).Name)) {
					return null;
				}
			}
			if (nav.HasChildren ()) {
				nav.MoveToFirstChild ();
				do {
					ITreeNavigator result = SearchMember (nav.Clone (), typeName, fullName);
					if (result != null)
						return result;
				} while (nav.MoveNext());
			}
			return null;
		}
		
		enum SearchMode 
		{
			Type   = 0,
			Member = 1,
			Disassembler = 2,
			Decompiler = 3
		}
		SearchMode searchMode = SearchMode.Type;
		Gtk.ListStore memberListStore;
		Gtk.ListStore typeListStore;
		
		void CreateColumns ()
		{
			foreach (TreeViewColumn column in searchTreeview.Columns) {
				searchTreeview.RemoveColumn (column);
			}
			TreeViewColumn col;
			Gtk.CellRenderer crp, crt;
			switch (searchMode) {
			case SearchMode.Member:
			case SearchMode.Disassembler:
			case SearchMode.Decompiler:
				col = new TreeViewColumn ();
				col.Title = GettextCatalog.GetString ("Member");
				crp = new Gtk.CellRendererPixbuf ();
				crt = new Gtk.CellRendererText ();
				col.PackStart (crp, false);
				col.PackStart (crt, true);
				col.AddAttribute (crp, "pixbuf", 0);
				col.AddAttribute (crt, "text", 1);
				searchTreeview.AppendColumn (col);
				col.Resizable = true;
				col = searchTreeview.AppendColumn (GettextCatalog.GetString ("Declaring Type"), new Gtk.CellRendererText (), "text", 2);
				col.Resizable = true;
				col = searchTreeview.AppendColumn (GettextCatalog.GetString ("Assembly"), new Gtk.CellRendererText (), "text", 3);
				col.Resizable = true;
				searchTreeview.Model = memberListStore;
				break;
			case SearchMode.Type:
				col = new TreeViewColumn ();
				col.Title = GettextCatalog.GetString ("Type");
				crp = new Gtk.CellRendererPixbuf ();
				crt = new Gtk.CellRendererText ();
				col.PackStart (crp, false);
				col.PackStart (crt, true);
				col.AddAttribute (crp, "pixbuf", 0);
				col.AddAttribute (crt, "text", 1);
				searchTreeview.AppendColumn (col);
				col.Resizable = true;
				col = searchTreeview.AppendColumn (GettextCatalog.GetString ("Namespace"), new Gtk.CellRendererText (), "text", 2);
				col.Resizable = true;
				col = searchTreeview.AppendColumn (GettextCatalog.GetString ("Assembly"), new Gtk.CellRendererText (), "text", 3);
				col.Resizable = true;
				searchTreeview.Model = typeListStore;
				break;
			}
		}
		Thread searchThread = null;
		public void StartSearch ()
		{
			if (searchThread != null)
				searchThread.Abort ();
			switch (searchMode) {
			case SearchMode.Member:
				IdeApp.Workbench.StatusBar.BeginProgress (GettextCatalog.GetString ("Searching member..."));
				break;
			case SearchMode.Disassembler:
				IdeApp.Workbench.StatusBar.BeginProgress (GettextCatalog.GetString ("Searching string in disassembled code..."));
				break;
			case SearchMode.Decompiler:
				IdeApp.Workbench.StatusBar.BeginProgress (GettextCatalog.GetString ("Searching string in decompiled code..."));
				break;
			case SearchMode.Type:
				IdeApp.Workbench.StatusBar.BeginProgress (GettextCatalog.GetString ("Searching type..."));
				break;
			}
			memberListStore.Clear ();
			typeListStore.Clear ();
			
			searchThread = new Thread (StartSearchThread);
			searchThread.IsBackground = true;
			searchThread.Priority = ThreadPriority.Lowest;
			searchThread.Start ();
		}
	
		void StartSearchThread ()
		{
			try {
				string pattern = searchEntry.Text.ToUpper ();
				int types = 0, curType = 0;
				foreach (DomCecilCompilationUnit unit in this.definitions) {
					types += unit.TypeCount;
				}
				List<IMember> members = new List<IMember> ();
				switch (searchMode) {
				case SearchMode.Member:
					foreach (DomCecilCompilationUnit unit in this.definitions) {
						foreach (IType type in unit.Types) {
							curType++;
							members.Clear ();
							foreach (IMember member in type.Members) {
								if (member.Name.ToUpper ().Contains (pattern)) {
									members.Add (member);
								}
							}
							DispatchService.GuiSyncDispatch (delegate {
								MonoDevelop.Ide.Gui.IdeApp.Workbench.StatusBar.SetProgressFraction ((double)curType / types);
								foreach (MonoDevelop.Projects.Dom.IMember member in members) {
									memberListStore.AppendValues (MonoDevelop.Ide.Gui.IdeApp.Services.Resources.GetIcon (member.StockIcon, Gtk.IconSize.Menu),
									                              member.Name,
									                              type.FullName,
									                              unit.AssemblyDefinition.Name.FullName,
									                              member);
								}
							});
						}
					}
					break;
				case SearchMode.Disassembler:
					IdeApp.Workbench.StatusBar.BeginProgress (GettextCatalog.GetString ("Searching string in disassembled code..."));
					foreach (DomCecilCompilationUnit unit in this.definitions) {
						foreach (IType type in unit.Types) {
							curType++;
							members.Clear ();
							foreach (IMethod method in type.Methods) {
								DomCecilMethod domMethod = method as DomCecilMethod;
								if (domMethod == null)
									continue;
								if (DomMethodNodeBuilder.Disassemble (domMethod, false).ToUpper ().Contains (pattern)) {
									members.Add (method);
								}
							}
							DispatchService.GuiSyncDispatch (delegate {
								MonoDevelop.Ide.Gui.IdeApp.Workbench.StatusBar.SetProgressFraction ((double)curType / types);
								foreach (MonoDevelop.Projects.Dom.IMember member in members) {
									memberListStore.AppendValues (MonoDevelop.Ide.Gui.IdeApp.Services.Resources.GetIcon (member.StockIcon, Gtk.IconSize.Menu),
									                              member.Name,
									                              type.FullName,
									                              unit.AssemblyDefinition.Name.FullName,
									                              member);
								}
							});
						}
					}
					break;
				case SearchMode.Decompiler:
					foreach (DomCecilCompilationUnit unit in this.definitions) {
						foreach (IType type in unit.Types) {
							curType++;
							members.Clear ();
							foreach (IMethod method in type.Methods) {
								DomCecilMethod domMethod = method as DomCecilMethod;
								if (domMethod == null)
									continue;
								if (DomMethodNodeBuilder.Decompile (domMethod, false).ToUpper ().Contains (pattern)) {
									members.Add (method);
								}
							}
							DispatchService.GuiSyncDispatch (delegate {
								MonoDevelop.Ide.Gui.IdeApp.Workbench.StatusBar.SetProgressFraction ((double)curType / types);
								foreach (MonoDevelop.Projects.Dom.IMember member in members) {
									memberListStore.AppendValues (MonoDevelop.Ide.Gui.IdeApp.Services.Resources.GetIcon (member.StockIcon, Gtk.IconSize.Menu),
									                              member.Name,
									                              type.FullName,
									                              unit.AssemblyDefinition.Name.FullName,
									                              member);
								}
							});
						}
					}
					break;
				case SearchMode.Type:
					foreach (DomCecilCompilationUnit unit in this.definitions) {
						foreach (IType type in unit.Types) {
							curType++;
							DispatchService.GuiSyncDispatch (delegate {
								MonoDevelop.Ide.Gui.IdeApp.Workbench.StatusBar.SetProgressFraction ((double)curType / types);
								if (type.FullName.ToUpper ().IndexOf (pattern) >= 0) {
									typeListStore.AppendValues (MonoDevelop.Ide.Gui.IdeApp.Services.Resources.GetIcon (type.StockIcon, Gtk.IconSize.Menu),
									                            type.Name,
									                            type.Namespace,
									                            unit.AssemblyDefinition.Name.FullName,
									                            type);
								}
							});
						}
					}
					break;
				}
			} finally {
				DispatchService.GuiSyncDispatch (delegate {
					MonoDevelop.Ide.Gui.IdeApp.Workbench.StatusBar.EndProgress ();
				});
				searchThread = null;
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
		
					
		List<DomCecilCompilationUnit> definitions = new List<DomCecilCompilationUnit> ();
		public void AddReference (string fileName)
		{
			AssemblyDefinition assemblyDefinition = AssemblyFactory.GetAssembly (fileName);
			definitions.Add (new DomCecilCompilationUnit (assemblyDefinition));
			treeView.LoadTree (assemblyDefinition);
		}
		
		[CommandHandler (SearchCommands.Find)]
		public void ShowSearchWidget ()
		{
			//this.searchWidget.Visible = true;
			this.notebook1.Page = 2;
			this.searchEntry.GrabFocus ();
		}
	}
}
