namespace Microsoft.VisualStudio.Text.AdornmentLibrary.ToolTip.Implementation
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.ComponentModel.Composition;
    using Microsoft.VisualStudio.Text.Adornments;
    using Microsoft.VisualStudio.Text.Editor;
    using Microsoft.VisualStudio.Threading;
    using Microsoft.VisualStudio.Utilities;

    [Export(typeof(IViewElementFactoryService))]
    internal sealed class ViewElementFactoryService : IViewElementFactoryService
    {
        private readonly IEnumerable<Lazy<IViewElementFactory, IViewElementFactoryMetadata>> unorderedViewFactories;
        private readonly IGuardedOperations guardedOperations;
        private readonly JoinableTaskContext joinableTaskContext;

        // Lazily computed.
        private ImmutableDictionary<(Type, Type), Lazy<IViewElementFactory, IViewElementFactoryMetadata>> factoryMap
            = ImmutableDictionary <(Type, Type), Lazy<IViewElementFactory, IViewElementFactoryMetadata>>.Empty;
        private IEnumerable<Lazy<IViewElementFactory, IViewElementFactoryMetadata>> orderedFactories;

        [ImportingConstructor]
        public ViewElementFactoryService(
            [ImportMany]IEnumerable<Lazy<IViewElementFactory, IViewElementFactoryMetadata>> unorderedViewFactories,
            IGuardedOperations guardedOperations,
            JoinableTaskContext joinableTaskContext)
        {
            this.joinableTaskContext = joinableTaskContext
                ?? throw new ArgumentNullException(nameof(joinableTaskContext));
            this.guardedOperations = guardedOperations
                ?? throw new ArgumentNullException(nameof(guardedOperations));
            this.unorderedViewFactories = unorderedViewFactories
                ?? throw new ArgumentNullException(nameof(unorderedViewFactories));
        }

        public TView CreateViewElement<TView>(ITextView textView, object model) where TView : class
        {
            if (!this.joinableTaskContext.IsOnMainThread)
            {
                throw new InvalidOperationException("Must be called on UI thread");
            }

            if (model == null)
            {
                throw new ArgumentNullException(nameof(model));
            }

            var lazyFactory = this.FindFactory<TView>(model.GetType());

            if (lazyFactory != null)
            {
                var factory = this.guardedOperations
                    .InstantiateExtension(this, lazyFactory);

                return this.guardedOperations.CallExtensionPoint(
                    () => factory.CreateViewElement<TView>(textView, model),
                    valueOnThrow: default(TView));
            }

            return null;
        }

        private IEnumerable<Lazy<IViewElementFactory, IViewElementFactoryMetadata>> OrderedFactories
            => this.orderedFactories ?? (this.orderedFactories = Orderer.Order(this.unorderedViewFactories));

        private Lazy<IViewElementFactory, IViewElementFactoryMetadata> FindFactory<TView>(Type modelType)
        {
            // Do we have this conversion cached?
            if (this.factoryMap.TryGetValue((modelType, typeof(TView)), out var lazyFactory))
            {
                return lazyFactory;
            }

            // Nope, try and find a suitable converter that matches this type exactly.
            var exactMatch = this.FindExactMatchingFactory<TView>(modelType);
            if (exactMatch != null)
            {
                return exactMatch;
            }
            else
            {
                // Try and find a suitable match from our type and interface hierarchy.
                return this.FindInheritanceMatchingFactory<TView>(modelType);
            }
        }

        private Lazy<IViewElementFactory, IViewElementFactoryMetadata> FindExactMatchingFactory<TView>(Type modelType)
        {
            var candidate = this.FindMatchingFactory<TView>(modelType, (item) => item.Metadata.FromFullName == modelType.AssemblyQualifiedName);
            if (candidate != null)
            {
                return candidate;
            }

            // Unknown conversion.
            return null;
        }

        private Lazy<IViewElementFactory, IViewElementFactoryMetadata> FindInheritanceMatchingFactory<TView>(Type modelType)
        {
            // Try and find a suitable match from the interfaces on this type.
            foreach (var iface in modelType.GetInterfaces())
            {
                var candidate = this.FindMatchingFactory<TView>(modelType, (item) => item.Metadata.FromFullName == iface.AssemblyQualifiedName);
                if (candidate != null)
                {
                    return candidate;
                }
            }

            // Still no, try and find a suitable converter from among the base types for this class.
            for (var t = modelType.BaseType; t != null; t = t.BaseType)
            {
                var candidate = this.FindMatchingFactory<TView>(modelType, (item) => item.Metadata.FromFullName == t.AssemblyQualifiedName);
                if (candidate != null)
                {
                    return candidate;
                }
            }

            // Unknown conversion.
            return null;
        }

        private Lazy<IViewElementFactory, IViewElementFactoryMetadata> FindMatchingFactory<TView>(
            Type modelType,
            Predicate<Lazy<IViewElementFactory, IViewElementFactoryMetadata>> fromSelector)
        {
            foreach (var candidate in this.OrderedFactories)
            {
                if (candidate.Metadata.ToFullName == typeof(TView).AssemblyQualifiedName && fromSelector.Invoke(candidate))
                {
                    this.factoryMap = this.factoryMap.Add((modelType, typeof(TView)), candidate);
                    return candidate;
                }
            }

            return null;
        }
    }
}
