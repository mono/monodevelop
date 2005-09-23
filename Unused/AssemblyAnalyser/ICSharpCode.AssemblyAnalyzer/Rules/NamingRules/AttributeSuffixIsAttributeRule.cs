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
	/// Description of AttributeSuffixIsAttribute.	
	/// </summary>
	public class AttributeSuffixIsAttributeRule : AbstractReflectionRule, ITypeRule
	{
		public override string Description {
			get {
				// FIXME: I18N
				return "Attribute names have the suffix 'Attribute'";
			}
		}
		
		public override string Details {
			get {
				// FIXME: I18N
				return "A type that extends <code><a href='help://types/System.Attribute'>Attribute</a></code> is an attribute and its name should always end with <i>Attribute</i> like in <code>CategoryAttribute</code>.";
			}
		}
		
		public Resolution Check(Type type)
		{
			if (type.IsSubclassOf(typeof(System.Attribute)) && !type.Name.EndsWith("Attribute")) {
				// FIXME: I18N
				return new Resolution (this, String.Format ("Change the name of the type <code>{0}</code> so that it ends with <I>Attribute</I>.", type.FullName), type.FullName);
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
	public class AttributeSuffixIsAttributeRuleTest
	{
		class MyAttribute : System.Attribute
		{
		}
		[Test]
		public void TestCorrectAttribute()
		{
			AttributeSuffixIsAttributeRule rule = new AttributeSuffixIsAttributeRule();
			Assertion.AssertNull(rule.Check(typeof(MyAttribute)));
		}
		
		class MyAttr : System.Attribute
		{
		}
		[Test]
		public void TestIncorrectAttribute()
		{
			AttributeSuffixIsAttributeRule rule = new AttributeSuffixIsAttributeRule();
			Assertion.AssertNotNull(rule.Check(typeof(MyAttr)));
		}
	}
}
#endif
#endregion
