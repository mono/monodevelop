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
	public class ConditionWithoutRequiredAttributesException : Exception
	{
		/// <summary>
		/// Constructs a new <see cref="ConditionWithoutRequiredAttributesException"/>
		/// </summary>
		public ConditionWithoutRequiredAttributesException() : base("conditions need at least one required attribute to be identified.")
		{
		}
	}
}
