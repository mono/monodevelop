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
	public class EventFirstParameterNameIsSender : AbstractReflectionRule, IEventRule
	{
		public override string Description {
			get {
				// FIXME: I18N
				return "First parameter in events is named 'sender'";
			}
		}
		
		public override string Details {
			get {
				// FIXME: I18N
				return "As a convention in .NET events have two parameters a sender which must be called <i>sender</i> and an event data object. The sender must always be from the type <code><a href='help://types/System.Object'>object</a></code> and never a specialized type.<BR>For example: <code>void MouseEventHandler(object sender, MouseEventArgs e);</code>";
			}
		}
		
		public EventFirstParameterNameIsSender()
		{
			base.certainty = 99;
		}
		
		public Resolution Check(EventInfo evnt)
		{
			MethodInfo invokeMethod = evnt.EventHandlerType.GetMethod("Invoke");
			ParameterInfo[] parameters = invokeMethod.GetParameters();

			if (parameters.Length > 0 && parameters[0].Name != "sender") {
				// FIXME: I18N
				return new Resolution (this, String.Format ("Rename the first parameter name of the event <code>{0}</code> from <i>{1}</i> to <i>sender</i>.", evnt.EventHandlerType.FullName, parameters[0].Name), evnt.EventHandlerType.FullName);
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
	public class EventFirstParameterNameIsSenderTest
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
			EventFirstParameterNameIsSender eventFirstParameterNameIsSender = new EventFirstParameterNameIsSender();
			Assertion.AssertNull(eventFirstParameterNameIsSender.Check(this.GetType().GetEvent("CorrectEvent")));
		}
		
		public delegate void IncorrectEventHandler(object s, EventArgs e);
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
			EventFirstParameterNameIsSender eventFirstParameterNameIsSender = new EventFirstParameterNameIsSender();
			Assertion.AssertNotNull(eventFirstParameterNameIsSender.Check(this.GetType().GetEvent("IncorrectEvent")));
		}
	}
}
#endif
#endregion
