// Copyright 2014-2017 ClassicalSharp | Licensed under BSD-3
using System;
using ClassicalSharp.Events;
using ClassicalSharp.Generator;
using ClassicalSharp.GraphicsAPI;
using ClassicalSharp.Map;
using OpenTK;

namespace ClassicalSharp.Renderers {
	
	public class CelestialRenderer : IGameComponent {
		
		int SunTex, MoonTex, vb = -1, vb2 = -1, vb3 = -1, vb4 = -1;
		int index = 0;
		int index2 = 0;
		int index3 = 0;
		double CelestialAngle;
		double LastCelestialAngle;
		Game game;
		const int count = 502 * 4;
		const int sunCount = 1 * 4;
		const int moonCount = 1 * 4;
		const int starCount = 500 * 4;
		JavaRandom rand;
		VertexP3fT2fC4b[] vertices2 = new VertexP3fT2fC4b[sunCount];
		VertexP3fT2fC4b[] vertices3 = new VertexP3fT2fC4b[moonCount];
		VertexP3fT2fC4b[] vertices4 = new VertexP3fT2fC4b[starCount];
		
		public void Init(Game game) {
			this.game = game;
			rand = new JavaRandom();
			double x = (0.0 / 24000.0) + 0.25;
			if (x < 0) x += 1;
			CelestialAngle = x + ((1.0 - (Math.Cos(x * Math.PI) + 1.0) / 2.0) - x) / 3.0;
			game.Events.TextureChanged += TextureChanged;
			game.Events.TexturePackChanged += TexturePackChanged;
			game.Graphics.ContextLost += ContextLost;
			game.Graphics.ContextRecreated += ContextRecreated;
			ContextRecreated();
		}
		
		public void Reset(Game game) {
			game.Graphics.DeleteTexture(ref SunTex);
			game.Graphics.DeleteTexture(ref MoonTex);
		}
		public void Ready(Game game) { }
		public void OnNewMap(Game game) { MakeVb(); }
		public void OnNewMapLoaded(Game game) {
			game.Server.Ticks = 0;
		}
		
		public void Dispose() {
			game.Graphics.DeleteTexture(ref SunTex);
			game.Graphics.DeleteTexture(ref MoonTex);
			ContextLost();
			
			game.Events.TextureChanged -= TextureChanged;
			game.Events.TexturePackChanged -= TexturePackChanged;
			game.Graphics.ContextLost -= ContextLost;
			game.Graphics.ContextRecreated -= ContextRecreated;			
		}
		
		void TexturePackChanged(object sender, EventArgs e) {
			game.Graphics.DeleteTexture(ref SunTex);
			game.Graphics.DeleteTexture(ref MoonTex);
		}
		
		void TextureChanged(object sender, TextureEventArgs e) {
			if (e.Name == "sun.png") {
				game.UpdateTexture(ref SunTex, e.Name, e.Data, false);
			} else if (e.Name == "moon.png") {
				game.UpdateTexture(ref MoonTex, e.Name, e.Data, false);
			}
		}
		
		FastColour nightFogCol = new FastColour(0x0f, 0x0f, 0x17);
		FastColour nightCloudCol = new FastColour(0x19, 0x19, 0x26);
		FastColour nightSkyCol = new FastColour(0x00, 0x00, 0x00, 0x00);
		
