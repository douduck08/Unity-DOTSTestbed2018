using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DynamicBone : MonoBehaviour {

    public Transform root;

    Quaternion m_RootInvertRotation;
    Vector3 objectMove = Vector3.zero;
    Vector3 objectPrevPosition = Vector3.zero;
    List<DynamicBoneParticle> particles = new List<DynamicBoneParticle> ();

    public class DynamicBoneParticle {
        public int parentIndex = -1;
        public Transform bone = null;
        public Vector3 initLocalPosition = Vector3.zero;
        public Quaternion initLocalRotation = Quaternion.identity;

        public Vector3 position = Vector3.zero;
        public Vector3 prevPosition = Vector3.zero;
        public Vector3 positionDiff = Vector3.zero;

        public Quaternion invertRotation = Quaternion.identity;
        public Quaternion localRotation = Quaternion.identity;
        public Vector3 localPosition = Vector3.zero;

        public float restLength;
        public float inert = 0;
        public float damping = 0;
        public float elasticity = 0;
    }

    // monobehaviour flow
    void Start () {
        SetupParticles ();
    }

    void Update () {
        PreUpdateBones ();
    }

    void LateUpdate () {
        UpdateBones ();
    }

    // internal method
    void SetupParticles () {
        m_RootInvertRotation = Quaternion.Inverse (root.rotation);
        objectPrevPosition = root.position;
        objectMove = Vector3.zero;

        particles = new List<DynamicBoneParticle> ();
        AppendParticles (particles, root, -1);
    }

    void AppendParticles (List<DynamicBoneParticle> particles, Transform bone, int parentIndex) {
        var particle = new DynamicBoneParticle ();
        particle.parentIndex = parentIndex;
        particle.bone = bone;

        particle.position = particle.prevPosition = bone.position;
        particle.initLocalPosition = bone.localPosition;
        particle.initLocalRotation = bone.localRotation;
        particle.localRotation = particle.initLocalRotation;

        if (parentIndex >= 0) {
            particle.positionDiff = particle.position - particles[parentIndex].position;
            particle.restLength = (particle.positionDiff).magnitude;
        }
        particle.inert = 0.5f;
        particle.damping = 0.2f;
        particle.elasticity = 0.05f;

        int index = particles.Count;
        particles.Add (particle);

        for (int i = 0; i < bone.childCount; i++) {
            var childBone = bone.GetChild (i);
            AppendParticles (particles, childBone, index);
        }
    }

    void PreUpdateBones () {
        InitTransforms ();
    }

    void InitTransforms () {
        for (int i = 0; i < particles.Count; i++) {
            var particle = particles[i];
            particle.bone.localPosition = particle.initLocalPosition;
            particle.bone.localRotation = particle.initLocalRotation;
        }
    }

    void UpdateBones () {
        UpdateDynamicBones (Time.smoothDeltaTime);
    }

    void UpdateDynamicBones (float t) {
        var currentObjectPos = root.position;
        objectMove = currentObjectPos - objectPrevPosition;
        objectPrevPosition = currentObjectPos;

        UpdateParticles ();
        ApplyParticlesToTransforms ();
    }

    void UpdateParticles () {
        // ignore force
        for (int i = 0; i < particles.Count; i++) {
            var particle = particles[i];
            if (particle.parentIndex >= 0) {
                // verlet integration
                Vector3 posDiff = particle.position - particle.prevPosition;
                Vector3 moveInert = objectMove * particle.inert;
                particle.prevPosition = particle.position + moveInert;
                particle.position += posDiff * (1 - particle.damping) + moveInert;
            } else {
                particle.prevPosition = particle.position;
                particle.position = particle.bone.position;
            }
        }

        var deltaRot = root.rotation * m_RootInvertRotation;
        for (int i = 1; i < particles.Count; i++) {
            var particle = particles[i];
            var parentParticle = particles[particle.parentIndex];

            // keep shape
            if (particle.elasticity > 0) {
                Vector3 restPos = parentParticle.position + deltaRot * particle.positionDiff;
                Vector3 invertDiff = restPos - particle.position;
                particle.position += invertDiff * particle.elasticity;
            }

            // keep length
            Vector3 posDiff = parentParticle.position - particle.position;
            float leng = posDiff.magnitude;
            if (leng > 0) {
                particle.position += posDiff * ((leng - particle.restLength) / leng);
            }
        }
    }

    void ApplyParticlesToTransforms () {
        particles[0].invertRotation = Quaternion.Inverse (particles[0].bone.rotation);
        for (int i = 1; i < particles.Count; i++) {
            var particle = particles[i];
            var parentParticle = particles[particle.parentIndex];

            // do not modify bone orientation if has more then one child
            if (parentParticle.bone.childCount <= 1) {
                parentParticle.localPosition = parentParticle.invertRotation * (particle.position - parentParticle.position);
                var rot = Quaternion.FromToRotation (particle.initLocalPosition, parentParticle.localPosition);
                parentParticle.localRotation = rot * parentParticle.initLocalRotation;
                // particle.invertRotation = Quaternion.Inverse (particle.initLocalRotation) * Quaternion.Inverse (parentParticle.localRotation) * parentParticle.initLocalRotation * parentParticle.invertRotation;
                particle.invertRotation = Quaternion.Inverse (parentParticle.localRotation * particle.initLocalRotation) * parentParticle.initLocalRotation * parentParticle.invertRotation;
            } else {
                particle.invertRotation = Quaternion.Inverse (particle.initLocalRotation) * parentParticle.invertRotation;
            }
        }

        particles[0].bone.localRotation = particles[0].localRotation; // do not modify position of root particle
        for (int i = 1; i < particles.Count; i++) {
            var particle = particles[i];
            particle.bone.localRotation = particle.localRotation;
            particle.bone.localPosition = particle.localPosition;
        }
    }
}