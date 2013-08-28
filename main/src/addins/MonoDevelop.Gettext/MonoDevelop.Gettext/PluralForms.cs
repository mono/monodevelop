//
// PluralForms.cs
//
// Author:
//   David Makovsk� <yakeen@sannyas-on.net>
//
// Copyright (C) 1999-2006 Vaclav Slavik (Code and design inspiration - poedit.org)
// Copyright (C) 2007 David Makovsk�
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// 

using System;
using System.Collections.Generic;
using System.Text;

namespace MonoDevelop.Gettext
{
/*
                                Simplified Grammar

			Expression:
				LogicalOrExpression '?' Expression ':' Expression
				LogicalOrExpression

			LogicalOrExpression:
				LogicalAndExpression "||" LogicalOrExpression   // to (a || b) || c
				LogicalAndExpression

			LogicalAndExpression:
				EqualityExpression "&&" LogicalAndExpression    // to (a && b) && c
				EqualityExpression

			EqualityExpression:
				RelationalExpression "==" RelationalExperession
				RelationalExpression "!=" RelationalExperession
				RelationalExpression

			RelationalExpression:
				MultiplicativeExpression '>' MultiplicativeExpression
				MultiplicativeExpression '<' MultiplicativeExpression
				MultiplicativeExpression ">=" MultiplicativeExpression
				MultiplicativeExpression "<=" MultiplicativeExpression
				MultiplicativeExpression

			MultiplicativeExpression:
				PmExpression '%' PmExpression
				PmExpression

			PmExpression:
				N
				Number
				'(' Expression ')'
*/
	class PluralFormsToken
	{
		public enum Type
		{
			Error,
			Eof,
			Number,
			N,
			Plural,
			Nplurals,
			Equal,
			Assign,
			Greater,
			GreaterOrEqual,
			Less,
			LessOrEqual,
			Reminder,
			NotEqual,
			LogicalAnd,
			LogicalOr,
			Question,
			Colon,
			Semicolon,
			LeftBracket,
			RightBracket
		}

		Type type;
		int number;
		
		public Type TokenType
		{
			get { return type; }
			set { type = value; }
		}
		
		public int Number
		{
			get { return number; }
			set { number = value; }
		}
		
		public PluralFormsToken ()
		{
		}

		public PluralFormsToken (PluralFormsToken src)
		{
			type = src.type;
			number = src.number;
		}
	}
	
	internal class PluralFormsScanner
	{
		string str;
		int pos = 0;
		PluralFormsToken token;
		
		public PluralFormsScanner(string str)
		{
			this.str = str;
			token = new PluralFormsToken ();
			NextToken ();
		}
		
		public PluralFormsToken  Token
		{
			get { return token; }
		}

		// returns false if error
		public bool NextToken ()
		{
			PluralFormsToken.Type type = PluralFormsToken.Type.Error;			
			while (pos < str.Length && str[pos] == ' ')
			{
				++pos;
			}
			if (pos >= str.Length || str[pos] == '\0')
			{
				type = PluralFormsToken.Type.Eof;
			} else if (Char.IsDigit (str[pos]))
			{
				int number = str[pos++] - '0';
				while (pos < str.Length && Char.IsDigit (str[pos]))
				{
					number = number * 10 + (str[pos++] - '0');
				}
				token.Number = number;
				type = PluralFormsToken.Type.Number;
			} else if (Char.IsLetter (str[pos]))
			{
				int begin = pos++;
				while (pos < str.Length && Char.IsLetterOrDigit (str[pos]))
				{
					++pos;
				}
				int size = pos - begin;
				if (size == 1 && str[begin] == 'n')
				{
					type = PluralFormsToken.Type.N;
				} else if (size == 6 && str.Substring (begin, size) == "plural")
				{
					type = PluralFormsToken.Type.Plural;
				} else if (size == 8 && str.Substring (begin, size) == "nplurals")
				{
					type = PluralFormsToken.Type.Nplurals;
				}
			} else if (str[pos] == '=')
			{
				++pos;
				if (pos < str.Length && str[pos] == '=')
				{
					++pos;
					type = PluralFormsToken.Type.Equal;
				} else
				{
					type = PluralFormsToken.Type.Assign;
				}
			} else if (str[pos] == '>')
			{
				++pos;
				if (pos < str.Length && str[pos] == '=')
				{
					++pos;
					type = PluralFormsToken.Type.GreaterOrEqual;
				} else
				{
					type = PluralFormsToken.Type.Greater;
				}
			} else if (str[pos] == '<')
			{
				++pos;
				if (pos < str.Length && str[pos] == '=')
				{
					++pos;
					type = PluralFormsToken.Type.LessOrEqual;
				} else
				{
					type = PluralFormsToken.Type.Less;
				}
			} else if (str[pos] == '%')
			{
				++pos;
				type = PluralFormsToken.Type.Reminder;
			} else if (str[pos] == '!' && str[pos + 1] == '=')
			{
				pos += 2;
				type = PluralFormsToken.Type.NotEqual;
			} else if (pos + 1 < str.Length && str[pos] == '&' && str[pos + 1] == '&')
			{
				pos += 2;
				type = PluralFormsToken.Type.LogicalAnd;
			} else if (pos + 1 < str.Length && str[pos] == '|' && str[pos + 1] == '|')
			{
				pos += 2;
				type = PluralFormsToken.Type.LogicalOr;
			} else if (str[pos] == '?')
			{
				++pos;
				type = PluralFormsToken.Type.Question;
			} else if (str[pos] == ':')
			{
				++pos;
				type = PluralFormsToken.Type.Colon;
			} else if (str[pos] == ';')
			{
				++pos;
				type = PluralFormsToken.Type.Semicolon;
			} else if (str[pos] == '(')
			{
				++pos;
				type = PluralFormsToken.Type.LeftBracket;
			} else if (str[pos] == ')')
			{
				++pos;
				type = PluralFormsToken.Type.RightBracket;
			}
			token.TokenType = type;
			return type != PluralFormsToken.Type.Error;
		}
	}
	
