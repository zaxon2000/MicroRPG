namespace GDS.Core {
    public static class GridUtil {
        public static string PrintShape(int[,] matrix) {
            var str = "";
            var (rows, cols) = matrix.GetLength2D();

            for (var i = 0; i < rows; i++) {
                for (var j = 0; j < cols; j++) {
                    str += matrix[i, j] >= 1 ? "■" : "□";//"⬚";
                }
                str += "\n";
            }

            return str;
        }
    }
}