// BehaviorPanel.cs
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
	public partial class BehaviorPanel : Gtk.Bin, IDialogPanel
	{
		public BehaviorPanel ()
		{
			this.Build ();
			indentationCombobox.InsertText (0, "None");
			indentationCombobox.InsertText (1, "Automatic");
			indentationCombobox.InsertText (2, "Smart");
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
				Services.MessageService.ShowError (ex);
			}
			
			return true;
		}
		
		public virtual void LoadPanelContents ()
		{
			this.autoInsertTemplateCheckbutton.Active  = SourceEditorOptions.Options.AutoInsertTemplates;
			this.convertTabsToSpacesCheckbutton.Active = SourceEditorOptions.Options.TabsToSpaces;
			this.autoInsertBraceCheckbutton.Active = SourceEditorOptions.Options.AutoInsertMatchingBracket;
			this.indentationCombobox.Active = (int)SourceEditorOptions.Options.IndentStyle;
			this.indentAndTabSizeSpinbutton.Value = SourceEditorOptions.Options.TabSize;
		}
		
		public virtual bool StorePanelContents ()
		{
			SourceEditorOptions.Options.AutoInsertTemplates = this.autoInsertTemplateCheckbutton.Active;
			SourceEditorOptions.Options.TabsToSpaces = this.convertTabsToSpacesCheckbutton.Active;
			SourceEditorOptions.Options.AutoInsertMatchingBracket = this.autoInsertBraceCheckbutton.Active;
			SourceEditorOptions.Options.IndentStyle = (MonoDevelop.Ide.Gui.Content.IndentStyle)this.indentationCombobox.Active;
			SourceEditorOptions.Options.TabSize = (int)this.indentAndTabSizeSpinbutton.Value;
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
