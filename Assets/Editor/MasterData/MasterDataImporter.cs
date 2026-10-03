using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

namespace Editor.MasterData
{
    public static class MasterDataImporter
    {
        /// <summary>
        /// CSVが格納されているフォルダパス
        /// </summary>
        private const string CSV_FOLDER_PATH = "Assets/Data/CSV";

        /// <summary>
        /// ScriptableObject（SO）として出力するフォルダパス
        /// </summary>
        private const string EXPORT_FOLDER_PATH = "Assets/Data/MastaerData";

        private const string MASTER_DATA_CONTAINER_FORMAT = "TF.MasterData.{0},Assembly-CSharp";
        private const string MASTER_DATA_RECORD_FORMAT = "TF.MasterData.{0}Record,Assembly-CSharp";

        [MenuItem("Tools/CSVを一括でMasterDataに変換")]
        public static void GenerateAllFromCSV()
        {
            // ----- フォルダが存在しない場合は作成 -----
            if (!Directory.Exists(CSV_FOLDER_PATH))
            {
                Directory.CreateDirectory(CSV_FOLDER_PATH);
            }

            if (!Directory.Exists(EXPORT_FOLDER_PATH))
            {
                Directory.CreateDirectory(EXPORT_FOLDER_PATH);
            }

            ///フォルダ内の全てのCSVファイルパスを取得
            string[] csvFiles = Directory.GetFiles(CSV_FOLDER_PATH, "*.csv");

            foreach (string csvPath in csvFiles)
            {
                // ファイル名（例："EnemyData"）を取得
                string fileName = Path.GetFileNameWithoutExtension(csvPath);

                // ファイル名から生成すべきSOのクラス名とレコードのクラス名を推測
                Type soType = Type.GetType(string.Format(MASTER_DATA_CONTAINER_FORMAT, fileName));
                Type recordType = Type.GetType(string.Format(MASTER_DATA_RECORD_FORMAT, fileName));

                if (soType == null || recordType == null)
                {
                    Debug.LogWarning($"クラスが見つかりません。ファイル：{fileName}");
                    continue;
                }

                // 既にSOが存在している場合は、削除して再生成する。
                string exportPath = $"{EXPORT_FOLDER_PATH}/{fileName}.asset";
                if (File.Exists(exportPath))
                {
                    AssetDatabase.DeleteAsset(exportPath);
                }

                // 新しいSOインスタンスと、データを格納するリストのインスタンスを作成
                ScriptableObject soInstance = ScriptableObject.CreateInstance(soType);
                Type listType = typeof(List<>).MakeGenericType(recordType);
                IList listInstance = (IList)Activator.CreateInstance(listType);

                // CSVの中身を読み込む
                string[] lines = File.ReadAllLines(csvPath);
                if (lines.Length >= 2)
                {
                    // 1行目はヘッダー（IdやAttackPowerなど）
                    string[] headers = lines[0].Split(',');

                    // 2行目以降のデータ処理
                    for (int i = 1; i < lines.Length; i++)
                    {
                        if (string.IsNullOrWhiteSpace(lines[i]))
                        {
                            continue;
                        }

                        string[] values = lines[i].Split(',');

                        // レコードのインスタンスを作成
                        object recordInstance = Activator.CreateInstance(recordType);

                        for (int j = 0; j < headers.Length; j++)
                        {
                            if (j >= values.Length)
                            {
                                break;
                            }

                            string headerName = headers[j].Trim();
                            string stringValue = values[j].Trim();

                            // リフレクションを使ってプロパティを取得
                            PropertyInfo property = recordType.GetProperty(headerName, BindingFlags.Public | BindingFlags.Instance);
                            if (property != null && property.CanWrite)
                            {
                                Type propType = property.PropertyType;
                                var value = ConvertPrimitiveOrEnumValue(stringValue, propType);
                                property.SetValue(recordInstance, value, BindingFlags.NonPublic | BindingFlags.Instance, null, null, null);
                            }
                        }

                        // リストに追加
                        listInstance.Add(recordInstance);
                    }
                }

                PropertyInfo recordProp = soType.GetProperty("Records", BindingFlags.Public | BindingFlags.Instance);
                if (recordProp != null && recordProp.CanWrite)
                {
                    recordProp.SetValue(soInstance, listInstance, BindingFlags.NonPublic | BindingFlags.Instance, null, null, null);
                }

                AssetDatabase.CreateAsset(soInstance, exportPath);

                // 保存したアセットのGUIDを取得する
                string guid = AssetDatabase.AssetPathToGUID(exportPath);

                // 現在のAddressablesの設定ファイルを取得する
                AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;

                if (settings != null)
                {
                    // デフォルトのグループにアセットを登録（エントリを作成）する
                    AddressableAssetGroup group = settings.DefaultGroup;
                    AddressableAssetEntry entry = settings.CreateOrMoveEntry(guid, group);

                    if (entry != null)
                    {
                        // アドレスをCSVの名前（例: "EnemyData"）に設定する
                        entry.address = fileName;

                        // ラベルをSettingsに追加し、そのアセットにラベルを付与する
                        settings.AddLabel(fileName);
                        entry.SetLabel(fileName, true, true);
                    }
                }
                else
                {
                    Debug.LogWarning("AddressableAssetSettingsが見つかりません。Window > Asset Management > Addressables > Groups から設定を作成してください。");
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("全てのCSVの一括返還が完了しました。");
        }

        private static object ConvertPrimitiveOrEnumValue(string value, Type type)
        {
            if (type == typeof(int))
            {
                return string.IsNullOrEmpty(value) ? 0 : int.Parse(value);
            }

            if (type == typeof(uint))
            {
                return string.IsNullOrEmpty(value) ? 0 : uint.Parse(value);
            }

            if (type == typeof(long))
            {
                return string.IsNullOrEmpty(value) ? 0 : long.Parse(value);
            }

            if (type == typeof(ulong))
            {
                return string.IsNullOrEmpty(value) ? 0 : ulong.Parse(value);
            }

            if (type == typeof(double))
            {
                return string.IsNullOrEmpty(value) ? 0 : double.Parse(value);
            }

            if (type == typeof(float))
            {
                return string.IsNullOrEmpty(value) ? 0f : float.Parse(value);
            }

            if (type == typeof(bool))
            {
                return !string.IsNullOrEmpty(value) && bool.Parse(value);
            }

            return value;
        }
    }
}
