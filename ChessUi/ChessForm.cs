﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using Chess;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Color = Chess.Color;

namespace ChessUi
{
    public partial class ChessForm : Form
    {
        public ChessForm() {
            InitializeComponent();
        }

        private Game ChessGame { get; set; }
        internal VisibleBoard VisibleBoard { get; set; }
        private Engine Engine { get; set; }

        private void Flipp() {
            VisibleBoard.Flipped = !VisibleBoard.Flipped;
            panel1.Invalidate();
        }

        private void Form1_Load(object sender, EventArgs e) {
            panelEdit.Height = 0;
            ChessGame = new Game();
            ChessGame.New();
            Engine = new Engine();
            VisibleBoard = new VisibleBoard(ChessGame, Engine);
        }

        internal void Redraw() {
            panel1.Invalidate();
        }

        private void MoveToList(Evaluation evaluatedMove) {
            var move = evaluatedMove.Move;
            //Removing moves in the future if the list was browsed back.
            for (int i = listView1.Items.Count - 1; i >= 0; i--) {
                if (move.NumberInGame < int.Parse(listView1.Items[i].SubItems[0].Text)) {
                    listView1.Items.RemoveAt(i);
                } else if (move.NumberInGame == int.Parse(listView1.Items[i].SubItems[0].Text) && move.Piece.Color == Chess.Color.White) {
                    listView1.Items.RemoveAt(i);
                } else if (move.NumberInGame == int.Parse(listView1.Items[i].SubItems[0].Text) && move.Piece.Color == Chess.Color.Black) {
                    if (listView1.Items[i].SubItems.Count > 3)
                        listView1.Items[i].SubItems.RemoveAt(2);
                }
            }

            if (move.Piece.Color == Chess.Color.White) {
                listView1.Items.Add(new ListViewItem {
                    Text = move.NumberInGame.ToString(),
                    SubItems =
                        {
                            new MoveListSubItem(evaluatedMove) {Text = move.ToString()}
                        },
                    UseItemStyleForSubItems = false
                });
            } else {
                //After a edit it can be blacks move.
                if (listView1.Items.Count == 0)
                    listView1.Items.Add(new ListViewItem {
                        Text = move.NumberInGame.ToString(),
                        SubItems =
                        {
                            new ListViewItem.ListViewSubItem {Text = ""}
                        },
                        UseItemStyleForSubItems = false
                    });

                listView1.Items[listView1.Items.Count - 1].SubItems.Add(new MoveListSubItem(evaluatedMove));
            }
            var list = GetMoveListItems();
            list.ForEach(x => x.ResetStyle());
            if (list.Any())
                list.Last().BackColor = VisibleBoard.SelectedColor;
        }
        
        private List<MoveListSubItem> GetMoveListItems() {
            var list = new List<MoveListSubItem>();
            foreach (var item in listView1.Items) {
                var listViewItem = (ListViewItem)item;
                var whiteMoveItem = listViewItem.SubItems[1] as MoveListSubItem;
                if (whiteMoveItem != null)
                    list.Add(whiteMoveItem);
                if (listViewItem.SubItems.Count > 2) {
                    var blackMoveItem = (MoveListSubItem)listViewItem.SubItems[2];
                    list.Add(blackMoveItem);
                }
            }
            return list;
        }

        private void MoveBackWards() {
            var lastIndex = listView1.Items.Count - 1;
            if (lastIndex < 0)
                return;

            var list = GetMoveListItems();

            var selectedSubItem = list.SingleOrDefault(x => x.BackColor == VisibleBoard.SelectedColor);
            if (selectedSubItem == null)
                return;

            list.ForEach(x => x.ResetStyle());

            var index = list.IndexOf(selectedSubItem);
            if (index > 0) {
                list[index - 1].BackColor = VisibleBoard.SelectedColor;
            }
            ChessGame.UndoLastMove();
            SetScoreLabel(list[index].EvaluatedMove);
            AnimateMove(list[index].EvaluatedMove, reverse: true);
            panel1.Invalidate();
        }

