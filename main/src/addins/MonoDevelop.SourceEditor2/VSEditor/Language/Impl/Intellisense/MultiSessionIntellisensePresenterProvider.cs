using System;

using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.Language.Intellisense.Implementation
{
    /// <summary>
    /// An intellisense presenter provider that caches the presenters it provides in the session's text view's
    /// property bag.
    /// </summary>
    internal class MultiSessionIntellisensePresenterProvider<TSession, TPresenter>
        where TSession : IIntellisenseSession
        where TPresenter : IMultiSessionIntellisensePresenter<TSession>
    {
        /// <summary>
        /// Tries to obtain a presenter from the cache (session's view's property bag). If a valid presenter was found
        /// in the cache returns true, otherwise returns false. It always constructs a new presenter using the
        /// provided factory.
        /// </summary>
        /// <returns><c>true</c> if the instance was retrieved from a cache; <c>false</c> if a new instance was created.</returns>
        public bool GetOrCreatePresenter(TSession session, Func<TPresenter> factory, out TPresenter presenter)
        {
            bool addPresenterToCache = true;

            // check view's property bag for a cached version of the presenter
            if (session.TextView.Properties.TryGetProperty<TPresenter>(typeof(TPresenter), out presenter))
            {
                // if there is a cached presenter, make sure it's not already in a relationship with a session
                if (!presenter.IsAttachedToSession)
                {
                    // if there is a usable cached presenter, ensure that its screen size is compatible with the current screen size
                    // (ensure compatibility of screen resolution)
                    if (presenter.AllowableScreenSize == Helpers.GetScreenRect(session))
                    {
                        // we have a valid cached presenter, attach it to the session and return
                        presenter.AttachToSession(session);
                        return true;
                    }
                    else
                    {
                        // This one won't work.  Go ahead and remove it from the cache, assuming it won't ever work again.
                        this.RemovePresenterFromCache(session);
                    }
                }
                else
                {
                    // don't add the new presenter in the cache since the cache already contains a valid presenter 
                    // (that happens to be attached to another session now, but we still want to re-use it for future sessions)
                    addPresenterToCache = false;
                }
            }

            // we don't have a valid presenter or the cache is empty
            presenter = factory.Invoke();

            if (addPresenterToCache)
            {
                this.AddPresenterToCache(session, presenter);
            }

            // Attach our newly-created presenter to the session
            presenter.AttachToSession(session);

            // return false because we couldn't find a valid object from the cache
            return false;
        }

        protected void AddPresenterToCache(TSession session, TPresenter presenter)
        {
            session.TextView.Properties.AddProperty(typeof(TPresenter), presenter);
            session.TextView.Closed += this.OnTextViewClosed;
        }

        protected void RemovePresenterFromCache(TSession session)
        {
            session.TextView.Properties.RemoveProperty(typeof(TPresenter));
        }

        protected virtual void OnCacheContainerClosing(IPropertyOwner cacheContainer)
        {
            TPresenter presenter;

            if (cacheContainer.Properties.TryGetProperty<TPresenter>(typeof(TPresenter), out presenter))
            {
                presenter.Dispose();

                cacheContainer.Properties.RemoveProperty(typeof(TPresenter));
            }
        }

        private void OnTextViewClosed(object sender, EventArgs args)
        {
            ITextView textView = (ITextView)sender;

            this.OnCacheContainerClosing(textView);

            textView.Closed -= this.OnTextViewClosed;
        }
    }
}
