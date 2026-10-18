using System;

namespace Switchboard.Domain
{
    /// <summary>
    /// The open part of the board (D42, D49): always the top-left rectangle, rows A to the Rows-th letter and
    /// columns 1 to Cols. Jack labels are a row letter and a column number, such as "C4".
    /// </summary>
    public readonly struct BoardSize : IEquatable<BoardSize>
    {
        /// <summary>The team's board in Final.unity is 6 by 6 (rows A to F, columns 1 to 6), the only fixed limit.</summary>
        public const int MaxSide = 6;

        public static readonly BoardSize Full = new BoardSize(MaxSide, MaxSide);

        public BoardSize(int cols, int rows)
        {
            Cols = cols;
            Rows = rows;
        }

        public int Cols { get; }
        public int Rows { get; }

        public bool IsValid => Cols >= 1 && Cols <= MaxSide && Rows >= 1 && Rows <= MaxSide;

        /// <summary>The letter of the last open row, such as 'C' for 3 rows.</summary>
        public char LastRow => (char)('A' + Math.Max(0, Rows - 1));

        public bool Contains(JackId jack) => Contains(jack.Label);

        public bool Contains(string label)
        {
            if (string.IsNullOrEmpty(label) || label.Length < 2) return false;
            int row = char.ToUpperInvariant(label[0]) - 'A';
            int column = 0;
            for (int i = 1; i < label.Length; i++)
            {
                char c = label[i];
                if (c < '0' || c > '9') return false;
                column = column * 10 + (c - '0');
                if (column > MaxSide) return false;
            }
            return row >= 0 && row < Rows && column >= 1 && column <= Cols;
        }

        public bool Equals(BoardSize other) => Cols == other.Cols && Rows == other.Rows;
        public override bool Equals(object obj) => obj is BoardSize other && Equals(other);
        public override int GetHashCode() => Cols * 31 + Rows;

        /// <summary>Columns by rows, as the designer writes it, such as "4x4".</summary>
        public override string ToString() => $"{Cols}x{Rows}";
    }
}
