// Copyright (c) Microsoft Corporation
// All rights reserved

using System;
using System.Collections.ObjectModel;

namespace Microsoft.VisualStudio.Language.Intellisense
{
    /// <summary>
    /// Defines a Peek session, which is a type of IntelliSense session.
    /// </summary>
    /// <remarks><see cref="IPeekSession"/> instances are created and managed by the <see cref="IPeekBroker"/> service.</remarks>
    public interface IPeekSession : IIntellisenseSession
    {
        /// <summary>
        /// Gets the collection of <see cref="IPeekableItem"/> objects.
        /// </summary>
        ReadOnlyObservableCollection<IPeekableItem> PeekableItems { get; }

        /// <summary>
        /// Case insensitive name of the relationship to which this session pertains.
        /// </summary>
        string RelationshipName { get; }

        /// <summary>
        /// Starts asynchronous query for <see cref="IPeekResult"/>s for the given relationship on the given <see cref="IPeekableItem"/>.
        /// </summary>
        /// <param name="peekableItem">A <see cref="IPeekableItem"/> to be queried for results.</param>
        /// <param name="relationshipName">The case insenitive name of the relationship.</param>
        /// <returns>The <see cref="IPeekResultQuery"/> instance representing the results and state of the query.</returns>
        IPeekResultQuery QueryPeekResults(IPeekableItem peekableItem, string relationshipName);

        /// <summary>
        /// Starts the nested Peek session, queries <see cref="IPeekableItemSource"/>s
        /// for <see cref="IPeekableItem"/>s and raises <see cref="NestedPeekTriggered"/> event on success.
        /// </summary>
        /// <param name="nestedSession">The nested Peek session.</param>
        void TriggerNestedPeekSession(IPeekSession nestedSession);

        /// <summary>
        /// Occurs when nested Peek command is triggered. In a typical case this event occurs
        /// when Peek command is invoked on a text view that represents one of Peek results
        /// pertaining to this Peek session.
        /// </summary>
        event EventHandler<NestedPeekTriggeredEventArgs> NestedPeekTriggered;

        /// <summary>
        /// Gets the <see cref="PeekSessionCreationOptions"/> that was used to create this Peek session.
        /// </summary>
        PeekSessionCreationOptions CreationOptions { get; }
    }
}