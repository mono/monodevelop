// Copyright (c) Microsoft Corporation
// All rights reserved

namespace Microsoft.VisualStudio.Text.Editor.DragDrop
{

    using System;
    using System.ComponentModel.Composition;

    /// <summary>
    /// Specifies the format that an <see cref="IDropHandler"/> handles.
    /// </summary>
    /// <remarks>
    /// You can specify multiple instances of this attribute in order to handle multiple <see cref="System.Windows.DataFormats"/>.
    /// This attribute should be used on an export of <see cref="IDropHandlerProvider"/>.</remarks>
    [MetadataAttribute]
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public sealed class DropFormatAttribute : Attribute
    {
        /// <summary>
        /// Stores the format specified in this <see cref="DropFormatAttribute"/>
        /// </summary>
        private string _dropFormat;

        /// <summary>
        /// Gets the drop format.
        /// </summary>
        public string DropFormats
        {
            // don't change the naming of this property; MEF depends on it.
            get { return _dropFormat; }
        }

        /// <summary>
        /// Initializes a new instance of <see cref="DropFormatAttribute"/> with the specified drop format.
        /// </summary>
        /// <param name="dropFormat">The drop format.</param>
        public DropFormatAttribute(string dropFormat)
        {
            _dropFormat = dropFormat;
        }
    }

}
