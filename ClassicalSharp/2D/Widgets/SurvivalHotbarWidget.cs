// Copyright 2014-2017 ClassicalSharp | Licensed under BSD-3
using System;
using System.Drawing;
using ClassicalSharp.GraphicsAPI;
using ClassicalSharp.Gui.Screens;
using ClassicalSharp.Mode;
using ClassicalSharp.Textures;
using BlockID = System.UInt16;
#if ANDROID
using Android.Graphics;
#endif

namespace ClassicalSharp.Gui.Widgets {
	public sealed class SurvivalHotbarWidget : HotbarWidget {
		
		TextAtlas posAtlas;
		Font font;
		float guiScale;
		public SurvivalHotbarWidget(Game game) : base(game) {
		}
		
		public override void DoAnim(short damage) {
			if (game.LocalPlayer.Health < damage) damage = game.LocalPlayer.Health;
			this.damage = damage;
			damageTime = 20;
		}
		
		// TODO: scaling
		public override void Init() {
			base.Init();
			font = new Font(game.FontName, (16 / 2) * (int)game.GuiHotbarScale);
			posAtlas = new TextAtlas(game, (16 / 2) * (int)game.GuiHotbarScale);
			posAtlas.Pack("0123456789", font, "f");
			game.Events.ChatFontChanged += ChatFontChanged;
			guiScale = game.GuiHotbarScale;
		}
		
		void ChatFontChanged(object sender, EventArgs e) { Recreate(); }
		
		public override void Render(double delta) {
			if (guiScale != game.GuiHotbarScale) {
				Recreate();
			}
			//base.Render(delta);
			
			RenderHotbarOutline();
			RenderHotbarBlocks();
			RenderHotbarItems();
			
			DrawCounts();
			DrawHearts();
			if (inWater) {
				DrawBubbles();
			}
		}
		
		public override void Dispose() {
			font.Dispose();
			posAtlas.Dispose();
			game.Events.ChatFontChanged -= ChatFontChanged;
		}
		
		
		void DrawCounts() {
			SurvivalGameMode surv = (SurvivalGameMode)game.Mode;
			VertexP3fT2fC4bN1v[] vertices = game.ModelCache.vertices;
			int index = 0;
			posAtlas.tex.Y = (short)(Y + (Height - (int)(12 * game.GuiHotbarScale)));
			int xAdj = (int)(14 * game.GuiHotbarScale);
			int xAdj2 = (int)(7 * game.GuiHotbarScale);
			
			int offset = 0;
			for (int i = 0; i < Inventory.BlocksPerRow; i++) {
				if (game.SurvInv.ItemList[i] == null) continue;
				int x = (int)(X + (elemSize + borderSize) * i + xAdj);
				int count = 0;
				if (game.SurvInv.ItemList[i] != null) count = game.SurvInv.ItemList[i].Count;
				int xAdj3 = (count.ToString().Length - 1) * xAdj2;
				if (xAdj3 < 0) xAdj3 = 0;
				x -= xAdj3;
				posAtlas.curX = x;
				if (count > 1)
					posAtlas.AddInt(count, vertices, ref index);
			}

			game.Graphics.BindTexture(posAtlas.tex.ID);
			game.Graphics.UpdateDynamicVb_IndexedTris(game.ModelCache.vb, game.ModelCache.vertices, index);
		}
		
		void DrawHearts() {
			Model.ModelCache cache = game.ModelCache;
			int index = 0, health = game.LocalPlayer.Health;
			int damage = this.damage;
			int inner = (int)(7 * game.GuiHotbarScale);
			int middle = (int)(8 * game.GuiHotbarScale);
			int outer = (int)(9 * game.GuiHotbarScale);
			
			int selBlockSize = (int)(23 * game.GuiHotbarScale);
			int offset = middle - inner;
			int y = Y + (Height - selBlockSize - outer);
			
			bool flash = false;
			
			if (damageTime <= 10 && damageTime >= 8 ||
			    damageTime >= 14 && damageTime <= 17) {
				flash = true;
			}
			
			TextureRec rec2 = flash ? backRecHurt : backRec;
			
			for (int heart = 0; heart < 10; heart++) {
				Texture tex = new Texture(0, X + middle * heart, y, outer, outer, rec2);
				IGraphicsApi.Make2DQuad(ref tex, FastColour.WhitePacked, cache.vertices, ref index);
				
				if (damage > 0 && health <= 1 && flash) {
					if (health == 1) {
						damage += 1;
					}
					TextureRec rec3 = (damage >= 2) ? fullRecHurt : halfRecHurt;
					tex = new Texture(0, X + middle * heart + offset, y + offset, inner, inner, rec3);
					IGraphicsApi.Make2DQuad(ref tex, FastColour.WhitePacked, cache.vertices, ref index);
					damage -= 2;
				}
				if (health <= 0) continue;
				
				TextureRec rec = (health >= 2) ? fullRec : halfRec;
				tex = new Texture(0, X + middle * heart + offset, y + offset, inner, inner, rec);
				IGraphicsApi.Make2DQuad(ref tex, FastColour.WhitePacked, cache.vertices, ref index);
				health -= 2;
			}
			
			game.Graphics.BindTexture(game.Gui.IconsTex);
			game.Graphics.UpdateDynamicVb_IndexedTris(cache.vb, cache.vertices, index);
		}
		
