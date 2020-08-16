namespace Hagar.TypeSystem
{
    public readonly struct QualifiedType
    {
        public QualifiedType(string assembly, string type)
        {
            Assembly = assembly;
            Type = type;
        }

        public void Deconstruct(out string assembly, out string type)
        {
            assembly = Assembly;
            type = Type;
        }

        public static implicit operator QualifiedType((string Assembly, string Type) args) => new QualifiedType(args.Assembly, args.Type);

        public string Assembly { get; }
        public string Type { get; }
    }
}