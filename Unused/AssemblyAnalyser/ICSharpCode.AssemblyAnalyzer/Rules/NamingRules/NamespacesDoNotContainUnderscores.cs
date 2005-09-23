// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krueger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.Collections;

namespace ICSharpCode.AssemblyAnalyser.Rules
{
	/// <summary>
	/// Description of NamespacesDoNotContainUnderScores
	/// </summary>
	public class NamespacesDoNotContainUnderscores : AbstractReflectionRule, INamespaceRule
	{
		public override string Description {
			get {
				// FIXME: I18N
				return "Namespace names do not contain underscores '_'";
			}
		}
		
		public override string Details {
			get {
				// FIXME: I18N
				return "Underscores should never be used inside namespace names.";
			}
		}
		
		public NamespacesDoNotContainUnderscores()
		{
			base.certainty = 99;
		}
		
		public Resolution Check(string namespaceName, ICollection types)
		{
			if (NamingUtilities.ContainsUnderscore(namespaceName)) {
				// FIXME: I18N
				return new Resolution (this, String.Format ("Remove all underscores in namespace <code>{0}</code>.", namespaceName), namespaceName);
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
	public class NamespacesDoNotContainUnderscoresTest
	{
		[Test]
		public void TestCorrectNamespaces()
		{
			NamespacesDoNotContainUnderscores rule = new NamespacesDoNotContainUnderscores();
			Assertion.AssertNull("Empty Namespace", rule.Check("", null));
			Assertion.AssertNull("Single Namespace", rule.Check("MyNamespace", null));
			Assertion.AssertNull("Complex Namespace", rule.Check("System.Windows.Form", null));
		}
		
		[Test]
		public void TestIncorrectNamespaces()
		{
			NamespacesDoNotContainUnderscores rule = new NamespacesDoNotContainUnderscores();
			Assertion.AssertNotNull(rule.Check("_", null));
			Assertion.AssertNotNull(rule.Check("A.Namespace.isWrong_", null));
			Assertion.AssertNotNull(rule.Check("System._Windows.Form", null));
		}
	}
}
#endif
#endregion
