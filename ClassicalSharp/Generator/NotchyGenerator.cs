// Copyright 2014-2017 ClassicalSharp | Licensed under BSD-3
// Based on:
// https://github.com/UnknownShadow200/ClassicalSharp/wiki/Minecraft-Classic-map-generation-algorithm
// Thanks to Jerralish for originally reverse engineering classic's algorithm, then preparing a high level overview of the algorithm.
// I believe this process adheres to clean room reverse engineering.
using System;
using System.Collections.Generic;
using BlockRaw = System.Byte;

namespace ClassicalSharp.Generator {
	
	public sealed partial class NotchyGenerator : IMapGenerator {
		
		int waterLevel, oneY;
		BlockRaw[] blocks;
		short[] heightmap;
		short[] heightmapChunk;
		short[] heightmapMin;
		JavaRandom rnd, rnd2, rnd3;
		int minHeight;
		long chunkSeed, chunkXMul, chunkZMul;
		
		public override string GeneratorName { get { return "Vanilla classic"; } }
		
		public override BlockRaw[] Generate() {
			oneY = Width * Length;
			waterLevel = Height / 2;
			blocks = new BlockRaw[Width * Height * Length];
			rnd = new JavaRandom(Seed);
			rnd2 = new JavaRandom(Seed);
			chunkXMul = rnd2.nextLong();
			chunkZMul = rnd2.nextLong();
			minHeight = Height;
			//GenerateChunk(2, 2);
			
			/*CombinedNoise n1 = new CombinedNoise(
				new OctaveNoise(8, rnd), new OctaveNoise(8, rnd));
			CombinedNoise n2 = new CombinedNoise(
				new OctaveNoise(8, rnd), new OctaveNoise(8, rnd));
			OctaveNoise n3 = new OctaveNoise(6, rnd);
			
			for (int z = 0; z < (Length + Length % 16) / 16; z++)
				for (int x = 0; x < (Width + Width % 16) / 16; x++) {
				chunkSeed = Seed + (x * chunkXMul) + (z * chunkZMul);
				if (heightmap == null) heightmap = new short[Width * Length];
				CreateChunkHeightmap(x, z, n1, n2, n3);
			}*/
			
			CreateHeightmap();
			CreateStrata();
			CarveCaves();
			CarveOreVeins(0.9f, "coal ore", Block.CoalOre);
			CarveOreVeins(0.7f, "iron ore", Block.IronOre);
			CarveOreVeins(0.5f, "gold ore", Block.GoldOre);
			
			FloodFillWaterBorders();
			FloodFillWater();
			FloodFillLava();

			CreateSurfaceLayer();
			PlantFlowers();
			PlantMushrooms();
			PlantTrees();
			return blocks;
		}
		
		public void GenerateChunk(int chunkX, int chunkZ) {
			chunkSeed = (chunkX * chunkXMul) ^ (chunkZ * chunkZMul) ^ Seed;
			CombinedNoise n1 = new CombinedNoise(
				new OctaveNoise(8, rnd), new OctaveNoise(8, rnd));
			CombinedNoise n2 = new CombinedNoise(
				new OctaveNoise(8, rnd), new OctaveNoise(8, rnd));
			OctaveNoise n3 = new OctaveNoise(6, rnd);
			
			if (heightmap == null) heightmap = new short[Width * Length];
			
			CreateChunkHeightmap(chunkX, chunkZ, n1, n2, n3);
			
			OctaveNoise n4 = new OctaveNoise(8, rnd);
			
			CreateChunkStrata(chunkX, chunkZ, n4);
			
			OctaveNoise n5 = new OctaveNoise(8, rnd);
			OctaveNoise n6 = new OctaveNoise(8, rnd);
			
			CreateChunkSurfaceLayer(chunkX, chunkZ, n5, n6);
			
		}
		
