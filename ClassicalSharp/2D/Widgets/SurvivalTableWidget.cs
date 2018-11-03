// Copyright 2014-2017 ClassicalSharp | Licensed under BSD-3
using System;
using System.Drawing;
using ClassicalSharp.GraphicsAPI;
using OpenTK.Input;
using BlockID = System.UInt16;

namespace ClassicalSharp.Gui.Widgets {
	public sealed class SurvivalTableWidget : Widget {
		
		public SurvivalTableWidget(Game game) : base(game) { }
		
		public const int MaxRowsDisplayed = 8;
		public Item[] CraftingItems;
		public Item CraftedItem;
		public bool CraftingSelected, CraftedSelected;
		public int ElementsPerRow;
		public int SelectedIndex;
		public int RecipeIndex;
		public TextAtlas posAtlas;
		public Font font;
		internal ScrollbarWidget scroll;
		public bool PendingClose;
		public Item cursorItem;
		public Item[] itemList;
		
		Texture descTex;
		int totalRows, blockSize;
		float selBlockExpand;
		StringBuffer buffer = new StringBuffer(128);
		public IsometricBlockDrawer drawer = new IsometricBlockDrawer();
		
		int TableX { get { return X - 5 - 10; } }
		int TableY { get { return Y - 5 - 30; } }
		int TableWidth { get { return ElementsPerRow * blockSize + 10 + 20; } }
		int TableHeight { get { return Math.Min(totalRows + (int)Math.Sqrt(CraftingItems.Length), MaxRowsDisplayed) * blockSize + 10 + 40; } }
		
		// These were sourced by taking a screenshot of vanilla
		// Then using paint to extract the colour components
		// Then using wolfram alpha to solve the glblendfunc equation
		static FastColour topBackCol = new FastColour(34, 34, 34, 168);
		static FastColour bottomBackCol = new FastColour(57, 57, 104, 202);
		static FastColour topSelCol = new FastColour(255, 255, 255, 142);
		static FastColour bottomSelCol = new FastColour(255, 255, 255, 192);
		static FastColour topSelCol2 = new FastColour(155, 155, 155, 142);
		static FastColour bottomSelCol2 = new FastColour(155, 155, 155, 192);
		
