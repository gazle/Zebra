using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
using System.IO;

namespace ChessLib
{
    public class ChessPosition
    {
        public const string StartFEN = "rnbqkbnr/pppppppp/////PPPPPPPP/RNBQKBNR w KQkq - 0 1";
        public string Event { get; set; }
        public string Site { get; set; }
        public string Date { get; set; }
        public string Round { get; set; }
        public string White { get; set; }
        public string Black { get; set; }
        public string Result { get; set; }
        const string pieceStr = " PNBRQK";                        // Used for decoding the FENstr
        const int maxDepth = 10;
        const int pvScore = 2000000;
        const int captureScore = 1000000;
        const int killer0Score = 900000;
        const int killer1Score = 800000;
        public SquareInfo[] Board { get; private set; }
        SquareInfo[] emptyBoard;
        public Collection<ChessMove> PossibleMoves { get; private set; }
        public ChessMove[,] Princ { get; private set; }                      // Principal variation
        Collection<ChessMove> tmpMoveList;
        ToMoveData thisSide;
        ToMoveData otherSide;
        ToMoveData white;
        ToMoveData black;
        public UInt64 HashKey { get; private set; }
        public int NodesEvaluated { get; private set; }
        UInt64[,] pieceKeys;
        UInt64[] castleKeys;
        UInt64 sideKey;
        PieceColour toMove;
        public PieceColour ToMove { get { return toMove; } private set { toMove = value; if (toMove == PieceColour.White) { thisSide = white; otherSide = black; } else { thisSide = black; otherSide = white; } } }
        public int HalfMovesSinceLastPawnMoveOrCapture { get; private set; }
        public int GameStartPlyNr { get; private set; }
        Stack<ChessMove> undoStack;
        public Collection<ChessMove> MoveHistory { get; private set; }
        public int CurrentGamePly { get { return undoStack.Count + GameStartPlyNr; } }
        public CastleFlags CastleFlags { get; private set; }
        public Square EpSquare { get; private set; }
        Dictionary<UInt64, int> pvTable;
        public event EventHandler PrincUpdated;
        ChessMove[,] killers;
        int[,] mvvLva;
        int[,] searchHistory;
        Random rng;
        bool stopSignalled;

        public ChessPosition()
        {
            initChessEval();
            Board = new SquareInfo[64];
            emptyBoard = new SquareInfo[64];
            Princ = new ChessMove[maxDepth, maxDepth];
            killers = new ChessMove[2, 1000];
            mvvLva = new int[8, 8];
            searchHistory = new int[16, 64];
            // Init the mvvLva array
            for (int v = 0; v <= 6; v++)
                for (int a = 0; a <= 6; a++)
                    mvvLva[v, a] = v * 100 +6 - a;
            // Init the killers array
            for (int i = 0; i < 1000; i++)
            {
                killers[0, i] = new ChessMove(new Square(99, 99), new Square(99, 99));
                killers[1, i] = new ChessMove(new Square(99, 99), new Square(99, 99));
            }
            // Init the emptyBoard and Board arrays
            for (int i = 0; i <= 63; i++)
            {
                emptyBoard[i] = new SquareInfo(Pieces.None, new Square(i));
                Board[i] = emptyBoard[i];
            }
            // Init the hashKeys
            Random rng = new Random();
            pieceKeys = new UInt64[16, 64];
            for (int p = 0; p <= 15; p++)
                for (int sq = 0; sq <= 63; sq++)
                    pieceKeys[p, sq] = (UInt64)rng.Next() + ((UInt64)rng.Next() << 15) + ((UInt64)rng.Next() << 30) + ((UInt64)rng.Next() << 45) + ((UInt64)rng.Next() << 60);
            castleKeys = new UInt64[16];
            for (int i = 0; i <= 15; i++)
                castleKeys[i] = (UInt64)rng.Next() + ((UInt64)rng.Next() << 15) + ((UInt64)rng.Next() << 30) + ((UInt64)rng.Next() << 45) + ((UInt64)rng.Next() << 60);
            sideKey = (UInt64)rng.Next() + ((UInt64)rng.Next() << 15) + ((UInt64)rng.Next() << 30) + ((UInt64)rng.Next() << 45) + ((UInt64)rng.Next() << 60);
            // Init the pvTable
            pvTable = new Dictionary<ulong, int>();
            EpSquare = new Square(0, 0);
            white = new ToMoveData(PieceColour.White, new Piece(PieceType.Pawn, PieceColour.White), new Piece(PieceType.Knight, PieceColour.White),
                new Piece(PieceType.Bishop, PieceColour.White), new Piece(PieceType.Rook, PieceColour.White), new Piece(PieceType.Queen, PieceColour.White),
                new Piece(PieceType.King, PieceColour.White), 1, 7, 1, CastleFlags.Wks, CastleFlags.Wqs, new Square("e1"), new Square("h1"), new Square("a1"));
            black = new ToMoveData(PieceColour.Black, new Piece(PieceType.Pawn, PieceColour.Black), new Piece(PieceType.Knight, PieceColour.Black),
                new Piece(PieceType.Bishop, PieceColour.Black), new Piece(PieceType.Rook, PieceColour.Black), new Piece(PieceType.Queen, PieceColour.Black),
                new Piece(PieceType.King, PieceColour.Black), 6, 0, -1, CastleFlags.Bks, CastleFlags.Bqs, new Square("e8"), new Square("h8"), new Square("a8"));
            PossibleMoves = new Collection<ChessMove>();
            MoveHistory = new Collection<ChessMove>();
            undoStack = new Stack<ChessMove>();
            NewGame();
        }

        public void NewGame()
        {
            Event = "Computer game";
            Site = "Computer";
            Date = DateTime.Now.ToShortDateString();
            Round = "?";
            White = "?";
            Black = "?";
            Result = "?";
            Initialise(StartFEN);
        }

