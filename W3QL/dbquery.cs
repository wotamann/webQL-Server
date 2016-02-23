using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Reflection;


namespace webQL
{


    // WOTAN CHANGED xxxxxxxxxxxxxxxxx
    // necessary for converting JSON dictionary<string,dynamic> into class zb. arguments
    // http://stackoverflow.com/questions/4943817/mapping-object-to-dictionary-and-vice-versa
    // used in Modul_DB 
    public static class ObjectExtensions
    {
        public static T ToObject<T>(this IDictionary<string, object> source)
            where T : class, new()
        {
            T someObject = new T();
            Type someObjectType = someObject.GetType();

          
            if (source.Count == 0) return someObject;

            foreach (KeyValuePair<string, dynamic> item in source)
            {

                if (item.Equals(null) || item.Value == null)
                {
                    someObjectType.GetProperty(item.Key).SetValue(someObject, null, null);
                    continue;
                }

                var v = item.Value;
                var t = item.Value.GetType();

                if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Dictionary<,>))
                {
                    v = (new System.Web.Script.Serialization.JavaScriptSerializer()).Serialize(item.Value);
                }
                else 
                {
                    v = item.Value.ToString();
                    // Console.WriteLine("PRIMITVE:" + item.Key + " : " + v);
                }

                someObjectType.GetProperty(item.Key).SetValue(someObject,v, null);
            }

