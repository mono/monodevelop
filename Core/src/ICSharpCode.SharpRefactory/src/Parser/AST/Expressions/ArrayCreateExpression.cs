using System;
using System.Collections;

namespace ICSharpCode.SharpRefactory.Parser.AST
{
	public class ArrayCreationParameter : AbstractNode 
	{
		ArrayList expressions = null;
		int       dimensions  = -1;
		
		public bool IsExpressionList {
			get {
				return expressions != null;
			}
		}
		
		public ArrayList Expressions {
			get {
				return expressions;
			}
			set {
				expressions = value;
			}
		}
		
		public int Dimensions {
			get {
				return dimensions;
			}
			set {
				dimensions = value;
			}
		}
		
		public ArrayCreationParameter(ArrayList expressions)
		{
			this.expressions = expressions;
		}
		
		public ArrayCreationParameter(int dimensions)
		{
			this.dimensions = dimensions;
		}
		
		public override object AcceptVisitor(IASTVisitor visitor, object data)
		{
			return visitor.Visit(this, data);
		}
		
		public override string ToString()
		{
			return String.Format("[ArrayCreationParameter: Dimensions={0}, Expressions={1}",
			                     dimensions,
			                     GetCollectionString(expressions));
		}
	}
	
	public class ArrayCreateExpression : Expression
	{
		TypeReference              createType       = null;
		ArrayList                  parameters       = null; // ArrayCreationParameter
		int[]                      rank             = null;
		ArrayInitializerExpression arrayInitializer = null; // Array Initializer OR NULL
		
		public TypeReference CreateType {
			get {
				return createType;
			}
			set {
				createType = value;
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
		
		public int[] Rank {
			get {
				return rank;
			}
			set {
				rank = value;
			}
		}
		
		public ArrayInitializerExpression ArrayInitializer {
			get {
				return arrayInitializer;
			}
			set {
				arrayInitializer = value;
			}
		}
		
		public ArrayCreateExpression(TypeReference createType)
		{
			this.createType = createType;
		}
		
//		public ArrayCreateExpression(TypeReference createType, ArrayInitializerExpression arrayInitializer)
//		{
//			this.createType = createType;
//			this.arrayInitializer = arrayInitializer;
//		}
		
		
		public override object AcceptVisitor(IASTVisitor visitor, object data)
		{
			return visitor.Visit(this, data);
		}
		
		public override string ToString()
		{
			return String.Format("[ArrayCreateExpression: CreateType={0}, Parameters={1}, ArrayInitializer={2}]",
			                     createType,
			                     GetCollectionString(parameters),
			                     arrayInitializer);
		}
	}
}
