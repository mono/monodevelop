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
	/// Is thrown when the xml has a false format.
	/// </summary>
	public class AddInTreeFormatException : Exception
	{
		/// <summary>
		/// Constructs a new <see cref="AddInTreeFormatException"/>
		/// </summary>
		public AddInTreeFormatException(string msg) : base("error reading the addin xml : " + msg)
		{
		}
	}
}
