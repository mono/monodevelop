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
	/// Description of OnlyPermissionSuffixPermissionRule.	
	/// </summary>
	public class OnlyPermissionsSuffixPermissionRule : AbstractReflectionRule, ITypeRule
	{
		public override string Description {
			get {
				// FIXME: I18N
				return "Only permission names have the suffix 'Permission'";
			}
		}
		
		public override string Details {
			get {
				// FIXME: I18N
				return "A type that does not implement <code><a href='help://types/System.Security.IPermission'>IPermission</a></code> should never have the suffix <i>Permission</i>.";
			}
		}
		
		public OnlyPermissionsSuffixPermissionRule()
		{
			base.certainty = 99;
			base.priorityLevel = PriorityLevel.CriticalError;
		}
		
		public Resolution Check(Type type)
		{
			if (!typeof(System.Security.IPermission).IsAssignableFrom(type) && type.Name.EndsWith("Permission")) {
				// FIXME: I18N
				return new Resolution (this, String.Format ("Change the name of the type <code>{0}</code> so that it does not end with <i>Permission</i>.", type.FullName), type.FullName);
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
	public class OnlyPermissionsSuffixPermissionRuleTest
	{
		class OtherClass
		{}
		[Test]
		public void TestCorrectPermission()
		{
			OnlyPermissionsSuffixPermissionRule rule = new OnlyPermissionsSuffixPermissionRule();
			Assertion.AssertNull(rule.Check(typeof(System.Security.Permissions.EnvironmentPermission)));
			Assertion.AssertNull(rule.Check(typeof(System.Security.Permissions.FileIOPermission)));
			Assertion.AssertNull(rule.Check(typeof(OtherClass)));
		}
		
		class NotAnPermission
		{
		}
		[Test]
		public void TestIncorrectPermission()
		{
			OnlyPermissionsSuffixPermissionRule rule = new OnlyPermissionsSuffixPermissionRule();
			Assertion.AssertNotNull(rule.Check(typeof(NotAnPermission)));
		}
	}
}
#endif
#endregion
