// Copyright (c) Microsoft Corporation
// All rights reserved

namespace Microsoft.VisualStudio.Text.Utilities
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.Composition;
    using System.Diagnostics;
    using Microsoft.VisualStudio.Utilities;

    /// <summary>
    /// Operations that guard calls to suspicious code and log errors to registered extension error handlers.
    /// </summary>
    [Export]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal sealed class GuardedOperations
    {
        [ImportMany]
        public List<Lazy<IExtensionErrorHandler>> _errorHandlerExports { get; set; }

        private List<IExtensionErrorHandler> _errorHandlers;

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
            var candidates = new List<Tuple<Lazy<TExtension, TMetadataView>, IContentType>>();
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
                        candidates.Add(Tuple.Create(providerHandle, contentTypeRegistryService.GetContentType(contentTypeName)));
                    }
                }
            }

            // sort the candidates by content type so that best match is first
            candidates.Sort((left, right) =>
                {
                    if (left.Item2 == right.Item2)
                    {
                        return 0;
                    }
                    else
                    {
                        if (left.Item2.IsOfType(right.Item2.TypeName))
                        {
                            return -1;
                        }
                        else if (right.Item2.IsOfType(left.Item2.TypeName))
                        {
                            return +1;
                        }
                        else
                        {
                            // the content types are unrelated, use alpha order of their names
                            return string.Compare(left.Item2.TypeName, right.Item2.TypeName, StringComparison.OrdinalIgnoreCase);
                        }
                    }
                });

            for (int c = 0; c < candidates.Count; ++c)
            {
                TExtension factory = InstantiateExtension(errorSource, candidates[c].Item1);
                if (factory != null)
                {
                    return factory;
                }
            }

            // no suitable provider found
            return default(TExtension);
        }

        /// <summary>
        /// Given a list of factory extensions that provide content types, filter the list, instantiate that
        /// subset which matches the given content type, and invoke the factory method. Return the non-null results.
        /// </summary>
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
                return call();
            }
            catch (Exception e)
            {
                HandleException(errorSource, e);

                return valueOnThrow;
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
                    handler(sender, EventArgs.Empty);
                }
                catch (Exception e)
                {
                    HandleException(sender, e);
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
                    handler(sender, args);
                }
                catch (Exception e)
                {
                    HandleException(sender, e);
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

    }
}