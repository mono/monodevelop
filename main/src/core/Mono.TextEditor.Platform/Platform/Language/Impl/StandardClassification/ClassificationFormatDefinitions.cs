//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
// This file contain implementations details that are subject to change without notice.
// Use at your own risk.
//
namespace Microsoft.VisualStudio.Language.StandardClassification.Implementation
{
    using System.ComponentModel.Composition;
    using System.Windows.Media;

    using Microsoft.VisualStudio.Utilities;
    using Microsoft.VisualStudio.Text.Classification;

    // =========== Classification formats ================

    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = PredefinedClassificationTypeNames.PeekBackground)]
    [Name("Peek Background")]
    [UserVisible(true)]
    [Order(After = LanguagePriority.NaturalLanguage, Before = LanguagePriority.FormalLanguage)]
    internal class PeekBackgroundClassificationFormatDefinition : ClassificationFormatDefinition
    {
        public PeekBackgroundClassificationFormatDefinition()
        {
            DisplayName = Strings.EditorFormat_PeekBackground;
            ForegroundCustomizable = false;
            BackgroundColor = Color.FromRgb(0xF2, 0xF8, 0xFC);
        }
    }

    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = PredefinedClassificationTypeNames.PeekBackgroundUnfocused)]
    [Name("Peek Background Unfocused")]
    [UserVisible(true)]
    [Order(After = LanguagePriority.NaturalLanguage, Before = LanguagePriority.FormalLanguage)]
    internal class PeekBackgroundUnfocusedClassificationFormatDefinition : ClassificationFormatDefinition
    {
        public PeekBackgroundUnfocusedClassificationFormatDefinition()
        {
            DisplayName = Strings.EditorFormat_PeekBackgroundUnfocused;
            ForegroundCustomizable = false;
            BackgroundColor = Color.FromRgb(0xEB, 0xEB, 0xEB);
        }
    }

    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = PredefinedClassificationTypeNames.PeekHistorySelected)]
    [Name("Peek History Selected")]
    [UserVisible(true)]
    [Order(After = LanguagePriority.NaturalLanguage, Before = LanguagePriority.FormalLanguage)]
    internal class PeekHistorySelectedClassificationFormatDefinition : ClassificationFormatDefinition
    {
        public PeekHistorySelectedClassificationFormatDefinition()
        {
            DisplayName = Strings.EditorFormat_PeekBreadcrumbSelected;
            ForegroundCustomizable = false;
            BackgroundColor = Color.FromRgb(0, 0x7A, 0xCC);
        }
    }

    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = PredefinedClassificationTypeNames.PeekHistoryHovered)]
    [Name("Peek History Hovered")]
    [UserVisible(true)]
    [Order(After = LanguagePriority.NaturalLanguage, Before = LanguagePriority.FormalLanguage)]
    internal class PeekHistoryHoveredClassificationFormatDefinition : ClassificationFormatDefinition
    {
        public PeekHistoryHoveredClassificationFormatDefinition()
        {
            DisplayName = Strings.EditorFormat_PeekBreadcrumbHovered;
            ForegroundCustomizable = false;
            BackgroundColor = Color.FromRgb(0x1C, 0x97, 0xEA);
        }
    }

    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = PredefinedClassificationTypeNames.PeekFocusedBorder)]
    [Name("Peek Focused Border")]
    [UserVisible(true)]
    [Order(After = LanguagePriority.NaturalLanguage, Before = LanguagePriority.FormalLanguage)]
    internal class PeekFocusedBorderClassificationFormatDefinition : ClassificationFormatDefinition
    {
        public PeekFocusedBorderClassificationFormatDefinition()
        {
            DisplayName = Strings.EditorFormat_PeekFocusedBorder;
            BackgroundCustomizable = false;
            ForegroundColor = Color.FromRgb(0, 0x7A, 0xCC);
        }
    }

    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = PredefinedClassificationTypeNames.PeekLabelText)]
    [Name("Peek Label Text")]
    [UserVisible(true)]
    [Order(After = LanguagePriority.NaturalLanguage, Before = LanguagePriority.FormalLanguage)]
    internal class PeekLabelTextClassificationFormatDefinition: ClassificationFormatDefinition
    {
        public PeekLabelTextClassificationFormatDefinition()
        {
            DisplayName = Strings.EditorFormat_PeekLabelText;
            BackgroundCustomizable = false;
            ForegroundColor = Color.FromRgb(0, 0, 0);
        }
    }

    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = PredefinedClassificationTypeNames.PeekHighlightedText)]
    [Name("Peek Highlighted Text")]
    [UserVisible(true)]
    [Order(After = LanguagePriority.NaturalLanguage, Before = LanguagePriority.FormalLanguage)]
    internal class PeekHighlightedTextClassificationFormatDefinition : ClassificationFormatDefinition
    {
        public PeekHighlightedTextClassificationFormatDefinition()
        {
            DisplayName = Strings.EditorFormat_PeekHighlightedText;
            ForegroundCustomizable = false;
            BackgroundColor = Color.FromRgb(0xC1, 0xDE, 0xF1);
        }
    }

    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = PredefinedClassificationTypeNames.PeekHighlightedTextUnfocused)]
    [Name("Peek Highlighted Text Unfocused")]
    [UserVisible(true)]
    [Order(After = LanguagePriority.NaturalLanguage, Before = LanguagePriority.FormalLanguage)]
    internal class PeekHighlightedTextUnfocusedClassificationFormatDefinition : ClassificationFormatDefinition
    {
        public PeekHighlightedTextUnfocusedClassificationFormatDefinition()
        {
            DisplayName = Strings.EditorFormat_PeekHighlightedTextUnfocused;
            ForegroundCustomizable = false;
            BackgroundColor = Color.FromRgb(0xD2, 0xDA, 0xE0);
        }
    }

    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = PredefinedClassificationTypeNames.Comment)]
    [Name("Comment")]
    [UserVisible(true)]
    [Order(After = LanguagePriority.NaturalLanguage, Before = "Excluded Code")]
    internal class CommentClassificationFormatDefinition : ClassificationFormatDefinition
    {
        public CommentClassificationFormatDefinition()
        {
            DisplayName = Strings.EditorFormat_Comment;
            ForegroundColor = Color.FromRgb(0x00, 0x80, 0x00);
        }
    }

    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = PredefinedClassificationTypeNames.ExcludedCode)]
    [Name("Excluded Code")]
    [UserVisible(true)]
    [Order(After = LanguagePriority.NaturalLanguage, Before = "Identifier")]
    internal class PreprocessorExcludeClassificationFormatDefinition : ClassificationFormatDefinition
    {
        public PreprocessorExcludeClassificationFormatDefinition()
        {
            DisplayName = Strings.EditorFormat_ExcludedCode;
            ForegroundColor = Colors.Gray;
        }
    }

    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = PredefinedClassificationTypeNames.Identifier)]
    [Name("Identifier")]
    [UserVisible(true)]
    [Order(After = LanguagePriority.NaturalLanguage, Before = "Keyword")]
    internal class IdentifierClassificationFormatDefinition : ClassificationFormatDefinition
    {
        public IdentifierClassificationFormatDefinition()
        {
            DisplayName = Strings.EditorFormat_Identifier;
        }
    }

    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = PredefinedClassificationTypeNames.Keyword)]
    [Name("Keyword")]
    [UserVisible(true)]
    [Order(After = LanguagePriority.NaturalLanguage, Before = "Literal")]
    internal class KeywordClassificationFormatDefinition : ClassificationFormatDefinition
    {
        public KeywordClassificationFormatDefinition()
        {
            DisplayName = Strings.EditorFormat_Keyword;
            ForegroundColor = Color.FromRgb(0x00, 0x00, 0xFF);
        }
    }

    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = PredefinedClassificationTypeNames.PreprocessorKeyword)]
    [Name("Preprocessor Keyword")]
    [UserVisible(true)]
    [Order(After = LanguagePriority.NaturalLanguage, Before = "String")]
    internal class PreprocessorKeywordClassificationFormatDefinition : ClassificationFormatDefinition
    {
        public PreprocessorKeywordClassificationFormatDefinition()
        {
            DisplayName = Strings.EditorFormat_PreprocessorKeyword;
            ForegroundColor = Color.FromRgb(0x80, 0x80, 0x80);
        }
    }

    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = PredefinedClassificationTypeNames.Operator)]
    [Name("Operator")]
    [UserVisible(true)]
    [Order(After = LanguagePriority.NaturalLanguage, Before = "Preprocessor Keyword")]
    internal class OperatorClassificationFormatDefinition : ClassificationFormatDefinition
    {
        public OperatorClassificationFormatDefinition()
        {
            DisplayName = Strings.EditorFormat_Operator;
            ForegroundColor = Color.FromRgb(0x00, 0x00, 0x00);
        }
    }

    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = PredefinedClassificationTypeNames.Literal)]
    [Name("Literal")]
    [UserVisible(true)]
    [Order(After = LanguagePriority.NaturalLanguage, Before = "Number")]
    internal class LiteralClassificationFormatDefinition : ClassificationFormatDefinition
    {
        public LiteralClassificationFormatDefinition()
        {
            DisplayName = Strings.EditorFormat_Literal;
        }
    }

    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = PredefinedClassificationTypeNames.String)]
    [Name("String")]
    [UserVisible(true)]
    [Order(After = LanguagePriority.NaturalLanguage, Before = "SymbolDefinitionClassificationFormat")]
    internal class StringClassificationFormatDefinition : ClassificationFormatDefinition
    {
        public StringClassificationFormatDefinition()
        {
            DisplayName = Strings.EditorFormat_String;
            ForegroundColor = Color.FromRgb(0xA3, 0x15, 0x15);
        }
    }

    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = PredefinedClassificationTypeNames.Number)]
    [Name("Number")]
    [UserVisible(true)]
    [Order(After = LanguagePriority.NaturalLanguage, Before = "Operator")]
    internal class StringCharacterNumericalClassificationFormatDefinition : ClassificationFormatDefinition
    {
        public StringCharacterNumericalClassificationFormatDefinition()
        {
            DisplayName = Strings.EditorFormat_Number;
            ForegroundColor = Color.FromRgb(0x00, 0x00, 0x00);
        }
    }

    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = PredefinedClassificationTypeNames.SymbolDefinition)]
    [Name("SymbolDefinitionClassificationFormat")]
    [UserVisible(true)]
    [Order(After = LanguagePriority.NaturalLanguage, Before = "SymbolReferenceClassificationFormat")]
    internal class SymbolDefinitionClassificationFormatDefinition : ClassificationFormatDefinition
    {
        public SymbolDefinitionClassificationFormatDefinition()
        {
            DisplayName = Strings.EditorFormat_SymbolDefinition;
            ForegroundColor = Color.FromRgb(0x2B, 0x91, 0xAF);
        }
    }

    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = PredefinedClassificationTypeNames.SymbolReference)]
    [Name("SymbolReferenceClassificationFormat")]
    [UserVisible(true)]
    [Order(After = LanguagePriority.NaturalLanguage, Before = LanguagePriority.FormalLanguage)]
    internal class SymbolReferenceClassificationFormatDefinition : ClassificationFormatDefinition
    {
        public SymbolReferenceClassificationFormatDefinition()
        {
            DisplayName = Strings.EditorFormat_SymbolReference;
            IsBold = true;
            ForegroundColor = Color.FromRgb(0x00, 0x88, 0x00);
        }
    }
    
    [Export(typeof(EditorFormatDefinition))]
    [Name(LanguagePriority.NaturalLanguage)]
    [UserVisible(false)]
    [Order(After = Priority.Default, Before = LanguagePriority.FormalLanguage)]
    [ClassificationType(ClassificationTypeNames = PredefinedClassificationTypeNames.NaturalLanguage)]
    internal class NaturalLanguagePriorityClassificationFormatDefinition : ClassificationFormatDefinition
    {
    }

    [Export(typeof(EditorFormatDefinition))]
    [Name(LanguagePriority.FormalLanguage)]
    [UserVisible(false)]
    [Order(After = LanguagePriority.NaturalLanguage, Before = Priority.High)]
    [ClassificationType(ClassificationTypeNames = PredefinedClassificationTypeNames.FormalLanguage)]
    internal class FormalLanguagePriorityClassificationFormatDefinition : ClassificationFormatDefinition
    {
    }
}
