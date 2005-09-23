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
	/// Description of NamespacesArePascalCased.	
	/// </summary>
	public class NamespacesArePascalCased : AbstractReflectionRule, INamespaceRule
	{
		public override string Description {
			get {
				// FIXME: I18N
				return "Namespace names should be pascal cased";
			}
		}
		
		public override string Details {
			get {
				// FIXME: I18N
				return "Pascal casing capitalized the first letter like in <code><b>S</b>ystem.<b>C</b>omponentModel.<b>D</b>esign</code>. Use pascal casing for all namespaces.";
			}
		}
		
		public NamespacesArePascalCased()
		{
			base.certainty = 99;
		}
		
		public Resolution Check(string namespaceName, ICollection types)
		{
			string[] namespaces = namespaceName.Split('.');
			foreach (string name in namespaces) {
				if (!NamingUtilities.IsPascalCase(name)) {
					for (int i = 0; i < namespaces.Length; ++i) {
						namespaces[i] = NamingUtilities.PascalCase(namespaces[i]);
					}
					// FIXME: I18N
					return new Resolution (this, String.Format ("Use pascal casing for the namespace <code>{0}</code>.<BR>For example: <code>{1}</code>.", namespaceName, String.Join (".", namespaces)), namespaceName);
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
	public class NamespacesArePascalCasedTest
	{
		[Test]
		public void TestCorrectNamespaces()
		{
			NamespacesArePascalCased namespacesArePascalCased = new NamespacesArePascalCased();
			Assertion.AssertNull("Empty Namespace", namespacesArePascalCased.Check("", null));
			Assertion.AssertNull("Single Namespace", namespacesArePascalCased.Check("MyNamespace", null));
			Assertion.AssertNull("Complex Namespace", namespacesArePascalCased.Check("System.Windows.Form", null));
		}
		
		[Test]
		public void TestIncorrectAttribute()
		{
			NamespacesArePascalCased namespacesArePascalCased = new NamespacesArePascalCased();
			Assertion.AssertNotNull(namespacesArePascalCased.Check("a", null));
			Assertion.AssertNotNull(namespacesArePascalCased.Check("A.Namespace.isWrong", null));
			Assertion.AssertNotNull(namespacesArePascalCased.Check("System.windows.Form", null));
		}
	}
}
#endif
#endregion
