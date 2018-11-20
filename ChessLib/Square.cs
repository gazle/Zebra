using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ChessLib
{
    public struct Square
    {
        int file;
        int rank;
        public int File { get { return file; } set { file = value; } }
        public int Rank { get { return rank; } set { rank = value; } }
        public int SquareNr { get { return file + rank * 8; } set { file = value & 7; rank = value >> 3; } }

        public Square(int file, int rank)
        {
            this.file = file;
            this.rank = rank;
        }

        public Square(string squareString)
        {
            this.file = squareString[0] - 'a';
            this.rank = squareString[1] - '1';
        }

        public Square(int squareNr)
        {
            this.file = squareNr & 7;
            this.rank = squareNr >> 3;
        }

        public bool isOnTheBoard()
        {
            if (file >= 0 && file < 8 && rank >= 0 && rank < 8) return true; else return false;
        }

        public static implicit operator int(Square s)
        {
            return s.Rank * 8 + s.File;
        }

        public override bool Equals(Object obj)
        {
            return obj is Square && this == (Square)obj;
        }
        public override int GetHashCode()
        {
            return file.GetHashCode() ^ rank.GetHashCode();
        }
        public static bool operator ==(Square sq1, Square sq2)
        {
            return sq1.file == sq2.file && sq1.rank == sq2.rank;
        }
        public static bool operator !=(Square sq1, Square sq2)
        {
            return !(sq1 == sq2);
        }

        public override string ToString()
        {
            return ((char)(File + 'a')).ToString() + ((char)(Rank + '1')).ToString();
        }
    }
}
