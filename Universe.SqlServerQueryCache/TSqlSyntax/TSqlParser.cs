using System.Text;

namespace Universe.SqlServerQueryCache.TSqlSyntax
{
    public class TSqlFragment
    {
        public int Start { get; set; }
        public int Length { get; set; }
        public TSqlFragmentKind Kind { get; set; }
    }

    public enum TSqlFragmentKind
    {
        Text,
        Comment,
        String,
        Keyword,
        DataType,
        // TODO: [Object|Column Name]
    }

    public class TSqlParser
    {
        public string Document { get; }

        public TSqlParser(string document)
        {
            Document = document;
        }

        public List<TSqlFragment> Parse()
        {
            List<TSqlFragment> tokens = new List<TSqlFragment>();
            var documentLength = Document.Length;

            void AddByStartEnd(int start, int end, TSqlFragmentKind kind)
            {
                tokens.Add(new TSqlFragment()
                {
                    Start = start,
                    Length = end - start + 1,
                    Kind = kind
                });
            }

            int? startFragment = null;
            StringBuilder buffer = new StringBuilder();

            void AddBufferIfNonEmpty()
            {
                if (startFragment.HasValue && buffer.Length > 0)
                {
                    var fList = ParseSqlCodeWithoutStringsAndWithoutComments(startFragment.Value, buffer);
                    foreach (var sqlFragment in fList) tokens.Add(sqlFragment);
                }

                startFragment = null;
                buffer.Clear();
            }

            int pos = 0;
            while(pos < documentLength)
            {
                if (pos >= documentLength) break;
                var isQuote = Document[pos] == '\'';
                if (isQuote)
                {
                    AddBufferIfNonEmpty();
                    int quoteStartPos = pos, quoteEndPos = FindEndOfString(pos);
                    AddByStartEnd(quoteStartPos, quoteEndPos, TSqlFragmentKind.String);
                    pos = quoteEndPos + 1;
                    continue;
                }

                bool isSingleLineComment = pos + 1 < documentLength && Document[pos] == '-' && Document[pos+1] == '-';
                if (isSingleLineComment)
                {
                    AddBufferIfNonEmpty();
                    int commentStartPos = pos, commentEndPos = FindEndOfSingleLineComment(pos);
                    AddByStartEnd(commentStartPos, commentEndPos, TSqlFragmentKind.Comment);
                    pos = commentEndPos + 1;
                    continue;
                }

                bool isMultiLineComment = pos + 1 < documentLength && Document[pos] == '/' && Document[pos + 1] == '*';
                if (isMultiLineComment)
                {
                    AddBufferIfNonEmpty();
                    int commentStartPos = pos, commentEndPos = FindEndOfMultiLineComment(pos);
                    AddByStartEnd(commentStartPos, commentEndPos, TSqlFragmentKind.Comment);
                    pos = commentEndPos + 1;
                    continue;
                }

                if (startFragment == null)
                {
                    startFragment = pos;
                    buffer.Clear();
                }

                var c = Document[pos];
                buffer.Append(c);

                pos++;
            }

            AddBufferIfNonEmpty();

            return tokens;
        }