        public bool Initialise(string strFen)
        {
            // returns false if position is illegal
            // No validation checks yet
            for (int i = 0; i <= 63; i++)
                Board[i] = emptyBoard[i];
            MoveHistory.Clear();
            undoStack.Clear();
            Square sq = new Square("a8");
            int p = 0;  // Position within the FEN string
            char c;
            while (p < strFen.Length && (c = strFen[p++]) != ' ')
            {
                if (c >= '1' && c <= '8')
                {
                    sq.File += c - '0';
                }
                int i = pieceStr.IndexOf(Char.ToUpper(c));
                if (i >= 1)
                {
                    // Replace the empty SquareInfos with new ones for the pieces
                    // The original empty SquareInfos are in emptyBoard
                    Board[sq] = new SquareInfo(new Piece((PieceType)i, c < 'a' ? PieceColour.White : PieceColour.Black), sq);
                    sq.File++;
                }
                if (c == '/')
                {
                    sq.Rank--;
                    sq.File = 0;
                }
            }
            c = strFen[p++];
            if (c == 'b')
            {
                ToMove = PieceColour.Black;
            }
            else
            {
                ToMove = PieceColour.White;
            }
            CastleFlags = 0;
            p++;
            while (p < strFen.Length && (c = strFen[p++]) != ' ')
            {
                switch (c)
                {
                    case 'K': CastleFlags |= CastleFlags.Wks; break;
                    case 'Q': CastleFlags |= CastleFlags.Wqs; break;
                    case 'k': CastleFlags |= CastleFlags.Bks; break;
                    case 'q': CastleFlags |= CastleFlags.Bqs; break;
                }
            }
            c = strFen[p++];
            if (c == '-')
                EpSquare = new Square(0, 0);
            else
            {
                char d = strFen[p++];
                EpSquare = new Square(c - 'a', d - '1');
            }
            StringBuilder n = new StringBuilder();
            p++;
            while (p < strFen.Length && (c = strFen[p++]) != ' ')
                n.Append(c);
            HalfMovesSinceLastPawnMoveOrCapture = int.Parse(n.ToString());
            n.Clear();
            while (p < strFen.Length && (c = strFen[p++]) != ' ')
                n.Append(c);
            GameStartPlyNr = (int.Parse(n.ToString()) - 1) * 2 + (ToMove == PieceColour.Black ? 1 : 0);
            initializePieceLists();
            generatePossibleMoves(PossibleMoves);
            removeIllegalMoves(PossibleMoves);
            return true;
        }

        public void MakeMovesFromString(string movesString)
        {
            StringReader reader = new StringReader(movesString);
            IEnumerable<ChessMove> moves;
            string word = readWord(reader);
            while (word != "")
            {
                moves = PossibleMoves;
                string w = "";
                string stripped = "";           // Strip off leading capital piece letter, and all x, -, +, =, # etc
                foreach (char c in word)
                {
                    if (c >= 'A' && c <= 'Z')
                    {
                        stripped += c;
                        w += 'P';
                    }
                    if (c >= 'a' && c <= 'r')   // Last lower case piece letter is 'r' for rook
                    {
                        stripped += c;
                        w += 'a';
                    }
                    else if (c >= '1' && c <= '8')
                    {
                        stripped += c;
                        w += '1';
                    }
                }
                // stripped = the move word with all non-letters and non-numbers removed
                // w = stripped with 'P' = uppercase letter, 'a' = lowercase letter, '1' = digit to make it so we can deal with each case
                if (word.StartsWith("O-O-O") || word.StartsWith("OOO") || word.StartsWith("o-o-o") || word.StartsWith("ooo") || word.StartsWith("0-0-0") || word.StartsWith("000"))
                    moves = moves.Where(o => o.Special == SpecialMoveType.CastleQS);
                else if (word.StartsWith("O-O") || word.StartsWith("OO") || word.StartsWith("o-o") || word.StartsWith("oo") || word.StartsWith("0-0") || word.StartsWith("00"))
                    moves = moves.Where(o => o.Special == SpecialMoveType.CastleKS);
                else
                {
                    if (w[0] == 'P')
                        // Filter PossibleMoves to the piece being moved represented by the first letter in the move word
                        // You get the idea
                        moves = moves.Where(o => Board[o.FromSq].Piece.PieceType == pieceTypeFromChar(word[0]));
                    else if (w != "a1a1")
                        // Filter moves not starting with a capital piece letter and of the format 'a1a1', to pawn moves.
                        moves = moves.Where(o => Board[o.FromSq].Piece.PieceType == PieceType.Pawn);
                    if (w.EndsWith("P"))
                        // Filter to promoted piece
                        moves = moves.Where(o => o.PromotedTo.PieceType == pieceTypeFromChar(stripped[stripped.Length - 1]));
                    switch (w)
                    {
                        case "Pa1":
                            moves = moves.Where(o => o.ToSq == new Square(stripped.Substring(1, 2)));
                            break;
                        case "P1a1":
                            moves = moves.Where(o => o.FromSq.Rank == stripped[1] - '1' && o.ToSq == new Square(stripped.Substring(2, 2)));
                            break;
                        case "Paa1":
                            moves = moves.Where(o => o.FromSq.File == stripped[1] - 'a' && o.ToSq == new Square(stripped.Substring(2, 2)));
                            break;
                        case "Pa1a1":
                            moves = moves.Where(o => o.FromSq == new Square(stripped.Substring(1, 2)) && o.ToSq == new Square(stripped.Substring(3, 2)));
                            break;
                        case "Paa":
                            moves = moves.Where(o => o.FromSq.File == stripped[1] - 'a' && o.ToSq.File == stripped[2] - 'a');
                            break;
                        case "Pa1a":
                            moves = moves.Where(o => o.FromSq == new Square(stripped.Substring(1, 2)) && o.ToSq.File == stripped[3] - 'a');
                            break;

                        case "aP":
                            moves = moves.Where(o => o.FromSq.File == stripped[0] - 'a' && o.ToSq.File == stripped[0] - 'a');
                            break;
                        case "a1a1P":
                        case "a1a1":
                            moves = moves.Where(o => o.FromSq == new Square(stripped.Substring(0, 2)) && o.ToSq == new Square(stripped.Substring(2, 2)));
                            break;
                        case "a1P":
                        case "a1":
                            moves = moves.Where(o => o.ToSq == new Square(stripped.Substring(0, 2)));
                            break;
                        case "aaP":
                        case "aa":
                            moves = moves.Where(o => o.FromSq.File == stripped[0] - 'a' && o.ToSq.File == stripped[1] - 'a');
                            break;
                        case "aa1P":
                        case "aa1":
                            moves = moves.Where(o => o.FromSq.File == stripped[0] - 'a' && o.ToSq == new Square(stripped.Substring(1, 2)));
                            break;
                        case "a1aP":
                        case "a1a":
                            moves = moves.Where(o => o.FromSq == new Square(stripped.Substring(0, 2)) && o.ToSq.File == stripped[2] - 'a');
                            break;
                        default:
                            // Do this in case PossibleMoves contains only one move
                            moves = moves.Take(0);
                            break;
                    }
                    if (moves.Count() == 0)
                    {
                        // Filter again trying with pieces for the lower case letters in some cases
                        switch (w)
                        {
                            case "aa1":
                                // eg. bc5, re2
                                moves = PossibleMoves.Where(o => Board[o.FromSq].Piece.PieceType == pieceTypeFromChar(stripped[0]) && o.ToSq == new Square(stripped.Substring(1, 2)));
                                break;
                            case "aa":
                            case "PP":
                            case "aP":
                            case "Pa":
                                // eg. bxn, QxR, qxB
                                moves = PossibleMoves.Where(o => Board[o.FromSq].Piece.PieceType == pieceTypeFromChar(stripped[0])
                                    && Board[o.ToSq].Piece.PieceType == pieceTypeFromChar(stripped[1]));
                                if (moves.Count() == 0)
                                    // eg. exn
                                    moves = PossibleMoves.Where(o => o.FromSq.File == stripped[0] - 'a' && Board[o.ToSq].Piece.PieceType == pieceTypeFromChar(stripped[1]));
                                break;
                            case "a1a":
                                // eg. c3xr
                                moves = PossibleMoves.Where(o => o.FromSq == new Square(stripped.Substring(0, 2)) && Board[o.ToSq].Piece.PieceType == pieceTypeFromChar(stripped[2]));
                                if (moves.Count() == 0)
                                    // eg. d8q
                                    moves = PossibleMoves.Where(o => o.ToSq == new Square(stripped.Substring(0, 2)) && o.PromotedTo.PieceType == pieceTypeFromChar(stripped[2]));
                                break;
                            case "aa1a":
                                // eg. cxd8=n
                                moves = PossibleMoves.Where(o => o.FromSq.File == stripped[0] - 'a' && o.ToSq == new Square(stripped.Substring(1, 2))
                                    && o.PromotedTo.PieceType == pieceTypeFromChar(stripped[3]));
                                break;
                            case "a1a1a":
                                // eg. b7a8q
                                moves = PossibleMoves.Where(o => o.FromSq == new Square(stripped.Substring(0, 2)) && o.ToSq == new Square(stripped.Substring(2, 2))
                                    && o.PromotedTo.PieceType == pieceTypeFromChar(stripped[4]));
                                break;
                            default:
                                // moves will be empty here
                                break;
                        }
                    }
                }
                if (moves.Count() > 1)
                    // If promotion piece not specified, filter to Queen
                    if (moves.All(o => o.Special == SpecialMoveType.Promotion))
                        moves = moves.Where(o => o.PromotedTo.PieceType == PieceType.Queen);
                List<ChessMove> filteredMoves = moves.ToList();
                if (filteredMoves.Count != 1)
                    // Attempts to filter the move word to a single unambiguous possibility failed so break out
                    break;
                MakeMove(filteredMoves[0]);
                word = readWord(reader);
            }
        }

