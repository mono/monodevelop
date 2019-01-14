//
// ItemSetCodon.cs
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
using Mono.Addins;
using MonoDevelop.Core;

namespace MonoDevelop.Components.Commands.ExtensionNodes
{
	[ExtensionNode (Description="A submenu")]
	class ItemSetCodon : InstanceExtensionNode
	{
		//these fields are assigned by reflection, suppress "never assigned" warning
		#pragma warning disable 649

		[NodeAttribute ("_label", "Label of the submenu", Localizable=true)]
		string label;

		[NodeAttribute("icon", "Icon of the submenu. The provided value must be a registered stock icon. A resource icon can also be specified using 'res:' as prefix for the name, for example: 'res:customIcon.png'")]
		string icon;
		
		[NodeAttribute("autohide", "Whether the submenu should be hidden when it contains no items.")]
		bool autohide;

		#pragma warning restore 649

		public string Label {
			get => label ?? Id;
			private set => label = value;
		}
		
		public override object CreateInstance ()
		{
			if (label == null) label = Id;

			label = StringParserService.Parse (label);
			if (icon != null) icon = CommandCodon.GetStockId (Addin, icon);
			var cset = new CommandEntrySet (label, icon) {
				CommandId = Id,
				AutoHide = autohide
			};
			foreach (InstanceExtensionNode e in ChildNodes) {
				var ce = e.CreateInstance () as CommandEntry;
				if (ce != null)
					cset.Add (ce);
				else
					throw new InvalidOperationException ("Invalid ItemSet child: " + e);
			}
			return cset;
		}
	}
}
