// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.Collections;
using System.Reflection;
using MonoDevelop.Core.AddIns.Conditions;
using MonoDevelop.Core.Services;

using MonoDevelop.Services;

namespace MonoDevelop.Core.AddIns.Codons
{
	[CodonNameAttribute("DialogPanel")]
	public class DialogPanelCodon : AbstractCodon
	{
		[XmlMemberAttribute("_label", IsRequired=true)]
		string label       = null;
		
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
		public override object BuildItem(object owner, ArrayList subItems, ConditionCollection conditions)
		{
			IDialogPanelDescriptor newItem = null;
			
			if (subItems == null || subItems.Count == 0) {				
				if (Class != null) {
					newItem = new DefaultDialogPanelDescriptor(ID, GettextCatalog.GetString (Label), (IDialogPanel)AddIn.CreateObject(Class));
				} else {
					newItem = new DefaultDialogPanelDescriptor(ID, GettextCatalog.GetString (Label));
				}
			} else {
				newItem = new DefaultDialogPanelDescriptor(ID, GettextCatalog.GetString (Label), subItems);
			}
			return newItem;
		}
	}
}
