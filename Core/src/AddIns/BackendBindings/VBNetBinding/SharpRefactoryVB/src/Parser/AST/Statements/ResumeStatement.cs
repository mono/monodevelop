using System;
using System.Collections;

using ICSharpCode.SharpRefactory.Parser.VB;

namespace ICSharpCode.SharpRefactory.Parser.AST.VB
{
	public class ResumeStatement : Statement
	{
		string labelName;
		bool next;
		
		public string LabelName
		{
			get {
				return labelName;
			}
			set {
				labelName = value;
			}
		}
		
		public bool Next
		{
			get {
				return next;
			}
			set {
				next = value;
			}
		}
		
		public ResumeStatement(bool next)
		{
			this.next = next;
		}
		
		public ResumeStatement(string labelName)
		{
			this.labelName = labelName;
		}
		
		public override object AcceptVisitor(IASTVisitor visitor, object data)
		{
			return visitor.Visit(this, data);
		}
	}
}
