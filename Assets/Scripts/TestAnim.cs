using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class TestAnim : MonoBehaviour
{
    public Animator anim;

    public Button switchBtn;

    public List<string> animNames = new List<string>();

    private int curIndex = 0;
    // Start is called before the first frame update
    void Start()
    {
        Slider sld = null;
        sld.value = 1.0f;
        sld.DOValue(1.0f, 2.0f);
        var seq = DOTween.Sequence();
        seq.Pause();
        seq.Play();
        var pos = Vector3.one;
        var p = transform.GetComponent<Animation>();
        // p.Play(())
        // k
        // pos.normalized
        // Image a;
        // a.DOFillAmount(0.0f, 1.0f)
        // ScrollRect a;
        // a.onValueChanged.AddListener((pos =>
        // {
        //     
        // }));
        // a.onValueChanged.RemoveAllListeners();
        var temp = new Color();
            
        switchBtn.onClick.AddListener(()=>
        {
            curIndex = (curIndex + 1) % 2;
            Debug.Log("switch to " + animNames[curIndex]);

            var info = anim.GetCurrentAnimatorStateInfo(0);
            var info2 = anim.GetCurrentAnimatorClipInfo(0);
            // ani.DOPlay(animNames[curIndex]);
            var targetNormalizedTime = 1 - Mathf.Clamp01(info.normalizedTime);
            anim.Play(animNames[curIndex], 0, targetNormalizedTime);
        });

        var t = transform.GetComponent<Animation>();
    }
    
    
    

    // Update is called once per frame
    void Update()
    {
        
    }
}
