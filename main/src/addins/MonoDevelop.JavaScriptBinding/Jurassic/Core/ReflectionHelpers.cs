using System;
using System.Collections.Generic;
using System.Reflection;
using Jurassic.Compiler;
using Jurassic.Library;

namespace Jurassic
{

    /// <summary>
    /// Contains static methods to ease reflection operations.
    /// Not used at runtime (used as a source by TricksGen).
    /// </summary>
    public static class ReflectionHelpers
    {
        public static MethodInfo TypeConverter_ToString;
        public static MethodInfo TypeConverter_ToConcatenatedString;
        public static MethodInfo TypeConverter_ToNumber;
        public static MethodInfo TypeConverter_ToBoolean;
        public static MethodInfo TypeConverter_ToObject;
        public static MethodInfo TypeConverter_ToInteger;
        public static MethodInfo TypeConverter_ToInt32;
        public static MethodInfo TypeConverter_ToUint32;
        public static MethodInfo TypeConverter_ToPrimitive;

        public static MethodInfo TypeComparer_Equals;
        public static MethodInfo TypeComparer_StrictEquals;
        public static MethodInfo TypeComparer_LessThan;
        public static MethodInfo TypeComparer_LessThanOrEqual;
        public static MethodInfo TypeComparer_GreaterThan;
        public static MethodInfo TypeComparer_GreaterThanOrEqual;

        public static MethodInfo TypeUtilities_TypeOf;
        public static MethodInfo TypeUtilities_EnumeratePropertyNames;
        public static MethodInfo TypeUtilities_Add;
        public static MethodInfo TypeUtilities_IsPrimitiveOrObject;
        public static MethodInfo TypeUtilities_VerifyThisObject;

        public static MethodInfo FunctionInstance_HasInstance;
        public static MethodInfo FunctionInstance_ConstructLateBound;
        public static MethodInfo FunctionInstance_CallWithStackTrace;
        public static MethodInfo FunctionInstance_InstancePrototype;

        public static MethodInfo ScriptEngine_Global;
        public static MethodInfo ScriptEngine_Boolean;
        public static MethodInfo ScriptEngine_Function;
        public static MethodInfo ScriptEngine_RegExp;
        public static MethodInfo ScriptEngine_Array;
        public static MethodInfo ScriptEngine_Object;
        public static MethodInfo Global_Eval;

        public static ConstructorInfo String_Constructor_Char_Int;
        public static MethodInfo String_Concat;
        public static MethodInfo String_Length;
        public static MethodInfo String_CompareOrdinal;
        public static MethodInfo String_Format;
        public static MethodInfo String_GetChars;

        public static ConstructorInfo ConcatenatedString_Constructor_String;
        public static ConstructorInfo ConcatenatedString_Constructor_String_String;
        public static MethodInfo ConcatenatedString_Length;
        public static MethodInfo ConcatenatedString_Concatenate_Object;
        public static MethodInfo ConcatenatedString_Concatenate_String;
        public static MethodInfo ConcatenatedString_Concatenate_ConcatenatedString;
        public static MethodInfo ConcatenatedString_Append_Object;
        public static MethodInfo ConcatenatedString_Append_String;
        public static MethodInfo ConcatenatedString_Append_ConcatenatedString;
        public static MethodInfo ConcatenatedString_ToString;

        public static MethodInfo IEnumerable_GetEnumerator;
        public static MethodInfo IEnumerator_MoveNext;
        public static MethodInfo IEnumerator_Current;

        public static MethodInfo Debugger_Break;
        public static MethodInfo JavaScriptException_ErrorObject;
        public static MethodInfo Boolean_Construct;
        public static MethodInfo Object_Construct;
        
        public static MethodInfo RegExp_Construct;
        public static MethodInfo Array_New;
        public static MethodInfo Delegate_CreateDelegate;
        public static MethodInfo Type_GetTypeFromHandle;
        public static MethodInfo MethodBase_GetMethodFromHandle;
        public static MethodInfo GeneratedMethod_Load;
        public static MethodInfo ClrInstanceWrapper_GetWrappedInstance;
        public static MethodInfo Decimal_ToDouble;
        public static MethodInfo BinderUtilities_ResolveOverloads;
        public static MethodInfo Convert_ToInt32_Double;

