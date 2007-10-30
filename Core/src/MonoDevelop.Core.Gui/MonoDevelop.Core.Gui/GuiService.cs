
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

		public static ResourceService Resources {
			get {
				if (resourceService == null)
					resourceService = new ResourceService ();
				return resourceService;
			}
		}
	
		public static MessageService MessageService {
			get {
				if (messageService == null)
					messageService = new MessageService ();
				return messageService;
			}
		}
	
		public static void RunAddinManager (Gtk.Window parent)
		{
			AddinManagerWindow.Run (parent);
		}
	}
}
