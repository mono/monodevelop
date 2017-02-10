// Copyright (c) Microsoft Corporation
// All rights reserved

namespace Microsoft.VisualStudio.Text.Utilities
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.ComponentModel.Composition;
    using Microsoft.VisualStudio.Text.Editor;
    using Microsoft.VisualStudio.Utilities;

    [Export]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public sealed class TextViewMarginState : IPartImportsSatisfiedNotification
    {
        [ImportMany]
        internal List<Lazy<IWpfTextViewMarginProvider, IWpfTextViewMarginMetadata>> marginProviders { get; set; }

        public IList<Lazy<IWpfTextViewMarginProvider, IWpfTextViewMarginMetadata>> OrderedMarginProviders { get; set; }

        internal ImmutableDictionary<string, List<Lazy<IWpfTextViewMarginProvider, IWpfTextViewMarginMetadata>>> _marginMap = ImmutableDictionary<string, List<Lazy<IWpfTextViewMarginProvider, IWpfTextViewMarginMetadata>>>.Empty.WithComparers(StringComparer.OrdinalIgnoreCase);

        public void OnImportsSatisfied()
        {
            OrderedMarginProviders = Orderer.Order(marginProviders);
        }

        public IReadOnlyList<Lazy<IWpfTextViewMarginProvider, IWpfTextViewMarginMetadata>> GetMarginProviders(string containerName)
        {
            var marginProviders = new List<Lazy<IWpfTextViewMarginProvider, IWpfTextViewMarginMetadata>>();
            if (!_marginMap.TryGetValue(containerName, out marginProviders))
            {
                marginProviders = new List<Lazy<IWpfTextViewMarginProvider, IWpfTextViewMarginMetadata>>();
                foreach (var marginProvider in this.OrderedMarginProviders)
                {
                    if (StringComparer.OrdinalIgnoreCase.Equals(marginProvider.Metadata.MarginContainer, containerName))
                    {
                        marginProviders.Add(marginProvider);
                    }
                }

                ImmutableInterlocked.Update(ref _marginMap, (s) => s.Add(containerName, marginProviders));
            }

            return marginProviders;
        }
    }
}