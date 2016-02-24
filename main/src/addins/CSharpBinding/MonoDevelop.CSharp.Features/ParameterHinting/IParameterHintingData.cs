// 
// IParameterDataProvider.cs
//  
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
// 
// Copyright (c) 2011 Xamarin Inc. (http://xamarin.com)
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
using Microsoft.CodeAnalysis;
using System.Collections.Immutable;
using System.Linq;

namespace ICSharpCode.NRefactory6.CSharp.Completion
{
	/// <summary>
	/// Provides intellisense information for a collection of parametrized members.
	/// </summary>
	public interface IParameterHintingData
	{
		/// <summary>
		/// Gets the symbol for which the parameter should be created.
		/// </summary>
		ISymbol Symbol {
			get;
		}
		
		int ParameterCount {
			get;
		}
		
		bool IsParameterListAllowed {
			get;
		}

		string GetParameterName (int currentParameter);
	}
	
	class ParameterHintingData : IParameterHintingData
	{
		public ISymbol Symbol {
			get;
			private set;
		}
		
		public ParameterHintingData(IMethodSymbol symbol)
		{
			this.Symbol = symbol;
		}
		
		public ParameterHintingData(IPropertySymbol symbol)
		{
			this.Symbol = symbol;
		}
		
		static ImmutableArray<IParameterSymbol> GetParameterList (IParameterHintingData data)
		{
			var ms = data.Symbol as IMethodSymbol;
			if (ms != null)
				return ms.Parameters;
				
			var ps = data.Symbol as IPropertySymbol;
			if (ps != null)
				return ps.Parameters;

			return ImmutableArray<IParameterSymbol>.Empty;
		}
		
		public string GetParameterName (int currentParameter)
		{
			var list = GetParameterList (this);
			if (currentParameter < 0 || currentParameter >= list.Length)
				throw new ArgumentOutOfRangeException ("currentParameter");
			return list [currentParameter].Name;
		}

		public int ParameterCount {
			get {
				return GetParameterList(this).Length;
			}
		}

		public bool IsParameterListAllowed {
			get {
				var param = GetParameterList(this).LastOrDefault();
				return param != null && param.IsParams;
			}
		}
	}
	
	class DelegateParameterHintingData : IParameterHintingData
	{
		readonly IMethodSymbol invocationMethod;

		public ISymbol Symbol {
			get;
			private set;
		}
		
		public DelegateParameterHintingData(ITypeSymbol symbol)
		{
			this.Symbol = symbol;
			this.invocationMethod = symbol.GetDelegateInvokeMethod();
		}
		
		public string GetParameterName (int currentParameter)
		{
			var list = invocationMethod.Parameters;
			if (currentParameter < 0 || currentParameter >= list.Length)
				throw new ArgumentOutOfRangeException ("currentParameter");
			return list [currentParameter].Name;
		}

		public int ParameterCount {
			get {
				return invocationMethod.Parameters.Length;
			}
		}

		public bool IsParameterListAllowed {
			get {
				var param = invocationMethod.Parameters.LastOrDefault();
				return param != null && param.IsParams;
			}
		}
	}

	class ArrayParameterHintingData : IParameterHintingData
	{
		readonly IArrayTypeSymbol arrayType;

		public ISymbol Symbol {
			get {
				return arrayType;
			}
		}
		
		public ArrayParameterHintingData(IArrayTypeSymbol arrayType)
		{
			this.arrayType = arrayType;
		}
		
		public string GetParameterName (int currentParameter)
		{
			return null;
		}

		public int ParameterCount {
			get {
				return arrayType.Rank;
			}
		}

		public bool IsParameterListAllowed {
			get {
				return false;
			}
		}
	}

	class TypeParameterHintingData : IParameterHintingData
	{
		public ISymbol Symbol {
			get;
			private set;
		}
		
		public TypeParameterHintingData(IMethodSymbol symbol)
		{
			this.Symbol = symbol;
		}
		
		public TypeParameterHintingData(INamedTypeSymbol symbol)
		{
			this.Symbol = symbol;
		}
		
		static ImmutableArray<ITypeParameterSymbol> GetTypeParameterList (IParameterHintingData data)
		{
			var ms = data.Symbol as IMethodSymbol;
			if (ms != null)
				return ms.TypeParameters;
				
			var ps = data.Symbol as INamedTypeSymbol;
			if (ps != null)
				return ps.TypeParameters;

			return ImmutableArray<ITypeParameterSymbol>.Empty;
		}

		
		public string GetParameterName (int currentParameter)
		{
			var list = GetTypeParameterList (this);
			if (currentParameter < 0 || currentParameter >= list.Length)
				throw new ArgumentOutOfRangeException ("currentParameter");
			return list [currentParameter].Name;
		}

		public int ParameterCount {
			get {
				return GetTypeParameterList(this).Length;
			}
		}

		public bool IsParameterListAllowed {
			get {
				return false;
			}
		}
	}
}