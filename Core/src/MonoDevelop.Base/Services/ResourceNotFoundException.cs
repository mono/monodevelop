// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;

using MonoDevelop.Services;

namespace MonoDevelop.Core.Services
{
	/// <summary>
	/// Is thrown when the GlobalResource manager can't find a requested
	/// resource.
	/// </summary>
	public class ResourceNotFoundException : Exception
	{
		public ResourceNotFoundException(string resource) : base(String.Format (GettextCatalog.GetString("Resource not found: {0}"), resource))
		{
		}
	}
}
