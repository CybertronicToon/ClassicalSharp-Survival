// Copyright 2014-2017 ClassicalSharp | Licensed under BSD-3
using System;
using System.Collections.Generic;
using ClassicalSharp.Events;
using BlockID = System.UInt16;
using BlockRaw = System.Byte;

namespace ClassicalSharp.Map {
	
	/// <summary> Manages lighting through a simple heightmap, where each block is either in sun or shadow. </summary>
	public sealed partial class BasicLighting : IWorldLighting {
		
		int oneY, shadow, shadowZSide, shadowXSide, shadowYBottom;
		Game game;
		
		public override void Reset(Game game) { heightmap = null; }
		
		public override void OnNewMap(Game game) {
			SetSun(WorldEnv.DefaultSunlight);
			SetShadow(WorldEnv.DefaultShadowlight);
			heightmap = null;
			lightMap = null;
			SunLightQueue = null;
			SunLightRemovalQueue = null;
			BlockLightQueue = null;
			BlockLightRemovalQueue = null;
		}
		
		public override void OnNewMapLoaded(Game game) {
			width = game.World.Width;
			height = game.World.Height;
			length = game.World.Length;
			this.game = game;
			oneY = width * length;
			
			heightmap = new short[width * length];
			lightMap = new Byte[width * length * height];
			SunLightQueue = new Queue<int>();
			SunLightRemovalQueue = new Queue<LightObj>();
			BlockLightQueue = new Queue<int>();
			BlockLightRemovalQueue = new Queue<LightObj>();
			Refresh();
			CreateSunLight();
			CreateBlockLight();
		}
		
		public override void Init(Game game) {
			this.game = game;
			game.WorldEvents.EnvVariableChanged += EnvVariableChanged;
			SetSun(WorldEnv.DefaultSunlight);
			SetShadow(WorldEnv.DefaultShadowlight);
		}
		
		public override void Dispose() {
			if (game != null)
				game.WorldEvents.EnvVariableChanged -= EnvVariableChanged;
			heightmap = null;
		}

		void EnvVariableChanged(object sender, EnvVarEventArgs e) {
			if (e.Var == EnvVar.SunlightColour) {
				SetSun(game.World.Env.Sunlight);
			} else if (e.Var == EnvVar.ShadowlightColour) {
				SetShadow(game.World.Env.Shadowlight);
			}
		}
		
		void SetSun(FastColour col) {
			Outside = col.Pack();
			FastColour.GetShaded(col, out OutsideXSide, out OutsideZSide, out OutsideYBottom);
			SetLights();
		}
		
		void SetShadow(FastColour col) {
			shadow = col.Pack();
			FastColour.GetShaded(col, out shadowXSide, out shadowZSide, out shadowYBottom);
			SetLights();
		}
		
		void SetLights() {
			FastColour sunCol = FastColour.Unpack(Outside);
			FastColour ShadowCol = FastColour.Unpack(shadow);
			xSides = new int[16];
			ySidesBottom = new int[16];
			ySidesTop = new int[16];
			zSides = new int[16];
			FastColour lightCol;
			
			for (int i = 0; i <= 15; i++) {
				lightCol = FastColour.Lerp(ShadowCol, sunCol, lightTable[i]);
				FastColour.GetShaded(lightCol, out xSides[i], out zSides[i], out ySidesBottom[i]);
				ySidesTop[i] = lightCol.Pack();
			}
			
		}
		
		
		public unsafe override void LightHint(int startX, int startZ, BlockRaw* mapPtr) {
			int x1 = Math.Max(startX, 0), x2 = Math.Min(width, startX + 18);
			int z1 = Math.Max(startZ, 0), z2 = Math.Min(length, startZ + 18);
			int xCount = x2 - x1, zCount = z2 - z1;
			int* skip = stackalloc int[xCount * zCount];
			
			int elemsLeft = InitialHeightmapCoverage(x1, z1, xCount, zCount, skip);
			if (!CalculateHeightmapCoverage(x1, z1, xCount, zCount, elemsLeft, skip, mapPtr)) {
				FinishHeightmapCoverage(x1, z1, xCount, zCount, skip);
			}
		}
		
		int GetLightHeight(int x, int z) {
			int index = (z * width) + x;
			int lightH = heightmap[index];
			return lightH == short.MaxValue ? CalcHeightAt(x, height - 1, z, index) : lightH;
		}
		
		
		// Outside colour is same as sunlight colour, so we reuse when possible
		public override bool IsLit(int x, int y, int z) {
			return y > GetLightHeight(x, z);
		}

		public override int LightCol(int x, int y, int z) {
			//return y > GetLightHeight(x, z) ? Outside : shadow;
			//if (y >= game.World.MaxY) return Outside;
			int lightloc = (y * length + z) * width + x;
			byte light = GetHighestLight(lightloc);
			return ySidesTop[light];
		}
		
		public override int LightCol_ZSide(int x, int y, int z) {
			//return y > GetLightHeight(x, z) ? OutsideXSide : shadowXSide;
			int lightloc = (y * length + z) * width + x;
			byte light = GetHighestLight(lightloc);
			return zSides[light];
		}
		

