// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

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
	[Description ("A dialog panel to be shown in an options dialog. The specified class must implement MonoDevelop.Core.Gui.Dialogs.IDialogPanel.")]
	public class DialogPanelCodon : TypeExtensionNode
	{
		[Description ("A dialog panel to be shown in an options dialog.")]
		[NodeAttribute("_label", true)]
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
					newItem = new DefaultDialogPanelDescriptor (Id, GettextCatalog.GetString (Label), (IDialogPanel)base.CreateInstance ());
				} else {
					newItem = new DefaultDialogPanelDescriptor (Id, GettextCatalog.GetString (Label));
				}
			} else {
				ArrayList subItems = new ArrayList ();
				foreach (TypeExtensionNode node in ChildNodes)
					subItems.Add (node.CreateInstance ());
				newItem = new DefaultDialogPanelDescriptor (Id, GettextCatalog.GetString (Label), subItems);
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
