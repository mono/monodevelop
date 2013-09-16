//---------------------------------------------------------------------
//  This file is part of the CLR Managed Debugger (mdbg) Sample.
// 
//  Copyright (C) Microsoft Corporation.  All rights reserved.
//---------------------------------------------------------------------
using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Runtime.InteropServices;
using System.Globalization;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

using Microsoft.Samples.Debugging.CorDebug; 
using Microsoft.Samples.Debugging.CorMetadata.NativeApi;
using Microsoft.Samples.Debugging.CorDebug.NativeApi;

namespace Microsoft.Samples.Debugging.CorMetadata
{
    public sealed class CorMetadataImport
    {
		// [Xamarin] Expression evaluator.
		public static Dictionary<CorElementType, Type> CoreTypes = new Dictionary<CorElementType, Type> ();

		// [Xamarin] Expression evaluator.
		static CorMetadataImport ()
		{
			CoreTypes.Add (CorElementType.ELEMENT_TYPE_BOOLEAN, typeof (bool));
			CoreTypes.Add (CorElementType.ELEMENT_TYPE_CHAR, typeof (char));
			CoreTypes.Add (CorElementType.ELEMENT_TYPE_I1, typeof (sbyte));
			CoreTypes.Add (CorElementType.ELEMENT_TYPE_U1, typeof (byte));
			CoreTypes.Add (CorElementType.ELEMENT_TYPE_I2, typeof (short));
			CoreTypes.Add (CorElementType.ELEMENT_TYPE_U2, typeof (ushort));
			CoreTypes.Add (CorElementType.ELEMENT_TYPE_I4, typeof (int));
			CoreTypes.Add (CorElementType.ELEMENT_TYPE_U4, typeof (uint));
			CoreTypes.Add (CorElementType.ELEMENT_TYPE_I8, typeof (long));
			CoreTypes.Add (CorElementType.ELEMENT_TYPE_U8, typeof (ulong));
			CoreTypes.Add (CorElementType.ELEMENT_TYPE_R4, typeof (float));
			CoreTypes.Add (CorElementType.ELEMENT_TYPE_R8, typeof (double));
			CoreTypes.Add (CorElementType.ELEMENT_TYPE_STRING, typeof (string));
			CoreTypes.Add (CorElementType.ELEMENT_TYPE_I, typeof (IntPtr));
			CoreTypes.Add (CorElementType.ELEMENT_TYPE_U, typeof (UIntPtr));
		}

        public CorMetadataImport(CorModule managedModule)
        {
            m_importer = managedModule.GetMetaDataInterface <IMetadataImport>();
            Debug.Assert(m_importer != null);
        }

        public CorMetadataImport(object metadataImport)
        {
            m_importer = (IMetadataImport) metadataImport;
            Debug.Assert(m_importer != null);
        }

        // methods
        public MethodInfo GetMethodInfo(int methodToken)
        {
            return new MetadataMethodInfo(m_importer,methodToken);
        }

        public Type GetType(int typeToken)
        {
            return new MetadataType(m_importer,typeToken);
        }


        // Get number of generic parameters on a given type.
        // Eg, for 'Foo<t, u>', returns 2.
        // for 'Foo', returns 0.
        public int CountGenericParams(int typeToken)
        {
            
            // This may fail in pre V2.0 debuggees.
            //Guid IID_IMetadataImport2 = new Guid("FCE5EFA0-8BBA-4f8e-A036-8F2022B08466");
            if( ! (m_importer is IMetadataImport2) )
                return 0; // this means we're pre v2.0 debuggees.
            

            IMetadataImport2 importer2 = (m_importer as IMetadataImport2);
            Debug.Assert( importer2!=null );
            
            int dummy;            
            uint dummy2;
            IntPtr hEnum = IntPtr.Zero;
            int count;
            importer2.EnumGenericParams(ref hEnum, typeToken, out dummy, 1, out dummy2);
            try
            {
                m_importer.CountEnum(hEnum, out count);
            }
            finally
            {
                if( hEnum != IntPtr.Zero )
                    importer2.CloseEnum(hEnum);
            }
            return count;
        }

