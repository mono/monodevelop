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
	/// Description of DelegatesHaveNoDelegateSuffix.	
	/// </summary>
	public class DelegatesHaveNoDelegateSuffix : AbstractReflectionRule, ITypeRule
	{
		public override string Description {
			get {
				// FIXME: I18N
				return "Delegate names do not have the suffix 'Delegate'";
			}
		}
		
		public override string Details {
			get {
				// FIXME: I18N
				return "Do not use the <i>Delegate</i> suffix for delegate names. You may consider using a callback instead.";
			}
		}
		
		public DelegatesHaveNoDelegateSuffix()
		{
			base.certainty = 99;
		}
		
		public Resolution Check(Type type)
		{
			if (type.IsSubclassOf(typeof(System.Delegate)) && type.Name.EndsWith("Delegate")) {
				// FIXME: I18N
				return new Resolution (this, String.Format ("Rename the delegate <code>{0}</code> so that it does not end with <i>Delegate</i>. Or replace it with a callback.", type.FullName), type.FullName);
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
	public class DelegatesHaveNoDelegateSuffixTest
	{
		delegate void MyDelegateWithoutDelegateSuffix();
		[Test]
		public void TestCorrectDelegate()
		{
			DelegatesHaveNoDelegateSuffix delegatesHaveNoDelegateSuffix = new DelegatesHaveNoDelegateSuffix();
			Assertion.AssertNull(delegatesHaveNoDelegateSuffix.Check(typeof(MyDelegateWithoutDelegateSuffix)));
		}
		
		delegate void MyDelegateWithDelegateSuffixDelegate();
		[Test]
		public void TestIncorrectDelegate()
		{
			DelegatesHaveNoDelegateSuffix delegatesHaveNoDelegateSuffix = new DelegatesHaveNoDelegateSuffix();
			Assertion.AssertNotNull(delegatesHaveNoDelegateSuffix.Check(typeof(MyDelegateWithDelegateSuffixDelegate)));
		}
	}
}
#endif
#endregion
