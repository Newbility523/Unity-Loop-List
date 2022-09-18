using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System;
using DG.Tweening;

namespace Engine
{
    public class GridList : ScrollRect
    {
        [SerializeField]
        private Vector2 cellSize;
        public Vector2 CellSize
        {
            get
            {
                return cellSize;
            }
            set
            {
                cellSize = value;
            }
        }

        [SerializeField]
        private Vector2 spaceing;
        public Vector2 Spacing
        {
            get
            {
                return spaceing;
            }
            set
            {
                spaceing = value;
            }
        }

        public bool Horizontal
        {
            get
            {
                return horizontal;
            }
            set
            {
                horizontal = value;
                if (horizontal)
                {
                    vertical = false;
                }
            }
        }

        public bool Vertical
        {
            get
            {
                return vertical;
            }
            set
            {
                vertical = value;
                if (vertical)
                {
                    horizontal = false;
                }
            }
        }

        public float top;
        public float bottom;
        public float left;
        public float right;

        public RectOffset padding;

        public int visibleLineCount_Vertical = 0;  // 可视行数
        public int visibleLineCount_Horizontal = 0;  // 可视行数
        public int itemCountInLine = 0;

        public GridLayoutGroup.Corner startCorner;       // 开始点位，默认用左上角

        private Vector2 anchorMin;
        private Vector2 anchorMax;
        private Vector2 pivot;
        private float horizontalPosDir = 1;
        private float verticalPosDir = -1;
        private float contentPosDir = 1;    // 顺的移动方向
        private float contentWidth = 0.0f;
        private float contentHeight = 0.0f;

        private int topLineIndexTest = 0;
        private int btmLineIndexTest = 0;
        private int lineCountTest = 0;

        private int topLineIndexTest_cache = 0;
        private int btmLineIndexTest_cache = -1;

        public Ease AnimType { get; set; } = Ease.Linear;

        private Tween tween;

        private bool blocking = false;

        // 总行数
        public int LineCount
        {
            get
            {
                if (itemCountInLine == 0 || cellCount == 0)
                {
                    return 0;
                }

                return Mathf.CeilToInt((float)cellCount / itemCountInLine);
            }
        }

        // 总格子数
        [SerializeField]
        private int cellCount;
        public int CellCount
        {
            get { return cellCount; }
            set
            {
                BlockUpdate();
                cellCount = value > 0 ? value : 0;
            }
        }

        // 缓存格子结构
        private class CellInfo
        {
            public int index;
            public RectTransform rt;
            public bool idel
            {
                get
                {
                    return index == -1;
                }
                set
                {
                    index = -1;
                }
            }

            public CellInfo(int index, RectTransform rt)
            {
                this.index = index;
                this.rt = rt;
                this.idel = true;
            }

            public void SetActive(bool isActive)
            {
                if (rt == null)
                {
                    return;
                }

                rt.gameObject.SetActive(isActive);
            }

            public void SetPos(Vector2 pos)
            {
                if (rt == null)
                {
                    return;
                }

                rt.anchoredPosition = pos;
            }
        }

        protected override void Awake()
        {
            base.Awake();
            this.onValueChanged.RemoveAllListeners();
            this.onValueChanged.AddListener(OnValueChange);
        }

        protected override void Start()
        {
            base.Start();
        }

        private enum GridUpdateState
        {
            Add = 1,
            Remove = -1,
        }

        private Action<int, int, Vector2> gridUpdateCB;

        public void SetUpdateListener(Action<int, int, Vector2> cb)
        {
            gridUpdateCB = cb;
        }

        private void GridUpdate(int index, GridUpdateState state, Vector2 pos)
        {
            if (index >= CellCount)
                return;

            gridUpdateCB?.Invoke(index, (int)state, pos);
        }

        public void InitList(int cellCount)
        {
            BlockUpdate();
            ClearAll();
            topLineIndexTest = 0;
            btmLineIndexTest = 0;
            lineCountTest = 0;
            topLineIndexTest_cache = 0;
            btmLineIndexTest_cache = -1;
            CellCount = cellCount;
            StopMovement();
            InitContentRt();
        }

