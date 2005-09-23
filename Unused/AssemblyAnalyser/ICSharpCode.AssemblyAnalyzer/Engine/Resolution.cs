// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krueger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using ICSharpCode.AssemblyAnalyser.Rules;

namespace ICSharpCode.AssemblyAnalyser
{
	/// </summary>
	/// <summary>
	/// Description of Resolution.	
	public class Resolution : System.MarshalByRefObject
	{
		IRule  failedRule;
		string text;
		string item;
		
		public IRule FailedRule {
			get {
				return failedRule;
			}
		}
		public string Text {
			get {
				return text;
			}
		}
		public string Item {
			get {
				return item;
			}
		}
		
		// instead of the SD substitution, Rules are expected to
		// pass in the final text, hint use String.Format ()
		public Resolution(IRule failedRule, string text, string item)
		{
			this.failedRule = failedRule;
			this.text = text;
			this.item = item;
		}
	}
}
