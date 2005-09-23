using System;
using System.Collections;

namespace ICSharpCode.SharpRefactory.Parser.AST 
{
	public class LabelStatement : Statement
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
		
		public LabelStatement(string label)
		{
			this.label = label;
		}
		
		public override object AcceptVisitor(IASTVisitor visitor, object data)
		{
			return visitor.Visit(this, data);
		}
		
		public override string ToString()
		{
			return String.Format("[LabelStatement: Label={0}]",
			                     label);
		}
	}
}