		void CreateChunkHeightmap(int chunkX, int chunkZ, CombinedNoise n1, CombinedNoise n2, OctaveNoise n3) {
			/*CombinedNoise n1 = new CombinedNoise(
				new OctaveNoise(8, rnd), new OctaveNoise(8, rnd));
			CombinedNoise n2 = new CombinedNoise(
				new OctaveNoise(8, rnd), new OctaveNoise(8, rnd));
			OctaveNoise n3 = new OctaveNoise(6, rnd);*/
			int index = 0;
			//short[] hMap = new short[16 * 16];
			int offsetX = (chunkX * 16);
			int offsetZ = (chunkZ * 16);
			CurrentState = "Building heightmap";
			
			for (int z = 0; z < 16; z++) {
				int zCur = z + offsetZ;
				CurrentProgress = (float)z / Length;
				for (int x = 0; x < 16; x++) {
					int xCur = x + offsetX;
					if (xCur >= Width || zCur >= Length) break;
					double hLow = n1.Compute(xCur * 1.3f, zCur * 1.3f) / 6 - 4, height = hLow;
					
					if (n3.Compute(xCur, zCur) <= 0) {
						double hHigh = n2.Compute(xCur * 1.3f, zCur * 1.3f) / 5 + 6;
						height = Math.Max(hLow, hHigh);
					}
					
					height *= 0.5;
					if (height < 0) height *= 0.8f;
					
					short adjHeight = (short)(height + waterLevel);
					minHeight = adjHeight < minHeight ? adjHeight : minHeight;
					index = zCur * Width + xCur;
					heightmap[index] = adjHeight;
				}
			}
			//heightmap = hMap;
		}
		
		void CreateChunkStrata(int chunkX, int chunkZ, OctaveNoise n) {
			CurrentState = "Creating strata";			
			int hMapIndex = 0, maxY = Height - 1, mapIndex = 0;
			// Try to bulk fill bottom of the map if possible
			int minStoneY = CreateChunkStrataFast(chunkX, chunkZ);
			int offsetX = (chunkX * 16);
			int offsetZ = (chunkZ * 16);

			for (int z = 0; z < 16; z++) {
				int zCur = z + offsetZ;
				CurrentProgress = (float)z / Length;
				for (int x = 0; x < 16; x++) {
					int xCur = x + offsetX;
					int index = zCur * Width + xCur;
					int dirtThickness = (int)(n.Compute(x, z) / 24 - 4);
					int dirtHeight = heightmap[index];
					int stoneHeight = dirtHeight + dirtThickness;	
					
					stoneHeight = Math.Min(stoneHeight, maxY);
					dirtHeight  = Math.Min(dirtHeight,  maxY);
					
					mapIndex = minStoneY * oneY + zCur * Width + xCur;
					for (int y = minStoneY; y <= stoneHeight; y++) {
						blocks[mapIndex] = Block.Stone; mapIndex += oneY;
					}
					
					stoneHeight = Math.Max(stoneHeight, 0);
					mapIndex = (stoneHeight + 1) * oneY + zCur * Width + xCur;
					for (int y = stoneHeight + 1; y <= dirtHeight; y++) {
						blocks[mapIndex] = Block.Dirt; mapIndex += oneY;
					}
				}
			}
		}
		
		int CreateChunkStrataFast(int chunkX, int chunkZ) {
			// Make lava layer at bottom
			int index = 0;
			int offsetX = (chunkX * 16);
			int offsetZ = (chunkZ * 16);
			for (int z = 0; z < 16; z++)
				for (int x = 0; x < 16; x++)
			{
				int xCur = x + offsetX;
				int zCur = z + offsetZ;
				index = zCur * Width + xCur;
				blocks[index] = Block.Lava;
			}
			
			// Invariant: the lowest value dirtThickness can possible be is -14
			int stoneHeight = minHeight - 14;
			if (stoneHeight <= 0) return 1; // no layer is fully stone
			
			// We can quickly fill in bottom solid layers
			for (int y = 1; y <= stoneHeight; y++)
				for (int z = 0; z < 16; z++)
					for (int x = 0; x < 16; x++)
			{
				int xCur = x + offsetX;
				int zCur = z + offsetZ;
				index = (y * Length + zCur) * Width + xCur;
				blocks[index] = Block.Stone;
			}
			return stoneHeight;
		}
		
