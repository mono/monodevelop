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
	/// Description of ExceptionSuffixIsException.	
	/// </summary>
	public class ExceptionSuffixIsExceptionRule : AbstractReflectionRule, ITypeRule
	{
		public override string Description {
			get {
				// FIXME: I18N
				return "Exception names have the suffix 'Exception'";
			}
		}
		
		public override string Details {
			get {
				// FIXME: I18N
				return "A type that extends <code><a href='help://types/System.Exception'>Exception</a></code> is an exception and should always use the suffix <i>Exception</i> like in <code>ArgumentNullException</code>.";
			}
		}
		
		public ExceptionSuffixIsExceptionRule()
		{
			base.certainty = 99;
		}
		
		public Resolution Check(Type type)
		{
			if (typeof(System.Exception).IsAssignableFrom(type) && !type.Name.EndsWith("Exception")) {
				// FIXME: I18M
				return new Resolution (this, String.Format ("Change the name of the type <code>{0}</code> so that it ends with <i>Exception</i>.", type.FullName), type.FullName);
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
	public class ExceptionSuffixIsExceptionRuleTest
	{
		class MyException : System.Exception
		{
		}
		[Test]
		public void TestCorrectException()
		{
			ExceptionSuffixIsExceptionRule rule = new ExceptionSuffixIsExceptionRule();
			Assertion.AssertNull(rule.Check(typeof(MyException)));
		}
		
		class MyExcpt : System.Exception
		{
		}
		[Test]
		public void TestIncorrectException()
		{
			ExceptionSuffixIsExceptionRule rule = new ExceptionSuffixIsExceptionRule();
			Assertion.AssertNotNull(rule.Check(typeof(MyExcpt)));
		}
	}
}
#endif
#endregion
