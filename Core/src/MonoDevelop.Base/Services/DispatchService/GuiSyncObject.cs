// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Lluis Sanchez Gual" email="lluis@ximian.com"/>
//     <version value="$version"/>
// </file>

using System;

namespace MonoDevelop.Services
{
	[SyncContext (typeof(GuiSyncContext))]
	public class GuiSyncObject: SyncObject
	{
	}
}