        public static MethodInfo ObjectInstance_Delete;
        public static MethodInfo ObjectInstance_DefineProperty;
        public static MethodInfo ObjectInstance_HasProperty;
        public static MethodInfo ObjectInstance_GetPropertyValue_String;
        public static MethodInfo ObjectInstance_GetPropertyValue_Int;
        public static MethodInfo ObjectInstance_SetPropertyValue_String;
        public static MethodInfo ObjectInstance_SetPropertyValue_Int;
        public static MethodInfo ObjectInstance_SetPropertyValueIfExists;
        public static MethodInfo ObjectInstance_InlinePropertyValues;
        public static MethodInfo ObjectInstance_InlineCacheKey;
        public static MethodInfo ObjectInstance_InlineGetPropertyValue;
        public static MethodInfo ObjectInstance_InlineSetPropertyValue;
        public static MethodInfo ObjectInstance_InlineSetPropertyValueIfExists;

        public static MethodInfo Scope_ParentScope;
        public static MethodInfo ObjectScope_CreateRuntimeScope;
        public static MethodInfo ObjectScope_ScopeObject;
        public static MethodInfo DeclarativeScope_CreateRuntimeScope;
        public static MethodInfo DeclarativeScope_Values;
        public static MethodInfo Scope_HasValue;
        public static MethodInfo Scope_GetValue;
        public static MethodInfo Scope_SetValue;
        public static MethodInfo Scope_Delete;

        public static ConstructorInfo JavaScriptException_Constructor_Error;
        public static ConstructorInfo JavaScriptException_Constructor_Object;
        public static ConstructorInfo UserDefinedFunction_Constructor;
        public static ConstructorInfo FunctionDelegate_Constructor;
        public static ConstructorInfo Arguments_Constructor;
        public static ConstructorInfo PropertyDescriptor_Constructor2;
        public static ConstructorInfo PropertyDescriptor_Constructor3;
        public static ConstructorInfo ClrInstanceWrapper_Constructor;
        public static ConstructorInfo Decimal_Constructor_Double;

        public static FieldInfo Undefined_Value;
        public static FieldInfo Null_Value;

        public static ConstructorInfo LongJumpException_Constructor;
        public static MethodInfo LongJumpException_RouteID;

