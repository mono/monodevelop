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
	/// Description of StreamSuffixIsStreamRule.	
	/// </summary>
	public class StreamSuffixIsStreamRule : AbstractReflectionRule, ITypeRule
	{
		public override string Description {
			get {
				// FIXME: I18N
				return "Stream names have the suffix 'Stream'";
			}
		}
		
		public override string Details {
			get {
				// FIXME: I18N
				return "A type that extends <code><a href='help://types/System.IO.Stream'>Stream</a></code> is a stream and its name should always end with <i>Stream</i> like in <code>FileStream</code>.";
			}
		}
		
		public StreamSuffixIsStreamRule()
		{
			base.certainty = 99;
		}
		
		public Resolution Check(Type type)
		{
			if (typeof(System.IO.Stream).IsAssignableFrom(type) && !type.Name.EndsWith("Stream")) {
				// FIXME: I18N
				return new Resolution (this, String.Format ("Change the name of the type <code>{0}</code> so that it ends with <I>Stream</I>.", type.FullName), type.FullName);
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
	public class StreamSuffixIsStreamRuleTest
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
			StreamSuffixIsStreamRule rule = new StreamSuffixIsStreamRule();
			Assertion.AssertNull(rule.Check(typeof(MyOtherClass)));
			Assertion.AssertNull(rule.Check(typeof(RealStream)));
		}
		
		class WrongStrm : System.IO.FileStream  
		{
			public WrongStrm(string path,FileMode mode) : base(path, mode)
			{}
		}
		[Test]
		public void TestIncorrectStream()
		{
			StreamSuffixIsStreamRule rule = new StreamSuffixIsStreamRule();
			Assertion.AssertNotNull(rule.Check(typeof(WrongStrm)));
		}
	}
}
#endif
#endregion
