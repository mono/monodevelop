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
	/// Description of InterfaceNotEmpty.	
	/// </summary>
	public class InterfaceNotEmptyRule : AbstractReflectionRule, ITypeRule
	{
		public override string Description {
			get {
				// FIXME: I18N
				return "Interfaces should not be empty";
			}
		}
		
		public override string Details {
			get {
				// FIXME: I18N
				return "Interfaces should specify behaviour. To mark classes use Attributes instead of empty interfaces.";
			}
		}
		public Resolution Check(Type type)
		{
			if (type.IsInterface && type.GetMembers().Length == 0) {
				// FIXME: I18N
				return new Resolution (this, String.Format ("Use a custom attribute to replace the empty interface <code>{0}</code>. Or specify behaviour for this interface.", type.FullName), type.FullName);
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
	public class InterfaceNotEmptyRuleTests
	{
		interface NonEmptyInterface1
		{
			void A();
		}
		interface NonEmptyInterface2
		{
			event EventHandler TestEvent;
		}
		interface NonEmptyInterface3
		{
			int MyProperty {
				get;
			}
		}
		[Test]
		public void TestNonEmptyInterface()
		{
			InterfaceNotEmptyRule rule = new InterfaceNotEmptyRule();
			Assertion.AssertNull(rule.Check(typeof(NonEmptyInterface1)));
			Assertion.AssertNull(rule.Check(typeof(NonEmptyInterface2)));
			Assertion.AssertNull(rule.Check(typeof(NonEmptyInterface3)));
		}
		
		interface EmptyInterface
		{
		}
		
		[Test]
		public void TestEmptyInterface()
		{
			InterfaceNotEmptyRule rule = new InterfaceNotEmptyRule();
			Assertion.AssertNotNull(rule.Check(typeof(EmptyInterface)));
		}
	}
}
#endif
#endregion
