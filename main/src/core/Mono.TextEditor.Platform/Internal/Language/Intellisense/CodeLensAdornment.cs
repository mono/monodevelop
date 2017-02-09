//-----------------------------------------------------------------------
// <copyright company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.Language.Intellisense
{
    /// <summary>
    /// Interaction logic for CodeLensAdornment
    /// </summary>
    [CLSCompliant(false)]
    public class CodeLensAdornment : ItemsControl
    {
        private readonly ICodeLensTag tag;
        private readonly ICodeLensAdornmentCache adornmentCache;
        private readonly JoinableTaskFactory jtf;
        private bool isConnected;

        private static readonly object boxedTrue = true;
        private static readonly object boxedFalse = false;

        static CodeLensAdornment()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(CodeLensAdornment), new FrameworkPropertyMetadata(typeof(CodeLensAdornment)));
        }

        public CodeLensAdornment(ICodeLensTag tag, ICodeLensAdornmentViewModel viewModel, CodeLensPresenterStyle presenterStyle, ResourceDictionary resourceDictionary, IWpfKeyboardTrackingService keyboardTrackingService, ICodeLensIndicatorService indicatorService, ICodeLensEventManager eventManager, ICodeLensAdornmentCache adornmentCache, JoinableTaskFactory jtf)
        {
            this.tag = tag;
            this.adornmentCache = adornmentCache;
            this.isConnected = viewModel.IsConnected;
            this.Resources.MergedDictionaries.Add(resourceDictionary);
            this.DataContext = viewModel;
            this.jtf = jtf;
            SetPresenterStyle(this, presenterStyle);
            SetKeyboardTrackingService(this, keyboardTrackingService);
            SetCodeLensIndicatorService(this, indicatorService);
            SetCodeLensEventManager(this, eventManager);
        }

        private static void SetBooleanProperty(DependencyObject obj, DependencyProperty property, bool value)
        {
            if (obj == null)
            {
                throw new ArgumentNullException("obj");
            }

            obj.SetValue(property, value ? boxedTrue : boxedFalse);
        }

        public static readonly DependencyProperty IsKeyboardTargetProperty = DependencyProperty.RegisterAttached("IsKeyboardTarget", typeof(bool), typeof(CodeLensAdornment), new FrameworkPropertyMetadata(boxedFalse, FrameworkPropertyMetadataOptions.Inherits));

        public static bool GetIsKeyboardTarget(DependencyObject obj)
        {
            if (obj == null)
            {
                throw new ArgumentNullException("obj");
            }

            return (bool)obj.GetValue(IsKeyboardTargetProperty);
        }

        public static void SetIsKeyboardTarget(DependencyObject obj, bool value)
        {
            SetBooleanProperty(obj, IsKeyboardTargetProperty, value);
        }

        public static readonly DependencyProperty IsTextViewKeyboardFocusedProperty = DependencyProperty.RegisterAttached("IsTextViewKeyboardFocused", typeof(bool), typeof(CodeLensAdornment), new FrameworkPropertyMetadata(boxedFalse, FrameworkPropertyMetadataOptions.Inherits));

        public static bool GetIsTextViewKeyboardFocused(DependencyObject obj)
        {
            if (obj == null)
            {
                throw new ArgumentNullException("obj");
            }

            return (bool)obj.GetValue(IsTextViewKeyboardFocusedProperty);
        }

        public static void SetIsTextViewKeyboardFocused(DependencyObject obj, bool value)
        {
            SetBooleanProperty(obj, IsTextViewKeyboardFocusedProperty, value);
        }

        internal static readonly DependencyProperty CurrentDetailsPopupPresenterProperty = DependencyProperty.RegisterAttached("CurrentDetailsPopupPresenter", typeof(CodeLensDataPointPresenter), typeof(CodeLensAdornment), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.Inherits));

        internal static CodeLensDataPointPresenter GetCurrentDetailsPopupPresenter(DependencyObject obj)
        {
            if (obj == null)
            {
                throw new ArgumentNullException("obj");
            }

            return (CodeLensDataPointPresenter)obj.GetValue(CurrentDetailsPopupPresenterProperty);
        }

        internal static void SetCurrentDetailsPopupPresenter(DependencyObject obj, CodeLensDataPointPresenter value)
        {
            if (obj == null)
            {
                throw new ArgumentNullException("obj");
            }

            obj.SetValue(CurrentDetailsPopupPresenterProperty, value);
        }

        public static readonly DependencyProperty AccessKeyPopupDelayProperty = DependencyProperty.RegisterAttached("AccessKeyPopupDelay", typeof(TimeSpan), typeof(CodeLensAdornment), new FrameworkPropertyMetadata(TimeSpan.FromSeconds(1), FrameworkPropertyMetadataOptions.Inherits));

        public static TimeSpan GetAccessKeyPopupDelay(DependencyObject obj)
        {
            if (obj == null)
            {
                throw new ArgumentNullException("obj");
            }

            return (TimeSpan)obj.GetValue(AccessKeyPopupDelayProperty);
        }

        public static void SetAccessKeyPopupDelay(DependencyObject obj, TimeSpan value)
        {
            if (obj == null)
            {
                throw new ArgumentNullException("obj");
            }

            obj.SetValue(AccessKeyPopupDelayProperty, value);
        }

        public static readonly DependencyProperty FadeInAnimationDurationProperty = DependencyProperty.Register("FadeInAnimationDuration", typeof(TimeSpan), typeof(CodeLensAdornment), new FrameworkPropertyMetadata(TimeSpan.FromMilliseconds(500)));

        public TimeSpan FadeInAnimationDuration
        {
            get { return (TimeSpan)GetValue(FadeInAnimationDurationProperty); }
            set { SetValue(FadeInAnimationDurationProperty, value); }
        }

        public static readonly DependencyProperty IsToolWindowProperty = DependencyProperty.RegisterAttached("IsToolWindow", typeof(bool), typeof(CodeLensAdornment), new FrameworkPropertyMetadata(boxedFalse, FrameworkPropertyMetadataOptions.Inherits));

        public static bool GetIsToolWindow(DependencyObject obj)
        {
            if (obj == null)
            {
                throw new ArgumentNullException("obj");
            }
            return (bool)obj.GetValue(IsToolWindowProperty);
        }

        public static void SetIsToolWindow(DependencyObject obj, bool value)
        {
            SetBooleanProperty(obj, IsToolWindowProperty, value);
        }

        public static readonly DependencyProperty TopRightCornerReservedSizeProperty = DependencyProperty.RegisterAttached("TopRightCornerReservedSize", typeof(Size), typeof(CodeLensAdornment), new FrameworkPropertyMetadata(new Size(0, 0), FrameworkPropertyMetadataOptions.Inherits));

        public static Size GetTopRightCornerReservedSize(DependencyObject obj)
        {
            if (obj == null)
            {
                throw new ArgumentNullException("obj");
            }
            return (Size)obj.GetValue(TopRightCornerReservedSizeProperty);
        }

        public static void SetTopRightCornerReservedSize(DependencyObject obj, Size value)
        {
            if (obj == null)
            {
                throw new ArgumentNullException("obj");
            }

            obj.SetValue(TopRightCornerReservedSizeProperty, value);
        }

        public static readonly DependencyProperty PresenterStyleProperty = DependencyProperty.RegisterAttached("PresenterStyle", typeof(CodeLensPresenterStyle), typeof(CodeLensAdornment), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.Inherits));

        public static CodeLensPresenterStyle GetPresenterStyle(DependencyObject obj)
        {
            if (obj == null)
            {
                throw new ArgumentNullException("obj");
            }

            return (CodeLensPresenterStyle)obj.GetValue(PresenterStyleProperty);
        }

        public static void SetPresenterStyle(DependencyObject obj, CodeLensPresenterStyle value)
        {
            if (obj == null)
            {
                throw new ArgumentNullException("obj");
            }

            obj.SetValue(PresenterStyleProperty, value);
        }

        private static readonly DependencyPropertyKey KeyboardTrackingServicePropertyKey = DependencyProperty.RegisterAttachedReadOnly("KeyboardTrackingService", typeof(IWpfKeyboardTrackingService), typeof(CodeLensAdornment), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.Inherits));
        public static readonly DependencyProperty KeyboardTrackingServiceProperty = KeyboardTrackingServicePropertyKey.DependencyProperty;

        public static IWpfKeyboardTrackingService GetKeyboardTrackingService(DependencyObject obj)
        {
            if (obj == null)
            {
                throw new ArgumentNullException("obj");
            }

            return (IWpfKeyboardTrackingService)obj.GetValue(KeyboardTrackingServiceProperty);
        }

        private static void SetKeyboardTrackingService(DependencyObject obj, IWpfKeyboardTrackingService value)
        {
            if (obj == null)
            {
                throw new ArgumentNullException("obj");
            }

            obj.SetValue(KeyboardTrackingServicePropertyKey, value);
        }

        private static readonly DependencyPropertyKey CodeLensIndicatorServicePropertyKey = DependencyProperty.RegisterAttachedReadOnly("CodeLensIndicatorService", typeof(ICodeLensIndicatorService), typeof(CodeLensAdornment), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.Inherits));
        public static readonly DependencyProperty CodeLensIndicatorServiceProperty = CodeLensIndicatorServicePropertyKey.DependencyProperty;

        public static ICodeLensIndicatorService GetCodeLensIndicatorService(DependencyObject obj)
        {
            if (obj == null)
            {
                throw new ArgumentNullException("obj");
            }

            return (ICodeLensIndicatorService)obj.GetValue(CodeLensIndicatorServiceProperty);
        }

        private static void SetCodeLensIndicatorService(DependencyObject obj, ICodeLensIndicatorService value)
        {
            if (obj == null)
            {
                throw new ArgumentNullException("obj");
            }

            obj.SetValue(CodeLensIndicatorServicePropertyKey, value);
        }

        public static readonly DependencyProperty IsAccessKeyPopupOpenProperty = DependencyProperty.RegisterAttached("IsAccessKeyPopupOpen", typeof(bool), typeof(CodeLensAdornment), new FrameworkPropertyMetadata(boxedFalse, FrameworkPropertyMetadataOptions.Inherits));

        public static bool GetIsAccessKeyPopupOpen(DependencyObject obj)
        {
            if (obj == null)
            {
                throw new ArgumentNullException("obj");
            }

            return (bool)obj.GetValue(IsAccessKeyPopupOpenProperty);
        }

        public static void SetIsAccessKeyPopupOpen(DependencyObject obj, bool value)
        {
            SetBooleanProperty(obj, IsAccessKeyPopupOpenProperty, value);
        }

        private static readonly DependencyPropertyKey CodeLensEventManagerPropertyKey = DependencyProperty.RegisterAttachedReadOnly("CodeLensEventManager", typeof(ICodeLensEventManager), typeof(CodeLensAdornment), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.Inherits));
        public static readonly DependencyProperty CodeLensEventManagerProperty = CodeLensEventManagerPropertyKey.DependencyProperty;

        public static ICodeLensEventManager GetCodeLensEventManager(DependencyObject obj)
        {
            if (obj == null)
            {
                throw new ArgumentNullException("obj");
            }

            return (ICodeLensEventManager)obj.GetValue(CodeLensEventManagerProperty);
        }

        private static void SetCodeLensEventManager(DependencyObject obj, ICodeLensEventManager value)
        {
            if (obj == null)
            {
                throw new ArgumentNullException("obj");
            }

            obj.SetValue(CodeLensEventManagerPropertyKey, value);
        }

        /// <summary>
        /// The maximum size of usable space inside the CodeLens details view
        /// </summary>
        public static readonly DependencyProperty DetailsMaxSizeProperty = DependencyProperty.RegisterAttached("DetailsMaxSize", typeof(Size), typeof(CodeLensAdornment), new FrameworkPropertyMetadata(new Size(), FrameworkPropertyMetadataOptions.Inherits));

        public static Size GetDetailsMaxSize(DependencyObject obj)
        {
            if (obj == null)
            {
                throw new ArgumentNullException("obj");
            }

            return (Size)obj.GetValue(DetailsMaxSizeProperty);
        }

        public static void SetDetailsMaxSize(DependencyObject obj, Size value)
        {
            if (obj == null)
            {
                throw new ArgumentNullException("obj");
            }

            obj.SetValue(DetailsMaxSizeProperty, value);
        }

        public bool IsConnected
        {
            get
            {
                return this.isConnected;
            }
        }

        public async Task ConnectAsync()
        {
            if (!this.isConnected)
            {
                await this.ConnectCoreAsync();
                this.isConnected = true;
            }
        }

        public void Disconnect()
        {
            if (this.isConnected)
            {
                this.isConnected = false;
                if (!this.IsLifetimeExtended)
                {
                    this.DisconnectCore();
                }
            }
        }

        private bool IsLifetimeExtended
        {
            get
            {
                return this.adornmentCache.IsLifetimeExtended(this.tag);
            }
        }

        private void OnExtendedLifetimeEnded()
        {
            if (!this.IsConnected)
            {
                this.DisconnectCore();
            }
        }

        private void DisconnectCore()
        {
            ICodeLensAdornmentViewModel viewModel = this.DataContext as ICodeLensAdornmentViewModel;
            if (viewModel != null)
            {
                viewModel.Disconnect();
            }
        }

        private async Task ConnectCoreAsync()
        {
            ICodeLensAdornmentViewModel viewModel = this.DataContext as ICodeLensAdornmentViewModel;
            if (viewModel != null)
            {
                await viewModel.ConnectAsync();
            }
        }

        protected override bool IsItemItsOwnContainerOverride(object item)
        {
            return item is CodeLensDataPointPresenter;
        }

        protected override DependencyObject GetContainerForItemOverride()
        {
            return new CodeLensDataPointPresenter();
        }

        protected override void PrepareContainerForItemOverride(DependencyObject element, object item)
        {
            base.PrepareContainerForItemOverride(element, item);

            CodeLensPresenterStyle presenterStyle = GetPresenterStyle(this);
            CodeLensDataPointPresenter presenter = element as CodeLensDataPointPresenter;
            Debug.Assert(presenter != null, "Expected that the container for CodeLensAdornment is a CodeLensDataPointPresenter");
            if (presenter != null && presenterStyle != null && presenterStyle.AreAnimationsAllowed == true)
            {
                presenter.BeginAnimation(CodeLensDataPointPresenter.OpacityProperty, new DoubleAnimation(0.0, 1.1, new Duration(this.FadeInAnimationDuration), FillBehavior.HoldEnd));
            }
        }

        protected override AutomationPeer OnCreateAutomationPeer()
        {
            return new CodeLensAdornmentAutomationPeer(this);
        }

        /// <summary>
        /// allows CodeLens details content to keep the details UI visible temporarily, if they are about to do something
        /// that would normally cause it to lose focus and go away.  This should only be used in cases where the work is not going to
        /// cause the indicator to disappear.  For example, showing a VS platform context menu over the details content would use this.
        /// <example>
        /// <code>
        /// using (var holdopen = CodeLensAdornment.KeepDetailsVisible(visualInside))
        /// {
        ///     DoWorkThatWouldCausePopupToClose();
        /// }
        /// </code>
        /// </example>
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public static IDisposable KeepDetailsVisible(Visual source)
        {
            return new PopupDetailsKeepOpenScope(source);
        }

        internal class PopupDetailsKeepOpenScope : IDisposable
        {
            /// <summary>
            /// the number of popup details keep open calls for a given popup
            /// </summary>
            /// <param name="obj"></param>
            /// <returns></returns>
            internal static int GetPopupKeepOpenCount(DependencyObject obj)
            {
                return (int)obj.GetValue(PopupKeepOpenCountProperty);
            }

            /// <summary>
            /// set the number of popup details keep open calls for a given popup
            /// </summary>
            /// <param name="obj"></param>
            /// <param name="value"></param>
            private static void SetPopupKeepOpenCount(DependencyObject obj, int value)
            {
                obj.SetValue(PopupKeepOpenCountProperty, value);
            }

            private static readonly DependencyProperty PopupKeepOpenCountProperty =
                DependencyProperty.RegisterAttached("PopupKeepOpenCount", typeof(int), typeof(PopupDetailsKeepOpenScope), new PropertyMetadata(0));

            private static int IncrementPopupKeepOpenCount(DependencyObject obj)
            {
                int count = PopupDetailsKeepOpenScope.GetPopupKeepOpenCount(obj) + 1;
                PopupDetailsKeepOpenScope.SetPopupKeepOpenCount(obj, count);
                return count;
            }

            private static int DecrementPopupKeepOpenCount(DependencyObject obj)
            {
                int count = PopupDetailsKeepOpenScope.GetPopupKeepOpenCount(obj) - 1;
                PopupDetailsKeepOpenScope.SetPopupKeepOpenCount(obj, count);
                return count;
            }

            private Popup Popup { get; set; }

            /// <summary>
            /// create a new popup keepopen scope for the visual source.  if no popup is found for this source, this is effectively a no-op.
            /// </summary>
            /// <param name="source"></param>
            public PopupDetailsKeepOpenScope(Visual source)
            {
                Popup popup = VisualTreeExtensions.FindAncestor<Popup>(source);
                if (popup != null)
                {
                    this.Popup = popup;

                    int count = PopupDetailsKeepOpenScope.IncrementPopupKeepOpenCount(this.Popup);
                    // if nobody was already holding the popup open, start holding it open
                    if (count == 1)
                    {
                        this.Popup.StaysOpen = true;
                    }
                }
            }

            void IDisposable.Dispose()
            {
                if (this.Popup != null)
                {
                    int count = DecrementPopupKeepOpenCount(this.Popup);
                    if (count == 0)
                    {
                        // turn off staysopen
                        this.Popup.StaysOpen = false;

                        // additionally, check to see if the popup still has focus, and if not, force the popup to close
                        if (!this.Popup.IsKeyboardFocusWithin)
                        {
                            this.Popup.IsOpen = false;
                        }
                    }
                }
            }
        }

        internal void NotifyDetailsPopupOpened()
        {
            this.jtf.RunAsync(async () => await this.adornmentCache.ExtendLifetimeAsync(this.tag, this.OnExtendedLifetimeEnded));
        }
    }
}
