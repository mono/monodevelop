using System;

namespace Microsoft.VisualStudio.TableManager
{
    /// <summary>
    /// A manager that provides stable snapshots of a collection of entries that can change over time.
    /// </summary>
    public interface ITableEntriesSnapshotFactory : IDisposable
    {
        /// <summary>
        /// Get the current snapshot of the entries associated with the factory.
        /// </summary>
        ITableEntriesSnapshot GetCurrentSnapshot();

        /// <summary>
        /// The version number associated with the current snapshot.
        /// </summary>
        int CurrentVersionNumber { get; }

        /// <summary>
        /// Get the snapshot associated with the specified <paramref name="versionNumber"/>. Return null if that snapshot
        /// is no longer available.
        /// </summary>
        ITableEntriesSnapshot GetSnapshot(int versionNumber);
    }

    /// <summary>
    /// Helper class for those that want to implement only part of the <see cref="ITableEntriesSnapshotFactory"/> interface.
    /// </summary>
    public class TableEntriesSnapshotFactoryBase : ITableEntriesSnapshotFactory
    {
        /// <summary>
        /// Get the current snapshot of the entries associated with the factory.
        /// </summary>
        public virtual ITableEntriesSnapshot GetCurrentSnapshot()
        {
            return TableEntriesSnapshotFactoryHelper.EmptySnapshot;
        }

        /// <summary>
        /// The version number associated with the current snapshot.
        /// </summary>
        /// <remarks><para>Returns -1, unless overridden, to match the version number of the returned snapshot.</para></remarks>
        public virtual int CurrentVersionNumber { get { return -1; } }

        /// <summary>
        /// Get the snapshot associated with the specified <paramref name="versionNumber"/>. Return null if that snapshot
        /// is no longer available.
        /// </summary>
        public virtual ITableEntriesSnapshot GetSnapshot(int versionNumber)
        {
            return (versionNumber == -1) ? TableEntriesSnapshotFactoryHelper.EmptySnapshot : null;
        }

        public virtual void Dispose()
        {
        }
    }

#if false   //TODO enable delete the else clause when we're able to coordinate a Roslyn new baseline.
    [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1402:FileMayOnlyContainASingleClass", Justification = "Reviewed.")]
    public static class TableEntriesSnapshotFactoryHelper
    {
        /// <summary>
        /// You can use this if a <see cref="ITableEntriesSnapshotFactory"/> needs to return an empty snapshot.
        /// </summary>
        public static readonly ITableEntriesSnapshot EmptySnapshot = new TableEntriesSnapshotBase();
    }
#else
    [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1402:FileMayOnlyContainASingleClass", Justification = "Reviewed.")]
    public static class TableEntriesSnapshotFactoryHelper
    {
        /// <summary>
        /// You can use this if a <see cref="ITableEntriesSnapshotFactory"/> needs to return an empty snapshot.
        /// </summary>
        public static readonly ITableEntriesSnapshot EmptySnapshot = new EmptySnapshotImplementation();

        private class EmptySnapshotImplementation : ITableEntriesSnapshot
        {
            public int Count
            {
                get
                {
                    return 0;
                }
            }

            public int VersionNumber
            {
                get
                {
                    return -1;
                }
            }

            public void StartCaching()
            {
            }

            public void StopCaching()
            {
            }

            public bool TryGetValue(int index, string columnName, out object content)
            {
                content = null;
                return false;
            }

            public object Identity(int index)
            {
                return null;
            }

            public object SnapshotIdentity
            {
                get
                {
                    return null;
                }
            }

            public int TranslateTo(int currentIndex, ITableEntriesSnapshot newerSnapshot)
            {
                return -1;
            }

            public void Dispose()
            {
            }
        }
    }
#endif
}
