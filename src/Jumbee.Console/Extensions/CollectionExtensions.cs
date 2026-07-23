namespace Jumbee.Console;

internal static class CollectionExtensions
{
    extension<T>(T[] arr)
    {
        public U[] Map<U>(Func<T, U> map)
        {
            var ret = new U[arr.Length];
            for (var i = 0; i < arr.Length; i++)
            {
                ret[i] = map(arr[i]);
            }
            return ret;
        }
    }

    extension<T>(T[][] arr)
    {
        internal T[][] Transpose()
        {
            if (arr == null || arr.Length == 0)
            {
                return [];
            }

            // Determine the number of rows (source.Length) and columns (source[0].Length)
            var rowCount = arr.Length;
            for (var i = 1; i < rowCount; i++)
            {
                if (arr[i].Length != arr[0].Length)
                {
                    throw new ArgumentException("All inner arrays must have the same length to transpose.");
                }
            }
            // This assumes all inner arrays have the same length for a successful transpose
            var columnCount = arr[0].Length;

            // Create the new jagged array with dimensions swapped
            var result = new T[columnCount][];

            for (var i = 0; i < columnCount; i++)
            {
                // Initialize each inner array of the result with the new row count
                result[i] = new T[rowCount];
                for (var j = 0; j < rowCount; j++)
                {
                    // Swap the indices (i, j) to (j, i)
                    result[i][j] = arr[j][i];
                }
            }

            return result;
        }
    }
}