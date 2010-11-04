// 
// CSharpFormattingPolicy.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
//  
// Copyright (c) 2009 Novell, Inc (http://www.novell.com)
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using MonoDevelop.Core;
using MonoDevelop.Core.Serialization;
using System.Reflection;
using System.Xml;
using System.Text;
using System.Linq;

namespace MonoDevelop.CSharp.Formatting
{
	public enum BraceStyle
	{
		DoNotChange,
		EndOfLine,
		EndOfLineWithoutSpace,
		NextLine,
		NextLineShifted,
		NextLineShifted2
	}
	
	public enum BraceForcement
	{
		DoNotChange,
		RemoveBraces,
		AddBraces
	}
	
	public enum ArrayInitializerPlacement
	{
		AlwaysNewLine,
		AlwaysSameLine
	}
	
	public enum PropertyFormatting
	{
		AllowOneLine,
		ForceOneLine,
		ForceNewLine
	}
	
	public class CSharpFormattingPolicy : IEquatable<CSharpFormattingPolicy>
	{
		public string Name {
			get;
			set;
		}
		
		public bool IsBuiltIn {
			get;
			set;
		}
		
		public CSharpFormattingPolicy Clone ()
		{
			return (CSharpFormattingPolicy)MemberwiseClone ();
		}
		
		#region Indentation
		[ItemProperty]
		public bool IndentNamespaceBody { // tested
			get;
			set;
		}
		
		[ItemProperty]
		public bool IndentClassBody { // tested
			get;
			set;
		}
		
		[ItemProperty]
		public bool IndentInterfaceBody { // tested
			get;
			set;
		}
		
		[ItemProperty]
		public bool IndentStructBody { // tested
			get;
			set;
		}
		
		[ItemProperty]
		public bool IndentEnumBody { // tested
			get;
			set;
		}
		
		[ItemProperty]
		public bool IndentMethodBody { // tested
			get;
			set;
		}
		
		[ItemProperty]
		public bool IndentPropertyBody { // tested
			get;
			set;
		}
		
		[ItemProperty]
		public bool IndentEventBody { // tested
			get;
			set;
		}
		
		[ItemProperty]
		public bool IndentBlocks { // tested
			get;
			set;
		}
		
		[ItemProperty]
		public bool IndentSwitchBody { // tested
			get;
			set;
		}
		
		[ItemProperty]
		public bool IndentCaseBody { // tested
			get;
			set;
		}
		
		[ItemProperty]
		public bool IndentBreakStatements { // tested
			get;
			set;
		}
		
		[ItemProperty]
		public bool AlignEmbeddedUsingStatements { // tested
			get;
			set;
		}
		
		[ItemProperty]
		public bool AlignEmbeddedIfStatements { // tested
			get;
			set;
		}
		
		[ItemProperty]
		public PropertyFormatting PropertyFormatting { // tested
			get;
			set;
		}
		
		#endregion
		
		#region Braces
		[ItemProperty]
		public BraceStyle NamespaceBraceStyle { // tested
			get;
			set;
		}
		
		[ItemProperty]
		public BraceStyle ClassBraceStyle { // tested
			get;
			set;
		}
		
		[ItemProperty]
		public BraceStyle InterfaceBraceStyle { // tested
			get;
			set;
		}
		
		[ItemProperty]
		public BraceStyle StructBraceStyle { // tested
			get;
			set;
		}
		
		[ItemProperty]
		public BraceStyle EnumBraceStyle { // tested
			get;
			set;
		}
		
		[ItemProperty]
		public BraceStyle MethodBraceStyle { // tested
			get;
			set;
		}
		
		[ItemProperty]
		public BraceStyle AnonymousMethodBraceStyle {
			get;
			set;
		}
		
		[ItemProperty]
		public BraceStyle ConstructorBraceStyle {  // tested
			get;
			set;
		}
		
		[ItemProperty]
		public BraceStyle DestructorBraceStyle { // tested
			get;
			set;
		}
		
