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
	/// Description of OnlyEventArgsSuffixEventArgsRule.	
	/// </summary>
	public class OnlyEventArgsSuffixEventArgsRule : AbstractReflectionRule, ITypeRule
	{
		public override string Description {
			get {
				// FIXME: I18N
				return "Only event argument names have the suffix 'EventArgs'";
			}
		}
		
		public override string Details {
			get {
				// FIXME: I18N
				return "A type that does not extend from <code><a href='help://types/System.EventArgs'>EventArgs</a></code> should never have the suffix <i>EventArgs</i>.";
			}
		}
		
		public OnlyEventArgsSuffixEventArgsRule()
		{
			base.certainty = 99;
			base.priorityLevel = PriorityLevel.CriticalError;
		}
		
		public Resolution Check(Type type)
		{
			if (!typeof(System.EventArgs).IsAssignableFrom(type) && type.Name.EndsWith("EventArgs")) {
				// FIXME: I18N
				return new Resolution (this, String.Format ("Change the name of the type <code>{0}</code> so that it does not end with <i>EventArgs</i>.", type.FullName), type.FullName);
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
	public class OnlyEventArgsSuffixEventArgsRuleTest
	{
		class CorrectEventArgs : System.EventArgs
		{
		}
		class OtherClass
		{
		}
		class MyEventArgs : CorrectEventArgs
		{
		}
		[Test]
		public void TestCorrectEventArgs()
		{
			OnlyEventArgsSuffixEventArgsRule rule = new OnlyEventArgsSuffixEventArgsRule();
			Assertion.AssertNull(rule.Check(typeof(CorrectEventArgs)));
			Assertion.AssertNull(rule.Check(typeof(OtherClass)));
			Assertion.AssertNull(rule.Check(typeof(MyEventArgs)));
		}
		
		class IncorrectEventArgs
		{
		}
		[Test]
		public void TestIncorrectEventArgs()
		{
			OnlyEventArgsSuffixEventArgsRule rule = new OnlyEventArgsSuffixEventArgsRule();
			Assertion.AssertNotNull(rule.Check(typeof(IncorrectEventArgs)));
		}
		
	}
}
#endif
#endregion