		static VertexP3fT2fC4bN1v[] vertices = new VertexP3fT2fC4bN1v[8 * 10 * (4 * 4)];
		int vb;
		public override void Render(double delta) {
			IGraphicsApi gfx = game.Graphics;
			gfx.Draw2DQuad(TableX, TableY, TableWidth, TableHeight, topBackCol, bottomBackCol);
			if (totalRows > MaxRowsDisplayed) scroll.Render(delta);
			
			if (SelectedIndex != -1 && !CraftingSelected && !CraftedSelected) {
				int x, y;
				GetCoords(SelectedIndex, out x, out y);
				float off = blockSize * 0.1f;
				int size = (int)(blockSize + off * 2);
				gfx.Draw2DQuad((int)(x - off), (int)(y - off),
				               size, size, topSelCol, bottomSelCol);
			} else if (SelectedIndex != -1 && CraftingSelected) {
				int x, y;
				GetCoords2(SelectedIndex, out x, out y);
				float off = blockSize * 0.1f;
				int size = (int)(blockSize + off * 2);
				gfx.Draw2DQuad((int)(x - off), (int)(y - off),
				               size, size, topSelCol, bottomSelCol);
			} else if (SelectedIndex != -1 && CraftedSelected) {
				int x, y;
				GetCoords3(SelectedIndex, out x, out y);
				float off = blockSize * 0.1f;
				int size = (int)(blockSize + off * 2);
				gfx.Draw2DQuad((int)(x - off), (int)(y - off),
				               size, size, topSelCol, bottomSelCol);
			}
			gfx.Texturing = true;
			gfx.SetBatchFormat(VertexFormat.P3fT2fC4bN1v);
			
			drawer.BeginBatch(game, vertices, vb);
			for (int i = 0; i < game.SurvInv.ItemList.Length; i++) {
				int x, y;
				if (!GetCoords(i, out x, out y)) continue;
				
				// We want to always draw the selected block on top of others
				if (i == SelectedIndex && !CraftingSelected && !CraftedSelected) continue;
				
				BlockID block;
				if (game.SurvInv.ItemList[i] != null) {
					block = game.SurvInv.ItemList[i].id;
				} else {
					block = Block.Air;
				}
				
				drawer.DrawBatch(block, blockSize * 0.7f / 2f,
				                 x + blockSize / 2, y + blockSize / 2);
			}
			
			for (int i = 0; i < CraftingItems.Length; i++) {
				int x, y;
				if (!GetCoords2(i, out x, out y)) continue;
				
				// We want to always draw the selected block on top of others
				if (i == SelectedIndex && CraftingSelected) continue;
				
				BlockID block;
				if (CraftingItems[i] != null) {
					block = CraftingItems[i].id;
				} else {
					block = Block.Air;
				}
				
				drawer.DrawBatch(block, blockSize * 0.7f / 2f,
				                 x + blockSize / 2, y + blockSize / 2);
			}
			
			for (int i = 0; i == 0; i++) {
				int x, y;
				if (!GetCoords3(i, out x, out y)) continue;
				
				// We want to always draw the selected block on top of others
				if (i == SelectedIndex && CraftedSelected) continue;
				
				BlockID block;
				if (CraftedItem != null) {
					block = CraftedItem.id;
				} else {
					block = Block.Air;
				}
				
				drawer.DrawBatch(block, blockSize * 0.7f / 2f,
				                 x + blockSize / 2, y + blockSize / 2);
			}
			
			if (SelectedIndex != -1 && !CraftingSelected && !CraftedSelected) {
				int x, y;
				GetCoords(SelectedIndex, out x, out y);
				
				BlockID block;
				if (game.SurvInv.ItemList[SelectedIndex] != null) {
					block = game.SurvInv.ItemList[SelectedIndex].id;
				} else {
					block = Block.Air;
				}
				
				drawer.DrawBatch(block, (blockSize + selBlockExpand) * 0.7f / 2,
				                 x + blockSize / 2, y + blockSize / 2);
			} else if (SelectedIndex != -1 && CraftingSelected) {
				int x, y;
				GetCoords2(SelectedIndex, out x, out y);
				
				BlockID block;
				if (CraftingItems[SelectedIndex] != null) {
					block = CraftingItems[SelectedIndex].id;
				} else {
					block = Block.Air;
				}
				
				drawer.DrawBatch(block, (blockSize + selBlockExpand) * 0.7f / 2,
				                 x + blockSize / 2, y + blockSize / 2);
			} else if (SelectedIndex != -1 && CraftedSelected) {
				int x, y;
				GetCoords3(SelectedIndex, out x, out y);
				
				BlockID block;
				if (CraftedItem != null) {
					block = CraftedItem.id;
				} else {
					block = Block.Air;
				}
				
				drawer.DrawBatch(block, (blockSize + selBlockExpand) * 0.7f / 2,
				                 x + blockSize / 2, y + blockSize / 2);
			}
			
			drawer.EndBatch();
			
			DrawCounts();
			
			drawer.BeginBatch(game, vertices, vb);
			
			if (cursorItem != null) {
				Point cursorPos = game.DesktopCursorPos;
				Point topLeft = game.PointToScreen(Point.Empty);
				cursorPos.X -= topLeft.X; cursorPos.Y -= topLeft.Y;
				drawer.DrawBatch(cursorItem.id, (blockSize + selBlockExpand) * 0.7f / 2,
				                 cursorPos.X, cursorPos.Y);
			}
			
			drawer.EndBatch();
			
			DrawHeldCount();
			
			if (descTex.IsValid) {
				descTex.Render(gfx);
			}
			gfx.Texturing = false;
		}
		