		void CreateChunkSurfaceLayer(int chunkX, int chunkZ, OctaveNoise n1, OctaveNoise n2) {
			CurrentState = "Creating surface";
			// TODO: update heightmap
			int offsetX = (chunkX * 16);
			int offsetZ = (chunkZ * 16);
			for (int z = 0; z < 16; z++) {
				int zCur = z + offsetZ;
				CurrentProgress = (float)z / Length;
				for (int x = 0; x < 16; x++) {
					int xCur = x + offsetX;
					int hMapIndex = zCur * Width + xCur;
					int y = heightmap[hMapIndex];
					if (y < 0 || y >= Height) continue;
					
					int index = (y * Length + zCur) * Width + xCur;
					BlockRaw blockAbove = y >= (Height - 1) ? Block.Air : blocks[index + oneY];
					if (blockAbove == Block.Water && (n2.Compute(xCur, zCur) > 12)) {
						blocks[index] = Block.Gravel;
					} else if (blockAbove == Block.Air) {
						blocks[index] = (y <= waterLevel && (n1.Compute(xCur, zCur) > 8)) ? Block.Sand : Block.Grass;
					}
				}
			}
		}
		
		/* ========================= */
		/* ===== OLD FUNCTIONS ===== */
		/* ========================= */
		
		void CreateHeightmap() {
			CombinedNoise n1 = new CombinedNoise(
				new OctaveNoise(8, rnd), new OctaveNoise(8, rnd));
			CombinedNoise n2 = new CombinedNoise(
				new OctaveNoise(8, rnd), new OctaveNoise(8, rnd));
			OctaveNoise n3 = new OctaveNoise(6, rnd);
			int index = 0;
			short[] hMap = new short[Width * Length];
			CurrentState = "Building heightmap";
			
			for (int z = 0; z < Length; z++) {
				CurrentProgress = (float)z / Length;
				for (int x = 0; x < Width; x++) {
					double hLow = n1.Compute(x * 1.3f, z * 1.3f) / 6 - 4, height = hLow;
					double hHigh = n2.Compute(x * 1.3f, z * 1.3f) / 5 + 6;
					
					if (n3.Compute(x, z) <= 0) {
						height = Math.Max(hLow, hHigh);
					}
					/*if (x == 135 && z == 138) {
						double nose = n3.Compute(x, z) / 8;
						Console.WriteLine(nose.ToString());
					}*/
					
					height *= 0.5;
					if (height < 0) height *= 0.8f;
					
					double erode = n3.Compute(x, z);
					
					//if (n3.Compute(x, z) > 3.0 && hHigh < hLow) {
					if (erode / 8 <= hLow && Closer(hLow, hHigh, erode / 4) == hLow && erode >= 0) {
					//if (n3.Compute(x * 1.3, z * 1.3) / 6 < -0.5 || n3.Compute(x * 1.3, z * 1.3) / 6 > 1.5) {
						if ((short)height % 2 != 0 && (short)height > 0) {
							height -= 1;
						}
					}
					
					short adjHeight = (short)(height + waterLevel);
					minHeight = adjHeight < minHeight ? adjHeight : minHeight;
					hMap[index++] = adjHeight;
				}
			}
			heightmap = hMap;
		}
		
		public static double Closer(double a, double b, double compare) {
			double compareValue = compare;
			
			long calcA = Math.Abs((long)a - (long)compareValue);
			long calcB = Math.Abs((long)b - (long)compareValue);
			
			if (calcA == calcB) {
				return 0;
			}
			
			if (calcA < calcB) {
				return a;
			}
			
			return b;
		}
		
		void CreateStrata() {
			OctaveNoise n = new OctaveNoise(8, rnd);
			CurrentState = "Creating strata";			
			int hMapIndex = 0, maxY = Height - 1, mapIndex = 0;
			// Try to bulk fill bottom of the map if possible
			int minStoneY = CreateStrataFast();
			heightmapMin = new short[Width * Length];

			for (int z = 0; z < Length; z++) {
				CurrentProgress = (float)z / Length;
				for (int x = 0; x < Width; x++) {
					int dirtThickness = (int)(n.Compute(x, z) / 24 - 4);
					int dirtHeight = heightmap[hMapIndex++];
					int stoneHeight = dirtHeight + dirtThickness;	
					
					stoneHeight = Math.Min(stoneHeight, maxY);
					dirtHeight  = Math.Min(dirtHeight,  maxY);
					
					mapIndex = minStoneY * oneY + z * Width + x;
					for (int y = minStoneY; y <= stoneHeight; y++) {
						blocks[mapIndex] = Block.Stone; mapIndex += oneY;
					}
					
					heightmapMin[hMapIndex - 1] = (short)(stoneHeight + 1);
					if (heightmapMin[hMapIndex - 1] > heightmap[hMapIndex - 1]) {
						heightmapMin[hMapIndex - 1] = heightmap[hMapIndex - 1];
					}
					
					stoneHeight = Math.Max(stoneHeight, 0);
					mapIndex = (stoneHeight + 1) * oneY + z * Width + x;
					for (int y = stoneHeight + 1; y <= dirtHeight; y++) {
						blocks[mapIndex] = Block.Dirt; mapIndex += oneY;
					}
				}
			}
		}
		
