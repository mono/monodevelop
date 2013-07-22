
grammar ExCSS;

options
{
    language=CSharp3;
	output=AST;
	ASTLabelType=CommonTree;
}

tokens{
	ATTRIBUTE;
	ATTRIBUTEOPERATOR;
	CHARSET;
	CLASS;
	DECLARATION;
	ELEMENTNAME;
	EXPRESSION;
	IMPORT;
	IMPORTANT;
	PROPERTY;
	RULESET;
	SELECTOR;
	SIMPLESELECTOR;
	STYLESHEET;
}

public parse
	:   stylesheet -> ^(STYLESHEET stylesheet)
		EOF
	;


stylesheet 
	:   charset?	
		imports* 
		//bodylist
		(
			  ruleset
			| media
			| page
			| font_face
		)*
		
		-> 	^(CHARSET charset?)
			^(IMPORT imports*)
			^(RULESET ruleset*)
	;

charset 
	: 	CHARSET_SYM STRING SEMI 
	;

imports 
	: 	IMPORT_SYM (STRING |URI) (media_query_list)? SEMI
	;

media 
	: 	MEDIA_SYM media_query_list 
		LBRACE 
			ruleset* 
		RBRACE;

media_query_list 
	:	 media_query (COMMA media_query)*;

media_query 
	:	(  	  'only' 
			| 'ONLY' 
			| 'not' 
			| 'NOT' 
		)? 
		  media_type (('and' | 'AND') expression)*
		| expression (('and' | 'AND') expression)*
    ;

media_type
	: 	IDENT
	;

expression
	: 	LPAREN 
			media_feature (COLON expr)? 
		RPAREN
	;

media_feature
	: 	IDENT
	;

page
    : 	PAGE_SYM (pseudoPage)?
        LBRACE
            declaration
            SEMI (declaration SEMI)*
        RBRACE
    ;

pseudoPage
    : COLON IDENT
    ;
	
font_face
	:	FONT_FACE_SYM
		LBRACE
			(declaration(SEMI declaration?)*)?
		RBRACE
	;

operator
    : 	  SOLIDUS
		| COMMA
		| OPEQ
		| DOT
		| COLON
		|
    ;

combinator 
	: 
		  PLUS 
		| GREATER 
		| TILDE 
		| /* empty */;

unaryOperator
	: 	  MINUS 
		| PLUS;

property
	:	property_prefix IDENT
			//-> ^(PROPERTY property_prefix IDENT)
	;

property_prefix
	: 	( '*' | '_' | '!' )?
	;

ruleset
	:	selector (COMMA selector)*
		LBRACE
			declaration? (SEMI declaration?)*
		RBRACE
			-> ^(SELECTOR selector* ^(DECLARATION declaration*))
;

selector
    : 	simple_selector (combinator simple_selector)* 
			-> ^(SIMPLESELECTOR simple_selector*)
    ;

simple_selector
    : 	element_name ((element_predicate)=>subsequent_element)* 
        | ((element_predicate)=>subsequent_element)+	
    ;

element_predicate
    : 	  HASH 
		| DOT 
		| LBRACKET 
		| COLON
    ;

subsequent_element
    : 	  HASH
		| cssClass
		| attrib
		| pseudo
    ;

cssClass
    : 	DOT IDENT -> ^(CLASS IDENT)
    ;

element_name
    : 	  IDENT -> ^(ELEMENTNAME IDENT)
		| STAR 	-> ^(ELEMENTNAME STAR)
    ;

attrib
    : 	LBRACKET
			IDENT attribute_selector? //attribute_operator?
		RBRACKET
			-> ^(ATTRIBUTE IDENT attribute_selector?)
	;
	
attribute_selector
	:	attribute_operator ( IDENT | STRING ) 
	;
	
