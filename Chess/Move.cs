﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chess
{
    public class Move
    {
        public Move(Piece piece, Square toSquare, bool isCastling = false, bool isEnPassant = false, Piece capturedEnPassant = null, Piece castleRook = null) {
            Piece = piece;
            FromSquare = piece.Square;
            ToSquare = toSquare;

            IsCastling = isCastling;
            IsEnpassant = isEnPassant;
            if (!isEnPassant) {
                Capture = toSquare.Piece;
                CapturedFrom = toSquare;
            } else {
                Debug.Assert(capturedEnPassant != null);
                Capture = capturedEnPassant;
                CapturedFrom = capturedEnPassant.Square;
            }

            CastleRook = castleRook;
        }

        public Piece CastleRook { get; private set; }

        public bool IsCastling { get; private set; }

        public Piece Piece { get; set; }

        public Square ToSquare { get; private set; }

        public Square FromSquare { get; private set; }

        public Piece Capture { get; private set; }

        public bool IsEnpassant { get; private set; }

        public bool IsPromotion { get; set; }

        public Square CapturedFrom { get; private set; }

        public int? ScoreAfterMove { get; set; }

        public int NumberInGame { get; set; }

        public override string ToString() {
            if (IsCastling) {
                if (ToSquare.File == File.G)
                    return "0-0";

                if (ToSquare.File == File.C)
                    return "0-0-0";

                throw new ApplicationException("Invalid form of castling");
            }

            if (ScoreInfo.HasFlag(ScoreInfo.StaleMate))
                return "½-½"; //todo, text should be added after the move

            var cap = Capture != null ? "x" : "";
            var checkormate = ScoreInfo.HasFlag(ScoreInfo.Mate) ? "#" : IsCheck ? "+" : "";
            var piece = !(Piece is Pawn) ? Piece.Char.ToString() : "";
            if (Piece is Pawn && Capture != null)
                piece = FromSquare.File.ToString().ToLower();

            return $"{piece}{cap}{ToSquare}{checkormate}";
        }

        public ScoreInfo ScoreInfo { get; set; }

        public bool IsCheck { get; set; }
        public Pawn PromotedPawn { get; set; }
        public bool BlackWasChecked { get; set; }
        public bool WhiteWasChecked { get; set; }
        public Move OpponentsBestAiMove { get; set; }
        public bool? IsLegal { get; set; }
        public ulong PreviousHash { get; set; }
        internal File? PreviousEnPassant { get; set; }
        public ulong Hash { get; set; }
        public int PreviousMovesSinceLastCaptureOrPawnMove { get; internal set; }

        public string ToCommandString() {
            return FromSquare + "-" + ToSquare;
        }

        public string BestLine() {
            if (OpponentsBestAiMove != null)
                return OpponentsBestAiMove + " " + OpponentsBestAiMove?.BestLine();
            return "";
        }
    }

    [Flags]
    public enum ScoreInfo : byte
    {
        DrawByRepetion = 1,
        InsufficientMaterial = 2,
        StaleMate = 4,
        Mate = 8
    }
}