        // Returns filename of scope, if available.
        // Returns null if filename is not available.
        public string GetScopeFilename()
        {
            int size;

            try
            {
                Guid mvid;
                m_importer.GetScopeProps(null, 0, out size, out mvid);
                StringBuilder sb = new StringBuilder(size);
                m_importer.GetScopeProps(sb, sb.Capacity, out size, out mvid);
                sb.Length = size;
                return sb.ToString();
            }
            catch
            {
                return null;            
            }
        }

        public string GetUserString(int token)
        {
            int size;
            m_importer.GetUserString(token,null,0,out size);
            StringBuilder sb = new StringBuilder(size);
            m_importer.GetUserString(token,sb,sb.Capacity,out size);
            sb.Length=size;
            return sb.ToString();
        }

        public const int TokenNotFound = -1;
        public const int TokenGlobalNamespace = 0;
        
        // returns a type token from name
        // when the function fails, we return token TokenNotFound value.
        public int GetTypeTokenFromName(string name)
        {
            int token = CorMetadataImport.TokenNotFound;
            if( name.Length==0 )
                // this is special global type (we'll return token 0)
                token = CorMetadataImport.TokenGlobalNamespace;
            else
            {
                try 
                {
                    m_importer.FindTypeDefByName(name,0,out token);
                }
                catch(COMException e)
                {
                    token=CorMetadataImport.TokenNotFound;
                    if((HResult)e.ErrorCode==HResult.CLDB_E_RECORD_NOTFOUND)
                    {
                        int i = name.LastIndexOf('.');
                        if(i>0)
                        {
                            int parentToken = GetTypeTokenFromName(name.Substring(0,i));
                            if( parentToken!=CorMetadataImport.TokenNotFound )
                            {
                                try 
                                {
                                    m_importer.FindTypeDefByName(name.Substring(i+1),parentToken,out token);
                                }
                                catch(COMException e2) 
                                {
                                    token=CorMetadataImport.TokenNotFound;
                                    if((HResult)e2.ErrorCode!=HResult.CLDB_E_RECORD_NOTFOUND)
                                        throw;
                                }
                            }
                        } 
                    }
                    else
                    throw;
                }
            }
            return token;
        }

        public string GetTypeNameFromRef(int token)
        {
            int resScope,size;
            m_importer.GetTypeRefProps(token,out resScope,null,0,out size);
            StringBuilder sb = new StringBuilder(size);
            m_importer.GetTypeRefProps(token,out resScope,sb,sb.Capacity,out size);
            return sb.ToString();
        }

        public string GetTypeNameFromDef(int token,out int extendsToken)
        {
            int size;
            TypeAttributes pdwTypeDefFlags;
            m_importer.GetTypeDefProps(token,null,0,out size,
                                       out pdwTypeDefFlags,out extendsToken);
            StringBuilder sb = new StringBuilder(size);
            m_importer.GetTypeDefProps(token,sb,sb.Capacity,out size,
                                       out pdwTypeDefFlags,out extendsToken);
            return sb.ToString();
        }


        public string GetMemberRefName(int token)
        {
            if(!m_importer.IsValidToken((uint)token))
                throw new ArgumentException();

            uint size;
            int classToken;
            IntPtr ppvSigBlob;
            int pbSig;

            m_importer.GetMemberRefProps((uint)token,
                                         out classToken,
                                         null,
                                         0,
                                         out size,
                                         out ppvSigBlob,
                                         out pbSig
                                         );

            StringBuilder member = new StringBuilder((int)size);
            m_importer.GetMemberRefProps((uint)token,
                                         out classToken,
                                         member,
                                         member.Capacity,
                                         out size,
                                         out ppvSigBlob,
                                         out pbSig
                                         );

            string className=null;
            switch(TokenUtils.TypeFromToken(classToken))
            {
            default:
                Debug.Assert(false);
                break;
            case CorTokenType.mdtTypeRef:
                className = GetTypeNameFromRef(classToken);
                break;
            case CorTokenType.mdtTypeDef: 
                {           
                    int parentToken;
                    className = GetTypeNameFromDef(classToken,out parentToken);
                    break;
                }
            }
            return className + "." + member.ToString();
        }


        public IEnumerable DefinedTypes
        {
            get 
            {
                return new TypeDefEnum(this);
            }
        }

        public object RawCOMObject
        {
            get 
            {
                return m_importer;
            }
        }


        // properties


        //////////////////////////////////////////////////////////////////////////////////
        //
        // CorMetadataImport variables
        //
        //////////////////////////////////////////////////////////////////////////////////

