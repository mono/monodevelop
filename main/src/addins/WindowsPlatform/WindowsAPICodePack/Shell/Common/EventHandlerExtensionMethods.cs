using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Threading;

namespace Microsoft.WindowsAPICodePack.Shell
{
    /// <summary>
    /// Provides extension methods for raising events safely.
    /// </summary>
    public static class EventHandlerExtensionMethods
    {       
        /// <summary>
        /// Safely raises an event using EventArgs.Empty
        /// </summary>
        /// <param name="eventHandler">EventHandler to raise</param>
        /// <param name="sender">Event sender</param>
        public static void SafeRaise(this EventHandler eventHandler, object sender)
        {
            if (eventHandler != null)
            {
                eventHandler(sender, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Safely raises an event.
        /// </summary>
        /// <typeparam name="T">Type of event args</typeparam>
        /// <param name="eventHandler">EventHandler&lt;T&gt; to raise</param>
        /// <param name="sender">Event sender</param>
        /// <param name="args">Event args</param>
        public static void SafeRaise<T>(this EventHandler<T> eventHandler, object sender, T args) where T : EventArgs
        {
            if (eventHandler != null)
            {
                eventHandler(sender, args);                                
            }
        }

        /// <summary>
        /// Safely raises an event using EventArgs.Empty
        /// </summary>
        /// <param name="eventHandler">EventHandler&lt;EventArgs&gt; to raise</param>
        /// <param name="sender">Event sender</param>
        public static void SafeRaise(this EventHandler<EventArgs> eventHandler, object sender)
        {
            SafeRaise(eventHandler, sender, EventArgs.Empty);
        }
    }
}
