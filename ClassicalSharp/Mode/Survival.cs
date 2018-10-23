// Copyright 2014-2017 ClassicalSharp | Licensed under BSD-3
using System;
using ClassicalSharp.Entities;
using ClassicalSharp.Entities.Mobs;
using ClassicalSharp.Gui.Screens;
using ClassicalSharp.Gui.Widgets;
using OpenTK;
using OpenTK.Input;
using BlockID = System.UInt16;

namespace ClassicalSharp.Mode {
	
	public sealed class SurvivalGameMode : IGameMode {
		
		Game game;
		int score = 0;
		internal byte[] invCount = new byte[Inventory.BlocksPerRow * Inventory.Rows];
		Random rnd = new Random();
		
		public SurvivalGameMode() {
			invCount[8] = 10; // tnt
			
			float leafTime = 0.20f;
			float dirtTime = 0.4f;
			float clothTime = 0.6f;
			float stoneTime = 0.6f;
			float cobblestoneTime = 0.9f;
			float woodTime = 1.2f;
			float oreTime = 1.8f;
			
			blockList[Block.Stone] = stoneTime;
			blockList[Block.Grass] = dirtTime;
			blockList[Block.Dirt] = dirtTime;
			blockList[Block.Cobblestone] = cobblestoneTime;
			blockList[Block.Wood] = woodTime;
			blockList[Block.Bedrock] = -1;
			blockList[Block.Sand] = dirtTime;
			blockList[Block.Gravel] = dirtTime;
			blockList[Block.GoldOre] = oreTime;
			blockList[Block.IronOre] = oreTime;
			blockList[Block.CoalOre] = oreTime;
			blockList[Block.Log] = 1.5f;
			blockList[Block.Leaves] = leafTime;
			blockList[Block.Sponge] = dirtTime;
			blockList[Block.Glass] = 0.3f;
			for (int i = 21; i <= 36; i++)
				blockList[i] = clothTime;
			blockList[Block.Gold] = oreTime;
			blockList[Block.Iron] = 3.0f;
			blockList[Block.DoubleSlab] = dirtTime;
			blockList[Block.Slab] = cobblestoneTime;
			blockList[Block.Brick] = cobblestoneTime;
			blockList[Block.Bookshelf] = woodTime;
			blockList[Block.MossyRocks] = stoneTime;
			blockList[Block.Obsidian] = 6.0f;
			
			blockList[Block.CobblestoneSlab] = stoneTime;
			blockList[Block.Sandstone] = stoneTime;
			blockList[Block.Snow] = leafTime;
			for (int i = 55; i <= 59; i++)
				blockList[i] = clothTime;
			blockList[Block.Ice] = 0.5f;
			blockList[Block.CeramicTile] = stoneTime;
			blockList[Block.Magma] = stoneTime;
			blockList[Block.Pillar] = stoneTime;
			blockList[Block.Crate] = woodTime;
			blockList[Block.StoneBrick] = stoneTime;
		}
		
