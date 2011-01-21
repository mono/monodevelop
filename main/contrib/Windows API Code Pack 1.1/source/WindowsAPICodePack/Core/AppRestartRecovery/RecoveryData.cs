//Copyright (c) Microsoft Corporation.  All rights reserved.

namespace Microsoft.WindowsAPICodePack.ApplicationServices
{
    /// <summary>
    /// The <see cref="System.Delegate"/> that represents the callback method invoked
    /// by the system when an application has registered for 
    /// application recovery. 
    /// </summary>
    /// <param name="state">An application-defined state object that is passed to the callback method.</param>
    /// <remarks>The callback method will be invoked
    /// prior to the application being terminated by Windows Error Reporting (WER). To keep WER from terminating the application before 
    /// the callback method completes, the callback method must
    /// periodically call the <see cref="ApplicationRestartRecoveryManager.ApplicationRecoveryInProgress"/> method. </remarks>
    /// <seealso cref="ApplicationRestartRecoveryManager.RegisterForApplicationRecovery(RecoverySettings)"/>
    public delegate int RecoveryCallback(object state);

    /// <summary>
    /// Defines a class that contains a callback delegate and properties of the application
    /// as defined by the user.
    /// </summary>
    public class RecoveryData
    {
        /// <summary>
        /// Initializes a recovery data wrapper with a callback method and the current
        /// state of the application.
        /// </summary>
        /// <param name="callback">The callback delegate.</param>
        /// <param name="state">The current state of the application.</param>
        public RecoveryData(RecoveryCallback callback, object state)
        {
            Callback = callback;
            State = state;
        }

        /// <summary>
        /// Gets or sets a value that determines the recovery callback function.
        /// </summary>
        public RecoveryCallback Callback { get; set; }

        /// <summary>
        /// Gets or sets a value that determines the application state.
        /// </summary>
        public object State { get; set; }

        /// <summary>
        /// Invokes the recovery callback function.
        /// </summary>
        public void Invoke()
        {
            if (Callback != null) { Callback(State); }
        }
    }
}
