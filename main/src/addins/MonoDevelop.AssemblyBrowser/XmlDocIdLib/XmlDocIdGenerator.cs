using System;
using System.Collections.Generic;
using System.Text;

using Mono.Cecil;

namespace XmlDocIdLib
{
    #region XmlDocIdGenerator
	class XmlDocIdGenerator
    {
        #region Constructors
        public XmlDocIdGenerator()
        {
            this.m_compat = CompatibilityType.Net35;
        }
        #endregion

        #region Public methods
        public string GetXmlDocPath(
            MemberReference Member)
        {
            if (Member == null)
                throw new ArgumentNullException("Member");

            StringBuilder stbBuilder = new StringBuilder();
            List<string> Path = new List<string>();

            // get path
            GetXmlDocPathRecursive(Member, Path);

            // generate string
            if (Path.Count == 0)
                return string.Empty;

            foreach (string strTemp in Path)
                stbBuilder.Append(strTemp);

            return stbBuilder.ToString();
        }

        public void SetCompatibilityType(
            CompatibilityType Compatibility)
        {
            if (Compatibility == CompatibilityType.None)
                throw new ArgumentException("Invalid parameter value.");

            this.m_compat = Compatibility;
        }

        public CompatibilityType GetCompatibilityType()
        {
            return this.m_compat;
        }
        #endregion

        #region Private methods
        private string GetXmlDocExplicitIfaceImplPath(
            MemberReference Member)
        {
            TypeReference declaringTypeRef = null;
            TypeDefinition declaringTypeDef = null;
            string strPath = string.Empty;

            if (Member.DeclaringType is GenericInstanceType)
                declaringTypeRef = (Member.DeclaringType as GenericInstanceType).ElementType;
            else
                declaringTypeRef = Member.DeclaringType;

            // lookup TypeDefinition for TypeReference
            declaringTypeDef = TryLookUpTypeDefinition(declaringTypeRef);

            if (declaringTypeDef == null || declaringTypeDef.IsInterface)
                return string.Empty;

            foreach (InterfaceImplementation tempIface in declaringTypeDef.Interfaces)
            {
                var tempIfaceRef = tempIface.InterfaceType;
                // check whether this member name begins with interface name (plus generic arguments)
                if (Member.Name.StartsWith(this.StripInterfaceName(tempIfaceRef.FullName)))
                {
                    // element begins with interface name, this is explicit interface implementation,
                    // get explicit interface implementation path

                    // add member's name to path, at this point
                    // name contains interface name (with generic arguments) plus member name
                    strPath = Member.Name;

                    // remove text between "<" and ">" and put interface parameters 
                    // (in explicit mode of course)
                    int LeftBrace = strPath.IndexOf("<");
                    int RightBrace = strPath.LastIndexOf(">");

                    if (LeftBrace != -1 && RightBrace != -1)
                    {
                        bool firstAppend = true;
                        GenericInstanceType tempGenericIfaceDef = null;
                        StringBuilder stbParameters = new StringBuilder();

                        // convert to definition
                        tempGenericIfaceDef = tempIfaceRef as GenericInstanceType;

                        if (tempGenericIfaceDef == null)
                            break;

                        strPath = strPath.Remove(LeftBrace, (RightBrace - LeftBrace) + 1);
                        stbParameters.Append("{");
                        foreach (TypeReference tempParam in tempGenericIfaceDef.GenericArguments)
                        {
                            // in "explicit" mode "@" is used as a separator instead of ","
                            // in "normal" mode
                            if (!firstAppend)
                                stbParameters.Append(CanAppendSpecialExplicitChar() ? "@" : ",");

                            GetXmlDocParameterPathRecursive(tempParam, true, stbParameters);
                            firstAppend = false;
                        }
                        stbParameters.Append("}");

                        // insert                             
                        strPath = strPath.Insert(LeftBrace, stbParameters.ToString());
                    }

                    // replace "." with "#"
                    if (CanAppendSpecialExplicitChar())
                        strPath = strPath.Replace(".", "#");

                    return strPath;
                }
            }

            return string.Empty;
        }

        private TypeDefinition TryLookUpTypeDefinition(
            TypeReference Reference)
        {
            // try find in the current assembly
            foreach (TypeDefinition tempTypeDef in Reference.Module.Types)
                if (tempTypeDef.ToString() == Reference.ToString())
                    return tempTypeDef;

            return null;
        }

