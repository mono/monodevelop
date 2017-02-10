namespace Microsoft.VisualStudio.Text.Implementation
{
    using System.Text;

    /// <summary>
    /// Corresponds to UTF8-no-BOM encoding.
    /// Determines whether multi-byte characters were decoded.
    /// Throws on invalid bytes.
    /// </summary>
    internal class ExtendedCharacterDetector : UTF8Encoding
    {
        internal bool DecodedExtendedCharacters { get; private set; }

        internal ExtendedCharacterDetector()
            : base(false, true)
        {
            DecodedExtendedCharacters = false;
        }

        public override Decoder GetDecoder()
        {
            return new ExtendedCharacterDetectionDecoder(base.GetDecoder(), HandleExtendedCharacter);
        }

        private void HandleExtendedCharacter()
        {
            DecodedExtendedCharacters = true;
        }
    }
}
