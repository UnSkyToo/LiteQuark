using System.Text;

namespace LiteQuark.Editor
{
    internal static class NameUtils
    {
        public static string ToPascalCase(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return name;
            }

            // Handle "id" -> "Id"
            if (name == "id")
            {
                return "Id";
            }

            var sb = new StringBuilder(name.Length);

            if (name.Contains("_"))
            {
                // snake_case -> PascalCase
                var parts = name.Split('_');
                foreach (var part in parts)
                {
                    if (part.Length > 0)
                    {
                        sb.Append(char.ToUpperInvariant(part[0]));
                        if (part.Length > 1)
                        {
                            sb.Append(part.Substring(1));
                        }
                    }
                }
            }
            else
            {
                // camelCase -> PascalCase
                sb.Append(char.ToUpperInvariant(name[0]));
                if (name.Length > 1)
                {
                    sb.Append(name.Substring(1));
                }
            }

            return sb.ToString();
        }

        public static string ToSnakeCase(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return name;
            }

            var sb = new StringBuilder();
            for (var i = 0; i < name.Length; i++)
            {
                var c = name[i];
                if (char.IsUpper(c))
                {
                    if (i > 0)
                    {
                        sb.Append('_');
                    }

                    sb.Append(char.ToLowerInvariant(c));
                }
                else
                {
                    sb.Append(c);
                }
            }

            return sb.ToString();
        }
    }
}