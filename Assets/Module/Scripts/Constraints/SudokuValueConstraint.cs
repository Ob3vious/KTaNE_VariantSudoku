using System.Collections.Generic;
using System.Linq;

public class SudokuValueConstraintFactory : SudokuConstraintFactory
{
    public IEnumerable<SudokuConstraint> GetConstraints(int[] grid)
    {
        List<SudokuConstraint> constraints = new List<SudokuConstraint>();

        for (int i = 0; i < grid.Length; i++)
            constraints.Add(new SudokuValueConstraint(i, grid[i]));

        return constraints;
    }
}

public class SudokuValueConstraint : SudokuConstraint
{
    private int _index;
    private int _value;

    public SudokuValueConstraint(int index, int value)
    {
        _index = index;
        _value = value;
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
            if (i == _value)
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
        return "R" + (_index / 6 + 1) + "C" + (_index % 6 + 1) + "=" + (_value + 1);
    }
}
