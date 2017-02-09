using System.Windows;

namespace Microsoft.VisualStudio.Language.Intellisense
{
    public static class CodeLensTextViewProperties
    {
        public static readonly object ActiveAdornmentHostPropertyKey = new ComponentResourceKey(typeof(CodeLensTextViewProperties), "ActiveAdornmentHost");

        public static readonly object ShowAccessKeysPropertyKey = new ComponentResourceKey(typeof(CodeLensTextViewProperties), "ShowAccessKeys");
    }
}
