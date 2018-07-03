//
// WorkbenchContextCodon .cs
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
using System.Collections.Generic;
using System.ComponentModel;
using Mono.Addins;

namespace MonoDevelop.Ide.Codons
{
	[ExtensionNode (Description="A workbench context.")]
	internal class WorkbenchContextCodon : ExtensionNode
	{
		ContextPadCodon[] pads;
		
		public ContextPadCodon[] Pads {
			get {
				if (pads == null) {
					var bs = new List<ContextPadCodon> ();
					foreach (ExtensionNode ob in ChildNodes) {
						ContextPadCodon spad = ob as ContextPadCodon;
						if (spad != null)
							bs.Add (spad);
					}
					pads = bs.ToArray ();
				}
				return pads;
			}
		}
	}
}
