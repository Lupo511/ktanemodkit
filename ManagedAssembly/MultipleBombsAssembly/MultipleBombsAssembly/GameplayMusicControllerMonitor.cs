using DarkTonic.MasterAudio;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace MultipleBombsAssembly
{
    public class GameplayMusicControllerMonitor : MonoBehaviour
    {
        private GameplayMusicController gameplayMusicController;
        private PlaylistController playlistController;
        private FieldInfo isPlayingField;
        private FieldInfo stingerResultField;
        private string currentStinger;
        private bool stingerPlayed;
        private int currentSongIndex;

        public void Awake()
        {
            gameplayMusicController = GetComponent<GameplayMusicController>();
            playlistController = GetComponent<PlaylistController>();
            isPlayingField = gameplayMusicController.GetType().GetField("isPlaying", BindingFlags.Instance | BindingFlags.NonPublic);
            stingerResultField = gameplayMusicController.GetType().GetField("stingerResult", BindingFlags.Instance | BindingFlags.NonPublic);
        }

        public void Start()
        {
            gameplayMusicController.enabled = false;
        }

        public void OnDestroy()
        {
            gameplayMusicController.enabled = true;
        }

        public void NewRoundStarted()
        {
            currentStinger = gameplayMusicController.Settings.StingerName;
        }

        public void Update()
        {
            if (gameplayMusicController.Settings.PlaylistName != string.Empty && SceneManager.Instance != null && SceneManager.Instance.GameplayState != null && (bool)isPlayingField.GetValue(gameplayMusicController))
            {
                float timeRemaining = float.MaxValue;
                float totalTime = float.MaxValue;
                foreach (Bomb bomb in SceneManager.Instance.GameplayState.Bombs)
                {
                    if (!bomb.IsSolved() && bomb.GetTimer().TimeRemaining < timeRemaining)
                        timeRemaining = bomb.GetTimer().TimeRemaining;
                    if (bomb.TotalTime < totalTime)
                        totalTime = bomb.TotalTime;
                }
                if (currentStinger != null && currentStinger != string.Empty)
                {
                    bool stingerTime = timeRemaining < (30f + SceneManager.Instance.GameplayState.AdjustTime(gameplayMusicController.Settings.StingerTime));
                    if (stingerTime && !stingerPlayed)
                    {
                        if (gameplayMusicController.Settings.StingerName != null && gameplayMusicController.Settings.StingerName != string.Empty) //To make sure it's as consistent as possible with vanilla behaviour
                            gameplayMusicController.Settings.StingerName = null;
                        stingerResultField.SetValue(gameplayMusicController, MasterAudio.PlaySound3DAtTransform(currentStinger, transform, 1f, null, 0f, null, false, false));
                        stingerPlayed = true;
                    }
                    if (!stingerTime && stingerPlayed)
                    {
                        PlaySoundResult stingerResult = (PlaySoundResult)stingerResultField.GetValue(gameplayMusicController);
                        if (stingerResult != null && stingerResult.ActingVariation != null)
                            stingerResult.ActingVariation.Stop();
                        stingerPlayed = false;
                    }
                }
                if (timeRemaining < 30f)
                {
                    if (currentSongIndex != playlistController.CurrentPlaylist.MusicSettings.Count - 1)
                    {
                        currentSongIndex = playlistController.CurrentPlaylist.MusicSettings.Count - 1;
                        playlistController.ClearQueue();
                        playlistController.PlaySong(playlistController.CurrentPlaylist.MusicSettings[currentSongIndex], PlaylistController.AudioPlayType.PlayNow);
                    }
                }
                else
                {
                    int newSongIndex = 0;
                    float timeRatio = timeRemaining / totalTime;
                    if (gameplayMusicController.Settings.useCrossfade)
                    {
                        newSongIndex = Mathf.Clamp(playlistController.CurrentPlaylist.MusicSettings.Count - (int)(timeRatio * playlistController.CurrentPlaylist.MusicSettings.Count), 0, playlistController.CurrentPlaylist.MusicSettings.Count);
                    }
                    else
                    {
                        newSongIndex = Mathf.Clamp(playlistController.CurrentPlaylist.MusicSettings.Count - (int)(timeRatio * (playlistController.CurrentPlaylist.MusicSettings.Count - 1)), 0, playlistController.CurrentPlaylist.MusicSettings.Count - 2);
                    }
                    if (newSongIndex != currentSongIndex)
                    {
                        currentSongIndex = newSongIndex;
                        playlistController.ClearQueue();
                        playlistController.QueuePlaylistClip(playlistController.CurrentPlaylist.MusicSettings[currentSongIndex].songName, true);
                    }
                }
            }
        }
    }
}
