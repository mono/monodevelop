// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version>$Revision: 4481 $</version>
// </file>

namespace ICSharpCode.NRefactory.PrettyPrinter
{
	public enum BraceStyle {
		DoNotChange,
		EndOfLine,
		EndOfLineWithoutSpace,
		NextLine,
		NextLineShifted,
		NextLineShifted2
	}
	
	public enum BraceForcement {
		DoNotChange,
		RemoveBraces,
		AddBraces,
		RemoveBracesForSingleLine
	}
	
	public enum ArrayInitializerPlacement {
		AlwaysNewLine,
		AlwaysSameLine,
	}
	
	/// <summary>
	/// Description of PrettyPrintOptions.	
	/// </summary>
	public class PrettyPrintOptions : AbstractPrettyPrintOptions
	{
		#region BraceStyle
		BraceStyle namespaceBraceStyle = BraceStyle.NextLine;
		BraceStyle classBraceStyle     = BraceStyle.NextLine;
		BraceStyle interfaceBraceStyle = BraceStyle.NextLine;
		BraceStyle structBraceStyle    = BraceStyle.NextLine;
		BraceStyle enumBraceStyle      = BraceStyle.NextLine;
		
		BraceStyle constructorBraceStyle  = BraceStyle.NextLine;
		BraceStyle destructorBraceStyle   = BraceStyle.NextLine;
		BraceStyle methodBraceStyle       = BraceStyle.NextLine;
		BraceStyle anonymousMethodBraceStyle  = BraceStyle.EndOfLine;
		
		BraceStyle propertyBraceStyle     = BraceStyle.EndOfLine;
		bool      allowPropertyGetBlockInline = true;
		BraceStyle propertyGetBraceStyle  = BraceStyle.EndOfLine;
		bool      allowPropertySetBlockInline = true;
		BraceStyle propertySetBraceStyle  = BraceStyle.EndOfLine;
		
		BraceStyle eventBraceStyle        = BraceStyle.EndOfLine;
		bool      allowEventAddBlockInline = true;
		BraceStyle eventAddBraceStyle     = BraceStyle.EndOfLine;
		bool      allowEventRemoveBlockInline = true;
		BraceStyle eventRemoveBraceStyle  = BraceStyle.EndOfLine;
		
		BraceStyle statementBraceStyle = BraceStyle.EndOfLine;
		
		public BraceStyle StatementBraceStyle {
			get {
				return statementBraceStyle;
			}
			set {
				statementBraceStyle = value;
			}
		}
		
