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
	/// Description of TypesShouldBeInNamespaces.	
	/// </summary>
	public class TypesShouldBeInNamespacesRule : AbstractReflectionRule, ITypeRule
	{
		public override string Description {
			get {
				// FIXME: I18N
				return "Types should be defined in namespaces";
			}
		}
		
		public override string Details {
			get {
				// FIXME: I18N
				return "A type should always be defined inside a namespace to avoid naming collisions.";
			}
		}
		
		public Resolution Check(Type type)
		{
			if (type.Namespace == null || type.Namespace.Length == 0) {
				// FIXME: I18N
				return new Resolution (this, String.Format ("Declare <code>{0}</code> inside a namespace.", type.FullName), type.FullName);
			}
			return null;
		}
	}
}

#region Unit Test
#if TEST
class OutsideNamespace
{
	
}
namespace ICSharpCode.AssemblyAnalyser.Rules
{
	using NUnit.Framework;

	[TestFixture]
	public class TypesShouldBeInNamespacesRuleTest
	{
		interface ICorrectInterface
		{
		}
		[Test]
		public void TestCorrectAttribute()
		{
			TypesShouldBeInNamespacesRule rule = new TypesShouldBeInNamespacesRule();
			Assertion.AssertNull(rule.Check(typeof(System.ICloneable)));
			Assertion.AssertNull(rule.Check(typeof(TypesShouldBeInNamespacesRuleTest)));
			Assertion.AssertNull(rule.Check(typeof(ICorrectInterface)));
		}
		
		[Test]
		public void TestIncorrectAttribute()
		{
			TypesShouldBeInNamespacesRule rule = new TypesShouldBeInNamespacesRule();
			Assertion.AssertNotNull(rule.Check(typeof(OutsideNamespace)));
		}
	}
}
#endif
#endregion
