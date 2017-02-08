using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.VisualStudio.Text.Implementation
{
    // The .NET Encoding DecoderFallbacks do not allow one to easily determine
    // whether a fallback actually occurs so this type overrides whatever is necessary
    // to detect it.
    internal class FallbackDetector : DecoderFallback
    {
        private DecoderFallback decoderFallback;

        internal bool FallbackOccurred { get; private set; }

        public FallbackDetector(DecoderFallback decoderFallback)
        {
            this.decoderFallback = decoderFallback;
        }

        public override DecoderFallbackBuffer CreateFallbackBuffer()
        {
            var buffer = new FallbackBufferDetector(this.decoderFallback.CreateFallbackBuffer());
            buffer.FallbackOccurred += (s, e) => this.FallbackOccurred = true;
            return buffer;
        }

        public override int MaxCharCount
        {
            get { return this.decoderFallback.MaxCharCount; }
        }

        private class FallbackBufferDetector : DecoderFallbackBuffer
        {
            private DecoderFallbackBuffer inner;

            internal event EventHandler FallbackOccurred;

            internal FallbackBufferDetector(DecoderFallbackBuffer inner)
            {
                this.inner = inner;
            }

            public override bool Fallback(byte[] bytesUnknown, int index)
            {
                if (this.FallbackOccurred != null)
                    this.FallbackOccurred(this, EventArgs.Empty);

                return this.inner.Fallback(bytesUnknown, index);
            }

            public override char GetNextChar()
            {
                return this.inner.GetNextChar();
            }

            public override bool MovePrevious()
            {
                return this.inner.MovePrevious();
            }

            public override int Remaining
            {
                get { return this.inner.Remaining; }
            }
        }
    }
}
