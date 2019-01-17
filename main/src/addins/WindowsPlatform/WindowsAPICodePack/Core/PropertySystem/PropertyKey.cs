//Copyright (c) Microsoft Corporation.  All rights reserved.

using System;
using System.Runtime.InteropServices;
using Microsoft.WindowsAPICodePack.Resources;

namespace Microsoft.WindowsAPICodePack.Shell.PropertySystem
{
    /// <summary>
    /// Defines a unique key for a Shell Property
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct PropertyKey : IEquatable<PropertyKey>
    {
        #region Private Fields

        private Guid formatId;
        private Int32 propertyId;

        #endregion

        #region Public Properties
        /// <summary>
        /// A unique GUID for the property
        /// </summary>
        public Guid FormatId
        {
            get
            {
                return formatId;
            }
        }

        /// <summary>
        ///  Property identifier (PID)
        /// </summary>
        public Int32 PropertyId
        {
            get
            {
                return propertyId;
            }
        }

        #endregion

        #region Public Construction

        /// <summary>
        /// PropertyKey Constructor
        /// </summary>
        /// <param name="formatId">A unique GUID for the property</param>
        /// <param name="propertyId">Property identifier (PID)</param>
        public PropertyKey(Guid formatId, Int32 propertyId)
        {
            this.formatId = formatId;
            this.propertyId = propertyId;
        }

        /// <summary>
        /// PropertyKey Constructor
        /// </summary>
        /// <param name="formatId">A string represenstion of a GUID for the property</param>
        /// <param name="propertyId">Property identifier (PID)</param>
        public PropertyKey(string formatId, Int32 propertyId)
        {
            this.formatId = new Guid(formatId);
            this.propertyId = propertyId;
        }

        #endregion

        #region IEquatable<PropertyKey> Members

        /// <summary>
        /// Returns whether this object is equal to another. This is vital for performance of value types.
        /// </summary>
        /// <param name="other">The object to compare against.</param>
        /// <returns>Equality result.</returns>
        public bool Equals(PropertyKey other)
        {
            return other.Equals((object)this);
        }

        #endregion

        #region equality and hashing

        /// <summary>
        /// Returns the hash code of the object. This is vital for performance of value types.
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return formatId.GetHashCode() ^ propertyId;
        }

        /// <summary>
        /// Returns whether this object is equal to another. This is vital for performance of value types.
        /// </summary>
        /// <param name="obj">The object to compare against.</param>
        /// <returns>Equality result.</returns>
        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;

            if (!(obj is PropertyKey))
                return false;

            PropertyKey other = (PropertyKey)obj;
            return other.formatId.Equals(formatId) && (other.propertyId == propertyId);
        }

        /// <summary>
        /// Implements the == (equality) operator.
        /// </summary>
        /// <param name="propKey1">First property key to compare.</param>
        /// <param name="propKey2">Second property key to compare.</param>
        /// <returns>true if object a equals object b. false otherwise.</returns>        
        public static bool operator ==(PropertyKey propKey1, PropertyKey propKey2)
        {
            return propKey1.Equals(propKey2);
        }

        /// <summary>
        /// Implements the != (inequality) operator.
        /// </summary>
        /// <param name="propKey1">First property key to compare</param>
        /// <param name="propKey2">Second property key to compare.</param>
        /// <returns>true if object a does not equal object b. false otherwise.</returns>
        public static bool operator !=(PropertyKey propKey1, PropertyKey propKey2)
        {
            return !propKey1.Equals(propKey2);
        }

        /// <summary>
        /// Override ToString() to provide a user friendly string representation
        /// </summary>
        /// <returns>String representing the property key</returns>        
        public override string ToString()
        {
            return string.Format(System.Globalization.CultureInfo.InvariantCulture,
                LocalizedMessages.PropertyKeyFormatString,
                formatId.ToString("B"), propertyId);
        }

        #endregion
    }
}
