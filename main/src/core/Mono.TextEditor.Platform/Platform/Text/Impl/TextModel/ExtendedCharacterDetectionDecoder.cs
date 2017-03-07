//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
// This file contain implementations details that are subject to change without notice.
// Use at your own risk.
//
namespace Microsoft.VisualStudio.Text.Implementation
{
    using System;
    using System.Text;

    /// <summary>
    /// Decoder that detects non-ASCII characters.
    /// </summary>
    class ExtendedCharacterDetectionDecoder : Decoder
    {
        private Decoder decoder;
        private Action response;

        internal ExtendedCharacterDetectionDecoder(Decoder decoder, Action response)
        {
            this.decoder = decoder;
            this.response = response;
        }

        public override int GetCharCount(byte[] bytes, int index, int count)
        {
            return decoder.GetCharCount(bytes, index, count);
        }

        public override int GetChars(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex)
        {
            int charCount = decoder.GetChars(bytes, byteIndex, byteCount, chars, charIndex);
            if (response != null)
            {
                int maxCharIndex = charIndex + charCount;
                for (int i = charIndex; i < maxCharIndex; i++)
                {
                    if (chars[i] > 0x7F)
                    {
                        response();
                        response = null;
                        break;
                    }
                }
            }
            return charCount;
        }
    }
}