            return someObject;
        }

        public static IDictionary<string, object> AsDictionary(this object source, BindingFlags bindingAttr = BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Instance)
        {
            return source.GetType().GetProperties(bindingAttr).ToDictionary
            (
                propInfo => propInfo.Name,
                propInfo => propInfo.GetValue(source, null)
            );

        }
    }
    

    public sealed class DbQuery
    {
        

        #region Private Enums
        private enum enMode : int
        {
            Select,
            Insert,
            Update,
            Delete
        }
        private enum enTableTypes : int
        {
            Table,
            View,
            All
        }
        private enum enResult : int
        {
            Failed,
            Empty,
            Success
        }
        private enum enFieldSize : int
        {
            None,
            Size,
            Precision,
            ScalePrecision
        }
        private enum enCompare : int
        {
            NullOnly,
            YesNoLike,
            YesLike
        }
        private enum enKind
        {
            ShortString,
            LongString,
            UdtString,
            Number,
            Date,
            DateTime,
            Time,
            Boolean,
            Guid,
            Binary,
            Xml,
            Variant
        }
        #endregion

        #region Öffentliche Enums
        public enum ConfigMode : int
        {
            Table,
            View,
            All
        }
        #endregion

        #region Private Schnittstelle IColumnFormat
        private interface IColumnFormat
        {
            Dictionary<string, ColumnFormatInfo> Dictionary { get; }
        }
        #endregion

        #region Private Schnittstelle IDefaultInsertArgs
        private interface IDefaultInsertArgs
        {
            Dictionary<string, InsertInfo> Dictionary { get; }
        }
        #endregion

        #region Private abstrakte Klasse ConverterBase und abgeleitete Klassen
        private abstract class ConverterBase
        {
            #region Geschützte Delegates
            protected static Action CultureChanged = null;
            #endregion

            #region Öffentliche Felder
            public string Format1;
            public string Format2;
            #endregion

            #region Geschützte statische Felder
            protected static CultureInfo mCulture;
            protected static readonly CultureInfo mCultureInvariant = CultureInfo.InvariantCulture;
            #endregion

            #region Konstruktoren
            static ConverterBase()
            {
                SetCulture(CultureInfo.CurrentCulture.LCID);
            }
            public ConverterBase(string format1, string format2)
            {
                this.Format1 = format1;
                this.Format2 = format2;
            }
            #endregion

            #region Abstrakte Methoden
            public abstract void AppendSerialized(SqlDataReader reader, int index, StringBuilder sb);
            public abstract string GetString(object dBvalue);
            public abstract enResult GetValue(string entry, out object dbValue);
            public abstract ConverterBase GetClone(string format1, string format2);
            #endregion

            #region Öffentliche Methoden
            public static void SetCulture(int id)
            {
                try
                {
                    if (mCulture != null && mCulture.LCID == id)
                        return;

                    CultureInfo temp = new CultureInfo(id, false);
                    mCulture = temp;

                    if (CultureChanged != null)
                        CultureChanged();
                }
                catch { }
            }
            public static string GetCultureName()
            {
                return mCulture.Name;
            }
            public static int GetCultureId()
            {
                return mCulture.LCID;
            }
            #endregion
        }
        private sealed class ByteConverter : ConverterBase
        {
            #region Konstruktor
            public ByteConverter(string format1, string format2) : base(format1, format2) { }
            #endregion

            #region Überschriebene Methoden
            public override void AppendSerialized(SqlDataReader rdr, int index, StringBuilder sb)
            {
                if (rdr.IsDBNull(index))
                    sb.Append("null");
                else
                    sb.Append(JsonConvert.SerializeObject(string.Format(mCulture, "{0:" + Format1 + "}", rdr[index])));
            }
            public override string GetString(object dBvalue)
            {
                if (dBvalue == DBNull.Value)
                    return string.Empty;

                try
                {
                    return Convert.ToByte(dBvalue).ToString(Format1, mCulture);
                }
                catch { return string.Empty; }
            }
            public override enResult GetValue(string entry, out object dbValue)
            {
                dbValue = DBNull.Value;

                if (string.IsNullOrWhiteSpace(entry))
                    return enResult.Empty;

                Byte result;

                if (!byte.TryParse(entry, NumberStyles.Integer, mCulture, out result))
                    return enResult.Failed;

                dbValue = result;
                return enResult.Success;
            }
            public override ConverterBase GetClone(string format1, string format2)
            {
                return new Int16Converter(format1, format2);
            }
            #endregion
        }
        private sealed class Int16Converter : ConverterBase
        {
            #region Konstruktor
            public Int16Converter(string format1, string format2) : base(format1, format2) { }
            #endregion

            #region Überschriebene Methoden
            public override void AppendSerialized(SqlDataReader rdr, int index, StringBuilder sb)
            {
                if (rdr.IsDBNull(index))
                    sb.Append("null");
                else
                    sb.Append(JsonConvert.SerializeObject(string.Format(mCulture, "{0:" + Format1 + "}", rdr[index])));
            }
            public override string GetString(object dBvalue)
            {
                if (dBvalue == DBNull.Value)
                    return string.Empty;

                try
                {
                    return Convert.ToInt16(dBvalue).ToString(Format1, mCulture);
                }
                catch { return string.Empty; }
            }
            public override enResult GetValue(string entry, out object dbValue)
            {
                dbValue = DBNull.Value;

                if (string.IsNullOrWhiteSpace(entry))
                    return enResult.Empty;

                Int16 result;

                if (!Int16.TryParse(entry, NumberStyles.Integer, mCulture, out result))
                    return enResult.Failed;

                dbValue = result;
                return enResult.Success;
            }
            public override ConverterBase GetClone(string format1, string format2)
            {
                return new Int16Converter(format1, format2);
            }
            #endregion
        }
        private sealed class Int32Converter : ConverterBase
        {
            #region Konstruktor
            public Int32Converter(string format1, string format2) : base(format1, format2) { }
            #endregion

            #region Überschriebene Methoden
            public override void AppendSerialized(SqlDataReader rdr, int index, StringBuilder sb)
            {
                if (rdr.IsDBNull(index))
                    sb.Append("null");
                else
                    sb.Append(JsonConvert.SerializeObject(string.Format(mCulture, "{0:" + Format1 + "}", rdr[index])));
            }
            public override string GetString(object dBvalue)
            {
                if (dBvalue == DBNull.Value)
                    return string.Empty;

                try
                {
                    return Convert.ToInt32(dBvalue).ToString(Format1, mCulture);
                }
                catch { return string.Empty; }
            }
            public override enResult GetValue(string entry, out object dbValue)
            {
                dbValue = DBNull.Value;

                if (string.IsNullOrWhiteSpace(entry))
                    return enResult.Empty;

                Int32 result;

                if (!Int32.TryParse(entry, NumberStyles.Integer, mCulture, out result))
                    return enResult.Failed;

                dbValue = result;
                return enResult.Success;
            }
            public override ConverterBase GetClone(string format1, string format2)
            {
                return new Int32Converter(format1, format2);
            }
            #endregion

            #region Öffentliche Methoden
            public enResult GetValue(string entry, out int value)
            {
                if (string.IsNullOrWhiteSpace(entry))
                {
                    value = 0;
                    return enResult.Empty;
                }

                if (!Int32.TryParse(entry, NumberStyles.Integer, mCulture, out value))
                    return enResult.Failed;

                return enResult.Success;
            }
            #endregion
        }
        private sealed class Int64Converter : ConverterBase
        {
            #region Konstruktor
            public Int64Converter(string format1, string format2) : base(format1, format2) { }
            #endregion

            #region Überschriebene Methoden
            public override void AppendSerialized(SqlDataReader rdr, int index, StringBuilder sb)
            {
                if (rdr.IsDBNull(index))
                    sb.Append("null");
                else
                    sb.Append(JsonConvert.SerializeObject(string.Format(mCulture, "{0:" + Format1 + "}", rdr[index])));
            }
            public override string GetString(object dBvalue)
            {
                if (dBvalue == DBNull.Value)
                    return string.Empty;

                try
                {
                    return Convert.ToInt64(dBvalue).ToString(Format1, mCulture);
                }
                catch { return string.Empty; }
            }
            public override enResult GetValue(string entry, out object dbValue)
            {
                dbValue = DBNull.Value;

                if (string.IsNullOrWhiteSpace(entry))
                    return enResult.Empty;

                Int64 result;

                if (!Int64.TryParse(entry, NumberStyles.Integer, mCulture, out result))
                    return enResult.Failed;

                dbValue = result;
                return enResult.Success;
            }
            public override ConverterBase GetClone(string format1, string format2)
            {
                return new Int64Converter(format1, format2);
            }
            #endregion
        }
        private sealed class SingleConverter : ConverterBase
        {
            #region Konstruktor
            public SingleConverter(string format1, string format2) : base(format1, format2) { }
            #endregion

            #region Überschriebene Methoden
            public override void AppendSerialized(SqlDataReader rdr, int index, StringBuilder sb)
            {
                if (rdr.IsDBNull(index))
                    sb.Append("null");
                else
                    sb.Append(JsonConvert.SerializeObject(string.Format(mCulture, "{0:" + Format1 + "}", rdr[index])));
            }
            public override string GetString(object dBvalue)
            {
                if (dBvalue == DBNull.Value)
                    return string.Empty;

                try
                {
                    return Convert.ToSingle(dBvalue).ToString(Format1, mCulture);
                }
                catch { return string.Empty; }
            }
            public override enResult GetValue(string entry, out object dbValue)
            {
                dbValue = DBNull.Value;

                if (string.IsNullOrWhiteSpace(entry))
                    return enResult.Empty;

                Single result;

                if (!Single.TryParse(entry, NumberStyles.Any, mCulture, out result))
                    return enResult.Failed;

                dbValue = result;
                return enResult.Success;
            }
            public override ConverterBase GetClone(string format1, string format2)
            {
                return new SingleConverter(format1, format2);
            }
            #endregion
        }
        private sealed class DoubleConverter : ConverterBase
        {
            #region Konstruktor
            public DoubleConverter(string format1, string format2) : base(format1, format2) { }
            #endregion

            #region Überschriebene Methoden
            public override void AppendSerialized(SqlDataReader rdr, int index, StringBuilder sb)
            {
                if (rdr.IsDBNull(index))
                    sb.Append("null");
                else
                    sb.Append(JsonConvert.SerializeObject(string.Format(mCulture, "{0:" + Format1 + "}", rdr[index])));
            }
            public override string GetString(object dBvalue)
            {
                if (dBvalue == DBNull.Value)
                    return string.Empty;

                try
                {
                    return Convert.ToDouble(dBvalue).ToString(Format1, mCulture);
                }
                catch { return string.Empty; }
            }
            public override enResult GetValue(string entry, out object dbValue)
            {
                dbValue = DBNull.Value;

                if (string.IsNullOrWhiteSpace(entry))
                    return enResult.Empty;

                Double result;

                if (!Double.TryParse(entry, NumberStyles.Any, mCulture, out result))
                    return enResult.Failed;

                dbValue = result;
                return enResult.Success;
            }
            public override ConverterBase GetClone(string format1, string format2)
            {
                return new DoubleConverter(format1, format2);
            }
            #endregion
        }
        private sealed class DecimalConverter : ConverterBase
        {
            #region Konstruktor
            public DecimalConverter(string format1, string format2) : base(format1, format2) { }
            #endregion

            #region Überschriebene Methoden
            public override void AppendSerialized(SqlDataReader rdr, int index, StringBuilder sb)
            {
                if (rdr.IsDBNull(index))
                    sb.Append("null");
                else
                    sb.Append(JsonConvert.SerializeObject(string.Format(mCulture, "{0:" + Format1 + "}", rdr[index])));
            }
            public override string GetString(object dBvalue)
            {
                if (dBvalue == DBNull.Value)
                    return string.Empty;

                try
                {
                    return Convert.ToDecimal(dBvalue).ToString(Format1, mCulture);
                }
                catch { return string.Empty; }
            }
            public override enResult GetValue(string entry, out object dbValue)
            {
                dbValue = DBNull.Value;

                if (string.IsNullOrWhiteSpace(entry))
                    return enResult.Empty;

                Decimal result;

                if (!Decimal.TryParse(entry, NumberStyles.Any, mCulture, out result))
                    return enResult.Failed;

                dbValue = result;
                return enResult.Success;
            }
            public override ConverterBase GetClone(string format1, string format2)
            {
                return new DecimalConverter(format1, format2);
            }
            #endregion
        }
        private sealed class DateTimeConverter : ConverterBase
        {
            #region Konstruktor
            public DateTimeConverter(string format1, string format2) : base(format1, format2) { }
            #endregion

            #region Überschriebene Methoden
            public override void AppendSerialized(SqlDataReader rdr, int index, StringBuilder sb)
            {

                if (rdr.IsDBNull(index))
                    sb.Append("null");
                else
                    sb.Append(JsonConvert.SerializeObject(string.Format(mCulture, "{0:" + Format1 + "}", rdr[index])));
            }
            public override string GetString(object dbValue)
            {
                if (dbValue == DBNull.Value)
                    return string.Empty;

                try
                {
                    return Convert.ToDateTime(dbValue).ToString(Format1, mCulture);
                }
                catch { return string.Empty; }
            }
            public override enResult GetValue(string entry, out object dbValue)
            {
                dbValue = DBNull.Value;

                if (string.IsNullOrWhiteSpace(entry))
                    return enResult.Empty;

                DateTime result;

                if (!DateTime.TryParse(entry, mCulture, DateTimeStyles.None, out result))
                    return enResult.Failed;

                dbValue = result;
                return enResult.Success;
            }
            public override ConverterBase GetClone(string format1, string format2)
            {
                return new DateTimeConverter(format1, format2);
            }
            #endregion
        }
        private sealed class DateTimeOffsetConverter : ConverterBase
        {
            #region Konstruktor
            public DateTimeOffsetConverter(string format1, string format2) : base(format1, format2) { }
            #endregion

            #region Überschriebene Methoden
            public override void AppendSerialized(SqlDataReader rdr, int index, StringBuilder sb)
            {

                if (rdr.IsDBNull(index))
                    sb.Append("null");
                else
                    sb.Append(JsonConvert.SerializeObject(string.Format(mCulture, "{0:" + Format1 + "}", rdr[index])));
            }
            public override string GetString(object dbValue)
            {
                if (dbValue == DBNull.Value)
                    return string.Empty;

                try
                {
                    return string.Format(mCulture, "{0:" + Format1 + "}", dbValue);
                }
                catch { return string.Empty; }
            }
            public override enResult GetValue(string entry, out object dbValue)
            {
                dbValue = DBNull.Value;

                if (string.IsNullOrWhiteSpace(entry))
                    return enResult.Empty;

                DateTimeOffset result;

                if (!DateTimeOffset.TryParse(entry, mCulture, DateTimeStyles.None, out result))
                    return enResult.Failed;

                dbValue = result;
                return enResult.Success;
            }
            public override ConverterBase GetClone(string format1, string format2)
            {
                return new DateTimeOffsetConverter(format1, format2);
            }
            #endregion
        }
        private sealed class TimeConverter : ConverterBase
        {
            #region Private statische Felder
            private static Regex mRegex;
            private static readonly string[] mTimePatterns = new string[] 
            {
                "HH mm", "H mm", "H m", "HH m",
                "HH mm ss", "HH mm s", "HH m ss", "HH m s",
                "H mm ss", "H mm s", "H m ss", "H m s",
                "HH", "HHmm", "HHmmss"
            };
            #endregion

            #region Konstruktoren
            static TimeConverter()
            {
                CultureChanged = () =>
                {
                    string sep = Regex.Escape(mCulture.DateTimeFormat.TimeSeparator);
                    mRegex = new Regex(@"\s*" + sep + @"\s*|\s+");
                };

                CultureChanged();
            }
            public TimeConverter(string format1, string format2) : base(format1, format2) { }
            #endregion

            #region Überschriebene Methoden
            public override void AppendSerialized(SqlDataReader rdr, int index, StringBuilder sb)
            {
                if (rdr.IsDBNull(index))
                    sb.Append("null");
                else
                    sb.Append(JsonConvert.SerializeObject
                        (new DateTime(rdr.GetTimeSpan(index).Ticks).ToString(Format1, mCulture)));
            }
            public override string GetString(object dbValue)
            {
                try
                {
                    if (dbValue == DBNull.Value || dbValue == null)
                        return string.Empty;

                    long ticks;

                    if (dbValue is TimeSpan)
                    {
                        ticks = ((TimeSpan)dbValue).Ticks;

                        if (ticks < 0 || ticks >= TimeSpan.TicksPerDay)
                            return string.Empty;
                    }
                    else
                        ticks = 0;

                    return new DateTime(ticks).ToString(Format1, mCulture);

                }
                catch { return string.Empty; }
            }
            public override enResult GetValue(string entry, out object dbValue)
            {
                dbValue = DBNull.Value;

                if (entry == null)
                    return enResult.Empty;

                entry = entry.Trim();

                if (entry.Length == 0)
                    return enResult.Empty;

                if (entry.Length == 1 && char.IsDigit(entry[0]))
                    entry = "0" + entry[0];

                DateTime date;

                if (!DateTime.TryParseExact(mRegex.Replace(entry, " "),
                    mTimePatterns, mCulture, DateTimeStyles.NoCurrentDateDefault, out date))
                    return enResult.Failed;

                dbValue = new TimeSpan(date.Ticks);

                return enResult.Success;
            }
            public override ConverterBase GetClone(string format1, string format2)
            {
                return new TimeConverter(format1, format2);
            }
            #endregion
        }

        // CHANGED BY WOTAN XXXXXXXXXXXXXXXX JSON  Differentiate '' and Null
        private sealed class StringConverter : ConverterBase
        {
            #region Konstruktor
            public StringConverter(string format1, string format2) : base(format1, format2) { }
            #endregion

            #region Überschriebene Methoden
            public override void AppendSerialized(SqlDataReader rdr, int index, StringBuilder sb)
            {
                if (rdr.IsDBNull(index))
                    sb.Append("null");
                else
                    sb.Append(JsonConvert.SerializeObject(rdr.GetString(index).TrimEnd()));
            }
            public override string GetString(object dbValue)
            {
                try
                {
                    return dbValue.ToString().TrimEnd();
                }
                catch { return string.Empty; }
            }
            // CHANGED BY WOTAN XXXXXXXXXXXXXXXX JSON
            public override enResult GetValue(string entry, out object dbValue)
            {
                // here 'entry != string.Empty &&'  catch EMPTY STRING otherwise "" becomes Null!!
                if (entry != string.Empty && string.IsNullOrWhiteSpace(entry))
                {
                    dbValue = DBNull.Value;
                    return enResult.Empty;
                }

                dbValue = entry.Trim();
                return enResult.Success;
            }
            public override ConverterBase GetClone(string format1, string format2)
            {
                return new StringConverter(format1, format2);
            }
            #endregion
        }
        // CHANGED BY WOTAN XXXXXXXXXXXXXXXX JSON Differentiate '' and Null
        private sealed class TextConverter : ConverterBase
        {
            #region Konstruktor
            public TextConverter(string format1, string format2) : base(format1, format2) { }
            #endregion

            #region Überschriebene Methoden
            public override void AppendSerialized(SqlDataReader rdr, int index, StringBuilder sb)
            {
                sb.Append(JsonConvert.SerializeObject(rdr[index]));
            }
            public override string GetString(object dBvalue)
            {
                try
                {
                    return dBvalue.ToString();
                }
                catch { return string.Empty; }
            }
            public override enResult GetValue(string entry, out object dbValue)
            {

                // here 'entry != string.Empty &&'  catch EMPTY STRING otherwise "" becomes Null!!
                if (entry != string.Empty && string.IsNullOrWhiteSpace(entry))
                {
                    dbValue = DBNull.Value;
                    return enResult.Empty;
                }

                dbValue = entry;
                return enResult.Success;
            }
            public override ConverterBase GetClone(string format1, string format2)
            {
                return new TextConverter(format1, format2);
            }
            #endregion
        }
        private sealed class BooleanConverter : ConverterBase
        {
            #region Konstruktor
            public BooleanConverter(string format1, string format2) : base(format1, format2) { }
            #endregion

            #region Überschriebene Methoden
            public override void AppendSerialized(SqlDataReader rdr, int index, StringBuilder sb)
            {
                if (rdr.IsDBNull(index))
                    sb.Append("null");
                else
                    sb.Append(JsonConvert.SerializeObject(rdr.GetBoolean(index) ? Format1 : Format2));
            }
            public override string GetString(object dBvalue)
            {
                if (dBvalue == DBNull.Value)
                    return string.Empty;

                try
                {
                    return Convert.ToBoolean(dBvalue) ? Format1 : Format2;
                }
                catch { return string.Empty; }
            }
            public override enResult GetValue(string entry, out object dbValue)
            {
                if (string.IsNullOrWhiteSpace(entry))
                {
                    dbValue = DBNull.Value;
                    return enResult.Empty;
                }

                entry = entry.Trim();
                StringComparison comparison = StringComparison.OrdinalIgnoreCase;

                if (entry.Equals(Format1, comparison) ||
                    entry.Equals("true", comparison) ||
                    entry.Equals("1"))
                {
                    dbValue = true;
                    return enResult.Success;
                }

                if (entry.Equals(Format2, comparison) ||
                    entry.Equals("false", comparison) ||
                    entry.Equals("0"))
                {
                    dbValue = false;
                    return enResult.Success;
                }

                dbValue = DBNull.Value;
                return enResult.Failed;
            }
            public override ConverterBase GetClone(string format1, string format2)
            {
                return new BooleanConverter(format1, format2);
            }
            #endregion
        }
        // CHANGED BY WOTAN XXXXXXXXXXXXXXXX JSON Differentiate '' and Null
        private sealed class Base64StringConverter : ConverterBase
        {
            #region Öffentliche Felder
            public readonly int MaxLength;
            #endregion

            #region Konstruktor
            public Base64StringConverter(int maxLength, string format1, string format2)
                : base(format1, format2)
            {
                this.MaxLength = maxLength;
            }
            public Base64StringConverter(string format1, string format2) : this(int.MaxValue, format1, format2) { }
            #endregion

            #region Überschriebene Methoden
            public override void AppendSerialized(SqlDataReader rdr, int index, StringBuilder sb)
            {
                if (rdr.IsDBNull(index))
                    sb.Append("null");
                else
                    sb.Append(JsonConvert.SerializeObject(Convert.ToBase64String((byte[])rdr[index])));
            }
            public override string GetString(object dBvalue)
            {
                if (dBvalue == DBNull.Value)
                    return string.Empty;

                try
                {
                    return Convert.ToBase64String((byte[])dBvalue);
                }
                catch { return string.Empty; }
            }
            public override enResult GetValue(string entry, out object dbValue)
            {

                // here 'entry != string.Empty &&'  catch EMPTY STRING otherwise "" becomes Null!!
                if (entry != string.Empty && string.IsNullOrWhiteSpace(entry))
                {
                    dbValue = DBNull.Value;
                    return enResult.Empty;
                }

                try
                {
                    byte[] bytes = Convert.FromBase64String(entry);

                    if (bytes.Length > MaxLength)
                        throw new Exception("'Value too large' - The value can't be stored in Database.");

                    dbValue = bytes;
                    return enResult.Success;
                }
                catch
                {
                    dbValue = DBNull.Value;
                    return enResult.Failed;
                }
            }
            public override ConverterBase GetClone(string format1, string format2)
            {
                return new Base64StringConverter(MaxLength, format1, format2);
            }
            #endregion
        }
        private sealed class GuidConverter : ConverterBase
        {
            #region Konstruktor
            public GuidConverter(string format1, string format2) : base(format1, format2) { }
            #endregion

            #region Überschriebene Methoden
            public override void AppendSerialized(SqlDataReader rdr, int index, StringBuilder sb)
            {
                if (rdr.IsDBNull(index))
                    sb.Append("null");
                else
                    sb.Append(JsonConvert.SerializeObject(rdr.GetGuid(index)));
            }
            public override string GetString(object dBvalue)
            {
                if (dBvalue == DBNull.Value || dBvalue == null)
                    return string.Empty;

                try
                {
                    return ((Guid)dBvalue).ToString();
                }
                catch { return string.Empty; }
            }
            public override enResult GetValue(string entry, out object dbValue)
            {
                dbValue = DBNull.Value;

                if (string.IsNullOrWhiteSpace(entry))
                    return enResult.Empty;

                Guid result;

                if (!Guid.TryParse(entry, out result))
                    return enResult.Failed;

                dbValue = result;
                return enResult.Success;
            }
            public override ConverterBase GetClone(string format1, string format2)
            {
                return new Int32Converter(format1, format2);
            }
            #endregion
        }
        private sealed class UdtConverter : ConverterBase
        {
            #region Konstruktor
            public UdtConverter(string format1, string format2) : base(format1, format2) { }
            #endregion

            #region Überschriebene Methoden
            public override void AppendSerialized(SqlDataReader rdr, int index, StringBuilder sb)
            {
                if (rdr.IsDBNull(index))
                    sb.Append("null");
                else
                    sb.Append(JsonConvert.SerializeObject(rdr[index].ToString()));
            }
            public override string GetString(object dBvalue)
            {
                try
                {
                    if (dBvalue == null)
                        return string.Empty;

                    return dBvalue.ToString();
                }
                catch { return string.Empty; }
            }
            public override enResult GetValue(string entry, out object dbValue)
            {
                if (string.IsNullOrWhiteSpace(entry))
                {
                    dbValue = DBNull.Value;
                    return enResult.Empty;
                }

                dbValue = entry;
                return enResult.Success;
            }
            public override ConverterBase GetClone(string format1, string format2)
            {
                return new TextConverter(format1, format2);
            }
            #endregion
        }
        #endregion

        #region Private Klasse ComparerInfo
        private sealed class ComparerInfo
        {
            #region Öffentliche Felder
            public readonly bool Unary;
            public readonly bool ForStringsOnly;
            public readonly bool ForNull;
            public readonly Func<string, SqlParameter, string> GetSql;
            #endregion

            #region Konstruktor
            public ComparerInfo(bool unary, bool forStringsOnly, bool forNull,
                               Func<string, SqlParameter, string> getSql)
            {
                this.Unary = unary;
                this.ForStringsOnly = forStringsOnly;
                this.ForNull = forNull;
                this.GetSql = getSql;
            }
            #endregion
        }
        #endregion

        #region Private Klasse ColumnConfigInfo
        private sealed class ColumnConfigInfo
        {
            #region Öffentliche Felder
            //
            public readonly ConverterBase Converter;
            public readonly enKind Kind;
            public readonly SqlDbType DataType;
            public readonly enFieldSize FieldSize;
            public readonly enCompare Compare;
            public readonly bool Sortable;
            //
            public object DefaultValue;
            //
            #endregion

            #region Konstruktor
            public ColumnConfigInfo(ConverterBase converter, enKind kind, SqlDbType datatype,
                enFieldSize fieldSize, enCompare compare, bool sortable, object defaultValue)
            {
                this.Converter = converter;
                this.DefaultValue = defaultValue;
                this.Kind = kind;
                this.DataType = datatype;
                this.FieldSize = fieldSize;
                this.Compare = compare;
                this.Sortable = sortable;
            }
            #endregion
        }
        #endregion

        #region Private Klasse ColumnInfo
        private sealed class ColumnInfo
        {
            #region Öffentliche Felder
            //
            public readonly string DbColumn;
            public readonly string DbBaseSchema;
            public readonly string DbBaseTable;
            public readonly string DbBaseColumn;
            public readonly enKind Kind;
            public readonly SqlDbType DbType;
            public readonly enFieldSize FieldSize;
            public readonly int Size;
            public readonly int Precision;
            public readonly int Scale;
            public readonly bool ReadOnly;
            public readonly bool Nullable;
            public readonly bool Sortable;
            public readonly bool HasDbDefaultValue;
            public readonly object DefaultValue;
            public readonly enCompare Compare;
            public readonly ConverterBase Converter;
            //
            public bool HasUserValue = false;
            //
            #endregion

            #region Konstruktor
            public ColumnInfo(string dbColumn, string dbBaseSchema, string dbBaseTable,
                string dbBaseColumn, enKind kind, SqlDbType dbType, enFieldSize fieldSize,
                int size, int precision, int scale, bool readOnly, bool nullable,
                bool sortable, bool hasDbDefaultValue, object defaultValue, enCompare compare,
                ConverterBase converter)
            {
                this.DbColumn = dbColumn;
                this.DbBaseSchema = dbBaseSchema;
                this.DbBaseTable = dbBaseTable;
                this.DbBaseColumn = dbBaseColumn;
                this.Kind = kind;
                this.DbType = dbType;
                this.FieldSize = fieldSize;
                this.Size = size;
                this.Precision = precision;
                this.Scale = scale;
                this.ReadOnly = readOnly;
                this.Nullable = nullable;
                this.Sortable = sortable;
                this.HasDbDefaultValue = hasDbDefaultValue;
                this.DefaultValue = defaultValue;
                this.Compare = compare;
                this.Converter = converter;
            }
            #endregion

            #region Öffentliche Methoden
            public SqlParameter CreateParameter(string name)
            {
                SqlParameter parameter = new SqlParameter(name, DbType);

                if (FieldSize == enFieldSize.Size)
                    parameter.Size = Size;
                else if (FieldSize == enFieldSize.ScalePrecision)
                {
                    parameter.Precision = (byte)Precision;
                    parameter.Scale = (byte)Scale;
                }
                return parameter;
            }
            #endregion
        }
        #endregion

        #region Private Klasse TableInfo
        private sealed class TableInfo
        {
            #region Öffentliche Felder
            public readonly SqlCommand Command;
            public readonly string Sql;
            public readonly bool HasSelect;
            public readonly bool WithDistinct;
            public readonly List<string> ColumnList;
            public readonly Dictionary<string, ColumnInfo> DictColumns;
            public readonly Dictionary<string, InsertInfo> DictInsert;
            #endregion

            #region Konstruktor
            public TableInfo(SqlCommand command, string sql, bool hasSelect, bool withDistinct,
                List<string> columnList, Dictionary<string, ColumnInfo> dictColumns,
                Dictionary<string, InsertInfo> dictInsert)
            {
                this.Command = command;
                this.Sql = sql;
                this.HasSelect = hasSelect;
                this.WithDistinct = withDistinct;
                this.ColumnList = columnList;
                this.DictColumns = dictColumns;
                this.DictInsert = dictInsert;
            }
            #endregion
        }
        #endregion

        #region Private Klasse InsertInfo
        private sealed class InsertInfo
        {
            #region Öffentliche Felder
            public bool IsExpression;
            public object Value = null;
            public string Expression = null;
            #endregion

            #region Konstruktor
            public InsertInfo(bool isExpression, object value, string expression)
            {
                this.IsExpression = isExpression;
                this.Value = value;
                this.Expression = expression;
            }
            #endregion
        }
        #endregion

        #region Private Klasse ColumnFormatInfo
        private sealed class ColumnFormatInfo
        {
            #region Öffentliche Felder
            public bool ForBoolean;
            public string Format1;
            public string Format2;
            #endregion

            #region Konstruktor
            public ColumnFormatInfo(bool forBoolean, string format1, string format2)
            {
                this.ForBoolean = forBoolean;
                this.Format1 = format1;
                this.Format2 = format2;
            }
            #endregion
        }
        #endregion

        #region Private Klasse Token
        private sealed class Token
        {
            #region Öffentliche Felder
            public readonly char Char;
            public readonly int Position;
            #endregion

            #region Konstruktor
            public Token(char c, int position)
            {
                this.Char = c;
                this.Position = position;
            }
            #endregion
        }
        #endregion

        #region Private Klasse Scanner
        private sealed class Scanner
        {
            #region Private Felder
            private readonly TableInfo mTableInfo;
            private readonly char[] mChars;
            private int mCurPos;
            #endregion

            #region Öffentliche Eigenschaften
            public int Length
            {
                get { return mChars.Length; }
            }
            public int CurPos
            {
                get { return mCurPos; }
            }
            public char CurChar
            {
                get { return mChars[mCurPos]; }
            }
            #endregion

            #region Konstruktor
            public Scanner(TableInfo tableInfo, ref string s)
            {
                mTableInfo = tableInfo;

                mCurPos = 0;

                if (s == null)
                    mChars = new char[0];
                else
                    mChars = s.ToCharArray();
            }
            #endregion

            #region Öffentliche Methoden
            public char GetChar(int index)
            {
                return mChars[index];
            }
            public string GetString(int startIndex, int length)
            {
                return new string(mChars, startIndex, length);
            }
            public bool HasNextChar(int startIndex)
            {
                for (int i = startIndex; i < mChars.Length; i++)
                {
                    if (!char.IsWhiteSpace(mChars[i]))
                        return true;
                }
                return false;
            }
            public Token NextChar()
            {
                SkipWhiteSpaces();

                if (mCurPos >= mChars.Length)
                    return new Token('\0', mChars.Length);

                return new Token(mChars[mCurPos], mCurPos++);
            }
            public bool GetRightPar(ref Token token, ref string errMsg)
            {
                if (token.Char != ')')
                {
                    errMsg =
                        "Fehlende schliessende runde Klammer an Position " +
                        (token.Position + 1).ToString("0");

                    return false;
                }

                token = NextChar();
                return true;
            }
            public bool GetEqualsOperator(ref Token token, ref string errMsg)
            {
                if (token.Char != '=')
                {
                    errMsg =
                        "Fehlendes '=' an Position " +
                        (token.Position + 1).ToString("0");

                    return false;
                }

                token = NextChar();
                return true;
            }
            public bool GetSortOperator(ref Token token, out string op, ref string errMsg)
            {
                int pos = token.Position;

                if (pos >= mChars.Length)
                {
                    op = "ASC";
                    return true;
                }

                for (; pos < mChars.Length; pos++)
                {
                    if (!char.IsLetter(mChars[pos]))
                        break;
                }

                op = new string(mChars, token.Position, pos - token.Position);

                if (op.Length == 0)
                {
                    if (pos < mChars.Length && mChars[pos] != ',')
                    {
                        errMsg =
                            "Ungültiges Zeichen an Position " +
                            (token.Position + 1).ToString("0") + ": " +
                            mChars[token.Position].ToString() +
                            " [erwartet: asc|desc]";

                        return false;
                    }
                    else
                        op = "ASC";
                }

                if (string.Compare(op, "desc", true) == 0 || string.Compare(op, "asc", true) == 0)
                {
                    mCurPos = pos;
                    op = op.ToUpper();
                    token = NextChar();
                    return true;
                }

                errMsg =
                    "Ungültiger Sortieroperator an Position " +
                    (token.Position + 1).ToString("0") + ": " + op;

                return false;
            }
            public bool GetLogOperator(ref Token token, out string op, ref string errMsg)
            {
                int pos = token.Position;

                for (; pos < mChars.Length; pos++)
                {
                    if (!char.IsLetter(mChars[pos]))
                        break;
                }

                op = new string(mChars, token.Position, pos - token.Position);

                if (op.Length == 0)
                {
                    errMsg =
                        "Ungültiges Zeichen an Position " +
                        (token.Position + 1).ToString("0") + ": " +
                        mChars[token.Position].ToString() + " [erwartet: and|or]";

                    return false;
                }

                if (string.Compare(op, "and", true) == 0 || string.Compare(op, "or", true) == 0)
                {
                    mCurPos = pos;
                    op = op.ToUpper();
                    token = NextChar();
                    return true;
                }

                errMsg =
                    "Ungültiger logischer Operator an Position " +
                    (token.Position + 1).ToString("0") + ": " + op;

                return false;
            }
            public bool GetCompOperator(ref Token token, ColumnInfo columnInfo,
                out ComparerInfo compInfo, ref string errMsg)
            {
                compInfo = null;

                if (token.Position >= mChars.Length)
                {
                    errMsg =
                        "Fehlender Vergleichsoperator an Position " +
                        (mChars.Length + 1).ToString("0");

                    return false;
                }

                int pos = token.Position;

                // Vergleichsoperator muss mit '$' beginnen
                // und darf sonst nur Buchstaben enthalten
                if (mChars[pos] == '$')
                {
                    for (++pos; pos < mChars.Length; pos++)
                    {
                        if (!char.IsLetter(mChars[pos]))
                            break;
                    }
                }

                string op = new string(mChars, token.Position, pos - token.Position);

                if (op.Length == 0)
                {
                    errMsg =
                        "Ungültiges Zeichen an Position " +
                        (token.Position + 1).ToString("0") + ": " +
                        mChars[token.Position].ToString() +
                        " [erwartet: Vergleichsoperator]";

                    return false;
                }

                if (!mDictComparerInfo.TryGetValue(op, out compInfo))
                {
                    errMsg =
                        "Ungültiger Vergleichsoperator an Position " +
                        (token.Position + 1).ToString("0") + ": " + op;

                    return false;
                }

                // Binäre Vergleichsoperatoren dürfen nur von WhiteSpaces
                // oder von Hochkomma (limitierte Werte) begrenzt sein
                if (!compInfo.Unary && pos < mChars.Length)
                {
                    char c = mChars[pos];

                    if (!char.IsWhiteSpace(c) && c != '\'')
                    {
                        errMsg =
                            "Ungültiges Zeichen nach Vergleichsoperator an Position " +
                            (pos + 1).ToString("0") + ": " + c.ToString();

                        return false;
                    }
                }

                // Text/Image/Binary DB-Datentypen erlauben nur die
                // Operatoren $null oder $nnull (Vergleich mit DbNull)
                if (!compInfo.ForNull && columnInfo.Compare == enCompare.NullOnly)
                {
                    errMsg =
                        "Der Vergleichsoperator " + op.ToLower() + " an Position " +
                        (CurPos + 1).ToString("0") + " unterstützt den DB-Datentyp " +
                        columnInfo.DbType.ToString() + " nicht";

                    return false;
                }

                // Die Operatoren $starts, $has, $ends, $nstarts, $nhas und $nends
                // sind nur für Strings (aber nicht für Text DB-Datentypen) erlaubt
                if (compInfo.ForStringsOnly && columnInfo.Compare != enCompare.YesLike)
                {
                    errMsg =
                        "Der Vergleichsoperator " + op.ToLower() + " an Position " +
                        (CurPos + 1).ToString("0") + " unterstützt den DB-Datentyp " +
                        columnInfo.DbType.ToString() + " nicht";

                    return false;
                }

                mCurPos = pos;
                token = NextChar();
                return true;
            }
            public bool GetColumn(ref Token token, out ColumnInfo columnInfo,
                out string column, ref string errMsg)
            {
                columnInfo = null;

                if (token.Position >= mChars.Length)
                {
                    column = string.Empty;

                    errMsg =
                        "Fehlende Spalte an Position " +
                        (mChars.Length + 1).ToString("0");

                    return false;
                }

                bool masked = mChars[token.Position] == '[';

                if (!masked)
                {
                    // Nicht maskierte Spalten dürfen folgende Zeichen enthalten:
                    // Beginn: Letter, '_'
                    // Danach: Letter, Digit, '_', '@', '$', '#'

                    int pos = token.Position;

                    char c = mChars[pos];

                    if (char.IsLetter(c) || c == '_')
                    {
                        for (++pos; pos < mChars.Length; pos++)
                        {
                            c = mChars[pos];

                            if (char.IsLetterOrDigit(c) ||
                                c == '_' ||
                                c == '@' ||
                                c == '$' ||
                                c == '#')
                                continue;

                            break;
                        }
                    }

                    column = new string(mChars, token.Position, pos - token.Position);

                    if (column.Length == 0)
                    {
                        errMsg =
                            "Ungültiges Zeichen an Position " +
                            (token.Position + 1).ToString("0") + ": " +
                            mChars[token.Position].ToString() + " [erwartet: Spalte]";

                        return false;
                    }

                    if (!mTableInfo.DictColumns.TryGetValue(column, out columnInfo))
                    {
                        errMsg =
                            "Unbekannte Spalte an Position " +
                            (token.Position + 1).ToString("0") + ": " + column;

                        return false;
                    }

                    // Spalte wird immer maskiert
                    column = '[' + column + ']';

                    mCurPos = pos;
                    token = NextChar();
                    return true;
                }
                else
                {
                    int pos = token.Position + 1;
                    int bracks = 0;
                    column = null;

                    // In maskierten Spalten sind paarweise (maskierte) 
                    // schliessende eckige Klammern und beliebige andere 
                    // Zeichen (WhiteSpaces, Sonderzeichen, ...) erlaubt

                    for (; pos < mChars.Length; pos++)
                    {
                        char c = mChars[pos];

                        if (c == ']' && bracks++ % 2 == 0 &&
                            (pos + 1 >= mChars.Length || mChars[pos + 1] != ']'))
                        {
                            pos++;
                            column = new string(mChars, token.Position, pos - token.Position);
                            break;
                        }
                    }

                    if (column == null)
                    {
                        // Fehlende schliessende eckige Klammer

                        column = new string(mChars, token.Position, pos - token.Position);

                        errMsg =
                            "Fehlende schliessende eckige Klammer bei Spalte an Position " +
                            (token.Position + 1).ToString("0");

                        return false;
                    }

                    // Es wird die demaskierte Spalte gesucht 
                    string col = column.Substring(1, column.Length - 2).Replace("]]", "]");

                    if (!mTableInfo.DictColumns.TryGetValue(col, out columnInfo))
                    {
                        errMsg =
                            "Unbekannte Spalte an Position " +
                            (token.Position + 1).ToString("0") + ": " + column;

                        return false;
                    }
                    //

                    mCurPos = pos;
                    token = NextChar();
                    return true;
                }
            }
            public bool GetValue(ref Token token, ColumnInfo columnInfo, bool forWhere,
                out object value, ref string errMsg)
            {
                value = DBNull.Value;

                if (token.Position >= mChars.Length)
                {
                    errMsg =
                        "Fehlender Wert an Position " +
                        (mChars.Length).ToString("0");

                    return false;
                }

                int pos = token.Position + 1;
                bool delimited = mChars[token.Position] == '\'';

                if (!delimited)
                {
                    // Nicht limitierte Werte können rechts von
                    // einem WhiteSpace oder sep begrenzt sein

                    char sep = forWhere ? ')' : ',';

                    for (; pos < mChars.Length; pos++)
                    {
                        char c = mChars[pos];

                        if (char.IsWhiteSpace(c) || c == sep)
                            break;
                    }

                    string entry = new string(mChars, token.Position, pos - token.Position);

                    if (!forWhere && string.Compare(entry, "null", true) == 0)
                        value = DBNull.Value;
                    else
                    {
                        //// ReadOnly-Spalten werden bei Save nicht
                        //// geparst und auf Länge überprüft

                        //if (forWhere || !columnInfo.ReadOnly)
                        //{
                        enResult result = columnInfo.Converter.GetValue(entry, out value);

                        if (result == enResult.Failed)
                        {
                            errMsg =
                                "Der Wert '" + entry + "' an Position " +
                                (token.Position + 1).ToString("0") +
                                " kann nicht in den DB-Datentyp " +
                                columnInfo.DbType.ToString() + " konvertiert werden";

                            return false;
                        }

                        if (columnInfo.FieldSize == enFieldSize.Size &&
                            columnInfo.Kind == enKind.ShortString &&
                            entry.Length > columnInfo.Size)
                        {
                            errMsg =
                                "Der Wert '" + entry + "' an Position " +
                                (token.Position + 1).ToString("0") +
                                " überschreitet die maximale Länge von " +
                                columnInfo.Size.ToString() + " Zeichen";

                            return false;
                        }
                    }
                    //}

                    mCurPos = pos;
                    token = NextChar();
                    return true;
                }
                else
                {
                    int delimiters = 0;
                    string entry = null;

                    // In limitierten Werten sind paarweise (maskierte)
                    // Hochkommas und beliebige andere Zeichen
                    // (WhiteSpaces, Sonderzeichen, ...) erlaubt

                    for (; pos < mChars.Length; pos++)
                    {
                        char c = mChars[pos];

                        if (c == '\'' && delimiters++ % 2 == 0 &&
                            (pos + 1 >= mChars.Length || mChars[pos + 1] != '\''))
                        {
                            pos++;
                            entry = new string(mChars, token.Position, pos - token.Position);
                            break;
                        }
                    }

                    if (entry == null)
                    {
                        // Fehlendes schliessendes einfaches Hochkomma

                        entry = new string(mChars, token.Position, pos - token.Position);

                        errMsg =
                            "Fehlendes schliessendes Hochkomma bei Wert an Position " +
                            (token.Position + 1).ToString("0");

                        return false;
                    }

                    // ReadOnly-Spalten werden bei Save nicht
                    // geparst und auf Länge überprüft

                    //if (forWhere || !columnInfo.ReadOnly)
                    //{
                    entry = entry.Substring(1, entry.Length - 2).Replace("''", "'");

                    enResult result = columnInfo.Converter.GetValue(entry, out value);

                    if (result == enResult.Failed)
                    {
                        errMsg =
                            "Der Wert '" + entry + "' an Position " +
                            (token.Position + 1).ToString("0") +
                            " kann nicht in den DB-Datentyp " +
                            columnInfo.DbType.ToString() + " konvertiert werden";

                        return false;
                    }

                    if (columnInfo.FieldSize == enFieldSize.Size &&
                        columnInfo.Kind == enKind.ShortString &&
                        entry.Length > columnInfo.Size)
                    {
                        errMsg =
                            "Der Wert '" + entry + "' an Position " +
                            (token.Position + 1).ToString("0") +
                            " überschreitet die maximale Länge von " +
                            columnInfo.Size.ToString() + " Zeichen";

                        return false;
                    }
                    //}

                    mCurPos = pos;
                    token = NextChar();
                    return true;
                }
            }
            #endregion

            #region Private Methoden
            private void SkipWhiteSpaces()
            {
                for (; mCurPos < mChars.Length; mCurPos++)
                {
                    if (!char.IsWhiteSpace(mChars[mCurPos]))
                        break;
                }
            }
            #endregion
        }
        #endregion

        #region Private Klasse Parser
        private sealed class Parser
        {
            #region Grammatiken
            // ------------------------------------------------------
            // Grammatik für Where:
            //
            //      WhereClause
            //          WhereItem
            //          WhereClause + Log.Operator + WhereItem
            //      WhereItem
            //          '(' + WhereClause + ')'
            //          Spalte + Unärer Vergleichsoperator
            //          Spalte + Binärer Vergleichsoperator + Wert
            //
            // Log.Operator      : AND|OR
            // Vergleichsoperator: $e|$ne|$empty|$nempty ....
            // Spalte            : gültige Spalte
            // Wert              : gültiger Wert
            // ------------------------------------------------------

            // ------------------------------------------------------
            // Grammatik für Select:
            //
            //      SelectClause
            //          Spalte
            //          SelectClause + ',' + Spalte
            //
            // Spalte: gültige Spalte
            // ------------------------------------------------------

            // ------------------------------------------------------
            // Grammatik für Sort:
            //
            //      SortClause
            //          SortItem
            //          SortClause + ',' + SortItem
            //      SortItem
            //          Spalte
            //          Spalte + Sortieroperator
            //
            // Spalte         : gültige Spalte
            // Sortieroperator: ASC|DESC
            // ------------------------------------------------------

            // ------------------------------------------------------
            // Grammatik für Update:
            //
            //      UpdateClause
            //          UpdateItem
            //          UpdateClause + ',' + UpdateItem
            //      UpdateItem
            //          Spalte + '=' + Wert
            //
            // Spalte: gültige Spalte
            // Wert  : gültiger Wert
            // ------------------------------------------------------

            // ------------------------------------------------------
            // Grammatik für Insert:
            //
            //      InsertClause
            //          InsertItem
            //          InsertClause + ',' + InsertItem
            //      InsertItem
            //          Spalte + '=' + Wert
            //
            // Spalte: gültige Spalte
            // Wert  : gültiger Wert
            // ------------------------------------------------------
            #endregion

            #region Private Felder
            private Scanner mScanner;
            private Token mToken;
            #endregion

            #region Öffentliche Methoden
            public bool ParseWhere(TableInfo tableInfo, string s,
                StringBuilder sb, out bool empty, ref string errMsg)
            {
                mScanner = new Scanner(tableInfo, ref s);
                mToken = mScanner.NextChar();

                if (mToken.Position >= mScanner.Length)
                {
                    empty = true;
                    return true;
                }

                empty = false;
                sb.Append(" WHERE ");

                if (GetWhereClause(tableInfo, sb, ref errMsg))
                {
                    if (mToken.Position < mScanner.Length)
                    {
                        string end =
                            mScanner.GetString(mToken.Position,
                            mScanner.Length - mToken.Position);

                        errMsg =
                            "Ungültige Eingabe ab Position " +
                            (mToken.Position + 1).ToString("0") + ": " + end;

                        return false;
                    }

                    return true;
                }
                else
                    return false;
            }
            public bool ParseSelect(TableInfo tableInfo, bool distinct,
                int top, string s, StringBuilder sb, ref string errMsg)
            {
                mScanner = new Scanner(tableInfo, ref s);
                mToken = mScanner.NextChar();

                if (mToken.Position >= mScanner.Length)
                {
                    errMsg =
                        "Es wurde keine Spalte ausgewählt";

                    return false;
                }

                string strTop = top < 0 ? string.Empty : "TOP " + top.ToString("0") + ' ';
                string strDistinct = (distinct || tableInfo.WithDistinct) ? "DISTINCT " : string.Empty;
                string tbl = !tableInfo.HasSelect ? tableInfo.Sql : '(' + tableInfo.Sql + ")V";

                if (mToken.Char == '*' && !mScanner.HasNextChar(mScanner.CurPos))
                {
                    if (tableInfo.ColumnList.Count == 0)
                    {
                        errMsg =
                            "Es wurde keine Spalte ausgewählt";

                        return false;
                    }

                    sb.Append("SELECT " + strDistinct + strTop);

                    for (int i = 0; i < tableInfo.ColumnList.Count; i++)
                    {
                        if (i == 0)
                            sb.Append(tableInfo.ColumnList[i]);
                        else
                            sb.Append(',' + tableInfo.ColumnList[i]);
                    }

                    sb.Append(" FROM " + tbl);
                    return true;
                }

                sb.Append("SELECT " + strDistinct + strTop);

                if (GetSelectClause(sb, ref errMsg))
                {
                    if (mToken.Position < mScanner.Length)
                    {
                        string end =
                            mScanner.GetString(mToken.Position,
                            mScanner.Length - mToken.Position);

                        errMsg =
                            "Ungültige Eingabe ab Position " +
                            (mToken.Position + 1).ToString("0") + ": " + end;

                        return false;
                    }

                    sb.Append(" FROM " + tbl);
                    return true;
                }
                else
                    return false;
            }
            public bool ParseSort(TableInfo tableInfo, string s,
                StringBuilder sb, ref string errMsg)
            {
                mScanner = new Scanner(tableInfo, ref s);
                mToken = mScanner.NextChar();

                if (mToken.Position >= mScanner.Length)
                    return true;

                sb.Append(" ORDER BY ");

                if (GetSortClause(sb, ref errMsg))
                {
                    if (mToken.Position < mScanner.Length)
                    {
                        string end =
                            mScanner.GetString(mToken.Position,
                            mScanner.Length - mToken.Position);

                        errMsg =
                            "Ungültige Eingabe ab Position " +
                            (mToken.Position + 1).ToString("0") + ": " + end;

                        return false;
                    }

                    return true;
                }
                else
                    return false;
            }
            public bool ParseUpdate(bool setAsJson, TableInfo tableInfo,
                string set, StringBuilder sb, out int count, ref string errMsg)
            {
                count = 0;

                if (setAsJson)
                {
                    StringBuilder sb1 = new StringBuilder(200);

                    if (!ParseJsonSet(false, set, tableInfo, sb1, null,
                        ref count, ref errMsg))
                        return false;

                    if (count == 0)
                        return true;

                    sb.Append("UPDATE " + (!tableInfo.HasSelect ?
                        tableInfo.Sql : "V") + " SET " + sb1);

                    if (tableInfo.HasSelect)
                        sb.Append(" FROM (" + tableInfo.Sql + ")V");

                    return true;
                }

                mScanner = new Scanner(tableInfo, ref set);
                mToken = mScanner.NextChar();

                if (mToken.Position >= mScanner.Length)
                    return true;

                sb.Append("UPDATE " + (!tableInfo.HasSelect ?
                    tableInfo.Sql : "V") + " SET ");

                if (GetUpdateClause(tableInfo, sb, ref count, ref errMsg))
                {
                    if (mToken.Position < mScanner.Length)
                    {
                        string end =
                            mScanner.GetString(mToken.Position,
                            mScanner.Length - mToken.Position);

                        errMsg =
                            "Ungültige Eingabe ab Position " +
                            (mToken.Position + 1).ToString("0") + ": " + end;

                        return false;
                    }

                    if (tableInfo.HasSelect)
                        sb.Append(" FROM (" + tableInfo.Sql + ")V");

                    return true;
                }
                else
                    return false;
            }
            public bool ParseInsert(bool setAsJson, TableInfo tableInfo,
                string set, StringBuilder sb, out int count, ref string errMsg)
            {
                count = 0;

                StringBuilder sbColumns = new StringBuilder(200);
                StringBuilder sbValues = new StringBuilder(100);

                if (setAsJson)
                {
                    if (!ParseJsonSet(true, set, tableInfo, sbColumns, sbValues,
                        ref count, ref errMsg))
                        return false;

                    AddInsertArgs(tableInfo, sb, sbColumns, sbValues, ref count);
                    return true;
                }

                mScanner = new Scanner(tableInfo, ref set);
                mToken = mScanner.NextChar();

                if (mToken.Position >= mScanner.Length)
                {
                    AddInsertArgs(tableInfo, sb, sbColumns, sbValues, ref count);
                    return true;
                }

                if (GetInsertClause(tableInfo, sbColumns, sbValues, ref count, ref errMsg))
                {
                    if (mToken.Position < mScanner.Length)
                    {
                        string end =
                            mScanner.GetString(mToken.Position,
                            mScanner.Length - mToken.Position);

                        errMsg =
                            "Ungültige Eingabe ab Position " +
                            (mToken.Position + 1).ToString("0") + ": " + end;

                        return false;
                    }

                    AddInsertArgs(tableInfo, sb, sbColumns, sbValues, ref count);
                    return true;
                }
                else
                    return false;
            }
            #endregion

            #region Private Methoden zu Where
            private bool GetWhereClause(TableInfo tableInfo, StringBuilder sb, ref string errMsg)
            {
                const string chars = "aAoO";

                if (!GetWhereItem(tableInfo, sb, ref errMsg))
                    return false;

                while (chars.Contains(mToken.Char))
                {
                    string op;

                    if (!mScanner.GetLogOperator(ref mToken, out op, ref errMsg))
                        return false;

                    sb.Append(' ' + op + ' ');

                    if (!GetWhereItem(tableInfo, sb, ref errMsg))
                        return false;
                }

                return true;
            }
            private bool GetWhereItem(TableInfo tableInfo, StringBuilder sb, ref string errMsg)
            {
                if (mToken.Char == '(')
                {
                    sb.Append('(');

                    mToken = mScanner.NextChar();

                    if (!GetWhereClause(tableInfo, sb, ref errMsg))
                        return false;

                    if (!mScanner.GetRightPar(ref mToken, ref errMsg))
                        return false;

                    sb.Append(')');

                    return true;
                }

                return GetWherePrimary(tableInfo, sb, ref errMsg);
            }
            private bool GetWherePrimary(TableInfo tableInfo, StringBuilder sb, ref string errMsg)
            {
                string column;
                ColumnInfo columnInfo;
                ComparerInfo compInfo;

                if (!mScanner.GetColumn(ref mToken, out columnInfo, out column, ref errMsg))
                    return false;

                if (!mScanner.GetCompOperator(ref mToken, columnInfo, out compInfo, ref errMsg))
                    return false;

                if (compInfo.Unary)
                {
                    sb.Append(compInfo.GetSql(column, null));
                    return true;
                }

                object value;

                if (!mScanner.GetValue(ref mToken, columnInfo, true, out value, ref errMsg))
                    return false;

                string name = "@w" + tableInfo.Command.Parameters.Count.ToString("0");
                SqlParameter p = columnInfo.CreateParameter(name);
                tableInfo.Command.Parameters.Add(p).Value = value;
                sb.Append(compInfo.GetSql(column, p));

                return true;
            }
            #endregion

            #region Private Methoden zu Select
            private bool GetSelectClause(StringBuilder sb, ref string errMsg)
            {
                if (!GetSelectItem(sb, ref errMsg))
                    return false;

                while (mToken.Char == ',')
                {
                    sb.Append(',');

                    mToken = mScanner.NextChar();

                    if (!GetSelectItem(sb, ref errMsg))
                        return false;
                }

                return true;
            }
            private bool GetSelectItem(StringBuilder sb, ref string errMsg)
            {
                string column;
                ColumnInfo columnInfo;

                if (!mScanner.GetColumn(ref mToken, out columnInfo, out column, ref errMsg))
                    return false;

                sb.Append(column);

                return true;
            }
            #endregion

            #region Private Methoden zu Sort
            private bool GetSortClause(StringBuilder sb, ref string errMsg)
            {
                if (!GetSortItem(sb, ref errMsg))
                    return false;

                while (mToken.Char == ',')
                {
                    sb.Append(',');

                    mToken = mScanner.NextChar();

                    if (!GetSortItem(sb, ref errMsg))
                        return false;
                }

                return true;
            }
            private bool GetSortItem(StringBuilder sb, ref string errMsg)
            {
                string entry;
                ColumnInfo columnInfo;


                if (!mScanner.GetColumn(ref mToken, out columnInfo, out entry, ref errMsg))
                    return false;

                sb.Append(entry);

                if (!mScanner.GetSortOperator(ref mToken, out entry, ref errMsg))
                    return false;

                sb.Append(' ' + entry + ' ');

                return true;
            }
            #endregion

            #region Private Methoden zu Update
            private bool GetUpdateClause(TableInfo tableInfo, StringBuilder sb,
                ref int count, ref string errMsg)
            {
                if (!GetUpdateItem(tableInfo, sb, ref count, ref errMsg))
                    return false;

                while (mToken.Char == ',')
                {
                    mToken = mScanner.NextChar();

                    if (!GetUpdateItem(tableInfo, sb, ref count, ref errMsg))
                        return false;
                }

                return true;
            }
            private bool GetUpdateItem(TableInfo tableInfo, StringBuilder sb,
                ref int count, ref string errMsg)
            {
                string column;
                ColumnInfo columnInfo;

                if (!mScanner.GetColumn(ref mToken, out columnInfo, out column, ref errMsg))
                    return false;

                if (!mScanner.GetEqualsOperator(ref mToken, ref errMsg))
                    return false;

                object value;

                if (!mScanner.GetValue(ref mToken, columnInfo, false, out value, ref errMsg))
                    return false;

                // Readonly Spalten werden ignoriert
                if (columnInfo.ReadOnly)
                    return true;

                string name = "@p" + tableInfo.Command.Parameters.Count.ToString("0");
                SqlParameter p = columnInfo.CreateParameter(name);
                tableInfo.Command.Parameters.Add(p).Value = value;

                if (count++ == 0)
                    sb.Append(column + '=' + name);
                else
                    sb.Append(',' + column + '=' + name);

                return true;
            }
            #endregion

            #region Private Methoden zu Insert
            private bool GetInsertClause(TableInfo tableInfo, StringBuilder sbColumns,
                StringBuilder sbValues, ref int count, ref string errMsg)
            {
                if (!GetInsertItem(tableInfo, sbColumns, sbValues, ref count, ref errMsg))
                    return false;

                while (mToken.Char == ',')
                {
                    mToken = mScanner.NextChar();

                    if (!GetInsertItem(tableInfo, sbColumns, sbValues, ref count, ref errMsg))
                        return false;
                }

                return true;
            }
            private bool GetInsertItem(TableInfo tableInfo, StringBuilder sbColumns,
                StringBuilder sbValues, ref int count, ref string errMsg)
            {
                string column;
                ColumnInfo columnInfo;

                if (!mScanner.GetColumn(ref mToken, out columnInfo, out column, ref errMsg))
                    return false;

                if (!mScanner.GetEqualsOperator(ref mToken, ref errMsg))
                    return false;

                object value;

                if (!mScanner.GetValue(ref mToken, columnInfo, false, out value, ref errMsg))
                    return false;

                // Readonly Spalten werden ignoriert
                if (columnInfo.ReadOnly)
                    return true;

                columnInfo.HasUserValue = true;

                string name = "@p" + tableInfo.Command.Parameters.Count.ToString("0");
                SqlParameter p = columnInfo.CreateParameter(name);
                tableInfo.Command.Parameters.Add(p).Value = value;

                if (count++ == 0)
                {
                    sbColumns.Append(column);
                    sbValues.Append(name);
                }
                else
                {
                    sbColumns.Append(',' + column);
                    sbValues.Append(',' + name);
                }

                return true;
            }
            private static void AddInsertArgs(TableInfo tableInfo, StringBuilder sb,
                StringBuilder sbColumns, StringBuilder sbValues, ref int count)
            {
                foreach (ColumnInfo columnInfo in tableInfo.DictColumns.Values)
                {
                    if (columnInfo.ReadOnly || columnInfo.HasUserValue)
                        continue;

                    InsertInfo insertInfo = null;
                    string column;
                    SqlParameter p;

                    // Wenn der Wert einer aktualisierbaren Spalte nicht gesetzt 
                    // ist und eine InsertInfo zu Schema/Tabelle/Spalte oder zum
                    // Namen einer benutzerdefinierten Abfrage gesetzt ist, wird
                    // der Wert/Ausdruck der ermittelten InsertInfo verwendet

                    if (tableInfo.DictInsert != null)
                        tableInfo.DictInsert.TryGetValue(columnInfo.DbBaseColumn, out insertInfo);

                    if (insertInfo != null)
                    {
                        column = '[' + columnInfo.DbColumn.Replace("]", "]]") + ']';

                        if (insertInfo.Value != null)
                        {
                            p = columnInfo.CreateParameter
                                ("@p" + tableInfo.Command.Parameters.Count.ToString("0"));

                            tableInfo.Command.Parameters.Add(p).Value = insertInfo.Value;

                            if (count++ == 0)
                            {
                                sbColumns.Append(column);
                                sbValues.Append(p.ParameterName);
                            }
                            else
                            {
                                sbColumns.Append(',' + column);
                                sbValues.Append(',' + p.ParameterName);
                            }
                            continue;
                        }
                        else if (insertInfo.Expression != null)
                        {
                            if (count++ == 0)
                            {
                                sbColumns.Append(column);
                                sbValues.Append(insertInfo.Expression);
                            }
                            else
                            {
                                sbColumns.Append(',' + column);
                                sbValues.Append(',' + insertInfo.Expression);
                            }
                            continue;
                        }
                    }

                    if (columnInfo.Nullable || columnInfo.HasDbDefaultValue)
                        continue;

                    // Wenn der Wert einer aktualisierbaren Spalte nicht gesetzt 
                    // ist und keine InsertInfo zu Schema/Tabelle/Spalte gesetzt
                    // ist, wird für Not-Null Spalten ohne DB-Defaultwert der
                    // interne Defaultwert zum DB-Datentyp verwendet

                    column = '[' + columnInfo.DbColumn.Replace("]", "]]") + ']';

                    p = columnInfo.CreateParameter
                        ("@p" + tableInfo.Command.Parameters.Count.ToString("0"));

                    tableInfo.Command.Parameters.Add(p).Value = columnInfo.DefaultValue;

                    if (count++ == 0)
                    {
                        sbColumns.Append(column);
                        sbValues.Append(p.ParameterName);
                    }
                    else
                    {
                        sbColumns.Append(',' + column);
                        sbValues.Append(',' + p.ParameterName);
                    }
                }

                if (count < 1)
                    return;

                if (!tableInfo.HasSelect)
                {
                    sb.Append("INSERT INTO " + tableInfo.Sql + '(' +
                        sbColumns + ')' + " SELECT " + sbValues);
                }
                else
                {
                    sb.Append("WITH V AS(SELECT " + sbColumns +
                        " FROM (" + tableInfo.Sql + ")V)INSERT INTO V("
                         + sbColumns + ") SELECT " + sbValues);
                }
            }
            #endregion

            #region Private Methoden zu Json-Set
            private static bool ParseJsonSet(bool forInsert, string set,
                TableInfo tableInfo, StringBuilder sb1, StringBuilder sb2,
                ref int count, ref string errMsg)
            {
                try
                {
                    if (set == null)
                        return true;

                    Dictionary<string, string> dictJson =
                        JsonConvert.DeserializeObject<Dictionary<string, string>>(set);

                    // dict = null: Set =: null, "null", "", " ", "   ", ...
                    // dict = leer: Set =: {}, { }, {  }, ...



                    if (dictJson == null || dictJson.Count == 0)
                        return true;

                    foreach (KeyValuePair<string, string> kvp in dictJson)
                    {
                        if (string.IsNullOrWhiteSpace(kvp.Key))
                        {
                            errMsg =
                                "Fehlende Spalte in JSON String";

                            throw new Exception(errMsg);
                        }

                        string column = DeMaskJsonColumn(kvp.Key);
                        ColumnInfo info;

                        if (!tableInfo.DictColumns.TryGetValue(column, out info))
                        {
                            errMsg =
                                "Unbekannte Spalte in JSON String: " + kvp.Key.Trim();

                            throw new Exception(errMsg);
                        }

                        if (info.ReadOnly)
                            continue;

                        object value;
                        enResult res = info.Converter.GetValue(kvp.Value, out value);

                        if (res == enResult.Empty)
                            continue;

                        if (res == enResult.Failed)
                        {
                            errMsg =
                                "Der Wert '" + kvp.Value + "' in JSON String kann " +
                                "nicht in den DB-Datentyp " + info.DbType.ToString() +
                                " konvertiert werden";

                            throw new Exception(errMsg);
                        }

                        if (info.FieldSize == enFieldSize.Size &&
                            info.Kind == enKind.ShortString)
                        {
                            string entry = (string)value;

                            if (entry.Length > info.Size)
                            {
                                errMsg =
                                    "Der Wert '" + entry + "' in JSON String " +
                                    " überschreitet die maximale Länge von " +
                                    info.Size.ToString() + " Zeichen";

                                throw new Exception(errMsg);
                            }
                        }

                        column = "[" + column.Replace("]", "]]") + "]";

                        string name = "@p" + tableInfo.Command.Parameters.Count.ToString("0");
                        SqlParameter parameter = info.CreateParameter(name);
                        tableInfo.Command.Parameters.Add(parameter).Value = value;

                        if (forInsert)
                        {
                            info.HasUserValue = true;

                            if (count++ == 0)
                            {
                                sb1.Append(column);
                                sb2.Append(name);
                            }
                            else
                            {
                                sb1.Append(',' + column);
                                sb2.Append(',' + name);
                            }
                        }
                        else
                        {
                            if (count++ == 0)
                                sb1.Append(column + '=' + name);
                            else
                                sb1.Append(',' + column + '=' + name);
                        }
                    }

                    return true;
                }
                catch (Exception ex)
                {
                    errMsg = ex.Message;
                    return false;
                }
            }
            private static string DeMaskJsonColumn(string column)
            {
                if (string.IsNullOrWhiteSpace(column))
                    return string.Empty;

                column = column.Trim();

                if (column.StartsWith("[") && column.EndsWith("]"))
                    return column.Substring(1, column.Length - 2).Replace("]]", "]");

                return column;
            }
            #endregion
        }
        #endregion

        #region Private Klasse JsonConfiguration
        private class JsonConfiguration
        {
            #region Öffentliche Klasse JsonSettings
            public class JsonSettings
            {
                #region Öffentliche Klasse JsonDefaultColumnFormat
                public class JsonDefaultColumnFormat
                {
                    public string ForIntegers;
                    public string ForDecimals;
                    public string ForDateTime;
                    public string ForTime;
                    public string ForBooleanTrue;
                    public string ForBooleanFalse;
                }
                #endregion

                #region Öffentliche Klasse JsonDefaultInsertValues
                public class JsonDefaultInsertValues
                {
                    public byte ForByte;
                    public short ForInt16;
                    public int ForInt32;
                    public long ForInt64;
                    public decimal ForDecimal;
                    public double ForDouble;
                    public float ForSingle;
                    public DateTime ForDateTime;
                    public TimeSpan ForTime;
                    public bool ForBoolean;
                    public string ForString;
                }
                #endregion

                #region Öffentliche Felder
                public string ConnectionString;
                public string Token;
                public string Rights;
                public readonly JsonDefaultColumnFormat DefaultColumnFormat = new JsonDefaultColumnFormat();
                public readonly JsonDefaultInsertValues DefaultInsertValues = new JsonDefaultInsertValues();
                #endregion
            }
            #endregion

            #region Öffentliche Klasse JsonQueryItem
            public class JsonQueryItem
            {
                #region Öffentliche Klasse JsonInsertInfo
                public sealed class JsonInsertInfo
                {
                    #region Öffentliche Felder
                    public object Value;
                    public bool IsExpression;
                    #endregion

                    #region Konstruktor
                    public JsonInsertInfo(object value, bool isExpression)
                    {
                        this.Value = value;
                        this.IsExpression = isExpression;
                    }
                    #endregion
                }
                #endregion

                #region Öffentliche Klasse JsonFormatInfo
                public sealed class JsonFormatInfo
                {
                    #region Öffentliche Felder
                    public string Format1;
                    public string Format2;
                    #endregion

                    #region Konstruktor
                    public JsonFormatInfo(string format1, string format2)
                    {
                        this.Format1 = format1;
                        this.Format2 = format2;
                    }
                    #endregion
                }
                #endregion

                #region Öffentliche Felder
                public string AllowedMethods;
                public string AllowedTableType;
                public string ConnectionString;
                public int CommandTimeOut;
                public string Sql;
                public string CultureName;
                public string Token;
                public string Rights;
                public readonly Dictionary<string, JsonFormatInfo> ColumnFormat;
                public readonly Dictionary<string, JsonInsertInfo> DefaultInsertArgs;
                #endregion

                #region Konstruktor
                public JsonQueryItem()
                {
                    ColumnFormat = new Dictionary<string, JsonFormatInfo>
                        (StringComparer.OrdinalIgnoreCase);

                    DefaultInsertArgs = new Dictionary<string, JsonInsertInfo>
                        (StringComparer.OrdinalIgnoreCase);
                }
                #endregion

                #region Öffentliche Methoden
                public QueryItem CreateQueryItem(string name)
                {
                    QueryItem item = new QueryItem(name);

                    Dictionary<string, InsertInfo> dictInsert =
                        ((IDefaultInsertArgs)item.DefaultInsertArgs).Dictionary;

                    item.AllowedMethods = AllowedMethods;
                    item.AllowedTableType = AllowedTableType;
                    item.ConnectionString = ConnectionString;
                    item.CommandTimeOut = CommandTimeOut;
                    item.Sql = Sql;
                    item.CultureName = CultureName;
                    item.Token = Token;
                    item.Rights= Rights;

                    foreach (KeyValuePair<string, JsonFormatInfo> kvp in ColumnFormat)
                    {
                        string column = kvp.Key;

                        if (!DeMask(ref column))
                            continue;

                        JsonFormatInfo info = kvp.Value;

                        if (info == null)
                            continue;

                        if (info.Format2 == null)
                            item.ColumnFormat.Add(column, info.Format1);
                        else
                            item.ColumnFormat.Add(column, info.Format1, info.Format2);
                    }

                    foreach (KeyValuePair<string, JsonInsertInfo> kvp in DefaultInsertArgs)
                    {
                        string column = kvp.Key;

                        if (!DeMask(ref column))
                            continue;

                        JsonInsertInfo info = kvp.Value;

                        if (info == null)
                            continue;

                        if (info.IsExpression)
                        {
                            string expression;

                            try
                            {
                                expression =
                                    info.Value == null ?
                                    string.Empty :
                                    Convert.ToString(info.Value);
                            }
                            catch { expression = string.Empty; }

                            item.DefaultInsertArgs.AddExpression(column, expression);
                        }
                        else
                        {
                            object value = info.Value ?? DBNull.Value;
                            InsertInfo temp;

                            if (dictInsert.TryGetValue(column, out temp))
                            {
                                temp.IsExpression = false;
                                temp.Value = value;
                                temp.Expression = null;
                            }
                            else
                                dictInsert.Add(column, new InsertInfo(false, value, null));
                        }
                    }

                    return item;
                }
                public static JsonQueryItem Create(QueryItem item)
                {
                    JsonQueryItem jItem = new JsonQueryItem();

                    jItem.AllowedMethods = item.AllowedMethods;
                    jItem.AllowedTableType = item.AllowedTableType;
                    jItem.ConnectionString = item.ConnectionString;
                    jItem.CommandTimeOut = item.CommandTimeOut;
                    jItem.Sql = item.Sql;
                    jItem.CultureName = item.CultureName;
                    jItem.Token = item.Token;
                    jItem.Rights = item.Rights;

                    Dictionary<string, ColumnFormatInfo> dictFormat =
                        ((IColumnFormat)item.ColumnFormat).Dictionary;

                    Dictionary<string, InsertInfo> dictInsert =
                        ((IDefaultInsertArgs)item.DefaultInsertArgs).Dictionary;

                    foreach (KeyValuePair<string, ColumnFormatInfo> kvp in dictFormat)
                    {
                        string column = "[" + kvp.Key.Replace("]", "]]") + "]";
                        ColumnFormatInfo info = kvp.Value;

                        JsonFormatInfo temp;

                        if (jItem.ColumnFormat.TryGetValue(column, out temp))
                        {
                            temp.Format1 = info.Format1;
                            temp.Format2 = info.Format2;
                        }
                        else
                            jItem.ColumnFormat.Add(column, new JsonFormatInfo(info.Format1, info.Format2));
                    }

                    foreach (KeyValuePair<string, InsertInfo> kvp in dictInsert)
                    {
                        string column = "[" + kvp.Key.Replace("]", "]]") + "]";
                        InsertInfo info = kvp.Value;

                        JsonInsertInfo temp;

                        if (jItem.DefaultInsertArgs.TryGetValue(column, out temp))
                        {
                            if (info.IsExpression)
                                temp.IsExpression = true;

                            temp.Value = info.Value;
                        }
                        else
                        {
                            object value = info.IsExpression ? info.Expression : info.Value;
                            temp = new JsonInsertInfo(value, info.IsExpression);
                            jItem.DefaultInsertArgs.Add(column, temp);
                        }
                    }

                    return jItem;
                }
                #endregion
            }
            #endregion

            #region Öffentliche Felder
            public readonly JsonSettings Settings;
            public readonly Dictionary<string, JsonQueryItem> Items;
            #endregion

            #region Konstruktoren
            public JsonConfiguration()
            {
                // Default-Konstruktor für JSON Deserialisierung 

                Settings = new JsonSettings();

                Items = new Dictionary<string, JsonQueryItem>
                    (StringComparer.OrdinalIgnoreCase);
            }
            public JsonConfiguration(Dictionary<string, QueryItem> queryItems)
            {
                Settings = new JsonSettings();

                Items = new Dictionary<string, JsonQueryItem>
                    (StringComparer.OrdinalIgnoreCase);

                CopyQuerySettings();

                foreach (KeyValuePair<string, QueryItem> kvp in queryItems)
                {
                    if (!Items.ContainsKey(kvp.Key))
                        Items.Add(kvp.Key, JsonConfiguration.JsonQueryItem.Create(kvp.Value));
                }
            }
            public JsonConfiguration(Dictionary<string, JsonQueryItem> jsonItems)
            {
                Settings = new JsonSettings();
                CopyQuerySettings();
                this.Items = jsonItems;
            }
            #endregion

            #region Private Methoden
            private void CopyQuerySettings()
            {
                Settings.ConnectionString = DbQuery.Settings.ConnectionString;
                Settings.Token = DbQuery.Settings.Token;
                Settings.Rights = DbQuery.Settings.Rights;

                Settings.DefaultColumnFormat.ForIntegers = DbQuery.Settings.DefaultColumnFormat.ForIntegers;
                Settings.DefaultColumnFormat.ForDecimals = DbQuery.Settings.DefaultColumnFormat.ForDecimals;
                Settings.DefaultColumnFormat.ForDateTime = DbQuery.Settings.DefaultColumnFormat.ForDateTime;
                Settings.DefaultColumnFormat.ForTime = DbQuery.Settings.DefaultColumnFormat.ForTime;
                Settings.DefaultColumnFormat.ForBooleanTrue = DbQuery.Settings.DefaultColumnFormat.ForBooleanTrue;
                Settings.DefaultColumnFormat.ForBooleanFalse = DbQuery.Settings.DefaultColumnFormat.ForBooleanFalse;

                Settings.DefaultInsertValues.ForByte = DbQuery.Settings.DefaultInsertValues.ForByte;
                Settings.DefaultInsertValues.ForInt16 = DbQuery.Settings.DefaultInsertValues.ForInt16;
                Settings.DefaultInsertValues.ForInt32 = DbQuery.Settings.DefaultInsertValues.ForInt32;
                Settings.DefaultInsertValues.ForInt64 = DbQuery.Settings.DefaultInsertValues.ForInt64;
                Settings.DefaultInsertValues.ForDecimal = DbQuery.Settings.DefaultInsertValues.ForDecimal;
                Settings.DefaultInsertValues.ForDouble = DbQuery.Settings.DefaultInsertValues.ForDouble;
                Settings.DefaultInsertValues.ForSingle = DbQuery.Settings.DefaultInsertValues.ForSingle;
                Settings.DefaultInsertValues.ForDateTime = DbQuery.Settings.DefaultInsertValues.ForDateTime;
                Settings.DefaultInsertValues.ForTime = DbQuery.Settings.DefaultInsertValues.ForTime;
                Settings.DefaultInsertValues.ForBoolean = DbQuery.Settings.DefaultInsertValues.ForBoolean;
                Settings.DefaultInsertValues.ForString = DbQuery.Settings.DefaultInsertValues.ForString;
            }
            private void CopyJsonSettings()
            {
                DbQuery.Settings.ConnectionString = Settings.ConnectionString;
                DbQuery.Settings.Token = Settings.Token;
                DbQuery.Settings.Rights = Settings.Rights;

                DbQuery.Settings.DefaultColumnFormat.ForIntegers = Settings.DefaultColumnFormat.ForIntegers;
                DbQuery.Settings.DefaultColumnFormat.ForDecimals = Settings.DefaultColumnFormat.ForDecimals;
                DbQuery.Settings.DefaultColumnFormat.ForDateTime = Settings.DefaultColumnFormat.ForDateTime;
                DbQuery.Settings.DefaultColumnFormat.ForTime = Settings.DefaultColumnFormat.ForTime;
                DbQuery.Settings.DefaultColumnFormat.ForBooleanTrue = Settings.DefaultColumnFormat.ForBooleanTrue;
                DbQuery.Settings.DefaultColumnFormat.ForBooleanFalse = Settings.DefaultColumnFormat.ForBooleanFalse;

                DbQuery.Settings.DefaultInsertValues.ForByte = Settings.DefaultInsertValues.ForByte;
                DbQuery.Settings.DefaultInsertValues.ForInt16 = Settings.DefaultInsertValues.ForInt16;
                DbQuery.Settings.DefaultInsertValues.ForInt32 = Settings.DefaultInsertValues.ForInt32;
                DbQuery.Settings.DefaultInsertValues.ForInt64 = Settings.DefaultInsertValues.ForInt64;
                DbQuery.Settings.DefaultInsertValues.ForDecimal = Settings.DefaultInsertValues.ForDecimal;
                DbQuery.Settings.DefaultInsertValues.ForDouble = Settings.DefaultInsertValues.ForDouble;
                DbQuery.Settings.DefaultInsertValues.ForSingle = Settings.DefaultInsertValues.ForSingle;
                DbQuery.Settings.DefaultInsertValues.ForDateTime = Settings.DefaultInsertValues.ForDateTime;
                DbQuery.Settings.DefaultInsertValues.ForTime = Settings.DefaultInsertValues.ForTime;
                DbQuery.Settings.DefaultInsertValues.ForBoolean = Settings.DefaultInsertValues.ForBoolean;
                DbQuery.Settings.DefaultInsertValues.ForString = Settings.DefaultInsertValues.ForString;
            }
            #endregion

            #region Öffentliche Methoden
            public void Configure(Dictionary<string, QueryItem> queryItems)
            {
                
                queryItems.Clear();

                foreach (KeyValuePair<string, JsonQueryItem> kvp in Items) queryItems.Add(kvp.Key, kvp.Value.CreateQueryItem(kvp.Key));

                CopyJsonSettings();

            }
            #endregion
        }
        #endregion

        #region Öffentliche Klasse Arguments
        /// <summary>
        /// Enthält Informationen für das Generieren 
        /// und Ausführen von SQL Anweisungen.
        /// </summary>
        public class Arguments
        {
            /// <summary>
            /// Boolean: bei True/1 werden Metadaten 
            /// zu den abgefragten Spalten angezeigt.
            /// </summary>
            public dynamic GetModelInfo { get; set; }
            /// <summary>
            /// C,R,U,D.
            /// </summary>
            public dynamic Method { get; set; }
            /// <summary>
            /// Wenn der Wert von Token in Settings oder im Abfrageobjekt 
            /// (QueryItem) in der Abfrageliste (QueryList) gesetzt ist, 
            /// muss der Wert (Wert von QueryItem überschreibt Wert 
            /// von Abfrageliste) mit dem Wert von Arguments übereinstimmen.
            /// </summary>
            public dynamic Token { get; set; }
            /// <summary>
            /// Name des gewünschten QueryItem Objektes.
            /// </summary>
            public dynamic Name { get; set; }
            /// <summary>
            /// Boolean: bei True/1 werden bei Read nur 
            /// eindeutige Datensätze zurückgegeben.
            /// </summary>
            public dynamic Distinct { get; set; }
            /// <summary>
            /// Get-Anweisung bei Read
            /// </summary>
            public dynamic Get { get; set; }
            /// <summary>
            /// Filter-Anweisung bei Read, Update oder Delete.
            /// </summary>
            public dynamic Filter { get; set; }
            /// <summary>
            /// Maximale Anzahl der abgefragten Datensätze bei Read 
            /// (keine Einschränkung, wenn kleiner 0 oder leer).
            /// </summary>
            public dynamic Limit { get; set; }
            /// <summary>
            /// Sort-Anweisung bei Read.
            /// </summary>
            public dynamic Sort { get; set; }
            /// <summary>
            /// Set-Anweisung bei Create oder Update.
            /// </summary>
            public dynamic Set { get; set; }
            /// <summary>
            /// Wird durchgereicht.
            /// </summary>
            public dynamic Message { get; set; }
            /// <summary>
            /// Index des ersten zurückgegebenen Datensatzes bei Read 
            /// (0, wenn kleiner 0 oder leer).
            /// </summary>
            public dynamic PageIndex { get; set; }
            /// <summary>
            /// Maximale Anzahl der zurückgegebenen Datensätze bei Read 
            /// (keine Einschränkung, wenn kleiner 1 oder leer).
            /// </summary>
            public dynamic PageSize { get; set; }
            // added by woko 2/2016
            /// <summary>
            /// Wenn Wert von Rights in Settings oder im Abfrageobjekt 
            /// (QueryItem) in der Abfrageliste (QueryList) gesetzt ist, 
            /// muss der Wert (Wert von QueryItem überschreibt Wert 
            /// von Abfrageliste) den Wert von Arguments beinhalten.
            /// </summary>
            public dynamic Rights { get; set; }


        }
        #endregion

        #region Öffentliche Klasse Result
        /// <summary>
        /// Enthält die Ergebnisdaten von CRUD Methoden. 
        /// Der Rückgabewert der Methoden ist ein in einen 
        /// JSON String serialisiertes Objekt vom Typ Result. 
        /// </summary>
        public class Result
        {
            /// <summary>
            /// C,R,U,D.
            /// </summary>
            public string Method { get; set; }
            /// <summary>
            /// Name des ermittelten QueryItem Objektes.
            /// </summary>
            public string Name { get; set; }
            /// <summary>
            /// Boolean: Wenn der Wert auf True oder 1 gesetzt ist, 
            /// werden bei Read nur eindeutige Datensätze zurückgegeben.
            /// </summary>
            public string Distinct { get; set; }
            /// <summary>
            /// Get-Anweisung bei Read
            /// </summary>
            public string Get { get; set; }
            /// <summary>
            /// Filter-Anweisung bei Read, Update oder Delete.
            /// </summary>
            public string Filter { get; set; }
            /// <summary>
            /// Maximale Anzahl der abgefragten Datensätze bei Read 
            /// (keine Einschränkung, wenn kleiner 0 oder leer).
            /// </summary>
            public string Limit { get; set; }
            /// <summary>
            /// Sort-Anweisung bei Read.
            /// </summary>
            public string Sort { get; set; }
            /// <summary>
            /// Set-Anweisung bei Create oder Update.
            /// </summary>
            public string Set { get; set; }
            /// <summary>
            /// Wird durchgereicht.
            /// </summary>
            public string Message { get; set; }
            /// <summary>
            /// Index des ersten zurückgegebenen Datensatzes bei Read 
            /// (0, wenn kleiner 0 oder leer).
            /// </summary>
            public object PageIndex { get; set; }
            /// <summary>
            /// Maximale Anzahl der zurückgegebenen Datensätze bei Read 
            /// (keine Einschränkung, wenn kleiner 1 oder leer).
            /// </summary>
            public object PageSize { get; set; }
            /// <summary>
            /// Die abgefragten Spalten ohne Metadaten.
            /// </summary>
            public List<string> Model { get; set; }
            /// <summary>
            /// Die abgefragten Spalten mit Metadaten.
            /// </summary>
            public Dictionary<string, Dictionary<string, string>> ModelInfo { get; set; }
            /// <summary>
            /// Die zurückgegebenen Datensätze bei Read.
            /// </summary>
            public List<Dictionary<string, string>> Data { get; set; }
            /// <summary>
            /// Anzahl der zurückgegebenen (Read) oder
            /// betroffenen (Create, Update, Delete) Datensätze.
            /// </summary>
            public object Affected { get; set; }
            /// <summary>
            /// Gesamtanzahl der durchlaufenen Datensätze bei Read.
            /// </summary>
            public object TotalReads { get; set; }
            /// <summary>
            /// Fehlermeldung bei Ausnahme.
            /// </summary>
            public string Error { get; set; }
        }
        #endregion

        #region Öffentliche Klasse Settings
        /// <summary>
        /// Standard Einstellungen für alle CRUD Methoden.
        /// </summary>
        public static class Settings
        {
            #region Öffentliche Eigenschaften: ConnectionString, Token, Rights
            /// <summary>
            /// Verbindungszeichenfolge zur SQL Server Datenbank. 
            /// Der Wert wird vom gesetzten Wert des aufgerufenen
            /// Abfrageobjektes (QueryItem) überschrieben. 
            /// Standardwert: NULL
            /// </summary>
            ///             
            public static string ConnectionString{ get; set;} 
             

            /// <summary>
            /// Wenn der Wert von Token in Settings oder im Abfrageobjekt 
            /// (QueryItem) in der Abfrageliste (QueryList) gesetzt ist, 
            /// muss der Wert (Wert von QueryItem überschreibt Wert 
            /// von Abfrageliste) mit dem Wert von Arguments übereinstimmen.
            /// Standardwert: NULL
            /// </summary>
            public static string Token { get; set; }

            /// <summary>
            /// Wenn der Wert von Rights in Settings oder im Abfrageobjekt 
            /// (QueryItem) in der Abfrageliste (QueryList) gesetzt ist, 
            /// muss der Wert (Wert von QueryItem überschreibt Wert 
            /// von Abfrageliste) den Wert von Arguments beinhalten.
            /// Arguments 'C' and Settings.Rights='ABCDE' fits, because Settings.Rights contains 'C' 
            /// Standardwert: NULL
            /// </summary>
            public static string Rights { get; set; }


            #endregion

            #region Öffentliche Klasse DefaultColumnFormat
            /// <summary>
            /// Standard-Spaltenformatierung. 
            /// Der Wert wird vom gesetzten Wert des aufgerufenen 
            /// Abfrageobjektes (QueryItem) überschrieben.
            /// </summary>
            public static class DefaultColumnFormat
            {
                #region Öffentliche Eigenschaften: Defaultformate zu Datentypen
                /// <summary>
                /// Standardformat für alle Ganzzahlen: 'F0'
                /// </summary>
                public static string ForIntegers
                {
                    get { return mInt32Converter.Format1; }
                    set
                    {
                        mByteConverter.Format1 = value;
                        mInt16Converter.Format1 = value;
                        mInt32Converter.Format1 = value;
                        mInt64Converter.Format1 = value;
                    }
                }
                /// <summary>
                /// Standardformat für alle Dezimal/Fließkommazahlen: 'F2'
                /// </summary>
                public static string ForDecimals
                {
                    get { return mDecimalConverter.Format1; }
                    set
                    {
                        mSingleConverter.Format1 = value;
                        mDoubleConverter.Format1 = value;
                        mDecimalConverter.Format1 = value;
                    }
                }
                /// <summary>
                /// Standardformat für Datum: 'd'
                /// </summary>
                public static string ForDateTime
                {
                    get { return mDateTimeConverter.Format1; }
                    set { mDateTimeConverter.Format1 = value; }
                }
                /// <summary>
                /// Standardformat für Datum: 't'
                /// </summary>
                public static string ForTime
                {
                    get { return mTimeConverter.Format1; }
                    set { mTimeConverter.Format1 = value; }
                }
                /// <summary>
                /// Standardformat für Boolean-True: 'true'
                /// </summary>
                public static string ForBooleanTrue
                {
                    get { return mBoolConverter.Format1; }
                    set { mBoolConverter.Format1 = value; }
                }
                /// <summary>
                /// Standardformat für Boolean-False: 'false'
                /// </summary>
                public static string ForBooleanFalse
                {
                    get { return mBoolConverter.Format2; }
                    set { mBoolConverter.Format2 = value; }
                }
                #endregion
            }
            #endregion

            #region Öffentliche Klasse DefaultInsertValues
            /// <summary>
            /// Standardwerte für nicht gesetzte Spaltenwerte 
            /// von aktualisierbaren Not-Null Spalten bei Create. 
            /// Der Wert wird vom gesetzten Wert/Ausdruck des 
            /// aufgerufenen Abfrageobjektes (QueryItem) überschrieben.
            /// </summary>
            public static class DefaultInsertValues
            {
                #region Öffentliche Eigenschaften: Defaultwerte zu Datentypen für Insert
                /// <summary>
                /// Standardwert: 0
                /// </summary>
                public static byte ForByte
                {
                    get { return (byte)mColConfigInfoTinyInt.DefaultValue; }
                    set { mColConfigInfoTinyInt.DefaultValue = value; }
                }
                /// <summary>
                /// Standardwert: 0
                /// </summary>
                public static short ForInt16
                {
                    get { return (short)mColConfigInfoSmallInt.DefaultValue; }
                    set { mColConfigInfoSmallInt.DefaultValue = value; }
                }
                /// <summary>
                /// Standardwert: 0
                /// </summary>
                public static int ForInt32
                {
                    get { return (int)mColConfigInfoInt.DefaultValue; }
                    set { mColConfigInfoInt.DefaultValue = value; }
                }
                /// <summary>
                /// Standardwert: 0
                /// </summary>
                public static long ForInt64
                {
                    get { return (long)mColConfigInfoBigInt.DefaultValue; }
                    set { mColConfigInfoBigInt.DefaultValue = value; }
                }
                /// <summary>
                /// Standardwert: 0
                /// </summary>
                public static decimal ForDecimal
                {
                    get { return (decimal)mColConfigInfoDecimal.DefaultValue; }
                    set
                    {
                        mColConfigInfoDecimal.DefaultValue = value;
                        mColConfigInfoMoney.DefaultValue = value;
                        mColConfigInfoSmallMoney.DefaultValue = value;
                    }
                }
                /// <summary>
                /// Standardwert: 0
                /// </summary>
                public static double ForDouble
                {
                    get { return (double)mColConfigInfoFloat.DefaultValue; }
                    set { mColConfigInfoFloat.DefaultValue = value; }
                }
                /// <summary>
                /// Standardwert: 0
                /// </summary>
                public static float ForSingle
                {
                    get { return (float)mColConfigInfoReal.DefaultValue; }
                    set { mColConfigInfoReal.DefaultValue = value; }
                }
                /// <summary>
                /// Standardwert: '01.01.1800'
                /// </summary>
                public static DateTime ForDateTime
                {
                    get { return (DateTime)mColConfigInfoDateTime.DefaultValue; }
                    set
                    {
                        mColConfigInfoDateTime.DefaultValue = value;
                        mColConfigInfoDateTime2.DefaultValue = value;
                        mColConfigInfoSmallDateTime.DefaultValue = value;
                        mColConfigInfoDate.DefaultValue = value;
                    }
                }
                /// <summary>
                /// Standardwert: '00:00'
                /// </summary>
                public static TimeSpan ForTime
                {
                    get { return (TimeSpan)mColConfigInfoTime.DefaultValue; }
                    set { mColConfigInfoTime.DefaultValue = value; }
                }
                /// <summary>
                /// Standardwert: False
                /// </summary>
                public static bool ForBoolean
                {
                    get { return (bool)mColConfigInfoBit.DefaultValue; }
                    set { mColConfigInfoBit.DefaultValue = value; }
                }
                /// <summary>
                /// Standardwert: ''
                /// </summary>
                public static string ForString
                {
                    get { return mColConfigInfoVarChar.DefaultValue.ToString(); }
                    set
                    {
                        object temp;

                        if (value == null)
                            temp = DBNull.Value;
                        else
                            temp = value;

                        mColConfigInfoVarChar.DefaultValue = temp;
                        mColConfigInfoChar.DefaultValue = temp;
                        mColConfigInfoNVarChar.DefaultValue = temp;
                        mColConfigInfoNChar.DefaultValue = temp;
                    }
                }
                #endregion
            }
            #endregion

            #region Statischer Konstruktor
            static Settings()
            {
                ConnectionString = null;
                Token = null;
                Rights = null;
            }
            #endregion
        }
        #endregion

        #region Öffentliche Klasse InformationSchemaTable
        /// <summary>
        /// InformationSchemaTable holds the Columns from SQL information_schema.TABLES.
        /// (wk)
        /// </summary>
        /// 
        public class InformationSchemaTable
        {
            public string Catalog { get; set; }
            public string Schema { get; set; }
            public string Name { get; set; }
            public string Type { get; set; }
        }
        #endregion

        #region Öffentliche Klasse QueryItem
        /// <summary>
        /// Enthält abfragespezifische Konfigurations-, SQL- und 
        /// Formatinformationen für CRUD Methoden. 
        /// Über das an die Methoden übergebene Arguments Objekt wird 
        /// in der Abfrageliste (QueryList) ein QueryItem Objekt mit
        /// gleichem Namen ermittelt. Wenn kein Objekt gefunden wird, 
        /// können die CRUD Methoden nicht erfolgreich ausgeführt werden.
        /// </summary>
        public sealed class QueryItem
        {
            #region Öffentliche Klasse ItemColumnFormat
            public sealed class ItemColumnFormat : IColumnFormat
            {
                #region Private Felder
                private readonly Dictionary<string, ColumnFormatInfo> mDictionary;
                #endregion

                #region Explizite IColumnFormat Eigenschaften
                Dictionary<string, ColumnFormatInfo> IColumnFormat.Dictionary
                {
                    get { return mDictionary; }
                }
                #endregion

                #region Konstruktor
                public ItemColumnFormat()
                {
                    mDictionary = new Dictionary<string, ColumnFormatInfo>
                        (StringComparer.OrdinalIgnoreCase);
                }
                #endregion

                #region Öffentliche Methoden
                public void Add(string column, string format)
                {
                    if (!DeMask(ref column))
                        return;

                    ColumnFormatInfo info;

                    if (mDictionary.TryGetValue(column, out info))
                    {
                        info.ForBoolean = false;
                        info.Format1 = format;
                        info.Format2 = null;
                    }
                    else
                        mDictionary.Add(column, new ColumnFormatInfo(false, format, null));
                }
                public void Add(string column, string trueFormat, string falseFormat)
                {
                    if (!DeMask(ref column))
                        return;

                    ColumnFormatInfo info;

                    if (mDictionary.TryGetValue(column, out info))
                    {
                        info.ForBoolean = true;
                        info.Format1 = trueFormat;
                        info.Format2 = falseFormat;
                    }
                    else
                        mDictionary.Add(column, new ColumnFormatInfo(true, trueFormat, falseFormat));
                }
                public bool Get(string column, out string format)
                {
                    format = null;

                    if (!DeMask(ref column))
                        return false;

                    ColumnFormatInfo info;

                    if (mDictionary.TryGetValue(column, out info) && !info.ForBoolean)
                    {
                        format = info.Format1;
                        return true;
                    }

                    return false;
                }
                public bool Get(string column, out string trueFormat, out string falseFormat)
                {
                    trueFormat = null;
                    falseFormat = null;

                    if (!DeMask(ref column))
                        return false;

                    ColumnFormatInfo info;

                    if (mDictionary.TryGetValue(column, out info) && info.ForBoolean)
                    {
                        trueFormat = info.Format1;
                        falseFormat = info.Format2;
                        return true;
                    }

                    return false;
                }
                public bool Contains(string column)
                {
                    if (!DeMask(ref column))
                        return false;

                    return mDictionary.ContainsKey(column);
                }
                public bool Remove(string column)
                {
                    if (!DeMask(ref column))
                        return false;

                    return mDictionary.Remove(column);
                }
                public void Clear()
                {
                    mDictionary.Clear();
                }
                #endregion
            }
            #endregion

            #region Öffentliche Klasse ItemDefaultInsertArgs
            public sealed class ItemDefaultInsertArgs : IDefaultInsertArgs
            {
                #region Private Felder
                private readonly Dictionary<string, InsertInfo> mDictionary;
                #endregion

                #region Explizite IDefaultInsertArgs Eigenschaften
                Dictionary<string, InsertInfo> IDefaultInsertArgs.Dictionary
                {
                    get { return mDictionary; }
                }
                #endregion

                #region Konstruktor
                public ItemDefaultInsertArgs()
                {
                    mDictionary = new Dictionary<string, InsertInfo>
                        (StringComparer.OrdinalIgnoreCase);
                }
                #endregion

                #region Private Methoden
                private void AddValue<T>(string column, T value)
                {
                    if (!DeMask(ref column))
                        return;

                    InsertInfo info;

                    if (mDictionary.TryGetValue(column, out info))
                    {
                        info.IsExpression = false;
                        info.Value = value;
                        info.Expression = null;
                    }
                    else
                        mDictionary.Add(column, new InsertInfo(false, value, null));
                }
                #endregion

                #region Öffentliche Methoden
                public void AddValue(string column, byte value)
                {
                    AddValue<byte>(column, value);
                }
                public void AddValue(string column, short value)
                {
                    AddValue<short>(column, value);
                }
                public void AddValue(string column, int value)
                {
                    AddValue<int>(column, value);
                }
                public void AddValue(string column, long value)
                {
                    AddValue<long>(column, value);
                }
                public void AddValue(string column, float value)
                {
                    AddValue<float>(column, value);
                }
                public void AddValue(string column, double value)
                {
                    AddValue<double>(column, value);
                }
                public void AddValue(string column, decimal value)
                {
                    AddValue<decimal>(column, value);
                }
                public void AddValue(string column, DateTime value)
                {
                    AddValue<DateTime>(column, value);
                }
                public void AddValue(string column, TimeSpan value)
                {
                    AddValue<TimeSpan>(column, value);
                }
                public void AddValue(string column, bool value)
                {
                    AddValue<bool>(column, value);
                }
                public void AddValue(string column, string value)
                {
                    AddValue<string>(column, value);
                }
                public void AddExpression(string column, string expression)
                {
                    if (!DeMask(ref column))
                        return;

                    InsertInfo info;

                    if (mDictionary.TryGetValue(column, out info))
                    {
                        info.IsExpression = true;
                        info.Value = null;
                        info.Expression = expression;
                    }
                    else
                        mDictionary.Add(column, new InsertInfo(true, null, expression));
                }
                public bool GetValue(string column, out object value)
                {
                    value = null;

                    if (!DeMask(ref column))
                        return false;

                    InsertInfo info;

                    if (mDictionary.TryGetValue(column, out info) && !info.IsExpression)
                    {
                        value = info.Value;
                        return true;
                    }

                    return false;
                }
                public bool GetExpression(string column, out string expression)
                {
                    expression = null;

                    if (!DeMask(ref column))
                        return false;

                    InsertInfo info;

                    if (mDictionary.TryGetValue(column, out info) && info.IsExpression)
                    {
                        expression = info.Expression;
                        return true;
                    }

                    return false;
                }
                public bool Contains(string column)
                {
                    if (!DeMask(ref column))
                        return false;

                    return mDictionary.ContainsKey(column);
                }
                public bool Remove(string column)
                {
                    if (!DeMask(ref column))
                        return false;

                    return mDictionary.Remove(column);
                }
                public void Clear()
                {
                    mDictionary.Clear();
                }
                #endregion
            }
            #endregion

            #region Private Felder
            private readonly string mName;
            private readonly ItemColumnFormat mColumnFormat = new ItemColumnFormat();
            private readonly ItemDefaultInsertArgs mDefaultInsertArgs = new ItemDefaultInsertArgs();
            #endregion

            #region Öffentliche Eigenschaften
            /// <summary>
            /// Eindeutiger Bezeichner including Wildcards ie. 'dbo.*' or '(dbo|person).*' or 'addre*'.
            /// </summary>
            public string Name
            {
                get { return mName; }
            }

            /// <summary>
            /// REGEX Expression calculated from Name including Wildcards ie. 'dbo.*' or '(dbo|person).*' or 'addre*'.
            /// wk
            /// </summary>
            public Regex NameRegex { get; set; }

            /// <summary>
            /// Holds the Default Schema of your Session.
            /// wk
            /// </summary>
            public string DefaultSchema { get; set; }

            /// <summary>
            /// InformationSchemaTableDictionary holds all available Tables in a Dictionary. An incoming DBQueryItem Name must be checked if the Name is a valid TableName, do prevent SQL Injection.
            /// used by method isNameValidTable
            /// (wk)
            /// </summary>
            public Dictionary<string, InformationSchemaTable> InformationSchemaTableDictionary = new Dictionary<string, InformationSchemaTable>();

            /// <summary>
            /// Zeichenfolge aus folgenden Zeichen: C, R, U, D.
            /// </summary>
            public string AllowedMethods { get; set; }
            /// <summary>
            /// information_schema.tables: T table, V view, [all] 
            /// wk
            /// </summary>
            public string AllowedTableType { get; set; }
            /// <summary>
            /// Verbindungszeichenfolge zur SQL Server Datenbank. 
            /// Wenn der Wert nicht gesetzt ist, wird der Wert aus 
            /// Settings verwendet.
            /// </summary>
            public string ConnectionString { get; set; }
            /// <summary>
            /// Erlaubte Ausführungszeit der SQL Abfrage in Sekunden.
            /// </summary>
            public int CommandTimeOut { get; set; }
            /// <summary>
            /// [Tabelle|Sicht] oder [Schema].[Tabelle|Sicht] oder 
            /// SQL Select-Abfrage.
            /// </summary>
            public string Sql { get; set; }
            /// <summary>
            /// Name der Kultureinstellung für die Spaltenformatierung von
            /// Zahlen, Datum und Uhrzeit. Wenn der Wert nicht gesetzt ist, 
            /// wird der Wert des aktuellen Threads oder der zuletzt gesetzte 
            /// gültige Wert verwendet.
            /// </summary>
            public string CultureName { get; set; }
            /// <summary>
            /// Wenn der Wert von Token in Settings gesetzt ist, 
            /// müssen beide Werte übereinstimmen.
            /// </summary>
            public string Token { get; set; }
            /// <summary>
            /// Spaltenformatierung. 
            /// Überschreibt die Standard-Spaltenformatierung in Settings.
            /// </summary>
            public ItemColumnFormat ColumnFormat
            {
                get { return mColumnFormat; }
            }
            /// <summary>
            /// Standardwerte und SQL-Ausdrücke (Funktionen und Select Anweisungen) 
            /// für nicht gesetzte Spaltenwerte von aktualisierbaren Spalten bei Create. 
            /// SQL-Select Anweisungen müssen in runde Klammern gesetzt werden. 
            /// Überschreibt den Standardwert in Settings.
            /// </summary>
            public ItemDefaultInsertArgs DefaultInsertArgs
            {
                get { return mDefaultInsertArgs; }
            }
            
            // added WOKO 2/2016
            /// <summary>
            /// Wenn der Wert von RIGHTS 'ABCDE' in Settings gesetzt ist, 
            /// und der gesendete Wert z.b 'C' in Arguments in Rights 'ABCED' vorhanden ist, dann ist access erlaubt.
            /// </summary>
            public string Rights { get; set; }

            #endregion

            #region Konstruktoren
            public QueryItem(string name)
            {
                mName = name;
            }
            public QueryItem(string name, QueryItem item)
            {
                mName = name;
                this.NameRegex = item.NameRegex;
                this.DefaultSchema = item.DefaultSchema;

                this.AllowedMethods = item.AllowedMethods;
                this.AllowedTableType = item.AllowedTableType;
                this.ConnectionString = item.ConnectionString;
                this.CommandTimeOut = item.CommandTimeOut;
                this.Sql = item.Sql;
                this.CultureName = item.CultureName;
                this.Token = item.Token;
                // added woko 2/16
                this.Rights = item.Rights;

                this.InformationSchemaTableDictionary = item.InformationSchemaTableDictionary;

                mColumnFormat = item.ColumnFormat;
                mDefaultInsertArgs = item.DefaultInsertArgs;
            }
            #endregion
        }
        #endregion

        #region Öffentliche Klasse QueryList
        /// <summary>
        /// Enthält die Abfrageobjekte (QueryItem) für CRUD Methoden. 
        /// Das gesuchte Objekt wird über den Namen des an die 
        /// Methoden übergebenen Arguments Objektes ermittelt.
        /// </summary>
        public static class QueryList
        {
            #region Private Felder
            private static Dictionary<string, QueryItem> mDictionary;
            #endregion

            #region Öffentliche Eigenschaften
            public static int Count
            {
                get { return mDictionary.Count; }
            }
            #endregion

            #region Statischer Konstruktor
            static QueryList()
            {
                mDictionary =
                    new Dictionary<string, QueryItem>(StringComparer.OrdinalIgnoreCase);
            }
            #endregion

            #region Private Methoden

            static async Task loadDictionaryWithValidTable(QueryItem item)
            {
                // loads InformationSchemaTableDictionary with all Tablenames corresponding to DBQueryItem 

                // prüft ob 'name' eine gültige Tabelle/View der DB ist
                // somit wird verhindert, dass eine SQL-Injection in die DB über einen 
                // name="DELETE FROM Table" verhindert wird

                using (SqlConnection con = new SqlConnection(GetConnectionString(item)))
                {
                    con.Open();
                    SqlCommand com = con.CreateCommand();
                    com.CommandText = "SELECT * FROM information_schema.tables";

                    using (SqlDataReader rdr = await com.ExecuteReaderAsync())
                    {
                        while (await rdr.ReadAsync())
                        {

                            InformationSchemaTable isTbl = new InformationSchemaTable();
                            // isTbl.Catalog = rdr["TABLE_CATALOG"].ToString();
                            isTbl.Schema = rdr["TABLE_SCHEMA"].ToString();
                            isTbl.Name = rdr["TABLE_NAME"].ToString();
                            isTbl.Type = rdr["TABLE_TYPE"].ToString();

                            item.InformationSchemaTableDictionary.Add(isTbl.Schema + "." + isTbl.Name, isTbl);
                        }
                    }

                }

            }

            static string getDefaultSchema(QueryItem item)
            {
                // return the getDefaultSchema form Session defined by DBQuery Item

                using (SqlConnection con = new SqlConnection(GetConnectionString(item)))
                {
                    con.Open();
                    SqlCommand com = con.CreateCommand();
                    com.CommandText = "SELECT SCHEMA_NAME()";
                    return Convert.ToString(com.ExecuteScalar());

                }

            }


            static async Task<bool> isNameValidTable(string schemaTableName, QueryItem item)
            {

                // prüft ob 'name' eine gültige Tabelle/View der DB ist
                // somit wird verhindert, dass eine SQL-Injection in die DB über einen 
                // name="DELETE FROM Table" verhindert wird
                // DICTIONARY VERSION  
                // + Very fast !!!  Iteration 10.000  ~ 2msec  ->  2700x faster
                // - TableInformation is not actual, the Server loads the Information once when starting!!!)

                string[] nameArr = schemaTableName.Split('.');
                string tablename = nameArr[nameArr.Length - 1];
                string schemaname = (nameArr.Length == 2) ? nameArr[0] : item.DefaultSchema;
                schemaTableName = schemaname + "." + tablename;

                if (item.InformationSchemaTableDictionary.ContainsKey(schemaTableName) == false) throw new Exception("'" + schemaTableName + "' is no valid 'Schema.Table' or 'Schema.View' in SQL-DB"); //return false;

                InformationSchemaTable isTblOut = new InformationSchemaTable();
                item.InformationSchemaTableDictionary.TryGetValue(schemaTableName, out isTblOut);


                // AllowedTableType = [V|T|null|string.empty] 
                if (!string.IsNullOrWhiteSpace(item.AllowedTableType))
                {   // AllowedTableType = TABLE  only allow tables           
                    if (item.AllowedTableType.IndexOf("T", StringComparison.OrdinalIgnoreCase) > -1)
                    {
                        if (item.AllowedTableType.IndexOf("V", StringComparison.OrdinalIgnoreCase) == -1)
                            if (isTblOut.Type != "BASE TABLE") throw new Exception("Matching DBQuery Item '" + item.Name + "' allows only Tabletype 'BASE TABLE'");//return false;
                    }
                    else
                    {   // AllowedTableType = VIEW only allow views                                                               
                        if (item.AllowedTableType.IndexOf("V", StringComparison.OrdinalIgnoreCase) > -1)
                            if (isTblOut.Type != "VIEW") throw new Exception("Matching DBQuery Item '" + item.Name + "' allows only Tabletype 'VIEW'"); // return false;
                    }
                }

                return true;

            }
          
            #endregion

            #region Öffentliche Methoden

            private const string FILENAME_DBQUERY_CONFIG_JSON = "DBQuery.config.json";
            public static string FullPathConfigJSON
            {
                get
                {
                    return Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), FILENAME_DBQUERY_CONFIG_JSON);
                }
            }

            public static Dictionary<string, QueryItem> List
            {
                get
                {
                    return mDictionary;
                }
            }

            public static void Add(QueryItem item)
            {
                if (item == null)
                    return;

                string name = item.Name;

                if (string.IsNullOrWhiteSpace(name))
                    return;

                if (mDictionary.ContainsKey(name))
                    mDictionary[name] = item;
                else
                    mDictionary.Add(name, item);
            }
            public async static Task<QueryItem> Get(string name)
            {
                QueryItem item = null;
                QueryItem itemOut = null;

                // if DBQueryItem empty -> exit 
                if (string.IsNullOrWhiteSpace(name))
                    return null;

                //  EQUALS - if DBQueryItem equals with requesting NAME -> EQUAL MATCH 
                if (mDictionary.TryGetValue(name, out itemOut))
                {
                    item = new QueryItem(name, itemOut);   // create new Object

                    // Console.WriteLine("NAME EQUALS - " + name + "- sql -" + item.Sql);
                    // if sql undefined try to take name as table/view  - is secure to SQL-Injection because 'name' is an item defined in dbQueryConfig on serverside
                    if (string.IsNullOrWhiteSpace(item.Sql)) item.Sql = name;

                    //System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
                    //sw.Start();
                    //int length = 10000;
                    //for (int i = 0; i < length; i++)
                    //{
                    //    await isNameValidTable(name, item);
                    //}
                    //Console.WriteLine("############### STOPWATCH isNameValidTable: " + sw.ElapsedMilliseconds);

                    return item;

                }


                // WILDCARD MATCH - Try to Match requesting NAME with DBQueryItem using WILDCARDS  "dbo.*":{..xxxxxxxxx..} 
                foreach (KeyValuePair<string, QueryItem> kvp in mDictionary)
                {
                    // Console.WriteLine("NAME TRY MATCH - name - kvp.Key -value - item: " + name + " - " + kvp.Key + " - " + kvp.Value.Sql );

                    //  MATCH - Test REGEX-MATCH  NAME vs QUERY-ITEM "per*":{ ... }                  

                    //  Console.WriteLine(kvp.Value.NameRegex.IsMatch(name) + "DBQ-REGEX:" + kvp.Value.NameRegex.ToString() + "  -  MATCH STRING:" + name);           
                    if (kvp.Value.NameRegex.IsMatch(name))          // NEW
                    {
                        item = new QueryItem(kvp.Key, kvp.Value);
                        // Console.WriteLine("NAME MATCHS - " + name + " - " + kvp.Key + " - TYPE " + item.AllowedTableType + " SQL:" + item.Sql);

                        // Test if SLQ = Null and NAME is valid Table/View  ->  SQL becomes the Table/View
                        if (string.IsNullOrWhiteSpace(item.Sql) && await isNameValidTable(name, item))
                        {
                            // Prevents SQL-Injection because the incoming 'name' will be checked in FN isTable(name, item) 

                            // Console.WriteLine(name + "  IS valid TABLE/View  - tableType" + item.AllowedTableType);
                            // if sql undefined try to set SQL with name as table/view  - is secure to SQL-Injection because the 'name' will be checked in FN isTable(name, item)                          
                            item.Sql = name;
                        }
                        // else use defined Item.SQL Sequence 
                        return item;
                    }
                }

                // Console.WriteLine("NAME NO EQUALS or MATCH in dbQueryConfig Items found for:" + name);
                return null;

            }
            public static bool Contains(string name)
            {
                if (string.IsNullOrWhiteSpace(name))
                    return false;

                return mDictionary.ContainsKey(name);
            }
            public static bool Remove(string name)
            {
                if (string.IsNullOrWhiteSpace(name))
                    return false;

                return mDictionary.Remove(name);
            }
            public static void Clear()
            {
                mDictionary.Clear();
            }

            public static bool LoadConfig(string fileName, out Exception ex)
            {
                ex = null;

                try
                {
                    JsonSerializer serializer = CreateJsonSerializer();
                    JsonConfiguration jsonConfig = null;

                    JsonSerializerSettings s = new JsonSerializerSettings();
                    s.MissingMemberHandling = MissingMemberHandling.Ignore;
                    s.StringEscapeHandling = StringEscapeHandling.Default;

                    string json = File.ReadAllText(fileName, Encoding.UTF8);
                    json = json.Replace(@"\", @"\\");

                
                    jsonConfig = JsonConvert.DeserializeObject<JsonConfiguration>(json);

                    // must be handled before    'jsonConfig.Configure(mDictionary);'             
                    // wenn in DbQueryConfig.json -> Settings -> "ConnectionString": "ODBC=Schillermed" generiere Connection aus ODBC Schillermed 
                    if (!string.IsNullOrWhiteSpace(jsonConfig.Settings.ConnectionString)) 
                        jsonConfig.Settings.ConnectionString = Tools.Ganymed.Connection.tryConnectionFromODBC(jsonConfig.Settings.ConnectionString);


                    if (jsonConfig != null) jsonConfig.Configure(mDictionary);
                    
                    if (mDictionary.Count == 0) throw new Exception("No DBQuery-Item in '" + FILENAME_DBQUERY_CONFIG_JSON + "' declared.");

                    
                    foreach (var dic in mDictionary)
                    {
                        // test if there is a global ConnectionString in setting or else in each DBQuery Item
                        if (string.IsNullOrWhiteSpace(jsonConfig.Settings.ConnectionString) && string.IsNullOrWhiteSpace(dic.Value.ConnectionString))
                        {
                            throw new Exception("No Connectionstring in '" + FILENAME_DBQUERY_CONFIG_JSON + "' found. Please declare a global ConnectionString in 'Settings' or add a ConnectionString to DBQuery-Item '" + dic.Key + "'");
                        }

                        // Get the Default Schema from DBQuery Item Session
                        dic.Value.DefaultSchema = getDefaultSchema(dic.Value);

                        // Loads Dictionary InformationSchemaTable with all available Table for each DBQuery-Item
                        Task t = loadDictionaryWithValidTable(dic.Value);

                        // convert each Name of DBQuery Item to a REGEXPRESSION 
                        string regexPattern = "^" + dic.Value.Name.Replace(".", "\\.").Replace("*", ".*").Replace("+", ".*").Replace("?", "[\\w]") + "$";
                        dic.Value.NameRegex = new Regex(regexPattern, RegexOptions.IgnoreCase);
                        // Console.WriteLine("LOAD DBQ-REGEXPRESSION: " + dic.Value.NameRegex);
                    }


                    return true;
                }
                catch (Exception exc)
                {
                    mDictionary.Clear();
                    ex = exc;
                    return false;
                }
            }

            public static bool LoadConfig(out Exception ex)
            {
                return LoadConfig(FullPathConfigJSON, out ex);
            }

            public static bool LoadConfig()
            {
                Exception ex;
                return LoadConfig(out ex);
            }

            public static bool SaveConfig(string fileName, out Exception ex)
            {
                ex = null;

                try
                {
                    JsonSerializer serializer = CreateJsonSerializer();
                    JsonConfiguration jsonConfig = new JsonConfiguration(mDictionary);

                    using (StreamWriter writer = new StreamWriter(fileName, false, Encoding.UTF8))
                    {
                        serializer.Serialize(writer, jsonConfig);
                    }

                    return true;
                }
                catch (Exception exc)
                {
                    ex = exc;
                    return false;
                }
            }
            public static bool SaveConfig(out Exception ex)
            {
                return SaveConfig(FullPathConfigJSON, out ex);
            }
            public static bool SaveConfig()
            {
                Exception ex;
                return SaveConfig(out ex);
            }
            #endregion
        }
        #endregion

        #region Private Konstanten
        private const string FILENAME_JSON_DEFAULT = "DBQuery.config.json";
        #endregion

        #region Private Felder
        //
        private static readonly BooleanConverter mBoolConverter = new BooleanConverter("true", "false");
        private static readonly Int64Converter mInt64Converter = new Int64Converter("F0", null);
        private static readonly Int32Converter mInt32Converter = new Int32Converter("F0", null);
        private static readonly Int16Converter mInt16Converter = new Int16Converter("F0", null);
        private static readonly ByteConverter mByteConverter = new ByteConverter("F0", null);
        private static readonly DoubleConverter mDoubleConverter = new DoubleConverter("N2", null);
        private static readonly SingleConverter mSingleConverter = new SingleConverter("N2", null);
        private static readonly DecimalConverter mDecimalConverter = new DecimalConverter("N2", null);
        private static readonly DateTimeConverter mDateTimeConverter = new DateTimeConverter("d", null);
        private static readonly TimeConverter mTimeConverter = new TimeConverter("t", null);
        private static readonly StringConverter mStringConverter = new StringConverter(null, null);
        private static readonly TextConverter mTextConverter = new TextConverter(null, null);
        private static readonly Base64StringConverter mBase64Converter = new Base64StringConverter(null, null);
        private static readonly Base64StringConverter mTimeStampConverter = new Base64StringConverter(8, null, null);
        private static readonly GuidConverter mGuidConverter = new GuidConverter(null, null);
        private static readonly UdtConverter mUdtConverter = new UdtConverter(null, null);
        //        
        private static readonly ColumnConfigInfo mColConfigInfoTinyInt = new ColumnConfigInfo(mByteConverter, enKind.Number, SqlDbType.TinyInt, enFieldSize.Precision, enCompare.YesNoLike, true, (byte)0);
        private static readonly ColumnConfigInfo mColConfigInfoSmallInt = new ColumnConfigInfo(mInt16Converter, enKind.Number, SqlDbType.SmallInt, enFieldSize.Precision, enCompare.YesNoLike, true, (short)0);
        private static readonly ColumnConfigInfo mColConfigInfoInt = new ColumnConfigInfo(mInt32Converter, enKind.Number, SqlDbType.Int, enFieldSize.Precision, enCompare.YesNoLike, true, (int)0);
        private static readonly ColumnConfigInfo mColConfigInfoBigInt = new ColumnConfigInfo(mInt64Converter, enKind.Number, SqlDbType.BigInt, enFieldSize.Precision, enCompare.YesNoLike, true, (long)0);
        private static readonly ColumnConfigInfo mColConfigInfoDecimal = new ColumnConfigInfo(mDecimalConverter, enKind.Number, SqlDbType.Decimal, enFieldSize.ScalePrecision, enCompare.YesNoLike, true, (decimal)0);
        private static readonly ColumnConfigInfo mColConfigInfoMoney = new ColumnConfigInfo(mDecimalConverter, enKind.Number, SqlDbType.Money, enFieldSize.None, enCompare.YesNoLike, true, (decimal)0);
        private static readonly ColumnConfigInfo mColConfigInfoSmallMoney = new ColumnConfigInfo(mDecimalConverter, enKind.Number, SqlDbType.SmallMoney, enFieldSize.None, enCompare.YesNoLike, true, (decimal)0);
        private static readonly ColumnConfigInfo mColConfigInfoFloat = new ColumnConfigInfo(mDoubleConverter, enKind.Number, SqlDbType.Float, enFieldSize.None, enCompare.YesNoLike, true, (double)0);
        private static readonly ColumnConfigInfo mColConfigInfoReal = new ColumnConfigInfo(mSingleConverter, enKind.Number, SqlDbType.Real, enFieldSize.None, enCompare.YesNoLike, true, (float)0);
        private static readonly ColumnConfigInfo mColConfigInfoDateTime = new ColumnConfigInfo(mDateTimeConverter, enKind.DateTime, SqlDbType.DateTime, enFieldSize.None, enCompare.YesNoLike, true, new DateTime(1800, 1, 1));
        private static readonly ColumnConfigInfo mColConfigInfoDateTime2 = new ColumnConfigInfo(mDateTimeConverter, enKind.DateTime, SqlDbType.DateTime2, enFieldSize.None, enCompare.YesNoLike, true, new DateTime(1800, 1, 1));
        private static readonly ColumnConfigInfo mColConfigInfoSmallDateTime = new ColumnConfigInfo(mDateTimeConverter, enKind.DateTime, SqlDbType.SmallDateTime, enFieldSize.None, enCompare.YesNoLike, true, new DateTime(1800, 1, 1));
        private static readonly ColumnConfigInfo mColConfigInfoDate = new ColumnConfigInfo(mDateTimeConverter, enKind.Date, SqlDbType.Date, enFieldSize.None, enCompare.YesNoLike, true, new DateTime(1800, 1, 1));
        private static readonly ColumnConfigInfo mColConfigInfoTime = new ColumnConfigInfo(mTimeConverter, enKind.Time, SqlDbType.Time, enFieldSize.None, enCompare.YesNoLike, true, new TimeSpan());
        private static readonly ColumnConfigInfo mColConfigInfoBit = new ColumnConfigInfo(mBoolConverter, enKind.Boolean, SqlDbType.Bit, enFieldSize.None, enCompare.YesNoLike, true, false);
        private static readonly ColumnConfigInfo mColConfigInfoVarChar = new ColumnConfigInfo(mStringConverter, enKind.ShortString, SqlDbType.VarChar, enFieldSize.Size, enCompare.YesLike, true, string.Empty);
        private static readonly ColumnConfigInfo mColConfigInfoNVarChar = new ColumnConfigInfo(mStringConverter, enKind.ShortString, SqlDbType.NVarChar, enFieldSize.Size, enCompare.YesLike, true, string.Empty);
        private static readonly ColumnConfigInfo mColConfigInfoChar = new ColumnConfigInfo(mStringConverter, enKind.ShortString, SqlDbType.VarChar, enFieldSize.Size, enCompare.YesLike, true, string.Empty);
        private static readonly ColumnConfigInfo mColConfigInfoNChar = new ColumnConfigInfo(mStringConverter, enKind.ShortString, SqlDbType.NVarChar, enFieldSize.Size, enCompare.YesLike, true, string.Empty);
        private static readonly ColumnConfigInfo mColConfigInfoText = new ColumnConfigInfo(mTextConverter, enKind.LongString, SqlDbType.Text, enFieldSize.Size, enCompare.NullOnly, false, string.Empty);
        private static readonly ColumnConfigInfo mColConfigInfoNText = new ColumnConfigInfo(mTextConverter, enKind.LongString, SqlDbType.NText, enFieldSize.Size, enCompare.NullOnly, false, string.Empty);
        private static readonly ColumnConfigInfo mColConfigInfoImage = new ColumnConfigInfo(mBase64Converter, enKind.Binary, SqlDbType.Image, enFieldSize.Size, enCompare.NullOnly, false, DBNull.Value);
        private static readonly ColumnConfigInfo mColConfigInfoBinary = new ColumnConfigInfo(mBase64Converter, enKind.Binary, SqlDbType.Binary, enFieldSize.Size, enCompare.NullOnly, false, DBNull.Value);
        private static readonly ColumnConfigInfo mColConfigInfoVarBinary = new ColumnConfigInfo(mBase64Converter, enKind.Binary, SqlDbType.VarBinary, enFieldSize.Size, enCompare.NullOnly, false, DBNull.Value);
        private static readonly ColumnConfigInfo mColConfigInfoTimeStamp = new ColumnConfigInfo(mTimeStampConverter, enKind.Binary, SqlDbType.Timestamp, enFieldSize.Size, enCompare.YesNoLike, true, DBNull.Value);
        private static readonly ColumnConfigInfo mColConfigInfoUniqueIdentifier = new ColumnConfigInfo(mGuidConverter, enKind.Guid, SqlDbType.UniqueIdentifier, enFieldSize.Size, enCompare.YesNoLike, true, DBNull.Value);
        // Exoten
        private static readonly ColumnConfigInfo mColConfigInfoXml = new ColumnConfigInfo(mTextConverter, enKind.Xml, SqlDbType.Xml, enFieldSize.None, enCompare.NullOnly, false, string.Empty);
        private static readonly ColumnConfigInfo mColConfigInfoUdt = new ColumnConfigInfo(mUdtConverter, enKind.UdtString, SqlDbType.Udt, enFieldSize.None, enCompare.YesNoLike, true, DBNull.Value);
        private static readonly ColumnConfigInfo mColConfigInfoVariant = new ColumnConfigInfo(mStringConverter, enKind.Variant, SqlDbType.Variant, enFieldSize.None, enCompare.YesLike, true, DBNull.Value);

        //
        private static readonly Dictionary<string, ComparerInfo> mDictComparerInfo;
        private static readonly Dictionary<SqlDbType, ColumnConfigInfo> mDictColumnConfigInfo;
        //
        #endregion

        #region Statischer Konstruktor
        static DbQuery()
        {
            Func<string, SqlParameter, string> getSql;
            ComparerInfo compInfo;

            mDictComparerInfo =
                new Dictionary<string, ComparerInfo>(StringComparer.OrdinalIgnoreCase);
            //
            getSql = (field, parameter) => { return field + " = " + parameter.ParameterName; };
            compInfo = new ComparerInfo(false, false, false, getSql);
            mDictComparerInfo.Add("$e", compInfo);
            //
            getSql = (field, parameter) => { return field + " <> " + parameter.ParameterName; };
            compInfo = new ComparerInfo(false, false, false, getSql);
            mDictComparerInfo.Add("$ne", compInfo);
            //
            getSql = (field, parameter) => { return field + " < " + parameter.ParameterName; };
            compInfo = new ComparerInfo(false, false, false, getSql);
            mDictComparerInfo.Add("$lt", compInfo);
            //
            getSql = (field, parameter) => { return field + " >= " + parameter.ParameterName; };
            compInfo = new ComparerInfo(false, false, false, getSql);
            mDictComparerInfo.Add("$nlt", compInfo);
            //
            getSql = (field, parameter) => { return field + " <= " + parameter.ParameterName; };
            compInfo = new ComparerInfo(false, false, false, getSql);
            mDictComparerInfo.Add("$lte", compInfo);
            //
            getSql = (field, parameter) => { return field + " > " + parameter.ParameterName; };
            compInfo = new ComparerInfo(false, false, false, getSql);
            mDictComparerInfo.Add("$nlte", compInfo);
            //
            getSql = (field, parameter) => { return field + " > " + parameter.ParameterName; };
            compInfo = new ComparerInfo(false, false, false, getSql);
            mDictComparerInfo.Add("$gt", compInfo);
            //
            getSql = (field, parameter) => { return field + " <= " + parameter.ParameterName; };
            compInfo = new ComparerInfo(false, false, false, getSql);
            mDictComparerInfo.Add("$ngt", compInfo);
            //
            getSql = (field, parameter) => { return field + " >= " + parameter.ParameterName; };
            compInfo = new ComparerInfo(false, false, false, getSql);
            mDictComparerInfo.Add("$gte", compInfo);
            //
            getSql = (field, parameter) => { return field + " < " + parameter.ParameterName; };
            compInfo = new ComparerInfo(false, false, false, getSql);
            mDictComparerInfo.Add("$ngte", compInfo);
            //
            getSql = (field, parameter) => { return "CHARINDEX(" + parameter.ParameterName + "," + field + ") = 1"; };
            compInfo = new ComparerInfo(false, true, false, getSql);
            mDictComparerInfo.Add("$starts", compInfo);
            //
            getSql = (field, parameter) => { return "CHARINDEX(" + parameter.ParameterName + "," + field + ") <> 1"; };
            compInfo = new ComparerInfo(false, true, false, getSql);
            mDictComparerInfo.Add("$nstarts", compInfo);
            //
            getSql = (field, parameter) => { return "CHARINDEX(" + parameter.ParameterName + "," + field + ") > 0"; };
            compInfo = new ComparerInfo(false, true, false, getSql);
            mDictComparerInfo.Add("$has", compInfo);
            //
            getSql = (field, parameter) => { return "CHARINDEX(" + parameter.ParameterName + "," + field + ") = 0"; };
            compInfo = new ComparerInfo(false, true, false, getSql);
            mDictComparerInfo.Add("$nhas", compInfo);
            //
            getSql = (field, parameter) => { return "RIGHT(RTRIM(" + field + "),CASE WHEN LEN(" + parameter.ParameterName + ") = 0 THEN 1 ELSE LEN(" + parameter.ParameterName + ") END) = " + parameter.ParameterName; };
            compInfo = new ComparerInfo(false, true, false, getSql);
            mDictComparerInfo.Add("$ends", compInfo);
            //
            getSql = (field, parameter) => { return "RIGHT(RTRIM(" + field + "),CASE WHEN LEN(" + parameter.ParameterName + ") = 0 THEN 1 ELSE LEN(" + parameter.ParameterName + ") END) <> " + parameter.ParameterName; };
            compInfo = new ComparerInfo(false, true, false, getSql);
            mDictComparerInfo.Add("$nends", compInfo);
            //
            getSql = (field, parameter) => { return field + " IS NULL"; };
            compInfo = new ComparerInfo(true, false, true, getSql);
            mDictComparerInfo.Add("$null", compInfo);
            //
            getSql = (field, parameter) => { return field + " IS NOT NULL"; };
            compInfo = new ComparerInfo(true, false, true, getSql);
            mDictComparerInfo.Add("$nnull", compInfo);
            //
            getSql = (field, parameter) => { return "ISNULL(LTRIM(RTRIM(" + field + ")),'') = ''"; };
            compInfo = new ComparerInfo(true, false, false, getSql);
            mDictComparerInfo.Add("$empty", compInfo);
            //
            getSql = (field, parameter) => { return "ISNULL(LTRIM(RTRIM(" + field + ")),'') <> ''"; };
            compInfo = new ComparerInfo(true, false, false, getSql);
            mDictComparerInfo.Add("$nempty", compInfo);
            //

            //
            mDictColumnConfigInfo = new Dictionary<SqlDbType, ColumnConfigInfo>(30);
            mDictColumnConfigInfo.Add(SqlDbType.TinyInt, mColConfigInfoTinyInt);
            mDictColumnConfigInfo.Add(SqlDbType.SmallInt, mColConfigInfoSmallInt);
            mDictColumnConfigInfo.Add(SqlDbType.Int, mColConfigInfoInt);
            mDictColumnConfigInfo.Add(SqlDbType.BigInt, mColConfigInfoBigInt);
            mDictColumnConfigInfo.Add(SqlDbType.Decimal, mColConfigInfoDecimal);
            mDictColumnConfigInfo.Add(SqlDbType.Money, mColConfigInfoMoney);
            mDictColumnConfigInfo.Add(SqlDbType.SmallMoney, mColConfigInfoSmallMoney);
            mDictColumnConfigInfo.Add(SqlDbType.Float, mColConfigInfoFloat);
            mDictColumnConfigInfo.Add(SqlDbType.Real, mColConfigInfoReal);
            mDictColumnConfigInfo.Add(SqlDbType.DateTime, mColConfigInfoDateTime);
            mDictColumnConfigInfo.Add(SqlDbType.DateTime2, mColConfigInfoDateTime2);
            mDictColumnConfigInfo.Add(SqlDbType.SmallDateTime, mColConfigInfoSmallDateTime);
            mDictColumnConfigInfo.Add(SqlDbType.Date, mColConfigInfoDate);
            mDictColumnConfigInfo.Add(SqlDbType.Time, mColConfigInfoTime);
            mDictColumnConfigInfo.Add(SqlDbType.Bit, mColConfigInfoBit);
            mDictColumnConfigInfo.Add(SqlDbType.VarChar, mColConfigInfoVarChar);
            mDictColumnConfigInfo.Add(SqlDbType.NVarChar, mColConfigInfoNVarChar);
            mDictColumnConfigInfo.Add(SqlDbType.Char, mColConfigInfoChar);
            mDictColumnConfigInfo.Add(SqlDbType.NChar, mColConfigInfoNChar);
            mDictColumnConfigInfo.Add(SqlDbType.Text, mColConfigInfoText);
            mDictColumnConfigInfo.Add(SqlDbType.NText, mColConfigInfoNText);
            mDictColumnConfigInfo.Add(SqlDbType.Image, mColConfigInfoImage);
            mDictColumnConfigInfo.Add(SqlDbType.Binary, mColConfigInfoBinary);
            mDictColumnConfigInfo.Add(SqlDbType.VarBinary, mColConfigInfoVarBinary);
            mDictColumnConfigInfo.Add(SqlDbType.Timestamp, mColConfigInfoTimeStamp);
            mDictColumnConfigInfo.Add(SqlDbType.UniqueIdentifier, mColConfigInfoUniqueIdentifier);
            // Exoten
            mDictColumnConfigInfo.Add(SqlDbType.DateTimeOffset, mColConfigInfoDateTime);
            mDictColumnConfigInfo.Add(SqlDbType.Xml, mColConfigInfoXml);
            mDictColumnConfigInfo.Add(SqlDbType.Udt, mColConfigInfoUdt);
            mDictColumnConfigInfo.Add(SqlDbType.Variant, mColConfigInfoVariant);
            //
        }
        #endregion

        #region Private Methoden
        private async static Task<TableInfo> GetTableInfo(enMode mode, QueryItem item,
            bool getModelInfo, SqlConnection connection)
        {
            SqlCommand com = connection.CreateCommand();

            com.CommandTimeout =
                item.CommandTimeOut < 0 ?
                0 : item.CommandTimeOut;

            com.CommandText = "SELECT SCHEMA_NAME()";
            string defSchema = Convert.ToString(await com.ExecuteScalarAsync());

            string sql = item.Sql;
            bool withDistinct;

            bool hasSelect = HasSelect(ref sql, out withDistinct);

            if (sql == string.Empty)
            {
                string errMsg = "QueryItem: No Table/View/SQL-Select directive found ";
                throw new Exception(errMsg);
            }

            if (hasSelect)
                com.CommandText = sql;
            else
                com.CommandText = "SELECT * FROM " + sql;

            Dictionary<string, ColumnFormatInfo> dictFormat =
                ((IColumnFormat)item.ColumnFormat).Dictionary;

            Dictionary<string, InsertInfo> dictInsert =
                ((IDefaultInsertArgs)item.DefaultInsertArgs).Dictionary;

            SqlParameter p_Schema = new SqlParameter("@s", SqlDbType.NVarChar);
            SqlParameter p_Table = new SqlParameter("@t", SqlDbType.NVarChar);
            SqlParameter p_Column = new SqlParameter("@c", SqlDbType.NVarChar);

            com.Parameters.Add(p_Schema).Value = DBNull.Value;
            com.Parameters.Add(p_Table).Value = DBNull.Value;
            com.Parameters.Add(p_Column).Value = DBNull.Value;

            DataTable tblSchema = null;
            DataColumn c_Column = null;
            DataColumn c_BaseColumn = null;
            DataColumn c_BaseTable = null;
            DataColumn c_BaseSchema = null;
            DataColumn c_DbType = null;
            DataColumn c_Size = null;
            DataColumn c_Scale = null;
            DataColumn c_Precision = null;
            DataColumn c_Nullable = null;
            DataColumn c_ReadOnly = null;
            DataColumn c_Hidden = null;

            Dictionary<string, ColumnInfo> dictCols =
                new Dictionary<string, ColumnInfo>(StringComparer.OrdinalIgnoreCase);

            CommandBehavior flags =
                CommandBehavior.SchemaOnly |
                CommandBehavior.KeyInfo |
                CommandBehavior.SequentialAccess;

            // Wenn Schema/Tabelle nicht existiert, wird hier ein Fehler ausgelöst
            using (SqlDataReader rdr = await com.ExecuteReaderAsync(flags))
            {
                tblSchema = rdr.GetSchemaTable();
                c_Column = tblSchema.Columns["ColumnName"];
                c_BaseColumn = tblSchema.Columns["BaseColumnName"];
                c_BaseTable = tblSchema.Columns["BaseTableName"];
                c_BaseSchema = tblSchema.Columns["BaseSchemaName"];
                c_DbType = tblSchema.Columns["ProviderType"];
                c_Size = tblSchema.Columns["ColumnSize"];
                c_Scale = tblSchema.Columns["NumericScale"];
                c_Precision = tblSchema.Columns["NumericPrecision"];
                c_Nullable = tblSchema.Columns["AllowDBNull"];
                c_ReadOnly = tblSchema.Columns["IsReadOnly"];
                c_Hidden = tblSchema.Columns["IsHidden"];
            }
            //

            // Für Insert/Update:
            // Identity (ReadOnly) Spalten werden für VIEWS über die Methode
            // GetSchemaTable() des Readers nicht korrekt angezeigt und Default Spalten
            // werden nicht abgefragt. Daher ist diese zusätzliche Abfrage nötig.
            // (siehe Kommentar weiter unten).
            // Rückgabe:
            // Spalte mit Identity:    1
            // Spalte mit Defaultwert: 2
            // Sonst:                  3
            com.CommandText =
                "SELECT 'R'=CASE WHEN COLUMNPROPERTY(OBJECT_ID(@t),@c,'IsIdentity')=1 " +
                "AND COLUMN_NAME=@c THEN 1 WHEN COLUMN_DEFAULT IS NOT NULL THEN 2 " +
                "ELSE 3 END FROM INFORMATION_SCHEMA.COLUMNS WHERE " +
                "TABLE_SCHEMA=@s AND TABLE_NAME=@t AND COLUMN_NAME=@c";
            //

            List<string> columnList = new List<string>(tblSchema.Rows.Count);

            foreach (DataRow row in tblSchema.Rows)
            {
                string baseSchema =
                    row[c_BaseSchema] == DBNull.Value ?
                    defSchema : Convert.ToString(row[c_BaseSchema]);

                string baseTable = Convert.ToString(row[c_BaseTable]);
                string baseColumn = Convert.ToString(row[c_BaseColumn]);
                string column = Convert.ToString(row[c_Column]);
                SqlDbType dbType = (SqlDbType)Convert.ToInt32(row[c_DbType]);
                bool nullable = Convert.ToBoolean(row[c_Nullable]);
                bool readOnly = Convert.ToBoolean(row[c_ReadOnly]);
                bool hidden = Convert.ToBoolean(row[c_Hidden]);
                bool hasDefValue = false;
                object defValue = DBNull.Value;
                ColumnConfigInfo configInfo;

                // Verborgene Spalten werden ignoriert
                if (hidden)
                    continue;

                // Spalten mit unbekannten DB-Datentypen werden ignoriert
                if (!mDictColumnConfigInfo.TryGetValue(dbType, out configInfo))
                    continue;

                // Für Insert/Update:
                // Weitere nicht aktualisierbare (ReadOnly) Spalten werden ermittelt
                // und aktualisierbare Not-Null Spalten ohne DB-Defaultwert erhalten
                // den internen Defaultwert (siehe Kommentar oben)
                if (mode == enMode.Insert || mode == enMode.Update || getModelInfo)
                {
                    if (!nullable || !readOnly)
                    {
                        p_Column.Value = baseColumn;
                        p_Table.Value = row[c_BaseTable];
                        p_Schema.Value = baseSchema;

                        object res = await com.ExecuteScalarAsync();

                        if (res != null)
                        {
                            int r = Convert.ToInt32(res);

                            if (r == 1)
                                readOnly = true;
                            else if (r == 2)
                                hasDefValue = true;
                            else
                                defValue = configInfo.DefaultValue;
                        }
                        else
                            defValue = configInfo.DefaultValue;
                    }
                }
                //

                // Size, Precision, Scale
                int size, precision, scale;
                enFieldSize fieldSize = configInfo.FieldSize;

                if (fieldSize == enFieldSize.Size)
                {
                    size = Convert.ToInt32(row[c_Size]);
                    precision = -1;
                    scale = -1;
                }
                else if (fieldSize == enFieldSize.Precision)
                {
                    precision = Convert.ToInt32(row[c_Precision]);
                    size = -1;
                    scale = -1;
                }
                else if (fieldSize == enFieldSize.ScalePrecision)
                {
                    size = -1;
                    precision = Convert.ToByte(row[c_Precision]);
                    scale = Convert.ToByte(row[c_Scale]);
                }
                else
                {
                    size = -1;
                    precision = -1;
                    scale = -1;
                }
                //

                ConverterBase converter;

                // Nur bei Select:
                // 1) Spaltenliste wird mit den maskierten Spalten gefüllt
                // 2) Wenn ein Format zu Schema/Tabelle/Spalte gesetzt ist,
                //    wird ein Converter mit diesem Format übergeben

                if (mode == enMode.Select)
                {
                    columnList.Add("[" + column.Replace("]", "]]") + "]");

                    if (dictFormat == null)
                        converter = configInfo.Converter;
                    else
                    {
                        ColumnFormatInfo info;

                        if (dictFormat.TryGetValue(DeMask(column), out info))
                            converter = configInfo.Converter.GetClone(info.Format1, info.Format2);
                        else
                            converter = configInfo.Converter;
                    }
                }
                else
                    converter = configInfo.Converter;
                //

                ColumnInfo columnInfo =
                    new ColumnInfo(column, baseSchema, baseTable, baseColumn, configInfo.Kind,
                        configInfo.DataType, fieldSize, size, precision, scale, readOnly, nullable,
                        configInfo.Sortable, hasDefValue, defValue, configInfo.Compare, converter);



                // DIRTY HACK !!! ignore doppelten KEY 'BusinessEntityID' from -> Select * FROM Person.Person, Person.PersonPhone WHERE Person.Person.BusinessEntityID=Person.PersonPhone.BusinessEntityID    
                if (!dictCols.ContainsKey(column))
                    dictCols.Add(column, columnInfo); // double keys use shema "" xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx          


            }

            com.Parameters.Clear();
            return new TableInfo(com, sql, hasSelect, withDistinct, columnList, dictCols, dictInsert);
        }
        private static bool HasSelect(ref string sql, out bool withDistinct)
        {
            withDistinct = false;

            if (string.IsNullOrWhiteSpace(sql))
            {
                sql = string.Empty;
                return false;
            }

            const string select = "SELECT";
            const string distinct = "DISTINCT";

            bool hasSelect = false;

            for (int i = 0; i < sql.Length; i++)
            {
                char c = sql[i];

                if (char.IsWhiteSpace(c) || c == '(')
                    continue;

                if (!sql.Substring(i).StartsWith(select, StringComparison.OrdinalIgnoreCase))
                    break;

                int next = i + select.Length;

                if (sql.Length <= next)
                    break;

                c = sql[next];

                if (char.IsWhiteSpace(c))
                {
                    for (i = next + 1; i < sql.Length; i++)
                    {
                        char c1 = sql[i];

                        if (char.IsWhiteSpace(c1))
                            continue;

                        if (sql.Substring(i).StartsWith(distinct, StringComparison.OrdinalIgnoreCase))
                        {
                            sql = sql.Substring(0, next) + sql.Substring(i + distinct.Length);
                            withDistinct = true;
                        }

                        break;
                    }

                    hasSelect = true;
                    break;
                }
                else
                {
                    hasSelect = (c == '*' || c == '[');
                    break;
                }
            }

            if (!hasSelect)
                sql = sql.Trim();

            return hasSelect;
        }
        private static string DeMask(string dbObj)
        {
            if (dbObj.StartsWith("[") && dbObj.EndsWith("]"))
                return dbObj.Substring(1, dbObj.Length - 2).Replace("]]", "]");
            else
                return dbObj;
        }
        private static bool DeMask(ref string obj)
        {
            if (string.IsNullOrWhiteSpace(obj))
                return false;

            if ((obj = DeMask(obj.Trim())) == string.Empty)
                return false;

            return true;
        }

        public class GetItemResult
        {
            public QueryItem item { set; get; }
            public string errMsg { set; get; }
        }


        private async static Task<GetItemResult> GetItem(Arguments args, enMode mode, QueryItem item)
        {

            GetItemResult r = new GetItemResult();
            r.errMsg = null;
            r.item = item;

            // item muss über den Namen von Args
            // in QueryList gefunden werden.
            //
            if (string.IsNullOrWhiteSpace(args.Name))
            {
                r.errMsg = "Name missing";
                return r;
            }

            r.item = await QueryList.Get(args.Name);
            if (r.item == null)
            {
                r.errMsg = "'" + args.Name + "' is not matching with any DBQuery Item";
                return r;
            }
            //

            // Die aufgerufene CRUD Methode muss in
            // AllowedMethods gesetzt sein.
            //
            string method;

            if (mode == enMode.Select)
                method = "R";
            else if (mode == enMode.Insert)
                method = "C";
            else if (mode == enMode.Update)
                method = "U";
            else
                method = "D";

         
           if (r.item.AllowedMethods == null || r.item.AllowedMethods.IndexOf(method, StringComparison.OrdinalIgnoreCase) < 0)
                {
                    if (mode == enMode.Select)
                    method = "Read";
                else if (mode == enMode.Insert)
                    method = "Create";
                else if (mode == enMode.Update)
                    method = "Update";
                else
                    method = "Delete";

                r.errMsg = "Method '" + method + "' not allowed, No Rights in DBQuery Item declared for using it!";
                return r;
            }
            //


            // Token in item überschreibt Token in Settings.
            // Ein gültiges Token (nicht null oder leer) muss
            // mit dem Token in args exakt übereinstimmen.
            //
            string token = null;

            if (!string.IsNullOrWhiteSpace(r.item.Token))
                token = r.item.Token;
            else if (!string.IsNullOrWhiteSpace(Settings.Token))
                token = Settings.Token;

            if (token != null)
            {
                if (args.Token == null || !args.Token.Equals(token))
                {
                    r.errMsg = "Invalid Token";
                    return r;
                }
            }

            // Rights in item überschreibt Rights in Settings.
            // Ein gültiges Rights (nicht null oder leer) muss
            // mit dem Rights in args exakt übereinstimmen.
            //
            string rights = null;

            if (!string.IsNullOrWhiteSpace(r.item.Rights))
                rights = r.item.Rights;
            else if (!string.IsNullOrWhiteSpace(Settings.Rights))
                rights = Settings.Rights;

            if (rights != null)
            {
                if (args.Rights == null || checkRights(args.Rights, rights) == false )
                {
                    r.errMsg = "Not sufficient Rights";
                    return r;
                }
            }

            
            // wenn errmsg = null dann erfolgreicher return 
            return r;
        }

        private static bool checkRights(string argumentRights, string settingRights)
        {
            // settingRights 'CDEF'   and request with argumentRights='ABC' -> is valid because 'C' has the rights

            foreach (char c in argumentRights)
            {

                if (settingRights.Contains(c) == true) return true;
                
            }
            return false;
        }

        private static string GetConnectionString(QueryItem item)
        {

            // Original version
            return string.IsNullOrWhiteSpace(item.ConnectionString) ? Settings.ConnectionString : item.ConnectionString;
        
        }


        private static Action SetModel(StringBuilder sb, Dictionary<string, ColumnInfo> dictColumns)
        {
            return () =>
            {
                sb.Append("\"Model\":[");

                if (dictColumns != null)
                {
                    for (int i = 0; i < dictColumns.Count; i++)
                    {
                        if (i > 0)
                            sb.Append(',');

                        sb.Append(JsonConvert.SerializeObject(dictColumns.ElementAt(i).Key));
                    }
                }
                sb.Append("],");
            };
        }

        
        private static Action SetModelInfo(StringBuilder sb,
            System.Collections.ICollection collection,
            Func<int, KeyValuePair<string, ColumnInfo>> getColumnInfo)
        {
            return () =>
            {
                sb.Append("\"ModelInfo\":{");

                if (collection != null)
                {
                    for (int i = 0; i < collection.Count; i++)
                    {
                        if (i > 0)
                            sb.Append(',');

                        KeyValuePair<string, ColumnInfo> kvp = getColumnInfo(i);
                        ColumnInfo info = kvp.Value;

                        sb.Append(JsonConvert.SerializeObject(kvp.Key) +

                          ":{" +
                          "\"Kind\":" + JsonConvert.SerializeObject(info.Kind.ToString()) +
                          ",\"Type\":" + JsonConvert.SerializeObject(info.DbType.ToString()) +
                          ",\"Size\":" + JsonConvert.SerializeObject(info.Size) +
                          ",\"Scale\":" + JsonConvert.SerializeObject(info.Scale) +
                          ",\"Precision\":" + JsonConvert.SerializeObject(info.Precision) +
                          ",\"Nullable\":" + JsonConvert.SerializeObject(info.Nullable) +
                          ",\"HasDefaultValue\":" + JsonConvert.SerializeObject(info.HasDbDefaultValue) +
                          ",\"ReadOnly\":" + JsonConvert.SerializeObject(info.ReadOnly) +
                          ",\"Sortable\":" + JsonConvert.SerializeObject(info.Sortable) +
                          ",\"BaseSchema\":" + JsonConvert.SerializeObject(info.DbBaseSchema) +
                          ",\"BaseTable\":" + JsonConvert.SerializeObject(info.DbBaseTable) +
                          ",\"BaseColumn\":" + JsonConvert.SerializeObject(info.DbBaseColumn) +
                          "}");
                    }
                }
                sb.Append("},");
            };
        }


        private static Func<Tuple<int, long>> SetEmptyData(StringBuilder sb,
            int affected)
        {
            return () =>
            {
                sb.Append("\"Data\":[],");
                return new Tuple<int, long>(affected, 0);
            };
        }
        private static void SetResult(StringBuilder sb, string crud, Arguments args,
            Action setModel, Action setModelInfo, Func<Tuple<int, long>> setData, string error)
        {
            sb.Clear();

            // Method, Name, Distinct, Get, Filter,
            // Limit, Sort, Set, Message, PageIndex, PageSize
            sb.Append("{\"Method\":\"" + crud + "\",");
            sb.Append("\"Name\":" + JsonConvert.SerializeObject(args.Name) + ',');
            sb.Append("\"Distinct\":" + JsonConvert.SerializeObject(args.Distinct) + ',');
            sb.Append("\"Get\":" + JsonConvert.SerializeObject(args.Get) + ',');
            sb.Append("\"Filter\":" + JsonConvert.SerializeObject(args.Filter) + ',');
            sb.Append("\"Limit\":" + JsonConvert.SerializeObject(args.Limit) + ',');
            sb.Append("\"Sort\":" + JsonConvert.SerializeObject(args.Sort) + ',');
            sb.Append("\"Set\":" + JsonConvert.SerializeObject(args.Set) + ',');
            sb.Append("\"Message\":" + JsonConvert.SerializeObject(args.Message) + ',');
            sb.Append("\"PageIndex\":" + JsonConvert.SerializeObject(args.PageIndex) + ',');
            sb.Append("\"PageSize\":" + JsonConvert.SerializeObject(args.PageSize) + ',');
            //

            // Model
            setModel();
            //

            // ModelInfo
            setModelInfo();
            //

            // Data
            Tuple<int, long> result = setData();
            //

            // Affected, TotalReads
            sb.Append("\"Affected\":" + JsonConvert.SerializeObject(result.Item1) + ',');
            sb.Append("\"TotalReads\":" + JsonConvert.SerializeObject(result.Item2) + ',');
            //

            // Error
            if (string.IsNullOrWhiteSpace(error))
                error = null;
            sb.Append("\"Error\":" + JsonConvert.SerializeObject(error) + "}");
            //
        }
        private static bool GetLimits(Arguments args, out int limit,
            out int pageIndex, out int pageSize, ref string errMsg)
        {
            limit = 0;
            pageIndex = 0;
            pageSize = 0;

            enResult res;

            // Limit
            //
            if ((res = mInt32Converter.GetValue(args.Limit, out limit))
                == enResult.Failed)
            {
                errMsg = "Ungültiger Wert für Limit: "
                    + args.Limit + " (erwartet: Int32 Ganzzahl)";

                return false;
            }
            else if (res == enResult.Empty)
                limit = -1;
            //

            // PageIndex
            //
            if ((res = mInt32Converter.GetValue(args.PageIndex, out pageIndex))
                == enResult.Failed)
            {
                errMsg = "Ungültiger Wert für PageIndex: "
                    + args.PageIndex + " (erwartet: Int32 Ganzzahl)";

                return false;
            }
            else if (res == enResult.Empty)
                pageIndex = -1;
            //

            // PageSize
            //
            if ((res = mInt32Converter.GetValue(args.PageSize, out pageSize))
                == enResult.Failed)
            {
                errMsg = "Ungültiger Wert für PageSize: "
                    + args.PageSize + " (erwartet: Int32 Ganzzahl)";

                return false;
            }
            else if (res == enResult.Empty)
                pageSize = -1;
            //

            return true;
        }
        private static bool ParseTrue(string value)
        {
            if (value == null)
                return false;

            string temp = value.Trim();

            return ("1".Equals(temp) || "true".Equals(temp, StringComparison.OrdinalIgnoreCase));
        }
        private static bool UseJsonForSet(string set)
        {
            if (string.IsNullOrEmpty(set))
                return false;

            for (int i = 0; i < set.Length; i++)
            {
                char c = set[i];

                if (char.IsWhiteSpace(c))
                    continue;

                if (c == '{')
                    return true;

                return false;
            }

            return false;
        }
        public static JsonSerializer CreateJsonSerializer()
        {
            JsonSerializer serializer = new JsonSerializer();
            serializer.Culture = CultureInfo.InvariantCulture;
            serializer.Formatting = Formatting.Indented;
            serializer.DateFormatHandling = DateFormatHandling.IsoDateFormat;
            serializer.DateParseHandling = DateParseHandling.DateTime;
            return serializer;
        }
        #endregion

        #region Öffentliche Methoden: Deserialize JSON Result To Result Object, Result.Data to List<Dict<string,string>>, Result.Model to List<string>,, Result.Error to string
        /// <summary>
        /// Deserializiert einen JSON-String als Resultat von ExecuteAsync zu einem .NET Result Object
        /// </summary>
        /// <param name="json"></param>
        /// <returns></returns>
        public static Result DeserializeToResultObject(string json)
        {
            return JsonConvert.DeserializeObject<Result>(json);
        }
        /// <summary>
        /// Deserializiert einen JSON-String als Resultat von ExecuteAsync. Wandelt 'Data' aus dem Resultat in ein .NET Object 'List of Dictionaries' um
        /// </summary>
        /// <param name="json"></param>
        /// <returns></returns>
        public static List<Dictionary<string, string>> DeserializeDataToListOfDict(string json)
        {
            return JsonConvert.DeserializeObject<DbQuery.Result>(json).Data;
        }
        /// <summary>
        /// Deserializiert einen JSON-String als Resultat von ExecuteAsync. Wandelt 'Model' aus dem Resultat in ein .NET Object 'List of Strings' um 
        /// </summary>
        /// <param name="json"></param>
        /// <returns></returns>
        public static List<string> DeserializeModelToListOfString(string json)
        {
            return JsonConvert.DeserializeObject<DbQuery.Result>(json).Model;
        }
        /// <summary>
        /// Deserializiert einen JSON-String als Resultat von ExecuteAsync. Wandelt 'Error' aus dem Resultat in ein .NET Object 'string' um 
        /// </summary>
        /// <param name="json"></param>
        /// <returns></returns>
        public static string DeserializeErrorToString(string json)
        {
            return JsonConvert.DeserializeObject<DbQuery.Result>(json).Error;
        }

        #endregion



        #region Öffentliche Methoden: CRUD ASYNC

        // WARNING: WOKO NOT SECURED ALTER TABLE ONLY DEMO!!!! HERE
        /// <summary>
        /// Alter Tables for Using Ganyweb with SchillermedDB
        /// </summary>
        /// <returns></returns>
        public static async Task<string> AlterTable(Arguments args)
        {

            // check if token !!!
            if (args.Token != "1F6StinFgHgtDyMEEUFBsiFb743sSDKSQk") return JsonConvert.SerializeObject("Invalid Token!");

            List<string> alter = new List<string>();

            alter.Add(@"ALTER TABLE [dbo].[Benutzer] ADD [wk_signadd][char](64) NULL; " );
            alter.Add(@"ALTER TABLE [dbo].[Benutzer] ADD [wk_signpicture][image] NULL; " );
            alter.Add(@"ALTER TABLE Benutzer ADD wk_Rechte char(16) NULL; " );

            alter.Add(@"ALTER TABLE Befund ADD [wk_Diktat][char](20) NULL");
            alter.Add(@"ALTER TABLE Befund ADD [wk_Schreibkraft][char](20) NULL");
            alter.Add(@"ALTER TABLE Befund ADD [wk_Unterschrift][char](20) NULL");
            alter.Add(@"ALTER TABLE Befund ADD [wk_UnterschriftDatum][datetime] NULL");
            alter.Add(@"ALTER TABLE Befund ADD [wk_Status][int] NOT NULL");
            alter.Add(@"ALTER TABLE Befund ADD [wk_AusgangsDatum] [datetime]NULL");
            alter.Add(@"ALTER TABLE Befund ADD [wk_AusgangsUnterschrift][char](20) NULL");
            alter.Add(@"ALTER TABLE Befund ADD [wk_AusgangsArt][char](64) NULL");
       
            // Console.WriteLine("Settings.ConnectionString:" + Settings.ConnectionString);

            try
            {
                
                foreach (var item in alter)
                {

                    using (SqlConnection con = new SqlConnection(Settings.ConnectionString))
                    {
                        con.Open();


                        using (SqlCommand cmd = new SqlCommand(item, con))
                        {

                            //Console.WriteLine("Command.alter inner:" + alter);

                            try
                            {
                                await cmd.ExecuteNonQueryAsync();
                            }
                            catch (Exception e)
                            {
                              //  Console.WriteLine(item + " in AlterTable makes ERROR:" + e.Message);
                            }
                        }
                    }
                    
                }


                return JsonConvert.SerializeObject("Alter Table executed!");
                
            }
            catch (Exception x)
            {

                //Console.WriteLine("ExecuteAlterTable:" + x.Message);
                return JsonConvert.SerializeObject("Alter Table ERROR: " + x.Message);
            }
                
            
        }

        /// <summary>
        /// Generiert eine SQL-Insert Anweisung und führt 
        /// diese gegen eine SQL Server Datenbank aus.
        /// </summary>
        /// <param name="args">
        /// Enthält die Parameter für die CRUD Methode.
        /// </param>
        /// <returns>
        /// Ein in einen JSON String serialisiertes Objekt vom 
        /// Typ Result mit den Ergebnisdaten der Abfrage.
        /// </returns>
        public static async Task<string> CreateAsync(Arguments args)
        {
            string crud = "C";
            args = args ?? new Arguments();
            QueryItem item;
            StringBuilder sb = new StringBuilder(400);
            int affected = -1;
            string errMsg = null;
            Dictionary<string, ColumnInfo> dictColumns = null;
            Func<int, KeyValuePair<string, ColumnInfo>> getColumnInfo = null;
            bool withModelInfo = ParseTrue(args.GetModelInfo);
            GetItemResult gIR;
            try
            {
                gIR = await GetItem(args, enMode.Insert, null);
                if (gIR.errMsg != null)
                    throw new Exception(gIR.errMsg);
                item = gIR.item;

                using (SqlConnection con = new SqlConnection(GetConnectionString(item)))
                {
                    con.Open();

                    int count;
                    Parser parser = new Parser(); ;
                    TableInfo tableInfo = await GetTableInfo(enMode.Insert, item, true, con);

                    if (withModelInfo)
                    {
                        dictColumns = tableInfo.DictColumns;
                        getColumnInfo = (i) => { return dictColumns.ElementAt(i); };
                    }

                    if (!parser.ParseInsert(UseJsonForSet(args.Set), tableInfo,
                        args.Set, sb, out count, ref errMsg))
                        throw new Exception("Set: " + errMsg);

                    if (count == 0)
                    {
                        SetResult(sb, crud, args, SetModel(sb, dictColumns),
                            SetModelInfo(sb, dictColumns, getColumnInfo), SetEmptyData(sb, 0), errMsg);

                        return sb.ToString();
                    }

                    tableInfo.Command.CommandText = sb.ToString();
                    affected = await tableInfo.Command.ExecuteNonQueryAsync();

                    SetResult(sb, crud, args, SetModel(sb, dictColumns),
                        SetModelInfo(sb, dictColumns, getColumnInfo), SetEmptyData(sb, affected), errMsg);

                    return sb.ToString();
                }
            }
            catch (Exception ex)
            {
                SetResult(sb, crud, args, SetModel(sb, dictColumns),
                    SetModelInfo(sb, dictColumns, getColumnInfo), SetEmptyData(sb, affected), ex.Message);

                return sb.ToString();
            }
        }
        /// <summary>
        /// Generiert eine SQL-Select Anweisung und führt 
        /// diese gegen eine SQL Server Datenbank aus.
        /// </summary>
        /// <param name="args">
        /// Enthält die Parameter für die CRUD Methode.
        /// </param>
        /// <returns>
        /// Ein in einen JSON String serialisiertes Objekt vom 
        /// Typ Result mit den Ergebnisdaten der Abfrage.
        /// </returns>
        public static async Task<string> ReadAsync(Arguments args)
        {
            string crud = "R";
            args = args ?? new Arguments();
            QueryItem item;
            StringBuilder sb = new StringBuilder(8000);
            bool getModelInfo = ParseTrue(args.GetModelInfo);
            List<Tuple<int, string, ColumnInfo>> infos = null;
            string errMsg = null;


            // Model:
            // Wenn Model nicht ermittelt werden konnte (infos = null),
            // wird ein leeres Model serialisert
            Action setModel = () =>
            {
                sb.Append("\"Model\":[");

                if (infos != null)
                {
                    for (int i = 0; i < infos.Count; i++)
                    {
                        if (i > 0)
                            sb.Append(',');

                        sb.Append(infos[i].Item2);
                    }
                }
                sb.Append("],");
            };
            //

            // ModelInfo:
            // Wenn ModelInfo nicht ermittelt werden konnte (infos = null),
            // wird eine leere ModelInfo serialisiert
            Action setModelInfo = () =>
            {
                sb.Append("\"ModelInfo\":{");

                if (infos != null && getModelInfo)
                {
                    for (int i = 0; i < infos.Count; i++)
                    {
                        if (i > 0)
                            sb.Append(',');

                        ColumnInfo ci = infos[i].Item3;

                        sb.Append(infos[i].Item2 +

                          ":{" +
                          "\"Kind\":" + JsonConvert.SerializeObject(ci.Kind.ToString()) +
                          ",\"Type\":" + JsonConvert.SerializeObject(ci.DbType.ToString()) +
                          ",\"Size\":" + JsonConvert.SerializeObject(ci.Size) +
                          ",\"Scale\":" + JsonConvert.SerializeObject(ci.Scale) +
                          ",\"Precision\":" + JsonConvert.SerializeObject(ci.Precision) +
                          ",\"Nullable\":" + JsonConvert.SerializeObject(ci.Nullable) +
                          ",\"HasDefaultValue\":" + JsonConvert.SerializeObject(ci.HasDbDefaultValue) +
                          ",\"ReadOnly\":" + JsonConvert.SerializeObject(ci.ReadOnly) +
                          ",\"Sortable\":" + JsonConvert.SerializeObject(ci.Sortable) +
                          ",\"BaseSchema\":" + JsonConvert.SerializeObject(ci.DbBaseSchema) +
                          ",\"BaseTable\":" + JsonConvert.SerializeObject(ci.DbBaseTable) +
                          ",\"BaseColumn\":" + JsonConvert.SerializeObject(ci.DbBaseColumn) +
                          "}");
                    }
                }
                sb.Append("},");
            };

            try
            {
                GetItemResult gIR;
                gIR = await GetItem(args, enMode.Select, null);
                if (gIR.errMsg != null)
                    throw new Exception(gIR.errMsg);
                item = gIR.item;

                using (SqlConnection con = new SqlConnection(GetConnectionString(item)))
                {
                    con.Open();

                    Parser parser = new Parser();
                    TableInfo tableInfo = await GetTableInfo(enMode.Select, item, getModelInfo, con);
                    int limit, pageIndex, pageSize;
                    bool empty;

                    bool checkLimits =
                        GetLimits(args, out limit, out pageIndex, out pageSize, ref errMsg);

                    int start = pageIndex < 0 ? 0 : pageIndex;
                    long end = start + (pageSize <= 0 ? int.MaxValue : pageSize);

                    if (!parser.ParseSelect(tableInfo, ParseTrue(args.Distinct),
                        limit, args.Get, sb, ref errMsg))
                        throw new Exception("Get: " + errMsg);

                    if (!parser.ParseWhere(tableInfo, args.Filter, sb, out empty, ref errMsg))
                        throw new Exception("Filter: " + errMsg);

                    if (!parser.ParseSort(tableInfo, args.Sort, sb, ref errMsg))
                        throw new Exception("Sort: " + errMsg);

                    tableInfo.Command.CommandText = sb.ToString();

                    using (SqlDataReader rdr = await tableInfo.Command.ExecuteReaderAsync())
                    {
                        int count = rdr.FieldCount;
                        ColumnInfo ci;

                        infos = new List<Tuple<int, string, ColumnInfo>>(count);

                        for (int i = 0; i < count; i++)
                        {
                            if (tableInfo.DictColumns.TryGetValue(rdr.GetName(i), out ci))
                            {
                                infos.Add(new Tuple<int, string, ColumnInfo>
                                    (i, JsonConvert.SerializeObject(ci.DbColumn), ci));
                            }
                        }

                        // Bei ungültigen Werten für limit, pageIndex oder pageSize wird erst
                        // hier ein Fehler ausgelöst, so kann Model noch serialisiert werden
                        if (!checkLimits)
                            throw new Exception(errMsg);

                        // Data
                        //
                        Func<Tuple<int, long>> setData = () =>
                        {
                            int affected = 0;
                            long totalReads = 0;

                            // Zeilen mit Index <= start (pageIndex) werden nur
                            // in totalReads mitgezählt, aber nicht serialisiert
                            for (; totalReads < start && rdr.Read(); totalReads++)
                                ;

                            sb.Append("\"Data\":[");

                            while (rdr.Read())
                            {
                                // Zeilen mit Index > end (pageIndex + pageSize) werden
                                // weiter in totalReads mitgezählt, aber nicht serialisiert
                                if (++totalReads > end)
                                    continue;

                                // In affected werden nur serialisierte Zeilen mitgezählt
                                if (affected++ == 0)
                                    sb.Append('{');
                                else
                                    sb.Append(",{");

                                for (int i = 0; i < infos.Count; i++)
                                {
                                    if (i > 0)
                                        sb.Append(',' + infos[i].Item2 + ':');
                                    else
                                        sb.Append(infos[i].Item2 + ':');

                                    infos[i].Item3.Converter.AppendSerialized(rdr, infos[i].Item1, sb);
                                }
                                sb.Append('}');
                            }
                            sb.Append("],");

                            return new Tuple<int, long>(affected, totalReads);
                        };
                        //

                        SetResult(sb, crud, args, setModel, setModelInfo, setData, errMsg);
                        return sb.ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                SetResult(sb, crud, args, setModel, setModelInfo, SetEmptyData(sb, 0), ex.Message);
                return sb.ToString();
            }
        }
        /// <summary>
        /// Generiert eine SQL-Update Anweisung und führt 
        /// diese gegen eine SQL Server Datenbank aus.
        /// </summary>
        /// <param name="args">
        /// Enthält die Parameter für die CRUD Methode.
        /// </param>
        /// <returns>
        /// Ein in einen JSON String serialisiertes Objekt vom 
        /// Typ Result mit den Ergebnisdaten der Abfrage.
        /// </returns>
        public static async Task<string> UpdateAsync(Arguments args)
        {
            string crud = "U";
            args = args ?? new Arguments();
            
            StringBuilder sb = new StringBuilder(400);
            string errMsg = null;
            int affected = -1;
            Dictionary<string, ColumnInfo> dictColumns = null;
            Func<int, KeyValuePair<string, ColumnInfo>> getColumnInfo = null;
            bool withModelInfo = ParseTrue(args.GetModelInfo);

            try
            {
                GetItemResult gIR = await GetItem(args, enMode.Update, null);
                if (gIR.errMsg != null)  throw new Exception(gIR.errMsg);
             
                QueryItem item = gIR.item;
                
                using (SqlConnection con = new SqlConnection(GetConnectionString(item)))
                {
                    con.Open();

                    int count;
                    bool empty;
                    Parser parser = new Parser();
                    TableInfo tableInfo = await GetTableInfo(enMode.Update, item, true, con);

                    if (withModelInfo)
                    {
                        dictColumns = tableInfo.DictColumns;
                        getColumnInfo = (i) => { return dictColumns.ElementAt(i); };
                    }

                    if (!parser.ParseUpdate(UseJsonForSet(args.Set), tableInfo,
                        args.Set, sb, out count, ref errMsg))
                        throw new Exception("Set: " + errMsg);

                    if (count == 0)
                    {
                        SetResult(sb, crud, args, SetModel(sb, dictColumns),
                            SetModelInfo(sb, dictColumns, getColumnInfo), SetEmptyData(sb, 0), errMsg);

                        return sb.ToString();
                    }

                    if (!parser.ParseWhere(tableInfo, args.Filter, sb, out empty, ref errMsg))
                        throw new Exception("Filter: " + errMsg);

                    // Leerer Filter ist bei Update/Delete nicht erlaubt
                    if (empty)
                    {
                        errMsg = "Update requires at least 1 Filter-Condition";
                        throw new Exception(errMsg);
                    }
                    //

                    tableInfo.Command.CommandText = sb.ToString();

                    affected = await tableInfo.Command.ExecuteNonQueryAsync();

                    SetResult(sb, crud, args, SetModel(sb, dictColumns),
                        SetModelInfo(sb, dictColumns, getColumnInfo), SetEmptyData(sb, affected), errMsg);

                    return sb.ToString();
                }
            }
            catch (Exception ex)
            {
                SetResult(sb, crud, args, SetModel(sb, dictColumns),
                    SetModelInfo(sb, dictColumns, getColumnInfo), SetEmptyData(sb, affected), ex.Message);

                return sb.ToString();
            }
        }
        /// <summary>
        /// Generiert eine SQL-Delete Anweisung und führt 
        /// diese gegen eine SQL Server Datenbank aus.
        /// </summary>
        /// <param name="args">
        /// Enthält die Parameter für die CRUD Methode.
        /// </param>
        /// <returns>
        /// Ein in einen JSON String serialisiertes Objekt vom 
        /// Typ Result mit den Ergebnisdaten der Abfrage.
        /// </returns>
        public static async Task<string> DeleteAsync(Arguments args)
        {
            string crud = "D";
            args = args ?? new Arguments();
            QueryItem item;
            StringBuilder sb = new StringBuilder(160);
            string errMsg = null;
            int affected = -1;
            Dictionary<string, ColumnInfo> dictColumns = null;
            Func<int, KeyValuePair<string, ColumnInfo>> getColumnInfo = null;
            bool withModelInfo = ParseTrue(args.GetModelInfo);

            try
            {

                GetItemResult gIR;
                gIR = await GetItem(args, enMode.Delete, null);
                if (gIR.errMsg != null)
                    throw new Exception(gIR.errMsg);
                item = gIR.item;


                using (SqlConnection con = new SqlConnection(GetConnectionString(item)))
                {
                    con.Open();

                    Parser parser = new Parser();
                    TableInfo tableInfo = await GetTableInfo(enMode.Delete, item, withModelInfo, con);

                    if (withModelInfo)
                    {
                        dictColumns = tableInfo.DictColumns;
                        getColumnInfo = (i) => { return dictColumns.ElementAt(i); };
                    }

                    bool empty;

                    string tbl =
                        !tableInfo.HasSelect ?
                        tableInfo.Sql :
                        "V FROM (" + tableInfo.Sql + ")V";

                    sb.Append("DELETE FROM " + tbl + ' ');

                    if (!parser.ParseWhere(tableInfo, args.Filter, sb, out empty, ref errMsg))
                        throw new Exception("Filter: " + errMsg);

                    // Leerer Filter ist bei Update/Delete nicht erlaubt
                    if (empty)
                    {
                        errMsg = "Delete requires at least 1 Filter-Condition";
                        throw new Exception(errMsg);
                    }
                    //

                    tableInfo.Command.CommandText = sb.ToString();
                    affected = await tableInfo.Command.ExecuteNonQueryAsync();

                    SetResult(sb, crud, args, SetModel(sb, dictColumns),
                        SetModelInfo(sb, dictColumns, getColumnInfo), SetEmptyData(sb, affected), errMsg);

                    return sb.ToString();
                }
            }
            catch (Exception ex)
            {
                SetResult(sb, crud, args, SetModel(sb, dictColumns),
                    SetModelInfo(sb, dictColumns, getColumnInfo), SetEmptyData(sb, affected), ex.Message);

                return sb.ToString();
            }
        }
        
        /// <summary>
        /// Generiert eine SQL Anweisung und führt 
        /// diese gegen eine SQL Server Datenbank aus.
        /// Gültige Werte von Methods des übergebenen Arguments 
        /// Objektes: C, R, U, D 
        /// (für Create, Read, Update, Delete)
        /// </summary>
        /// <param name="args">
        /// Enthält die Parameter für die CRUD Methode.
        /// </param>
        /// <returns>
        /// Ein in einen JSON String serialisiertes Objekt vom 
        /// Typ Result mit den Ergebnisdaten der Abfrage.
        /// </returns>
        ///         
        public static async Task<string> ExecuteAsync(Arguments args)
        {
            args = args ?? new Arguments();
            string method = args.Method ?? string.Empty;
            string crud = method.Trim().ToUpper();
            

            switch (crud)
            {
                case "A":
                    return await AlterTable(args);
                case "C":
                    return await CreateAsync(args);
                case "R":
                    return await ReadAsync(args);
                case "U":
                    return await UpdateAsync(args);
                case "D":
                    return await DeleteAsync(args);
                default:

                    string errMsg = crud == string.Empty ?
                        "Fehlende Methode" :
                        "Ungültige Methode: '" + method.Trim() + "'";

                    StringBuilder sb = new StringBuilder(240);

                    SetResult(sb, method, args, SetModel(sb, null), SetModelInfo(sb, null, null),
                        SetEmptyData(sb, 0), errMsg);

                    return sb.ToString();
            }
        }


        // PUBLIC INSTANCE WRAPPER for Node.js and Edge.js ------------------------------------------------------------------------------------------

        /// <summary>
        /// Node EDGE.JS Wrapper with correct signatur for ExecuteAsync
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public async Task<object> ExecuteQuery(dynamic input)
        {
            /* 
             * Node.JS Script 
             * 
                var	edge = require('edge'),
 	            db={};

                db.query= edge.func({
    	            assemblyFile: __dirname + '/dbQuery/dbQuery/bin/Debug/dbQuery.dll',
    	            typeName: 'webQL.dbQuery.DbQuery',
    	            methodName: 'ExecuteQuery' // Func<object,Task<object>>
	            });

                db.load = edge.func({
    	            assemblyFile: __dirname + '/dbQuery/dbQuery/bin/Debug/dbQuery.dll',
    	            typeName: 'webQL.dbQuery.DbQuery',
    	            methodName: 'LoadConfig' // Func<object,Task<object>>
	            });

             */
            DbQuery.Arguments args = (DbQuery.Arguments)JsonConvert.DeserializeObject(JsonConvert.SerializeObject(input), typeof(DbQuery.Arguments));

            // // only for GANYWEB!!
            //if (args.Message == "GanymedSelection") args.Filter = Basics.Tools.Ganymed.PatientFilterCRUD(args.Filter);

            return await ExecuteAsync(args);
        }
        public async Task<object> LoadConfig(dynamic input)
        {

            Exception ex = null;
            DbQuery.QueryList.LoadConfig(out ex);
            if (ex != null) return "Error DbQuery.QueryList.LoadConfig:" + ex.Message;
            return null;
        }



        #endregion

        #region Öffentliche Methoden: Generate a JSON Object with all TABLES|VIEWS of the connected Database
        public static bool GenerateJSONConfigurationFromDatabase(string fileName, ConfigMode mode,
            QueryItem baseItem, out Exception ex)
        {
            ex = null;

            string where;

            if (mode == ConfigMode.Table)
                where = " WHERE TABLE_TYPE='BASE TABLE' ";
            else if (mode == ConfigMode.View)
                where = " WHERE TABLE_TYPE='VIEW' ";
            else
                where = " WHERE TABLE_TYPE IN('BASE TABLE','VIEW') ";

            string sql =
                "SELECT " +
                "'['+REPLACE(TABLE_SCHEMA,']',']]')" +
                "+'].['+" +
                "REPLACE(TABLE_NAME,']',']]')+']' NAME " +
                "FROM INFORMATION_SCHEMA.TABLES" + where;

            Dictionary<string, JsonConfiguration.JsonQueryItem> jsonItems =
                new Dictionary<string, JsonConfiguration.JsonQueryItem>
                (StringComparer.OrdinalIgnoreCase);

            try
            {
                using (SqlConnection con = new SqlConnection(GetConnectionString(baseItem)))
                {
                    con.Open();
                    SqlCommand com = con.CreateCommand();
                    com.CommandText = sql;

                    using (SqlDataReader rdr = com.ExecuteReader())
                    {
                        while (rdr.Read())
                        {
                            string name = rdr.GetString(0);

                            if (jsonItems.ContainsKey(name))
                                continue;

                            JsonConfiguration.JsonQueryItem temp =
                                new JsonConfiguration.JsonQueryItem();

                            temp.AllowedMethods = baseItem.AllowedMethods;
                            temp.AllowedTableType = baseItem.AllowedTableType;
                            temp.ConnectionString = baseItem.ConnectionString;
                            temp.CultureName = baseItem.CultureName;
                            temp.CommandTimeOut = baseItem.CommandTimeOut;
                            temp.Token = baseItem.Token;
                            temp.Rights = baseItem.Rights;
                            temp.Sql = name;

                            jsonItems.Add(name, temp);
                        }
                    }
                }

                JsonConfiguration jsonConfig = new JsonConfiguration(jsonItems);
                JsonSerializer serializer = CreateJsonSerializer();

                using (StreamWriter writer = new StreamWriter(fileName, false, Encoding.UTF8))
                {
                    serializer.Serialize(writer, jsonConfig);
                }

                return true;
            }
            catch (Exception exc)
            {
                ex = exc;
                return false;
            }
        }
        public static bool GenerateJSONConfigurationFromDatabase(string fileName, ConfigMode mode,
            QueryItem baseItem)
        {
            Exception ex;
            return GenerateJSONConfigurationFromDatabase(fileName, mode, baseItem, out ex);
        }
        public static bool GenerateJSONConfigurationFromDatabase(ConfigMode mode, QueryItem baseItem,
            out Exception ex)
        {
            string fileName = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), FILENAME_JSON_DEFAULT);
            return GenerateJSONConfigurationFromDatabase(fileName, mode, baseItem, out ex);
        }
        public static bool GenerateJSONConfigurationFromDatabase(ConfigMode mode, QueryItem baseItem)
        {
            Exception ex;
            return GenerateJSONConfigurationFromDatabase(mode, baseItem, out ex);
        }
        #endregion

    }

}
