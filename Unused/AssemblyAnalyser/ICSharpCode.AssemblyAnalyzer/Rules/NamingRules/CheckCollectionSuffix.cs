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
	/// Description of CheckCollectionSuffix.	
	/// </summary>
	public class CheckCollectionSuffix : AbstractReflectionRule, ITypeRule
	{
		public override string Description {
			get {
				// FIXME: I18N
				return "Collections names have the suffix 'Collection', 'Queue' or 'Stack'";
			}
		}
		
		public override string Details {
			get {
				// FIXME: I18N
				return "A type that implements <code><a href='help://types/System.Collections.ICollection'>ICollection</a></code> or <code><a href='help://types/System.Collections.IEnumerable'>IEnumerable</a></code> is a collection and its name should use the suffix <i>Collection</i>.<BR>An exception to this rule are queues (extend <code><a href='help://types/System.Collections.Queue'>Queue</a></code>) which should use the suffix <i>Queue</i> and stacks (extend <code><a href='help://types/System.Collections.Stack'>Stack</a></code>) that should use the <i>Stack</i> suffix. <BR>For example: <code>StringCollection</code>, <code>StateStack</code> or <code>EventQueue</code> are valid names.";
			}
		}
		
		public CheckCollectionSuffix()
		{
			base.certainty = 90;
		}
		
		public Resolution Check(Type type)
		{
			if ((typeof(ICollection).IsAssignableFrom(type) || typeof(IEnumerable).IsAssignableFrom(type)) && !typeof(System.Collections.IDictionary).IsAssignableFrom(type)) {
				if (typeof(Queue).IsAssignableFrom(type)) {
					if (!type.Name.EndsWith("Queue")) {
						// FIXME: I18N
						return new Resolution (this, String.Format ("Change the name of the queue <code>{0}</code> so that it ends with <i>Queue</i>.", type.FullName), type.FullName);
					}
				} else if (typeof(Stack).IsAssignableFrom(type)) {
					if (!type.Name.EndsWith("Stack")) {
						// FIXME: I18N
						return new Resolution (this, String.Format ("Change the name of the queue <code>{0}</code> so that it ends with <i>Stack</i>.", type.FullName), type.FullName);
					}
				} else {
					if (!type.Name.EndsWith("Collection")) {
						// FIXME: I18N
						return new Resolution (this, String.Format ("Change the name of the queue <code>{0}</code> so that it ends with <i>Collection</i>.", type.FullName), type.FullName);
					}
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
	public class CheckCollectionSuffixTest
	{
		#region Collection suffix tests
		class MyCollection : System.Collections.ArrayList
		{
		}
		class MyDictionary : System.Collections.Hashtable
		{
		}
		[Test]
		public void TestCorrectCollection()
		{
			CheckCollectionSuffix checkCollectionSuffix = new CheckCollectionSuffix();
			Assertion.AssertNull(checkCollectionSuffix.Check(typeof(MyCollection)));
			Assertion.AssertNull(checkCollectionSuffix.Check(typeof(MyDictionary)));
			Assertion.AssertNull(checkCollectionSuffix.Check(typeof(CheckCollectionSuffixTest)));
		}
		
		class MyColl : System.Collections.ArrayList
		{
		}
		[Test]
		public void TestIncorrectCollection()
		{
			CheckCollectionSuffix checkCollectionSuffix = new CheckCollectionSuffix();
			Assertion.AssertNotNull(checkCollectionSuffix.Check(typeof(MyColl)));
		}
		#endregion
		
		#region Queue suffix tests
		class MyQueue : System.Collections.Queue
		{
		}
		[Test]
		public void TestCorrectQueue()
		{
			CheckCollectionSuffix checkCollectionSuffix = new CheckCollectionSuffix();
			Assertion.AssertNull(checkCollectionSuffix.Check(typeof(MyQueue)));
		}
		
		class MyQWEQWEQ : System.Collections.Queue
		{
		}
		[Test]
		public void TestIncorrectQueue()
		{
			CheckCollectionSuffix checkCollectionSuffix = new CheckCollectionSuffix();
			Assertion.AssertNotNull(checkCollectionSuffix.Check(typeof(MyQWEQWEQ)));
		}
		#endregion 
		
		#region Stack suffix tests
		class MyStack : System.Collections.Stack
		{
		}
		[Test]
		public void TestCorrectStack()
		{
			CheckCollectionSuffix checkCollectionSuffix = new CheckCollectionSuffix();
			Assertion.AssertNull(checkCollectionSuffix.Check(typeof(MyStack)));
		}
		class MySfwefew : System.Collections.Stack
		{
		}
		[Test]
		public void TestIncorrectStack()
		{
			CheckCollectionSuffix checkCollectionSuffix = new CheckCollectionSuffix();
			Assertion.AssertNotNull(checkCollectionSuffix.Check(typeof(MySfwefew)));
		}
		#endregion
	}
}
#endif
#endregion
