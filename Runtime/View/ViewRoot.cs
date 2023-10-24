using System;
using UnityEngine;

namespace Twenty2.VomitLib.View
{
    public class ViewRoot : MonoBehaviour
    {
        [SerializeField] private Camera _viewCamera;

        [SerializeField] private Transform _hiddenCanvas;

        public Camera ViewCamera => _viewCamera;

        public Transform HiddenCanvas => _hiddenCanvas;

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
        }
    }
}