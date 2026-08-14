using System;
using System.Collections.Generic;
using MoreMountains.Feedbacks;
using Unity.Netcode;
using UnityEngine;


namespace DeepSeaGame
{
    public class ClientCharacterFeedbacks : NetworkBehaviour
    {
        [SerializeField]
        private ServerCharacter _serverCharacter;

        [SerializeField]
        private ParticleSystem _damagedParticles, _deathParticles;
        
        [SerializeField] private Color _damageColor;

        // [SerializeField]
        // private List<Gibfab> _gibfabs;

        private MMF_Player _damageFeedback;
        private MMF_Player _deathFeedback;

        private void Awake()
        {
            // _damageFeedback = transform.GetChild(0).GetComponent<MMF_Player>();
            // _deathFeedback = transform.GetChild(1).GetComponent<MMF_Player>();
        }

        [Rpc(SendTo.ClientsAndHost, InvokePermission = RpcInvokePermission.Everyone)]
        public void PlayDamageNumbersRpc(int damage)
        {
            GameManager.Instance.SpawnWorldText(damage.ToString(), transform.position, _damageColor);
        }

        [Rpc(SendTo.ClientsAndHost, InvokePermission = RpcInvokePermission.Everyone)]
        public void PlayDamageFeedbacksRpc(Vector2 hitDirection)
        {
            AudioManager.Instance.PlayOneShot(_serverCharacter.CharacterData.DamageSFX, transform.position);
            RotateFeedbacks(hitDirection);
            // _damageFeedback.PlayFeedbacks();
        }

        public void PlayDeathFeedbacks(Vector3 payload)
        {
            Vector2 hitDirection = new(payload.x, payload.y);
            float knockbackForce = payload.z;

            AudioManager.Instance.PlayOneShot(_serverCharacter.CharacterData.DeathSFX, transform.position);
            // SoundManager.Instance.PlayOneShot(FMODEvents.Instance.MobSquash, transform.position);
            RotateFeedbacks(hitDirection);
            // _deathFeedback.PlayFeedbacks();

            // foreach (Gibfab gibfab in _gibfabs)
            // {
            //     float spread = 45f;

            //     float minInitialUpSpeed = 0.075f;
            //     float maxInitialUpSpeed = 0.225f;

            //     float minVelocityMag = 1f;
            //     float maxVelocityMag = 10f;

            //     float minAddVelocity = 0;
            //     float maxAddVelocity = 10;
            //     float addedVelocity = Mathf.Lerp(minAddVelocity, maxAddVelocity, knockbackForce / 100);

            //     float randomVelocityMagnitude = UnityEngine.Random.Range(minVelocityMag, maxVelocityMag) + addedVelocity;
            //     float t = Mathf.InverseLerp(minVelocityMag + addedVelocity, maxVelocityMag + addedVelocity, randomVelocityMagnitude);
            //     float initialUpwardSpeed = Mathf.Lerp(minInitialUpSpeed, maxInitialUpSpeed, t);

            //     float baseAngle = Mathf.Atan2(hitDirection.y, hitDirection.x);
            //     float offsetAngle = UnityEngine.Random.Range(-spread, spread) * Mathf.Deg2Rad;
            //     float angle = baseAngle + offsetAngle;
            //     Vector2 randomizedDirection = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));

            //     gibfab.LaunchGib(initialUpwardSpeed, 0, randomizedDirection * randomVelocityMagnitude);
            // }
        }

        private void RotateFeedbacks(Vector2 hitDirection)
        {
            if (_deathParticles == null || _damagedParticles == null)
            {
                Debug.LogWarning($"Either Death or Damaged Particles are null therefore can't be rotated");
            }

            if (!float.IsFinite(hitDirection.x) || !float.IsFinite(hitDirection.y)) return;

            if (hitDirection == Vector2.zero)
                hitDirection = Vector2.up; // Default direction if none provided

            float angle = Mathf.Atan2(hitDirection.y, hitDirection.x) * Mathf.Rad2Deg;
            // Debug.Log($"RotateGibs: hitDir={hitDirection}, angle={angle}");
            // _damagedParticles.transform.rotation = Quaternion.Euler(0f, 0f, angle);
            // _deathParticles.transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }
    }

}