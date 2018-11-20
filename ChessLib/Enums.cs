using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ChessLib
{
    public static class Pieces
    {
        static readonly Piece none = new Piece(PieceType.None, PieceColour.None);
        public static Piece None { get { return none; } }
        static readonly Piece whitePawn = new Piece(PieceType.Pawn, PieceColour.White);
        public static Piece WhitePawn { get { return whitePawn; } }
        static readonly Piece whiteKnight = new Piece(PieceType.Knight, PieceColour.White);
        public static Piece WhiteKnight { get { return whiteKnight; } }
        static readonly Piece whiteBishop = new Piece(PieceType.Bishop, PieceColour.White);
        public static Piece WhiteBishop { get { return whiteBishop; } }
        static readonly Piece whiteRook = new Piece(PieceType.Rook, PieceColour.White);
        public static Piece WhiteRook { get { return whiteRook; } }
        static readonly Piece whiteQueen = new Piece(PieceType.Queen, PieceColour.White);
        public static Piece WhiteQueen { get { return whiteQueen; } }
        static readonly Piece whiteKing = new Piece(PieceType.King, PieceColour.White);
        public static Piece WhiteKing { get { return whiteKing; } }
        static readonly Piece blackPawn = new Piece(PieceType.Pawn, PieceColour.Black);
        public static Piece BlackPawn { get { return blackPawn; } }
        static readonly Piece blackKnight = new Piece(PieceType.Knight, PieceColour.Black);
        public static Piece BlackKnight { get { return blackKnight; } }
        static readonly Piece blackBishop = new Piece(PieceType.Bishop, PieceColour.Black);
        public static Piece BlackBishop { get { return blackBishop; } }
        static readonly Piece blackRook = new Piece(PieceType.Rook, PieceColour.Black);
        public static Piece BlackRook { get { return blackRook; } }
        static readonly Piece blackQueen = new Piece(PieceType.Queen, PieceColour.Black);
        public static Piece BlackQueen { get { return blackQueen; } }
        static readonly Piece blackKing = new Piece(PieceType.King, PieceColour.Black);
        public static Piece BlackKing { get { return blackKing; } }

        static Pieces()
        {
            //None = new Piece(PieceType.None, PieceColour.None);
        }
    }

    public enum PieceType
    {
        None, Pawn, Knight, Bishop, Rook, Queen, King
    }

    public enum PieceColour
    {
        None = 0, White = 1, Black = 2           // Leave as 0 and 1, it is used as an index into piece position arrays
    }

    public enum SpecialMoveType
    {
        Nothing, PawnMove, DoublePawnMove, Promotion, EnPassant, CastleKS, CastleQS
    }

    public enum CastleFlags
    {
        Wks = 1, Wqs = 2, Bks = 4, Bqs = 8
    }
}
