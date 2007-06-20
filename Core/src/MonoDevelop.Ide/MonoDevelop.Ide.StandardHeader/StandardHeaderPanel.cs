//
// StandardHeaderPanel.cs
//
// Author:
//   Mike Krüger <mkrueger@novell.com>
//
// Copyright (C) 2007 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.IO;
using System.Collections.Generic;

using MonoDevelop.Core.Gui.Dialogs;
using MonoDevelop.Core;

using Gtk;
using MonoDevelop.Components;

namespace MonoDevelop.Ide.StandardHeaders
{
	public partial class StandardHeaderPanel : Gtk.Bin, IDialogPanel
	{
		public StandardHeaderPanel()
		{
			this.Build();
		}
		
		
		public void LoadPanelContents()
		{
			this.headerTextview.Buffer.Text         = StandardHeaderService.Header;
			this.generateCommentsCheckbutton.Active = StandardHeaderService.GenerateComments;
			
			foreach (KeyValuePair<string, string> header in StandardHeaderService.CustomTemplates) {
				templateCombobox.AppendText (header.Key);
			}
			foreach (KeyValuePair<string, string> header in StandardHeaderService.HeaderTemplates) {
				templateCombobox.AppendText (header.Key);
			}
			
			this.headerTextview.Buffer.Changed += new EventHandler (ClearTemplateComboBox);
			
			this.addButton.Clicked += delegate {
				NewHeaderTemplateDialog newHeaderTemplateDialog = new NewHeaderTemplateDialog ();
				Gtk.ResponseType response = (Gtk.ResponseType)newHeaderTemplateDialog.Run ();
				if (response == Gtk.ResponseType.Ok) {
					StandardHeaderService.AddTemplate (newHeaderTemplateDialog.HeaderName, this.headerTextview.Buffer.Text);
					templateCombobox.AppendText (newHeaderTemplateDialog.HeaderName);
				}
				newHeaderTemplateDialog.Destroy ();
			};
			
			this.removeButton.Clicked += delegate {
				if (templateCombobox.Active < 0)
					return;
				if (Services.MessageService.AskQuestion (MonoDevelop.Core.GettextCatalog.GetString ("Are you sure to remove the custom header template '{0}'?", templateCombobox.ActiveText), "MonoDevelop")) {
					StandardHeaderService.RemoveTemplate (templateCombobox.ActiveText);
					templateCombobox.RemoveText (templateCombobox.Active);
				}
			};
			
			templateCombobox.Changed += delegate {
				if (templateCombobox.Active < 0)
					return;
				this.headerTextview.Buffer.Changed -= new System.EventHandler (ClearTemplateComboBox);
				this.addButton.Sensitive = false;
				foreach (System.Collections.Generic.KeyValuePair<string, string> header in StandardHeaderService.CustomTemplates) {
					if (header.Key == templateCombobox.ActiveText) {
						this.headerTextview.Buffer.Text = header.Value;
						this.removeButton.Sensitive = true;
						break;
					}
				}
							
				foreach (System.Collections.Generic.KeyValuePair<string, string> header in StandardHeaderService.HeaderTemplates) {
					if (header.Key == templateCombobox.ActiveText) {
						this.headerTextview.Buffer.Text = header.Value;
						this.removeButton.Sensitive = false;
						break;
					}
				}
				this.headerTextview.Buffer.Changed += new System.EventHandler (ClearTemplateComboBox);
			};
		}
		
		void ClearTemplateComboBox (object sender, EventArgs e)
		{
			templateCombobox.Active     = -1;
			this.removeButton.Sensitive = false;
			this.addButton.Sensitive    = true;
		}
		
		public bool StorePanelContents()
		{
			StandardHeaderService.Header           = headerTextview.Buffer.Text;
			StandardHeaderService.GenerateComments = generateCommentsCheckbutton.Active;
			StandardHeaderService.CommitChanges ();
			return true;
		}
		
#region Cut & Paste from abstract option panel
		bool   wasActivated = false;
		bool   isFinished   = true;
		object customizationObject = null;
		
		public Widget Control {
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
#endregion
	}
	
}
