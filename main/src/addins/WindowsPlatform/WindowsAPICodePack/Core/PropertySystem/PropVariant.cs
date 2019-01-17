//Copyright (c) Microsoft Corporation.  All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.WindowsAPICodePack.Resources;
using Microsoft.WindowsAPICodePack.Shell.PropertySystem;

namespace MS.WindowsAPICodePack.Internal
{
    /// <summary>
    /// Represents the OLE struct PROPVARIANT.
    /// This class is intended for internal use only.
    /// </summary>
    /// <remarks>
    /// Originally sourced from http://blogs.msdn.com/adamroot/pages/interop-with-propvariants-in-net.aspx
    /// and modified to support additional types including vectors and ability to set values
    /// </remarks>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Portability", "CA1900:ValueTypeFieldsShouldBePortable", MessageId = "_ptr2")]
    [StructLayout(LayoutKind.Explicit)]
    public sealed class PropVariant : IDisposable
    {
        #region Vector Action Cache

        // A static dictionary of delegates to get data from array's contained within PropVariants
        private static Dictionary<Type, Action<PropVariant, Array, uint>> _vectorActions = null;
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
        private static Dictionary<Type, Action<PropVariant, Array, uint>> GenerateVectorActions()
        {
            Dictionary<Type, Action<PropVariant, Array, uint>> cache = new Dictionary<Type, Action<PropVariant, Array, uint>>();

            cache.Add(typeof(Int16), (pv, array, i) =>
            {
                short val;
                PropVariantNativeMethods.PropVariantGetInt16Elem(pv, i, out val);
                array.SetValue(val, i);
            });

            cache.Add(typeof(UInt16), (pv, array, i) =>
            {
                ushort val;
                PropVariantNativeMethods.PropVariantGetUInt16Elem(pv, i, out val);
                array.SetValue(val, i);
            });

            cache.Add(typeof(Int32), (pv, array, i) =>
            {
                int val;
                PropVariantNativeMethods.PropVariantGetInt32Elem(pv, i, out val);
                array.SetValue(val, i);
            });

            cache.Add(typeof(UInt32), (pv, array, i) =>
            {
                uint val;
                PropVariantNativeMethods.PropVariantGetUInt32Elem(pv, i, out val);
                array.SetValue(val, i);
            });

            cache.Add(typeof(Int64), (pv, array, i) =>
            {
                long val;
                PropVariantNativeMethods.PropVariantGetInt64Elem(pv, i, out val);
                array.SetValue(val, i);
            });

            cache.Add(typeof(UInt64), (pv, array, i) =>
            {
                ulong val;
                PropVariantNativeMethods.PropVariantGetUInt64Elem(pv, i, out val);
                array.SetValue(val, i);
            });

            cache.Add(typeof(DateTime), (pv, array, i) =>
            {
                System.Runtime.InteropServices.ComTypes.FILETIME val;
                PropVariantNativeMethods.PropVariantGetFileTimeElem(pv, i, out val);

                long fileTime = GetFileTimeAsLong(ref val);

                array.SetValue(DateTime.FromFileTime(fileTime), i);
            });

            cache.Add(typeof(Boolean), (pv, array, i) =>
            {
                bool val;
                PropVariantNativeMethods.PropVariantGetBooleanElem(pv, i, out val);
                array.SetValue(val, i);
            });

            cache.Add(typeof(Double), (pv, array, i) =>
            {
                double val;
                PropVariantNativeMethods.PropVariantGetDoubleElem(pv, i, out val);
                array.SetValue(val, i);
            });

            cache.Add(typeof(Single), (pv, array, i) => // float
            {                
                float[] val = new float[1];
                Marshal.Copy(pv._ptr2, val, (int)i, 1);
                array.SetValue(val[0], (int)i);
            });

            cache.Add(typeof(Decimal), (pv, array, i) =>
            {
                int[] val = new int[4];
                for (int a = 0; a < val.Length; a++)
                {
                    val[a] = Marshal.ReadInt32(pv._ptr2,
                        (int)i * sizeof(decimal) + a * sizeof(int)); //index * size + offset quarter
                }
                array.SetValue(new decimal(val), i);
            });

            cache.Add(typeof(String), (pv, array, i) =>
            {
                string val = string.Empty;
                PropVariantNativeMethods.PropVariantGetStringElem(pv, i, ref val);
                array.SetValue(val, i);
            });

            return cache;
        }
        #endregion

