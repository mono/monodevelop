using Microsoft.VisualStudio.Utilities;
using System;

namespace Microsoft.VisualStudio.Language.Intellisense
{
    /// <summary>
    /// A MEF attribute for providing the display status of a CodeLens indicator option.
    /// </summary>
    public sealed class CodeLensOptionBehaviorAttribute : SingletonBaseMetadataAttribute
    {
        private readonly bool modifiable;
        private readonly bool visible;

        /// <summary>
        /// CodeLensOptionBehaviorAttribute determines how the CodeLens indicator option is displayed
        /// to the user.
        /// </summary>
        /// <param name="modifiable">if false the option will appear grayed out</param>
        /// <param name="visible">if false the option will not be shown</param>
        public CodeLensOptionBehaviorAttribute(bool modifiable=true, bool visible=true)
        {
            this.modifiable = modifiable;
            this.visible = visible;
        }

        /// <summary>
        /// Gets whether or not the option's value can be modified. Editing the option is disabled if OptionModifiable is false
        /// </summary>
        public bool OptionModifiable
        {
            get
            {
                return this.modifiable;
            }
        }

        /// <summary>
        /// Gets the visibility state of the option.
        /// </summary>
        public bool OptionVisible
        {
            get
            {
                return this.visible;
            }
        }
    }
}
