// created by jay 0.7 (c) 1998 Axel.Schreiner@informatik.uni-osnabrueck.de

#line 2 "cs-parser.jay"
//
// cs-parser.jay: The Parser for the C# compiler
//
// Authors: Miguel de Icaza (miguel@gnu.org)
//          Ravi Pratap     (ravi@ximian.com)
//          Marek Safar		(marek.safar@gmail.com)
//
// Licensed under the terms of the GNU GPL
//
// (C) 2001 Ximian, Inc (http://www.ximian.com)
// (C) 2004 Novell, Inc
//
// TODO:
//   (1) Figure out why error productions dont work.  `type-declaration' is a
//       great spot to put an `error' because you can reproduce it with this input:
//	 "public X { }"
//
// Possible optimization:
//   Run memory profiler with parsing only, and consider dropping 
//   arraylists where not needed.   Some pieces can use linked lists.
//
using System.Text;
using System.IO;
using System;

namespace Mono.CSharp
{
	using System.Collections;

	/// <summary>
	///    The C# Parser
	/// </summary>
	public class CSharpParser {
		NamespaceEntry  current_namespace;
		TypeContainer   current_container;
		DeclSpace	current_class;
	
		IAnonymousHost anonymous_host;

		/// <summary>
		///   Current block is used to add statements as we find
		///   them.  
		/// </summary>
		Block      current_block;

		Delegate   current_delegate;
		
		GenericMethod current_generic_method;
		AnonymousMethodExpression current_anonymous_method;

		/// <summary>
		///   This is used by the unary_expression code to resolve
		///   a name against a parameter.  
		/// </summary>
		Parameters current_local_parameters;

		/// <summary>
		///   Using during property parsing to describe the implicit
		///   value parameter that is passed to the "set" and "get"accesor
		///   methods (properties and indexers).
		/// </summary>
		Expression implicit_value_parameter_type;
		Parameters indexer_parameters;

		/// <summary>
		///   Hack to help create non-typed array initializer
		/// </summary>
		public static Expression current_array_type;
		Expression pushed_current_array_type;

		/// <summary>
		///   Used to determine if we are parsing the get/set pair
		///   of an indexer or a property
		/// </summmary>
		bool parsing_indexer;

		bool parsing_anonymous_method;

		///
		/// An out-of-band stack.
		///
		static Stack oob_stack;

		///
		/// Switch stack.
		///
		Stack switch_stack;

		static public int yacc_verbose_flag;

		///
		/// The current file.
		///
		SourceFile file;

		///
		/// Temporary Xml documentation cache.
		/// For enum types, we need one more temporary store.
		///
		string tmpComment;
		string enumTypeComment;
	       		
		/// Current attribute target
		string current_attr_target;
		
		/// assembly and module attribute definitions are enabled
		bool global_attrs_enabled = true;
		bool has_get, has_set;
		bool parameter_modifiers_not_allowed;
		bool params_modifiers_not_allowed;
		bool arglist_allowed;

		readonly CompilationUnit cu;
#line default

  /** error output stream.
      It should be changeable.
    */
  public System.IO.TextWriter ErrorOutput = System.Console.Out;

  /** simplified error message.
      @see <a href="#yyerror(java.lang.String, java.lang.String[])">yyerror</a>
    */
  public void yyerror (string message) {
    yyerror(message, null);
  }

  /** (syntax) error message.
      Can be overwritten to control message format.
      @param message text to be displayed.
      @param expected vector of acceptable tokens, if available.
    */
  public void yyerror (string message, string[] expected) {
    if ((yacc_verbose_flag > 0) && (expected != null) && (expected.Length  > 0)) {
      ErrorOutput.Write (message+", expecting");
      for (int n = 0; n < expected.Length; ++ n)
        ErrorOutput.Write (" "+expected[n]);
        ErrorOutput.WriteLine ();
    } else
      ErrorOutput.WriteLine (message);
  }

  /** debugging support, requires the package jay.yydebug.
      Set to null to suppress debugging messages.
    */
  internal yydebug.yyDebug debug;

  protected static  int yyFinal = 5;
 // Put this array into a separate class so it is only initialized if debugging is actually used
 // Use MarshalByRefObject to disable inlining
 class YYRules : MarshalByRefObject {
  public static  string [] yyRule = {
    "$accept : compilation_unit",
    "compilation_unit : outer_declarations opt_EOF",
    "compilation_unit : outer_declarations global_attributes opt_EOF",
    "compilation_unit : global_attributes opt_EOF",
    "compilation_unit : opt_EOF",
    "opt_EOF :",
    "opt_EOF : EOF",
    "outer_declarations : outer_declaration",
    "outer_declarations : outer_declarations outer_declaration",
    "outer_declaration : extern_alias_directive",
    "outer_declaration : using_directive",
    "outer_declaration : namespace_member_declaration",
    "extern_alias_directives : extern_alias_directive",
    "extern_alias_directives : extern_alias_directives extern_alias_directive",
    "extern_alias_directive : EXTERN IDENTIFIER IDENTIFIER SEMICOLON",
    "using_directives : using_directive",
    "using_directives : using_directives using_directive",
    "using_directive : using_alias_directive",
    "using_directive : using_namespace_directive",
    "using_alias_directive : USING IDENTIFIER ASSIGN namespace_or_type_name SEMICOLON",
    "using_alias_directive : USING error",
    "using_namespace_directive : USING namespace_name SEMICOLON",
    "$$1 :",
    "namespace_declaration : opt_attributes NAMESPACE namespace_or_type_name $$1 namespace_body opt_semicolon",
    "opt_semicolon :",
    "opt_semicolon : SEMICOLON",
    "opt_comma :",
    "opt_comma : COMMA",
    "namespace_name : namespace_or_type_name",
    "$$2 :",
    "namespace_body : OPEN_BRACE $$2 namespace_body_body",
    "namespace_body_body : opt_extern_alias_directives opt_using_directives opt_namespace_member_declarations CLOSE_BRACE",
    "$$3 :",
    "namespace_body_body : error $$3 CLOSE_BRACE",
    "namespace_body_body : opt_extern_alias_directives opt_using_directives opt_namespace_member_declarations EOF",
    "opt_using_directives :",
    "opt_using_directives : using_directives",
    "opt_extern_alias_directives :",
    "opt_extern_alias_directives : extern_alias_directives",
    "opt_namespace_member_declarations :",
    "opt_namespace_member_declarations : namespace_member_declarations",
    "namespace_member_declarations : namespace_member_declaration",
    "namespace_member_declarations : namespace_member_declarations namespace_member_declaration",
    "namespace_member_declaration : type_declaration",
    "namespace_member_declaration : namespace_declaration",
    "namespace_member_declaration : field_declaration",
    "namespace_member_declaration : method_declaration",
    "type_declaration : class_declaration",
    "type_declaration : struct_declaration",
    "type_declaration : interface_declaration",
    "type_declaration : enum_declaration",
    "type_declaration : delegate_declaration",
    "global_attributes : attribute_sections",
    "opt_attributes :",
    "opt_attributes : attribute_sections",
    "attribute_sections : attribute_section",
    "attribute_sections : attribute_sections attribute_section",
    "attribute_section : OPEN_BRACKET attribute_target_specifier attribute_list opt_comma CLOSE_BRACKET",
    "attribute_section : OPEN_BRACKET attribute_list opt_comma CLOSE_BRACKET",
    "attribute_target_specifier : attribute_target COLON",
    "attribute_target : IDENTIFIER",
    "attribute_target : EVENT",
    "attribute_target : RETURN",
    "attribute_target : error",
    "attribute_list : attribute",
    "attribute_list : attribute_list COMMA attribute",
    "attribute : attribute_name opt_attribute_arguments",
    "attribute_name : namespace_or_type_name",
    "opt_attribute_arguments :",
    "opt_attribute_arguments : OPEN_PARENS attribute_arguments CLOSE_PARENS",
    "attribute_arguments : opt_positional_argument_list",
    "attribute_arguments : positional_argument_list COMMA named_argument_list",
    "attribute_arguments : named_argument_list",
    "opt_positional_argument_list :",
    "opt_positional_argument_list : positional_argument_list",
    "positional_argument_list : expression",
    "positional_argument_list : positional_argument_list COMMA expression",
    "named_argument_list : named_argument",
    "named_argument_list : named_argument_list COMMA named_argument",
    "named_argument_list : named_argument_list COMMA expression",
    "named_argument : IDENTIFIER ASSIGN expression",
    "class_body : OPEN_BRACE opt_class_member_declarations CLOSE_BRACE",
    "opt_class_member_declarations :",
    "opt_class_member_declarations : class_member_declarations",
    "class_member_declarations : class_member_declaration",
    "class_member_declarations : class_member_declarations class_member_declaration",
    "class_member_declaration : constant_declaration",
    "class_member_declaration : field_declaration",
    "class_member_declaration : method_declaration",
    "class_member_declaration : property_declaration",
    "class_member_declaration : event_declaration",
    "class_member_declaration : indexer_declaration",
    "class_member_declaration : operator_declaration",
    "class_member_declaration : constructor_declaration",
    "class_member_declaration : destructor_declaration",
    "class_member_declaration : type_declaration",
    "$$4 :",
    "$$5 :",
    "$$6 :",
    "$$7 :",
    "struct_declaration : opt_attributes opt_modifiers opt_partial STRUCT $$4 type_name $$5 opt_class_base opt_type_parameter_constraints_clauses $$6 struct_body $$7 opt_semicolon",
    "struct_declaration : opt_attributes opt_modifiers opt_partial STRUCT error",
    "$$8 :",
    "struct_body : OPEN_BRACE $$8 opt_struct_member_declarations CLOSE_BRACE",
    "opt_struct_member_declarations :",
    "opt_struct_member_declarations : struct_member_declarations",
    "struct_member_declarations : struct_member_declaration",
    "struct_member_declarations : struct_member_declarations struct_member_declaration",
    "struct_member_declaration : constant_declaration",
    "struct_member_declaration : field_declaration",
    "struct_member_declaration : method_declaration",
    "struct_member_declaration : property_declaration",
    "struct_member_declaration : event_declaration",
    "struct_member_declaration : indexer_declaration",
    "struct_member_declaration : operator_declaration",
    "struct_member_declaration : constructor_declaration",
    "struct_member_declaration : type_declaration",
    "struct_member_declaration : destructor_declaration",
    "constant_declaration : opt_attributes opt_modifiers CONST type constant_declarators SEMICOLON",
    "constant_declarators : constant_declarator",
    "constant_declarators : constant_declarators COMMA constant_declarator",
    "constant_declarator : IDENTIFIER ASSIGN constant_expression",
    "constant_declarator : IDENTIFIER",
    "field_declaration : opt_attributes opt_modifiers type variable_declarators SEMICOLON",
    "field_declaration : opt_attributes opt_modifiers FIXED type fixed_variable_declarators SEMICOLON",
    "field_declaration : opt_attributes opt_modifiers FIXED type error",
    "field_declaration : opt_attributes opt_modifiers VOID variable_declarators SEMICOLON",
    "fixed_variable_declarators : fixed_variable_declarator",
    "fixed_variable_declarators : fixed_variable_declarators COMMA fixed_variable_declarator",
    "fixed_variable_declarator : IDENTIFIER OPEN_BRACKET expression CLOSE_BRACKET",
    "fixed_variable_declarator : IDENTIFIER OPEN_BRACKET CLOSE_BRACKET",
    "variable_declarators : variable_declarator",
    "variable_declarators : variable_declarators COMMA variable_declarator",
    "variable_declarator : IDENTIFIER ASSIGN variable_initializer",
    "variable_declarator : IDENTIFIER",
    "variable_declarator : IDENTIFIER OPEN_BRACKET opt_expression CLOSE_BRACKET",
    "variable_initializer : expression",
    "variable_initializer : array_initializer",
    "variable_initializer : STACKALLOC type OPEN_BRACKET expression CLOSE_BRACKET",
    "variable_initializer : ARGLIST",
    "variable_initializer : STACKALLOC type",
    "$$9 :",
    "method_declaration : method_header $$9 method_body",
    "opt_error_modifier :",
    "opt_error_modifier : modifiers",
    "open_parens : OPEN_PARENS",
    "open_parens : OPEN_PARENS_LAMBDA",
    "$$10 :",
    "$$11 :",
    "method_header : opt_attributes opt_modifiers type member_name open_parens $$10 opt_formal_parameter_list CLOSE_PARENS $$11 opt_type_parameter_constraints_clauses",
    "$$12 :",
    "$$13 :",
    "method_header : opt_attributes opt_modifiers VOID member_name open_parens $$12 opt_formal_parameter_list CLOSE_PARENS $$13 opt_type_parameter_constraints_clauses",
    "$$14 :",
    "method_header : opt_attributes opt_modifiers PARTIAL VOID member_name open_parens opt_formal_parameter_list CLOSE_PARENS $$14 opt_type_parameter_constraints_clauses",
    "method_header : opt_attributes opt_modifiers type modifiers member_name open_parens opt_formal_parameter_list CLOSE_PARENS",
    "method_body : block",
    "method_body : SEMICOLON",
    "opt_formal_parameter_list :",
    "opt_formal_parameter_list : formal_parameter_list",
    "opt_parameter_list_no_mod :",
    "$$15 :",
    "opt_parameter_list_no_mod : $$15 formal_parameter_list",
    "formal_parameter_list : fixed_parameters",
    "formal_parameter_list : fixed_parameters COMMA parameter_array",
    "formal_parameter_list : fixed_parameters COMMA arglist_modifier",
    "formal_parameter_list : parameter_array COMMA error",
    "formal_parameter_list : fixed_parameters COMMA parameter_array COMMA error",
    "formal_parameter_list : arglist_modifier COMMA error",
    "formal_parameter_list : fixed_parameters COMMA ARGLIST COMMA error",
    "formal_parameter_list : parameter_array",
    "formal_parameter_list : arglist_modifier",
    "fixed_parameters : fixed_parameter",
    "fixed_parameters : fixed_parameters COMMA fixed_parameter",
    "fixed_parameter : opt_attributes opt_parameter_modifier type IDENTIFIER",
    "fixed_parameter : opt_attributes opt_parameter_modifier type IDENTIFIER OPEN_BRACKET CLOSE_BRACKET",
    "fixed_parameter : opt_attributes opt_parameter_modifier type",
    "fixed_parameter : opt_attributes opt_parameter_modifier type error",
    "fixed_parameter : opt_attributes opt_parameter_modifier type IDENTIFIER ASSIGN constant_expression",
    "opt_parameter_modifier :",
    "opt_parameter_modifier : parameter_modifiers",
    "parameter_modifiers : parameter_modifier",
    "parameter_modifiers : parameter_modifiers parameter_modifier",
    "parameter_modifier : REF",
    "parameter_modifier : OUT",
    "parameter_modifier : THIS",
    "parameter_array : opt_attributes params_modifier type IDENTIFIER",
    "parameter_array : opt_attributes params_modifier type error",
    "params_modifier : PARAMS",
    "params_modifier : PARAMS parameter_modifier",
    "params_modifier : PARAMS params_modifier",
    "arglist_modifier : ARGLIST",
    "$$16 :",
    "$$17 :",
    "$$18 :",
    "property_declaration : opt_attributes opt_modifiers type namespace_or_type_name $$16 OPEN_BRACE $$17 accessor_declarations $$18 CLOSE_BRACE",
    "accessor_declarations : get_accessor_declaration",
    "accessor_declarations : get_accessor_declaration accessor_declarations",
    "accessor_declarations : set_accessor_declaration",
    "accessor_declarations : set_accessor_declaration accessor_declarations",
    "accessor_declarations : error",
    "$$19 :",
    "get_accessor_declaration : opt_attributes opt_modifiers GET $$19 accessor_body",
    "$$20 :",
    "set_accessor_declaration : opt_attributes opt_modifiers SET $$20 accessor_body",
    "accessor_body : block",
    "accessor_body : SEMICOLON",
    "$$21 :",
    "$$22 :",
    "$$23 :",
    "$$24 :",
    "interface_declaration : opt_attributes opt_modifiers opt_partial INTERFACE $$21 type_name $$22 opt_class_base opt_type_parameter_constraints_clauses $$23 interface_body $$24 opt_semicolon",
    "interface_declaration : opt_attributes opt_modifiers opt_partial INTERFACE error",
    "interface_body : OPEN_BRACE opt_interface_member_declarations CLOSE_BRACE",
    "opt_interface_member_declarations :",
    "opt_interface_member_declarations : interface_member_declarations",
    "interface_member_declarations : interface_member_declaration",
    "interface_member_declarations : interface_member_declarations interface_member_declaration",
    "interface_member_declaration : interface_method_declaration",
    "interface_member_declaration : interface_property_declaration",
    "interface_member_declaration : interface_event_declaration",
    "interface_member_declaration : interface_indexer_declaration",
    "interface_member_declaration : delegate_declaration",
    "interface_member_declaration : class_declaration",
    "interface_member_declaration : struct_declaration",
    "interface_member_declaration : enum_declaration",
    "interface_member_declaration : interface_declaration",
    "interface_member_declaration : constant_declaration",
    "opt_new : opt_modifiers",
    "$$25 :",
    "interface_method_declaration_body : OPEN_BRACE $$25 opt_statement_list CLOSE_BRACE",
    "interface_method_declaration_body : SEMICOLON",
    "$$26 :",
    "$$27 :",
    "interface_method_declaration : opt_attributes opt_new type namespace_or_type_name open_parens opt_formal_parameter_list CLOSE_PARENS $$26 opt_type_parameter_constraints_clauses $$27 interface_method_declaration_body",
    "$$28 :",
    "$$29 :",
    "interface_method_declaration : opt_attributes opt_new VOID namespace_or_type_name open_parens opt_formal_parameter_list CLOSE_PARENS $$28 opt_type_parameter_constraints_clauses $$29 interface_method_declaration_body",
    "$$30 :",
    "$$31 :",
    "interface_property_declaration : opt_attributes opt_new type IDENTIFIER OPEN_BRACE $$30 accessor_declarations $$31 CLOSE_BRACE",
    "interface_property_declaration : opt_attributes opt_new type error",
    "interface_event_declaration : opt_attributes opt_new EVENT type IDENTIFIER SEMICOLON",
    "interface_event_declaration : opt_attributes opt_new EVENT type error",
    "interface_event_declaration : opt_attributes opt_new EVENT type IDENTIFIER ASSIGN",
    "$$32 :",
    "$$33 :",
    "interface_event_declaration : opt_attributes opt_new EVENT type IDENTIFIER OPEN_BRACE $$32 event_accessor_declarations $$33 CLOSE_BRACE",
    "$$34 :",
    "$$35 :",
    "interface_indexer_declaration : opt_attributes opt_new type THIS OPEN_BRACKET opt_parameter_list_no_mod CLOSE_BRACKET OPEN_BRACE $$34 accessor_declarations $$35 CLOSE_BRACE",
    "$$36 :",
    "operator_declaration : opt_attributes opt_modifiers operator_declarator $$36 operator_body",
    "operator_body : block",
    "operator_body : SEMICOLON",
    "$$37 :",
    "operator_declarator : type OPERATOR overloadable_operator open_parens $$37 opt_parameter_list_no_mod CLOSE_PARENS",
    "operator_declarator : conversion_operator_declarator",
    "overloadable_operator : BANG",
    "overloadable_operator : TILDE",
    "overloadable_operator : OP_INC",
    "overloadable_operator : OP_DEC",
    "overloadable_operator : TRUE",
    "overloadable_operator : FALSE",
    "overloadable_operator : PLUS",
    "overloadable_operator : MINUS",
    "overloadable_operator : STAR",
    "overloadable_operator : DIV",
    "overloadable_operator : PERCENT",
    "overloadable_operator : BITWISE_AND",
    "overloadable_operator : BITWISE_OR",
    "overloadable_operator : CARRET",
    "overloadable_operator : OP_SHIFT_LEFT",
    "overloadable_operator : OP_SHIFT_RIGHT",
    "overloadable_operator : OP_EQ",
    "overloadable_operator : OP_NE",
    "overloadable_operator : OP_GT",
    "overloadable_operator : OP_LT",
    "overloadable_operator : OP_GE",
    "overloadable_operator : OP_LE",
    "$$38 :",
    "conversion_operator_declarator : IMPLICIT OPERATOR type open_parens $$38 opt_parameter_list_no_mod CLOSE_PARENS",
    "$$39 :",
    "conversion_operator_declarator : EXPLICIT OPERATOR type open_parens $$39 opt_parameter_list_no_mod CLOSE_PARENS",
    "conversion_operator_declarator : IMPLICIT error",
    "conversion_operator_declarator : EXPLICIT error",
    "constructor_declaration : opt_attributes opt_modifiers constructor_declarator constructor_body",
    "constructor_declarator : constructor_header",
    "constructor_declarator : constructor_header constructor_initializer",
    "$$40 :",
    "constructor_header : IDENTIFIER $$40 open_parens opt_formal_parameter_list CLOSE_PARENS",
    "constructor_body : block_prepared",
    "constructor_body : SEMICOLON",
    "constructor_initializer : COLON BASE open_parens opt_argument_list CLOSE_PARENS",
    "constructor_initializer : COLON THIS open_parens opt_argument_list CLOSE_PARENS",
    "constructor_initializer : COLON error",
    "opt_finalizer :",
    "opt_finalizer : UNSAFE",
    "opt_finalizer : EXTERN",
    "$$41 :",
    "destructor_declaration : opt_attributes opt_finalizer TILDE $$41 IDENTIFIER OPEN_PARENS CLOSE_PARENS block",
    "event_declaration : opt_attributes opt_modifiers EVENT type variable_declarators SEMICOLON",
    "$$42 :",
    "$$43 :",
    "event_declaration : opt_attributes opt_modifiers EVENT type namespace_or_type_name OPEN_BRACE $$42 event_accessor_declarations $$43 CLOSE_BRACE",
    "event_declaration : opt_attributes opt_modifiers EVENT type namespace_or_type_name error",
    "event_accessor_declarations : add_accessor_declaration remove_accessor_declaration",
    "event_accessor_declarations : remove_accessor_declaration add_accessor_declaration",
    "event_accessor_declarations : add_accessor_declaration",
    "event_accessor_declarations : remove_accessor_declaration",
    "event_accessor_declarations : error",
    "event_accessor_declarations :",
    "$$44 :",
    "add_accessor_declaration : opt_attributes ADD $$44 block",
    "add_accessor_declaration : opt_attributes ADD error",
    "add_accessor_declaration : opt_attributes modifiers ADD",
    "$$45 :",
    "remove_accessor_declaration : opt_attributes REMOVE $$45 block",
    "remove_accessor_declaration : opt_attributes REMOVE error",
    "remove_accessor_declaration : opt_attributes modifiers REMOVE",
    "$$46 :",
    "$$47 :",
    "indexer_declaration : opt_attributes opt_modifiers indexer_declarator OPEN_BRACE $$46 accessor_declarations $$47 CLOSE_BRACE",
    "indexer_declarator : type THIS OPEN_BRACKET opt_parameter_list_no_mod CLOSE_BRACKET",
    "indexer_declarator : type namespace_or_type_name DOT THIS OPEN_BRACKET opt_formal_parameter_list CLOSE_BRACKET",
    "$$48 :",
    "enum_declaration : opt_attributes opt_modifiers ENUM IDENTIFIER opt_enum_base $$48 enum_body opt_semicolon",
    "opt_enum_base :",
    "opt_enum_base : COLON type",
    "$$49 :",
    "$$50 :",
    "enum_body : OPEN_BRACE $$49 opt_enum_member_declarations $$50 CLOSE_BRACE",
    "opt_enum_member_declarations :",
    "opt_enum_member_declarations : enum_member_declarations opt_comma",
    "enum_member_declarations : enum_member_declaration",
    "enum_member_declarations : enum_member_declarations COMMA enum_member_declaration",
    "enum_member_declaration : opt_attributes IDENTIFIER",
    "$$51 :",
    "enum_member_declaration : opt_attributes IDENTIFIER $$51 ASSIGN expression",
    "$$52 :",
    "$$53 :",
    "delegate_declaration : opt_attributes opt_modifiers DELEGATE type type_name open_parens opt_formal_parameter_list CLOSE_PARENS $$52 opt_type_parameter_constraints_clauses $$53 SEMICOLON",
    "opt_nullable :",
    "opt_nullable : INTERR",
    "namespace_or_type_name : IDENTIFIER opt_type_argument_list",
    "namespace_or_type_name : IDENTIFIER DOUBLE_COLON IDENTIFIER opt_type_argument_list",
    "namespace_or_type_name : namespace_or_type_name DOT IDENTIFIER opt_type_argument_list",
    "member_name : IDENTIFIER opt_type_parameter_list",
    "member_name : namespace_or_type_name DOT IDENTIFIER opt_type_parameter_list",
    "type_name : IDENTIFIER opt_type_parameter_list",
    "opt_type_argument_list :",
    "opt_type_argument_list : OP_GENERICS_LT type_arguments OP_GENERICS_GT",
    "opt_type_parameter_list :",
    "opt_type_parameter_list : OP_GENERICS_LT type_arguments OP_GENERICS_GT",
    "type_arguments : type_argument",
    "type_arguments : type_arguments COMMA type_argument",
    "type_argument : type",
    "type_argument : attribute_sections type",
    "type : namespace_or_type_name opt_nullable",
    "type : builtin_types opt_nullable",
    "type : array_type",
    "type : pointer_type",
    "pointer_type : type STAR",
    "pointer_type : VOID STAR",
    "non_expression_type : builtin_types opt_nullable",
    "non_expression_type : non_expression_type rank_specifier",
    "non_expression_type : non_expression_type STAR",
    "non_expression_type : expression rank_specifiers",
    "non_expression_type : expression STAR",
    "non_expression_type : multiplicative_expression STAR",
    "type_list : type",
    "type_list : type_list COMMA type",
    "builtin_types : OBJECT",
    "builtin_types : STRING",
    "builtin_types : BOOL",
    "builtin_types : DECIMAL",
    "builtin_types : FLOAT",
    "builtin_types : DOUBLE",
    "builtin_types : integral_type",
    "integral_type : SBYTE",
    "integral_type : BYTE",
    "integral_type : SHORT",
    "integral_type : USHORT",
    "integral_type : INT",
    "integral_type : UINT",
    "integral_type : LONG",
    "integral_type : ULONG",
    "integral_type : CHAR",
    "integral_type : VOID",
    "array_type : type rank_specifiers opt_nullable",
    "primary_expression : literal",
    "primary_expression : type_name",
    "primary_expression : IDENTIFIER DOUBLE_COLON IDENTIFIER",
    "primary_expression : parenthesized_expression",
    "primary_expression : default_value_expression",
    "primary_expression : member_access",
    "primary_expression : invocation_expression",
    "primary_expression : element_access",
    "primary_expression : this_access",
    "primary_expression : base_access",
    "primary_expression : post_increment_expression",
    "primary_expression : post_decrement_expression",
    "primary_expression : new_expression",
    "primary_expression : typeof_expression",
    "primary_expression : sizeof_expression",
    "primary_expression : checked_expression",
    "primary_expression : unchecked_expression",
    "primary_expression : pointer_member_access",
    "primary_expression : anonymous_method_expression",
    "literal : boolean_literal",
    "literal : integer_literal",
    "literal : real_literal",
    "literal : LITERAL_CHARACTER",
    "literal : LITERAL_STRING",
    "literal : NULL",
    "real_literal : LITERAL_FLOAT",
    "real_literal : LITERAL_DOUBLE",
    "real_literal : LITERAL_DECIMAL",
    "integer_literal : LITERAL_INTEGER",
    "boolean_literal : TRUE",
    "boolean_literal : FALSE",
    "parenthesized_expression_0 : OPEN_PARENS expression CLOSE_PARENS",
    "parenthesized_expression_0 : OPEN_PARENS expression error",
    "parenthesized_expression : parenthesized_expression_0 CLOSE_PARENS_NO_CAST",
    "parenthesized_expression : parenthesized_expression_0 CLOSE_PARENS",
    "parenthesized_expression : parenthesized_expression_0 CLOSE_PARENS_MINUS",
    "member_access : primary_expression DOT IDENTIFIER opt_type_argument_list",
    "member_access : predefined_type DOT IDENTIFIER opt_type_argument_list",
    "predefined_type : builtin_types",
    "invocation_expression : primary_expression OPEN_PARENS opt_argument_list CLOSE_PARENS",
    "invocation_expression : parenthesized_expression_0 CLOSE_PARENS_OPEN_PARENS OPEN_PARENS CLOSE_PARENS",
    "invocation_expression : parenthesized_expression_0 CLOSE_PARENS_OPEN_PARENS primary_expression",
    "invocation_expression : parenthesized_expression_0 CLOSE_PARENS_OPEN_PARENS OPEN_PARENS non_simple_argument CLOSE_PARENS",
    "invocation_expression : parenthesized_expression_0 CLOSE_PARENS_OPEN_PARENS OPEN_PARENS argument_list COMMA argument CLOSE_PARENS",
    "opt_object_or_collection_initializer :",
    "opt_object_or_collection_initializer : object_or_collection_initializer",
    "object_or_collection_initializer : OPEN_BRACE opt_member_initializer_list CLOSE_BRACE",
    "object_or_collection_initializer : OPEN_BRACE member_initializer_list COMMA CLOSE_BRACE",
    "opt_member_initializer_list :",
    "opt_member_initializer_list : member_initializer_list",
    "member_initializer_list : member_initializer",
    "member_initializer_list : member_initializer_list COMMA member_initializer",
    "member_initializer : IDENTIFIER ASSIGN initializer_value",
    "member_initializer : non_assignment_expression",
    "member_initializer : OPEN_BRACE expression_list CLOSE_BRACE",
    "member_initializer : OPEN_BRACE CLOSE_BRACE",
    "initializer_value : expression",
    "initializer_value : object_or_collection_initializer",
    "opt_argument_list :",
    "opt_argument_list : argument_list",
    "argument_list : argument",
    "argument_list : argument_list COMMA argument",
    "argument_list : argument_list error",
    "argument : expression",
    "argument : non_simple_argument",
    "non_simple_argument : REF variable_reference",
    "non_simple_argument : OUT variable_reference",
    "non_simple_argument : ARGLIST OPEN_PARENS argument_list CLOSE_PARENS",
    "non_simple_argument : ARGLIST OPEN_PARENS CLOSE_PARENS",
    "non_simple_argument : ARGLIST",
    "variable_reference : expression",
    "element_access : primary_expression OPEN_BRACKET expression_list CLOSE_BRACKET",
    "element_access : primary_expression rank_specifiers",
    "expression_list : expression",
    "expression_list : expression_list COMMA expression",
    "this_access : THIS",
    "base_access : BASE DOT IDENTIFIER opt_type_argument_list",
    "base_access : BASE OPEN_BRACKET expression_list CLOSE_BRACKET",
    "base_access : BASE error",
    "post_increment_expression : primary_expression OP_INC",
    "post_decrement_expression : primary_expression OP_DEC",
    "new_expression : object_or_delegate_creation_expression",
    "new_expression : array_creation_expression",
    "new_expression : anonymous_type_expression",
    "object_or_delegate_creation_expression : NEW type OPEN_PARENS opt_argument_list CLOSE_PARENS opt_object_or_collection_initializer",
    "object_or_delegate_creation_expression : NEW type object_or_collection_initializer",
    "array_creation_expression : NEW type OPEN_BRACKET expression_list CLOSE_BRACKET opt_rank_specifier opt_array_initializer",
    "array_creation_expression : NEW type rank_specifiers array_initializer",
    "array_creation_expression : NEW rank_specifiers array_initializer",
    "array_creation_expression : NEW error",
    "array_creation_expression : NEW type error",
    "anonymous_type_expression : NEW OPEN_BRACE anonymous_type_parameters CLOSE_BRACE",
    "anonymous_type_parameters :",
    "anonymous_type_parameters : anonymous_type_parameter",
    "anonymous_type_parameters : anonymous_type_parameters COMMA anonymous_type_parameter",
    "anonymous_type_parameter : IDENTIFIER ASSIGN variable_initializer",
    "anonymous_type_parameter : IDENTIFIER",
    "anonymous_type_parameter : member_access",
    "anonymous_type_parameter : error",
    "opt_rank_specifier :",
    "opt_rank_specifier : rank_specifiers",
    "opt_rank_specifier_or_nullable :",
    "opt_rank_specifier_or_nullable : INTERR",
    "opt_rank_specifier_or_nullable : opt_nullable rank_specifiers",
    "opt_rank_specifier_or_nullable : opt_nullable rank_specifiers INTERR",
    "rank_specifiers : rank_specifier opt_rank_specifier",
    "rank_specifier : OPEN_BRACKET opt_dim_separators CLOSE_BRACKET",
    "opt_dim_separators :",
    "opt_dim_separators : dim_separators",
    "dim_separators : COMMA",
    "dim_separators : dim_separators COMMA",
    "opt_array_initializer :",
    "opt_array_initializer : array_initializer",
    "array_initializer : OPEN_BRACE CLOSE_BRACE",
    "array_initializer : OPEN_BRACE variable_initializer_list opt_comma CLOSE_BRACE",
    "variable_initializer_list : variable_initializer",
    "variable_initializer_list : variable_initializer_list COMMA variable_initializer",
    "$$54 :",
    "typeof_expression : TYPEOF $$54 OPEN_PARENS typeof_type_expression CLOSE_PARENS",
    "typeof_type_expression : type",
    "typeof_type_expression : unbound_type_name",
    "unbound_type_name : IDENTIFIER GENERIC_DIMENSION",
    "unbound_type_name : IDENTIFIER DOUBLE_COLON IDENTIFIER GENERIC_DIMENSION",
    "unbound_type_name : unbound_type_name DOT IDENTIFIER GENERIC_DIMENSION",
    "unbound_type_name : namespace_or_type_name DOT IDENTIFIER GENERIC_DIMENSION",
    "sizeof_expression : SIZEOF OPEN_PARENS type CLOSE_PARENS",
    "checked_expression : CHECKED OPEN_PARENS expression CLOSE_PARENS",
    "unchecked_expression : UNCHECKED OPEN_PARENS expression CLOSE_PARENS",
    "pointer_member_access : primary_expression OP_PTR IDENTIFIER",
    "$$55 :",
    "anonymous_method_expression : DELEGATE opt_anonymous_method_signature $$55 block",
    "opt_anonymous_method_signature :",
    "opt_anonymous_method_signature : anonymous_method_signature",
    "$$56 :",
    "anonymous_method_signature : open_parens $$56 opt_formal_parameter_list CLOSE_PARENS",
    "default_value_expression : DEFAULT_OPEN_PARENS type CLOSE_PARENS",
    "unary_expression : primary_expression",
    "unary_expression : BANG prefixed_unary_expression",
    "unary_expression : TILDE prefixed_unary_expression",
    "unary_expression : cast_expression",
    "cast_list : parenthesized_expression_0 CLOSE_PARENS_CAST unary_expression",
    "cast_list : parenthesized_expression_0 CLOSE_PARENS_NO_CAST default_value_expression",
    "cast_list : parenthesized_expression_0 CLOSE_PARENS_OPEN_PARENS cast_expression",
    "cast_expression : cast_list",
    "cast_expression : OPEN_PARENS non_expression_type CLOSE_PARENS prefixed_unary_expression",
    "prefixed_unary_expression : unary_expression",
    "prefixed_unary_expression : PLUS prefixed_unary_expression",
    "prefixed_unary_expression : MINUS prefixed_unary_expression",
    "prefixed_unary_expression : OP_INC prefixed_unary_expression",
    "prefixed_unary_expression : OP_DEC prefixed_unary_expression",
    "prefixed_unary_expression : STAR prefixed_unary_expression",
    "prefixed_unary_expression : BITWISE_AND prefixed_unary_expression",
    "multiplicative_expression : prefixed_unary_expression",
    "multiplicative_expression : multiplicative_expression STAR prefixed_unary_expression",
    "multiplicative_expression : multiplicative_expression DIV prefixed_unary_expression",
    "multiplicative_expression : multiplicative_expression PERCENT prefixed_unary_expression",
    "additive_expression : multiplicative_expression",
    "additive_expression : additive_expression PLUS multiplicative_expression",
    "additive_expression : additive_expression MINUS multiplicative_expression",
    "shift_expression : additive_expression",
    "shift_expression : shift_expression OP_SHIFT_LEFT additive_expression",
    "shift_expression : shift_expression OP_SHIFT_RIGHT additive_expression",
    "opt_error :",
    "opt_error : error",
    "nullable_type_or_conditional : type opt_error",
    "relational_expression : shift_expression",
    "relational_expression : relational_expression OP_LT shift_expression",
    "relational_expression : relational_expression OP_GT shift_expression",
    "relational_expression : relational_expression OP_LE shift_expression",
    "relational_expression : relational_expression OP_GE shift_expression",
    "$$57 :",
    "relational_expression : relational_expression IS $$57 nullable_type_or_conditional",
    "$$58 :",
    "relational_expression : relational_expression AS $$58 nullable_type_or_conditional",
    "equality_expression : relational_expression",
    "equality_expression : equality_expression OP_EQ relational_expression",
    "equality_expression : equality_expression OP_NE relational_expression",
    "and_expression : equality_expression",
    "and_expression : and_expression BITWISE_AND equality_expression",
    "exclusive_or_expression : and_expression",
    "exclusive_or_expression : exclusive_or_expression CARRET and_expression",
    "inclusive_or_expression : exclusive_or_expression",
    "inclusive_or_expression : inclusive_or_expression BITWISE_OR exclusive_or_expression",
    "conditional_and_expression : inclusive_or_expression",
    "conditional_and_expression : conditional_and_expression OP_AND inclusive_or_expression",
    "conditional_or_expression : conditional_and_expression",
    "conditional_or_expression : conditional_or_expression OP_OR conditional_and_expression",
    "conditional_expression : conditional_or_expression",
    "conditional_expression : conditional_or_expression INTERR expression COLON expression",
    "conditional_expression : conditional_or_expression OP_COALESCING expression",
    "conditional_expression : conditional_or_expression INTERR CLOSE_PARENS",
    "assignment_expression : prefixed_unary_expression ASSIGN expression",
    "assignment_expression : prefixed_unary_expression OP_MULT_ASSIGN expression",
    "assignment_expression : prefixed_unary_expression OP_DIV_ASSIGN expression",
    "assignment_expression : prefixed_unary_expression OP_MOD_ASSIGN expression",
    "assignment_expression : prefixed_unary_expression OP_ADD_ASSIGN expression",
    "assignment_expression : prefixed_unary_expression OP_SUB_ASSIGN expression",
    "assignment_expression : prefixed_unary_expression OP_SHIFT_LEFT_ASSIGN expression",
    "assignment_expression : prefixed_unary_expression OP_SHIFT_RIGHT_ASSIGN expression",
    "assignment_expression : prefixed_unary_expression OP_AND_ASSIGN expression",
    "assignment_expression : prefixed_unary_expression OP_OR_ASSIGN expression",
    "assignment_expression : prefixed_unary_expression OP_XOR_ASSIGN expression",
    "lambda_parameter_list : lambda_parameter",
    "lambda_parameter_list : lambda_parameter_list COMMA lambda_parameter",
    "lambda_parameter : parameter_modifier type IDENTIFIER",
    "lambda_parameter : type IDENTIFIER",
    "lambda_parameter : IDENTIFIER",
    "opt_lambda_parameter_list :",
    "opt_lambda_parameter_list : lambda_parameter_list",
    "$$59 :",
    "lambda_expression_body : $$59 expression",
    "lambda_expression_body : block",
    "$$60 :",
    "lambda_expression : IDENTIFIER ARROW $$60 lambda_expression_body",
    "$$61 :",
    "lambda_expression : OPEN_PARENS_LAMBDA opt_lambda_parameter_list CLOSE_PARENS ARROW $$61 lambda_expression_body",
    "expression : assignment_expression",
    "expression : non_assignment_expression",
    "non_assignment_expression : conditional_expression",
    "non_assignment_expression : lambda_expression",
    "non_assignment_expression : query_expression",
    "constant_expression : expression",
    "boolean_expression : expression",
    "$$62 :",
    "$$63 :",
    "$$64 :",
    "$$65 :",
    "class_declaration : opt_attributes opt_modifiers opt_partial CLASS $$62 type_name $$63 opt_class_base opt_type_parameter_constraints_clauses $$64 class_body $$65 opt_semicolon",
    "opt_partial :",
    "opt_partial : PARTIAL",
    "opt_modifiers :",
    "opt_modifiers : modifiers",
    "modifiers : modifier",
    "modifiers : modifiers modifier",
    "modifier : NEW",
    "modifier : PUBLIC",
    "modifier : PROTECTED",
    "modifier : INTERNAL",
    "modifier : PRIVATE",
    "modifier : ABSTRACT",
    "modifier : SEALED",
    "modifier : STATIC",
    "modifier : READONLY",
    "modifier : VIRTUAL",
    "modifier : OVERRIDE",
    "modifier : EXTERN",
    "modifier : VOLATILE",
    "modifier : UNSAFE",
    "opt_class_base :",
    "opt_class_base : class_base",
    "class_base : COLON type_list",
    "opt_type_parameter_constraints_clauses :",
    "opt_type_parameter_constraints_clauses : type_parameter_constraints_clauses",
    "type_parameter_constraints_clauses : type_parameter_constraints_clause",
    "type_parameter_constraints_clauses : type_parameter_constraints_clauses type_parameter_constraints_clause",
    "type_parameter_constraints_clause : WHERE IDENTIFIER COLON type_parameter_constraints",
    "type_parameter_constraints : type_parameter_constraint",
    "type_parameter_constraints : type_parameter_constraints COMMA type_parameter_constraint",
    "type_parameter_constraint : type",
    "type_parameter_constraint : NEW OPEN_PARENS CLOSE_PARENS",
    "type_parameter_constraint : CLASS",
    "type_parameter_constraint : STRUCT",
    "$$66 :",
    "block : OPEN_BRACE $$66 opt_statement_list CLOSE_BRACE",
    "$$67 :",
    "block_prepared : OPEN_BRACE $$67 opt_statement_list CLOSE_BRACE",
    "opt_statement_list :",
    "opt_statement_list : statement_list",
    "statement_list : statement",
    "statement_list : statement_list statement",
    "statement : declaration_statement",
    "statement : valid_declaration_statement",
    "statement : labeled_statement",
    "valid_declaration_statement : block",
    "valid_declaration_statement : empty_statement",
    "valid_declaration_statement : expression_statement",
    "valid_declaration_statement : selection_statement",
    "valid_declaration_statement : iteration_statement",
    "valid_declaration_statement : jump_statement",
    "valid_declaration_statement : try_statement",
    "valid_declaration_statement : checked_statement",
    "valid_declaration_statement : unchecked_statement",
    "valid_declaration_statement : lock_statement",
    "valid_declaration_statement : using_statement",
    "valid_declaration_statement : unsafe_statement",
    "valid_declaration_statement : fixed_statement",
    "embedded_statement : valid_declaration_statement",
    "embedded_statement : declaration_statement",
    "embedded_statement : labeled_statement",
    "empty_statement : SEMICOLON",
    "$$68 :",
    "labeled_statement : IDENTIFIER COLON $$68 statement",
    "declaration_statement : local_variable_declaration SEMICOLON",
    "declaration_statement : local_constant_declaration SEMICOLON",
    "local_variable_type : primary_expression opt_rank_specifier_or_nullable",
    "local_variable_type : builtin_types opt_rank_specifier_or_nullable",
    "local_variable_pointer_type : primary_expression STAR",
    "local_variable_pointer_type : builtin_types STAR",
    "local_variable_pointer_type : VOID STAR",
    "local_variable_pointer_type : local_variable_pointer_type STAR",
    "local_variable_declaration : local_variable_type variable_declarators",
    "local_variable_declaration : local_variable_pointer_type opt_rank_specifier_or_nullable variable_declarators",
    "local_constant_declaration : CONST local_variable_type constant_declarators",
    "expression_statement : statement_expression SEMICOLON",
    "statement_expression : expression",
    "statement_expression : error",
    "selection_statement : if_statement",
    "selection_statement : switch_statement",
    "if_statement : IF OPEN_PARENS boolean_expression CLOSE_PARENS embedded_statement",
    "if_statement : IF OPEN_PARENS boolean_expression CLOSE_PARENS embedded_statement ELSE embedded_statement",
    "$$69 :",
    "switch_statement : SWITCH OPEN_PARENS $$69 expression CLOSE_PARENS switch_block",
    "switch_block : OPEN_BRACE opt_switch_sections CLOSE_BRACE",
    "opt_switch_sections :",
    "opt_switch_sections : switch_sections",
    "switch_sections : switch_section",
    "switch_sections : switch_sections switch_section",
    "$$70 :",
    "switch_section : switch_labels $$70 statement_list",
    "switch_labels : switch_label",
    "switch_labels : switch_labels switch_label",
    "switch_label : CASE constant_expression COLON",
    "switch_label : DEFAULT_COLON",
    "switch_label : error",
    "iteration_statement : while_statement",
    "iteration_statement : do_statement",
    "iteration_statement : for_statement",
    "iteration_statement : foreach_statement",
    "while_statement : WHILE OPEN_PARENS boolean_expression CLOSE_PARENS embedded_statement",
    "do_statement : DO embedded_statement WHILE OPEN_PARENS boolean_expression CLOSE_PARENS SEMICOLON",
    "$$71 :",
    "for_statement : FOR open_parens opt_for_initializer SEMICOLON $$71 opt_for_condition SEMICOLON opt_for_iterator CLOSE_PARENS embedded_statement",
    "opt_for_initializer :",
    "opt_for_initializer : for_initializer",
    "for_initializer : local_variable_declaration",
    "for_initializer : statement_expression_list",
    "opt_for_condition :",
    "opt_for_condition : boolean_expression",
    "opt_for_iterator :",
    "opt_for_iterator : for_iterator",
    "for_iterator : statement_expression_list",
    "statement_expression_list : statement_expression",
    "statement_expression_list : statement_expression_list COMMA statement_expression",
    "foreach_statement : FOREACH open_parens type IN expression CLOSE_PARENS",
    "$$72 :",
    "foreach_statement : FOREACH open_parens type IDENTIFIER IN expression CLOSE_PARENS $$72 embedded_statement",
    "jump_statement : break_statement",
    "jump_statement : continue_statement",
    "jump_statement : goto_statement",
    "jump_statement : return_statement",
    "jump_statement : throw_statement",
    "jump_statement : yield_statement",
    "break_statement : BREAK SEMICOLON",
    "continue_statement : CONTINUE SEMICOLON",
    "goto_statement : GOTO IDENTIFIER SEMICOLON",
    "goto_statement : GOTO CASE constant_expression SEMICOLON",
    "goto_statement : GOTO DEFAULT SEMICOLON",
    "return_statement : RETURN opt_expression SEMICOLON",
    "throw_statement : THROW opt_expression SEMICOLON",
    "yield_statement : IDENTIFIER RETURN expression SEMICOLON",
    "yield_statement : IDENTIFIER RETURN SEMICOLON",
    "yield_statement : IDENTIFIER BREAK SEMICOLON",
    "opt_expression :",
    "opt_expression : expression",
    "try_statement : TRY block catch_clauses",
    "try_statement : TRY block FINALLY block",
    "try_statement : TRY block catch_clauses FINALLY block",
    "try_statement : TRY block error",
    "catch_clauses : catch_clause",
    "catch_clauses : catch_clauses catch_clause",
    "opt_identifier :",
    "opt_identifier : IDENTIFIER",
    "$$73 :",
    "catch_clause : CATCH opt_catch_args $$73 block",
    "opt_catch_args :",
    "opt_catch_args : catch_args",
    "catch_args : open_parens type opt_identifier CLOSE_PARENS",
    "checked_statement : CHECKED block",
    "unchecked_statement : UNCHECKED block",
    "$$74 :",
    "unsafe_statement : UNSAFE $$74 block",
    "$$75 :",
    "fixed_statement : FIXED open_parens type fixed_pointer_declarators CLOSE_PARENS $$75 embedded_statement",
    "fixed_pointer_declarators : fixed_pointer_declarator",
    "fixed_pointer_declarators : fixed_pointer_declarators COMMA fixed_pointer_declarator",
    "fixed_pointer_declarator : IDENTIFIER ASSIGN expression",
    "fixed_pointer_declarator : IDENTIFIER",
    "$$76 :",
    "lock_statement : LOCK OPEN_PARENS expression CLOSE_PARENS $$76 embedded_statement",
    "$$77 :",
    "using_statement : USING open_parens local_variable_declaration CLOSE_PARENS $$77 embedded_statement",
    "$$78 :",
    "using_statement : USING open_parens expression CLOSE_PARENS $$78 embedded_statement",
    "$$79 :",
    "query_expression : first_from_clause $$79 query_body",
    "first_from_clause : FROM IDENTIFIER IN expression",
    "first_from_clause : FROM type IDENTIFIER IN expression",
    "from_clause : FROM IDENTIFIER IN expression",
    "from_clause : FROM type IDENTIFIER IN expression",
    "query_body : opt_query_body_clauses select_or_group_clause opt_query_continuation",
    "select_or_group_clause : SELECT expression",
    "select_or_group_clause : GROUP expression BY expression",
    "opt_query_body_clauses :",
    "opt_query_body_clauses : query_body_clauses",
    "query_body_clauses : query_body_clause",
    "query_body_clauses : query_body_clauses query_body_clause",
    "query_body_clause : from_clause",
    "query_body_clause : let_clause",
    "query_body_clause : where_clause",
    "query_body_clause : join_clause",
    "query_body_clause : orderby_clause",
    "let_clause : LET IDENTIFIER ASSIGN expression",
    "where_clause : WHERE boolean_expression",
    "join_clause : JOIN IDENTIFIER IN expression ON expression EQUALS expression opt_join_into",
    "join_clause : JOIN type IDENTIFIER IN expression ON expression EQUALS expression opt_join_into",
    "opt_join_into :",
    "opt_join_into : INTO IDENTIFIER",
    "orderby_clause : ORDERBY orderings",
    "orderings : order_by",
    "orderings : order_by COMMA orderings_then_by",
    "orderings_then_by : then_by",
    "orderings_then_by : orderings_then_by COMMA then_by",
    "order_by : expression",
    "order_by : expression ASCENDING",
    "order_by : expression DESCENDING",
    "then_by : expression",
    "then_by : expression ASCENDING",
    "then_by : expression DESCENDING",
    "opt_query_continuation :",
    "$$80 :",
    "opt_query_continuation : INTO IDENTIFIER $$80 query_body",
  };
 public static string getRule (int index) {
    return yyRule [index];
 }
}
  protected static  string [] yyNames = {    
    "end-of-file",null,null,null,null,null,null,null,null,null,null,null,
    null,null,null,null,null,null,null,null,null,null,null,null,null,null,
    null,null,null,null,null,null,null,"'!'",null,null,null,"'%'","'&'",
    null,"'('","')'","'*'","'+'","','","'-'","'.'","'/'",null,null,null,
    null,null,null,null,null,null,null,"':'","';'","'<'","'='","'>'",
    "'?'",null,null,null,null,null,null,null,null,null,null,null,null,
    null,null,null,null,null,null,null,null,null,null,null,null,null,null,
    null,"'['",null,"']'","'^'",null,null,null,null,null,null,null,null,
    null,null,null,null,null,null,null,null,null,null,null,null,null,null,
    null,null,null,null,null,null,"'{'","'|'","'}'","'~'",null,null,null,
    null,null,null,null,null,null,null,null,null,null,null,null,null,null,
    null,null,null,null,null,null,null,null,null,null,null,null,null,null,
    null,null,null,null,null,null,null,null,null,null,null,null,null,null,
    null,null,null,null,null,null,null,null,null,null,null,null,null,null,
    null,null,null,null,null,null,null,null,null,null,null,null,null,null,
    null,null,null,null,null,null,null,null,null,null,null,null,null,null,
    null,null,null,null,null,null,null,null,null,null,null,null,null,null,
    null,null,null,null,null,null,null,null,null,null,null,null,null,null,
    null,null,null,null,null,null,null,null,null,null,null,null,null,null,
    null,"EOF","NONE","ERROR","FIRST_KEYWORD","ABSTRACT","AS","ADD",
    "ASSEMBLY","BASE","BOOL","BREAK","BYTE","CASE","CATCH","CHAR",
    "CHECKED","CLASS","CONST","CONTINUE","DECIMAL","DEFAULT","DELEGATE",
    "DO","DOUBLE","ELSE","ENUM","EVENT","EXPLICIT","EXTERN","FALSE",
    "FINALLY","FIXED","FLOAT","FOR","FOREACH","GOTO","IF","IMPLICIT","IN",
    "INT","INTERFACE","INTERNAL","IS","LOCK","LONG","NAMESPACE","NEW",
    "NULL","OBJECT","OPERATOR","OUT","OVERRIDE","PARAMS","PRIVATE",
    "PROTECTED","PUBLIC","READONLY","REF","RETURN","REMOVE","SBYTE",
    "SEALED","SHORT","SIZEOF","STACKALLOC","STATIC","STRING","STRUCT",
    "SWITCH","THIS","THROW","TRUE","TRY","TYPEOF","UINT","ULONG",
    "UNCHECKED","UNSAFE","USHORT","USING","VIRTUAL","VOID","VOLATILE",
    "WHERE","WHILE","ARGLIST","PARTIAL","ARROW","QUERY_FIRST_TOKEN",
    "FROM","JOIN","ON","EQUALS","SELECT","GROUP","BY","LET","ORDERBY",
    "ASCENDING","DESCENDING","INTO","QUERY_LAST_TOKEN","GET","\"get\"",
    "SET","\"set\"","LAST_KEYWORD","OPEN_BRACE","CLOSE_BRACE",
    "OPEN_BRACKET","CLOSE_BRACKET","OPEN_PARENS","CLOSE_PARENS","DOT",
    "COMMA","COLON","SEMICOLON","TILDE","PLUS","MINUS","BANG","ASSIGN",
    "OP_LT","OP_GENERICS_LT","OP_GT","OP_GENERICS_GT","BITWISE_AND",
    "BITWISE_OR","STAR","PERCENT","DIV","CARRET","INTERR","DOUBLE_COLON",
    "\"::\"","OP_INC","\"++\"","OP_DEC","\"--\"","OP_SHIFT_LEFT","\"<<\"",
    "OP_SHIFT_RIGHT","\">>\"","OP_LE","\"<=\"","OP_GE","\">=\"","OP_EQ",
    "\"==\"","OP_NE","\"!=\"","OP_AND","\"&&\"","OP_OR","\"||\"",
    "OP_MULT_ASSIGN","\"*=\"","OP_DIV_ASSIGN","\"/=\"","OP_MOD_ASSIGN",
    "\"%=\"","OP_ADD_ASSIGN","\"+=\"","OP_SUB_ASSIGN","\"-=\"",
    "OP_SHIFT_LEFT_ASSIGN","\"<<=\"","OP_SHIFT_RIGHT_ASSIGN","\">>=\"",
    "OP_AND_ASSIGN","\"&=\"","OP_XOR_ASSIGN","\"^=\"","OP_OR_ASSIGN",
    "\"|=\"","OP_PTR","\"->\"","OP_COALESCING","\"??\"","LITERAL_INTEGER",
    "\"int literal\"","LITERAL_FLOAT","\"float literal\"",
    "LITERAL_DOUBLE","\"double literal\"","LITERAL_DECIMAL",
    "\"decimal literal\"","LITERAL_CHARACTER","\"character literal\"",
    "LITERAL_STRING","\"string literal\"","IDENTIFIER",
    "OPEN_PARENS_LAMBDA","CLOSE_PARENS_CAST","CLOSE_PARENS_NO_CAST",
    "CLOSE_PARENS_OPEN_PARENS","CLOSE_PARENS_MINUS","DEFAULT_OPEN_PARENS",
    "GENERIC_DIMENSION","DEFAULT_COLON","LOWPREC","UMINUS","HIGHPREC",
  };

  /** index-checked interface to yyNames[].
      @param token single character or %token value.
      @return token name or [illegal] or [unknown].
    */
  public static string yyname (int token) {
    if ((token < 0) || (token > yyNames.Length)) return "[illegal]";
    string name;
    if ((name = yyNames[token]) != null) return name;
    return "[unknown]";
  }

  /** computes list of expected tokens on error by tracing the tables.
      @param state for which to compute the list.
      @return list of token names.
    */
  protected string[] yyExpecting (int state) {
    int token, n, len = 0;
    bool[] ok = new bool[yyNames.Length];

    if ((n = yySindex[state]) != 0)
      for (token = n < 0 ? -n : 0;
           (token < yyNames.Length) && (n+token < yyTable.Length); ++ token)
        if (yyCheck[n+token] == token && !ok[token] && yyNames[token] != null) {
          ++ len;
          ok[token] = true;
        }
    if ((n = yyRindex[state]) != 0)
      for (token = n < 0 ? -n : 0;
           (token < yyNames.Length) && (n+token < yyTable.Length); ++ token)
        if (yyCheck[n+token] == token && !ok[token] && yyNames[token] != null) {
          ++ len;
          ok[token] = true;
        }

    string [] result = new string[len];
    for (n = token = 0; n < len;  ++ token)
      if (ok[token]) result[n++] = yyNames[token];
    return result;
  }

  /** the generated parser, with debugging messages.
      Maintains a state and a value stack, currently with fixed maximum size.
      @param yyLex scanner.
      @param yydebug debug message writer implementing yyDebug, or null.
      @return result of the last reduction, if any.
      @throws yyException on irrecoverable parse error.
    */
  internal Object yyparse (yyParser.yyInput yyLex, Object yyd)
				 {
    this.debug = (yydebug.yyDebug)yyd;
    return yyparse(yyLex);
  }

  /** initial size and increment of the state/value stack [default 256].
      This is not final so that it can be overwritten outside of invocations
      of yyparse().
    */
  protected int yyMax;

  /** executed at the beginning of a reduce action.
      Used as $$ = yyDefault($1), prior to the user-specified action, if any.
      Can be overwritten to provide deep copy, etc.
      @param first value for $1, or null.
      @return first.
    */
  protected Object yyDefault (Object first) {
    return first;
  }

  /** the generated parser.
      Maintains a state and a value stack, currently with fixed maximum size.
      @param yyLex scanner.
      @return result of the last reduction, if any.
      @throws yyException on irrecoverable parse error.
    */
  internal Object yyparse (yyParser.yyInput yyLex)
  {
    if (yyMax <= 0) yyMax = 256;			// initial size
    int yyState = 0;                                   // state stack ptr
    int [] yyStates = new int[yyMax];	                // state stack 
    Object yyVal = null;                               // value stack ptr
    Object [] yyVals = new Object[yyMax];	        // value stack
    int yyToken = -1;					// current input
    int yyErrorFlag = 0;				// #tks to shift

    /*yyLoop:*/ for (int yyTop = 0;; ++ yyTop) {
      if (yyTop >= yyStates.Length) {			// dynamically increase
        int[] i = new int[yyStates.Length+yyMax];
        yyStates.CopyTo (i, 0);
        yyStates = i;
        Object[] o = new Object[yyVals.Length+yyMax];
        yyVals.CopyTo (o, 0);
        yyVals = o;
      }
      yyStates[yyTop] = yyState;
      yyVals[yyTop] = yyVal;
      if (debug != null) debug.push(yyState, yyVal);

      /*yyDiscarded:*/ for (;;) {	// discarding a token does not change stack
        int yyN;
        if ((yyN = yyDefRed[yyState]) == 0) {	// else [default] reduce (yyN)
          if (yyToken < 0) {
            yyToken = yyLex.advance() ? yyLex.token() : 0;
            if (debug != null)
              debug.lex(yyState, yyToken, yyname(yyToken), yyLex.value());
          }
          if ((yyN = yySindex[yyState]) != 0 && ((yyN += yyToken) >= 0)
              && (yyN < yyTable.Length) && (yyCheck[yyN] == yyToken)) {
            if (debug != null)
              debug.shift(yyState, yyTable[yyN], yyErrorFlag-1);
            yyState = yyTable[yyN];		// shift to yyN
            yyVal = yyLex.value();
            yyToken = -1;
            if (yyErrorFlag > 0) -- yyErrorFlag;
            goto continue_yyLoop;
          }
          if ((yyN = yyRindex[yyState]) != 0 && (yyN += yyToken) >= 0
              && yyN < yyTable.Length && yyCheck[yyN] == yyToken)
            yyN = yyTable[yyN];			// reduce (yyN)
          else
            switch (yyErrorFlag) {
  
            case 0:
              // yyerror(String.Format ("syntax error, got token `{0}'", yyname (yyToken)), yyExpecting(yyState));
              if (debug != null) debug.error("syntax error");
              goto case 1;
            case 1: case 2:
              yyErrorFlag = 3;
              do {
                if ((yyN = yySindex[yyStates[yyTop]]) != 0
                    && (yyN += Token.yyErrorCode) >= 0 && yyN < yyTable.Length
                    && yyCheck[yyN] == Token.yyErrorCode) {
                  if (debug != null)
                    debug.shift(yyStates[yyTop], yyTable[yyN], 3);
                  yyState = yyTable[yyN];
                  yyVal = yyLex.value();
                  goto continue_yyLoop;
                }
                if (debug != null) debug.pop(yyStates[yyTop]);
              } while (-- yyTop >= 0);
              if (debug != null) debug.reject();
              throw new yyParser.yyException("irrecoverable syntax error");
  
            case 3:
              if (yyToken == 0) {
                if (debug != null) debug.reject();
                throw new yyParser.yyException("irrecoverable syntax error at end-of-file");
              }
              if (debug != null)
                debug.discard(yyState, yyToken, yyname(yyToken),
  							yyLex.value());
              yyToken = -1;
              goto continue_yyDiscarded;		// leave stack alone
            }
        }
        int yyV = yyTop + 1-yyLen[yyN];
        if (debug != null)
          debug.reduce(yyState, yyStates[yyV-1], yyN, YYRules.getRule (yyN), yyLen[yyN]);
        yyVal = yyDefault(yyV > yyTop ? null : yyVals[yyV]);
        switch (yyN) {
case 5:
#line 334 "cs-parser.jay"
  {
		Lexer.check_incorrect_doc_comment ();
	  }
  break;
case 6:
#line 338 "cs-parser.jay"
  {
		Lexer.check_incorrect_doc_comment ();
	  }
  break;
case 14:
#line 360 "cs-parser.jay"
  {
		LocatedToken lt = (LocatedToken) yyVals[-2+yyTop];
		string s = lt.Value;
		if (s != "alias"){
			Report.Error (1003, lt.Location, "'alias' expected");
		} else if (RootContext.Version == LanguageVersion.ISO_1) {
			Report.FeatureIsNotAvailable (lt.Location, "external alias");
		} else {
			lt = (LocatedToken) yyVals[-1+yyTop]; 
			current_namespace.AddUsingExternalAlias (lt.Value, lt.Location);
		}
	  }
  break;
case 17:
#line 381 "cs-parser.jay"
  {
		if (RootContext.Documentation != null)
			Lexer.doc_state = XmlCommentState.Allowed;
	  }
  break;
case 18:
#line 386 "cs-parser.jay"
  {
		if (RootContext.Documentation != null)
			Lexer.doc_state = XmlCommentState.Allowed;
	  }
  break;
case 19:
#line 395 "cs-parser.jay"
  {
		LocatedToken lt = (LocatedToken) yyVals[-3+yyTop];
		current_namespace.AddUsingAlias (lt.Value, (MemberName) yyVals[-1+yyTop], (Location) yyVals[-4+yyTop]);
	  }
  break;
case 20:
#line 399 "cs-parser.jay"
  {
		CheckIdentifierToken (yyToken, GetLocation (yyVals[0+yyTop]));
	  }
  break;
case 21:
#line 406 "cs-parser.jay"
  {
		current_namespace.AddUsing ((MemberName) yyVals[-1+yyTop], (Location) yyVals[-2+yyTop]);
	  }
  break;
case 22:
#line 418 "cs-parser.jay"
  {
		MemberName name = (MemberName) yyVals[0+yyTop];

		if (yyVals[-2+yyTop] != null) {
			Report.Error(1671, name.Location, "A namespace declaration cannot have modifiers or attributes");
		}

		if (name.TypeArguments != null)
			syntax_error (lexer.Location, "namespace name expected");

		current_namespace = new NamespaceEntry (current_namespace, file, name.GetName ());
		cu.AddNamespace (current_namespace);		
		
		current_class = current_namespace.SlaveDeclSpace;
		current_container = current_class.PartialContainer;
	  }
  break;
case 23:
#line 435 "cs-parser.jay"
  { 
		current_namespace = current_namespace.Parent;
		current_class = current_namespace.SlaveDeclSpace;
		current_container = current_class.PartialContainer;
	  }
  break;
case 28:
#line 453 "cs-parser.jay"
  {
		MemberName name = (MemberName) yyVals[0+yyTop];

		if (name.TypeArguments != null)
			syntax_error (lexer.Location, "namespace name expected");

		yyVal = name;
	  }
  break;
case 29:
#line 465 "cs-parser.jay"
  {
		if (RootContext.Documentation != null)
			Lexer.doc_state = XmlCommentState.Allowed;
	  }
  break;
case 32:
#line 478 "cs-parser.jay"
  {
		Report.Error (1518, lexer.Location, "Expected `class', `delegate', `enum', `interface', or `struct'");
	  }
  break;
case 34:
#line 486 "cs-parser.jay"
  {
		Report.Error (1513, lexer.Location, "} expected");
	  }
  break;
case 43:
#line 513 "cs-parser.jay"
  {
		if (yyVals[0+yyTop] != null) {
			DeclSpace ds = (DeclSpace)yyVals[0+yyTop];

			if ((ds.ModFlags & (Modifiers.PRIVATE|Modifiers.PROTECTED)) != 0){
				Report.Error (1527, ds.Location, 
				"Namespace elements cannot be explicitly declared as private, protected or protected internal");
			}
		}
		current_namespace.DeclarationFound = true;
	  }
  break;
case 44:
#line 524 "cs-parser.jay"
  {
		current_namespace.DeclarationFound = true;
	  }
  break;
case 45:
#line 528 "cs-parser.jay"
  {
		Report.Error (116, ((MemberCore) yyVals[0+yyTop]).Location, "A namespace can only contain types and namespace declarations");
	  }
  break;
case 46:
#line 531 "cs-parser.jay"
  {
		Report.Error (116, ((MemberCore) yyVals[0+yyTop]).Location, "A namespace can only contain types and namespace declarations");
	  }
  break;
case 52:
#line 557 "cs-parser.jay"
  {
	if (yyVals[0+yyTop] != null) {
		Attributes attrs = (Attributes)yyVals[0+yyTop];
		if (global_attrs_enabled) {
			CodeGen.Assembly.AddAttributes (attrs.Attrs);
		} else {
			foreach (Attribute a in attrs.Attrs) {
				Report.Error (1730, a.Location, "Assembly and module attributes must precede all other elements except using clauses and extern alias declarations");
			}
		}
	}

	yyVal = yyVals[0+yyTop];
}
  break;
case 53:
#line 574 "cs-parser.jay"
  {
		global_attrs_enabled = false;
		yyVal = null;
      }
  break;
case 54:
#line 579 "cs-parser.jay"
  { 
		global_attrs_enabled = false;
		yyVal = yyVals[0+yyTop];
	  }
  break;
case 55:
#line 588 "cs-parser.jay"
  {
		if (current_attr_target != String.Empty) {
			ArrayList sect = (ArrayList) yyVals[0+yyTop];

			if (global_attrs_enabled) {
				if (current_attr_target == "module") {
					CodeGen.Module.AddAttributes (sect);
					yyVal = null;
				} else if (current_attr_target != null && current_attr_target.Length > 0) {
					CodeGen.Assembly.AddAttributes (sect);
					yyVal = null;
				} else {
					yyVal = new Attributes (sect);
				}
				if (yyVal == null) {
					if (RootContext.Documentation != null) {
						Lexer.check_incorrect_doc_comment ();
						Lexer.doc_state =
							XmlCommentState.Allowed;
					}
				}
			} else {
				yyVal = new Attributes (sect);
			}		
		}
		else
			yyVal = null;
		current_attr_target = null;
	  }
  break;
case 56:
#line 618 "cs-parser.jay"
  {
		if (current_attr_target != String.Empty) {
			Attributes attrs = yyVals[-1+yyTop] as Attributes;
			ArrayList sect = (ArrayList) yyVals[0+yyTop];

			if (global_attrs_enabled) {
				if (current_attr_target == "module") {
					CodeGen.Module.AddAttributes (sect);
					yyVal = null;
				} else if (current_attr_target == "assembly") {
					CodeGen.Assembly.AddAttributes (sect);
					yyVal = null;
				} else {
					if (attrs == null)
						attrs = new Attributes (sect);
					else
						attrs.AddAttributes (sect);			
				}
			} else {
				if (attrs == null)
					attrs = new Attributes (sect);
				else
					attrs.AddAttributes (sect);
			}		
			yyVal = attrs;
		}
		else
			yyVal = null;
		current_attr_target = null;
	  }
  break;
case 57:
#line 652 "cs-parser.jay"
  {
		yyVal = yyVals[-2+yyTop];
 	  }
  break;
case 58:
#line 656 "cs-parser.jay"
  {
		yyVal = yyVals[-2+yyTop];
	  }
  break;
case 59:
#line 663 "cs-parser.jay"
  {
		current_attr_target = (string)yyVals[-1+yyTop];
		yyVal = yyVals[-1+yyTop];
	  }
  break;
case 60:
#line 671 "cs-parser.jay"
  {
		LocatedToken lt = (LocatedToken) yyVals[0+yyTop];
		yyVal = CheckAttributeTarget (lt.Value, lt.Location);
	  }
  break;
case 61:
#line 675 "cs-parser.jay"
  { yyVal = "event"; }
  break;
case 62:
#line 676 "cs-parser.jay"
  { yyVal = "return"; }
  break;
case 63:
#line 678 "cs-parser.jay"
  {
  		string name = yyNames [yyToken].ToLower ();
		yyVal = CheckAttributeTarget (name, GetLocation (yyVals[0+yyTop]));
	  }
  break;
case 64:
#line 686 "cs-parser.jay"
  {
		ArrayList attrs = new ArrayList (4);
		attrs.Add (yyVals[0+yyTop]);

		yyVal = attrs;
	       
	  }
  break;
case 65:
#line 694 "cs-parser.jay"
  {
		ArrayList attrs = (ArrayList) yyVals[-2+yyTop];
		attrs.Add (yyVals[0+yyTop]);

		yyVal = attrs;
	  }
  break;
case 66:
#line 704 "cs-parser.jay"
  {
		MemberName mname = (MemberName) yyVals[-1+yyTop];
		if (mname.IsGeneric) {
			Report.Error (404, lexer.Location,
				      "'<' unexpected: attributes cannot be generic");
		}

		object [] arguments = (object []) yyVals[0+yyTop];
		MemberName left = mname.Left;
		string identifier = mname.Name;

		Expression left_expr = left == null ? null : left.GetTypeExpression ();

		if (current_attr_target == String.Empty)
			yyVal = null;
		else if (global_attrs_enabled && (current_attr_target == "assembly" || current_attr_target == "module"))
			/* FIXME: supply "nameEscaped" parameter here.*/
			yyVal = new GlobalAttribute (current_namespace, current_attr_target,
						  left_expr, identifier, arguments, mname.Location, lexer.IsEscapedIdentifier (mname.Location));
		else
			yyVal = new Attribute (current_attr_target, left_expr, identifier, arguments, mname.Location, lexer.IsEscapedIdentifier (mname.Location));
	  }
  break;
case 67:
#line 729 "cs-parser.jay"
  { /* reserved attribute name or identifier: 17.4 */ }
  break;
case 68:
#line 733 "cs-parser.jay"
  { yyVal = null; }
  break;
case 69:
#line 735 "cs-parser.jay"
  {
		yyVal = yyVals[-1+yyTop];
	  }
  break;
case 70:
#line 743 "cs-parser.jay"
  {
		if (yyVals[0+yyTop] == null)
			yyVal = null;
		else {
			yyVal = new object [] { yyVals[0+yyTop], null };
		}
	  }
  break;
case 71:
#line 751 "cs-parser.jay"
  {
		yyVal = new object[] { yyVals[-2+yyTop], yyVals[0+yyTop] };
	  }
  break;
case 72:
#line 755 "cs-parser.jay"
  {
		yyVal = new object [] { null, yyVals[0+yyTop] };
	  }
  break;
case 73:
#line 762 "cs-parser.jay"
  { yyVal = null; }
  break;
case 75:
#line 768 "cs-parser.jay"
  {
		ArrayList args = new ArrayList (4);
		args.Add (new Argument ((Expression) yyVals[0+yyTop], Argument.AType.Expression));

		yyVal = args;
	  }
  break;
case 76:
#line 775 "cs-parser.jay"
  {
		ArrayList args = (ArrayList) yyVals[-2+yyTop];
		args.Add (new Argument ((Expression) yyVals[0+yyTop], Argument.AType.Expression));

		yyVal = args;
	 }
  break;
case 77:
#line 785 "cs-parser.jay"
  {
		ArrayList args = new ArrayList (4);
		args.Add (yyVals[0+yyTop]);

		yyVal = args;
	  }
  break;
case 78:
#line 792 "cs-parser.jay"
  {	  
		ArrayList args = (ArrayList) yyVals[-2+yyTop];
		args.Add (yyVals[0+yyTop]);

		yyVal = args;
	  }
  break;
case 79:
#line 799 "cs-parser.jay"
  {
		  Report.Error (1016, ((Expression) yyVals[0+yyTop]).Location, "Named attribute argument expected");
		  yyVal = null;
		}
  break;
case 80:
#line 807 "cs-parser.jay"
  {
		/* FIXME: keep location*/
		yyVal = new DictionaryEntry (
			((LocatedToken) yyVals[-2+yyTop]).Value, 
			new Argument ((Expression) yyVals[0+yyTop], Argument.AType.Expression));
	  }
  break;
case 96:
#line 849 "cs-parser.jay"
  {
		lexer.ConstraintsParsing = true;
	  }
  break;
case 97:
#line 853 "cs-parser.jay"
  { 
		MemberName name = MakeName ((MemberName) yyVals[0+yyTop]);
		push_current_class (new Struct (current_namespace, current_class, name, (int) yyVals[-4+yyTop], (Attributes) yyVals[-5+yyTop]), yyVals[-3+yyTop]);
	  }
  break;
case 98:
#line 859 "cs-parser.jay"
  {
		lexer.ConstraintsParsing = false;

		current_class.SetParameterInfo ((ArrayList) yyVals[0+yyTop]);

		if (RootContext.Documentation != null)
			current_container.DocComment = Lexer.consume_doc_comment ();
	  }
  break;
case 99:
#line 868 "cs-parser.jay"
  {
		if (RootContext.Documentation != null)
			Lexer.doc_state = XmlCommentState.Allowed;
	  }
  break;
case 100:
#line 873 "cs-parser.jay"
  {
		yyVal = pop_current_class ();
	  }
  break;
case 101:
#line 876 "cs-parser.jay"
  {
		CheckIdentifierToken (yyToken, GetLocation (yyVals[0+yyTop]));
	  }
  break;
case 102:
#line 883 "cs-parser.jay"
  {
		if (RootContext.Documentation != null)
			Lexer.doc_state = XmlCommentState.Allowed;
	  }
  break;
case 118:
#line 925 "cs-parser.jay"
  {
		int modflags = (int) yyVals[-4+yyTop];
		foreach (VariableDeclaration constant in (ArrayList) yyVals[-1+yyTop]){
			Location l = constant.Location;
			if ((modflags & Modifiers.STATIC) != 0) {
				Report.Error (504, l, "The constant `{0}' cannot be marked static", current_container.GetSignatureForError () + '.' + (string) constant.identifier);
				continue;
			}

			Const c = new Const (
				current_class, (Expression) yyVals[-2+yyTop], (string) constant.identifier, 
				(Expression) constant.expression_or_array_initializer, modflags, 
				(Attributes) yyVals[-5+yyTop], l);

			if (RootContext.Documentation != null) {
				c.DocComment = Lexer.consume_doc_comment ();
				Lexer.doc_state = XmlCommentState.Allowed;
			}
			current_container.AddConstant (c);
		}
	  }
  break;
case 119:
#line 950 "cs-parser.jay"
  {
		ArrayList constants = new ArrayList (4);
		if (yyVals[0+yyTop] != null)
			constants.Add (yyVals[0+yyTop]);
		yyVal = constants;
	  }
  break;
case 120:
#line 957 "cs-parser.jay"
  {
		if (yyVals[0+yyTop] != null) {
			ArrayList constants = (ArrayList) yyVals[-2+yyTop];
			constants.Add (yyVals[0+yyTop]);
		}
	  }
  break;
case 121:
#line 967 "cs-parser.jay"
  {
		yyVal = new VariableDeclaration ((LocatedToken) yyVals[-2+yyTop], yyVals[0+yyTop]);
	  }
  break;
case 122:
#line 971 "cs-parser.jay"
  {
		/* A const field requires a value to be provided*/
		Report.Error (145, ((LocatedToken) yyVals[0+yyTop]).Location, "A const field requires a value to be provided");
		yyVal = null;
	  }
  break;
case 123:
#line 984 "cs-parser.jay"
  { 
		Expression type = (Expression) yyVals[-2+yyTop];
		int mod = (int) yyVals[-3+yyTop];

		current_array_type = null;

		foreach (VariableDeclaration var in (ArrayList) yyVals[-1+yyTop]){
			Field field = new Field (current_class, type, mod, var.identifier, 
						 (Attributes) yyVals[-4+yyTop], var.Location);

			field.Initializer = var.expression_or_array_initializer;

			if (RootContext.Documentation != null) {
				field.DocComment = Lexer.consume_doc_comment ();
				Lexer.doc_state = XmlCommentState.Allowed;
			}
			current_container.AddField (field);
			yyVal = field; /* FIXME: might be better if it points to the top item*/
		}
	  }
  break;
case 124:
#line 1010 "cs-parser.jay"
  { 
			Expression type = (Expression) yyVals[-2+yyTop];
			int mod = (int) yyVals[-4+yyTop];

			current_array_type = null;

			foreach (VariableDeclaration var in (ArrayList) yyVals[-1+yyTop]) {
				FixedField field = new FixedField (current_class, type, mod, var.identifier,
					(Expression)var.expression_or_array_initializer, (Attributes) yyVals[-5+yyTop], var.Location);

				if (RootContext.Documentation != null) {
					field.DocComment = Lexer.consume_doc_comment ();
					Lexer.doc_state = XmlCommentState.Allowed;
				}
				current_container.AddField (field);
				yyVal = field; /* FIXME: might be better if it points to the top item*/
			}
	  }
  break;
case 125:
#line 1033 "cs-parser.jay"
  {
		Report.Error (1641, GetLocation (yyVals[-1+yyTop]), "A fixed size buffer field must have the array size specifier after the field name");
	  }
  break;
case 126:
#line 1040 "cs-parser.jay"
  {
		current_array_type = null;
		Report.Error (670, (Location) yyVals[-2+yyTop], "Fields cannot have void type");
	  }
  break;
case 127:
#line 1048 "cs-parser.jay"
  {
		ArrayList decl = new ArrayList (2);
		decl.Add (yyVals[0+yyTop]);
		yyVal = decl;
  	  }
  break;
case 128:
#line 1054 "cs-parser.jay"
  {
		ArrayList decls = (ArrayList) yyVals[-2+yyTop];
		decls.Add (yyVals[0+yyTop]);
		yyVal = yyVals[-2+yyTop];
	  }
  break;
case 129:
#line 1063 "cs-parser.jay"
  {
		yyVal = new VariableDeclaration ((LocatedToken) yyVals[-3+yyTop], yyVals[-1+yyTop]);
	  }
  break;
case 130:
#line 1067 "cs-parser.jay"
  {
		Report.Error (443, lexer.Location, "Value or constant expected");
		yyVal = new VariableDeclaration ((LocatedToken) yyVals[-2+yyTop], null);
	  }
  break;
case 131:
#line 1075 "cs-parser.jay"
  {
		ArrayList decl = new ArrayList (4);
		if (yyVals[0+yyTop] != null)
			decl.Add (yyVals[0+yyTop]);
		yyVal = decl;
	  }
  break;
case 132:
#line 1082 "cs-parser.jay"
  {
		ArrayList decls = (ArrayList) yyVals[-2+yyTop];
		decls.Add (yyVals[0+yyTop]);
		yyVal = yyVals[-2+yyTop];
	  }
  break;
case 133:
#line 1091 "cs-parser.jay"
  {
		yyVal = new VariableDeclaration ((LocatedToken) yyVals[-2+yyTop], yyVals[0+yyTop]);
	  }
  break;
case 134:
#line 1095 "cs-parser.jay"
  {
		yyVal = new VariableDeclaration ((LocatedToken) yyVals[0+yyTop], null);
	  }
  break;
case 135:
#line 1099 "cs-parser.jay"
  {
		Report.Error (650, ((LocatedToken) yyVals[-3+yyTop]).Location, "Syntax error, bad array declarator. To declare a managed array the rank specifier precedes the variable's identifier. " +
			"To declare a fixed size buffer field, use the fixed keyword before the field type");
		yyVal = null;
	  }
  break;
case 136:
#line 1108 "cs-parser.jay"
  {
		yyVal = yyVals[0+yyTop];
	  }
  break;
case 137:
#line 1112 "cs-parser.jay"
  {
		yyVal = yyVals[0+yyTop];
	  }
  break;
case 138:
#line 1116 "cs-parser.jay"
  {
		yyVal = new StackAlloc ((Expression) yyVals[-3+yyTop], (Expression) yyVals[-1+yyTop], (Location) yyVals[-4+yyTop]);
	  }
  break;
case 139:
#line 1120 "cs-parser.jay"
  {
		yyVal = new ArglistAccess ((Location) yyVals[0+yyTop]);
	  }
  break;
case 140:
#line 1124 "cs-parser.jay"
  {
		Report.Error (1575, (Location) yyVals[-1+yyTop], "A stackalloc expression requires [] after type");
                yyVal = null;
	  }
  break;
case 141:
#line 1131 "cs-parser.jay"
  {
		anonymous_host = (IAnonymousHost) yyVals[0+yyTop];
		if (RootContext.Documentation != null)
			Lexer.doc_state = XmlCommentState.NotAllowed;
	  }
  break;
case 142:
#line 1137 "cs-parser.jay"
  {
		Method method = (Method) yyVals[-2+yyTop];
		method.Block = (ToplevelBlock) yyVals[0+yyTop];
		current_container.AddMethod (method);

		anonymous_host = null;
		current_generic_method = null;
		current_local_parameters = null;

		if (RootContext.Documentation != null)
			Lexer.doc_state = XmlCommentState.Allowed;
	  }
  break;
case 144:
#line 1154 "cs-parser.jay"
  {
		int m = (int) yyVals[0+yyTop];
		int i = 1;

		while (m != 0){
			if ((i & m) != 0){
				Report.Error (1585, lexer.Location,
					"Member modifier `{0}' must precede the member type and name",
					Modifiers.Name (i));
			}
			m &= ~i;
			i = i << 1;
		}
	  }
  break;
case 147:
#line 1186 "cs-parser.jay"
  {
		arglist_allowed = true;
	  }
  break;
case 148:
#line 1190 "cs-parser.jay"
  {
		lexer.ConstraintsParsing = true;
	  }
  break;
case 149:
#line 1194 "cs-parser.jay"
  {
		lexer.ConstraintsParsing = false;
		arglist_allowed = false;
		MemberName name = (MemberName) yyVals[-6+yyTop];
		current_local_parameters = (Parameters) yyVals[-3+yyTop];

		if (yyVals[0+yyTop] != null && name.TypeArguments == null)
			Report.Error (80, lexer.Location,
				      "Constraints are not allowed on non-generic declarations");

		Method method;

		GenericMethod generic = null;
		if (name.TypeArguments != null) {
			generic = new GenericMethod (current_namespace, current_class, name,
						     (Expression) yyVals[-7+yyTop], current_local_parameters);

			generic.SetParameterInfo ((ArrayList) yyVals[0+yyTop]);
		}

		method = new Method (current_class, generic, (Expression) yyVals[-7+yyTop], (int) yyVals[-8+yyTop], false,
				     name, current_local_parameters, (Attributes) yyVals[-9+yyTop]);

		anonymous_host = method;
		current_generic_method = generic;

		if (RootContext.Documentation != null)
			method.DocComment = Lexer.consume_doc_comment ();

		yyVal = method;
	  }
  break;
case 150:
#line 1229 "cs-parser.jay"
  {
		arglist_allowed = true;
	  }
  break;
case 151:
#line 1233 "cs-parser.jay"
  {
		lexer.ConstraintsParsing = true;
	  }
  break;
case 152:
#line 1237 "cs-parser.jay"
  {
		lexer.ConstraintsParsing = false;
		arglist_allowed = false;

		MemberName name = (MemberName) yyVals[-6+yyTop];
		current_local_parameters = (Parameters) yyVals[-3+yyTop];

		if (yyVals[0+yyTop] != null && name.TypeArguments == null)
			Report.Error (80, lexer.Location,
				      "Constraints are not allowed on non-generic declarations");

		Method method;
		GenericMethod generic = null;
		if (name.TypeArguments != null) {
			generic = new GenericMethod (current_namespace, current_class, name,
						     TypeManager.system_void_expr, current_local_parameters);

			generic.SetParameterInfo ((ArrayList) yyVals[0+yyTop]);
		}

		method = new Method (current_class, generic, TypeManager.system_void_expr,
				     (int) yyVals[-8+yyTop], false, name, current_local_parameters, (Attributes) yyVals[-9+yyTop]);

		anonymous_host = method;
		current_generic_method = generic;

		if (RootContext.Documentation != null)
			method.DocComment = Lexer.consume_doc_comment ();

		yyVal = method;
	}
  break;
case 153:
#line 1273 "cs-parser.jay"
  {
		lexer.ConstraintsParsing = true;
	  }
  break;
case 154:
#line 1277 "cs-parser.jay"
  {
		lexer.ConstraintsParsing = false;

		MemberName name = (MemberName) yyVals[-5+yyTop];
		current_local_parameters = (Parameters) yyVals[-3+yyTop];

		if (yyVals[-1+yyTop] != null && name.TypeArguments == null)
			Report.Error (80, lexer.Location,
				      "Constraints are not allowed on non-generic declarations");

		Method method;
		GenericMethod generic = null;
		if (name.TypeArguments != null) {
			generic = new GenericMethod (current_namespace, current_class, name,
						     TypeManager.system_void_expr, current_local_parameters);

			generic.SetParameterInfo ((ArrayList) yyVals[0+yyTop]);
		}

		int modifiers = (int) yyVals[-8+yyTop];


		const int invalid_partial_mod = Modifiers.Accessibility | Modifiers.ABSTRACT | Modifiers.EXTERN |
			Modifiers.NEW | Modifiers.OVERRIDE | Modifiers.SEALED | Modifiers.VIRTUAL;

		if ((modifiers & invalid_partial_mod) != 0) {
			Report.Error (750, name.Location, "A partial method cannot define access modifier or " +
       			"any of abstract, extern, new, override, sealed, or virtual modifiers");
			modifiers &= ~invalid_partial_mod;
		}

		if ((current_class.ModFlags & Modifiers.PARTIAL) == 0) {
			Report.Error (751, name.Location, "A partial method must be declared within a " +
       			"partial class or partial struct");
		}

		modifiers |= Modifiers.PARTIAL | Modifiers.PRIVATE;
		
		method = new Method (current_class, generic, TypeManager.system_void_expr,
				     modifiers, false, name, current_local_parameters, (Attributes) yyVals[-9+yyTop]);

		anonymous_host = method;
		current_generic_method = generic;

		if (RootContext.Documentation != null)
			method.DocComment = Lexer.consume_doc_comment ();

		yyVal = method;
	  }
  break;
case 155:
#line 1330 "cs-parser.jay"
  {
		MemberName name = (MemberName) yyVals[-3+yyTop];
		Report.Error (1585, name.Location, 
			"Member modifier `{0}' must precede the member type and name", Modifiers.Name ((int) yyVals[-4+yyTop]));

		Method method = new Method (current_class, null, TypeManager.system_void_expr,
					    0, false, name, (Parameters) yyVals[-1+yyTop], (Attributes) yyVals[-7+yyTop]);

		current_local_parameters = (Parameters) yyVals[-1+yyTop];

		if (RootContext.Documentation != null)
			method.DocComment = Lexer.consume_doc_comment ();

		yyVal = null;
	  }
  break;
case 157:
#line 1349 "cs-parser.jay"
  { yyVal = null; }
  break;
case 158:
#line 1353 "cs-parser.jay"
  { yyVal = Parameters.EmptyReadOnlyParameters; }
  break;
case 160:
#line 1358 "cs-parser.jay"
  { yyVal = Parameters.EmptyReadOnlyParameters; }
  break;
case 161:
#line 1360 "cs-parser.jay"
  {
		parameter_modifiers_not_allowed = true;
	  }
  break;
case 162:
#line 1364 "cs-parser.jay"
  {
		parameter_modifiers_not_allowed = false;
		yyVal = yyVals[0+yyTop];
	  }
  break;
case 163:
#line 1372 "cs-parser.jay"
  { 
		ArrayList pars_list = (ArrayList) yyVals[0+yyTop];

		Parameter [] pars = new Parameter [pars_list.Count];
		pars_list.CopyTo (pars);

	  	yyVal = new Parameters (pars); 
	  }
  break;
case 164:
#line 1381 "cs-parser.jay"
  {
		ArrayList pars_list = (ArrayList) yyVals[-2+yyTop];
		pars_list.Add (yyVals[0+yyTop]);

		Parameter [] pars = new Parameter [pars_list.Count];
		pars_list.CopyTo (pars);

		yyVal = new Parameters (pars); 
	  }
  break;
case 165:
#line 1391 "cs-parser.jay"
  {
		ArrayList pars_list = (ArrayList) yyVals[-2+yyTop];
		/*pars_list.Add (new ArglistParameter (GetLocation ($3)));*/

		Parameter [] pars = new Parameter [pars_list.Count];
		pars_list.CopyTo (pars);

		yyVal = new Parameters (pars, true);
	  }
  break;
case 166:
#line 1401 "cs-parser.jay"
  {
		if (yyVals[-2+yyTop] != null)
			Report.Error (231, ((Parameter) yyVals[-2+yyTop]).Location, "A params parameter must be the last parameter in a formal parameter list");
		yyVal = null;
	  }
  break;
case 167:
#line 1407 "cs-parser.jay"
  {
		if (yyVals[-2+yyTop] != null)
			Report.Error (231, ((Parameter) yyVals[-2+yyTop]).Location, "A params parameter must be the last parameter in a formal parameter list");
		yyVal = null;
	  }
  break;
case 168:
#line 1413 "cs-parser.jay"
  {
		Report.Error (257, (Location) yyVals[-2+yyTop], "An __arglist parameter must be the last parameter in a formal parameter list");
		yyVal = null;
	  }
  break;
case 169:
#line 1418 "cs-parser.jay"
  {
		Report.Error (257, (Location) yyVals[-2+yyTop], "An __arglist parameter must be the last parameter in a formal parameter list");
		yyVal = null;
	  }
  break;
case 170:
#line 1423 "cs-parser.jay"
  {
		yyVal = new Parameters (new Parameter[] { (Parameter) yyVals[0+yyTop] } );
	  }
  break;
case 171:
#line 1427 "cs-parser.jay"
  {
		yyVal = new Parameters (new Parameter[0], true);
	  }
  break;
case 172:
#line 1434 "cs-parser.jay"
  {
		ArrayList pars = new ArrayList (4);

		pars.Add (yyVals[0+yyTop]);
		yyVal = pars;
	  }
  break;
case 173:
#line 1441 "cs-parser.jay"
  {
		ArrayList pars = (ArrayList) yyVals[-2+yyTop];
		Parameter p = (Parameter)yyVals[0+yyTop];
		if (p != null) {
			if ((p.modFlags & Parameter.Modifier.This) != 0)
				Report.Error (1100, p.Location, "The parameter modifier `this' can only be used on the first parameter");
			pars.Add (p);
		}
		yyVal = yyVals[-2+yyTop];
	  }
  break;
case 174:
#line 1458 "cs-parser.jay"
  {
		LocatedToken lt = (LocatedToken) yyVals[0+yyTop];
		yyVal = new Parameter ((Expression) yyVals[-1+yyTop], lt.Value, (Parameter.Modifier) yyVals[-2+yyTop], (Attributes) yyVals[-3+yyTop], lt.Location);
	  }
  break;
case 175:
#line 1466 "cs-parser.jay"
  {
		LocatedToken lt = (LocatedToken) yyVals[-2+yyTop];
		Report.Error (1552, lt.Location, "Array type specifier, [], must appear before parameter name");
		yyVal = null;
	  }
  break;
case 176:
#line 1474 "cs-parser.jay"
  {
		Report.Error (1001, GetLocation (yyVals[0+yyTop]), "Identifier expected");
		yyVal = null;
	  }
  break;
case 177:
#line 1481 "cs-parser.jay"
  {
		CheckIdentifierToken (yyToken, GetLocation (yyVals[0+yyTop]));
		yyVal = null;
	  }
  break;
case 178:
#line 1491 "cs-parser.jay"
  {
		LocatedToken lt = (LocatedToken) yyVals[-2+yyTop];
		Report.Error (241, lt.Location, "Default parameter specifiers are not permitted");
		 yyVal = null;
	   }
  break;
case 179:
#line 1499 "cs-parser.jay"
  { yyVal = Parameter.Modifier.NONE; }
  break;
case 181:
#line 1505 "cs-parser.jay"
  {
		yyVal = yyVals[0+yyTop];
	  }
  break;
case 182:
#line 1509 "cs-parser.jay"
  {
		Parameter.Modifier p2 = (Parameter.Modifier)yyVals[0+yyTop];
  		Parameter.Modifier mod = (Parameter.Modifier)yyVals[-1+yyTop] | p2;
  		if (((Parameter.Modifier)yyVals[-1+yyTop] & p2) == p2) {
  			Error_DuplicateParameterModifier (lexer.Location, p2);
  		} else {
	  		switch (mod & ~Parameter.Modifier.This) {
  				case Parameter.Modifier.REF:
					Report.Error (1101, lexer.Location, "The parameter modifiers `this' and `ref' cannot be used altogether");
  					break;
   				case Parameter.Modifier.OUT:
					Report.Error (1102, lexer.Location, "The parameter modifiers `this' and `out' cannot be used altogether");
  					break;
  				default:
 					Report.Error (1108, lexer.Location, "A parameter cannot have specified more than one modifier");
 					break;
 			}
  		}
  		yyVal = mod;
	  }
  break;
case 183:
#line 1533 "cs-parser.jay"
  {
	  	if (parameter_modifiers_not_allowed)
	  		Error_ParameterModifierNotValid ("ref", (Location)yyVals[0+yyTop]);
	  		
	  	yyVal = Parameter.Modifier.REF;
	  }
  break;
case 184:
#line 1540 "cs-parser.jay"
  {
	  	if (parameter_modifiers_not_allowed)
	  		Error_ParameterModifierNotValid ("out", (Location)yyVals[0+yyTop]);
	  
	  	yyVal = Parameter.Modifier.OUT;
	  }
  break;
case 185:
#line 1547 "cs-parser.jay"
  {
		if (parameter_modifiers_not_allowed)
	  		Error_ParameterModifierNotValid ("this", (Location)yyVals[0+yyTop]);

	  	if (RootContext.Version <= LanguageVersion.ISO_2)
	  		Report.FeatureIsNotAvailable (GetLocation (yyVals[0+yyTop]), "extension methods");
	  			
		yyVal = Parameter.Modifier.This;
	  }
  break;
case 186:
#line 1560 "cs-parser.jay"
  { 
		LocatedToken lt = (LocatedToken) yyVals[0+yyTop];
		yyVal = new ParamsParameter ((Expression) yyVals[-1+yyTop], lt.Value, (Attributes) yyVals[-3+yyTop], lt.Location);
	  }
  break;
case 187:
#line 1564 "cs-parser.jay"
  {
		CheckIdentifierToken (yyToken, GetLocation (yyVals[0+yyTop]));
		yyVal = null;
	  }
  break;
case 188:
#line 1572 "cs-parser.jay"
  {
		if (params_modifiers_not_allowed)
			Report.Error (1670, ((Location) yyVals[0+yyTop]), "The `params' modifier is not allowed in current context");
	  }
  break;
case 189:
#line 1577 "cs-parser.jay"
  {
		Parameter.Modifier mod = (Parameter.Modifier)yyVals[0+yyTop];
		if ((mod & Parameter.Modifier.This) != 0) {
			Report.Error (1104, (Location)yyVals[-1+yyTop], "The parameter modifiers `this' and `params' cannot be used altogether");
		} else {
			Report.Error (1611, (Location)yyVals[-1+yyTop], "The params parameter cannot be declared as ref or out");
		}	  
	  }
  break;
case 190:
#line 1586 "cs-parser.jay"
  {
		Error_DuplicateParameterModifier ((Location)yyVals[-1+yyTop], Parameter.Modifier.PARAMS);
	  }
  break;
case 191:
#line 1593 "cs-parser.jay"
  {
	  	if (!arglist_allowed)
	  		Report.Error (1669, (Location) yyVals[0+yyTop], "__arglist is not valid in this context");
	  }
  break;
case 192:
#line 1604 "cs-parser.jay"
  {
		if (RootContext.Documentation != null)
			tmpComment = Lexer.consume_doc_comment ();
	  }
  break;
case 193:
#line 1609 "cs-parser.jay"
  {
		implicit_value_parameter_type = (Expression) yyVals[-3+yyTop];

		lexer.PropertyParsing = true;
	  }
  break;
case 194:
#line 1615 "cs-parser.jay"
  {
		lexer.PropertyParsing = false;
		has_get = has_set = false;
	  }
  break;
case 195:
#line 1620 "cs-parser.jay"
  { 
		if (yyVals[-2+yyTop] == null)
			break;

		Property prop;
		Accessors accessors = (Accessors) yyVals[-2+yyTop];
		Accessor get_block = accessors.get_or_add;
		Accessor set_block = accessors.set_or_remove;

		MemberName name = (MemberName) yyVals[-6+yyTop];

		if (name.TypeArguments != null)
			syntax_error (lexer.Location, "a property can't have type arguments");

		prop = new Property (current_class, (Expression) yyVals[-7+yyTop], (int) yyVals[-8+yyTop], false,
				     name, (Attributes) yyVals[-9+yyTop], get_block, set_block, accessors.declared_in_reverse, current_block);

		current_container.AddProperty (prop);
		implicit_value_parameter_type = null;

		if (RootContext.Documentation != null)
			prop.DocComment = ConsumeStoredComment ();

	  }
  break;
case 196:
#line 1648 "cs-parser.jay"
  {
		yyVal = new Accessors ((Accessor) yyVals[0+yyTop], null);
	 }
  break;
case 197:
#line 1652 "cs-parser.jay"
  { 
		Accessors accessors = (Accessors) yyVals[0+yyTop];
		accessors.get_or_add = (Accessor) yyVals[-1+yyTop];
		yyVal = accessors;
	 }
  break;
case 198:
#line 1658 "cs-parser.jay"
  {
		yyVal = new Accessors (null, (Accessor) yyVals[0+yyTop]);
	 }
  break;
case 199:
#line 1662 "cs-parser.jay"
  { 
		Accessors accessors = (Accessors) yyVals[0+yyTop];
		accessors.set_or_remove = (Accessor) yyVals[-1+yyTop];
		accessors.declared_in_reverse = true;
		yyVal = accessors;
	 }
  break;
case 200:
#line 1669 "cs-parser.jay"
  {
		Report.Error (1014, GetLocation (yyVals[0+yyTop]), "A get or set accessor expected");
		yyVal = null;
	  }
  break;
case 201:
#line 1677 "cs-parser.jay"
  {
		/* If this is not the case, then current_local_parameters has already*/
		/* been set in indexer_declaration*/
		if (parsing_indexer == false)
			current_local_parameters = null;
		else 
			current_local_parameters = indexer_parameters;
		lexer.PropertyParsing = false;

		anonymous_host = SimpleAnonymousHost.GetSimple ();
	  }
  break;
case 202:
#line 1689 "cs-parser.jay"
  {
		if (has_get) {
			Report.Error (1007, (Location) yyVals[-2+yyTop], "Property accessor already defined");
			break;
		}
		Accessor accessor = new Accessor ((ToplevelBlock) yyVals[0+yyTop], (int) yyVals[-3+yyTop], (Attributes) yyVals[-4+yyTop], (Location) yyVals[-2+yyTop]);
		has_get = true;
		current_local_parameters = null;
		lexer.PropertyParsing = true;

		SimpleAnonymousHost.Simple.Propagate (accessor);
		anonymous_host = null;

		if (RootContext.Documentation != null)
			if (Lexer.doc_state == XmlCommentState.Error)
				Lexer.doc_state = XmlCommentState.NotAllowed;

		yyVal = accessor;
	  }
  break;
case 203:
#line 1712 "cs-parser.jay"
  {
		Parameter [] args;
		Parameter implicit_value_parameter = new Parameter (
			implicit_value_parameter_type, "value", 
			Parameter.Modifier.NONE, null, (Location) yyVals[0+yyTop]);

		if (parsing_indexer == false) {
			args  = new Parameter [1];
			args [0] = implicit_value_parameter;
			current_local_parameters = new Parameters (args);
		} else {
			Parameter [] fpars = indexer_parameters.FixedParameters;

			if (fpars != null){
				int count = fpars.Length;

				args = new Parameter [count + 1];
				fpars.CopyTo (args, 0);
				args [count] = implicit_value_parameter;
			} else 
				args = null;
			current_local_parameters = new Parameters (
				args);
		}
		
		lexer.PropertyParsing = false;

		anonymous_host = SimpleAnonymousHost.GetSimple ();
	  }
  break;
case 204:
#line 1742 "cs-parser.jay"
  {
		if (has_set) {
			Report.Error (1007, ((LocatedToken) yyVals[-2+yyTop]).Location, "Property accessor already defined");
			break;
		}
		Accessor accessor = new Accessor ((ToplevelBlock) yyVals[0+yyTop], (int) yyVals[-3+yyTop], (Attributes) yyVals[-4+yyTop], (Location) yyVals[-2+yyTop]);
		has_set = true;
		current_local_parameters = null;
		lexer.PropertyParsing = true;

		SimpleAnonymousHost.Simple.Propagate (accessor);
		anonymous_host = null;

		if (RootContext.Documentation != null
			&& Lexer.doc_state == XmlCommentState.Error)
			Lexer.doc_state = XmlCommentState.NotAllowed;

		yyVal = accessor;
	  }
  break;
case 206:
#line 1765 "cs-parser.jay"
  { yyVal = null; }
  break;
case 207:
#line 1773 "cs-parser.jay"
  {
		lexer.ConstraintsParsing = true;
	  }
  break;
case 208:
#line 1777 "cs-parser.jay"
  {
		MemberName name = MakeName ((MemberName) yyVals[0+yyTop]);
		push_current_class (new Interface (current_namespace, current_class, name, (int) yyVals[-4+yyTop], (Attributes) yyVals[-5+yyTop]), yyVals[-3+yyTop]);
	  }
  break;
case 209:
#line 1783 "cs-parser.jay"
  {
		lexer.ConstraintsParsing = false;

		current_class.SetParameterInfo ((ArrayList) yyVals[0+yyTop]);

		if (RootContext.Documentation != null) {
			current_container.DocComment = Lexer.consume_doc_comment ();
			Lexer.doc_state = XmlCommentState.Allowed;
		}
	  }
  break;
case 210:
#line 1794 "cs-parser.jay"
  {
		if (RootContext.Documentation != null)
			Lexer.doc_state = XmlCommentState.Allowed;
	  }
  break;
case 211:
#line 1799 "cs-parser.jay"
  {
		yyVal = pop_current_class ();
	  }
  break;
case 212:
#line 1802 "cs-parser.jay"
  {
		CheckIdentifierToken (yyToken, GetLocation (yyVals[0+yyTop]));
	  }
  break;
case 218:
#line 1825 "cs-parser.jay"
  { 
		if (yyVals[0+yyTop] == null)
			break;

		Method m = (Method) yyVals[0+yyTop];

		if (m.IsExplicitImpl)
		        Report.Error (541, m.Location, "`{0}': explicit interface declaration can only be declared in a class or struct",
				m.GetSignatureForError ());

		current_container.AddMethod (m);

		if (RootContext.Documentation != null)
			Lexer.doc_state = XmlCommentState.Allowed;
	  }
  break;
case 219:
#line 1841 "cs-parser.jay"
  { 
		if (yyVals[0+yyTop] == null)
			break;

		Property p = (Property) yyVals[0+yyTop];

		if (p.IsExplicitImpl)
		        Report.Error (541, p.Location, "`{0}': explicit interface declaration can only be declared in a class or struct",
				p.GetSignatureForError ());

		current_container.AddProperty (p);

		if (RootContext.Documentation != null)
			Lexer.doc_state = XmlCommentState.Allowed;
	  }
  break;
case 220:
#line 1857 "cs-parser.jay"
  { 
		if (yyVals[0+yyTop] != null){
			Event e = (Event) yyVals[0+yyTop];

			if (e.IsExplicitImpl)
		        Report.Error (541, e.Location, "`{0}': explicit interface declaration can only be declared in a class or struct",
				e.GetSignatureForError ());
			
			current_container.AddEvent (e);
		}

		if (RootContext.Documentation != null)
			Lexer.doc_state = XmlCommentState.Allowed;
	  }
  break;
case 221:
#line 1872 "cs-parser.jay"
  { 
		if (yyVals[0+yyTop] == null)
			break;

		Indexer i = (Indexer) yyVals[0+yyTop];

		if (i.IsExplicitImpl)
		        Report.Error (541, i.Location, "`{0}': explicit interface declaration can only be declared in a class or struct",
				i.GetSignatureForError ());

		current_container.AddIndexer (i);

		if (RootContext.Documentation != null)
			Lexer.doc_state = XmlCommentState.Allowed;
	  }
  break;
case 222:
#line 1888 "cs-parser.jay"
  {
		if (yyVals[0+yyTop] != null) {
			Report.Error (524, GetLocation (yyVals[0+yyTop]), "`{0}': Interfaces cannot declare classes, structs, interfaces, delegates, enumerations or constants",
				((MemberCore)yyVals[0+yyTop]).GetSignatureForError ());
		}
	  }
  break;
case 223:
#line 1895 "cs-parser.jay"
  {
		if (yyVals[0+yyTop] != null) {
			Report.Error (524, GetLocation (yyVals[0+yyTop]), "`{0}': Interfaces cannot declare classes, structs, interfaces, delegates, enumerations or constants",
				((MemberCore)yyVals[0+yyTop]).GetSignatureForError ());
		}
	  }
  break;
case 224:
#line 1902 "cs-parser.jay"
  {
		if (yyVals[0+yyTop] != null) {
			Report.Error (524, GetLocation (yyVals[0+yyTop]), "`{0}': Interfaces cannot declare classes, structs, interfaces, delegates, enumerations or constants",
				((MemberCore)yyVals[0+yyTop]).GetSignatureForError ());
		}
	  }
  break;
case 225:
#line 1909 "cs-parser.jay"
  {
		if (yyVals[0+yyTop] != null) {
			Report.Error (524, GetLocation (yyVals[0+yyTop]), "`{0}': Interfaces cannot declare classes, structs, interfaces, delegates, enumerations or constants",
				((MemberCore)yyVals[0+yyTop]).GetSignatureForError ());
		}
	  }
  break;
case 226:
#line 1916 "cs-parser.jay"
  {
		if (yyVals[0+yyTop] != null) {
			Report.Error (524, GetLocation (yyVals[0+yyTop]), "`{0}': Interfaces cannot declare classes, structs, interfaces, delegates, enumerations or constants",
				((MemberCore)yyVals[0+yyTop]).GetSignatureForError ());
		}
	  }
  break;
case 227:
#line 1923 "cs-parser.jay"
  {
		Report.Error (525, GetLocation (yyVals[0+yyTop]), "Interfaces cannot contain fields or constants");
	  }
  break;
case 228:
#line 1930 "cs-parser.jay"
  {
		int val = (int) yyVals[0+yyTop];
		val = Modifiers.Check (Modifiers.NEW | Modifiers.UNSAFE, val, 0, GetLocation (yyVals[0+yyTop]));
		yyVal = val;
	  }
  break;
case 229:
#line 1939 "cs-parser.jay"
  {
		Report.Error (531, (Location)yyVals[0+yyTop],
			      "`{0}.{1}{2}': interface members cannot have a definition",
			      current_class.GetSignatureForError (),
			      ((MemberName) yyVals[-1+yyTop]).GetSignatureForError (),
			      ((Parameters)yyVals[-5+yyTop]).GetSignatureForError ());
	  
		lexer.ConstraintsParsing = false;
	  }
  break;
case 230:
#line 1949 "cs-parser.jay"
  {
		yyVal = null;
	  }
  break;
case 232:
#line 1958 "cs-parser.jay"
  {
		lexer.ConstraintsParsing = true;
	  }
  break;
case 233:
#line 1962 "cs-parser.jay"
  {
		/* Refer to the name as $-1 in interface_method_declaration_body	  */
		yyVal = yyVals[-5+yyTop];
	  }
  break;
case 234:
#line 1967 "cs-parser.jay"
  {
		lexer.ConstraintsParsing = false;

		MemberName name = (MemberName) yyVals[-7+yyTop];

		if (yyVals[-2+yyTop] != null && name.TypeArguments == null)
			Report.Error (80, lexer.Location,
				      "Constraints are not allowed on non-generic declarations");

		GenericMethod generic = null;
		if (name.TypeArguments != null) {
			generic = new GenericMethod (current_namespace, current_class, name,
						     (Expression) yyVals[-8+yyTop], (Parameters) yyVals[-5+yyTop]);

			generic.SetParameterInfo ((ArrayList) yyVals[-2+yyTop]);
		}

		yyVal = new Method (current_class, generic, (Expression) yyVals[-8+yyTop], (int) yyVals[-9+yyTop], true, name,
				 (Parameters) yyVals[-5+yyTop], (Attributes) yyVals[-10+yyTop]);
		if (RootContext.Documentation != null)
			((Method) yyVal).DocComment = Lexer.consume_doc_comment ();
	  }
  break;
case 235:
#line 1991 "cs-parser.jay"
  {
		lexer.ConstraintsParsing = true;
	  }
  break;
case 236:
#line 1995 "cs-parser.jay"
  {
		yyVal = yyVals[-5+yyTop];
	  }
  break;
case 237:
#line 1999 "cs-parser.jay"
  {
		lexer.ConstraintsParsing = false;

		MemberName name = (MemberName) yyVals[-7+yyTop];

		if (yyVals[-2+yyTop] != null && name.TypeArguments == null)
			Report.Error (80, lexer.Location,
				      "Constraints are not allowed on non-generic declarations");

		GenericMethod generic = null;
		if (name.TypeArguments != null) {
			generic = new GenericMethod (current_namespace, current_class, name,
						     TypeManager.system_void_expr, (Parameters) yyVals[-5+yyTop]);

			generic.SetParameterInfo ((ArrayList) yyVals[-2+yyTop]);
		}

		yyVal = new Method (current_class, generic, TypeManager.system_void_expr, (int) yyVals[-9+yyTop],
				 true, name, (Parameters) yyVals[-5+yyTop], (Attributes) yyVals[-10+yyTop]);
		if (RootContext.Documentation != null)
			((Method) yyVal).DocComment = Lexer.consume_doc_comment ();
	  }
  break;
case 238:
#line 2028 "cs-parser.jay"
  {
		lexer.PropertyParsing = true;
		implicit_value_parameter_type = (Expression)yyVals[-2+yyTop];
	  }
  break;
case 239:
#line 2033 "cs-parser.jay"
  {
		has_get = has_set = false; 
		lexer.PropertyParsing = false;
		implicit_value_parameter_type = null;
	  }
  break;
case 240:
#line 2039 "cs-parser.jay"
  {
		LocatedToken lt = (LocatedToken) yyVals[-5+yyTop];
		MemberName name = new MemberName (lt.Value, lt.Location);

		if (yyVals[-6+yyTop] == TypeManager.system_void_expr) {
			Report.Error (547, lt.Location, "`{0}': property or indexer cannot have void type", lt.Value);
			break;
		}

		Property p = null;
		if (yyVals[-2+yyTop] == null) {
			p = new Property (current_class, (Expression) yyVals[-6+yyTop], (int) yyVals[-7+yyTop], true,
				   name, (Attributes) yyVals[-8+yyTop],
				   null, null, false);

			Report.Error (548, p.Location, "`{0}': property or indexer must have at least one accessor", p.GetSignatureForError ());
			break;
		}

		Accessors accessor = (Accessors) yyVals[-2+yyTop];
		p = new Property (current_class, (Expression) yyVals[-6+yyTop], (int) yyVals[-7+yyTop], true,
				   name, (Attributes) yyVals[-8+yyTop],
				   accessor.get_or_add, accessor.set_or_remove, accessor.declared_in_reverse);

		if (accessor.get_or_add != null && accessor.get_or_add.Block != null) {
			Report.Error (531, p.Location, "`{0}.get': interface members cannot have a definition", p.GetSignatureForError ());
			yyVal = null;
			break;
		}

		if (accessor.set_or_remove != null && accessor.set_or_remove.Block != null) {
			Report.Error (531, p.Location, "`{0}.set': interface members cannot have a definition", p.GetSignatureForError ());
			yyVal = null;
			break;
		}

		if (RootContext.Documentation != null)
			p.DocComment = Lexer.consume_doc_comment ();

		yyVal = p;
	  }
  break;
case 241:
#line 2082 "cs-parser.jay"
  {
		CheckIdentifierToken (yyToken, GetLocation (yyVals[0+yyTop]));
		yyVal = null;
	  }
  break;
case 242:
#line 2091 "cs-parser.jay"
  {
		LocatedToken lt = (LocatedToken) yyVals[-1+yyTop];
		yyVal = new EventField (current_class, (Expression) yyVals[-2+yyTop], (int) yyVals[-4+yyTop], true,
				     new MemberName (lt.Value, lt.Location),
				     (Attributes) yyVals[-5+yyTop]);
		if (RootContext.Documentation != null)
			((EventField) yyVal).DocComment = Lexer.consume_doc_comment ();
	  }
  break;
case 243:
#line 2099 "cs-parser.jay"
  {
		CheckIdentifierToken (yyToken, GetLocation (yyVals[0+yyTop]));
		yyVal = null;
	  }
  break;
case 244:
#line 2103 "cs-parser.jay"
  {
		LocatedToken lt = (LocatedToken) yyVals[-1+yyTop];
		Report.Error (68, lt.Location, "`{0}.{1}': event in interface cannot have initializer", current_container.Name, lt.Value);
		yyVal = null;
	  }
  break;
case 245:
#line 2109 "cs-parser.jay"
  {
		implicit_value_parameter_type = (Expression) yyVals[-2+yyTop];
		lexer.EventParsing = true;
	  }
  break;
case 246:
#line 2114 "cs-parser.jay"
  {
		lexer.EventParsing = false;
		implicit_value_parameter_type = null;
	  }
  break;
case 247:
#line 2118 "cs-parser.jay"
  {
		Report.Error (69, (Location) yyVals[-7+yyTop], "Event in interface cannot have add or remove accessors");
 		yyVal = null;
 	  }
  break;
case 248:
#line 2128 "cs-parser.jay"
  {
		lexer.PropertyParsing = true;
		implicit_value_parameter_type = (Expression)yyVals[-5+yyTop];
	  }
  break;
case 249:
#line 2133 "cs-parser.jay"
  { 
		has_get = has_set = false;
 		lexer.PropertyParsing = false;
 		implicit_value_parameter_type = null;
	  }
  break;
case 250:
#line 2139 "cs-parser.jay"
  {
		Indexer i = null;
		if (yyVals[-2+yyTop] == null) {
			i = new Indexer (current_class, (Expression) yyVals[-9+yyTop],
				  new MemberName (TypeContainer.DefaultIndexerName, (Location) yyVals[-8+yyTop]),
				  (int) yyVals[-10+yyTop], true, (Parameters) yyVals[-6+yyTop], (Attributes) yyVals[-11+yyTop],
				  null, null, false);

			Report.Error (548, i.Location, "`{0}': property or indexer must have at least one accessor", i.GetSignatureForError ());
			break;
		}

		Accessors accessors = (Accessors) yyVals[-2+yyTop];
		i = new Indexer (current_class, (Expression) yyVals[-9+yyTop],
				  new MemberName (TypeContainer.DefaultIndexerName, (Location) yyVals[-8+yyTop]),
				  (int) yyVals[-10+yyTop], true, (Parameters) yyVals[-6+yyTop], (Attributes) yyVals[-11+yyTop],
				   accessors.get_or_add, accessors.set_or_remove, accessors.declared_in_reverse);

		if (accessors.get_or_add != null && accessors.get_or_add.Block != null) {
			Report.Error (531, i.Location, "`{0}.get': interface members cannot have a definition", i.GetSignatureForError ());
			yyVal = null;
			break;
		}

		if (accessors.set_or_remove != null && accessors.set_or_remove.Block != null) {
			Report.Error (531, i.Location, "`{0}.set': interface members cannot have a definition", i.GetSignatureForError ());
			yyVal = null;
			break;
		}

		if (RootContext.Documentation != null)
			i.DocComment = ConsumeStoredComment ();

		yyVal = i;
	  }
  break;
case 251:
#line 2178 "cs-parser.jay"
  {
		anonymous_host = SimpleAnonymousHost.GetSimple ();
	  }
  break;
case 252:
#line 2182 "cs-parser.jay"
  {
		if (yyVals[-2+yyTop] == null)
			break;

		OperatorDeclaration decl = (OperatorDeclaration) yyVals[-2+yyTop];
		Operator op = new Operator (
			current_class, decl.optype, decl.ret_type, (int) yyVals[-3+yyTop], 
			current_local_parameters,
			(ToplevelBlock) yyVals[0+yyTop], (Attributes) yyVals[-4+yyTop], decl.location);

		if (RootContext.Documentation != null) {
			op.DocComment = tmpComment;
			Lexer.doc_state = XmlCommentState.Allowed;
		}

		SimpleAnonymousHost.Simple.Propagate (op);
		anonymous_host = null;

		/* Note again, checking is done in semantic analysis*/
		current_container.AddOperator (op);

		current_local_parameters = null;
	  }
  break;
case 254:
#line 2209 "cs-parser.jay"
  { yyVal = null; }
  break;
case 255:
#line 2214 "cs-parser.jay"
  {
		params_modifiers_not_allowed = true;
	  }
  break;
case 256:
#line 2218 "cs-parser.jay"
  {
		params_modifiers_not_allowed = false;

		Location loc = (Location) yyVals[-5+yyTop];
		Operator.OpType op = (Operator.OpType) yyVals[-4+yyTop];
		current_local_parameters = (Parameters)yyVals[-1+yyTop];
		
		int p_count = current_local_parameters.Count;
		if (p_count == 1) {
			if (op == Operator.OpType.Addition)
				op = Operator.OpType.UnaryPlus;
			else if (op == Operator.OpType.Subtraction)
				op = Operator.OpType.UnaryNegation;
		}
		
		if (IsUnaryOperator (op)) {
			if (p_count == 2) {
				Report.Error (1020, loc, "Overloadable binary operator expected");
			} else if (p_count != 1) {
				Report.Error (1535, loc, "Overloaded unary operator `{0}' takes one parameter",
					Operator.GetName (op));
			}
		} else {
			if (p_count > 2) {
				Report.Error (1534, loc, "Overloaded binary operator `{0}' takes two parameters",
					Operator.GetName (op));
			} else if (p_count != 2) {
				Report.Error (1019, loc, "Overloadable unary operator expected");
			}
		}
		
		if (RootContext.Documentation != null) {
			tmpComment = Lexer.consume_doc_comment ();
			Lexer.doc_state = XmlCommentState.NotAllowed;
		}

		yyVal = new OperatorDeclaration (op, (Expression) yyVals[-6+yyTop], loc);
	  }
  break;
case 258:
#line 2261 "cs-parser.jay"
  { yyVal = Operator.OpType.LogicalNot; }
  break;
case 259:
#line 2262 "cs-parser.jay"
  { yyVal = Operator.OpType.OnesComplement; }
  break;
case 260:
#line 2263 "cs-parser.jay"
  { yyVal = Operator.OpType.Increment; }
  break;
case 261:
#line 2264 "cs-parser.jay"
  { yyVal = Operator.OpType.Decrement; }
  break;
case 262:
#line 2265 "cs-parser.jay"
  { yyVal = Operator.OpType.True; }
  break;
case 263:
#line 2266 "cs-parser.jay"
  { yyVal = Operator.OpType.False; }
  break;
case 264:
#line 2268 "cs-parser.jay"
  { yyVal = Operator.OpType.Addition; }
  break;
case 265:
#line 2269 "cs-parser.jay"
  { yyVal = Operator.OpType.Subtraction; }
  break;
case 266:
#line 2271 "cs-parser.jay"
  { yyVal = Operator.OpType.Multiply; }
  break;
case 267:
#line 2272 "cs-parser.jay"
  {  yyVal = Operator.OpType.Division; }
  break;
case 268:
#line 2273 "cs-parser.jay"
  { yyVal = Operator.OpType.Modulus; }
  break;
case 269:
#line 2274 "cs-parser.jay"
  { yyVal = Operator.OpType.BitwiseAnd; }
  break;
case 270:
#line 2275 "cs-parser.jay"
  { yyVal = Operator.OpType.BitwiseOr; }
  break;
case 271:
#line 2276 "cs-parser.jay"
  { yyVal = Operator.OpType.ExclusiveOr; }
  break;
case 272:
#line 2277 "cs-parser.jay"
  { yyVal = Operator.OpType.LeftShift; }
  break;
case 273:
#line 2278 "cs-parser.jay"
  { yyVal = Operator.OpType.RightShift; }
  break;
case 274:
#line 2279 "cs-parser.jay"
  { yyVal = Operator.OpType.Equality; }
  break;
case 275:
#line 2280 "cs-parser.jay"
  { yyVal = Operator.OpType.Inequality; }
  break;
case 276:
#line 2281 "cs-parser.jay"
  { yyVal = Operator.OpType.GreaterThan; }
  break;
case 277:
#line 2282 "cs-parser.jay"
  { yyVal = Operator.OpType.LessThan; }
  break;
case 278:
#line 2283 "cs-parser.jay"
  { yyVal = Operator.OpType.GreaterThanOrEqual; }
  break;
case 279:
#line 2284 "cs-parser.jay"
  { yyVal = Operator.OpType.LessThanOrEqual; }
  break;
case 280:
#line 2289 "cs-parser.jay"
  {
		params_modifiers_not_allowed = true;
	  }
  break;
case 281:
#line 2293 "cs-parser.jay"
  {
		params_modifiers_not_allowed = false;

		Location loc = (Location) yyVals[-5+yyTop];
		current_local_parameters = (Parameters)yyVals[-1+yyTop];  
		  
		if (RootContext.Documentation != null) {
			tmpComment = Lexer.consume_doc_comment ();
			Lexer.doc_state = XmlCommentState.NotAllowed;
		}

		yyVal = new OperatorDeclaration (Operator.OpType.Implicit, (Expression) yyVals[-4+yyTop], loc);
	  }
  break;
case 282:
#line 2307 "cs-parser.jay"
  {
		params_modifiers_not_allowed = true;
	  }
  break;
case 283:
#line 2311 "cs-parser.jay"
  {
		params_modifiers_not_allowed = false;
		
		Location loc = (Location) yyVals[-5+yyTop];
		current_local_parameters = (Parameters)yyVals[-1+yyTop];  
		  
		if (RootContext.Documentation != null) {
			tmpComment = Lexer.consume_doc_comment ();
			Lexer.doc_state = XmlCommentState.NotAllowed;
		}

		yyVal = new OperatorDeclaration (Operator.OpType.Explicit, (Expression) yyVals[-4+yyTop], loc);
	  }
  break;
case 284:
#line 2325 "cs-parser.jay"
  {
		syntax_error ((Location) yyVals[-1+yyTop], "'operator' expected");
	  }
  break;
case 285:
#line 2329 "cs-parser.jay"
  {
		syntax_error ((Location) yyVals[-1+yyTop], "'operator' expected");
	  }
  break;
case 286:
#line 2339 "cs-parser.jay"
  { 
		Constructor c = (Constructor) yyVals[-1+yyTop];
		c.Block = (ToplevelBlock) yyVals[0+yyTop];
		c.OptAttributes = (Attributes) yyVals[-3+yyTop];
		int yield_method = c.ModFlags & Modifiers.METHOD_YIELDS;
		int mods = (int) yyVals[-2+yyTop];
		
		if (RootContext.Documentation != null)
			c.DocComment = ConsumeStoredComment ();

		if ((mods & Modifiers.STATIC) != 0 && c.Name == current_container.Basename) {
			if ((mods & Modifiers.Accessibility) != 0){
				Report.Error (515, c.Location,
					"`{0}': access modifiers are not allowed on static constructors",
					c.GetSignatureForError ());
			}
	
			if (c.Initializer != null){
				Report.Error (514, c.Location,
					"`{0}': static constructor cannot have an explicit `this' or `base' constructor call",
					c.GetSignatureForError ());
			}
		}

		c.ModFlags = Modifiers.Check (Constructor.AllowedModifiers, mods, Modifiers.PRIVATE, c.Location) | yield_method;
		current_container.AddConstructor (c);

		current_local_parameters = null;
		if (RootContext.Documentation != null)
			Lexer.doc_state = XmlCommentState.Allowed;
	  }
  break;
case 287:
#line 2374 "cs-parser.jay"
  {
		yyVal = yyVals[0+yyTop];
	  }
  break;
case 288:
#line 2378 "cs-parser.jay"
  {
		((Constructor)yyVals[-1+yyTop]).Initializer = (ConstructorInitializer) yyVals[0+yyTop];
		yyVal = yyVals[-1+yyTop];
	  }
  break;
case 289:
#line 2386 "cs-parser.jay"
  {
		if (RootContext.Documentation != null) {
			tmpComment = Lexer.consume_doc_comment ();
			Lexer.doc_state = XmlCommentState.Allowed;
		}
	  }
  break;
case 290:
#line 2393 "cs-parser.jay"
  {
		LocatedToken lt = (LocatedToken) yyVals[-4+yyTop];
		current_local_parameters = (Parameters) yyVals[-1+yyTop];
		current_block = new ToplevelBlock (null, current_local_parameters, null, lt.Location);

		yyVal = new Constructor (current_class, lt.Value, 0, current_local_parameters,
				      null, lt.Location);

		anonymous_host = (IAnonymousHost) yyVal;
	  }
  break;
case 292:
#line 2407 "cs-parser.jay"
  { current_block = null; yyVal = null; }
  break;
case 293:
#line 2412 "cs-parser.jay"
  {
		yyVal = new ConstructorBaseInitializer ((ArrayList) yyVals[-1+yyTop], (Location) yyVals[-3+yyTop]);
	  }
  break;
case 294:
#line 2416 "cs-parser.jay"
  {
		yyVal = new ConstructorThisInitializer ((ArrayList) yyVals[-1+yyTop], (Location) yyVals[-3+yyTop]);
	  }
  break;
case 295:
#line 2419 "cs-parser.jay"
  {
		Report.Error (1018, (Location) yyVals[-1+yyTop], "Keyword this or base expected");
		yyVal = null;
	  }
  break;
case 296:
#line 2426 "cs-parser.jay"
  { yyVal = 0; }
  break;
case 297:
#line 2427 "cs-parser.jay"
  { yyVal = Modifiers.UNSAFE; }
  break;
case 298:
#line 2428 "cs-parser.jay"
  { yyVal = Modifiers.EXTERN; }
  break;
case 299:
#line 2433 "cs-parser.jay"
  {
		if (RootContext.Documentation != null) {
			tmpComment = Lexer.consume_doc_comment ();
			Lexer.doc_state = XmlCommentState.NotAllowed;
		}
	  }
  break;
case 300:
#line 2440 "cs-parser.jay"
  {
		LocatedToken lt = (LocatedToken) yyVals[-3+yyTop];
		if (lt.Value != current_container.MemberName.Name){
			Report.Error (574, lt.Location, "Name of destructor must match name of class");
		} else if (current_container.Kind != Kind.Class){
			Report.Error (575, lt.Location, "Only class types can contain destructor");
		} else {
			Location l = lt.Location;

			int m = (int) yyVals[-6+yyTop];
			if (!RootContext.StdLib && current_container.Name == "System.Object")
				m |= Modifiers.PROTECTED | Modifiers.VIRTUAL;
			else
				m |= Modifiers.PROTECTED | Modifiers.OVERRIDE;
                        
			Method d = new Destructor (
				current_class, TypeManager.system_void_expr, m, "Finalize", 
				Parameters.EmptyReadOnlyParameters, (Attributes) yyVals[-7+yyTop], l);
			if (RootContext.Documentation != null)
				d.DocComment = ConsumeStoredComment ();
		  
			d.Block = (ToplevelBlock) yyVals[0+yyTop];
			current_container.AddMethod (d);
		}
	  }
  break;
case 301:
#line 2471 "cs-parser.jay"
  {
		current_array_type = null;
		foreach (VariableDeclaration var in (ArrayList) yyVals[-1+yyTop]) {

			MemberName name = new MemberName (var.identifier,
				var.Location);

			EventField e = new EventField (
				current_class, (Expression) yyVals[-2+yyTop], (int) yyVals[-4+yyTop], false, name,
				(Attributes) yyVals[-5+yyTop]);

			e.Initializer = var.expression_or_array_initializer;

			current_container.AddEvent (e);

			if (RootContext.Documentation != null) {
				e.DocComment = Lexer.consume_doc_comment ();
				Lexer.doc_state = XmlCommentState.Allowed;
			}
		}
	  }
  break;
case 302:
#line 2496 "cs-parser.jay"
  {
		implicit_value_parameter_type = (Expression) yyVals[-2+yyTop];  
		lexer.EventParsing = true;
	  }
  break;
case 303:
#line 2501 "cs-parser.jay"
  {
		lexer.EventParsing = false;  
	  }
  break;
case 304:
#line 2505 "cs-parser.jay"
  {
		MemberName name = (MemberName) yyVals[-5+yyTop];

		if (yyVals[-2+yyTop] == null){
			Report.Error (65, (Location) yyVals[-7+yyTop], "`{0}.{1}': event property must have both add and remove accessors",
				current_container.Name, name.GetSignatureForError ());
			yyVal = null;
		} else {
			Accessors accessors = (Accessors) yyVals[-2+yyTop];
			
			if (name.TypeArguments != null)
				syntax_error (lexer.Location, "an event can't have type arguments");

			if (accessors.get_or_add == null || accessors.set_or_remove == null)
				/* CS0073 is already reported, so no CS0065 here.*/
				yyVal = null;
			else {
				Event e = new EventProperty (
					current_class, (Expression) yyVals[-6+yyTop], (int) yyVals[-8+yyTop], false, name,
					(Attributes) yyVals[-9+yyTop], accessors.get_or_add, accessors.set_or_remove);
				if (RootContext.Documentation != null) {
					e.DocComment = Lexer.consume_doc_comment ();
					Lexer.doc_state = XmlCommentState.Allowed;
				}

				current_container.AddEvent (e);
				implicit_value_parameter_type = null;
			}
		}
	  }
  break;
case 305:
#line 2535 "cs-parser.jay"
  {
		MemberName mn = (MemberName) yyVals[-1+yyTop];

		if (mn.Left != null)
			Report.Error (71, mn.Location, "An explicit interface implementation of an event must use property syntax");
		else 
			Report.Error (71, mn.Location, "Event declaration should use property syntax");

		if (RootContext.Documentation != null)
			Lexer.doc_state = XmlCommentState.Allowed;
	  }
  break;
case 306:
#line 2550 "cs-parser.jay"
  {
		yyVal = new Accessors ((Accessor) yyVals[-1+yyTop], (Accessor) yyVals[0+yyTop]);
	  }
  break;
case 307:
#line 2554 "cs-parser.jay"
  {
		Accessors accessors = new Accessors ((Accessor) yyVals[0+yyTop], (Accessor) yyVals[-1+yyTop]);
		accessors.declared_in_reverse = true;
		yyVal = accessors;
	  }
  break;
case 308:
#line 2559 "cs-parser.jay"
  { yyVal = null; }
  break;
case 309:
#line 2560 "cs-parser.jay"
  { yyVal = null; }
  break;
case 310:
#line 2562 "cs-parser.jay"
  { 
		Report.Error (1055, GetLocation (yyVals[0+yyTop]), "An add or remove accessor expected");
		yyVal = null;
	  }
  break;
case 311:
#line 2566 "cs-parser.jay"
  { yyVal = null; }
  break;
case 312:
#line 2571 "cs-parser.jay"
  {
		Parameter [] args = new Parameter [1];
		Parameter implicit_value_parameter = new Parameter (
			implicit_value_parameter_type, "value", 
			Parameter.Modifier.NONE, null, (Location) yyVals[0+yyTop]);

		args [0] = implicit_value_parameter;
		
		current_local_parameters = new Parameters (args);  
		lexer.EventParsing = false;
		
		anonymous_host = SimpleAnonymousHost.GetSimple ();
	  }
  break;
case 313:
#line 2585 "cs-parser.jay"
  {
		Accessor accessor = new Accessor ((ToplevelBlock) yyVals[0+yyTop], 0, (Attributes) yyVals[-3+yyTop], (Location) yyVals[-2+yyTop]);
		lexer.EventParsing = true;
		
		current_local_parameters = null;
		SimpleAnonymousHost.Simple.Propagate (accessor);
		anonymous_host = null;
		
		yyVal = accessor;
	  }
  break;
case 314:
#line 2595 "cs-parser.jay"
  {
		Report.Error (73, (Location) yyVals[-1+yyTop], "An add or remove accessor must have a body");
		yyVal = null;
	  }
  break;
case 315:
#line 2599 "cs-parser.jay"
  {
		Report.Error (1609, (Location) yyVals[0+yyTop], "Modifiers cannot be placed on event accessor declarations");
		yyVal = null;
	  }
  break;
case 316:
#line 2607 "cs-parser.jay"
  {
		Parameter [] args = new Parameter [1];
		Parameter implicit_value_parameter = new Parameter (
			implicit_value_parameter_type, "value", 
			Parameter.Modifier.NONE, null, (Location) yyVals[0+yyTop]);

		args [0] = implicit_value_parameter;
		
		current_local_parameters = new Parameters (args);  
		lexer.EventParsing = false;
	  }
  break;
case 317:
#line 2619 "cs-parser.jay"
  {
		yyVal = new Accessor ((ToplevelBlock) yyVals[0+yyTop], 0, (Attributes) yyVals[-3+yyTop], (Location) yyVals[-2+yyTop]);
		lexer.EventParsing = true;
	  }
  break;
case 318:
#line 2623 "cs-parser.jay"
  {
		Report.Error (73, (Location) yyVals[-1+yyTop], "An add or remove accessor must have a body");
		yyVal = null;
	  }
  break;
case 319:
#line 2627 "cs-parser.jay"
  {
		Report.Error (1609, (Location) yyVals[0+yyTop], "Modifiers cannot be placed on event accessor declarations");
		yyVal = null;
	  }
  break;
case 320:
#line 2636 "cs-parser.jay"
  {
		IndexerDeclaration decl = (IndexerDeclaration) yyVals[-1+yyTop];

		implicit_value_parameter_type = decl.type;
		
		lexer.PropertyParsing = true;
		parsing_indexer  = true;
		
		indexer_parameters = decl.param_list;
		anonymous_host = SimpleAnonymousHost.GetSimple ();
	  }
  break;
case 321:
#line 2648 "cs-parser.jay"
  {
		  lexer.PropertyParsing = false;
		  has_get = has_set = false;
		  parsing_indexer  = false;
	  }
  break;
case 322:
#line 2654 "cs-parser.jay"
  { 
		if (yyVals[-2+yyTop] == null)
			break;

		/* The signature is computed from the signature of the indexer.  Look*/
	 	/* at section 3.6 on the spec*/
		Indexer indexer;
		IndexerDeclaration decl = (IndexerDeclaration) yyVals[-5+yyTop];
		Location loc = decl.location;
		Accessors accessors = (Accessors) yyVals[-2+yyTop];
		Accessor get_block = accessors.get_or_add;
		Accessor set_block = accessors.set_or_remove;

		MemberName name;
		if (decl.interface_type != null)
			name = new MemberName (decl.interface_type, TypeContainer.DefaultIndexerName, loc);
		else
			name = new MemberName (TypeContainer.DefaultIndexerName, loc);

		indexer = new Indexer (current_class, decl.type, name,
				       (int) yyVals[-6+yyTop], false, decl.param_list, (Attributes) yyVals[-7+yyTop],
				       get_block, set_block, accessors.declared_in_reverse);

		if (RootContext.Documentation != null)
			indexer.DocComment = ConsumeStoredComment ();

		current_container.AddIndexer (indexer);
		
		current_local_parameters = null;
		implicit_value_parameter_type = null;
		indexer_parameters = null;
	  }
  break;
case 323:
#line 2690 "cs-parser.jay"
  {
		Parameters pars = (Parameters) yyVals[-1+yyTop];
		if (pars.Empty){
			Report.Error (1551, (Location) yyVals[-3+yyTop], "Indexers must have at least one parameter");
		}
		if (RootContext.Documentation != null) {
			tmpComment = Lexer.consume_doc_comment ();
			Lexer.doc_state = XmlCommentState.Allowed;
		}

		yyVal = new IndexerDeclaration ((Expression) yyVals[-4+yyTop], null, pars, (Location) yyVals[-3+yyTop]);
	  }
  break;
case 324:
#line 2703 "cs-parser.jay"
  {
		Parameters pars = (Parameters) yyVals[-1+yyTop];
		if (pars.Empty){
			Report.Error (1551, (Location) yyVals[-3+yyTop], "Indexers must have at least one parameter");
		}

		MemberName name = (MemberName) yyVals[-5+yyTop];
		yyVal = new IndexerDeclaration ((Expression) yyVals[-6+yyTop], name, pars, (Location) yyVals[-3+yyTop]);

		if (RootContext.Documentation != null) {
			tmpComment = Lexer.consume_doc_comment ();
			Lexer.doc_state = XmlCommentState.Allowed;
		}
	  }
  break;
case 325:
#line 2723 "cs-parser.jay"
  {
		if (RootContext.Documentation != null)
			enumTypeComment = Lexer.consume_doc_comment ();
	  }
  break;
case 326:
#line 2729 "cs-parser.jay"
  {
		LocatedToken lt = (LocatedToken) yyVals[-4+yyTop];
		Location enum_location = lt.Location;

		MemberName name = MakeName (new MemberName (lt.Value, enum_location));
		Enum e = new Enum (current_namespace, current_class, (Expression) yyVals[-3+yyTop], (int) yyVals[-6+yyTop],
				   name, (Attributes) yyVals[-7+yyTop]);
		
		if (RootContext.Documentation != null)
			e.DocComment = enumTypeComment;


		EnumMember em = null;
		foreach (VariableDeclaration ev in (ArrayList) yyVals[-1+yyTop]) {
			em = new EnumMember (
				e, em, ev.identifier, (Expression) ev.expression_or_array_initializer,
				ev.OptAttributes, ev.Location);

/*			if (RootContext.Documentation != null)*/
				em.DocComment = ev.DocComment;

			e.AddEnumMember (em);
		}

		current_container.AddTypeContainer (e);
		current_namespace.AddType (e);
		yyVal = e;

	  }
  break;
case 327:
#line 2761 "cs-parser.jay"
  { yyVal = TypeManager.system_int32_expr; }
  break;
case 328:
#line 2762 "cs-parser.jay"
  { yyVal = yyVals[0+yyTop];   }
  break;
case 329:
#line 2767 "cs-parser.jay"
  {
		if (RootContext.Documentation != null)
			Lexer.doc_state = XmlCommentState.Allowed;
	  }
  break;
case 330:
#line 2772 "cs-parser.jay"
  {
	  	/* here will be evaluated after CLOSE_BLACE is consumed.*/
		if (RootContext.Documentation != null)
			Lexer.doc_state = XmlCommentState.Allowed;
	  }
  break;
case 331:
#line 2778 "cs-parser.jay"
  {
		yyVal = yyVals[-2+yyTop];
	  }
  break;
case 332:
#line 2784 "cs-parser.jay"
  { yyVal = new ArrayList (4); }
  break;
case 333:
#line 2785 "cs-parser.jay"
  { yyVal = yyVals[-1+yyTop]; }
  break;
case 334:
#line 2790 "cs-parser.jay"
  {
		ArrayList l = new ArrayList (4);

		l.Add (yyVals[0+yyTop]);
		yyVal = l;
	  }
  break;
case 335:
#line 2797 "cs-parser.jay"
  {
		ArrayList l = (ArrayList) yyVals[-2+yyTop];

		l.Add (yyVals[0+yyTop]);

		yyVal = l;
	  }
  break;
case 336:
#line 2808 "cs-parser.jay"
  {
		VariableDeclaration vd = new VariableDeclaration (
			(LocatedToken) yyVals[0+yyTop], null, (Attributes) yyVals[-1+yyTop]);

		if (RootContext.Documentation != null) {
			vd.DocComment = Lexer.consume_doc_comment ();
			Lexer.doc_state = XmlCommentState.Allowed;
		}

		yyVal = vd;
	  }
  break;
case 337:
#line 2820 "cs-parser.jay"
  {
		if (RootContext.Documentation != null) {
			tmpComment = Lexer.consume_doc_comment ();
			Lexer.doc_state = XmlCommentState.NotAllowed;
		}
	  }
  break;
case 338:
#line 2827 "cs-parser.jay"
  { 
		VariableDeclaration vd = new VariableDeclaration (
			(LocatedToken) yyVals[-3+yyTop], yyVals[0+yyTop], (Attributes) yyVals[-4+yyTop]);

		if (RootContext.Documentation != null)
			vd.DocComment = ConsumeStoredComment ();

		yyVal = vd;
	  }
  break;
case 339:
#line 2844 "cs-parser.jay"
  {
		MemberName name = MakeName ((MemberName) yyVals[-3+yyTop]);
		Parameters p = (Parameters) yyVals[-1+yyTop];

		Delegate del = new Delegate (current_namespace, current_class, (Expression) yyVals[-4+yyTop],
					     (int) yyVals[-6+yyTop], name, p, (Attributes) yyVals[-7+yyTop]);

		if (RootContext.Documentation != null) {
			del.DocComment = Lexer.consume_doc_comment ();
			Lexer.doc_state = XmlCommentState.Allowed;
		}

		current_container.AddDelegate (del);
		current_namespace.AddDelegate (del);
		current_delegate = del;
		lexer.ConstraintsParsing = true;
	  }
  break;
case 340:
#line 2862 "cs-parser.jay"
  {
		lexer.ConstraintsParsing = false;
	  }
  break;
case 341:
#line 2866 "cs-parser.jay"
  {
		current_delegate.SetParameterInfo ((ArrayList) yyVals[-2+yyTop]);
		yyVal = current_delegate;

		current_delegate = null;
	  }
  break;
case 342:
#line 2876 "cs-parser.jay"
  {
		lexer.CheckNullable (false);
		yyVal = false;
	  }
  break;
case 343:
#line 2881 "cs-parser.jay"
  {
	  	/* FIXME: A hack with parsing conditional operator as nullable type*/
		/*if (RootContext.Version < LanguageVersion.ISO_2)*/
		/*	Report.FeatureIsNotAvailable (lexer.Location, "nullable types");*/
	  		
		lexer.CheckNullable (true);
		yyVal = true;
	  }
  break;
case 344:
#line 2893 "cs-parser.jay"
  {
		LocatedToken lt = (LocatedToken) yyVals[-1+yyTop];
		yyVal = new MemberName (lt.Value, (TypeArguments) yyVals[0+yyTop], lt.Location);
	  }
  break;
case 345:
#line 2897 "cs-parser.jay"
  {
		LocatedToken lt1 = (LocatedToken) yyVals[-3+yyTop];
		LocatedToken lt2 = (LocatedToken) yyVals[-1+yyTop];
		yyVal = new MemberName (lt1.Value, lt2.Value, (TypeArguments) yyVals[0+yyTop], lt2.Location);
	  }
  break;
case 346:
#line 2902 "cs-parser.jay"
  {
		LocatedToken lt = (LocatedToken) yyVals[-1+yyTop];
		yyVal = new MemberName ((MemberName) yyVals[-3+yyTop], lt.Value, (TypeArguments) yyVals[0+yyTop], lt.Location);
	  }
  break;
case 347:
#line 2910 "cs-parser.jay"
  {
		LocatedToken lt = (LocatedToken) yyVals[-1+yyTop];
		yyVal = new MemberName (lt.Value, (TypeArguments) yyVals[0+yyTop], lt.Location);
	  }
  break;
case 348:
#line 2915 "cs-parser.jay"
  {
		LocatedToken lt = (LocatedToken) yyVals[-1+yyTop];
		yyVal = new MemberName ((MemberName) yyVals[-3+yyTop], lt.Value, (TypeArguments) yyVals[0+yyTop], lt.Location);
	  }
  break;
case 349:
#line 2923 "cs-parser.jay"
  {
		LocatedToken lt = (LocatedToken) yyVals[-1+yyTop];
		yyVal = new MemberName (lt.Value, (TypeArguments)yyVals[0+yyTop], lt.Location);	  
	  }
  break;
case 350:
#line 2934 "cs-parser.jay"
  { yyVal = null; }
  break;
case 351:
#line 2936 "cs-parser.jay"
  {
		if (RootContext.Version < LanguageVersion.ISO_2)
			Report.FeatureIsNotAvailable (GetLocation (yyVals[-2+yyTop]), "generics");	  
	  
		yyVal = yyVals[-1+yyTop];
	  }
  break;
case 352:
#line 2949 "cs-parser.jay"
  { yyVal = null; }
  break;
case 353:
#line 2951 "cs-parser.jay"
  {
		if (RootContext.Version < LanguageVersion.ISO_2)
			Report.FeatureIsNotAvailable (GetLocation (yyVals[-2+yyTop]), "generics");
	  
		yyVal = yyVals[-1+yyTop];
	  }
  break;
case 354:
#line 2961 "cs-parser.jay"
  {
		TypeArguments type_args = new TypeArguments (lexer.Location);
		type_args.Add ((Expression) yyVals[0+yyTop]);
		yyVal = type_args;
	  }
  break;
case 355:
#line 2967 "cs-parser.jay"
  {
		TypeArguments type_args = (TypeArguments) yyVals[-2+yyTop];
		type_args.Add ((Expression) yyVals[0+yyTop]);
		yyVal = type_args;
	  }
  break;
case 356:
#line 2976 "cs-parser.jay"
  {
		yyVal = yyVals[0+yyTop];
  	  }
  break;
case 357:
#line 2980 "cs-parser.jay"
  {
		SimpleName sn = yyVals[0+yyTop] as SimpleName;
		if (sn == null)
			Error_TypeExpected (GetLocation (yyVals[0+yyTop]));
		else
			yyVals[0+yyTop] = new TypeParameterName (sn.Name, (Attributes) yyVals[-1+yyTop], lexer.Location);
		yyVal = yyVals[0+yyTop];  	  
  	  }
  break;
case 358:
#line 2998 "cs-parser.jay"
  {
		MemberName name = (MemberName) yyVals[-1+yyTop];

		if ((bool) yyVals[0+yyTop]) {
			yyVal = new ComposedCast (name.GetTypeExpression (), "?", lexer.Location);
		} else {
			if (RootContext.Version > LanguageVersion.ISO_2 && name.Name == "var")
				yyVal = new VarExpr (name.Location);
			else
				yyVal = name.GetTypeExpression ();
		}
	  }
  break;
case 359:
#line 3011 "cs-parser.jay"
  {
		if ((bool) yyVals[0+yyTop])
			yyVal = new ComposedCast ((Expression) yyVals[-1+yyTop], "?", lexer.Location);
	  }
  break;
case 362:
#line 3021 "cs-parser.jay"
  {
		/**/
		/* Note that here only unmanaged types are allowed but we*/
		/* can't perform checks during this phase - we do it during*/
		/* semantic analysis.*/
		/**/
		yyVal = new ComposedCast ((Expression) yyVals[-1+yyTop], "*", Lexer.Location);
	  }
  break;
case 363:
#line 3030 "cs-parser.jay"
  {
		yyVal = new ComposedCast (TypeManager.system_void_expr, "*", (Location) yyVals[-1+yyTop]);
	  }
  break;
case 364:
#line 3037 "cs-parser.jay"
  {
		if ((bool) yyVals[0+yyTop])
			yyVal = new ComposedCast ((Expression) yyVals[-1+yyTop], "?", lexer.Location);
	  }
  break;
case 365:
#line 3042 "cs-parser.jay"
  {
		Location loc = GetLocation (yyVals[-1+yyTop]);
		if (loc.IsNull)
			loc = lexer.Location;
		yyVal = new ComposedCast ((Expression) yyVals[-1+yyTop], (string) yyVals[0+yyTop], loc);
	  }
  break;
case 366:
#line 3049 "cs-parser.jay"
  {
		Location loc = GetLocation (yyVals[-1+yyTop]);
		if (loc.IsNull)
			loc = lexer.Location;
		yyVal = new ComposedCast ((Expression) yyVals[-1+yyTop], "*", loc);
	  }
  break;
case 367:
#line 3056 "cs-parser.jay"
  {
		yyVal = new ComposedCast ((Expression) yyVals[-1+yyTop], (string) yyVals[0+yyTop]);
	  }
  break;
case 368:
#line 3060 "cs-parser.jay"
  {
		yyVal = new ComposedCast ((Expression) yyVals[-1+yyTop], "*");
	  }
  break;
case 369:
#line 3069 "cs-parser.jay"
  {
		yyVal = new ComposedCast ((Expression) yyVals[-1+yyTop], "*");
	  }
  break;
case 370:
#line 3076 "cs-parser.jay"
  {
		ArrayList types = new ArrayList (4);

		types.Add (yyVals[0+yyTop]);
		yyVal = types;
	  }
  break;
case 371:
#line 3083 "cs-parser.jay"
  {
		ArrayList types = (ArrayList) yyVals[-2+yyTop];

		types.Add (yyVals[0+yyTop]);
		yyVal = types;
	  }
  break;
case 372:
#line 3096 "cs-parser.jay"
  { yyVal = TypeManager.system_object_expr; }
  break;
case 373:
#line 3097 "cs-parser.jay"
  { yyVal = TypeManager.system_string_expr; }
  break;
case 374:
#line 3098 "cs-parser.jay"
  { yyVal = TypeManager.system_boolean_expr; }
  break;
case 375:
#line 3099 "cs-parser.jay"
  { yyVal = TypeManager.system_decimal_expr; }
  break;
case 376:
#line 3100 "cs-parser.jay"
  { yyVal = TypeManager.system_single_expr; }
  break;
case 377:
#line 3101 "cs-parser.jay"
  { yyVal = TypeManager.system_double_expr; }
  break;
case 379:
#line 3106 "cs-parser.jay"
  { yyVal = TypeManager.system_sbyte_expr; }
  break;
case 380:
#line 3107 "cs-parser.jay"
  { yyVal = TypeManager.system_byte_expr; }
  break;
case 381:
#line 3108 "cs-parser.jay"
  { yyVal = TypeManager.system_int16_expr; }
  break;
case 382:
#line 3109 "cs-parser.jay"
  { yyVal = TypeManager.system_uint16_expr; }
  break;
case 383:
#line 3110 "cs-parser.jay"
  { yyVal = TypeManager.system_int32_expr; }
  break;
case 384:
#line 3111 "cs-parser.jay"
  { yyVal = TypeManager.system_uint32_expr; }
  break;
case 385:
#line 3112 "cs-parser.jay"
  { yyVal = TypeManager.system_int64_expr; }
  break;
case 386:
#line 3113 "cs-parser.jay"
  { yyVal = TypeManager.system_uint64_expr; }
  break;
case 387:
#line 3114 "cs-parser.jay"
  { yyVal = TypeManager.system_char_expr; }
  break;
case 388:
#line 3115 "cs-parser.jay"
  { yyVal = TypeManager.system_void_expr; }
  break;
case 389:
#line 3120 "cs-parser.jay"
  {
		string rank_specifiers = (string) yyVals[-1+yyTop];
		if ((bool) yyVals[0+yyTop])
			rank_specifiers += "?";

		yyVal = current_array_type = new ComposedCast ((Expression) yyVals[-2+yyTop], rank_specifiers);
	  }
  break;
case 390:
#line 3134 "cs-parser.jay"
  {
		/* 7.5.1: Literals*/
	  }
  break;
case 391:
#line 3138 "cs-parser.jay"
  {
		MemberName mn = (MemberName) yyVals[0+yyTop];
		yyVal = mn.GetTypeExpression ();
	  }
  break;
case 392:
#line 3143 "cs-parser.jay"
  {
		LocatedToken lt1 = (LocatedToken) yyVals[-2+yyTop];
		LocatedToken lt2 = (LocatedToken) yyVals[0+yyTop];
		if (RootContext.Version == LanguageVersion.ISO_1)
			Report.FeatureIsNotAvailable (lt1.Location, "namespace alias qualifier");

		yyVal = new QualifiedAliasMember (lt1.Value, lt2.Value, lt2.Location);
	  }
  break;
case 412:
#line 3173 "cs-parser.jay"
  { yyVal = new CharLiteral ((char) lexer.Value, lexer.Location); }
  break;
case 413:
#line 3174 "cs-parser.jay"
  { yyVal = new StringLiteral ((string) lexer.Value, lexer.Location); }
  break;
case 414:
#line 3175 "cs-parser.jay"
  { yyVal = new NullLiteral (lexer.Location); }
  break;
case 415:
#line 3179 "cs-parser.jay"
  { yyVal = new FloatLiteral ((float) lexer.Value, lexer.Location); }
  break;
case 416:
#line 3180 "cs-parser.jay"
  { yyVal = new DoubleLiteral ((double) lexer.Value, lexer.Location); }
  break;
case 417:
#line 3181 "cs-parser.jay"
  { yyVal = new DecimalLiteral ((decimal) lexer.Value, lexer.Location); }
  break;
case 418:
#line 3185 "cs-parser.jay"
  { 
		object v = lexer.Value;

		if (v is int){
			yyVal = new IntLiteral ((int) v, lexer.Location);
		} else if (v is uint)
			yyVal = new UIntLiteral ((UInt32) v, lexer.Location);
		else if (v is long)
			yyVal = new LongLiteral ((Int64) v, lexer.Location);
		else if (v is ulong)
			yyVal = new ULongLiteral ((UInt64) v, lexer.Location);
		else
			Console.WriteLine ("OOPS.  Unexpected result from scanner");
	  }
  break;
case 419:
#line 3202 "cs-parser.jay"
  { yyVal = new BoolLiteral (true, lexer.Location); }
  break;
case 420:
#line 3203 "cs-parser.jay"
  { yyVal = new BoolLiteral (false, lexer.Location); }
  break;
case 421:
#line 3208 "cs-parser.jay"
  {
		yyVal = yyVals[-1+yyTop];
		lexer.Deambiguate_CloseParens (yyVal);
		/* After this, the next token returned is one of*/
		/* CLOSE_PARENS_CAST, CLOSE_PARENS_NO_CAST (CLOSE_PARENS), CLOSE_PARENS_OPEN_PARENS*/
		/* or CLOSE_PARENS_MINUS.*/
	  }
  break;
case 422:
#line 3215 "cs-parser.jay"
  { CheckToken (1026, yyToken, "Expecting ')'", lexer.Location); }
  break;
case 423:
#line 3220 "cs-parser.jay"
  {
		yyVal = yyVals[-1+yyTop];
	  }
  break;
case 424:
#line 3224 "cs-parser.jay"
  {
		yyVal = yyVals[-1+yyTop];
	  }
  break;
case 425:
#line 3228 "cs-parser.jay"
  {
		/* If a parenthesized expression is followed by a minus, we need to wrap*/
		/* the expression inside a ParenthesizedExpression for the CS0075 check*/
		/* in Binary.DoResolve().*/
		yyVal = new ParenthesizedExpression ((Expression) yyVals[-1+yyTop]);
	  }
  break;
case 426:
#line 3238 "cs-parser.jay"
  {
		LocatedToken lt = (LocatedToken) yyVals[-1+yyTop];
		yyVal = new MemberAccess ((Expression) yyVals[-3+yyTop], lt.Value, (TypeArguments) yyVals[0+yyTop], lt.Location);
	  }
  break;
case 427:
#line 3243 "cs-parser.jay"
  {
		LocatedToken lt = (LocatedToken) yyVals[-1+yyTop];
		/* TODO: Location is wrong as some predefined types doesn't hold a location*/
		yyVal = new MemberAccess ((Expression) yyVals[-3+yyTop], lt.Value, (TypeArguments) yyVals[0+yyTop], lt.Location);
	  }
  break;
case 429:
#line 3256 "cs-parser.jay"
  {
		if (yyVals[-3+yyTop] == null)
			Report.Error (1, (Location) yyVals[-2+yyTop], "Parse error");
	        else
			yyVal = new Invocation ((Expression) yyVals[-3+yyTop], (ArrayList) yyVals[-1+yyTop]);
	  }
  break;
case 430:
#line 3263 "cs-parser.jay"
  {
		yyVal = new Invocation ((Expression) yyVals[-3+yyTop], new ArrayList ());
	  }
  break;
case 431:
#line 3267 "cs-parser.jay"
  {
		yyVal = new InvocationOrCast ((Expression) yyVals[-2+yyTop], (Expression) yyVals[0+yyTop]);
	  }
  break;
case 432:
#line 3271 "cs-parser.jay"
  {
		ArrayList args = new ArrayList (1);
		args.Add (yyVals[-1+yyTop]);
		yyVal = new Invocation ((Expression) yyVals[-4+yyTop], args);
	  }
  break;
case 433:
#line 3277 "cs-parser.jay"
  {
		ArrayList args = ((ArrayList) yyVals[-3+yyTop]);
		args.Add (yyVals[-1+yyTop]);
		yyVal = new Invocation ((Expression) yyVals[-6+yyTop], args);
	  }
  break;
case 434:
#line 3285 "cs-parser.jay"
  { yyVal = null; }
  break;
case 436:
#line 3291 "cs-parser.jay"
  {
	  	if (yyVals[-1+yyTop] == null)
	  	  yyVal = CollectionOrObjectInitializers.Empty;
	  	else
	  	  yyVal = new CollectionOrObjectInitializers ((ArrayList) yyVals[-1+yyTop], GetLocation (yyVals[-2+yyTop]));
	  }
  break;
case 437:
#line 3298 "cs-parser.jay"
  {
	  	yyVal = new CollectionOrObjectInitializers ((ArrayList) yyVals[-2+yyTop], GetLocation (yyVals[-3+yyTop]));
	  }
  break;
case 438:
#line 3304 "cs-parser.jay"
  { yyVal = null; }
  break;
case 439:
#line 3306 "cs-parser.jay"
  {
		yyVal = yyVals[0+yyTop];
	}
  break;
case 440:
#line 3313 "cs-parser.jay"
  {
	  	ArrayList a = new ArrayList ();
	  	a.Add (yyVals[0+yyTop]);
	  	yyVal = a;
	  }
  break;
case 441:
#line 3319 "cs-parser.jay"
  {
	  	ArrayList a = (ArrayList)yyVals[-2+yyTop];
	  	a.Add (yyVals[0+yyTop]);
	  	yyVal = a;
	  }
  break;
case 442:
#line 3328 "cs-parser.jay"
  {
	  	LocatedToken lt = yyVals[-2+yyTop] as LocatedToken;
	  	yyVal = new ElementInitializer (lt.Value, (Expression)yyVals[0+yyTop], lt.Location);
	  }
  break;
case 443:
#line 3333 "cs-parser.jay"
  {
		yyVal = new CollectionElementInitializer ((Expression)yyVals[0+yyTop]);
	  }
  break;
case 444:
#line 3337 "cs-parser.jay"
  {
	  	yyVal = new CollectionElementInitializer ((ArrayList)yyVals[-1+yyTop], GetLocation (yyVals[-2+yyTop]));
	  }
  break;
case 445:
#line 3341 "cs-parser.jay"
  {
	  	Report.Error (1920, GetLocation (yyVals[-1+yyTop]), "An element initializer cannot be empty");
	  }
  break;
case 448:
#line 3352 "cs-parser.jay"
  { yyVal = null; }
  break;
case 450:
#line 3358 "cs-parser.jay"
  { 
		ArrayList list = new ArrayList (4);
		list.Add (yyVals[0+yyTop]);
		yyVal = list;
	  }
  break;
case 451:
#line 3364 "cs-parser.jay"
  {
		ArrayList list = (ArrayList) yyVals[-2+yyTop];
		list.Add (yyVals[0+yyTop]);
		yyVal = list;
	  }
  break;
case 452:
#line 3369 "cs-parser.jay"
  {
		CheckToken (1026, yyToken, "Expected `,' or `)'", GetLocation (yyVals[0+yyTop]));
		yyVal = null;
	  }
  break;
case 453:
#line 3377 "cs-parser.jay"
  {
		yyVal = new Argument ((Expression) yyVals[0+yyTop], Argument.AType.Expression);
	  }
  break;
case 454:
#line 3381 "cs-parser.jay"
  {
		yyVal = yyVals[0+yyTop];
	  }
  break;
case 455:
#line 3388 "cs-parser.jay"
  { 
		yyVal = new Argument ((Expression) yyVals[0+yyTop], Argument.AType.Ref);
	  }
  break;
case 456:
#line 3392 "cs-parser.jay"
  { 
		yyVal = new Argument ((Expression) yyVals[0+yyTop], Argument.AType.Out);
	  }
  break;
case 457:
#line 3396 "cs-parser.jay"
  {
		ArrayList list = (ArrayList) yyVals[-1+yyTop];
		Argument[] args = new Argument [list.Count];
		list.CopyTo (args, 0);

		Expression expr = new Arglist (args, (Location) yyVals[-3+yyTop]);
		yyVal = new Argument (expr, Argument.AType.Expression);
	  }
  break;
case 458:
#line 3405 "cs-parser.jay"
  {
		yyVal = new Argument (new Arglist ((Location) yyVals[-2+yyTop]), Argument.AType.Expression);
	  }
  break;
case 459:
#line 3409 "cs-parser.jay"
  {
		yyVal = new Argument (new ArglistAccess ((Location) yyVals[0+yyTop]), Argument.AType.ArgList);
	  }
  break;
case 460:
#line 3415 "cs-parser.jay"
  { note ("section 5.4"); yyVal = yyVals[0+yyTop]; }
  break;
case 461:
#line 3420 "cs-parser.jay"
  {
		yyVal = new ElementAccess ((Expression) yyVals[-3+yyTop], (ArrayList) yyVals[-1+yyTop]);
	  }
  break;
case 462:
#line 3424 "cs-parser.jay"
  {
		/* So the super-trick is that primary_expression*/
		/* can only be either a SimpleName or a MemberAccess. */
		/* The MemberAccess case arises when you have a fully qualified type-name like :*/
		/* Foo.Bar.Blah i;*/
		/* SimpleName is when you have*/
		/* Blah i;*/
		  
		Expression expr = (Expression) yyVals[-1+yyTop];  
		if (expr is ComposedCast){
			yyVal = new ComposedCast (expr, (string) yyVals[0+yyTop]);
		} else if (!(expr is SimpleName || expr is MemberAccess || expr is ConstructedType || expr is QualifiedAliasMember)){
			Error_ExpectingTypeName (expr);
			yyVal = TypeManager.system_object_expr;
		} else {
			/**/
			/* So we extract the string corresponding to the SimpleName*/
			/* or MemberAccess*/
			/* */
			yyVal = new ComposedCast (expr, (string) yyVals[0+yyTop]);
		}
		current_array_type = (Expression)yyVal;
	  }
  break;
case 463:
#line 3451 "cs-parser.jay"
  {
		ArrayList list = new ArrayList (4);
		list.Add (yyVals[0+yyTop]);
		yyVal = list;
	  }
  break;
case 464:
#line 3457 "cs-parser.jay"
  {
		ArrayList list = (ArrayList) yyVals[-2+yyTop];
		list.Add (yyVals[0+yyTop]);
		yyVal = list;
	  }
  break;
case 465:
#line 3466 "cs-parser.jay"
  {
		yyVal = new This (current_block, (Location) yyVals[0+yyTop]);
	  }
  break;
case 466:
#line 3473 "cs-parser.jay"
  {
		LocatedToken lt = (LocatedToken) yyVals[-1+yyTop];
		yyVal = new BaseAccess (lt.Value, (TypeArguments) yyVals[0+yyTop], lt.Location);
	  }
  break;
case 467:
#line 3478 "cs-parser.jay"
  {
		yyVal = new BaseIndexerAccess ((ArrayList) yyVals[-1+yyTop], (Location) yyVals[-3+yyTop]);
	  }
  break;
case 468:
#line 3481 "cs-parser.jay"
  {
		Report.Error (175, (Location) yyVals[-1+yyTop], "Use of keyword `base' is not valid in this context");
		yyVal = null;
	  }
  break;
case 469:
#line 3489 "cs-parser.jay"
  {
		yyVal = new UnaryMutator (UnaryMutator.Mode.PostIncrement,
				       (Expression) yyVals[-1+yyTop], (Location) yyVals[0+yyTop]);
	  }
  break;
case 470:
#line 3497 "cs-parser.jay"
  {
		yyVal = new UnaryMutator (UnaryMutator.Mode.PostDecrement,
				       (Expression) yyVals[-1+yyTop], (Location) yyVals[0+yyTop]);
	  }
  break;
case 474:
#line 3511 "cs-parser.jay"
  {
		if (yyVals[0+yyTop] != null) {
			if (RootContext.Version <= LanguageVersion.ISO_2)
				Report.FeatureIsNotAvailable (GetLocation (yyVals[-5+yyTop]), "object initializers");
				
			yyVal = new NewInitialize ((Expression) yyVals[-4+yyTop], (ArrayList) yyVals[-2+yyTop], (CollectionOrObjectInitializers) yyVals[0+yyTop], (Location) yyVals[-5+yyTop]);
		}
		else
			yyVal = new New ((Expression) yyVals[-4+yyTop], (ArrayList) yyVals[-2+yyTop], (Location) yyVals[-5+yyTop]);
	  }
  break;
case 475:
#line 3522 "cs-parser.jay"
  {
		if (RootContext.Version <= LanguageVersion.ISO_2)
			Report.FeatureIsNotAvailable (GetLocation (yyVals[-2+yyTop]), "collection initializers");
	  
		yyVal = new NewInitialize ((Expression) yyVals[-1+yyTop], null, (CollectionOrObjectInitializers) yyVals[0+yyTop], (Location) yyVals[-2+yyTop]);
	  }
  break;
case 476:
#line 3534 "cs-parser.jay"
  {
		yyVal = new ArrayCreation ((Expression) yyVals[-5+yyTop], (ArrayList) yyVals[-3+yyTop], (string) yyVals[-1+yyTop], (ArrayList) yyVals[0+yyTop], (Location) yyVals[-6+yyTop]);
	  }
  break;
case 477:
#line 3538 "cs-parser.jay"
  {
		yyVal = new ArrayCreation ((Expression) yyVals[-2+yyTop], (string) yyVals[-1+yyTop], (ArrayList) yyVals[0+yyTop], (Location) yyVals[-3+yyTop]);
	  }
  break;
case 478:
#line 3542 "cs-parser.jay"
  {
		yyVal = new ImplicitlyTypedArrayCreation ((string) yyVals[-1+yyTop], (ArrayList) yyVals[0+yyTop], (Location) yyVals[-2+yyTop]);
	  }
  break;
case 479:
#line 3546 "cs-parser.jay"
  {
		Report.Error (1031, (Location) yyVals[-1+yyTop], "Type expected");
                yyVal = null;
	  }
  break;
case 480:
#line 3551 "cs-parser.jay"
  {
		Report.Error (1526, (Location) yyVals[-2+yyTop], "A new expression requires () or [] after type");
		yyVal = null;
	  }
  break;
case 481:
#line 3559 "cs-parser.jay"
  {
	  	if (RootContext.Version <= LanguageVersion.ISO_2)
	  		Report.FeatureIsNotAvailable (GetLocation (yyVals[-3+yyTop]), "anonymous types");

		yyVal = new AnonymousTypeDeclaration ((ArrayList) yyVals[-1+yyTop], current_container, GetLocation (yyVals[-3+yyTop]));
	  }
  break;
case 482:
#line 3568 "cs-parser.jay"
  { yyVal = null; }
  break;
case 483:
#line 3570 "cs-parser.jay"
  {
	  	ArrayList a = new ArrayList (4);
	  	a.Add (yyVals[0+yyTop]);
	  	yyVal = a;
	  }
  break;
case 484:
#line 3576 "cs-parser.jay"
  {
	  	ArrayList a = (ArrayList) yyVals[-2+yyTop];
	  	a.Add (yyVals[0+yyTop]);
	  	yyVal = a;
	  }
  break;
case 485:
#line 3585 "cs-parser.jay"
  {
		LocatedToken lt = (LocatedToken)yyVals[-2+yyTop];
	  	yyVal = new AnonymousTypeParameter ((Expression)yyVals[0+yyTop], lt.Value, lt.Location);
	  }
  break;
case 486:
#line 3590 "cs-parser.jay"
  {
		LocatedToken lt = (LocatedToken)yyVals[0+yyTop];
	  	yyVal = new AnonymousTypeParameter (new SimpleName (lt.Value, lt.Location),
	  		lt.Value, lt.Location);
	  }
  break;
case 487:
#line 3596 "cs-parser.jay"
  {
	  	MemberAccess ma = (MemberAccess) yyVals[0+yyTop];
	  	yyVal = new AnonymousTypeParameter (ma, ma.Identifier, ma.Location);
	  }
  break;
case 488:
#line 3601 "cs-parser.jay"
  {
		Report.Error (746, lexer.Location, "Invalid anonymous type member declarator. " +
		"Anonymous type members must be a member assignment, simple name or member access expression");
	  }
  break;
case 489:
#line 3609 "cs-parser.jay"
  {
		  yyVal = "";
	  }
  break;
case 490:
#line 3613 "cs-parser.jay"
  {
			yyVal = yyVals[0+yyTop];
	  }
  break;
case 491:
#line 3620 "cs-parser.jay"
  {
		yyVal = "";
	  }
  break;
case 492:
#line 3624 "cs-parser.jay"
  {
		yyVal = "?";
	  }
  break;
case 493:
#line 3628 "cs-parser.jay"
  {
		if ((bool) yyVals[-1+yyTop])
			yyVal = "?" + yyVals[0+yyTop];
		else
			yyVal = yyVals[0+yyTop];
	  }
  break;
case 494:
#line 3635 "cs-parser.jay"
  {
		if ((bool) yyVals[-2+yyTop])
			yyVal = "?" + yyVals[-1+yyTop] + "?";
		else
			yyVal = yyVals[-1+yyTop] + "?";
	  }
  break;
case 495:
#line 3645 "cs-parser.jay"
  {
		  yyVal = (string) yyVals[0+yyTop] + (string) yyVals[-1+yyTop];
	  }
  break;
case 496:
#line 3652 "cs-parser.jay"
  {
		yyVal = "[" + (string) yyVals[-1+yyTop] + "]";
	  }
  break;
case 497:
#line 3659 "cs-parser.jay"
  {
		yyVal = "";
	  }
  break;
case 498:
#line 3663 "cs-parser.jay"
  {
		  yyVal = yyVals[0+yyTop];
	  }
  break;
case 499:
#line 3670 "cs-parser.jay"
  {
		yyVal = ",";
	  }
  break;
case 500:
#line 3674 "cs-parser.jay"
  {
		yyVal = (string) yyVals[-1+yyTop] + ",";
	  }
  break;
case 501:
#line 3681 "cs-parser.jay"
  {
		yyVal = null;
	  }
  break;
case 502:
#line 3685 "cs-parser.jay"
  {
		yyVal = yyVals[0+yyTop];
	  }
  break;
case 503:
#line 3692 "cs-parser.jay"
  {
		ArrayList list = new ArrayList (4);
		yyVal = list;
	  }
  break;
case 504:
#line 3697 "cs-parser.jay"
  {
		yyVal = (ArrayList) yyVals[-2+yyTop];
	  }
  break;
case 505:
#line 3704 "cs-parser.jay"
  {
		ArrayList list = new ArrayList (4);
		list.Add (yyVals[0+yyTop]);
		yyVal = list;
	  }
  break;
case 506:
#line 3710 "cs-parser.jay"
  {
		ArrayList list = (ArrayList) yyVals[-2+yyTop];
		list.Add (yyVals[0+yyTop]);
		yyVal = list;
	  }
  break;
case 507:
#line 3719 "cs-parser.jay"
  {
	  	pushed_current_array_type = current_array_type;
	  	lexer.TypeOfParsing = true;
	  }
  break;
case 508:
#line 3724 "cs-parser.jay"
  {
	  	lexer.TypeOfParsing = false;
		Expression type = (Expression)yyVals[-1+yyTop];
		if (type == TypeManager.system_void_expr)
			yyVal = new TypeOfVoid ((Location) yyVals[-4+yyTop]);
		else
			yyVal = new TypeOf (type, (Location) yyVals[-4+yyTop]);
		current_array_type = pushed_current_array_type;
	  }
  break;
case 509:
#line 3737 "cs-parser.jay"
  {
		yyVal = yyVals[0+yyTop];
	  }
  break;
case 510:
#line 3741 "cs-parser.jay"
  {
		yyVal = new UnboundTypeExpression ((MemberName)yyVals[0+yyTop], lexer.Location);
	  }
  break;
case 511:
#line 3748 "cs-parser.jay"
  {
		if (RootContext.Version < LanguageVersion.ISO_2)
			Report.FeatureIsNotAvailable (lexer.Location, "generics");
	  
		LocatedToken lt = (LocatedToken) yyVals[-1+yyTop];
		TypeArguments ta = new TypeArguments ((int)yyVals[0+yyTop], lt.Location);

		yyVal = new MemberName (lt.Value, ta, lt.Location);
	  }
  break;
case 512:
#line 3758 "cs-parser.jay"
  {
		LocatedToken lt = (LocatedToken) yyVals[-3+yyTop];
		MemberName left = new MemberName (lt.Value, lt.Location);
		lt = (LocatedToken) yyVals[-1+yyTop];
		TypeArguments ta = new TypeArguments ((int)yyVals[0+yyTop], lt.Location);
		
		yyVal = new MemberName (left, lt.Value, ta, lt.Location);
	  }
  break;
case 513:
#line 3767 "cs-parser.jay"
  {
		LocatedToken lt = (LocatedToken) yyVals[-1+yyTop];
		TypeArguments ta = new TypeArguments ((int)yyVals[0+yyTop], lt.Location);
		
	  	yyVal = new MemberName ((MemberName)yyVals[-3+yyTop], lt.Value, ta, lt.Location);
	  }
  break;
case 514:
#line 3774 "cs-parser.jay"
  {
		LocatedToken lt = (LocatedToken) yyVals[-1+yyTop];
		TypeArguments ta = new TypeArguments ((int)yyVals[0+yyTop], lt.Location);
		
	  	yyVal = new MemberName ((MemberName)yyVals[-3+yyTop], lt.Value, ta, lt.Location);
	  }
  break;
case 515:
#line 3784 "cs-parser.jay"
  { 
		yyVal = new SizeOf ((Expression) yyVals[-1+yyTop], (Location) yyVals[-3+yyTop]);
	  }
  break;
case 516:
#line 3791 "cs-parser.jay"
  {
		yyVal = new CheckedExpr ((Expression) yyVals[-1+yyTop], (Location) yyVals[-3+yyTop]);
	  }
  break;
case 517:
#line 3798 "cs-parser.jay"
  {
		yyVal = new UnCheckedExpr ((Expression) yyVals[-1+yyTop], (Location) yyVals[-3+yyTop]);
	  }
  break;
case 518:
#line 3805 "cs-parser.jay"
  {
		Expression deref;
		LocatedToken lt = (LocatedToken) yyVals[0+yyTop];

		deref = new Indirection ((Expression) yyVals[-2+yyTop], lt.Location);
		yyVal = new MemberAccess (deref, lt.Value);
	  }
  break;
case 519:
#line 3816 "cs-parser.jay"
  {
		start_anonymous (false, (Parameters) yyVals[0+yyTop], (Location) yyVals[-1+yyTop]);
	  }
  break;
case 520:
#line 3820 "cs-parser.jay"
  {
		yyVal = end_anonymous ((ToplevelBlock) yyVals[0+yyTop], (Location) yyVals[-3+yyTop]);
	}
  break;
case 521:
#line 3826 "cs-parser.jay"
  { yyVal = null; }
  break;
case 523:
#line 3832 "cs-parser.jay"
  {
	  	params_modifiers_not_allowed = true; 
	  }
  break;
case 524:
#line 3836 "cs-parser.jay"
  {
	  	params_modifiers_not_allowed = false;
	  	yyVal = yyVals[-1+yyTop];
	  }
  break;
case 525:
#line 3844 "cs-parser.jay"
  {
		if (RootContext.Version < LanguageVersion.ISO_2)
			Report.FeatureIsNotAvailable (lexer.Location, "default value expression");

		yyVal = new DefaultValueExpression ((Expression) yyVals[-1+yyTop], lexer.Location);
	  }
  break;
case 527:
#line 3855 "cs-parser.jay"
  {
		yyVal = new Unary (Unary.Operator.LogicalNot, (Expression) yyVals[0+yyTop], (Location) yyVals[-1+yyTop]);
	  }
  break;
case 528:
#line 3859 "cs-parser.jay"
  {
		yyVal = new Unary (Unary.Operator.OnesComplement, (Expression) yyVals[0+yyTop], (Location) yyVals[-1+yyTop]);
	  }
  break;
case 530:
#line 3867 "cs-parser.jay"
  {
		yyVal = new Cast ((Expression) yyVals[-2+yyTop], (Expression) yyVals[0+yyTop]);
	  }
  break;
case 531:
#line 3871 "cs-parser.jay"
  {
	  	yyVal = new Cast ((Expression) yyVals[-2+yyTop], (Expression) yyVals[0+yyTop]);
	  }
  break;
case 532:
#line 3875 "cs-parser.jay"
  {
		yyVal = new Cast ((Expression) yyVals[-2+yyTop], (Expression) yyVals[0+yyTop]);
	  }
  break;
case 534:
#line 3883 "cs-parser.jay"
  {
		/* TODO: wrong location*/
		yyVal = new Cast ((Expression) yyVals[-2+yyTop], (Expression) yyVals[0+yyTop], lexer.Location);
	  }
  break;
case 536:
#line 3896 "cs-parser.jay"
  { 
	  	yyVal = new Unary (Unary.Operator.UnaryPlus, (Expression) yyVals[0+yyTop], (Location) yyVals[-1+yyTop]);
	  }
  break;
case 537:
#line 3900 "cs-parser.jay"
  { 
		yyVal = new Unary (Unary.Operator.UnaryNegation, (Expression) yyVals[0+yyTop], (Location) yyVals[-1+yyTop]);
	  }
  break;
case 538:
#line 3904 "cs-parser.jay"
  {
		yyVal = new UnaryMutator (UnaryMutator.Mode.PreIncrement,
				       (Expression) yyVals[0+yyTop], (Location) yyVals[-1+yyTop]);
	  }
  break;
case 539:
#line 3909 "cs-parser.jay"
  {
		yyVal = new UnaryMutator (UnaryMutator.Mode.PreDecrement,
				       (Expression) yyVals[0+yyTop], (Location) yyVals[-1+yyTop]);
	  }
  break;
case 540:
#line 3914 "cs-parser.jay"
  {
		yyVal = new Indirection ((Expression) yyVals[0+yyTop], (Location) yyVals[-1+yyTop]);
	  }
  break;
case 541:
#line 3918 "cs-parser.jay"
  {
		yyVal = new Unary (Unary.Operator.AddressOf, (Expression) yyVals[0+yyTop], (Location) yyVals[-1+yyTop]);
	  }
  break;
case 543:
#line 3926 "cs-parser.jay"
  {
		yyVal = new Binary (Binary.Operator.Multiply, 
			         (Expression) yyVals[-2+yyTop], (Expression) yyVals[0+yyTop]);
	  }
  break;
case 544:
#line 3931 "cs-parser.jay"
  {
		yyVal = new Binary (Binary.Operator.Division, 
			         (Expression) yyVals[-2+yyTop], (Expression) yyVals[0+yyTop]);
	  }
  break;
case 545:
#line 3936 "cs-parser.jay"
  {
		yyVal = new Binary (Binary.Operator.Modulus, 
			         (Expression) yyVals[-2+yyTop], (Expression) yyVals[0+yyTop]);
	  }
  break;
case 547:
#line 3945 "cs-parser.jay"
  {
		yyVal = new Binary (Binary.Operator.Addition, 
			         (Expression) yyVals[-2+yyTop], (Expression) yyVals[0+yyTop]);
	  }
  break;
case 548:
#line 3950 "cs-parser.jay"
  {
		yyVal = new Binary (Binary.Operator.Subtraction, 
			         (Expression) yyVals[-2+yyTop], (Expression) yyVals[0+yyTop]);
	  }
  break;
case 550:
#line 3959 "cs-parser.jay"
  {
		yyVal = new Binary (Binary.Operator.LeftShift, 
			         (Expression) yyVals[-2+yyTop], (Expression) yyVals[0+yyTop]);
	  }
  break;
case 551:
#line 3964 "cs-parser.jay"
  {
		yyVal = new Binary (Binary.Operator.RightShift, 
			         (Expression) yyVals[-2+yyTop], (Expression) yyVals[0+yyTop]);
	  }
  break;
case 552:
#line 3972 "cs-parser.jay"
  {
		yyVal = false;
	  }
  break;
case 553:
#line 3976 "cs-parser.jay"
  {
		lexer.PutbackNullable ();
		yyVal = true;
	  }
  break;
case 554:
#line 3984 "cs-parser.jay"
  {
		if (((bool) yyVals[0+yyTop]) && (yyVals[-1+yyTop] is ComposedCast))
			yyVal = ((ComposedCast) yyVals[-1+yyTop]).RemoveNullable ();
		else
			yyVal = yyVals[-1+yyTop];
	  }
  break;
case 556:
#line 3995 "cs-parser.jay"
  {
		yyVal = new Binary (Binary.Operator.LessThan, 
			         (Expression) yyVals[-2+yyTop], (Expression) yyVals[0+yyTop]);
	  }
  break;
case 557:
#line 4000 "cs-parser.jay"
  {
		yyVal = new Binary (Binary.Operator.GreaterThan, 
			         (Expression) yyVals[-2+yyTop], (Expression) yyVals[0+yyTop]);
	  }
  break;
case 558:
#line 4005 "cs-parser.jay"
  {
		yyVal = new Binary (Binary.Operator.LessThanOrEqual, 
			         (Expression) yyVals[-2+yyTop], (Expression) yyVals[0+yyTop]);
	  }
  break;
case 559:
#line 4010 "cs-parser.jay"
  {
		yyVal = new Binary (Binary.Operator.GreaterThanOrEqual, 
			         (Expression) yyVals[-2+yyTop], (Expression) yyVals[0+yyTop]);
	  }
  break;
case 560:
#line 4015 "cs-parser.jay"
  {
		yyErrorFlag = 3;
	  }
  break;
case 561:
#line 4018 "cs-parser.jay"
  {
		yyVal = new Is ((Expression) yyVals[-3+yyTop], (Expression) yyVals[0+yyTop], (Location) yyVals[-2+yyTop]);
	  }
  break;
case 562:
#line 4022 "cs-parser.jay"
  {
		yyErrorFlag = 3;
	  }
  break;
case 563:
#line 4025 "cs-parser.jay"
  {
		yyVal = new As ((Expression) yyVals[-3+yyTop], (Expression) yyVals[0+yyTop], (Location) yyVals[-2+yyTop]);
	  }
  break;
case 565:
#line 4033 "cs-parser.jay"
  {
		yyVal = new Binary (Binary.Operator.Equality, 
			         (Expression) yyVals[-2+yyTop], (Expression) yyVals[0+yyTop]);
	  }
  break;
case 566:
#line 4038 "cs-parser.jay"
  {
		yyVal = new Binary (Binary.Operator.Inequality, 
			         (Expression) yyVals[-2+yyTop], (Expression) yyVals[0+yyTop]);
	  }
  break;
case 568:
#line 4047 "cs-parser.jay"
  {
		yyVal = new Binary (Binary.Operator.BitwiseAnd, 
			         (Expression) yyVals[-2+yyTop], (Expression) yyVals[0+yyTop]);
	  }
  break;
case 570:
#line 4056 "cs-parser.jay"
  {
		yyVal = new Binary (Binary.Operator.ExclusiveOr, 
			         (Expression) yyVals[-2+yyTop], (Expression) yyVals[0+yyTop]);
	  }
  break;
case 572:
#line 4065 "cs-parser.jay"
  {
		yyVal = new Binary (Binary.Operator.BitwiseOr, 
			         (Expression) yyVals[-2+yyTop], (Expression) yyVals[0+yyTop]);
	  }
  break;
case 574:
#line 4074 "cs-parser.jay"
  {
		yyVal = new Binary (Binary.Operator.LogicalAnd, 
			         (Expression) yyVals[-2+yyTop], (Expression) yyVals[0+yyTop]);
	  }
  break;
case 576:
#line 4083 "cs-parser.jay"
  {
		yyVal = new Binary (Binary.Operator.LogicalOr, 
			         (Expression) yyVals[-2+yyTop], (Expression) yyVals[0+yyTop]);
	  }
  break;
case 578:
#line 4092 "cs-parser.jay"
  {
		yyVal = new Conditional ((Expression) yyVals[-4+yyTop], (Expression) yyVals[-2+yyTop], (Expression) yyVals[0+yyTop]);
	  }
  break;
case 579:
#line 4096 "cs-parser.jay"
  {
		if (RootContext.Version < LanguageVersion.ISO_2)
			Report.FeatureIsNotAvailable (GetLocation (yyVals[-1+yyTop]), "null coalescing operator");
			
		yyVal = new Nullable.NullCoalescingOperator ((Expression) yyVals[-2+yyTop], (Expression) yyVals[0+yyTop], lexer.Location);
	  }
  break;
case 580:
#line 4104 "cs-parser.jay"
  {
		yyVal = new ComposedCast ((Expression) yyVals[-2+yyTop], "?", lexer.Location);
		lexer.PutbackCloseParens ();
	  }
  break;
case 581:
#line 4112 "cs-parser.jay"
  {
		yyVal = new Assign ((Expression) yyVals[-2+yyTop], (Expression) yyVals[0+yyTop]);
	  }
  break;
case 582:
#line 4116 "cs-parser.jay"
  {
		yyVal = new CompoundAssign (
			Binary.Operator.Multiply, (Expression) yyVals[-2+yyTop], (Expression) yyVals[0+yyTop]);
	  }
  break;
case 583:
#line 4121 "cs-parser.jay"
  {
		yyVal = new CompoundAssign (
			Binary.Operator.Division, (Expression) yyVals[-2+yyTop], (Expression) yyVals[0+yyTop]);
	  }
  break;
case 584:
#line 4126 "cs-parser.jay"
  {
		yyVal = new CompoundAssign (
			Binary.Operator.Modulus, (Expression) yyVals[-2+yyTop], (Expression) yyVals[0+yyTop]);
	  }
  break;
case 585:
#line 4131 "cs-parser.jay"
  {
		yyVal = new CompoundAssign (
			Binary.Operator.Addition, (Expression) yyVals[-2+yyTop], (Expression) yyVals[0+yyTop]);
	  }
  break;
case 586:
#line 4136 "cs-parser.jay"
  {
		yyVal = new CompoundAssign (
			Binary.Operator.Subtraction, (Expression) yyVals[-2+yyTop], (Expression) yyVals[0+yyTop]);
	  }
  break;
case 587:
#line 4141 "cs-parser.jay"
  {
		yyVal = new CompoundAssign (
			Binary.Operator.LeftShift, (Expression) yyVals[-2+yyTop], (Expression) yyVals[0+yyTop]);
	  }
  break;
case 588:
#line 4146 "cs-parser.jay"
  {
		yyVal = new CompoundAssign (
			Binary.Operator.RightShift, (Expression) yyVals[-2+yyTop], (Expression) yyVals[0+yyTop]);
	  }
  break;
case 589:
#line 4151 "cs-parser.jay"
  {
		yyVal = new CompoundAssign (
			Binary.Operator.BitwiseAnd, (Expression) yyVals[-2+yyTop], (Expression) yyVals[0+yyTop]);
	  }
  break;
case 590:
#line 4156 "cs-parser.jay"
  {
		yyVal = new CompoundAssign (
			Binary.Operator.BitwiseOr, (Expression) yyVals[-2+yyTop], (Expression) yyVals[0+yyTop]);
	  }
  break;
case 591:
#line 4161 "cs-parser.jay"
  {
		yyVal = new CompoundAssign (
			Binary.Operator.ExclusiveOr, (Expression) yyVals[-2+yyTop], (Expression) yyVals[0+yyTop]);
	  }
  break;
case 592:
#line 4169 "cs-parser.jay"
  {
		ArrayList pars = new ArrayList (4);
		pars.Add (yyVals[0+yyTop]);

		yyVal = pars;
	  }
  break;
case 593:
#line 4176 "cs-parser.jay"
  {
		ArrayList pars = (ArrayList) yyVals[-2+yyTop];
		Parameter p = (Parameter)yyVals[0+yyTop];
		if (pars[0].GetType () != p.GetType ()) {
			Report.Error (748, p.Location, "All lambda parameters must be typed either explicitly or implicitly");
		}
		
		pars.Add (p);
		yyVal = pars;
	  }
  break;
case 594:
#line 4190 "cs-parser.jay"
  {
		LocatedToken lt = (LocatedToken) yyVals[0+yyTop];

		yyVal = new Parameter ((Expression) yyVals[-1+yyTop], lt.Value, (Parameter.Modifier) yyVals[-2+yyTop], null, lt.Location);
	  }
  break;
case 595:
#line 4196 "cs-parser.jay"
  {
		LocatedToken lt = (LocatedToken) yyVals[0+yyTop];

		yyVal = new Parameter ((Expression) yyVals[-1+yyTop], lt.Value, Parameter.Modifier.NONE, null, lt.Location);
	  }
  break;
case 596:
#line 4202 "cs-parser.jay"
  {
	  	LocatedToken lt = (LocatedToken) yyVals[0+yyTop];
		yyVal = new ImplicitLambdaParameter (lt.Value, lt.Location);
	  }
  break;
case 597:
#line 4209 "cs-parser.jay"
  { yyVal = Parameters.EmptyReadOnlyParameters; }
  break;
case 598:
#line 4210 "cs-parser.jay"
  { 
		ArrayList pars_list = (ArrayList) yyVals[0+yyTop];
		yyVal = new Parameters ((Parameter[])pars_list.ToArray (typeof (Parameter)));
	  }
  break;
case 599:
#line 4217 "cs-parser.jay"
  {
		start_block (lexer.Location);
	  }
  break;
case 600:
#line 4221 "cs-parser.jay"
  {
		Block b = end_block (lexer.Location);
		b.AddStatement (new ContextualReturn ((Expression) yyVals[0+yyTop]));
		yyVal = b;
	  }
  break;
case 601:
#line 4226 "cs-parser.jay"
  { 
	  	yyVal = yyVals[0+yyTop]; 
	  }
  break;
case 602:
#line 4233 "cs-parser.jay"
  {
		LocatedToken lt = (LocatedToken) yyVals[-1+yyTop];
		Parameter p = new ImplicitLambdaParameter (lt.Value, lt.Location);
		start_anonymous (true, new Parameters (p), (Location) yyVals[0+yyTop]);
	  }
  break;
case 603:
#line 4239 "cs-parser.jay"
  {
		yyVal = end_anonymous ((ToplevelBlock) yyVals[0+yyTop], (Location) yyVals[-2+yyTop]);
	  }
  break;
case 604:
#line 4243 "cs-parser.jay"
  {
		start_anonymous (true, (Parameters) yyVals[-2+yyTop], (Location) yyVals[0+yyTop]);
	  }
  break;
case 605:
#line 4247 "cs-parser.jay"
  {
		yyVal = end_anonymous ((ToplevelBlock) yyVals[0+yyTop], (Location) yyVals[-2+yyTop]);
	  }
  break;
case 613:
#line 4279 "cs-parser.jay"
  {
		lexer.ConstraintsParsing = true;
	  }
  break;
case 614:
#line 4283 "cs-parser.jay"
  {
		MemberName name = MakeName ((MemberName) yyVals[0+yyTop]);
		push_current_class (new Class (current_namespace, current_class, name, (int) yyVals[-4+yyTop], (Attributes) yyVals[-5+yyTop]), yyVals[-3+yyTop]);
	  }
  break;
case 615:
#line 4289 "cs-parser.jay"
  {
		lexer.ConstraintsParsing = false;

		current_class.SetParameterInfo ((ArrayList) yyVals[0+yyTop]);

		if (RootContext.Documentation != null) {
			current_container.DocComment = Lexer.consume_doc_comment ();
			Lexer.doc_state = XmlCommentState.Allowed;
		}
	  }
  break;
case 616:
#line 4300 "cs-parser.jay"
  {
		if (RootContext.Documentation != null)
			Lexer.doc_state = XmlCommentState.Allowed;
	  }
  break;
case 617:
#line 4305 "cs-parser.jay"
  {
		yyVal = pop_current_class ();
	  }
  break;
case 618:
#line 4312 "cs-parser.jay"
  { yyVal = null; }
  break;
case 619:
#line 4314 "cs-parser.jay"
  { yyVal = yyVals[0+yyTop]; }
  break;
case 620:
#line 4318 "cs-parser.jay"
  { yyVal = (int) 0; }
  break;
case 623:
#line 4325 "cs-parser.jay"
  { 
		int m1 = (int) yyVals[-1+yyTop];
		int m2 = (int) yyVals[0+yyTop];

		if ((m1 & m2) != 0) {
			Location l = lexer.Location;
			Report.Error (1004, l, "Duplicate `{0}' modifier", Modifiers.Name (m2));
		}
		yyVal = (int) (m1 | m2);
	  }
  break;
case 624:
#line 4338 "cs-parser.jay"
  { yyVal = Modifiers.NEW; }
  break;
case 625:
#line 4339 "cs-parser.jay"
  { yyVal = Modifiers.PUBLIC; }
  break;
case 626:
#line 4340 "cs-parser.jay"
  { yyVal = Modifiers.PROTECTED; }
  break;
case 627:
#line 4341 "cs-parser.jay"
  { yyVal = Modifiers.INTERNAL; }
  break;
case 628:
#line 4342 "cs-parser.jay"
  { yyVal = Modifiers.PRIVATE; }
  break;
case 629:
#line 4343 "cs-parser.jay"
  { yyVal = Modifiers.ABSTRACT; }
  break;
case 630:
#line 4344 "cs-parser.jay"
  { yyVal = Modifiers.SEALED; }
  break;
case 631:
#line 4345 "cs-parser.jay"
  { yyVal = Modifiers.STATIC; }
  break;
case 632:
#line 4346 "cs-parser.jay"
  { yyVal = Modifiers.READONLY; }
  break;
case 633:
#line 4347 "cs-parser.jay"
  { yyVal = Modifiers.VIRTUAL; }
  break;
case 634:
#line 4348 "cs-parser.jay"
  { yyVal = Modifiers.OVERRIDE; }
  break;
case 635:
#line 4349 "cs-parser.jay"
  { yyVal = Modifiers.EXTERN; }
  break;
case 636:
#line 4350 "cs-parser.jay"
  { yyVal = Modifiers.VOLATILE; }
  break;
case 637:
#line 4351 "cs-parser.jay"
  { yyVal = Modifiers.UNSAFE; }
  break;
case 640:
#line 4360 "cs-parser.jay"
  { current_container.AddBasesForPart (current_class, (ArrayList) yyVals[0+yyTop]); }
  break;
case 641:
#line 4364 "cs-parser.jay"
  { yyVal = null; }
  break;
case 642:
#line 4366 "cs-parser.jay"
  { yyVal = yyVals[0+yyTop]; }
  break;
case 643:
#line 4370 "cs-parser.jay"
  {
		ArrayList constraints = new ArrayList (1);
		constraints.Add (yyVals[0+yyTop]);
		yyVal = constraints;
	  }
  break;
case 644:
#line 4375 "cs-parser.jay"
  {
		ArrayList constraints = (ArrayList) yyVals[-1+yyTop];
		Constraints new_constraint = (Constraints)yyVals[0+yyTop];

		foreach (Constraints c in constraints) {
			if (new_constraint.TypeParameter == c.TypeParameter) {
				Report.Error (409, new_constraint.Location, "A constraint clause has already been specified for type parameter `{0}'",
					new_constraint.TypeParameter);
			}
		}

		constraints.Add (new_constraint);
		yyVal = constraints;
	  }
  break;
case 645:
#line 4392 "cs-parser.jay"
  {
		LocatedToken lt = (LocatedToken) yyVals[-2+yyTop];
		yyVal = new Constraints (lt.Value, (ArrayList) yyVals[0+yyTop], lt.Location);
	  }
  break;
case 646:
#line 4399 "cs-parser.jay"
  {
		ArrayList constraints = new ArrayList (1);
		constraints.Add (yyVals[0+yyTop]);
		yyVal = constraints;
	  }
  break;
case 647:
#line 4404 "cs-parser.jay"
  {
		ArrayList constraints = (ArrayList) yyVals[-2+yyTop];

		constraints.Add (yyVals[0+yyTop]);
		yyVal = constraints;
	  }
  break;
case 649:
#line 4414 "cs-parser.jay"
  {
		yyVal = SpecialConstraint.Constructor;
	  }
  break;
case 650:
#line 4417 "cs-parser.jay"
  {
		yyVal = SpecialConstraint.ReferenceType;
	  }
  break;
case 651:
#line 4420 "cs-parser.jay"
  {
		yyVal = SpecialConstraint.ValueType;
	  }
  break;
case 652:
#line 4440 "cs-parser.jay"
  {
		++lexer.parsing_block;
		start_block ((Location) yyVals[0+yyTop]);
	  }
  break;
case 653:
#line 4445 "cs-parser.jay"
  {
	 	--lexer.parsing_block;
		yyVal = end_block ((Location) yyVals[0+yyTop]);
	  }
  break;
case 654:
#line 4453 "cs-parser.jay"
  {
		++lexer.parsing_block;
	  }
  break;
case 655:
#line 4457 "cs-parser.jay"
  {
		--lexer.parsing_block;
		yyVal = end_block ((Location) yyVals[0+yyTop]);
	  }
  break;
case 660:
#line 4475 "cs-parser.jay"
  {
		if (yyVals[0+yyTop] != null && (Block) yyVals[0+yyTop] != current_block){
			current_block.AddStatement ((Statement) yyVals[0+yyTop]);
			current_block = (Block) yyVals[0+yyTop];
		}
	  }
  break;
case 661:
#line 4482 "cs-parser.jay"
  {
		current_block.AddStatement ((Statement) yyVals[0+yyTop]);
	  }
  break;
case 677:
#line 4507 "cs-parser.jay"
  {
		  Report.Error (1023, GetLocation (yyVals[0+yyTop]), "An embedded statement may not be a declaration or labeled statement");
		  yyVal = null;
	  }
  break;
case 678:
#line 4512 "cs-parser.jay"
  {
		  Report.Error (1023, GetLocation (yyVals[0+yyTop]), "An embedded statement may not be a declaration or labeled statement");
		  yyVal = null;
	  }
  break;
case 679:
#line 4520 "cs-parser.jay"
  {
		  yyVal = EmptyStatement.Value;
	  }
  break;
case 680:
#line 4527 "cs-parser.jay"
  {
		LocatedToken lt = (LocatedToken) yyVals[-1+yyTop];
		LabeledStatement labeled = new LabeledStatement (lt.Value, lt.Location);

		if (current_block.AddLabel (labeled))
			current_block.AddStatement (labeled);
	  }
  break;
case 682:
#line 4539 "cs-parser.jay"
  {
		current_array_type = null;
		if (yyVals[-1+yyTop] != null){
			DictionaryEntry de = (DictionaryEntry) yyVals[-1+yyTop];
			Expression e = (Expression) de.Key;

			yyVal = declare_local_variables (e, (ArrayList) de.Value, e.Location);
		}
	  }
  break;
case 683:
#line 4550 "cs-parser.jay"
  {
		current_array_type = null;
		if (yyVals[-1+yyTop] != null){
			DictionaryEntry de = (DictionaryEntry) yyVals[-1+yyTop];

			yyVal = declare_local_constants ((Expression) de.Key, (ArrayList) de.Value);
		}
	  }
  break;
case 684:
#line 4568 "cs-parser.jay"
  { 
		/* FIXME: Do something smart here regarding the composition of the type.*/

		/* Ok, the above "primary_expression" is there to get rid of*/
		/* both reduce/reduce and shift/reduces in the grammar, it should*/
		/* really just be "type_name".  If you use type_name, a reduce/reduce*/
		/* creeps up.  If you use namespace_or_type_name (which is all we need*/
		/* really) two shift/reduces appear.*/
		/* */

		/* So the super-trick is that primary_expression*/
		/* can only be either a SimpleName or a MemberAccess. */
		/* The MemberAccess case arises when you have a fully qualified type-name like :*/
		/* Foo.Bar.Blah i;*/
		/* SimpleName is when you have*/
		/* Blah i;*/
		  
		Expression expr = (Expression) yyVals[-1+yyTop];
		SimpleName sn = expr as SimpleName;
		if (!(sn != null || expr is MemberAccess || expr is ComposedCast || expr is ConstructedType || expr is QualifiedAliasMember)) {
			Error_ExpectingTypeName (expr);
			yyVal = null;
		} else {
			/**/
			/* So we extract the string corresponding to the SimpleName*/
			/* or MemberAccess*/
			/* */

			if ((string) yyVals[0+yyTop] == "") {
				if (sn != null && RootContext.Version > LanguageVersion.ISO_2 && sn.Name == "var")
					yyVal = new VarExpr (sn.Location);
				else
					yyVal = yyVals[-1+yyTop];
			} else
				yyVal = new ComposedCast ((Expression) yyVals[-1+yyTop], (string) yyVals[0+yyTop]);
		}
	  }
  break;
case 685:
#line 4606 "cs-parser.jay"
  {
		if ((string) yyVals[0+yyTop] == "")
			yyVal = yyVals[-1+yyTop];
		else
			yyVal = current_array_type = new ComposedCast ((Expression) yyVals[-1+yyTop], (string) yyVals[0+yyTop], lexer.Location);
	  }
  break;
case 686:
#line 4616 "cs-parser.jay"
  {
		Expression expr = (Expression) yyVals[-1+yyTop];  

		if (!(expr is SimpleName || expr is MemberAccess || expr is ComposedCast || expr is ConstructedType || expr is QualifiedAliasMember)) {
			Error_ExpectingTypeName (expr);

			yyVal = null;
		} else 
			yyVal = new ComposedCast ((Expression) yyVals[-1+yyTop], "*");
	  }
  break;
case 687:
#line 4627 "cs-parser.jay"
  {
		yyVal = new ComposedCast ((Expression) yyVals[-1+yyTop], "*", lexer.Location);
	  }
  break;
case 688:
#line 4631 "cs-parser.jay"
  {
		yyVal = new ComposedCast (TypeManager.system_void_expr, "*", (Location) yyVals[-1+yyTop]);
	  }
  break;
case 689:
#line 4635 "cs-parser.jay"
  {
		yyVal = new ComposedCast ((Expression) yyVals[-1+yyTop], "*");
	  }
  break;
case 690:
#line 4642 "cs-parser.jay"
  {
		if (yyVals[-1+yyTop] != null) {
			VarExpr ve = yyVals[-1+yyTop] as VarExpr;
			if (ve != null)
				ve.VariableInitializer = (ArrayList)yyVals[0+yyTop];
				
			yyVal = new DictionaryEntry (yyVals[-1+yyTop], yyVals[0+yyTop]);
		} else
			yyVal = null;
	  }
  break;
case 691:
#line 4653 "cs-parser.jay"
  {
		if (yyVals[-2+yyTop] != null){
			Expression t;

			if ((string) yyVals[-1+yyTop] == "")
				t = (Expression) yyVals[-2+yyTop];
			else
				t = new ComposedCast ((Expression) yyVals[-2+yyTop], (string) yyVals[-1+yyTop]);
			yyVal = new DictionaryEntry (t, yyVals[0+yyTop]);
		} else 
			yyVal = null;
	  }
  break;
case 692:
#line 4669 "cs-parser.jay"
  {
		if (yyVals[-1+yyTop] != null)
			yyVal = new DictionaryEntry (yyVals[-1+yyTop], yyVals[0+yyTop]);
		else
			yyVal = null;
	  }
  break;
case 693:
#line 4678 "cs-parser.jay"
  { yyVal = yyVals[-1+yyTop]; }
  break;
case 694:
#line 4687 "cs-parser.jay"
  {
		Expression expr = (Expression) yyVals[0+yyTop];
		ExpressionStatement s = expr as ExpressionStatement;
		if (s == null) {
			expr.Error_InvalidExpressionStatement ();
			yyVal = null;
		}
		yyVal = new StatementExpression (s);
	  }
  break;
case 695:
#line 4697 "cs-parser.jay"
  {
		Report.Error (1002, GetLocation (yyVals[0+yyTop]), "Expecting `;'");
		yyVal = null;
	  }
  break;
case 698:
#line 4711 "cs-parser.jay"
  { 
		Location l = (Location) yyVals[-4+yyTop];

		yyVal = new If ((Expression) yyVals[-2+yyTop], (Statement) yyVals[0+yyTop], l);

		/* FIXME: location for warning should be loc property of $5.*/
		if (yyVals[0+yyTop] == EmptyStatement.Value)
			Report.Warning (642, 3, l, "Possible mistaken empty statement");

	  }
  break;
case 699:
#line 4723 "cs-parser.jay"
  {
		Location l = (Location) yyVals[-6+yyTop];

		yyVal = new If ((Expression) yyVals[-4+yyTop], (Statement) yyVals[-2+yyTop], (Statement) yyVals[0+yyTop], l);

		/* FIXME: location for warning should be loc property of $5 and $7.*/
		if (yyVals[-2+yyTop] == EmptyStatement.Value)
			Report.Warning (642, 3, l, "Possible mistaken empty statement");
		if (yyVals[0+yyTop] == EmptyStatement.Value)
			Report.Warning (642, 3, l, "Possible mistaken empty statement");
	  }
  break;
case 700:
#line 4738 "cs-parser.jay"
  { 
		if (switch_stack == null)
			switch_stack = new Stack (2);
		switch_stack.Push (current_block);
	  }
  break;
case 701:
#line 4745 "cs-parser.jay"
  {
		yyVal = new Switch ((Expression) yyVals[-2+yyTop], (ArrayList) yyVals[0+yyTop], (Location) yyVals[-5+yyTop]);
		current_block = (Block) switch_stack.Pop ();
	  }
  break;
case 702:
#line 4755 "cs-parser.jay"
  {
		yyVal = yyVals[-1+yyTop];
	  }
  break;
case 703:
#line 4762 "cs-parser.jay"
  {
	  	Report.Warning (1522, 1, lexer.Location, "Empty switch block"); 
		yyVal = new ArrayList ();
	  }
  break;
case 705:
#line 4771 "cs-parser.jay"
  {
		ArrayList sections = new ArrayList (4);

		sections.Add (yyVals[0+yyTop]);
		yyVal = sections;
	  }
  break;
case 706:
#line 4778 "cs-parser.jay"
  {
		ArrayList sections = (ArrayList) yyVals[-1+yyTop];

		sections.Add (yyVals[0+yyTop]);
		yyVal = sections;
	  }
  break;
case 707:
#line 4788 "cs-parser.jay"
  {
		current_block = current_block.CreateSwitchBlock (lexer.Location);
	  }
  break;
case 708:
#line 4792 "cs-parser.jay"
  {
		yyVal = new SwitchSection ((ArrayList) yyVals[-2+yyTop], current_block.Explicit);
	  }
  break;
case 709:
#line 4799 "cs-parser.jay"
  {
		ArrayList labels = new ArrayList (4);

		labels.Add (yyVals[0+yyTop]);
		yyVal = labels;
	  }
  break;
case 710:
#line 4806 "cs-parser.jay"
  {
		ArrayList labels = (ArrayList) (yyVals[-1+yyTop]);
		labels.Add (yyVals[0+yyTop]);

		yyVal = labels;
	  }
  break;
case 711:
#line 4815 "cs-parser.jay"
  { yyVal = new SwitchLabel ((Expression) yyVals[-1+yyTop], (Location) yyVals[-2+yyTop]); }
  break;
case 712:
#line 4816 "cs-parser.jay"
  { yyVal = new SwitchLabel (null, (Location) yyVals[0+yyTop]); }
  break;
case 713:
#line 4817 "cs-parser.jay"
  {
		Report.Error (
			1523, GetLocation (yyVals[0+yyTop]), 
			"The keyword case or default must precede code in switch block");
	  }
  break;
case 718:
#line 4833 "cs-parser.jay"
  {
		Location l = (Location) yyVals[-4+yyTop];
		yyVal = new While ((Expression) yyVals[-2+yyTop], (Statement) yyVals[0+yyTop], l);
	  }
  break;
case 719:
#line 4842 "cs-parser.jay"
  {
		Location l = (Location) yyVals[-6+yyTop];

		yyVal = new Do ((Statement) yyVals[-5+yyTop], (Expression) yyVals[-2+yyTop], l);
	  }
  break;
case 720:
#line 4852 "cs-parser.jay"
  {
		Location l = lexer.Location;
		start_block (l);  
		Block assign_block = current_block;

		if (yyVals[-1+yyTop] is DictionaryEntry){
			DictionaryEntry de = (DictionaryEntry) yyVals[-1+yyTop];
			
			Expression type = (Expression) de.Key;
			ArrayList var_declarators = (ArrayList) de.Value;

			foreach (VariableDeclaration decl in var_declarators){

				LocalInfo vi;

				vi = current_block.AddVariable (type, decl.identifier, decl.Location);
				if (vi == null)
					continue;

				Expression expr = decl.expression_or_array_initializer;
					
				LocalVariableReference var;
				var = new LocalVariableReference (assign_block, decl.identifier, l);

				if (expr != null) {
					Assign a = new Assign (var, expr, decl.Location);
					
					assign_block.AddStatement (new StatementExpression (a));
				}
			}
			
			/* Note: the $$ below refers to the value of this code block, not of the LHS non-terminal.*/
			/* This can be referred to as $5 below.*/
			yyVal = null;
		} else {
			yyVal = yyVals[-1+yyTop];
		}
	  }
  break;
case 721:
#line 4893 "cs-parser.jay"
  {
		Location l = (Location) yyVals[-9+yyTop];

		For f = new For ((Statement) yyVals[-5+yyTop], (Expression) yyVals[-4+yyTop], (Statement) yyVals[-2+yyTop], (Statement) yyVals[0+yyTop], l);

		current_block.AddStatement (f);

		yyVal = end_block (lexer.Location);
	  }
  break;
case 722:
#line 4905 "cs-parser.jay"
  { yyVal = EmptyStatement.Value; }
  break;
case 726:
#line 4915 "cs-parser.jay"
  { yyVal = null; }
  break;
case 728:
#line 4920 "cs-parser.jay"
  { yyVal = EmptyStatement.Value; }
  break;
case 731:
#line 4930 "cs-parser.jay"
  {
		/* CHANGE: was `null'*/
		Statement s = (Statement) yyVals[0+yyTop];
		Block b = new Block (current_block, s.loc, lexer.Location);   

		b.AddStatement (s);
		yyVal = b;
	  }
  break;
case 732:
#line 4939 "cs-parser.jay"
  {
		Block b = (Block) yyVals[-2+yyTop];

		b.AddStatement ((Statement) yyVals[0+yyTop]);
		yyVal = yyVals[-2+yyTop];
	  }
  break;
case 733:
#line 4949 "cs-parser.jay"
  {
		Report.Error (230, (Location) yyVals[-5+yyTop], "Type and identifier are both required in a foreach statement");
		yyVal = null;
	  }
  break;
case 734:
#line 4955 "cs-parser.jay"
  {
		start_block (lexer.Location);
		Block foreach_block = current_block;

		LocatedToken lt = (LocatedToken) yyVals[-3+yyTop];
		Location l = lt.Location;
		LocalInfo vi = foreach_block.AddVariable ((Expression) yyVals[-4+yyTop], lt.Value, l);
		if (vi != null) {
			vi.SetReadOnlyContext (LocalInfo.ReadOnlyContext.Foreach);

			/* Get a writable reference to this read-only variable.*/
			/**/
			/* Note that the $$ here refers to the value of _this_ code block,*/
			/* not the value of the LHS non-terminal.  This can be referred to as $8 below.*/
			yyVal = new LocalVariableReference (foreach_block, lt.Value, l, vi, false);
		} else {
			yyVal = null;
		}
	  }
  break;
case 735:
#line 4975 "cs-parser.jay"
  {
		LocalVariableReference v = (LocalVariableReference) yyVals[-1+yyTop];
		Location l = (Location) yyVals[-8+yyTop];

		if (v != null) {
			Foreach f = new Foreach ((Expression) yyVals[-6+yyTop], v, (Expression) yyVals[-3+yyTop], (Statement) yyVals[0+yyTop], l);
			current_block.AddStatement (f);
		}

		yyVal = end_block (lexer.Location);
	  }
  break;
case 742:
#line 4999 "cs-parser.jay"
  {
		yyVal = new Break ((Location) yyVals[-1+yyTop]);
	  }
  break;
case 743:
#line 5006 "cs-parser.jay"
  {
		yyVal = new Continue ((Location) yyVals[-1+yyTop]);
	  }
  break;
case 744:
#line 5013 "cs-parser.jay"
  {
		LocatedToken lt = (LocatedToken) yyVals[-1+yyTop];
		yyVal = new Goto (lt.Value, lt.Location);
	  }
  break;
case 745:
#line 5018 "cs-parser.jay"
  {
		yyVal = new GotoCase ((Expression) yyVals[-1+yyTop], (Location) yyVals[-3+yyTop]);
	  }
  break;
case 746:
#line 5022 "cs-parser.jay"
  {
		yyVal = new GotoDefault ((Location) yyVals[-2+yyTop]);
	  }
  break;
case 747:
#line 5029 "cs-parser.jay"
  {
		yyVal = new Return ((Expression) yyVals[-1+yyTop], (Location) yyVals[-2+yyTop]);
	  }
  break;
case 748:
#line 5036 "cs-parser.jay"
  {
		yyVal = new Throw ((Expression) yyVals[-1+yyTop], (Location) yyVals[-2+yyTop]);
	  }
  break;
case 749:
#line 5043 "cs-parser.jay"
  {
		LocatedToken lt = (LocatedToken) yyVals[-3+yyTop];
		string s = lt.Value;
		if (s != "yield"){
			Report.Error (1003, lt.Location, "; expected");
			yyVal = null;
		}
		if (RootContext.Version == LanguageVersion.ISO_1){
			Report.FeatureIsNotAvailable (lt.Location, "yield statement");
			yyVal = null;
		}
		if (anonymous_host == null){
			Report.Error (204, lt.Location, "yield statement can only be used within a method, operator or property");
			yyVal = null;
		} else {
			anonymous_host.SetYields ();
			yyVal = new Yield ((Expression) yyVals[-1+yyTop], lt.Location); 
		}
	  }
  break;
case 750:
#line 5063 "cs-parser.jay"
  {
		Report.Error (1627, (Location) yyVals[-1+yyTop], "Expression expected after yield return");
		yyVal = null;
	  }
  break;
case 751:
#line 5068 "cs-parser.jay"
  {
		LocatedToken lt = (LocatedToken) yyVals[-2+yyTop];
		string s = lt.Value;
		if (s != "yield"){
			Report.Error (1003, lt.Location, "; expected");
			yyVal = null;
		}
		if (RootContext.Version == LanguageVersion.ISO_1){
			Report.FeatureIsNotAvailable (lt.Location, "yield statement");
			yyVal = null;
		}
		if (anonymous_host == null){
			Report.Error (204, lt.Location, "yield statement can only be used within a method, operator or property");
			yyVal = null;
		} else {
			anonymous_host.SetYields ();
			yyVal = new YieldBreak (lt.Location);
		}
	  }
  break;
case 754:
#line 5096 "cs-parser.jay"
  {
		yyVal = new TryCatch ((Block) yyVals[-1+yyTop], (ArrayList) yyVals[0+yyTop], (Location) yyVals[-2+yyTop], false);
	  }
  break;
case 755:
#line 5100 "cs-parser.jay"
  {
		yyVal = new TryFinally ((Statement) yyVals[-2+yyTop], (Block) yyVals[0+yyTop], (Location) yyVals[-3+yyTop]);
	  }
  break;
case 756:
#line 5104 "cs-parser.jay"
  {
		yyVal = new TryFinally (new TryCatch ((Block) yyVals[-3+yyTop], (ArrayList) yyVals[-2+yyTop], (Location) yyVals[-4+yyTop], true), (Block) yyVals[0+yyTop], (Location) yyVals[-4+yyTop]);
	  }
  break;
case 757:
#line 5108 "cs-parser.jay"
  {
		Report.Error (1524, (Location) yyVals[-2+yyTop], "Expected catch or finally");
		yyVal = null;
	  }
  break;
case 758:
#line 5116 "cs-parser.jay"
  {
		ArrayList l = new ArrayList (4);

		l.Add (yyVals[0+yyTop]);
		yyVal = l;
	  }
  break;
case 759:
#line 5123 "cs-parser.jay"
  {
		ArrayList l = (ArrayList) yyVals[-1+yyTop];

		l.Add (yyVals[0+yyTop]);
		yyVal = l;
	  }
  break;
case 760:
#line 5132 "cs-parser.jay"
  { yyVal = null; }
  break;
case 762:
#line 5138 "cs-parser.jay"
  {
		Expression type = null;
		
		if (yyVals[0+yyTop] != null) {
			DictionaryEntry cc = (DictionaryEntry) yyVals[0+yyTop];
			type = (Expression) cc.Key;
			LocatedToken lt = (LocatedToken) cc.Value;

			if (lt != null){
				ArrayList one = new ArrayList (4);

				one.Add (new VariableDeclaration (lt, null));

				start_block (lexer.Location);
				current_block = declare_local_variables (type, one, lt.Location);
			}
		}
	  }
  break;
case 763:
#line 5155 "cs-parser.jay"
  {
		Expression type = null;
		string id = null;
		Block var_block = null;

		if (yyVals[-2+yyTop] != null){
			DictionaryEntry cc = (DictionaryEntry) yyVals[-2+yyTop];
			type = (Expression) cc.Key;
			LocatedToken lt = (LocatedToken) cc.Value;

			if (lt != null){
				id = lt.Value;
				var_block = end_block (lexer.Location);
			}
		}

		yyVal = new Catch (type, id, (Block) yyVals[0+yyTop], var_block, ((Block) yyVals[0+yyTop]).loc);
	  }
  break;
case 764:
#line 5176 "cs-parser.jay"
  { yyVal = null; }
  break;
case 766:
#line 5182 "cs-parser.jay"
  {
		yyVal = new DictionaryEntry (yyVals[-2+yyTop], yyVals[-1+yyTop]);
	  }
  break;
case 767:
#line 5190 "cs-parser.jay"
  {
		yyVal = new Checked ((Block) yyVals[0+yyTop]);
	  }
  break;
case 768:
#line 5197 "cs-parser.jay"
  {
		yyVal = new Unchecked ((Block) yyVals[0+yyTop]);
	  }
  break;
case 769:
#line 5204 "cs-parser.jay"
  {
		RootContext.CheckUnsafeOption ((Location) yyVals[0+yyTop]);
	  }
  break;
case 770:
#line 5206 "cs-parser.jay"
  {
		yyVal = new Unsafe ((Block) yyVals[0+yyTop]);
	  }
  break;
case 771:
#line 5215 "cs-parser.jay"
  {
		ArrayList list = (ArrayList) yyVals[-1+yyTop];
		Expression type = (Expression) yyVals[-2+yyTop];
		Location l = (Location) yyVals[-4+yyTop];
		int top = list.Count;

		start_block (lexer.Location);

		for (int i = 0; i < top; i++){
			Pair p = (Pair) list [i];
			LocalInfo v;

			v = current_block.AddVariable (type, (string) p.First, l);
			if (v == null)
				continue;

			v.SetReadOnlyContext (LocalInfo.ReadOnlyContext.Fixed);
			v.Pinned = true;
			p.First = v;
			list [i] = p;
		}
	  }
  break;
case 772:
#line 5238 "cs-parser.jay"
  {
		Location l = (Location) yyVals[-6+yyTop];

		Fixed f = new Fixed ((Expression) yyVals[-4+yyTop], (ArrayList) yyVals[-3+yyTop], (Statement) yyVals[0+yyTop], l);

		current_block.AddStatement (f);

		yyVal = end_block (lexer.Location);
	  }
  break;
case 773:
#line 5250 "cs-parser.jay"
  { 
	   	ArrayList declarators = new ArrayList (4);
	   	if (yyVals[0+yyTop] != null)
			declarators.Add (yyVals[0+yyTop]);
		yyVal = declarators;
	  }
  break;
case 774:
#line 5257 "cs-parser.jay"
  {
		ArrayList declarators = (ArrayList) yyVals[-2+yyTop];
		if (yyVals[0+yyTop] != null)
			declarators.Add (yyVals[0+yyTop]);
		yyVal = declarators;
	  }
  break;
case 775:
#line 5267 "cs-parser.jay"
  {
		LocatedToken lt = (LocatedToken) yyVals[-2+yyTop];
		/* FIXME: keep location*/
		yyVal = new Pair (lt.Value, yyVals[0+yyTop]);
	  }
  break;
case 776:
#line 5273 "cs-parser.jay"
  {
		Report.Error (210, ((LocatedToken) yyVals[0+yyTop]).Location, "You must provide an initializer in a fixed or using statement declaration");
		yyVal = null;
	  }
  break;
case 777:
#line 5281 "cs-parser.jay"
  {
		/**/
 	  }
  break;
case 778:
#line 5285 "cs-parser.jay"
  {
		yyVal = new Lock ((Expression) yyVals[-3+yyTop], (Statement) yyVals[0+yyTop], (Location) yyVals[-5+yyTop]);
	  }
  break;
case 779:
#line 5292 "cs-parser.jay"
  {
		start_block (lexer.Location);
		Block assign_block = current_block;

		DictionaryEntry de = (DictionaryEntry) yyVals[-1+yyTop];
		Location l = (Location) yyVals[-3+yyTop];

		Expression type = (Expression) de.Key;
		ArrayList var_declarators = (ArrayList) de.Value;

		Stack vars = new Stack ();

		foreach (VariableDeclaration decl in var_declarators) {
			LocalInfo vi = current_block.AddVariable (type, decl.identifier, decl.Location);
			if (vi == null)
				continue;
			vi.SetReadOnlyContext (LocalInfo.ReadOnlyContext.Using);

			Expression expr = decl.expression_or_array_initializer;
			if (expr == null) {
				Report.Error (210, l, "You must provide an initializer in a fixed or using statement declaration");
				continue;
			}
			LocalVariableReference var;

			/* Get a writable reference to this read-only variable.*/
			var = new LocalVariableReference (assign_block, decl.identifier, l, vi, false);

			/* This is so that it is not a warning on using variables*/
			vi.Used = true;

			vars.Push (new DictionaryEntry (var, expr));

			/* Assign a = new Assign (var, expr, decl.Location);*/
			/* assign_block.AddStatement (new StatementExpression (a));*/
		}

		/* Note: the $$ here refers to the value of this code block and not of the LHS non-terminal.*/
		/* It can be referred to as $5 below.*/
		yyVal = vars;
	  }
  break;
case 780:
#line 5334 "cs-parser.jay"
  {
		Statement stmt = (Statement) yyVals[0+yyTop];
		Stack vars = (Stack) yyVals[-1+yyTop];
		Location l = (Location) yyVals[-5+yyTop];

		while (vars.Count > 0) {
			  DictionaryEntry de = (DictionaryEntry) vars.Pop ();
			  stmt = new Using ((Expression) de.Key, (Expression) de.Value, stmt, l);
		}
		current_block.AddStatement (stmt);
		yyVal = end_block (lexer.Location);
	  }
  break;
case 781:
#line 5347 "cs-parser.jay"
  {
		start_block (lexer.Location);
	  }
  break;
case 782:
#line 5351 "cs-parser.jay"
  {
		current_block.AddStatement (new UsingTemporary ((Expression) yyVals[-3+yyTop], (Statement) yyVals[0+yyTop], (Location) yyVals[-5+yyTop]));
		yyVal = end_block (lexer.Location);
	  }
  break;
case 783:
#line 5362 "cs-parser.jay"
  {
		++lexer.query_parsing;
	  }
  break;
case 784:
#line 5366 "cs-parser.jay"
  {
		if (--lexer.query_parsing == 1)
			lexer.query_parsing = 0;
			
		Linq.AQueryClause from = yyVals[-2+yyTop] as Linq.AQueryClause;
			
		from.Tail.Next = (Linq.AQueryClause)yyVals[0+yyTop];
		yyVal = from;
		
		current_block.SetEndLocation (lexer.Location);
		current_block = current_block.Parent;
	  }
  break;
case 785:
#line 5382 "cs-parser.jay"
  {
		current_block = new Linq.QueryBlock (current_block, GetLocation (yyVals[-3+yyTop]));
		LocatedToken lt = (LocatedToken) yyVals[-2+yyTop];
		
		current_block.AddVariable (Linq.ImplicitQueryParameter.ImplicitType.Instance, lt.Value, lt.Location);
		yyVal = new Linq.QueryExpression (lt, new Linq.QueryStartClause ((Expression)yyVals[0+yyTop]));
	  }
  break;
case 786:
#line 5390 "cs-parser.jay"
  {
		current_block = new Linq.QueryBlock (current_block, GetLocation (yyVals[-4+yyTop]));
		LocatedToken lt = (LocatedToken) yyVals[-2+yyTop];

		Expression type = (Expression)yyVals[-3+yyTop];
		current_block.AddVariable (type, lt.Value, lt.Location);
		yyVal = new Linq.QueryExpression (lt, new Linq.Cast (type, (Expression)yyVals[0+yyTop]));
	  }
  break;
case 787:
#line 5402 "cs-parser.jay"
  {
		LocatedToken lt = (LocatedToken) yyVals[-2+yyTop];
		
		current_block.AddVariable (Linq.ImplicitQueryParameter.ImplicitType.Instance,
			lt.Value, lt.Location);
			
		yyVal = new Linq.SelectMany (lt, (Expression)yyVals[0+yyTop]);			
	  }
  break;
case 788:
#line 5411 "cs-parser.jay"
  {
		LocatedToken lt = (LocatedToken) yyVals[-2+yyTop];

		Expression type = (Expression)yyVals[-3+yyTop];
		current_block.AddVariable (type, lt.Value, lt.Location);
		yyVal = new Linq.SelectMany (lt, new Linq.Cast (type, (Expression)yyVals[0+yyTop]));
	  }
  break;
case 789:
#line 5422 "cs-parser.jay"
  {
	  	Linq.AQueryClause head = (Linq.AQueryClause)yyVals[-1+yyTop];
		
		if (yyVals[0+yyTop] != null)
			head.Next = (Linq.AQueryClause)yyVals[0+yyTop];
				
		if (yyVals[-2+yyTop] != null) {
			Linq.AQueryClause clause = (Linq.AQueryClause)yyVals[-2+yyTop];
			clause.Tail.Next = head;
			head = clause;
		}
		
		yyVal = head;
	  }
  break;
case 790:
#line 5440 "cs-parser.jay"
  {
		yyVal = new Linq.Select ((Expression)yyVals[0+yyTop], GetLocation (yyVals[-1+yyTop]));
	  }
  break;
case 791:
#line 5444 "cs-parser.jay"
  {
	    yyVal = new Linq.GroupBy ((Expression)yyVals[-2+yyTop], (Expression)yyVals[0+yyTop], GetLocation (yyVals[-3+yyTop]));
	  }
  break;
case 795:
#line 5457 "cs-parser.jay"
  {
		((Linq.AQueryClause)yyVals[-1+yyTop]).Tail.Next = (Linq.AQueryClause)yyVals[0+yyTop];
		yyVal = yyVals[-1+yyTop];
	  }
  break;
case 801:
#line 5473 "cs-parser.jay"
  {
		LocatedToken lt = (LocatedToken) yyVals[-2+yyTop];
		current_block.AddVariable (Linq.ImplicitQueryParameter.ImplicitType.Instance,
			lt.Value, lt.Location);	  
	  	yyVal = new Linq.Let (lt, (Expression)yyVals[0+yyTop], GetLocation (yyVals[-3+yyTop]));
	  }
  break;
case 802:
#line 5483 "cs-parser.jay"
  {
		yyVal = new Linq.Where ((Expression)yyVals[0+yyTop], GetLocation (yyVals[-1+yyTop]));
	  }
  break;
case 803:
#line 5490 "cs-parser.jay"
  {
		Location loc = GetLocation (yyVals[-8+yyTop]);
		LocatedToken lt = (LocatedToken) yyVals[-7+yyTop];
		current_block.AddVariable (Linq.ImplicitQueryParameter.ImplicitType.Instance,
			lt.Value, lt.Location);
		
		if (yyVals[0+yyTop] == null) {
	  		yyVal = new Linq.Join (lt, (Expression)yyVals[-5+yyTop], (Expression)yyVals[-3+yyTop],
				(Expression)yyVals[-1+yyTop], loc);
		} else {
			LocatedToken lt_into = (LocatedToken) yyVals[0+yyTop];
			yyVal = new Linq.GroupJoin (lt, (Expression)yyVals[-5+yyTop], (Expression)yyVals[-3+yyTop],
				(Expression)yyVals[-1+yyTop], lt_into, loc);
		}
	  }
  break;
case 804:
#line 5506 "cs-parser.jay"
  {
		Location loc = GetLocation (yyVals[-9+yyTop]);
		LocatedToken lt = (LocatedToken) yyVals[-7+yyTop];
		current_block.AddVariable ((Expression)yyVals[-8+yyTop], lt.Value, lt.Location);
		
		Linq.Cast cast = new Linq.Cast ((Expression)yyVals[-8+yyTop], (Expression)yyVals[-5+yyTop]);
		if (yyVals[0+yyTop] == null) {
		  	yyVal = new Linq.Join (lt, cast, (Expression)yyVals[-3+yyTop],
				(Expression)yyVals[-1+yyTop], loc);
		} else {
			LocatedToken lt_into = (LocatedToken) yyVals[0+yyTop];
			yyVal = new Linq.GroupJoin (lt, cast, (Expression)yyVals[-3+yyTop],
				(Expression)yyVals[-1+yyTop], lt_into, loc);
		}
	  }
  break;
case 806:
#line 5526 "cs-parser.jay"
  {
	  	yyVal = yyVals[0+yyTop];
	  }
  break;
case 807:
#line 5533 "cs-parser.jay"
  {
		yyVal = yyVals[0+yyTop];
	  }
  break;
case 809:
#line 5541 "cs-parser.jay"
  {
		((Linq.AQueryClause)yyVals[-2+yyTop]).Next = (Linq.AQueryClause)yyVals[0+yyTop];
		yyVal = yyVals[-2+yyTop];
	  }
  break;
case 810:
#line 5549 "cs-parser.jay"
  {
		yyVal = yyVals[0+yyTop];
	  }
  break;
case 811:
#line 5553 "cs-parser.jay"
  {
		((Linq.AQueryClause)yyVals[-2+yyTop]).Tail.Next = (Linq.AQueryClause)yyVals[0+yyTop];
		yyVal = yyVals[-2+yyTop];
	  }
  break;
case 812:
#line 5561 "cs-parser.jay"
  {
		yyVal = new Linq.OrderByAscending ((Expression)yyVals[0+yyTop]);	
	  }
  break;
case 813:
#line 5565 "cs-parser.jay"
  {
		yyVal = new Linq.OrderByAscending ((Expression)yyVals[-1+yyTop]);	
	  }
  break;
case 814:
#line 5569 "cs-parser.jay"
  {
		yyVal = new Linq.OrderByDescending ((Expression)yyVals[-1+yyTop]);	
	  }
  break;
case 815:
#line 5576 "cs-parser.jay"
  {
		yyVal = new Linq.ThenByAscending ((Expression)yyVals[0+yyTop]);	
	  }
  break;
case 816:
#line 5580 "cs-parser.jay"
  {
		yyVal = new Linq.ThenByAscending ((Expression)yyVals[-1+yyTop]);	
	  }
  break;
case 817:
#line 5584 "cs-parser.jay"
  {
		yyVal = new Linq.ThenByDescending ((Expression)yyVals[-1+yyTop]);	
	  }
  break;
case 819:
#line 5593 "cs-parser.jay"
  {
		/* query continuation block is not linked with query block but with block*/
		/* before. This means each query can use same range variable names for*/
		/* different identifiers.*/

		current_block.SetEndLocation (GetLocation (yyVals[-1+yyTop]));
		current_block = current_block.Parent;
		current_block = new Linq.QueryBlock (current_block, GetLocation (yyVals[-1+yyTop]));
		
		LocatedToken lt = (LocatedToken) yyVals[0+yyTop];
		current_block.AddVariable (Linq.ImplicitQueryParameter.ImplicitType.Instance,
			lt.Value, lt.Location);
	  }
  break;
case 820:
#line 5607 "cs-parser.jay"
  {
  		yyVal = new Linq.QueryExpression ((LocatedToken) yyVals[-2+yyTop],
  			(Linq.AQueryClause)yyVals[0+yyTop]);
	  }
  break;
#line default
        }
        yyTop -= yyLen[yyN];
        yyState = yyStates[yyTop];
        int yyM = yyLhs[yyN];
        if (yyState == 0 && yyM == 0) {
          if (debug != null) debug.shift(0, yyFinal);
          yyState = yyFinal;
          if (yyToken < 0) {
            yyToken = yyLex.advance() ? yyLex.token() : 0;
            if (debug != null)
               debug.lex(yyState, yyToken,yyname(yyToken), yyLex.value());
          }
          if (yyToken == 0) {
            if (debug != null) debug.accept(yyVal);
            return yyVal;
          }
          goto continue_yyLoop;
        }
        if (((yyN = yyGindex[yyM]) != 0) && ((yyN += yyState) >= 0)
            && (yyN < yyTable.Length) && (yyCheck[yyN] == yyState))
          yyState = yyTable[yyN];
        else
          yyState = yyDgoto[yyM];
        if (debug != null) debug.shift(yyStates[yyTop], yyState);
	 goto continue_yyLoop;
      continue_yyDiscarded: continue;	// implements the named-loop continue: 'continue yyDiscarded'
      }
    continue_yyLoop: continue;		// implements the named-loop continue: 'continue yyLoop'
    }
  }

   static  short [] yyLhs  = {              -1,
    0,    0,    0,    0,    2,    2,    1,    1,    4,    4,
    4,    8,    8,    5,    9,    9,    6,    6,   10,   10,
   11,   17,   14,   18,   18,   19,   19,   13,   21,   16,
   20,   25,   20,   20,   23,   23,   22,   22,   24,   24,
   26,   26,    7,    7,    7,    7,   27,   27,   27,   27,
   27,    3,   15,   15,   35,   35,   36,   36,   37,   39,
   39,   39,   39,   38,   38,   40,   41,   42,   42,   43,
   43,   43,   44,   44,   45,   45,   46,   46,   46,   48,
   49,   50,   50,   51,   51,   52,   52,   52,   52,   52,
   52,   52,   52,   52,   52,   63,   65,   68,   69,   31,
   31,   71,   67,   70,   70,   72,   72,   73,   73,   73,
   73,   73,   73,   73,   73,   73,   73,   53,   75,   75,
   76,   76,   28,   28,   28,   28,   79,   79,   80,   80,
   78,   78,   81,   81,   81,   82,   82,   82,   82,   82,
   87,   29,   88,   88,   90,   90,   93,   94,   85,   95,
   96,   85,   97,   85,   85,   86,   86,   92,   92,  100,
  101,  100,   99,   99,   99,   99,   99,   99,   99,   99,
   99,  102,  102,  105,  105,  105,  105,  105,  106,  106,
  107,  107,  108,  108,  108,  103,  103,  109,  109,  109,
  104,  110,  112,  113,   54,  111,  111,  111,  111,  111,
  117,  114,  118,  115,  116,  116,  119,  120,  122,  123,
   32,   32,  121,  124,  124,  125,  125,  126,  126,  126,
  126,  126,  126,  126,  126,  126,  126,  131,  134,  132,
  132,  135,  136,  127,  137,  138,  127,  139,  140,  128,
  128,  129,  129,  129,  142,  143,  129,  144,  145,  130,
  148,   57,  147,  147,  150,  146,  146,  149,  149,  149,
  149,  149,  149,  149,  149,  149,  149,  149,  149,  149,
  149,  149,  149,  149,  149,  149,  149,  149,  149,  152,
  151,  153,  151,  151,  151,   58,  154,  154,  158,  156,
  155,  155,  157,  157,  157,  161,  161,  161,  162,   59,
   55,  163,  164,   55,   55,  141,  141,  141,  141,  141,
  141,  167,  165,  165,  165,  168,  166,  166,  166,  170,
  171,   56,  169,  169,  174,   33,  172,  172,  176,  177,
  173,  175,  175,  178,  178,  179,  180,  179,  181,  182,
   34,  183,  183,   12,   12,   12,   91,   91,   62,  184,
  184,  185,  185,  186,  186,  187,  187,   74,   74,   74,
   74,  190,  190,  191,  191,  191,  191,  191,  191,  195,
  195,  188,  188,  188,  188,  188,  188,  188,  196,  196,
  196,  196,  196,  196,  196,  196,  196,  196,  189,  197,
  197,  197,  197,  197,  197,  197,  197,  197,  197,  197,
  197,  197,  197,  197,  197,  197,  197,  197,  198,  198,
  198,  198,  198,  198,  217,  217,  217,  216,  215,  215,
  218,  218,  199,  199,  199,  201,  201,  219,  202,  202,
  202,  202,  202,  223,  223,  224,  224,  225,  225,  226,
  226,  227,  227,  227,  227,  228,  228,  160,  160,  221,
  221,  221,  222,  222,  220,  220,  220,  220,  220,  231,
  203,  203,  230,  230,  204,  205,  205,  205,  206,  207,
  208,  208,  208,  232,  232,  233,  233,  233,  233,  233,
  234,  237,  237,  237,  238,  238,  238,  238,  235,  235,
  239,  239,  239,  239,  193,  192,  240,  240,  241,  241,
  236,  236,   84,   84,  242,  242,  243,  209,  244,  244,
  245,  245,  245,  245,  210,  211,  212,  213,  247,  214,
  246,  246,  249,  248,  200,  250,  250,  250,  250,  253,
  253,  253,  252,  252,  251,  251,  251,  251,  251,  251,
  251,  194,  194,  194,  194,  254,  254,  254,  255,  255,
  255,  256,  256,  257,  258,  258,  258,  258,  258,  259,
  258,  260,  258,  261,  261,  261,  262,  262,  263,  263,
  264,  264,  265,  265,  266,  266,  267,  267,  267,  267,
  268,  268,  268,  268,  268,  268,  268,  268,  268,  268,
  268,  269,  269,  270,  270,  270,  271,  271,  273,  272,
  272,  275,  274,  276,  274,   47,   47,  229,  229,  229,
   77,  278,  279,  280,  281,  282,   30,   61,   61,   60,
   60,   89,   89,  283,  283,  283,  283,  283,  283,  283,
  283,  283,  283,  283,  283,  283,  283,   64,   64,  284,
   66,   66,  285,  285,  286,  287,  287,  288,  288,  288,
  288,  289,   98,  290,  159,  133,  133,  291,  291,  292,
  292,  292,  294,  294,  294,  294,  294,  294,  294,  294,
  294,  294,  294,  294,  294,  308,  308,  308,  296,  309,
  295,  293,  293,  312,  312,  313,  313,  313,  313,  310,
  310,  311,  297,  314,  314,  298,  298,  315,  315,  317,
  316,  318,  319,  319,  320,  320,  323,  321,  322,  322,
  324,  324,  324,  299,  299,  299,  299,  325,  326,  331,
  327,  329,  329,  333,  333,  330,  330,  332,  332,  335,
  334,  334,  328,  336,  328,  300,  300,  300,  300,  300,
  300,  337,  338,  339,  339,  339,  340,  341,  342,  342,
  342,   83,   83,  301,  301,  301,  301,  343,  343,  345,
  345,  347,  344,  346,  346,  348,  302,  303,  349,  306,
  351,  307,  350,  350,  352,  352,  353,  304,  354,  305,
  355,  305,  358,  277,  356,  356,  359,  359,  357,  361,
  361,  360,  360,  363,  363,  364,  364,  364,  364,  364,
  365,  366,  367,  367,  369,  369,  368,  370,  370,  372,
  372,  371,  371,  371,  373,  373,  373,  362,  374,  362,
  };
   static  short [] yyLen = {           2,
    2,    3,    2,    1,    0,    1,    1,    2,    1,    1,
    1,    1,    2,    4,    1,    2,    1,    1,    5,    2,
    3,    0,    6,    0,    1,    0,    1,    1,    0,    3,
    4,    0,    3,    4,    0,    1,    0,    1,    0,    1,
    1,    2,    1,    1,    1,    1,    1,    1,    1,    1,
    1,    1,    0,    1,    1,    2,    5,    4,    2,    1,
    1,    1,    1,    1,    3,    2,    1,    0,    3,    1,
    3,    1,    0,    1,    1,    3,    1,    3,    3,    3,
    3,    0,    1,    1,    2,    1,    1,    1,    1,    1,
    1,    1,    1,    1,    1,    0,    0,    0,    0,   13,
    5,    0,    4,    0,    1,    1,    2,    1,    1,    1,
    1,    1,    1,    1,    1,    1,    1,    6,    1,    3,
    3,    1,    5,    6,    5,    5,    1,    3,    4,    3,
    1,    3,    3,    1,    4,    1,    1,    5,    1,    2,
    0,    3,    0,    1,    1,    1,    0,    0,   10,    0,
    0,   10,    0,   10,    8,    1,    1,    0,    1,    0,
    0,    2,    1,    3,    3,    3,    5,    3,    5,    1,
    1,    1,    3,    4,    6,    3,    4,    6,    0,    1,
    1,    2,    1,    1,    1,    4,    4,    1,    2,    2,
    1,    0,    0,    0,   10,    1,    2,    1,    2,    1,
    0,    5,    0,    5,    1,    1,    0,    0,    0,    0,
   13,    5,    3,    0,    1,    1,    2,    1,    1,    1,
    1,    1,    1,    1,    1,    1,    1,    1,    0,    4,
    1,    0,    0,   11,    0,    0,   11,    0,    0,    9,
    4,    6,    5,    6,    0,    0,   10,    0,    0,   12,
    0,    5,    1,    1,    0,    7,    1,    1,    1,    1,
    1,    1,    1,    1,    1,    1,    1,    1,    1,    1,
    1,    1,    1,    1,    1,    1,    1,    1,    1,    0,
    7,    0,    7,    2,    2,    4,    1,    2,    0,    5,
    1,    1,    5,    5,    2,    0,    1,    1,    0,    8,
    6,    0,    0,   10,    6,    2,    2,    1,    1,    1,
    0,    0,    4,    3,    3,    0,    4,    3,    3,    0,
    0,    8,    5,    7,    0,    8,    0,    2,    0,    0,
    5,    0,    2,    1,    3,    2,    0,    5,    0,    0,
   12,    0,    1,    2,    4,    4,    2,    4,    2,    0,
    3,    0,    3,    1,    3,    1,    2,    2,    2,    1,
    1,    2,    2,    2,    2,    2,    2,    2,    2,    1,
    3,    1,    1,    1,    1,    1,    1,    1,    1,    1,
    1,    1,    1,    1,    1,    1,    1,    1,    3,    1,
    1,    3,    1,    1,    1,    1,    1,    1,    1,    1,
    1,    1,    1,    1,    1,    1,    1,    1,    1,    1,
    1,    1,    1,    1,    1,    1,    1,    1,    1,    1,
    3,    3,    2,    2,    2,    4,    4,    1,    4,    4,
    3,    5,    7,    0,    1,    3,    4,    0,    1,    1,
    3,    3,    1,    3,    2,    1,    1,    0,    1,    1,
    3,    2,    1,    1,    2,    2,    4,    3,    1,    1,
    4,    2,    1,    3,    1,    4,    4,    2,    2,    2,
    1,    1,    1,    6,    3,    7,    4,    3,    2,    3,
    4,    0,    1,    3,    3,    1,    1,    1,    0,    1,
    0,    1,    2,    3,    2,    3,    0,    1,    1,    2,
    0,    1,    2,    4,    1,    3,    0,    5,    1,    1,
    2,    4,    4,    4,    4,    4,    4,    3,    0,    4,
    0,    1,    0,    4,    3,    1,    2,    2,    1,    3,
    3,    3,    1,    4,    1,    2,    2,    2,    2,    2,
    2,    1,    3,    3,    3,    1,    3,    3,    1,    3,
    3,    0,    1,    2,    1,    3,    3,    3,    3,    0,
    4,    0,    4,    1,    3,    3,    1,    3,    1,    3,
    1,    3,    1,    3,    1,    3,    1,    5,    3,    3,
    3,    3,    3,    3,    3,    3,    3,    3,    3,    3,
    3,    1,    3,    3,    2,    1,    0,    1,    0,    2,
    1,    0,    4,    0,    6,    1,    1,    1,    1,    1,
    1,    1,    0,    0,    0,    0,   13,    0,    1,    0,
    1,    1,    2,    1,    1,    1,    1,    1,    1,    1,
    1,    1,    1,    1,    1,    1,    1,    0,    1,    2,
    0,    1,    1,    2,    4,    1,    3,    1,    3,    1,
    1,    0,    4,    0,    4,    0,    1,    1,    2,    1,
    1,    1,    1,    1,    1,    1,    1,    1,    1,    1,
    1,    1,    1,    1,    1,    1,    1,    1,    1,    0,
    4,    2,    2,    2,    2,    2,    2,    2,    2,    2,
    3,    3,    2,    1,    1,    1,    1,    5,    7,    0,
    6,    3,    0,    1,    1,    2,    0,    3,    1,    2,
    3,    1,    1,    1,    1,    1,    1,    5,    7,    0,
   10,    0,    1,    1,    1,    0,    1,    0,    1,    1,
    1,    3,    6,    0,    9,    1,    1,    1,    1,    1,
    1,    2,    2,    3,    4,    3,    3,    3,    4,    3,
    3,    0,    1,    3,    4,    5,    3,    1,    2,    0,
    1,    0,    4,    0,    1,    4,    2,    2,    0,    3,
    0,    7,    1,    3,    3,    1,    0,    6,    0,    6,
    0,    6,    0,    3,    4,    5,    4,    5,    3,    2,
    4,    0,    1,    1,    2,    1,    1,    1,    1,    1,
    4,    2,    9,   10,    0,    2,    2,    1,    3,    1,
    3,    1,    2,    2,    1,    2,    2,    0,    0,    4,
  };
   static  short [] yyDefRed = {            0,
    6,    0,    0,    0,    0,    0,    4,    0,    7,    9,
   10,   11,   17,   18,   44,    0,   43,   45,   46,   47,
   48,   49,   50,   51,    0,   55,  141,    0,   20,    0,
    0,    0,   63,   61,   62,    0,    0,    0,    0,    0,
   64,    0,    1,    0,    8,    3,  629,  635,  627,    0,
  624,  634,  628,  626,  625,  632,  630,  631,  637,  633,
  636,    0,    0,  622,   56,    0,    0,    0,    0,    0,
  344,    0,   21,    0,    0,    0,    0,   59,    0,   66,
    2,    0,  374,  380,  387,  375,    0,  377,    0,    0,
  376,  383,  385,  372,  379,  381,  373,  384,  386,  382,
    0,    0,    0,    0,    0,    0,  360,  361,  378,  623,
  652,  157,  142,  156,   14,    0,    0,    0,    0,    0,
  354,    0,    0,    0,   65,   58,    0,    0,    0,  420,
    0,  414,    0,  465,  419,  507,    0,  388,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,  418,  415,
  416,  417,  412,  413,    0,    0,    0,    0,   70,    0,
    0,   75,   77,  391,  428,    0,    0,  390,  393,  394,
  395,  396,  397,  398,  399,  400,  401,  402,  403,  404,
  405,  406,  407,  408,  409,  410,  411,    0,    0,  607,
  471,  472,  473,  535,    0,  529,  533,    0,    0,    0,
    0,    0,    0,    0,    0,    0,  608,  606,  609,  610,
  783,    0,    0,    0,    0,  363,    0,    0,    0,  131,
    0,    0,  343,  358,  613,    0,    0,    0,  362,    0,
    0,    0,    0,    0,  359,    0,   19,    0,    0,  351,
  345,  346,   57,  468,    0,    0,    0,  145,  146,  523,
  519,  522,  479,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,  528,  536,  537,
  527,  541,  540,  538,  539,  602,    0,    0,    0,  349,
  184,  183,  185,    0,    0,    0,    0,  592,    0,    0,
   69,    0,    0,    0,    0,    0,    0,    0,    0,  469,
  470,    0,  462,  424,    0,    0,    0,  425,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,  562,  560,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
   29,    0,    0,    0,    0,  325,  125,    0,    0,  127,
    0,    0,    0,  347,    0,    0,  126,  150,    0,    0,
    0,  212,    0,  101,    0,  499,    0,    0,  123,    0,
  147,  490,  495,  389,  695,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,  769,    0,    0,    0,  679,    0,  694,  663,    0,
    0,    0,    0,  658,  660,  661,  662,  664,  665,  666,
  667,  668,  669,  670,  671,  672,  673,  674,  675,    0,
    0,    0,    0,    0,  696,  697,  714,  715,  716,  717,
  736,  737,  738,  739,  740,  741,  355,  463,    0,    0,
    0,    0,    0,  488,    0,    0,    0,    0,    0,    0,
  483,  480,    0,    0,    0,    0,  475,    0,  478,    0,
    0,    0,    0,    0,  422,  421,  368,  367,  364,    0,
  366,  365,    0,    0,   80,    0,  392,  595,    0,    0,
    0,  525,    0,   76,   79,   78,  543,  545,  544,    0,
    0,    0,    0,  453,    0,  454,    0,  450,    0,  518,
  530,  531,    0,    0,  532,    0,  581,  582,  583,  584,
  585,  586,  587,  588,  589,  591,  590,    0,  542,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,  580,    0,    0,  579,    0,    0,
    0,    0,    0,  784,  796,    0,    0,  794,  797,  798,
  799,  800,    0,   25,   23,    0,    0,    0,    0,    0,
  124,  753,    0,    0,  139,  136,  133,  137,    0,    0,
    0,  132,    0,    0,  614,  208,   97,  496,  500,    0,
    0,  742,  767,    0,    0,    0,  743,  677,  676,  678,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
  700,    0,    0,  768,    0,    0,  688,    0,    0,    0,
  680,  653,  687,    0,    0,  685,  686,  684,  659,  682,
  683,    0,  689,    0,  693,  467,    0,  466,  516,  191,
    0,    0,    0,  159,    0,    0,    0,  172,  520,    0,
    0,  423,    0,  481,    0,    0,    0,    0,    0,  440,
  443,    0,    0,  477,  503,  505,    0,  515,    0,    0,
    0,    0,    0,  517,  785,    0,  534,  601,  603,    0,
  353,  594,  593,  604,  461,  460,  456,  455,    0,  429,
  452,    0,  426,  430,    0,    0,    0,  427,    0,  563,
  561,    0,  612,  802,    0,    0,    0,    0,    0,    0,
  807,    0,    0,    0,    0,  795,   32,   12,    0,   30,
    0,    0,  329,    0,  130,    0,  128,  135,    0,    0,
  348,    0,    0,    0,    0,    0,    0,    0,    0,    0,
  119,    0,    0,  724,  731,    0,  723,    0,    0,  611,
    0,  746,  744,    0,    0,  747,    0,  748,  757,    0,
    0,    0,  758,  770,    0,    0,    0,  751,  750,    0,
    0,    0,    0,  464,    0,    0,    0,  181,    0,  524,
    0,    0,    0,  485,    0,  484,  445,    0,    0,  436,
    0,    0,    0,    0,    0,    0,  511,    0,  508,    0,
  786,  600,    0,  458,    0,  451,  432,    0,  553,  554,
  578,    0,    0,    0,    0,    0,  813,  814,    0,  790,
    0,    0,  789,    0,   13,   15,    0,    0,  339,    0,
  326,  129,    0,  151,  153,    0,    0,  639,    0,    0,
  155,  148,    0,    0,    0,    0,    0,  773,  720,    0,
    0,    0,  745,    0,  777,    0,    0,  762,  765,  755,
    0,  759,  781,  779,    0,  749,  681,  494,  189,  190,
    0,  182,    0,    0,    0,  165,  173,  166,  168,    0,
  444,  446,  447,  442,  437,  441,    0,  474,  435,  506,
  504,    0,    0,    0,  605,  457,    0,  787,    0,    0,
    0,  801,    0,    0,  810,    0,  819,   33,   16,   41,
    0,    0,    0,    0,  330,    0,  334,    0,    0,    0,
    0,    0,    0,  615,    0,  643,  209,   98,    0,  121,
  120,    0,    0,  771,    0,    0,  732,    0,    0,    0,
    0,    0,    0,    0,  756,    0,    0,  718,  177,    0,
  187,  186,    0,    0,  502,  476,  512,  514,  513,  433,
  788,    0,    0,  816,  817,    0,  791,    0,   34,   31,
   42,  340,    0,    0,    0,  333,  138,  152,  154,    0,
    0,    0,  644,    0,    0,  149,    0,  775,    0,  774,
  727,    0,  733,    0,    0,  778,    0,  701,  761,    0,
  763,  782,  780,    0,    0,  169,  167,    0,    0,  811,
  820,    0,    0,  331,  335,    0,    0,    0,  616,    0,
  210,  102,   99,  719,  772,    0,  734,  699,  713,    0,
  712,    0,    0,  705,    0,  709,  766,  175,  178,    0,
    0,  341,    0,  650,    0,  651,    0,    0,  646,    0,
   95,   87,   88,    0,    0,   84,   86,   89,   90,   91,
   92,   93,   94,    0,    0,  223,  224,  226,  225,  222,
  227,    0,    0,  216,  218,  219,  220,  221,    0,    0,
    0,    0,    0,  729,    0,    0,  702,  706,    0,  710,
    0,    0,  338,    0,    0,    0,    0,    0,    0,   81,
   85,  617,    0,    0,  213,  217,  211,  116,  109,  110,
  108,  111,  112,  113,  114,  115,  117,    0,    0,  106,
  100,    0,  735,  711,    0,    0,  803,    0,  649,  647,
    0,    0,    0,    0,    0,    0,  251,  257,    0,    0,
    0,  299,  619,    0,    0,    0,  103,  107,  721,  806,
  804,    0,    0,  285,    0,  284,    0,    0,    0,    0,
    0,    0,  654,  292,  286,  291,    0,  288,  320,    0,
    0,    0,  241,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,  263,  262,  259,  264,  265,  258,  277,
  276,  269,  270,  266,  268,  267,  271,  260,  261,  272,
  273,  279,  278,  274,  275,    0,    0,    0,    0,  254,
  253,  252,    0,  295,    0,    0,    0,    0,  243,    0,
    0,    0,  238,    0,  118,  305,  302,  301,  282,  280,
    0,  255,    0,    0,    0,  193,    0,    0,    0,  200,
    0,  321,    0,    0,    0,  245,  242,  244,    0,    0,
    0,    0,    0,    0,    0,  290,    0,  323,  162,    0,
    0,  655,    0,    0,    0,    0,  197,  199,    0,    0,
  235,    0,  239,  232,  310,    0,  303,    0,    0,    0,
    0,    0,    0,  194,  293,  294,  201,  203,  322,  300,
  246,    0,  248,    0,    0,    0,    0,    0,    0,    0,
  306,    0,  307,  283,  281,  256,  324,    0,    0,    0,
    0,  236,    0,  240,  233,  314,    0,  318,    0,  315,
  319,  304,    0,    0,  195,  206,  205,  202,  204,  247,
    0,  249,    0,  313,  317,  229,  231,  237,    0,  234,
    0,  250,    0,  230,
  };
  protected static  short [] yyDgoto  = {             5,
    6,    7,    8,    9,   10,   11,   12,  709,  817,   13,
   14,  103,   32,   15,  631,  342,  212,  555,   77,  710,
  553,  711,  818,  901,  814,  902,   17,   18,   19,   20,
   21,   22,   23,   24,  632,   26,   38,   39,   40,   41,
   42,   80,  158,  159,  160,  161,  398,  163, 1009, 1044,
 1045, 1046, 1047, 1048, 1049, 1050, 1051, 1052, 1053,   62,
  104,  164,  365,  827,  726,  914, 1013,  975, 1071, 1108,
 1070, 1109, 1110,  119,  730,  731,  741,  230,  349,  350,
  220,  567,  563,  568,   27,  113,   66,    0,   63,  250,
  232,  633,  581,  919,  573,  909,  910,  399,  634, 1223,
 1224,  635,  636,  637,  638,  766,  767,  286,  769, 1199,
 1232, 1251, 1298, 1233, 1234, 1318, 1299, 1300,  363,  725,
 1011,  974, 1069, 1062, 1063, 1064, 1065, 1066, 1067, 1068,
 1094, 1328,  400, 1331, 1285, 1323, 1282, 1321, 1241, 1284,
 1267, 1260, 1301, 1303, 1329, 1127, 1202, 1152, 1196, 1247,
 1128, 1245, 1244, 1129, 1155, 1130, 1158, 1148, 1156,  495,
 1089, 1160, 1243, 1289, 1268, 1269, 1307, 1309, 1131, 1207,
 1256,  346,  714,  558,  905,  820,  964,  906,  907, 1003,
  903, 1002,  615,   71,  280,  120,  121,  165,  107,  108,
  265,  233,  234,  166,  912,  109,  167,  168,  169,  170,
  171,  172,  173,  174,  175,  176,  177,  178,  179,  180,
  181,  182,  183,  184,  185,  186,  187,  188,  189,  496,
  497,  498,  878,  457,  648,  649,  650,  874,  190,  439,
  677,  191,  192,  193,  373,  946,  450,  451,  616,  367,
  368,  657,  258,  662,  663,  251,  443,  252,  442,  194,
  195,  196,  197,  198,  199,  800,  690,  200,  524,  523,
  201,  202,  203,  204,  205,  206,  207,  208,  287,  288,
  289,  669,  670,  209,  474,  793,  210,  694,  361,  724,
  972, 1054,   64,  828,  915,  916, 1038, 1039,  236, 1203,
  403,  404,  588,  589,  590,  408,  409,  410,  411,  412,
  413,  414,  415,  416,  417,  418,  419,  591,  761,  420,
  421,  422,  423,  424,  425,  426,  747,  988, 1022, 1023,
 1024, 1025, 1079, 1026,  427,  428,  429,  430,  736,  982,
  926, 1072,  737,  738, 1074, 1075,  431,  432,  433,  434,
  435,  436,  752,  753,  990,  848,  934,  849,  605,  837,
  979,  838,  931,  937,  936,  211,  544,  340,  545,  546,
  705,  813,  547,  548,  549,  550,  551,  552, 1117,  701,
  702,  894,  895,  958,
  };
  protected static  short [] yySindex = {           26,
    0, -366, -205, -210,    0,   26,    0,   43,    0,    0,
    0,    0,    0,    0,    0,11338,    0,    0,    0,    0,
    0,    0,    0,    0,   64,    0,    0, -190,    0,  549,
   87,   -6,    0,    0,    0,  320,   87,   35,  142,  176,
    0,  183,    0,   43,    0,    0,    0,    0,    0,   35,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0, 8874,11488,    0,    0,   92,  207,   35,  664,  152,
    0,  178,    0,  320,  142,   35,  301,    0, 6300,    0,
    0,   87,    0,    0,    0,    0, 5978,    0,  256, 5978,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
 -281,  399,  199,  252,  208,  376,    0,    0,    0,    0,
    0,    0,    0,    0,    0, -245,  409,  664,  384,  -72,
    0,  435,  435,  455,    0,    0, -195,  470,    3,    0,
  675,    0,  480,    0,    0,    0,  506,    0, 8950, 6383,
 6788, 6788, 6788, 6788, 6788, 6788, 6788, 6788,    0,    0,
    0,    0,    0,    0,  327, 4519, 5978,  524,    0,  546,
  565,    0,    0,    0,    0,  501,  683,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,   21,  536,    0,
    0,    0,    0,    0, 1072,    0,    0,  614,  342, -197,
  377,  591,  579,  613,  595, -280,    0,    0,    0,    0,
    0,  662,   19,  651, -202,    0,  419,  680,  672,    0,
    3,  594,    0,    0,    0,  804,  832,  686,    0,  710,
  294,    3,  751,  376,    0, 1908,    0,  384,  664,    0,
    0,    0,    0,    0, 6383,  665, 6383,    0,    0,    0,
    0,    0,    0, 2235,  -87,  733, 5978,  764, 6383,   -7,
   75,  336,  -61,  376,  244,  780,  396,    0,    0,    0,
    0,    0,    0,    0,    0,    0, 6383,  664,  688,    0,
    0,    0,    0,  320,  118, 5978,  768,    0,  773,  386,
    0, 6300, 6300, 6788, 6788, 6788, 5407, 5002,  744,    0,
    0,  756,    0,    0, 7001,  745, 7076,    0,  761, 6383,
 6383, 6383, 6383, 6383, 6383, 6383, 6383, 6383, 6383, 6383,
 6788, 6788, 6788, 6788,    0,    0, 6788, 6788, 6788, 6788,
 6788, 6788, 6788, 6788, 6788, 6788, 5490, 6788, 6383,  956,
    0,  839,  843,    3, 5978,    0,    0,  853,  747,    0,
 6383, 5085,  664,    0,  788,  792,    0,    0,  569,    3,
  806,    0,  806,    0,  806,    0,  893,  897,    0,    3,
    0,    0,    0,    0,    0,  913,  452, 7206,  921, 1908,
    3,    3,    3, -191,  922,  933, 6383,  952, 6383,  936,
  644,    0,    3,  937,  955,    0,   15,    0,    0,  960,
  632,  873, 1908,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,  953,
  954,  792,  659,  958,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,  696,  435,
  959, -224,  936,    0, 6383,  578,  683,    0,   24, -239,
    0,    0, 5651, 5407, 5002, -241,    0, 4597,    0,  394,
 8974,  961, 6383, 1034,    0,    0,    0,    0,    0, 6788,
    0,    0, 6871,  936,    0,   -3,    0,    0,  122, 4519,
  988,    0,  565,    0,    0,    0,    0,    0,    0,  709,
 6383, 6383,  969,    0,  982,    0, -175,    0,  435,    0,
    0,    0, 4680,  683,    0,  435,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,  501,    0,  501,
  614,  614, 5978, 5978,  342,  342,  342,  342, -197, -197,
  377,  591,  579,  613,    0,  966,  595,    0, 6383, 9018,
 9050,  904, 6383,    0,    0,  682,  956,    0,    0,    0,
    0,    0, -141,    0,    0, -224,  384,  989, 5734,  906,
    0,    0,  990, 5978,    0,    0,    0,    0,  443,  975,
 -244,    0, -224, -224,    0,    0,    0,    0,    0, -224,
 -224,    0,    0,  967,  434,  910,    0,    0,    0,    0,
 1019, 5978, 1991, 5978, 6383,  991,  992, 6383, 6383,  993,
    0,  994, -132,    0,  936, 6544,    0, 6383,  996, 5895,
    0,    0,    0,    0,  751,    0,    0,    0,    0,    0,
    0, 1000,    0,  792,    0,    0, 6383,    0,    0,    0,
  538,   64, 1003,    0, 1006, 1008, 1017,    0,    0, -189,
 5085,    0, 7277,    0, 2235, 6056,  343, 1009, 1018,    0,
    0,  722, 1014,    0,    0,    0, 1021,    0,   45,  412,
  384, 1024, 1025,    0,    0, 6383,    0,    0,    0, 6383,
    0,    0,    0,    0,    0,    0,    0,    0, 4841,    0,
    0, 5002,    0,    0,  -61, 1028, -164,    0, -155,    0,
    0, 6383,    0,    0,   27,  130,   44,  171,  983,  795,
    0, 1023, 6383, 6383, 1044,    0,    0,    0, 1121,    0,
 1073, 1039,    0,  839,    0, 1049,    0,    0,  388,    0,
    0, 1042, 1048, 1046, 1046, 1046, 1053, 1054, 1050, 1056,
    0, 1061,  172,    0,    0, 1058,    0, 1064, -223,    0,
 1059,    0,    0, 1074, 1075,    0, 6383,    0,    0,    3,
  936,  612,    0,    0, 1078, 1079, 1080,    0,    0, 1069,
 1908, 1062, 1000,    0,  538, 5978,  566,    0, 5978,    0,
  309, 1208, 1210,    0, 4680,    0,    0, -238, 6139,    0,
 5246,  751, 1105, 5085, 1109, 1022,    0, 1032,    0, 1033,
    0,    0,  936,    0, -109,    0,    0, 5002,    0,    0,
    0, 6383, 1188, 6383, 1192, 6383,    0,    0, 6383,    0,
 1137, 1045,    0, 1132,    0,    0, 1073,   64,    0,   64,
    0,    0, 5407,    0,    0, 5978, 1159,    0, 1159, 1159,
    0,    0, 6383,  910, 6383, 1123,  753,    0,    0, 2152,
 6383, 1209,    0, 1908,    0, 1136, 5978,    0,    0,    0,
  936,    0,    0,    0, 1908,    0,    0,    0,    0,    0,
 -194,    0, -162, 1135, 1138,    0,    0,    0,    0, -189,
    0,    0,    0,    0,    0,    0,  733,    0,    0,    0,
    0,  -19,   57, 1055,    0,    0, 1139,    0, 6383, 1163,
 6383,    0,  825, 1142,    0, 6383,    0,    0,    0,    0,
 -120,   64, 1159, 1067,    0, 1147,    0, 1153, 1159, 1159,
  384, 1150, 1081,    0, 1159,    0,    0,    0, 1159,    0,
    0, 1154, 6383,    0, 1082, 6383,    0, 1158, 6383, 1251,
 1908, 1169,  192,  936,    0, 1908, 1908,    0,    0,   33,
    0,    0, 1280, 1281,    0,    0,    0,    0,    0,    0,
    0, 6383, 1190,    0,    0, 6383,    0,  956,    0,    0,
    0,    0,    0, 1174,   64,    0,    0,    0,    0, 5978,
 1173, 1177,    0, 1182, 1189,    0, 1179,    0, 1908,    0,
    0, 1187,    0, 1186, 1908,    0, -206,    0,    0, 1197,
    0,    0,    0, 1194, 6383,    0,    0, 1218, 6383,    0,
    0, 1198, 1195,    0,    0,  384, 4924,   64,    0,   64,
    0,    0,    0,    0,    0, 2152,    0,    0,    0, 6383,
    0, 1207, -206,    0, -206,    0,    0,    0,    0, 6383,
 1225,    0, 6383,    0, 1211,    0,  384, 1204,    0,11518,
    0,    0,    0, 1221,   64,    0,    0,    0,    0,    0,
    0,    0,    0,  839,11488,    0,    0,    0,    0,    0,
    0, 1223,   64,    0,    0,    0,    0,    0,  839,   64,
  839, 1222, 1064,    0, 1908, 1220,    0,    0, 1908,    0,
 1219, 6383,    0, 1226, 4924,    0,    0, 8487, 1216,    0,
    0,    0,  317, 5329,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0, 1231,   64,    0,
    0, 1908,    0,    0, 1908, 1151,    0, 1219,    0,    0,
 5978, 5978, -148,   39,  320,  557,    0,    0,  259, 1230,
 1239,    0,    0, 5978,   37, -235,    0,    0,    0,    0,
    0,  196,  198,    0, 5978,    0, 5978,    3, 2003, 1238,
 1235,  372,    0,    0,    0,    0,  173,    0,    0, 1160,
 -157,  140,    0, 1241,  387,  140,  775,  548,  -39,  790,
  -28,  -28, -224,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    3,    0, -242, 1245,    0,
    0,    0, 1908,    0,    3,    3,  -90, 1248,    0,  584,
 -224,    0,    0, -224,    0,    0,    0,    0,    0,    0,
 1244,    0, 1252, -224, 1255,    0, 1249, 5002, 5002,    0,
11488,    0,  -90,  -90, 1254,    0,    0,    0, 1257, 1261,
  -90, 1263,  -69,    0,    0,    0,    0,    0,    0, -224,
  -90,    0, 1264, 1266,  837, 1259,    0,    0,  936,  -69,
    0, 1272,    0,    0,    0,  495,    0,   64,   64, 1270,
 1273, 1275, 1274,    0,    0,    0,    0,    0,    0,    0,
    0, 1159,    0, 1265, 1159, 1392, 1397,11308, 1290,11368,
    0,11398,    0,    0,    0,    0,    0, 1291,  588,  588,
 1293,    0,  -90,    0,    0,    0,  936,    0,  936,    0,
    0,    0,11428,11458,    0,    0,    0,    0,    0,    0,
  599,    0,  599,    0,    0,    0,    0,    0, 1298,    0,
 1908,    0, 1299,    0,
  };
  protected static  short [] yyRindex = {         1430,
    0,    0,    0,    0,    0, 1430,    0, 1665,    0,    0,
    0,    0,    0,    0,    0, 8640,    0,    0,    0,    0,
    0,    0,    0,    0, 1349,    0,    0,    0,    0, -114,
 1296,    0,    0,    0,    0,  917,  633,    0, 1303,    0,
    0,  727,    0, 1665,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,  311, 8387,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0, 2657, 1303, 1307,    0,    0, 1306,    0,
    0, 1314,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
11178,  312, 2791,    0,    0, 2791,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0, 2905,    0,  479,    0,
    0, 2523, 2523,    0,    0,    0,    0,    0, 1315,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,10975, 1313,    0,    0,    0, 1316,
 1321,    0,    0,    0,    0, 9352, 9179,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0, 9281,    0,    0, 9627, 9845,10210,
10423,10541,10659,10777, 1106,  926,    0,    0,    0,    0,
    0,    0,    0, 1329,    0,    0,   80,    0,    0,    0,
    0,    0,    0,    0,    0, 1256, 1267, 1328,    0,    0,
    0,    0, 2348, 2791,    0, 1332,    0,  563,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,  298,    0,    0,    0,    0,    0,  112,
    0, 3019,    0,  552,    0,10932, 3019,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,  120,    0,    0, 1330,    0,    0,    0,
    0,    0,    0,    0,    0,    0, 1328, 1336,    0,    0,
    0,    0,    0,    0,    0, 3137,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,  758,
    0, 1246,  -17,    0,    0,    0,    0,    0,    0,    0,
 1340,    0,    0,    0,    0,    0,    0,    0,  154,    0,
    0,    0,    0,    0,    0,    0,    0, 1342,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0, 1338,    0, 1338,    0,
    0,    0,    0,   79,    0,    0, 8154,    0,    0,    0,
  201, 8220, 1348,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0, -278,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0, 3255,
    0, 8800,    0,    0,    0,  378,    0,  465,    0,    0,
    0,    0, 1351, 1328, 1336,  -32,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,  458, 6627,    0,    0,    0,    0,    0,    0,
    0,    0, 1345,    0,    0,    0,    0,    0,    0,    0,
    0,    0,  -82,    0,    0,    0, 1352,    0, 3255,    0,
    0,    0,    0, 3713,    0, 3255,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0, 9454,    0, 9525,
 9698, 9774,    0,    0, 9921, 9992,10068,10139,10281,10352,
10470,10588,10706,10824,    0,    0,10895,    0,    0,    0,
    0,    0,    0,    0,    0,    0,  835,    0,    0,    0,
    0,    0, 3887,    0,    0, 8800, 1353,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,  -18,
  658,    0, 8800, 8800,    0,    0,    0,    0,    0, 8800,
 8800,    0,    0,  201, 1276,    0,    0,    0,    0,    0,
    0,    0, 1347,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,  -29,    0,    0,    0,    0,    0,    0,
    0,  730,    0,    0,    0,    0,    0,    0,    0,    0,
 9094, 7466,    0,    0,  833,  860,  864,    0,    0,    0,
    0,    0,    0,    0,    0,    0,11048,    0, 1357,    0,
    0,    0,    0,    0,    0,    0, 1358,    0,  535,  471,
 1356,    0, 1360,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0, 1359, -156,    0,    0,  822,    0,
    0,    0,    0,    0,  112,    0,  112,    0,    0,  562,
    0,  719,    0,    0, 2862,    0,    0,    0, 3978,    0,
 4079,    0,    0, 1102,    0,    0,    0,    0,  649,   91,
    0,    0,    0, -228, -228, -228,    0,    0,  882, 1361,
    0,    0,    0,    0,    0,    0,    0, 1363,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0, 1373,
    0, 1520,    0,    0,    0,    0,    0,    0,    0,    0,
    0, 1297,  731,    0, 9138,    0, 9162,    0,    0,    0,
 8906,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0, 3373, 3477, 1374,    0,    0,    0,    0,    0,    0,
    0,    0, 6627,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0, 4170, 4271,    0, -267,
    0,    0, 1328,    0,    0,    0, 1380,    0, 1380, 1380,
    0,    0,    0,    0,    0,  888,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
  669,    0,    0,  945,  946,    0,    0,    0,    0, 1359,
    0,    0,    0,    0,    0,    0, 3595,    0,    0,    0,
    0,  535,  535,    0,    0,    0, -135,    0,    0,    0,
    0,    0, 1247,  787,    0,    0,    0,    0,    0,    0,
    0, 4355, 1377,    0,    0, 1358,    0,    0,  606,  606,
  284,  254,    0,    0,  618,    0,    0,    0,  606,    0,
    0,    0,    0,    0,    0, 1378,    0,    0,    0, 1714,
    0,    0, 1386,    0,    0,    0,    0,    0,    0,  687,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,  758,    0,    0,
    0,    0,  325,    0, -136,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0, 1381,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,  358,    0, 7550,    0, 8070,
    0,    0,    0,    0,    0, 1387,    0,    0,    0,    0,
    0,    0, 1393,    0, 4436,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,  310,  368,    0, 8455,
    0,    0,    0,    0, 7645,    0,    0,    0,    0,    0,
    0,    0,    0, 1102, 8566,    0,    0,    0,    0,    0,
    0,    0, 8149,    0,    0,    0,    0,    0, 1102, 7729,
 1102,    0, 1388,    0,    0,    0,    0,    0,    0,    0,
  867,    0,    0,    0,    0, 7908, 7991,  311,    0,    0,
    0,    0, 1603,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0, 7824,    0,
    0,    0,    0,    0, -214,    0,    0,  867,    0,    0,
    0,    0,    0,    0, 7358,    0,    0,    0,    0,  620,
    0,    0,    0,    0, -173,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
 1395,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,  205,    0,    0,   -1,    0,    0,
    0,    0, 8800,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0, 8680,    0,    0,    0,
    0,    0, 1332,    0,    0,    0,  354,    0,    0,    0,
 8800, 8680,    0, 8800,    0,    0,    0,    0,    0,    0,
    0,    0,    0, 8906,    0,    0,    0, 1336, 1336,    0,
  957,    0, 3018,11213,    0,    0,    0,    0,    0,    0,
  354,    0, 7396, 8755, 8755,    0, 8755,    0,    0, 1804,
  354,    0,    0,    0,    0,    0,    0,    0,    0, 7396,
    0,    0,    0,    0,    0,    0,    0,11248,11278,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,  606,    0,    0,  606, 1396, 1399,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,  354,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
 1332,    0,    0,    0,
  };
  protected static  short [] yyGindex = {            0,
    0,  691, 1760, 1764, -497, -618, -701,    0,    0,    0,
    0,    9,    0,    0,    1,    0,    0, -694,  -67,    0,
    0,    0,    0,    0,    0,    0, -712, -667, -643, -907,
 -904, -749, -675, -624,   70,   -2,    0, 1733,    0, 1696,
    0,    0,    0,    0,    0, 1482,  -45, 1484,    0,    0,
    0,  734, -630, -743, -568, -511, -498, -456, -448, -980,
    0, -124,    0,  490,    0, -790,    0,    0,    0,    0,
    0,    0,  666,  555,  636,  947, -776,  -96,    0, 1224,
 1424, -420,  930, -234,    0,    0,    0,    0, -102,  -80,
  -33, -532,    0,    0,    0,    0,    0,  -62,  558, -520,
    0,    0, 1031, 1036, 1043,    0,    0, -557, 1038,    0,
 -556,    0,    0,    0,    0,  483,    0,    0,    0,    0,
    0,    0,    0,    0,    0,  741,    0,    0,    0,    0,
    0,  467,-1096,    0,    0,    0,    0,    0,    0,    0,
  545,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0, -437,
    0,    0,    0,    0,  550,  547,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,  852,    0,
    0,    0,  -74, -108, -174,   41, 1579,  -60,    0,    0,
    0, 1557,  -98,  -88,    0,    0, -217,    0,    0, 1521,
 -237,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0, -229,    0, -458,
 -459, -571,    0,  328,    0,    0, 1047,    0, -425, -262,
 1334,    0,    0,    0, 1051,    0,    0, 1184, -336,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0, 1525,
 1012, 1524,    0,  925,  801,    0, 1308,  932,    0,    0,
 1501, 1502, 1503, 1505, 1504,    0,    0,    0,    0, 1364,
    0, 1066,    0,    0,    0,    0,    0, -572,    0,    0,
    0,    0,  -63,    0,    0,  942,    0,  777,    0,    0,
  781, -387, -230, -226, -225,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0, -791,    0,  -54,
    0, 1485,    0, -562,    0,    0,    0,    0,    0,    0,
  841,    0,    0,  840,    0,    0,    0,    0,    0,    0,
    0,    0,    0,  851,    0,    0,    0,    0,    0,    0,
    0,    0,    0, 1116,    0,    0,    0,    0,    0,    0,
    0,  948,    0,    0,    0,    0,  912,    0,    0,    0,
    0,    0,    0, 1325,    0,    0,    0,    0,  757,    0,
    0,    0,  924,    0,
  };
  protected static  short [] yyTable = {           110,
   16,  106,  231,  114,  219,  405,   16,  124,  106,  406,
  407,   31,   37,  241,  242,  619,  448,  653,  402,  821,
 1163,  459,   65,  712,  449,  744,  106,  651,  224,  106,
  735,  235,  256,  162,  490,  757,  447,  656,  917,  918,
  722,  723,  354,  687,  686,   33,   37,  727,  728, 1019,
   29,  266,  930,  347,  708,  708,  920,  106,   82, 1088,
  244,  939, 1020,  938,  325,  618,  465,  221,  303,   25,
  106,  841,   34,  768, 1093,   25,  116,  595,  106,  264,
  681,   28,  388, 1225,   37,  596,  624,  342,  344,  504,
 1164,  681,  816,  941,  263,  106,  106,  332, 1209,  454,
  799,  326, 1056,  216,   35, 1057, 1227, 1144,  337,  218,
  796,  638,  962,  218,  707,   65,  900,  630,  968,  969,
  451,  351,  458,  749,   72,  644,  871,  237,  976,  338,
  228,  645,  627,  352,  372,  638,  959,  750,  118,  986,
  358,    4,  228,    2,  992,  993,  681,  223,  449,  229,
  708,  371,  388,  339,  751, 1056,  456, 1145, 1057,  374,
  585,  229,  402,  228,  468, 1230,  217,  110,  452,  491,
  245,  228,  405,  459,  246,  401,  406,  407,  106,  466,
   53,  327,  229,  328,  354,  402, 1265, 1015,  360,  469,
  229,  652,  388, 1018,  465,  682,  106,  370,  899,  438,
  961,  441,  329,  228,  330,  570,  798,  859,  228,  862,
  228,  815, 1165,  462,  454,  388, 1216,  106, 1029,  795,
  774,  654,  229,  342,  842,  106,  887,  229,   27,  229,
  218,  475,  518,  520, 1333,  451,  575,   36,  576,  218,
  577,  708,   30, 1076,  960,  348,  484,  485,  618, 1021,
 1255,  438,  494,  940,  350,  350,  597,   67,  350,  886,
 1058,  682,  922,  556,  507,  508,  509,  510,  511,  512,
  513,  514,  515,  516,  517,    4,  453,  927,  454,  574,
  455,  609,    1, 1113,  106,  942,  459,  463,  459,  580,
 1210,  536,  106,  538, 1146, 1041,    4,  229,  239,    1,
  592,  593,  594,  303,  228,  562,  566,  466,  118,  240,
    2,   53,  606, 1058,  583,  687,  686,  584,  476,  401,
 1139,  802,  352,  467, 1217,  622, 1102,  603,  604,  610,
   72,  628, 1041,  342, 1059,  342,  343,  228,  804,  248,
 1042,  562,  401,  562, 1147,  350,  352,  118,  303,  352,
  352,  350,  342,  981,  352,  651,  229, 1098,  276, 1092,
   69,    3,  350,  880, 1043, 1102,   73,  239,  350,  134,
  248,  134,   69,  857, 1097,  402, 1111, 1042,  671, 1061,
  639,  374,   70,  778,  228, 1060,  611, 1059,  402,  304,
  683,    4,  304,  569,  278,  721, 1098,  688,  994,  640,
  106, 1043, 1099,  229,  279,  303,   69,  448,  438,  494,
  995,  668,  566,  449,  266,  449,   70,  665,  492,  106,
  249,  216,  118,   69,   69,  504, 1100,  447, 1204,    4,
  352,  352, 1061,   70,  786,  947,   69, 1205, 1060, 1101,
  228, 1099,  264,  350,  388,  676,  676,  352,  388,  350,
  134,  249,  134,  735,  351,  111,   72,  685,  353,  229,
  351,  242,  106,  106,  112, 1100,  343,  388,   47,  660,
  305,  306,  307,  308,  642,  643,  308,  350, 1101,  106,
  106,  350,   74,  228,   74,  350,  303,  228,  596,  350,
  596, 1302,   48,  693, 1305,  228,  350,  700, 1206,  787,
  350, 1103,  229,  106,  350,   49,  229,  248,  350,   72,
   51,  948,   76,  716,  229,   52,  762,   53,   54,   55,
   56,  352,  464,  350,  225,   57,  388,  763,  352,   58,
  405,  106,  401,  106,  406,  407,  228,  228,  734,  353,
 1103,   59,  754,  402,   60,  401,   61,   78,  226,  740,
   79,  756,  693,  745,   47,  229,  229,  228, 1104,  350,
  755,  228,  693,  228,  760,  478,  342,  350,   72,  672,
  428, 1105,  350,  228,  350,  227,  229,  803,   48,  115,
  229,  764,  229,  618,  619,  224,  468,  223,  249,  785,
 1121,   49,  229,  640,   87,  566,   51, 1104,   89,  122,
  438,   52,  352,   53,   54,   55,   56,  618,  619,  228,
 1105,   57,  470, 1106,   53,   58,  105,  640,  805,  836,
  791, 1107, 1153,  370,  792,  123,  402,   59,  471,   65,
   60, 1154,   61,  494,  618,  619,  494,  402,   53,  989,
 1221,  213,  945,  729,  215, 1168,  801,  370,  491,  648,
  864,   53, 1106,  350,  370,  217,   53,  810,  811, 1133,
 1107,   53,  482,   53,   53,   53,   53,  126,  482,  847,
  276,   53,  238,  648,    4,   53, 1257, 1258, 1239,  276,
  648, 1242,  648,  372, 1263,  255,  276,   53,  850,  336,
   53, 1240,   53,  261, 1274,  336,   43,  371,   46,   69,
  401,  846,  337,  214,  277,  106,  278,  645,  106,   70,
  285,  290,   53,  402,   53,  278,  279, 1273,  402,  402,
  779,  371,  278, 1270, 1271,  279, 1272,  619,  371,  870,
  668,  645,  279,  872,   81,  111,  222,  323,  566,  324,
  645,  359,  486,  352, 1200,  352, 1322,  352,  486,  228,
 1213,  228,  494,  823,  482,   47,  888, 1286,  890,  228,
  892,  402,  658,  893,  223,  106,   69,  402,  229,  352,
  229,  352,  229,  241,  242,  278,   70,  908,  229,   48,
  331,  788,  332,  401,  351,  279,  106,  740,  935,  693,
 1253, 1254,   49,  216,  401,  928,  352,   51,  353,  297,
  223,  298,   52,  299,   53,   54,   55,   56,   70,  352,
 1287,  460,   57,  239,   69,  111,   58,   47,   16,  247,
  904,  243,  614,  369,  720,  300,  369,  301,   59,  487,
  395,   60,  395,   61,  395,  487,  342,  247,  966,  342,
  479,   48,  369,  951,  281,  953,  765,  257,  405,  356,
  957,  282,  406,  407,   49,  342,  395,  402,  395,   51,
  356,  402, 1149,  283,   52,  302,   53,   54,   55,   56,
  401,  991,  281,  259,   57,  401,  401,  978,   58,  282,
  693,  750, 1150,  984,  405,  294,  295,  296,  406,  407,
   59,  283,  291,   60,  402,   61,  395,  402,  851,  557,
  350,  812,   16,  350,  350,  309,  998,  812,  812,  106,
  893,  812,  812,  351,  812,  812,  292,  342,  401,  350,
  342,  428,  228,  350,  401,  352,   68,   69,   69,   83,
  253,   84,  812,  357,   85,  293,  342,   70,   70,   86,
   83,  229,   84,   88,  357,   85,  106, 1236,  353,  740,
   86,  111,   91, 1031,   88,  641, 1237,  278,   70,   92,
 1316, 1238, 1326,   91,   93,  904,  334,  279,   94,  641,
   92, 1327,  405,  333,  740,   93,  406,  407,  641,   94,
   95,  642,   96,  287, 1081,  402,   97, 1083,  321,  322,
  642,   95,  287,   96,   98,   99,  335,   97,  100,   67,
   67,  117,  336,   67,  217,   98,   99,  111, 1040,  100,
 1055,  259,  117,  140,  401,  661,  613,  140,  401,  140,
  614,  140,  345,  231,  106,  341,  134,  106,  134,    4,
  134,  703,  704,  106,  285,  176, 1118,  176,  254,  176,
  228,  359,  356,  623,  357, 1040, 1170,  614,  297,  355,
  298,  401,  299,  174,  401,  174,  366,  174,  808,  362,
  106,  106,  626, 1055,  808,  808,  627, 1173,  808,  808,
 1040,  808,  808,  106,  300,  675,  301,  689,  689,  627,
  356, 1211,  369,  552,  106, 1214,  106,  364,  782, 1201,
 1219, 1220,  627,   68,  696,  698,  458,   68,  690,  691,
  405,   24,  690,  691,  406,  407,  873,  792,  792, 1040,
  879,   74,  440,  402,  302, 1222,  228,  560,  719,  561,
  552,  924,   74,  925, 1228, 1229,  809,  525,  526,  527,
  528,  461,  809,  809, 1151,  477,  809,  809,  480,  809,
  809,  481,  401, 1162, 1166,  834,  733, 1215,  739,  807,
  808, 1169,  268,  269,  270,  271,  272,  273,  274,  275,
  356,  552, 1218, 1288,  473,  295,  296,  552,  552,  552,
  552,  552,  552,  552,  552,  552,  552,  552,  552,  954,
  955,  577,  494,  494,  793,  793,  552, 1313,  552, 1314,
  552,  499,  552,  552,  552, 1277, 1280, 1278,  157,  163,
  552,  163,  552,  500,  552,  552,  805, 1231,  506,  552,
  552,  554,  805,  805,  829,  830,  805,  805,  559,  805,
  805,  552,  278,  552,  110,  552,  170,  552,  170,  552,
  171,  552,  171, 1231, 1231,  570, 1317, 1317,  297,  571,
  298, 1231,  299, 1266, 1324,   24, 1325,  521,  522,  110,
  110, 1231,  122,  343,  122,  552,  776,  617,  776,  578,
 1266,  614,  529,  530,  300,  577,  301,  579, 1290, 1292,
  401,  577,  577,  577,  577,  577,  577,  577,  577,  577,
  577,  577,  577,  350,  350,  582,  350,  350,   60,  598,
  577,  577,  577,  587,  577,  539,  577,  577,  577,  111,
  599,  540,  541, 1231,  302,  487,  488,  489,  542,  543,
  577,  191,  164,  191,  164,  620,  600,  620,  602,  601,
  861,  607,  608,  863,  612,  620,  621,  629,  666,  664,
  625,  674,  519,  519,  519,  519,  679,  692,  519,  519,
  519,  519,  519,  519,  519,  519,  519,  519,   52,  519,
  680,  699,  713,  348,  353,  614,  718,  729,   24,  732,
  806,  575,   24,  742,  743,  746,  748,   24,  758,   24,
  356,  770,   24,  780,   24,   24,  771,   24,  772,   24,
  911,   24,  783,   24,   24,   24,   24,  773,  781,   24,
   24,  784,  789,  809,  790,   24,  797,   24,   24,   24,
  812,  933,   24,   24,   24,    2,   24,  819,    3,   24,
  824,   24,   24,   24,   24,  822,  825,  826,   24,   24,
   24,  831,  832,   24,   24,   24,  834,  833,  835,    5,
  839,  843,   24,   24,  840,   24,   24,   24,   24,   24,
   24,  856,  844,  845,   24,  575,  853,  854,  855,  310,
  858,  575,  575,  575,  575,  575,  575,  575,  575,  575,
  575,  575,  575,  868,  519,  869,   24,   24,  453,  882,
  575,  575,  575,  881,  575,   24,  575,  575,  575,  883,
  884,  667,  889,  311,  487,  312,  891,  313,  896,  314,
  575,  315,  897,  316,  575,  317,  898,  318,  913,  319,
  923,  320,   24,  929,  932,  943,   24,  950,  944,  949,
  952,   24,  956,   24,  963,  575,   24,  965,   24,  967,
  970,   24,  977,   24, 1006,   24,  983,   24,  971,  836,
   24,  985,  987,   24,   24,  996,  997,  999, 1004,  575,
 1008,   24,   24,   24, 1007, 1010,   24,   24,   24,   24,
   24, 1014, 1012,   24, 1017,   24,   24,   24,   24, 1016,
 1028, 1037,   24,   24,   24, 1027, 1030,   24,   24,   24,
 1032, 1077, 1033, 1082, 1085, 1116,   24,   24, 1084,   24,
   24,   24,   24,   24,   24, 1090,  815, 1095,   24, 1132,
 1112, 1114,  815,  815, 1119, 1137,  815,  815, 1140,  815,
  815, 1157, 1159, 1197, 1198,   52, 1212, 1208, 1226,   54,
   24,   24, 1246, 1252,   54, 1235,   54,  815, 1248,   54,
 1250,   54, 1259, 1279,   54, 1261,   54, 1262,   54, 1304,
   54, 1264, 1275,   54, 1276, 1283,   54,   54, 1294, 1037,
 1297, 1295, 1126, 1296,   54,   54,   54, 1306, 1136,   54,
   54,   54, 1308,   54, 1312, 1315,   54, 1320,   54,   54,
   54,   54, 1332, 1334,    5,   54,   54,   54,   28,   26,
   54,   54,   54,   27,   73, 1142, 1143,   22,  521,   54,
   54,  597,   54,   54,   74,   54,   54,   54, 1161,   72,
   53,   54,  327,   24,  497,   53,  656,   53,  598, 1171,
   53, 1172,   53,  207,  448,   53,  752,   53,  498,   53,
  752,   53,  657,   71,   96,  438,  328,   53,   53,  722,
  449,  439,   26,  491,  509,   53,   53,   53,  510,  453,
   53,   53,   53,  692,   53,  725,  764,   53,   27,   53,
   53,   53,   53,  641,  493,  703,   53,   53,   53,  641,
  726,   53,   53,   53,  760,  728,  730,  704,  192,  312,
   53,   53,  316,   53,   53,   44,   53,   53,   53,   45,
   75,  125,   53,  483, 1138,  754,  486, 1167, 1091,  572,
  921, 1249, 1319,  717,  754,  754,  754,  754,  754, 1330,
  754,  754,  519,  754,  754,  754,   54,  754,  754,  754,
  754,  865,  860, 1096, 1281,  754,  866,  754,  754,  754,
  754,  754,  754,  867, 1291,  754, 1005,  437, 1293,  754,
  754,  472,  754,  754,  754,  678,  502,  876,  776,  501,
  505,  691,  877,  531,  754,  532,  754,  533,  754,  754,
  534,  537,  754,  673,  754,  754,  754,  754,  754,  754,
  754,  754,  754,  754,  754,  754,  973,  754,  885, 1115,
  754, 1120,  586, 1078, 1080,  754, 1073,  852,  228, 1001,
  228,  706,  980,  228, 1141,  618,    0,   53,  228, 1000,
    0,    0,  228,  754,  754,  228,    0,  754,    0,    0,
    0,  228,  754,  754,  754,  754,  754,    0,  228,  618,
    0,    0,  754,  228,  754,    0,    0,  228,    0,    0,
    0,  754,    0,  754,    0,    0,    0,    0,    0,  228,
    0,  228,    0,    0,    0,  228,  618,    0,    0,    0,
    0,    0,    0,  228,  228,    0,    0,  228,    0,    0,
  228,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,  754,    0,  754,    0,  754,
    0,  754,    0,  754,    0,  754,    0,  754,  754,  698,
    0,    0,    0,  754,    0,  754,    0,    0,  698,  698,
  698,  698,  698,    0,  698,  698,    0,  698,  698,  698,
    0,  698,  698,  698,    0,    0,    0,    0,    0,  698,
    0,  698,  698,  698,  698,  698,  698,    0,    0,  698,
    0,    0,    0,  698,  698,    0,  698,  698,  698,    0,
    0,    0,    0,    0,    0,    0,    0,    0,  698,    0,
  698,    0,  698,  698,    0,    0,  698,    0,  698,  698,
  698,  698,  698,  698,  698,  698,  698,  698,  698,  698,
  228,  698,    0,    0,  698,    0,    0,    0,    0,  698,
    0,    0,    0,    0,    0,    0,    0,    0,    0,   53,
    0,   53,    0,    0,   53,    0,    0,  698,  698,   53,
    0,  698,    0,   53,    0,    0,  698,  698,  698,  698,
  698,    0,   53,    0,    0,    0,  698,    0,  698,   53,
    0,    0,    0,    0,   53,  698,    0,  698,   53,    0,
   53,    0,   53,    0,    0,    0,    0,   53,    0,    0,
   53,    0,   53,    0,    0,    0,   53,    0,    0,   53,
    0,    0,    0,    0,   53,   53,    0,    0,   53,    0,
    0,   53,    0,    0,    0,    0,    0,    0,    0,  698,
    0,  698,    0,  698,    0,  698,    0,  698,    0,  698,
    0,  698,  698,  375,    0,    0,    0,  698,    0,  698,
  158,    0,  127,   83,  376,   84,    0,    0,   85,  377,
    0,  378,  379,   86,    0,  129,  380,   88,    0,    0,
    0,    0,    0,  130,    0,  381,   91,  382,  383,  384,
  385,    0,    0,   92,    0,    0,    0,  386,   93,    0,
  131,  132,   94,    0,    0,    0,    0,    0,    0,    0,
    0,    0,  387,    0,   95,    0,   96,  133,    0,    0,
   97,    0,  388,  134,  389,  135,  390,  136,   98,   99,
  391,  392,  100,  393,    0,  394,  375,    0,  395,    0,
    0,   53,    0,  139,    0,  127,   83,    0,   84,    0,
    0,   85,  128,    0,    0,    0,   86,    0,  129,    0,
   88,  111,    0,    0,    0,  140,  130,    0,    0,   91,
  396,  141,  142,  143,  144,    0,   92,    0, 1174,    0,
  145,   93,  146,  131,  132,   94,    0,    0,    0,  147,
    0,  148,    0,    0,    0,    0,    0,   95,    0,   96,
  133,    0,    0,   97,    0,    0,  134,    0,  135,    0,
  136,   98,   99,  137,    0,  100,    0,    0,  394,    0,
 1175,    0,    0,    0,    0,    0,  139,    0,    0,    0,
    0,    0,    0,  149,    0,  150,    0,  151,    0,  152,
    0,  153,    0,  154,    0,  397,  156,    0,  140,    0,
    0,  157,    0,    0,  141,  142,  143,  144,    0,    0,
    0,    0,    0,  145,    0,  146, 1176, 1177, 1178, 1179,
    0, 1180,  147, 1181,  148, 1182, 1183, 1184, 1185, 1186,
 1187,    0,    0,    0, 1188,    0, 1189,    0, 1190,    0,
 1191,    0, 1192,    0, 1193,    0, 1194,  375, 1195,    0,
    0,    0,    0,    0,    0,    0,  127,   83,    0,   84,
    0,    0,   85,  128,    0,    0,  149,   86,  150,  129,
  151,   88,  152,    0,  153,    0,  154,  130,  262,  156,
   91,    0,    0,    0,  157,    0,    0,   92,    0,    0,
    0,    0,   93,    0,  131,  132,   94,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,   95,    0,
   96,  133,    0,    0,   97,    0,    0,  134,    0,  135,
    0,  136,   98,   99,  137,    0,  100,    0,    0,  138,
  444,    0,    0,    0,    0,    0,    0,  139,    0,  127,
   83,    0,   84,    0,    0,   85,  128,    0,    0,    0,
   86,    0,  129,    0,   88,    0,    0,    0,    0,  140,
  130,    0,    0,   91,    0,  141,  142,  143,  144,    0,
   92,    0,    0,    0,  145,   93,  146,  131,  132,   94,
    0,    0,    0,  147,    0,  148,    0,    0,    0,    0,
    0,   95,    0,   96,  133,    0,    0,   97,    0,    0,
  134,    0,  135,    0,  136,   98,   99,  137,    0,  100,
    0,    0,  138,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,  149,    0,  150,
    0,  151,    0,  152,    0,  153,    0,  154,    0,  262,
  156,    0,  445,  489,    0,  157,    0,    0,  489,  489,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,  489,    0,    0,    0,    0,    0,    0,    0,
    0,    0,  489,    0,    0,  489,  489,    0,    0,    0,
  489,    0,    0,  489,    0,  489,    0,  489,  489,  489,
  489,    0,    0,    0,    0,  489,    0,    0,    0,  489,
  149,    0,  150,  489,  151,    0,  152,    0,  153,    0,
  154,  489,  446,    0,  489,    0,  489,  489,  157,    0,
    0,    0,    0,  489,  489,  489,  489,  489,  489,  489,
  489,  489,  489,  489,  489,    0,    0,    0,    0,    0,
    0,  489,  489,    0,  489,  489,  489,  489,  489,  489,
  489,    0,  489,  489,    0,  489,  489,    0,  489,  489,
  489,  489,  489,  489,  489,  489,  489,    0,    0,  489,
    0,  489,    0,  489,    0,  489,    0,  489,    0,  489,
    0,  489,    0,  489,    0,  489,    0,  489,    0,  489,
    0,  489,    0,  489,    0,  489,    0,  489,    0,  489,
    0,  489,    0,  489,    0,  489,    0,  489,  350,  489,
    0,  489,    0,  350,  350,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,  489,  489,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,  350,    0,    0,
    0,    0,    0,    0,    0,    0,    0,  350,    0,    0,
  350,  350,    0,    0,    0,  350,    0,    0,  350,    0,
  350,    0,  350,  350,  350,  350,    0,    0,    0,    0,
  350,    0,    0,    0,  350,    0,    0,    0,  350,    0,
    0,    0,    0,    0,    0,    0,  350,    0,    0,  350,
    0,  350,  350,    0,    0,    0,    0,    0,  350,  350,
  350,  350,  350,  350,  350,  350,  350,  350,  350,  350,
    0,    0,    0,    0,    0,    0,  350,  350,  350,  350,
  350,  350,  350,  350,  350,  350,    0,    0,    0,    0,
    0,  350,    0,  350,  350,  350,  350,  350,    0,    0,
  350,  350,  350,    0,    0,    0,    0,  350,  350,    0,
    0,    0,  350,    0,  350,    0,  350,    0,  350,    0,
  350,    0,  350,    0,    0,    0,    0,    0,    0,    0,
    0,  350,    0,    0,    0,    0,    0,    0,    0,    0,
    0,  350,    0,    0,  350,  350,  350,    0,    0,  350,
    0,    0,    0,    0,  350,    0,  350,  350,  350,  350,
  350,  350,    0,    0,  350,    0,    0,    0,  350,    0,
    0,    0,  350,    0,    0,    0,    0,    0,    0,    0,
  350,    0,    0,  350,    0,  350,  350,    0,    0,    0,
    0,    0,  350,  350,  350,  350,  350,  350,  350,  350,
  350,  350,  350,  350,    0,    0,    0,    0,    0,    0,
  350,  350,  350,  350,  350,  350,  350,  350,  350,  350,
    0,    0,    0,    0,    0,  350,    0,  350,  350,  350,
  350,  350,    0,    0,  350,  350,  342,    0,    0,    0,
    0,  342,  342,    0,    0,    0,  350,    0,  350,    0,
  350,    0,  350,    0,  350,    0,  350,    0,    0,    0,
    0,    0,    0,    0,    0,  342,    0,    0,    0,    0,
    0,    0,    0,    0,    0,  342,    0,    0,  342,  342,
  350,    0,    0,  342,    0,    0,  342,    0,  342,    0,
  342,  342,  342,  342,  350,  350,    0,    0,  342,    0,
    0,    0,  342,    0,    0,    0,  342,  818,    0,    0,
    0,    0,    0,    0,  342,    0,    0,  342,    0,  342,
  342,    0,    0,    0,    0,    0,  342,  342,  342,  342,
  342,  342,  342,  342,  342,  342,  342,  342,    0,    0,
    0,    0,    0,    0,  342,  342,  342,  342,  342,  342,
  388,  342,  342,  342,    0,    0,  388,    0,    0,  342,
    0,  342,  342,  342,  342,  342,    0,    0,  342,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
  342,    0,  342,    0,  342,    0,  342,    0,  342,  388,
  342,  818,    0,  388,    0,    0,    0,  818,  818,  818,
  818,  818,  818,  818,  818,  818,  818,  818,    0,    0,
    0,    0,    0,    0,  342,    0,  818,  818,  818,    0,
  818,    0,  818,  818,  818,    0,    0,    0,  342,  342,
    0,    0,    0,    0,  388,    0,  818,    0,    0,    0,
  388,  388,  388,  388,  388,  388,  388,  388,  388,  388,
  388,  388,    0,    0,    0,    0,    0,    0,  388,  388,
  388,  388,  388,  388,  352,  388,  388,  388,   53,    0,
  352,    0,    0,  388,    0,  388,  388,  388,  388,    0,
    0,    0,  388,  388,    0,    0,    0,    0,    0,    0,
    0,    0,   53,    0,  388,    0,  388,    0,  388,    0,
  388,    0,  388,    0,  388,   53,    0,  352,    0,    0,
   53,    0,    0,    0,    0,   53,    0,   53,   53,   53,
   53,    0,    0,    0,    0,   53,    0,    0,  388,   53,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,   53,  388,  388,   53,    0,   53,    0,  352,    0,
    0,    0,    0,    0,  352,  352,  352,  352,  352,  352,
  352,  352,  352,  352,  352,  352,   53,    0,   53,    0,
    0,    0,  196,  352,  352,  352,  352,  352,  352,  352,
  352,  352,  423,  352,  352,    0,  352,  352,  423,  352,
    0,  352,  352,  352,  352,  352,  352,  352,    0,    0,
  352,    0,  352,    0,  352,    0,  352,    0,  352,    0,
  352,    0,  352,    0,  352,    0,  352,    0,  352,    0,
  352,    0,  352,    0,  352,  423,  352,    0,  352,    0,
  352,    0,  352,    0,  352,    0,  352,    0,  352,    0,
  352,    0,  352,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,  352,    0,    0,    0,
    0,    0,    0,    0,    0,    0,  423,    0,    0,    0,
    0,    0,  423,  423,  423,  423,  423,  423,  423,  423,
  423,  423,  423,  423,    0,    0,    0,    0,    0,    0,
    0,  423,  423,  423,  423,  423,  423,  423,  423,  423,
  350,  423,  423,    0,  423,  423,  350,  423,    0,  423,
  423,  423,  423,  423,  423,  423,    0,    0,  423,    0,
  423,    0,  423,    0,  423,    0,  423,    0,  423,    0,
  423,    0,  423,    0,  423,    0,  423,    0,  423,    0,
  423,    0,  423,  350,  423,    0,  423,    0,  423,    0,
  423,    0,  423,    0,  423,    0,  423,    0,  423,    0,
  423,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,  423,    0,    0,    0,    0,    0,
    0,    0,    0,    0,  350,    0,    0,    0,    0,    0,
  350,  350,  350,  350,  350,  350,  350,  350,  350,  350,
  350,  350,    0,    0,    0,    0,    0,    0,    0,  350,
  350,  350,  350,  350,  350,  350,  350,  350,  489,  350,
  350,    0,  350,  350,  489,  350,    0,  350,  350,  350,
  350,  350,  350,  350,    0,    0,  350,    0,  350,    0,
  350,    0,  350,    0,  350,    0,  350,    0,  350,    0,
  350,    0,  350,    0,  350,    0,  350,    0,  350,    0,
  350,  489,  350,    0,  350,    0,  350,    0,  350,    0,
  350,    0,  350,    0,  350,    0,  350,    0,  350,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,  350,    0,    0,    0,    0,    0,    0,    0,
    0,    0,  489,    0,    0,    0,    0,    0,  489,  489,
  489,  489,  489,  489,  489,  489,  489,  489,  489,  489,
    0,    0,  434,    0,    0,    0,  489,  489,  434,  489,
  489,  489,  489,  489,  489,  489,    0,  489,  489,    0,
  489,  489,    0,  489,    0,  489,  489,  489,  489,  489,
  489,  489,    0,    0,  489,    0,  489,    0,  489,    0,
  489,    0,  489,    0,  489,  434,  489,    0,  489,    0,
  489,    0,  489,    0,  489,    0,  489,    0,  489,    0,
  489,    0,  489,    0,  489,    0,  489,    0,  489,    0,
  489,    0,  489,    0,  489,    0,  489,    0,    0,    0,
    0,    0,    0,    0,    0,    0,  434,    0,    0,    0,
  489,    0,  434,  434,  434,  434,  434,  434,  434,  434,
  434,  434,  434,  434,    0,    0,    0,    0,    0,    0,
    0,  434,  434,  434,  434,  434,  434,  434,  434,  434,
  501,  434,  434,    0,  434,  434,  501,  434,    0,  434,
  434,  434,  434,  434,  434,  434,    0,    0,  434,    0,
  434,    0,  434,    0,  434,    0,  434,    0,  434,    0,
  434,    0,  434,    0,  434,    0,  434,    0,  434,    0,
  434,    0,  434,  501,  434,    0,  434,    0,  434,    0,
  434,    0,  434,    0,  434,    0,  434,    0,  434,    0,
  434,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,  434,    0,    0,    0,    0,    0,
    0,    0,    0,    0,  501,    0,    0,    0,    0,    0,
  501,  501,  501,  501,  501,  501,  501,  501,  501,  501,
  501,  501,    0,    0,    0,    0,    0,    0,    0,  501,
  501,  501,  501,  501,  501,  501,  501,  501,  431,  501,
  501,    0,  501,  501,  431,  501,    0,  501,  501,  501,
  501,  501,  501,  501,    0,    0,  501,    0,  501,    0,
  501,    0,  501,    0,  501,    0,  501,    0,  501,    0,
  501,    0,  501,    0,  501,    0,  501,    0,  501,    0,
  501,  431,  501,    0,  501,    0,  501,    0,  501,    0,
  501,    0,  501,    0,  501,    0,  501,    0,  501,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,  501,    0,    0,    0,    0,    0,    0,    0,
    0,    0,  431,    0,    0,    0,    0,    0,  431,  431,
  431,  431,  431,  431,  431,  431,  431,  431,  431,  431,
    0,    0,    0,    0,    0,    0,    0,  431,    0,  431,
    0,  431,    0,  431,  431,  431,    0,  431,  431,    0,
  431,  431,    0,  431,    0,  431,  431,  431,  431,  431,
  431,  431,    0,    0,    0,    0,    0,    0,  431,    0,
  431,    0,  431,    0,  431,    0,  431,    0,  431,    0,
  431,    0,  431,    0,  431,    0,  431,    0,  431,    0,
  431,    0,  431,    0,  431,    0,  431,    0,  431,    0,
  431,    0,  431,   37,    0,    0,  431,   37,    0,    0,
    0,    0,   37,    0,   37,    0,    0,   37,    0,   37,
  431,    0,   37,    0,   37,    0,   37,    0,   37,    0,
    0,    0,    0,    0,   37,   37,    0,    0,    0,    0,
    0,    0,   37,   37,   37,    0,    0,   37,   37,   37,
    0,   37,    0,    0,   37,    0,   37,   37,   37,   37,
    0,    0,    0,   37,   37,   37,    0,    0,   37,   37,
   37,    0,    0,    0,    0,    0,    0,   37,   37,    0,
   37,   37,   37,   37,   37,   37,    0,    0,    0,   37,
    0,    0,    0,    0,   38,    0,    0,    0,   38,    0,
    0,    0,    0,   38,    0,   38,    0,    0,   38,    0,
   38,   37,   37,   38,    0,   38,    0,   38,    0,   38,
    0,    0,    0,    0,    0,   38,   38,    0,    0,    0,
    0,    0,    0,   38,   38,   38,    0,    0,   38,   38,
   38,    0,   38,    0,    0,   38,    0,   38,   38,   38,
   38,    0,    0,    0,   38,   38,   38,    0,    0,   38,
   38,   38,    0,    0,    0,    0,    0,    0,   38,   38,
    0,   38,   38,   38,   38,   38,   38,    0,    0,    0,
   38,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,   37,   35,    0,    0,    0,   35,
    0,    0,   38,   38,   35,    0,   35,    0,    0,   35,
    0,   35,    0,    0,   35,    0,   35,    0,   35,    0,
   35,    0,    0,   35,    0,    0,   35,   35,    0,    0,
    0,    0,    0,    0,   35,   35,   35,    0,    0,   35,
   35,   35,    0,   35,    0,    0,   35,    0,   35,   35,
   35,   35,    0,    0,    0,   35,   35,   35,    0,    0,
   35,   35,   35,    0,    0,    0,    0,    0,    0,   35,
   35,    0,   35,   35,    0,   35,   35,   35,    0,    0,
    0,   35,    0,    0,    0,   38,   36,    0,    0,    0,
   36,    0,    0,    0,    0,   36,    0,   36,    0,    0,
   36,    0,   36,   35,   35,   36,    0,   36,    0,   36,
    0,   36,    0,    0,   36,    0,    0,   36,   36,    0,
    0,    0,    0,    0,    0,   36,   36,   36,    0,    0,
   36,   36,   36,    0,   36,    0,    0,   36,    0,   36,
   36,   36,   36,    0,    0,    0,   36,   36,   36,    0,
    0,   36,   36,   36,    0,    0,    0,    0,    0,    0,
   36,   36,    0,   36,   36,    0,   36,   36,   36,    0,
    0,    0,   36,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,   35,   39,    0,    0,
    0,   53,    0,    0,   36,   36,   53,    0,   53,    0,
    0,   53,    0,   53,    0,    0,   53,    0,   53,    0,
   53,    0,   53,    0,    0,   53,    0,    0,   53,   53,
    0,    0,    0,    0,    0,    0,   53,   53,   53,    0,
    0,   53,   53,   53,    0,   53,    0,    0,   53,    0,
   53,   53,   53,   53,    0,    0,    0,   53,   53,   53,
    0,    0,   53,   53,   53,    0,    0,    0,    0,    0,
    0,   53,   53,    0,   53,   53,    0,   53,   53,   53,
    0,   40,    0,   53,    0,   53,    0,   36,    0,    0,
   53,    0,   53,    0,    0,   53,    0,   53,    0,    0,
   53,    0,   53,    0,   53,   39,   53,    0,    0,   53,
    0,    0,   53,   53,    0,    0,    0,    0,    0,    0,
   53,   53,   53,    0,    0,   53,   53,   53,    0,   53,
    0,    0,   53,    0,   53,   53,   53,   53,    0,    0,
    0,   53,   53,   53,    0,    0,   53,   53,   53,    0,
    0,    0,    0,    0,    0,   53,   53,    0,   53,   53,
    0,   53,   53,   53,    0,    0,    0,   53,    0,    0,
  707,  707,  707,  707,    0,    0,  707,  707,    0,  707,
  707,  707,    0,  707,  707,  707,    0,    0,   53,   40,
    0,  707,    0,  707,  707,  707,  707,  707,  707,    0,
    0,  707,    0,    0,    0,  707,  707,    0,  707,  707,
  707,    0,    0,    0,    0,    0,    0,    0,    0,    0,
  707,    0,  707,    0,  707,  707,    0,    0,  707,    0,
  707,  707,  707,  707,  707,  707,  707,  707,  707,  707,
  707,  707,    0,  707,    0,    0,  707,    0,    0,    0,
    0,  707,    0,    0,   83,    0,   84,    0,    0,   85,
    0,    0,    0,    0,   86,    0,    0,    0,   88,  707,
    0,    0,   53,  707,    0,    0,    0,   91,  707,  707,
  707,  707,  707,    0,   92,    0,    0,    0,  707,   93,
  707,    0,    0,   94,    0,  281,    0,  707,    0,  707,
    0,    0,  282,    0,    0,   95,    0,   96,    0,    0,
    0,   97,    0,    0,  283,    0,    0,    0,    0,   98,
   99,    0,    0,  100,    0,    0,  117,    0,    0,    0,
    0,  127,   83,    0,   84,    0,    0,   85,  128,    0,
    0,  707,   86,  707,  129,  707,   88,  707,    0,  707,
    0,  707,  130,  707,  707,   91,    0,    0,    0,  707,
    0,    0,   92,    0,    0,    0,    0,   93,    0,  131,
  132,   94,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,   95,    0,   96,  133,  564,    0,   97,
    0,    0,  134,    0,  135,    0,  136,   98,   99,  137,
    0,  100,    0,    0,  138,    0,    0,    0,  565,    0,
    0,    0,  139,    0,  127,   83,    0,   84,    0,    0,
   85,  128,    0,    0,    0,   86,    0,  129,    0,   88,
  458,  655,    0,    0,  140,  130,  284,    0,   91,    0,
  141,  142,  143,  144,    0,   92,    0,    0,    0,  145,
   93,  146,  131,  132,   94,    0,  491,    0,  147,    0,
  148,    0,    0,  492,    0,    0,   95,    0,   96,  133,
    0,    0,   97,    0,    0,  134,    0,  135,    0,  136,
   98,   99,  137,    0,  100,    0,    0,  138,    0,    0,
    0,  493,    0,    0,    0,  139,    0,    0,    0,    0,
    0,    0,  149,    0,  150,    0,  151,    0,  152,    0,
  153,    0,  154,    0,  262,  156,    0,  140,  684,    0,
  157,    0,    0,  141,  142,  143,  144,    0,    0,    0,
    0,    0,  145,    0,  146,    0,    0,    0,    0,    0,
    0,  147,    0,  148,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,  127,   83,    0,   84,    0,
    0,   85,  128,    0,    0,  149,   86,  150,  129,  151,
   88,  152,    0,  153,    0,  154,  130,  262,  156,   91,
    0,    0,    0,  157,    0,    0,   92,    0,    0,    0,
    0,   93,    0,  131,  132,   94,    0,  491,    0,    0,
    0,    0,    0,    0,  492,    0,    0,   95,    0,   96,
  133,    0,    0,   97,    0,    0,  134,    0,  135,    0,
  136,   98,   99,  137,    0,  100,    0,    0,  138,    0,
    0,    0,  493,    0,    0,    0,  139,    0,    0,   83,
    0,   84,    0,    0,   85,    0, 1034,    0,    0,   86,
    0,    0,    0,   88,    0,    0,    0,    0,  140,  794,
    0,    0,   91,    0,  141,  142,  143,  144,    0,   92,
    0,    0,    0,  145,   93,  146, 1035,    0,   94,    0,
    0,    0,  147,    0,  148,    0,    0,    0,    0,    0,
   95,    0,   96,    0,    0,    0,   97, 1036,    0,    0,
    0,    0,    0,    0,   98,   99,    0,    0,  100,    0,
    0,  117,    0,    0,    0,    0,  127,   83,    0,   84,
    0,    0,   85,  128,    0,    0,  149,   86,  150,  129,
  151,   88,  152,    0,  153,    0,  154,  130,  262,  156,
   91,    0,    0,    0,  157,    0,    0,   92,    0,    0,
    0,    0,   93,    0,  131,  132,   94,    0,  491,    0,
    0,    0,    0,    0,    0,  492,    0,    0,   95,    0,
   96,  133,    0,    0,   97,    0,    0,  134,    0,  135,
    0,  136,   98,   99,  137,    0,  100,    0,    0,  138,
    0,    0,    0,  493,    0,    0,    0,  139,    0,  127,
   83,    0,   84,    0,    0,   85,  128,    0,    0,    0,
   86,    0,  129,    0,   88,    0,    0,    0,    0,  140,
  130,   74,    0,   91,    0,  141,  142,  143,  144,    0,
   92,    0,    0,    0,  145,   93,  146,  131,  132,   94,
    0,    0,    0,  147,    0,  148,    0,    0,    0,    0,
    0,   95,    0,   96,  133,  564,    0,   97,    0,    0,
  134,    0,  135,    0,  136,   98,   99,  137,    0,  100,
    0,    0,  138,    0,    0,    0,  565,    0,    0,    0,
  139,    0,    0,    0,    0,    0,    0,  149,    0,  150,
    0,  151,    0,  152,    0,  153,    0,  154,  458,  262,
  156,    0,  140,    0,    0,  157,    0,    0,  141,  142,
  143,  144,    0,    0,    0,    0,    0,  145,    0,  146,
    0,    0,    0,    0,    0,    0,  147,    0,  148,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
  127,   83,    0,   84,    0,    0,   85,  128,    0,    0,
  149,   86,  150,  129,  151,   88,  152,    0,  153,    0,
  154,  130,  262,  156,   91,    0,    0,    0,  157,    0,
    0,   92,    0,    0,    0,    0,   93,    0,  131,  132,
   94,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,   95,    0,   96,  133,    0,    0,   97,    0,
    0,  134,    0,  135,    0,  136,   98,   99,  137,    0,
  100,    0,    0,  138,    0,    0,    0,    0,    0,    0,
    0,  139,    0,    0,   83,    0,   84,    0,    0,   85,
    0,    0,    0,    0,   86,    0,    0,    0,   88,  646,
  875, 1134,    0,  140,    0,    0,    0,   91,    0,  141,
  142,  143,  144,    0,   92,    0,    0,    0,  145,   93,
  146,    0,    0,   94,    0,    0,    0,  147,    0,  148,
    0,    0,    0,    0,    0,   95,    0,   96,    0,    0,
    0,   97,    0,    0,    0,    0,    0,    0,    0,   98,
   99,    0,    0,  100,    0,    0, 1135,    0,    0,    0,
    0,  127,   83,    0,   84,    0,    0,   85,  128,    0,
    0,  149,   86,  150,  129,  151,   88,  152,    0,  153,
    0,  154,  130,  647,  156,   91,    0,    0,    0,  157,
    0,    0,   92,    0,    0,    0,    0,   93,    0,  131,
  132,   94,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,   95,    0,   96,  133,    0,    0,   97,
    0,    0,  134,    0,  135,    0,  136,   98,   99,  137,
    0,  100,    0,    0,  138,    0,    0,    0,    0,    0,
    0,    0,  139,    0,  127,   83,    0,   84,    0,    0,
   85,  128,    0,    0,    0,   86,    0,  129,    0,   88,
    0,    0,    0,    0,  140,  130,   74,  366,   91,    0,
  141,  142,  143,  144,    0,   92,    0,    0,    0,  145,
   93,  146,  131,  132,   94,    0,    0,    0,  147,    0,
  148,    0,    0,    0,    0,    0,   95,    0,   96,  133,
    0,    0,   97,    0,    0,  134,    0,  135,    0,  136,
   98,   99,  137,    0,  100,    0,    0,  138,    0,    0,
    0,    0,    0,    0,    0,  139,    0,    0,    0,    0,
    0,    0,  149,    0,  150,    0,  151,    0,  152,    0,
  153,    0,  154,    0,  262,  156,    0,  140,  535,    0,
  157,    0,    0,  141,  142,  143,  144,    0,    0,    0,
    0,    0,  145,    0,  146,    0,    0,    0,    0,    0,
    0,  147,    0,  148,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,  127,   83,    0,   84,    0,
    0,   85,  128,    0,    0,  149,   86,  150,  129,  151,
   88,  152,    0,  153,    0,  154,  130,  262,  156,   91,
    0,    0,    0,  157,    0,    0,   92,    0,    0,    0,
    0,   93,    0,  131,  132,   94,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,   95,    0,   96,
  133,    0,    0,   97,    0,    0,  134,    0,  135,    0,
  136,   98,   99,  137,    0,  100,    0,    0,  138,    0,
    0,    0,    0,    0,    0,    0,  139,    0,  127,   83,
    0,   84,    0,    0,   85,  128,    0,    0,    0,   86,
    0,  129,    0,   88,  646,    0,    0,    0,  140,  130,
    0,    0,   91,    0,  141,  142,  143,  144,    0,   92,
    0,    0,    0,  145,   93,  146,  131,  132,   94,    0,
    0,    0,  147,    0,  148,    0,    0,    0,    0,    0,
   95,    0,   96,  133,    0,    0,   97,    0,    0,  134,
    0,  135,    0,  136,   98,   99,  137,    0,  100,    0,
    0,  138,    0,    0,    0,    0,    0,    0,    0,  139,
    0,    0,    0,    0,    0,    0,  149,    0,  150,    0,
  151,    0,  152,    0,  153,    0,  154,    0,  647,  156,
  715,  140,    0,    0,  157,    0,    0,  141,  142,  143,
  144,    0,    0,    0,    0,    0,  145,    0,  146,    0,
    0,    0,    0,    0,    0,  147,    0,  148,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,  127,
   83,    0,   84,    0,    0,   85,  128,    0,    0,  149,
   86,  150,  129,  151,   88,  152,    0,  153,    0,  154,
  130,  262,  156,   91,    0,    0,    0,  157,    0,    0,
   92,    0,    0,    0,    0,   93,    0,  131,  132,   94,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,   95,    0,   96,  133,    0,    0,   97,    0,    0,
  134,    0,  135,    0,  136,   98,   99,  137,    0,  100,
    0,    0,  138,    0,    0,    0,    0,    0,    0,    0,
  139,    0,    0,   83,    0,   84,    0,    0,   85,    0,
    0,    0,    0,   86,    0,    0,    0,   88,    0,    0,
    0,    0,  140,    0,    0,    0,   91,  759,  141,  142,
  143,  144,    0,   92,    0,    0,    0,  145,   93,  146,
    0,    0,   94,    0,    0,    0,  147,    0,  148,    0,
    0,    0,    0,    0,   95,    0,   96,    0,    0,    0,
   97,    0,    0,    0,    0,    0,    0,    0,   98,   99,
    0,    0,  100,    0,    0,  117,    0,    0,    0,    0,
  127,   83,    0,   84,    0,    0,   85,  128,    0,    0,
  149,   86,  150,  129,  151,   88,  152,    0,  153,    0,
  154,  130,  262,  156,   91,    0,    0,    0,  157,    0,
    0,   92,    0,    0,    0,    0,   93,    0,  131,  132,
   94,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,   95,    0,   96,  133,    0,    0,   97,    0,
    0,  134,    0,  135,    0,  136,   98,   99,  137,    0,
  100,    0,    0,  138,    0,    0,    0,    0,    0,    0,
    0,  139,    0,  127,   83,    0,   84,    0,    0,   85,
  128,    0,    0,    0,   86,    0,  129,    0,   88,    0,
  777,    0,    0,  140,  130,   74,    0,   91,    0,  141,
  142,  143,  144,    0,   92,    0,    0,    0,  145,   93,
  146,  131,  132,   94,    0,    0,    0,  147,    0,  148,
    0,    0,    0,    0,    0,   95,    0,   96,  133,    0,
    0,   97,    0,    0,  134,    0,  135,    0,  136,   98,
   99,  137,    0,  100,    0,    0,  138,    0,    0,    0,
    0,    0,    0,    0,  139,    0,    0,    0,    0,    0,
    0,  149,    0,  150,    0,  151,    0,  152,    0,  153,
    0,  154,  453,  262,  156,    0,  140,    0,    0,  157,
    0,    0,  141,  142,  143,  144,    0,    0,    0,    0,
    0,  145,    0,  146,    0,    0,    0,    0,    0,    0,
  147,    0,  148,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,  127,   83,    0,   84,    0,    0,
   85,  128,    0,    0,  149,   86,  150,  129,  151,   88,
  152,    0,  153,    0,  154,  130,  262,  156,   91,    0,
    0,    0,  157,    0,    0,   92,    0,    0,    0,    0,
   93,    0,  131,  132,   94,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,   95,    0,   96,  133,
    0,    0,   97,    0,    0,  134,    0,  135,    0,  136,
   98,   99,  137,    0,  100,    0,    0,  138,    0,    0,
    0,    0,    0,    0,    0,  139,    0,  127,   83,    0,
   84,    0,    0,   85,  128,    0,    0,    0,   86,    0,
  129,    0,   88,    0,    0,    0,    0,  140,  130,    0,
    0,   91,    0,  141,  142,  143,  144,    0,   92,    0,
    0,    0,  145,   93,  146,  131,  132,   94,    0,    0,
    0,  147,    0,  148,    0,    0,    0,    0,    0,   95,
    0,   96,  133,    0,    0,   97,    0,    0,  134,    0,
  135,    0,  136,   98,   99,  137,    0,  100,    0,    0,
  138,    0,    0,    0,    0,    0,    0,    0,  139,    0,
    0,    0,    0,    0,    0,  149,    0,  150,    0,  151,
    0,  152,    0,  153,    0,  154,    0,  155,  156,    0,
  140,    0,    0,  157,    0,    0,  141,  142,  143,  144,
    0,    0,    0,    0,    0,  145,    0,  146,    0,    0,
    0,    0,    0,    0,  147,    0,  148,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,  127,   83,
    0,   84,    0,    0,   85,  128,    0,    0,  149,   86,
  150,  129,  151,   88,  152,    0,  153,    0,  154,  130,
  262,  156,   91,    0,    0,    0,  157,    0,    0,   92,
    0,    0,    0,    0,   93,    0,  131,  132,   94,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
   95,    0,   96,  133,    0,    0,   97,    0,    0,  134,
    0,  135,    0,  136,   98,   99,  137,    0,  100,    0,
    0,  394,    0,    0,    0,    0,    0,    0,    0,  139,
    0,  599,  599,    0,  599,    0,    0,  599,  599,    0,
    0,    0,  599,    0,  599,    0,  599,    0,    0,    0,
    0,  140,  599,    0,    0,  599,    0,  141,  142,  143,
  144,    0,  599,    0,    0,    0,  145,  599,  146,  599,
  599,  599,    0,    0,    0,  147,    0,  148,    0,    0,
    0,    0,    0,  599,    0,  599,  599,    0,    0,  599,
    0,    0,  599,    0,  599,    0,  599,  599,  599,  599,
    0,  599,    0,    0,  599,    0,    0,    0,    0,    0,
    0,    0,  599,    0,    0,    0,    0,    0,    0,  149,
    0,  150,    0,  151,    0,  152,    0,  153,    0,  154,
    0,  262,  156,    0,  599,    0,    0,  157,    0,    0,
  599,  599,  599,  599,    0,    0,    0,    0,    0,  599,
    0,  599,    0,    0,    0,    0,    0,    0,  599,    0,
  599,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,  127,   83,    0,   84,    0,    0,   85,  128,
    0,    0,  599,   86,  599,  129,  599,   88,  599,    0,
  599,    0,  599,  130,  599,  599,   91,    0,    0,    0,
  599,    0,    0,   92,    0,    0,    0,    0,   93,    0,
  131,  132,   94,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,   95,    0,   96,  133,    0,    0,
   97,    0,    0,  134,    0,  135,    0,  136,   98,   99,
  137,    0,  100,    0,    0,  138,    0,    0,    0,    0,
    0,    0,    0,    0,    0,  127,   83,    0,   84,    0,
    0,   85,  128,    0,    0,    0,   86,    0,  129,    0,
   88,    0,    0,    0,    0,  140,  130,    0,    0,   91,
    0,  141,  142,  143,  144,    0,   92,    0,    0,    0,
  145,   93,  146,  131,  132,   94,    0,    0,    0,  147,
    0,  148,    0,    0,    0,    0,    0,   95,    0,   96,
  133,    0,    0,   97,    0,    0,  134,    0,  135,    0,
  136,   98,   99,  137,    0,  100,    0,    0,  138,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,  149,    0,  150,    0,  151,    0,  152,
    0,  153,    0,  154,    0,  267,    0,    0,  140,    0,
    0,  157,    0,    0,  141,  142,  143,  144,    0,    0,
    0,    0,    0,  145,    0,    0,    0,    0,    0,    0,
    0,    0,  147,    0,  148,  127,   83,    0,   84,    0,
    0,   85,  128,    0,    0,    0,   86,    0,  129,    0,
   88,    0,    0,    0,    0,    0,  130,    0,    0,   91,
    0,    0,    0,    0,    0,    0,   92,    0,    0,    0,
    0,   93,    0,  131,  132,   94,  149,    0,  150,    0,
  151,    0,  152,    0,  153,    0,  154,   95,  267,   96,
  133,    0,    0,   97,  157,    0,  134,    0,  135,    0,
  136,   98,   99,  137,    0,  100,    0,    0,  138,    0,
  127,   83,    0,   84,    0,    0,   85,  128,    0,    0,
    0,   86,    0,  129,    0,   88,    0,    0,    0,    0,
    0,  130,    0,    0,   91,    0,    0,    0,  140,    0,
    0,   92,    0,    0,  141,    0,   93,  144,  131,  132,
   94,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,   95,    0,   96,  133,    0,    0,   97,    0,
    0,  134,    0,  135,    0,  136,   98,   99,  137,    0,
  100,    0,    0,  138,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,  149,    0,  150,    0,
  151,    0,  152,  503,  153,    0,  154,    0,  267,    0,
    0,    0,    0,    0,  157,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
  127,   83,    0,   84,    0,    0,   85,  128,    0,    0,
    0,   86,    0,  129,    0,   88,    0,    0,    0,    0,
    0,  130,    0,    0,   91,    0,    0,    0,    0,    0,
    0,   92,    0,    0,    0,    0,   93,    0,  131,  132,
   94,  149,    0,  150,    0,  151,    0,  152,    0,  153,
    0,  154,   95,  267,   96,  133,    0,    0,   97,  157,
    0,  134,    0,  135,    0,  136,   98,   99,  137,    0,
  100,  127,   83,  138,   84,    0,    0,   85,  128,    0,
    0,    0,   86,    0,  129,    0,   88,    0,    0,    0,
    0,    0,  130,    0,    0,   91,    0,    0,    0,    0,
    0,    0,   92,  445,    0,    0,    0,   93,    0,  131,
  132,   94,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,   95,    0,   96,  133,    0,    0,   97,
    0,    0,  134,    0,  135,    0,  136,   98,   99,  137,
    0,  100,    0,    0,  138,    0,    0,    0,  350,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,  149,  350,  150,  775,  151,    0,  152,    0,  153,
    0,  154,    0,  267,    0,  350,   53,    0,   53,  157,
  350,    0,    0,  350,    0,  350,    0,  350,  350,  350,
  350,    0,    0,    0,    0,  350,    0,    0,    0,  350,
   53,    0,    0,  350,    0,    0,    0,    0,    0,    0,
    0,  350,    0,   53,  350,    0,  350,    0,   53,    0,
    0,    0,    0,   53,    0,   53,   53,   53,   53,    0,
    0,   53,  149,   53,  150,    0,  151,   53,  152,    0,
  153,    0,  154,  350,  267,  289,   54,  350,   54,   53,
  157,   54,   53,   54,   53,    0,   54,    0,   54,   54,
    0,   54,  350,   54,    0,   54,  350,   54,   54,   54,
   54,    0,    0,   54,   54,    0,    0,    0,    0,   54,
  311,   54,   54,   54,    0,    0,   54,   54,   54,    0,
   54,    0,   54,   54,   54,   54,   54,   54,   54,   54,
    0,   54,   54,   54,   54,    0,    0,   54,   54,   54,
    0,   54,    0,    0,    0,    0,   54,   54,    0,   54,
   54,    0,   54,   54,   54,  350,  289,    0,   54,    0,
   53,    0,    0,    0,    0,   53,    0,   53,    0,    0,
   53,    0,   53,   53,   54,   53,   54,   53,    0,   53,
    0,   53,   53,   53,   53,    0,    0,   53,   53,   54,
    0,    0,    0,   53,    0,   53,   53,   53,    0,    0,
   53,    0,   53,    0,   53,    0,    0,   53,    0,   53,
   53,   53,   53,    0,    0,    0,   53,   53,   53,    0,
    0,   53,   53,   53,    0,    0,    0,    0,    0,    0,
   53,   53,    0,   53,   53,    0,   53,   53,   53,    0,
    0,    0,   53,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,   53,    0,    0,    0,    0,
   53,    0,   53,   54,   82,   53,    0,   53,   53,    0,
   53,    0,   53,   53,   53,    0,   53,   53,   53,   53,
    0,    0,   53,   53,    0,    0,    0,    0,   53,    0,
   53,   53,   53,    0,    0,   53,    0,   53,    0,   53,
    0,    0,   53,    0,   53,   53,   53,   53,    0,    0,
    0,   53,   53,   53,    0,    0,   53,   53,   53,    0,
    0,    0,    0,    0,    0,   53,   53,    0,   53,   53,
    0,   53,   53,   53,    0,    0,    0,   53,    0,   53,
    0,    0,    0,    0,   53,    0,   53,   53,    0,   53,
    0,   53,   53,    0,   53,    0,   53,    0,   53,   83,
   53,   53,   53,   53,    0,    0,   53,   53,   53,    0,
    0,    0,   53,    0,   53,   53,   53,    0,    0,   53,
    0,   53,    0,   53,    0,    0,   53,    0,   53,   53,
   53,   53,    0,    0,    0,   53,   53,   53,    0,    0,
   53,   53,   53,    0,    0,    0,    0,    0,    0,   53,
   53,    0,   53,   53,    0,   53,   53,   53,    0,    0,
    0,   53,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,   53,    0,    0,    0,    0,   53,
    0,   53,   53,  104,   53,    0,   53,   53,    0,   53,
    0,   53,   53,   53,    0,   53,   53,   53,   53,    0,
    0,   53,   53,    0,    0,    0,    0,   53,    0,   53,
   53,   53,    0,    0,   53,    0,   53,    0,   53,    0,
    0,   53,    0,   53,   53,   53,   53,    0,    0,    0,
   53,   53,   53,    0,    0,   53,   53,   53,    0,    0,
    0,    0,    0,    0,   53,   53,    0,   53,   53,    0,
   53,   53,   53,    0,    0,    0,   53,    0,  635,    0,
    0,    0,    0,  635,    0,  635,   53,    0,  635,    0,
  635,  635,    0,  635,    0,  635,    0,  635,  105,  635,
  635,  635,  635,    0,    0,  635,  635,   53,    0,    0,
    0,  635,    0,  635,  635,  635,    0,    0,  635,    0,
  635,    0,  635,    0,    0,  635,    0,  635,  635,  635,
  635,    0,    0,    0,  635,  635,  635,    0,    0,  635,
  635,  635,    0,    0,    0,    0,    0,    0,  635,  635,
    0,  635,  635,    0,  635,  635,  635,    0,    0,    0,
  635,  637,    0,    0,    0,    0,  637,    0,  637,    0,
    0,  637,    0,  637,  637,    0,  637,    0,  637,    0,
  637,   53,  637,  637,  637,  637,    0,    0,  637,  637,
    0,  298,    0,    0,  637,    0,  637,  637,  637,    0,
    0,  637,    0,  637,    0,  637,    0,    0,  637,    0,
  637,  637,  637,  637,    0,    0,    0,  637,  637,  637,
    0,    0,  637,  637,  637,    0,    0,    0,    0,    0,
    0,  637,  637,    0,  637,  637,    0,  637,  637,  637,
   53,    0,    0,  637,    0,   53,    0,   53,    0,    0,
   53,    0,   53,   53,    0,   53,    0,   53,    0,   53,
    0,   53,   53,    0,   53,  635,    0,    0,   53,    0,
    0,    0,    0,    0,  297,   53,   53,   53,    0,    0,
   53,    0,   53,    0,   53,    0,    0,   53,    0,   53,
   53,   53,   53,    0,    0,    0,   53,   53,   53,    0,
    0,   53,   53,   53,    0,    0,    0,    0,    0,    0,
   53,   53,    0,   53,   53,    0,   53,   53,   53,   53,
    0,    0,   53,    0,   53,  352,   53,    0,    0,   53,
    0,   53,   53,    0,   53,    0,   53,    0,   53,    0,
   53,   53,    0,   53,  214,    0,    0,   53,  637,    0,
    0,    0,    0,    0,   53,   53,   53,    0,    0,   53,
    0,   53,  352,   53,    0,    0,   53,    0,   53,   53,
   53,   53,    0,    0,    0,   53,   53,   53,    0,    0,
   53,   53,   53,    0,    0,    0,    0,    0,    0,   53,
   53,  526,   53,   53,    0,   53,   53,   53,    0,    0,
    0,   53,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,  215,    0,    0,    0,   53,  526,  352,
    0,  352,    0,  352,    0,    0,  352,    0,  352,  352,
    0,  352,  352,    0,  352,    0,  352,  352,  352,  352,
  352,  352,  352,    0,    0,  352,    0,  352,    0,  352,
    0,  352,    0,  352,    0,  352,    0,  352,    0,  352,
    0,  352,    0,  352,    0,  352,    0,  352,    0,  352,
    0,  352,    0,  352,    0,  352,    0,  352,    0,  352,
    0,  352,    0,  352,    0,  352,    0,  352,  526,    0,
  526,    0,  526,    0,  526,  526,   53,  526,  526,    0,
  526,  352,  526,  526,    0,  526,  526,  526,    0,    0,
    0,    0,    0,    0,    0,  526,    0,  526,    0,  526,
    0,  526,    0,  526,    0,  526,    0,  526,    0,  526,
    0,  526,    0,  526,    0,  526,    0,  526,    0,  526,
    0,  526,    0,  526,    0,  526,    0,  526,    0,  526,
    0,    0,  621,  526,  621,    0,    0,  621,    0,  621,
  621,    0,  621,    0,  621,    0,  621,  491,  621,  621,
  621,    0,    0,    0,  621,  621,    0,    0,    0,    0,
  621,    0,  621,  621,    0,    0,    0,  621,    0,    0,
    0,  621,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,  621,    0,  621,    0,    0,    0,  621,
  621,    0,    0,    0,    0,    0,    0,  621,  621,    0,
  620,  621,  620,    0,  621,  620,    0,  620,  620,  621,
  620,    0,  620,    0,  620,    0,  620,  620,  620,    0,
    0,    0,  620,  620,    0,  621,    0,  621,  620,    0,
  620,  620,   83,    0,   84,  620,    0,   85,    0,  620,
 1121,    0,   86,    0,   87,    0,   88,    0,   89, 1122,
 1123,  620,    0,  620,   90,   91,    0,  620,  620,    0,
 1124,    0,   92,    0,    0,  620,  620,   93,    0,  620,
    0,   94,  620,    0,    0,    0,    0,  620,    0,    0,
    0,    0,    0,   95,    0,   96,    0,    0,    0,   97,
    0,    0,    0,    0,    0,    0,    0,   98,   99,    0,
    0,  100,    0,    0,  101,    0,    0,    0,  296,  102,
    0,  620,    0,  620,  621,    0,  620,    0,  620,  620,
    0,  620,    0,  620,    0,  620,    0,  620,  620,    0,
    0,    0,    0,    0,  620,    0,    0,    0,    0,    0,
    0,  620,  620,    0,    0,    0,  620,    0,    0,    0,
  620,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,  620,    0,  620,    0,    0,    0,  620,  620,
    0,    0,    0,    0,    0,    0,  620,  620,    0,    0,
  620,    0,  620,  620,    0,  620,    0,  620,  620,    0,
  620,    0,  620,    0,    0,  620,    0,  620,    0,  620,
    0,  620,    0,    0,    0,    0,    0,  620,  620,    0,
    0,    0,    0,    0, 1125,  620,  620,    0,    0,    0,
  620,    0,    0,    0,  620,  161,    0,  161,    0,    0,
  161,    0,    0,    0,    0,  161,  620,    0,  620,  161,
    0,    0,  620,  620,    0,    0,    0,    0,  161,    0,
  620,  620,    0,    0,  620,  161,    0,  620,    0,    0,
  161,    0,  620,    0,  161,    0,  161,    0,  161,    0,
    0,    0,    0,  161,    0,    0,  161,    0,  161,    0,
    0,    0,  161,    0,    0,  161,    0,    0,    0,    0,
  161,  161,    0,  620,  161,    0,    0,  161,    0,    0,
  161,  161,  161,    0,    0,  161,    0,    0,    0,    0,
  161,    0,    0,    0,  161,    0,    0,    0,    0,    0,
    0,    0,    0,  161,    0,  161,  160,    0,    0,    0,
  161,    0,    0,    0,    0,  161,    0,    0,    0,  161,
    0,  161,    0,  161,    0,   53,    0,   53,  161,    0,
   53,  161,    0,  161,    0,   53,    0,  161,    0,   53,
  161,    0,    0,    0,    0,  161,  161,  620,   53,  161,
    0,    0,  161,    0,    0,   53,  161,    0,    0,    0,
   53,    0,    0,    0,   53,    0,   53,    0,   53,    0,
    0,    0,    0,   53,    0,    0,   53,    0,   53,    0,
  161,    0,   53,  160,    0,   53,    0,  161,    0,    0,
   53,   53,    0,    0,   53,    0,    0,   53,    0,   83,
    0,   84,    0,    0,   85,    0,    0,    0,    0,   86,
    0,   87,    0,   88,    0,   89,    0,    0,    0,    0,
    0,   90,   91,    0,    0,    0,    0,    0,  158,   92,
    0,   53,    0,   53,   93,    0,   53,    0,   94,    0,
    0,   53,    0,    0,    0,   53,    0,    0,    0,    0,
   95,    0,   96,    0,   53,    0,   97,    0,    0,    0,
    0,   53,  161,    0,   98,   99,   53,    0,  100,    0,
   53,  101,   53,    0,   53,   83,  102,   84,    0,   53,
   85,    0,   53,    0,   53,   86,    0,    0,   53,   88,
    0,   53,    0,    0,    0,    0,   53,   53,   91,   83,
   53,   84,    0,   53,   85,   92,    0,   53,    0,   86,
   93,    0,    0,   88,   94,    0,    0,    0,    0,    0,
    0,    0,   91,    0,    0,    0,   95,    0,   96,   92,
    0,    0,   97,    0,   93,    0,    0,    0,   94,    0,
   98,   99,    0,   83,  100,   84,    0,  117,   85,    0,
   95,    0,   96,   86,    0,    0,   97,   88,    0,    0,
    0,    0,    0,    0,   98,   99,   91,    0,  100,    0,
    0,  117,    0,   92,    0,   83,    0,   84,   93,    0,
   85,   74,   94,    0,    0,   86,    0,    0,    0,   88,
    0,    0,    0,    0,   95,    0,   96,    0,   91,    0,
   97,    0,    0,    0,    0,   92,    0,    0,   98,   99,
   93,    0,  100,   53,   94,  117,    0,    0,    0,  179,
    0,  179,    0,    0,  179,    0,   95,    0,   96,  179,
    0,    0,   97,  179,    0,    0,    0,    0,    0,    0,
   98,   99,  179,    0,  100,    0,    0,  117,    0,  179,
    0,    0,    0,    0,  179,    0,    0,  260,  179,    0,
    0,    0,    0,  188,    0,  188,    0,    0,  188,    0,
  179,    0,  179,  188,    0,    0,  179,  188,    0,    0,
    0,  659,    0,    0,  179,  179,  188,  180,  179,  180,
    0,  179,  180,  188,  526,    0,    0,  180,  188,    0,
  526,  180,  188,    0,    0,    0,    0,    0,    0,    0,
  180,    0,    0,    0,  188,    0,  188,  180,    0,    0,
  188,    0,  180,    0,    0,  695,  180,    0,  188,  188,
    0,    0,  188,    0,    0,  188,    0,  526,  180,    0,
  180,    0,    0,    0,  180,    0,    0,    0,    0,    0,
    0,    0,  180,  180,    0,    0,  180,  697,    0,  180,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,  526,    0,
    0,    0,    0,    0,  526,  526,  526,  526,  526,  526,
  526,  526,  526,  526,  526,  526,  542,    0,    0,    0,
    0,  179,  542,  526,    0,  526,    0,  526,    0,  526,
  526,  526,    0,  526,  526,    0,  526,  526,    0,  526,
    0,  526,  526,  526,  526,  526,  526,  526,    0,    0,
    0,    0,    0,    0,  526,    0,  526,    0,  526,  542,
  526,    0,  526,    0,  526,  188,  526,    0,  526,    0,
  526,    0,  526,    0,  526,    0,  526,    0,  526,    0,
  526,    0,  526,    0,  526,    0,  526,  546,  526,  180,
    0,    0,  526,  546,    0,    0,    0,    0,    0,    0,
  542,    0,    0,    0,    0,    0,  542,  542,  542,  542,
  542,  542,  542,  542,  542,  542,  542,  542,    0,    0,
    0,    0,    0,    0,    0,  542,  542,  542,    0,  542,
  546,  542,  542,  542,    0,  542,  542,    0,    0,  542,
    0,  542,    0,  542,  542,  542,  542,  542,  542,  542,
    0,    0,    0,    0,    0,    0,  542,    0,  542,    0,
  542,    0,  542,    0,  542,    0,  542,    0,  542,    0,
  542,  546,    0,    0,    0,    0,    0,  546,  546,  546,
  546,  546,  546,  546,  546,  546,  546,  546,  546,  547,
    0,    0,    0,    0,  542,  547,  546,  546,  546,    0,
  546,    0,  546,  546,  546,    0,  546,  546,    0,    0,
  546,    0,  546,    0,  546,  546,    0,    0,    0,  546,
  546,    0,    0,    0,    0,    0,    0,  546,    0,  546,
    0,  546,  547,  546,    0,  546,    0,  546,    0,  546,
    0,  546,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
  548,    0,    0,    0,    0,  546,  548,    0,    0,    0,
    0,    0,    0,  547,    0,    0,    0,    0,    0,  547,
  547,  547,  547,  547,  547,  547,  547,  547,  547,  547,
  547,    0,    0,    0,    0,    0,    0,    0,  547,  547,
  547,    0,  547,  548,  547,  547,  547,    0,  547,  547,
    0,    0,  547,    0,  547,    0,  547,  547,    0,    0,
    0,  547,  547,    0,    0,    0,    0,    0,    0,  547,
    0,  547,    0,  547,    0,  547,    0,  547,    0,  547,
    0,  547,    0,  547,  548,    0,    0,    0,    0,    0,
  548,  548,  548,  548,  548,  548,  548,  548,  548,  548,
  548,  548,  549,    0,    0,    0,    0,  547,  549,  548,
  548,  548,    0,  548,    0,  548,  548,  548,    0,  548,
  548,    0,    0,  548,    0,  548,    0,  548,  548,    0,
    0,    0,  548,  548,    0,    0,    0,    0,    0,    0,
  548,    0,  548,    0,  548,  549,  548,    0,  548,    0,
  548,    0,  548,    0,  548,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,  550,    0,    0,    0,    0,  548,  550,
    0,    0,    0,    0,    0,    0,  549,    0,    0,    0,
    0,    0,  549,  549,  549,  549,  549,  549,  549,  549,
  549,  549,  549,  549,    0,    0,    0,    0,    0,    0,
    0,  549,  549,  549,    0,  549,  550,  549,  549,  549,
    0,    0,    0,    0,    0,  549,    0,  549,    0,  549,
  549,  549,    0,    0,  549,  549,    0,    0,    0,    0,
    0,    0,  549,    0,  549,    0,  549,    0,  549,  551,
  549,    0,  549,    0,  549,  551,  549,  550,    0,    0,
    0,    0,    0,  550,  550,  550,  550,  550,  550,  550,
  550,  550,  550,  550,  550,    0,    0,    0,    0,    0,
  549,    0,  550,  550,  550,    0,  550,    0,  550,  550,
  550,    0,  551,    0,    0,    0,  550,    0,  550,    0,
  550,  550,  550,    0,    0,  550,  550,    0,    0,    0,
    0,    0,    0,  550,    0,  550,    0,  550,    0,  550,
  555,  550,    0,  550,    0,  550,  555,  550,    0,    0,
    0,    0,    0,  551,    0,    0,    0,    0,    0,  551,
  551,  551,  551,  551,  551,  551,  551,  551,  551,  551,
  551,  550,    0,    0,    0,    0,    0,    0,  551,  551,
  551,    0,  551,  555,  551,  551,  551,    0,    0,    0,
    0,    0,  551,    0,  551,    0,  551,  551,  551,    0,
    0,  551,  551,    0,    0,    0,    0,    0,    0,  551,
    0,  551,    0,  551,    0,  551,  556,  551,    0,  551,
    0,  551,  556,  551,  555,    0,    0,    0,    0,    0,
  555,  555,  555,  555,  555,  555,  555,  555,  555,  555,
  555,  555,    0,    0,    0,    0,    0,  551,    0,  555,
  555,  555,    0,  555,    0,  555,  555,  555,    0,  556,
    0,    0,    0,  555,    0,  555,    0,  555,  555,  555,
    0,    0,  555,  555,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,  555,    0,  555,  557,  555,    0,
  555,    0,  555,  557,  555,    0,    0,    0,    0,    0,
  556,    0,    0,    0,    0,    0,  556,  556,  556,  556,
  556,  556,  556,  556,  556,  556,  556,  556,  555,    0,
    0,    0,    0,    0,    0,  556,  556,  556,    0,  556,
  557,  556,  556,  556,    0,    0,    0,    0,    0,  556,
    0,  556,    0,  556,  556,  556,    0,    0,  556,  556,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
  556,    0,  556,  558,  556,    0,  556,    0,  556,  558,
  556,  557,    0,    0,    0,    0,    0,  557,  557,  557,
  557,  557,  557,  557,  557,  557,  557,  557,  557,    0,
    0,    0,    0,    0,  556,    0,  557,  557,  557,    0,
  557,    0,  557,  557,  557,    0,  558,    0,    0,    0,
  557,    0,  557,    0,  557,  557,  557,    0,    0,  557,
  557,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,  557,    0,  557,  559,  557,    0,  557,    0,  557,
  559,  557,    0,    0,    0,    0,    0,  558,    0,    0,
    0,    0,    0,  558,  558,  558,  558,  558,  558,  558,
  558,  558,  558,  558,  558,  557,    0,    0,    0,    0,
    0,    0,  558,  558,  558,    0,  558,  559,  558,  558,
  558,    0,    0,    0,    0,    0,  558,    0,  558,    0,
  558,  558,  558,    0,    0,  558,  558,    0,    0,    0,
    0,    0,    0,    0,    0,  564,    0,  558,    0,  558,
    0,  558,    0,  558,    0,  558,    0,  558,  559,    0,
    0,    0,    0,    0,  559,  559,  559,  559,  559,  559,
  559,  559,  559,  559,  559,  559,    0,    0,    0,    0,
    0,  558,    0,  559,  559,  559,    0,  559,    0,  559,
  559,  559,    0,    0,    0,    0,    0,  559,    0,  559,
    0,  559,  559,  559,    0,    0,  559,  559,    0,    0,
    0,    0,    0,    0,    0,    0,  565,    0,  559,    0,
  559,    0,  559,    0,  559,    0,  559,    0,  559,  564,
    0,    0,    0,    0,    0,  564,  564,  564,  564,  564,
  564,  564,  564,  564,  564,  564,  564,    0,    0,    0,
    0,    0,  559,    0,  564,  564,  564,    0,  564,    0,
  564,  564,  564,    0,    0,    0,    0,    0,    0,    0,
    0,    0,  564,  564,  564,    0,    0,  564,  564,    0,
    0,    0,    0,    0,    0,    0,    0,  566,    0,    0,
    0,    0,    0,  564,    0,  564,    0,  564,    0,  564,
  565,    0,    0,    0,    0,    0,  565,  565,  565,  565,
  565,  565,  565,  565,  565,  565,  565,  565,    0,    0,
    0,    0,    0,  564,    0,  565,  565,  565,    0,  565,
    0,  565,  565,  565,    0,    0,    0,    0,    0,    0,
    0,    0,    0,  565,  565,  565,    0,    0,  565,  565,
    0,    0,    0,    0,    0,    0,    0,    0,  567,    0,
    0,    0,    0,    0,  565,    0,  565,    0,  565,    0,
  565,  566,    0,    0,    0,    0,    0,  566,  566,  566,
  566,  566,  566,  566,  566,  566,  566,  566,  566,    0,
    0,    0,    0,    0,  565,    0,  566,  566,  566,    0,
  566,    0,  566,  566,  566,  568,    0,    0,    0,    0,
    0,    0,    0,    0,  566,  566,  566,    0,    0,  566,
  566,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,  566,    0,  566,    0,  566,
    0,  566,  567,    0,    0,    0,    0,    0,  567,  567,
  567,  567,  567,  567,  567,  567,  567,  567,  567,  567,
    0,    0,    0,    0,    0,  566,    0,  567,  567,  567,
    0,  567,    0,  567,  567,  567,  569,    0,    0,    0,
    0,    0,    0,    0,    0,  567,  567,  567,    0,  568,
  567,  567,    0,    0,    0,  568,  568,  568,  568,  568,
  568,  568,  568,  568,  568,  568,  568,    0,    0,    0,
  567,    0,  567,    0,  568,  568,  568,    0,  568,    0,
  568,  568,  568,  570,    0,    0,    0,    0,    0,    0,
    0,    0,  568,  568,  568,    0,  567,  568,  568,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,  568,    0,  568,
  569,    0,    0,    0,    0,    0,  569,  569,  569,  569,
  569,  569,  569,  569,  569,  569,  569,  569,    0,    0,
    0,    0,    0,  568,    0,  569,  569,  569,    0,  569,
    0,  569,  569,  569,  571,    0,    0,    0,    0,    0,
    0,    0,    0,    0,  569,  569,    0,  570,  569,  569,
    0,    0,    0,  570,  570,  570,  570,  570,  570,  570,
  570,  570,  570,  570,  570,    0,    0,    0,  569,    0,
  569,    0,  570,  570,  570,    0,  570,    0,  570,  570,
  570,  572,    0,    0,    0,    0,    0,    0,    0,    0,
    0,  570,  570,    0,  569,  570,  570,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,  570,    0,  570,  571,    0,
    0,    0,    0,    0,  571,  571,  571,  571,  571,  571,
  571,  571,  571,  571,  571,  571,    0,    0,    0,    0,
    0,  570,    0,  571,  571,  571,    0,  571,    0,  571,
  571,  571,  573,    0,    0,    0,    0,    0,    0,    0,
    0,    0,  571,  571,    0,  572,    0,  571,    0,    0,
    0,  572,  572,  572,  572,  572,  572,  572,  572,  572,
  572,  572,  572,    0,    0,    0,  571,    0,  571,    0,
  572,  572,  572,    0,  572,    0,  572,  572,  572,  574,
    0,    0,    0,    0,    0,    0,    0,    0,    0,  572,
  572,    0,  571,    0,  572,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,  572,    0,  572,  573,    0,    0,    0,
    0,    0,  573,  573,  573,  573,  573,  573,  573,  573,
  573,  573,  573,  573,    0,    0,    0,    0,    0,  572,
    0,  573,  573,  573,    0,  573,    0,  573,  573,  573,
  576,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,  573,    0,  574,    0,  573,    0,    0,    0,  574,
  574,  574,  574,  574,  574,  574,  574,  574,  574,  574,
  574,    0,    0,    0,  573,    0,  573,  546,  574,  574,
  574,    0,  574,  546,  574,  574,  574,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,  574,    0,
  573,    0,  574,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
  546,  574,    0,  574,  576,    0,  352,    0,    0,    0,
  576,  576,  576,  576,  576,  576,  576,  576,  576,  576,
  576,  576,    0,    0,    0,    0,    0,  574,    0,  576,
  576,  576,    0,  576,    0,  576,  576,  576,    0,    0,
    0,    0,    0,  352,    0,    0,    0,    0,    0,  576,
    0,    0,    0,  576,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,  546,    0,    0,
  546,    0,  546,    0,  576,    0,  546,  546,    0,  352,
  546,    0,  546,    0,  546,  546,    0,    0,    0,  546,
  546,    0,    0,    0,    0,    0,    0,  546,  576,  546,
    0,  546,    0,  546,    0,  546,    0,  546,    0,  546,
  352,  546,  352,  352,  352,  352,  352,    0,    0,  352,
  352,    0,    0,  352,    0,  352,    0,  352,  352,  352,
  352,  352,  352,  352,    0,  546,  352,    0,  352,    0,
  352,    0,  352,    0,  352,    0,  352,    0,  352,    0,
  352,    0,  352,    0,  352,    0,  352,    0,  352,    0,
  352,    0,  352,    0,  352,    0,  352,    0,  352,    0,
  352,    0,  352,    0,  352,    0,  352,    0,  352,    0,
    0,    0,  352,  352,    0,  352,    0,  352,  352,    0,
    0,    0,  352,  352,    0,    0,  352,    0,  352,    0,
  352,  352,  352,  352,  352,  352,  352,    0,  388,  352,
    0,  352,    0,  352,    0,  352,    0,  352,    0,  352,
    0,  352,    0,  352,    0,  352,    0,  352,    0,    0,
    0,    0,  388,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,   53,    0,  388,    0,    0,    0,  352,
  388,  352,    0,  388,    0,  388,    0,  388,  388,  388,
  388,    0,    0,    0,    0,  388,    0,   53,    0,  388,
    0,    0,    0,  388,    0,    0,    0,    0,   53,    0,
   53,  388,    0,    0,  388,   53,  388,    0,    0,    0,
   53,    0,   53,   53,   53,   53,    0,    0,    0,    0,
   53,    0,   53,    0,   53,    0,    0,    0,   53,    0,
   53,    0,    0,  388,    0,   53,   53,    0,    0,   53,
   53,   53,    0,    0,    0,   53,    0,   53,   53,   53,
   53,    0,   53,   53,    0,   53,  388,    0,   47,   53,
 1310,   53,    0,   53,    0,   53,    0,  198,    0,    0,
   53,   53,    0,    0,   53,   53,   53,   53,   53,   53,
   53,    0,   48,    0,    0,   53,    0,    0,   47,   53,
    0,    0,    0,    0,    0,   49,    0,    0,    0,    0,
   51,   53,  308,    0,   53,   52,   53,   53,   54,   55,
   56,    0,   48, 1311,    0,   57,    0,    0,   47,   58,
    0,    0,    0,    0,    0,   49,    0,    0,    0,   50,
   51,   59,  309,    0,   60,   52,   61,   53,   54,   55,
   56,    0,   48,    0,    0,   57,    0,    0,   47,   58,
 1286,    0,    0,    0,    0,   49,    0,    0,    0,    0,
   51,   59,    0,    0,   60,   52,   61,   53,   54,   55,
   56,    0,   48, 1287,    0,   57,    0,    0,   47,   58,
    0,    0,    0,    0,    0,   49,    0,    0,    0,    0,
   51,   59,    0,    0,   60,   52,   61,   53,   54,   55,
   56,    0,   48,    0,    0,   57,    0,    0,   47,   58,
 1310,    0,    0,    0,    0,   49,    0,    0,    0,    0,
   51,   59,    0,    0,   60,   52,   61,   53,   54,   55,
   56,    0,   48, 1311,    0,   57,    0,    0,   47,   58,
    0,    0,    0,    0,    0,   49,    0,    0,    0,    0,
   51,   59,    0,    0,   60,   52,   61,   53,   54,   55,
   56,    0,   48,    0,    0,   57,    0,    0,   47,   58,
    0,    0,    0,    0,    0,   49,    0,    0,    0,    0,
   51,   59,    0,    0,   60,   52,   61,   53,   54,   55,
   56,    0, 1086,    0,    0,   57,    0,    0,    0,   58,
    0,    0,    0,    0,    0,   49,    0,    0,    0,    0,
   51,   59,    0,    0,   60,   52,   61,   53,   54,   55,
   56,    0,    0,    0,    0,   57,    0,    0,    0,   58,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0, 1087,    0,    0,   60,    0,   61,
  };
  protected static  short [] yyCheck = {            63,
    0,   62,  105,   66,  101,  236,    6,   75,   69,  236,
  236,    3,    4,  122,  123,  403,  254,  455,  236,  714,
  256,  256,   25,  556,  254,  598,   87,  453,  103,   90,
  593,  106,  131,   79,  297,  608,  254,  458,  829,  830,
  573,  574,  217,  503,  503,  256,   38,  580,  581,  256,
  256,  140,  844,  256,  269,  553,  833,  118,   50, 1040,
  256,  256,  269,  855,  262,  402,  256,  101,  167,    0,
  131,  295,  283,  631, 1055,    6,   68,  269,  139,  140,
  256,  448,  256,  326,   76,  277,  423,  366,  213,  307,
  326,  256,  711,  256,  140,  156,  157,  365,  256,  256,
  256,  299, 1010,  385,  315, 1010, 1203,  256,  389,  101,
  682,  340,  903,  105,  256,  118,  818,  342,  909,  910,
  256,  366,  364,  256,  370,  365,  365,  373,  919,  410,
  366,  371,  371,  378,  233,  364,  257,  270,   69,  931,
  221,  366,  366,  285,  936,  937,  256,  389,  378,  385,
  365,  232,  326,  434,  287, 1063,  255,  306, 1063,  234,
  378,  385,  380,  366,  263,  256,  448,  231,  256,  448,
  366,  366,  403,  256,  370,  236,  403,  403,  239,  369,
  448,  379,  385,  381,  359,  403,  256,  979,  222,  264,
  385,  454,  366,  985,  256,  371,  257,  231,  817,  245,
  902,  247,  400,  366,  402,  448,  371,  765,  366,  767,
  366,  709,  448,  259,  371,  389,  256,  278,  995,  679,
  641,  456,  385,  256,  448,  286,  798,  385,  365,  385,
  222,  277,  321,  322, 1331,  371,  361,  448,  363,  231,
  365,  456,  448, 1020,  365,  448,  292,  293,  585,  456,
 1231,  297,  298,  448,  256,  370,  448,  448,  373,  369,
 1010,  371,  835,  344,  310,  311,  312,  313,  314,  315,
  316,  317,  318,  319,  320,  366,  364,  840,  366,  360,
  368,  267,  257, 1075,  345,  448,  369,  295,  371,  370,
  448,  337,  353,  339,  256, 1008,  366,  385,  371,  257,
  381,  382,  383,  402,  366,  351,  352,  369,  239,  382,
  285,  448,  393, 1063,  377,  775,  775,  378,  278,  380,
 1112,  295,  340,  385,  364,  422, 1070,  390,  391,  315,
  370,  440, 1045,  366, 1010,  368,  366,  366,  295,  368,
 1008,  387,  403,  389,  306,  364,  364,  278,  447,  368,
  368,  370,  385,  926,  372,  781,  385, 1070,  344, 1054,
  380,  336,  364,  784, 1008, 1109,  373,  371,  370,  371,
  368,  373,  380,  761, 1069,  593, 1071, 1045,  382, 1010,
  443,  456,  390,  646,  366, 1010,  372, 1063,  606,  369,
  499,  366,  369,  353,  380,  570, 1109,  506,  366,  445,
  461, 1045, 1070,  385,  390,  504,  380,  645,  454,  455,
  378,  474,  458,  643,  503,  645,  390,  463,  448,  480,
  449,  385,  353,  380,  380,  643, 1070,  645,  256,  366,
  449,  449, 1063,  390,  390,  455,  380,  265, 1063, 1070,
  366, 1109,  503,  364,  366,  491,  492,  368,  370,  370,
  371,  449,  373, 1016,  364,  364,  370,  503,  368,  385,
  370,  570,  523,  524,  373, 1109,  448,  389,  261,  461,
  450,  451,  452,  453,  451,  452,  453,  366, 1109,  540,
  541,  370,  448,  366,  448,  366,  585,  366,  369,  370,
  371, 1282,  285,  539, 1285,  366,  385,  543,  326,  455,
  389, 1070,  385,  564,  385,  298,  385,  368,  389,  370,
  303,  455,  371,  559,  385,  308,  615,  310,  311,  312,
  313,  368,  448,  370,  273,  318,  448,  624,  449,  322,
  761,  592,  593,  594,  761,  761,  366,  366,  593,  449,
 1109,  334,  605,  761,  337,  606,  339,  372,  297,  595,
  368,  606,  598,  599,  261,  385,  385,  366, 1070,  448,
  606,  366,  608,  366,  610,  448,  366,  448,  370,  448,
  370, 1070,  368,  366,  370,  324,  385,  448,  285,  373,
  385,  627,  385,  273,  273,  660,  685,  389,  449,  657,
  274,  298,  385,  340,  278,  641,  303, 1109,  282,  448,
  646,  308,  449,  310,  311,  312,  313,  297,  297,  366,
 1109,  318,  369, 1070,  261,  322,   62,  364,  448,  448,
  666, 1070,  364,  340,  670,  448,  844,  334,  385,  632,
  337,  373,  339,  679,  324,  324,  682,  855,  285,  448,
 1173,   87,  877,  448,   90,  448,  692,  364,  448,  340,
  342,  298, 1109,  449,  371,  448,  303,  703,  704,  343,
 1109,  308,  365,  310,  311,  312,  313,  367,  371,  750,
  344,  318,  118,  364,  366,  322, 1233, 1234, 1211,  344,
  371, 1214,  373,  782, 1241,  131,  344,  334,  751,  365,
  337, 1212,  339,  139, 1251,  371,    6,  340,    8,  380,
  761,  747,  378,  448,  378,  766,  380,  340,  769,  390,
  156,  157,  359,  931,  361,  380,  390, 1250,  936,  937,
  378,  364,  380, 1244, 1245,  390, 1247, 1115,  371,  775,
  793,  364,  390,  779,   44,  364,  338,  396,  784,  398,
  373,  448,  365,  366,  373,  368, 1303,  370,  371,  366,
  364,  366,  798,  366,  369,  261,  802,  263,  804,  366,
  806,  979,  369,  809,  389,  826,  380,  985,  385,  392,
  385,  394,  385,  882,  883,  380,  390,  823,  385,  285,
  404,  370,  406,  844,  366,  390,  847,  833,  851,  835,
 1228, 1229,  298,  385,  855,  841,  378,  303,  380,  366,
  389,  368,  308,  370,  310,  311,  312,  313,  390,  432,
  316,  257,  318,  371,  380,  364,  322,  261,  818,  368,
  820,  367,  389,  366,  382,  392,  369,  394,  334,  365,
  366,  337,  368,  339,  370,  371,  366,  368,  906,  369,
  286,  285,  385,  889,  307,  891,  309,  368, 1079,  371,
  896,  314, 1079, 1079,  298,  385,  392, 1075,  394,  303,
  382, 1079,  306,  326,  308,  432,  310,  311,  312,  313,
  931,  934,  307,  368,  318,  936,  937,  923,  322,  314,
  926,  270,  326,  929, 1115,  385,  386,  387, 1115, 1115,
  334,  326,  369,  337, 1112,  339,  432, 1115,  287,  345,
  366,  340,  902,  369,  370,  370,  952,  346,  347,  970,
  956,  350,  351,  366,  353,  354,  371,  366,  979,  385,
  369,  370,  366,  389,  985,  378,  378,  380,  380,  266,
  256,  268,  371,  371,  271,  371,  385,  390,  390,  276,
  266,  385,  268,  280,  382,  271, 1007,  364,  380,  995,
  276,  364,  289,  999,  280,  378,  373,  380,  390,  296,
  373,  378,  364,  289,  301,  965,  388,  390,  305,  364,
  296,  373, 1203,  383, 1020,  301, 1203, 1203,  373,  305,
  317,  364,  319,  364, 1030, 1203,  323, 1033,  375,  376,
  373,  317,  373,  319,  331,  332,  384,  323,  335,  367,
  368,  338,  408,  371,  448,  331,  332,  364, 1008,  335,
 1010,  368,  338,  365, 1075,  461,  385,  369, 1079,  371,
  389,  373,  372, 1126, 1085,  364,  369, 1088,  371,  366,
  373,  350,  351, 1094,  480,  367, 1082,  369,  364,  371,
  366,  448,  371,  385,  373, 1045, 1143,  389,  366,  370,
  368, 1112,  370,  367, 1115,  369,  371,  371,  340,  256,
 1121, 1122,  367, 1063,  346,  347,  371, 1148,  350,  351,
 1070,  353,  354, 1134,  392,  367,  394,  523,  524,  371,
  371, 1162,  373,  262, 1145, 1166, 1147,  256,  367, 1152,
 1171, 1172,  371,  367,  540,  541,  364,  371,  369,  369,
 1331,    0,  373,  373, 1331, 1331,  779,  350,  351, 1109,
  783,  448,  448, 1331,  432, 1196,  366,  371,  564,  373,
  299,  369,  448,  371, 1205, 1206,  340,  327,  328,  329,
  330,  368,  346,  347, 1126,  448,  350,  351,  371,  353,
  354,  369, 1203, 1135, 1136,  371,  592,  373,  594,  355,
  356, 1143,  141,  142,  143,  144,  145,  146,  147,  148,
  371,  340,  373, 1266,  385,  386,  387,  346,  347,  348,
  349,  350,  351,  352,  353,  354,  355,  356,  357,  355,
  356,  256, 1228, 1229,  350,  351,  365, 1290,  367, 1292,
  369,  448,  371,  372,  373,  359, 1259,  361,  454,  367,
  379,  369,  381,  448,  383,  384,  340, 1207,  448,  388,
  389,  373,  346,  347,  725,  726,  350,  351,  366,  353,
  354,  400,  380,  402, 1288,  404,  367,  406,  369,  408,
  367,  410,  369, 1233, 1234,  448, 1299, 1300,  366,  448,
  368, 1241,  370, 1243, 1307,    0, 1309,  323,  324, 1313,
 1314, 1251,  371,  448,  373,  434,  369,  385,  371,  367,
 1260,  389,  331,  332,  392,  340,  394,  371, 1268, 1269,
 1331,  346,  347,  348,  349,  350,  351,  352,  353,  354,
  355,  356,  357,  367,  368,  373,  370,  371,  372,  368,
  365,  366,  367,  373,  369,  340,  371,  372,  373,  364,
  368,  346,  347, 1303,  432,  294,  295,  296,  353,  354,
  385,  367,  367,  369,  369,  359,  387,  361,  389,  368,
  766,  385,  368,  769,  365,  373,  373,  369,  295,  369,
  373,  344,  321,  322,  323,  324,  368,  372,  327,  328,
  329,  330,  331,  332,  333,  334,  335,  336,    0,  338,
  369,  448,  364,  448,  380,  389,  367,  448,  257,  341,
  378,  256,  261,  373,  373,  373,  373,  266,  373,  268,
  371,  369,  271,  365,  273,  274,  371,  276,  371,  278,
  826,  280,  369,  282,  283,  284,  285,  371,  371,  288,
  289,  371,  369,  371,  370,  294,  369,  296,  297,  298,
  357,  847,  301,  302,  303,  285,  305,  369,  336,  308,
  369,  310,  311,  312,  313,  367,  369,  372,  317,  318,
  319,  369,  369,  322,  323,  324,  371,  378,  368,    0,
  373,  373,  331,  332,  371,  334,  335,  336,  337,  338,
  339,  373,  369,  369,  343,  340,  369,  369,  369,  378,
  389,  346,  347,  348,  349,  350,  351,  352,  353,  354,
  355,  356,  357,  256,  453,  256,  365,  366,  364,  448,
  365,  366,  367,  365,  369,  374,  371,  372,  373,  448,
  448,  470,  295,  412,  473,  414,  295,  416,  352,  418,
  385,  420,  448,  422,  389,  424,  365,  426,  340,  428,
  378,  430,  257,  295,  369,  371,  261,  369,  371,  455,
  348,  266,  371,  268,  448,  410,  271,  371,  273,  367,
  371,  276,  369,  278,  970,  280,  369,  282,  448,  448,
  285,  281,  364,  288,  289,  256,  256,  348,  365,  434,
  364,  296,  297,  298,  372,  364,  301,  302,  303,  448,
  305,  373,  364,  308,  369,  310,  311,  312,  313,  373,
  367, 1007,  317,  318,  319,  369,  349,  322,  323,  324,
  373,  365,  378,  349,  371,  357,  331,  332,  368,  334,
  335,  336,  337,  338,  339,  365,  340,  365,  343,  374,
  369,  372,  346,  347,  369,  365,  350,  351,  448,  353,
  354,  372,  364,  366,  370,  257,  366,  448,  364,  261,
  365,  366,  369,  365,  266,  368,  268,  371,  367,  271,
  366,  273,  369,  365,  276,  369,  278,  367,  280,  365,
  282,  369,  369,  285,  369,  364,  288,  289,  369, 1085,
  367,  369, 1088,  369,  296,  297,  298,  256, 1094,  301,
  302,  303,  256,  305,  365,  365,  308,  365,  310,  311,
  312,  313,  365,  365,    0,  317,  318,  319,  373,  367,
  322,  323,  324,  367,  369, 1121, 1122,  364,  364,  331,
  332,  369,  334,  335,  369,  337,  338,  339, 1134,  369,
  261,  343,  364,  448,  367,  266,  365,  268,  369, 1145,
  271, 1147,  273,  448,  369,  276,  367,  278,  367,  280,
  373,  282,  365,  369,  448,  365,  364,  288,  289,  373,
  369,  365,  365,  448,  369,  296,  297,  298,  369,  371,
  301,  302,  303,  373,  305,  373,  364,  308,  365,  310,
  311,  312,  313,  364,  448,  365,  317,  318,  319,  373,
  373,  322,  323,  324,  369,  369,  369,  365,  364,  364,
  331,  332,  364,  334,  335,    6,  337,  338,  339,    6,
   38,   76,  343,  292, 1109,  256,  293, 1142, 1045,  356,
  834, 1224, 1300,  560,  265,  266,  267,  268,  269, 1323,
  271,  272,  781,  274,  275,  276,  448,  278,  279,  280,
  281,  771,  765, 1063, 1260,  286,  771,  288,  289,  290,
  291,  292,  293,  771, 1268,  296,  965,  239, 1269,  300,
  301,  265,  303,  304,  305,  492,  306,  781,  645,  305,
  307,  524,  782,  333,  315,  334,  317,  335,  319,  320,
  336,  338,  323,  480,  325,  326,  327,  328,  329,  330,
  331,  332,  333,  334,  335,  336,  915,  338,  793, 1079,
  341, 1085,  378, 1023, 1025,  346, 1016,  752,  266,  958,
  268,  547,  925,  271, 1118,  273,   -1,  448,  276,  956,
   -1,   -1,  280,  364,  365,  283,   -1,  368,   -1,   -1,
   -1,  289,  373,  374,  375,  376,  377,   -1,  296,  297,
   -1,   -1,  383,  301,  385,   -1,   -1,  305,   -1,   -1,
   -1,  392,   -1,  394,   -1,   -1,   -1,   -1,   -1,  317,
   -1,  319,   -1,   -1,   -1,  323,  324,   -1,   -1,   -1,
   -1,   -1,   -1,  331,  332,   -1,   -1,  335,   -1,   -1,
  338,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,  436,   -1,  438,   -1,  440,
   -1,  442,   -1,  444,   -1,  446,   -1,  448,  449,  256,
   -1,   -1,   -1,  454,   -1,  456,   -1,   -1,  265,  266,
  267,  268,  269,   -1,  271,  272,   -1,  274,  275,  276,
   -1,  278,  279,  280,   -1,   -1,   -1,   -1,   -1,  286,
   -1,  288,  289,  290,  291,  292,  293,   -1,   -1,  296,
   -1,   -1,   -1,  300,  301,   -1,  303,  304,  305,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,  315,   -1,
  317,   -1,  319,  320,   -1,   -1,  323,   -1,  325,  326,
  327,  328,  329,  330,  331,  332,  333,  334,  335,  336,
  448,  338,   -1,   -1,  341,   -1,   -1,   -1,   -1,  346,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,  266,
   -1,  268,   -1,   -1,  271,   -1,   -1,  364,  365,  276,
   -1,  368,   -1,  280,   -1,   -1,  373,  374,  375,  376,
  377,   -1,  289,   -1,   -1,   -1,  383,   -1,  385,  296,
   -1,   -1,   -1,   -1,  301,  392,   -1,  394,  305,   -1,
  307,   -1,  309,   -1,   -1,   -1,   -1,  314,   -1,   -1,
  317,   -1,  319,   -1,   -1,   -1,  323,   -1,   -1,  326,
   -1,   -1,   -1,   -1,  331,  332,   -1,   -1,  335,   -1,
   -1,  338,   -1,   -1,   -1,   -1,   -1,   -1,   -1,  436,
   -1,  438,   -1,  440,   -1,  442,   -1,  444,   -1,  446,
   -1,  448,  449,  256,   -1,   -1,   -1,  454,   -1,  456,
  367,   -1,  265,  266,  267,  268,   -1,   -1,  271,  272,
   -1,  274,  275,  276,   -1,  278,  279,  280,   -1,   -1,
   -1,   -1,   -1,  286,   -1,  288,  289,  290,  291,  292,
  293,   -1,   -1,  296,   -1,   -1,   -1,  300,  301,   -1,
  303,  304,  305,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,  315,   -1,  317,   -1,  319,  320,   -1,   -1,
  323,   -1,  325,  326,  327,  328,  329,  330,  331,  332,
  333,  334,  335,  336,   -1,  338,  256,   -1,  341,   -1,
   -1,  448,   -1,  346,   -1,  265,  266,   -1,  268,   -1,
   -1,  271,  272,   -1,   -1,   -1,  276,   -1,  278,   -1,
  280,  364,   -1,   -1,   -1,  368,  286,   -1,   -1,  289,
  373,  374,  375,  376,  377,   -1,  296,   -1,  286,   -1,
  383,  301,  385,  303,  304,  305,   -1,   -1,   -1,  392,
   -1,  394,   -1,   -1,   -1,   -1,   -1,  317,   -1,  319,
  320,   -1,   -1,  323,   -1,   -1,  326,   -1,  328,   -1,
  330,  331,  332,  333,   -1,  335,   -1,   -1,  338,   -1,
  328,   -1,   -1,   -1,   -1,   -1,  346,   -1,   -1,   -1,
   -1,   -1,   -1,  436,   -1,  438,   -1,  440,   -1,  442,
   -1,  444,   -1,  446,   -1,  448,  449,   -1,  368,   -1,
   -1,  454,   -1,   -1,  374,  375,  376,  377,   -1,   -1,
   -1,   -1,   -1,  383,   -1,  385,  374,  375,  376,  377,
   -1,  379,  392,  381,  394,  383,  384,  385,  386,  387,
  388,   -1,   -1,   -1,  392,   -1,  394,   -1,  396,   -1,
  398,   -1,  400,   -1,  402,   -1,  404,  256,  406,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,  265,  266,   -1,  268,
   -1,   -1,  271,  272,   -1,   -1,  436,  276,  438,  278,
  440,  280,  442,   -1,  444,   -1,  446,  286,  448,  449,
  289,   -1,   -1,   -1,  454,   -1,   -1,  296,   -1,   -1,
   -1,   -1,  301,   -1,  303,  304,  305,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,  317,   -1,
  319,  320,   -1,   -1,  323,   -1,   -1,  326,   -1,  328,
   -1,  330,  331,  332,  333,   -1,  335,   -1,   -1,  338,
  256,   -1,   -1,   -1,   -1,   -1,   -1,  346,   -1,  265,
  266,   -1,  268,   -1,   -1,  271,  272,   -1,   -1,   -1,
  276,   -1,  278,   -1,  280,   -1,   -1,   -1,   -1,  368,
  286,   -1,   -1,  289,   -1,  374,  375,  376,  377,   -1,
  296,   -1,   -1,   -1,  383,  301,  385,  303,  304,  305,
   -1,   -1,   -1,  392,   -1,  394,   -1,   -1,   -1,   -1,
   -1,  317,   -1,  319,  320,   -1,   -1,  323,   -1,   -1,
  326,   -1,  328,   -1,  330,  331,  332,  333,   -1,  335,
   -1,   -1,  338,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,  436,   -1,  438,
   -1,  440,   -1,  442,   -1,  444,   -1,  446,   -1,  448,
  449,   -1,  368,  256,   -1,  454,   -1,   -1,  261,  262,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,  285,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,  295,   -1,   -1,  298,  299,   -1,   -1,   -1,
  303,   -1,   -1,  306,   -1,  308,   -1,  310,  311,  312,
  313,   -1,   -1,   -1,   -1,  318,   -1,   -1,   -1,  322,
  436,   -1,  438,  326,  440,   -1,  442,   -1,  444,   -1,
  446,  334,  448,   -1,  337,   -1,  339,  340,  454,   -1,
   -1,   -1,   -1,  346,  347,  348,  349,  350,  351,  352,
  353,  354,  355,  356,  357,   -1,   -1,   -1,   -1,   -1,
   -1,  364,  365,   -1,  367,  368,  369,  370,  371,  372,
  373,   -1,  375,  376,   -1,  378,  379,   -1,  381,  382,
  383,  384,  385,  386,  387,  388,  389,   -1,   -1,  392,
   -1,  394,   -1,  396,   -1,  398,   -1,  400,   -1,  402,
   -1,  404,   -1,  406,   -1,  408,   -1,  410,   -1,  412,
   -1,  414,   -1,  416,   -1,  418,   -1,  420,   -1,  422,
   -1,  424,   -1,  426,   -1,  428,   -1,  430,  256,  432,
   -1,  434,   -1,  261,  262,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,  448,  449,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,  285,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,  295,   -1,   -1,
  298,  299,   -1,   -1,   -1,  303,   -1,   -1,  306,   -1,
  308,   -1,  310,  311,  312,  313,   -1,   -1,   -1,   -1,
  318,   -1,   -1,   -1,  322,   -1,   -1,   -1,  326,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,  334,   -1,   -1,  337,
   -1,  339,  340,   -1,   -1,   -1,   -1,   -1,  346,  347,
  348,  349,  350,  351,  352,  353,  354,  355,  356,  357,
   -1,   -1,   -1,   -1,   -1,   -1,  364,  365,  366,  367,
  368,  369,  370,  371,  372,  373,   -1,   -1,   -1,   -1,
   -1,  379,   -1,  381,  382,  383,  384,  385,   -1,   -1,
  388,  389,  256,   -1,   -1,   -1,   -1,  261,  262,   -1,
   -1,   -1,  400,   -1,  402,   -1,  404,   -1,  406,   -1,
  408,   -1,  410,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,  285,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,  295,   -1,   -1,  298,  299,  434,   -1,   -1,  303,
   -1,   -1,   -1,   -1,  308,   -1,  310,  311,  312,  313,
  448,  449,   -1,   -1,  318,   -1,   -1,   -1,  322,   -1,
   -1,   -1,  326,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
  334,   -1,   -1,  337,   -1,  339,  340,   -1,   -1,   -1,
   -1,   -1,  346,  347,  348,  349,  350,  351,  352,  353,
  354,  355,  356,  357,   -1,   -1,   -1,   -1,   -1,   -1,
  364,  365,  366,  367,  368,  369,  370,  371,  372,  373,
   -1,   -1,   -1,   -1,   -1,  379,   -1,  381,  382,  383,
  384,  385,   -1,   -1,  388,  389,  256,   -1,   -1,   -1,
   -1,  261,  262,   -1,   -1,   -1,  400,   -1,  402,   -1,
  404,   -1,  406,   -1,  408,   -1,  410,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,  285,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,  295,   -1,   -1,  298,  299,
  434,   -1,   -1,  303,   -1,   -1,  306,   -1,  308,   -1,
  310,  311,  312,  313,  448,  449,   -1,   -1,  318,   -1,
   -1,   -1,  322,   -1,   -1,   -1,  326,  256,   -1,   -1,
   -1,   -1,   -1,   -1,  334,   -1,   -1,  337,   -1,  339,
  340,   -1,   -1,   -1,   -1,   -1,  346,  347,  348,  349,
  350,  351,  352,  353,  354,  355,  356,  357,   -1,   -1,
   -1,   -1,   -1,   -1,  364,  365,  366,  367,  368,  369,
  256,  371,  372,  373,   -1,   -1,  262,   -1,   -1,  379,
   -1,  381,  382,  383,  384,  385,   -1,   -1,  388,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
  400,   -1,  402,   -1,  404,   -1,  406,   -1,  408,  295,
  410,  340,   -1,  299,   -1,   -1,   -1,  346,  347,  348,
  349,  350,  351,  352,  353,  354,  355,  356,   -1,   -1,
   -1,   -1,   -1,   -1,  434,   -1,  365,  366,  367,   -1,
  369,   -1,  371,  372,  373,   -1,   -1,   -1,  448,  449,
   -1,   -1,   -1,   -1,  340,   -1,  385,   -1,   -1,   -1,
  346,  347,  348,  349,  350,  351,  352,  353,  354,  355,
  356,  357,   -1,   -1,   -1,   -1,   -1,   -1,  364,  365,
  366,  367,  368,  369,  256,  371,  372,  373,  261,   -1,
  262,   -1,   -1,  379,   -1,  381,  382,  383,  384,   -1,
   -1,   -1,  388,  389,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,  285,   -1,  400,   -1,  402,   -1,  404,   -1,
  406,   -1,  408,   -1,  410,  298,   -1,  299,   -1,   -1,
  303,   -1,   -1,   -1,   -1,  308,   -1,  310,  311,  312,
  313,   -1,   -1,   -1,   -1,  318,   -1,   -1,  434,  322,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,  334,  448,  449,  337,   -1,  339,   -1,  340,   -1,
   -1,   -1,   -1,   -1,  346,  347,  348,  349,  350,  351,
  352,  353,  354,  355,  356,  357,  359,   -1,  361,   -1,
   -1,   -1,  365,  365,  366,  367,  368,  369,  370,  371,
  372,  373,  256,  375,  376,   -1,  378,  379,  262,  381,
   -1,  383,  384,  385,  386,  387,  388,  389,   -1,   -1,
  392,   -1,  394,   -1,  396,   -1,  398,   -1,  400,   -1,
  402,   -1,  404,   -1,  406,   -1,  408,   -1,  410,   -1,
  412,   -1,  414,   -1,  416,  299,  418,   -1,  420,   -1,
  422,   -1,  424,   -1,  426,   -1,  428,   -1,  430,   -1,
  432,   -1,  434,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,  448,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,  340,   -1,   -1,   -1,
   -1,   -1,  346,  347,  348,  349,  350,  351,  352,  353,
  354,  355,  356,  357,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,  365,  366,  367,  368,  369,  370,  371,  372,  373,
  256,  375,  376,   -1,  378,  379,  262,  381,   -1,  383,
  384,  385,  386,  387,  388,  389,   -1,   -1,  392,   -1,
  394,   -1,  396,   -1,  398,   -1,  400,   -1,  402,   -1,
  404,   -1,  406,   -1,  408,   -1,  410,   -1,  412,   -1,
  414,   -1,  416,  299,  418,   -1,  420,   -1,  422,   -1,
  424,   -1,  426,   -1,  428,   -1,  430,   -1,  432,   -1,
  434,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,  448,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,  340,   -1,   -1,   -1,   -1,   -1,
  346,  347,  348,  349,  350,  351,  352,  353,  354,  355,
  356,  357,   -1,   -1,   -1,   -1,   -1,   -1,   -1,  365,
  366,  367,  368,  369,  370,  371,  372,  373,  256,  375,
  376,   -1,  378,  379,  262,  381,   -1,  383,  384,  385,
  386,  387,  388,  389,   -1,   -1,  392,   -1,  394,   -1,
  396,   -1,  398,   -1,  400,   -1,  402,   -1,  404,   -1,
  406,   -1,  408,   -1,  410,   -1,  412,   -1,  414,   -1,
  416,  299,  418,   -1,  420,   -1,  422,   -1,  424,   -1,
  426,   -1,  428,   -1,  430,   -1,  432,   -1,  434,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,  448,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,  340,   -1,   -1,   -1,   -1,   -1,  346,  347,
  348,  349,  350,  351,  352,  353,  354,  355,  356,  357,
   -1,   -1,  256,   -1,   -1,   -1,  364,  365,  262,  367,
  368,  369,  370,  371,  372,  373,   -1,  375,  376,   -1,
  378,  379,   -1,  381,   -1,  383,  384,  385,  386,  387,
  388,  389,   -1,   -1,  392,   -1,  394,   -1,  396,   -1,
  398,   -1,  400,   -1,  402,  299,  404,   -1,  406,   -1,
  408,   -1,  410,   -1,  412,   -1,  414,   -1,  416,   -1,
  418,   -1,  420,   -1,  422,   -1,  424,   -1,  426,   -1,
  428,   -1,  430,   -1,  432,   -1,  434,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,  340,   -1,   -1,   -1,
  448,   -1,  346,  347,  348,  349,  350,  351,  352,  353,
  354,  355,  356,  357,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,  365,  366,  367,  368,  369,  370,  371,  372,  373,
  256,  375,  376,   -1,  378,  379,  262,  381,   -1,  383,
  384,  385,  386,  387,  388,  389,   -1,   -1,  392,   -1,
  394,   -1,  396,   -1,  398,   -1,  400,   -1,  402,   -1,
  404,   -1,  406,   -1,  408,   -1,  410,   -1,  412,   -1,
  414,   -1,  416,  299,  418,   -1,  420,   -1,  422,   -1,
  424,   -1,  426,   -1,  428,   -1,  430,   -1,  432,   -1,
  434,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,  448,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,  340,   -1,   -1,   -1,   -1,   -1,
  346,  347,  348,  349,  350,  351,  352,  353,  354,  355,
  356,  357,   -1,   -1,   -1,   -1,   -1,   -1,   -1,  365,
  366,  367,  368,  369,  370,  371,  372,  373,  256,  375,
  376,   -1,  378,  379,  262,  381,   -1,  383,  384,  385,
  386,  387,  388,  389,   -1,   -1,  392,   -1,  394,   -1,
  396,   -1,  398,   -1,  400,   -1,  402,   -1,  404,   -1,
  406,   -1,  408,   -1,  410,   -1,  412,   -1,  414,   -1,
  416,  299,  418,   -1,  420,   -1,  422,   -1,  424,   -1,
  426,   -1,  428,   -1,  430,   -1,  432,   -1,  434,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,  448,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,  340,   -1,   -1,   -1,   -1,   -1,  346,  347,
  348,  349,  350,  351,  352,  353,  354,  355,  356,  357,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,  365,   -1,  367,
   -1,  369,   -1,  371,  372,  373,   -1,  375,  376,   -1,
  378,  379,   -1,  381,   -1,  383,  384,  385,  386,  387,
  388,  389,   -1,   -1,   -1,   -1,   -1,   -1,  396,   -1,
  398,   -1,  400,   -1,  402,   -1,  404,   -1,  406,   -1,
  408,   -1,  410,   -1,  412,   -1,  414,   -1,  416,   -1,
  418,   -1,  420,   -1,  422,   -1,  424,   -1,  426,   -1,
  428,   -1,  430,  257,   -1,   -1,  434,  261,   -1,   -1,
   -1,   -1,  266,   -1,  268,   -1,   -1,  271,   -1,  273,
  448,   -1,  276,   -1,  278,   -1,  280,   -1,  282,   -1,
   -1,   -1,   -1,   -1,  288,  289,   -1,   -1,   -1,   -1,
   -1,   -1,  296,  297,  298,   -1,   -1,  301,  302,  303,
   -1,  305,   -1,   -1,  308,   -1,  310,  311,  312,  313,
   -1,   -1,   -1,  317,  318,  319,   -1,   -1,  322,  323,
  324,   -1,   -1,   -1,   -1,   -1,   -1,  331,  332,   -1,
  334,  335,  336,  337,  338,  339,   -1,   -1,   -1,  343,
   -1,   -1,   -1,   -1,  257,   -1,   -1,   -1,  261,   -1,
   -1,   -1,   -1,  266,   -1,  268,   -1,   -1,  271,   -1,
  273,  365,  366,  276,   -1,  278,   -1,  280,   -1,  282,
   -1,   -1,   -1,   -1,   -1,  288,  289,   -1,   -1,   -1,
   -1,   -1,   -1,  296,  297,  298,   -1,   -1,  301,  302,
  303,   -1,  305,   -1,   -1,  308,   -1,  310,  311,  312,
  313,   -1,   -1,   -1,  317,  318,  319,   -1,   -1,  322,
  323,  324,   -1,   -1,   -1,   -1,   -1,   -1,  331,  332,
   -1,  334,  335,  336,  337,  338,  339,   -1,   -1,   -1,
  343,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,  448,  257,   -1,   -1,   -1,  261,
   -1,   -1,  365,  366,  266,   -1,  268,   -1,   -1,  271,
   -1,  273,   -1,   -1,  276,   -1,  278,   -1,  280,   -1,
  282,   -1,   -1,  285,   -1,   -1,  288,  289,   -1,   -1,
   -1,   -1,   -1,   -1,  296,  297,  298,   -1,   -1,  301,
  302,  303,   -1,  305,   -1,   -1,  308,   -1,  310,  311,
  312,  313,   -1,   -1,   -1,  317,  318,  319,   -1,   -1,
  322,  323,  324,   -1,   -1,   -1,   -1,   -1,   -1,  331,
  332,   -1,  334,  335,   -1,  337,  338,  339,   -1,   -1,
   -1,  343,   -1,   -1,   -1,  448,  257,   -1,   -1,   -1,
  261,   -1,   -1,   -1,   -1,  266,   -1,  268,   -1,   -1,
  271,   -1,  273,  365,  366,  276,   -1,  278,   -1,  280,
   -1,  282,   -1,   -1,  285,   -1,   -1,  288,  289,   -1,
   -1,   -1,   -1,   -1,   -1,  296,  297,  298,   -1,   -1,
  301,  302,  303,   -1,  305,   -1,   -1,  308,   -1,  310,
  311,  312,  313,   -1,   -1,   -1,  317,  318,  319,   -1,
   -1,  322,  323,  324,   -1,   -1,   -1,   -1,   -1,   -1,
  331,  332,   -1,  334,  335,   -1,  337,  338,  339,   -1,
   -1,   -1,  343,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,  448,  257,   -1,   -1,
   -1,  261,   -1,   -1,  365,  366,  266,   -1,  268,   -1,
   -1,  271,   -1,  273,   -1,   -1,  276,   -1,  278,   -1,
  280,   -1,  282,   -1,   -1,  285,   -1,   -1,  288,  289,
   -1,   -1,   -1,   -1,   -1,   -1,  296,  297,  298,   -1,
   -1,  301,  302,  303,   -1,  305,   -1,   -1,  308,   -1,
  310,  311,  312,  313,   -1,   -1,   -1,  317,  318,  319,
   -1,   -1,  322,  323,  324,   -1,   -1,   -1,   -1,   -1,
   -1,  331,  332,   -1,  334,  335,   -1,  337,  338,  339,
   -1,  257,   -1,  343,   -1,  261,   -1,  448,   -1,   -1,
  266,   -1,  268,   -1,   -1,  271,   -1,  273,   -1,   -1,
  276,   -1,  278,   -1,  280,  365,  282,   -1,   -1,  285,
   -1,   -1,  288,  289,   -1,   -1,   -1,   -1,   -1,   -1,
  296,  297,  298,   -1,   -1,  301,  302,  303,   -1,  305,
   -1,   -1,  308,   -1,  310,  311,  312,  313,   -1,   -1,
   -1,  317,  318,  319,   -1,   -1,  322,  323,  324,   -1,
   -1,   -1,   -1,   -1,   -1,  331,  332,   -1,  334,  335,
   -1,  337,  338,  339,   -1,   -1,   -1,  343,   -1,   -1,
  265,  266,  267,  268,   -1,   -1,  271,  272,   -1,  274,
  275,  276,   -1,  278,  279,  280,   -1,   -1,  448,  365,
   -1,  286,   -1,  288,  289,  290,  291,  292,  293,   -1,
   -1,  296,   -1,   -1,   -1,  300,  301,   -1,  303,  304,
  305,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
  315,   -1,  317,   -1,  319,  320,   -1,   -1,  323,   -1,
  325,  326,  327,  328,  329,  330,  331,  332,  333,  334,
  335,  336,   -1,  338,   -1,   -1,  341,   -1,   -1,   -1,
   -1,  346,   -1,   -1,  266,   -1,  268,   -1,   -1,  271,
   -1,   -1,   -1,   -1,  276,   -1,   -1,   -1,  280,  364,
   -1,   -1,  448,  368,   -1,   -1,   -1,  289,  373,  374,
  375,  376,  377,   -1,  296,   -1,   -1,   -1,  383,  301,
  385,   -1,   -1,  305,   -1,  307,   -1,  392,   -1,  394,
   -1,   -1,  314,   -1,   -1,  317,   -1,  319,   -1,   -1,
   -1,  323,   -1,   -1,  326,   -1,   -1,   -1,   -1,  331,
  332,   -1,   -1,  335,   -1,   -1,  338,   -1,   -1,   -1,
   -1,  265,  266,   -1,  268,   -1,   -1,  271,  272,   -1,
   -1,  436,  276,  438,  278,  440,  280,  442,   -1,  444,
   -1,  446,  286,  448,  449,  289,   -1,   -1,   -1,  454,
   -1,   -1,  296,   -1,   -1,   -1,   -1,  301,   -1,  303,
  304,  305,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,  317,   -1,  319,  320,  321,   -1,  323,
   -1,   -1,  326,   -1,  328,   -1,  330,  331,  332,  333,
   -1,  335,   -1,   -1,  338,   -1,   -1,   -1,  342,   -1,
   -1,   -1,  346,   -1,  265,  266,   -1,  268,   -1,   -1,
  271,  272,   -1,   -1,   -1,  276,   -1,  278,   -1,  280,
  364,  365,   -1,   -1,  368,  286,  448,   -1,  289,   -1,
  374,  375,  376,  377,   -1,  296,   -1,   -1,   -1,  383,
  301,  385,  303,  304,  305,   -1,  307,   -1,  392,   -1,
  394,   -1,   -1,  314,   -1,   -1,  317,   -1,  319,  320,
   -1,   -1,  323,   -1,   -1,  326,   -1,  328,   -1,  330,
  331,  332,  333,   -1,  335,   -1,   -1,  338,   -1,   -1,
   -1,  342,   -1,   -1,   -1,  346,   -1,   -1,   -1,   -1,
   -1,   -1,  436,   -1,  438,   -1,  440,   -1,  442,   -1,
  444,   -1,  446,   -1,  448,  449,   -1,  368,  369,   -1,
  454,   -1,   -1,  374,  375,  376,  377,   -1,   -1,   -1,
   -1,   -1,  383,   -1,  385,   -1,   -1,   -1,   -1,   -1,
   -1,  392,   -1,  394,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,  265,  266,   -1,  268,   -1,
   -1,  271,  272,   -1,   -1,  436,  276,  438,  278,  440,
  280,  442,   -1,  444,   -1,  446,  286,  448,  449,  289,
   -1,   -1,   -1,  454,   -1,   -1,  296,   -1,   -1,   -1,
   -1,  301,   -1,  303,  304,  305,   -1,  307,   -1,   -1,
   -1,   -1,   -1,   -1,  314,   -1,   -1,  317,   -1,  319,
  320,   -1,   -1,  323,   -1,   -1,  326,   -1,  328,   -1,
  330,  331,  332,  333,   -1,  335,   -1,   -1,  338,   -1,
   -1,   -1,  342,   -1,   -1,   -1,  346,   -1,   -1,  266,
   -1,  268,   -1,   -1,  271,   -1,  273,   -1,   -1,  276,
   -1,   -1,   -1,  280,   -1,   -1,   -1,   -1,  368,  369,
   -1,   -1,  289,   -1,  374,  375,  376,  377,   -1,  296,
   -1,   -1,   -1,  383,  301,  385,  303,   -1,  305,   -1,
   -1,   -1,  392,   -1,  394,   -1,   -1,   -1,   -1,   -1,
  317,   -1,  319,   -1,   -1,   -1,  323,  324,   -1,   -1,
   -1,   -1,   -1,   -1,  331,  332,   -1,   -1,  335,   -1,
   -1,  338,   -1,   -1,   -1,   -1,  265,  266,   -1,  268,
   -1,   -1,  271,  272,   -1,   -1,  436,  276,  438,  278,
  440,  280,  442,   -1,  444,   -1,  446,  286,  448,  449,
  289,   -1,   -1,   -1,  454,   -1,   -1,  296,   -1,   -1,
   -1,   -1,  301,   -1,  303,  304,  305,   -1,  307,   -1,
   -1,   -1,   -1,   -1,   -1,  314,   -1,   -1,  317,   -1,
  319,  320,   -1,   -1,  323,   -1,   -1,  326,   -1,  328,
   -1,  330,  331,  332,  333,   -1,  335,   -1,   -1,  338,
   -1,   -1,   -1,  342,   -1,   -1,   -1,  346,   -1,  265,
  266,   -1,  268,   -1,   -1,  271,  272,   -1,   -1,   -1,
  276,   -1,  278,   -1,  280,   -1,   -1,   -1,   -1,  368,
  286,  448,   -1,  289,   -1,  374,  375,  376,  377,   -1,
  296,   -1,   -1,   -1,  383,  301,  385,  303,  304,  305,
   -1,   -1,   -1,  392,   -1,  394,   -1,   -1,   -1,   -1,
   -1,  317,   -1,  319,  320,  321,   -1,  323,   -1,   -1,
  326,   -1,  328,   -1,  330,  331,  332,  333,   -1,  335,
   -1,   -1,  338,   -1,   -1,   -1,  342,   -1,   -1,   -1,
  346,   -1,   -1,   -1,   -1,   -1,   -1,  436,   -1,  438,
   -1,  440,   -1,  442,   -1,  444,   -1,  446,  364,  448,
  449,   -1,  368,   -1,   -1,  454,   -1,   -1,  374,  375,
  376,  377,   -1,   -1,   -1,   -1,   -1,  383,   -1,  385,
   -1,   -1,   -1,   -1,   -1,   -1,  392,   -1,  394,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
  265,  266,   -1,  268,   -1,   -1,  271,  272,   -1,   -1,
  436,  276,  438,  278,  440,  280,  442,   -1,  444,   -1,
  446,  286,  448,  449,  289,   -1,   -1,   -1,  454,   -1,
   -1,  296,   -1,   -1,   -1,   -1,  301,   -1,  303,  304,
  305,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,  317,   -1,  319,  320,   -1,   -1,  323,   -1,
   -1,  326,   -1,  328,   -1,  330,  331,  332,  333,   -1,
  335,   -1,   -1,  338,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,  346,   -1,   -1,  266,   -1,  268,   -1,   -1,  271,
   -1,   -1,   -1,   -1,  276,   -1,   -1,   -1,  280,  364,
  365,  283,   -1,  368,   -1,   -1,   -1,  289,   -1,  374,
  375,  376,  377,   -1,  296,   -1,   -1,   -1,  383,  301,
  385,   -1,   -1,  305,   -1,   -1,   -1,  392,   -1,  394,
   -1,   -1,   -1,   -1,   -1,  317,   -1,  319,   -1,   -1,
   -1,  323,   -1,   -1,   -1,   -1,   -1,   -1,   -1,  331,
  332,   -1,   -1,  335,   -1,   -1,  338,   -1,   -1,   -1,
   -1,  265,  266,   -1,  268,   -1,   -1,  271,  272,   -1,
   -1,  436,  276,  438,  278,  440,  280,  442,   -1,  444,
   -1,  446,  286,  448,  449,  289,   -1,   -1,   -1,  454,
   -1,   -1,  296,   -1,   -1,   -1,   -1,  301,   -1,  303,
  304,  305,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,  317,   -1,  319,  320,   -1,   -1,  323,
   -1,   -1,  326,   -1,  328,   -1,  330,  331,  332,  333,
   -1,  335,   -1,   -1,  338,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,  346,   -1,  265,  266,   -1,  268,   -1,   -1,
  271,  272,   -1,   -1,   -1,  276,   -1,  278,   -1,  280,
   -1,   -1,   -1,   -1,  368,  286,  448,  371,  289,   -1,
  374,  375,  376,  377,   -1,  296,   -1,   -1,   -1,  383,
  301,  385,  303,  304,  305,   -1,   -1,   -1,  392,   -1,
  394,   -1,   -1,   -1,   -1,   -1,  317,   -1,  319,  320,
   -1,   -1,  323,   -1,   -1,  326,   -1,  328,   -1,  330,
  331,  332,  333,   -1,  335,   -1,   -1,  338,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,  346,   -1,   -1,   -1,   -1,
   -1,   -1,  436,   -1,  438,   -1,  440,   -1,  442,   -1,
  444,   -1,  446,   -1,  448,  449,   -1,  368,  369,   -1,
  454,   -1,   -1,  374,  375,  376,  377,   -1,   -1,   -1,
   -1,   -1,  383,   -1,  385,   -1,   -1,   -1,   -1,   -1,
   -1,  392,   -1,  394,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,  265,  266,   -1,  268,   -1,
   -1,  271,  272,   -1,   -1,  436,  276,  438,  278,  440,
  280,  442,   -1,  444,   -1,  446,  286,  448,  449,  289,
   -1,   -1,   -1,  454,   -1,   -1,  296,   -1,   -1,   -1,
   -1,  301,   -1,  303,  304,  305,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,  317,   -1,  319,
  320,   -1,   -1,  323,   -1,   -1,  326,   -1,  328,   -1,
  330,  331,  332,  333,   -1,  335,   -1,   -1,  338,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,  346,   -1,  265,  266,
   -1,  268,   -1,   -1,  271,  272,   -1,   -1,   -1,  276,
   -1,  278,   -1,  280,  364,   -1,   -1,   -1,  368,  286,
   -1,   -1,  289,   -1,  374,  375,  376,  377,   -1,  296,
   -1,   -1,   -1,  383,  301,  385,  303,  304,  305,   -1,
   -1,   -1,  392,   -1,  394,   -1,   -1,   -1,   -1,   -1,
  317,   -1,  319,  320,   -1,   -1,  323,   -1,   -1,  326,
   -1,  328,   -1,  330,  331,  332,  333,   -1,  335,   -1,
   -1,  338,   -1,   -1,   -1,   -1,   -1,   -1,   -1,  346,
   -1,   -1,   -1,   -1,   -1,   -1,  436,   -1,  438,   -1,
  440,   -1,  442,   -1,  444,   -1,  446,   -1,  448,  449,
  367,  368,   -1,   -1,  454,   -1,   -1,  374,  375,  376,
  377,   -1,   -1,   -1,   -1,   -1,  383,   -1,  385,   -1,
   -1,   -1,   -1,   -1,   -1,  392,   -1,  394,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,  265,
  266,   -1,  268,   -1,   -1,  271,  272,   -1,   -1,  436,
  276,  438,  278,  440,  280,  442,   -1,  444,   -1,  446,
  286,  448,  449,  289,   -1,   -1,   -1,  454,   -1,   -1,
  296,   -1,   -1,   -1,   -1,  301,   -1,  303,  304,  305,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,  317,   -1,  319,  320,   -1,   -1,  323,   -1,   -1,
  326,   -1,  328,   -1,  330,  331,  332,  333,   -1,  335,
   -1,   -1,  338,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
  346,   -1,   -1,  266,   -1,  268,   -1,   -1,  271,   -1,
   -1,   -1,   -1,  276,   -1,   -1,   -1,  280,   -1,   -1,
   -1,   -1,  368,   -1,   -1,   -1,  289,  373,  374,  375,
  376,  377,   -1,  296,   -1,   -1,   -1,  383,  301,  385,
   -1,   -1,  305,   -1,   -1,   -1,  392,   -1,  394,   -1,
   -1,   -1,   -1,   -1,  317,   -1,  319,   -1,   -1,   -1,
  323,   -1,   -1,   -1,   -1,   -1,   -1,   -1,  331,  332,
   -1,   -1,  335,   -1,   -1,  338,   -1,   -1,   -1,   -1,
  265,  266,   -1,  268,   -1,   -1,  271,  272,   -1,   -1,
  436,  276,  438,  278,  440,  280,  442,   -1,  444,   -1,
  446,  286,  448,  449,  289,   -1,   -1,   -1,  454,   -1,
   -1,  296,   -1,   -1,   -1,   -1,  301,   -1,  303,  304,
  305,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,  317,   -1,  319,  320,   -1,   -1,  323,   -1,
   -1,  326,   -1,  328,   -1,  330,  331,  332,  333,   -1,
  335,   -1,   -1,  338,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,  346,   -1,  265,  266,   -1,  268,   -1,   -1,  271,
  272,   -1,   -1,   -1,  276,   -1,  278,   -1,  280,   -1,
  365,   -1,   -1,  368,  286,  448,   -1,  289,   -1,  374,
  375,  376,  377,   -1,  296,   -1,   -1,   -1,  383,  301,
  385,  303,  304,  305,   -1,   -1,   -1,  392,   -1,  394,
   -1,   -1,   -1,   -1,   -1,  317,   -1,  319,  320,   -1,
   -1,  323,   -1,   -1,  326,   -1,  328,   -1,  330,  331,
  332,  333,   -1,  335,   -1,   -1,  338,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,  346,   -1,   -1,   -1,   -1,   -1,
   -1,  436,   -1,  438,   -1,  440,   -1,  442,   -1,  444,
   -1,  446,  364,  448,  449,   -1,  368,   -1,   -1,  454,
   -1,   -1,  374,  375,  376,  377,   -1,   -1,   -1,   -1,
   -1,  383,   -1,  385,   -1,   -1,   -1,   -1,   -1,   -1,
  392,   -1,  394,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,  265,  266,   -1,  268,   -1,   -1,
  271,  272,   -1,   -1,  436,  276,  438,  278,  440,  280,
  442,   -1,  444,   -1,  446,  286,  448,  449,  289,   -1,
   -1,   -1,  454,   -1,   -1,  296,   -1,   -1,   -1,   -1,
  301,   -1,  303,  304,  305,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,  317,   -1,  319,  320,
   -1,   -1,  323,   -1,   -1,  326,   -1,  328,   -1,  330,
  331,  332,  333,   -1,  335,   -1,   -1,  338,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,  346,   -1,  265,  266,   -1,
  268,   -1,   -1,  271,  272,   -1,   -1,   -1,  276,   -1,
  278,   -1,  280,   -1,   -1,   -1,   -1,  368,  286,   -1,
   -1,  289,   -1,  374,  375,  376,  377,   -1,  296,   -1,
   -1,   -1,  383,  301,  385,  303,  304,  305,   -1,   -1,
   -1,  392,   -1,  394,   -1,   -1,   -1,   -1,   -1,  317,
   -1,  319,  320,   -1,   -1,  323,   -1,   -1,  326,   -1,
  328,   -1,  330,  331,  332,  333,   -1,  335,   -1,   -1,
  338,   -1,   -1,   -1,   -1,   -1,   -1,   -1,  346,   -1,
   -1,   -1,   -1,   -1,   -1,  436,   -1,  438,   -1,  440,
   -1,  442,   -1,  444,   -1,  446,   -1,  448,  449,   -1,
  368,   -1,   -1,  454,   -1,   -1,  374,  375,  376,  377,
   -1,   -1,   -1,   -1,   -1,  383,   -1,  385,   -1,   -1,
   -1,   -1,   -1,   -1,  392,   -1,  394,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,  265,  266,
   -1,  268,   -1,   -1,  271,  272,   -1,   -1,  436,  276,
  438,  278,  440,  280,  442,   -1,  444,   -1,  446,  286,
  448,  449,  289,   -1,   -1,   -1,  454,   -1,   -1,  296,
   -1,   -1,   -1,   -1,  301,   -1,  303,  304,  305,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
  317,   -1,  319,  320,   -1,   -1,  323,   -1,   -1,  326,
   -1,  328,   -1,  330,  331,  332,  333,   -1,  335,   -1,
   -1,  338,   -1,   -1,   -1,   -1,   -1,   -1,   -1,  346,
   -1,  265,  266,   -1,  268,   -1,   -1,  271,  272,   -1,
   -1,   -1,  276,   -1,  278,   -1,  280,   -1,   -1,   -1,
   -1,  368,  286,   -1,   -1,  289,   -1,  374,  375,  376,
  377,   -1,  296,   -1,   -1,   -1,  383,  301,  385,  303,
  304,  305,   -1,   -1,   -1,  392,   -1,  394,   -1,   -1,
   -1,   -1,   -1,  317,   -1,  319,  320,   -1,   -1,  323,
   -1,   -1,  326,   -1,  328,   -1,  330,  331,  332,  333,
   -1,  335,   -1,   -1,  338,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,  346,   -1,   -1,   -1,   -1,   -1,   -1,  436,
   -1,  438,   -1,  440,   -1,  442,   -1,  444,   -1,  446,
   -1,  448,  449,   -1,  368,   -1,   -1,  454,   -1,   -1,
  374,  375,  376,  377,   -1,   -1,   -1,   -1,   -1,  383,
   -1,  385,   -1,   -1,   -1,   -1,   -1,   -1,  392,   -1,
  394,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,  265,  266,   -1,  268,   -1,   -1,  271,  272,
   -1,   -1,  436,  276,  438,  278,  440,  280,  442,   -1,
  444,   -1,  446,  286,  448,  449,  289,   -1,   -1,   -1,
  454,   -1,   -1,  296,   -1,   -1,   -1,   -1,  301,   -1,
  303,  304,  305,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,  317,   -1,  319,  320,   -1,   -1,
  323,   -1,   -1,  326,   -1,  328,   -1,  330,  331,  332,
  333,   -1,  335,   -1,   -1,  338,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,  265,  266,   -1,  268,   -1,
   -1,  271,  272,   -1,   -1,   -1,  276,   -1,  278,   -1,
  280,   -1,   -1,   -1,   -1,  368,  286,   -1,   -1,  289,
   -1,  374,  375,  376,  377,   -1,  296,   -1,   -1,   -1,
  383,  301,  385,  303,  304,  305,   -1,   -1,   -1,  392,
   -1,  394,   -1,   -1,   -1,   -1,   -1,  317,   -1,  319,
  320,   -1,   -1,  323,   -1,   -1,  326,   -1,  328,   -1,
  330,  331,  332,  333,   -1,  335,   -1,   -1,  338,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,  436,   -1,  438,   -1,  440,   -1,  442,
   -1,  444,   -1,  446,   -1,  448,   -1,   -1,  368,   -1,
   -1,  454,   -1,   -1,  374,  375,  376,  377,   -1,   -1,
   -1,   -1,   -1,  383,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,  392,   -1,  394,  265,  266,   -1,  268,   -1,
   -1,  271,  272,   -1,   -1,   -1,  276,   -1,  278,   -1,
  280,   -1,   -1,   -1,   -1,   -1,  286,   -1,   -1,  289,
   -1,   -1,   -1,   -1,   -1,   -1,  296,   -1,   -1,   -1,
   -1,  301,   -1,  303,  304,  305,  436,   -1,  438,   -1,
  440,   -1,  442,   -1,  444,   -1,  446,  317,  448,  319,
  320,   -1,   -1,  323,  454,   -1,  326,   -1,  328,   -1,
  330,  331,  332,  333,   -1,  335,   -1,   -1,  338,   -1,
  265,  266,   -1,  268,   -1,   -1,  271,  272,   -1,   -1,
   -1,  276,   -1,  278,   -1,  280,   -1,   -1,   -1,   -1,
   -1,  286,   -1,   -1,  289,   -1,   -1,   -1,  368,   -1,
   -1,  296,   -1,   -1,  374,   -1,  301,  377,  303,  304,
  305,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,  317,   -1,  319,  320,   -1,   -1,  323,   -1,
   -1,  326,   -1,  328,   -1,  330,  331,  332,  333,   -1,
  335,   -1,   -1,  338,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,  436,   -1,  438,   -1,
  440,   -1,  442,  368,  444,   -1,  446,   -1,  448,   -1,
   -1,   -1,   -1,   -1,  454,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
  265,  266,   -1,  268,   -1,   -1,  271,  272,   -1,   -1,
   -1,  276,   -1,  278,   -1,  280,   -1,   -1,   -1,   -1,
   -1,  286,   -1,   -1,  289,   -1,   -1,   -1,   -1,   -1,
   -1,  296,   -1,   -1,   -1,   -1,  301,   -1,  303,  304,
  305,  436,   -1,  438,   -1,  440,   -1,  442,   -1,  444,
   -1,  446,  317,  448,  319,  320,   -1,   -1,  323,  454,
   -1,  326,   -1,  328,   -1,  330,  331,  332,  333,   -1,
  335,  265,  266,  338,  268,   -1,   -1,  271,  272,   -1,
   -1,   -1,  276,   -1,  278,   -1,  280,   -1,   -1,   -1,
   -1,   -1,  286,   -1,   -1,  289,   -1,   -1,   -1,   -1,
   -1,   -1,  296,  368,   -1,   -1,   -1,  301,   -1,  303,
  304,  305,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,  317,   -1,  319,  320,   -1,   -1,  323,
   -1,   -1,  326,   -1,  328,   -1,  330,  331,  332,  333,
   -1,  335,   -1,   -1,  338,   -1,   -1,   -1,  261,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,  436,  285,  438,  368,  440,   -1,  442,   -1,  444,
   -1,  446,   -1,  448,   -1,  298,  261,   -1,  263,  454,
  303,   -1,   -1,  306,   -1,  308,   -1,  310,  311,  312,
  313,   -1,   -1,   -1,   -1,  318,   -1,   -1,   -1,  322,
  285,   -1,   -1,  326,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,  334,   -1,  298,  337,   -1,  339,   -1,  303,   -1,
   -1,   -1,   -1,  308,   -1,  310,  311,  312,  313,   -1,
   -1,  316,  436,  318,  438,   -1,  440,  322,  442,   -1,
  444,   -1,  446,  366,  448,  368,  261,  370,  263,  334,
  454,  266,  337,  268,  339,   -1,  271,   -1,  273,  274,
   -1,  276,  385,  278,   -1,  280,  389,  282,  283,  284,
  285,   -1,   -1,  288,  289,   -1,   -1,   -1,   -1,  294,
  365,  296,  297,  298,   -1,   -1,  301,  302,  303,   -1,
  305,   -1,  307,  308,  309,  310,  311,  312,  313,  314,
   -1,  316,  317,  318,  319,   -1,   -1,  322,  323,  324,
   -1,  326,   -1,   -1,   -1,   -1,  331,  332,   -1,  334,
  335,   -1,  337,  338,  339,  448,  449,   -1,  343,   -1,
  261,   -1,   -1,   -1,   -1,  266,   -1,  268,   -1,   -1,
  271,   -1,  273,  274,  359,  276,  361,  278,   -1,  280,
   -1,  282,  283,  284,  285,   -1,   -1,  288,  289,  374,
   -1,   -1,   -1,  294,   -1,  296,  297,  298,   -1,   -1,
  301,   -1,  303,   -1,  305,   -1,   -1,  308,   -1,  310,
  311,  312,  313,   -1,   -1,   -1,  317,  318,  319,   -1,
   -1,  322,  323,  324,   -1,   -1,   -1,   -1,   -1,   -1,
  331,  332,   -1,  334,  335,   -1,  337,  338,  339,   -1,
   -1,   -1,  343,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,  261,   -1,   -1,   -1,   -1,
  266,   -1,  268,  448,  365,  271,   -1,  273,  274,   -1,
  276,   -1,  278,  374,  280,   -1,  282,  283,  284,  285,
   -1,   -1,  288,  289,   -1,   -1,   -1,   -1,  294,   -1,
  296,  297,  298,   -1,   -1,  301,   -1,  303,   -1,  305,
   -1,   -1,  308,   -1,  310,  311,  312,  313,   -1,   -1,
   -1,  317,  318,  319,   -1,   -1,  322,  323,  324,   -1,
   -1,   -1,   -1,   -1,   -1,  331,  332,   -1,  334,  335,
   -1,  337,  338,  339,   -1,   -1,   -1,  343,   -1,  261,
   -1,   -1,   -1,   -1,  266,   -1,  268,  448,   -1,  271,
   -1,  273,  274,   -1,  276,   -1,  278,   -1,  280,  365,
  282,  283,  284,  285,   -1,   -1,  288,  289,  374,   -1,
   -1,   -1,  294,   -1,  296,  297,  298,   -1,   -1,  301,
   -1,  303,   -1,  305,   -1,   -1,  308,   -1,  310,  311,
  312,  313,   -1,   -1,   -1,  317,  318,  319,   -1,   -1,
  322,  323,  324,   -1,   -1,   -1,   -1,   -1,   -1,  331,
  332,   -1,  334,  335,   -1,  337,  338,  339,   -1,   -1,
   -1,  343,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,  261,   -1,   -1,   -1,   -1,  266,
   -1,  268,  448,  365,  271,   -1,  273,  274,   -1,  276,
   -1,  278,  374,  280,   -1,  282,  283,  284,  285,   -1,
   -1,  288,  289,   -1,   -1,   -1,   -1,  294,   -1,  296,
  297,  298,   -1,   -1,  301,   -1,  303,   -1,  305,   -1,
   -1,  308,   -1,  310,  311,  312,  313,   -1,   -1,   -1,
  317,  318,  319,   -1,   -1,  322,  323,  324,   -1,   -1,
   -1,   -1,   -1,   -1,  331,  332,   -1,  334,  335,   -1,
  337,  338,  339,   -1,   -1,   -1,  343,   -1,  261,   -1,
   -1,   -1,   -1,  266,   -1,  268,  448,   -1,  271,   -1,
  273,  274,   -1,  276,   -1,  278,   -1,  280,  365,  282,
  283,  284,  285,   -1,   -1,  288,  289,  374,   -1,   -1,
   -1,  294,   -1,  296,  297,  298,   -1,   -1,  301,   -1,
  303,   -1,  305,   -1,   -1,  308,   -1,  310,  311,  312,
  313,   -1,   -1,   -1,  317,  318,  319,   -1,   -1,  322,
  323,  324,   -1,   -1,   -1,   -1,   -1,   -1,  331,  332,
   -1,  334,  335,   -1,  337,  338,  339,   -1,   -1,   -1,
  343,  261,   -1,   -1,   -1,   -1,  266,   -1,  268,   -1,
   -1,  271,   -1,  273,  274,   -1,  276,   -1,  278,   -1,
  280,  448,  282,  283,  284,  285,   -1,   -1,  288,  289,
   -1,  374,   -1,   -1,  294,   -1,  296,  297,  298,   -1,
   -1,  301,   -1,  303,   -1,  305,   -1,   -1,  308,   -1,
  310,  311,  312,  313,   -1,   -1,   -1,  317,  318,  319,
   -1,   -1,  322,  323,  324,   -1,   -1,   -1,   -1,   -1,
   -1,  331,  332,   -1,  334,  335,   -1,  337,  338,  339,
  261,   -1,   -1,  343,   -1,  266,   -1,  268,   -1,   -1,
  271,   -1,  273,  274,   -1,  276,   -1,  278,   -1,  280,
   -1,  282,  283,   -1,  285,  448,   -1,   -1,  289,   -1,
   -1,   -1,   -1,   -1,  374,  296,  297,  298,   -1,   -1,
  301,   -1,  303,   -1,  305,   -1,   -1,  308,   -1,  310,
  311,  312,  313,   -1,   -1,   -1,  317,  318,  319,   -1,
   -1,  322,  323,  324,   -1,   -1,   -1,   -1,   -1,   -1,
  331,  332,   -1,  334,  335,   -1,  337,  338,  339,  261,
   -1,   -1,  343,   -1,  266,  262,  268,   -1,   -1,  271,
   -1,  273,  274,   -1,  276,   -1,  278,   -1,  280,   -1,
  282,  283,   -1,  285,  365,   -1,   -1,  289,  448,   -1,
   -1,   -1,   -1,   -1,  296,  297,  298,   -1,   -1,  301,
   -1,  303,  299,  305,   -1,   -1,  308,   -1,  310,  311,
  312,  313,   -1,   -1,   -1,  317,  318,  319,   -1,   -1,
  322,  323,  324,   -1,   -1,   -1,   -1,   -1,   -1,  331,
  332,  262,  334,  335,   -1,  337,  338,  339,   -1,   -1,
   -1,  343,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,  365,   -1,   -1,   -1,  448,  299,  366,
   -1,  368,   -1,  370,   -1,   -1,  373,   -1,  375,  376,
   -1,  378,  379,   -1,  381,   -1,  383,  384,  385,  386,
  387,  388,  389,   -1,   -1,  392,   -1,  394,   -1,  396,
   -1,  398,   -1,  400,   -1,  402,   -1,  404,   -1,  406,
   -1,  408,   -1,  410,   -1,  412,   -1,  414,   -1,  416,
   -1,  418,   -1,  420,   -1,  422,   -1,  424,   -1,  426,
   -1,  428,   -1,  430,   -1,  432,   -1,  434,  369,   -1,
  371,   -1,  373,   -1,  375,  376,  448,  378,  379,   -1,
  381,  448,  383,  384,   -1,  386,  387,  388,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,  396,   -1,  398,   -1,  400,
   -1,  402,   -1,  404,   -1,  406,   -1,  408,   -1,  410,
   -1,  412,   -1,  414,   -1,  416,   -1,  418,   -1,  420,
   -1,  422,   -1,  424,   -1,  426,   -1,  428,   -1,  430,
   -1,   -1,  266,  434,  268,   -1,   -1,  271,   -1,  273,
  274,   -1,  276,   -1,  278,   -1,  280,  448,  282,  283,
  284,   -1,   -1,   -1,  288,  289,   -1,   -1,   -1,   -1,
  294,   -1,  296,  297,   -1,   -1,   -1,  301,   -1,   -1,
   -1,  305,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,  317,   -1,  319,   -1,   -1,   -1,  323,
  324,   -1,   -1,   -1,   -1,   -1,   -1,  331,  332,   -1,
  266,  335,  268,   -1,  338,  271,   -1,  273,  274,  343,
  276,   -1,  278,   -1,  280,   -1,  282,  283,  284,   -1,
   -1,   -1,  288,  289,   -1,  359,   -1,  361,  294,   -1,
  296,  297,  266,   -1,  268,  301,   -1,  271,   -1,  305,
  274,   -1,  276,   -1,  278,   -1,  280,   -1,  282,  283,
  284,  317,   -1,  319,  288,  289,   -1,  323,  324,   -1,
  294,   -1,  296,   -1,   -1,  331,  332,  301,   -1,  335,
   -1,  305,  338,   -1,   -1,   -1,   -1,  343,   -1,   -1,
   -1,   -1,   -1,  317,   -1,  319,   -1,   -1,   -1,  323,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,  331,  332,   -1,
   -1,  335,   -1,   -1,  338,   -1,   -1,   -1,  374,  343,
   -1,  266,   -1,  268,  448,   -1,  271,   -1,  273,  274,
   -1,  276,   -1,  278,   -1,  280,   -1,  282,  283,   -1,
   -1,   -1,   -1,   -1,  289,   -1,   -1,   -1,   -1,   -1,
   -1,  296,  297,   -1,   -1,   -1,  301,   -1,   -1,   -1,
  305,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,  317,   -1,  319,   -1,   -1,   -1,  323,  324,
   -1,   -1,   -1,   -1,   -1,   -1,  331,  332,   -1,   -1,
  335,   -1,  448,  338,   -1,  266,   -1,  268,  343,   -1,
  271,   -1,  273,   -1,   -1,  276,   -1,  278,   -1,  280,
   -1,  282,   -1,   -1,   -1,   -1,   -1,  288,  289,   -1,
   -1,   -1,   -1,   -1,  448,  296,  297,   -1,   -1,   -1,
  301,   -1,   -1,   -1,  305,  266,   -1,  268,   -1,   -1,
  271,   -1,   -1,   -1,   -1,  276,  317,   -1,  319,  280,
   -1,   -1,  323,  324,   -1,   -1,   -1,   -1,  289,   -1,
  331,  332,   -1,   -1,  335,  296,   -1,  338,   -1,   -1,
  301,   -1,  343,   -1,  305,   -1,  307,   -1,  309,   -1,
   -1,   -1,   -1,  314,   -1,   -1,  317,   -1,  319,   -1,
   -1,   -1,  323,   -1,   -1,  326,   -1,   -1,   -1,   -1,
  331,  332,   -1,  448,  335,   -1,   -1,  338,   -1,   -1,
  266,  342,  268,   -1,   -1,  271,   -1,   -1,   -1,   -1,
  276,   -1,   -1,   -1,  280,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,  289,   -1,  366,  367,   -1,   -1,   -1,
  296,   -1,   -1,   -1,   -1,  301,   -1,   -1,   -1,  305,
   -1,  307,   -1,  309,   -1,  266,   -1,  268,  314,   -1,
  271,  317,   -1,  319,   -1,  276,   -1,  323,   -1,  280,
  326,   -1,   -1,   -1,   -1,  331,  332,  448,  289,  335,
   -1,   -1,  338,   -1,   -1,  296,  342,   -1,   -1,   -1,
  301,   -1,   -1,   -1,  305,   -1,  307,   -1,  309,   -1,
   -1,   -1,   -1,  314,   -1,   -1,  317,   -1,  319,   -1,
  366,   -1,  323,  369,   -1,  326,   -1,  448,   -1,   -1,
  331,  332,   -1,   -1,  335,   -1,   -1,  338,   -1,  266,
   -1,  268,   -1,   -1,  271,   -1,   -1,   -1,   -1,  276,
   -1,  278,   -1,  280,   -1,  282,   -1,   -1,   -1,   -1,
   -1,  288,  289,   -1,   -1,   -1,   -1,   -1,  369,  296,
   -1,  266,   -1,  268,  301,   -1,  271,   -1,  305,   -1,
   -1,  276,   -1,   -1,   -1,  280,   -1,   -1,   -1,   -1,
  317,   -1,  319,   -1,  289,   -1,  323,   -1,   -1,   -1,
   -1,  296,  448,   -1,  331,  332,  301,   -1,  335,   -1,
  305,  338,  307,   -1,  309,  266,  343,  268,   -1,  314,
  271,   -1,  317,   -1,  319,  276,   -1,   -1,  323,  280,
   -1,  326,   -1,   -1,   -1,   -1,  331,  332,  289,  266,
  335,  268,   -1,  338,  271,  296,   -1,  448,   -1,  276,
  301,   -1,   -1,  280,  305,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,  289,   -1,   -1,   -1,  317,   -1,  319,  296,
   -1,   -1,  323,   -1,  301,   -1,   -1,   -1,  305,   -1,
  331,  332,   -1,  266,  335,  268,   -1,  338,  271,   -1,
  317,   -1,  319,  276,   -1,   -1,  323,  280,   -1,   -1,
   -1,   -1,   -1,   -1,  331,  332,  289,   -1,  335,   -1,
   -1,  338,   -1,  296,   -1,  266,   -1,  268,  301,   -1,
  271,  448,  305,   -1,   -1,  276,   -1,   -1,   -1,  280,
   -1,   -1,   -1,   -1,  317,   -1,  319,   -1,  289,   -1,
  323,   -1,   -1,   -1,   -1,  296,   -1,   -1,  331,  332,
  301,   -1,  335,  448,  305,  338,   -1,   -1,   -1,  266,
   -1,  268,   -1,   -1,  271,   -1,  317,   -1,  319,  276,
   -1,   -1,  323,  280,   -1,   -1,   -1,   -1,   -1,   -1,
  331,  332,  289,   -1,  335,   -1,   -1,  338,   -1,  296,
   -1,   -1,   -1,   -1,  301,   -1,   -1,  448,  305,   -1,
   -1,   -1,   -1,  266,   -1,  268,   -1,   -1,  271,   -1,
  317,   -1,  319,  276,   -1,   -1,  323,  280,   -1,   -1,
   -1,  448,   -1,   -1,  331,  332,  289,  266,  335,  268,
   -1,  338,  271,  296,  256,   -1,   -1,  276,  301,   -1,
  262,  280,  305,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
  289,   -1,   -1,   -1,  317,   -1,  319,  296,   -1,   -1,
  323,   -1,  301,   -1,   -1,  448,  305,   -1,  331,  332,
   -1,   -1,  335,   -1,   -1,  338,   -1,  299,  317,   -1,
  319,   -1,   -1,   -1,  323,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,  331,  332,   -1,   -1,  335,  448,   -1,  338,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,  340,   -1,
   -1,   -1,   -1,   -1,  346,  347,  348,  349,  350,  351,
  352,  353,  354,  355,  356,  357,  256,   -1,   -1,   -1,
   -1,  448,  262,  365,   -1,  367,   -1,  369,   -1,  371,
  372,  373,   -1,  375,  376,   -1,  378,  379,   -1,  381,
   -1,  383,  384,  385,  386,  387,  388,  389,   -1,   -1,
   -1,   -1,   -1,   -1,  396,   -1,  398,   -1,  400,  299,
  402,   -1,  404,   -1,  406,  448,  408,   -1,  410,   -1,
  412,   -1,  414,   -1,  416,   -1,  418,   -1,  420,   -1,
  422,   -1,  424,   -1,  426,   -1,  428,  256,  430,  448,
   -1,   -1,  434,  262,   -1,   -1,   -1,   -1,   -1,   -1,
  340,   -1,   -1,   -1,   -1,   -1,  346,  347,  348,  349,
  350,  351,  352,  353,  354,  355,  356,  357,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,  365,  366,  367,   -1,  369,
  299,  371,  372,  373,   -1,  375,  376,   -1,   -1,  379,
   -1,  381,   -1,  383,  384,  385,  386,  387,  388,  389,
   -1,   -1,   -1,   -1,   -1,   -1,  396,   -1,  398,   -1,
  400,   -1,  402,   -1,  404,   -1,  406,   -1,  408,   -1,
  410,  340,   -1,   -1,   -1,   -1,   -1,  346,  347,  348,
  349,  350,  351,  352,  353,  354,  355,  356,  357,  256,
   -1,   -1,   -1,   -1,  434,  262,  365,  366,  367,   -1,
  369,   -1,  371,  372,  373,   -1,  375,  376,   -1,   -1,
  379,   -1,  381,   -1,  383,  384,   -1,   -1,   -1,  388,
  389,   -1,   -1,   -1,   -1,   -1,   -1,  396,   -1,  398,
   -1,  400,  299,  402,   -1,  404,   -1,  406,   -1,  408,
   -1,  410,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
  256,   -1,   -1,   -1,   -1,  434,  262,   -1,   -1,   -1,
   -1,   -1,   -1,  340,   -1,   -1,   -1,   -1,   -1,  346,
  347,  348,  349,  350,  351,  352,  353,  354,  355,  356,
  357,   -1,   -1,   -1,   -1,   -1,   -1,   -1,  365,  366,
  367,   -1,  369,  299,  371,  372,  373,   -1,  375,  376,
   -1,   -1,  379,   -1,  381,   -1,  383,  384,   -1,   -1,
   -1,  388,  389,   -1,   -1,   -1,   -1,   -1,   -1,  396,
   -1,  398,   -1,  400,   -1,  402,   -1,  404,   -1,  406,
   -1,  408,   -1,  410,  340,   -1,   -1,   -1,   -1,   -1,
  346,  347,  348,  349,  350,  351,  352,  353,  354,  355,
  356,  357,  256,   -1,   -1,   -1,   -1,  434,  262,  365,
  366,  367,   -1,  369,   -1,  371,  372,  373,   -1,  375,
  376,   -1,   -1,  379,   -1,  381,   -1,  383,  384,   -1,
   -1,   -1,  388,  389,   -1,   -1,   -1,   -1,   -1,   -1,
  396,   -1,  398,   -1,  400,  299,  402,   -1,  404,   -1,
  406,   -1,  408,   -1,  410,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,  256,   -1,   -1,   -1,   -1,  434,  262,
   -1,   -1,   -1,   -1,   -1,   -1,  340,   -1,   -1,   -1,
   -1,   -1,  346,  347,  348,  349,  350,  351,  352,  353,
  354,  355,  356,  357,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,  365,  366,  367,   -1,  369,  299,  371,  372,  373,
   -1,   -1,   -1,   -1,   -1,  379,   -1,  381,   -1,  383,
  384,  385,   -1,   -1,  388,  389,   -1,   -1,   -1,   -1,
   -1,   -1,  396,   -1,  398,   -1,  400,   -1,  402,  256,
  404,   -1,  406,   -1,  408,  262,  410,  340,   -1,   -1,
   -1,   -1,   -1,  346,  347,  348,  349,  350,  351,  352,
  353,  354,  355,  356,  357,   -1,   -1,   -1,   -1,   -1,
  434,   -1,  365,  366,  367,   -1,  369,   -1,  371,  372,
  373,   -1,  299,   -1,   -1,   -1,  379,   -1,  381,   -1,
  383,  384,  385,   -1,   -1,  388,  389,   -1,   -1,   -1,
   -1,   -1,   -1,  396,   -1,  398,   -1,  400,   -1,  402,
  256,  404,   -1,  406,   -1,  408,  262,  410,   -1,   -1,
   -1,   -1,   -1,  340,   -1,   -1,   -1,   -1,   -1,  346,
  347,  348,  349,  350,  351,  352,  353,  354,  355,  356,
  357,  434,   -1,   -1,   -1,   -1,   -1,   -1,  365,  366,
  367,   -1,  369,  299,  371,  372,  373,   -1,   -1,   -1,
   -1,   -1,  379,   -1,  381,   -1,  383,  384,  385,   -1,
   -1,  388,  389,   -1,   -1,   -1,   -1,   -1,   -1,  396,
   -1,  398,   -1,  400,   -1,  402,  256,  404,   -1,  406,
   -1,  408,  262,  410,  340,   -1,   -1,   -1,   -1,   -1,
  346,  347,  348,  349,  350,  351,  352,  353,  354,  355,
  356,  357,   -1,   -1,   -1,   -1,   -1,  434,   -1,  365,
  366,  367,   -1,  369,   -1,  371,  372,  373,   -1,  299,
   -1,   -1,   -1,  379,   -1,  381,   -1,  383,  384,  385,
   -1,   -1,  388,  389,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,  400,   -1,  402,  256,  404,   -1,
  406,   -1,  408,  262,  410,   -1,   -1,   -1,   -1,   -1,
  340,   -1,   -1,   -1,   -1,   -1,  346,  347,  348,  349,
  350,  351,  352,  353,  354,  355,  356,  357,  434,   -1,
   -1,   -1,   -1,   -1,   -1,  365,  366,  367,   -1,  369,
  299,  371,  372,  373,   -1,   -1,   -1,   -1,   -1,  379,
   -1,  381,   -1,  383,  384,  385,   -1,   -1,  388,  389,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
  400,   -1,  402,  256,  404,   -1,  406,   -1,  408,  262,
  410,  340,   -1,   -1,   -1,   -1,   -1,  346,  347,  348,
  349,  350,  351,  352,  353,  354,  355,  356,  357,   -1,
   -1,   -1,   -1,   -1,  434,   -1,  365,  366,  367,   -1,
  369,   -1,  371,  372,  373,   -1,  299,   -1,   -1,   -1,
  379,   -1,  381,   -1,  383,  384,  385,   -1,   -1,  388,
  389,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,  400,   -1,  402,  256,  404,   -1,  406,   -1,  408,
  262,  410,   -1,   -1,   -1,   -1,   -1,  340,   -1,   -1,
   -1,   -1,   -1,  346,  347,  348,  349,  350,  351,  352,
  353,  354,  355,  356,  357,  434,   -1,   -1,   -1,   -1,
   -1,   -1,  365,  366,  367,   -1,  369,  299,  371,  372,
  373,   -1,   -1,   -1,   -1,   -1,  379,   -1,  381,   -1,
  383,  384,  385,   -1,   -1,  388,  389,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,  256,   -1,  400,   -1,  402,
   -1,  404,   -1,  406,   -1,  408,   -1,  410,  340,   -1,
   -1,   -1,   -1,   -1,  346,  347,  348,  349,  350,  351,
  352,  353,  354,  355,  356,  357,   -1,   -1,   -1,   -1,
   -1,  434,   -1,  365,  366,  367,   -1,  369,   -1,  371,
  372,  373,   -1,   -1,   -1,   -1,   -1,  379,   -1,  381,
   -1,  383,  384,  385,   -1,   -1,  388,  389,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,  256,   -1,  400,   -1,
  402,   -1,  404,   -1,  406,   -1,  408,   -1,  410,  340,
   -1,   -1,   -1,   -1,   -1,  346,  347,  348,  349,  350,
  351,  352,  353,  354,  355,  356,  357,   -1,   -1,   -1,
   -1,   -1,  434,   -1,  365,  366,  367,   -1,  369,   -1,
  371,  372,  373,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,  383,  384,  385,   -1,   -1,  388,  389,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,  256,   -1,   -1,
   -1,   -1,   -1,  404,   -1,  406,   -1,  408,   -1,  410,
  340,   -1,   -1,   -1,   -1,   -1,  346,  347,  348,  349,
  350,  351,  352,  353,  354,  355,  356,  357,   -1,   -1,
   -1,   -1,   -1,  434,   -1,  365,  366,  367,   -1,  369,
   -1,  371,  372,  373,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,  383,  384,  385,   -1,   -1,  388,  389,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,  256,   -1,
   -1,   -1,   -1,   -1,  404,   -1,  406,   -1,  408,   -1,
  410,  340,   -1,   -1,   -1,   -1,   -1,  346,  347,  348,
  349,  350,  351,  352,  353,  354,  355,  356,  357,   -1,
   -1,   -1,   -1,   -1,  434,   -1,  365,  366,  367,   -1,
  369,   -1,  371,  372,  373,  256,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,  383,  384,  385,   -1,   -1,  388,
  389,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,  404,   -1,  406,   -1,  408,
   -1,  410,  340,   -1,   -1,   -1,   -1,   -1,  346,  347,
  348,  349,  350,  351,  352,  353,  354,  355,  356,  357,
   -1,   -1,   -1,   -1,   -1,  434,   -1,  365,  366,  367,
   -1,  369,   -1,  371,  372,  373,  256,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,  383,  384,  385,   -1,  340,
  388,  389,   -1,   -1,   -1,  346,  347,  348,  349,  350,
  351,  352,  353,  354,  355,  356,  357,   -1,   -1,   -1,
  408,   -1,  410,   -1,  365,  366,  367,   -1,  369,   -1,
  371,  372,  373,  256,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,  383,  384,  385,   -1,  434,  388,  389,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,  408,   -1,  410,
  340,   -1,   -1,   -1,   -1,   -1,  346,  347,  348,  349,
  350,  351,  352,  353,  354,  355,  356,  357,   -1,   -1,
   -1,   -1,   -1,  434,   -1,  365,  366,  367,   -1,  369,
   -1,  371,  372,  373,  256,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,  384,  385,   -1,  340,  388,  389,
   -1,   -1,   -1,  346,  347,  348,  349,  350,  351,  352,
  353,  354,  355,  356,  357,   -1,   -1,   -1,  408,   -1,
  410,   -1,  365,  366,  367,   -1,  369,   -1,  371,  372,
  373,  256,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,  384,  385,   -1,  434,  388,  389,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,  408,   -1,  410,  340,   -1,
   -1,   -1,   -1,   -1,  346,  347,  348,  349,  350,  351,
  352,  353,  354,  355,  356,  357,   -1,   -1,   -1,   -1,
   -1,  434,   -1,  365,  366,  367,   -1,  369,   -1,  371,
  372,  373,  256,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,  384,  385,   -1,  340,   -1,  389,   -1,   -1,
   -1,  346,  347,  348,  349,  350,  351,  352,  353,  354,
  355,  356,  357,   -1,   -1,   -1,  408,   -1,  410,   -1,
  365,  366,  367,   -1,  369,   -1,  371,  372,  373,  256,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,  384,
  385,   -1,  434,   -1,  389,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,  408,   -1,  410,  340,   -1,   -1,   -1,
   -1,   -1,  346,  347,  348,  349,  350,  351,  352,  353,
  354,  355,  356,  357,   -1,   -1,   -1,   -1,   -1,  434,
   -1,  365,  366,  367,   -1,  369,   -1,  371,  372,  373,
  256,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,  385,   -1,  340,   -1,  389,   -1,   -1,   -1,  346,
  347,  348,  349,  350,  351,  352,  353,  354,  355,  356,
  357,   -1,   -1,   -1,  408,   -1,  410,  256,  365,  366,
  367,   -1,  369,  262,  371,  372,  373,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,  385,   -1,
  434,   -1,  389,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
  299,  408,   -1,  410,  340,   -1,  262,   -1,   -1,   -1,
  346,  347,  348,  349,  350,  351,  352,  353,  354,  355,
  356,  357,   -1,   -1,   -1,   -1,   -1,  434,   -1,  365,
  366,  367,   -1,  369,   -1,  371,  372,  373,   -1,   -1,
   -1,   -1,   -1,  299,   -1,   -1,   -1,   -1,   -1,  385,
   -1,   -1,   -1,  389,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,  366,   -1,   -1,
  369,   -1,  371,   -1,  410,   -1,  375,  376,   -1,  262,
  379,   -1,  381,   -1,  383,  384,   -1,   -1,   -1,  388,
  389,   -1,   -1,   -1,   -1,   -1,   -1,  396,  434,  398,
   -1,  400,   -1,  402,   -1,  404,   -1,  406,   -1,  408,
  366,  410,  368,  369,  370,  371,  299,   -1,   -1,  375,
  376,   -1,   -1,  379,   -1,  381,   -1,  383,  384,  385,
  386,  387,  388,  389,   -1,  434,  392,   -1,  394,   -1,
  396,   -1,  398,   -1,  400,   -1,  402,   -1,  404,   -1,
  406,   -1,  408,   -1,  410,   -1,  412,   -1,  414,   -1,
  416,   -1,  418,   -1,  420,   -1,  422,   -1,  424,   -1,
  426,   -1,  428,   -1,  430,   -1,  432,   -1,  434,   -1,
   -1,   -1,  365,  366,   -1,  368,   -1,  370,  371,   -1,
   -1,   -1,  375,  376,   -1,   -1,  379,   -1,  381,   -1,
  383,  384,  385,  386,  387,  388,  389,   -1,  261,  392,
   -1,  394,   -1,  396,   -1,  398,   -1,  400,   -1,  402,
   -1,  404,   -1,  406,   -1,  408,   -1,  410,   -1,   -1,
   -1,   -1,  285,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,  261,   -1,  298,   -1,   -1,   -1,  432,
  303,  434,   -1,  306,   -1,  308,   -1,  310,  311,  312,
  313,   -1,   -1,   -1,   -1,  318,   -1,  285,   -1,  322,
   -1,   -1,   -1,  326,   -1,   -1,   -1,   -1,  261,   -1,
  298,  334,   -1,   -1,  337,  303,  339,   -1,   -1,   -1,
  308,   -1,  310,  311,  312,  313,   -1,   -1,   -1,   -1,
  318,   -1,  285,   -1,  322,   -1,   -1,   -1,  261,   -1,
  263,   -1,   -1,  366,   -1,  298,  334,   -1,   -1,  337,
  303,  339,   -1,   -1,   -1,  308,   -1,  310,  311,  312,
  313,   -1,  285,  316,   -1,  318,  389,   -1,  261,  322,
  263,  359,   -1,  361,   -1,  298,   -1,  365,   -1,   -1,
  303,  334,   -1,   -1,  337,  308,  339,  310,  311,  312,
  313,   -1,  285,   -1,   -1,  318,   -1,   -1,  261,  322,
   -1,   -1,   -1,   -1,   -1,  298,   -1,   -1,   -1,   -1,
  303,  334,  365,   -1,  337,  308,  339,  310,  311,  312,
  313,   -1,  285,  316,   -1,  318,   -1,   -1,  261,  322,
   -1,   -1,   -1,   -1,   -1,  298,   -1,   -1,   -1,  302,
  303,  334,  365,   -1,  337,  308,  339,  310,  311,  312,
  313,   -1,  285,   -1,   -1,  318,   -1,   -1,  261,  322,
  263,   -1,   -1,   -1,   -1,  298,   -1,   -1,   -1,   -1,
  303,  334,   -1,   -1,  337,  308,  339,  310,  311,  312,
  313,   -1,  285,  316,   -1,  318,   -1,   -1,  261,  322,
   -1,   -1,   -1,   -1,   -1,  298,   -1,   -1,   -1,   -1,
  303,  334,   -1,   -1,  337,  308,  339,  310,  311,  312,
  313,   -1,  285,   -1,   -1,  318,   -1,   -1,  261,  322,
  263,   -1,   -1,   -1,   -1,  298,   -1,   -1,   -1,   -1,
  303,  334,   -1,   -1,  337,  308,  339,  310,  311,  312,
  313,   -1,  285,  316,   -1,  318,   -1,   -1,  261,  322,
   -1,   -1,   -1,   -1,   -1,  298,   -1,   -1,   -1,   -1,
  303,  334,   -1,   -1,  337,  308,  339,  310,  311,  312,
  313,   -1,  285,   -1,   -1,  318,   -1,   -1,  261,  322,
   -1,   -1,   -1,   -1,   -1,  298,   -1,   -1,   -1,   -1,
  303,  334,   -1,   -1,  337,  308,  339,  310,  311,  312,
  313,   -1,  285,   -1,   -1,  318,   -1,   -1,   -1,  322,
   -1,   -1,   -1,   -1,   -1,  298,   -1,   -1,   -1,   -1,
  303,  334,   -1,   -1,  337,  308,  339,  310,  311,  312,
  313,   -1,   -1,   -1,   -1,  318,   -1,   -1,   -1,  322,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,  334,   -1,   -1,  337,   -1,  339,
  };

#line 5614 "cs-parser.jay"

// <summary>
//   A class used to pass around variable declarations and constants
// </summary>
public class VariableDeclaration {
	public string identifier;
	public Expression expression_or_array_initializer;
	public Location Location;
	public Attributes OptAttributes;
	public string DocComment;

	public VariableDeclaration (LocatedToken lt, object eoai, Attributes opt_attrs)
	{
		this.identifier = lt.Value;
		if (eoai is ArrayList) {
			this.expression_or_array_initializer = new ArrayCreation (CSharpParser.current_array_type, "", (ArrayList)eoai, lt.Location);
		} else {
			this.expression_or_array_initializer = (Expression)eoai;
		}
		this.Location = lt.Location;
		this.OptAttributes = opt_attrs;
	}

	public VariableDeclaration (LocatedToken lt, object eoai) : this (lt, eoai, null)
	{
	}
}

// <summary>
//   A class used to hold info about an indexer declarator
// </summary>
public class IndexerDeclaration {
	public Expression type;
	public MemberName interface_type;
	public Parameters param_list;
	public Location location;

	public IndexerDeclaration (Expression type, MemberName interface_type,
				   Parameters param_list, Location loc)
	{
		this.type = type;
		this.interface_type = interface_type;
		this.param_list = param_list;
		this.location = loc;
	}
}

//
// We use this when we do not have an object in advance that is an IAnonymousHost
//
public class SimpleAnonymousHost : IAnonymousHost {
	public static readonly SimpleAnonymousHost Simple = new SimpleAnonymousHost ();

	bool yields;
	ArrayList anonymous_methods;

	public static SimpleAnonymousHost GetSimple () {
		Simple.yields = false;
		Simple.anonymous_methods = null;
		return Simple;
	}

	public void SetYields ()
	{
		yields = true;
	}

	public void AddAnonymousMethod (AnonymousMethodExpression anonymous)
	{
		if (anonymous_methods == null)
			anonymous_methods = new ArrayList ();
		anonymous_methods.Add (anonymous);
	}

	public void Propagate (IAnonymousHost real_host)
	{
		if (yields)
			real_host.SetYields ();
		if (anonymous_methods != null) {
			foreach (AnonymousMethodExpression ame in anonymous_methods)
				real_host.AddAnonymousMethod (ame);
		}
	}
}

// <summary>
//  A class used to hold info about an operator declarator
// </summary>
struct OperatorDeclaration {
	public readonly Operator.OpType optype;
	public readonly Expression ret_type;
	public readonly Location location;

	public OperatorDeclaration (Operator.OpType op, Expression ret_type, Location location)
	{
		optype = op;
		this.ret_type = ret_type;
		this.location = location;
	}
}

void Error_ExpectingTypeName (Expression expr)
{
	if (expr is Invocation){
		Report.Error (1002, expr.Location, "Expecting `;'");
	} else {
		expr.Error_InvalidExpressionStatement ();
	}
}

static void Error_ParameterModifierNotValid (string modifier, Location loc)
{
	Report.Error (631, loc, "The parameter modifier `{0}' is not valid in this context",
			              modifier);
}

static void Error_DuplicateParameterModifier (Location loc, Parameter.Modifier mod)
{
	Report.Error (1107, loc, "Duplicate parameter modifier `{0}'",
  		Parameter.GetModifierSignature (mod));
}

static void Error_TypeExpected (Location loc)
{
	Report.Error (1031, loc, "Type expected");
}

void push_current_class (TypeContainer tc, object partial_token)
{
	if (partial_token != null)
		current_container = current_container.AddPartial (tc);
	else
		current_container = current_container.AddTypeContainer (tc);
		
	current_namespace.AddType (tc);
	current_class = tc;
}

DeclSpace pop_current_class ()
{
	DeclSpace retval = current_class;

	current_class = current_class.Parent;
	current_container = current_class.PartialContainer;

	return retval;
}

// <summary>
//   Given the @class_name name, it creates a fully qualified name
//   based on the containing declaration space
// </summary>
MemberName
MakeName (MemberName class_name)
{
	Namespace ns = current_namespace.NS;

	if (current_container.Name.Length == 0){
		if (ns.Name.Length != 0)
			return new MemberName (ns.MemberName, class_name);
		else
			return class_name;
	} else {
		return new MemberName (current_container.MemberName, class_name);
	}
}

Block declare_local_variables (Expression type, ArrayList variable_declarators, Location loc)
{
	Block implicit_block;
	ArrayList inits = null;

	//
	// We use the `Used' property to check whether statements
	// have been added to the current block.  If so, we need
	// to create another block to contain the new declaration
	// otherwise, as an optimization, we use the same block to
	// add the declaration.
	//
	// FIXME: A further optimization is to check if the statements
	// that were added were added as part of the initialization
	// below.  In which case, no other statements have been executed
	// and we might be able to reduce the number of blocks for
	// situations like this:
	//
	// int j = 1;  int k = j + 1;
	//
	if (current_block.Used)
		implicit_block = new Block (current_block, loc, Location.Null);
	else
		implicit_block = current_block;

	foreach (VariableDeclaration decl in variable_declarators){

		if (implicit_block.AddVariable (type, decl.identifier, decl.Location) != null) {
			if (decl.expression_or_array_initializer != null){
				if (inits == null)
					inits = new ArrayList (4);
				inits.Add (decl);
			}
		}
	}

	if (inits == null)
		return implicit_block;

	foreach (VariableDeclaration decl in inits){
		Assign assign;
		Expression expr = decl.expression_or_array_initializer;
		
		LocalVariableReference var;
		var = new LocalVariableReference (implicit_block, decl.identifier, loc);

		assign = new Assign (var, expr, decl.Location);

		implicit_block.AddStatement (new StatementExpression (assign));
	}
	
	return implicit_block;
}

Block declare_local_constants (Expression type, ArrayList declarators)
{
	Block implicit_block;

	if (current_block.Used)
		implicit_block = new Block (current_block);
	else
		implicit_block = current_block;

	foreach (VariableDeclaration decl in declarators){
		implicit_block.AddConstant (type, decl.identifier, (Expression) decl.expression_or_array_initializer, decl.Location);
	}
	
	return implicit_block;
}

string CheckAttributeTarget (string a, Location l)
{
	switch (a) {
	case "assembly" : case "module" : case "field" : case "method" : case "param" : case "property" : case "type" :
			return a;
	}

	Report.Warning (658, 1, l,
		 "`{0}' is invalid attribute target. All attributes in this attribute section will be ignored", a);
	return string.Empty;
}

static bool IsUnaryOperator (Operator.OpType op)
{
	switch (op) {
		
	case Operator.OpType.LogicalNot: 
	case Operator.OpType.OnesComplement: 
	case Operator.OpType.Increment:
	case Operator.OpType.Decrement:
	case Operator.OpType.True: 
	case Operator.OpType.False: 
	case Operator.OpType.UnaryPlus: 
	case Operator.OpType.UnaryNegation:
		return true;
	}
	return false;
}

void syntax_error (Location l, string msg)
{
	Report.Error (1003, l, "Syntax error, " + msg);
}

void note (string s)
{
	// Used to put annotations
}

Tokenizer lexer;

public Tokenizer Lexer {
	get {
		return lexer;
	}
}		   

static CSharpParser ()
{
	oob_stack = new Stack ();
}

public CSharpParser (SeekableStreamReader reader, SourceFile file, ArrayList defines, CompilationUnit cu)
{
	this.file = file;
	this.cu = cu;
	
	current_namespace = cu.DefaultNamespace;
	current_class = current_namespace.SlaveDeclSpace;
	current_container = current_class.PartialContainer; // == RootContest.ToplevelTypes
	oob_stack.Clear ();
	lexer = new Tokenizer (reader, file, defines);
}

public void parse ()
{
	int errors = Report.Errors;
	try {
		if (yacc_verbose_flag > 1)
			yyparse (lexer, new yydebug.yyDebugSimple ());
		else
			yyparse (lexer);
		Tokenizer tokenizer = lexer as Tokenizer;
		tokenizer.cleanup ();
	} catch (Exception e){
		//
		// Removed for production use, use parser verbose to get the output.
		//
		// Console.WriteLine (e);
		if (Report.Errors == errors)
			Report.Error (-25, lexer.Location, "Parsing error");
		if (yacc_verbose_flag > 0)
			Console.WriteLine (e);
	}

	if (RootContext.ToplevelTypes.NamespaceEntry != null)
		throw new InternalErrorException ("who set it?");
}

static void CheckToken (int error, int yyToken, string msg, Location loc)
{
	if (yyToken >= Token.FIRST_KEYWORD && yyToken <= Token.LAST_KEYWORD)
		Report.Error (error, loc, "{0}: `{1}' is a keyword", msg, yyNames [yyToken].ToLower ());
	else
		Report.Error (error, loc, msg);
}

void CheckIdentifierToken (int yyToken, Location loc)
{
	CheckToken (1041, yyToken, "Identifier expected", loc);
}

string ConsumeStoredComment ()
{
	string s = tmpComment;
	tmpComment = null;
	Lexer.doc_state = XmlCommentState.Allowed;
	return s;
}

Location GetLocation (object obj)
{
	if (obj is MemberCore)
		return ((MemberCore) obj).Location;
	if (obj is MemberName)
		return ((MemberName) obj).Location;
	if (obj is LocatedToken)
		return ((LocatedToken) obj).Location;
	if (obj is Location)
		return (Location) obj;
	return lexer.Location;
}

void start_block (Location loc)
{
	if (current_block == null || parsing_anonymous_method) {
		current_block = new ToplevelBlock (current_block, current_local_parameters, current_generic_method, loc);
		parsing_anonymous_method = false;
	} else {
		current_block = new ExplicitBlock (current_block, loc, Location.Null);
	}
}

Block
end_block (Location loc)
{
	Block retval = current_block.Explicit;
	retval.SetEndLocation (loc);
	current_block = retval.Parent;
	return retval;
}

void
start_anonymous (bool lambda, Parameters parameters, Location loc)
{
	oob_stack.Push (current_anonymous_method);
	oob_stack.Push (current_local_parameters);

	current_local_parameters = parameters;

	ToplevelBlock top_current_block = current_block == null ? null : current_block.Toplevel;

	current_anonymous_method = lambda 
		? new LambdaExpression (
			current_anonymous_method, current_generic_method, current_container,
			parameters, top_current_block, loc) 
		: new AnonymousMethodExpression (
			current_anonymous_method, current_generic_method, current_container,
			parameters, top_current_block, loc);

	// Force the next block to be created as a ToplevelBlock
	parsing_anonymous_method = true;
}

/*
 * Completes the anonymous method processing, if lambda_expr is null, this
 * means that we have a Statement instead of an Expression embedded 
 */
AnonymousMethodExpression 
end_anonymous (ToplevelBlock anon_block, Location loc)
{
	AnonymousMethodExpression retval;

	if (RootContext.Version == LanguageVersion.ISO_1){
		Report.FeatureIsNotAvailable (loc, "anonymous methods");
		retval = null;
	} else  {
		current_anonymous_method.Block = anon_block;
		if ((anonymous_host != null) && (current_anonymous_method.Parent == null))
			anonymous_host.AddAnonymousMethod (current_anonymous_method);

		retval = current_anonymous_method;
	}

	current_local_parameters = (Parameters) oob_stack.Pop ();
	current_anonymous_method = (AnonymousMethodExpression) oob_stack.Pop ();

	return retval;
}

/* end end end */
}
#line default
namespace yydebug {
        using System;
	 internal interface yyDebug {
		 void push (int state, Object value);
		 void lex (int state, int token, string name, Object value);
		 void shift (int from, int to, int errorFlag);
		 void pop (int state);
		 void discard (int state, int token, string name, Object value);
		 void reduce (int from, int to, int rule, string text, int len);
		 void shift (int from, int to);
		 void accept (Object value);
		 void error (string message);
		 void reject ();
	 }
	 
	 class yyDebugSimple : yyDebug {
		 void println (string s){
			 Console.Error.WriteLine (s);
		 }
		 
		 public void push (int state, Object value) {
			 println ("push\tstate "+state+"\tvalue "+value);
		 }
		 
		 public void lex (int state, int token, string name, Object value) {
			 println("lex\tstate "+state+"\treading "+name+"\tvalue "+value);
		 }
		 
		 public void shift (int from, int to, int errorFlag) {
			 switch (errorFlag) {
			 default:				// normally
				 println("shift\tfrom state "+from+" to "+to);
				 break;
			 case 0: case 1: case 2:		// in error recovery
				 println("shift\tfrom state "+from+" to "+to
					     +"\t"+errorFlag+" left to recover");
				 break;
			 case 3:				// normally
				 println("shift\tfrom state "+from+" to "+to+"\ton error");
				 break;
			 }
		 }
		 
		 public void pop (int state) {
			 println("pop\tstate "+state+"\ton error");
		 }
		 
		 public void discard (int state, int token, string name, Object value) {
			 println("discard\tstate "+state+"\ttoken "+name+"\tvalue "+value);
		 }
		 
		 public void reduce (int from, int to, int rule, string text, int len) {
			 println("reduce\tstate "+from+"\tuncover "+to
				     +"\trule ("+rule+") "+text);
		 }
		 
		 public void shift (int from, int to) {
			 println("goto\tfrom state "+from+" to "+to);
		 }
		 
		 public void accept (Object value) {
			 println("accept\tvalue "+value);
		 }
		 
		 public void error (string message) {
			 println("error\t"+message);
		 }
		 
		 public void reject () {
			 println("reject");
		 }
		 
	 }
}
// %token constants
 class Token {
  public const int EOF = 257;
  public const int NONE = 258;
  public const int ERROR = 259;
  public const int FIRST_KEYWORD = 260;
  public const int ABSTRACT = 261;
  public const int AS = 262;
  public const int ADD = 263;
  public const int ASSEMBLY = 264;
  public const int BASE = 265;
  public const int BOOL = 266;
  public const int BREAK = 267;
  public const int BYTE = 268;
  public const int CASE = 269;
  public const int CATCH = 270;
  public const int CHAR = 271;
  public const int CHECKED = 272;
  public const int CLASS = 273;
  public const int CONST = 274;
  public const int CONTINUE = 275;
  public const int DECIMAL = 276;
  public const int DEFAULT = 277;
  public const int DELEGATE = 278;
  public const int DO = 279;
  public const int DOUBLE = 280;
  public const int ELSE = 281;
  public const int ENUM = 282;
  public const int EVENT = 283;
  public const int EXPLICIT = 284;
  public const int EXTERN = 285;
  public const int FALSE = 286;
  public const int FINALLY = 287;
  public const int FIXED = 288;
  public const int FLOAT = 289;
  public const int FOR = 290;
  public const int FOREACH = 291;
  public const int GOTO = 292;
  public const int IF = 293;
  public const int IMPLICIT = 294;
  public const int IN = 295;
  public const int INT = 296;
  public const int INTERFACE = 297;
  public const int INTERNAL = 298;
  public const int IS = 299;
  public const int LOCK = 300;
  public const int LONG = 301;
  public const int NAMESPACE = 302;
  public const int NEW = 303;
  public const int NULL = 304;
  public const int OBJECT = 305;
  public const int OPERATOR = 306;
  public const int OUT = 307;
  public const int OVERRIDE = 308;
  public const int PARAMS = 309;
  public const int PRIVATE = 310;
  public const int PROTECTED = 311;
  public const int PUBLIC = 312;
  public const int READONLY = 313;
  public const int REF = 314;
  public const int RETURN = 315;
  public const int REMOVE = 316;
  public const int SBYTE = 317;
  public const int SEALED = 318;
  public const int SHORT = 319;
  public const int SIZEOF = 320;
  public const int STACKALLOC = 321;
  public const int STATIC = 322;
  public const int STRING = 323;
  public const int STRUCT = 324;
  public const int SWITCH = 325;
  public const int THIS = 326;
  public const int THROW = 327;
  public const int TRUE = 328;
  public const int TRY = 329;
  public const int TYPEOF = 330;
  public const int UINT = 331;
  public const int ULONG = 332;
  public const int UNCHECKED = 333;
  public const int UNSAFE = 334;
  public const int USHORT = 335;
  public const int USING = 336;
  public const int VIRTUAL = 337;
  public const int VOID = 338;
  public const int VOLATILE = 339;
  public const int WHERE = 340;
  public const int WHILE = 341;
  public const int ARGLIST = 342;
  public const int PARTIAL = 343;
  public const int ARROW = 344;
  public const int QUERY_FIRST_TOKEN = 345;
  public const int FROM = 346;
  public const int JOIN = 347;
  public const int ON = 348;
  public const int EQUALS = 349;
  public const int SELECT = 350;
  public const int GROUP = 351;
  public const int BY = 352;
  public const int LET = 353;
  public const int ORDERBY = 354;
  public const int ASCENDING = 355;
  public const int DESCENDING = 356;
  public const int INTO = 357;
  public const int QUERY_LAST_TOKEN = 358;
  public const int GET = 359;
  public const int get = 360;
  public const int SET = 361;
  public const int set = 362;
  public const int LAST_KEYWORD = 363;
  public const int OPEN_BRACE = 364;
  public const int CLOSE_BRACE = 365;
  public const int OPEN_BRACKET = 366;
  public const int CLOSE_BRACKET = 367;
  public const int OPEN_PARENS = 368;
  public const int CLOSE_PARENS = 369;
  public const int DOT = 370;
  public const int COMMA = 371;
  public const int COLON = 372;
  public const int SEMICOLON = 373;
  public const int TILDE = 374;
  public const int PLUS = 375;
  public const int MINUS = 376;
  public const int BANG = 377;
  public const int ASSIGN = 378;
  public const int OP_LT = 379;
  public const int OP_GENERICS_LT = 380;
  public const int OP_GT = 381;
  public const int OP_GENERICS_GT = 382;
  public const int BITWISE_AND = 383;
  public const int BITWISE_OR = 384;
  public const int STAR = 385;
  public const int PERCENT = 386;
  public const int DIV = 387;
  public const int CARRET = 388;
  public const int INTERR = 389;
  public const int DOUBLE_COLON = 390;
  public const int OP_INC = 392;
  public const int OP_DEC = 394;
  public const int OP_SHIFT_LEFT = 396;
  public const int OP_SHIFT_RIGHT = 398;
  public const int OP_LE = 400;
  public const int OP_GE = 402;
  public const int OP_EQ = 404;
  public const int OP_NE = 406;
  public const int OP_AND = 408;
  public const int OP_OR = 410;
  public const int OP_MULT_ASSIGN = 412;
  public const int OP_DIV_ASSIGN = 414;
  public const int OP_MOD_ASSIGN = 416;
  public const int OP_ADD_ASSIGN = 418;
  public const int OP_SUB_ASSIGN = 420;
  public const int OP_SHIFT_LEFT_ASSIGN = 422;
  public const int OP_SHIFT_RIGHT_ASSIGN = 424;
  public const int OP_AND_ASSIGN = 426;
  public const int OP_XOR_ASSIGN = 428;
  public const int OP_OR_ASSIGN = 430;
  public const int OP_PTR = 432;
  public const int OP_COALESCING = 434;
  public const int LITERAL_INTEGER = 436;
  public const int LITERAL_FLOAT = 438;
  public const int LITERAL_DOUBLE = 440;
  public const int LITERAL_DECIMAL = 442;
  public const int LITERAL_CHARACTER = 444;
  public const int LITERAL_STRING = 446;
  public const int IDENTIFIER = 448;
  public const int OPEN_PARENS_LAMBDA = 449;
  public const int CLOSE_PARENS_CAST = 450;
  public const int CLOSE_PARENS_NO_CAST = 451;
  public const int CLOSE_PARENS_OPEN_PARENS = 452;
  public const int CLOSE_PARENS_MINUS = 453;
  public const int DEFAULT_OPEN_PARENS = 454;
  public const int GENERIC_DIMENSION = 455;
  public const int DEFAULT_COLON = 456;
  public const int LOWPREC = 457;
  public const int UMINUS = 458;
  public const int HIGHPREC = 459;
  public const int yyErrorCode = 256;
 }
 namespace yyParser {
  using System;
  /** thrown for irrecoverable syntax errors and stack overflow.
    */
  internal class yyException : System.Exception {
    public yyException (string message) : base (message) {
    }
  }

  /** must be implemented by a scanner object to supply input to the parser.
    */
  internal interface yyInput {
    /** move on to next token.
        @return false if positioned beyond tokens.
        @throws IOException on input error.
      */
    bool advance (); // throws java.io.IOException;
    /** classifies current token.
        Should not be called if advance() returned false.
        @return current %token or single character.
      */
    int token ();
    /** associated with current token.
        Should not be called if advance() returned false.
        @return value for token().
      */
    Object value ();
  }
 }
} // close outermost namespace, that MUST HAVE BEEN opened in the prolog
