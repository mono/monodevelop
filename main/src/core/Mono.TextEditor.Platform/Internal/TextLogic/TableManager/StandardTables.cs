using System;

namespace Microsoft.VisualStudio.TableManager
{
    /// <summary>
    /// Standard <see cref="ITableManager.Identifier"/> used by the Error and Task Lists.
    /// </summary>
    public class StandardTables
    {
        /// <summary>
        /// The string equivalent of the <see cref="ITableManager.Identifier"/> for the <see cref="ITableManager"/> providing errors for the Error List.
        /// </summary>
        public const string ErrorsTableString = "{EFC0279A-C409-470F-B801-C8AACEBA13D1}";

        /// <summary>
        /// <see cref="ITableManager.Identifier"/> for the <see cref="ITableManager"/> providing errors for the Error List.
        /// </summary>
        public static readonly Guid ErrorsTable = new Guid(ErrorsTableString);

        /// <summary>
        /// The string equivalent of the <see cref="ITableManager.Identifier"/> for the <see cref="ITableManager"/> providing tasks for the Task List.
        /// </summary>
        public const string TasksTableString = "{81E75593-4D5C-44A8-B67C-500DB17DF424}";

        /// <summary>
        /// <see cref="ITableManager.Identifier"/> for the <see cref="ITableManager"/> providing tasks for the Task List.
        /// </summary>
        public static readonly Guid TasksTable = new Guid(TasksTableString);

        // TODO remove this block and replace it with comments in the various StandardTableColumnDefinitions saying what data is used to populate each column.
        // ITableDataSources providing entries for the ErrorTableDataSource should make sure the entries they define return values for the following keys:
        //
        // StandardTableKeyNames.Line
        //  Line number (0-based) of the start of the error (integer)
        //
        // StandardTableKeyNames.Column
        //  Column number (0-based) of the start of the error (integer)
        //
        // StandardTableKeyNames.DocumentName
        //  Absolute path name of the document containing the error (string)
        //
        // StandardTableKeyNames.Text
        //  Description of the error (string)
        //
        // ShimTableColumnDefinitions.ErrorCategory
        //  Category of the error (Microsoft.VisualStudio.Shell.Interop.VSTASKCATEGORY)
        //
        // ShimTableColumnDefinitions.Priority
        //  Priority of the error (Microsoft.VisualStudio.Shell.Interop.VSTASKPRIORITY)
        //
        // ShimTableColumnDefinitions.ProjectName
        //  Name of the project containing the document with the error (string)
        //
        // ShimTableColumnDefinitions.Project
        //  Pointer to the project containing the document with the error (IVsHierarchy)
        //
        // ShimTableColumnDefinitions.ImageIndex (not used at the moment and will probably be replaced with an image moniker in the near future.)
        //  Image index of the for the error (int)
        //
        // Do not specify the ShimTableColumnDefinitions.ProviderGuid key when creating errors using the managed APIs. This key is used
        // (by the shims) to handle the cases where tasks were directed to the error list.
    }
}