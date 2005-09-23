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
import MonoDevelop.Internal.Parser
import Boo.Lang.Compiler.Ast as AST

class Constructor(BooAbstractMethod):
	def constructor(m as ModifierEnum, region as IRegion, bodyRegion as IRegion):
		FullyQualifiedName = 'ctor'
		self.region = region
		self.bodyRegion = bodyRegion
		modifiers = m

class Destructor(BooAbstractMethod):
	def constructor(className as string, m as ModifierEnum, region as IRegion, bodyRegion as IRegion):
		FullyQualifiedName = '~' + className
		self.region = region
		self.bodyRegion = bodyRegion
		modifiers = m

class BooAbstractMethod(AbstractMethod):
	[Property(Node)]
	_node as AST.Method
	
	def AddModifier(m as ModifierEnum):
		modifiers = modifiers | m

class Event(AbstractEvent):
	def AddModifier(m as ModifierEnum):
		modifiers = modifiers | m
	
	def constructor(name as string, rtype as IReturnType, m as ModifierEnum, region as IRegion, bodyRegion as IRegion):
		FullyQualifiedName = name
		returnType = rtype
		self.region = region
		self.bodyRegion = bodyRegion
		modifiers = m

class Field(AbstractField):
	def AddModifier(m as ModifierEnum):
		modifiers = modifiers | m
	
	def constructor(rtype as IReturnType, fullyQualifiedName as string, m as ModifierEnum, region as IRegion):
		self.returnType = rtype
		self.FullyQualifiedName = fullyQualifiedName
		self.region = region
		modifiers = m
	
	def SetModifiers(m as ModifierEnum):
		modifiers = m

class Indexer(AbstractIndexer):
	def AddModifier(m as ModifierEnum):
		modifiers = modifiers | m
	
	def constructor(rtype as IReturnType, parameters as ParameterCollection, m as ModifierEnum, region as IRegion, bodyRegion as IRegion):
		returnType = rtype
		self.parameters = parameters
		self.region = region
		self.bodyRegion = bodyRegion
		modifiers = m

class Method(BooAbstractMethod):
	def constructor(name as string, rtype as IReturnType, m as ModifierEnum, region as IRegion, bodyRegion as IRegion):
		FullyQualifiedName = name
		self.returnType = rtype
		self.region = region
		self.bodyRegion = bodyRegion
		modifiers = m

class Property(AbstractProperty):
	[Property(Node)]
	_node as AST.Property
	
	def AddModifier(m as ModifierEnum):
		modifiers = modifiers | m
	
	def constructor(fullyQualifiedName as string, rtype as IReturnType, getter as IMethod, setter as IMethod, getRegion as IRegion, setRegion as IRegion, m as ModifierEnum, region as IRegion, bodyRegion as IRegion):
		self.FullyQualifiedName = fullyQualifiedName
		self.returnType = rtype
		self.getterMethod = getter
		self.setterMethod = setter
		self.getterRegion = getRegion
		self.setterRegion = setRegion
		self.region = region
		self.bodyRegion = bodyRegion
		modifiers = m
