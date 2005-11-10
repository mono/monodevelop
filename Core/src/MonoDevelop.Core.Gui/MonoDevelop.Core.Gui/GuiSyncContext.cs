// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Lluis Sanchez Gual" email="lluis@ximian.com"/>
//     <version value="$version"/>
// </file>

using System;
using MonoDevelop.Core;

namespace MonoDevelop.Core.Gui
{
	public class GuiSyncContext: SyncContext
	{
		DispatchService dispatcher;
		
		public override void Dispatch (StatefulMessageHandler cb, object ob)
		{
			if (dispatcher == null)
				dispatcher = Services.DispatchService;
				
			if (dispatcher.IsGuiThread)
				cb (ob);
			else
				dispatcher.GuiSyncDispatch (cb, ob);
		}
		
		public override void AsyncDispatch (StatefulMessageHandler cb, object ob)
		{
			if (dispatcher == null)
				dispatcher = Services.DispatchService;
				
			if (dispatcher.IsGuiThread)
				cb (ob);
			else
				dispatcher.GuiDispatch (cb, ob);
		}
	}
}