        public void Refresh(RefreshKeepType keepType = RefreshKeepType.init)
        {
            var targetPos = Vector2.zero;
            ClearAll();
            switch (keepType)
            {
                case RefreshKeepType.init:
                    targetPos = Vector2.zero;
                    break;
                case RefreshKeepType.Pos:
                    targetPos = content.anchoredPosition;
                    break;
            }
            
            EnableUpdate();
            OnValueChange(targetPos);
        }
        
        /// <summary>
        /// 移动到指定索引位置，位置为目标格子放加上空隙
        /// </summary>
        /// <param name="index">目标格子索引，由 0 开始</param>
        /// <param name="offset">偏移量，顺着滑动方向为正</param>
        /// <param name="speed">移动速度，当速度小于等于 0 不做动画</param>
        public void ScrollToIndex(int index, float offset = 0.0f, float speed = 0)
        {
            index = Mathf.Clamp(index, 0, CellCount);
            var pos = GetLinePos(index) - GetLinePos(0);
            ScrollToPos(pos, offset, speed);
        }

        /// <summary>
        /// 移动到指定位置
        /// </summary>
        /// <param name="pos">位置，大于 0</param>
        /// <param name="offset">偏移量，顺着滑动方向为正</param>
        /// <param name="speed">移动速度，当速度小于等于 0 不做动画</param>
        public void ScrollToPos(float pos, float offset = 0.0f, float speed = 0)
        {
            pos += offset;
            var max = Mathf.Abs(Vertical ? content.rect.height - viewport.rect.height : content.rect.width - viewport.rect.width);
            var min = 0.0f;

            if (contentPosDir < 0)
            {
                min = contentPosDir * max;
                max = 0.0f;
            }
            pos = Mathf.Clamp(pos, min, max);

            StopMovement();
            StopAnim();

            var posX = 0.0f;
            var posY = 0.0f;
            var rt = content.transform as RectTransform;
            posX = Vertical ? rt.anchoredPosition.x : pos;
            posY = Horizontal ? rt.anchoredPosition.y : pos;
            var targetPos = new Vector2(posX, posY);
            if (speed > 0)
            {
                var time = (targetPos - rt.anchoredPosition).magnitude / speed;
                tween = content.DOAnchorPos(targetPos, time).SetEase(AnimType);
            }
            else
            {
                rt.anchoredPosition = targetPos;
            }
        }

        public void StopAnim()
        {
            if (tween == null || !tween.IsActive()) return;

            tween.Kill();
            tween = null;
        }

        public void AddAt(int index)
        {
            ModifyAt(index, ModifyType.Add);
        }
        
        public void RemoveAt(int index)
        {
            ModifyAt(index, ModifyType.Remove);
        }

        private enum ModifyType 
        {
            Add,
            Remove,
        }
        
        private void ModifyAt(int index, ModifyType t)
        {
            BlockUpdate();
            
            var oriLineCount = LineCount;
            var oriWidth = contentWidth;
            var oriHeight = contentHeight;
            var oriPos = content.anchoredPosition;

            CellCount += t == ModifyType.Add ? 1 : -1;
            var newLineCount = LineCount;
            Cal_ContentSize(out var newWidth, out var newHeight);
            
            var from = topLineIndexTest_cache * itemCountInLine;
            var to = btmLineIndexTest_cache * itemCountInLine - 1;

            if (newLineCount != oriLineCount)
            {
                content.sizeDelta = new Vector2(newWidth, newHeight);
                contentWidth = newWidth;
                contentHeight = newHeight;
            }
            
            if (index <= from)
            {
                var delta = new Vector2(newWidth, newHeight) - new Vector2(oriWidth, oriHeight);
                var operationDir = t == ModifyType.Add ? 1 : -1;
                var newPos = oriPos + contentPosDir * delta;
                content.anchoredPosition = newPos;
            }
            
            // TODO 仅更新变化的一个，其他仅做移动
            Refresh(RefreshKeepType.Pos);
        }