        /// <summary>
        /// Initializes static members of this class.
        /// </summary>
        static ReflectionHelpers()
        {
            // Retrieve the various MethodInfos used for type conversion.
            TypeConverter_ToString = GetStaticMethod(typeof(TypeConverter), "ToString", typeof(object));
            TypeConverter_ToConcatenatedString = GetStaticMethod(typeof(TypeConverter), "ToConcatenatedString", typeof(object));
            TypeConverter_ToNumber = GetStaticMethod(typeof(TypeConverter), "ToNumber", typeof(object));
            TypeConverter_ToBoolean = GetStaticMethod(typeof(TypeConverter), "ToBoolean", typeof(object));
            TypeConverter_ToObject = GetStaticMethod(typeof(TypeConverter), "ToObject", typeof(ScriptEngine), typeof(object), typeof(int), typeof(string), typeof(string));
            TypeConverter_ToInteger = GetStaticMethod(typeof(TypeConverter), "ToInteger", typeof(object));
            TypeConverter_ToInt32 = GetStaticMethod(typeof(TypeConverter), "ToInt32", typeof(object));
            TypeConverter_ToUint32 = GetStaticMethod(typeof(TypeConverter), "ToUint32", typeof(object));
            TypeConverter_ToPrimitive = GetStaticMethod(typeof(TypeConverter), "ToPrimitive", typeof(object), typeof(PrimitiveTypeHint));

            TypeComparer_Equals = GetStaticMethod(typeof(TypeComparer), "Equals", typeof(object), typeof(object));
            TypeComparer_StrictEquals = GetStaticMethod(typeof(TypeComparer), "StrictEquals", typeof(object), typeof(object));
            TypeComparer_LessThan = GetStaticMethod(typeof(TypeComparer), "LessThan", typeof(object), typeof(object));
            TypeComparer_LessThanOrEqual = GetStaticMethod(typeof(TypeComparer), "LessThanOrEqual", typeof(object), typeof(object));
            TypeComparer_GreaterThan = GetStaticMethod(typeof(TypeComparer), "GreaterThan", typeof(object), typeof(object));
            TypeComparer_GreaterThanOrEqual = GetStaticMethod(typeof(TypeComparer), "GreaterThanOrEqual", typeof(object), typeof(object));

            TypeUtilities_TypeOf = GetStaticMethod(typeof(TypeUtilities), "TypeOf", typeof(object));
            TypeUtilities_EnumeratePropertyNames = GetStaticMethod(typeof(TypeUtilities), "EnumeratePropertyNames", typeof(ScriptEngine), typeof(object));
            TypeUtilities_Add = GetStaticMethod(typeof(TypeUtilities), "Add", typeof(object), typeof(object));
            TypeUtilities_IsPrimitiveOrObject = GetStaticMethod(typeof(TypeUtilities), "IsPrimitiveOrObject", typeof(object));
            TypeUtilities_VerifyThisObject = GetStaticMethod(typeof(TypeUtilities), "VerifyThisObject", typeof(ScriptEngine), typeof(object), typeof(string));

            ObjectInstance_Delete = GetInstanceMethod(typeof(ObjectInstance), "Delete", typeof(string), typeof(bool));
            ObjectInstance_DefineProperty = GetInstanceMethod(typeof(ObjectInstance), "DefineProperty", typeof(string), typeof(PropertyDescriptor), typeof(bool));
            ObjectInstance_HasProperty = GetInstanceMethod(typeof(ObjectInstance), "HasProperty", typeof(string));
            ObjectInstance_GetPropertyValue_String = GetInstanceMethod(typeof(ObjectInstance), "GetPropertyValue", typeof(string));
            ObjectInstance_GetPropertyValue_Int = GetInstanceMethod(typeof(ObjectInstance), "GetPropertyValue", typeof(uint));
            ObjectInstance_SetPropertyValue_String = GetInstanceMethod(typeof(ObjectInstance), "SetPropertyValue", typeof(string), typeof(object), typeof(bool));
            ObjectInstance_SetPropertyValue_Int = GetInstanceMethod(typeof(ObjectInstance), "SetPropertyValue", typeof(uint), typeof(object), typeof(bool));
            ObjectInstance_SetPropertyValueIfExists = GetInstanceMethod(typeof(ObjectInstance), "SetPropertyValueIfExists", typeof(string), typeof(object), typeof(bool));
            ObjectInstance_InlinePropertyValues = GetInstanceMethod(typeof(ObjectInstance), "get_InlinePropertyValues");
            ObjectInstance_InlineCacheKey = GetInstanceMethod(typeof(ObjectInstance), "get_InlineCacheKey");
            ObjectInstance_InlineGetPropertyValue = GetInstanceMethod(typeof(ObjectInstance), "InlineGetPropertyValue",
                new Type[] { typeof(string), typeof(int).MakeByRefType(), typeof(object).MakeByRefType() });
            ObjectInstance_InlineSetPropertyValue = GetInstanceMethod(typeof(ObjectInstance), "InlineSetPropertyValue",
                new Type[] { typeof(string), typeof(object), typeof(bool), typeof(int).MakeByRefType(), typeof(object).MakeByRefType() });
            ObjectInstance_InlineSetPropertyValueIfExists = GetInstanceMethod(typeof(ObjectInstance), "InlineSetPropertyValueIfExists",
                new Type[] { typeof(string), typeof(object), typeof(bool), typeof(int).MakeByRefType(), typeof(object).MakeByRefType() });

            Scope_ParentScope = GetInstanceMethod(typeof(Scope), "get_ParentScope");
            ObjectScope_CreateRuntimeScope = GetStaticMethod(typeof(ObjectScope), "CreateRuntimeScope", typeof(Scope), typeof(ObjectInstance), typeof(bool), typeof(bool));
            ObjectScope_ScopeObject = GetInstanceMethod(typeof(ObjectScope), "get_ScopeObject");
            DeclarativeScope_CreateRuntimeScope = GetStaticMethod(typeof(DeclarativeScope), "CreateRuntimeScope", typeof(Scope), typeof(string[]));
            DeclarativeScope_Values = GetInstanceMethod(typeof(DeclarativeScope), "get_Values");
            Scope_HasValue = GetInstanceMethod(typeof(Scope), "HasValue", typeof(string));
            Scope_GetValue = GetInstanceMethod(typeof(Scope), "GetValue", typeof(string));
            Scope_SetValue = GetInstanceMethod(typeof(Scope), "SetValue", typeof(string), typeof(object));
            Scope_Delete = GetInstanceMethod(typeof(Scope), "Delete", typeof(string));

            FunctionInstance_HasInstance = GetInstanceMethod(typeof(FunctionInstance), "HasInstance", typeof(object));
            FunctionInstance_ConstructLateBound = GetInstanceMethod(typeof(FunctionInstance), "ConstructLateBound", typeof(object[]));
            FunctionInstance_CallWithStackTrace = GetInstanceMethod(typeof(FunctionInstance), "CallWithStackTrace", typeof(string), typeof(string), typeof(int), typeof(object), typeof(object[]));
            FunctionInstance_InstancePrototype = GetInstanceMethod(typeof(FunctionInstance), "get_InstancePrototype");

            ScriptEngine_Global = GetInstanceMethod(typeof(ScriptEngine), "get_Global");
            ScriptEngine_Boolean = GetInstanceMethod(typeof(ScriptEngine), "get_Boolean");
            ScriptEngine_Function = GetInstanceMethod(typeof(ScriptEngine), "get_Function");
            ScriptEngine_RegExp = GetInstanceMethod(typeof(ScriptEngine), "get_RegExp");
            ScriptEngine_Array = GetInstanceMethod(typeof(ScriptEngine), "get_Array");
            ScriptEngine_Object = GetInstanceMethod(typeof(ScriptEngine), "get_Object");
            Global_Eval = GetStaticMethod(typeof(GlobalObject), "Eval", typeof(ScriptEngine), typeof(object), typeof(Scope), typeof(object), typeof(bool));

            String_Constructor_Char_Int = GetConstructor(typeof(string), typeof(char), typeof(int));
            String_Concat = GetStaticMethod(typeof(string), "Concat", typeof(string), typeof(string));
            String_Length = GetInstanceMethod(typeof(string), "get_Length");
            String_CompareOrdinal = GetStaticMethod(typeof(string), "CompareOrdinal", typeof(string), typeof(string));
            String_Format = GetStaticMethod(typeof(string), "Format", typeof(string), typeof(object[]));
            String_GetChars = GetInstanceMethod(typeof(string), "get_Chars", typeof(int));

            ConcatenatedString_Constructor_String = GetConstructor(typeof(ConcatenatedString), typeof(string));
            ConcatenatedString_Constructor_String_String = GetConstructor(typeof(ConcatenatedString), typeof(string), typeof(string));
            ConcatenatedString_Length = GetInstanceMethod(typeof(ConcatenatedString), "get_Length");
            ConcatenatedString_Concatenate_Object = GetInstanceMethod(typeof(ConcatenatedString), "Concatenate", typeof(object));
            ConcatenatedString_Concatenate_String = GetInstanceMethod(typeof(ConcatenatedString), "Concatenate", typeof(string));
            ConcatenatedString_Concatenate_ConcatenatedString = GetInstanceMethod(typeof(ConcatenatedString), "Concatenate", typeof(ConcatenatedString));
            ConcatenatedString_Append_Object = GetInstanceMethod(typeof(ConcatenatedString), "Append", typeof(object));
            ConcatenatedString_Append_String = GetInstanceMethod(typeof(ConcatenatedString), "Append", typeof(string));
            ConcatenatedString_Append_ConcatenatedString = GetInstanceMethod(typeof(ConcatenatedString), "Append", typeof(ConcatenatedString));
            ConcatenatedString_ToString = GetInstanceMethod(typeof(ConcatenatedString), "ToString");

            JavaScriptException_Constructor_Error = GetConstructor(typeof(JavaScriptException), typeof(ScriptEngine), typeof(string), typeof(string), typeof(int), typeof(string), typeof(string));
            JavaScriptException_Constructor_Object = GetConstructor(typeof(JavaScriptException), typeof(object), typeof(int), typeof(string), typeof(string));
            IEnumerable_GetEnumerator = GetInstanceMethod(typeof(IEnumerable<string>), "GetEnumerator");
            IEnumerator_MoveNext = GetInstanceMethod(typeof(System.Collections.IEnumerator), "MoveNext");
            IEnumerator_Current = GetInstanceMethod(typeof(IEnumerator<string>), "get_Current");
            Debugger_Break = GetStaticMethod(typeof(System.Diagnostics.Debugger), "Break");
            JavaScriptException_ErrorObject = GetInstanceMethod(typeof(JavaScriptException), "get_ErrorObject");
            Boolean_Construct = GetInstanceMethod(typeof(BooleanConstructor), "Construct", typeof(bool));
            
            RegExp_Construct = GetInstanceMethod(typeof(Jurassic.Library.RegExpConstructor), "Construct", typeof(object), typeof(string));
            Array_New = GetInstanceMethod(typeof(ArrayConstructor), "New", typeof(object[]));
            Object_Construct = GetInstanceMethod(typeof(ObjectConstructor), "Construct");
            UserDefinedFunction_Constructor = GetConstructor(typeof(UserDefinedFunction), typeof(ObjectInstance),
                typeof(string), typeof(IList<string>), typeof(Scope), typeof(string), typeof(GeneratedMethod), typeof(bool));
            Delegate_CreateDelegate = GetStaticMethod(typeof(Delegate), "CreateDelegate", typeof(Type), typeof(MethodInfo));
            Type_GetTypeFromHandle = GetStaticMethod(typeof(Type), "GetTypeFromHandle", typeof(RuntimeTypeHandle));
            MethodBase_GetMethodFromHandle = GetStaticMethod(typeof(MethodBase), "GetMethodFromHandle", typeof(RuntimeMethodHandle));
            FunctionDelegate_Constructor = GetConstructor(typeof(Library.FunctionDelegate), typeof(object), typeof(IntPtr));
            Arguments_Constructor = GetConstructor(typeof(ArgumentsInstance), typeof(ObjectInstance), typeof(UserDefinedFunction), typeof(DeclarativeScope), typeof(object[]));
            PropertyDescriptor_Constructor2 = GetConstructor(typeof(PropertyDescriptor), typeof(object), typeof(Library.PropertyAttributes));
            PropertyDescriptor_Constructor3 = GetConstructor(typeof(PropertyDescriptor), typeof(FunctionInstance), typeof(FunctionInstance), typeof(Library.PropertyAttributes));
            ClrInstanceWrapper_Constructor = GetConstructor(typeof(ClrInstanceWrapper), typeof(ScriptEngine), typeof(object));
            Decimal_Constructor_Double = GetConstructor(typeof(decimal), typeof(double));

            GeneratedMethod_Load = GetStaticMethod(typeof(GeneratedMethod), "Load", typeof(long));
            ClrInstanceWrapper_GetWrappedInstance = GetInstanceMethod(typeof(ClrInstanceWrapper), "get_WrappedInstance");
            Decimal_ToDouble = GetStaticMethod(typeof(decimal), "ToDouble", typeof(decimal));
            BinderUtilities_ResolveOverloads = GetStaticMethod(typeof(BinderUtilities), "ResolveOverloads", typeof(RuntimeMethodHandle[]), typeof(ScriptEngine), typeof(object), typeof(object[]));
            Convert_ToInt32_Double = GetStaticMethod(typeof(Convert), "ToInt32", typeof(double));

            Undefined_Value = GetField(typeof(Undefined), "Value");
            Null_Value = GetField(typeof(Null), "Value");

            LongJumpException_Constructor = GetConstructor(typeof(LongJumpException), typeof(int));
            LongJumpException_RouteID = GetInstanceMethod(typeof(LongJumpException), "get_RouteID");

#if DEBUG
            // When using Reflection Emit, all calls into Jurassic.dll are cross-assembly and thus
            // must be public.
            var text = new System.Text.StringBuilder();
            foreach (var reflectionField in GetMembers())
            {
                var methodBase = reflectionField.MemberInfo as MethodBase;
                if (methodBase != null && (methodBase.Attributes & MethodAttributes.Public) != MethodAttributes.Public)
                {
                    text.Append(methodBase.DeclaringType.ToString());
                    text.Append("/");
                    text.AppendLine(methodBase.ToString());
                }
                var field = reflectionField.MemberInfo as FieldInfo;
                if (field != null && (field.Attributes & FieldAttributes.Public) != FieldAttributes.Public)
                    text.AppendLine(field.ToString());
                if ((reflectionField.MemberInfo.DeclaringType.Attributes & TypeAttributes.Public) != TypeAttributes.Public)
                    text.AppendLine(reflectionField.MemberInfo.DeclaringType.ToString());
            }
            if (text.Length > 0)
                throw new InvalidOperationException("The following members need to be public: " + Environment.NewLine + text.ToString());

            // For ease of debugging, all runtime calls should have the DebuggerHidden
            // attribute.
            //text.Clear();
            //foreach (var reflectionField in GetMembers())
            //{
            //    var methodBase = reflectionField.MemberInfo as MethodBase;
            //    if (methodBase != null && Attribute.GetCustomAttribute(methodBase, typeof(System.Diagnostics.DebuggerHiddenAttribute)) == null)
            //    {
            //        text.Append(methodBase.DeclaringType.ToString());
            //        text.Append("/");
            //        text.AppendLine(methodBase.ToString());
            //    }
            //}
            //if (text.Length > 0)
            //    throw new InvalidOperationException("The following methods do not have [DebuggerHidden]: " + Environment.NewLine + text.ToString());
#endif
        }

