// Copyright © 2021 Pokeyi - https://pokeyi.dev - pokeyi@pm.me - This work is licensed under the MIT License.

// using System;
using UdonSharp;
using UnityEngine;
// using UnityEngine.UI;
using VRC.SDKBase;
using VRC.SDK3.Components;
// using VRC.Udon.Common.Interfaces;

namespace Pokeyi.UdonSharp
{
    [AddComponentMenu("Pokeyi.VRChat/P.VRC Object Spawner")]
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)] // No variables are serialized over network.
    [RequireComponent(typeof(VRCObjectPool))] // Make sure object has an object-pool component.

    public class P_ObjectSpawner : UdonSharpBehaviour
    {   // Object spawner & controller for VRChat:
        [Header(":: VRC Object Spawner by Pokeyi ::")]

        [Header("[X] -Left/+Right   [Y] -Down/+Up   [Z] -Back/+Fwd")]
        [Space]
        [Tooltip("Local velocity each spawned object will move while active.")]
        [SerializeField] private Vector3 spawnVelocity;
        [Tooltip("Whether spawned object faces its velocity or keeps original rotation.")]
        [SerializeField] private bool faceVelocity;
        [Tooltip("Object pool is shuffled after each spawn.")]
        [SerializeField] private bool shufflePool;
        [Tooltip("Max distance from source before target is returned.")]
        [SerializeField] private float returnDistance = 0F;
        [Tooltip("Max time per spawn before target is returned.")]
        [SerializeField] private float returnTime = 1F;

        private VRCObjectPool objectPool; // Reference to object pool component.
        private float[] spawnTime; // Spawned objects' spawn times.
        private bool lastAllInactive = false; // Last all-inactive state to avoid repeat.

        public void Start()
        {   // Assign reference to object pool, populate spawn time array:
            objectPool = (VRCObjectPool)GetComponent(typeof(VRCObjectPool));
            if (objectPool != null) spawnTime = new float[objectPool.Pool.Length];
        }

        public void Update()
        {
            if (objectPool == null) return;
            bool allInactive = true;
            for (int i = 0; i < objectPool.Pool.Length; i++)
            {   // Apply per-frame positioning and check despawn conditions for all actively spawned objects:
                GameObject targetObject = objectPool.Pool[i].gameObject;
                if ((targetObject != null) && (targetObject.activeSelf))
                {   
                    allInactive = false;
                    float deltaTime = Time.deltaTime;
                    Vector3 velocity = (transform.right * spawnVelocity.x) + (transform.up * spawnVelocity.y) + (transform.forward * spawnVelocity.z);
                    targetObject.transform.position += deltaTime * velocity;
                    if (faceVelocity) targetObject.transform.localRotation = Quaternion.LookRotation(spawnVelocity);
                    float distance = Vector3.Distance(targetObject.transform.position, transform.position);
                    spawnTime[i] += deltaTime; // Despawn if conditions are met:
                    if (((returnDistance > 0) && (distance >= returnDistance)) || ((returnTime > 0) && (spawnTime[i] >= returnTime))) Despawn(targetObject);
                } // Reset timer for inactive objects:
                else spawnTime[i] = 0F;
            } // Check for shuffle functionality if no objects are currently active:
            if ((!shufflePool) || (allInactive == lastAllInactive)) return;
            if ((allInactive) && (Networking.IsOwner(gameObject))) objectPool.Shuffle();
            lastAllInactive = allInactive;
        }

        private void Despawn(GameObject despawnObject)
        {   // Return object to pool:
            if ((objectPool == null) || (despawnObject == null)) return;
            if (Networking.IsOwner(gameObject)) objectPool.Return(despawnObject);
        }

        public void _SpawnObject()
        {   // Set network ownership and spawn next object from pool:
            if (objectPool == null) return;
            if (!Networking.IsOwner(gameObject)) Networking.SetOwner(Networking.LocalPlayer, gameObject);
            objectPool.TryToSpawn();
        }

        public void _ResetObjects()
        {   // Set network ownership and return all objects to pool:
            if (objectPool == null) return;
            if (!Networking.IsOwner(gameObject)) Networking.SetOwner(Networking.LocalPlayer, gameObject);
            for (int i = 0; i < objectPool.Pool.Length; i++)
            {
                GameObject resetObject = objectPool.Pool[i].gameObject;
                if (resetObject != null) objectPool.Return(resetObject);
            }
        }
    }
}

/* MIT License

Copyright (c) 2021 Pokeyi - https://pokeyi.dev - pokeyi@pm.me

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE. */