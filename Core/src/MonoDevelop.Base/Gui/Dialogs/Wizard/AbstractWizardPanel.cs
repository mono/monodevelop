// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using Gtk;

using MonoDevelop.Core.AddIns.Codons;

namespace MonoDevelop.Gui.Dialogs
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
