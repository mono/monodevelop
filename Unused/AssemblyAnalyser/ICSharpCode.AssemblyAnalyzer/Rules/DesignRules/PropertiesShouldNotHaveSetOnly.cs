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
	/// Description of PropertiesShouldNotHaveSetOnly.	
	/// </summary>
	public class PropertiesShouldNotHaveSetOnly : AbstractReflectionRule, IPropertyRule
	{
		public override string Description {
			get {
				// FIXME: I18N
				return "Properties should not be write only";
			}
		}
		
		public override string Details {
			get {
				// FIXME: I18N
				return "Write only properties generally indicate a design flaw and should be avoided.";
			}
		}
		
		public Resolution Check(PropertyInfo property)
		{
			if (!property.CanRead && property.CanWrite) {
				// FIXME: I18N
				return new Resolution (this, String.Format ("Add a getter to the property <code>{0}</code> in the type <code>{1}</code>.", property.Name, property.DeclaringType.FullName), NamingUtilities.Combine(property.DeclaringType.FullName, property.Name));
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
	public class PropertiesShouldNotHaveSetOnlyTest
	{
		class A {
			public int Inta {
				get {
					return 5;
				}
				set {
					
				}
			}
			public string StrB {
				get {
					return "";
				}
			}
		}
		[Test]
		public void TestCorrectProperties()
		{
			PropertiesShouldNotHaveSetOnly rule = new PropertiesShouldNotHaveSetOnly();
			Assertion.AssertNull(rule.Check(typeof(A).GetProperty("Inta")));
			Assertion.AssertNull(rule.Check(typeof(A).GetProperty("StrB")));
		}
		
		class B {
			public int Inta {
				set {
				}
			}
			public string StrB {
				set {
				}
			}
		}
		
		[Test]
		public void TestIncorrectProperties()
		{
			PropertiesShouldNotHaveSetOnly rule = new PropertiesShouldNotHaveSetOnly();
			Assertion.AssertNotNull(rule.Check(typeof(B).GetProperty("Inta")));
			Assertion.AssertNotNull(rule.Check(typeof(B).GetProperty("StrB")));
		}
	}
}
#endif
#endregion
