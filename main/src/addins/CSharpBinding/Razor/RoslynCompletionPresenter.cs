//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;
using MonoDevelop.Ide;
using MonoDevelop.Ide.CodeCompletion;

namespace MonoDevelop.Ide.CodeCompletion
{
    public interface IContentTypeMetadata
    {
        IEnumerable<string> ContentTypes { get; }
    }


    [Export (typeof (IIntelliSensePresenter<ICompletionPresenterSession, ICompletionSession>))]
    [Name (PredefinedCompletionPresenterNames.RoslynCompletionPresenter)]
    [ContentType (ContentTypeNames.RoslynContentType)]
    internal sealed class RoslynCompletionPresenter : IIntelliSensePresenter<ICompletionPresenterSession, ICompletionSession>
    {
        [ImportMany (typeof (IMyRoslynCompletionDataProvider))]
        internal List<Lazy<IMyRoslynCompletionDataProvider, IContentTypeMetadata>> _completionDataProviders { get; set; }

        public ICompletionPresenterSession CreateSession (ITextView textView, ITextBuffer subjectBuffer, ICompletionSession sessionOpt)
        {
            foreach (var completionDataProviderHandle in _completionDataProviders) {
                foreach (string contentTypeName in completionDataProviderHandle.Metadata.ContentTypes) {
                    if (string.Compare (subjectBuffer.ContentType.TypeName, contentTypeName, StringComparison.OrdinalIgnoreCase) == 0) {
                        string languageName;
                        if (TryGetLanguageNameFromContentType (subjectBuffer.ContentType, out languageName)) {
                            if (Workspace.TryGetWorkspace (subjectBuffer.AsTextContainer (), out var workspace)) {
                                CompletionService completionService = workspace.Services.GetLanguageServices (languageName).GetService<CompletionService> ();
                                return new RoslynCompletionPresenterSession (textView, subjectBuffer, completionDataProviderHandle.Value, completionService);
                            }
                        }
                    }
                }
            }

            return null;
        }

        // TODO: Remove this
        private static bool TryGetLanguageNameFromContentType (IContentType contentType, out string languageName)
        {
            if (contentType.IsOfType ("htmlx")) {
                languageName = "HTML";
            }
            else if (contentType.IsOfType ("css")) {
                languageName = "CSS";
            }
            else if (contentType.IsOfType ("JSON")) {
                languageName = "JSON";
            }
            else if (contentType.IsOfType ("CSharp")) {
                languageName = LanguageNames.CSharp;
            }
            else {
                languageName = null;
            }

            return languageName != null;
        }

    }
}