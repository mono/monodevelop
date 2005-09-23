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
	public class AddInInitializeException : Exception
	{
		/// <summary>
		/// Constructs a new <see cref="AddInInitializeException"/>
		/// </summary>
		public AddInInitializeException(string fileName, Exception e) : base("Could not load add-in file : " + fileName + "\n exception got :" + e.ToString())
		{
		}
	}
}
