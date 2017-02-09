//-----------------------------------------------------------------------
// <copyright company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Windows;

namespace Microsoft.VisualStudio.Language.Intellisense
{
    public class CodeLensPinEventArgs : EventArgs
    {
        private ICodeLensIndicatorService indicatorService;
        private ICodeLensDescriptor descriptor;
        private string indicatorName;

        internal CodeLensPinEventArgs(ICodeLensIndicatorService indicatorService, ICodeLensDescriptor descriptor, string indicatorName, CodeLensPresenterStyle presenterStyle, ResourceDictionary resourceDictionary)
        {
            this.descriptor = descriptor;
            this.indicatorName = indicatorName;
            this.PresenterStyle = presenterStyle;
            this.ResourceDictionary = resourceDictionary;
            this.indicatorService = indicatorService;
        }

        /// <summary>
        /// The localized name of the indicator.  This property is not set until CreateIndicator is called.
        /// </summary>
        public string LocalizedIndicatorName
        {
            get;
            private set;
        }

        public CodeLensPresenterStyle PresenterStyle { get; private set; }

        public ResourceDictionary ResourceDictionary { get; private set; }

        /// <summary>
        /// Factory method to create the indicator for this event 
        /// </summary>
        /// <returns>The indicator corresponding to the event arguments</returns>
        public ICodeLensIndicator CreateIndicator()
        {
            string localizedName;
            ICodeLensIndicator indicator = this.indicatorService.CreateIndicator(this.descriptor, this.indicatorName, out localizedName);
            this.LocalizedIndicatorName = localizedName;
            return indicator;
        }
    }
}
