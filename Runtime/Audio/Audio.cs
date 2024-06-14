﻿using System.Collections.Generic;
using QFramework;
using System;
using System.Linq;
using Cysharp.Threading.Tasks;
using Twenty2.VomitLib.Config;
using UnityEngine;
using UnityEngine.AddressableAssets;
using Object = UnityEngine.Object;

namespace Twenty2.VomitLib.Audio
{
    public static class Audio
    {
        private static Dictionary<int, AudioSource> _audioSources;
        
        private static Dictionary<string, AudioClip> _audioClipsCache = new();

        private static AudioConfig _config;
        
        private static float _seVolume = 1f;

        #region Init

        public static void Init(bool preload, float bgmVolume = 1f, float seVolume = 1f)
        {
            _config = Vomit.RuntimeConfig.AudioConfig;
            
            GenerateAudioSource(0);
            
            SetSEVolume(seVolume);
            SetBgmVolume(bgmVolume);

            if (preload)
            {
                Preload();
            }
        }

        private static void Preload()
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

        #endregion

        #region SE

        public static void PlaySE(string se)
        {
            PlaySE(SearchAudio(se, true));
        }

        public static void SetSEVolume(float v, int index = 0)
        {
            _seVolume = v;
        }
        
        private static void PlaySE(AudioClip se)
        {
            _audioSources[0].PlayOneShot(se, _seVolume);
        }

        #endregion

        #region BGM

        /// <summary>
        /// 将 index 的音频播放器静音
        /// </summary>
        /// <param name="index"></param>
        /// <returns>原来的音量</returns>
        public static float Mute(int index)
        {
            float origin = _audioSources[index].volume;
            _audioSources[index].volume = 0;
            return origin;
        }
        
        public static void SetBgmVolume(float v, int index = 0)
        {
            _audioSources[index].volume = v;
        }

        public static UniTask PlayBgm(string bgm, bool isLoop)
        {
            return PlayBgm(SearchAudio(bgm, false), 0, isLoop);
        }
        
        // 混合
        private static UniTask PlayBgm(AudioClip bgm, float mix, bool isLoop, int index = 0)
        {
            var originClip = _audioSources[index].clip;
            
            if (mix > 0)
            {
                // TODO 混合音频
            }

            _audioSources[index].clip = bgm;
            _audioSources[index].loop = isLoop;
            _audioSources[index].Play();

            if (_audioSources.Values.All(audioSource => audioSource.clip != originClip))
            {
                Addressables.Release(originClip);
            }

            return UniTask.WaitForSeconds(bgm.length, true);
        }

        #endregion


        public static void GenerateAudioSource(int index)
        {
            if (_audioSources.ContainsKey(index))
            {
                Debug.LogError("重复创建音频播放器!");
                return;
            }
            
            _audioSources.Add(index, new GameObject($"__AudioSource__[{index}]").AddComponent<AudioSource>());
            Object.DontDestroyOnLoad(_audioSources[index].gameObject);
        }

        public static void DeleteAudioSource(int index)
        {
            if (_audioSources.ContainsKey(index))
            {
                Addressables.Release(_audioSources[index].clip);
                Object.Destroy(_audioSources[index].gameObject);
                _audioSources.Remove(index);
            }
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