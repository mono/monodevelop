// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Lluis Sanchez Gual" email="lluis@ximian.com"/>
//     <version value="$version"/>
// </file>

using System;

namespace MonoDevelop.Services
{
	public class GuiSyncContext: SyncContext
	{
		DispatchService dispatcher;
		
		public override void Dispatch (StatefulMessageHandler cb, object ob)
		{
			if (dispatcher == null)
				dispatcher = Runtime.DispatchService;
				
			if (dispatcher.IsGuiThread)
				cb (ob);
			else
				dispatcher.GuiSyncDispatch (cb, ob);
		}
		
		public override void AsyncDispatch (StatefulMessageHandler cb, object ob)
		{
			if (dispatcher == null)
				dispatcher = Runtime.DispatchService;
				
			dispatcher.GuiDispatch (cb, ob);
		}
	}
}
