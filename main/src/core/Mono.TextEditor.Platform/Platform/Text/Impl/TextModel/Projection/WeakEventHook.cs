namespace Microsoft.VisualStudio.Text.Projection.Implementation
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Text;

    using Microsoft.VisualStudio.Text.Implementation;
    using Microsoft.VisualStudio.Utilities;
    using Microsoft.VisualStudio.Text.Differencing;
    using Microsoft.VisualStudio.Text.Utilities;
    using System.Collections.ObjectModel;

    /// <summary>
    /// This class is intended to manage the all the references from a projected buffer (the source buffer) back to the
    /// projection buffer (the target buffer) doing the projection. You can have a scenario where someone creates a projection
    /// buffer and then forgets about it. Unfortunately the projection buffer is kept alive by the events it has hooked on
    /// the source buffer so we get a memory leak.
    /// 
    /// The fix here is to gather all the references into one place so that we don't keep the target buffer alive
    /// simply to maintain state no one is interested in.
    /// </summary>
    internal class WeakEventHook
    {
        private readonly WeakReference<BaseProjectionBuffer> _targetBuffer;
        private BaseBuffer _sourceBuffer;

        public WeakEventHook(BaseProjectionBuffer targetBuffer, BaseBuffer sourceBuffer)
        {
            _targetBuffer = new WeakReference<BaseProjectionBuffer>(targetBuffer);
            _sourceBuffer = sourceBuffer;

            sourceBuffer.ChangedImmediate += OnSourceTextChanged;
            sourceBuffer.ContentTypeChangedImmediate += OnSourceBufferContentTypeChanged;
            sourceBuffer.ReadOnlyRegionsChanged += OnSourceBufferReadOnlyRegionsChanged;
        }

        public BaseBuffer SourceBuffer {  get { return _sourceBuffer; } }

        public BaseProjectionBuffer GetTargetBuffer()  // Not a property since it has side-effects
        {
            BaseProjectionBuffer targetBuffer;
            if (_targetBuffer.TryGetTarget(out targetBuffer))
            {
                return targetBuffer;
            }

            // The target buffer that was listening to events on the source buffer has died (no one was using it).
            // Dead buffers tell no tales so they get to stop listening to tales as well. Unsubscribe from the
            // events it hooked on the source buffer.
            this.UnsubscribeFromSourceBuffer();
            return null;
        }

        private void OnSourceTextChanged(object sender, TextContentChangedEventArgs e)
        {
            BaseProjectionBuffer targetBuffer = this.GetTargetBuffer();
            if (targetBuffer != null)
            {
                targetBuffer.OnSourceTextChanged(sender, e);
            }
        }

        private void OnSourceBufferContentTypeChanged(object sender, ContentTypeChangedEventArgs e)
        {
            BaseProjectionBuffer targetBuffer = this.GetTargetBuffer();
            if (targetBuffer != null)
            {
                targetBuffer.OnSourceBufferContentTypeChanged(sender, e);
            }
        }

        private void OnSourceBufferReadOnlyRegionsChanged(object sender, SnapshotSpanEventArgs e)
        {
            BaseProjectionBuffer targetBuffer = this.GetTargetBuffer();
            if (targetBuffer != null)
            {
                targetBuffer.OnSourceBufferReadOnlyRegionsChanged(sender, e);
            }
        }

        public void UnsubscribeFromSourceBuffer()
        {
            if (_sourceBuffer != null)
            {
                _sourceBuffer.ChangedImmediate -= OnSourceTextChanged;
                _sourceBuffer.ContentTypeChangedImmediate -= OnSourceBufferContentTypeChanged;
                _sourceBuffer.ReadOnlyRegionsChanged -= OnSourceBufferReadOnlyRegionsChanged;

                _sourceBuffer = null;
            }
        }
    }
}
