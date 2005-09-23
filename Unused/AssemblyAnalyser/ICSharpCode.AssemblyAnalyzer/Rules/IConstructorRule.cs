// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krueger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.Reflection;

namespace ICSharpCode.AssemblyAnalyser.Rules
{
	/// <summary>
	/// Description of IConstructorRule.	
	/// </summary>
	public interface IConstructorRule : IReflectionRule
	{
		Resolution Check(ConstructorInfo constructor);
	}
}
