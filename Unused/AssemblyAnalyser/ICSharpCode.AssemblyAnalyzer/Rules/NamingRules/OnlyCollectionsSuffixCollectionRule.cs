// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krueger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.Collections;
using System.Reflection;

namespace ICSharpCode.AssemblyAnalyser.Rules
{
	/// <summary>
	/// Description of OnlyCollectionsSuffixCollectionRule.	
	/// </summary>
	public class OnlyCollectionsSuffixCollectionRule : AbstractReflectionRule, ITypeRule
	{
		public override string Description {
			get {
				// FIXME: I18N
				return "Only collection names should have the suffix 'Collection', 'Queue' or 'Stack'";
			}
		}
		
		public override string Details {
			get {
				// FIXME: I18N
				return "A type that does not implement <code><a href='help://types/System.Collections.ICollection'>ICollection</a></code> or <code><a href='help://types/System.Collections.IEnumerable'>IEnumerable</a></code> should never have the suffix <i>Collection</i>.<BR>A type that doesn't extend <code><a href='help://types/System.Collections.Queue'>Queue</a></code> should never have the suffix <i>Queue</i> and a type that doesn't extend <code><a href='help://types/System.Collections.Stack'>Stack</a></code> should never have a <i>Stack</i> suffix.";
			}
		}
		
		public OnlyCollectionsSuffixCollectionRule()
		{
			base.certainty = 99;
			base.priorityLevel = PriorityLevel.CriticalError;
		}
		
		public Resolution Check(Type type)
		{
			if (!typeof(ICollection).IsAssignableFrom(type) && !typeof(IEnumerable).IsAssignableFrom(type)) {
				// FIXME: I18N
				if (!typeof(Queue).IsAssignableFrom(type) && type.Name.EndsWith("Queue")) {
					return new Resolution (this, String.Format ("Change the name of <code>{0}</code> so that it does not end with <i>Queue</i>.", type.FullName), type.FullName);
				} else if (!typeof(Stack).IsAssignableFrom(type) && type.Name.EndsWith("Stack")) {
					return new Resolution (this, String.Format ("Change the name of <code>{0}</code> so that it does not end with <i>Stack</i>.", type.FullName), type.FullName);
				} else if (type.Name.EndsWith("Collection")) {
					return new Resolution (this, String.Format ("Change the name of <code>{0}</code> so that it does not end with <i>Collection</i>.", type.FullName), type.FullName);
				}
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
	public class OnlyCollectionsSuffixCollectionRuleTest
	{
		#region Collection suffix tests
		class MyCollection : System.Collections.ArrayList
		{
		}
		class OtherClass 
		{
		}
		[Test]
		public void TestCorrectCollection()
		{
			OnlyCollectionsSuffixCollectionRule rule = new OnlyCollectionsSuffixCollectionRule();
			Assertion.AssertNull(rule.Check(typeof(MyCollection)));
			Assertion.AssertNull(rule.Check(typeof(OtherClass)));
		}
		
		class My2Collection
		{
		}
		[Test]
		public void TestIncorrectCollection()
		{
			OnlyCollectionsSuffixCollectionRule rule = new OnlyCollectionsSuffixCollectionRule();
			Assertion.AssertNotNull(rule.Check(typeof(My2Collection)));
		}
		#endregion
		
		#region Queue suffix tests
		class MyQueue : System.Collections.Queue
		{
		}
		[Test]
		public void TestCorrectQueue()
		{
			OnlyCollectionsSuffixCollectionRule rule = new OnlyCollectionsSuffixCollectionRule();
			Assertion.AssertNull(rule.Check(typeof(MyQueue)));
		}
		
		class My2Queue
		{
		}
		[Test]
		public void TestIncorrectQueue()
		{
			OnlyCollectionsSuffixCollectionRule rule = new OnlyCollectionsSuffixCollectionRule();
			Assertion.AssertNotNull(rule.Check(typeof(My2Queue)));
		}
		#endregion 
		
		#region Stack suffix tests
		class MyStack : System.Collections.Stack
		{
		}
		[Test]
		public void TestCorrectStack()
		{
			OnlyCollectionsSuffixCollectionRule rule = new OnlyCollectionsSuffixCollectionRule();
			Assertion.AssertNull(rule.Check(typeof(MyStack)));
		}
		
		class My2Stack
		{
		}
		[Test]
		public void TestIncorrectStack()
		{
			OnlyCollectionsSuffixCollectionRule rule = new OnlyCollectionsSuffixCollectionRule();
			Assertion.AssertNotNull(rule.Check(typeof(My2Stack)));
		}
		#endregion
	}
}
#endif
#endregion