        internal IMetadataImport  m_importer;
    }

    //////////////////////////////////////////////////////////////////////////////////
    //
    // MetadataMethodInfo
    //
    //////////////////////////////////////////////////////////////////////////////////

    public sealed class MetadataMethodInfo : MethodInfo
    {
        internal MetadataMethodInfo(IMetadataImport importer,int methodToken)
        {
            if(!importer.IsValidToken((uint)methodToken))
                throw new ArgumentException();

            m_importer = importer;
            m_methodToken=methodToken;

            int size;
            uint pdwAttr;
            IntPtr ppvSigBlob;
            uint pulCodeRVA,pdwImplFlags;
            uint pcbSigBlob;

            m_importer.GetMethodProps((uint)methodToken,
                                      out m_classToken,
                                      null,
                                      0,
                                      out size,
                                      out pdwAttr,
                                      out ppvSigBlob, 
                                      out pcbSigBlob,
                                      out pulCodeRVA,
                                      out pdwImplFlags);

            StringBuilder szMethodName = new StringBuilder(size);
            m_importer.GetMethodProps((uint)methodToken,
                                    out m_classToken,
                                    szMethodName,
                                    szMethodName.Capacity,
                                    out size,
                                    out pdwAttr,
                                    out ppvSigBlob, 
                                    out pcbSigBlob,
                                    out pulCodeRVA,
                                    out pdwImplFlags);

			// [Xamarin] Expression evaluator.
			CorCallingConvention callingConv;
			MetadataHelperFunctions.ReadMethodSignature (importer, ref ppvSigBlob, out callingConv, out m_retType, out m_argTypes);
			m_name = szMethodName.ToString ();
            m_methodAttributes = (MethodAttributes)pdwAttr;
        }

		// [Xamarin] Expression evaluator.
        public override Type ReturnType 
        {
            get 
            {
				return m_retType;
            }
        }

        public override ICustomAttributeProvider ReturnTypeCustomAttributes 
        {
            get 
            {
                throw new NotImplementedException();
            }
        }

        public override Type ReflectedType 
        { 
            get 
            {
                throw new NotImplementedException();
            }
        }

        public override Type DeclaringType 
        { 
            get 
            {
                if(TokenUtils.IsNullToken(m_classToken))
                    return null;                            // this is method outside of class
                
                return new MetadataType(m_importer,m_classToken);
            }
        }

        public override string Name 
        {
            get 
            {
                return m_name;
            }
        }

        public override MethodAttributes Attributes 
        { 
            get
            {
                return m_methodAttributes;
            }
        }

        public override RuntimeMethodHandle MethodHandle 
        { 
            get 
            {
                throw new NotImplementedException();
            }
        }

        public override MethodInfo GetBaseDefinition()
        {
            throw new NotImplementedException();
        }

        public override bool IsDefined (Type attributeType, bool inherit)
        {
            throw new NotImplementedException();
        }

        public override object[] GetCustomAttributes(Type attributeType, bool inherit)
        {
            throw new NotImplementedException();
        }

        public override object[] GetCustomAttributes(bool inherit)
        {
            throw new NotImplementedException();
        }

        public override object Invoke(object obj, BindingFlags invokeAttr, Binder binder, object[] parameters, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        public override System.Reflection.MethodImplAttributes GetMethodImplementationFlags()
        {
            throw new NotImplementedException();
        }

		// [Xamarin] Expression evaluator.
        public override System.Reflection.ParameterInfo[] GetParameters()
        {
            ArrayList al = new ArrayList();
            IntPtr hEnum = new IntPtr();
			int nArg = 0;
            try 
            {
                while(true) 
                {
                    uint count;
                    int paramToken;
                    m_importer.EnumParams(ref hEnum,
                                          m_methodToken, out paramToken,1,out count);
                    if(count!=1)
                        break;
					MetadataParameterInfo mp = new MetadataParameterInfo (m_importer, paramToken,
													 this, DeclaringType, m_argTypes [nArg++]);
					if (mp.Name != string.Empty)
						al.Add(mp);
                }
            }
            finally 
            {
                m_importer.CloseEnum(hEnum);
            }
            return (ParameterInfo[]) al.ToArray(typeof(ParameterInfo));
        }

        public override int MetadataToken
        {
            get 
            {
                return m_methodToken;
            }
        }

        public string[] GetGenericArgumentNames() 
        {
            return MetadataHelperFunctions.GetGenericArgumentNames(m_importer,m_methodToken);
        }

        private IMetadataImport m_importer;
        private string m_name;
        private int m_classToken;
        private int m_methodToken;
        private MethodAttributes m_methodAttributes;
		// [Xamarin] Expression evaluator.
		private List<Type> m_argTypes;
		private Type m_retType;
	}