        private void BlockUpdate()
        {
            blocking = true;
        }
        
        private void EnableUpdate()
        {
            blocking = false;
        }

        private void Cal_ContentSize(out float w, out float h)
        {
            w = 0.0f;
            h = 0.0f;
            if (LineCount > 0)
            {
                if (vertical)
                {
                    w = padding.left + padding.right + cellSize.x * itemCountInLine + spaceing.x * (itemCountInLine - 1);
                    h = padding.top + padding.bottom + cellSize.y * LineCount + spaceing.y * (LineCount - 1);
                }
                else 
                {
                    w = padding.left + padding.right + cellSize.x * LineCount + spaceing.x * (LineCount  - 1);
                    h = padding.top + padding.bottom + cellSize.y * itemCountInLine + spaceing.y * (itemCountInLine - 1);
                }
            }
           
            var viewPortW = viewport.rect.width;
            var viewPortH = viewport.rect.height;
            w = w > viewPortW ? w : viewPortW + 0.1f;
            h = h > viewPortH ? h : viewPortH + 0.1f;
        }

        public enum RefreshKeepType 
        {
            init,
            Pos,
            View,
        }
        
        public void UpdateCount(int newCount, RefreshKeepType keepType = RefreshKeepType.init)
        {
            CellCount = newCount;
            Refresh();
        }

        private void InitContentRt()
        {
            Cal_ContentSize(out contentWidth, out contentHeight);

            switch (startCorner)
            {
                case GridLayoutGroup.Corner.UpperRight:
                    anchorMin = new Vector2(1, 1);
                    anchorMax = new Vector2(1, 1);
                    pivot = new Vector2(1, 1);
                    horizontalPosDir = -1;
                    verticalPosDir = -1;
                    contentPosDir = 1;
                    break;
                case GridLayoutGroup.Corner.LowerLeft:
                    anchorMin = new Vector2(0, 0);
                    anchorMax = new Vector2(0, 0);
                    pivot = new Vector2(0, 0);
                    horizontalPosDir = 1;
                    verticalPosDir = 1;
                    contentPosDir = Vertical ? -1 : -1;
                    break;
                case GridLayoutGroup.Corner.LowerRight:
                    anchorMin = new Vector2(1, 0);
                    anchorMax = new Vector2(1, 0);
                    pivot = new Vector2(1, 0);
                    horizontalPosDir = -1;
                    verticalPosDir = 1;
                    contentPosDir = Vertical ? -1 : 1;
                    break;
                // 默认左上角
                // case GridLayoutGroup.Corner.UpperLeft:
                default:
                    anchorMin = new Vector2(0, 1);
                    anchorMax = new Vector2(0, 1);
                    pivot = new Vector2(0, 1);
                    horizontalPosDir = 1;
                    verticalPosDir = -1;
                    contentPosDir = Vertical ? 1 : -1;
                    break;
            }

            content.anchorMin = anchorMin;
            content.anchorMax = anchorMax;
            content.pivot = pivot;
            content.sizeDelta = new Vector2(contentWidth, contentHeight);
            content.anchoredPosition = Vector2.zero;

            // cellList.Clear();
        }