		bool pickingBlock = false;
		uint tickCount = 0;
		float[] blockList = new float[byte.MaxValue];
		int pickDelay = 0;
		bool pickDelayed = false;
		short drownTimer = 0;
		short lavaTimer = 0;
		public void Tick() {
			
			
			LocalPlayer p = game.LocalPlayer;
			if (p.onGround) {
					if (!wasOnGround) {
						if (p.fallDistance >= 3.5f) {
							//short damage = (short)Math.Ceiling(((p.fallDistance * 0.5f) - 1.5f));
							short damage = (short)(p.fallDistance - 3f);
							if (damage > 0) p.Damage(damage);
						}
					p.fallDistance = 0;
				}
			}
			
			wasOnGround = p.onGround;
			
			HotbarWidget widget = (HotbarWidget)game.Gui.hudScreen.hotbar;
			if (widget.damageTime > 0) {
				widget.damageTime -= 1;
			}
			
			if (widget.pop > 0) {
				widget.pop -= 1;
			}
			
			if (game.World.blocks1 == null) {
				return;
			}
			int headX = Utils.Floor(p.EyePosition.X);
			int headY = Utils.Floor(p.EyePosition.Y);
			int headZ = Utils.Floor(p.EyePosition.Z);
			if (game.World.IsValidPos(headX, headY, headZ)) {
				BlockID headBlock = game.World.blocks2[(headY * game.World.Length + headZ) * game.World.Width + headX];
				bool headInWater = (headBlock == Block.Water || headBlock == Block.StillWater);
				if (headInWater && p.Air != 0) {
					widget.inWater = true;
					p.Air -= 1;
				} else if (headInWater && p.Air == 0) {
					widget.inWater = true;
					if (drownTimer <= 0) {
						p.Damage(2);
						drownTimer = 11;
					} else {
						drownTimer -= 1;
					}
				} else {
					widget.inWater = false;
					p.Air = 300;
					drownTimer = 0;
				}
			} else {
				widget.inWater = false;
				p.Air = 300;
				drownTimer = 0;
			}
			if (p.TouchesAnyLava()) {
				if (lavaTimer == 0) {
					p.Damage(10);
					lavaTimer = 11;
				} else {
					lavaTimer -= 1;
				}
			} else if (lavaTimer != 0) {
				lavaTimer -= 1;
			}
			
			CheckPlayerDied();
			
			if (pickDelay > 0) {
				pickDelayed = true;
				pickDelay -= 1;
				return;
			}
			bool pickBegan = false;
			if (pickDelay == 0 && pickDelayed && pickingBlock) {
				pickBegan = true;
			}
			pickDelayed = false;
			if (game.World.blocks1 == null || game.World.blocks1.Length == 0) {
				return;
			}
			Vector3I pos = game.SelectedPos.BlockPos;
			if (pos.X < 0 || pos.Y < 0 || pos.Z  < 0 ||
			    pos.X >= game.World.Width || pos.Y >= game.World.Height || pos.Z >= game.World.Length) {
				return;
			}
			byte selectedBlock = (byte)game.SelectedPos.Block;
			if (pickingBlock) {
				if (blockList[selectedBlock] == 0) {
					game.Picking.pickProg = 10;
					game.Picking.UpdateTexture(game.SelectedPos);
				} else if (blockList[selectedBlock] < 0) {
					game.Picking.pickProg = 0;
					return;
				} else {// if (pickTime >= blockList[selectedBlock]) {
					//game.Picking.pickProg += (1f/blockList[selectedBlock]) * (1f/30f) * 10;
					if (pickBegan) {
						game.Picking.pickingBlock = true;
					} else {
						game.Picking.pickProg += 1f/(3*blockList[selectedBlock]);
					}
					game.Picking.UpdateTexture(game.SelectedPos);
				}
			}
			
			BlockID old = game.World.GetBlock(pos.X, pos.Y, pos.Z);
			if (game.Picking.pickProg >= 10 && old != 0) {
				game.UpdateBlock(pos.X, pos.Y, pos.Z, Block.Air);
				game.UserEvents.RaiseBlockChanged(pos, old, Block.Air);
				HandleDelete(old, pos);
				game.Picking.pickProg = 0;
				game.Picking.UpdateState(game.SelectedPos);
				pickDelay = 5;
				game.Picking.pickingBlock = false;
				pickingBlock = false;
			}
			tickCount++;
		}
		
		//public bool HandlesKeyDown(Key key) { return false; }		
		public bool HandlesKeyDown(Key key) {
			if (key == game.Input.Keys[KeyBind.Inventory] && game.Gui.ActiveScreen == game.Gui.hudScreen) {
				game.Gui.SetNewScreen(new SurvivalInventoryScreen(game));
				return true;
			} else if (key == game.Input.Keys[KeyBind.DropBlock] && !game.ClassicMode) {
				Inventory inv = game.Inventory;
				if (inv.CanChangeSelected() && inv.Selected != Block.Air) {
					// Don't assign Selected directly, because we don't want held block
					// switching positions if they already have air in their inventory hotbar.
					inv[inv.SelectedIndex] = Block.Air;
					game.Events.RaiseHeldBlockChanged();
				}
				return true;
			}
			return false;
		}
		
		public bool PickingLeft() {
			// always play delete animations, even if we aren't picking a block.
			game.HeldBlockRenderer.ClickAnim(true);
			byte id = game.Entities.GetClosetPlayer(game.LocalPlayer);
			if (game.Entities.List[id] is EntityItem) return false;
			return id != EntityList.SelfID && PickEntity(id);
		}
		
		public bool PickingRight() {
			if (game.Inventory.Selected == Block.RedMushroom) {
				DepleteInventoryHeld();
				game.LocalPlayer.Health -= 5;
				CheckPlayerDied();
				return true;
			} else if (game.Inventory.Selected == Block.BrownMushroom) {
				DepleteInventoryHeld();
				game.LocalPlayer.Health += 5;
				if (game.LocalPlayer.Health > 20) game.LocalPlayer.Health = 20;
				return true;
			}
			return false; 
		}
		