    public enum MetadataTokenType
    {
        Module = 0x00000000,       
        TypeRef              = 0x01000000,                 
        TypeDef              = 0x02000000,       
        FieldDef             = 0x04000000,       
        MethodDef            = 0x06000000,       
        ParamDef             = 0x08000000,       
        InterfaceImpl        = 0x09000000,       
        MemberRef            = 0x0a000000,       
        CustomAttribute      = 0x0c000000,       
        Permission           = 0x0e000000,       
        Signature            = 0x11000000,       
        Event                = 0x14000000,       
        Property             = 0x17000000,       
        ModuleRef            = 0x1a000000,       
        TypeSpec             = 0x1b000000,       
        Assembly             = 0x20000000,       
        AssemblyRef          = 0x23000000,       
        File                 = 0x26000000,       
        ExportedType         = 0x27000000,       
        ManifestResource     = 0x28000000,       
        GenericPar           = 0x2a000000,       
        MethodSpec           = 0x2b000000,       
        String               = 0x70000000,       
        Name                 = 0x71000000,       
        BaseType             = 0x72000000, 
        Invalid              = 0x7FFFFFFF, 
    }

    public enum CorCallingConvention
    {
        Default       = 0x0,

        VarArg        = 0x5,
        Field         = 0x6,
        LocalSig     = 0x7,
        Property      = 0x8,
        Unmanaged         = 0x9,
        GenericInst   = 0xa,  // generic method instantiation
        NativeVarArg  = 0xb,  // used ONLY for 64bit vararg PInvoke calls

            // The high bits of the calling convention convey additional info
        Mask      = 0x0f,  // Calling convention is bottom 4 bits
        HasThis   = 0x20,  // Top bit indicates a 'this' parameter
        ExplicitThis = 0x40,  // This parameter is explicitly in the signature
        Generic   = 0x10,  // Generic method sig with explicit number of type arguments (precedes ordinary parameter count)
    };



    
    // Struct isn't complete yet; just here for the IsTokenOfType method
    
    public struct MetadataToken
    {
        public MetadataToken(int value)
        {
            this.value = value;
        }

        public int Value
        {
            get
            {
                return value;
            }
        }

        public MetadataTokenType Type
        {
            get
            {
                return (MetadataTokenType)(value & 0xFF000000);
            }
        }

        public int Index
        {
            get
            {
                return value & 0x00FFFFFF;
            }
        }
        
        public static implicit operator int(MetadataToken token) { return token.value; }
        public static bool operator==(MetadataToken v1, MetadataToken v2) { return (v1.value == v2.value);}
        public static bool operator!=(MetadataToken v1, MetadataToken v2) { return !(v1 == v2);}

        public static bool IsTokenOfType(int token, params MetadataTokenType[] types)
        {
            for (int i = 0; i < types.Length; i++)
            {
                if ((int)(token & 0xFF000000) == (int)types[i])
                    return true;
            }

            return false;
        }

        public bool IsOfType(params MetadataTokenType[] types) { return IsTokenOfType(Value, types); }

        public override bool Equals(object other)
        {
            if (other is MetadataToken)
            {
                MetadataToken oToken = (MetadataToken)other;
                return (value == oToken.value);
            }
            return false;
        }

        public override int GetHashCode() { return value.GetHashCode();}

        private int value;
    }

    static class MetadataHelperFunctions
    {
        private static uint TokenFromRid(uint rid, uint tktype) {return (rid) | (tktype);}