        private IEnumerable<TSqlFragment> ParseSqlCodeWithoutStringsAndWithoutComments(int startPos, StringBuilder argString)
        {
            StringBuilder bufferOther = new StringBuilder();
            int? posOther = null, posSpecial = null;
            StringBuilder bufferSpecial = new StringBuilder();

            IEnumerable<TSqlFragment> ReturnOtherBufferIfExists()
            {
                if (bufferOther.Length > 0 && posOther.HasValue)
                {
                    yield return new TSqlFragment()
                    {
                        Start = posOther.Value,
                        Length = bufferOther.Length,
                        Kind = TSqlFragmentKind.Text
                    };
                    bufferOther.Clear();
                    posOther = null;
                }
            }

            IEnumerable<TSqlFragment> ReturnSpecialBufferIfExists()
            {
                if (bufferSpecial.Length > 0 && posSpecial.HasValue)
                {
                    // Add Special: Text|Keyword|DataType
                    TSqlFragmentKind kind = TSqlFragmentKind.Text;
                    var word = bufferSpecial.ToString();
                    if (LazyKeywords.Value.Contains(word)) kind = TSqlFragmentKind.Keyword;
                    else if (LazyDataTypes.Value.Contains(word)) kind = TSqlFragmentKind.DataType;
                    yield return new TSqlFragment()
                    {
                        Start = posSpecial.Value,
                        Length = bufferSpecial.Length,
                        Kind = kind
                    };
                    bufferSpecial.Clear();
                    posSpecial = null;
                }
            }

            for (int i = 0, len = argString.Length; i < len; i++)
            {
                var c = argString[i];
                bool isSpecial = (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z') || (c >= '0' && c <= '9') || c == '_';
                if (isSpecial)
                {
                    foreach (var sqlFragment in ReturnOtherBufferIfExists()) yield return sqlFragment;

                    if (posSpecial == null)
                    {
                        posSpecial = startPos + i;
                    }

                    bufferSpecial.Append(c);
                }
                else
                {
                    foreach (var sqlFragment1 in ReturnSpecialBufferIfExists()) yield return sqlFragment1;

                    if (posOther == null)
                    {
                        posOther = startPos + i;
                    }

                    bufferOther.Append(c);
                }
            }

            foreach (var sqlFragment in ReturnOtherBufferIfExists()) yield return sqlFragment;
            foreach (var sqlFragment1 in ReturnSpecialBufferIfExists()) yield return sqlFragment1;
        }

        private int FindEndOfMultiLineComment(int startPos)
        {
            var doc = Document;
            var len = doc.Length;

            again:

            bool isEndOfComment = startPos + 1 < len && doc[startPos] == '*' && doc[startPos + 1] == '/';
            if (isEndOfComment) return startPos + 1;

            if (startPos + 1 == len) return startPos;
            startPos++;
            goto again;
        }

        private int FindEndOfSingleLineComment(int startPos)
        {
            var doc = Document;
            var len = doc.Length;
            
            again:

            if (startPos + 1 == len) return startPos;
            if (doc[startPos] == '\n') return startPos;
            startPos++;
            goto again;
        }

        // arg is position of starting quote
        int FindEndOfString(int startPos)
        {
            var doc = Document;
            var len = doc.Length;
            if (startPos == doc.Length - 1)
                return startPos;

            startPos++;

            again:
            
            bool isDoubleQuote = startPos + 1 < len && doc[startPos] == '\'' && doc[startPos + 1] == '\'';
            if (isDoubleQuote)
            {
                startPos += 2;
                goto again;
            }

            bool isSingleQuote = startPos < len && doc[startPos] == '\'';
            if (isSingleQuote)
                return startPos;

            // syntax error
            if (startPos + 1 == len) return startPos;
            startPos++;
            goto again;
        }


        private static Lazy<HashSet<string>> LazyDataTypes =
            new Lazy<HashSet<string>>(() => new HashSet<string>(_dataTypesArray, StringComparer.OrdinalIgnoreCase),
                LazyThreadSafetyMode.ExecutionAndPublication);

        private static Lazy<HashSet<string>> LazyKeywords =
            new Lazy<HashSet<string>>(() => new HashSet<string>(_keywordsArray, StringComparer.OrdinalIgnoreCase),
                LazyThreadSafetyMode.ExecutionAndPublication);


        private static readonly string[] _dataTypesArray = new[]
        {
            "tinyint", "smallint", "int", "bigint", "bit", 
            "decimal", "numeric", "money", "smallmoney", 
            "float", "real",
            "date", "time", "datetime2", "datetimeoffset", "datetime", "smalldatetime", 
            "char", "varchar", "text",
            "nchar", "nvarchar", "ntext", 
            "binary", "varbinary", "image", 
            "cursor", "geography", "geometry",
            "hierarchyid", "json", "vector", "rowversion", "sql_variant", "table", "uniqueidentifier", "xml"
        };

        private static readonly string[] _keywordsArray = new[]
        {
            "ADD", "EXTERNAL", "PROCEDURE", "ALL", "FETCH", "PUBLIC", "ALTER", "FILE", "RAISERROR", "AND", "FILLFACTOR",
            "READ", "ANY", "FOR", "READTEXT", "AS", "FOREIGN", "RECONFIGURE", "ASC", "FREETEXT", "REFERENCES",
            "AUTHORIZATION", "FREETEXTTABLE", "REPLICATION", "BACKUP", "FROM", "RESTORE", "BEGIN", "FULL", "RESTRICT",
            "BETWEEN", "FUNCTION", "RETURN", "BREAK", "GOTO", "REVERT", "BROWSE", "GRANT", "REVOKE", "BULK", "GROUP",
            "RIGHT", "BY", "HAVING", "ROLLBACK", "CASCADE", "HOLDLOCK", "ROWCOUNT", "CASE", "IDENTITY", "ROWGUIDCOL",
            "CHECK", "IDENTITY_INSERT", "RULE", "CHECKPOINT", "IDENTITYCOL", "SAVE", "CLOSE", "IF", "SCHEMA",
            "CLUSTERED", "IN", "SECURITYAUDIT", "COALESCE", "INDEX", "SELECT", "COLLATE", "INNER",
            "SEMANTICKEYPHRASETABLE", "COLUMN", "INSERT", "SEMANTICSIMILARITYDETAILSTABLE", "COMMIT", "INTERSECT",
            "SEMANTICSIMILARITYTABLE", "COMPUTE", "INTO", "SESSION_USER", "CONSTRAINT", "IS", "SET", "CONTAINS", "JOIN",
            "SETUSER", "CONTAINSTABLE", "KEY", "SHUTDOWN", "CONTINUE", "KILL", "SOME", "CONVERT", "LEFT", "STATISTICS",
            "CREATE", "LIKE", "SYSTEM_USER", "CROSS", "LINENO", "TABLE", "CURRENT", "LOAD", "TABLESAMPLE",
            "CURRENT_DATE", "MERGE", "TEXTSIZE", "CURRENT_TIME", "NATIONAL", "THEN", "CURRENT_TIMESTAMP", "NOCHECK",
            "TO", "CURRENT_USER", "NONCLUSTERED", "TOP", "CURSOR", "NOT", "TRAN", "DATABASE", "NULL", "TRANSACTION",
            "DBCC", "NULLIF", "TRIGGER", "DEALLOCATE", "OF", "TRUNCATE", "DECLARE", "OFF", "TRY_CONVERT", "DEFAULT",
            "OFFSETS", "TSEQUAL", "DELETE", "ON", "UNION", "DENY", "OPEN", "UNIQUE", "DESC", "OPENDATASOURCE",
            "UNPIVOT", "DISK", "OPENQUERY", "UPDATE", "DISTINCT", "OPENROWSET", "UPDATETEXT", "DISTRIBUTED", "OPENXML",
            "USE", "DOUBLE", "OPTION", "USER", "DROP", "OR", "VALUES", "DUMP", "ORDER", "VARYING", "ELSE", "OUTER",
            "VIEW", "END", "OVER", "WAITFOR", "ERRLVL", "PERCENT", "WHEN", "ESCAPE", "PIVOT", "WHERE", "EXCEPT", "PLAN",
            "WHILE", "EXEC", "PRECISION", "WITH", "EXECUTE", "PRIMARY", "WITHIN", "GROUP", "EXISTS", "PRINT",
            "WRITETEXT", "EXIT", "PROC",
            "Count", "Count_Big", "Avg", "Sum", "Max", "Min", "STDEV", "STDEVP", "VAR", "VARP"
        };

    }
}