	internal class PluralFormsNode
	{
		PluralFormsToken token;
		PluralFormsNode[] nodes = new PluralFormsNode[3];
    
		public PluralFormsNode (PluralFormsToken token)
		{
			this.token = token;
		}
		
		public PluralFormsToken Token
		{
			get { return token; }
		}
		
		public PluralFormsNode Node (int i)
        {
			if (i >= 0 && i <= 2)
				return nodes[i];
			else
				return null;
        }
        
         public void SetNode (int i, PluralFormsNode n)
         {
			 if (i >= 0 && i <= 2)
				nodes[i] = n;
         }
         
         public PluralFormsNode ReleaseNode (int i)
         {
			 PluralFormsNode node = nodes[i];
			 nodes[i] = null;
			 return node;
         }
         
         public int Evaluate (int n)
         {
			switch (token.TokenType)
			{
				// leaf
				case PluralFormsToken.Type.Number:
					return token.Number;
				case PluralFormsToken.Type.N:
					return n;
				// 2 args
				case PluralFormsToken.Type.Equal:
					return nodes[0].Evaluate (n) == nodes[1].Evaluate (n) ? 1 : 0;
				case PluralFormsToken.Type.NotEqual:
					return nodes[0].Evaluate (n) != nodes[1].Evaluate (n) ? 1 : 0;
				case PluralFormsToken.Type.Greater:
					return nodes[0].Evaluate (n) > nodes[1].Evaluate (n) ? 1 : 0;
				case PluralFormsToken.Type.GreaterOrEqual:
					return nodes[0].Evaluate (n) >= nodes[1].Evaluate (n) ? 1 : 0;
				case PluralFormsToken.Type.Less:
					return nodes[0].Evaluate (n) < nodes[1].Evaluate (n) ? 1 : 0;
				case PluralFormsToken.Type.LessOrEqual:
					return nodes[0].Evaluate (n) <= nodes[1].Evaluate (n) ? 1 : 0;
				case PluralFormsToken.Type.Reminder:
						int number = nodes[0].Evaluate (n);
						if (number != 0)
							return nodes[0].Evaluate (n) % number;
						else
							return 0;
				case PluralFormsToken.Type.LogicalAnd:
					return nodes[0].Evaluate (n) != 0 && nodes[1].Evaluate (n) != 0 ? 1 : 0;
				case PluralFormsToken.Type.LogicalOr:
					return nodes[0].Evaluate (n) != 0 || nodes[1].Evaluate (n) != 0 ? 1 : 0;
				// 3 args
				case PluralFormsToken.Type.Question:
					return nodes[0].Evaluate (n) != 0 ? nodes[1].Evaluate (n) : nodes[2].Evaluate (n);
				default:
					return 0;
			}
         }
	 }
	
