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
	/// Description of OnlyStreamsSuffixStreamRule.	
	/// </summary>
	public class OnlyStreamsSuffixStreamRule : AbstractReflectionRule, ITypeRule
	{
		public override string Description {
			get {
				// FIXME: I18N
				return "Only stream names have the suffix 'Stream'";
			}
		}
		
		public override string Details {
			get {
				// FIXME: I18N
				return "A type that does not extend from <code><a href='help://types/System.IO.Stream'>Stream</a></code> should never have the suffix <i>Stream</i>.";
			}
		}
		
		public OnlyStreamsSuffixStreamRule()
		{
			base.certainty = 99;
			base.priorityLevel = PriorityLevel.CriticalError;
		}
		
		
		public Resolution Check(Type type)
		{
			if (!typeof(System.IO.Stream).IsAssignableFrom(type) && type.Name.EndsWith("Stream")) {
				// FIXME: I18N
				return new Resolution (this, String.Format ("Change the name of the type <code>{0}</code> so that it does not end with <i>Stream</i>.", type.FullName), type.FullName);
			}
			return null;
		}
	}
}
#region Unit Test
#if TEST
namespace ICSharpCode.AssemblyAnalyser.Rules
{
	using System.IO;
	using NUnit.Framework;

	[TestFixture]
	public class OnlyStreamsSuffixStreamRuleTest
	{
		class MyOtherClass
		{
		}
		class RealStream : System.IO.FileStream  
		{
			public RealStream(string path,FileMode mode) : base(path, mode)
			{}
		}
		[Test]
		public void TestCorrectStream()
		{
			OnlyStreamsSuffixStreamRule rule = new OnlyStreamsSuffixStreamRule();
			Assertion.AssertNull(rule.Check(typeof(MyOtherClass)));
			Assertion.AssertNull(rule.Check(typeof(RealStream)));
		}
		
		class MyStream
		{
		}
		[Test]
		public void TestIncorrectStream()
		{
			OnlyStreamsSuffixStreamRule rule = new OnlyStreamsSuffixStreamRule();
			Assertion.AssertNotNull(rule.Check(typeof(MyStream)));
		}
	}
}
#endif
#endregion