        private string StripInterfaceName(
            string OrginalName)
        {
            StringBuilder builderStrippedName = new StringBuilder();

            // split name 
            string[] strSlices = OrginalName.Split(new char[] { '`' });

            // remove numbers at the begining of each string to "<" charter
            if (strSlices.Length > 1)
                for (int i = 0; i < strSlices.Length; i++)
                    if (strSlices[i].Contains("<"))
                        strSlices[i] = strSlices[i].Remove(0, strSlices[i].IndexOf("<"));

            // build string
            foreach (string tempString in strSlices)
                builderStrippedName.Append(tempString);

            return builderStrippedName.ToString();
        }

        private void GetXmlDocPathRecursive(
            MemberReference Member,
            List<string> CurrPath)
        {
            /*
             * determine type of the current member, if current path is empty
             * we have also to insert to path element type:
             * - "N:" - for namespace (not used here)
             * - "T:" - for a type (class, structure, delegate)
             * - "M:" - for a method (or constructor)
             * - "F:" - for a field
             * - "P:" - for a property or indexer
             * - "E:" - for an event
             */

            StringBuilder stbTempPath = new StringBuilder();
            string strExplicitPath = string.Empty;

            if (Member is TypeReference)
            {
                TypeReference thisTypeRef = null;
                GenericInstanceType thisGenericTypeDef = null;
                GenericParameter thisGenericParam = null;
                string strTempTypeName = string.Empty;

                if (Member is GenericInstanceType)
                {
                    thisGenericTypeDef = Member as GenericInstanceType;
                    thisTypeRef = thisGenericTypeDef.ElementType;
                }
                else if (Member is GenericParameter)
                {
                    thisGenericParam = Member as GenericParameter;
                    CurrPath.Add("`" + thisGenericParam.Position.ToString());

                    // return immediatelly, because there is nothing to do.
                    return;
                }
                else
                {
                    // cast to TypeReference
                    thisTypeRef = Member as TypeReference;
                }

                // if nested, scan enclosing type 
                if (this.IsNested(thisTypeRef))
                    GetXmlDocPathRecursive(Member.DeclaringType, CurrPath);

                // determine namespace
                string strNamespace = string.Empty;
                if ((thisTypeRef.Namespace != null && thisTypeRef.Namespace.Length > 0) || thisTypeRef.DeclaringType != null)
                    strNamespace = thisTypeRef.Namespace + ".";

                // remove "`" char or not
                string strTempShortTypeName = thisTypeRef.Name;
                if (thisTypeRef.Name.Contains("`") && thisGenericTypeDef != null)
                    strTempShortTypeName = thisTypeRef.Name.Remove(thisTypeRef.Name.IndexOf("`"));

                // class, interface, structure or delegate
                if (CurrPath.Count == 0)
                    strTempTypeName = "T:" + strNamespace + strTempShortTypeName;
                else if (CurrPath.Count > 0 && !this.IsNested(thisTypeRef))
                    strTempTypeName = strNamespace + strTempShortTypeName;
                else
                    strTempTypeName = "." + strTempShortTypeName;

                CurrPath.Add(strTempTypeName);

                // add generic _arguments_ (not parameters !)
                if (thisTypeRef.Name.Contains("`") && thisGenericTypeDef != null)
                {
                    bool firstAppend = true;

                    // open bracket
                    CurrPath.Add("{");

                    foreach (TypeReference tempGenArgument in thisGenericTypeDef.GenericArguments)
                    {
                        // add comma
                        if (!firstAppend)
                            CurrPath.Add(",");

                        // add argument's xmlDocPath
                        GetXmlDocPathRecursive(tempGenArgument as MemberReference, CurrPath);

                        // first append done
                        firstAppend = false;
                    }

                    // close bracket
                    CurrPath.Add("}");
                }
            }
			else if (Member is MethodReference)
            {
				var thisMethodDef = Member as MethodReference;

                // method, get type's path firstAppend
                CurrPath.Add("M:");
                if (Member.DeclaringType != null)
                    GetXmlDocPathRecursive(Member.DeclaringType, CurrPath);

                // method's path
                // check whether this is constructor method, or explicitly implemented method
                strExplicitPath = GetXmlDocExplicitIfaceImplPath(Member);

				//if (thisMethodDef.IsStatic && thisMethodDef.IsConstructor)
				//    stbTempPath.Append(".#cctor");
				//if (!thisMethodDef.IsStatic && thisMethodDef.IsConstructor)
				//    stbTempPath.Append(".#ctor");
				stbTempPath.Append (".");
				if (strExplicitPath.Length > 0)
					stbTempPath.Append (strExplicitPath);
                else
					stbTempPath.Append (thisMethodDef.Name);

                // check whether this method is generic
                if (thisMethodDef.GenericParameters.Count > 0)
					stbTempPath.Append("``").Append (thisMethodDef.GenericParameters.Count);

                if (thisMethodDef.Parameters.Count > 0)
                    stbTempPath.Append("(");
                bool firstAppend = true;
                foreach (ParameterDefinition TempParam in thisMethodDef.Parameters)
                {
                    if (!firstAppend)
                        stbTempPath.Append(",");

                    stbTempPath.Append(GetXmlDocParameterPath(TempParam.ParameterType, false));
                    firstAppend = false;
                }

                if (thisMethodDef.Parameters.Count > 0)
                    stbTempPath.Append(")");

                // check whether this is a conversion operator (implicit or explicit)
                // if so, we have to read return type and add "~" char.
                //if (IsOperator(thisMethodDef))
                //{
                //    OperatorType OpType = GetOperatorType(thisMethodDef);

                //    if (OpType == OperatorType.op_Implicit || OpType == OperatorType.op_Explicit)
                //    {
                //        // add return type parameter path
                //        stbTempPath.Append("~");
                //        stbTempPath.Append(GetXmlDocParameterPath(thisMethodDef.ReturnType, false));
                //    }
                //}

                // add to path
                CurrPath.Add(stbTempPath.ToString());
            }
			else if (Member is FieldReference)
            {
                // field, get type's path name
                CurrPath.Add("F:");
                if (Member.DeclaringType != null)
                    GetXmlDocPathRecursive(Member.DeclaringType, CurrPath);

                // field's path
                CurrPath.Add("." + Member.Name);
            }
			else if (Member is PropertyReference)
            {
                // property or indexer, get declaring type's path 
                CurrPath.Add("P:");
                if (Member.DeclaringType != null)
                    GetXmlDocPathRecursive(Member.DeclaringType, CurrPath);

                // property's path
                // check whether this is explicitly implemented property
                strExplicitPath = GetXmlDocExplicitIfaceImplPath(Member);
				stbTempPath.Append (".");
                if (strExplicitPath.Length > 0)
                    stbTempPath.Append (strExplicitPath);
                else
                    stbTempPath.Append (Member.Name);

                // is it an indexer ?
                bool firstAppend = true;
                PropertyDefinition piProperty = Member as PropertyDefinition;
                if (piProperty.Parameters.Count > 0)
                    stbTempPath.Append("(");

                foreach (ParameterDefinition TempParam in piProperty.Parameters)
                {
                    if (!firstAppend)
                        stbTempPath.Append(",");

                    stbTempPath.Append(GetXmlDocParameterPath(TempParam.ParameterType, false));
                    firstAppend = false;
                }

                if (piProperty.Parameters.Count > 0)
                    stbTempPath.Append(")");

                CurrPath.Add(stbTempPath.ToString());
            }
			else if (Member is EventReference)
            {
                // event, get type's path firstAppend
                CurrPath.Add("E:");
                if (Member.DeclaringType != null)
                    GetXmlDocPathRecursive(Member.DeclaringType, CurrPath);

                // event's path
                CurrPath.Add("." + Member.Name);
            }
        }

