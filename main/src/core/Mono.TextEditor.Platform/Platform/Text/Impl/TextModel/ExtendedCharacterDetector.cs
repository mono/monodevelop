//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
// This file contain implementations details that are subject to change without notice.
// Use at your own risk.
//
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
