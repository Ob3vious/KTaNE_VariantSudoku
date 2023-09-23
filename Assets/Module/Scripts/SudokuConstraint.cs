using System.Collections.Generic;

public interface SudokuConstraint
{
    bool Reduce(SudokuCellOption[] grid);
    IEnumerable<IEnumerable<SudokuConstraint>> GetReductions();
}

public interface SudokuConstraintFactory
{
    IEnumerable<SudokuConstraint> GetConstraints(int[] grid);
}

public interface SudokuBaseConstraintFactory
{
    IEnumerable<SudokuConstraint> GetConstraints();
}
