using System.Diagnostics;

namespace GitSharp.Core
{
    public static class Ensure
    {
        public static void That(bool istrue)
        {
            Debug.Assert(istrue);
        }
    }
}