        #region Dynamic Construction / Factory (Expressions)

        /// <summary>
        /// Attempts to create a PropVariant by finding an appropriate constructor.
        /// </summary>
        /// <param name="value">Object from which PropVariant should be created.</param>
        public static PropVariant FromObject(object value)
        {
            if (value == null)
            {
                return new PropVariant();
            }
            else
            {
                var func = GetDynamicConstructor(value.GetType());
                return func(value);
            }
        }

        // A dictionary and lock to contain compiled expression trees for constructors
        private static Dictionary<Type, Func<object, PropVariant>> _cache = new Dictionary<Type, Func<object, PropVariant>>();
        private static object _padlock = new object();

        // Retrieves a cached constructor expression.
        // If no constructor has been cached, it attempts to find/add it.  If it cannot be found
        // an exception is thrown.
        // This method looks for a public constructor with the same parameter type as the object.
        private static Func<object, PropVariant> GetDynamicConstructor(Type type)
        {
            lock (_padlock)
            {
                // initial check, if action is found, return it
                Func<object, PropVariant> action;
                if (!_cache.TryGetValue(type, out action))
                {
                    // iterates through all constructors
                    ConstructorInfo constructor = typeof(PropVariant)
                        .GetConstructor(new Type[] { type });

                    if (constructor == null)
                    { // if the method was not found, throw.
                        throw new ArgumentException(LocalizedMessages.PropVariantTypeNotSupported);
                    }
                    else // if the method was found, create an expression to call it.
                    {
                        // create parameters to action                    
                        var arg = Expression.Parameter(typeof(object), "arg");

                        // create an expression to invoke the constructor with an argument cast to the correct type
                        var create = Expression.New(constructor, Expression.Convert(arg, type));

                        // compiles expression into an action delegate
                        action = Expression.Lambda<Func<object, PropVariant>>(create, arg).Compile();
                        _cache.Add(type, action);
                    }
                }
                return action;
            }
        }

        #endregion

        #region Fields

        [FieldOffset(0)]
        decimal _decimal;

        // This is actually a VarEnum value, but the VarEnum type
        // requires 4 bytes instead of the expected 2.
        [FieldOffset(0)]
        ushort _valueType;

        // Reserved Fields
        //[FieldOffset(2)]
        //ushort _wReserved1;
        //[FieldOffset(4)]
        //ushort _wReserved2;
        //[FieldOffset(6)]
        //ushort _wReserved3;

        // In order to allow x64 compat, we need to allow for
        // expansion of the IntPtr. However, the BLOB struct
        // uses a 4-byte int, followed by an IntPtr, so
        // although the valueData field catches most pointer values,
        // we need an additional 4-bytes to get the BLOB
        // pointer. The valueDataExt field provides this, as well as
        // the last 4-bytes of an 8-byte value on 32-bit
        // architectures.
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2006:UseSafeHandleToEncapsulateNativeResources")]
        [FieldOffset(12)]
        IntPtr _ptr2;
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2006:UseSafeHandleToEncapsulateNativeResources")]
        [FieldOffset(8)]
        IntPtr _ptr;
        [FieldOffset(8)]
        Int32 _int32;
        [FieldOffset(8)]
        UInt32 _uint32;
        [FieldOffset(8)]
        byte _byte;
        [FieldOffset(8)]
        sbyte _sbyte;
        [FieldOffset(8)]
        short _short;
        [FieldOffset(8)]
        ushort _ushort;
        [FieldOffset(8)]
        long _long;
        [FieldOffset(8)]
        ulong _ulong;
        [FieldOffset(8)]
        double _double;
        [FieldOffset(8)]
        float _float;

        #endregion // struct fields

        #region Constructors

        /// <summary>
        /// Default constrcutor
        /// </summary>
        public PropVariant()
        {
            // left empty
        }

