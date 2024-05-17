# FluentFilter.MongoDB

Provides a way to create filters for MongoDB.Driver using plain text.

## Usage

### Simple sample
```Csharp
    var filterText = "Sum between 10000 and 2000 or Price >= 100";
    ...
    var filter = FluentFilter.Parse(filterText).Create();
    var results = await collection.Find(filter).ToListAsync(cancellationToken);
```

### Advanced sample 1
```Csharp
    BsonValue expressionEvaluator(String expressionText)
    {
        return expressionText switch {
            "today" => new BsonDateTime(DateTime.UtcNow.Date),
            "tomorrow" => new BsonDateTime(DateTime.UtcNow.Date.AddDays(1)),
            _ => throw new InvalidOperation($"Expression {expressionText} cannot be evaluated");
        };
    }
    ...
    var filterText = "CreateDate between ${today} and ${tomorrow}";
    ...
    var filter = FluentFilter.Parse(filterText).Create(expressionEvaluator);
    var results = await collection.Find(filter).ToListAsync(cancellationToken);
```

### Advanced sample 2
```Csharp
    BsonValue expressionEvaluator(String expressionText)
    {
        return expressionText switch {
            "today" => new BsonDateTime(DateTime.UtcNow.Date),
            "tomorrow" => new BsonDateTime(DateTime.UtcNow.Date.AddDays(1)),
            _ => throw new InvalidOperation($"Expression {expressionText} cannot be evaluated");
        };
    }
    ...
    var filterText = "CreateDate between ${today} and ${tomorrow}";
    var filterFactory = FluentFilter.Parse(filterText);
    ...
    var filter = filterFactory.Create(expressionEvaluator);
    var results = await collection.Find(filter).ToListAsync(cancellationToken);
```

## Introduction

Library expects plain text filter that conforms the following grammar:

### 1. Logical operators

#### 1.1. AND - conjunction
```
Sum <= 10000.00 AND Price <= 200.00
```

#### 1.2. OR - disjunction
```
Sum > 10000.00 OR Price > 200.00
```

#### 1.3. NOT - negation
```
NOT (Sum <= 10000.00 AND Price <= 200.00)
```
`NOT` requires parenthesis to specify expression it applies to.

#### 1.4. (...) - group
```
(Sum > 10000.00 OR Price > 200.00) AND Qty > 50
```
Use parenthesis to enforce necessary priority.

#### 1.5. AND, OR - priority
`AND` has higher priority than `OR`. That is the following filter :
```
Sum > 10000.00 OR Price > 200.00 AND Qty > 50
```
is equivalent to
```
Sum > 10000.00 OR (Price > 200.00 AND Qty > 50)
```
To remember this you just need to realize that `AND` is boolean multiplication
and `OR` is almost boolean addition, so priority of boolean operators resemles priority of arithmetic operators.

### 2. Primitives
Most of primitives requeres path to tested field to be the left hand side

#### 2.1. Value comparison ($lt, $lte, $gt, $gte, $eg, $ne)
- `Sum < 1000`
- `Sum <= 1000`
- `Date > #2017-06-14#`
- `String >= "ABC"`
- `_id == ObjectId("0A1B2C3D4E5F6a7b8c9d0e1f")`
- `Uuid != UUID("2C62A140-E79E-4C8E-94E1-C9C6E18BF13E")`
- `Sum < ${:sum}`
- `Sum <= ${:sum}`
- `Date > ${:dob}`
- `String >= ${:lowBound}`
- `_id == ${:objectId}`
- `Uuid != ${:uuid}`

#### 2.2. Value between
- `CreateDate between #2024-01-01# and #2025-01-01#`
- `CreateDate between #2024-01-01# and ${tomorrow}`
- `CreateDate between ${today} and ${tomorrow}`
- `CreateDate between ${today} and #2025-01-01#`
- `CreateDate not between #2024-01-01# and #2025-01-01#`
- `CreateDate not between #2024-01-01# and ${tomorrow}`
- `CreateDate not between ${today} and ${tomorrow}`
- `CreateDate not between ${today} and #2025-01-01#`

