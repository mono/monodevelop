// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krueger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.IO;
using System.Reflection;
using ICSharpCode.AssemblyAnalyser.Rules;

namespace ICSharpCode.AssemblyAnalyser
{
	/// <summary>
	/// Description of AssemblyVersionNumberRule.
	/// </summary>
	public class AssemblyVersionNumberRule : AbstractReflectionRule, IAssemblyRule
	{
		public override string Description {
			get {
				// FIXME: I18N
				return "Assemblies should have version numbers";
			}
		}
		
		public override string Details {
			get {
				// FIXME: I18N
				return "The Version number is part of the assembly identity. Use the <code><a href='help://types/System.Reflection.AssemblyVersionAttribute'>AssemblyVersion</a></code> attribute to assign a version number.";
			}
		}
		
		public AssemblyVersionNumberRule()
		{
			certainty = 95;
		}
		
		public Resolution Check(Assembly assembly)
		{
			if (assembly.GetName().Version == new Version(0, 0, 0, 0)) {
				// FIXME: I18N
				return new Resolution(this, String.Format ("Add an <code><a href='help://types/System.Reflection.AssemblyVersionAttribute'>AssemblyVersion</a></code> attribute to the assembly {0}.", Path.GetFileName (assembly.Location)), assembly.Location);
			}
			return null;
		}
	}
}