        private bool MoveForWards() {
            var lastIndex = listView1.Items.Count - 1;
            if (lastIndex < 0)
                return false;

            var list = GetMoveListItems();

            var selectedSubItem = list.SingleOrDefault(x => x.BackColor == VisibleBoard.SelectedColor);

            if (selectedSubItem == null) {
                var nextSubItem = list.FirstOrDefault();
                if (nextSubItem == null)
                    return false;
            }

            var index = list.IndexOf(selectedSubItem) + 1;
            if (index > list.Count - 1)
                return false;

            list.ForEach(x => x.ResetStyle());

            var nextMove = list[index].EvaluatedMove.Move;

            list[index].BackColor = VisibleBoard.SelectedColor;

            ChessGame.PerformLegalMove(nextMove);
            SetScoreLabel(list[index].EvaluatedMove);
            AnimateMove(list[index].EvaluatedMove);
            panel1.Invalidate();
            return true;
        }

        private void Undo() {
            StopAi();
            MoveBackWards();
            //RemoveLastMoveFromList();
            panel1.Invalidate();
        }

        private void Form1_KeyDown(object sender, KeyEventArgs e) {
            if (e.Control && e.KeyCode == Keys.Z) {
                Undo();
            }

            if (ComputerIsOff()) {
                if (e.KeyCode == Keys.Left) {
                    MoveBackWards();
                }

                if (e.KeyCode == Keys.Right) {
                    MoveForWards();
                }
            }
        }

        private void StopAi() {
            checkBoxAI_white.Checked = false;
            checkBoxAIblack.Checked = false;
            Engine.Stop();
        }

        private bool ComputerIsOff() {
            return !checkBoxAI_white.Checked && !checkBoxAIblack.Checked;
        }

        private void panel1_Resize(object sender, EventArgs e) {
            panel1.Invalidate();
        }

        private void panel1_Paint(object sender, PaintEventArgs e) {
            VisibleBoard.Paint(e.Graphics);
        }

        private void panel1_MouseMove(object sender, MouseEventArgs e) {
            if (VisibleBoard.MouseDownSquare?.Piece != null) {
                VisibleBoard.MouseX = e.X;
                VisibleBoard.MouseY = e.Y;
                panel1.Invalidate();
            }
        }

        private void panel1_MouseUp(object sender, MouseEventArgs e) {

            VisibleBoard.MouseUp(e.X, e.Y);

            if (VisibleBoard.EditMode) {
                MakeEditMove(e.X, e.Y);
                return;
            }

            if (VisibleBoard.MouseDownSquare == null)
                return;

            if (VisibleBoard.MouseUpSquare == null) {
                VisibleBoard.MouseDownSquare = null;
                panel1.Invalidate();
                return;
            }

            if (Engine.ThinkingFor == null) {
                var cmd = new MoveCommand(VisibleBoard.MouseDownSquare.File, VisibleBoard.MouseDownSquare.Rank,
                    VisibleBoard.MouseUpSquare.File, VisibleBoard.MouseUpSquare.Rank);
                if (ChessGame.TryPossibleMoveCommand(cmd)) {
                    var evaluatedMove = new Evaluation { Move = ChessGame.OtherPlayer.Moves.First() };
                    MoveToList(evaluatedMove);
                    //SetScoreLabel(evaluatedMove);
                    panel1.Invalidate();
                    Application.DoEvents();
                    CheckForEnd();
                    EngineMove();
                }
            }

            VisibleBoard.MouseDownSquare = null;
            VisibleBoard.MouseUpSquare = null;
            panel1.Invalidate();

        }

        private void MakeEditMove(int x, int y) {
            var fromSquare = VisibleBoard.MouseDownSquare;
            var toSquare = VisibleBoard.MouseUpSquare;
            if (VisibleBoard.MouseDownSquare == null)
                return;


            ChessGame.MakeEditMove(fromSquare, toSquare);
            VisibleBoard.MouseDownSquare = null;
            panel1.Invalidate();
        }

        private void CheckForEnd() {
            if (ChessGame.Ended) {
                if (ChessGame.Winner != null)
                    MessageBox.Show(this, $"{ChessGame.Winner.Color} won!", "Chess Ai");
                else
                    MessageBox.Show(this, "Draw");
                StopAi();
            }
        }