		int CreateStrataFast() {
			// Make lava layer at bottom
			int mapIndex = 0;
			for (int z = 0; z < Length; z++)
				for (int x = 0; x < Width; x++)
			{
				blocks[mapIndex++] = Block.Lava;
			}
			
			// Invariant: the lowest value dirtThickness can possible be is -14
			int stoneHeight = minHeight - 14;
			if (stoneHeight <= 0) return 1; // no layer is fully stone
			
			// We can quickly fill in bottom solid layers
			for (int y = 1; y <= stoneHeight; y++)
				for (int z = 0; z < Length; z++)
					for (int x = 0; x < Width; x++)
			{
				blocks[mapIndex++] = Block.Stone;
			}
			return stoneHeight;
		}
		
		void CarveCaves() {
			int cavesCount = blocks.Length / 8192;
			int caveLimit = 5;
			CurrentState = "Carving caves";
			
			for (int i = 0; i < cavesCount; i++) {
				CurrentProgress = (float)i / cavesCount;
				double caveX = rnd.Next(Width);
				//double caveY = rnd.NextGaussian(Height/4, Height/4);
				double caveY = rnd.Next(Height);
				double caveZ = rnd.Next(Length);
				//if (caveY > heightmap[(int)caveZ * Width + (int)caveX]) {
				/*if (caveY > (Height / 2) + 4) {
					//double caveDiff = caveY - heightmap[(int)caveZ * Width + (int)caveX];
					double caveDiff = caveY - ((Height / 2) + 4);
					caveY -= caveDiff;
					//i -= 1;
					//continue;
				} while (blocks[((int)caveY * Length + (int)caveZ) * Width + (int)caveX] == Block.Dirt && caveY > 0) {
					caveY -= 1;
				}*/
				
				if (caveY < 0)
					caveY *= -1;
				
				double caveLen = (rnd.NextFloat() * rnd.NextFloat() * 200);
				double theta = rnd.NextFloat() * 2 * Math.PI, deltaTheta = 0;
				double phi = rnd.NextFloat() * 2 * Math.PI, deltaPhi = 0;
				double caveRadius = rnd.NextFloat() * rnd.NextFloat();
				
				//int caveLen2 = (int)caveLen;
				if (caveLen < 0) {
					caveLen *= -1;
				}
				
				for (int j = 0; j < (int)caveLen; j++) {
					/*if (caveX < 0 - caveLimit) {
						if (Math.Sin(theta) < 0 && Math.Cos(phi) > 0) {
							theta *= -1;
						} else if (Math.Sin(theta) > 0 && Math.Cos(phi) < 0) {
							phi *= -1;
						}
					}
					if (caveX >= Width + caveLimit) {
						if (Math.Sin(theta) > 0 && Math.Cos(phi) > 0) {
							theta *= -1;
						}// else if (Math.Sin(theta) > 0 && Math.Cos(phi) > 0) {
						//	phi *= -1;
						//}
					}*/
					caveX += Math.Sin(theta) * Math.Cos(phi);
					/*if (caveZ < 0) {
						if (Math.Cos(theta) < 0 && Math.Cos(phi) > 0) {
							theta *= -1;
						} else if (Math.Cos(theta) > 0 && Math.Cos(phi) < 0) {
							phi *= -1;
						}
					}*/
					caveZ += Math.Cos(theta) * Math.Cos(phi);
					/*if (caveY < 0 - caveLimit) {
						if (Math.Sin(phi) < 0) {
							phi *= -1;
						}
					} else if (caveY > Height  - 1 + caveLimit) {
						if (Math.Sin(phi) > 0) {
							phi *= -1;
						}
					}*/
					/*if (caveZ > 0 && caveZ < Width - 1 && caveX > 0 && caveX < Width - 1) {
						if (caveY > heightmap[(int)caveZ * Width + (int)caveX] + (Height / 2)) {
							if (Math.Sin(phi) > 0) {
								phi *= -1;
							}
						}
					}*/
					caveY += Math.Sin(phi);
					
					theta = theta + deltaTheta * 0.2;
					deltaTheta = deltaTheta * 0.9 + rnd.NextFloat() - rnd.NextFloat();
					phi = phi / 2 + deltaPhi / 4;
					deltaPhi = deltaPhi * 0.75 + rnd.NextFloat() - rnd.NextFloat();
					if (rnd.NextFloat() < 0.25) continue;
					
					double cenX = (caveX + (rnd.Next(4) - 2) * 0.2);
					double cenY = (caveY + (rnd.Next(4) - 2) * 0.2);
					double cenZ = (caveZ + (rnd.Next(4) - 2) * 0.2);
					
					double radius = (Height - cenY) / (double)Height;
					radius = 1.2 + (radius * 3.5 + 1) * caveRadius;
					radius = radius * Math.Sin(j * Math.PI / caveLen) + 0.5;
					FillOblateSpheroid(cenX, cenY, cenZ, radius, Block.Air);
				}
			}
		}
		
