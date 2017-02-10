namespace Microsoft.VisualStudio.Text.Utilities
{
    using System.Collections.Generic;

    public interface ITextViewRoleMetadata
    {
        IEnumerable<string> TextViewRoles { get; }
    }
}