attribute_operator
	:
		  OPEQ 				-> ^(ATTRIBUTEOPERATOR OPEQ)
		| INCLUDES 			-> ^(ATTRIBUTEOPERATOR INCLUDES)
		| DASHMATCH 		-> ^(ATTRIBUTEOPERATOR DASHMATCH)
		| CONTAINSMATCH 	-> ^(ATTRIBUTEOPERATOR CONTAINSMATCH)
		| STARTSWITHMATCH 	-> ^(ATTRIBUTEOPERATOR STARTSWITHMATCH)
		| ENDSWITHMATCH 	-> ^(ATTRIBUTEOPERATOR ENDSWITHMATCH)

	;

pseudo
    : 	COLON (COLON)?
		( 	  IDENT
            | FUNCTION IDENT? RPAREN
            | LPAREN expr RPAREN
		)
    ;

declaration 
	: 	property COLON expr prio?
			-> ^(PROPERTY property ^(EXPRESSION expr prio?))
			
	;

prio 
	: 	IMPORTANT_SYM
			-> ^(IMPORTANT IMPORTANT_SYM)
	;

expr 
	:	term (operator term)*
			//-> ^(EXPRESSION term*)
	;

term
    : unaryOperator?
      (
             NUMBER
           | PERCENTAGE
           | LENGTH
           | EMS
           | EXS
           | ANGLE
           | TIME
           | FREQ
       )
    | STRING
    | IDENT
    | URI
    | hexColor
    | (FUNCTION (expr)? RPAREN)
    ;

hexColor
    : HASH
    ;


FUNCTION
	: 	IDENT LPAREN
	;

	
	

WS      		: (' '|'\t')+           { $channel = Hidden; } ;
NL      		: ('\r' '\n'? | '\n')   { $channel = Hidden; } ;
INCLUDES        : '~=';
DASHMATCH       : '|=';
CONTAINSMATCH   : '*=';
STARTSWITHMATCH : '^=';
ENDSWITHMATCH   : '$=';
GREATER         : '>';
LBRACE          : '{';
RBRACE          : '}';
LBRACKET        : '[';
RBRACKET        : ']';
OPEQ            : '=';
SEMI            : ';';
COLON           : ':';
SOLIDUS         : '/';
MINUS           : '-';
PLUS            : '+';
STAR            : '*';
LPAREN          : '(';
RPAREN          : ')';
COMMA           : ',';
DOT             : '.';
TILDE           : '~';
IDENT           : '-'? NMSTART (NMCHAR)*;
HASH            : '#' NAME;
IMPORT_SYM      : '@' I M P O R T;
PAGE_SYM        : '@' P A G E;
MEDIA_SYM       : '@' M E D I A;
CHARSET_SYM     : '@' C H A R S E T;
FONT_FACE_SYM   : '@' F O N T MINUS F A C E;
IMPORTANT_SYM   : '!' (WS|COMMENT)* I M P O R T A N T;
URI				:   U R L URL_ARGUMENTS;
URI_PREFIX		: U R L MINUS P R E F I X URL_ARGUMENTS;




STRING          : '\'' ( ~('\n'|'\r'|'\f'|'\'') )*
                    (
                          '\''
                        | { $type = INVALID; }
                    )

                | '"' ( ~('\n'|'\r'|'\f'|'"') )*
                    (
                          '"'
                        | { $type = INVALID; }
                    )
                ;

NUMBER
    :   (
              '0'..'9' ('.' '0'..'9'+)?
            | '.' '0'..'9'+
        )
        (
              (E (M|X))=>
                E
                (
                      M     { $type = EMS;          }
                    | X     { $type = EXS;          }
                )
            | ((I)?P(X|T|C))=>
                (I)?P
                (
                      X
                    | T
                    | C
                )
                            { $type = LENGTH;       }
            | (C M)=>
                C M         { $type = LENGTH;       }
            | (M (M|S))=>
                M
                (
                      M     { $type = LENGTH;       }

                    | S     { $type = TIME;         }
                )
            | (I N)=>
                I N         { $type = LENGTH;       }

            | (D E G)=>
                D E G       { $type = ANGLE;        }
            | (R A D)=>
                R A D       { $type = ANGLE;        }

            | (S)=>S        { $type = TIME;         }

            | (K? H Z)=>
                K? H    Z   { $type = FREQ;         }

            | IDENT         { $type = DIMENSION;    }

            | '%'           { $type = PERCENTAGE;   }

            | // Just a number
        )
    ;

	COMMENT	: '/*' ( options { greedy=false; } : .*) '*/'
                    {
                        $channel = 2; 
                    }
                ;