		bool drowning = false;
		void DrawBubbles() {
			Model.ModelCache cache = game.ModelCache;
			int index = 0, air = Utils.Ceil(((float)game.LocalPlayer.Air / 30f));
			int damage = this.damage;
			int inner = (int)(7 * game.GuiHotbarScale);
			int middle = (int)(8 * game.GuiHotbarScale);
			int outer = (int)(9 * game.GuiHotbarScale);
			
			int selBlockSize = (int)(23 * game.GuiHotbarScale);
			//int offset = middle - inner;
			//int offset = (int)(101 * game.GuiHotbarScale);
			int y = Y + (Height - selBlockSize - outer * 2);
			
			if (air > 0) {
				drowning = false;
			}
			if ((float)game.LocalPlayer.Air / 30 == air && !drowning) {
				this.pop = 3;
				if (air == 0) {
					drowning = true;
				}
			}
			
			bool pop = (this.pop > 0);
			
			TextureRec rec2 = BubbleRec;
			
			for (int heart = 0; heart < 10; heart++) {
				if (air <= 0 && !pop) continue;
				if (pop && air == 0) {
					rec2 = PoppedRec;
					pop = false;
				}
				Texture tex = new Texture(0, X + middle * heart, y, outer, outer, rec2);
				IGraphicsApi.Make2DQuad(ref tex, FastColour.WhitePacked, cache.vertices, ref index);
				air -= 1;
			}
			
			game.Graphics.BindTexture(game.Gui.IconsTex);
			game.Graphics.UpdateDynamicVb_IndexedTris(cache.vb, cache.vertices, index);
		}
		
		static TextureRec backRec = new TextureRec(16 / 256f, 0 / 256f, 9 / 256f, 9 / 256f);
		static TextureRec fullRec = new TextureRec(53 / 256f, 1 / 256f, 7 / 256f, 7 / 256f);
		static TextureRec halfRec = new TextureRec(62 / 256f, 1 / 256f, 7 / 256f, 7 / 256f);
		static TextureRec backRecHurt = new TextureRec(25 / 256f, 0 / 256f, 9 / 256f, 9 / 256f);
		static TextureRec fullRecHurt = new TextureRec(71 / 256f, 1 / 256f, 7 / 256f, 7 / 256f);
		static TextureRec halfRecHurt = new TextureRec(80 / 256f, 1 / 256f, 7 / 256f, 7 / 256f);
		
		static TextureRec BubbleRec = new TextureRec(16 / 256f, 18 / 256f, 9 / 256f, 9 / 256f);
		static TextureRec PoppedRec = new TextureRec(25 / 256f, 18 / 256f, 9 / 256f, 9 / 256f);
		
		
		
		
		
		
		public override void RenderHotbarOutline() {
			int texId = game.UseClassicGui ? game.Gui.GuiClassicTex : game.Gui.GuiTex;
			backTex.ID = texId;
			backTex.Render(game.Graphics);
			
			int i = game.SurvInv.SelectedIndex;
			int x = (int)(X + barXOffset + (elemSize + borderSize) * i + elemSize / 2);
			
			selTex.ID = texId;
			selTex.X1 = (int)(x - selBlockSize / 2);
			game.Graphics.Draw2DTexture(ref selTex, FastColour.White);
		}
		
		public override void RenderHotbarBlocks() {
			Model.ModelCache cache = game.ModelCache;
			drawer.BeginBatch(game, cache.vertices, cache.vb);
			
			for (int i = 0; i < Inventory.BlocksPerRow; i++) {
				if (game.SurvInv.ItemList[i] == null) continue;
				BlockID block = game.SurvInv.ItemList[i].id;
				int x = (int)(X + barXOffset + (elemSize + borderSize) * i + elemSize / 2);
				int y = (int)(Y + (Height - barHeight / 2));
				
				float scale = (elemSize * 13.5f/16f) / 2f;
				if (BlockInfo.Draw[block] != DrawType.Sprite) {
					drawer.DrawBatch(block, scale, x, y);
				}
			}
			drawer.EndBatch();
		}
		
		int texIndex;
		int index;
		public override void RenderHotbarItems() {
			Model.ModelCache cache = game.ModelCache;
			
			for (int i = 0; i < Inventory.BlocksPerRow; i++) {
				if (game.SurvInv.ItemList[i] == null) continue;
				BlockID block = game.SurvInv.ItemList[i].id;
				if (BlockInfo.Draw[block] != DrawType.Sprite) continue;
				int x = (int)(X + barXOffset + (elemSize + borderSize) * i + elemSize / 2);
				int y = (int)(Y + (Height - barHeight));
				int lower = (int)(3 * game.GuiHotbarScale);
				int side = (int)(8 * game.GuiHotbarScale);
				int outer = (int)(16 * game.GuiHotbarScale);
				
				y += lower;
				x -= side;
				
				if (BlockInfo.Draw[block] == DrawType.Sprite) {
					int texLoc = BlockInfo.GetTextureLoc(block, Side.Right);
					TextureRec rec = TerrainAtlas1D.GetTexRec(texLoc, 1, out texIndex);
					
					Texture tex = new Texture(0, (int)x, (int)y, outer, outer, rec);
					IGraphicsApi.Make2DQuad(ref tex, FastColour.WhitePacked, cache.vertices, ref index);
					
					game.Graphics.BindTexture(TerrainAtlas1D.TexIds[texIndex]);
					game.Graphics.UpdateDynamicVb_IndexedTris(cache.vb, cache.vertices, index);
				}
			}
			index = 0;
			texIndex = 0;
		}
	}
}