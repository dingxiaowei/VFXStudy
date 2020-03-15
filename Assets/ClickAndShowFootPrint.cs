using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.VFX;

public class VFXEffectTypeData
{
    public VisualEffect Effect;
    public VFXEventAttribute EventAttribute;
}

public class ClickAndShowFootPrint : MonoBehaviour
{
    static readonly int startPositionId = Shader.PropertyToID("Position");
    static readonly int normalId = Shader.PropertyToID("Normal");

    public float ValidTouchDistance = 200;

    Dictionary<VisualEffectAsset, VFXEffectTypeData> mEffectTypeData = new Dictionary<VisualEffectAsset, VFXEffectTypeData>(32);
    GameObject m_RootGameObject;
    void Start()
    {
        m_RootGameObject = new GameObject("VFXSystem");
        m_RootGameObject.transform.position = Vector3.zero;
        m_RootGameObject.transform.rotation = Quaternion.identity;
        GameObject.DontDestroyOnLoad(m_RootGameObject);

        Application.lowMemory += OnLowMemory;
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);  //摄像机需要设置MainCamera的Tag这里才能找到
            RaycastHit hitInfo;
            if (Physics.Raycast(ray, out hitInfo, ValidTouchDistance, LayerMask.GetMask("Ground")))
            {
                GameObject gameObj = hitInfo.collider.gameObject;
                Vector3 hitPoint = hitInfo.point;
                Vector3 hitNormal = hitInfo.normal;
                Debug.Log("click object name is " + gameObj.name + " , hit point " + hitPoint.ToString() + " ,hit normal " + hitNormal.ToString());
                LoadVFX(hitPoint, hitNormal);
            }
        }
    }

    void LoadVFX(Vector3 position, Vector3 normal)
    {
        var vfxAsset = AssetDatabase.LoadAssetAtPath<VisualEffectAsset>("Assets/VFX/HelloVFX.vfx");
        SpawnPointEffect(vfxAsset, position, normal);
    }

    void SpawnPointEffect(VisualEffectAsset asset, Vector3 position, Vector3 normal)
    {
        VFXEffectTypeData effectData;
        if (!mEffectTypeData.TryGetValue(asset, out effectData))
        {
            effectData = RegisterImpactType(asset);
        }
        effectData.Effect.SetVector3(startPositionId, position);
        Quaternion Rotation = Quaternion.FromToRotation(Vector3.forward, normal);
        Vector3 rot = Rotation.eulerAngles;
        effectData.Effect.SetVector3("Direction", rot);
        effectData.Effect.Play(effectData.EventAttribute);
        effectData.Effect.pause = false;
    }

    VFXEffectTypeData RegisterImpactType(VisualEffectAsset template)
    {
        if (template == null)
            throw new System.Exception("VFX Template is null");
        if (mEffectTypeData.ContainsKey(template))
            throw new System.Exception("mEffectTypeData Contains template");

        GameObject go = new GameObject(template.name);
        go.transform.parent = m_RootGameObject.transform;
        go.transform.position = Vector3.zero;
        go.transform.rotation = Quaternion.identity;
        go.transform.localScale = Vector3.one;
        var vfx = go.AddComponent<VisualEffect>();
        vfx.visualEffectAsset = template;
        vfx.Reinit();
        vfx.Stop();

        var data = new VFXEffectTypeData
        {
            Effect = vfx,
            EventAttribute = vfx.CreateVFXEventAttribute(),
        };
        mEffectTypeData.Add(template, data);
        return data;
    }

    void OnLowMemory()
    {
        ClearCache();
    }

    void ClearCache()
    {
        m_RootGameObject.transform.DetachChildren();
        mEffectTypeData.Clear();
    }
}