        /// <summary>
        /// Set a string value
        /// </summary>
        public PropVariant(string value)
        {
            if (value == null)
            {
                throw new ArgumentException(LocalizedMessages.PropVariantNullString, "value");
            }

            _valueType = (ushort)VarEnum.VT_LPWSTR;
            _ptr = Marshal.StringToCoTaskMemUni(value);
        }

        /// <summary>
        /// Set a string vector
        /// </summary>
        public PropVariant(string[] value)
        {
            if (value == null) { throw new ArgumentNullException("value"); }

            PropVariantNativeMethods.InitPropVariantFromStringVector(value, (uint)value.Length, this);
        }

        /// <summary>
        /// Set a bool vector
        /// </summary>
        public PropVariant(bool[] value)
        {
            if (value == null) { throw new ArgumentNullException("value"); }

            PropVariantNativeMethods.InitPropVariantFromBooleanVector(value, (uint)value.Length, this);
        }

        /// <summary>
        /// Set a short vector
        /// </summary>
        public PropVariant(short[] value)
        {
            if (value == null) { throw new ArgumentNullException("value"); }

            PropVariantNativeMethods.InitPropVariantFromInt16Vector(value, (uint)value.Length, this);
        }

        /// <summary>
        /// Set a short vector
        /// </summary>
        public PropVariant(ushort[] value)
        {
            if (value == null) { throw new ArgumentNullException("value"); }

            PropVariantNativeMethods.InitPropVariantFromUInt16Vector(value, (uint)value.Length, this);

        }

        /// <summary>
        /// Set an int vector
        /// </summary>
        public PropVariant(int[] value)
        {
            if (value == null) { throw new ArgumentNullException("value"); }

            PropVariantNativeMethods.InitPropVariantFromInt32Vector(value, (uint)value.Length, this);
        }

        /// <summary>
        /// Set an uint vector
        /// </summary>
        public PropVariant(uint[] value)
        {
            if (value == null) { throw new ArgumentNullException("value"); }

            PropVariantNativeMethods.InitPropVariantFromUInt32Vector(value, (uint)value.Length, this);
        }

        /// <summary>
        /// Set a long vector
        /// </summary>
        public PropVariant(long[] value)
        {
            if (value == null) { throw new ArgumentNullException("value"); }

            PropVariantNativeMethods.InitPropVariantFromInt64Vector(value, (uint)value.Length, this);
        }

        /// <summary>
        /// Set a ulong vector
        /// </summary>
        public PropVariant(ulong[] value)
        {
            if (value == null) { throw new ArgumentNullException("value"); }

            PropVariantNativeMethods.InitPropVariantFromUInt64Vector(value, (uint)value.Length, this);
        }

        /// <summary>>
        /// Set a double vector
        /// </summary>
        public PropVariant(double[] value)
        {
            if (value == null) { throw new ArgumentNullException("value"); }

            PropVariantNativeMethods.InitPropVariantFromDoubleVector(value, (uint)value.Length, this);
        }


        /// <summary>
        /// Set a DateTime vector
        /// </summary>
        public PropVariant(DateTime[] value)
        {
            if (value == null) { throw new ArgumentNullException("value"); }
            System.Runtime.InteropServices.ComTypes.FILETIME[] fileTimeArr =
                new System.Runtime.InteropServices.ComTypes.FILETIME[value.Length];

            for (int i = 0; i < value.Length; i++)
            {
                fileTimeArr[i] = DateTimeToFileTime(value[i]);
            }

            PropVariantNativeMethods.InitPropVariantFromFileTimeVector(fileTimeArr, (uint)fileTimeArr.Length, this);
        }

        /// <summary>
        /// Set a bool value
        /// </summary>
        public PropVariant(bool value)
        {
            _valueType = (ushort)VarEnum.VT_BOOL;
            _int32 = (value == true) ? -1 : 0;
        }

        /// <summary>
        /// Set a DateTime value
        /// </summary>
        public PropVariant(DateTime value)
        {
            _valueType = (ushort)VarEnum.VT_FILETIME;

            System.Runtime.InteropServices.ComTypes.FILETIME ft = DateTimeToFileTime(value);
            PropVariantNativeMethods.InitPropVariantFromFileTime(ref ft, this);
        }


