using System.Text.RegularExpressions;

namespace Sunlighter.LrParserGenLib
{
    public sealed class Token<T>(Symbol type, T value)
    {
        private readonly Symbol type = type;
        private readonly T value = value;

        public Symbol Type => type;

        public T Value => value;
    }

    public static class Tokenization
    {
        private static readonly Regex rWhiteSpace = new Regex("\\s+", RegexOptions.Compiled);

        public static ImmutableList<string> SplitOnWhiteSpace(this string str)
        {
            if (string.IsNullOrWhiteSpace(str)) return ImmutableList<string>.Empty;

            return rWhiteSpace.Split(str).ToImmutableList();
        }

        public static ImmutableList<Token<object?>> Tokenize(this string str, Func<string, Token<object?>> tokenizeItem)
        {
            return str.SplitOnWhiteSpace().Select(tokenizeItem).ToImmutableList();
        }

        public static Func<string, Token<object?>> MakeTokenizer(ImmutableList<string> specialTokens)
        {
            ImmutableSortedSet<string> specialTokenSet = specialTokens.ToImmutableSortedSet();

            return delegate (string str)
            {
                if (specialTokenSet.Contains(str))
                {
                    return new Token<object?>(new NamedSymbol(str), str);
                }
                else if (BigInteger.TryParse(str, out BigInteger result))
                {
                    return new Token<object?>(new TypeSymbol(typeof(BigInteger)), result);
                }
                else
                {
                    return new Token<object?>(new TypeSymbol(typeof(string)), str);
                }
            };
        }
    }
}
