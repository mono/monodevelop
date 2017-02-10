using System;
using System.Collections.Generic;
using System.IO;

namespace Microsoft.VisualStudio.Text.Implementation
{
    internal abstract class BaseTextStorageLoader : ITextStorageLoader
    {
        protected TextReader reader;
        protected string id;
        protected int fileSize;

        protected bool loadStarted = false;
        protected bool loadCompleted = false;
        protected bool hasConsistentLineEndings = true;
        protected int longestLineLength = 0;

        public BaseTextStorageLoader(TextReader reader, int fileSize, string id)
        {
            this.reader = reader;
            this.fileSize = fileSize;
            this.id = id;
        }

        public IEnumerable<ITextStorage> Load()
        {
            if (this.loadStarted)
            {
                throw new InvalidOperationException();
            }
            this.loadStarted = true;
            return DoLoad();
        }

        protected abstract IEnumerable<ITextStorage> DoLoad();

        public bool HasConsistentLineEndings
        {
            get
            {
                if (!this.loadCompleted)
                {
                    throw new InvalidOperationException();
                }
                return this.hasConsistentLineEndings;
            }
        }

        public int LongestLineLength
        {
            get
            {
                if (!this.loadCompleted)
                {
                    throw new InvalidOperationException();
                }
                return this.longestLineLength;
            }
        }
    }
}
