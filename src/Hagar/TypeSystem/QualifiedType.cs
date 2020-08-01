namespace Hagar.TypeSystem
{
    public readonly struct QualifiedType
    {
        public QualifiedType(string assembly, string type)
        {
            this.Assembly = assembly;
            this.Type = type;
        }

        public void Deconstruct(out string assembly, out string type)
        {
            assembly = this.Assembly;
            type = this.Type;
        }

        public static implicit operator QualifiedType((string Assembly, string Type) args) => new QualifiedType(args.Assembly, args.Type);

        public string Assembly { get; }
        public string Type { get; }
    }
}