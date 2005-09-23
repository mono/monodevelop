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
	/// Description of EventSecondParameterIsEventArgsRule.	
	/// </summary>
	public class EventSecondParameterIsEventArgsRule : AbstractReflectionRule, IEventRule
	{
		public override string Description {
			get {
				// FIXME: I18N
				return "Second parameter type in events is a System.EventArgs type";
			}
		}
		
		public override string Details {
			get {
				// FIXME: I18N
				return "As a convention in .NET events have two parameters a sender and an event data object. The event data must always extend from the type <code><a href='help://types/System.EventArgs'>System.EventArgs</a></code> .<BR>For example: <code>void MouseEventHandler(object sender, MouseEventArgs e);</code> where <code>MouseEventArgs</code> extends <code><a href='help://types/System.EventArgs'>System.EventArgs</a></code>.";
			}
		}
		
		public EventSecondParameterIsEventArgsRule()
		{
			base.certainty = 99;
		}
		
		public Resolution Check(EventInfo evnt)
		{
			MethodInfo invokeMethod = evnt.EventHandlerType.GetMethod("Invoke");
			ParameterInfo[] parameters = invokeMethod.GetParameters();

			if (parameters.Length > 1 && !typeof(System.EventArgs).IsAssignableFrom(parameters[1].ParameterType)) {
				// FIXME: I18N
				return new Resolution (this, String.Format ("Change the second parameter of the event <code>{0}</code> from <code>{1}</code> so that it is from the type <code><a href='help://types/System.EventArgs'>EventArgs</a></code> or any more specialized type.", evnt.EventHandlerType.FullName, parameters[1].ParameterType.FullName), evnt.EventHandlerType.FullName);
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
	public class EventSecondParameterIsEventArgsRuleTest
	{
		public delegate void CorrectEventHandler(object sender, EventArgs e);
		public event CorrectEventHandler CorrectEvent;
		protected virtual void OnCorrectEvent(EventArgs e)
		{
			if (CorrectEvent != null) {
				CorrectEvent(this, e);
			}
		}
		[Test]
		public void TestCorrectEventHandler()
		{
			EventSecondParameterIsEventArgsRule rule = new EventSecondParameterIsEventArgsRule();
			Assertion.AssertNull(rule.Check(this.GetType().GetEvent("CorrectEvent")));
		}
		
		public delegate void IncorrectEventHandler(object sender, int e);
		public event IncorrectEventHandler IncorrectEvent;
		protected virtual void OnIncorrectEvent(int e)
		{
			if (IncorrectEvent != null) {
				IncorrectEvent(this, e);
			}
		}
		
		[Test]
		public void TestIncorrectEventHandler()
		{
			EventSecondParameterIsEventArgsRule rule = new EventSecondParameterIsEventArgsRule();
			Assertion.AssertNotNull(rule.Check(this.GetType().GetEvent("IncorrectEvent")));
		}
	}
}
#endif
#endregion
