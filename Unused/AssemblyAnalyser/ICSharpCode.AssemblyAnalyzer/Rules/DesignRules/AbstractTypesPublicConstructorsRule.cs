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
	/// Description of AssemblyStrongName.	
	/// </summary>
	public class AbstractTypesPublicConstructorsRule : AbstractReflectionRule, ITypeRule
	{
		public override string Description {
			get {
				// FIXME: I18N
				return "Abstract types should not have public constructors";
			}
		}
		
		public override string Details {
			get {
				// FIXME: I18N
				return "You cannot create abstract types therefore public constructors do not make any sense.";
			}
		}
		
		public AbstractTypesPublicConstructorsRule()
		{
			priorityLevel = PriorityLevel.CriticalWarning;
		}
		
		public Resolution Check(Type type)
		{
			if (type.IsAbstract) {
				foreach (ConstructorInfo info in type.GetConstructors()) {
					if (info.IsPublic) {
						// FIXME: I18N
 						return new Resolution (this, String.Format ("Change all constructors access levels in <code>{0}</code> to <code>protected</code>.", type.FullName), type.FullName);
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
	public class AbstractTypesPublicConstructorsRuleTest
	{
		abstract class AbstractClassPublic1 {
			public AbstractClassPublic1()
			{
			}
		}
		abstract class AbstractClassPublic2 {
			protected AbstractClassPublic2()
			{
			}
			public AbstractClassPublic2(int x, int y)
			{
			}
		}
		[Test]
		public void TestAbstractTypesWithPublicConstructor()
		{
			AbstractTypesPublicConstructorsRule rule = new AbstractTypesPublicConstructorsRule();
			Assertion.AssertNotNull(rule.Check(typeof(AbstractClassPublic1)));
			Assertion.AssertNotNull(rule.Check(typeof(AbstractClassPublic2)));
		}
		
		abstract class AbstractClass1 {
			protected AbstractClass1()
			{
			}
		}
		abstract class AbstractClass2 {
			protected AbstractClass2(int x, int y)
			{
			}
		}
		
		[Test]
		public void TestAbstractTypesWithoutPublicConstructor()
		{
			AbstractTypesPublicConstructorsRule rule = new AbstractTypesPublicConstructorsRule();
			Assertion.AssertNull(rule.Check(typeof(AbstractClass1)));
			Assertion.AssertNull(rule.Check(typeof(AbstractClass2)));
		}
	}
}
#endif
#endregion
