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
	/// Description of OnlyAttributesSuffixAttribute.	
	/// </summary>
	public class OnlyAttributesSuffixAttributeRule : AbstractReflectionRule, ITypeRule
	{
		public override string Description {
			get {
				// FIXME: I18N
				return "Only attribute names have the suffix 'Attribute'";
			}
		}
		
		public override string Details {
			get {
				// FIXME: I18N
				return "A type that does not extend from <code><a href='help://types/System.Attribute'>Attribute</a></code> should never have the suffix <i>Attribute</i>.";
			}
		}
		
		public OnlyAttributesSuffixAttributeRule()
		{
			base.certainty = 99;
			base.priorityLevel = PriorityLevel.CriticalError;
		}
		
		public Resolution Check(Type type)
		{
			if (!type.IsSubclassOf(typeof(System.Attribute)) && type.Name.EndsWith("Attribute")) {
				// FIXME: I18N
				return new Resolution (this, String.Format ("Change the name of the type <code>{0}</code> so that it does not end with <i>Attribute</i>.", type.FullName), type.FullName);
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
	public class OnlyAttributesSuffixAttributeTest
	{
		class MyOtherClass
		{
		}
		class RealAttribute : System.Attribute
		{
		}
		[Test]
		public void TestCorrectAttribute()
		{
			OnlyAttributesSuffixAttributeRule rule = new OnlyAttributesSuffixAttributeRule();
			Assertion.AssertNull(rule.Check(typeof(MyOtherClass)));
			Assertion.AssertNull(rule.Check(typeof(RealAttribute)));
		}
		
		class MyAttribute
		{
		}
		[Test]
		public void TestIncorrectAttribute()
		{
			OnlyAttributesSuffixAttributeRule rule = new OnlyAttributesSuffixAttributeRule();
			Assertion.AssertNotNull(rule.Check(typeof(MyAttribute)));
		}
	}
}
#endif
#endregion
