using System;
using System.Collections;

using ICSharpCode.SharpRefactory.Parser.VB;

namespace ICSharpCode.SharpRefactory.Parser.AST.VB
{
	public class LabelStatement : Statement
	{
		string label;
		Statement embeddedStatement;
		
		public Statement EmbeddedStatement
		{
			get {
				return embeddedStatement;
			}
			set {
				embeddedStatement = value;
			}
		}
		
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