        string readWord(TextReader reader)
        {
            string word = "";
            // Skip non-letters
            int c = reader.Read();
            while (c != -1 && !(c >= 'A' && c <= 'Z' || c >= 'a' && c <= 'z'))
            {
                c = reader.Read();
            }
            while (c != -1 && !char.IsWhiteSpace((char)c))
            {
                word += (char)c;
                c = reader.Read();
            }
            return word;
        }

        PieceType pieceTypeFromChar(char c)
        {
            int a = pieceStr.IndexOf(Char.ToUpper(c));
            return a != -1 ? (PieceType)a : PieceType.None;
        }

        void initializePieceLists()
        {
            // Create a linked list within the SquareInfo on each square of the board linking the pieces for faster move generation
            // Also generates the unique hashKey for the position
            SquareInfo prevSqInfo;
            for (int i = 0; i < 64; i++)
            {
                if (Board[i].Piece.PieceType == PieceType.King)
                    if (Board[i].Piece.PieceColour == PieceColour.White)
                        white.KingSqInfo = Board[i];                        // white.KingSqInfo Initialized here
                    else
                        black.KingSqInfo = Board[i];                        // black.KingSqInfo Initialized here
            }
            prevSqInfo = white.KingSqInfo;                                  // The SquareInfo of the white king
            HashKey ^= pieceKeys[prevSqInfo.Piece.ToInt(), prevSqInfo.Location];
            for (int i = 0; i < 64; i++)
            {
                if (Board[i].Piece.PieceColour == PieceColour.White && Board[i].Piece.PieceType != PieceType.King)
                {
                    // Square contains a piece, the piece is white and not the king
                    Board[i].Prev = prevSqInfo;
                    prevSqInfo.Next = Board[i];
                    prevSqInfo = Board[i];
                    HashKey ^= pieceKeys[prevSqInfo.Piece.ToInt(), prevSqInfo.Location];
                }
            }
            prevSqInfo.Next = white.KingSqInfo;                                      // Last square points back to the king
            white.KingSqInfo.Prev = prevSqInfo;                                      // Close the linked list

            prevSqInfo = black.KingSqInfo;
            HashKey ^= pieceKeys[prevSqInfo.Piece.ToInt(), prevSqInfo.Location];
            for (int i = 0; i < 64; i++)
            {
                if (Board[i].Piece.PieceColour == PieceColour.Black && Board[i].Piece.PieceType != PieceType.King)
                {
                    // Square contains a piece, the piece is black and not the king
                    Board[i].Prev = prevSqInfo;
                    prevSqInfo.Next = Board[i];
                    prevSqInfo = Board[i];
                    HashKey ^= pieceKeys[prevSqInfo.Piece.ToInt(), prevSqInfo.Location];
                }
            }
            prevSqInfo.Next = black.KingSqInfo;                                      // Last square points back to the king
            black.KingSqInfo.Prev = prevSqInfo;                                      // Close the linked list
            HashKey ^= castleKeys[(int)CastleFlags] ^ (toMove == PieceColour.Black ? sideKey : 0);
        }

        void generateCaptureMoves(Collection<ChessMove> possibleMoves)
        {
            // Very inefficient atm, generating all moves then removing the non-captures
            generatePossibleMoves(possibleMoves);
            for (int i = 0; i < possibleMoves.Count; i++)
            {
                if (Board[possibleMoves[i].ToSq].Piece == Pieces.None)
                    possibleMoves.RemoveAt(i--);
            }
        }

