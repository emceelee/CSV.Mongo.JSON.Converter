using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;

namespace CSV.JSON.Utility
{
    public static class CsvUtility
    {
        public static dynamic CreateObject(string[] header, string[] record, string[] stringProperties)
        {
            if(stringProperties == null)
            {
                stringProperties = new string[0];
            }

            var recordObject = new ExpandoObject() as IDictionary<string, object>;

            for (int i = 0; i < header.Length; ++i)
            {
                string headerValue = header[i];
                string recordValue = null;

                if (i < record.Length)
                {
                    recordValue = record[i];
                }

                if (!String.IsNullOrEmpty(recordValue))
                {
                    string[] splitHeaderValue = SplitHeader(headerValue);
                    string currentHeaderValue = splitHeaderValue[0];
                    string lastHeaderValue = splitHeaderValue[splitHeaderValue.Length - 1];
                    bool nestedObject = splitHeaderValue.Length > 1;
                    Object recordValueObject;

                    if (nestedObject)
                    {
                        dynamic nested;
                        recordObject.TryGetValue(currentHeaderValue, out nested);
                        recordObject[currentHeaderValue] = CreateNestedObject(splitHeaderValue.Skip(1).ToArray(), recordValue, nested, stringProperties);
                    }
                    else
                    {
                        recordValueObject = DetermineObjectValue(recordValue, lastHeaderValue, stringProperties);
                        recordObject[headerValue] = recordValueObject;
                    }
                }
            }

            return recordObject;
        }

        private static dynamic CreateNestedObject(string[] splitHeaderValue, string recordValue, dynamic nested, string[] stringProperties)
        {
            dynamic returnObject;

            string currentHeaderValue = splitHeaderValue[0];
            string lastHeaderValue = splitHeaderValue[splitHeaderValue.Length - 1];
            bool nestedObject = splitHeaderValue.Length > 1;

            int currentHeaderValueInteger;
            bool enumerable = Int32.TryParse(currentHeaderValue, out currentHeaderValueInteger);

            if (nested == null)
            {
                if (enumerable)
                {
                    returnObject = new List<dynamic>();
                }
                else
                {
                    returnObject = new ExpandoObject();
                }
            }
            else
            {
                returnObject = nested;
            }

            //IEnumerable
            if (enumerable)
            {
                var listObject = (List<dynamic>)returnObject;
                dynamic currentObject = null;

                //update existing object in list
                if (currentHeaderValueInteger < listObject.Count())
                {
                    currentObject = listObject[currentHeaderValueInteger];
                    listObject[currentHeaderValueInteger] = CreateNestedObject(splitHeaderValue.Skip(1).ToArray(), recordValue, currentObject, stringProperties);
                }
                //Create new object
                else if (currentHeaderValueInteger == listObject.Count())
                {
                    if (nestedObject)
                    {
                        currentObject = CreateNestedObject(splitHeaderValue.Skip(1).ToArray(), recordValue, currentObject, stringProperties);
                    }
                    else
                    {
                        currentObject = DetermineObjectValue(recordValue, lastHeaderValue, stringProperties);
                    }

                    listObject.Add(currentObject);
                }
                else
                {
                    throw new ArgumentOutOfRangeException($"Failed to add index {currentHeaderValueInteger}.  Only {listObject.Count()} objects currently exist.");
                }
            }
            //Property
            else
            {
                var expando = (IDictionary<string, object>)returnObject;

                if (nestedObject)
                {
                    dynamic nestedNext;
                    expando.TryGetValue(currentHeaderValue, out nestedNext);
                    expando[currentHeaderValue] = CreateNestedObject(splitHeaderValue.Skip(1).ToArray(), recordValue, nestedNext, stringProperties);
                }
                else
                {
                    Object recordValueObject = DetermineObjectValue(recordValue, lastHeaderValue, stringProperties);
                    expando[currentHeaderValue] = recordValueObject;
                }
            }
            return returnObject;
        }

        private static Object DetermineObjectValue(string recordValue, string lastHeaderValue, string[] stringProperties)
        {

            Object recordValueObject = recordValue;
            DateTime recordValueDate;
            if (DateTime.TryParse(recordValue, out recordValueDate))
            {
                recordValueObject = DateTime.SpecifyKind(recordValueDate, DateTimeKind.Utc);
            }

            int recordValueInt;
            if (Int32.TryParse(recordValue, out recordValueInt))
            {
                if (!stringProperties.Contains(lastHeaderValue.ToLower()))
                {
                    recordValueObject = recordValueInt;
                }
            }
            else
            {
                double recordValueDouble;
                if (Double.TryParse(recordValue, out recordValueDouble))
                {
                    if (!stringProperties.Contains(lastHeaderValue.ToLower()))
                    {
                        recordValueObject = recordValueDouble;
                    }
                }
            }

            bool recordValueBool;
            if (Boolean.TryParse(recordValue, out recordValueBool))
            {
                recordValueObject = recordValueBool;
            };

            return recordValueObject;
        }

        private static string[] SplitHeader(string header)
        {
            return header.Split('/');
        }
    }
}
