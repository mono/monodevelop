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
	public class PadCodon : ExtensionNode
	{
		IPadContent content;
		string id;
		
		[NodeAttribute("_label", "Display name of the pad.", Localizable=true)]
		string label = null;
		
		[NodeAttribute("class", "Class name.")]
		string className = null;
		
		[NodeAttribute("icon", "Icon of the pad. It can be a stock icon or a resource icon (use 'res:' as prefix in the last case).")]
		string icon = null;
		
		[NodeAttribute("defaultPlacement",
		               "Default placement of the pad inside the workbench. " +
		               "It can be: left, right, top, bottom, or a relative position, for example: 'ProjectPad/left'" +
		               "would show the pad at the left side of the project pad. When using " +
		               "relative placements several positions can be provided. If the " +
		               "pad can be placed in the first position, the next one will be " +
		               "tried. For example 'ProjectPad/left; bottom'."
		               )]
		string defaultPlacement = "left";
		
		string[] contexts;
		
		public IPadContent PadContent {
			get {
				if (content == null)
					content = CreatePad ();
				return content; 
			}
		}
		
		public string PadId {
			get { return id != null ? id : base.Id; }
			set { id = value; }
		}
		
		public string Label {
			get { return label; }
		}
		
		public string Icon {
			get { return icon; }
		}
		
		public string ClassName {
			get { return className; }
		}
		
		/// <summary>
		/// Returns the default placement of the pad: left, right, top, bottom.
		/// Relative positions can be used, for example: "ProjectPad/left"
		/// would show the pad at the left of the project pad. When using
		/// relative placements several positions can be provided. If the
		/// pad can be placed in the first position, the next one will be
		/// tried. For example "ProjectPad/left; bottom".
		/// </summary>
		public string DefaultPlacement {
			get { return defaultPlacement; }
		}
		
		public string[] Contexts {
			get { return contexts; }
		}
		
		public bool Initialized {
			get {
				return content != null;
			}
		}
		
		public PadCodon ()
		{
		}
		
		public PadCodon (IPadContent content, string id, string label, string defaultPlacement, string icon)
		{
			this.id               = id;
			this.content          = content;
			this.label            = label;
			this.defaultPlacement = defaultPlacement;
			this.icon             = icon;
		}
		
		protected virtual IPadContent CreatePad ()
		{
			Counters.PadsLoaded++;
			return (IPadContent) Addin.CreateInstance (className, true);
		}
		
	}
}
