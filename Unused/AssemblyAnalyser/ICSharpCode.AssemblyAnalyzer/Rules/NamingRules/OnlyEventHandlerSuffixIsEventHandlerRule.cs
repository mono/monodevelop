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
	/// Description of OnlyEventHandlerSuffixIsEventHandlerRule.	
	/// </summary>
	public class OnlyEventHandlerSuffixIsEventHandlerRule : AbstractReflectionRule, IMemberRule
	{
		public override string Description {
			get {
				// FIXME: I18N
				return "Only event handler names have the suffix 'EventHandler'";
			}
		}
		
		public override string Details {
			get {
				// FIXME: I18N
				return "Only delegates that get used in events have the suffix <i>EventHandler</i>.";
			}
		}
		
		public OnlyEventHandlerSuffixIsEventHandlerRule()
		{
			base.certainty = 99;
			base.priorityLevel = PriorityLevel.CriticalError;
		}
		
		public Resolution Check(Module module, MemberInfo member)
		{
			if (member is MethodInfo) {
				MethodInfo mi = (MethodInfo)member;
				if (mi.IsSpecialName) {
					return null;
				}
			}
			if (member.Name.EndsWith("EventHandler")) {
				// FIXME: I18N
				return new Resolution (this, String.Format ("", member.Name, member.DeclaringType.FullName), NamingUtilities.Combine (member.ReflectedType.FullName, member.Name));
			}
			return null;
		}
	}
}