        /// <summary>
        /// Set a byte value
        /// </summary>
        public PropVariant(byte value)
        {
            _valueType = (ushort)VarEnum.VT_UI1;
            _byte = value;
        }

        /// <summary>
        /// Set a sbyte value
        /// </summary>
        public PropVariant(sbyte value)
        {
            _valueType = (ushort)VarEnum.VT_I1;
            _sbyte = value;
        }

        /// <summary>
        /// Set a short value
        /// </summary>
        public PropVariant(short value)
        {
            _valueType = (ushort)VarEnum.VT_I2;
            _short = value;
        }

        /// <summary>
        /// Set an unsigned short value
        /// </summary>
        public PropVariant(ushort value)
        {
            _valueType = (ushort)VarEnum.VT_UI2;
            _ushort = value;
        }

        /// <summary>
        /// Set an int value
        /// </summary>
        public PropVariant(int value)
        {
            _valueType = (ushort)VarEnum.VT_I4;
            _int32 = value;
        }

        /// <summary>
        /// Set an unsigned int value
        /// </summary>
        public PropVariant(uint value)
        {
            _valueType = (ushort)VarEnum.VT_UI4;
            _uint32 = value;
        }

        /// <summary>
        /// Set a decimal  value
        /// </summary>
        public PropVariant(decimal value)
        {
            _decimal = value;

            // It is critical that the value type be set after the decimal value, because they overlap.
            // If valuetype is written first, its value will be lost when _decimal is written.
            _valueType = (ushort)VarEnum.VT_DECIMAL;
        }

        /// <summary>
        /// Create a PropVariant with a contained decimal array.
        /// </summary>
        /// <param name="value">Decimal array to wrap.</param>
        public PropVariant(decimal[] value)
        {
            if (value == null) { throw new ArgumentNullException("value"); }

            _valueType = (ushort)(VarEnum.VT_DECIMAL | VarEnum.VT_VECTOR);
            _int32 = value.Length;

            // allocate required memory for array with 128bit elements
            _ptr2 = Marshal.AllocCoTaskMem(value.Length * sizeof(decimal));            
            for (int i = 0; i < value.Length; i++)
            {                
                int[] bits = decimal.GetBits(value[i]);
                Marshal.Copy(bits, 0, _ptr2, bits.Length);
            }
        }

        /// <summary>
        /// Create a PropVariant containing a float type.
        /// </summary>        
        public PropVariant(float value)
        {
            _valueType = (ushort)VarEnum.VT_R4;

            _float = value;
        }

        /// <summary>
        /// Creates a PropVariant containing a float[] array.
        /// </summary>        
        public PropVariant(float[] value)
        {
            if (value == null) { throw new ArgumentNullException("value"); }

            _valueType = (ushort)(VarEnum.VT_R4 | VarEnum.VT_VECTOR);
            _int32 = value.Length;
                        
            _ptr2 = Marshal.AllocCoTaskMem(value.Length * sizeof(float));

            Marshal.Copy(value, 0, _ptr2, value.Length);
        }

        /// <summary>
        /// Set a long
        /// </summary>
        public PropVariant(long value)
        {
            _long = value;
            _valueType = (ushort)VarEnum.VT_I8;
        }

        /// <summary>
        /// Set a ulong
        /// </summary>
        public PropVariant(ulong value)
        {
            _valueType = (ushort)VarEnum.VT_UI8;
            _ulong = value;
        }

        /// <summary>
        /// Set a double
        /// </summary>
        public PropVariant(double value)
        {
            _valueType = (ushort)VarEnum.VT_R8;
            _double = value;
        }

        #endregion

        #region Uncalled methods - These are currently not called, but I think may be valid in the future.

        /// <summary>
        /// Set an IUnknown value
        /// </summary>
        /// <param name="value">The new value to set.</param>
        internal void SetIUnknown(object value)
        {
            _valueType = (ushort)VarEnum.VT_UNKNOWN;
            _ptr = Marshal.GetIUnknownForObject(value);
        }


