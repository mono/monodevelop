//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
// This file contain implementations details that are subject to change without notice.
// Use at your own risk.
//
namespace Microsoft.VisualStudio.Text.Classification.Implementation
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.ComponentModel.Composition;
    using Microsoft.VisualStudio.Text.Tagging;
    using Microsoft.VisualStudio.Text.Utilities;
    using Microsoft.VisualStudio.Utilities;

    [Export(typeof(ITaggerProvider))]
    [ContentType("any")]
    [TagType(typeof(ClassificationTag))]
    internal class ClassifierTaggerProvider : ITaggerProvider
    {
        [ImportMany(typeof(IClassifierProvider))]
        internal List<Lazy<IClassifierProvider, INamedContentTypeMetadata>> _classifierProviders { get; set; }

        [Import]
        internal GuardedOperations _guardedOperations { get; set; }

        [Import]
        private IContentTypeRegistryService ContentTypeRegistryService { get; set; }

        public ITagger<T> CreateTagger<T>(ITextBuffer buffer) where T : ITag
        {
            var classifiers = 
                _guardedOperations.InvokeEligibleFactories
                    (_classifierProviders, (IClassifierProvider provider) => (provider.GetClassifier(buffer)), buffer.ContentType, this.ContentTypeRegistryService, this);

            return classifiers.Count > 0
                    ? new ClassifierTagger(classifiers) as ITagger<T>
                    : null;
        }
    }
}
