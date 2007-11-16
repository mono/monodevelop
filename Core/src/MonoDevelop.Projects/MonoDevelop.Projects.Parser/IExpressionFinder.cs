//  IExpressionFinder.cs
//
//  This file was derived from a file from #Develop. 
//
//  Copyright (C) 2001-2007 Mike Krüger <mkrueger@novell.com>
// 
//  This program is free software; you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation; either version 2 of the License, or
//  (at your option) any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
//  GNU General Public License for more details.
//  
//  You should have received a copy of the GNU General Public License
//  along with this program; if not, write to the Free Software
//  Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA

using System;

namespace MonoDevelop.Projects.Parser
{
	public interface IExpressionFinder
	{
		/// <summary>
		/// Finds an expression before the current offset.
		/// </summary>
		ExpressionResult FindExpression(string text, int offset);
		
		/// <summary>
		/// Finds an expression around the current offset.
		/// </summary>
		ExpressionResult FindFullExpression(string text, int offset);
		
		/// <summary>
		/// Removed the last part of the expression.
		/// </summary>
		/// <example>
		/// "arr[i]" => "arr"
		/// "obj.Field" => "obj"
		/// "obj.Method(args,...)" => "obj.Method"
		/// </example>
		string RemoveLastPart(string expression);
	}
	
	/// <summary>
	/// Structure containing the result of a call to an expression finder.
	/// </summary>
	public struct ExpressionResult
	{
		/// <summary>The expression that has been found at the specified offset.</summary>
		public string Expression;
		/// <summary>Specifies the context in which the expression was found.</summary>
		public ExpressionContext Context;
		/// <summary>An object carrying additional language-dependend data.</summary>
		public object Tag;
		
		public ExpressionResult(string expression) : this(expression, ExpressionContext.Default, null) {}
		public ExpressionResult(string expression, ExpressionContext context) : this(expression, context, null) {}
		public ExpressionResult(string expression, object tag) : this(expression, ExpressionContext.Default, tag)  {}
		
		public ExpressionResult(string expression, ExpressionContext context, object tag)
		{
			this.Expression = expression;
			this.Context = context;
			this.Tag = tag;
		}
		
		public override string ToString()
		{
			if (Context == ExpressionContext.Default)
				return "<" + Expression + ">";
			else
				return "<" + Expression + "> (" + Context.ToString() + ")";
		}
	}
}