		void CarveOreVeins(float abundance, string blockName, BlockRaw block) {
			int numVeins = (int)(blocks.Length * abundance / 16384);
			CurrentState = "Carving " + blockName;
			
			for (int i = 0; i < numVeins; i++) {
				CurrentProgress = (float)i / numVeins;
				double veinX = rnd.Next(Width);
				double veinY = rnd.Next(Height);
				double veinZ = rnd.Next(Length);
				if (veinY > heightmap[(int)veinZ * Width + (int)veinX]) {
					double veinDiff = veinY - heightmap[(int)veinZ * Width + (int)veinX];
					veinY -= veinDiff;
					//i -= 1;
					//continue;
				} while (blocks[((int)veinY * Length + (int)veinZ) * Width + (int)veinX] == Block.Dirt && veinY > 0) {
					veinY -= 1;
				}
				
				double veinLen = (rnd.NextFloat() * rnd.NextFloat() * 75 * abundance);
				double theta = rnd.NextFloat() * 2 * Math.PI, deltaTheta = 0;
				double phi = rnd.NextFloat() * 2 * Math.PI, deltaPhi = 0;
				
				if (veinLen < 0) {
					veinLen *= -1;
				}
				
				for (int j = 0; j < (int)veinLen; j++) {
					veinX += Math.Sin(theta) * Math.Cos(phi);
					veinZ += Math.Cos(theta) * Math.Cos(phi);
					veinY += Math.Sin(phi);
					
					theta = theta + deltaTheta * 0.2;
					deltaTheta = deltaTheta * 0.9 + rnd.NextFloat() - rnd.NextFloat();
					phi = phi / 2 + deltaPhi / 4;
					deltaPhi = deltaPhi * 0.9 + rnd.NextFloat() - rnd.NextFloat();
					
					double radius = abundance * Math.Sin(j * Math.PI / veinLen) + 1;
					FillOblateSpheroid(veinX, veinY, veinZ, radius, block);
				}
			}
		}
		
		void FloodFillWaterBorders() {
			int waterY = waterLevel - 1;
			int index1 = (waterY * Length + 0) * Width + 0;
			int index2 = (waterY * Length + (Length - 1)) * Width + 0;
			CurrentState = "Flooding edge water";
			
			for (int x = 0; x < Width; x++) {
				CurrentProgress = 0 + ((float)x / Width) * 0.5f;
				FloodFill(index1, Block.Water);
				FloodFill(index2, Block.Water);
				index1++; index2++;
			}
			
			index1 = (waterY * Length + 0) * Width + 0;
			index2 = (waterY * Length + 0) * Width + (Width - 1);
			for (int z = 0; z < Length; z++) {
				CurrentProgress = 0.5f + ((float)z / Length) * 0.5f;
				FloodFill(index1, Block.Water);
				FloodFill(index2, Block.Water);
				index1 += Width; index2 += Width;
			}
		}
		
