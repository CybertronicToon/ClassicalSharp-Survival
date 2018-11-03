// Copyright 2014-2017 ClassicalSharp | Licensed under BSD-3
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using ClassicalSharp.Entities;
using ClassicalSharp.Events;
using ClassicalSharp.Renderers;
using OpenTK;
using OpenTK.Input;
using BlockID = System.UInt16;

namespace ClassicalSharp.Commands {
	
	public sealed class HelpCommand : Command {
		
		public HelpCommand() {
			Name = "Help";
			Help = new string[] {
				"&a/client help [command name]",
				"&eDisplays the help for the given command.",
			};
		}
		
		public override void Execute(string[] args) {
			if (args.Length == 1) {
				game.Chat.Add("&eList of client commands:");
				game.CommandList.PrintDefinedCommands(game);
				game.Chat.Add("&eTo see help for a command, type /client help [cmd name]");
			} else {
				Command cmd = game.CommandList.GetMatch(args[1]);
				if (cmd == null) return;
				string[] help = cmd.Help;
				for (int i = 0; i < help.Length; i++) {
					game.Chat.Add(help[i]);
				}
			}
		}
	}

	public sealed class GpuInfoCommand : Command {
		
		public GpuInfoCommand() {
			Name = "GpuInfo";
			Help = new string[] {
				"&a/client gpuinfo",
				"&eDisplays information about your GPU.",
			};
		}
		
		public override void Execute(string[] args) {
			string[] lines = game.Graphics.ApiInfo;
			for (int i = 0; i < lines.Length; i++) {
				game.Chat.Add("&a" + lines[i]);
			}
		}
	}
	
	public sealed class RenderTypeCommand : Command {
		
		public RenderTypeCommand() {
			Name = "RenderType";
			Help = new string[] {
				"&a/client rendertype [normal/legacy/legacyfast]",
				"&bnormal: &eDefault renderer, with all environmental effects enabled.",
				"&blegacy: &eMay be slightly slower than normal, but produces the same environmental effects.",
				"&blegacyfast: &eSacrifices clouds, fog and overhead sky for faster performance.",
				"&bnormalfast: &eSacrifices clouds, fog and overhead sky for faster performance.",
			};
		}
		
		public override void Execute(string[] args) {
			if (args.Length == 1) {
				game.Chat.Add("&e/client: &cYou didn't specify a new render type.");
			} else if (game.SetRenderType(args[1])) {
				game.Chat.Add("&e/client: &fRender type is now " + args[1] + ".");
			} else {
				game.Chat.Add("&e/client: &cUnrecognised render type &f\"" + args[1] + "\"&c.");
			}
		}
	}
	
	public sealed class ResolutionCommand : Command {
		
		public ResolutionCommand() {
			Name = "Resolution";
			Help = new string[] {
				"&a/client resolution [width] [height]",
				"&ePrecisely sets the size of the rendered window.",
			};
		}
		
		public override void Execute(string[] args) {
			int width, height;
			if (args.Length < 3) {
				game.Chat.Add("&e/client: &cYou didn't specify width and height");
			} else if (!Int32.TryParse(args[1], out width) || !Int32.TryParse(args[2], out height)) {
				game.Chat.Add("&e/client: &cWidth and height must be integers.");
			} else if (width <= 0 || height <= 0) {
				game.Chat.Add("&e/client: &cWidth and height must be above 0.");
			} else {
				game.window.ClientSize = new Size(width, height);
				Options.Set(OptionsKey.WindowWidth, width);
				Options.Set(OptionsKey.WindowHeight, height);
			}
		}
	}

	
	public sealed class ModelCommand : Command {
		
		public ModelCommand() {
			Name = "Model";
			Help = new string[] {
				"&a/client model [name]",
				"&bnames: &echibi, chicken, creeper, human, pig, sheep",
				"&e       skeleton, spider, zombie, sitting, <numerical block id>",
			};
			SingleplayerOnly = true;
		}
		