		void DrawCounts() {
			VertexP3fT2fC4bN1v[] vertices = game.ModelCache.vertices;
			int index = 0;
			posAtlas.tex.Y = (short)(Y + ((int)(12 * game.GuiInventoryScale)));
			int xAdj = (int)(14 * game.GuiInventoryScale);
			int xAdj2 = (int)(7 * game.GuiInventoryScale);
			int yAdd = 0;
			
			for (int y = 0; y < totalRows; y++) {
			for (int i = 0; i < 9; i++) {
				int x = X + (i * blockSize);
				int count = 0;
				if (game.SurvInv.ItemList[i + yAdd] != null) count = game.SurvInv.ItemList[i + yAdd].Count;
				int xAdj3 = (count.ToString().Length - 1) * xAdj2;
				//if (xAdj3 < 0) xAdj3 = 0;
				//x -= xAdj3;
				posAtlas.curX = x;
				if (count > 1)
					posAtlas.AddInt(count, vertices, ref index);
			}
			

			game.Graphics.BindTexture(posAtlas.tex.ID);
			game.Graphics.UpdateDynamicVb_IndexedTris(game.ModelCache.vb, game.ModelCache.vertices, index);
			posAtlas.tex.Y += (short)blockSize;
			yAdd += 9;
			}
			
			int gridSize = (int)Math.Sqrt(CraftingItems.Length);
			
			for (int y = 0; y < gridSize; y++) {
			for (int i = 0; i < gridSize; i++) {
				int x = X + (i * blockSize);
				int count = 0;
				if (CraftingItems[i + (y * gridSize)] != null) count = CraftingItems[i + (y * gridSize)].Count;
				int xAdj3 = (count.ToString().Length - 1) * xAdj2;
				//if (xAdj3 < 0) xAdj3 = 0;
				//x -= xAdj3;
				posAtlas.curX = x;
				if (count > 1)
					posAtlas.AddInt(count, vertices, ref index);
			}
			

			game.Graphics.BindTexture(posAtlas.tex.ID);
			game.Graphics.UpdateDynamicVb_IndexedTris(game.ModelCache.vb, game.ModelCache.vertices, index);
			posAtlas.tex.Y += (short)blockSize;
			yAdd += gridSize;
			}
			
			posAtlas.tex.Y -= (short)((gridSize + 1) * blockSize);
			posAtlas.tex.Y += (short)(((gridSize + 1) * blockSize) / 2);
			
			for (int y = 0; y < 1; y++) {
			for (int i = 0; i < 1; i++) {
				int x = X + (8 * blockSize);
				int count = 0;
				if (CraftedItem != null) count = CraftedItem.Count;
				int xAdj3 = (count.ToString().Length - 1) * xAdj2;
				//if (xAdj3 < 0) xAdj3 = 0;
				//x -= xAdj3;
				posAtlas.curX = x;
				if (count > 1)
					posAtlas.AddInt(count, vertices, ref index);
			}
			

			game.Graphics.BindTexture(posAtlas.tex.ID);
			game.Graphics.UpdateDynamicVb_IndexedTris(game.ModelCache.vb, game.ModelCache.vertices, index);
			posAtlas.tex.Y += (short)blockSize;
			yAdd += gridSize;
			}
		}
		
		void DrawHeldCount() {
			Point cursorPos = game.DesktopCursorPos;
			Point topLeft = game.PointToScreen(Point.Empty);
			cursorPos.X -= topLeft.X; cursorPos.Y -= topLeft.Y;
			
			VertexP3fT2fC4bN1v[] vertices = game.ModelCache.vertices;
			int index = 0;
			int yAdj = (int)(9 * game.GuiInventoryScale);
			posAtlas.tex.Y = (short)(cursorPos.Y - (blockSize / 2) + yAdj);
			int xAdj = 0;
			int xAdj2 = 0;
			int yAdd = 0;
			
			int x = cursorPos.X - (blockSize / 2);
			int count = 0;
			if (cursorItem != null) count = cursorItem.Count;
			int xAdj3 = (count.ToString().Length - 1) * xAdj2;
			//if (xAdj3 < 0) xAdj3 = 0;
			//x -= xAdj3;
			posAtlas.curX = x;
			if (count > 1)
				posAtlas.AddInt(count, vertices, ref index);
			

			game.Graphics.BindTexture(posAtlas.tex.ID);
			game.Graphics.UpdateDynamicVb_IndexedTris(game.ModelCache.vb, game.ModelCache.vertices, index);
		}
		
		bool GetCoords(int i, out int screenX, out int screenY) {
			int x = i % ElementsPerRow, y = i / ElementsPerRow;
			screenX = X + blockSize * x;
			screenY = Y + blockSize * y + 3;
			screenY -= scroll.ScrollY * blockSize;
			y -= scroll.ScrollY;
			return y >= 0 && y < MaxRowsDisplayed;
		}
		
		bool GetCoords2(int i, out int screenX, out int screenY) {
			int i2 = i + game.SurvInv.ItemList.Length;
			int gridSize = (int)Math.Sqrt(CraftingItems.Length);
			int x = (i) % (gridSize), y = (i) / (gridSize);
			screenX = X + blockSize * x;
			screenY = Y + blockSize * (y + 4) + 3;
			screenY -= scroll.ScrollY * blockSize;
			y -= scroll.ScrollY;
			return y >= 0 && y < MaxRowsDisplayed + gridSize;
		}
		
