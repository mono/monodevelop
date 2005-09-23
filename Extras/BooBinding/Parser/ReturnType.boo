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
import System.Diagnostics
import MonoDevelop.Internal.Parser
import MonoDevelop.Core.Services
import MonoDevelop.Services
import Boo.Lang.Compiler.Ast as AST

/////////////////////////////////////
///          Return Type          ///
/////////////////////////////////////
class ReturnType(AbstractReturnType):
	def constructor(fullyQualifiedName as string):
		self(fullyQualifiedName, array(int, 0), 0)
	
	def constructor(fullyQualifiedName as string, arrayDimensions as (int), pointerNestingLevel as int):
		self.FullyQualifiedName = fullyQualifiedName
		self.arrayDimensions = arrayDimensions
		self.pointerNestingLevel = pointerNestingLevel
	
	def constructor(t as AST.TypeReference):
		super.pointerNestingLevel = 0
		if t isa AST.SimpleTypeReference:
			super.arrayDimensions = array(int, 0)
			name = cast(AST.SimpleTypeReference, t).Name
			expandedName = BooBinding.BooAmbience.ReverseTypeConversionTable[name]
			name = expandedName if expandedName != null
			super.FullyQualifiedName = name
		elif t isa AST.ArrayTypeReference:
			ar as AST.ArrayTypeReference = t
			depth = 1
			while ar.ElementType isa AST.ArrayTypeReference:
				depth += 1
				ar = ar.ElementType
			dimensions = array(int, depth)
			for i as int in range(depth):
				dimensions[i] = 1
			self.arrayDimensions = dimensions
			if ar.ElementType isa AST.SimpleTypeReference:
				super.FullyQualifiedName = cast(AST.SimpleTypeReference, ar.ElementType).Name
			else:
				Error ("Got unknown TypeReference in Array: ${t}")
				super.FullyQualifiedName = "<Error>"
		else:
			super.arrayDimensions = array(int, 0)
			super.FullyQualifiedName = "<Error>"
			Error ("Got unknown TypeReference ${t}")
	
	static def CreateReturnType(node as AST.Node) as IReturnType:
		if node isa AST.Field:
			t = (node as AST.Field).Type
		elif node isa AST.Property:
			t = (node as AST.Property).Type
		elif node isa AST.Method:
			t = (node as AST.Method).ReturnType
		else:
			raise "Unknown node ${node.GetType().FullName}"
		str = t as AST.SimpleTypeReference
		if (str != null and str.Name != "unknown") or t isa AST.ArrayTypeReference:
			return ReturnType(t)
		else:
			if node isa AST.Field:
				return InferredReturnType((node as AST.Field).Initializer, node.LexicalInfo)
			elif node isa AST.Property:
				prop as AST.Property = node
				return InferredReturnType(GetReturnExpression(prop.Getter), node.LexicalInfo)
			elif node isa AST.Method:
				return InferredReturnType(GetReturnExpression(node), node.LexicalInfo)
	
	private static def GetReturnExpression(method as AST.Method):
		return null if method == null
		return null if method.Body == null
		visitor = FindReturnExpressionVisitor()
		method.Body.Accept(visitor)
		return visitor.Expression
	
	private class FindReturnExpressionVisitor(AST.DepthFirstVisitor):
		[Getter(Expression)]
		_expression as AST.Expression
		
		override def OnReturnStatement(node as AST.ReturnStatement):
			if _expression isa AST.NullLiteralExpression or not (node.Expression isa AST.NullLiteralExpression):
				_expression = node.Expression
	
	def constructor(t as AST.TypeDefinition):
		self(t.FullName)
	
	def constructor(c as IClass):
		self(c.FullyQualifiedName)
	
	def Clone() as ReturnType:
		return ReturnType(FullyQualifiedName, arrayDimensions, pointerNestingLevel)
	
	override def ToString():
		return "[${GetType().Name} Name=${FullyQualifiedName}]"
	
	private def Error (message):
		BooParser.Error (self.GetType (), message)

/////////////////////////////////////
///     Namespace Return Type     ///
/////////////////////////////////////
class NamespaceReturnType(AbstractReturnType):
	def constructor(fullyQualifiedName as string):
		self.FullyQualifiedName = fullyQualifiedName
		self.arrayDimensions = array(int, 0)
		self.pointerNestingLevel = 0
	
	override def ToString():
		return "[${GetType().Name} Name=${FullyQualifiedName}]"

/////////////////////////////////////
///      Inferred Return Type     ///
/////////////////////////////////////
class InferredReturnType(AbstractReturnType):
	_expression as AST.Expression
	
	_filename as string
	_caretLine as int
	_caretColumn as int
	
	def constructor(expression as AST.Expression, info as AST.LexicalInfo):
		_expression = expression
		if info == null or expression == null:
			_resolved = true // don't resolve but return error
		else:
			_filename = info.FileName
			_caretLine = info.Line
			_caretColumn = info.Column
	
	_baseType as IReturnType
	_resolved as bool = false
	
	override FullyQualifiedName as string:
		get:
			r = self.BaseType
			if r == null:
				return "<Error>"
			else:
				return r.FullyQualifiedName
		set:
			raise NotSupportedException()
	
	override PointerNestingLevel as int:
		get:
			r = self.BaseType
			if r == null:
				return 0
			else:
				return r.PointerNestingLevel
	
	override ArrayDimensions as (int):
		get:
			r = self.BaseType
			if r == null:
				return array(int, 0)
			else:
				return r.ArrayDimensions
	
	BaseType as IReturnType:
		get:
			if not _resolved:
				_resolved = true
				_baseType = Resolve()
			return _baseType
	
	def Resolve() as IReturnType:
		resolver = Resolver()
		projService = ServiceManager.GetService(typeof(ProjectService)) as ProjectService
		if resolver.Initialize(projService.ParserDatabase.GetFileParserContext (_filename), _caretLine, _caretColumn, _filename):
			visitor = ExpressionTypeVisitor(Resolver : resolver)
			visitor.Visit(_expression)
			return visitor.ReturnType
		else:
			return null
