// Copyright (c) Microsoft Corporation
// All rights reserved

namespace WebToolingAddin
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.Composition;
    using System.Linq;

    using System.ComponentModel;
    using Microsoft.VisualStudio.Text.Classification;
    using Microsoft.VisualStudio.Text.Formatting;
    using Microsoft.VisualStudio.Text.Operations;
    using Microsoft.VisualStudio.Text.Outlining;
    using Microsoft.VisualStudio.Text.Projection;
    using Microsoft.VisualStudio.Text.Utilities;
    using Microsoft.VisualStudio.Utilities;
    using Microsoft.VisualStudio.Text.Editor;
    using Microsoft.VisualStudio.Text;
    using MonoDevelop.Ide.Editor;
    using Microsoft.VisualStudio.Text.Editor.Implementation;
    using Mono.TextEditor;
    using Microsoft.VisualStudio.Platform;

    /// <summary>
    /// Provides a VisualStudio Service that aids in creation of Editor Views
    /// </summary>
    [Export(typeof(ITextEditorFactoryService))]
    internal sealed class TextEditorFactoryService : ITextEditorFactoryService
    {
        [Import]
        internal GuardedOperations GuardedOperations { get; set; }

        [Import]
        internal IContentTypeRegistryService ContentTypeRegistryService { get; set; }

        [Import]
        internal IEditorOperationsFactoryService EditorOperationsProvider { get; set; }

        [Import]
        internal ITextBufferFactoryService TextBufferFactoryService { get; set; }

        [ImportMany]
        internal List<Lazy<ITextViewModelProvider, IContentTypeAndTextViewRoleMetadata>> TextViewModelProviders { get; set; }

        [Import]
        internal IBufferGraphFactoryService BufferGraphFactoryService { get; set; }

        [Import]
        internal IClassificationTypeRegistryService ClassificationTypeRegistryService { get; set; }

        [Import]
        internal IEditorOptionsFactoryService EditorOptionsFactoryService { get; set; }


        [Import]
        internal ITextSearchService2 TextSearchService { get; set; }

        [Import]
        internal ITextStructureNavigatorSelectorService TextStructureNavigatorSelectorService { get; set; }


        [ImportMany(typeof(IWpfTextViewCreationListener))]
        internal List<Lazy<IWpfTextViewCreationListener, IDeferrableContentTypeAndTextViewRoleMetadata>> TextViewCreationListeners { get; set; }

        [ImportMany(typeof(IWpfTextViewConnectionListener))]
        internal List<Lazy<IWpfTextViewConnectionListener, IContentTypeAndTextViewRoleMetadata>> TextViewConnectionListeners { get; set; }

        [Import]
        internal ISmartIndentationService SmartIndentationService { get; set; }

        [Import(AllowDefault=true)]
        internal IOutliningManagerService OutliningManagerService { get; set; }

        [Import]
        internal ITextUndoHistoryRegistry UndoHistoryRegistry { get; set; }

        public event EventHandler<TextViewCreatedEventArgs> TextViewCreated;

        public IWpfTextView CreateTextView(MonoDevelop.Ide.Editor.TextEditor textEditor, ITextViewRoleSet roles = null, IEditorOptions parentOptions = null)
        {
            if (textEditor == null)
            {
                throw new ArgumentNullException("textBuffer");
            }

            ITextBuffer textBuffer = textEditor.GetPlatformTextBuffer();
            ITextDataModel dataModel = new VacuousTextDataModel(textBuffer);

            ITextViewModel viewModel = UIExtensionSelector.InvokeBestMatchingFactory
                            (TextViewModelProviders,
                             dataModel.ContentType,
                             roles,
                             (provider) => (provider.CreateTextViewModel(dataModel, roles)),
                             ContentTypeRegistryService,
                             this.GuardedOperations,
                             this) ?? new VacuousTextViewModel(dataModel);

            TextView view = new TextView(textEditor, viewModel, roles ?? this.DefaultRoles, parentOptions ?? this.EditorOptionsFactoryService.GlobalOptions, this);
            view.Properties.AddProperty(typeof(MonoDevelop.Ide.Editor.TextEditor), textEditor);

            this.TextViewCreated?.Invoke(this, new TextViewCreatedEventArgs(view));

            return view;
        }

        public ITextViewRoleSet NoRoles
        {
            get { return new TextViewRoleSet(new string[0]); }
        }

        public ITextViewRoleSet AllPredefinedRoles
        {
            get { return CreateTextViewRoleSet(PredefinedTextViewRoles.Analyzable, 
                                               PredefinedTextViewRoles.Debuggable,
                                               PredefinedTextViewRoles.Document,
                                               PredefinedTextViewRoles.Editable,
                                               PredefinedTextViewRoles.Interactive,
                                               PredefinedTextViewRoles.Structured,
                                               PredefinedTextViewRoles.Zoomable,
                                               PredefinedTextViewRoles.PrimaryDocument); }
        }

        public ITextViewRoleSet DefaultRoles
        {
            // notice that Debuggable and PrimaryDocument are excluded!
            get
            {
                return CreateTextViewRoleSet(PredefinedTextViewRoles.Analyzable,
                                             PredefinedTextViewRoles.Document,
                                             PredefinedTextViewRoles.Editable,
                                             PredefinedTextViewRoles.Interactive,
                                             PredefinedTextViewRoles.Structured,
                                             PredefinedTextViewRoles.Zoomable);
            }
        }

        public ITextViewRoleSet CreateTextViewRoleSet(IEnumerable<string> roles)
        {
            return new TextViewRoleSet(roles);
        }

        public ITextViewRoleSet CreateTextViewRoleSet(params string[] roles)
        {
            return new TextViewRoleSet(roles);
        }
    }
}