		public void Render(double deltaTime) {
			if (vb == -1) return;
			LastCelestialAngle = CelestialAngle;
			double x = ((game.Server.Ticks * 1) / 24000.0) - 0.25;
			if (x < 0) x += 1;
			CelestialAngle = x + ((1.0 - (Math.Cos(x * Math.PI) + 1.0) / 2.0) - x) / 3.0;
			game.Graphics.DepthWrite = false;
			game.Graphics.Texturing = true;
			game.Graphics.AlphaBlending = true;
			game.Graphics.DisableMipmaps();
			game.Graphics.Fog = false;
			game.Graphics.AlphaBlendFunc(BlendFunc.SourceAlpha, BlendFunc.One);
			game.Graphics.BindTexture(SunTex);
			game.Graphics.SetBatchFormat(VertexFormat.P3fT2fC4b);
			
			Matrix4 m = Matrix4.Identity, rotY, rotX;			
			
			if (game.Server.DoDayNightCycle) {
				float skyMul = (float)(Math.Cos(CelestialAngle * (2 * Math.PI)) * 2 + 0.5);
				Utils.Clamp(ref skyMul, 0, 1);
				FastColour newSkyCol = FastColour.Lerp(nightSkyCol, WorldEnv.DefaultSkyColour, skyMul);
				if (newSkyCol != game.World.Env.SkyCol) game.World.Env.SetSkyColour(newSkyCol);
				FastColour newFogCol = FastColour.Lerp(nightFogCol, WorldEnv.DefaultFogColour, skyMul);
				if (newFogCol != game.World.Env.FogCol) game.World.Env.SetFogColour(newFogCol);
				FastColour newCloudCol = FastColour.Lerp(nightCloudCol, WorldEnv.DefaultCloudsColour, skyMul);
				if (newCloudCol != game.World.Env.CloudsCol) game.World.Env.SetCloudsColour(newCloudCol);
				byte SunLightSub = CalcSunLightSub();
				if (SunLightSub != game.Lighting.sunLightSub) {
					game.Lighting.sunLightSub = SunLightSub;
					game.MapRenderer.Refresh();
					game.MapBordersRenderer.ResetEdges();
				}
			}
			
			// Base skybox rotation
			float rotTime = (float)(game.accumulator * 2 * Math.PI); // So speed of 1 rotates whole skybox every second
			WorldEnv env = game.World.Env;
			//Matrix4.RotateY(out rotY, env.SkyboxHorSpeed * rotTime);
			//Matrix4.Mult(out m, ref m, ref rotY);
			Matrix4.RotateX(out rotX, (float)(360d * CelestialAngle) * Utils.Deg2Rad + 90f * Utils.Deg2Rad);
			Matrix4.Mult(out m, ref m, ref rotX);
			
			
			// Rotate around camera
			Vector2 rotation = game.Camera.GetCameraOrientation();
			Matrix4.RotateY(out rotY, rotation.X); // Camera yaw
			Matrix4.Mult(out m, ref m, ref rotY);
			Matrix4.RotateX(out rotX, rotation.Y); // Cammera pitch
			Matrix4.Mult(out m, ref m, ref rotX);
			Matrix4.Mult(out m, ref m, ref game.Camera.tiltM);
			
			game.Graphics.LoadMatrix(ref m);			
			game.Graphics.BindVb(vb);
			game.Graphics.DrawVb_IndexedTris(sunCount);
			
			game.Graphics.BindTexture(MoonTex);
			
			m = Matrix4.Identity;
			Matrix4.RotateX(out rotX, (float)(360d * CelestialAngle) * Utils.Deg2Rad + 90f * Utils.Deg2Rad);
			Matrix4.Mult(out m, ref m, ref rotX);
			
			
			// Rotate around camera
			rotation = game.Camera.GetCameraOrientation();
			Matrix4.RotateY(out rotY, rotation.X); // Camera yaw
			Matrix4.Mult(out m, ref m, ref rotY);
			Matrix4.RotateX(out rotX, rotation.Y); // Cammera pitch
			Matrix4.Mult(out m, ref m, ref rotX);
			Matrix4.Mult(out m, ref m, ref game.Camera.tiltM);
			
			game.Graphics.LoadMatrix(ref m);			
			game.Graphics.BindVb(vb);
			game.Graphics.DrawVb_IndexedTris(moonCount, sunCount);
			
			/*game.Graphics.AlphaBlendFunc(BlendFunc.SourceAlpha, BlendFunc.InvSourceAlpha);
			//game.Graphics.AlphaBlending = false;
			
			game.Graphics.BindTexture(0);
			
			m = Matrix4.Identity;
			Matrix4.RotateX(out rotX, (float)(360d * CelestialAngle) * Utils.Deg2Rad + 90f * Utils.Deg2Rad);
			Matrix4.Mult(out m, ref m, ref rotX);
			
			
			// Rotate around camera
			rotation = game.Camera.GetCameraOrientation();
			Matrix4.RotateY(out rotY, rotation.X); // Camera yaw
			Matrix4.Mult(out m, ref m, ref rotY);
			Matrix4.RotateX(out rotX, rotation.Y); // Cammera pitch
			Matrix4.Mult(out m, ref m, ref rotX);
			Matrix4.Mult(out m, ref m, ref game.Camera.tiltM);
			
			game.Graphics.LoadMatrix(ref m);			
			game.Graphics.BindVb(vb);
			game.Graphics.DrawVb_IndexedTris(starCount, (sunCount + moonCount));*/
			
			game.Graphics.Fog = true;
			game.Graphics.AlphaBlendFunc(BlendFunc.SourceAlpha, BlendFunc.InvSourceAlpha);
			game.Graphics.AlphaBlending = false;
			game.Graphics.Texturing = false;
			game.Graphics.LoadMatrix(ref game.Graphics.View);
			game.Graphics.DepthWrite = true;
		}
		