        void generatePossibleMoves(Collection<ChessMove> possibleMoves)
        {
            possibleMoves.Clear();
            tmpMoveList = possibleMoves;
            SquareInfo sqInfo = thisSide.KingSqInfo;
            Square location = sqInfo.Location;
            addKingMove(location, 1, 1);
            addKingMove(location, 1, -1);
            addKingMove(location, 1, 0);
            addKingMove(location, 0, 1);
            addKingMove(location, 0, -1);
            addKingMove(location, -1, -1);
            addKingMove(location, -1, 0);
            addKingMove(location, -1, 1);
            sqInfo = sqInfo.Next;
            location = sqInfo.Location;
            while (sqInfo != thisSide.KingSqInfo)
            {
                switch (sqInfo.Piece.PieceType)
                {
                    case PieceType.Pawn:
                        {
                            addPawnMove(location);
                            addPawnCapture(location, 1);
                            addPawnCapture(location, -1);
                            break;
                        }
                    case PieceType.Knight:
                        {
                            addMove(location, 1, 2);
                            addMove(location, 1, -2);
                            addMove(location, 2, 1);
                            addMove(location, 2, -1);
                            addMove(location, -1, 2);
                            addMove(location, -1, -2);
                            addMove(location, -2, 1);
                            addMove(location, -2, -1);
                            break;
                        }
                    case PieceType.Bishop:
                        {
                            addSlidingMoves(location, 1, 1);
                            addSlidingMoves(location, 1, -1);
                            addSlidingMoves(location, -1, 1);
                            addSlidingMoves(location, -1, -1);
                            break;
                        }
                    case PieceType.Rook:
                        {
                            addSlidingMoves(location, 1, 0);
                            addSlidingMoves(location, 0, 1);
                            addSlidingMoves(location, -1, 0);
                            addSlidingMoves(location, 0, -1);
                            break;
                        }
                    case PieceType.Queen:
                        {
                            addSlidingMoves(location, 1, 1);
                            addSlidingMoves(location, 1, -1);
                            addSlidingMoves(location, -1, 1);
                            addSlidingMoves(location, -1, -1);
                            addSlidingMoves(location, 1, 0);
                            addSlidingMoves(location, 0, 1);
                            addSlidingMoves(location, -1, 0);
                            addSlidingMoves(location, 0, -1);
                            break;
                        }
                }
                sqInfo = sqInfo.Next;
                location = sqInfo.Location;
            } // End while (sqInfo != thisSide.KingSqInfo);
            if (EpSquare != 0)
            {
                addEnPassantMove(-1);
                addEnPassantMove(1);
            }
            if ((CastleFlags & thisSide.CanCastleKS) != 0
                && Board[thisSide.KingKCastleSq].Piece.PieceType == PieceType.None && Board[thisSide.RookKCastleSq].Piece.PieceType == PieceType.None
                && !isSquareAttacked(thisSide.KingStartSq, otherSide)          // Can't castle out of check,
                && !isSquareAttacked(thisSide.RookKCastleSq, otherSide))       // or through check. No need to check for castling into check here.
            {
                addQuietMove(new ChessMove(thisSide.KingStartSq, thisSide.KingKCastleSq, SpecialMoveType.CastleKS));
            }
            if ((CastleFlags & thisSide.CanCastleQS) != 0
                && Board[thisSide.KingQCastleSq].Piece.PieceType == PieceType.None && Board[thisSide.RookQCastleSq].Piece.PieceType == PieceType.None
                && Board[thisSide.KingStartSq - 3].Piece.PieceType == PieceType.None
                && !isSquareAttacked(thisSide.KingStartSq, otherSide)          // Can't castle out of check,
                && !isSquareAttacked(thisSide.RookQCastleSq, otherSide))       // or through check.
            {
                addQuietMove(new ChessMove(thisSide.KingStartSq, thisSide.KingQCastleSq, SpecialMoveType.CastleQS));
            }
        }

        void addQuietMove(ChessMove move)
        {
            move.Piece = Board[move.FromSq].Piece;
            if (killers[0, CurrentGamePly].Piece == move.Piece && killers[0, CurrentGamePly].FromSq == move.FromSq && killers[0, CurrentGamePly].ToSq == move.ToSq)
                move.Score = killer0Score;
            else
                if (killers[1, CurrentGamePly].Piece == move.Piece && killers[1, CurrentGamePly].FromSq == move.FromSq && killers[1, CurrentGamePly].ToSq == move.ToSq)
                    move.Score = killer1Score;
                else
                    move.Score += searchHistory[move.Piece.ToInt(), move.ToSq];
            tmpMoveList.Add(move);
        }

        void addCaptureMove(ChessMove move)
        {
            move.Piece = Board[move.FromSq].Piece;
            // Adjust score according to most valuable victim least valuable attacker
            move.Score = captureScore + mvvLva[(int)Board[move.ToSq].Piece.PieceType, (int)move.Piece.PieceType];
            tmpMoveList.Add(move);
        }

        void addPawnMove(Square from)
        {
            Square to = new Square(from.File, from.Rank + thisSide.PawnYDir);
            if (Board[to].Piece.PieceType == PieceType.None)
            {
                if (to.Rank == thisSide.PawnPromRank)
                {
                    addQuietMove(new ChessMove(from, to, thisSide.Queen));
                    addQuietMove(new ChessMove(from, to, thisSide.Rook));
                    addQuietMove(new ChessMove(from, to, thisSide.Bishop));
                    addQuietMove(new ChessMove(from, to, thisSide.Knight));
                }
                else
                {
                    addQuietMove(new ChessMove(from, to, SpecialMoveType.PawnMove));
                    if (from.Rank == thisSide.PawnStartRank)                   // Check for double pawn move
                    {
                        to.Rank += thisSide.PawnYDir;
                        if (Board[to].Piece.PieceType == PieceType.None)
                        {
                            addQuietMove(new ChessMove(from, to, SpecialMoveType.DoublePawnMove));
                        }
                    }
                }
            }
        }

        void addPawnCapture(Square from, int xDir)
        {
            Square to = new Square(from.File + xDir, from.Rank + thisSide.PawnYDir);
            if (to.File >= 0 && to.File <= 7 && Board[to].Piece.PieceColour == otherSide.Colour)
            {
                if (to.Rank == thisSide.PawnPromRank)
                {
                    addCaptureMove(new ChessMove(from, to, thisSide.Queen));
                    addCaptureMove(new ChessMove(from, to, thisSide.Rook));
                    addCaptureMove(new ChessMove(from, to, thisSide.Bishop));
                    addCaptureMove(new ChessMove(from, to, thisSide.Knight));
                }
                else
                    addCaptureMove(new ChessMove(from, to));
            }
        }

        void addEnPassantMove(int xDir)
        {
            // xDir = X direction of the capture to the EpSq
            Square from = new Square(EpSquare.File - xDir, EpSquare.Rank - thisSide.PawnYDir);
            if (from.File >= 0 && from.File <= 7 && Board[from].Piece == thisSide.Pawn)
                addCaptureMove(new ChessMove(from, EpSquare, SpecialMoveType.EnPassant));
        }

        void addMove(Square from, int xDir, int yDir)
        {
            PieceColour captureColour = PieceColour.None;
            Square to = new Square(from.File + xDir, from.Rank + yDir);
            if (to.File >= 0 && to.File <= 7 && to.Rank >= 0 && to.Rank <= 7 && (captureColour = Board[to].Piece.PieceColour) != toMove)
            {
                // if to is on the board and doesn't contain a piece of the side to move
                if (captureColour != otherSide.Colour)
                    addQuietMove(new ChessMove(from, to));
                else
                    addCaptureMove(new ChessMove(from, to));
            }
        }