		[ItemProperty]
		public BraceStyle PropertyBraceStyle { // tested
			get;
			set;
		}
		
		[ItemProperty]
		public BraceStyle PropertyGetBraceStyle { // tested
			get;
			set;
		}
		
		[ItemProperty]
		public BraceStyle PropertySetBraceStyle { // tested
			get;
			set;
		}
		
		[ItemProperty]
		public bool AllowPropertyGetBlockInline { // tested
			get;
			set;
		}
		
		[ItemProperty]
		public bool AllowPropertySetBlockInline { // tested
			get;
			set;
		}
		
		[ItemProperty]
		public BraceStyle EventBraceStyle { // tested
			get;
			set;
		}
		
		[ItemProperty]
		public BraceStyle EventAddBraceStyle { // tested
			get;
			set;
		}
		
		[ItemProperty]
		public BraceStyle EventRemoveBraceStyle { // tested
			get;
			set;
		}
		
		[ItemProperty]
		public bool AllowEventAddBlockInline { // tested
			get;
			set;
		}
		
		[ItemProperty]
		public bool AllowEventRemoveBlockInline { // tested
			get;
			set;
		}
		
		[ItemProperty]
		public BraceStyle StatementBraceStyle { // tested
			get;
			set;
		}
		
		[ItemProperty]
		public bool AllowIfBlockInline {
			get;
			set;
		}
		
		#endregion
		
		#region Force Braces
		[ItemProperty]
		public BraceForcement IfElseBraceForcement { // tested
			get;
			set;
		}
		
		[ItemProperty]
		public BraceForcement ForBraceForcement { // tested
			get;
			set;
		}
		
		[ItemProperty]
		public BraceForcement ForEachBraceForcement { // tested
			get;
			set;
		}
		
		[ItemProperty]
		public BraceForcement WhileBraceForcement { // tested
			get;
			set;
		}
		
		[ItemProperty]
		public BraceForcement UsingBraceForcement { // tested
			get;
			set;
		}
		
		[ItemProperty]
		public BraceForcement FixedBraceForcement { // tested
			get;
			set;
		}
		#endregion
		
		#region NewLines
		[ItemProperty]
		public bool PlaceElseOnNewLine { // tested
			get;
			set;
		}
		
		[ItemProperty]
		public bool PlaceElseIfOnNewLine { // tested
			get;
			set;
		}
		
		[ItemProperty]
		public bool PlaceCatchOnNewLine { // tested
			get;
			set;
		}
		
		[ItemProperty]
		public bool PlaceFinallyOnNewLine { // tested
			get;
			set;
		}
		
		[ItemProperty]
		public bool PlaceWhileOnNewLine { // tested
			get;
			set;
		}
		
		[ItemProperty]
		public ArrayInitializerPlacement PlaceArrayInitializersOnNewLine {
			get;
			set;
		}
		#endregion
		
		#region Spaces
		// Methods
		[ItemProperty]
		public bool BeforeMethodDeclarationParentheses { // tested
			get;
			set;
		}
		
		[ItemProperty]
		public bool BetweenEmptyMethodDeclarationParentheses {
			get;
			set;
		}
		
		[ItemProperty]
		public bool BeforeMethodDeclarationParameterComma { // tested
			get;
			set;
		}
		
		[ItemProperty]
		public bool AfterMethodDeclarationParameterComma { // tested
			get;
			set;
		}
		
		[ItemProperty]
		public bool WithinMethodDeclarationParentheses { // tested
			get;
			set;
		}
		
		// Method calls
		[ItemProperty]
		public bool BeforeMethodCallParentheses { // tested
			get;
			set;
		}
		
		[ItemProperty]
		public bool BetweenEmptyMethodCallParentheses { // tested
			get;
			set;
		}
		
		[ItemProperty]
		public bool BeforeMethodCallParameterComma { // tested
			get;
			set;
		}
		