		// [Xamarin] Expression evaluator.
		public static void ReadMethodSignature (IMetadataImport importer, ref IntPtr pData, out CorCallingConvention cconv, out Type retType, out List<Type> argTypes)
		{
			cconv = MetadataHelperFunctions.CorSigUncompressCallingConv (ref pData);
			uint numArgs = 0;
			// FIXME: Use number of <T>s.
			uint types = 0;
			if ((cconv & CorCallingConvention.Generic) == CorCallingConvention.Generic)
				types = MetadataHelperFunctions.CorSigUncompressData (ref pData);

			if (cconv != CorCallingConvention.Field)
				numArgs = MetadataHelperFunctions.CorSigUncompressData (ref pData);

			retType = MetadataHelperFunctions.ReadType (importer, ref pData);
			argTypes = new List<Type> ();
			for (int n = 0; n < numArgs; n++)
				argTypes.Add (MetadataHelperFunctions.ReadType (importer, ref pData));
		}

		// [Xamarin] Expression evaluator.
		class GenericType
		{
			// Used as marker for generic method args
		}

		// [Xamarin] Expression evaluator.
		static Type ReadType (IMetadataImport importer, ref IntPtr pData)
		{
			CorElementType et;
			unsafe {
				byte* pBytes = (byte*)pData;
				et = (CorElementType) (*pBytes);
				pData = (IntPtr) (pBytes + 1);
			}

			switch (et)
			{
				case CorElementType.ELEMENT_TYPE_VOID: return typeof (void);
				case CorElementType.ELEMENT_TYPE_BOOLEAN: return typeof (bool);
				case CorElementType.ELEMENT_TYPE_CHAR: return typeof (char);
				case CorElementType.ELEMENT_TYPE_I1: return typeof (sbyte);
				case CorElementType.ELEMENT_TYPE_U1: return typeof (byte);
				case CorElementType.ELEMENT_TYPE_I2: return typeof (short);
				case CorElementType.ELEMENT_TYPE_U2: return typeof (ushort);
				case CorElementType.ELEMENT_TYPE_I4: return typeof (int);
				case CorElementType.ELEMENT_TYPE_U4: return typeof (uint);
				case CorElementType.ELEMENT_TYPE_I8: return typeof (long);
				case CorElementType.ELEMENT_TYPE_U8: return typeof (ulong);
				case CorElementType.ELEMENT_TYPE_R4: return typeof (float);
				case CorElementType.ELEMENT_TYPE_R8: return typeof (double);
				case CorElementType.ELEMENT_TYPE_STRING: return typeof (string);
				case CorElementType.ELEMENT_TYPE_I: return typeof (IntPtr);
				case CorElementType.ELEMENT_TYPE_U: return typeof (UIntPtr);
				case CorElementType.ELEMENT_TYPE_OBJECT: return typeof (object);

				case CorElementType.ELEMENT_TYPE_VAR:
				case CorElementType.ELEMENT_TYPE_MVAR:
					// Generic args in methods not supported. Return a dummy type.
					CorSigUncompressData (ref pData);
					return typeof(GenericType);

				case CorElementType.ELEMENT_TYPE_GENERICINST: {
					Type t = ReadType (importer, ref pData);
					List<Type> typeArgs = new List<Type> ();
					uint num = CorSigUncompressData (ref pData);
					for (int n=0; n<num; n++) {
						typeArgs.Add (ReadType (importer, ref pData));
					}
					return MetadataType.MakeGeneric (t, typeArgs);
				}

				case CorElementType.ELEMENT_TYPE_PTR: {
						Type t = ReadType (importer, ref pData);
						return MetadataType.MakePointer (t);
					}

				case CorElementType.ELEMENT_TYPE_BYREF: {
						Type t = ReadType (importer, ref pData);
						return MetadataType.MakeByRef(t);
					}

				case CorElementType.ELEMENT_TYPE_END:
				case CorElementType.ELEMENT_TYPE_VALUETYPE:
				case CorElementType.ELEMENT_TYPE_CLASS: {
						uint token = CorSigUncompressToken (ref pData);
						return new MetadataType (importer, (int) token);
					}

				case CorElementType.ELEMENT_TYPE_ARRAY: {
						Type t = ReadType (importer, ref pData);
						int rank = (int)CorSigUncompressData (ref pData);
						if (rank == 0)
							return MetadataType.MakeArray (t, null, null);

						uint numSizes = CorSigUncompressData (ref pData);
						var sizes = new List<int> (rank);
						for (int n = 0; n < numSizes && n < rank; n++)
							sizes.Add ((int)CorSigUncompressData (ref pData));

						uint numLoBounds = CorSigUncompressData (ref pData);
						var loBounds = new List<int> (rank);
						for (int n = 0; n < numLoBounds && n < rank; n++)
							loBounds.Add ((int)CorSigUncompressData (ref pData));

						return MetadataType.MakeArray (t, sizes, loBounds);
					}

				case CorElementType.ELEMENT_TYPE_SZARRAY: {
						Type t = ReadType (importer, ref pData);
						return MetadataType.MakeArray (t, null, null);
					}

				case CorElementType.ELEMENT_TYPE_FNPTR: {
						CorCallingConvention cconv;
						Type retType;
						List<Type> argTypes;
						ReadMethodSignature (importer, ref pData, out cconv, out retType, out argTypes);
						return MetadataType.MakeDelegate (retType, argTypes);
					}

				case CorElementType.ELEMENT_TYPE_CMOD_REQD:
				case CorElementType.ELEMENT_TYPE_CMOD_OPT:
						return ReadType (importer, ref pData);
			}
			throw new NotSupportedException ("Unknown sig element type: " + et);
		}