        private int GetLineIndex(float pos)
        {
            if (LineCount <= 0)
            {
                return -1;
            }

            if (LineCount <= 1)
            {
                return 0;
            }

            pos = Mathf.Abs(pos);

            switch (startCorner)
            {
                case GridLayoutGroup.Corner.UpperLeft:
                    if (Vertical)
                    {
                        pos -= padding.top + cellSize.y + Spacing.y * 0.5f;
                    }
                    else
                    {
                        pos -= padding.left + cellSize.x + Spacing.x * 0.5f;
                    }
                    break;
                case GridLayoutGroup.Corner.UpperRight:
                    if (Vertical)
                    {
                        pos -= padding.top + cellSize.y + Spacing.y * 0.5f;
                    }
                    else
                    {
                        pos -= padding.right + cellSize.x + Spacing.x * 0.5f;
                    }
                    break;
                case GridLayoutGroup.Corner.LowerLeft:
                    if (Vertical)
                    {
                        pos -= padding.bottom + cellSize.y + Spacing.y * 0.5f;
                    }
                    else
                    {
                        pos -= padding.left + cellSize.x + Spacing.x * 0.5f;
                    }
                    break;
                case GridLayoutGroup.Corner.LowerRight:
                    if (Vertical)
                    {
                        pos -= padding.bottom + cellSize.y + Spacing.y * 0.5f;
                    }
                    else
                    {
                        pos -= padding.right + cellSize.x + Spacing.x * 0.5f;
                    }
                    break;
            }

            pos = pos > 0 ? pos : 0;

            float singleSize = 0;
            if (Vertical)
            {
                singleSize = cellSize.y + Spacing.y;
            }
            else
            {
                singleSize = cellSize.x + Spacing.x;
            }

            return Mathf.CeilToInt(pos / singleSize);
        }

        private float GetLinePos(int cellIndex)
        {
            float pos;
            var lineIndex = Mathf.FloorToInt((float)cellIndex / itemCountInLine);
            var indexInline = cellIndex % itemCountInLine;

            var startPos = Vector2.zero;

            switch (startCorner)
            {
                case GridLayoutGroup.Corner.UpperLeft:
                    startPos = new Vector2(padding.left, padding.top);
                    break;
                case GridLayoutGroup.Corner.UpperRight:
                    startPos = new Vector2(padding.right, padding.top);
                    break;
                case GridLayoutGroup.Corner.LowerLeft:
                    startPos = new Vector2(padding.left, padding.bottom);
                    break;
                case GridLayoutGroup.Corner.LowerRight:
                    startPos = new Vector2(padding.right, padding.bottom);
                    break;
            }

            if (Vertical)
            {
                pos = startPos.y + (cellSize.y + spaceing.y) * lineIndex;
            }
            else
            {
                pos = startPos.x + (cellSize.x + spaceing.x) * lineIndex;
            }

            pos *= contentPosDir;

            return pos;
        }

        // Cell Pivot 为中心
        private Vector2 GetCellPos(int index)
        {
            float x, y;
            var lineIndex = Mathf.FloorToInt((float)index / itemCountInLine);
            var indexInline = index % itemCountInLine;

            var startPos = Vector2.zero;

            switch (startCorner)
            {
                case GridLayoutGroup.Corner.UpperLeft:
                    startPos = new Vector2(padding.left, padding.top);
                    break;
                case GridLayoutGroup.Corner.UpperRight:
                    startPos = new Vector2(padding.right, padding.top);
                    break;
                case GridLayoutGroup.Corner.LowerLeft:
                    startPos = new Vector2(padding.left, padding.bottom);
                    break;
                case GridLayoutGroup.Corner.LowerRight:
                    startPos = new Vector2(padding.right, padding.bottom);
                    break;
            }

            if (Vertical)
            {
                x = startPos.x + cellSize.x * 0.5f + (cellSize.x + spaceing.x) * indexInline;
                y = startPos.y + cellSize.y * 0.5f + (cellSize.y + spaceing.y) * lineIndex;
            }
            else
            {
                x = startPos.x + cellSize.x * 0.5f + (cellSize.x + spaceing.x) * lineIndex;
                y = startPos.y + cellSize.y * 0.5f + (cellSize.y + spaceing.y) * indexInline;
            }

            x *= horizontalPosDir;
            y *= verticalPosDir;

            return new Vector2(x, y);
        }