		[ItemProperty]
		public bool AfterMethodCallParameterComma { // tested
			get;
			set;
		}
		
		[ItemProperty]
		public bool WithinMethodCallParentheses { // tested
			get;
			set;
		}
		
		// fields
		
		[ItemProperty]
		public bool BeforeFieldDeclarationComma { // tested
			get;
			set;
		}
		
		[ItemProperty]
		public bool AfterFieldDeclarationComma { // tested
			get;
			set;
		}
		
		// local variables
		
		[ItemProperty]
		public bool BeforeLocalVariableDeclarationComma { // tested
			get;
			set;
		}
		
		[ItemProperty]
		public bool AfterLocalVariableDeclarationComma { // tested
			get;
			set;
		}
		
		// constructors
		
		[ItemProperty]
		public bool BeforeConstructorDeclarationParentheses { // tested
			get;
			set;
		}
		
		[ItemProperty]
		public bool BetweenEmptyConstructorDeclarationParentheses { // tested
			get;
			set;
		}
		
		[ItemProperty]
		public bool BeforeConstructorDeclarationParameterComma { // tested
			get;
			set;
		}
		
		[ItemProperty]
		public bool AfterConstructorDeclarationParameterComma { // tested
			get;
			set;
		}
		
		[ItemProperty]
		public bool WithinConstructorDeclarationParentheses { // tested
			get;
			set;
		}
		
		// indexer
		[ItemProperty]
		public bool BeforeIndexerDeclarationBracket { // tested
			get;
			set;
		}
		
		[ItemProperty]
		public bool WithinIndexerDeclarationBracket { // tested
			get;
			set;
		}
		
		[ItemProperty]
		public bool BeforeIndexerDeclarationParameterComma {
			get;
			set;
		}
		
		[ItemProperty]
		public bool AfterIndexerDeclarationParameterComma {
			get;
			set;
		}
		
		// delegates
		
		[ItemProperty]
		public bool BeforeDelegateDeclarationParentheses {
			get;
			set;
		}
		
		[ItemProperty]
		public bool BetweenEmptyDelegateDeclarationParentheses {
			get;
			set;
		}
		
		[ItemProperty]
		public bool BeforeDelegateDeclarationParameterComma {
			get;
			set;
		}
		
		[ItemProperty]
		public bool AfterDelegateDeclarationParameterComma {
			get;
			set;
		}
		
		[ItemProperty]
		public bool WithinDelegateDeclarationParentheses {
			get;
			set;
		}
		
		
		[ItemProperty]
		public bool NewParentheses { // tested
			get;
			set;
		}
		
		[ItemProperty]
		public bool IfParentheses { // tested
			get;
			set;
		}
		
		[ItemProperty]
		public bool WhileParentheses { // tested
			get;
			set;
		}
		
		[ItemProperty]
		public bool ForParentheses { // tested
			get;
			set;
		}
		
		[ItemProperty]
		public bool ForeachParentheses { // tested
			get;
			set;
		}
		
		[ItemProperty]
		public bool CatchParentheses { // tested
			get;
			set;
		}
		
		[ItemProperty]
		public bool SwitchParentheses { // tested
			get;
			set;
		}
		
		[ItemProperty]
		public bool LockParentheses { // tested
			get;
			set;
		}
		
		[ItemProperty]
		public bool UsingParentheses { // tested
			get;
			set;
		}
		
		[ItemProperty]
		public bool AroundAssignmentParentheses { // tested
			get;
			set;
		}
		
		[ItemProperty]
		public bool AroundLogicalOperatorParentheses { // tested
			get;
			set;
		}
		
		[ItemProperty]
		public bool AroundEqualityOperatorParentheses { // tested
			get;
			set;
		}
		
		[ItemProperty]
		public bool AroundRelationalOperatorParentheses { // tested
			get;
			set;
		}
		
		[ItemProperty]
		public bool AroundBitwiseOperatorParentheses { // tested
			get;
			set;
		}
		
