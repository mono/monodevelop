//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
// This file contain implementations details that are subject to change without notice.
// Use at your own risk.
//
namespace MonoDevelop.SourceEditor.Braces
{
	using Microsoft.VisualStudio.Text.BraceCompletion;
	using Microsoft.VisualStudio.Text.Operations;
	using Microsoft.VisualStudio.Text.Utilities;
	using Microsoft.VisualStudio.Utilities;
	using System;
	using System.Collections.Generic;
	using System.ComponentModel.Composition;
	using System.Linq;

	[Export (typeof (IBraceCompletionAggregatorFactory))]
	internal class BraceCompletionAggregatorFactory : IBraceCompletionAggregatorFactory
	{
		#region Internal Properties

		internal IEnumerable<Lazy<IBraceCompletionSessionProvider, IBraceCompletionMetadata>> SessionProviders { get; private set; }
		internal IEnumerable<Lazy<IBraceCompletionContextProvider, IBraceCompletionMetadata>> ContextProviders { get; private set; }
		internal IEnumerable<Lazy<IBraceCompletionDefaultProvider, IBraceCompletionMetadata>> DefaultProviders { get; private set; }
		internal IContentTypeRegistryService ContentTypeRegistryService { get; private set; }
		internal ITextBufferUndoManagerProvider UndoManager { get; private set; }
		internal IEditorOperationsFactoryService EditorOperationsFactoryService { get; private set; }
		internal GuardedOperations GuardedOperations { get; private set; }

		#endregion

		#region Constructors

		[ImportingConstructor]
		public BraceCompletionAggregatorFactory (
			[ImportMany (typeof (IBraceCompletionSessionProvider))]IEnumerable<Lazy<IBraceCompletionSessionProvider, IBraceCompletionMetadata>> sessionProviders,
			[ImportMany (typeof (IBraceCompletionContextProvider))]IEnumerable<Lazy<IBraceCompletionContextProvider, IBraceCompletionMetadata>> contextProviders,
			[ImportMany (typeof (IBraceCompletionDefaultProvider))]IEnumerable<Lazy<IBraceCompletionDefaultProvider, IBraceCompletionMetadata>> defaultProviders,
			IContentTypeRegistryService contentTypeRegistryService,
			ITextBufferUndoManagerProvider undoManager,
			IEditorOperationsFactoryService editorOperationsFactoryService,
			GuardedOperations guardedOperations)
		{
			SessionProviders = sessionProviders;
			ContextProviders = contextProviders;
			DefaultProviders = defaultProviders;
			ContentTypeRegistryService = contentTypeRegistryService;
			UndoManager = undoManager;
			EditorOperationsFactoryService = editorOperationsFactoryService;
			GuardedOperations = guardedOperations;
		}

		#endregion

		#region IBraceCompletionAggregatorFactory

		public IBraceCompletionAggregator CreateAggregator ()
		{
			return new BraceCompletionAggregator (this);
		}

		public IEnumerable<string> ContentTypes {
			get {
				return DefaultProviders.SelectMany (export => export.Metadata.ContentTypes)
					.Concat (ContextProviders.SelectMany (export => export.Metadata.ContentTypes))
					.Concat (SessionProviders.SelectMany (export => export.Metadata.ContentTypes));
			}
		}

		#endregion
	}
}
