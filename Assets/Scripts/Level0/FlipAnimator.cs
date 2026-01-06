using UnityEngine;

namespace Xeon.Performance.Level0
{
    public class FlipAnimator : MonoBehaviour
    {
        [SerializeField]
        private Sprite[] sprites;
        [SerializeField]
        private float frameDuration = 0.1f;

        [SerializeField]
        private SpriteRenderer spriteRenderer;

        private int currentFrame = 0;
        private float elapsed = 0f;

        private void Update()
        {
            elapsed += Time.deltaTime;
            if (elapsed >= frameDuration)
            {
                currentFrame = (currentFrame + 1) % sprites.Length;
                spriteRenderer.sprite = sprites[currentFrame];
                elapsed = 0f;
            }
        }
    }
}
