//
// SolutionPadCodon.cs
//
// Author:
//   Lluis Sanchez Gual
//

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
using System.Collections;
using System.Collections.Generic;
using MonoDevelop.Core;
using Mono.Addins;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Gui.Pads;
using System.ComponentModel;
using MonoDevelop.Ide.Gui.Components;

namespace MonoDevelop.Ide.Codons
{
	[ExtensionNode ("SolutionPad", "Registers a pad which shows information about a project in a tree view.")]
	internal class SolutionPadCodon : PadCodon
	{
		NodeBuilder[] builders;
		TreePadOption[] options;
		TreeViewPad pad;
		string contextMenuPath;
		/*
		string placement = null;*/

//		public string DefaultPlacement {
//			get { return placement; }
//		}
		
		void BuildChildren ()
		{
			List<NodeBuilder> bs = new List<NodeBuilder> ();
			List<TreePadOption> ops = new List<TreePadOption> ();
			
			foreach (ExtensionNode ob in ChildNodes) {
				NodeBuilderCodon nbc = ob as NodeBuilderCodon;
				if (nbc != null)
					bs.Add (nbc.NodeBuilder);
				else if (ob is PadOptionCodon) {
					PadOptionCodon poc = (PadOptionCodon) ob;
					ops.Add (poc.Option);
				} else if (ob is PadContextMenuExtensionNode)
					contextMenuPath = ((PadContextMenuExtensionNode) ob).MenuPath;
			}
			builders = bs.ToArray ();
			options = ops.ToArray ();
		}
		
		protected override PadContent CreatePad ()
		{
			if (builders == null)
				BuildChildren ();
			
			AddinManager.ExtensionChanged += OnExtensionChanged;
			
			if (ClassName != null && ClassName.Length > 0) {
				object ob = Addin.CreateInstance (ClassName, true);
				if (!(ob is TreeViewPad))
					throw new InvalidOperationException ("'" + ClassName + "' is not a subclass of TreeViewPad.");
				pad = (TreeViewPad) ob;
			} else
				pad = new SolutionPad ();

			pad.Initializer = delegate {
				pad.Initialize (builders, options, contextMenuPath);
			};
			pad.Id = Id;
			return pad;
		}
		
		void OnExtensionChanged (object s, ExtensionEventArgs args)
		{
			string codonpath = Path;
			if (args.PathChanged (codonpath)) {
				BuildChildren ();
				pad.TreeView.UpdateBuilders (builders, options);
			}
		}
	}
}