        // private Vector2 GetCellCont(int index)
        // {
        //     float x, y;
        //     var lineIndex = Mathf.FloorToInt(index / itemCountInLine);
        //     var indexInline = index % itemCountInLine;
        //
        //     var startPos = Vector2.zero;
        //
        //     switch (startCorner)
        //     {
        //         case GridLayoutGroup.Corner.UpperLeft:
        //             startPos = new Vector2(padding.left, padding.top);
        //             break;
        //         case GridLayoutGroup.Corner.UpperRight:
        //             startPos = new Vector2(padding.right, padding.top);
        //             break;
        //         case GridLayoutGroup.Corner.LowerLeft:
        //             startPos = new Vector2(padding.left, padding.bottom);
        //             break;
        //         case GridLayoutGroup.Corner.LowerRight:
        //             startPos = new Vector2(padding.right, padding.bottom);
        //             break;
        //     }
        //
        //     if (Vertical)
        //     {
        //         x = startPos.x + cellSize.x * 0.5f + (cellSize.x + spaceing.x) * indexInline;
        //         y = startPos.y + cellSize.y * 0.5f + (cellSize.y + spaceing.y) * lineIndex;
        //     }
        //     else
        //     {
        //         x = startPos.x + cellSize.x * 0.5f + (cellSize.x + spaceing.x) * lineIndex;
        //         y = startPos.y + cellSize.y * 0.5f + (cellSize.y + spaceing.y) * indexInline;
        //     }
        //
        //     x *= horizontalPosDir;
        //     y *= verticalPosDir;
        //
        //     return new Vector2(x, y);
        // }


        //private Vector2 cachePos = Vector2.zero;
        protected override void LateUpdate()
        {
            base.LateUpdate();
#if UNITY_EDITOR
            if (!UnityEditor.EditorApplication.isPlaying && content)
            {
                contentWidth = 0;
                contentHeight = 0;
                InitList(content.childCount);
                for (int i = 0; i < CellCount; i++)
                {
                    content.GetChild(i).localPosition = GetCellPos(i);
                }
            }
#endif
        }

        private void ClearAll()
        {
            var fromIndex = topLineIndexTest_cache * itemCountInLine;
            var toIndex = Math.Min(btmLineIndexTest_cache * itemCountInLine, CellCount);
            
            for (var i = fromIndex; i <= toIndex; ++i)
            {
                GridUpdate(i, GridUpdateState.Remove, Vector2.zero);
            }
           
            blocking = false;
            topLineIndexTest_cache = 0;
            btmLineIndexTest_cache = -1;
        }

        private void OnValueChange(Vector2 pos)
        {
            // Debug.LogWarning("OnValueChange");
            // return;
            if (blocking)
            {
                return;
            }
            
            var curPos = content.anchoredPosition;
            float tempPos;
            tempPos = Vertical ? curPos.y : curPos.x;

            // 如果为负数，说明异向
            if (tempPos * contentPosDir < 0)
            {
                tempPos = 0.0f;
            }

            var temp0 = GetLineIndex(tempPos);

            var viewSize = Vertical ? viewRect.rect.height : viewRect.rect.width;
            var temp1 = GetLineIndex(tempPos + contentPosDir * viewSize);

            topLineIndexTest = Mathf.Min(temp0, temp1);
            btmLineIndexTest = Mathf.Max(temp0, temp1);

            lineCountTest = btmLineIndexTest - topLineIndexTest + 1;

            var exLineCount = visibleLineCount_Vertical - lineCountTest;
            exLineCount = exLineCount > 0 ? exLineCount : 0;

            // 如果不能均分，优先下方多一行
            var topEx = Mathf.FloorToInt(exLineCount * 0.5f);
            var btmEx = topEx + 1;

            var topLineIndexTest_temp = topLineIndexTest - topEx;
            topLineIndexTest_temp = Mathf.Max(topLineIndexTest_temp, 0);

            var btmLineIndexTest_temp = topLineIndexTest_temp + visibleLineCount_Vertical - 1;
            btmLineIndexTest_temp = Mathf.Min(btmLineIndexTest_temp, LineCount - 1);

            // 回首掏
            topLineIndexTest_temp = btmLineIndexTest_temp - visibleLineCount_Vertical + 1;
            topLineIndexTest_temp = Mathf.Max(topLineIndexTest_temp, 0);

            UpdateView(topLineIndexTest_temp, btmLineIndexTest_temp);
        }

