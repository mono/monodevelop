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
	/// Description of TypeNamesDoNotContainUnderscores.
	/// </summary>
	public class TypeNamesDoNotContainUnderscores : AbstractReflectionRule, ITypeRule
	{
		public override string Description {
			get {
				// FIXME: I18N
				return "Type names do not contain underscores '_'";
			}
		}
		
		public override string Details {
			get {
				// FIXME: I18N
				return "Underscores should never be used inside type names.";
			}
		}
		
		public TypeNamesDoNotContainUnderscores()
		{
			base.certainty = 99;
		}
		
		public Resolution Check(Type type)
		{
			if (NamingUtilities.ContainsUnderscore(type.Name)) {
				// FIXME: I18N
				return new Resolution (this, String.Format ("Remove all underscores in the type name <code>{0}</code>.", type.FullName), type.FullName);
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
	public class TypeNamesDoNotContainUnderscoresTest
	{
		interface ICorrectInterface
		{
		}
		[Test]
		public void TestCorrectTypenames()
		{
			TypeNamesDoNotContainUnderscores typeNamesDoNotContainUnderscores = new TypeNamesDoNotContainUnderscores();
			Assertion.AssertNull(typeNamesDoNotContainUnderscores.Check(typeof(ICorrectInterface)));
		}
		
		class Wrong_Class
		{
		}
		[Test]
		public void TestIncorrectTypenames()
		{
			TypeNamesDoNotContainUnderscores typeNamesDoNotContainUnderscores = new TypeNamesDoNotContainUnderscores();
			Assertion.AssertNotNull(typeNamesDoNotContainUnderscores.Check(typeof(Wrong_Class)));
		}
	}
}
#endif
#endregion