        void addKingMove(Square from, int xDir, int yDir)
        {
            PieceColour captureColour = PieceColour.None;
            Square to = new Square(from.File + xDir, from.Rank + yDir);
            if (to.File >= 0 && to.File <= 7 && to.Rank >= 0 && to.Rank <= 7 && (captureColour = Board[to].Piece.PieceColour) != toMove)
            {
                // if to is on the board and doesn't contain a piece of the side to move
                if (captureColour != otherSide.Colour)
                    addQuietMove(new ChessMove(from, to));
                else
                    addCaptureMove(new ChessMove(from, to));
            }
        }

        void addSlidingMoves(Square from, int xDir, int yDir)
        {
            PieceColour captureColour = PieceColour.None;
            Square to = new Square(from.File + xDir, from.Rank + yDir);
            while (to.File >= 0 && to.File <= 7 && to.Rank >= 0 && to.Rank <= 7 && (captureColour = Board[to].Piece.PieceColour) == PieceColour.None)
            {
                addQuietMove(new ChessMove(from, to));
                to.File += xDir; to.Rank += yDir;              // Value type so no need to new another one
            }
            if (captureColour == otherSide.Colour)
            {
                // If Capture
                addCaptureMove(new ChessMove(from, to));
            }
        }

        void removeIllegalMoves(Collection<ChessMove> possibleMoves)
        {
            for (int i = 0; i < possibleMoves.Count; i++)
            {
                makeMove(possibleMoves[i]);
                if (isInCheck(otherSide, thisSide))
                    possibleMoves.RemoveAt(i--);
                undoMove();
            }
        }

        void fillMoveText(ChessMove move)
        {
            move.Text = "";
            if (move.Special == SpecialMoveType.CastleKS)
            {
                move.Text = "O-O";
                return;
            }
            if (move.Special == SpecialMoveType.CastleQS)
            {
                move.Text = "O-O-O";
                return;
            }
            PieceType piece = Board[move.FromSq].Piece.PieceType;
            bool capture = (Board[move.ToSq].Piece.PieceType != PieceType.None || move.Special == SpecialMoveType.EnPassant);
            if (piece == PieceType.Pawn)
            {
                if (capture)
                    move.Text += ((char)(move.FromSq.File + 'a')).ToString();
            }
            else
            {
                // Can more than one of the move's PieceType move to the same square?
                int sameTo = 0, sameFile = 0, sameRank = 0;
                foreach (ChessMove m in PossibleMoves)
                {
                    if (Board[m.FromSq].Piece.PieceType == piece)
                        if (m.FromSq != move.FromSq && m.ToSq == move.ToSq)
                        {
                            sameTo++;
                            if (m.FromSq.File == move.FromSq.File)
                                sameFile++;     // Another possible move with the same PieceType and From file moving to the same square
                            if (m.FromSq.Rank == move.FromSq.Rank)
                                sameRank++;     // Likewise with rank
                        }
                }
                move.Text = pieceStr[(int)piece].ToString();
                if (sameTo > 0)
                {
                    if (sameFile == 0 && sameRank == 0 || sameRank > 0)
                        move.Text += ((char)(move.FromSq.File + 'a')).ToString();           // Needs rank specification to disambiguify
                    if (sameFile > 0)
                        move.Text += ((char)(move.FromSq.Rank + '1')).ToString();           // Needs file specificatino to disambiguify
                }
            }
            if (capture)
                move.Text += "x";                                                       // Capture
            move.Text += move.ToSq.ToString();
            if (move.Special == SpecialMoveType.Promotion)
                move.Text += "=" + pieceStr[(int)move.PromotedTo.PieceType].ToString(); // Promotion
        }

        public void SavePGN(string filename)
        {
            Pgn pgnGame = new Pgn(this);
            using (StreamWriter stream = new StreamWriter(filename))
            {
                stream.Write(pgnGame);
            }
        }

        bool isInCheck(ToMoveData thisSide, ToMoveData otherSide)
        {
            Square sq = thisSide.KingSqInfo.Location;
            return isSquareAttacked(sq, otherSide);
        }

        bool isSquareAttacked(Square sq, ToMoveData enemySide)
        {
            int file = sq.File;
            int rank = sq.Rank;
            Piece enemyKnight = enemySide.Knight;
            if (checkForEnemyKnightAt(file + 1, rank + 2, enemyKnight) || checkForEnemyKnightAt(file + 2, rank + 1, enemyKnight) || checkForEnemyKnightAt(file + 2, rank - 1, enemyKnight) || checkForEnemyKnightAt(file + 1, rank - 2, enemyKnight)
                || checkForEnemyKnightAt(file - 1, rank - 2, enemyKnight) || checkForEnemyKnightAt(file - 2, rank - 1, enemyKnight) || checkForEnemyKnightAt(file - 2, rank + 1, enemyKnight) || checkForEnemyKnightAt(file - 1, rank + 2, enemyKnight))
                return true;
            Piece enemyPawn = enemySide.Pawn;
            if (checkForEnemyPawnAt(file - 1, rank - enemySide.PawnYDir, enemyPawn) || checkForEnemyPawnAt(file + 1, rank - enemySide.PawnYDir, enemyPawn))
                return true;
            Piece enemyBishop = enemySide.Bishop;
            Piece enemyRook = enemySide.Rook;
            Piece enemyQueen = enemySide.Queen;
            if ((checkForSlidingAttacksFrom(file, rank, 1, 1, enemyBishop, enemyQueen)) || (checkForSlidingAttacksFrom(file, rank, 1, -1, enemyBishop, enemyQueen))
                || (checkForSlidingAttacksFrom(file, rank, -1, -1, enemyBishop, enemyQueen)) || (checkForSlidingAttacksFrom(file, rank, -1, 1, enemyBishop, enemyQueen))
                || (checkForSlidingAttacksFrom(file, rank, 0, 1, enemyRook, enemyQueen)) || (checkForSlidingAttacksFrom(file, rank, 1, 0, enemyRook, enemyQueen))
                || (checkForSlidingAttacksFrom(file, rank, 0, -1, enemyRook, enemyQueen)) || (checkForSlidingAttacksFrom(file, rank, -1, 0, enemyRook, enemyQueen)))
                return true;
            Square enemyKingSq = enemySide.KingSqInfo.Location;
            int fileDiff = file - enemyKingSq.File;
            int rankDiff = rank - enemyKingSq.Rank;
            if (fileDiff >= -1 && fileDiff <= 1 && rankDiff >= -1 && rankDiff <= 1)
                return true;
            return false;
        }

