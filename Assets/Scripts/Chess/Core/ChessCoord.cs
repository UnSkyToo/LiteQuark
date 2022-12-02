namespace LiteGamePlay.Chess
{
    /// <summary>
    /// <para>X : A - O</para>
    /// <para>Y : 1 - 15</para>
    /// </summary>
    public class ChessCoord
    {
        public int X { get; }
        public int Y { get; }

        public ChessCoord(int x, int y)
        {
            X = x;
            Y = y;
        }

        public bool IsValid(int width, int height)
        {
            return X >= 0 && Y >= 0 && X < width && Y < height;
        }

        public override string ToString()
        {
            return $"{(char)(X + 'A')}{Y + 1}";
        }

        public static ChessCoord Parse(string data)
        {
            try
            {
                if (data.Length != 2 && data.Length != 3)
                {
                    return null;
                }

                if (!char.IsLetter(data[0]))
                {
                    return null;
                }

                var ix = char.ToLower(data[0]) - 'a';

                var y = data.Substring(1);
                if (!int.TryParse(y, out var iy))
                {
                    return null;
                }

                return new ChessCoord(ix, iy - 1);
            }
            catch
            {
                return null;
            }
        }
    }
}