		public override void Execute(string[] args) {
			if (args.Length == 1) {
				game.Chat.Add("&e/client model: &cYou didn't specify a model name.");
			} else {
				game.LocalPlayer.SetModel(Utils.ToLower(args[1]));
			}
		}
	}
	
	public sealed class CuboidCommand : Command {
		
		public CuboidCommand() {
			Name = "Cuboid";
			Help = new string[] {
				"&a/client cuboid [block] [persist]",
				"&eFills the 3D rectangle between two points with [block].",
				"&eIf no block is given, uses your currently held block.",
				"&e  If persist is given and is \"yes\", then the command",
				"&e  will repeatedly cuboid, without needing to be typed in again.",
			};
			SingleplayerOnly = true;
		}
		int block = -1;
		Vector3I mark1, mark2;
		bool persist = false;
		
		public override void Execute(string[] args) {
			game.UserEvents.BlockChanged -= BlockChanged;
			block = -1;
			mark1 = new Vector3I(int.MaxValue);
			mark2 = new Vector3I(int.MaxValue);
			persist = false;
			
			if (!ParseBlock(args)) return;
			if (args.Length > 2 && Utils.CaselessEquals(args[2], "yes"))
				persist = true;
			
			game.Chat.Add("&eCuboid: &fPlace or delete a block.", MessageType.ClientStatus3);
			game.UserEvents.BlockChanged += BlockChanged;
		}
		
		bool ParseBlock(string[] args) {
			if (args.Length == 1) return true;
			if (Utils.CaselessEquals(args[1], "yes")) { persist = true; return true; }
			
			int temp = BlockInfo.FindID(args[1]);
			BlockID block = 0;
			
			if (temp != -1) {
				block = (BlockID)temp;
			} else if (!BlockID.TryParse(args[1], out block)) {
				game.Chat.Add("&eCuboid: &c\"" + args[1] + "\" is not a valid block name or id."); return false;
			}
			
			if (block >= Block.CpeCount && !BlockInfo.IsCustomDefined(block)) {
				game.Chat.Add("&eCuboid: &cThere is no block with id \"" + args[1] + "\"."); return false;
			}
			this.block = block;
			return true;
		}

		void BlockChanged(object sender, BlockChangedEventArgs e) {
			if (mark1.X == int.MaxValue) {
				mark1 = e.Coords;
				game.UpdateBlock(mark1.X, mark1.Y, mark1.Z, e.OldBlock);
				game.Chat.Add("&eCuboid: &fMark 1 placed at (" + e.Coords + "), place mark 2.",
				              MessageType.ClientStatus3);
			} else {
				mark2 = e.Coords;				
				DoCuboid();		
				game.Chat.Add(null, MessageType.ClientStatus3);
				
				if (!persist) {
					game.UserEvents.BlockChanged -= BlockChanged;
				} else {
					mark1 = new Vector3I(int.MaxValue);
					game.Chat.Add("&eCuboid: &fPlace or delete a block.", MessageType.ClientStatus3);
				}
			}
		}
		
		void DoCuboid() {
			Vector3I min = Vector3I.Min(mark1, mark2);
			Vector3I max = Vector3I.Max(mark1, mark2);
			if (!game.World.IsValidPos(min) || !game.World.IsValidPos(max)) return;
			
			BlockID toPlace = (BlockID)block;
			if (block == -1) toPlace = game.Inventory.Selected;
			
			for (int y = min.Y; y <= max.Y; y++)
				for (int z = min.Z; z <= max.Z; z++)
					for (int x = min.X; x <= max.X; x++) 
			{
				game.UpdateBlock(x, y, z, toPlace);
			}
		}
	}	
	
	public sealed class TeleportCommand : Command {
		
		public TeleportCommand() {
			Name = "TP";
			Help = new string[] {
				"&a/client tp [x y z]",
				"&eMoves you to the given coordinates.",
			};
			SingleplayerOnly = true;
		}
		
