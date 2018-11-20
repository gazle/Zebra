using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ChessLib
{
    class ToMoveData
    {
        public readonly PieceColour Colour;
        public readonly Piece Pawn;
        public readonly Piece Knight;
        public readonly Piece Bishop;
        public readonly Piece Rook;
        public readonly Piece Queen;
        public readonly Piece King;
        public readonly int PawnStartRank;
        public readonly int PawnPromRank;
        public readonly int PawnYDir;
        public readonly CastleFlags CanCastleKS;
        public readonly CastleFlags CanCastleQS;
        public readonly Square KingStartSq;
        public readonly Square KingRookSq;
        public readonly Square QueenRookSq;
        public readonly Square KingKCastleSq;
        public readonly Square KingQCastleSq;
        public readonly Square RookKCastleSq;
        public readonly Square RookQCastleSq;
        public SquareInfo KingSqInfo { get; set; }

        public ToMoveData(PieceColour colour, Piece pawn, Piece knight, Piece bishop, Piece rook, Piece queen, Piece king, int pawnStartRank, int pawnPromRank, int pawnYDir,
            CastleFlags canCastleKS, CastleFlags canCastleQS, Square kingStartSq, Square kingRookSq, Square queenRookSq)
        {
            Colour = colour;
            Pawn = pawn;
            Knight = knight;
            Bishop = bishop;
            Rook = rook;
            Queen = queen;
            King = king;
            PawnStartRank = pawnStartRank;
            PawnPromRank = pawnPromRank;
            PawnYDir = pawnYDir;
            CanCastleKS = canCastleKS;
            CanCastleQS = canCastleQS;
            KingStartSq = kingStartSq;
            KingRookSq = kingRookSq;
            QueenRookSq = queenRookSq;
            KingKCastleSq = new Square(kingStartSq.File + 2, kingStartSq.Rank);
            KingQCastleSq = new Square(kingStartSq.File - 2, kingStartSq.Rank);
            RookKCastleSq = new Square(kingStartSq.File + 1, kingStartSq.Rank);
            RookQCastleSq = new Square(kingStartSq.File - 1, kingStartSq.Rank);
        }
    }
}
