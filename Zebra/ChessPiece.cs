using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Collections.ObjectModel;
using ChessLib;

namespace Zebra
{
    class ChessPiece : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        PieceType pieceType;
        public PieceType PieceType { get { return pieceType; } set { pieceType = value; OnPropertyChanged("PieceType"); } }
        PieceColour pieceColour;
        public PieceColour PieceColour { get { return pieceColour; } set { pieceColour = value; OnPropertyChanged("PieceColour"); } }
        int rank;
        public int Rank { get { return rank; } set { rank = value; OnPropertyChanged("Rank"); } }
        int file;
        public int File { get { return file; } set { file = value; OnPropertyChanged("File"); } }

        public ChessPiece(PieceType pieceType, PieceColour pieceColour, int file, int rank)
        {
            this.pieceType = pieceType;
            this.pieceColour = pieceColour;
            this.rank = rank;
            this.file = file;
        }

        protected virtual void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