        bool checkForEnemyKnightAt(int file, int rank, Piece enemyKnight)
        {
            return (file >= 0 && file <= 7 && rank >= 0 && rank <= 7 && Board[file + rank * 8].Piece == enemyKnight);
        }

        bool checkForEnemyPawnAt(int file, int rank, Piece enemyPawn)
        {
            return (file >= 0 && file <= 7 && rank >= 0 && rank <= 7 && Board[file + rank * 8].Piece == enemyPawn);
        }

        bool checkForSlidingAttacksFrom(int file, int rank, int xDir, int yDir, Piece piece1, Piece piece2)
        {
            file += xDir;
            rank += yDir;
            while (file >= 0 && file <= 7 && rank >= 0 && rank <= 7)
            {
                Piece p = Board[file + rank * 8].Piece;
                if (p == piece1 || p == piece2) return true;
                if (p != Pieces.None) return false;
                file += xDir;
                rank += yDir;
            }
            return false;
        }

        public bool GotoGamePly(int plyNr)
        {
            if (plyNr < GameStartPlyNr)
                plyNr = GameStartPlyNr;
            if (plyNr > GameStartPlyNr + MoveHistory.Count)
                plyNr = GameStartPlyNr + MoveHistory.Count;
            if (plyNr != CurrentGamePly)
            {
                while (CurrentGamePly < plyNr)
                    makeMove(MoveHistory[undoStack.Count]);
                while (CurrentGamePly > plyNr)
                    undoMove();
                generatePossibleMoves(PossibleMoves);
                removeIllegalMoves(PossibleMoves);
                return true;
            }
            else return false;      // Position not adjusted
        }

        public bool MakeMove(ChessMove move)
        {
            // Make a move at Ply in the MoveHistory deleting all moves following
            // Look for the move with matching from and to squares in PossibleMoves and make that move since it has SpecialMoveType set up
            ChessMove foundMove = PossibleMoves.FirstOrDefault(o => o.FromSq == move.FromSq && o.ToSq == move.ToSq);
            if (foundMove == null) return false;                   // No matching move
            if (foundMove.Special == SpecialMoveType.Promotion)
                // If the match is a promotion, filter to the promoted piece if specified else a queen
                foundMove = PossibleMoves.FirstOrDefault(o => o.FromSq == move.FromSq && o.ToSq == move.ToSq &&
                    o.PromotedTo.PieceType == (move.PromotedTo.PieceType != PieceType.None ? move.PromotedTo.PieceType : PieceType.Queen));
            while (MoveHistory.Count > undoStack.Count)
                MoveHistory.RemoveAt(MoveHistory.Count - 1);
            foundMove.GamePlyNr = CurrentGamePly;
            fillMoveText(foundMove);
            makeMove(foundMove);
            MoveHistory.Add(foundMove);
            generatePossibleMoves(PossibleMoves);
            removeIllegalMoves(PossibleMoves);
            if (isInCheck(thisSide, otherSide))
                if (PossibleMoves.Count > 0)
                    foundMove.Text += "+";
                else
                    foundMove.Text += "#";
            return true;
        }

        void makeMove(ChessMove move)
        {
            Square from = move.FromSq;
            Square to = move.ToSq;
            SquareInfo CapturedSqInfo = Board[to];
            Board[to] = Board[from];                    // Move the piece
            Board[from] = emptyBoard[from];             // Replace from SquareInfo with the empty SquareInfo from emptyBoard
            Board[to].Location = to;                    // Update SqInfo location
            move.OldHashKey = HashKey;
            move.OldEpSq = EpSquare;
            EpSquare = new Square(0, 0);
            move.OldCastleFlags = CastleFlags;
            HashKey ^= pieceKeys[move.Piece.ToInt(), from] ^ pieceKeys[move.Piece.ToInt(), to];
            bool pawnMoveOrCapture = false;
            switch (move.Special)
            {
                case SpecialMoveType.PawnMove:
                    pawnMoveOrCapture = true;
                    break;
                case SpecialMoveType.DoublePawnMove:
                    EpSquare = new Square(from.File, from.Rank + thisSide.PawnYDir);
                    pawnMoveOrCapture = true;
                    break;
                case SpecialMoveType.Promotion:
                    Board[to].Piece = move.PromotedTo;
                    pawnMoveOrCapture = true;
                    HashKey ^= pieceKeys[move.Piece.ToInt(), to] ^ pieceKeys[move.PromotedTo.ToInt(), to];
                    break;
                case SpecialMoveType.EnPassant:
                    // After the piece has been moved make to = the square of the captured ep pawn
                    to.Rank -= thisSide.PawnYDir;
                    CapturedSqInfo = Board[to];                 // CapturedSqInfo is the enemy pawn not the square moved into
                    Board[to] = emptyBoard[to];
                    break;
                case SpecialMoveType.CastleKS:
                    // Move the rook
                    to = thisSide.RookKCastleSq;
                    from = thisSide.KingRookSq;
                    Board[to] = Board[from];                    // Move the piece
                    Board[from] = emptyBoard[from];             // Replace from SquareInfo with the empty SquareInfo from emptyBoard
                    Board[to].Location = to;                    // Update SqInfo location
                    HashKey ^= pieceKeys[Board[to].Piece.ToInt(), from] ^ pieceKeys[Board[to].Piece.ToInt(), to];
                    break;
                case SpecialMoveType.CastleQS:
                    to = thisSide.RookQCastleSq;
                    from = thisSide.QueenRookSq;
                    Board[to] = Board[from];                    // Move the piece
                    Board[from] = emptyBoard[from];             // Replace from SquareInfo with the empty SquareInfo from emptyBoard
                    Board[to].Location = to;                    // Update SqInfo location
                    HashKey ^= pieceKeys[Board[to].Piece.ToInt(), from] ^ pieceKeys[Board[to].Piece.ToInt(), to];
                    break;
            }
            if (CapturedSqInfo.Piece.PieceColour != PieceColour.None)
            {
                CapturedSqInfo.Prev.Next = CapturedSqInfo.Next;                      // Delink the captured piece
                CapturedSqInfo.Next.Prev = CapturedSqInfo.Prev;
                pawnMoveOrCapture = true;
                // Clear enemy castling rights when capturing one of their rooks on its starting square
                if (to == otherSide.KingRookSq)
                    CastleFlags = CastleFlags & ~(otherSide.CanCastleKS);
                if (to == otherSide.QueenRookSq)
                    CastleFlags = CastleFlags & ~(otherSide.CanCastleQS);
                HashKey ^= pieceKeys[CapturedSqInfo.Piece.ToInt(), to];
            }
            // Clear castle flag if king or rooks have moved
            if (Board[thisSide.KingStartSq].Piece == thisSide.King)
            {
                if (Board[thisSide.KingRookSq].Piece != thisSide.Rook)
                    CastleFlags = CastleFlags & ~thisSide.CanCastleKS;
                if (Board[thisSide.QueenRookSq].Piece != thisSide.Rook)
                    CastleFlags = CastleFlags & ~thisSide.CanCastleQS;
            }
            else
                CastleFlags = CastleFlags & ~(thisSide.CanCastleKS | thisSide.CanCastleQS);
            ToMove = toMove == PieceColour.White ? PieceColour.Black : PieceColour.White;
            HashKey ^= castleKeys[(int)move.OldCastleFlags] ^ castleKeys[(int)CastleFlags] ^ sideKey;
            move.CapturedSqInfo = CapturedSqInfo;
            move.HalfMovesSinceLastPawnMoveOrCapture = HalfMovesSinceLastPawnMoveOrCapture;
            undoStack.Push(move);
            if (pawnMoveOrCapture)
                HalfMovesSinceLastPawnMoveOrCapture = 0;
            else
                HalfMovesSinceLastPawnMoveOrCapture++;
            return;
        }