	internal class PluralFormsCalculator
	{
		int nplurals;
		PluralFormsNode plural;
		
		public PluralFormsCalculator ()
		{
			nplurals = 0;
			plural = null;
		}

		// input: number, returns msgstr index
		public int Evaluate (int n)
		{
			if (plural == null)
			{
				return 0;
			}
			int number = plural.Evaluate (n);
			if (number < 0 || number > nplurals)
			{
				return 0;
			}
			return number;
		}

		// input: text after "Plural-Forms:" (e.g. "nplurals=2; plural=(n != 1);"),
		// if s == 0, creates default handler
		// returns 0 if error
		public static PluralFormsCalculator Make (string str)
		{
			PluralFormsCalculator calculator = new PluralFormsCalculator ();
			if (str != null)
			{
				PluralFormsScanner scanner = new PluralFormsScanner (str);
				PluralFormsParser p = new PluralFormsParser (scanner);
				if (!p.Parse (calculator))
				{
					return null;
				}
			}
			return calculator;
		}

		public void Init (int nplurals, PluralFormsNode plural)
		{
			this.nplurals = nplurals;
			this.plural = plural;
		}
	}
	
	internal class PluralFormsParser
	{
		// stops at SEMICOLON, returns 0 if error
		PluralFormsScanner scanner;
		
		public PluralFormsParser (PluralFormsScanner scanner)
		{
			this.scanner = scanner;
		}
		
		public bool Parse (PluralFormsCalculator calculator)
		{
			if (Token.TokenType != PluralFormsToken.Type.Nplurals)
				return false;
			if (! NextToken ())
				return false;
			if (Token.TokenType != PluralFormsToken.Type.Assign)
				return false;
			if (! NextToken ())
				return false;
			if (Token.TokenType != PluralFormsToken.Type.Number)
				return false;
			int nplurals = Token.Number;
			if (! NextToken ())
				return false;
			if (Token.TokenType != PluralFormsToken.Type.Semicolon)
				return false;
			if (! NextToken ())
				return false;
			if (Token.TokenType != PluralFormsToken.Type.Plural)
				return false;
			if (! NextToken ())
				return false;
			if (Token.TokenType != PluralFormsToken.Type.Assign)
				return false;
			if (! NextToken ())
				return false;
			PluralFormsNode plural = ParsePlural ();
			if (plural == null)
				return false;
			if (Token.TokenType != PluralFormsToken.Type.Semicolon)
				return false;
			if (! NextToken ())
				return false;
			if (Token.TokenType != PluralFormsToken.Type.Eof)
				return false;
			calculator.Init (nplurals, plural);
			return true;
		}

		PluralFormsNode ParsePlural ()
		{
			PluralFormsNode p = Expression ();
			if (p == null)
			{
				return null;
			}
			if (Token.TokenType != PluralFormsToken.Type.Semicolon)
			{
				return null;
			}
			return p;
		}
    
		PluralFormsToken Token
		{
			get { return scanner.Token; }
		}
		
		bool NextToken ()
		{
			if (! scanner.NextToken ())
				return false;
			return true;
		}

		PluralFormsNode Expression ()
		{
			PluralFormsNode p = LogicalOrExpression ();
			if (p == null)
				return null;
			PluralFormsNode n = p;
			if (Token.TokenType == PluralFormsToken.Type.Question)
			{
				PluralFormsNode qn = new PluralFormsNode (new PluralFormsToken (Token));
				if (! NextToken ())
				{
					return null;
				}
				p = Expression ();
				if (p == null)
				{
					return null;
				}
				qn.SetNode (1, p);
				if (Token.TokenType != PluralFormsToken.Type.Colon)
				{
					return null;
				}
				if (! NextToken ())
				{
					return null;
				}
				p = Expression ();
				if (p == null)
				{
					return null;
				}
				qn.SetNode (2, p);
				qn.SetNode (0, n);
				return qn;
			}
			return n;
		}
		
		PluralFormsNode LogicalOrExpression ()
		{
			PluralFormsNode p = LogicalAndExpression ();
			if (p == null)
				return null;
			PluralFormsNode ln = p;
			if (Token.TokenType == PluralFormsToken.Type.LogicalOr)
			{
				PluralFormsNode un = new PluralFormsNode (new PluralFormsToken(Token));
				if (! NextToken ())
				{
					return null;
				}
				p = LogicalOrExpression ();
				if (p == null)
				{
					return null;
				}
				PluralFormsNode rn = p; // right
				if (rn.Token.TokenType == PluralFormsToken.Type.LogicalOr)
				{
					// see logicalAndExpression comment
					un.SetNode (0, ln);
					un.SetNode (1, rn.ReleaseNode (0));
					rn.SetNode (0, un);
					return rn;
				}
				
				un.SetNode (0, ln);
				un.SetNode (1, rn);
				return un;
			}
			return ln;
		}
		
