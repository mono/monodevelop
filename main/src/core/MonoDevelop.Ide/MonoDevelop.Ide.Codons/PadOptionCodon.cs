//
// PadOptionCodon.cs
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
using System.ComponentModel;
using MonoDevelop.Ide.Gui.Pads;
using MonoDevelop.Core;
using Mono.Addins;
using MonoDevelop.Ide.Gui.Components;

namespace MonoDevelop.Ide.Codons
{
	[ExtensionNode (Description="A display option of a solution pad.")]
	internal class PadOptionCodon : ExtensionNode
	{
		[NodeAttribute("_label", true, "Display name of the option", Localizable=true)]
		string label = null;
		
		[NodeAttribute("defaultValue", "Default value of the option")]
		bool defaultValue;
		
		TreePadOption option;
		
		public TreePadOption Option {
			get { return option; }
		}
		
		protected override void Read (NodeElement elem)
		{
			base.Read (elem);
			option = new TreePadOption (Id, label, defaultValue);
		}
	}
}