        private string GetXmlDocParameterPath(
            TypeReference Type,
            bool ExplicitMode)
        {
            StringBuilder stbCurrPath = new StringBuilder();

            GetXmlDocParameterPathRecursive(Type, ExplicitMode, stbCurrPath);

            return stbCurrPath.ToString();
        }

        private void GetXmlDocParameterPathRecursive(
            TypeReference tpType,
            bool ExplicitMode,
            StringBuilder CurrPath)
        {
            if (tpType == null)
                return;

            if (tpType.GenericParameters.Count > 0)
            {
                CurrPath.Append(
                    tpType.Namespace +
                    ((CanAppendSpecialExplicitChar() && ExplicitMode) ? "#" : ".") +
                    StripGenericName(tpType.Name));

                // list parameters or types
                bool firstAppend = true;
                CurrPath.Append("{");
                foreach (GenericParameter TempType in tpType.GenericParameters)
                {
                    if (!firstAppend)
                        CurrPath.Append(",");

                    CurrPath.Append(GetXmlDocParameterPath(TempType, ExplicitMode));
                    firstAppend = false;
                }
                CurrPath.Append("}");
            }
            else if (tpType is GenericInstanceType)
            {
                GenericInstanceType thisGenericType = tpType as GenericInstanceType;

                // if nested, scan enclosing type
                if (tpType.DeclaringType != null)
                    CurrPath.Append(GetXmlDocParameterPath(tpType.DeclaringType, ExplicitMode));

                // determine namespace
                string strNamespace = string.Empty;
                if ((tpType.Namespace != null && tpType.Namespace.Length > 0) || tpType.DeclaringType != null)
                {
                    strNamespace = tpType.Namespace +
                        ((CanAppendSpecialExplicitChar() && ExplicitMode) ? "#" : ".");
                }

				CurrPath.Append(strNamespace).Append (StripGenericName(thisGenericType.Name));

                // list parameters or types
                bool firstAppend = true;
                CurrPath.Append("{");
                foreach (TypeReference tempTypeRef in thisGenericType.GenericArguments)
                {
                    if (!firstAppend)
                        CurrPath.Append(",");

                    CurrPath.Append(GetXmlDocParameterPath(tempTypeRef, ExplicitMode));
                    firstAppend = false;
                }
                CurrPath.Append("}");
            }
            else if (tpType is GenericParameter)
            {
                GenericParameter thisGenParam = tpType as GenericParameter;

                if (ExplicitMode)
                {
                    // in explicit mode we print parameter name
                    CurrPath.Append(thisGenParam.Name);
                }
                else
                {
                    // in non-explicit mode we print parameter order
                    int paramOrder = 0;

                    // find
                    for (int i = 0; i < thisGenParam.Owner.GenericParameters.Count; i++)
                    {
                        if (thisGenParam.Owner.GenericParameters[i].Name == tpType.Name)
                        {
                            paramOrder = i;
                            break;
                        }
                    }
                    if (thisGenParam.Owner is MethodReference)
						CurrPath.Append("``").Append (paramOrder);
                    else
						CurrPath.Append("`").Append (paramOrder);
                }
            }
            else if (tpType is PointerType)
            {
                // parameter is pointer type
                CurrPath.Append(GetXmlDocParameterPath((tpType as PointerType).ElementType, ExplicitMode));
                CurrPath.Append("*");
            }
            else if (tpType is ArrayType)
            {
                ArrayType thisArrayType = tpType as ArrayType;
                if (thisArrayType.ElementType != null)
                    CurrPath.Append(GetXmlDocParameterPath(thisArrayType.ElementType, ExplicitMode));

                int iRank = thisArrayType.Rank;
                if (iRank == 1)
                {
                    CurrPath.Append("[]");
                }
                else
                {
                    bool firstAppend = true;
                    CurrPath.Append("[");

                    for (int i = 0; i < (ExplicitMode ? iRank - 1 : iRank); i++)
                    {
                        // in explicit mode for .NET3.5/VS2008, 
                        // there is no separator char "," used for multi-dimensional array,
                        // so there are three cases when comma shall be added:
                        // firstAppend = false; ExplicitMode = false; CanAppendSpecialExplicitChar() = true;
                        // firstAppend = false; ExplicitMode = false; CanAppendSpecialExplicitChar() = false;
                        // firstAppend = false; ExplicitMode = true; CanAppendSpecialExplicitChar() = false;
                        // below this is stored in decent manner
                        if (!firstAppend && (!ExplicitMode || !CanAppendSpecialExplicitChar()))
                            CurrPath.Append(",");

                        CurrPath.Append(((CanAppendSpecialExplicitChar() && ExplicitMode) ? "@" : "0:"));
                        if (thisArrayType.Dimensions[i].UpperBound > 0)
                            CurrPath.Append(thisArrayType.Dimensions[i].UpperBound.ToString());
                        firstAppend = false;
                    }

                    CurrPath.Append("]");
                }
            }
//            else if (!tpType.IsValueType)
//            {
//                // parameter is passed by reference
//                CurrPath.Append(GetXmlDocParameterPath((tpType as ReferenceType).ElementType, false));
//                CurrPath.Append("@");
//            }
//            else if (tpType is ModifierOptional)
//            {
//                // parameter has optional modifier
//                ModifierOptional thisModOpt = tpType as ModifierOptional;
//
//                CurrPath.Append(GetXmlDocParameterPath(thisModOpt.ElementType, ExplicitMode));
//                CurrPath.Append("!");
//                CurrPath.Append(GetXmlDocParameterPath(thisModOpt.ModifierType, ExplicitMode));
//            }
//            else if (tpType is ModifierRequired)
//            {
//                // parameter has required modifier
//                ModifierRequired thisModReq = tpType as ModifierRequired;
//
//                CurrPath.Append(GetXmlDocParameterPath(thisModReq.ElementType, ExplicitMode));
//                CurrPath.Append("|");
//                CurrPath.Append(GetXmlDocParameterPath(thisModReq.ModifierType, ExplicitMode));
//            }
            else if (tpType is FunctionPointerType)
            {
                // type is function pointer
                FunctionPointerType thisFuncPtr = tpType as FunctionPointerType;
//                string tempString = string.Empty;

                // return type
                CurrPath.Append("=FUNC:");
                CurrPath.Append(GetXmlDocParameterPath(thisFuncPtr.ReturnType, ExplicitMode));

                // method's parameters
                if (thisFuncPtr.Parameters.Count > 0)
                {
                    bool firstAppend = true;
                    CurrPath.Append("(");

                    foreach (ParameterDefinition tempParam in thisFuncPtr.Parameters)
                    {
                        if (!firstAppend)
                            CurrPath.Append(",");

                        CurrPath.Append(GetXmlDocParameterPath(tempParam.ParameterType, ExplicitMode));
                        firstAppend = false;
                    }

                    CurrPath.Append(")");
                }
                else
                {
                    CurrPath.Append("(System.Void)");
                }
            }
            else if (tpType is PinnedType)
            {
                // type is pinned type
                CurrPath.Append(GetXmlDocParameterPath((tpType as PinnedType).ElementType, ExplicitMode));
                CurrPath.Append("^");
            }
            else if (tpType is TypeReference)
            {
                // if nested, scan enclosing type
                if (tpType.DeclaringType != null)
                    CurrPath.Append(GetXmlDocParameterPath(tpType.DeclaringType, ExplicitMode));

                // determine namespace
                string strNamespace = string.Empty;
                if ((tpType.Namespace != null && tpType.Namespace.Length > 0) || tpType.DeclaringType != null)
                {
                    strNamespace = tpType.Namespace +
                        ((CanAppendSpecialExplicitChar() && ExplicitMode) ? "#" : ".");
                }

                // concrete type
				CurrPath.Append(strNamespace).Append (
                    ((CanAppendSpecialExplicitChar() && ExplicitMode) ? tpType.Name.Replace(".", "#") : tpType.Name));
            }
        }

        private OperatorType GetOperatorType(MethodDefinition OperatorMethod)
        {
            try
            {
                return (OperatorType)Enum.Parse(typeof(OperatorType), OperatorMethod.Name.Trim());
            }
            catch
            {
                return OperatorType.None;
            }
        }

        public bool IsNested(
            TypeReference Type)
        {
            if (Type.IsNested)
                return true;

            return false;
        }

        private bool IsOperator(MethodDefinition Method)
        {
            if (Method.IsSpecialName && Method.Name.StartsWith("op_"))
                return true;

            return false;
        }

        private bool CanAppendSpecialExplicitChar()
        {
            if (m_compat == CompatibilityType.Net35)
                return true;

            return false;
        }

        private string StripGenericName(string OrginalClassName)
        {
            if (OrginalClassName.IndexOf("`") != -1)
                return OrginalClassName.Remove(OrginalClassName.IndexOf("`"));
            else
                return OrginalClassName;
        }
        #endregion

        #region Private members
        private CompatibilityType m_compat;
        #endregion
    }
    #endregion
}
