using System;
using Microsoft.VisualStudio.TableControl;

namespace Microsoft.VisualStudio.TableManager
{
    /// <summary>
    /// Standard <see cref="ITableDataSource.SourceTypeIdentifier"/> used by the Error and TaskLists.
    /// </summary>
    public static class StandardTableDataSources
    {
        /// <summary>
        /// The string equivalent of the <see cref="ITableDataSource.SourceTypeIdentifier"/> for sources providing errors to the error list.
        /// </summary>
        /// <remarks>
        /// <para>Corresponds to GUID_Error_TaskProvider</para>
        /// </remarks>
        public const string ErrorTableDataSourceString = "{18267819-C975-4292-8741-255590F76EB5}";

        /// <summary>
        /// <see cref="ITableDataSource.SourceTypeIdentifier"/> for sources providing errors to the error list.
        /// </summary>
        /// <remarks>
        /// <para>Corresponds to GUID_Error_TaskProvider</para>
        /// </remarks>
        public static readonly Guid ErrorTableDataSource = new Guid(ErrorTableDataSourceString);

        /// <summary>
        /// <see cref="ITableDataSource.SourceTypeIdentifier"/> for sources providing comment tasks to the task list.
        /// </summary>
        /// <remarks>
        /// <para>Corresponds to GUID_Comment_TaskProvider</para>
        /// </remarks>
        public const string CommentTableDataSourceString = "{5A2D2729-ADFF-4a2e-A44F-55EBBF5DF64B}";

        /// <summary>
        /// The string equivalent of the <see cref="ITableDataSource.SourceTypeIdentifier"/> for sources providing comment tasks to the task list.
        /// </summary>
        /// <remarks>
        /// <para>Corresponds to GUID_Comment_TaskProvider</para>
        /// </remarks>
        public static readonly Guid CommentTableDataSource = new Guid(CommentTableDataSourceString);

        /// <summary>
        /// The string equivalent of the <see cref="ITableDataSource.SourceTypeIdentifier"/> for sources providing shotcut tasks to the task list.
        /// </summary>
        /// <remarks>
        /// <para>Corresponds to GUID_Shortcut_TaskProvider</para>
        /// </remarks>
        public const string ShortcutTableDataSourceString = "{8B277264-F4D9-4af1-9FC1-27FB974870D5}";

        /// <summary>
        /// <see cref="ITableDataSource.SourceTypeIdentifier"/> for sources providing shotcut tasks to the task list.
        /// </summary>
        /// <remarks>
        /// <para>Corresponds to GUID_Shortcut_TaskProvider</para>
        /// </remarks>
        public static readonly Guid ShortcutTableDataSource = new Guid(ShortcutTableDataSourceString);

        /// <summary>
        /// The string equivalent of the <see cref="ITableDataSource.SourceTypeIdentifier"/> for sources providing user tasks to the task list.
        /// </summary>
        /// <remarks>
        /// <para>Corresponds to GUID_User_TaskProvider</para>
        /// </remarks>
        public const string UserTableDataSourceString = "{1593EE17-2E95-41f0-8DC6-A0BFE991388F}";

        /// <summary>
        /// <see cref="ITableDataSource.SourceTypeIdentifier"/> for sources providing user tasks to the task list.
        /// </summary>
        /// <remarks>
        /// <para>Corresponds to GUID_User_TaskProvider</para>
        /// </remarks>
        public static readonly Guid UserTableDataSource = new Guid(UserTableDataSourceString);

        /// <summary>
        /// Represents a string identifier of an "any data source". Used by <see cref="ITableControlEventProcessorProvider"/> to identify
        /// event processors that are not limited to any particular data source.
        /// </summary>
        public const string AnyDataSourceString = "{CB34259B-760A-48A8-B5AD-BFCBCB83B887}";

        /// <summary>
        /// Represents an identifier of an "any data source". Used by <see cref="ITableControlEventProcessorProvider"/> to identify
        /// event processors that are not limited to any particular data source.
        /// </summary>
        public static readonly Guid AnyDataSource = new Guid(AnyDataSourceString);
    }
}
