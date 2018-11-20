using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;
using ChessLib;
using System.Windows;
using System.Windows.Threading;

namespace Zebra
{
    class ViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        Random rng;
        ChessPosition position;
        public ChessPosition Position { get { return position; } set { position = value; OnPropertyChanged("Position"); } }
        ObservableCollection<ChessPiece> chessPieces;
        public ObservableCollection<ChessPiece> ChessPieces { get { return chessPieces; } set { chessPieces = value; OnPropertyChanged("ChessPieces"); } }
        ObservableCollection<ChessMove> possibleMoves;
        public ObservableCollection<ChessMove> PossibleMoves { get { return possibleMoves; } set { possibleMoves = value; OnPropertyChanged("PossibleMoves"); } }
        ObservableCollection<ChessMove> moveHistory;
        public ObservableCollection<ChessMove> MoveHistory { get { return moveHistory; } set { moveHistory = value; OnPropertyChanged("MoveHistory"); } }
        int halfMovesSinceLastPawnMoveOrCapture;
        public int HalfMovesSinceLastPawnMoveOrCapture { get { return halfMovesSinceLastPawnMoveOrCapture; } set { halfMovesSinceLastPawnMoveOrCapture = value; OnPropertyChanged("HalfMovesSinceLastPawnMoveOrCapture"); } }
        UInt64 hashKey;
        public UInt64 HashKey { get { return hashKey; } set { hashKey = value; OnPropertyChanged("HashKey"); } }
        int nodesEvaluated;
        public int NodesEvaluated { get { return nodesEvaluated; } set { nodesEvaluated = value; OnPropertyChanged("NodesEvaluated"); } }
        int gameStartPly;
        public int GameStartPly { get { return gameStartPly; } set { gameStartPly = value; OnPropertyChanged("GameStartPly"); } }
        int currentGamePly;
        public int CurrentGamePly { get { return currentGamePly; } set { currentGamePly = value; OnPropertyChanged("CurrentGamePly"); } }
        bool isSearching;
        public bool IsSearching { get { return isSearching; } set { isSearching = value; OnPropertyChanged("IsSearching"); } }
        readonly ICommand newGameCommand;
        public ICommand NewGameCommand { get { return newGameCommand; } }
        readonly ICommand movePieceCommand;
        public ICommand MovePieceCommand { get { return movePieceCommand; } }
        readonly ICommand makeMoveCommand;
        public ICommand MakeMoveCommand { get { return makeMoveCommand; } }
        readonly ICommand undoMoveCommand;
        public ICommand UndoMoveCommand { get { return undoMoveCommand; } }
        readonly ICommand remakeMoveCommand;
        public ICommand RemakeMoveCommand { get { return remakeMoveCommand; } }
        readonly ICommand makeRandomMoveCommand;
        public ICommand MakeRandomMoveCommand { get { return makeRandomMoveCommand; } }
        readonly ICommand makeMovesFromClipboardCommand;
        public ICommand MakeMovesFromClipboardCommand { get { return makeMovesFromClipboardCommand; } }
        readonly ICommand gotoGamePlyCommand;
        public ICommand GotoGamePlyCommand { get { return gotoGamePlyCommand; } }
        readonly ICommand gotoStartCommand;
        public ICommand GotoStartCommand { get { return gotoStartCommand; } }
        readonly ICommand gotoEndCommand;
        public ICommand GotoEndCommand { get { return gotoEndCommand; } }
        readonly ICommand makeBestMoveCommand;
        public ICommand MakeBestMoveCommand { get { return makeBestMoveCommand; } }
        readonly ICommand stopSearchCommand;
        public ICommand StopSearchCommand { get { return stopSearchCommand; } }
        readonly ICommand saveCommand;
        public ICommand SaveCommand { get { return saveCommand; } }
        readonly BackgroundWorker worker;
        string engineText;
        public string EngineText { get { return engineText; } set { engineText = value; OnPropertyChanged("EngineText"); } }

        public ViewModel()
        {
            rng = new Random();
            position = new ChessPosition();
            gameStartPly = position.GameStartPlyNr;
            possibleMoves = new ObservableCollection<ChessMove>();
            moveHistory = new ObservableCollection<ChessMove>();
            chessPieces = new ObservableCollection<ChessPiece>();
            remakeObservableCollections();
            newGameCommand = new DelegateCommand(newGame);
            movePieceCommand = new DelegateCommand(movePiece);
            makeMoveCommand = new DelegateCommand(makeMove);
            undoMoveCommand = new DelegateCommand(undoMove);
            gotoStartCommand = new DelegateCommand(gotoStart);
            gotoEndCommand = new DelegateCommand(gotoEnd);
            remakeMoveCommand = new DelegateCommand(remakeMove);
            makeRandomMoveCommand = new DelegateCommand(makeRandomMove);
            makeMovesFromClipboardCommand = new DelegateCommand(makeMovesFromClipboard);
            gotoGamePlyCommand = new DelegateCommand(gotoGamePly);
            makeBestMoveCommand = new DelegateCommand(makeBestMove);
            stopSearchCommand = new DelegateCommand(stopSearch);
            saveCommand = new DelegateCommand(save);
            worker = new BackgroundWorker();
            worker.WorkerReportsProgress = true;
            worker.DoWork += new DoWorkEventHandler(worker_DoWork);
            worker.ProgressChanged += new ProgressChangedEventHandler(worker_ProgressChanged);
            worker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(worker_RunWorkerCompleted);
            position.PrincUpdated += new EventHandler(position_PrincUpdated);
        }

