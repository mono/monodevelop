// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;

using MonoDevelop.Core;

namespace MonoDevelop.Core.Gui
{
	/// <summary>
	/// Is thrown when the GlobalResource manager can't find a requested
	/// resource.
	/// </summary>
	public class ResourceNotFoundException : Exception
	{
		public ResourceNotFoundException (string resource)
			: base (GettextCatalog.GetString ("Resource not found: {0}", resource))
		{
		}
	}
}
