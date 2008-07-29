// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="none" email=""/>
//     <version>$Revision: 975 $</version>
// </file>

using System;
using System.Diagnostics;
using System.Collections;
using System.Globalization;

namespace ICSharpCode.NRefactory.Parser.AST {
	
	public class PrimitiveExpression : Expression
	{
		object val;
		string stringValue;
		
		public object Value {
			get {
				return val;
			}
			set {
				val = value;
			}
		}
		
		public string StringValue {
			get {
				return stringValue;
			}
			set {
				stringValue = value == null ? String.Empty : value;
			}
		}
		
		public PrimitiveExpression(object val, string stringValue)
		{
			this.Value       = val;
			this.StringValue = stringValue;
		}
		
		public override object AcceptVisitor(IAstVisitor visitor, object data)
		{
			return visitor.Visit(this, data);
		}
		
		public override string ToString()
		{
			return String.Format("[PrimitiveExpression: Value={1}, ValueType={2}, StringValue={0}]",
			                     stringValue,
			                     Value,
			                     Value == null ? "null" : Value.GetType().FullName
			                    );
		}
	}
}
