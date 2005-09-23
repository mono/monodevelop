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
	/// Description of EnumsHaveNoEnumSuffix.	
	/// </summary>
	public class EnumsHaveNoEnumSuffix : AbstractReflectionRule, ITypeRule
	{
		public override string Description {
			get {
				// FIXME: I18N
				return "Enumeration names do not have the suffix 'Enum'";
			}
		}
		
		public override string Details {
			get {
				// FIXME: I18N
				return "Do not use the <i>Enum</i> suffix for enumeration names.";
			}
		}
		
		public EnumsHaveNoEnumSuffix()
		{
			base.certainty = 99;
		}
		
		public Resolution Check(Type type)
		{
			if (type.IsSubclassOf(typeof(System.Enum)) && type.Name.EndsWith("Enum")) {
				// FIXME: I18N
				return new Resolution (this, String.Format ("Rename the enumeration <code>{0}</code> so that it does not end with <i>Enum</i>.", type.FullName), type.FullName);
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
	public class EnumsHaveNoEnumSuffixTest
	{
		enum CorrectEnumWithAnotherSuffix
		{
			A, B, C
		}
		[Test]
		public void TestCorrectEnum()
		{
			EnumsHaveNoEnumSuffix enumsHaveNoEnumSuffix = new EnumsHaveNoEnumSuffix();
			Assertion.AssertNull(enumsHaveNoEnumSuffix.Check(typeof(CorrectEnumWithAnotherSuffix)));
		}
		
		enum IncorrectEnum
		{
			A, B, C
		}
		[Test]
		public void TestIncorrectDictionary()
		{
			EnumsHaveNoEnumSuffix enumsHaveNoEnumSuffix = new EnumsHaveNoEnumSuffix();
			Assertion.AssertNotNull(enumsHaveNoEnumSuffix.Check(typeof(IncorrectEnum)));
		}
		
	}
}
#endif
#endregion
