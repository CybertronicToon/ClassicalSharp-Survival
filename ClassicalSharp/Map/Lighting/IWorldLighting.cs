// Copyright 2014-2017 ClassicalSharp | Licensed under BSD-3
using System;
using System.Collections.Generic;
using OpenTK;
using BlockID = System.UInt16;
using BlockRaw = System.Byte;

namespace ClassicalSharp.Map {
	
	/// <summary> Manages lighting for each block in the world.  </summary>
	public abstract class IWorldLighting : IGameComponent {

		protected internal short[] heightmap;
		public int Outside, OutsideZSide, OutsideXSide, OutsideYBottom;
		protected int width, height, length;
		public byte[] lightMap;
		public float[] lightTable = new float[] {0.05f, 0.067f, 0.085f, 0.106f,
			0.129f, 0.156f, 0.186f, 0.221f, 0.261f, 0.309f, 0.367f, 0.437f, 0.525f, 0.638f, 0.789f, 1.0f};
		public int[] xSides, ySidesBottom, ySidesTop, zSides;
		public Queue<int> BlockLightQueue;
		public Queue<LightObj> BlockLightRemovalQueue;
		public Queue<int> SunLightQueue;
		public Queue<LightObj> SunLightRemovalQueue;
		
		public struct LightObj {
			public LightObj(int i, byte v) {
				index = i;
				val = v;
			}
			public int index;
			public byte val;
		}
		
		public byte sunLightSub = 0;
		
		public virtual int GetSunLight(World map) {
			return map.Env.Sun;
		}
		
		// Equivalent to
		// for x = startX; x < startX + 18; x++
		//    for z = startZ; z < startZ + 18; z++
		//       CalcHeightAt(x, maxY, z) if height == short.MaxValue
		// Except this function is a lot more optimised and minimises cache misses.
		public unsafe abstract void LightHint(int startX, int startZ, BlockRaw* mapPtr);
		
		/// <summary> Called when a block is changed, to update the lighting information. </summary>
		/// <remarks> Derived classes ***MUST*** mark all chunks affected by this lighting change
		/// as needing to be refreshed. </remarks>
		public abstract void OnBlockChanged(int x, int y, int z, BlockID oldBlock, BlockID newBlock);
		
		/// <summary> Discards all cached lighting information. </summary>
		public virtual void Refresh() { }
		
		
		/// <summary> Returns whether the block at the given coordinates is fully in sunlight. </summary>
		/// <remarks> *** Does NOT check that the coordinates are inside the map. *** </remarks>
		public abstract bool IsLit(int x, int y, int z);

		/// <summary> Returns the light colour of the block at the given coordinates. </summary>
		/// <remarks> *** Does NOT check that the coordinates are inside the map. *** </remarks>
		public abstract int LightCol(int x, int y, int z);

		/// <summary> Returns the light colour of the block at the given coordinates. </summary>
		/// <remarks> *** Does NOT check that the coordinates are inside the map. *** 
		/// NOTE: This actually returns X shaded colour, but is called ZSide to avoid breaking compatibility. </remarks>
		public abstract int LightCol_ZSide(int x, int y, int z);
		

		public abstract int LightCol_Sprite_Fast(int x, int y, int z);		
		public abstract int LightCol_YTop_Fast(int x, int y, int z);
		public abstract int LightCol_YBottom_Fast(int x, int y, int z);
		public abstract int LightCol_XSide_Fast(int x, int y, int z);
		public abstract int LightCol_ZSide_Fast(int x, int y, int z);

		public virtual byte GetLightFromSun(int val) {
			return (byte)(lightMap[val]>>4);
		}
		public virtual byte GetLightFromBlock(int val) {
			return (byte)(lightMap[val]&15);
		}
		public virtual void SetLightFromSun(int loc, byte val) {
			lightMap[loc] = (byte)((lightMap[loc]&15) | (val<<4));
		}
		public virtual void SetLightFromSun(int x, int y, int z, byte val) {
			SetLightFromSun(((y * length + z) * width + x), val);
		}
		public virtual void SetLightFromBlock(int loc, byte val) {
			lightMap[loc] = (byte)((val&0x0f) | (lightMap[loc]&0xf0));
		}
		public virtual void SetLightFromBlock(int x, int y, int z, byte val) {
			SetLightFromBlock(((y * length + z) * width + x), val);
		}
		
		public virtual byte GetHighestLight(int val) {
			byte sunLight = GetLightFromSun(val);
			byte blockLight = GetLightFromBlock(val);
			if (sunLightSub > 0) {
				if (sunLightSub <= sunLight) {
					sunLight = (byte)(sunLight - sunLightSub);
				} else {
					sunLight = 0;
				}
			}
			return (sunLight < blockLight ? blockLight : sunLight);
		}
			

		public virtual void Dispose() { }
		public virtual void Reset(Game game) { }
		public virtual void OnNewMap(Game game) { }
		public virtual void OnNewMapLoaded(Game game) { }
		public virtual void Init(Game game) {  }
		public virtual void Ready(Game game) { }
	}
}
