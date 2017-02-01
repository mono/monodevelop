using System;
using System.Diagnostics;
using Microsoft.VisualStudio.Text.Utilities;

namespace Microsoft.VisualStudio.Text.Implementation
{
    internal class PageManager : Page
    {
        // this class inherits from page so that it participates in the MRU list, of which it is the sentinel node.

        int loadedPageCount;

        public PageManager()
        {
            // set up sentinel node of MRU list (this object).
            // The sentinel is considered the most recently used page, so sentinel.More is the least
            // recently used page and sentinel.Less is the most recently used real page.
			More = this;
			Less = this;
            VerifyMRU();
        }

        public void Add(Page page)
        {
            // This method is called only during construction, which must be single-threaded,
            // so no locking is required.
            Debug.Assert(page != this, "Trying to add sentinel to PageManager");

            // the sentinel node (ourself) is considered the most
            // recently used, so sentinel.Less is the most recently used
            // real page.

            this.loadedPageCount++;
            // insert as most recently used
            page.Less = this.Less;
            page.More = this;
            this.Less.More = page;
            this.Less = page;
            Trace("Add active page " + page.Id);
            VerifyMRU();
        }

		public void UnloadPageWhileLocked()
        {
            Debug.Assert(this.loadedPageCount > 0, "Zero page count?");
            VerifyMRU();
            if (this.loadedPageCount >= Math.Max(1, TextModelOptions.CompressedStorageMaxLoadedPages))
            {
                Page page = this.More;	// the least recently used page
                Debug.Assert(page != this, "Trying to remove sentinel from MRU list");

                // remove page from MRU list
                page.Less.More = page.More;
                page.More.Less = page.Less;
                page.Less = null;
                page.More = null;

                page.UnloadWhileLocked();
                Trace("Unloaded page " + page.Id);
                this.loadedPageCount--;
                VerifyMRU();
            }
        }

        public void MarkUsedWhileLocked(Page page)
        {
			if (this.Less != page)
            {
                if (page.Less != null)
                {
                    // page is already in MRU list; remove it from its current position
                    page.Less.More = page.More;
                    page.More.Less = page.Less;
                }
                else
                {
                    this.loadedPageCount++;
                }

                // insert as most recently used
                page.Less = this.Less;
                page.More = this;
                this.Less.More = page;
                this.Less = page;
                VerifyMRU();
            }
        }

        public override string Id 
        { 
            get { return "sentinel"; } 
        }

        public override bool UnloadWhileLocked()
        {
            // you can't unload the sentinel node.
            throw new InvalidOperationException("Text Storage LRU list is invalid");
        }

        [Conditional("PAGED_STORAGE_TRACING")]
        public static void Trace(string s)
        {
            Debug.WriteLine("@@@ " + s);
        }

        [Conditional("DEBUG")]
        private void VerifyMRU()
        {
            int length = 0;
            Page p = this.More;
            while (p != this)
            {
                length++;
                p = p.More;
            }
            Debug.Assert(length == this.loadedPageCount, string.Format("Page count ({0}) mismatch with MRU length ({1})", this.loadedPageCount, length));
        }
    }
}
