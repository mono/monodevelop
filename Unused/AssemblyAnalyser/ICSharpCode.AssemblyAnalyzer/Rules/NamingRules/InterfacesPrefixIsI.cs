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
	/// Description of InterfacesPrefixIsI.	
	/// </summary>
	public class InterfacesPrefixIsI : AbstractReflectionRule, ITypeRule
	{
		public override string Description {
			get {
				// FIXME: I18N
				return "Interface names have the Prefix 'I'";
			}
		}
		
		public override string Details {
			get {
				// FIXME: I18N
				return "An interface name should always start with <i>I</i> like in <code>IComparable</code>.";
			}
		}
		
		public InterfacesPrefixIsI()
		{
			base.certainty = 99;
		}
		
		public Resolution Check(Type type)
		{
			if (type.IsInterface && !type.Name.StartsWith("I")) {
				// FIXME: I18N
				return new Resolution (this, String.Format ("Change the name of the interface <code>{0}</code> so that it starts with <i>I</i>.", type.FullName), type.FullName);
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
	public class InterfacesPrefixIsITest
	{
		interface ICorrectInterface
		{
		}
		[Test]
		public void TestCorrectAttribute()
		{
			InterfacesPrefixIsI interfacesPrefixIsI = new InterfacesPrefixIsI();
			Assertion.AssertNull(interfacesPrefixIsI.Check(typeof(System.ICloneable)));
			Assertion.AssertNull(interfacesPrefixIsI.Check(typeof(System.IComparable)));
			Assertion.AssertNull(interfacesPrefixIsI.Check(typeof(ICorrectInterface)));
		}
		
		interface WrongInterface
		{
		}
		[Test]
		public void TestIncorrectAttribute()
		{
			InterfacesPrefixIsI interfacesPrefixIsI = new InterfacesPrefixIsI();
			Assertion.AssertNotNull(interfacesPrefixIsI.Check(typeof(WrongInterface)));
		}
	}
}
#endif
#endregion
