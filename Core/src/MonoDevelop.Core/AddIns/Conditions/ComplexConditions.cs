// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.Diagnostics;
using System.Xml;

namespace MonoDevelop.Core.AddIns.Conditions
{
	/// <summary>
	/// Negates a condition
	/// </summary>
	public class NegatedCondition : AbstractCondition
	{
		ConditionCollection conditions;
		public NegatedCondition(ConditionCollection conditions)
		{
			this.conditions = conditions;
		}
		
		public override bool IsValid(object owner)
		{
			Debug.Assert(conditions.Count == 1);
			return !conditions[0].IsValid(owner);
		}
	}

	/// <summary>
	/// Gives back the and result of two conditions.
	/// </summary>
	public class AndCondition : AbstractCondition
	{
		ConditionCollection conditions;
		public AndCondition(ConditionCollection conditions)
		{
			this.conditions = conditions;
		}
		
		public override bool IsValid(object owner)
		{
			Debug.Assert(conditions.Count > 1);
			
			bool valid = true;
			
			foreach (ICondition condition in conditions) {
				valid &= condition.IsValid(owner);
			}
			
			return valid;
		}
	}
	
	/// <summary>
	/// Gives back the or result of two conditions.
	/// </summary>
	public class OrCondition : AbstractCondition
	{
		ConditionCollection conditions;
		public OrCondition(ConditionCollection conditions)
		{
			this.conditions = conditions;
		}
		
		public override bool IsValid(object owner)
		{
			Debug.Assert(conditions.Count > 1);
			bool valid = false;
			
			foreach (ICondition condition in conditions) {
				valid |= condition.IsValid(owner);
			}
			
			return valid;
		}
	}
}
