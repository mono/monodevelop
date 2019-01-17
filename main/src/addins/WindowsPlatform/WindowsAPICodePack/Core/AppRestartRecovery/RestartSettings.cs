//Copyright (c) Microsoft Corporation.  All rights reserved.

using System;
using Microsoft.WindowsAPICodePack.Resources;

namespace Microsoft.WindowsAPICodePack.ApplicationServices
{
    /// <summary>
    /// Specifies the options for an application to be automatically
    /// restarted by Windows Error Reporting. 
    /// </summary>
    /// <remarks>Regardless of these 
    /// settings, the application
    /// will not be restarted if it executed for less than 60 seconds before
    /// terminating.</remarks>
    public class RestartSettings
    {
        private string command;
        private RestartRestrictions restrictions;

        /// <summary>
        /// Creates a new instance of the RestartSettings class.
        /// </summary>
        /// <param name="command">The command line arguments 
        /// used to restart the application.</param>
        /// <param name="restrictions">A bitwise combination of the RestartRestrictions 
        /// values that specify  
        /// when the application should not be restarted.
        /// </param>
        public RestartSettings(string command, RestartRestrictions restrictions)
        {
            this.command = command;
            this.restrictions = restrictions;
        }

        /// <summary>
        /// Gets the command line arguments used to restart the application.
        /// </summary>
        /// <value>A <see cref="System.String"/> object.</value>
        public string Command
        {
            get { return command; }
        }

        /// <summary>
        /// Gets the set of conditions when the application 
        /// should not be restarted.
        /// </summary>
        /// <value>A set of <see cref="RestartRestrictions"/> values.</value>
        public RestartRestrictions Restrictions
        {
            get { return restrictions; }
        }

        /// <summary>
        /// Returns a string representation of the current state
        /// of this object.
        /// </summary>
        /// <returns>A <see cref="System.String"/> that displays 
        /// the command line arguments 
        /// and restrictions for restarting the application.</returns>
        public override string ToString()
        {
            return string.Format(System.Globalization.CultureInfo.InvariantCulture,
                LocalizedMessages.RestartSettingsFormatString,
                command, restrictions.ToString());
        }
    }
}

