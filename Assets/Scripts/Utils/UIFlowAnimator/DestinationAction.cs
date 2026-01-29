using System;
using System.Collections.Generic;
using UnityEngine;
using Utils.Pools;
using Random = UnityEngine.Random;

namespace Utils.ObjectFlowAnimator
{
    public struct DestinationActionProperties
    {
        public Vector3 startPos;
        public Vector3 endPos;

        public FlowParticle prefab;
        public Sprite sprite;
        public RectTransform parent;

        public int particleCount;

        public DestinationActionData destinationActionData;

        public Action onSpawn;
        public Action onReceivedItem;
        public Action onCompleted;
    }

    public class ParticleData
    {
        public FlowParticle particle;
        public float xa;
        public float ya;
        public float moveSpeedFactor;
        public bool reached;
    }
    public class DestinationAction
    {
        private int numberOfReachedParticles = 0;
        private int numberOfDiedParticles = 0;
        private int numberOfSpawnedParticles = 0;

        private float animationCounter = 0;

        private List<ParticleData> spawnedObjects = new List<ParticleData>();

        private float rotation;
        private float particleHeight = -1;

        private Vector3 temp;

        DestinationActionProperties actionProperties;

        public DestinationAction(DestinationActionProperties properties)
        {
            actionProperties = properties;
        }

        public void Tick()
        {
            if (IsDone()) return;

            float dt = Time.fixedDeltaTime;

            animationCounter += dt;

            if (animationCounter >= .01f * actionProperties.destinationActionData.spawnDelayFactor && numberOfSpawnedParticles < actionProperties.particleCount)
            {
                animationCounter = 0.0f;

                FlowParticle particle = Pools.Pools.Instance.Spawn(actionProperties.prefab);

                if (actionProperties.sprite != null)
                {
                    particle.Image.sprite = actionProperties.sprite;
                }

                particle.transform.SetParent(actionProperties.parent);

                particle.transform.localScale = actionProperties.destinationActionData.scale;

                particle.transform.localScale *= 0.0f;

                particle.transform.position = actionProperties.startPos;
                
                if (particleHeight < 0)
                    particleHeight = particle.Image.rectTransform.rect.height;

                ParticleData data = new ParticleData()
                {
                    particle = particle,
                    xa = particleHeight * .125f * Random.Range(actionProperties.destinationActionData.speedRandX.x, actionProperties.destinationActionData.speedRandX.y),
                    ya = particleHeight * .125f * Random.Range(actionProperties.destinationActionData.speedRandY.x, actionProperties.destinationActionData.speedRandY.y),
                };

                spawnedObjects.Add(data);

                numberOfSpawnedParticles++;

                if (actionProperties.onSpawn != null) actionProperties.onSpawn();
            }

            for (int i = 0; i < spawnedObjects.Count; i++)
            {
                if (spawnedObjects[i].moveSpeedFactor < 1.0f)
                {
                    spawnedObjects[i].moveSpeedFactor += .01f * dt * 60.0f;

                    if (spawnedObjects[i].moveSpeedFactor > 1.0f)
                    {
                        spawnedObjects[i].moveSpeedFactor = 1.0f;
                    }
                }

                if (!spawnedObjects[i].reached || actionProperties.destinationActionData.bounceBack)
                {
                    temp.Set(spawnedObjects[i].xa, spawnedObjects[i].ya, 0);

                    spawnedObjects[i].particle.transform.position = spawnedObjects[i].particle.transform.position + (temp * dt * 60);

                    spawnedObjects[i].particle.transform.rotation = Quaternion.Euler(0,0,
                        spawnedObjects[i].particle.transform.rotation.eulerAngles.z + (100.0f * spawnedObjects[i].xa * dt * 60 / particleHeight));

                    spawnedObjects[i].xa *= .975f * Mathf.Pow(.99f, dt * 60);
                    spawnedObjects[i].ya *= .95f * Mathf.Pow(.99f, dt * 60);


                    if (actionProperties.destinationActionData.bounceBack && spawnedObjects[i].reached)
                    {
                        spawnedObjects[i].particle.transform.localScale = spawnedObjects[i].particle.transform.localScale * .935f;

                        if (spawnedObjects[i].particle.transform.localScale.x < 0.025f)
                        {
                            numberOfDiedParticles++;

                            Pools.Pools.Instance.Despawn(spawnedObjects[i].particle);
                            spawnedObjects.RemoveAt(i);
                            i--;
                        }

                    }
                    else
                    {
                        spawnedObjects[i].particle.transform.localScale = spawnedObjects[i].particle.transform.localScale +
                                                              (actionProperties.destinationActionData.scale - spawnedObjects[i].particle.transform.localScale) * .2f;

                        rotation = Mathf.Atan2(actionProperties.endPos.y - spawnedObjects[i].particle.transform.position.y,
                                          actionProperties.endPos.x - spawnedObjects[i].particle.transform.position.x);

                        temp.Set(Mathf.Cos(rotation), Mathf.Sin(rotation), 0);

                        spawnedObjects[i].particle.transform.position = spawnedObjects[i].particle.transform.position +
                                                                 (spawnedObjects[i].moveSpeedFactor * actionProperties.destinationActionData.speed * particleHeight * dt * 30.0f *
                                                                 temp);

                        if (Vector3.Distance(spawnedObjects[i].particle.transform.position, actionProperties.endPos) < particleHeight * .5f)
                        {
                            spawnedObjects[i].reached = true;

                            if (actionProperties.destinationActionData.bounceBack)
                            {
                                spawnedObjects[i].ya = particleHeight * Random.Range(.05f, .1f);
                                spawnedObjects[i].xa = particleHeight * Random.Range(0.0f, .125f);
                            }

                            numberOfReachedParticles++;

                            if (actionProperties.onReceivedItem != null) actionProperties.onReceivedItem();

                            if (numberOfReachedParticles >= actionProperties.particleCount)
                            {
                                if (actionProperties.onCompleted != null) actionProperties.onCompleted();
                            }
                        }
                    }

                }
                else
                {
                    spawnedObjects[i].particle.transform.localScale = (spawnedObjects[i].particle.transform.localScale * .8f);

                    spawnedObjects[i].particle.transform.position = (spawnedObjects[i].particle.transform.position + (actionProperties.endPos - spawnedObjects[i].particle.transform.position) * .5f);

                    if (spawnedObjects[i].particle.transform.localScale.x < 0.025f)
                    {
                        numberOfDiedParticles++;

                        Pools.Pools.Instance.Despawn(spawnedObjects[i].particle);
                        spawnedObjects.RemoveAt(i);
                        i--;
                    }

                }
            }
        }

        public bool IsDone()
        {
            return numberOfDiedParticles >= actionProperties.particleCount;
        }

    }
}