		public void StopPickingLeft() {
			pickingBlock = false;
			game.Picking.pickProg = 0;
			game.Picking.UpdateState(game.SelectedPos);
			game.Picking.pickingBlock = false;
			PickedBlock = 0;
			pickDelay = 0;
		}
		
		BlockID PickedBlock;

		public void PickLeft(BlockID old) {
			Vector3I pos = game.SelectedPos.BlockPos;
			PickedBlock = old;
			//game.UpdateBlock(pos.X, pos.Y, pos.Z, Block.Air);
			//game.UserEvents.RaiseBlockChanged(pos, old, Block.Air);
			//HandleDelete(old, pos);
			pickingBlock = true;
			if (pickDelay > 0 || pickDelayed) {
				pickDelayed = true;
				return;
			}
			//pickingBlock = true;
			game.Picking.pickingBlock = true;
			if (game.Picking.pickProg >= 10 || blockList[old] == 0) {
				game.UpdateBlock(pos.X, pos.Y, pos.Z, Block.Air);
				game.UserEvents.RaiseBlockChanged(pos, old, Block.Air);
				HandleDelete(old, pos);
				game.Picking.pickProg = 0;
				game.Picking.UpdateState(game.SelectedPos);
				game.Picking.pickingBlock = false;
				pickingBlock = false;
			}
		}
		
		public void PickMiddle(BlockID old) { }
		
		public void PickRight(BlockID old, BlockID block) {
			//int index = game.Inventory.SelectedIndex, offset = game.Inventory.Offset;
			int index = game.SurvInv.SelectedIndex, offset = 0;
			//if (invCount[offset + index] == 0) return;
			
			Vector3I pos = game.SelectedPos.TranslatedPos;
			game.UpdateBlock(pos.X, pos.Y, pos.Z, block);
			game.UserEvents.RaiseBlockChanged(pos, old, block);
			DepleteInventoryHeld();
		}
		
		void DepleteInventoryHeld() {
			//int index = game.Inventory.SelectedIndex, offset = game.Inventory.Offset;
			//invCount[offset + index]--;
			//if (invCount[offset + index] != 0) return;
			int index = game.SurvInv.SelectedIndex, offset = 0;
			game.SurvInv.ItemList[index].Count -= 1;
			if (game.SurvInv.ItemList[index].Count != 0) return;
			
			// bypass HeldBlock's normal behaviour
			//game.Inventory[index] = Block.Air;
			game.SurvInv.ItemList[index] = null;
			game.Events.RaiseHeldBlockChanged();
		}
		
		bool PickEntity(byte id) {
			Entity entity = game.Entities.List[id];
			LocalPlayer p = game.LocalPlayer;
			
			Vector3 delta = p.Position - entity.Position;
			if (delta.LengthSquared > p.ReachDistance * p.ReachDistance) return true;
			
			delta.Y = 0.0f;
			delta = Vector3.Normalize(delta) * 0.5f;
			delta.Y = -0.5f;
			
			entity.Velocity -= delta;
			game.Chat.Add("PICKED ON: " + id + "," + entity.ModelName);
			
			entity.Health -= 2;
			if (entity.Health < 0) {
				game.Entities.RemoveEntity(id);
				score += entity.Model.SurivalScore;
				UpdateScore();
			}
			return true;
		}
		
		public Widget MakeHotbar() { return new SurvivalHotbarWidget(game); }
		
		
		void HandleDelete(BlockID old, Vector3I pos) {
			Vector3 posVec = new Vector3(pos.X + 0.5f, pos.Y + 0.5f, pos.Z + 0.5f);
			if (old == Block.Log) {
				SpawnItem(Block.Wood, (byte)rnd.Next(3, 6), posVec);
			} else if (old == Block.CoalOre) {
				SpawnItem(Block.Slab, (byte)rnd.Next(1, 4), posVec);
			} else if (old == Block.IronOre) {
				SpawnItem(Block.Iron, 1, posVec);
			} else if (old == Block.GoldOre) {
				SpawnItem(Block.Gold, 1, posVec);
			} else if (old == Block.Grass) {
				SpawnItem(Block.Dirt, 1, posVec);
			} else if (old == Block.Stone) {
				SpawnItem(Block.Cobblestone, 1, posVec);
			} else if (old == Block.DoubleSlab) { 
				SpawnItem(Block.Slab, 2, posVec);
			} else if (old == Block.Leaves) {
				if (rnd.Next(1, 16) == 1) { // TODO: is this chance accurate?
					SpawnItem(Block.Sapling, 1, posVec);
				}
			} else {
				SpawnItem(old, 1, posVec);
			}
		}
		
