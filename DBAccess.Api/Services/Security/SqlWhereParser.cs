using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace DBAccess.Api.Services.Security;

internal static class SqlWhereParser
{
    // Public entry: parse, validate, and return sanitized SQL where fragment
    public static string ParseAndSanitize(string input, SqlGuardOptions opt, out List<string> columns)
    {
        columns = new List<string>();
        if (string.IsNullOrWhiteSpace(input)) return string.Empty;
        var tokenizer = new Tokenizer(input);
        var tokens = tokenizer.Tokenize();
        var parser = new Parser(tokens);
        var expr = parser.ParseExpression();

        // Collect columns
        columns = expr.CollectColumns().Select(c => c.LastPart).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        if (opt.EnableWhitelist)
        {
            foreach (var col in columns)
            {
                if (!opt.AllowedColumns.Contains(col))
                    throw new ArgumentException($"列不在白名单: {col}");
            }
        }

        // Emit sanitized SQL
        var sb = new StringBuilder();
        expr.Emit(sb);
        return sb.ToString();
    }

    #region Lexer
    private enum TokKind
    {
        EOF,
        Ident,
        Number,
        String,
        LParen,
        RParen,
        Comma,
        Eq, Neq, Lt, Lte, Gt, Gte,
        And, Or, Not,
        Like, In, Between, Is, Null
    }

    private readonly struct Token
    {
        public TokKind Kind { get; }
        public string Text { get; }
        public Token(TokKind k, string t) { Kind = k; Text = t; }
        public override string ToString() => $"{Kind}:{Text}";
    }

    private sealed class Tokenizer
    {
        private readonly string _s;
        private int _i;
        public Tokenizer(string s) { _s = s; _i = 0; }
        public List<Token> Tokenize()
        {
            var list = new List<Token>();
            Token t;
            while ((t = Next()).Kind != TokKind.EOF)
                list.Add(t);
            list.Add(t); // add EOF
            return list;
        }

        private Token Next()
        {
            SkipWs();
            if (_i >= _s.Length) return new Token(TokKind.EOF, string.Empty);
            char c = _s[_i];
            // punctuation
            switch (c)
            {
                case '(': _i++; return new Token(TokKind.LParen, "(");
                case ')': _i++; return new Token(TokKind.RParen, ")");
                case ',': _i++; return new Token(TokKind.Comma, ",");
                case '=': _i++; return new Token(TokKind.Eq, "=");
                case '<':
                    if (Peek('>')) { _i += 2; return new Token(TokKind.Neq, "<>"); }
                    if (Peek('=')) { _i += 2; return new Token(TokKind.Lte, "<="); }
                    _i++; return new Token(TokKind.Lt, "<");
                case '>':
                    if (Peek('=')) { _i += 2; return new Token(TokKind.Gte, ">="); }
                    _i++; return new Token(TokKind.Gt, ">");
                case '!':
                    if (Peek('=')) { _i += 2; return new Token(TokKind.Neq, "!="); }
                    throw Error("不支持的运算符 !");
                case '\'':
                    return ReadString();
            }

            if (char.IsDigit(c)) return ReadNumber();
            if (c == '[' || char.IsLetter(c) || c == '_') return ReadIdentOrKeyword();

            throw Error($"非法字符: {c}");
        }

        private void SkipWs()
        {
            while (_i < _s.Length && char.IsWhiteSpace(_s[_i])) _i++;
        }

        private bool Peek(char ch)
        {
            return _i + 1 < _s.Length && _s[_i + 1] == ch;
        }

        private Token ReadNumber()
        {
            int start = _i;
            while (_i < _s.Length && char.IsDigit(_s[_i])) _i++;
            if (_i < _s.Length && _s[_i] == '.')
            {
                _i++;
                while (_i < _s.Length && char.IsDigit(_s[_i])) _i++;
            }
            var t = _s.Substring(start, _i - start);
            return new Token(TokKind.Number, t);
        }

        private Token ReadString()
        {
            // starting with '
            _i++; // skip opening quote
            var sb = new StringBuilder();
            while (_i < _s.Length)
            {
                char c = _s[_i++];
                if (c == '\'')
                {
                    // doubled '' escape
                    if (_i < _s.Length && _s[_i] == '\'') { sb.Append('\''); _i++; }
                    else return new Token(TokKind.String, sb.ToString());
                }
                else
                {
                    sb.Append(c);
                }
            }
            throw Error("字符串字面量未闭合");
        }

