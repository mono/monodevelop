// Copyright (c) Microsoft Corporation
// All rights reserved

namespace Microsoft.VisualStudio.Text.Implementation
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using Microsoft.VisualStudio.Text.Utilities;

    internal class EncodedStreamReader
    {
        /// <summary>
        /// Open a stream reader from the specified stream, using the default encoding
        /// unless either byte order marks are found (specifying a different encoding)
        /// or -- if no byte order marks are found -- one of the given encoding detectors
        /// can deduce an appropriate encoding.
        /// 
        /// Note that the stream passed to OpenStreamReader must support both Peek and setting
        /// the position.
        /// </summary>
        /// <param name="stream">stream on which to open the stream reader</param>
        /// <returns>The detected encoding or null.</returns>
        public static Encoding DetectEncoding(Stream stream,
                                              List<Lazy<IEncodingDetector, IEncodingDetectorMetadata>> encodingDetectorExtensions,
                                              GuardedOperations guardedOperations)
        {
            if (stream == null)
                throw new ArgumentNullException("stream");

            long position = stream.Position;

            bool isStreamEmpty;

            Encoding detectedEncoding = CheckForBoM(stream, out isStreamEmpty);

            // If there was no BoM, try the detector extensions.
            if (detectedEncoding == null && !isStreamEmpty)
            {
                detectedEncoding = SniffForEncoding(stream, encodingDetectorExtensions, guardedOperations);

                //Rewind the stream.
                stream.Position = position;
            }

            return detectedEncoding;
        }

        internal static Encoding CheckForBoM(Stream stream, out bool isStreamEmpty)
        {
            long position = stream.Position;

            //Open a stream reader to check for byte order marks
            //(we can't use an encoding that has byte order marks as the encoding for deciding whether or not).
            using (StreamReader reader = new NonStreamClosingStreamReader(stream, Encoding.ASCII, detectEncodingFromByteOrderMarks: true))
            {
                //We need to peek in order to force the stream reader to actually
                //get the encoding.

                //Ah, except that there is a bug in the handling of the utf-32be encoding and peek.
                //(if you peek you get the byte order mark and a subsequent read will also get the byte order
                //mark). Read does not have this problem. If peek starts working reliably, we can go back to
                //using it and not have to recreate the StreamReader when we detect byte order marks.
                int peekedChar = reader.Read();

                isStreamEmpty = peekedChar == -1;

                //Rewind the stream.
                stream.Position = position;

                if (reader.CurrentEncoding == Encoding.ASCII)
                {
                    //No byte order marks were found.                    
                    return null;
                }
                else
                {
                    System.Diagnostics.Debug.Assert(reader.CurrentEncoding.GetPreamble().Length > 0);
                    return reader.CurrentEncoding;
                }
            }
        }

        /// <summary>
        /// Class to act as a stream reader, but that doesn't close the stream on dispose.
        /// </summary>
        internal class NonStreamClosingStreamReader : StreamReader
        {
            internal NonStreamClosingStreamReader(Stream stream, Encoding encoding, bool detectEncodingFromByteOrderMarks)
                : base(stream, encoding, detectEncodingFromByteOrderMarks)
            { }

            protected override void Dispose(bool disposing)
            {
                // Force the base to not dispose the stream this reader was created with since it doesn't own it.
                base.Dispose(false);
            }
        }

        private static Encoding SniffForEncoding(Stream stream, List<Lazy<IEncodingDetector, IEncodingDetectorMetadata>> orderedEncodingDetectors, GuardedOperations guardedOperations)
        {
            long position = stream.Position;

            foreach (Lazy<IEncodingDetector, IEncodingDetectorMetadata> sniffer in orderedEncodingDetectors)
            {
                Encoding encoding = null;
                try
                {
                    encoding = sniffer.Value.GetStreamEncoding(stream);
                }
                catch (Exception e)
                {
                    guardedOperations.HandleException(sniffer, e);
                }

                //Rewind the stream
                stream.Position = position;

                //Return if we smelled something.
                if (encoding != null)
                    return encoding;
            }

            return null;
        }

    }
}