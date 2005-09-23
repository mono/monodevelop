// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.Xml;

namespace MonoDevelop.Core.AddIns.Conditions
{
	/// <summary>
	/// This is a abstract implementation of the <see cref="ICondition"/> interface.
	/// </summary>
	public abstract class AbstractCondition : ICondition
	{
		[XmlMemberAttribute("action")]
		ConditionFailedAction action = ConditionFailedAction.Exclude;
		
		/// <summary>
		/// Returns the action, if the condition is failed.
		/// </summary>
		public ConditionFailedAction Action {
			get {
				return action;
			}
			set {
				action = value;
			}
		}
		
		/// <summary>
		/// Inheriting classes need to overwrite this method.
		/// </summary>
		/// <seealso cref="ICondition"/> interface.
		public abstract bool IsValid(object caller);
	}
}
