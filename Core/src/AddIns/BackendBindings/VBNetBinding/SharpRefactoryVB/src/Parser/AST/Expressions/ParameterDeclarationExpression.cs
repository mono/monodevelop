using System;
using System.Collections;

using ICSharpCode.SharpRefactory.Parser.VB;

namespace ICSharpCode.SharpRefactory.Parser.AST.VB {
	
	public class ParameterDeclarationExpression : Expression
	{
		TypeReference  typeReference;
		string         parameterName;
		ParamModifiers paramModifiers;
		ArrayList      attributes = new ArrayList();
		Expression	   defaultValue;
		
		public override string ToString()
		{
			return String.Format("[ParameterDeclarationExpression: DefaultValue={0}, TypeReference={1}, ParameterName={2}, ParamModifiers={3}, Attributes=TODO]",
			                     defaultValue,
			                     typeReference,
			                     parameterName,
			                     paramModifiers);
		}
		
		public Expression DefaultValue {
			get {
				return defaultValue;
			}
			set {
				defaultValue = value;
			}
		}
		
		public TypeReference TypeReference {
			get {
				return typeReference;
			}
			set {
				typeReference = value;
			}
		}
		
		public string ParameterName {
			get {
				return parameterName;
			}
			set {
				parameterName = value;
			}
		}
		
		public ParamModifiers ParamModifiers {
			get {
				return paramModifiers;
			}
			set {
				paramModifiers = value;
			}
		}
		
		public ArrayList Attributes {
			get {
				return attributes;
			}
			set {
				attributes = value;
			}
		}
		
		public ParameterDeclarationExpression(TypeReference typeReference, string parameterName, ParamModifiers paramModifiers, Expression defaultValue)
		{
			this.typeReference = typeReference;
			this.parameterName = parameterName;
			this.paramModifiers = paramModifiers;
			this.attributes = attributes;
			this.defaultValue = defaultValue;
		}
		
		public ParameterDeclarationExpression(TypeReference typeReference, string parameterName, ParamModifiers paramModifiers)
		{
			this.typeReference  = typeReference;
			this.parameterName  = parameterName;
			this.paramModifiers = paramModifiers;
		}
		public override object AcceptVisitor(IASTVisitor visitor, object data)
		{
			return visitor.Visit(this, data);
		}
	}
}
