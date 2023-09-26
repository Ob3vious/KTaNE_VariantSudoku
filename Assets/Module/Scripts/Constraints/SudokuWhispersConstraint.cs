using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SudokuWhispersConstraintFactory : SudokuConstraintFactory
{
    private System.Random _random;
    public SudokuWhispersConstraintFactory(System.Random random)
    {
        _random = random;
    }

    public IEnumerable<SudokuConstraint> GetConstraints(int[] grid)
    {
        List<SudokuWhispersConstraint> constraints = new List<SudokuWhispersConstraint>();

        for (int i = 0; i < grid.Length; i++)
        {
            //one horizontal and one vertical step respectively
            int[] hshifts = new int[] { 1, -1, 0, 1 };
            int[] vshifts = new int[] { 0, 1, 1, 1 };

            for (int j = 0; j < 4; j++)
            {
                if (i % 6 + hshifts[j] >= 6 || i % 6 + hshifts[j] < 0 || i / 6 + vshifts[j] >= 6)
                    continue;

                int otherIndex = i + hshifts[j] + 6 * vshifts[j];

                if (Mathf.Abs(grid[i] - grid[otherIndex]) < 3)
                    continue;

                constraints.Add(new SudokuWhispersConstraint(new List<int> { i, otherIndex }));
            }
        }

        //return constraints.Select(x => (SudokuConstraint)x);

        Shuffle(constraints);

        Queue<SudokuWhispersConstraint> usable = new Queue<SudokuWhispersConstraint>();

        foreach (SudokuWhispersConstraint constraint in constraints)
            usable.Enqueue(constraint);

        List<SudokuConstraint> merged = new List<SudokuConstraint>();

        List<SudokuWhispersConstraint> toRemove = new List<SudokuWhispersConstraint>();


        while (usable.Count > 0)
        {
            SudokuWhispersConstraint currentConstraint = usable.Dequeue();

            if (toRemove.Contains(currentConstraint))
            {
                toRemove.Remove(currentConstraint);
                continue;
            }

            if (merged.Any(x => ((SudokuWhispersConstraint)x).BadCrossing(currentConstraint)))
                continue;

            Queue<SudokuWhispersConstraint> mergeQueue = new Queue<SudokuWhispersConstraint>(usable);

            bool hasMerged = false;

            while (mergeQueue.Count > 0)
            {
                SudokuWhispersConstraint comparison = mergeQueue.Dequeue();

                if (toRemove.Contains(comparison))
                    continue;

                SudokuWhispersConstraint merge = currentConstraint.Merge(comparison);
                if (merge == null)
                    continue;

                toRemove.Add(comparison);

                hasMerged = true;
                usable.Enqueue(merge);
                break;
            }

            if (hasMerged)
                continue;

            merged.Add(currentConstraint);
        }

        return merged;
    }

    //TODO make a better random source
    private List<T> Shuffle<T>(List<T> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int swapIdx = _random.Next(i, list.Count);
            T temp = list[swapIdx];
            list[swapIdx] = list[i];
            list[i] = temp;
        }
        return list;
    }
}

public class SudokuWhispersConstraint : SudokuConstraint
{
    private List<int> _indices;

    public SudokuWhispersConstraint(List<int> indices)
    {
        _indices = indices;
    }

    public IEnumerable<IEnumerable<SudokuConstraint>> GetReductions()
    {
        if (_indices.Count == 2)
            return new List<IEnumerable<SudokuConstraint>> { new List<SudokuConstraint>() };

        List<IEnumerable<SudokuConstraint>> reductions = new List<IEnumerable<SudokuConstraint>>
        {
            new List<SudokuConstraint> { new SudokuWhispersConstraint(_indices.Skip(1).ToList()) },
            new List<SudokuConstraint> { new SudokuWhispersConstraint(_indices.Take(_indices.Count - 1).ToList()) }
        };

        for (int i = 2; i <= _indices.Count - 2; i++)
            reductions.Add(new List<SudokuConstraint> { new SudokuWhispersConstraint(_indices.Take(i).ToList()), new SudokuWhispersConstraint(_indices.Skip(i).ToList()) });

        return reductions;
    }

