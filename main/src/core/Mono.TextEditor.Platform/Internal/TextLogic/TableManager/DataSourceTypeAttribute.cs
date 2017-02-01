using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.TableManager
{
    /// <summary>
    /// Attribute on an ITableControlEventProcessorProvider to restrict the ITableControlEventProcessor it creates to events on entries created by <see cref="ITableDataSource"/>
    /// whose <see cref="ITableDataSource.SourceTypeIdentifier"/> matches this attribute.
    /// </summary>
    /// <remarks>
    /// <para>The ITableControlEventProcessorProvider can have multiple DataSourceType attributes.</para>
    /// <para>TODO move to internal\TextUIWpf\TableControl.</para>
    /// </remarks>
    [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1309:FieldNamesMustNotBeginWithUnderscore", Justification = "This is OK here.")]
    [SuppressMessage("Reliability", "RS0003", Justification = "Roslyn-specific rule")]
    public sealed class DataSourceTypeAttribute : MultipleBaseMetadataAttribute
    {
        private string _dataSourceTypes;

        public DataSourceTypeAttribute(string dataSourceTypes)
        {
            if (string.IsNullOrEmpty(dataSourceTypes))
            {
                throw new ArgumentNullException("dataSourceTypes");
            }

            _dataSourceTypes = dataSourceTypes;
        }

        [DefaultValue("")]
        public string DataSourceTypes
        {
            get
            {
                return _dataSourceTypes;
            }
        }
    }
}
