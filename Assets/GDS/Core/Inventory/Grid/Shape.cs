using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace GDS.Core {
    [CreateAssetMenu(menuName = "SO/Core/Shape")]
    public class Shape : ScriptableObject {
        public int Width = 1;
        public int Height = 1;
        public int[] Data = new int[1];
        public int[,] Mask => ComputeMask();

        public string hash = "";
        Dictionary<string, int[,]> maskCache = new();


        void OnValidate() {
            Width = Mathf.Max(1, Width);
            Height = Mathf.Max(1, Height);

            int expectedSize = Width * Height;
            if (Data == null || Data.Length != expectedSize) Resize();
            hash = ComputeHash();
        }

        void Resize() {
            var newData = new int[Width * Height];
            int oldWidth = Mathf.Min(Width, Data.Length);
            int oldHeight = Data.Length / oldWidth;

            for (int y = 0; y < Mathf.Min(oldHeight, Height); y++)
                for (int x = 0; x < Mathf.Min(oldWidth, Width); x++)
                    newData[y * Width + x] = Data[y * oldWidth + x];


            Data = newData;
        }

        string ComputeHash() {
            var sb = new StringBuilder(Data.Length + 4);
            sb.Append($"{Width}x{Height}|");
            for (var i = 0; i < Data.Length; i++) sb.Append(Data[i]);
            return sb.ToString();
        }

        int[,] ComputeMask() {
            int[,] mask;
            if (maskCache.TryGetValue(hash, out mask)) return mask;

            mask = BuildMask();
            maskCache[hash] = mask;
            return mask;
        }

        int[,] BuildMask() {
            var mask = new int[Height, Width];
            for (int y = 0; y < Height; y++)
                for (int x = 0; x < Width; x++)
                    mask[y, x] = Data[y * Width + x];

            return mask;
        }

        public void Toggle(int x, int y) {
            Data[y * Width + x] ^= 1;
            OnValidate();
        }
    }
}