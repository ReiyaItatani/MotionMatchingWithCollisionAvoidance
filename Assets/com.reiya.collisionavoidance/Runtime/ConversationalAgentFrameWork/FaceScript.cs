using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FaceScript : MonoBehaviour {

    [HideInInspector] public bool freeze;
    [HideInInspector] public float expressFactor;

    [HideInInspector] public bool talkingNow;

    [HideInInspector] public SkinnedMeshRenderer meshRenderer;
    [HideInInspector] public SkinnedMeshRenderer meshRendererEyes;
    [HideInInspector] public SkinnedMeshRenderer meshRendererEyelashes;
    [HideInInspector] public SkinnedMeshRenderer meshRendererBeards;
    [HideInInspector] public SkinnedMeshRenderer meshRendererMoustaches;

    private int[] shapeKey_mapBeard;
    private int[] shapeKey_mapMoustache;
    private int shapeKeyCount;
    
    private float[] values;
    private float[] valuesDisp;
    [HideInInspector] public float[] targets;
    private float[] speeds;

    private bool blinkingNow;
    private float blinkTimer;
    [HideInInspector] public float blinkCloseSpeed;
    [HideInInspector] public float blinkOpenSpeed;

    private int blink_left = 23; //23
    private int blink_right = 24;  //24
    private int browsDown_left = 15;  //15
    private int browsDown_right = 16; //16
#if AvatarSDK
    private int browsIn_left = 4; //non
    private int browsIn_right = 5; //non
    private int browsOuterLower_left = 6; //17(Both Brows)
    private int browsOuterLower_right = 7; //17(Both Brows)
#elif MicroSoftRocketBox
    private int browsOuterLower = 17; //17(Both Brows)
#endif
    private int browsUp_left = 68; //68
    private int browsUp_right = 70; //70
    private int cheekPuff_left = 132; //132
    private int cheekPuff_right = 133; //133
    private int eyesWide_left = 142; //142
    private int eyesWide_right = 149; //149
    private int frown_left = 44; //44
    private int frown_right = 45; //45
    private int jawBackward = 117; //117
    private int jawForeward = 37; //37
    private int jawRotateY_left = 38; //38
    private int jawRotateY_right = 40; //40
#if AvatarSDK
    private int jawRotateZ_left = 20; //non
    private int jawRotateZ_right = 21; //non
#endif
    private int jawDown = 39; //39
    private int jawLeft = 38; //38
    private int jawRight = 40; //40
#if AvatarSDK
    private int jawUp = 25; //non
#endif
    private int lowerLipDown_left = 48; //48
    private int lowerLipDown_right = 49; //49
    private int lowerLipIn = 157; //157
    private int lowerLipOut = 160; //160
    private int midmouth_left = 47; //47
    private int midmouth_right = 53; //53
    private int mouthDown = 154; //154
#if AvatarSDK
    private int mouthNarrow_left = 33; //46(both)
    private int mouthNarrow_right = 34;//46(both)
#elif MicroSoftRocketBox
    private int mouthNarrow = 46;
#endif
    private int mouthOpen = 39; //39
#if AvatarSDK
    private int mouthUp = 36; //non
#endif
    private int mouthWhistle_left = 62; //62
    private int mouthWhistle_right = 63; //63
    private int noseScrunch_left = 64; //64
    private int noseScrunch_right = 65; //65
    private int smileLeft = 58; //58
    private int smileRight = 59; //59
    private int squint_left = 33; //33
    private int squint_right = 34; //34
    private int toungeUp = 129; //129
    private int upperLipIn = 167; //167
    private int upperLipOut = 169; //169
    private int upperLipUp_left = 171; //171
    private int upperLipUp_right = 172; //172

    [Header("Expression Parameters")]
    [HideInInspector] public float exp_happy;
    [HideInInspector] public float exp_sad;
    [HideInInspector] public float exp_angry;
    [HideInInspector] public float exp_shock;
    [HideInInspector] public float exp_disgust;
    [HideInInspector] public float exp_fear;

    private Renderer bodyRenderer;

    public bool blinkOff = false;

    void Update()
    {
        if(!blinkOff) Blink();
        ExpressionPass();

        WrinklePass();

        if(talkingNow)
        {
            VisemesPass();
        }

        if (blinkOff)
        {
         /*   targets[browsOuterLower_left] = 8;
            targets[browsOuterLower_right] = 8;
            targets[browsUp_left] = 25;
            targets[browsUp_right] = 25;
            targets[mouthOpen] = 33;
            targets[smileLeft] = 100;
            targets[smileRight] = 100;
            targets[squint_left] = 27;
            targets[squint_right] = 27;
            targets[toungeUp] = 7;
            targets[upperLipIn] = 35;
            //targets[mouthOpen] = 25;
            //targets[toungeUp] = 20;

            /*v[10] = 0.5f;
            v[13] = 0.2f; // 0.25f;
            v[14] = 0.1f; // 0.25f;
            //v[11] = 0.05f; // 0.5f;
            //v[12] = 0.02f;

            targets[cheekPuff_left] = 10 * v[1];
            targets[cheekPuff_right] = 10 * v[1];
            targets[jawBackward] = 10 * v[2];
            targets[lowerLipDown_left] = 25 * v[3]
                + 15 * v[4]
                + 15 * v[5]
                + 40 * v[6]
                + 15 * v[7]
                + 30 * v[8]
                + 5 * v[9]
                + 10 * v[11]
                + 30 * v[12];
            targets[lowerLipDown_right] = 25 * v[3]
                + 15 * v[4]
                + 15 * v[5]
                + 40 * v[6]
                + 15 * v[7]
                + 30 * v[8]
                + 5 * v[9]
                + 10 * v[11]
                + 30 * v[12];
            targets[lowerLipIn] = 100 * v[1]
                + 75 * v[2];
            targets[lowerLipOut] = 20 * v[6]
                + 20 * v[7]
                + 20 * v[11]
                + 30 * v[12]
                + 10 * v[13]
                + 30 * v[14];
            targets[midmouth_left] = 45 * v[13]
                + 70 * v[14];
            targets[midmouth_right] = 45 * v[13]
                + 70 * v[14];
            targets[mouthUp] = 10 * v[1]
                + 5 * v[2];
            targets[mouthDown] = 10 * v[3]
                + 5 * v[4]
                + 10 * v[5]
                + 5 * v[11]
                + 10 * v[12];
            targets[mouthNarrow_left] = 40 * v[2]
                + 10 * v[3]
                + 30 * v[6];
            targets[mouthNarrow_right] = 40 * v[2]
                + 10 * v[3]
                + 30 * v[6];
            targets[mouthOpen] = 15 * v[2]
                + 20 * v[3]
                + 15 * v[4]
                + 15 * v[5]
                + 10 * v[6]
                + 5 * v[7]
                + 20 * v[8]
                + 15 * v[9]
                + 50 * v[10]
                + 15 * v[11]
                + 5 * v[12]
                + 40 * v[13]
                + 15 * v[14];
            targets[mouthWhistle_left] = 50 * v[4]
                + 55 * v[5]
                + 50 * v[6]
                + 50 * v[7]
                + 20 * v[8]
                + 10 * v[9]
                + 50 * v[11]
                + 60 * v[12];
            targets[mouthWhistle_right] = 50 * v[4]
                + 55 * v[5]
                + 50 * v[6]
                + 50 * v[7]
                + 20 * v[8]
                + 10 * v[9]
                + 50 * v[11]
                + 60 * v[12];
            targets[upperLipIn] = 100 * v[1]
                + 20 * v[11]
                + 40 * v[12];
            targets[upperLipOut] = 40 * v[2]
                + 20 * v[6]
                + 10 * v[7]
                + 10 * v[13]
                + 10 * v[14];
            targets[toungeUp] = 20 * v[3]
                + 20 * v[8]
                + 10 * v[9];
            targets[upperLipUp_left] = 20 * v[6]
                + 5 * v[7]
                + 5 * v[9];
            targets[upperLipUp_right] = 20 * v[6]
                + 5 * v[7]
                + 5 * v[9];*/
        }

        StepTargets();
        SetShapeKeys();
    }

    void Start () {
        InitKeys();
        // talkingNow = false;
        blinkingNow = false;

        bodyRenderer = meshRenderer.gameObject.GetComponent<Renderer>();

        values = new float[175];
        for(int i = 0; i < 175; i++)
        {
            values[i] = 0.0f;
        }

        valuesDisp = new float[175];
        for (int i = 0; i < 175; i++)
        {
            valuesDisp[i] = 0.0f;
        }

        targets = new float[175];
        for (int i = 0; i < 175; i++)
        {
            targets[i] = 0.0f;
        }

        speeds = new float[175];
        for (int i = 0; i < 175; i++)
        {
            speeds[i] = 3.0f;
        }

        blinkCloseSpeed = 12f;
        blinkOpenSpeed = 8f;

        expressFactor = 1f;
        blink_max = 4f;
        blink_min = 2f;
    }

    float rand_factor = 3f;
    float rand_min = -10f;
    float rand_max = 10f;

    void InitKeys(){
    #if AvatarSDK
        blink_left = 0; // Blink_Left
        blink_right = 1;  // Blink_Right
        browsDown_left = 2;  // BrowsDown_Left
        browsDown_right = 3; // BrowsDown_Right
        browsIn_left = 4; // BrowsIn_Left
        browsIn_right = 5; // BrowsIn_Right
        browsOuterLower_left = 6; // BrowsOuterLower_Left
        browsOuterLower_right = 7; // BrowsOuterLower_Right
        browsUp_left = 8; // BrowsUp_Left
        browsUp_right = 9; // BrowsUp_Right
        cheekPuff_left = 10; // CheekPuff_Left
        cheekPuff_right = 11; // CheekPuff_Right
        eyesWide_left = 12; // EyesWide_Left
        eyesWide_right = 13; // EyesWide_Right
        frown_left = 14; // Frown_Left
        frown_right = 15; // Frown_Right
        jawBackward = 16; // JawBackward
        jawForeward = 17; // JawForeward
        jawRotateY_left = 18; // JawRotateY_Left
        jawRotateY_right = 19; // JawRotateY_Right
        jawRotateZ_left = 20; // JawRotateZ_Left
        jawRotateZ_right = 21; // JawRotateZ_Right
        jawDown = 22; // Jaw_Down
        jawLeft = 23; // Jaw_Left
        jawRight = 24; // Jaw_Right
        jawUp = 25; // Jaw_Up
        lowerLipDown_left = 26; // LowerLipDown_Left
        lowerLipDown_right = 27; // LowerLipDown_Right
        lowerLipIn = 28; // LowerLipIn
        lowerLipOut = 29; // LowerLipOut
        midmouth_left = 30; // Midmouth_Left
        midmouth_right = 31; // Midmouth_Right
        mouthDown = 32; // MouthDown
        mouthNarrow_left = 33; // MouthNarrow_Left
        mouthNarrow_right = 34; // MouthNarrow_Right
        mouthOpen = 35; // MouthOpen
        mouthUp = 36; // MouthUp
        mouthWhistle_left = 37; // MouthWhistle_NarrowAdjust_Left
        mouthWhistle_right = 38; // MouthWhistle_NarrowAdjust_Right
        noseScrunch_left = 39; // NoseScrunch_Left
        noseScrunch_right = 40; // NoseScrunch_Right
        smileLeft = 41; // Smile_Left
        smileRight = 42; // Smile_Right
        squint_left = 43; // Squint_Left
        squint_right = 44; // Squint_Right
        toungeUp = 45; // TongueUp
        upperLipIn = 46; // UpperLipIn
        upperLipOut = 47; // UpperLipOut
        upperLipUp_left = 48; // UpperLipUp_Left
        upperLipUp_right = 49; // UpperLipUp_Right
    #endif
    }

    void RandomMimicks()
    {
        valuesDisp[blink_left] = Mathf.Clamp(valuesDisp[blink_left]  + (Random.value - 0.5f) * rand_factor,rand_min,rand_max);
        valuesDisp[blink_right] = Mathf.Clamp(valuesDisp[blink_right]  + (Random.value - 0.5f) * rand_factor, rand_min, rand_max);
        valuesDisp[eyesWide_left] = Mathf.Clamp(valuesDisp[eyesWide_left] + (Random.value - 0.5f) * rand_factor, rand_min, rand_max);
        valuesDisp[eyesWide_right] = Mathf.Clamp(valuesDisp[eyesWide_right] + (Random.value - 0.5f) * rand_factor, rand_min, rand_max);

        valuesDisp[browsDown_left] = Mathf.Clamp(valuesDisp[browsDown_left] + (Random.value - 0.5f) * rand_factor, rand_min, rand_max);
        valuesDisp[browsDown_right] = Mathf.Clamp(valuesDisp[browsDown_right] + (Random.value - 0.5f) * rand_factor, rand_min, rand_max);

        valuesDisp[browsUp_left] = Mathf.Clamp(valuesDisp[browsUp_left] + (Random.value - 0.5f) * rand_factor, rand_min, rand_max);
        valuesDisp[browsUp_right] = Mathf.Clamp(valuesDisp[browsUp_right] + (Random.value - 0.5f) * rand_factor, rand_min, rand_max);

        valuesDisp[smileLeft] = Mathf.Clamp(valuesDisp[smileLeft] + (Random.value - 0.5f) * rand_factor, rand_min, rand_max);
        valuesDisp[smileRight] = Mathf.Clamp(valuesDisp[smileRight] + (Random.value - 0.5f) * rand_factor, rand_min, rand_max);
    }

    private int tmpKey1, tmpKey2;

    public void InitShapeKeys()
    {
        shapeKeyCount = meshRenderer.sharedMesh.blendShapeCount;
        Dictionary<string, int> shapeKeyDict_body = new Dictionary<string, int>();
        for (int i = 0; i < shapeKeyCount; i++)
        {
            shapeKeyDict_body.Add(meshRenderer.sharedMesh.GetBlendShapeName(i), i);
        }

        if (meshRendererMoustaches != null)
        {
            int scMoustaches = meshRendererMoustaches.sharedMesh.blendShapeCount;
            Dictionary<string, int> shapeKeyDict_moustaches = new Dictionary<string, int>();
            shapeKey_mapMoustache = new int[shapeKeyCount];
            for (int i = 0; i < shapeKeyCount; i++)
            {
                shapeKey_mapMoustache[i] = -1;
            }
            for (int i = 0; i < scMoustaches; i++)
            {
                shapeKeyDict_moustaches.Add(meshRendererMoustaches.sharedMesh.GetBlendShapeName(i), i);
            }
            for (int i = 0; i < scMoustaches; i++)
            {
                tmpKey1 = -1;
                tmpKey2 = -1;
                shapeKeyDict_body.TryGetValue(meshRenderer.sharedMesh.GetBlendShapeName(i), out tmpKey1);
                shapeKeyDict_moustaches.TryGetValue(meshRenderer.sharedMesh.GetBlendShapeName(i), out tmpKey2);
                if (tmpKey1 != -1 && tmpKey2 != -1)
                {
                    shapeKey_mapMoustache[tmpKey1] = tmpKey2;
                }
            }
            shapeKeyDict_moustaches.Clear();
        }

        if (meshRendererBeards != null)
        {
            int scBeards = meshRendererBeards.sharedMesh.blendShapeCount;
            Dictionary<string, int> shapeKeyDict_beards = new Dictionary<string, int>();
            shapeKey_mapBeard = new int[shapeKeyCount];
            for (int i = 0; i < shapeKeyCount; i++)
            {
                shapeKey_mapBeard[i] = -1;
            }
            for (int i = 0; i < scBeards; i++)
            {
                shapeKeyDict_beards.Add(meshRendererBeards.sharedMesh.GetBlendShapeName(i), i);
            }
            for (int i = 0; i < scBeards; i++)
            {
                tmpKey1 = -1;
                tmpKey2 = -1;
                shapeKeyDict_body.TryGetValue(meshRenderer.sharedMesh.GetBlendShapeName(i), out tmpKey1);
                shapeKeyDict_beards.TryGetValue(meshRenderer.sharedMesh.GetBlendShapeName(i), out tmpKey2);
                if (tmpKey1 != -1 && tmpKey2 != -1)
                {
                    shapeKey_mapBeard[tmpKey1] = tmpKey2;
                }
            }
            shapeKeyDict_beards.Clear();
        }
        shapeKeyDict_body.Clear();
    }

    void StepTargets()
    {
        for (int i = 0; i < 175; i++)
        {
            if( Mathf.Abs( values[i] - targets[i] ) <= speeds[i])
            {
                values[i] = targets[i];
            } else
            {
                values[i] -= (speeds[i] * Mathf.Sign(values[i] - targets[i]));
            } 
        }

        if (Mathf.Abs(w_happy - exp_happy) <= 3)
        {
            w_happy = exp_happy;
        }
        else
        {
            w_happy -= (3 * Mathf.Sign(w_happy - exp_happy));
        }

        if (Mathf.Abs(w_angry - exp_angry) <= 3)
        {
            w_angry = exp_angry;
        }
        else
        {
            w_angry -= (3 * Mathf.Sign(w_angry - exp_angry));
        }

        if (Mathf.Abs(w_shock - exp_shock) <= 3)
        {
            w_shock = exp_shock;
        }
        else
        {
            w_shock -= (3 * Mathf.Sign(w_shock - exp_shock));
        }

        if (Mathf.Abs(w_sad - exp_sad) <= 3)
        {
            w_sad = exp_sad;
        }
        else
        {
            w_sad -= (3 * Mathf.Sign(w_sad - exp_sad));
        }
    }

    private void SetShapeKeys()
    {
            meshRenderer.SetBlendShapeWeight(blink_left, values[blink_left] + valuesDisp[blink_left]);
            meshRenderer.SetBlendShapeWeight(blink_right, values[blink_right] + valuesDisp[blink_right]);
            meshRenderer.SetBlendShapeWeight(browsDown_left, values[browsDown_left] + valuesDisp[browsDown_left]);
            meshRenderer.SetBlendShapeWeight(browsDown_right, values[browsDown_right] + valuesDisp[browsDown_right]);
#if AvatarSDK
            meshRenderer.SetBlendShapeWeight(browsIn_left, values[browsIn_left] + valuesDisp[browsIn_left]);
            meshRenderer.SetBlendShapeWeight(browsIn_right, values[browsIn_right] + valuesDisp[browsIn_right]);
            meshRenderer.SetBlendShapeWeight(browsOuterLower_left, values[browsOuterLower_left] + valuesDisp[browsOuterLower_left]);
            meshRenderer.SetBlendShapeWeight(browsOuterLower_right, values[browsOuterLower_right] + valuesDisp[browsOuterLower_right]);
#elif MicroSoftRocketBox
            meshRenderer.SetBlendShapeWeight(browsOuterLower, values[browsOuterLower] + valuesDisp[browsOuterLower]);
#endif
            meshRenderer.SetBlendShapeWeight(browsUp_left, values[browsUp_left] + valuesDisp[browsUp_left]);
            meshRenderer.SetBlendShapeWeight(browsUp_right, values[browsUp_right] + valuesDisp[browsUp_right]);
            meshRenderer.SetBlendShapeWeight(cheekPuff_left, values[cheekPuff_left] + valuesDisp[cheekPuff_left]);
            meshRenderer.SetBlendShapeWeight(cheekPuff_right, values[cheekPuff_right] + valuesDisp[cheekPuff_right]);
            meshRenderer.SetBlendShapeWeight(eyesWide_left, values[eyesWide_left] + valuesDisp[eyesWide_left]);
            meshRenderer.SetBlendShapeWeight(eyesWide_right, values[eyesWide_right] + valuesDisp[eyesWide_right]);
            meshRenderer.SetBlendShapeWeight(frown_left, values[frown_left] + valuesDisp[frown_left]);
            meshRenderer.SetBlendShapeWeight(frown_right, values[frown_right] + valuesDisp[frown_right]);
            meshRenderer.SetBlendShapeWeight(jawBackward, values[jawBackward] + valuesDisp[jawBackward]);
            meshRenderer.SetBlendShapeWeight(jawForeward, values[jawForeward] + valuesDisp[jawForeward]);
            meshRenderer.SetBlendShapeWeight(jawRotateY_left, values[jawRotateY_left] + valuesDisp[jawRotateY_left]);
            meshRenderer.SetBlendShapeWeight(jawRotateY_right, values[jawRotateY_right] + valuesDisp[jawRotateY_right]);
#if AvatarSDK
                meshRenderer.SetBlendShapeWeight(jawRotateZ_left, values[jawRotateZ_left] + valuesDisp[jawRotateZ_left]);
                meshRenderer.SetBlendShapeWeight(jawRotateZ_right, values[jawRotateZ_right] + valuesDisp[jawRotateZ_right]);
#endif
            meshRenderer.SetBlendShapeWeight(jawDown, values[jawDown] + valuesDisp[jawDown]);
            meshRenderer.SetBlendShapeWeight(jawLeft, values[jawLeft] + valuesDisp[jawLeft]);
            meshRenderer.SetBlendShapeWeight(jawRight, values[jawRight] + valuesDisp[jawRight]);
#if AvatarSDK
                meshRenderer.SetBlendShapeWeight(jawUp, values[jawUp] + valuesDisp[jawUp]);
#endif
            meshRenderer.SetBlendShapeWeight(lowerLipDown_left, values[lowerLipDown_left] + valuesDisp[lowerLipDown_left]);
            meshRenderer.SetBlendShapeWeight(lowerLipDown_right, values[lowerLipDown_right] + valuesDisp[lowerLipDown_right]);
            meshRenderer.SetBlendShapeWeight(lowerLipIn, values[lowerLipIn] + valuesDisp[lowerLipIn]);
            meshRenderer.SetBlendShapeWeight(lowerLipOut, values[lowerLipOut] + valuesDisp[lowerLipOut]);
            meshRenderer.SetBlendShapeWeight(midmouth_left, values[midmouth_left] + valuesDisp[midmouth_left]);
            meshRenderer.SetBlendShapeWeight(midmouth_right, values[midmouth_right] + valuesDisp[midmouth_right]);
            meshRenderer.SetBlendShapeWeight(mouthDown, values[mouthDown] + valuesDisp[mouthDown]);
#if AvatarSDK
                meshRenderer.SetBlendShapeWeight(mouthNarrow_left, values[mouthNarrow_left] + valuesDisp[mouthNarrow_left]);
                meshRenderer.SetBlendShapeWeight(mouthNarrow_right, values[mouthNarrow_right] + valuesDisp[mouthNarrow_right]);
#elif MicroSoftRocketBox
                meshRenderer.SetBlendShapeWeight(mouthNarrow, values[mouthNarrow] + valuesDisp[mouthNarrow]);
#endif
            meshRenderer.SetBlendShapeWeight(mouthOpen, values[mouthOpen] + valuesDisp[mouthOpen]);
#if AvatarSDK
                meshRenderer.SetBlendShapeWeight(mouthUp, values[mouthUp] + valuesDisp[mouthUp]);
#endif
            meshRenderer.SetBlendShapeWeight(mouthWhistle_left, values[mouthWhistle_left] + valuesDisp[mouthWhistle_left]);
            meshRenderer.SetBlendShapeWeight(mouthWhistle_right, values[mouthWhistle_right] + valuesDisp[mouthWhistle_right]);
            meshRenderer.SetBlendShapeWeight(noseScrunch_left, values[noseScrunch_left] + valuesDisp[noseScrunch_left]);
            meshRenderer.SetBlendShapeWeight(noseScrunch_right, values[noseScrunch_right] + valuesDisp[noseScrunch_right]);
            meshRenderer.SetBlendShapeWeight(smileLeft, values[smileLeft] + valuesDisp[smileLeft]);
            meshRenderer.SetBlendShapeWeight(smileRight, values[smileRight] + valuesDisp[smileRight]);
            meshRenderer.SetBlendShapeWeight(squint_left, values[squint_left] + valuesDisp[squint_left]);
            meshRenderer.SetBlendShapeWeight(squint_right, values[squint_right] + valuesDisp[squint_right]);
            meshRenderer.SetBlendShapeWeight(toungeUp, values[toungeUp] + valuesDisp[toungeUp]);
            meshRenderer.SetBlendShapeWeight(upperLipIn, values[upperLipIn] + valuesDisp[upperLipIn]);
            meshRenderer.SetBlendShapeWeight(upperLipOut, values[upperLipOut] + valuesDisp[upperLipOut]);
            meshRenderer.SetBlendShapeWeight(upperLipUp_left, values[upperLipUp_left] + valuesDisp[upperLipUp_left]);
            meshRenderer.SetBlendShapeWeight(upperLipUp_right, values[upperLipUp_right] + valuesDisp[upperLipUp_right]);

        if(meshRendererEyelashes != null)
        {
            meshRendererEyelashes.SetBlendShapeWeight(blink_left, values[blink_left] + valuesDisp[blink_left]);
            meshRendererEyelashes.SetBlendShapeWeight(blink_right, values[blink_right] + valuesDisp[blink_right]);
            meshRendererEyelashes.SetBlendShapeWeight(browsDown_left, values[browsDown_left] + valuesDisp[browsDown_left]);
            meshRendererEyelashes.SetBlendShapeWeight(browsDown_right, values[browsDown_right] + valuesDisp[browsDown_right]);
#if AvatarSDK
            meshRendererEyelashes.SetBlendShapeWeight(browsIn_left, values[browsIn_left] + valuesDisp[browsIn_left]);
            meshRendererEyelashes.SetBlendShapeWeight(browsIn_right, values[browsIn_right] + valuesDisp[browsIn_right]);
            meshRendererEyelashes.SetBlendShapeWeight(browsOuterLower_left, values[browsOuterLower_left] + valuesDisp[browsOuterLower_left]);
            meshRendererEyelashes.SetBlendShapeWeight(browsOuterLower_right, values[browsOuterLower_right] + valuesDisp[browsOuterLower_right]);
#elif MicroSoftRocketBox
            meshRendererEyelashes.SetBlendShapeWeight(browsOuterLower, values[browsOuterLower] + valuesDisp[browsOuterLower]);
#endif
            meshRendererEyelashes.SetBlendShapeWeight(browsUp_left, values[browsUp_left] + valuesDisp[browsUp_left]);
            meshRendererEyelashes.SetBlendShapeWeight(browsUp_right, values[browsUp_right] + valuesDisp[browsUp_right]);
            meshRendererEyelashes.SetBlendShapeWeight(cheekPuff_left, values[cheekPuff_left] + valuesDisp[cheekPuff_left]);
            meshRendererEyelashes.SetBlendShapeWeight(cheekPuff_right, values[cheekPuff_right] + valuesDisp[cheekPuff_right]);
            meshRendererEyelashes.SetBlendShapeWeight(eyesWide_left, values[eyesWide_left] + valuesDisp[eyesWide_left]);
            meshRendererEyelashes.SetBlendShapeWeight(eyesWide_right, values[eyesWide_right] + valuesDisp[eyesWide_right]);
            meshRendererEyelashes.SetBlendShapeWeight(frown_left, values[frown_left] + valuesDisp[frown_left]);
            meshRendererEyelashes.SetBlendShapeWeight(frown_right, values[frown_right] + valuesDisp[frown_right]);
            meshRendererEyelashes.SetBlendShapeWeight(jawBackward, values[jawBackward] + valuesDisp[jawBackward]);
            meshRendererEyelashes.SetBlendShapeWeight(jawForeward, values[jawForeward] + valuesDisp[jawForeward]);
            meshRendererEyelashes.SetBlendShapeWeight(jawRotateY_left, values[jawRotateY_left] + valuesDisp[jawRotateY_left]);
            meshRendererEyelashes.SetBlendShapeWeight(jawRotateY_right, values[jawRotateY_right] + valuesDisp[jawRotateY_right]);
            # if AvatarSDK
                meshRendererEyelashes.SetBlendShapeWeight(jawRotateZ_left, values[jawRotateZ_left] + valuesDisp[jawRotateZ_left]);
                meshRendererEyelashes.SetBlendShapeWeight(jawRotateZ_right, values[jawRotateZ_right] + valuesDisp[jawRotateZ_right]);  
            #endif
            meshRendererEyelashes.SetBlendShapeWeight(jawDown, values[jawDown] + valuesDisp[jawDown]);
            meshRendererEyelashes.SetBlendShapeWeight(jawLeft, values[jawLeft] + valuesDisp[jawLeft]);
            meshRendererEyelashes.SetBlendShapeWeight(jawRight, values[jawRight] + valuesDisp[jawRight]);
            # if AvatarSDK
                meshRendererEyelashes.SetBlendShapeWeight(jawUp, values[jawUp] + valuesDisp[jawUp]);
            #endif
            meshRendererEyelashes.SetBlendShapeWeight(lowerLipDown_left, values[lowerLipDown_left] + valuesDisp[lowerLipDown_left]);
            meshRendererEyelashes.SetBlendShapeWeight(lowerLipDown_right, values[lowerLipDown_right] + valuesDisp[lowerLipDown_right]);
            meshRendererEyelashes.SetBlendShapeWeight(lowerLipIn, values[lowerLipIn] + valuesDisp[lowerLipIn]);
            meshRendererEyelashes.SetBlendShapeWeight(lowerLipOut, values[lowerLipOut] + valuesDisp[lowerLipOut]);
            meshRendererEyelashes.SetBlendShapeWeight(midmouth_left, values[midmouth_left] + valuesDisp[midmouth_left]);
            meshRendererEyelashes.SetBlendShapeWeight(midmouth_right, values[midmouth_right] + valuesDisp[midmouth_right]);
            meshRendererEyelashes.SetBlendShapeWeight(mouthDown, values[mouthDown] + valuesDisp[mouthDown]);
#if AvatarSDK
            meshRendererEyelashes.SetBlendShapeWeight(mouthNarrow_left, values[mouthNarrow_left] + valuesDisp[mouthNarrow_left]);
            meshRendererEyelashes.SetBlendShapeWeight(mouthNarrow_right, values[mouthNarrow_right] + valuesDisp[mouthNarrow_right]);
#elif MicroSoftRocketBox
            meshRendererEyelashes.SetBlendShapeWeight(mouthNarrow, values[mouthNarrow] + valuesDisp[mouthNarrow]);
#endif
            meshRendererEyelashes.SetBlendShapeWeight(mouthOpen, values[mouthOpen] + valuesDisp[mouthOpen]);
#if AvatarSDK
            meshRendererEyelashes.SetBlendShapeWeight(mouthUp, values[mouthUp] + valuesDisp[mouthUp]);
#endif
            meshRendererEyelashes.SetBlendShapeWeight(mouthWhistle_left, values[mouthWhistle_left] + valuesDisp[mouthWhistle_left]);
            meshRendererEyelashes.SetBlendShapeWeight(mouthWhistle_right, values[mouthWhistle_right] + valuesDisp[mouthWhistle_right]);
            meshRendererEyelashes.SetBlendShapeWeight(noseScrunch_left, values[noseScrunch_left] + valuesDisp[noseScrunch_left]);
            meshRendererEyelashes.SetBlendShapeWeight(noseScrunch_right, values[noseScrunch_right] + valuesDisp[noseScrunch_right]);
            meshRendererEyelashes.SetBlendShapeWeight(smileLeft, values[smileLeft] + valuesDisp[smileLeft]);
            meshRendererEyelashes.SetBlendShapeWeight(smileRight, values[smileRight] + valuesDisp[smileRight]);
            meshRendererEyelashes.SetBlendShapeWeight(squint_left, values[squint_left] + valuesDisp[squint_left]);
            meshRendererEyelashes.SetBlendShapeWeight(squint_right, values[squint_right] + valuesDisp[squint_right]);
            meshRendererEyelashes.SetBlendShapeWeight(toungeUp, values[toungeUp] + valuesDisp[toungeUp]);
            meshRendererEyelashes.SetBlendShapeWeight(upperLipIn, values[upperLipIn] + valuesDisp[upperLipIn]);
            meshRendererEyelashes.SetBlendShapeWeight(upperLipOut, values[upperLipOut] + valuesDisp[upperLipOut]);
            meshRendererEyelashes.SetBlendShapeWeight(upperLipUp_left, values[upperLipUp_left] + valuesDisp[upperLipUp_left]);
            meshRendererEyelashes.SetBlendShapeWeight(upperLipUp_right, values[upperLipUp_right] + valuesDisp[upperLipUp_right]);
        }

        if (meshRendererEyes != null)
        {
            meshRendererEyes.SetBlendShapeWeight(blink_left, values[blink_left] + valuesDisp[blink_left]);
            meshRendererEyes.SetBlendShapeWeight(blink_right, values[blink_right] + valuesDisp[blink_right]);
            meshRendererEyes.SetBlendShapeWeight(browsDown_left, values[browsDown_left] + valuesDisp[browsDown_left]);
            meshRendererEyes.SetBlendShapeWeight(browsDown_right, values[browsDown_right] + valuesDisp[browsDown_right]);
#if AvatarSDK
            meshRendererEyes.SetBlendShapeWeight(browsIn_left, values[browsIn_left] + valuesDisp[browsIn_left]);
            meshRendererEyes.SetBlendShapeWeight(browsIn_right, values[browsIn_right] + valuesDisp[browsIn_right]);
            meshRendererEyes.SetBlendShapeWeight(browsOuterLower_left, values[browsOuterLower_left] + valuesDisp[browsOuterLower_left]);
            meshRendererEyes.SetBlendShapeWeight(browsOuterLower_right, values[browsOuterLower_right] + valuesDisp[browsOuterLower_right]);
#elif MicroSoftRocketBox
            meshRendererEyes.SetBlendShapeWeight(browsOuterLower, values[browsOuterLower] + valuesDisp[browsOuterLower]);
#endif
            meshRendererEyes.SetBlendShapeWeight(browsUp_left, values[browsUp_left] + valuesDisp[browsUp_left]);
            meshRendererEyes.SetBlendShapeWeight(browsUp_right, values[browsUp_right] + valuesDisp[browsUp_right]);
            meshRendererEyes.SetBlendShapeWeight(cheekPuff_left, values[cheekPuff_left] + valuesDisp[cheekPuff_left]);
            meshRendererEyes.SetBlendShapeWeight(cheekPuff_right, values[cheekPuff_right] + valuesDisp[cheekPuff_right]);
            meshRendererEyes.SetBlendShapeWeight(eyesWide_left, values[eyesWide_left] + valuesDisp[eyesWide_left]);
            meshRendererEyes.SetBlendShapeWeight(eyesWide_right, values[eyesWide_right] + valuesDisp[eyesWide_right]);
            meshRendererEyes.SetBlendShapeWeight(frown_left, values[frown_left] + valuesDisp[frown_left]);
            meshRendererEyes.SetBlendShapeWeight(frown_right, values[frown_right] + valuesDisp[frown_right]);
            meshRendererEyes.SetBlendShapeWeight(jawBackward, values[jawBackward] + valuesDisp[jawBackward]);
            meshRendererEyes.SetBlendShapeWeight(jawForeward, values[jawForeward] + valuesDisp[jawForeward]);
            meshRendererEyes.SetBlendShapeWeight(jawRotateY_left, values[jawRotateY_left] + valuesDisp[jawRotateY_left]);
            meshRendererEyes.SetBlendShapeWeight(jawRotateY_right, values[jawRotateY_right] + valuesDisp[jawRotateY_right]);
#if AvatarSDK
                meshRendererEyes.SetBlendShapeWeight(jawRotateZ_left, values[jawRotateZ_left] + valuesDisp[jawRotateZ_left]);
                meshRendererEyes.SetBlendShapeWeight(jawRotateZ_right, values[jawRotateZ_right] + valuesDisp[jawRotateZ_right]);
#endif
            meshRendererEyes.SetBlendShapeWeight(jawDown, values[jawDown] + valuesDisp[jawDown]);
            meshRendererEyes.SetBlendShapeWeight(jawLeft, values[jawLeft] + valuesDisp[jawLeft]);
            meshRendererEyes.SetBlendShapeWeight(jawRight, values[jawRight] + valuesDisp[jawRight]);
#if AvatarSDK
                meshRendererEyes.SetBlendShapeWeight(jawUp, values[jawUp] + valuesDisp[jawUp]);
#endif
            meshRendererEyes.SetBlendShapeWeight(lowerLipDown_left, values[lowerLipDown_left] + valuesDisp[lowerLipDown_left]);
            meshRendererEyes.SetBlendShapeWeight(lowerLipDown_right, values[lowerLipDown_right] + valuesDisp[lowerLipDown_right]);
            meshRendererEyes.SetBlendShapeWeight(lowerLipIn, values[lowerLipIn] + valuesDisp[lowerLipIn]);
            meshRendererEyes.SetBlendShapeWeight(lowerLipOut, values[lowerLipOut] + valuesDisp[lowerLipOut]);
            meshRendererEyes.SetBlendShapeWeight(midmouth_left, values[midmouth_left] + valuesDisp[midmouth_left]);
            meshRendererEyes.SetBlendShapeWeight(midmouth_right, values[midmouth_right] + valuesDisp[midmouth_right]);
            meshRendererEyes.SetBlendShapeWeight(mouthDown, values[mouthDown] + valuesDisp[mouthDown]);
#if AvatarSDK
                meshRendererEyes.SetBlendShapeWeight(mouthNarrow_left, values[mouthNarrow_left] + valuesDisp[mouthNarrow_left]);
                meshRendererEyes.SetBlendShapeWeight(mouthNarrow_right, values[mouthNarrow_right] + valuesDisp[mouthNarrow_right]);
#elif MicroSoftRocketBox
                meshRendererEyes.SetBlendShapeWeight(mouthNarrow, values[mouthNarrow] + valuesDisp[mouthNarrow]);
#endif
            meshRendererEyes.SetBlendShapeWeight(mouthOpen, values[mouthOpen] + valuesDisp[mouthOpen]);
#if AvatarSDK
                meshRendererEyes.SetBlendShapeWeight(mouthUp, values[mouthUp] + valuesDisp[mouthUp]); 
#endif
            meshRendererEyes.SetBlendShapeWeight(mouthWhistle_left, values[mouthWhistle_left] + valuesDisp[mouthWhistle_left]);
            meshRendererEyes.SetBlendShapeWeight(mouthWhistle_right, values[mouthWhistle_right] + valuesDisp[mouthWhistle_right]);
            meshRendererEyes.SetBlendShapeWeight(noseScrunch_left, values[noseScrunch_left] + valuesDisp[noseScrunch_left]);
            meshRendererEyes.SetBlendShapeWeight(noseScrunch_right, values[noseScrunch_right] + valuesDisp[noseScrunch_right]);
            meshRendererEyes.SetBlendShapeWeight(smileLeft, values[smileLeft] + valuesDisp[smileLeft]);
            meshRendererEyes.SetBlendShapeWeight(smileRight, values[smileRight] + valuesDisp[smileRight]);
            meshRendererEyes.SetBlendShapeWeight(squint_left, values[squint_left] + valuesDisp[squint_left]);
            meshRendererEyes.SetBlendShapeWeight(squint_right, values[squint_right] + valuesDisp[squint_right]);
            meshRendererEyes.SetBlendShapeWeight(toungeUp, values[toungeUp] + valuesDisp[toungeUp]);
            meshRendererEyes.SetBlendShapeWeight(upperLipIn, values[upperLipIn] + valuesDisp[upperLipIn]);
            meshRendererEyes.SetBlendShapeWeight(upperLipOut, values[upperLipOut] + valuesDisp[upperLipOut]);
            meshRendererEyes.SetBlendShapeWeight(upperLipUp_left, values[upperLipUp_left] + valuesDisp[upperLipUp_left]);
            meshRendererEyes.SetBlendShapeWeight(upperLipUp_right, values[upperLipUp_right] + valuesDisp[upperLipUp_right]);
        }

        if (meshRendererBeards != null)
        {
            for(int i = 0; i < shapeKeyCount; i++)
            {
                tmpKey1 = shapeKey_mapBeard[i];
                if(tmpKey1 != -1)
                {
                    meshRendererBeards.SetBlendShapeWeight(tmpKey1, values[i] + valuesDisp[i]);
                }
            }
        }

        if (meshRendererMoustaches != null)
        {
            for (int i = 0; i < shapeKeyCount; i++)
            {
                tmpKey1 = shapeKey_mapMoustache[i];
                if (tmpKey1 != -1)
                {
                    meshRendererMoustaches.SetBlendShapeWeight(tmpKey1, values[i] + valuesDisp[i]);
                }
            }
        }
    }

    private void ExpressionPass()
    {
        targets[browsDown_left] = (exp_angry + exp_disgust * 0.5f) * expressFactor;
        targets[browsDown_right] = targets[browsDown_left];
        targets[cheekPuff_left] = exp_angry * 0.02f * expressFactor;
        targets[cheekPuff_right] = targets[cheekPuff_left];
        targets[frown_left] = (exp_angry * 0.4f + exp_sad * 0.85f + exp_disgust * 0.1f + exp_fear * 0.6f) * expressFactor;
        targets[frown_right] = targets[frown_left];
        targets[mouthDown] = (exp_angry * 0.1f + exp_sad * 0.1f + exp_fear * 0.05f) * expressFactor;
#if AvatarSDK
        targets[mouthNarrow_left] = (exp_angry * 0.2f + exp_shock * 0.2f + exp_fear * 0.5f ) * expressFactor;
        targets[mouthNarrow_right] = targets[mouthNarrow_left];
#elif MicroSoftRocketBox
        targets[mouthNarrow] = (exp_angry * 0.2f + exp_shock * 0.2f + exp_fear * 0.5f ) * expressFactor;
#endif

        targets[squint_left] = (exp_angry * 0.3f + exp_sad * 0.1f + exp_happy * 0.4f + exp_disgust * 0.3f) * expressFactor;
        targets[squint_right] = targets[squint_left];
        
        targets[browsUp_left] = (exp_happy * 0.3f + exp_shock + exp_fear * 0.9f + exp_sad * 0.05f) * expressFactor;
        targets[browsUp_right] = targets[browsUp_left];
        targets[eyesWide_left] = (exp_shock * 0.8f + exp_fear * 0.9f) * expressFactor;
        targets[eyesWide_right] = targets[eyesWide_left];
        targets[mouthOpen] = (exp_happy * 0.12f + exp_shock * 0.3f + exp_fear * 0.5f) * expressFactor;
#if AvatarSDK
        targets[upperLipIn] = exp_happy * 0.02f * expressFactor;
        targets[lowerLipIn] = exp_happy * 0.02f * expressFactor;
#endif
        targets[smileLeft] = exp_happy * 0.95f * expressFactor;
        targets[smileRight] = targets[smileLeft];
        
#if AvatarSDK
        targets[browsOuterLower_left] = (exp_sad * 0.95f + exp_shock * 0.5f) * expressFactor;
        targets[browsOuterLower_right] = targets[browsOuterLower_left];
#elif MicroSoftRocketBox
        targets[browsOuterLower] = (exp_sad * 0.95f + exp_shock * 0.5f) * expressFactor;
#endif

#if AvatarSDK
        targets[browsIn_left] = (exp_sad * 0.05f + exp_fear * 0.8f) * expressFactor;
        targets[browsIn_right] = targets[browsIn_left];
#endif
        targets[jawBackward] = exp_sad * 0.05f * expressFactor;
        
        targets[jawDown] = exp_disgust * 0.2f * expressFactor;
        targets[noseScrunch_left] = exp_disgust * 0.85f * expressFactor;
        targets[noseScrunch_right] = targets[noseScrunch_left];
#if AvatarSDK
        targets[mouthUp] = exp_disgust * 0.05f * expressFactor;
#endif
        targets[jawForeward] = exp_disgust * 0.1f * expressFactor;
        targets[upperLipOut] = exp_disgust * 0.45f * expressFactor;
        targets[lowerLipIn] = exp_disgust * 0.15f * expressFactor;
        targets[mouthWhistle_left] = (exp_disgust * 0.1f) * expressFactor;
        targets[mouthWhistle_right] = targets[mouthWhistle_left];
        targets[midmouth_left] = exp_disgust * 0.15f * expressFactor;
        targets[midmouth_right] = targets[midmouth_left];

        targets[lowerLipDown_left] = exp_fear * 0.3f * expressFactor;
        targets[lowerLipDown_right] = targets[lowerLipDown_left];
    }

    private float w_happy;
    private float w_angry;
    private float w_shock;
    private float w_sad;

    private void WrinklePass()
    {
        bodyRenderer.material.SetFloat("_NF1", w_happy / 120f); // factor_happy
        bodyRenderer.material.SetFloat("_NF2", w_angry / 110f); // factor_angry
        bodyRenderer.material.SetFloat("_NF3", w_shock / 100f); // c_factor_shock - values[browsUp_left]
        bodyRenderer.material.SetFloat("_NF4", w_sad / 100f); // c_factor_sad

        bodyRenderer.material.SetFloat("_BF1", w_happy / 260f); // cheeck happy
        bodyRenderer.material.SetFloat("_BF2", w_angry / 140f); // cheeck angry
    }

    public void StartTalking()
    {
        talkingNow = true;
    }

    public void StopTalking()
    {
        talkingNow = false;
    }

    public void SetTargetsImmediate()
    {
        for (int i = 0; i < 175; i++)
        {
            values[i] = targets[i];
        }
        w_angry = exp_angry;
        w_happy = exp_happy;
        w_sad = exp_sad;
        w_shock = exp_shock;
    }

    [HideInInspector] public float blink_max;
    [HideInInspector] public float blink_min;
    private float blink_max_pre;

    void Blink()
    {
        if(blink_max != blink_max_pre)
        {
            // blinkTimer = 0f;
            blink_max_pre = blink_max;
        }

        blinkTimer -= Time.deltaTime;

        // time to blink
        if(!blinkingNow && blinkTimer <= 0)
        {
            targets[blink_left] = 100f;
            targets[blink_right] = 100f;
            speeds[blink_left] = blinkCloseSpeed;
            speeds[blink_right] = blinkCloseSpeed;

            blinkingNow = true;
            blinkTimer = 0.2f;
        }

        if(blinkingNow && blinkTimer <= 0)
        {
            targets[blink_left] = 0f;
            targets[blink_right] = 0f;
            speeds[blink_left] = blinkOpenSpeed;
            speeds[blink_right] = blinkOpenSpeed;

            blinkTimer = Random.Range(blink_min,blink_max);
            blinkingNow = false;
        }
    }

    public float[] v = new float[15];

    void VisemesPass()
    {
        targets[cheekPuff_left] = 10 * v[1];
        targets[cheekPuff_right] = 10 * v[1];
        targets[jawBackward] = 10 * v[2];
        targets[lowerLipDown_left] = 25 * v[3]
            + 15 * v[4]
            + 15 * v[5]
            + 40 * v[6]
            + 15 * v[7]
            + 30 * v[8]
            + 5 * v[9]
            + 10 * v[11]
            + 30 * v[12];
        targets[lowerLipDown_right] = 25 * v[3]
            + 15 * v[4]
            + 15 * v[5]
            + 40 * v[6]
            + 15 * v[7]
            + 30 * v[8]
            + 5 * v[9]
            + 10 * v[11]
            + 30 * v[12];
        targets[lowerLipIn] = 100 * v[1]
            + 75 * v[2];
        targets[lowerLipOut] = 20 * v[6]
            + 20 * v[7]
            + 20 * v[11]
            + 30 * v[12]
            + 10 * v[13]
            + 30 * v[14];
        targets[midmouth_left] = 45 * v[13]
            + 70 * v[14];
        targets[midmouth_right] = 45 * v[13]
            + 70 * v[14];
#if AvatarSDK
        targets[mouthUp] = 10 * v[1]
            + 5 * v[2];
#endif
        targets[mouthDown] = 10 * v[3]
            + 5 * v[4]
            + 10 * v[5]
            + 5 * v[11]
            + 10 * v[12];
#if AvatarSDK
        targets[mouthNarrow_left] = 40 * v[2]
            + 10 * v[3]
            + 30 * v[6];
        targets[mouthNarrow_right] = 40 * v[2]
            + 10 * v[3]
            + 30 * v[6];
#elif MicroSoftRocketBox
        targets[mouthNarrow] = 40 * v[2]
            + 10 * v[3]
            + 30 * v[6];
#endif
        targets[mouthOpen] = 15 * v[2]
            + 20 * v[3]
            + 15 * v[4]
            + 15 * v[5]
            + 10 * v[6]
            + 5 * v[7]
            + 20 * v[8]
            + 15 * v[9]
            + 50 * v[10]
            + 15 * v[11]
            + 5 * v[12]
            + 40 * v[13]
            + 15 * v[14];
        targets[mouthWhistle_left] = 50 * v[4]
            + 55 * v[5]
            + 50 * v[6]
            + 50 * v[7]
            + 20 * v[8]
            + 10 * v[9]
            + 50 * v[11]
            + 60 * v[12];
        targets[mouthWhistle_right] = 50 * v[4]
            + 55 * v[5]
            + 50 * v[6]
            + 50 * v[7]
            + 20 * v[8]
            + 10 * v[9]
            + 50 * v[11]
            + 60 * v[12];
        targets[upperLipIn] = 100 * v[1]
            + 20 * v[11]
            + 40 * v[12];
        targets[upperLipOut] = 40 * v[2]
            + 20 * v[6]
            + 10 * v[7]
            + 10 * v[13]
            + 10 * v[14];
        targets[toungeUp] = 20 * v[3]
            + 20 * v[8]
            + 10 * v[9];
        targets[upperLipUp_left] = 20 * v[6]
            + 5 * v[7]
            + 5 * v[9];
        targets[upperLipUp_right] = 20 * v[6]
            + 5 * v[7]
            + 5 * v[9];
    }
}
