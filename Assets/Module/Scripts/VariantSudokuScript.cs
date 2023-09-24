using System.Collections;
using System.Threading;
using UnityEngine;

public class VariantSudokuScript : MonoBehaviour
{
    private Thread _thread = null;
    private static bool _isUsingThreads = false;

    private SudokuPuzzle _puzzle;

    void Start()
    {
        StartCoroutine(GeneratePuzzle());
    }

    private IEnumerator GeneratePuzzle()
    {
        yield return new WaitForSeconds(UnityEngine.Random.Range(0.5f, 2f));

        int seed = UnityEngine.Random.Range(int.MinValue, int.MaxValue);

        Debug.Log("Seed is: " + seed);

        yield return new WaitWhile(() => _isUsingThreads);

        _isUsingThreads = true;

        _thread = new Thread(() =>
        {
            _puzzle = SudokuPuzzle.Generate(new System.Random(seed));
        });

        _thread.Start();

        yield return new WaitWhile(() => _puzzle == null);

        _isUsingThreads = false;

        Debug.Log("The puzzle is: " + _puzzle);

        Debug.Log(_puzzle.IsUnique());
    }

    //Don't hold up the next modules if the module is gone before finishing generation
    void OnDestroy()
    {
        if (_thread == null)
            return;

        _thread.Interrupt();
        _thread = null;
        _isUsingThreads = false;
    }
}
