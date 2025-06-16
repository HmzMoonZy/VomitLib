using System;
using System.Collections.Generic;
using UnityEngine;

namespace Twenty2.VomitLib.View
{
    public class SpriteRegister : MonoBehaviour
    {
        [Serializable]
        public class RegisterData
        {
            public string m_name;
            public Sprite m_object;
        }

        [SerializeField] private RegisterData[] m_datas = null;
        
        private Dictionary<string, Sprite> m_dic = null;



        [ContextMenu("BindNames")]
        private void LoadGameObject()
        {
            if (m_dic != null) return;
            m_dic = new Dictionary<string, Sprite>(m_datas.Length);

            for (int i = 0; i < m_datas.Length; i++)
            {
                var data = m_datas[i];
                if (string.IsNullOrEmpty(data.m_name) && data.m_object != null)
                {
                    data.m_name = data.m_object.name;
                }
                m_dic[data.m_name] = data.m_object;
            }
        }

        /// <summary>
        /// 获得GameObject
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public Sprite GetSprite(string name)
        {
            if (m_dic == null)
            {
                LoadGameObject();
            }
        
            m_dic.TryGetValue(name, out var obj);
            return obj;
        }
        public Dictionary<string, Sprite> GetDic()
        {
            if (m_dic == null) {
                LoadGameObject();
            }
            return m_dic;
        }
        
        
#if UNITY_EDITOR
        [ContextMenu("重置Sprite名字")]
        private void ResetSpriteNames()
        {
            for (int i = 0; i < m_datas.Length; i++)
            {
                var data = m_datas[i];
                if (data.m_object != null)
                {
                    data.m_name = data.m_object.name;
                }
            }
        }
#endif
    }
}