Filter `A BETWEEN from AND to` is just shorthand for `A >= from AND A <= to`.

#### 2.3. Value IN ($in, $nin)
- `Grade IN ("A", "B")`
- `Grade IN ("A", ${:second:})`
- `Grade IN ${grades}`
- `Grade NOT IN ("F", "E")`
- `Grade NOT IN ("F", ${:another:})`
- `Grade NOT IN ${grades}`

##### Notes:
Variants `Grade IN ${grades}` and `Grade NOT IN ${grades}` requres expression `grades` to be evaluated as BsonArray.

#### 2.4. Value Exist ($exist)
- `Grade EXIST`
- `Grade NOT EXIST`

#### 2.5. Value MATCH regex ($regex)
- `Comment MATCH /first/i`
- `Comment MATCH "first" OPTIONS "i"`
- `Comment MATCH ${regex}`
- `Comment MATCH ${regex} OPTIONS ${options}`
- `Comment NOT MATCH /first/i`
- `Comment NOT MATCH "first" OPTIONS "i"`
- `Comment MATCH ${regex}`
- `Comment MATCH ${regex} OPTIONS ${options}`

#### 2.6. TYPEOF Value comparison ($type)
- `TYPEOF Sum == "number"`
- `TYPEOF Sum == 3`
- `TYPEOF Sum != "number"`
- `TYPEOF Sum != 3`

