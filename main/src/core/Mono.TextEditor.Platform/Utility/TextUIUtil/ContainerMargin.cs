// Copyright (c) Microsoft Corporation
// All rights reserved

namespace Microsoft.VisualStudio.Text.Utilities
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Windows;
    using System.Windows.Controls;
    using Microsoft.VisualStudio.Text;
    using Microsoft.VisualStudio.Text.Editor;

    /// <summary>
    /// A base class implementation of an editor margin that hosts other editor margins inside
    /// it. The control can be oriented either horizontally or vertically and contains an ordered
    /// set of margins.
    /// 
    /// Warning: it is dangerous to defer creation of ContainerMargins, because that will forcibly
    /// defer its children. A naive client trying to look up a child of a deferred container margin won't find it.
    /// </summary>
    internal class ContainerMargin : Grid, IWpfTextViewMargin
    {
        #region Private Members
        bool _isDisposed = false;
        bool _ignoreChildVisibilityEvents = false;
        internal List<Tuple<Lazy<IWpfTextViewMarginProvider, IWpfTextViewMarginMetadata>, IWpfTextViewMargin>> _currentMargins;

        private int _nonMarginChildren;
        private readonly string _marginName;
        private readonly Orientation _orientation;
        private readonly IReadOnlyList<Lazy<IWpfTextViewMarginProvider, IWpfTextViewMarginMetadata>> _marginProviders;
        private readonly GuardedOperations _guardedOperations;
        private Dictionary<string, int> _optionSubscriptions;

        protected readonly IWpfTextViewHost TextViewHost;

        private static Lazy<IWpfTextViewMarginProvider, IWpfTextViewMarginMetadata> workaroundMarginProvider;

        protected ContainerMargin(string name, Orientation orientation, IWpfTextViewHost textViewHost, GuardedOperations guardedOperations,
                                  TextViewMarginState marginState)
        {
            _marginName = name;
            _orientation = orientation;
            _guardedOperations = guardedOperations;

            this.TextViewHost = textViewHost;

            _marginProviders = marginState.GetMarginProviders(_marginName);
        }
        #endregion

        public static IWpfTextViewMargin Create(string name, Orientation orientation, IWpfTextViewHost textViewHost, GuardedOperations guardedOperations,
                                                TextViewMarginState marginState)
        {
            ContainerMargin margin = new ContainerMargin(name, orientation, textViewHost, guardedOperations, marginState);

            margin.Initialize();

            return margin;
        }

        #region IWpfTextViewMargin Members
        /// <summary>
        /// The FrameworkElement that renders the margin.
        /// </summary>
        public FrameworkElement VisualElement
        {
            get
            {
                this.ThrowIfDisposed();
                return this;
            }
        }
        #endregion

        #region ITextViewMargin Members
        public double MarginSize
        {
            get
            {
                this.ThrowIfDisposed();
                return (_orientation == Orientation.Horizontal)
                       ? this.ActualHeight
                       : this.ActualWidth;
            }
        }

        public virtual bool Enabled
        {
            get
            {
                ThrowIfDisposed();
                return true;
            }
        }

        public ITextViewMargin GetTextViewMargin(string marginName)
        {
            if (string.Compare(marginName, _marginName, StringComparison.OrdinalIgnoreCase) == 0)
            {
                return this;
            }
            else
            {
                for (int cm = 0; cm < _currentMargins.Count; ++cm)
                {
                    var marginData = _currentMargins[cm];
                    if (marginData.Item2 != null)
                    {
                        ITextViewMargin result = marginData.Item2.GetTextViewMargin(marginName);
                        if (result != null)
                        {
                            return result;
                        }
                    }
                    else if (string.Compare(marginName, marginData.Item1.Metadata.Name, StringComparison.OrdinalIgnoreCase) == 0)
                    {
                        // the margin hasn't been created yet but the name matches, so create it now
                        bool initiallyCollapsed = false;
                        if (_optionSubscriptions != null)
                        {
                            string optionName = marginData.Item1.Metadata.OptionName;
                            if (optionName != null)
                            {
                                int marginIndex;
                                if (_optionSubscriptions.TryGetValue(optionName, out marginIndex))
                                {
                                    // since the option was previously registered, we know that it is
                                    // defined and returns a boolean
                                    if (!TextViewHost.TextView.Options.GetOptionValue<bool>(optionName))
                                    {
                                        initiallyCollapsed = true;
                                    }
                                }
                            }
                        }
                        return InsertDeferredMargin(cm, initiallyCollapsed);
                    }
                }
            }

            return null;
        }

        public void Dispose()
        {
            if (!_isDisposed)
            {
                this.Close();
                GC.SuppressFinalize(this);
                _isDisposed = true;
            }
        }
        #endregion

        protected void ThrowIfDisposed()
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException("ContainerMarginMargin");
            }
        }

        protected virtual void Initialize()
        {
            this.TextViewHost.TextView.TextDataModel.ContentTypeChanged += OnContentTypeChanged;

            this.IsVisibleChanged += delegate(object sender, DependencyPropertyChangedEventArgs e)
            {
                if ((bool)e.NewValue)
                {
                    this.RegisterEvents();
                }
                else
                {
                    this.UnregisterEvents();
                }
            };

            _nonMarginChildren = this.Children.Count;
            this.AddMargins(this.GetMarginProviders(), null);
        }

        protected virtual void RegisterEvents()
        {
        }

        protected virtual void UnregisterEvents()
        {
        }

        protected virtual void AddMargins(IList<Lazy<IWpfTextViewMarginProvider, IWpfTextViewMarginMetadata>> providers,
                                          List<Tuple<Lazy<IWpfTextViewMarginProvider, IWpfTextViewMarginMetadata>, IWpfTextViewMargin>> oldMargins)
        {
            _currentMargins = new List<Tuple<Lazy<IWpfTextViewMarginProvider, IWpfTextViewMarginMetadata>, IWpfTextViewMargin>>(providers.Count);

            try
            {
                _ignoreChildVisibilityEvents = true;

                if (oldMargins != null)
                {
                    // reset to a clean state before adding margins (this happens when the content type changes)
                    this.RowDefinitions.Clear();
                    this.ColumnDefinitions.Clear();
                    this.Children.RemoveRange(_nonMarginChildren, this.Children.Count - _nonMarginChildren);

                    if (_optionSubscriptions != null)
                    {
                        _optionSubscriptions = null;
                        TextViewHost.TextView.Options.OptionChanged -= OnOptionChanged;
                    }
                }

                int marginIndex = 0;
                foreach (var marginProvider in providers)
                {
                    string deferOptionName = marginProvider.Metadata.OptionName;
                    bool defer = false;
                    bool subscribe = false;

                    if (deferOptionName != null)
                    {
                        // the DeferCreation attribute is present
                        if (deferOptionName == "" || !TextViewHost.TextView.Options.IsOptionDefined(deferOptionName, false))
                        {
                            // no option specified, or unknown option specified: defer until touched but don't subscribe
                            defer = true;
                        }
                        else
                        {
                            object value = TextViewHost.TextView.Options.GetOptionValue(deferOptionName);
                            if (value is bool)
                            {
                                // legitimate option specified: listen to option, and defer creation if option is false
                                subscribe = true;
                                defer = !(bool)value;
                            }
                            else
                            {
                                // the option isn't boolean: defer until touched
                                defer = true;
                            }
                        }
                    }

                    // Try to reuse the existing margin if possible
                    IWpfTextViewMargin margin = null;
                    var existingMarginData = (oldMargins == null)
                                                 ? null
                                                 : oldMargins.Find(a => marginProvider == a.Item1);

                    if (existingMarginData != null && existingMarginData.Item2 != null)
                    {
                        // margin is already instantiated
                        margin = existingMarginData.Item2;
                    }
                    else if (!defer)
                    {
                        margin = _guardedOperations.InstantiateExtension(marginProvider, marginProvider, mp => mp.CreateMargin(this.TextViewHost, this));
                        if (margin == null)
                        {
                            // don't make space for this margin, and if it was deferred, don't subscribe to its option since we couldn't create it
                            continue;
                        }
                    }
                    else if (marginIndex == 0)
                    {
                        // work around a WPF bug. if the first margin is deferred, WPF won't show it when
                        // we create it. So we create a dummy margin of size zero that is not deferred.
                        if (workaroundMarginProvider == null)
                        {
                            workaroundMarginProvider = new Lazy<IWpfTextViewMarginProvider, IWpfTextViewMarginMetadata>(new WorkaroundMetadata());
                        }
                        this.AddMargin(new WorkaroundMargin(), workaroundMarginProvider, trackVisibility: false);
                        marginIndex++;
                    }

                    if (subscribe)
                    {
                        SubscribeToOptionChange(deferOptionName, marginIndex);
                    }
                    this.AddMargin(margin, marginProvider, trackVisibility: existingMarginData == null);
                    marginIndex++;
                }
            }
            finally
            {
                _ignoreChildVisibilityEvents = false;
            }

            // check to see if any visible children are available
            this.Visibility = this.HasVisibleChild() ? Visibility.Visible : Visibility.Collapsed;
        }

        private void AddMargin(IWpfTextViewMargin margin, Lazy<IWpfTextViewMarginProvider, IWpfTextViewMarginMetadata> marginProvider, bool trackVisibility)
        {
            _currentMargins.Add(new Tuple<Lazy<IWpfTextViewMarginProvider, IWpfTextViewMarginMetadata>, IWpfTextViewMargin>(marginProvider, margin));

            // if the margin is deferred (lazy) then it will be null, but we will still reserve a space for it in the grid

            // calculate the length of the grid cell used to hold the margin. deferred margins start out with 'auto'
            GridLength gridCellLength;
            if (margin != null)
            {
                gridCellLength = new GridLength(marginProvider.Metadata.GridCellLength, marginProvider.Metadata.GridUnitType);
            }
            else
            {
                gridCellLength = GridLength.Auto;
            }

            if (_orientation == Orientation.Horizontal)
            {
                RowDefinition newRow = new RowDefinition();
                newRow.Height = gridCellLength;
                this.RowDefinitions.Add(newRow);
                if (margin != null)
                {
                    Grid.SetColumn(margin.VisualElement, 0);
                    Grid.SetRow(margin.VisualElement, this.RowDefinitions.Count - 1);
                }
            }
            else
            {
                ColumnDefinition newColumn = new ColumnDefinition();
                newColumn.Width = gridCellLength;
                this.ColumnDefinitions.Add(newColumn);

                if (margin != null)
                {
                    Grid.SetColumn(margin.VisualElement, this.ColumnDefinitions.Count - 1);
                    Grid.SetRow(margin.VisualElement, 0);
                }
            }

            if (margin != null)
            {
                this.Children.Add(margin.VisualElement);

                if (trackVisibility)
                {
                    DependencyPropertyDescriptor descriptor = DependencyPropertyDescriptor.FromProperty(UIElement.VisibilityProperty, typeof(UIElement));
                    if (descriptor != null)
                    {
                        descriptor.AddValueChanged(margin.VisualElement, OnChildMarginVisibilityChanged);
                    }
                }
            }
        }

        private ITextViewMargin InsertDeferredMargin(int marginIndex, bool collapse)
        {
            var marginData = _currentMargins[marginIndex];
            IWpfTextViewMargin margin = _guardedOperations.InstantiateExtension(marginData.Item1, marginData.Item1, mp => mp.CreateMargin(this.TextViewHost, this));
            if (margin != null)
            {
                _currentMargins[marginIndex] = Tuple.Create(marginData.Item1, margin);

                GridLength gridCellLength = new GridLength(marginData.Item1.Metadata.GridCellLength, marginData.Item1.Metadata.GridUnitType);
                if (_orientation == Orientation.Horizontal)
                {
                    this.RowDefinitions[marginIndex].Height = gridCellLength;
                    Grid.SetColumn(margin.VisualElement, 0);
                    Grid.SetRow(margin.VisualElement, marginIndex);
                }
                else
                {
                    this.ColumnDefinitions[marginIndex].Width = gridCellLength;
                    Grid.SetColumn(margin.VisualElement, marginIndex);
                    Grid.SetRow(margin.VisualElement, 0);
                }
                if (collapse)
                {
                    margin.VisualElement.Visibility = Visibility.Collapsed;
                }
                this.Children.Add(margin.VisualElement);

                DependencyPropertyDescriptor descriptor = DependencyPropertyDescriptor.FromProperty(UIElement.VisibilityProperty, typeof(UIElement));
                if (descriptor != null)
                {
                    descriptor.AddValueChanged(margin.VisualElement, OnChildMarginVisibilityChanged);
                }
            }
            return margin;
        }

        private void SubscribeToOptionChange(string optionName, int marginIndex)
        {
            if (_optionSubscriptions == null)
            {
                _optionSubscriptions = new Dictionary<string, int>();
                TextViewHost.TextView.Options.OptionChanged += OnOptionChanged;
            }
            _optionSubscriptions.Add(optionName, marginIndex);
        }

        void OnOptionChanged(object sender, EditorOptionChangedEventArgs e)
        {
            int marginIndex;
            if (_optionSubscriptions.TryGetValue(e.OptionId, out marginIndex))
            {
                var margin = _currentMargins[marginIndex].Item2;
                if (TextViewHost.TextView.Options.GetOptionValue<bool>(e.OptionId))
                {
                    if (margin == null)
                    {
                        InsertDeferredMargin(marginIndex, collapse: false);
                    }
                    if (margin != null)
                    {
                        margin.VisualElement.Visibility = Visibility.Visible;
                    }
                }
                else
                {
                    Debug.Assert(margin != null);
                    margin.VisualElement.Visibility = Visibility.Collapsed;
                }
            }
        }

        protected virtual void Close()
        {
            this.TextViewHost.TextView.TextDataModel.ContentTypeChanged -= OnContentTypeChanged;
            if (_optionSubscriptions != null)
            {
                TextViewHost.TextView.Options.OptionChanged -= OnOptionChanged;
            }

            foreach (var margin in _currentMargins)
            {
                this.DisposeMargin(margin.Item2);
            }
            _currentMargins.Clear();
        }

        private IList<Lazy<IWpfTextViewMarginProvider, IWpfTextViewMarginMetadata>> GetMarginProviders()
        {
            // grab margin providers that match the content type and text view roles of the current text view
            ITextView textView = this.TextViewHost.TextView;
            var providers = UIExtensionSelector.SelectMatchingExtensions<IWpfTextViewMarginProvider, IWpfTextViewMarginMetadata>
                                                       (_marginProviders, textView.TextDataModel.ContentType, null, textView.Roles);

            // Find any providers that replace other providers.
            HashSet<string> replacedNames = new HashSet<string>();
            foreach (var provider in providers)
            {
                IEnumerable<string> replaces = provider.Metadata.Replaces;
                if (replaces != null)
                {
                    foreach (string r in replaces)
                        replacedNames.Add(r.ToLowerInvariant());
                }
            }

            if (replacedNames.Count > 0)
            {
                for (int i = providers.Count - 1; (i >= 0); --i)
                {
                    var provider = providers[i];
                    string name = provider.Metadata.Name;
                    if (!string.IsNullOrWhiteSpace(name))
                    {
                        if (replacedNames.Contains(name.ToLowerInvariant()))
                        {
                            providers.RemoveAt(i);
                        }
                    }
                }
            }

            return providers;
        }

        private void DisposeMargin(IWpfTextViewMargin margin)
        {
            if (margin != null)
            {
                DependencyPropertyDescriptor descriptor = DependencyPropertyDescriptor.FromProperty(UIElement.VisibilityProperty, typeof(UIElement));
                if (descriptor != null)
                {
                    descriptor.RemoveValueChanged(margin.VisualElement, OnChildMarginVisibilityChanged);
                }
                margin.Dispose();
            }
        }

        protected void OnChildMarginVisibilityChanged(object sender, EventArgs e)
        {
            if (!_ignoreChildVisibilityEvents)
            {
                if (this.Visibility == System.Windows.Visibility.Collapsed)
                {
                    // show the container margin if it's collapsed right now and one of its children became visible
                    if (this.HasVisibleChild())
                    {
                        this.Visibility = Visibility.Visible;
                    }
                }
                else if (this.Visibility == System.Windows.Visibility.Visible)
                {
                    // if the container margin is visible and all of its children are collapsed then collapse
                    // the container
                    if (!this.HasVisibleChild())
                    {
                        this.Visibility = System.Windows.Visibility.Collapsed;
                    }
                }
            }
        }

        protected virtual bool HasVisibleChild()
        {
            foreach (var export in _currentMargins)
            {
                if (export.Item2 != null && export.Item2.VisualElement.Visibility == System.Windows.Visibility.Visible)
                {
                    return true;
                }
            }

            return false;
        }

        private void OnContentTypeChanged(object sender, TextDataModelContentTypeChangedEventArgs e)
        {
            // generate a new list of margin providers
            IList<Lazy<IWpfTextViewMarginProvider, IWpfTextViewMarginMetadata>> providers = this.GetMarginProviders();

            // Dispose of any margin in _currentMargins that isn't listed in the new providers.
            for (int i = _currentMargins.Count - 1; (i >= 0); --i)
            {
                var marginData = _currentMargins[i];
                if (!providers.Contains(marginData.Item1))
                {
                    this.DisposeMargin(marginData.Item2);
                    _currentMargins.RemoveAt(i);
                }
            }

            this.AddMargins(providers, _currentMargins);
        }
    }
}