        private Token ReadIdentOrKeyword()
        {
            // Support bracketed identifier part [name with space], then optional . and further parts or simple identifier starting with letter/_
            int start = _i;
            var parts = new List<string>();
            bool bracketedFirst = false;

            // Try read chain (part(.part)*)
            int save = _i;
            if (TryReadIdentChain(parts, out bracketedFirst))
            {
                // If single-part and matches keyword, return keyword; otherwise return as Ident with joined by .
                if (parts.Count == 1 && !bracketedFirst)
                {
                    var upper = parts[0].ToUpperInvariant();
                    return upper switch
                    {
                        "AND" => new Token(TokKind.And, upper),
                        "OR" => new Token(TokKind.Or, upper),
                        "NOT" => new Token(TokKind.Not, upper),
                        "LIKE" => new Token(TokKind.Like, upper),
                        "IN" => new Token(TokKind.In, upper),
                        "BETWEEN" => new Token(TokKind.Between, upper),
                        "IS" => new Token(TokKind.Is, upper),
                        "NULL" => new Token(TokKind.Null, upper),
                        _ => new Token(TokKind.Ident, parts[0])
                    };
                }
                return new Token(TokKind.Ident, string.Join(".", parts));
            }
            // fallback single letter/_ identifier
            _i = save;
            if (char.IsLetter(_s[_i]) || _s[_i] == '_')
            {
                _i++;
                while (_i < _s.Length && (char.IsLetterOrDigit(_s[_i]) || _s[_i] == '_')) _i++;
                var name = _s.Substring(start, _i - start);
                var upper = name.ToUpperInvariant();
                return upper switch
                {
                    "AND" => new Token(TokKind.And, upper),
                    "OR" => new Token(TokKind.Or, upper),
                    "NOT" => new Token(TokKind.Not, upper),
                    "LIKE" => new Token(TokKind.Like, upper),
                    "IN" => new Token(TokKind.In, upper),
                    "BETWEEN" => new Token(TokKind.Between, upper),
                    "IS" => new Token(TokKind.Is, upper),
                    "NULL" => new Token(TokKind.Null, upper),
                    _ => new Token(TokKind.Ident, name)
                };
            }

            throw Error("非法标识符");
        }

        private bool TryReadIdentChain(List<string> parts, out bool firstBracketed)
        {
            firstBracketed = false;
            int count = 0;
            while (true)
            {
                if (_i >= _s.Length) break;
                if (_s[_i] == '[')
                {
                    var name = ReadBracketedIdentifier();
                    if (count == 0) firstBracketed = true;
                    parts.Add(name);
                }
                else if (char.IsLetter(_s[_i]) || _s[_i] == '_')
                {
                    int start = _i; _i++;
                    while (_i < _s.Length && (char.IsLetterOrDigit(_s[_i]) || _s[_i] == '_')) _i++;
                    parts.Add(_s.Substring(start, _i - start));
                }
                else break;
                count++;
                // dot separator
                if (_i < _s.Length && _s[_i] == '.') { _i++; continue; }
                break;
            }
            return count > 0;
        }

        private string ReadBracketedIdentifier()
        {
            // [name]] with ]] escape? We will not support nested ']' escapes; only allow A-Za-z0-9_ and spaces inside.
            _i++; // skip [
            var sb = new StringBuilder();
            while (_i < _s.Length)
            {
                char c = _s[_i++];
                if (c == ']') break;
                if (!(char.IsLetterOrDigit(c) || c == '_' || c == ' '))
                    throw Error("[] 标识符中包含非法字符");
                sb.Append(c);
            }
            return sb.ToString();
        }

        private static Exception Error(string msg) => new ArgumentException(msg);
    }
    #endregion

    #region AST
    private abstract class Expr
    {
        public abstract void Emit(StringBuilder sb);
        public virtual IEnumerable<ColumnRef> CollectColumns() => Enumerable.Empty<ColumnRef>();
    }

    private sealed class ColumnRef
    {
        public List<string> Parts { get; } = new();
        public string LastPart => Parts.Count == 0 ? string.Empty : Parts[^1];
        public void Emit(StringBuilder sb)
        {
            // Emit as [part].[part]
            for (int i = 0; i < Parts.Count; i++)
            {
                if (i > 0) sb.Append('.');
                sb.Append('[').Append(Parts[i]).Append(']');
            }
        }
    }

    private sealed class Literal
    {
        public string? StringValue { get; set; }
        public string? NumberText { get; set; }
        public bool IsNull { get; set; }
        public void Emit(StringBuilder sb)
        {
            if (IsNull) { sb.Append("NULL"); return; }
            if (NumberText != null) { sb.Append(NumberText); return; }
            // string
            var s = StringValue ?? string.Empty;
            sb.Append('\'');
            sb.Append(s.Replace("'", "''"));
            sb.Append('\'');
        }
    }

