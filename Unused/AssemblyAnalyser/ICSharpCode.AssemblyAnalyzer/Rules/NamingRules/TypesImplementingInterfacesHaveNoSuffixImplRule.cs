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
	/// Description of TypesImplementingInterfacesHaveNoSuffixImplRule.	
	/// </summary>
	public class TypesImplementingInterfacesHaveNoSuffixImplRule : AbstractReflectionRule, ITypeRule
	{
		public override string Description {
			get {
				// FIXME: I18N
				return "Types that implement interfaces have no 'Impl' suffix";
			}
		}
		
		public override string Details {
			get {
				// FIXME: I18N
				return "Do not use the <i>Impl</i> suffix for providing an interface implementation. Consider using the interface name without the <i>I</i> like <code>Component</code> that implements <code>IComponent</code>.";
			}
		}
		
		public Resolution Check(Type type)
		{
			if (type.GetInterfaces().Length > 0 && type.Name.EndsWith("Impl")) {
				// FIXME: I18N
				return new Resolution (this, String.Format ("Change the name of the type <code>{0}</code> so that it does not end with <i>Impl</i>.", type.FullName), type.FullName);
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
	public class TypesImplementingInterfacesHaveNoSuffixImplRuleTest
	{
		interface IInterface
		{
		}
		class AClassImplTest : IInterface
		{
			
		}
		[Test]
		public void TestCorrectTypenames()
		{
			TypesImplementingInterfacesHaveNoSuffixImplRule rule = new TypesImplementingInterfacesHaveNoSuffixImplRule();
			Assertion.AssertNull(rule.Check(typeof(AClassImplTest)));
		}
		
		class BImpl : IInterface
		{
			
		}
		[Test]
		public void TestIncorrectTypenames()
		{
			TypesImplementingInterfacesHaveNoSuffixImplRule rule = new TypesImplementingInterfacesHaveNoSuffixImplRule();
			Assertion.AssertNotNull(rule.Check(typeof(BImpl)));
		}
	}
}
#endif
#endregion
