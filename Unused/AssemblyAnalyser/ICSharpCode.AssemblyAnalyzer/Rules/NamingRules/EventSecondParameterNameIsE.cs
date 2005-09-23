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
	/// Description of EventSecondParameterNameIsE	
	/// </summary>
	public class EventSecondParameterNameIsE : AbstractReflectionRule, IEventRule
	{
		public override string Description {
			get {
				// FIXME: I18N
				return "Second parameter in events is named 'e'";
			}
		}
		
		public override string Details {
			get {
				// FIXME: I18N
				return "As a convention in .NET events have two parameters a sender and an event data object which is called <i>e</i>. The event data object must be extend from the type <code><a href='help://types/System.EventArgs'>EventArgs</a></code>.<BR>For example: <code>void MouseEventHandler(object sender, MouseEventArgs e);</code>";
			}
		}
		
		public EventSecondParameterNameIsE()
		{
			base.certainty = 99;
		}
		
		public Resolution Check(EventInfo evnt)
		{
			MethodInfo invokeMethod = evnt.EventHandlerType.GetMethod("Invoke");
			ParameterInfo[] parameters = invokeMethod.GetParameters();

			if (parameters.Length > 1 && parameters[1].Name != "e") {
				// FIXME: I18N
				return new Resolution (this, String.Format ("Rename the second parameter name in the event <code>{0}</code> from <i>{1}</i> to <i>e</i>.", evnt.EventHandlerType.FullName, parameters[1].Name), evnt.EventHandlerType.FullName);
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
	public class EventSecondParameterNameIsETest
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
			EventSecondParameterNameIsE eventSecondParameterNameIsE = new EventSecondParameterNameIsE();
			Assertion.AssertNull(eventSecondParameterNameIsE.Check(this.GetType().GetEvent("CorrectEvent")));
		}
		
		public delegate void IncorrectEventHandler(object sender, EventArgs notE);
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
			EventSecondParameterNameIsE eventSecondParameterNameIsE = new EventSecondParameterNameIsE();
			Assertion.AssertNotNull(eventSecondParameterNameIsE.Check(this.GetType().GetEvent("IncorrectEvent")));
		}
	}
}
#endif
#endregion
