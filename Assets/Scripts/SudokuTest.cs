#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace LiteSudoku
{
    public class GUIColorScope : IDisposable
    {
        private readonly Color Color_;

        public GUIColorScope(Color newColor)
        {
            Color_ = GUI.color;
            GUI.color = newColor;
        }

        public void Dispose()
        {
            GUI.color = Color_;
        }
    }

    public class SudokuWindow : EditorWindow
    {
        private readonly Grid Grid_ = new Grid();
        private SudokuSolver Solver_;
        private int StepIndex_;
        private int TargetStepIndex_;
        
        [MenuItem("Lite/Sudoku Test")]
        private static void OpenWin()
        {
            var win = EditorWindow.GetWindow<SudokuWindow>();
            win.Show();
        }

        private void OnEnable()
        {
            Grid_.GenerateFromData("000060009600080105090002000008000000053090006000700090000100043004050000062007008");
            Solver_ = new SudokuSolver(Grid_);
            StepIndex_ = 0;
            TargetStepIndex_ = 58;
        }

        private bool Step()
        {
            if (Solver_.Solve())
            {
                StepIndex_++;
                return true;
            }

            return false;
        }

        private void OnGUI()
        {
            var gx = 10;
            var gy = 50;

            if (GUI.Button(new Rect(5, 5, 120, 40), "Step"))
            {
                Step();
            }
            
            using (new GUIColorScope(Color.red))
            {
                EditorGUI.LabelField(new Rect(150, 5, 100, 20), $"{StepIndex_} - {(Solver_.Data.IsValid ? Solver_.Data.StrategyType : "None")}");
            }

            if (GUI.Button(new Rect(300, 5, 40, 40), "Go"))
            {
                while (StepIndex_ < TargetStepIndex_)
                {
                    if (!Step())
                    {
                        break;
                    }
                }
            }

            TargetStepIndex_ = EditorGUI.IntField(new Rect(350, 5, 100, 20), TargetStepIndex_);
            
            for (var y = 0; y < 9; ++y)
            {
                var by = gy + y * (22 * 3 + 5);
                
                for (var x = 0; x < 9; ++x)
                {
                    var bx = gx + x * (17 * 3 + 5);

                    var cell = Grid_.GetCell(x, y);

                    if (cell.IsPlaced())
                    {
                        // using (new GUIColorScope(Color.green))
                        {
                            EditorGUI.TextField(new Rect(bx, by, 17 * 3, 22 * 3), $"{cell.PlaceValue}", new GUIStyle(GUI.skin.textField)
                            {
                                alignment = TextAnchor.MiddleCenter,
                                fontSize = 50,
                            });
                        }
                    }
                    else
                    {
                        for (var iy = 0; iy < 3; ++iy)
                        {
                            for (var ix = 0; ix < 3; ++ix)
                            {
                                var index = iy * 3 + ix + 1;
                                var val = cell.IsClue(index);
                                var color = cell.GetClueColor(index);
                                
                                using (new GUIColorScope(color))
                                {
                                    EditorGUI.TextField(new Rect(bx + ix * 17, by + iy * 22, 14, 19), $"{(val ? index : " ")}");
                                }
                            }
                        }
                    }
                }
            }
        }
    }
    
    public static class Extend
    {
        public static List<T> AddUnique<T>(this List<T> list, T value)
        {
            if (list.Contains(value))
            {
                return list;
            }
            
            list.Add(value);
            return list;
        }
        
        public static List<T> AddUniqueRange<T>(this List<T> list, IEnumerable<T> values)
        {
            foreach (var v in values)
            {
                if (!list.Contains(v))
                {
                    list.Add(v);
                }
            }

            return list;
        }

        public static int[] GetCellIndexWithClue(this Cell[] cells, int clue)
        {
            var indices = new List<int>();

            for (var index = 0; index < cells.Length; ++index)
            {
                if (cells[index].IsClue(clue))
                {
                    indices.Add(index);
                }
            }

            return indices.ToArray();
        }

        public static Cell[] GetMatchCellsByClue(this Cell[] cells, int clue)
        {
            var result = new List<Cell>();

            foreach (var cell in cells)
            {
                if (cell.IsClue(clue))
                {
                    result.Add(cell);
                }
            }

            return result.ToArray();
        }

        public static Cell[] GetMatchCellsByClueCount(this Cell[] cells, int minClueCount, int maxClueCount)
        {
            var result = new List<Cell>();

            foreach (var cell in cells)
            {
                var clueCount = cell.GetClueCount();
                if (clueCount >= minClueCount && clueCount <= maxClueCount)
                {
                    result.Add(cell);
                }
            }

            return result.ToArray();
        }

        public static bool CellsInSameBlock(this Cell[] cells)
        {
            if (cells.Length == 0)
            {
                return false;
            }
            
            return cells.All(cell => cell.BlockIndex == cells[0].BlockIndex);
        }
        
        public static bool CellsInSameRow(this Cell[] cells)
        {
            if (cells.Length == 0)
            {
                return false;
            }
            
            return cells.All(cell => cell.RowIndex == cells[0].RowIndex);
        }

        public static bool CellsInSameColumn(this Cell[] cells)
        {
            if (cells.Length == 0)
            {
                return false;
            }

            return cells.All(cell => cell.ColumnIndex == cells[0].ColumnIndex);
        }
    }

    public static class Utils
    {
        public static string ArrayToString(int[] list)
        {
            var result = new StringBuilder();
            
            for (var i = 0; i < list.Length; ++i)
            {
                result.Append(list[i]);

                if (i != list.Length - 1)
                {
                    result.Append(',');
                }
            }

            return result.ToString();
        }
        
        public static bool ArrayEqual(int[] a, int[] b)
        {
            if (a == null || b == null)
            {
                return false;
            }

            if (a.Length != b.Length)
            {
                return false;
            }

            for (var i = 0; i < a.Length; ++i)
            {
                if (a[i] != b[i])
                {
                    return false;
                }
            }

            return true;
        }

        public static bool ArrayTripleApproxClues(Cell a, Cell b, Cell c)
        {
            var map = new Dictionary<int, int>();
            foreach (var clue in a.GetClueValues().Concat(b.GetClueValues()).Concat(c.GetClueValues()))
            {
                if (!map.ContainsKey(clue))
                {
                    map.Add(clue, 1);
                }
                else
                {
                    map[clue]++;
                }
            }

            if (map.Keys.Count > 3)
            {
                return false;
            }

            return true;
        }

        public static bool ArrayTripleApproxIndices(int[] a, int[] b, int[] c)
        {
            var map = new Dictionary<int, int>();
            foreach (var clue in a.Concat(b).Concat(c))
            {
                if (!map.ContainsKey(clue))
                {
                    map.Add(clue, 1);
                }
                else
                {
                    map[clue]++;
                }
            }

            if (map.Keys.Count != 3)
            {
                return false;
            }

            return true;
        }

        public static int[] GetIntersectionClues(params Cell[] cells)
        {
            var clues = new List<int>();

            foreach (var cell in cells)
            {
                clues.AddUniqueRange(cell.GetClueValues());
            }

            return clues.ToArray();
        }
    }

    public class Cell
    {
        public int RowIndex => CellID / 9;
        public int ColumnIndex => CellID % 9;
        public int BlockIndex => (RowIndex / 3) * 3 + ColumnIndex / 3;
            
        public int CellID { get; }
        
        public int OriginValue { get; }
        public int PlaceValue { get; private set; }
        public int Clues { get; private set; }
        public Color[] CluesColor { get; private set; }

        public string DebugInfo => $"<{RowIndex + 1},{ColumnIndex + 1}> - {PlaceValue} ({Utils.ArrayToString(GetClueValues())})";

        public Cell(int cellID, int origin)
        {
            CellID = cellID;
            OriginValue = origin;
            PlaceValue = origin;
            Clues = 0;
            CluesColor = new Color[9];
            ClearCluesColor();
        }

        private void ClearCluesColor()
        {
            for (var i = 0; i < 9; ++i)
            {
                CluesColor[i] = Color.white;
            }
        }

        public bool IsOrigin()
        {
            return OriginValue != 0;
        }
        
        public bool IsPlaced()
        {
            return IsOrigin() || PlaceValue != 0;
        }
        
        public bool IsClue()
        {
            return !IsPlaced() && Clues != 0;
        }

        public void Place(int value)
        {
            if (IsOrigin())
            {
                return;
            }
            
            PlaceValue = value;
            Clues = 0;
            ClearCluesColor();
        }

        public void AddClue(int value)
        {
            Clues |= 1 << value;
        }

        public void SubClue(int value)
        {
            Clues &= (~(1 << value));
        }

        public bool IsClue(int value)
        {
            return (Clues & (1 << value)) == (1 << value);
        }

        public int GetClueCount()
        {
            if (!IsClue())
            {
                return 0;
            }
            
            var count = 0;

            for (var i = 1; i <= 9; ++i)
            {
                count += IsClue(i) ? 1 : 0;
            }

            return count;
        }

        public int[] GetClueValues()
        {
            if (!IsClue())
            {
                return Array.Empty<int>();
            }

            var result = new List<int>();
            
            for (var i = 1; i <= 9; ++i)
            {
                if (IsClue(i))
                {
                    result.Add(i);
                }
            }

            return result.ToArray();
        }

        public void SetClueColor(int value, Color hintColor)
        {
            if (value is >= 1 and <= 9)
            {
                CluesColor[value - 1] = hintColor;
            }
        }

        public Color GetClueColor(int value)
        {
            if (value is >= 1 and <= 9)
            {
                return CluesColor[value - 1];
            }

            return Color.white;
        }

        public static bool operator ==(Cell left, Cell right)
        {
            if (object.Equals(left, null))
            {
                return object.Equals(right, null);
            }

            if (object.Equals(right, null))
            {
                return false;
            }
            
            return left.CellID == right.CellID;
        }

        public static bool operator !=(Cell left, Cell right)
        {
            return !(left == right);
        }
    }

    public class Grid
    {
        private readonly Cell[] Cells_;

        public Grid()
        {
            Cells_ = new Cell[9 * 9];
        }

        public void GenerateFromData(string data)
        {
            if (data.Length != Cells_.Length)
            {
                Console.WriteLine($"error data : {data}");
                return;
            }
            
            for (var i = 0; i < data.Length; ++i)
            {
                if (int.TryParse(data[i].ToString(), out var value) && value is >= 0 and <= 9)
                {
                    Cells_[i] = new Cell(i, value);
                }
                else
                {
                    Console.WriteLine($"error data : {data} - {data[i]}");
                    return;
                }
            }
        }

        private bool IsValidIndex(int index)
        {
            return index is >= 0 and < 81;
        }

        public Cell GetCell(int cellID)
        {
            if (!IsValidIndex(cellID))
            {
                return null;
            }

            return Cells_[cellID];
        }

        private bool IsValidCoordinate(int x, int y)
        {
            return x is >= 0 and < 9 && y is >= 0 and < 9;
        }

        public Cell GetCell(int x, int y)
        {
            if (!IsValidCoordinate(x, y))
            {
                return null;
            }
            
            var index = y * 9 + x;
            return Cells_[index];
        }

        public Cell[] GetCells()
        {
            return Cells_;
        }

        public void Place(int x, int y, int value)
        {
            var cell = GetCell(x, y);
            cell?.Place(value);
        }

        // 0 1 2
        // 3 4 5
        // 6 7 8
        public Cell[] GetCellsByBlock(int blockIndex)
        {
            if (blockIndex is < 0 or > 8)
            {
                return Array.Empty<Cell>();
            }

            var cells = new List<Cell>(9);

            var baseY = (blockIndex / 3) * 3;
            var baseX = (blockIndex % 3) * 3;
            
            for (var y = 0; y < 3; ++y)
            {
                for (var x = 0; x < 3; ++x)
                {
                    cells.Add(GetCell(baseX + x, baseY + y));
                }
            }

            return cells.ToArray();
        }

        public Cell[] GetCellsByRow(int rowIndex)
        {
            if (rowIndex is < 0 or > 8)
            {
                return Array.Empty<Cell>();
            }

            var cells = new List<Cell>(9);

            for (var x = 0; x < 9; ++x)
            {
                cells.Add(GetCell(x, rowIndex));
            }

            return cells.ToArray();
        }

        public Cell[] GetCellsByColumn(int columnIndex)
        {
            if (columnIndex is < 0 or > 8)
            {
                return Array.Empty<Cell>();
            }
            
            var cells = new List<Cell>(9);

            for (var y = 0; y < 9; ++y)
            {
                cells.Add(GetCell(columnIndex, y));
            }

            return cells.ToArray();
        }

        public Cell[] GetSeeCells(Cell target)
        {
            var cells = new List<Cell>(21);

            var blockCells = GetCellsByBlock(target.BlockIndex);
            cells.AddRange(blockCells);
            
            var rowCells = GetCellsByRow(target.RowIndex);
            cells.AddUniqueRange(rowCells);

            var columnCells = GetCellsByColumn(target.ColumnIndex);
            cells.AddUniqueRange(columnCells);

            return cells.ToArray();
        }

        public Cell[] GetIntersectionCells(Cell a, Cell b)
        {
            var cells = new List<Cell>();
            
            if (a.BlockIndex == b.BlockIndex)
            {
                var blockCells = GetCellsByBlock(a.BlockIndex);
                cells.AddRange(blockCells);
            }

            if (a.RowIndex == b.RowIndex)
            {
                var rowCells = GetCellsByRow(a.RowIndex);
                cells.AddUniqueRange(rowCells);
            }

            if (a.ColumnIndex == b.ColumnIndex)
            {
                var columnCells = GetCellsByColumn(a.ColumnIndex);
                cells.AddUniqueRange(columnCells);
            }

            return cells.ToArray();
        }
    }

    public interface ICommand
    {
        bool IsAssist { get; }
        
        void BeforeExecute(Grid grid);
        void Execute(Grid grid);
    }

    public abstract class CommandBase : ICommand
    {
        public virtual bool IsAssist => false;
        
        public int CellID { get; }

        protected CommandBase(int cellID)
        {
            CellID = cellID;
        }

        public abstract void BeforeExecute(Grid grid);

        public abstract void Execute(Grid grid);
    }
    
    public class AddClueCommand : CommandBase
    {
        public int ClueValue { get; }
        
        public AddClueCommand(int cellID, int clueValue)
            : base(cellID)
        {
            ClueValue = clueValue;
        }
        
        public override void BeforeExecute(Grid grid)
        {
        }
        
        public override void Execute(Grid grid)
        {
            var cell = grid.GetCell(CellID);
            cell.AddClue(ClueValue);
        }
    }

    public class SubClueCommand : CommandBase
    {
        public int ClueValue { get; }
        
        public SubClueCommand(int cellID, int clueValue)
            : base(cellID)
        {
            ClueValue = clueValue;
        }
        
        public override void BeforeExecute(Grid grid)
        {
            var cell = grid.GetCell(CellID);
            cell.SetClueColor(ClueValue, Color.red);
        }
        
        public override void Execute(Grid grid)
        {
            var cell = grid.GetCell(CellID);
            cell.SubClue(ClueValue);
            cell.SetClueColor(ClueValue, Color.white);
        }
    }

    public class PlacedCommand : CommandBase
    {
        public int PlacedValue { get; }
        
        public PlacedCommand(int cellID, int placedValue)
            : base(cellID)
        {
            PlacedValue = placedValue;
        }
        
        public override void BeforeExecute(Grid grid)
        {
            var cell = grid.GetCell(CellID);
            cell.SetClueColor(PlacedValue, Color.green);
        }
        
        public override void Execute(Grid grid)
        {
            var cell = grid.GetCell(CellID);
            cell.Place(PlacedValue);
            cell.SetClueColor(PlacedValue, Color.white);
        }
    }

    public class HintColorCommand : CommandBase
    {
        public override bool IsAssist => true;
        
        public int ClueValue { get; }
        public Color HintColor { get; }

        public HintColorCommand(int cellID, int clueValue, Color hintColor)
            : base(cellID)
        {
            ClueValue = clueValue;
            HintColor = hintColor;
        }

        public override void BeforeExecute(Grid grid)
        {
            var cell = grid.GetCell(CellID);
            cell.SetClueColor(ClueValue, HintColor);
        }

        public override void Execute(Grid grid)
        {
            var cell = grid.GetCell(CellID);
            cell.SetClueColor(ClueValue, Color.white);
        }
    }

    public class CommandCenter
    {
        // private readonly Stack<ICommand> UndoCommands_;
        
        public CommandCenter()
        {
            // UndoCommands_ = new Stack<ICommand>();
        }

        public void Execute(Grid grid, ICommand command)
        {
            if (command == null)
            {
                return;
            }
            
            command.Execute(grid);
            // RedoCommands_.Clear();
            // UndoCommands_.Push(command);
        }

        // public void Undo()
        // {
        //     if (UndoCommands_.Count == 0)
        //     {
        //         return;
        //     }
        //
        //     var command = UndoCommands_.Pop();
        //     command.Undo();
        //     RedoCommands_.Push(command);
        // }
        //
        // public void Redo()
        // {
        //     if (RedoCommands_.Count == 0)
        //     {
        //         return;
        //     }
        //
        //     var command = RedoCommands_.Pop();
        //     command.Execute();
        //     UndoCommands_.Push(command);
        // }
    }

    public class Inference
    {
        public Cell Head { get; }
        public Cell Tail { get; }
        public bool IsStrong { get; }

        public Inference(Cell head, Cell tail, bool isStrong)
        {
            Head = head;
            Tail = tail;
            IsStrong = isStrong;
        }

        public override string ToString()
        {
            return $"R{Head.RowIndex}C{Head.ColumnIndex}{(IsStrong ? "=" : "-")}R{Tail.RowIndex}C{Tail.ColumnIndex}";
        }
        
        protected bool Equals(Inference other)
        {
            return Equals(Head, other.Head) && Equals(Tail, other.Tail) && IsStrong == other.IsStrong;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((Inference)obj);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Head, Tail, IsStrong);
        }

        public static bool operator ==(Inference left, Inference right)
        {
            if (object.Equals(left, null))
            {
                return object.Equals(right, null);
            }

            if (object.Equals(right, null))
            {
                return false;
            }
            
            return left.Head == right.Head && left.Tail == right.Tail && left.IsStrong == right.IsStrong;
        }

        public static bool operator !=(Inference left, Inference right)
        {
            return !(left == right);
        }
    }

    public class Chain
    {
        private readonly List<Inference> Links_;

        public Chain()
        {
            Links_ = new List<Inference>();
        }

        public bool AddInference(Inference inference)
        {
            if (Links_.Contains(inference))
            {
                return false;
            }

            foreach (var link in Links_)
            {
                if (link.Head == inference.Tail && link.Tail == inference.Head)
                {
                    return false;
                }
            }
            
            Links_.Add(inference);
            return true;
        }
    }

    public struct StrategyData
    {
        public static readonly StrategyData Invalid = new StrategyData(SolveStrategyType.Invalid);
        
        public SolveStrategyType StrategyType { get; }
        
        public bool IsValid => StrategyType != SolveStrategyType.Invalid && !IsExecute_ && Nodes_.Count > 0;

        private readonly List<StrategyNode> Nodes_;
        private bool IsExecute_;

        public StrategyData(SolveStrategyType strategyType)
        {
            StrategyType = strategyType;
            Nodes_ = new List<StrategyNode>();
            IsExecute_ = false;
        }

        public void AddNode(StrategyNode node)
        {
            if (!node.IsValid)
            {
                return;
            }
            
            Nodes_.Add(node);
        }
        
        public void BeforeExecute()
        {
            foreach (var node in Nodes_)
            {
                node.BeforeExecute();
            }
        }

        public void Execute()
        {
            foreach (var node in Nodes_)
            {
                node.Execute();
            }

            IsExecute_ = true;
            Nodes_.Clear();
        }
    }
    
    public struct StrategyNode
    {
        public static readonly StrategyNode Invalid = new StrategyNode(null);
        
        public bool IsValid => !IsExecute_ && GetNormalCommandCount() > 0 && Grid_ != null;
        
        private readonly Grid Grid_;
        private readonly List<ICommand> Commands_;
        private bool IsExecute_;
        
        public StrategyNode(Grid grid)
        {
            Grid_ = grid;
            Commands_ = new List<ICommand>();
            IsExecute_ = false;
        }

        private void AddCommand<T>(T command) where T : ICommand
        {
            Commands_.Add(command);
        }

        private int GetNormalCommandCount()
        {
            return Commands_.Sum(command => (command.IsAssist ? 0 : 1));
        }

        public void AddPlacedCommand(Cell cell, int value, bool autoSubSeeCellClue)
        {
            if (cell.IsPlaced())
            {
                return;
            }
            
            AddCommand(new PlacedCommand(cell.CellID, value));

            if (autoSubSeeCellClue)
            {
                var seeCells = Grid_.GetSeeCells(cell);
                foreach (var seeCell in seeCells)
                {
                    if (seeCell != cell)
                    {
                        AddSubClueCommand(seeCell, value);
                    }
                }
            }
        }

        public void AddSubClueCommand(Cell cell, int value)
        {
            if (cell.IsPlaced())
            {
                return;
            }

            if (!cell.IsClue(value))
            {
                return;
            }

            AddCommand(new SubClueCommand(cell.CellID, value));
        }

        public void AddHitColorCommand(Cell cell, int value, Color color)
        {
            if (cell.IsPlaced())
            {
                return;
            }

            if (!cell.IsClue(value))
            {
                return;
            }
            
            AddCommand(new HintColorCommand(cell.CellID, value, color));
        }

        public void BeforeExecute()
        {
            if (!IsValid)
            {
                return;
            }

            foreach (var command in Commands_)
            {
                command.BeforeExecute(Grid_);
            }
        }

        public void Execute()
        {
            if (!IsValid)
            {
                return;
            }
            
            foreach (var command in Commands_)
            {
                command.Execute(Grid_);
            }

            IsExecute_ = true;
            Commands_.Clear();
        }
    }

    public enum SolveStrategyType
    {
        Invalid,
        SolvedCells,
        NakedSingles,
        HiddenSingles,
        NakedPairs,
        NakedTriples,
        NakedQuads,
        HiddenPairs,
        HiddenTriples,
        HiddenQuads,
        PointingPairs,
        BoxLineReduction,
        XWing,
        YWing,
        XYWing,
        XYZWing,
        SimpleColouring,
        SwordFish,
        Chain,
    }

    public class SudokuSolver
    {
        private readonly Grid Grid_;
        private List<Func<StrategyData>> Solver_ = null;
        
        public SudokuSolver(Grid grid)
        {
            Grid_ = grid;
            // Grid_.GenerateFromData("004600089700934005002500006601040000827153964340000001000800003203415690410300500");
            // Grid_.GenerateFromData("965000000072365190031907600784000030009400700000708000040010500000000002008573060");
            FillClue();
        }
        
        private void FillClue()
        {
            var cells = Grid_.GetCells();
            
            foreach (var cell in cells)
            {
                if (cell.IsPlaced())
                {
                    continue;
                }

                for (var i = 1; i <= 9; ++i)
                {
                    cell.AddClue(i);
                }
            }
        }

        public StrategyData Data => StrategyData_;
        
        private StrategyData StrategyData_ = StrategyData.Invalid;
        public bool Solve()
        {
            if (Solver_ == null)
            {
                Solver_ = new List<Func<StrategyData>>
                {
                    CheckForSolvedCells,
                    NakedSingles,
                    HiddenSingles,
                    NakedPairs,
                    NakedTriples,
                    HiddenPairs,
                    // HiddenTriples,
                    PointingPairs,
                    BoxLineReduction,
                    XWing,
                    SimpleColouring,
                };
            }
            
            if (StrategyData_.IsValid)
            {
                StrategyData_.Execute();
                return true;
            }

            foreach (var strategy in Solver_)
            {
                StrategyData_ = strategy?.Invoke() ?? StrategyData.Invalid;
                if (StrategyData_.IsValid)
                {
                    StrategyData_.BeforeExecute();
                    return true;
                }
            }

            return false;
        }

        private StrategyData CheckForSolvedCells()
        {
            var data = new StrategyData(SolveStrategyType.SolvedCells);
            
            foreach (var cell in Grid_.GetCells())
            {
                if (cell.GetClueCount() == 1)
                {
                    var node = new StrategyNode(Grid_);
                    var clueValue = cell.GetClueValues()[0];
                    node.AddPlacedCommand(cell, clueValue, true);
                    data.AddNode(node);
                }
            }

            return data;
        }
        
        private StrategyData NakedSingles()
        {
            var data = new StrategyData(SolveStrategyType.NakedSingles);
            
            foreach (var cell in Grid_.GetCells())
            {
                if (cell.IsPlaced())
                {
                    var node = new StrategyNode(Grid_);
                    var seeCells = Grid_.GetSeeCells(cell);
                    foreach (var seeCell in seeCells)
                    {
                        node.AddSubClueCommand(seeCell, cell.PlaceValue);
                    }
                    data.AddNode(node);
                }
            }

            return data;
        }

        private StrategyData HiddenSingles()
        {
            var data = new StrategyData(SolveStrategyType.HiddenSingles);
            
            foreach (var cell in Grid_.GetCells())
            {
                HS_TrimWithCell(cell);
            }

            return data;

            void HS_TrimWithCell(Cell cell)
            {
                var blockCells = Grid_.GetCellsByBlock(cell.BlockIndex);
                HS_HandleCells(blockCells);

                var rowCells = Grid_.GetCellsByRow(cell.RowIndex);
                HS_HandleCells(rowCells);

                var columnCells = Grid_.GetCellsByColumn(cell.ColumnIndex);
                HS_HandleCells(columnCells);
            }

            void HS_HandleCells(Cell[] cells)
            {
                var map = new Dictionary<int, int[]>();
                for (var i = 1; i <= 9; ++i)
                {
                    var indices = cells.GetCellIndexWithClue(i);
                    map.Add(i, indices);
                }

                var node = new StrategyNode(Grid_);
                foreach (var n in map)
                {
                    if (n.Value.Length == 1)
                    {
                        node.AddPlacedCommand(cells[n.Value[0]], n.Key, true);
                    }
                }
                data.AddNode(node);
            }
        }

        private StrategyData NakedPairs()
        {
            var data = new StrategyData(SolveStrategyType.NakedPairs);
            
            foreach (var cell in Grid_.GetCells())
            {
                if (cell.GetClueCount() == 2)
                {
                    NP_TrimWithCell(cell);
                }
            }

            return data;

            void NP_TrimWithCell(Cell cell)
            {
                var seeCells = Grid_.GetSeeCells(cell);
                var pairCell = cell;

                foreach (var seeCell in seeCells)
                {
                    if (cell != seeCell && cell.Clues == seeCell.Clues)
                    {
                        pairCell = seeCell;
                        break;
                    }
                }

                if (pairCell != cell)
                {
                    var node = new StrategyNode(Grid_);
                    var deletionClues = cell.GetClueValues();
                    var intersectionCells = Grid_.GetIntersectionCells(cell, pairCell);
                    
                    foreach (var intersectionCell in intersectionCells)
                    {
                        if (intersectionCell != cell && intersectionCell != pairCell)
                        {
                            foreach (var v in deletionClues)
                            {
                                node.AddSubClueCommand(intersectionCell, v);
                            }
                        }
                    }

                    if (node.IsValid)
                    {
                        foreach (var v in deletionClues)
                        {
                            node.AddHitColorCommand(cell, v, Color.yellow);
                            node.AddHitColorCommand(pairCell, v, Color.yellow);
                        }
                        
                        data.AddNode(node);
                    }
                }
            }
        }

        private StrategyData NakedTriples()
        {
            var data = new StrategyData(SolveStrategyType.NakedTriples);
            
            foreach (var cell in Grid_.GetCells())
            {
                var clueCount = cell.GetClueCount();

                if (clueCount is > 0 and <= 3)
                {
                    NT_TrimWithCell(cell);
                }
            }

            return data;

            void NT_TrimWithCell(Cell cell)
            {
                var blockCells = Grid_.GetCellsByBlock(cell.BlockIndex);
                NT_HandleCells(blockCells);

                var rowCells = Grid_.GetCellsByRow(cell.RowIndex);
                NT_HandleCells(rowCells);

                var columnCells = Grid_.GetCellsByColumn(cell.ColumnIndex);
                NT_HandleCells(columnCells);
            }

            void NT_HandleCells(Cell[] cells)
            {
                var matchCells = cells.GetMatchCellsByClueCount(2, 3);
                if (matchCells.Length < 3)
                {
                    return;
                }

                for (var n1 = 0; n1 < matchCells.Length - 2; ++n1)
                {
                    for (var n2 = n1 + 1; n2 < matchCells.Length - 1; ++n2)
                    {
                        for (var n3 = n2 + 1; n3 < matchCells.Length; ++n3)
                        {
                            var c1 = matchCells[n1];
                            var c2 = matchCells[n2];
                            var c3 = matchCells[n3];
                            
                            if (Utils.ArrayTripleApproxClues(c1, c2, c3))
                            {
                                var node = new StrategyNode(Grid_);
                                var intersectionClues = Utils.GetIntersectionClues(c1, c2, c3);
                                
                                foreach (var cell in cells)
                                {
                                    if (cell != c1 && cell != c2 && cell != c3)
                                    {
                                        foreach (var intersectionClue in intersectionClues)
                                        {
                                            node.AddSubClueCommand(cell, intersectionClue);
                                        }
                                    }
                                }

                                if (node.IsValid)
                                {
                                    foreach (var intersectionClue in intersectionClues)
                                    {
                                        node.AddHitColorCommand(c1, intersectionClue, Color.yellow);
                                        node.AddHitColorCommand(c2, intersectionClue, Color.yellow);
                                        node.AddHitColorCommand(c3, intersectionClue, Color.yellow);
                                    }
                                    
                                    data.AddNode(node);
                                }
                            }
                        }
                    }
                }
            }
        }

        private StrategyData HiddenPairs()
        {
            var data = new StrategyData(SolveStrategyType.HiddenPairs);
            
            foreach (var cell in Grid_.GetCells())
            {
                HP_TrimWithCell(cell);
            }

            return data;

            void HP_TrimWithCell(Cell cell)
            {
                var blockCells = Grid_.GetCellsByBlock(cell.BlockIndex);
                HP_HandleCells(blockCells);

                var rowCells = Grid_.GetCellsByRow(cell.RowIndex);
                HP_HandleCells(rowCells);

                var columnCells = Grid_.GetCellsByColumn(cell.ColumnIndex);
                HP_HandleCells(columnCells);
            }

            void HP_HandleCells(Cell[] cells)
            {
                var map = new Dictionary<int, int[]>();
                for (var i = 1; i <= 9; ++i)
                {
                    var indices = cells.GetCellIndexWithClue(i);
                    map.Add(i, indices);
                }

                foreach (var n in map)
                {
                    if (n.Value.Length == 2)
                    {
                        foreach (var m in map)
                        {
                            if (n.Key != m.Key && Utils.ArrayEqual(n.Value, m.Value))
                            {
                                var node = new StrategyNode(Grid_);
                                
                                foreach (var cell in cells)
                                {
                                    if (cell == cells[n.Value[0]] || cell == cells[n.Value[1]])
                                    {
                                        for (var c = 1; c <= 9; ++c)
                                        {
                                            if (c != n.Key && c != m.Key)
                                            {
                                                node.AddSubClueCommand(cell, c);
                                            }
                                        }
                                    }
                                    else
                                    {
                                        node.AddSubClueCommand(cell, n.Key);
                                        node.AddSubClueCommand(cell, m.Key);
                                    }
                                }

                                if (node.IsValid)
                                {
                                    node.AddHitColorCommand(cells[n.Value[0]], n.Key, Color.yellow);
                                    node.AddHitColorCommand(cells[n.Value[1]], n.Key, Color.yellow);

                                    node.AddHitColorCommand(cells[n.Value[0]], m.Key, Color.yellow);
                                    node.AddHitColorCommand(cells[n.Value[1]], m.Key, Color.yellow);
                                    
                                    data.AddNode(node);
                                }
                            }
                        }
                    }
                }
            }
        }

        private StrategyData HiddenTriples()
        {
            var data = new StrategyData(SolveStrategyType.HiddenTriples);

            foreach (var cell in Grid_.GetCells())
            {
                HT_TrimWithCell(cell);
            }

            return data;

            void HT_TrimWithCell(Cell cell)
            {
                var blockCells = Grid_.GetCellsByBlock(cell.BlockIndex);
                HT_HandleCells(blockCells);

                var rowCells = Grid_.GetCellsByRow(cell.RowIndex);
                HT_HandleCells(rowCells);

                var columnCells = Grid_.GetCellsByColumn(cell.ColumnIndex);
                HT_HandleCells(columnCells);
            }

            void HT_HandleCells(Cell[] cells)
            {
                var map = new List<int[]>();
                map.Add(Array.Empty<int>());
                for (var i = 1; i <= 9; ++i)
                {
                    var indices = cells.GetCellIndexWithClue(i);
                    map.Add(indices);
                }

                for (var n1 = 0; n1 < map.Count - 2; ++n1)
                {
                    for (var n2 = n1 + 1; n2 < map.Count - 1; ++n2)
                    {
                        for (var n3 = n2 + 1; n3 < map.Count; ++n3)
                        {
                            var i1 = map[n1];
                            var i2 = map[n2];
                            var i3 = map[n3];

                            if (Utils.ArrayTripleApproxIndices(i1, i2, i3))
                            {
                                var node = new StrategyNode(Grid_);
                                var intersectionIndices = new List<int>(i1).AddUniqueRange(i2).AddUniqueRange(i3);
                                var c1 = cells[intersectionIndices[0]];
                                var c2 = cells[intersectionIndices[1]];
                                var c3 = cells[intersectionIndices[2]];
                                var intersectionClues = Utils.GetIntersectionClues(c1, c2, c3);
                                
                                foreach (var cell in cells)
                                {
                                    if (cell == c1 || cell == c2 || cell == c3)
                                    {
                                        for (var c = 1; c <= 9; ++c)
                                        {
                                            if (!intersectionClues.Contains(c))
                                            {
                                                node.AddSubClueCommand(cell, c);
                                            }
                                        }
                                    }
                                    else
                                    {
                                        foreach (var clue in intersectionClues)
                                        {
                                            node.AddSubClueCommand(cell, clue);
                                        }
                                    }
                                }
                                
                                if (node.IsValid)
                                {
                                    foreach (var intersectionClue in intersectionClues)
                                    {
                                        node.AddHitColorCommand(c1, intersectionClue, Color.yellow);
                                        node.AddHitColorCommand(c2, intersectionClue, Color.yellow);
                                        node.AddHitColorCommand(c3, intersectionClue, Color.yellow);
                                    }
                                    
                                    data.AddNode(node);
                                }
                            }
                        }
                    }
                }
            }
        }

        private StrategyData PointingPairs()
        {
            var data = new StrategyData(SolveStrategyType.PointingPairs);

            for (var i = 0; i < 9; ++i)
            {
                var blockCells = Grid_.GetCellsByBlock(i);
                PP_HandleCells(blockCells);
            }

            return data;

            void PP_HandleCells(Cell[] cells)
            {
                for (var clue = 1; clue <= 9; ++clue)
                {
                    var matchCells = cells.GetMatchCellsByClue(clue);
                    if (matchCells.Length is < 2 or > 3)
                    {
                        continue;
                    }

                    var node = new StrategyNode(Grid_);

                    if (matchCells.CellsInSameRow())
                    {
                        var seeCells = Grid_.GetCellsByRow(matchCells[0].RowIndex);
                        foreach (var seeCell in seeCells)
                        {
                            if (!matchCells.Contains(seeCell))
                            {
                                node.AddSubClueCommand(seeCell, clue);
                            }
                        }
                    }
                    else if (matchCells.CellsInSameColumn())
                    {
                        var seeCells = Grid_.GetCellsByColumn(matchCells[0].ColumnIndex);
                        foreach (var seeCell in seeCells)
                        {
                            if (!matchCells.Contains(seeCell))
                            {
                                node.AddSubClueCommand(seeCell, clue);
                            }
                        }
                    }

                    if (node.IsValid)
                    {
                        foreach (var cell in matchCells)
                        {
                            node.AddHitColorCommand(cell, clue, Color.yellow);
                        }
                        
                        data.AddNode(node);
                    }
                }
            }
        }

        private StrategyData BoxLineReduction()
        {
            var data = new StrategyData(SolveStrategyType.BoxLineReduction);

            for (var i = 0; i < 9; ++i)
            {
                var rowCells = Grid_.GetCellsByRow(i);
                BLR_HandleCells(rowCells);

                var columnCells = Grid_.GetCellsByColumn(i);
                BLR_HandleCells(columnCells);
            }

            return data;

            void BLR_HandleCells(Cell[] cells)
            {
                for (var clue = 1; clue <= 9; ++clue)
                {
                    var matchCells = cells.GetMatchCellsByClue(clue);
                    if (matchCells.Length is < 2 or > 3)
                    {
                        continue;
                    }

                    var node = new StrategyNode(Grid_);

                    if (matchCells.CellsInSameBlock())
                    {
                        var seeCells = Grid_.GetCellsByBlock(matchCells[0].BlockIndex);
                        foreach (var seeCell in seeCells)
                        {
                            if (!matchCells.Contains(seeCell))
                            {
                                node.AddSubClueCommand(seeCell, clue);
                            }
                        }
                    }

                    if (node.IsValid)
                    {
                        foreach (var cell in matchCells)
                        {
                            node.AddHitColorCommand(cell, clue, Color.yellow);
                        }
                        
                        data.AddNode(node);
                    }
                }
            }
        }

        private StrategyData XWing()
        {
            var data = new StrategyData(SolveStrategyType.XWing);

            for (var row = 0; row < 8; ++row)
            {
                var rowCells = Grid_.GetCellsByRow(row);
                XW_HandleRowCells(rowCells);
            }

            if (data.IsValid)
            {
                return data;
            }

            for (var column = 0; column < 8; ++column)
            {
                var columnCells = Grid_.GetCellsByColumn(column);
                XW_HandleColumnCells(columnCells);
            }
            
            return data;
            
            void XW_HandleRowCells(Cell[] cells)
            {
                for (var clue = 1; clue <= 9; ++clue)
                {
                    var matchCells = cells.GetMatchCellsByClue(clue);
                    if (matchCells.Length != 2 || matchCells[0].BlockIndex == matchCells[1].BlockIndex)
                    {
                        continue;
                    }

                    for (var i = 0; i < 9; ++i)
                    {
                        if (i == matchCells[0].RowIndex)
                        {
                            continue;
                        }

                        var rowCells2 = Grid_.GetCellsByRow(i);
                        var matchCells2 = rowCells2.GetMatchCellsByClue(clue);
                        if (matchCells2.Length != 2)
                        {
                            continue;
                        }

                        if (matchCells[0].ColumnIndex == matchCells2[0].ColumnIndex && matchCells[1].ColumnIndex == matchCells2[1].ColumnIndex)
                        {
                            var node = new StrategyNode(Grid_);
                            
                            var cells1 = Grid_.GetCellsByColumn(matchCells[0].ColumnIndex);
                            foreach (var cell in cells1)
                            {
                                if (cell != matchCells[0] && cell != matchCells2[0])
                                {
                                    node.AddSubClueCommand(cell, clue);
                                }
                            }
                            
                            var cells2 = Grid_.GetCellsByColumn(matchCells[1].ColumnIndex);
                            foreach (var cell in cells2)
                            {
                                if (cell != matchCells[1] && cell != matchCells2[1])
                                {
                                    node.AddSubClueCommand(cell, clue);
                                }
                            }

                            if (node.IsValid)
                            {
                                node.AddHitColorCommand(matchCells[0], clue, Color.yellow);
                                node.AddHitColorCommand(matchCells[1], clue, Color.yellow);
                                node.AddHitColorCommand(matchCells2[0], clue, Color.yellow);
                                node.AddHitColorCommand(matchCells2[1], clue, Color.yellow);
                                
                                data.AddNode(node);
                            }
                        }
                    }
                }
            }
            
            void XW_HandleColumnCells(Cell[] cells)
            {
                for (var clue = 1; clue <= 9; ++clue)
                {
                    var matchCells = cells.GetMatchCellsByClue(clue);
                    if (matchCells.Length != 2 || matchCells[0].BlockIndex == matchCells[1].BlockIndex)
                    {
                        continue;
                    }

                    for (var i = 0; i < 9; ++i)
                    {
                        if (i == matchCells[0].ColumnIndex)
                        {
                            continue;
                        }

                        var columnCells2 = Grid_.GetCellsByColumn(i);
                        var matchCells2 = columnCells2.GetMatchCellsByClue(clue);
                        if (matchCells2.Length != 2)
                        {
                            continue;
                        }

                        if (matchCells[0].RowIndex == matchCells2[0].RowIndex && matchCells[1].RowIndex == matchCells2[1].RowIndex)
                        {
                            var node = new StrategyNode(Grid_);
                            
                            var cells1 = Grid_.GetCellsByRow(matchCells[0].RowIndex);
                            foreach (var cell in cells1)
                            {
                                if (cell != matchCells[0] && cell != matchCells2[0])
                                {
                                    node.AddSubClueCommand(cell, clue);
                                }
                            }
                            
                            var cells2 = Grid_.GetCellsByRow(matchCells[1].RowIndex);
                            foreach (var cell in cells2)
                            {
                                if (cell != matchCells[1] && cell != matchCells2[1])
                                {
                                    node.AddSubClueCommand(cell, clue);
                                }
                            }

                            if (node.IsValid)
                            {
                                node.AddHitColorCommand(matchCells[0], clue, Color.yellow);
                                node.AddHitColorCommand(matchCells[1], clue, Color.yellow);
                                node.AddHitColorCommand(matchCells2[0], clue, Color.yellow);
                                node.AddHitColorCommand(matchCells2[1], clue, Color.yellow);
                                
                                data.AddNode(node);
                            }
                        }
                    }
                }
            }
        }

        private StrategyData SimpleColouring()
        {
            var data = new StrategyData(SolveStrategyType.SimpleColouring);
            
            SC_TrimWithClue(1);
            
            return data;

            void SC_TrimWithClue(int clue)
            {
                var result = new List<Inference>();

                void AddTwoWayInference(Cell a, Cell b, bool isStrong)
                {
                    result.AddUnique(new Inference(a, b, isStrong));
                    result.AddUnique(new Inference(b, a, isStrong));
                }

                void AddInferenceByCells(Cell[] cells)
                {
                    var matchCells = cells.GetMatchCellsByClue(clue);
                    if (matchCells.Length > 1)
                    {
                        for (var i = 0; i < matchCells.Length - 1; ++i)
                        {
                            for (var j = i + 1; j < matchCells.Length; ++j)
                            {
                                AddTwoWayInference(matchCells[i], matchCells[j], matchCells.Length == 2);
                            }
                        }
                    }
                }
                
                for (var blockIndex = 0; blockIndex < 9; ++blockIndex)
                {
                    var blockCells = Grid_.GetCellsByBlock(blockIndex);
                    AddInferenceByCells(blockCells);
                }
                
                for (var rowIndex = 0; rowIndex < 9; ++rowIndex)
                {
                    var rowCells = Grid_.GetCellsByRow(rowIndex);
                    AddInferenceByCells(rowCells);
                }

                for (var columnIndex = 0; columnIndex < 9; ++columnIndex)
                {
                    var columnCells = Grid_.GetCellsByColumn(columnIndex);
                    AddInferenceByCells(columnCells);
                }

                List<Inference> FindInferencesInResult(Cell head)
                {
                    var inferences = new List<Inference>();

                    // foreach (var n in result)
                    // {
                    //     if (n.Head == head)
                    //     {
                    //         inferences.AddUnique(n);
                    //     }
                    // }

                    for (var i = 0; i < result.Count; ++i)
                    {
                        if (result[i].Head == head)
                        {
                            inferences.AddUnique(result[i]);
                        }
                    }

                    return inferences;
                }

                Chain GenerateChain(Chain chain, Inference from)
                {
                    if (!chain.AddInference(from))
                    {
                        return chain;
                    }

                    var nextList = FindInferencesInResult(from.Tail);
                    foreach (var next in nextList)
                    {
                        GenerateChain(chain, next);
                    }

                    return chain;
                }
                
                var map = new Dictionary<Cell, Chain>();
                foreach (var inference in result)
                {
                    if (!map.ContainsKey(inference.Head))
                    {
                        var chain = GenerateChain(new Chain(), inference);
                        map.Add(inference.Head, chain);
                    }
                }
            }
        }
    }
}
#endif