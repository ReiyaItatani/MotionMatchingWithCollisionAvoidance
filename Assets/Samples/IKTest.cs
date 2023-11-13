using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IKTest : MonoBehaviour
{
    public Animator animator;
    public Transform target;

    void OnAnimatorIK(int layerIndex)
    {
        if (animator)
        {
            // IKを有効にする
            animator.SetIKPositionWeight(AvatarIKGoal.RightHand, 1f);
            animator.SetIKPosition(AvatarIKGoal.RightHand, target.position);

            // 必要に応じて、他のIKの目標もここで設定できます（例：回転、他の手足の位置など）
        }
    }
}
