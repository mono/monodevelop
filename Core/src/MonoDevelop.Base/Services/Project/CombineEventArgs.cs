// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using MonoDevelop.Internal.Project;

namespace MonoDevelop.Services
{
	public delegate void CombineEventHandler(object sender, CombineEventArgs e);
	
	public class CombineEventArgs : EventArgs
	{
		Combine combine;
		
		public Combine Combine {
			get {
				return combine;
			}
		}
		
		public CombineEventArgs(Combine combine)
		{
			this.combine = combine;
		}
	}
}
