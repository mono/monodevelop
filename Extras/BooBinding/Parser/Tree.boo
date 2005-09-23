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
import Boo.Lang.Compiler.Ast as AST

/////////////////////////////////////
///       Compilation Unit        ///
/////////////////////////////////////
class CompilationUnit(AbstractCompilationUnit):
	override MiscComments as CommentCollection:
		get:
			return null
	
	override DokuComments as CommentCollection:
		get:
			return null
	
	override TagComments as TagCollection:
		get:
			return null

/////////////////////////////////////
///             Class             ///
/////////////////////////////////////
class Class(AbstractClass):
	_cu as ICompilationUnit
	
	override CompilationUnit as ICompilationUnit:
		get:
			return _cu
	
	def constructor(cu as CompilationUnit, t as ClassType, m as ModifierEnum, region as IRegion):
		_cu = cu
		classType = t
		self.region = region
		modifiers = m
	
	def UpdateModifier():
		if classType == ClassType.Enum:
			for f as Field in Fields:
				f.AddModifier(ModifierEnum.Public)
			
			return
		
		for f as Field in Fields:
			if f.Modifiers == ModifierEnum.None:
				f.AddModifier(ModifierEnum.Protected)
		
		if classType != ClassType.Interface:
			return
		
		for c as Class in InnerClasses:
			c.modifiers = c.modifiers | ModifierEnum.Public
		
		for m as IMethod in Methods:
			if m isa BooAbstractMethod:
				cast(BooAbstractMethod, m).AddModifier(ModifierEnum.Public)
			else:
				Debug.Assert(false, 'Unexpected type in method of interface. Can not set modifier to public!')
		
		for e as Event in Events:
			e.AddModifier(ModifierEnum.Public)
		
		for f as Field in Fields:
			f.AddModifier(ModifierEnum.Public)
		
		for i as Indexer in Indexer:
			i.AddModifier(ModifierEnum.Public)
		
		for p as Property in Properties:
			p.AddModifier(ModifierEnum.Public)
		
	


/////////////////////////////////////
///           Parameter           ///
/////////////////////////////////////
class Parameter(AbstractParameter):
	def constructor(name as string, rtype as ReturnType):
		Name = name
		returnType = rtype

/////////////////////////////////////
///          Attributes           ///
/////////////////////////////////////
class AttributeSection(AbstractAttributeSection):
	def constructor(attributeTarget as AttributeTarget, attributes as AttributeCollection):
		self.attributeTarget = attributeTarget
		self.attributes = attributes

class ASTAttribute(AbstractAttribute):
	def constructor(name as string, positionalArguments as ArrayList, namedArguments as SortedList):
		self.name = name
		self.positionalArguments = positionalArguments
		self.namedArguments = namedArguments
	


