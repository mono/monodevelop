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
	/// Description of MembershipConditionNamesSuffixIsMembershipCondition
	/// </summary>
	public class MembershipConditionNamesSuffixIsMembershipCondition : AbstractReflectionRule, ITypeRule
	{
		public override string Description {
			get {
				// FIXME: I18N
				return "MembershipCondition names have the suffix 'MembershipCondition'";
			}
		}
		
		// System.Attribute
		public override string Details {
			get {
				// FIXME: I18N
				return " A type that implements <code><a href='help://types/System.Security.Policy.IMembershipCondition'>IMembershipCondition</a></code> is a condition and its name should always end with <i>MembershipCondition</i> like in <code>UrlMembershipCondition</code>.";
			}
		}
		
		public Resolution Check(Type type)
		{
			if (typeof(System.Security.Policy.IMembershipCondition).IsAssignableFrom(type) && !type.Name.EndsWith("MembershipCondition")) {
				// FIXME: I18N
				return new Resolution (this, String.Format ("Change the name of the type <code>{0}</code> so that it ends with <I>MembershipCondition</I>.", type.FullName), type.FullName);
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
	public class MembershipConditionNamesSuffixIsMembershipConditionTest
	{
		class MyClass 
		{
		}
		[Test]
		public void TestCorrectMembershipCondition()
		{
			MembershipConditionNamesSuffixIsMembershipCondition membershipConditionNamesSuffixIsMembershipCondition = new MembershipConditionNamesSuffixIsMembershipCondition();
			Assertion.AssertNull(membershipConditionNamesSuffixIsMembershipCondition.Check(typeof(System.Security.Policy.AllMembershipCondition)));
			Assertion.AssertNull(membershipConditionNamesSuffixIsMembershipCondition.Check(typeof(System.Security.Policy.ZoneMembershipCondition)));
			Assertion.AssertNull(membershipConditionNamesSuffixIsMembershipCondition.Check(typeof(MyClass)));
		}
		
		class MyClass2 : System.Security.Policy.IMembershipCondition
		{
			#region System.Security.ISecurityEncodable interface implementation
			public void FromXml(System.Security.SecurityElement e)
			{
				
			}
			
			public System.Security.SecurityElement ToXml()
			{
				return null;
			}
			#endregion
			
			#region System.Security.ISecurityPolicyEncodable interface implementation
			public void FromXml(System.Security.SecurityElement e, System.Security.Policy.PolicyLevel level)
			{
				
			}
			
			public System.Security.SecurityElement ToXml(System.Security.Policy.PolicyLevel level)
			{
				return null;
			}
			#endregion
			
			#region System.Security.Policy.IMembershipCondition interface implementation
			public System.Security.Policy.IMembershipCondition Copy()
			{
				return null;
			}
			
			public bool Check(System.Security.Policy.Evidence evidence)
			{
				return false;
			}
			#endregion
		}
		
		[Test]
		public void TestIncorrectAttribute()
		{
			MembershipConditionNamesSuffixIsMembershipCondition membershipConditionNamesSuffixIsMembershipCondition = new MembershipConditionNamesSuffixIsMembershipCondition();
			Assertion.AssertNotNull(membershipConditionNamesSuffixIsMembershipCondition.Check(typeof(MyClass2)));
		}
	}
}
#endif
#endregion