		public BraceStyle NamespaceBraceStyle {
			get {
				return namespaceBraceStyle;
			}
			set {
				namespaceBraceStyle = value;
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
		
		public BraceStyle AnonymousMethodBraceStyle {
			get {
				return anonymousMethodBraceStyle;
			}
			set {
				anonymousMethodBraceStyle = value;
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
		
		public bool AllowPropertyGetBlockInline {
			get {
				return allowPropertyGetBlockInline;
			}
			set {
				allowPropertyGetBlockInline = value;
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
		public bool AllowPropertySetBlockInline {
			get {
				return allowPropertySetBlockInline;
			}
			set {
				allowPropertySetBlockInline = value;
			}
		}
		
		public BraceStyle EventBraceStyle {
			get {
				return eventBraceStyle;
			}
			set {
				eventBraceStyle = value;
			}
		}
		
		public BraceStyle EventAddBraceStyle {
			get {
				return eventAddBraceStyle;
			}
			set {
				eventAddBraceStyle = value;
			}
		}
		public bool AllowEventAddBlockInline {
			get {
				return allowEventAddBlockInline;
			}
			set {
				allowEventAddBlockInline = value;
			}
		}
		
		public BraceStyle EventRemoveBraceStyle {
			get {
				return eventRemoveBraceStyle;
			}
			set {
				eventRemoveBraceStyle = value;
			}
		}
		public bool AllowEventRemoveBlockInline {
			get {
				return allowEventRemoveBlockInline;
			}
			set {
				allowEventRemoveBlockInline = value;
			}
		}
		#endregion
		
		#region Force Braces
		BraceForcement ifElseBraceForcement = BraceForcement.DoNotChange;
		public BraceForcement IfElseBraceForcement {
			get {
				return ifElseBraceForcement;
			}
			set {
				ifElseBraceForcement = value;
			}
		}
		
		BraceForcement forBraceForcement = BraceForcement.DoNotChange;
		public BraceForcement ForBraceForcement {
			get {
				return forBraceForcement;
			}
			set {
				forBraceForcement = value;
			}
		}
		
		BraceForcement foreachBraceForcement = BraceForcement.DoNotChange;
		public BraceForcement ForEachBraceForcement {
			get {
				return foreachBraceForcement;
			}
			set {
				foreachBraceForcement = value;
			}
		}
		
		BraceForcement whileBraceForcement = BraceForcement.DoNotChange;
		public BraceForcement WhileBraceForcement {
			get {
				return whileBraceForcement;
			}
			set {
				whileBraceForcement = value;
			}
		}
		
		BraceForcement usingBraceForcement = BraceForcement.DoNotChange;
		public BraceForcement UsingBraceForcement {
			get {
				return usingBraceForcement;
			}
			set {
				usingBraceForcement = value;
			}
		}
		
		BraceForcement fixedBraceForcement = BraceForcement.DoNotChange;
		public BraceForcement FixedBraceForcement {
			get {
				return fixedBraceForcement;
			}
			set {
				fixedBraceForcement = value;
			}
		}
		#endregion
		
		#region Indentation
		bool indentNamespaceBody = true;
		bool indentClassBody     = true;
		bool indentInterfaceBody = true;
		bool indentStructBody    = true;
		bool indentEnumBody      = true;
		
		bool indentMethodBody    = true;
		bool indentPropertyBody  = true;
		bool indentEventBody     = true;
		bool indentBlocks        = true;
		
		bool indentSwitchBody = true;
		bool indentCaseBody   = true;
		bool indentBreakStatements = true;
		
		public bool IndentBlocks {
			get {
				return indentBlocks;
			}
			set {
				indentBlocks = value;
			}
		}

		public bool IndentClassBody {
			get {
				return indentClassBody;
			}
			set {
				indentClassBody = value;
			}
		}

		public bool IndentStructBody {
			get {
				return indentStructBody;
			}
			set {
				indentStructBody = value;
			}
		}
		
		public bool IndentPropertyBody {
			get {
				return indentPropertyBody;
			}
			set {
				indentPropertyBody = value;
			}
		}
		
		public bool IndentNamespaceBody {
			get {
				return indentNamespaceBody;
			}
			set {
				indentNamespaceBody = value;
			}
		}

		public bool IndentMethodBody {
			get {
				return indentMethodBody;
			}
			set {
				indentMethodBody = value;
			}
		}

		public bool IndentInterfaceBody {
			get {
				return indentInterfaceBody;
			}
			set {
				indentInterfaceBody = value;
			}
		}

		public bool IndentEventBody {
			get {
				return indentEventBody;
			}
			set {
				indentEventBody = value;
			}
		}
	
		public bool IndentEnumBody {
			get {
				return indentEnumBody;
			}
			set {
				indentEnumBody = value;
			}
		}
		
		public bool IndentBreakStatements {
			get {
				return indentBreakStatements;
			}
			set {
				indentBreakStatements = value;
			}
		}
		
		public bool IndentSwitchBody {
			get {
				return indentSwitchBody;
			}
			set {
				indentSwitchBody = value;
			}
		}

		public bool IndentCaseBody {
			get {
				return indentCaseBody;
			}
			set {
				indentCaseBody = value;
			}
		}

		#endregion
		
		#region NewLines
		public bool PlaceCatchOnNewLine { get; set; }
		public bool PlaceFinallyOnNewLine { get; set; }
		public bool PlaceElseOnNewLine { get; set; }
		public bool PlaceElseIfOnNewLine { get; set; }
		public bool PlaceNonBlockElseOnNewLine { get; set; }
		public bool PlaceWhileOnNewLine { get; set; }
		public ArrayInitializerPlacement PlaceArrayInitializersOnNewLine { get; set; }
		#endregion
		
		#region Spaces
		
		#region Before Parentheses
		bool spaceBeforeMethodCallParentheses        = false;
		bool spaceBeforeDelegateDeclarationParentheses = false;
		bool spaceBeforeMethodDeclarationParentheses = false;
		bool spaceBeforeConstructorDeclarationParentheses = false;
		
		bool ifParentheses      = true;
		bool whileParentheses   = true;
		bool forParentheses     = true;
		bool foreachParentheses = true;
		bool catchParentheses   = true;
		bool switchParentheses  = true;
		bool lockParentheses    = true;
		bool usingParentheses   = true;
		bool fixedParentheses   = true;
		bool sizeOfParentheses  = false;
		bool typeOfParentheses  = false;
		bool checkedParentheses  = false;
		bool uncheckedParentheses  = false;
		bool newParentheses  = false;
		
		public bool CheckedParentheses {
			get {
				return checkedParentheses;
			}
			set {
				checkedParentheses = value;
			}
		}
		public bool SpaceBeforeNewParentheses {
			get {
				return newParentheses;
			}
			set {
				newParentheses = value;
			}
		}
		public bool SizeOfParentheses {
			get {
				return sizeOfParentheses;
			}
			set {
				sizeOfParentheses = value;
			}
		}
		public bool TypeOfParentheses {
			get {
				return typeOfParentheses;
			}
			set {
				typeOfParentheses = value;
			}
		}
		public bool UncheckedParentheses {
			get {
				return uncheckedParentheses;
			}
			set {
				uncheckedParentheses = value;
			}
		}
		
		public bool SpaceBeforeConstructorDeclarationParentheses {
			get {
				return spaceBeforeConstructorDeclarationParentheses;
			}
			set {
				spaceBeforeConstructorDeclarationParentheses = value;
			}
		}
		
		public bool SpaceBeforeDelegateDeclarationParentheses {
			get {
				return spaceBeforeDelegateDeclarationParentheses;
			}
			set {
				spaceBeforeDelegateDeclarationParentheses = value;
			}
		}
		
		public bool SpaceBeforeMethodCallParentheses {
			get {
				return spaceBeforeMethodCallParentheses;
			}
			set {
				spaceBeforeMethodCallParentheses = value;
			}
		}
		
		public bool SpaceBeforeMethodDeclarationParentheses {
			get {
				return spaceBeforeMethodDeclarationParentheses;
			}
			set {
				spaceBeforeMethodDeclarationParentheses = value;
			}
		}
		
		public bool SpaceBeforeIfParentheses {
			get {
				return ifParentheses;
			}
			set {
				ifParentheses = value;
			}
		}
		
		public bool SpaceBeforeWhileParentheses {
			get {
				return whileParentheses;
			}
			set {
				whileParentheses = value;
			}
		}
		public bool SpaceBeforeForeachParentheses {
			get {
				return foreachParentheses;
			}
			set {
				foreachParentheses = value;
			}
		}
		public bool SpaceBeforeLockParentheses {
			get {
				return lockParentheses;
			}
			set {
				lockParentheses = value;
			}
		}
		public bool SpaceBeforeUsingParentheses {
			get {
				return usingParentheses;
			}
			set {
				usingParentheses = value;
			}
		}
		
		public bool SpaceBeforeCatchParentheses {
			get {
				return catchParentheses;
			}
			set {
				catchParentheses = value;
			}
		}
		public bool FixedParentheses {
			get {
				return fixedParentheses;
			}
			set {
				fixedParentheses = value;
			}
		}
		public bool SpaceBeforeSwitchParentheses {
			get {
				return switchParentheses;
			}
			set {
				switchParentheses = value;
			}
		}
		public bool SpaceBeforeForParentheses {
			get {
				return forParentheses;
			}
			set {
				forParentheses = value;
			}
		}
		
		#endregion
		
		#region AroundOperators
		bool aroundAssignmentParentheses = true;
		bool aroundLogicalOperatorParentheses = true;
		bool aroundEqualityOperatorParentheses = true;
		bool aroundRelationalOperatorParentheses = true;
		bool aroundBitwiseOperatorParentheses = true;
		bool aroundAdditiveOperatorParentheses = true;
		bool aroundMultiplicativeOperatorParentheses = true;
		bool aroundShiftOperatorParentheses = true;
		
		public bool SpaceAroundAdditiveOperator {
			get {
				return aroundAdditiveOperatorParentheses;
			}
			set {
				aroundAdditiveOperatorParentheses = value;
			}
		}
		public bool SpaceAroundAssignment {
			get {
				return aroundAssignmentParentheses;
			}
			set {
				aroundAssignmentParentheses = value;
			}
		}
		public bool SpaceAroundBitwiseOperator {
			get {
				return aroundBitwiseOperatorParentheses;
			}
			set {
				aroundBitwiseOperatorParentheses = value;
			}
		}
		public bool SpaceAroundEqualityOperator {
			get {
				return aroundEqualityOperatorParentheses;
			}
			set {
				aroundEqualityOperatorParentheses = value;
			}
		}
		public bool SpaceAroundLogicalOperator {
			get {
				return aroundLogicalOperatorParentheses;
			}
			set {
				aroundLogicalOperatorParentheses = value;
			}
		}
		public bool SpaceAroundMultiplicativeOperator {
			get {
				return aroundMultiplicativeOperatorParentheses;
			}
			set {
				aroundMultiplicativeOperatorParentheses = value;
			}
		}
		public bool SpaceAroundRelationalOperator {
			get {
				return aroundRelationalOperatorParentheses;
			}
			set {
				aroundRelationalOperatorParentheses = value;
			}
		}
		public bool SpaceAroundShiftOperator {
			get {
				return aroundShiftOperatorParentheses;
			}
			set {
				aroundShiftOperatorParentheses = value;
			}
		}
		#endregion
		
		#region SpacesWithinParentheses
		bool withinCheckedExpressionParantheses = false;
		public bool SpacesWithinCheckedExpressionParantheses {
			get {
				return withinCheckedExpressionParantheses;
			}
			set {
				withinCheckedExpressionParantheses = value;
			}
		}
		
		bool withinTypeOfParentheses = false;
		public bool SpacesWithinTypeOfParentheses {
			get {
				return withinTypeOfParentheses;
			}
			set {
				withinTypeOfParentheses = value;
			}
		}
		
		bool withinSizeOfParentheses = false;
		public bool SpacesWithinSizeOfParentheses {
			get {
				return withinSizeOfParentheses;
			}
			set {
				withinSizeOfParentheses = value;
			}
		}
		
		bool withinCastParentheses = false;
		public bool SpacesWithinCastParentheses {
			get {
				return withinCastParentheses;
			}
			set {
				withinCastParentheses = value;
			}
		}
		
		bool withinUsingParentheses = false;
		public bool SpacesWithinUsingParentheses {
			get {
				return withinUsingParentheses;
			}
			set {
				withinUsingParentheses = value;
			}
		}
		
		bool withinLockParentheses = false;
		public bool SpacesWithinLockParentheses {
			get {
				return withinLockParentheses;
			}
			set {
				withinLockParentheses = value;
			}
		}
		
		bool withinSwitchParentheses = false;
		public bool SpacesWithinSwitchParentheses {
			get {
				return withinSwitchParentheses;
			}
			set {
				withinSwitchParentheses = value;
			}
		}
		
		bool withinCatchParentheses = false;
		public bool SpacesWithinCatchParentheses {
			get {
				return withinCatchParentheses;
			}
			set {
				withinCatchParentheses = value;
			}
		}
		
		bool withinForEachParentheses = false;
		public bool SpacesWithinForEachParentheses {
			get {
				return withinForEachParentheses;
			}
			set {
				withinForEachParentheses = value;
			}
		}
		
		bool withinForParentheses = false;
		public bool SpacesWithinForParentheses {
			get {
				return withinForParentheses;
			}
			set {
				withinForParentheses = value;
			}
		}
		
		bool withinWhileParentheses = false;
		public bool SpacesWithinWhileParentheses {
			get {
				return withinWhileParentheses;
			}
			set {
				withinWhileParentheses = value;
			}
		}
		
		bool withinIfParentheses = false;
		public bool SpacesWithinIfParentheses {
			get {
				return withinIfParentheses;
			}
			set {
				withinIfParentheses = value;
			}
		}
		
		bool spacesWithinMethodDeclarationParentheses = false;
		public bool SpacesWithinMethodDeclarationParentheses {
			get {
				return spacesWithinMethodDeclarationParentheses;
			}
			set {
				spacesWithinMethodDeclarationParentheses = value;
			}
		}
		
		bool spacesWithinMethodCallParentheses = false;
		public bool SpacesWithinMethodCallParentheses {
			get {
				return spacesWithinMethodCallParentheses;
			}
			set {
				spacesWithinMethodCallParentheses = value;
			}
		}
		
		bool withinParentheses = false;
		public bool SpacesWithinParentheses {
			get {
				return withinParentheses;
			}
			set {
				withinParentheses = value;
			}
		}
		
		#endregion
		
		#region SpacesInConditionalOperator
		bool conditionalOperatorBeforeConditionSpace = true;
		bool conditionalOperatorAfterConditionSpace = true;
		
		bool conditionalOperatorBeforeSeparatorSpace = true;
		bool conditionalOperatorAfterSeparatorSpace = true;
		
		public bool SpaceAfterConditionalOperatorCondition {
			get {
				return conditionalOperatorAfterConditionSpace;
			}
			set {
				conditionalOperatorAfterConditionSpace = value;
			}
		}
		public bool SpaceAfterConditionalOperatorSeparator {
			get {
				return conditionalOperatorAfterSeparatorSpace;
			}
			set {
				conditionalOperatorAfterSeparatorSpace = value;
			}
		}
		public bool SpaceBeforeConditionalOperatorCondition {
			get {
				return conditionalOperatorBeforeConditionSpace;
			}
			set {
				conditionalOperatorBeforeConditionSpace = value;
			}
		}
		public bool SpaceBeforeConditionalOperatorSeparator {
			get {
				return conditionalOperatorBeforeSeparatorSpace;
			}
			set {
				conditionalOperatorBeforeSeparatorSpace = value;
			}
		}
		#endregion

		#region OtherSpaces
		bool spacesAfterComma     = true;
		public bool SpacesAfterComma {
			get {
				return spacesAfterComma;
			}
			set {
				spacesAfterComma = value;
			}
		}
		
		bool spacesAfterSemicolon = true;
		public bool SpacesAfterSemicolon {
			get {
				return spacesAfterSemicolon;
			}
			set {
				spacesAfterSemicolon = value;
			}
		}
		
		bool spacesAfterTypecast  = false;
		public bool SpaceAfterTypecast {
			get {
				return spacesAfterTypecast;
			}
			set {
				spacesAfterTypecast = value;
			}
		}
		
		bool spacesBeforeComma    = false;
		public bool SpacesBeforeComma {
			get {
				return spacesBeforeComma;
			}
			set {
				spacesBeforeComma = value;
			}
		}
		
		bool spacesWithinBrackets = false;
		public bool SpacesWithinBrackets {
			get {
				return spacesWithinBrackets;
			}
			set {
				spacesWithinBrackets = value;
			}
		}
		#endregion
		#endregion
		
		public PrettyPrintOptions ()
		{
			PlaceNonBlockElseOnNewLine = true;
			PlaceElseIfOnNewLine = true;
		}
	}
}