    private sealed class LogicalExpr : Expr
    {
        public string Op { get; } // AND / OR
        public Expr Left { get; }
        public Expr Right { get; }
        public LogicalExpr(string op, Expr l, Expr r) { Op = op; Left = l; Right = r; }
        public override void Emit(StringBuilder sb)
        {
            sb.Append('(');
            Left.Emit(sb);
            sb.Append(' ').Append(Op).Append(' ');
            Right.Emit(sb);
            sb.Append(')');
        }
        public override IEnumerable<ColumnRef> CollectColumns() => Left.CollectColumns().Concat(Right.CollectColumns());
    }

    private sealed class NotExpr : Expr
    {
        public Expr Inner { get; }
        public NotExpr(Expr e) { Inner = e; }
        public override void Emit(StringBuilder sb)
        {
            sb.Append("(NOT ");
            Inner.Emit(sb);
            sb.Append(')');
        }
        public override IEnumerable<ColumnRef> CollectColumns() => Inner.CollectColumns();
    }

    private sealed class CompareExpr : Expr
    {
        public ColumnRef Left { get; }
        public string Op { get; }
        public Literal Right { get; }
        public CompareExpr(ColumnRef l, string op, Literal r) { Left = l; Op = op; Right = r; }
        public override void Emit(StringBuilder sb)
        {
            Left.Emit(sb); sb.Append(' ').Append(Op).Append(' '); Right.Emit(sb);
        }
        public override IEnumerable<ColumnRef> CollectColumns() { yield return Left; }
    }

    private sealed class LikeExpr : Expr
    {
        public ColumnRef Left { get; }
        public bool Not { get; }
        public Literal Pattern { get; }
        public LikeExpr(ColumnRef l, bool not, Literal pat) { Left = l; Not = not; Pattern = pat; }
        public override void Emit(StringBuilder sb)
        {
            Left.Emit(sb); sb.Append(Not ? " NOT LIKE " : " LIKE "); Pattern.Emit(sb);
        }
        public override IEnumerable<ColumnRef> CollectColumns() { yield return Left; }
    }

    private sealed class InExpr : Expr
    {
        public ColumnRef Left { get; }
        public bool Not { get; }
        public List<Literal> Items { get; }
        public InExpr(ColumnRef l, bool not, List<Literal> items) { Left = l; Not = not; Items = items; }
        public override void Emit(StringBuilder sb)
        {
            Left.Emit(sb); sb.Append(Not ? " NOT IN (" : " IN (");
            for (int i = 0; i < Items.Count; i++) { if (i > 0) sb.Append(','); Items[i].Emit(sb); }
            sb.Append(')');
        }
        public override IEnumerable<ColumnRef> CollectColumns() { yield return Left; }
    }

    private sealed class BetweenExpr : Expr
    {
        public ColumnRef Left { get; }
        public bool Not { get; }
        public Literal A { get; }
        public Literal B { get; }
        public BetweenExpr(ColumnRef l, bool not, Literal a, Literal b) { Left = l; Not = not; A = a; B = b; }
        public override void Emit(StringBuilder sb)
        {
            Left.Emit(sb); sb.Append(Not ? " NOT BETWEEN " : " BETWEEN "); A.Emit(sb); sb.Append(" AND "); B.Emit(sb);
        }
        public override IEnumerable<ColumnRef> CollectColumns() { yield return Left; }
    }

    private sealed class IsNullExpr : Expr
    {
        public ColumnRef Left { get; }
        public bool Not { get; }
        public IsNullExpr(ColumnRef l, bool not) { Left = l; Not = not; }
        public override void Emit(StringBuilder sb)
        {
            Left.Emit(sb); sb.Append(Not ? " IS NOT NULL" : " IS NULL");
        }
        public override IEnumerable<ColumnRef> CollectColumns() { yield return Left; }
    }
    #endregion

    #region Parser
    private sealed class Parser
    {
        private readonly List<Token> _toks;
        private int _i = 0;
        public Parser(List<Token> toks) { _toks = toks; }
        private Token LA => _toks[_i];
        private bool Match(TokKind k) { if (LA.Kind == k) { _i++; return true; } return false; }
        private Token Expect(TokKind k, string msg) { if (LA.Kind != k) throw new ArgumentException(msg); return _toks[_i++]; }

        public Expr ParseExpression() => ParseOr();

        private Expr ParseOr()
        {
            var left = ParseAnd();
            while (Match(TokKind.Or))
            {
                var right = ParseAnd();
                left = new LogicalExpr("OR", left, right);
            }
            return left;
        }

        private Expr ParseAnd()
        {
            var left = ParseNot();
            while (Match(TokKind.And))
            {
                var right = ParseNot();
                left = new LogicalExpr("AND", left, right);
            }
            return left;
        }

        private Expr ParseNot()
        {
            if (Match(TokKind.Not))
            {
                var inner = ParsePrimary();
                return new NotExpr(inner);
            }
            return ParsePrimary();
        }