##### Notes:
You must use either alias or numeric value of the BsonType. See article [BSON Types](https://www.mongodb.com/docs/manual/reference/bson-types/)
in the MongoDB manual

#### 2.7. TYPEOF Value IN ($type)
- `TYPEOF Sum IN ("int", "long", "double", "decimal")`
- `TYPEOF Sum IN (1, 16, 18, 19)`
- `TYPEOF Sum IN ${types}`
- `TYPEOF Sum != "number"`
- `TYPEOF Sum != 3`

##### Notes:
Variants `TYPEOF Sum IN ${types}` and `TYPEOF Sum NOT IN ${types}` requres expression `types` to be evaluated as BsonArray.

#### 2.8. ANYOF Value IS ($elemMatch)
- `ANYOF Numbers IS ($ BETWEEN 10 AND 100)`
- `ANYOF Numbers IS NOT($ BETWEEN 10 AND 100)`
- `ANYOF Docs IS (A <= 10 AND B >= 100)`
- `ANYOF Docs IS NOT (A <= 10 AND B >= 100)`

### 3. Pathes
If path to the tested value consists only of identifies (`/^[A-Za-z_][A-Za-z0-9_]*$/`) and indices (`/^[0-9]+$/`) you may write it as is:

- `Sum`
- `Sums.0`
- `Doc.Prop1`
- `Docs.1.Prop1`
- if your path coinside with one of the reserved words, then enclose it in backticks (<code>\`</code>)

If your identifiers do not match regex `/^[A-Za-z_][A-Za-z0-9_]*$/` then you have to enclose entire path in backticks (<code>\`</code>).
If your identifiers contain backtick - just duplicate it.

- <code>\`:Sum\`</code>
- <code>\`:Sums.0\`</code>
- <code>\`Doc-Prop1\`</code>
- <code>\`:Doc.:Prop1\`</code>
- <code>\`:Docs.1.:Prop1\`</code>
- <code>\`:Docs.1.:Prop1\`</code>
- <code>\`Docs\`\`Prop1\`</code>

To refer to an element of array use dollar sign (`$`).
- `ANYOF Numbers IS ($ BETWEEN 10 AND 100)`

### 4. Values
Each value used in primitives can be either expression or literal.

### 5. Expressions
Expressions are fragments that match regex `${[^}]+}`.

- `${2 + 2}`
- `${today}`
- `${random}`

Interpretation of expression is responsibility of the caller:
- On the parsing phase expressions are only stripped of prefix `${` and suffix `}`.
- On the creation phase each expression is passed to `expressionEvaluator` function to get corresponding `BsonValue`

### 6. Literals

#### 6.1. String
Strings literas must be enclosed in double quotes. Double quotes inside string must be duplicated.

- <code>""</code>
- <code>"string"</code>
- <code>"Multiline\
String"</code>
- <code>"String with "" quote."</code>

#### 6.2. Numbers
Number literas must match regular expression `/^[+\-]?[0-9]+(\.[0-9]+)?([Ee][+\-]?[0-9]+)?$/`

- `+12`
- `-12.5`
- `12.5e0`
- `-12.5e+5`
- `+12.5e-5`

#### 6.3. Dates
Dates must match one of the following formats
<table>
<tr><th>Format</th><th>Sample</th></tr>
<tr><td><code>#yyyy-MM-dd#</code></td>
    <td><code>#2013-09-18#</code></td></td>
<tr><td><code>#yyyy-MM-dd HH:mm#</code></td>
    <td><code>#2013-09-18 12:53#</code></td></td>
<tr><td><code>#yyyy-MM-dd HH:mm:ss#</code></td>
    <td><code>#2013-09-18 12:53:23#</code></td></td>
<tr><td rowspan="4" valign="top"><code>#yyyy-MM-dd HH:mm:ss.f#</code></td>
    <td><code>#2013-09-18 12:53:23.1#</code></td></td>
<tr><td><code>#2013-09-18 12:53:23.12#</code></td></td>
<tr><td><code>#2013-09-18 12:53:23.123#</code></td></td>
<tr><td>etc.</td></td>
</table>

#### 6.4. Booleans
Booleans literas are just words `true` and `false` in any case:

- `true`, `false`
- `TRUE`, `FALSE`
- `True`, `False`
- `tRUE`, `fALSE`
- etc.

#### 6.5. Null
Null literals are just word `null` in any case:

- `null`
- `NULL`
- `Null`
- `nULL`
- etc.

#### 6.6. Regex
Regex literas must match regular expression `/^\/(\/\/|[^/])*\/[imsx]*$/`.
Slashes must be duplicated.

- `/[0-9]+/`
- `/^[0-9]+$/`
- `/^[a-z][0-9]*$/i`
- `/^[a-z]//[0-9]*$/i`

#### 6.7. Binary
Binary literas looks like call of `Binary()` function with one or two string arguments.
Last argument is Base64 encoded value (it may contain whitespaces).
In the case with two arguments the first argument is the BinarySubType.

- `Binary("")`
- `Binary("AA==")`
- `Binary("AAB=")`
- `Binary("AABC")`
- `Binary("AABC AABC AABC AABC AABC AABC AA==")`

- `Binary("Binary", "")`
- `Binary("MD5", "AA==")`
- `Binary("Encrypted", "AAB=")`
- `Binary("UserDefined", "AABC")`
- `Binary("OldBinary", "AABC AABC AABC AABC AABC AABC AA==")`

#### 6.8. ObjectId
ObjectId literas looks like call of `ObjectId()` function with one string argument.
The argument is hex encoded value.

- `ObjectId("0A1B2C3D4E5F6a7b8c9d0e1f")`

#### 6.9 UUID
ObjectId literas looks like call of `ObjectId()` function with one string argument.
Last argument is 32 hexadecimal digits separated with dashes: `00000000-0000-0000-0000-000000000000`.
In the case with two arguments the first argument is the GuidRepresentation.

- `Uuid("00000000-0000-0000-0000-000000000000")`
- `Uuid("CSharpLegacy", "00000000-0000-0000-0000-000000000000")`
- `Uuid("JavaLegacy", "00000000-0000-0000-0000-000000000000")`
- `Uuid("PythonLegacy", "00000000-0000-0000-0000-000000000000")`
- `Uuid("Standard", "00000000-0000-0000-0000-000000000000")`

#### 7. Reserved words
The following words are used in grammar and have priority over pathes. To use them as path just enclose
in backticks (<code>\`</code>).

- `AND`
- `ANYOF`
- `BETWEEN`
- `EXIST`
- `FALSE`
- `IN`
- `IS`
- `MATCH`
- `NOT`
- `NULL`
- `OR`
- `RPAREN`
- `TRUE`
- `TYPEOF`
