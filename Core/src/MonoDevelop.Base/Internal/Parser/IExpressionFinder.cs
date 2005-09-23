// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;

namespace MonoDevelop.Internal.Parser
{
	public interface IExpressionFinder
	{
		/// <summary>
		/// Finds an expression before the current offset.
		/// </summary>
		string FindExpression(string text, int offset);
	}
}