        private void panel1_MouseDown(object sender, MouseEventArgs e) {

            VisibleBoard.MouseDown(e.X, e.Y);
            VisibleBoard.MouseX = e.X;
            VisibleBoard.MouseY = e.Y;
            panel1.Invalidate();
        }

        private void loadToolStripMenuItem_Click(object sender, EventArgs e) {
            var ofd = new OpenFileDialog();
            if (ofd.ShowDialog(this) == DialogResult.OK)
                LoadFile(ofd.FileName);
            panel1.Invalidate();
        }

        private void LoadFile(string fileName) {
            var gameFile = GameFile.Load(fileName);
            ChessGame.Load(gameFile);
            var moves = new List<Move>();
            moves.AddRange(ChessGame.WhitePlayer.Moves);
            moves.AddRange(ChessGame.BlackPlayer.Moves);
            moves = moves.OrderBy(x => x.NumberInGame).ThenBy(x => x.Piece.Color).ToList();

            foreach (var move in moves) {
                MoveToList(new Evaluation { Move = move });
            }
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e) {
            var sfd = new SaveFileDialog();
            if (sfd.ShowDialog(this) == DialogResult.OK)
            {
                while (MoveForWards()){}
                ChessGame.Save(sfd.FileName);
            }
        }

        private void newToolStripMenuItem_Click(object sender, EventArgs e) {
            if (
                MessageBox.Show(this, "Are you sure?", "New chess game", MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question) == DialogResult.Yes) {
                checkBoxAIblack.Checked = false;
                checkBoxAI_white.Checked = false;
                listView1.Items.Clear();
                ChessGame.New();
                panel1.Invalidate();
            }
        }

        private void checkBoxAIblack_CheckedChanged(object sender, EventArgs e) {
            if (Engine.ThinkingFor == null)
                EngineMove();
        }

        private async void EngineMove() {
            if (ChessGame.Ended)
                return;

            var checkWhite = checkBoxAI_white.Checked;
            var checkedBlack = checkBoxAIblack.Checked;
            if (VisibleBoard.Flipped) {
                checkWhite = checkBoxAIblack.Checked;
                checkedBlack = checkBoxAI_white.Checked;
            }

            if (ChessGame.CurrentPlayer.Color == Chess.Color.Black && checkedBlack ||
                ChessGame.CurrentPlayer.Color == Chess.Color.White && checkWhite) {

                var thinkFor = ChessGame.CurrentPlayer.Color == Chess.Color.White
                    ? TimeSpan.FromSeconds((int)numericUpDownThinkWhite.Value)
                    : TimeSpan.FromSeconds((int)numericUpDownThinkBlack.Value);

                if (VisibleBoard.Flipped) {
                    thinkFor = ChessGame.CurrentPlayer.Color == Chess.Color.Black
                ? TimeSpan.FromSeconds((int)numericUpDownThinkWhite.Value)
                : TimeSpan.FromSeconds((int)numericUpDownThinkBlack.Value);
                }


                var moveResult = Engine.AsyncBestMoveDeepeningSearch(ChessGame.Copy(), thinkFor);

                //make the progress bar start moving
                Thread.Sleep(10);
                InitProgress();
                panel1.Invalidate();
                Application.DoEvents();

                try {
                    await moveResult;
                } catch (Exception ex) {
                    MessageBox.Show(ex.ToString());
                    EngineMove();
                }

                var evaluatedMove = moveResult.Result;
                if (evaluatedMove == null) {
                    return;
                }

                AnimateMove(evaluatedMove);
                SetScoreLabel(evaluatedMove);
                if (!ChessGame.TryPossibleMoveCommand(new MoveCommand(evaluatedMove.Move))) {
                    MessageBox.Show(this, $"Engine tries invalid move\r\n{evaluatedMove.Move.ToString()}");
                    return;
                }

                evaluatedMove.Move = ChessGame.OtherPlayer.Moves.First();
                MoveToList(evaluatedMove);

                CheckForEnd();

                panel1.Invalidate();
                Application.DoEvents();
                EngineMove();
            }
        }

