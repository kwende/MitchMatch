using DontPanic.CV.Utilities.Serialize;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Common
{
    public static class Serializer
    {
        public static T Deserialize<T>(string filePath)
        {
            byte[] bytes = File.ReadAllBytes(filePath);
            return GenericDataContractSerializer.DeserializeBinary<T>(bytes);
        }

        public static void Serialize<T>(T toSerialize, string filePath)
        {
            var bytes = GenericDataContractSerializer.SerializeBinary(toSerialize);
            File.WriteAllBytes(filePath, bytes);
        }
    }
}