        // The below have been translated manually from the inline C++ helpers in cor.h
        
        internal static uint CorSigUncompressBigData(
            ref IntPtr pData)             // [IN,OUT] compressed data 
        {
            unsafe
            {
                byte *pBytes = (byte*)pData;
                uint res;

                // 1 byte data is handled in CorSigUncompressData   
                //  Debug.Assert(*pBytes & 0x80);    

                // Medium.  
                if ((*pBytes & 0xC0) == 0x80)  // 10?? ????  
                {   
                    res = (uint)((*pBytes++ & 0x3f) << 8);
                    res |= *pBytes++;
                }   
                else // 110? ???? 
                {
                    res = (uint)(*pBytes++ & 0x1f) << 24;
                    res |= (uint)(*pBytes++) << 16;
                    res |= (uint)(*pBytes++) << 8;
                    res |= (uint)(*pBytes++);
                }
                
                pData = (IntPtr)pBytes;
                return res; 
            }
        }

        internal static uint CorSigUncompressData(
            ref IntPtr pData)             // [IN,OUT] compressed data 
        {
            unsafe
            {
                byte *pBytes = (byte*)pData;
                
                // Handle smallest data inline. 
                if ((*pBytes & 0x80) == 0x00)        // 0??? ????    
                {
                    uint retval = (uint)(*pBytes++);
                    pData = (IntPtr)pBytes;
                    return retval;
                }
                return CorSigUncompressBigData(ref pData);  
            }
        }

// Function translated directly from cor.h but never tested; included here in case someone wants to use it in future
/*        internal static uint CorSigUncompressData(      // return number of bytes of that compressed data occupied in pData 
            IntPtr pData,              // [IN] compressed data 
            out uint pDataOut)              // [OUT] the expanded *pData    
        {   
            unsafe
            {
                uint       cb = 0xffffffff;    
                byte *pBytes = (byte*)(pData); 
                pDataOut = 0;

                // Smallest.    
                if ((*pBytes & 0x80) == 0x00)       // 0??? ????    
                {   
                    pDataOut = *pBytes;    
                    cb = 1; 
                }   
                // Medium.  
                else if ((*pBytes & 0xC0) == 0x80)  // 10?? ????    
                {   
                    pDataOut = (uint)(((*pBytes & 0x3f) << 8 | *(pBytes+1)));  
                    cb = 2; 
                }   
                else if ((*pBytes & 0xE0) == 0xC0)      // 110? ????    
                {   
                    pDataOut = (uint)(((*pBytes & 0x1f) << 24 | *(pBytes+1) << 16 | *(pBytes+2) << 8 | *(pBytes+3)));  
                    cb = 4; 
                }   
                return cb;  
            }
        }*/

        static uint[] g_tkCorEncodeToken ={(uint)MetadataTokenType.TypeDef, (uint)MetadataTokenType.TypeRef, (uint)MetadataTokenType.TypeSpec, (uint)MetadataTokenType.BaseType};

