using System.Linq;

public class SudokuCellOption
{
    public bool[] Options { get; private set; }

    public SudokuCellOption()
    {
        Options = Enumerable.Repeat(true, 6).ToArray();
    }

    public SudokuCellOption(SudokuCellOption old)
    {
        Options = old.Options.Select(x => x).ToArray();
    }

    public int Entropy()
    {
        return Options.Count(x => x);
    }

    public void Eliminate(int value)
    {
        Options[value] = false;
    }
}
