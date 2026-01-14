using Sunlighter.TypeTraitsLib;
using Sunlighter.TypeTraitsLib.Building;

namespace Sunlighter.LrParserGenLib
{
    [UnionOfDescendants]
    public abstract class Symbol
    {
        private static readonly Lazy<ImmutableSortedSet<Symbol>> emptySet =
            new Lazy<ImmutableSortedSet<Symbol>>(() => ImmutableSortedSet<Symbol>.Empty.WithComparer(Builder.Instance.GetAdapter<Symbol>()), LazyThreadSafetyMode.ExecutionAndPublication);

        public static ImmutableSortedSet<Symbol> EmptySet => emptySet.Value;
    }

    [GensymInt32]
    [UnionCaseName("unnamed")]
    public sealed class UnnamedSymbol : Symbol
    {
        private static readonly object staticSyncRoot = new object();
        private static long nextId = 0L;

        private readonly long id;

        public UnnamedSymbol()
        {
            lock(staticSyncRoot)
            {
                id = nextId;
                ++nextId;
            }
        }

        public long ID => id;
    }

    [Record]
    [UnionCaseName("named")]
    public sealed class NamedSymbol : Symbol
    {
        private readonly string name;

        public NamedSymbol([Bind("name")] string name)
        {
            this.name = name;
        }

        [Bind("name")]
        public string Name => name;
    }

    [Record]
    [UnionCaseName("type")]
    public sealed class TypeSymbol : Symbol
    {
        private readonly Type value;

        public TypeSymbol([Bind("value")] Type value)
        {
            this.value = value;
        }

        [Bind("value")]
        public Type Value => value;
    }

    [Singleton(0xA2F4212Bu)]
    [UnionCaseName("epsilon")]
    public sealed class EpsilonSymbol : Symbol
    {
        private static readonly EpsilonSymbol value = new EpsilonSymbol();

        private EpsilonSymbol() { }

        public static EpsilonSymbol Value => value;
    }

    [Singleton(0x88B6F56Au)]
    [UnionCaseName("dot")]
    public sealed class DotSymbol : Symbol
    {
        private static readonly DotSymbol value = new DotSymbol();

        private DotSymbol() { }

        public static DotSymbol Value => value;
    }

    [Singleton(0x50662C62u)]
    [UnionCaseName("eof")]
    public sealed class EofSymbol : Symbol
    {
        private static readonly EofSymbol value = new EofSymbol();

        private EofSymbol() { }

        public static EofSymbol Value => value;
    }

    [Singleton(0x5B072AB2u)]
    [UnionCaseName("start")]
    public sealed class StartSymbol : Symbol
    {
        private static readonly StartSymbol value = new StartSymbol();

        private StartSymbol() { }

        public static StartSymbol Value => value;
    }
}