		void FloodFillWater() {
			int numSources = Width * Length / 800;
			CurrentState = "Flooding water";
			
			for (int i = 0; i < numSources; i++) {
				CurrentProgress = (float)i / numSources;
				int x = rnd.Next(Width), z = rnd.Next(Length);
				int y = waterLevel - rnd.Next(1, 3);
				FloodFill((y * Length + z) * Width + x, Block.Water);
			}
		}
		
		void FloodFillLava() {
			int numSources = Width * Length / 20000;
			CurrentState = "Flooding lava";
			
			for (int i = 0; i < numSources; i++) {
				CurrentProgress = (float)i / numSources;
				int x = rnd.Next(Width), z = rnd.Next(Length);
				int y = (int)((waterLevel - 3) * rnd.NextFloat() * rnd.NextFloat());
				FloodFill((y * Length + z) * Width + x, Block.Lava);
			}
		}
		
		void CreateSurfaceLayer() {
			OctaveNoise n1 = new OctaveNoise(8, rnd), n2 = new OctaveNoise(8, rnd);
			CurrentState = "Creating surface";
			// TODO: update heightmap
			
			int hMapIndex = 0;
			for (int z = 0; z < Length; z++) {
				CurrentProgress = (float)z / Length;
				for (int x = 0; x < Width; x++) {
					int yMax = heightmap[hMapIndex++];
					int yMin = heightmapMin[hMapIndex - 1];
					if (yMax < 0 || yMax >= Height) continue;
					if (yMin >= Height - 1) continue;
					if (yMin < 0) yMin = 0;
					for (int y = yMin; y <= yMax; y++) {
						
						int index = (y * Length + z) * Width + x;
						if (blocks[index] == Block.Air ||
						    blocks[index] == Block.Water || blocks[index] == Block.Lava) continue;
						BlockRaw blockAbove = y >= (Height - 1) ? Block.Air : blocks[index + oneY];
						if (blockAbove == Block.Water && (n2.Compute(x, z) > 12)) {
							blocks[index] = Block.Gravel;
						} else if (blockAbove == Block.Air) {
							blocks[index] = (y <= waterLevel && (n1.Compute(x, z) > 8)) ? Block.Sand : Block.Grass;
						}
					}
				}
			}
		}
		
		void PlantFlowers() {
			int numPatches = Width * Length / 3000;
			CurrentState = "Planting flowers";
			
			for (int i = 0; i < numPatches; i++) {
				CurrentProgress = (float)i / numPatches;
				BlockRaw type = (BlockRaw)(Block.Dandelion + rnd.Next(2));
				int patchX = rnd.Next(Width), patchZ = rnd.Next(Length);
				for (int j = 0; j < 10; j++) {
					int flowerX = patchX, flowerZ = patchZ;
					for (int k = 0; k < 5; k++) {
						flowerX += rnd.Next(6) - rnd.Next(6);
						flowerZ += rnd.Next(6) - rnd.Next(6);
						if (flowerX < 0 || flowerZ < 0 || flowerX >= Width || flowerZ >= Length)
							continue;
						
						int flowerY = heightmap[flowerZ * Width + flowerX] + 1;
						if (flowerY <= 0 || flowerY >= Height) continue;
						
						int index = (flowerY * Length + flowerZ) * Width + flowerX;
						if (blocks[index] == Block.Air && blocks[index - oneY] == Block.Grass)
							blocks[index] = type;
					}
				}
			}
		}
		
		void PlantMushrooms() {
			int numPatches = blocks.Length / 2000;
			CurrentState = "Planting mushrooms";
			
			for (int i = 0; i < numPatches; i++) {
				CurrentProgress = (float)i / numPatches;
				BlockRaw type = (BlockRaw)(Block.BrownMushroom + rnd.Next(2));
				int patchX = rnd.Next(Width);
				int patchY = rnd.Next(Height);
				int patchZ = rnd.Next(Length);
				
				for (int j = 0; j < 20; j++) {
					int mushX = patchX, mushY = patchY, mushZ = patchZ;
					for (int k = 0; k < 5; k++) {
						mushX += rnd.Next(6) - rnd.Next(6);
						mushZ += rnd.Next(6) - rnd.Next(6);
						if (mushX < 0 || mushZ < 0 || mushX >= Width || mushZ >= Length)
							continue;
						int solidHeight = heightmap[mushZ * Width + mushX];
						if (mushY >= (solidHeight - 1))
							continue;
						
						int index = (mushY * Length + mushZ) * Width + mushX;
						if (blocks[index] == Block.Air && blocks[index - oneY] == Block.Stone)
							blocks[index] = type;
					}
				}
			}
		}
		
