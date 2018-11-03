// Copyright 2014-2017 ClassicalSharp | Licensed under BSD-3
using System;
using System.Collections.Generic;
using OpenTK;
using BlockID = System.UInt16;

namespace ClassicalSharp {
	public sealed class SurvivalInventory : IGameComponent {
		
		int selectedI;
		Game game;
		//public List<Item> ItemList;
		public Item[] ItemList;
		
		/// <summary> Gets or sets the index of the selected block within the current row. </summary>
		/// <remarks> Fails if the server has forbidden user from changing the held block. </remarks>
		public int SelectedIndex {
			get { return selectedI; }
			set {
				selectedI = value; game.Events.RaiseHeldBlockChanged();
			}
		}
		
		/*public struct Item {
			public Item(byte c, byte s, byte id) {
				this.id = id;
				Count = c;
				//Slot = s;
			}
			public byte Count;
			//public byte Slot;
			public BlockID id;
		}*/
		
		/*public Item GetItem(byte slot) {
			Item item = ItemList.Find(delegate(Item i) {return i.Slot == slot;});
			return item;
		}
		
		public bool ContainsItem(byte slot) {
			return ItemList.Exists(delegate(Item i) {return i.Slot == slot;});
		}
		
		public void SetItem(Item item) {
			Item item = ItemList.Find(delegate(Item i) {return i.Slot == slot;});
		}*/
		
		public sbyte TryAddItem(sbyte Count, BlockID id) {
			int itemIndex = Array.FindIndex(ItemList, delegate(Item i) {
				return (i != null && i.id == id && i.Count < 64);});
			Item item = null;
			if (itemIndex != -1) {
			item = ItemList[itemIndex];
			}
			//int itemid = ItemList.IndexOf(item);
			if (itemIndex > ItemList.Length) {
				return Count;
			}
			if (item == null) {
				int index = NextFreeSlot();
				if (index < ItemList.Length && index != -1) {
					ItemList[index] = new Item(Count, (byte)index, id);
					return 0;
				}
				return Count;
			} else if (item.Count + Count <= 64) {
				item.Count += Count;
				ItemList[itemIndex] = item;
				return 0;
			} else {
				Count = (sbyte)((item.Count + Count) - 64);
				item.Count = 64;
				ItemList[itemIndex] = item;
				return Count;
			}
		}
		
		/*public void RemoveItem(Item item) {
			ItemList.Remove(item);
		}
		
		public void RemoveItem(byte slot) {
			Item item = ItemList.Find(delegate(Item i) {return i.Slot == slot;});
			ItemList.Remove(item);
		}*/
		public int NextFreeSlot() {
			int index = Array.FindIndex(ItemList, delegate(Item i) {
				return i == null;
			                               });
			/*for (int i = 0; i <= 9; i++) {
				if (ItemList[i] = 0) 
			}*/
			//Console.WriteLine(index);
			//Console.WriteLine(ItemList.Capacity);
			return index;
		}
		
		public BlockID Selected {
			get {
				if (ItemList[selectedI] != null) {
					return ItemList[selectedI].id;
				} else {
					return Block.Air;
				}
			}
			set {
				/*if (!CanChangeSelected()) return;
				CanPick = true;
				
				// Change the selected index if this block already in hotbar
				for (int i = 0; i < BlocksPerRow; i++) {
					if (this[i] != value) continue;
					
					BlockID prevSelected = this[selectedI];
					this[selectedI] = this[i];
					this[i] = prevSelected;
					
					game.Events.RaiseHeldBlockChanged();
					return;
				}
				
				this[selectedI] = value;
				game.Events.RaiseHeldBlockChanged();*/
			}
		}
		
		public void Init(Game game) {
			this.game = game;
			//this.ItemList = new List<Item>();
			this.ItemList = new Item[36];
			Item item = new Item(10, 0, Block.TNT);
			ItemList[8] = item;
		}
		
		public void Reset(Game game) { }
		
		public void Ready(Game game) { }
		public void OnNewMap(Game game) { }
		public void OnNewMapLoaded(Game game) { }
		public void Dispose() { } 
		
	}
	
	public sealed class Item {
		public Item(sbyte c, byte s, BlockID id) {
			this.id = id;
			Count = c;
			isBlock = true;
		}
		public Item(sbyte c, byte s, BlockID id, bool isBlock) {
			this.id = id;
			Count = c;
			isBlock = isBlock;
		}
		public Item(Item item) {
			this.id = item.id;
			this.Count = item.Count;
			this.isBlock = item.isBlock;
		}
		public sbyte Count;
		//public byte Slot;
		public BlockID id;
		public bool isBlock;
	}
}