        private Expr ParsePrimary()
        {
            if (Match(TokKind.LParen))
            {
                var e = ParseExpression();
                Expect(TokKind.RParen, ") 缺失");
                return e;
            }
            return ParsePredicate();
        }

        private static readonly HashSet<TokKind> CompOps = new() { TokKind.Eq, TokKind.Neq, TokKind.Lt, TokKind.Lte, TokKind.Gt, TokKind.Gte };

        private Expr ParsePredicate()
        {
            var col = ParseColumnRef();
            // IS [NOT] NULL
            if (Match(TokKind.Is))
            {
                bool not = Match(TokKind.Not);
                Expect(TokKind.Null, "期望 NULL");
                return new IsNullExpr(col, not);
            }
            // [NOT] LIKE
            bool notLike = false;
            if (LA.Kind == TokKind.Not)
            {
                // lookahead NOT LIKE / NOT IN / NOT BETWEEN
                _i++;
                if (LA.Kind == TokKind.Like)
                {
                    _i++;
                    var lit = ParseLiteral();
                    return new LikeExpr(col, true, lit);
                }
                if (LA.Kind == TokKind.In)
                {
                    _i++;
                    var list = ParseLiteralList();
                    return new InExpr(col, true, list);
                }
                if (LA.Kind == TokKind.Between)
                {
                    _i++;
                    var a = ParseLiteral();
                    Expect(TokKind.And, "期望 AND");
                    var b = ParseLiteral();
                    return new BetweenExpr(col, true, a, b);
                }
                throw new ArgumentException("NOT 后仅允许 LIKE/IN/BETWEEN");
            }
            if (Match(TokKind.Like))
            {
                var lit = ParseLiteral();
                return new LikeExpr(col, notLike, lit);
            }
            if (Match(TokKind.In))
            {
                var list = ParseLiteralList();
                return new InExpr(col, false, list);
            }
            if (Match(TokKind.Between))
            {
                var a = ParseLiteral();
                Expect(TokKind.And, "期望 AND");
                var b = ParseLiteral();
                return new BetweenExpr(col, false, a, b);
            }
            // comparison
            if (!CompOps.Contains(LA.Kind)) throw new ArgumentException("缺少比较运算符");
            var opTok = _toks[_i++];
            var right = ParseLiteral();
            var op = opTok.Kind switch
            {
                TokKind.Eq => "=",
                TokKind.Neq => "<>",
                TokKind.Lt => "<",
                TokKind.Lte => "<=",
                TokKind.Gt => ">",
                TokKind.Gte => ">=",
                _ => throw new ArgumentException("不支持的比较运算符")
            };
            return new CompareExpr(col, op, right);
        }

        private ColumnRef ParseColumnRef()
        {
            var col = new ColumnRef();
            // Expect chain of IDENT parts separated by dots. IDENT token text may be joined by tokenizer for bracketed segments.
            if (LA.Kind != TokKind.Ident) throw new ArgumentException("期望列名");
            var raw = _toks[_i++].Text; // e.g., name or dbo.t.col or [name with space]
            var parts = raw.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            foreach (var p in parts)
            {
                // Trim possible brackets from tokenizer join (we stored inside without brackets)
                var inner = p.Trim().Trim('[', ']');
                if (string.IsNullOrWhiteSpace(inner)) throw new ArgumentException("空的列名片段");
                // Only allow letters, digits, underscore and space inside a part; others rejected
                foreach (var ch in inner)
                {
                    if (!(char.IsLetterOrDigit(ch) || ch == '_' || ch == ' '))
                        throw new ArgumentException("列名包含非法字符");
                }
                col.Parts.Add(inner);
            }
            return col;
        }

        private Literal ParseLiteral()
        {
            if (Match(TokKind.Null)) return new Literal { IsNull = true };
            if (LA.Kind == TokKind.Number)
            {
                var t = _toks[_i++].Text;
                // normalize number
                if (!decimal.TryParse(t, NumberStyles.Any, CultureInfo.InvariantCulture, out _))
                    throw new ArgumentException("非法数字");
                return new Literal { NumberText = t };
            }
            if (LA.Kind == TokKind.String)
            {
                var t = _toks[_i++].Text; // raw content without quotes
                return new Literal { StringValue = t };
            }
            throw new ArgumentException("右侧只允许数字、字符串或 NULL");
        }

        private List<Literal> ParseLiteralList()
        {
            Expect(TokKind.LParen, "期望 (");
            var list = new List<Literal>();
            if (Match(TokKind.RParen)) return list; // empty () not allowed semantically, but keep for syntax completion
            list.Add(ParseLiteral());
            while (Match(TokKind.Comma)) list.Add(ParseLiteral());
            Expect(TokKind.RParen, ") 缺失");
            return list;
        }
    }
    #endregion
}
