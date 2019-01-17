//Copyright (c) Microsoft Corporation.  All rights reserved.

namespace Microsoft.WindowsAPICodePack.ApplicationServices
{
    /// <summary>
    /// Specifies the power source currently supplying power to the system.
    /// </summary>
    /// <remarks>Application should be aware of the power source because 
    /// some power sources provide a finite power supply.
    /// An application might take steps to conserve power while 
    /// the system is using such a source.
    /// </remarks>
    public enum PowerSource
    {
        /// <summary>
        /// The computer is powered by an AC power source 
        /// or a similar device, such as a laptop powered 
        /// by a 12V automotive adapter.
        /// </summary>
        AC = 0,
        /// <summary>
        /// The computer is powered by a built-in battery. 
        /// A battery has a limited 
        /// amount of power; applications should conserve resources
        /// where possible.
        /// </summary>
        Battery = 1,
        /// <summary>
        /// The computer is powered by a short-term power source 
        /// such as a UPS device.
        /// </summary>
        Ups = 2
    }
}
