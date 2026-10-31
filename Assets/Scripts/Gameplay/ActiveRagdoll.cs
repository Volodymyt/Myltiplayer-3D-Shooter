using System;
using System.Collections.Generic;
using UnityEngine;

namespace Gameplay
{
    public class ActiveRagdoll : MonoBehaviour
    {
        [Header("Skeletons")]
        [SerializeField] private Transform ragdollModel;
        [SerializeField] private Transform animatedModel;
        [SerializeField] private Animator animator;
        [SerializeField] private Rigidbody ragdollHips;
        [SerializeField] private bool hideAnimatedMesh = true;

        [Header("Joints (lower = more jelly)")]
        [SerializeField] private float jointSpring = 2000f;
        [SerializeField] private float jointDamper = 60f;
        [SerializeField] private float legSpringMultiplier = 2.5f;
        [SerializeField] private float armSpringMultiplier = 2.5f;
        [SerializeField] private float deadJointDamper = 1f;

        [Header("Joint Limits")]
        [SerializeField] private bool limitsWhileAlive = false;
        [SerializeField] private bool selfCollision = false;

        [Header("Balance")]
        [SerializeField] private float uprightStrength = 600f;
        [SerializeField] private float uprightDamping = 40f;
        [SerializeField] private float chestUprightWeight = 1f;
        [SerializeField] private float headUprightWeight = 0.5f;
        [Range(0f, 1f)]
        [SerializeField] private float torsoAnimationFollow = 0.3f;
        [Range(0f, 1f)]
        [SerializeField] private float throwTorsoFollow = 1f;
        [SerializeField] private float torsoFollowBlendSpeed = 6f;
        [SerializeField] private float headLift = 25f;
        [SerializeField] private float feetDown = 10f;
        [SerializeField] private float heightSpring = 120f;
        [SerializeField] private float heightDamping = 15f;
        [Range(0f, 1f)]
        [SerializeField] private float gravityCompensation = 0.3f;
        [SerializeField] private float standHeightOverride = 0f;

        [Header("Movement")]
        [SerializeField] private float groundAcceleration = 12f;
        [SerializeField] private float airAcceleration = 3f;
        [SerializeField] private float jumpVelocity = 5f;
        [SerializeField] private bool slipperyFeet = true;
        [Range(0f, 1f)]
        [SerializeField] private float feetFriction = 0.15f;
        [SerializeField] private float walkHipsLift = 0.06f;

        [Header("Ground")]
        [SerializeField] private LayerMask groundMask = ~0;
        [SerializeField] private float groundCheckRadius = 0.2f;
        [SerializeField] private float groundCheckExtra = 0.3f;

        [Header("Hits And Knockout")]
        [SerializeField] private float knockoutVelocity = 4f;
        [SerializeField] private float knockoutDuration = 1.5f;
        [SerializeField] private float hitKnockoutDuration = 1.2f;
        [Range(0f, 1f)]
        [SerializeField] private float knockoutMinStrength = 0.05f;

        [Header("Throw")]
        [SerializeField] private float throwArmBoost = 3f;
        [SerializeField] private float throwBoostHold = 0.4f;
        [SerializeField] private string throwChargingParameter = "IsCharging";
        [SerializeField] private string throwStateName = "Throw";
        [SerializeField] private int throwAnimatorLayer = 1;

        [Header("Pushing")]
        [SerializeField] private float pushHoldTime = 0.2f;
        [SerializeField] private float pushAcceleration = 15f;

        [Header("Own Bear Look")]
        [SerializeField] private bool ownerUsesRemoteLook = true;
        [SerializeField] private float visualSendRate = 30f;

        [Header("Camera / Root")]
        [SerializeField] private float rootSmoothTime = 0.06f;

        public static readonly List<ActiveRagdoll> All = new List<ActiveRagdoll>();

        public bool IsDead { get; private set; }
        public bool IsGrounded { get; private set; }
        public bool HasAuthority { get; private set; }
        public float Yaw { get; set; }
        public Vector3 MoveInput => _moveInput;
        public float Strength => _strength;
        public Vector3 Velocity => ragdollHips != null ? ragdollHips.linearVelocity : Vector3.zero;
        public Rigidbody Hips => ragdollHips;
        public Rigidbody DisplayHips => _visualHips != null ? _visualHips : ragdollHips;

        public event Action Destroying;

        private class Limb
        {
            public ConfigurableJoint Joint;
            public Transform Target;
            public Transform ConnectedTarget;
            public Quaternion StartRelativeRotation;
            public Quaternion JointFrame;
            public bool IsLeg;
            public bool IsArm;
            public ConfigurableJointMotion AngularX, AngularY, AngularZ;
        }

        private class UprightBody
        {
            public Rigidbody Body;
            public Quaternion RestRotation;
            public Transform Target;
            public float Weight;
        }

