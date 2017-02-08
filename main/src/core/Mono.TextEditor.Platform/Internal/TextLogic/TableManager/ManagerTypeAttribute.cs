using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.TableManager
{
    /// <summary>
    /// Attribute on an ITableControlEventProcessorProvider to restrict the ITableControlEventProcessor it creates to events on entries provided through an <see cref="ITableManager"/>
    /// whose <see cref="ITableManager.Identifier"/> matches this attribute.
    /// </summary>
    /// <remarks>
    /// <para>The ITableControlEventProcessorProvider can have multiple ManagerType attributes.</para>
    /// <para>TODO move to internal\TextUIWpf\TableControl.</para>
    /// </remarks>
    [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1309:FieldNamesMustNotBeginWithUnderscore", Justification = "This is OK here.")]
    [SuppressMessage("Reliability", "RS0003", Justification = "Roslyn-specific rule")]
    public sealed class ManagerTypeAttribute : MultipleBaseMetadataAttribute
    {
        private string _managerTypes;

        public ManagerTypeAttribute(string managerTypes)
        {
            if (string.IsNullOrEmpty(managerTypes))
            {
                throw new ArgumentNullException("managerTypes");
            }

            _managerTypes = managerTypes;
        }

        public string ManagerIdentifiers
        {
            get
            {
                return _managerTypes;
            }
        }
    }
}
