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
	/// Description of MembersArePascalCased.	
	/// </summary>
	public class MembersArePascalCased : AbstractReflectionRule, IMemberRule
	{
		public override string Description {
			get {
				// FIXME: I18N
				return "Member names should be pascal cased";
			}
		}
		
		public override string Details {
			get {
				// FIXME: I18N
				return "Pascal casing capitalized the first letter like in <code><b>W</b>riteLine</code>. Use pascal casing for all public identifiers that consist of compound words.";
			}
		}
		
		public MembersArePascalCased()
		{
			base.certainty = 99;
		}
		
		public Resolution Check(Module module, MemberInfo member)
		{
			if (member is FieldInfo || member is ConstructorInfo) {
				return null;
			}
			if (member is MethodInfo) {
				MethodInfo mi = (MethodInfo)member;
				if (mi.IsSpecialName) {
					return null;
				}
			}
			
			if (!NamingUtilities.IsPascalCase(member.Name)) {
				// FIXME: I18N
				return new Resolution (this, String.Format ("Use pascal casing for <code>{0}</code> in <code>{1}</code>.<BR>For example : <code>${2}</code>.", member.Name, member.DeclaringType, NamingUtilities.PascalCase (member.Name)), NamingUtilities.Combine(member.DeclaringType.FullName, member.Name));
			}
			return null;
		}
	}
}
