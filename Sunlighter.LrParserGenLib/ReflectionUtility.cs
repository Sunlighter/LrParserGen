using Sunlighter.TypeTraitsLib;
using System.Reflection;

namespace Sunlighter.LrParserGenLib
{
    public sealed class ReflectionResults
    {
        private readonly Grammar grammar;
        private readonly ImmutableParserState<object?> initialState;
        private readonly ImmutableList<string> specialTokens;

        public ReflectionResults
        (
            Grammar grammar,
            ImmutableParserState<object?> initialState,
            ImmutableList<string> specialTokens
        )
        {
            this.grammar = grammar;
            this.initialState = initialState;
            this.specialTokens = specialTokens;
        }

        public Grammar Grammar => grammar;

        public ImmutableParserState<object?> InitialState => initialState;

        public ImmutableList<string> SpecialTokens => specialTokens;
    }

    public static class ReflectionUtility
    {
        private static ImmutableList<MethodBase> GetGrammarRules(this Type sourceType)
        {
            ImmutableList<MethodBase> results = ImmutableList<MethodBase>.Empty;

            foreach(ConstructorInfo ci in sourceType.GetConstructors(BindingFlags.Public | BindingFlags.Instance).Where(c => c.IsDefined(typeof(GrammarRuleAttribute))))
            {
                results = results.Add(ci);
            }

            foreach(MethodInfo mi in sourceType.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance).Where(m => m.IsDefined(typeof(GrammarRuleAttribute))))
            {
                results = results.Add(mi);
            }

            return results;
        }

        private static ImmutableList<PrecedenceRule> GetPrecedenceRules(this Type sourceType)
        {
            return sourceType.GetCustomAttributes<PrecedenceRuleAttribute>()
                .OrderBy(pra => pra.Level)
                .Select
                (
                    pra => new PrecedenceRule
                    (
                        ImmutableSortedSet<Symbol>.Empty
                            .WithComparer(Symbol.Adapter)
                            .Union(pra.Symbols.SplitOnWhiteSpace().Select(s => new NamedSymbol(s))),
                        pra.Associativity
                    )
                )
                .ToImmutableList();
        }

        private static ReductionFunc<T> BuildReductionFunc<T>(ImmutableList<Func<ImmutableList<T>, T>> reductionRules)
        {
            T result(ReductionInfo info, ImmutableList<T> args)
            {
                if (info.RuleNumber > 0 && info.RuleNumber <= reductionRules.Count)
                {
                    return reductionRules[info.RuleNumber - 1](args);
                }
                else
                {
                    throw new InvalidOperationException($"No reduction found for rule {info.RuleNumber}");
                }
            }

            return result;
        }

        private static Func<ImmutableList<object?>, object?> GetReduction(MethodBase mb)
        {
            if (mb is ConstructorInfo ci)
            {
                object? doCi(ImmutableList<object?> args)
                {
                    return ci.Invoke(args.ToArray());
                }

                return doCi;
            }
            else if (mb is MethodInfo mi)
            {
                if (mi.IsStatic)
                {
                    object? doStaticMi(ImmutableList<object?> args)
                    {
                        return mi.Invoke(null, args.ToArray());
                    }

                    return doStaticMi;
                }
                else
                {
                    object? doInstanceMi(ImmutableList<object?> args)
                    {
                        if (args.Count > 0)
                        {
                            return mi.Invoke(args[0], args.RemoveAt(0).ToArray());
                        }
                        else
                        {
                            throw new ArgumentException("Insufficient arguments");
                        }
                    }

                    return doInstanceMi;
                }
            }
            else throw new InvalidOperationException("Unknown type of MethodBase");
        }

        private static Rule GetRule(MethodBase mb)
        {
            Symbol lhs;
            ImmutableList<Symbol> rhs = ImmutableList<Symbol>.Empty;

            Symbol GetParameterSymbol(ParameterInfo pi)
            {
                if (pi.IsDefined(typeof(TokenTypeNameAttribute)))
                {
                    TokenTypeNameAttribute n = pi.GetCustomAttribute<TokenTypeNameAttribute>(false).AssertNotNull();
                    return new NamedSymbol(n.Name);
                }
                else if (pi.IsDefined(typeof(TokenTypeAttribute)))
                {
                    TokenTypeAttribute t = pi.GetCustomAttribute<TokenTypeAttribute>(false).AssertNotNull();
                    return new TypeSymbol(t.TokenType);
                }
                else
                {
                    return new TypeSymbol(pi.ParameterType);
                }
            }

            void AddParameters()
            {
                foreach(ParameterInfo pi in mb.GetParameters())
                {
                    rhs = rhs.Add(GetParameterSymbol(pi));
                }
            }

            if (mb is ConstructorInfo ci)
            {
                lhs = new TypeSymbol(ci.DeclaringType.AssertNotNull());
                AddParameters();
            }
            else if (mb is MethodInfo mi)
            {
                lhs = GetParameterSymbol(mi.ReturnParameter);
                AddParameters();
                if (!mi.IsStatic)
                {
                    rhs = rhs.Insert(0, new TypeSymbol(mi.DeclaringType.AssertNotNull()));
                }
            }
            else
            {
                throw new InvalidOperationException("Unknown type of MethodBase");
            }

            return new Rule(lhs, rhs);
        }

        public static ReflectionResults BuildGrammar(this Type sourceType, Symbol goal)
        {
            ImmutableList<MethodBase> mb = sourceType.GetGrammarRules();
            ImmutableList<PrecedenceRule> precedenceRules = sourceType.GetPrecedenceRules();

            ImmutableList<Func<ImmutableList<object?>, object?>> reductions = mb.Select(GetReduction).ToImmutableList();
            ImmutableList<Rule> rules = mb.Select(GetRule).ToImmutableList();

            rules = rules.Insert(0, new Rule(StartSymbol.Value, ImmutableList<Symbol>.Empty.Add(goal)));

            Grammar g = new Grammar(rules, precedenceRules);
            ReductionFunc<object?> rFunc = BuildReductionFunc(reductions);

            ImmutableList<string> specialTokens = g.GrammarSymbols.OfType<NamedSymbol>().Select(ns => ns.Name).ToImmutableList();

            return new ReflectionResults(g, new ImmutableParserState<object?>(g.IntParseTableData, g.GetRuleInfo(), rFunc), specialTokens);
        }
    }

    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor, AllowMultiple = false, Inherited = false)]
    public sealed class GrammarRuleAttribute : Attribute
    {

    }

    [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.ReturnValue, AllowMultiple = false, Inherited = false)]
    public sealed class TokenTypeNameAttribute(string tokenTypeName) : Attribute
    {
        public string Name => tokenTypeName;
    }

    [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.ReturnValue, AllowMultiple = false, Inherited = false)]
    public sealed class TokenTypeAttribute(Type tokenType) : Attribute
    {
        public Type TokenType => tokenType;
    }

    [AttributeUsage(AttributeTargets.Class)]
    public sealed class PrecedenceRuleAttribute(int level, string symbols, Associativity associativity) : Attribute
    {
        public int Level => level;

        public string Symbols => symbols;

        public Associativity Associativity => associativity;
    }
}
