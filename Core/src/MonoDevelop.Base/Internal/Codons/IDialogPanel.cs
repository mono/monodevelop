// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.Collections;
using System.CodeDom.Compiler;
using Gtk;

using MonoDevelop.Core.AddIns;
using MonoDevelop.Core.AddIns.Codons;
using MonoDevelop.Core.Properties;

namespace MonoDevelop.Core.AddIns.Codons
{
	public enum DialogMessage {
		OK,
		Cancel,
		Help,
		Next,
		Prev,
		Finish,
		Activated
	}
	
	public interface IDialogPanel
	{
		/// <summary>
		/// Some panels do get an object which they can customize, like
		/// Wizard Dialogs. Check the dialog description for more details
		/// about this.
		/// </summary>
		object CustomizationObject {
			get;
			set;
		}
		
		Widget Control {
			get;
		}
		
		bool EnableFinish {
			get;
		}

		Gtk.Image Icon {
			get;
		}
		
		/// <returns>
		/// true, if the DialogMessage could be executed.
		/// </returns>
		bool ReceiveDialogMessage(DialogMessage message);
		
		event EventHandler EnableFinishChanged;
	}
}
