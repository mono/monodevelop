//
// PadCodon.cs
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
using MonoDevelop.Core;
using Mono.Addins;
using MonoDevelop.Core.Gui;
using MonoDevelop.Ide.Gui;

namespace MonoDevelop.Ide.Codons
{
	[ExtensionNode ("Pad", "Registers a pad to be shown in the workbench.")]
	internal class PadCodon : ExtensionNode
	{
		IPadContent content;
		
		[Description ("Unused.")]
		[NodeAttribute("context")]
		string context = null;
		
		[Description ("Display name of the pad.")]
		[NodeAttribute("_label")]
		string label = null;
		
		[Description ("Class name.")]
		[NodeAttribute("class")]
		string className = null;
		
		[Description ("Icon of the pad. It can be a stock icon or a resource icon (use 'res:' as prefix in the last case).")]
		[NodeAttribute("icon")]
		string icon = null;

		string[] contexts;
		
		public IPadContent Pad {
			get {
				if (content == null)
					content = CreatePad ();
				return content; 
			}
		}
		
		public string Label {
			get { return GettextCatalog.GetString(label); }
		}
		
		public string Icon {
			get { return icon; }
		}
		
		public string ClassName {
			get { return className; }
		}
		
		public string[] Contexts {
			get { return contexts; }
		}
		
		protected virtual IPadContent CreatePad ()
		{
			if (context != null)
				contexts = context.Split (',');
			return (IPadContent) Addin.CreateInstance (className, true);
		}
	}
}