		public void RenderStars(double deltaTime) {
			if (vb == -1) return;
			double x = ((game.Server.Ticks * 1) / 24000.0) - 0.25;
			if (x < 0) x += 1;
			CelestialAngle = x + ((1.0 - (Math.Cos(x * Math.PI) + 1.0) / 2.0) - x) / 3.0;
			game.Graphics.DepthWrite = false;
			game.Graphics.Texturing = true;
			game.Graphics.AlphaBlending = true;
			game.Graphics.DisableMipmaps();
			game.Graphics.Fog = false;
			game.Graphics.AlphaBlendFunc(BlendFunc.SourceAlpha, BlendFunc.One);
			game.Graphics.BindTexture(0);
			game.Graphics.SetBatchFormat(VertexFormat.P3fT2fC4b);
			
			Matrix4 m = Matrix4.Identity, rotY, rotX;			
			
			m = Matrix4.Identity;
			Matrix4.RotateX(out rotX, (float)(360d * CelestialAngle) * Utils.Deg2Rad + 90f * Utils.Deg2Rad);
			Matrix4.Mult(out m, ref m, ref rotX);
			
			
			// Rotate around camera
			Vector2 rotation = game.Camera.GetCameraOrientation();
			Matrix4.RotateY(out rotY, rotation.X); // Camera yaw
			Matrix4.Mult(out m, ref m, ref rotY);
			Matrix4.RotateX(out rotX, rotation.Y); // Cammera pitch
			Matrix4.Mult(out m, ref m, ref rotX);
			Matrix4.Mult(out m, ref m, ref game.Camera.tiltM);
			
			game.Graphics.LoadMatrix(ref m);			
			game.Graphics.BindVb(vb);
			game.Graphics.DrawVb_IndexedTris(starCount, (sunCount + moonCount));
			
			game.Graphics.Fog = true;
			game.Graphics.AlphaBlending = false;
			game.Graphics.Texturing = false;
			game.Graphics.LoadMatrix(ref game.Graphics.View);
			game.Graphics.DepthWrite = true;
		}
		