        void undoMove()
        {
            ToMove = toMove == PieceColour.White ? PieceColour.Black : PieceColour.White;   // Make ToMove the side of the move being undone
            ChessMove lastMove = undoStack.Pop();
            HalfMovesSinceLastPawnMoveOrCapture = lastMove.HalfMovesSinceLastPawnMoveOrCapture;
            SquareInfo capturedSqInfo = lastMove.CapturedSqInfo;
            Square from = lastMove.FromSq;
            Square to = lastMove.ToSq;
            EpSquare = lastMove.OldEpSq;
            CastleFlags = lastMove.OldCastleFlags;
            HashKey = lastMove.OldHashKey;
            Board[from] = Board[to];
            Board[from].Location = from;
            switch (lastMove.Special)
            {
                case SpecialMoveType.Promotion:
                    Board[from].Piece = thisSide.Pawn;
                    break;
                case SpecialMoveType.EnPassant:
                    Board[to] = emptyBoard[to];
                    to.Rank -= thisSide.PawnYDir;
                    break;
                case SpecialMoveType.CastleKS:
                    // Move the rook back
                    from = thisSide.KingRookSq;
                    Square cto = thisSide.RookKCastleSq;
                    Board[from] = Board[cto];
                    Board[cto] = emptyBoard[cto];
                    Board[from].Location = from;
                    break;
                case SpecialMoveType.CastleQS:
                    from = thisSide.QueenRookSq;
                    cto = thisSide.RookQCastleSq;
                    Board[from] = Board[cto];
                    Board[cto] = emptyBoard[cto];
                    Board[from].Location = from;
                    break;
            }
            // Have to do this bit last because case SpecialMoveType.EnPassant changes the to square
            Board[to] = capturedSqInfo;
            if (capturedSqInfo.Piece != Pieces.None)
            {
                // Link in the restored capturedSqInfo
                capturedSqInfo.Prev.Next = capturedSqInfo;
                capturedSqInfo.Next.Prev = capturedSqInfo;
            }
        }

        public void FindBestMove(int maxDepth)
        {
            for (int i = 0; i < Princ.GetLength(0); i++)
                for (int j = 0; j < Princ.GetLength(1); j++)
                    Princ[i, j] = null;
            for (int p = 0; p <= Pieces.BlackKing.ToInt(); p++)
                for (int sq = 0; sq <= 63; sq++)
                    searchHistory[p, sq] = 0;
            NodesEvaluated = 0;
            stopSignalled = false;
            for (int i = 1; i <= maxDepth; i++)
            {
                negamax(i, -99999, 99999, 0);
                if (stopSignalled)
                    break;
            }
        }

        public void StopSearch()
        {
            stopSignalled = true;
        }

        void updatePrinc(ChessMove move, int ply)
        {
            Princ[ply, ply] = move;
            for (int i = ply + 1; i < maxDepth; i++)
                Princ[ply, i] = Princ[ply + 1, i];
            pvTable[HashKey] = (int)move.FromSq + ((int)move.ToSq << 8);
        }

        int quiescence(int alpha, int beta, int ply)
        {
            int eval = staticEval();
            if (ply >= maxDepth)
                return eval;
            if (eval >= beta)
                return beta;
            if (eval > alpha)
                alpha = eval;
            ChessMove move;
            Collection<ChessMove> possibleMoves = new Collection<ChessMove>();
            generateCaptureMoves(possibleMoves);
            for (int i = 0; i < possibleMoves.Count; i++)
            {
                // Find the next move in the list with the highest score
                int highestScore = 0;
                int bestIndex = i;
                for (int j = i; j < possibleMoves.Count; j++)
                {
                    if (possibleMoves[j].Score > highestScore)
                    {
                        highestScore = possibleMoves[j].Score;
                        bestIndex = j;
                    }
                    move = possibleMoves[i];
                    possibleMoves[i] = possibleMoves[bestIndex];
                    possibleMoves[bestIndex] = move;
                }
                move = possibleMoves[i];        // Now contains the next highest scoring move for move ordering
                if (Board[move.ToSq].Piece != Pieces.None)
                {
                    makeMove(move);
                    if (!isInCheck(otherSide, thisSide))
                    {
                        eval = -quiescence(-beta, -alpha, ply + 1);
                        undoMove();
                        if (stopSignalled)
                            return 0;
                        if (eval >= beta)
                        {
                            return eval;      // Cutoff
                        }
                        if (eval > alpha)
                        {
                            alpha = eval;
                            updatePrinc(move, ply);
                        }
                    }
                    else
                    {
                        undoMove();
                        if (stopSignalled)
                            return 0;
                    }
                }
            }
            // No need to check for mate in Quiessence
            return alpha;
        }

