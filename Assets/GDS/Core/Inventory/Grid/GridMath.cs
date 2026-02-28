#nullable enable
using System;
using UnityEngine;

namespace GDS.Core {

    public static class GridMath {
        public enum FillDirection { Horizontal, Vertical }
        public static (int Height, int Width) GetLength2D<T>(this T[,] matrix) => (matrix.GetLength(0), matrix.GetLength(1));
        public static Size GetSize<T>(this T[,] matrix) => new(matrix.GetLength(1), matrix.GetLength(0));

        public static T[,] CreateMatrix<T>(Size size, T value) {
            var (columns, rows) = size;
            T[,] matrix = new T[rows, columns];
            for (int i = 0; i < rows; i++)
                for (int j = 0; j < columns; j++) matrix[i, j] = value;
            return matrix;
        }

        public static T[,] CreateMatrix<T>(Size size, Func<Pos, T> CreateFn) {
            var (columns, rows) = size;
            T[,] matrix = new T[rows, columns];
            for (int i = 0; i < rows; i++)
                for (int j = 0; j < columns; j++) matrix[i, j] = CreateFn(new Pos(j, i));
            return matrix;
        }

        // Tries to fit the shape of on item into a grid matrix 
        // Uses marching window algorithm - checks every position in the grid by overlapping item matrix over grid matrix
        // Returns the first position (in the grid) where matrices don't overlap, i.e. the "overlapped sum" of both matrices have all values <= 1
        public static Pos? TryFitItemIntoGrid(int[,] grid, int[,] item, FillDirection fillDirection = FillDirection.Horizontal) {
            var (h, w) = grid.GetLength2D();
            var (ih, iw) = item.GetLength2D();
            if (ih > h || iw > w) return null;

            if (fillDirection == FillDirection.Horizontal) {
                for (var i = 0; i < h - ih + 1; i++)
                    for (var j = 0; j < w - iw + 1; j++)
                        if (CanFitItemAtPos(grid, item, ih, iw, j, i)) return new Pos(j, i);
            } else {
                for (var j = 0; j < w - iw + 1; j++)
                    for (var i = 0; i < h - ih + 1; i++)
                        if (CanFitItemAtPos(grid, item, ih, iw, j, i)) return new Pos(j, i);
            }

            return null;
        }

        public static bool CanFitItemAtPos(int[,] grid, int[,] item, int ih, int iw, int x, int y) {
            for (var k = 0; k < ih; k++)
                for (var l = 0; l < iw; l++)
                    if (item[k, l] + grid[y + k, x + l] > 1) return false;
            return true;
        }

        public static Pos ScreenPosToGridPos(Vector3 screenPos, int cellSize, Size gridSize) {
            int x = (int)Math.Floor(screenPos.x / cellSize);
            int y = (int)Math.Floor(screenPos.y / cellSize);
            // gridCoord can go out of grid bounds due to borders, AR scaling, etc.
            x = Math.Clamp(x, 0, gridSize.W - 1);
            y = Math.Clamp(y, 0, gridSize.H - 1);
            return new Pos(x, y);
        }




        // TODO: Rename this perhaps
        // A better name would be TranslateAndAddToTarget(source, target, offset)
        // Translates the shape by an offset, then adds occupancy values into shape matrix while respecting the shape "mask"
        public static int[,] ComputedShape(int[,] occupancy, int[,] shape, Pos offset) {
            var (x, y) = offset;
            var (rows, cols) = shape.GetLength2D();
            var (h, w) = occupancy.GetLength2D();
            if (x + cols > w) return shape;
            if (y + rows > h) return shape;

            // TODO: optimize by using a temp matrix cache
            int[,] computed = new int[rows, cols];
            for (var i = 0; i < rows; i++) {
                for (var j = 0; j < cols; j++) {
                    // skip the "unmasked bits"
                    if (shape[i, j] == 0) { computed[i, j] = 0; continue; }
                    computed[i, j] = shape[i, j] + occupancy[i + y, j + x];
                }
            }

            return computed;
        }

        // TODO: Rename this perhaps
        // Translates the shape by an offset and then adds shape values into occupancy, thus creating an overlap state
        // Returns a new computed matrix
        public static int[,] ComputedOccupancy(int[,] occupancy, int[,] shape, Pos offset) {
            var (x, y) = offset;
            var (rows, cols) = shape.GetLength2D(); ;
            var (h, w) = occupancy.GetLength2D();
            if (x + cols > w) return shape;
            if (y + rows > h) return shape;

            int[,] computed = CloneMatrix(occupancy);
            for (var i = 0; i < rows; i++) {
                for (var j = 0; j < cols; j++) {
                    computed[i + y, j + x] = shape[i, j] + occupancy[i + y, j + x];
                }
            }

            return computed;
        }

        public static int[,] CloneMatrix(int[,] source) {
            var (rows, cols) = source.GetLength2D();
            int[,] clone = new int[rows, cols];
            for (int i = 0; i < rows; i++)
                for (int j = 0; j < cols; j++)
                    clone[i, j] = source[i, j];
            return clone;
        }

        public static int[,] Rotate(this int[,] matrix, Direction direction) => direction switch {
            Direction.W => matrix.Rotate90(),
            Direction.S => matrix.Rotate180(),
            Direction.E => matrix.Rotate270(),
            _ => matrix
        };

        public static int[,] Rotate180(this int[,] matrix) {
            var (h, w) = matrix.GetLength2D();
            int[,] rotated = new int[h, w];
            for (int i = 0; i < h; i++)
                for (int j = 0; j < w; j++)
                    rotated[h - 1 - i, w - 1 - j] = matrix[i, j];
            return rotated;
        }

        public static int[,] Rotate270(this int[,] matrix) {
            var (h, w) = matrix.GetLength2D();
            int[,] rotated = new int[w, h];
            for (int i = 0; i < h; i++)
                for (int j = 0; j < w; j++)
                    rotated[j, h - 1 - i] = matrix[i, j];
            return rotated;
        }

        public static int[,] Rotate90(this int[,] matrix) {
            var (h, w) = matrix.GetLength2D();
            int[,] rotated = new int[w, h];
            for (int i = 0; i < h; i++)
                for (int j = 0; j < w; j++)
                    rotated[w - 1 - j, i] = matrix[i, j];
            return rotated;
        }

        public static Direction Rotate(this Direction direction) => direction switch {
            Direction.N => Direction.E,
            Direction.E => Direction.S,
            Direction.S => Direction.W,
            Direction.W => Direction.N,
            _ => Direction.N
        };

        public static Pos AdjustPosForSizeAndDir(Size size, Direction dir) => dir switch {
            Direction.N => new Pos(0, 0),
            Direction.E => new Pos(size.H, 0),
            Direction.S => new Pos(size.W, size.H),
            Direction.W => new Pos(0, size.W),
            _ => new Pos(0, 0)
        };

        public static bool InLocalBounds(Vector3 pos, Rect rect) => pos.x >= 0 && pos.y >= 0 && pos.x < rect.width && pos.y < rect.height;
        public static bool InWorldBounds(Vector3 pos, Rect rect) => pos.x >= rect.x && pos.y >= rect.y && pos.x < rect.x + rect.width && pos.y < rect.y + rect.height;
        public static bool InBounds(Pos pos, Size size) => pos.X >= 0 && pos.Y >= 0 && pos.X < size.W && pos.Y < size.H;
    }

}