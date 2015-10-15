// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;
using Microsoft.CodeAnalysis;

namespace ICSharpCode.NRefactory6.CSharp.GenerateMember
{
	public abstract partial class AbstractGenerateMemberService<TSimpleNameSyntax, TExpressionSyntax>
		where TSimpleNameSyntax : TExpressionSyntax
		where TExpressionSyntax : SyntaxNode
	{
		protected AbstractGenerateMemberService()
		{
		}

		protected static readonly ISet<TypeKind> EnumType = new HashSet<TypeKind> { TypeKind.Enum };
		protected static readonly ISet<TypeKind> ClassInterfaceModuleStructTypes = new HashSet<TypeKind>
		{
			TypeKind.Class,
			TypeKind.Module,
			TypeKind.Struct,
			TypeKind.Interface
		};

		protected bool ValidateTypeToGenerateIn(
			Solution solution,
			INamedTypeSymbol typeToGenerateIn,
			bool isStatic,
			ISet<TypeKind> typeKinds,
			CancellationToken cancellationToken)
		{
			if (typeToGenerateIn == null)
			{
				return false;
			}

			if (typeToGenerateIn.IsAnonymousType)
			{
				return false;
			}

			if (!typeKinds.Contains(typeToGenerateIn.TypeKind))
			{
				return false;
			}

			if (typeToGenerateIn.TypeKind == TypeKind.Interface && isStatic)
			{
				return false;
			}

			// TODO(cyrusn): Make sure that there is a totally visible part somewhere (i.e.
			// venus) that we can generate into.
			var locations = typeToGenerateIn.Locations;
			return locations.Any(loc => loc.IsInSource);
		}

		protected bool TryDetermineTypeToGenerateIn(
			SemanticDocument document,
			INamedTypeSymbol containingType,
			TExpressionSyntax simpleNameOrMemberAccessExpression,
			CancellationToken cancellationToken,
			out INamedTypeSymbol typeToGenerateIn,
			out bool isStatic)
		{
			typeToGenerateIn = null;
			isStatic = false;

			var semanticModel = document.SemanticModel;
			var isMemberAccessExpression = simpleNameOrMemberAccessExpression.IsMemberAccessExpression();
			if (isMemberAccessExpression ||
				simpleNameOrMemberAccessExpression.IsConditionalMemberAccessExpression())
			{
				var beforeDotExpression = isMemberAccessExpression ?
					simpleNameOrMemberAccessExpression.GetExpressionOfMemberAccessExpression() :
					simpleNameOrMemberAccessExpression.GetExpressionOfConditionalMemberAccessExpression();
				if (beforeDotExpression != null)
				{
					var typeInfo = semanticModel.GetTypeInfo(beforeDotExpression, cancellationToken);
					var semanticInfo = semanticModel.GetSymbolInfo(beforeDotExpression, cancellationToken);

					typeToGenerateIn = typeInfo.Type is ITypeParameterSymbol
						? ((ITypeParameterSymbol)typeInfo.Type).GetNamedTypeSymbolConstraint()
						: typeInfo.Type as INamedTypeSymbol;

					isStatic = semanticInfo.Symbol is INamedTypeSymbol;
				}
			}
			else if (simpleNameOrMemberAccessExpression.IsPointerMemberAccessExpression())
			{
				var beforeArrowExpression = simpleNameOrMemberAccessExpression.GetExpressionOfMemberAccessExpression();
				if (beforeArrowExpression != null)
				{
					var typeInfo = semanticModel.GetTypeInfo(beforeArrowExpression, cancellationToken);

					if (typeInfo.Type.IsPointerType())
					{
						typeToGenerateIn = ((IPointerTypeSymbol)typeInfo.Type).PointedAtType as INamedTypeSymbol;
						isStatic = false;
					}
				}
			}
			else if (simpleNameOrMemberAccessExpression.IsAttributeNamedArgumentIdentifier())
			{
				var attributeNode = simpleNameOrMemberAccessExpression.GetAncestors().FirstOrDefault(CSharpSyntaxFactsService.IsAttribute);
				var attributeName = attributeNode.GetNameOfAttribute();
				var attributeType = semanticModel.GetTypeInfo(attributeName, cancellationToken);

				typeToGenerateIn = attributeType.Type as INamedTypeSymbol;
				isStatic = false;
			}
			else if (simpleNameOrMemberAccessExpression.IsObjectInitializerNamedAssignmentIdentifier())
			{
				var objectCreationNode = simpleNameOrMemberAccessExpression.GetAncestors().FirstOrDefault(CSharpSyntaxFactsService.IsObjectCreationExpression);
				typeToGenerateIn = semanticModel.GetTypeInfo(objectCreationNode, cancellationToken).Type as INamedTypeSymbol;
				isStatic = false;
			}
			else
			{
				// Generating into the containing type.
				typeToGenerateIn = containingType;
				isStatic = simpleNameOrMemberAccessExpression.IsInStaticContext();
			}

			if (typeToGenerateIn != null)
			{
				typeToGenerateIn = typeToGenerateIn.OriginalDefinition;
			}

			return typeToGenerateIn != null;
		}
	}
}