        // uncompress a token
        internal static uint CorSigUncompressToken(   // return the token.    
            ref IntPtr pData)             // [IN,OUT] compressed data 
        {
            uint     tk; 
            uint     tkType; 

            tk = CorSigUncompressData(ref pData);   
            tkType = g_tkCorEncodeToken[tk & 0x3];  
            tk = TokenFromRid(tk >> 2, tkType); 
            return tk;  
        }


// Function translated directly from cor.h but never tested; included here in case someone wants to use it in future
/*        internal static uint CorSigUncompressToken(     // return number of bytes of that compressed data occupied in pData 
            IntPtr pData,              // [IN] compressed data 
            out uint     pToken)                // [OUT] the expanded *pData    
        {
            uint       cb; 
            uint     tk; 
            uint     tkType; 

            cb = CorSigUncompressData(pData, out tk); 
            tkType = g_tkCorEncodeToken[tk & 0x3];  
            tk = TokenFromRid(tk >> 2, tkType); 
            pToken = tk;   
            return cb;  
        }*/

        internal static CorCallingConvention CorSigUncompressCallingConv(
            ref IntPtr pData)             // [IN,OUT] compressed data 
        {
            unsafe
            {
                byte *pBytes = (byte*) pData;
                CorCallingConvention retval = (CorCallingConvention)(*pBytes++);
                pData = (IntPtr)pBytes;
                return retval;
            }
        }

// Function translated directly from cor.h but never tested; included here in case someone wants to use it in future
/*        private enum SignMasks : uint {
            ONEBYTE  = 0xffffffc0,        // Mask the same size as the missing bits.  
            TWOBYTE  = 0xffffe000,        // Mask the same size as the missing bits.  
            FOURBYTE = 0xf0000000,        // Mask the same size as the missing bits.  
        };

        // uncompress a signed integer
        internal static uint CorSigUncompressSignedInt( // return number of bytes of that compressed data occupied in pData
            IntPtr pData,              // [IN] compressed data 
            out int         pInt)                  // [OUT] the expanded *pInt 
        {
            uint       cb; 
            uint       ulSigned;   
            uint       iData;  

            cb = CorSigUncompressData(pData, out iData);
            pInt = 0;
            if (cb == 0xffffffff) return cb;
            ulSigned = iData & 0x1; 
            iData = iData >> 1; 
            if (ulSigned != 0)   
            {   
                if (cb == 1)    
                {   
                    iData |= (uint)SignMasks.ONEBYTE; 
                }   
                else if (cb == 2)   
                {   
                    iData |= (uint)SignMasks.TWOBYTE; 
                }   
                else    
                {   
                    iData |= (uint)SignMasks.FOURBYTE;    
                }   
            }   
            pInt = (int)iData;  
            return cb;  
        }*/


        // uncompress encoded element type
        internal static CorElementType CorSigUncompressElementType(//Element type
            ref IntPtr pData)             // [IN,OUT] compressed data 
        {
            unsafe
            {
                byte *pBytes = (byte*)pData;

                CorElementType retval = (CorElementType)(*pBytes++);
                pData = (IntPtr)pBytes;
                return retval;
            }
        }

// Function translated directly from cor.h but never tested; included here in case someone wants to use it in future
/*        internal static uint CorSigUncompressElementType(// return number of bytes of that compressed data occupied in pData
            IntPtr pData,              // [IN] compressed data 
            out CorElementType pElementType)       // [OUT] the expanded *pData    
        {  
            unsafe
            {
                byte *pBytes = (byte*)pData;
                pElementType = (CorElementType)(*pBytes & 0x7f);    
                return 1;   
            }
        }*/
        
        static internal string[] GetGenericArgumentNames(IMetadataImport importer,
                                                int typeOrMethodToken) 
        {          
            IMetadataImport2 importer2 = (importer as IMetadataImport2);
            if(importer2 == null)
                return new string[0]; // this means we're pre v2.0 debuggees.
            
            Debug.Assert( importer2!=null );

            string[] genargs = null;
            
            IntPtr hEnum = IntPtr.Zero;
            try{
                int i=0;
                do{
                    uint nOut;
                    int genTypeToken;
                    importer2.EnumGenericParams(ref hEnum, typeOrMethodToken, 
                                                out genTypeToken, 1, out nOut);
                    if( genargs==null )
                    {
                        int count;
                        importer.CountEnum(hEnum, out count);
                        genargs = new string[count];
                    }
                    if( nOut==0 )
                        break;

                    Debug.Assert( nOut==1 );
                    if( nOut==1 )
                    {
                        uint genIndex;
                        int genFlags, ptkOwner, ptkKind;
                        ulong genArgNameSize;

                        importer2.GetGenericParamProps(genTypeToken,
                                                       out genIndex,
                                                       out genFlags,
                                                       out ptkOwner,
                                                       out ptkKind,
                                                       null,
                                                       0,
                                                       out genArgNameSize);
                        StringBuilder genArgName = new StringBuilder((int)genArgNameSize);
                        importer2.GetGenericParamProps(genTypeToken,
                                                       out genIndex,
                                                       out genFlags,
                                                       out ptkOwner,
                                                       out ptkKind,
                                                       genArgName,
                                                       (ulong)genArgName.Capacity,
                                                       out genArgNameSize);

                        genargs[i] = genArgName.ToString();
                    }
                    ++i;
                } while( i<genargs.Length );
            }
            finally
            {
                if( hEnum != IntPtr.Zero )
                    importer2.CloseEnum(hEnum);
            }
            return genargs;
        }