		public void Render2(double deltaTime) {
			if (vb2 == -1 || vb3 == -1) return;
			LastCelestialAngle = CelestialAngle;
			double x = ((game.Server.Ticks * 1) / 24000.0) - 0.25;
			if (x < 0) x += 1;
			CelestialAngle = x + ((1.0 - (Math.Cos(x * Math.PI) + 1.0) / 2.0) - x) / 3.0;
			if (CelestialAngle != LastCelestialAngle) {
				UpdateVb();
			}
			game.Graphics.DepthWrite = false;
			game.Graphics.Texturing = true;
			game.Graphics.AlphaBlending = true;
			game.Graphics.DisableMipmaps();
			game.Graphics.Fog = false;
			game.Graphics.AlphaBlendFunc(BlendFunc.SourceAlpha, BlendFunc.One);
			game.Graphics.BindTexture(SunTex);
			game.Graphics.SetBatchFormat(VertexFormat.P3fT2fC4b);
			
			Matrix4 m = Matrix4.Identity, rotY, rotX;			
			
			if (game.Server.DoDayNightCycle) {
				float skyMul = (float)(Math.Cos(CelestialAngle * (2 * Math.PI)) * 2 + 0.5);
				Utils.Clamp(ref skyMul, 0, 1);
				FastColour newSkyCol = FastColour.Lerp(nightSkyCol, WorldEnv.DefaultSkyColour, skyMul);
				if (newSkyCol != game.World.Env.SkyCol) game.World.Env.SetSkyColour(newSkyCol);
				FastColour newFogCol = FastColour.Lerp(nightFogCol, WorldEnv.DefaultFogColour, skyMul);
				if (newFogCol != game.World.Env.FogCol) game.World.Env.SetFogColour(newFogCol);
				FastColour newCloudCol = FastColour.Lerp(nightCloudCol, WorldEnv.DefaultCloudsColour, skyMul);
				if (newCloudCol != game.World.Env.CloudsCol) game.World.Env.SetCloudsColour(newCloudCol);
				byte SunLightSub = CalcSunLightSub();
				if (SunLightSub != game.Lighting.sunLightSub) {
					game.Lighting.sunLightSub = SunLightSub;
					game.MapRenderer.Refresh();
					game.MapBordersRenderer.ResetEdges();
				}
			}
			
			// Base skybox rotation
			float rotTime = (float)(game.accumulator * 2 * Math.PI); // So speed of 1 rotates whole skybox every second
			WorldEnv env = game.World.Env;
			//Matrix4.RotateY(out rotY, env.SkyboxHorSpeed * rotTime);
			//Matrix4.Mult(out m, ref m, ref rotY);
			Matrix4.RotateX(out rotX, (float)(360d * CelestialAngle) * Utils.Deg2Rad + 90f * Utils.Deg2Rad);
			Matrix4.Mult(out m, ref m, ref rotX);
			
			
			// Rotate around camera
			Vector2 rotation = game.Camera.GetCameraOrientation();
			Matrix4.RotateY(out rotY, rotation.X); // Camera yaw
			Matrix4.Mult(out m, ref m, ref rotY);
			Matrix4.RotateX(out rotX, rotation.Y); // Cammera pitch
			Matrix4.Mult(out m, ref m, ref rotX);
			Matrix4.Mult(out m, ref m, ref game.Camera.tiltM);
			
			game.Graphics.LoadMatrix(ref m);			
			game.Graphics.BindVb(vb);
			game.Graphics.UpdateDynamicVb_IndexedTris(vb2, vertices2, sunCount);
			
			game.Graphics.BindTexture(MoonTex);
			
			m = Matrix4.Identity;
			Matrix4.RotateX(out rotX, (float)(360d * CelestialAngle) * Utils.Deg2Rad + 90f * Utils.Deg2Rad);
			Matrix4.Mult(out m, ref m, ref rotX);
			
			
			// Rotate around camera
			rotation = game.Camera.GetCameraOrientation();
			Matrix4.RotateY(out rotY, rotation.X); // Camera yaw
			Matrix4.Mult(out m, ref m, ref rotY);
			Matrix4.RotateX(out rotX, rotation.Y); // Cammera pitch
			Matrix4.Mult(out m, ref m, ref rotX);
			Matrix4.Mult(out m, ref m, ref game.Camera.tiltM);
			
			game.Graphics.LoadMatrix(ref m);			
			game.Graphics.BindVb(vb);
			game.Graphics.UpdateDynamicVb_IndexedTris(vb2, vertices3, moonCount);
			
			//game.Graphics.AlphaBlendFunc(BlendFunc.SourceAlpha, BlendFunc.InvSourceAlpha);
			//game.Graphics.AlphaBlending = false;
			
			game.Graphics.BindTexture(0);
			
			m = Matrix4.Identity;
			Matrix4.RotateX(out rotX, (float)(360d * CelestialAngle) * Utils.Deg2Rad + 90f * Utils.Deg2Rad);
			Matrix4.Mult(out m, ref m, ref rotX);
			
			
			// Rotate around camera
			rotation = game.Camera.GetCameraOrientation();
			Matrix4.RotateY(out rotY, rotation.X); // Camera yaw
			Matrix4.Mult(out m, ref m, ref rotY);
			Matrix4.RotateX(out rotX, rotation.Y); // Cammera pitch
			Matrix4.Mult(out m, ref m, ref rotX);
			Matrix4.Mult(out m, ref m, ref game.Camera.tiltM);
			
			game.Graphics.LoadMatrix(ref m);			
			game.Graphics.BindVb(vb);
			game.Graphics.UpdateDynamicVb_IndexedTris(vb4, vertices4, starCount);
			
			game.Graphics.Fog = true;
			game.Graphics.AlphaBlendFunc(BlendFunc.SourceAlpha, BlendFunc.InvSourceAlpha);
			game.Graphics.AlphaBlending = false;
			game.Graphics.Texturing = false;
			game.Graphics.LoadMatrix(ref game.Graphics.View);
			game.Graphics.DepthWrite = true;
		}
		
