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
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using System.Composition;
using System.Globalization;
using System.Threading;

namespace MonoDevelop.Ide.TypeSystem
{
	static class MonoDocDocumentationProvider
	{
		static bool hadError;
		static Dictionary<string, string> commentCache = new Dictionary<string, string> ();

		public static string GetDocumentation (string idString)
		{
			// If we had an exception while getting the help xml the monodoc help provider
			// shouldn't try it again. A corrupt .zip file could cause long tooltip delays otherwise.
			if (hadError)
				return null;
			string result;
			if (commentCache.TryGetValue (idString, out result))
				return result;
			
			XmlDocument doc = null;
			try {
				var helpTree = MonoDevelop.Projects.HelpService.HelpTree;
				if (helpTree == null)
					return null;
#pragma warning disable 618
				switch (idString[0]) {
				case 'T':
					doc = helpTree.GetHelpXml (idString);
					if (doc == null)
						return null;
					return doc.SelectSingleNode ("/Type/Docs").OuterXml;
				case 'M':
					var openIdx = idString.LastIndexOf ('(');
					var idx = idString.LastIndexOf ('.', openIdx < 0 ? idString.Length - 1 : openIdx);
					var typeId = "T:" + idString.Substring (2, idx - 2);
					doc = helpTree.GetHelpXml (typeId);
					if (doc == null)
						return null;
					string memberName;
					if (openIdx < 0) {
						memberName = idString.Substring (idx + 1);
						var xmlNode = doc.SelectSingleNode ("/Type/Members/Member[@MemberName='" + memberName + "']/Docs");
						return xmlNode?.OuterXml;
					}
					string parameterString = idString.Substring (openIdx + 1, idString.Length - openIdx - 2);
					var parameterTypes = parameterString.Split (new [] { ',' }, StringSplitOptions.RemoveEmptyEntries);
					memberName = idString.Substring (idx + 1, openIdx - idx - 1);
					foreach (var o in doc.SelectNodes ("/Type/Members/Member[@MemberName='" + memberName + "']")) {
						var curNode = o as XmlElement;
						if (curNode == null)
							continue;
						var paramList = curNode.SelectNodes ("Parameters/*");
						if (paramList.Count == 0)
							continue;
						if (parameterTypes.Length != paramList.Count)
							continue;
						bool matched = true;
						for (int i = 0; i < parameterTypes.Length; i++) {
							if (!CompareNames (parameterTypes [i], paramList [i].Attributes ["Type"].Value)) {
								matched = false;
								break;
							}
						}
						if (matched)
							return curNode.SelectSingleNode ("Docs")?.OuterXml;
					}
					return null;
				case 'P':
				case 'F':
				case 'E':
					idx = idString.LastIndexOf ('.', idString.Length - 1 );
					typeId = "T:" + idString.Substring (2, idx - 2);
					doc = helpTree.GetHelpXml (typeId);
					if (doc == null)
						return null;
					memberName = idString.Substring (idx + 1);
					var memberNode = doc.SelectSingleNode ("/Type/Members/Member[@MemberName='" + memberName + "']/Docs");
					return memberNode?.OuterXml;
				}
				return null;
			} catch (Exception e) {
				hadError = true;
				LoggingService.LogError ("Error while reading monodoc file.", e);
			}
			return null;
		}

		static bool CompareNames (string idStringName, string monoDocName)
		{
			if (idStringName.Length != monoDocName.Length)
				return false;
			for (int i = 0; i < idStringName.Length; i++) {
				if (idStringName [i] == '+') {
					if (monoDocName [i] != '.')
						return false;
					continue;
				}
				if (idStringName [i] != monoDocName [i])
					return false;

			}
			return true;
		}

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
#pragma warning disable 618
				if (entity.Kind == SymbolKind.NamedType) {
					doc = helpTree.GetHelpXml (idString);
				} else {
					var containingType = entity.ContainingType;
					if (containingType == null)
						return null;
					var parentId = containingType.GetDocumentationCommentId ();
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
			switch (t.TypeKind) {
			case TypeKind.Array:
				var arr = (IArrayTypeSymbol)t;
				return GetTypeString (arr.ElementType) + "[" + new string (',', arr.Rank - 1) + "]";
			case TypeKind.Pointer:
				var ptr = (IPointerTypeSymbol)t;
				return "*" + GetTypeString (ptr.PointedAtType);
			default:
				var docComment = t.GetDocumentationCommentId ();
				return docComment != null && docComment.Length > 2 ? docComment.Substring (2) : t.Name;
			}
		}
	}
}
