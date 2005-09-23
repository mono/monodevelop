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
	/// Description of TypesHaveNoPublicInstanceFields
	/// </summary>
	public class TypesHaveNoPublicInstanceFields : AbstractReflectionRule, IFieldRule
	{
		public override string Description {
			get {
				// FIXME: I18N
				return "Types do not have externally visible instance fields";
			}
		}
		
		public override string Details {
			get {
				// FIXME: I18N
				return "Public or internal instance fields are a design flaw and should be avoided. Use properties instead. They are more flexible and hide the implementation details of the underlying data. Furthermore, properties do not have a performance penalty.";
			}
		}
		
		public TypesHaveNoPublicInstanceFields()
		{
			base.certainty = 90;
		}
		
		public Resolution Check(Module module, FieldInfo field)
		{
			if (!field.IsStatic && (field.IsPublic || field.IsAssembly)) {
				// FIXME: I18N
				return new Resolution (this, String.Format ("Make the field <code>{0}</code> in the type <code>{1}</code> private or protected. Provide a public or internal property if the field should be accessed from outside.", field.Name, field.DeclaringType.FullName), NamingUtilities.Combine (field.DeclaringType.FullName, field.Name));
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
	public class TypesHaveNoPublicInstanceFieldsTest
	{
		class A {
			int a;
			protected string b;
			public static int c = 12;
			public int AA {
				get {
					return a;
				}
				set {
					a = value;
				}
			}
			public string B {
				get {
					return b;
				}
				set {
					b = value;
				}
			}
		}
		[Test]
		public void TestCorrectFields()
		{
			TypesHaveNoPublicInstanceFields rule = new TypesHaveNoPublicInstanceFields();
			Assertion.AssertNull(rule.Check(null, typeof(A).GetField("a", BindingFlags.NonPublic | BindingFlags.Instance)));
			Assertion.AssertNull(rule.Check(null, typeof(A).GetField("b", BindingFlags.NonPublic | BindingFlags.Instance)));
			Assertion.AssertNull(rule.Check(null, typeof(A).GetField("c", BindingFlags.Public | BindingFlags.Static)));
		}
		
		class B {
			public int a = 5;
			internal string b ="";
		}
		
		[Test]
		public void TestIncorrectFields()
		{
			TypesHaveNoPublicInstanceFields rule = new TypesHaveNoPublicInstanceFields();
			Assertion.AssertNotNull(rule.Check(null, typeof(B).GetField("a", BindingFlags.Public | BindingFlags.Instance)));
			Assertion.AssertNotNull(rule.Check(null, typeof(B).GetField("b", BindingFlags.NonPublic | BindingFlags.Instance)));
		}
	}
}
#endif
#endregion
