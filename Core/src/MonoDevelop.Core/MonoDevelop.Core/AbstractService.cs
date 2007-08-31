// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;

using Mono.Addins;
using MonoDevelop.Core;

namespace MonoDevelop.Core
{
	public class AbstractService : IService
	{
		public virtual void InitializeService()
		{
			OnInitialize(EventArgs.Empty);
		}
		
		
		public virtual void UnloadService()
		{
			OnUnload(EventArgs.Empty);
		}
		
		protected virtual void OnInitialize(EventArgs e)
		{
			if (Initialize != null) {
				Initialize(this, e);
			}
		}
		
		protected virtual void OnUnload(EventArgs e)
		{
			if (Unload != null) {
				Unload(this, e);
			}
		}
		
		public event EventHandler Initialize;
		public event EventHandler Unload;
	}
}
