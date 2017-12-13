//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
// This file contain implementations details that are subject to change without notice.
// Use at your own risk.
//
namespace Microsoft.VisualStudio.Text.AdornmentLibrary.ToolTip.Implementation
{
    using System;
    using System.Collections.ObjectModel;
    using System.Diagnostics;
    using System.Windows;
    using Microsoft.VisualStudio.Text;
    using Microsoft.VisualStudio.Text.Adornments;
    using Microsoft.VisualStudio.Text.Editor;
    using MonoDevelop.Components;

    /// <summary>
    /// An adornment provider that can create and display ToolTips taking an arbitrary object as content.
    /// </summary>
    internal class ToolTipProvider : IToolTipProvider
    {
        #region Private Members
        private readonly IMdTextView _textView;
        internal readonly ISpaceReservationManager _spaceReservationManager;
        internal ISpaceReservationAgent _agent;
        #endregion

        internal ToolTipProvider(IMdTextView textView)
        {
            _textView = textView;
            _spaceReservationManager = _textView.GetSpaceReservationManager("ToolTip");
            _spaceReservationManager.AgentChanged += OnAgentChanged;
        }

        void OnAgentChanged(object sender, SpaceReservationAgentChangedEventArgs e)
        {
            if (_agent == e.OldAgent)
                _agent = null;
        }

        #region IToolTipProvider Members
        public void ClearToolTip()
        {
            if (_agent != null)
            {
                _spaceReservationManager.RemoveAgent(_agent);
                //_agent should be null (cleared by OnAgentChanged)
            }
        }

        public void ShowToolTip(ITrackingSpan span, object toolTipContent, PopupStyles style)
        {
            if (span == null)
                throw new ArgumentNullException("span");
            if (span.TextBuffer != _textView.TextBuffer)
                throw new ArgumentException("Invalid span");
            if (toolTipContent == null)
                throw new ArgumentNullException("toolTipContent");

            var element = toolTipContent as Control;
            if (element == null)
            {
                string toolTipContentAsString = toolTipContent as string;
                if (toolTipContentAsString != null)
                {
                    element = BuildTooltipUIElement(toolTipContentAsString);
                }
                else
                {
                    throw new ArgumentException("Invalid contnet", nameof(toolTipContent));
                }
            }

            this.ClearToolTip();
            _agent = _spaceReservationManager.CreatePopupAgent(span, style, element);
            _spaceReservationManager.AddAgent(_agent);
        }

        public void ShowToolTip(ITrackingSpan span, object toolTipContent)
        {
            this.ShowToolTip(span, toolTipContent, PopupStyles.None);
        }
        #endregion

        internal static Control BuildTooltipUIElement(string toolTipText)
        {
            // Make a pretty ToolTip-looking thing, using a border and a TextBlock.
            //TextBlock txt = new TextBlock();
            //txt.Text = toolTipText;
            //txt.Background = SystemColors.InfoBrush;
            //txt.Foreground = SystemColors.InfoTextBrush;
            //txt.Padding = new Thickness(1.0);

            //Border border = new Border();
            //border.BorderBrush = SystemColors.WindowFrameBrush;
            //border.BorderThickness = new Thickness(1.0);
            //border.Child = txt;
            //return border;
            return new XwtControl(new Xwt.Label(toolTipText));
        }
    }
}