        private readonly List<Limb> _limbs = new List<Limb>();
        private readonly List<UprightBody> _upright = new List<UprightBody>();
        private readonly HashSet<Collider> _ownColliders = new HashSet<Collider>();
        private readonly HashSet<Rigidbody> _legBodies = new HashSet<Rigidbody>();
        private readonly RaycastHit[] _groundHits = new RaycastHit[16];

        private Rigidbody[] _bodies;
        private RagdollPart[] _parts;
        private Rigidbody _head;
        private Rigidbody[] _feet = Array.Empty<Rigidbody>();
        private float _totalMass;
        private Dictionary<string, Transform> _animatedBonesCache;

        private float _standHeight;
        private float _groundY;
        private Vector3 _hipsOffsetInRoot;
        private Vector3 _rootVelocity;

        private Vector3 _moveInput;
        private float _jumpCooldown;

        private float _strength = 1f;
        private float _appliedStrength = -1f;
        private float _knockTimer;
        private float _knockDuration;

        private bool _isThrowing;
        private float _throwHoldTimer;
        private float _currentTorsoFollow = -1f;

        private int _throwChargingHash;
        private int _throwStateHash;
        private bool _hasThrowChargingParam;
        private bool _throwBoostActive;
        private float _throwBoostTimer;

        private Vector3 _externalPush;
        private float _pushTimer;

        private bool _hasRemoteState;
        private Vector3 _remotePos;
        private Quaternion _remoteRot;

        private Transform _visualModel;
        private Rigidbody _visualHips;
        private Rigidbody[] _visualBodies;
        private RagdollPart[] _visualParts;
        private readonly List<Limb> _visualLimbs = new List<Limb>();
        private readonly List<Collider> _visualColliders = new List<Collider>();
        private float _visualSampleTimer;
        private bool _visualHasSample;
        private Vector3 _visualPos;
        private Vector3 _visualVel;
        private Quaternion _visualRot;
        private float _visualSampleTime;

