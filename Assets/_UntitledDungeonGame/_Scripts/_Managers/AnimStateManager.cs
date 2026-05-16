using System.Collections.Generic;
using UnityEngine;
using MEC;

namespace UntitledDungeonGame
{
    public static class AnimStateManager
    {
        // tracks last played state per animator instance
        private static readonly Dictionary<int, int> _lastStatePerAnimator = new();

        public static void ChangeAnimationState(Animator animator, AnimationClip animClip, float? desiredDuration = null)
        {
            if (animator == null || animClip == null) return;

            Timing.RunCoroutine(ChangeState(animator, animClip, desiredDuration).CancelWith(animator.gameObject));
        }

        private static IEnumerator<float> ChangeState(Animator animator, AnimationClip animClip, float? desiredDuration)
        {
            // optional: gives the animator a frame to settle; can experiment removing this if unnecessary
            yield return Timing.WaitForOneFrame;

            int animHash = Animator.StringToHash(animClip.name);

            if (IsAlreadyPlaying(animator, animHash))
                yield break; // avoid redundant restarts

            // adjust playback speed if requested
            if (desiredDuration.HasValue && animClip.length > 0f)
            {
                animator.speed = animClip.length / desiredDuration.Value;
            }
            else
            {
                animator.speed = 1f;
            }

            PlayHashAnimation(animator, animHash);
            yield return Timing.WaitForOneFrame; // let it settle
            var stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            if (stateInfo.shortNameHash != animHash)
            {
                // fallback if the requested state didn’t stick
                ForceResetAndPlay(animator, animClip);
            }
        }

        private static bool IsAlreadyPlaying(Animator animator, int hash)
        {
            return _lastStatePerAnimator.TryGetValue(animator.GetInstanceID(), out var last) && last == hash;
        }

        private static void SetLastState(Animator animator, int hash)
        {
            _lastStatePerAnimator[animator.GetInstanceID()] = hash;
        }

        private static void PlayHashAnimation(Animator animator, int newHashState)
        {
            if (animator == null) return;
            if (!animator.gameObject.activeInHierarchy) return;

            animator.Play(newHashState, 0, 0f); // force from start on base layer
            SetLastState(animator, newHashState);
        }

        // fallback if you detect the animator is stuck (e.g., via telemetry mismatch)
        public static void ForceResetAndPlay(Animator animator, AnimationClip animClip)
        {
            if (animator == null || animClip == null) return;

            int hash = Animator.StringToHash(animClip.name);
            animator.Rebind(); // reset internal bindings/state
            animator.Update(0); // flush immediately
            animator.Play(hash, 0, 0f);
            SetLastState(animator, hash);
        }
    }
}