//  DialogPanelCodon.cs
//
//  This file was derived from a file from #Develop. 
//
//  Copyright (C) 2001-2007 Mike Krüger <mkrueger@novell.com>
// 
//  This program is free software; you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation; either version 2 of the License, or
//  (at your option) any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
//  GNU General Public License for more details.
//  
//  You should have received a copy of the GNU General Public License
//  along with this program; if not, write to the Free Software
//  Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA

using System;
using System.Collections;
using System.Reflection;
using System.Xml;
using System.ComponentModel;
using MonoDevelop.Core;
using Mono.Addins;
using MonoDevelop.Core.Gui.Dialogs;

namespace MonoDevelop.Core.Gui.Codons
{
	[ExtensionNode (Description="A dialog panel to be shown in an options dialog. The specified class must implement MonoDevelop.Core.Gui.Dialogs.IDialogPanel.")]
	internal class DialogPanelCodon : TypeExtensionNode
	{
		[NodeAttribute("_label", true, "Name of the panel", Localizable=true)]
		string label = null;
		
		string clsName;
		
		public string Label {
			get {
				return label;
			}
			set {
				label = value;
			}
		}
		
		/// <summary>
		/// Creates an item with the specified sub items. And the current
		/// Condition status for this item.
		/// </summary>
		public override object CreateInstance ()
		{
			IDialogPanelDescriptor newItem = null;
			
			if (ChildNodes.Count == 0) {
				if (clsName.Length > 0) {
					newItem = new DefaultDialogPanelDescriptor (Id, Label, (IDialogPanel)base.CreateInstance ());
				} else {
					newItem = new DefaultDialogPanelDescriptor (Id, Label);
				}
			} else {
				ArrayList subItems = new ArrayList ();
				foreach (TypeExtensionNode node in ChildNodes)
					subItems.Add (node.CreateInstance ());
				newItem = new DefaultDialogPanelDescriptor (Id, Label, subItems);
			}
			return newItem;
		}
		
		protected override void Read (NodeElement elem)
		{
			base.Read (elem);
			clsName = elem.GetAttribute ("class");
		}
	}
}
