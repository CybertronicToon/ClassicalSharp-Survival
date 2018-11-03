// Copyright 2014-2017 ClassicalSharp | Licensed under BSD-3
using System;
using ClassicalSharp.GraphicsAPI;
using ClassicalSharp.Model;
using ClassicalSharp.Textures;
using BlockID = System.UInt16;
using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace ClassicalSharp.Renderers {
	
	public sealed class PickedPosRenderer : IGameComponent {
		Game game;
		int vb;
		int vb2;
		int curTexLoc, lastTexLoc;
		int pickTexId;
		IGraphicsApi graphics;
		
		public void Init(Game game) {
			this.game = game;
			graphics = game.Graphics;
			col = new FastColour(0, 0, 0, 102).Pack();
			col2 = new FastColour(255, 255, 255, 127).Pack();
			//col2 = FastColour.WhitePacked;
			
			ContextRecreated();
			//game.Events.TerrainAtlasChanged += ResetTextures;
			game.Graphics.ContextLost += ContextLost;
			game.Graphics.ContextRecreated += ContextRecreated;
		}
		
		public void Dispose() { 
			ContextLost();
			game.Events.TerrainAtlasChanged -= ResetTextures;
			game.Graphics.ContextLost -= ContextLost;
			game.Graphics.ContextRecreated -= ContextRecreated;
		}

		public void Ready(Game game) { }
		public void Reset(Game game) { }
		public void OnNewMap(Game game) { }
		public void OnNewMapLoaded(Game game) { }		
		
		int col;
		int col2;
		int index;
		int index2;
		public float pickProg = 0;
		const int verticesCount = 16 * 6;
		VertexP3fC4b[] vertices = new VertexP3fC4b[verticesCount];
		
		Vector3I oldPos;
		
		public void UpdateTexture(PickedPos selected) {
			if (selected.BlockPos != oldPos) {
				pickProg = 0;
			}
			MakeTexture(ref pickTexId, ref lastTexLoc, 240 + Utils.Floor(pickProg));
			oldPos = selected.BlockPos;
		}
		
		public void UpdateState(PickedPos selected) {
			if (selected.BlockPos != oldPos) {
				pickProg = 0;
			}
			index = 0;
			index2 = 0;
			Vector3 camPos = game.CurrentCameraPos;
			float dist = (camPos - selected.Min).LengthSquared;

			float offset = 0.01f;
			if (dist < 4 * 4) offset = 0.00625f;
			if (dist < 2 * 2) offset = 0.00500f;
			offset = 0.00150f;
			
			MakeTexture(ref pickTexId, ref lastTexLoc, 240 + Utils.Floor(pickProg));
			
			Vector3 p1 = selected.Min - new Vector3(offset, offset, offset);
			Vector3 p2 = selected.Max + new Vector3(offset, offset, offset);
			
			/*drawer.elementsPerAtlas1D = TerrainAtlas1D.elementsPerAtlas1D;
			drawer.invVerElementSize  = TerrainAtlas1D.invElementSize;
			//drawer.minBB =new Vector3(0, 0, 0);
			//drawer.maxBB = new Vector3(1f, 1f, 1f);
			drawer.minBB = BlockInfo.MinBB[Block.Stone];
			drawer.maxBB = BlockInfo.MaxBB[Block.Stone];
			drawer.minBB.Y = 1 - drawer.minBB.Y;
			drawer.maxBB.Y = 1 - drawer.maxBB.Y;
			drawer.x1 = p1.X; drawer.x2 = p2.X;
			drawer.y1 = p1.Y; drawer.y2 = p2.Y;
			drawer.z1 = p1.Z; drawer.z2 = p2.Z;
			drawer.Tinted = false;
			drawer.TintColour = new FastColour(0, 0, 0, 0);
			
				index2 = 0;
				
				
				drawer.Bottom(1, FastColour.WhitePacked, 244, vertices2, ref index2);
				drawer.Front(1, FastColour.WhitePacked, 244, vertices2, ref index2);
				drawer.Right(1, FastColour.WhitePacked, 244, vertices2, ref index2);
				drawer.Back(1, FastColour.WhitePacked, 244, vertices2, ref index2);
				drawer.Left(1, FastColour.WhitePacked, 244, vertices2, ref index2);
				drawer.Top(1, FastColour.WhitePacked, 244, vertices2, ref index2);*/
			
			
			
			float size = 1/16f;
			if (dist < 32 * 32) size = 1/32f;
			if (dist < 16 * 16) size = 1/64f;
			if (dist < 8 * 8) size = 1/96f;
			if (dist < 4 * 4) size = 1/128f;
			if (dist < 2 * 2) size = 1/192f;

			DrawLines(p1, p2, size);
			oldPos = selected.BlockPos;
		}
		
		CuboidDrawer drawer = new CuboidDrawer();
		
		const int verticesCount2 = 24 * 12;
		VertexP3fT2fC4b[] vertices2 = new VertexP3fT2fC4b[verticesCount2];
		
		public bool pickingBlock = false;
		
		public void Render(double delta) {
			if (game.Graphics.LostContext) return;
			IGraphicsApi gfx = game.Graphics;
			
			#if !USE_DX && !ANDROID
				gfx.AlphaBlending = true;
				//gfx.DepthWrite = false;
				
				gfx.DrawLine(new Vector3(pos2.X, pos2.Y, pos2.Z), new Vector3(pos1.X, pos2.Y, pos2.Z));
				gfx.DrawLine(new Vector3(pos1.X, pos2.Y, pos2.Z), new Vector3(pos1.X, pos2.Y, pos1.Z));
				gfx.DrawLine(new Vector3(pos1.X, pos2.Y, pos1.Z), new Vector3(pos2.X, pos2.Y, pos1.Z));
				gfx.DrawLine(new Vector3(pos2.X, pos2.Y, pos1.Z), new Vector3(pos2.X, pos2.Y, pos2.Z));
				
				gfx.DrawLine(new Vector3(pos2.X, pos2.Y, pos2.Z), new Vector3(pos2.X, pos1.Y, pos2.Z));
				gfx.DrawLine(new Vector3(pos1.X, pos2.Y, pos2.Z), new Vector3(pos1.X, pos1.Y, pos2.Z));
				gfx.DrawLine(new Vector3(pos1.X, pos2.Y, pos1.Z), new Vector3(pos1.X, pos1.Y, pos1.Z));
				gfx.DrawLine(new Vector3(pos2.X, pos2.Y, pos1.Z), new Vector3(pos2.X, pos1.Y, pos1.Z));
				
				gfx.DrawLine(new Vector3(pos2.X, pos1.Y, pos2.Z), new Vector3(pos1.X, pos1.Y, pos2.Z));
				gfx.DrawLine(new Vector3(pos1.X, pos1.Y, pos2.Z), new Vector3(pos1.X, pos1.Y, pos1.Z));
				gfx.DrawLine(new Vector3(pos1.X, pos1.Y, pos1.Z), new Vector3(pos2.X, pos1.Y, pos1.Z));
				gfx.DrawLine(new Vector3(pos2.X, pos1.Y, pos1.Z), new Vector3(pos2.X, pos1.Y, pos2.Z));
				
				//gfx.DepthWrite = true;
				gfx.AlphaBlending = false;
			#else
			gfx.AlphaBlending = true;
			gfx.DepthWrite = false;
			gfx.SetBatchFormat(VertexFormat.P3fC4b);
			gfx.UpdateDynamicVb_IndexedTris(vb, vertices, index);
			gfx.DepthWrite = true;
			gfx.AlphaBlending = false;
			#endif
			
			if (pickingBlock) {
				//gfx.AlphaTest = true;
				gfx.Texturing = true;
				//gfx.AlphaTest = true;
				gfx.AlphaBlending = true;
				gfx.FaceCulling = true;
				float[] texCols = new float[4];
				texCols[0] = (127f/256f);
				texCols[1] = (127f/256f);
				texCols[2] = (127f/256f);
				texCols[3] = (128f/256f);
				/*GL.TexEnvi(TextureEnvTarget.TextureEnv, TextureEnvParameter.TextureEnvMode, (int)TextureEnvMode.Combine);
				//GL.TexEnvi(TextureEnvTarget.TextureEnv, TextureEnvParameter.CombineRgb, (int)TextureEnvModeCombine.Subtract);
				GL.TexEnvi(TextureEnvTarget.TextureEnv, TextureEnvParameter.CombineAlpha, (int)TextureEnvModeCombine.Subtract);
				GL.TexEnvfv(TextureEnvTarget.TextureEnv, TextureEnvParameter.TextureEnvColor, texCols);
				//GL.TexEnvi(TextureEnvTarget.TextureEnv, TextureEnvParameter.Source0Rgb, (int)TextureEnvModeSource.Texture);
				GL.TexEnvi(TextureEnvTarget.TextureEnv, TextureEnvParameter.Src0Alpha, (int)TextureEnvModeSource.PrimaryColor);
				//GL.TexEnvi(TextureEnvTarget.TextureEnv, TextureEnvParameter.Src1Rgb, (int)TextureEnvModeSource.Constant);
				GL.TexEnvi(TextureEnvTarget.TextureEnv, TextureEnvParameter.Src1Alpha, (int)TextureEnvModeSource.Constant);*/
				//GL.Enable(EnableCap.ColorMaterial);
				//graphics.AlphaTestFunc(CompareFunc.Greater, 0.0f);
				//GL.BlendFuncSeparate(BlendingFactor.DstColor, BlendingFactor.SrcColor, BlendingFactor.DstAlpha, BlendingFactor.DstAlpha);
				graphics.RGBAlphaBlendFunc(BlendFunc.DestColor, BlendFunc.SourceColor, BlendFunc.DestAlpha, BlendFunc.SourceAlpha);
				
				//VertexP3fT2fC4b[] vertices2 = new VertexP3fT2fC4b[8 * 10 * (4 * 4)];
				//game.Graphics.CreateDynamicVb(VertexFormat.P3fT2fC4b, vertices.Length);
				
				
				gfx.BindTexture(pickTexId);
				gfx.SetBatchFormat(VertexFormat.P3fT2fC4b);
				gfx.BindVb(vb2);
				gfx.UpdateDynamicVb_IndexedTris(vb2, vertices2, index2);
				
				//graphics.AlphaBlendFunc(BlendFunc.SourceAlpha, BlendFunc.InvSourceAlpha);
				graphics.AlphaBlendFunc(BlendFunc.SourceAlpha, BlendFunc.InvSourceAlpha);
				//graphics.AlphaTestFunc(CompareFunc.Greater, 0.5f);
				//GL.Disable(EnableCap.ColorMaterial);
				//GL.TexEnvi(TextureEnvTarget.TextureEnv, TextureEnvParameter.TextureEnvMode, (int)TextureEnvMode.Modulate);
				gfx.FaceCulling = false;
				gfx.AlphaBlending = false;
				//gfx.AlphaTest = false;
				gfx.Texturing = false;
				//gfx.AlphaTest = false;
			}
			
		}
		
		Vector3 pos1, pos2;
		void DrawLines(Vector3 p1, Vector3 p2, float size) {
			pos1 = p1; pos2 = p2;
			YQuad2(p1.Y, p1.X, p1.Z, p2.X, p2.Z, p2.X - p1.X, p2.Z - p1.Z, true);
			YQuad2(p2.Y, p1.X, p1.Z, p2.X, p2.Z, p2.X - p1.X, p2.Z - p1.Z, false);
			XQuad2(p1.X, p1.Z, p1.Y, p2.Z, p2.Y, p2.Z - p1.Z, p2.Y - p1.Y, true);
			XQuad2(p2.X, p1.Z, p1.Y, p2.Z, p2.Y, p2.Z - p1.Z, p2.Y - p1.Y, false);
			ZQuad2(p1.Z, p1.X, p1.Y, p2.X, p2.Y, p2.X - p1.X, p2.Y - p1.Y, false);
			ZQuad2(p2.Z, p1.X, p1.Y, p2.X, p2.Y, p2.X - p1.X, p2.Y - p1.Y, true);
			
			#if USE_DX || ANDROID
			// bottom face
			YQuad(p1.Y, p1.X, p1.Z + size, p1.X + size, p2.Z - size);
			YQuad(p1.Y, p2.X, p1.Z + size, p2.X - size, p2.Z - size);
			YQuad(p1.Y, p1.X, p1.Z, p2.X, p1.Z + size);
			YQuad(p1.Y, p1.X, p2.Z, p2.X, p2.Z - size);
			// top face
			YQuad(p2.Y, p1.X, p1.Z + size, p1.X + size, p2.Z - size);
			YQuad(p2.Y, p2.X, p1.Z + size, p2.X - size, p2.Z - size);
			YQuad(p2.Y, p1.X, p1.Z, p2.X, p1.Z + size);
			YQuad(p2.Y, p1.X, p2.Z, p2.X, p2.Z - size);
			// left face
			XQuad(p1.X, p1.Z, p1.Y + size, p1.Z + size, p2.Y - size);
			XQuad(p1.X, p2.Z, p1.Y + size, p2.Z - size, p2.Y - size);
			XQuad(p1.X, p1.Z, p1.Y, p2.Z, p1.Y + size);
			XQuad(p1.X, p1.Z, p2.Y, p2.Z, p2.Y - size);
			// right face
			XQuad(p2.X, p1.Z, p1.Y + size, p1.Z + size, p2.Y - size);
			XQuad(p2.X, p2.Z, p1.Y + size, p2.Z - size, p2.Y - size);
			XQuad(p2.X, p1.Z, p1.Y, p2.Z, p1.Y + size);
			XQuad(p2.X, p1.Z, p2.Y, p2.Z, p2.Y - size);
			// front face
			ZQuad(p1.Z, p1.X, p1.Y + size, p1.X + size, p2.Y - size);
			ZQuad(p1.Z, p2.X, p1.Y + size, p2.X - size, p2.Y - size);
			ZQuad(p1.Z, p1.X, p1.Y, p2.X, p1.Y + size);
			ZQuad(p1.Z, p1.X, p2.Y, p2.X, p2.Y - size);
			// back face
			ZQuad(p2.Z, p1.X, p1.Y + size, p1.X + size, p2.Y - size);
			ZQuad(p2.Z, p2.X, p1.Y + size, p2.X - size, p2.Y - size);
			ZQuad(p2.Z, p1.X, p1.Y, p2.X, p1.Y + size);
			ZQuad(p2.Z, p1.X, p2.Y, p2.X, p2.Y - size);
			#endif
		}
		
		void XQuad(float x, float z1, float y1, float z2, float y2) {
			vertices[index++] = new VertexP3fC4b(x, y1, z1, col);
			vertices[index++] = new VertexP3fC4b(x, y2, z1, col);
			vertices[index++] = new VertexP3fC4b(x, y2, z2, col);
			vertices[index++] = new VertexP3fC4b(x, y1, z2, col);
		}
		
		void ZQuad(float z, float x1, float y1, float x2, float y2) {
			vertices[index++] = new VertexP3fC4b(x1, y1, z, col);
			vertices[index++] = new VertexP3fC4b(x1, y2, z, col);
			vertices[index++] = new VertexP3fC4b(x2, y2, z, col);
			vertices[index++] = new VertexP3fC4b(x2, y1, z, col);
		}
		
		void YQuad(float y, float x1, float z1, float x2, float z2) {
			vertices[index++] = new VertexP3fC4b(x1, y, z1, col);
			vertices[index++] = new VertexP3fC4b(x1, y, z2, col);
			vertices[index++] = new VertexP3fC4b(x2, y, z2, col);
			vertices[index++] = new VertexP3fC4b(x2, y, z1, col);
		}
		
		void XQuad2(float x, float z1, float y1, float z2, float y2, float u, float v, bool flip) {
			if (!flip) {
				vertices2[index2++] = new VertexP3fT2fC4b(x, y1, z1, 0, v, col2);
				vertices2[index2++] = new VertexP3fT2fC4b(x, y2, z1, 0, 0, col2);
				vertices2[index2++] = new VertexP3fT2fC4b(x, y2, z2, u, 0, col2);
				vertices2[index2++] = new VertexP3fT2fC4b(x, y1, z2, u, v, col2);
			} else {
				vertices2[index2++] = new VertexP3fT2fC4b(x, y1, z2, 0, v, col2);
				vertices2[index2++] = new VertexP3fT2fC4b(x, y2, z2, 0, 0, col2);
				vertices2[index2++] = new VertexP3fT2fC4b(x, y2, z1, u, 0, col2);
				vertices2[index2++] = new VertexP3fT2fC4b(x, y1, z1, u, v, col2);
			}
		}
		
		void ZQuad2(float z, float x1, float y1, float x2, float y2, float u, float v, bool flip) {
			if (!flip) {
				vertices2[index2++] = new VertexP3fT2fC4b(x1, y1, z, 0, v, col2);
				vertices2[index2++] = new VertexP3fT2fC4b(x1, y2, z, 0, 0, col2);
				vertices2[index2++] = new VertexP3fT2fC4b(x2, y2, z, u, 0, col2);
				vertices2[index2++] = new VertexP3fT2fC4b(x2, y1, z, u, v, col2);
			} else {
				vertices2[index2++] = new VertexP3fT2fC4b(x2, y1, z, 0, v, col2);
				vertices2[index2++] = new VertexP3fT2fC4b(x2, y2, z, 0, 0, col2);
				vertices2[index2++] = new VertexP3fT2fC4b(x1, y2, z, u, 0, col2);
				vertices2[index2++] = new VertexP3fT2fC4b(x1, y1, z, u, v, col2);
			}
		}
		
		void YQuad2(float y, float x1, float z1, float x2, float z2, float u, float v, bool flip) {
			if (!flip) {
				vertices2[index2++] = new VertexP3fT2fC4b(x1, y, z1, 0, 0, col2);
				vertices2[index2++] = new VertexP3fT2fC4b(x1, y, z2, 0, v, col2);
				vertices2[index2++] = new VertexP3fT2fC4b(x2, y, z2, u, v, col2);
				vertices2[index2++] = new VertexP3fT2fC4b(x2, y, z1, u, 0, col2);
			} else {
				vertices2[index2++] = new VertexP3fT2fC4b(x2, y, z1, 0, 0, col2);
				vertices2[index2++] = new VertexP3fT2fC4b(x2, y, z2, 0, v, col2);
				vertices2[index2++] = new VertexP3fT2fC4b(x1, y, z2, u, v, col2);
				vertices2[index2++] = new VertexP3fT2fC4b(x1, y, z1, u, 0, col2);
			}
		}
		
		void ContextLost() {
			game.Graphics.DeleteVb(ref vb);
			game.Graphics.DeleteVb(ref vb2);
			game.Graphics.DeleteTexture(ref pickTexId);
			lastTexLoc = -1;
		}
		
		void ContextRecreated() {
			vb = game.Graphics.CreateDynamicVb(VertexFormat.P3fC4b, verticesCount);
			vb2 = game.Graphics.CreateDynamicVb(VertexFormat.P3fT2fC4b, verticesCount2);
			//MakeTexture(ref pickTexId, ref lastTexLoc, 240 + (int)pickProg);
			//ResetTextures(null, null);
		}
		
		void ResetTextures(object sender, EventArgs e) {
			lastTexLoc = -1;// lastSideTexLoc = -1;
			MakeTexture(ref pickTexId, ref lastTexLoc, 240 + (int)pickProg);
		}
		
		void MakeTexture(ref int id, ref int lastTexLoc, int newTex) {
			int texLoc = newTex;
			if (texLoc == lastTexLoc || game.Graphics.LostContext) return;
			lastTexLoc = texLoc;
			
			game.Graphics.DeleteTexture(ref id);
			id = TerrainAtlas2D.LoadTextureElement(texLoc);
		}
	}
}