		public override void Execute(string[] args) {
			if (args.Length != 4) {
				game.Chat.Add("&e/client teleport: &cYou didn't specify X, Y and Z coordinates.");
			} else {
				float x = 0, y = 0, z = 0;
				if (!Utils.TryParseDecimal(args[1], out x) ||
				    !Utils.TryParseDecimal(args[2], out y) ||
				    !Utils.TryParseDecimal(args[3], out z)) {
					game.Chat.Add("&e/client teleport: &cCoordinates must be decimals");
					return;
				}
				
				Vector3 v = new Vector3(x, y, z);
				LocationUpdate update = LocationUpdate.MakePos(v, false);
				game.LocalPlayer.SetLocation(update, false);
			}
		}
	}
	
		public sealed class SunLightSubCommand : Command {
		
		public SunLightSubCommand() {
			Name = "SunLightSub";
			Help = new string[] {
				"Subtracts sunlight.",
			};
		}
		
		public override void Execute(string[] args) {
			if (args.Length != 2) {
				game.Chat.Add("ERROR!");
			} else {
				float light = 0;
				if (!Utils.TryParseDecimal(args[1], out light)) {
					game.Chat.Add("ERROR!");
					return;
				}
				if (light > 15 || light < 0) {
					game.Chat.Add("INVALID!");
				}
				byte light2 = (byte)light;
				
				game.Lighting.sunLightSub = light2;
				game.MapRenderer.Refresh();
				game.MapBordersRenderer.ResetSides();
			}
		}
	}
	
		public sealed class SetTimeCommand : Command {
		
		public SetTimeCommand() {
			Name = "SetTime";
			Help = new string[] {
				"Sets the time.",
			};
		}
		
		public override void Execute(string[] args) {
			if (args.Length != 2) {
				game.Chat.Add("ERROR!");
			} else {
				float time = 0;
				if (!Utils.TryParseDecimal(args[1], out time)) {
					game.Chat.Add("ERROR!");
					return;
				}
				if (time < 0) {
					game.Chat.Add("INVALID!");
				}
				int time2 = (int)time;
				
				game.Server.Ticks = time2;
			}
		}
	}
	
		public sealed class DoDayNightCycleCommand : Command {
		
		public DoDayNightCycleCommand() {
			Name = "DoDayNightCycle";
			Help = new string[] {
				"Enables or disables the day/night cycle.",
			};
		}
		
		public override void Execute(string[] args) {
			if (args.Length != 2) {
				game.Chat.Add("ERROR!");
			} else {
				bool DoDayNightCycle;
				if (Utils.CaselessEquals(args[1], "true")) {
					DoDayNightCycle = true;
				} else if (Utils.CaselessEquals(args[1], "false")) {
					DoDayNightCycle = false;
				} else {
					game.Chat.Add("INVALID!");
					return;
				}
				
				game.Server.DoDayNightCycle = DoDayNightCycle;
			}
		}
	}
	
	public sealed class SpawnCommand : Command {
		public SpawnCommand() {
			Name = "Spawn";
			SingleplayerOnly = true;
		}
		
		public override void Execute(string[] args) {
			if (args.Length != 2 && args.Length != 3) {
				game.Chat.Add("Unspecified error");
			} else {
				BlockID id = 5;
				EntityItem Item = new EntityItem(game);
				if (!BlockID.TryParse(args[1], out id)) {
					game.Chat.Add("Unspecified error");
					return;
				}
				
				Item.ItemId = id;
				float x = game.LocalPlayer.Position.X;
				float z = game.LocalPlayer.Position.Z;
				Vector3 pos = Respawn.FindSpawnPosition(game, x, z, Item.Size);
				Item.SetLocation(LocationUpdate.MakePos(pos, false), false);
				game.Entities.List[200] = Item;
			}
		}
	}
}