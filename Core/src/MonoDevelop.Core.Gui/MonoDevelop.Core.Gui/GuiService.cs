
using MonoDevelop.Core;
using MonoDevelop.Core.Gui.Dialogs;
using System;
using Mono.Addins.Gui;

namespace MonoDevelop.Core.Gui
{
	public class Services
	{
		static ResourceService resourceService;
		static MessageService messageService;
		static DispatchService dispatchService;

		public static ResourceService Resources {
			get {
				if (resourceService == null)
					resourceService = (ResourceService) ServiceManager.GetService (typeof(ResourceService));
				return resourceService;
			}
		}
	
		public static MessageService MessageService {
			get {
				if (messageService == null)
					messageService = (MessageService) ServiceManager.GetService (typeof(MessageService));
				return messageService;
			}
		}
	
		public static DispatchService DispatchService {
			get {
				if (dispatchService == null)
					dispatchService = (DispatchService) ServiceManager.GetService (typeof(DispatchService));
				return dispatchService;
			}
		}
		
		public static void RunAddinManager (Gtk.Window parent)
		{
			AddinManagerWindow.Run (parent);
		}
	}
}
