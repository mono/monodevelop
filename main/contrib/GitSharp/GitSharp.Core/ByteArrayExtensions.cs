using System;

namespace GitSharp.Core
{
    public static class ByteArrayExtensions
    {
        public class ParsedLine
        {
            public int NextIndex { get; private set;}
            public byte[] Buffer { get; private set; }

            public ParsedLine(int nextIndex, byte[] buffer)
            {
                NextIndex = nextIndex;
                Buffer = buffer;
            }
        }

        public static bool StartsWith(this byte[] buffer, byte[] bufferToCompareWith)
        {
			if (buffer == null)
            {
                throw new ArgumentNullException("buffer");
            }
            if (bufferToCompareWith == null)
            {
                throw new ArgumentNullException("bufferToCompareWith");
            }

            if (buffer.Length < bufferToCompareWith.Length)
            {
                return false;
            }

            int curpos = -1;

            while (++curpos < bufferToCompareWith.Length)
            {
                if (bufferToCompareWith[curpos] != buffer[curpos])
                {
                    return false;
                }
            }

            return true;
        }

        public static ParsedLine ReadLine(this byte[] source, int startIndex)
        {
			if (source == null)
            {
                throw new ArgumentNullException("source");
            }
            if (startIndex < 0)
            {
                throw new ArgumentOutOfRangeException("startIndex", "Parameter is expected gretaer or equal than zero.");
            }

            if (startIndex >= source.Length)
            {
                return new ParsedLine(-1, null);
            }

            int currentIndex = startIndex - 1;
            int indexModifier = 0;

            while (indexModifier == 0 && ++currentIndex < source.Length)
            {
                int num = source[currentIndex];
                switch (num)
                {
                    case 13:
                        if ((currentIndex != (source.Length - 1)) && (source[currentIndex + 1] == 10))
                        {
                            indexModifier = 2;
                        }
                        break;

                    case 10:
                        indexModifier = 1;
                        break;
                }
            }

            var output = new byte[currentIndex - startIndex];
            Array.Copy(source, startIndex, output, 0, currentIndex - startIndex);

            return new ParsedLine (currentIndex + indexModifier, output);
        }
    }
}