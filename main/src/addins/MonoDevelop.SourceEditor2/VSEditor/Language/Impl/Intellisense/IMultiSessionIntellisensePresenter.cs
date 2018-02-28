using System;
using System.Windows;
using Rect = Xwt.Rectangle;

namespace Microsoft.VisualStudio.Language.Intellisense.Implementation
{
    /// <summary>
    /// An internal interface defining the basic operations for an <see cref="IIntellisensePresenter"/> that can be associated with more
    /// than one <see cref="IIntellisenseSession"/>.
    /// </summary>
    internal interface IMultiSessionIntellisensePresenter<TSession> : IDisposable 
        where TSession : IIntellisenseSession
    {
        bool IsAttachedToSession { get; }

        void AttachToSession(TSession session);
        void DetachFromSession();

        Rect? AllowableScreenSize { get; }
    }
}
