// 
// MonoDocDocumentationProvider.cs
//  
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
// 
// Copyright (c) 2012 Xamarin Inc. (http://xamarin.com)
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
using System;
using System.Collections.Generic;
using System.Xml;
using MonoDevelop.Core;
using System.Text;
using Microsoft.CodeAnalysis;

namespace MonoDevelop.Ide.TypeSystem
{
	static class MonoDocDocumentationProvider
	{
		static bool hadError;
		static Dictionary<string, string> commentCache = new Dictionary<string, string> ();

		public static string GetDocumentation (ISymbol entity)
		{
			if (entity == null)
				throw new System.ArgumentNullException ("entity");
			
			// If we had an exception while getting the help xml the monodoc help provider
			// shouldn't try it again. A corrupt .zip file could cause long tooltip delays otherwise.
			if (hadError)
				return null;
			var idString = entity.GetDocumentationCommentId ();
			if (string.IsNullOrEmpty (idString))
				return null;
			string result;
			if (commentCache.TryGetValue (idString, out result))
				return result;
			XmlDocument doc = null;
			try {
				var helpTree = MonoDevelop.Projects.HelpService.HelpTree;
				if (helpTree == null)
					return null;
				if (entity.Kind == SymbolKind.NamedType) {
					doc = helpTree.GetHelpXml (idString);
				} else {
					var parentId = entity.ContainingType.GetDocumentationCommentId ();

					doc = helpTree.GetHelpXml (parentId);
					if (doc == null)
						return null;
					XmlNode node = SelectNode (doc, entity);
					
					if (node != null)
						return commentCache [idString] = node.OuterXml;
					return null;
				}
			} catch (Exception e) {
				hadError = true;
				LoggingService.LogError ("Error while reading monodoc file.", e);
			}
			if (doc == null) {
				commentCache [idString] = null;
				return null;
			}
			return commentCache [idString] = doc.OuterXml;
		}

		internal static void ClearCommentCache ()
		{
			commentCache = new Dictionary<string, string> ();
		}

		static XmlNode SelectNode (XmlDocument doc, ISymbol entity)
		{
			switch (entity.Kind) {
			case SymbolKind.NamedType:
			case SymbolKind.Field:
			case SymbolKind.Property:
			case SymbolKind.Event:
				return doc.SelectSingleNode ("/Type/Members/Member[@MemberName='" + entity.Name + "']");
			
			case SymbolKind.Method:
				var method = (IMethodSymbol)entity;
				if (method.MethodKind == MethodKind.Constructor)
					return SelectOverload (doc.SelectNodes ("/Type/Members/Member[@MemberName='.ctor']"), method);
				return SelectOverload (doc.SelectNodes ("/Type/Members/Member[@MemberName='" + entity.Name + "']"), method);
			default:
				throw new ArgumentOutOfRangeException ();
			}
		}

		static XmlNode SelectOverload (XmlNodeList nodes, IMethodSymbol entity)
		{
			XmlNode node = null;
			if (nodes.Count == 1) {
				node = nodes [0];
			} else {
				var p = entity.Parameters;
				foreach (XmlNode curNode in nodes) {
					var paramList = curNode.SelectNodes ("Parameters/*");
					if (p.Length == 0 && paramList.Count == 0) 
						return curNode;
					if (p.Length != paramList.Count) 
						continue;
					bool matched = true;
					for (int i = 0; i < p.Length; i++) {
						var idString = GetTypeString (p [i].Type);
						if (idString != paramList [i].Attributes ["Type"].Value) {
							matched = false;
							break;
						}
					}
					if (matched) {
						return curNode;
					}
				}
			}
			if (node != null) {
				System.Xml.XmlNode result = node.SelectSingleNode ("Docs");
				return result;
			}
			return null;
		}
		

		static string GetTypeString (ITypeSymbol t)
		{
			return t.ToDisplayString (SymbolDisplayFormat.CSharpErrorMessageFormat);
//			if (t.TypeKind == TypeKind.Unknown)
//				return t.Name;
//			
//			if (t.TypeKind == TypeKind.TypeParameter)
//				return t.ToDisplayString (SymbolDisplayFormat.CSharpErrorMessageFormat);
//			
////			var typeWithElementType = t as ;
////			if (typeWithElementType != null) {
////				var sb = new StringBuilder ();
////			
////				if (typeWithElementType is PointerType) {
////					sb.Append ("*");
////				} 
////				sb.Append (GetTypeString (typeWithElementType.ElementType));
////
////				if (typeWithElementType is ArrayType) {
////					sb.Append ("[");
////					sb.Append (new string (',', ((ArrayType)t).Dimensions - 1));
////					sb.Append ("]");
////				}
////				return sb.ToString ();
////			}
//
//			var result = new StringBuilder ();
//			
//			if (t.ContainingType != null) {
//				string typeString = GetTypeString (t.ContainingType);
//				result.Append (typeString);
//				result.Append (".");
//			} else {
//				result.Append (t.ContainingNamespace.ToDisplayString (SymbolDisplayFormat.CSharpErrorMessageFormat) + ".");
//			}
//
//			result.Append (t.Name);
//
////			if (t.par > 0) {
////				result.Append ("<");
////				for (int i = 0; i < t.TypeParameterCount; i++) {
////					if (i > 0)
////						result.Append (",");
////					if (t.TypeArguments.Count > 0) {
////						result.Append (GetTypeString (t.TypeArguments [i]));
////					} else {
////						result.Append (t.TypeParameters [i].FullName);
////					}
////				}
////				result.Append (">");
////			}
//			return result.ToString ();
		}
	}
}
