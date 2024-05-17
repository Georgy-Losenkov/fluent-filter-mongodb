%namespace MongoDB.Driver.Parsing
%partial
%parsertype ExpressionParser
%visibility internal
%using MongoDB.Bson
%tokentype Token

%union {
        public Int32 Type;
        public String Text;
        public BsonValue Value;
        public Int32 Index;
    }

%start filter

%token AND, ANYOF, BETWEEN, COMMA ",", EQ, EXIST, EXPRESSION "${...}", GT, GTE, IN, IS, LITERAL, LPAREN "(", LT, LTE, MATCH, NEQ, NOT, OPTIONS, OR, PATH, RPAREN ")", TYPEOF

%%

filter
    : orGroup                                     { Success($1.Index); }
    ;

orGroup
    : orGroup2                                    { $$.Index = $1.Index; }
    | andGroup                                    { $$.Index = $1.Index; }
    ;

orGroup2
    : orGroup2 OR andGroup                        { $$.Index = AddEntry(EntryType.Or, $1.Index, $3.Index); }
    | andGroup OR andGroup
    {
    var index = AddEntry(EntryType.Or, index1: -1, $1.Index);
    $$.Index = AddEntry(EntryType.Or, index, $3.Index);
    }
    ;

andGroup
    : andGroup2                                   { $$.Index = $1.Index; }
    | primitive                                   { $$.Index = $1.Index; }
    ;

andGroup2
    : andGroup2 AND primitive                     { $$.Index = AddEntry(EntryType.And, $1.Index, $3.Index); }
    | primitive AND primitive
    {
    var index = AddEntry(EntryType.And, index1: -1, $1.Index);
    $$.Index = AddEntry(EntryType.And, index, $3.Index);
    }
    ;

primitive
    : PATH LT  value                              { $$.Index = AddTextEntry(EntryType.Lt , $1.Text, $3.Index); }
    | PATH LTE value                              { $$.Index = AddTextEntry(EntryType.Lte, $1.Text, $3.Index); }
    | PATH GT  value                              { $$.Index = AddTextEntry(EntryType.Gt , $1.Text, $3.Index); }
    | PATH GTE value                              { $$.Index = AddTextEntry(EntryType.Gte, $1.Text, $3.Index); }
    | PATH EQ  value                              { $$.Index = AddTextEntry(EntryType.Eq , $1.Text, $3.Index); }
    | PATH NEQ value                              { $$.Index = AddTextEntry(EntryType.Neq, $1.Text, $3.Index); }
    | PATH     BETWEEN value AND value            { $$.Index = AddTextEntry(EntryType.Between , $1.Text, $3.Index, $5.Index); }
    | PATH NOT BETWEEN value AND value            { $$.Index = AddTextEntry(EntryType.Nbetween, $1.Text, $4.Index, $6.Index); }
    | PATH     MATCH value                        { $$.Index = AddTextEntry(EntryType.Match , $1.Text, $3.Index); }
    | PATH NOT MATCH value                        { $$.Index = AddTextEntry(EntryType.Nmatch, $1.Text, $4.Index); }
    | PATH     MATCH value OPTIONS value          { $$.Index = AddTextEntry(EntryType.MatchOp , $1.Text, $3.Index, $5.Index); }
    | PATH NOT MATCH value OPTIONS value          { $$.Index = AddTextEntry(EntryType.NmatchOp, $1.Text, $4.Index, $6.Index); }
    | PATH     IN valueList                       { $$.Index = AddTextEntry(EntryType.In , $1.Text, $3.Index); }
    | PATH NOT IN valueList                       { $$.Index = AddTextEntry(EntryType.Nin, $1.Text, $4.Index); }
    | PATH     EXIST                              { $$.Index = AddTextEntry(EntryType.Exist , $1.Text); }
    | PATH NOT EXIST                              { $$.Index = AddTextEntry(EntryType.Nexist, $1.Text); }
    | ANYOF PATH IS     LPAREN orGroup RPAREN     { $$.Index = AddTextEntry(EntryType.AnyIs , $2.Text, $5.Index); }
    | ANYOF PATH IS NOT LPAREN orGroup RPAREN     { $$.Index = AddTextEntry(EntryType.AnyNis, $2.Text, $6.Index); }
    | TYPEOF PATH EQ  value                       { $$.Index = AddTextEntry(EntryType.TypeEq , $2.Text, $4.Index); }
    | TYPEOF PATH NEQ value                       { $$.Index = AddTextEntry(EntryType.TypeNeq, $2.Text, $4.Index); }
    | TYPEOF PATH     IN valueList                { $$.Index = AddTextEntry(EntryType.TypeIn , $2.Text, $4.Index); }
    | TYPEOF PATH NOT IN valueList                { $$.Index = AddTextEntry(EntryType.TypeNin, $2.Text, $5.Index); }
    | LPAREN orGroup RPAREN                       { $$.Index = $2.Index; }
    | NOT LPAREN orGroup RPAREN                   { $$.Index = AddEntry(EntryType.Not, $3.Index); }
    ;


valueList
    : LPAREN valueListPlus RPAREN                 { $$.Index = $2.Index; }
    | expressionArray                             { $$.Index = $1.Index; }
    ;

valueListPlus
    : valueListPlus COMMA value                   { $$.Index = AddEntry(EntryType.List, index1: $1.Index, index2: $3.Index); }
    |                     value                   { $$.Index = AddEntry(EntryType.List, index1:       -1, index2: $1.Index); }
    ;

value
    : literal                                     { $$.Index = $1.Index; }
    | expression                                  { $$.Index = $1.Index; }
    ;

literal
    : LITERAL                                     { $$.Index = AddValueEntry(EntryType.Value, $1.Value); }
    ;

expression
    : EXPRESSION                                  { $$.Index = AddTextEntry(EntryType.ValueExpr, $1.Text); }
    ;

expressionArray
    : EXPRESSION                                  { $$.Index = AddTextEntry(EntryType.ArrayExpr, $1.Text); }
    ;

%%