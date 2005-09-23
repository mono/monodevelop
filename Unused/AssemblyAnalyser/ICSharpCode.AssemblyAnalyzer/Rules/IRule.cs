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
	/// Description of IRule.	
	/// </summary>
	public interface IRule
	{
		void EndAnalysis();
		void StartAnalysis();
		
		PriorityLevel PriorityLevel {
			get;
		}
		
		int Certainty {
			get;
		}
		
		string Description {
			get;
		}
		
		string Details {
			get;
		}
	}
}
