// Copyright (C) Microsoft Corporation.  All Rights Reserved.

using System.Collections.Generic;
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

        private FrugalList<IClassificationType> BaseTypesList
        {
            get
            {
                if (this.baseTypes == null)
                {
                    this.baseTypes = new FrugalList<IClassificationType>();
                }
                return this.baseTypes;
            }
        }

        internal void AddBaseType(IClassificationType baseType)
        {
            this.BaseTypesList.Add(baseType);
        }

        public string Classification
        {
            get { return this.name; }
        }

        public bool IsOfType(string type)
        {
            if (this.name == type)
                return true;
            else
            {
                foreach (IClassificationType baseType in this.BaseTypesList)
                {
                    if ( baseType.IsOfType(type) )
                        return true;
                }
            }

            return false;
        }

        public IEnumerable<IClassificationType> BaseTypes
        {
            get { return BaseTypesList.AsReadOnly(); }
        }

        public override string ToString()
        {
            return this.name;
        }
    }
}
