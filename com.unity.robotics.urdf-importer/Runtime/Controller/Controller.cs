using System;
using System.Collections.Generic;
using Unity.Robotics;
using UnityEngine;

namespace Unity.Robotics.UrdfImporter.Control
{
    public enum RotationDirection { None = 0, Positive = 1, Negative = -1 };
    public enum ControlType { PositionControl };

    // Modified (bada0817 fork):
    // - only movable joints (non-fixed) are selectable; the root and fixed links were selectable before,
    //   so arrow keys often landed on parts that cannot move
    // - the first selected joint is highlighted at start, and colors of every joint are stored once at start
    // - renderers are taken from the link's "Visuals" child (GetChild(0) only as a fallback), all material slots
    //   are highlighted and restored, and highlight colors also work for glTF (glTFast) materials (MaterialExtensions)
    // - works with the Input System package as well as the legacy Input Manager
    // - no crash when the robot has fewer than two movable joints
    public class Controller : MonoBehaviour
    {
        private ArticulationBody[] articulationChain;
        // Stores original colors of each joint's visuals (per renderer and material slot), captured at start
        private Color[][] prevColors;
        private int previousIndex;

        [InspectorReadOnly(hideInEditMode: true)]
        public string selectedJoint;
        [HideInInspector]
        public int selectedIndex;

        public ControlType control = ControlType.PositionControl;
        public float stiffness;
        public float damping;
        public float forceLimit;
        public float speed = 5f; // Units: degree/s
        public float torque = 100f; // Units: Nm or N
        public float acceleration = 5f;// Units: m/s^2 / degree/s^2

        [Tooltip("Color to highlight the currently selected join")]
        public Color highLightColor = new Color(1.0f, 0, 0, 1.0f);

        void Start()
        {
            this.gameObject.AddComponent<FKRobot>();
            ArticulationBody[] allBodies = this.GetComponentsInChildren<ArticulationBody>();
            var movable = new List<ArticulationBody>();
            int defDyanmicVal = 10;
            foreach (ArticulationBody joint in allBodies)
            {
                joint.gameObject.AddComponent<JointControl>();
                joint.jointFriction = defDyanmicVal;
                joint.angularDamping = defDyanmicVal;
                ArticulationDrive currentDrive = joint.xDrive;
                currentDrive.forceLimit = forceLimit;
                joint.xDrive = currentDrive;
                if (joint.jointType != ArticulationJointType.FixedJoint)
                {
                    movable.Add(joint);
                }
            }
            articulationChain = movable.ToArray();
            previousIndex = selectedIndex = 0;
            if (articulationChain.Length == 0)
            {
                Debug.LogWarning("[URDF Importer Controller] No movable joints found.");
                enabled = false;
                return;
            }

            prevColors = new Color[articulationChain.Length][];
            for (int i = 0; i < articulationChain.Length; i++)
            {
                StoreJointColors(i);
            }
            DisplaySelectedJoint(selectedIndex);
            ApplyHighlight(selectedIndex);
        }

        void SetSelectedJointIndex(int index)
        {
            if (articulationChain.Length > 0) 
            {
                selectedIndex = (index + articulationChain.Length) % articulationChain.Length;
            }
        }

        void Update()
        {
            bool SelectionInput1 = KeyDownRight();
            bool SelectionInput2 = KeyDownLeft();

            SetSelectedJointIndex(selectedIndex); // to make sure it is in the valid range
            UpdateDirection(selectedIndex);

            if (SelectionInput2)
            {
                SetSelectedJointIndex(selectedIndex - 1);
                Highlight(selectedIndex);
            }
            else if (SelectionInput1)
            {
                SetSelectedJointIndex(selectedIndex + 1);
                Highlight(selectedIndex);
            }

            // UpdateDirection stops the previously selected joint and then sets previousIndex to the new joint
            UpdateDirection(selectedIndex);
        }

        /// <summary>
        /// Highlights the color of the robot by changing the color of the part to a color set by the user in the inspector window
        /// </summary>
        /// <param name="selectedIndex">Index of the link selected in the Articulation Chain</param>
        private void Highlight(int selectedIndex)
        {
            if (selectedIndex == previousIndex || selectedIndex < 0 || selectedIndex >= articulationChain.Length) 
            {
                return;
            }

            // reset colors for the previously selected joint
            ResetJointColors(previousIndex);

            DisplaySelectedJoint(selectedIndex);
            ApplyHighlight(selectedIndex);
        }

        void DisplaySelectedJoint(int selectedIndex)
        {
            if (selectedIndex < 0 || selectedIndex >= articulationChain.Length) 
            {
                return;
            }
            selectedJoint = articulationChain[selectedIndex].name + " (" + selectedIndex + ")";
        }

