using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering;



//ScreenLineRenderer Draws a line from TargetA to TargetB
//Can be used in URP



[RequireComponent(typeof(LineRenderer))]
public class ScreenLineRenderer : MonoBehaviour
{
    //Use self name as matching ID



    public enum EScreenLineRendererTargetStyle
    {
        ByGameObject,
        ByTransform,
        ByWorldVector
    }

    public enum EScreenLineRendererLineStyle
    {
        RightAngled,
        SingleLine
    }
    [Header("Target A")]
    public EScreenLineRendererTargetStyle targetA_Style = EScreenLineRendererTargetStyle.ByGameObject;
    public GameObject   targetA_ByGameObject = null;           
    public Transform    targetA_ByTransform = null;           
    public Vector3      targetA_ByVector = Vector3.zero;

    [Header("Target B")]
    public EScreenLineRendererTargetStyle targetB_Style = EScreenLineRendererTargetStyle.ByGameObject;
    public GameObject   targetB_ByGameObject = null;
    public Transform    targetB_ByTransform = null;
    public Vector3      targetB_ByVector = Vector3.zero;

    private Vector3 targetA_Vector = Vector3.zero;
    private Vector3 targetB_Vector = Vector3.zero;

    [Header("Line Style")]
    public EScreenLineRendererLineStyle lineStyle = EScreenLineRendererLineStyle.RightAngled;

    [Header("Setup")]
    public Camera targetCamera = null;
    public float distanceFromCamera = 2;
    private LineRenderer lineRenderer;
    private void OnEnable()
    {
        RenderPipelineManager.beginFrameRendering += BeginFrameRenderingCallback;
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.enabled = true;
        lineRenderer.positionCount = 3;
    }


    private void OnDisable()
    {
        RenderPipelineManager.beginFrameRendering -= BeginFrameRenderingCallback;
        lineRenderer.enabled = false;

    }

