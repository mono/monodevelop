// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.Drawing;
using System.Diagnostics;
using System.Collections;
using System.Xml;

using MonoDevelop.Services;
using MonoDevelop.Core.Services;
using MonoDevelop.Core.AddIns;
using MonoDevelop.Core.Properties;
using MonoDevelop.Core.AddIns.Codons;
using Gtk;

namespace MonoDevelop.Gui.Dialogs
{
	/// <summary>
	/// TreeView options are used, when more options will be edited (for something like
	/// IDE Options + Plugin Options)
	/// </summary>
	public class WizardDialog : Dialog
	{
		StatusPanel       statusPanel  = null;
		
		Gtk.Frame             dialogPanel  = new Gtk.Frame();

		/// <remarks>
		/// On this stack the indices of the previous active wizard panels. This
		/// is used to restore the path the user gone. (for the back button)
		/// </remarks>
		Stack             idStack      = new Stack();
		
		ArrayList         wizardPanels = new ArrayList();
		int               activePanelNumber  = 0;
		
		EventHandler enableNextChangedHandler;
		EventHandler enableCancelChangedHandler;
		EventHandler nextWizardPanelIDChangedHandler;
		EventHandler finishPanelHandler;
		
		public ArrayList WizardPanels {
			get {
				return wizardPanels;
			}
		}
		
		public int ActivePanelNumber {
			get {
				return activePanelNumber;
			}
		}
		
		public IWizardPanel CurrentWizardPane {
			get {
				return (IWizardPanel)((IDialogPanelDescriptor)wizardPanels[activePanelNumber]).DialogPanel;
			}
		}
		
		int GetPanelNumber(string id)
		{
			for (int i = 0; i < wizardPanels.Count; ++i) {
				IDialogPanelDescriptor descriptor = (IDialogPanelDescriptor)wizardPanels[i];
				if (descriptor.ID == id) {
					return i;
				}
			}
			return -1;
		}
		
		public int GetSuccessorNumber(int curNr)
		{
			IWizardPanel panel = (IWizardPanel)((IDialogPanelDescriptor)wizardPanels[curNr]).DialogPanel;
			
			if (panel.IsLastPanel) {
				return wizardPanels.Count + 1;
			}
			
			int nextID = GetPanelNumber(panel.NextWizardPanelID);
			if (nextID < 0) {
				return curNr + 1;
			}
			return nextID;
		}
		
		/// <value> returns true, if all dialog panels could be finished</value>
		bool CanFinish {
			get {
				int currentNr = 0;
				while (currentNr < wizardPanels.Count) {
					IDialogPanelDescriptor descriptor = (IDialogPanelDescriptor)wizardPanels[currentNr];
					if (!descriptor.DialogPanel.EnableFinish) {
						return false;
					}
					currentNr = GetSuccessorNumber(currentNr);
				}
				return true;
			}
		}
		
		Gtk.Button backButton;
		Gtk.Button nextButton;
		Gtk.Button finishButton;
		Gtk.Button cancelButton;
//		Gtk.Button helpButton;
		
		void CheckFinishedState(object sender, EventArgs e)
		{
			finishButton.Sensitive = CanFinish;
		}
		
		void AddNodes(object customizer, ArrayList dialogPanelDescriptors)
		{
			foreach (IDialogPanelDescriptor descriptor in dialogPanelDescriptors) {
				
				if (descriptor.DialogPanel != null) { // may be null, if it is only a "path"
					descriptor.DialogPanel.EnableFinishChanged += new EventHandler(CheckFinishedState);
					descriptor.DialogPanel.CustomizationObject    = customizer;
					wizardPanels.Add(descriptor);
				}
				
				if (descriptor.DialogPanelDescriptors != null) {
					AddNodes(customizer, descriptor.DialogPanelDescriptors);
				}
			}
		}
		
		void EnableCancelChanged(object sender, EventArgs e)
		{
			cancelButton.Sensitive = CurrentWizardPane.EnableCancel;
		}
		
		void EnableNextChanged(object sender, EventArgs e)
		{
			nextButton.Sensitive = CurrentWizardPane.EnableNext && GetSuccessorNumber(activePanelNumber) < wizardPanels.Count;
			backButton.Sensitive = CurrentWizardPane.EnablePrevious && idStack.Count > 0;
		}
		
		void NextWizardPanelIDChanged(object sender, EventArgs e)
		{
			EnableNextChanged(null, null);
			finishButton.Sensitive = CanFinish;
			statusPanel.QueueDraw ();
		}
		
		void ActivatePanel(int number)
		{
			// take out old event handlers
			if (CurrentWizardPane != null) {
				CurrentWizardPane.EnableNextChanged        -= enableNextChangedHandler;
				CurrentWizardPane.EnableCancelChanged      -= enableCancelChangedHandler;
				CurrentWizardPane.EnablePreviousChanged    -= enableNextChangedHandler;
				CurrentWizardPane.NextWizardPanelIDChanged -= nextWizardPanelIDChangedHandler;
				CurrentWizardPane.IsLastPanelChanged       -= nextWizardPanelIDChangedHandler;
				CurrentWizardPane.FinishPanelRequested     -= finishPanelHandler;
				
			}
			
			// set new active panel
			activePanelNumber = number;
			
			// insert new event handlers
			if (CurrentWizardPane != null) {
				CurrentWizardPane.EnableNextChanged        += enableNextChangedHandler;
				CurrentWizardPane.EnableCancelChanged      += enableCancelChangedHandler;
				CurrentWizardPane.EnablePreviousChanged    += enableNextChangedHandler;
				CurrentWizardPane.NextWizardPanelIDChanged += nextWizardPanelIDChangedHandler;
				CurrentWizardPane.IsLastPanelChanged       += nextWizardPanelIDChangedHandler;
				CurrentWizardPane.FinishPanelRequested     += finishPanelHandler;
			}
			
			// initialize panel status
			EnableNextChanged(null, null);
			NextWizardPanelIDChanged(null, null);
			EnableCancelChanged(null, null);
			
			// take out panel control & show new one
			if (dialogPanel.Child != null) {
				statusPanel.GdkWindow.InvalidateRect (new Gdk.Rectangle (0, 0, 400, 400), true);
				dialogPanel.Remove (dialogPanel.Child);
			}
			if (CurrentWizardPane.ToString () == "MonoDevelop.Gui.Dialogs.OptionPanels.CompletionDatabaseWizard.CreationFinishedPanel") {
				Runtime.LoggingService.Info ("This is an ugly hack for an even uglier bug, Restart MD now");
				System.Environment.Exit (0);
			}
			dialogPanel.Add(CurrentWizardPane.Control);

			this.ShowAll ();
			
		}
		
