using System;
using UnityEngine;

namespace Twenty2.VomitLib.Config
{
    public enum UnitySupportAudioFormat
    {
        mp3,
        
        ogg,
        
        wav,
        
        aiff,
        
        aif,
        
        mod,
        
        it,
        
        s3m,
        
        xm,
    }
    
    [Serializable]
    public class AudioConfig
    {
        [Tooltip("音效资源前缀")]
        public string PrefixSE;

        [Tooltip("音效资源格式")]
        public UnitySupportAudioFormat SEFormat;
        
        [Tooltip("音乐资源前缀")]
        public string PrefixBgm;
        
        [Tooltip("音乐资源格式")]
        public UnitySupportAudioFormat BgmFormat;
        
        public string AudioLabel;
    }
}