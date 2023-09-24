using System.Collections.Generic;
using System.Linq;

public class SudokuThermoConstraintFactory : SudokuConstraintFactory
{
    private System.Random _random;
    public SudokuThermoConstraintFactory(System.Random random)
    {
        _random = random;
    }

    public IEnumerable<SudokuConstraint> GetConstraints(int[] grid)
    {
        List<SudokuThermoConstraint> constraints = new List<SudokuThermoConstraint>();

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

                if (grid[i] == grid[otherIndex])
                    continue;

                if (grid[i] < grid[otherIndex])
                    constraints.Add(new SudokuThermoConstraint(new List<int> { i, otherIndex }));
                else
                    constraints.Add(new SudokuThermoConstraint(new List<int> { otherIndex, i }));
            }
        }

        Shuffle(constraints);

        Queue<SudokuThermoConstraint> usable = new Queue<SudokuThermoConstraint>();

        foreach (SudokuThermoConstraint constraint in constraints)
        {
            if (usable.Any(x => x.BadCrossing(constraint)))
                continue;

            usable.Enqueue(constraint);
        }

        List<SudokuConstraint> merged = new List<SudokuConstraint>();

        List<SudokuThermoConstraint> toRemove = new List<SudokuThermoConstraint>();


        while (usable.Count > 0)
        {
            SudokuThermoConstraint currentConstraint = usable.Dequeue();

            if (toRemove.Contains(currentConstraint))
            {
                toRemove.Remove(currentConstraint);
                continue;
            }

            Queue<SudokuThermoConstraint> mergeQueue = new Queue<SudokuThermoConstraint>(usable);

            bool hasMerged = false;

            while (mergeQueue.Count > 0)
            {
                SudokuThermoConstraint comparison = mergeQueue.Dequeue();

                if (toRemove.Contains(comparison))
                    continue;

                SudokuThermoConstraint merge = currentConstraint.Merge(comparison);
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

public class SudokuThermoConstraint : SudokuConstraint
{
    private List<int> _indices;

    public SudokuThermoConstraint(List<int> indices)
    {
        _indices = indices;
    }

    public IEnumerable<IEnumerable<SudokuConstraint>> GetReductions()
    {
        if (_indices.Count == 2)
            return new List<IEnumerable<SudokuConstraint>> { new List<SudokuConstraint>() };

        List<IEnumerable<SudokuConstraint>> reductions = new List<IEnumerable<SudokuConstraint>>
        {
            new List<SudokuConstraint> { new SudokuThermoConstraint(_indices.Skip(1).ToList()) },
            new List<SudokuConstraint> { new SudokuThermoConstraint(_indices.Take(_indices.Count - 1).ToList()) }
        };

        for (int i = 2; i <= _indices.Count - 2; i++)
            reductions.Add(new List<SudokuConstraint> { new SudokuThermoConstraint(_indices.Take(i).ToList()), new SudokuThermoConstraint(_indices.Skip(i).ToList()) });

        return reductions;
    }

    public bool Reduce(SudokuCellOption[] grid)
    {
        bool didReduce = false;

        List<SudokuCellOption> cells = _indices.Select(x => grid[x]).ToList();

        for (int i = 0; i < cells.Count - 1; i++)
        {
            if (cells[i].Entropy() == 0)
                return didReduce;

            int lowerBound = cells[i].Options.IndexOf(x => x) + 1;

            for (int j = 0; j < lowerBound; j++)
            {
                if (!cells[i + 1].Options[j])
                    continue;

                didReduce |= true;
                cells[i + 1].Eliminate(j);
            }
        }

        for (int i = cells.Count - 1; i > 0; i--)
        {
            if (cells[i].Entropy() == 0)
                return didReduce;

            int upperBound = 4 - cells[i].Options.Reverse().IndexOf(x => x);

            for (int j = 5; j > upperBound; j--)
            {
                if (!cells[i - 1].Options[j])
                    continue;

                didReduce |= true;
                cells[i - 1].Eliminate(j);
            }
        }

        return didReduce;
    }

    public bool BadCrossing(SudokuThermoConstraint other)
    {
        //check all against middle of this
        if (_indices.Skip(1).Take(_indices.Count - 2).Any(x => other._indices.Contains(x)))
            return true;
        //check outer of this against middle of other
        if (other._indices.Skip(1).Take(other._indices.Count - 2).Any(x => x == _indices.First() || x == _indices.Last()))
            return true;
        //check if outer ones match
        if (other._indices.First() == _indices.First() || other._indices.Last() == _indices.Last())
            return true;

        //only possible if no duplicates or the last of one is the first of the other
        return false;
    }

    public SudokuThermoConstraint Merge(SudokuThermoConstraint other)
    {

        if (_indices.First() == other._indices.Last())
            return new SudokuThermoConstraint(other._indices.Concat(_indices.Skip(1)).ToList());
        if (other._indices.First() == _indices.Last())
            return new SudokuThermoConstraint(_indices.Concat(other._indices.Skip(1)).ToList());
        return null;
    }

    public override string ToString()
    {
        return _indices.Select(x => "R" + (x / 6 + 1) + "C" + (x % 6 + 1)).Join("<");
    }
}