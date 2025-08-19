// using Il2CppDMM;
using Il2CppInterop.Runtime;
using MelonLoader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace Mod.Cheats
{
    [RegisterTypeInIl2Cpp]
    internal class MinimapIconManager : MonoBehaviour
    {
        public MinimapIconManager(IntPtr ptr) : base(ptr) { }
        //private GameObject? miniMapIconContainer;

        private void Start()
        {
            //MelonLogger.Msg("MapTileManager Start method called.");
            //miniMapIconContainer = new GameObject("miniMapIconContainer", Il2CppType.Of<DMMapIcon>());
            //miniMapIconContainer.transform.SetParent(GameObject.Find("MainPlayer(Clone)").transform);
            //SetMapContainerComponents();
            //if (miniMapIconContainer != null)
            //{
            //    //LoadTiles();
            //    //ArrangeTiles();
            //}
        }
        private void SetMapContainerComponents()
        {
            //if (miniMapIconContainer == null)
            //{
            //    MelonLogger.Msg("miniMapIconContainer is null.");
            //    return;
            //}
            //RectTransform rectTransform = miniMapIconContainer.GetComponent<RectTransform>();
            //if (rectTransform != null)
            //{
            //    rectTransform.anchoredPosition = new Vector2(62, 155); // local position is not true monitor size position
            //    //rectTransform.sizeDelta = new Vector2(tileSize / 50, tileSize / 50);

            //    RawImage rawImage = miniMapIconContainer.GetComponent<RawImage>();
            //    rawImage.color = new Color(1, 1, 1, 0); // Transparent background

            //    // Adjust the sorting order to make it 1 level lower
            //    //Canvas canvas = miniMapIconContainer.AddComponent<Canvas>();
            //    //canvas.overrideSorting = true;
            //}
        }
    }
}
