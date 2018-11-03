// Copyright 2014-2017 ClassicalSharp | Licensed under BSD-3
using System;
using ClassicalSharp.GraphicsAPI;
using OpenTK;

namespace ClassicalSharp {

	/// <summary> Draws the vertices for a cuboid region. </summary>
	public sealed class CuboidDrawer2 {

		public int elementsPerAtlas1D;
		public float invVerElementSize;
		
		public readonly Vector3 topNormal = new Vector3(0, 1, 0);
		public readonly Vector3 bottomNormal = new Vector3(0, -1, 0);
		public readonly Vector3 leftNormal = new Vector3(-1, 0, 0);
		public readonly Vector3 rightNormal = new Vector3(1, 0, 0);
		public readonly Vector3 frontNormal = new Vector3(0, 0, -1);
		public readonly Vector3 backNormal = new Vector3(0, 0, 1);
		
		/// <summary> Whether a colour tinting effect should be applied to all faces. </summary>
		public bool Tinted;
		
		/// <summary> The tint colour to multiply colour of faces by. </summary>
		public FastColour TintColour;
		
		public Vector3 minBB, maxBB;
		public float x1, y1, z1, x2, y2, z2;
		
		public bool isHeld = false;
		public float RotAngle = -44.767f;
		
		
		/// <summary> Draws the left face of the given cuboid region. </summary>
		public void Left(int count, int col, int texLoc, VertexP3fT2fC4bN1v[] vertices, ref int index) {
			float vOrigin = (texLoc % elementsPerAtlas1D) * invVerElementSize;
			float u1 = minBB.Z, u2 = (count - 1) + maxBB.Z * 15.99f/16f;
			float v1 = vOrigin + maxBB.Y * invVerElementSize;
			float v2 = vOrigin + minBB.Y * invVerElementSize * 15.99f/16f;
			if (Tinted) col = TintBlock(col);
			
			VertexP3fT2fC4bN1v v; v.X = x1; v.Colour = col;
			v.Normal = leftNormal;
			if (isHeld) v.Normal = Utils.RotateY(v.Normal, RotAngle);
			v.Y = y2; v.Z = z2 + (count - 1); v.U = u2; v.V = v1; vertices[index++] = v; 
			          v.Z = z1;               v.U = u1;           vertices[index++] = v;
			v.Y = y1;                                   v.V = v2; vertices[index++] = v;
			          v.Z = z2 + (count - 1); v.U = u2;           vertices[index++] = v;
		}

		/// <summary> Draws the right face of the given cuboid region. </summary>
		public void Right(int count, int col, int texLoc, VertexP3fT2fC4bN1v[] vertices, ref int index) {
			float vOrigin = (texLoc % elementsPerAtlas1D) * invVerElementSize;
			float u1 = (count - minBB.Z), u2 = (1 - maxBB.Z) * 15.99f/16f;
			float v1 = vOrigin + maxBB.Y * invVerElementSize;
			float v2 = vOrigin + minBB.Y * invVerElementSize * 15.99f/16f;
			if (Tinted) col = TintBlock(col);
			
			VertexP3fT2fC4bN1v v; v.X = x2; v.Colour = col;
			v.Normal = rightNormal;
			if (isHeld) v.Normal = Utils.RotateY(v.Normal, RotAngle);
			v.Y = y2; v.Z = z1;               v.U = u1; v.V = v1; vertices[index++] = v;
			          v.Z = z2 + (count - 1); v.U = u2;           vertices[index++] = v;
			v.Y = y1;                                   v.V = v2; vertices[index++] = v;
			          v.Z = z1;               v.U = u1;           vertices[index++] = v;
		}

		/// <summary> Draws the front face of the given cuboid region. </summary>
		public void Front(int count, int col, int texLoc, VertexP3fT2fC4bN1v[] vertices, ref int index) {
			float vOrigin = (texLoc % elementsPerAtlas1D) * invVerElementSize;
			float u1 = (count - minBB.X), u2 = (1 - maxBB.X) * 15.99f/16f;
			float v1 = vOrigin + maxBB.Y * invVerElementSize;
			float v2 = vOrigin + minBB.Y * invVerElementSize * 15.99f/16f;
			if (Tinted) col = TintBlock(col);
			
			VertexP3fT2fC4bN1v v; v.Z = z1; v.Colour = col;
			v.Normal = frontNormal;
			if (isHeld) v.Normal = Utils.RotateY(v.Normal, RotAngle);
			v.X = x2 + (count - 1); v.Y = y1; v.U = u2; v.V = v2; vertices[index++] = v;
			v.X = x1;                         v.U = u1;           vertices[index++] = v;
			                        v.Y = y2;           v.V = v1; vertices[index++] = v;
			v.X = x2 + (count - 1);           v.U = u2;           vertices[index++] = v;
		}
		
