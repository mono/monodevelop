// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.Xml;
using MonoDevelop.Core.Services;

namespace MonoDevelop.Core.AddIns.Conditions
{
	/// <summary>
	/// This condition compares two strings using the stringParserService 
	/// for the two strings to compare.
	/// </summary>
	[ConditionAttribute()]
	public class CompareCondition : AbstractCondition
	{
		[XmlMemberAttribute("string", IsRequired=true)]
		string s1;
		
		[XmlMemberAttribute("equals", IsRequired=true)]
		string s2;
		
		/// <summary>
		/// Returns the first string to compare.
		/// </summary>
		public string String1 {
			get {
				return s1;
			}
			set {
				s1 = value;
			}
		}
		
		/// <summary>
		/// Returns the second string to compare.
		/// </summary>
		public string String2 {
			get {
				return s2;
			}
			set {
				s2 = value;
			}
		}
		
		/// <summary>
		/// Returns true, if both <code>stringParserService</code> expanded 
		/// strings are equal.
		/// </summary>
		public override bool IsValid(object owner)
		{
			StringParserService stringParserService = (StringParserService)ServiceManager.GetService(typeof(StringParserService));
			return stringParserService.Parse(s1) == stringParserService.Parse(s2);
		}
	}
}
