////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (c) Microsoft Corporation.  All rights reserved.
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.VisualStudio.Language.Intellisense
{
    /// <summary>
    /// Provides information about the change of the current parameter in a signature help session.
    /// </summary>
    public class CurrentParameterChangedEventArgs : EventArgs
    {
        private IParameter _prevCurrentParameter;
        private IParameter _newCurrentParameter;

        /// <summary>
        /// Initializes a new instance of <see cref="CurrentParameterChangedEventArgs"/>.
        /// </summary>
        /// <param name="previousCurrentParameter">The parameter that was previously the current parameter.</param>
        /// <param name="newCurrentParameter">The parameter that is now the current parameter.</param>
        public CurrentParameterChangedEventArgs(IParameter previousCurrentParameter, IParameter newCurrentParameter)
        {
            _prevCurrentParameter = previousCurrentParameter;
            _newCurrentParameter = newCurrentParameter;
        }

        /// <summary>
        /// Gets the parameter that was previously the current parameter.
        /// </summary>
        public IParameter PreviousCurrentParameter
        {
            get { return _prevCurrentParameter; }
        }

        /// <summary>
        /// Gets the parameter that is now the current parameter.
        /// </summary>
        public IParameter NewCurrentParameter
        {
            get { return _newCurrentParameter; }
        }
    }
}