		bool GetCoords3(int i, out int screenX, out int screenY) {
			int i2 = i + game.SurvInv.ItemList.Length;
			int gridSize = (int)Math.Sqrt(CraftingItems.Length);
			int x = (8) % (9), y = (i) / (gridSize);
			screenX = X + blockSize * x;
			screenY = Y + (int)(blockSize * (y + 4.5) + 3);
			screenY -= scroll.ScrollY * blockSize;
			y -= scroll.ScrollY;
			return y >= 0 && y < MaxRowsDisplayed + gridSize;
		}
		
		Point GetMouseCoords(int i) {
			int x, y;
			GetCoords(i, out x, out y);
			x += blockSize / 2; y += blockSize / 2;
			
			Point topLeft = game.PointToScreen(Point.Empty);
			x += topLeft.X; y += topLeft.Y;
			return new Point(x, y);
		}
		
		public override void Init() {
			scroll = new ScrollbarWidget(game);
			itemList = game.SurvInv.ItemList;
			RecreateElements();
			Reposition();
			//SetBlockTo(game.Inventory.Selected);
			Recreate();
		}
		
		public override void Dispose() {
			game.Graphics.DeleteVb(ref vb);
			game.Graphics.DeleteTexture(ref descTex);
			lastCreatedIndex = -1000;
		}
		
		public override void Recreate() {
			Dispose();
			vb = game.Graphics.CreateDynamicVb(VertexFormat.P3fT2fC4bN1v, vertices.Length);
			RecreateDescTex();
		}
		
		public override void Reposition() {
			blockSize = (int)(50 * Math.Sqrt(game.GuiInventoryScale));
			selBlockExpand = (float)(25 * Math.Sqrt(game.GuiInventoryScale));
			UpdatePos();
			UpdateScrollbarPos();
		}
		
		void UpdateDescTexPos() {
			descTex.X1 = X + Width / 2 - descTex.Width / 2;
			descTex.Y1 = Y - descTex.Height - 5;
		}
		
		void UpdatePos() {
			int rowsDisplayed = Math.Min(MaxRowsDisplayed, totalRows);
			Width  = blockSize * ElementsPerRow;
			Height = blockSize * (rowsDisplayed + 2);
			X = game.Width  / 2 - Width  / 2;
			Y = game.Height / 2 - Height / 2;
			UpdateDescTexPos();
		}
		
		void UpdateScrollbarPos() {
			scroll.X = TableX + TableWidth;
			scroll.Y = TableY;
			scroll.Height = TableHeight;
			scroll.TotalRows = totalRows;
		}

		public void SetBlockTo(BlockID block) {
			SelectedIndex = -1;
			for (int i = 0; i < game.SurvInv.ItemList.Length; i++) {
				if (game.SurvInv.ItemList[i].id == block) SelectedIndex = i;
			}
			
			scroll.ScrollY = (SelectedIndex / ElementsPerRow) - (MaxRowsDisplayed - 1);
			scroll.ClampScrollY();
			MoveCursorToSelected();
			RecreateDescTex();
		}
		
		public void OnInventoryChanged() {
			RecreateElements();
			if (SelectedIndex >= game.SurvInv.ItemList.Length)
				SelectedIndex = game.SurvInv.ItemList.Length - 1;
			
			scroll.ScrollY = SelectedIndex / ElementsPerRow;
			scroll.ClampScrollY();
			RecreateDescTex();
		}
		
		void MoveCursorToSelected() {
			if (SelectedIndex == -1) return;
			game.DesktopCursorPos = GetMouseCoords(SelectedIndex);
		}
		
		void UpdateBlockInfoString(BlockID block) {
			buffer.Clear();
			if (game.PureClassic) { buffer.Append("Select block"); return; }
			
			buffer.Append(BlockInfo.Name[block]);
			if (game.ClassicMode) return;
			
			buffer.Append(" (ID ");
			buffer.AppendNum(block);
			buffer.Append("&f, place ");
			buffer.Append(BlockInfo.CanPlace[block] ? "&aYes" : "&cNo");
			buffer.Append("&f, delete ");
			buffer.Append(BlockInfo.CanDelete[block] ? "&aYes" : "&cNo");
			buffer.Append("&f)");
		}
		
		int lastCreatedIndex = -1000;
		void RecreateDescTex() {
			if (SelectedIndex == lastCreatedIndex || game.SurvInv == null) return;
			lastCreatedIndex = SelectedIndex;
			
			game.Graphics.DeleteTexture(ref descTex);
			if (SelectedIndex == -1) return;
			BlockID block;
			if (game.SurvInv.ItemList[SelectedIndex] != null) {
				block = game.SurvInv.ItemList[SelectedIndex].id;
				UpdateBlockInfoString(block);
			} else {
				buffer.Clear();
			}
			string value = buffer.ToString();
			
			DrawTextArgs args = new DrawTextArgs(value, font, true);
			descTex = game.Drawer2D.MakeTextTexture(ref args, 0, 0);
			UpdateDescTexPos();
		}
		