        int negamax(int depth, int alpha, int beta, int ply)
        {
            if (depth == 0)
                return quiescence(alpha, beta, ply);
            int eval;
            int nrLegalMoves = 0;
            ChessMove move;
            Collection<ChessMove> possibleMoves = new Collection<ChessMove>();
            generatePossibleMoves(possibleMoves);
            int m;
            if (pvTable.TryGetValue(HashKey, out m))
                foreach (ChessMove v in possibleMoves)
                    if (v.FromSq + (v.ToSq << 8) == m)
                        v.Score = pvScore;
            for (int i = 0; i < possibleMoves.Count;i++ )
            {
                // Find the next move in the list with the highest score
                int highestScore = 0;
                int bestIndex = i;
                for (int j = i; j < possibleMoves.Count; j++)
                {
                    if (possibleMoves[j].Score > highestScore)
                    {
                        highestScore = possibleMoves[j].Score;
                        bestIndex = j;
                    }
                }
                move = possibleMoves[bestIndex];        // Now contains the next highest scoring move for move ordering
                possibleMoves[bestIndex] = possibleMoves[i];
                possibleMoves[i] = move;
                makeMove(move);
                if (undoStack.Any(o => o.OldHashKey == HashKey))
                {
                    undoMove();
                    return 0;
                }
                if (!isInCheck(otherSide, thisSide))
                {
                    nrLegalMoves++;
                    eval = -negamax(depth - 1, -beta, -alpha, ply + 1);
                    undoMove();
                    if (stopSignalled)
                        return 0;
                    if (eval >= beta)
                    {
                        if (move.CapturedSqInfo.Piece == Pieces.None)
                        {
                            killers[1, CurrentGamePly] = killers[0, CurrentGamePly];
                            killers[0, CurrentGamePly] = move;
                        }
                        return eval;      // Cutoff
                    }
                    if (eval > alpha)
                    {
                        alpha = eval;
                        if (move.CapturedSqInfo.Piece == Pieces.None)
                        {
                            searchHistory[move.Piece.ToInt(), move.ToSq] += depth;
                        }
                        updatePrinc(move, ply);
                        if (ply == 0)
                        {
                            fillMoveText(move);
                            OnPrincUpdated();
                        }
                    }
                }
                else
                {
                    undoMove();
                    if (stopSignalled)
                        return 0;
                }
            }
            if (nrLegalMoves == 0)
            {
                if (isInCheck(thisSide, otherSide))
                    return -50000 + ply * 1000;
                else
                    return 0;
            }
            return alpha;
        }

        protected virtual void OnPrincUpdated()
        {
            if (PrincUpdated != null)
                PrincUpdated(this, EventArgs.Empty);
        }


        // ****************** //
        // Static Evaluation  //
        // ****************** //
        int[] pieceValues;
        int[] pawnTable = {   0 , 0 , 0 , 0 , 0 , 0 , 0 , 0 ,
                              10 ,10 , 0 ,-10 , -10 , 0 , 10 , 10 ,
                              5 , 0 , 0 , 5 , 5 , 0 , 0 , 5 ,
                              0 , 0 , 10 , 20 , 20 , 10 , 0 , 0 ,
                              5 , 5 , 5 , 10 , 10 , 5 , 5 , 5 ,
                              10 , 10 , 10 , 20 , 20 , 10 , 10 , 10 ,
                              20 , 20 , 20 , 30 , 30 , 20 , 20 , 20 ,
                              0 , 0 , 0 , 0 , 0 , 0 , 0 , 0};

        int[] knightTable = {   0 , -10 , 0 , 0 , 0 , 0 , -10 , 0 ,
                                0 , 0 , 0 , 5 , 5 , 0 , 0 , 0 ,
                                0 , 0 , 10 , 10 , 10 , 10 , 0 , 0 ,
                                0 , 0 , 10 , 20 , 20 , 10 , 5 , 0 ,
                                5 , 10 , 15 , 20 , 20 , 15 , 10 , 5 ,
                                5 , 10 , 10 , 20 , 20 , 10 , 10 , 5 ,
                                0 , 0 , 5 , 10 , 10 , 5 , 0 , 0 ,
                                0 , 0 , 0 , 0 , 0 , 0 , 0 , 0 };

        int[] bishopTable = {   0 , 0 , -10 , 0 , 0 , -10 , 0 , 0 ,
                                0 , 0 , 0 , 10 , 10 , 0 , 0 , 0 ,
                                0 , 0 , 10 , 15 , 15 , 10 , 0 , 0 ,
                                0 , 10 , 15 , 20 , 20 , 15 , 10 , 0 ,
                                0 , 10 , 15 , 20 , 20 , 15 , 10 , 0 ,
                                0 , 0 , 10 , 15 , 15 , 10 , 0 , 0 ,
                                0 , 0 , 0 , 10 , 10 , 0 , 0 , 0 ,
                                0 , 0 , 0 , 0 , 0 , 0 , 0 , 0 };

        int[] rookTable = {   0 , 0 , 5 , 10 , 10 , 5 , 0 , 0 ,
                              0 , 0 , 5 , 10 , 10 , 5 , 0 , 0 ,
                              0 , 0 , 5 , 10 , 10 , 5 , 0 , 0 ,
                              0 , 0 , 5 , 10 , 10 , 5 , 0 , 0 ,
                              0 , 0 , 5 , 10 , 10 , 5 , 0 , 0 ,
                              0 , 0 , 5 , 10 , 10 , 5 , 0 , 0 ,
                              25 , 25 , 25 , 25 , 25 , 25 , 25 , 25 ,
                              0 , 0 , 5 , 10 , 10 , 5 , 0 , 0 };

        void initChessEval()
        {
            pieceValues = new int[7];
            pieceValues[(int)PieceType.Pawn] = 100;
            pieceValues[(int)PieceType.Knight] = 300;
            pieceValues[(int)PieceType.Bishop] = 330;
            pieceValues[(int)PieceType.Rook] = 500;
            pieceValues[(int)PieceType.Queen] = 900;
            pieceValues[(int)PieceType.King] = 10000;
        }

        int staticEval()
        {
            NodesEvaluated++;
            int totalEvaluation = 0;
            for (ToMoveData side = thisSide; side != null; side = (side == thisSide ? otherSide : null))
            {
                int value = 0;
                SquareInfo sq = side.KingSqInfo;
                Piece piece;
                do
                {
                    piece = sq.Piece;
                    value += pieceValues[(int)piece.PieceType];
                    switch (piece.PieceType)
                    {
                        case PieceType.Pawn:
                            {
                                value += pawnTable[side.Colour == PieceColour.Black ? 63 - sq.Location : sq.Location];
                                break;
                            }
                        case PieceType.Knight:
                            {
                                value += knightTable[side.Colour == PieceColour.Black ? 63 - sq.Location : sq.Location];
                                break;
                            }
                        case PieceType.Bishop:
                            {
                                value += bishopTable[side.Colour == PieceColour.Black ? 63 - sq.Location : sq.Location];
                                break;
                            }
                        case PieceType.Rook:
                            {
                                value += rookTable[side.Colour == PieceColour.Black ? 63 - sq.Location : sq.Location];
                                break;
                            }
                        case PieceType.Queen:
                            {
                                value += rookTable[side.Colour == PieceColour.Black ? 63 - sq.Location : sq.Location];
                                break;
                            }
                    }
                    sq = sq.Next;
                } while (sq != side.KingSqInfo);
                if (side == thisSide)
                    totalEvaluation += value;
                else
                    totalEvaluation -= value;
            }
            return totalEvaluation;
        }
    }
}