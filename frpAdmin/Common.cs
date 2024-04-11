using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Linq;
using System.Reflection;

namespace frpAdmin
{
    internal class Common
    {

        // 安全检查校验数据库并且返回数据库的目录
        public static string DatabaseValidationAndGetPath() {
            string DatebaseFile = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + @"\datebase";
            
            string DatebasePath = Path.Combine(DatebaseFile, "database.json");

            if (!Directory.Exists(DatebaseFile)) 
            {
                Directory.CreateDirectory(DatebaseFile);
            }

            if (!File.Exists(DatebasePath))
            {
                string DateBaseJson = JsonConvert.SerializeObject(new object[] { }, Formatting.Indented);
                File.WriteAllText(DatebasePath, DateBaseJson);
            }

            return DatebasePath;
        }

        // 读取数据表
        public static JObject DateBaseRead() {
            try
            {
                string databasePath = DatabaseValidationAndGetPath();
                string databaseJson = File.ReadAllText(databasePath);
                JArray databaseJsonArray = JArray.Parse(databaseJson);

                return new JObject
                {
                    ["code"] = "success",
                    ["data"] = databaseJsonArray
                };
            }
            catch (Exception ex)
            {
                return new JObject
                {
                    ["code"] = "error",
                    ["message"] = ex.Message
                };
            }
        }

        // 读取数据表通过ID
        public static JObject DateBaseReadByID(int ReadID)
        {
            try
            {
                string databasePath = DatabaseValidationAndGetPath();
                string databaseJson = File.ReadAllText(databasePath);
                JArray databaseJsonArray = JArray.Parse(databaseJson);

                var ReadItem = databaseJsonArray.FirstOrDefault(item => item["id"] != null && (int)item["id"] == ReadID);

                return new JObject
                {
                    ["code"] = "success",
                    ["data"] = ReadItem
                };
            }
            catch (Exception ex)
            {
                return new JObject
                {
                    ["code"] = "error",
                    ["message"] = ex.Message
                };
            }
        }

        // 给数据库添加成员
        public static JObject DateBaseAddItem(string ItemName, string ItemPact, string ItemServerIP, int ItemServerPort, string ItemRemotePassword,int ItemRemotePort,int ItemLocalPort) {
            try { 
                
                // 写入库
                
                string DateBasePath = DatabaseValidationAndGetPath();
                string DateBaseJson = File.ReadAllText(DateBasePath);
                JArray DateBaseJsonArray = JArray.Parse(DateBaseJson);

                if (DateBaseJsonArray.Any(item =>
                    item["name"] != null && item["name"].ToString() == ItemName
                )) {
                    return new JObject {
                        ["code"] = "error",
                        ["message"] = "计划已存在，请换个名字！",
                    };
                }

                int maxId = DateBaseJsonArray.Select(item => (int)item["id"]).DefaultIfEmpty(0).Max();
                int newItemId = maxId + 1;

                JObject AddItemData = new JObject
                {
                    ["id"] = newItemId,
                    ["name"] = ItemName,
                    ["pact"] = ItemPact,
                    ["serverip"] = ItemServerIP,
                    ["serverport"] = ItemServerPort,
                    ["serverpassword"] = ItemRemotePassword,
                    ["remoteport"] = ItemRemotePort,
                    ["localport"] = ItemLocalPort,
                };

                DateBaseJsonArray.Add(AddItemData);

                string DateBaseOutputJson = DateBaseJsonArray.ToString(Formatting.Indented);
                File.WriteAllText(DateBasePath, DateBaseOutputJson);

                // 写入到列表中

                JObject AddListResult = Main.Instance.ListAddItem(newItemId, ItemName, ItemPact, ItemServerIP + ":" + ItemServerPort.ToString(), ItemRemotePassword, ItemRemotePort, ItemLocalPort);
                if (AddListResult["code"].ToString() != "success") {
                    return new JObject
                    {
                        ["code"] = "error",
                        ["message"] = $"计划添加成功，但是更新到列表中失败！{AddListResult["code"].ToString()}",
                    };
                }

                return new JObject
                {
                    ["code"] = "success",
                    ["message"] = "计划添加成功！",
                };
            }
            catch (Exception ex)
            {
                return new JObject
                {
                    ["code"] = "error",
                    ["message"] = ex.Message,
                };
            }
        }


        // 给数据库删除成员
        public static JObject DateBaseRemoveItem(int itemId)
        {
            try
            {
                string databasePath = DatabaseValidationAndGetPath();
                string databaseJson = File.ReadAllText(databasePath);
                JArray databaseJsonArray = JArray.Parse(databaseJson);

                var itemToRemove = databaseJsonArray.FirstOrDefault(item => item["id"] != null && (int)item["id"] == itemId);

                if (itemToRemove == null)
                {
                    return new JObject
                    {
                        ["code"] = "error",
                        ["message"] = "未找到指定的条目，无法删除！"
                    };
                }

                databaseJsonArray.Remove(itemToRemove);

                string updatedJson = databaseJsonArray.ToString(Formatting.Indented);
                File.WriteAllText(databasePath, updatedJson);

                return new JObject
                {
                    ["code"] = "success",
                    ["message"] = "计划删除成功！"
                };
            }
            catch (Exception ex)
            {
                return new JObject
                {
                    ["code"] = "error",
                    ["message"] = ex.Message
                };
            }
        }




    }
}
