// Copyright 2014-2017 ClassicalSharp | Licensed under BSD-3
using System;
using System.Collections.Generic;
using OpenTK;
using BlockID = System.UInt16;

namespace ClassicalSharp {
	public class Recipes {
		public Recipes() {
			
		}
		
		public static Recipe[] MakeRecipeList() {
			Recipe[] recipes = new Recipe[0];
			Recipe recipe;
			BlockID[] ingredients;
			BlockID[,] pattern;
			
			Array.Resize(ref recipes, recipes.Length + 1);
			pattern = new BlockID[2,2] {
				{Block.Wood, Block.Wood},
				{Block.Wood, Block.Wood},
			};
			recipes[0] = new Recipe(pattern, Block.Log, 1);
			
			Array.Resize(ref recipes, recipes.Length + 1);
			ingredients = new BlockID[1] {Block.Sand};
			recipes[1] = new Recipe(ingredients, Block.Glass, 1);
			
			Array.Resize(ref recipes, recipes.Length + 1);
			pattern = new BlockID[2,2] {
				{Block.Cobblestone, Block.Cobblestone},
				{Block.Cobblestone, Block.Cobblestone},
			};
			recipes[2] = new Recipe(pattern, Block.MossyRocks, 4);
			
			Array.Resize(ref recipes, recipes.Length + 1);
			ingredients = new BlockID[1] {Block.Wood};
			recipes[3] = new Recipe(ingredients, Block.Log, 1);
			
			Array.Resize(ref recipes, recipes.Length + 1);
			ingredients = new BlockID[1] {Block.Cobblestone};
			recipes[4] = new Recipe(ingredients, Block.Stone, 1);
			
			Array.Resize(ref recipes, recipes.Length + 1);
			pattern = new BlockID[1,3] {
				{Block.Cobblestone, Block.Cobblestone, Block.Cobblestone},
			};
			recipes[5] = new Recipe(pattern, Block.Slab, 3);
			
			return recipes;
		}
	}
	
	public sealed class Recipe {
		public Recipe(BlockID[] Ingredients, BlockID output, sbyte count) {
			this.Shapeless = true;
			this.Ingredients = Ingredients;
			this.Output = output;
			this.Count = count;
		}
		public Recipe(BlockID[,] Pattern, BlockID output, sbyte count) {
			this.Shapeless = false;
			this.Pattern = Pattern;
			this.Output = output;
			this.Count = count;
		}
		public bool Shapeless;
		public BlockID[] Ingredients;
		public BlockID[,] Pattern;
		public BlockID Output;
		public sbyte Count;
	}
}
