// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;

namespace MonoDevelop.Core.AddIns
{
	/// <summary>
	/// Is thrown when the AddInTree could not find the requested path.
	/// </summary>
	public class AddInLoadException : Exception
	{
		/// <summary>
		/// Constructs a new <see cref="AddInLoadException"/>
		/// </summary>
		public AddInLoadException(string reason) : base(reason)
		{
		}
	}
	
	public class MissingDependencyException: Exception
	{
		public MissingDependencyException (string message): base (message)
		{
		}
	}
}
