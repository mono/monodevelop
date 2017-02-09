// Copyright (c) Microsoft Corporation
// All rights reserved

namespace Microsoft.VisualStudio.Text.Editor
{
    using System.Windows;
    using System.Windows.Controls;

    /// <summary>
    /// Represents collapsed text in the text view.
    /// </summary>
    /// <remarks>
    /// By default, this is a gray rectangle with gray text.
    /// </remarks>
    public abstract class OutliningCollapsedAdornmentControl : ContentControl
    {
        static OutliningCollapsedAdornmentControl()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(OutliningCollapsedAdornmentControl), new FrameworkPropertyMetadata(typeof(OutliningCollapsedAdornmentControl)));
        }
    }

    /// <summary>
    /// Allows collapsing and expanding an outlining region.
    /// </summary>
    /// <remarks>
    /// By default, this is a gray square with a plus or minus.
    /// </remarks>
    public abstract class OutliningMarginHeaderControl : Control
    {
        static OutliningMarginHeaderControl()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(OutliningMarginHeaderControl), new FrameworkPropertyMetadata(typeof(OutliningMarginHeaderControl)));
        }

        #region bool IsExpanded dependency property

        /// <summary>
        /// A <see cref="DependencyProperty"/> that determines whether this control collapses or expands
        /// the outlining regions that it controls.
        /// </summary>
        public static readonly DependencyProperty IsExpandedProperty = DependencyProperty.RegisterAttached("IsExpanded", typeof(bool), typeof(OutliningMarginHeaderControl));

        /// <summary>
        /// Sets <see cref="IsExpandedProperty"/>.
        /// </summary>
        public static void SetIsExpanded(OutliningMarginHeaderControl control, bool isExpanded)
        {
            control.SetValue(IsExpandedProperty, isExpanded);
        }

        /// <summary>
        /// Gets <see cref="IsExpandedProperty"/>.
        /// </summary>
        public static bool GetIsExpanded(OutliningMarginHeaderControl control)
        {
            return true.Equals(control.GetValue(IsExpandedProperty));
        }

        /// <summary>
        /// Gets or sets <see cref="IsExpandedProperty"/>.
        /// </summary>
        public bool IsExpanded
        {
            get
            {
                return GetIsExpanded(this);
            }
            set
            {
                SetIsExpanded(this, value);
            }
        }
        #endregion

        #region bool IsHighlighted dependency property

        /// <summary>
        /// A <see cref="DependencyProperty"/> that determines whether this control should be currently displaying its mouse-hover highlight.
        /// </summary>
        public static readonly DependencyProperty IsHighlightedProperty = DependencyProperty.RegisterAttached("IsHighlighted", typeof(bool), typeof(OutliningMarginHeaderControl));

        /// <summary>
        /// Sets <see cref="IsHighlightedProperty"/>.
        /// </summary>
        public static void SetIsHighlighted(OutliningMarginHeaderControl control, bool isExpanded)
        {
            control.SetValue(IsHighlightedProperty, isExpanded);
        }

        /// <summary>
        /// Gets <see cref="IsHighlightedProperty"/>.
        /// </summary>
        public static bool GetIsHighlighted(OutliningMarginHeaderControl control)
        {
            return true.Equals(control.GetValue(IsHighlightedProperty));
        }

        /// <summary>
        /// Gets or sets <see cref="IsHighlightedProperty"/>.
        /// </summary>
        public bool IsHighlighted
        {
            get
            {
                return GetIsHighlighted(this);
            }
            set
            {
                SetIsHighlighted(this, value);
            }
        }
        #endregion
    }

    /// <summary>
    /// Indicates the vertical extent of an expanded outlining region
    /// and allows the user to collapse it.
    /// </summary>
    public abstract class OutliningMarginBracketControl : Control
    {
        static OutliningMarginBracketControl()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(OutliningMarginBracketControl), new FrameworkPropertyMetadata(typeof(OutliningMarginBracketControl)));
        }

        #region bool IsHighlighted dependency property

        /// <summary>
        /// A <see cref="DependencyProperty"/> that determines whether this control should be currently displaying its mouse-hover highlight.
        /// </summary>
        public static readonly DependencyProperty IsHighlightedProperty =
            DependencyProperty.RegisterAttached("IsHighlighted", typeof(bool), typeof(OutliningMarginBracketControl),
            new FrameworkPropertyMetadata(IsHighlightedPropertyChangedCallback));

        /// <summary>
        /// Sets <see cref="IsHighlightedProperty"/>.
        /// </summary>
        public static void SetIsHighlighted(OutliningMarginBracketControl control, bool isExpanded)
        {
            control.SetValue(IsHighlightedProperty, isExpanded);
        }

        /// <summary>
        /// Gets <see cref="IsHighlightedProperty"/>.
        /// </summary>
        public static bool GetIsHighlighted(OutliningMarginBracketControl control)
        {
            return true.Equals(control.GetValue(IsHighlightedProperty));
        }

        /// <summary>
        /// Gets or sets <see cref="IsHighlightedProperty"/>.
        /// </summary>
        public bool IsHighlighted
        {
            get
            {
                return GetIsHighlighted(this);
            }
            set
            {
                SetIsHighlighted(this, value);
            }
        }

        private static void IsHighlightedPropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs args)
        {
            ((OutliningMarginBracketControl)d).OnIsHighlightedChanged((bool)args.NewValue);
        }

        /// <summary>
        /// The event handler called when <see cref="OutliningMarginBracketControl.IsHighlighted"/> is changed.
        /// </summary>
        /// <param name="newValue">The new value of <see cref="OutliningMarginBracketControl.IsHighlighted"/>.</param>
        protected virtual void OnIsHighlightedChanged(bool newValue)
        {
        }
        #endregion

        #region double FirstLineOffset dependency property

        /// <summary>
        /// A <see cref="DependencyProperty"/> that indicates the vertical offset that the bracket control should use to render itself.
        /// </summary>
        public static readonly DependencyProperty FirstLineOffsetProperty = DependencyProperty.RegisterAttached("FirstLineOffset", typeof(double), typeof(OutliningMarginBracketControl));

        /// <summary>
        /// Sets <see cref="FirstLineOffsetProperty"/>.
        /// </summary>
        public static void SetFirstLineOffset(OutliningMarginBracketControl control, double firstLineOffset)
        {
            control.SetValue(FirstLineOffsetProperty, firstLineOffset);
        }

        /// <summary>
        /// Gets <see cref="FirstLineOffsetProperty"/>.
        /// </summary>
        public static double GetFirstLineOffset(OutliningMarginBracketControl control)
        {
            return (double)control.GetValue(FirstLineOffsetProperty);
        }

        /// <summary>
        /// Gets or sets <see cref="FirstLineOffsetProperty"/>.
        /// </summary>
        public double FirstLineOffset
        {
            get
            {
                return GetFirstLineOffset(this);
            }
            set
            {
                SetFirstLineOffset(this, value);
            }
        }
        #endregion
    }

    /// <summary>
    /// Highlights an outlining region in the text view when the mouse hovers over this region in the outlining margin.
    /// </summary>
    public abstract class CollapseHintAdornmentControl : Control
    {
        #region bool IsHighlighted dependency property

        /// <summary>
        /// A <see cref="DependencyProperty"/> that determines whether this control should be currently displaying its mouse-hover highlight.
        /// </summary>
        /// <remarks>
        /// This control should display nothing at all when this property is false.
        /// </remarks>
        public static readonly DependencyProperty IsHighlightedProperty = DependencyProperty.RegisterAttached("IsHighlighted", typeof(bool), typeof(CollapseHintAdornmentControl));

        /// <summary>
        /// Sets <see cref="IsHighlightedProperty"/>.
        /// </summary>
        public static void SetIsHighlighted(CollapseHintAdornmentControl control, bool isExpanded)
        {
            control.SetValue(IsHighlightedProperty, isExpanded);
        }

        /// <summary>
        /// Gets <see cref="IsHighlightedProperty"/>.
        /// </summary>
        public static bool GetIsHighlighted(CollapseHintAdornmentControl control)
        {
            return true.Equals(control.GetValue(IsHighlightedProperty));
        }

        /// <summary>
        /// Gets or sets <see cref="IsHighlightedProperty"/>.
        /// </summary>
        public bool IsHighlighted
        {
            get
            {
                return GetIsHighlighted(this);
            }
            set
            {
                SetIsHighlighted(this, value);
            }
        }
        #endregion

        static CollapseHintAdornmentControl()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(CollapseHintAdornmentControl), new FrameworkPropertyMetadata(typeof(CollapseHintAdornmentControl)));
        }
    }

    /// <summary>
    /// Represents the outlining margin.
    /// </summary>
    public abstract class OutliningMarginControl : ContentControl
    {
        static OutliningMarginControl()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(OutliningMarginControl), new FrameworkPropertyMetadata(typeof(OutliningMarginControl)));
        }
    }
}
