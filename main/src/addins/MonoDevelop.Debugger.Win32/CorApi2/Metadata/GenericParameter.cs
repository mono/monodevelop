using System;
using System.Globalization;
using System.Reflection;

namespace CorApi2.Metadata
{
    namespace Microsoft.Samples.Debugging.CorMetadata
    {
        public class MethodGenericParameter : GenericParameter
        {
            public MethodGenericParameter (int index) : base (index)
            {
            }
        }


        public class TypeGenericParameter : GenericParameter
        {
            public TypeGenericParameter (int index) : base (index)
            {
            }
        }

        public abstract class GenericParameter : Type
        {
            public int Index { get; private set; }

            public GenericParameter (int index)
            {
                Index = index;
            }

            public override Type MakeByRefType ()
            {
                return this;
            }

            public override Type MakePointerType ()
            {
                return this;
            }

            public override Type MakeArrayType ()
            {
                return this;
            }

            public override Type MakeArrayType (int rank)
            {
                return this;
            }

            public override Type MakeGenericType (params Type[] typeArguments)
            {
                return this;
            }

            public override object[] GetCustomAttributes (bool inherit)
            {
                return new object[0];
            }

            public override bool IsDefined (Type attributeType, bool inherit)
            {
                return false;
            }

            public override ConstructorInfo[] GetConstructors (BindingFlags bindingAttr)
            {
                throw new NotImplementedException ();
            }

            public override Type GetInterface (string name, bool ignoreCase)
            {
                return null;
            }

            public override Type[] GetInterfaces ()
            {
                return EmptyTypes;
            }

            public override EventInfo GetEvent (string name, BindingFlags bindingAttr)
            {
                return null;
            }

            public override EventInfo[] GetEvents (BindingFlags bindingAttr)
            {
                return new EventInfo[0];
            }

            public override Type[] GetNestedTypes (BindingFlags bindingAttr)
            {
                return EmptyTypes;
            }

            public override Type GetNestedType (string name, BindingFlags bindingAttr)
            {
                return null;
            }

            public override Type GetElementType ()
            {
                return null;
            }

            protected override bool HasElementTypeImpl ()
            {
                return false;
            }

            protected override PropertyInfo GetPropertyImpl (string name, BindingFlags bindingAttr, Binder binder,
                Type returnType, Type[] types, ParameterModifier[] modifiers)
            {
                return null;
            }

            public override PropertyInfo[] GetProperties (BindingFlags bindingAttr)
            {
                return new PropertyInfo[0];
            }

            protected override MethodInfo GetMethodImpl (string name, BindingFlags bindingAttr, Binder binder,
                CallingConventions callConvention, Type[] types, ParameterModifier[] modifiers)
            {
                return null;
            }

            public override MethodInfo[] GetMethods (BindingFlags bindingAttr)
            {
                return new MethodInfo[0];
            }

            public override FieldInfo GetField (string name, BindingFlags bindingAttr)
            {
                return null;
            }

            public override FieldInfo[] GetFields (BindingFlags bindingAttr)
            {
                return new FieldInfo[0];
            }

            public override MemberInfo[] GetMembers (BindingFlags bindingAttr)
            {
                return new MemberInfo[0];
            }

            protected override TypeAttributes GetAttributeFlagsImpl ()
            {
                throw new NotImplementedException ();
            }

            protected override bool IsArrayImpl ()
            {
                return false;
            }

            protected override bool IsByRefImpl ()
            {
                return false;
            }

            protected override bool IsPointerImpl ()
            {
                return false;
            }

            protected override bool IsPrimitiveImpl ()
            {
                return false;
            }

            protected override bool IsCOMObjectImpl ()
            {
                return false;
            }

            public override object InvokeMember (string name, BindingFlags invokeAttr, Binder binder, object target,
                object[] args, ParameterModifier[] modifiers, CultureInfo culture, string[] namedParameters)
            {
                throw new NotImplementedException ();
            }

            public override Type UnderlyingSystemType { get { throw new NotImplementedException (); } }

            protected override ConstructorInfo GetConstructorImpl (BindingFlags bindingAttr, Binder binder, CallingConventions callConvention, Type[] types, ParameterModifier[] modifiers)
            {
                throw new NotImplementedException ();
            }

            public override string Name { get { return string.Format("`{0}", Index); }}
            public override Guid GUID { get {return Guid.Empty;}}
            public override Module Module { get {throw new NotImplementedException ();} }
            public override Assembly Assembly { get { throw new NotImplementedException (); } }
            public override string FullName { get { return Name; }}
            public override string Namespace { get {throw new NotImplementedException ();} }

            public override string AssemblyQualifiedName { get { throw new NotImplementedException (); }}
            public override Type BaseType { get {throw new NotImplementedException ();} }

            public override object[] GetCustomAttributes (Type attributeType, bool inherit)
            {
                return new object[0];
            }
        }
    }
}