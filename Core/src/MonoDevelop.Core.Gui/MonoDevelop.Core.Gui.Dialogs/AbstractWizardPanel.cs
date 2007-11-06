//  AbstractWizardPanel.cs
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
using Gtk;

namespace MonoDevelop.Core.Gui.Dialogs
{
	public abstract class AbstractWizardPanel : AbstractOptionPanel, IWizardPanel
	{
		string nextWizardPanelID = String.Empty;
		bool   enablePrevious    = true;
		bool   enableNext        = true;
		bool   isLastPanel       = false;
		bool   enableCancel      = true;
		
		public string NextWizardPanelID {
			get {
				return nextWizardPanelID;
			}
			set {
				if (nextWizardPanelID != value) {
					nextWizardPanelID = value;
					OnNextWizardPanelIDChanged(EventArgs.Empty);
				}
			}
		}
		
		public bool IsLastPanel {
			get {
				return isLastPanel;
			}
			set {
				if (isLastPanel != value) {
					isLastPanel = value;
					OnIsLastPanelChanged(EventArgs.Empty);
				}
			}
		}
		
		public bool EnableNext {
			get {
				return enableNext;
			}
			set {
				if (enableNext != value) {
					enableNext = value;
					OnEnableNextChanged(EventArgs.Empty);
				}
			}
		}
		
		public bool EnablePrevious {
			get {
				return enablePrevious;
			}
			set {
				if (enablePrevious != value) {
					enablePrevious = value;
					OnEnablePreviousChanged(EventArgs.Empty);
				}
			}
		}
		
		public bool EnableCancel {
			get {
				return enableCancel;
			}
			set {
				if (enableCancel != value) {
					enableCancel = value;
					OnEnableCancelChanged(EventArgs.Empty);
				}
			}
		}
		
		public AbstractWizardPanel() : base()
		{
		}
		
		protected virtual void FinishPanel()
		{
			if (FinishPanelRequested != null) {
				FinishPanelRequested(this, EventArgs.Empty);
			}
		}
		
		protected virtual void OnEnableNextChanged(EventArgs e)
		{
			if (EnableNextChanged != null) {
				EnableNextChanged(this, e);
			}
		}

		protected virtual void OnEnablePreviousChanged(EventArgs e)
		{
			if (EnablePreviousChanged != null) {
				EnablePreviousChanged(this, e);
			}
		}
		
		protected virtual void OnEnableCancelChanged(EventArgs e)
		{
			if (EnableCancelChanged != null) {
				EnableCancelChanged(this, e);
			}
		}
		

		protected virtual void OnNextWizardPanelIDChanged(EventArgs e)
		{
			if (NextWizardPanelIDChanged != null) {
				NextWizardPanelIDChanged(this, e);
			}
		}
		
		protected virtual void OnIsLastPanelChanged(EventArgs e)
		{
			if (IsLastPanelChanged != null) {
				IsLastPanelChanged(this, e);
			}
		}
		
		public event EventHandler EnablePreviousChanged;
		public event EventHandler EnableNextChanged;
		public event EventHandler EnableCancelChanged;
		
		public event EventHandler NextWizardPanelIDChanged;
		public event EventHandler IsLastPanelChanged;
		
		public event EventHandler FinishPanelRequested;

		public override bool ReceiveDialogMessage (DialogMessage a)
		{
			//Runtime.LoggingService.Info ("In middle receive dialog message");
			return true;
		}
	}
}