        /// <summary>
        /// Set a safe array value
        /// </summary>
        /// <param name="array">The new value to set.</param>
        internal void SetSafeArray(Array array)
        {
            if (array == null) { throw new ArgumentNullException("array"); }
            const ushort vtUnknown = 13;
            IntPtr psa = PropVariantNativeMethods.SafeArrayCreateVector(vtUnknown, 0, (uint)array.Length);

            IntPtr pvData = PropVariantNativeMethods.SafeArrayAccessData(psa);
            try // to remember to release lock on data
            {
                for (int i = 0; i < array.Length; ++i)
                {
                    object obj = array.GetValue(i);
                    IntPtr punk = (obj != null) ? Marshal.GetIUnknownForObject(obj) : IntPtr.Zero;
                    Marshal.WriteIntPtr(pvData, i * IntPtr.Size, punk);
                }
            }
            finally
            {
                PropVariantNativeMethods.SafeArrayUnaccessData(psa);
            }

            _valueType = (ushort)VarEnum.VT_ARRAY | (ushort)VarEnum.VT_UNKNOWN;
            _ptr = psa;
        }

        #endregion

        #region public Properties

        /// <summary>
        /// Gets or sets the variant type.
        /// </summary>
        public VarEnum VarType
        {
            get { return (VarEnum)_valueType; }
            set { _valueType = (ushort)value; }
        }

        /// <summary>
        /// Checks if this has an empty or null value
        /// </summary>
        /// <returns></returns>
        public bool IsNullOrEmpty
        {
            get
            {
                return (_valueType == (ushort)VarEnum.VT_EMPTY || _valueType == (ushort)VarEnum.VT_NULL);
            }
        }

        /// <summary>
        /// Gets the variant value.
        /// </summary>
        public object Value
        {
            get
            {
                switch ((VarEnum)_valueType)
                {
                    case VarEnum.VT_I1:
                        return _sbyte;
                    case VarEnum.VT_UI1:
                        return _byte;
                    case VarEnum.VT_I2:
                        return _short;
                    case VarEnum.VT_UI2:
                        return _ushort;
                    case VarEnum.VT_I4:
                    case VarEnum.VT_INT:
                        return _int32;
                    case VarEnum.VT_UI4:
                    case VarEnum.VT_UINT:
                        return _uint32;
                    case VarEnum.VT_I8:
                        return _long;
                    case VarEnum.VT_UI8:
                        return _ulong;
                    case VarEnum.VT_R4:
                        return _float;
                    case VarEnum.VT_R8:
                        return _double;
                    case VarEnum.VT_BOOL:
                        return _int32 == -1;
                    case VarEnum.VT_ERROR:
                        return _long;
                    case VarEnum.VT_CY:
                        return _decimal;
                    case VarEnum.VT_DATE:
                        return DateTime.FromOADate(_double);
                    case VarEnum.VT_FILETIME:
                        return DateTime.FromFileTime(_long);
                    case VarEnum.VT_BSTR:
                        return Marshal.PtrToStringBSTR(_ptr);
                    case VarEnum.VT_BLOB:
                        return GetBlobData();
                    case VarEnum.VT_LPSTR:
                        return Marshal.PtrToStringAnsi(_ptr);
                    case VarEnum.VT_LPWSTR:
                        return Marshal.PtrToStringUni(_ptr);
                    case VarEnum.VT_UNKNOWN:
                        return Marshal.GetObjectForIUnknown(_ptr);
                    case VarEnum.VT_DISPATCH:
                        return Marshal.GetObjectForIUnknown(_ptr);
                    case VarEnum.VT_DECIMAL:
                        return _decimal;
                    case VarEnum.VT_ARRAY | VarEnum.VT_UNKNOWN:
                        return CrackSingleDimSafeArray(_ptr);
                    case (VarEnum.VT_VECTOR | VarEnum.VT_LPWSTR):
                        return GetVector<string>();
                    case (VarEnum.VT_VECTOR | VarEnum.VT_I2):
                        return GetVector<Int16>();
                    case (VarEnum.VT_VECTOR | VarEnum.VT_UI2):
                        return GetVector<UInt16>();
                    case (VarEnum.VT_VECTOR | VarEnum.VT_I4):
                        return GetVector<Int32>();
                    case (VarEnum.VT_VECTOR | VarEnum.VT_UI4):
                        return GetVector<UInt32>();
                    case (VarEnum.VT_VECTOR | VarEnum.VT_I8):
                        return GetVector<Int64>();
                    case (VarEnum.VT_VECTOR | VarEnum.VT_UI8):
                        return GetVector<UInt64>();
                    case (VarEnum.VT_VECTOR|VarEnum.VT_R4):
                        return GetVector<float>();
                    case (VarEnum.VT_VECTOR | VarEnum.VT_R8):
                        return GetVector<Double>();
                    case (VarEnum.VT_VECTOR | VarEnum.VT_BOOL):
                        return GetVector<Boolean>();
                    case (VarEnum.VT_VECTOR | VarEnum.VT_FILETIME):
                        return GetVector<DateTime>();
                    case (VarEnum.VT_VECTOR | VarEnum.VT_DECIMAL):
                        return GetVector<Decimal>();
                    default:
                        // if the value cannot be marshaled
                        return null;
                }
            }
        }

