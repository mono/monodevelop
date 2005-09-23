using System;
using System.Collections;

namespace ICSharpCode.SharpRefactory.Parser.AST 
{
	public class IndexerExpression : Expression
	{
		Expression targetObject;
		ArrayList  indices; // Expression list
		
		public Expression TargetObject {
			get {
				return targetObject;
			}
			set {
				targetObject = value;
			}
		}
		
		public ArrayList Indices {
			get {
				return indices;
			}
			set {
				indices = value;
			}
		}
		
		public IndexerExpression(Expression targetObject, ArrayList indices)
		{
			this.targetObject = targetObject;
			this.indices = indices;
		}
		
		public override object AcceptVisitor(IASTVisitor visitor, object data)
		{
			return visitor.Visit(this, data);
		}
		
		public override string ToString()
		{
			return String.Format("[IndexerExpression: TargetObject={0}, Indices={1}]",
			                     targetObject,
			                     GetCollectionString(indices));
		}

	}
}
