// Copyright (c) Microsoft Corporation.  All rights reserved.

using System;

namespace Microsoft.WindowsAPICodePack.ExtendedLinguisticServices
{

    /// <summary>
    /// A <see cref="MappingAsyncResult">MappingAsyncResult</see> subclass to be used only for asynchronous calls to service actions.
    /// <seealso cref="MappingService.BeginDoAction">MappingService.BeginDoAction</seealso>
    /// </summary>
    public class MappingActionAsyncResult : MappingAsyncResult
    {
        
        internal MappingActionAsyncResult(
            object callerData,
            AsyncCallback asyncCallback,
            MappingPropertyBag bag,
            int rangeIndex,
            string actionId)
            : base(callerData, asyncCallback)
        {
            base.SetResult(bag, new MappingResultState());
            RangeIndex = rangeIndex;
            ActionId = actionId;
        }

        /// <summary>
        /// Gets the range index parameter for <see cref="MappingService.DoAction">MappingService.DoAction</see> or <see cref="MappingService.BeginDoAction">MappingService.BeginDoAction</see>.
        /// </summary>
        public int RangeIndex { get; private set; }

        /// <summary>
        /// Gets the action ID parameter for <see cref="MappingService.DoAction">MappingService.DoAction</see> or <see cref="MappingService.BeginDoAction">MappingService.BeginDoAction</see>.
        /// </summary>
        public string ActionId { get; private set; }
    }

}
