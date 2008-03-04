//
// CommonAssemblySigningPreferences.cs
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
using MonoDevelop.Core.Gui;
using MonoDevelop.Core.Gui.Dialogs;
using MonoDevelop.Projects;
using MonoDevelop.Core;

namespace MonoDevelop.Projects.Gui.Dialogs.OptionPanels
{
	internal partial class CommonAssemblySigningPreferences : Gtk.Bin, IDialogPanel
	{
		Project project;
		AbstractProjectConfiguration configuration;
		public CommonAssemblySigningPreferences()
		{
			this.Build();
		}
		
		public void LoadPanelContents ()
		{
			Properties props = (Properties) CustomizationObject;
			configuration = props.Get<AbstractProjectConfiguration> ("Config");
			project = ((Properties)CustomizationObject).Get<Project> ("Project");
			this.signAssemblyCheckbutton.Toggled += new EventHandler (SignAssemblyCheckbuttonActivated);
			if (configuration != null) {
				this.signAssemblyCheckbutton.Active = configuration.SignAssembly;
				this.strongNameFileEntry.Path = configuration.AssemblyKeyFile;
			}
			if (project != null) {
				this.strongNameFileEntry.DefaultPath = project.BaseDirectory;
			}
			SignAssemblyCheckbuttonActivated (null, null);
		}
		
		void SignAssemblyCheckbuttonActivated (object sender, EventArgs e)
		{
			this.strongNameFileLabel.Sensitive = this.strongNameFileEntry.Sensitive = this.signAssemblyCheckbutton.Active;
		}
		
		public bool StorePanelContents ()
		{
			if (configuration != null) {
				configuration.SignAssembly = this.signAssemblyCheckbutton.Active;
				configuration.AssemblyKeyFile = this.strongNameFileEntry.Path;
			}
			return true;
		}
		
#region Cut&paste from abstract option panel
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
