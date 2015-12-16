// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace ICSharpCode.NRefactory6.CSharp.CodeGeneration
{
	#if NR6
	public
	#endif
	class CodeGenerationOptions
	{
		internal readonly static Type typeInfo;
		readonly object instance;

		internal object Instance {
			get {
				return instance;
			}
		}

		static CodeGenerationOptions ()
		{
			typeInfo = Type.GetType ("Microsoft.CodeAnalysis.CodeGeneration.CodeGenerationOptions" + ReflectionNamespaces.WorkspacesAsmName, true);
		}

		public CodeGenerationOptions(
			Location contextLocation = null,
			Location afterThisLocation = null,
			Location beforeThisLocation = null,
			bool addImports = true,
			bool placeSystemNamespaceFirst = true,
			IEnumerable<INamespaceSymbol> additionalImports = null,
			bool generateMembers = true,
			bool mergeNestedNamespaces = true,
			bool mergeAttributes = true,
			bool generateDefaultAccessibility = true,
			bool generateMethodBodies = true,
			bool generateDocumentationComments = false,
			bool autoInsertionLocation = true,
			bool reuseSyntax = false)
		{
			instance = Activator.CreateInstance (typeInfo, new object[] {
				contextLocation,
				afterThisLocation,
				beforeThisLocation,
				addImports,
				placeSystemNamespaceFirst,
				additionalImports,
				generateMembers,
				mergeNestedNamespaces,
				mergeAttributes,
				generateDefaultAccessibility,
				generateMethodBodies,
				generateDocumentationComments,
				autoInsertionLocation,
				reuseSyntax
			});
		}
	}
}