		[ItemProperty]
		public bool AroundAdditiveOperatorParentheses { // tested
			get;
			set;
		}
		
		[ItemProperty]
		public bool AroundMultiplicativeOperatorParentheses { // tested
			get;
			set;
		}
		
		[ItemProperty]
		public bool AroundShiftOperatorParentheses { // tested
			get;
			set;
		}
		
		[ItemProperty]
		public bool WithinParentheses { // tested
			get;
			set;
		}
		
		
		[ItemProperty]
		public bool WithinIfParentheses { // tested
			get;
			set;
		}
		
		[ItemProperty]
		public bool WithinWhileParentheses { // tested
			get;
			set;
		}
		
		[ItemProperty]
		public bool WithinForParentheses { // tested
			get;
			set;
		}
		
		[ItemProperty]
		public bool WithinForEachParentheses { // tested
			get;
			set;
		}
		
		[ItemProperty]
		public bool WithinCatchParentheses { // tested
			get;
			set;
		}
		
		[ItemProperty]
		public bool WithinSwitchParentheses { // tested
			get;
			set;
		}
		
		[ItemProperty]
		public bool WithinLockParentheses { // tested
			get;
			set;
		}
		
		[ItemProperty]
		public bool WithinUsingParentheses { // tested
			get;
			set;
		}
		
		[ItemProperty]
		public bool WithinCastParentheses { // tested
			get;
			set;
		}
		
		[ItemProperty]
		public bool WithinSizeOfParentheses { // tested
			get;
			set;
		}
		
		[ItemProperty]
		public bool BeforeSizeOfParentheses { // tested
			get;
			set;
		}
		
		[ItemProperty]
		public bool WithinTypeOfParentheses { // tested
			get;
			set;
		}
		
		[ItemProperty]
		public bool BeforeTypeOfParentheses { // tested
			get;
			set;
		}
		
		[ItemProperty]
		public bool WithinCheckedExpressionParantheses { // tested
			get;
			set;
		}
		
		[ItemProperty]
		public bool ConditionalOperatorBeforeConditionSpace { // tested
			get;
			set;
		}
		
		[ItemProperty]
		public bool ConditionalOperatorAfterConditionSpace { // tested
			get;
			set;
		}
		
		[ItemProperty]
		public bool ConditionalOperatorBeforeSeparatorSpace { // tested
			get;
			set;
		}
		
		[ItemProperty]
		public bool ConditionalOperatorAfterSeparatorSpace { // tested
			get;
			set;
		}
		
		// brackets
		[ItemProperty]
		public bool SpacesWithinBrackets { // tested
			get;
			set;
		}
		[ItemProperty]
		public bool SpacesBeforeBrackets { // tested
			get;
			set;
		}
		[ItemProperty]
		public bool BeforeBracketComma { // tested
			get;
			set;
		}
		[ItemProperty]
		public bool AfterBracketComma { // tested
			get;
			set;
		}
		
		
		[ItemProperty]
		public bool SpacesBeforeForSemicolon { // tested
			get;
			set;
		}
		
		[ItemProperty]
		public bool SpacesAfterForSemicolon { // tested
			get;
			set;
		}
		
		[ItemProperty]
		public bool SpacesAfterTypecast { // tested
			get;
			set;
		}
		
		[ItemProperty]
		public bool SpacesBeforeArrayDeclarationBrackets { // tested
			get;
			set;
		}
		#endregion
		
		#region Blank Lines
		[ItemProperty]
		public int BlankLinesBeforeUsings {
			get;
			set;
		}
		
		[ItemProperty]
		public int BlankLinesAfterUsings {
			get;
			set;
		}
		
		[ItemProperty]
		public int BlankLinesBeforeFirstDeclaration {
			get;
			set;
		}
		
		[ItemProperty]
		public int BlankLinesBetweenTypes {
			get;
			set;
		}
		
		[ItemProperty]
		public int BlankLinesBetweenFields {
			get;
			set;
		}
		