		/// <summary> Draws the back face of the given cuboid region. </summary>
		public void Back(int count, int col, int texLoc, VertexP3fT2fC4bN1v[] vertices, ref int index) {
			float vOrigin = (texLoc % elementsPerAtlas1D) * invVerElementSize;
			float u1 = minBB.X, u2 = (count - 1) + maxBB.X * 15.99f/16f;
			float v1 = vOrigin + maxBB.Y * invVerElementSize;
			float v2 = vOrigin + minBB.Y * invVerElementSize * 15.99f/16f;			
			if (Tinted) col = TintBlock(col);
			
			VertexP3fT2fC4bN1v v; v.Z = z2; v.Colour = col;
			v.Normal = backNormal;
			if (isHeld) v.Normal = Utils.RotateY(v.Normal, RotAngle);
			v.X = x2 + (count - 1); v.Y = y2; v.U = u2; v.V = v1; vertices[index++] = v;
			v.X = x1;                         v.U = u1;           vertices[index++] = v;
			                        v.Y = y1;           v.V = v2; vertices[index++] = v;
			v.X = x2 + (count - 1);           v.U = u2;           vertices[index++] = v;
		}
		
		/// <summary> Draws the bottom face of the given cuboid region. </summary>
		public void Bottom(int count, int col, int texLoc, VertexP3fT2fC4bN1v[] vertices, ref int index) {
			float vOrigin = (texLoc % elementsPerAtlas1D) * invVerElementSize;
			float u1 = minBB.X, u2 = (count - 1) + maxBB.X * 15.99f/16f;
			float v1 = vOrigin + minBB.Z * invVerElementSize;
			float v2 = vOrigin + maxBB.Z * invVerElementSize * 15.99f/16f;
			if (Tinted) col = TintBlock(col);
			
			VertexP3fT2fC4bN1v v; v.Y = y1; v.Colour = col;
			v.Normal = bottomNormal;
			if (isHeld) v.Normal = Utils.RotateY(v.Normal, RotAngle);
			v.X = x2 + (count - 1); v.Z = z2; v.U = u2; v.V = v2; vertices[index++] = v;
			v.X = x1;                         v.U = u1;           vertices[index++] = v;
			                        v.Z = z1;           v.V = v1; vertices[index++] = v;
			v.X = x2 + (count - 1);           v.U = u2;           vertices[index++] = v;
		}

		/// <summary> Draws the top face of the given cuboid region. </summary>
		public void Top(int count, int col, int texLoc, VertexP3fT2fC4bN1v[] vertices, ref int index) {
			float vOrigin = (texLoc % elementsPerAtlas1D) * invVerElementSize;
			float u1 = minBB.X, u2 = (count - 1) + maxBB.X * 15.99f/16f;
			float v1 = vOrigin + minBB.Z * invVerElementSize;
			float v2 = vOrigin + maxBB.Z * invVerElementSize * 15.99f/16f;
			if (Tinted) col = TintBlock(col);
			
			VertexP3fT2fC4bN1v v; v.Y = y2; v.Colour = col;
			v.Normal = topNormal;
			if (isHeld) v.Normal = Utils.RotateY(v.Normal, RotAngle);
			v.X = x2 + (count - 1); v.Z = z1; v.U = u2; v.V = v1; vertices[index++] = v;
			v.X = x1;                         v.U = u1;           vertices[index++] = v;
			                        v.Z = z2;           v.V = v2; vertices[index++] = v;
			v.X = x2 + (count - 1);           v.U = u2;           vertices[index++] = v;
		}

		int TintBlock(int col) {
			FastColour rgbCol = FastColour.Unpack(col);
			rgbCol *= TintColour;
			return rgbCol.Pack();
		}
	}
}