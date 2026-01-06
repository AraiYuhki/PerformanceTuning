using UnityEngine;

namespace Xeon.Performance.Common
{
    public class Item : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer image;

        private static readonly int SpriteRectId = Shader.PropertyToID("_SpriteRect");
        private MaterialPropertyBlock materialPropertyBlock;

        public bool IsReleased { get; set; }
        public int Id { get; set; }

        public Color Color
        {
            get => image.color;
            set => image.color = value;
        }

        private void Awake()
        {
            materialPropertyBlock = new MaterialPropertyBlock();
        }

        /// <summary>
        /// _SpriteRect を設定（xy = scale, zw = offset）
        /// </summary>
        public void SetSpriteRect(Vector4 rect)
        {
            image.GetPropertyBlock(materialPropertyBlock);
            materialPropertyBlock.SetVector(SpriteRectId, rect);
            image.SetPropertyBlock(materialPropertyBlock);
        }

        /// <summary>
        /// スプライトシートのフレームインデックスから _SpriteRect を設定
        /// </summary>
        public void SetSpriteRect(int columns, int rows, int colorIndex, int frameIndex)
        {
            float frameWidth = 1f / columns;
            float frameHeight = 1f / rows;
            int col = frameIndex % columns;

            // rect: xy = scale (tiling), zw = offset
            var rect = new Vector4(
                1f,
                1f,
                col * frameWidth,
                (float)frameHeight * colorIndex
            );
            SetSpriteRect(rect);
        }
    }
}