		void RecreateElements() {
			int totalElements = 0;
			BlockID[] map = game.Inventory.Map;
			//int numItems = game.SurvInv.NextFreeSlot();
			int numItems = game.SurvInv.ItemList.Length - 1;
			Item[] items = game.SurvInv.ItemList;
			
			/*for (int i = 0; i < map.Length; i++) {
				if (Show(map[i])) { totalElements++; }
			}*/
			for (int i = 0; i < numItems; i++) {
				//if (Show(items[i].id)) { totalElements++; }
				totalElements++;
			}
			
			totalRows = Utils.CeilDiv(totalElements, ElementsPerRow);
			UpdateScrollbarPos();
			UpdatePos();

			//Elements = new Item[totalElements];
			int index = 0;
			/*for (int i = 0; i < map.Length; i++) {
				if (Show(map[i])) { Elements[index++] = map[i]; }
			}*/
			//for (int i = 0; i < numItems; i++) {
			//	if (Show(items[i].id)) { Elements[index++] = items[i].id; }
			//}
		}
		
		bool Show(BlockID block) {
			if (block == Block.Air) return false;

			if (block < Block.CpeCount) {
				int count = game.SupportsCPEBlocks ? Block.CpeCount : Block.OriginalCount;
				return block < count;
			}
			return game.UseCPE;
		}
		