		byte CalcSunLightSub() {
			float SunLightSub = (float)Math.Cos(CelestialAngle * (2.0f * Math.PI)) * 2.0f + 0.5f;
			Utils.Clamp(ref SunLightSub, 0, 1);
			return (byte)((1.0f - SunLightSub) * 11.0f);
		}
		
		void ContextLost() {
			game.Graphics.DeleteVb(ref vb);
			game.Graphics.DeleteVb(ref vb2);
			game.Graphics.DeleteVb(ref vb3);
			game.Graphics.DeleteVb(ref vb4);
			madeStars = false;
		}
		void ContextRecreated() {
			vb2 = game.Graphics.CreateDynamicVb(VertexFormat.P3fT2fC4b, count);
			vb3 = game.Graphics.CreateDynamicVb(VertexFormat.P3fT2fC4b, count);
			vb4 = game.Graphics.CreateDynamicVb(VertexFormat.P3fT2fC4b, count);
			MakeVb();
			UpdateVb();
		}
		
		bool madeStars = false;
		
		public void UpdateVb() {
			index = 0;
			index2 = 0;
			index3 = 0;
			if (!madeStars) {
				MakeSunAndMoon();
				MakeStars();
			} else {
				UpdateStars();
			}
		}
		
		void MakeSunAndMoon() {

			const float posX = 0.06f;
			const float posY = 0.06f;
			const float posZ = 0.2f;
			const float posAdd = 0.0f;
			VertexP3fT2fC4b v; v.Colour = FastColour.WhitePacked;
			
			// Render the front quad			                        
			v.X =  posX; v.Y = -posY + posAdd; v.Z = -posZ; v.U = 0.00f; v.V = 0.00f; vertices2[index] = v; index++;
			v.X = -posX;                         v.U = 1.00f;              vertices2[index] = v; index++;
			            v.Y =  posY + posAdd;                          v.V = 1.00f; vertices2[index] = v; index++;
			v.X =  posX;                         v.U = 0.00f;              vertices2[index] = v; index++;
			
			const float posX2 = 0.04f;
			const float posY2 = 0.04f;
			
			// Render the front quad			                        
			v.X =  -posX2; v.Y = -posY2 + posAdd; v.Z = posZ; v.U = 0.00f; v.V = 0.00f; vertices3[index2] = v; index2++;
			v.X = posX2;                         v.U = 1.00f;              vertices3[index2] = v; index2++;
			            v.Y =  posY2 + posAdd;                          v.V = 1.00f; vertices3[index2] = v; index2++;
			v.X =  -posX2;                         v.U = 0.00f;              vertices3[index2] = v; index2++;
		}
		
