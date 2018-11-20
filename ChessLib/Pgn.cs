using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ChessLib
{
    public class Pgn
    {
        public string Event { get; set; }
        public string Site { get; set; }
        public string Date { get; set; }
        public string Round { get; set; }
        public string White { get; set; }
        public string Black { get; set; }
        public string Result { get; set; }
        public string MoveText { get; set; }

        public Pgn(ChessPosition position)
        {
            Event = position.Event;
            Site = position.Site;
            Date = position.Date;
            Round = position.Round;
            White = position.White;
            Black = position.Black;
            Result = position.Result;
            StringBuilder sb = new StringBuilder();
            int plyNr = position.GameStartPlyNr;
            StringBuilder c = new StringBuilder();      // Current line
            string s;
            if (plyNr % 2 == 1)
                c.Append(plyNr / 2 + 1).Append(". ...");
            foreach (ChessMove move in position.MoveHistory)
            {
                s = "";
                if (plyNr % 2 == 0)
                    s = ((plyNr / 2 + 1).ToString() + ".");
                s += move.Text + " ";
                if (c.Length + s.Length > 80)
                {
                    // If the line length will go over 80 then append the current line to the sb and reset the current line to the string that would have put it over
                    sb.AppendLine(c.ToString());
                    c.Clear();
                }
                c.Append(s);
                plyNr++;
            }
            sb.Append(c);
            MoveText = sb.ToString();
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("[Event \"" + Event + "\"]");
            sb.AppendLine("[Site \"" + Site + "\"]");
            sb.AppendLine("[Date \"" + Date + "\"]");
            sb.AppendLine("[Round \"" + Round + "\"]");
            sb.AppendLine("[White \"" + White + "\"]");
            sb.AppendLine("[Black \"" + Black + "\"]");
            sb.AppendLine("[Result \"" + Result + "\"]");
            sb.AppendLine();
            sb.AppendLine(MoveText);
            return sb.ToString();
        }
    }
}
