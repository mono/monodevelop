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
	/// Default actions, when a condition is failed.
	/// </summary>
	public enum ConditionFailedAction {
		Nothing,
		Exclude,
		Disable
	}
	
	
	/// <summary>
	/// The <code>ICondition</code> interface describes the basic funcionalaty
	/// a condition must have.
	/// </summary>
	public interface ICondition
	{
		/// <summary>
		/// Returns the action which occurs, when this condition fails.
		/// </summary>
		ConditionFailedAction Action {
			get;
			set;
		}
		
		/// <summary>
		/// Returns true, when the condition is valid otherwise false.
		/// </summary>
		bool IsValid(object caller);
	}
}