		void MakeStars() {
			VertexP3fT2fC4b v;
			v.Colour = starColour.Pack();
			
			for (int i = 0; i < 500; i++) {
				float pitch = (float)(Utils.Lerp(0.025f, 0.975f, rand.NextFloat()) * 2 * Math.PI);
				float yaw   = (float)(Utils.Lerp(0.025f, 0.975f, rand.NextFloat()) * 2 * Math.PI);
				
				const float size = 0.001f;
				Vector3 p00 = new Vector3(-size, -size, 0.2f);
				Vector3 p10 = new Vector3(+size, -size, 0.2f);
				Vector3 p11 = new Vector3(+size, +size, 0.2f);
				Vector3 p01 = new Vector3(-size, +size, 0.2f);
				
				p00 = Utils.RotateY(p00, yaw);
				p10 = Utils.RotateY(p10, yaw);
				p11 = Utils.RotateY(p11, yaw);
				p01 = Utils.RotateY(p01, yaw);
				
				p00 = Utils.RotateZ(p00, pitch);
				p10 = Utils.RotateZ(p10, pitch);
				p11 = Utils.RotateZ(p11, pitch);
				p01 = Utils.RotateZ(p01, pitch);
				
				v.X = p00.X; v.Y = p00.Y; v.Z = p00.Z; v.U = 0.00f; v.V = 0.00f; vertices4[index3] = v; index3++;
				v.X = p10.X; v.Y = p10.Y; v.Z = p10.Z; v.U = 1.00f;              vertices4[index3] = v; index3++;
				v.X = p11.X; v.Y = p11.Y; v.Z = p11.Z;              v.V = 1.00f; vertices4[index3] = v; index3++;
				v.X = p01.X; v.Y = p01.Y; v.Z = p01.Z; v.U = 0.00f;              vertices4[index3] = v; index3++;
			}
			madeStars = true;
		}
		
		void UpdateStars() {
				float skyMul = (float)(Math.Cos(CelestialAngle * (2 * Math.PI)) * 4 + 0.5);
				Utils.Clamp(ref skyMul, 0, 1);
				FastColour newStarColour = FastColour.Lerp2(starColour, starColourHidden, skyMul);
				if (newStarColour != curStarColour) {
					int newStarColourPacked = newStarColour.Pack();
					for (int i = 0; i < starCount; i++) {
						vertices4[i].Colour = newStarColourPacked;
					}
					curStarColour = newStarColour;
				}
		}
		
		FastColour starColour = new FastColour(0x3f, 0x3f, 0x3f);
		FastColour starColourHidden = new FastColour(0x3f, 0x3f, 0x3f, 0x00);
		FastColour curStarColour;
		
