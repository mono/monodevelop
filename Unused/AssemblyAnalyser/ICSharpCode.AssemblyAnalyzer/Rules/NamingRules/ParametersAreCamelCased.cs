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
	/// Description of ParametersArePascalCased.	
	/// </summary>
	public class ParametersAreCamelCased : AbstractReflectionRule, IParameterRule
	{
		public override string Description {
			get {
				// FIXME: I18N
				return "Parameter names should be camel cased";
			}
		}
		
		public override string Details {
			get {
				// FIXME: I18N
				return "Camel casing lowercase the first letter like in <code><b>m</b>ousePosition</code> but the starting letters of all subsequent words are capitalized. Use camel casing for all parameters.";
			}
		}
		
		public ParametersAreCamelCased()
		{
			base.certainty = 99;
		}
		
		public Resolution Check(Module module, ParameterInfo param)
		{
			if (!NamingUtilities.IsCamelCase(param.Name)) {
				string memberName = NamingUtilities.Combine(param.Member.DeclaringType.FullName, param.Member.Name);
				// FIXME: I18N
				return new Resolution (this, String.Format ("Use camel casing for the parameter <code>{0}</code> inside member <code>{1}</code>.<BR>For example: <code>{2}</code>.", param.Name, memberName, NamingUtilities.CamelCase (param.Name)), memberName);
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
	public class ParametersAreCamelCasedTest
	{
		public class A {
			public void TestMethod1(int right)
			{
			}
			public void TestMethod2(int a, int b, int c, int d)
			{
			}
			public void TestMethod3(int Wrong)
			{
			}
			public void TestMethod4(int A, int B, int C, int D)
			{
			}
			public static void TestMethod(MethodInfo methodInfo, bool isNull)
			{
				ParametersAreCamelCased parametersAreCamelCased = new ParametersAreCamelCased();
				foreach (ParameterInfo parameter in methodInfo.GetParameters()) {
					if (isNull) {
						Assertion.AssertNull(parametersAreCamelCased.Check(null, parameter));
					} else {
						Assertion.AssertNotNull(parametersAreCamelCased.Check(null, parameter));
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
