using Sunlighter.TypeTraitsLib;
using Sunlighter.TypeTraitsLib.Building;
using System.Reflection;

namespace Sunlighter.LrParserGenLib
{
    public abstract class ReflectionResults
    {
        private readonly Grammar grammar;
        private readonly ImmutableList<string> specialTokens;

        protected ReflectionResults
        (
            Grammar grammar,
            ImmutableList<string> specialTokens
        )
        {
            this.grammar = grammar;
            this.specialTokens = specialTokens;
        }

        public Grammar Grammar => grammar;

        public ImmutableList<string> SpecialTokens => specialTokens;
    }

    public sealed class StaticReflectionResults : ReflectionResults
    {
        private readonly ImmutableParserState<object?> initialState;

        public StaticReflectionResults
        (
            Grammar grammar,
            ImmutableParserState<object?> initialState,
            ImmutableList<string> specialTokens
        )
            : base(grammar, specialTokens)
        {
            this.initialState = initialState;
        }

        public ImmutableParserState<object?> InitialState => initialState;
    }

    public sealed class InstanceReflectionResults : ReflectionResults
    {
        private readonly Func<object?, ImmutableParserState<object?>> unboundInitialState;

        public InstanceReflectionResults
        (
            Grammar grammar,
            Func<object?, ImmutableParserState<object?>> unboundInitialState,
            ImmutableList<string> specialTokens
        )
            : base(grammar, specialTokens)
        {
            this.unboundInitialState = unboundInitialState;
        }

        public ImmutableParserState<object?> GetInitialState(object? instance)
        {
            return unboundInitialState(instance);
        }
    }

    public static class ReflectionUtility
    {
        private static ImmutableList<MethodInfo> GetGrammarRuleMethods(this Type sourceType)
        {
            ImmutableList<MethodInfo> results = ImmutableList<MethodInfo>.Empty;

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
                            .WithComparer(Builder.Instance.GetAdapter<Symbol>())
                            .Union(pra.Symbols.SplitOnWhiteSpace().Select(s => new NamedSymbol(s))),
                        pra.Associativity
                    )
                )
                .ToImmutableList();
        }

        private static ReductionFunc<T> BuildStaticReductionFunc<T>(ImmutableList<Func<ImmutableList<T>, T>> reductionRules)
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

        private static Func<object?, ReductionFunc<T>> BuildReductionFunc<T>(ImmutableList<Func<object?, Func<ImmutableList<T>, T>>> reductionRules)
        {
            ReductionFunc<T> bindReductionFunc(object? instance)
            {
                ImmutableList<Func<ImmutableList<T>, T>> boundReductionRules = reductionRules.Select(r => r(instance)).ToImmutableList();

                T result(ReductionInfo info, ImmutableList<T> args)
                {
                    if (info.RuleNumber > 0 && info.RuleNumber <= reductionRules.Count)
                    {
                        return boundReductionRules[info.RuleNumber - 1](args);
                    }
                    else
                    {
                        throw new InvalidOperationException($"No reduction found for rule {info.RuleNumber}");
                    }
                }

                return result;
            }

            return bindReductionFunc;
        }

        private static Func<ImmutableList<object?>, object?> GetStaticReduction(MethodInfo mi)
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
                throw new InvalidOperationException("Method is not static");
            }
        }
        
        private static Func<object?, Func<ImmutableList<object?>, object?>> GetReduction(MethodInfo mi)
        {
            if (mi.IsStatic)
            {
                Func<ImmutableList<object?>, object?> bindStaticMi(object? _)
                {
                    object? doStaticMi(ImmutableList<object?> args)
                    {
                        return mi.Invoke(null, args.ToArray());
                    }
                    return doStaticMi;
                }

                return bindStaticMi;
            }
            else
            {
                Func<ImmutableList<object?>, object?> bindInstanceMi(object? instance)
                {
                    object? doInstanceMi(ImmutableList<object?> args)
                    {
                        return mi.Invoke(instance, args.ToArray());
                    }

                    return doInstanceMi;
                }

                return bindInstanceMi;
            }
        }

        private static Rule GetRule(MethodInfo mi)
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
                foreach(ParameterInfo pi in mi.GetParameters())
                {
                    rhs = rhs.Add(GetParameterSymbol(pi));
                }
            }

            lhs = GetParameterSymbol(mi.ReturnParameter);
            AddParameters();

            return new Rule(lhs, rhs);
        }

        private static Lazy<ITypeTraits<(ImmutableList<Rule>, ImmutableList<PrecedenceRule>)>> grammarInfoTypeTraits =
            new Lazy<ITypeTraits<(ImmutableList<Rule>, ImmutableList<PrecedenceRule>)>>(GetGrammarInfoTypeTraits, LazyThreadSafetyMode.ExecutionAndPublication);

        private static ITypeTraits<(ImmutableList<Rule>, ImmutableList<PrecedenceRule>)> GetGrammarInfoTypeTraits()
        {
            return new ValueTupleTypeTraits<ImmutableList<Rule>, ImmutableList<PrecedenceRule>>
            (
                new ListTypeTraits<Rule>(Builder.Instance.GetTypeTraits<Rule>()),
                new ListTypeTraits<PrecedenceRule>(Builder.Instance.GetTypeTraits<PrecedenceRule>())
            );
        }

        public static ReflectionResults BuildParser(this Type sourceType, Symbol goal, ICacheStorage? cacheStorage = null)
        {
            ImmutableList<MethodInfo> miList = sourceType.GetGrammarRuleMethods();
            ImmutableList<PrecedenceRule> precedenceRules = sourceType.GetPrecedenceRules();

            ImmutableList<Rule> rules = miList.Select(GetRule).ToImmutableList();
            rules = rules.Insert(0, new Rule(StartSymbol.Value, ImmutableList<Symbol>.Empty.Add(goal)));
            Grammar g = new Grammar(rules, precedenceRules);
            ImmutableList<string> specialTokens = g.GrammarSymbols.OfType<NamedSymbol>().Select(ns => ns.Name).ToImmutableList();

            IntParseTableData parseTable = (cacheStorage ?? NullCache.Value).GetCachedValue
            (
                grammarInfoTypeTraits.Value,
                IntParseTableData.TypeTraits,
                (rules, precedenceRules),
                _ => g.IntParseTableData.WithoutConversionDictionary()
            );

            if (miList.Any(mi => !mi.IsStatic))
            {
                ImmutableList<Func<object?, Func<ImmutableList<object?>, object?>>> reductions = miList.Select(GetReduction).ToImmutableList();
                Func<object?, ReductionFunc<object?>> rFunc = BuildReductionFunc(reductions);
                return new InstanceReflectionResults(g, obj => new ImmutableParserState<object?>(parseTable, g.GetRuleInfo(), rFunc(obj)), specialTokens);
            }
            else
            {
                ImmutableList<Func<ImmutableList<object?>, object?>> reductions = miList.Select(GetStaticReduction).ToImmutableList();
                ReductionFunc<object?> rFunc = BuildStaticReductionFunc(reductions);
                return new StaticReflectionResults(g, new ImmutableParserState<object?>(parseTable, g.GetRuleInfo(), rFunc), specialTokens);
            }
        }
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
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
