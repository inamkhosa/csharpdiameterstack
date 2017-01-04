using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
using System.Data;

namespace DiameterStack
{

    /// <summary>
    /// @Author Inam Khosa 
    /// @Date: October, 2013
    /// </summary>

    public struct AttributeInfo
    {
        public string AttributeName { set; get; }
        public int AvpCode { set; get; }
        public string DataType { set; get; }
        public bool isMandatory { set; get; }
        public bool isEncrypted { set; get; }
        public bool isVendorSpecific { set; get; }
        public bool isGrouped { set; get; }
        public int VendorId { set; get; }
    }
    /// <summary>
    /// 
    /// </summary>
    public class AvpDictionary
    {

        Dictionary<int, AttributeInfo> Attributes { set; get; }
        
        string dictionaryXMLPath { set; get; }

        /// <summary>
        /// 
        /// </summary>
        public AvpDictionary()
        {

        }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public Dictionary<int, AttributeInfo> LoadDictionary()
        {
            Dictionary<int, AttributeInfo> dictionary = new Dictionary<int, AttributeInfo>();
            
            int AttributeCode = -1;

            try
            {
             
 
                dictionaryXMLPath =ConfigurationManager.AppSettings["DictionaryFile"];
               
                DataSet ds = new DataSet();

                ds.ReadXml(dictionaryXMLPath);

                if (ds.Tables.Count > 0)
                    foreach (DataRow row in ds.Tables["avp"].Rows)
                    {
                        AttributeCode = Convert.ToInt32(row["code"]);

                        AttributeInfo attributeInfo = new AttributeInfo();

                        attributeInfo.AvpCode = Convert.ToInt32(row["code"]);

                        attributeInfo.AttributeName = row["name"].ToString();

                        attributeInfo.DataType = row["type"].ToString().Trim();

                        if (row["isVendorSpecific"].ToString().Trim() == "true")

                            attributeInfo.isVendorSpecific = true;
                        else
                            attributeInfo.isVendorSpecific = false;

                        attributeInfo.VendorId = Convert.ToInt32(row["VendorId"].ToString().Trim());

                        if (attributeInfo.DataType == "Grouped")
                            attributeInfo.isGrouped = true;
                        else
                            attributeInfo.isGrouped = false;

                        if (row["isMandatory"].ToString().Trim() == "true")
                            attributeInfo.isMandatory = true;
                        else
                            attributeInfo.isMandatory = false;

                        if (row["isEncrypted"].ToString().Trim() == "true")
                            attributeInfo.isEncrypted = true;
                        else
                            attributeInfo.isEncrypted = false;

                        if(dictionary.ContainsKey(AttributeCode))
                            throw new Exception("AVP "+AttributeCode+" already in dictionary");
                        else
                            dictionary.Add(AttributeCode, attributeInfo);
                    }

                return dictionary;
            }
            catch (Exception exp)
            {
                int count = dictionary.Count;
                Common.StackLog.Write2TraceLog("AvpDictionary::LoadDictionary" , AttributeCode.ToString() +" "+ exp.ToString());
                throw exp;
            }

        }

    }
}
