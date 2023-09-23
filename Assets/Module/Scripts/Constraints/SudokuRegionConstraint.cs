using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class SudokuRowConstraintFactory : SudokuBaseConstraintFactory
{
    public IEnumerable<SudokuConstraint> GetConstraints()
    {
        List<SudokuConstraint> constraints = new List<SudokuConstraint>();

        for (int i = 0; i < 6; i++)
            constraints.Add(new SudokuRegionConstraint(Enumerable.Range(0, 6).Select(x => x + i * 6).ToArray()));

        return constraints;
    }
}

public class SudokuColumnConstraintFactory : SudokuBaseConstraintFactory
{
    public IEnumerable<SudokuConstraint> GetConstraints()
    {
        List<SudokuConstraint> constraints = new List<SudokuConstraint>();

        for (int i = 0; i < 6; i++)
            constraints.Add(new SudokuRegionConstraint(Enumerable.Range(0, 6).Select(x => x * 6 + i).ToArray()));

        return constraints;
    }
}

public class SudokuBoxConstraintFactory : SudokuBaseConstraintFactory
{
    public IEnumerable<SudokuConstraint> GetConstraints()
    {
        List<SudokuConstraint> constraints = new List<SudokuConstraint>();

        for (int i = 0; i < 6; i++)
            constraints.Add(new SudokuRegionConstraint(Enumerable.Range(0, 6).Select(x => (x % 3 + 3 * (i % 2)) + (2 * (i / 2) + x / 3) * 6).ToArray()));

        return constraints;
    }
}

public class SudokuRegionConstraint : SudokuConstraint
{
    private int[] _indices;
    public SudokuRegionConstraint(int[] indices)
    {
        _indices = indices;
    }

    public IEnumerable<IEnumerable<SudokuConstraint>> GetReductions()
    {
        return null;
    }

    public bool Reduce(SudokuCellOption[] grid)
    {
        bool didReduce = false;

        bool tempDidReduce;

        do
        {
            tempDidReduce = false;

            SudokuCellOption[] thisRegion = _indices.Select(x => grid[x]).ToArray();

            for (int i = 0; i < thisRegion.Length; i++)
            {
                if (thisRegion[i].Entropy() != 1)
                    continue;

                int value = Enumerable.Range(0, 6).IndexOf(x => thisRegion[i].Options[x]);

                for (int j = 0; j < thisRegion.Length; j++)
                {
                    if (i == j)
                        continue;

                    if (thisRegion[j].Options[value])
                    {
                        tempDidReduce |= true;
                        didReduce |= true;
                        thisRegion[j].Eliminate(value);
                    }
                }
            }

            for (int i = 0; i < 6; i++)
            {
                if (thisRegion.Count(x => x.Options[i]) != 1)
                    continue;

                int index = Enumerable.Range(0, 6).IndexOf(x => thisRegion[x].Options[i]);

                if (thisRegion[index].Entropy() == 1)
                    continue;

                tempDidReduce |= true;
                didReduce |= true;
                for (int j = 0; j < 6; j++)
                {
                    if (i == j)
                        continue;

                    thisRegion[index].Eliminate(j);
                }
            }
        } while (tempDidReduce);

        return didReduce;
    }
}