		public override int LightCol_Sprite_Fast(int x, int y, int z) {
			//return y > heightmap[(z * width) + x] ? Outside : shadow;
			int lightloc = (y * length + z) * width + x;
			byte light = GetHighestLight(lightloc);
			return ySidesTop[light];
		}
		
		public override int LightCol_YTop_Fast(int x, int y, int z) {
			/*if (y < game.World.MaxY) {
				BlockID curBlock = game.World.blocks2[((y + 1) * length + z) * width + x];
				if (curBlock == Block.Water || curBlock == Block.StillWater) {
					return y + 2 > heightmap[(z * width) + x] + 1 ? Outside : shadow;
				}
			}*/
			//return y > heightmap[(z * width) + x] ? Outside : shadow;
			if (y >= game.World.MaxY) return Outside;
			int lightloc = ((y + 1) * length + z) * width + x;
			byte light = GetHighestLight(lightloc);
			return ySidesTop[light];
			
		}
		
		public override int LightCol_YBottom_Fast(int x, int y, int z) {
			//return y > heightmap[(z * width) + x] ? OutsideYBottom : shadowYBottom;
			if (y <= 0) return shadowYBottom;
			int lightloc = (y * length + z) * width + x;
			byte light = GetHighestLight(lightloc);
			return ySidesBottom[light];
		}
		
		public override int LightCol_XSide_Fast(int x, int y, int z) {
			//return y > heightmap[(z * width) + x] ? OutsideXSide : shadowXSide;
			int lightloc = (y * length + z) * width + x;
			byte light = GetHighestLight(lightloc);
			return xSides[light];
		}
		
		public override int LightCol_ZSide_Fast(int x, int y, int z) {
			//return y > heightmap[(z * width) + x] ? OutsideZSide : shadowZSide;
			int lightloc = (y * length + z) * width + x;
			byte light = GetHighestLight(lightloc);
			return zSides[light];
		}
		
		public void CreateSunLight() {
			Console.WriteLine("Creating Sunlight");
			int top = height - 1;
			/*for (int x = 0; x < width; x++)
				for(int z = 0; z < length; z++) {
				int lightloc = (top * length + z) * width + x;
				SetLightFromSun(lightloc, (byte)15);
				SunLightQueue.Enqueue(lightloc);
			}*/
			
			short[] sunMap = new short[width * length];
			short yHeight;
			BlockID block;
			for (int x = 0; x < width; x++)
				for (int z = 0; z < length; z++) {
				yHeight = (short)game.World.MaxY;
				for (int y = 0; y < height; y++) {
					block = game.World.GetBlock(x, yHeight, z);
					if (BlockInfo.BlocksLight[block] || block == Block.Leaves) break;
					yHeight -= 1;
					continue;
				}
				sunMap[(z * width) + x] = yHeight;
			}
			
			for (int x = 0; x < width; x++)
				for (int z = 0; z < length; z++) {
				short xLo = 0;
				short xHi = 0;
				short zLo = 0;
				short zHi = 0;
				if (x > 0) {
					xLo = sunMap[(z * width) + (x - 1)];
				} if (x < game.World.MaxX) {
					xHi = sunMap[(z * width) + (x + 1)];
				} if (z > 0) {
					zLo = sunMap[((z - 1) * width) + x];
				} if (z < game.World.MaxZ) {
					zLo = sunMap[((z + 1) * width) + x];
				}
				
				short xMax = Math.Max(xLo, xHi);
				short zMax = Math.Max(zLo, zHi);
				short max = Math.Max(xMax, zMax);
				short cur = sunMap[(z * width) + x];
				//if (cur == -1) cur = 0;
				if (cur == game.World.MaxY) continue;
				cur += 1;
				if (max > cur) {
					for (int y = cur; y <= max; y++) {
						int lightloc = (y * length + z) * width + x;
						SetLightFromSun(lightloc, 15);
						SunLightQueue.Enqueue(lightloc);
					}
					if (max < game.World.MaxY) {
						for (int y = (max + 1); y <= game.World.MaxY; y++) {
							int lightloc = (y * length + z) * width + x;
							SetLightFromSun(lightloc, 15);
						}
					}
				} else {
					for (int y = cur; y <= game.World.MaxY; y++) {
						int lightloc = (y * length + z) * width + x;
						SetLightFromSun(lightloc, 15);
						if (y == cur) SunLightQueue.Enqueue(lightloc);
					}
				}
			}
			
			UpdateSunlight();
		}
		
