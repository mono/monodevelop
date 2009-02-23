//
// TreeViewPad.cs
//
// Author:
//   Lluis Sanchez Gual
//
// Copyright (C) 2005 Novell, Inc (http://www.novell.com)
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
using System.IO;
using System.ComponentModel;
using System.Drawing;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Xml;
using System.Resources;
using System.Text;

using Mono.Addins;
using MonoDevelop.Core;
using MonoDevelop.Projects;
using MonoDevelop.Core.Gui.Dialogs;
using MonoDevelop.Components;
using MonoDevelop.Ide.Commands;
using MonoDevelop.Core.Gui;
using MonoDevelop.Components.Commands;
using MonoDevelop.Ide.Gui.Components;

namespace MonoDevelop.Ide.Gui.Pads
{
	/// <summary>
	/// This class implements a project browser.
	/// </summary>
	public class TreeViewPad : AbstractPadContent, IMementoCapable, ICommandDelegatorRouter
	{
		protected ExtensibleTreeView treeView = new ExtensibleTreeView ();
		
		public ExtensibleTreeView TreeView {
			get {
				return treeView;
			}
		}
		
		public override Gtk.Widget Control {
			get {
				return treeView;
			}
		}
		
		public TreeViewPad ()
		{
			treeView.Tree.CursorChanged += new EventHandler (OnSelectionChanged);
		}
		
		protected virtual void OnSelectionChanged (object sender, EventArgs args)
		{
			// nothing
		}
		
		public TreeViewPad (NodeBuilder[] builders, TreePadOption[] options)
		{
			Initialize (builders, options);
		}
		
		public void Initialize (NodeBuilder[] builders, TreePadOption[] options)
		{
			Initialize (builders, options, null);
		}
		
		public virtual void Initialize (NodeBuilder[] builders, TreePadOption[] options, string contextMenuPath)
		{
			treeView.Initialize (builders, options, contextMenuPath);
		}
		
		#region ICommandDelegatorRouter
		object ICommandDelegatorRouter.GetNextCommandTarget ()
		{
			return treeView.GetNextCommandTarget ();
		}
		
		object ICommandDelegatorRouter.GetDelegatedCommandTarget ()
		{
			return treeView.GetDelegatedCommandTarget ();
		}
		#endregion
		
		#region IMementoCapable
		ICustomXmlSerializer IMementoCapable.CreateMemento ()
		{
			return treeView.SaveTreeState ();
		}

		void IMementoCapable.SetMemento (ICustomXmlSerializer memento)
		{
			treeView.RestoreTreeState ((NodeState)memento);
		}
		#endregion
	}
	
	public delegate void TreeNodeCallback (ITreeNavigator nav);
}
