﻿// Copyright (c) Microsoft Corporation
// All rights reserved

namespace Microsoft.VisualStudio.Text.Editor.Implementation
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.ComponentModel.Composition;

    using Microsoft.VisualStudio.Utilities;
    using Microsoft.VisualStudio.Text.Projection;
    using Microsoft.VisualStudio.Text.Utilities;

    internal class ConnectionManager
    {
        private class Listener
        {
            private readonly Lazy<IWpfTextViewConnectionListener, IContentTypeAndTextViewRoleMetadata> importInfo;
            private IWpfTextViewConnectionListener listener;
            private readonly GuardedOperations guardedOperations;

            public Listener(Lazy<IWpfTextViewConnectionListener, IContentTypeAndTextViewRoleMetadata> importInfo, GuardedOperations guardedOperations)
            {
                this.importInfo = importInfo;
                this.guardedOperations = guardedOperations;
            }

            public IContentTypeAndTextViewRoleMetadata Metadata
            {
                get { return importInfo.Metadata; }
            }

            public IWpfTextViewConnectionListener Instance
            {
                get
                {
                    if (this.listener == null)
                    {
                        this.listener = this.guardedOperations.InstantiateExtension(this.importInfo, this.importInfo);
                    }
                    return this.listener;
                }
            }
        }

        IWpfTextView _textView;
        List<Listener> listeners = new List<Listener>();
        GuardedOperations _guardedOperations;

        public ConnectionManager(IWpfTextView textView, 
                                 ICollection<Lazy<IWpfTextViewConnectionListener, IContentTypeAndTextViewRoleMetadata>> textViewConnectionListeners,
                                 GuardedOperations guardedOperations)
        {
            if (textView == null)
            {
                throw new ArgumentNullException("textView");
            }
            if (textViewConnectionListeners == null)
            {
                throw new ArgumentNullException("textViewConnectionListeners");
            }
            if (guardedOperations == null)
            {
                throw new ArgumentNullException("guardedOperations");
            }

            _textView = textView;
            _guardedOperations = guardedOperations;

            List<Lazy<IWpfTextViewConnectionListener, IContentTypeAndTextViewRoleMetadata>> filteredListeners =
                UIExtensionSelector.SelectMatchingExtensions(textViewConnectionListeners, _textView.Roles);

            if (filteredListeners.Count > 0)
            {
                foreach (var listenerExport in filteredListeners)
                {
                    Listener listener = new Listener(listenerExport, guardedOperations);
                    this.listeners.Add(listener);

                    Collection<ITextBuffer> subjectBuffers =
                        textView.BufferGraph.GetTextBuffers(buffer => (Match(listenerExport.Metadata, buffer.ContentType)));

                    if (subjectBuffers.Count > 0)
                    {
                        var instance = listener.Instance;
                        if (instance != null)
                        {
                            _guardedOperations.CallExtensionPoint(instance,
                                                                  () => instance.SubjectBuffersConnected(_textView, ConnectionReason.TextViewLifetime, subjectBuffers));
                        }
                    }
                }
                textView.BufferGraph.GraphBuffersChanged += OnGraphBuffersChanged;
                textView.BufferGraph.GraphBufferContentTypeChanged += OnGraphBufferContentTypeChanged;
            }
        }

        public void Close()
        {
            if (this.listeners.Count > 0)
            {
                foreach (var listener in this.listeners)
                {
                    Collection<ITextBuffer> subjectBuffers =
                        _textView.BufferGraph.GetTextBuffers(buffer => (Match(listener.Metadata, buffer.ContentType)));

                    if (subjectBuffers.Count > 0)
                    {
                        var instance = listener.Instance;
                        if (instance != null)
                        {
                            _guardedOperations.CallExtensionPoint(instance,
                                                                  () => instance.SubjectBuffersDisconnected(_textView, ConnectionReason.TextViewLifetime, subjectBuffers));
                        }
                    }
                }
                _textView.BufferGraph.GraphBuffersChanged -= OnGraphBuffersChanged;
                _textView.BufferGraph.GraphBufferContentTypeChanged -= OnGraphBufferContentTypeChanged;
            }
        }

        private static bool Match(IContentTypeMetadata metadata, IContentType bufferContentType)
        {
            foreach (string listenerContentType in metadata.ContentTypes)
            {
                if (bufferContentType.IsOfType(listenerContentType))
                {
                    return true;
                }
            }
            return false;
        }

        private void OnGraphBuffersChanged(object sender, GraphBuffersChangedEventArgs args)
        {
            if (args.AddedBuffers.Count > 0)
            {
                foreach (Listener listener in this.listeners)
                {
                    Collection<ITextBuffer> subjectBuffers = new Collection<ITextBuffer>();
                    foreach (ITextBuffer buffer in args.AddedBuffers)
                    {
                        if (Match(listener.Metadata, buffer.ContentType))
                        {
                            subjectBuffers.Add(buffer);
                        }
                    }
                    if (subjectBuffers.Count > 0)
                    {
                        var instance = listener.Instance;
                        if (instance != null)
                        {
                            _guardedOperations.CallExtensionPoint(instance,
                                                                  () => instance.SubjectBuffersConnected(_textView, ConnectionReason.BufferGraphChange, subjectBuffers));
                        }
                    }
                }
            }

            if (args.RemovedBuffers.Count > 0)
            {
                foreach (Listener listener in this.listeners)
                {
                    Collection<ITextBuffer> subjectBuffers = new Collection<ITextBuffer>();
                    foreach (ITextBuffer buffer in args.RemovedBuffers)
                    {
                        if (Match(listener.Metadata, buffer.ContentType))
                        {
                            subjectBuffers.Add(buffer);
                        }
                    }
                    if (subjectBuffers.Count > 0)
                    {
                        var instance = listener.Instance;
                        if (instance != null)
                        {
                            _guardedOperations.CallExtensionPoint(instance,
                                                                  () => instance.SubjectBuffersDisconnected(_textView, ConnectionReason.BufferGraphChange, subjectBuffers));
                        }
                    }
                }
            }
        }

        private void OnGraphBufferContentTypeChanged(object sender, GraphBufferContentTypeChangedEventArgs args)
        {
            var connectedListeners = new List<IWpfTextViewConnectionListener>();
            var disconnectedListeners = new List<IWpfTextViewConnectionListener>();

            foreach (Listener listener in this.listeners)
            {
                bool beforeMatch = Match(listener.Metadata, args.BeforeContentType);
                bool afterMatch = Match(listener.Metadata, args.AfterContentType);
                if (beforeMatch != afterMatch)
                {
                    var instance = listener.Instance;
                    if (instance != null)
                    {
                        if (beforeMatch)
                        {
                            disconnectedListeners.Add(instance);
                        }
                        else
                        {
                            connectedListeners.Add(instance);
                        }
                    }
                }
            }

            Collection<ITextBuffer> subjectBuffers = new Collection<ITextBuffer>(new List<ITextBuffer>(1) { args.TextBuffer });
            foreach (var instance in disconnectedListeners)
            {
                _guardedOperations.CallExtensionPoint(instance,
                                                      () => instance.SubjectBuffersDisconnected(_textView, ConnectionReason.ContentTypeChange, subjectBuffers));
            }

            foreach (var instance in connectedListeners)
            {
                _guardedOperations.CallExtensionPoint(instance,
                                                      () => instance.SubjectBuffersConnected(_textView, ConnectionReason.ContentTypeChange, subjectBuffers));
            }
        }
    }
}
