using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using Engine;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.UI;

public class GridListTest : MonoBehaviour
{
    public GridList list;
    public GameObject prefab;
    public int count = 0;

    public Button addBtn;
    public Button removeBtn;
    public InputField inputField;
    
    private Stack<GameObject> cells = new Stack<GameObject>();
    private Dictionary<int, GameObject> dict = new Dictionary<int, GameObject>();
    
    void Start()
    {
        list.SetUpdateListener(OnItemUpdate);
        list.InitList(count);
        list.Refresh();
        
        addBtn.onClick.AddListener((() =>
        {
            var indexStr = inputField.text;
            list.AddAt(Convert.ToInt32(indexStr));
        }));
        
        removeBtn.onClick.AddListener((() =>
        {
            var indexStr = inputField.text;
            list.RemoveAt(Convert.ToInt32(indexStr));
        }));
    }

    void OnItemUpdate(int index, int state, Vector2 pos)
    {
        // show
        if (state == 1)
        {
            var c = GetItem();
            dict[index] = c;
            c.SetActive(true);
            c.transform.SetParent(list.content);
            c.transform.Find("index").GetComponent<Text>().text = index.ToString();

            var rt = c.transform as RectTransform;
            rt.anchorMin = list.content.anchorMin;
            rt.anchorMax = list.content.anchorMax;
            // rt.anchorMin = self.anchorMin
            // view.transform.anchorMax = self.anchorMax
            rt.anchoredPosition = pos;
        }
        else
        {
            Debug.Log("remove index " + index);
            if (dict.TryGetValue(index, out var c))
            {
                c.SetActive(false);
                dict[index] = null;
                cells.Push(c);
            }
        }
    }
    
    private GameObject GetItem()
    {
        if (cells.Count > 0)
        {
            return cells.Pop();
        }

        return Instantiate(prefab);
    }
}
