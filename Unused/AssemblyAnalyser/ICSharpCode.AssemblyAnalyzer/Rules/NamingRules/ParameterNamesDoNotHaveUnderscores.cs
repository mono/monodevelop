// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krueger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.Reflection;
using System.Collections;

namespace ICSharpCode.AssemblyAnalyser.Rules
{
	/// <summary>
	/// Description of ParameterNamesDoNotHaveUnderscores.	
	/// </summary>
	public class ParameterNamesDoNotHaveUnderscores : AbstractReflectionRule, IParameterRule
	{
		public override string Description {
			get {
				// FIXME: I18N
				return "Parameter names do not contain underscores '_'";
			}
		}
		
		public override string Details {
			get {
				// FIXME: I18N
				return "Underscores should never be used inside parameter names.";
			}
		}
		
		public ParameterNamesDoNotHaveUnderscores()
		{
			base.certainty = 99;
		}
		
		public Resolution Check(Module module, ParameterInfo param)
		{
			if (param.Name != null && param.Name.IndexOf('_') >= 0) {
				string memberName = NamingUtilities.Combine(param.Member.DeclaringType.FullName, param.Member.Name);
				// FIXME: I18N
				return new Resolution (this, String.Format ("Remove all underscores in parameter <code>{0}</code> inside member <code>{1}</code>.", param.Name, memberName), memberName);
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
	public class ParameterNamesDoNotHaveUnderscoresTest
	{
		public class A {
			public void TestMethod1(int right)
			{
			}
			public void TestMethod2(int a, int b, int c, int d)
			{
			}
			public void TestMethod3(int wrong_)
			{
			}
			public void TestMethod4(int _a, int b_c, int ____, int wrong_)
			{
			}
			public static void TestMethod(MethodInfo methodInfo, bool isNull)
			{
				ParameterNamesDoNotHaveUnderscores parameterNamesDoNotHaveUnderscores = new ParameterNamesDoNotHaveUnderscores();
				foreach (ParameterInfo parameter in methodInfo.GetParameters()) {
					if (isNull) {
						Assertion.AssertNull(parameterNamesDoNotHaveUnderscores.Check(null, parameter));
					} else {
						Assertion.AssertNotNull(parameterNamesDoNotHaveUnderscores.Check(null, parameter));
					}
				}
			}
		}
		
		
		[Test]
		public void TestCorrectParameters()
		{
			A.TestMethod(typeof(A).GetMethod("TestMethod1"), true);
			A.TestMethod(typeof(A).GetMethod("TestMethod2"), true);
		}
		
		[Test]
		public void TestIncorrectParameters()
		{
			A.TestMethod(typeof(A).GetMethod("TestMethod3"), false);
			A.TestMethod(typeof(A).GetMethod("TestMethod4"), false);
		}
	}
}
#endif
#endregion
