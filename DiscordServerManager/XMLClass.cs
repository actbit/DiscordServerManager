using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace DiscordServerManager
{
    internal class XMLClass
    {
        public static string SaveToFile<T>(T control)
        {
            var writer = new StringWriter(); // 出力先のWriterを定義
            var serializer = new XmlSerializer(typeof(T));
            serializer.Serialize(writer, control);

            var xml = writer.ToString();
            //Console.WriteLine(xml);


            return xml;
        }

        public static T LoadFromFile<T>(string s)
        {
            var serializer = new XmlSerializer(typeof(T));
            var deserializedBook = serializer.Deserialize(new StringReader(s));
            return (T)(deserializedBook ?? throw new InvalidOperationException("デシリアライズに失敗しました"));
        }
    }
}
