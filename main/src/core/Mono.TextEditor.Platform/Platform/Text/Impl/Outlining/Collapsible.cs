//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
// This file contain implementations details that are subject to change without notice.
// Use at your own risk.
//
namespace Microsoft.VisualStudio.Text.Outlining
{
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.VisualStudio.Text.Tagging;
    using Microsoft.VisualStudio.Text.Utilities;
    using System;

    /// <summary>
    /// An un-collapsed region (also the base for collapsed regions).
    /// </summary>
    internal class Collapsible : ICollapsible
    {
        public ITrackingSpan Extent { get; private set; }
        
        public bool IsCollapsed { get; internal set; }

        public object CollapsedForm
        {
            get { return ((Tag != null) ? Tag.CollapsedForm : null); }
        }

        public object CollapsedHintForm
        {
            get { return ((Tag != null) ? Tag.CollapsedHintForm : null); }
        }

        public IOutliningRegionTag Tag { get; internal set; }

        public bool IsCollapsible
        {
            get
            {
                return true;
            }
        }

        public override bool Equals(object obj)
        {
            Collapsible other = obj as Collapsible;
            if (other != null)
            {
                return Tag.Equals(other.Tag) &&
                       Extent.Equals(other.Extent) &&
                       IsCollapsed == other.IsCollapsed;
            }
            
            return false;
        }

        public override int GetHashCode()
        {
            return Tag.GetHashCode() ^ Extent.GetHashCode() ^ IsCollapsed.GetHashCode();
        }
        

        public Collapsible(ITrackingSpan underlyingSpan, IOutliningRegionTag tag)
        {
            Extent = underlyingSpan;
            Tag = tag;

            IsCollapsed = false;
        }
    }

    /// <summary>
    /// A collapsed region.
    /// </summary>
    internal class Collapsed : Collapsible, ICollapsed
    {
        public TrackingSpanNode<Collapsed> Node { get; set; }

        public bool IsValid { get { return IsCollapsed; } }

        public IEnumerable<ICollapsed> CollapsedChildren
        {
            get 
            {
                if (!IsValid)
                    throw new InvalidOperationException("This collapsed region is no longer valid, as it has been expanded already.");

                return Node.Children.Select(child => child.Item); 
            }
        }

        public void Invalidate()
        {
            this.IsCollapsed = false;
            this.Node = null;
        }

        public Collapsed(ITrackingSpan extent, IOutliningRegionTag tag)
            : base(extent, tag)
        {
            IsCollapsed = true;
        }
    }
}
