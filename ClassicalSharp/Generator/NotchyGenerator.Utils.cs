// Copyright 2014-2017 ClassicalSharp | Licensed under BSD-3
// Based on:
// https://github.com/UnknownShadow200/ClassicalSharp/wiki/Minecraft-Classic-map-generation-algorithm
// Thanks to Jerralish for originally reverse engineering classic's algorithm, then preparing a high level overview of the algorithm.
// I believe this process adheres to clean room reverse engineering.
using System;
using System.Collections.Generic;
using BlockRaw = System.Byte;

namespace ClassicalSharp.Generator {
	
	public sealed partial class NotchyGenerator {
		
		void FillOblateSpheroid(double x, double y, double z, double radius, BlockRaw block) {
			double radius2 = radius;
			if (radius2 < 0) {
				radius2 *= -1;
			}
			int xStart = (int)Math.Floor(Math.Max(x - radius2, 0));
			int xEnd = (int)Math.Ceiling(Math.Min(x + radius2, Width - 1));
			int yStart = (int)Math.Floor(Math.Max(y - radius2, 0));
			int yEnd = (int)Math.Ceiling(Math.Min(y + radius2, Height - 1));
			int zStart = (int)Math.Floor(Math.Max(z - radius2, 0));
			int zEnd = (int)Math.Ceiling(Math.Min(z + radius2, Length - 1));
			double radiusSq = Math.Pow(radius2, 2);
			
			for (int yy = yStart; yy <= yEnd; yy++)
				for (int zz = zStart; zz <= zEnd; zz++)
					for (int xx = xStart; xx <= xEnd; xx++)
			{
				double dx = xx - x, dy = yy - y, dz = zz - z;
				if ((Math.Pow(dx, 2) + 2 * Math.Pow(dy, 2) + Math.Pow(dz, 2)) < radiusSq) {
					int index = (yy * Length + zz) * Width + xx;
					if (blocks[index] == Block.Stone)
						blocks[index] = block;
				}
			}
		}
		
		void FloodFill(int startIndex, BlockRaw block) {
			if (startIndex < 0) return; // y below map, immediately ignore
			FastIntStack stack = new FastIntStack(4);
			stack.Push(startIndex);	
			
			while (stack.Size > 0) {
				int index = stack.Pop();
				if (blocks[index] != Block.Air) continue;
				blocks[index] = block;
				
				int x = index % Width;
				int y = index / oneY;
				int z = (index / Width) % Length;
				
				if (x > 0) stack.Push(index - 1);
				if (x < Width - 1) stack.Push(index + 1);
				if (z > 0) stack.Push(index - Width);
				if (z < Length - 1) stack.Push(index + Width);
				if (y > 0) stack.Push(index - oneY);
			}
		}
		
		sealed class FastIntStack {
			public int[] Values;
			public int Size;
			
			public FastIntStack(int capacity) {
				Values = new int[capacity];
				Size = 0;
			}
			
			public int Pop() {
				return Values[--Size];
			}
			
			public void Push(int item) {
				if (Size == Values.Length) {
					int[] array = new int[Values.Length * 2];
					Buffer.BlockCopy(Values, 0, array, 0, Size * sizeof(int));
					Values = array;
				}
				Values[Size++] = item;
			}
		}
	}
	
	// Based on https://docs.oracle.com/javase/7/docs/api/java/util/Random.html
	public sealed class JavaRandom {
		
		long seed;
		const long value = 0x5DEECE66DL;
		const long mask = ((1L << 48) - 1);
		
		public JavaRandom() { SetSeed(NextUnique() ^ Utils.CurrentTimeMillis()); }
		
		public JavaRandom(int seed) { SetSeed(seed); }
		public void SetSeed(int seed) {
			this.seed = (seed ^ value) & mask;
			haveNextNextGaussian = false;
		}
		
		public JavaRandom(long seed) { SetSeed(seed); }
		public void SetSeed(long seed) {
			this.seed = (seed ^ value) & mask;
			haveNextNextGaussian = false;
		}
		
		private int NextSeed(int bits) {
			seed = (seed * value + 0xBL) & mask;
			return (int)((ulong)seed >> (48 - bits));
		}
		
		public int Next(int min, int max) { return min + Next(max - min); }
		
		public int Next(int n) {
			if ((n & -n) == n) { // i.e., n is a power of 2
				return (int)((n * (long)NextSeed(31)) >> 31);
			}

			int bits, val;
			do {
				bits = NextSeed(31);
				val = bits % n;
			} while (bits - val + (n - 1) < 0);
			return val;
		}
		
		public long nextLong() {
			return ((long)NextSeed(32) << 32) + NextSeed(32);
		}
		
		public double NextDouble() {
			seed = (seed * value + 0xBL) & mask;
			int Num1 = (int)((ulong)seed >> (48 - 26));
			
			seed = (seed * value + 0xBL) & mask;
			int Num2 = (int)((ulong)seed >> (48 - 27));
			
			return (((long)Num1 << 27) + Num2)
				/ (double) (1L << 53);
		}
		
		public float NextFloat() {
			return NextSeed(24) / ((float)(1 << 24));
		}
		
		private double nextNextGaussian;
		private bool haveNextNextGaussian = false;
		
		public double NextGaussian() {
			if (haveNextNextGaussian) {
				haveNextNextGaussian = false;
				return nextNextGaussian;
			} else {
				double v1, v2, s;
				do {
					v1 = 2 * NextDouble() - 1;   // between -1.0 and 1.0
					v2 = 2 * NextDouble() - 1;   // between -1.0 and 1.0
					s = v1 * v1 + v2 * v2;
				} while (s >= 1 || s == 0);
				double multiplier = Math.Sqrt(-2 * Math.Log(s)/s);    // Says to use strictmath here
				nextNextGaussian = v2 * multiplier;
				haveNextNextGaussian = true;
				return v1 * multiplier;
			}
		}
		
		public double NextGaussian(double deviation, double average) {
			return NextGaussian() * deviation + average;
		}
		
		public double NextGaussianFloat() {
			if (haveNextNextGaussian) {
				haveNextNextGaussian = false;
				return nextNextGaussian;
			} else {
				double v1, v2, s;
				do {
					v1 = 2 * NextFloat() - 1;   // between -1.0 and 1.0
					v2 = 2 * NextFloat() - 1;   // between -1.0 and 1.0
					s = v1 * v1 + v2 * v2;
				} while (s >= 1 || s == 0);
				double multiplier = Math.Sqrt(-2 * Math.Log(s)/s);    // Says to use strictmath here
				nextNextGaussian = v2 * multiplier;
				haveNextNextGaussian = true;
				return v1 * multiplier;
			}
		}
		
		public double NextGaussianFloat(float deviation, float average) {
			return NextGaussianFloat() * deviation + average;
		}
			
		private static JavaRandom uniqueRand = new JavaRandom(Utils.CurrentTimeMillis());
		private static long NextUnique() {
			return uniqueRand.nextLong();
		}
	}
}