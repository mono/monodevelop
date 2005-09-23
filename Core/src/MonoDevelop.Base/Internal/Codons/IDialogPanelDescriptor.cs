// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.Collections;

namespace MonoDevelop.Core.AddIns.Codons
{
	public interface IDialogPanelDescriptor
	{
		/// <value>
		/// Returns the ID of the dialog panel codon
		/// </value>
		string ID {
			get;
		}
		
		/// <value>
		/// Returns the label of the dialog panel
		/// </value>
		string Label {
			get;
			set;
		}
		
		ArrayList DialogPanelDescriptors {
			get;
			set;
		}
		
		/// <value>
		/// Returns the dialog panel object
		/// </value>
		IDialogPanel DialogPanel {
			get;
			set;
		}
	}
}
