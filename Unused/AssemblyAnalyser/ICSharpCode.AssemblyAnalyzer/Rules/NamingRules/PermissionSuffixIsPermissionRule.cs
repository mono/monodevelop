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
	/// Description of PermissionSuffixIsPermissionRule.	
	/// </summary>
	public class PermissionSuffixIsPermissionRule : AbstractReflectionRule, ITypeRule
	{
		public override string Description {
			get {
				// FIXME: I18N
				return "Permission names have the suffix 'Permission'";
			}
		}
		
		// System.Attribute
		public override string Details {
			get {
				// FIXME: I18N
				return "A type that extends <code><a href='help://types/System.Security.IPermission'>IPermission</a></code> is a permission and its name should always end with <i>Permission</i> like in <code>FileIOPermission</code>.";
			}
		}
		
		public Resolution Check(Type type)
		{
			if (typeof(System.Security.IPermission).IsAssignableFrom(type) && !type.Name.EndsWith("Permission")) {
				// FIXME: I18N
				return new Resolution (this, String.Format ("Change the name of the type <code>{0}</code> so that it ends with <I>Permission</I>.", type.FullName), type.FullName);
			}
			return null;
		}
	}
}

#region Unit Test
#if TEST
namespace ICSharpCode.AssemblyAnalyser.Rules
{
	using NUnit.Framework;

	[TestFixture]
	public class PermissionSuffixIsPermissionRuleTest
	{
		[Test]
		public void TestCorrectPermission()
		{
			PermissionSuffixIsPermissionRule rule = new PermissionSuffixIsPermissionRule();
			Assertion.AssertNull(rule.Check(typeof(System.Security.Permissions.EnvironmentPermission)));
			Assertion.AssertNull(rule.Check(typeof(System.Security.Permissions.FileIOPermission)));
			Assertion.AssertNull(rule.Check(typeof(PermissionSuffixIsPermissionRuleTest)));
		}
		
	}
}
#endif
#endregion
