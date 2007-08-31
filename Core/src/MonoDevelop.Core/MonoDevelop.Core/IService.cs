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
	/// <summary>
	/// This interface must be implemented by all services.
	/// </summary>
	public interface IService
	{
		/// <summary>
		/// This method is called after the services are loaded.
		/// </summary>
		void InitializeService();
		
		/// <summary>
		/// This method is called before the service is unloaded.
		/// </summary>
		void UnloadService();
		
		event EventHandler Initialize;
		event EventHandler Unload;
	}
}
