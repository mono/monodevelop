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
    /// Scope used to store information about an IInputElement
    /// to which focus should be restored at the end of the scope.
    /// </summary>
    internal class RestoreFocusScope
    {
        private List<IInputElement> previousFocusAncestors;

        public RestoreFocusScope(IInputElement restoreFocus)
        {
            this.RestoreFocus = restoreFocus;
        }

        /// <summary>
        /// Restores focus to the correct element.
        /// </summary>
        public void PerformRestoration()
        {
            IInputElement restoreFocus = this.RestoreFocus;
            if (restoreFocus != null)
            {
                Keyboard.Focus(restoreFocus);
            }
        }

        /// <summary>
        /// Preserve and retrieve the IInputElement for focus restoration.
        /// When stored, all ancestors are remembered (as they could potentially become focusable later).
        /// When retrieved, only the top item still focusable and connected to the presentation source is returned.
        /// </summary>
        private IInputElement RestoreFocus
        {
            // Find the first focusable ancestor that is still connected to the presentation source
            get
            {
                IInputElement restoreFocus = null;

                if (previousFocusAncestors != null)
                {
                    restoreFocus = previousFocusAncestors.Find(e => PresentationSource.FromDependencyObject((DependencyObject)e) != null && e.Focusable) as IInputElement;
                }

                return restoreFocus;
            }
            // Collect all ancestors of the item, this is necessary if the item a the top
            // becomes unavailable for focus due to it being destroyed hidden or otherwise removed
            // from the visual tree.
            set
            {
                previousFocusAncestors = new List<IInputElement>();
                DependencyObject ancestor = value as DependencyObject;
                while (ancestor != null)
                {
                    IInputElement inputElement = ancestor as IInputElement;
                    if (inputElement != null)
                    {
                        previousFocusAncestors.Add(inputElement);
                    }
                    ancestor = VisualTreeExtensions.GetVisualOrLogicalParent(ancestor);
                }
            }
        }
    }
}
