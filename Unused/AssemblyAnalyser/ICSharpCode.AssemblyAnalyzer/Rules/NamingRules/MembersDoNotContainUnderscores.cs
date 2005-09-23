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
	/// Description of MembersDoNotContainUnderscores.	
	/// </summary>
	public class MembersDoNotContainUnderscores : AbstractReflectionRule, IMemberRule
	{
		public override string Description {
			get {
				// FIXME: I18N
				return "Member names do not contain underscores '_'";
			}
		}
		
		public override string Details {
			get {
				// FIXME: I18N
				return "Underscores should never be used inside public members.";
			}
		}
		
		public MembersDoNotContainUnderscores()
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
			
			if (NamingUtilities.ContainsUnderscore(member.Name)) {
				// FIXME: I18N
				return new Resolution (this, String.Format ("Remove all underscores in member name <code>{0}</code> in the type <code>{1}</code>.", member.Name, member.DeclaringType.FullName), NamingUtilities.Combine(member.ReflectedType.FullName, member.Name));
			}
			
			return null;
		}
	}
}
