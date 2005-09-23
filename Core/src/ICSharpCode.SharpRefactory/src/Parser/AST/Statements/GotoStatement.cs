using System;
using System.Collections;

namespace ICSharpCode.SharpRefactory.Parser.AST 
{
	public class GotoStatement : Statement
	{
		string label;
		
		public string Label {
			get {
				return label;
			}
			set {
				label = value;
			}
		}
		
		public GotoStatement(string label)
		{
			this.label = label;
		}
		
		public override object AcceptVisitor(IASTVisitor visitor, object data)
		{
			return visitor.Visit(this, data);
		}
		
		public override string ToString()
		{
			return String.Format("[GotoStatement: Label={0}]",
			                     label);
		}
	}
}