		public WizardDialog (string title, object customizer, string treePath)
		{
			IAddInTreeNode node = AddInTreeSingleton.AddInTree.GetTreeNode(treePath);
			this.Title = title;
			this.BorderWidth = 6;
			this.HasSeparator = false;
			
			if (node != null) {
				AddNodes(customizer, node.BuildChildItems(this));
			}
			InitializeComponents();
			
			enableNextChangedHandler        = new EventHandler(EnableNextChanged);
			nextWizardPanelIDChangedHandler = new EventHandler(NextWizardPanelIDChanged);
			enableCancelChangedHandler      = new EventHandler(EnableCancelChanged);
			finishPanelHandler              = new EventHandler(FinishPanelEvent);
			ActivatePanel(0);
		}
		
		void FinishPanelEvent(object sender, EventArgs e)
		{
			AbstractWizardPanel panel = (AbstractWizardPanel)CurrentWizardPane;
			bool isLast = panel.IsLastPanel;
			panel.IsLastPanel = false;
			ShowNextPanelEvent(sender, e);
			panel.IsLastPanel = isLast;
		}
		
		void ShowNextPanelEvent(object sender, EventArgs e)
		{
			int nextID = GetSuccessorNumber(this.ActivePanelNumber);
			Debug.Assert(nextID < wizardPanels.Count && nextID >= 0);

			if (!CurrentWizardPane.ReceiveDialogMessage(DialogMessage.Next)) {
				return;
			}
			CurrentWizardPane.ReceiveDialogMessage (DialogMessage.Next);
			idStack.Push(activePanelNumber);
			ActivatePanel(nextID);
			CurrentWizardPane.ReceiveDialogMessage(DialogMessage.Activated);
		}
		
		void ShowPrevPanelEvent(object sender, EventArgs e)
		{
			Debug.Assert(idStack.Count > 0);
			if (!CurrentWizardPane.ReceiveDialogMessage(DialogMessage.Prev)) {
				return;
			}
			ActivatePanel((int)idStack.Pop());
		}
		
		void FinishEvent(object sender, EventArgs e)
		{
			foreach (IDialogPanelDescriptor descriptor in wizardPanels) {
				if (!descriptor.DialogPanel.ReceiveDialogMessage(DialogMessage.Finish)) {
					return;
				}
			}
			this.Respond ((int) ResponseType.Close);
		}
		
		void CancelEvent(object sender, EventArgs e)
		{
			foreach (IDialogPanelDescriptor descriptor in wizardPanels) {
				if (!descriptor.DialogPanel.ReceiveDialogMessage(DialogMessage.Cancel)) {
					return;
				}
			}
			this.Respond ((int) ResponseType.Cancel);
		}
		
		protected void HelpEvent(object sender, EventArgs e)
		{
			CurrentWizardPane.ReceiveDialogMessage(DialogMessage.Help);
		}

		
		void InitializeComponents()
		{
			dialogPanel.Shadow = Gtk.ShadowType.None;
		
			//this.GdkWindow.SkipPagerHint = true;
			//this.GdkWindow.SkipTaskbarHint = true;
			this.WindowPosition = WindowPosition.Center;
			this.SetDefaultSize (640, 440);
		
			backButton = new ImageButton(Gtk.Stock.GoBack, GettextCatalog.GetString("Back"));
			backButton.Clicked   += new EventHandler(ShowPrevPanelEvent);
			
			nextButton = new ImageButton(Gtk.Stock.GoForward, GettextCatalog.GetString ("Next"));
			nextButton.Clicked   += new EventHandler(ShowNextPanelEvent);
			
			finishButton = new ImageButton(Gtk.Stock.Apply, GettextCatalog.GetString ("Finish"));
			finishButton.Clicked   += new EventHandler(FinishEvent);

			cancelButton = new Button (Gtk.Stock.Cancel);
			cancelButton.Clicked += new EventHandler(CancelEvent);
			
			// don't emit response for back and next
			this.ActionArea.PackStart (backButton);
			this.ActionArea.PackStart (nextButton);
			this.AddActionWidget (finishButton, (int) ResponseType.Close);
			this.AddActionWidget (cancelButton, (int) ResponseType.Cancel);
			
//			helpButton.Text = resourceService.GetString("Global.HelpButtonText");
//			helpButton.Clicked += new EventHandler (HelpEvent);
//			this.ActionArea.Add(helpButton);
			
			Gtk.HBox topbox = new Gtk.HBox (false, 2);			
			statusPanel = new StatusPanel(this);			
			topbox.PackStart (statusPanel, false, false, 2);			
			topbox.PackStart (dialogPanel);

			this.VBox.PackStart (topbox);
		}
	}
}
