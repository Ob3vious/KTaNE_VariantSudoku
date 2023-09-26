using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SudokuPuzzle
{
    private SudokuCellOption[] _grid;
    private List<SudokuConstraint> _baseConstraints;
    private List<SudokuConstraint> _extraConstraints;
    private System.Random _random;

    public SudokuPuzzle(System.Random random)
    {
        _grid = Enumerable.Range(0, 36).Select(_ => new SudokuCellOption()).ToArray();
        _baseConstraints = new List<SudokuConstraint>();
        _extraConstraints = new List<SudokuConstraint>();

        _baseConstraints.AddRange(new SudokuRowConstraintFactory().GetConstraints());
        _baseConstraints.AddRange(new SudokuColumnConstraintFactory().GetConstraints());
        _baseConstraints.AddRange(new SudokuBoxConstraintFactory().GetConstraints());
        _random = random;
    }

    public SudokuPuzzle(SudokuPuzzle old)
    {
        _grid = old._grid.Select(x => new SudokuCellOption(x)).ToArray();
        _baseConstraints = old._baseConstraints;
        _extraConstraints = new List<SudokuConstraint>(old._extraConstraints);
        _random = old._random;
    }

    public static SudokuPuzzle Generate(System.Random random)
    {
        Stack<SudokuPuzzle> possibilities = new Stack<SudokuPuzzle>();

        possibilities.Push(new SudokuPuzzle(random));

        while (possibilities.Count > 0)
        {
            SudokuPuzzle possibility = possibilities.Pop();

            possibility.Reduce();

            if (possibility.IsBroken())
                continue;

            if (possibility.IsSolved())
            {
                Debug.Log("Grid: " + possibility._grid.Select(x => "[" + Enumerable.Range(0, 6).Where(y => x.Options[y]).Select(y => y + 1).Join("") + "]").Join(""));

                possibility.GenerateConstraints();
                if (!possibility.IsUnique())
                    continue;

                Queue<SudokuConstraint> constraints = new Queue<SudokuConstraint>(possibility._extraConstraints);
                while (constraints.Count > 0)
                {
                    SudokuConstraint currentConstraint = constraints.Dequeue();
                    IEnumerable<IEnumerable<SudokuConstraint>> alternatives = possibility.Shuffle(currentConstraint.GetReductions().ToList());

                    foreach (IEnumerable<SudokuConstraint> alternative in alternatives)
                    {
                        SudokuPuzzle newPuzzle = new SudokuPuzzle(possibility);

                        newPuzzle._extraConstraints.Remove(currentConstraint);
                        newPuzzle._extraConstraints.AddRange(alternative);

                        //Debug.Log("Trying to replace " + possibility + " by " + newPuzzle);

                        if (!newPuzzle.IsUnique())
                            continue;

                        possibility._extraConstraints.Remove(currentConstraint);
                        possibility._extraConstraints.AddRange(alternative);

                        //TODO account for clue quality
                        constraints = new Queue<SudokuConstraint>(possibility.Shuffle(constraints.Concat(alternative).ToList()).OrderBy(x => !(x is SudokuValueConstraint)));

                        break;
                    }
                }

                return possibility;
            }

            IEnumerable<SudokuPuzzle> newPossibilities = possibility.Shuffle(possibility.Bifurcate(possibility.PickRandom(possibility.WeakestIndices().ToList())).ToList());
            foreach (SudokuPuzzle newPossibility in newPossibilities)
                possibilities.Push(newPossibility);
        }

        throw new Exception("Could not generate sudoku");
    }

    public void GenerateConstraints()
    {
        int[] grid = _grid.Select(x => x.Options.IndexOf(y => y)).ToArray();

        _extraConstraints.AddRange(new SudokuValueConstraintFactory().GetConstraints(grid));

        _extraConstraints.AddRange(new SudokuWhispersConstraintFactory(_random).GetConstraints(grid).ToList());
        //_extraConstraints.AddRange(new SudokuThermoConstraintFactory(_random).GetConstraints(grid).ToList());
        //_extraConstraints.AddRange(new SudokuQuadruplesConstraintFactory().GetConstraints(grid).ToList());
        //_extraConstraints.AddRange(new SudokuKropkiConstraintFactory(_random).GetConstraints(grid).ToList());
        //_extraConstraints.AddRange(new SudokuParityConstraintFactory().GetConstraints(grid).ToList());

        Shuffle(_extraConstraints).OrderBy(x => !(x is SudokuValueConstraint));
    }

    public bool IsUnique()
    {
        bool hasSolution = false;

        Stack<SudokuPuzzle> possibilities = new Stack<SudokuPuzzle>();

        SudokuPuzzle puzzleTest = new SudokuPuzzle(_random);
        puzzleTest._baseConstraints = _baseConstraints;
        puzzleTest._extraConstraints = _extraConstraints;

        possibilities.Push(puzzleTest);

        while (possibilities.Count > 0)
        {
            SudokuPuzzle possibility = possibilities.Pop();

            possibility.Reduce();

            if (possibility.IsBroken())
                continue;

            if (possibility.IsSolved())
            {
                //Debug.Log(possibility._grid.Select(x => "[" + Enumerable.Range(0, 6).Where(y => x.Options[y]).Select(y => y + 1).Join("") + "]").Join(""));

                if (hasSolution)
                    return false;

                hasSolution = true;

                continue;
            }

            IEnumerable<SudokuPuzzle> newPossibilities = possibility.Bifurcate(possibility.WeakestIndices().First());
            foreach (SudokuPuzzle newPossibility in newPossibilities)
                possibilities.Push(newPossibility);
        }

        return hasSolution;
    }

    public void Reduce()
    {
        List<SudokuConstraint> allConstraints = _baseConstraints.Concat(_extraConstraints).ToList();

        bool didReduce;
        do
        {
            didReduce = false;

            foreach (SudokuConstraint constraint in allConstraints)
                didReduce |= constraint.Reduce(_grid);

            if (IsBroken())
                break;
        } while (didReduce);
    }

    public IEnumerable<SudokuPuzzle> Bifurcate(int index)
    {
        List<SudokuPuzzle> bifurcations = new List<SudokuPuzzle>();

        for (int i = 0; i < 6; i++)
        {
            if (!_grid[index].Options[i])
                continue;

            SudokuPuzzle bifurcation = new SudokuPuzzle(this);
            SudokuCellOption option = bifurcation._grid[index];

            for (int j = 0; j < 6; j++)
            {
                if (i == j)
                    continue;

                option.Eliminate(j);
            }

            bifurcations.Add(bifurcation);
        }

        return bifurcations;
    }

    public IEnumerable<int> WeakestIndices()
    {
        List<int> weakestIndices = new List<int>();

        int lowestEntropy = 7;

        for (int i = 0; i < _grid.Length; i++)
        {
            int entropy = _grid[i].Entropy();

            if (entropy <= 1)
                continue;

            if (entropy < lowestEntropy)
            {
                lowestEntropy = entropy;
                weakestIndices = new List<int> { i };
                continue;
            }

            if (entropy == lowestEntropy)
            {
                weakestIndices.Add(i);
                continue;
            }
        }

        return weakestIndices;
    }

    public bool IsBroken()
    {
        for (int i = 0; i < _grid.Length; i++)
            if (_grid[i].Entropy() == 0)
                return true;

        return false;
    }

    public bool IsSolved()
    {
        for (int i = 0; i < _grid.Length; i++)
            if (_grid[i].Entropy() != 1)
                return false;

        return true;
    }

    public override string ToString()
    {
        return _extraConstraints.Join(", ");
    }

    //Due to inaccessibility of UnityEngine.Random
    private T PickRandom<T>(List<T> list)
    {
        int index = _random.Next(0, list.Count);
        return list[index];
    }

    //Due to inaccessibility of UnityEngine.Random
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
