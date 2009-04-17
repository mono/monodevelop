//
// DialogPanelCodon.cs
//
// Author:
//   Mike Krüger <mkrueger@novell.com>
//
// Copyright (C) 2009 Novell, Inc (http://www.novell.com)
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
using System.Linq;
using System.Collections.Generic;
using Mono.Addins;
using MonoDevelop.Core.Gui.Dialogs;

namespace MonoDevelop.Core.Gui.Codons
{
	[ExtensionNode (Description="A dialog panel to be shown in an options dialog. The specified class must implement MonoDevelop.Core.Gui.Dialogs.IDialogPanel.")]
	class DialogPanelCodon : TypeExtensionNode
	{
		[NodeAttribute("_label", true, "Name of the panel", Localizable=true)]
		string label = null;
		
		string className;
		
		public string Label {
			get {
				return label;
			}
			set {
				label = value;
			}
		}
		
		IEnumerable<IDialogPanelDescriptor> ChildDescriptors {
			get {
				foreach (TypeExtensionNode node in ChildNodes) {
					yield return (IDialogPanelDescriptor)node.CreateInstance ();
				}
			}
		}
		
		public override object CreateInstance ()
		{
			if (ChildNodes.Count == 0) 
				return string.IsNullOrEmpty (className) ? new DefaultDialogPanelDescriptor (Id, Label) : new DefaultDialogPanelDescriptor (Id, Label, (IDialogPanel)CreateInstance ());
			return new DefaultDialogPanelDescriptor (Id, Label, new List<IDialogPanelDescriptor> (ChildDescriptors));
		}
		
		protected override void Read (NodeElement el)
		{
			base.Read (el);
			className = el.GetAttribute ("class");
		}
	}
}