		void PlantTrees() {
			int numPatches = Width * Length / 4000;
			CurrentState = "Planting trees";
			
			for (int i = 0; i < numPatches; i++) {
				CurrentProgress = (float)i / numPatches;
				int patchX = rnd.Next(Width), patchZ = rnd.Next(Length);
				
				for (int j = 0; j < 20; j++) {
					int treeX = patchX, treeZ = patchZ;
					for (int k = 0; k < 20; k++) {
						treeX += rnd.Next(6) - rnd.Next(6);
						treeZ += rnd.Next(6) - rnd.Next(6);
						if (treeX < 0 || treeZ < 0 || treeX >= Width ||
						    treeZ >= Length || rnd.NextFloat() >= 0.25)
							continue;
						
						int treeY = heightmap[treeZ * Width + treeX] + 1;
						if (treeY >= Height) continue;
						int treeHeight = 5 + rnd.Next(3);
						
						int index = (treeY * Length + treeZ) * Width + treeX;
						BlockRaw blockUnder = treeY > 0 ? blocks[index - oneY] : Block.Air;
						
						if (blockUnder == Block.Grass && CanGrowTree(treeX, treeY, treeZ, treeHeight)) {
							GrowTree(treeX, treeY, treeZ, treeHeight);
						}
					}
				}
			}
		}
		
		bool CanGrowTree(int treeX, int treeY, int treeZ, int treeHeight) {
			// check tree base
			int baseHeight = treeHeight - 4;
			for (int y = treeY; y < treeY + baseHeight; y++)
				for (int z = treeZ - 1; z <= treeZ + 1; z++)
					for (int x = treeX - 1; x <= treeX + 1; x++)
			{
				if (x < 0 || y < 0 || z < 0 || x >= Width || y >= Height || z >= Length)
					return false;
				int index = (y * Length + z) * Width + x;
				if (blocks[index] != 0) return false;
			}
			
			// and also check canopy
			for (int y = treeY + baseHeight; y < treeY + treeHeight; y++)
				for (int z = treeZ - 2; z <= treeZ + 2; z++)
					for (int x = treeX - 2; x <= treeX + 2; x++)
			{
				if (x < 0 || y < 0 || z < 0 || x >= Width || y >= Height || z >= Length)
					return false;
				int index = (y * Length + z) * Width + x;
				if (blocks[index] != 0) return false;
			}
			return true;
		}
		
		void GrowTree(int treeX, int treeY, int treeZ, int height) {
			int baseHeight = height - 4;
			int index = 0;
			
			// leaves bottom layer
			for (int y = treeY + baseHeight; y < treeY + baseHeight + 2; y++)
				for (int zz = -2; zz <= 2; zz++)
					for (int xx = -2; xx <= 2; xx++)
			{
				int x = xx + treeX, z = zz + treeZ;
				index = (y * Length + z) * Width + x;
				
				if (Math.Abs(xx) == 2 && Math.Abs(zz) == 2) {
					if (rnd.NextFloat() >= 0.5)
						blocks[index] = Block.Leaves;
				} else {
					blocks[index] = Block.Leaves;
				}
			}
			
			// leaves top layer
			int bottomY = treeY + baseHeight + 2;
			for (int y = treeY + baseHeight + 2; y < treeY + height; y++)
				for (int zz = -1; zz <= 1; zz++)
					for (int xx = -1; xx <= 1; xx++)
			{
				int x = xx + treeX, z = zz + treeZ;
				index = (y * Length + z) * Width + x;

				if (xx == 0 || zz == 0) {
					blocks[index] = Block.Leaves;
				} else if (y == bottomY && rnd.NextFloat() >= 0.5) {
					blocks[index] = Block.Leaves;
				}
			}
			
			// then place trunk
			index = (treeY * Length + treeZ) * Width + treeX;
			for (int y = 0; y < height - 1; y++) {
				blocks[index] = Block.Log;
				index += oneY;
			}
		}
	}
}
