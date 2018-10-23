// Copyright 2014-2017 ClassicalSharp | Licensed under BSD-3
using System;
using System.Threading;
using ClassicalSharp.Events;
using SharpWave;
using SharpWave.Codecs;
using OpenTK;

namespace ClassicalSharp.Audio {
	
	public sealed partial class AudioPlayer {
		
		Soundboard digBoard, stepBoard;
		const int maxSounds = 6;
		
		public void SetSounds(int volume) {
			if (volume > 0) InitSound();
			else DisposeSound();
		}
		
		void InitSound() {
			if (digBoard == null) InitSoundboards();
			monoOutputs = new IAudioOutput[maxSounds];
			stereoOutputs = new IAudioOutput[maxSounds];
		}
		
		void InitSoundboards() {
			digBoard = new Soundboard();
			digBoard.Init("dig_", files);
			stepBoard = new Soundboard();
			stepBoard.Init("step_", files);
		}

		void PlayBlockSound(object sender, BlockChangedEventArgs e) {
			if (e.Block == 0) {
				PlayDigSound(BlockInfo.DigSounds[e.OldBlock]);
			} else if (!game.ClassicMode) {
				PlayDigSound(BlockInfo.StepSounds[e.Block]);
			}
		}
		
		public void PlayDigSound(byte type) { PlaySound(type, digBoard); }
		
		public void PlayStepSound(byte type) { PlaySound(type, stepBoard); }
		
		AudioChunk chunk = new AudioChunk();
		void PlaySound(byte type, Soundboard board) {
			if (type == SoundType.None || monoOutputs == null) return;
			Sound snd = board.PickRandomSound(type);
			if (snd == null) return;
			
			chunk.Channels = snd.Channels;
			chunk.BitsPerSample = snd.BitsPerSample;
			chunk.BytesOffset = 0;
			chunk.BytesUsed = snd.Data.Length;
			chunk.Data = snd.Data;
			
			float volume = game.SoundsVolume / 100.0f;
			if (board == digBoard) {
				if (type == SoundType.Metal) chunk.SampleRate = (snd.SampleRate * 6) / 5;
				else chunk.SampleRate = (snd.SampleRate * 4) / 5;
			} else {
				volume *= 0.50f;
				
				if (type == SoundType.Metal) chunk.SampleRate = (snd.SampleRate * 7) / 5;
				else chunk.SampleRate = snd.SampleRate;
			}
			
			if (snd.Channels == 1) {
				PlayCurrentSound(monoOutputs, volume);
			} else if (snd.Channels == 2) {
				PlayCurrentSound(stereoOutputs, volume);
			}
		}
		
		IAudioOutput firstSoundOut;
		void PlayCurrentSound(IAudioOutput[] outputs, float volume) {
			for (int i = 0; i < monoOutputs.Length; i++) {
				IAudioOutput output = outputs[i];
				if (output == null) output = MakeSoundOutput(outputs, i);
				if (!output.DoneRawAsync()) continue;	
				
				LastChunk l = output.Last;
				if (l.Channels == 0 || (l.Channels == chunk.Channels && l.BitsPerSample == chunk.BitsPerSample 
				                        && l.SampleRate == chunk.SampleRate)) {
					PlaySound(output, volume); return;
				}
			}
			
			// This time we try to play the sound on all possible devices,
			// even if it requires the expensive case of recreating a device
			for (int i = 0; i < monoOutputs.Length; i++) {
				IAudioOutput output = outputs[i];
				if (!output.DoneRawAsync()) continue;
				
				PlaySound(output, volume); return;
			}
		}
		
		
		IAudioOutput MakeSoundOutput(IAudioOutput[] outputs, int i) {
			IAudioOutput output = GetPlatformOut();
			output.Create(1, firstSoundOut);
			if (firstSoundOut == null)
				firstSoundOut = output;
			
			outputs[i] = output;
			return output;
		}
		
		void PlaySound(IAudioOutput output, float volume) {
			try {
				output.Initalise(chunk);
				//uint newSrc = output.GetSource();
				//output.SetSource(newSrc);
				output.SetVolume(volume);
				
				/*LocalPlayer player = game.LocalPlayer;
				
				Vector3 camPos = game.CurrentCameraPos;
				Vector3 eyePos = game.LocalPlayer.EyePosition;
				Vector3 oneY = Vector3.UnitY;
				
				Vector3 lookPos;
				//lookPos.X = (float)(Math.Cos(player.HeadX) * Math.Sin(player.HeadY));
				//lookPos.Y = (float)(-Math.Sin(player.HeadX));
				//lookPos.Z = (float)(Math.Cos(player.HeadY) * Math.Cos(player.HeadX));
				lookPos.X = game.Graphics.View.Row1.X;
				lookPos.Y = game.Graphics.View.Row1.Y;
				lookPos.Z = game.Graphics.View.Row1.Z;
				lookPos = Vector3.Normalize(lookPos);
				
				Vector3 upPos;
				upPos.X = game.Graphics.View.Row2.X;
				upPos.Y = game.Graphics.View.Row2.Y;
				upPos.Z = game.Graphics.View.Row2.Z;
				upPos = Vector3.Normalize(upPos);*/
				
				Vector3 pos = game.LocalPlayer.EyePosition;
				Vector3 feetPos = game.LocalPlayer.Position;
				Vector3 vel = game.LocalPlayer.Velocity;
				float yaw = game.LocalPlayer.HeadY;
				output.SetSoundGain(0f);
				output.SetSoundRefDist(0.5f);
				output.SetSoundMaxDist(1000f);
				output.SetListenerPos(0, 0, 0);
				//output.SetListenerPos(pos.X, pos.Y, pos.Z);
				//output.SetListenerVel(vel.X, vel.Y, vel.Z);
				//output.SetListenerDir(yaw);
				//output.SetSoundPos(feetPos.X, feetPos.Y, feetPos.Z);
				if (!output.IsPosSet()) {
				//	output.SetSoundPos(feetPos.X, feetPos.Y, feetPos.Z);
				//	output.SetSoundVel(0, 0, 0);
				}
				output.SetSoundPos(0, 0, 0);
				//output.SetSoundRelative(true);
				
				output.PlayRawAsync(chunk);
			} catch (InvalidOperationException ex) {
				ErrorHandler.LogError("AudioPlayer.PlayCurrentSound()", ex);
				if (ex.Message == "No audio devices found")
					game.Chat.Add("&cNo audio devices found, disabling sounds.");
				else
					game.Chat.Add("&cAn error occured when trying to play sounds, disabling sounds.");
				
				SetSounds(0);
				game.SoundsVolume = 0;
			}
		}
		
		void DisposeSound() {
			DisposeOutputs(ref monoOutputs);
			DisposeOutputs(ref stereoOutputs);
			if (firstSoundOut != null) {
				firstSoundOut.Dispose();
				firstSoundOut = null;
			}
		}
		
		void DisposeOutputs(ref IAudioOutput[] outputs) {
			if (outputs == null) return;
			bool soundPlaying = true;
			
			while (soundPlaying) {
				soundPlaying = false;
				for (int i = 0; i < outputs.Length; i++) {
					if (outputs[i] == null) continue;
					soundPlaying |= !outputs[i].DoneRawAsync();
				}
				if (soundPlaying)
					Thread.Sleep(1);
			}
			
			for (int i = 0; i < outputs.Length; i++) {
				if (outputs[i] == null || outputs[i] == firstSoundOut) continue;
				outputs[i].Dispose();
			}
			outputs = null;
		}
	}
}
