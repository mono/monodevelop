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
	/// Description of EventsHaveTwoParameters.	
	/// </summary>
	public class EventsHaveTwoParametersRule : AbstractReflectionRule, IEventRule
	{
		public override string Description {
			get {
				// FIXME: I18N
				return "Events have two parameters";
			}
		}
		
		public override string Details {
			get {
				// FIXME: I18N
				return "As a convention in .NET events have two parameters a sender and an event data object. <BR>For example: <code>void MouseEventHandler(object sender, MouseEventArgs e);</code>";
			}
		}
		
		public EventsHaveTwoParametersRule()
		{
			base.certainty = 99;
		}
		
		public Resolution Check(EventInfo evnt)
		{
			MethodInfo invokeMethod = evnt.EventHandlerType.GetMethod("Invoke");
			ParameterInfo[] parameters = invokeMethod.GetParameters();

			if (parameters.Length != 2) {
				// FIXME: I18N
				return new Resolution (this, String.Format ("Change <code>{0}</code> so that it has only two parameters. A sender and an event data object.", evnt.EventHandlerType.FullName), evnt.EventHandlerType.FullName);
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
	public class EventsHaveTwoParametersTest
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
			EventsHaveTwoParametersRule rule = new EventsHaveTwoParametersRule();
			Assertion.AssertNull(rule.Check(this.GetType().GetEvent("CorrectEvent")));
		}
		
		public delegate void IncorrectEventHandler(object sender, EventArgs e, int i);
		public event IncorrectEventHandler IncorrectEvent;
		protected virtual void OnIncorrectEvent(EventArgs e)
		{
			if (IncorrectEvent != null) {
				IncorrectEvent(this, e, 5);
			}
		}
		
		[Test]
		public void TestIncorrectEventHandler()
		{
			EventsHaveTwoParametersRule rule = new EventsHaveTwoParametersRule();
			Assertion.AssertNotNull(rule.Check(this.GetType().GetEvent("IncorrectEvent")));
		}
	}
}
#endif
#endregion
