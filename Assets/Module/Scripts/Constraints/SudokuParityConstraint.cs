using System.Collections.Generic;

public class SudokuParityConstraintFactory : SudokuConstraintFactory
{
    public IEnumerable<SudokuConstraint> GetConstraints(int[] grid)
    {
        List<SudokuConstraint> constraints = new List<SudokuConstraint>();

        for (int i = 0; i < grid.Length; i++)
            constraints.Add(new SudokuParityConstraint(i, (grid[i] + 1) % 2 == 0));

        return constraints;
    }
}

public class SudokuParityConstraint : SudokuConstraint
{
    private int _index;
    private bool _even;

    public SudokuParityConstraint(int index, bool even)
    {
        _index = index;
        _even = even;
    }

    public IEnumerable<IEnumerable<SudokuConstraint>> GetReductions()
    {
        return new List<IEnumerable<SudokuConstraint>> { new List<SudokuConstraint>() };
    }

    public bool Reduce(SudokuCellOption[] grid)
    {
        bool didReduce = false;

        SudokuCellOption option = grid[_index];

        for (int i = 0; i < 6; i++)
        {
            if (((i + 1) % 2 == 0) == _even)
                continue;

            if (!option.Options[i])
                continue;

            option.Eliminate(i);
            didReduce |= true;
        }

        return didReduce;
    }

    public override string ToString()
    {
        return "R" + (_index / 6 + 1) + "C" + (_index % 6 + 1) + "=" + (_even ? "E" : "O");
    }
}
