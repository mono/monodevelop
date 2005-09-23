// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krueger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.Collections;
using System.Reflection;

namespace ICSharpCode.AssemblyAnalyser.Rules
{
	/// <summary>
	/// Description of INamespaceRule.	
	/// </summary>
	public interface INamespaceRule : IReflectionRule
	{
		Resolution Check(string namespaceName, ICollection types);
	}
}
