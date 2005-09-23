// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krueger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.IO;
using System.Reflection;

namespace ICSharpCode.AssemblyAnalyser.Rules
{
	/// <summary>
	/// Description of AssemblyStrongName.	
	/// </summary>
	public class AssemblyStrongNameRule : AbstractReflectionRule, IAssemblyRule
	{
		public override string Description {
			get {
				// FIXME: I18N
				return "Assemblies should be strong named";
			}
		}
		
		public override string Details {
			get {
				// FIXME: I18N
				return "Assemblies with a strong name can be placed in the GAC. Furthermore only strong named assemblies can be referenced by a strong named assembly (Your assembly cannot be used by a strong named assembly if you do not sign it).";
			}
		}
		
		public AssemblyStrongNameRule()
		{
			certainty = 95;
		}
		
		public Resolution Check(Assembly assembly)
		{
			byte[] publicKeyToken = assembly.GetName().GetPublicKeyToken();
			// FIXME: I18N
			if (publicKeyToken == null || publicKeyToken.Length == 0) {
				return new Resolution (this, String.Format ("Sign the assembly {0} with a strong name.", Path.GetFileName (assembly.Location)), assembly.Location);
			}
			return null;
		}
	}
}