		// [Xamarin] Expression evaluator.
		static object[] emptyAttributes = new object[0];

		static internal object[] GetDebugAttributes (IMetadataImport importer, int token)
		{
			ArrayList attributes = new ArrayList ();
			object attr = MetadataHelperFunctions.GetCustomAttribute (importer, token, typeof (System.Diagnostics.DebuggerTypeProxyAttribute));
			if (attr != null)
				attributes.Add (attr);
			attr = MetadataHelperFunctions.GetCustomAttribute (importer, token, typeof (System.Diagnostics.DebuggerDisplayAttribute));
			if (attr != null)
				attributes.Add (attr);
			attr = MetadataHelperFunctions.GetCustomAttribute (importer, token, typeof (System.Diagnostics.DebuggerBrowsableAttribute));
			if (attr != null)
				attributes.Add (attr);
			attr = MetadataHelperFunctions.GetCustomAttribute (importer, token, typeof (System.Runtime.CompilerServices.CompilerGeneratedAttribute));
			if (attr != null)
				attributes.Add (attr);

			if (attributes.Count == 0)
				return emptyAttributes;
			else
				return attributes.ToArray ();
		}

		// [Xamarin] Expression evaluator.
		static internal object GetCustomAttribute (IMetadataImport importer, int token, Type type)
		{
			uint sigSize = 0;
			IntPtr ppvSig = IntPtr.Zero;
			int hr = importer.GetCustomAttributeByName (token, type.FullName, out ppvSig, out sigSize);
			if (hr != 0)
				return null;

			byte[] data = new byte[sigSize];
			Marshal.Copy (ppvSig, data, 0, (int)sigSize);
			BinaryReader br = new BinaryReader (new MemoryStream (data));

			// Prolog
			if (br.ReadUInt16 () != 1)
				throw new InvalidOperationException ("Incorrect attribute prolog");

			ConstructorInfo ctor = type.GetConstructors ()[0];
			ParameterInfo[] pars = ctor.GetParameters ();

			object[] args = new object[pars.Length];

			// Fixed args
			for (int n=0; n<pars.Length; n++)
				args [n] = ReadValue (br, pars[n].ParameterType);

			object ob = Activator.CreateInstance (type, args);
			
			// Named args
			uint nargs = br.ReadUInt16 ();
			for (; nargs > 0; nargs--) {
				byte fieldOrProp = br.ReadByte ();
				byte atype = br.ReadByte ();

				// Boxed primitive
				if (atype == 0x51)
					atype = br.ReadByte ();
				CorElementType et = (CorElementType) atype;
				string pname = br.ReadString ();
				object val = ReadValue (br, CorMetadataImport.CoreTypes[et]);

				if (fieldOrProp == 0x53) {
					FieldInfo fi = type.GetField (pname);
					fi.SetValue (ob, val);
				}
				else {
					PropertyInfo pi = type.GetProperty (pname);
					pi.SetValue (ob, val, null);
				}
			}
			return ob;
		}

		// [Xamarin] Expression evaluator.
		static object ReadValue (BinaryReader br, Type type)
		{
			if (type.IsEnum) {
				object ob = ReadValue (br, Enum.GetUnderlyingType (type));
				return Enum.ToObject (type, Convert.ToInt64 (ob));
			}
			if (type == typeof (string) || type == typeof(Type))
				return br.ReadString ();
			if (type == typeof (int))
				return br.ReadInt32 ();
			throw new InvalidOperationException ("Can't parse value of type: " + type);
		}
	}
} // namspace Microsoft.Debugger.MetadataWrapper
