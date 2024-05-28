using System.Collections.Generic;
using QFramework;
using System;
using Cysharp.Threading.Tasks;
using Twenty2.VomitLib.Config;
using UnityEngine;
using UnityEngine.AddressableAssets;
using Object = UnityEngine.Object;

namespace Twenty2.VomitLib.Audio
{
    public static class Audio
    {
        /// <summary>
        /// 当前播放的背景音乐
        /// </summary>
        public static AudioClip CurrentBgm => _as.clip;

        private static AudioSource _as;

        private static float _bgmVolume;
        private static float _seVolume;
        
        private static Dictionary<string, AudioClip> _audioClipsCache = new();

        private static AudioConfig _config;
        
        public static void Init(bool preload, float bgmVolume = 1f, float seVolume = 1f)
        {
            _config = Vomit.RuntimeConfig.AudioConfig;
            
            _as = new GameObject("__AudioSource__").AddComponent<AudioSource>();
            Object.DontDestroyOnLoad(_as.gameObject);

            SetSEVolume(seVolume);
            SetBgmVolume(bgmVolume);

            if (preload)
            {
                Preload();
            }
        }

        public static void Preload()
        {
            var label = _config.AudioLabel;

            Addressables.LoadAssetsAsync<AudioClip>(label, OnLoad);
            
            return;
            
            void OnLoad(AudioClip clip)
            {
                _audioClipsCache.Add(clip.name, clip);
                #if UNITY_EDITOR
                LogKit.I($"Load Audio : {clip.name}");
                #endif
            }
        }
        
        public static void PlayBgm(string bgm, bool isLoop)
        {
            PlayBgm(SearchAudio(bgm, false), 0, isLoop, null);
        }
        
        public static void PlayBgm(string bgm, float mix, Action onPlayFinished)
        {
            PlayBgm(SearchAudio(bgm, false), mix, false, onPlayFinished);
        }
        
        public static void PlaySE(string se)
        {
            PlaySE(SearchAudio(se, true));
        }
        
        public static void SetSEVolume(float v)
        {
            _seVolume = v;
        }

        public static void SetBgmVolume(float v)
        {
            _bgmVolume = v;
        }
        
        private static void PlaySE(AudioClip se)
        {
            _as.PlayOneShot(se, _seVolume);
        }
        
        // 混合
        private static void PlayBgm(AudioClip bgm, float mix, bool isLoop, Action onPlayFinished)
        {
            if (bgm != _as.clip)
            {
                Addressables.Release(_as.clip);
            }

            if (mix > 0)
            {
                // TODO 混合音频
            }
            
            _as.clip = bgm;
            _as.loop = isLoop;
            _as.volume = _bgmVolume;
            _as.Play();
            
            UniTask.Create(async () =>
            {
                await UniTask.Delay((int)bgm.length * 1000 + 200);
                onPlayFinished?.Invoke();
            });
        }


        private static AudioClip SearchAudio(string key, bool isCache)
        {
            if (_audioClipsCache.TryGetValue(key, out var ret))
            {
                return ret;
            }

            ret = Addressables.LoadAssetAsync<AudioClip>(key).WaitForCompletion();

            if (isCache)
            {
                _audioClipsCache.Add(key, ret);
            }
            
            return ret;
        }
    }
}