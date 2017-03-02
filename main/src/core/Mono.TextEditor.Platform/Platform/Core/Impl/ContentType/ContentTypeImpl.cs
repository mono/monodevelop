//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
// This file contain implementations details that are subject to change without notice.
// Use at your own risk.
//
using System;
using System.Collections.Generic;

namespace Microsoft.VisualStudio.Utilities.Implementation
{
    internal partial class ContentTypeImpl : IContentType
    {
        readonly private string name;
        private List<IContentType> baseTypeList = new List<IContentType>(1);    // the typical size

        internal ContentTypeImpl(string name)
        {
            this.name = name;
        }

        private List<IContentType> BaseTypeList
        {
            get { return this.baseTypeList; }
        }

        internal void AddBaseType(ContentTypeImpl baseType)
        {
            // TODO: should be part of ctor; this class should be invariant
            if (!this.BaseTypeList.Contains(baseType))
            {
                this.BaseTypeList.Add(baseType);
            }
        }

        public string TypeName
        {
            get { return this.name; }
        }

        public string DisplayName
        {
            get { return this.name; }
        }

        public bool IsOfType(string type)
        {
            if (String.Compare(type, name, StringComparison.OrdinalIgnoreCase) == 0)
            {
                return true;
            }
            else
            {
                foreach (IContentType baseType in this.BaseTypeList)
                {
                    if (baseType.IsOfType(type))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public IEnumerable<IContentType> BaseTypes
        {
            // TODO: when base types are invariant, don't need AsReadOnly
            get { return BaseTypeList.AsReadOnly(); }
        }

        public override string ToString()
        {
            return this.name;
        }
    }
}