        public struct ReflectionField
        {
            public string FieldName;
            public MemberInfo MemberInfo;
        }

        /// <summary>
        /// Gets an enumerable list of all the MemberInfos that are statically known to be used by this DLL.
        /// </summary>
        /// <returns> An enumerable list of all the MemberInfos that are used by this DLL. </returns>
        public static IEnumerable<ReflectionField> GetMembers()
        {
            foreach (FieldInfo field in typeof(ReflectionHelpers).GetFields(BindingFlags.NonPublic | BindingFlags.Static))
            {
                if (field.FieldType != typeof(MethodInfo) && field.FieldType != typeof(ConstructorInfo) && field.FieldType != typeof(FieldInfo))
                    continue;
                yield return new ReflectionField() { FieldName = field.Name, MemberInfo = (MemberInfo)field.GetValue(null) };
            }
        }

        /// <summary>
        /// Gets the FieldInfo for a field.  Throws an exception if the search fails.
        /// </summary>
        /// <param name="type"> The type to search. </param>
        /// <param name="name"> The name of the field. </param>
        /// <returns> The FieldInfo for a field. </returns>
        public static FieldInfo GetField(Type type, string name)
        {
            FieldInfo result = type.GetField(name);
            if (result == null)
                throw new InvalidOperationException(string.Format("The field '{1}' does not exist on type '{0}'.", type, name));
            return result;
        }

