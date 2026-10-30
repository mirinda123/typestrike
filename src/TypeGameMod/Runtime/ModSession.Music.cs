using System;
using System.IO;

namespace TypeGameMod
{
    internal sealed partial class ModSession
    {
        WeaponMusic music;
        readonly MusicPolicy musicPolicy = new MusicPolicy();
        internal volatile bool WeaponMusicEnabled = true;
        void StartMusic()
        {
            music = new WeaponMusic(
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "audio", "MOON-Paris.m4a"), delegate {
                    return IsAlive &&
                           BitConverter.ToUInt32(Read(ControlBase + Control.MusicState, 4), 0) == 1;
                }, log);
        }

        void UpdateMusic(bool armed, bool inRound, bool focused)
        {
            if (music == null)
                return;
            bool play = musicPolicy.Observe(armed, WeaponMusicEnabled, music.Ready, inRound);
            music.Update(musicPolicy.ReplacesNative, focused && play);
            Write(ControlBase + Control.MusicWanted,
                  BitConverter.GetBytes(musicPolicy.ReplacesNative ? 1u : 0u));
            Write(ControlBase + Control.MusicTelemetry, music.Telemetry());
        }

        void StopMusic()
        {
            if (music != null)
            {
                music.Dispose();
                music = null;
            }
            if (processHandle != IntPtr.Zero && IsAlive && caveBase != 0)
                Write(ControlBase + Control.MusicWanted, BitConverter.GetBytes(0u));
        }
    }
}