        /// <summary>
        /// Sets the direction of movement of the joint on every update
        /// </summary>
        /// <param name="jointIndex">Index of the link selected in the Articulation Chain</param>
        private void UpdateDirection(int jointIndex)
        {
            if (jointIndex < 0 || jointIndex >= articulationChain.Length) 
            {
                return;
            }

            float moveDirection = VerticalInput();
            JointControl current = articulationChain[jointIndex].GetComponent<JointControl>();
            if (previousIndex != jointIndex)
            {
                JointControl previous = articulationChain[previousIndex].GetComponent<JointControl>();
                previous.direction = RotationDirection.None;
                previousIndex = jointIndex;
            }

            if (current.controltype != control) 
            {
                UpdateControlType(current);
            }

            if (moveDirection > 0)
            {
                current.direction = RotationDirection.Positive;
            }
            else if (moveDirection < 0)
            {
                current.direction = RotationDirection.Negative;
            }
            else
            {
                current.direction = RotationDirection.None;
            }
        }

        /// <summary>
        /// Renderers of a link's visual meshes ("Visuals" child; first child as a fallback)
        /// </summary>
        private Renderer[] GetTargetRenderers(int index)
        {
            Transform link = articulationChain[index].transform;
            Transform visuals = link.Find("Visuals");
            if (visuals != null)
            {
                return visuals.GetComponentsInChildren<Renderer>(true);
            }
            return link.childCount > 0 ? link.GetChild(0).GetComponentsInChildren<Renderer>(true) : new Renderer[0];
        }

        /// <summary>
        /// Stores original colors of a joint's visuals (every renderer and material slot)
        /// </summary>
        /// <param name="index">Index of the part in the Articulation chain</param>
        private void StoreJointColors(int index)
        {
            var colors = new List<Color>();
            foreach (Renderer renderer in GetTargetRenderers(index))
            {
                foreach (Material material in renderer.sharedMaterials)
                {
                    colors.Add(material != null ? MaterialExtensions.GetMaterialColor(material) : Color.white);
                }
            }
            prevColors[index] = colors.ToArray();
        }

        private void ApplyHighlight(int index)
        {
            foreach (Renderer renderer in GetTargetRenderers(index))
            {
                foreach (Material material in renderer.materials)
                {
                    MaterialExtensions.SetMaterialColor(material, highLightColor);
                }
            }
        }

        /// <summary>
        /// Resets original colors of the part being highlighted
        /// </summary>
        /// <param name="index">Index of the part in the Articulation chain</param>
        private void ResetJointColors(int index)
        {
            Color[] colors = prevColors[index];
            int k = 0;
            foreach (Renderer renderer in GetTargetRenderers(index))
            {
                foreach (Material material in renderer.materials)
                {
                    if (colors == null || k >= colors.Length)
                    {
                        return;
                    }
                    MaterialExtensions.SetMaterialColor(material, colors[k++]);
                }
            }
        }

        public void UpdateControlType(JointControl joint)
        {
            joint.controltype = control;
            if (control == ControlType.PositionControl)
            {
                ArticulationDrive drive = joint.joint.xDrive;
                drive.stiffness = stiffness;
                drive.damping = damping;
                joint.joint.xDrive = drive;
            }
        }

        // ---- input (Input System package and/or legacy Input Manager, whichever is enabled)
        private static bool KeyDownRight()
        {
#if URDF_IMPORTER_INPUT_SYSTEM && ENABLE_INPUT_SYSTEM
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb != null && kb.rightArrowKey.wasPressedThisFrame) return true;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetKeyDown(KeyCode.RightArrow)) return true;
#endif
            return false;
        }

        private static bool KeyDownLeft()
        {
#if URDF_IMPORTER_INPUT_SYSTEM && ENABLE_INPUT_SYSTEM
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb != null && kb.leftArrowKey.wasPressedThisFrame) return true;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetKeyDown(KeyCode.LeftArrow)) return true;
#endif
            return false;
        }

        // Same keys as the legacy "Vertical" axis: up/down arrows and W/S
        private static float VerticalInput()
        {
#if URDF_IMPORTER_INPUT_SYSTEM && ENABLE_INPUT_SYSTEM
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb != null)
            {
                float v = (kb.upArrowKey.isPressed || kb.wKey.isPressed ? 1f : 0f) - (kb.downArrowKey.isPressed || kb.sKey.isPressed ? 1f : 0f);
                if (v != 0f) return v;
            }
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetAxis("Vertical");
#else
            return 0f;
#endif
        }

        public void OnGUI()
        {
            GUIStyle centeredStyle = GUI.skin.GetStyle("Label");
            centeredStyle.alignment = TextAnchor.UpperCenter;
            GUI.Label(new Rect(Screen.width / 2 - 200, 10, 400, 20), "Press left/right arrow keys to select a robot joint.", centeredStyle);
            GUI.Label(new Rect(Screen.width / 2 - 200, 30, 400, 20), "Press up/down arrow keys to move " + selectedJoint + ".", centeredStyle);
        }
    }
}