        /// <summary>
        /// Gets the ConstructorInfo for a constructor.  Throws an exception if the search fails.
        /// </summary>
        /// <param name="type"> The type to search. </param>
        /// <param name="parameterTypes"> The types of the parameters accepted by the constructor. </param>
        /// <returns> The ConstructorInfo for the constructor. </returns>
        public static ConstructorInfo GetConstructor(Type type, params Type[] parameterTypes)
        {
            var flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
            ConstructorInfo result = type.GetConstructor(flags, null, parameterTypes, null);
            if (result == null)
                throw new InvalidOperationException(string.Format("The constructor {0}({1}) does not exist.", type.FullName, StringHelpers.Join<Type>(", ", parameterTypes)));
            return result;
        }

        /// <summary>
        /// Gets the MethodInfo for an instance method.  Throws an exception if the search fails.
        /// </summary>
        /// <param name="type"> The type to search. </param>
        /// <param name="name"> The name of the method to search for. </param>
        /// <param name="parameterTypes"> The types of the parameters accepted by the method. </param>
        /// <returns> The MethodInfo for the method. </returns>
        public static MethodInfo GetInstanceMethod(Type type, string name, params Type[] parameterTypes)
        {
            var flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly | BindingFlags.ExactBinding;
            MethodInfo result = type.GetMethod(name, flags, null, parameterTypes, null);
            if (result == null)
                throw new InvalidOperationException(string.Format("The instance method {0}.{1}({2}) does not exist.", type.FullName, name, StringHelpers.Join<Type>(", ", parameterTypes)));
            return result;
        }

