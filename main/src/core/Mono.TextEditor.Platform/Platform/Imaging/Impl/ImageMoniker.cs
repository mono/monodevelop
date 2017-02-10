#if !TARGET_VS
using System;

namespace Microsoft.VisualStudio.Imaging.Interop
{
    public struct ImageMoniker
    {
        public Guid Guid;
        public int Id;

        public ImageMoniker(Guid guid, int id)
        {
            Guid = guid;
            Id   = id;
        }
    }
}
#endif
