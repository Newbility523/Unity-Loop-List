using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;

/// <summary>
/// 播放Animator并加入播放完成回调。
/// </summary>
[RequireComponent(typeof(Animator))]
public class UIAnim : MonoBehaviour
{
    private Action showCallBack;
    private Action hideCallBack;

    public void SetShowCallBack(Action action)
    {
        showCallBack = action;
    }
    
    public void UICallBackShow()
    {
        showCallBack?.Invoke();
    }
    
    public void SetHideCallBack(Action action)
    {
        hideCallBack = action;
    }
    
    public void UICallBackHide()
    {
        hideCallBack?.Invoke();
    }
}