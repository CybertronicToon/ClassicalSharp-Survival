// Copyright 2014-2017 ClassicalSharp | Licensed under BSD-3
using System;
using ClassicalSharp.Model;
using ClassicalSharp.Physics;
using ClassicalSharp.Mode;
using OpenTK;
using BlockID = System.UInt16;

namespace ClassicalSharp.Entities {
	
	public class EntityItem : Entity {
		
		LocalInterpComponent interp;
		CollisionsComponent collisions;
		PhysicsComponent physics;
		static HacksComponent hacks = new HacksComponent(null, null);
		
		public BlockID ItemId;
		public sbyte ItemCount;
		
		public EntityItem(Game game) : base(game) {
			this.ItemId = 1;
			this.ItemCount = 1;
			SetModel(ItemId.ToString());
			
			Model.Gravity = 0.04f;
			Model.Drag = new Vector3(0.95f, 0.98f, 0.95f);
			Model.GroundFriction = new Vector3(0.6f, 1f, 0.6f);
			Model.GroundBounce = new Vector3(0f, 0.5f, 0f);
			
			Model.RecalcProperties(this);
			UpdateModelBounds();
			this.ModelScale = new Vector3(1/4f, 1/4f, 1/4f);
			interp = new LocalInterpComponent(game, this);
			
			collisions = new CollisionsComponent(game, this);
			physics = new PhysicsComponent(game, this);
			physics.hacks = hacks;
			physics.collisions = collisions;
		}
		
		public override void RenderModel(double deltaTime, float t) {
			SetModel(ItemId.ToString());
			
			Model.Gravity = 0.04f;
			Model.Drag = new Vector3(0.95f, 0.98f, 0.95f);
			Model.GroundFriction = new Vector3(0.6f, 1f, 0.6f);
			Model.GroundBounce = new Vector3(0f, 0.5f, 0f);
			
			bool sprite = BlockInfo.Draw[ItemId] == DrawType.Sprite;
			if (!sprite) {
				this.ModelScale = new Vector3(1/4f, 1/4f, 1/4f);
			} else {
				this.ModelScale = new Vector3(1/2f, 1/2f, 1/2f);
			}
			Model.RecalcProperties(this);
			UpdateModelBounds();
			Position = Vector3.Lerp(interp.prev.Pos, interp.next.Pos, t);
			if (!sprite) {
				interp.LerpAngles(t);
			} else {
				this.RotY = game.LocalPlayer.HeadY - 45f;
			}
			anim.GetCurrentAnimState(t);
			Model.Render(this);
		}
		
		public override void RenderName() {}
		
		public override void Despawn() {}
		
		public override void Tick(double delta) {
			if (!game.World.HasBlocks) return;
			OldVelocity = Velocity;
			interp.AdvanceState();
			physics.UpdateVelocityState();
			
			bool sprite = BlockInfo.Draw[ItemId] == DrawType.Sprite;
			
			physics.PhysicsTick(Vector3.Zero);
			
			interp.next.Pos = Position; Position = interp.prev.Pos;
			//if (sprite) {
			//	interp.prevRotY = RotY; interp.nextRotY = game.LocalPlayer.HeadY - 45f;
			//}
			anim.UpdateAnimState(interp.prev.Pos, interp.next.Pos, delta);
			
			SurvivalGameMode surv = null;
			
			if (game.Mode.GetType() == typeof(SurvivalGameMode)) {
				surv = (SurvivalGameMode)game.Mode;
			}
			for (int id = 0; id < EntityList.MaxCount; id++) {
				Entity other = game.Entities.List[id];
				if (other != game.LocalPlayer) continue;
				if (other == null || other == this) continue;
				//if (!other.Model.Pushes) continue;
				
				bool yIntersects = 
					this.Position.Y <= (other.Position.Y + other.Size.Y) && 
					other.Position.Y  <= (this.Position.Y + this.Size.Y);
				if (!yIntersects) continue;
				
				float dX = other.Position.X - this.Position.X;
				float dZ = other.Position.Z - this.Position.Z;
				float dist = dX * dX + dZ * dZ;
				if (dist > 1.5f) continue; // TODO: range needs to be lower?
				
				if (surv == null) {
					int changeInv = 0;
					changeInv = game.Inventory.SelectedIndex;
					game.Inventory[changeInv] = this.ItemId;
					
					int entId = game.Entities.GetEntityID(this);
					game.Entities.RemoveEntity((byte)entId);
					break;
				} else {
					//surv.AddToHotbar(ItemId, ItemCount);
					game.SurvInv.TryAddItem(ItemCount, ItemId);
					
					int entId = game.Entities.GetEntityID(this);
					game.Entities.RemoveEntity((byte)entId);
					break;
				}
			}
		}
		
		public override void SetLocation(LocationUpdate update, bool interpolate) {
			interp.SetLocation(update, interpolate);
		}
	}
}
