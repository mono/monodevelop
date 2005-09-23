// PrettyPrintData.cs
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
using ICSharpCode.SharpRefactory.Parser;
using ICSharpCode.SharpRefactory.Parser.AST;

namespace ICSharpCode.SharpRefactory.PrettyPrinter
{
	/*
	public class PrettyPrintData
	{
		StringBuilder sourceText = new StringBuilder();
		int indentationLevel     = 0;
		IProperties properties   = new DefaultProperties();
		bool appendedNewLine = false;
		
		public bool AppendedNewLine {
			get {
				return appendedNewLine;
			}
			set {
				appendedNewLine = value;
			}
		}
		
		public int IndentationLevel {
			get {
				return indentationLevel;
			}
			set {
				indentationLevel = value;
			}
		}
		
		public IProperties Properties {
			get {
				return properties;
			}
			set {
				properties = value;
			}
		}
		
		public StringBuilder SourceText {
			get {
				return sourceText;
			}
			set {
				sourceText = value;
			}
		}
		
		
		public PrettyPrintData()
		{
			SetDefaultProperties();
		}
		
		public void AppendIndentation()
		{
			for (int i = 0; i < indentationLevel; ++i) {
				AppendText(properties.GetProperty("IndentationString").ToString());
			}
		}
		
		public void AppendNewLine()
		{
			AppendText("\n");
			appendedNewLine = true;
		}
		
		public void AppendText(string text)
		{
			sourceText.Append(text);
			appendedNewLine = false;
		}
		
		public void SetDefaultProperties()
		{
			// Indent:
			properties.SetProperty("IndentationString", "\t");
			properties.SetProperty("AlignMultilineParameter", true);
			properties.SetProperty("AlignMultilineParameterInCalls", true);
			properties.SetProperty("SpecialElseIfTreatment", false);
			properties.SetProperty("IndentCasesFromSwitch", true);
			
			// Brace Styles:
			properties.SetProperty("NamespaceBraceStyle", BraceStyle.NextLine); // I GET USED!
			properties.SetProperty("ClassBraceStyle", BraceStyle.NextLine); // I GET USED!
			properties.SetProperty("EnumBraceStyle", BraceStyle.EndOfLine); // I GET USED!
			properties.SetProperty("MethodBraceStyle", BraceStyle.NextLine);  // I GET USED!
			properties.SetProperty("PropertyBraceStyle", BraceStyle.EndOfLine); // I GET USED!
			properties.SetProperty("InnerPropertyBraceStyle", BraceStyle.EndOfLine); // I GET USED!
			properties.SetProperty("DefaultBraceStyle", BraceStyle.EndOfLine); // I GET USED!
			
			// New Line placement:
			properties.SetProperty("ElseOnNewLine", false);
			properties.SetProperty("WhileOnNewLine", false);
			properties.SetProperty("CatchOnNewLine", false);
			properties.SetProperty("FinallyOnNewLine", false);
			
			// Spaces before parentheses:
			properties.SetProperty("SpacesBeforeMethodCallParentheses", false); // I GET USED!
			properties.SetProperty("SpacesBeforeMethodDeclarationParentheses", false); // I GET USED!
			properties.SetProperty("SpacesBeforeIfParentheses", true); // I GET USED!
			properties.SetProperty("SpacesBeforeWhileParentheses", true); // I GET USED!
			properties.SetProperty("SpacesBeforeForParentheses", true);
			properties.SetProperty("SpacesBeforeCatchParentheses", true);
			properties.SetProperty("SpacesBeforeSwitchParentheses", true);
			properties.SetProperty("SpacesBeforeLockParentheses", true);
			properties.SetProperty("SpacesBeforeFixedParentheses", true);
			properties.SetProperty("SpacesBeforeUsingParentheses", true);
			
			// Spaces around operators:
			properties.SetProperty("SpacesAroundAssignments", true); // I GET USED!
			properties.SetProperty("SpacesAroundLogicalOperators", true); // I GET USED!
			properties.SetProperty("SpacesAroundEqualityOperators", true); // I GET USED!
			properties.SetProperty("SpacesAroundRelationalOperators", true); // I GET USED!
			properties.SetProperty("SpacesAroundBitWiseOperators", true); // I GET USED!
			properties.SetProperty("SpacesAroundAdditiveOperators", true); // I GET USED!
			properties.SetProperty("SpacesAroundMulticativeOperators", true); // I GET USED!
			properties.SetProperty("SpacesAroundShiftOperators", true); // I GET USED!
			properties.SetProperty("SpacesAroundOtherOperators", true); // I GET USED!
			
			// Spaces before left brace:
			properties.SetProperty("SpacesBeforeNamespaceBrace", true);// I GET USED!
			properties.SetProperty("SpacesBeforeClassBrace", true); // I GET USED!
			properties.SetProperty("SpacesBeforeEnumBrace", true); // I GET USED!
			
			properties.SetProperty("SpacesBeforeMethodDeclarationBrace", false); // I GET USED!
			
			properties.SetProperty("SpacesBeforeIfBrace", true); // I GET USED!
			properties.SetProperty("SpacesBeforeElseBrace", true); // I GET USED!
			properties.SetProperty("SpacesBeforeWhileBrace", true);
			properties.SetProperty("SpacesBeforeForBrace", true);
			properties.SetProperty("SpacesBeforeDoBrace", true);
			properties.SetProperty("SpacesBeforeSwitchBrace", true);
			properties.SetProperty("SpacesBeforeTryBrace", true);
			properties.SetProperty("SpacesBeforeCatchBrace", true);
			properties.SetProperty("SpacesBeforeFinallyBrace", true);
			
			properties.SetProperty("SpacesBeforeLockBrace", true);
			properties.SetProperty("SpacesBeforeFixedBrace", true);
			properties.SetProperty("SpacesBeforeUsingBrace", true);
			properties.SetProperty("SpacesBeforeCheckedBrace", true);
			properties.SetProperty("SpacesBeforeUncheckedBrace", true);
			properties.SetProperty("SpacesBeforeUnsafeBrace", true);
			
			properties.SetProperty("SpacesBeforePropertyBrace", true); // I GET USED!
			properties.SetProperty("SpacesBeforeGetBrace", true); // I GET USED!
			properties.SetProperty("SpacesBeforeSetBrace", true); // I GET USED!
			properties.SetProperty("SpacesBeforeEventBrace", true);
			properties.SetProperty("SpacesBeforeAddBrace", true);
			properties.SetProperty("SpacesBeforeRemoveBrace", true);
		
			// Within Parentheses:
			properties.SetProperty("SpacesWithinParentheses", false); // I GET USED!
			properties.SetProperty("SpacesWithinMethodCallParentheses", false); // I GET USED!
			properties.SetProperty("SpacesWithinMethodDeclarationParentheses", false); // I GET USED!
			properties.SetProperty("SpacesWithinIfParentheses", false);
			properties.SetProperty("SpacesWithinWhileParentheses", false);
			properties.SetProperty("SpacesWithinForParentheses", false);
			properties.SetProperty("SpacesWithinCatchParentheses", false);
			properties.SetProperty("SpacesWithinSwitchParentheses", false);
			properties.SetProperty("SpacesWithinTypeCastParentheses", false);
			properties.SetProperty("SpacesWithinLockParentheses", false);
			properties.SetProperty("SpacesWithinUsingParentheses", false);
			properties.SetProperty("SpacesWithinFixedParentheses", false);
			
			// Conditional operator:
			properties.SetProperty("SpacesInConditionalBeforeInterr", true);
			properties.SetProperty("SpacesInConditionalAfterInterr", true);
			properties.SetProperty("SpacesInConditionalBeforeColon", true);
			properties.SetProperty("SpacesInConditionalAfterColon", true);
			
			// Other spaces:
			properties.SetProperty("SpacesWithinBrackets", true);
			properties.SetProperty("SpacesBeforeComma", false);
			properties.SetProperty("SpacesAfterComma", true);
			properties.SetProperty("SpacesBeforeSemicolon", true);
			properties.SetProperty("SpacesAfterSemicolon", true);
			properties.SetProperty("SpacesAfterTypeCast", true);
		}
	}
	
	public enum BraceStyle {
		EndOfLine,
		NextLine,
		NextLineShifted,
		NextLineShifted2
	}*/
}
