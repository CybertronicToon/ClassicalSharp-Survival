// Copyright 2014-2017 ClassicalSharp | Licensed under BSD-3
using System;
using System.Collections;
using System.Collections.Generic;
using ClassicalSharp.Map;
using ClassicalSharp.Events;
using BlockID = System.UInt16;
using BlockRaw = System.Byte;

namespace ClassicalSharp.Singleplayer {
	
	public delegate void PhysicsAction(int index, BlockRaw block);
	
	public struct PhysicsTick {
		public Vector3I Position;
		public BlockRaw BlockRaw;
		public int ID;
		public int TickTime;
		static int NextID;
		
		public PhysicsTick(Vector3I pos, BlockRaw block, int tickTime) {
			Position = pos;
			BlockRaw = block;
			TickTime = tickTime;
			ID = NextID;
			NextID++;
		}
	}
	
	public class PhysicsTickComparer : IComparer<PhysicsTick> {
		
		public int Compare(PhysicsTick one, PhysicsTick two) {
			if (one.TickTime.CompareTo(two.TickTime) != 0)
				return one.TickTime.CompareTo(two.TickTime);
			return one.ID.CompareTo(two.ID);
		}
		
	}

	public class PhysicsBase {
		Game game;
		World map;
		Random rnd = new Random();
		int width, length, height, oneY;

		FallingPhysics falling;
		TNTPhysics tnt;
		FoliagePhysics foliage;
		LiquidPhysics liquid;
		OtherPhysics other;
		
		bool enabled = true;
		public bool Enabled {
			get { return enabled; }
			set { enabled = value; liquid.Clear(); }
		}
		
		public PhysicsAction[] OnActivate = new PhysicsAction[Block.DefaultCount];
		public PhysicsAction[] OnRandomTick = new PhysicsAction[Block.DefaultCount];
		public PhysicsAction[] OnPlace = new PhysicsAction[Block.DefaultCount];
		public PhysicsAction[] OnDelete = new PhysicsAction[Block.DefaultCount];
		
		public PhysicsBase(Game game) {
			this.game = game;
			map = game.World;
			game.WorldEvents.OnNewMapLoaded += ResetMap;
			game.UserEvents.BlockChanged += BlockChanged;
			enabled = Options.GetBool(OptionsKey.BlockPhysics, true);
			
			falling = new FallingPhysics(game, this);
			tnt = new TNTPhysics(game, this);
			foliage = new FoliagePhysics(game, this);
			liquid = new LiquidPhysics(game, this);
			other = new OtherPhysics(game, this);
		}
		
		public List<PhysicsTick> tickList = new List<PhysicsTick>();
		public List<PhysicsTick> tickList2 = new List<PhysicsTick>();
		public int tickCount = 0;
		public void Tick() {
			if (!Enabled || game.World.blocks1 == null) return;
			liquid.nextWaterTick = null;
			liquid.nextLavaTick = null;
			
			PhysicsTickComparer TickComparer = new PhysicsTickComparer();
			
			/*foreach (PhysicsTick i in tickList2) {
				PhysicsTick tick = (PhysicsTick)i;
				tickList.Add(tick);
			}*/
			tickList.AddRange(tickList2);
			tickList2.Clear();
			tickList.Sort(TickComparer);
			
			
			foreach (PhysicsTick i in tickList) {
				PhysicsTick tick = (PhysicsTick)i;
				bool doTick = true;
				if (tick.TickTime > tickCount) doTick = false;
				if (!doTick) break;
				int index = (tick.Position.Y * length + tick.Position.Z) * width + tick.Position.X;
				if (tick.BlockRaw == Block.Lava || tick.BlockRaw == Block.StillLava) {
					liquid.ActivateLava(index, tick.BlockRaw);
				} else if (tick.BlockRaw == Block.Water || tick.BlockRaw == Block.StillWater) {
					liquid.ActivateWater(index, tick.BlockRaw);
				}
			}
			
			tickList.RemoveAll(TickIsDone);
			
			tickList.AddRange(tickList2);
			tickList2.Clear();
			tickList.Sort(TickComparer);
			
			if (tickList.Count == 0 && tickList2.Count == 0) {
				tickList = new List<PhysicsTick>();
				tickList2 = new List<PhysicsTick>();
			}
			
			/*foreach (PhysicsTick i in tickList2) {
				PhysicsTick tick = (PhysicsTick)i;
				tickList.Add(tick);
			}
			tickList2.Clear();
			tickList.Sort(TickComparer);*/
			
			TickRandomBlocks();
			tickCount++;
			liquid.nextWaterTick = null;
			liquid.nextLavaTick = null;
		}
		