    public bool Reduce(SudokuCellOption[] grid)
    {
        bool didReduce = false;

        List<SudokuCellOption> cells = _indices.Select(x => grid[x]).ToList();

        //Debug.Log(cells.Select(x => x.Options.Select(y => y ? 1 : 0).Join("")).Join(","));

        bool hasFlipped = false;

        while (true)
        {
            for (int i = 0; i < cells.Count - 1; i++)
            {
                if (cells[i].Entropy() == 0)
                    return didReduce;

                int lowerBound = cells[i].Options.IndexOf(x => x) + 3;
                int upperBound = 2 - cells[i].Options.Reverse().IndexOf(x => x);

                //Debug.Log(cells[i].Options.Select(y => y ? 1 : 0).Join("") + "; L:" + lowerBound + "; U:" + upperBound);

                for (int j = 0; j < 6; j++)
                {
                    if (j >= lowerBound || j <= upperBound)
                        continue;

                    if (!cells[i + 1].Options[j])
                        continue;

                    didReduce |= true;
                    cells[i + 1].Eliminate(j);
                }

                //Debug.Log(cells[i].Options.Select(y => y ? 1 : 0).Join("") + "; L:" + lowerBound + "; U:" + upperBound + " => " + cells[i + 1].Options.Select(y => y ? 1 : 0).Join(""));
            }

            if (hasFlipped)
                break;

            hasFlipped = true;
            cells.Reverse();
        }

        //Debug.Log(cells.Select(x => x.Options.Select(y => y ? 1 : 0).Join("")).Reverse().Join(","));

        //Debug.Log(didReduce + " ... " + this);

        return didReduce;
    }

    public bool BadCrossing(SudokuWhispersConstraint other)
    {
        //check all against middle of this
        if (_indices.Skip(1).Take(_indices.Count - 2).Any(x => other._indices.Contains(x)))
            return true;
        //check outer of this against middle of other
        if (other._indices.Skip(1).Take(other._indices.Count - 2).Any(x => x == _indices.First() || x == _indices.Last()))
            return true;

        //only possible if no duplicates or either end matches
        return false;
    }

    public SudokuWhispersConstraint Merge(SudokuWhispersConstraint other)
    {
        //no loops please
        if (_indices.First() == other._indices.First() && _indices.Last() == other._indices.Last())
            return null;
        if (_indices.First() == other._indices.Last() && _indices.Last() == other._indices.First())
            return null;

        if (_indices.First() == other._indices.Last())
        {
            //Debug.Log(_indices.Join("-") + " + " + other._indices.Join("-") + "=>" + other._indices.Concat(_indices.Skip(1)).Join("-"));
            return new SudokuWhispersConstraint(other._indices.Concat(_indices.Skip(1)).ToList());
        }
        if (other._indices.First() == _indices.Last())
        {
            //Debug.Log(_indices.Join("-") + " + " + other._indices.Join("-") + "=>" + _indices.Concat(other._indices.Skip(1)).Join("-"));
            return new SudokuWhispersConstraint(_indices.Concat(other._indices.Skip(1)).ToList());
        }
        if (_indices.First() == other._indices.First())
        {
            //Debug.Log(_indices.Join("-") + " + " + other._indices.Join("-") + "=>" + other._indices.Skip(1).Reverse().Concat(_indices).Join("-"));
            return new SudokuWhispersConstraint(other._indices.Skip(1).Reverse().Concat(_indices).ToList());
        }
        if (other._indices.Last() == _indices.Last())
        {
            //Debug.Log(_indices.Join("-") + " + " + other._indices.Join("-") + "=>" + _indices.Concat(other._indices.Take(other._indices.Count - 1).Reverse()).Join("-"));
            return new SudokuWhispersConstraint(_indices.Concat(other._indices.Take(other._indices.Count - 1).Reverse()).ToList());
        }
        return null;
    }

    public override string ToString()
    {
        return _indices.Select(x => "R" + (x / 6 + 1) + "C" + (x % 6 + 1)).Join("-");
    }
}