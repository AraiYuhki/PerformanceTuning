using System;
using UnityEngine;

namespace Xeon.Performance.Level0
{
    public class Item : Common.Item
    {
        [SerializeField] private float pullDuration = 0.3f;
        
        private bool isPulling = false;
        private Player player;

        private float elapsed = 0f;
        private Vector3 startPosition = Vector3.zero;
        private Action onRelease;

        public void Setup(Player player, Action onRelease)
        {
            this.player = player;
            this.onRelease = onRelease;
        }

        private void OnEnable()
        {
            elapsed = 0f;
            isPulling = false;
            startPosition = transform.position;
        }

        private void Update()
        {
            if (!isPulling)
                return;
            elapsed += Time.deltaTime;
            var percent = elapsed / pullDuration;
            if (percent >= 1f)
            {
                onRelease?.Invoke();
                isPulling = false;
                elapsed = 0f;
                return;
            }

            transform.position = Vector3.Lerp(startPosition, player.transform.position, percent);
        }
        
        private void OnTriggerEnter2D(Collider2D collision)
        {
            isPulling = true;
            startPosition = transform.position;
        }
    }
}