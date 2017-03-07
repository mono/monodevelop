//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
namespace Microsoft.VisualStudio.Text.Utilities
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.Composition;
    using System.Diagnostics;
    using System.Linq;
    using Microsoft.VisualStudio.Utilities;

    /// <summary>
    /// Operations that guard calls to suspicious code and log errors to registered extension error handlers.
    /// </summary>
    [Export]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public sealed class GuardedOperations
    {
        [ImportMany]
        private List<Lazy<IExtensionErrorHandler>> _errorHandlerExports = null;

        [ImportMany]
        private List<Lazy<IExtensionPerformanceTracker>> _perTrackerExports = null;

        private List<IExtensionErrorHandler> _errorHandlers;
        private FrugalList<IExtensionPerformanceTracker> _perfTrackers;

        public GuardedOperations()
        {
        }

        /// <summary>
        /// For unit testing.
        /// </summary>
        public GuardedOperations(IExtensionErrorHandler extensionErrorHandler)
        {
            _errorHandlers = new List<IExtensionErrorHandler>();
            _errorHandlers.Add(extensionErrorHandler);
            _perfTrackers = new FrugalList<IExtensionPerformanceTracker>();
        }

        internal static bool ReThrowIfNoHandlers { get; set; } // For unit testing.

        public List<IExtensionErrorHandler> ErrorHandlers
        {
            get
            {
                if (_errorHandlers == null) 
                {
                    _errorHandlers = new List<IExtensionErrorHandler>();
                    if (_errorHandlerExports != null)       // can be null during unit testing
                    {
                        foreach (var export in _errorHandlerExports)
                        {
                            try
                            {
                                var handler = export.Value;
                                if (handler != null)
                                {
                                    _errorHandlers.Add(handler);
                                }
                            }
                            catch (Exception)
                            {
                                Debug.Fail("Exception instantiating error handler!");
                            }
                        }
                    }
                }
                return _errorHandlers;
            }
            set
            {
                // for unit testing
                _errorHandlers = value;
            }
        }

        private FrugalList<IExtensionPerformanceTracker> PerfTrackers
        {
            get
            {
                if (_perfTrackers == null)
                {
                    _perfTrackers = new FrugalList<IExtensionPerformanceTracker>();
                    if (_perTrackerExports != null)       // can be null during unit testing
                    {
                        foreach (var export in _perTrackerExports)
                        {
                            try
                            {
                                var perfTracker = export.Value;
                                if (perfTracker != null)
                                {
                                    _perfTrackers.Add(perfTracker);
                                }
                            }
                            catch (Exception)
                            {
                                Debug.Fail("Exception instantiating perf tracker");
                            }
                        }
                    }
                }
                return _perfTrackers;
            }
            set
            {
                // for unit testing
                _perfTrackers = value;
            }
        }

        public TExtensionInstance InvokeBestMatchingFactory<TExtensionFactory, TExtensionInstance, TMetadataView>
                (IList<Lazy<TExtensionFactory, TMetadataView>> providerHandles,
                 IContentType dataContentType,
                 Func<TExtensionFactory, TExtensionInstance> getter,
                 IContentTypeRegistryService contentTypeRegistryService,
                 object errorSource)
            where TMetadataView : IContentTypeMetadata
            where TExtensionFactory : class
        {
            var factory = InvokeBestMatchingFactory(providerHandles, dataContentType, contentTypeRegistryService, errorSource);

            if (factory == null)
            {
                return default(TExtensionInstance);
            }

            TExtensionInstance extensionInstance = default(TExtensionInstance);
            this.CallExtensionPoint(errorSource, () => extensionInstance = getter(factory));
            return extensionInstance;
        }

        public TExtension InvokeBestMatchingFactory<TExtension, TMetadataView>
                (IList<Lazy<TExtension, TMetadataView>> providerHandles,
                 IContentType dataContentType,
                 IContentTypeRegistryService contentTypeRegistryService,
                 object errorSource)
            where TMetadataView : IContentTypeMetadata
        {
            var candidates = new List<Lazy<TExtension, TMetadataView>>();
            foreach (var providerHandle in providerHandles)
            {
                foreach (string contentTypeName in providerHandle.Metadata.ContentTypes)
                {
                    if (string.Compare(dataContentType.TypeName, contentTypeName, StringComparison.OrdinalIgnoreCase) == 0)
                    {
                        // we have an exact match--no need to look further if this one is happy
                        TExtension factory = InstantiateExtension(errorSource, providerHandle);
                        if (factory != null)
                        {
                            return factory;
                        }
                    }
                    else if (dataContentType.IsOfType(contentTypeName))
                    {
                        candidates.Add(providerHandle);
                        break;
                    }
                }
            }

            SortCandidates(candidates, dataContentType, contentTypeRegistryService);

            for (int c = 0; c < candidates.Count; ++c)
            {
                TExtension factory = InstantiateExtension(errorSource, candidates[c]);
                if (factory != null)
                {
                    return factory;
                }
            }

            // no suitable provider found
            return default(TExtension);
        }

        public List<TExtensionInstance> InvokeMatchingFactories<TExtensionInstance, TExtensionFactory, TMetadataView>
            (IEnumerable<Lazy<TExtensionFactory, TMetadataView>> lazyFactories,
             Func<TExtensionFactory, TExtensionInstance> getter,
             IContentType dataContentType,
             object errorSource)
                where TMetadataView : IContentTypeMetadata          // content type is required
                where TExtensionFactory : class
                where TExtensionInstance : class
        {
            var result = new List<TExtensionInstance>();
            foreach (var lazyFactory in lazyFactories)
            {
                if (ExtensionSelector.ContentTypeMatch(dataContentType, lazyFactory.Metadata.ContentTypes))
                {
                    try
                    {
                        TExtensionFactory factory = lazyFactory.Value;
                        if (factory != null)
                        {
                            TExtensionInstance instance = getter(factory);
                            if (instance != null)
                            {
                                result.Add(instance);
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        HandleException(errorSource, e);
                    }
                }
            }
            return result;
        }

        // The algorithm here is that assets can have a Name attribute and one or more Replaces attribute.
        // Assets without names are treated normally (they are always considered eligible).
        // Named assets are considered ineligible if:
        //  There is a "better" asset with the same name (better means a more specific content type).
        //  There is another assert with a Replaces attribute that matches the name of the asset.
        public IEnumerable<Lazy<TExtensionFactory, TMetadataView>> FindEligibleFactories<TExtensionFactory, TMetadataView>
                                            (IEnumerable<Lazy<TExtensionFactory, TMetadataView>> lazyFactories,
                                            IContentType dataContentType,
                                            IContentTypeRegistryService contentTypeRegistryService)
                                                where TMetadataView : INamedContentTypeMetadata          // content type is required
                                                where TExtensionFactory : class
        {
            Dictionary<string, List<Lazy<TExtensionFactory, TMetadataView>>> namedFactories = null;
            HashSet<string> replaced = null;
            foreach (var lazyFactory in lazyFactories)
            {
                if (ExtensionSelector.ContentTypeMatch(dataContentType, lazyFactory.Metadata.ContentTypes))
                {
                    if (string.IsNullOrEmpty(lazyFactory.Metadata.Name))
                    {
                        yield return lazyFactory;
                    }
                    else
                    {
                        if (namedFactories == null)
                        {
                            namedFactories = new Dictionary<string, List<Lazy<TExtensionFactory, TMetadataView>>>(StringComparer.OrdinalIgnoreCase);
                        }

                        List<Lazy<TExtensionFactory, TMetadataView>> factories;
                        if (!namedFactories.TryGetValue(lazyFactory.Metadata.Name, out factories))
                        {
                            factories = new List<Lazy<TExtensionFactory, TMetadataView>>();
                            namedFactories.Add(lazyFactory.Metadata.Name, factories);
                        }

                        factories.Add(lazyFactory);

                        if (lazyFactory.Metadata.Replaces != null)
                        {
                            if (replaced == null)
                            {
                                replaced = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                            }

                            foreach (var s in lazyFactory.Metadata.Replaces)
                            {
                                replaced.Add(s);
                            }
                        }
                    }
                }
            }

            if (namedFactories != null)
            {
                foreach (var candidates in namedFactories.Values)
                {
                    var candidate = candidates[0];
                    if ((replaced == null) || !replaced.Contains(candidate.Metadata.Name))
                    {
                        SortCandidates(candidates, dataContentType, contentTypeRegistryService);
                        yield return candidates[0];
                    }
                }
            }
        }


        /// <summary>
        /// Given a list of factory extensions that provide content types, filter the list, instantiate that
        /// subset which matches the given content type, and invoke the factory method. Return the non-null results.
        /// </summary>
        public List<TExtensionInstance> InvokeEligibleFactories<TExtensionInstance, TExtensionFactory, TMetadataView>
                    (IEnumerable<Lazy<TExtensionFactory, TMetadataView>> lazyFactories,
                     Func<TExtensionFactory, TExtensionInstance> getter,
                     IContentType dataContentType,
                     IContentTypeRegistryService contentTypeRegistryService,
                     object errorSource)
            where TMetadataView : INamedContentTypeMetadata          // content type is required
            where TExtensionFactory : class
            where TExtensionInstance : class
        {
            var result = new List<TExtensionInstance>();
            foreach (var lazyFactory in FindEligibleFactories(lazyFactories, dataContentType, contentTypeRegistryService))
            {
                try
                {
                    TExtensionFactory factory = lazyFactory.Value;
                    if (factory != null)
                    {
                        TExtensionInstance instance = getter(factory);
                        if (instance != null)
                        {
                            result.Add(instance);
                        }
                    }
                }
                catch (Exception e)
                {
                    HandleException(errorSource, e);
                }
            }

            return result;
        }

        public TExtension InstantiateExtension<TExtension>(object errorSource, Lazy<TExtension> provider)
        {
            try
            {
                return provider.Value;
            }
            catch (Exception e)
            {
                HandleException(errorSource, e);
                return default(TExtension);
            }
        }

        public TExtension InstantiateExtension<TExtension, TMetadata>(object errorSource, Lazy<TExtension, TMetadata> provider)
        {
            try
            {
                return provider.Value;
            }
            catch (Exception e)
            {
                HandleException(errorSource, e);
                return default(TExtension);
            }
        }

        public TExtensionInstance InstantiateExtension<TExtension, TMetadata, TExtensionInstance>(
            object errorSource, Lazy<TExtension, TMetadata> provider, Func<TExtension, TExtensionInstance> getter)
        {
            try
            {
                return getter(provider.Value);
            }
            catch (Exception e)
            {
                HandleException(errorSource, e);
                return default(TExtensionInstance);
            }
        }

        public void CallExtensionPoint(object errorSource, Action call)
        {
            try
            {
                call();
            }
            catch (Exception e)
            {
                HandleException(errorSource, e);
            }
        }

        public T CallExtensionPoint<T>(object errorSource, Func<T> call, T valueOnThrow)
        {
            try
            {
                BeforeCallingEventHandler(call);
                return call();
            }
            catch (Exception e)
            {
                HandleException(errorSource, e);

                return valueOnThrow;
            }
            finally
            {
                AfterCallingEventHandler(call);
            }            
        }

        public void CallExtensionPoint(Action call)
        {
            this.CallExtensionPoint(errorSource: null, call: call);
        }

        public T CallExtensionPoint<T>(Func<T> call, T valueOnThrow)
        {
            return this.CallExtensionPoint(errorSource: null, call: call, valueOnThrow: valueOnThrow);
        }

        public void RaiseEvent(object sender, EventHandler eventHandlers)
        {
            if (eventHandlers == null)
            {
                return;
            }

            var handlers = eventHandlers.GetInvocationList();

            foreach (EventHandler handler in handlers)
            {
                try
                {
                    BeforeCallingEventHandler(handler);
                    handler(sender, EventArgs.Empty);
                }
                catch (Exception e)
                {
                    HandleException(sender, e);
                }
                finally
                {
                    AfterCallingEventHandler(handler);
                }
            }
        }

        public void RaiseEvent<TArgs>(object sender, EventHandler<TArgs> eventHandlers, TArgs args) where TArgs : EventArgs
        {
            if (eventHandlers == null)
            {
                return;
            }
            var handlers = eventHandlers.GetInvocationList();

            foreach (EventHandler<TArgs> handler in handlers)
            {
                try
                {
                    BeforeCallingEventHandler(handler);
                    handler(sender, args);
                }
                catch (Exception e)
                {
                    HandleException(sender, e);
                }
                finally
                {
                    AfterCallingEventHandler(handler);
                }
            }
        }

        private void AfterCallingEventHandler(Delegate handler)
        {
            if (PerfTrackers.Count == 0)
            {
                return;
            }

            foreach (var perfTracker in PerfTrackers)
            {
                try
                {
                    perfTracker.AfterCallingEventHandler(handler);
                }
                catch (Exception e)
                {
                    HandleException(perfTracker, e);
                }
            }
        }

        private void BeforeCallingEventHandler(Delegate handler)
        {
            if (PerfTrackers.Count == 0)
            {
                return;
            }

            foreach (var perfTracker in PerfTrackers)
            {
                try
                {
                    perfTracker.BeforeCallingEventHandler(handler);
                }
                catch (Exception e)
                {
                    HandleException(perfTracker, e);
                }
            }
        }

        public void HandleException(object errorSource, Exception e)
        {
            bool handled = false;
            foreach (var errorHandler in ErrorHandlers)
            {
                try
                {
                    errorHandler.HandleError(errorSource, e);
                    handled = true;
                }
                catch (Exception doubleFaultException)
                {
                    // TODO: What is the right behavior here?
                    Debug.Fail(doubleFaultException.ToString());
                }
            }
            if (!handled)
            {
                // TODO: What is the right behavior here?
                Debug.Fail(e.ToString());

                if (GuardedOperations.ReThrowIfNoHandlers)
                    throw new Exception("Unhandled exception.", e);
            }
        }

        private static void SortCandidates<TExtension, TMetadataView>(List<Lazy<TExtension, TMetadataView>> candidates, IContentType dataContentType, IContentTypeRegistryService contentTypeRegistryService)
                        where TMetadataView : IContentTypeMetadata
        {
            if (candidates.Count > 1)
            {
                var contentTypes = new List<IContentType>();
                foreach (var c in candidates)
                {
                    foreach (string contentTypeName in c.Metadata.ContentTypes)
                    {
                        if (dataContentType.IsOfType(contentTypeName))
                        {
                            var type = contentTypeRegistryService.GetContentType(contentTypeName);
                            if (!contentTypes.Contains(type))
                            {
                                contentTypes.Add(type);
                            }
                        }
                    }
                }

                contentTypes.Sort(CompareContentTypes);
                candidates.Sort((left, right) =>
                {
                    int leftIndex = BestContentTypeScore(left.Metadata.ContentTypes, contentTypes); 
                    int rightIndex = BestContentTypeScore(right.Metadata.ContentTypes, contentTypes);

                    return leftIndex - rightIndex;  // Sort these in ascending order.
                });
            }
        }

        private static int BestContentTypeScore(IEnumerable<string> contentTypes, List<IContentType> sortedContentTypes)
        {
            return contentTypes.Min(s => ContentTypeScore(s, sortedContentTypes));
        }

        private static int ContentTypeScore(string contentTypeName, List<IContentType> sortedContentTypes)
        {
            for (int i = 0; (i < sortedContentTypes.Count); ++i)
            {
                if (string.Compare(sortedContentTypes[i].TypeName, contentTypeName, StringComparison.OrdinalIgnoreCase) == 0)
                {
                    return i;
                }
            }

            return sortedContentTypes.Count;
        }

        private static int CompareContentTypes(IContentType left, IContentType right)
        {
            if (left == right)
            {
                return 0;
            }
            else
            {
                if (left.IsOfType(right.TypeName))
                {
                    return -1;
                }
                else if (right.IsOfType(left.TypeName))
                {
                    return +1;
                }
                else
                {
                    // the content types are unrelated, use alpha order of their names
                    return string.Compare(left.TypeName, right.TypeName, StringComparison.OrdinalIgnoreCase);
                }
            }
        }
    }
}