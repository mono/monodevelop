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

class ExpressionTypeVisitor(DepthFirstVisitor):
	protected override def OnError(node as Node, error as Exception):
		Error (error.ToString ())
		super(node, error)
	
	private def Log (message):
		BooParser.Log (self.GetType (), message)
	
	private def Error (message):
		BooParser.Error (self.GetType (), message)

	[Property(ReturnType)]
	_returnType as IReturnType
	
	[Property(ReturnClass)]
	_returnClass as IClass
	
	[Property(Resolver)]
	_resolver as Resolver
	
	private def CreateReturnType(fullClassName as string):
		_returnClass = null
		if fullClassName == null:
			_returnType = null
		else:
			_returnType = BooBinding.Parser.ReturnType(fullClassName)
	
	private def CreateReturnType(reference as TypeReference):
		_returnClass = null
		if reference == null:
			_returnType = null
		else:
			_returnType = BooBinding.Parser.ReturnType(reference)
	
	private def CreateReturnType(c as IClass):
		_returnClass = c
		if c == null:
			_returnType = null
		else:
			_returnType = BooBinding.Parser.ReturnType(c)
	
	private def SetReturnType(r as IReturnType):
		_returnClass = null
		_returnType = r
	
	private def Debug(node):
		if node == null:
			Log ("-- null --")
		else:
			Log ("${node.ToString()} - ${node.GetType().FullName}")
	
	override def OnCallableBlockExpression(node as CallableBlockExpression):
		Debug(node)
		CreateReturnType("System.Delegate")
	
	override def OnMethodInvocationExpression(node as MethodInvocationExpression):
		Debug(node)
		Debug(node.Target)
		if node.Target isa MemberReferenceExpression:
			// call a method on another object
			mre as MemberReferenceExpression = node.Target
			Visit(mre.Target)
			if _returnClass == null and _returnType != null:
				_returnClass = _resolver.SearchType(_returnType.FullyQualifiedName)
			return if ProcessMethod(node, mre.Name, _returnClass)
			// try if the MemberReferenceExpression is a fully qualified class name (constructor call)
			ProcessMemberReferenceExpression(mre.Name)
			CreateReturnType(_returnClass)
		elif node.Target isa ReferenceExpression:
			re as ReferenceExpression = node.Target
			// try if it is a method on the current object
			return if ProcessMethod(node, re.Name, _resolver.CallingClass)
			// try if it is a builtin method
			return if ProcessMethod(node, re.Name, _resolver.BuiltinClass)
			// try if it is a class name -> constructor
			CreateReturnType(_resolver.SearchType(re.Name))
		else:
			SetReturnType(null)
	
	private def ProcessMethod(node as MethodInvocationExpression, name as string, c as IClass) as bool:
		return false if c == null
		possibleOverloads = FindMethods(c, name, node.Arguments.Count)
		Log ("found ${possibleOverloads.Count} overloads (multiple overloads not supported yet)")
		if possibleOverloads.Count >= 1:
			SetReturnType(cast(IMethod, possibleOverloads[0]).ReturnType)
			return true
		/*// find best overload
		argumentTypes = array(IReturnType, node.Arguments.Count)
		for i as int in range(argumentTypes.Length):
			Visit(node.Arguments[i])
			argumentTypes[i] = _returnType
		...
		*/
		return false
	
	private def FindMethods(c as IClass, name as string, arguments as int):
		possibleOverloads = ArrayList()
		//for cl as IClass in c.ClassInheritanceTree:
		for cl as IClass in _resolver.ParserContext.GetClassInheritanceTree(c):
			for m as IMethod in cl.Methods:
				if m.Parameters.Count == arguments and name == m.Name:
					possibleOverloads.Add(m)
		return possibleOverloads
	
	override def OnSlicingExpression(node as SlicingExpression):
		Debug(node)
		Visit(node.Target)
		slice as Slice = node.Indices[0]
		if (slice.End != null):
			// Boo slice, returns a part of the source -> same type as source
			return
		if _returnType != null and _returnType.ArrayDimensions != null and _returnType.ArrayDimensions.Length > 0:
			SetReturnType(BooBinding.Parser.ReturnType(_returnType.FullyQualifiedName, _returnType.ArrayDimensions[0 : _returnType.ArrayDimensions.Length - 1], 0))
			return
		if _returnClass == null and _returnType != null:
			_returnClass = _resolver.SearchType(_returnType.FullyQualifiedName)
		if _returnClass != null:
			indexers = FindIndexer(_returnClass, 1)
			if indexers.Count > 0:
				SetReturnType(cast(IIndexer, indexers[0]).ReturnType)
				return
		SetReturnType(null)
	
	private def FindIndexer(c as IClass, arguments as int):
		possibleOverloads = ArrayList()
		//for cl as IClass in c.ClassInheritanceTree:
		for cl as IClass in _resolver.ParserContext.GetClassInheritanceTree(c):
			for m as IIndexer in cl.Indexer:
				if m.Parameters.Count == arguments:
					possibleOverloads.Add(m)
		return possibleOverloads
	
	override def OnBinaryExpression(node as BinaryExpression):
		Debug(node)
		CombineTypes(node.Left, node.Right)
	
	override def OnTernaryExpression(node as TernaryExpression):
		Debug(node)
		CombineTypes(node.TrueValue, node.FalseValue)
	
	private def CombineTypes(a as Expression, b as Expression):
		Visit(a)
	
	override def OnReferenceExpression(node as ReferenceExpression):
		// Resolve reference (to a variable, field, parameter or type)
		rt = _resolver.GetTypeFromLocal(node.Name)
		if rt != null:
			SetReturnType(rt)
			return

		return if ProcessMember(node.Name, _resolver.CallingClass)
		if _resolver.IsNamespace(node.Name):
			SetReturnType(NamespaceReturnType(node.Name))
		else:
			CreateReturnType(_resolver.SearchType(node.Name))
	
	override def OnMemberReferenceExpression(node as MemberReferenceExpression):
		Debug(node)
		Visit(node.Target)
		ProcessMemberReferenceExpression(node.Name)
	
	private def ProcessMemberReferenceExpression(name as string):
	"""Gets the return type of the MemberReferenceExpression with the specified name
	on the current return type."""
		if _returnType isa NamespaceReturnType:
			name = _returnType.FullyQualifiedName + '.' + name
			if _resolver.IsNamespace(name):
				SetReturnType(NamespaceReturnType(name))
			else:
				CreateReturnType(_resolver.SearchType(name))
			return
		if _returnClass == null and _returnType != null:
			_returnClass = _resolver.SearchType(_returnType.FullyQualifiedName)
		return if ProcessMember(name, _returnClass)
		SetReturnType(null)
	
	private def ProcessMember(name as string, parentClass as IClass):
		return false if parentClass == null
		for cl as IClass in _resolver.ParserContext.GetClassInheritanceTree(parentClass):
			for c as IClass in cl.InnerClasses:
				if c.Name == name:
					CreateReturnType(c)
					return true
			for f as IField in cl.Fields:
				if f.Name == name:
					SetReturnType(f.ReturnType)
					return true
			for p as IProperty in cl.Properties:
				if p.Name == name:
					SetReturnType(p.ReturnType)
					return true
			for m as IMethod in cl.Methods:
				if m.Name == name:
					CreateReturnType("System.Delegate")
					return true
		return false
	
	override def OnTimeSpanLiteralExpression(node as TimeSpanLiteralExpression):
		CreateReturnType("System.TimeSpan")
	
	override def OnIntegerLiteralExpression(node as IntegerLiteralExpression):
		CreateReturnType("System.Int32")
	
	override def OnDoubleLiteralExpression(node as DoubleLiteralExpression):
		CreateReturnType("System.Double")
	
	override def OnNullLiteralExpression(node as NullLiteralExpression):
		CreateReturnType("System.Object")
	
	override def OnStringLiteralExpression(node as StringLiteralExpression):
		CreateReturnType("System.String")
	
	override def OnSelfLiteralExpression(node as SelfLiteralExpression):
		CreateReturnType(_resolver.CallingClass)
	
	override def OnSuperLiteralExpression(node as SuperLiteralExpression):
		CreateReturnType(_resolver.ParentClass)
	
	override def OnBoolLiteralExpression(node as BoolLiteralExpression):
		CreateReturnType("System.Boolean")
	
	override def OnRELiteralExpression(node as RELiteralExpression):
		CreateReturnType("System.Text.RegularExpressions.Regex")
	
	override def OnHashLiteralExpression(node as HashLiteralExpression):
		CreateReturnType("System.Collections.Hashtable")
	
	override def OnListLiteralExpression(node as ListLiteralExpression):
		CreateReturnType("System.Collections.ArrayList")
	
	override def OnArrayLiteralExpression(node as ArrayLiteralExpression):
		CreateReturnType("System.Array")
	
	override def OnAsExpression(node as AsExpression):
		CreateReturnType(node.Type)
	
	override def OnCastExpression(node as CastExpression):
		CreateReturnType(node.Type)
	
	override def OnTypeofExpression(node as TypeofExpression):
		CreateReturnType("System.Type")
