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
        internal List<Lazy<IClassifierProvider, IContentTypeMetadata>> _classifierProviders { get; set; }

        [Import]
        internal GuardedOperations _guardedOperations { get; set; }

        public ITagger<T> CreateTagger<T>(ITextBuffer buffer) where T : ITag
        {
            var classifiers = 
                _guardedOperations.InvokeMatchingFactories
                    (_classifierProviders, (IClassifierProvider provider) => (provider.GetClassifier(buffer)), buffer.ContentType, this);

            return classifiers.Count > 0
                    ? new ClassifierTagger(classifiers) as ITagger<T>
                    : null;
        }
    }
}
