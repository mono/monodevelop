// OptionsPanelExtensionNode.cs
//
// Author:
//   Lluis Sanchez Gual <lluis@novell.com>
//
// Copyright (c) 2008 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//
//

using System;
using Mono.Addins;
using MonoDevelop.Ide.Gui.Dialogs;
using MonoDevelop.Core;

namespace MonoDevelop.Ide.Extensions
{
	[ExtensionNode ("Panel")]
	class OptionsPanelNode: TypeExtensionNode
	{
		[NodeAttribute ("class")]
		protected string typeName;

		[NodeAttribute ("_label", Localizable=true)]
		protected string label;
		
		[NodeAttribute]
		protected PanelGrouping grouping = PanelGrouping.Auto;
		
		[NodeAttribute]
		protected bool fill = false;
		
		[NodeAttribute]
		protected string replaces;

		Type panelType;
		string headerLabel;
		
		public OptionsPanelNode ()
		{
		}
		
		public OptionsPanelNode (Type panelType)
		{
			this.panelType = panelType;
		}
		
		public string Label {
			get {
				return BrandingService.BrandApplicationName (label);
			}
			set {
				label = value;
			}
		}

		public string HeaderLabel {
			get {
				return BrandingService.BrandApplicationName (headerLabel ?? label);
			}
			set {
				headerLabel = value;
			}
		}

		public PanelGrouping Grouping {
			get {
				return grouping;
			}
			set {
				grouping = value;
			}
		}

		public bool Fill {
			get {
				return fill;
			}
			set {
				fill = value;
			}
		}

		public new string TypeName {
			get {
				return typeName;
			}
		}
		
		internal bool CustomNode {
			get { return panelType != null; }
		}

		public string Replaces {
			get {
				return this.replaces;
			}
		}
		
		public virtual IOptionsPanel CreatePanel ()
		{
			if (panelType != null)
				return (IOptionsPanel) Activator.CreateInstance (panelType);
			IOptionsPanel p = Addin.CreateInstance (typeName, true) as IOptionsPanel;
			if (p == null)
				throw new System.InvalidOperationException ("Type '" + typeName + "' does not implement IOptionsPanel");
			return p;
		}
	}
	
	public enum PanelGrouping
	{
		Auto,
		Tab,
		Box
	}
}