		public void UpdateSunlight() {
			while (SunLightRemovalQueue.Count > 0) {
				LightObj curLightObj = SunLightRemovalQueue.Peek();
				int light = curLightObj.index;
				byte curLight = curLightObj.val;
				SunLightRemovalQueue.Dequeue();
				
				int x = light % width;
				int y = light / oneY; // posIndex / (width * length)
				int z = (light / width) % length;
				
				RefreshChunk(x, y, z);
				
				if (x > 0) {
					ushort block = game.World.GetBlock(x - 1, y, z);
					int blockLoc = (y * length + z) * width + (x - 1);
					byte blockLevel = GetLightFromSun(blockLoc);
					byte lFac = 1; // Lighting factor
					if (block == Block.Water || block == Block.StillWater || block == Block.Ice) lFac = 2;
					if (blockLevel != 0 && blockLevel < curLight) {
						SetLightFromSun(blockLoc, 0);
						SunLightRemovalQueue.Enqueue(new LightObj(blockLoc, blockLevel));
					} else if (blockLevel >= curLight) {
						SunLightQueue.Enqueue(blockLoc);
					}
				}
				if (x < width - 1) {
					ushort block = game.World.GetBlock(x + 1, y, z);
					int blockLoc = (y * length + z) * width + (x + 1);
					byte blockLevel = GetLightFromSun(blockLoc);
					byte lFac = 1; // Lighting factor
					if (block == Block.Water || block == Block.StillWater || block == Block.Ice) lFac = 2;
					if (blockLevel != 0 && blockLevel < curLight) {
						SetLightFromSun(blockLoc, 0);
						SunLightRemovalQueue.Enqueue(new LightObj(blockLoc, blockLevel));
					} else if (blockLevel >= curLight) {
						SunLightQueue.Enqueue(blockLoc);
					}
				}
				if (z > 0) {
					ushort block = game.World.GetBlock(x, y, z - 1);
					int blockLoc = ((y * length + (z - 1)) * width + x);
					byte blockLevel = GetLightFromSun(blockLoc);
					byte lFac = 1; // Lighting factor
					if (block == Block.Water || block == Block.StillWater || block == Block.Ice) lFac = 2;
					if (blockLevel != 0 && blockLevel < curLight) {
						SetLightFromSun(blockLoc, 0);
						SunLightRemovalQueue.Enqueue(new LightObj(blockLoc, blockLevel));
					} else if (blockLevel >= curLight) {
						SunLightQueue.Enqueue(blockLoc);
					}
				}
				if (z < length - 1) {
					ushort block = game.World.GetBlock(x, y, z + 1);
					int blockLoc = ((y * length + (z + 1)) * width + x);
					byte blockLevel = GetLightFromSun(blockLoc);
					byte lFac = 1; // Lighting factor
					if (block == Block.Water || block == Block.StillWater || block == Block.Ice) lFac = 2;
					if (blockLevel != 0 && blockLevel < curLight) {
						SetLightFromSun(blockLoc, 0);
						SunLightRemovalQueue.Enqueue(new LightObj(blockLoc, blockLevel));
					} else if (blockLevel >= curLight) {
						SunLightQueue.Enqueue(blockLoc);
					}
				}
				if (y > 0) {
					ushort block = game.World.GetBlock(x, y - 1, z);
					int blockLoc = (((y - 1) * length + z) * width + x);
					byte blockLevel = GetLightFromSun(blockLoc);
					byte lFac = 1; // Lighting factor
					if (block == Block.Water || block == Block.StillWater || block == Block.Ice) lFac = 2;
					if (blockLevel != 0 && blockLevel < curLight) {
						SetLightFromSun(blockLoc, 0);
						SunLightRemovalQueue.Enqueue(new LightObj(blockLoc, blockLevel));
					} else if (blockLevel == 15 && curLight == 15) {
						SetLightFromSun(blockLoc, 0);
						SunLightRemovalQueue.Enqueue(new LightObj(blockLoc, blockLevel));
					} else if (blockLevel >= curLight) {
						SunLightQueue.Enqueue(blockLoc);
					}
				}
				if (y < height - 1) {
					ushort block = game.World.GetBlock(x, y + 1, z);
					int blockLoc = (((y + 1) * length + z) * width + x);
					byte blockLevel = GetLightFromSun(blockLoc);
					byte lFac = 1; // Lighting factor
					if (block == Block.Water || block == Block.StillWater || block == Block.Ice) lFac = 2;
					if (blockLevel != 0 && blockLevel < curLight) {
						SetLightFromSun(blockLoc, 0);
						SunLightRemovalQueue.Enqueue(new LightObj(blockLoc, blockLevel));
					} else if (blockLevel >= curLight) {
						SunLightQueue.Enqueue(blockLoc);
					}
				}
				
			}
			
			while (SunLightQueue.Count > 0) {
				int light = SunLightQueue.Peek();
				byte curLight = GetLightFromSun(light);
				SunLightQueue.Dequeue();
				
				int x = light % width;
				int y = light / oneY; // posIndex / (width * length)
				int z = (light / width) % length;
				
				byte xLiLo = 0; // xLightLow
				byte xLiHi = 0; // xLightHigh
				byte yLiLo = 0;
				byte yLiHi = 0;
				byte zLiLo = 0;
				byte zLiHi = 0;
				
				RefreshChunk(x, y, z);
				
				bool isLess = false;
				
				/* ==== REMOVAL ==== */
				
				if (x > 0) {
					ushort block = game.World.GetBlock(x - 1, y, z);
					int blockLoc = (y * length + z) * width + (x - 1);
					if (BlockInfo.BlocksLight[block]) {
						xLiLo = 0;
					} else {
						xLiLo = GetLightFromSun(blockLoc);
					}
				}
				if (x < width - 1) {
					ushort block = game.World.GetBlock(x + 1, y, z);
					int blockLoc = (y * length + z) * width + (x + 1);
					if (BlockInfo.BlocksLight[block]) {
						xLiHi = 0;
					} else {
						xLiHi = GetLightFromSun(blockLoc);
					}
				}
				if (z > 0) {
					ushort block = game.World.GetBlock(x, y, z - 1);
					int blockLoc = ((y * length + (z - 1)) * width + x);
					if (BlockInfo.BlocksLight[block]) {
						zLiLo = 0;
					} else {
						zLiLo = GetLightFromSun(blockLoc);
					}
				}
				if (z < length - 1) {
					ushort block = game.World.GetBlock(x, y, z + 1);
					int blockLoc = ((y * length + (z + 1)) * width + x);
					if (BlockInfo.BlocksLight[block]) {
						zLiHi = 0;
					} else {
						zLiHi = GetLightFromSun(blockLoc);
					}
				}
				if (y > 0) {
					ushort block = game.World.GetBlock(x, y - 1, z);
					int blockLoc = (((y - 1) * length + z) * width + x);
					if (BlockInfo.BlocksLight[block]) {
						yLiLo = 0;
					} else {
						yLiLo = GetLightFromSun(blockLoc);
					}
				}
				if (y < height - 1) {
					ushort block = game.World.GetBlock(x, y + 1, z);
					int blockLoc = (((y + 1) * length + z) * width + x);
					if (BlockInfo.BlocksLight[block]) {
						yLiHi = 0;
					} else {
						yLiHi = GetLightFromSun(blockLoc);
					}
				} else if (y == height - 1) {
					yLiHi = 15;
				}
				
				byte minLight = curLight;
				if (minLight != 15) minLight += 1;
				
				/*byte xMax = Math.Max(xLiLo, xLiHi);
				byte yMax = Math.Max(yLiLo, yLiHi);
				byte zMax = Math.Max(zLiLo, zLiHi);
				byte preMax = Math.Max(xMax, yMax);
				byte max = Math.Max(preMax, zMax);
				
				bool isLess = false;
				
				if (max < minLight) {
					if (max > 0) {
						SetLightFromSun(light, (byte)(max - 1));
						curLight = (byte)(max - 1);
					} else {
						SetLightFromSun(light, max);
						curLight = 0;
						continue;
					}
					isLess = true;
				}
				
				else if (xLiLo < minLight || xLiHi < minLight || zLiLo < minLight || zLiHi < minLight
				    || yLiLo < minLight || yLiHi < minLight) {
					if (minLight == 15) {
						if (yLiHi != 15 && curLight == 15 && y != height - 1) {
							if (max != 0) {
							SetLightFromSun(light, (byte)(max - 1));
							} else {
								SetLightFromSun(light, (byte)0);
							}
							byte maxLo;
							if (max > 1) {
								maxLo = (byte)(max - 2);
							} else {
								maxLo = 0;
							}
							if (y > 0) {
								SunLightQueue.Enqueue((((y - 1) * length + z) * width + x));
							}
							if (y < height && yLiHi < maxLo) {
								//SunLightQueue.Enqueue((((y + 1) * length + z) * width + x));
							}
							if (z > 0 && zLiLo < maxLo) {
								SunLightQueue.Enqueue(((y * length + (z - 1)) * width + x));
							}
							if (z < zMax && zLiHi < maxLo) {
								SunLightQueue.Enqueue(((y * length + (z + 1)) * width + x));
							}
							if (x > 0 && xLiLo < maxLo) {
								SunLightQueue.Enqueue((y * length + z) * width + (x - 1));
							}
							if (x < xMax && xLiHi < maxLo) {
								SunLightQueue.Enqueue((y * length + z) * width + (x + 1));
							}
							continue;
						}
					}
					
				}*/ if (curLight == 0 && !BlockInfo.BlocksLight[game.World.GetBlock(x, y, z)]) {
					byte xMax = Math.Max(xLiLo, xLiHi);
					byte yMax = Math.Max(yLiLo, yLiHi);
					byte zMax = Math.Max(zLiLo, zLiHi);
					byte preMax = Math.Max(xMax, yMax);
					byte max = Math.Max(preMax, zMax);
					if (yLiHi == 15) {
						SetLightFromSun(light, 15);
						curLight = 15;
					} else if (max > curLight) {
						SetLightFromSun(light, (byte)(max - 1));
						curLight = (byte)(max - 1);
					}
				}
				
				/* ==== PROPOGATION ==== */
				
				if (x > 0) {
					ushort block = game.World.GetBlock(x - 1, y, z);
					int blockLoc = (y * length + z) * width + (x - 1);
					byte lFac = 1; // Lighting factor
					if (block == Block.Water || block == Block.StillWater || block == Block.Ice) lFac = 2;
					if (!BlockInfo.BlocksLight[block] || lFac != 1) {
						byte blockLight = GetLightFromSun(blockLoc);
						if (blockLight + 2 <= curLight) {
							SetLightFromSun((int)blockLoc, (byte)(curLight - lFac));
							SunLightQueue.Enqueue(blockLoc);
						} else if (isLess) {
							SunLightQueue.Enqueue(blockLoc);
						}
					}
				}
				if (x < width - 1) {
					ushort block = game.World.GetBlock(x + 1, y, z);
					int blockLoc = (y * length + z) * width + (x + 1);
					byte lFac = 1; // Lighting factor
					if (block == Block.Water || block == Block.StillWater || block == Block.Ice) lFac = 2;
					if (!BlockInfo.BlocksLight[block] || lFac != 1) {
						byte blockLight = GetLightFromSun(blockLoc);
						if (blockLight + 2 <= curLight) {
							SetLightFromSun((int)blockLoc, (byte)(curLight - lFac));
							SunLightQueue.Enqueue(blockLoc);
						} else if (isLess) {
							SunLightQueue.Enqueue(blockLoc);
						}
					}
				}
				if (z > 0) {
					ushort block = game.World.GetBlock(x, y, z - 1);
					int blockLoc = ((y * length + (z - 1)) * width + x);
					byte lFac = 1; // Lighting factor
					if (block == Block.Water || block == Block.StillWater || block == Block.Ice) lFac = 2;
					if (!BlockInfo.BlocksLight[block] || lFac != 1) {
						byte blockLight = GetLightFromSun(blockLoc);
						if (blockLight + 2 <= curLight) {
							SetLightFromSun((int)blockLoc, (byte)(curLight - lFac));
							SunLightQueue.Enqueue(blockLoc);
						} else if (isLess) {
							SunLightQueue.Enqueue(blockLoc);
						}
					}
				}
				if (z < length - 1) {
					ushort block = game.World.GetBlock(x, y, z + 1);
					int blockLoc = ((y * length + (z + 1)) * width + x);
					byte lFac = 1; // Lighting factor
					if (block == Block.Water || block == Block.StillWater || block == Block.Ice) lFac = 2;
					if (!BlockInfo.BlocksLight[block] || lFac != 1) {
						byte blockLight = GetLightFromSun(blockLoc);
						if (blockLight + 2 <= curLight) {
							SetLightFromSun((int)blockLoc, (byte)(curLight - lFac));
							SunLightQueue.Enqueue(blockLoc);
						} else if (isLess) {
							SunLightQueue.Enqueue(blockLoc);
						}
					}
				}
				if (y > 0) {
					ushort block = game.World.GetBlock(x, y - 1, z);
					int blockLoc = (((y - 1) * length + z) * width + x);
					byte lFac = 1; // Lighting factor
					if (block == Block.Water || block == Block.StillWater || block == Block.Ice) lFac = 2;
					if (!BlockInfo.BlocksLight[block] || lFac != 1) {
						byte blockLight = GetLightFromSun(blockLoc);
						//if (blockLight + 2 <= curLight) {
						if (curLight == (byte)15 && lFac == 1 && block != Block.Leaves) {
							SetLightFromSun((int)blockLoc, (byte)15);
							SunLightQueue.Enqueue(blockLoc);
						} else if (blockLight + 2 <= curLight && curLight > 0) {
							SetLightFromSun((int)blockLoc, (byte)(curLight - lFac));
							SunLightQueue.Enqueue(blockLoc);
						}
						//}
					}
				}
				if (y < height - 1) {
					ushort block = game.World.GetBlock(x, y + 1, z);
					int blockLoc = (((y + 1) * length + z) * width + x);
					byte lFac = 1; // Lighting factor
					if (block == Block.Water || block == Block.StillWater || block == Block.Ice) lFac = 2;
					if (!BlockInfo.BlocksLight[block] || lFac != 1) {
						byte blockLight = GetLightFromSun(blockLoc);
						if (blockLight + 2 <= curLight) {
							SetLightFromSun((int)blockLoc, (byte)(curLight - lFac));
							SunLightQueue.Enqueue(blockLoc);
							
						} if (isLess) {
							SunLightQueue.Enqueue(blockLoc);
						}
					}
				}
			}
		}
		
