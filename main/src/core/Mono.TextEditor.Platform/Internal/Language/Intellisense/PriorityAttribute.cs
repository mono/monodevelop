using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.Language.Intellisense
{
    /// <summary>
    /// Represents an attribute which assigns an integer priority to a MEF extension.
    /// </summary>
    public sealed class PriorityAttribute : SingletonBaseMetadataAttribute
    {
        private readonly int priority;

        /// <summary>
        /// Creates a new instance of this attribute, assigning it a priority value.
        /// </summary>
        /// <param name="priority">The priority for the MEF extension.  Lower integer
        /// values represent higher precedence.</param>
        public PriorityAttribute(int priority)
        {
            this.priority = priority;
        }

        /// <summary>
        /// Gets the priority for the attributed MEF extension.
        /// </summary>
        public int Priority
        {
            get
            {
                return this.priority;
            }
        }
    }
}