    void BeginFrameRenderingCallback (ScriptableRenderContext a, Camera[] b)
    {


        RenderLogic();
    }
    void RenderLogic()
    {
        if (!targetCamera)
        {
            lineRenderer.SetPosition(0, Vector3.zero);
            lineRenderer.SetPosition(1, Vector3.zero);
            lineRenderer.SetPosition(2, Vector3.zero);
            return;
        }
        //targetCamera = Camera.main;


        switch (targetA_Style)
        {
            case EScreenLineRendererTargetStyle.ByGameObject:
                targetA_Vector = targetA_ByGameObject.transform.position;
                break;
            case EScreenLineRendererTargetStyle.ByTransform:
                targetA_Vector = targetA_ByTransform.position;
                break;
            case EScreenLineRendererTargetStyle.ByWorldVector:
                targetA_Vector = targetA_ByVector;
                break;
            default:
                throw new System.NotImplementedException();
        }

        switch (targetB_Style)
        {
            case EScreenLineRendererTargetStyle.ByGameObject:
                targetB_Vector = targetB_ByGameObject.transform.position;
                break;
            case EScreenLineRendererTargetStyle.ByTransform:
                targetB_Vector = targetB_ByTransform.position;
                break;
            case EScreenLineRendererTargetStyle.ByWorldVector:
                targetB_Vector = targetB_ByVector;
                break;
            default:
                throw new System.NotImplementedException();
        }


        //Vector3 screenPointSelf = targetCamera.WorldToScreenPoint(targetA_Vector);
        Vector3 viewportPointSelf = targetCamera.WorldToViewportPoint(targetA_Vector);
        Vector3 worldPointSelf;

        //Vector3 screenPointTarget = targetCamera.WorldToScreenPoint(targetB_Vector);   //Convert world space to Screenspace and then to RectTransform Space via Camera Space
        Vector3 viewportPointTarget = targetCamera.WorldToViewportPoint(targetB_Vector);   //Convert world space to Screenspace and then to RectTransform Space via Camera Space
        Vector3 worldPointTarget;

        //Vector3 screenPointMid = screenPointSelf + Vector3.Project(screenPointTarget - screenPointSelf, Vector3.right);
        Vector3 viewportPointMid = viewportPointSelf + Vector3.Project(viewportPointTarget - viewportPointSelf, Vector3.right);
        Vector3 worldPointMid;


        var distance = distanceFromCamera;
        var frustumHeight = 2.0f * distance * Mathf.Tan(targetCamera.fieldOfView * 0.5f * Mathf.Deg2Rad);
        var frustumWidth = frustumHeight * targetCamera.aspect;
        var fustrumBottomLeft = targetCamera.transform.position + (targetCamera.transform.forward * distance) + (targetCamera.transform.right * frustumWidth * -0.5f) + (targetCamera.transform.up * frustumHeight * -0.5f);
        var fustrumTopLeft = fustrumBottomLeft + targetCamera.transform.up * frustumHeight;
        var fustrumBottomRight = fustrumBottomLeft + targetCamera.transform.right * frustumWidth;

        //Draw Fustrum Box
        //Debug.DrawRay(fustrumBottomLeft, targetCamera.transform.up * frustumHeight, Color.white);
        //Debug.DrawRay(fustrumBottomLeft, targetCamera.transform.right * frustumWidth, Color.white);
        //Debug.DrawRay(fustrumBottomLeft + targetCamera.transform.up * frustumHeight, targetCamera.transform.right * frustumWidth, Color.white);
        //Debug.DrawRay(fustrumBottomLeft + targetCamera.transform.right * frustumWidth, targetCamera.transform.up * frustumHeight, Color.white);

        //This remaps the viewport to the fustrum box. Because the viewport min max is (0,0) to (1,1), we can use this to lerp values.
        worldPointSelf =
        Vector3.Lerp(fustrumBottomLeft, fustrumBottomRight, viewportPointSelf.x) +
        Vector3.Lerp(fustrumBottomLeft, fustrumTopLeft, viewportPointSelf.y) - fustrumBottomLeft;

        worldPointTarget =
        Vector3.Lerp(fustrumBottomLeft, fustrumBottomRight, viewportPointTarget.x) +
        Vector3.Lerp(fustrumBottomLeft, fustrumTopLeft, viewportPointTarget.y) - fustrumBottomLeft;

        worldPointMid =
        Vector3.Lerp(fustrumBottomLeft, fustrumBottomRight, viewportPointMid.x) +
        Vector3.Lerp(fustrumBottomLeft, fustrumTopLeft, viewportPointMid.y) - fustrumBottomLeft;

        //Test if both objects is in front of camera
        if (Vector3.Dot(targetCamera.transform.forward, targetB_Vector - targetCamera.transform.position) >= 0 &&
            Vector3.Dot(targetCamera.transform.forward, targetA_Vector - targetCamera.transform.position) >= 0)
        {
            lineRenderer.SetPosition(0, worldPointSelf);
            switch (lineStyle)
            {
                case EScreenLineRendererLineStyle.RightAngled:
                    lineRenderer.SetPosition(1, worldPointMid);
                    break;
                case EScreenLineRendererLineStyle.SingleLine:
                    lineRenderer.SetPosition(1, worldPointTarget);
                    break;
            }
            lineRenderer.SetPosition(2, worldPointTarget);
        }
        else
        {
            lineRenderer.SetPosition(0, Vector3.zero);
            lineRenderer.SetPosition(1, Vector3.zero);
            lineRenderer.SetPosition(2, Vector3.zero);
        }

        Debug.DrawRay(worldPointSelf, Vector3.up, Color.cyan);
        Debug.DrawRay(worldPointMid, Vector3.up, Color.cyan);
        Debug.DrawRay(worldPointTarget, Vector3.up, Color.cyan);

    }

    private void OnDrawGizmos()
    {
        if (targetCamera)
        {
            var distance = distanceFromCamera;
            var frustumHeight = 2.0f * distance * Mathf.Tan(targetCamera.fieldOfView * 0.5f * Mathf.Deg2Rad);
            var frustumWidth = frustumHeight * targetCamera.aspect;
            var fustrumBottomLeft = targetCamera.transform.position + (targetCamera.transform.forward * distance) + (targetCamera.transform.right * frustumWidth * -0.5f) + (targetCamera.transform.up * frustumHeight * -0.5f);

            //Draw Fustrum Box
            Debug.DrawRay(fustrumBottomLeft, targetCamera.transform.up * frustumHeight, Color.white);
            Debug.DrawRay(fustrumBottomLeft, targetCamera.transform.right * frustumWidth, Color.white);
            Debug.DrawRay(fustrumBottomLeft + targetCamera.transform.up * frustumHeight, targetCamera.transform.right * frustumWidth, Color.white);
            Debug.DrawRay(fustrumBottomLeft + targetCamera.transform.right * frustumWidth, targetCamera.transform.up * frustumHeight, Color.white);
        }
    }
    private void OnDrawGizmosSelected()
    {
        //MidPoint of 2 objects
        var midpoint = targetA_Vector + ((targetB_Vector - targetA_Vector) / 2);
        if (targetCamera)
            Debug.DrawLine(targetCamera.transform.position, midpoint, Color.blue);

        Debug.DrawLine(targetA_Vector, targetB_Vector, Color.blue);
    }

}
