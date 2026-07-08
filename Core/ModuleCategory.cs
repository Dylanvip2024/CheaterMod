using System;

namespace FullBrightMod.Core
{
    public sealed class ModuleCategory : IEquatable<ModuleCategory>
    {
        public string Name { get; }

        private ModuleCategory(string name) { Name = name; }

        public static readonly ModuleCategory Combat   = new ModuleCategory("Combat");
        public static readonly ModuleCategory Player    = new ModuleCategory("Player");
        public static readonly ModuleCategory Movement = new ModuleCategory("Movement");
        public static readonly ModuleCategory Render    = new ModuleCategory("Render");
        public static readonly ModuleCategory World     = new ModuleCategory("World");
        public static readonly ModuleCategory Misc      = new ModuleCategory("Misc");
        public static readonly ModuleCategory Hidden    = new ModuleCategory("Hidden");

        public static ModuleCategory Create(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            return new ModuleCategory(name);
        }

        public bool Equals(ModuleCategory other)
        {
            if (ReferenceEquals(other, null)) return false;
            if (ReferenceEquals(this, other)) return true;
            return string.Equals(Name, other.Name, StringComparison.OrdinalIgnoreCase);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as ModuleCategory);
        }

        public override int GetHashCode()
        {
            return Name?.ToLowerInvariant().GetHashCode() ?? 0;
        }

        public override string ToString() => Name;

        public static bool operator ==(ModuleCategory a, ModuleCategory b)
        {
            if (ReferenceEquals(a, b)) return true;
            if (ReferenceEquals(a, null) || ReferenceEquals(b, null)) return false;
            return a.Equals(b);
        }

        public static bool operator !=(ModuleCategory a, ModuleCategory b) => !(a == b);
    }
}