        private List<Guid> animations = new List<Guid>(new []{Guid.NewGuid()});
        private void AnimateMove(Evaluation evaluatedMove, bool reverse = false) {
            var from = VisibleBoard.Squares.Single(x => x.Key.ToString() == evaluatedMove.Move.FromSquare.ToString());
            var to = VisibleBoard.Squares.Single(x => x.Key.ToString() == evaluatedMove.Move.ToSquare.ToString());
            var piece = from.Key.Piece ?? to.Key.Piece;
            if (reverse) {
                var temp = from;
                from = to;
                to = temp;
            }
            const float steps = 20;
            var dx = (to.Value.X - from.Value.X) / (steps- 1);
            var dy = (to.Value.Y - from.Value.Y) / (steps - 1);
            var animId = Guid.NewGuid();
            animations.Remove(animations.Last());
            animations.Add(animId);
            for (int i = 0; i < steps; i++) {
                if (!animations.Contains(animId))
                    break;
                var x = from.Value.X + dx * i;
                var y = from.Value.Y + dy * i;
                VisibleBoard.OffsetAnimated(piece, x, y);
                panel1.Invalidate();
                Application.DoEvents();
            }
            VisibleBoard.OffsetAnimated(null, 0, 0);
            panel1.Invalidate();
        }

        private void InitProgress() {
            var progWhite = VisibleBoard.Flipped ? progressBarTop : progressBarBottom;
            var progBlack = VisibleBoard.Flipped ? progressBarBottom : progressBarTop;
            progBlack.Value = 0;
            progWhite.Value = 0;
            progBlack.Hide();
            progWhite.Hide();

            if (ChessGame.CurrentPlayer.Color == Chess.Color.White) {
                progWhite.Maximum = Engine.SearchFor.Seconds;
                progWhite.Show();
            } else if (ChessGame.CurrentPlayer.Color == Chess.Color.Black) {
                progBlack.Maximum = Engine.SearchFor.Seconds;
                progBlack.Show();
            }
            Application.DoEvents();
        }

        private void SetScoreLabel(Evaluation evaluatedMove) {
            labelScoreAndLine.Text = $"Best: {evaluatedMove.Move}   Nodes: {evaluatedMove.Nodes.KiloNumber()}   Score: {evaluatedMove.Value}   Best line: {evaluatedMove.BestLine}";
        }

        private void checkBoxAI_white_CheckedChanged(object sender, EventArgs e) {
            if (Engine.ThinkingFor == null)
                EngineMove();
        }

        private void undoToolStripMenuItem_Click(object sender, EventArgs e) {
            Undo();
        }

        private void buttonFlip_Click(object sender, EventArgs e) {
            Flipp();
        }

        ToolTip mTooltip;
        Point mLastPos = new Point(-1, -1);

        private void listview_MouseMove(object sender, MouseEventArgs e) {
            ListViewHitTestInfo info = listView1.HitTest(e.X, e.Y);

            if (mTooltip == null)
                mTooltip = new ToolTip();

            if (mLastPos != e.Location) {
                if (info.Item != null && info.SubItem != null) {
                    mTooltip.Show(info.SubItem.ToString(), info.Item.ListView, e.X + 16, e.Y + 16, 20000);
                } else {
                    mTooltip.SetToolTip(listView1, string.Empty);
                }
            }

            mLastPos = e.Location;
        }

        private void timer1_Tick(object sender, EventArgs e) {
            var progWhite = VisibleBoard.Flipped ? progressBarTop : progressBarBottom;
            var progBlack = VisibleBoard.Flipped ? progressBarBottom : progressBarTop;

            if (Engine.ThinkingFor == Chess.Color.White) {
                if (progWhite.Value < progWhite.Maximum)
                    progWhite.Value += 1;
            } else if (Engine.ThinkingFor == Chess.Color.Black) {
                if (progBlack.Value < progBlack.Maximum)
                    progBlack.Value += 1;
            }
        }

        private void newspaperToolStripMenuItem_Click(object sender, EventArgs e) {
            regilarToolStripMenuItem.Checked = !newspaperToolStripMenuItem.Checked;
            changePieceImage();
        }