        private void Awake()
        {
            if (animator == null) animator = GetComponentInParent<Animator>();

            _throwChargingHash = Animator.StringToHash(throwChargingParameter);
            _throwStateHash = Animator.StringToHash(throwStateName);
            foreach (var p in animator.parameters)
                if (p.nameHash == _throwChargingHash && p.type == AnimatorControllerParameterType.Bool)
                    _hasThrowChargingParam = true;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

            if (hideAnimatedMesh)
                foreach (var r in animatedModel.GetComponentsInChildren<Renderer>())
                    r.enabled = false;

            foreach (var a in ragdollModel.GetComponentsInChildren<Animator>())
                a.enabled = false;

            var animatedBones = new Dictionary<string, Transform>();
            foreach (var t in animatedModel.GetComponentsInChildren<Transform>(true))
                animatedBones.TryAdd(t.name, t);
            _animatedBonesCache = animatedBones;

            ConvertCharacterJoints();

            foreach (var joint in ragdollModel.GetComponentsInChildren<ConfigurableJoint>())
            {
                if (!animatedBones.TryGetValue(joint.name, out var target))
                {
                    Debug.LogWarning($"[ActiveRagdoll] No bone in the animated copy for joint '{joint.name}'", joint);
                    continue;
                }

                joint.configuredInWorldSpace = false;
                joint.rotationDriveMode = RotationDriveMode.Slerp;

                Transform connected = joint.connectedBody != null ? joint.connectedBody.transform : joint.transform.parent;
                if (!animatedBones.TryGetValue(connected.name, out var connectedTarget))
                    connectedTarget = target.parent;

                _limbs.Add(new Limb
                {
                    Joint = joint,
                    Target = target,
                    ConnectedTarget = connectedTarget,
                    StartRelativeRotation = Quaternion.Inverse(connected.rotation) * joint.transform.rotation,
                    JointFrame = GetJointFrame(joint),
                    AngularX = joint.angularXMotion,
                    AngularY = joint.angularYMotion,
                    AngularZ = joint.angularZMotion
                });
            }

            _bodies = ragdollModel.GetComponentsInChildren<Rigidbody>();
            _parts = new RagdollPart[_bodies.Length];
            _totalMass = 0f;

            for (int i = 0; i < _bodies.Length; i++)
            {
                var rb = _bodies[i];
                rb.isKinematic = false;
                rb.useGravity = true;
                rb.interpolation = RigidbodyInterpolation.Interpolate;
                rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
                rb.maxAngularVelocity = 50f;
                rb.solverIterations = 12;
                _totalMass += rb.mass;

                var part = rb.GetComponent<RagdollPart>();
                if (part == null) part = rb.gameObject.AddComponent<RagdollPart>();
                part.Init(this, i, rb);
                _parts[i] = part;
            }

            foreach (var c in ragdollModel.GetComponentsInChildren<Collider>(true))
                _ownColliders.Add(c);

            _head = FindRagdollBody(HumanBodyBones.Head)
                    ?? FindBodyByName("spine.006", "spine.005", "head", "Head", "mixamorig:Head");
            Rigidbody chest = FindRagdollBody(HumanBodyBones.UpperChest)
                              ?? FindRagdollBody(HumanBodyBones.Chest)
                              ?? FindBodyByName("spine.003", "spine.002", "chest", "Chest", "Spine2", "mixamorig:Spine2", "mixamorig:Spine1");

            var feet = new List<Rigidbody>();
            AddIfFound(feet, FindRagdollBody(HumanBodyBones.LeftFoot) ?? FindRagdollBody(HumanBodyBones.LeftLowerLeg)
                             ?? FindBodyByName("foot.L", "shin.L", "LeftFoot", "mixamorig:LeftFoot", "LeftLeg", "mixamorig:LeftLeg"));
            AddIfFound(feet, FindRagdollBody(HumanBodyBones.RightFoot) ?? FindRagdollBody(HumanBodyBones.RightLowerLeg)
                             ?? FindBodyByName("foot.R", "shin.R", "RightFoot", "mixamorig:RightFoot", "RightLeg", "mixamorig:RightLeg"));
            _feet = feet.ToArray();

            foreach (var foot in _feet)
            {
                var current = foot;
                for (int guard = 0; current != null && current != ragdollHips && guard < 16; guard++)
                {
                    _legBodies.Add(current);
                    var j = current.GetComponent<Joint>();
                    current = j != null ? j.connectedBody : null;
                }
            }
            foreach (var limb in _limbs)
                if (limb.Joint.TryGetComponent(out Rigidbody limbBody) && _legBodies.Contains(limbBody))
                    limb.IsLeg = true;

            if (_head == null) Debug.LogWarning("[ActiveRagdoll] Head Rigidbody not found, Head Lift will not work", this);
            if (chest == null) Debug.LogWarning("[ActiveRagdoll] Chest Rigidbody not found, only the hips will keep balance", this);
            if (_feet.Length == 0) Debug.LogWarning("[ActiveRagdoll] Feet Rigidbodies not found", this);

            Yaw = transform.eulerAngles.y;
            Quaternion yawRot = Quaternion.Euler(0f, Yaw, 0f);

            AddUpright(ragdollHips, 1f, yawRot);
            if (chest != ragdollHips) AddUpright(chest, chestUprightWeight, yawRot);
            if (_head != chest && _head != ragdollHips) AddUpright(_head, headUprightWeight, yawRot);

            var armBodies = new HashSet<Rigidbody>();
            var hands = new List<Rigidbody>();
            AddIfFound(hands, FindRagdollBody(HumanBodyBones.LeftHand) ?? FindRagdollBody(HumanBodyBones.LeftLowerArm)
                              ?? FindBodyByName("hand.L", "forearm.L", "LeftHand", "mixamorig:LeftHand", "LeftForeArm", "mixamorig:LeftForeArm"));
            AddIfFound(hands, FindRagdollBody(HumanBodyBones.RightHand) ?? FindRagdollBody(HumanBodyBones.RightLowerArm)
                              ?? FindBodyByName("hand.R", "forearm.R", "RightHand", "mixamorig:RightHand", "RightForeArm", "mixamorig:RightForeArm"));
            foreach (var hand in hands)
            {
                var current = hand;
                for (int guard = 0; current != null && guard < 16; guard++)
                {
                    if (current == ragdollHips || _upright.Exists(u => u.Body == current)) break;
                    armBodies.Add(current);
                    var j = current.GetComponent<Joint>();
                    current = j != null ? j.connectedBody : null;
                }
            }
            foreach (var limb in _limbs)
                if (limb.Joint.TryGetComponent(out Rigidbody limbBody) && armBodies.Contains(limbBody))
                    limb.IsArm = true;

            if (slipperyFeet)
            {
                var slippery = new PhysicsMaterial("RagdollFeet")
                {
                    dynamicFriction = feetFriction,
                    staticFriction = feetFriction,
                    frictionCombine = PhysicsMaterialCombine.Minimum
                };
                foreach (var foot in _feet)
                    foreach (var c in foot.GetComponentsInChildren<Collider>())
                        if (c.attachedRigidbody == foot)
                            c.sharedMaterial = slippery;
            }

            SetLimits(limitsWhileAlive);
            SetSelfCollision(selfCollision);

            _hipsOffsetInRoot = Quaternion.Inverse(yawRot) * (ragdollHips.position - transform.position);
            _standHeight = standHeightOverride > 0f ? standHeightOverride : MeasureStandHeight();
            _groundY = ragdollHips.position.y - _standHeight;

            SetAuthority(false);
            ApplyJointStrength(1f);

            ragdollModel.SetParent(null, true);
        }

        private void OnEnable()
        {
            if (!All.Contains(this)) All.Add(this);
        }

        private void OnDisable() => All.Remove(this);

