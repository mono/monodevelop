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
	/// Description of AvoidNamespacesWithFewMembers.	
	/// </summary>
	public class AvoidNamespacesWithFewMembers : AbstractReflectionRule, INamespaceRule
	{
		public override string Description {
			get {
				// FIXME: I18N
				return "Avoid having namespaces with few type members";
			}
		}
		
		public override string Details {
			get {
				// FIXME: I18N
				return " A namespace should generally contain a minimum of five types.";
			}
		}
		
		public AvoidNamespacesWithFewMembers()
		{
			base.certainty     = 50;
			base.priorityLevel = PriorityLevel.Warning;
		}
		
		public Resolution Check(string namespaceName, ICollection types)
		{
			if (namespaceName != null && namespaceName.Length > 0 && types.Count < 5) {
				// FIXME: I18N
				return new Resolution (this, String.Format ("Consider merging the types inside namespace <code>{0}</code> with another namespace.", namespaceName), namespaceName);
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
	public class AvoidNamespacesWithFewMembersTest
	{
		[Test]
		public void TestCorrectNamespaces()
		{
			AvoidNamespacesWithFewMembers rule = new AvoidNamespacesWithFewMembers();
			Assertion.AssertNull(rule.Check("MyNamespace", new Type[] {typeof(System.Object),
			                                                             typeof(System.Object),
			                                                             typeof(System.Object),
			                                                             typeof(System.Object),
			                                                             typeof(System.Object),
			                                                             typeof(System.Object)}));
		}
		
		[Test]
		public void TestIncorrectAttribute()
		{
			AvoidNamespacesWithFewMembers rule = new AvoidNamespacesWithFewMembers();
			Assertion.AssertNotNull(rule.Check("a", new Type[] {}));
		}
	}
}
#endif
#endregion
