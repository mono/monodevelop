// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Lluis Sanchez Gual" email="lluis@ximian.com"/>
//     <version value="$version"/>
// </file>

using System;
using MonoDevelop.Internal.Project;

namespace MonoDevelop.Internal.Project
{
	public delegate void CombineEntryEventHandler(object sender, CombineEntryEventArgs e);
	
	public class CombineEntryEventArgs : EventArgs
	{
		CombineEntry entry;
		
		public CombineEntry CombineEntry {
			get {
				return entry;
			}
		}
		
		public CombineEntryEventArgs (CombineEntry entry)
		{
			this.entry = entry;
		}
	}
}
