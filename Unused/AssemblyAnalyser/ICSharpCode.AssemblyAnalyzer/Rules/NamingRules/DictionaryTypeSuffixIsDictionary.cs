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
	/// Description of DictionaryTypeSuffixIsDictionary.	
	/// </summary>
	public class DictionaryTypeSuffixIsDictionary : AbstractReflectionRule, ITypeRule
	{
		public override string Description {
			get {
				// FIXME: I18N
				return "Dictionary names have the suffix 'Dictionary'.";
			}
		}
		
		public override string Details {
			get {
				// FIXME: I18N
				return "A type that implements <code><a href='help://types/System.Collections.IDictionary'>IDictionary</a></code> is a dictonary and should use the suffix <i>Dictionary</i> like in <code>HybridDictionary</code>.";
			}
		}
		
		public Resolution Check(Type type)
		{
			if (typeof(System.Collections.IDictionary).IsAssignableFrom(type) && !type.Name.EndsWith("Dictionary")) {
				// FIXME: I18N
				return new Resolution (this, String.Format ("Change the name of the dictionary <code>{0}</code> so that it ends with <i>Dictionary</i>.", type.FullName), type.FullName);
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
	public class DictionaryTypeSuffixIsDictionaryTest
	{
		class CorrectDictionary : System.Collections.Hashtable
		{
		}
		[Test]
		public void TestCorrectDictionary()
		{
			DictionaryTypeSuffixIsDictionary dictionaryTypeSuffixIsDictionary = new DictionaryTypeSuffixIsDictionary();
			Assertion.AssertNull(dictionaryTypeSuffixIsDictionary.Check(typeof(CorrectDictionary)));
		}
		
		class IncorrectDictionaryWrongSuffix : System.Collections.Hashtable
		{
		}
		[Test]
		public void TestIncorrectDictionary()
		{
			DictionaryTypeSuffixIsDictionary dictionaryTypeSuffixIsDictionary = new DictionaryTypeSuffixIsDictionary();
			Assertion.AssertNotNull(dictionaryTypeSuffixIsDictionary.Check(typeof(IncorrectDictionaryWrongSuffix)));
		}
		
	}
}
#endif
#endregion
