//DOCUMENTED CODE
using UnityEngine;
using UnityEngine.Animations.Rigging;

public class RealWorldRigScript : MonoBehaviour
{
    [SerializeField] private TwoBoneIKConstraint leftHandRig;
    [SerializeField] private TwoBoneIKConstraint rightHandRig;
    [SerializeField] private ChainIKConstraint rightPinky;
    [SerializeField] private ChainIKConstraint rightRingFinger;
    [SerializeField] private ChainIKConstraint rightMiddleFinger;
    [SerializeField] private ChainIKConstraint rightIndexFinger;
    [SerializeField] private ChainIKConstraint rightThumb;

    public void OnWeaponSpawn(GameObject gun)
    {   // Assigns IK targets based on the spawned gun's model.
        if (gun == null)
        {   // This should never happen, safety check in case
            Debug.LogError("OnWeaponSpawn called with null gun!");
            return;
        }

        Transform targets = gun.transform.Find("Gun Model").Find("Targets");
        if (targets == null)
        {   // Safety check in case the gun model is missing the expected hierarchy
            Debug.LogError($"Gun '{gun.name}' is missing a 'Targets' child transform!");
            return;
        }

        AssignTarget(leftHandRig, targets, "Left Hand Target");
        AssignTarget(rightHandRig, targets, "Right Hand Target");
        AssignTarget(rightPinky, targets, "Right Pinky Target");
        AssignTarget(rightRingFinger, targets, "Right Ring Target");
        AssignTarget(rightMiddleFinger, targets, "Right Middle Target");
        AssignTarget(rightIndexFinger, targets, "Right Index Target");
        AssignTarget(rightThumb, targets, "Right Thumb Target");
        // Rig is built: constraints will use the new targets for IK solving.
        var rigBuilder = transform.GetComponentInParent<RigBuilder>();
        rigBuilder.Build();

    }

    private void AssignTarget<T>(T constraint, Transform parent, string targetName) where T : MonoBehaviour
    {   // The <T> makes the method generic, so it can work with both TwoBoneIKConstraint and ChainIKConstraint without code duplication.
        if (constraint == null) return;

        Transform target = parent.Find(targetName);
        if (target == null)
        {   //Safety check in case the target is missing from the gun model's hierarchy.
            Debug.LogWarning($"Target '{targetName}' not found under '{parent.name}'!");
            return;
        }

        switch (constraint)
        {   // Switch statement is used to handle both constraint types, avoiding code duplication
            case TwoBoneIKConstraint twoBone:
                var data = twoBone.data; 
                data.target = target;
                twoBone.data = data; 
                break;
            case ChainIKConstraint chain:
                var chainData = chain.data; 
                chainData.target = target;
                chain.data = chainData; 
                break;
            default:
                Debug.LogWarning($"Constraint '{constraint.name}' is not supported!");
                break;
        }
    }
}