        private void changePieceImage() {
            VisibleBoard.PieceImage = regilarToolStripMenuItem.Checked ? PieceImage.Regular : PieceImage.Newspaper;
            panel1.Invalidate();
        }

        private void regilarToolStripMenuItem_Click(object sender, EventArgs e) {
            newspaperToolStripMenuItem.Checked = !regilarToolStripMenuItem.Checked;
            changePieceImage();
        }

        private void boardToolStripMenuItem_Click(object sender, EventArgs e) {
            if (boardToolStripMenuItem.Checked)
                EnterEditMode();
            else
                ExitEditMode();
        }

        private void ExitEditMode() {
            if (ChessGame.OtherKingAttacked())
            {
                MessageBox.Show(this, $"{ChessGame.OtherPlayer.Color} King can not be attacked when it is {ChessGame.CurrentPlayer.Color}s turn.", "Invalid Setup");
                return;
            }

            WhiteLongCastlingRights();
            WhiteShortCastlingRights();
            BlackLongCastlingRights();
            BlackShortCastlingRights();
            boardToolStripMenuItem.Checked = false;
            checkBoxAI_white.Enabled = true;
            checkBoxAIblack.Enabled = true;
            ChessGame.EditMode = false;
            VisibleBoard.EditMode = false;
            ToggleEditPanel();
            ChessGame.SetInitials();
        }

        private void EnterEditMode() {
            boardToolStripMenuItem.Checked = true;
            StopAi();
            checkBoxAI_white.Enabled = false;
            checkBoxAIblack.Enabled = false;
            checkBoxWCK.Checked = ChessGame.WhitePlayer.CanCastleKingSide;
            checkBoxWCQ.Checked = ChessGame.WhitePlayer.CanCastleQueenSide;
            checkBoxBCK.Checked = ChessGame.BlackPlayer.CanCastleKingSide;
            checkBoxBCQ.Checked = ChessGame.BlackPlayer.CanCastleQueenSide;
            if (ChessGame.CurrentPlayer.Color == Color.White)
                radioButtonWhiteMoves.Checked = true;
            else
                radioButtonBlackMoves.Checked = true;
            listView1.Items.Clear();
            ChessGame.EnterEditMode();
            VisibleBoard.EditMode = true;
            ToggleEditPanel();
        }

        private async void ToggleEditPanel() {
            if (VisibleBoard.EditMode) {
                for (int i = 0; i < 30; i++) {
                    await Task.Delay(10);
                    panelEdit.Height = i * 10;
                }
            } else if (panelEdit.Height == 290) {
                for (int i = 0; i < 30; i++) {
                    await Task.Delay(10);
                    panelEdit.Height = 290 - i * 10;
                }
            }
        }

        private void radioButtonWhite_CheckedChanged(object sender, EventArgs e) {
            pictureBoxBishop.Image = Properties.Resources.WhiteBishop;
            pictureBoxBishop.PieceType = PieceType.WhiteBishop;

            pictureBoxKnight.Image = Properties.Resources.WhiteKnight;
            pictureBoxKnight.PieceType = PieceType.WhiteNight;

            pictureBoxPawn.Image = Properties.Resources.WhitePawn;
            pictureBoxPawn.PieceType = PieceType.WhitePawn;

            pictureBoxQueen.Image = Properties.Resources.WhiteQueen;
            pictureBoxQueen.PieceType = PieceType.WhiteQueen;

            pictureBoxRook.Image = Properties.Resources.WhiteRook;
            pictureBoxRook.PieceType = PieceType.WhiteRook;
        }

        private void radioButtonBlack_CheckedChanged(object sender, EventArgs e) {
            pictureBoxBishop.Image = Properties.Resources.BlackBishop;
            pictureBoxBishop.PieceType = PieceType.BlackBishop;

            pictureBoxKnight.Image = Properties.Resources.BlackKnight;
            pictureBoxKnight.PieceType = PieceType.BlackKnight;

            pictureBoxPawn.Image = Properties.Resources.BlackPawn;
            pictureBoxPawn.PieceType = PieceType.BlackPawn;

            pictureBoxQueen.Image = Properties.Resources.BlackQueen;
            pictureBoxQueen.PieceType = PieceType.BlackQueen;

            pictureBoxRook.Image = Properties.Resources.BlackRook;
            pictureBoxRook.PieceType = PieceType.BlackRook;
        }

