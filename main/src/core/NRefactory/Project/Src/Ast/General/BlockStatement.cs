// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="none" email=""/>
//     <version>$Revision: 1965 $</version>
// </file>

using System;

namespace ICSharpCode.NRefactory.Ast
{
	public class BlockStatement : Statement
	{
		// Children in C#: LabelStatement, LocalVariableDeclaration, Statement
		// Children in VB: LabelStatement, EndStatement, Statement
		
		public static new NullBlockStatement Null {
			get {
				return NullBlockStatement.Instance;
			}
		}
		
		public override object AcceptVisitor(IAstVisitor visitor, object data)
		{
			return visitor.VisitBlockStatement(this, data);
		}
		
		public override string ToString()
		{
			return String.Format("[BlockStatement: Children={0}]",
			                     GetCollectionString(base.Children));
		}
	}
	
	public class NullBlockStatement : BlockStatement
	{
		static NullBlockStatement nullBlockStatement = new NullBlockStatement();
		
		public override bool IsNull {
			get {
				return true;
			}
		}
		
		public static NullBlockStatement Instance {
			get {
				return nullBlockStatement;
			}
		}
		
		NullBlockStatement()
		{
		}
		
		public override object AcceptVisitor(IAstVisitor visitor, object data)
		{
			return data;
		}
		public override object AcceptChildren(IAstVisitor visitor, object data)
		{
			return data;
		}
		public override void AddChild(INode childNode)
		{
			throw new InvalidOperationException();
		}
		
		public override string ToString()
		{
			return String.Format("[NullBlockStatement]");
		}
	}
}
