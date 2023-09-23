using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SudokuQuadruplesConstraintFactory : SudokuConstraintFactory
{
    public IEnumerable<SudokuConstraint> GetConstraints(int[] grid)
    {
        List<SudokuConstraint> constraints = new List<SudokuConstraint>();

        for (int i = 0; i < grid.Length; i++)
        {
            if (i % 6 >= 5 || i / 6 >= 5)
                continue;

            //one horizontal and one vertical step respectively
            int[] shifts = new int[] { 0, 1, 6, 7 };

            constraints.Add(new SudokuQuadruplesConstraint(i, shifts.Select(x => grid[i + x]).Distinct().OrderBy(x => x).ToArray()));
        }

        return constraints;
    }
}

public class SudokuQuadruplesConstraint : SudokuConstraint
{
    private int _index;
    private int[] _values;

    public SudokuQuadruplesConstraint(int index, int[] values)
    {
        _index = index;
        _values = values;
    }

    public IEnumerable<IEnumerable<SudokuConstraint>> GetReductions()
    {
        if (_values.Length == 1)
            return new List<IEnumerable<SudokuConstraint>> { new List<SudokuConstraint>() };
        return _values.Select(x => (IEnumerable<SudokuConstraint>)new SudokuConstraint[] { new SudokuQuadruplesConstraint(_index, _values.Except(new int[] { x }).ToArray()) });
    }

    public bool Reduce(SudokuCellOption[] grid)
    {
        int[] shifts = new int[] { 0, 1, 6, 7 };

        SudokuCellOption[] quadruplet = shifts.Select(x => grid[_index + x]).ToArray();

        bool didReduce = false;

        bool tempDidReduce;

        do
        {
            tempDidReduce = false;

            for (int i = 0; i < _values.Length; i++)
            {
                int value = _values[i];

                if (quadruplet.Count(x => x.Options[value]) < 1)
                {
                    for (int j = 0; j < 4; j++)
                        for (int k = 0; k < 6; k++)
                        {
                            didReduce |= quadruplet[j].Options[k];
                            quadruplet[j].Eliminate(k);
                        }
                    return didReduce;
                }


                if (quadruplet.Count(x => x.Options[value]) != 1)
                    continue;

                int index = Enumerable.Range(0, 4).IndexOf(x => quadruplet[x].Options[value]);

                if (quadruplet[index].Entropy() == 1 && quadruplet[index].Options[value])
                    continue;

                tempDidReduce |= true;
                didReduce |= true;
                for (int j = 0; j < 6; j++)
                {
                    if (j == value)
                        continue;

                    quadruplet[index].Eliminate(j);
                }
            }
        } while (tempDidReduce);

        return didReduce;
    }

    public override string ToString()
    {
        return "R" + (_index / 6 + 1) + (_index / 6 + 2) + "C" + (_index % 6 + 1) + (_index % 6 + 2) + "=" + "[" + _values.Select(x => x + 1).Join("") + "]";
    }
}