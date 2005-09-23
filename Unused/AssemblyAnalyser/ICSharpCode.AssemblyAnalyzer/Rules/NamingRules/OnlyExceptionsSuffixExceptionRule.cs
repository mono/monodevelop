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
	/// Description of OnlyExceptionsSuffixExceptionRule.	
	/// </summary>
	public class OnlyExceptionsSuffixExceptionRule : AbstractReflectionRule, ITypeRule
	{
		public override string Description {
			get {
				// FIXME: I18N
				return "Only exception names have the suffix 'Exception'";
			}
		}
		
		public override string Details {
			get {
				// FIXME: I18N
				return "A type that does not extend from <code><a href='help://types/System.Exception'>Exception</a></code> should never have the suffix <i>Exception</i>.";
			}
		}
		
		public OnlyExceptionsSuffixExceptionRule()
		{
			base.certainty = 99;
			base.priorityLevel = PriorityLevel.CriticalError;
		}
		
		
		public Resolution Check(Type type)
		{
			if (!typeof(System.Exception).IsAssignableFrom(type) && type.Name.EndsWith("Exception")) {
				// FIXME: I18N
				return new Resolution (this, String.Format ("Change the name of the type <code>{0}</code> so that it doesn't end with <i>Exception</i>.", type.FullName), type.FullName);
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
	public class OnlyExceptionsSuffixExceptionRuleTest
	{
		class MyException : System.Exception
		{
		}
		class OtherClass
		{}
		[Test]
		public void TestCorrectException()
		{
			OnlyExceptionsSuffixExceptionRule rule = new OnlyExceptionsSuffixExceptionRule();
			Assertion.AssertNull(rule.Check(typeof(MyException)));
			Assertion.AssertNull(rule.Check(typeof(OtherClass)));
		}
		
		class NotAnException
		{
		}
		[Test]
		public void TestIncorrectException()
		{
			OnlyExceptionsSuffixExceptionRule rule = new OnlyExceptionsSuffixExceptionRule();
			Assertion.AssertNotNull(rule.Check(typeof(NotAnException)));
		}
	}
}
#endif
#endregion
