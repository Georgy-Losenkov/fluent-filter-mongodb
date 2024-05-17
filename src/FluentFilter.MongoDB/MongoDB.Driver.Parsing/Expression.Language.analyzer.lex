%namespace MongoDB.Driver.Parsing
%scannertype ExpressionScanner
%visibility internal
%tokentype Token

%option minimize, parser, verbose, persistbuffer, noembedbuffers, nofiles, caseInsensitive, summary

// Keywords

AND       and
ANYOF     anyof
BETWEEN   between
EXIST     exist
FALSE     false
IN        in
IS        is
NOT       not
NULL      null
MATCH     match
OPTIONS   options
OR        or
TRUE      true
TYPEOF    typeof

// Operators

LT        <
LTE       <=
GT        >
GTE       >=
EQ        ==
NEQ       !=


// Delimiters

LPAREN    \(
RPAREN    \)
COMMA     \,

// Fragments

DIGIT             [0-9]
DIGITS            {DIGIT}+
IDENT             [A-Za-z_][A-Za-z0-9_]*
WS                [ \r\n\t]

// Literal

STRING            \"(\"\"|[^"])*\"

DATETIME_3        #{DIGIT}{4}-{DIGIT}{2}-{DIGIT}{2}#
DATETIME_5        #{DIGIT}{4}-{DIGIT}{2}-{DIGIT}{2}[ ]{DIGIT}{2}:{DIGIT}{2}#
DATETIME_6        #{DIGIT}{4}-{DIGIT}{2}-{DIGIT}{2}[ ]{DIGIT}{2}:{DIGIT}{2}:{DIGIT}{2}#
DATETIME_7        #{DIGIT}{4}-{DIGIT}{2}-{DIGIT}{2}[ ]{DIGIT}{2}:{DIGIT}{2}:{DIGIT}{2}\.{DIGITS}#
NUMBER            [+\-]?{DIGITS}(\.{DIGITS})?([Ee][+\-]?{DIGITS})?
BINARY_SIMPLE     binary\({WS}*{STRING}{WS}*\)
BINARY_EXTENDED   binary\({WS}*{STRING}{WS}*{COMMA}{WS}*{STRING}{WS}*\)
OBJECTID          objectid\({WS}*{STRING}{WS}*\)
UUID_SIMPLE       uuid\({WS}*{STRING}{WS}*\)
UUID_EXTENDED     uuid\({WS}*{STRING}{WS}*{COMMA}{WS}*{STRING}{WS}*\)
PATH_PLAIN        {IDENT}(\.({IDENT}|{DIGITS}))*
PATH_QUOTED       \`(\`\`|[^`])*\`
PATH_SELF         $
REGEX             \/(\/\/|[^/])*\/[imsx]*
EXPRESSION        $\{[^\}]+\}
WHITESPACE        {WS}+

%{
%}

// =============================================================
%%  // Start of rules
// =============================================================

/* Scanner body */

{WHITESPACE}+       /* skip */

{AND}               { return (Int32)Token.AND; }
{OR}                { return (Int32)Token.OR; }
{ANYOF}             { return (Int32)Token.ANYOF; }
{NOT}               { return (Int32)Token.NOT; }
{MATCH}             { return (Int32)Token.MATCH; }
{EXIST}             { return (Int32)Token.EXIST; }
{IN}                { return (Int32)Token.IN; }
{IS}                { return (Int32)Token.IS; }
{LPAREN}            { return (Int32)Token.LPAREN; }
{RPAREN}            { return (Int32)Token.RPAREN; }
{COMMA}             { return (Int32)Token.COMMA; }
{OPTIONS}           { return (Int32)Token.OPTIONS; }
{BETWEEN}           { return (Int32)Token.BETWEEN; }
{TYPEOF}            { return (Int32)Token.TYPEOF; }

{LT}                { return (Int32)Token.LT; }
{LTE}               { return (Int32)Token.LTE; }
{GT}                { return (Int32)Token.GT; }
{GTE}               { return (Int32)Token.GTE; }
{EQ}                { return (Int32)Token.EQ; }
{NEQ}               { return (Int32)Token.NEQ; }

{BINARY_SIMPLE}     { HandleSimpleBinary(); return (Int32)Token.LITERAL; }
{BINARY_EXTENDED}   { HandleExtendedBinary(); return (Int32)Token.LITERAL; }
{DATETIME_3}        { HandleDateTime3(); return (Int32)Token.LITERAL; }
{DATETIME_5}        { HandleDateTime5(); return (Int32)Token.LITERAL; }
{DATETIME_6}        { HandleDateTime6(); return (Int32)Token.LITERAL; }
{DATETIME_7}        { HandleDateTime7(); return (Int32)Token.LITERAL; }
{TRUE}              { HandleTrue(); return (Int32)Token.LITERAL; }
{FALSE}             { HandleFalse(); return (Int32)Token.LITERAL; }
{NULL}              { HandleNull(); return (Int32)Token.LITERAL; }
{NUMBER}            { HandleNumber(); return (Int32)Token.LITERAL; }
{OBJECTID}          { HandleObjectId(); return (Int32)Token.LITERAL; }
{REGEX}             { HandleRegex(); return (Int32)Token.LITERAL; }
{STRING}            { HandleString(); return (Int32)Token.LITERAL; }
{UUID_SIMPLE}       { HandleSimpleUuid(); return (Int32)Token.LITERAL; }
{UUID_EXTENDED}     { HandleExtendedUuid(); return (Int32)Token.LITERAL; }

{EXPRESSION}        { HandleExpression(); return (Int32)Token.EXPRESSION; }

{PATH_PLAIN}        { HandlePlainPath(); return (Int32)Token.PATH; }
{PATH_QUOTED}       { HandleQuotedPath(); return (Int32)Token.PATH; }
{PATH_SELF}         { HandleSelfPath(); return (Int32)Token.PATH; }


%%