		unsafe void MakeVb() {
			if (game.Graphics.LostContext) return;
			game.Graphics.DeleteVb(ref vb);
			VertexP3fT2fC4b* vertices = stackalloc VertexP3fT2fC4b[count];
			IntPtr start = (IntPtr)vertices;
			
			const float posX = 0.06f;
			const float posY = 0.06f;
			const float posZ = 0.2f;
			const float posAdd = 0.0f;
			VertexP3fT2fC4b v; v.Colour = FastColour.WhitePacked;
			
			// Render the front quad			                        
			v.X =  posX; v.Y = -posY + posAdd; v.Z = -posZ; v.U = 0.00f; v.V = 0.00f; *vertices = v; vertices++;
			v.X = -posX;                         v.U = 1.00f;              *vertices = v; vertices++;
			            v.Y =  posY + posAdd;                          v.V = 1.00f; *vertices = v; vertices++;
			v.X =  posX;                         v.U = 0.00f;              *vertices = v; vertices++;
			
			const float posX2 = 0.04f;
			const float posY2 = 0.04f;
			
			// Render the front quad			                        
			v.X =  -posX2; v.Y = -posY2 + posAdd; v.Z = posZ; v.U = 0.00f; v.V = 0.00f; *vertices = v; vertices++;
			v.X = posX2;                         v.U = 1.00f;              *vertices = v; vertices++;
			            v.Y =  posY2 + posAdd;                          v.V = 1.00f; *vertices = v; vertices++;
			v.X =  -posX2;                         v.U = 0.00f;              *vertices = v; vertices++;
			
			RenderStars(ref vertices);
			
			// Render the left quad
			/*v.X =  pos; v.Y = -pos; v.Z =  pos; v.U = 0.00f; v.V = 1.00f; *vertices = v; vertices++;
			                        v.Z = -pos; v.U = 0.25f;              *vertices = v; vertices++;
			            v.Y =  pos;                          v.V = 0.50f; *vertices = v; vertices++;
			                        v.Z =  pos; v.U = 0.00f;              *vertices = v; vertices++;*/
			
			/*for (int i = 0; i < 100; i++) {
				//float randAdd = (float)rand.NextFloat();
				//float randAdd2 = (float)rand.NextFloat();
				//randAdd = Utils.Lerp(-4, 4, randAdd);
				//float posX3 = 0.0025f * (float)(Math.Cos(randAdd) * (100 * randAdd2));
				//float posY3 = 0.0025f * (float)(Math.Sin(randAdd) * (100 * randAdd2));
				float xSize = 0.002f;
				float ySize = 0.002f;
				//float posZ2 = (0.02f * randAdd2);
				float randAdd = (float)rand.NextFloat();
				randAdd = Utils.Lerp(-1, 1, randAdd);
				float rxy = (float)Math.Sqrt(1f - (randAdd * randAdd));
				float phi = (float)rand.NextFloat();
				phi = Utils.Lerp(0, (float)(2 * Math.PI), phi);
				float posX3 = (rxy * (float)Math.Cos(phi)) * 0.2f;
				float posY3 = (rxy * (float)Math.Sin(phi)) * 0.2f;
				float posZ2 = 0.2f * randAdd;
				v.Colour = starColour.Pack();
				v.X = -xSize + posX3;  v.Y = -ySize + posY3;  v.Z = posZ2; v.U = 0.00f; v.V = 0.00f; *vertices = v; vertices++;
				v.X =  xSize+ posX3;                                       v.U = 1.00f;              *vertices = v; vertices++;
				                       v.Y =  ySize + posY3;                            v.V = 1.00f; *vertices = v; vertices++;
				v.X = -xSize+ posX3;                                       v.U = 0.00f;              *vertices = v; vertices++;
			}*/
			
			vb = game.Graphics.CreateVb(start, VertexFormat.P3fT2fC4b, count);
		}
		
		unsafe void RenderStars(ref VertexP3fT2fC4b* vertices) {
			VertexP3fT2fC4b v;
			v.Colour = starColour.Pack();
			
			for (int i = 0; i < 500; i++) {
				float pitch = (float)(Utils.Lerp(0.025f, 0.975f, rand.NextFloat()) * 2 * Math.PI);
				float yaw   = (float)(Utils.Lerp(0.025f, 0.975f, rand.NextFloat()) * 2 * Math.PI);
				
				const float size = 0.001f;
				Vector3 p00 = new Vector3(-size, -size, 0.2f);
				Vector3 p10 = new Vector3(+size, -size, 0.2f);
				Vector3 p11 = new Vector3(+size, +size, 0.2f);
				Vector3 p01 = new Vector3(-size, +size, 0.2f);
				
				p00 = Utils.RotateY(p00, yaw);
				p10 = Utils.RotateY(p10, yaw);
				p11 = Utils.RotateY(p11, yaw);
				p01 = Utils.RotateY(p01, yaw);
				
				p00 = Utils.RotateZ(p00, pitch);
				p10 = Utils.RotateZ(p10, pitch);
				p11 = Utils.RotateZ(p11, pitch);
				p01 = Utils.RotateZ(p01, pitch);
				
				v.X = p00.X; v.Y = p00.Y; v.Z = p00.Z; v.U = 0.00f; v.V = 0.00f; *vertices = v; vertices++;
				v.X = p10.X; v.Y = p10.Y; v.Z = p10.Z; v.U = 1.00f;              *vertices = v; vertices++;
				v.X = p11.X; v.Y = p11.Y; v.Z = p11.Z;              v.V = 1.00f; *vertices = v; vertices++;
				v.X = p01.X; v.Y = p01.Y; v.Z = p01.Z; v.U = 0.00f;              *vertices = v; vertices++;
			}
		}
	}
}
