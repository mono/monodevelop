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
	/// Description of EventsDoNotHaveBeforeOrAfterPrefix.	
	/// </summary>
	public class EventsDoNotHaveBeforeOrAfterPrefix : AbstractReflectionRule, IEventRule
	{
		public override string Description {
			get {
				// FIXME: I18N
				return "Event names do not have a 'Before' or 'After' prefix";
			}
		}
		
		public override string Details {
			get {
				// FIXME: I18N
				return "Use present or past tense for pre/post events instead using <i>Before</i> or <i>After</i> prefix.";
			}
		}
		
		public EventsDoNotHaveBeforeOrAfterPrefix()
		{
			base.certainty = 90;
		}
		
		public Resolution Check(EventInfo evnt)
		{
			if (evnt.Name.StartsWith("Before")) {
				// FIXME: I18N
				return new Resolution (this, String.Format ("Change event name <code>{0}</code> in <code>{1}</code> so that it does not use the <i>Before</i> prefix.", evnt.Name, evnt.ReflectedType.FullName), NamingUtilities.Combine (evnt.ReflectedType.FullName, evnt.Name));
			} else if (evnt.Name.StartsWith("After")) {
				// FIXME: I18N
				return new Resolution (this, String.Format ("Change event name <code>{0}</code> in <code>{1}</code> so that it does not use the <i>After</i> prefix.", evnt.Name, evnt.ReflectedType.FullName), NamingUtilities.Combine (evnt.ReflectedType.FullName, evnt.Name));
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
	public class EventHandlerSuffixIsEventHandlerTest
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
			EventHandlerSuffixIsEventHandler eventHandlerSuffixIsEventHandler = new EventHandlerSuffixIsEventHandler();
			Assertion.AssertNull(eventHandlerSuffixIsEventHandler.Check(this.GetType().GetEvent("CorrectEvent")));
		}
		
		public delegate void IncorrectEventHandlerWithWrongSuffix(object sender, EventArgs e);
		public event IncorrectEventHandlerWithWrongSuffix IncorrectEvent;
		protected virtual void OnIncorrectEvent(EventArgs e)
		{
			if (IncorrectEvent != null) {
				IncorrectEvent(this, e);
			}
		}
		
		[Test]
		public void TestIncorrectEventHandler()
		{
			EventHandlerSuffixIsEventHandler eventHandlerSuffixIsEventHandler = new EventHandlerSuffixIsEventHandler();
			EventInfo evnt = this.GetType().GetEvent("IncorrectEvent");
			Assertion.AssertNotNull("Type name is >" + evnt.EventHandlerType.FullName + "<", eventHandlerSuffixIsEventHandler.Check(evnt));
		}
	}
}
#endif
#endregion
