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
	/// Description of TypesArePascalCased.	
	/// </summary>
	public class TypesArePascalCased : AbstractReflectionRule, ITypeRule
	{
		public override string Description {
			get {
				// FIXME: I18N
				return "Type names should be pascal cased";
			}
		}
		
		public override string Details {
			get {
				// FIXME: I18N
				return "Pascal casing capitalized the first letter like in <code><b>A</b>ppDomain</code>. Use pascal casing for all type names.";
			}
		}
		
		public TypesArePascalCased()
		{
			base.certainty = 99;
		}
		
		public Resolution Check(Type type)
		{
			if (!NamingUtilities.IsPascalCase(type.Name)) {
				// FIXME: I18N
				return new Resolution(this, String.Format ("Use pascal casing for the type <code>{0}</code>.<BR>For example: <code>{1}</code>.", type.FullName, NamingUtilities.PascalCase (type.FullName)),  type.FullName);
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
	public class TypesArePascalCasedTest
	{
		interface IInterface
		{
		}
		class AClassImplTest
		{
			
		}
		[Test]
		public void TestCorrectTypenames()
		{
			TypesArePascalCased typesArePascalCased = new TypesArePascalCased();
			Assertion.AssertNull(typesArePascalCased.Check(typeof(IInterface)));
			Assertion.AssertNull(typesArePascalCased.Check(typeof(AClassImplTest)));
		}
		
		class wrong
		{
			
		}
		[Test]
		public void TestIncorrectTypenames()
		{
			TypesArePascalCased typesArePascalCased = new TypesArePascalCased();
			Assertion.AssertNotNull(typesArePascalCased.Check(typeof(wrong)));
		}
	}
}
#endif
#endregion
