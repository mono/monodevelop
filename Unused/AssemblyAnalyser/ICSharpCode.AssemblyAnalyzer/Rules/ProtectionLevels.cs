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
	[Flags]
	public enum ProtectionLevels
	{
		None = 0,
		
		Public   = 1,
		Family   = 2,
		Private  = 4,
		Assembly = 8,
		FamilyAndAssembly = 16,
		FamilyOrAssembly  = 32,
		
		NestedPublic   = 64,
		NestedFamily   = 128,
		NestedPrivate  = 256,
		NestedAssembly = 512,
		NestedFamilyAndAssembly = 1024,
		NestedFamilyOrAssembly = 2048,
		
		All = Public | Family | Private | Assembly | FamilyAndAssembly | FamilyOrAssembly |
		      NestedPublic | NestedFamily | NestedPrivate | NestedAssembly | NestedFamilyAndAssembly | NestedFamilyOrAssembly,
	}
}
