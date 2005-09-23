namespace ICSharpCode.SharpRefactory.Parser
{
	public sealed class ParserUtil
	{
		public static bool IsUnaryOperator(Token t)
		{
			return t.kind == Tokens.Plus      || t.kind == Tokens.Minus             ||
			       t.kind == Tokens.Not       || t.kind == Tokens.BitwiseComplement ||
			       t.kind == Tokens.Increment || t.kind == Tokens.Decrement         ||
			       t.kind == Tokens.True      || t.kind == Tokens.False;
		}
		
		public static bool IsBinaryOperator (Token t)
		{
			return t.kind == Tokens.Plus  || t.kind == Tokens.Minus ||
			       t.kind == Tokens.Times || t.kind == Tokens.Div   ||
			       t.kind == Tokens.Mod   ||
			       t.kind == Tokens.BitwiseAnd   || t.kind == Tokens.BitwiseOr  ||
			       t.kind == Tokens.Xor          ||
			       t.kind == Tokens.ShiftLeft    || t.kind == Tokens.ShiftRight ||
			       t.kind == Tokens.Equal        || t.kind == Tokens.NotEqual   ||
			       t.kind == Tokens.GreaterThan  || t.kind == Tokens.LessThan   ||
			       t.kind == Tokens.GreaterEqual || t.kind == Tokens.LessEqual;
		}
		
		public static bool IsTypeKW(Token t)
		{
			return t.kind == Tokens.Char    || t.kind == Tokens.Bool   ||
			       t.kind == Tokens.Object  || t.kind == Tokens.String ||
			       t.kind == Tokens.Sbyte   || t.kind == Tokens.Byte   ||
			       t.kind == Tokens.Short   || t.kind == Tokens.Ushort ||
			       t.kind == Tokens.Int     || t.kind == Tokens.Uint   ||
			       t.kind == Tokens.Long    || t.kind == Tokens.Ulong  ||
			       t.kind == Tokens.Float   || t.kind == Tokens.Double ||
			       t.kind == Tokens.Decimal;
		}
	}
}
