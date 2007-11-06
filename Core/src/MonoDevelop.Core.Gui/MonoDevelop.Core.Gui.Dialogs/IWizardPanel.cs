//  IWizardPanel.cs
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
using System.CodeDom.Compiler;
using Gtk;

using Mono.Addins;
using MonoDevelop.Core;
using MonoDevelop.Core.Gui.Codons;

namespace MonoDevelop.Core.Gui.Dialogs
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
