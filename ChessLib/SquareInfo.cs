using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ChessLib
{
    public class SquareInfo
    {
        Piece piece;
        public Piece Piece { get { return piece; } set { piece = value; } }
        SquareInfo prev;
        public SquareInfo Prev { get { return prev; } set { prev = value; } }
        SquareInfo next;
        public SquareInfo Next { get { return next; } set { next = value; } }
        Square location;
        public Square Location { get { return location; } set { location = value; } }

        public SquareInfo(Piece piece, Square location)
        {
            this.piece = piece;
            this.location = location;
        }
    }
}