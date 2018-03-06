////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (c) Microsoft Corporation.  All rights reserved.
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Projection;
using Microsoft.VisualStudio.Text.Utilities;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.Language.Intellisense.Implementation
{
    [PartCreationPolicy(CreationPolicy.NonShared)]
    [Export(typeof(ITextViewConnectionListener))]
    [ContentType("text")]
    [TextViewRole(PredefinedTextViewRoles.Editable)]
    [TextViewRole(PredefinedTextViewRoles.EmbeddedPeekTextView)]
    [TextViewRole(PredefinedTextViewRoles.CodeDefinitionView)]
    internal class IntellisenseManagerConnectionListener : ITextViewConnectionListener
    {
        [ImportMany]
        internal List<Lazy<IIntellisenseControllerProvider, IContentTypeMetadata>> IntellisenseControllerFactories { get; set; }

        [Import]
        internal IGuardedOperations GuardedOperations { get; set; }

        public void SubjectBuffersConnected(
            ITextView textView,
            ConnectionReason reason,
            IReadOnlyCollection<ITextBuffer> subjectBuffers)
        {
            IntellisenseManager manager = textView.Properties.GetOrCreateSingletonProperty(
                delegate { return new IntellisenseManager(this, textView); });

            // Create the appropriate Intellisense controllers for the content types in the buffer graph. It's important that we do
            // this after creating the brokers, as the controllers will most likely start using the brokers immediately.

            for (int f = 0; f < this.IntellisenseControllerFactories.Count; ++f)
            {
                var factory = this.IntellisenseControllerFactories[f];

                // filter subject buffers to get the ones that match the factory content types
                FrugalList<ITextBuffer> matchingSubjectBuffers = new FrugalList<ITextBuffer>();
                foreach (string factoryContentType in factory.Metadata.ContentTypes)
                {
                    foreach (ITextBuffer subjectBuffer in subjectBuffers)
                    {
                        if (subjectBuffer.ContentType.IsOfType(factoryContentType) &&
                            !matchingSubjectBuffers.Contains(subjectBuffer))
                        {
                            matchingSubjectBuffers.Add(subjectBuffer);
                        }
                    }
                }

                if (matchingSubjectBuffers.Count > 0)
                {
                    // This controller factory is registered for the content type we understand.  Go ahead and create
                    // one.  Note that this won't give us a handle to a controller object.  We wouldn't be able to do anything
                    // with such a reference anyway.

                    if (manager.Controllers[f] == null)
                    {
                        manager.Controllers[f] = this.GuardedOperations.InstantiateExtension
                                                        (factory, factory,
                                                         provider => provider.TryCreateIntellisenseController(textView, matchingSubjectBuffers));
                    }
                    else
                    {
                        foreach (ITextBuffer matchingSubjectBuffer in matchingSubjectBuffers)
                        {
                            manager.Controllers[f].ConnectSubjectBuffer(matchingSubjectBuffer);
                        }
                    }
                }
            }
        }

        public void SubjectBuffersDisconnected(
            ITextView textView,
            ConnectionReason reason,
            IReadOnlyCollection<ITextBuffer> subjectBuffers)
        {
            // Notify controllers that subject buffer is no longer interesting. We let the controller figure out if the
            // buffer was interesting in the first place.
            IntellisenseManager manager = textView.Properties.GetProperty<IntellisenseManager>(typeof(IntellisenseManager));

            for (int f = 0; f < manager.Controllers.Length; ++f)
            {
                if (manager.Controllers[f] != null)
                {
                    foreach (ITextBuffer subjectBuffer in subjectBuffers)
                    {
                        manager.Controllers[f].DisconnectSubjectBuffer(subjectBuffer);
                    }
                }
            }
        }
    }

    internal class IntellisenseManager
    {
        private readonly IntellisenseManagerConnectionListener _componentContext;
        private readonly ITextView _associatedTextView;
        public IIntellisenseController[] Controllers { get; private set; }

        internal IntellisenseManager(IntellisenseManagerConnectionListener componentContext, ITextView associatedTextView)
        {
            _componentContext = componentContext;
            this.Controllers = new IIntellisenseController[_componentContext.IntellisenseControllerFactories.Count];
            _associatedTextView = associatedTextView;

            _associatedTextView.Closed += this.OnViewClosed;
            _associatedTextView.BufferGraph.GraphBufferContentTypeChanged += this.OnGraphBufferContentTypeChange;
        }
        
        private void OnViewClosed(object sender, EventArgs e)
        {
            // Detach each of the Intellisense controllers on the associated view.  We won't need them anymore
            foreach (var controller in this.Controllers)
            {
                if (controller != null)
                {
                    controller.Detach(_associatedTextView);
                }
            }

            // Stop listening to events
            _associatedTextView.Closed -= this.OnViewClosed;
            _associatedTextView.BufferGraph.GraphBufferContentTypeChanged -= this.OnGraphBufferContentTypeChange;
        }
        
        private void OnGraphBufferContentTypeChange(object sender, GraphBufferContentTypeChangedEventArgs args)
        {
            if (args.BeforeContentType.IsOfType("text") && args.AfterContentType.IsOfType("text"))
            {
                // We won't get subject buffers connected/disconnected calls when both the before & after content
                // types are "text", but we still need to manage intellisense controllers in this situation.
                // The broker associated with the subjectBuffer in question remains the same.
                ITextBuffer subjectBuffer = args.TextBuffer;

                for (int f = 0; f < _componentContext.IntellisenseControllerFactories.Count; ++f)
                {
                    var factory = _componentContext.IntellisenseControllerFactories[f];
                    bool beforeMatch = false;
                    bool afterMatch = false;
                    foreach (string factoryContentType in factory.Metadata.ContentTypes)
                    {
                        if (args.BeforeContentType.IsOfType(factoryContentType))
                        {
                            beforeMatch = true;
                        }
                        if (args.AfterContentType.IsOfType(factoryContentType))
                        {
                            afterMatch = true;
                        }
                    }
                    if (beforeMatch != afterMatch)
                    {
                        if (beforeMatch)
                        {
                            if (this.Controllers[f] != null)
                            {
                                // the controller will be null if its creation failed
                                this.Controllers[f].DisconnectSubjectBuffer(subjectBuffer);
                                // should we destroy the controller if it has no more buffers?
                            }
                        }

                        if (afterMatch)
                        {
                            if (this.Controllers[f] != null)
                            {
                                this.Controllers[f].ConnectSubjectBuffer(subjectBuffer);
                            }
                            else
                            {
                                this.Controllers[f] =
                                    this._componentContext.GuardedOperations.InstantiateExtension(
                                        factory,
                                        factory,
                                        provider => provider.TryCreateIntellisenseController
                                            (_associatedTextView, new FrugalList<ITextBuffer>() { subjectBuffer }));
                            }
                        }
                    }
                }
            }
        }
    }
}
