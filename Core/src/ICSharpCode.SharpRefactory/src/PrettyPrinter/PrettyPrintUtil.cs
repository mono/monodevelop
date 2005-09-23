// PrettyPrintUtil.cs
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
using System.Collections;
using System.Diagnostics;

using ICSharpCode.SharpRefactory.Parser;
using ICSharpCode.SharpRefactory.Parser.AST;

namespace ICSharpCode.SharpRefactory.PrettyPrinter
{
	/*
	public sealed class PrettyPrintUtil
	{
		PrettyPrintData prettyPrintData;
		
		public PrettyPrintUtil(PrettyPrintData prettyPrintData)
		{
			this.prettyPrintData = prettyPrintData;
		}
		
		public void PrintBlock(PrettyPrintVisitor visitor, INode blockNode, string braceStyle, string spacesBeforeBrace)
		{
			PrintBlock(visitor, blockNode, braceStyle, spacesBeforeBrace, true);
		}
		
		public void PrintBlock(PrettyPrintVisitor visitor, INode blockNode, string braceStyle, string spacesBeforeBrace, bool appendNewLine)
		{
			if (blockNode is BlockStatement && !(blockNode is SwitchStatement)) {
				int oldIndentLevel = prettyPrintData.IndentationLevel;
				
				new SpecialVisitor(prettyPrintData).VisitSpecial(blockNode.Specials["before"], false);
				
				AppendBrace((BraceStyle)prettyPrintData.Properties.GetProperty(braceStyle),
				            (bool)prettyPrintData.Properties.GetProperty(spacesBeforeBrace));
				
				if (blockNode.Children != null) {
					foreach (INode node in blockNode.Children) {
						node.AcceptVisitor(visitor, prettyPrintData);
					}
				}
				new SpecialVisitor(prettyPrintData).VisitSpecial(blockNode.Specials["after"]);
				
				AppendCloseBrace(braceStyle, oldIndentLevel, appendNewLine);
			} else {
				prettyPrintData.AppendNewLine();
				++prettyPrintData.IndentationLevel;
				blockNode.AcceptVisitor(visitor, prettyPrintData);
				--prettyPrintData.IndentationLevel;
				if (appendNewLine) {
					prettyPrintData.AppendNewLine();
				}
			}
		}
		
		
		public void PrintBlockOrStatementOnSameLine(PrettyPrintVisitor visitor, INode blockNode, string braceStyle, string spacesBeforeBrace, bool appendNewLine)
		{
			if (blockNode is BlockStatement && !(blockNode is SwitchStatement)) {
				int oldIndentLevel = prettyPrintData.IndentationLevel;
				
				new SpecialVisitor(prettyPrintData).VisitSpecial(blockNode.Specials["before"], false);
				
				AppendBrace((BraceStyle)prettyPrintData.Properties.GetProperty(braceStyle),
				            (bool)prettyPrintData.Properties.GetProperty(spacesBeforeBrace));
				
				if (blockNode.Children != null) {
					foreach (INode node in blockNode.Children) {
						node.AcceptVisitor(visitor, prettyPrintData);
					}
				}
				new SpecialVisitor(prettyPrintData).VisitSpecial(blockNode.Specials["after"]);
				
				AppendCloseBrace(braceStyle, oldIndentLevel, appendNewLine);
			} else {
				//prettyPrintData.AppendNewLine();
				//++prettyPrintData.IndentationLevel;
				blockNode.AcceptVisitor(visitor, prettyPrintData);
				//--prettyPrintData.IndentationLevel;
				if (appendNewLine) {
					prettyPrintData.AppendNewLine();
				}
			}
		}
		
		public void PrintOptionalBlock(PrettyPrintVisitor visitor, INode parentNode, string braceStyle, string spacesBeforeBrace)
		{
			Debug.Assert(parentNode.Children.Count == 1);
			PrintBlock(visitor, (INode)parentNode.Children[0], braceStyle, spacesBeforeBrace);
		}
		
		public void PrintParameterDeclarationExpression(ParameterDeclarationExpression pde)
		{
			if (pde.ParamModifiers != ParamModifiers.In) {
				prettyPrintData.AppendText(pde.ParamModifiers.ToString().ToLower());
				prettyPrintData.AppendText(" ");
			}
			
			prettyPrintData.AppendText(pde.TypeReference.Type);
			prettyPrintData.AppendText(" ");
			prettyPrintData.AppendText(pde.ParameterName);
			
		}
		
		public void PrintParameterList(ArrayList parameters, string spacesBeforeProperty, string spacesAfterProperty)
		{
			for (int i = 0; i < parameters.Count; ++i) {
				ParameterDeclarationExpression pde = (ParameterDeclarationExpression)parameters[i];
				PrintParameterDeclarationExpression(pde);
				
				PrintOptionalComma(i, parameters.Count, spacesBeforeProperty, spacesAfterProperty);
			}
		}
		
		public void PrintOptionalComma(int currentIndex, int count, string spacesBeforeProperty, string spacesAfterProperty)
		{
			if (currentIndex + 1 < count) {
				if ((bool)prettyPrintData.Properties.GetProperty("SpacesBeforeComma")) {
					prettyPrintData.AppendText(" ");
				}
				prettyPrintData.AppendText(",");
				if ((bool)prettyPrintData.Properties.GetProperty("SpacesAfterComma")) {
					prettyPrintData.AppendText(" ");
				}
			}
		}
		
		public void PrintEqual()
		{
			bool insertSpaces = (bool)prettyPrintData.Properties.GetProperty("SpacesAroundAssignments");
			if (insertSpaces) {
				prettyPrintData.AppendText(" = ");
			} else {
				prettyPrintData.AppendText("=");
			}
		}
		
		public void AppendBrace(string braceStyleProperty, string putSpaceBeforeProperty)
		{
			AppendBrace((BraceStyle)prettyPrintData.Properties.GetProperty(braceStyleProperty), 
			            (bool)prettyPrintData.Properties.GetProperty(putSpaceBeforeProperty));
			
		}
		
		public void AppendBrace(BraceStyle braceStyle, bool putSpaceBefore)
		{
			switch (braceStyle) {
				case BraceStyle.EndOfLine:
					if (putSpaceBefore) {
						prettyPrintData.AppendText(" ");
					}
					++prettyPrintData.IndentationLevel;
					prettyPrintData.AppendText("{");
					prettyPrintData.AppendNewLine();
					break;
				case BraceStyle.NextLine:
					prettyPrintData.AppendNewLine();
					prettyPrintData.AppendIndentation();
					++prettyPrintData.IndentationLevel;
					prettyPrintData.AppendText("{");
					prettyPrintData.AppendNewLine();
					break;
				case BraceStyle.NextLineShifted:
					prettyPrintData.AppendNewLine();
					++prettyPrintData.IndentationLevel;
					prettyPrintData.AppendIndentation();
					prettyPrintData.AppendText("{");
					prettyPrintData.AppendNewLine();
					break;
				case BraceStyle.NextLineShifted2:
					prettyPrintData.AppendNewLine();
					++prettyPrintData.IndentationLevel;
					prettyPrintData.AppendIndentation();
					++prettyPrintData.IndentationLevel;
					prettyPrintData.AppendText("{");
					prettyPrintData.AppendNewLine();
					break;
			}
		}
		
		public void AppendCloseBrace(string braceStyleProperty, int baseIndentLevel)
		{
			AppendCloseBrace((BraceStyle)prettyPrintData.Properties.GetProperty(braceStyleProperty), 
			                 baseIndentLevel);
		}
		public void AppendCloseBrace(string braceStyleProperty, int baseIndentLevel, bool newLine)
		{
			AppendCloseBrace((BraceStyle)prettyPrintData.Properties.GetProperty(braceStyleProperty), 
			                 baseIndentLevel,
			                 newLine);
		}
		
		public void AppendCloseBrace(BraceStyle braceStyle, int baseIndentLevel)
		{
			AppendCloseBrace(braceStyle, baseIndentLevel, true);
		}
		public void AppendCloseBrace(BraceStyle braceStyle, int baseIndentLevel, bool appendNewLine)
		{
			switch (braceStyle) {
				case BraceStyle.EndOfLine:
					prettyPrintData.IndentationLevel = baseIndentLevel;
					prettyPrintData.AppendIndentation();prettyPrintData.AppendText("}");
					if (appendNewLine) {
						prettyPrintData.AppendNewLine();
					}
					break;
				case BraceStyle.NextLine:
					goto case BraceStyle.EndOfLine;
				case BraceStyle.NextLineShifted:
					prettyPrintData.IndentationLevel = baseIndentLevel + 1;
					prettyPrintData.AppendIndentation();prettyPrintData.AppendText("}");
					prettyPrintData.IndentationLevel = baseIndentLevel;
					if (appendNewLine) {
						prettyPrintData.AppendNewLine();
					}
					break;
				case BraceStyle.NextLineShifted2:
					goto case BraceStyle.NextLineShifted;
			}
		}
		
		public string GetModifierAttributes(Modifier attr)
		{
			if ((attr & Modifier.Public) == Modifier.Public) {
				return "public ";
			}
			
			if ((attr & Modifier.Private) == Modifier.Private) {
				return "private ";
			}
			
			if ((attr & (Modifier.Protected | Modifier.Internal)) == (Modifier.Protected | Modifier.Internal) ) {
				return "protected internal ";
			}
			
			if ((attr & Modifier.Internal)  == Modifier.Internal) {
				return "internal ";
			}
			
			if ((attr & Modifier.Protected) == Modifier.Protected) {
				return "protected ";
			}
			return String.Empty;
		}
		
		public string GetOperator(BinaryOperatorType type)
		{
			switch (type) {
				case BinaryOperatorType.Add:
					return "+";
				case BinaryOperatorType.BitwiseAnd:
					return "&";
				case BinaryOperatorType.BitwiseOr:
					return "|";
				case BinaryOperatorType.LogicalAnd:
					return "&&";
				case BinaryOperatorType.LogicalOr:
					return "||";
				case BinaryOperatorType.Divide:
					return "/";
				case BinaryOperatorType.GreaterThan:
					return ">";
				case BinaryOperatorType.GreaterThanOrEqual:
					return ">=";
				case BinaryOperatorType.Equality:
					return "==";
				case BinaryOperatorType.InEquality:
					return "!=";
				case BinaryOperatorType.LessThan:
					return "<";
				case BinaryOperatorType.LessThanOrEqual:
					return "<=";
				case BinaryOperatorType.Modulus:
					return "%";
				case BinaryOperatorType.Multiply:
					return "*";
				case BinaryOperatorType.Subtract:
					return "-";
				case BinaryOperatorType.ValueEquality:
					return "==";
				case BinaryOperatorType.ShiftLeft:
					return "<<";
				case BinaryOperatorType.ShiftRight:
					return ">>";
				case BinaryOperatorType.IS:
					return "is";
				case BinaryOperatorType.AS:
					return "as";
				case BinaryOperatorType.ExclusiveOr:
					return "^";
			}
			return String.Empty;
		}
		
		public bool IsLogicalOperator(BinaryOperatorType type)
		{
			return type == BinaryOperatorType.LogicalAnd ||
			       type == BinaryOperatorType.LogicalOr;
		
		}
		
		public bool IsEqualityOperator(BinaryOperatorType type)
		{
			return type == BinaryOperatorType.ValueEquality ||
			       type == BinaryOperatorType.Equality ||
			       type == BinaryOperatorType.InEquality;
		}
		
		public bool IsRelationalOperator(BinaryOperatorType type)
		{
			return type == BinaryOperatorType.GreaterThan || 
			       type == BinaryOperatorType.GreaterThanOrEqual ||
			       type == BinaryOperatorType.LessThan ||
			       type == BinaryOperatorType.LessThanOrEqual;
		}
		
		public bool IsBitWiseOperator(BinaryOperatorType type)
		{
			return type == BinaryOperatorType.BitwiseAnd || 
			       type == BinaryOperatorType.BitwiseOr ||
			       type == BinaryOperatorType.ExclusiveOr;
		}
		
		public bool IsAdditiveOperator(BinaryOperatorType type)
		{
			return type == BinaryOperatorType.Add || 
			       type == BinaryOperatorType.Subtract;
		}
		
		public bool IsMultiplicativeOperator(BinaryOperatorType type)
		{
			return type == BinaryOperatorType.Multiply || 
			       type == BinaryOperatorType.Divide ||
			       type == BinaryOperatorType.Modulus;
		}
		
		public bool IsShiftOperator(BinaryOperatorType type)
		{
			return type == BinaryOperatorType.ShiftLeft || 
			       type == BinaryOperatorType.ShiftRight;
		}
	}*/
}
