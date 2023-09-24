using System.Collections.Generic;
using System.Linq;

public class SudokuKropkiConstraintFactory : SudokuConstraintFactory
{
    private System.Random _random;
    public SudokuKropkiConstraintFactory(System.Random random)
    {
        _random = random;
    }

    public IEnumerable<SudokuConstraint> GetConstraints(int[] grid)
    {
        List<SudokuConstraint> constraints = new List<SudokuConstraint>();

        for (int i = 0; i < grid.Length; i++)
        {
            //one horizontal and one vertical step respectively
            int[] shifts = new int[] { 1, 6 };
            for (int j = 0; j < 2; j++)
            {
                if (i % 6 + shifts[j] % 6 >= 6 || i / 6 + shifts[j] / 6 >= 6)
                    continue;

                bool mayBlack = SudokuKropkiConstraint.HasBlackKropkiRelation(grid[i] + 1, grid[i + shifts[j]] + 1);
                bool mayWhite = SudokuKropkiConstraint.HasWhiteKropkiRelation(grid[i] + 1, grid[i + shifts[j]] + 1);

                if (!mayBlack && !mayWhite)
                    continue;

                bool choiceBlack = mayBlack;
                if (mayBlack && mayWhite)
                {
                    choiceBlack = _random.Next(2) == 0;
                }

                constraints.Add(new SudokuKropkiConstraint(i, j == 1, choiceBlack));
            }
        }

        return constraints;
    }
}

public class SudokuKropkiConstraint : SudokuConstraint
{
    private int _index;
    private bool _isVertical;
    private bool _isBlack;

    public SudokuKropkiConstraint(int index, bool isVertical, bool isBlack)
    {
        _index = index;
        _isVertical = isVertical;
        _isBlack = isBlack;
    }

    public IEnumerable<IEnumerable<SudokuConstraint>> GetReductions()
    {
        return new List<IEnumerable<SudokuConstraint>> { new List<SudokuConstraint>() };
    }

    public bool Reduce(SudokuCellOption[] grid)
    {
        bool didReduce = false;

        int altIndex = _index + (_isVertical ? 6 : 1);

        SudokuCellOption option1 = grid[_index];
        SudokuCellOption option2 = grid[altIndex];

        bool firstTime = true;
        bool needsSwap = true;

        while (true)
        {
            needsSwap = firstTime;
            firstTime = false;
            for (int i = 0; i < 6; i++)
            {
                if (!option1.Options[i])
                    continue;

                if (Enumerable.Range(0, 6).Any(x => option2.Options[x] && (_isBlack ? HasBlackKropkiRelation(i + 1, x + 1) : HasWhiteKropkiRelation(i + 1, x + 1))))
                    continue;

                option1.Eliminate(i);
                didReduce |= true;
                needsSwap |= true;
            }

            if (needsSwap)
            {
                SudokuCellOption swap = option1;
                option1 = option2;
                option2 = swap;

                continue;
            }

            break;
        }

        return didReduce;
    }

    public override string ToString()
    {
        string coordinate;
        if (_isVertical)
            coordinate = "R" + (_index / 6 + 1) + (_index / 6 + 2) + "C" + (_index % 6 + 1);
        else
            coordinate = "R" + (_index / 6 + 1) + "C" + (_index % 6 + 1) + (_index % 6 + 2);
        return coordinate + "=" + (_isBlack ? "K" : "W");
    }

    public static bool HasBlackKropkiRelation(int a, int b)
    {
        return a * 2 == b || b * 2 == a;
    }

    public static bool HasWhiteKropkiRelation(int a, int b)
    {
        return a - b == 1 || b - a == 1;
    }
}