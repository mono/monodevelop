using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.TableManager
{
    // TODO move to internal\TextUIWpf\TableControl.

    /// <summary>
    /// Attribute on an ITableControlEventProcessorProvider to restrict the ITableControlEventProcessor it creates to events on entries created by <see cref="ITableDataSource"/>
    /// whose <see cref="ITableDataSource.Identifier"/> matches this attribute.
    /// </summary>
    /// <remarks>
    /// <para>The ITableControlEventProcessorProvider can have multiple DataSource attributes.</para>
    /// </remarks>
    [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1309:FieldNamesMustNotBeginWithUnderscore", Justification = "This is OK here.")]
    [SuppressMessage("Reliability", "RS0003", Justification = "Roslyn-specific rule")]
    public sealed class DataSourceAttribute : MultipleBaseMetadataAttribute
    {
        private readonly string _dataSources;

        public DataSourceAttribute(string dataSources)
        {
            if (string.IsNullOrEmpty(dataSources))
            {
                throw new ArgumentNullException("dataSources");
            }

            _dataSources = dataSources;
        }

        /// <summary>
        /// Return the DataSource identifier associated with this attribute.
        /// </summary>
        public string DataSources
        {
            get
            {
                return _dataSources;
            }
        }
    }
}
