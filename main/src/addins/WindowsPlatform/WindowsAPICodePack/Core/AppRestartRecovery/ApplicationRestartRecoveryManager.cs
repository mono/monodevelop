//Copyright (c) Microsoft Corporation.  All rights reserved.

using System;
using System.Runtime.InteropServices;
using Microsoft.WindowsAPICodePack.Resources;
using MS.WindowsAPICodePack.Internal;

namespace Microsoft.WindowsAPICodePack.ApplicationServices
{
    /// <summary>
    /// Provides access to the Application Restart and Recovery
    /// features available in Windows Vista or higher. Application Restart and Recovery lets an
    /// application do some recovery work to save data before the process exits.
    /// </summary>
    public static class ApplicationRestartRecoveryManager
    {
        /// <summary>
        /// Registers an application for recovery by Application Restart and Recovery.
        /// </summary>
        /// <param name="settings">An object that specifies
        /// the callback method, an optional parameter to pass to the callback
        /// method and a time interval.</param>
        /// <exception cref="System.ArgumentException">
        /// The registration failed due to an invalid parameter.
        /// </exception>
        /// <exception cref="System.ComponentModel.Win32Exception">
        /// The registration failed.</exception>
        /// <remarks>The time interval is the period of time within 
        /// which the recovery callback method 
        /// calls the <see cref="ApplicationRecoveryInProgress"/> method to indicate
        /// that it is still performing recovery work.</remarks>        
        public static void RegisterForApplicationRecovery(RecoverySettings settings)
        {           
            CoreHelpers.ThrowIfNotVista();

            if (settings == null) { throw new ArgumentNullException("settings"); }

            GCHandle handle = GCHandle.Alloc(settings.RecoveryData);

            HResult hr = AppRestartRecoveryNativeMethods.RegisterApplicationRecoveryCallback(
                AppRestartRecoveryNativeMethods.InternalCallback, (IntPtr)handle, settings.PingInterval, (uint)0);

            if (!CoreErrorHelper.Succeeded(hr))
            {
                if (hr == HResult.InvalidArguments)
                {
                    throw new ArgumentException(LocalizedMessages.ApplicationRecoveryBadParameters, "settings");
                }

                throw new ApplicationRecoveryException(LocalizedMessages.ApplicationRecoveryFailedToRegister);
            }
        }

        /// <summary>
        /// Removes an application's recovery registration.
        /// </summary>
        /// <exception cref="Microsoft.WindowsAPICodePack.ApplicationServices.ApplicationRecoveryException">
        /// The attempt to unregister for recovery failed.</exception>
        public static void UnregisterApplicationRecovery()
        {            
            CoreHelpers.ThrowIfNotVista();

            HResult hr = AppRestartRecoveryNativeMethods.UnregisterApplicationRecoveryCallback();

            if (!CoreErrorHelper.Succeeded(hr))
            {
                throw new ApplicationRecoveryException(LocalizedMessages.ApplicationRecoveryFailedToUnregister);
            }
        }

        /// <summary>
        /// Removes an application's restart registration.
        /// </summary>
        /// <exception cref="Microsoft.WindowsAPICodePack.ApplicationServices.ApplicationRecoveryException">
        /// The attempt to unregister for restart failed.</exception>
        public static void UnregisterApplicationRestart()
        {            
            CoreHelpers.ThrowIfNotVista();

            HResult hr = AppRestartRecoveryNativeMethods.UnregisterApplicationRestart();

            if (!CoreErrorHelper.Succeeded(hr))
            {
                throw new ApplicationRecoveryException(LocalizedMessages.ApplicationRecoveryFailedToUnregisterForRestart);
            }
        }

        /// <summary>
        /// Called by an application's <see cref="RecoveryCallback"/> method 
        /// to indicate that it is still performing recovery work.
        /// </summary>
        /// <returns>A <see cref="System.Boolean"/> value indicating whether the user
        /// canceled the recovery.</returns>
        /// <exception cref="Microsoft.WindowsAPICodePack.ApplicationServices.ApplicationRecoveryException">
        /// This method must be called from a registered callback method.</exception>
        public static bool ApplicationRecoveryInProgress()
        {            
            CoreHelpers.ThrowIfNotVista();

            bool canceled = false;
            HResult hr = AppRestartRecoveryNativeMethods.ApplicationRecoveryInProgress(out canceled);

            if (!CoreErrorHelper.Succeeded(hr))
            {
                throw new InvalidOperationException(LocalizedMessages.ApplicationRecoveryMustBeCalledFromCallback);
            }

            return canceled;
        }

        /// <summary>
        /// Called by an application's <see cref="RecoveryCallback"/> method to 
        /// indicate that the recovery work is complete.
        /// </summary>
        /// <remarks>
        /// This should
        /// be the last call made by the <see cref="RecoveryCallback"/> method because
        /// Windows Error Reporting will terminate the application
        /// after this method is invoked.
        /// </remarks>
        /// <param name="success"><b>true</b> to indicate the the program was able to complete its recovery
        /// work before terminating; otherwise <b>false</b>.</param>
        public static void ApplicationRecoveryFinished(bool success)
        {            
            CoreHelpers.ThrowIfNotVista();

            AppRestartRecoveryNativeMethods.ApplicationRecoveryFinished(success);
        }

        /// <summary>
        /// Registers an application for automatic restart if 
        /// the application 
        /// is terminated by Windows Error Reporting.
        /// </summary>
        /// <param name="settings">An object that specifies
        /// the command line arguments used to restart the 
        /// application, and 
        /// the conditions under which the application should not be 
        /// restarted.</param>
        /// <exception cref="System.ArgumentException">Registration failed due to an invalid parameter.</exception>
        /// <exception cref="System.InvalidOperationException">The attempt to register failed.</exception>
        /// <remarks>A registered application will not be restarted if it executed for less than 60 seconds before terminating.</remarks>
        public static void RegisterForApplicationRestart(RestartSettings settings)
        {
            // Throw PlatformNotSupportedException if the user is not running Vista or beyond
            CoreHelpers.ThrowIfNotVista();
            if (settings == null) { throw new ArgumentNullException("settings"); }

            HResult hr = AppRestartRecoveryNativeMethods.RegisterApplicationRestart(settings.Command, settings.Restrictions);

            if (hr == HResult.Fail)
            {
                throw new InvalidOperationException(LocalizedMessages.ApplicationRecoveryFailedToRegisterForRestart);
            }
            else if (hr == HResult.InvalidArguments)
            {
                throw new ArgumentException(LocalizedMessages.ApplicationRecoverFailedToRegisterForRestartBadParameters);
            }
        }

    }
}

