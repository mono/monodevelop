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
	/// Description of EventFirstParameterNameIsSender.	
	/// </summary>
	public class EventHandlersReturnVoidRule : AbstractReflectionRule, IEventRule
	{
		public override string Description {
			get {
				// FIXME: I18N
				return "Event handlers return void";
			}
		}
		
		public override string Details {
			get {
				// FIXME: I18N
				return "Event handlers return <code>void</code> because they can send event to multiple target methods. Any return value would get lost.";
			}
		}
		
		public EventHandlersReturnVoidRule()
		{
			base.certainty = 99;
		}
		
		public Resolution Check(EventInfo evnt)
		{
			MethodInfo invokeMethod = evnt.EventHandlerType.GetMethod("Invoke");
			if (invokeMethod.ReturnType != typeof(void)) {
				// FIXME: I18N
				return new Resolution (this, String.Format ("Change <code>{0}</code> so that it returns <code>void</code> instead of <code>{1}</code>.", evnt.EventHandlerType.FullName, invokeMethod.ReturnType.FullName), evnt.EventHandlerType.FullName);
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
	public class EventHandlersReturnVoidRuleTest
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
			EventHandlersReturnVoidRule rule = new EventHandlersReturnVoidRule();
			Assertion.AssertNull(rule.Check(this.GetType().GetEvent("CorrectEvent")));
		}
		
		public delegate int IncorrectEventHandler(object sender, EventArgs e);
		public event IncorrectEventHandler IncorrectEvent;
		protected virtual void OnIncorrectEvent(EventArgs e)
		{
			if (IncorrectEvent != null) {
				IncorrectEvent(this, e);
			}
		}
		
		[Test]
		public void TestIncorrectEventHandler()
		{
			EventHandlersReturnVoidRule rule = new EventHandlersReturnVoidRule();
			Assertion.AssertNotNull(rule.Check(this.GetType().GetEvent("IncorrectEvent")));
		}
	}
}
#endif
#endregion
