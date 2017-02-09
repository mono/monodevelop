using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace Microsoft.VisualStudio.Language.Intellisense
{
    /// <summary>
    /// Allows to register and listen to access keys in VS.
    /// </summary>
    public interface IVSAccessKeyManagerInternal
    {
        /// <summary>
        /// Register for an access key in VS.
        /// </summary>
        /// <param name="key">The access key to be registered</param>
        /// <param name="currentElement">The wpf element that is trying to register.</param>
        /// <param name="presentationSource">The presentation source of the element that is being used to register the access key</param>
        void RegisterAccessKey(string key, IInputElement currentElement, PresentationSource presentationSource);

        /// <summary>
        /// Disassociates the specified access keys from the specified element
        /// </summary>
        /// <param name="key">The access key to be registered</param>
        /// <param name="currentElement">The wpf element that is trying to register.</param>
        /// <param name="presentationSource">The presentation source of the element that is being used to register the access key</param>
        void UnRegisterAccessKey(string key, IInputElement currentElement, PresentationSource presentationSource);

        /// <summary>
        /// Adds a handler for the AccessKeyPressed attached event.
        /// </summary>
        /// <param name="currentElement">The wpf element that is trying to register.</param>
        /// <param name="presentationSource">The presentation source of the element that is being used to register the access ke</param>
        /// <param name="onAccessKeyPressedHandler">The event handler that will be called when the access key is pressed. You should mark the event handled if your conditions are satisfied and then mark yourself as the target so that OnAccessKey Event is then called for your target.</param>
        void AddVSAccessKeyPressedHandler(IInputElement currentElement, PresentationSource presentationSource, AccessKeyPressedEventHandler onAccessKeyPressedHandler);

        /// <summary>
        /// Removes the specified AccessKeyPressed event handler from the specified object.
        /// </summary>
        /// <param name="currentElement">The wpf element that is trying to register.</param>
        /// <param name="presentationSource">The presentation source of the element that is being used to register the access ke</param>
        /// <param name="onAccessKeyPressedHandler">The event handler that will be called when the access key is pressed. You should mark the event handled if your conditions are satisfied and then mark yourself as the target so that OnAccessKey Event is then called for your target.</param>
        void RemoveVSAccessKeyPressedHandler(IInputElement currentElement, PresentationSource presentationSource, AccessKeyPressedEventHandler onAccessKeyPressedHandler);

        /// <summary>
        /// Indicates whether the specified key is registered as an access keys for this specific element.
        /// </summary>
        /// <param name="element">The object on which the query is made. It is usually the input element to which the key is registered</param>
        /// <param name="key">The key you want to query</param>
        bool IsVSAccessKeyRegisteredByElement(IInputElement element, string key);

        /// <summary>
        /// Indicates whether the specified key is registered as an access key.
        /// </summary>
        /// <param name="key">The key you want to query</param>
        bool IsVSAccessKeyRegistered(string key);

    }
}
