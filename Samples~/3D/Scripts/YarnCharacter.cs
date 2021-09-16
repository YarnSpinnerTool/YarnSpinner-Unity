using UnityEngine;

namespace Yarn.Unity.Example
{
    /// <summary>Script for the 3D RPG sample project in YarnSpinner. DialogueRunner invokes YarnCharacterView, 
    /// which locates the YarnCharacter who is speaking. Put this script on your various NPC gameObjects.</summary>
    public class YarnCharacter : MonoBehaviour
    {
        [Tooltip("This must match the character name used in Yarn dialogue scripts.")]
        public string characterName = "MyName";

        [Tooltip("When positioning the message bubble in worldspace, YarnCharacterManager adds this additional offset to this gameObject's position. Taller characters should use taller offsets, etc.")]
        public Vector3 messageBubbleOffset = new Vector3(0f, 3f, 0f);

        [Tooltip("if true, then apply messageBubbleOffset relative to this transform's rotation and scale")]
        public bool offsetUsesRotation = false;

        public Vector3 positionWithOffset
        { 
            get {
                if (!offsetUsesRotation)
                {
                    return transform.position + messageBubbleOffset;
                }
                else
                {
                    return transform.position + transform.TransformPoint(messageBubbleOffset); // convert offset into local space
                }
            }
        }

        // Start is called before the first frame update, but AFTER Awake()
        // ... this is important because YarnCharacterManager.Awake() must run before YarnCharacter.Start()
        void Start()
        {
            if (YarnCharacterView.instance == null)
            {
                Debug.LogError("YarnCharacter can't find the YarnCharacterView instance! Is the 3D Dialogue prefab and YarnCharacterView script in the scene?");
                return;
            }

            YarnCharacterView.instance.RegisterYarnCharacter(this);
        }

        void OnDestroy()
        {
            if (YarnCharacterView.instance != null)
            {
                YarnCharacterView.instance.ForgetYarnCharacter(this);
            }
        }
    }
}
