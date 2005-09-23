// OutputFormatter.cs
// Copyright (C) 2003 Mike Krueger (mike@icsharpcode.net)
// 
// This program is free software; you can redistribute it and/or
// modify it under the terms of the GNU General Public License
// as published by the Free Software Foundation; either version 2
// of the License, or (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place - Suite 330, Boston, MA  02111-1307, USA.

using System;
using System.Text;
using System.Collections;
using System.Diagnostics;

using ICSharpCode.SharpRefactory.Parser;
using ICSharpCode.SharpRefactory.Parser.AST;

namespace ICSharpCode.SharpRefactory.PrettyPrinter
{
	public enum BraceStyle {
		EndOfLine,
		NextLine,
		NextLineShifted,
		NextLineShifted2
	}
	
	/// <summary>
	/// Description of PrettyPrintOptions.	
	/// </summary>
	public class PrettyPrintOptions
	{
		char indentationChar = '\t';
		int  tabSize         = 4;
		int  indentSize      = 4;
		
		BraceStyle nameSpaceBraceStyle = BraceStyle.NextLine;
		BraceStyle classBraceStyle     = BraceStyle.NextLine;
		BraceStyle interfaceBraceStyle = BraceStyle.NextLine;
		BraceStyle structBraceStyle    = BraceStyle.NextLine;
		BraceStyle enumBraceStyle      = BraceStyle.NextLine;
		
		BraceStyle constructorBraceStyle  = BraceStyle.NextLine; // was EndOfLine
		BraceStyle destructorBraceStyle   = BraceStyle.NextLine; // was EndOfLine
		BraceStyle methodBraceStyle       = BraceStyle.NextLine; // was EndOfLine
		
		BraceStyle propertyBraceStyle     = BraceStyle.EndOfLine;
		BraceStyle propertyGetBraceStyle  = BraceStyle.EndOfLine;
		BraceStyle propertySetBraceStyle  = BraceStyle.EndOfLine;
		
		public char IndentationChar {
			get {
				return indentationChar;
			}
			set {
				indentationChar = value;
			}
		}
		
		public int TabSize {
			get {
				return tabSize;
			}
			set {
				tabSize = value;
			}
		}
		
		public int IndentSize {
			get {
				return indentSize;
			}
			set {
				indentSize = value;
			}
		}
		
		public BraceStyle NameSpaceBraceStyle {
			get {
				return nameSpaceBraceStyle;
			}
			set {
				nameSpaceBraceStyle = value;
			}
		}
		
		public BraceStyle ClassBraceStyle {
			get {
				return classBraceStyle;
			}
			set {
				classBraceStyle = value;
			}
		}
		
		public BraceStyle InterfaceBraceStyle {
			get {
				return interfaceBraceStyle;
			}
			set {
				interfaceBraceStyle = value;
			}
		}
		
		public BraceStyle StructBraceStyle {
			get {
				return structBraceStyle;
			}
			set {
				structBraceStyle = value;
			}
		}
		
		public BraceStyle EnumBraceStyle {
			get {
				return enumBraceStyle;
			}
			set {
				enumBraceStyle = value;
			}
		}
		
		
		public BraceStyle ConstructorBraceStyle {
			get {
				return constructorBraceStyle;
			}
			set {
				constructorBraceStyle = value;
			}
		}
		
		public BraceStyle DestructorBraceStyle {
			get {
				return destructorBraceStyle;
			}
			set {
				destructorBraceStyle = value;
			}
		}
		
		public BraceStyle MethodBraceStyle {
			get {
				return methodBraceStyle;
			}
			set {
				methodBraceStyle = value;
			}
		}
		
		public BraceStyle PropertyBraceStyle {
			get {
				return propertyBraceStyle;
			}
			set {
				propertyBraceStyle = value;
			}
		}
		public BraceStyle PropertyGetBraceStyle {
			get {
				return propertyGetBraceStyle;
			}
			set {
				propertyGetBraceStyle = value;
			}
		}
		public BraceStyle PropertySetBraceStyle {
			get {
				return propertySetBraceStyle;
			}
			set {
				propertySetBraceStyle = value;
			}
		}
		
		
	}
}
