// Copyright (c) Microsoft Corporation
// All rights reserved

namespace Microsoft.VisualStudio.Text.Classification.Implementation
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.ComponentModel.Composition;
    using Microsoft.VisualStudio.Utilities;
    using System.Collections;

    public interface IClassificationTypeDefinitionMetadata
    {
        string Name { get; }
        [System.ComponentModel.DefaultValue(null)]
        IEnumerable<string> BaseDefinition { get; }
    }

    [Export(typeof(IClassificationTypeRegistryService))]
    internal sealed class ClassificationTypeRegistryService : IClassificationTypeRegistryService
    {
        [ImportMany]
        internal List<Lazy<ClassificationTypeDefinition, IClassificationTypeDefinitionMetadata>> _classificationTypeDefinitions { get; set; }

        [Export]
        [Name("(TRANSIENT)")]
        public ClassificationTypeDefinition transientClassificationType;

        [Export]
        [Name("text")]
        public ClassificationTypeDefinition textClassificationType;

        #region Private Members
        private Dictionary<string, ClassificationTypeImpl> _classificationTypes;
        private Dictionary<string, ClassificationTypeImpl> _transientClassificationTypes;

        #endregion // Private Members

        #region Public Members
        public IClassificationType GetClassificationType(string type)
        {
            ClassificationTypeImpl classificationType = null;

            this.ClassificationTypes.TryGetValue(type, out classificationType);

            return classificationType;
        }

        /// <summary>
        /// Create a new classification type and add it to the registry.
        /// </summary>
        public IClassificationType CreateClassificationType(string type, IEnumerable<IClassificationType> baseTypes)
        {
            if (type == null)
            {
                throw new ArgumentNullException("type");
            }

            if (baseTypes == null)
            {
                throw new ArgumentNullException("baseTypes");
            }
            if (ClassificationTypes.ContainsKey(type))
            {
                throw new InvalidOperationException(LookUp.Strings.ClassificationAlreadyAdded);
            }

            // Use the non-canonical name for the actual type
            ClassificationTypeImpl classificationType = new ClassificationTypeImpl(type);
            foreach (var baseType in baseTypes)
            {
                classificationType.AddBaseType(baseType);
            }

            ClassificationTypes.Add(type, classificationType);

            return classificationType;
        }

        /// <summary>
        /// Create a transient classification type that can be used to represent
        /// classification types generated at runtime.
        /// </summary>
        /// <param name="baseTypes">The base types associated with this transient type.</param>
        /// <returns>The new transient type.</returns>
        public IClassificationType CreateTransientClassificationType(IEnumerable<IClassificationType> baseTypes)
        {
            // Validate
            if (baseTypes == null)
            {
                throw new ArgumentNullException("baseTypes");
            }
            if (!baseTypes.GetEnumerator().MoveNext())
            {
                throw new InvalidOperationException(LookUp.Strings.TransientTypesNeedAtLeastOneBaseType);
            }

            return BuildTransientClassificationType(baseTypes);
        }

        /// <summary>
        /// Create a transient classification type that can be used to represent
        /// classification types generated at runtime.
        /// </summary>
        /// <param name="baseTypes">The base types associated with this transient type.</param>
        /// <returns>The new transient type.</returns>
        public IClassificationType CreateTransientClassificationType(params IClassificationType[] baseTypes)
        {
            // Validate
            if (baseTypes == null)
            {
                throw new ArgumentNullException("baseTypes");
            }
            if (baseTypes.Length == 0)
            {
                throw new InvalidOperationException(LookUp.Strings.TransientTypesNeedAtLeastOneBaseType);
            }

            return BuildTransientClassificationType(baseTypes);
        }
        #endregion // Public Members

        #region Private Methods

        /// <summary>
        /// The transient type contributed by this assembly.
        /// </summary>
        private IClassificationType TransientClassificationType
        {
            get
            {
                return ClassificationTypes["(TRANSIENT)"];
            }
        }

        /// <summary>
        /// The map of classification type names to actual IClassificationTypes.
        /// 
        /// Used to lazily init the map.
        /// </summary>
        private Dictionary<string, ClassificationTypeImpl> ClassificationTypes
        {
            get
            {
                if (_classificationTypes == null)
                {
                    _classificationTypes = new Dictionary<string, ClassificationTypeImpl>(StringComparer.InvariantCultureIgnoreCase);
                    BuildClassificationTypes(_classificationTypes);
                }
                return _classificationTypes;
            }
        }

        /// <summary>
        /// Consumes all of the IClassificationTypeProvisions in the system to build the 
        /// list of classification types in the system.
        /// </summary>
        private void BuildClassificationTypes(Dictionary<string,ClassificationTypeImpl> classificationTypes)
        {
            // For each content baseType provision, create an IClassificationType.
            foreach (Lazy<ClassificationTypeDefinition, IClassificationTypeDefinitionMetadata> classificationTypeDefinition in _classificationTypeDefinitions)
            {
                string classificationName = classificationTypeDefinition.Metadata.Name;

                ClassificationTypeImpl type = null;

                if (!classificationTypes.TryGetValue(classificationName, out type))
                {
                    type = new ClassificationTypeImpl(classificationName);
                    classificationTypes.Add(classificationName, type);
                }

                IEnumerable<string> baseTypes = classificationTypeDefinition.Metadata.BaseDefinition;
                if (baseTypes != null)
                {
                    ClassificationTypeImpl baseType = null;

                    foreach (string baseClassificationType in baseTypes)
                    {
                        if (!classificationTypes.TryGetValue(baseClassificationType, out baseType))
                        {
                            baseType = new ClassificationTypeImpl(baseClassificationType);
                            classificationTypes.Add(baseClassificationType, baseType);
                        }

                        type.AddBaseType(baseType);
                    }
                }
            }
        }

        /// <summary>
        /// Builds a new transient classification type based on a set of actual base
        /// types.
        /// 
        /// With multiple projection buffers, it is possible to have a transient classification
        /// type with transient types as parents.
        /// </summary>
        /// <param name="baseTypes"></param>
        /// <returns></returns>
        private IClassificationType BuildTransientClassificationType(IEnumerable<IClassificationType> baseTypes)
        {
            // Lazily init
            if (_transientClassificationTypes == null)
            {
                _transientClassificationTypes = new Dictionary<string, ClassificationTypeImpl>(StringComparer.InvariantCultureIgnoreCase);
            }

            List<IClassificationType> sortedBaseTypes = new List<IClassificationType>(baseTypes);
            sortedBaseTypes.Sort(delegate(IClassificationType a, IClassificationType b)
                                 { return string.CompareOrdinal(a.Classification, b.Classification); });

            // Build the transient name
            StringBuilder sb = new StringBuilder();
            foreach (IClassificationType type in sortedBaseTypes)
            {
                sb.Append(type.Classification);
                sb.Append(" - ");
            }

            // Append "(transient)" onto the name.
            sb.Append(this.TransientClassificationType.Classification);

            // Look for a cached type
            ClassificationTypeImpl transientType;
            if (!_transientClassificationTypes.TryGetValue(sb.ToString(), out transientType))
            {
                // Didn't find a cached type, so create a new one
                transientType = new ClassificationTypeImpl(sb.ToString());

                foreach (IClassificationType type in sortedBaseTypes)
                {
                    transientType.AddBaseType(type);
                }

                // Add in the transient type as a base type
                transientType.AddBaseType(TransientClassificationType);

                // Cache this type so it doesn't need to be created again.
                _transientClassificationTypes[transientType.Classification] = transientType;
            }

            return transientType;
        }
        #endregion // Private Methods
    }
}