        private void OnValidate()
        {
            if (!Application.isPlaying || _limbs.Count == 0) return;

            ApplyJointStrength(_strength);
            if (!IsDead) SetLimits(limitsWhileAlive);
            SetSelfCollision(selfCollision);
            if (_visualModel != null)
            {
                ApplyStrengthTo(_visualLimbs, IsDead ? 0f : 1f);
                if (!IsDead) SetLimitsOn(_visualLimbs, limitsWhileAlive);
                SetSelfCollisionOn(_visualColliders, selfCollision);
            }

            if (animatedModel != null)
                foreach (var r in animatedModel.GetComponentsInChildren<Renderer>())
                    r.enabled = !hideAnimatedMesh;
        }

        private void OnDestroy()
        {
            if (IsDead)
            {
                if (_visualModel != null && ragdollModel != null) Destroy(ragdollModel.gameObject);
                return;
            }

            Destroying?.Invoke();
            if (ragdollModel != null) Destroy(ragdollModel.gameObject);
            if (_visualModel != null) Destroy(_visualModel.gameObject);
        }

        public void SetAuthority(bool hasAuthority)
        {
            HasAuthority = hasAuthority;
            if (!IsDead && ragdollHips != null)
                ragdollHips.isKinematic = !hasAuthority;

            if (hasAuthority && ownerUsesRemoteLook && !IsDead && _visualModel == null)
                CreateVisualCopy();
        }

        public void SetMoveInput(Vector3 worldVelocity)
        {
            worldVelocity.y = 0f;
            _moveInput = worldVelocity;
        }

        public bool Jump()
        {
            if (!HasAuthority || IsDead || !IsGrounded || _strength < 0.5f) return false;

            foreach (var rb in _bodies)
            {
                Vector3 v = rb.linearVelocity;
                v.y = Mathf.Max(v.y, 0f) + jumpVelocity;
                rb.linearVelocity = v;
            }

            _jumpCooldown = 0.25f;
            IsGrounded = false;
            return true;
        }

        public void KnockOut(float duration)
        {
            if (IsDead) return;
            if (_knockTimer > 0f && _knockTimer > duration) return;
            _knockDuration = Mathf.Max(0.05f, duration);
            _knockTimer = _knockDuration;
        }

        public void ApplyHit(RagdollPart part, Vector3 impulse, Vector3 point, Vector3 knockback = default)
        {
            if (part == null || part.Owner != this) return;

            if (HasAuthority && knockback != Vector3.zero)
                foreach (var body in _bodies)
                    body.AddForce(knockback, ForceMode.VelocityChange);

            var physicsBody = _bodies[part.Index];
            if (!physicsBody.isKinematic)
                physicsBody.AddForceAtPosition(impulse, point, ForceMode.Impulse);

            if (_visualBodies != null && part.Index < _visualBodies.Length)
            {
                var visualBody = _visualBodies[part.Index];
                if (!visualBody.isKinematic)
                    visualBody.AddForceAtPosition(impulse, point, ForceMode.Impulse);
            }

            if (HasAuthority && !IsDead)
                KnockOut(hitKnockoutDuration);
        }

        public void Die(Vector3 impulse = default, RagdollPart hitPart = null)
        {
            if (IsDead) return;
            IsDead = true;
            _strength = 0f;
            _moveInput = Vector3.zero;

            ragdollHips.isKinematic = false;
            foreach (var rb in _bodies) rb.useGravity = true;
            ApplyJointStrength(0f);
            SetLimits(true);

            if (_visualModel != null)
            {
                _visualHips.isKinematic = false;
                foreach (var rb in _visualBodies) rb.useGravity = true;
                ApplyStrengthTo(_visualLimbs, 0f);
                SetLimitsOn(_visualLimbs, true);
                ragdollModel.gameObject.SetActive(false);

                if (impulse != Vector3.zero)
                {
                    var body = hitPart != null && hitPart.Index < _visualBodies.Length ? _visualBodies[hitPart.Index] : _visualHips;
                    body.AddForce(impulse, ForceMode.Impulse);
                }
                return;
            }

            if (impulse != Vector3.zero)
                (hitPart != null ? _bodies[hitPart.Index] : ragdollHips).AddForce(impulse, ForceMode.Impulse);
        }

        public void ReceivePush(Vector3 velocity)
        {
            if (!HasAuthority || IsDead) return;
            velocity.y = 0f;
            _externalPush = velocity;
            _pushTimer = pushHoldTime;
        }

        public void BeginThrow()
        {
            _isThrowing = true;
            _throwHoldTimer = 0f;
        }

        public void EndThrow(float holdTime = 0.6f)
        {
            _isThrowing = false;
            _throwHoldTimer = holdTime;
        }

        public void Teleport(Vector3 hipsPosition)
        {
            Vector3 delta = hipsPosition - ragdollHips.position;
            foreach (var rb in _bodies)
            {
                rb.position += delta;
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }

            if (_visualBodies != null)
            {
                foreach (var rb in _visualBodies)
                {
                    rb.position += delta;
                    if (!rb.isKinematic)
                    {
                        rb.linearVelocity = Vector3.zero;
                        rb.angularVelocity = Vector3.zero;
                    }
                }
                _visualHasSample = false;
            }
        }

