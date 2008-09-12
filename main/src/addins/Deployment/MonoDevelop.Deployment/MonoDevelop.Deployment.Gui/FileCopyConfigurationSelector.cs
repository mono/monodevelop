
using System;
using MonoDevelop.Core;
using MonoDevelop.Deployment;
using Mono.Addins;

namespace MonoDevelop.Deployment.Gui
{
	
	
	[System.ComponentModel.Category("widget")]
	[System.ComponentModel.ToolboxItem(true)]
	public partial class FileCopyConfigurationSelector : Gtk.Bin
	{
		FileCopyHandler[] handlers;
		FileCopyConfiguration currentConfig;
		bool loading;
		
		public event EventHandler ConfigurationChanged;
		
		public FileCopyConfigurationSelector ()
		{
			this.Build();
			
			loading = true;
			handlers = DeployService.GetFileCopyHandlers ();
			foreach (FileCopyHandler handler in handlers) {
				comboHandlers.AppendText (handler.Name);
			}
			loading = false;
			comboHandlers.Active = 0;
		}
		
		public FileCopyConfiguration Configuration {
			get {
				return currentConfig;
			}
			set {
				if (value != null) {
					loading = true;
					int i = Array.IndexOf (handlers, value.Handler);
					comboHandlers.Active = i;
					if (i != -1)
						LoadConfiguration (value);
					else
						LoadConfiguration (null);
					loading = false;
				} else {
					// There is no configuration, create a default one
					comboHandlers.Active = -1;
					comboHandlers.Active = 0;
				}
			}
		}
		
		public static bool HasEditor (FileCopyConfiguration config)
		{
			return GetEditor (config) != null;
		}
		
		static IFileCopyConfigurationEditor GetEditor (FileCopyConfiguration config)
		{
			foreach (IFileCopyConfigurationEditor editor in AddinManager.GetExtensionObjects ("/MonoDevelop/Deployment/FileCopyConfigurationEditors", false)) {
				if (editor.CanEdit (config))
					return editor;
			}
			return null;
		}

		protected virtual void OnComboHandlersChanged(object sender, System.EventArgs e)
		{
			if (loading)
				return;
			if (comboHandlers.Active < 0 || comboHandlers.Active >= handlers.Length) {
				LoadConfiguration (null);
				return;
			}
			
			FileCopyConfiguration config = handlers [comboHandlers.Active].CreateConfiguration ();
			LoadConfiguration (config);
		}
		
		void LoadConfiguration (FileCopyConfiguration config)
		{
			if (editorBox.Child != null)
				editorBox.Remove (editorBox.Child);
			
			IFileCopyConfigurationEditor editor = null;
			if (config != null)
				editor = GetEditor (config);
			
			if (editor != null)
				editorBox.Add (editor.CreateEditor (config));
			
			ShowAll ();
			
			currentConfig = config;
			if (ConfigurationChanged != null)
				ConfigurationChanged (this, EventArgs.Empty);
		}
	}
}
