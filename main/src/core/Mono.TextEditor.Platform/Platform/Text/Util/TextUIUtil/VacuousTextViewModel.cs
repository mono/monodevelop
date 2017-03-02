//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
namespace Microsoft.VisualStudio.Text.Utilities
{
    using System;
    using Microsoft.VisualStudio.Utilities;
    using Microsoft.VisualStudio.Text;
    using Microsoft.VisualStudio.Text.Editor;

    /// <summary>
    /// Implements a text view model that simply reexposes the data buffer and an optional edit buffer. The VisualBuffer
    /// is the same as the edit buffer, which is in turn the same as the data buffer if no edit buffer is specified.
    /// This is the default if no view model provider is specified or if the specified one declines to build a model.
    /// </summary>
    public class VacuousTextViewModel : ITextViewModel
    {
        private ITextDataModel dataModel;
        private ITextBuffer editBuffer;

        public VacuousTextViewModel(ITextDataModel dataModel) : this(dataModel, dataModel.DataBuffer) { }

        public VacuousTextViewModel(ITextDataModel dataModel, ITextBuffer editBuffer)
        {
            this.dataModel = dataModel;
            this.editBuffer = editBuffer;
            this.Properties = new PropertyCollection();
        }

        public ITextDataModel DataModel
        {
            get { return this.dataModel; }
        }

        public ITextBuffer DataBuffer
        {
            get { return this.dataModel.DataBuffer; }
        }

        public ITextBuffer EditBuffer
        {
            get { return this.editBuffer; }
        }

        public ITextBuffer VisualBuffer
        {
            get { return this.EditBuffer; }
        }

        public void Dispose() 
        { 
            GC.SuppressFinalize(this);
        }

        public PropertyCollection Properties { get; internal set; }


        public SnapshotPoint GetNearestPointInVisualBuffer(SnapshotPoint editBufferPoint)
        {
            // The edit buffer is the same as the visual buffer, so just return the passed-in point.
            return editBufferPoint;
        }

        public SnapshotPoint GetNearestPointInVisualSnapshot(SnapshotPoint editBufferPoint, ITextSnapshot targetVisualSnapshot, PointTrackingMode trackingMode)
        {
            // The edit buffer is the same as the visual buffer, so just return the passed-in point translated to the correct snapshot.
            return editBufferPoint.TranslateTo(targetVisualSnapshot, trackingMode);
        }

        public bool IsPointInVisualBuffer(SnapshotPoint editBufferPoint, PositionAffinity affinity)
        {
            // The edit buffer is the same as the visual buffer, so just return true.
            return true;
        }
    }
}