		public override bool HandlesMouseMove(int mouseX, int mouseY) {
			if (scroll.HandlesMouseMove(mouseX, mouseY)) return true;
			CraftingSelected = false;
			CraftedSelected = false;
			SelectedIndex = -1;
			if (Contains(X, Y + 3, Width, MaxRowsDisplayed * blockSize - 3 * 2, mouseX, mouseY)) {
				for (int i = 0; i < game.SurvInv.ItemList.Length; i++) {
					int x, y;
					GetCoords(i, out x, out y);
					
					if (Contains(x, y, blockSize, blockSize, mouseX, mouseY)) {
						SelectedIndex = i;
						break;
					}
				}
				for (int i = 0; i < CraftingItems.Length; i++) {
					int x, y;
					GetCoords2(i, out x, out y);
					
					if (Contains(x, y, blockSize, blockSize, mouseX, mouseY)) {
						SelectedIndex = i;
						CraftingSelected = true;
						break;
					}
				}
				for (int i = 0; i == 0; i ++) {
					int x, y;
					GetCoords3(i, out x, out y);
					
					if (Contains(x, y, blockSize, blockSize, mouseX, mouseY)) {
						SelectedIndex = i;
						CraftedSelected = true;
						break;
					}
				}
			}
			RecreateDescTex();
			return true;
		}
		
		
		public override bool HandlesMouseClick(int mouseX, int mouseY, MouseButton button) {
			PendingClose = false;
			if (button == MouseButton.Right) {
				if (!CraftingSelected) {
				if (SelectedIndex == -1) return false;
				Item item;
				if (itemList[SelectedIndex] != null &&
				    cursorItem != null &&
				    itemList[SelectedIndex].id == cursorItem.id &&
				    itemList[SelectedIndex].Count < 64) {
					itemList[SelectedIndex].Count += 1;
					cursorItem.Count -= 1;
					if (cursorItem.Count == 0) cursorItem = null;
					return true;
				} else if (itemList[SelectedIndex] == null &&
				           cursorItem != null) {
					itemList[SelectedIndex] = new Item(1, 0, cursorItem.id);
					cursorItem.Count -= 1;
					if (cursorItem.Count == 0) cursorItem = null;
					return true;
				}
				if (itemList[SelectedIndex] != null && cursorItem == null) {
					item = new Item(1, 0, itemList[SelectedIndex].id);
					cursorItem = item;
					itemList[SelectedIndex].Count -= 1;
					if (itemList[SelectedIndex].Count == 0)
						itemList[SelectedIndex] = null;
					return true;
				}/* else if (game.SurvInv.ItemList[SelectedIndex] == null && cursorItem != null) {
					game.SurvInv.ItemList[SelectedIndex] = cursorItem;
					cursorItem = null;
				} else {
					item = cursorItem;
					cursorItem = game.SurvInv.ItemList[SelectedIndex];
					game.SurvInv.ItemList[SelectedIndex] = item;
				}*/
				} else if (CraftingSelected) {
				if (SelectedIndex == -1) return false;
				Item item;
				if (CraftingItems[SelectedIndex] != null &&
				    cursorItem != null &&
				    CraftingItems[SelectedIndex].id == cursorItem.id &&
				    CraftingItems[SelectedIndex].Count < 64) {
					CraftingItems[SelectedIndex].Count += 1;
					cursorItem.Count -= 1;
					if (cursorItem.Count == 0) cursorItem = null;
					RecalcRecipe();
					return true;
				} else if (CraftingItems[SelectedIndex] == null &&
				           cursorItem != null) {
					CraftingItems[SelectedIndex] = new Item(1, 0, cursorItem.id);
					cursorItem.Count -= 1;
					if (cursorItem.Count == 0) cursorItem = null;
					RecalcRecipe();
					return true;
				}
				if (CraftingItems[SelectedIndex] != null && cursorItem == null) {
					item = new Item(1, 0, CraftingItems[SelectedIndex].id);
					cursorItem = item;
					CraftingItems[SelectedIndex].Count -= 1;
					if (CraftingItems[SelectedIndex].Count == 0)
						CraftingItems[SelectedIndex] = null;
					RecalcRecipe();
					return true;
				}/* else if (CraftingItems[SelectedIndex] == null && cursorItem != null) {
					CraftingItems[SelectedIndex] = cursorItem;
					cursorItem = null;
				} else {
					item = cursorItem;
					cursorItem = CraftingItems[SelectedIndex];
					CraftingItems[SelectedIndex] = item;
				}*/
				}
				return false;
			} else if (button != MouseButton.Left) {
				return false;
			}

			if (scroll.HandlesMouseClick(mouseX, mouseY, button)) {
				return true;
			} else if (SelectedIndex != -1) {
				//game.Inventory.Selected = Elements[SelectedIndex];
				//PendingClose = true;
				if (!CraftingSelected && !CraftedSelected) {
				if (itemList[SelectedIndex] != null &&
				    cursorItem != null &&
				    itemList[SelectedIndex].id == cursorItem.id &&
				    itemList[SelectedIndex].Count < 64) {
					if (itemList[SelectedIndex].Count + cursorItem.Count > 64) {
						cursorItem.Count = (sbyte)((itemList[SelectedIndex].Count + cursorItem.Count) - 64);
						itemList[SelectedIndex].Count = 64;
						return true;
					} else if (itemList[SelectedIndex].Count + cursorItem.Count <= 64) {
						itemList[SelectedIndex].Count += cursorItem.Count;
						cursorItem = null;
						return true;
					}
				}
				if (itemList[SelectedIndex] != null && cursorItem == null) {
					cursorItem = NewItem(itemList[SelectedIndex]);
					itemList[SelectedIndex] = null;
				} else if (itemList[SelectedIndex] == null && cursorItem != null) {
					itemList[SelectedIndex] = cursorItem;
					cursorItem = null;
				} else if (itemList[SelectedIndex] != null && cursorItem != null) {
					Item item = cursorItem;
					cursorItem = NewItem(itemList[SelectedIndex]);
					itemList[SelectedIndex] = item;
				}
				} else if (CraftingSelected) {
				if (CraftingItems[SelectedIndex] != null &&
				    cursorItem != null &&
				    CraftingItems[SelectedIndex].id == cursorItem.id &&
				    CraftingItems[SelectedIndex].Count < 64) {
					if (CraftingItems[SelectedIndex].Count + cursorItem.Count > 64) {
						cursorItem.Count = (sbyte)((CraftingItems[SelectedIndex].Count + cursorItem.Count) - 64);
						CraftingItems[SelectedIndex].Count = 64;
						RecalcRecipe();
						return true;
					} else if (CraftingItems[SelectedIndex].Count + cursorItem.Count <= 64) {
						CraftingItems[SelectedIndex].Count += cursorItem.Count;
						cursorItem = null;
						RecalcRecipe();
						return true;
					}
				}
				if (CraftingItems[SelectedIndex] != null && cursorItem == null) {
					cursorItem = NewItem(CraftingItems[SelectedIndex]);
					CraftingItems[SelectedIndex] = null;
					RecalcRecipe();
				} else if (CraftingItems[SelectedIndex] == null && cursorItem != null) {
					CraftingItems[SelectedIndex] = cursorItem;
					cursorItem = null;
					RecalcRecipe();
				} else if (CraftingItems[SelectedIndex] != null && cursorItem != null) {
					Item item = cursorItem;
					cursorItem = NewItem(CraftingItems[SelectedIndex]);
					CraftingItems[SelectedIndex] = item;
					RecalcRecipe();
				}
				} else if (CraftedSelected) {
				if (CraftedItem != null &&
				    cursorItem != null &&
				    CraftedItem.id == cursorItem.id &&
				    cursorItem.Count < 64) {if (CraftedItem.Count + cursorItem.Count <= 64) {
						cursorItem.Count += CraftedItem.Count;
						CraftedItem = null;
						SubtractRecipe(game.RecipeList[RecipeIndex]);
						RecalcRecipe();
						return true;
					}
				}
				if (CraftedItem != null && cursorItem == null) {
					cursorItem = NewItem(CraftedItem);
					CraftedItem = null;
					SubtractRecipe(game.RecipeList[RecipeIndex]);
					RecalcRecipe();
				}
				}
				return true;
			} else if (Contains(TableX, TableY, TableWidth, TableHeight, mouseX, mouseY)) {
				return true;
			}
			return false;
		}
		
