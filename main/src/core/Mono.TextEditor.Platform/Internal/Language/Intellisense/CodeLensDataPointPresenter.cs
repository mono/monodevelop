//-----------------------------------------------------------------------
// <copyright company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;

namespace Microsoft.VisualStudio.Language.Intellisense
{
    /// <summary>
    /// Interaction logic for DataPointPresenter.xaml
    /// </summary>
    [TemplatePart(Name = "PART_ToolTipPresenter", Type = typeof(ContentPresenter))]
    [TemplatePart(Name = "PART_ContentPresenter", Type = typeof(ContentPresenter))]
    [TemplatePart(Name = "PART_ContentToolTip", Type = typeof(ToolTip))]
    internal class CodeLensDataPointPresenter : Button
    {
        private ContentPresenter contentPresenter;
        private ToolTip contentToolTip;
        private ContentPresenter toolTipContentPresenter;

        static CodeLensDataPointPresenter()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(CodeLensDataPointPresenter), new FrameworkPropertyMetadata(typeof(CodeLensDataPointPresenter)));
            CodeLensAdornment.IsKeyboardTargetProperty.OverrideMetadata(typeof(CodeLensDataPointPresenter), new FrameworkPropertyMetadata(OnPropertyAffectingIsAccessKeyTargetChanged));
            CodeLensAdornment.IsTextViewKeyboardFocusedProperty.OverrideMetadata(typeof(CodeLensDataPointPresenter), new FrameworkPropertyMetadata(OnPropertyAffectingIsAccessKeyTargetChanged));
            CodeLensAdornment.CurrentDetailsPopupPresenterProperty.OverrideMetadata(typeof(CodeLensDataPointPresenter), new FrameworkPropertyMetadata(OnPropertyAffectingIsAccessKeyTargetChanged));
        }

        public CodeLensDataPointPresenter()
        {
            PresentationSource.AddSourceChangedHandler(this, this.SourceChangedHandler);
            this.CommandBindings.Add(new CommandBinding(CodeLensIndicatorCommands.ShowDetails, OnShowDetailsExecuted));
        }

        public static ComponentResourceKey DetailsPopupKey
        {
            get { return new ComponentResourceKey(typeof(CodeLensDataPointPresenter), "DetailsPopup"); }
        }

        public static ComponentResourceKey AccessKeyPopupKey
        {
            get { return new ComponentResourceKey(typeof(CodeLensDataPointPresenter), "AccessKey"); }
        }

        public static ComponentResourceKey ToolTipStyleKey
        {
            get { return new ComponentResourceKey(typeof(CodeLensDataPointPresenter), "IndicatorToolTip"); }
        }

        public static ComponentResourceKey PinnedPopupContentKey
        {
            get { return new ComponentResourceKey(typeof(CodeLensDataPointPresenter), "PinnedPopupContent"); }
        }

        public static readonly DependencyProperty AccessKeyProperty = DependencyProperty.Register("AccessKey", typeof(string), typeof(CodeLensDataPointPresenter), new FrameworkPropertyMetadata(null, OnAccessKeyChanged));

        public string AccessKey
        {
            get { return (string)GetValue(AccessKeyProperty); }
            set { SetValue(AccessKeyProperty, value); }
        }

        public static readonly DependencyProperty IsAccessKeyTargetProperty = DependencyProperty.Register("IsAccessKeyTarget", typeof(bool), typeof(CodeLensDataPointPresenter), new FrameworkPropertyMetadata(false, OnIsAccessKeyTargetChanged));

        public bool IsAccessKeyTarget
        {
            get { return (bool)GetValue(IsAccessKeyTargetProperty); }
            set { SetValue(IsAccessKeyTargetProperty, value); }
        }

        public static readonly DependencyProperty ToolTipFormatStringProperty = DependencyProperty.Register("ToolTipFormatString", typeof(string), typeof(CodeLensDataPointPresenter), new FrameworkPropertyMetadata(string.Empty));

        public string ToolTipFormatString
        {
            get { return (string)GetValue(ToolTipFormatStringProperty); }
            set { SetValue(ToolTipFormatStringProperty, value); }
        }

        private static readonly DependencyPropertyKey IsPopupOpenPropertyKey = DependencyProperty.RegisterReadOnly("IsPopupOpen", typeof(bool), typeof(CodeLensDataPointPresenter), new FrameworkPropertyMetadata(false));
        public static readonly DependencyProperty IsPopupOpenProperty = IsPopupOpenPropertyKey.DependencyProperty;

        public bool IsPopupOpen
        {
            get { return (bool)GetValue(IsPopupOpenProperty); }
            private set { SetValue(IsPopupOpenPropertyKey, value); }
        }

        private RestoreFocusScope detailsPopupRestoreFocusScope { get; set; }
        private Popup detailsPopup { get; set; }
        private Popup accessKeyPopup { get; set; }
        private DispatcherTimer accessKeyPopupTimer { get; set; }
        private HwndSource mnemonicTrackingSource { get; set; }
        private ICodeLensIndicator Indicator
        {
            get
            {
                ICodeLensIndicator indicator = this.DataContext as ICodeLensIndicator;
                Debug.Assert(this.DataContext == null || this.DataContext == BindingOperations.DisconnectedSource || indicator != null, "Expected that the DataContext is an ICodeLensIndicator");
                return indicator;
            }
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            this.toolTipContentPresenter = this.GetTemplateChild("PART_ToolTipPresenter") as ContentPresenter;
            this.ContentPresenter = this.GetTemplateChild("PART_ContentPresenter") as ContentPresenter;
            this.ContentToolTip = this.GetTemplateChild("PART_ContentToolTip") as ToolTip;
        }

        private ContentPresenter ContentPresenter
        {
            get
            {
                return this.contentPresenter;
            }
            set
            {
                if (this.contentPresenter != value)
                {
                    if (this.contentPresenter != null)
                    {
                        this.contentPresenter.ToolTipOpening -= OnContentToolTipOpening;
                    }
                    this.contentPresenter = value;
                    if (this.contentPresenter != null)
                    {
                        this.contentPresenter.ToolTipOpening += OnContentToolTipOpening;
                    }
                }
            }
        }

        private ToolTip ContentToolTip
        {
            get
            {
                return this.contentToolTip;
            }
            set
            {
                if (this.contentToolTip != value)
                {
                    if (this.contentToolTip != null)
                    {
                        this.contentToolTip.Closed -= OnContentToolTipClosed;
                    }
                    this.contentToolTip = value;
                    if (this.contentToolTip != null)
                    {
                        this.contentToolTip.Closed += OnContentToolTipClosed;
                    }
                }
            }
        }

        private CodeLensAdornment Adornment
        {
            get
            {
                return ItemsControl.ItemsControlFromItemContainer(this) as CodeLensAdornment;
            }
        }

        // makes the popup appear
        private void OnShowDetailsExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            if (InputManager.Current.IsInMenuMode)
            {
                // If we're currently in menu mode, we need to leave menu mode before attempting to show the popup.
                // This allows focus to return where it used to be (during exit of menu mode) and then move into the popup.
                // This follows the same pattern that MenuItem uses, where menu mode is exited (through Keyboard.Focus(null))
                // and then invocation of the click handler is invoked at Render priority.
                this.LeaveMenuMode();
                this.Dispatcher.BeginInvoke((Action)ShowDetailsPopupCore, DispatcherPriority.Render);
            }
            else
            {
                this.ShowDetailsPopupCore();
            }
        }

        /// <summary>
        /// Restores focus to the previously-focused element when in menu mode.
        /// </summary>
        /// <remarks>
        /// Copied from MenuBase.RestorePreviousFocus
        /// </remarks>
        private void LeaveMenuMode()
        {
            // Only restore WPF focus if the HWND with focus is an
            // HwndSource.  This enables child HWNDs, other top-level
            // non-WPF HWNDs, or even child HWNDs of other WPF top-level
            // windows to retain focus when menus are dismissed.
            IntPtr hwndWithFocus = NativeMethods.GetFocus();
            HwndSource hwndSourceWithFocus = hwndWithFocus != IntPtr.Zero ? HwndSource.FromHwnd(hwndWithFocus) : null;
            if (hwndSourceWithFocus != null)
            {
                Keyboard.Focus(null);
            }
            else
            {
                // In the case where Win32 focus is not on a WPF
                // HwndSource, we just clear WPF focus completely.
                //
                // Note that calling Focus(null) will set focus to the root
                // element of the active source, which is not what we want.
                Keyboard.ClearFocus();
            }
        }

        private void ShowDetailsPopupCore()
        {
            // if the popup exists for this datapoint reuse it, otherwise, create it
            if (this.detailsPopup == null)
            {
                this.detailsPopup = this.FindResource(DetailsPopupKey) as Popup;

                Debug.Assert(this.detailsPopup != null, "DetailsPopup was not found in resources");
                if (this.detailsPopup == null)
                {
                    return;
                }

                // hook up the command binding for hide, unpin, details
                this.detailsPopup.CommandBindings.Add(new CommandBinding(CodeLensIndicatorCommands.HideDetails, OnHideDetailsExecuted));
                this.detailsPopup.CommandBindings.Add(new CommandBinding(CodeLensIndicatorCommands.PinDetails, OnPinDetailsExecuted));

                // Ensure that the view model is connected for this indicator and that it's not cached.
                ICodeLensIndicator indicator = this.Indicator;
                if (indicator != null)
                {
                    indicator.Connect();
                }

                // bind some poperties that need to be set on the popup since it isn't actually in the visual tree with the presenter
                BindingOperations.SetBinding(detailsPopup, FrameworkElement.DataContextProperty, new Binding
                {
                    Source = this,
                    Path = new PropertyPath("(0).ViewModel", FrameworkElement.DataContextProperty),
                });

                BindingOperations.SetBinding(detailsPopup, FrameworkElement.UseLayoutRoundingProperty, new Binding
                {
                    Source = this,
                    Path = new PropertyPath(FrameworkElement.UseLayoutRoundingProperty),
                });

                BindingOperations.SetBinding(detailsPopup, FrameworkElement.SnapsToDevicePixelsProperty, new Binding
                {
                    Source = this,
                    Path = new PropertyPath(FrameworkElement.SnapsToDevicePixelsProperty),
                });

                // when the popup appears, make the callout have focus.
                this.detailsPopup.Opened += PopupOpened;

                // handle ESC on the popup
                this.detailsPopup.KeyDown += PopupKeyDown;
                this.detailsPopup.PreviewKeyDown += PopupPreviewKeyDown;
                this.detailsPopup.PreviewKeyUp += PopupPreviewKeyUp;

                // while not required, this does unhook the popup from the presenter, and other things downstream
                this.detailsPopup.Closed += PopupClosed;

                this.detailsPopup.IsKeyboardFocusWithinChanged += OnIsKeyboardFocusWithinChanged;
            }

            this.HideToolTip();


            CodeLensAdornment adornment = this.Adornment;
            CodeLensDataPointPresenter currentPopupPresenter;
            // If another details popup within the same adornment as this one is currently open, transfer its restore focus element
            // and then close the other adornment's popup.  Otherwise, save what currently has focus to restore focus to when this
            // popup is closed.
            if (adornment != null && (currentPopupPresenter = CodeLensAdornment.GetCurrentDetailsPopupPresenter(adornment)) != null)
            {
                this.detailsPopupRestoreFocusScope = currentPopupPresenter.HideDetailsPopupForNewPopup();
            }
            else
            {
                this.detailsPopupRestoreFocusScope = new RestoreFocusScope(Keyboard.FocusedElement);
            }

            if (adornment != null)
            {
                CodeLensAdornment.SetCurrentDetailsPopupPresenter(adornment, this);
            }

            this.detailsPopup.PlacementTarget = this;
            this.detailsPopup.IsOpen = true;
            this.IsPopupOpen = true;

            if (adornment != null)
            {
                adornment.NotifyDetailsPopupOpened();
            }

            // Report that this data point was clicked
            ReportDataPointClick(InputManager.Current.MostRecentInputDevice is KeyboardDevice);
        }

        private RestoreFocusScope HideDetailsPopupForNewPopup()
        {
            this.HideDetailsPopup();
            RestoreFocusScope result = this.detailsPopupRestoreFocusScope;
            this.detailsPopupRestoreFocusScope = null;
            return result;
        }

        // makes the popup hide
        private void HideDetailsPopup()
        {
            if (this.detailsPopup != null && this.detailsPopup.IsOpen)
            {
                this.detailsPopup.IsOpen = false;

                // We need to stop keyboard tracking immediately.  Waiting for PopupClosed
                // introduces a delay which can lead to out-of-order cancelation when holding
                // down the arrow keys
                this.StopKeyboardTracking();
            }
        }

        private void RestoreFocus()
        {
            if (this.detailsPopupRestoreFocusScope != null)
            {
                this.detailsPopupRestoreFocusScope.PerformRestoration();
                this.detailsPopupRestoreFocusScope = null;
            }
        }

        private void OnIsKeyboardFocusWithinChanged(object source, DependencyPropertyChangedEventArgs e)
        {
            bool lostFocus = !((bool)e.NewValue);

            // WPF isn't doing a very good job of closing the popup when focus moves to other editor toolwindows if the toolwindow is being newly created
            // this only seems to be the case specifically with Microsoft.VisualStudio.Platform.WindowManagement.Controls.GenericPaneContentPresenter having focus,
            // and somehow to do with the way the shell tries to wait until the loaded event occurs to move focus around.
            // but that's way off in code we can't reference directly, so there's no "good" way to test for this specific case.
            if (lostFocus && !this.detailsPopup.StaysOpen)
            {
                var focusedElement = Keyboard.FocusedElement as Visual;
                if (focusedElement != null)
                {
                    // this is kindof gross, but works.  see if the newly focused element is inside of a popup or context menu, and if not, close the popup
                    if (VisualTreeExtensions.FindAncestor<Popup>(focusedElement) == null && VisualTreeExtensions.FindAncestor<ContextMenu>(focusedElement) == null)
                    {
                        this.detailsPopup.IsOpen = false;
                    }
                }
            }
        }

        // the hide details command was invoked by someone, usually content inside the popup.
        // this only does anything if the popup is not unpinned (if unpinned, the hide command doesn't go through this binding)
        private void OnHideDetailsExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            if (this.detailsPopup != null)
            {
                this.RestoreFocus();
                this.HideDetailsPopup();
            }
        }

        // the unpin details command was invoked by the user
        private void OnPinDetailsExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            if (this.detailsPopup != null)
            {
                ICodeLensEventManager eventManager = CodeLensAdornment.GetCodeLensEventManager(this);
                if (eventManager != null)
                {
                    ICodeLensIndicator indicator = this.DataContext as ICodeLensIndicator;

                    // close the details popup
                    this.detailsPopup.IsOpen = false;

                    eventManager.OnPinInvoked(indicator.DataPointProviderName,
                                              InputManager.Current.MostRecentInputDevice is KeyboardDevice,
                                              new CodeLensPinEventArgs(
                                                    CodeLensAdornment.GetCodeLensIndicatorService(this),
                                                    indicator.CodeLensDescriptor,
                                                    indicator.DataPointProviderName,
                                                    CodeLensAdornment.GetPresenterStyle(this),
                                                    new ResourceDictionary() { 
                                                        { "DetailsTemplateSelector", this.FindResource("DetailsTemplateSelector") },
                                                        { "LoadingDetailsDescription", this.FindResource("LoadingDetailsDescription") },
                                                        { "RetryLoadingDescription", this.FindResource("RetryLoadingDescription") }
                                                    }));
                }
            }
        }

        // the presentation source has changed, if we're no longer on screen, make sure the popup goes away too
        private void SourceChangedHandler(object sender, SourceChangedEventArgs e)
        {
            if (e.NewSource == null)
            {
                this.HideDetailsPopup();
            }
        }

        private static void OnIsAccessKeyTargetChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            CodeLensDataPointPresenter presenter = obj as CodeLensDataPointPresenter;
            if (presenter != null)
            {
                presenter.UpdateAccessKeyActivation();
            }
        }

        private static void OnPropertyAffectingIsAccessKeyTargetChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            CodeLensDataPointPresenter presenter = obj as CodeLensDataPointPresenter;
            if (presenter != null)
            {
                presenter.IsAccessKeyTarget = CodeLensAdornment.GetCurrentDetailsPopupPresenter(presenter) != null || (CodeLensAdornment.GetIsKeyboardTarget(presenter) && CodeLensAdornment.GetIsTextViewKeyboardFocused(presenter));
            }
        }

        private static void OnAccessKeyChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            CodeLensDataPointPresenter presenter = obj as CodeLensDataPointPresenter;
            if (presenter != null)
            {
                presenter.ChangeAccessKey((string)e.OldValue, (string)e.NewValue);
            }
        }

        protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);

            if (e.Property == Button.DataContextProperty ||
                e.Property == CodeLensAdornment.CodeLensIndicatorServiceProperty)
            {
                UpdateAccessKey();
            }
            else if (e.Property == CodeLensAdornment.IsAccessKeyPopupOpenProperty)
            {
                bool showAccessKeyPopup = (bool)e.NewValue && this.AccessKey != null;
                if (showAccessKeyPopup)
                {
                    DeferShowAccessKeyPopup();
                }
                else
                {
                    HideAccessKeyPopup();
                }
            }
        }

        private void HideToolTip()
        {
            if (this.ContentToolTip != null)
            {
                this.ContentToolTip.IsOpen = false;
            }
        }

        private void ChangeAccessKey(string oldKey, string newKey)
        {
            if (this.IsAccessKeyTarget)
            {
                if (oldKey != null)
                {
                    this.UnregisterAccessKey(oldKey);
                }
                if (newKey != null)
                {
                    this.RegisterAccessKey(newKey);
                }
            }
        }

        private void UpdateAccessKeyActivation()
        {
            if (this.IsAccessKeyTarget)
            {
                string newKey = this.AccessKey;
                if (newKey != null)
                {
                    this.RegisterAccessKey(newKey);
                }
            }
            else
            {
                string oldKey = this.AccessKey;
                if (oldKey != null)
                {
                    this.UnregisterAccessKey(oldKey);
                }
            }
        }

        private void RegisterAccessKey(string key)
        {
            AccessKeyManager.Register(key, this);
            if (CodeLensAdornment.GetIsAccessKeyPopupOpen(this))
            {
                DeferShowAccessKeyPopup();
            }
        }

        private void UnregisterAccessKey(string key)
        {
            AccessKeyManager.Unregister(key, this);
            HideAccessKeyPopup();
        }

        private void OnContentToolTipOpening(object sender, ToolTipEventArgs e)
        {
            // Don't show the tooltip if we're already showing the access key tip
            if (this.accessKeyPopup != null && this.accessKeyPopup.IsOpen)
            {
                e.Handled = true;
                return;
            }

            // Hook up the style each time the tooltip is shown.  CodeLensPresenterStyle's tooltip brush and text properties
            // are not live-updating and must be manually refreshed each time the tooltip is shown.
            if (this.ContentToolTip != null && this.toolTipContentPresenter != null)
            {
                this.ContentToolTip.SetResourceReference(FrameworkElement.StyleProperty, ToolTipStyleKey);

                // Hook up the tooltip lazily.  This avoids evaluating AdditionalInformation bindings on every element
                // even though tooltips are likely to be shown infrequently (certainly not on every constructed indicator).
                this.ContentToolTip.SetBinding(AutomationProperties.NameProperty, new MultiBinding
                {
                    Converter = new CodeLensIndicatorToolTipConverter(),
                    Bindings =
                    {
                        new Binding
                        {
                            Source = this,
                            Path = new PropertyPath(ToolTipFormatStringProperty)
                        },
                        new Binding
                        {
                            Source = this.Indicator,
                            Path = new PropertyPath("ViewModel.AdditionalInformation")
                        },
                        new Binding
                        {
                            Source = this,
                            Path = new PropertyPath(AccessKeyProperty)
                        }
                    }
                });

                this.toolTipContentPresenter.SetBinding(ContentPresenter.ContentProperty, new Binding
                {
                    Source = this.ContentToolTip,
                    Path = new PropertyPath(AutomationProperties.NameProperty)
                });
            }
        }

        private void OnContentToolTipClosed(object sender, RoutedEventArgs e)
        {
            ToolTip source = (ToolTip)sender;
            BindingOperations.ClearBinding(this.toolTipContentPresenter, ContentPresenter.ContentProperty);
            BindingOperations.ClearBinding(source, AutomationProperties.NameProperty);
            source.Style = null;
        }

        private void UpdateAccessKey()
        {
            ICodeLensIndicator indicator = this.Indicator;
            ICodeLensIndicatorService indicatorService = CodeLensAdornment.GetCodeLensIndicatorService(this);
            ICodeLensAccessKeySource accessKeySource = null;
            if (indicator != null && indicatorService != null && (accessKeySource = indicatorService.GetAccessKeySource(indicator.DataPointProviderName)) != null)
            {
                this.SetBinding(AccessKeyProperty, new Binding
                {
                    Source = accessKeySource,
                    Path = new PropertyPath("AccessKey")
                });
            }
            else
            {
                BindingOperations.ClearBinding(this, AccessKeyProperty);
            }
        }

        private void DeferShowAccessKeyPopup()
        {
            Debug.Assert(this.accessKeyPopupTimer == null, "Unexpected for multiple calls ot DeferShowAccessKeyPopup to happen before the popup is either shown or canceled");
            if (this.accessKeyPopupTimer == null && (this.accessKeyPopup == null || !this.accessKeyPopup.IsOpen))
            {
                this.accessKeyPopupTimer = new DispatcherTimer(CodeLensAdornment.GetAccessKeyPopupDelay(this), DispatcherPriority.Normal, (s, e) => ShowAccessKeyPopup(), this.Dispatcher);
                this.accessKeyPopupTimer.Start();
            }
        }

        private void ShowAccessKeyPopup()
        {
            StopAccessKeyPopupTimer();

            if (this.accessKeyPopup == null)
            {
                this.accessKeyPopup = this.FindResource(AccessKeyPopupKey) as Popup;

                Debug.Assert(this.accessKeyPopup != null, "AccessKeyPopup was not found in resources");
                if (this.accessKeyPopup == null)
                {
                    return;
                }

                // bind some poperties that need to be set on the popup since it isn't actually in the visual tree with the presenter
                accessKeyPopup.DataContext = this;
            }

            // Make sure the tooltip does not stay open while the access key popup is open
            this.HideToolTip();

            this.accessKeyPopup.PlacementTarget = this;
            this.accessKeyPopup.IsOpen = true;
        }

        private void HideAccessKeyPopup()
        {
            StopAccessKeyPopupTimer();

            if (this.accessKeyPopup != null)
            {
                this.accessKeyPopup.IsOpen = false;
            }
        }

        private void StopAccessKeyPopupTimer()
        {
            if (this.accessKeyPopupTimer != null)
            {
                this.accessKeyPopupTimer.Stop();
                this.accessKeyPopupTimer = null;
            }
        }

        private void ReportDataPointClick(bool keyboardUsed)
        {
            ICodeLensIndicator indicator = this.Indicator;
            ICodeLensEventManager eventManager = CodeLensAdornment.GetCodeLensEventManager(this);

            if (indicator != null && eventManager != null)
            {
                eventManager.OnIndicatorInvoked(indicator.DataPointProviderName, keyboardUsed);
            }
        }

        #region popup event handlers

        private void PopupOpened(object sender, EventArgs e)
        {
            // It is critical that HWND focus be immediately placed on the popup's HWND.  This ensures
            // that HwndSource focus restoration from the previous popup is canceled.
            HwndSource source = HwndSource.FromVisual(this.detailsPopup.Child) as HwndSource;
            if (source != null)
            {
                NativeMethods.SetFocus(source.Handle);
            }

            // the popup is opening, force keyboard focus to go to the content
            this.detailsPopup.Child.MoveFocus(new TraversalRequest(FocusNavigationDirection.First));
            // hook up VS specific IWpfKeyboardTrackingService to ensure the popup gets all keyboard input
            this.StartKeyboardTracking();

            // if alt is already down when the popup is opened, show key indicators
            if (Keyboard.Modifiers == ModifierKeys.Alt)
            {
                ShowPopupAccessKeyIndicators();
            }

            // if menu mode occurs while the popup is open, unhook from keyboard events
            InputManager.Current.EnterMenuMode += PopupEnterMenuMode;
            InputManager.Current.LeaveMenuMode += PopupLeaveMenuMode;
        }

        // messages that will be captured by keyboard tracking
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_KEYUP = 0x0101;
        private const int WM_CHAR = 0x0102;
        private const int WM_SYSKEYDOWN = 0x0104;
        private const int WM_SYSKEYUP = 0x0105;
        private const int WM_SYSCHAR = 0x0106;
        private const int WM_SYSDEADCHAR = 0x0107;

        private void StopKeyboardTracking()
        {
            if (this.mnemonicTrackingSource != null)
            {
                this.mnemonicTrackingSource.RemoveHook(this.MnemonicHook);
                this.mnemonicTrackingSource = null;

                // unhook keyboard tracking
                IWpfKeyboardTrackingService keyboardTrackingService = CodeLensAdornment.GetKeyboardTrackingService(this);
                if (keyboardTrackingService != null)
                {
                    keyboardTrackingService.EndTrackingKeyboard();
                }
            }
        }

        private void StartKeyboardTracking()
        {
            if (this.mnemonicTrackingSource != null)
            {
                this.mnemonicTrackingSource.RemoveHook(this.MnemonicHook);
                this.mnemonicTrackingSource = null;
            }

            IWpfKeyboardTrackingService keyboardTrackingService = CodeLensAdornment.GetKeyboardTrackingService(this);
            HwndSource hwndSource = (HwndSource)HwndSource.FromVisual(this.detailsPopup.Child);
            this.mnemonicTrackingSource = hwndSource;

            if (hwndSource != null)
            {
                hwndSource.AddHook(this.MnemonicHook);

                if (keyboardTrackingService != null)
                {
                    List<uint> messagesToCapture = new List<uint>() { WM_KEYDOWN, WM_KEYUP, WM_CHAR, WM_SYSKEYDOWN, WM_SYSKEYUP, WM_SYSCHAR, WM_SYSDEADCHAR };
                    keyboardTrackingService.BeginTrackingKeyboard(hwndSource.Handle, messagesToCapture);
                }
            }
        }

        private IntPtr MnemonicHook(IntPtr hwnd, int message, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            switch (message)
            {
                // This logic is copied and modified from HwndSource.OnMnemonic
                case WM_SYSCHAR:
                case WM_SYSDEADCHAR:
                    string text = new string((char)wParam, 1);
                    if ((text != null) && (text.Length > 0))
                    {
                        // First, find the scope that access keys are registered in for this adornment.
                        // This will let us know which scope we should attempt to process the key mnemonic in.
                        AccessKeyPressedEventArgs accessKeyArgs = new AccessKeyPressedEventArgs(text);
                        this.RaiseEvent(accessKeyArgs);

                        // Check to see if the mnemonic is registered, and if it is, execute it.
                        if (AccessKeyManager.IsKeyRegistered(accessKeyArgs.Scope, text))
                        {
                            AccessKeyManager.ProcessKey(accessKeyArgs.Scope, text, false);
                            handled = true;
                        }
                    }
                    break;
            }

            return IntPtr.Zero;
        }

        // handle escape, make the popup go away
        private void PopupKeyDown(object sender, KeyEventArgs e)
        {
            switch (GetRealKey(e))
            {
                case Key.Escape:
                    {
                        this.RestoreFocus();
                        this.detailsPopup.IsOpen = false;
                        e.Handled = true;
                    }
                    break;
            }
        }

        // handles alt+arrow key navigation
        private void PopupPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.Alt && !e.IsRepeat)
            {
                switch (GetRealKey(e))
                {
                    case Key.LeftAlt:
                    case Key.RightAlt:
                        DeferShowPopupAccessKeyIndicators();
                        break;

                    case Key.Right:
                        e.Handled = FocusNextDetailsPanel(1);
                        break;

                    case Key.Left:
                        e.Handled = FocusNextDetailsPanel(-1);
                        break;
                }
            }
        }

        private void PopupPreviewKeyUp(object sender, KeyEventArgs e)
        {
            // if someone released alt, hide the popup's access key indicators
            switch (GetRealKey(e))
            {
                case Key.LeftAlt:
                case Key.RightAlt:
                    HidePopupAccessKeyIndicators();
                    break;
            }
        }

        private static Key GetRealKey(KeyEventArgs e)
        {
            if (e.Key == Key.System)
            {
                return e.SystemKey;
            }
            else
            {
                return e.Key;
            }
        }

        // unhook the popup from the presenter when the popup closes
        private void PopupClosed(object sender, EventArgs e)
        {
            // if menu mode occurs while the popup is open, unhook from keyboard events
            InputManager.Current.EnterMenuMode -= PopupEnterMenuMode;
            InputManager.Current.LeaveMenuMode -= PopupLeaveMenuMode;

            // stop keyboard tracking when we go away
            StopKeyboardTracking();

            // hide the access key indicators if they were visible
            HidePopupAccessKeyIndicators();

            // you can get this weird null case if you use unpin, it removed all the content first, then closes the popup
            if (this.detailsPopup != null)
            {
                this.detailsPopup.PlacementTarget = null;
            }
            this.detailsPopupRestoreFocusScope = null;
            this.IsPopupOpen = false;

            // If we're still the "current" popup (i.e. no other popups were opened directly from this one),
            // make sure the field gets cleared.
            CodeLensAdornment adornment = this.Adornment;
            if (adornment != null && CodeLensAdornment.GetCurrentDetailsPopupPresenter(adornment) == this)
            {
                CodeLensAdornment.SetCurrentDetailsPopupPresenter(adornment, null);
            }
        }

        // if a menu is appearing, stop tracking the keyboard, it will want that
        private void PopupEnterMenuMode(object sender, EventArgs e)
        {
            StopKeyboardTracking();
        }

        // if a menu is going away, but the popup is still open, re-hook the keyboard
        private void PopupLeaveMenuMode(object sender, EventArgs e)
        {
            if (this.IsPopupOpen)
            {
                Dispatcher.BeginInvoke((Action)StartKeyboardTracking, DispatcherPriority.Input);
            }
        }

        private static int Modulo(int value, int modulus)
        {
            return (value + modulus) % modulus;
        }

        private CodeLensDataPointPresenter GetNextDataPointPresenter(int direction)
        {
            ItemsControl itemsControl = this.Adornment;
            if (itemsControl != null)
            {
                int currentIndex = itemsControl.ItemContainerGenerator.IndexFromContainer(this);
                if (currentIndex >= 0)
                {
                    for (int nextIndex = Modulo(currentIndex + direction, itemsControl.Items.Count); nextIndex != currentIndex; nextIndex = Modulo(nextIndex + direction, itemsControl.Items.Count))
                    {
                        CodeLensDataPointPresenter nextContainer = itemsControl.ItemContainerGenerator.ContainerFromIndex(nextIndex) as CodeLensDataPointPresenter;
                        if (nextContainer != null && nextContainer.IsEnabled)
                        {
                            return nextContainer;
                        }
                    }
                }
            }

            return null;
        }

        private bool FocusNextDetailsPanel(int direction)
        {
            CodeLensDataPointPresenter nextContainer = GetNextDataPointPresenter(direction);
            if (nextContainer != null)
            {
                HidePopupAccessKeyIndicators();
                nextContainer.OnClick();
                return true;
            }

            return false;
        }

        /// <summary>
        /// returns true if there are any other indicators enabled
        /// </summary>
        private bool CanCycleIndicators
        {
            get
            {
                return GetNextDataPointPresenter(1) != null;
            }
        }

        private void DeferShowPopupAccessKeyIndicators()
        {
            // only show indicators if there is more than one enabled indicator right now            
            if (this.accessKeyPopupTimer == null && !CodeLensAdornment.GetIsAccessKeyPopupOpen(this.detailsPopup) && CanCycleIndicators)
            {
                this.accessKeyPopupTimer = new DispatcherTimer(CodeLensAdornment.GetAccessKeyPopupDelay(this), DispatcherPriority.Normal, (s, e) => ShowPopupAccessKeyIndicators(), this.Dispatcher);
                this.accessKeyPopupTimer.Start();
            }
        }

        private void ShowPopupAccessKeyIndicators()
        {
            // stop the timer
            if (this.accessKeyPopupTimer != null)
            {
                this.accessKeyPopupTimer.Stop();
                this.accessKeyPopupTimer = null;
            }

            // if alt is still down show the access key popups
            if (Keyboard.Modifiers == ModifierKeys.Alt)
            {
                CodeLensAdornment.SetIsAccessKeyPopupOpen(this.detailsPopup, true);
            }
        }

        private void HidePopupAccessKeyIndicators()
        {
            CodeLensAdornment.SetIsAccessKeyPopupOpen(this.detailsPopup, false);
        }

        #endregion
    }

    /// <summary>
    /// converter used to set the attached property for size
    /// expected:
    /// value[0] = CodeLensCalloutBorder
    /// value[0] = MaxWidth (ideally bound from the container)
    /// value[1] = MaxHeight (ideally bound from the container)
    /// value
    /// </summary>
    internal sealed class CodeLensPopupSizeConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length != 3)
            {
                Debug.Fail("CodeLensPopupSizeConverter expected exactly 3 values");
                return new Size(100, 100);
            }

            var border = values[0] as CodeLensCalloutBorder;
            var maxWidth = values[1] as double?;
            var maxHeight = values[2] as double?;

            if (border == null)
            {
                Debug.Fail("CodeLensPopupSizeConverter expected CodeLensCalloutBorder value");
                return new Size(100, 100);
            }

            if (maxWidth == null || maxHeight == null || maxWidth.Value <= 0 || maxHeight.Value <= 0)
            {
                Debug.Fail("CodeLensPopupSizeConverter expected valid width and height values");
                return new Size(100, 100);
            }

            return border.GetMaxContentSize(maxWidth.Value, maxHeight.Value);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// offset the popup by the parameter amount, using SystemParameters.MenuDropAlignment to take into account which
    /// direction the popup will be aligned.  note, only the offset field of the converter is used, the converter doesn't
    /// use parameter or value in any way.
    /// </summary>
    internal sealed class CodeLensPopupOffsetConverter : IValueConverter
    {
        public double Offset { get; set; }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            double offset = this.Offset;

            // offset the popup by that much, taking into account the value of SystemParameters.MenuDropAlignment
            // if right aligned, offset the opposite direction
            if (SystemParameters.IsMenuDropRightAligned)
            {
                offset = -offset;
            }

            return offset;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
