using Newtonsoft.Json;
using Newtonsoft.Json.Bson;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using TS3QueryLib.Net.Core;

namespace SunServices.Helpers
{
    public class FileDataHelper
    {
        public static void Write(object obj, string name)
        {
            Directory.CreateDirectory("Data");
            MemoryStream ms = new MemoryStream();
            using (BsonDataWriter writer = new BsonDataWriter(ms))
            {
                JsonSerializer serializer = new JsonSerializer();
                serializer.Serialize(writer, obj);
                FileStream file = new FileStream("Data/" + name, FileMode.Create, FileAccess.Write);
                ms.WriteTo(file);
                file.Close();
                ms.Close();
            }
        }

        public static T Read<T>(string name)
        {
            using (FileStream file = new FileStream("Data/" + name, FileMode.Open, FileAccess.Read))
            {
                byte[] bytes = new byte[file.Length];
                file.Read(bytes, 0, (int)file.Length);
                MemoryStream ms = new MemoryStream(bytes);

                using (BsonDataReader reader = new BsonDataReader(ms))
                {
                    if (typeof(T).Name == "List`1")
                    {
                        reader.ReadRootValueAsArray = true;
                    }
                    JsonSerializer serializer = new JsonSerializer();
                    return serializer.Deserialize<T>(reader);
                }


            }

        }

        public static T Read<T>(string name, string absolutePath)
        {
            using (FileStream file = new FileStream(absolutePath + "/" + name, FileMode.Open, FileAccess.Read))
            {
                byte[] bytes = new byte[file.Length];
                file.Read(bytes, 0, (int)file.Length);
                MemoryStream ms = new MemoryStream(bytes);

                using (BsonDataReader reader = new BsonDataReader(ms))
                {
                    if (typeof(T).Name == "List`1")
                    {
                        reader.ReadRootValueAsArray = true;
                    }
                    JsonSerializer serializer = new JsonSerializer();
                    return serializer.Deserialize<T>(reader);
                }


            }

        }
    }
}
