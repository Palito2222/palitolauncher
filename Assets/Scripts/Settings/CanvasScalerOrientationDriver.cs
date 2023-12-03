using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

//	@kurtdekker - to help fitting portrait / landscape UI stuff together
//
//	Put this where the CanvasScaler is.
//
//	You should set CanvasScaler to:
//		ScaleWithScreenSize,
//		MatchWidthOrHeight
//		100% height
//	and author all your UI as above ^ ^ ^
//
//	CAUTION: Takes Control of CanvasScaler! See below for what it does!
//
//	Baselines to a height of 600, keeping that as the
//	scale of the minimum axis (narrowest).
//
//	This means in portrait it would assert 600 width,
//	making the height larger...

public class CanvasScalerOrientationDriver : MonoBehaviour
{
    bool? Landscape;

    const float Baseline = 600;

    void Drive()
    {
        var cs = GetComponent<CanvasScaler>();

        cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        cs.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;

        float aspect = (float)Screen.width / Screen.height;

        if ((bool)Landscape)
        {
            cs.referenceResolution = new Vector2(aspect * Baseline, Baseline);
            cs.matchWidthOrHeight = 1;
        }
        else
        {
            cs.referenceResolution = new Vector2(Baseline, aspect * Baseline);
            cs.matchWidthOrHeight = 0;
        }
    }

    void Update()
    {
        bool landscape = Screen.width > Screen.height;

        if (Landscape != landscape)
        {
            Landscape = landscape;

            Drive();
        }
    }
}