        void remakeObservableCollections()
        {
            chessPieces.Clear();
            for (int rank = 0; rank <= 7; rank++)
                for (int file = 0; file <= 7; file++)
                {
                    Piece p = position.Board[file + rank * 8].Piece;
                    if (p.PieceColour != PieceColour.None)
                        chessPieces.Add(new ChessPiece(p.PieceType, p.PieceColour, file, rank));
                }
            possibleMoves.Clear();
            foreach (ChessMove move in position.PossibleMoves)
                possibleMoves.Add(move);
            HalfMovesSinceLastPawnMoveOrCapture = position.HalfMovesSinceLastPawnMoveOrCapture;
            HashKey = position.HashKey;
            NodesEvaluated = position.NodesEvaluated;
            CurrentGamePly = position.CurrentGamePly;
        }

        #region ICommand handlers
        void newGame(object o)
        {
            position.NewGame();
            moveHistory.Clear();
            remakeObservableCollections();
        }

        void movePiece(object o)
        {
            object[] args = (object[])o;
            ChessPiece piece = (ChessPiece)args[0];
            int toFile = (int)args[1];
            int toRank = (int)args[2];
            if (toFile < 0 || toFile > 7 || toRank < 0 || toRank > 7 || (toFile == piece.File && toRank == piece.Rank)) return;
            ChessMove move = new ChessMove(new Square(piece.File, piece.Rank), new Square(toFile, toRank));
            makeMove(move);
        }

        void makeMove(object o)
        {
            ChessMove move = (ChessMove)o;
            if (position.MakeMove(move))        // Makes a move and deletes all moves after in MoveHistory
            {
                // If the move was valid then delete all moves after in this.moveHistory and add the move
                while (moveHistory.Count > position.MoveHistory.Count - 1)
                    moveHistory.RemoveAt(moveHistory.Count - 1);
                moveHistory.Add(position.MoveHistory[moveHistory.Count]);
                remakeObservableCollections();
            }
        }

        void undoMove(object o)
        {
            if (position.GotoGamePly(position.CurrentGamePly - 1))
                remakeObservableCollections();
        }

        void remakeMove(object o)
        {
            if (position.GotoGamePly(position.CurrentGamePly + 1))
                remakeObservableCollections();
        }

        void makeRandomMove(object o)
        {
            ChessMove move = position.PossibleMoves[rng.Next(position.PossibleMoves.Count)];
            makeMove(move);
        }

        void gotoGamePly(object o)
        {
            // o contains Selected Index, first item is zero
            int plyNr = (int)o + 1 + GameStartPly;
            if (position.GotoGamePly(plyNr))
                remakeObservableCollections();
        }

        void gotoStart(object o)
        {
            if (position.GotoGamePly(0))
                remakeObservableCollections();
        }

        void gotoEnd(object o)
        {
            if (position.GotoGamePly(MoveHistory.Count))
                remakeObservableCollections();
        }

        void makeBestMove(object o)
        {
            worker.RunWorkerAsync();
            IsSearching = true;
        }

        void stopSearch(object o)
        {
            position.StopSearch();
        }

        void makeMovesFromClipboard(object o)
        {
            if (Clipboard.ContainsText())
            {
                string text = Clipboard.GetText();
                if (text != "")
                {
                    while (moveHistory.Count > position.CurrentGamePly - position.GameStartPlyNr)
                        MoveHistory.RemoveAt(MoveHistory.Count - 1);
                    position.MakeMovesFromString(text);
                    while (moveHistory.Count < position.MoveHistory.Count)
                    {
                        moveHistory.Add(position.MoveHistory[moveHistory.Count]);
                    }
                    remakeObservableCollections();
                }
            }
        }

        void save(object o)
        {
            position.SavePGN((string)o);
        }
        #endregion

        #region BackgroundWorker stuff
        void worker_DoWork(object sender, DoWorkEventArgs e)
        {
            position.FindBestMove(6);
        }

        void worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            StringBuilder b = new StringBuilder(Environment.NewLine);
            for (int i = 0; i < 6; i++)
            {
                b.Append(position.Princ[0, i]).Append(" ");
            }
            EngineText += b.ToString();
        }

        void worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            ChessMove move = position.Princ[0, 0];
            makeMove(move);
            IsSearching = false;
        }

        void position_PrincUpdated(object sender, EventArgs e)
        {
            worker.ReportProgress(0, null);
        }

        protected virtual void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion
    }
}
