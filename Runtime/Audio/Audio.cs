using System.Collections.Generic;
using System;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Twenty2.VomitLib.Audio
{
    public static class Audio
    {
        public enum AudioType
        {
            SE,
            BGM,
        }
        
        public class AudioClipData
        {
            public AudioClip Clip;
            public AudioType Type;
            public bool IsCache;
        }
        
        private static AudioSource _bgmSource;
        private static AudioSource _seSource;
        
        private static Dictionary<string, AudioClipData> _clipsCaches = new();

        private static Func<string, AudioClipData> _onLoadAudioClip;
        
        private static Action<AudioClip> _onReleaseAudioClip;

        private static bool _isInited = false;

        public static void Init(Func<string, AudioClipData> onLoadAudioClip, Action<AudioClip> onReleaseAudioClip)
        {
            if (_isInited)
            {
                Log.Error("Audio.Init() has been called!");
                return;
            }
            
            _onLoadAudioClip = onLoadAudioClip;
            _onReleaseAudioClip = onReleaseAudioClip;
            
            _bgmSource = new GameObject($"__AudioSource__[BGM]").AddComponent<AudioSource>();
            _bgmSource.loop = true;
            Object.DontDestroyOnLoad(_bgmSource.gameObject);
            
            _seSource = new GameObject($"__AudioSource__[SE]").AddComponent<AudioSource>();
            _seSource.loop = false;
            Object.DontDestroyOnLoad(_seSource.gameObject);
            
            SetSEVolume(1);
            SetBgmVolume(1);
            
            _isInited = true;
        }

        public static void ReleaseCaches()
        {
            StopBGM();
            
            _seSource.Stop();
            
            var clips = _clipsCaches.Values.ToList();
            _clipsCaches.Clear();
            foreach (var clip in clips)
            {
                _onReleaseAudioClip?.Invoke(clip.Clip);
            }
        }

        public static void Play(string key)
        {
            if (!_isInited)
            {
                Log.Error("Audio.Init() has not been called!");
                return;
            }
            
            var acData = SearchAudio(key);
            
            if(acData != null)
            {
                Play(acData);
            }
        }

        #region SE
        public static bool IsSEMute => _seSource.mute;

        public static float SeVolume => _seSource.volume;
        
        public static void SetSEVolume(float v)
        {
            _seSource.volume = v;
        }

        public static void MuteSE(bool mute)
        {
            _seSource.mute = mute;
        }
        
        #endregion

        #region BGM
        
        public static bool IsBgmMute => _bgmSource.mute;
        
        public static float BGMVolume => _bgmSource.volume;
        
        public static bool IsBGMPlaying => _bgmSource.isPlaying;

        public static AudioClipData CurrentBGM { get; private set; }
        
        public static void SetBgmVolume(float v)
        {
            _bgmSource.volume = v;
        }

        public static void MuteBGM(bool mute)
        {
            _bgmSource.mute = mute;
        }

        public static void StopBGM()
        {
            _bgmSource.Stop();
            _bgmSource.clip = null;
            
            if (CurrentBGM != null && !CurrentBGM.IsCache)
            {
                _onReleaseAudioClip?.Invoke(CurrentBGM.Clip);
            }
            CurrentBGM = null;
        }

        public static void PauseBGM()
        {
            _bgmSource.Pause();
        }

        public static void ResumeBGM()
        {
            _bgmSource.UnPause();
        }
        
        #endregion
        
        private static void Play(AudioClipData acData)
        {
            switch (acData.Type)
            {
                case AudioType.BGM:
                    if (acData.Clip == _bgmSource.clip)
                    {
                        Log.Info($"Audio {acData.Clip.name} is playing!");
                        return;
                    }

                    if (CurrentBGM != null && !CurrentBGM.IsCache)
                    {
                        _onReleaseAudioClip?.Invoke(CurrentBGM.Clip);
                    }

                    CurrentBGM = acData;
                    _bgmSource.clip = acData.Clip;
                    _bgmSource.Play();
                    break;
                case AudioType.SE:
                    _seSource.PlayOneShot(acData.Clip);
                    break;
            }
        }

        private static AudioClipData SearchAudio(string key)
        {
            if (_clipsCaches.TryGetValue(key, out var ret))
            {
                return ret;
            }

            ret = _onLoadAudioClip?.Invoke(key);

            if (ret == null)
            {
                Log.Error($"Audio {key} Not Found!");
                return null;
            }

            if (ret.IsCache)
            {
                _clipsCaches.Add(key, ret);    
            }
            
            return ret;
        }
    }
}