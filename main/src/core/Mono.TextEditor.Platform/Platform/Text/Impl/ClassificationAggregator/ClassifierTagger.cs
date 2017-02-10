namespace Microsoft.VisualStudio.Text.Classification.Implementation
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using Microsoft.VisualStudio.Text.Tagging;

    internal class ClassifierTagger : IAccurateTagger<ClassificationTag>, IDisposable
    {
        internal IList<IClassifier> Classifiers { get; private set; }

        internal ClassifierTagger(IList<IClassifier> classifiers)
        {
            Classifiers = classifiers;

            foreach(var classifier in classifiers)
            {
                classifier.ClassificationChanged += OnClassificationChanged;
            }
        }

        #region ITagger<ClassificationTag> members
        public IEnumerable<ITagSpan<ClassificationTag>> GetTags(NormalizedSnapshotSpanCollection spans)
        {
            foreach (IClassifier classifier in Classifiers)
            {
                foreach(var snapshotSpan in spans)
                {
                    foreach(var classificationSpan in classifier.GetClassificationSpans(snapshotSpan))
                    {
                        yield return new TagSpan<ClassificationTag>(
                                classificationSpan.Span, 
                                new ClassificationTag(classificationSpan.ClassificationType));
                    }
                }
            }
        }

        public event EventHandler<SnapshotSpanEventArgs> TagsChanged;
        #endregion

        #region IAccurateTagger<ClassificationTag> members
        public IEnumerable<ITagSpan<ClassificationTag>> GetAllTags(NormalizedSnapshotSpanCollection spans, CancellationToken cancel)
        {
            foreach (IClassifier classifier in Classifiers)
            {
                IAccurateClassifier classifier2 = classifier as IAccurateClassifier;

                foreach (var snapshotSpan in spans)
                {
                    foreach (var classificationSpan in (classifier2 != null)
                                                       ? classifier2.GetAllClassificationSpans(snapshotSpan, cancel)
                                                       : classifier.GetClassificationSpans(snapshotSpan))
                    {
                        yield return new TagSpan<ClassificationTag>(
                                classificationSpan.Span,
                                new ClassificationTag(classificationSpan.ClassificationType));
                    }
                }
            }
        }
        #endregion

        #region IDisposable members

        public void Dispose()
        {
            foreach(var classifier in Classifiers)
            {
                classifier.ClassificationChanged -= OnClassificationChanged;
            }

            Classifiers.Clear();

            GC.SuppressFinalize(this);
        }

        #endregion

        /// <summary>
        /// Handles the classification Changed events from the classifiers by turning
        /// them into TagsChanged events.
        /// </summary>
        void OnClassificationChanged(object sender, ClassificationChangedEventArgs e)
        {
            var tempEvent = TagsChanged;
            if (tempEvent != null)
                tempEvent(this, new SnapshotSpanEventArgs(e.ChangeSpan));
        }
    }
}