		public void RecalcRecipe() {
			Recipe[] recipeList = game.RecipeList;
			Item[] ListOfItems = (Item[])CraftingItems.Clone();
			BlockID[] BlockList = CraftingList(ListOfItems);
			BlockID[] BlockList2 = CraftingList2(ListOfItems);
			BlockID[,] BlockList2D = CraftingDim(BlockList);
			for (int i = 0; i < ListOfItems.Length; i++) {
				if (ListOfItems[i] == null) ListOfItems[i] = new Item(0, 0, Block.Air);
			}
			for (int i = 0; i < recipeList.Length; i++) {
				if (recipeList[i].Shapeless == true) {
					if (BlockList2.Length <= 0) continue;
					BlockID[] RecipeBlocks = new BlockID[recipeList[i].Ingredients.Length];
					Array.Copy(recipeList[i].Ingredients, RecipeBlocks, recipeList[i].Ingredients.Length);
					Array.Sort(RecipeBlocks);
					if (RecipeBlocks.Length != BlockList2.Length) continue;
					bool recipeMatch = true;
					for (int x = 0; x < BlockList2.Length; x++) {
						if (RecipeBlocks[x] != BlockList2[x]) {
							recipeMatch = false;
							break;
						}
					}
					if (recipeMatch) {
						if (BlockList2.Length > 0) Console.WriteLine(BlockList2[0]);
						CraftedItem = new Item(recipeList[i].Count, 0, recipeList[i].Output);
						RecipeIndex = i;
						return;
					}
				} else {
					if (BlockList2D.GetLength(0) != recipeList[i].Pattern.GetLength(0) ||
					    BlockList2D.GetLength(1) != recipeList[i].Pattern.GetLength(1)) continue;
					bool isMatch = true;
					for (int x = 0; x < BlockList2D.GetLength(1); x++)
						for (int y = 0; y < BlockList2D.GetLength(0); y++) {
						if (BlockList2D[y, x] != recipeList[i].Pattern[y, x]) isMatch = false;
					}
					if (!isMatch) continue;
					CraftedItem = new Item(recipeList[i].Count, 0, recipeList[i].Output);
					RecipeIndex = i;
					return;
				}
			}
			CraftedItem = null;
			RecipeIndex = -1;
		}
		
		public BlockID[] CraftingList(Item[] items) {
			BlockID[] BlockList = new BlockID[0];
			int nextFree = 0;
			for (int i = 0;  i < items.Length; i++) {
				Array.Resize(ref BlockList, BlockList.Length + 1);
				if (CraftingItems[i] == null || CraftingItems[i].id == Block.Air) {
					BlockList[nextFree] = Block.Air;
				} else {
					BlockList[nextFree] = CraftingItems[i].id;
				}
				nextFree++;
			}
			//Array.Sort(BlockList);
			return BlockList;
		}
		
		public BlockID[] CraftingList2(Item[] items) {
			BlockID[] BlockList = new BlockID[0];
			int nextFree = 0;
			for (int i = 0;  i < items.Length; i++) {
				if (CraftingItems[i] == null || CraftingItems[i].id == Block.Air) continue;
				Array.Resize(ref BlockList, BlockList.Length + 1);
				BlockList[nextFree] = CraftingItems[i].id;
				nextFree++;
			}
			Array.Sort(BlockList);
			return BlockList;
		}
		
