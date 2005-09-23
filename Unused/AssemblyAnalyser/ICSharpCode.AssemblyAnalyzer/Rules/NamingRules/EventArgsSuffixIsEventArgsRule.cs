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
	/// Description of EventArgsSuffixIsEventArgs
	/// </summary>
	public class EventArgsSuffixIsEventArgsRule : AbstractReflectionRule, ITypeRule
	{
		public override string Description {
			get {
				// FIXME: I18N
				return "EventArgs names have the suffix 'EventArgs'";
			}
		}
		
		public override string Details {
			get {
				// FIXME: I18N
				return "A type that extends <code><a href='help://types/System.EventArgs'>EventArgs</a></code> is an event argument and should use the suffix <i>EventArgs</i> like in <code>MouseEventArgs</code>.";
			}
		}
		
		public Resolution Check(Type type)
		{
			if (typeof(System.EventArgs).IsAssignableFrom(type) && !type.Name.EndsWith("EventArgs")) {
				// FIXME: I18N
				return new Resolution (this, String.Format ("Rename the event argument <code>{0}</code> so that it ends with <i>EventArgs</i>.", type.FullName), type.FullName);
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
	public class EventArgsSuffixIsEventArgsRuleTest
	{
		class CorrectEventArgs : System.EventArgs
		{
		}
		[Test]
		public void TestCorrectEventArgs()
		{
			EventArgsSuffixIsEventArgsRule rule = new EventArgsSuffixIsEventArgsRule();
			Assertion.AssertNull(rule.Check(typeof(CorrectEventArgs)));
		}
		
		class IncorrectEventArgsWithWrongSuffix : System.EventArgs
		{
		}
		[Test]
		public void TestIncorrectEventArgs()
		{
			EventArgsSuffixIsEventArgsRule rule = new EventArgsSuffixIsEventArgsRule();
			Assertion.AssertNotNull(rule.Check(typeof(IncorrectEventArgsWithWrongSuffix)));
		}
		
	}
}
#endif
#endregion