        public RagdollPart GetPart(int index)
        {
            var parts = _visualParts ?? _parts;
            return parts != null && index >= 0 && index < parts.Length ? parts[index] : null;
        }

        public void SetIgnoreBodyCollisions(Collider other, bool ignore)
        {
            if (other == null) return;

            foreach (var c in _ownColliders)
                if (c != null && c.enabled)
                    Physics.IgnoreCollision(other, c, ignore);

            foreach (var c in _visualColliders)
                if (c != null && c.enabled)
                    Physics.IgnoreCollision(other, c, ignore);
        }

        public Transform ToDisplay(Transform physicsTransform)
        {
            if (_visualModel == null || physicsTransform == null || !physicsTransform.IsChildOf(ragdollModel))
                return physicsTransform;

            var path = new List<string>();
            for (var t = physicsTransform; t != ragdollModel; t = t.parent)
                path.Insert(0, t.name);

            var mapped = _visualModel.Find(string.Join("/", path));
            return mapped != null ? mapped : physicsTransform;
        }

        public void ApplyRemoteState(Vector3 hipsPos, Quaternion hipsRot, float yaw)
        {
            _remotePos = hipsPos;
            _remoteRot = hipsRot;
            Yaw = yaw;
            _hasRemoteState = true;
        }

        private void FixedUpdate()
        {
            if (ragdollHips == null) return;
            if (IsDead) return;

            float dt = Time.fixedDeltaTime;
            UpdateKnockout(dt);
            UpdateThrowBoost(dt);
            DriveJoints(_limbs);

            if (!HasAuthority)
            {
                FollowRemoteState();
                return;
            }

            if (_jumpCooldown > 0f) _jumpCooldown -= dt;
            if (_pushTimer > 0f) _pushTimer -= dt;
            CheckGround();
            Balance();
            Move();

            if (_visualModel != null)
                UpdateVisualCopy(dt);
        }

        private void LateUpdate()
        {
            if (ragdollHips == null) return;

            Quaternion yawRot = Quaternion.Euler(0f, Yaw, 0f);
            Vector3 target = DisplayHips.position - yawRot * _hipsOffsetInRoot;
            if (IsGrounded) target.y = Mathf.Max(target.y, _groundY);

            transform.position = Vector3.SmoothDamp(transform.position, target, ref _rootVelocity, rootSmoothTime);
            transform.rotation = yawRot;
        }

        private void UpdateKnockout(float dt)
        {
            float target = 1f;
            if (_knockTimer > 0f)
            {
                _knockTimer -= dt;
                float t = 1f - Mathf.Clamp01(_knockTimer / _knockDuration);
                target = Mathf.Lerp(knockoutMinStrength, 1f, t * t * t);
            }

            _strength = target;
            if (Mathf.Abs(_appliedStrength - _strength) > 0.01f)
                ApplyJointStrength(_strength);
        }

        private void UpdateThrowBoost(float dt)
        {
            bool throwingNow = false;
            if (animator != null && animator.isActiveAndEnabled)
            {
                if (_hasThrowChargingParam && animator.GetBool(_throwChargingHash))
                    throwingNow = true;
                else if (throwAnimatorLayer >= 0 && throwAnimatorLayer < animator.layerCount &&
                         animator.GetCurrentAnimatorStateInfo(throwAnimatorLayer).shortNameHash == _throwStateHash)
                    throwingNow = true;
            }

            if (throwingNow) _throwBoostTimer = throwBoostHold;
            else if (_throwBoostTimer > 0f) _throwBoostTimer -= dt;

            bool active = throwingNow || _throwBoostTimer > 0f;
            if (active == _throwBoostActive) return;

            _throwBoostActive = active;
            ApplyJointStrength(_strength);
            if (_visualModel != null) ApplyStrengthTo(_visualLimbs, 1f);
        }

        private static void DriveJoints(List<Limb> limbs)
        {
            foreach (var limb in limbs)
            {
                Quaternion animatedRelative = Quaternion.Inverse(limb.ConnectedTarget.rotation) * limb.Target.rotation;

                limb.Joint.targetRotation =
                    Quaternion.Inverse(limb.JointFrame)
                    * Quaternion.Inverse(animatedRelative)
                    * limb.StartRelativeRotation
                    * limb.JointFrame;
            }
        }

