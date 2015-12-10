// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ICSharpCode.NRefactory6.CSharp.GenerateType
{
	public class GenerateTypeDialogOptions
	{
		public bool IsPublicOnlyAccessibility { get; }
		public TypeKindOptions TypeKindOptions { get; }
		public bool IsAttribute { get; }

		public GenerateTypeDialogOptions(
			bool isPublicOnlyAccessibility = false,
			TypeKindOptions typeKindOptions = TypeKindOptions.AllOptions,
			bool isAttribute = false)
		{
			IsPublicOnlyAccessibility = isPublicOnlyAccessibility;
			this.TypeKindOptions = typeKindOptions;
			IsAttribute = isAttribute;
		}
	}
}