CDO             : '<!--'

                    {
                        $channel = 3; 
                    }
                ;

CDC             : '-->'

                    {
                        $channel = 4; 
                    }
                ;
				



fragment HEXCHAR     : ('a'..'f'|'A'..'F'|'0'..'9')  ;
fragment NONASCII    : '\u0080'..'\uFFFF'            ;   // NB:Upper bound should be \u4177777
fragment UNICODE     : '\\' HEXCHAR (HEXCHAR (HEXCHAR (HEXCHAR  (HEXCHAR HEXCHAR?)? )? )? )? ('\r'|'\n'|'\t'|'\f'|' ')*  ;
fragment ESCAPE      : UNICODE | '\\' ~('\r'|'\n'|'\f'|HEXCHAR)  ;
fragment NMSTART     : '_' | 'a'..'z' | 'A'..'Z' | NONASCII | ESCAPE ;
fragment NMCHAR      : '_' | 'a'..'z'  | 'A'..'Z'  | '0'..'9'  | '-' | NONASCII | ESCAPE ;
fragment NAME        : NMCHAR+   ;
fragment URL         : ('['|'!'|'#'|'$'|'%'|'&'|'*'|'-'|'~'| NONASCII| ESCAPE)* ;

fragment    INVALID :;
fragment    EMS         :;  // 'em'
fragment    EXS         :;  // 'ex'
fragment    LENGTH      :;  // 'px'. 'cm', 'mm', 'in'. 'pt', 'pc'
fragment    ANGLE       :;  // 'deg', 'rad', 'grad'
fragment    TIME        :;  // 'ms', 's'
fragment    FREQ        :;  // 'khz', 'hz'
fragment    DIMENSION   :;  // nnn'Somethingnotyetinvented'
fragment    PERCENTAGE  :;  // '%'
fragment URL_ARGUMENTS
:
        '('
            ((WS)=>WS)?
            (
              URL
              |STRING
            )
            WS?
        ')'
