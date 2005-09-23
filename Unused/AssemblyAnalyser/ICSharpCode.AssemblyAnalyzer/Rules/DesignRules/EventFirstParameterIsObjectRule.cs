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
	/// Description of EventFirstParameterIsObject.	
	/// </summary>
	public class EventFirstParameterIsObjectRule : AbstractReflectionRule, IEventRule
	{
		public override string Description {
			get {
				// FIXME: I18N
				return "The first parameter of an event is from type System.Object";
			}
		}
		
		public override string Details {
			get {
				// FIXME: I18N
				return "As a convention in .NET events have two parameters a sender and an event data object. The sender must always be from the type <code><a href='help://types/System.Object'>object</a></code> and never a specialized type.<BR>For example: <code>void MouseEventHandler(object sender, MouseEventArgs e);</code>";
			}
		}
		
		public EventFirstParameterIsObjectRule()
		{
			base.certainty = 99;
		}
		
		public Resolution Check(EventInfo evnt)
		{
			MethodInfo invokeMethod = evnt.EventHandlerType.GetMethod("Invoke");
			ParameterInfo[] parameters = invokeMethod.GetParameters();

			if (parameters.Length > 0 && parameters[0].ParameterType != typeof(System.Object)) {
				// FIXME: I18N
				return new Resolution (this, String.Format ("Change the first parameter of <code>{0}</code> from the type <code>{1}</code> to the type <code><a href='help://types/System.Object'>object</a></code>.", evnt.EventHandlerType.FullName, parameters[0].ParameterType.FullName), evnt.EventHandlerType.FullName);
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
	public class EventFirstParameterIsObjectRuleTest
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
			EventFirstParameterIsObjectRule rule = new EventFirstParameterIsObjectRule();
			Assertion.AssertNull(rule.Check(this.GetType().GetEvent("CorrectEvent")));
		}
		
		public delegate void IncorrectEventHandler(int sender, EventArgs e);
		public event IncorrectEventHandler IncorrectEvent;
		protected virtual void OnIncorrectEvent(EventArgs e)
		{
			if (IncorrectEvent != null) {
				IncorrectEvent(6, e);
			}
		}
		
		[Test]
		public void TestIncorrectEventHandler()
		{
			EventFirstParameterIsObjectRule rule = new EventFirstParameterIsObjectRule();
			Assertion.AssertNotNull(rule.Check(this.GetType().GetEvent("IncorrectEvent")));
		}
	}
}
#endif
#endregion
