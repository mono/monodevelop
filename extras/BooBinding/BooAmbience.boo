#region license
// Copyright (c) 2004, Daniel Grunwald (daniel@danielgrunwald.de)
// All rights reserved.
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
/*
namespace BooBinding

import System
import System.Collections
import System.Text
import MonoDevelop.Projects.Parser
import MonoDevelop.Core
import MonoDevelop.Core.Properties
import MonoDevelop.Projects.Ambience

class BooAmbience(Ambience):
	[Getter(TypeConversionTable)]
	static _typeConversionTable = {
		'System.Void'    : 'void',
		'System.Object'  : 'object',
		'System.Boolean' : 'bool',
		'System.Byte'    : 'byte',
		'System.SByte'   : 'sbyte',
		//'System.Char'   : 'char',
		//'System.Enum'   : 'enum',
		'System.Int16'  : 'short',
		'System.Int32'  : 'int',
		'System.Int64'  : 'long',
		'System.UInt16' : 'ushort',
		'System.UInt32' : 'uint',
		'System.UInt64' : 'ulong',
		'System.Single' : 'single',
		'System.Double' : 'double',
		'System.Decimal' : 'decimal',
		'System.String' : 'string',
		'System.DateTime' : 'date',
		'System.TimeSpan' : 'timespan',
		'System.Type'  : 'type',
		'System.Array' : 'array',
		'System.Text.RegularExpressions.Regex' : 'regex'
		}
	
	static _reverseTypeConversionTable as Hashtable
	
	static ReverseTypeConversionTable:
		get:
			if _reverseTypeConversionTable == null:
				_reverseTypeConversionTable = Hashtable()
				for e as DictionaryEntry in _typeConversionTable:
					_reverseTypeConversionTable.Add(e.Value, e.Key)
			return _reverseTypeConversionTable
	
	
	private def ModifierIsSet(modifier as ModifierEnum, query as ModifierEnum) as bool:
		return (modifier & query) == query
	
	override def Convert(modifier as ModifierEnum, conversionFlags as ConversionFlags) as string:
		if ShowAccessibility(conversionFlags):
			if ModifierIsSet(modifier, ModifierEnum.Public):
				return 'public '
			elif ModifierIsSet(modifier, ModifierEnum.Private):
				return 'private '
			elif ModifierIsSet(modifier, ModifierEnum.ProtectedAndInternal):
				return 'protected internal '
			elif ModifierIsSet(modifier, ModifierEnum.ProtectedOrInternal):
				return 'internal protected '
			elif ModifierIsSet(modifier, ModifierEnum.Internal):
				return 'internal '
			elif ModifierIsSet(modifier, ModifierEnum.Protected):
				return 'protected '
		return ''
	
	private def GetModifier(decoration as IDecoration, conversionFlags as ConversionFlags) as string:
		ret as string = ''
		if IncludeHTMLMarkup(conversionFlags) or IncludePangoMarkup(conversionFlags):
			ret += '<i>'
		
		if decoration.IsStatic:
			ret += 'static '
		elif decoration.IsFinal:
			ret += 'final '
		elif decoration.IsVirtual:
			ret += 'virtual '
		elif decoration.IsOverride:
			ret += 'override '
		elif decoration.IsNew:
			ret += 'new '
		
		if IncludeHTMLMarkup(conversionFlags) or IncludePangoMarkup(conversionFlags):
			ret += '</i>'
		
		return ret
	
	override def Convert(c as IClass, conversionFlags as ConversionFlags, resolver as ITypeNameResolver) as string:
		builder as StringBuilder = StringBuilder()
		builder.Append(Convert(c.Modifiers, conversionFlags))

		cType = c.ClassType
		
		if ShowClassModifiers(conversionFlags):
			if c.IsSealed:
				if cType == ClassType.Delegate or cType == ClassType.Enum:
					pass
				else:
					//builder.Append('final ')
					AppendPangoHtmlTag (builder, 'final ', 'i', conversionFlags)
			elif c.IsAbstract and cType != ClassType.Interface:
				//builder.Append('abstract ')
				AppendPangoHtmlTag (builder, 'abstract ', 'i', conversionFlags)
		
		if ShowClassModifiers(conversionFlags):
			if cType == ClassType.Delegate:
				builder.Append('callable ')
			elif cType == ClassType.Class:
				builder.Append('class ')
			elif cType == ClassType.Struct:
				builder.Append('struct ')
			elif cType == ClassType.Interface:
				builder.Append('interface ')
			elif cType == ClassType.Enum:
				builder.Append('enum ')
		
		if cType == ClassType.Delegate and c.Methods.Count > 0:
			for m as IMethod in c.Methods:
				if m.Name == 'Invoke':
					builder.Append(Convert(m.ReturnType, conversionFlags))
					builder.Append(' ')
		
		if UseFullyQualifiedMemberNames(conversionFlags):
			AppendPangoHtmlTag (builder, c.FullyQualifiedName, 'b', conversionFlags)
		else:
			AppendPangoHtmlTag (builder, c.Name, 'b', conversionFlags)
		
		if c.ClassType == ClassType.Delegate:
			builder.Append(' (')
			if IncludeHTMLMarkup(conversionFlags):
				builder.Append('<br>')
			
			for m as IMethod in c.Methods:
				if m.Name == 'Invoke':
					for i in range(m.Parameters.Count):
						if IncludeHTMLMarkup(conversionFlags):
							builder.Append('&nbsp;&nbsp;&nbsp;')
						
						builder.Append(Convert(m.Parameters[i], conversionFlags))
						if i + 1 < m.Parameters.Count:
							builder.Append(', ')
						
						if IncludeHTMLMarkup:
							builder.Append('<br>')
			
			builder.Append(Char.Parse(')'))
		elif ShowInheritanceList(conversionFlags):
			if c.BaseTypes.Count > 0:
				builder.Append('(')
				for i in range(c.BaseTypes.Count):
					builder.Append(c.BaseTypes[i])
					if i + 1 < c.BaseTypes.Count:
						builder.Append(', ')
				builder.Append(')')
		
		if IncludeBodies(conversionFlags):
			builder.Append(':\n')
		
		return builder.ToString()
	
	override def ConvertEnd(c as IClass, conversionFlags as ConversionFlags) as string:
		return ''
	
	override def Convert(field as IField, conversionFlags as ConversionFlags, resolver as ITypeNameResolver) as string:
		builder as StringBuilder = StringBuilder()
		builder.Append(Convert(field.Modifiers, conversionFlags))
		
		if ShowMemberModifiers(conversionFlags):
			if field.IsStatic and field.IsLiteral:
				AppendPangoHtmlTag (builder, 'const ', 'i', conversionFlags)
			elif field.IsStatic:
				AppendPangoHtmlTag (builder, 'static ', 'i', conversionFlags)
			
			if field.IsReadonly:
				AppendPangoHtmlTag (builder, 'readonly ', 'i', conversionFlags)
		
		if UseFullyQualifiedMemberNames(conversionFlags):
			AppendPangoHtmlTag (builder, field.FullyQualifiedName, 'b', conversionFlags)
		else:
			AppendPangoHtmlTag (builder, field.Name, 'b', conversionFlags)
		
		if field.ReturnType != null:
			AppendPangoHtmlTag (builder, ' as ' + Convert (field.ReturnType, conversionFlags), 'b', conversionFlags)
		
		return builder.ToString()
	
	override def Convert(property as IProperty, conversionFlags as ConversionFlags, resolver as ITypeNameResolver) as string:
		builder as StringBuilder = StringBuilder()
		builder.Append(Convert(property.Modifiers, conversionFlags))
		if ShowMemberModifiers(conversionFlags):
			builder.Append(GetModifier(property, conversionFlags))
		
		if UseFullyQualifiedMemberNames(conversionFlags):
			AppendPangoHtmlTag (builder, property.FullyQualifiedName, 'b', conversionFlags)
		else:
			AppendPangoHtmlTag (builder, property.Name, 'b', conversionFlags)
		
		if property.Parameters.Count > 0:
			builder.Append('(')
			if IncludeHTMLMarkup(conversionFlags):
				builder.Append('<br>')
			
			for i in range(property.Parameters.Count):
				if IncludeHTMLMarkup(conversionFlags):
					builder.Append('&nbsp;&nbsp;&nbsp;')
				
				builder.Append(Convert(property.Parameters[i], conversionFlags))
				if i + 1 < property.Parameters.Count:
					builder.Append(', ')
				
				if IncludeHTMLMarkup(conversionFlags):
					builder.Append('<br>')
			
			builder.Append(')')
		
		if property.ReturnType != null:
			builder.Append(' as ')
			builder.Append(Convert(property.ReturnType, conversionFlags))
		
		if IncludeBodies(conversionFlags):
			builder.Append(': ')
			if property.CanGet:
				builder.Append('get ')
			
			if property.CanSet:
				builder.Append('set ')
		
		return builder.ToString()
	
	override def Convert(e as IEvent, conversionFlags as ConversionFlags, resolver as ITypeNameResolver) as string:
		builder as StringBuilder = StringBuilder()
		builder.Append(Convert(e.Modifiers, conversionFlags))
		if ShowMemberModifiers(conversionFlags):
			builder.Append(GetModifier(e, conversionFlags))
		
		if UseFullyQualifiedMemberNames(conversionFlags):
			AppendPangoHtmlTag (builder, e.FullyQualifiedName, 'b', conversionFlags)
		else:
			AppendPangoHtmlTag (builder, e.Name, 'b', conversionFlags)
		
		if e.ReturnType != null:
			builder.Append(' as ')
			builder.Append(Convert(e.ReturnType, conversionFlags))
		
		return builder.ToString()
	
	override def Convert(m as IIndexer, conversionFlags as ConversionFlags, resolver as ITypeNameResolver) as string:
		builder as StringBuilder = StringBuilder()
		builder.Append(Convert(m.Modifiers, conversionFlags))
		
		if ShowMemberModifiers(conversionFlags) and m.IsStatic:
			AppendPangoHtmlTag (builder, 'static ', 'i', conversionFlags)
		
		if m.ReturnType != null:
			builder.Append(Convert(m.ReturnType, conversionFlags))
			builder.Append(' ')
		
		if UseFullyQualifiedMemberNames(conversionFlags):
			AppendPangoHtmlTag (builder, m.FullyQualifiedName, 'b', conversionFlags)
		else:
			AppendPangoHtmlTag (builder, m.Name, 'b', conversionFlags)

		builder.Append('Indexer(')
		if IncludeHTMLMarkup:
			builder.Append('<br>')
		
		for i in range(m.Parameters.Count):
			if IncludeHTMLMarkup(conversionFlags):
				builder.Append('&nbsp;&nbsp;&nbsp;')
			
			builder.Append(Convert(m.Parameters[i], conversionFlags))
			if i + 1 < m.Parameters.Count:
				builder.Append(', ')
			
			if IncludeHTMLMarkup(conversionFlags):
				builder.Append('<br>')
		
		builder.Append(')')
		
		return builder.ToString()
	
	override def Convert(m as IMethod, conversionFlags as ConversionFlags, resolver as ITypeNameResolver) as string:
		builder as StringBuilder = StringBuilder()
		builder.Append(Convert(m.Modifiers, conversionFlags))
		if ShowMemberModifiers(conversionFlags):
			builder.Append(GetModifier(m, conversionFlags))
		
		//builder.Append('def ') if ShowReturnType
		
		if m.IsConstructor:
			AppendPangoHtmlTag (builder, 'constructor', 'b', conversionFlags)
		else:
			if UseFullyQualifiedMemberNames(conversionFlags):
				AppendPangoHtmlTag (builder, m.FullyQualifiedName, 'b', conversionFlags)
			else:
				AppendPangoHtmlTag (builder, m.Name, 'b', conversionFlags)
		
		builder.Append('(')
		if IncludeHTMLMarkup(conversionFlags):
			builder.Append('<br>')
		
		for i in range(m.Parameters.Count):
			if IncludeHTMLMarkup(conversionFlags):
				builder.Append('&nbsp;&nbsp;&nbsp;')
			
			builder.Append(Convert(m.Parameters[i], conversionFlags))
			if i + 1 < m.Parameters.Count:
				builder.Append(', ')
			
			if IncludeHTMLMarkup(conversionFlags):
				builder.Append('<br>')
		
		builder.Append(')')
		
		//if m.ReturnType != null and ShowReturnType and not m.IsConstructor:
		if m.ReturnType != null and not m.IsConstructor:
			builder.Append(' as ')
			builder.Append(Convert(m.ReturnType, conversionFlags))
		
		if IncludeBodies(conversionFlags):
			if m.DeclaringType != null:
				if m.DeclaringType.ClassType != ClassType.Interface:
					builder.Append(': ')
			else:
				builder.Append(': ')
			
		
		return builder.ToString()
	
	override def ConvertEnd(m as IMethod, conversionFlags as ConversionFlags) as string:
		return ''
	
	override def Convert(returnType as IReturnType, conversionFlags as ConversionFlags, resolver as ITypeNameResolver) as string:
		if returnType == null:
			return ''
		
		builder as StringBuilder = StringBuilder()
		
		for i in range(returnType.ArrayCount):
			builder.Append('(')
		
		if returnType.FullyQualifiedName != null and _typeConversionTable[returnType.FullyQualifiedName] != null:
			builder.Append(_typeConversionTable[returnType.FullyQualifiedName])
		else:
			if UseFullyQualifiedNames(conversionFlags):
				builder.Append(returnType.FullyQualifiedName)
			else:
				builder.Append(returnType.Name)
			
		
		//if linkSet:
		//	builder.Append('</a>')
		
		if returnType.PointerNestingLevel > 0:
			// Sometimes there are negative pointer nesting levels
			// (especially in exception constructors in the BCL
			for i in range(returnType.PointerNestingLevel):
				builder.Append('*')
		
		for i in range(returnType.ArrayCount):
			if returnType.ArrayDimensions[i] > 1:
				builder.Append(',')
				builder.Append(returnType.ArrayDimensions[i])
			builder.Append(')')
		
		return builder.ToString()
	
	override def Convert(param as IParameter, conversionFlags as ConversionFlags, resolver as ITypeNameResolver) as string:
		builder as StringBuilder = StringBuilder()
		
		if param.IsRef:
			AppendPangoHtmlTag (builder, 'ref ', 'i', conversionFlags)
		elif param.IsOut:
			AppendPangoHtmlTag (builder, 'out ', 'i', conversionFlags)
		elif param.IsParams:
			AppendPangoHtmlTag (builder, 'params ', 'i', conversionFlags)
		
		if ShowParameterNames(conversionFlags):
			builder.Append(param.Name)
			builder.Append(' as ')
		builder.Append(Convert(param.ReturnType, conversionFlags))
		
		return builder.ToString()

	override def Convert(localVariable as LocalVariable, conversionFlags as ConversionFlags, resolver as ITypeNameResolver) as string:
		builder as StringBuilder = StringBuilder()
					
		builder.Append(localVariable.Name)
		builder.Append(' as ')
		builder.Append(Convert(localVariable.ReturnType, conversionFlags))
		
		return builder.ToString()

	private def AppendPangoHtmlTag (sb as StringBuilder, text as string, tag as string, conversionFlags as ConversionFlags):
		sb.Append ('<').Append (tag).Append ('>') if IncludeHTMLMarkup(conversionFlags) or IncludePangoMarkup(conversionFlags)
		sb.Append (text)
		sb.Append ('</').Append (tag).Append ('>') if IncludeHTMLMarkup(conversionFlags) or IncludePangoMarkup(conversionFlags)
	
	override def WrapAttribute(attribute as string) as string:
		return '[' + attribute + ']'
	
	override def WrapComment(comment as string) as string:
		return '// ' + comment
	
	override def GetIntrinsicTypeName(dotNetTypeName as string) as string:
		if _typeConversionTable[dotNetTypeName] != null:
			return _typeConversionTable[dotNetTypeName]
		return dotNetTypeName
*/