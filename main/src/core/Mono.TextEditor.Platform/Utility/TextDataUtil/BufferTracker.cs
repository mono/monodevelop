// Copyright (c) Microsoft Corporation
// All rights reserved

namespace Microsoft.VisualStudio.Text.Utilities
{
    using System;
    using System.Collections.Generic;
    using Microsoft.VisualStudio.Text.Projection;
    using Microsoft.VisualStudio.Utilities;

    internal sealed class BufferTracker : IDisposable
    {
        private static List<WeakReference> allocatedBuffers = new List<WeakReference>();

        private ITextBufferFactoryService textBufferFactoryService;
        private IProjectionBufferFactoryService projectionBufferFactoryService;

        public BufferTracker(ITextBufferFactoryService textBufferFactoryService,
                             IProjectionBufferFactoryService projectionBufferFactoryService)
        {
            this.textBufferFactoryService = textBufferFactoryService;
            this.projectionBufferFactoryService = projectionBufferFactoryService;
            textBufferFactoryService.TextBufferCreated += OnBufferCreated;
            projectionBufferFactoryService.ProjectionBufferCreated += OnBufferCreated;
        }

        public void Dispose()
        {
            this.textBufferFactoryService.TextBufferCreated -= OnBufferCreated;
            this.projectionBufferFactoryService.ProjectionBufferCreated -= OnBufferCreated;
            FreeSnapshotTrackers();
            allocatedBuffers.Clear();
        }

        private static void FreeSnapshotTrackers()
        {
            for (int b = 0; b < allocatedBuffers.Count; ++b)
            {
                ITextBuffer buffer = allocatedBuffers[b] as ITextBuffer;
                if (buffer != null)
                {
                    SnapshotTracker st;
                    if (buffer.Properties.TryGetProperty<SnapshotTracker>(typeof(SnapshotTracker), out st))
                    {
                        st.Dispose();
                        buffer.Properties.RemoveProperty(typeof(SnapshotTracker));
                    }
                }
            }
        }

        public static void TagBuffer(ITextBuffer buffer, string tag)
        {
            TextUtilities.TagBuffer(buffer, tag);
        }

        public static string GetTag(ITextBuffer buffer)
        {
            return TextUtilities.GetTag(buffer);
        }

        public static int ReportLiveBuffers(System.IO.TextWriter writer)
        {
            int totalBuffers = 0;
            if (allocatedBuffers != null)
            {
                for (int e = 0; e < allocatedBuffers.Count; ++e)
                {
                    if (allocatedBuffers[e].IsAlive)
                    {
                        ++totalBuffers;
                    }
                }

                if (writer != null)
                {
                    writer.Write
                        (String.Format
                            (System.Globalization.CultureInfo.CurrentCulture,
                             "{0}\r\n", totalBuffers));

                    int liveBuffers = 0;
                    for (int b = 0; b < allocatedBuffers.Count; ++b)
                    {
                        ITextBuffer buffer = allocatedBuffers[b].Target as ITextBuffer;
                        if (buffer != null)
                        {
                            ++liveBuffers;

                            string tag = GetTag(buffer);
                            writer.Write
                                (String.Format
                                    (System.Globalization.CultureInfo.CurrentCulture,
                                     "{0,5} {1}\r\n", b, !String.IsNullOrEmpty(tag) ? tag : "Untagged"));

                            SnapshotTracker st;
                            if (buffer.Properties.TryGetProperty<SnapshotTracker>(typeof(SnapshotTracker), out st))
                            {
                                st.ReportLiveSnapshots(writer);
                            }

                            foreach (KeyValuePair<Object, Object> pair in ((IPropertyOwner)buffer).Properties.PropertyList)
                            {
                                if (pair.Key.ToString() != "tag")
                                {
                                    string rhsType;
                                    if (pair.Value == null)
                                    {
                                        rhsType = "?null";
                                    }
                                    else
                                    {
                                        rhsType = pair.Value.GetType().Name;
                                        if (pair.Value is WeakReference)
                                        {
                                            Object target = (pair.Value as WeakReference).Target;
                                            if (target != null)
                                            {
                                                rhsType = rhsType + "(" + target.GetType().Name + ")";
                                            }
                                        }
                                    }
                                    writer.Write(String.Format(System.Globalization.CultureInfo.CurrentCulture, "      {0}\r\n", rhsType));
                                }
                            }
                        }
                    }
                }
            }

            return totalBuffers;
        }

        private void OnBufferCreated(object sender, TextBufferCreatedEventArgs e)
        {
            // look for a hole in allocatedBuffers. This is linear in number of extant buffers
            // but we won't take this path except in diagnostic conditions.
            for (int b = 0; b < allocatedBuffers.Count; ++b)
            {
                if (!allocatedBuffers[b].IsAlive)
                {
                    allocatedBuffers[b].Target = e.TextBuffer;
                    return;
                }
            }
            allocatedBuffers.Add(new WeakReference(e.TextBuffer));
            e.TextBuffer.Properties.AddProperty(typeof(SnapshotTracker), new SnapshotTracker(e.TextBuffer));
        }
    }
}