		[ItemProperty]
		public int BlankLinesBetweenEventFields {
			get;
			set;
		}
		
		[ItemProperty]
		public int BlankLinesBetweenMembers {
			get;
			set;
		}
		#endregion
		
		public CSharpFormattingPolicy ()
		{
			IndentNamespaceBody = true;
			IndentClassBody = IndentInterfaceBody = IndentStructBody = IndentEnumBody = true;
			IndentMethodBody = IndentPropertyBody = IndentEventBody = true;
			IndentBlocks = true;
			IndentSwitchBody = false;
			IndentCaseBody = true;
			IndentBreakStatements = true;
			NamespaceBraceStyle = BraceStyle.NextLine;
			ClassBraceStyle = InterfaceBraceStyle = StructBraceStyle = EnumBraceStyle = BraceStyle.NextLine;
			MethodBraceStyle = ConstructorBraceStyle = DestructorBraceStyle = BraceStyle.NextLine;
			AnonymousMethodBraceStyle = BraceStyle.EndOfLine;

			PropertyBraceStyle = PropertyGetBraceStyle = PropertySetBraceStyle = BraceStyle.EndOfLine;
			AllowPropertyGetBlockInline = AllowPropertySetBlockInline = true;

			EventBraceStyle = EventAddBraceStyle = EventRemoveBraceStyle = BraceStyle.EndOfLine;
			AllowEventAddBlockInline = AllowEventRemoveBlockInline = true;
			StatementBraceStyle = BraceStyle.EndOfLine;

			PlaceElseOnNewLine = false;
			PlaceCatchOnNewLine = false;
			PlaceFinallyOnNewLine = false;
			PlaceWhileOnNewLine = false;
			PlaceArrayInitializersOnNewLine = ArrayInitializerPlacement.AlwaysSameLine;

			BeforeMethodCallParentheses = true;
			BeforeMethodDeclarationParentheses = true;
			BeforeConstructorDeclarationParentheses = true;
			BeforeDelegateDeclarationParentheses = true;
			AfterMethodCallParameterComma = true;
			
			NewParentheses = true;
			IfParentheses = true;
			WhileParentheses = true;
			ForParentheses = true;
			ForeachParentheses = true;
			CatchParentheses = true;
			SwitchParentheses = true;
			LockParentheses = true;
			UsingParentheses = true;
			AroundAssignmentParentheses = true;
			AroundLogicalOperatorParentheses = true;
			AroundEqualityOperatorParentheses = true;
			AroundRelationalOperatorParentheses = true;
			AroundBitwiseOperatorParentheses = true;
			AroundAdditiveOperatorParentheses = true;
			AroundMultiplicativeOperatorParentheses = true;
			AroundShiftOperatorParentheses = true;
			WithinParentheses = false;
			WithinMethodCallParentheses = false;
			WithinMethodDeclarationParentheses = false;
			WithinIfParentheses = false;
			WithinWhileParentheses = false;
			WithinForParentheses = false;
			WithinForEachParentheses = false;
			WithinCatchParentheses = false;
			WithinSwitchParentheses = false;
			WithinLockParentheses = false;
			WithinUsingParentheses = false;
			WithinCastParentheses = false;
			WithinSizeOfParentheses = false;
			WithinTypeOfParentheses = false;
			WithinCheckedExpressionParantheses = false;
			ConditionalOperatorBeforeConditionSpace = true;
			ConditionalOperatorAfterConditionSpace = true;
			ConditionalOperatorBeforeSeparatorSpace = true;
			ConditionalOperatorAfterSeparatorSpace = true;

			SpacesWithinBrackets = false;
			SpacesBeforeBrackets = true;
			BeforeBracketComma = false;
			AfterBracketComma = true;
					
			SpacesBeforeForSemicolon = false;
			SpacesAfterForSemicolon = true;
			SpacesAfterTypecast = false;
			
			AlignEmbeddedIfStatements = true;
			AlignEmbeddedUsingStatements = true;
			PropertyFormatting = PropertyFormatting.AllowOneLine;
			BeforeMethodDeclarationParameterComma = false;
			AfterMethodDeclarationParameterComma = true;
			BeforeFieldDeclarationComma = false;
			AfterFieldDeclarationComma = true;
			BeforeLocalVariableDeclarationComma = false;
			AfterLocalVariableDeclarationComma = true;
			
			BeforeIndexerDeclarationBracket = true;
			WithinIndexerDeclarationBracket = false;
			BeforeIndexerDeclarationParameterComma = false;
		
		
			AfterIndexerDeclarationParameterComma = true;
			
			BlankLinesBeforeUsings = 0;
			BlankLinesAfterUsings = 1;
			
			
			BlankLinesBeforeFirstDeclaration = 0;
			BlankLinesBetweenTypes = 1;
			BlankLinesBetweenFields = 0;
			BlankLinesBetweenEventFields = 0;
			BlankLinesBetweenMembers = 1;
		}
		
