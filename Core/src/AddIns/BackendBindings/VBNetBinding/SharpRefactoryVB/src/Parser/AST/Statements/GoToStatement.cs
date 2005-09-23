using System;
using System.Collections;

using ICSharpCode.SharpRefactory.Parser.VB;

namespace ICSharpCode.SharpRefactory.Parser.AST.VB
{
	public class GoToStatement : Statement
	{
		string labelName;
		
		public string LabelName
		{
			get {
				return labelName;
			}
			set {
				labelName = value;
			}
		}
		
		public GoToStatement(string labelName)
		{
			this.labelName = labelName;
		}
		
		public override object AcceptVisitor(IASTVisitor visitor, object data)
		{
			return visitor.Visit(this, data);
		}
	}
}
