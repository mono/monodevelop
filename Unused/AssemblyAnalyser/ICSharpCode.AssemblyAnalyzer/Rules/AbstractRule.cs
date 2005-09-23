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
	/// Description of AbstractRule.	
	/// </summary>
	public abstract class AbstractRule : System.MarshalByRefObject, IRule
	{
		protected PriorityLevel priorityLevel = PriorityLevel.Error;
		protected int certainty = 99;
		
		#region ICSharpCode.AssemblyAnalyser.Rules.IRule interface implementation
		public PriorityLevel PriorityLevel {
			get {
				return priorityLevel;
			}
		}
		
		public int Certainty {
			get {
				return certainty;
			}
		}
		
		public abstract string Description {
			get;
		}
		
		public abstract string Details {
			get;
		}
		
		public virtual void EndAnalysis()
		{
		}
		
		public virtual void StartAnalysis()
		{
		}
		#endregion
	}
	
	public class CustomRule : AbstractRule
	{
		string description;
		string details;
		
		public override string Description {
			get {
				return description;
			}
		}
		
		public override string Details {
			get {
				return details;
			}
		}
		
		public CustomRule(string description, string details)
		{
			this.description = description;
			this.details = details;
		}
		public CustomRule(string description, string details, PriorityLevel priorityLevel, int certainty)
		{
			this.description = description;
			this.details = details;
			this.priorityLevel = priorityLevel;
			this.certainty = certainty;
		}
	}
}
