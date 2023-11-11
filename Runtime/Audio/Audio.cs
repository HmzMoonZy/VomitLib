namespace Twenty2.VomitLib.Audio
{
    using System;
    using Cysharp.Threading.Tasks;
    using QFramework;
    using UnityEngine;
    using Object = UnityEngine.Object;


    public static class Audio
    {
        /// <summary>
        /// 当前播放的背景音乐
        /// </summary>
        public static AudioClip CurrentBgm => _as.clip;

        private static float _bgmVolumeFactors;
        private static float _seVolumeFactors;

        private static float _realBgmVolume;
        private static float _realSEVolume;

        private static AudioSource _as;

        private static Func<string, AudioClip> _onSearchAudioClip;

        public static void Init(Func<string, AudioClip> onSearchAudioClip = null, float bgmFactors = 0.8f,
            float bgmVolume = 1f, float seFactors = 1f, float seVolume = 1f)
        {
            var audioSource = new GameObject("__AudioSource__", typeof(AudioSource));
            _as = audioSource.GetComponent<AudioSource>();
            Object.DontDestroyOnLoad(_as.gameObject);

            _bgmVolumeFactors = bgmFactors;
            _seVolumeFactors = seFactors;

            SetSEVolume(seVolume);
            SetBgmVolume(bgmVolume);

            _onSearchAudioClip = onSearchAudioClip;
        }

        public static void SetSEVolume(float v)
        {
            _realSEVolume = v * _seVolumeFactors;
            LogKit.I($"Set SE Volume : {v} real volume : {_realSEVolume}");
        }

        public static void SetBgmVolume(float v)
        {
            _realBgmVolume = v * _bgmVolumeFactors;
            LogKit.I($"Set Music Volume : {v},real volume : {_realBgmVolume}");
        }

        public static void PlayBgm(AudioClip bgm, bool isLoop = true)
        {
            _as.clip = bgm;
            _as.loop = isLoop;
            _as.Play();
        }

        public static void PlayBgm(string bgm, bool isLoop = true)
        {
            PlayBgm(_onSearchAudioClip.Invoke(bgm), isLoop);
        }

        public static void PlayBgm(AudioClip bgm, Action onPlayFinished)
        {
            PlayBgm(bgm, false);

            UniTask.Create(async () =>
            {
                await UniTask.Delay((int) bgm.length * 1000 + 200);
                onPlayFinished?.Invoke();
            });
        }

        public static void PlayBgm(string bgm, Action onPlayFinished)
        {
            PlayBgm(_onSearchAudioClip.Invoke(bgm), onPlayFinished);
        }

        public static void PlaySE(AudioClip se)
        {
            _as.PlayOneShot(se, _realSEVolume);
        }

        public static void PlaySE(string se)
        {
            PlaySE(_onSearchAudioClip.Invoke(se));
        }
    }
}