		public BlockID[,] RecipeDim(Recipe recipe) {
			/*for (int x = 0; x < recipe.Ingredients.GetLength(0); x++)
				for (int y = 0; y < recipe.Ingredients.GetLength(*/
			return new BlockID[0,0];
		}
		
		public BlockID[,] CraftingDim(BlockID[] blocks) {
			int gridSize = (int)Math.Sqrt(blocks.Length);
			BlockID[,] blocks2D = make2D(blocks, gridSize, gridSize);
			int xMin = gridSize - 1;
			int xMax = 0;
			int yMin = gridSize - 1;
			int yMax = 0;
			bool hasBlocks = false;
			for (int x = 0; x < gridSize; x++)
				for (int y = 0; y < gridSize; y++) {
				if (blocks2D[y, x] != Block.Air) {
					if (x < xMin) xMin = x;
					if (x > xMax) xMax = x;
					if (y < yMin) yMin = y;
					if (y > yMax) yMax = y;
					hasBlocks = true;
				}
			}
			if (!hasBlocks) return new BlockID[0, 0];
			int xSize;
			if (xMin == xMax) xSize = 1;
			else xSize = (xMax + 1) - (xMin + 1) + 1;
			int ySize;
			if (yMin == yMax) ySize = 1;
			else ySize = (yMax + 1) - (yMin + 1) + 1;
			BlockID[,] blocks2D2 = new BlockID[ySize, xSize];
			for(int x = 0; x < xSize; x++)
				for (int y = 0; y < ySize; y++) {
				int x2 = x + xMin;
				int y2 = y + yMin;
				blocks2D2[y, x] = blocks2D[y2, x2];
			}
			return blocks2D2;
		}
		
		public BlockID[,] make2D(BlockID[] blocks, int width, int height) {
			BlockID[,] blocks2D = new BlockID[height, width];
			
			for (int x = 0; x < width; x++)
				for (int y = 0; y < height; y++) {
				blocks2D[y, x] = blocks[y * width + x];
			}
			
			return blocks2D;
		}
		
		public void SubtractRecipe(Recipe recipe) {
			for (int i = 0; i < CraftingItems.Length; i++) {
				if (CraftingItems[i] != null) {
					CraftingItems[i].Count -= 1;
					if (CraftingItems[i].Count <= 0) CraftingItems[i] = null;
				}
			}
		}
		
		public Item NewItem(Item item) {
			return new Item(item.Count, 0, item.id);
		}
		
		public override bool HandlesKeyDown(Key key) {
			if (SelectedIndex == -1) return false;
			
			if (key == Key.Left || key == Key.Keypad4) {
				ScrollRelative(-1);
			} else if (key == Key.Right || key == Key.Keypad6) {
				ScrollRelative(1);
			} else if (key == Key.Up || key == Key.Keypad8) {
				ScrollRelative(-ElementsPerRow);
			} else if (key == Key.Down || key == Key.Keypad2) {
				ScrollRelative(ElementsPerRow);
			} else {
				return false;
			}
			return true;
		}
		
		void ScrollRelative(int delta) {
			int startIndex = SelectedIndex;
			SelectedIndex += delta;
			if (SelectedIndex < 0) SelectedIndex -= delta;
			if (SelectedIndex >= game.SurvInv.ItemList.Length) SelectedIndex -= delta;
			
			int scrollDelta = (SelectedIndex / ElementsPerRow) - (startIndex / ElementsPerRow);
			scroll.ScrollY += scrollDelta;
			scroll.ClampScrollY();
			RecreateDescTex();
			MoveCursorToSelected();
		}
		
		public override bool HandlesMouseScroll(float delta) {
			int startScrollY = scroll.ScrollY;
			bool bounds = Contains(TableX - scroll.Width, TableY, TableWidth + scroll.Width,
			                       TableHeight, game.Mouse.X, game.Mouse.Y);
			if (!bounds) return false;
			
			scroll.HandlesMouseScroll(delta);
			if (SelectedIndex == - 1) return true;
			
			SelectedIndex += (scroll.ScrollY - startScrollY) * ElementsPerRow;
			if (SelectedIndex >= game.SurvInv.ItemList.Length) SelectedIndex = -1;
			
			RecreateDescTex();
			return true;
		}
		
		public override bool HandlesMouseUp(int mouseX, int mouseY, MouseButton button) {
			return scroll.HandlesMouseUp(mouseX, mouseY, button);
		}
	}
}
