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

using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Gui.Pads;

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
				new ModuleDefinitionNodeBuilder (),
				new ReferenceFolderNodeBuilder (this),
				new ModuleReferenceNodeBuilder (),
				new ResourceFolderNodeBuilder (),
				new ResourceNodeBuilder (),
				new NamespaceBuilder (),
				new DomTypeNodeBuilder (),
				new DomMethodNodeBuilder (),
				new DomFieldNodeBuilder (),
				new DomEventNodeBuilder (),
				new DomPropertyNodeBuilder (),
				new BaseTypeFolderNodeBuilder (),
				new DomReturnTypeNodeBuilder ()
				}, new TreePadOption [] {});
			scrolledwindow2.Add (treeView);
			scrolledwindow2.ShowAll ();
            
			disassemblerTextview.ModifyFont (Pango.FontDescription.FromString ("Monospace 10"));
            disassemblerTextview.ModifyBase (Gtk.StateType.Normal, new Gdk.Color (255, 255, 220));
			
			treeView.Tree.CursorChanged += delegate {
				ITreeNavigator nav = treeView.GetSelectedNode ();
				if (nav != null) {
					System.Console.WriteLine(nav.TypeNodeBuilder);
					IAssemblyBrowserNodeBuilder builder = nav.TypeNodeBuilder as IAssemblyBrowserNodeBuilder;
					
					if (builder != null) {
						this.label2.Markup                    = builder.GetDescription (nav.DataItem);
						this.disassemblerTextview.Buffer.Text = builder.GetDisassembly (nav.DataItem);
					} else {
						this.label2.Markup =  this.disassemblerTextview.Buffer.Text = "";
					}
				}
			};
		}

		public void AddReference (string fileName)
		{
			AssemblyDefinition assemblyDefinition = AssemblyFactory.GetAssembly (fileName);
			treeView.LoadTree (assemblyDefinition);
		}
	}
}