        #endregion

        #region Private Methods

        private static long GetFileTimeAsLong(ref System.Runtime.InteropServices.ComTypes.FILETIME val)
        {
            return (((long)val.dwHighDateTime) << 32) + val.dwLowDateTime;
        }

        private static System.Runtime.InteropServices.ComTypes.FILETIME DateTimeToFileTime(DateTime value)
        {
            long hFT = value.ToFileTime();
            System.Runtime.InteropServices.ComTypes.FILETIME ft =
                new System.Runtime.InteropServices.ComTypes.FILETIME();
            ft.dwLowDateTime = (int)(hFT & 0xFFFFFFFF);
            ft.dwHighDateTime = (int)(hFT >> 32);
            return ft;
        }

        private object GetBlobData()
        {
            byte[] blobData = new byte[_int32];

            IntPtr pBlobData = _ptr2;
            Marshal.Copy(pBlobData, blobData, 0, _int32);

            return blobData;
        }

        private Array GetVector<T>()
        {
            int count = PropVariantNativeMethods.PropVariantGetElementCount(this);
            if (count <= 0) { return null; }

            lock (_padlock)
            {
                if (_vectorActions == null)
                {
                    _vectorActions = GenerateVectorActions();
                }
            }

            Action<PropVariant, Array, uint> action;
            if (!_vectorActions.TryGetValue(typeof(T), out action))
            {
                throw new InvalidCastException(LocalizedMessages.PropVariantUnsupportedType);
            }

            Array array = new T[count];
            for (uint i = 0; i < count; i++)
            {
                action(this, array, i);
            }

            return array;
        }

        private static Array CrackSingleDimSafeArray(IntPtr psa)
        {
            uint cDims = PropVariantNativeMethods.SafeArrayGetDim(psa);
            if (cDims != 1)
                throw new ArgumentException(LocalizedMessages.PropVariantMultiDimArray, "psa");

            int lBound = PropVariantNativeMethods.SafeArrayGetLBound(psa, 1U);
            int uBound = PropVariantNativeMethods.SafeArrayGetUBound(psa, 1U);

            int n = uBound - lBound + 1; // uBound is inclusive

            object[] array = new object[n];
            for (int i = lBound; i <= uBound; ++i)
            {
                array[i] = PropVariantNativeMethods.SafeArrayGetElement(psa, ref i);
            }

            return array;
        }

        #endregion

        #region IDisposable Members

        /// <summary>
        /// Disposes the object, calls the clear function.
        /// </summary>
        public void Dispose()
        {
            PropVariantNativeMethods.PropVariantClear(this);

            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Finalizer
        /// </summary>
        ~PropVariant()
        {
            Dispose();
        }

        #endregion

        /// <summary>
        /// Provides an simple string representation of the contained data and type.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return string.Format(System.Globalization.CultureInfo.InvariantCulture,
                "{0}: {1}", Value, VarType.ToString());
        }

    }

}
