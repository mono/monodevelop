using System;

namespace Microsoft.WindowsAPICodePack.Shell
{
    /// <summary>
    /// Event argument for The GlassAvailabilityChanged event
    /// </summary>
    public class AeroGlassCompositionChangedEventArgs : EventArgs
    {        
        internal AeroGlassCompositionChangedEventArgs(bool avialbility)
        {
            GlassAvailable = avialbility;
        }

        /// <summary>
        /// The new GlassAvailable state
        /// </summary>
        public bool GlassAvailable { get; private set; }

    }

}
