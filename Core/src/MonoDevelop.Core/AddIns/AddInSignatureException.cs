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
	public class AddInSignatureException : Exception
	{
		/// <summary>
		/// Constructs a new <see cref="AddInTreeFormatException"/>
		/// </summary>
		public AddInSignatureException(string msg) : base("signature failure : " + msg)
		{
		}
	}
}
