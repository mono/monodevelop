//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
// This file contain implementations details that are subject to change without notice.
// Use at your own risk.
//
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.Text.Utilities;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.Text.Differencing.Implementation
{
    [Export(typeof(ITextDifferencingSelectorService))]
    class TextDifferencingSelectorService : ITextDifferencingSelectorService
    {
        [ImportMany(typeof(ITextDifferencingService))]
        internal List<Lazy<ITextDifferencingService, IContentTypeMetadata>> _textDifferencingServices { get; set; }

        [Import]
        internal IContentTypeRegistryService _contentTypeRegistryService { get; set; }

        [Import]
        internal GuardedOperations _guardedOperations { get; set; }

        public ITextDifferencingService GetTextDifferencingService(IContentType contentType)
        {
            ITextDifferencingService service =
                _guardedOperations.InvokeBestMatchingFactory
                    (_textDifferencingServices,
                     contentType,
                     differencingService => differencingService,
                    _contentTypeRegistryService, this);

            return service ?? DefaultTextDifferencingService;
        }

        static DefaultTextDifferencingService _defaultTextDifferencingService = new DefaultTextDifferencingService();
        public ITextDifferencingService DefaultTextDifferencingService { get { return _defaultTextDifferencingService; } }
    }
}
