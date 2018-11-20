using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ChessLib
{
    public class ChessMove
    {
        Square fromSq;
        public Square FromSq { get { return fromSq; } }
        Square toSq;
        public Square ToSq { get { return toSq; } }
        Piece promotedTo;
        public Piece PromotedTo { get { return promotedTo; } }
        SpecialMoveType special;
        public SpecialMoveType Special { get { return special; } }
        internal Piece Piece;
        internal SquareInfo CapturedSqInfo;
        internal int HalfMovesSinceLastPawnMoveOrCapture;
        internal Square OldEpSq;
        internal CastleFlags OldCastleFlags;
        internal UInt64 OldHashKey;
        internal int Score;
        public int GamePlyNr { get; internal set; }
        public string Text { get; internal set; }

        public ChessMove(Square fromSq, Square toSq)
        {
            this.fromSq = fromSq;
            this.toSq = toSq;
            this.special = SpecialMoveType.Nothing;
        }

        public ChessMove(Square fromSq, Square toSq, SpecialMoveType special)
        {
            this.fromSq = fromSq;
            this.toSq = toSq;
            this.special = special;
        }

        public ChessMove(Square fromSq, Square toSq, Piece promotedTo)
        {
            this.fromSq = fromSq;
            this.toSq = toSq;
            this.special = SpecialMoveType.Promotion;
            this.promotedTo = promotedTo;
        }

        public override string ToString()
        {
            if (Text == null)
                return fromSq.ToString() + toSq.ToString() + (promotedTo.PieceType != PieceType.None ? " pnbrqk".Substring((int)promotedTo.PieceType, 1) : "");
            else
                return Text;
        }
    }
}
