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
	/// <summary>
	/// This interface extends the IDialogPanel interface with wizard specific
	/// funcitons.
	/// </summary>
	public interface IWizardPanel : IDialogPanel
	{
		/// <remarks>
		/// This is used for wizards which has more than one path, this
		/// may be null for a standard wizard.
		/// </remarks>
		/// <value>The ID of the panel that follows this panel</value>
		string NextWizardPanelID {
			get;
		}
		
		/// <value>
		/// true, if this panel has no successor and is the last panel in it's path. 
		/// This is only used for wizard that have no linear endings.
		/// </value>
		bool IsLastPanel {
			get;
		}
		
		/// <value>
		/// If true, the user can access the next panel. 
		/// </value>
		bool EnableNext {
			get;
		}
		
		/// <value>
		/// If true, the user can access the previous panel. 
		/// </value>
		bool EnablePrevious {
			get;
		}
		
		/// <value>
		/// If true, the user can cancel the wizard
		/// </value>
		bool EnableCancel {
			get;
		}		
		
		/// <remarks>
		/// Is fired when the EnableNext property has changed.
		/// </remarks>
		event EventHandler EnableNextChanged;
		
		/// <remarks>
		/// Is fired when the NextWizardPanelID property has changed.
		/// </remarks>
		event EventHandler NextWizardPanelIDChanged;
		
		/// <remarks>
		/// Is fired when the IsLastPanel property has changed.
		/// </remarks>
		event EventHandler IsLastPanelChanged;
		
		/// <remarks>
		/// Is fired when the EnablePrevious property has changed.
		/// </remarks>
		event EventHandler EnablePreviousChanged;
		
		/// <remarks>
		/// Is fired when the EnableCancel property has changed.
		/// </remarks>
		event EventHandler EnableCancelChanged;
		
		/// <remarks>
		/// Is fired when the panel wants that the wizard goes over
		/// to the next panel. This event overrides the EnableNext
		/// property. (You can move over to the next with EnableNext
		/// == false)
		/// </remarks>
		event EventHandler FinishPanelRequested;
	}
}