		public static CSharpFormattingPolicy Load (FilePath selectedFile)
		{
			using (var stream = System.IO.File.OpenRead (selectedFile)) {
				return Load (stream);
			}
		}
		
		public static CSharpFormattingPolicy Load (System.IO.Stream input)
		{
			CSharpFormattingPolicy result = new CSharpFormattingPolicy ();
			result.Name = "noname";
			using (XmlTextReader reader = new XmlTextReader (input)) {
				while (reader.Read ()) {
					if (reader.NodeType == XmlNodeType.Element) {
						if (reader.LocalName == "Property") {
							var info = typeof (CSharpFormattingPolicy).GetProperty (reader.GetAttribute ("name"));
							string valString = reader.GetAttribute ("value");
							object value;
							if (info.PropertyType == typeof (bool)) {
								value = Boolean.Parse (valString);
							} else if (info.PropertyType == typeof (int)) {
								value = Int32.Parse (valString);
							} else {
								value = Enum.Parse (info.PropertyType, valString);
							}
							info.SetValue (result, value, null);
						} else if (reader.LocalName == "FormattingProfile") {
							result.Name = reader.GetAttribute ("name");
						}
					} else if (reader.NodeType == XmlNodeType.EndElement && reader.LocalName == "FormattingProfile") {
						Console.WriteLine ("result:" + result.Name);
						return result;
					}
				}
			}
			return result;
		}
		
		public void Save (string fileName)
		{
			using (var writer = new XmlTextWriter (fileName, Encoding.Default)) {
				writer.Formatting = System.Xml.Formatting.Indented;
				writer.Indentation = 1;
				writer.IndentChar = '\t';
				writer.WriteStartElement ("FormattingProfile");
				writer.WriteAttributeString ("name", Name);
				foreach (PropertyInfo info in typeof (CSharpFormattingPolicy).GetProperties ()) {
					if (info.GetCustomAttributes (false).Any (o => o.GetType () == typeof(ItemPropertyAttribute))) {
						writer.WriteStartElement ("Property");
						writer.WriteAttributeString ("name", info.Name);
						writer.WriteAttributeString ("value", info.GetValue (this, null).ToString ());
						writer.WriteEndElement ();
					}
				}
				writer.WriteEndElement ();
			}
		}
		
		public bool Equals (CSharpFormattingPolicy other)
		{
			foreach (PropertyInfo info in typeof (CSharpFormattingPolicy).GetProperties ()) {
				if (info.GetCustomAttributes (false).Any (o => o.GetType () == typeof(ItemPropertyAttribute))) {
					object val = info.GetValue (this, null);
					object otherVal = info.GetValue (other, null);
					if (val == null) {
						if (otherVal == null)
							continue;
						return false;
					}
					if (!val.Equals (otherVal)) {
						//Console.WriteLine ("!equal");
						return false;
					}
				}
			}
			//Console.WriteLine ("== equal");
			return true;
		}
	}
}