		private bool TickIsDone(PhysicsTick tick) {
			return tick.TickTime <= this.tickCount;
		}
		
		void BlockChanged(object sender, BlockChangedEventArgs e) {
			if (!Enabled) return;
			Vector3I p = e.Coords;
			int index = (p.Y * length + p.Z) * width + p.X;
			BlockRaw newB = (BlockRaw)e.Block, oldB = (BlockRaw)e.OldBlock;
			
			if (newB == Block.Air && IsEdgeWater(p.X, p.Y, p.Z)) { 
				newB = Block.StillWater; 
				game.UpdateBlock(p.X, p.Y, p.Z, Block.StillWater);
			}
			
			if (newB == Block.Air) {
				PhysicsAction delete = OnDelete[oldB];
				if (delete != null) delete(index, oldB);
			} else {
				PhysicsAction place = OnPlace[newB];
				if (place != null) place(index, newB);
			}
			ActivateNeighbours(p.X, p.Y, p.Z, index);
		}
		
		/// <summary> Activates the direct neighbouring blocks of the given coordinates. </summary>
		public void ActivateNeighbours(int x, int y, int z, int index) {
			if (x > 0) Activate(index - 1);
			if (x < map.MaxX) Activate(index + 1);
			if (z > 0) Activate(index - map.Width);
			if (z < map.MaxZ) Activate(index + map.Width);
			if (y > 0) Activate(index - oneY);
			if (y < map.MaxY) Activate(index + oneY);
		}
		
		/// <summary> Activates the block at the particular packed coordinates. </summary>
		public void Activate(int index) {
			BlockRaw block = map.blocks1[index];
			PhysicsAction activate = OnActivate[block];
			if (activate != null) activate(index, block);
		}
		
		bool IsEdgeWater(int x, int y, int z) {
			WorldEnv env = map.Env;
			if (!(env.EdgeBlock == Block.Water || env.EdgeBlock == Block.StillWater))
				return false;
			
			return y >= env.SidesHeight && y < env.EdgeHeight 
				&& (x == 0 || z == 0 || x == (map.Width - 1) || z == (map.Length - 1));
		}
		
		void ResetMap(object sender, EventArgs e) {
			falling.ResetMap();
			liquid.ResetMap();
			width = map.Width;
			height = map.Height;
			length = map.Length;
			oneY = width * length;
			tickList = new List<PhysicsTick>();
			tickList2 = new List<PhysicsTick>();
		}
		
		public void Dispose() {
			game.WorldEvents.OnNewMapLoaded -= ResetMap;
			game.UserEvents.BlockChanged -= BlockChanged;
		}
		
		void TickRandomBlocks() {
			int xMax = width - 1, yMax = height - 1, zMax = length - 1;
			for (int y = 0; y < height; y += 16)
				for (int z = 0; z < length; z += 16)
					for (int x = 0; x < width; x += 16)
			{
				int lo = (y * length + z) * width + x;
				int hi = (Math.Min(yMax, y + 15) * length + Math.Min(zMax, z + 15))
					* width + Math.Min(xMax, x + 15);
				
				// Inlined 3 random ticks for this chunk
				int index = rnd.Next(lo, hi);
				BlockRaw block = map.blocks1[index];
				PhysicsAction tick = OnRandomTick[block];
				if (tick != null) tick(index, block);
				
				index = rnd.Next(lo, hi);
				block = map.blocks1[index];
				tick = OnRandomTick[block];
				if (tick != null) tick(index, block);
				
				index = rnd.Next(lo, hi);
				block = map.blocks1[index];
				tick = OnRandomTick[block];
				if (tick != null) tick(index, block);
			}
		}
	}
}