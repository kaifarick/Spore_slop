using System.Linq;
using SporeSlop.Player;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace SporeSlop.EditorTools
{
    /// <summary>
    /// One-click, idempotent setup for the player's character animation:
    /// builds <c>PlayerAnimator.controller</c> from the imported Mixamo clips and puts the X Bot model
    /// under the Player prefab's Visual (capsule mesh renderer is disabled, physics capsules are untouched).
    /// Run via SporeSlop → Setup Player Animation. Safe to re-run.
    /// </summary>
    public static class PlayerAnimationSetup
    {
        const string ModelPath = "Assets/_Project/Models/X Bot.fbx";
        const string ClipDir = "Assets/_Project/Models/Animation/";
        const string ControllerPath = "Assets/_Project/Animation/PlayerAnimator.controller";
        const string UpperBodyMaskPath = "Assets/_Project/Animation/UpperBodyMask.mask";
        const string PlayerPrefabPath = "Assets/_Project/Prefabs/Player.prefab";
        const string ModelChildName = "Model";

        // Locomotion blend thresholds match PlayerMovement walkSpeed / sprintSpeed.
        const float WalkSpeed = 4f;
        const float RunSpeed = 7f;

        [MenuItem("SporeSlop/Setup Player Animation")]
        public static void Run()
        {
            ConfigureGettingUpClip();

            AnimatorController controller = BuildController();
            if (controller == null)
                return;

            if (!SetupPlayerPrefab(controller))
                return;

            AssetDatabase.SaveAssets();
            Debug.Log("[PlayerAnimationSetup] Done: Getting Up bake + Holding upper body + PlayerAnimator + Player.prefab.");
        }

        /// <summary>
        /// Mixamo Getting Up stores stand-up rise in Root Transform Position (Y).
        /// With Animator.applyRootMotion=false that rise is discarded (hips stay on the floor).
        /// Bake Root Y/XZ/Rotation into pose so bone animation contains the stand-up.
        /// Red "Loop Match" dots on a one-shot clip are expected (start ≠ end height) — not an error.
        /// </summary>
        static void ConfigureGettingUpClip()
        {
            const string path = ClipDir + "Getting Up.fbx";
            ModelImporter importer = AssetImporter.GetAtPath(path) as ModelImporter;
            if (importer == null)
            {
                Debug.LogError($"[PlayerAnimationSetup] Missing: {path}");
                return;
            }

            ModelImporterClipAnimation[] clips = importer.clipAnimations;
            if (clips == null || clips.Length == 0)
                clips = importer.defaultClipAnimations;

            if (clips == null || clips.Length == 0)
            {
                Debug.LogError($"[PlayerAnimationSetup] No clips in {path}");
                return;
            }

            bool dirty = false;
            for (int i = 0; i < clips.Length; i++)
            {
                ModelImporterClipAnimation clip = clips[i];
                clip.loopTime = false;

                // Bake Into Pose = lockRoot*; Keep Original = keepOriginal*
                if (!clip.lockRootHeightY || !clip.lockRootPositionXZ || !clip.lockRootRotation
                    || !clip.keepOriginalPositionY || !clip.keepOriginalPositionXZ || !clip.keepOriginalOrientation)
                {
                    clip.lockRootRotation = true;
                    clip.keepOriginalOrientation = true;
                    clip.lockRootHeightY = true;
                    clip.keepOriginalPositionY = true;
                    clip.heightFromFeet = false;
                    clip.lockRootPositionXZ = true;
                    clip.keepOriginalPositionXZ = true;
                    dirty = true;
                }

                clips[i] = clip;
            }

            if (!dirty)
            {
                Debug.Log("[PlayerAnimationSetup] Getting Up already baked into pose.");
                return;
            }

            importer.clipAnimations = clips;
            EditorUtility.SetDirty(importer);
            importer.SaveAndReimport();
            Debug.Log("[PlayerAnimationSetup] Getting Up: Root Transform baked into pose (Y/XZ/Rotation).");
        }

        static AnimatorController BuildController()
        {
            AnimationClip idle = LoadClip("Idle");
            AnimationClip walk = LoadClip("Walking");
            AnimationClip run = LoadClip("Run");
            AnimationClip air = LoadClip("Jumping Up");
            AnimationClip gettingUp = LoadClip("Getting Up");
            if (idle == null || walk == null || run == null || air == null || gettingUp == null)
                return null;

            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (controller == null)
                controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);

            // Rebuild from scratch so re-running never duplicates states or transitions.
            while (controller.layers.Length > 0)
                controller.RemoveLayer(0);
            foreach (AnimatorControllerParameter parameter in controller.parameters)
                controller.RemoveParameter(parameter);

            controller.AddLayer("Base Layer");
            controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
            controller.AddParameter("CycleSpeed", AnimatorControllerParameterType.Float);
            controller.AddParameter("IsGrounded", AnimatorControllerParameterType.Bool);
            controller.AddParameter("IsRagdolled", AnimatorControllerParameterType.Bool);
            controller.AddParameter("IsGettingUp", AnimatorControllerParameterType.Bool);
            controller.AddParameter("IsHolding", AnimatorControllerParameterType.Bool);

            AnimatorStateMachine machine = controller.layers[0].stateMachine;

            AnimatorState locomotion = controller.CreateBlendTreeInController("Locomotion", out BlendTree tree, 0);
            tree.blendType = BlendTreeType.Simple1D;
            tree.blendParameter = "Speed";
            tree.useAutomaticThresholds = false;
            tree.AddChild(idle, 0f);
            tree.AddChild(walk, WalkSpeed);
            tree.AddChild(run, RunSpeed);
            machine.defaultState = locomotion;
            locomotion.speedParameterActive = true;
            locomotion.speedParameter = "CycleSpeed";

            AnimatorState airState = machine.AddState("Air");
            airState.motion = air;

            AnimatorState ragdollState = machine.AddState("Ragdoll");
            // Animator is disabled during bone ragdoll; Idle keeps the graph valid.
            ragdollState.motion = idle;

            AnimatorState gettingUpState = machine.AddState("GettingUp");
            gettingUpState.motion = gettingUp;

            AnimatorStateTransition toAir = locomotion.AddTransition(airState);
            toAir.hasExitTime = false;
            toAir.duration = 0.1f;
            toAir.AddCondition(AnimatorConditionMode.IfNot, 0f, "IsGrounded");
            toAir.AddCondition(AnimatorConditionMode.IfNot, 0f, "IsRagdolled");
            toAir.AddCondition(AnimatorConditionMode.IfNot, 0f, "IsGettingUp");

            AnimatorStateTransition landing = airState.AddTransition(locomotion);
            landing.hasExitTime = false;
            landing.duration = 0.15f;
            landing.AddCondition(AnimatorConditionMode.If, 0f, "IsGrounded");
            landing.AddCondition(AnimatorConditionMode.IfNot, 0f, "IsGettingUp");

            AnimatorStateTransition toRagdoll = machine.AddAnyStateTransition(ragdollState);
            toRagdoll.hasExitTime = false;
            toRagdoll.duration = 0.05f;
            toRagdoll.canTransitionToSelf = false;
            toRagdoll.AddCondition(AnimatorConditionMode.If, 0f, "IsRagdolled");

            // Soft blend into Idle — 0.08 was a hard cut after bake fixed the toss.
            AnimatorStateTransition gettingUpDone = gettingUpState.AddTransition(locomotion);
            gettingUpDone.hasExitTime = false;
            gettingUpDone.duration = 0.3f;
            gettingUpDone.hasFixedDuration = true;
            gettingUpDone.AddCondition(AnimatorConditionMode.IfNot, 0f, "IsGettingUp");

            AnimationClip holding = LoadClip("Holding");
            if (holding != null)
                AddHoldingUpperBodyLayer(controller, holding);
            else
                Debug.LogWarning("[PlayerAnimationSetup] Holding clip missing — upper-body hold layer skipped.");

            EditorUtility.SetDirty(controller);
            return controller;
        }

        static void AddHoldingUpperBodyLayer(AnimatorController controller, AnimationClip holding)
        {
            AvatarMask mask = GetOrCreateUpperBodyMask();

            controller.AddLayer("UpperBody");
            AnimatorControllerLayer[] layers = controller.layers;
            int index = layers.Length - 1;
            // Weight driven by PlayerAnimation (0 idle / 1 holding). Keeps base Idle when empty.
            layers[index].defaultWeight = 0f;
            layers[index].blendingMode = AnimatorLayerBlendingMode.Override;
            layers[index].avatarMask = mask;
            controller.layers = layers;

            AnimatorStateMachine sm = controller.layers[index].stateMachine;
            AnimatorState holdState = sm.AddState("Holding");
            holdState.motion = holding;
            sm.defaultState = holdState;
        }

        static AvatarMask GetOrCreateUpperBodyMask()
        {
            AvatarMask mask = AssetDatabase.LoadAssetAtPath<AvatarMask>(UpperBodyMaskPath);
            if (mask == null)
            {
                mask = new AvatarMask();
                AssetDatabase.CreateAsset(mask, UpperBodyMaskPath);
            }

            for (int i = 0; i < (int)AvatarMaskBodyPart.LastBodyPart; i++)
                mask.SetHumanoidBodyPartActive((AvatarMaskBodyPart)i, false);

            // Arms only — Body in the mask replaced Idle torso with full Holding stance.
            mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.LeftArm, true);
            mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.RightArm, true);
            mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.LeftFingers, true);
            mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.RightFingers, true);
            EditorUtility.SetDirty(mask);
            return mask;
        }

        static bool SetupPlayerPrefab(AnimatorController controller)
        {
            GameObject modelAsset = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
            if (modelAsset == null)
            {
                Debug.LogError($"[PlayerAnimationSetup] Model not found: {ModelPath}");
                return false;
            }

            GameObject root = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);
            try
            {
                Transform visual = root.transform.Find("Visual");
                if (visual == null)
                {
                    Debug.LogError("[PlayerAnimationSetup] Player prefab has no 'Visual' child.");
                    return false;
                }

                CharacterController cc = root.GetComponent<CharacterController>();

                // The scene instance already overrides Center.y = 0 (player root = capsule centre).
                // Make the prefab match so the model's feet offset below is valid everywhere.
                cc.center = Vector3.zero;

                Transform model = visual.Find(ModelChildName);
                if (model == null)
                {
                    GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(modelAsset, visual);
                    instance.name = ModelChildName;
                    model = instance.transform;
                }

                // Model pivot is at the feet: put it at the bottom of the CharacterController capsule.
                model.localPosition = new Vector3(0f, cc.center.y - cc.height * 0.5f, 0f);
                model.localRotation = Quaternion.identity;

                MeshRenderer capsuleMesh = visual.GetComponent<MeshRenderer>();
                if (capsuleMesh != null)
                    capsuleMesh.enabled = false;

                Animator animator = model.GetComponent<Animator>();
                if (animator == null)
                    animator = model.gameObject.AddComponent<Animator>();

                animator.runtimeAnimatorController = controller;
                animator.applyRootMotion = false;
                if (animator.avatar == null)
                    animator.avatar = AssetDatabase.LoadAllAssetsAtPath(ModelPath).OfType<Avatar>().FirstOrDefault();

                PlayerAnimation playerAnimation = root.GetComponent<PlayerAnimation>();
                if (playerAnimation == null)
                    playerAnimation = root.AddComponent<PlayerAnimation>();

                SerializedObject serialized = new SerializedObject(playerAnimation);
                serialized.FindProperty("animator").objectReferenceValue = animator;
                serialized.ApplyModifiedPropertiesWithoutUndo();

                Transform holdPoint = visual.Find("HoldPoint");
                if (holdPoint != null)
                {
                    HoldPointHandFollow follow = holdPoint.GetComponent<HoldPointHandFollow>();
                    if (follow == null)
                        follow = holdPoint.gameObject.AddComponent<HoldPointHandFollow>();

                    PlayerGrab grab = root.GetComponent<PlayerGrab>();
                    SerializedObject followSo = new SerializedObject(follow);
                    followSo.FindProperty("animator").objectReferenceValue = animator;
                    followSo.FindProperty("grab").objectReferenceValue = grab;
                    SerializedProperty followHands = followSo.FindProperty("followHandsWhileHolding");
                    if (followHands != null)
                        followHands.boolValue = false;
                    followSo.ApplyModifiedPropertiesWithoutUndo();
                }

                PrefabUtility.SaveAsPrefabAsset(root, PlayerPrefabPath);
                return true;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        static AnimationClip LoadClip(string fileName)
        {
            string path = ClipDir + fileName + ".fbx";
            AnimationClip clip = AssetDatabase.LoadAllAssetsAtPath(path)
                .OfType<AnimationClip>()
                .FirstOrDefault(c => !c.name.StartsWith("__preview__"));

            if (clip == null)
                Debug.LogError($"[PlayerAnimationSetup] No AnimationClip found in {path}");

            return clip;
        }
    }
}