        /// <summary>
        /// Gets the MethodInfo for a static method.  Throws an exception if the search fails.
        /// </summary>
        /// <param name="type"> The type to search. </param>
        /// <param name="name"> The name of the method to search for. </param>
        /// <param name="parameterTypes"> The types of the parameters accepted by the method. </param>
        /// <returns> The MethodInfo for the method. </returns>
        public static MethodInfo GetStaticMethod(Type type, string name, params Type[] parameterTypes)
        {
            var flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.DeclaredOnly | BindingFlags.ExactBinding;
            MethodInfo result = type.GetMethod(name, flags, null, parameterTypes, null);
            if (result == null)
                throw new InvalidOperationException(string.Format("The static method {0}.{1}({2}) does not exist.", type.FullName, name, StringHelpers.Join<Type>(", ", parameterTypes)));
            return result;
        }

        /// <summary>
        /// Gets the MethodInfo for a generic instance method.  Throws an exception if the search fails.
        /// </summary>
        /// <param name="type"> The type to search. </param>
        /// <param name="name"> The name of the method to search for. </param>
        /// <returns> The MethodInfo for the method. </returns>
        private static MethodInfo GetGenericInstanceMethod(Type type, string name)
        {
            var flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly;
            MethodInfo result = type.GetMethod(name, flags);
            if (result == null)
                throw new InvalidOperationException(string.Format("The instance method {0}.{1}(...) does not exist.", type.FullName, name));
            return result;
        }

    }

}