#nullable enable

namespace Yarn.Unity.Samples
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using UnityEngine;
    using UnityEngine.Events;
    using Yarn.Unity;

    public class SimplePathMovement : MonoBehaviour
    {
        public SimplePath? path;
        [SerializeField] float turnSpeed = 300f;
        [SerializeField] float moveSpeed = 300f;
        [SerializeField] Animator? animator = null;
        [SerializeField] string speedParameter = "Speed";

        public CancellationTokenSource? walkCTS = null;


        [YarnCommand("pause_walking")]
        public void PauseWalking()
        {
            StopWalking();
        }

        [YarnCommand("unpause_walking")]
        public void UnpauseWalking()
        {
            StartWalking();
        }

        public void Awake()
        {
            if (this.path == null)
            {
                return;
            }

            if (this.path.pathElements.Count > 0)
            {
                this.transform.position = this.path.transform.TransformPoint(this.path.pathElements[0].position);
            }

            if (this.path.pathElements.Count > 1)
            {
                var pos0 = this.path.transform.TransformPoint(this.path.pathElements[0].position);
                var pos1 = this.path.transform.TransformPoint(this.path.pathElements[1].position);

                var rotation = Quaternion.LookRotation(pos1 - pos0);
                this.transform.rotation = rotation;
            }
        }


        public void StartWalking()
        {
            if (this.path == null)
            {
                return;
            }

            walkCTS?.Cancel();
            walkCTS?.Dispose();
            walkCTS = CancellationTokenSource.CreateLinkedTokenSource(this.destroyCancellationToken);
            WalkPath(this.path, walkCTS.Token).SuppressCancellationThrow();
        }

        public void StopWalking()
        {
            walkCTS?.Cancel();
            walkCTS?.Dispose();
            walkCTS = null;

        }

        public void OnEnable()
        {
            StartWalking();
        }

        public void OnDisable()
        {
            StopWalking();
        }

        private int currentPathSegment = 0;

        private async YarnTask WalkPath(SimplePath path, CancellationToken cancellationToken)
        {
            while (true)
            {
                if (path.pathElements.Count == 0)
                {
                    return;
                }

                currentPathSegment %= path.pathElements.Count;
                var pos = path.transform.TransformPoint(path.pathElements[currentPathSegment].position);
                await MoveToPoint(pos, cancellationToken);

                if (path.pathElements[currentPathSegment].delay > 0)
                {
                    await YarnTask.Delay(TimeSpan.FromSeconds(path.pathElements[currentPathSegment].delay), cancellationToken);
                }

                currentPathSegment += 1;
            }
        }

        private async YarnTask MoveToPoint(Vector3 position, CancellationToken cancellationToken)
        {
            var targetPosition = position;

            if (Vector3.Distance(targetPosition, this.transform.position) > 0.001)
            {

                var desiredOrientation = Quaternion.LookRotation(targetPosition - this.transform.position);

                do
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        return;
                    }

                    transform.rotation = Quaternion.RotateTowards(transform.rotation, desiredOrientation, this.turnSpeed * Time.deltaTime);
                    await YarnTask.Yield();

                } while (Quaternion.Angle(desiredOrientation, transform.rotation) > 1);

                transform.rotation = desiredOrientation;
            }

            if (animator != null)
            {
                animator.SetFloat(speedParameter, 1f);
            }

            do
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    if (animator != null)
                    {
                        animator.SetFloat(speedParameter, 0f);
                    }
                    return;

                }


                transform.position = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);

                await YarnTask.Yield();

            } while (Vector3.Distance(transform.position, targetPosition) > 0.05f);


            if (animator != null)
            {
                animator.SetFloat(speedParameter, 0f);
            }


        }
    }
}