        private void CheckGround()
        {
            Vector3 origin = ragdollHips.position;
            float maxDist = _standHeight + groundCheckExtra;

            int count = Physics.SphereCastNonAlloc(origin, groundCheckRadius, Vector3.down, _groundHits,
                maxDist, groundMask, QueryTriggerInteraction.Ignore);

            bool found = false;
            float bestY = float.MinValue;

            for (int i = 0; i < count; i++)
            {
                var h = _groundHits[i];
                if (_ownColliders.Contains(h.collider)) continue;
                if (h.collider.transform.IsChildOf(ragdollModel)) continue;
                if (_visualModel != null && h.collider.transform.IsChildOf(_visualModel)) continue;
                var attached = h.collider.attachedRigidbody;
                if (attached != null && attached.TryGetComponent(out RagdollPart p) && p.Owner == this) continue;

                float y = h.distance <= 0f ? origin.y - _standHeight : h.point.y;
                if (y > bestY)
                {
                    bestY = y;
                    found = true;
                }
            }

            IsGrounded = found && _jumpCooldown <= 0f;
            if (found) _groundY = bestY;
        }

        private void Balance()
        {
            float s = _strength;

            if (_throwHoldTimer > 0f) _throwHoldTimer -= Time.fixedDeltaTime;
            bool throwing = _isThrowing || _throwHoldTimer > 0f;
            float wantedFollow = throwing ? throwTorsoFollow : torsoAnimationFollow;
            if (_currentTorsoFollow < 0f) _currentTorsoFollow = wantedFollow;
            _currentTorsoFollow = Mathf.MoveTowards(_currentTorsoFollow, wantedFollow, torsoFollowBlendSpeed * Time.fixedDeltaTime);

            Quaternion yawRot = Quaternion.Euler(0f, Yaw, 0f);
            foreach (var u in _upright)
            {
                Quaternion upright = yawRot * u.RestRotation;
                Quaternion targetRot = u.Target != null
                    ? Quaternion.Slerp(upright, u.Target.rotation, _currentTorsoFollow)
                    : upright;
                Quaternion delta = targetRot * Quaternion.Inverse(u.Body.rotation);
                delta.ToAngleAxis(out float angle, out Vector3 axis);
                if (angle > 180f) angle -= 360f;
                if (float.IsNaN(axis.x) || float.IsInfinity(axis.x)) continue;

                Vector3 torque = axis * (angle * Mathf.Deg2Rad * uprightStrength)
                                 - u.Body.angularVelocity * uprightDamping;
                u.Body.AddTorque(torque * (s * u.Weight), ForceMode.Acceleration);
            }

            if (_head != null)
                _head.AddForce(Vector3.up * (headLift * s), ForceMode.Acceleration);

            bool walking = _moveInput.sqrMagnitude > 0.01f;
            if (!walking && IsGrounded)
                foreach (var foot in _feet)
                    foot.AddForce(Vector3.down * (feetDown * s), ForceMode.Acceleration);

            Vector3 antiGravity = -Physics.gravity * (gravityCompensation * s);
            foreach (var rb in _bodies)
                rb.AddForce(antiGravity, ForceMode.Acceleration);

            if (IsGrounded)
            {
                float targetHeight = _standHeight + (walking ? walkHipsLift : 0f);
                float heightError = Mathf.Clamp((_groundY + targetHeight) - ragdollHips.position.y, -0.5f, 0.5f);
                float accel = heightError * heightSpring - ragdollHips.linearVelocity.y * heightDamping;
                accel = Mathf.Max(0f, accel);
                ragdollHips.AddForce(Vector3.up * (accel * _totalMass * s), ForceMode.Force);
            }
        }

        private void Move()
        {
            Vector3 vel = ragdollHips.linearVelocity;
            Vector3 horizontal = new Vector3(vel.x, 0f, vel.z);
            float accel = IsGrounded ? groundAcceleration : airAcceleration;
            Vector3 push = _pushTimer > 0f ? _externalPush : Vector3.zero;

            Vector3 acceleration = Vector3.zero;

            if (_strength >= 0.3f)
            {
                acceleration = (_moveInput + push - horizontal) * (accel * _strength);

                if (push != Vector3.zero)
                {
                    Vector3 dir = push.normalized;
                    float deficit = push.magnitude - Vector3.Dot(horizontal, dir);
                    if (deficit > 0f)
                        acceleration += dir * (deficit * pushAcceleration * (1f - _strength * 0.5f));
                }
            }
            else if (push != Vector3.zero)
            {
                Vector3 dir = push.normalized;
                float deficit = push.magnitude - Vector3.Dot(horizontal, dir);
                if (deficit > 0f)
                    acceleration = dir * (deficit * pushAcceleration);
            }

            if (acceleration == Vector3.zero) return;
            foreach (var rb in _bodies)
                rb.AddForce(acceleration, ForceMode.Acceleration);
        }

        private void FollowRemoteState()
        {
            if (!_hasRemoteState) return;

            ragdollHips.MovePosition(Vector3.Lerp(ragdollHips.position, _remotePos, 0.5f));
            ragdollHips.MoveRotation(Quaternion.Slerp(ragdollHips.rotation, _remoteRot, 0.5f));
            IsGrounded = false;
        }

