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
	/// Description of EventHandlerSuffixIsEventHandler.	
	/// </summary>
	public class EventHandlerSuffixIsEventHandler : AbstractReflectionRule, IEventRule
	{
		public override string Description {
			get {
				// FIXME: I18N
				return "Event handler names have the suffix 'EventHandler'";
			}
		}
		
		public override string Details {
			get {
				// FIXME: I18N
				return "An event handler delegate name should use the suffix <i>EventHandler</i> like in <code>DragAndDropEventHandler</code>.";
			}
		}
		
		public Resolution Check(EventInfo evnt)
		{
			if (!evnt.EventHandlerType.Name.EndsWith("EventHandler")) {
				// FIXME: I18N
				return new Resolution (this, String.Format ("Change the name of the member <code>{0}</code> in the type <code>{1}</code> so that it does not end with <i>EventHandler</i>.", evnt.EventHandlerType.FullName, evnt.DeclaringType.FullName), evnt.EventHandlerType.FullName);
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
	public class EventsDoNotHaveBeforeOrAfterPrefixTest
	{
		public event EventHandler CorrectEvent;
		protected virtual void OnCorrectEvent(EventArgs e)
		{
			if (CorrectEvent != null) {
				CorrectEvent(this, e);
			}
		}
		[Test]
		public void TestCorrectEventHandler()
		{
			EventsDoNotHaveBeforeOrAfterPrefix eventsDoNotHaveBeforeOrAfterPrefix = new EventsDoNotHaveBeforeOrAfterPrefix();
			Assertion.AssertNull(eventsDoNotHaveBeforeOrAfterPrefix.Check(this.GetType().GetEvent("CorrectEvent")));
		}

		public event EventHandler BeforeIncorrectEvent;
		protected virtual void OnBeforeIncorrectEvent(EventArgs e)
		{
			if (BeforeIncorrectEvent != null) {
				BeforeIncorrectEvent(this, e);
			}
		}
		[Test]
		public void TestIncorrectEventHandler1()
		{
			EventsDoNotHaveBeforeOrAfterPrefix eventsDoNotHaveBeforeOrAfterPrefix = new EventsDoNotHaveBeforeOrAfterPrefix();
			Assertion.AssertNotNull(eventsDoNotHaveBeforeOrAfterPrefix.Check(this.GetType().GetEvent("BeforeIncorrectEvent")));
		}
		
		public event EventHandler AfterIncorrectEvent;
		protected virtual void OnAfterIncorrectEvent(EventArgs e)
		{
			if (AfterIncorrectEvent != null) {
				AfterIncorrectEvent(this, e);
			}
		}
		[Test]
		public void TestIncorrectEventHandler2()
		{
			EventsDoNotHaveBeforeOrAfterPrefix eventsDoNotHaveBeforeOrAfterPrefix = new EventsDoNotHaveBeforeOrAfterPrefix();
			Assertion.AssertNotNull(eventsDoNotHaveBeforeOrAfterPrefix.Check(this.GetType().GetEvent("AfterIncorrectEvent")));
		}
		
	}
}
#endif
#endregion
