using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace XLuaExpand
{
    [CustomEditor(typeof(ComponentBinder))]
    public class ComponentBinderEditor : Editor
    {
        private ComponentBinder binder;
        private List<ComponentBinder.Node> nodeList = new List<ComponentBinder.Node>();
        private Vector2 scrollPosition;
        private string LUA_PATH;
        private string LUA_FILE_EXTENSION = ".txt";
        private static readonly string CLASS_DEFINE_START = "----------CLASS DEFINE BEGIN----------";
        private static readonly string CLASS_DEFINE_END = "----------CLASS DEFINE END----------";
        private void OnEnable()
        {
            LUA_PATH = Path.Combine(Application.dataPath, "XLuaExpand/Lua");
            binder = target as ComponentBinder;
            if (binder)
            {
                nodeList.Clear();
                ComponentBinderUtil.Traverse(binder.gameObject, (key, type, value, index) =>
                 {
                     nodeList.Add(new ComponentBinder.Node(key, type, value, index));
                 });
            }
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            GUILayout.Label("Prefab 节点预览", EditorStyles.boldLabel);
            scrollPosition = GUILayout.BeginScrollView(scrollPosition, GUILayout.Height(200));
            GUIStyle _style = new GUIStyle();
            _style.fontSize = 12;
            _style.normal.textColor = Color.green;

            foreach (var node in nodeList)
            {
                if (node.index == -1)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(string.Format("{0}", node.type), _style,GUILayout.MaxWidth(150));
                    GUILayout.Label(string.Format("{0}", node.key), EditorStyles.label);
                    GUILayout.EndHorizontal();
                }
                else
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(string.Format("{0}", node.type), _style,GUILayout.MaxWidth(150));
                    GUILayout.Label(string.Format("{0}[{1}]", node.key,node.index), EditorStyles.label);
                    GUILayout.EndHorizontal();
                    //GUILayout.Label(string.Format("{0} {1}[{2}]", node.type, node.key, node.index),_style);
                }
            }
            GUILayout.EndScrollView();
            if (GUILayout.Button("Export", EditorStyles.miniButtonMid))
            {
                DoExport();
                AssetDatabase.Refresh();
                Debug.Log("[ComponentBinderEditor.OnInspectorGUI]  Export Lua file Finished!!!");
            }

            if (GUI.changed)
                EditorUtility.SetDirty(target);

        }

        /// <summary>
        /// 导出
        /// </summary>
        private void DoExport()
        {
            binder.SetNodes(nodeList);
            
            GameObject _activeObj = Selection.activeGameObject;
            PrefabAssetType _type = PrefabUtility.GetPrefabAssetType(_activeObj);
            if (_type == PrefabAssetType.Regular)
            {//常规prefab 
                GameObject root = PrefabUtility.GetOutermostPrefabInstanceRoot(_activeObj);
                PrefabUtility.SaveAsPrefabAsset(root, PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(root));
            }
            else
            {
                Debug.LogError("选中的不是一个Prefab实例！！！");
            }
            
            AssetDatabase.SaveAssets();
            Inject2Lua();
        }

        /// <summary>
        /// 将节点列表注入到lua
        /// </summary>
        private void Inject2Lua()
        {
            string _luaFilePath;
            string _luaParentDirPath;
            LuaBehavior _lua = binder.gameObject.GetComponent<LuaBehavior>();

            List<string> codeBlockList = new List<string>();
            if (_lua)
            {
                if (!string.IsNullOrEmpty(_lua.filename))
                {
                    _luaFilePath = Path.Combine(LUA_PATH, _lua.filename)+ LUA_FILE_EXTENSION;
                    _luaParentDirPath = LUA_PATH;
                    if (_lua.filename.Contains("/"))
                    {
                        _luaParentDirPath = Path.Combine(LUA_PATH, _lua.filename.Substring(0, _lua.filename.LastIndexOf('/')));
                    }
                    //Debug.Log($"[ComponentBinderEditor.OnInspectorGUI] _luaFilePath = {_luaFilePath} , _luaParentDirPath = {_luaParentDirPath}");

                    if (!Directory.Exists(_luaParentDirPath))
                    {
                        Directory.CreateDirectory(_luaParentDirPath);
                    }


                    if (File.Exists(_luaFilePath))
                    {
                        StreamReader _reader = new StreamReader(_luaFilePath, System.Text.Encoding.UTF8);
                        string _line;
                        bool _isCodeBlock = true;
                        while (!_reader.EndOfStream)
                        {
                            _line = _reader.ReadLine();
                            if (_line == CLASS_DEFINE_START)
                            {
                                _isCodeBlock = false;
                                continue;
                            }
                            else if (_line == CLASS_DEFINE_END)
                            {
                                _isCodeBlock = true;
                                continue;
                            }

                            if (_isCodeBlock)
                            {
                                codeBlockList.Add(_line);
                            }
                        }
                        _reader.Close();
                        _reader.Dispose();
                    }

                    StreamWriter _writer = new StreamWriter(_luaFilePath, false, System.Text.Encoding.UTF8);
                    _writer.WriteLine(CLASS_DEFINE_START);
                    if (nodeList != null)
                    {
                        foreach (var node in nodeList)
                        {
                            if (node.index == -1)
                            {
                                _writer.WriteLine("---@field {0} {1}", node.key, node.type);
                            }
                            else
                            {
                                _writer.WriteLine("---@field {0}[{1}] {2}", node.key, node.index, node.type);
                            }
                        }
                    }
                    _writer.WriteLine("---@class {0} : Base", Path.GetFileNameWithoutExtension(_luaFilePath));
                    _writer.WriteLine(CLASS_DEFINE_END);

                    foreach (var code in codeBlockList)
                    {
                        _writer.WriteLine(code);
                    }

                    _writer.Close();
                    _writer.Dispose();

                }
            }
        }
    }
}