        internal void OnPartCollision(RagdollPart part, Collision collision)
        {
            if (!HasAuthority || IsDead || part.IsVisual) return;
            if (_legBodies.Contains(part.Body)) return;

            if (collision.rigidbody != null &&
                collision.rigidbody.TryGetComponent(out RagdollPart otherPart) &&
                otherPart.Owner == this) return;

            float deltaV = collision.impulse.magnitude / Mathf.Max(_totalMass, 0.01f);
            if (deltaV > knockoutVelocity)
                KnockOut(knockoutDuration);
        }

        private void CreateVisualCopy()
        {
            var clone = Instantiate(ragdollModel.gameObject, ragdollModel.position, ragdollModel.rotation);
            clone.name = ragdollModel.name + "_Visual";
            _visualModel = clone.transform;

            foreach (var r in ragdollModel.GetComponentsInChildren<Renderer>())
                r.enabled = false;

            _visualBodies = _visualModel.GetComponentsInChildren<Rigidbody>();
            if (_visualBodies.Length != _bodies.Length)
            {
                Debug.LogError("[ActiveRagdoll] Visual copy does not match the physics body, disabling Owner Uses Remote Look", this);
                Destroy(clone);
                _visualModel = null;
                _visualBodies = null;
                foreach (var r in ragdollModel.GetComponentsInChildren<Renderer>()) r.enabled = true;
                return;
            }

            _visualParts = new RagdollPart[_visualBodies.Length];
            int hipsIndex = Array.IndexOf(_bodies, ragdollHips);
            for (int i = 0; i < _visualBodies.Length; i++)
            {
                var rb = _visualBodies[i];
                rb.isKinematic = i == hipsIndex;
                rb.useGravity = true;
                var part = rb.GetComponent<RagdollPart>();
                if (part == null) part = rb.gameObject.AddComponent<RagdollPart>();
                part.Init(this, i, rb, isVisual: true);
                _visualParts[i] = part;
            }
            _visualHips = _visualBodies[hipsIndex];

            foreach (var limb in _limbs)
            {
                int index = Array.IndexOf(_bodies, limb.Joint.GetComponent<Rigidbody>());
                if (index < 0) continue;
                var joint = _visualBodies[index].GetComponent<ConfigurableJoint>();
                if (joint == null) continue;

                _visualLimbs.Add(new Limb
                {
                    Joint = joint,
                    Target = limb.Target,
                    ConnectedTarget = limb.ConnectedTarget,
                    StartRelativeRotation = limb.StartRelativeRotation,
                    JointFrame = limb.JointFrame,
                    IsLeg = limb.IsLeg,
                    IsArm = limb.IsArm,
                    AngularX = limb.AngularX,
                    AngularY = limb.AngularY,
                    AngularZ = limb.AngularZ
                });
            }

            _visualColliders.AddRange(_visualModel.GetComponentsInChildren<Collider>(true));
            foreach (var v in _visualColliders)
                foreach (var p in _ownColliders)
                    if (v != null && p != null)
                        Physics.IgnoreCollision(v, p, true);

            ApplyStrengthTo(_visualLimbs, 1f);
            SetLimitsOn(_visualLimbs, limitsWhileAlive);
            SetSelfCollisionOn(_visualColliders, selfCollision);
        }

        private void UpdateVisualCopy(float dt)
        {
            _visualSampleTimer += dt;
            if (!_visualHasSample || _visualSampleTimer >= 1f / Mathf.Max(1f, visualSendRate))
            {
                _visualSampleTimer = 0f;
                _visualPos = ragdollHips.position;
                _visualRot = ragdollHips.rotation;
                _visualVel = ragdollHips.linearVelocity;
                _visualSampleTime = Time.time;
                _visualHasSample = true;
            }

            float age = Mathf.Min(Time.time - _visualSampleTime, 0.2f);
            Vector3 target = _visualPos + _visualVel * age;

            _visualHips.MovePosition(Vector3.Lerp(_visualHips.position, target, 0.5f));
            _visualHips.MoveRotation(Quaternion.Slerp(_visualHips.rotation, _visualRot, 0.5f));
            DriveJoints(_visualLimbs);
        }

        private void ApplyJointStrength(float multiplier)
        {
            _appliedStrength = multiplier;
            ApplyStrengthTo(_limbs, multiplier);
        }

        private void ApplyStrengthTo(List<Limb> limbs, float multiplier)
        {
            foreach (var limb in limbs)
            {
                float limbMultiplier = limb.IsLeg ? legSpringMultiplier
                    : limb.IsArm ? armSpringMultiplier * (_throwBoostActive ? throwArmBoost : 1f)
                    : 1f;
                limb.Joint.slerpDrive = new JointDrive
                {
                    positionSpring = jointSpring * multiplier * limbMultiplier,
                    positionDamper = Mathf.Max(jointDamper * multiplier * Mathf.Sqrt(limbMultiplier), deadJointDamper),
                    maximumForce = float.MaxValue
                };
            }
        }