;
fragment  A : ('a'|'A') ('\r'|'\n'|'\t'|'\f'|' ')*  |  '\\' ('0' ('0' ('0' '0'?)?)?)? ('4'|'6')'1';
fragment  B : ('b'|'B') ('\r'|'\n'|'\t'|'\f'|' ')*  |  '\\' ('0' ('0' ('0' '0'?)?)?)? ('4'|'6')'2';
fragment  C : ('c'|'C') ('\r'|'\n'|'\t'|'\f'|' ')*  |  '\\' ('0' ('0' ('0' '0'?)?)?)? ('4'|'6')'3';
fragment  D : ('d'|'D') ('\r'|'\n'|'\t'|'\f'|' ')*  |  '\\' ('0' ('0' ('0' '0'?)?)?)? ('4'|'6')'4';
fragment  E : ('e'|'E') ('\r'|'\n'|'\t'|'\f'|' ')*  |  '\\' ('0' ('0' ('0' '0'?)?)?)? ('4'|'6')'5';
fragment  F : ('f'|'F') ('\r'|'\n'|'\t'|'\f'|' ')*  |  '\\' ('0' ('0' ('0' '0'?)?)?)? ('4'|'6')'6';
fragment  G : ('g'|'G') ('\r'|'\n'|'\t'|'\f'|' ')*  |  '\\' ('g' | 'G' | ('0' ('0' ('0' '0'?)?)?)? ('4'|'6')'7');
fragment  H : ('h'|'H') ('\r'|'\n'|'\t'|'\f'|' ')*  |  '\\' ('h' | 'H' | ('0' ('0' ('0' '0'?)?)?)? ('4'|'6')'8');
fragment  I : ('i'|'I') ('\r'|'\n'|'\t'|'\f'|' ')*  |  '\\' ('i' | 'I' | ('0' ('0' ('0' '0'?)?)?)? ('4'|'6')'9');
fragment  J : ('j'|'J') ('\r'|'\n'|'\t'|'\f'|' ')*  |  '\\' ('j' | 'J' | ('0' ('0' ('0' '0'?)?)?)? ('4'|'6')('A'|'a'));
fragment  K : ('k'|'K') ('\r'|'\n'|'\t'|'\f'|' ')*  |  '\\' ('k' | 'K' | ('0' ('0' ('0' '0'?)?)?)? ('4'|'6')('B'|'b'));
fragment  L : ('l'|'L') ('\r'|'\n'|'\t'|'\f'|' ')*  |  '\\' ('l' | 'L' | ('0' ('0' ('0' '0'?)?)?)? ('4'|'6')('C'|'c'));
fragment  M : ('m'|'M') ('\r'|'\n'|'\t'|'\f'|' ')*  |  '\\' ('m' | 'M' | ('0' ('0' ('0' '0'?)?)?)? ('4'|'6')('D'|'d'));
fragment  N : ('n'|'N') ('\r'|'\n'|'\t'|'\f'|' ')*  |  '\\' ('n' | 'N' | ('0' ('0' ('0' '0'?)?)?)? ('4'|'6')('E'|'e'));
fragment  O : ('o'|'O') ('\r'|'\n'|'\t'|'\f'|' ')*  |  '\\' ('o' | 'O' | ('0' ('0' ('0' '0'?)?)?)? ('4'|'6')('F'|'f'));
fragment  P : ('p'|'P') ('\r'|'\n'|'\t'|'\f'|' ')*  |  '\\' ('p' | 'P' | ('0' ('0' ('0' '0'?)?)?)? ('5'|'7')('0'));
fragment  Q : ('q'|'Q') ('\r'|'\n'|'\t'|'\f'|' ')*  |  '\\' ('q' | 'Q' | ('0' ('0' ('0' '0'?)?)?)? ('5'|'7')('1'));
fragment  R : ('r'|'R') ('\r'|'\n'|'\t'|'\f'|' ')*  |  '\\' ('r' | 'R' | ('0' ('0' ('0' '0'?)?)?)? ('5'|'7')('2'));
fragment  S : ('s'|'S') ('\r'|'\n'|'\t'|'\f'|' ')*  |  '\\' ('s' | 'S' | ('0' ('0' ('0' '0'?)?)?)? ('5'|'7')('3'));
fragment  T : ('t'|'T') ('\r'|'\n'|'\t'|'\f'|' ')*  |  '\\' ('t' | 'T' | ('0' ('0' ('0' '0'?)?)?)? ('5'|'7')('4'));
fragment  U : ('u'|'U') ('\r'|'\n'|'\t'|'\f'|' ')*  |  '\\' ('u' | 'U' | ('0' ('0' ('0' '0'?)?)?)? ('5'|'7')('5'));
fragment  V : ('v'|'V') ('\r'|'\n'|'\t'|'\f'|' ')*  |  '\\' ('v' | 'V' | ('0' ('0' ('0' '0'?)?)?)? ('5'|'7')('6'));
fragment  W : ('w'|'W') ('\r'|'\n'|'\t'|'\f'|' ')*  |  '\\' ('w' | 'W' | ('0' ('0' ('0' '0'?)?)?)? ('5'|'7')('7'));
fragment  X : ('x'|'X') ('\r'|'\n'|'\t'|'\f'|' ')*  |  '\\' ('x' | 'X' | ('0' ('0' ('0' '0'?)?)?)? ('5'|'7')('8'));
fragment  Y : ('y'|'Y') ('\r'|'\n'|'\t'|'\f'|' ')*  |  '\\' ('y' | 'Y' | ('0' ('0' ('0' '0'?)?)?)? ('5'|'7')('9'));
fragment  Z : ('z'|'Z') ('\r'|'\n'|'\t'|'\f'|' ')*  |  '\\' ('z' | 'Z' | ('0' ('0' ('0' '0'?)?)?)? ('5'|'7')('A'|'a'));