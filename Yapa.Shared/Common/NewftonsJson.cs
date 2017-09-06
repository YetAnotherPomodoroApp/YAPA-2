using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using YAPA.Shared.Contracts;

namespace Yapa.Shared.Common
{
    public class NewftonsJson : IJson
    {
        public string Serialize(object obj)
        {
            return JsonConvert.SerializeObject(obj);
        }

        public T Deserialize<T>(string obj)
        {
            return JsonConvert.DeserializeObject<T>(obj);
        }

        public T ConvertToType<T>(object value)
        {
            if (value == null)
            {
                return default(T);
            }
            else if (value.GetType() == typeof(T))
            {
                return (T)value;
            }
            else if (typeof(T).GetTypeInfo().IsEnum)
            {
                return (T)Enum.ToObject(typeof(T), value);
            }
            else if (typeof(T).GetTypeInfo().IsValueType || value is string)
            {
                return (T)Convert.ChangeType(value, typeof(T));
            }
            else if (value is JArray)
            {
                return ((JArray)value).ToObject<T>();
            }
            else
            {
                return ((JObject)value).ToObject<T>();
            }
        }

        public bool AreEqual(object valA, object valB)
        {
            if (valA == null && valB == null)
            {
                return true;
            }

            if (valA == null && valB != null
                || valA != null && valB == null)
            {
                return false;
            }

            if (valA.GetType().GetTypeInfo().IsValueType || valA is string)
            {
                return valA.Equals(valB);
            }
            else if (valA.GetType().GetTypeInfo().ImplementedInterfaces.FirstOrDefault(x => x == typeof(IEnumerable)) != null)
            {
                var listA = (IEnumerable)valA;
                var listB = (IEnumerable)((JObject)valB).ToObject(valA.GetType());


                if (Count(listA) != Count(listB))
                {
                    return false;
                }

                foreach (var b in listB)
                {
                    var contains = false;
                    foreach (var a in listA)
                    {
                        contains |= b.ToString().Equals(a);
                    }

                    if (contains == false)
                    {
                        return false;
                    }
                }
                return true;
            }

            return false;
        }

        public int Count(IEnumerable source)
        {
            int c = 0;
            var e = source.GetEnumerator();
            while (e.MoveNext())
                c++;
            return c;
        }
    }
}
