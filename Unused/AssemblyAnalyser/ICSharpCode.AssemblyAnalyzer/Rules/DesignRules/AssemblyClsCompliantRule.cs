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
	public class AssemblyClsCompliantRule : AbstractReflectionRule, IAssemblyRule
	{
		public override string Description {
			get {
				// FIXME: I18N
				return "Assemblies should be marked CLSCompliant";
			}
		}
		
		public override string Details {
			get {
				// FIXME: I18N
				return "Assemblies should be marked CLS (Common Language Specification) compliant using the <code><a href='help://types/System.CLSCompliantAttribute'>CLSCompliantAttribute</a></code> assemblies without this attribute are not CLS compliant. It is possible to have non-CLS compliant parts in a CLS compliant assembly. In this case all non-compliant members must have the CLSCompliant attribute set to <code>false</code>. You should supply for each non-CLS compliant member a CLS compliant alternative.";
			}
		}
		
		public AssemblyClsCompliantRule()
		{
			certainty = 99;
		}
		
		public Resolution Check(Assembly assembly)
		{
			object[] attributes = assembly.GetCustomAttributes(typeof(System.CLSCompliantAttribute), true);
			if (attributes == null || attributes.Length == 0) {
				// FIXME: I18N
				return new Resolution (this, String.Format ("Declare a <code><a href='help://types/System.CLSCompliantAttribute'>CLSCompliantAttribute</a></code> in the assembly <code>{0}</code> and its value should be <code>true</code>.", Path.GetFileName (assembly.Location)), assembly.Location);
			} else {
				foreach (CLSCompliantAttribute attr in attributes) {
					if (!attr.IsCompliant) {
						// FIXME: I18N
						return new Resolution (this, String.Format ("Set the <code><a href='help://types/System.CLSCompliantAttribute'>CLSCompliantAttribute</a></code> in the assembly <code>{0}</code> to <code>true<code>.", Path.GetFileName (assembly.Location)), assembly.Location);
					}
				}
			}
			return null;
		}
	}
}
