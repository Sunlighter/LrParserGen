using Sunlighter.TypeTraitsLib;

namespace Sunlighter.LrParserGenLib
{
    public abstract class Symbol
    {
        private static ITypeTraits<Symbol> GetTypeTraits()
        {
            return new UnionTypeTraits<string, Symbol>
            (
                StringTypeTraits.Value,
                ImmutableList<IUnionCaseTypeTraits<string, Symbol>>.Empty.AddRange
                (
                    new IUnionCaseTypeTraits<string, Symbol>[]
                    {
                        new UnionCaseTypeTraits2<string, Symbol, StartSymbol>
                        (
                            "start",
                            new ConvertTypeTraits<StartSymbol, DBNull>(e => DBNull.Value, new UnitTypeTraits<DBNull>(HashToken.None, DBNull.Value), d => StartSymbol.Value)
                        ),
                        new UnionCaseTypeTraits2<string, Symbol, UnnamedSymbol>
                        (
                            "unnamed",
                            UnnamedSymbolTypeTraits.Value
                        ),
                        new UnionCaseTypeTraits2<string, Symbol, NamedSymbol>
                        (
                            "named",
                            new ConvertTypeTraits<NamedSymbol, string>(n => n.Name, StringTypeTraits.Value, s => new NamedSymbol(s))
                        ),
                        new UnionCaseTypeTraits2<string, Symbol, TypeSymbol>
                        (
                            "typeAsSymbol",
                            new ConvertTypeTraits<TypeSymbol, Type>(ts => ts.Value, TypeTypeTraits.Value, ty => new TypeSymbol(ty))
                        ),
                        new UnionCaseTypeTraits2<string, Symbol, EpsilonSymbol>
                        (
                            "epsilon",
                            new ConvertTypeTraits<EpsilonSymbol, DBNull>(e => DBNull.Value, new UnitTypeTraits<DBNull>(HashToken.None, DBNull.Value), d => EpsilonSymbol.Value)
                        ),
                        new UnionCaseTypeTraits2<string, Symbol, DotSymbol>
                        (
                            "dot",
                            new ConvertTypeTraits<DotSymbol, DBNull>(e => DBNull.Value, new UnitTypeTraits<DBNull>(HashToken.None, DBNull.Value), d => DotSymbol.Value)
                        ),
                        new UnionCaseTypeTraits2<string, Symbol, EofSymbol>
                        (
                            "eof",
                            new ConvertTypeTraits<EofSymbol, DBNull>(e => DBNull.Value, new UnitTypeTraits<DBNull>(HashToken.None, DBNull.Value), d=> EofSymbol.Value)
                        ),
                    }
                )
            );
        }

        private static readonly Lazy<ITypeTraits<Symbol>> typeTraits = new Lazy<ITypeTraits<Symbol>>(GetTypeTraits, LazyThreadSafetyMode.ExecutionAndPublication);

        public static ITypeTraits<Symbol> Traits => typeTraits.Value;

        private static readonly Lazy<Adapter<Symbol>> adapter = new Lazy<Adapter<Symbol>>(() => Adapter<Symbol>.Create(typeTraits.Value), LazyThreadSafetyMode.ExecutionAndPublication);

        public static Adapter<Symbol> Adapter => adapter.Value;

        private static readonly Lazy<ImmutableSortedSet<Symbol>> emptySet =
            new Lazy<ImmutableSortedSet<Symbol>>(() => ImmutableSortedSet<Symbol>.Empty.WithComparer(adapter.Value), LazyThreadSafetyMode.ExecutionAndPublication);

        public static ImmutableSortedSet<Symbol> EmptySet => emptySet.Value;
    }

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

    public sealed class UnnamedSymbolTypeTraits : ITypeTraits<UnnamedSymbol>
    {
        private static readonly Lazy<SerializerStateID> stateId = new Lazy<SerializerStateID>(() => new SerializerStateID(), LazyThreadSafetyMode.ExecutionAndPublication);

        private static readonly UnnamedSymbolTypeTraits value = new UnnamedSymbolTypeTraits();

        private UnnamedSymbolTypeTraits() { }

        public static UnnamedSymbolTypeTraits Value => value;

        public int Compare(UnnamedSymbol a, UnnamedSymbol b)
        {
            return Int64TypeTraits.Value.Compare(a.ID, b.ID);
        }

        public void AddToHash(HashBuilder b, UnnamedSymbol a)
        {
            Int64TypeTraits.Value.AddToHash(b, a.ID);
        }

        public bool CanSerialize(UnnamedSymbol a) => true;

        private sealed class SerializerState
        {
            private int next;
            private ImmutableSortedDictionary<long, int> done;

            public SerializerState()
            {
                next = 0;
                done = ImmutableSortedDictionary<long, int>.Empty;
            }

            public int GetInt32(UnnamedSymbol us)
            {
                if (done.TryGetValue(us.ID, out int i))
                {
                    return i;
                }
                else
                {
                    int idx = next;
                    ++next;
                    done = done.Add(us.ID, idx);
                    return idx;
                }
            }
        }

        public void Serialize(Serializer dest, UnnamedSymbol a)
        {
            SerializerState ss = dest.GetSerializerState(stateId.Value, () => new SerializerState());
            dest.Writer.Write(ss.GetInt32(a));
        }

        private sealed class DeserializerState
        {
            private ImmutableSortedDictionary<int, UnnamedSymbol> done;

            public DeserializerState()
            {
                done = ImmutableSortedDictionary<int, UnnamedSymbol>.Empty;
            }

            public UnnamedSymbol GetSymbol(int serializedId)
            {
                if (done.TryGetValue(serializedId, out UnnamedSymbol? us))
                {
                    return us;
                }
                else
                {
                    UnnamedSymbol us2 = new UnnamedSymbol();
                    done = done.Add(serializedId, us2);
                    return us2;
                }
            }
        }

        public UnnamedSymbol Deserialize(Deserializer src)
        {
            DeserializerState ds = src.GetSerializerState(stateId.Value, () => new DeserializerState());
            return ds.GetSymbol(src.Reader.ReadInt32());
        }

        public void MeasureBytes(ByteMeasurer measurer, UnnamedSymbol a)
        {
            measurer.AddBytes(4L);
        }

        public void AppendDebugString(DebugStringBuilder sbm, UnnamedSymbol a)
        {
            StringBuilder sb = sbm.Builder;
            sb.Append('#');
            sb.Append(a.ID);
        }
    }

    public sealed class NamedSymbol : Symbol
    {
        private readonly string name;

        public NamedSymbol(string name)
        {
            this.name = name;
        }

        public string Name => name;
    }

    public sealed class TypeSymbol : Symbol
    {
        private readonly Type value;

        public TypeSymbol(Type value)
        {
            this.value = value;
        }

        public Type Value => value;
    }

    public sealed class EpsilonSymbol : Symbol
    {
        private static readonly EpsilonSymbol value = new EpsilonSymbol();

        private EpsilonSymbol() { }

        public static EpsilonSymbol Value => value;
    }

    public sealed class DotSymbol : Symbol
    {
        private static readonly DotSymbol value = new DotSymbol();

        private DotSymbol() { }

        public static DotSymbol Value => value;
    }

    public sealed class EofSymbol : Symbol
    {
        private static readonly EofSymbol value = new EofSymbol();

        private EofSymbol() { }

        public static EofSymbol Value => value;
    }

    public sealed class StartSymbol : Symbol
    {
        private static readonly StartSymbol value = new StartSymbol();

        private StartSymbol() { }

        public static StartSymbol Value => value;
    }
}
