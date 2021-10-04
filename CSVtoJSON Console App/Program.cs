using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace CSVtoJSON_Console_App
{
    public static class Program
    {
        public const int comma = 44;
        public const int doubleQuotes = 34;
        public const int lineFeed = 10;
        public const int carriageReturn = 13;

        public enum endOf
        {
            notTheEnd = 1, cell, cell_startEndQuotes, row, row_startEndQuotes, file, file_startEndQuotes
        }

        public enum jsonValueType
        {
            stringValue = 1, numberValue, boolValue, nullValue
        }

        static void Main(string[] args)
        {
            //file path
            string path = Directory.GetCurrentDirectory();
            string inputFilename = "sample0.csv";
            string outputFilename = "sample0.json";
            bool includeHeaderRow = true;

            Console.WriteLine("Enter input file name:");
            inputFilename = Console.ReadLine();
            Console.WriteLine("Enter output file name:");
            outputFilename = Console.ReadLine();
            Console.WriteLine("Is first row a header row?(true/false)");
            string isHeaderRow = Console.ReadLine();
            bool isBool = false;
            if (Boolean.TryParse(isHeaderRow, out isBool))
            {
                includeHeaderRow = Convert.ToBoolean(isHeaderRow);
            }
            else
            {
                includeHeaderRow = false;
            }

            var sourceFilePath = path + "\\files\\" + inputFilename;
            var targetFilePath = path + "\\files\\" + outputFilename;

            try
            {
                // read character by character from file and put them into a dictionary of linked lists
                var jsonSchema = ReadAsString(sourceFilePath, includeHeaderRow);
                // write to json file
                jsonSchema.WriteToFile(targetFilePath, includeHeaderRow);

                return;
            }
            catch (IndexOutOfRangeException)
            {
                throw;
            }
            catch { throw; }
        }

        private static JsonSchema ReadAsString(string filePath, bool includeHeaderRow)
        {
            #region initialization
            char[] c = new char[1];
            StringBuilder sb = new StringBuilder();
            int colIndex = 0; // column number counter
            int rowIndex = 0; // row number counter
            int pos = 0; // current position of a character being read at each cell
            bool startsWithDoubleQuote = false;
            bool endsWithDoubleQuote = false;
            bool doubleQuoteNotAtStart = false;
            int doubleQuoteCount = 0;
            endOf endType = endOf.notTheEnd;
            JsonSchema js = new JsonSchema();
            #endregion

            try
            { 
                #region convert CSV data into a dictionary (a list of key-value-pairs)
                using (var reader = new StreamReader(File.OpenRead(filePath)))
                {
                    try
                    {
                        bool isEndOfFile = false;
                        if (reader.Peek() < 0)
                        {
                            isEndOfFile = true;
                            reader.Close();
                            return js;
                        }

                        do
                        {
                            endType = c[0].GetAction(isEndOfFile, sb.ToString(), startsWithDoubleQuote, endsWithDoubleQuote, doubleQuoteNotAtStart, doubleQuoteCount);
                            switch (endType)
                            {
                                case endOf.cell:
                                    // sb.Remove(sb.Length - 1, 1) to remove comma
                                    ProcessCellValue(colIndex, rowIndex, js.keys, js.jsonObj, includeHeaderRow, sb.Remove(sb.Length - 1, 1).ToString());
                                    colIndex++;
                                    pos = 0;
                                    sb.Clear();
                                    startsWithDoubleQuote = false;
                                    endsWithDoubleQuote = false;
                                    doubleQuoteNotAtStart = false;
                                    doubleQuoteCount = 0;
                                    break;
                                case endOf.cell_startEndQuotes:
                                    // remove leading double quote and ending double quote and comma
                                    sb.Remove(sb.Length - 1, 1); // remove comma
                                    if (sb[sb.Length - 1] == doubleQuotes) // remove " " ,
                                    {
                                        sb.Remove(0, 1); // remove leading double quote
                                        sb.Remove(sb.Length - 2, 2); // remove trialing double quote with its escape character '\'
                                    }
                                    else
                                    {
                                        sb.Insert(0, '\\');
                                    }  
                                    ProcessCellValue(colIndex, rowIndex, js.keys, js.jsonObj, includeHeaderRow, sb.ToString());
                                    colIndex++;
                                    pos = 0;
                                    sb.Clear();
                                    startsWithDoubleQuote = false;
                                    endsWithDoubleQuote = false;
                                    doubleQuoteNotAtStart = false;
                                    doubleQuoteCount = 0;
                                    break;
                                case endOf.row:
                                    ProcessCellValue(colIndex, rowIndex, js.keys, js.jsonObj, includeHeaderRow, sb.ToString());
                                    // In some cases, at the end of a row, it contains both line feed and carriage return.
                                    // peek next charact and see if it's \r or \n, advance by another character
                                    if (reader.Peek() == lineFeed || reader.Peek() == carriageReturn)
                                        reader.Read(c, 0, 1); // advance by 1 character
                                    colIndex = 0;
                                    pos = 0;
                                    sb.Clear();
                                    startsWithDoubleQuote = false;
                                    endsWithDoubleQuote = false;
                                    doubleQuoteNotAtStart = false;
                                    doubleQuoteCount = 0;
                                    rowIndex++;
                                    break;
                                case endOf.row_startEndQuotes:
                                    // remove leading double quote and ending double quote and linefeed
                                    sb.Remove(sb.Length - 1, 1); // remove lineFeed or carriageReturn
                                    if (sb[sb.Length - 1] == doubleQuotes) // remove " " \r or \n
                                    {
                                        sb.Remove(0, 1); // remove leading double quote
                                        sb.Remove(sb.Length - 2, 2); // remove trialing double quote with its escape character '\'
                                    }
                                    else
                                    {
                                        sb.Insert(0, '\\');
                                    }
                                    ProcessCellValue(colIndex, rowIndex, js.keys, js.jsonObj, includeHeaderRow, sb.ToString());
                                    // In some cases, at the end of a row, it contains both line feed and carriage return.
                                    // peek next charact and see if it's \r or \n, advance by another character
                                    if (reader.Peek() == lineFeed || reader.Peek() == carriageReturn)
                                        reader.Read(c, 0, 1); // advance by 1 character
                                    colIndex = 0;
                                    pos = 0;
                                    sb.Clear();
                                    startsWithDoubleQuote = false;
                                    endsWithDoubleQuote = false;
                                    doubleQuoteNotAtStart = false;
                                    doubleQuoteCount = 0;
                                    rowIndex++;
                                    break;
                                case endOf.file:
                                    ProcessCellValue(colIndex, rowIndex, js.keys, js.jsonObj, includeHeaderRow, sb.ToString());
                                    sb.Clear();
                                    endType = endOf.file;
                                    break;
                                case endOf.file_startEndQuotes:
                                    // remove leading double quote and ending double quote
                                    if (sb[sb.Length - 1] == doubleQuotes) // remove " " \r or \n
                                    {
                                        sb.Remove(0, 1); // remove leading double quote
                                        sb.Remove(sb.Length - 2, 2); // remove trialing double quote with its escape character '\'
                                    }
                                    else
                                    {
                                        sb.Insert(0, '\\');
                                    }
                                    ProcessCellValue(colIndex, rowIndex, js.keys, js.jsonObj, includeHeaderRow, sb.ToString());
                                    sb.Clear();
                                    endType = endOf.file;
                                    break;
                                case endOf.notTheEnd:
                                    if (reader.Read(c, 0, 1) > 0)
                                    {
                                        if ((int)c[0] == doubleQuotes)
                                        {
                                            doubleQuoteCount++;
                                            if (reader.Peek() == comma)
                                                endsWithDoubleQuote = true;
                                        }
                                        
                                        if (pos == 0)
                                            startsWithDoubleQuote = ((int)c[0] == doubleQuotes);
                                        else
                                        {
                                            // double quote that's not at position 0
                                            doubleQuoteNotAtStart = (!doubleQuoteNotAtStart) ? ((int)c[0] == doubleQuotes) : doubleQuoteNotAtStart;
                                            if ((int)c[0] == doubleQuotes)
                                                sb.Append('\\');
                                        }
                                        
                                        sb.Append(c[0]);
                                        pos++;
                                    }
                                    else
                                        isEndOfFile = true;
                                    break;
                                default:
                                    break;
                            }

                        } while (endType != endOf.file);
                    }
                    catch
                    {
                        reader.Close();
                        throw;
                    }
                }
                #endregion

                return js;
            }
            catch
            {
                throw;
            }
        }

        private static endOf GetAction(this char c, bool isEndOfFile, string strVal, bool startsWithDoubleQuote, bool endsWithDoubleQuote, bool doubleQuoteNotAtStart, int doubleQuoteCount)
        {
            try
            {
                // If there's no more character to read. it's the end of file
                //if (c < 0)
                //    return endOf.file;
                if (isEndOfFile || (int)c == comma || (int)c == lineFeed || (int)c == carriageReturn)
                {
                    int action = 0;
                    // case: "....", starts and ends with a double quote with an even number of double quotes inside
                    if (startsWithDoubleQuote && endsWithDoubleQuote && doubleQuoteCount % 2 == 0)
                    {
                        Regex rx1 = new Regex("^\"(.((\n|\r)|(\r\n)?)(\"*?))*?\"(,|\n|\r|(\r\n))");
                        if (isEndOfFile)
                            rx1 = new Regex("^\".*((\n|\r)|(\r\n)?).*\"");
                        var mat = rx1.Matches(strVal);
                        action = mat.Count;
                    }
                    // case: "..."...", starts and ends with a double quote but with an odd number of double quotes inside
                    else if (startsWithDoubleQuote && endsWithDoubleQuote && doubleQuoteCount % 2 > 0)
                    {
                        return endOf.notTheEnd;
                    }
                    // case: "..."..., starts but does not end with a double quote with an even number of double quotes inside
                    else if (startsWithDoubleQuote && !endsWithDoubleQuote && doubleQuoteCount % 2 == 0)
                    {
                        Regex rx1 = new Regex("^\"(.((\n|\r)|(\r\n)?)(\"*?))*?(,|\n|\r|(\r\n))"); // ("^\".*((\n|\r)|(\r\n)?).*\".*(,|\n|\r|(\r\n))"); // sees "Ford"Explorer, E350"1997", as two items
                        
                        if (isEndOfFile)
                            rx1 = new Regex("^\".*((\n|\r)|(\r\n)?).*\".*");
                        var mat = rx1.Matches(strVal);
                        action = mat.Count;
                    }
                    // case: "..."..."..., starts but does not end with a double quote but with an odd number of double quotes inside
                    else if (startsWithDoubleQuote && !endsWithDoubleQuote && doubleQuoteCount % 2 > 0)
                    {
                        return endOf.notTheEnd;
                    }
                    // case: ..."..."...",
                    else if (!startsWithDoubleQuote && doubleQuoteCount % 2 > 0)
                    {
                        return endOf.notTheEnd;
                    }
                    // case: ..."...,..."...,
                    else
                    {
                        if (doubleQuoteNotAtStart)
                        {
                            Regex rx2 = new Regex("^(?!\")((.|\"|\n|\r)*)(,|\n|\r|(\r\n))");
                            if (isEndOfFile)
                                rx2 = new Regex("^(?!\")((.|\"|\n|\r)*)");
                            var mat = rx2.Matches(strVal);
                            action = mat.Count;
                        }
                        else
                        {
                            Regex rx3 = new Regex(".*((.|\"|\n|\r)*)(,|\n|\r|(\r\n))");
                            if (isEndOfFile)
                                rx3 = new Regex(".*((.|\"|\n|\r)*)");
                            var mat = rx3.Matches(strVal);
                            action = mat.Count;
                        }
                    }

                    if (action == 1)
                    {
                        if (startsWithDoubleQuote)
                        {
                            if (isEndOfFile)
                                return endOf.file_startEndQuotes;
                            else if ((int)c == comma)
                                return endOf.cell_startEndQuotes;
                            else
                                return endOf.row_startEndQuotes;
                        }
                        else
                        {
                            if (isEndOfFile)
                                return endOf.file;
                            else if ((int)c == comma)
                                return endOf.cell;
                            else
                                return endOf.row;
                        }
                        
                    }
                }

                if (isEndOfFile)
                    return endOf.file;

                return endOf.notTheEnd;
            }
            catch
            {
                throw;
            }
        }

        private static void ProcessCellValue(int colIndex, int rowIndex, List<Tuple<string, jsonValueType>> keys, Dictionary<string, LinkedList<string>> jsonObj, bool includeHeaderRow, string value)
        {
            try
            {
                if (rowIndex == 0) // If it's row 0
                {
                    // if row 0 is a header row, use the string value as a dicionary key, otherwise set set NoTile1, Notile2, Notile3, ... as dicionary key
                    // 
                    Tuple<string, jsonValueType> key = new Tuple<string, jsonValueType>(
                        (includeHeaderRow) ? value.ToString() : "Column" + (colIndex + 1).ToString(),
                        (includeHeaderRow) ? jsonValueType.nullValue : Helper.GetValueType(value)
                        );
                    
                    // add key into the keys list/array to help us remember the index (colIndex) of each key later
                    keys.Add(key);

                    LinkedList<string> jsonNode = new LinkedList<string>();

                    // if row 0 isn't a header row, add a new node to the linked list
                    if (!includeHeaderRow) 
                        jsonNode.AddLast(value.ToString().Trim());

                    // add a new key/value pair into a dictionary 
                    jsonObj.Add(key.Item1, jsonNode);
                }
                else // row > 0
                {
                    // check to see if the current row's size is greater the header size.
                    if (colIndex + 1 > keys.Count)
                    {
                        Console.WriteLine("Row {0} column {1} of the CSV file, near {2} contains too many elements", rowIndex.ToString(), colIndex.ToString(), value.Replace('\r', '\0').Replace('\n', '\0'));
                        throw new IndexOutOfRangeException("Row " + rowIndex.ToString() + " column " + colIndex.ToString() + " of the CSV file, near '" + value.ToString() + "' contains too many elements");
                    }
                    else
                    {
                        // get the dictionary key name by column index (colIndex)
                        // keep track of the value type. 
                        // As long as the previous detected type isn't a string, we need to get the value type again
                        // if the new value type isn't the same as before, we need to change the type to string
                        if (keys[colIndex].Item2 != jsonValueType.stringValue)
                            keys[colIndex] = new Tuple<string, jsonValueType>(
                                keys[colIndex].Item1,
                                Helper.GetValueType(value.Trim())
                            );

                        // find the linked list by key name and add a new node to the linked list
                        jsonObj[keys[colIndex].Item1].AddLast(value.Trim());
                    }
                }

            }
            catch
            {
                throw;
            }
        }

    }

    public static class Helper
    {
        public static void WriteToFile(this JsonSchema jsonSchema, string filePath, bool includeHeaderRow)
        {
            StreamWriter sw = new StreamWriter(@filePath);
            try
            {
                int count1 = 0;
                sw.Write('[');
                var item1 = jsonSchema.jsonObj.FirstOrDefault();
                
                // empty file
                if (jsonSchema.jsonObj.Count == 0 || (jsonSchema.jsonObj.Count == 1 && jsonSchema.keys[0].Item1 == "" && jsonSchema.jsonObj[jsonSchema.keys[0].Item1].Count == 0) ||(jsonSchema.jsonObj.Count == 1 && jsonSchema.keys[0].Item1 == "" && jsonSchema.jsonObj[jsonSchema.keys[0].Item1].Count == 1 && jsonSchema.jsonObj[jsonSchema.keys[0].Item1].First.Value == ""))
                {
                    sw.Write(']');
                    jsonSchema.jsonObj.Clear();
                    sw.Close();
                    return;
                }

                // 
                if (includeHeaderRow && item1.Value.First == null)
                {
                    if (count1 > 0)
                        sw.Write(',');

                    sw.Write('{');
                    int count2 = 0;
                    foreach (KeyValuePair<string, LinkedList<string>> kvp in jsonSchema.jsonObj)
                    {
                        if (count2 > 0)
                            sw.Write(',');

                        sw.Write(string.Format("\"{0}\": null", kvp.Key));
                        count2++;
                    }
                    sw.Write('}');
                }
                
                while (item1.Value.First != null)
                {
                    if (count1 > 0)
                        sw.Write(',');

                    sw.Write('{');
                    int count2 = 0;
                    foreach (KeyValuePair<string, LinkedList<string>> kvp in jsonSchema.jsonObj)
                    {
                        if (count2 > 0)
                            sw.Write(',');

                        if (includeHeaderRow)
                        {
                            if (jsonSchema.keys[count2].Item2 != Program.jsonValueType.stringValue)
                                sw.Write(string.Format("\"{0}\": {1}", kvp.Key, kvp.Value.First.Value));
                            else
                                sw.Write(string.Format("\"{0}\": \"{1}\"", kvp.Key, kvp.Value.First.Value));
                        }
                        else
                        {
                            if (jsonSchema.keys[count2].Item2 != Program.jsonValueType.stringValue)
                                sw.Write(string.Format("{0}", kvp.Value.First.Value));
                            else
                                sw.Write(string.Format("\"{0}\"", kvp.Value.First.Value));
                        }

                        kvp.Value.RemoveFirst();
                        count2++;
                    }

                    sw.Write('}');
                    count1++;

                }

                sw.Write(']');
                jsonSchema.jsonObj.Clear();
                sw.Close();
            }
            catch
            {
                jsonSchema.jsonObj.Clear();
                sw.Close();
                throw;
            }
        }

        public static LinkedList<T> Get<T>(this Dictionary<string, object> jsonObj, string key)
        {
            return (LinkedList<T>)jsonObj[key];
        }

        public static Program.jsonValueType GetValueType(string value)
        {
            // 1. First we need to test and see if value is numeric
            if (value.IsNumeric(style: NumberStyles.AllowExponent))
                return Program.jsonValueType.numberValue;
            else if (value.IsBoolean())
                return Program.jsonValueType.boolValue;
            else
                return Program.jsonValueType.stringValue;
        }

        private static bool IsNumeric(this string str, NumberStyles style = NumberStyles.Number, CultureInfo culture = null)
        {
            double num;
            if (culture == null) culture = CultureInfo.InvariantCulture;
            return Double.TryParse(str, style, culture, out num) && !String.IsNullOrWhiteSpace(str);
        }

        private static bool IsBoolean(this string str)
        {
            bool boolVal;
            return Boolean.TryParse(str, out boolVal) && !String.IsNullOrWhiteSpace(str);
        }
    }

    public class JsonSchema
    {
        public List<Tuple<string, Program.jsonValueType>> keys { get; set; }
        public Dictionary<string, LinkedList<string>> jsonObj { get; set; }

        public JsonSchema()
        {
            keys = new List<Tuple<string, Program.jsonValueType>>();
            jsonObj = new Dictionary<string, LinkedList<string>>();
        }
    }
}

