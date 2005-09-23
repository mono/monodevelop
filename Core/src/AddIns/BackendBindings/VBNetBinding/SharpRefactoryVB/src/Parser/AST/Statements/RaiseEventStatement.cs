using System;
using System.Collections;

using ICSharpCode.SharpRefactory.Parser.VB;

namespace ICSharpCode.SharpRefactory.Parser.AST.VB
{
	public class RaiseEventStatement : Statement
	{
		string eventName;
		ArrayList parameters;
		
		public string EventName {
			get {
				return eventName;
			}
			set {
				eventName = value;
			}
		}
		public ArrayList Parameters {
			get {
				return parameters;
			}
			set {
				parameters = value;
			}
		}
		
		public RaiseEventStatement(string eventName, ArrayList parameters)
		{
			this.eventName = eventName;
			this.parameters = parameters;
		}
		
		public override object AcceptVisitor(IASTVisitor visitor, object data)
		{
			return visitor.Visit(this, data);
		}
		
		public override string ToString()
		{
			return  String.Format("[RaiseEventStatement: EventName={0}]", 
			                     EventName);
		}
	}
}