		public void CreateBlockLight() {
			Console.WriteLine("Creating BlockLight");
			int top = height - 1;
			/*for (int x = 0; x < width; x++)
				for(int z = 0; z < length; z++) {
				int lightloc = (0 * length + z) * width + x;
				SetLightFromBlock(lightloc, (byte)15);
				BlockLightQueue.Enqueue(lightloc);
			}*/
			int lightloc = 0;
			for (int x = 0; x < width; x++)
				for (int y = 0; y < height; y++)
					for (int z = 0; z < length; z++) {
				if (BlockInfo.FullBright[game.World.GetBlock(x, y, z)]) {
					lightloc = (y * length + z) * width + x;
					SetLightFromBlock(lightloc, 15);
					BlockLightQueue.Enqueue(lightloc);
				}
			}
			
			UpdateBlockLight();
		}
		
		public void UpdateBlockLight() {
			while (BlockLightRemovalQueue.Count > 0) {
				LightObj curLightObj = BlockLightRemovalQueue.Peek();
				int light = curLightObj.index;
				byte curLight = curLightObj.val;
				BlockLightRemovalQueue.Dequeue();
				
				int x = light % width;
				int y = light / oneY; // posIndex / (width * length)
				int z = (light / width) % length;
				
				RefreshChunk(x, y, z);
				
				if (x > 0) {
					ushort block = game.World.GetBlock(x - 1, y, z);
					int blockLoc = (y * length + z) * width + (x - 1);
					byte blockLevel = GetLightFromBlock(blockLoc);
					byte lFac = 1; // Lighting factor
					if (block == Block.Water || block == Block.StillWater || block == Block.Ice) lFac = 2;
					if (blockLevel != 0 && blockLevel < curLight) {
						SetLightFromBlock(blockLoc, 0);
						BlockLightRemovalQueue.Enqueue(new LightObj(blockLoc, blockLevel));
					} else if (blockLevel >= curLight) {
						BlockLightQueue.Enqueue(blockLoc);
					}
				}
				if (x < width - 1) {
					ushort block = game.World.GetBlock(x + 1, y, z);
					int blockLoc = (y * length + z) * width + (x + 1);
					byte blockLevel = GetLightFromBlock(blockLoc);
					byte lFac = 1; // Lighting factor
					if (block == Block.Water || block == Block.StillWater || block == Block.Ice) lFac = 2;
					if (blockLevel != 0 && blockLevel < curLight) {
						SetLightFromBlock(blockLoc, 0);
						BlockLightRemovalQueue.Enqueue(new LightObj(blockLoc, blockLevel));
					} else if (blockLevel >= curLight) {
						BlockLightQueue.Enqueue(blockLoc);
					}
				}
				if (z > 0) {
					ushort block = game.World.GetBlock(x, y, z - 1);
					int blockLoc = ((y * length + (z - 1)) * width + x);
					byte blockLevel = GetLightFromBlock(blockLoc);
					byte lFac = 1; // Lighting factor
					if (block == Block.Water || block == Block.StillWater || block == Block.Ice) lFac = 2;
					if (blockLevel != 0 && blockLevel < curLight) {
						SetLightFromBlock(blockLoc, 0);
						BlockLightRemovalQueue.Enqueue(new LightObj(blockLoc, blockLevel));
					} else if (blockLevel >= curLight) {
						BlockLightQueue.Enqueue(blockLoc);
					}
				}
				if (z < length - 1) {
					ushort block = game.World.GetBlock(x, y, z + 1);
					int blockLoc = ((y * length + (z + 1)) * width + x);
					byte blockLevel = GetLightFromBlock(blockLoc);
					byte lFac = 1; // Lighting factor
					if (block == Block.Water || block == Block.StillWater || block == Block.Ice) lFac = 2;
					if (blockLevel != 0 && blockLevel < curLight) {
						SetLightFromBlock(blockLoc, 0);
						BlockLightRemovalQueue.Enqueue(new LightObj(blockLoc, blockLevel));
					} else if (blockLevel >= curLight) {
						BlockLightQueue.Enqueue(blockLoc);
					}
				}
				if (y > 0) {
					ushort block = game.World.GetBlock(x, y - 1, z);
					int blockLoc = (((y - 1) * length + z) * width + x);
					byte blockLevel = GetLightFromBlock(blockLoc);
					byte lFac = 1; // Lighting factor
					if (block == Block.Water || block == Block.StillWater || block == Block.Ice) lFac = 2;
					if (blockLevel != 0 && blockLevel < curLight) {
						SetLightFromBlock(blockLoc, 0);
						BlockLightRemovalQueue.Enqueue(new LightObj(blockLoc, blockLevel));
					} else if (blockLevel >= curLight) {
						BlockLightQueue.Enqueue(blockLoc);
					}
				}
				if (y < height - 1) {
					ushort block = game.World.GetBlock(x, y + 1, z);
					int blockLoc = (((y + 1) * length + z) * width + x);
					byte blockLevel = GetLightFromBlock(blockLoc);
					byte lFac = 1; // Lighting factor
					if (block == Block.Water || block == Block.StillWater || block == Block.Ice) lFac = 2;
					if (blockLevel != 0 && blockLevel < curLight) {
						SetLightFromBlock(blockLoc, 0);
						BlockLightRemovalQueue.Enqueue(new LightObj(blockLoc, blockLevel));
					} else if (blockLevel >= curLight) {
						BlockLightQueue.Enqueue(blockLoc);
					}
				}
				
			}
			
			while (BlockLightQueue.Count > 0) {
				int light = BlockLightQueue.Peek();
				byte curLight = GetLightFromBlock(light);
				BlockLightQueue.Dequeue();
				
				int x = light % width;
				int y = light / oneY; // posIndex / (width * length)
				int z = (light / width) % length;
				
				byte xLiLo = 0; // xLightLow
				byte xLiHi = 0; // xLightHigh
				byte yLiLo = 0;
				byte yLiHi = 0;
				byte zLiLo = 0;
				byte zLiHi = 0;
				
				RefreshChunk(x, y, z);
				
				bool isLess = false;
				
				/* ==== REMOVAL ==== */
				
				if (x > 0) {
					ushort block = game.World.GetBlock(x - 1, y, z);
					int blockLoc = (y * length + z) * width + (x - 1);
					if (BlockInfo.BlocksLight[block] && !BlockInfo.FullBright[block]) {
						xLiLo = 0;
					} else {
						xLiLo = GetLightFromBlock(blockLoc);
					}
				}
				if (x < width - 1) {
					ushort block = game.World.GetBlock(x + 1, y, z);
					int blockLoc = (y * length + z) * width + (x + 1);
					if (BlockInfo.BlocksLight[block] && !BlockInfo.FullBright[block]) {
						xLiHi = 0;
					} else {
						xLiHi = GetLightFromBlock(blockLoc);
					}
				}
				if (z > 0) {
					ushort block = game.World.GetBlock(x, y, z - 1);
					int blockLoc = ((y * length + (z - 1)) * width + x);
					if (BlockInfo.BlocksLight[block] && !BlockInfo.FullBright[block]) {
						zLiLo = 0;
					} else {
						zLiLo = GetLightFromBlock(blockLoc);
					}
				}
				if (z < length - 1) {
					ushort block = game.World.GetBlock(x, y, z + 1);
					int blockLoc = ((y * length + (z + 1)) * width + x);
					if (BlockInfo.BlocksLight[block] && !BlockInfo.FullBright[block]) {
						zLiHi = 0;
					} else {
						zLiHi = GetLightFromBlock(blockLoc);
					}
				}
				if (y > 0) {
					ushort block = game.World.GetBlock(x, y - 1, z);
					int blockLoc = (((y - 1) * length + z) * width + x);
					if (BlockInfo.BlocksLight[block] && !BlockInfo.FullBright[block]) {
						yLiLo = 0;
					} else {
						yLiLo = GetLightFromBlock(blockLoc);
					}
				}
				if (y < height - 1) {
					ushort block = game.World.GetBlock(x, y + 1, z);
					int blockLoc = (((y + 1) * length + z) * width + x);
					if (BlockInfo.BlocksLight[block] && !BlockInfo.FullBright[block]) {
						yLiHi = 0;
					} else {
						yLiHi = GetLightFromBlock(blockLoc);
					}
				} else if (y == height - 1) {
					yLiHi = 15;
				}
				
				byte minLight = curLight;
				if (minLight != 15) minLight += 1;
				
				/*byte xMax = Math.Max(xLiLo, xLiHi);
				byte yMax = Math.Max(yLiLo, yLiHi);
				byte zMax = Math.Max(zLiLo, zLiHi);
				byte preMax = Math.Max(xMax, yMax);
				byte max = Math.Max(preMax, zMax);
				
				bool isLess = false;
				
				if (max < minLight) {
					if (max > 0) {
						SetLightFromBlock(light, (byte)(max - 1));
						curLight = (byte)(max - 1);
					} else {
						SetLightFromBlock(light, max);
						curLight = 0;
						continue;
					}
					isLess = true;
				}
				
				else if (xLiLo < minLight || xLiHi < minLight || zLiLo < minLight || zLiHi < minLight
				    || yLiLo < minLight || yLiHi < minLight) {
					if (minLight == 15) {
						if (yLiHi != 15 && curLight == 15 && y != height - 1) {
							if (max != 0) {
							SetLightFromBlock(light, (byte)(max - 1));
							} else {
								SetLightFromBlock(light, (byte)0);
							}
							byte maxLo;
							if (max > 1) {
								maxLo = (byte)(max - 2);
							} else {
								maxLo = 0;
							}
							if (y > 0) {
								BlockLightQueue.Enqueue((((y - 1) * length + z) * width + x));
							}
							if (y < height && yLiHi < maxLo) {
								//BlockLightQueue.Enqueue((((y + 1) * length + z) * width + x));
							}
							if (z > 0 && zLiLo < maxLo) {
								BlockLightQueue.Enqueue(((y * length + (z - 1)) * width + x));
							}
							if (z < zMax && zLiHi < maxLo) {
								BlockLightQueue.Enqueue(((y * length + (z + 1)) * width + x));
							}
							if (x > 0 && xLiLo < maxLo) {
								BlockLightQueue.Enqueue((y * length + z) * width + (x - 1));
							}
							if (x < xMax && xLiHi < maxLo) {
								BlockLightQueue.Enqueue((y * length + z) * width + (x + 1));
							}
							continue;
						}
					}
					
				}*/ if (curLight == 0 && !BlockInfo.BlocksLight[game.World.GetBlock(x, y, z)]) {
					byte xMax = Math.Max(xLiLo, xLiHi);
					byte yMax = Math.Max(yLiLo, yLiHi);
					byte zMax = Math.Max(zLiLo, zLiHi);
					byte preMax = Math.Max(xMax, yMax);
					byte max = Math.Max(preMax, zMax);
					if (max > curLight) {
						SetLightFromBlock(light, (byte)(max - 1));
						curLight = (byte)(max - 1);
					}
				}
				
				/* ==== PROPOGATION ==== */
				
				if (x > 0) {
					ushort block = game.World.GetBlock(x - 1, y, z);
					int blockLoc = (y * length + z) * width + (x - 1);
					byte lFac = 1; // Lighting factor
					if (block == Block.Water || block == Block.StillWater || block == Block.Ice) lFac = 2;
					if (!BlockInfo.BlocksLight[block] || lFac != 1) {
						byte blockLight = GetLightFromBlock(blockLoc);
						if (blockLight + 2 <= curLight) {
							SetLightFromBlock((int)blockLoc, (byte)(curLight - lFac));
							BlockLightQueue.Enqueue(blockLoc);
						} else if (isLess) {
							BlockLightQueue.Enqueue(blockLoc);
						}
					}
				}
				if (x < width - 1) {
					ushort block = game.World.GetBlock(x + 1, y, z);
					int blockLoc = (y * length + z) * width + (x + 1);
					byte lFac = 1; // Lighting factor
					if (block == Block.Water || block == Block.StillWater || block == Block.Ice) lFac = 2;
					if (!BlockInfo.BlocksLight[block] || lFac != 1) {
						byte blockLight = GetLightFromBlock(blockLoc);
						if (blockLight + 2 <= curLight) {
							SetLightFromBlock((int)blockLoc, (byte)(curLight - lFac));
							BlockLightQueue.Enqueue(blockLoc);
						} else if (isLess) {
							BlockLightQueue.Enqueue(blockLoc);
						}
					}
				}
				if (z > 0) {
					ushort block = game.World.GetBlock(x, y, z - 1);
					int blockLoc = ((y * length + (z - 1)) * width + x);
					byte lFac = 1; // Lighting factor
					if (block == Block.Water || block == Block.StillWater || block == Block.Ice) lFac = 2;
					if (!BlockInfo.BlocksLight[block] || lFac != 1) {
						byte blockLight = GetLightFromBlock(blockLoc);
						if (blockLight + 2 <= curLight) {
							SetLightFromBlock((int)blockLoc, (byte)(curLight - lFac));
							BlockLightQueue.Enqueue(blockLoc);
						} else if (isLess) {
							BlockLightQueue.Enqueue(blockLoc);
						}
					}
				}
				if (z < length - 1) {
					ushort block = game.World.GetBlock(x, y, z + 1);
					int blockLoc = ((y * length + (z + 1)) * width + x);
					byte lFac = 1; // Lighting factor
					if (block == Block.Water || block == Block.StillWater || block == Block.Ice) lFac = 2;
					if (!BlockInfo.BlocksLight[block] || lFac != 1) {
						byte blockLight = GetLightFromBlock(blockLoc);
						if (blockLight + 2 <= curLight) {
							SetLightFromBlock((int)blockLoc, (byte)(curLight - lFac));
							BlockLightQueue.Enqueue(blockLoc);
						} else if (isLess) {
							BlockLightQueue.Enqueue(blockLoc);
						}
					}
				}
				if (y > 0) {
					ushort block = game.World.GetBlock(x, y - 1, z);
					int blockLoc = (((y - 1) * length + z) * width + x);
					byte lFac = 1; // Lighting factor
					if (block == Block.Water || block == Block.StillWater || block == Block.Ice) lFac = 2;
					if (!BlockInfo.BlocksLight[block] || lFac != 1) {
						byte blockLight = GetLightFromBlock(blockLoc);
						if (blockLight + 2 <= curLight) {
							SetLightFromBlock((int)blockLoc, (byte)(curLight - lFac));
							BlockLightQueue.Enqueue(blockLoc);
							
						} if (isLess) {
							BlockLightQueue.Enqueue(blockLoc);
						}
					}
				}
				if (y < height - 1) {
					ushort block = game.World.GetBlock(x, y + 1, z);
					int blockLoc = (((y + 1) * length + z) * width + x);
					byte lFac = 1; // Lighting factor
					if (block == Block.Water || block == Block.StillWater || block == Block.Ice) lFac = 2;
					if (!BlockInfo.BlocksLight[block] || lFac != 1) {
						byte blockLight = GetLightFromBlock(blockLoc);
						if (blockLight + 2 <= curLight) {
							SetLightFromBlock((int)blockLoc, (byte)(curLight - lFac));
							BlockLightQueue.Enqueue(blockLoc);
							
						} if (isLess) {
							BlockLightQueue.Enqueue(blockLoc);
						}
					}
				}
			}
		}
		
		
		public  void RefreshChunk(int x, int y, int z) {
			int cx = x >> 4;
			int cy = y >> 4;
			int cz = z >> 4;
			if (game.MapRenderer.chunks == null) return;
			game.MapRenderer.RefreshChunk(cx, cy, cz);
			if ((x + 1) % 16 == 0 && x < game.World.MaxX) {
				game.MapRenderer.RefreshChunk(cx + 1, cy, cz);
			} if ((x + 1) % 16 == 1 && x > 0) {
				game.MapRenderer.RefreshChunk(cx - 1, cy, cz);
			} if ((y + 1) % 16 == 0 && y < game.World.MaxY) {
				game.MapRenderer.RefreshChunk(cx, cy + 1, cz);
			} if ((y + 1) % 16 == 1 && y > 0) {
				game.MapRenderer.RefreshChunk(cx, cy - 1, cz);
			} if ((z + 1) % 16 == 0 && z < game.World.MaxZ) {
				game.MapRenderer.RefreshChunk(cx, cy, cz + 1);
			} if ((z + 1) % 16 == 1 && z > 0) {
				game.MapRenderer.RefreshChunk(cx, cy, cz - 1);
			}
		}
		
		
		public override void Refresh() {
			for (int i = 0; i < heightmap.Length; i++)
				heightmap[i] = short.MaxValue;
		}
	}
}
