//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
// This file contain implementations details that are subject to change without notice.
// Use at your own risk.
//
namespace Microsoft.VisualStudio.Text.Editor.Implementation
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
    internal sealed class TextEditorFactoryService : ITextEditorFactoryService, IPartImportsSatisfiedNotification
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

        [Import]
        internal IMultiSelectionBrokerFactory MultiSelectionBrokerFactory { get; set; }

        [ImportMany(typeof(ITextViewCreationListener))]
        internal List<Lazy<ITextViewCreationListener, IDeferrableContentTypeAndTextViewRoleMetadata>> TextViewCreationListeners { get; set; }

        [ImportMany(typeof(ITextViewConnectionListener))]
        internal List<Lazy<ITextViewConnectionListener, IContentTypeAndTextViewRoleMetadata>> TextViewConnectionListeners { get; set; }

        [Import]
        internal ISmartIndentationService SmartIndentationService { get; set; }

        [Import(AllowDefault=true)]
        internal IOutliningManagerService OutliningManagerService { get; set; }

        [Import]
        internal ITextUndoHistoryRegistry UndoHistoryRegistry { get; set; }

        public event EventHandler<TextViewCreatedEventArgs> TextViewCreated;

        private readonly static ITextViewRoleSet _noRoles = new TextViewRoleSet(new string[0]);

        private readonly static ITextViewRoleSet _allRoles = RolesFromParameters(PredefinedTextViewRoles.Analyzable,
                                                                                 PredefinedTextViewRoles.Debuggable,
                                                                                 PredefinedTextViewRoles.Document,
                                                                                 PredefinedTextViewRoles.Editable,
                                                                                 PredefinedTextViewRoles.Interactive,
                                                                                 PredefinedTextViewRoles.Structured,
                                                                                 PredefinedTextViewRoles.Zoomable,
                                                                                 PredefinedTextViewRoles.PrimaryDocument);

        private readonly static ITextViewRoleSet _defaultRoles = RolesFromParameters(PredefinedTextViewRoles.Analyzable,
                                                                                     PredefinedTextViewRoles.Document,
                                                                                     PredefinedTextViewRoles.Editable,
                                                                                     PredefinedTextViewRoles.Interactive,
                                                                                     PredefinedTextViewRoles.Structured,
                                                                                     PredefinedTextViewRoles.Zoomable);

        public ITextView CreateTextView (ITextBuffer textBuffer)
        {
            MonoDevelop.Ide.Editor.ITextDocument textDocument = textBuffer.GetTextEditor();
            TextEditor textEditor = textDocument as TextEditor;

            return CreateTextView(textEditor);
        }

        public ITextView CreateTextView (MonoDevelop.Ide.Editor.TextEditor textEditor, ITextViewRoleSet roles = null, IEditorOptions parentOptions = null)
        {
            if (textEditor == null)
            {
                throw new ArgumentNullException("textEditor");
            }

            if (roles == null) {
                roles = _defaultRoles;
            }

            ITextBuffer textBuffer = textEditor.GetContent<Mono.TextEditor.ITextEditorDataProvider>().GetTextEditorData().Document.TextBuffer;
            ITextDataModel dataModel = new VacuousTextDataModel(textBuffer);

            ITextViewModel viewModel = UIExtensionSelector.InvokeBestMatchingFactory
                            (TextViewModelProviders,
                             dataModel.ContentType,
                             roles,
                             (provider) => (provider.CreateTextViewModel(dataModel, roles)),
                             ContentTypeRegistryService,
                             this.GuardedOperations,
                             this) ?? new VacuousTextViewModel(dataModel);

            var view = ((MonoDevelop.SourceEditor.SourceEditorView)textEditor.Implementation).TextEditor;
            view.Initialize(viewModel, roles, parentOptions ?? this.EditorOptionsFactoryService.GlobalOptions, this);
            view.Properties.AddProperty(typeof(MonoDevelop.Ide.Editor.TextEditor), textEditor);

            this.TextViewCreated?.Invoke(this, new TextViewCreatedEventArgs(view));

            return view;
        }

        public ITextViewRoleSet NoRoles
        {
            get { return _noRoles; }
        }

        public ITextViewRoleSet AllPredefinedRoles
        {
            get { return _allRoles; }
        }

        public ITextViewRoleSet DefaultRoles
        {
            // notice that Debuggable and PrimaryDocument are excluded!
            get
            {
                return _defaultRoles;
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

        private static ITextViewRoleSet RolesFromParameters (params string[] roles)
        {
            return new TextViewRoleSet(roles);
        }

        [ImportMany]
        private List<Lazy<SpaceReservationManagerDefinition, IOrderable>> _spaceReservationManagerDefinitions = null;
        internal Dictionary<string, int> OrderedSpaceReservationManagerDefinitions = new Dictionary<string, int>();

        public void OnImportsSatisfied()
        {
            IList<Lazy<SpaceReservationManagerDefinition, IOrderable>> orderedManagers = Orderer.Order(_spaceReservationManagerDefinitions);
            for (int i = 0; (i < orderedManagers.Count); ++i)
            {
                this.OrderedSpaceReservationManagerDefinitions.Add(orderedManagers[i].Metadata.Name, i);
            }
        }
    }
}
