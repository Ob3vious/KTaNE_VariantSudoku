using System;
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

            constraints.Add(new SudokuQuadruplesConstraint(i, shifts.Select(x => grid[i + x]).OrderBy(x => x).ToArray()));
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
        return _values.Distinct().Select(x => Array.IndexOf(_values, x)).Select(x => (IEnumerable<SudokuConstraint>)new SudokuConstraint[] { new SudokuQuadruplesConstraint(_index, _values.Take(x).Concat(_values.Skip(x + 1)).ToArray()) });
    }

    public bool Reduce(SudokuCellOption[] grid)
    {
        int[] shifts = new int[] { 0, 1, 6, 7 };

        SudokuCellOption[] quadruplet = shifts.Select(x => grid[_index + x]).ToArray();

        bool didReduce = false;

        bool tempDidReduce;

        IEnumerable<int> distinctValues = _values.Distinct();

        do
        {
            tempDidReduce = false;

            foreach (int value in distinctValues)
            {
                if (quadruplet.Count(x => x.Options[value]) < _values.Count(x => x == value))
                {
                    for (int i = 0; i < 4; i++)
                        for (int j = 0; j < 6; j++)
                        {
                            didReduce |= quadruplet[i].Options[j];
                            quadruplet[i].Eliminate(j);
                        }
                    return didReduce;
                }


                if (quadruplet.Count(x => x.Options[value]) != _values.Count(x => x == value))
                    continue;

                IEnumerable<int> indices = Enumerable.Range(0, 4).Where(x => quadruplet[x].Options[value]);

                foreach (int index in indices)
                {
                    if (quadruplet[index].Entropy() == 1 && quadruplet[index].Options[value])
                        continue;

                    tempDidReduce |= true;
                    didReduce |= true;
                    for (int i = 0; i < 6; i++)
                    {
                        if (i == value)
                            continue;

                        quadruplet[index].Eliminate(i);
                    }
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