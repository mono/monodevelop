using System;
using System.Drawing;
using System.Collections;

using ICSharpCode.SharpRefactory.Parser.AST.VB;

namespace ICSharpCode.SharpRefactory.Parser.VB
{
	public class LocalLookupVariable
	{
		TypeReference typeRef;
		Point         startPos;
		Point         endPos;
		
		public TypeReference TypeRef {
			get {
				return typeRef;
			}
		}
		public Point StartPos {
			get {
				return startPos;
			}
		}
		public Point EndPos {
			get {
				return endPos;
			}
		}
		
		public LocalLookupVariable(TypeReference typeRef, Point startPos, Point endPos)
		{
			this.typeRef = typeRef;
			this.startPos = startPos;
			this.endPos = endPos;
		}
	}
	
	public class LookupTableVisitor : AbstractASTVisitor
	{
		Hashtable variables      = new Hashtable();
		ArrayList withStatements = new ArrayList();
		
		public Hashtable Variables {
			get {
				return variables;
			}
		}
		
		public ArrayList WithStatements {
			get {
				return withStatements;
			}
		}
		
		public void AddVariable(TypeReference typeRef, string name, Point startPos, Point endPos)
		{
			if (name == null || name.Length == 0) {
				return;
			}
			name = name.ToLower();
			ArrayList list;
			if (variables[name] == null) {
				variables[name] = list = new ArrayList();
			} else {
				list = (ArrayList)variables[name];
			}
			list.Add(new LocalLookupVariable(typeRef, startPos, endPos));
		}
		
		public override object Visit(LocalVariableDeclaration localVariableDeclaration, object data)
		{
			foreach (VariableDeclaration varDecl in localVariableDeclaration.Variables) {
				AddVariable(varDecl.Type, 
				            varDecl.Name,
				            localVariableDeclaration.StartLocation,
				            CurrentBlock == null ? new Point(-1, -1) : CurrentBlock.EndLocation);
			}
			return data;
		}
		
		public override object Visit(LoopControlVariableExpression loopControlVariableExpression, object data)
		{
			AddVariable(loopControlVariableExpression.Type, 
			            loopControlVariableExpression.Name,
			            loopControlVariableExpression.StartLocation,
			            CurrentBlock == null ? new Point(-1, -1) : CurrentBlock.EndLocation);
			return data;
		}
		
		public override object Visit(WithStatement withStatement, object data)
		{
			withStatements.Add(withStatement);
			return base.Visit(withStatement, data);
		}
		
	}
}
