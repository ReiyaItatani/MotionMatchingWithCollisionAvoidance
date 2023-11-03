using UnityEngine;
using System.Collections;

[System.Serializable]
public class ParameterPair
{
    public int maxRange;
    public string name;
}
public class RandomState : StateMachineBehaviour
{        
    public ParameterPair[] _randomIntParameters;

    public string _randomOffset = "RandomOffset";

    public string _mirrorParameter = "Mirror";

    public int maxRandomOffset = 2;

    protected bool init = false;

    // OnStateEnter is called before OnStateEnter is called on any state inside this state machine
    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
        if (!init)
        {
            //Random.InitState(System.DateTime.Now.Millisecond + animator.GetInstanceID() + (int)Time.realtimeSinceStartup);
            init = true;
        }

        for(int i = 0; i< _randomIntParameters.Length;i++)
            if (string.IsNullOrEmpty(_randomIntParameters[i].name) == false)
            {
                int randomInt = Random.Range(0, _randomIntParameters[i].maxRange);
                animator.SetInteger(_randomIntParameters[i].name, randomInt);
            }


        if (string.IsNullOrEmpty(_randomOffset) == false)
        {
            float randomOffset = Random.Range(0.0f, maxRandomOffset);
            animator.SetFloat(_randomOffset, randomOffset);
        }

        if (string.IsNullOrEmpty(_mirrorParameter) == false)
        {
            bool mirror = Random.Range(0, 2) % 2 == 0;
            animator.SetBool(_mirrorParameter, mirror);
        }
    }
}