        private void pictureBox_MouseDown(object sender, MouseEventArgs e) {
            var pictureBox = (PictureBox)sender;
            pictureBox.DoDragDrop(pictureBox, DragDropEffects.Copy | DragDropEffects.Move);
        }

        private void panel1_DragEnter(object sender, DragEventArgs e) {
            if (!e.Data.GetDataPresent(typeof(ChessPiecePictureBox))) {
                e.Effect = DragDropEffects.None;
                return;
            }

            e.Effect = DragDropEffects.Copy;
            var img = ((ChessPiecePictureBox)e.Data.GetData(typeof(ChessPiecePictureBox))).Image;
            var bitmap = new Bitmap(img);

            Cursor.Current = CreateCursor(bitmap, 45, 45);
            bitmap.Dispose();
        }

        [DllImport("user32.dll")]
        public static extern IntPtr CreateIconIndirect(ref IconInfo icon);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetIconInfo(IntPtr hIcon, ref IconInfo pIconInfo);

        public struct IconInfo
        {
            public bool fIcon;
            public int xHotspot;
            public int yHotspot;
            public IntPtr hbmMask;
            public IntPtr hbmColor;
        }

        private static Cursor CreateCursor(Bitmap bmp, int xHotSpot, int yHotSpot) {
            var inconInfo = new IconInfo();
            GetIconInfo(bmp.GetHicon(), ref inconInfo);
            inconInfo.xHotspot = xHotSpot;
            inconInfo.yHotspot = yHotSpot;
            inconInfo.fIcon = false;
            return new Cursor(CreateIconIndirect(ref inconInfo));
        }

        private void panel1_DragDrop(object sender, DragEventArgs e) {
            var chessPiecePictureBox = e.Data.GetData(typeof(ChessPiecePictureBox)) as ChessPiecePictureBox;
            if (!VisibleBoard.EditMode || chessPiecePictureBox == null)
                return;
            var clientPoint = panel1.PointToClient(new Point(e.X, e.Y));

            VisibleBoard.Drop(chessPiecePictureBox.PieceType, clientPoint.X, clientPoint.Y);


            panel1.Invalidate();
        }

        private void panel1_DragOver(object sender, DragEventArgs e) {
            if (!VisibleBoard.EditMode || !(sender is ChessPiecePictureBox))
                return;
        }

        private void pictureBox_GiveFeedback(object sender, GiveFeedbackEventArgs e) {
            e.UseDefaultCursors = false;
        }

        private void buttonClear_Click(object sender, EventArgs e) {
            if (!ChessGame.EditMode)
                return;

            ChessGame.EditClearPieces();
            checkBoxBCK.Checked = false;
            checkBoxBCQ.Checked = false;
            checkBoxWCK.Checked = false;
            checkBoxWCQ.Checked = false;
            panel1.Invalidate();
        }

        private void radioButtonWhiteMoves_CheckedChanged(object sender, EventArgs e) {
            ChessGame.CurrentPlayer = ChessGame.WhitePlayer;
            panel1.Invalidate();
        }

        private void radioButtonBlackMoves_CheckedChanged(object sender, EventArgs e) {
            ChessGame.CurrentPlayer = ChessGame.BlackPlayer;
            panel1.Invalidate();
        }

        private void buttonDone_Click(object sender, EventArgs e) {
            ExitEditMode();
        }

        private void checkBoxWCK_CheckedChanged(object sender, EventArgs e) {
            WhiteShortCastlingRights();
        }

        private void checkBoxWCQ_CheckedChanged(object sender, EventArgs e) {
            WhiteLongCastlingRights();
        }

        private void checkBoxBCQ_CheckedChanged(object sender, EventArgs e) {
            BlackLongCastlingRights();
        }

        private void checkBoxBCK_CheckedChanged(object sender, EventArgs e) {
            BlackShortCastlingRights();
        }

