// GeneralOptionsPanel.cs
//
// Author:
//   Mike Krüger <mkrueger@novell.com>
//
// Copyright (c) 2008 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using MonoDevelop.Core.Gui.Dialogs;
using MonoDevelop.Core.Gui;

namespace MonoDevelop.SourceEditor.OptionPanels
{
	public partial class GeneralOptionsPanel : Gtk.Bin, IDialogPanel
	{
		public GeneralOptionsPanel()
		{
			this.Build();
			this.radiobutton1.Toggled += CheckFontSelection;
			this.radiobutton2.Toggled += CheckFontSelection;
		}
		
		void CheckFontSelection (object sender, EventArgs args)
		{
			this.fontselection.Sensitive = this.radiobutton2.Active;
		}
		
		bool   wasActivated = false;
		bool   isFinished   = true;
		object customizationObject = null;
		
		public Gtk.Widget Control {
			get {
				return this;
			}
		}

		public virtual Gtk.Image Icon {
			get {
				return null;
			}
		}
		
		public bool WasActivated {
			get {
				return wasActivated;
			}
		}
		
		public virtual object CustomizationObject {
			get {
				return customizationObject;
			}
			set {
				customizationObject = value;
				OnCustomizationObjectChanged();
			}
		}
		
		public virtual bool EnableFinish {
			get {
				return isFinished;
			}
			set {
				if (isFinished != value) {
					isFinished = value;
					OnEnableFinishChanged();
				}
			}
		}
		
		public virtual bool ReceiveDialogMessage(DialogMessage message)
		{
			try {
				switch (message) {
					case DialogMessage.Activated:
						if (!wasActivated) {
							LoadPanelContents();
							wasActivated = true;
						}
						break;
					case DialogMessage.OK:
						if (wasActivated) {
							return StorePanelContents();
						}
						break;
				}
			} catch (Exception ex) {
				MessageService.ShowException (ex);
			}
			
			return true;
		}
		
		public virtual void LoadPanelContents()
		{
			this.codeCompletioncheckbutton.Active = SourceEditorOptions.Options.EnableCodeCompletion;
			this.quickFinderCheckbutton.Active    = SourceEditorOptions.Options.EnableQuickFinder;
			this.foldingCheckbutton.Active        = SourceEditorOptions.Options.ShowFoldMargin;
			this.radiobutton1.Active              = SourceEditorOptions.Options.EditorFontType == EditorFontType.DefaultMonospace;
			this.radiobutton2.Active              = SourceEditorOptions.Options.EditorFontType == EditorFontType.UserSpecified;
			this.fontselection.FontName           = SourceEditorOptions.Options.FontName;
			CheckFontSelection (this, EventArgs.Empty);
		}
		
		public virtual bool StorePanelContents()
		{
			SourceEditorOptions.Options.EnableCodeCompletion = this.codeCompletioncheckbutton.Active;
			SourceEditorOptions.Options.EnableQuickFinder    = this.quickFinderCheckbutton.Active;
			SourceEditorOptions.Options.ShowFoldMargin       = this.foldingCheckbutton.Active;
			SourceEditorOptions.Options.EditorFontType       = this.radiobutton1.Active ? EditorFontType.DefaultMonospace : EditorFontType.UserSpecified;
			SourceEditorOptions.Options.FontName             = this.fontselection.FontName;
			return true;
		}
		
		protected virtual void OnEnableFinishChanged()
		{
			if (EnableFinishChanged != null) {
				EnableFinishChanged(this, null);
			}
		}
		protected virtual void OnCustomizationObjectChanged()
		{
			if (CustomizationObjectChanged != null) {
				CustomizationObjectChanged(this, null);
			}
		}
		
		public event EventHandler CustomizationObjectChanged;
		public event EventHandler EnableFinishChanged;
	}
}