		void SpawnItem(BlockID item, byte count, Vector3 pos) {
			if (!game.Server.IsSinglePlayer) {
				game.SurvInv.TryAddItem((sbyte)count, item);
				return;
			}
			int id = game.Entities.NextFreeID();
			if (id == -1) {
				AddToHotbar(item, count);
				return;
			}
			EntityItem newEnt = new EntityItem(game);
			newEnt.ItemId = item;
			newEnt.ItemCount = (sbyte)count;
			newEnt.SetLocation(LocationUpdate.MakePos(pos, false), false);
			game.Entities.List[id] = newEnt;
		}
		
		public void AddToHotbar(BlockID block, int count) {
			int index = -1, offset = game.Inventory.Offset;
			
			// Try searching for same block, then try invalid block
			for (int i = 0; i < Inventory.BlocksPerRow; i++) {
				if (game.Inventory[i] == block) index = i;
			}
			if (index == -1) {
				for (int i = Inventory.BlocksPerRow - 1; i >= 0; i--) {
					if (game.Inventory[i] == Block.Air) index = i;
				}
			}
			if (index == -1) return; // no free slots
			
			for (int j = 0; j < count; j++) {
				if (invCount[offset + index] >= 99) return; // no more count
				game.Inventory[index] = block;
				invCount[offset + index]++; // TODO: do we need to raise an event if changing held block still?
				// TODO: we need to spawn block models instead
			}
		}

		
		public void OnNewMapLoaded(Game game) {
			UpdateScore();
			wasOnGround = true;
			showedDeathScreen = false;
			game.LocalPlayer.Health = 20;
			string[] models = { "sheep", "pig", "skeleton", "zombie", "creeper", "spider" };
			
			/*for (int i = 0; i < 254; i++) {
				MobEntity fail = new MobEntity(game, models[rnd.Next(models.Length)]);
				float x = rnd.Next(0, game.World.Width) + 0.5f;
				float z = rnd.Next(0, game.World.Length) + 0.5f;
				
				Vector3 pos = Respawn.FindSpawnPosition(game, x, z, fail.Size);
				fail.SetLocation(LocationUpdate.MakePos(pos, false), false);
				game.Entities.List[i] = fail;
			}*/
		}
		
		public void Init(Game game) {
			this.game = game;
			ResetInventory();
			game.Server.AppName += " (survival)";
		}
		
		void ResetInventory() {
			BlockID[] hotbar = game.Inventory.Hotbar;
			for (int i = 0; i < hotbar.Length; i++)
				hotbar[i] = Block.Air;
			hotbar[Inventory.BlocksPerRow - 1] = Block.TNT;
		}
		
		void UpdateScore() {
			game.Chat.Add("&fScore: &e" + score, MessageType.Status1);
		}
		
		
		public void Ready(Game game) { }
		public void Reset(Game game) { }
		public void OnNewMap(Game game) { }
		public void Dispose() { }
		
		public void BeginFrame(double delta) { }
		
		bool wasOnGround = true;
		float fallY = -1000;
		bool showedDeathScreen = false;
		
		public void EndFrame(double delta) {
			LocalPlayer p = game.LocalPlayer;
			if (p.onGround) {
				/*if (wasOnGround) return;
				//short damage = (short)((fallY - p.interp.next.Pos.Y) - 3);
				Console.WriteLine(p.fallDistance.ToString());
				short damage = (short)((p.fallDistance * 0.5f) - 1.5f);
				// TODO: shouldn't take damage when land in water or lava
				// TODO: is the damage formula correct
				if (damage > 0) p.Damage(damage);
				p.fallDistance = 0;*/
				//fallY = -1000;
			} else {
				//float prevPos = p.interp.prev.Pos.Y;
				//float nextPos = p.interp.next.Pos.Y;
				//if (prevPos > nextPos) {
				//	p.fallDistance += (prevPos - nextPos);
				//}
				//fallY = Math.Max(fallY, p.interp.prev.Pos.Y);
			}
			
			//wasOnGround = p.onGround;
			//CheckPlayerDied();
		}
		
		void CheckPlayerDied() {
			LocalPlayer p = game.LocalPlayer;
			if (p.Health <= 0 && !showedDeathScreen) {
				showedDeathScreen = true;
				game.Gui.SetNewScreen(new DeathScreen(game));
				// TODO: Should only reset inventory when actually click on 'load' or 'generate'
				ResetInventory();
			}
		}
	}
}
