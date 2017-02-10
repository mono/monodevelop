// Copyright (c) Microsoft Corporation
// All rights reserved

namespace Microsoft.VisualStudio.Text.Utilities
{
    using System;
    using System.Collections.Generic;
    using System.IO;

    /// <summary>
    /// Tracks the extant snapshots of a text buffer for diagnostic purposes.
    /// </summary>
    internal sealed class SnapshotTracker : IDisposable
    {
        private readonly ITextBuffer textBuffer;
        List<WeakReference> snapshots = new List<WeakReference>();

        public SnapshotTracker(ITextBuffer textBuffer)
        {
            this.textBuffer = textBuffer;
            this.snapshots.Add(new WeakReference(textBuffer.CurrentSnapshot));
            textBuffer.ChangedLowPriority += OnTextContentChanged;
        }

        public void Dispose()
        {
            this.textBuffer.ChangedLowPriority -= OnTextContentChanged;
            GC.SuppressFinalize(this);
        }

        private void OnTextContentChanged(object sender, TextContentChangedEventArgs e)
        {
            this.snapshots.Add(new WeakReference(e.After));
        }

        public int ReportLiveSnapshots(System.IO.TextWriter writer)
        {
            int liveSnapshots = 0;
            writer.Write("      Snapshots: ");
            for (int s = 0; s < this.snapshots.Count; ++s)
            {
                object target = snapshots[s].Target;
                if (target != null)
                {
                    liveSnapshots++;
                    ITextVersion v = ((ITextSnapshot)target).Version;
                    writer.Write(string.Format(System.Globalization.CultureInfo.CurrentCulture, "{0}:{1} ", v.VersionNumber, v.Length));
                }
            }
            writer.WriteLine();
            return liveSnapshots;
        }
    }
}