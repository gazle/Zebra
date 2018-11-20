using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ChessLib
{
    public struct Piece
    {
        PieceType pieceType;
        public PieceType PieceType { get { return pieceType; } set { pieceType = value; } }
        PieceColour pieceColour;
        public PieceColour PieceColour { get { return pieceColour; } set { pieceColour = value; } }

        public Piece(PieceType pieceType, PieceColour colour)
        {
            this.pieceType = pieceType;
            this.pieceColour = colour;
        }

        public override bool Equals(Object obj)
        {
            return obj is Piece && this == (Piece)obj;
        }
        public override int GetHashCode()
        {
            return PieceType.GetHashCode() ^ PieceColour.GetHashCode();
        }
        public static bool operator ==(Piece p1, Piece p2)
        {
            return p1.PieceType == p2.PieceType && p1.PieceColour == p2.PieceColour;
        }
        public static bool operator !=(Piece p1, Piece p2)
        {
            return !(p1 == p2);
        }

        public int ToInt()
        {
            // 0 to 15
            return (int)pieceType + 8 * (pieceColour == PieceColour.White ? 0 : (pieceColour == PieceColour.Black ? 1 : 0));
        }

        public override string ToString()
        {
            return PieceType.ToString();
        }
    }
}
