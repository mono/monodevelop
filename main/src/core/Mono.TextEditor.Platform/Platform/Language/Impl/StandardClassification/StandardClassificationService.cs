//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
// This file contain implementations details that are subject to change without notice.
// Use at your own risk.
//
namespace Microsoft.VisualStudio.Language.StandardClassification.Implementation
{
    using Microsoft.VisualStudio.Text.Classification;
    using Microsoft.VisualStudio.Utilities;
    using System.ComponentModel.Composition;

    /// <summary>
    /// Helper service to get hold of standard classifications.
    /// </summary>
    [Export(typeof(IStandardClassificationService))]
    internal sealed class StandardClassificationService : IStandardClassificationService
    {
        // =========== Classification types ================

        [Export]
        [Name(PredefinedClassificationTypeNames.NaturalLanguage)]
        internal ClassificationTypeDefinition naturalLanguageClassificationTypeDefinition;

        [Export]
        [Name(PredefinedClassificationTypeNames.FormalLanguage)]
        internal ClassificationTypeDefinition formalLanguageClassificationTypeDefinition;

        [Export]
        [Name(PredefinedClassificationTypeNames.Comment)]
        [BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
        internal ClassificationTypeDefinition commentClassificationTypeDefinition;

        [Export]
        [Name(PredefinedClassificationTypeNames.Identifier)]
        [BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
        internal ClassificationTypeDefinition identifierClassificationTypeDefinition;

        [Export]
        [Name(PredefinedClassificationTypeNames.Keyword)]
        [BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
        internal ClassificationTypeDefinition keywordClassificationTypeDefinition;

        [Export]
        [Name(PredefinedClassificationTypeNames.WhiteSpace)]
        [BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
        internal ClassificationTypeDefinition whitespaceClassificationTypeDefinition;

        [Export]
        [Name(PredefinedClassificationTypeNames.Operator)]
        [BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
        internal ClassificationTypeDefinition operatorClassificationTypeDefinition;

        [Export]
        [Name(PredefinedClassificationTypeNames.Literal)]
        [BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
        internal ClassificationTypeDefinition literalClassificationTypeDefinition;

        [Export]
        [Name(PredefinedClassificationTypeNames.String)]
        [BaseDefinition(PredefinedClassificationTypeNames.Literal)]
        internal ClassificationTypeDefinition stringClassificationTypeDefinition;

        [Export]
        [Name(PredefinedClassificationTypeNames.Character)]
        [BaseDefinition(PredefinedClassificationTypeNames.Literal)]
        internal ClassificationTypeDefinition characterClassificationTypeDefinition;

        [Export]
        [Name(PredefinedClassificationTypeNames.Number)]
        [BaseDefinition(PredefinedClassificationTypeNames.Literal)]
        internal ClassificationTypeDefinition numberClassificationTypeDefinition;

        [Export]
        [Name(PredefinedClassificationTypeNames.Other)]
        [BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
        internal ClassificationTypeDefinition otherClassificationTypeDefinition;

        [Export]
        [Name(PredefinedClassificationTypeNames.ExcludedCode)]
        [BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
        internal ClassificationTypeDefinition excludedCodeClassificationTypeDefinition;

        [Export]
        [Name(PredefinedClassificationTypeNames.PreprocessorKeyword)]
        [BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
        internal ClassificationTypeDefinition preprocessorKeywordClassificationTypeDefinition;

        [Export]
        [Name(PredefinedClassificationTypeNames.SymbolDefinition)]
        [BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
        internal ClassificationTypeDefinition symbolDefinitionClassificationTypeDefinition;

        [Export]
        [Name(PredefinedClassificationTypeNames.SymbolReference)]
        [BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
        internal ClassificationTypeDefinition symbolReferenceClassificationTypeDefinition;

        [Import]
        internal IClassificationTypeRegistryService ClassificationTypeRegistry;

        #region IStandardClassificationService Members

        public IClassificationType CharacterLiteral
        {
            get
            {
                if (this.characterLiteral == null)
                {
                    this.characterLiteral = ClassificationTypeRegistry.GetClassificationType(PredefinedClassificationTypeNames.Character);
                }
                return this.characterLiteral;
            }
        }
        IClassificationType characterLiteral;

        public IClassificationType Comment
        {
            get
            {
                if (this.comment == null)
                {
                    this.comment = ClassificationTypeRegistry.GetClassificationType(PredefinedClassificationTypeNames.Comment);
                }
                return this.comment;
            }
        }
        IClassificationType comment;

        public IClassificationType FormalLanguage
        {
            get
            {
                if (this.formalLanguage == null)
                {
                    this.formalLanguage = ClassificationTypeRegistry.GetClassificationType(PredefinedClassificationTypeNames.FormalLanguage);
                }
                return this.formalLanguage;
            }
        }
        IClassificationType formalLanguage;

        public IClassificationType Identifier
        {
            get
            {
                if (this.identifier == null)
                {
                    this.identifier = ClassificationTypeRegistry.GetClassificationType(PredefinedClassificationTypeNames.Identifier);
                }
                return this.identifier;
            }
        }
        IClassificationType identifier;

        public IClassificationType Keyword
        {
            get
            {
                if (this.keyword == null)
                {
                    this.keyword = ClassificationTypeRegistry.GetClassificationType(PredefinedClassificationTypeNames.Keyword);
                }
                return this.keyword;
            }
        }
        IClassificationType keyword;

        public IClassificationType Literal
        {
            get
            {
                if (this.literal == null)
                {
                    this.literal = ClassificationTypeRegistry.GetClassificationType(PredefinedClassificationTypeNames.Literal);
                }
                return this.literal;
            }
        }
        IClassificationType literal;

        public IClassificationType NaturalLanguage
        {
            get
            {
                if (this.naturalLanguage == null)
                {
                    this.naturalLanguage = ClassificationTypeRegistry.GetClassificationType(PredefinedClassificationTypeNames.NaturalLanguage);
                }
                return this.naturalLanguage;
            }
        }
        IClassificationType naturalLanguage;

        public IClassificationType NumberLiteral
        {
            get
            {
                if (this.numericalLiteral == null)
                {
                    this.numericalLiteral = ClassificationTypeRegistry.GetClassificationType(PredefinedClassificationTypeNames.Number);
                }
                return this.numericalLiteral;
            }
        }
        IClassificationType numericalLiteral;

        public IClassificationType Operator
        {
            get
            {
                if (this.@operator == null)
                {
                    this.@operator = ClassificationTypeRegistry.GetClassificationType(PredefinedClassificationTypeNames.Operator);
                }
                return this.@operator;
            }
        }
        IClassificationType @operator;

        public IClassificationType StringLiteral
        {
            get
            {
                if (this.stringLiteral == null)
                {
                    this.stringLiteral = ClassificationTypeRegistry.GetClassificationType(PredefinedClassificationTypeNames.String);
                }
                return this.stringLiteral;
            }
        }
        IClassificationType stringLiteral;

        public IClassificationType WhiteSpace
        {
            get
            {
                if (this.whiteSpace == null)
                {
                    this.whiteSpace = ClassificationTypeRegistry.GetClassificationType(PredefinedClassificationTypeNames.WhiteSpace);
                }
                return this.whiteSpace;
            }
        }
        IClassificationType whiteSpace;

        public IClassificationType Other
        {
            get
            {
                if (this.other == null)
                {
                    this.other = ClassificationTypeRegistry.GetClassificationType(PredefinedClassificationTypeNames.Other);
                }
                return this.other;
            }
        }
        IClassificationType other;

        public IClassificationType PreprocessorKeyword
        {
            get
            {
                if (this.preprocessorKeyword == null)
                {
                    this.preprocessorKeyword = ClassificationTypeRegistry.GetClassificationType(PredefinedClassificationTypeNames.PreprocessorKeyword);
                }
                return this.preprocessorKeyword;
            }
        }
        IClassificationType preprocessorKeyword;

        public IClassificationType ExcludedCode
        {
            get
            {
                if (this.excludedCode == null)
                {
                    this.excludedCode = ClassificationTypeRegistry.GetClassificationType(PredefinedClassificationTypeNames.ExcludedCode);
                }
                return this.excludedCode;
            }
        }
        IClassificationType excludedCode;

        public IClassificationType SymbolDefinition
        {
            get
            {
                if (this.symbolDefinition == null)
                {
                    this.symbolDefinition = ClassificationTypeRegistry.GetClassificationType(PredefinedClassificationTypeNames.SymbolDefinition);
                }
                return this.symbolDefinition;
            }
        }
        IClassificationType symbolDefinition;

        public IClassificationType SymbolReference
        {
            get
            {
                if (this.symbolReference == null)
                {
                    this.symbolReference = ClassificationTypeRegistry.GetClassificationType(PredefinedClassificationTypeNames.SymbolReference);
                }
                return this.symbolReference;
            }
        }
        IClassificationType symbolReference;

        #endregion
    }
}
