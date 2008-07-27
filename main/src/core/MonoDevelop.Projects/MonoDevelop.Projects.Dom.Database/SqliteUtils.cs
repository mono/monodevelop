//
// SqliteUtils.cs
//
// Author:
//   Scott Peterson  <lunchtimemama@gmail.com>
//
// Copyright (C) 2007 Novell, Inc.
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Reflection;
using System.Text;

namespace Hyena.Data.Sqlite
{
    internal static class SqliteUtils
    {
        public static string GetType (Type type)
        {
            if (type == typeof (string)) {
                return "TEXT";
            } else if (type == typeof (int) || type == typeof (long) || type == typeof (bool)
                || type == typeof (DateTime) || type == typeof (TimeSpan) || type.IsEnum) {
                return "INTEGER";
            } else {
                throw new Exception (String.Format (
                    "The type {0} cannot be bound to a database column.", type.Name));
            }
        }
        
        public static object ToDbFormat<T> (T value)
        {
        	return ToDbFormat (typeof(T), value);
        }
        
        public static object ToDbFormat (Type type, object value)
        {
            if (type == typeof (DateTime)) {
                return DateTime.MinValue.Equals ((DateTime)value)
                    ? (object)null
                    : DateTimeUtil.FromDateTime ((DateTime)value);
            } else if (type == typeof (TimeSpan)) {
                return TimeSpan.MinValue.Equals ((TimeSpan)value)
                    ? (object)null
                    : ((TimeSpan)value).TotalMilliseconds;
            } else if (type.IsEnum) {
                return Convert.ChangeType (value, Enum.GetUnderlyingType (type));
            } else if (type == typeof (bool)) {
                return ((bool)value) ? 1 : 0;
            }
            
            return value;
        }
        
        public static T FromDbFormat<T> (object value)
        {
        	return (T)FromDbFormat (typeof (T), value);
        }
        
        public static object FromDbFormat (Type type, object value)
        {
            if (type == typeof (DateTime)) {
                return value == null
                    ? DateTime.MinValue
                    : DateTimeUtil.ToDateTime (Convert.ToInt64 (value));
            } else if (type == typeof (TimeSpan)) {
                return value == null
                    ? TimeSpan.MinValue
                    : TimeSpan.FromMilliseconds (Convert.ToInt64 (value));
            } else if (value == null) {
                if (type.IsValueType) {
                    return Activator.CreateInstance (type);
                } else {
                    return null;
                }
            } else if (type.IsEnum) {
                return Enum.ToObject (type, value);
            } else if (type == typeof (bool)) {
                return ((int)value == 1);
            } else {
                return Convert.ChangeType (value, type);
            }
        }
        
        public static string BuildColumnSchema (string type, string name, string default_value,
            DatabaseColumnConstraints constraints)
        {
            StringBuilder builder = new StringBuilder ();
            builder.Append (name);
            builder.Append (' ');
            builder.Append (type);
            if ((constraints & DatabaseColumnConstraints.NotNull) > 0) {
                builder.Append (" NOT NULL");
            }
            if ((constraints & DatabaseColumnConstraints.Unique) > 0) {
                builder.Append (" UNIQUE");
            }
            if ((constraints & DatabaseColumnConstraints.PrimaryKey) > 0) {
                builder.Append (" PRIMARY KEY");
            }
            if (default_value != null) {
                builder.Append (" DEFAULT ");
                builder.Append (default_value);
            }
            return builder.ToString ();
        }
    }
}