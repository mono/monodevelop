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
		public override void Dispatch (StatefulMessageHandler cb, object ob)
		{
			if (DispatchService.IsGuiThread)
				cb (ob);
			else
				DispatchService.GuiSyncDispatch (cb, ob);
		}
		
		public override void AsyncDispatch (StatefulMessageHandler cb, object ob)
		{
			if (DispatchService.IsGuiThread)
				cb (ob);
			else
				DispatchService.GuiDispatch (cb, ob);
		}
	}
}