        private void WhiteShortCastlingRights() {
            var kingOk = ChessGame.WhitePlayer.Pieces.OfType<King>().Single().Square.ToString() == "e1";
            var rookOk = ChessGame.WhitePlayer.Pieces.OfType<Rook>().Any(rook => rook.Square.ToString() == "h1");

            if (kingOk && rookOk) {
                ChessGame.WhitePlayer.CanCastleKingSide = checkBoxWCK.Checked;
            } else {
                ChessGame.WhitePlayer.CanCastleKingSide = false;
                if (checkBoxWCK.Checked)
                {
                    checkBoxWCK.Checked = false;
                    MessageBox.Show(this, "Piece setup does not allow castling King side.");
                }
            }
        }

        private void WhiteLongCastlingRights() {
            var kingOk = ChessGame.WhitePlayer.Pieces.OfType<King>().Single().Square.ToString() == "e1";
            var rookOk = ChessGame.WhitePlayer.Pieces.OfType<Rook>().Any(rook => rook.Square.ToString() == "a1");

            if (kingOk && rookOk) {
                ChessGame.WhitePlayer.CanCastleQueenSide = checkBoxWCQ.Checked;
            } else {
                ChessGame.WhitePlayer.CanCastleQueenSide = false;
                if (checkBoxWCQ.Checked)
                {
                    checkBoxWCQ.Checked = false;
                    MessageBox.Show(this, "Piece setup does not allow castling Queen side.");
                }
            }
        }

        private void BlackShortCastlingRights() {
            var kingOk = ChessGame.BlackPlayer.Pieces.OfType<King>().Single().Square.ToString() == "e8";
            var rookOk = ChessGame.BlackPlayer.Pieces.OfType<Rook>().Any(rook => rook.Square.ToString() == "h8");

            if (kingOk && rookOk) {
                ChessGame.BlackPlayer.CanCastleKingSide = checkBoxBCK.Checked;
            } else {
                ChessGame.BlackPlayer.CanCastleKingSide = false;
                if (checkBoxBCK.Checked)
                {
                    checkBoxBCK.Checked = false;
                    MessageBox.Show(this, "Piece setup does not allow castling King side.");
                }
            }
        }

        private void BlackLongCastlingRights() {
            var kingOk = ChessGame.BlackPlayer.Pieces.OfType<King>().Single().Square.ToString() == "e8";
            var rookOk = ChessGame.BlackPlayer.Pieces.OfType<Rook>().Any(rook => rook.Square.ToString() == "a8");

            if (kingOk && rookOk) {
                ChessGame.BlackPlayer.CanCastleQueenSide = checkBoxBCQ.Checked;
            } else {
                ChessGame.BlackPlayer.CanCastleQueenSide = false;
                if (checkBoxBCQ.Checked)
                {
                    checkBoxBCQ.Checked = false;
                    MessageBox.Show(this, "Piece setup does not allow castling Queen side.");
                }
            }
        }

        private void buttonEditBoard_Click(object sender, EventArgs e) {
            if (!VisibleBoard.EditMode)
                EnterEditMode();
            else
                ExitEditMode();            
        }

        private void colorsToolStripMenuItem_Click(object sender, EventArgs e) {
            var colorForm = new ColorForm(this);
            colorForm.Show(this);
        }

        private void fENToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var fenForm = new FenForm();
            fenForm.FEN = ChessGame.GetFEN();
            if (fenForm.ShowDialog(this) == DialogResult.OK)
            {
                ChessGame.LoadFEN(fenForm.FEN);
                panel1.Invalidate();
            }
        }
    }
    public class ChessPiecePictureBox : PictureBox
    {
        public PieceType PieceType { get; set; }
    }

    public class DoubledBufferedPanel : Panel
    {
        public DoubledBufferedPanel() {
            base.DoubleBuffered = true;
        }
    }

    public class MoveListSubItem : ListViewItem.ListViewSubItem
    {
        public MoveListSubItem(Evaluation evaluatedMove) {
            EvaluatedMove = evaluatedMove;
            Text = EvaluatedMove.Move.ToString();
        }

        public Evaluation EvaluatedMove { get; }

        public override string ToString() {
            return EvaluatedMove.ToString();
        }
    }


}
