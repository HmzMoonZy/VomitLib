using System.Collections.Generic;
using QFramework;
using System;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Twenty2.VomitLib.Config;
using UnityEngine;
using UnityEngine.AddressableAssets;
using Object = UnityEngine.Object;

namespace Twenty2.VomitLib.Audio
{
    public static class Audio
    {
        private static Dictionary<int, AudioSource> _audioSources = new();
        
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
        public static void Mute(int index, out float originVolume)
        {
            originVolume = _audioSources[index].volume;
            _audioSources[index].volume = 0;
        }
        
        public static void SetBgmVolume(float v, int index = 0)
        {
            _audioSources[index].volume = v;
        }

        /// <summary>
        /// 播放BGM, 任务被取消后并不会停止BGM的播放
        /// </summary>
        public static UniTask PlayBgm(string bgm, float mix, bool isLoop, int index = 0, CancellationToken cancellationToken = default)
        {
            return PlayBgm(SearchAudio(bgm, false), mix, isLoop, index, cancellationToken);
        }
        
        /// <summary>
        /// 播放BGM, 任务被取消后并不会停止BGM的播放
        /// </summary>
        private static UniTask PlayBgm(AudioClip bgm, float mix, bool isLoop, int index = 0, CancellationToken cancellationToken = default)
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

            return UniTask.WaitForSeconds(bgm.length, true, cancellationToken: cancellationToken);
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
            if (!_audioSources.ContainsKey(index))
            {
                return;
            }

            var originClip = _audioSources[index].clip;
            Object.Destroy(_audioSources[index].gameObject);
            _audioSources.Remove(index);
            if (_audioSources.Values.All(audioSource => audioSource.clip != originClip))
            {
                Addressables.Release(originClip);
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