		PluralFormsNode LogicalAndExpression ()
		{
			PluralFormsNode p = EqualityExpression ();
			if (p == null)
				return null;
			PluralFormsNode ln = p; // left
			if (Token.TokenType == PluralFormsToken.Type.LogicalAnd)
			{
				PluralFormsNode un = new PluralFormsNode (new PluralFormsToken(Token)); // up
				if (! NextToken ())
				{
					return null;
				}
				p = LogicalAndExpression ();
				if (p == null)
				{
					return null;
				}
				PluralFormsNode rn = p; // right
				if (rn.Token.TokenType == PluralFormsToken.Type.LogicalAnd)
				{
					// transform 1 && (2 && 3) -> (1 && 2) && 3
					//     u                  r
					// l       r     ->   u      3
					//       2   3      l   2
					un.SetNode (0, ln);
					un.SetNode (1, rn.ReleaseNode (0));
					rn.SetNode (0, un);
					return rn;
				}

				un.SetNode (0, ln);
				un.SetNode (1, rn);
				return un;
			}
			return ln;
		}
		
		PluralFormsNode EqualityExpression ()
		{
			PluralFormsNode p = RelationalExpression ();
			if (p == null)
				return null;
			PluralFormsNode n = p;
			if (Token.TokenType == PluralFormsToken.Type.Equal || Token.TokenType == PluralFormsToken.Type.NotEqual)
			{
				PluralFormsNode qn = new PluralFormsNode (new PluralFormsToken(Token));
				if (! NextToken ())
				{
					return null;
				}
				p = RelationalExpression ();
				if (p == null)
				{
					return null;
				}
				qn.SetNode (1, p);
				qn.SetNode (0, n);
				return qn;
			}
			return n;
		}
		
		PluralFormsNode MultiplicativeExpression ()
		{
			PluralFormsNode p = PmExpression ();
			if (p == null)
			{
				return null;
			}
			PluralFormsNode n = p;
			if (Token.TokenType == PluralFormsToken.Type.Reminder)
			{
				PluralFormsNode qn = new PluralFormsNode (new PluralFormsToken(Token));
				if (! NextToken())
				{
					return null;
				}
				p = PmExpression ();
				if (p == null)
				{
					return null;
				}
				qn.SetNode (1, p);
				qn.SetNode (0, n);
				return qn;
			}
			return n;
		}
		
		PluralFormsNode RelationalExpression ()
		{
			PluralFormsNode p = MultiplicativeExpression ();
			if (p == null)
				return null;
			PluralFormsNode n = p;
			if (Token.TokenType == PluralFormsToken.Type.Greater
			    || Token.TokenType == PluralFormsToken.Type.Less
				|| Token.TokenType == PluralFormsToken.Type.GreaterOrEqual
				|| Token.TokenType == PluralFormsToken.Type.LessOrEqual)
			{
				PluralFormsNode qn = new PluralFormsNode (new PluralFormsToken(Token));
				if (! NextToken ()) {
					return null;
				}
				p = MultiplicativeExpression ();
				if (p == null) {
					return null;
				}
				qn.SetNode (1, p);
				qn.SetNode (0, n);
				return qn;
			}
			return n;
		}
		
		PluralFormsNode PmExpression ()
		{
			PluralFormsNode n;
			if (Token.TokenType == PluralFormsToken.Type.N || Token.TokenType == PluralFormsToken.Type.Number) {
				n = new PluralFormsNode (new PluralFormsToken(Token));
				if (! NextToken ()) {
					return null;
				}
			} else if (Token.TokenType == PluralFormsToken.Type.LeftBracket) {
				if (! NextToken ()) {
					return null;
				}
				PluralFormsNode p = Expression ();
				if (p == null) {
					return null;
				}
				n = p;
				if (Token.TokenType != PluralFormsToken.Type.RightBracket) {
					return null;
				}
				if (! NextToken ()) {
					return null;
				}
			} else {
				return null;
			}
			return n;
		}
	}
}
