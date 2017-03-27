//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
// This file contain implementations details that are subject to change without notice.
// Use at your own risk.
//
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.Text.Utilities;

namespace Microsoft.VisualStudio.Text.Classification.Implementation
{
    internal class ClassificationTypeImpl : IClassificationType
    {
        private string name;
        private FrugalList<IClassificationType> baseTypes;

        internal ClassificationTypeImpl(string name)
        {
            this.name = name;
        }

        internal void AddBaseType(IClassificationType baseType)
        {
            if (this.baseTypes == null)
            {
                this.baseTypes = new FrugalList<IClassificationType>();
            }

            this.baseTypes.Add(baseType);
        }

        public string Classification
        {
            get { return this.name; }
        }

        public bool IsOfType(string type)
        {
            if (this.name == type)
                return true;
            else if (this.baseTypes != null)
            {
                foreach (IClassificationType baseType in this.baseTypes)
                {
                    if ( baseType.IsOfType(type) )
                        return true;
                }
            }

            return false;
        }

        public IEnumerable<IClassificationType> BaseTypes
        {
            get { return (this.baseTypes != null) ? (IEnumerable<IClassificationType>)(this.baseTypes.AsReadOnly()) : Enumerable.Empty<IClassificationType>(); }
        }

        public override string ToString()
        {
            return this.name;
        }
    }
}
