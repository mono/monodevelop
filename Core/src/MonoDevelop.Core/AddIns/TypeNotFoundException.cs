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
	/// Is thrown when the AddInTree could not create a specified object.
	/// </summary>
	public class TypeNotFoundException : Exception
	{
		/// <summary>
		/// Constructs a new <see cref="TypeNotFoundException"/>
		/// </summary>
		public TypeNotFoundException(string typeName) : base("Unable to create object from type : " + typeName)
		{
		}
	}
}
