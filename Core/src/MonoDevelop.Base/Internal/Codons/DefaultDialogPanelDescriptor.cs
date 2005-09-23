// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.Collections;
using System.CodeDom.Compiler;

using MonoDevelop.Core.AddIns;
using MonoDevelop.Core.AddIns.Codons;
using MonoDevelop.Core.AddIns.Conditions;
using MonoDevelop.Core.Properties;

namespace MonoDevelop.Core.AddIns.Codons
{
	public class DefaultDialogPanelDescriptor : IDialogPanelDescriptor
	{
		string       id    = String.Empty;
		string       label = String.Empty;
		ArrayList    dialogPanelDescriptors = null;
		IDialogPanel dialogPanel = null;
		
		public string ID {
			get {
				return id;
			}
		}
		
		public string Label {
			get {
				return label;
			}
			set {
				label = value;
			}
		}
		
		public ArrayList DialogPanelDescriptors {
			get {
				return dialogPanelDescriptors;
			}
			set {
				dialogPanelDescriptors = value;
			}
		}
		
		public IDialogPanel DialogPanel {
			get {
				return dialogPanel;
			}
			set {
				dialogPanel = value;
			}
		}
		
		public DefaultDialogPanelDescriptor(string id, string label)
		{
			this.id    = id;
			this.label = label;
		}
		
		public DefaultDialogPanelDescriptor(string id, string label, ArrayList dialogPanelDescriptors) : this(id, label)
		{
			this.dialogPanelDescriptors = dialogPanelDescriptors;
		}
		
		public DefaultDialogPanelDescriptor(string id, string label, IDialogPanel dialogPanel) : this(id, label)
		{
			this.dialogPanel = dialogPanel;
		}
	}
}
