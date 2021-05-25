using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;
using XLua;

namespace XLuaExpand
{


    public class ComponentBinderUtil
    {
        public delegate void NodeAnalysisOver(string key, string type, Component value, int index);

        public static void Traverse(GameObject obj, NodeAnalysisOver func)
        {
            int _childCount = obj.transform.childCount;
            GameObject _nodeObj;
            Transform _nodeTrans;
            bool _stopTraverse = false;
            for (int i = 0; i < _childCount; i++)
            {
                _nodeTrans = obj.transform.GetChild(i);
                if (_nodeTrans)
                {
                    _nodeObj = obj.transform.GetChild(i).gameObject;
                    if (_nodeObj && _nodeObj.name[0] != '_')
                    {
                        string[] _tempTypes = _nodeObj.name.Split(new char[] { '_' });
                        if (_tempTypes == null || _tempTypes.Length < 2)
                        {
                            continue;
                        }

                        string _nodeName = _tempTypes[0];
                        for (int k = 1; k < _tempTypes.Length; k++)
                        {
                            System.Type type = ConvertName2Type(_tempTypes[k]);
                            _stopTraverse = type == null;
                            int _index = -1;
                            if (Regex.IsMatch(_nodeName, @"\[\d*]"))
                            {
                                string[] _tempNameArray = Regex.Split(_nodeName, @"(\[\d*])");
                                string _tempStr = _tempNameArray[1];
                                _index = int.Parse(_tempStr.Substring(1, _tempStr.Length - 2));
                                _nodeName = type != null ? string.Format("{0}{1}", _tempNameArray[0], _tempTypes[k]) : _tempNameArray[0];
                            }
                            else
                            {
                                _nodeName = type != null ? string.Format("{0}{1}", _nodeName, _tempTypes[k]) : _nodeName;
                            }

                            Component _component;
                            string _nodeType;
                            _component = _nodeObj.GetComponent<LuaBehavior>();
                            if (type == null)
                            {
                                _nodeType = _tempTypes[k];
                                if (!_component)
                                {
                                    _component = _nodeObj.AddComponent<LuaBehavior>();
                                }
                            }
                            else
                            {
                                _nodeType = type.ToString();
                                _component = _nodeObj.GetComponent(type);
                            }

                            func(_nodeName, _nodeType, _component, _index);
                        }
                    }
                    if (!_stopTraverse)
                    {
                        Traverse(_nodeObj, func);
                    }
                }

            }
        }

        private static Type ConvertName2Type(string name)
        {
            Type _result = null;
            switch (name)
            {
                case "RectTransform":
                    _result = typeof(RectTransform);
                    break;
                case "Image":
                    _result = typeof(Image);
                    break;
                case "Button":
                    _result = typeof(Button);
                    break;
                case "Text":
                    _result = typeof(Text);
                    break;
                default:
                    Debug.LogErrorFormat("[ComponentBinder.ConvertName2Type] name not default ,name = {0}", name);
                    break;
            }

            return _result;
        }
    }

    public class ComponentBinder : MonoBehaviour
    {
        [System.Serializable]
        public class Node
        {
            public string key;
            public string type;
            public Component component;
            public int index;

            public Node(string key, string type, Component com, int index)
            {
                this.key = key;
                this.type = type;
                this.component = com;
                this.index = index;
            }
        }
         
        //[HideInInspector]
        public Node[] nodes;

        public void InjectNode(XLua.LuaTable table)
        {
            foreach (var node in nodes)
            {
                if (node == null)
                {
                    Debug.LogError($"[{node.key}] [{node.type}] Node is null");
                    continue;
                }

                if (node.index != -1)
                {
                    if (!table.ContainsKey(node.key))
                    {
                        table.Set(node.key, LuaMain.instance.NewTable(false));
                    }

                    if (node.component.GetType() == typeof(LuaBehavior))
                    {
                        LuaTable _tempTable;
                        table.Get(node.key, out _tempTable);
                        int _tempIndex = node.index;
                        Component _tempCom = node.component;
                        (node.component as LuaBehavior).rebindCallback = () =>
                        {
                            _tempTable.Set(_tempIndex, (_tempCom as LuaBehavior).GetLuaObject());
                        };
                    }
                    else
                    {
                        LuaTable _tempTable;
                        table.Get(node.key, out _tempTable);
                        _tempTable.Set(node.index, node.component);
                    }
                }
                else
                {
                    if (node.component.GetType() == typeof(LuaBehavior))
                    {
                        string _tempKey = node.key;
                        Component _tempCom = node.component;
                        (node.component as LuaBehavior).rebindCallback = () =>
                        {
                            table.Set(_tempKey, (_tempCom as LuaBehavior).GetLuaObject());
                        };
                    }
                    else
                    {
                        table.Set(node.key, node.component);
                    }
                }
            }
        }
        public void SetNodes(List<Node> nodeList)
        {
            this.nodes = nodeList.ToArray();
        }
    }


}
