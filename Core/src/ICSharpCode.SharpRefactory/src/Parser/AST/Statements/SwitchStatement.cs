using System;
using System.Collections;

namespace ICSharpCode.SharpRefactory.Parser.AST 
{
	public class SwitchStatement : BlockStatement
	{
		Expression switchExpression;
		ArrayList  switchSections  = new ArrayList();
		
		public Expression SwitchExpression {
			get {
				return switchExpression;
			}
			set {
				switchExpression = value;
			}
		}
		
		public ArrayList SwitchSections {
			get {
				return switchSections;
			}
			set {
				switchSections = value;
			}
		}
		
		public SwitchStatement(Expression switchExpression, ArrayList switchSections)
		{
			this.switchExpression = switchExpression;
			this.switchSections = switchSections;
		}
		
		public SwitchStatement(Expression switchExpression)
		{
			this.switchExpression = switchExpression;
		}
		
		public override object AcceptVisitor(IASTVisitor visitor, object data)
		{
			return visitor.Visit(this, data);
		}
	}
	
	public class SwitchSection : BlockStatement
	{
		ArrayList switchLabels = new ArrayList();
		
		public ArrayList SwitchLabels {
			get {
				return switchLabels;
			}
			set {
				switchLabels = value;
			}
		}
		
		public override object AcceptVisitor(IASTVisitor visitor, object data)
		{
			return visitor.Visit(this, data);
		}
	}
	
	public class CaseLabel
	{
		Expression label;
		
		/// <value>null means default case</value>
		public Expression Label {
			get {
				return label;
			}
			set {
				label = value;
			}
		}
		
		public CaseLabel(Expression label)
		{
			this.label = label;
		}
		
		public CaseLabel()
		{
			this.label = null;
		}
	}
}