        private void UpdateView(int topLineIndexTest_new, int btmLineIndexTest_new)
        {
            var change = topLineIndexTest_cache != topLineIndexTest_new || btmLineIndexTest_cache != btmLineIndexTest_new;

            if (!change)
            {
                return;
            }

            var lineCount = 0;
            var fromIndex = 0;
            var toIndex = -1;
            RectTransform tempRt;

            // recovery first
            if (topLineIndexTest_new > topLineIndexTest_cache)
            {
                fromIndex = topLineIndexTest_cache * itemCountInLine;
                toIndex = Mathf.Min((topLineIndexTest_new) * itemCountInLine - 1, (btmLineIndexTest_cache + 1) * itemCountInLine - 1);
                for (var i = fromIndex; i <= toIndex; ++i)
                {
                    GridUpdate(i, GridUpdateState.Remove, Vector2.zero);
                }
            }

            if (btmLineIndexTest_cache > btmLineIndexTest_new)
            {
                fromIndex = Mathf.Max((btmLineIndexTest_new + 1) * itemCountInLine, topLineIndexTest_cache * itemCountInLine);
                toIndex = (btmLineIndexTest_cache + 1) * itemCountInLine - 1;

                for (var i = toIndex; i >= fromIndex; --i)
                {
                    GridUpdate(i, GridUpdateState.Remove, Vector2.zero);
                }
            }

            // Show new cellItem
            if (topLineIndexTest_new < topLineIndexTest_cache)
            {
                fromIndex = topLineIndexTest_new * itemCountInLine;
                toIndex = Mathf.Min((btmLineIndexTest_new + 1) * itemCountInLine - 1, topLineIndexTest_cache * itemCountInLine - 1);

                for (var i = toIndex; i >= fromIndex; --i)
                {
                    GridUpdate(i, GridUpdateState.Add, GetCellPos(i));
                }
            }

            if (btmLineIndexTest_new > btmLineIndexTest_cache)
            {
                fromIndex = Mathf.Max((topLineIndexTest_new) * itemCountInLine, (btmLineIndexTest_cache + 1) * itemCountInLine);
                toIndex = (btmLineIndexTest_new + 1) * itemCountInLine - 1;

                for (var i = fromIndex; i <= toIndex; ++i)
                {
                    GridUpdate(i, GridUpdateState.Add, GetCellPos(i));
                }
            }

            topLineIndexTest_cache = topLineIndexTest_new;
            btmLineIndexTest_cache = btmLineIndexTest_new;
        }

        private void _ClearAllCells()
        {
            
        }

        private void ShowCellIndex(RectTransform cell, int index)
        {
            cell.GetComponentInChildren<Text>().text = index.ToString();
        }

        //public override void OnScroll(PointerEventData data)
        //{
        //    base.OnScroll(data);
        //}

        //public override void OnDrag(PointerEventData eventData)
        //{
        //    base.OnDrag(eventData);
        //}

        // // Debug
        //private void OnGUI()
        //{
        //    GUILayout.Label("topLineIndexTest: " + topLineIndexTest);
        //    GUILayout.Label("btmLineIndexTest: " + btmLineIndexTest);
        //    GUILayout.Label("topLineIndexTest_cache: " + topLineIndexTest_cache);
        //    GUILayout.Label("btmLineIndexTest_cache: " + btmLineIndexTest_cache);
        //    GUILayout.Label("lineCountTest: " + lineCountTest);
        //}

        protected override void OnDestroy()
        {
            StopAnim();
            onValueChanged.RemoveAllListeners();
            gridUpdateCB = null;
            base.OnDestroy();
        }
    }
}