        private void SetLimits(bool limited) => SetLimitsOn(_limbs, limited);

        private static void SetLimitsOn(List<Limb> limbs, bool limited)
        {
            foreach (var limb in limbs)
            {
                limb.Joint.angularXMotion = limited ? limb.AngularX : ConfigurableJointMotion.Free;
                limb.Joint.angularYMotion = limited ? limb.AngularY : ConfigurableJointMotion.Free;
                limb.Joint.angularZMotion = limited ? limb.AngularZ : ConfigurableJointMotion.Free;
            }
        }

        private void SetSelfCollision(bool collide) => SetSelfCollisionOn(new List<Collider>(_ownColliders), collide);

        private static void SetSelfCollisionOn(List<Collider> list, bool collide)
        {
            for (int i = 0; i < list.Count; i++)
                for (int k = i + 1; k < list.Count; k++)
                    if (list[i] != null && list[k] != null)
                        Physics.IgnoreCollision(list[i], list[k], !collide);
        }

        private float MeasureStandHeight()
        {
            Physics.SyncTransforms();
            float minY = float.MaxValue;
            foreach (var c in _ownColliders)
                if (c.enabled && !c.isTrigger)
                    minY = Mathf.Min(minY, c.bounds.min.y);

            float h = ragdollHips.position.y - minY;
            if (minY == float.MaxValue || h < 0.1f || h > 5f)
            {
                Debug.LogWarning("[ActiveRagdoll] Could not measure hips height, set Stand Height Override manually", this);
                h = 1f;
            }
            return h;
        }

        private Rigidbody FindRagdollBody(HumanBodyBones bone)
        {
            if (animator == null || !animator.isHuman) return null;
            Transform animatedBone = animator.GetBoneTransform(bone);
            if (animatedBone == null) return null;

            foreach (var t in ragdollModel.GetComponentsInChildren<Transform>(true))
            {
                if (t.name != animatedBone.name) continue;
                for (var p = t; p != null && p != ragdollModel.parent; p = p.parent)
                    if (p.TryGetComponent(out Rigidbody rb))
                        return rb;
            }
            return null;
        }

        private Rigidbody FindBodyByName(params string[] names)
        {
            foreach (var n in names)
                foreach (var rb in _bodies)
                    if (rb.name == n)
                        return rb;
            return null;
        }

        private void AddUpright(Rigidbody body, float weight, Quaternion yawRot)
        {
            if (body == null) return;
            Transform target = null;
            _animatedBonesCache?.TryGetValue(body.name, out target);
            _upright.Add(new UprightBody
            {
                Body = body,
                RestRotation = Quaternion.Inverse(yawRot) * body.rotation,
                Target = target,
                Weight = weight
            });
        }

        private static void AddIfFound(List<Rigidbody> list, Rigidbody rb)
        {
            if (rb != null && !list.Contains(rb)) list.Add(rb);
        }

        private static Quaternion GetJointFrame(ConfigurableJoint joint)
        {
            Vector3 right = joint.axis;
            Vector3 forward = Vector3.Cross(joint.axis, joint.secondaryAxis).normalized;
            Vector3 up = Vector3.Cross(forward, right).normalized;
            return Quaternion.LookRotation(forward, up);
        }

        private void ConvertCharacterJoints()
        {
            foreach (var cj in ragdollModel.GetComponentsInChildren<CharacterJoint>())
            {
                var go = cj.gameObject;
                var connected = cj.connectedBody;
                var anchor = cj.anchor;
                var axis = cj.axis;
                var swingAxis = cj.swingAxis;
                var autoAnchor = cj.autoConfigureConnectedAnchor;
                var connectedAnchor = cj.connectedAnchor;
                float lowTwist = cj.lowTwistLimit.limit;
                float highTwist = cj.highTwistLimit.limit;
                float swing1 = cj.swing1Limit.limit;
                float swing2 = cj.swing2Limit.limit;

                DestroyImmediate(cj);

                var j = go.AddComponent<ConfigurableJoint>();
                j.connectedBody = connected;
                j.anchor = anchor;
                j.axis = axis;
                j.secondaryAxis = swingAxis;
                j.autoConfigureConnectedAnchor = autoAnchor;
                if (!autoAnchor) j.connectedAnchor = connectedAnchor;

                j.xMotion = j.yMotion = j.zMotion = ConfigurableJointMotion.Locked;
                j.angularXMotion = j.angularYMotion = j.angularZMotion = ConfigurableJointMotion.Limited;
                j.lowAngularXLimit = new SoftJointLimit { limit = lowTwist };
                j.highAngularXLimit = new SoftJointLimit { limit = highTwist };
                j.angularYLimit = new SoftJointLimit { limit = swing1 };
                j.angularZLimit = new SoftJointLimit { limit = swing2 };
                j.enablePreprocessing = false;
            }
        }
    }
}