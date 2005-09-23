#region license
// Copyright (c) 2004-2005, Daniel Grunwald (daniel@danielgrunwald.de)
// Copyright (c) 2005, Peter Johanson (latexer@gentoo.org)
// All rights reserved.
//
// The BooBinding.Parser code is originally that of Daniel Grunwald
// (daniel@danielgrunwald.de) from the SharpDevelop BooBinding. The code has
// been imported here, and modified, including, but not limited to, changes
// to function with MonoDevelop, additions, refactorings, etc.
//
// BooBinding is free software; you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation; either version 2 of the License, or
// (at your option) any later version.
// 
// BooBinding is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with BooBinding; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
#endregion

namespace BooBinding.Parser

import System
import System.Collections
import MonoDevelop.Internal.Parser
import Boo.Lang.Compiler.Ast

class VariableLookupVisitor(DepthFirstVisitor):
	[Property(Resolver)]
	_resolver as Resolver
	
	[Property(LookFor)]
	_lookFor as string
	
	[Getter(ReturnType)]
	_returnType as IReturnType
	
	private def Finish(expr as Expression):
		return if expr == null
		return if _returnType != null
		visitor = ExpressionTypeVisitor(Resolver: _resolver)
		visitor.Visit(expr)
		_returnType = visitor.ReturnType
	
	private def Finish(reference as TypeReference):
		return if _returnType != null
		return if reference == null
		_returnType = BooBinding.Parser.ReturnType(reference)
	
	override def OnDeclaration(node as Declaration):
		return if node.Name != _lookFor
		Finish(node.Type)
	
	override def OnDeclarationStatement(node as DeclarationStatement):
		return if node.Declaration.Name != _lookFor
		Visit(node.Declaration)
		Finish(node.Initializer)
	
	override def OnBinaryExpression(node as BinaryExpression):
		BooParser.Log (self.GetType (), "Binary expression: '${node}'")
		if node.Operator == BinaryOperatorType.Assign and node.Left isa ReferenceExpression:
			reference as ReferenceExpression = node.Left
			if reference.Name == _lookFor:
				Finish(node.Right) unless reference isa MemberReferenceExpression
		super(node)

class VariableListLookupVisitor(DepthFirstVisitor):
	[Property(Resolver)]
	_resolver as Resolver
	
	[Getter(Results)]
	_results as Hashtable = {}
	
	private def Add(name as string, expr as Expression):
		return if name == null or expr == null
		return if _results.ContainsKey(name)
		visitor = ExpressionTypeVisitor(Resolver: _resolver)
		visitor.Visit(expr)
		_results.Add(name, visitor.ReturnType)
	
	private def Add(name as string, reference as TypeReference):
		return if reference == null or name == null
		return if _results.ContainsKey(name)
		_results.Add(name, BooBinding.Parser.ReturnType(reference))
	
	override def OnDeclaration(node as Declaration):
		Add(node.Name, node.Type)
	
	override def OnDeclarationStatement(node as DeclarationStatement):
		Visit(node.Declaration)
		Add(node.Declaration.Name, node.Initializer)
	
	override def OnBinaryExpression(node as BinaryExpression):
		if node.Operator == BinaryOperatorType.Assign and node.Left isa ReferenceExpression:
			reference as ReferenceExpression = node.Left
			Add(reference.Name, node.Right) unless reference isa MemberReferenceExpression
		super(node)
